import os
import time
import requests
import pytest

from playwright.sync_api import Page
from utils.api import api_helper
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")


def _compile_entity(entity_id: str):
    resp = requests.post(
        f"{API_BASE}/api/entity-definitions/{entity_id}/compile",
        headers=api_helper.get_headers(),
        timeout=120,
    )
    assert resp.status_code == 200, resp.text


def _wait_table(table_name: str, timeout_s: float = 20.0):
    start = time.time()
    while time.time() - start < timeout_s:
        if db_helper.table_exists(table_name):
            return
        time.sleep(0.5)
    raise AssertionError(f"Table not created: {table_name}")


def _create_entity(entity_name: str, fields: list[dict], display_name: dict | None = None) -> dict:
    assert api_helper.login_as_admin()
    payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": entity_name,
        "displayName": display_name
        or {"zh": entity_name, "en": entity_name, "ja": entity_name},
        "structureType": "Single",
        "fields": fields,
    }
    resp = api_helper.post("/api/entity-definitions", payload)
    assert resp.status_code in (200, 201), resp.text
    return resp.json()["data"]

def _get_field_id(entity_id: str, property_name: str, timeout_s: float = 10.0) -> str:
    start = time.time()
    while time.time() - start < timeout_s:
        val = db_helper.execute_scalar(
            f"SELECT \"Id\"::text FROM \"FieldMetadatas\" WHERE \"EntityDefinitionId\" = '{entity_id}' AND \"PropertyName\" = '{property_name}' LIMIT 1"
        )
        if val:
            return val.strip()
        time.sleep(0.3)
    raise AssertionError(f"Missing field id for {property_name} in entity {entity_id}")


def _publish_new(entity_id: str):
    pub = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
    assert pub.status_code == 200, pub.text


def _publish_changes(entity_id: str):
    pub = api_helper.post(f"/api/entity-definitions/{entity_id}/publish-changes", {})
    assert pub.status_code == 200, pub.text

def _put_with_retry(endpoint: str, payload: dict, max_attempts: int = 5) -> requests.Response:
    last = None
    for attempt in range(1, max_attempts + 1):
        resp = api_helper.put(endpoint, payload)
        last = resp
        if resp.status_code != 409:
            return resp
        # 并发冲突：等待发布/后台任务落库完成后重试
        time.sleep(0.4 * attempt)
    return last  # type: ignore[return-value]

def _wait_entity_status(entity_id: str, expected: str, timeout_s: float = 20.0):
    start = time.time()
    while time.time() - start < timeout_s:
        status = db_helper.execute_scalar(f"SELECT \"Status\" FROM \"EntityDefinitions\" WHERE \"Id\" = '{entity_id}'")
        if status == expected:
            return
        time.sleep(0.5)
    raise AssertionError(f"Expected entity {entity_id} status={expected}, got {status}")


def test_batch1_003_schema_evolution(auth_admin, page: Page, clean_platform):
    page = auth_admin
    entity_name = "EvoEntity"
    full_type = "BobCrm.Base.Custom.EvoEntity"
    table_name = "EvoEntitys"

    # V1: ColA length 50
    v1 = _create_entity(
        entity_name,
        [
            {"propertyName": "ColA", "displayName": {"zh": "列A", "en": "ColA", "ja": "列A"}, "dataType": "String", "length": 50, "isRequired": False, "sortOrder": 10},
        ],
        {"zh": "演进实体", "en": "Evolution Entity", "ja": "進化エンティティ"},
    )
    entity_id = v1["id"]
    col_a_id = _get_field_id(entity_id, "ColA")

    _publish_new(entity_id)
    _compile_entity(entity_id)
    _wait_table(table_name)

    # Insert one row
    assert api_helper.login_as_admin()
    ins = api_helper.post(f"/api/dynamic-entities/{full_type}", {"ColA": "OldVal"})
    assert ins.status_code in (200, 201), ins.text

    # V2: lengthen ColA -> 100, add ColB default 'New'
    # 发布流程可能仍在落库，更新操作对 409 并发冲突做重试
    update_payload = {
        "fields": [
            {"id": col_a_id, "propertyName": "ColA", "length": 100},
            {"propertyName": "ColB", "displayName": {"zh": "列B", "en": "ColB", "ja": "列B"}, "dataType": "String", "length": 50, "defaultValue": "New", "isRequired": False, "sortOrder": 20},
        ]
    }
    upd = _put_with_retry(f"/api/entity-definitions/{entity_id}", update_payload)
    assert upd.status_code == 200, upd.text

    _publish_changes(entity_id)
    _compile_entity(entity_id)

    # Verify schema updated
    col_a_len = db_helper.execute_scalar(
        f"SELECT character_maximum_length FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='ColA'"
    )
    assert col_a_len == "100", col_a_len

    col_b_exists = db_helper.execute_scalar(
        f"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='ColB'"
    )
    assert col_b_exists == "1", col_b_exists

    # Verify old data remains and ColB default applied
    qry = api_helper.post(
        f"/api/dynamic-entities/{full_type}/query",
        {"take": 10, "skip": 0, "orderBy": "Id", "orderByDescending": False, "filters": []},
    )
    assert qry.status_code == 200, qry.text
    rows = qry.json()["data"]["data"]
    assert any(r.get("ColA") == "OldVal" and r.get("ColB") == "New" for r in rows), rows


def test_batch1_004_cascade_publishing(auth_admin, page: Page, clean_platform):
    page = auth_admin
    assert api_helper.login_as_admin()

    child = _create_entity(
        "ChildEnt",
        [{"propertyName": "Name", "displayName": {"zh": "名称", "en": "Name", "ja": "名称"}, "dataType": "String", "length": 50, "isRequired": False, "sortOrder": 10}],
        {"zh": "子实体", "en": "Child Entity", "ja": "子エンティティ"},
    )
    child_id = child["id"]

    parent = _create_entity(
        "ParentEnt",
        [
            {
                "propertyName": "ChildRef",
                "displayName": {"zh": "子引用", "en": "ChildRef", "ja": "子参照"},
                "dataType": "Int32",
                "isEntityRef": True,
                "referencedEntityId": child_id,
                "isRequired": False,
                "sortOrder": 10,
            }
        ],
        {"zh": "父实体", "en": "Parent Entity", "ja": "親エンティティ"},
    )
    parent_id = parent["id"]

    # Publish Parent -> expect Child auto Published too
    _publish_new(parent_id)

    # Verify both status -> Published
    _wait_entity_status(parent_id, "Published")
    _wait_entity_status(child_id, "Published")

    # Verify both tables created
    _wait_table("ChildEnts")
    _wait_table("ParentEnts")
