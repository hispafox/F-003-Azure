namespace Distribution.Demo.Api.Distribution;

// Slide 12 — los tres caminos de migración.
public enum EscenarioMigracion
{
    A_EmpaquetarSinReescribir,    // .NET Framework + MSIX Packaging Tool
    B_DotNet8MasMsix,             // modernizar + empaquetar
    C_AppNuevaDirectaMsix,        // proyecto nuevo con WAP desde inicio
}

public sealed record DecisionMigracion(
    bool Recomendado, IReadOnlyList<string> Razones);

// Slides 12 y 18 — decisión "¿migrar ClickOnce → MSIX?" y escenario
// de migración. Tablas de decisión puras citando las slides.
public static class MigrationDecisionAdvisor
{
    // Slide 18 — factores que empujan a migrar YA y factores que
    // justifican esperar.
    public static DecisionMigracion DebeMigrar(
        bool intunePlaneado,
        bool dotNet8Planeado,
        bool certAuthenticodeExpira,
        bool problemasActualizacion,
        bool clickOnceFuncionaBien,
        bool equipoSinBandwidth)
    {
        var aFavor = new List<string>();
        if (intunePlaneado) aFavor.Add("Intune/MDM planeado: ClickOnce no se integra (slide 18).");
        if (dotNet8Planeado) aFavor.Add("Migración a .NET 8+ planeada: ClickOnce solo soporta .NET Framework (slide 3/11).");
        if (certAuthenticodeExpira) aFavor.Add("El certificado Authenticode caduca: aprovechar para mover a MSIX signing (slide 8/18).");
        if (problemasActualizacion) aFavor.Add("Problemas recurrentes de actualización en ClickOnce (slide 18).");

        var enContra = new List<string>();
        if (clickOnceFuncionaBien && !problemasActualizacion)
            enContra.Add("ClickOnce funciona sin problemas: la urgencia es menor (slide 18).");
        if (equipoSinBandwidth) enContra.Add("Equipo sin bandwidth para la migración ahora (slide 18).");

        bool recomendado = aFavor.Count > enContra.Count;
        var razones = recomendado ? aFavor
            : enContra.Count > 0 ? enContra
            : aFavor.Count > 0 ? aFavor
            : ["Sin señales fuertes: empezad por apps nuevas en MSIX y migrad las existentes con calma (slide 18)."];
        return new DecisionMigracion(recomendado, razones);
    }

    // Slide 12 — qué camino seguir.
    public static EscenarioMigracion RecomendarEscenario(
        bool esAppNueva, bool sobreDotNetFramework, bool tieneTiempoEquipo)
    {
        if (esAppNueva) return EscenarioMigracion.C_AppNuevaDirectaMsix;
        if (sobreDotNetFramework && tieneTiempoEquipo)
            return EscenarioMigracion.B_DotNet8MasMsix;
        return EscenarioMigracion.A_EmpaquetarSinReescribir;
    }
}
