using Datos.Demo.Api.Datos;

namespace Datos.Demo.Api.Endpoints;

public sealed record CorsDto(List<string> Origenes, bool AllowCredentials);

public static class DatosEndpoints
{
    public static void MapDatos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var d = app.MapGroup("/datos");

        // Slides 6-9 — cifrado at-rest recomendado.
        d.MapGet("/cifrado", (Sensibilidad sensibilidad, bool regulacionClaves) =>
            Results.Ok(EncryptionAdvisor.Recomendar(sensibilidad, regulacionClaves)));

        // Slide 3 — ¿versión TLS permitida?
        d.MapGet("/tls/{version}", (string version) =>
            Results.Ok(new
            {
                version,
                permitida = TlsTransitValidator.VersionPermitida(version),
            }));

        // Slide 13 — auditar una política CORS.
        d.MapPost("/cors", (CorsDto dto) =>
        {
            var v = CorsPolicyValidator.Validar(dto.Origenes, dto.AllowCredentials);
            return v.Segura ? Results.Ok(v) : Results.BadRequest(v);
        });

        // Slide 14 — checklist completo de seguridad de datos.
        d.MapPost("/checklist", (ChecklistDatos c, IDataProtectionAssessor svc) =>
            Results.Ok(svc.Evaluar(c)));
    }
}
