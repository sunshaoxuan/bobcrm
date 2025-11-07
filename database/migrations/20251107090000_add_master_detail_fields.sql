-- =============================================================================
-- 迁移脚本：为 EntityDefinition 表添加主子表配置字段
-- 版本：20251107090000
-- 说明：添加主子表结构支持和实体锁定机制
-- =============================================================================

-- 1. 添加主子表结构配置字段
ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "ParentEntityId" uuid NULL;

ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "ParentEntityName" varchar(100) NULL;

ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "ParentForeignKeyField" varchar(100) NULL;

ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "ParentCollectionProperty" varchar(100) NULL;

ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "CascadeDeleteBehavior" varchar(20) NOT NULL DEFAULT 'NoAction';

ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "AutoCascadeSave" boolean NOT NULL DEFAULT true;

-- 2. 添加实体锁定字段（模板引用时锁定）
ALTER TABLE "EntityDefinitions"
ADD COLUMN IF NOT EXISTS "IsLocked" boolean NOT NULL DEFAULT false;

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS "IX_EntityDefinitions_ParentEntityId"
ON "EntityDefinitions" ("ParentEntityId");

-- 4. 创建外键约束（自引用）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_EntityDefinitions_EntityDefinitions_ParentEntityId'
    ) THEN
        ALTER TABLE "EntityDefinitions"
        ADD CONSTRAINT "FK_EntityDefinitions_EntityDefinitions_ParentEntityId"
        FOREIGN KEY ("ParentEntityId")
        REFERENCES "EntityDefinitions" ("Id")
        ON DELETE RESTRICT;
    END IF;
END$$;

-- 5. 添加字段注释
COMMENT ON COLUMN "EntityDefinitions"."ParentEntityId" IS '父实体ID（用于主子表结构）';
COMMENT ON COLUMN "EntityDefinitions"."ParentEntityName" IS '父实体名称（冗余字段，便于查询）';
COMMENT ON COLUMN "EntityDefinitions"."ParentForeignKeyField" IS '父实体外键字段名（如OrderId）';
COMMENT ON COLUMN "EntityDefinitions"."ParentCollectionProperty" IS '在父实体中的集合属性名（如OrderLines）';
COMMENT ON COLUMN "EntityDefinitions"."CascadeDeleteBehavior" IS '级联删除行为：NoAction、Cascade、SetNull、Restrict';
COMMENT ON COLUMN "EntityDefinitions"."AutoCascadeSave" IS '是否自动级联保存（保存主表时自动保存子表）';
COMMENT ON COLUMN "EntityDefinitions"."IsLocked" IS '是否已被锁定（模板引用后不可修改实体类型和名称）';

-- =============================================================================
-- 回滚脚本（如需回滚，请执行以下SQL）
-- =============================================================================
/*
-- 删除外键约束
ALTER TABLE "EntityDefinitions"
DROP CONSTRAINT IF EXISTS "FK_EntityDefinitions_EntityDefinitions_ParentEntityId";

-- 删除索引
DROP INDEX IF EXISTS "IX_EntityDefinitions_ParentEntityId";

-- 删除列
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "IsLocked";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "AutoCascadeSave";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "CascadeDeleteBehavior";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "ParentCollectionProperty";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "ParentForeignKeyField";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "ParentEntityName";
ALTER TABLE "EntityDefinitions" DROP COLUMN IF EXISTS "ParentEntityId";
*/
