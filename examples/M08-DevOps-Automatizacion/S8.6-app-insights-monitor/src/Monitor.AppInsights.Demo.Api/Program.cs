using System.Text.Json.Serialization;
using Monitor.AppInsights.Demo.Api.Endpoints;
using Monitor.AppInsights.Demo.Api.Monitor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IAppInsightsPlanner, AppInsightsPlanner>();

var app = builder.Build();

app.MapMonitor();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
