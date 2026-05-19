using Messaging.Demo.Api.Messaging;

namespace Messaging.Demo.Api.Tests;

// CAPA 1 — deduplicación por MessageId dentro de ventana (slide 10).
[Trait("Category", "Unit")]
public class Unit_DedupTests
{
    private static MensajeEntrante M(string id, double seg) =>
        new(id, TimeSpan.FromSeconds(seg));

    [Fact]
    public void Duplicado_Dentro_De_Ventana_Se_Descarta()
    {
        var r = MessageDeduplicator.Procesar(TimeSpan.FromSeconds(60),
        [
            M("PED-001", 0),
            M("PED-001", 30),   // duplicado dentro de 60 s → descartado
            M("PED-002", 5),
        ]);

        Assert.Equal(new[] { "PED-001", "PED-002" }, r.Entregados);
        Assert.Equal(new[] { "PED-001" }, r.Descartados);
    }

    [Fact]
    public void Mismo_Id_Fuera_De_Ventana_Se_Reentrega()
    {
        var r = MessageDeduplicator.Procesar(TimeSpan.FromSeconds(60),
        [
            M("PED-001", 0),
            M("PED-001", 120),  // 120 s > ventana → se vuelve a entregar
        ]);

        Assert.Equal(new[] { "PED-001", "PED-001" }, r.Entregados);
        Assert.Empty(r.Descartados);
    }

    [Fact]
    public void Ids_Distintos_Todos_Entregados()
    {
        var r = MessageDeduplicator.Procesar(MessageDeduplicator.VentanaPorDefecto,
        [
            M("A", 0), M("B", 1), M("C", 2),
        ]);

        Assert.Equal(3, r.Entregados.Count);
        Assert.Empty(r.Descartados);
    }

    [Fact]
    public void Procesa_En_Orden_De_Encolado_Aunque_Llegue_Desordenado()
    {
        var r = MessageDeduplicator.Procesar(TimeSpan.FromSeconds(60),
        [
            M("X", 50),         // llega "después" en la lista
            M("X", 0),          // pero se encoló antes → este es el original
        ]);

        Assert.Single(r.Entregados);
        Assert.Single(r.Descartados);
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(19, false)]
    [InlineData(604800, true)]      // 7 días
    [InlineData(604801, false)]
    public void Ventana_Valida_Rango_SB(int seg, bool valida)
        => Assert.Equal(valida,
            MessageDeduplicator.VentanaValida(TimeSpan.FromSeconds(seg)));

    [Fact]
    public void Ventana_Fuera_De_Rango_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            MessageDeduplicator.Procesar(TimeSpan.FromSeconds(5),
                [M("A", 0)]));
}
