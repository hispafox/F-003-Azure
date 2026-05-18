namespace Sql.Demo.Api.Sql;

// Slides 4, 5, 21 — el modelo de compra (DTU vs vCore vs Serverless vs
// Hyperscale) expresado como tabla de decisión pura, igual que el
// AccessTierPolicy de S5.1. Alimenta el endpoint /sql/tier-sugerido.
public enum SqlTier
{
    Basic,                    // slide 4: dev/test, ~4€/mes, 2 GB
    S0,                       // slide 4: curso/dev, ~13€/mes, 10 DTU
    GeneralPurposeServerless, // slide 5: tráfico intermitente, auto-pausa ≈ 0€
    GeneralPurpose,           // slide 4: control fino vCore, > 60 conexiones
    Hyperscale,               // slide 21: > 1 TB, crecimiento ilimitado
}

public static class SqlTierAdvisor
{
    // Slide 21 — Standard ≤ 1 TB; por encima toca Hyperscale.
    public const int LimiteGbHyperscale = 1024;

    // Slide 10 — S0 admite ~60 conexiones simultáneas; por encima,
    // mejor vCore (General Purpose) para control fino.
    public const int MaxConexionesS0 = 60;

    public static SqlTier Sugerir(bool intermitente, int maxConexiones, int datosGb)
    {
        if (maxConexiones < 0)
            throw new ArgumentOutOfRangeException(nameof(maxConexiones));
        if (datosGb < 0)
            throw new ArgumentOutOfRangeException(nameof(datosGb));

        // > 1 TB: ningún tier estándar llega — Hyperscale (slide 21).
        if (datosGb > LimiteGbHyperscale)
            return SqlTier.Hyperscale;

        // Tráfico esporádico (dev, staging, picos): Serverless se pausa
        // sin actividad y cuesta ≈ 0€ parado (slide 5).
        if (intermitente)
            return SqlTier.GeneralPurposeServerless;

        // Carga sostenida con muchas conexiones: S0 se queda corto en
        // su límite de pool (slide 10) — vCore General Purpose.
        if (maxConexiones > MaxConexionesS0)
            return SqlTier.GeneralPurpose;

        // BBDD diminuta de dev/test: Basic es lo más barato (slide 4).
        if (datosGb <= 2 && maxConexiones <= 5)
            return SqlTier.Basic;

        // Caso por defecto del curso: S0 (slide 4).
        return SqlTier.S0;
    }
}
