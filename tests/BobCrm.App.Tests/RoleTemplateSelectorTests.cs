using System;
using System.Collections.Generic;
using BobCrm.App.Components.Shared;
using BobCrm.App.Models;

namespace BobCrm.App.Tests;

public class RoleTemplateSelectorTests : TestContext
{
    [Fact]
    public void DisplaysInheritLabelWhenNoOptions()
    {
        var node = new FunctionMenuNode
        {
            Id = Guid.NewGuid(),
            TemplateOptions = new List<FunctionTemplateOption>()
        };

        var cut = RenderComponent<RoleTemplateSelector>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.InheritLabel, "Inherit")
            .Add(p => p.Disabled, false)
            .Add(p => p.SelectedTemplateBindingId, null));

        Assert.DoesNotContain("select", cut.Markup, StringComparison.OrdinalIgnoreCase);
        var inherit = cut.Find(".role-template-inherit");
        Assert.Equal("Inherit", inherit.TextContent.Trim());
    }

    [Fact]
    public void InvokesCallbackWhenSelectionChanges()
    {
        var node = new FunctionMenuNode
        {
            Id = Guid.NewGuid(),
            TemplateOptions = new List<FunctionTemplateOption>
            {
                new() { BindingId = 10, TemplateId = 7, TemplateName = "Default", IsDefault = true },
                new() { BindingId = 20, TemplateId = 8, TemplateName = "Custom" }
            }
        };

        var captured = new List<int?>();
        var cut = RenderComponent<RoleTemplateSelector>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.Disabled, false)
            .Add(p => p.InheritLabel, "Inherit")
            .Add(p => p.SelectedTemplateBindingId, null)
            .Add(p => p.OnTemplateChanged, (Action<int?>)(value => captured.Add(value))));

        var select = cut.Find("select.role-template-dropdown");
        select.Change("20");
        select.Change(string.Empty);

        Assert.Equal(new int?[] { 20, null }, captured);
    }
}
