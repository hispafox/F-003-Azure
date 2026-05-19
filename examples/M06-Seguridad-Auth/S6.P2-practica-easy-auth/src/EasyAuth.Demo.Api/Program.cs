using EasyAuth.Demo.Api.Endpoints;
using EasyAuth.Demo.Api.EasyAuth;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone config advisor + endpoints en el
// plan de Easy Auth. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IEasyAuthSetupPlanner, EasyAuthSetupPlanner>();

var app = builder.Build();

app.MapEasyAuth();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
