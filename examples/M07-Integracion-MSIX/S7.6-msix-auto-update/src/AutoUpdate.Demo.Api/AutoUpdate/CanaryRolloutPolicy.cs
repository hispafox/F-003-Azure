using System.Security.Cryptography;
using System.Text;

namespace AutoUpdate.Demo.Api.AutoUpdate;

// Slide 10 — canales que ven los usuarios.
public enum CanalDistribucion { Stable, Beta, Dev }

// Slide 20 — etapas de staged release.
public static class EtapasCanary
{
    public static readonly IReadOnlyList<int> Porcentajes = [5, 25, 50, 100];
}

public sealed record DecisionRollout(bool RecibeNueva, int PorcentajeUmbral, int Hash);

// Slides 10, 20, 25 — asignación de cohortes para staged rollout.
// Lógica pura y DETERMINISTA: el mismo userId siempre cae en la misma
// cohorte → un usuario que entra en el 5% sigue dentro en el 25%/50%.
public static class CanaryRolloutPolicy
{
    // Hash estable de userId → entero [0..99].
    public static int Cohorte(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        byte[] sha = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        // Tomamos los primeros 4 bytes como uint y aplicamos mod 100.
        uint n = BitConverter.ToUInt32(sha, 0);
        return (int)(n % 100);
    }

    // ¿Este userId recibe la nueva versión en una etapa con `porcentaje`
    // de rollout? Slide 20.
    public static DecisionRollout RecibeActualizacion(string userId, int porcentaje)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(porcentaje);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(porcentaje, 100);
        int c = Cohorte(userId);
        return new DecisionRollout(RecibeNueva: c < porcentaje,
            PorcentajeUmbral: porcentaje, Hash: c);
    }

    // Slide 20/24 — siguiente etapa si la actual está sana; si no, no
    // avanzar (mantener / rollback). Devuelve null si ya está al 100%.
    public static int? SiguienteEtapa(int etapaActual, bool saludOk)
    {
        if (!EtapasCanary.Porcentajes.Contains(etapaActual))
            throw new ArgumentOutOfRangeException(nameof(etapaActual),
                $"Etapa fuera del catálogo {string.Join(",", EtapasCanary.Porcentajes)}.");
        if (!saludOk) return etapaActual;          // no avanzar (slide 21)
        return EtapasCanary.Porcentajes
            .SkipWhile(p => p != etapaActual)
            .Skip(1)
            .Cast<int?>()
            .FirstOrDefault();
    }

    // Slide 10 — URL del .appinstaller del canal.
    public static string AppInstallerUri(CanalDistribucion canal, string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        string sufijo = canal switch
        {
            CanalDistribucion.Stable => "stable",
            CanalDistribucion.Beta => "beta",
            CanalDistribucion.Dev => "dev",
            _ => throw new ArgumentOutOfRangeException(nameof(canal)),
        };
        return $"{baseUrl.TrimEnd('/')}/msix-{sufijo}/MiApp-{sufijo}.appinstaller";
    }
}
