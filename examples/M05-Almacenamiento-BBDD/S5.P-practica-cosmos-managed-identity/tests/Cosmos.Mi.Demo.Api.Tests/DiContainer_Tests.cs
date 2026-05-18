using Azure.Core;
using Cosmos.Mi.Demo.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Cosmos.Mi.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Construir TokenCredential +
// CosmosClient(keyless) + Container + repo NO autentica (ambos SDK son
// lazy) → corre sin Docker, Azure ni red. Único test que ejercita el
// grafo DI keyless (la integración usa key auth). Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Grafo_Keyless_Se_Resuelve_Y_Comparte_La_Credencial()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var cred = sp.GetRequiredService<TokenCredential>();
        Assert.NotNull(cred);
        Assert.NotNull(sp.GetRequiredService<CosmosClient>());
        Assert.NotNull(sp.GetRequiredService<Container>());
        Assert.NotNull(sp.GetRequiredService<IProductoRepository>());

        // Una sola credencial singleton compartida (patrón S5.4 slide 21).
        Assert.Same(cred, factory.Services.GetRequiredService<TokenCredential>());
    }
}
