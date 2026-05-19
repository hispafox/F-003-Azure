using KeyVault.Demo.Api.Endpoints;
using KeyVault.Demo.Api.KeyVault;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone el advisor + la referencia en un
// plan de almacenamiento. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<IKeyVaultPlanner, KeyVaultPlanner>();

var app = builder.Build();

app.MapKeyVault();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
