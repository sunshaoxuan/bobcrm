using System.Collections.Generic;

using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Endpoints;

public record BulkUpsertFieldPermissionsRequest(
    List<FieldPermissionDto> Permissions);
