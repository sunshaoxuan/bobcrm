import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = "http://localhost:3000"

# TC-DATA-001 动态实体 CRUD & TC-CRM-001 客户管理
# TC-DASH-001 仪表盘

def test_data_001_dynamic_crud(auth_admin, page: Page):
    # Pre-req: TestProduct entity exists (from previous tests or assume persistent env)
    # If using sequential execution, this runs after entity lifecycle
    
    page.goto(f"{BASE_URL}/dynamic-entity/TestProduct")
    
    # Create
    page.click("button:has-text('New')")
    page.fill("input[name='ProductName']", "AutoTest Product")
    page.click("button:has-text('Save')")
    
    page.screenshot(path="tests/e2e/screenshots/TC-DATA-001-created.png")
    
    # Verify List
    expect(page.locator("text=AutoTest Product")).to_be_visible()
    
    # Cleanup data
    db_helper.execute_query("DELETE FROM test_products WHERE ProductName = 'AutoTest Product'")

def test_crm_001_customer(auth_admin, page: Page):
    page.goto(f"{BASE_URL}/customers")
    
    page.click("button:has-text('New Customer')") # Or local equivalent
    page.fill("input[name='Name']", "AutoTest Customer")
    page.click("button:has-text('Save')")
    
    expect(page.locator("text=AutoTest Customer")).to_be_visible()
    page.screenshot(path="tests/e2e/screenshots/TC-CRM-001-customer.png")
    
    db_helper.execute_query("DELETE FROM customer WHERE Name = 'AutoTest Customer'")

def test_dash_001_dashboard(auth_admin, page: Page):
    page.goto(f"{BASE_URL}/")
    
    expect(page.locator(".dashboard-hero-metric")).to_have_count(4)
    page.screenshot(path="tests/e2e/screenshots/TC-DASH-001-overview.png")
