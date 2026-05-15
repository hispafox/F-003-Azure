namespace AzureFunctions.Demo.Models;

// Slide 5 — Producto del CRUD en memoria. Record con value-equality e
// inmutabilidad por defecto. Las mutaciones se hacen con "with".
public sealed record Producto(string Id, string Nombre, decimal Precio, int Stock);

// DTO de entrada para POST/PUT. No incluye Id — lo genera el repositorio
// en el POST; en el PUT el id viene de la route.
public sealed record CrearProductoDto(string Nombre, decimal Precio, int Stock);
