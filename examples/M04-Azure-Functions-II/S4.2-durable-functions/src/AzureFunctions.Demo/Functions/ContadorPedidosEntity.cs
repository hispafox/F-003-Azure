using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Entities;

namespace AzureFunctions.Demo.Functions;

// Slide 17 — Durable Entity: objeto con estado que persiste entre
// ejecuciones (en Azure Storage). A diferencia de un singleton in-memory
// (que se pierde al reiniciar la instancia), una Entity sobrevive cold
// starts y escalado.
//
// Caso: contar cuántos pedidos se han completado, de forma fiable y
// sin condiciones de carrera (las operaciones de una Entity se serializan).
public sealed class ContadorPedidosState
{
    public int Completados { get; set; }
    public int Compensados { get; set; }
    public int Rechazados { get; set; }

    public void RegistrarCompletado() => Completados++;
    public void RegistrarCompensado() => Compensados++;
    public void RegistrarRechazado() => Rechazados++;

    public ContadorPedidosState Snapshot() => new()
    {
        Completados = Completados,
        Compensados = Compensados,
        Rechazados = Rechazados,
    };
}

public sealed class ContadorPedidosEntity
{
    [Function(nameof(ContadorPedidosEntity))]
    public static Task Run([EntityTrigger] TaskEntityDispatcher dispatcher)
        => dispatcher.DispatchAsync<ContadorPedidosState>();
}
