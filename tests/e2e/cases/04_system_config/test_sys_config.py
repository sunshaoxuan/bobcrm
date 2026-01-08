import pytest
import os
import re
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-SYS-001 菜单管理

@pytest.fixture
def cleanup_menu():
    yield
    if db_helper.table_exists("FunctionNodes"):
        db_helper.execute_query('DELETE FROM "FunctionNodes" WHERE "Code" = \'TestMenu\' OR "Name" = \'TestMenu\'')

def test_sys_001_menus(auth_admin, page: Page, cleanup_menu):
    page = auth_admin
    page.goto(f"{BASE_URL}/menus")
    
    # UI is localized; match both EN/ZH/Key variants (use regex form).
    page.get_by_role("button", name=re.compile(r"Add Menu|新增根菜单|BTN_ADD_ROOT_MENU")).click()

    modal = page.locator(".ant-modal:visible")

    # The modal mixes normal <Input> and <MultilingualInput> (which also renders inputs).
    # Select fields by the stable form-field order to avoid accidentally filling the i18n trigger input.
    code_input = modal.locator(".menu-form .form-field").nth(1).locator("input")
    name_input = modal.locator(".menu-form .form-field").nth(2).locator("input")

    code_input.fill("TestMenu")
    name_input.fill("TestMenu")

    # Route field uses a stable placeholder.
    modal.locator("input[placeholder='/customers']").fill("/test")

    # Confirm (modal primary button).
    modal.get_by_role("button", name=re.compile(r"保存|Save|BTN_SAVE")).click()

    # Tree/detail panel should contain the new code/name.
    expect(page.locator(".menu-layout")).to_contain_text("TestMenu", timeout=8000)
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-001-menu-created.png")

# TC-SYS-002 系统设置

def test_sys_002_settings(auth_admin, page: Page):
    page = auth_admin
    page.goto(f"{BASE_URL}/settings")
    
    # Smoke check: settings shell renders and cards are present (avoid brittle UI text selectors).
    expect(page.locator(".settings-shell")).to_be_visible(timeout=5000)
    expect(page.locator(".settings-card").first).to_be_visible(timeout=5000)
    
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-002-settings.png")

# TC-SYS-003 枚举管理

@pytest.fixture
def cleanup_enum():
    yield
    if db_helper.table_exists("EnumOptions") and db_helper.table_exists("EnumDefinitions"):
        db_helper.execute_query('DELETE FROM "EnumOptions" WHERE "EnumDefinitionId" IN (SELECT "Id" FROM "EnumDefinitions" WHERE "Code" = \'OrderStatus\')')
        db_helper.execute_query('DELETE FROM "EnumDefinitions" WHERE "Code" = \'OrderStatus\'')

def test_sys_003_enums(auth_admin, page: Page, cleanup_enum):
    page = auth_admin
    page.goto(f"{BASE_URL}/system/enums")
    
    # Click the primary "new enum" action (avoid brittle i18n text matching).
    page.locator("button.ant-btn-primary:has(.anticon-plus)").click()
    expect(page).to_have_url(re.compile(r"/system/enums/create"))

    # Code
    code_input = page.locator("input.ant-input").first
    expect(code_input).to_be_visible(timeout=10000)
    code_input.fill("OrderStatus")

    # Display name (MultilingualInput): open overlay and fill the first visible language field.
    page.locator(".multilingual-trigger").first.click()
    overlay = page.locator(".multilingual-overlay-container")
    expect(overlay).to_be_visible(timeout=5000)
    overlay.locator("input.multilingual-field").first.fill("订单状态")
    page.locator(".multilingual-overlay-backdrop").click()

    # Save
    page.get_by_role("button", name=re.compile(r"保存|Save|BTN_SAVE")).click()
    expect(page).to_have_url(re.compile(r"/system/enums$"), timeout=8000)
    expect(page.locator("text=OrderStatus")).to_be_visible(timeout=8000)

# TC-SYS-004 文件管理

def test_sys_004_files(auth_admin, page: Page):
    page = auth_admin
    page.goto(f"{BASE_URL}/files")

    # Create dummy file in test sandbox directory
    import tempfile
    import pathlib

    tmp_dir = pathlib.Path(tempfile.mkdtemp(prefix="bobcrm-e2e-"))
    file_path = tmp_dir / "dummy.txt"
    file_path.write_text("test content", encoding="utf-8")

    page.set_input_files("input[type='file']", str(file_path))
    page.get_by_role("button", name=re.compile(r"Upload|上传|アップロード")).click()

    expect(page.locator("text=已上传")).to_be_visible(timeout=8000)
    expect(page.locator("p:has-text('已上传') a")).to_be_visible(timeout=8000)
    page.screenshot(path="tests/e2e/screenshots/TC-SYS-004-upload.png")
    
    try:
        file_path.unlink(missing_ok=True)
        tmp_dir.rmdir()
    except Exception:
        pass
