# 客户系统开发任务与接口文档（按代码重编）

> 本清单基于当前仓库代码与测试用例梳理，作为里程碑与完成度对照。用勾选项统计完成率；“接口”小节链接到详细契约。

## 一、基础平台与鉴权
- [x] 技术栈选型：.NET 8 + Blazor Server + Ant Design Blazor
- [x] 身份认证：登录/注册/注销/刷新/会话校验（JWT Bearer）
- [x] 授权策略：受保护端点与角色/权限检查
- [x] 用户偏好：主题/语言/自定义颜色 API（UserPreferences）
- [x] Docker 化 PostgreSQL（compose）
- [ ] 外部登录/SSO（可选）

接口（详见：docs/API-01-接口文档.md）
- POST /api/auth/login、/refresh、/logout、/register、GET /session
- GET/PUT /api/user/preferences

## 二、客户域与访问控制
- [x] 客户 CRUD（Code/Name/Version/ExtData）
- [x] 客户访问授权（CustomerAccess：GET/POST 用户是否可编辑）
- [ ] 客户批量导入/导出（CSV/Excel）

接口（详见：docs/API-01-接口文档.md）
- GET/POST/PUT/DELETE /api/customers、GET /api/customers/{id}
- GET/POST /api/customers/{id}/access

## 三、实体元数据与模板
- [x] 实体元数据自动注册（AutoRegisterEntityMetadataAsync）
- [x] 实体定义端点（公共访问）：获取全部、按完整类型名查询
- [x] 表单模板：模板端点（模板列表/保存/应用）
- [ ] 模板版本化与发布审批流程

接口（详见：docs/ARCH-02-实体元数据自动注册机制.md、docs/API-01-接口文档.md）

## 四、字段与布局
- [x] 字段定义查询与标签统计（/api/fields、/api/fields/tags）
- [x] 布局读取/保存（scope=user|default|effective）
- [x] 布局自动生成（按标签 flow/free，支持选择持久化）
- [x] 字段动作：mailto、RDP（端点与服务 + 测试）
- [ ] 字段动作：文件/链接（file:// 上传/下载/安全校验）
- [ ] 文件上传控件与存储抽象（IFileStorageService）
- [ ] 图片/地图等富控件（Image/ImageArray/Map）

接口（详见：docs/API-01-接口文档.md）

## 五、UI 层（Calm Design Language）
- [x] 设计 Tokens：design-tokens.css（颜色/半径/阴影/动效…）
- [x] 主题提供器：ThemeState/ThemeInitializer（light/dark）
- [x] Ant Design 覆盖样式（ant-overrides.css）
- [x] Theme Playground（/theme-playground）
- [x] App Shell/Header/Sider（LayoutState/InteractionState/GlobalOverlayHost）
- [x] 关键页面：Customers、CustomerNew、FormDesigner、Templates、EntityDefinitions、EntityDefinitionEdit、Login/Register/Activate/Profile/Settings
- [x] 实体选择器/元数据树（初版）
- [ ] 设计器交互增强：拖拽吸附/对齐线、撤销/重做
- [ ] 无障碍与键盘导航完善（focus ring/Tab 次序）
- [ ] 响应式细节收口（sm/md/lg 基线对齐）

参考文档：
- UI 规范：docs/UI-01-UIUE设计说明书.md
- 阶段计划：docs/UI-02-阶段1-实现计划.md
- 差距与验证：docs/UI-00-阶段0-差距报告.md、docs/UI-03-阶段2-布局验证记录.md

## 六、多语言（I18N）
- [x] 多语言资源端点 + 版本/ETag 缓存（服务端）
- [x] 客户端 I18nService + 本地缓存（localStorage）
- [ ] 实体字段本地化落库（Customer/ProductLocalization 全量覆盖）
- [ ] 导入导出（CSV/JSON）与可视化对账

参考文档：docs/I18N-01-多语机制设计文档.md

## 七、测试与质量
- [x] 轻量集成测试（xUnit + WebApplicationFactory）
- [x] 覆盖率报告（reportgenerator），覆盖率≈90%（以最近报告为准）
- [x] 关键域用例：鉴权/客户/字段/布局/多语/权限（>100 测试）
- [ ] 性能冒烟测试与基线
- [ ] 错误码与异常契约统一（Result<T>/ProblemDetails）

参考文档：docs/TEST-02-测试覆盖率报告.md

## 八、运维与可观测
- [ ] 健康检查/就绪探针（/healthz、/ready）
- [ ] 结构化日志与可观测事件（OpenTelemetry/Serilog）
- [ ] 指标导出（Prometheus）

## 九、文档与流程
- [x] 统一文档命名与索引（PROC-00）
- [x] PR 检查清单（PROC-01）
- [x] 文档同步规范（PROC-02）
- [x] 文档代码差距审计报告（PROC-04）
- [ ] 接口版本化与变更策略（待补充）

---

## 统计（使用勾选项计算）
- 完成率：按已勾选/总任务项统计
- 偏差率：对照 docs/PROC-04-文档代码差距审计报告.md 的“差距条目/总对齐项”
- UI 完成率：统计 docs/UI-02-阶段1-实现计划.md 勾选项
- 覆盖率：见 docs/TEST-02-测试覆盖率报告.md

## 附：接口总览
- 详见：docs/API-01-接口文档.md（鉴权、客户、字段、布局、多语、偏好等）

## 变更记录
- 2025-11-08：按代码重编任务清单，标注完成项；对齐 UI/测试/多语/文档流程。
