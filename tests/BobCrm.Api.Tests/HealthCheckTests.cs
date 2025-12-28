using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services.HealthChecks;
using BobCrm.Api.Services.Storage;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace BobCrm.Api.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task DbConnectionHealthCheck_ShouldBeHealthy_ForInMemoryProvider()
    {
        await using var db = CreateInMemoryContext();
        var check = new DbConnectionHealthCheck(db);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("InMemory provider");
    }

    [Fact]
    public async Task DbConnectionHealthCheck_ShouldBeUnhealthy_WhenCannotConnect()
    {
        await using var db = CreateSqliteReadOnlyMissingFileContext();
        var check = new DbConnectionHealthCheck(db);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cannot connect");
    }

    [Fact]
    public async Task DiskSpaceHealthCheck_ShouldBeDegraded_WhenRootCannotBeResolved()
    {
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(MockBehavior.Strict);
        env.SetupGet(x => x.ContentRootPath).Returns(string.Empty);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var check = new DiskSpaceHealthCheck(env.Object, config);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("disk root");
    }

    [Fact]
    public async Task DiskSpaceHealthCheck_ShouldRespectThreshold()
    {
        var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(MockBehavior.Strict);
        env.SetupGet(x => x.ContentRootPath).Returns(Environment.CurrentDirectory);

        var configLow = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HealthChecks:Disk:MinFreeBytes"] = "0" })
            .Build();
        var healthy = await new DiskSpaceHealthCheck(env.Object, configLow)
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        healthy.Status.Should().Be(HealthStatus.Healthy);

        var configHigh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HealthChecks:Disk:MinFreeBytes"] = long.MaxValue.ToString() })
            .Build();
        var unhealthy = await new DiskSpaceHealthCheck(env.Object, configHigh)
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        unhealthy.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task S3ConnectivityHealthCheck_ShouldBeHealthy_WhenNotConfigured()
    {
        var options = Options.Create(new S3Options { ServiceUrl = "" });
        var check = new S3ConnectivityHealthCheck(options);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public async Task S3ConnectivityHealthCheck_ShouldBeUnhealthy_WhenServiceUrlInvalid()
    {
        var options = Options.Create(new S3Options { ServiceUrl = "not a url" });
        var check = new S3ConnectivityHealthCheck(options);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Invalid S3 ServiceUrl");
    }

    [Fact]
    public async Task SmtpConnectivityHealthCheck_ShouldBeHealthy_WhenNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var provider = services.BuildServiceProvider();

        var check = new SmtpConnectivityHealthCheck(provider);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("not configured");
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateSqliteReadOnlyMissingFileContext()
    {
        var connection = new SqliteConnection("Data Source=missing-healthcheck.db;Mode=ReadOnly");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AppDbContext(options);
    }
}
