using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    public static ProductosVersionadasFunctions NewVersionadas()
        => new(new InMemoryProductoCatalogo());

    public static OperacionesFunctions NewOperaciones(
        IFeatureFlags flags, IHealthAggregator? health = null)
    {
        var catalogo = new InMemoryProductoCatalogo();
        var selector = new ProcesadorSelector(
            flags, new ProcesadorLegacy(), new ProcesadorNuevo());
        var agg = health ?? new HealthAggregator([new CatalogoHealthCheck(catalogo)]);
        return new OperacionesFunctions(
            agg, selector, flags, NullLogger<OperacionesFunctions>.Instance);
    }
}

// Feature flags controlables en test (sin tocar env vars del proceso).
internal sealed class FakeFeatureFlags : IFeatureFlags
{
    private readonly HashSet<string> _activos;
    public FakeFeatureFlags(params string[] activos) => _activos = [.. activos];
    public bool Activo(string nombre) => _activos.Contains(nombre);
}

// Health check que siempre devuelve lo que le digas.
internal sealed class FixedHealthCheck(string nombre, bool ok) : IHealthCheck
{
    public string Nombre => nombre;
    public bool Comprobar() => ok;
}
