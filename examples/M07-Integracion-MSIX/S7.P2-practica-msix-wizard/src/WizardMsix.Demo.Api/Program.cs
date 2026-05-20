using System.Text.Json.Serialization;
using WizardMsix.Demo.Api.Endpoints;
using WizardMsix.Demo.Api.Wizard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPracticaMsixWizardPlanner, PracticaMsixWizardPlanner>();

var app = builder.Build();

app.MapWizard();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
