# 文档索引（按类型组织）

- 文件按 **类型/职责** 存放：`design/`（架构与产品设计）、`guides/`（手册/指南）、`reference/`（接口/规范）、`history/`（差距与修复记录）、`plans/`（计划）、`process/`（流程）、`examples/`（示例）、`test-cases/`（测试用例库）、`prompts/`（AI审查Prompt）。
- 命名仍遵循 `分类-编号-标题.md` 规则，便于快速检索。
- 若需历史版本，可通过 Git 查看对应文件的变更记录。

## 1. 设计文档（design/）
- [ARCH-01-实体自定义与发布系统设计文档](design/ARCH-01-实体自定义与发布系统设计文档.md)
- [ARCH-02-实体元数据自动注册机制](design/ARCH-02-实体元数据自动注册机制.md)
- [ARCH-10-AggVO系统指南](design/ARCH-10-AggVO系统指南.md)
- [ARCH-11-动态实体指南](design/ARCH-11-动态实体指南.md)
- [ARCH-12-对象存储（MinIO-S3）使用说明](design/ARCH-12-对象存储（MinIO-S3）使用说明.md)
- [ARCH-13-系统与用户设置模型](design/ARCH-13-系统与用户设置模型.md)
- [ARCH-14-数据结构自动对齐系统设计文档](design/ARCH-14-数据结构自动对齐系统设计文档.md)
- [ARCH-20-实体定义管理设计](design/ARCH-20-实体定义管理设计.md)
- [ARCH-21-组织与权限体系设计](design/ARCH-21-组织与权限体系设计.md)
- [ARCH-22-标准实体模板化与权限联动设计](design/ARCH-22-标准实体模板化与权限联动设计.md)
- [ARCH-23-功能体系规划](design/ARCH-23-功能体系规划.md)
- [ARCH-24-紧凑型顶部菜单导航设计](design/ARCH-24-紧凑型顶部菜单导航设计.md)
- [ARCH-24-紧凑型顶部菜单导航-实施计划](design/ARCH-24-紧凑型顶部菜单导航-实施计划.md)
- [ARCH-25-通用主从模式设计](design/ARCH-25-通用主从模式设计.md)
- [ARCH-26-动态枚举系统设计](design/ARCH-26-动态枚举系统设计.md)
- [ARCH-26-动态枚举系统设计](design/ARCH-26-动态枚举系统设计.md)
- [PROD-01-客户信息管理系统设计文档（含 docx）](design/PROD-01-客户信息管理系统设计文档.md)
- [PROD-02-客户系统开发任务与接口文档](design/PROD-02-客户系统开发任务与接口文档.md)
- [UI-01-UIUE设计说明书](design/UI-01-UIUE设计说明书.md)
- [UI-02-阶段1-实现计划](design/UI-02-阶段1-实现计划.md)
- [UI-03-阶段2-布局验证记录](design/UI-03-阶段2-布局验证记录.md)
- [UI-04-实体定义画面改版说明（归档，指向 ARCH-20/21）](design/UI-04-实体定义画面改版说明.md)

## 2. 指南与手册（guides/）
- [GUIDE-01-部署指南](guides/GUIDE-01-部署指南.md)
- [GUIDE-11-实体定义与动态实体操作指南](guides/GUIDE-11-实体定义与动态实体操作指南.md)
- [GUIDE-10-Roslyn环境配置](guides/GUIDE-10-Roslyn环境配置.md)
- [GUIDE-05-动态枚举系统使用指南](guides/GUIDE-05-动态枚举系统使用指南.md)
- [GUIDE-06-菜单编辑器使用指南](guides/GUIDE-06-菜单编辑器使用指南.md)
- [GUIDE-08-中优先级I18n指南](guides/GUIDE-08-中优先级I18n指南.md)
- [I18N-01-多语机制设计文档](guides/I18N-01-多语机制设计文档.md)
- [I18N-02-元数据多语机制设计文档](guides/I18N-02-元数据多语机制设计文档.md)
- [OPS-01-容器问题-设计说明](guides/OPS-01-容器问题-设计说明.md)
- [OPS-02-容器问题-修复历史](guides/OPS-02-容器问题-修复历史.md)
- [OPS-03-容器问题-故障排查](guides/OPS-03-容器问题-故障排查.md)
- [TEST-10-测试综述](guides/TEST-10-测试综述.md)
- [TEST-01-测试指南](guides/TEST-01-测试指南.md)
- [TEST-02-测试覆盖率报告](guides/TEST-02-测试覆盖率报告.md)
- [TEST-03-实体发布与对齐测试覆盖报告](guides/TEST-03-实体发布与对齐测试覆盖报告.md)
- [TEST-04-菜单工作流集成测试覆盖](guides/TEST-04-菜单工作流集成测试覆盖.md)

## 3. 参考（reference/）
- [API-01-接口文档](reference/API-01-接口文档.md)
- [API-02-API契约测试覆盖率报告](reference/API-02-API契约测试覆盖率报告.md)
- [API-03-端点分析说明](reference/API-03-端点分析说明.md)
- [API-04-端点目录](reference/API-04-端点目录.md)
- [API-05-端点风险与改进建议](reference/API-05-端点风险与改进建议.md)
- [API-06-端点摘要表](reference/API-06-端点摘要表.md)

## 4. 审计与评审报告（history/）
> 一次性的审计、评审、迁移记录和已完成的计划

- [AUDIT-01-文档代码差距审计报告-2025-11](history/AUDIT-01-文档代码差距审计报告-2025-11.md)
- [AUDIT-02-文档代码合规性问题清单](history/AUDIT-02-文档代码合规性问题清单.md)
- [AUDIT-03-项目现状分析报告](history/AUDIT-03-项目现状分析报告.md)
- [AUDIT-04-ARCH-30-动态实体多语研究报告](history/AUDIT-04-ARCH-30-动态实体多语研究报告.md)
- [ARCH-30-任务执行记录与评审材料](history/ARCH-30/README.md)
- [MIGRATION-01-系统实体模板化迁移记录](history/MIGRATION-01-系统实体模板化迁移记录.md)
- [PROC-06-v0.6.0-开发计划](history/PROC-06-v0.6.0-开发计划.md) (已完成)
- [UI-07-阶段0-差距报告](history/UI-07-阶段0-差距报告.md)
- [GAP-01-架构与功能差距记录](history/GAP-01-架构与功能差距记录.md)
- [TMP-01-模板设计器进度跟踪](history/TMP-01-模板设计器进度跟踪.md)

## 5. 评审记录（reviews/）

> 代码评审、版本评审、验证报告等（命名与编号规则见 `docs/process/STD-02-文档编写规范.md`）。

- 入口：`reviews/README.md`
- [REVIEW-08-v0.7.0-项目审计](reviews/REVIEW-08-v0.7.0-项目审计.md)
- [REVIEW-09-代码评审与改进清单-2025-11](reviews/REVIEW-09-代码评审与改进清单-2025-11.md)
- [REVIEW-10-多语言功能代码评审-2025-11](reviews/REVIEW-10-多语言功能代码评审-2025-11.md)
- [REVIEW-12-Comprehensive-Code-Review-2025-12](reviews/REVIEW-12-Comprehensive-Code-Review-2025-12.md)
- [REVIEW-13-Code-Quality-Review-2025-12](reviews/REVIEW-13-Code-Quality-Review-2025-12.md)
- [REVIEW-14-代码质量评审-2025-12](reviews/REVIEW-14-代码质量评审-2025-12.md)

## 6. 开发计划（plans/）
> 详细的版本开发计划，包含任务拆解、验收标准和实施细节

- [PLAN-01-v0.7.0-菜单导航完善](plans/PLAN-01-v0.7.0-菜单导航完善.md)
- [PLAN-18-代码质量与覆盖率总计划](plans/PLAN-18-代码质量与覆盖率总计划.md)

## 7. 流程与规范（process/）
> 可重复使用的流程模板和规范

- 本页：`PROC-00-文档索引.md`
- [PROC-01-PR检查清单](process/PROC-01-PR检查清单.md)
- [PROC-02-文档同步规范](process/PROC-02-文档同步规范.md)
- [STD-01-开发质量标准](process/STD-01-开发质量标准.md)
- [STD-02-文档编写规范](process/STD-02-文档编写规范.md)
- [STD-03-通用编程规范](process/STD-03-通用编程规范.md)
- [STD-06-集成测试规范](process/STD-06-集成测试规范.md)
- [STD-08-Git协作与提交规范](process/STD-08-Git协作与提交规范.md)

## 8. 示例（examples/）
- [PROMPT-01-全面代码审查](prompts/PROMPT-01-全面代码审查.md) - 完整的代码审查执行指令
- [TEMPLATE-01-代码审查报告模板](prompts/TEMPLATE-01-代码审查报告模板.md) - 审查报告输出格式
- [TEMPLATE-02-改进计划模板](prompts/TEMPLATE-02-改进计划模板.md) - 改进计划输出格式

> **提示**：如需新增文档，请先确认所属类型，再放入对应目录，并在本索引补充条目。
