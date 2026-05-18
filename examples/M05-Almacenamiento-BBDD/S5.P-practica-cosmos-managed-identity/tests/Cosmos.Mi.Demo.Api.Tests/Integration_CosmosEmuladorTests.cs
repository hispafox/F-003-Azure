using System.Net;
using System.Net.Http.Json;
using Cosmos.Mi.Demo.Api.Cosmos;
using Cosmos.Mi.Demo.Api.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.CosmosDb;

namespace Cosmos.Mi.Demo.Api.Tests;

// CAPA 2 — CRUD real contra el EMULADOR de Cosmos vía la API completa.
//
// OJO: el emulador NO soporta Entra ID / Managed Identity (usa una key
// fija). El camino keyless de la práctica se valida en CAPA 0 (DI) y a
// mano contra Azure (ver README). Aquí el test SUSTITUYE el CosmosClient
// keyless por uno con key del emulador para poder ejercitar el repo/CRUD
// y la partition key /categoria. SkippableFact → verde sin Docker.
[Trait("Category", "Integration")]
public class Integration_CosmosEmuladorTests
{
    [SkippableFact]
    public async Task Crud_Productos_Contra_Emulador()
    {
        CosmosDbContainer? emulador = null;
        CosmosClient? testClient = null;
        try
        {
            try
            {
                emulador = new CosmosDbBuilder(
                    "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
                    .Build();
                await emulador.StartAsync();
            }
            catch (Exception ex)
            {
                Skip.If(true, $"Emulador Cosmos no disponible: {ex.GetType().Name}");
                return;
            }

            testClient = new CosmosClient(
                emulador.GetConnectionString(),
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => emulador.HttpClient,
                    RequestTimeout = TimeSpan.FromMinutes(3),
                    LimitToEndpoint = true,
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    },
                });

            var db = (await testClient.CreateDatabaseIfNotExistsAsync(
                CosmosDefaults.Database)).Database;
            await db.CreateContainerIfNotExistsAsync(
                CosmosDefaults.Container, CosmosDefaults.PartitionKeyPath);

            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.ConfigureTestServices(services =>
                {
                    services.RemoveAll<CosmosClient>();
                    services.RemoveAll<Container>();
                    services.AddSingleton(testClient);
                    services.AddSingleton(testClient.GetContainer(
                        CosmosDefaults.Database, CosmosDefaults.Container));
                }));
            var client = factory.CreateClient();

            // ── Crear (slide 8/9) ──
            var crear = await client.PostAsJsonAsync("/productos",
                new CrearProductoDto("electronica", "Laptop", 999.99m));
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            var creado = await crear.Content.ReadFromJsonAsync<ProductoCreadoDto>();
            Assert.NotNull(creado);
            Assert.Equal("electronica", creado!.Categoria);
            Assert.True(creado.RuConsumidas > 0);                  // slide 12

            // ── Leer por id dentro de su partición (1 RU, slide 12) ──
            var get = await client.GetAsync($"/productos/electronica/{creado.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // ── Listar single-partition (slide 12) ──
            var lista = await client.GetFromJsonAsync<ListaResp>(
                "/productos?categoria=electronica");
            Assert.NotNull(lista);
            Assert.Contains(lista!.Productos, p => p.Id == creado.Id);
            Assert.False(lista.CrossPartition);

            // ── Listar cross-partition (sin categoría) ──
            var todo = await client.GetFromJsonAsync<ListaResp>("/productos");
            Assert.True(todo!.CrossPartition);
        }
        finally
        {
            testClient?.Dispose();
            if (emulador is not null)
                await emulador.DisposeAsync();
        }
    }

    private sealed record ListaResp(List<Producto> Productos, double Ru, bool CrossPartition);
}
