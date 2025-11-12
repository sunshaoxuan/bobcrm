using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEditInlineEditingTests
{
    [Fact]
    public void EntityNameInput_SetsNamespace_WhenNameProvided()
    {
        var component = new EntityDefinitionEdit();
        var model = CreateEditModel(component);
        SetPrivateField(component, "_model", model);
        SetModelCategory(model, "CRM");

        SetPrivateProperty(component, "EntityNameInput", "OrderHeader");

        GetNamespace(model).Should().Be("BobCrm.CRM.OrderHeader");
    }

    [Fact]
    public void EntityNameInput_UsesSelectedDomain()
    {
        var component = new EntityDefinitionEdit();
        var model = CreateEditModel(component);
        SetPrivateField(component, "_model", model);
        SetModelCategory(model, "SCM");

        SetPrivateProperty(component, "EntityNameInput", "Warehouse");

        GetNamespace(model).Should().Be("BobCrm.SCM.Warehouse");
    }

    [Fact]
    public void EntityNameInput_ClearsNamespace_WhenEmpty()
    {
        var component = new EntityDefinitionEdit();
        var model = CreateEditModel(component);
        SetPrivateField(component, "_model", model);
        SetPrivateProperty(component, "EntityNameInput", "Initial");

        SetPrivateProperty(component, "EntityNameInput", "   ");

        GetNamespace(model).Should().BeEmpty();
    }

    [Fact]
    public void AddFieldRow_AppendsField_WithDefaults()
    {
        var component = new EntityDefinitionEdit();
        var model = CreateEditModel(component);
        var list = new List<FieldMetadataDto>
        {
            new() { Id = Guid.NewGuid(), PropertyName = "Name", SortOrder = 100 }
        };
        SetPrivateField(component, "_model", model);
        SetModelFields(model, list);

        InvokeMethod(component, "AddFieldRow");

        var fields = GetFields(model);
        fields.Should().HaveCount(2);
        var newest = fields[^1];
        newest.SortOrder.Should().Be(110);
        newest.DataType.Should().Be(FieldDataType.String);
        newest.DisplayName.Should().NotBeNull();
    }

    [Fact]
    public async Task OnFieldDisplayNameChanged_UpdatesValue()
    {
        var component = new EntityDefinitionEdit();
        var field = new FieldMetadataDto { DisplayName = new MultilingualTextDto() };
        var payload = new MultilingualTextDto { ["en"] = "Display" };

        await (Task)InvokeMethod(component, "OnFieldDisplayNameChanged", field, payload);

        field.DisplayName!["en"].Should().Be("Display");
    }

    private static object CreateEditModel(EntityDefinitionEdit component)
    {
        var modelType = component.GetType().GetNestedType("EditModel", BindingFlags.NonPublic)!;
        var instance = Activator.CreateInstance(modelType)!;
        var displayProp = modelType.GetProperty("DisplayName")!;
        displayProp.SetValue(instance, new MultilingualTextDto());
        var categoryProp = modelType.GetProperty("Category")!;
        categoryProp.SetValue(instance, "Custom");
        return instance;
    }

    private static void SetModelFields(object editModel, List<FieldMetadataDto> fields)
    {
        editModel.GetType().GetProperty("Fields")!.SetValue(editModel, fields);
    }

    private static List<FieldMetadataDto> GetFields(object editModel)
    {
        return (List<FieldMetadataDto>)editModel.GetType().GetProperty("Fields")!.GetValue(editModel)!;
    }

    private static string GetNamespace(object editModel)
    {
        return (string)editModel.GetType().GetProperty("Namespace")!.GetValue(editModel)!;
    }

    private static void SetModelCategory(object editModel, string value)
    {
        editModel.GetType().GetProperty("Category")!.SetValue(editModel, value);
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static void SetPrivateProperty(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        prop!.SetValue(target, value);
    }

    private static object InvokeMethod(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        return method!.Invoke(target, args) ?? Task.CompletedTask;
    }
}
