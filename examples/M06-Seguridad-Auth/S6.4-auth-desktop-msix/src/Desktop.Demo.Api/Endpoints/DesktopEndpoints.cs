using Desktop.Demo.Api.Desktop;

namespace Desktop.Demo.Api.Endpoints;

public sealed record PlanDto(ContextoDesktop Contexto, string ClientId, EstadoToken Estado);

public static class DesktopEndpoints
{
    public static void MapDesktop(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var d = app.MapGroup("/desktop");

        // Slide 3 — método de auth recomendado por contexto.
        d.MapGet("/flujo", (ContextoDesktop contexto) =>
        {
            var m = DesktopFlowAdvisor.Recomendar(contexto);
            return Results.Ok(new
            {
                contexto = contexto.ToString(),
                metodo = m.ToString(),
                recomendado = DesktopFlowAdvisor.EsRecomendado(m),
                clientePublico = DesktopFlowAdvisor.EsClientePublico,
            });
        });

        // Slides 7, 11 — redirect URI correcto por tipo de app.
        d.MapGet("/redirect-uri", (TipoApp tipo, string clientId) =>
        {
            var uri = RedirectUriAdvisor.Para(tipo, clientId);
            return Results.Ok(new
            {
                tipo = tipo.ToString(),
                redirectUri = uri,
                broker = RedirectUriAdvisor.EsBroker(uri),
                legacy = RedirectUriAdvisor.EsLegacy(uri),
            });
        });

        // Slides 10, 12 — siguiente acción del ciclo de token.
        d.MapPost("/token-accion", (EstadoToken estado) =>
        {
            var a = TokenLifecycle.Siguiente(estado);
            return Results.Ok(new
            {
                accion = a.ToString(),
                requiereUi = TokenLifecycle.RequiereUi(a),
            });
        });

        // Slides 3-12 — plan de auth desktop completo.
        d.MapPost("/plan", (PlanDto dto, IDesktopAuthPlanner planner) =>
            Results.Ok(planner.Planificar(dto.Contexto, dto.ClientId, dto.Estado)));
    }
}
