using Practica.Demo.Api.Endpoints;
using Practica.Demo.Api.Practica;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone Easy Auth + KV References en el
// plan de la práctica. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IPracticaPlanner, PracticaPlanner>();

var app = builder.Build();

app.MapPractica();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
