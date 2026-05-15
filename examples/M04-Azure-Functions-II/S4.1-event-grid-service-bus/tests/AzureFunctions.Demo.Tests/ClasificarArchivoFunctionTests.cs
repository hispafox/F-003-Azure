using System.Text.Json;
using AzureFunctions.Demo.Functions;
using Azure.Messaging.EventGrid;

namespace AzureFunctions.Demo.Tests;

public class ClasificarArchivoFunctionTests
{
    private static EventGridEvent BlobCreated(string url)
    {
        var data = BinaryData.FromString(JsonSerializer.Serialize(new
        {
            url,
            contentLength = 1024,
            contentType = "application/octet-stream",
        }));

        return new EventGridEvent(
            subject: $"/blobServices/default/containers/uploads/blobs/{Path.GetFileName(url)}",
            eventType: "Microsoft.Storage.BlobCreated",
            dataVersion: "1.0",
            data: data);
    }

    [Fact]
    public void Clasificar_Pdf_Encola_A_Facturas()
    {
        var (fn, tracker) = TestHost.NewClasificar();

        var result = fn.Clasificar(BlobCreated("https://storage/uploads/factura-001.pdf"));

        Assert.NotNull(result.MensajeFacturas);
        Assert.Null(result.MensajeImports);

        using var doc = JsonDocument.Parse(result.MensajeFacturas!);
        Assert.Equal("factura", doc.RootElement.GetProperty("tipo").GetString());
        Assert.Equal(1, tracker.Snapshot().Clasificados);
    }

    [Fact]
    public void Clasificar_Csv_Encola_A_Imports()
    {
        var (fn, tracker) = TestHost.NewClasificar();

        var result = fn.Clasificar(BlobCreated("https://storage/uploads/data-001.csv"));

        Assert.Null(result.MensajeFacturas);
        Assert.NotNull(result.MensajeImports);

        using var doc = JsonDocument.Parse(result.MensajeImports!);
        Assert.Equal("import", doc.RootElement.GetProperty("tipo").GetString());
        Assert.Equal(1, tracker.Snapshot().Clasificados);
    }

    [Fact]
    public void Clasificar_Tipo_No_Relevante_No_Encola_A_Ningun_Sitio()
    {
        // Slide 19/24 — null en ambos outputs = el binding NO se materializa.
        // No queremos llenar las queues con basura (imágenes, vídeos, etc.).
        var (fn, tracker) = TestHost.NewClasificar();

        var result = fn.Clasificar(BlobCreated("https://storage/uploads/foto.jpg"));

        Assert.Null(result.MensajeFacturas);
        Assert.Null(result.MensajeImports);
        Assert.Equal(0, tracker.Snapshot().Clasificados);
    }

    [Fact]
    public void Clasificar_Evento_No_Es_BlobCreated_Se_Ignora()
    {
        // Aunque la suscripción de EG ya filtre por tipo, defensive: si nos
        // llega otro tipo (Microsoft.Storage.BlobDeleted, etc.) no hacemos nada.
        var (fn, _) = TestHost.NewClasificar();
        var evento = new EventGridEvent(
            subject: "/blob/x",
            eventType: "Microsoft.Storage.BlobDeleted",
            dataVersion: "1.0",
            data: BinaryData.FromString("{}"));

        var result = fn.Clasificar(evento);

        Assert.Null(result.MensajeFacturas);
        Assert.Null(result.MensajeImports);
    }

    [Fact]
    public void Clasificar_BlobCreated_Sin_Url_Se_Ignora()
    {
        var (fn, _) = TestHost.NewClasificar();
        var evento = new EventGridEvent(
            subject: "/blob/x",
            eventType: "Microsoft.Storage.BlobCreated",
            dataVersion: "1.0",
            data: BinaryData.FromString("{}"));

        var result = fn.Clasificar(evento);

        Assert.Null(result.MensajeFacturas);
        Assert.Null(result.MensajeImports);
    }

    [Theory]
    [InlineData("doc.PDF", "factura")]   // mayúsculas
    [InlineData("data.CSV", "import")]   // mayúsculas
    [InlineData("DOC.Pdf", "factura")]   // mixto
    [InlineData("a.txt", null)]
    [InlineData("noextension", null)]
    public void ClasificarUrl_Es_CaseInsensitive_Sobre_Extension(string filename, string? esperado)
    {
        var url = $"https://storage/uploads/{filename}";
        Assert.Equal(esperado, ClasificarArchivoFunction.ClasificarUrl(url));
    }
}
