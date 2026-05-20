namespace ClaudeCode.Limites.Demo.Api.Limites;

public enum TipoTareaIa
{
    Boilerplate,                    // controllers, DTOs, tests
    TransformacionDatos,            // SQL→modelo, JSON→DTO
    InfrastructureAsCode,           // Bicep, YAML, Dockerfile
    DocumentacionDesdeCodigo,
    AnalisisErroresConLogs,
    RefactoringMecanico,            // renames, formatting

    LogicaNegocioCompleja,          // dominio específico
    DecisionArquitectura,           // big picture
    OptimizacionFinaRendimiento,    // necesita medir
    SeguridadCritica,               // requiere expertise
    DebuggingRaceConditions,        // timing-dependent

    Otro,
}

public enum ImpactoIa { Acelera, Frena, Neutro }

public sealed record ClasificacionTarea(
    TipoTareaIa Tipo, ImpactoIa Impacto, string Slide, IReadOnlyList<string> Razones);

// Slide 5 — clasificador "acelera vs frena" según el tipo de tarea.
// Lógica pura. Si la tarea es de boilerplate / transformación / IaC /
// docs / análisis errores / refactor mecánico → Acelera. Si es
// lógica de negocio compleja, arquitectura, perf tuning fino,
// seguridad o race conditions → Frena (o genera resultado malo). Para
// Otro, devuelve Neutro con sugerencia de evaluar caso por caso.
public static class AceleraOFrenaClassifier
{
    public static ClasificacionTarea Clasificar(TipoTareaIa tipo)
    {
        return tipo switch
        {
            TipoTareaIa.Boilerplate => new(tipo, ImpactoIa.Acelera, "5",
                ["Boilerplate (controllers, DTOs, tests) ahorra 60-80% de tiempo (slide 5).",
                 "IA genera y humano revisa: ratio típico 5-7x velocidad."]),

            TipoTareaIa.TransformacionDatos => new(tipo, ImpactoIa.Acelera, "5",
                ["SQL→modelo / JSON→DTO / mappings: gran ganancia con prompt bien escrito.",
                 "Adecuado para `claude -p` (one-shot, slide 12 de S9.1)."]),

            TipoTareaIa.InfrastructureAsCode => new(tipo, ImpactoIa.Acelera, "5",
                ["Bicep + YAML pipelines + Dockerfile: ver S9.3 con sus prompts canónicos.",
                 "Valida con `bicep build` / `az deployment what-if` antes de aplicar."]),

            TipoTareaIa.DocumentacionDesdeCodigo => new(tipo, ImpactoIa.Acelera, "5",
                ["README + architecture.md + ADR generados desde el código fuente.",
                 "Pídele a Claude que lea el código, no inventa lo que no existe (slide 4)."]),

            TipoTareaIa.AnalisisErroresConLogs => new(tipo, ImpactoIa.Acelera, "5",
                ["Análisis de stack traces + correlación con logs: muy fuerte con MCP a App Insights.",
                 "Modo Pipe (`cat logs | claude -p ...`) cubre el caso (slide 12 de S9.1)."]),

            TipoTareaIa.RefactoringMecanico => new(tipo, ImpactoIa.Acelera, "5",
                ["Renames, formatting, sustituir patrón A por B en N archivos.",
                 "Subagent `code-reviewer` posterior valida que no haya regresiones."]),

            TipoTareaIa.LogicaNegocioCompleja => new(tipo, ImpactoIa.Frena, "5",
                ["Lógica del dominio específica del producto → IA inventa o sobre-generaliza.",
                 "Tú decides el modelo y los invariantes; IA implementa los métodos `for free`."]),

            TipoTareaIa.DecisionArquitectura => new(tipo, ImpactoIa.Frena, "5",
                ["IA propone opciones pero NO debe decidir por ti (slide 5).",
                 "Usa Extended Thinking + Plan Mode para que enumere trade-offs."]),

            TipoTareaIa.OptimizacionFinaRendimiento => new(tipo, ImpactoIa.Frena, "5",
                ["Optimización fina necesita MEDIR antes (slide 5).",
                 "Pásale métricas reales (P95/P99/RU) al prompt — sin medir es adivinanza."]),

            TipoTareaIa.SeguridadCritica => new(tipo, ImpactoIa.Frena, "5",
                ["IA puede generar código inseguro si no pides seguridad explícitamente.",
                 "Pide hardening + threat model en el prompt; ejecuta security review humana."]),

            TipoTareaIa.DebuggingRaceConditions => new(tipo, ImpactoIa.Frena, "5",
                ["Race conditions y timing-dependent bugs son difíciles incluso para humanos.",
                 "Combina IA (lee el código, sugiere hipótesis) con repro determinístico."]),

            _ => new(tipo, ImpactoIa.Neutro, "5",
                ["Tipo de tarea no clasificada — evalúa caso por caso.",
                 "Empieza con `claude -p` corto: si itera < 3 veces vale la pena."]),
        };
    }
}
