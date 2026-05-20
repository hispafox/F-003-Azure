namespace PracticaMsix.Demo.Api.Practica;

// Slides 4-11 — los 8 pasos guiados de la práctica.
public enum PasoPractica
{
    CrearSolucion,           // paso 1, slide 4
    PersonalizarApp,         // paso 2, slide 5
    ConfigurarManifest,      // paso 3, slide 6
    GenerarCertificado,      // paso 4, slide 7
    BuildMsix,               // paso 5, slide 8
    InstalarPaquete,         // paso 6, slide 9
    SimularActualizacion,    // paso 7, slide 10
    ConfigurarAppInstaller,  // paso 8, slide 11 (reto)
}

public sealed record PasoInfo(
    int Numero, PasoPractica Paso, string Descripcion,
    IReadOnlyList<string> CriteriosValidacion);

// Slides 4-11 + 15 — máquina de estados de la práctica. Cada paso
// expone sus criterios de validación; se avanza solo si TODOS pasan
// (mismo modelo que MigrationRoadmap de S7.7).
public static class PracticaSteps
{
    public static IReadOnlyList<PasoInfo> Pasos { get; } =
    [
        new(1, PasoPractica.CrearSolucion, "Crear solución + WPF + Packaging Project (slide 4)",
        [
            "dotnet new sln + wpf creados",
            "Packaging Project añadido con referencia al WPF",
            "Packaging marcado como Startup Project",
        ]),
        new(2, PasoPractica.PersonalizarApp, "Personalizar la app: título, versión visible (slide 5)",
        [
            "MainWindow muestra Package.Current.Id.Version",
            "Compila en modo Release | x64",
        ]),
        new(3, PasoPractica.ConfigurarManifest, "Configurar Package.appxmanifest (slide 6)",
        [
            "Identity.Name = Empresa.AppName",
            "Publisher con prefijo CN= (slide 6/7)",
            "Capabilities: internetClient + runFullTrust (rescap)",
            "Visual assets generados (iconos)",
        ]),
        new(4, PasoPractica.GenerarCertificado, "Crear certificado self-signed (slide 7)",
        [
            "New-SelfSignedCertificate con KeyUsage DigitalSignature",
            "Subject del cert COINCIDE con Publisher del manifest",
            "Cert exportado a .cer para distribuirlo a TrustedPeople",
        ]),
        new(5, PasoPractica.BuildMsix, "Build del .msix firmado (slide 8)",
        [
            "Configuration Release / Platform x64",
            "Publish → Create App Packages → Sideloading",
            "Archivo {Empresa.App}_{Version}_x64.msix generado",
            "Firma verificada (Get-AuthenticodeSignature)",
        ]),
        new(6, PasoPractica.InstalarPaquete, "Instalar en el PC del alumno (slide 9)",
        [
            "Certificado importado a Cert:\\LocalMachine\\TrustedPeople",
            "Add-AppxPackage instala sin warnings",
            "App aparece en Start Menu",
            "App arranca y muestra la versión correcta",
        ]),
        new(7, PasoPractica.SimularActualizacion, "Generar v1.0.1.0 y reinstalar (slide 10)",
        [
            "Cambio visible en MainWindow (color, texto)",
            "Version incrementada a 1.0.1.0 en el manifest",
            "Build genera el nuevo .msix",
            "Add-AppxPackage actualiza in-place (datos del usuario se mantienen)",
        ]),
        new(8, PasoPractica.ConfigurarAppInstaller, "AppInstaller con auto-update (slide 11 — reto)",
        [
            ".appinstaller XML válido con MainPackage + UpdateSettings",
            "Versión del .appinstaller coincide con la del MSIX",
            "Apertura del .appinstaller instala vía AppInstaller dialog",
        ]),
    ];

    public static PasoInfo Info(PasoPractica paso) =>
        Pasos.FirstOrDefault(p => p.Paso == paso)
            ?? throw new ArgumentOutOfRangeException(nameof(paso));

    public static PasoPractica? SiguientePaso(
        PasoPractica actual, IReadOnlyList<bool> criteriosOk)
    {
        ArgumentNullException.ThrowIfNull(criteriosOk);
        var info = Info(actual);

        if (criteriosOk.Count != info.CriteriosValidacion.Count)
            throw new ArgumentException(
                $"Se esperaban {info.CriteriosValidacion.Count} criterios para el paso {actual}, " +
                $"recibidos {criteriosOk.Count}.", nameof(criteriosOk));

        if (!criteriosOk.All(x => x)) return null;     // no avanza

        int idx = Pasos.ToList().FindIndex(p => p.Paso == actual);
        return idx + 1 < Pasos.Count ? Pasos[idx + 1].Paso : null;
    }
}
