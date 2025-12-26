using System.Collections.Generic;
using BobCrm.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BobCrm.Api.Tests;

public class ConfigurationValidationTests
{
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "BobCrm.Api.Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void ValidateJwt_ShouldThrow_WhenMissingKey_InNonDev()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "BobCrm.Api",
            ["Jwt:Audience"] = "BobCrm.Client"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => BobCrmConfigurationValidation.ValidateJwt(cfg, env));
        Assert.Contains("Jwt:Key is required", ex.Message);
    }

    [Fact]
    public void ValidateJwt_ShouldThrow_WhenKeyTooShort_InNonDev()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "too-short",
            ["Jwt:Issuer"] = "BobCrm.Api",
            ["Jwt:Audience"] = "BobCrm.Client"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => BobCrmConfigurationValidation.ValidateJwt(cfg, env));
        Assert.Contains("at least 32 characters", ex.Message);
    }

    [Fact]
    public void ValidateJwt_ShouldThrow_WhenMissingIssuerOrAudience_InNonDev()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('a', 32),
            ["Jwt:Issuer"] = "BobCrm.Api"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => BobCrmConfigurationValidation.ValidateJwt(cfg, env));
        Assert.Contains("Jwt:Audience is required", ex.Message);
    }

    [Fact]
    public void GetCorsAllowedOrigins_ShouldThrow_WhenMissing_InNonDev()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var cfg = BuildConfig(new Dictionary<string, string?>());

        var ex = Assert.Throws<InvalidOperationException>(() => BobCrmConfigurationValidation.GetCorsAllowedOrigins(cfg, env));
        Assert.Contains("Cors:AllowedOrigins", ex.Message);
    }

    [Fact]
    public void GetCorsAllowedOrigins_ShouldSupportSemicolonFormat()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var cfg = BuildConfig(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins"] = "https://a.example; https://b.example"
        });

        var origins = BobCrmConfigurationValidation.GetCorsAllowedOrigins(cfg, env);
        Assert.Equal(2, origins.Length);
        Assert.Equal("https://a.example", origins[0]);
        Assert.Equal("https://b.example", origins[1]);
    }
}

