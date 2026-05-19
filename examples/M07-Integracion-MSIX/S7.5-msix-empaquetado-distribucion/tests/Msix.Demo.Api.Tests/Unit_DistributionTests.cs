using Msix.Demo.Api.Msix;

namespace Msix.Demo.Api.Tests;

// CAPA 1 — canal de distribución + política auto-update (slides 7, 8, 9, 26, 27).
[Trait("Category", "Unit")]
public class Unit_DistributionTests
{
    [Fact]
    public void Publico_Es_Microsoft_Store()
        => Assert.Contains(CanalDistribucion.MicrosoftStore,
            DistributionChannelAdvisor.Recomendar(
                new EscenarioDistribucion(AudienciaPublica: true)).Canales);

    [Fact]
    public void Publico_Mas_Power_Users_Anade_Winget()
    {
        var canales = DistributionChannelAdvisor.Recomendar(
            new EscenarioDistribucion(AudienciaPublica: true, DeveloperPowerUsers: true)).Canales;
        Assert.Contains(CanalDistribucion.MicrosoftStore, canales);
        Assert.Contains(CanalDistribucion.Winget, canales);
    }

    [Fact]
    public void Corporativo_Con_Intune_Es_Intune()
        => Assert.Contains(CanalDistribucion.Intune,
            DistributionChannelAdvisor.Recomendar(
                new EscenarioDistribucion(MdmIntune: true)).Canales);

    [Fact]
    public void Corporativo_Con_Blob_Y_Auto_Update_Es_AppInstaller()
        => Assert.Contains(CanalDistribucion.AppInstaller,
            DistributionChannelAdvisor.Recomendar(
                new EscenarioDistribucion(HostingAzureBlob: true,
                    AutoUpdateNecesario: true)).Canales);

    [Fact]
    public void Sin_Senales_Cae_En_AppInstaller_Por_Defecto()
        => Assert.Contains(CanalDistribucion.AppInstaller,
            DistributionChannelAdvisor.Recomendar(
                new EscenarioDistribucion(HostingAzureBlob: false,
                    AutoUpdateNecesario: false)).Canales);

    [Fact]
    public void Politica_Por_Defecto_Comprueba_Al_Abrir_Y_En_Background()
    {
        var p = DistributionChannelAdvisor.PoliticaPorDefecto();
        Assert.Equal(1, p.HoursBetweenUpdateChecks);
        Assert.True(p.ShowPrompt);
        Assert.True(p.AutomaticBackgroundTask);
        Assert.True(p.ForceUpdateFromAnyVersion);
    }
}
