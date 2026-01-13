import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper
import os
from utils.api import api_helper

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

    # Precondition: create a role via API (UI /role/new is not implemented yet, but Roles list & permissions panel are)
    assert api_helper.login_as_admin(), "Failed to login as admin via API for role setup"
    create = api_helper.post(
        "/api/access/roles",
        {
            "organizationId": None,
            "code": "TestRoleCode",
            "name": "TestRole",
            "description": "Integration Test Role",
            "isEnabled": True,
            "functionIds": [],
            "dataScopes": [],
        },
    )
    assert create.status_code == 200, create.text

    # A1: Navigate to Roles
    page.goto(f"{BASE_URL}/roles")
    expect(page).to_have_url(f"{BASE_URL}/roles")
    
    # B1: Assert the created role is visible in the list
    role_card = page.locator(".role-card", has_text="TestRoleCode").first
    expect(role_card).to_be_visible(timeout=15000)
    role_card.click()

    # C1: Permissions tree/panel should be available (TC-USER-004 entry)
    tree = page.locator("[data-testid='permission-tree']")
    expect(tree).to_be_visible(timeout=15000)
    first_node = tree.locator(".permission-node").first
    if first_node.count() > 0:
        first_node.click()
        panel = page.locator(".permission-panel")
        expect(panel).to_be_visible(timeout=15000)
        save_btn = page.locator("[data-testid='save-permissions-button']").first
        expect(save_btn).to_be_visible(timeout=15000)
        save_btn.click()

    # Delete is handled by cleanup_role fixture (DB), since UI delete is not exposed on Roles.razor yet.
    return
