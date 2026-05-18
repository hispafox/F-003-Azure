namespace Dr.Demo.Api.Dr;

public enum EstrategiaDr { ActiveActive, WarmStandby, ColdStandby }

// Slide 24 — criticidad del negocio.
public enum Criticidad { MisionCritica, Importante, Interno }

public sealed record PerfilDr(
    string Rpo, string Rto, int RpoMaxMinutos, int RtoMaxMinutos,
    double MultiplicadorCoste);

// Slides 8, 14, 22, 24 — RPO (cuántos datos pierdes) y RTO (cuánto estás
// caído) por estrategia, y qué estrategia toca según criticidad. Pura.
public static class RpoRtoCalculator
{
    public static PerfilDr Perfil(EstrategiaDr estrategia) => estrategia switch
    {
        // Cosmos multi-write: RPO < 1s, RTO < 1min. Coste ~2x base.
        EstrategiaDr.ActiveActive => new("< 1 s", "< 1 min", 1, 1, 2.0),
        // Secundario reducido; escalar al failover. Coste ~1.3-1.5x.
        EstrategiaDr.WarmStandby => new("5-15 min", "15-60 min", 15, 60, 1.4),
        // Infra apagada; provisionar + restore. Coste ~1.05-1.1x.
        EstrategiaDr.ColdStandby => new("horas", "4-24 h", 720, 1440, 1.05),
        _ => throw new ArgumentOutOfRangeException(nameof(estrategia)),
    };

    // Slide 24 — la estrategia que recomienda la criticidad.
    public static EstrategiaDr Recomendar(Criticidad criticidad) => criticidad switch
    {
        Criticidad.MisionCritica => EstrategiaDr.ActiveActive,
        Criticidad.Importante => EstrategiaDr.WarmStandby,
        Criticidad.Interno => EstrategiaDr.ColdStandby,
        _ => throw new ArgumentOutOfRangeException(nameof(criticidad)),
    };

    // Slide 22 — ¿la estrategia cumple los objetivos del negocio?
    public static bool CumpleObjetivos(
        EstrategiaDr estrategia, int rpoObjetivoMin, int rtoObjetivoMin)
    {
        if (rpoObjetivoMin < 0 || rtoObjetivoMin < 0)
            throw new ArgumentOutOfRangeException(nameof(rpoObjetivoMin));
        var p = Perfil(estrategia);
        return p.RpoMaxMinutos <= rpoObjetivoMin && p.RtoMaxMinutos <= rtoObjetivoMin;
    }
}
