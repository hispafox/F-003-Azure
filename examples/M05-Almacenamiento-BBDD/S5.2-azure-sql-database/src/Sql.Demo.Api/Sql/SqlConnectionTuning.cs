using Microsoft.Data.SqlClient;

namespace Sql.Demo.Api.Sql;

// Slide 10 — connection pooling: el error más caro es no reutilizar
// conexiones. EF Core ya gestiona el pool, pero el tamaño y el cifrado
// se configuran en la connection string. Esta clase pura toma la cadena
// base del alumno y le aplica/normaliza los ajustes correctos (slides
// 6, 10) sin que tenga que recordar la sintaxis. También detecta si usa
// Managed Identity (para el checklist de producción, slide 20).
public static class SqlConnectionTuning
{
    // Slide 10 — límites de pool sensatos para una app web.
    public const int MaxPoolSize = 100;
    public const int MinPoolSize = 5;
    public const int ConnectionLifetimeSeg = 300;

    // Devuelve la cadena con pooling + cifrado forzados (slides 6, 10).
    // Idempotente: re-afinar una cadena ya afinada no la cambia.
    public static string Afinar(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var b = new SqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = MaxPoolSize,
            MinPoolSize = MinPoolSize,
            Pooling = true,
            ConnectTimeout = 30,
        };

        // Slide 6 — Encrypt obligatorio contra Azure SQL. Solo lo forzamos
        // si el alumno no lo ha desactivado explícitamente (Testcontainers
        // usa un certificado self-signed → TrustServerCertificate).
        if (!connectionString.Contains("Encrypt", StringComparison.OrdinalIgnoreCase))
            b.Encrypt = true;

        b.ConnectRetryCount = 3;
        return b.ConnectionString;
    }

    // Slide 6/20 — ¿la cadena usa Entra ID / Managed Identity (sin
    // password) o autenticación SQL clásica? El checklist de producción
    // exige lo primero.
    public static bool UsaManagedIdentity(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var b = new SqlConnectionStringBuilder(connectionString);
        return b.Authentication is
                SqlAuthenticationMethod.ActiveDirectoryDefault or
                SqlAuthenticationMethod.ActiveDirectoryManagedIdentity or
                SqlAuthenticationMethod.ActiveDirectoryInteractive
            && string.IsNullOrEmpty(b.Password);
    }
}
