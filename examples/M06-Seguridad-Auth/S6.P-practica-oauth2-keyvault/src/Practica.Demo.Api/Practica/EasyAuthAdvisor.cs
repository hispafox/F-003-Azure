namespace Practica.Demo.Api.Practica;

public enum TipoApp { Api, WebApp }

// Slide 8 — Easy Auth: una API devuelve 401 si no hay token; una web
// app redirige al login de Entra ID. Y el issuer v2.0. Lógica pura.
public static class EasyAuthAdvisor
{
    // `--action` de `az webapp auth update` según el tipo de app.
    public static string AccionNoAutenticado(TipoApp tipo) => tipo switch
    {
        TipoApp.Api => "Return401",
        TipoApp.WebApp => "LoginWithAzureActiveDirectory",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    // Slide 8 — el issuer de Entra ID v2.0 para el tenant.
    public static string Issuer(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"https://login.microsoftonline.com/{tenantId}/v2.0";
    }
}
