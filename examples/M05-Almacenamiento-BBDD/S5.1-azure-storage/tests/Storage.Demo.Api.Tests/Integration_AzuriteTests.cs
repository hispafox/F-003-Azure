using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Storage.Demo.Api.Models;
using Testcontainers.Azurite;

namespace Storage.Demo.Api.Tests;

// CAPA 3 — round-trip REAL contra Azurite (Blob + Table + Queue) a través
// de la API completa (WebApplicationFactory). Azurite NO emula Azure
// Files → File Storage queda sin test (documentado en el README).
//
// SkippableFact: si Docker no está, se SALTA → `dotnet test` siempre
// verde en máquinas/CI sin Docker (mismo patrón que M04-S4.5).
[Trait("Category", "Integration")]
public class Integration_AzuriteTests
{
    [SkippableFact]
    public async Task Blob_Table_Queue_RoundTrip_Contra_Azurite()
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
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.UseSetting("StorageConnection", conn));
            var client = factory.CreateClient();

            // ── Blob: subir → listar → descargar → borrar ──
            var contenido = "factura,total\nF-1,1299.99";
            var post = new HttpRequestMessage(HttpMethod.Post, "/blob/facturas/2026/05/f-1.csv")
            {
                Content = new StringContent(contenido, Encoding.UTF8, "text/csv"),
            };
            var postResp = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

            var lista = await client.GetFromJsonAsync<List<BlobItemDto>>(
                "/blob/facturas?prefijo=2026/05/");
            Assert.NotNull(lista);
            Assert.Contains(lista!, b => b.Nombre == "2026/05/f-1.csv");

            var bajado = await client.GetAsync("/blob/facturas/2026/05/f-1.csv");
            Assert.Equal(HttpStatusCode.OK, bajado.StatusCode);
            Assert.Equal(contenido, await bajado.Content.ReadAsStringAsync());

            var del = await client.DeleteAsync("/blob/facturas/2026/05/f-1.csv");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            // ── Table: insertar → query por partición → leer por clave ──
            var crear = await client.PostAsJsonAsync("/table",
                new CrearAuditDto("2026-05-15", "pedro@x.com", "CrearPedido", "PED-1"));
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            var entidad = await crear.Content.ReadFromJsonAsync<AuditEntity>();

            var porParticion = await client.GetFromJsonAsync<List<AuditEntity>>("/table/2026-05-15");
            Assert.NotNull(porParticion);
            Assert.Contains(porParticion!, e => e.RowKey == entidad!.RowKey);

            var porClave = await client.GetAsync($"/table/2026-05-15/{entidad!.RowKey}");
            Assert.Equal(HttpStatusCode.OK, porClave.StatusCode);

            // ── Queue: encolar → recibir ──
            var enc = await client.PostAsJsonAsync("/queue/pedidos",
                new MensajeColaDto("hola-cola"));
            Assert.Equal(HttpStatusCode.Accepted, enc.StatusCode);

            var recibido = await client.GetAsync("/queue/pedidos");
            Assert.Equal(HttpStatusCode.OK, recibido.StatusCode);
            Assert.Contains("hola-cola", await recibido.Content.ReadAsStringAsync());
        }
        finally
        {
            if (azurite is not null)
                await azurite.DisposeAsync();
        }
    }
}

