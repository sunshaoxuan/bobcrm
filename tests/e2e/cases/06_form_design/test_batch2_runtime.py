import os
import json
import uuid
import time
import requests
import pytest
from playwright.sync_api import Page, expect

from utils.api import api_helper
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")


def _get_templates(entity_type: str) -> list[dict]:
    assert api_helper.login_as_admin()
    resp = requests.get(
        f"{API_BASE}/api/templates",
        params={"entityType": entity_type},
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]["items"]


def _get_binding(entity_type: str, usage_type: str = "Detail") -> dict:
    assert api_helper.login_as_admin()
    resp = requests.get(
        f"{API_BASE}/api/templates/bindings/{entity_type}",
        params={"usageType": usage_type},
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def _get_template_detail(template_id: int) -> dict:
    assert api_helper.login_as_admin()
    resp = requests.get(
        f"{API_BASE}/api/templates/{template_id}",
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def _copy_template(template_id: int, name: str, entity_type: str, usage_type: int = 0) -> dict:
    assert api_helper.login_as_admin()
    resp = requests.post(
        f"{API_BASE}/api/templates/{template_id}/copy",
        json={
            "name": name,
            "entityType": entity_type,
            "usageType": usage_type,
            "description": "Batch2 E2E: copy template for binding override",
        },
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code in (200, 201), resp.text
    return resp.json()["data"]


def _upsert_binding(entity_type: str, usage_type: int, template_id: int, is_system: bool) -> dict:
    assert api_helper.login_as_admin()
    resp = requests.put(
        f"{API_BASE}/api/templates/bindings",
        json={
            "entityType": entity_type,
            "usageType": usage_type,
            "templateId": template_id,
            "isSystem": is_system,
            "requiredFunctionCode": None,
        },
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def _ensure_editable_detail_binding_template(entity_type: str) -> dict:
    binding = _get_binding(entity_type, "Detail")
    tpl = _get_template_detail(int(binding["templateId"]))

    # Prefer a non-system binding for E2E so TemplateRuntimeService picks it deterministically.
    if binding.get("isSystem") is True:
        copied = _copy_template(
            template_id=int(binding["templateId"]),
            name=f"{tpl.get('name') or 'Detail'} (E2E)",
            entity_type=entity_type,
            usage_type=0,
        )
        _upsert_binding(entity_type, 0, int(copied["id"]), False)
        tpl = _get_template_detail(int(copied["id"]))

    return tpl


def _update_template(template_id: int, name: str, entity_type: str, layout_json: str) -> None:
    assert api_helper.login_as_admin()
    payload = {
        "name": name,
        "entityType": entity_type,
        "layoutJson": layout_json,
        "description": "Batch2 runtime: ensure tabbox exists",
    }
    resp = requests.put(
        f"{API_BASE}/api/templates/{template_id}",
        json=payload,
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text


def _ensure_template_has_tabbox(entity_type: str) -> None:
    tpl = _ensure_editable_detail_binding_template(entity_type)
    layout_text = tpl.get("layoutJson") or "[]"
    try:
        layout = json.loads(layout_text)
    except Exception:
        layout = []

    if any(isinstance(w, dict) and str(w.get("type", "")).lower() == "tabbox" for w in layout):
        return

    def _pop_widget(nodes: list[dict], data_field_lower: str) -> tuple[list[dict], dict | None]:
        popped = None
        next_nodes: list[dict] = []
        for w in nodes:
            if popped is None and isinstance(w, dict) and str(w.get("dataField", "")).lower() == data_field_lower:
                popped = w
                continue

            if popped is None and isinstance(w, dict) and isinstance(w.get("children"), list):
                children, child_popped = _pop_widget(w["children"], data_field_lower)
                if child_popped is not None:
                    updated = dict(w)
                    updated["children"] = children
                    next_nodes.append(updated)
                    popped = child_popped
                    continue

            next_nodes.append(w)

        return next_nodes, popped

    next_layout, price = _pop_widget(layout, "price")
    assert price is not None, layout

    active_tab_id = uuid.uuid4().hex
    tabbox = {
        "id": uuid.uuid4().hex,
        "type": "tabbox",
        "label": "Tabs",
        "activeTabId": active_tab_id,
        "children": [
            {
                "id": uuid.uuid4().hex,
                "type": "tab",
                "label": "Pricing",
                "tabId": active_tab_id,
                "isDefault": True,
                "children": [price],
            }
        ],
    }
    next_layout.append(tabbox)

    _update_template(
        template_id=int(tpl["id"]),
        name=str(tpl.get("name") or ""),
        entity_type=entity_type,
        layout_json=json.dumps(next_layout, ensure_ascii=False),
    )

    # Ensure runtime sees the updated template binding (reduce flakiness from stale selection).
    assert api_helper.login_as_admin()
    runtime = requests.post(
        f"{API_BASE}/api/templates/runtime/{entity_type}",
        json={"usageType": 0},
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert runtime.status_code == 200, runtime.text
    runtime_tpl = runtime.json()["data"]["template"]
    runtime_layout = json.loads(runtime_tpl.get("layoutJson") or "[]")
    assert any(isinstance(w, dict) and str(w.get("type", "")).lower() == "tabbox" for w in runtime_layout), runtime_layout


def _ensure_product_instance(full_type_name: str) -> int:
    assert api_helper.login_as_admin()

    # Create via dynamic-entities API (stable, no generated DTO dependencies)
    create = api_helper.post(
        f"/api/dynamic-entities/{full_type_name}",
        {"Name": "Test Product", "Price": 12.34, "IsActive": True},
    )
    assert create.status_code in (200, 201), create.text

    try:
        created = create.json()
        entity = created.get("data", {}).get("data", None)
        if isinstance(entity, dict):
            entity_id = entity.get("Id", None) or entity.get("id", None)
            if entity_id is not None:
                return int(entity_id)
    except Exception:
        pass

    # Query back the latest Id
    qry = api_helper.post(
        f"/api/dynamic-entities/{full_type_name}/query",
        {"take": 1, "skip": 0, "orderBy": "Id", "orderByDescending": True, "filters": []},
    )
    assert qry.status_code == 200, qry.text
    row = qry.json()["data"]["data"][0]
    entity_id = row.get("Id", None) or row.get("id", None)
    assert entity_id is not None, row
    return int(entity_id)


def _wait_for_runtime_loaded(page: Page) -> None:
    # PageLoader is interactive server; wait for LoadDataAsync completion (loading -> runtime-shell).
    expect(page.locator(".runtime-shell")).to_be_visible(timeout=30000)
    expect(page.locator(".runtime-state.loading")).to_have_count(0, timeout=30000)


def test_batch2_003_runtime_renders_tabbox_and_number_input(auth_admin, page: Page, standard_product):
    page = auth_admin
    entity_type = standard_product["entity_route"]
    full_type = standard_product["full_type_name"]
    # Capture console logs
    page.on("console", lambda msg: print(f"BROWSER CONSOLE: {msg.text}"))


    # Ensure no stale polymorphic bindings override our updates
    db_helper.execute_query(f"DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" = '{entity_type}'")
    # Ensure no stale user bindings exist (force fresh creation)
    db_helper.execute_query(f"DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" = '{entity_type}' AND \"IsSystem\" = false")

    _ensure_template_has_tabbox(entity_type)
    entity_id = _ensure_product_instance(full_type)

    # Verify API route used by PageLoader exists before UI (reduce flaky UI failures)
    assert api_helper.login_as_admin()
    check = requests.get(
        f"{API_BASE}/api/{entity_type}s/{entity_id}",
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert check.status_code == 200, check.text

    try:
        page.goto(f"{BASE_URL}/product/{entity_id}")
        page.wait_for_selector(".runtime-shell")

        # 验证 TabBox 渲染
        # Retry with reload if TabBox is missing (cache propagation)
        try:
            expect(page.locator(".runtime-tab-container")).to_be_visible(timeout=5000)
        except AssertionError:
            print("TabBox not found, reloading...")
            page.reload()
            page.wait_for_selector(".runtime-shell")
            expect(page.locator(".runtime-tab-container")).to_be_visible()

        # 验证 Number 输入框 (Id) 渲染 (View Mode)
        # 查找 label=Id 的 .runtime-widget-shell
        id_widget = page.locator(".runtime-widget-shell[data-field='Id']")
        expect(id_widget).to_be_visible()

        # View Mode renders value in span or div, just check containment
        expect(id_widget).to_contain_text(str(entity_id))
    except:
        import os
        debug_path = os.path.join(os.getcwd(), "debug_page_content.html")
        print("WRITING DEBUG HTML TO debug_page_content.html")
        with open("c:/workspace/bobcrm/debug_page_content.html", "w", encoding="utf-8") as f:
            f.write(page.content())
        try:
            page.screenshot(path="c:/workspace/bobcrm/debug_page_screenshot.png")
            print("WRITING DEBUG SCREENSHOT TO debug_page_screenshot.png")
        except:
            print("FAILED TO WRITE SCREENSHOT")
        raise

    # Enter edit mode to assert input controls exist
    expect(page.locator(".test-runtime-mode-edit")).to_be_visible(timeout=15000)
    page.locator(".test-runtime-mode-edit").click()
    
    # Confirm edit mode by waiting for Save button
    expect(page.locator(".test-runtime-save")).to_be_visible(timeout=10000)

    # Verify Price field becomes an input number (Id might remain text if read-only)
    price_widget = page.locator(".runtime-widget-shell[data-field='Price']")
    # AntDesign InputNumber always contains an input element
    expect(price_widget.locator("input")).to_be_visible(timeout=15000)


def test_batch2_004_validation_shows_required_error(auth_admin, page: Page, standard_product):
    page = auth_admin
    entity_type = standard_product["entity_route"]
    full_type = standard_product["full_type_name"]

    _ensure_template_has_tabbox(entity_type)
    entity_id = _ensure_product_instance(full_type)

    page.goto(f"{BASE_URL}/{entity_type}/{entity_id}")
    _wait_for_runtime_loaded(page)

    # Enter edit mode
    expect(page.locator(".test-runtime-mode-edit")).to_be_visible(timeout=15000)
    page.locator(".test-runtime-mode-edit").click()

    # Clear required field (Name) and attempt save
    name_input = page.locator(".runtime-widget-shell[data-field='Name'] input").first
    expect(name_input).to_be_visible(timeout=15000)
    name_input.fill("")
    name_input.press("Tab")

    page.locator(".test-runtime-save").click()

    # Local validators (preferred) OR i18n key fallback
    expect(page.locator("text=/Required\\.|MSG_VALIDATION_REQUIRED|必須/")).to_be_visible(timeout=15000)
