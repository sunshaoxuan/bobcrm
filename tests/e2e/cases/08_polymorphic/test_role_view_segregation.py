import os
import json
import time
import uuid
import requests
import pytest
from playwright.sync_api import Page, expect

from utils.api import api_helper
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")
E2E_LANG = os.getenv("E2E_LANG", "en").strip() or "en"


def _admin_headers() -> dict:
    assert api_helper.login_as_admin()
    return {"X-Lang": E2E_LANG.lower(), **api_helper.get_headers()}


def _ensure_function(code: str, name: str, route: str | None = None) -> str:
    """
    Ensure a FunctionNode exists, return its id (guid string).
    """
    headers = _admin_headers()
    resp = requests.get(f"{API_BASE}/api/access/functions/manage", headers=headers, timeout=30)
    assert resp.status_code == 200, resp.text
    tree = resp.json()["data"]

    def _walk(nodes: list[dict]) -> dict | None:
        for n in nodes:
            if str(n.get("code")) == code:
                return n
            child = _walk(n.get("children") or [])
            if child is not None:
                return child
        return None

    existing = _walk(tree)
    if existing is not None:
        return str(existing["id"])

    # create under APP.ROOT
    root = _walk(tree)
    # find APP.ROOT
    def _find_root(nodes: list[dict]) -> dict | None:
        for n in nodes:
            if str(n.get("code")) == "APP.ROOT":
                return n
            child = _find_root(n.get("children") or [])
            if child is not None:
                return child
        return None

    app_root = _find_root(tree)
    assert app_root is not None, "APP.ROOT not found"

    create = requests.post(
        f"{API_BASE}/api/access/functions",
        json={
            "parentId": app_root["id"],
            "code": code,
            "name": name,
            "displayName": None,
            "route": route,
            "icon": "appstore",
            "isMenu": True,
            "sortOrder": 999,
            "templateStateBindingId": None,
            "templateId": None,
        },
        headers=headers,
        timeout=30,
    )
    assert create.status_code == 200, create.text
    return str(create.json()["data"]["id"])


def _ensure_role(code: str, name: str, function_ids: list[str]) -> str:
    """
    Ensure a RoleProfile exists, return roleId (guid string).
    """
    headers = _admin_headers()
    roles = requests.get(f"{API_BASE}/api/access/roles", headers=headers, timeout=30)
    assert roles.status_code == 200, roles.text
    for r in roles.json()["data"]:
        if str(r.get("code")) == code:
            return str(r["id"])

    create = requests.post(
        f"{API_BASE}/api/access/roles",
        json={
            "organizationId": None,
            "code": code,
            "name": name,
            "description": "E2E polymorphic role",
            "isEnabled": True,
            "functionIds": function_ids,
            "dataScopes": [],
        },
        headers=headers,
        timeout=30,
    )
    assert create.status_code == 200, create.text
    return str(create.json()["data"]["id"])


def _ensure_user(username: str, email: str, password: str) -> str:
    headers = _admin_headers()
    users = requests.get(f"{API_BASE}/api/users", headers=headers, timeout=30)
    assert users.status_code == 200, users.text
    for u in users.json()["data"]:
        if str(u.get("userName")) == username:
            return str(u["id"])

    create = requests.post(
        f"{API_BASE}/api/users",
        json={
            "userName": username,
            "email": email,
            "password": password,
            "emailConfirmed": True,
            "roles": [],
        },
        headers=headers,
        timeout=30,
    )
    assert create.status_code == 200, create.text
    return str(create.json()["data"]["id"])


def _assign_role(user_id: str, role_id: str) -> None:
    headers = _admin_headers()
    resp = requests.post(
        f"{API_BASE}/api/access/assignments",
        json={
            "userId": user_id,
            "roleId": role_id,
            "organizationId": None,
            "validFrom": None,
            "validTo": None,
        },
        headers=headers,
        timeout=30,
    )
    if resp.status_code == 200:
        return
    # Idempotency: assignment may already exist from previous runs
    if resp.status_code == 400:
        try:
            payload = resp.json()
            raws = payload.get("details", {}).get("rawMessage", [])
            raw = raws[0] if isinstance(raws, list) and len(raws) > 0 else payload.get("message", "")
            if "assignment already exists" in str(raw).lower():
                return
        except Exception:
            pass
    assert False, resp.text


def _ensure_account_entity() -> dict:
    headers = _admin_headers()
    entities = requests.get(f"{API_BASE}/api/entity-definitions", headers=headers, timeout=30)
    assert entities.status_code == 200, entities.text
    items = entities.json()["data"] if isinstance(entities.json().get("data"), list) else entities.json().get("data", [])
    for e in items:
        if str(e.get("fullTypeName", "")).lower() == "bobcrm.base.custom.account":
            entity_id = e.get("id")
            return {"entity_id": entity_id, "entity_route": e.get("entityRoute", "account"), "full_type_name": e.get("fullTypeName")}

    payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": "Account",
        "displayName": {"en": "Account", "zh": "账户", "ja": "アカウント"},
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
                "propertyName": "Balance",
                "displayName": {"en": "Balance", "zh": "余额", "ja": "残高"},
                "dataType": "Decimal",
                "precision": 18,
                "scale": 2,
                "isRequired": False,
                "sortOrder": 20,
            },
        ],
    }

    created = requests.post(f"{API_BASE}/api/entity-definitions", json=payload, headers=headers, timeout=30)
    assert created.status_code in (200, 201), created.text
    entity_id = created.json()["data"]["id"]

    pub = requests.post(f"{API_BASE}/api/entity-definitions/{entity_id}/publish", json={}, headers=headers, timeout=60)
    assert pub.status_code == 200, pub.text

    comp = requests.post(f"{API_BASE}/api/entity-definitions/{entity_id}/compile", json={}, headers=headers, timeout=180)
    assert comp.status_code == 200, comp.text

    # Ensure system templates exist for the entity
    regen = requests.post(f"{API_BASE}/api/admin/templates/account/regenerate", headers=headers, timeout=60)
    assert regen.status_code == 200, regen.text

    detail = requests.get(f"{API_BASE}/api/entity-definitions/{entity_id}", headers=headers, timeout=30)
    assert detail.status_code == 200, detail.text
    dto = detail.json()["data"]

    return {"entity_id": entity_id, "entity_route": dto.get("entityRoute", "account"), "full_type_name": dto.get("fullTypeName", "BobCrm.Base.Custom.Account")}


def _create_account_instance(full_type_name: str) -> int:
    assert api_helper.login_as_admin()
    create = api_helper.post(f"/api/dynamic-entities/{full_type_name}", {"Name": "Acme", "Balance": 9999.99})
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

    qry = api_helper.post(
        f"/api/dynamic-entities/{full_type_name}/query",
        {"take": 1, "skip": 0, "orderBy": "Id", "orderByDescending": True, "filters": []},
    )
    assert qry.status_code == 200, qry.text
    row = qry.json()["data"]["data"][0]
    entity_id = row.get("Id", None) or row.get("id", None)
    assert entity_id is not None, row
    return int(entity_id)


def _get_binding_template_id(entity_type: str) -> int:
    headers = _admin_headers()
    binding = requests.get(
        f"{API_BASE}/api/templates/bindings/{entity_type}",
        params={"usageType": "Detail"},
        headers=headers,
        timeout=30,
    )
    assert binding.status_code == 200, binding.text
    return int(binding.json()["data"]["templateId"])


def _copy_template(template_id: int, name: str, entity_type: str) -> int:
    headers = _admin_headers()
    resp = requests.post(
        f"{API_BASE}/api/templates/{template_id}/copy",
        json={"name": name, "entityType": entity_type, "usageType": 0, "description": "Batch9 polymorphic E2E"},
        headers=headers,
        timeout=30,
    )
    assert resp.status_code in (200, 201), resp.text
    return int(resp.json()["data"]["id"])


def _get_template_detail(template_id: int) -> dict:
    headers = _admin_headers()
    resp = requests.get(f"{API_BASE}/api/templates/{template_id}", headers=headers, timeout=30)
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def _update_template_layout(template_id: int, name: str, entity_type: str, layout: list[dict]) -> None:
    headers = _admin_headers()
    resp = requests.put(
        f"{API_BASE}/api/templates/{template_id}",
        json={"name": name, "entityType": entity_type, "layoutJson": json.dumps(layout, ensure_ascii=False), "description": "Batch9 polymorphic E2E"},
        headers=headers,
        timeout=30,
    )
    assert resp.status_code == 200, resp.text


def _extract_widget(layout: list[dict], data_field_lower: str) -> tuple[list[dict], dict | None]:
    popped = None
    next_nodes: list[dict] = []
    for w in layout:
        if popped is None and isinstance(w, dict) and str(w.get("dataField", "")).lower() == data_field_lower:
            popped = w
            continue
        if popped is None and isinstance(w, dict) and isinstance(w.get("children"), list):
            children, child_popped = _extract_widget(w["children"], data_field_lower)
            if child_popped is not None:
                updated = dict(w)
                updated["children"] = children
                next_nodes.append(updated)
                popped = child_popped
                continue
        next_nodes.append(w)
    return next_nodes, popped


@pytest.fixture
def cleanup_polymorphic_account():
    """
    清理本用例产生的 Account 实体/模板/绑定/菜单节点（避免测试垃圾累积）。
    """
    # Pre-clean
    db_helper.execute_query("""
    UPDATE "FunctionNodes"
    SET "TemplateStateBindingId" = NULL
    WHERE "TemplateStateBindingId" IN (
      SELECT "Id" FROM "TemplateStateBindings" WHERE "EntityType" = 'account' OR "EntityType" = 'Account'
    );
    """.strip())
    db_helper.execute_query("DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"FormTemplates\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"FieldMetadatas\" WHERE \"EntityDefinitionId\" IN (SELECT \"Id\" FROM \"EntityDefinitions\" WHERE \"EntityName\" = 'Account');")
    db_helper.execute_query("DELETE FROM \"EntityDefinitions\" WHERE \"EntityName\" = 'Account';")
    db_helper.execute_query('DROP TABLE IF EXISTS "Accounts" CASCADE')
    db_helper.execute_query("DROP TABLE IF EXISTS accounts CASCADE")
    yield
    # Post-clean
    db_helper.execute_query("""
    UPDATE "FunctionNodes"
    SET "TemplateStateBindingId" = NULL
    WHERE "TemplateStateBindingId" IN (
      SELECT "Id" FROM "TemplateStateBindings" WHERE "EntityType" = 'account' OR "EntityType" = 'Account'
    );
    """.strip())
    db_helper.execute_query("DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"FormTemplates\" WHERE \"EntityType\" = 'account' OR \"EntityType\" = 'Account';")
    db_helper.execute_query("DELETE FROM \"FieldMetadatas\" WHERE \"EntityDefinitionId\" IN (SELECT \"Id\" FROM \"EntityDefinitions\" WHERE \"EntityName\" = 'Account');")
    db_helper.execute_query("DELETE FROM \"EntityDefinitions\" WHERE \"EntityName\" = 'Account';")
    db_helper.execute_query('DROP TABLE IF EXISTS "Accounts" CASCADE')
    db_helper.execute_query("DROP TABLE IF EXISTS accounts CASCADE")


def test_role_view_segregation(page: Page, cleanup_polymorphic_account):
    """
    FIX-09 / TC-CORE-CLOSURE
    - SalesUser 在 M_SALES 视图下只能看到 Name，且 API 不返回 Balance
    - 手动篡改 tid=AdminTemplateId 必须 403
    """
    # 1) Prepare permissions: functions + roles + users
    fn_admin_id = _ensure_function("M_ADMIN", "M_ADMIN")
    fn_sales_id = _ensure_function("M_SALES", "M_SALES")

    role_admin_id = _ensure_role("TEST.ADMIN", "E2E Admin Role", [fn_admin_id])
    role_sales_id = _ensure_role("TEST.SALES", "E2E Sales Role", [fn_sales_id])

    admin_user_id = _ensure_user("AdminUser", "adminuser@example.com", "Admin@12345")
    sales_user_id = _ensure_user("SalesUser", "salesuser@example.com", "Admin@12345")

    _assign_role(admin_user_id, role_admin_id)
    _assign_role(sales_user_id, role_sales_id)

    # 2) Prepare entity + instance
    ent = _ensure_account_entity()
    entity_type = str(ent["entity_route"]).lower()
    full_type = str(ent["full_type_name"])
    record_id = _create_account_instance(full_type)

    # 3) Prepare templates
    base_tpl_id = _get_binding_template_id(entity_type)
    base_tpl = _get_template_detail(base_tpl_id)
    base_layout = json.loads(base_tpl.get("layoutJson") or "[]")

    # Admin template: include Balance + Name (use base layout as-is)
    admin_tpl_id = _copy_template(base_tpl_id, "Admin(Account)", entity_type)
    _update_template_layout(admin_tpl_id, "Admin(Account)", entity_type, base_layout)

    # Sales template: only Name
    sales_tpl_id = _copy_template(base_tpl_id, "Sales(Account)", entity_type)
    next_layout, name_widget = _extract_widget(base_layout, "name")
    assert name_widget is not None, "Name widget not found in base template"
    _update_template_layout(sales_tpl_id, "Sales(Account)", entity_type, [name_widget])

    # 4) Bind templates to menu codes via TemplateStateBindings (ViewState = DetailView)
    db_helper.execute_query(
        f"""
        INSERT INTO "TemplateStateBindings" ("EntityType","ViewState","TemplateId","MatchFieldName","MatchFieldValue","Priority","IsDefault","RequiredPermission","CreatedAt")
        VALUES ('{entity_type}','DetailView',{admin_tpl_id},NULL,NULL,10,false,'M_ADMIN',NOW());
        """.strip()
    )
    db_helper.execute_query(
        f"""
        INSERT INTO "TemplateStateBindings" ("EntityType","ViewState","TemplateId","MatchFieldName","MatchFieldValue","Priority","IsDefault","RequiredPermission","CreatedAt")
        VALUES ('{entity_type}','DetailView',{sales_tpl_id},NULL,NULL,10,false,'M_SALES',NOW());
        """.strip()
    )

    # 5) SalesUser: UI should not show Balance; API must not return Balance
    # Login as SalesUser and inject token
    login_sales = requests.post(
        f"{API_BASE}/api/auth/login",
        json={"username": "SalesUser", "password": "Admin@12345"},
        headers={"X-Lang": E2E_LANG.lower()},
        timeout=30,
    )
    assert login_sales.status_code == 200, login_sales.text
    sales_token = login_sales.json()["data"]["accessToken"]

    page.goto(f"{BASE_URL}/login")
    page.evaluate(
        """
        (payload) => {
            localStorage.setItem('accessToken', payload.token);
            localStorage.setItem('lang', payload.lang);
            localStorage.setItem('apiBase', payload.apiBase);
            localStorage.setItem('configured', 'true');
        }
        """,
        {"token": sales_token, "lang": E2E_LANG.lower(), "apiBase": API_BASE},
    )

    target = f"{BASE_URL}/{entity_type}/{record_id}?vs=M_SALES&e2e_mode=true"
    page.goto(target)
    expect(page).to_have_url(target, timeout=15000)
    page.wait_for_selector(".runtime-shell, .runtime-state.error", timeout=30000)
    expect(page.locator(".runtime-shell")).to_be_visible(timeout=30000)

    # Balance widget should be absent
    expect(page.locator(".runtime-widget-shell[data-field='Balance']")).to_have_count(0)

    # API must not return Balance in raw response
    api_resp = requests.get(
        f"{API_BASE}/api/{entity_type}s/{record_id}",
        params={"vs": "M_SALES"},
        headers={"Authorization": f"Bearer {sales_token}", "X-Lang": E2E_LANG.lower()},
        timeout=30,
    )
    assert api_resp.status_code == 200, api_resp.text
    payload = api_resp.json()
    fields = payload.get("fields") or []
    keys = {str(f.get("key")) for f in fields if isinstance(f, dict)}
    assert "Balance" not in keys, f"Balance leaked in API response: {keys}"

    # 6) Tamper: tid=AdminTemplateId must be forbidden
    api_forbidden = requests.get(
        f"{API_BASE}/api/{entity_type}s/{record_id}",
        params={"tid": admin_tpl_id},
        headers={"Authorization": f"Bearer {sales_token}", "X-Lang": E2E_LANG.lower()},
        timeout=30,
    )
    assert api_forbidden.status_code == 403, api_forbidden.text

    forbidden_url = f"{BASE_URL}/{entity_type}/{record_id}?tid={admin_tpl_id}&e2e_mode=true"
    page.goto(forbidden_url)
    # Error page should be visible (no silent fallback)
    expect(page.locator(".runtime-state.error")).to_be_visible(timeout=15000)
