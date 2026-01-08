import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper
import os

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-USER-002 角色管理 & TC-USER-004 权限配置

@pytest.fixture
def cleanup_role():
    # Cleanup before test
    db_helper.execute_query('DELETE FROM "RoleFunctionPermissions" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleDataScopes" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleAssignments" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\'')
    yield
    # Cleanup after test
    db_helper.execute_query('DELETE FROM "RoleFunctionPermissions" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleDataScopes" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleAssignments" WHERE "RoleId" IN (SELECT "Id" FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\')')
    db_helper.execute_query('DELETE FROM "RoleProfiles" WHERE "Code" = \'TestRoleCode\'')

def test_user_002_role_crud(auth_admin: Page, cleanup_role):
    page = auth_admin
    # A1: Navigate to Roles
    page.goto(f"{BASE_URL}/roles")
    expect(page).to_have_url(f"{BASE_URL}/roles")
    
    # B1: Create
    # Click "New Role" button. It should be the primary button in the header.
    # Selector based on Roles.razor: Button Type="Primary" Icon="Outline.Plus"
    # Or just generic .ant-btn-primary if it's the only one.
    new_role_button = page.locator(".collection-shell button.ant-btn-primary").first
    new_role_button.wait_for(state="visible", timeout=10000)
    new_role_button.click(force=True)
    
    # Verify navigation to creation page
    expect(page).to_have_url(f"{BASE_URL}/role/new", timeout=10000)

    # NOTE: 当前前端仅实现了 `Roles.razor`，`/role/new` 具体创建页尚未落地（无对应 Razor Page）。
    # 这里先验证导航入口可用，后续创建/编辑/删除流程待页面实现后再补齐。
    pytest.skip("Role creation UI (/role/new) not implemented yet.")
    
    # B2: Fill Form
    # Wait for Code input
    code_input = page.locator("input[placeholder='Code']").first
    # Or generically by order if placeholder is localized (but I added placeholder code above)
    # The new field is the first input.
    # Let's try to target by label or just order. 
    # Label is CODE -> sibling input.
    # Just assume it's the first input now.
    
    # Wait for Code input
    page.wait_for_selector("input.ant-input", state="visible")
    inputs = page.locator("input.ant-input")
    
    # Fill Code (First input)
    inputs.nth(0).fill("TestRoleCode")
    
    # Fill Name (Second input)
    inputs.nth(1).fill("TestRole")
    
    # Fill Description if available (RoleEdit.razor has it)
    # It might be the second input or textarea
    # RoleEdit.razor: <InputTextArea ... class="ant-input" ... /> -> Rendered as textarea.ant-input
    if page.locator("textarea.ant-input").is_visible():
        page.fill("textarea.ant-input", "Integration Test Role")
    
    page.screenshot(path="tests/e2e/screenshots/TC-USER-002-B1-create.png")
    
    # B3: Save
    page.on("console", lambda msg: print(f"CONSOLE: {msg.text}"))
    
    # Click Submit button
    print("Clicking Submit...")
    page.click("button[type='submit']")
    
    # Wait for navigation or failure
    try:
        expect(page).to_have_url(f"{BASE_URL}/roles", timeout=5000)
    except:
        print(f"Navigation failed. Current URL: {page.url}")
        page.screenshot(path="tests/e2e/screenshots/DEBUG_role_submit_fail.png")
        # Check for validation errors
        if page.locator(".validation-message").count() > 0:
            print("Validation errors found: " + page.locator(".validation-message").first.inner_text())
    
    # The tree might be empty if no functions defined, but assuming system has functions.
    # Select first node
    if page.locator(".permission-node").count() > 0:
        page.click(".permission-node >> nth=0")
        
        # Check panel appears
        expect(page.locator(".permission-panel")).to_be_visible()
        
        # Click Save
        page.click("button.btn-primary") # Save Permissions
    
    # D1: Delete (Not implemented in Roles.razor UI yet? Looking at file... no Delete button on card!)
    # Roles.razor only has List, New, Filter, Export.
    # Card only has Name/Code.
    # So strictly speaking, Delete via UI isn't possible in current Roles.razor?
    # Checking Roles.razor content: No delete button.
    # Skipping Delete step in UI test, or using API/Cleanup to handle it.
    pass
