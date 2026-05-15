using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 7 — Las 5 funciones del CRUD en una misma clase. Patrón Functions
// isolated 2.0 con modelo ASP.NET Core (HttpRequest + IActionResult).
public sealed class ProductosApi
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IProductoService _productos;
    private readonly ILogger<ProductosApi> _logger;

    public ProductosApi(IProductoService productos, ILogger<ProductosApi> logger)
    {
        _productos = productos;
        _logger = logger;
    }

    // GET /api/productos
    [Function(nameof(ListarProductos))]
    public IActionResult ListarProductos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "productos")]
        HttpRequest req)
    {
        _logger.LogInformation("Listando productos");
        var lista = _productos.Listar();
        return new OkObjectResult(new { total = lista.Count, productos = lista });
    }

    // GET /api/productos/{id}
    [Function(nameof(ObtenerProducto))]
    public IActionResult ObtenerProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "productos/{id}")]
        HttpRequest req,
        string id)
    {
        var producto = _productos.GetById(id);
        return producto is null
            ? new NotFoundObjectResult(new { error = $"Producto '{id}' no encontrado" })
            : new OkObjectResult(producto);
    }

    // POST /api/productos
    [Function(nameof(CrearProducto))]
    public async Task<IActionResult> CrearProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "productos")]
        HttpRequest req)
    {
        var dto = await DeserializarAsync(req);
        var error = Validar(dto);
        if (error is not null) return new BadRequestObjectResult(new { error });

        var producto = _productos.Crear(dto!);
        _logger.LogInformation("Producto creado: {Id} - {Nombre}", producto.Id, producto.Nombre);
        return new CreatedResult($"/api/productos/{producto.Id}", producto);
    }

    // PUT /api/productos/{id}
    [Function(nameof(ActualizarProducto))]
    public async Task<IActionResult> ActualizarProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "productos/{id}")]
        HttpRequest req,
        string id)
    {
        var dto = await DeserializarAsync(req);
        var error = Validar(dto);
        if (error is not null) return new BadRequestObjectResult(new { error });

        var actualizado = _productos.Actualizar(id, dto!);
        return actualizado is null
            ? new NotFoundObjectResult(new { error = $"Producto '{id}' no encontrado" })
            : new OkObjectResult(actualizado);
    }

    // DELETE /api/productos/{id}
    [Function(nameof(BorrarProducto))]
    public IActionResult BorrarProducto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "productos/{id}")]
        HttpRequest req,
        string id)
    {
        if (!_productos.Borrar(id))
        {
            return new NotFoundObjectResult(new { error = $"Producto '{id}' no encontrado" });
        }

        _logger.LogInformation("Producto borrado: {Id}", id);
        return new NoContentResult();
    }

    // Capturamos JsonException en vez de dejar que crash internamente.
    // Así el cliente recibe un 400 con mensaje útil en vez de un 500.
    private async Task<CrearProductoDto?> DeserializarAsync(HttpRequest req)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<CrearProductoDto>(req.Body, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Body JSON inválido");
            return null;
        }
    }

    private static string? Validar(CrearProductoDto? dto) => dto switch
    {
        null => "Body JSON inválido o vacío",
        { Nombre: var n } when string.IsNullOrWhiteSpace(n) => "Nombre obligatorio",
        { Precio: <= 0 } => "Precio debe ser mayor que 0",
        { Stock: < 0 } => "Stock no puede ser negativo",
        _ => null,
    };
}
