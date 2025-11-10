using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 国际化资源同步器 - 确保数据库包含所有必需的国际化键
/// </summary>
public class I18nResourceSynchronizer
{
    private readonly AppDbContext _db;
    private readonly ILogger<I18nResourceSynchronizer> _logger;

    public I18nResourceSynchronizer(AppDbContext db, ILogger<I18nResourceSynchronizer> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 同步所有国际化资源到数据库
    /// </summary>
    public async Task SyncResourcesAsync()
    {
        var resources = GetAllRequiredResources();
        var existingKeysList = await _db.LocalizationResources
            .Select(r => r.Key)
            .ToListAsync();
        var existingKeys = existingKeysList.ToHashSet();

        var missingResources = resources
            .Where(r => !existingKeys.Contains(r.Key))
            .ToList();

        if (missingResources.Any())
        {
            _logger.LogInformation("同步 {Count} 个缺失的国际化资源到数据库", missingResources.Count);
            await _db.LocalizationResources.AddRangeAsync(missingResources);
            await _db.SaveChangesAsync();
            _logger.LogInformation("成功同步国际化资源");
        }
        else
        {
            _logger.LogDebug("所有国际化资源已存在，无需同步");
        }
    }

    /// <summary>
    /// 获取所有必需的国际化资源
    /// </summary>
    private List<LocalizationResource> GetAllRequiredResources()
    {
        var resources = new List<LocalizationResource>();

        // EntityDefinitionEdit.razor 相关资源
        resources.AddRange(new[]
        {
            new LocalizationResource { Key = "LBL_CREATE_ENTITY_DEF", ZH = "新建实体定义", JA = "エンティティ定義を作成", EN = "Create Entity Definition" },
            new LocalizationResource { Key = "LBL_EDIT_ENTITY_DEF", ZH = "编辑实体定义", JA = "エンティティ定義を編集", EN = "Edit Entity Definition" },
            new LocalizationResource { Key = "LBL_NAMESPACE", ZH = "命名空间", JA = "ネームスペース", EN = "Namespace" },
            new LocalizationResource { Key = "LBL_ENTITY_NAME", ZH = "实体名称", JA = "エンティティ名", EN = "Entity Name" },
            new LocalizationResource { Key = "TXT_ENTITY_NAME_HINT", ZH = "将用作C#类名，如: Product", JA = "C#クラス名として使用されます、例: Product", EN = "Will be used as C# class name, e.g.: Product" },
            new LocalizationResource { Key = "TXT_MULTILINGUAL_DISPLAY_NAME_HINT", ZH = "请提供实体的多语言显示名称（至少一种语言）", JA = "エンティティの多言語表示名を提供してください（少なくとも1言語）", EN = "Please provide multilingual display names (at least one language)" },
            new LocalizationResource { Key = "LBL_STRUCTURE_TYPE", ZH = "结构类型", JA = "構造タイプ", EN = "Structure Type" },
            new LocalizationResource { Key = "LBL_SINGLE_ENTITY", ZH = "单一实体", JA = "単一エンティティ", EN = "Single Entity" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_ENTITY", ZH = "主从实体", JA = "マスター・詳細エンティティ", EN = "Master-Detail Entity" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_GRANDCHILD_ENTITY", ZH = "主从孙实体", JA = "マスター・詳細・孫エンティティ", EN = "Master-Detail-Grandchild Entity" },
            new LocalizationResource { Key = "LBL_INTERFACES", ZH = "接口", JA = "インターフェース", EN = "Interfaces" },
            new LocalizationResource { Key = "TXT_AUDIT_FIELDS", ZH = "审计字段", JA = "監査フィールド", EN = "Audit Fields" },
            new LocalizationResource { Key = "TXT_VERSION", ZH = "版本", JA = "バージョン", EN = "Version" },
            new LocalizationResource { Key = "TXT_TIME_VERSION", ZH = "时间版本", JA = "タイムバージョン", EN = "Time Version" },
            new LocalizationResource { Key = "LBL_ICON", ZH = "图标", JA = "アイコン", EN = "Icon" },
            new LocalizationResource { Key = "LBL_CATEGORY", ZH = "分类", JA = "カテゴリ", EN = "Category" },
            new LocalizationResource { Key = "TXT_CATEGORY_PLACEHOLDER", ZH = "业务管理", JA = "ビジネス管理", EN = "Business Management" },
            new LocalizationResource { Key = "LBL_SORT_ORDER", ZH = "排序", JA = "並び順", EN = "Sort Order" },
            new LocalizationResource { Key = "LBL_ENABLED", ZH = "启用", JA = "有効", EN = "Enabled" },
            new LocalizationResource { Key = "LBL_FIELD_DEFINITION", ZH = "字段定义", JA = "フィールド定義", EN = "Field Definition" },
            new LocalizationResource { Key = "BTN_ADD_FIELD", ZH = "添加字段", JA = "フィールドを追加", EN = "Add Field" },
            new LocalizationResource { Key = "LBL_PROPERTY_NAME", ZH = "属性名", JA = "プロパティ名", EN = "Property Name" },
            new LocalizationResource { Key = "LBL_DISPLAY_NAME", ZH = "显示名", JA = "表示名", EN = "Display Name" },
            new LocalizationResource { Key = "LBL_DATA_TYPE", ZH = "数据类型", JA = "データタイプ", EN = "Data Type" },
            new LocalizationResource { Key = "LBL_LENGTH", ZH = "长度", JA = "長さ", EN = "Length" },
            new LocalizationResource { Key = "LBL_REQUIRED", ZH = "必填", JA = "必須", EN = "Required" },
            new LocalizationResource { Key = "LBL_ACTIONS", ZH = "操作", JA = "操作", EN = "Actions" },
            new LocalizationResource { Key = "BTN_EDIT", ZH = "编辑", JA = "編集", EN = "Edit" },
            new LocalizationResource { Key = "BTN_DELETE", ZH = "删除", JA = "削除", EN = "Delete" },
            new LocalizationResource { Key = "MSG_CONFIRM_DELETE", ZH = "确定删除？", JA = "削除してもよろしいですか？", EN = "Confirm delete?" },
            new LocalizationResource { Key = "LBL_CREATE_FIELD", ZH = "新建字段", JA = "新しいフィールド", EN = "Create Field" },
            new LocalizationResource { Key = "LBL_EDIT_FIELD", ZH = "编辑字段", JA = "フィールドを編集", EN = "Edit Field" },
            new LocalizationResource { Key = "TXT_FOR_STRING_TYPE", ZH = "对于String类型", JA = "String型の場合", EN = "For String type" },
            new LocalizationResource { Key = "TXT_FOR_DECIMAL_TYPE", ZH = "对于Decimal类型", JA = "Decimal型の場合", EN = "For Decimal type" },
            new LocalizationResource { Key = "LBL_PRECISION", ZH = "精度", JA = "精度", EN = "Precision" },
            new LocalizationResource { Key = "LBL_SCALE", ZH = "小数位", JA = "スケール", EN = "Scale" },
            new LocalizationResource { Key = "LBL_DEFAULT_VALUE", ZH = "默认值", JA = "デフォルト値", EN = "Default Value" },
            new LocalizationResource { Key = "TXT_DEFAULT_VALUE_HINT", ZH = "如: 0, NOW, NEWID", JA = "例: 0, NOW, NEWID", EN = "e.g.: 0, NOW, NEWID" },
            new LocalizationResource { Key = "MSG_LOAD_FAILED", ZH = "加载失败", JA = "読み込み失敗", EN = "Load failed" },
            new LocalizationResource { Key = "MSG_DISPLAY_NAME_REQUIRED", ZH = "显示名至少需要提供一种语言的文本", JA = "表示名は少なくとも1言語必要です", EN = "Display name requires at least one language" },
            new LocalizationResource { Key = "MSG_CREATE_SUCCESS", ZH = "创建成功", JA = "作成に成功しました", EN = "Created successfully" },
            new LocalizationResource { Key = "MSG_UPDATE_SUCCESS", ZH = "更新成功", JA = "更新に成功しました", EN = "Updated successfully" },
            new LocalizationResource { Key = "MSG_SAVE_FAILED", ZH = "保存失败", JA = "保存に失敗しました", EN = "Save failed" },
            new LocalizationResource { Key = "MSG_FIELD_DISPLAY_NAME_REQUIRED", ZH = "字段显示名至少需要提供一种语言的文本", JA = "フィールドの表示名は少なくとも1言語必要です", EN = "Field display name requires at least one language" },
            new LocalizationResource { Key = "TXT_YES", ZH = "是", JA = "はい", EN = "Yes" },
            new LocalizationResource { Key = "TXT_NO", ZH = "否", JA = "いいえ", EN = "No" },
        });

        // MasterDetailConfig.razor 相关资源
        resources.AddRange(new[]
        {
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_CONFIG", ZH = "主子表配置", JA = "マスター・詳細設定", EN = "Master-Detail Configuration" },
            new LocalizationResource { Key = "BTN_SAVE_CONFIG", ZH = "保存配置", JA = "設定を保存", EN = "Save Configuration" },
            new LocalizationResource { Key = "BTN_PREVIEW_STRUCTURE", ZH = "预览结构", JA = "構造をプレビュー", EN = "Preview Structure" },
            new LocalizationResource { Key = "MSG_ENTITY_NOT_FOUND", ZH = "实体不存在", JA = "エンティティが見つかりません", EN = "Entity not found" },
            new LocalizationResource { Key = "MSG_ENTITY_NOT_FOUND_DESC", ZH = "未找到指定的实体定义", JA = "指定されたエンティティ定義が見つかりません", EN = "Entity definition not found" },
            new LocalizationResource { Key = "LBL_ENTITY_INFO", ZH = "实体信息", JA = "エンティティ情報", EN = "Entity Information" },
            new LocalizationResource { Key = "LBL_FULL_TYPE_NAME", ZH = "完整类型名", JA = "完全な型名", EN = "Full Type Name" },
            new LocalizationResource { Key = "LBL_CURRENT_STRUCTURE", ZH = "当前结构", JA = "現在の構造", EN = "Current Structure" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_TWO_LEVEL", ZH = "主子结构（两层）", JA = "マスター・詳細（2階層）", EN = "Master-Detail (Two Level)" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_THREE_LEVEL", ZH = "主子孙结构（三层）", JA = "マスター・詳細・孫（3階層）", EN = "Master-Detail-Grandchild (Three Level)" },
            new LocalizationResource { Key = "LBL_CHILD_ENTITY_CONFIG", ZH = "子实体配置", JA = "子エンティティ設定", EN = "Child Entity Configuration" },
            new LocalizationResource { Key = "BTN_ADD_CHILD_ENTITY", ZH = "添加子实体", JA = "子エンティティを追加", EN = "Add Child Entity" },
            new LocalizationResource { Key = "MSG_NO_CHILD_ENTITY_CONFIGURED", ZH = "尚未配置子实体", JA = "子エンティティがまだ設定されていません", EN = "No child entity configured yet" },
            new LocalizationResource { Key = "BTN_ADD_NOW", ZH = "立即添加", JA = "今すぐ追加", EN = "Add Now" },
            new LocalizationResource { Key = "LBL_CHILD_ENTITY_NAME", ZH = "子实体名称", JA = "子エンティティ名", EN = "Child Entity Name" },
            new LocalizationResource { Key = "LBL_FOREIGN_KEY_FIELD", ZH = "外键字段", JA = "外部キーフィールド", EN = "Foreign Key Field" },
            new LocalizationResource { Key = "LBL_COLLECTION_PROPERTY", ZH = "集合属性", JA = "コレクションプロパティ", EN = "Collection Property" },
            new LocalizationResource { Key = "LBL_CASCADE_DELETE", ZH = "级联删除", JA = "カスケード削除", EN = "Cascade Delete" },
            new LocalizationResource { Key = "TXT_PLACEHOLDER_ORDER_ID", ZH = "例如: OrderId", JA = "例: OrderId", EN = "e.g.: OrderId" },
            new LocalizationResource { Key = "TXT_PLACEHOLDER_ORDER_LINES", ZH = "例如: OrderLines", JA = "例: OrderLines", EN = "e.g.: OrderLines" },
            new LocalizationResource { Key = "LBL_CASCADE_NO_ACTION", ZH = "不操作", JA = "操作なし", EN = "No Action" },
            new LocalizationResource { Key = "LBL_CASCADE_SET_NULL", ZH = "设为NULL", JA = "NULLに設定", EN = "Set NULL" },
            new LocalizationResource { Key = "LBL_CASCADE_RESTRICT", ZH = "限制", JA = "制限", EN = "Restrict" },
            new LocalizationResource { Key = "LBL_AUTO_SAVE", ZH = "自动保存", JA = "自動保存", EN = "Auto Save" },
            new LocalizationResource { Key = "BTN_REMOVE", ZH = "移除", JA = "削除", EN = "Remove" },
            new LocalizationResource { Key = "TXT_FOREIGN_KEY_FIELD_DESC", ZH = "子表中指向主表的外键字段名", JA = "子テーブルのマスターテーブルへの外部キーフィールド名", EN = "Foreign key field name in child table pointing to master" },
            new LocalizationResource { Key = "TXT_COLLECTION_PROPERTY_DESC", ZH = "主表中引用子表集合的属性名", JA = "マスターテーブルの子テーブルコレクションプロパティ名", EN = "Collection property name in master referencing children" },
            new LocalizationResource { Key = "LBL_CONFIG_DESCRIPTION", ZH = "配置说明", JA = "設定の説明", EN = "Configuration Description" },
            new LocalizationResource { Key = "LBL_CASCADE_DELETE_BEHAVIOR", ZH = "级联删除行为", JA = "カスケード削除動作", EN = "Cascade Delete Behavior" },
            new LocalizationResource { Key = "TXT_CASCADE_NO_ACTION_DESC", ZH = "删除主表时不影响子表", JA = "マスター削除時に子テーブルに影響なし", EN = "No impact on children when master is deleted" },
            new LocalizationResource { Key = "TXT_CASCADE_DELETE_DESC", ZH = "删除主表时自动删除关联子表", JA = "マスター削除時に関連する子を自動削除", EN = "Automatically delete related children when master is deleted" },
            new LocalizationResource { Key = "TXT_CASCADE_SET_NULL_DESC", ZH = "删除主表时将子表外键设为NULL", JA = "マスター削除時に子の外部キーをNULLに設定", EN = "Set child foreign key to NULL when master is deleted" },
            new LocalizationResource { Key = "TXT_CASCADE_RESTRICT_DESC", ZH = "存在子表时禁止删除主表", JA = "子が存在する場合、マスター削除を禁止", EN = "Prevent master deletion if children exist" },
            new LocalizationResource { Key = "LBL_AUTO_CASCADE_SAVE", ZH = "自动级联保存", JA = "自動カスケード保存", EN = "Auto Cascade Save" },
            new LocalizationResource { Key = "TXT_AUTO_CASCADE_SAVE_DESC", ZH = "保存主表时自动保存子表", JA = "マスター保存時に子を自動保存", EN = "Automatically save children when master is saved" },
            new LocalizationResource { Key = "LBL_ADD_CHILD_ENTITY", ZH = "添加子实体", JA = "子エンティティを追加", EN = "Add Child Entity" },
            new LocalizationResource { Key = "LBL_CHILD_ENTITY", ZH = "子实体", JA = "子エンティティ", EN = "Child Entity" },
            new LocalizationResource { Key = "TXT_SELECT_CHILD_ENTITY", ZH = "选择子实体", JA = "子エンティティを選択", EN = "Select Child Entity" },
            new LocalizationResource { Key = "LBL_ENTITY_STRUCTURE_PREVIEW", ZH = "实体结构预览", JA = "エンティティ構造プレビュー", EN = "Entity Structure Preview" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL", ZH = "主子表", JA = "マスター・詳細", EN = "Master-Detail" },
            new LocalizationResource { Key = "LBL_MASTER_DETAIL_GRANDCHILD", ZH = "主子孙表", JA = "マスター・詳細・孫", EN = "Master-Detail-Grandchild" },
            new LocalizationResource { Key = "LBL_ENTITY", ZH = "实体", JA = "エンティティ", EN = "Entity" },
            new LocalizationResource { Key = "LBL_CHILD_ENTITIES", ZH = "子实体", JA = "子エンティティ", EN = "Child Entities" },
            new LocalizationResource { Key = "LBL_FOREIGN_KEY", ZH = "外键", JA = "外部キー", EN = "Foreign Key" },
            new LocalizationResource { Key = "LBL_COLLECTION", ZH = "集合", JA = "コレクション", EN = "Collection" },
            new LocalizationResource { Key = "BTN_ADD", ZH = "添加", JA = "追加", EN = "Add" },
            new LocalizationResource { Key = "MSG_LOAD_CANDIDATES_FAILED", ZH = "加载候选实体失败", JA = "候補エンティティの読み込みに失敗しました", EN = "Failed to load candidate entities" },
            new LocalizationResource { Key = "MSG_PLEASE_SELECT_CHILD_ENTITY", ZH = "请选择子实体", JA = "子エンティティを選択してください", EN = "Please select child entity" },
            new LocalizationResource { Key = "MSG_PLEASE_INPUT_FOREIGN_KEY", ZH = "请输入外键字段名", JA = "外部キーフィールド名を入力してください", EN = "Please input foreign key field name" },
            new LocalizationResource { Key = "MSG_PLEASE_INPUT_COLLECTION_PROPERTY", ZH = "请输入集合属性名", JA = "コレクションプロパティ名を入力してください", EN = "Please input collection property name" },
            new LocalizationResource { Key = "MSG_CHILD_ENTITY_ALREADY_ADDED", ZH = "该子实体已经添加过了", JA = "この子エンティティは既に追加されています", EN = "This child entity has already been added" },
            new LocalizationResource { Key = "MSG_CONFIG_SAVED", ZH = "配置已保存", JA = "設定を保存しました", EN = "Configuration saved" },
            new LocalizationResource { Key = "MSG_PREVIEW_FAILED", ZH = "预览失败", JA = "プレビューに失敗しました", EN = "Preview failed" },
            new LocalizationResource { Key = "TXT_SINGLE_ENTITY_DESC", ZH = "独立的单一实体，无关联子表", JA = "独立した単一エンティティ、関連する子テーブルなし", EN = "Independent single entity, no related child tables" },
            new LocalizationResource { Key = "TXT_MASTER_DETAIL_DESC", ZH = "主子两层结构，主表包含一组子表记录", JA = "2階層のマスター・詳細構造、マスターが子レコードのセットを含む", EN = "Two-level master-detail structure, master contains a set of child records" },
            new LocalizationResource { Key = "TXT_MASTER_DETAIL_GRANDCHILD_DESC", ZH = "主子孙三层结构，主表→子表→孙表的嵌套关系", JA = "3階層のマスター・詳細・孫構造、マスター→子→孫のネスト関係", EN = "Three-level master-detail-grandchild structure with nested relationships" },
        });

        // EntityDefinitionPublish.razor 相关资源
        resources.AddRange(new[]
        {
            new LocalizationResource { Key = "LBL_ENTITY_PUBLISH", ZH = "实体发布", JA = "エンティティ公開", EN = "Entity Publish" },
            new LocalizationResource { Key = "BTN_PUBLISH_ENTITY", ZH = "发布实体", JA = "エンティティを公開", EN = "Publish Entity" },
            new LocalizationResource { Key = "BTN_GENERATE_CODE", ZH = "生成代码", JA = "コードを生成", EN = "Generate Code" },
            new LocalizationResource { Key = "BTN_COMPILE", ZH = "编译", JA = "コンパイル", EN = "Compile" },
            new LocalizationResource { Key = "LBL_STATUS", ZH = "状态", JA = "ステータス", EN = "Status" },
            new LocalizationResource { Key = "LBL_DRAFT", ZH = "草稿", JA = "下書き", EN = "Draft" },
            new LocalizationResource { Key = "LBL_MODIFIED", ZH = "已修改", JA = "変更済み", EN = "Modified" },
            new LocalizationResource { Key = "LBL_PUBLISHED", ZH = "已发布", JA = "公開済み", EN = "Published" },
            new LocalizationResource { Key = "LBL_FIELD_COUNT", ZH = "字段数", JA = "フィールド数", EN = "Field Count" },
            new LocalizationResource { Key = "LBL_INTERFACE_COUNT", ZH = "接口数", JA = "インターフェース数", EN = "Interface Count" },
            new LocalizationResource { Key = "LBL_IS_LOCKED", ZH = "是否锁定", JA = "ロック中", EN = "Is Locked" },
            new LocalizationResource { Key = "LBL_CREATED_TIME", ZH = "创建时间", JA = "作成時刻", EN = "Created Time" },
            new LocalizationResource { Key = "LBL_CREATED_BY", ZH = "创建人", JA = "作成者", EN = "Created By" },
            new LocalizationResource { Key = "LBL_DDL_PREVIEW", ZH = "DDL预览", JA = "DDLプレビュー", EN = "DDL Preview" },
            new LocalizationResource { Key = "LBL_DDL_SCRIPT", ZH = "DDL脚本", JA = "DDLスクリプト", EN = "DDL Script" },
            new LocalizationResource { Key = "MSG_NO_DDL_SCRIPT", ZH = "暂无DDL脚本", JA = "DDLスクリプトがありません", EN = "No DDL script available" },
            new LocalizationResource { Key = "MSG_CLICK_GENERATE_CODE", ZH = "请先点击【生成代码】以生成C#代码", JA = "まず[コードを生成]をクリックしてC#コードを生成してください", EN = "Please click [Generate Code] first to generate C# code" },
            new LocalizationResource { Key = "LBL_DDL_HISTORY", ZH = "DDL历史", JA = "DDL履歴", EN = "DDL History" },
            new LocalizationResource { Key = "LBL_EXECUTED_TIME", ZH = "执行时间", JA = "実行時刻", EN = "Executed Time" },
            new LocalizationResource { Key = "LBL_SOURCE", ZH = "来源", JA = "ソース", EN = "Source" },
            new LocalizationResource { Key = "LBL_GENERATED_CODE", ZH = "生成的代码", JA = "生成されたコード", EN = "Generated Code" },
            new LocalizationResource { Key = "MSG_CLICK_COMPILE", ZH = "请先点击【编译】以编译代码", JA = "まず[コンパイル]をクリックしてコードをコンパイルしてください", EN = "Please click [Compile] first to compile code" },
            new LocalizationResource { Key = "LBL_COMPILATION_STATUS", ZH = "编译状态", JA = "コンパイルステータス", EN = "Compilation Status" },
            new LocalizationResource { Key = "LBL_SUCCESS", ZH = "成功", JA = "成功", EN = "Success" },
            new LocalizationResource { Key = "LBL_FAILED", ZH = "失败", JA = "失敗", EN = "Failed" },
            new LocalizationResource { Key = "LBL_ASSEMBLY", ZH = "程序集", JA = "アセンブリ", EN = "Assembly" },
            new LocalizationResource { Key = "LBL_TYPE", ZH = "类型", JA = "型", EN = "Type" },
            new LocalizationResource { Key = "LBL_LOADED_TYPES", ZH = "已加载类型", JA = "ロード済み型", EN = "Loaded Types" },
            new LocalizationResource { Key = "LBL_COMPILATION_ERROR", ZH = "编译错误", JA = "コンパイルエラー", EN = "Compilation Error" },
            new LocalizationResource { Key = "LBL_LINE", ZH = "行", JA = "行", EN = "Line" },
            new LocalizationResource { Key = "LBL_COLUMN", ZH = "列", JA = "列", EN = "Column" },
            new LocalizationResource { Key = "MSG_PUBLISH_SUCCESS", ZH = "发布成功", JA = "公開に成功しました", EN = "Published successfully" },
            new LocalizationResource { Key = "MSG_PUBLISH_FAILED", ZH = "发布失败", JA = "公開に失敗しました", EN = "Publish failed" },
            new LocalizationResource { Key = "MSG_CODE_GENERATION_SUCCESS", ZH = "代码生成成功", JA = "コード生成に成功しました", EN = "Code generated successfully" },
            new LocalizationResource { Key = "MSG_CODE_GENERATION_FAILED", ZH = "代码生成失败", JA = "コード生成に失敗しました", EN = "Code generation failed" },
            new LocalizationResource { Key = "MSG_COMPILATION_SUCCESS", ZH = "编译成功", JA = "コンパイルに成功しました", EN = "Compiled successfully" },
            new LocalizationResource { Key = "MSG_COMPILATION_FAILED", ZH = "编译失败", JA = "コンパイルに失敗しました", EN = "Compilation failed" },
            new LocalizationResource { Key = "MSG_COMPILATION_FAILED_DETAIL", ZH = "编译失败，请查看错误详情", JA = "コンパイルに失敗しました。エラーの詳細をご確認ください", EN = "Compilation failed, please check error details" },
            new LocalizationResource { Key = "MSG_LOAD_DDL_FAILED", ZH = "加载DDL失败", JA = "DDLの読み込みに失敗しました", EN = "Failed to load DDL" },
            new LocalizationResource { Key = "MSG_LOAD_HISTORY_FAILED", ZH = "加载历史失败", JA = "履歴の読み込みに失敗しました", EN = "Failed to load history" },
            new LocalizationResource { Key = "LBL_SCRIPT_PREVIEW", ZH = "脚本预览", JA = "スクリプトプレビュー", EN = "Script Preview" },
            new LocalizationResource { Key = "BTN_REFRESH", ZH = "刷新", JA = "更新", EN = "Refresh" },
        });

        // DynamicEntityData.razor 相关资源
        resources.AddRange(new[]
        {
            new LocalizationResource { Key = "LBL_DATA_MANAGEMENT", ZH = "数据管理", JA = "データ管理", EN = "Data Management" },
            new LocalizationResource { Key = "BTN_CREATE", ZH = "新建", JA = "新規作成", EN = "Create" },
            new LocalizationResource { Key = "LBL_DYNAMIC_ENTITY_DATA_MANAGEMENT", ZH = "动态实体数据管理", JA = "動的エンティティデータ管理", EN = "Dynamic Entity Data Management" },
            new LocalizationResource { Key = "LBL_ENTITY_TYPE", ZH = "实体类型", JA = "エンティティタイプ", EN = "Entity Type" },
            new LocalizationResource { Key = "TXT_DYNAMIC_ENTITY_DESC", ZH = "此页面用于管理动态编译加载的实体数据。请确保实体已成功编译加载。", JA = "このページは動的にコンパイルされたエンティティデータを管理するためのものです。エンティティが正常にコンパイルされていることを確認してください。", EN = "This page is for managing dynamically compiled entity data. Please ensure the entity has been successfully compiled." },
            new LocalizationResource { Key = "LBL_DATA_LIST", ZH = "数据列表", JA = "データリスト", EN = "Data List" },
            new LocalizationResource { Key = "MSG_NO_DATA", ZH = "暂无数据", JA = "データがありません", EN = "No data available" },
            new LocalizationResource { Key = "MSG_CREATE_FEATURE_IN_DEVELOPMENT", ZH = "新建功能开发中", JA = "新規作成機能は開発中です", EN = "Create feature in development" },
            new LocalizationResource { Key = "MSG_EDIT_FEATURE_IN_DEVELOPMENT", ZH = "编辑功能开发中", JA = "編集機能は開発中です", EN = "Edit feature in development" },
            new LocalizationResource { Key = "MSG_DELETE_SUCCESS", ZH = "删除成功", JA = "削除に成功しました", EN = "Deleted successfully" },
            new LocalizationResource { Key = "MSG_DELETE_FAILED", ZH = "删除失败", JA = "削除に失敗しました", EN = "Delete failed" },
            new LocalizationResource { Key = "MSG_LOAD_DATA_FAILED", ZH = "加载数据失败", JA = "データの読み込みに失敗しました", EN = "Failed to load data" },
        });

        return resources;
    }
}
