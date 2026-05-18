using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedIdentity.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. No hay CAPA de integración (Entra ID
// no es emulable), así que este test es el único que ejercita el grafo
// DI real → cubre la lección M03-S3.4. Construir TokenCredential +
// BlobServiceClient NO autentica (lazy), corre sin Azure ni red.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void TokenCredential_Singleton_Compartido_Por_Los_Clientes()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var cred = sp.GetRequiredService<TokenCredential>();
        var blob = sp.GetRequiredService<BlobServiceClient>();

        Assert.NotNull(cred);
        Assert.NotNull(blob);

        // Slide 21 — la MISMA credencial singleton para todos los
        // clientes (no una credencial nueva por cliente/petición).
        Assert.Same(cred, factory.Services.GetRequiredService<TokenCredential>());
    }
}
