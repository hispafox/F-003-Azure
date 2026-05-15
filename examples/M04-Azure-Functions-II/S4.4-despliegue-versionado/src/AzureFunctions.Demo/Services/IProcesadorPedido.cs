using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 16 — dos implementaciones de la MISMA operación. El feature flag
// FEATURE_NUEVO_PROCESAMIENTO decide cuál se usa en runtime. Si la nueva
// falla en producción, se apaga el flag y vuelve la legacy sin redeploy.
public interface IProcesadorPedido
{
    string Nombre { get; }
    ResultadoProceso Procesar(Pedido pedido);
}

public sealed class ProcesadorLegacy : IProcesadorPedido
{
    public string Nombre => "legacy";

    public ResultadoProceso Procesar(Pedido pedido)
        => new(pedido.Id, Nombre, pedido.Total);
}

public sealed class ProcesadorNuevo : IProcesadorPedido
{
    public string Nombre => "nuevo";

    // La "mejora": aplica un 5% de descuento de fidelización. Si esto
    // resultara estar mal calculado en prod, apagar el flag revierte
    // al cálculo legacy instantáneamente.
    public ResultadoProceso Procesar(Pedido pedido)
        => new(pedido.Id, Nombre, Math.Round(pedido.Total * 0.95m, 2));
}

// Selector: encapsula la decisión del feature flag para que las
// funciones no lean env vars directamente (testeable).
public interface IProcesadorSelector
{
    IProcesadorPedido Seleccionar();
}

public sealed class ProcesadorSelector(
    IFeatureFlags flags,
    ProcesadorLegacy legacy,
    ProcesadorNuevo nuevo) : IProcesadorSelector
{
    public const string Flag = "NUEVO_PROCESAMIENTO";

    public IProcesadorPedido Seleccionar()
        => flags.Activo(Flag) ? nuevo : legacy;
}
