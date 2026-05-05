using System.ComponentModel.DataAnnotations;

namespace AzureFunctions.Demo.Configuration;

public sealed class ProductosOptions
{
    public const string SectionName = "Productos";

    [Range(1, 200)]
    public int MaxPorPagina { get; init; } = 50;

    [Range(1, 100)]
    public int PorPaginaPorDefecto { get; init; } = 20;
}
