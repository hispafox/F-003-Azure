using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

public class TimerFunctionsTests
{
    private static (TimerFunctions fn, IProductoService productos, IInformeService informes) Build()
    {
        var productos = new InMemoryProductoService();
        var informes = new InMemoryInformeService(productos);
        var fn = new TimerFunctions(productos, informes, NullLogger<TimerFunctions>.Instance);
        return (fn, productos, informes);
    }

    [Fact]
    public void CleanupCadaMinuto_Runs_With_Default_TimerInfo()
    {
        var (fn, _, _) = Build();
        fn.CleanupCadaMinuto(new TimerInfo());
        // Si llegamos aquí sin excepción, el timer corrió OK.
    }

    [Fact]
    public void CleanupCadaMinuto_Handles_PastDue_TimerInfo()
    {
        var (fn, _, _) = Build();

        var timer = new TimerInfo
        {
            IsPastDue = true,
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddMinutes(-5),
                Next = DateTime.UtcNow.AddMinutes(-1),
                LastUpdated = DateTime.UtcNow
            }
        };

        fn.CleanupCadaMinuto(timer);
    }

    [Fact]
    public void InformeDiario_Generates_Informe_For_Yesterday()
    {
        var (fn, _, informes) = Build();
        var ayer = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));

        fn.InformeDiario(new TimerInfo());

        var informe = informes.GetByFecha(ayer);
        Assert.NotNull(informe);
        Assert.Equal(3, informe!.TotalProductos); // los 3 sembrados por defecto
    }

    [Fact]
    public void InformeDiario_Is_Idempotent_Across_Multiple_Runs()
    {
        var (fn, _, informes) = Build();

        // El timer se dispara 5 veces (simulamos retries / past-due / dobles)
        for (var i = 0; i < 5; i++)
        {
            fn.InformeDiario(new TimerInfo());
        }

        // Solo debe existir un informe para "ayer"
        Assert.Single(informes.Listar());
    }
}
