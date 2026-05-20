using WizardMsix.Demo.Api.Wizard;

namespace WizardMsix.Demo.Api.Endpoints;

public sealed record PlanRequest(
    ContextoEmpaquetado Contexto, ParametrosWizard Parametros);

public static class WizardEndpoints
{
    public static void MapWizard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var w = app.MapGroup("/wizard");

        // Slide 15 — qué comandos CLI ejecuta el wizard por debajo.
        w.MapPost("/expandir", (ParametrosWizard p) =>
            Results.Ok(WizardComandosExpander.Expandir(p)));

        // Slide 15/17 — ¿wizard o CLI?
        w.MapPost("/elegir", (ContextoEmpaquetado c) =>
            Results.Ok(WizardVsCliAdvisor.Recomendar(c)));

        // Slide 17 — lo que el wizard NO permite.
        w.MapGet("/limitaciones", () =>
            Results.Ok(WizardVsCliAdvisor.LimitacionesWizard));

        // Slide 16 — diagnosticar un error/codigo.
        w.MapGet("/troubleshoot", (string codigoOMensaje) =>
        {
            var d = MsixErrorTroubleshooter.Diagnosticar(codigoOMensaje);
            return d is null
                ? Results.NotFound(new { mensaje = "Sin entrada en el catálogo (slide 16)." })
                : Results.Ok(d);
        });

        // Slide 16 — catálogo completo de errores.
        w.MapGet("/errores", () =>
            Results.Ok(MsixErrorTroubleshooter.Todos()));

        // Plan + checklist (slide 19).
        w.MapPost("/plan", (PlanRequest req, IPracticaMsixWizardPlanner planner) =>
            Results.Ok(planner.Planificar(req.Contexto, req.Parametros)));
    }
}
