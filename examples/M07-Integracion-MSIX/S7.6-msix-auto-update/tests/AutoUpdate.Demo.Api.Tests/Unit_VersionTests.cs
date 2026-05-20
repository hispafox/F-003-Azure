using AutoUpdate.Demo.Api.AutoUpdate;

namespace AutoUpdate.Demo.Api.Tests;

// CAPA 1 — comparación de versiones + rollback (slides 7, 8, 13).
[Trait("Category", "Unit")]
public class Unit_VersionTests
{
    [Fact]
    public void Disponible_Mayor_Actualiza()
    {
        var d = UpdateVersionAdvisor.Comparar("2.4.1.0", "2.4.2.0");
        Assert.True(d.DebeActualizar);
        Assert.Equal("mayor", d.Comparacion);
    }

    [Fact]
    public void Misma_Version_No_Actualiza()
        => Assert.False(UpdateVersionAdvisor.Comparar("2.4.1.0", "2.4.1.0").DebeActualizar);

    [Fact]
    public void Disponible_Menor_Bloqueada_Sin_Force()
        => Assert.False(UpdateVersionAdvisor.Comparar("2.4.2.0", "2.4.1.0").DebeActualizar);

    [Fact]
    public void Disponible_Menor_Permitida_Con_Force()
    {
        var d = UpdateVersionAdvisor.Comparar("2.4.2.0", "2.4.1.0", forceFromAnyVersion: true);
        Assert.True(d.DebeActualizar);
        Assert.Contains("ForceUpdateFromAnyVersion", d.Razon);
    }

    [Theory]
    [InlineData("2.4.1.0", "2.4.5.0", true)]    // por debajo del mínimo → obligatoria
    [InlineData("2.4.5.0", "2.4.5.0", false)]
    [InlineData("2.4.6.0", "2.4.5.0", false)]
    public void Es_Obligatoria(string actual, string minimo, bool esperado)
        => Assert.Equal(esperado, UpdateVersionAdvisor.EsObligatoria(actual, minimo));

    [Fact]
    public void Plan_Rollback_Republica_Previa_Con_Build_Plus_1()
    {
        var plan = UpdateVersionAdvisor.PlanificarRollback(
            "2.4.147.0",
            ["2.4.145.0", "2.4.146.0", "2.4.147.0"]);

        Assert.NotNull(plan);
        Assert.Equal("2.4.146.0", plan!.VersionPreviaBuena);
        Assert.Equal("2.4.148.0", plan.EtiquetaRollback);
    }

    [Fact]
    public void Plan_Rollback_Devuelve_Null_Si_Es_La_Primera()
        => Assert.Null(UpdateVersionAdvisor.PlanificarRollback(
            "2.4.145.0", ["2.4.145.0"]));

    [Fact]
    public void Plan_Rollback_Soporta_Historial_Desordenado()
    {
        var plan = UpdateVersionAdvisor.PlanificarRollback(
            "2.4.147.0",
            ["2.4.147.0", "2.4.145.0", "2.4.146.0"]);
        Assert.Equal("2.4.146.0", plan!.VersionPreviaBuena);
    }

    [Fact]
    public void Version_Mal_Formada_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            UpdateVersionAdvisor.Comparar("2.4", "2.4.1.0"));
}
