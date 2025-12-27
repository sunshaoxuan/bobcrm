using System;

using BobCrm.Api.Base;

using BobCrm.Api.Base.Models;

using BobCrm.Api.Base.Models.Metadata;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.EntityFrameworkCore.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer

{

    public static async Task InitializeAsync(DbContext db)

    {

        var loggerFactory = db.GetService<ILoggerFactory>();
        var logger = loggerFactory != null
            ? loggerFactory.CreateLogger(nameof(DatabaseInitializer))
            : NullLogger.Instance;

        logger.LogInformation("[DatabaseInitializer] Applying pending migrations using MigrateAsync");

        await db.Database.MigrateAsync();

        logger.LogInformation("[DatabaseInitializer] Migrations completed successfully");

        await EnsureTrigramExtensionAsync(db);

        if (db is AppDbContext appDbContext)

        {

            var synchronizer = new EntityDefinitionSynchronizer(appDbContext, NullLogger<EntityDefinitionSynchronizer>.Instance, null, null);

            await synchronizer.SyncSystemEntitiesAsync();

            // 初始化系统枚举
            var enumSeeder = new BobCrm.Api.Services.EnumSeeder(appDbContext);
            await enumSeeder.EnsureSystemEnumsAsync();
            logger.LogInformation("[DatabaseInitializer] System enums initialized successfully");

            // 初始化系统菜单
            var menuSeeder = new BobCrm.Api.Services.SystemMenuSeeder(appDbContext);
            await menuSeeder.EnsureSystemMenusAsync();
            logger.LogInformation("[DatabaseInitializer] System menus initialized successfully");

            await CleanupSampleEntityDefinitionsAsync(appDbContext);

            await CleanupTestUsersAsync(appDbContext);

        }

        // ✅ Field data types（系统字段类型档案）

        {

            var presets = new[]

            {

                CreateFieldDataType("String", "System.String", "Text",

                    CreateLabel("适合短文本字段", "短いテキストに使用", "Use for short text fields")),

                CreateFieldDataType("Text", "System.String", "Text",

                    CreateLabel("适合长文本字段", "長いテキストに使用", "Use for long text")),

                CreateFieldDataType("Int32", "System.Int32", "Number",

                    CreateLabel("32位整型", "32bit 整数", "32-bit integer")),

                CreateFieldDataType("Int64", "System.Int64", "Number",

                    CreateLabel("64位整型", "64bit 整数", "64-bit integer")),

                CreateFieldDataType("Decimal", "System.Decimal", "Number",

                    CreateLabel("十进制数值", "10進小数", "Decimal number")),

                CreateFieldDataType("Boolean", "System.Boolean", "Boolean",

                    CreateLabel("布尔值", "ブール値", "Boolean value")),

                CreateFieldDataType("DateTime", "System.DateTime", "DateTime",

                    CreateLabel("日期时间", "日時", "Date & time")),

                CreateFieldDataType("Date", "System.DateOnly", "DateTime",

                    CreateLabel("日期", "日付", "Date only")),

                CreateFieldDataType("Guid", "System.Guid", "Identity",

                    CreateLabel("全局唯一标识", "グローバル一意識別子", "Global unique identifier")),

                CreateFieldDataType("EntityRef", "System.Guid", "Reference",

                    CreateLabel("实体引用", "エンティティ参照", "Entity reference"))

            };

            var existing = await db.Set<FieldDataTypeEntry>()

                .IgnoreQueryFilters()

                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var preset in presets)

            {

                if (!existing.TryGetValue(preset.Code, out var record))

                {

                    await db.Set<FieldDataTypeEntry>().AddAsync(preset);

                }

                else

                {

                    record.ClrType = preset.ClrType;

                    record.Category = preset.Category;

                    record.Description = preset.Description;

                    record.IsEnabled = true;

                    record.SortOrder = preset.SortOrder;

                    record.UpdatedAt = DateTime.UtcNow;

                }

            }

        }

        // ✅ Field source 枚举档案

        {

            var presets = new[]

            {

                CreateFieldSource("System", "LBL_FIELD_SOURCE_SYSTEM", "TXT_FIELD_SOURCE_SYSTEM_DESC", 10),

                CreateFieldSource("Custom", "LBL_FIELD_SOURCE_CUSTOM", "TXT_FIELD_SOURCE_CUSTOM_DESC", 20),

                CreateFieldSource("Interface", "LBL_FIELD_SOURCE_INTERFACE", "TXT_FIELD_SOURCE_INTERFACE_DESC", 30)

            };

            var existing = await db.Set<FieldSourceEntry>()

                .IgnoreQueryFilters()

                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var preset in presets)

            {

                if (!existing.TryGetValue(preset.Code, out var record))

                {

                    await db.Set<FieldSourceEntry>().AddAsync(preset);

                }

                else

                {

                    record.Name = preset.Name;

                    record.Description = preset.Description;

                    record.IsEnabled = true;

                    record.SortOrder = preset.SortOrder;

                    record.UpdatedAt = DateTime.UtcNow;

                }

            }

        }

        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        // 初始化期间禁用查询过滤器，避免权限检查干扰

        if (!await db.Set<Customer>().IgnoreQueryFilters().AnyAsync())

        {

            var customer1 = new Customer { Code = "C001", Name = "示例客户A", Version = 1 };

            var customer2 = new Customer { Code = "C002", Name = "示例客户B", Version = 1 };

            await db.Set<Customer>().AddRangeAsync(customer1, customer2);

            await db.SaveChangesAsync();

            // Add localized names

            await db.Set<CustomerLocalization>().AddRangeAsync(

                new CustomerLocalization { CustomerId = customer1.Id, Language = "zh", Name = "示例客户A" },

                new CustomerLocalization { CustomerId = customer1.Id, Language = "ja", Name = "サンプル顧客A" },

                new CustomerLocalization { CustomerId = customer1.Id, Language = "en", Name = "Sample Customer A" },

                new CustomerLocalization { CustomerId = customer2.Id, Language = "zh", Name = "示例客户B" },

                new CustomerLocalization { CustomerId = customer2.Id, Language = "ja", Name = "サンプル顧客B" },

                new CustomerLocalization { CustomerId = customer2.Id, Language = "en", Name = "Sample Customer B" }

            );

        }

        // ✅ SystemSettings - 每次启动都同步（Upsert 模式）

        {

            var existing = await db.Set<SystemSettings>().FirstOrDefaultAsync();

            if (existing == null)

            {

                // 不存在则创建

                await db.Set<SystemSettings>().AddAsync(new SystemSettings

                {

                    CompanyName = "OneCRM",

                    DefaultTheme = "calm-light",

                    DefaultPrimaryColor = "#739FD6",

                    DefaultLanguage = "ja",

                    DefaultHomeRoute = "/",

                    DefaultNavMode = NavDisplayModes.IconText,

                    TimeZoneId = "Asia/Tokyo",

                    AllowSelfRegistration = false,

                    UpdatedAt = DateTime.UtcNow

                });

            }

            else

            {

                // 存在则更新默认值（仅更新未自定义的字段）

                // 注意：这里只更新 null 或空值，保留用户的自定义设置

                if (string.IsNullOrEmpty(existing.CompanyName))

                    existing.CompanyName = "OneCRM";

                if (string.IsNullOrEmpty(existing.DefaultTheme))

                    existing.DefaultTheme = "calm-light";

                if (string.IsNullOrEmpty(existing.DefaultPrimaryColor))

                    existing.DefaultPrimaryColor = "#739FD6";

                if (string.IsNullOrEmpty(existing.DefaultLanguage))

                    existing.DefaultLanguage = "ja";

                if (string.IsNullOrEmpty(existing.DefaultHomeRoute))

                    existing.DefaultHomeRoute = "/";

                if (string.IsNullOrEmpty(existing.TimeZoneId))

                    existing.TimeZoneId = "Asia/Tokyo";

                existing.UpdatedAt = DateTime.UtcNow;

            }

        }

        // ✅ FieldDefinition - 每次启动都同步（按 Key 新增或更新）

        {

            var presetFields = new[]

            {

                new FieldDefinition

                {

                    Key = "email",

                    DisplayName = "LBL_EMAIL",

                    DataType = "email",

                    Required = true,

                    Validation = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",

                    DefaultValue = "",

                    Tags = "[\"常用\"]",

                    Actions = "[{\"icon\":\"mail\",\"titleKey\":\"ACT_MAIL\",\"type\":\"click\",\"action\":\"mailto\"}]"

                },

                new FieldDefinition

                {

                    Key = "link",

                    DisplayName = "LBL_LINK",

                    DataType = "link",

                    Required = false,

                    DefaultValue = "https://example.com",

                    Tags = "[\"常用\"]",

                    Actions = "[{\"icon\":\"link\",\"titleKey\":\"ACT_OPEN\",\"action\":\"openLink\"},{\"icon\":\"copy\",\"titleKey\":\"ACT_COPY\",\"action\":\"copy\"}]"

                },

                new FieldDefinition

                {

                    Key = "file",

                    DisplayName = "LBL_FILE_PATH",

                    DataType = "file",

                    Required = false,

                    DefaultValue = "C:/data/readme.txt",

                    Tags = "[\"常用\"]",

                    Actions = "[{\"icon\":\"copy\",\"titleKey\":\"ACT_COPY_PATH\",\"action\":\"copy\"}]"

                },

                new FieldDefinition

                {

                    Key = "rds",

                    DisplayName = "LBL_RDS",

                    DataType = "rds",

                    Required = false,

                    DefaultValue = null,

                    Tags = "[\"远程\"]",

                    Actions = "[{\"icon\":\"download\",\"titleKey\":\"ACT_DOWNLOAD_RDP\",\"action\":\"downloadRdp\"}]"

                },

                new FieldDefinition

                {

                    Key = "priority",

                    DisplayName = "LBL_PRIORITY",

                    DataType = "number",

                    Required = false,

                    DefaultValue = "1",

                    Tags = "[\"扩展\"]",

                    Actions = "[]"

                }

            };

            var existingFields = await db.Set<FieldDefinition>()

                .IgnoreQueryFilters()

                .Where(f => presetFields.Select(p => p.Key).Contains(f.Key))

                .ToDictionaryAsync(f => f.Key);

            foreach (var preset in presetFields)

            {

                if (!existingFields.TryGetValue(preset.Key, out var existing))

                {

                    // 新增

                    await db.Set<FieldDefinition>().AddAsync(preset);

                }

                else

                {

                    // 更新（覆盖预置字段的定义）

                    existing.DisplayName = preset.DisplayName;

                    existing.DataType = preset.DataType;

                    existing.Required = preset.Required;

                    existing.Validation = preset.Validation;

                    existing.DefaultValue = preset.DefaultValue;

                    existing.Tags = preset.Tags;

                    existing.Actions = preset.Actions;

                }

            }

        }

        // ✅ LocalizationLanguage - 每次启动都同步（按 Code 新增或更新）

        {

            var presetLanguages = new[]

            {

                new LocalizationLanguage { Code = "ja", NativeName = "日本語" },

                new LocalizationLanguage { Code = "zh", NativeName = "中文" },

                new LocalizationLanguage { Code = "en", NativeName = "English" }

            };

            var existingLanguages = await db.Set<LocalizationLanguage>()

                .IgnoreQueryFilters()

                .Where(l => presetLanguages.Select(p => p.Code).Contains(l.Code))

                .ToDictionaryAsync(l => l.Code);

            foreach (var preset in presetLanguages)

            {

                if (!existingLanguages.TryGetValue(preset.Code, out var existing))

                {

                    // 新增语言

                    await db.Set<LocalizationLanguage>().AddAsync(preset);

                }

                else

                {

                    // 更新本地化名称

                    existing.NativeName = preset.NativeName;

                }

            }

        }

        // ✅ EntityDomain - 通过数据库管理领域档案

        {

            var presetDomains = new[]

            {

                CreateDomain("CRM", 10, "CRM - 客户关系管理", "CRM - 顧客関係管理", "CRM - Customer Relationship Management"),

                CreateDomain("SCM", 20, "SCM - 供应链管理", "SCM - サプライチェーン管理", "SCM - Supply Chain Management"),

                CreateDomain("FA", 30, "FA - 财务会计", "FA - 財務会計", "FA - Financial Accounting"),

                CreateDomain("HR", 40, "HR - 人力资源", "HR - 人事", "HR - Human Resources"),

                CreateDomain("MFM", 50, "MFM - 生产制造", "MFM - 生産製造", "MFM - Manufacturing"),

                CreateDomain("System", 5, "System - 内置实体", "System - システム内蔵エンティティ", "System - Built-in Entities"),

                CreateDomain("Custom", 100, "Custom - 自定义实体", "Custom - カスタムエンティティ", "Custom - Custom Entities")

            };

            var codes = presetDomains.Select(d => d.Code).ToArray();

            var existingDomains = await db.Set<EntityDomain>()

                .IgnoreQueryFilters()

                .Where(d => codes.Contains(d.Code))

                .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var preset in presetDomains)

            {

                if (!existingDomains.TryGetValue(preset.Code, out var existing))

                {

                    await db.Set<EntityDomain>().AddAsync(preset);

                }

                else

                {

                    existing.Name = new Dictionary<string, string?>(preset.Name, StringComparer.OrdinalIgnoreCase);

                    existing.SortOrder = preset.SortOrder;

                    existing.IsSystem = preset.IsSystem;

                    existing.IsEnabled = true;

                    existing.UpdatedAt = DateTime.UtcNow;

                }

            }

        }

        // EntityDefinition 自动同步已在 Program.cs 中由 EntityDefinitionSynchronizer 处理

        // ✅ 统一从 JSON 文件加载 i18n 资源（单一数据源原则，动态语言支持）

        {

            // 从JSON文件加载所有i18n资源

            var allResources = await I18nResourceLoader.LoadResourcesAsync();

            // 批量查询已存在的键（优化：一次查询代替 N 次查询）

            var existingKeysList = await db.Set<LocalizationResource>()

                .Select(r => r.Key)

                .ToListAsync();

            var existingKeysSet = existingKeysList.ToHashSet();

            // 分离新增和更新的资源

            var toAdd = allResources.Where(r => !existingKeysSet.Contains(r.Key)).ToList();

            var toUpdate = allResources.Where(r => existingKeysSet.Contains(r.Key)).ToList();

            // 批量添加新资源

            if (toAdd.Any())

            {

                await db.Set<LocalizationResource>().AddRangeAsync(toAdd);

            }

            // 批量更新已存在的资源

            if (toUpdate.Any())

            {

                var existingDict = await db.Set<LocalizationResource>()

                    .Where(r => toUpdate.Select(u => u.Key).Contains(r.Key))

                    .ToDictionaryAsync(r => r.Key);

                foreach (var resource in toUpdate)

                {

                    if (existingDict.TryGetValue(resource.Key, out var existing))

                    {

                        // ✅ 动态更新翻译字典，不硬编码语种

                        existing.Translations = new Dictionary<string, string>(resource.Translations);

                    }

                }

            }

        }

        try

        {

            await db.SaveChangesAsync();

        }

        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)

        {

            // Primary key constraint violation - some records already exist in database

            // This can happen in test scenarios where records are deleted then re-initialized

            // Log and continue, as the goal is to ensure records exist

            if (ex.Message.Contains("duplicate key") || ex.Message.Contains("UNIQUE constraint"))

            {

                // Ignore duplicate key errors - records already exist, which is acceptable

            }

            else

            {

                throw;

            }

        }

        // 创建默认客户档案显示模板（customerId = 0 表示全局模板，不绑定具体客户）

        // 所有用户默认使用此模板，除非他们保存了自己的模板

        if (!await db.Set<UserLayout>().IgnoreQueryFilters().AnyAsync(UserLayoutScope.ForUser("__default__", 0)))

        {

            var defaultTemplate = new

            {

                mode = "flow",

                items = new Dictionary<string, object>

                {

                    // email 字段 - 必填，占半行

                    ["email"] = new

                    {

                        order = 0,

                        w = 6,  // 向后兼容

                        Width = 50,

                        WidthUnit = "%",

                        Height = 32,

                        HeightUnit = "px",

                        visible = true,

                        label = "LBL_EMAIL",

                        type = "textbox"

                    },

                    // link 字段 - 网址链接，占半行

                    ["link"] = new

                    {

                        order = 1,

                        w = 6,

                        Width = 50,

                        WidthUnit = "%",

                        Height = 32,

                        HeightUnit = "px",

                        visible = true,

                        label = "LBL_LINK",

                        type = "textbox"

                    },

                    // file 字段 - 文件路径，占半行

                    ["file"] = new

                    {

                        order = 2,

                        w = 6,

                        Width = 50,

                        WidthUnit = "%",

                        Height = 32,

                        HeightUnit = "px",

                        visible = true,

                        label = "LBL_FILE_PATH",

                        type = "textbox"

                    },

                    // rds 字段 - 远程桌面连接，占半行

                    ["rds"] = new

                    {

                        order = 3,

                        w = 6,

                        Width = 50,

                        WidthUnit = "%",

                        Height = 32,

                        HeightUnit = "px",

                        visible = true,

                        label = "LBL_RDS",

                        type = "textbox"

                    },

                    // priority 字段 - 优先级数字，占1/4行

                    ["priority"] = new

                    {

                        order = 4,

                        w = 3,

                        Width = 25,

                        WidthUnit = "%",

                        Height = 32,

                        HeightUnit = "px",

                        visible = true,

                        label = "LBL_PRIORITY",

                        type = "textbox"

                    }

                }

            };

            var templateJson = System.Text.Json.JsonSerializer.Serialize(defaultTemplate);

            var defaultLayout = new UserLayout

            {

                UserId = "__default__",

                LayoutJson = templateJson

            };

            UserLayoutScope.ApplyCustomerScope(defaultLayout, 0); // 0 表示全局用户级模板

            db.Set<UserLayout>().Add(defaultLayout);

            await db.SaveChangesAsync();

        }

        if (isNpgsql)

        {

            try

            {

                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_value_gin ON \"FieldValues\" USING GIN (\"Value\" gin_trgm_ops);");

                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_tags_gin ON \"FieldDefinitions\" USING GIN (\"Tags\" gin_trgm_ops);");

                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_actions_gin ON \"FieldDefinitions\" USING GIN (\"Actions\" gin_trgm_ops);");

                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_userlayouts_layoutjson_gin ON \"UserLayouts\" USING GIN (\"LayoutJson\" gin_trgm_ops);");

                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_customer_field ON \"FieldValues\" (\"CustomerId\", \"FieldDefinitionId\");");

            }

            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create PostgreSQL trigram indexes, continuing without them.");
            }

        }

    }

    private static async Task EnsureTrigramExtensionAsync(DbContext db)
    {
        if (!db.Database.IsNpgsql())
        {
            return;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        }
        catch
        {
        }
    }

    public static async Task RecreateAsync(DbContext db)

    {

        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (isNpgsql)

        {

            // PostgreSQL: Force terminate connections before dropping

            var connectionString = db.Database.GetConnectionString();

            if (!string.IsNullOrEmpty(connectionString))

            {

                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

                var dbName = builder.Database;

                // Connect to postgres database to terminate connections

                builder.Database = "postgres";

                var adminConnString = builder.ToString();

                using (var adminConn = new Npgsql.NpgsqlConnection(adminConnString))

                {

                    await adminConn.OpenAsync();

                    // Terminate all connections to the target database

                    using (var terminateCmd = new Npgsql.NpgsqlCommand(

                        $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}' AND pid <> pg_backend_pid();",

                        adminConn))

                    {

                        try { await terminateCmd.ExecuteNonQueryAsync(); }
                        catch (Exception ex)
                        {
                            var loggerFactory = db.GetService<ILoggerFactory>();
                            var logger = loggerFactory != null
                                ? loggerFactory.CreateLogger(nameof(DatabaseInitializer))
                                : NullLogger.Instance;
                            logger.LogDebug(ex, "Failed to terminate active PostgreSQL connections for database {DatabaseName}.", dbName);
                        }

                    }

                }

            }

        }

        // Use EF Core's built-in method to delete the database

        // This ensures proper handling of migrations history

        try

        {

            await db.Database.EnsureDeletedAsync();

        }

        catch

        {

            // Ignore errors - database might not exist

        }

        // Let MigrateAsync handle creating the database and applying all migrations

        // This is more reliable than manually creating the database

        await InitializeAsync(db);

    }

    private static readonly string[] DeprecatedSampleNamespaces =

    {

        "BobCrm.Base.Test",

        "BobCrm.Base.Sample",

        "BobCrm.Base.Example"

    };

    private static readonly string[] DeprecatedSampleEntityNames =

    {

        "TestSingleEntity",

        "Order",

        "OrderLine",

        "OrderLineAttribute",

        "ChildEntity",

        "DraftEntity",

        "PublishedEntity",

        "IndependentEntity",

        "Product"

    };



    private const string RuntimeWorkflowNamespace = "BobCrm.Dynamic";





    private static async Task CleanupSampleEntityDefinitionsAsync(AppDbContext db)



    {



        var sampleEntities = await db.EntityDefinitions



            .Where(ed =>



                ed.Source == EntitySource.Custom &&



                (DeprecatedSampleNamespaces.Contains(ed.Namespace) ||



                 DeprecatedSampleEntityNames.Contains(ed.EntityName)))



            .ToListAsync();



        var workflowQuery = db.EntityDefinitions
            .Where(ed =>
                ed.Source == EntitySource.Custom &&
                ed.Namespace == RuntimeWorkflowNamespace &&
                ed.EntityName != null);

        if (db.Database.IsNpgsql())
        {
            workflowQuery = workflowQuery.Where(ed => EF.Functions.ILike(ed.EntityName!, "Workflow%"));
        }
        else
        {
            workflowQuery = workflowQuery.Where(ed => EF.Functions.Like(ed.EntityName!, "Workflow%"));
        }

        var workflowEntities = await workflowQuery.ToListAsync();



        if (workflowEntities.Any())



        {



            var entityRoutes = workflowEntities



                .Select(e => e.EntityRoute)



                .Where(r => !string.IsNullOrWhiteSpace(r))



                .Select(r => r!)



                .ToHashSet(StringComparer.OrdinalIgnoreCase);



            if (entityRoutes.Count > 0)



            {



                var bindings = await db.TemplateBindings

                    .Where(b => b.EntityType != null && entityRoutes.Contains(b.EntityType))


                    .ToListAsync();



                if (bindings.Count > 0)



                {



                    db.TemplateBindings.RemoveRange(bindings);



                }



                var templates = await db.FormTemplates

                    .Where(t => t.EntityType != null && entityRoutes.Contains(t.EntityType))


                    .ToListAsync();



                if (templates.Count > 0)



                {



                    db.FormTemplates.RemoveRange(templates);



                }



                var workflowFunctionNodes = await db.FunctionNodes
                    .Join(
                        db.TemplateStateBindings,
                        fn => fn.TemplateStateBindingId,
                        b => b.Id,
                        (fn, b) => new { fn, binding = b })
                    .Where(x => entityRoutes.Contains(x.binding.EntityType))
                    .Select(x => x.fn)
                    .ToListAsync();

                if (workflowFunctionNodes.Count > 0)

                {

                    var nodeIds = workflowFunctionNodes.Select(fn => fn.Id).ToList();

                    var permissions = await db.RoleFunctionPermissions

                        .Where(p => nodeIds.Contains(p.FunctionId))

                        .ToListAsync();

                    if (permissions.Count > 0)

                    {

                        db.RoleFunctionPermissions.RemoveRange(permissions);

                    }

                    db.FunctionNodes.RemoveRange(workflowFunctionNodes);

                }


            }



        }



        var entitiesToRemove = sampleEntities



            .Concat(workflowEntities)



            .DistinctBy(e => e.Id)



            .ToList();



        if (!entitiesToRemove.Any())



        {



            return;



        }



        db.EntityDefinitions.RemoveRange(entitiesToRemove);



        await db.SaveChangesAsync();



    }



        private static async Task CleanupTestUsersAsync(AppDbContext db)

    {

        var testUsers = await db.Users

            .Where(u => u.UserName != null && u.UserName.StartsWith("user_"))

            .ToListAsync();

        if (!testUsers.Any())

        {

            return;

        }

        var userIds = testUsers.Select(u => u.Id).ToList();

        db.CustomerAccesses.RemoveRange(db.CustomerAccesses.Where(ca => userIds.Contains(ca.UserId)));

        db.RoleAssignments.RemoveRange(db.RoleAssignments.Where(ra => userIds.Contains(ra.UserId)));

        db.UserLayouts.RemoveRange(db.UserLayouts.Where(ul => userIds.Contains(ul.UserId)));

        db.UserPreferences.RemoveRange(db.UserPreferences.Where(up => userIds.Contains(up.UserId)));

        db.FormTemplates.RemoveRange(db.FormTemplates.Where(ft => userIds.Contains(ft.UserId)));

        db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(rt => userIds.Contains(rt.UserId)));

        db.UserClaims.RemoveRange(db.UserClaims.Where(uc => userIds.Contains(uc.UserId)));

        db.UserLogins.RemoveRange(db.UserLogins.Where(ul => userIds.Contains(ul.UserId)));

        db.UserTokens.RemoveRange(db.UserTokens.Where(ut => userIds.Contains(ut.UserId)));

        db.UserRoles.RemoveRange(db.UserRoles.Where(ur => userIds.Contains(ur.UserId)));

        db.Users.RemoveRange(testUsers);

        await db.SaveChangesAsync();

    }

    private static EntityDomain CreateDomain(string code, int sortOrder, string zh, string ja, string en)

    {

        return new EntityDomain

        {

            Code = code,

            SortOrder = sortOrder,

            IsSystem = true,

            IsEnabled = true,

            Name = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)

            {

                ["zh"] = zh,

                ["ja"] = ja,

                ["en"] = en

            },

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow

        };

    }

    private static FieldDataTypeEntry CreateFieldDataType(

        string code,

        string clrType,

        string category,

        Dictionary<string, string?> description)

    {

        return new FieldDataTypeEntry

        {

            Code = code,

            ClrType = clrType,

            Category = category,

            Description = description,

            SortOrder = 100

        };

    }

    private static FieldSourceEntry CreateFieldSource(string code, string name, string description, int sortOrder)

    {

        return new FieldSourceEntry

        {

            Code = code,

            Name = name,

            Description = description,

            SortOrder = sortOrder

        };

    }

    private static Dictionary<string, string?> CreateLabel(string zh, string ja, string en)

    {

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)

        {

            ["zh"] = zh,

            ["ja"] = ja,

            ["en"] = en

        };

    }

}

