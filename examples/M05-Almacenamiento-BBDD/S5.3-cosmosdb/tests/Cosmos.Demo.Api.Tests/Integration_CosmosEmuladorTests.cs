using System.Net;
using System.Net.Http.Json;
using Cosmos.Demo.Api.Cosmos;
using Cosmos.Demo.Api.Domain;
using Cosmos.Demo.Api.Endpoints;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.CosmosDb;

namespace Cosmos.Demo.Api.Tests;

// CAPA 2 — round-trip REAL contra el EMULADOR de Cosmos en Docker, vía
// la API completa (WebApplicationFactory). Cubre lo que la lógica pura
// no puede: PartitionKey real, soft delete, TransactionalBatch, RU.
//
// SkippableFact: el emulador de Cosmos es pesado y a veces no arranca
// (o no hay Docker) → se SALTA y `dotnet test` queda verde (patrón
// M05-S5.1/S5.2). Cosmos NO tiene proveedor in-memory tipo SQLite, así
// que no hay CAPA "component": la lógica va a CAPA 1, el resto aquí.
[Trait("Category", "Integration")]
public class Integration_CosmosEmuladorTests
{
    [SkippableFact]
    public async Task RoundTrip_Pedidos_Contra_Emulador()
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

            // El emulador usa cert self-signed → Gateway + el HttpClient
            // que Testcontainers prepara confiando en ese cert.
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

            // El bootstrap (db + container con PK /clienteId) lo hace el
            // test, no el arranque de la app (igual criterio que S5.2).
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

            const string cli = "cli-001";

            // ── Crear (embed: items dentro, slide 14) ──
            var crear = await client.PostAsJsonAsync("/pedidos", new CrearPedidoDto(
                cli, "Pedro García",
                [
                    new CrearPedidoItemDto("p-1", "Laptop", 1, 999.99m),
                    new CrearPedidoItemDto("p-2", "Mouse", 2, 29.99m),
                ]));
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            var creado = await crear.Content.ReadFromJsonAsync<PedidoCreadoDto>();
            Assert.NotNull(creado);
            Assert.Equal(1059.97m, creado!.Total);          // 999.99 + 2×29.99
            Assert.True(creado.RuConsumidas > 0);            // slide 8

            // ── Leer por id dentro de su partición (1 RU, slide 8) ──
            var get = await client.GetAsync($"/pedidos/{cli}/{creado.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // ── Query single-partition (slide 8) ──
            var lista = await client.GetFromJsonAsync<PorClienteResp>($"/pedidos/{cli}");
            Assert.NotNull(lista);
            Assert.Contains(lista!.Pedidos, p => p.Id == creado.Id);
            Assert.True(lista.Ru > 0);

            // ── Soft delete: tras borrar, leer da 404 (slide 12) ──
            var del = await client.DeleteAsync($"/pedidos/{cli}/{creado.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
            var trasDel = await client.GetAsync($"/pedidos/{cli}/{creado.Id}");
            Assert.Equal(HttpStatusCode.NotFound, trasDel.StatusCode);

            // ── TransactionalBatch: pedido + movimiento atómico (slide 17) ──
            var batch = await client.PostAsJsonAsync("/pedidos/con-movimiento",
                new CrearPedidoConMovimientoDto(
                    new CrearPedidoDto(cli, "Pedro García",
                        [new CrearPedidoItemDto("p-9", "Teclado", 1, 89.90m)]),
                    "cargo", 89.90m));
            Assert.Equal(HttpStatusCode.OK, batch.StatusCode);
        }
        finally
        {
            testClient?.Dispose();
            if (emulador is not null)
                await emulador.DisposeAsync();
        }
    }

    private sealed record PorClienteResp(List<Pedido> Pedidos, double Ru);
}
