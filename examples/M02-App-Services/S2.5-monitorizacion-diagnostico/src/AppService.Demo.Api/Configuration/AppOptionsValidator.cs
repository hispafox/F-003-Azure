using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Configuration;

// Slide 22 — Validador custom: complementa los DataAnnotations del POCO con
// reglas que necesitan lógica (cross-field, parsing, etc.). Se ejecuta
// junto con ValidateDataAnnotations() en ValidateOnStart, así que un fallo
// aquí impide que la app arranque (mejor que descubrirlo en runtime).
public sealed class AppOptionsValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        var errors = new List<string>();

        if (options.ApiKey.Length < 8)
        {
            errors.Add("AppOptions:ApiKey must be at least 8 characters long");
        }

        if (options.ConnectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !options.ConnectionString.Contains("Encrypt=true", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("AppOptions:ConnectionString uses Password= but does not require Encrypt=true");
        }

        if (options.ApiKey.StartsWith("@Microsoft.KeyVault", StringComparison.Ordinal))
        {
            errors.Add(
                "AppOptions:ApiKey looks like an unresolved Key Vault reference. " +
                "Check the Managed Identity has 'Key Vault Secrets User' on the vault.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
