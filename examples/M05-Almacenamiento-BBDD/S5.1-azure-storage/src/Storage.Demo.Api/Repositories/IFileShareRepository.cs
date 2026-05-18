using Azure.Storage.Files.Shares;

namespace Storage.Demo.Api.Repositories;

// Slide 3/20 — Azure Files: file share SMB/NFS (un NAS en la nube).
//
// ⚠️ Azurite NO emula Azure Files (solo Blob/Queue/Table). Por eso este
// repo NO tiene test de integración: se valida contra un Storage real
// (ver README). El contrato y el endpoint sí existen para que el alumno
// vea el SDK.
public interface IFileShareRepository
{
    Task EscribirAsync(string share, string ruta, Stream contenido);
    Task<byte[]?> LeerAsync(string share, string ruta);
}

public sealed class FileShareRepository(ShareServiceClient client) : IFileShareRepository
{
    public async Task EscribirAsync(string share, string ruta, Stream contenido)
    {
        var s = client.GetShareClient(share);
        await s.CreateIfNotExistsAsync();
        var dir = s.GetRootDirectoryClient();
        var file = dir.GetFileClient(ruta);
        await file.CreateAsync(contenido.Length);
        await file.UploadAsync(contenido);
    }

    public async Task<byte[]?> LeerAsync(string share, string ruta)
    {
        var file = client.GetShareClient(share).GetRootDirectoryClient().GetFileClient(ruta);
        if (!await file.ExistsAsync()) return null;
        var resp = await file.DownloadAsync();
        using var ms = new MemoryStream();
        await resp.Value.Content.CopyToAsync(ms);
        return ms.ToArray();
    }
}
