using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;

// Use Postgres from docker (see docker-compose.yml):
// Host=localhost;Port=5432;Database=bobcrm;Username=postgres;Password=postgres

namespace BobCrm.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Force provider to postgres for tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Db:Provider"] = "postgres",
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=bobcrm;Username=postgres;Password=postgres"
            };
            config.AddInMemoryCollection(dict!);
        });

        var host = base.CreateHost(builder);
        // Reset and seed database to a clean state
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseInitializer.RecreateAsync(db).GetAwaiter().GetResult();
            // seed admin user/role and grant access to all customers
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!rm.RoleExistsAsync("admin").GetAwaiter().GetResult())
            {
                rm.CreateAsync(new IdentityRole("admin")).GetAwaiter().GetResult();
            }
            var admin = um.FindByNameAsync("admin").GetAwaiter().GetResult();
            if (admin == null)
            {
                admin = new IdentityUser { UserName = "admin", Email = "admin@local", EmailConfirmed = true };
                um.CreateAsync(admin, "Admin@12345").GetAwaiter().GetResult();
                um.AddToRoleAsync(admin, "admin").GetAwaiter().GetResult();
            }
            else if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true; um.UpdateAsync(admin).GetAwaiter().GetResult();
            }

            var repo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerAccess>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var customers = db.Customers.Select(c => c.Id).ToList();
            foreach (var cid in customers)
            {
                var exists = repo.Query(a => a.CustomerId == cid && a.UserId == admin!.Id).Any();
                if (!exists)
                {
                    repo.AddAsync(new CustomerAccess { CustomerId = cid, UserId = admin!.Id, CanEdit = true }).GetAwaiter().GetResult();
                }
            }
            uow.SaveChangesAsync().GetAwaiter().GetResult();
        }
        return host;
    }
}
