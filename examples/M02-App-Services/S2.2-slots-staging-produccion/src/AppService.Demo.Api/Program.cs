using AppService.Demo.Api.Configuration;
using AppService.Demo.Api.Endpoints;
using AppService.Demo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Slide 26 (S2.1) — Logs visibles en App Service > Log Stream
builder.Logging.AddAzureWebAppDiagnostics();

// Settings tipadas con validación al arranque
builder.Services
    .AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// CORS leyendo orígenes desde AppOptions:AllowedOrigins
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

builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AppService.Demo.Api");
});

// Slides 16 y 29 — Verificaciones del endpoint /warmup
builder.Services.AddSingleton<DependencyChecks>();

builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurableHealthCheck>("app");

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
app.MapWarmup();   // Slide 16 — /warmup para swap pre-flight
app.MapVersion();  // Slide 9 — /version para verificar swap visualmente

app.Run();

public partial class Program;
