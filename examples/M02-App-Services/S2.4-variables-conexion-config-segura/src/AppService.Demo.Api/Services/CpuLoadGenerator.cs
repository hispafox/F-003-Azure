namespace AppService.Demo.Api.Services;

// Slides 5 y 6 — Genera carga CPU REAL (no Thread.Sleep) buscando primos.
// Solo así sube la métrica "CpuPercentage" del plan y el autoscale dispara.
public sealed class CpuLoadGenerator
{
    public int BurnCpu(TimeSpan duration, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + duration;
        var found = 0;
        var n = 2;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (IsPrime(n)) found++;
            n++;
        }

        return found;
    }

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n < 4) return true;
        if (n % 2 == 0) return false;
        for (var i = 3; (long)i * i <= n; i += 2)
        {
            if (n % i == 0) return false;
        }
        return true;
    }
}
