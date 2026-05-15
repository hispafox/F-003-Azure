using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 7 — fuente única de verdad. Las dos versiones de la API
// proyectan ESTE dominio a su contrato respectivo. Si la lógica de
// negocio fuera distinta por versión, sería un rediseño, no versionado.
public interface IProductoCatalogo
{
    IReadOnlyList<Producto> Listar();
    Producto? GetById(string id);
}

public sealed class InMemoryProductoCatalogo : IProductoCatalogo
{
    private static readonly IReadOnlyList<Producto> Seed =
    [
        new("p001", "Laptop Dell", 1299.00m, "EUR", 5),
        new("p002", "Monitor 27\"", 349.00m, "EUR", 12),
        new("p003", "Teclado mecánico", 89.90m, "EUR", 30),
    ];

    public IReadOnlyList<Producto> Listar() => Seed;

    public Producto? GetById(string id) => Seed.FirstOrDefault(p => p.Id == id);
}

// Proyecciones a cada contrato. Centralizadas para que el mapeo viva en
// un sitio testeable, no disperso por las funciones.
public static class ProductoMappers
{
    public static ProductoV1 ToV1(this Producto p) => new(p.Id, p.Nombre, p.Precio);

    public static ProductoV2 ToV2(this Producto p) =>
        new(p.Id, p.Nombre, p.Precio, p.Moneda, p.Stock);
}
