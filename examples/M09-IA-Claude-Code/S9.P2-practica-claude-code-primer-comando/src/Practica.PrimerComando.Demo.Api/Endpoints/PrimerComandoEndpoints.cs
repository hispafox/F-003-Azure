using Practica.PrimerComando.Demo.Api.PrimerComando;

namespace Practica.PrimerComando.Demo.Api.Endpoints;

public sealed record PromptRequest(string Prompt);

public static class PrimerComandoEndpoints
{
    public static void MapPrimerComando(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/primercomando");

        // Slide 3 — preflight ligero.
        g.MapPost("/preflight", (EscenarioPreflight e) =>
            Results.Ok(PrimerComandoPreflight.Comprobar(e)));

        // Slides 4-11 — evaluador de cada paso.
        g.MapPost("/paso", (EvidenciaPaso e) =>
            Results.Ok(PasoEvaluator.Evaluar(e)));

        // Slide 9 — referencia de slash commands.
        g.MapGet("/slash-commands",
            () => Results.Ok(PracticaPrimerComandoPlanner.SlashCommandsEsencialesSlide9));

        // Slide 12 — detector de patterns del prompt.
        g.MapPost("/prompt", (PromptRequest r) =>
            Results.Ok(PromptPatronDetector.Analizar(r.Prompt)));

        // Slide 2 — plan + checklist completo.
        g.MapPost("/plan", (PlanRequest req, IPracticaPrimerComandoPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
