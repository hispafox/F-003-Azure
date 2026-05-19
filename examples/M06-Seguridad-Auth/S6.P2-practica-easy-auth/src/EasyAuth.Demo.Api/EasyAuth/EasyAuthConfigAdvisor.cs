namespace EasyAuth.Demo.Api.EasyAuth;

public enum TipoApp { SitioWeb, Api }

// Slide 3 — proveedores de identidad que soporta Easy Auth.
public enum Proveedor { MicrosoftEntra, Google, Facebook, X, GitHub, Apple }

// Slides 2-5 — Easy Auth: la acción ante peticiones no autenticadas
// depende del tipo de app (web → redirect 302 al login; API → 401).
// Tabla de decisión pura.
public static class EasyAuthConfigAdvisor
{
    // Valor del setting "Unauthenticated requests" (slide 5).
    public static string AccionNoAutenticado(TipoApp tipo) => tipo switch
    {
        TipoApp.SitioWeb => "RedirectToLoginPage",   // HTTP 302 (slide 5)
        TipoApp.Api => "Return401",                   // HTTP 401
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

    public static int CodigoHttp(TipoApp tipo) =>
        tipo == TipoApp.SitioWeb ? 302 : 401;

    // Slide 5 — Token store habilitado por defecto (recomendado).
    public const bool TokenStorePorDefecto = true;

    // Slide 3 — proveedores soportados.
    public static IReadOnlyList<Proveedor> Proveedores { get; } =
        [.. Enum.GetValues<Proveedor>()];

    public static bool SoportaProveedor(Proveedor p) => Proveedores.Contains(p);
}
