using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class EstadoFunctionTests
{
    [Fact]
    public void Estado_Devuelve_Snapshot_De_Los_3_Servicios()
    {
        var productos = new InMemoryProductoService(); // seed 3
        var tracker = new InMemoryLimpiezaTracker();
        var log = new InMemoryNotificacionLog();

        // simular 2 timer ticks + 1 cambio del Change Feed
        tracker.Registrar(50, false);
        tracker.Registrar(75, false);
        log.Anotar(new Pedido { Id = "p1", ClienteId = "c1", Estado = "nuevo", Total = 10m });

        var fn = TestHost.NewEstado(productos, tracker, log);

        var result = fn.Estado(new DefaultHttpContext().Request) as OkObjectResult;

        Assert.NotNull(result);
        // El body es anónimo; lo serializamos a JSON para verificar la
        // estructura sin pelearnos con reflection sobre propiedades anidadas.
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        Assert.Contains("\"total\":3", json);              // 3 productos seed
        Assert.Contains("\"totalEjecuciones\":2", json);   // 2 timer ticks
        Assert.Contains("\"totalNotificaciones\":1", json); // 1 cambio Cosmos
    }
}
