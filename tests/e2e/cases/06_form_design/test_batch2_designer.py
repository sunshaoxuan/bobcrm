import os
import json
import uuid
import requests

from utils.api import api_helper

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


def _update_template(template_id: int, name: str, entity_type: str, layout_json: str, description: str | None = None) -> dict:
    assert api_helper.login_as_admin()
    payload = {
        "name": name,
        "entityType": entity_type,
        "layoutJson": layout_json,
        "description": description,
    }
    resp = requests.put(
        f"{API_BASE}/api/templates/{template_id}",
        json=payload,
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def _ensure_price_in_tabbox(layout: list[dict]) -> list[dict]:
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
    tab = {
        "id": uuid.uuid4().hex,
        "type": "tab",
        "label": "Pricing",
        "tabId": active_tab_id,
        "isDefault": True,
        "children": [price],
    }
    tabbox = {
        "id": uuid.uuid4().hex,
        "type": "tabbox",
        "label": "Tabs",
        "activeTabId": active_tab_id,
        "children": [tab],
    }
    next_layout.append(tabbox)
    return next_layout


def test_batch2_002_designer_payload_update_persists(standard_product):
    entity_type = standard_product["entity_route"]
    tpl = _ensure_editable_detail_binding_template(entity_type)
    layout_text = tpl.get("layoutJson") or "[]"
    layout = json.loads(layout_text)
    assert isinstance(layout, list), layout_text

    updated_layout = _ensure_price_in_tabbox(layout)
    updated_text = json.dumps(updated_layout, ensure_ascii=False)

    _update_template(
        template_id=int(tpl["id"]),
        name=str(tpl.get("name") or ""),
        entity_type=entity_type,
        layout_json=updated_text,
        description="Batch2: move Price into TabContainer",
    )

    reloaded = _get_template_detail(int(tpl["id"]))
    reloaded_layout = json.loads(reloaded.get("layoutJson") or "[]")
    assert any(isinstance(w, dict) and str(w.get("type", "")).lower() == "tabbox" for w in reloaded_layout), reloaded_layout
