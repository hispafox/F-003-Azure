using Bonus.IntroIaAgentica.Demo.Api.Intro;

namespace Bonus.IntroIaAgentica.Demo.Api.Tests;

// CAPA 1 — evaluador del nivel de madurez (slide 10 + 18).
[Trait("Category", "Unit")]
public class Unit_NivelUsoTests
{
    [Fact]
    public void Solo_Prompts_Concretos_Es_Nivel1_Ayudante()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            UsaPromptsConcretos: true));
        Assert.Equal(NivelUso.Nivel1_Ayudante, r.Nivel);
    }

    [Fact]
    public void Skills_Mas_Mcp_Es_Nivel2_Colega()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            ConfiguraSkills: true,
            ConfiguraMcp: true));
        Assert.Equal(NivelUso.Nivel2_Colega, r.Nivel);
    }

    [Fact]
    public void Agents_Mas_Workflows_Es_Nivel3_Agente_Autonomo()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            ConfiguraSkills: true,
            ConfiguraMcp: true,
            TieneAgentsPropios: true,
            EjecutaWorkflowsAutomaticos: true));
        Assert.Equal(NivelUso.Nivel3_AgenteAutonomo, r.Nivel);
    }

    [Fact]
    public void Cuatro_Principios_Cumplidos_Da_4()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            SkillsEnGit: true,
            AgentsConPermisosMinimos: true,
            HumanoEnLoopAccionesConImpacto: true,
            AuditaElUsoDeIa: true));
        Assert.Equal(4, r.PrincipiosCumplidos);
    }

    [Fact]
    public void Cero_Principios_Cumplidos_Da_0_Y_Lista_4_Proximos_Pasos()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo());
        Assert.Equal(0, r.PrincipiosCumplidos);
        // 4 principios + 2 de subida nivel = al menos 4 sugerencias.
        Assert.True(r.ProximosPasos.Count >= 4);
    }

    [Fact]
    public void Nivel1_Sugiere_Configurar_Skills_Y_Mcp()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo());
        Assert.Contains(r.ProximosPasos, s =>
            s.Contains("skills", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(r.ProximosPasos, s =>
            s.Contains("MCP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nivel2_Sugiere_Agents_Y_Workflows()
    {
        var r = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            ConfiguraSkills: true,
            ConfiguraMcp: true));
        Assert.Contains(r.ProximosPasos, s =>
            s.Contains("agents", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(r.ProximosPasos, s =>
            s.Contains("Nivel 3", StringComparison.Ordinal));
    }

    [Fact]
    public void Descripcion_Refleja_El_Nivel()
    {
        var n1 = NivelUsoEvaluator.Evaluar(new EscenarioEquipo());
        Assert.Contains("conductor", n1.Descripcion, StringComparison.OrdinalIgnoreCase);

        var n3 = NivelUsoEvaluator.Evaluar(new EscenarioEquipo(
            TieneAgentsPropios: true,
            EjecutaWorkflowsAutomaticos: true));
        Assert.Contains("supervisión", n3.Descripcion, StringComparison.OrdinalIgnoreCase);
    }
}
