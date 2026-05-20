using Migration.Demo.Api.Migration;

namespace Migration.Demo.Api.Tests;

// CAPA 1 — roadmap por fases con criterios de salida (slides 2, 11).
[Trait("Category", "Unit")]
public class Unit_RoadmapTests
{
    [Fact]
    public void Empaquetado_Tiene_Criterios_De_Salida()
    {
        var info = MigrationRoadmap.Info(FaseMigracion.Empaquetado);
        Assert.Equal(FaseMigracion.Empaquetado, info.Fase);
        Assert.NotEmpty(info.CriteriosSalida);
        Assert.Contains(info.CriteriosSalida, c => c.Contains("Identity"));
    }

    [Fact]
    public void Avanza_Si_Todos_Los_Criterios_Pasan()
    {
        var info = MigrationRoadmap.Info(FaseMigracion.Empaquetado);
        var todosOk = Enumerable.Repeat(true, info.CriteriosSalida.Count).ToList();
        Assert.Equal(FaseMigracion.Piloto,
            MigrationRoadmap.SiguienteFase(FaseMigracion.Empaquetado, todosOk));
    }

    [Fact]
    public void Si_Un_Criterio_Falla_No_Avanza()
    {
        var info = MigrationRoadmap.Info(FaseMigracion.Piloto);
        var criterios = Enumerable.Repeat(true, info.CriteriosSalida.Count).ToList();
        criterios[0] = false;
        Assert.Null(MigrationRoadmap.SiguienteFase(FaseMigracion.Piloto, criterios));
    }

    [Fact]
    public void Modernizar_Dotnet_8_Es_Fase_Final()
    {
        var info = MigrationRoadmap.Info(FaseMigracion.ModernizarDotNet8);
        var todosOk = Enumerable.Repeat(true, info.CriteriosSalida.Count).ToList();
        Assert.Null(MigrationRoadmap.SiguienteFase(FaseMigracion.ModernizarDotNet8, todosOk));
    }

    [Fact]
    public void Numero_De_Criterios_Tiene_Que_Coincidir()
        => Assert.Throws<ArgumentException>(() =>
            MigrationRoadmap.SiguienteFase(FaseMigracion.Empaquetado, [true]));

    [Fact]
    public void Todas_Las_Fases_Documentadas()
        => Assert.Equal(
            Enum.GetValues<FaseMigracion>().Length,
            MigrationRoadmap.Fases.Count);
}
