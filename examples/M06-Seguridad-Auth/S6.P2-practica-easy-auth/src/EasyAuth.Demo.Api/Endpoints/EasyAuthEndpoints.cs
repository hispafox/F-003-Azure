using EasyAuth.Demo.Api.EasyAuth;

namespace EasyAuth.Demo.Api.Endpoints;

public sealed record PlanDto(TipoApp Tipo, string Proveedor, string AppUrl);

public static class EasyAuthEndpoints
{
    private static IReadOnlyDictionary<string, string?> Headers(HttpContext ctx) =>
        ctx.Request.Headers.ToDictionary(
            h => h.Key, h => (string?)h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

    public static void MapEasyAuth(this IEndpointRouteBuilder app)
    {
        // Endpoint público (health check, sin auth) — slide 2.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // Página protegida. En Azure, Easy Auth (sitio web) redirige con
        // 302 al login si no hay sesión (slide 5/6); aquí replicamos ese
        // contrato leyendo las cabeceras X-MS-CLIENT-PRINCIPAL-*.
        app.MapGet("/", (HttpContext ctx) =>
        {
            var p = EasyAuthHeaders.Desde(Headers(ctx));
            if (!p.Autenticado)
                return Results.Redirect(
                    AuthEndpoints.Login("aad", "/"), permanent: false);

            return Results.Ok(new
            {
                mensaje = "App protegida con Easy Auth — has pasado el control",
                usuario = p.Nombre,
                identityProvider = p.IdentityProvider,
            });
        });

        // Slide 4 — /.auth/me: claims del usuario (vacío si no hay sesión,
        // igual que el endpoint real de App Service).
        app.MapGet(AuthEndpoints.Me, (HttpContext ctx) =>
        {
            var p = EasyAuthHeaders.Desde(Headers(ctx));
            return p.Autenticado
                ? Results.Ok(new[] { new { p.Nombre, p.Id, p.IdentityProvider } })
                : Results.Ok(Array.Empty<object>());
        });

        var ea = app.MapGroup("/easyauth");

        // Slides 2-5 — config recomendada por tipo de app.
        ea.MapGet("/config", (TipoApp tipo) =>
            Results.Ok(new
            {
                tipo = tipo.ToString(),
                accionNoAutenticado = EasyAuthConfigAdvisor.AccionNoAutenticado(tipo),
                codigoHttp = EasyAuthConfigAdvisor.CodigoHttp(tipo),
                tokenStore = EasyAuthConfigAdvisor.TokenStorePorDefecto,
                proveedores = EasyAuthConfigAdvisor.Proveedores
                    .Select(x => x.ToString()),
            }));

        // Slides 2-6 — plan de configuración + checklist del entregable.
        ea.MapPost("/plan", (PlanDto dto, IEasyAuthSetupPlanner planner) =>
            Results.Ok(planner.Planificar(dto.Tipo, dto.Proveedor, dto.AppUrl)));
    }
}
