using AppService.Demo.Api.Configuration;
using AppService.Demo.Api.Endpoints;
using AppService.Demo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

// Slide 22 — Graceful shutdown: cuando el autoscale hace scale-in o un swap
// reemplaza la instancia, el host espera hasta 30 s a que las requests en
// vuelo terminen antes de matar el proceso. Sin esto, los usuarios ven 502.
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

builder.Services
    .AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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

builder.Services.AddSingleton<DependencyChecks>();

// Slide 5 — generador de carga CPU para escenificar autoscale en clase
builder.Services.AddSingleton<CpuLoadGenerator>();

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
app.MapHealth();      // /health  +  /health/details (JSON, slide 21)
app.MapWarmup();
app.MapVersion();
app.MapLoad();        // /load/cpu — generar carga (slides 5-7)
app.MapStatic();      // /api/products, /api/categorias con Cache-Control (slides 25, 29)

app.Run();

public partial class Program;
