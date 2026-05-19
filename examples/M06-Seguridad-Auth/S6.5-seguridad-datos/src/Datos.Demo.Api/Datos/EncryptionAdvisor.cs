namespace Datos.Demo.Api.Datos;

public enum Sensibilidad { Normal, Confidencial, AltamenteConfidencial }

public enum EstrategiaCifrado
{
    MmkAtRest,          // AES-256 Microsoft-managed (por defecto, slide 6)
    CmkAtRest,          // Customer-managed keys en Key Vault (slide 7)
    AlwaysEncrypted,    // cifrado client-side; ni Azure lo lee (slide 9)
}

public sealed record RecomendacionCifrado(
    EstrategiaCifrado Estrategia,
    bool CifradoAtRestPorDefecto,   // siempre true en Azure (slide 6)
    string Nota);

// Slides 6-9 — qué cifrado at-rest aplicar según sensibilidad del dato
// y si la regulación exige controlar las claves. Tabla de decisión pura.
public static class EncryptionAdvisor
{
    public static RecomendacionCifrado Recomendar(
        Sensibilidad sensibilidad, bool regulacionExigeControlarClaves)
    {
        // Slide 9 — datos ultra-sensibles (tarjeta, SSN): Always
        // Encrypted, cifrado incluso dentro de SQL Server.
        if (sensibilidad == Sensibilidad.AltamenteConfidencial)
            return new(EstrategiaCifrado.AlwaysEncrypted, true,
                "Always Encrypted: ni Azure descifra; sin WHERE/ORDER BY salvo equality determinista (slide 9)");

        // Slide 7 — regulación (banca/sanidad/gov) → Customer-Managed Keys.
        if (regulacionExigeControlarClaves)
            return new(EstrategiaCifrado.CmkAtRest, true,
                "CMK en Key Vault con purge protection (slide 7); requiere MI del recurso");

        // Slide 6 — el 90%: AES-256 Microsoft-managed, ON por defecto.
        return new(EstrategiaCifrado.MmkAtRest, true,
            "AES-256 Microsoft-managed: ON por defecto, sin configurar nada (slide 6)");
    }

    // Slide 6 — TODOS los servicios Azure cifran at-rest por defecto.
    public const bool AtRestSiempreActivo = true;
}
