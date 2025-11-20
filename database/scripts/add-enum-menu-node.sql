-- =====================================================
-- 添加枚举管理菜单节点
-- =====================================================
-- 作用: 在系统设置 > 实体管理 下添加"枚举定义"菜单项
-- 执行时机: 数据库初始化后手动执行一次
-- =====================================================

DO $$
DECLARE
    v_root_id uuid;
    v_sys_id uuid;
    v_entity_mgmt_id uuid;
    v_enum_menu_id uuid;
BEGIN
    -- 1. 获取根节点 APP.ROOT
    SELECT "Id" INTO v_root_id
    FROM "FunctionNodes"
    WHERE "Code" = 'APP.ROOT'
    LIMIT 1;

    IF v_root_id IS NULL THEN
        RAISE EXCEPTION 'Root node APP.ROOT not found. Please ensure database is initialized.';
    END IF;

    RAISE NOTICE 'Found root node: %', v_root_id;

    -- 2. 获取或创建 SYS 域节点
    SELECT "Id" INTO v_sys_id
    FROM "FunctionNodes"
    WHERE "Code" = 'SYS' AND "ParentId" = v_root_id
    LIMIT 1;

    IF v_sys_id IS NULL THEN
        -- 创建 SYS 域节点
        v_sys_id := gen_random_uuid();
        INSERT INTO "FunctionNodes" (
            "Id", "ParentId", "Code", "Name", "Icon",
            "IsMenu", "SortOrder", "CreatedAt", "UpdatedAt"
        )
        VALUES (
            v_sys_id,
            v_root_id,
            'SYS',
            'MENU_SYS',  -- 将通过 i18n 解析为"系统设置"
            'setting',
            true,
            1000,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created SYS domain node: %', v_sys_id;
    ELSE
        RAISE NOTICE 'Found SYS domain node: %', v_sys_id;
    END IF;

    -- 3. 获取或创建 SYS.ENTITY 模块节点（实体管理）
    SELECT "Id" INTO v_entity_mgmt_id
    FROM "FunctionNodes"
    WHERE "Code" = 'SYS.ENTITY' AND "ParentId" = v_sys_id
    LIMIT 1;

    IF v_entity_mgmt_id IS NULL THEN
        -- 创建实体管理模块节点
        v_entity_mgmt_id := gen_random_uuid();
        INSERT INTO "FunctionNodes" (
            "Id", "ParentId", "Code", "Name", "Icon",
            "IsMenu", "SortOrder", "CreatedAt", "UpdatedAt"
        )
        VALUES (
            v_entity_mgmt_id,
            v_sys_id,
            'SYS.ENTITY',
            'MENU_SYS_ENTITY',  -- i18n: "实体管理"
            'database',
            true,
            100,
            NOW(),
            NOW()
        );
        RAISE NOTICE 'Created entity management module: %', v_entity_mgmt_id;
    ELSE
        RAISE NOTICE 'Found entity management module: %', v_entity_mgmt_id;
    END IF;

    -- 4. 检查枚举菜单节点是否已存在
    SELECT "Id" INTO v_enum_menu_id
    FROM "FunctionNodes"
    WHERE "Code" = 'SYS.ENTITY.ENUM' AND "ParentId" = v_entity_mgmt_id
    LIMIT 1;

    IF v_enum_menu_id IS NOT NULL THEN
        RAISE NOTICE 'Enum menu node already exists: %, skipping creation', v_enum_menu_id;
    ELSE
        -- 5. 创建枚举定义菜单节点
        v_enum_menu_id := gen_random_uuid();
        INSERT INTO "FunctionNodes" (
            "Id",
            "ParentId",
            "Code",
            "Name",
            "Icon",
            "RouteTemplate",
            "IsMenu",
            "SortOrder",
            "CreatedAt",
            "UpdatedAt"
        )
        VALUES (
            v_enum_menu_id,
            v_entity_mgmt_id,
            'SYS.ENTITY.ENUM',
            'MENU_SYS_ENTITY_ENUM',  -- i18n: "枚举定义"
            'ordered-list',
            '/enums',
            true,
            20,
            NOW(),
            NOW()
        );

        RAISE NOTICE 'Successfully created enum menu node: %', v_enum_menu_id;
        RAISE NOTICE 'Menu path: 系统设置 > 实体管理 > 枚举定义';
        RAISE NOTICE 'Route: /enums';
    END IF;

    RAISE NOTICE '=== Enum menu node installation complete ===';

EXCEPTION WHEN OTHERS THEN
    RAISE EXCEPTION 'Error adding enum menu node: %', SQLERRM;
END $$;

-- 验证插入结果
SELECT
    fn."Code" as "FunctionCode",
    fn."Name" as "NameKey",
    fn."RouteTemplate" as "Route",
    fn."Icon",
    fn."SortOrder",
    parent."Code" as "ParentCode"
FROM "FunctionNodes" fn
LEFT JOIN "FunctionNodes" parent ON parent."Id" = fn."ParentId"
WHERE fn."Code" IN ('SYS', 'SYS.ENTITY', 'SYS.ENTITY.ENUM')
ORDER BY fn."SortOrder";
