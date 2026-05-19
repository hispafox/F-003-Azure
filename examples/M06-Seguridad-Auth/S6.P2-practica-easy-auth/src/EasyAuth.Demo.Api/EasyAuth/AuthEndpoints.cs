namespace EasyAuth.Demo.Api.EasyAuth;

// Slides 4-6 — los endpoints integrados que App Service expone bajo
// `/.auth/*` (sin escribir código). Construcción pura de esas rutas.
public static class AuthEndpoints
{
    public const string Me = "/.auth/me";
    public const string Prefijo = "/.auth/";

    public static string Login(string proveedor = "aad", string? postLoginRedirect = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proveedor);
        var url = $"/.auth/login/{proveedor.ToLowerInvariant()}";
        return string.IsNullOrWhiteSpace(postLoginRedirect)
            ? url
            : $"{url}?post_login_redirect_url={Uri.EscapeDataString(postLoginRedirect)}";
    }

    public static string LoginCallback(string proveedor = "aad")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proveedor);
        return $"/.auth/login/{proveedor.ToLowerInvariant()}/callback";
    }

    public static string Logout(string? postLogoutRedirect = null)
    {
        const string url = "/.auth/logout";
        return string.IsNullOrWhiteSpace(postLogoutRedirect)
            ? url
            : $"{url}?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirect)}";
    }

    // Una ruta /.auth/* la gestiona Easy Auth, NO tu código (slide 5).
    public static bool EsRutaEasyAuth(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase);
    }
}
