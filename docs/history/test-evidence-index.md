# 测试存证文件索引

> **文档类型**: 测试存证索引  
> **创建日期**: 2026-01-15  
> **目的**: 为`docs/history/test-results/`下的所有截图和视频文件提供可读索引

---

## 存证目录结构

```
docs/history/test-results/
└── PC-001/              # 测试批次ID
    ├── 20260114_220224/ # 执行时间戳 (2026-01-14 22:02:24)
    │   ├── screenshots/ # 测试截图
    │   └── videos/      # 浏览器录屏
    ├── 20260114_230139/
    ├── 20260114_230542/
    ├── 20260114_230921/
    ├── 20260114_232107/
    └── 20260115_011421/ # 最新执行 (2026-01-15 01:14:21)
```

---

## 文件命名规范

### 1. 测试失败截图
**格式**: `FAILURE_<test_name>[<browser>].png`

**示例**:
- `FAILURE_test_auth_002_login_success[chromium].png` - Auth登录成功测试失败截图
- `FAILURE_test_batch2_003_runtime_renders_tabbox_and_number_input[chromium].png` - Form渲染测试失败

### 2. 测试步骤截图  
**格式**: `TC-<module>-<id>-<step>-<description>.png`

**示例**:
- `TC-AUTH-001-A1-setup-page.png` - 认证模块，测试001，步骤A1，初始设置页面
- `TC-AUTH-002-A9-dashboard.png` - 认证模块，测试002，步骤A9，Dashboard页面

### 3. 调试截图
**格式**: `DEBUG-<scenario>.png`

---

## 索引表（按测试模块分类）

### Authentication (认证模块) - Run: 20260114_220224

| 文件名 | 测试用例 | 用途说明 |
|--------|---------|---------|
| `FAILURE_test_auth_002_login_success[chromium].png` | TC-AUTH-002 | **已知回归**: 登录成功测试失败（UI按钮不响应） |
| `TC-AUTH-002-A1-login-form.png` | TC-AUTH-002 | 步骤A1: 登录表单 |
| `DEBUG-login-after-click.png` | - | 调试: 点击登录按钮后状态 |

### Form Design (表单设计模块) - Run: 20260114_220224

| 文件名 | 测试用例 | 用途说明 |
|--------|---------|---------|
| `FAILURE_test_batch2_003_runtime_renders_tabbox_and_number_input[chromium].png` | TC-FORM-B2-003 | **已修复** (commit 8aef1987): 运行时JsonValueKind bug |

---

## 维护规则

1. **每次E2E执行后**，更新本索引文档，添加新的run时间戳条目
2. **失败截图**必须在索引表中注明失败原因和后续修复commit
3. **录屏文件**建议只保留最近3次执行，旧文件移至归档目录

---

## 相关文档
- [STD-06: 集成测试规范](file:///c:/workspace/bobcrm/docs/process/STD-06-集成测试规范.md)
- [TEST-05: 实时质量大屏](file:///c:/workspace/bobcrm/docs/history/TEST-05-v1.0-实时质量大屏.md)
