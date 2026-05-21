using Bonus.IntroIaAgentica.Demo.Api.Intro;

namespace Bonus.IntroIaAgentica.Demo.Api.Tests;

// CAPA 1 — comparador Claude Code vs Cowork (slide 9).
[Trait("Category", "Unit")]
public class Unit_CcVsCoworkTests
{
    [Fact]
    public void Tabla_Tiene_Las_12_Filas_Canonicas()
    {
        Assert.Equal(12, CcVsCoworkRecommender.Tabla.Count);
        Assert.Contains(CcVsCoworkRecommender.Tabla, f => f.Criterio == "Skills");
        Assert.Contains(CcVsCoworkRecommender.Tabla, f => f.Criterio == "MCP");
        Assert.Contains(CcVsCoworkRecommender.Tabla, f => f.Criterio == "Scheduled tasks");
    }

    [Fact]
    public void Dev_En_Terminal_Recomienda_Claude_Code()
    {
        var r = CcVsCoworkRecommender.Recomendar(new EscenarioUso(
            TrabajaEnTerminal: true, EditaCodigo: true, EsDeveloper: true));
        Assert.Equal(Herramienta.ClaudeCode, r.Cual);
    }

    [Fact]
    public void Pm_Con_Informes_Recomienda_Cowork()
    {
        var r = CcVsCoworkRecommender.Recomendar(new EscenarioUso(
            GeneraInformes: true,
            NecesitaScheduledTasks: true,
            EsKnowledgeWorker: true));
        Assert.Equal(Herramienta.Cowork, r.Cual);
    }

    [Fact]
    public void Equipo_Mixto_Recomienda_Ambas()
    {
        var r = CcVsCoworkRecommender.Recomendar(new EscenarioUso(
            EditaCodigo: true,
            EsDeveloper: true,
            GeneraInformes: true,
            NecesitaScheduledTasks: true,
            EsKnowledgeWorker: true));
        Assert.Equal(Herramienta.Ambas, r.Cual);
    }

    [Fact]
    public void Sin_Senales_Defaultea_A_Claude_Code()
    {
        var r = CcVsCoworkRecommender.Recomendar(new EscenarioUso());
        Assert.Equal(Herramienta.ClaudeCode, r.Cual);
    }

    [Fact]
    public void Cada_Recomendacion_Lleva_Razones_No_Vacias()
    {
        var r = CcVsCoworkRecommender.Recomendar(new EscenarioUso(EsDeveloper: true));
        Assert.NotEmpty(r.Razones);
    }
}
