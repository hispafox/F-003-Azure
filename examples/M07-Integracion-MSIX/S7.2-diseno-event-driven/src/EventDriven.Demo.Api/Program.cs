using System.Text.Json.Serialization;
using EventDriven.Demo.Api.Endpoints;
using EventDriven.Demo.Api.EventDriven;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre (PatronEvento / EstiloSaga) — legible en la API.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone EventDesignAdvisor +
// EventValidator en el plan de diseño. Sin estado → singleton
// (lo cruza el test DI). El EventStore es por petición (stateful).
builder.Services.AddSingleton<IEventDrivenPlanner, EventDrivenPlanner>();

var app = builder.Build();

app.MapEventDriven();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
