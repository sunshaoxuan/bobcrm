using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db) =>
        {
            var users = await db.Users.Select(u => new { u.Id, u.UserName, u.Email }).ToListAsync();
            return Results.Ok(users);
        });

        group.MapGet("/{userId}", async (string userId, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(userId);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            return Results.Ok(new { user.Id, user.UserName, user.Email });
        });

        return app;
    }
}
