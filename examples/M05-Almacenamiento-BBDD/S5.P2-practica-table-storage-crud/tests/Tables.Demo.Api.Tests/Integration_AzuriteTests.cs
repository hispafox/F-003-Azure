using System.Net;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.Testing;
using Tables.Demo.Api.Domain;
using Testcontainers.Azurite;

namespace Tables.Demo.Api.Tests;

// CAPA 2 — CRUD REAL contra Azurite (que SÍ emula Table Storage) a
// través de la API completa. Mismo patrón que el Integration de S5.1.
// SkippableFact: sin Docker se SALTA → `dotnet test` siempre verde.
[Trait("Category", "Integration")]
public class Integration_AzuriteTests
{
    [SkippableFact]
    public async Task Crud_Productos_RoundTrip_Contra_Azurite()
    {
        AzuriteContainer? azurite = null;
        try
        {
            try
            {
                azurite = new AzuriteBuilder()
                    .WithImage("mcr.microsoft.com/azure-storage/azurite:3.33.0")
                    .Build();
                await azurite.StartAsync();
            }
            catch (Exception ex)
            {
                Skip.If(true, $"Docker no disponible: {ex.GetType().Name}");
                return;
            }

            var conn = azurite.GetConnectionString();

            // La tabla la crea el test (la app no hace bootstrap, igual
            // criterio que S5.2/S5.3).
            await new TableServiceClient(conn)
                .CreateTableIfNotExistsAsync(ProductosService.TablaNombre);

            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b =>
                    b.UseSetting("Storage:ConnectionString", conn));
            var client = factory.CreateClient();

            // ── POST crear ──
            var crear = await client.PostAsJsonAsync("/productos", new Producto
            {
                PartitionKey = "electronica",
                RowKey = "laptop001",
                Nombre = "Laptop Dell",
                Precio = 1299.00,
                Stock = 5,
            });
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);

            // ── POST clave inválida → 400 (TableKeys, slide 5) ──
            var malo = await client.PostAsJsonAsync("/productos", new Producto
            {
                PartitionKey = "cat/mala",
                RowKey = "x",
                Nombre = "x",
            });
            Assert.Equal(HttpStatusCode.BadRequest, malo.StatusCode);

            // ── GET por categoría (PartitionKey) ──
            var lista = await client.GetFromJsonAsync<CategoriaResp>(
                "/productos/categoria/electronica");
            Assert.NotNull(lista);
            Assert.Contains(lista!.Productos, p => p.RowKey == "laptop001");

            // ── GET uno ──
            var uno = await client.GetAsync("/productos/electronica/laptop001");
            Assert.Equal(HttpStatusCode.OK, uno.StatusCode);

            // ── PUT actualizar ──
            var put = await client.PutAsJsonAsync(
                "/productos/electronica/laptop001", new Producto
                {
                    PartitionKey = "electronica",
                    RowKey = "laptop001",
                    Nombre = "Laptop Dell XPS",
                    Precio = 1499.00,
                    Stock = 3,
                });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            // ── DELETE → luego 404 ──
            var del = await client.DeleteAsync("/productos/electronica/laptop001");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
            var tras = await client.GetAsync("/productos/electronica/laptop001");
            Assert.Equal(HttpStatusCode.NotFound, tras.StatusCode);
        }
        finally
        {
            if (azurite is not null)
                await azurite.DisposeAsync();
        }
    }

    private sealed record CategoriaResp(string Categoria, int Total, List<Producto> Productos);
}
