using PracticaMsix.Demo.Api.Practica;

namespace PracticaMsix.Demo.Api.Tests;

// CAPA 1 — máquina de 8 pasos de la práctica (slides 4-11, 15).
[Trait("Category", "Unit")]
public class Unit_StepsTests
{
    [Fact]
    public void Hay_Exactamente_8_Pasos()
        => Assert.Equal(8, PracticaSteps.Pasos.Count);

    [Fact]
    public void Pasos_Numerados_De_1_A_8_En_Orden()
    {
        var nums = PracticaSteps.Pasos.Select(p => p.Numero).ToList();
        Assert.Equal(Enumerable.Range(1, 8), nums);
    }

    [Fact]
    public void Cada_Paso_Tiene_Criterios_Testeables()
    {
        foreach (var p in PracticaSteps.Pasos)
            Assert.NotEmpty(p.CriteriosValidacion);
    }

    [Fact]
    public void Avanza_Si_Todos_Los_Criterios_Pasan()
    {
        var info = PracticaSteps.Info(PasoPractica.CrearSolucion);
        var ok = Enumerable.Repeat(true, info.CriteriosValidacion.Count).ToList();
        Assert.Equal(PasoPractica.PersonalizarApp,
            PracticaSteps.SiguientePaso(PasoPractica.CrearSolucion, ok));
    }

    [Fact]
    public void Si_Un_Criterio_Falla_No_Avanza()
    {
        var info = PracticaSteps.Info(PasoPractica.ConfigurarManifest);
        var criterios = Enumerable.Repeat(true, info.CriteriosValidacion.Count).ToList();
        criterios[1] = false;
        Assert.Null(PracticaSteps.SiguientePaso(PasoPractica.ConfigurarManifest, criterios));
    }

    [Fact]
    public void Ultimo_Paso_Devuelve_Null_Practica_Completada()
    {
        var info = PracticaSteps.Info(PasoPractica.ConfigurarAppInstaller);
        var ok = Enumerable.Repeat(true, info.CriteriosValidacion.Count).ToList();
        Assert.Null(PracticaSteps.SiguientePaso(PasoPractica.ConfigurarAppInstaller, ok));
    }

    [Fact]
    public void Numero_De_Criterios_Debe_Coincidir()
        => Assert.Throws<ArgumentException>(() =>
            PracticaSteps.SiguientePaso(PasoPractica.CrearSolucion, [true]));

    [Fact]
    public void Paso_Manifest_Recuerda_CN_Equals_Cert_Subject()
    {
        var info = PracticaSteps.Info(PasoPractica.GenerarCertificado);
        Assert.Contains(info.CriteriosValidacion, c => c.Contains("COINCIDE"));
    }
}
