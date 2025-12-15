import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = "http://localhost:3000"

# TC-ORG-001 组织结构

@pytest.fixture
def cleanup_org():
    yield
    db_helper.execute_query("DELETE FROM OrganizationNodes WHERE Code IN ('HQ', 'TECH')")

def test_org_001_structure(auth_admin, page: Page, cleanup_org):
    page.goto(f"{BASE_URL}/organizations")
    
    # B1: Create Root
    # Use icon selector instead of text to support i18n
    page.click("button:has(.anticon-plus)")
    
    page.fill("input[name='name']", "总公司")
    page.fill("input[name='code']", "HQ")
    page.click("button:has-text('Save')")
    
    expect(page.locator("text=总公司")).to_be_visible()
    
    # C1: Create Child
    # Access context menu or child button on the row
    page.click("text=总公司") # Select row
    page.click("button[icon='plus']") # Add sub
    
    page.fill("input[name='name']", "技术部")
    page.fill("input[name='code']", "TECH")
    page.click("button:has-text('Save')")
    
    # Expand tree if needed
    page.click(".ant-tree-switcher") 
    expect(page.locator("text=技术部")).to_be_visible()
    
    page.screenshot(path="tests/e2e/screenshots/TC-ORG-001-structure.png")
