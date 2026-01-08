import pytest
import os
import re
from playwright.sync_api import Page, expect
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")

# TC-FORM-001 ~ 003 表单设计流程

@pytest.fixture
def cleanup_template():
    yield
    if db_helper.table_exists("TemplateBindings") and db_helper.table_exists("FormTemplates"):
        db_helper.execute_query('DELETE FROM "TemplateBindings" WHERE "TemplateId" IN (SELECT "Id" FROM "FormTemplates" WHERE "Name" = \'ProductForm\')')
        db_helper.execute_query('DELETE FROM "FormTemplates" WHERE "Name" = \'ProductForm\'')

def test_form_design_lifecycle(auth_admin, page: Page, cleanup_template):
    page = auth_admin
    # TC-FORM-001: Templates page smoke
    page.goto(f"{BASE_URL}/templates")

    # New template -> navigates to designer page.
    page.locator("button.btn.btn-primary").first.click()
    expect(page).to_have_url(re.compile(r".*/designer/new$"))
    page.screenshot(path="tests/e2e/screenshots/TC-FORM-002-designer.png")

    # TC-FORM-003: Binding
    page.goto(f"{BASE_URL}/templates/bindings")
    expect(page.locator(".template-bindings-page")).to_be_visible(timeout=5000)
