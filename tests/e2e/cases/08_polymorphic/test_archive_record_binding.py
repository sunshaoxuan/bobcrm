import os
import json
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


def _create_entity(payload: dict) -> str:
    headers = _admin_headers()
    created = requests.post(f"{API_BASE}/api/entity-definitions", json=payload, headers=headers, timeout=30)
    assert created.status_code in (200, 201), created.text
    entity_id = str(created.json()["data"]["id"])

    pub = requests.post(f"{API_BASE}/api/entity-definitions/{entity_id}/publish", json={}, headers=headers, timeout=60)
    assert pub.status_code == 200, pub.text
    comp = requests.post(f"{API_BASE}/api/entity-definitions/{entity_id}/compile", json={}, headers=headers, timeout=180)
    assert comp.status_code == 200, comp.text
    return entity_id


def _create_record(full_type: str, data: dict) -> int:
    assert api_helper.login_as_admin()
    resp = api_helper.post(f"/api/dynamic-entities/{full_type}", data)
    assert resp.status_code in (200, 201), resp.text
    created = resp.json()
    entity = (created.get("data") or {}).get("data") or {}
    rid = entity.get("Id") or entity.get("id")
    assert rid is not None, created
    return int(rid)


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
        json={"name": name, "entityType": entity_type, "usageType": 0, "description": "PLAN-25 archive binding"},
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
        json={"name": name, "entityType": entity_type, "layoutJson": json.dumps(layout, ensure_ascii=False), "description": "PLAN-25 archive binding"},
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
def cleanup_test_entities():
    # 依赖全局 teardown（Test_ 前缀）做最终清理，这里额外兜底清一次，避免本用例单独运行时残留
    db_helper.execute_query("""
    UPDATE "FunctionNodes"
    SET "TemplateStateBindingId" = NULL
    WHERE "TemplateStateBindingId" IN (
      SELECT "Id" FROM "TemplateStateBindings" WHERE "EntityType" ILIKE 'test\\_%'
    );
    """.strip())
    db_helper.execute_query("DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" ILIKE 'test\\_%';")
    db_helper.execute_query("DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" ILIKE 'test\\_%';")
    db_helper.execute_query("DELETE FROM \"FormTemplates\" WHERE \"EntityType\" ILIKE 'test\\_%';")
    yield
    db_helper.execute_query("""
    UPDATE "FunctionNodes"
    SET "TemplateStateBindingId" = NULL
    WHERE "TemplateStateBindingId" IN (
      SELECT "Id" FROM "TemplateStateBindings" WHERE "EntityType" ILIKE 'test\\_%'
    );
    """.strip())
    db_helper.execute_query("DELETE FROM \"TemplateStateBindings\" WHERE \"EntityType\" ILIKE 'test\\_%';")
    db_helper.execute_query("DELETE FROM \"TemplateBindings\" WHERE \"EntityType\" ILIKE 'test\\_%';")
    db_helper.execute_query("DELETE FROM \"FormTemplates\" WHERE \"EntityType\" ILIKE 'test\\_%';")


def test_archive_record_binding_switches_template(auth_admin: Page, cleanup_test_entities):
    """
    PLAN-25 / Theme1+Theme3:
    - 使用“档案记录 ID（LookupEntityName）”驱动 TemplateStateBinding 规则引擎
    - 运行时根据记录字段切换模板，并且 API 返回字段随模板剪裁变化（SEC-05 仍成立）
    """
    suffix = uuid.uuid4().hex[:8]
    tier_entity_name = f"Test_Tier_{suffix}"
    account_entity_name = f"Test_Account_{suffix}"

    # 1) Create Tier entity (archive/inventory)
    tier_payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": tier_entity_name,
        "displayName": {"en": "Test Tier", "zh": "测试档案", "ja": "テスト"},
        "structureType": "Single",
        "fields": [
            {"propertyName": "Name", "displayName": {"en": "Name", "zh": "名称", "ja": "名称"}, "dataType": "String", "length": 100, "isRequired": True, "sortOrder": 10},
        ],
    }
    _create_entity(tier_payload)

    # Resolve routes/types
    headers = _admin_headers()
    entities = requests.get(f"{API_BASE}/api/entity-definitions", headers=headers, timeout=30).json()["data"]
    tier_def = next(e for e in entities if str(e.get("entityName")) == tier_entity_name)
    tier_route = str(tier_def.get("entityRoute")).lower()
    tier_full = str(tier_def.get("fullTypeName"))

    vip_id = _create_record(tier_full, {"Name": "VIP"})
    _create_record(tier_full, {"Name": "REG"})

    # 2) Create Account entity with a "lookup-like" field (TierId) using LookupEntityName to drive RecordSelector semantics
    acct_payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": account_entity_name,
        "displayName": {"en": "Test Account", "zh": "测试客户", "ja": "テスト"},
        "structureType": "Single",
        "fields": [
            {"propertyName": "Name", "displayName": {"en": "Name", "zh": "名称", "ja": "名称"}, "dataType": "String", "length": 100, "isRequired": True, "sortOrder": 10},
            {"propertyName": "TierId", "displayName": {"en": "Tier", "zh": "档案", "ja": "ランク"}, "dataType": "Int32", "isRequired": False, "sortOrder": 20, "lookupEntityName": tier_entity_name, "lookupDisplayField": "Name"},
            {"propertyName": "Balance", "displayName": {"en": "Balance", "zh": "余额", "ja": "残高"}, "dataType": "Decimal", "precision": 18, "scale": 2, "isRequired": False, "sortOrder": 30},
        ],
    }
    _create_entity(acct_payload)

    entities = requests.get(f"{API_BASE}/api/entity-definitions", headers=headers, timeout=30).json()["data"]
    acct_def = next(e for e in entities if str(e.get("entityName")) == account_entity_name)
    acct_route = str(acct_def.get("entityRoute")).lower()
    acct_full = str(acct_def.get("fullTypeName"))

    acct_id = _create_record(acct_full, {"Name": "Acme", "TierId": vip_id, "Balance": 9999.99})

    # 3) Prepare templates: VIP includes Balance; default hides Balance
    base_tpl_id = _get_binding_template_id(acct_route)
    base_tpl = _get_template_detail(base_tpl_id)
    base_layout = json.loads(base_tpl.get("layoutJson") or "[]")

    # VIP template: include Name + Balance (explicitly ensure Balance widget exists)
    _, name_widget = _extract_widget(base_layout, "name")
    assert name_widget is not None, "Name widget not found in base template"
    _, balance_widget = _extract_widget(base_layout, "balance")
    assert balance_widget is not None, "Balance widget not found in base template"

    vip_tpl_id = _copy_template(base_tpl_id, f"VIP({acct_route})", acct_route)
    _update_template_layout(vip_tpl_id, f"VIP({acct_route})", acct_route, [name_widget, balance_widget])

    default_tpl_id = _copy_template(base_tpl_id, f"DEFAULT({acct_route})", acct_route)
    _update_template_layout(default_tpl_id, f"DEFAULT({acct_route})", acct_route, [name_widget])

    # 4) Create state bindings via API (DetailView):
    # Rule: TierId == VIP_ID -> VIP template, otherwise default template
    sb_create = requests.post(
        f"{API_BASE}/api/templates/state-bindings",
        json={
            "entityType": acct_route,
            "viewState": "DetailView",
            "templateId": vip_tpl_id,
            "matchFieldName": "TierId",
            "matchFieldValue": str(vip_id),
            "priority": 10,
            "isDefault": False,
            "requiredPermission": None,
        },
        headers=headers,
        timeout=30,
    )
    assert sb_create.status_code in (200, 201), sb_create.text

    sb_default = requests.post(
        f"{API_BASE}/api/templates/state-bindings",
        json={
            "entityType": acct_route,
            "viewState": "DetailView",
            "templateId": default_tpl_id,
            "matchFieldName": None,
            "matchFieldValue": None,
            "priority": 0,
            "isDefault": True,
            "requiredPermission": None,
        },
        headers=headers,
        timeout=30,
    )
    assert sb_default.status_code in (200, 201), sb_default.text

    # 5) Runtime: open detail page WITHOUT mid/tid (rule engine decides), Balance must be visible for VIP record
    page = auth_admin

    target = f"{BASE_URL}/{acct_route}/{acct_id}?e2e_mode=true"
    page.goto(target)
    expect(page).to_have_url(target, timeout=15000)
    page.wait_for_selector(".runtime-shell, .runtime-state.error", timeout=120000)
    if page.locator(".runtime-state.error").count() > 0:
        raise AssertionError(page.locator(".runtime-state.error").inner_text())
    expect(page.locator(".runtime-shell")).to_be_visible(timeout=120000)

    # VIP template includes Balance
    expect(page.locator(".runtime-widget-shell[data-field='Balance']")).to_have_count(1)

    # 6) API must return Balance for VIP (still filtered by template)
    api_resp = requests.get(
        f"{API_BASE}/api/{acct_route}s/{acct_id}",
        headers={"Authorization": f"Bearer {api_helper.token}", "X-Lang": E2E_LANG.lower()},
        timeout=30,
    )
    assert api_resp.status_code == 200, api_resp.text
    keys = {str(f.get("key")) for f in (api_resp.json().get("fields") or []) if isinstance(f, dict)}
    assert "Balance" in keys

