using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

public class EntityDefinitionFieldConsistencyTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public EntityDefinitionFieldConsistencyTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.UseBearer(accessToken);
        return client;
    }

    private async Task<Guid> SeedEnumDefinitionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var enumDef = new EnumDefinition
        {
            Code = $"test_enum_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "测试枚举" }, { "ja", "テスト列挙" }, { "en", "Test Enum" } },
            IsSystem = false,
            IsEnabled = true
        };

        db.EnumDefinitions.Add(enumDef);
        await db.SaveChangesAsync();
        return enumDef.Id;
    }

    private async Task<(Guid EntityId, Guid SystemFieldId, Guid InterfaceFieldId, Guid CustomFieldId)> SeedEntityWithSourceFieldsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entityId = Guid.NewGuid();
        var entityName = $"TestEntity_{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;

        var definition = new EntityDefinition
        {
            Id = entityId,
            Namespace = "BobCrm.Test",
            EntityName = entityName,
            FullTypeName = $"BobCrm.Test.{entityName}",
            EntityRoute = entityName.ToLowerInvariant(),
            ApiEndpoint = $"/api/{entityName.ToLowerInvariant()}",
            StructureType = "Single",
            Status = EntityStatus.Draft,
            Source = "Custom",
            IsEnabled = true,
            IsRootEntity = true,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "测试实体", ["en"] = "Test Entity" },
            CreatedAt = now,
            UpdatedAt = now
        };

        var systemFieldId = Guid.NewGuid();
        definition.Fields.Add(new FieldMetadata
        {
            Id = systemFieldId,
            EntityDefinitionId = definition.Id,
            PropertyName = "Id",
            DataType = FieldDataType.Guid,
            IsRequired = true,
            SortOrder = 0,
            DefaultValue = "sys_default",
            Source = FieldSource.System,
            CreatedAt = now,
            UpdatedAt = now
        });

        var interfaceFieldId = Guid.NewGuid();
        definition.Fields.Add(new FieldMetadata
        {
            Id = interfaceFieldId,
            EntityDefinitionId = definition.Id,
            PropertyName = "Code",
            DisplayNameKey = "LBL_FIELD_CODE",
            DataType = FieldDataType.String,
            Length = 64,
            IsRequired = true,
            SortOrder = 1,
            DefaultValue = "if_default",
            Source = FieldSource.Interface,
            CreatedAt = now,
            UpdatedAt = now
        });

        var customFieldId = Guid.NewGuid();
        definition.Fields.Add(new FieldMetadata
        {
            Id = customFieldId,
            EntityDefinitionId = definition.Id,
            PropertyName = "CustomField",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "自定义字段", ["en"] = "Custom Field" },
            DataType = FieldDataType.String,
            Length = 100,
            IsRequired = false,
            SortOrder = 2,
            DefaultValue = "custom_default",
            Source = FieldSource.Custom,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.EntityDefinitions.Add(definition);
        await db.SaveChangesAsync();
        return (definition.Id, systemFieldId, interfaceFieldId, customFieldId);
    }

    [Fact]
    public async Task UpdateEntityDefinition_SourceProtection_Enforced()
    {
        var (entityId, systemFieldId, interfaceFieldId, customFieldId) = await SeedEntityWithSourceFieldsAsync();
        var client = await CreateAuthenticatedClientAsync();

        var dto = new UpdateEntityDefinitionDto
        {
            Fields = new List<UpdateFieldMetadataDto>
            {
                new()
                {
                    Id = systemFieldId,
                    DataType = FieldDataType.String,
                    IsRequired = false,
                    DefaultValue = "should_not_apply",
                    SortOrder = 10,
                    DisplayName = new MultilingualText { ["zh"] = "系统字段-改名" }
                },
                new()
                {
                    Id = interfaceFieldId,
                    IsRequired = false,
                    DefaultValue = "interface_default_changed",
                    SortOrder = 11,
                    DisplayName = new MultilingualText { ["zh"] = "接口字段-改名" }
                },
                new()
                {
                    Id = customFieldId,
                    DataType = FieldDataType.Int32,
                    IsRequired = true,
                    DefaultValue = "custom_default_changed",
                    SortOrder = 12,
                    DisplayName = new MultilingualText { ["zh"] = "自定义字段-改名" }
                }
            }
        };

        var resp = await client.PutAsJsonAsync($"/api/entity-definitions/{entityId}", dto);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.EntityDefinitions
            .Include(ed => ed.Fields)
            .FirstAsync(ed => ed.Id == entityId);

        var systemField = updated.Fields.First(f => f.Id == systemFieldId);
        Assert.Equal(FieldSource.System, systemField.Source);
        Assert.Equal(FieldDataType.Guid, systemField.DataType);
        Assert.True(systemField.IsRequired);
        Assert.Equal("sys_default", systemField.DefaultValue);
        Assert.Equal(10, systemField.SortOrder);
        Assert.Equal("系统字段-改名", systemField.DisplayName!["zh"]);

        var interfaceField = updated.Fields.First(f => f.Id == interfaceFieldId);
        Assert.Equal(FieldSource.Interface, interfaceField.Source);
        Assert.Equal(FieldDataType.String, interfaceField.DataType);
        Assert.True(interfaceField.IsRequired);
        Assert.Equal("interface_default_changed", interfaceField.DefaultValue);
        Assert.Equal(11, interfaceField.SortOrder);
        Assert.Equal("接口字段-改名", interfaceField.DisplayName!["zh"]);

        var customField = updated.Fields.First(f => f.Id == customFieldId);
        Assert.Equal(FieldSource.Custom, customField.Source);
        Assert.Equal(FieldDataType.Int32, customField.DataType);
        Assert.True(customField.IsRequired);
        Assert.Equal("custom_default_changed", customField.DefaultValue);
        Assert.Equal(12, customField.SortOrder);
        Assert.Equal("自定义字段-改名", customField.DisplayName!["zh"]);
    }

    [Fact]
    public async Task UpdateEntityDefinition_DeleteProtectedFields_ReturnsErrFieldProtectedBySource()
    {
        var (entityId, systemFieldId, interfaceFieldId, customFieldId) = await SeedEntityWithSourceFieldsAsync();
        var client = await CreateAuthenticatedClientAsync();

        var dto = new UpdateEntityDefinitionDto
        {
            Fields = new List<UpdateFieldMetadataDto>
            {
                new() { Id = customFieldId }
            }
        };

        var resp = await client.PutAsJsonAsync($"/api/entity-definitions/{entityId}", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var err = await resp.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(err);
        Assert.Equal(ErrorCodes.FieldProtectedBySource, err!.Code);
    }

    [Fact]
    public async Task CreateAndUpdateEntityDefinition_EnumField_PersistsEnumConfig()
    {
        var enumId = await SeedEnumDefinitionAsync();
        var client = await CreateAuthenticatedClientAsync();

        var entityName = $"EnumEntity_{Guid.NewGuid():N}";
        var create = new CreateEntityDefinitionDto
        {
            Namespace = "BobCrm.Test",
            EntityName = entityName,
            DisplayName = new MultilingualText { ["zh"] = "枚举实体" },
            Fields = new List<CreateFieldMetadataDto>
            {
                new()
                {
                    PropertyName = "Status",
                    DisplayName = new MultilingualText { ["zh"] = "状态" },
                    DataType = FieldDataType.Enum,
                    IsRequired = true,
                    SortOrder = 1,
                    EnumDefinitionId = enumId,
                    IsMultiSelect = true
                }
            }
        };

        var createResp = await client.PostAsJsonAsync("/api/entity-definitions", create);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.ReadAsJsonAsync();
        var entityId = Guid.Parse(created.UnwrapData().GetProperty("id").GetString()!);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var field = await db.FieldMetadatas.FirstAsync(f => f.EntityDefinitionId == entityId && f.PropertyName == "Status");
            Assert.Equal(FieldDataType.Enum, field.DataType);
            Assert.Equal(enumId, field.EnumDefinitionId);
            Assert.True(field.IsMultiSelect);
        }

        // Change Enum -> String should clear enum config
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingFields = await db.FieldMetadatas
                .Where(f => f.EntityDefinitionId == entityId)
                .ToListAsync();

            var updateFields = existingFields.Select(f => new UpdateFieldMetadataDto
            {
                Id = f.Id,
                PropertyName = f.PropertyName,
                DisplayName = f.DisplayName != null ? new MultilingualText(f.DisplayName) : null,
                DataType = f.DataType,
                Length = f.Length,
                Precision = f.Precision,
                Scale = f.Scale,
                IsRequired = f.IsRequired,
                SortOrder = f.SortOrder,
                DefaultValue = f.DefaultValue,
                ValidationRules = f.ValidationRules,
                EnumDefinitionId = f.EnumDefinitionId,
                IsMultiSelect = f.IsMultiSelect
            }).ToList();

            // Find Status field and modify
            var statusIndex = updateFields.FindIndex(f => f.PropertyName == "Status");
            updateFields[statusIndex] = updateFields[statusIndex] with { DataType = FieldDataType.String };

            var update = new UpdateEntityDefinitionDto
            {
                Fields = updateFields
            };

            var updateResp = await client.PutAsJsonAsync($"/api/entity-definitions/{entityId}", update);
            Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var field = await db.FieldMetadatas.FirstAsync(f => f.EntityDefinitionId == entityId && f.PropertyName == "Status");
            Assert.Equal(FieldDataType.String, field.DataType);
            Assert.Null(field.EnumDefinitionId);
            Assert.False(field.IsMultiSelect);
        }
    }
}
