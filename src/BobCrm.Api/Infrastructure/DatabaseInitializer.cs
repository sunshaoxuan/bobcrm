using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BobCrm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(DbContext db)
    {
        // First try to create schema from model (dev-friendly)
        try { await db.Database.EnsureCreatedAsync(); } catch { }
        // If __EFMigrationsHistory exists but tables not created, force create tables
        try { await db.Set<Customer>().CountAsync(); }
        catch
        {
            try
            {
                var creator = db.Database.GetService<IRelationalDatabaseCreator>();
                await creator.CreateTablesAsync();
            }
            catch { }
        }
        // If there are code-based migrations, apply them (no-op if none)
        var hasMigrations = db.Database.GetMigrations().Any();
        if (hasMigrations)
            await db.Database.MigrateAsync();

        if (!await db.Set<Customer>().AnyAsync())
        {
            await db.Set<Customer>().AddRangeAsync(
                new Customer { Code = "C001", Name = "客户A", Version = 1 },
                new Customer { Code = "C002", Name = "客户B", Version = 1 }
            );
            if (!await db.Set<FieldDefinition>().AnyAsync())
            {
                await db.Set<FieldDefinition>().AddAsync(new FieldDefinition
                {
                    Key = "email",
                    DisplayName = "邮箱",
                    DataType = "email",
                    Required = true,
                    Validation = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    DefaultValue = "",
                    Tags = "[\"常用\"]",
                    Actions = "[{\"icon\":\"mail\",\"title\":\"发邮件\",\"type\":\"click\",\"action\":\"mailto\"}]"
                });
            }
            await db.SaveChangesAsync();
        }

        var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
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
        await db.Database.EnsureDeletedAsync();
        await InitializeAsync(db);
    }
}
