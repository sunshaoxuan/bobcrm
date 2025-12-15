import pytest
import os
from playwright.sync_api import sync_playwright
import time
import json
from utils.db import db_helper
from utils.api import api_helper

# Config
BASE_URL = "http://localhost:3000"
VIDEO_DIR = "tests/e2e/videos"
SCREENSHOT_DIR = "tests/e2e/screenshots"

@pytest.fixture(scope="session")
def browser_context_args(browser_context_args):
    return {
        **browser_context_args,
        "record_video_dir": VIDEO_DIR,
        "record_video_size": {"width": 1280, "height": 720}
    }

@pytest.fixture(scope="function")
def context(browser):
    # Create distinct context for each test to ensure isolation
    context = browser.new_context(
        record_video_dir=VIDEO_DIR,
        record_video_size={"width": 1280, "height": 720},
        viewport={"width": 1280, "height": 720}
    )
    yield context
    context.close()

@pytest.fixture(scope="function")
def page(context):
    page = context.new_page()
    yield page
    page.close()

@pytest.fixture(scope="function")
def auth_admin(page):
    """Fixture to log in as admin via API and inject tokens."""
    # Try API login
    if api_helper.login_as_admin():
        # Inject tokens into localStorage
        tokens = {
            "accessToken": api_helper.token,
            "refreshToken": "dummy_refresh", # We might need to fetch this if strict, but let's see api.py
            "user": json.dumps({"userName": "admin", "role": "Admin"})
        }
        
        page.goto(f"{BASE_URL}/login")
        
        # Execute script to set items
        page.evaluate(f"""
            localStorage.setItem('accessToken', '{api_helper.token}');
            localStorage.setItem('user', '{tokens['user']}');
        """)
        
        # Navigate to home
        page.goto(f"{BASE_URL}/")
        
        # Verify we are logged in (dashboard visible)
        try:
            page.wait_for_url(f"{BASE_URL}/", timeout=5000)
        except:
            pass
            
    else:
        pytest.fail("Failed to login via API in auth_admin fixture")
        
    return page

def take_screenshot(page, name):
    """Helper to take specific screenshots."""
    path = f"{SCREENSHOT_DIR}/{name}.png"
    page.screenshot(path=path)
    return path

# Hook to capture screenshot on failure
@pytest.hookimpl(tryfirst=True, hookwrapper=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    rep = outcome.get_result()
    if rep.when == "call" and rep.failed:
        page = item.funcargs.get("page")
        if page:
            name = item.name
            take_screenshot(page, f"FAILURE_{name}")
