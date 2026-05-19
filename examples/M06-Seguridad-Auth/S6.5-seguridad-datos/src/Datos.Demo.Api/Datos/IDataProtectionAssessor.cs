namespace Datos.Demo.Api.Datos;

// Slide 14 — el checklist de seguridad de datos del equipo.
public sealed record ChecklistDatos(
    bool HttpsForzado,
    string TlsMinimo,                 // "1.2", "TLS1_2", ...
    string SqlConnectionString,
    string StorageConnectionString,
    bool TdeHabilitado,
    Sensibilidad SensibilidadMaxima,
    bool RegulacionExigeClaves,
    IReadOnlyList<string> CorsOrigenes,
    bool CorsAllowCredentials);

public sealed record AssessmentDatos(
    int Puntuacion,
    int Cumplidos,
    int Total,
    EstrategiaCifrado CifradoRecomendado,
    IReadOnlyList<string> Hallazgos);

// Slide 14 — evalúa el checklist de seguridad de datos componiendo los
// validadores puros. Servicio inyectable (seam para el test de
// contenedor).
public interface IDataProtectionAssessor
{
    AssessmentDatos Evaluar(ChecklistDatos c);
}

public sealed class DataProtectionAssessor : IDataProtectionAssessor
{
    public AssessmentDatos Evaluar(ChecklistDatos c)
    {
        ArgumentNullException.ThrowIfNull(c);

        var cors = CorsPolicyValidator.Validar(c.CorsOrigenes, c.CorsAllowCredentials);
        var cifrado = EncryptionAdvisor.Recomendar(
            c.SensibilidadMaxima, c.RegulacionExigeClaves);

        var items = new (bool ok, string nombre)[]
        {
            (c.HttpsForzado, "HTTPS forzado (Web Apps / Functions)"),
            (TlsTransitValidator.VersionPermitida(c.TlsMinimo), "TLS 1.2 mínimo"),
            (TlsTransitValidator.SqlCifradoEnTransito(c.SqlConnectionString),
                "SQL connection string con Encrypt=true"),
            (TlsTransitValidator.StorageCifradoEnTransito(c.StorageConnectionString),
                "Storage por HTTPS (DefaultEndpointsProtocol=https)"),
            (c.TdeHabilitado, "TDE habilitado en Azure SQL"),
            (EncryptionAdvisor.AtRestSiempreActivo, "Cifrado at-rest (AES-256, por defecto)"),
            (cors.Segura, "CORS con orígenes explícitos (sin wildcard+credenciales)"),
        };

        var total = items.Length;
        var cumplidos = items.Count(i => i.ok);
        var puntuacion = (int)Math.Round(100.0 * cumplidos / total);

        var hallazgos = items.Where(i => !i.ok).Select(i => $"Falta: {i.nombre}")
            .Concat(cors.Problemas.Select(p => $"CORS: {p}"))
            .ToArray();

        return new AssessmentDatos(
            puntuacion, cumplidos, total, cifrado.Estrategia, hallazgos);
    }
}
