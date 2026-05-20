using System.Text.Json.Serialization;
using Iac.Bicep.Demo.Api.Endpoints;
using Iac.Bicep.Demo.Api.Iac;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IIacPlanner, IacPlanner>();

var app = builder.Build();

app.MapIac();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
