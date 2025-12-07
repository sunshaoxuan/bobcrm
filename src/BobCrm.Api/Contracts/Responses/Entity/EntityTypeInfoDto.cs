namespace BobCrm.Api.Contracts.Responses.Entity;

// Assuming EntityTypeInfo is already defined in BobCrm.Api.Core.DomainCommon or similar, 
// but if it's a DTO we should define it here. 
// Based on the code, dynamicEntityService.GetEntityTypeInfo returns an object that is serialized.
// I'll assume for now we can just use object or define a proper DTO if I knew the structure.
// Looking at the code: dynamicEntityService.GetEntityTypeInfo(fullTypeName)
// I'll skip creating a specific DTO for this one if it returns a domain object, 
// but for strict governance I should wrap it.
// Let's assume it returns a complex object and just use object for now in the endpoint, 
// or if I can find the definition of EntityTypeInfo.
// I'll check the file content again.
