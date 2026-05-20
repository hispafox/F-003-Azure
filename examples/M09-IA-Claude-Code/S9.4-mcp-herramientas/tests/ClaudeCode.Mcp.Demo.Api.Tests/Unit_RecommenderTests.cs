using ClaudeCode.Mcp.Demo.Api.Mcp;

namespace ClaudeCode.Mcp.Demo.Api.Tests;

// CAPA 1 — recomendador de MCP servers (slides 4-7, 11, 15).
[Trait("Category", "Unit")]
public class Unit_RecommenderTests
{
    [Fact]
    public void Filesystem_Esta_Siempre_Incluido()
    {
        var r = McpServerRecommender.Recomendar(new EscenarioMcp());
        Assert.Contains(r, s => s.Nombre == "filesystem");
    }

    [Fact]
    public void Equipo_Con_Ado_Recibe_Server_Azure_DevOps()
    {
        var r = McpServerRecommender.Recomendar(new EscenarioMcp(UsaAzureDevOps: true));
        Assert.Contains(r, s => s.Nombre == "azure-devops");
        var ado = r.Single(s => s.Nombre == "azure-devops");
        Assert.Contains(ado.PermisosMinimos, p => p.Contains("Read", StringComparison.Ordinal));
    }

    [Fact]
    public void Equipo_Sin_Ado_No_Recibe_Server_Azure_DevOps()
    {
        var r = McpServerRecommender.Recomendar(new EscenarioMcp(UsaAzureDevOps: false));
        Assert.DoesNotContain(r, s => s.Nombre == "azure-devops");
    }

    [Theory]
    [InlineData("UsaGitHub", "github")]
    [InlineData("UsaCosmosDb", "azure-cosmos")]
    [InlineData("UsaSqlServer", "sql-server")]
    [InlineData("UsaPostgres", "postgres")]
    [InlineData("UsaNotionODocs", "notion")]
    [InlineData("UsaSlackOTeams", "slack")]
    [InlineData("UsaJiraOLinear", "linear")]
    [InlineData("NecesitaBrowserAutomation", "puppeteer")]
    [InlineData("NecesitaObservabilidad", "sentry")]
    public void Cada_Flag_Anade_Su_Server(string propiedad, string serverEsperado)
    {
        // Construye el escenario con sólo el flag indicado en true.
        var e = propiedad switch
        {
            "UsaGitHub" => new EscenarioMcp(UsaGitHub: true),
            "UsaCosmosDb" => new EscenarioMcp(UsaCosmosDb: true),
            "UsaSqlServer" => new EscenarioMcp(UsaSqlServer: true),
            "UsaPostgres" => new EscenarioMcp(UsaPostgres: true),
            "UsaNotionODocs" => new EscenarioMcp(UsaNotionODocs: true),
            "UsaSlackOTeams" => new EscenarioMcp(UsaSlackOTeams: true),
            "UsaJiraOLinear" => new EscenarioMcp(UsaJiraOLinear: true),
            "NecesitaBrowserAutomation" => new EscenarioMcp(NecesitaBrowserAutomation: true),
            "NecesitaObservabilidad" => new EscenarioMcp(NecesitaObservabilidad: true),
            _ => throw new InvalidOperationException(propiedad),
        };

        var r = McpServerRecommender.Recomendar(e);
        Assert.Contains(r, s => s.Nombre == serverEsperado);
    }

    [Fact]
    public void Cada_Server_Tiene_Permisos_Minimos_No_Vacios()
    {
        var r = McpServerRecommender.Recomendar(new EscenarioMcp(
            UsaAzureDevOps: true, UsaGitHub: true, UsaCosmosDb: true,
            UsaNotionODocs: true, UsaSlackOTeams: true));
        Assert.All(r, s => Assert.NotEmpty(s.PermisosMinimos));
    }

    [Fact]
    public void Cada_Server_Tiene_Categoria_Y_Slide()
    {
        var r = McpServerRecommender.Recomendar(new EscenarioMcp(
            UsaAzureDevOps: true, UsaGitHub: true));
        Assert.All(r, s => Assert.False(string.IsNullOrWhiteSpace(s.Slide)));
    }
}
