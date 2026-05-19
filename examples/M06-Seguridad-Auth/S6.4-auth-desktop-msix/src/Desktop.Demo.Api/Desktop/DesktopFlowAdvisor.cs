namespace Desktop.Demo.Api.Desktop;

public enum ContextoDesktop
{
    WindowsEntraJoined,    // PC unido a Entra ID → SSO nativo
    WindowsGenerico,       // Windows sin join → browser del sistema
    MultiPlataforma,       // Linux/macOS → browser del sistema
    KioscoOCli,            // sin navegador / pantalla compartida
}

public enum MetodoAuthDesktop { Wam, SystemBrowser, EmbeddedBrowser, DeviceCode }

// Slide 3 — flujos OAuth2 para desktop. WAM es la mejor opción en
// Windows (SSO nativo, como Office/Teams). Tabla de decisión pura.
public static class DesktopFlowAdvisor
{
    public static MetodoAuthDesktop Recomendar(ContextoDesktop ctx) => ctx switch
    {
        ContextoDesktop.WindowsEntraJoined => MetodoAuthDesktop.Wam,
        ContextoDesktop.WindowsGenerico => MetodoAuthDesktop.SystemBrowser,
        ContextoDesktop.MultiPlataforma => MetodoAuthDesktop.SystemBrowser,
        ContextoDesktop.KioscoOCli => MetodoAuthDesktop.DeviceCode,
        _ => throw new ArgumentOutOfRangeException(nameof(ctx)),
    };

    // Slide 3 — el embedded browser (WebView2) es solo "aceptable":
    // nunca recomendado frente a system browser o WAM.
    public static bool EsRecomendado(MetodoAuthDesktop m) =>
        m != MetodoAuthDesktop.EmbeddedBrowser;

    // Slide 4 — una app desktop es SIEMPRE cliente público (no puede
    // guardar un client secret): usa PKCE, nunca client credentials.
    public const bool EsClientePublico = true;
}
