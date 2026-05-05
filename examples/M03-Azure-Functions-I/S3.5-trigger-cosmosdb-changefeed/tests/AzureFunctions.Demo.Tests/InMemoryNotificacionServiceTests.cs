using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class InMemoryNotificacionServiceTests
{
    [Fact]
    public void EnviarSiNoEnviada_Primera_Vez_Devuelve_True()
    {
        var svc = new InMemoryNotificacionService();

        var ok = svc.EnviarSiNoEnviada("ped-1", "cliente-1", "confirmado", "Tu pedido…");

        Assert.True(ok);
        Assert.Equal(1, svc.Total);
    }

    [Fact]
    public void EnviarSiNoEnviada_Segunda_Vez_Misma_Clave_Devuelve_False()
    {
        var svc = new InMemoryNotificacionService();
        svc.EnviarSiNoEnviada("ped-1", "cliente-1", "confirmado", "Tu pedido…");

        var ok = svc.EnviarSiNoEnviada("ped-1", "cliente-1", "confirmado", "Mensaje distinto");

        Assert.False(ok);
        Assert.Equal(1, svc.Total);
        // Y el mensaje original NO se sobreescribe
        Assert.Equal("Tu pedido…", svc.Buscar("ped-1", "confirmado")!.Mensaje);
    }

    [Fact]
    public void Buscar_Es_CaseInsensitive_Sobre_Estado()
    {
        var svc = new InMemoryNotificacionService();
        svc.EnviarSiNoEnviada("ped-1", "cliente-1", "Confirmado", "msg");

        Assert.NotNull(svc.Buscar("ped-1", "confirmado"));
        Assert.NotNull(svc.Buscar("ped-1", "CONFIRMADO"));
    }

    [Fact]
    public void EnviarSiNoEnviada_Concurrente_Solo_Cuenta_Una_Vez()
    {
        // Slide 10 — at-least-once + escalado por particiones (slide 11):
        // dos hilos pueden intentar enviar la misma notificación a la vez.
        // ConcurrentDictionary.GetOrAdd garantiza exactamente un insert.
        var svc = new InMemoryNotificacionService();

        var resultados = Enumerable.Range(0, 100).AsParallel()
            .Select(_ => svc.EnviarSiNoEnviada("ped-1", "cliente-1", "confirmado", "msg"))
            .ToList();

        Assert.Equal(1, resultados.Count(r => r));
        Assert.Equal(99, resultados.Count(r => !r));
        Assert.Equal(1, svc.Total);
    }

    [Fact]
    public void ListarPorCliente_Devuelve_Solo_De_Ese_Cliente()
    {
        var svc = new InMemoryNotificacionService();
        svc.EnviarSiNoEnviada("ped-1", "cliente-A", "confirmado", "m1");
        svc.EnviarSiNoEnviada("ped-2", "cliente-A", "enviado", "m2");
        svc.EnviarSiNoEnviada("ped-3", "cliente-B", "confirmado", "m3");

        Assert.Equal(2, svc.ListarPorCliente("cliente-A").Count);
        Assert.Single(svc.ListarPorCliente("cliente-B"));
        Assert.Empty(svc.ListarPorCliente("cliente-C"));
    }
}
