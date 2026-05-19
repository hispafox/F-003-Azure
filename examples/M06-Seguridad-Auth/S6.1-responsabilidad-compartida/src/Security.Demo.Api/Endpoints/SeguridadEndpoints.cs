using Security.Demo.Api.Security;

namespace Security.Demo.Api.Endpoints;

public sealed record ScanDto(string Contenido);

public static class SeguridadEndpoints
{
    public static void MapSeguridad(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var seg = app.MapGroup("/seguridad");

        // Slide 3 — ¿quién es responsable de esta capa en este modelo?
        seg.MapGet("/responsabilidad", (Capa capa, ModeloServicio modelo) =>
            Results.Ok(new
            {
                capa = capa.ToString(),
                modelo = modelo.ToString(),
                responsable = ResponsibilityMatrix.Responsable(capa, modelo).ToString(),
                siempreTuya = ResponsibilityMatrix.SiempreTuya(capa),
            }));

        // Slide 20 — STRIDE: amenaza + mitigaciones de una categoría.
        seg.MapGet("/stride/{categoria}", (Stride categoria) =>
            Results.Ok(StrideAnalyzer.Describir(categoria)));

        // Slides 4, 22 — escanear contenido en busca de secretos.
        seg.MapPost("/scan", (ScanDto dto) =>
        {
            var hallazgos = SecretScanner.Escanear(dto.Contenido);
            return Results.Ok(new { tieneSecretos = hallazgos.Count > 0, hallazgos });
        });

        // Slides 10, 17 — Secure Score a partir del checklist.
        seg.MapPost("/secure-score", (ChecklistSeguridad c, ISecureScore svc) =>
            Results.Ok(svc.Calcular(c)));
    }
}
