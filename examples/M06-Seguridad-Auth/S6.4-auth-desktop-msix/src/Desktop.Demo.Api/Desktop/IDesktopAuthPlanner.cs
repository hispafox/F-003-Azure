namespace Desktop.Demo.Api.Desktop;

public sealed record PlanDesktop(
    string Contexto,
    string Metodo,
    bool MetodoRecomendado,
    bool ClientePublico,
    string RedirectUri,
    string AccionToken,
    bool RequiereUi,
    string Nota);

// Compone DesktopFlowAdvisor + RedirectUriAdvisor + TokenLifecycle en un
// plan de autenticación desktop. Servicio inyectable (seam para el test
// de contenedor).
public interface IDesktopAuthPlanner
{
    PlanDesktop Planificar(ContextoDesktop ctx, string clientId, EstadoToken estado);
}

public sealed class DesktopAuthPlanner : IDesktopAuthPlanner
{
    public PlanDesktop Planificar(
        ContextoDesktop ctx, string clientId, EstadoToken estado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var metodo = DesktopFlowAdvisor.Recomendar(ctx);

        // WAM/MSIX → broker URI; resto → http://localhost (slide 7/11).
        var tipoApp = metodo == MetodoAuthDesktop.Wam ? TipoApp.Wam : TipoApp.SystemBrowser;
        var redirectUri = RedirectUriAdvisor.Para(tipoApp, clientId);

        var accion = TokenLifecycle.Siguiente(estado);

        return new PlanDesktop(
            ctx.ToString(),
            metodo.ToString(),
            DesktopFlowAdvisor.EsRecomendado(metodo),
            DesktopFlowAdvisor.EsClientePublico,
            redirectUri,
            accion.ToString(),
            TokenLifecycle.RequiereUi(accion),
            accion switch
            {
                AccionToken.UsarCacheSilent => "Token de cache: sin interacción.",
                AccionToken.RefrescarSilent => "Refresh silencioso: sin interacción.",
                AccionToken.InteractiveConClaims =>
                    "Conditional Access: re-login con los claims pedidos (slide 12).",
                _ => "Login interactivo (primera vez o refresh caducado).",
            });
    }
}
