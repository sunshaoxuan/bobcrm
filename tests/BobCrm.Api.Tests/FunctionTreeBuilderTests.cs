using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

public class FunctionTreeBuilderTests
{
    [Fact]
    public async Task BuildAsync_ShouldIncludeLocalizedNamesAndTemplateOptions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var template = new FormTemplate
        {
            Id = 1,
            Name = "List Template",
            EntityType = "product",
            UsageType = FormTemplateUsageType.List,
            IsSystemDefault = true
        };
        var binding = new TemplateBinding
        {
            Id = 10,
            EntityType = "product",
            UsageType = FormTemplateUsageType.List,
            TemplateId = template.Id,
            Template = template,
            IsSystem = true,
            RequiredFunctionCode = "CRM.CORE.PRODUCT"
        };

        db.FormTemplates.Add(template);
        db.TemplateBindings.Add(binding);
        db.LocalizationResources.Add(new LocalizationResource
        {
            Key = "MENU_CRM_CORE_PRODUCT",
            Translations = new Dictionary<string, string>
            {
                ["zh"] = "产品",
                ["en"] = "Products"
            }
        });

        var root = new FunctionNode
        {
            Code = "APP.ROOT",
            Name = "Root",
            SortOrder = 0,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "应用根节点" }
        };
        var child = new FunctionNode
        {
            ParentId = root.Id,
            Code = "CRM.CORE.PRODUCT",
            Name = "Product List",
            SortOrder = 10,
            DisplayNameKey = "MENU_CRM_CORE_PRODUCT",
            TemplateId = template.Id,
            Template = template,
            TemplateBindingId = binding.Id
        };

        db.FunctionNodes.AddRange(root, child);
        await db.SaveChangesAsync();

        var logger = NullLogger<MultilingualFieldService>.Instance;
        var multilingual = new MultilingualFieldService(db, logger);
        var builder = new FunctionTreeBuilder(db, multilingual);
        var nodes = await db.FunctionNodes
            .AsNoTracking()
            .Include(n => n.Template)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

        var tree = await builder.BuildAsync(nodes);

        tree.Should().HaveCount(1);
        var rootDto = tree[0];
        rootDto.Children.Should().HaveCount(1);
        var childDto = rootDto.Children[0];
        childDto.Code.Should().Be(child.Code);
        childDto.DisplayNameTranslations.Should().NotBeNull();
        childDto.DisplayNameTranslations!["en"].Should().Be("Products");
        childDto.TemplateOptions.Should().ContainSingle(option =>
            option.BindingId == binding.Id &&
            option.IsDefault &&
            option.TemplateName == template.Name);
    }

    [Fact]
    public async Task BuildAsync_WithLang_ShouldReturnSingleLanguageDisplayName()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        db.LocalizationResources.Add(new LocalizationResource
        {
            Key = "MENU_TEST_NODE",
            Translations = new Dictionary<string, string>
            {
                ["ja"] = "テストノード",
                ["en"] = "Test Node"
            }
        });

        var node = new FunctionNode
        {
            Code = "APP.ROOT",
            Name = "Root",
            SortOrder = 0,
            DisplayNameKey = "MENU_TEST_NODE"
        };

        db.FunctionNodes.Add(node);
        await db.SaveChangesAsync();

        var logger = NullLogger<MultilingualFieldService>.Instance;
        var multilingual = new MultilingualFieldService(db, logger);
        var builder = new FunctionTreeBuilder(db, multilingual);
        var nodes = await db.FunctionNodes
            .AsNoTracking()
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

        var tree = await builder.BuildAsync(nodes, "ja");

        tree.Should().HaveCount(1);
        var dto = tree[0];
        dto.DisplayName.Should().Be("テストノード");
        dto.DisplayNameTranslations.Should().BeNull();
    }
}
