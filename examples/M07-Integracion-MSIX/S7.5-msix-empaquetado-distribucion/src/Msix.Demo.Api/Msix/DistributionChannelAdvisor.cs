namespace Msix.Demo.Api.Msix;

public enum CanalDistribucion
{
    MicrosoftStore,         // slide 26 — público
    AppInstaller,           // slide 7/26 — sideloading con auto-update
    Intune,                 // slide 27 — MDM empresarial
    Winget,                 // slide 26 — developer/power users
    StoreForBusiness,       // empresas que ya están en el ecosistema Store
}

public sealed record EscenarioDistribucion(
    bool AudienciaPublica = false,
    bool MdmIntune = false,
    bool HostingAzureBlob = true,        // tienen Blob/CDN para .msix
    bool AutoUpdateNecesario = true,
    bool DeveloperPowerUsers = false);

public sealed record RecomendacionDistribucion(
    IReadOnlyList<CanalDistribucion> Canales,
    IReadOnlyList<string> Razones);

// Slides 7, 8, 9, 26, 27 — decisión de canal(es) de distribución.
public static class DistributionChannelAdvisor
{
    public static RecomendacionDistribucion Recomendar(EscenarioDistribucion e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var canales = new List<CanalDistribucion>();
        var razones = new List<string>();

        if (e.AudienciaPublica)
        {
            canales.Add(CanalDistribucion.MicrosoftStore);
            razones.Add("Audiencia pública → Microsoft Store (slide 26).");
            if (e.DeveloperPowerUsers)
            {
                canales.Add(CanalDistribucion.Winget);
                razones.Add("Developer/power users → winget complementa al Store (slide 26).");
            }
        }
        else
        {
            // Distribución corporativa.
            if (e.MdmIntune)
            {
                canales.Add(CanalDistribucion.Intune);
                razones.Add("MDM empresarial con Intune → despliegue silencioso (slide 27).");
            }
            if (e.HostingAzureBlob && e.AutoUpdateNecesario)
            {
                canales.Add(CanalDistribucion.AppInstaller);
                razones.Add("Hosting en Azure Blob + auto-update → .appinstaller (slide 7/8).");
            }
            if (canales.Count == 0)
            {
                canales.Add(CanalDistribucion.AppInstaller);
                razones.Add("Distribución corporativa por defecto → sideloading con AppInstaller (slide 9).");
            }
        }

        return new RecomendacionDistribucion(canales, razones);
    }

    // Slide 7/26 — política de UpdateSettings del .appinstaller.
    public sealed record PoliticaAutoUpdate(
        int HoursBetweenUpdateChecks,
        bool ShowPrompt,
        bool AutomaticBackgroundTask,
        bool ForceUpdateFromAnyVersion);

    public static PoliticaAutoUpdate PoliticaPorDefecto() =>
        new(HoursBetweenUpdateChecks: 1,           // slide 7 — al abrir
            ShowPrompt: true,                       // notificar al usuario
            AutomaticBackgroundTask: true,          // bajar en background
            ForceUpdateFromAnyVersion: true);       // saltar versiones
}
