namespace AzureFunctions.Demo.Models;

// Slide 6 — Producto del HTTP trigger. Record con value-equality.
public sealed record Producto(string Id, string Nombre, decimal Precio);

public sealed record CrearProductoDto(string Nombre, decimal Precio);
