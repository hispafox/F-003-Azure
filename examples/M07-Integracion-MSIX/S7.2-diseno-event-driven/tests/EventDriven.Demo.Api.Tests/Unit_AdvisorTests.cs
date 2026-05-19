using EventDriven.Demo.Api.EventDriven;

namespace EventDriven.Demo.Api.Tests;

// CAPA 1 — tablas de decisión de diseño (slides 6, 8, 13, 22).
[Trait("Category", "Unit")]
public class Unit_AdvisorTests
{
    [Fact]
    public void Audit_Trail_Es_Event_Sourcing()
        => Assert.Equal(PatronEvento.EventSourcing,
            EventDesignAdvisor.RecomendarPatron(false, false, true));

    [Fact]
    public void Consumidor_Autonomo_Eventos_Grandes_Es_Carried_State()
        => Assert.Equal(PatronEvento.EventCarriedStateTransfer,
            EventDesignAdvisor.RecomendarPatron(true, false, false));

    [Fact]
    public void Por_Defecto_Es_Event_Notification()
        => Assert.Equal(PatronEvento.EventNotification,
            EventDesignAdvisor.RecomendarPatron(false, true, false));

    [Fact]
    public void Buen_Caso_Cuando_Pesan_Las_Senales_A_Favor()
    {
        var d = EventDesignAdvisor.EsBuenCaso(
            multiplesConsumidores: true, procesamientoPesado: true,
            escaladoIndependiente: true, disponibilidadSobreConsistencia: true,
            equipoPuedeComplejidad: true, crudSimple: false,
            consistenciaFuerteInmediata: false, volumenBajo: false);
        Assert.True(d.Recomendado);
        Assert.NotEmpty(d.Razones);
    }

    [Fact]
    public void Mal_Caso_Crud_Simple_Volumen_Bajo()
    {
        var d = EventDesignAdvisor.EsBuenCaso(
            false, false, false, false,
            equipoPuedeComplejidad: false, crudSimple: true,
            consistenciaFuerteInmediata: true, volumenBajo: true);
        Assert.False(d.Recomendado);
        Assert.Contains(d.Razones, r => r.Contains("CRUD simple"));
    }

    [Theory]
    [InlineData(3, false, EstiloSaga.Choreography)]
    [InlineData(4, false, EstiloSaga.Choreography)]
    [InlineData(5, false, EstiloSaga.Orchestration)]
    [InlineData(3, true, EstiloSaga.Orchestration)]
    public void Recomendar_Saga(int pasos, bool cond, EstiloSaga esperado)
        => Assert.Equal(esperado,
            EventDesignAdvisor.RecomendarSaga(pasos, cond));

    [Fact]
    public void Saga_Pasos_No_Positivos_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            EventDesignAdvisor.RecomendarSaga(0, false));

    [Fact]
    public void Compensacion_Es_Rollback_Inverso_De_Lo_Completado()
    {
        var seq = EventDesignAdvisor.SecuenciaCompensacion(
            ["Reservar inventario", "Cobrar tarjeta", "Programar envío"],
            falloEnPaso: 3);

        Assert.Equal(
            new[] { "Compensar: Cobrar tarjeta", "Compensar: Reservar inventario" },
            seq);
    }

    [Fact]
    public void Compensacion_Falla_Primer_Paso_No_Compensa_Nada()
        => Assert.Empty(EventDesignAdvisor.SecuenciaCompensacion(
            ["A", "B"], falloEnPaso: 1));
}
