import pytest
import os
from playwright.sync_api import sync_playwright, expect
import time
import json
from utils.db import db_helper, drop_all_dynamic_content
from utils.api import api_helper
import requests
from datetime import datetime, timezone

# Config
BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")
E2E_LANG = os.getenv("E2E_LANG", "en").strip() or "en"
VIDEO_DIR = "tests/e2e/videos"
SCREENSHOT_DIR = "tests/e2e/screenshots"
_STANDARD_PRODUCT_CACHE = None
_E2E_DURATIONS = []  # list[dict]

@pytest.fixture(scope="session", autouse=True)
def ensure_admin_exists():
    """
    Ensure the environment is initialized for E2E runs.

    Many UI flows rely on setup being done and persisted (DB), but tests run in isolated browser contexts.
    """
    # Batch6: Hard reset test/perf dynamic content before starting a full E2E run.
    try:
        drop_all_dynamic_content(strict=True)
    except Exception as ex:
        pytest.fail(f"Global cleanup failed before E2E session: {ex}")

    last_error = None
    for attempt in range(1, 8):
        try:
            resp = requests.post(
                f"{API_BASE}/api/setup/admin",
                json={"username": "admin", "email": "admin@example.com", "password": "Admin@12345"},
                headers={"X-Lang": E2E_LANG.lower()},
                timeout=15,
            )
            resp.raise_for_status()

            # Ensure system default templates exist for all seeded entity definitions.
            regen = requests.post(
                f"{API_BASE}/api/admin/templates/regenerate-defaults",
                headers={"X-Lang": E2E_LANG.lower()},
                timeout=60,
            )
            regen.raise_for_status()
            break
        except Exception as ex:
            last_error = ex
            time.sleep(0.8)

    if last_error is not None:
        pytest.fail(f"Failed to initialize E2E admin/templates after retries: {last_error}")

    yield

    # Batch6: Ensure zero residual test/perf tables after E2E run.
    try:
        after = drop_all_dynamic_content(strict=True)
        leaked = after.get("dropped_tables") or []
        if leaked:
            print(f"[E2E] Global cleanup removed leaked tables at session end: {leaked}")
    except Exception as ex:
        pytest.fail(f"Global cleanup failed after E2E session: {ex}")

@pytest.fixture
def clean_platform():
    """
    清理 Batch 1 测试产生的动态实体元数据和物理表。
    不要清理 System Admin 用户。
    """
    # 1. 物理表清理 (Hard Drop)
    # DefaultTableName = EntityName + "s"（系统当前实现），同时兼容指令中的表名写法
    entity_names = ["TypeTester", "Constrainer", "EvoEntity", "ParentEnt", "ChildEnt"]
    target_tables = set(entity_names + [f"{n}s" for n in entity_names])

    for tbl in sorted(target_tables):
        db_helper.execute_query(f'DROP TABLE IF EXISTS "{tbl}" CASCADE')
        db_helper.execute_query(f'DROP TABLE IF EXISTS {tbl.lower()} CASCADE')

    # 2. 元数据清理 (Metadata)
    # 注意顺序：先删 Field，再删 Entity
    cleanup_sql = """
    DELETE FROM "FieldMetadatas" 
    WHERE "EntityDefinitionId" IN (
        SELECT "Id" FROM "EntityDefinitions" 
        WHERE "EntityName" IN ('TypeTester', 'Constrainer', 'EvoEntity', 'ParentEnt', 'ChildEnt')
    );
    DELETE FROM "EntityDefinitions" 
    WHERE "EntityName" IN ('TypeTester', 'Constrainer', 'EvoEntity', 'ParentEnt', 'ChildEnt');
    """.strip()
    db_helper.execute_query(cleanup_sql)

    yield
    # Post-check (Optional)

@pytest.fixture(scope="session")
def browser_context_args(browser_context_args):
    return {
        **browser_context_args,
        "record_video_dir": VIDEO_DIR,
        "record_video_size": {"width": 1280, "height": 720}
    }

@pytest.fixture(scope="function")
def context(browser):
    # Create distinct context for each test to ensure isolation
    context = browser.new_context(
        record_video_dir=VIDEO_DIR,
        record_video_size={"width": 1280, "height": 720},
        viewport={"width": 1280, "height": 720}
    )

    # Ensure required client-side config exists for each isolated context.
    context.add_cookies(
        [
            {"name": "lang", "value": E2E_LANG.lower(), "url": BASE_URL},
            {"name": "apiBase", "value": API_BASE, "url": BASE_URL},
        ]
    )
    context.add_init_script(
        f"""
        try {{
            localStorage.setItem('lang', '{E2E_LANG.lower()}');
            localStorage.setItem('apiBase', '{API_BASE}');
            localStorage.setItem('configured', 'true');
        }} catch (e) {{}}
        """
    )

    yield context
    context.close()

@pytest.fixture(scope="function")
def page(context):
    page = context.new_page()

    # Make page.goto resilient for Blazor Server prerendering:
    # wait until app.js is loaded so event handlers & JS helpers exist.
    _orig_goto = page.goto

    def _goto_and_wait(url: str, *args, **kwargs):
        # Use a less strict wait mode for Blazor Server to avoid ERR_ABORTED during fast redirects.
        if "wait_until" not in kwargs and "waitUntil" not in kwargs:
            kwargs["wait_until"] = "domcontentloaded"

        try:
            result = _orig_goto(url, *args, **kwargs)
        except Exception as ex:
            # Blazor can trigger a fast client-side redirect which aborts the initial navigation.
            msg = str(ex)
            if "net::ERR_ABORTED" not in msg and "Navigation to" not in msg:
                raise
            result = None

        if isinstance(url, str) and url.startswith(BASE_URL) and "/api/" not in url:
            # Optional: wait for bobcrm helpers (may not be available during prerender).
            try:
                page.wait_for_function(
                    "() => typeof window !== 'undefined' && window.bobcrm && typeof window.bobcrm.getCookie === 'function'",
                    timeout=15000,
                )
            except Exception:
                pass

            # Required: ensure the circuit is interactive (splash removed or stage ready).
            page.wait_for_function(
                "() => !document.querySelector('.app-splash') || document.querySelector('.app-stage.ready')",
                timeout=15000,
            )

        return result

    page.goto = _goto_and_wait  # type: ignore[assignment]

    yield page
    page.close()

@pytest.fixture(scope="function")
def auth_admin(page):
    """Fixture to log in as admin via API and inject tokens into localStorage."""
    if not api_helper.login_as_admin():
        pytest.fail("Failed to login via API in auth_admin fixture")

    page.goto(f"{BASE_URL}/login")
    page.evaluate(
        """
        (tokens) => {
            localStorage.setItem('accessToken', tokens.accessToken);
            if (tokens.refreshToken) localStorage.setItem('refreshToken', tokens.refreshToken);
            localStorage.setItem('lang', tokens.lang);
            localStorage.setItem('apiBase', tokens.apiBase);
            localStorage.setItem('configured', 'true');
        }
        """,
        {
            "accessToken": api_helper.token,
            "refreshToken": api_helper.refresh_token,
            "lang": E2E_LANG.lower(),
            "apiBase": API_BASE,
        },
    )

    page.goto(f"{BASE_URL}/")
    expect(page).to_have_url(f"{BASE_URL}/", timeout=8000)
    page.wait_for_timeout(1000)

    token = page.evaluate("localStorage.getItem('accessToken')")
    assert token is not None

    return page

@pytest.fixture
def standard_product(auth_admin):
    """
    预置一个标准 Product 实体，包含 String, Decimal, Bool 等典型字段。
    确保它已发布并生成默认模板。
    """
    global _STANDARD_PRODUCT_CACHE
    if _STANDARD_PRODUCT_CACHE is not None:
        return _STANDARD_PRODUCT_CACHE

    assert api_helper.login_as_admin()

    # 1) Finder: reuse existing Product if present (avoid destructive deletes due to FK constraints)
    existing = requests.get(
        f"{API_BASE}/api/entity-definitions",
        headers={"X-Lang": E2E_LANG.lower(), **api_helper.get_headers()},
        timeout=30,
    )
    assert existing.status_code == 200, existing.text
    entities = existing.json()["data"] if isinstance(existing.json().get("data"), list) else existing.json().get("data", [])

    entity_id = None
    for e in entities:
        if str(e.get("fullTypeName", "")).lower() == "bobcrm.base.custom.product":
            entity_id = e.get("id")
            break

    # 2) Definer: Create Entity 'Product' (Name, Price, IsActive) if missing
    payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": "Product",
        # 重要：后端当前 ResolveLabel 取 DisplayName.Values 的第一个非空值（不按语言），
        # 因此这里把 en 放在最前面以保证 E2E 断言稳定。
        "displayName": {"en": "Product", "zh": "产品", "ja": "製品"},
        "structureType": "Single",
        "fields": [
            {
                "propertyName": "Name",
                "displayName": {"en": "Name", "zh": "名称", "ja": "名称"},
                "dataType": "String",
                "length": 100,
                "isRequired": True,
                "sortOrder": 10,
            },
            {
                "propertyName": "Price",
                "displayName": {"en": "Price", "zh": "价格", "ja": "価格"},
                "dataType": "Decimal",
                "precision": 18,
                "scale": 2,
                "isRequired": False,
                "sortOrder": 20,
            },
            {
                "propertyName": "IsActive",
                "displayName": {"en": "IsActive", "zh": "启用", "ja": "有効"},
                "dataType": "Boolean",
                "isRequired": False,
                "sortOrder": 30,
            },
        ],
    }

    if entity_id is None:
        resp = api_helper.post("/api/entity-definitions", payload)
        assert resp.status_code in (200, 201), resp.text
        entity_id = resp.json()["data"]["id"]

    # 4) Compiler: ensure /api/products exists
    detail = requests.get(
        f"{API_BASE}/api/entity-definitions/{entity_id}",
        headers={"X-Lang": E2E_LANG.lower(), **api_helper.get_headers()},
        timeout=30,
    )
    assert detail.status_code == 200, detail.text
    dto = detail.json()["data"]

    # Required fields must exist for Batch2
    existing_fields = {str(f.get("propertyName", "")).lower() for f in dto.get("fields", [])}
    for required in ("name", "price", "isactive"):
        assert required in existing_fields, f"Product entity missing field: {required}"

    # Ensure physical table exists; dev restart may leave metadata but drop tables.
    def _has_id_column() -> bool:
        q = ("SELECT 1 FROM information_schema.columns "
             "WHERE table_schema='public' AND (table_name='Products' OR table_name='products') "
             "AND (column_name='Id' OR column_name='id') LIMIT 1;")
        return bool(db_helper.execute_scalar(q))

    if not db_helper.table_exists("Products") or not _has_id_column():
        # Hard cleanup: detach FunctionNodes -> delete templates/bindings -> delete entity -> recreate & publish.
        db_helper.execute_query("""
        UPDATE "FunctionNodes"
        SET "TemplateStateBindingId" = NULL
        WHERE "TemplateStateBindingId" IN (
            SELECT "Id" FROM "TemplateStateBindings" WHERE "EntityType" = 'product'
        );
        """.strip())
        db_helper.execute_query("""
        DELETE FROM "TemplateBindings" WHERE "EntityType" = 'product';
        DELETE FROM "TemplateStateBindings" WHERE "EntityType" = 'product';
        DELETE FROM "FormTemplates" WHERE "EntityType" = 'product';
        """.strip())
        db_helper.execute_query("""
        DELETE FROM "FieldMetadatas"
        WHERE "EntityDefinitionId" IN (SELECT "Id" FROM "EntityDefinitions" WHERE "EntityName" = 'Product');
        DELETE FROM "EntityDefinitions" WHERE "EntityName" = 'Product';
        """.strip())
        db_helper.execute_query('DROP TABLE IF EXISTS "Products" CASCADE')
        db_helper.execute_query("DROP TABLE IF EXISTS products CASCADE")

        resp = api_helper.post("/api/entity-definitions", payload)
        assert resp.status_code in (200, 201), resp.text
        entity_id = resp.json()["data"]["id"]

        pub = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
        assert pub.status_code == 200, pub.text

        # reload dto after recreate
        detail = requests.get(
            f"{API_BASE}/api/entity-definitions/{entity_id}",
            headers={"X-Lang": E2E_LANG.lower(), **api_helper.get_headers()},
            timeout=30,
        )
        assert detail.status_code == 200, detail.text
        dto = detail.json()["data"]

    compile_resp = requests.post(
        f"{API_BASE}/api/entity-definitions/{entity_id}/compile",
        headers=api_helper.get_headers(),
        timeout=180,
    )
    assert compile_resp.status_code == 200, compile_resp.text

    # Wait physical table (CREATE TABLE can take a moment)
    for _ in range(40):
        if db_helper.table_exists("Products"):
            break
        time.sleep(0.5)
    assert db_helper.table_exists("Products"), "Table not created: Products"

    # 5) Ensure default templates exist for this entity
    regen = requests.post(
        f"{API_BASE}/api/admin/templates/product/regenerate",
        headers={"X-Lang": E2E_LANG.lower(), **api_helper.get_headers()},
        timeout=60,
    )
    assert regen.status_code == 200, regen.text

    _STANDARD_PRODUCT_CACHE = {
        "entity_id": entity_id,
        "entity_route": dto.get("entityRoute", "product"),
        "full_type_name": dto.get("fullTypeName", "BobCrm.Base.Custom.Product"),
    }
    return _STANDARD_PRODUCT_CACHE

def take_screenshot(page, name):
    """生成特定截图的辅助函数。"""
    path = f"{SCREENSHOT_DIR}/{name}.png"
    try:
        page.screenshot(path=path)
    except Exception as e:
        print(f"截图失败: {e}")
    return path

def save_page_content(page, name):
    """保存页面 HTML 的辅助函数。"""
    path = f"{SCREENSHOT_DIR}/{name}.html"
    try:
        content = page.content()
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
    except Exception as e:
        print(f"保存页面内容失败: {e}")
    return path

# 失败时捕获截图的 Hook
@pytest.hookimpl(tryfirst=True, hookwrapper=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    rep = outcome.get_result()
    if rep.when == "call" and rep.failed:
        page = item.funcargs.get("page")
        if page:
            name = item.name
            take_screenshot(page, f"FAILURE_{name}")
            save_page_content(page, f"FAILURE_{name}")


def pytest_runtest_logreport(report):
    """
    Batch6: record per-test durations for regression matrix reporting.
    """
    if report.when != "call":
        return

    nodeid = getattr(report, "nodeid", "")
    duration = float(getattr(report, "duration", 0.0) or 0.0)

    # Categorize by tests/e2e/cases/<NN_xxx>/...
    category = "unknown"
    try:
        parts = nodeid.replace("\\", "/").split("tests/e2e/cases/")
        if len(parts) > 1:
            rest = parts[1]
            category = rest.split("/", 1)[0] if "/" in rest else rest
    except Exception:
        category = "unknown"

    _E2E_DURATIONS.append(
        {
            "nodeid": nodeid,
            "category": category,
            "outcome": getattr(report, "outcome", "unknown"),
            "duration_s": duration,
        }
    )


def pytest_terminal_summary(terminalreporter, exitstatus, config):
    """
    Batch6: print and persist a simple duration distribution summary.
    """
    if not _E2E_DURATIONS:
        return

    durations = sorted(d["duration_s"] for d in _E2E_DURATIONS)
    total = sum(durations)

    def _pct(p: float) -> float:
        if not durations:
            return 0.0
        idx = int(round((len(durations) - 1) * p))
        idx = max(0, min(idx, len(durations) - 1))
        return durations[idx]

    summary = {
        "count": len(durations),
        "total_s": total,
        "min_s": durations[0],
        "p50_s": _pct(0.50),
        "p90_s": _pct(0.90),
        "p95_s": _pct(0.95),
        "max_s": durations[-1],
    }

    terminalreporter.write_sep("-", "E2E durations (Batch6)")
    terminalreporter.write_line(json.dumps(summary, ensure_ascii=False))

    # Write detailed report to disk (not intended to be committed)
    ts = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    out_dir = os.path.join("tests", "e2e", "reports")
    os.makedirs(out_dir, exist_ok=True)
    # Keep a stable filename to avoid accumulating junk files across repeated local runs.
    out_path = os.path.join(out_dir, "batch6_durations_latest.json")
    payload = {
        "generated_at_utc": ts,
        "summary": summary,
        "items": _E2E_DURATIONS,
    }
    try:
        with open(out_path, "w", encoding="utf-8") as f:
            json.dump(payload, f, ensure_ascii=False, indent=2)
        terminalreporter.write_line(f"[E2E] Duration report written: {out_path}")
    except Exception as ex:
        terminalreporter.write_line(f"[E2E] Failed to write duration report: {ex}")
