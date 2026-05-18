using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sql.Demo.Api.Data;
using Sql.Demo.Api.Domain;
using Testcontainers.MsSql;

namespace Sql.Demo.Api.Tests;

// CAPA 3 — round-trip REAL contra SQL Server en Docker, a través de la
// API completa (WebApplicationFactory). Aplica la migración InitialCreate
// de verdad (slide 8) y ejercita el provider SqlServer + el retry de
// Program.cs (slide 13) — cosas que SQLite no cubre.
//
// SkippableFact: si Docker no está, se SALTA → `dotnet test` siempre
// verde en máquinas/CI sin Docker (mismo patrón que M05-S5.1).
[Trait("Category", "Integration")]
public class Integration_SqlServerTests
{
    [SkippableFact]
    public async Task Crud_Y_Pedido_RoundTrip_Contra_SqlServer()
    {
        MsSqlContainer? sql = null;
        try
        {
            try
            {
                sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .Build();
                await sql.StartAsync();
            }
            catch (Exception ex)
            {
                Skip.If(true, $"Docker no disponible: {ex.GetType().Name}");
                return;
            }

            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b =>
                    b.UseSetting("SqlConnection", sql.GetConnectionString()));

            // Slide 8 / slide 35 — la migración se aplica AQUÍ (pipeline /
            // arranque del test), nunca dentro de Program.cs.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VentasDbContext>();
                await db.Database.MigrateAsync();
            }

            var client = factory.CreateClient();

            // ── Producto: crear → listar ──
            var crear = await client.PostAsJsonAsync("/productos",
                new CrearProductoDto("Auriculares", 59.95m, 8));
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            var producto = await crear.Content.ReadFromJsonAsync<Producto>();

            var lista = await client.GetFromJsonAsync<List<Producto>>("/productos");
            Assert.Contains(lista!, p => p.Nombre == "Auriculares");

            // ── Pedido: descuenta stock en una transacción (slide 12) ──
            var pedidoResp = await client.PostAsJsonAsync("/pedidos",
                new CrearPedidoDto(producto!.Id, 3));
            Assert.Equal(HttpStatusCode.Created, pedidoResp.StatusCode);
            var pedido = await pedidoResp.Content.ReadFromJsonAsync<PedidoDto>();
            Assert.Equal(179.85m, pedido!.Total);          // 59.95 × 3

            var trasPedido = await client.GetFromJsonAsync<Producto>(
                $"/productos/{producto.Id}");
            Assert.Equal(5, trasPedido!.Stock);            // 8 − 3

            // ── Sin stock → 409 Conflict, stock intacto ──
            var sinStock = await client.PostAsJsonAsync("/pedidos",
                new CrearPedidoDto(producto.Id, 999));
            Assert.Equal(HttpStatusCode.Conflict, sinStock.StatusCode);

            // ── Listar pedidos con Include (slide 31) ──
            var pedidos = await client.GetFromJsonAsync<List<PedidoDto>>("/pedidos");
            Assert.Contains(pedidos!, p => p.ProductoNombre == "Auriculares");

            // ── conn-info: SQL auth del contenedor → no Managed Identity ──
            var info = await client.GetFromJsonAsync<ConnInfo>("/sql/conn-info");
            Assert.True(info!.Configurada);
            Assert.False(info.UsaManagedIdentity);
        }
        finally
        {
            if (sql is not null)
                await sql.DisposeAsync();
        }
    }

    private sealed record ConnInfo(bool Configurada, bool UsaManagedIdentity);
}
