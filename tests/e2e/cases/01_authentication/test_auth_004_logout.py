import os
import pytest
from playwright.sync_api import Page, expect

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-AUTH-004 用户登出

def test_auth_004_logout(auth_admin: Page):
    # Context is already logged in as admin via fixture
    page = auth_admin
    
    # A1: Verify dashboard
    expect(page).to_have_url(f"{BASE_URL}/")
    page.wait_for_timeout(1000)

    # Sanity: ensure we're actually logged in (token present).
    has_token = page.evaluate("localStorage.getItem('accessToken') != null")
    assert has_token is True

    # A2: Click logout button
    logout_btn = page.locator("[data-testid='logout-button']")
    try:
        logout_btn.wait_for(state="visible", timeout=8000)
    except Exception:
        # Fallback: match Ant icon class if testid isn't available.
        logout_btn = page.locator("button:has(.anticon-poweroff)")
        logout_btn.wait_for(state="visible", timeout=8000)

    logout_btn.click()
    
    # A4: Wait for login page (increase timeout for redirect)
    expect(page).to_have_url(f"{BASE_URL}/login", timeout=10000)
    
    # A5: Check token cleared
    token = page.evaluate("localStorage.getItem('accessToken')")
    assert token is None
    
    # A6: Access restricted page
    page.goto(f"{BASE_URL}/")
    expect(page).to_have_url(f"{BASE_URL}/login")
    
    page.screenshot(path="tests/e2e/screenshots/TC-AUTH-004-A6-redirect.png")
