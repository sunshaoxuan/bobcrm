using BobCrm.Api.Contracts.Responses.Layout;

namespace BobCrm.Api.Application.Queries;

public interface IFieldQueries
{
    List<FieldDefinitionResponseDto> GetDefinitions();
}
