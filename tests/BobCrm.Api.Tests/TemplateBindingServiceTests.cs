using System;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

public class TemplateBindingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TemplateBindingService _service;

    public TemplateBindingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BindingTests_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
        _service = new TemplateBindingService(_db, Mock.Of<ILogger<TemplateBindingService>>());
    }

    [Fact]
    public async Task GetBindingAsync_ShouldSkipEmptyUserBinding()
    {
        var emptyTemplate = new FormTemplate
        {
            Id = 1,
            Name = "Empty detail",
            EntityType = "customer",
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "[]",
            UpdatedAt = DateTime.UtcNow
        };

        var usableTemplate = new FormTemplate
        {
            Id = 2,
            Name = "System detail",
            EntityType = "customer",
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "[{\"type\":\"text\",\"label\":\"LBL_NAME\",\"dataField\":\"name\"}]",
            UpdatedAt = DateTime.UtcNow
        };

        _db.FormTemplates.AddRange(emptyTemplate, usableTemplate);
        _db.TemplateBindings.AddRange(
            new TemplateBinding
            {
                EntityType = "customer",
                UsageType = FormTemplateUsageType.Detail,
                TemplateId = emptyTemplate.Id,
                Template = emptyTemplate,
                IsSystem = false,
                UpdatedBy = "user",
                UpdatedAt = DateTime.UtcNow
            },
            new TemplateBinding
            {
                EntityType = "customer",
                UsageType = FormTemplateUsageType.Detail,
                TemplateId = usableTemplate.Id,
                Template = usableTemplate,
                IsSystem = true,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync();

        var result = await _service.GetBindingAsync("customer", FormTemplateUsageType.Detail);

        result.Should().NotBeNull();
        result!.TemplateId.Should().Be(usableTemplate.Id);
    }

    [Fact]
    public async Task GetBindingAsync_ShouldFallbackToStateBindingWhenNoTemplateBindingExists()
    {
        var usableTemplate = new FormTemplate
        {
            Id = 10,
            Name = "State detail",
            EntityType = "customer",
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "[{\"type\":\"text\",\"label\":\"LBL_NAME\",\"dataField\":\"name\"}]",
            UpdatedAt = DateTime.UtcNow
        };

        _db.FormTemplates.Add(usableTemplate);
        _db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            EntityType = "customer",
            ViewState = "DetailView",
            TemplateId = usableTemplate.Id,
            Template = usableTemplate,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var result = await _service.GetBindingAsync("customer", FormTemplateUsageType.Detail);

        result.Should().NotBeNull();
        result!.TemplateId.Should().Be(usableTemplate.Id);
        result.Template.Should().NotBeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
