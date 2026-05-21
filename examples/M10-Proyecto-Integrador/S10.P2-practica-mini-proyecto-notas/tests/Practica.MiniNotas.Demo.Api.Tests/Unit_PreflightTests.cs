using Practica.MiniNotas.Demo.Api.MiniNotas;

namespace Practica.MiniNotas.Demo.Api.Tests;

// CAPA 1 — preflight ligero del mini-proyecto (slide 3).
[Trait("Category", "Unit")]
public class Unit_PreflightTests
{
    private static EscenarioPreflight TodoOk() => new(
        TieneDotNet8SDK: true,
        TieneAzCli: true,
        TieneCurl: true,
        TieneJq: true,
        TieneGit: true,
        HizoM01: true,
        HizoM02: true,
        HizoM05: true);

    [Fact]
    public void Todo_Ok_Esta_Listo()
    {
        var r = MiniNotasPreflight.Comprobar(TodoOk());
        Assert.True(r.ListoParaArrancar);
        Assert.DoesNotContain(r.Hallazgos, h => h.Nivel == NivelPreflight.Bloqueante);
    }

    [Theory]
    [InlineData("TieneDotNet8SDK")]
    [InlineData("TieneAzCli")]
    [InlineData("TieneCurl")]
    public void Falta_Herramienta_Bloqueante_Impide_Arrancar(string propiedad)
    {
        var e = propiedad switch
        {
            "TieneDotNet8SDK" => TodoOk() with { TieneDotNet8SDK = false },
            "TieneAzCli" => TodoOk() with { TieneAzCli = false },
            "TieneCurl" => TodoOk() with { TieneCurl = false },
            _ => throw new InvalidOperationException(propiedad),
        };

        var r = MiniNotasPreflight.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h => h.Nivel == NivelPreflight.Bloqueante);
    }

    [Fact]
    public void Sin_Jq_Es_Solo_Aviso()
    {
        var r = MiniNotasPreflight.Comprobar(TodoOk() with { TieneJq = false });
        Assert.True(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelPreflight.Aviso
            && h.Comprobacion.Contains("jq", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("HizoM01")]
    [InlineData("HizoM02")]
    [InlineData("HizoM05")]
    public void Sin_Modulos_Previos_Solo_Aviso(string propiedad)
    {
        var e = propiedad switch
        {
            "HizoM01" => TodoOk() with { HizoM01 = false },
            "HizoM02" => TodoOk() with { HizoM02 = false },
            "HizoM05" => TodoOk() with { HizoM05 = false },
            _ => throw new InvalidOperationException(propiedad),
        };

        var r = MiniNotasPreflight.Comprobar(e);
        Assert.True(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h => h.Nivel == NivelPreflight.Aviso);
    }
}
