using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Storage.Demo.Api.Models;
using Storage.Demo.Api.Storage;

namespace Storage.Demo.Api.Repositories;

// Slides 7-10 — encapsula el BlobServiceClient tras una interfaz para que
// los endpoints sean finos y la integración se pueda testear contra
// Azurite (o mockear en unit tests si hiciera falta).
public interface IBlobRepository
{
    Task SubirAsync(string container, string blobName, Stream contenido,
        string contentType, IDictionary<string, string>? metadata = null);
    Task<(byte[] datos, string contentType)?> DescargarAsync(string container, string blobName);
    Task<IReadOnlyList<BlobItemDto>> ListarAsync(string container, string? prefijo);
    Task<bool> BorrarAsync(string container, string blobName);
}

public sealed class BlobRepository(BlobServiceClient client) : IBlobRepository
{
    public async Task SubirAsync(string container, string blobName, Stream contenido,
        string contentType, IDictionary<string, string>? metadata = null)
    {
        var c = client.GetBlobContainerClient(container);
        await c.CreateIfNotExistsAsync(PublicAccessType.None);
        var blob = c.GetBlobClient(blobName);
        await blob.UploadAsync(contenido, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = metadata,
        });
    }

    public async Task<(byte[] datos, string contentType)?> DescargarAsync(
        string container, string blobName)
    {
        var blob = client.GetBlobContainerClient(container).GetBlobClient(blobName);
        if (!await blob.ExistsAsync()) return null;
        var resp = await blob.DownloadContentAsync();
        return (resp.Value.Content.ToArray(), resp.Value.Details.ContentType);
    }

    public async Task<IReadOnlyList<BlobItemDto>> ListarAsync(string container, string? prefijo)
    {
        var c = client.GetBlobContainerClient(container);
        if (!await c.ExistsAsync()) return [];

        var items = new List<BlobItemDto>();
        await foreach (var b in c.GetBlobsAsync(BlobTraits.Metadata, prefix: prefijo))
        {
            items.Add(new BlobItemDto(
                b.Name,
                b.Properties.ContentLength ?? 0,
                b.Properties.LastModified,
                new Dictionary<string, string>(
                    b.Metadata ?? new Dictionary<string, string>())));
        }
        return items;
    }

    public async Task<bool> BorrarAsync(string container, string blobName)
    {
        var blob = client.GetBlobContainerClient(container).GetBlobClient(blobName);
        return await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }
}
