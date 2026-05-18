using Microsoft.EntityFrameworkCore;
using Sql.Demo.Api.Domain;

namespace Sql.Demo.Api.Data;

// Slide 7 — el DbContext: el "mapa" entre las clases C# y las tablas SQL.
// No fija el provider (UseSqlServer / UseSqlite) a propósito: eso se
// decide fuera (Program.cs → SQL Server; tests → SQLite in-memory). Así
// el mismo modelo se ejercita en los tres escenarios.
public sealed class VentasDbContext(DbContextOptions<VentasDbContext> options)
    : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Slide 7 — configuración explícita del esquema: longitudes,
        // tipo decimal, índice de búsqueda y la relación 1—N.
        modelBuilder.Entity<Producto>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
            e.Property(p => p.Precio).HasColumnType("decimal(18,2)");
            e.HasIndex(p => p.Nombre);
        });

        modelBuilder.Entity<Pedido>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Total).HasColumnType("decimal(18,2)");
            e.HasIndex(p => p.Fecha);
            e.HasOne(p => p.Producto)
                .WithMany(pr => pr.Pedidos)
                .HasForeignKey(p => p.ProductoId)
                .OnDelete(DeleteBehavior.Restrict); // no borrar producto con pedidos
        });
    }
}
