import os
import json
import uuid
import time
import requests
import pytest
from playwright.sync_api import Page, expect

from utils.api import api_helper
from utils.db import db_helper

BASE_URL = os.getenv("BASE_URL", "http://localhost:3000").rstrip("/")
API_BASE = os.getenv("API_BASE", "http://localhost:5200").rstrip("/")

def _get_template_detail(template_id: int) -> dict:
    assert api_helper.login_as_admin()
    resp = requests.get(
        f"{API_BASE}/api/templates/{template_id}",
        headers=api_helper.get_headers(),
        timeout=30,
    )
    assert resp.status_code == 200, resp.text
    return resp.json()["data"]

@pytest.fixture
def cleanup_cascade_entity():
    def cleanup():
        entity_name = "TestCascadePublish"
        # 1. Drop Table
        db_helper.execute_query(f'DROP TABLE IF EXISTS "{entity_name}s"')
        # 2. Delete Bindings
        db_helper.execute_query(f'DELETE FROM "TemplateBindings" WHERE "EntityType" = \'{entity_name}\'')
        # 3. Delete Templates
        db_helper.execute_query(f'DELETE FROM "FormTemplates" WHERE "EntityType" = \'{entity_name}\'')
        # 4. Delete Fields & Entity Definition
        db_helper.execute_query(f'''
            DELETE FROM "FieldMetadatas" 
            WHERE "EntityDefinitionId" IN (SELECT "Id" FROM "EntityDefinitions" WHERE "EntityName" = '{entity_name}')
        ''')
        db_helper.execute_query(f'DELETE FROM "EntityDefinitions" WHERE "EntityName" = \'{entity_name}\'')

    cleanup()
    yield
    cleanup()

def test_batch3_001_cascade_publish(auth_admin, page: Page, cleanup_cascade_entity):
    """
    Test Case: Verify that adding a field to an entity and publishing it automatically updates the associated form templates.
    Steps:
    1. Create a new entity 'TestCascadePublish' with one field 'FieldA'.
    2. Publish the entity.
    3. Verify that the default 'DetailView' template contains 'FieldA'.
    4. Update the entity definition: Add a new field 'FieldB'.
    5. Publish changes.
    6. Verify that the default 'DetailView' template now contains BOTH 'FieldA' and 'FieldB'.
    """
    assert api_helper.login_as_admin()
    
    entity_name = "TestCascadePublish"
    
    # --- Step 1: Create Entity ---
    create_payload = {
        "namespace": "BobCrm.Base.Custom",
        "entityName": entity_name,
        "displayName": {"zh": "级联测试", "en": "Cascade Test"},
        "structureType": "Single",
        "fields": [
            {
                "propertyName": "FieldA",
                "displayName": {"zh": "字段A", "en": "Field A"},
                "dataType": "String",
                "isRequired": True,
                "sortOrder": 10,
            }
        ],
    }
    
    resp = api_helper.post("/api/entity-definitions", create_payload)
    assert resp.status_code in (200, 201), f"Create failed: {resp.text}"
    entity_id = resp.json()["data"]["id"]
    
    # --- Step 2: Publish Entity ---
    publish_resp = api_helper.post(f"/api/entity-definitions/{entity_id}/publish", {})
    assert publish_resp.status_code == 200, f"Publish new entity failed: {publish_resp.text}"
    
    # --- Step 3: Verify Initial Template ---
    # Parse bindings from publish response
    publish_data = publish_resp.json()["data"]
    bindings = publish_data.get("bindings", [])
    assert len(bindings) > 0, f"No bindings returned in publish response: {publish_data}"
    
    # Find Detail binding
    # UsageType enum ToString() is likely 'Detail', 'List', 'Edit', 'Combined'
    detail_binding = next((b for b in bindings if b["usageType"] == "Detail"), None)
    assert detail_binding, f"Detail binding not found. Available: {[b['usageType'] for b in bindings]}"
    
    template_id = detail_binding["templateId"]
    
    template_detail = _get_template_detail(template_id)
    # The API returns FormTemplate model, which has 'LayoutJson' property (likely camelCased to 'layoutJson')
    layout_json_str = template_detail.get("layoutJson")
    assert layout_json_str, f"layoutJson missing in template detail. Keys: {template_detail.keys()}"
    content = json.loads(layout_json_str)
    
    # Helper to check if field exists in layout
    def has_field(layout_content, field_name):
        layout_str = json.dumps(layout_content)
        return f'"{field_name}"' in layout_str

    assert has_field(content, "FieldA"), "Template should contain FieldA initially"
    
    # --- Step 4: Add New Field ---
    # We need to fetch the current entity definition first to update it properly (handling optimistic concurrency if needed, but here simple PUT is fine)
    # Actually, simpler to just PATCH/PUT the specific update.
    # But based on the API, we need to send the full DTO usually.
    # Let's clean up the payload for update.
    
    # --- Step 4: Add New Field ---
    # Since the entity is Published, we can use Patch semantics (send only new fields).
    # This avoids issues with mapping existing generic FieldDtos back to UpdateDta.
    
    new_field = {
        "propertyName": "FieldB",
        "displayName": {"zh": "字段B", "en": "Field B"},
        "dataType": "Integer",
        "sortOrder": 20,
        "isRequired": False
    }
    
    update_payload = {"fields": [new_field]}
    
    update_resp = requests.put(
        f"{API_BASE}/api/entity-definitions/{entity_id}",
        json=update_payload,
        headers=api_helper.get_headers()
    )
    assert update_resp.status_code == 200, f"Update entity failed: {update_resp.text}"
    
    # --- Step 5: Publish Changes ---
    publish_changes_resp = api_helper.post(f"/api/entity-definitions/{entity_id}/publish-changes", {})
    assert publish_changes_resp.status_code == 200, f"Publish changes failed: {publish_changes_resp.text}"
    
    # --- Step 6: Verify Template Update ---
    # Re-fetch template content
    template_detail_v2 = _get_template_detail(template_id)
    layout_json_str_v2 = template_detail_v2.get("layoutJson")
    assert layout_json_str_v2, f"layoutJson missing in template detail v2"
    content_v2 = json.loads(layout_json_str_v2)
    
    assert has_field(content_v2, "FieldA"), "Template should still contain FieldA"
    assert has_field(content_v2, "FieldB"), "Template should now contain FieldB after cascade publish"




def test_batch3_002_entity_conversion(auth_admin, page: Page):
    """
    Test Case: Entity Conversion (Lead -> Account)
    
    Status:
    - [x] Create Source Entity (TestLead)
    - [x] Create Target Entity (TestAccount)
    - [x] Insert Source Data
    - [x] Perform Conversion (Simulate by Read -> Map -> Create)
    - [x] Verify Target Data
    """
    
    # 1. Define Entity Names
    ts = int(time.time())
    namespace = "BobCrm.Base.Custom"
    lead_entity_name = f"TestLead{ts}"
    lead_full_name = f"{namespace}.{lead_entity_name}"
    account_entity_name = f"TestAccount{ts}"
    account_full_name = f"{namespace}.{account_entity_name}"
    
    # 2. Create Source Entity Definition (Lead)
    lead_payload = {
        "namespace": namespace,
        "entityName": lead_entity_name,
        "displayName": {"zh": "测试线索", "en": "Test Lead"},
        "structureType": "Custom",
        "fields": [
            {
                "propertyName": "Company",
                "displayName": {"zh": "公司", "en": "Company"},
                "dataType": "Text",
                "length": 100,
                "isRequired": True
            },
            {
                "propertyName": "ContactName",
                "displayName": {"zh": "联系人", "en": "Contact Name"},
                "dataType": "Text",
                "length": 50
            },
            {
                "propertyName": "Email",
                "displayName": {"zh": "邮箱", "en": "Email"},
                "dataType": "Email"
            }
        ]
    }
    
    resp_lead = requests.post(
        f"{API_BASE}/api/entity-definitions",
        json=lead_payload,
        headers=api_helper.get_headers()
    )
    assert resp_lead.status_code == 201, f"Create Lead Entity failed: {resp_lead.text}"
    lead_id = resp_lead.json()["data"]["id"]
    
    # Publish Lead
    requests.post(
        f"{API_BASE}/api/entity-definitions/{lead_id}/publish",
        json={},
        headers=api_helper.get_headers()
    )
    
    # 3. Create Target Entity Definition (Account)
    account_payload = {
        "namespace": namespace,
        "entityName": account_entity_name,
        "displayName": {"zh": "测试客户", "en": "Test Account"},
        "structureType": "Custom",
        "fields": [
            {
                "propertyName": "AccountName",
                "displayName": {"zh": "客户名称", "en": "Account Name"},
                "dataType": "Text",
                "length": 100,
                "isRequired": True
            },
             {
                "propertyName": "PrimaryContact",
                "displayName": {"zh": "主要联系人", "en": "Primary Contact"},
                "dataType": "Text",
                "length": 50
            },
             {
                "propertyName": "BusinessEmail",
                "displayName": {"zh": "业务邮箱", "en": "Business Email"},
                "dataType": "Email"
            }
        ]
    }
    
    resp_account = requests.post(
        f"{API_BASE}/api/entity-definitions",
        json=account_payload,
        headers=api_helper.get_headers()
    )
    assert resp_account.status_code == 201, f"Create Account Entity failed: {resp_account.text}"
    account_id = resp_account.json()["data"]["id"]
    
    # Publish Account
    requests.post(
        f"{API_BASE}/api/entity-definitions/{account_id}/publish",
        json={},
        headers=api_helper.get_headers()
    )
    
    # Retrieve Entity Gen Info for Debug
    debug_lead = requests.get(f"{API_BASE}/api/entity-definitions/{lead_id}", headers=api_helper.get_headers()) 
    print(f"DEBUG LEAD DEF: {debug_lead.text}")

    # 4. Create Source Data (Lead Record)
    # We need to use the newly created dynamic API for the entity
    # Endpoint pattern: /api/dynamic-entities/{fullTypeName}
    
    lead_data = {
        "Company": "Acme Corp",
        "ContactName": "John Doe",
        "Email": "john.doe@acme.com"
    }
    
    # Wait for endpoint to be available (dynamic definitions might take a moment)
    for i in range(5):
        create_lead_resp = requests.post(
            f"{API_BASE}/api/dynamic-entities/{lead_full_name}",
            json=lead_data,
            headers=api_helper.get_headers()
        )
        if create_lead_resp.status_code == 201:
            break
        time.sleep(1)
        
    assert create_lead_resp.status_code == 201, f"Create Lead Data failed after retries: {create_lead_resp.text} (Status: {create_lead_resp.status_code})"
    lead_record_id = create_lead_resp.json()["data"]["data"]["Id"]
    
    # 5. Perform Conversion (Simulate Business Logic)
    # Step A: Read Lead Data
    get_lead_resp = requests.get(
        f"{API_BASE}/api/dynamic-entities/{lead_full_name}/{lead_record_id}",
        headers=api_helper.get_headers()
    )
    assert get_lead_resp.status_code == 200
    source_record = get_lead_resp.json()["data"]["data"]
    print(f"DEBUG SOURCE_RECORD: {source_record}")
    
    # Step B: Map Data (Lead -> Account)
    account_data = {
        "AccountName": source_record["Company"],
        "PrimaryContact": source_record["ContactName"],
        "BusinessEmail": source_record["Email"]
    }
    
    # Step C: Create Account Record
    create_account_resp = requests.post(
        f"{API_BASE}/api/dynamic-entities/{account_full_name}",
        json=account_data,
        headers=api_helper.get_headers()
    )
    assert create_account_resp.status_code == 201, f"Conversion failed (Create Account): {create_account_resp.text}"
    account_record_id = create_account_resp.json()["data"]["data"]["Id"]
    
    # 6. Verify Target Data
    get_account_resp = requests.get(
        f"{API_BASE}/api/dynamic-entities/{account_full_name}/{account_record_id}",
        headers=api_helper.get_headers()
    )
    assert get_account_resp.status_code == 200
    target_record = get_account_resp.json()["data"]["data"]
    
    assert target_record["AccountName"] == "Acme Corp"
    assert target_record["PrimaryContact"] == "John Doe"
    assert target_record["BusinessEmail"] == "john.doe@acme.com"
    
    # Cleanup (Optional, relying on DB rollback or separate cleanu tool usually better, but here we use specific names)
    # We can delete definitions at the end
    requests.delete(f"{API_BASE}/api/entity-definitions/{lead_id}", headers=api_helper.get_headers())
    requests.delete(f"{API_BASE}/api/entity-definitions/{account_id}", headers=api_helper.get_headers())

def test_batch3_003_advanced_validation(auth_admin, page: Page):
    """
    测试用例: 高级校验规则 (Advanced Validation)
    
    验证内容:
    - [x] 正则表达式校验 (Regex Validation)
    - [x] 数值范围校验 (Range Validation: Min/Max)
    - [x] 必填字段校验 (Required Field)
    """
    
    # 1. 定义实体名称
    ts = int(time.time())
    namespace = "BobCrm.Base.Custom"
    entity_name = f"TestValidation{ts}"
    full_name = f"{namespace}.{entity_name}"
    
    # 2. 创建包含高级校验规则的实体定义
    create_payload = {
        "namespace": namespace,
        "entityName": entity_name,
        "displayName": {"zh": "校验测试", "en": "Validation Test"},
        "structureType": "Custom",
        "fields": [
            {
                "propertyName": "Code",
                "displayName": {"zh": "编码", "en": "Code"},
                "dataType": "Text",
                "length": 20,
                "isRequired": True,
                # 正则: 3个大写字母-3个数字 (例如: ABC-123)
                "validationRules": json.dumps({
                    "regex": "^[A-Z]{3}-\\d{3}$",
                    "regexMessage": "格式必须为 ABC-123"
                })
            },
            {
                "propertyName": "Score",
                "displayName": {"zh": "分数", "en": "Score"},
                "dataType": "Integer",
                "isRequired": False,
                # 范围: 0 - 100
                "validationRules": json.dumps({
                    "min": 0,
                    "max": 100,
                    "rangeMessage": "分数必须在0到100之间"
                })
            },
            {
                "propertyName": "Description",
                "displayName": {"zh": "描述", "en": "Description"},
                "dataType": "Text",
                "length": 200,
                "isRequired": True # 必填
            }
        ]
    }
    
    resp = requests.post(
        f"{API_BASE}/api/entity-definitions",
        json=create_payload,
        headers=api_helper.get_headers()
    )
    assert resp.status_code == 201, f"创建实体失败: {resp.text}"
    entity_id = resp.json()["data"]["id"]
    
    # 发布实体
    requests.post(
        f"{API_BASE}/api/entity-definitions/{entity_id}/publish",
        json={},
        headers=api_helper.get_headers()
    )
    
    # 等待动态接口就绪
    time.sleep(2) 
    
    endpoint = f"{API_BASE}/api/dynamic-entities/{full_name}"
    headers = api_helper.get_headers()

    # 3. 验证必填校验 (Required)
    # 场景: 缺少 Description
    invalid_payload_required = {
        "Code": "ABC-123",
        "Score": 80
        # Description 缺失
    }
    resp_req = requests.post(endpoint, json=invalid_payload_required, headers=headers)
    assert resp_req.status_code == 400, f"应拦截必填缺失: {resp_req.text}"
    assert "VALIDATION_FAILED" in resp_req.text, "应返回验证失败错误码"

    # 4. 验证正则校验 (Regex)
    # 场景: Code 格式错误
    invalid_payload_regex = {
        "Code": "wrong-format",
        "Score": 80,
        "Description": "Test"
    }
    resp_regex = requests.post(endpoint, json=invalid_payload_regex, headers=headers)
    assert resp_regex.status_code == 400, f"应拦截正则错误: {resp_regex.text}"
    assert "VALIDATION_FAILED" in resp_regex.text, "应返回验证失败错误码"
    
    # 5. 验证范围校验 (Range)
    # 场景: Score 超出最大值
    invalid_payload_range = {
        "Code": "ABC-123",
        "Score": 101,
        "Description": "Test"
    }
    resp_range = requests.post(endpoint, json=invalid_payload_range, headers=headers)
    assert resp_range.status_code == 400, f"应拦截范围错误: {resp_range.text}"
    assert "VALIDATION_FAILED" in resp_range.text, "应返回验证失败错误码"
    
    # 6. 验证正常创建 (Success)
    valid_payload = {
        "Code": "ABC-123",
        "Score": 99,
        "Description": "Valid Data"
    }
    resp_valid = requests.post(endpoint, json=valid_payload, headers=headers)
    assert resp_valid.status_code == 201, f"正常创建失败: {resp_valid.text}"
    
    # 清理
    requests.delete(f"{API_BASE}/api/entity-definitions/{entity_id}", headers=headers)
