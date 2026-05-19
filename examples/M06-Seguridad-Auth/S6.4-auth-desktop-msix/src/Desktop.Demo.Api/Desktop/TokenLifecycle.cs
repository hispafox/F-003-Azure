namespace Desktop.Demo.Api.Desktop;

public enum AccionToken
{
    UsarCacheSilent,      // access token de cache, sin interacción
    RefrescarSilent,      // refresh token → nuevo access token, sin UI
    Interactive,          // abrir login (primera vez o refresh caducado)
    InteractiveConClaims, // Conditional Access pide claims extra (slide 12)
}

public sealed record EstadoToken(
    bool HayCuentaEnCache,
    bool AccessTokenValido,
    bool RefreshTokenValido,
    bool RetoConditionalAccess);

// Slides 10, 12 — el ciclo de vida del token en desktop como máquina de
// estados pura: AcquireTokenSilent vs Interactive vs claims challenge.
public static class TokenLifecycle
{
    public static AccionToken Siguiente(EstadoToken e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Slide 12 — un reto de Conditional Access (MFA, device
        // compliant) exige login interactivo CON los claims pedidos.
        if (e.RetoConditionalAccess) return AccionToken.InteractiveConClaims;

        // Primera vez: no hay cuenta cacheada → interactivo (slide 10.1).
        if (!e.HayCuentaEnCache) return AccionToken.Interactive;

        // Access token en cache aún válido → silent (slide 10.2).
        if (e.AccessTokenValido) return AccionToken.UsarCacheSilent;

        // Access caducado pero refresh válido → refresh silent (10.3).
        if (e.RefreshTokenValido) return AccionToken.RefrescarSilent;

        // Refresh también caducado (~90 días) → re-login (slide 10.4).
        return AccionToken.Interactive;
    }

    public static bool RequiereUi(AccionToken a) => a is
        AccionToken.Interactive or AccionToken.InteractiveConClaims;
}
