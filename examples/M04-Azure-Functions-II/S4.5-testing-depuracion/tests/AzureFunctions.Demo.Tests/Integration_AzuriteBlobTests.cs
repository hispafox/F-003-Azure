using AzureFunctions.Demo.Services;
using Azure.Storage.Blobs;
using Testcontainers.Azurite;

namespace AzureFunctions.Demo.Tests;

// CAPA 3 — Integration test (slide 8/15). Levanta Azurite en un container
// (Testcontainers), sube un blob de verdad, lo lee y corre la lógica del
// blob trigger contra ese contenido real.
//
// CLAVE (slide 15 + regla del proyecto "la suite siempre verde"): es un
// [SkippableFact]. Si Docker NO está disponible, el test se SALTA en vez
// de fallar — `dotnet test` sigue verde en máquinas/CI sin Docker. El
// patrón queda documentado y se ejecuta donde haya Docker.
[Trait("Category", "Integration")]
public class Integration_AzuriteBlobTests
{
    [SkippableFact]
    public async Task Blob_RoundTrip_Real_Contra_Azurite()
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
                // Docker no disponible / imagen no descargable → skip limpio.
                Skip.If(true,
                    $"Docker no disponible, integration test omitido: {ex.GetType().Name}");
                return;
            }

            // Arrange: subir un CSV real al blob storage emulado.
            var conn = azurite.GetConnectionString();
            var blobSvc = new BlobServiceClient(conn);
            var container = blobSvc.GetBlobContainerClient("uploads");
            await container.CreateIfNotExistsAsync();

            var csv = "nombre,precio\nLaptop,999\nMonitor,349\nTeclado,79";
            var blob = container.GetBlobClient("ventas.csv");
            await blob.UploadAsync(BinaryData.FromString(csv), overwrite: true);

            // Act: leer el blob (lo que el Blob trigger recibiría) y
            // pasarlo por el MISMO servicio que usa la función real.
            var descarga = await blob.DownloadContentAsync();
            var contenido = descarga.Value.Content.ToString();
            var resumen = new CsvResumenService().Procesar(contenido, "ventas.csv");

            // Assert: el round-trip real produce el resumen esperado.
            Assert.Equal("ventas.csv", resumen.Archivo);
            Assert.Equal(3, resumen.TotalFilas);
            Assert.Equal(["nombre", "precio"], resumen.Columnas);
        }
        finally
        {
            if (azurite is not null)
                await azurite.DisposeAsync();
        }
    }
}
