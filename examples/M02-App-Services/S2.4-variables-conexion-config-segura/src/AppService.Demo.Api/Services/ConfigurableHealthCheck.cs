using AppService.Demo.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AppService.Demo.Api.Services;

// Slide 32 — Reproduce un fallo de salud cambiando AppOptions:Healthy=false desde
// App Settings sin redeploy. Permite verificar el "auto-restart" de App Service.
public sealed class ConfigurableHealthCheck(IOptionsMonitor<AppOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(options.CurrentValue.Healthy
            ? HealthCheckResult.Healthy("App is healthy.")
            : HealthCheckResult.Unhealthy("AppOptions:Healthy=false"));
    }
}
