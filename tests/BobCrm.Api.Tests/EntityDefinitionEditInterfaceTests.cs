using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class EntityDefinitionEditInterfaceTests
{
    [Fact]
    public void SyncInterfaceFields_AddsBaseTemplates()
    {
        var component = new EntityDefinitionEdit();
        var editModel = CreateEditModel(component);
        SetPrivateField(component, "_model", editModel);
        SetPrivateField(component, "_selectedInterfaces", new[] { EntityInterfaceType.Base });

        InvokeSync(component);

        var fields = GetFields(editModel);
        fields.Should().NotBeNull();
        fields.Select(f => f.PropertyName)
            .Should().Contain(new[] { "Id", "IsDeleted", "DeletedAt", "DeletedBy" });
    }

    [Fact]
    public void SyncInterfaceFields_RemovesTemplates_WhenInterfaceUnchecked()
    {
        var component = new EntityDefinitionEdit();
        var editModel = CreateEditModel(component);
        SetPrivateField(component, "_model", editModel);
        SetPrivateField(component, "_selectedInterfaces", new[] { EntityInterfaceType.Base });
        InvokeSync(component);

        SetPrivateField(component, "_selectedInterfaces", System.Array.Empty<string>());
        InvokeSync(component);

        GetFields(editModel).Should().BeEmpty();
    }

    [Fact]
    public void SyncInterfaceFields_AddsOrganizationTemplates()
    {
        var component = new EntityDefinitionEdit();
        var editModel = CreateEditModel(component);
        SetPrivateField(component, "_model", editModel);
        SetPrivateField(component, "_selectedInterfaces", new[] { EntityInterfaceType.Organization });

        InvokeSync(component);

        var fields = GetFields(editModel);
        fields.Select(f => f.PropertyName)
            .Should()
            .Contain(new[]
            {
                "OrganizationId",
                "OrganizationCode",
                "OrganizationName",
                "OrganizationPathCode"
            });

        var orgId = fields.First(f => f.PropertyName == "OrganizationId");
        orgId.IsEntityRef.Should().BeTrue();
        orgId.TableName.Should().Be("OrganizationNodes");
    }

    private static object CreateEditModel(EntityDefinitionEdit component)
    {
        var modelType = component.GetType().GetNestedType("EditModel", BindingFlags.NonPublic)!;
        var instance = Activator.CreateInstance(modelType)!;
        var fieldsProp = modelType.GetProperty("Fields")!;
        fieldsProp.SetValue(instance, new List<FieldMetadataDto>());
        return instance;
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static List<FieldMetadataDto> GetFields(object editModel)
    {
        var modelType = editModel.GetType();
        var fieldsProp = modelType.GetProperty("Fields")!;
        return (List<FieldMetadataDto>)fieldsProp.GetValue(editModel)!;
    }

    private static void InvokeSync(EntityDefinitionEdit component)
    {
        var method = component.GetType().GetMethod("SyncInterfaceFields", BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(component, null);
    }
}
