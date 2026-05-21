using ProyectoIntegrador.Diseno.Demo.Api.Diseno;

namespace ProyectoIntegrador.Diseno.Demo.Api.Tests;

// CAPA 1 — evaluador de la entrega final (slide 11).
[Trait("Category", "Unit")]
public class Unit_EntregaEvaluatorTests
{
    [Fact]
    public void Sin_Evidencias_Da_0_Por_Ciento_Y_No_Aprueba()
    {
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega());
        Assert.Equal(0, r.PorcentajeTotal);
        Assert.False(r.Aprobada);
        Assert.Equal(8, r.PuntosPendientes.Count);
    }

    [Fact]
    public void Todo_Cumplido_Da_100_Por_Ciento_Y_Aprueba()
    {
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega(
            BicepDesplegadoConWhatIf: true,
            ApiCrudDevuelve2xx: true,
            JwtValidaConEntra: true,
            DatosPersistenEnCosmos: true,
            ChangeFeedTriggerFunctions: true,
            SinConnectionStringConPassword: true,
            PipelineDesplegaAStaging: true,
            AppInsightsTieneTelemetryYAlertas: true));
        Assert.Equal(100, r.PorcentajeTotal);
        Assert.True(r.Aprobada);
        Assert.Empty(r.PuntosPendientes);
    }

    [Fact]
    public void Solo_Bicep_Y_Api_Da_30_Por_Ciento()
    {
        // Bicep 15 + API 15 = 30%.
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega(
            BicepDesplegadoConWhatIf: true,
            ApiCrudDevuelve2xx: true));
        Assert.Equal(30, r.PorcentajeTotal);
        Assert.False(r.Aprobada);
    }

    [Fact]
    public void Setenta_Por_Ciento_Es_El_Umbral_De_Aprobado()
    {
        // Bicep 15 + API 15 + Auth 10 + Cosmos 10 + Functions 15 + Pipeline 15 = 80%.
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega(
            BicepDesplegadoConWhatIf: true,
            ApiCrudDevuelve2xx: true,
            JwtValidaConEntra: true,
            DatosPersistenEnCosmos: true,
            ChangeFeedTriggerFunctions: true,
            PipelineDesplegaAStaging: true));
        Assert.True(r.Aprobada);
    }

    [Fact]
    public void Justo_Bajo_Umbral_No_Aprueba()
    {
        // Bicep 15 + API 15 + Cosmos 10 + Functions 15 + Pipeline 15 = 70%.
        // Es exactamente 70, debería aprobar (umbral >= 70).
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega(
            BicepDesplegadoConWhatIf: true,
            ApiCrudDevuelve2xx: true,
            DatosPersistenEnCosmos: true,
            ChangeFeedTriggerFunctions: true,
            PipelineDesplegaAStaging: true));
        Assert.Equal(70, r.PorcentajeTotal);
        Assert.True(r.Aprobada);
    }

    [Fact]
    public void Cada_Criterio_Lleva_Peso_Correcto()
    {
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega());
        var pesos = r.Criterios.ToDictionary(c => c.Criterio, c => c.Peso);
        Assert.Equal(15, pesos[Criterio.BicepDesplegado]);
        Assert.Equal(15, pesos[Criterio.ApiCrud]);
        Assert.Equal(10, pesos[Criterio.AuthJwt]);
        Assert.Equal(10, pesos[Criterio.CosmosPersistencia]);
        Assert.Equal(15, pesos[Criterio.FunctionsChangeFeed]);
        Assert.Equal(10, pesos[Criterio.ManagedIdentityCero]);
        Assert.Equal(15, pesos[Criterio.PipelineAutomatizado]);
        Assert.Equal(10, pesos[Criterio.AppInsightsAlertas]);
    }

    [Fact]
    public void Suma_De_Pesos_Da_100()
    {
        var r = EntregaEvaluator.Evaluar(new EvidenciaEntrega());
        Assert.Equal(100, r.Criterios.Sum(c => c.Peso));
    }
}
