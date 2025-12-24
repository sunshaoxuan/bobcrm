using BobCrm.App.Models;

namespace BobCrm.App.Services;

public record FunctionTreeResponse(List<FunctionMenuNode> Tree, string? Version);
