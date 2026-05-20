using WizardMsix.Demo.Api.Wizard;

namespace WizardMsix.Demo.Api.Tests;

// CAPA 1 — decisión Wizard vs CLI (slides 15/17).
[Trait("Category", "Unit")]
public class Unit_WizardVsCliTests
{
    [Fact]
    public void Aprendizaje_Simple_Es_Wizard()
        => Assert.Equal(FlujoEmpaquetado.Wizard,
            WizardVsCliAdvisor.Recomendar(
                new ContextoEmpaquetado(AprendizajeInicial: true,
                    AppSimpleSingleArch: true)).Flujo);

    [Theory]
    [InlineData(true, false, false, false, false)]   // CI/CD
    [InlineData(false, true, false, false, false)]   // Key Vault
    [InlineData(false, false, true, false, false)]   // multi-arch
    [InlineData(false, false, false, true, false)]   // equipo grande
    [InlineData(false, false, false, false, true)]   // distrib corporativa
    public void Cualquier_Factor_Senior_Tira_A_Cli(
        bool ci, bool kv, bool multi, bool equipo, bool corp)
    {
        var r = WizardVsCliAdvisor.Recomendar(new ContextoEmpaquetado(
            PipelineCiCd: ci, CertDesdeKeyVault: kv,
            MultiArquitectura: multi, EquipoGrande: equipo,
            DistribucionCorporativa: corp));
        Assert.Equal(FlujoEmpaquetado.Cli, r.Flujo);
        Assert.NotEmpty(r.Razones);
    }

    [Fact]
    public void Limitaciones_Wizard_Listan_KeyVault_Y_MultiArch()
    {
        var l = WizardVsCliAdvisor.LimitacionesWizard;
        Assert.Contains(l, x => x.Contains("Key Vault"));
        Assert.Contains(l, x => x.Contains("Multi-arch") || x.Contains("multi-arch"));
        Assert.Contains(l, x => x.Contains("AppInstaller"));
    }

    [Fact]
    public void Sin_Senales_Recomienda_Wizard_Por_Defecto()
        => Assert.Equal(FlujoEmpaquetado.Wizard,
            WizardVsCliAdvisor.Recomendar(
                new ContextoEmpaquetado(AppSimpleSingleArch: false)).Flujo);
}
