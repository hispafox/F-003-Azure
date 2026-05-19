using Messaging.Demo.Api.Messaging;

namespace Messaging.Demo.Api.Tests;

// CAPA 1 — filtros SQL de suscripción (slides 3-5).
[Trait("Category", "Unit")]
public class Unit_SqlFilterTests
{
    private static Dictionary<string, object?> Props(
        params (string, object?)[] kv) =>
        kv.ToDictionary(x => x.Item1, x => x.Item2);

    [Theory]
    [InlineData("total > 100", 150.0, true)]
    [InlineData("total > 100", 100.0, false)]
    [InlineData("total >= 100", 100.0, true)]
    [InlineData("total < 100", 99.0, true)]
    [InlineData("total <> 100", 100.0, false)]
    [InlineData("total != 100", 101.0, true)]
    [InlineData("total = 100", 100.0, true)]
    public void Comparaciones_Numericas(string filtro, double total, bool esperado)
        => Assert.Equal(esperado,
            SqlFilterEvaluator.Coincide(filtro, Props(("total", total))));

    [Fact]
    public void Filtro_Pais_Texto()
        => Assert.True(SqlFilterEvaluator.Coincide(
            "pais = 'ES'", Props(("pais", "ES"))));

    [Fact]
    public void And_Combina_Predicados_Slide4()
        => Assert.True(SqlFilterEvaluator.Coincide(
            "prioridad = 'urgente' AND categoria = 'electronica'",
            Props(("prioridad", "urgente"), ("categoria", "electronica"))));

    [Fact]
    public void And_Falla_Si_Un_Predicado_No_Cumple()
        => Assert.False(SqlFilterEvaluator.Coincide(
            "prioridad = 'urgente' AND categoria = 'electronica'",
            Props(("prioridad", "normal"), ("categoria", "electronica"))));

    [Fact]
    public void Or_Y_Parentesis()
        => Assert.True(SqlFilterEvaluator.Coincide(
            "(total > 500 OR clienteTipo = 'premium') AND pais = 'ES'",
            Props(("total", 100.0), ("clienteTipo", "premium"), ("pais", "ES"))));

    [Theory]
    [InlineData("clienteId LIKE 'PREM-%'", "PREM-001", true)]
    [InlineData("clienteId LIKE 'PREM-%'", "STD-001", false)]
    [InlineData("clienteId NOT LIKE 'PREM-%'", "STD-001", true)]
    public void Like(string filtro, string id, bool esperado)
        => Assert.Equal(esperado,
            SqlFilterEvaluator.Coincide(filtro, Props(("clienteId", id))));

    [Fact]
    public void Propiedad_Ausente_Es_Unknown_No_Entrega()
        => Assert.False(SqlFilterEvaluator.Coincide(
            "total > 100", Props(("pais", "ES"))));

    [Fact]
    public void Is_Null_E_Is_Not_Null()
    {
        Assert.True(SqlFilterEvaluator.Coincide(
            "descuento IS NULL", Props(("pais", "ES"))));
        Assert.True(SqlFilterEvaluator.Coincide(
            "pais IS NOT NULL", Props(("pais", "ES"))));
    }

    [Fact]
    public void Filtro_Verdadero_1_Igual_1()
        => Assert.True(SqlFilterEvaluator.Coincide(
            "1=1", Props(("x", 1.0))));

    [Fact]
    public void Not_Niega()
        => Assert.False(SqlFilterEvaluator.Coincide(
            "NOT total > 100", Props(("total", 150.0))));

    [Theory]
    [InlineData("total >>> 100")]
    [InlineData("total = ")]
    [InlineData("(total > 100")]
    public void Sintaxis_Invalida_Lanza(string filtro)
        => Assert.Throws<FormatException>(() =>
            SqlFilterEvaluator.Coincide(filtro, Props(("total", 1.0))));

    [Fact]
    public void Filtro_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            SqlFilterEvaluator.Coincide("  ", Props()));
}
