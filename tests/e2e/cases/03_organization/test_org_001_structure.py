import pytest
import re
from playwright.sync_api import Page, expect
from utils.db import db_helper
import os

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-ORG-001 组织结构

@pytest.fixture
def cleanup_org():
    yield
    # PostgreSQL requires quotes for PascalCase table names
    db_helper.execute_query('DELETE FROM "OrganizationNodes" WHERE "Code" IN (\'HQ\', \'TECH\')')

def test_org_001_structure(auth_admin: Page, cleanup_org):
    page = auth_admin
    page.goto(f"{BASE_URL}/organizations")
    
    # Wait for page to load
    page.wait_for_load_state("networkidle")
    
    # B1: Create Root Organization
    # Use icon selector instead of text to support i18n
    # Find the first button with plus icon in org-tree-panel (Add Root button)
    page.click(".org-tree-panel button:has(.anticon-plus)")
    
    # Wait for form to appear in detail panel
    page.wait_for_selector(".org-node-form", state="visible", timeout=5000)
    
    # Fill form fields - Ant Design Input uses class="field-control", not name attribute
    # First field-control is Code, second is Name
    code_input = page.locator(".org-node-form .field-control").first
    name_input = page.locator(".org-node-form .field-control").nth(1)
    
    code_input.fill("HQ")
    name_input.fill("总公司")
    
    # Save button - use role+regex (Playwright CSS selector does not support /regex/ in :has-text).
    # BTN_SAVE translations: zh="保存", ja="保存", en="Save"
    page.locator(".org-node-form").get_by_role(
        "button",
        name=re.compile(r"保存|Save|BTN_SAVE"),
    ).click()
    
    # Wait for save to complete and tree to update
    # Wait for API call and UI update (LoadTree selects persisted node, enabling "Add Child").
    page.wait_for_timeout(800)
    # Verify root appears in tree (format: "HQ - 总公司")
    expect(page.locator("button.org-tree-node:has-text('HQ - 总公司')")).to_be_visible(timeout=5000)
    
    # C1: Create Child Organization
    # Wait for children panel "Add" button to be enabled (parent must be persisted, not Guid.Empty).
    children_add_button = page.locator(".org-detail-panel .org-card:has(.org-table) button:has(.anticon-plus)")
    expect(children_add_button).to_be_enabled(timeout=8000)
    children_add_button.click()
    
    # Step 3: Wait for new row to appear in table
    page.wait_for_selector(".org-table tbody tr", state="visible")
    
    # Fill child form in the table row
    # Child form is in a table, first row after header (or the new row)
    child_row = page.locator(".org-table tbody tr").first
    child_code_input = child_row.locator("input.field-control").first
    child_name_input = child_row.locator("input.field-control").nth(1)
    
    child_code_input.fill("TECH")
    child_name_input.fill("技术部")
    
    # Step 4: Click Save button in the table row
    child_row.get_by_role(
        "button",
        name=re.compile(r"保存|Save|BTN_SAVE"),
    ).click()
    
    # Wait for save to complete and UI to update
    page.wait_for_timeout(1500)
    
    # Verify child appears - it should be in the children table or tree
    # Check both table and tree
    child_in_table = page.locator(".org-table tbody tr:has-text('技术部')")
    child_in_tree = page.locator("button.org-tree-node:has-text('技术部')")
    
    # At least one should be visible
    expect(child_in_table.or_(child_in_tree)).to_be_visible(timeout=5000)
    
    page.screenshot(path="tests/e2e/screenshots/TC-ORG-001-structure.png")
