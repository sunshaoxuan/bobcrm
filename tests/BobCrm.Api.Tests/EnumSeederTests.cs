using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EnumSeeder 单元测试
/// 确保系统枚举正确初始化
/// </summary>
public class EnumSeederTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly EnumSeeder _seeder;

    public EnumSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _seeder = new EnumSeeder(_db);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_CreatesAllSystemEnums()
    {
        // Act
        await _seeder.EnsureSystemEnumsAsync();

        // Assert
        var enums = await _db.EnumDefinitions.Include(e => e.Options).ToListAsync();
        
        Assert.NotEmpty(enums);
        Assert.All(enums, e => Assert.True(e.IsSystem));
        Assert.All(enums, e => Assert.True(e.IsEnabled));
        
        // 验证预期的系统枚举都存在
        Assert.Contains(enums, e => e.Code == "form_template_usage");
        Assert.Contains(enums, e => e.Code == "layout_mode");
        Assert.Contains(enums, e => e.Code == "detail_display_mode");
        Assert.Contains(enums, e => e.Code == "modal_size");
        Assert.Contains(enums, e => e.Code == "boolean");
        Assert.Contains(enums, e => e.Code == "gender");
        Assert.Contains(enums, e => e.Code == "priority");
        Assert.Contains(enums, e => e.Code == "status");
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_CreatesFormTemplateUsageEnum()
    {
        // Act
        await _seeder.EnsureSystemEnumsAsync();

        // Assert
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "form_template_usage");

        Assert.NotNull(enumDef);
        Assert.True(enumDef.IsSystem);
        Assert.Contains("表单模板使用类型", enumDef.DisplayName["zh"]);
        
        // 验证选项
        Assert.Equal(4, enumDef.Options.Count);
        Assert.Contains(enumDef.Options, o => o.Value == "DETAIL");
        Assert.Contains(enumDef.Options, o => o.Value == "EDIT");
        Assert.Contains(enumDef.Options, o => o.Value == "LIST");
        Assert.Contains(enumDef.Options, o => o.Value == "COMBINED");
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_CreatesPriorityEnumWithColors()
    {
        // Act
        await _seeder.EnsureSystemEnumsAsync();

        // Assert
        var enumDef = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == "priority");

        Assert.NotNull(enumDef);
        
        var lowOption = enumDef.Options.FirstOrDefault(o => o.Value == "LOW");
        var highOption = enumDef.Options.FirstOrDefault(o => o.Value == "HIGH");
        var urgentOption = enumDef.Options.FirstOrDefault(o => o.Value == "URGENT");

        Assert.NotNull(lowOption);
        Assert.NotNull(highOption);
        Assert.NotNull(urgentOption);
        
        Assert.Equal("green", lowOption.ColorTag);
        Assert.Equal("orange", highOption.ColorTag);
        Assert.Equal("red", urgentOption.ColorTag);
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_IsIdempotent()
    {
        // Act - 运行两次
        await _seeder.EnsureSystemEnumsAsync();
        var countAfterFirst = await _db.EnumDefinitions.CountAsync();
        
        await _seeder.EnsureSystemEnumsAsync();
        var countAfterSecond = await _db.EnumDefinitions.CountAsync();

        // Assert - 数量应该相同
        Assert.Equal(countAfterFirst, countAfterSecond);
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_UpdatesDisplayNamesOnRerun()
    {
        // Arrange - 首次运行
        await _seeder.EnsureSystemEnumsAsync();
        
        // 修改一个枚举的显示名
        var enumDef = await _db.EnumDefinitions.FirstAsync(e => e.Code == "boolean");
        enumDef.DisplayName["zh"] = "旧名称";
        await _db.SaveChangesAsync();

        // Act - 再次运行
        await _seeder.EnsureSystemEnumsAsync();

        // Assert - 显示名应该被更新
        var updated = await _db.EnumDefinitions.FirstAsync(e => e.Code == "boolean");
        Assert.Equal("是/否", updated.DisplayName["zh"]);
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_CreatesMultilingualDisplayNames()
    {
        // Act
        await _seeder.EnsureSystemEnumsAsync();

        // Assert - 所有枚举都应该有中英日三语
        var enums = await _db.EnumDefinitions.Include(e => e.Options).ToListAsync();
        
        foreach (var enumDef in enums)
        {
            Assert.True(enumDef.DisplayName.ContainsKey("zh"), $"Enum {enumDef.Code} missing Chinese");
            Assert.True(enumDef.DisplayName.ContainsKey("en"), $"Enum {enumDef.Code} missing English");
            Assert.True(enumDef.DisplayName.ContainsKey("ja"), $"Enum {enumDef.Code} missing Japanese");
            
            foreach (var option in enumDef.Options)
            {
                Assert.True(option.DisplayName.ContainsKey("zh"), $"Option {option.Value} missing Chinese");
            }
        }
    }

    [Fact]
    public async Task EnsureSystemEnumsAsync_SetCorrectSortOrders()
    {
        // Act
        await _seeder.EnsureSystemEnumsAsync();

        // Assert
        var layoutMode = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstAsync(e => e.Code == "layout_mode");

        var sortedOptions = layoutMode.Options.OrderBy(o => o.SortOrder).ToList();
        Assert.Equal("LEFT_RIGHT_SPLIT", sortedOptions[0].Value);
        Assert.Equal("TOP_BOTTOM_SPLIT", sortedOptions[1].Value);
        Assert.Equal("LIST_ONLY", sortedOptions[2].Value);
    }
}
