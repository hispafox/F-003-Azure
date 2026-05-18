namespace Dr.Demo.Api.Dr;

public enum ServicioAzure
{
    CosmosDb, AzureSql, BlobStorage, TableStorage,
    QueueStorage, AppService, KeyVault,
}

public sealed record CaracteristicasBackup(
    bool BackupAutomatico,
    string Retencion,
    bool PointInTime,
    bool SoftDelete,
    bool RequiereConfiguracionManual,
    string Nota);

// Slides 3, 6, 19 — qué backup trae cada servicio "de fábrica" y qué
// debes configurar tú. Tabla de decisión pura (como los *Advisor de
// S5.2–S5.4).
public static class BackupPolicyAdvisor
{
    public static CaracteristicasBackup Describir(ServicioAzure servicio) => servicio switch
    {
        ServicioAzure.CosmosDb => new(
            BackupAutomatico: true,
            Retencion: "Continuous 7/30 días (o periodic cada 4h)",
            PointInTime: true,
            SoftDelete: false,           // slide 19: beta / vía ticket
            RequiereConfiguracionManual: false,
            Nota: "Continuous backup recomendado: PITR resolución de segundos (slide 4)"),

        ServicioAzure.AzureSql => new(
            BackupAutomatico: true,
            Retencion: "7-35 días según tier (+ LTR hasta 10 años)",
            PointInTime: true,
            SoftDelete: true,            // slide 19: 7 días tras drop
            RequiereConfiguracionManual: false,
            Nota: "Backups continuos automáticos; restore = DB nueva (slide 5)"),

        ServicioAzure.BlobStorage => new(
            BackupAutomatico: false,
            Retencion: "Soft delete + versioning (configurable 1-365 días)",
            PointInTime: true,           // con versionado
            SoftDelete: true,            // slide 6/19: 7 días default
            RequiereConfiguracionManual: true,
            Nota: "Activar soft delete + versioning + GRS/GZRS (slide 6)"),

        ServicioAzure.TableStorage => new(
            BackupAutomatico: false,
            Retencion: "—",
            PointInTime: false,
            SoftDelete: false,
            RequiereConfiguracionManual: true,
            Nota: "Sin backup automático: exportar tú (slide 3)"),

        ServicioAzure.QueueStorage => new(
            BackupAutomatico: false,
            Retencion: "—",
            PointInTime: false,
            SoftDelete: false,
            RequiereConfiguracionManual: true,
            Nota: "Mensajes efímeros por diseño: no se respaldan (slide 3)"),

        ServicioAzure.AppService => new(
            BackupAutomatico: false,
            Retencion: "Configurable (no por defecto, requiere Standard+)",
            PointInTime: false,          // restore de snapshot, no PITR
            SoftDelete: false,
            RequiereConfiguracionManual: true,
            Nota: "Stateless: el DR es redesplegar desde pipeline + IaC (slide 8)"),

        ServicioAzure.KeyVault => new(
            BackupAutomatico: true,
            Retencion: "Soft delete 7-90 días + purge protection",
            PointInTime: false,
            SoftDelete: true,            // slide 19: 90 días
            RequiereConfiguracionManual: false,
            Nota: "Geo-redundante built-in; purge protection ON en prod (slide 19)"),

        _ => throw new ArgumentOutOfRangeException(nameof(servicio)),
    };

    public static bool RequiereConfiguracionManual(ServicioAzure servicio)
        => Describir(servicio).RequiereConfiguracionManual;
}
