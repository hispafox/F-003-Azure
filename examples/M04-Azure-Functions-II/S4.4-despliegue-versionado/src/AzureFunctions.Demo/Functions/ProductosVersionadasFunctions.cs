using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 7 — versionado de APIs HTTP por ruta. v1 y v2 conviven: v1 sigue
// sirviendo a clientes antiguos mientras v2 ofrece el contrato nuevo.
// Estrategia de deprecación: publicar v2 → avisar → monitorizar uso de
// v1 → retirar v1 cuando no tenga tráfico.
public sealed class ProductosVersionadasFunctions
{
    private readonly IProductoCatalogo _catalogo;

    public ProductosVersionadasFunctions(IProductoCatalogo catalogo)
    {
        _catalogo = catalogo;
    }

    // GET /api/v1/productos — contrato original {id, nombre, precio}
    [Function(nameof(ListarV1))]
    public IActionResult ListarV1(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/productos")]
        HttpRequest req)
    {
        var items = _catalogo.Listar().Select(p => p.ToV1()).ToList();
        return new OkObjectResult(new { version = "v1", total = items.Count, items });
    }

    // GET /api/v2/productos — contrato nuevo {+moneda, +stock}
    [Function(nameof(ListarV2))]
    public IActionResult ListarV2(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/productos")]
        HttpRequest req)
    {
        var items = _catalogo.Listar().Select(p => p.ToV2()).ToList();
        return new OkObjectResult(new { version = "v2", total = items.Count, items });
    }

    [Function(nameof(GetV1))]
    public IActionResult GetV1(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/productos/{id}")]
        HttpRequest req, string id)
    {
        var p = _catalogo.GetById(id);
        return p is null
            ? new NotFoundObjectResult(new { error = $"'{id}' no encontrado" })
            : new OkObjectResult(p.ToV1());
    }

    [Function(nameof(GetV2))]
    public IActionResult GetV2(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/productos/{id}")]
        HttpRequest req, string id)
    {
        var p = _catalogo.GetById(id);
        return p is null
            ? new NotFoundObjectResult(new { error = $"'{id}' no encontrado" })
            : new OkObjectResult(p.ToV2());
    }
}
