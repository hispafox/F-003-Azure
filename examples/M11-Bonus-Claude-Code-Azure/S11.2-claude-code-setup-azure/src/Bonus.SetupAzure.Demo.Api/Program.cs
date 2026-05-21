using System.Text.Json.Serialization;
using Bonus.SetupAzure.Demo.Api.Endpoints;
using Bonus.SetupAzure.Demo.Api.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<ISetupAzurePlanner, SetupAzurePlanner>();

var app = builder.Build();

app.MapSetup();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
