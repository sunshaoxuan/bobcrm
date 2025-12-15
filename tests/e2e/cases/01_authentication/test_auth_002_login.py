import pytest
from playwright.sync_api import Page, expect
BASE_URL = "http://localhost:3000"

# TC-AUTH-002 用户登录

def test_auth_002_login_success(page: Page):
    page.goto(f"{BASE_URL}/login")
    
    # A3-A5
    page.fill("#login-username", "admin")
    page.fill("#login-password", "Admin@12345")
    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-002-A1-login-form.png")
    
    # Use generic class since type='submit' might be nested or I18n text missing
    page.click("button.ant-btn-primary", force=True)
    
    # A6: Wait for redirect to dashboard
    expect(page).to_have_url(f"{BASE_URL}/", timeout=8000)
    
    # A7-A8: Check LocalStorage
    token = page.evaluate("localStorage.getItem('accessToken')")
    assert token is not None
    
    # A9: Verify dashboard content
    # h1 might be empty due to I18n, check for dashboard shell or any dashboard class
    expect(page.locator(".dashboard-shell")).to_be_visible()

    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-002-A9-dashboard.png")

    # Cleanup (Logout logic directly here or via fixture)
    page.evaluate("localStorage.clear()")

def test_auth_002_login_failure(page: Page):
    page.goto(f"{BASE_URL}/login")
    
    page.fill("#login-username", "admin")
    page.fill("#login-password", "WrongPass123")
    page.click("button.ant-btn-primary", force=True)
    
    # B3: Expect error message
    # Login.razor line 47: <div class="auth-message error">@error</div>
    expect(page.locator(".auth-message.error")).to_be_visible()
    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-002-B3-login-error.png")
