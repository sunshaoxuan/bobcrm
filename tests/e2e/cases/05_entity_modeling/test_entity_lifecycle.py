import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = "http://localhost:3000"

# TC-ENT-001 to 004 实体建模流程

@pytest.fixture
def cleanup_entity():
    yield
    # Force cleanup chain
    db_helper.execute_query("DROP TABLE IF EXISTS test_products")
    db_helper.execute_query("DELETE FROM TemplateBindings WHERE EntityTypeName = 'TestProduct'")
    db_helper.execute_query("DELETE FROM FieldDefinitions WHERE EntityDefinitionId IN (SELECT Id FROM EntityDefinitions WHERE Name = 'TestProduct')")
    db_helper.execute_query("DELETE FROM EntityDefinitions WHERE Name = 'TestProduct'")

def test_entity_lifecycle(auth_admin, page: Page, cleanup_entity):
    # TC-ENT-001: Create Entity
    page.goto(f"{BASE_URL}/entity-definitions/create")
    
    page.fill("input[name='name']", "TestProduct")
    page.fill("input[name='displayName']", "测试产品")
    page.fill("input[name='tableName']", "test_products")
    page.click("button:has-text('Next')") # Or Save
    
    page.wait_for_url("**/edit/**")
    page.screenshot(path="tests/e2e/screenshots/TC-ENT-001-created.png")

    # TC-ENT-002: Add Fields
    page.click("text=Fields") # Tab
    page.click("button:has-text('Add Field')")
    
    page.fill("input[name='name']", "ProductName")
    page.fill("input[name='displayName']", "产品名称")
    # Select type string
    page.click(".ant-select")
    page.click("text=String")
    page.click("button:has-text('Save')")
    
    expect(page.locator("text=ProductName")).to_be_visible()
    
    # Add Price Field
    page.click("button:has-text('Add Field')")
    page.fill("input[name='name']", "Price")
    # Select type decimal
    # ... (simplified)
    page.click("button:has-text('Save')")

    # TC-ENT-003: Publish
    page.click("button:has-text('Publish')")
    page.click("button:has-text('Confirm')")
    
    expect(page.locator("text=Published")).to_be_visible()
    page.screenshot(path="tests/e2e/screenshots/TC-ENT-003-published.png")

    # Verification: Check DB table created
    assert db_helper.table_exists("test_products")
