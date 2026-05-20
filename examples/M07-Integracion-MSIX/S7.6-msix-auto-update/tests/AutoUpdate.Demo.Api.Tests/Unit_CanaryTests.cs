using AutoUpdate.Demo.Api.AutoUpdate;

namespace AutoUpdate.Demo.Api.Tests;

// CAPA 1 — canary rollout (slides 10, 20, 25).
[Trait("Category", "Unit")]
public class Unit_CanaryTests
{
    [Fact]
    public void Cohorte_Es_Determinista()
    {
        // El mismo userId siempre cae en la misma cohorte → un usuario
        // que entró en el 5% sigue dentro en el 25% (monotonicidad).
        int c1 = CanaryRolloutPolicy.Cohorte("alice@empresa.com");
        int c2 = CanaryRolloutPolicy.Cohorte("alice@empresa.com");
        Assert.Equal(c1, c2);
        Assert.InRange(c1, 0, 99);
    }

    [Fact]
    public void Porcentaje_100_Todos_Reciben()
    {
        foreach (var u in new[] { "a", "b", "c", "d", "e" })
            Assert.True(CanaryRolloutPolicy.RecibeActualizacion(u, 100).RecibeNueva);
    }

    [Fact]
    public void Porcentaje_0_Nadie_Recibe()
    {
        foreach (var u in new[] { "a", "b", "c", "d", "e" })
            Assert.False(CanaryRolloutPolicy.RecibeActualizacion(u, 0).RecibeNueva);
    }

    [Fact]
    public void Monotono_25_Implica_5()
    {
        // Si un usuario entra en el 5%, entra en el 25%, el 50% y el 100%.
        foreach (var u in new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" })
        {
            var d5 = CanaryRolloutPolicy.RecibeActualizacion(u, 5);
            var d25 = CanaryRolloutPolicy.RecibeActualizacion(u, 25);
            if (d5.RecibeNueva) Assert.True(d25.RecibeNueva, $"User {u} dentro del 5% pero NO del 25%");
        }
    }

    [Theory]
    [InlineData(5, true, 25)]
    [InlineData(25, true, 50)]
    [InlineData(50, true, 100)]
    [InlineData(100, true, null)]      // ya al 100%
    [InlineData(25, false, 25)]        // salud KO → mantener
    public void Siguiente_Etapa(int actual, bool salud, int? esperada)
        => Assert.Equal(esperada, CanaryRolloutPolicy.SiguienteEtapa(actual, salud));

    [Fact]
    public void Etapa_Fuera_De_Catalogo_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            CanaryRolloutPolicy.SiguienteEtapa(10, true));

    [Theory]
    [InlineData(CanalDistribucion.Stable, "msix-stable", "MiApp-stable")]
    [InlineData(CanalDistribucion.Beta, "msix-beta", "MiApp-beta")]
    [InlineData(CanalDistribucion.Dev, "msix-dev", "MiApp-dev")]
    public void AppInstaller_Uri_Por_Canal_Slide_10(
        CanalDistribucion canal, string carpeta, string archivo)
    {
        var uri = CanaryRolloutPolicy.AppInstallerUri(canal, "https://example.com");
        Assert.Contains(carpeta, uri);
        Assert.Contains(archivo, uri);
    }
}
