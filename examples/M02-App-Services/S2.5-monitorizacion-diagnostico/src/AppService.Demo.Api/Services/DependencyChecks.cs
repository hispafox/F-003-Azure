namespace AppService.Demo.Api.Services;

public sealed record CheckResult(string Name, bool Ok, string? Detail = null);

public sealed record WarmupResult(bool AllOk, IReadOnlyList<CheckResult> Checks);

// Slides 16 y 29 — Este servicio simula las verificaciones que harías antes de
// que App Service redirija tráfico al slot durante un swap. En producción aquí
// llamarías a Cosmos, Service Bus, Redis, etc.
public sealed class DependencyChecks
{
    public Task<WarmupResult> RunAsync(CancellationToken ct = default)
    {
        var workingSetMb = Environment.WorkingSet / 1_000_000;

        var checks = new List<CheckResult>
        {
            new("configuration", true, "AppOptions loaded"),
            new("external-api-client", true, "HttpClient ready"),
            new("memory", true, $"{workingSetMb} MB working set"),
        };

        return Task.FromResult(new WarmupResult(checks.All(c => c.Ok), checks));
    }
}
