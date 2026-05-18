namespace Cosmos.Demo.Api.Cosmos;

// Mismos nombres que Microsoft.Azure.Cosmos.ConsistencyLevel (slide 11).
public enum NivelConsistencia { Strong, BoundedStaleness, Session, ConsistentPrefix, Eventual }

// Slide 11 — los 5 niveles de consistencia como tabla de decisión pura.
// "Session por defecto es correcto para el 90%": esta clase lo codifica.
public static class ConsistencyAdvisor
{
    public static NivelConsistencia Recomendar(
        bool requiereSiempreUltimaEscritura, bool datosFinancieros, bool latenciaMinima)
    {
        // Inventario crítico / financiero / "todos ven lo mismo" → Strong
        // (dobla RU y añade latencia, pero el negocio lo exige).
        if (datosFinancieros || requiereSiempreUltimaEscritura)
            return NivelConsistencia.Strong;

        // Telemetría/feeds donde solo importa latencia mínima → Eventual.
        if (latenciaMinima)
            return NivelConsistencia.Eventual;

        // El 90% de los casos.
        return NivelConsistencia.Session;
    }

    // Slide 11 — Strong y Bounded Staleness cuestan ~2x RU; el resto 1x.
    public static int MultiplicadorRu(NivelConsistencia nivel) => nivel switch
    {
        NivelConsistencia.Strong or NivelConsistencia.BoundedStaleness => 2,
        _ => 1,
    };
}
