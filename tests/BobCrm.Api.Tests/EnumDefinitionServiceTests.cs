using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Enum;
using BobCrm.Api.Contracts.Requests.Enum;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EnumDefinitionService 单元测试
/// 目标覆盖率：90%+
/// </summary>
public class EnumDefinitionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly EnumDefinitionService _service;

    public EnumDefinitionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new EnumDefinitionService(_db, NullLogger<EnumDefinitionService>.Instance);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEnums_WhenIncludeDisabledIsFalse()
    {
        // Arrange
        await SeedTestEnumsAsync();

        // Act
        var result = await _service.GetAllAsync(includeDisabled: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only enabled enums
        Assert.All(result, e => Assert.True(e.IsEnabled));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEnums_WhenIncludeDisabledIsTrue()
    {
        // Arrange
        await SeedTestEnumsAsync();

        // Act
        var result = await _service.GetAllAsync(includeDisabled: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // All enums including disabled
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoEnums()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithLang_ReturnsSingleLanguage()
    {
        // Arrange
        await SeedTestEnumsAsync();

        // Act
        var result = await _service.GetAllAsync(includeDisabled: false, lang: "zh");

        // Assert
        Assert.All(result, e =>
        {
            Assert.NotNull(e.DisplayName);
            Assert.Null(e.DisplayNameTranslations);
        });
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsEnum_WhenExists()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("test_enum");

        // Act
        var result = await _service.GetByIdAsync(enumDef.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(enumDef.Id, result.Id);
        Assert.Equal("test_enum", result.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByCodeAsync Tests

    [Fact]
    public async Task GetByCodeAsync_ReturnsEnum_WhenExists()
    {
        // Arrange
        await CreateTestEnumAsync("test_code");

        // Act
        var result = await _service.GetByCodeAsync("test_code");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_code", result.Code);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetByCodeAsync("non_existent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetOptionsAsync Tests

    [Fact]
    public async Task GetOptionsAsync_ReturnsEnabledOptions_WhenIncludeDisabledIsFalse()
    {
        // Arrange
        var enumDef = await CreateTestEnumWithOptionsAsync();

        // Act
        var result = await _service.GetOptionsAsync(enumDef.Id, includeDisabled: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only enabled options
        Assert.All(result, o => Assert.True(o.IsEnabled));
    }

    [Fact]
    public async Task GetOptionsAsync_ReturnsAllOptions_WhenIncludeDisabledIsTrue()
    {
        // Arrange
        var enumDef = await CreateTestEnumWithOptionsAsync();

        // Act
        var result = await _service.GetOptionsAsync(enumDef.Id, includeDisabled: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // All options
    }

    [Fact]
    public async Task GetOptionsAsync_ReturnsSortedOptions()
    {
        // Arrange
        var enumDef = await CreateTestEnumWithOptionsAsync();

        // Act
        var result = await _service.GetOptionsAsync(enumDef.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("VALUE1", result[0].Value);
        Assert.Equal("VALUE2", result[1].Value);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesEnum_WithValidRequest()
    {
        // Arrange
        var request = new CreateEnumDefinitionRequest
        {
            Code = "new_enum",
            DisplayName = new() { { "zh", "新枚举" }, { "en", "New Enum" } },
            Description = new() { { "zh", "测试" } },
            Options = new List<CreateEnumOptionRequest>
            {
                new() { Value = "OPT1", DisplayName = new() { { "zh", "选项1" } }, SortOrder = 0 },
                new() { Value = "OPT2", DisplayName = new() { { "zh", "选项2" } }, SortOrder = 1 }
            }
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_enum", result.Code);
        Assert.False(result.IsSystem);
        Assert.True(result.IsEnabled);
        Assert.Equal(2, result.Options.Count);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenCodeAlreadyExists()
    {
        // Arrange
        await CreateTestEnumAsync("duplicate");
        var request = new CreateEnumDefinitionRequest
        {
            Code = "duplicate",
            DisplayName = new() { { "zh", "重复" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesEnum_WithValidRequest()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("update_test", isSystem: false);
        var request = new UpdateEnumDefinitionRequest
        {
            DisplayName = new() { { "zh", "更新后" }, { "en", "Updated" } },
            Description = new() { { "zh", "新描述" } },
            IsEnabled = false
        };

        // Act
        var result = await _service.UpdateAsync(enumDef.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DisplayName);
        Assert.NotNull(result.DisplayNameTranslations);
        Assert.Equal("更新后", result.DisplayNameTranslations!["zh"]);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_ForSystemEnum()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("system_enum", isSystem: true);
        var request = new UpdateEnumDefinitionRequest
        {
            DisplayName = new() { { "zh", "尝试修改" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(enumDef.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEnumNotExists()
    {
        // Arrange
        var request = new UpdateEnumDefinitionRequest
        {
            DisplayName = new() { { "zh", "不存在" } }
        };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DeletesEnum_WhenNotSystemAndNotReferenced()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("delete_test", isSystem: false);

        // Act
        var result = await _service.DeleteAsync(enumDef.Id);

        // Assert
        Assert.True(result);
        var deletedEnum = await _db.EnumDefinitions.FindAsync(enumDef.Id);
        Assert.Null(deletedEnum);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_ForSystemEnum()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("system_delete", isSystem: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(enumDef.Id));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenReferencedByField()
    {
        // Arrange
        var enumDef = await CreateTestEnumAsync("referenced", isSystem: false);
        
        // Create an entity and field that references this enum
        var entity = new EntityDefinition
        {
            EntityName = "TestEntity",
            Namespace = "Test",
            EntityRoute = "test",
            Status = EntityStatus.Draft
        };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var field = new FieldMetadata
        {
            EntityDefinitionId = entity.Id,
            PropertyName = "TestField",
            DataType = "Enum",
            EnumDefinitionId = enumDef.Id
        };
        _db.FieldMetadatas.Add(field);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(enumDef.Id));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEnumNotExists()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UpdateOptionsAsync Tests

    [Fact]
    public async Task UpdateOptionsAsync_UpdatesOptions_WithValidRequest()
    {
        // Arrange
        var enumDef = await CreateTestEnumWithOptionsAsync(isSystem: false);
        var option = enumDef.Options.First();
        
        var request = new UpdateEnumOptionsRequest
        {
            Options = new List<UpdateEnumOptionRequest>
            {
                new()
                {
                    Id = option.Id,
                    DisplayName = new() { { "zh", "更新选项" } },
                    SortOrder = 99,
                    IsEnabled = false,
                    ColorTag = "red"
                }
            }
        };

        // Act
        var result = await _service.UpdateOptionsAsync(enumDef.Id, request);

        // Assert
        Assert.NotNull(result);
        var updated = result.First(o => o.Id == option.Id);
        Assert.Null(updated.DisplayName);
        Assert.NotNull(updated.DisplayNameTranslations);
        Assert.Equal("更新选项", updated.DisplayNameTranslations!["zh"]);
        Assert.Equal(99, updated.SortOrder);
        Assert.False(updated.IsEnabled);
        Assert.Equal("red", updated.ColorTag);
    }

    [Fact]
    public async Task UpdateOptionsAsync_ThrowsException_ForSystemEnum()
    {
        // Arrange
        var enumDef = await CreateTestEnumWithOptionsAsync(isSystem: true);
        var request = new UpdateEnumOptionsRequest { Options = new() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateOptionsAsync(enumDef.Id, request));
    }

    [Fact]
    public async Task UpdateOptionsAsync_ThrowsException_WhenEnumNotExists()
    {
        // Arrange
        var request = new UpdateEnumOptionsRequest { Options = new() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateOptionsAsync(Guid.NewGuid(), request));
    }

    #endregion

    #region Helper Methods

    private async Task<EnumDefinition> CreateTestEnumAsync(string code, bool isSystem = false)
    {
        var enumDef = new EnumDefinition
        {
            Code = code,
            DisplayName = new() { { "zh", $"测试_{code}" }, { "en", $"Test_{code}" } },
            IsSystem = isSystem,
            IsEnabled = true
        };

        _db.EnumDefinitions.Add(enumDef);
        await _db.SaveChangesAsync();
        return enumDef;
    }

    private async Task<EnumDefinition> CreateTestEnumWithOptionsAsync(bool isSystem = false)
    {
        var enumDef = new EnumDefinition
        {
            Code = "test_with_options",
            DisplayName = new() { { "zh", "带选项测试" } },
            IsSystem = isSystem,
            IsEnabled = true,
            Options = new List<EnumOption>
            {
                new() { Value = "VALUE1", DisplayName = new() { { "zh", "值1" } }, SortOrder = 0, IsEnabled = true },
                new() { Value = "VALUE2", DisplayName = new() { { "zh", "值2" } }, SortOrder = 1, IsEnabled = true },
                new() { Value = "VALUE3", DisplayName = new() { { "zh", "值3" } }, SortOrder = 2, IsEnabled = false }
            }
        };

        _db.EnumDefinitions.Add(enumDef);
        await _db.SaveChangesAsync();
        return enumDef;
    }

    private async Task SeedTestEnumsAsync()
    {
        var enums = new[]
        {
            new EnumDefinition { Code = "enum1", DisplayName = new() { { "zh", "枚举1" } }, IsSystem = false, IsEnabled = true },
            new EnumDefinition { Code = "enum2", DisplayName = new() { { "zh", "枚举2" } }, IsSystem = true, IsEnabled = true },
            new EnumDefinition { Code = "enum3", DisplayName = new() { { "zh", "枚举3" } }, IsSystem = false, IsEnabled = false }
        };

        _db.EnumDefinitions.AddRange(enums);
        await _db.SaveChangesAsync();
    }

    #endregion
}
