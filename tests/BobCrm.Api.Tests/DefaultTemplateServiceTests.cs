using System;
using System.Collections.Generic;
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
    private readonly DefaultTemplateGenerator _generator = new();
    private readonly TemplateBindingService _bindingService;
    private readonly DefaultTemplateService _service;

    public DefaultTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"templates_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var bindingLogger = Mock.Of<ILogger<TemplateBindingService>>();
        var serviceLogger = Mock.Of<ILogger<DefaultTemplateService>>();
        _bindingService = new TemplateBindingService(_db, bindingLogger);
        _service = new DefaultTemplateService(_db, _generator, _bindingService, serviceLogger);
    }

    [Fact]
    public async Task EnsureSystemTemplateAsync_ShouldCreateTemplateAndBinding()
    {
        var entity = CreateEntityDefinition();

        await _service.EnsureSystemTemplateAsync(entity, "publisher");

        var template = await _db.FormTemplates.SingleAsync();
        template.EntityType.Should().Be(entity.FullName);
        template.IsSystemDefault.Should().BeTrue();
        template.UserId.Should().Be("__system__");
        template.UsageType.Should().Be(FormTemplateUsageType.Detail);

        var binding = await _db.TemplateBindings.SingleAsync();
        binding.EntityType.Should().Be(entity.FullName);
        binding.IsSystem.Should().BeTrue();
        binding.TemplateId.Should().Be(template.Id);
    }

    [Fact]
    public async Task EnsureSystemTemplateAsync_ShouldUpdateExistingTemplate()
    {
        var entity = CreateEntityDefinition();
        await _service.EnsureSystemTemplateAsync(entity, "publisher");

        var template = await _db.FormTemplates.SingleAsync();
        template.LayoutJson.Should().Contain("Name");

        entity.Fields.Add(new FieldMetadata
        {
            PropertyName = "Updated",
            DataType = FieldDataType.Boolean,
            SortOrder = 5,
            IsRequired = false,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "更新" }
        });

        await _service.EnsureSystemTemplateAsync(entity, "publisher");

        var updatedTemplate = await _db.FormTemplates.SingleAsync();
        updatedTemplate.LayoutJson.Should().Contain("Updated");
        updatedTemplate.Id.Should().Be(template.Id);
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
