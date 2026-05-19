namespace Distribution.Demo.Api.Distribution;

// Slides 4, 11, 26 — formatos de distribución desktop Windows.
public enum FormatoDistribucion { ClickOnce, Msix, Msi, Winget }

// Slide 26 — características en la matriz de decisión.
public enum CaracteristicaDistribucion
{
    AutoUpdate,
    UserInstall,             // instala sin permisos de admin
    AdminRequired,
    Sandboxing,
    AppIdentity,
    ModernApis,
    LegacyApps,
    DotNet8Plus,
    IntuneCompatible,
    MicrosoftStoreCompatible,
    DifferentialUpdates,
    FuturoMicrosoft,         // slide 11
}

// Slides 4, 11, 26 — tabla de características pura (look-up). El valor
// docente es el "qué soporta qué" y por qué Microsoft empuja MSIX.
public static class DistributionFormatComparator
{
    public static bool Soporta(FormatoDistribucion f, CaracteristicaDistribucion c) =>
        Matriz[f].Contains(c);

    // Slide 4 — un único punto donde ClickOnce pierde respecto a MSIX:
    // formato estándar, instalación/desinstalación limpia, Intune,
    // auto-update con auth, .NET 8+, sideloading completo, CI/CD.
    public static int VentajasMsixSobreClickOnce()
    {
        int v = 0;
        foreach (CaracteristicaDistribucion c in Enum.GetValues<CaracteristicaDistribucion>())
            if (Soporta(FormatoDistribucion.Msix, c) &&
                !Soporta(FormatoDistribucion.ClickOnce, c))
                v++;
        return v;
    }

    // Hechos de slides 4 (ClickOnce vs MSIX) y 26 (matriz final).
    private static readonly Dictionary<FormatoDistribucion, HashSet<CaracteristicaDistribucion>> Matriz =
        new()
        {
            [FormatoDistribucion.ClickOnce] =
            [
                CaracteristicaDistribucion.AutoUpdate,
                CaracteristicaDistribucion.UserInstall,
                CaracteristicaDistribucion.LegacyApps,
            ],
            [FormatoDistribucion.Msix] =
            [
                CaracteristicaDistribucion.AutoUpdate,
                CaracteristicaDistribucion.UserInstall,
                CaracteristicaDistribucion.Sandboxing,
                CaracteristicaDistribucion.AppIdentity,
                CaracteristicaDistribucion.ModernApis,
                CaracteristicaDistribucion.DotNet8Plus,
                CaracteristicaDistribucion.IntuneCompatible,
                CaracteristicaDistribucion.MicrosoftStoreCompatible,
                CaracteristicaDistribucion.DifferentialUpdates,
                CaracteristicaDistribucion.FuturoMicrosoft,
            ],
            [FormatoDistribucion.Msi] =
            [
                CaracteristicaDistribucion.AdminRequired,
                CaracteristicaDistribucion.LegacyApps,
                CaracteristicaDistribucion.IntuneCompatible,
            ],
            [FormatoDistribucion.Winget] =
            [
                CaracteristicaDistribucion.AutoUpdate,
                CaracteristicaDistribucion.UserInstall,
                CaracteristicaDistribucion.Sandboxing,                 // vía MSIX
                CaracteristicaDistribucion.AppIdentity,
                CaracteristicaDistribucion.ModernApis,
                CaracteristicaDistribucion.IntuneCompatible,
                CaracteristicaDistribucion.DifferentialUpdates,        // vía MSIX
                CaracteristicaDistribucion.FuturoMicrosoft,
            ],
        };
}
