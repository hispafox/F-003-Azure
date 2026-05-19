using Datos.Demo.Api.Datos;
using Datos.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: evalúa el checklist de seguridad de datos
// (slide 14). Sin estado → singleton (lo cruza el test de contenedor).
builder.Services.AddSingleton<IDataProtectionAssessor, DataProtectionAssessor>();

var app = builder.Build();

app.MapDatos();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
