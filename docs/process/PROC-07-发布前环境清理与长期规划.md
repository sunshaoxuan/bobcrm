# PROC-07: 发布前环境清理与长期规划 (Post-Batch 4)

## 1. 立即执行的清理作业 (Immediate Cleanup)

针对演期 E2E 和 性能测试产生的“残留数据”，执行以下清理以保证生产/演示环境的纯净。

### 1.1 数据库“僵尸”数据清理
*   **清理逻辑**: 删除所有以 `Test` 或 `Perf` 开头的动态实体定义及其物理表。
*   **清理脚本建议**:
    ```sql
    -- 1. 删除表
    DO $$ DECLARE
        r RECORD;
    BEGIN
        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND (tablename LIKE 'Test%' OR tablename LIKE 'Perf%')) LOOP
            EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
        END LOOP;
    END $$;

    -- 2. 删除元数据
    DELETE FROM "FieldMetadatas" WHERE "EntityDefinitionId" IN (SELECT "Id" FROM "EntityDefinitions" WHERE "EntityName" LIKE 'Test%' OR "EntityName" LIKE 'Perf%');
    DELETE FROM "EntityDefinitions" WHERE "EntityName" LIKE 'Test%' OR "EntityName" LIKE 'Perf%';
    ```

### 1.2 工程目录清理
*   删除根目录下的临时日志文件 (`*.log`)。
*   确认 `GUIDE-99-最终用户操作手册.md` 已移至 `docs/guides/` 下。

---

## 2. 后续工作规划 (Future Roadmap)

### Phase 6: 企业级特性增强 (Enterprise Readiness)
*   **数据隔离**: 实现真正的多租户 (Multi-tenancy) 逻辑隔离，基于 `OrganizationId` 进行全局查询拦截。
*   **高级设计器**: 增加“工作流 (Workflow) 设计器”，支持简单的审批链配置。
*   **深度集成**: 支持 Webhook 推送，允许动态实体变更时通知第三方系统。

### Phase 7: 性能与可扩展性 (Scalability)
*   **分布式缓存**: 将现有的内存 I18n 和元数据缓存迁移至 Redis。
*   **水平扩展**: 优化 `AssemblyLoadContext` 的分布式同步，确保多实例部署时代码加载的一致性。

---

## 3. 评审结论 (Conclusion)
目前系统已具备 **v1.0-RC1 (Release Candidate)** 的发布水平。核心引擎稳定，性能达标，文档基本完备。建议在正式发布前进行一次全站的样美化 (UI Polish)。
