using ClaudeCode.Mcp.Demo.Api.Mcp;

namespace ClaudeCode.Mcp.Demo.Api.Endpoints;

public sealed record ConfigRequest(string Json);
public sealed record PlanRequest(EscenarioMcp Escenario, string? ConfigJson = null);

public static class McpEndpoints
{
    public static void MapMcp(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/mcp");

        // Slide 3 — parsea claude_desktop_config.json.
        g.MapPost("/config/parsear", (ConfigRequest r) =>
            Results.Ok(McpConfigParser.Parsear(r.Json)));

        // Slides 4-7, 11, 15 — recomienda servers MCP por escenario.
        g.MapPost("/recomendar", (EscenarioMcp e) =>
            Results.Ok(McpServerRecommender.Recomendar(e)));

        // Slide 9 — security check del config actual.
        g.MapPost("/seguridad", (ConfigRequest r) =>
            Results.Ok(McpSecurityChecker.Comprobar(McpConfigParser.Parsear(r.Json))));

        // Plan + checklist.
        g.MapPost("/plan", (PlanRequest req, IMcpPlanner planner) =>
            Results.Ok(planner.Planificar(req.Escenario, req.ConfigJson)));
    }
}
