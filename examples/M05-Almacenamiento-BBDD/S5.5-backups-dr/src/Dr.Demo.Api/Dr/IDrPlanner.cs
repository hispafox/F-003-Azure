namespace Dr.Demo.Api.Dr;

public sealed record PlanLinea(
    string Servicio,
    bool RequiereConfiguracionManual,
    string Backup,
    bool PointInTime);

public sealed record PlanDr(
    string Criticidad,
    string EstrategiaRecomendada,
    PerfilDr Perfil,
    bool CumpleObjetivos,
    IReadOnlyList<PlanLinea> Servicios,
    IReadOnlyList<string> Avisos);

// Servicio inyectable (seam para DI) que compone los tres advisors puros
// en un plan de DR — slides 8, 12, 16. La lógica vive en los *Advisor;
// esto solo orquesta y es lo que se cruza en el test de contenedor.
public interface IDrPlanner
{
    PlanDr Generar(
        Criticidad criticidad,
        IReadOnlyList<ServicioAzure> servicios,
        int rpoObjetivoMin,
        int rtoObjetivoMin);
}

public sealed class DrPlanner : IDrPlanner
{
    public PlanDr Generar(
        Criticidad criticidad,
        IReadOnlyList<ServicioAzure> servicios,
        int rpoObjetivoMin,
        int rtoObjetivoMin)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        var estrategia = RpoRtoCalculator.Recomendar(criticidad);
        var perfil = RpoRtoCalculator.Perfil(estrategia);
        var cumple = RpoRtoCalculator.CumpleObjetivos(
            estrategia, rpoObjetivoMin, rtoObjetivoMin);

        var lineas = servicios.Select(s =>
        {
            var c = BackupPolicyAdvisor.Describir(s);
            return new PlanLinea(s.ToString(), c.RequiereConfiguracionManual,
                c.Retencion, c.PointInTime);
        }).ToArray();

        var avisos = new List<string>();
        // Slide 3 — lo que NO tiene backup automático.
        foreach (var l in lineas.Where(l => l.RequiereConfiguracionManual))
            avisos.Add($"{l.Servicio}: sin backup automático — configúralo tú (slide 3/6).");
        // Slide 22 — la estrategia no alcanza los objetivos del negocio.
        if (!cumple)
            avisos.Add(
                $"La estrategia {estrategia} no cumple RPO≤{rpoObjetivoMin}min / " +
                $"RTO≤{rtoObjetivoMin}min: sube de estrategia o ajusta el SLA (slide 22).");

        return new PlanDr(
            criticidad.ToString(), estrategia.ToString(), perfil, cumple, lineas, avisos);
    }
}
