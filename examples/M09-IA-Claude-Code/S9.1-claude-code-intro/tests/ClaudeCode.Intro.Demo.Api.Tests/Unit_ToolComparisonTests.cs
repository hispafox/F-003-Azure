using ClaudeCode.Intro.Demo.Api.ClaudeCode;

namespace ClaudeCode.Intro.Demo.Api.Tests;

// CAPA 1 — Claude Code vs GitHub Copilot (slide 5).
[Trait("Category", "Unit")]
public class Unit_ToolComparisonTests
{
    [Fact]
    public void Tabla_Tiene_Al_Menos_Seis_Filas()
    {
        Assert.True(ToolComparison.Tabla.Count >= 6);
    }

    [Fact]
    public void Solo_Autocompletado_En_Ide_Recomienda_Copilot()
    {
        var r = ToolComparison.Recomendar(new EscenarioElegirHerramienta(
            QuieresAutocompletadoEnIde: true,
            NecesitasAgenteQueEjecuta: false,
            ProyectoMultiArchivo: false,
            NecesitasMcp: false));
        Assert.Equal(HerramientaIa.GithubCopilot, r.Herramienta);
    }

    [Fact]
    public void Agente_Sin_Ide_Recomienda_Claude_Code()
    {
        var r = ToolComparison.Recomendar(new EscenarioElegirHerramienta(
            QuieresAutocompletadoEnIde: false,
            NecesitasAgenteQueEjecuta: true));
        Assert.Equal(HerramientaIa.ClaudeCode, r.Herramienta);
    }

    [Fact]
    public void Mcp_Sin_Ide_Recomienda_Claude_Code()
    {
        var r = ToolComparison.Recomendar(new EscenarioElegirHerramienta(
            QuieresAutocompletadoEnIde: false,
            NecesitasMcp: true));
        Assert.Equal(HerramientaIa.ClaudeCode, r.Herramienta);
    }

    [Fact]
    public void Senales_De_Ambos_Recomienda_Combinacion()
    {
        var r = ToolComparison.Recomendar(new EscenarioElegirHerramienta(
            QuieresAutocompletadoEnIde: true,
            NecesitasAgenteQueEjecuta: true,
            NecesitasMcp: true));
        Assert.Equal(HerramientaIa.Combinacion, r.Herramienta);
    }

    [Fact]
    public void Recomendacion_Incluye_Razones_No_Vacias()
    {
        var r = ToolComparison.Recomendar(new EscenarioElegirHerramienta());
        Assert.NotEmpty(r.Razones);
    }
}
