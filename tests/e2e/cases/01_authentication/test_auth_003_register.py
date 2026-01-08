import pytest
import os
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-AUTH-003 用户注册与激活

@pytest.fixture
def cleanup_user():
    yield
    # Cleanup after test
    db_helper.execute_query("DELETE FROM \"RefreshTokens\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"UserName\" = 'newuser')")
    db_helper.execute_query("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"UserName\" = 'newuser')")
    db_helper.execute_query("DELETE FROM \"AspNetUsers\" WHERE \"UserName\" = 'newuser'")

def test_auth_003_register_and_activate(page: Page, cleanup_user):
    # A1
    page.goto(f"{BASE_URL}/register")
    
    # A2-A4
    page.fill("#register-username", "newuser")
    page.fill("#register-email", "newuser@example.com")
    page.fill("#register-password", "NewUser@123")
    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-003-A1-register-form.png")
    
    # A5
    page.click("button.ant-btn-primary", force=True)
    
    # Register.razor navigates to /activate on success.
    expect(page).to_have_url(f"{BASE_URL}/activate", timeout=10000)

    
    # B1: Simulate activation
    db_helper.execute_query("UPDATE \"AspNetUsers\" SET \"EmailConfirmed\" = TRUE WHERE \"UserName\" = 'newuser'")
    
    # B3: Login with new user
    page.goto(f"{BASE_URL}/login")
    page.fill("#login-username", "newuser")
    page.fill("#login-password", "NewUser@123")
    page.click("button.ant-btn-primary", force=True)
    
    expect(page).to_have_url(f"{BASE_URL}/")
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-003-B3-newuser-login.png")
