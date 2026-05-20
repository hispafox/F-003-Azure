using Practica.CcMcp.Demo.Api.Practica;

namespace Practica.CcMcp.Demo.Api.Tests;

// CAPA 1 — comparador de prompts (slide 12).
[Trait("Category", "Unit")]
public class Unit_PromptComparisonTests
{
    [Fact]
    public void Prompt_Vago_Puntua_Bajo_Detallado_Alto()
    {
        var c = PromptComparison.Comparar(
            vago: "Crea un servicio de pedidos",
            medio: "Crea un PedidoService con CRUD para Cosmos DB en .NET 10",
            detallado: "Crea un PedidoService.cs para .NET 10 con Cosmos DB. " +
                "Mantén las convenciones del proyecto. Output: archivos en " +
                "src/Application/Orders. Criterio éxito: tests verdes y sin warnings.");

        Assert.True(c.Vago.Puntuacion < c.Detallado.Puntuacion);
        Assert.True(c.Detallado.Puntuacion >= 75);
    }

    [Fact]
    public void Delta_Vago_A_Detallado_Es_Positivo()
    {
        var c = PromptComparison.Comparar(
            "crea algo",
            "Crea un servicio en .NET 10",
            "CONTEXTO: .NET 10. Mantén las convenciones. Output: archivos. " +
                "Criterio éxito: tests verdes.");
        Assert.True(c.DeltaVagoADetallado > 0);
    }

    [Fact]
    public void Detallado_Sin_Criterio_Exito_Avisa_En_Lecciones()
    {
        var c = PromptComparison.Comparar(
            "crea algo",
            "Crea un servicio en .NET 10",
            "CONTEXTO: proyecto .NET 10. Mantén convenciones. " +
                "Output: archivos en src/Application/. Sin criterio medible.");
        Assert.Contains(c.Lecciones, l =>
            l.Contains("criterio éxito", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detecta_Cuatro_Ingredientes_En_Detallado()
    {
        var c = PromptComparison.Comparar(
            "x",
            "y",
            "CONTEXTO: proyecto .NET 10. Mantén la API pública estable. " +
                "Output: archivos en src/. Criterio éxito: tests verdes.");
        Assert.Equal(4, c.Detallado.IngredientesDetectados.Count);
    }

    [Fact]
    public void Vago_Muy_Corto_Tiene_Puntuacion_Capada_A_25()
    {
        var c = PromptComparison.Comparar(
            "json output",
            "Crea un servicio",
            "CONTEXTO: .NET 10. Mantén. Output: archivos. Criterio: tests.");
        Assert.True(c.Vago.Puntuacion <= 25);
    }

    [Fact]
    public void Lecciones_Incluyen_Puntuaciones_Vago_Y_Detallado()
    {
        var c = PromptComparison.Comparar(
            "x todo",
            "Crea un servicio en .NET",
            "CONTEXTO: .NET 10. Mantén. Output: archivos. Criterio: tests verdes.");
        Assert.Contains(c.Lecciones, l =>
            l.Contains("/100", StringComparison.Ordinal));
    }

    [Fact]
    public void Prompt_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PromptComparison.Comparar(" ", "medio", "detallado"));
    }
}
