using Microsoft.AspNetCore.Routing;

namespace BobCrm.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var themeGroup = app.MapGroup("/api/theme")
            .WithTags("Theme")
            .WithOpenApi();

        MapThemeDefaults(themeGroup);

        return app;
    }

    private static void MapThemeDefaults(IEndpointRouteBuilder themeGroup)
    {
        themeGroup.MapGet("/defaults", (IConfiguration cfg, ILogger<Program> logger) =>
        {
            var initColor = cfg.GetValue<string>("Theme:InitColor");
            var initTheme = cfg.GetValue<string>("Theme:InitTheme") ?? "light";
            logger.LogDebug("[Theme] Defaults requested: theme={Theme}, color={Color}", initTheme, initColor);
            return Results.Json(new { initColor, initTheme });
        })
        .WithName("GetThemeDefaults")
        .WithSummary("获取主题默认值")
        .WithDescription("供早期客户端快速读取开箱即用的主题信息")
        .AllowAnonymous();
    }
}
