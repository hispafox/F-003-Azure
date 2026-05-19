using System.Text.Json.Serialization;
using Messaging.Demo.Api.Endpoints;
using Messaging.Demo.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Enums por nombre en la API (TipoMensaje/ServicioMensajeria) — más
// legible que enteros y permite el binding desde el body del plan.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone SqlFilterEvaluator +
// MessageDeduplicator + MessagingServiceAdvisor en el plan de
// mensajería. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IMessagingPlanner, MessagingPlanner>();

var app = builder.Build();

app.MapMessaging();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
