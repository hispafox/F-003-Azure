using Dr.Demo.Api.Dr;
using Dr.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone los advisors puros en un plan de
// DR. Sin estado → singleton (lo cruza el test de contenedor).
builder.Services.AddSingleton<IDrPlanner, DrPlanner>();

var app = builder.Build();

app.MapDr();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
