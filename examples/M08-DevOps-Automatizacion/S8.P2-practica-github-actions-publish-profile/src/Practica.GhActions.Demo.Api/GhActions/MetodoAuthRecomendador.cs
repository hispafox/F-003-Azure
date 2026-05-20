namespace Practica.GhActions.Demo.Api.GhActions;

public enum MetodoAuth { PublishProfile, Oidc, EnvironmentSecret }

public sealed record RecomendacionAuth(
    MetodoAuth Metodo,
    IReadOnlyList<string> Razones,
    IReadOnlyList<string> Riesgos);

public sealed record EscenarioAuth(
    bool SideProjectPersonal = true,
    bool ControlaEntraId = false,
    bool MultiEnvironment = false,
    bool AuditoriaRequerida = false,
    bool EquipoGrande = false,
    bool ProyectoEnProduccion = false);

// Slide 13/18 — recomendador Publish Profile vs OIDC vs
// Environment Secret. Lógica pura: no hay "una respuesta correcta",
// pero hay una serie de heurísticas que predicen qué eligen los
// equipos según contexto.
public static class MetodoAuthRecomendador
{
    public static RecomendacionAuth Recomendar(EscenarioAuth e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // 1) Si hay requisitos serios (auditoría, multi-env, prod, equipo
        //    grande) y el alumno controla Entra ID → OIDC sin dudar.
        if (e.ControlaEntraId &&
            (e.AuditoriaRequerida || e.MultiEnvironment ||
             e.EquipoGrande || e.ProyectoEnProduccion))
        {
            return new RecomendacionAuth(
                Metodo: MetodoAuth.Oidc,
                Razones:
                [
                    "Producción / multi-env / equipo grande → OIDC con Federated " +
                    "Credentials (slide 13).",
                    "Tokens vivos minutos (no passwords longevas).",
                    "Microsoft Entra audita cada autenticación.",
                    "Sin nada que rotar manualmente.",
                ],
                Riesgos:
                [
                    "Setup ~30-60 min: App Registration + Federated Credential + " +
                    "grants RBAC.",
                    "Requiere permisos para crear App Registration en Entra.",
                ]);
        }

        // 2) Side-project / aprendizaje / no controla Entra → Publish Profile.
        if (e.SideProjectPersonal || !e.ControlaEntraId)
        {
            return new RecomendacionAuth(
                Metodo: MetodoAuth.PublishProfile,
                Razones:
                [
                    "Side-project o no controlas Entra ID → Publish Profile (slide 13).",
                    "Setup en 5 minutos: descargar XML + crear secret.",
                    "Es una password longeva: rota cada 90 días (slide 18).",
                    "Cuando crezcas, sustituyes solo el step de auth.",
                ],
                Riesgos:
                [
                    "El secret es una password de vida larga: si se filtra, acceso " +
                    "permanente hasta rotar (slide 13/17).",
                    "Limitada auditoría: no se sabe qué pipeline lo usó.",
                ]);
        }

        // 3) Caso intermedio: usar Environment + secret + reviewers.
        return new RecomendacionAuth(
            Metodo: MetodoAuth.EnvironmentSecret,
            Razones:
            [
                "Producción sin Entra controlado → Publish Profile + GitHub " +
                "Environment con required reviewers (slide 18).",
                "El Environment añade aprobación manual y branch policy.",
                "Sigue siendo password longeva, pero con gating humano.",
            ],
            Riesgos:
            [
                "Sigue siendo password en GitHub Secrets: rotar cada 90 días.",
                "Migrar a OIDC cuando el equipo controle Entra ID.",
            ]);
    }
}
