using Plataforma.Demo.Api.Plataforma;

namespace Plataforma.Demo.Api.Tests;

// CAPA 1 — coste comparado (slides 12, 17).
[Trait("Category", "Unit")]
public class Unit_CosteTests
{
    [Fact]
    public void Cinco_Usuarios_Sin_Addons_Ado_Es_Cero_Github_Es_20()
    {
        // 5 ADO Basic gratis → $0. GitHub 5 × $4 = $20.
        var c = MigrationCostEstimator.Comparar(new EscenarioCoste(5));
        Assert.Equal(0m, c.Ado.TotalMes);
        Assert.Equal(20m, c.Github.TotalMes);
        Assert.Equal(TipoPlataforma.AzureDevOps, c.MasBarata);
        Assert.Equal(20m, c.AhorroMes);
    }

    [Fact]
    public void Diez_Usuarios_Sin_Addons_Ado_30_Github_40()
    {
        // ADO: (10 - 5) × $6 = $30. GitHub: 10 × $4 = $40.
        var c = MigrationCostEstimator.Comparar(new EscenarioCoste(10));
        Assert.Equal(30m, c.Ado.TotalMes);
        Assert.Equal(40m, c.Github.TotalMes);
        Assert.Equal(TipoPlataforma.AzureDevOps, c.MasBarata);
    }

    [Fact]
    public void Test_Plans_Solo_Cuenta_En_Ado()
    {
        var c = MigrationCostEstimator.Comparar(
            new EscenarioCoste(10, TestPlans: true));
        // ADO: $30 base + 10 × $52 Test Plans = $550.
        Assert.Equal(550m, c.Ado.TotalMes);
        // GitHub: igual que sin addon.
        Assert.Equal(40m, c.Github.TotalMes);
        Assert.Equal(TipoPlataforma.GitHubActions, c.MasBarata);
    }

    [Fact]
    public void Ghas_Cuenta_En_Ambas_Plataformas()
    {
        var c = MigrationCostEstimator.Comparar(
            new EscenarioCoste(10, GhasOAdvancedSecurity: true));
        // Ambas suman 10 × $49 sobre su base.
        Assert.Equal(30m + 490m, c.Ado.TotalMes);
        Assert.Equal(40m + 490m, c.Github.TotalMes);
        // ADO sigue más barata por la base.
        Assert.Equal(TipoPlataforma.AzureDevOps, c.MasBarata);
    }

    [Fact]
    public void Usuarios_Cero_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            MigrationCostEstimator.Comparar(new EscenarioCoste(0)));
}
