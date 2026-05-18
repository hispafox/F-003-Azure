using Microsoft.EntityFrameworkCore;
using Sql.Demo.Api.Data;
using Sql.Demo.Api.Endpoints;
using Sql.Demo.Api.Repositories;
using Sql.Demo.Api.Sql;

var builder = WebApplication.CreateBuilder(args);

// Slide 6 — la connection string viene de configuración (App Settings /
// Key Vault). En producción: `Authentication=Active Directory Default`
// (Managed Identity, sin password). En local/tests: SQL auth o el cs del
// contenedor de Testcontainers.
var rawCs = builder.Configuration["SqlConnection"];

// Sin cadena configurada → placeholder LocalDB. NO conecta a nada: solo
// permite que EF conozca el provider para generar migraciones
// (`dotnet ef`) y que la app/los tests arranquen. El alumno pone la suya.
var connectionString = string.IsNullOrWhiteSpace(rawCs)
    ? "Server=(localdb)\\MSSQLLocalDB;Database=VentasDemo;Trusted_Connection=True;"
    : SqlConnectionTuning.Afinar(rawCs); // slides 6, 10 (pool + Encrypt)

builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        // Slide 13 — resiliencia: reintentar errores transitorios de
        // Azure SQL (DB serverless despertando, throttling, failover).
        sql.EnableRetryOnFailure(
            maxRetryCount: AzureSqlRetryPolicy.MaxReintentos,
            maxRetryDelay: AzureSqlRetryPolicy.MaxRetraso,
            errorNumbersToAdd: AzureSqlRetryPolicy.ErroresTransitorios);
        sql.CommandTimeout(60);
    }));

// DbContext es Scoped (vida = request) → los repos también Scoped.
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();

var app = builder.Build();

app.MapVentas();

// Slide 35 (anti-pattern 8): NO migramos en el arranque de la app
// (race conditions / deploy lock). Las migraciones se aplican en el
// pipeline o las aplica el test de integración explícitamente.

app.Run();

// Para WebApplicationFactory<Program> en los tests de integración.
public partial class Program { }
