using AppService.Demo.Api.Configuration;

namespace AppService.Demo.Api.Tests;

public class AppOptionsValidatorTests
{
    private static AppOptions Build(string? apiKey = null, string? connectionString = null) => new()
    {
        Greeting = "ok",
        EnvironmentLabel = "test",
        ApiKey = apiKey ?? "12345678",
        ConnectionString = connectionString ?? "Server=localhost;Database=db;Integrated Security=true"
    };

    [Fact]
    public void Validate_Succeeds_For_Valid_Options()
    {
        var validator = new AppOptionsValidator();

        var result = validator.Validate(null, Build());

        Assert.True(result.Succeeded, string.Join(",", result.Failures ?? []));
    }

    [Fact]
    public void Validate_Fails_When_ApiKey_Too_Short()
    {
        var validator = new AppOptionsValidator();

        var result = validator.Validate(null, Build(apiKey: "abc"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_When_Password_Without_Encrypt()
    {
        var validator = new AppOptionsValidator();

        var result = validator.Validate(null, Build(
            connectionString: "Server=tcp:srv.database.windows.net;Database=db;User=admin;Password=p"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Encrypt=true"));
    }

    [Fact]
    public void Validate_Fails_When_ApiKey_Is_Unresolved_KeyVault_Reference()
    {
        var validator = new AppOptionsValidator();

        var result = validator.Validate(null, Build(
            apiKey: "@Microsoft.KeyVault(VaultName=kv;SecretName=ApiKey)"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Key Vault"));
    }
}
