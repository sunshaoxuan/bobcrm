import pytest
from playwright.sync_api import Page, expect
import os

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-USER-003 个人资料

def test_user_003_profile(auth_admin: Page):
    page = auth_admin
    page.goto(f"{BASE_URL}/profile")
    
    # Verify username is visible (use .first to avoid strict mode violation with email/role)
    expect(page.locator(".info-value", has_text="admin").first).to_be_visible()
    
    # B1: Edit email (mocking change)
    # page.fill("input[name='email']", "admin-updated@example.com")
    # page.click("button:has-text('Save')")
    
    # C1: Password change (skip actual change to avoid locking out future tests)
    # Just verify form exists (use section-title or input elements to avoid i18n issues)
    expect(page.locator("input[type='password']").first).to_be_visible()
    
    page.screenshot(path="tests/e2e/screenshots/TC-USER-003-profile.png")
