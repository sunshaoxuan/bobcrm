import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = "http://localhost:3000"

# TC-SYS-001 菜单管理

@pytest.fixture
def cleanup_menu():
    yield
    db_helper.execute_query("DELETE FROM MenuItems WHERE Name = 'TestMenu'")

def test_sys_001_menus(auth_admin, page: Page, cleanup_menu):
    page.goto(f"{BASE_URL}/menus")
    
    page.click("button:has-text('Add Menu')")
    
    page.fill("input[name='name']", "TestMenu")
    page.fill("input[name='route']", "/test")
    page.click("button:has-text('Save')")
    
    expect(page.locator("text=TestMenu")).to_be_visible()
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-001-menu-created.png")

# TC-SYS-002 系统设置

def test_sys_002_settings(auth_admin, page: Page):
    page.goto(f"{BASE_URL}/settings")
    
    # Toggle Theme
    page.click("text=Dark Mode") # Switch
    
    # Verify dark mode applied (check body class or background)
    # expect(page.locator("body")).to_have_class(re.compile(r"dark"))
    
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-002-settings.png")
    
    # Revert
    page.click("text=Dark Mode")

# TC-SYS-003 枚举管理

@pytest.fixture
def cleanup_enum():
    yield
    db_helper.execute_query("DELETE FROM EnumItems WHERE EnumDefinitionId IN (SELECT Id FROM EnumDefinitions WHERE Name = 'OrderStatus')")
    db_helper.execute_query("DELETE FROM EnumDefinitions WHERE Name = 'OrderStatus'")

def test_sys_003_enums(auth_admin, page: Page, cleanup_enum):
    page.goto(f"{BASE_URL}/system/enums")
    
    page.click("button:has-text('Create')")
    
    page.fill("input[name='name']", "OrderStatus")
    page.fill("input[name='displayName']", "订单状态")
    
    # Add Item
    page.click("button:has-text('Add Item')")
    page.fill("input[placeholder='Value']", "pending")
    page.fill("input[placeholder='Label']", "Pending")
    
    page.click("button:has-text('Save')")
    
    expect(page.locator("text=OrderStatus")).to_be_visible()

# TC-SYS-004 文件管理

def test_sys_004_files(auth_admin, page: Page):
    page.goto(f"{BASE_URL}/files")
    
    # Mocking file upload or using a real dummy file
    with page.expect_file_chooser() as fc_info:
        page.click("input[type='file']")
    
    # Create dummy file
    with open("dummy.txt", "w") as f:
        f.write("test content")
        
    file_chooser = fc_info.value
    file_chooser.set_files("dummy.txt")
    
    page.click("button:has-text('Upload')")
    
    expect(page.locator("text=http")).to_be_visible() # URL shown
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-004-upload.png")
    
    import os
    os.remove("dummy.txt")
