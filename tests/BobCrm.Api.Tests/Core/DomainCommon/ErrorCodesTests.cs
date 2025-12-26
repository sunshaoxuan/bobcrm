using Xunit;
using BobCrm.Api.Core.DomainCommon;
using System.Reflection;
using System.Text.Json;

namespace BobCrm.Api.Tests.Core.DomainCommon;

public class ErrorCodesTests
{
    [Fact]
    public void ErrorCodes_ShouldNotHaveDuplicateValues()
    {
        var constants = GetErrorCodes();
        var duplicates = constants.GroupBy(x => x.Value)
                                  .Where(g => g.Count() > 1)
                                  .Select(g => g.Key)
                                  .ToList();

        Assert.True(duplicates.Count == 0, $"Duplicate error codes found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void ErrorCodes_ShouldNotHaveNullOrEmptyValues()
    {
        var constants = GetErrorCodes();
        var invalid = constants.Where(x => string.IsNullOrWhiteSpace(x.Value))
                               .Select(x => x.Name)
                               .ToList();

        Assert.True(invalid.Count == 0, $"Invalid (null/empty) error codes found: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void ErrorCodes_ShouldHaveI18nKeys()
    {
        var json = File.ReadAllText("Resources/i18n-resources.json");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var missing = new List<string>();
        foreach (var (_, code) in GetErrorCodes())
        {
            var key = $"ERR_{code}";
            if (!root.TryGetProperty(key, out _))
            {
                missing.Add(key);
            }
        }

        Assert.True(missing.Count == 0, $"Missing i18n keys for error codes: {string.Join(", ", missing)}");
    }

    private List<(string Name, string Value)> GetErrorCodes()
    {
        return typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(fi => (fi.Name, (string)fi.GetValue(null)!))
            .ToList();
    }
}
