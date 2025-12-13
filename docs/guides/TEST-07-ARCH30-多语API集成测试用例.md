# TEST-07: ARCH-30 多语API集成测试用例

**版本**: 1.0
**创建日期**: 2025-12-13
**目标**: 覆盖ARCH-30系统级多语API架构优化的完整业务流程
**执行方式**: AI自动化测试

---

## 测试概述

### 测试范围
- 高频API多语切换（用户菜单、导航菜单、实体列表）
- 中频API多语切换（实体定义、枚举、实体域、功能节点）
- 低频API多语切换（动态实体查询）
- 向后兼容性验证
- 前端多语渲染验证

### 测试环境
- 后端: http://localhost:5200
- 前端: http://localhost:3000
- 默认用户: admin / Admin@12345
- 支持语言: zh（中文）、ja（日语）、en（英语）

---

## 场景1: 用户登录与菜单多语切换

### 1.1 场景描述
验证用户登录后，功能菜单能够根据语言参数正确显示对应语言的菜单名称。

### 1.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 用户名 | admin |
| 密码 | Admin@12345 |
| 测试语言 | zh, ja, en |
| 预期菜单项 | "系统设置" / "システム設定" / "System Settings" |

### 1.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用登录API获取Token | 返回200，含accessToken | ❌ |
| 2 | 调用 `GET /api/access/functions/me` (无lang参数) | 返回多语字典格式 `displayNameTranslations` | ❌ |
| 3 | 调用 `GET /api/access/functions/me?lang=zh` | 返回单语格式 `displayName: "系统设置"` | ❌ |
| 4 | 调用 `GET /api/access/functions/me?lang=ja` | 返回单语格式 `displayName: "システム設定"` | ❌ |
| 5 | 调用 `GET /api/access/functions/me?lang=en` | 返回单语格式 `displayName: "System Settings"` | ❌ |
| 6 | 验证响应体积: 单语模式 < 多语模式 | 单语响应体积减少≥10% | ❌ |

### 1.4 验收标准
- [ ] 无lang参数时返回完整多语字典
- [ ] 有lang参数时仅返回对应语言的单语字符串
- [ ] 树形结构所有子节点语言一致

---

## 场景2: 导航菜单多语绑定

### 2.1 场景描述
验证导航菜单绑定接口支持多语参数，前端能正确渲染对应语言的菜单。

### 2.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 端点 | `/api/templates/menu-bindings` |
| 查询参数 | `usageType=Detail` |
| 测试语言 | zh, ja |

### 2.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 登录获取Token | 成功 | ❌ |
| 2 | 调用 `GET /api/templates/menu-bindings?usageType=Detail` (无lang) | 返回菜单绑定列表，含多语字典 | ❌ |
| 3 | 调用 `GET /api/templates/menu-bindings?usageType=Detail&lang=zh` | 菜单名称为中文单语 | ❌ |
| 4 | 调用 `GET /api/templates/menu-bindings?usageType=Detail&lang=ja` | 菜单名称为日语单语 | ❌ |
| 5 | 打开前端首页，设置语言为中文 | 导航菜单显示中文 | ✅ |
| 6 | 切换语言为日语 | 导航菜单显示日语 | ✅ |

### 2.4 验收标准
- [ ] API支持lang参数
- [ ] 前端菜单正确响应语言切换

---

## 场景3: 实体列表多语显示

### 3.1 场景描述
验证实体列表接口的多语支持，以及前端实体选择器的多语渲染。

### 3.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 端点 | `/api/entities`, `/api/entities/all` |
| 预期实体 | Customer (客户/顧客/Customer) |

### 3.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/entities` (无lang) | 返回实体列表，displayNameTranslations为字典 | ❌ |
| 2 | 调用 `GET /api/entities?lang=zh` | displayName为中文 "客户" | ❌ |
| 3 | 调用 `GET /api/entities?lang=ja` | displayName为日语 "顧客" | ❌ |
| 4 | 调用 `GET /api/entities/all?lang=en` | displayName为英文 "Customer" | ❌ |
| 5 | 打开前端实体管理页面(zh) | 实体列表显示中文名称 | ✅ |
| 6 | 切换语言为英文 | 实体列表显示英文名称 | ✅ |

### 3.4 验收标准
- [ ] 两个端点均支持lang参数
- [ ] 向后兼容：无lang时返回完整字典

---

## 场景4: 实体定义详情多语

### 4.1 场景描述
验证实体定义管理接口的字段级多语支持，包括字段显示名的三级优先级解析。

### 4.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试实体 | Customer |
| 接口字段 | Code (DisplayNameKey: LBL_FIELD_CODE) |
| 自定义字段 | 任意自定义字段 (DisplayName字典) |

### 4.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/entity-definitions` (无lang) | 返回实体定义列表，含多语字典 | ❌ |
| 2 | 调用 `GET /api/entity-definitions?lang=zh` | displayName为中文单语 | ❌ |
| 3 | 获取Customer实体ID | 记录entityId | ❌ |
| 4 | 调用 `GET /api/entity-definitions/{id}` (无lang) | 字段列表含displayNameTranslations | ❌ |
| 5 | 调用 `GET /api/entity-definitions/{id}?lang=zh` | 接口字段Code的displayName解析为"编码" | ❌ |
| 6 | 验证字段解析优先级 | DisplayNameKey > DisplayName字典 > PropertyName | ❌ |
| 7 | 打开前端实体定义编辑页(zh) | 字段标签显示中文 | ✅ |

### 4.4 验收标准
- [ ] 字段级多语正确解析
- [ ] 三级优先级逻辑正确
- [ ] 前端字段标签正确显示

---

## 场景5: 枚举定义多语

### 5.1 场景描述
验证枚举定义及选项的多语支持。

### 5.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试枚举 | view_state |
| 测试选项 | List, DetailView, DetailEdit |

### 5.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/enums` (无lang) | 返回枚举列表，displayNameTranslations为字典 | ❌ |
| 2 | 调用 `GET /api/enums?lang=zh` | displayName为中文单语 | ❌ |
| 3 | 调用 `GET /api/enums/by-code/view_state?lang=ja` | 返回日语显示名 | ❌ |
| 4 | 获取view_state枚举ID | 记录enumId | ❌ |
| 5 | 调用 `GET /api/enums/{id}/options?lang=zh` | 选项displayName为中文 | ❌ |
| 6 | 调用 `GET /api/enums/{id}/options?lang=en` | 选项displayName为英文 | ❌ |
| 7 | 验证Accept-Language被忽略 | 无lang时，即使有Accept-Language也返回多语字典 | ❌ |

### 5.4 验收标准
- [ ] 枚举定义支持lang参数
- [ ] 枚举选项支持lang参数
- [ ] 向后兼容：忽略Accept-Language头

---

## 场景6: 实体域多语

### 6.1 场景描述
验证实体域（Domain）的多语支持。

### 6.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 端点 | `/api/entity-domains` |
| 测试域 | CRM核心 / CRMコア / CRM Core |

### 6.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/entity-domains` (无lang) | 返回域列表，nameTranslations为字典 | ❌ |
| 2 | 调用 `GET /api/entity-domains?lang=zh` | name为中文单语 | ❌ |
| 3 | 获取域ID | 记录domainId | ❌ |
| 4 | 调用 `GET /api/entity-domains/{id}?lang=ja` | name为日语单语 | ❌ |
| 5 | 打开前端域管理页面 | 域名称正确显示 | ✅ |

### 6.4 验收标准
- [ ] 域列表和详情均支持lang参数

---

## 场景7: 功能节点管理多语

### 7.1 场景描述
验证功能节点CRUD操作的多语支持，包括创建和更新后的响应。

### 7.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 新建功能代码 | TEST.FUNC.MULTILANG.{uuid} |
| 显示名 | {"zh": "测试功能", "ja": "テスト機能", "en": "Test Function"} |

### 7.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/access/functions` (无lang) | 返回功能树，displayNameTranslations为字典 | ❌ |
| 2 | 调用 `GET /api/access/functions?lang=zh` | displayName为中文单语 | ❌ |
| 3 | 调用 `GET /api/access/functions/manage?lang=ja` | displayName为日语单语 | ❌ |
| 4 | 调用 `POST /api/access/functions?lang=zh` 创建新功能 | 返回的DTO中displayName为中文 | ❌ |
| 5 | 调用 `PUT /api/access/functions/{id}?lang=en` 更新功能 | 返回的DTO中displayName为英文 | ❌ |
| 6 | 验证树形结构语言一致性 | 所有子节点使用相同语言 | ❌ |
| 7 | 打开前端功能管理页面 | 功能树正确显示 | ✅ |

### 7.4 验收标准
- [ ] GET/POST/PUT均支持lang参数
- [ ] 树形结构语言一致

---

## 场景8: 动态实体查询多语（核心场景）

### 8.1 场景描述
验证动态实体查询的meta.fields元数据支持，这是ARCH-30的核心创新点。

### 8.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试实体 | 任意已发布的自定义实体 |
| 接口字段 | Code (DisplayNameKey: LBL_FIELD_CODE) |
| 自定义字段 | 任意自定义字段 |

### 8.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 确认测试实体已发布 | Status = Published | ❌ |
| 2 | 调用 `POST /api/dynamic-entities/{fullTypeName}/query` (无lang) | 返回 `{meta: {fields: [...]}, data: [...], total: n}` | ❌ |
| 3 | 验证meta.fields结构(无lang) | 接口字段有displayNameKey，自定义字段有displayNameTranslations | ❌ |
| 4 | 调用查询接口 `?lang=zh` | meta.fields中所有字段有displayName(中文单语) | ❌ |
| 5 | 调用查询接口 `?lang=ja` | meta.fields中所有字段有displayName(日语单语) | ❌ |
| 6 | 验证接口字段Code | lang=zh时displayName="编码"，来自LBL_FIELD_CODE | ❌ |
| 7 | 验证自定义字段 | displayName来自DisplayName字典 | ❌ |
| 8 | 验证Accept-Language被忽略 | 无lang时返回多语模式 | ❌ |

### 8.4 验收标准
- [ ] 查询响应包含meta.fields
- [ ] 双模式正确：单语/多语
- [ ] 字段显示名三级优先级正确

---

## 场景9: 动态实体详情多语

### 9.1 场景描述
验证动态实体详情接口的includeMeta参数和多语支持。

### 9.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试实体 | 同场景8 |
| 测试记录ID | 1 (或已存在的记录) |

### 9.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `GET /api/dynamic-entities/{fullTypeName}/1` (默认) | 返回原始实体对象，无meta字段 | ❌ |
| 2 | 调用 `GET /api/dynamic-entities/{fullTypeName}/1?includeMeta=false` | 返回原始实体对象，无meta字段 | ❌ |
| 3 | 调用 `GET /api/dynamic-entities/{fullTypeName}/1?includeMeta=true` (无lang) | 返回 `{meta: {fields: [...]}, data: {...}}` | ❌ |
| 4 | 验证meta.fields(无lang) | 多语模式，有displayNameKey或displayNameTranslations | ❌ |
| 5 | 调用 `?includeMeta=true&lang=zh` | meta.fields中displayName为中文 | ❌ |
| 6 | 验证向后兼容 | 默认includeMeta=false，不破坏现有客户端 | ❌ |

### 9.4 验收标准
- [ ] includeMeta参数工作正常
- [ ] 默认不返回meta（向后兼容）
- [ ] 与lang参数组合工作正常

---

## 场景10: 字段元数据缓存验证

### 10.1 场景描述
验证FieldMetadataCache的缓存机制是否正常工作。

### 10.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试实体 | 同场景8 |
| 缓存过期 | 30分钟滑动过期 |

### 10.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 首次调用动态实体查询 | 记录响应时间T1 | ❌ |
| 2 | 立即再次调用相同查询 | 响应时间T2 < T1（缓存命中） | ❌ |
| 3 | 使用不同lang参数调用 | 创建新的缓存条目 | ❌ |
| 4 | 修改实体定义（添加字段） | 触发缓存失效 | ❌ |
| 5 | 再次查询 | 返回更新后的字段列表 | ❌ |

### 10.4 验收标准
- [ ] 缓存命中时响应更快
- [ ] 实体修改后缓存正确失效

---

## 场景11: 前端完整流程验证

### 11.1 场景描述
端到端验证前端页面在不同语言下的显示效果。

### 11.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 测试语言 | zh → ja → en |
| 测试页面 | 登录、首页、实体列表、实体详情、实体编辑 |

### 11.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 打开登录页(zh) | 登录表单显示中文 | ✅ `login-zh.png` |
| 2 | 登录成功进入首页 | 菜单显示中文 | ✅ `home-zh.png` |
| 3 | 进入客户列表页 | 列标题显示中文 | ✅ `list-zh.png` |
| 4 | 点击查看客户详情 | 字段标签显示中文 | ✅ `detail-zh.png` |
| 5 | 切换语言为日语 | 页面刷新 | ❌ |
| 6 | 验证首页菜单 | 菜单显示日语 | ✅ `home-ja.png` |
| 7 | 验证客户列表 | 列标题显示日语 | ✅ `list-ja.png` |
| 8 | 验证客户详情 | 字段标签显示日语 | ✅ `detail-ja.png` |
| 9 | 切换语言为英语 | 页面刷新 | ❌ |
| 10 | 验证关键页面 | 所有页面显示英文 | ✅ `home-en.png` |

### 11.4 验收标准
- [ ] 三种语言下页面正确显示
- [ ] 语言切换无报错
- [ ] 截图存档供对比

---

## 场景12: 向后兼容性验证

### 12.1 场景描述
验证新API对旧客户端的向后兼容性。

### 12.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 模拟旧客户端 | 不传lang参数，不处理新字段 |

### 12.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用所有改造端点（不传lang） | 全部返回200 | ❌ |
| 2 | 验证响应结构 | 保持原有字段，新增字段为可选 | ❌ |
| 3 | 验证displayNameTranslations | 无lang时存在，有lang时为null | ❌ |
| 4 | 验证动态实体详情默认行为 | includeMeta默认false，返回原始对象 | ❌ |
| 5 | 发送Accept-Language头（无lang参数） | 除3个高频端点外，均返回多语字典 | ❌ |

### 12.4 验收标准
- [ ] 所有端点向后兼容
- [ ] 旧客户端无需修改即可使用

---

## 场景13: 错误处理验证

### 13.1 场景描述
验证异常情况下的错误处理和多语错误消息。

### 13.2 测试数据
| 数据项 | 值 |
|--------|-----|
| 无效语言代码 | `lang=xx`, `lang=invalid` |
| 不存在的实体 | `fullTypeName=NonExistent.Entity` |

### 13.3 测试流程

| 步骤 | 操作 | 预期结果 | 截图 |
|------|------|----------|------|
| 1 | 调用 `?lang=xx`（无效语言） | 回退到默认语言或返回400 | ❌ |
| 2 | 调用 `?lang=` （空值） | 视为无lang，返回多语模式 | ❌ |
| 3 | 查询不存在的动态实体 | 返回404，错误消息根据Accept-Language | ❌ |
| 4 | 查询不存在的实体定义 | 返回404 | ❌ |

### 13.4 验收标准
- [ ] 无效参数优雅处理
- [ ] 错误消息国际化

---

## 执行清单

### API测试（无需截图）
- [ ] 场景1: 用户登录与菜单多语切换
- [ ] 场景2: 导航菜单多语绑定 (API部分)
- [ ] 场景3: 实体列表多语显示 (API部分)
- [ ] 场景4: 实体定义详情多语
- [ ] 场景5: 枚举定义多语
- [ ] 场景6: 实体域多语
- [ ] 场景7: 功能节点管理多语
- [ ] 场景8: 动态实体查询多语
- [ ] 场景9: 动态实体详情多语
- [ ] 场景10: 字段元数据缓存验证
- [ ] 场景12: 向后兼容性验证
- [ ] 场景13: 错误处理验证

### UI测试（需要截图）
- [ ] 场景2: 导航菜单多语绑定 (UI部分)
- [ ] 场景3: 实体列表多语显示 (UI部分)
- [ ] 场景11: 前端完整流程验证

### 预期截图清单
| 截图文件 | 描述 |
|----------|------|
| `login-zh.png` | 中文登录页 |
| `home-zh.png` | 中文首页菜单 |
| `list-zh.png` | 中文列表页 |
| `detail-zh.png` | 中文详情页 |
| `home-ja.png` | 日语首页菜单 |
| `list-ja.png` | 日语列表页 |
| `detail-ja.png` | 日语详情页 |
| `home-en.png` | 英语首页菜单 |

---

## 附录: 端点清单

| 端点 | 方法 | lang支持 | Accept-Language |
|------|------|----------|-----------------|
| `/api/access/functions/me` | GET | ✅ | ✅ |
| `/api/templates/menu-bindings` | GET | ✅ | ✅ |
| `/api/entities` | GET | ✅ | ✅ |
| `/api/entities/all` | GET | ✅ | ✅ |
| `/api/entity-definitions` | GET | ✅ | ❌ |
| `/api/entity-definitions/{id}` | GET | ✅ | ❌ |
| `/api/enums` | GET | ✅ | ❌ |
| `/api/enums/{id}` | GET | ✅ | ❌ |
| `/api/enums/by-code/{code}` | GET | ✅ | ❌ |
| `/api/enums/{id}/options` | GET | ✅ | ❌ |
| `/api/entity-domains` | GET | ✅ | ❌ |
| `/api/entity-domains/{id}` | GET | ✅ | ❌ |
| `/api/access/functions` | GET | ✅ | ❌ |
| `/api/access/functions/manage` | GET | ✅ | ❌ |
| `/api/access/functions` | POST | ✅ | ❌ |
| `/api/access/functions/{id}` | PUT | ✅ | ❌ |
| `/api/dynamic-entities/{type}/query` | POST | ✅ | ❌ |
| `/api/dynamic-entities/{type}/{id}` | GET | ✅ | ❌ |

---

**文档版本**: 1.0
**创建日期**: 2025-12-13
