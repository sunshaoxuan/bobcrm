using BobCrm.Api.Domain;
using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext db)
    {
        Console.WriteLine("[DatabaseInitializer] Applying pending migrations using MigrateAsync");
        await db.Database.MigrateAsync();
        if (db is AppDbContext appDbContext)
        {
            var synchronizer = new EntityDefinitionSynchronizer(appDbContext, NullLogger<EntityDefinitionSynchronizer>.Instance);
            await synchronizer.SyncSystemEntitiesAsync();
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
        if (!await db.Set<SystemSettings>().AnyAsync())
        {
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
            await db.SaveChangesAsync();
        }
        if (!await db.Set<FieldDefinition>().IgnoreQueryFilters().AnyAsync())
        {
            await db.Set<FieldDefinition>().AddRangeAsync(
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
            );
        }
        if (!await db.Set<LocalizationLanguage>().IgnoreQueryFilters().AnyAsync())
        {
            await db.Set<LocalizationLanguage>().AddRangeAsync(
                new LocalizationLanguage { Code = "ja", NativeName = "日本語" },
                new LocalizationLanguage { Code = "zh", NativeName = "中文" },
                new LocalizationLanguage { Code = "en", NativeName = "English" }
            );
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
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_value_gin ON \"FieldValues\" USING GIN (\"Value\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_tags_gin ON \"FieldDefinitions\" USING GIN (\"Tags\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fielddefinitions_actions_gin ON \"FieldDefinitions\" USING GIN (\"Actions\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_userlayouts_layoutjson_gin ON \"UserLayouts\" USING GIN (\"LayoutJson\");");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS idx_fieldvalues_customer_field ON \"FieldValues\" (\"CustomerId\", \"FieldDefinitionId\");");
            }
            catch { }
        }
    }
    public static async Task RecreateAsync(DbContext db)
    {
        try { await db.Database.EnsureDeletedAsync(); } catch { }
        await InitializeAsync(db);
    }
}
