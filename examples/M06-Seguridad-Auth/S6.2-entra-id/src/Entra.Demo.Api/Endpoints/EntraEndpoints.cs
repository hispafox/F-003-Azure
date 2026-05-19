using Entra.Demo.Api.Entra;

namespace Entra.Demo.Api.Endpoints;

public sealed record TokenDto(string Jwt);
public sealed record AutorizarDto(List<string> RolesDelToken, string RolRequerido);

public static class EntraEndpoints
{
    public static void MapEntra(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var entra = app.MapGroup("/entra");

        // Slide 10 — ¿qué tipo de identidad usar en este escenario?
        entra.MapGet("/identidad", (Escenario escenario) =>
        {
            var t = IdentityTypeAdvisor.Recomendar(escenario);
            return Results.Ok(new
            {
                escenario = escenario.ToString(),
                tipo = t.ToString(),
                tieneSecreto = IdentityTypeAdvisor.TieneSecreto(t),
            });
        });

        // Slides 6-7 — ¿RBAC de Azure o rol de Entra ID?
        entra.MapGet("/rol", (string nombre) =>
        {
            var s = RoleClassifier.Clasificar(nombre);
            return Results.Ok(new
            {
                rol = nombre,
                sistema = s.ToString(),
                dondeSeAsigna = RoleClassifier.DondeSeAsigna(s),
            });
        });

        // Slide 18 — decodificar (NO validar) un JWT y ver sus claims.
        entra.MapPost("/token", (TokenDto dto) =>
        {
            try
            {
                return Results.Ok(JwtInspector.Inspeccionar(dto.Jwt));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Slide 19 — App Roles: ¿el token autoriza esta operación?
        entra.MapPost("/autorizar", (AutorizarDto dto, IAppRolesAuthorizer auth) =>
        {
            var d = auth.Autorizar(dto.RolesDelToken, dto.RolRequerido);
            return d.Autorizado ? Results.Ok(d) : Results.Json(d, statusCode: 403);
        });
    }
}
