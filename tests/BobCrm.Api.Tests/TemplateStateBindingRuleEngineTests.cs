using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using FluentAssertions;

namespace BobCrm.Api.Tests;

public class TemplateStateBindingRuleEngineTests
{
    [Fact]
    public void SelectTemplateId_ShouldReturnHighestPriorityMatch_WhenFieldMatches()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                EntityType = "order",
                ViewState = "DetailView",
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                EntityType = "order",
                ViewState = "DetailView",
                TemplateId = 200,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 20
            },
            new()
            {
                Id = 3,
                EntityType = "order",
                ViewState = "DetailView",
                TemplateId = 300,
                IsDefault = true
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Draft\"}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(200);
    }

    [Fact]
    public void SelectTemplateId_ShouldFallbackToDefault_WhenNoRuleMatches()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 300,
                IsDefault = true,
                Priority = -1
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Approved\"}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(300);
    }

    [Fact]
    public void SelectTemplateId_ShouldFallbackToDefault_WhenFieldMissing()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 200,
                IsDefault = true
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Name\":\"Order-1\"}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(200);
    }

    [Fact]
    public void SelectTemplateId_ShouldReturnDefault_WhenNoDataProvided()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 200,
                IsDefault = true
            }
        };

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, null);

        selected.Should().Be(200);
    }

    [Fact]
    public void SelectTemplateId_ShouldReturnGenericBinding_WhenNoDefaultExists()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 250,
                MatchFieldName = null,
                Priority = 5
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Approved\"}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(250);
    }

    [Fact]
    public void SelectTemplateId_ShouldIgnoreRule_WhenMatchValueMissing()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = null,
                Priority = 99
            },
            new()
            {
                Id = 2,
                TemplateId = 200,
                IsDefault = true
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Status\":\"Draft\"}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(200);
    }

    [Fact]
    public void SelectTemplateId_ShouldHandleNonObjectEntityData_ByFallingBackToDefault()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Status",
                MatchFieldValue = "Draft",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 200,
                IsDefault = true
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("[{\"Status\":\"Draft\"}]");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(200);
    }

    [Fact]
    public void SelectTemplateId_ShouldMatchNonStringJsonValues_UsingRawText()
    {
        var bindings = new List<TemplateStateBinding>
        {
            new()
            {
                Id = 1,
                TemplateId = 100,
                MatchFieldName = "Step",
                MatchFieldValue = "2",
                Priority = 10
            },
            new()
            {
                Id = 2,
                TemplateId = 200,
                IsDefault = true
            }
        };

        var data = JsonSerializer.Deserialize<JsonElement>("{\"Step\":2}");

        var selected = TemplateStateBindingRuleEngine.SelectTemplateId(bindings, data);

        selected.Should().Be(100);
    }
}
