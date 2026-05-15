using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Endpoint de inspección: GET /api/estado devuelve el estado en memoria
// de las 3 piezas que no son HTTP (timer + blob no necesita estado, pero
// el timer sí, y el Cosmos trigger graba en INotificacionLog). Sirve para
// validar desde curl que las 4 funciones están vivas y que el timer y el
// trigger de Cosmos están haciendo su trabajo.
public sealed class EstadoFunction
{
    private readonly IProductoService _productos;
    private readonly ILimpiezaTracker _limpieza;
    private readonly INotificacionLog _notificaciones;

    public EstadoFunction(
        IProductoService productos,
        ILimpiezaTracker limpieza,
        INotificacionLog notificaciones)
    {
        _productos = productos;
        _limpieza = limpieza;
        _notificaciones = notificaciones;
    }

    [Function(nameof(Estado))]
    public IActionResult Estado(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estado")] HttpRequest req)
    {
        return new OkObjectResult(new
        {
            productos = new
            {
                total = _productos.Total,
            },
            timer = new
            {
                totalEjecuciones = _limpieza.TotalEjecuciones,
                ultimas5 = _limpieza.Historial.Take(5),
            },
            cosmosChangeFeed = new
            {
                totalNotificaciones = _notificaciones.Total,
                ultimas5 = _notificaciones.Listar().Take(5),
            },
        });
    }
}
