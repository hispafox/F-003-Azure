using System.Text.Json.Serialization;
using Deploy.Demo.Api.Deploy;
using Deploy.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IDeploymentPlanner, DeploymentPlanner>();

var app = builder.Build();

app.MapDeploy();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
