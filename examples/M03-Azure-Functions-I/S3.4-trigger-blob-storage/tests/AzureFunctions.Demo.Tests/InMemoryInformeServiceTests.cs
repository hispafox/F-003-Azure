using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class InMemoryInformeServiceTests
{
    private static IInformeService NewService() =>
        new InMemoryInformeService(new InMemoryProductoService());

    [Fact]
    public void First_Call_Generates_New_Informe()
    {
        var svc = NewService();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var (yaExistia, informe) = svc.GenerarSiNoExiste(hoy);

        Assert.False(yaExistia);
        Assert.Equal(hoy, informe.Fecha);
        Assert.Equal($"informe-{hoy:yyyy-MM-dd}", informe.Id);
    }

    [Fact]
    public void Second_Call_Returns_Existing_Informe()
    {
        var svc = NewService();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var (_, primero) = svc.GenerarSiNoExiste(hoy);
        var (yaExistia, segundo) = svc.GenerarSiNoExiste(hoy);

        Assert.True(yaExistia);
        Assert.Same(primero, segundo); // misma referencia: idempotencia real
    }

    [Fact]
    public void Multiple_Threads_Producing_Same_Date_Get_Same_Informe()
    {
        var svc = NewService();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var resultados = new System.Collections.Concurrent.ConcurrentBag<string>();
        Parallel.For(0, 50, _ =>
        {
            var (_, informe) = svc.GenerarSiNoExiste(hoy);
            resultados.Add(informe.Id);
        });

        // Todos los hilos deben haber visto el MISMO id
        Assert.Single(resultados.Distinct());
        Assert.Single(svc.Listar());
    }

    [Fact]
    public void Listar_Returns_Newest_First()
    {
        var svc = NewService();
        svc.GenerarSiNoExiste(new DateOnly(2026, 4, 20));
        svc.GenerarSiNoExiste(new DateOnly(2026, 4, 22));
        svc.GenerarSiNoExiste(new DateOnly(2026, 4, 21));

        var listado = svc.Listar();

        Assert.Equal(new DateOnly(2026, 4, 22), listado[0].Fecha);
        Assert.Equal(new DateOnly(2026, 4, 20), listado[2].Fecha);
    }
}
