using Practica.MiniNotas.Demo.Api.MiniNotas;

namespace Practica.MiniNotas.Demo.Api.Tests;

// CAPA 1 — comparador de alcance Mini vs Completo (slide 2).
[Trait("Category", "Unit")]
public class Unit_AlcanceComparatorTests
{
    [Fact]
    public void End_To_End_Minimo_Devuelve_Mini()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            QuieresUnEndToEndMinimo: true));
        Assert.Equal(Recomendacion.Mini, r.Cual);
    }

    [Fact]
    public void Menos_De_Una_Hora_Devuelve_Mini()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            TienesMenosDeUnaHora: true));
        Assert.Equal(Recomendacion.Mini, r.Cual);
    }

    [Fact]
    public void Necesita_Auth_Devuelve_Completo()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            NecesitasAuthEntra: true));
        Assert.Equal(Recomendacion.Completo, r.Cual);
    }

    [Fact]
    public void Necesita_Functions_Y_Sb_Devuelve_Completo()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            NecesitasFunctionsYSb: true));
        Assert.Equal(Recomendacion.Completo, r.Cual);
    }

    [Fact]
    public void Necesita_Pipeline_Completo_Devuelve_Completo()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            NecesitasPipelineCompleto: true));
        Assert.Equal(Recomendacion.Completo, r.Cual);
    }

    [Fact]
    public void Proyecto_Produccion_Devuelve_Completo()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            QuieresProyectoDeProduccion: true));
        Assert.Equal(Recomendacion.Completo, r.Cual);
    }

    [Fact]
    public void Sin_Senales_Recomienda_Empezar_Por_Mini()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo());
        Assert.Equal(Recomendacion.EmpezarPorMini, r.Cual);
    }

    [Fact]
    public void Sin_Conocer_Modulos_Previos_Recomienda_Repasar()
    {
        var r = AlcanceComparator.Comparar(new EscenarioObjetivo(
            YaConocesM01M02M05: false));
        Assert.Contains(r.Justificacion, j =>
            j.Contains("Repasa M01/M02/M05", StringComparison.Ordinal));
    }

    [Fact]
    public void Incluye_Web_App_Persistencia_Crud_Tests_Y_Deploy()
    {
        Assert.Contains(Feature.WebApp, AlcanceComparator.IncluidasEnMini);
        Assert.Contains(Feature.Persistencia, AlcanceComparator.IncluidasEnMini);
        Assert.Contains(Feature.EndpointsCrud, AlcanceComparator.IncluidasEnMini);
        Assert.Contains(Feature.TestsUnitarios, AlcanceComparator.IncluidasEnMini);
        Assert.Contains(Feature.Deploy, AlcanceComparator.IncluidasEnMini);
    }

    [Fact]
    public void No_Incluye_Auth_Sb_Functions_Pipeline_AppInsights()
    {
        Assert.Contains(Feature.Auth, AlcanceComparator.NoIncluidasEnMini);
        Assert.Contains(Feature.ServiceBus, AlcanceComparator.NoIncluidasEnMini);
        Assert.Contains(Feature.Functions, AlcanceComparator.NoIncluidasEnMini);
        Assert.Contains(Feature.PipelineCiCd, AlcanceComparator.NoIncluidasEnMini);
        Assert.Contains(Feature.AppInsights, AlcanceComparator.NoIncluidasEnMini);
    }
}
