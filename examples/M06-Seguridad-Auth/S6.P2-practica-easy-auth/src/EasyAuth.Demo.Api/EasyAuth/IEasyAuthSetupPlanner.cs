namespace EasyAuth.Demo.Api.EasyAuth;

public sealed record PlanEasyAuth(
    string TipoApp,
    string AccionNoAutenticado,
    int CodigoHttp,
    bool TokenStore,
    string LoginUrl,
    string LogoutUrl,
    string MeUrl,
    IReadOnlyList<string> Checklist);

// Compone EasyAuthConfigAdvisor + AuthEndpoints en un plan de
// configuración de Easy Auth. Servicio inyectable (seam del test DI).
public interface IEasyAuthSetupPlanner
{
    PlanEasyAuth Planificar(TipoApp tipo, string proveedor, string appUrl);
}

public sealed class EasyAuthSetupPlanner : IEasyAuthSetupPlanner
{
    public PlanEasyAuth Planificar(TipoApp tipo, string proveedor, string appUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proveedor);
        ArgumentException.ThrowIfNullOrWhiteSpace(appUrl);

        return new PlanEasyAuth(
            tipo.ToString(),
            EasyAuthConfigAdvisor.AccionNoAutenticado(tipo),
            EasyAuthConfigAdvisor.CodigoHttp(tipo),
            EasyAuthConfigAdvisor.TokenStorePorDefecto,
            AuthEndpoints.Login(proveedor, appUrl),
            AuthEndpoints.Logout(appUrl),
            AuthEndpoints.Me,
            // Slide 2 — recorrido / verificación de la práctica.
            [
                "Web App F1 desplegada (contenido visible sin auth)",
                "Easy Auth añadido: Microsoft (Create new app registration)",
                "Require authentication + acción no-auth según tipo de app",
                "Sin login → " + EasyAuthConfigAdvisor.CodigoHttp(tipo) +
                    "; con login → tu identidad",
                "/.auth/me devuelve claims; /.auth/logout cierra sesión",
                "Cero código C# de auth (el middleware vive en App Service)",
            ]);
    }
}
