using System.Security.Cryptography;
using System.Text;
using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Endpoints;

public static class SecretsEndpoints
{
    public static IEndpointRouteBuilder MapSecrets(this IEndpointRouteBuilder app)
    {
        // Slides 9, 25, 27 — En Azure, AppOptions:ApiKey debería venir como
        // Key Vault Reference. Aquí NUNCA devolvemos el valor en claro: solo
        // metadatos verificables (longitud, fingerprint, fuente detectada).
        // Si la fuente detectada es "key-vault-reference-unresolved" sabemos
        // que el MI no tiene rol o que el secret no existe.
        app.MapGet("/secrets/api-key/check", (IOptions<AppOptions> options) =>
        {
            var key = options.Value.ApiKey;

            string source = key switch
            {
                _ when key.StartsWith("@Microsoft.KeyVault", StringComparison.Ordinal)
                    => "key-vault-reference-unresolved",
                _ when key.StartsWith("local-api-key", StringComparison.Ordinal)
                    => "default-appsettings",
                _ => "explicit"
            };

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

            return Results.Ok(new
            {
                isPresent = !string.IsNullOrWhiteSpace(key),
                length = key.Length,
                fingerprint = hash[..16],
                source
            });
        });

        return app;
    }
}
