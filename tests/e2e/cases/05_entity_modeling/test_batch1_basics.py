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


def _create_publish_compile(entity_name: str, fields: list[dict], display_name: dict | None = None) -> str:
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
    entity_id = resp.json()["data"]["id"]

    pub = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
    assert pub.status_code == 200, pub.text

    _compile_entity(entity_id)
    return entity_id


def _wait_table(table_name: str, timeout_s: float = 20.0):
    start = time.time()
    while time.time() - start < timeout_s:
        if db_helper.table_exists(table_name):
            return
        time.sleep(0.5)
    raise AssertionError(f"Table not created: {table_name}")


def test_batch1_001_types_and_runtime(auth_admin, page: Page, clean_platform):
    page = auth_admin
    entity_name = "TypeTester"
    full_type = "BobCrm.Base.Custom.TypeTester"
    table_name = "TypeTesters"

    fields = [
        {"propertyName": "F_String", "displayName": {"zh": "字符串", "en": "String", "ja": "文字列"}, "dataType": "String", "length": 50, "isRequired": False, "sortOrder": 10},
        {"propertyName": "F_Int", "displayName": {"zh": "整数", "en": "Int", "ja": "整数"}, "dataType": "Int32", "isRequired": False, "sortOrder": 20},
        {"propertyName": "F_Dec", "displayName": {"zh": "小数", "en": "Decimal", "ja": "小数"}, "dataType": "Decimal", "precision": 18, "scale": 2, "isRequired": False, "sortOrder": 30},
        {"propertyName": "F_Bool", "displayName": {"zh": "布尔", "en": "Bool", "ja": "真偽"}, "dataType": "Boolean", "isRequired": False, "sortOrder": 40},
        {"propertyName": "F_Date", "displayName": {"zh": "日期", "en": "Date", "ja": "日付"}, "dataType": "Date", "isRequired": False, "sortOrder": 50},
        {"propertyName": "F_DateTime", "displayName": {"zh": "时间", "en": "DateTime", "ja": "日時"}, "dataType": "DateTime", "isRequired": False, "sortOrder": 60},
    ]

    _create_publish_compile(entity_name, fields, {"zh": "类型测试", "en": "Type Tester", "ja": "型テスト"})
    _wait_table(table_name)

    # 验证物理类型（information_schema）
    f_string_type = db_helper.execute_scalar(
        f"SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_String'"
    )
    f_string_len = db_helper.execute_scalar(
        f"SELECT character_maximum_length FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_String'"
    )
    assert f_string_type in ("character varying", "text"), f_string_type
    assert f_string_type == "text" or f_string_len == "50", (f_string_type, f_string_len)

    f_bool_type = db_helper.execute_scalar(
        f"SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_Bool'"
    )
    assert f_bool_type == "boolean", f_bool_type

    f_dec_type = db_helper.execute_scalar(
        f"SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_Dec'"
    )
    f_dec_precision = db_helper.execute_scalar(
        f"SELECT numeric_precision FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_Dec'"
    )
    f_dec_scale = db_helper.execute_scalar(
        f"SELECT numeric_scale FROM information_schema.columns WHERE table_schema='public' AND table_name='{table_name}' AND column_name='F_Dec'"
    )
    assert f_dec_type == "numeric", f_dec_type
    assert f_dec_precision == "18", f_dec_precision
    assert f_dec_scale == "2", f_dec_scale

    # Runtime：插入并查询（dynamic-entities）
    assert api_helper.login_as_admin()
    ins = api_helper.post(f"/api/dynamic-entities/{full_type}", {"F_String": "Test", "F_Int": 123, "F_Bool": True})
    assert ins.status_code in (200, 201), ins.text

    qry = api_helper.post(f"/api/dynamic-entities/{full_type}/query", {"take": 10, "skip": 0, "orderBy": "Id", "orderByDescending": True, "filters": []})
    assert qry.status_code == 200, qry.text
    data = qry.json()["data"]["data"]
    assert isinstance(data, list) and len(data) >= 1
    row = data[0]
    assert row.get("F_String") == "Test"
    assert isinstance(row.get("F_Int"), int)
    assert isinstance(row.get("F_Bool"), bool)


def test_batch1_002_constraints_validation(auth_admin, page: Page, clean_platform):
    page = auth_admin
    entity_name = "Constrainer"
    full_type = "BobCrm.Base.Custom.Constrainer"
    table_name = "Constrainers"

    fields = [
        {"propertyName": "ReqField", "displayName": {"zh": "必填字段", "en": "ReqField", "ja": "必須"}, "dataType": "String", "isRequired": True, "length": 50, "sortOrder": 10},
    ]

    _create_publish_compile(entity_name, fields, {"zh": "约束测试", "en": "Constrainer", "ja": "制約テスト"})
    _wait_table(table_name)

    assert api_helper.login_as_admin()

    # Action 1: 插入空对象，预期 400（校验失败，不允许 500）
    r1 = api_helper.post(f"/api/dynamic-entities/{full_type}", {})
    assert r1.status_code == 400, r1.text

    # Action 2: 超长字符串，预期 400（校验失败，不允许 500）
    r2 = api_helper.post(f"/api/dynamic-entities/{full_type}", {"ReqField": "A" * 5000})
    assert r2.status_code == 400, r2.text

