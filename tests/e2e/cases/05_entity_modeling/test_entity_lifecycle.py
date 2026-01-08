import pytest
import os
from playwright.sync_api import Page, expect
from utils.db import db_helper
from utils.api import api_helper
import requests

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")

# TC-ENT-001 to 004 实体建模流程

@pytest.fixture
def cleanup_entity():
    def cleanup():
        # Force cleanup chain (DefaultTableName = EntityName + "s" -> TestProducts)
        db_helper.execute_query(
            """
DO $$
BEGIN
  BEGIN
    EXECUTE 'DROP TABLE IF EXISTS "TestProducts"';
  EXCEPTION WHEN undefined_table THEN
    NULL;
  END;
  BEGIN
    EXECUTE 'DROP TABLE IF EXISTS testproducts';
  EXCEPTION WHEN undefined_table THEN
    NULL;
  END;
END $$;
            """.strip()
        )
        if db_helper.table_exists("TemplateBindings"):
            db_helper.execute_query('DELETE FROM "TemplateBindings" WHERE "EntityType" = \'TestProduct\' OR "EntityType" LIKE \'%TestProduct%\'')

        if db_helper.table_exists("FieldMetadatas") and db_helper.table_exists("EntityDefinitions"):
            db_helper.execute_query('DELETE FROM "FieldMetadatas" WHERE "EntityDefinitionId" IN (SELECT "Id" FROM "EntityDefinitions" WHERE "EntityName" = \'TestProduct\')')

        if db_helper.table_exists("EntityDefinitions"):
            db_helper.execute_query('DELETE FROM "EntityDefinitions" WHERE "EntityName" = \'TestProduct\' OR "FullTypeName" LIKE \'%TestProduct%\'')

    cleanup()
    yield

def test_entity_lifecycle(auth_admin, page: Page, cleanup_entity):
    # API-based E2E: Create + publish entity definition (UI is AntDesign-heavy and brittle for generic selectors).
    assert api_helper.login_as_admin()

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

    publish_resp = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
    assert publish_resp.status_code == 200, publish_resp.text

    # Compile/load the entity type so DynamicEntity endpoints can operate.
    compile_resp = requests.post(
        f"{API_BASE}/api/entity-definitions/{entity_id}/compile",
        headers=api_helper.get_headers(),
        timeout=120,
    )
    assert compile_resp.status_code == 200, compile_resp.text

    assert db_helper.table_exists("TestProducts")
