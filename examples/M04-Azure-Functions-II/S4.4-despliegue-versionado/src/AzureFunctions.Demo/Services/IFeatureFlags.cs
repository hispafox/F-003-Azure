namespace AzureFunctions.Demo.Services;

// Slide 16 — feature flags vía App Settings (env vars). Apagar un flag
// es un "rollback sin redeploy": cambias el setting en el Portal y la
// función conmuta de lógica sin volver a desplegar.
public interface IFeatureFlags
{
    bool Activo(string nombre);
}

public sealed class EnvFeatureFlags : IFeatureFlags
{
    // Convención: el App Setting se llama FEATURE_<NOMBRE> y vale "true".
    // Cualquier otro valor (o ausencia) = desactivado → camino legacy.
    public bool Activo(string nombre)
    {
        var raw = Environment.GetEnvironmentVariable($"FEATURE_{nombre}");
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }
}
