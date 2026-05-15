namespace AzureFunctions.Demo.Services;

// Slide 10/17 — health check para verificación post-deploy. Agrega N
// comprobaciones; si cualquiera falla, el endpoint devuelve 503 y el
// pipeline aborta el swap / hace rollback.
public interface IHealthCheck
{
    string Nombre { get; }
    bool Comprobar();
}

public sealed record HealthResultado(
    string Estado,                 // "Healthy" | "Unhealthy"
    IReadOnlyDictionary<string, string> Checks);

public interface IHealthAggregator
{
    HealthResultado Evaluar();
}

public sealed class HealthAggregator(IEnumerable<IHealthCheck> checks) : IHealthAggregator
{
    public HealthResultado Evaluar()
    {
        var resultados = new Dictionary<string, string>();
        var sano = true;

        foreach (var c in checks)
        {
            bool ok;
            try { ok = c.Comprobar(); }
            catch { ok = false; } // un check que lanza = unhealthy, no 500

            resultados[c.Nombre] = ok ? "ok" : "fail";
            if (!ok) sano = false;
        }

        return new HealthResultado(sano ? "Healthy" : "Unhealthy", resultados);
    }
}

// Check de ejemplo: el catálogo responde y tiene datos. En real serían
// "Cosmos accesible", "Service Bus accesible", etc.
public sealed class CatalogoHealthCheck(IProductoCatalogo catalogo) : IHealthCheck
{
    public string Nombre => "catalogo";
    public bool Comprobar() => catalogo.Listar().Count > 0;
}
