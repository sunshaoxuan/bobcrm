# v0.9.x TASK-01: 实体关系建模（Lookup）

- [x] `FieldMetadata` 增加 `LookupEntityName` / `LookupDisplayField` / `ForeignKeyAction`
- [x] API DTO 同步暴露 Lookup 配置（Create/Update/Response）
- [x] `PostgreSQLDDLGenerator` 生成 `FK_[Source]_[Target]` 外键约束并支持删除行为
- [x] 发布前校验 Lookup 引用实体存在且可发布（Draft 不允许）
- [x] 单元测试覆盖 DDL 分支与异常场景

