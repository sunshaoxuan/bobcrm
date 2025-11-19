using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using FluentAssertions;

namespace BobCrm.Api.Tests;

public class DefaultTemplateGeneratorTests
{
    private readonly DefaultTemplateGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_ShouldMapFieldTypesToExpectedWidgets()
    {
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Contact",
            EntityRoute = "contact",
            ApiEndpoint = "/api/contact",
            Fields = new List<FieldMetadata>
            {
                CreateField("Name", FieldDataType.String, required: true, sortOrder: 0, displayName: "姓名"),
                CreateField("BirthDate", FieldDataType.Date, required: false, sortOrder: 1, displayName: "生日"),
                CreateField("IsActive", FieldDataType.Boolean, required: false, sortOrder: 2, displayName: "启用"),
                CreateField("Notes", FieldDataType.Text, required: false, sortOrder: 3, displayName: "备注", length: 500),
                CreateField("Credit", FieldDataType.Decimal, required: false, sortOrder: 4, displayName: "授信"),
                CreateField("Owner", FieldDataType.EntityRef, required: false, sortOrder: 5, displayName: "负责人", entityRef: true)
            }
        };

        var template = await _generator.GenerateAsync(entity);

        template.Should().NotBeNull();
        template.IsSystemDefault.Should().BeTrue();
        template.UsageType.Should().Be(FormTemplateUsageType.Detail);
        template.UserId.Should().Be("__system__");
        template.EntityType.Should().Be(entity.EntityRoute);
        template.LayoutJson.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(template.LayoutJson!);
        var root = doc.RootElement;

        // New format: array of widgets
        root.ValueKind.Should().Be(JsonValueKind.Array);
        var widgets = root.EnumerateArray().ToList();
        widgets.Count.Should().Be(entity.Fields.Count);

        // Find widgets by dataField property
        var nameWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Name");
        nameWidget.GetProperty("type").GetString().Should().Be("text");
        nameWidget.GetProperty("required").GetBoolean().Should().BeTrue();

        var birthDateWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "BirthDate");
        birthDateWidget.GetProperty("type").GetString().Should().Be("date");

        var isActiveWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "IsActive");
        isActiveWidget.GetProperty("type").GetString().Should().Be("checkbox");

        var notesWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Notes");
        notesWidget.GetProperty("type").GetString().Should().Be("textarea");

        var creditWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Credit");
        creditWidget.GetProperty("type").GetString().Should().Be("number");

        var ownerWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Owner");
        ownerWidget.GetProperty("type").GetString().Should().Be("select");
    }

    [Fact]
    public async Task GenerateAsync_ShouldCarryRequiredFlagsIntoLayout()
    {
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Case",
            EntityRoute = "case",
            ApiEndpoint = "/api/case",
            Fields = new List<FieldMetadata>
            {
                CreateField("Title", FieldDataType.String, required: true, sortOrder: 0, displayName: "标题"),
                CreateField("Description", FieldDataType.Text, required: false, sortOrder: 1, displayName: "描述", length: 500)
            }
        };

        var template = await _generator.GenerateAsync(entity);
        using var doc = JsonDocument.Parse(template.LayoutJson!);
        var root = doc.RootElement;

        // New format: array of widgets
        root.ValueKind.Should().Be(JsonValueKind.Array);
        var widgets = root.EnumerateArray().ToList();

        var titleWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Title");
        titleWidget.GetProperty("required").GetBoolean().Should().BeTrue();

        var descWidget = widgets.First(w => w.GetProperty("dataField").GetString() == "Description");
        descWidget.GetProperty("required").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_ShouldProduceTableLayoutForListUsage()
    {
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Order",
            EntityRoute = "order",
            ApiEndpoint = "/api/orders",
            Fields = new List<FieldMetadata>
            {
                CreateField("Name", FieldDataType.String, true, 0, "名称"),
                CreateField("Amount", FieldDataType.Decimal, false, 1, "金额"),
                CreateField("IsClosed", FieldDataType.Boolean, false, 2, "关闭"),
                CreateField("CreatedAt", FieldDataType.DateTime, false, 3, "创建时间"),
            }
        };

        var template = await _generator.GenerateAsync(entity, FormTemplateUsageType.List);

        template.UsageType.Should().Be(FormTemplateUsageType.List);
        using var doc = JsonDocument.Parse(template.LayoutJson!);
        var root = doc.RootElement;

        // New format: array of widgets, should contain one datagrid widget for List usage
        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(1);

        var dataGrid = root[0];
        dataGrid.GetProperty("type").GetString().Should().Be("datagrid");
        dataGrid.GetProperty("entityType").GetString().Should().Be("order");
        dataGrid.GetProperty("showPagination").GetBoolean().Should().BeTrue();

        // Verify columns are included
        var columnsJson = dataGrid.GetProperty("columnsJson").GetString();
        columnsJson.Should().NotBeNullOrEmpty();
        var columns = JsonDocument.Parse(columnsJson!);
        columns.RootElement.GetArrayLength().Should().Be(entity.Fields.Count);
    }

    private static FieldMetadata CreateField(
        string name,
        string dataType,
        bool required,
        int sortOrder,
        string displayName,
        bool entityRef = false,
        int? length = null)
    {
        return new FieldMetadata
        {
            PropertyName = name,
            DataType = dataType,
            SortOrder = sortOrder,
            IsRequired = required,
            DisplayName = new Dictionary<string, string?> { ["zh"] = displayName },
            IsEntityRef = entityRef,
            ReferencedEntityId = entityRef ? Guid.NewGuid() : null,
            Length = length
        };
    }
}
