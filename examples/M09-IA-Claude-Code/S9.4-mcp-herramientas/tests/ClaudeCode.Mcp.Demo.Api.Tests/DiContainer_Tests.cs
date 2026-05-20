using ClaudeCode.Mcp.Demo.Api.Mcp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.Mcp.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    private const string ConfigOk = """
        {
          "mcpServers": {
            "filesystem": {
              "command": "npx",
              "args": ["-y", "@modelcontextprotocol/server-filesystem", "/home/dev/projects/repo"]
            },
            "github": {
              "command": "npx", "args": ["-y", "x"],
              "env": { "GITHUB_TOKEN": "${GH_TOKEN}" }
            }
          }
        }
        """;

    [Fact]
    public void McpPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IMcpPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IMcpPlanner>());

        var plan = planner.Planificar(
            escenario: new EscenarioMcp(
                UsaAzureDevOps: true, UsaGitHub: true,
                UsaCosmosDb: true, UsaNotionODocs: true),
            configJson: ConfigOk);

        Assert.Contains(plan.ServersRecomendados, s => s.Nombre == "azure-devops");
        Assert.Contains(plan.ServersRecomendados, s => s.Nombre == "github");
        Assert.Contains(plan.ServersRecomendados, s => s.Nombre == "azure-cosmos");
        Assert.NotNull(plan.ConfigActual);
        Assert.Equal(2, plan.ConfigActual!.Servers.Count);
        Assert.NotNull(plan.Seguridad);
        Assert.True(plan.Checklist.Count >= 6);
    }
}
