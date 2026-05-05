using System.ComponentModel.DataAnnotations;

namespace AzureFunctions.Demo.Models;

public sealed class Producto
{
    public required string Id { get; init; }
    public required string Nombre { get; set; }
    public required string Categoria { get; set; }
    public required decimal Precio { get; set; }
    public int Stock { get; set; }
    public DateTimeOffset CreadoEn { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActualizadoEn { get; set; } = DateTimeOffset.UtcNow;
}

// Slide 8 + 15 — DTO con validación por DataAnnotations.
public sealed record CrearProductoDto
{
    [Required(ErrorMessage = "Nombre es obligatorio")]
    [StringLength(100, MinimumLength = 3)]
    public string Nombre { get; init; } = "";

    [Required(ErrorMessage = "Categoria es obligatoria")]
    public string Categoria { get; init; } = "";

    [Range(0.01, 999_999.99, ErrorMessage = "Precio debe estar entre 0.01 y 999999.99")]
    public decimal Precio { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock no puede ser negativo")]
    public int Stock { get; init; }
}

public sealed record ActualizarProductoDto
{
    [StringLength(100, MinimumLength = 3)]
    public string? Nombre { get; init; }

    public string? Categoria { get; init; }

    [Range(0.01, 999_999.99)]
    public decimal? Precio { get; init; }

    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }
}

// Slide 7 — Filtros que se reciben por query string en el endpoint de búsqueda.
public sealed record BuscarProductosQuery(
    string? Nombre,
    string? Categoria,
    decimal? MinPrecio,
    decimal? MaxPrecio,
    int Pagina,
    int PorPagina);
