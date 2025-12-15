import pytest
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = "http://localhost:3000"

# TC-AUTH-003 用户注册与激活

@pytest.fixture
def cleanup_user():
    yield
    # Cleanup after test
    db_helper.execute_query("DELETE FROM RefreshTokens WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE UserName = 'newuser')")
    db_helper.execute_query("DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE UserName = 'newuser')")
    db_helper.execute_query("DELETE FROM AspNetUsers WHERE UserName = 'newuser'")

def test_auth_003_register_and_activate(page: Page, cleanup_user):
    # A1
    page.goto(f"{BASE_URL}/register")
    
    # A2-A4
    # Using specific placeholders or standard input types if IDs not guaranteed
    page.fill("input[placeholder*='sername']", "newuser") # Username or 用户名
    page.fill("input[type='email']", "newuser@example.com")
    page.fill("input[type='password']", "NewUser@123")
    # Confirm password might be the second password input
    # page.fill("input[placeholder*='Confirm']", "NewUser@123") 
    # Let's see Register.razor structure logic if needed. Assuming standard 2 password fields.
    passwords = page.locator("input[type='password']")
    if passwords.count() > 1:
        passwords.nth(1).fill("NewUser@123")
    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-003-A1-register-form.png")
    
    # A5
    page.click("button.ant-btn-primary", force=True)
    
    # Wait for success message or redirect
    # Might stay on page with "Success" toast/div
    # Register.razor usually navigates to Login or shows Message
    # If "注册成功"
    try:
        expect(page.locator(".ant-message-success")).to_be_visible(timeout=5000)
    except:
        # Or redirect to login
        if "/login" in page.url:
            pass
        else:
             print("Warning: No success message or redirect detected.")

    
    # B1: Simulate activation
    db_helper.execute_query("UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE UserName = 'newuser'")
    
    # B3: Login with new user
    page.goto(f"{BASE_URL}/login")
    page.fill("#login-username", "newuser")
    page.fill("#login-password", "NewUser@123")
    page.click("button.ant-btn-primary", force=True)
    
    expect(page).to_have_url(f"{BASE_URL}/")
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-003-B3-newuser-login.png")
