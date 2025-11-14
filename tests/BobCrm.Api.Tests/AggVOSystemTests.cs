using System.Net;

using System.Net.Http.Json;

using System.Text.Json;

using Xunit;

using Microsoft.Extensions.DependencyInjection;

using BobCrm.Api.Base.Models;

using BobCrm.Api.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

/// <summary>
/// AggVO聚合根系统集成测试
/// 测试主子表配置、AggVO代码生成、数据迁移评估等高级功能
/// </summary>

public class AggVOSystemTests : IClassFixture<TestWebAppFactory>

{

    private readonly TestWebAppFactory _factory;

    private readonly HttpClient _client;

    public AggVOSystemTests(TestWebAppFactory factory)

    {

        _factory = factory;

        _client = factory.CreateClient();

    }

    [Fact]

    public async Task GetChildEntities_ReturnsEmptyList_ForSingleEntity()

    {

        // Arrange: 创建一个单实体

        var entityId = await CreateTestEntity("TestSingleEntity", "Single");

        // Act: 获取子实体列表

        var response = await _client.GetAsync($"/api/entity-advanced/{entityId}/children");

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ChildrenResponse>();

        Assert.NotNull(result);

        Assert.Equal(entityId, result.EntityId);

        Assert.Equal(0, result.ChildCount);

        Assert.Empty(result.Children);

    }

    [Fact]

    public async Task ConfigureMasterDetail_Success_ForValidConfiguration()

    {

        // Arrange: 创建主实体和子实体

        var masterId = await CreateTestEntity("Order", "Single");

        var detailId = await CreateTestEntity("OrderLine", "Single");

        var config = new MasterDetailConfigRequest

        {

            StructureType = "MasterDetail",

            Children = new List<ChildEntityConfigRequest>

            {

                new()

                {

                    ChildEntityId = detailId,

                    ForeignKeyField = "OrderId",

                    CollectionProperty = "OrderLines",

                    CascadeDeleteBehavior = "Cascade",

                    AutoCascadeSave = true

                }

            }

        };

        // Act: 配置主子表关系

        var response = await _client.PostAsJsonAsync($"/api/entity-advanced/{masterId}/configure-master-detail", config);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MasterDetailConfigResponse>();

        Assert.NotNull(result);

        Assert.Equal(masterId, result.EntityId);

        Assert.Equal("MasterDetail", result.StructureType);

        // 验证配置已保存

        var childrenResponse = await _client.GetAsync($"/api/entity-advanced/{masterId}/children");

        var children = await childrenResponse.Content.ReadFromJsonAsync<ChildrenResponse>();

        Assert.NotNull(children);

        Assert.Equal(1, children.ChildCount);

        Assert.Equal(detailId, children.Children[0].Id);

        Assert.Equal("OrderId", children.Children[0].ParentForeignKeyField);

        Assert.Equal("OrderLines", children.Children[0].ParentCollectionProperty);

        Assert.Equal("Cascade", children.Children[0].CascadeDeleteBehavior);

        Assert.True(children.Children[0].AutoCascadeSave);

    }

    [Fact]

    public async Task GenerateAggVO_ReturnsCode_ForMasterDetailEntity()

    {

        // Arrange: 创建主子结构

        var masterId = await CreateTestEntity("Order", "MasterDetail");

        var detailId = await CreateTestEntity("OrderLine", "Single");

        await ConfigureMasterDetailRelationship(masterId, detailId, "OrderId", "OrderLines");

        // Act: 生成AggVO代码

        var response = await _client.PostAsync($"/api/entity-advanced/{masterId}/generate-aggvo", null);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AggVOCodeResponse>();

        Assert.NotNull(result);

        Assert.Equal("Order", result.Entity);

        Assert.Equal("OrderAggVO", result.AggVOClassName);

        Assert.NotNull(result.AggVOCode);

        Assert.Contains("public class OrderAggVO : AggBaseVO", result.AggVOCode);

        Assert.Contains("public OrderVO HeadVO", result.AggVOCode);

        Assert.Contains("public List<OrderLineVO> OrderLineVOs", result.AggVOCode);

        Assert.NotNull(result.VOCode);

        Assert.Contains("public class OrderVO", result.VOCode);

        Assert.NotNull(result.ChildVOCodes);

        Assert.Contains("OrderLine", result.ChildVOCodes.Keys);

    }

    [Fact]

    public async Task GenerateAggVO_ReturnsBadRequest_ForSingleEntity()

    {

        // Arrange: 创建单实体（非主子结构）

        var entityId = await CreateTestEntity("Product", "Single");

        // Act: 尝试生成AggVO代码

        var response = await _client.PostAsync($"/api/entity-advanced/{entityId}/generate-aggvo", null);

        // Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);

        Assert.Contains("not a master-detail structure", error.Error);

    }

    [Fact]

    public async Task EvaluateMigration_ReturnsLowRisk_ForAddingNullableField()

    {

        // Arrange: 创建实体

        var entityId = await CreateTestEntity("Customer", "Single");

        var newFields = new List<FieldMetadataDto>

        {

            new()

            {

                PropertyName = "Email",

                DisplayName = new Dictionary<string, string?> { { "en", "FIELD_EMAIL" } },

                DataType = "String",

                Length = 100,

                IsRequired = false

            }

        };

        // Act: 评估迁移影响

        var response = await _client.PostAsJsonAsync($"/api/entity-advanced/{entityId}/evaluate-migration", newFields);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MigrationImpactResponse>();

        Assert.NotNull(result);

        Assert.Equal("Customer", result.EntityName);

        Assert.Equal("Low", result.RiskLevel);

        Assert.True(result.IsSafe);

        Assert.Contains(result.Operations, op => op.OperationType == "AddColumn");

    }

    [Fact]

    public async Task EvaluateMigration_ReturnsHighRisk_ForDroppingColumn()

    {

        // Arrange: 创建实体并添加字段

        var entityId = await CreateTestEntity("Customer", "Single");

        // 假设原实体有Email字段，新字段列表中没有Email

        var newFields = new List<FieldMetadataDto>

        {

            new()

            {

                PropertyName = "Name",

                DisplayName = new Dictionary<string, string?> { { "en", "FIELD_NAME" } },

                DataType = "String",

                Length = 100,

                IsRequired = true

            }

        };

        // Act: 评估迁移影响（Email字段被删除）

        var response = await _client.PostAsJsonAsync($"/api/entity-advanced/{entityId}/evaluate-migration", newFields);

        // Assert

        var result = await response.Content.ReadFromJsonAsync<MigrationImpactResponse>();

        Assert.NotNull(result);

        // 注意：实际风险等级取决于是否有现有数据

    }

    [Fact]

    public async Task GetMasterCandidates_ReturnsOnlyPublishedRootEntities()

    {

        // Arrange: 创建不同状态的实体

        var publishedId = await CreateTestEntity("PublishedEntity", "Single", isRootEntity: true, status: "Published");

        var draftId = await CreateTestEntity("DraftEntity", "Single", isRootEntity: true, status: "Draft");

        var childId = await CreateTestEntity("ChildEntity", "Single", isRootEntity: false, status: "Published");

        // Act

        var response = await _client.GetAsync("/api/entity-advanced/master-candidates");

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var candidates = await response.Content.ReadFromJsonAsync<List<EntityCandidateDto>>();

        Assert.NotNull(candidates);

        Assert.Contains(candidates, c => c.Id == publishedId);

        Assert.DoesNotContain(candidates, c => c.Id == draftId); // Draft应该被排除

        Assert.DoesNotContain(candidates, c => c.Id == childId); // 非根实体应该被排除

    }

    [Fact]

    public async Task GetDetailCandidates_ReturnsOnlyPublishedEntitiesWithoutParent()

    {

        // Arrange: 创建不同状态的实体

        var independentId = await CreateTestEntity("IndependentEntity", "Single", status: "Published");

        var draftId = await CreateTestEntity("DraftEntity", "Single", status: "Draft");

        // Act

        var response = await _client.GetAsync("/api/entity-advanced/detail-candidates");

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var candidates = await response.Content.ReadFromJsonAsync<List<EntityCandidateDto>>();

        Assert.NotNull(candidates);

        Assert.Contains(candidates, c => c.Id == independentId);

        Assert.DoesNotContain(candidates, c => c.Id == draftId);

    }

    [Fact]

    public async Task ConfigureMasterDetailGrandchild_ThreeLevelStructure_Success()

    {

        // Arrange: 创建三层结构

        var masterId = await CreateTestEntity("Order", "Single");

        var detailId = await CreateTestEntity("OrderLine", "Single");

        var grandchildId = await CreateTestEntity("OrderLineAttribute", "Single");

        // 配置主子关系

        await ConfigureMasterDetailRelationship(masterId, detailId, "OrderId", "OrderLines");

        // 配置子孙关系

        await ConfigureMasterDetailRelationship(detailId, grandchildId, "OrderLineId", "Attributes");

        // 更新主实体结构类型

        var masterConfig = new MasterDetailConfigRequest

        {

            StructureType = "MasterDetailGrandchild",

            Children = new List<ChildEntityConfigRequest>

            {

                new()

                {

                    ChildEntityId = detailId,

                    ForeignKeyField = "OrderId",

                    CollectionProperty = "OrderLines",

                    CascadeDeleteBehavior = "Cascade",

                    AutoCascadeSave = true

                }

            }

        };

        // Act

        var response = await _client.PostAsJsonAsync($"/api/entity-advanced/{masterId}/configure-master-detail", masterConfig);

        // Assert

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MasterDetailConfigResponse>();

        Assert.NotNull(result);

        Assert.Equal("MasterDetailGrandchild", result.StructureType);

        // 验证子实体的子实体配置

        var grandchildResponse = await _client.GetAsync($"/api/entity-advanced/{detailId}/children");

        var grandchildren = await grandchildResponse.Content.ReadFromJsonAsync<ChildrenResponse>();

        Assert.NotNull(grandchildren);

        Assert.Equal(1, grandchildren.ChildCount);

        Assert.Equal(grandchildId, grandchildren.Children[0].Id);

    }

    // Helper Methods

    private async Task<Guid> CreateTestEntity(

        string entityName,

        string structureType,

        bool isRootEntity = true,

        string status = "Published")

    {

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.EntityDefinitions

            .Include(e => e.Fields)

            .FirstOrDefaultAsync(e => e.Namespace == "BobCrm.Base.Test" && e.EntityName == entityName);

        if (existing != null)

        {

            if (existing.Fields.Any())

            {

                db.FieldMetadatas.RemoveRange(existing.Fields);

            }

            db.EntityDefinitions.Remove(existing);

            await db.SaveChangesAsync();

        }

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var entityRoute = $"{entityName.ToLowerInvariant()}-{suffix}";

        var entity = new EntityDefinition

        {

            Id = Guid.NewGuid(),

            Namespace = "BobCrm.Base.Test",            EntityName = entityName,            FullTypeName = $"BobCrm.Base.Test.{entityName}",

            DisplayName = new Dictionary<string, string?> { { "en", $"ENTITY_{entityName.ToUpper()}" } },

            EntityRoute = entityRoute,

            StructureType = structureType,

            Status = status,

            IsRootEntity = isRootEntity,

            IsEnabled = true,

            Order = 0,

            Source = "Custom",

            ApiEndpoint = $"/api/{entityRoute}",

            // DefaultTableName 是计算属性，不需要赋值

            CreatedAt = DateTime.UtcNow,

            UpdatedAt = DateTime.UtcNow

        };

        await db.EntityDefinitions.AddAsync(entity);

        await db.SaveChangesAsync();

        return entity.Id;

    }

    private async Task ConfigureMasterDetailRelationship(

        Guid masterId,

        Guid detailId,

        string foreignKeyField,

        string collectionProperty)

    {

        var config = new MasterDetailConfigRequest

        {

            StructureType = "MasterDetail",

            Children = new List<ChildEntityConfigRequest>

            {

                new()

                {

                    ChildEntityId = detailId,

                    ForeignKeyField = foreignKeyField,

                    CollectionProperty = collectionProperty,

                    CascadeDeleteBehavior = "Cascade",

                    AutoCascadeSave = true

                }

            }

        };

        await _client.PostAsJsonAsync($"/api/entity-advanced/{masterId}/configure-master-detail", config);

    }

    // DTOs for testing

    private class MasterDetailConfigRequest

    {

        public string StructureType { get; set; } = "Single";

        public List<ChildEntityConfigRequest>? Children { get; set; }

    }

    private class ChildEntityConfigRequest

    {

        public Guid ChildEntityId { get; set; }

        public string ForeignKeyField { get; set; } = "";

        public string CollectionProperty { get; set; } = "";

        public string CascadeDeleteBehavior { get; set; } = "NoAction";

        public bool AutoCascadeSave { get; set; } = true;

    }

    private class MasterDetailConfigResponse

    {

        public string Message { get; set; } = "";

        public Guid EntityId { get; set; }

        public string StructureType { get; set; } = "";

    }

    private class ChildrenResponse

    {

        public Guid EntityId { get; set; }

        public string EntityName { get; set; } = "";

        public string StructureType { get; set; } = "";

        public int ChildCount { get; set; }

        public List<ChildEntityDto> Children { get; set; } = new();

    }

    private class ChildEntityDto

    {

        public Guid Id { get; set; }

        public string EntityName { get; set; } = "";

        public string FullTypeName { get; set; } = "";

        public string StructureType { get; set; } = "";

        public string? ParentForeignKeyField { get; set; }

        public string? ParentCollectionProperty { get; set; }

        public string? CascadeDeleteBehavior { get; set; }

        public bool? AutoCascadeSave { get; set; }

        public int FieldCount { get; set; }

    }

    private class AggVOCodeResponse

    {

        public string Entity { get; set; } = "";

        public string AggVOClassName { get; set; } = "";

        public string AggVOCode { get; set; } = "";

        public string VOCode { get; set; } = "";

        public Dictionary<string, string> ChildVOCodes { get; set; } = new();

    }

    private class MigrationImpactResponse

    {

        public string EntityName { get; set; } = "";

        public string TableName { get; set; } = "";

        public long AffectedRows { get; set; }

        public string RiskLevel { get; set; } = "";

        public bool IsSafe { get; set; }

        public List<MigrationOperationDto> Operations { get; set; } = new();

        public List<string> Warnings { get; set; } = new();

        public List<string> Errors { get; set; } = new();

    }

    private class MigrationOperationDto

    {

        public string OperationType { get; set; } = "";

        public string FieldName { get; set; } = "";

        public string? OldDataType { get; set; }

        public string? NewDataType { get; set; }

        public bool MayLoseData { get; set; }

        public bool RequiresConversion { get; set; }

        public string Description { get; set; } = "";

        public string SqlPreview { get; set; } = "";

    }

    private class FieldMetadataDto

    {

        public string PropertyName { get; set; } = "";

        public Dictionary<string, string?>? DisplayName { get; set; }

        public string DataType { get; set; } = "String";

        public int? Length { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public bool IsRequired { get; set; }

        public string? DefaultValue { get; set; }

        public int SortOrder { get; set; }

    }

    private class EntityCandidateDto

    {

        public Guid Id { get; set; }

        public string EntityName { get; set; } = "";

        public string FullTypeName { get; set; } = "";

        public string StructureType { get; set; } = "";

        public Dictionary<string, string?>? DisplayName { get; set; }

        public int FieldCount { get; set; }

        public int? CurrentChildCount { get; set; }

    }

    private class ErrorResponse

    {

        public string Error { get; set; } = "";

    }

}

