using System;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

public record ScopeBinding(RoleDataScope Scope, Guid? OrganizationId);
