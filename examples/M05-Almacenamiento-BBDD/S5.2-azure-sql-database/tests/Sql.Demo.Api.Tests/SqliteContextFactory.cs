using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sql.Demo.Api.Data;

namespace Sql.Demo.Api.Tests;

// Crea un VentasDbContext sobre SQLite **in-memory**. La conexión se
// mantiene abierta mientras viva el factory: si se cierra, la BD
// desaparece. EnsureCreated() materializa el modelo (no las migraciones
// — esas son SQL Server-specific; se ejercitan en la CAPA 3).
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<VentasDbContext> _options;

    public SqliteContextFactory()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<VentasDbContext>()
            .UseSqlite(_conn)
            .Options;

        using var db = new VentasDbContext(_options);
        db.Database.EnsureCreated();
    }

    // Un contexto nuevo por operación = simula el ciclo Scoped real
    // (no se comparte el change-tracker entre "requests").
    public VentasDbContext NewContext() => new(_options);

    public void Dispose() => _conn.Dispose();
}
