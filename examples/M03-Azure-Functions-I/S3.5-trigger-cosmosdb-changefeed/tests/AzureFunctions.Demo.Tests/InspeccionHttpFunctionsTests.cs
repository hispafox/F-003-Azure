using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class InspeccionHttpFunctionsTests
{
    private static (InspeccionHttpFunctions http, INotificacionService notif, IResumenClienteService res) Build()
    {
        var notificaciones = new InMemoryNotificacionService();
        var resumenes = new InMemoryResumenClienteService();
        var http = TestHost.NewInspeccion(notificaciones, resumenes);
        return (http, notificaciones, resumenes);
    }

    [Fact]
    public void ListarNotificaciones_Sin_Filtro_Devuelve_Todas()
    {
        var (http, notif, _) = Build();
        notif.EnviarSiNoEnviada("ped-1", "cliente-A", "confirmado", "msg-1");
        notif.EnviarSiNoEnviada("ped-2", "cliente-B", "enviado", "msg-2");

        var result = http.ListarNotificaciones(HttpRequestFactory.Empty()) as OkObjectResult;

        Assert.NotNull(result);
        var body = result!.Value!;
        var total = (int)body.GetType().GetProperty("total")!.GetValue(body)!;
        Assert.Equal(2, total);
    }

    [Fact]
    public void ListarNotificaciones_Con_ClienteId_Filtra()
    {
        var (http, notif, _) = Build();
        notif.EnviarSiNoEnviada("ped-1", "cliente-A", "confirmado", "msg-1");
        notif.EnviarSiNoEnviada("ped-2", "cliente-B", "enviado", "msg-2");
        notif.EnviarSiNoEnviada("ped-3", "cliente-A", "entregado", "msg-3");

        var result = http.ListarNotificaciones(HttpRequestFactory.WithQuery("clienteId=cliente-A")) as OkObjectResult;

        Assert.NotNull(result);
        var body = result!.Value!;
        var total = (int)body.GetType().GetProperty("total")!.GetValue(body)!;
        Assert.Equal(2, total);
    }

    [Fact]
    public void ListarResumenes_Devuelve_Todos_Los_Materializados()
    {
        var (http, _, res) = Build();
        res.Upsert(new[]
        {
            new ResumenCliente { Id = "resumen-cliente-A", ClienteId = "cliente-A", TotalPedidos = 3 },
            new ResumenCliente { Id = "resumen-cliente-B", ClienteId = "cliente-B", TotalPedidos = 1 },
        });

        var result = http.ListarResumenes(HttpRequestFactory.Empty()) as OkObjectResult;

        Assert.NotNull(result);
        var total = (int)result!.Value!.GetType().GetProperty("total")!.GetValue(result.Value)!;
        Assert.Equal(2, total);
    }

    [Fact]
    public void ObtenerResumen_Existente_Devuelve_200_Con_Documento()
    {
        var (http, _, res) = Build();
        res.Upsert(new[] { new ResumenCliente { Id = "resumen-cliente-A", ClienteId = "cliente-A", TotalPedidos = 5, ImporteAcumulado = 500m } });

        var result = http.ObtenerResumen(HttpRequestFactory.Empty(), "cliente-A") as OkObjectResult;

        Assert.NotNull(result);
        var doc = result!.Value as ResumenCliente;
        Assert.NotNull(doc);
        Assert.Equal(5, doc!.TotalPedidos);
        Assert.Equal(500m, doc.ImporteAcumulado);
    }

    [Fact]
    public void ObtenerResumen_Inexistente_Devuelve_404()
    {
        var (http, _, _) = Build();

        var result = http.ObtenerResumen(HttpRequestFactory.Empty(), "no-existe") as NotFoundObjectResult;

        Assert.NotNull(result);
        Assert.Equal(404, result!.StatusCode);
    }
}
