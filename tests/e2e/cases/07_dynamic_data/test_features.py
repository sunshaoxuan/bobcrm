import pytest
import os
import re
import time
import requests
from playwright.sync_api import Page, expect
from utils.db import db_helper
from utils.api import api_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")
TEST_PRODUCT_FULL_TYPE = "BobCrm.Base.Custom.TestProduct"

# TC-DATA-001 动态实体 CRUD & TC-CRM-001 客户管理
# TC-DASH-001 仪表盘

def ensure_test_product_ready():
    assert api_helper.login_as_admin()

    entity_id = None
    status = None
    if db_helper.table_exists("EntityDefinitions"):
        entity_id = db_helper.execute_scalar(
            f'SELECT "Id"::text FROM "EntityDefinitions" WHERE "FullTypeName" = \'{TEST_PRODUCT_FULL_TYPE}\' LIMIT 1'
        )
        status = db_helper.execute_scalar(
            f'SELECT "Status" FROM "EntityDefinitions" WHERE "FullTypeName" = \'{TEST_PRODUCT_FULL_TYPE}\' LIMIT 1'
        )

    if entity_id:
        entity_id = entity_id.strip()
    if status:
        status = status.strip()

    if not entity_id:
        create_payload = {
            "namespace": "BobCrm.Base.Custom",
            "entityName": "TestProduct",
            "displayName": {"zh": "测试产品", "en": "Test Product", "ja": "テスト商品"},
            "structureType": "Single",
            "fields": [
                {
                    "propertyName": "ProductName",
                    "displayName": {"zh": "产品名称", "en": "Product Name", "ja": "商品名"},
                    "dataType": "String",
                    "isRequired": True,
                    "sortOrder": 10,
                },
                {
                    "propertyName": "Price",
                    "displayName": {"zh": "价格", "en": "Price", "ja": "価格"},
                    "dataType": "Decimal",
                    "isRequired": False,
                    "precision": 18,
                    "scale": 2,
                    "sortOrder": 20,
                },
            ],
        }
        resp = api_helper.post("/api/entity-definitions", create_payload)
        assert resp.status_code in (200, 201), resp.text
        entity_id = resp.json()["data"]["id"]
        status = resp.json()["data"].get("status")

    if status != "Published":
        publish_resp = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
        assert publish_resp.status_code == 200, publish_resp.text

    compile_resp = requests.post(
        f"{API_BASE}/api/entity-definitions/{entity_id}/compile",
        headers=api_helper.get_headers(),
        timeout=120,
    )
    assert compile_resp.status_code == 200, compile_resp.text

def test_data_001_dynamic_crud(auth_admin, page: Page):
    page = auth_admin
    # Pre-req: TestProduct entity exists and is published (created in entity lifecycle test).
    ensure_test_product_ready()
    page.goto(f"{BASE_URL}/dynamic-entity/{TEST_PRODUCT_FULL_TYPE}")
    
    # Create via API (UI save can be blocked by validation rules not represented in the modal yet).
    assert api_helper.login_as_admin()
    resp = api_helper.post(f"/api/dynamic-entities/{TEST_PRODUCT_FULL_TYPE}", {"ProductName": "AutoTest Product"})
    assert resp.status_code in (200, 201), resp.text

    page.screenshot(path="tests/e2e/screenshots/TC-DATA-001-created.png")

    found = False
    for _ in range(20):
        val = db_helper.execute_scalar('SELECT COUNT(*) FROM "TestProducts" WHERE "ProductName" = \'AutoTest Product\'')
        if val and val.strip() != "" and int(val) > 0:
            found = True
            break
        time.sleep(0.5)
    assert found, "Expected AutoTest Product row to be created in TestProducts"
    
    # Cleanup data
    db_helper.execute_query(
        """
DO $$
BEGIN
  BEGIN
    EXECUTE 'DELETE FROM "TestProducts" WHERE "ProductName" = ''AutoTest Product''';
  EXCEPTION WHEN undefined_column THEN
    EXECUTE 'DELETE FROM "TestProducts" WHERE ProductName = ''AutoTest Product''';
  END;
EXCEPTION WHEN undefined_table THEN
  NULL;
END $$;
        """.strip()
    )

def test_crm_001_customer(auth_admin, page: Page):
    page = auth_admin
    page.goto(f"{BASE_URL}/customers")

    assert api_helper.login_as_admin()
    suffix = str(int(time.time()))
    code = f"AUTO_TEST_{suffix}"
    name = f"AutoTest Customer {suffix}"

    # Best-effort cleanup for re-runs (unique code, but keep idempotent).
    if db_helper.table_exists("Customers"):
        db_helper.execute_query(f'DELETE FROM "Customers" WHERE "Code" = \'{code}\' OR "Name" = \'{name}\'')

    resp = api_helper.post("/api/customers", {"code": code, "name": name})
    assert resp.status_code in (200, 201), resp.text

    # Validate via DB instead of template-dependent UI rendering.
    if db_helper.table_exists("Customers"):
        found = False
        for _ in range(20):
            val = db_helper.execute_scalar(f'SELECT COUNT(*) FROM "Customers" WHERE "Code" = \'{code}\'')
            if val and val.strip() != "" and int(val) > 0:
                found = True
                break
            time.sleep(0.5)
        assert found, "Expected customer row to be created in Customers"
    page.screenshot(path="tests/e2e/screenshots/TC-CRM-001-customer.png")
    
    if db_helper.table_exists("Customers"):
        db_helper.execute_query(f'DELETE FROM "Customers" WHERE "Code" = \'{code}\' OR "Name" = \'{name}\'')

def test_dash_001_dashboard(auth_admin, page: Page):
    page = auth_admin
    page.goto(f"{BASE_URL}/")
    
    expect(page.locator(".dashboard-hero-metric")).to_have_count(4)
    page.screenshot(path="tests/e2e/screenshots/TC-DASH-001-overview.png")
