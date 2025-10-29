using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext db)
    {
        try { await db.Database.EnsureCreatedAsync(); } catch { }
        try { await db.Set<Customer>().CountAsync(); }
        catch
        {
            try { var creator = db.Database.GetService<IRelationalDatabaseCreator>(); await creator.CreateTablesAsync(); } catch { }
        }

        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isNpgsql)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"LocalizationLanguages\" (\"Id\" SERIAL PRIMARY KEY, \"Code\" text NOT NULL, \"NativeName\" text NOT NULL);");
            }
            catch { }
        }

        if (!await db.Set<Customer>().AnyAsync())
        {
            await db.Set<Customer>().AddRangeAsync(
                new Customer { Code = "C001", Name = "LBL_CUSTOMER", Version = 1 },
                new Customer { Code = "C002", Name = "LBL_CUSTOMER", Version = 1 }
            );
        }

        if (!await db.Set<FieldDefinition>().AnyAsync())
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

        if (!await db.Set<LocalizationLanguage>().AnyAsync())
        {
            await db.Set<LocalizationLanguage>().AddRangeAsync(
                new LocalizationLanguage { Code = "ja", NativeName = "日本語" },
                new LocalizationLanguage { Code = "zh", NativeName = "中文" },
                new LocalizationLanguage { Code = "en", NativeName = "English" }
            );
        }

        if (!await db.Set<LocalizationResource>().AnyAsync())
        {
            await db.Set<LocalizationResource>().AddRangeAsync(
                new LocalizationResource { Key = "LBL_CUSTOMER", ZH = "客户", JA = "顧客", EN = "Customer" },
                new LocalizationResource { Key = "LBL_EMAIL", ZH = "邮箱", JA = "メール", EN = "Email" },
                new LocalizationResource { Key = "BTN_SAVE", ZH = "保存", JA = "保存", EN = "Save" },
                new LocalizationResource { Key = "LBL_LINK", ZH = "链接", JA = "リンク", EN = "Link" },
                new LocalizationResource { Key = "LBL_FILE_PATH", ZH = "文件路径", JA = "ファイルパス", EN = "File Path" },
                new LocalizationResource { Key = "LBL_RDS", ZH = "RDS连接", JA = "RDS接続", EN = "RDS" },
                new LocalizationResource { Key = "LBL_PRIORITY", ZH = "优先级", JA = "優先度", EN = "Priority" },
                new LocalizationResource { Key = "ACT_MAIL", ZH = "发邮件", JA = "メール送信", EN = "Mail" },
                new LocalizationResource { Key = "ACT_OPEN", ZH = "打开", JA = "開く", EN = "Open" },
                new LocalizationResource { Key = "ACT_COPY", ZH = "复制", JA = "コピー", EN = "Copy" },
                new LocalizationResource { Key = "ACT_COPY_PATH", ZH = "复制路径", JA = "パスをコピー", EN = "Copy Path" },
                new LocalizationResource { Key = "ACT_DOWNLOAD_RDP", ZH = "下载RDP", JA = "RDPダウンロード", EN = "Download RDP" }
            );
        }

        await db.SaveChangesAsync();

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

