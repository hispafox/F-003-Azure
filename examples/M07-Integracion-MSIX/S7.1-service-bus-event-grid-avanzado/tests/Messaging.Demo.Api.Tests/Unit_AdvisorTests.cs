using Messaging.Demo.Api.Messaging;

namespace Messaging.Demo.Api.Tests;

// CAPA 1 — árbol de decisión de servicio (slides 16/17/32) +
// clasificación de DLQ (slides 9/30/31).
[Trait("Category", "Unit")]
public class Unit_AdvisorTests
{
    [Fact]
    public void Streaming_Es_Event_Hubs()
        => Assert.Equal(ServicioMensajeria.EventHubs,
            MessagingServiceAdvisor.Recomendar(
                new EscenarioMensajeria(TipoMensaje.Streaming)).Servicio);

    [Fact]
    public void Replay_Es_Event_Hubs()
        => Assert.Equal(ServicioMensajeria.EventHubs,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.EventoNegocio, RequiereReplay: true)).Servicio);

    [Fact]
    public void Push_Webhook_Es_Event_Grid()
        => Assert.Equal(ServicioMensajeria.EventGrid,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.EventoNegocio, PushAWebhook: true)).Servicio);

    [Fact]
    public void Mensaje_Grande_Es_Premium()
    {
        var r = MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
            TipoMensaje.EventoNegocio, TamanoMensajeKb: 1024));
        Assert.Equal(ServicioMensajeria.ServiceBusPremium, r.Servicio);
        Assert.Contains("€600", r.CosteAproximado);
    }

    [Fact]
    public void Vnet_Obligatoria_Es_Premium()
        => Assert.Equal(ServicioMensajeria.ServiceBusPremium,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.Comando, RequiereVNet: true)).Servicio);

    [Fact]
    public void Fanout_Evento_Negocio_Es_Topic()
        => Assert.Equal(ServicioMensajeria.ServiceBusTopic,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.EventoNegocio,
                FanOutMultiplesSuscriptores: true)).Servicio);

    [Fact]
    public void Fifo_Punto_A_Punto_Es_ServiceBusQueue_Con_Sessions()
    {
        var r = MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
            TipoMensaje.Comando, RequiereFifo: true));
        Assert.Equal(ServicioMensajeria.ServiceBusQueue, r.Servicio);
        Assert.Contains(r.Razones, x => x.Contains("Sessions"));
    }

    [Fact]
    public void Comando_Bajo_Volumen_Es_Storage_Queue()
        => Assert.Equal(ServicioMensajeria.StorageQueue,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.Comando, OperacionesMes: 50_000)).Servicio);

    [Fact]
    public void Comando_Alto_Volumen_Es_ServiceBusQueue()
        => Assert.Equal(ServicioMensajeria.ServiceBusQueue,
            MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                TipoMensaje.Comando, OperacionesMes: 50_000_000)).Servicio);

    [Theory]
    [InlineData("MaxDeliveryCountExceeded", "reintentos")]
    [InlineData("TTLExpiredException", "a tiempo")]
    [InlineData("HeaderSizeExceeded", "Cabeceras")]
    [InlineData("SubscriptionRuleEvaluationFailed", "filtro")]
    [InlineData("AlgoRaro", "DeadLetterReason")]
    public void Clasifica_DeadLetter(string motivo, string fragmento)
        => Assert.Contains(fragmento,
            MessagingServiceAdvisor.ClasificarDeadLetter(motivo));

    [Fact]
    public void DeadLetter_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            MessagingServiceAdvisor.ClasificarDeadLetter("  "));
}
