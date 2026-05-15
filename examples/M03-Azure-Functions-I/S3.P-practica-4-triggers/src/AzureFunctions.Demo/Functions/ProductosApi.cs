using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 1/4 — HTTP. CRUD ligero sobre /api/productos.
// Patrón híbrido (slide 23 de S3.6): trigger declarativo + servicio
// inyectado por DI para la lógica.
public sealed class ProductosApi
{
    private readonly IProductoService _productos;
    private readonly ILogger<ProductosApi> _logger;

    public ProductosApi(IProductoService productos, ILogger<ProductosApi> logger)
    {
        _productos = productos;
        _logger = logger;
    }

    [Function(nameof(ListarProductos))]
    public IActionResult ListarProductos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "productos")]
        HttpRequest req)
    {
        var lista = _productos.Listar();
        return new OkObjectResult(new { total = lista.Count, items = lista });
    }

    [Function(nameof(GetProducto))]
    public IActionResult GetProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "productos/{id}")]
        HttpRequest req,
        string id)
    {
        var producto = _productos.GetById(id);
        return producto is null
            ? new NotFoundObjectResult(new { error = $"Producto '{id}' no encontrado" })
            : new OkObjectResult(producto);
    }

    [Function(nameof(CrearProducto))]
    public async Task<IActionResult> CrearProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "productos")]
        HttpRequest req)
    {
        CrearProductoDto? dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<CrearProductoDto>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido en POST /productos");
            return new BadRequestObjectResult(new { error = "Body JSON inválido" });
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Nombre) || dto.Precio <= 0)
        {
            return new BadRequestObjectResult(new { error = "Nombre y precio > 0 son obligatorios" });
        }

        var producto = _productos.Crear(dto);
        _logger.LogInformation("Producto creado: {Id} ({Nombre})", producto.Id, producto.Nombre);
        return new CreatedResult($"/api/productos/{producto.Id}", producto);
    }
}
