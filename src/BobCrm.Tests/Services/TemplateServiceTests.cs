using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Infrastructure.Ef;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BobCrm.Tests.Services;

public class TemplateServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly IRepository<FormTemplate> _repo;
    private readonly IUnitOfWork _uow;
    private readonly II18nService _i18n = new FakeI18n();
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _repo = new EfRepository<FormTemplate>(_dbContext);
        _uow = new EfUnitOfWork(_dbContext);
        _service = new TemplateService(_repo, _uow, _i18n, NullLogger<TemplateService>.Instance);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldIncludeSystemTemplates()
    {
        var userId = "user-1";
        _dbContext.FormTemplates.Add(new FormTemplate
        {
            Name = "System Default",
            EntityType = "customer",
            UserId = "system",
            IsSystemDefault = true,
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "[]"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetTemplatesAsync(userId, entityType: "customer", usageType: "Detail");
        var templates = ((IEnumerable)result).Cast<object>().ToList();

        templates.Should().HaveCount(1);
        var name = templates[0].GetType().GetProperty("Name")?.GetValue(templates[0]) as string;
        name.Should().Be("System Default");
    }

    [Fact]
    public async Task CopyTemplateAsync_ShouldCreateUserCopy()
    {
        var source = new FormTemplate
        {
            Name = "Source",
            EntityType = "customer",
            UserId = "system",
            IsSystemDefault = true,
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "{ }"
        };
        _dbContext.FormTemplates.Add(source);
        await _dbContext.SaveChangesAsync();

        var request = new CopyTemplateRequest(
            Name: "My Copy",
            EntityType: "customer",
            UsageType: FormTemplateUsageType.Edit,
            Description: "from source");

        var copy = await _service.CopyTemplateAsync(source.Id, "user-1", request);

        copy.Id.Should().NotBe(0);
        copy.Name.Should().Be("My Copy");
        copy.UserId.Should().Be("user-1");
        copy.IsSystemDefault.Should().BeFalse();
        copy.UsageType.Should().Be(FormTemplateUsageType.Edit);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldRemoveUserTemplate()
    {
        var template = new FormTemplate
        {
            Name = "User Template",
            EntityType = "customer",
            UserId = "user-2",
            UsageType = FormTemplateUsageType.Detail,
            LayoutJson = "{}"
        };
        _dbContext.FormTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        await _service.DeleteTemplateAsync(template.Id, "user-2");

        var check = await _dbContext.FormTemplates.FindAsync(template.Id);
        check.Should().BeNull();
    }

    private class FakeI18n : II18nService
    {
        public string CurrentLang => "en";
        public event Action? OnChanged
        {
            add { }
            remove { }
        }
        public Task LoadAsync(string lang, bool force = false, CancellationToken ct = default) => Task.CompletedTask;
        public string T(string key) => key;
    }
}
