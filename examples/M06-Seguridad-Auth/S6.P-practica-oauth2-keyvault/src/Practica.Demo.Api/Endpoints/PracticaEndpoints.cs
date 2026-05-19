using Practica.Demo.Api.Practica;

namespace Practica.Demo.Api.Endpoints;

public sealed record PlanDto(TipoApp Tipo, string TenantId, string ClientId, string Vault);

public static class PracticaEndpoints
{
    public static void MapPractica(this IEndpointRouteBuilder app)
    {
        // Slide 9 — endpoint público (health check, sin token).
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // Slide 9 — endpoint protegido. En Azure, Easy Auth ya rechazó
        // las peticiones sin token (Return401) e inyecta las cabeceras
        // X-MS-CLIENT-PRINCIPAL-*. En local NO hay Easy Auth delante:
        // replicamos la decisión leyendo esas cabeceras (401 si faltan).
        app.MapGet("/api/perfil", (HttpContext ctx) =>
        {
            var headers = ctx.Request.Headers.ToDictionary(
                h => h.Key, h => (string?)h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            var p = EasyAuthPrincipal.Desde(headers);
            if (!p.Autenticado)
                return Results.Json(new { error = "401 — falta token (Easy Auth)" },
                    statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(new
            {
                autenticado = true,
                nombre = p.Nombre,
                identityProvider = p.IdentityProvider,
                mensaje = "Autenticado con Entra ID; los secretos vienen de Key Vault",
            });
        });

        var pr = app.MapGroup("/practica");

        // Slide 8 — acción Easy Auth + issuer por tipo de app.
        pr.MapGet("/easyauth", (TipoApp tipo, string tenantId) =>
            Results.Ok(new
            {
                tipo = tipo.ToString(),
                accion = EasyAuthAdvisor.AccionNoAutenticado(tipo),
                issuer = EasyAuthAdvisor.Issuer(tenantId),
            }));

        // Slide 7/11 — App Settings con Key Vault References.
        pr.MapGet("/appsettings", (string tenantId, string clientId, string vault) =>
        {
            var s = KeyVaultRefAppSettings.Construir(tenantId, clientId, vault);
            return Results.Ok(new
            {
                appSettings = s,
                soloReferencias = KeyVaultRefAppSettings.SoloReferencias(s),
            });
        });

        // Slides 7-8-11 — plan completo de la práctica.
        pr.MapPost("/plan", (PlanDto dto, IPracticaPlanner planner) =>
            Results.Ok(planner.Planificar(
                dto.Tipo, dto.TenantId, dto.ClientId, dto.Vault)));
    }
}
