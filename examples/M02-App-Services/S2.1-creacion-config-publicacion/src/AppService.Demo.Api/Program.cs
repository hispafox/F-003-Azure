using AppService.Demo.Api.Configuration;
using AppService.Demo.Api.Endpoints;
using AppService.Demo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Slide 26 — Logs visibles en App Service > Log Stream y App Service > Diagnose and solve
builder.Logging.AddAzureWebAppDiagnostics();

// Slide 12 — Settings tipadas con validación al arranque
builder.Services
    .AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Slide 27 — CORS leyendo orígenes desde AppOptions:AllowedOrigins
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

// Slide 31 — HttpClient registrado como typed client (singleton del handler)
builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AppService.Demo.Api");
});

// Slide 13 — Health check al que apunta App Service > Health check
builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurableHealthCheck>("app");

var app = builder.Build();

// Slide 21 — HTTPS forzado fuera de Development; complementa el flag --https-only del Portal
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors(CorsPolicy);

app.MapHello();
app.MapInfo();
app.MapHealth();

app.Run();

// Habilita WebApplicationFactory<Program> en los tests
public partial class Program;
