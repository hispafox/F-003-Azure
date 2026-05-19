namespace Entra.Demo.Api.Entra;

public enum Escenario
{
    RecursoAzureAccedeAOtro,   // App Service → Cosmos DB
    ScriptOPipeline,           // CI/CD se autentica
    AppAutenticaUsuarios,      // Web App → "Login con tu cuenta"
}

public enum TipoIdentidad { ManagedIdentity, ServicePrincipal, AppRegistration }

// Slide 10 — Managed Identity vs Service Principal vs App Registration.
// Tabla de decisión pura + la prioridad de uso (slide 10).
public static class IdentityTypeAdvisor
{
    public static TipoIdentidad Recomendar(Escenario escenario) => escenario switch
    {
        Escenario.RecursoAzureAccedeAOtro => TipoIdentidad.ManagedIdentity,
        Escenario.ScriptOPipeline => TipoIdentidad.ServicePrincipal,
        Escenario.AppAutenticaUsuarios => TipoIdentidad.AppRegistration,
        _ => throw new ArgumentOutOfRangeException(nameof(escenario)),
    };

    // Slide 10 — Managed Identity no tiene secreto (lo gestiona Azure).
    public static bool TieneSecreto(TipoIdentidad t) => t != TipoIdentidad.ManagedIdentity;

    // Slide 10 — prioridad: 1) MI  2) SP con certificado  3) SP con secret.
    public static IReadOnlyList<TipoIdentidad> Prioridad { get; } =
    [
        TipoIdentidad.ManagedIdentity,
        TipoIdentidad.ServicePrincipal,
        TipoIdentidad.AppRegistration,
    ];
}
