using BobCrm.Api.Contracts.Responses.Entity;
using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Tests.Contracts;

public class DtoContractTests
{
    [Fact]
    public void DdlExecutionHistoryDto_ShouldBeInstantiable()
    {
        var dto = new DdlExecutionHistoryDto();
        Assert.NotNull(dto);
        Assert.NotNull(dto.ScriptType);
        Assert.NotNull(dto.Status);
        Assert.NotNull(dto.ScriptPreview);
        Assert.Empty(dto.ScriptType);
        Assert.Empty(dto.Status);
        Assert.Empty(dto.ScriptPreview);
        
        // Nullable properties can be null
        Assert.Null(dto.ErrorMessage);
        Assert.Null(dto.CreatedBy);
    }
}
