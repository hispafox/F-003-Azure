using AppService.Demo.Api.Configuration;
using AppService.Demo.Api.Endpoints;
using AppService.Demo.Api.Services;
using AppService.Demo.Api.Telemetry;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<AppOptions>, AppOptionsValidator>();

const string CorsPolicy = "DefaultPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection(AppOptions.SectionName)
            .Get<AppOptions>()?.AllowedOrigins ?? [];

        if (origins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddHttpClient<ExternalApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AppOptions>>().Value;
    client.BaseAddress = new Uri(options.ExternalApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
    client.DefaultRequestHeaders.Add("User-Agent", "AppService.Demo.Api");
});

builder.Services.AddSingleton<DependencyChecks>();
builder.Services.AddSingleton<CpuLoadGenerator>();
builder.Services.AddSingleton<AppMeter>();

builder.Services.AddFeatureManagement();

builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurableHealthCheck>("app");

// Slides 11, 20 — OpenTelemetry + exporter de Azure Monitor.
// Solo se activa si hay APPLICATIONINSIGHTS_CONNECTION_STRING; en local
// el código no requiere AI para arrancar.
var aiConnectionString =
    builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(o => o.ConnectionString = aiConnectionString)
        .WithTracing(tracing => tracing.AddSource(AppMeter.MeterName))
        .WithMetrics(metrics => metrics
            .AddMeter(AppMeter.MeterName)
            .AddRuntimeInstrumentation());
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors(CorsPolicy);

app.MapHello();
app.MapInfo();
app.MapHealth();
app.MapWarmup();
app.MapVersion();
app.MapLoad();
app.MapStatic();
app.MapConfig();
app.MapFeatureFlags();
app.MapSecrets();
app.MapOrders();      // POST /demo/orders (slides 21, 22, 23)
app.MapDemoErrors();  // GET  /demo/error?type=... (slides 4, 12, 17)
app.MapLogDemo();     // POST /demo/log (slides 23, 25)

app.Run();

public partial class Program;
