using AppService.Demo.Api.Configuration;
using AppService.Demo.Api.Endpoints;
using AppService.Demo.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Slides 5, 18, 22 — Options pattern + DataAnnotations + validador custom.
// ValidateOnStart hace que la app FALLE al arrancar si la config está mal.
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

// Slides 11 y 16 — Feature flags via Microsoft.FeatureManagement
builder.Services.AddFeatureManagement();

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
app.MapWarmup();
app.MapVersion();
app.MapLoad();
app.MapStatic();
app.MapConfig();        // /config + /connection (slides 7, 28)
app.MapFeatureFlags();  // /features/new-ui (slides 11, 16)
app.MapSecrets();       // /secrets/api-key/check (slides 9, 25, 27)

app.Run();

public partial class Program;
