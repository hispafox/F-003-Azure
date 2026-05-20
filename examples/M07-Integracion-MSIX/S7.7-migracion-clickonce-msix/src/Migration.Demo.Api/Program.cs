using System.Text.Json.Serialization;
using Migration.Demo.Api.Endpoints;
using Migration.Demo.Api.Migration;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (FaseMigracion / NivelRiesgo / ComportamientoApp).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone ClickOnceManifestMapper +
// MigrationCompatibilityCheck + MigrationRoadmap. Sin estado → singleton.
builder.Services.AddSingleton<IMigrationPlanner, MigrationPlanner>();

var app = builder.Build();

app.MapMigration();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
