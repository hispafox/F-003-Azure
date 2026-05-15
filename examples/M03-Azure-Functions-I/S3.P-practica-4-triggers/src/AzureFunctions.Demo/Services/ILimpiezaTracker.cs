using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Lleva la cuenta de las pasadas del timer (lo que en un caso real serían
// las eliminaciones efectivas en BD). Permite que los HTTP endpoints
// puedan inspeccionar cuántas veces ha corrido el timer desde que arrancó.
public interface ILimpiezaTracker
{
    LimpiezaResultado Registrar(int registrosEliminados, bool llegoTarde);
    IReadOnlyList<LimpiezaResultado> Historial { get; }
    int TotalEjecuciones { get; }
}
