using Iac.Bicep.Demo.Api.Iac;

namespace Iac.Bicep.Demo.Api.Endpoints;

public sealed record BicepRequest(string Bicep);
public sealed record WhatIfRequest(string Output);
public sealed record PlanRequest(
    EscenarioIac Escenario, string? Bicep, string? WhatIfOutput);

public static class IacEndpoints
{
    public static void MapIac(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var i = app.MapGroup("/iac");

        // Slide 3 — comparativa Bicep / ARM / Terraform.
        i.MapGet("/comparativa", () => Results.Ok(ToolingComparison.Comparativa));

        // Slides 3, 19, 22 — recomendación por escenario.
        i.MapPost("/recomendar", (EscenarioIac e) =>
            Results.Ok(ToolingComparison.Recomendar(e)));

        // Slides 6, 11, 19 — linter del archivo .bicep.
        i.MapPost("/validar", (BicepRequest r) =>
            Results.Ok(BicepFileValidator.Validar(r.Bicep)));

        // Slides 5, 14 — parser del output what-if.
        i.MapPost("/whatif/parsear", (WhatIfRequest r) =>
            Results.Ok(WhatIfDiffParser.Parsear(r.Output)));

        // Plan + checklist del entregable.
        i.MapPost("/plan", (PlanRequest req, IIacPlanner planner) =>
            Results.Ok(planner.Planificar(req.Escenario, req.Bicep, req.WhatIfOutput)));
    }
}
