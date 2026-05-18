using Azure.Storage.Blobs;
using ManagedIdentity.Demo.Api.Security;

namespace ManagedIdentity.Demo.Api.Endpoints;

public sealed record EscaneoDto(string Valor);

public static class SeguridadEndpoints
{
    public static void MapSeguridad(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var seg = app.MapGroup("/seguridad");

        // Slides 2, 10, 13 — ¿este valor de configuración expone un secreto?
        seg.MapPost("/scan", (EscaneoDto dto) =>
            Results.Ok(ConnectionSecretScanner.Escanear(dto.Valor)));

        // Slides 17, 23, 27 — rol RBAC mínimo + sufijo de App Setting MI.
        seg.MapGet("/rol", (ServicioDestino servicio, Acceso acceso) =>
        {
            var rol = RbacRoleAdvisor.Recomendar(servicio, acceso);
            return Results.Ok(new
            {
                servicio = servicio.ToString(),
                acceso = acceso.ToString(),
                rol,
                appSetting = RbacRoleAdvisor.SufijoAppSetting(servicio),
                esPeligroso = RbacRoleAdvisor.EsRolPeligroso(rol),
            });
        });

        // Slide 13 — checklist: escanea la sección "Conexiones" de la
        // config y marca cuáles llevan secreto (deberían ser 0).
        seg.MapGet("/checklist", (IConfiguration cfg) =>
        {
            var filas = cfg.GetSection("Conexiones").GetChildren().Select(c =>
            {
                var r = ConnectionSecretScanner.Escanear(c.Value ?? "");
                return new
                {
                    clave = c.Key,
                    seguro = !r.TieneSecreto,
                    esKeyVaultRef = r.EsKeyVaultReference,
                    indicadores = r.IndicadoresEncontrados,
                };
            }).ToArray();
            return Results.Ok(new { todoSeguro = filas.All(f => f.seguro), filas });
        });

        // Slides 6, 8 — demo REAL sin key: lista contenedores usando solo
        // DefaultAzureCredential. Requiere Azure + `az login` (o MI).
        app.MapGet("/blob/contenedores", async (
            BlobServiceClient blob, IConfiguration cfg) =>
        {
            if (string.IsNullOrWhiteSpace(cfg["StorageBlobEndpoint"]))
                return Results.Problem(
                    "StorageBlobEndpoint no configurado. Esta demo NO usa key: " +
                    "necesita un Storage real + `az login` (o MI en Azure).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var nombres = new List<string>();
            await foreach (var c in blob.GetBlobContainersAsync())
                nombres.Add(c.Name);
            return Results.Ok(new
            {
                conexion = "sin key — DefaultAzureCredential (slide 6)",
                contenedores = nombres,
            });
        });
    }
}
