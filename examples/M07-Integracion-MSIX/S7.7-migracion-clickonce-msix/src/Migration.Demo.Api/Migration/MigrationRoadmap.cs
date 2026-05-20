namespace Migration.Demo.Api.Migration;

// Slide 2/11 — fases de la migración por orden.
public enum FaseMigracion
{
    Empaquetado,            // semana 1-2: WAP + MSIX firmado en test
    Piloto,                 // semana 3:   distribuir a 5-10 personas
    RolloutCompleto,        // semana 4-6: todos los usuarios + CI/CD
    ModernizarDotNet8,      // opcional, después
}

public sealed record FaseInfo(
    FaseMigracion Fase, string DuracionEstimada,
    IReadOnlyList<string> CriteriosSalida);

// Slides 2 y 11 — el roadmap como máquina de fases con criterios de
// salida testeables. Solo se avanza si TODOS los criterios pasan.
public static class MigrationRoadmap
{
    public static IReadOnlyDictionary<FaseMigracion, FaseInfo> Fases { get; } =
        new Dictionary<FaseMigracion, FaseInfo>
        {
            [FaseMigracion.Empaquetado] = new(
                FaseMigracion.Empaquetado, "1-2 semanas",
                [
                    "Packaging project (WAP) creado y compila",
                    "Package.appxmanifest válido (Identity Name + CN= + Version 4 partes)",
                    "Iconos de todos los tamaños generados",
                    "MSIX firmado con cert (self-signed o Enterprise CA)",
                    "Instala y desinstala limpiamente en PC de test",
                ]),
            [FaseMigracion.Piloto] = new(
                FaseMigracion.Piloto, "1 semana",
                [
                    "AppInstaller configurado con auto-update",
                    "Subido a Azure Blob Storage",
                    "Grupo piloto (5-10 personas) instalado",
                    "Sin tickets de soporte críticos durante 48 h",
                    "Migración de datos del usuario (ClickOnce→MSIX) funciona",
                ]),
            [FaseMigracion.RolloutCompleto] = new(
                FaseMigracion.RolloutCompleto, "2-3 semanas",
                [
                    "Pipeline CI/CD publica MSIX automáticamente",
                    "Comunicación a usuarios enviada (slide 16)",
                    "Staged rollout 5% → 25% → 50% → 100% sin regresiones",
                    "Health checks post-update OK en ≥ 95% de instalaciones",
                    "ClickOnce file share marcado read-only (transición)",
                ]),
            [FaseMigracion.ModernizarDotNet8] = new(
                FaseMigracion.ModernizarDotNet8, "opcional",
                [
                    "App migrada a .NET 8+ (dotnet-upgrade-assistant)",
                    "Single-file + self-contained build",
                    "Soporte multi-arch x64 + ARM64 en .msixbundle",
                ]),
        };

    public static FaseInfo Info(FaseMigracion fase) =>
        Fases.TryGetValue(fase, out var info)
            ? info
            : throw new ArgumentOutOfRangeException(nameof(fase));

    // Solo avanza si TODOS los criterios de salida están OK.
    public static FaseMigracion? SiguienteFase(
        FaseMigracion actual, IReadOnlyList<bool> criteriosOk)
    {
        ArgumentNullException.ThrowIfNull(criteriosOk);
        var info = Info(actual);

        if (criteriosOk.Count != info.CriteriosSalida.Count)
            throw new ArgumentException(
                $"Se esperaban {info.CriteriosSalida.Count} criterios para la fase {actual}, " +
                $"recibidos {criteriosOk.Count}.", nameof(criteriosOk));

        if (!criteriosOk.All(x => x)) return null;     // no avanza

        return actual switch
        {
            FaseMigracion.Empaquetado => FaseMigracion.Piloto,
            FaseMigracion.Piloto => FaseMigracion.RolloutCompleto,
            FaseMigracion.RolloutCompleto => FaseMigracion.ModernizarDotNet8,
            FaseMigracion.ModernizarDotNet8 => null,
            _ => throw new ArgumentOutOfRangeException(nameof(actual)),
        };
    }
}
