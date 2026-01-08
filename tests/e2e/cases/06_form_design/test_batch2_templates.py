import os
import json
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


def _get_template_detail(template_id: int) -> dict:
    assert api_helper.login_as_admin()
    resp = requests.get(
        f"{API_BASE}/api/templates/{template_id}",
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]


def test_batch2_001_default_templates_generated(standard_product):
    entity_type = standard_product["entity_route"]

    items = _get_templates(entity_type)
    system_defaults = [t for t in items if t.get("isSystemDefault") is True]

    assert len(system_defaults) >= 2, system_defaults

    # 至少应包含 List + Detail（系统默认会生成 List/DetailView/DetailEdit）
    usage_tokens = {str(t.get("usageType")) for t in system_defaults}
    assert any("List" in u or u == "0" for u in usage_tokens), usage_tokens
    assert any("Detail" in u or "Edit" in u or u in ("1", "2", "3") for u in usage_tokens), usage_tokens

    # 模板内容 JSON 应覆盖业务字段（Name/Price/IsActive）
    required = {"name", "price", "isactive"}
    checked = 0
    for t in system_defaults:
        detail = _get_template_detail(int(t["id"]))
        layout = detail.get("layoutJson") or ""
        assert isinstance(layout, str) and layout.strip(), detail

        lower = layout.lower()
        # List 模板的 columnsJson 内部使用 lower-case，Detail 模板使用原字段名；统一用 lower 匹配
        assert all(k in lower for k in required), (required, lower[:200])
        checked += 1
        if checked >= 2:
            break
