using Tables.Demo.Api.Domain;
using Tables.Demo.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Slide 8 — DI. TableClient es thread-safe → servicio singleton.
builder.Services.AddSingleton<IProductosService, ProductosService>();

var app = builder.Build();

app.MapProductos();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
