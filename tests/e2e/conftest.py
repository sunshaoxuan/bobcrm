import pytest
import os
from playwright.sync_api import sync_playwright, expect
import time
import json
from utils.db import db_helper
from utils.api import api_helper
import requests

# Config
BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")
E2E_LANG = os.getenv("E2E_LANG", "en").strip() or "en"
VIDEO_DIR = "tests/e2e/videos"
SCREENSHOT_DIR = "tests/e2e/screenshots"

@pytest.fixture(scope="session", autouse=True)
def ensure_admin_exists():
    """
    Ensure the environment is initialized for E2E runs.

    Many UI flows rely on setup being done and persisted (DB), but tests run in isolated browser contexts.
    """
    last_error = None
    for attempt in range(1, 8):
        try:
            resp = requests.post(
                f"{API_BASE}/api/setup/admin",
                json={"username": "admin", "email": "admin@example.com", "password": "Admin@12345"},
                headers={"X-Lang": E2E_LANG.lower()},
                timeout=15,
            )
            resp.raise_for_status()

            # Ensure system default templates exist for all seeded entity definitions.
            regen = requests.post(
                f"{API_BASE}/api/admin/templates/regenerate-defaults",
                headers={"X-Lang": E2E_LANG.lower()},
                timeout=60,
            )
            regen.raise_for_status()
            return
        except Exception as ex:
            last_error = ex
            time.sleep(0.8)

    pytest.fail(f"Failed to initialize E2E admin/templates after retries: {last_error}")

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

    # Ensure required client-side config exists for each isolated context.
    context.add_cookies(
        [
            {"name": "lang", "value": E2E_LANG.lower(), "url": BASE_URL},
            {"name": "apiBase", "value": API_BASE, "url": BASE_URL},
        ]
    )
    context.add_init_script(
        f"""
        try {{
            localStorage.setItem('lang', '{E2E_LANG.lower()}');
            localStorage.setItem('apiBase', '{API_BASE}');
            localStorage.setItem('configured', 'true');
        }} catch (e) {{}}
        """
    )

    yield context
    context.close()

@pytest.fixture(scope="function")
def page(context):
    page = context.new_page()

    # Make page.goto resilient for Blazor Server prerendering:
    # wait until app.js is loaded so event handlers & JS helpers exist.
    _orig_goto = page.goto

    def _goto_and_wait(url: str, *args, **kwargs):
        # Use a less strict wait mode for Blazor Server to avoid ERR_ABORTED during fast redirects.
        if "wait_until" not in kwargs and "waitUntil" not in kwargs:
            kwargs["wait_until"] = "domcontentloaded"

        try:
            result = _orig_goto(url, *args, **kwargs)
        except Exception as ex:
            # Blazor can trigger a fast client-side redirect which aborts the initial navigation.
            msg = str(ex)
            if "net::ERR_ABORTED" not in msg and "Navigation to" not in msg:
                raise
            result = None

        if isinstance(url, str) and url.startswith(BASE_URL) and "/api/" not in url:
            # Optional: wait for bobcrm helpers (may not be available during prerender).
            try:
                page.wait_for_function(
                    "() => typeof window !== 'undefined' && window.bobcrm && typeof window.bobcrm.getCookie === 'function'",
                    timeout=15000,
                )
            except Exception:
                pass

            # Required: ensure the circuit is interactive (splash removed or stage ready).
            page.wait_for_function(
                "() => !document.querySelector('.app-splash') || document.querySelector('.app-stage.ready')",
                timeout=15000,
            )

        return result

    page.goto = _goto_and_wait  # type: ignore[assignment]

    yield page
    page.close()

@pytest.fixture(scope="function")
def auth_admin(page):
    """Fixture to log in as admin via API and inject tokens into localStorage."""
    if not api_helper.login_as_admin():
        pytest.fail("Failed to login via API in auth_admin fixture")

    page.goto(f"{BASE_URL}/login")
    page.evaluate(
        """
        (tokens) => {
            localStorage.setItem('accessToken', tokens.accessToken);
            if (tokens.refreshToken) localStorage.setItem('refreshToken', tokens.refreshToken);
            localStorage.setItem('lang', tokens.lang);
            localStorage.setItem('apiBase', tokens.apiBase);
            localStorage.setItem('configured', 'true');
        }
        """,
        {
            "accessToken": api_helper.token,
            "refreshToken": api_helper.refresh_token,
            "lang": E2E_LANG.lower(),
            "apiBase": API_BASE,
        },
    )

    page.goto(f"{BASE_URL}/")
    expect(page).to_have_url(f"{BASE_URL}/", timeout=8000)
    page.wait_for_timeout(1000)

    token = page.evaluate("localStorage.getItem('accessToken')")
    assert token is not None

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
