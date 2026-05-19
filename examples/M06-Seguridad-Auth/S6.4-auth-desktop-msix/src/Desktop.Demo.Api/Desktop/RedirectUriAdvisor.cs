namespace Desktop.Demo.Api.Desktop;

public enum TipoApp { SystemBrowser, Wam, Msix, Legacy }

// Slides 7, 11 — el redirect URI correcto según cómo se autentique la
// app desktop. Lógica pura: construir y clasificar.
public static class RedirectUriAdvisor
{
    public const string SystemBrowser = "http://localhost";
    public const string LegacyOob = "urn:ietf:wg:oauth:2.0:oob";

    // WAM y MSIX usan el broker plugin URI con el client id (slide 7/11).
    public static string Para(TipoApp tipo, string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        return tipo switch
        {
            TipoApp.SystemBrowser => SystemBrowser,
            TipoApp.Wam or TipoApp.Msix =>
                $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}",
            TipoApp.Legacy => LegacyOob,
            _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
        };
    }

    // Slide 7 — `oob` es legacy y NO recomendado.
    public static bool EsLegacy(string redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
        return redirectUri.Trim().Equals(LegacyOob, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EsBroker(string redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
        return redirectUri.StartsWith(
            "ms-appx-web://microsoft.aad.brokerplugin/", StringComparison.OrdinalIgnoreCase);
    }
}
