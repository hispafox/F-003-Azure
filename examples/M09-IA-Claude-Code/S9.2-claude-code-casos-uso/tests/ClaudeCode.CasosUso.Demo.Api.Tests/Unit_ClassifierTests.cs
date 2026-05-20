using ClaudeCode.CasosUso.Demo.Api.CasosUso;

namespace ClaudeCode.CasosUso.Demo.Api.Tests;

// CAPA 1 — clasificador de caso por palabras clave (slides 2-16).
[Trait("Category", "Unit")]
public class Unit_ClassifierTests
{
    [Theory]
    [InlineData("Migrar PedidoService de .NET Framework 4.8 a .NET 10", CasoUso.MigracionLegacyANet, "2")]
    [InlineData("Genera documentación markdown de los endpoints", CasoUso.DocumentacionDesdeCodigo, "3")]
    [InlineData("Revisa los últimos 3 commits buscando problemas de seguridad", CasoUso.CodeReview, "4")]
    [InlineData("Genera datos de prueba en Cosmos DB", CasoUso.GenerarDatosPrueba, "5")]
    [InlineData("Tengo errores en producción desde las 14:00, analiza los logs", CasoUso.TroubleshootingLogs, "6")]
    [InlineData("Crea un azure-pipelines.yml para este proyecto", CasoUso.PipelineCiCd, "7")]
    [InlineData("Exporta la infraestructura del rg a bicep", CasoUso.BicepDesdeInfra, "8")]
    [InlineData("Vamos a implementar la búsqueda paso a paso", CasoUso.PairProgramming, "9")]
    [InlineData("Tengo esta especificación OpenAPI, genera la API completa", CasoUso.ApiCompletaDesdeSpec, "10")]
    [InlineData("Necesito migración de schema en Cosmos, renombrar campo", CasoUso.MigracionEsquemaBd, "11")]
    [InlineData("Genera tests de integración end-to-end con WebApplicationFactory", CasoUso.TestsIntegracionE2e, "12")]
    [InlineData("Optimiza el endpoint, P99 está demasiado alto", CasoUso.OptimizacionRendimiento, "13")]
    [InlineData("Genera README.md y architecture.md y los ADR", CasoUso.DocumentacionTecnica, "14")]
    [InlineData("Estima el coste mensual de la infraestructura", CasoUso.AnalisisCosteAzure, "15")]
    [InlineData("Rename column customer_name sin downtime con expand-contract", CasoUso.ExpandContractRefactor, "16")]
    public void Clasifica_Descripciones_Tipicas(string descripcion, CasoUso esperado, string slide)
    {
        var r = CaseClassifier.Clasificar(descripcion);
        Assert.Equal(esperado, r.Caso);
        Assert.Equal(slide, r.Slide);
        Assert.NotEmpty(r.PalabrasClaveDetectadas);
    }

    [Fact]
    public void Descripcion_Sin_Palabras_Clave_Es_Otro()
    {
        var r = CaseClassifier.Clasificar("haz algo con esto");
        Assert.Equal(CasoUso.Otro, r.Caso);
        Assert.Empty(r.PalabrasClaveDetectadas);
    }

    [Fact]
    public void Expand_Contract_Gana_A_Migracion_Esquema_Si_Hay_Ambos()
    {
        // Diseño: "expand-contract" es más específico que "renombrar columna";
        // está antes en las reglas y debe ganar.
        var r = CaseClassifier.Clasificar(
            "Renombrar columna customer_name usando expand-contract sin downtime");
        Assert.Equal(CasoUso.ExpandContractRefactor, r.Caso);
    }

    [Fact]
    public void Detecta_Multiples_Palabras_Clave_Pero_Devuelve_El_Primer_Caso()
    {
        var r = CaseClassifier.Clasificar(
            "Optimiza el endpoint /api/pedidos, P95 alto, latency mala");
        Assert.Equal(CasoUso.OptimizacionRendimiento, r.Caso);
        Assert.True(r.PalabrasClaveDetectadas.Count >= 2);
    }

    [Fact]
    public void Descripcion_Vacia_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CaseClassifier.Clasificar(" "));
    }
}
