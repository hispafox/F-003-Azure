using Datos.Demo.Api.Datos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Datos.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Sin CAPA de integración (cifrado/
// TLS/CMK/CORS es configuración, no algo emulable), este test es el
// único que ejercita el grafo DI. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void DataProtectionAssessor_Se_Resuelve_Y_Evalua()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<IDataProtectionAssessor>();
        Assert.NotNull(svc);
        Assert.Same(svc, factory.Services.GetRequiredService<IDataProtectionAssessor>());

        var r = svc.Evaluar(new ChecklistDatos(
            true, "1.2", "Server=x;Encrypt=true;",
            "https://stx.blob.core.windows.net", true,
            Sensibilidad.Normal, false,
            ["https://app.azurewebsites.net"], true));

        Assert.Equal(100, r.Puntuacion);
        Assert.Equal(EstrategiaCifrado.MmkAtRest, r.CifradoRecomendado);
    }
}
