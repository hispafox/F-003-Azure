using Entra.Demo.Api.Endpoints;
using Entra.Demo.Api.Entra;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: autorización por App Roles (slide 19).
// Sin estado → singleton (lo cruza el test de contenedor).
builder.Services.AddSingleton<IAppRolesAuthorizer, AppRolesAuthorizer>();

var app = builder.Build();

app.MapEntra();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
