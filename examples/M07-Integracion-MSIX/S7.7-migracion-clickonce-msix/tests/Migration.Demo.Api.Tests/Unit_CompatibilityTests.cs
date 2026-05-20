using Migration.Demo.Api.Migration;

namespace Migration.Demo.Api.Tests;

// CAPA 1 — compatibility check (slides 3, 12).
[Trait("Category", "Unit")]
public class Unit_CompatibilityTests
{
    [Fact]
    public void Wpf_Filesystem_Http_Es_Ok()
    {
        var r = MigrationCompatibilityCheck.Evaluar(
            [ComportamientoApp.Wpf,
             ComportamientoApp.UsaFilesystemDelUsuario,
             ComportamientoApp.LlamadasHttp]);
        Assert.Equal(NivelRiesgo.Ok, r.Riesgo);
        Assert.False(r.RequierePsf);
    }

    [Theory]
    [InlineData(ComportamientoApp.EscribeHKLM)]
    [InlineData(ComportamientoApp.WindowsService)]
    [InlineData(ComportamientoApp.BuscaDllsEnPathGlobal)]
    public void Comportamientos_Que_Requieren_Psf_Son_Precaucion(
        ComportamientoApp comportamiento)
    {
        var r = MigrationCompatibilityCheck.Evaluar(
            [ComportamientoApp.Wpf, comportamiento]);
        Assert.Equal(NivelRiesgo.Precaucion, r.Riesgo);
        Assert.True(r.RequierePsf);
    }

    [Theory]
    [InlineData(ComportamientoApp.KernelDriver)]
    [InlineData(ComportamientoApp.EscribeProgramFilesOWindows)]
    public void Bloqueadores_Detectados_Slide_3(ComportamientoApp comportamiento)
    {
        var r = MigrationCompatibilityCheck.Evaluar(
            [ComportamientoApp.Wpf, comportamiento]);
        Assert.Equal(NivelRiesgo.Bloqueador, r.Riesgo);
    }

    [Fact]
    public void Hallazgos_Sin_Duplicados()
    {
        var r = MigrationCompatibilityCheck.Evaluar(
            [ComportamientoApp.Wpf, ComportamientoApp.Wpf, ComportamientoApp.LlamadasHttp]);
        Assert.Equal(2, r.Hallazgos.Count);
    }

    [Fact]
    public void Bloqueador_Tiene_Prioridad_Sobre_Precaucion()
    {
        // Si hay un bloqueador, el nivel global es Bloqueador aunque
        // también haya factores que requieren PSF.
        var r = MigrationCompatibilityCheck.Evaluar(
            [ComportamientoApp.EscribeHKLM, ComportamientoApp.KernelDriver]);
        Assert.Equal(NivelRiesgo.Bloqueador, r.Riesgo);
    }
}
