using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BobCrm.Api.Tests;

public class DefaultTemplateServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DefaultTemplateGenerator _generator;
    private readonly DefaultTemplateService _service;

    public DefaultTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"templates_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var generatorLogger = Mock.Of<ILogger<DefaultTemplateGenerator>>();
        var serviceLogger = Mock.Of<ILogger<DefaultTemplateService>>();
        _generator = new DefaultTemplateGenerator(_db, generatorLogger);
        _service = new DefaultTemplateService(_db, _generator, serviceLogger);
    }

    [Fact]
    public async Task EnsureTemplatesAsync_ShouldCreateTemplatesForAllUsages()
    {
        var entity = CreateEntityDefinition();

        var result = await _service.EnsureTemplatesAsync(entity, "publisher");

        result.Templates.Should().HaveCount(3);
        var templates = await _db.FormTemplates.ToListAsync();
        templates.Should().HaveCount(3);
        templates.Select(t => t.EntityType).Should().AllBeEquivalentTo(entity.EntityRoute);
        templates.Should().OnlyContain(t => t.IsSystemDefault);
    }

    [Fact]
    public async Task EnsureTemplatesAsync_ShouldUpdateExistingTemplates()
    {
        var entity = CreateEntityDefinition();
        await _service.EnsureTemplatesAsync(entity, "publisher");

        var template = await _db.FormTemplates.FirstAsync(t => t.UsageType == FormTemplateUsageType.Detail);
        template.LayoutJson.Should().Contain("Name");

        entity.Fields.Add(new FieldMetadata
        {
            PropertyName = "Updated",
            DataType = FieldDataType.Boolean,
            SortOrder = 5,
            IsRequired = false,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "更新" }
        });

        await _service.EnsureTemplatesAsync(entity, "publisher");

        var updatedTemplate = await _db.FormTemplates.FirstAsync(t => t.UsageType == FormTemplateUsageType.Detail);
        updatedTemplate.LayoutJson.Should().Contain("Updated");
        updatedTemplate.Id.Should().Be(template.Id);
    }

    [Fact]
    public async Task GetDefaultTemplateAsync_ShouldReturnExistingTemplate_WhenPresent()
    {
        var entity = CreateEntityDefinition();
        await _service.EnsureTemplatesAsync(entity, "publisher");

        var template = await _service.GetDefaultTemplateAsync(entity, FormTemplateUsageType.Detail, "requester");

        template.EntityType.Should().Be(entity.EntityRoute);
        template.UsageType.Should().Be(FormTemplateUsageType.Detail);
        (await _db.FormTemplates.CountAsync(t => t.UsageType == FormTemplateUsageType.Detail)).Should().Be(1);
    }

    [Fact]
    public async Task GetDefaultTemplateAsync_ShouldGenerateTemplate_WhenMissing()
    {
        var entity = CreateEntityDefinition();

        var template = await _service.GetDefaultTemplateAsync(entity, FormTemplateUsageType.Edit);

        template.UsageType.Should().Be(FormTemplateUsageType.Edit);
        template.EntityType.Should().Be(entity.EntityRoute);
        (await _db.FormTemplates.CountAsync()).Should().Be(1);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private static EntityDefinition CreateEntityDefinition()
    {
        return new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "Test",
            EntityName = "Invoice",
            EntityRoute = "invoice",
            ApiEndpoint = "/api/invoice",
            Status = EntityStatus.Draft,
            Fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "Name",
                    DataType = FieldDataType.String,
                    SortOrder = 0,
                    IsRequired = true,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "名称" }
                },
                new FieldMetadata
                {
                    PropertyName = "Amount",
                    DataType = FieldDataType.Decimal,
                    SortOrder = 1,
                    IsRequired = false,
                    DisplayName = new Dictionary<string, string?> { ["zh"] = "金额" }
                }
            }
        };
    }
}
