using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class EstadoFunctionTests
{
    [Fact]
    public void Estado_Devuelve_Snapshot_Consolidado_De_Los_4_Triggers()
    {
        var tracker = new InMemoryEstadoTracker();
        tracker.Encolado("ped-1");
        tracker.ProcesadoCola("ped-1");
        tracker.NotificadoPorTopic("ped-1", "sub-notificaciones");
        tracker.ClasificadoArchivo("https://x/y.pdf", "factura");

        var fn = new EstadoFunction(tracker);

        var result = fn.Estado(new DefaultHttpContext().Request) as OkObjectResult;

        Assert.NotNull(result);
        var snapshot = Assert.IsType<EstadoSnapshot>(result!.Value);
        Assert.Equal(1, snapshot.Encolados);
        Assert.Equal(1, snapshot.Procesados);
        Assert.Equal(1, snapshot.Notificaciones);
        Assert.Equal(1, snapshot.Clasificados);
    }
}
