using Desktop.Demo.Api.Desktop;
using Desktop.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone flow advisor + redirect URI +
// ciclo de token en un plan. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IDesktopAuthPlanner, DesktopAuthPlanner>();

var app = builder.Build();

app.MapDesktop();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
