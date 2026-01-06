using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityDefinitionEndpoints 字段级 CRUD 深度测试（Phase 6）
/// 重点覆盖：新增/删除/修改字段，Source=System/Interface 的保护策略，以及 Enum 配置写入/清理。
/// </summary>
public class EntityDefinitionFieldCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private static readonly TimeSpan SqliteTimestampTickDelay = TimeSpan.FromMilliseconds(1100);

    public EntityDefinitionFieldCrudTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static CreateEntityDefinitionDto CreateValidEntityDefinitionDto(string namespaceName, string entityName)
        => new CreateEntityDefinitionDto
        {
            Namespace = namespaceName,
            EntityName = entityName,
            DisplayName = new MultilingualText { ["zh"] = "测试实体", ["en"] = "Test Entity", ["ja"] = "テスト" },
            Description = new MultilingualText { ["zh"] = "测试描述", ["en"] = "Description" },
            Fields =
            [
                new CreateFieldMetadataDto
                {
                    PropertyName = "Code",
                    DisplayName = new MultilingualText { ["zh"] = "编码", ["en"] = "Code", ["ja"] = "コード" },
                    DataType = "String",
                    IsRequired = true,
                    Length = 64,
                    SortOrder = 1
                }
            ]
        };

    private static CreateEntityDefinitionDto CreateEntityDefinitionDtoWithTwoCustomFields(string namespaceName, string entityName)
        => new CreateEntityDefinitionDto
        {
            Namespace = namespaceName,
            EntityName = entityName,
            DisplayName = new MultilingualText { ["zh"] = "测试实体", ["en"] = "Test Entity", ["ja"] = "テスト" },
            Description = new MultilingualText { ["zh"] = "测试描述", ["en"] = "Description" },
            Fields =
            [
                new CreateFieldMetadataDto
                {
                    PropertyName = "Code",
                    DisplayName = new MultilingualText { ["zh"] = "编码", ["en"] = "Code", ["ja"] = "コード" },
                    DataType = FieldDataType.String,
                    IsRequired = true,
                    Length = 64,
                    SortOrder = 1
                },
                new CreateFieldMetadataDto
                {
                    PropertyName = "Description",
                    DisplayName = new MultilingualText { ["zh"] = "描述", ["en"] = "Description", ["ja"] = "説明" },
                    DataType = FieldDataType.String,
                    Length = 200,
                    SortOrder = 2
                }
            ]
        };

    private static Task WaitForSqliteTimestampTickAsync() => Task.Delay(SqliteTimestampTickDelay);

    [Fact]
    public async Task UpdateEntityDefinition_CustomFieldUpdate_ShouldPersistEditableProperties()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        await WaitForSqliteTimestampTickAsync();

        Guid fieldId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created!.Id);
            entity.Fields.Should().ContainSingle();
            fieldId = entity.Fields.Single().Id;
        }

        var update = new UpdateEntityDefinitionDto
        {
            Description = new MultilingualText { ["zh"] = $"更新描述-{Guid.NewGuid():N}" },
            Fields =
            [
                new UpdateFieldMetadataDto
                {
                    Id = fieldId,
                    PropertyName = "Code2",
                    DisplayName = new MultilingualText { ["zh"] = "编码2", ["en"] = "Code2", ["ja"] = "コード2" },
                    DataType = FieldDataType.String,
                    Length = 128,
                    IsRequired = false,
                    SortOrder = 99,
                    DefaultValue = "DEFAULT",
                    ValidationRules = "{\"minLength\":1}"
                }
            ]
        };

        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created.Id);
            var stored = entity.Fields.Single(x => x.Id == fieldId);
            stored.PropertyName.Should().Be("Code2");
            stored.DataType.Should().Be(FieldDataType.String);
            stored.Length.Should().Be(128);
            stored.IsRequired.Should().BeFalse();
            stored.SortOrder.Should().Be(99);
            stored.DefaultValue.Should().Be("DEFAULT");
            stored.ValidationRules.Should().Be("{\"minLength\":1}");
            stored.IsDeleted.Should().BeFalse();
            stored.Source.Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateEntityDefinition_RemovingCustomField_ShouldSoftDeleteRemoved()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateEntityDefinitionDtoWithTwoCustomFields("BobCrm.Test", uniqueName));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        await WaitForSqliteTimestampTickAsync();

        Guid codeFieldId;
        Guid descriptionFieldId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created!.Id);
            entity.Fields.Should().HaveCount(2);
            codeFieldId = entity.Fields.Single(x => x.PropertyName == "Code").Id;
            descriptionFieldId = entity.Fields.Single(x => x.PropertyName == "Description").Id;
        }

        var removeDescriptionUpdate = new UpdateEntityDefinitionDto
        {
            Description = new MultilingualText { ["zh"] = $"更新描述-{Guid.NewGuid():N}" },
            Fields =
            [
                new UpdateFieldMetadataDto { Id = codeFieldId, PropertyName = "Code", SortOrder = 10 }
            ]
        };

        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", removeDescriptionUpdate);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created.Id);
            entity.Fields.Should().Contain(x => x.Id == codeFieldId && !x.IsDeleted);
            entity.Fields.Should().Contain(x => x.Id == descriptionFieldId && x.IsDeleted);
        }
    }

    [Fact]
    public async Task UpdateEntityDefinition_RemovingProtectedSystemField_ShouldReturn400WithCode()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created!.Id);
            var field = entity.Fields.Single();
            field.Source = FieldSource.System;
            await db.SaveChangesAsync();
        }

        var update = new UpdateEntityDefinitionDto
        {
            Fields = []
        };

        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var root = await response.ReadAsJsonAsync();
        root.TryGetProperty("code", out var code).Should().BeTrue();
        code.GetString().Should().Be("FIELD_PROTECTED_BY_SOURCE");
    }

    [Fact]
    public async Task UpdateEntityDefinition_SystemFieldUpdate_ShouldOnlyUpdateDisplayNameAndSortOrder()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        await WaitForSqliteTimestampTickAsync();

        Guid fieldId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created!.Id);
            var field = entity.Fields.Single();
            field.Source = FieldSource.System;
            fieldId = field.Id;
            await db.SaveChangesAsync();
        }

        var update = new UpdateEntityDefinitionDto
        {
            Description = new MultilingualText { ["zh"] = $"更新描述-{Guid.NewGuid():N}" },
            Fields =
            [
                new UpdateFieldMetadataDto
                {
                    Id = fieldId,
                    PropertyName = "ShouldNotChange",
                    DataType = FieldDataType.Int32,
                    Length = 1,
                    DefaultValue = "X",
                    DisplayName = new MultilingualText { ["zh"] = "系统字段(可改名)", ["en"] = "System Field", ["ja"] = "システム項目" },
                    SortOrder = 123
                }
            ]
        };

        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created.Id);
            var stored = entity.Fields.Single(x => x.Id == fieldId);
            stored.PropertyName.Should().Be("Code");
            stored.DataType.Should().Be(FieldDataType.String);
            stored.Length.Should().Be(64);
            stored.DefaultValue.Should().BeNull();
            stored.SortOrder.Should().Be(123);
            stored.DisplayName.Should().NotBeNull();
            stored.DisplayName!.GetValueOrDefault("zh").Should().Be("系统字段(可改名)");
        }
    }

    [Fact]
    public async Task UpdateEntityDefinition_InterfaceFieldUpdate_ShouldOnlyUpdateDisplayNameSortOrderAndDefaultValue()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/api/entity-definitions", CreateValidEntityDefinitionDto("BobCrm.Test", uniqueName));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        await WaitForSqliteTimestampTickAsync();

        Guid fieldId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created!.Id);
            var field = entity.Fields.Single();
            field.Source = FieldSource.Interface;
            field.DefaultValue = "A";
            fieldId = field.Id;
            await db.SaveChangesAsync();
        }

        var update = new UpdateEntityDefinitionDto
        {
            Description = new MultilingualText { ["zh"] = $"更新描述-{Guid.NewGuid():N}" },
            Fields =
            [
                new UpdateFieldMetadataDto
                {
                    Id = fieldId,
                    PropertyName = "ShouldNotChange",
                    DataType = FieldDataType.Int32,
                    Length = 1,
                    DisplayName = new MultilingualText { ["zh"] = "接口字段(可改名)", ["en"] = "Interface Field", ["ja"] = "インターフェース項目" },
                    SortOrder = 321,
                    DefaultValue = "B"
                }
            ]
        };

        var response = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.EntityDefinitions.Include(x => x.Fields).SingleAsync(x => x.Id == created.Id);
            var stored = entity.Fields.Single(x => x.Id == fieldId);
            stored.PropertyName.Should().Be("Code");
            stored.DataType.Should().Be(FieldDataType.String);
            stored.Length.Should().Be(64);
            stored.DefaultValue.Should().Be("B");
            stored.SortOrder.Should().Be(321);
            stored.DisplayName.Should().NotBeNull();
            stored.DisplayName!.GetValueOrDefault("zh").Should().Be("接口字段(可改名)");
        }
    }

    [Fact]
    public async Task UpdateEntityDefinition_ChangingEnumToNonEnum_ShouldClearEnumConfig()
    {
        var client = await GetAuthenticatedClientAsync();
        var uniqueName = $"FieldCrud_{Guid.NewGuid():N}";

        Guid enumId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var enumDef = new EnumDefinition
            {
                Code = $"enum_{Guid.NewGuid():N}",
                DisplayName = new Dictionary<string, string?> { ["zh"] = "枚举", ["en"] = "Enum", ["ja"] = "列挙" },
                Description = new Dictionary<string, string?>(),
                IsEnabled = true
            };
            db.EnumDefinitions.Add(enumDef);
            await db.SaveChangesAsync();
            enumId = enumDef.Id;
        }

        var createDto = new CreateEntityDefinitionDto
        {
            Namespace = "BobCrm.Test",
            EntityName = uniqueName,
            DisplayName = new MultilingualText { ["zh"] = "枚举实体" },
            Fields =
            [
                new CreateFieldMetadataDto
                {
                    PropertyName = "Status",
                    DisplayName = new MultilingualText { ["zh"] = "状态" },
                    DataType = FieldDataType.Enum,
                    EnumDefinitionId = enumId,
                    IsMultiSelect = true,
                    SortOrder = 1,
                    IsRequired = true
                }
            ]
        };

        var create = await client.PostAsJsonAsync("/api/entity-definitions", createDto);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync<EntityDefinitionDto>();
        created.Should().NotBeNull();

        await WaitForSqliteTimestampTickAsync();

        Guid statusFieldId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var field = await db.FieldMetadatas.SingleAsync(f => f.EntityDefinitionId == created!.Id && f.PropertyName == "Status");
            field.DataType.Should().Be(FieldDataType.Enum);
            field.EnumDefinitionId.Should().Be(enumId);
            field.IsMultiSelect.Should().BeTrue();
            statusFieldId = field.Id;
        }

        var toNonEnumUpdate = new UpdateEntityDefinitionDto
        {
            Description = new MultilingualText { ["zh"] = $"更新描述-{Guid.NewGuid():N}" },
            Fields =
            [
                new UpdateFieldMetadataDto
                {
                    Id = statusFieldId,
                    PropertyName = "Status",
                    DataType = FieldDataType.String,
                    Length = 20
                }
            ]
        };

        var nonEnumResp = await client.PutAsJsonAsync($"/api/entity-definitions/{created!.Id}", toNonEnumUpdate);
        nonEnumResp.StatusCode.Should().Be(HttpStatusCode.OK, await nonEnumResp.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var field = await db.FieldMetadatas.SingleAsync(f => f.Id == statusFieldId);
            field.DataType.Should().Be(FieldDataType.String);
            field.EnumDefinitionId.Should().BeNull();
            field.IsMultiSelect.Should().BeFalse();
        }
    }
}
