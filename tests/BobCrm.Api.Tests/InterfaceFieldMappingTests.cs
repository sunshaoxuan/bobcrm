using BobCrm.Api.Base.Models;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class InterfaceFieldMappingTests
{
    [Theory]
    [InlineData(InterfaceType.Base, 1)]
    [InlineData(InterfaceType.Archive, 2)]
    [InlineData(InterfaceType.Audit, 4)]
    [InlineData(InterfaceType.Version, 1)]
    [InlineData(InterfaceType.TimeVersion, 3)]
    [InlineData(InterfaceType.Organization, 1)]
    public void GetFields_KnownTypes_ShouldReturnExpectedCounts(string interfaceType, int expectedCount)
    {
        var fields = InterfaceFieldMapping.GetFields(interfaceType);
        fields.Should().HaveCount(expectedCount);
        fields.Should().OnlyContain(f => !string.IsNullOrWhiteSpace(f.PropertyName));
    }

    [Fact]
    public void GetFields_UnknownType_ShouldReturnEmpty()
    {
        var fields = InterfaceFieldMapping.GetFields("Unknown");
        fields.Should().BeEmpty();
    }

    [Fact]
    public void GetFields_Base_ShouldContainRequiredPrimaryKeyId()
    {
        var fields = InterfaceFieldMapping.GetFields(InterfaceType.Base);
        fields.Should().ContainSingle();
        fields[0].PropertyName.Should().Be("Id");
        fields[0].IsPrimaryKey.Should().BeTrue();
        fields[0].IsRequired.Should().BeTrue();
        fields[0].DataType.Should().Be(FieldDataType.Guid);
    }
}

