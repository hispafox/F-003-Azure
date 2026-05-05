using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Endpoints de inspección — el Change Feed actualiza memoria a través
// de las dos funciones trigger; estos GET devuelven ese estado para
// poder verificarlo desde curl tras hacer un upsert en Cosmos.
//
// En producción harías queries directas contra el contenedor
// "resumenes-clientes" en lugar de mantener un espejo en memoria.
public sealed class InspeccionHttpFunctions
{
    private readonly INotificacionService _notificaciones;
    private readonly IResumenClienteService _resumenes;

    public InspeccionHttpFunctions(
        INotificacionService notificaciones,
        IResumenClienteService resumenes)
    {
        _notificaciones = notificaciones;
        _resumenes = resumenes;
    }

    [Function(nameof(ListarNotificaciones))]
    public IActionResult ListarNotificaciones(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notificaciones")] HttpRequest req)
    {
        var clienteId = req.Query["clienteId"].ToString();

        var lista = string.IsNullOrWhiteSpace(clienteId)
            ? _notificaciones.ListarTodas()
            : _notificaciones.ListarPorCliente(clienteId);

        return new OkObjectResult(new
        {
            total = lista.Count,
            items = lista,
        });
    }

    [Function(nameof(ListarResumenes))]
    public IActionResult ListarResumenes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumenes")] HttpRequest req)
    {
        var lista = _resumenes.ListarTodos();
        return new OkObjectResult(new
        {
            total = lista.Count,
            items = lista,
        });
    }

    [Function(nameof(ObtenerResumen))]
    public IActionResult ObtenerResumen(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumenes/{clienteId}")] HttpRequest req,
        string clienteId)
    {
        var resumen = _resumenes.Get(clienteId);
        return resumen is null
            ? new NotFoundObjectResult(new { error = $"No hay resumen materializado para {clienteId}" })
            : new OkObjectResult(resumen);
    }
}
