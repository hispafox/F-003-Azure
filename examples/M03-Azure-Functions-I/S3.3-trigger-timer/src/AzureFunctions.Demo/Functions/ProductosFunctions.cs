using System.ComponentModel.DataAnnotations;
using AzureFunctions.Demo.Configuration;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureFunctions.Demo.Functions;

// Slide 5 — CRUD completo sobre /api/productos con AuthorizationLevel.Function
// (slide 10) — requiere function key en Azure pero es anónimo en local.
// Slide 13 — DI por constructor: servicio + options + logger.
public sealed class ProductosFunctions(
    IProductoService service,
    IOptions<ProductosOptions> options,
    ILogger<ProductosFunctions> logger)
{
    private readonly ProductosOptions _options = options.Value;

    [Function(nameof(Listar))]
    public IActionResult Listar(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "productos")] HttpRequest req)
    {
        // Slide 7 — query parameters
        var query = new BuscarProductosQuery(
            Nombre: req.Query["nombre"].ToString().NullIfEmpty(),
            Categoria: req.Query["categoria"].ToString().NullIfEmpty(),
            MinPrecio: TryParseDecimal(req.Query["minPrecio"]),
            MaxPrecio: TryParseDecimal(req.Query["maxPrecio"]),
            Pagina: TryParseInt(req.Query["pagina"]) ?? 1,
            PorPagina: Math.Clamp(
                TryParseInt(req.Query["porPagina"]) ?? _options.PorPaginaPorDefecto,
                1,
                _options.MaxPorPagina));

        var resultados = service.Buscar(query, out var total);
        logger.LogInformation("Listar productos page={Pagina}/{TotalPaginas} total={Total}",
            query.Pagina, Math.Max(1, (total + query.PorPagina - 1) / query.PorPagina), total);

        // Slide 9 — headers custom para metadatos de paginación
        req.HttpContext.Response.Headers["X-Total-Count"] = total.ToString();

        return new OkObjectResult(new
        {
            total,
            pagina = query.Pagina,
            porPagina = query.PorPagina,
            items = resultados
        });
    }

    [Function(nameof(GetPorId))]
    public IActionResult GetPorId(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "productos/{id}")] HttpRequest req,
        string id)
    {
        var producto = service.GetById(id);
        return producto is null
            ? NotFoundProblem(req, $"Producto '{id}' no encontrado")
            : new OkObjectResult(producto);
    }

    [Function(nameof(Crear))]
    public async Task<IActionResult> Crear(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "productos")] HttpRequest req)
    {
        CrearProductoDto? dto;
        try { dto = await req.ReadFromJsonAsync<CrearProductoDto>(); }
        catch (System.Text.Json.JsonException) { dto = null; }

        if (dto is null)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Type = "https://aka.ms/functions/errors/bad-json",
                Title = "Invalid JSON body",
                Status = StatusCodes.Status400BadRequest,
                Detail = "El body de la peticion debe ser JSON valido"
            });
        }

        // Slide 15 — validación con DataAnnotations
        if (!TryValidate(dto, out var errors))
        {
            return ValidationProblem(req, errors);
        }

        var creado = service.Crear(dto);
        logger.LogInformation("Producto {ProductoId} creado", creado.Id);

        return new CreatedResult($"/api/productos/{creado.Id}", creado);
    }

    [Function(nameof(Actualizar))]
    public async Task<IActionResult> Actualizar(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "productos/{id}")] HttpRequest req,
        string id)
    {
        ActualizarProductoDto? dto;
        try { dto = await req.ReadFromJsonAsync<ActualizarProductoDto>(); }
        catch (System.Text.Json.JsonException) { dto = null; }

        if (dto is null) return new BadRequestObjectResult(new ProblemDetails
        {
            Title = "Invalid JSON body",
            Status = StatusCodes.Status400BadRequest
        });

        if (!TryValidate(dto, out var errors))
        {
            return ValidationProblem(req, errors);
        }

        var actualizado = service.Actualizar(id, dto);
        return actualizado is null
            ? NotFoundProblem(req, $"Producto '{id}' no encontrado")
            : new OkObjectResult(actualizado);
    }

    [Function(nameof(Eliminar))]
    public IActionResult Eliminar(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "productos/{id}")] HttpRequest req,
        string id)
    {
        return service.Eliminar(id)
            ? new NoContentResult()
            : NotFoundProblem(req, $"Producto '{id}' no encontrado");
    }

    // ── Helpers ──

    private static IActionResult NotFoundProblem(HttpRequest req, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = "https://aka.ms/functions/errors/not-found",
            Title = "Resource Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = req.Path
        };
        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static IActionResult ValidationProblem(HttpRequest req, IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Type = "https://aka.ms/functions/errors/validation",
            Title = "One or more validation errors occurred",
            Status = StatusCodes.Status422UnprocessableEntity,
            Instance = req.Path
        };
        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static bool TryValidate(object dto, out IDictionary<string, string[]> errors)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

        errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(""), (r, member) => (member, r.ErrorMessage ?? ""))
            .GroupBy(t => t.member, t => t.Item2)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return ok;
    }

    private static decimal? TryParseDecimal(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static int? TryParseInt(string? s) =>
        int.TryParse(s, out var v) ? v : null;
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
