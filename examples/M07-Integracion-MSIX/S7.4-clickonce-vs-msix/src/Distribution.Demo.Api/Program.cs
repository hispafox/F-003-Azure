using System.Text.Json.Serialization;
using Distribution.Demo.Api.Distribution;
using Distribution.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (FormatoDistribucion / CaracteristicaDistribucion /
// EscenarioMigracion / TipoCertificado / EscenarioFirma).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone DistributionFormatComparator +
// MigrationDecisionAdvisor + SigningCertAdvisor en el plan. Sin estado
// → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IDistributionPlanner, DistributionPlanner>();

var app = builder.Build();

app.MapDistribution();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
