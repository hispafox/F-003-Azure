using WizardMsix.Demo.Api.Wizard;

namespace WizardMsix.Demo.Api.Tests;

// CAPA 1 — expansión del wizard a CLI (slide 15).
[Trait("Category", "Unit")]
public class Unit_ComandosTests
{
    private static ParametrosWizard P() => new(
        "MiEmpresa", "MiApp", "1.0.0.0",
        @"C:\bin\Release\x64",
        @"C:\src\cert.pfx",
        @"C:\out\MiApp_1.0.0.0_x64.msix");

    [Fact]
    public void Expansion_Incluye_Las_4_Herramientas_En_Orden()
    {
        var c = WizardComandosExpander.Expandir(P()).ToList();
        Assert.Equal(4, c.Count);
        Assert.Equal(HerramientaCli.MakeAppx, c[0].Herramienta);
        Assert.Equal(HerramientaCli.SignTool, c[1].Herramienta);
        Assert.Equal(HerramientaCli.ImportCertificate, c[2].Herramienta);
        Assert.Equal(HerramientaCli.AddAppPackage, c[3].Herramienta);
    }

    [Fact]
    public void Comando_MakeAppx_Apunta_Al_Output_Dir()
    {
        var c = WizardComandosExpander.Expandir(P());
        var make = c.First(x => x.Herramienta == HerramientaCli.MakeAppx).Linea;
        Assert.Contains(@"C:\bin\Release\x64", make);
        Assert.Contains(@"C:\out\MiApp_1.0.0.0_x64.msix", make);
    }

    [Fact]
    public void Comando_SignTool_Usa_El_Pfx_Y_Sha256()
    {
        var sign = WizardComandosExpander.Expandir(P())
            .First(x => x.Herramienta == HerramientaCli.SignTool).Linea;
        Assert.Contains("/fd SHA256", sign);
        Assert.Contains(@"C:\src\cert.pfx", sign);
    }

    [Fact]
    public void Import_Certificate_Usa_El_Cer_Junto_Al_Pfx()
    {
        var imp = WizardComandosExpander.Expandir(P())
            .First(x => x.Herramienta == HerramientaCli.ImportCertificate).Linea;
        // El .cer se asume al lado del .pfx (Path.ChangeExtension).
        Assert.Contains(@"C:\src\cert.cer", imp);
        Assert.Contains("TrustedPeople", imp);
    }

    [Fact]
    public void Empresa_Vacia_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            WizardComandosExpander.Expandir(new ParametrosWizard(
                "", "App", "1.0.0.0", "out", "cert.pfx", "msix")));
}
