using Security.Demo.Api.Endpoints;
using Security.Demo.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: calcula el Secure Score del checklist
// (slide 10/17). Sin estado → singleton (lo cruza el test de contenedor).
builder.Services.AddSingleton<ISecureScore, SecureScoreCalculator>();

var app = builder.Build();

app.MapSeguridad();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
