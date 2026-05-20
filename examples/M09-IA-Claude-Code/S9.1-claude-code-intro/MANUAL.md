# Manual del alumno — S9.1 · Claude Code: introducción y setup

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue (en este caso, el `.claude/` que entregas en git). Este manual va antes: te cuenta por qué Claude Code no es Copilot pero tampoco lo reemplaza, qué modo de ejecución sirve para qué tarea concreta del día a día, y cuál es el `settings.json` que evita que tu primer experimento con un agente acabe en un `rm -rf` lleno de arrepentimiento.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.1](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.1-claude-code-intro-v3.md). Tres piezas de lógica pura (recomendador de modo + features, comparativa Claude Code vs Copilot, builder del `settings.json` del equipo) más un planificador con checklist de onboarding.

*Creado: 2026-05-20 22:55 +0200*

---

## 1. La idea en una frase

Claude Code es **un agente en la terminal con acceso al filesystem, a bash y a servidores MCP**. Eso lo hace una bestia distinta a Copilot: no completa la siguiente línea mientras tecleas; abre tu proyecto entero, lee diez archivos, ejecuta los tests, modifica otros tres, vuelve a ejecutar tests, y te devuelve un diff trabajado durante 20 minutos sin que estés delante. Esto cambia el modelo mental: dejas de pensar en "autocompletado" y empiezas a pensar en "tareas que delegas". Y como cualquier delegación seria, exige que firmes un contrato (el `settings.json` del equipo) antes de dejar entrar al agente a tu casa.

El submódulo entrena dos decisiones que el alumno va a tomar todos los días: **qué modo de ejecución usar para qué tarea** (interactivo cuando dudas, one-shot cuando sabes lo que quieres, pipe para mascar logs, headless en CI/CD) y **cómo configurar el `.claude/` del equipo** para que la potencia no se convierta en accidente. Más una pregunta de fondo: Claude Code vs Copilot, ¿cuál? Spoiler: casi siempre los dos.

---

## 2. El problema real que hay detrás

Tres situaciones que verás repetidas en cualquier equipo que adopta Claude Code:

**Caso 1 — el `rm -rf` del primer día.** Un developer instala Claude Code, le pide "limpia los archivos basura del proyecto", y el agente (sin `settings.json`, sin hooks, sin nada) interpreta "basura" como toda la carpeta `bin/obj` más, por arrastre, un directorio `data/` con datos de prueba que él aún no había committeado. `rm -rf` ejecutado. Tres horas reconstruyendo. **El hook `PreToolUse(Bash)` que bloquea comandos destructivos es la diferencia** entre experimentar con un agente y experimentar con un cuchillo de cocina sin funda.

**Caso 2 — los secretos en el contexto del agente.** Otro equipo arrancó sin `excludePatterns`. El agente, leyendo el repo entero para "entender el proyecto", se topó con un `local.settings.json` que un developer había olvidado en una rama. El contenido de la connection string entró en el contexto de la conversación; la conversación, según política de la empresa, no debe contener datos clasificados como "Confidencial+". Incidente menor, conversación con seguridad mayor. **Excluir `*.env`, `*.pfx`, `local.settings.json` y `.secrets/*` desde el día uno** no es paranoia; es el contrato base.

**Caso 3 — "¿Copilot o Claude Code?" eternamente sin resolver.** Tercer equipo: el debate se prolonga durante semanas en Slack. Unos defienden Copilot (autocompletado de toda la vida, suscripción fija, sin que nadie tenga que aprender a "prompting"). Otros defienden Claude Code (contexto multi-archivo, refactor cross-cutting, IaC generado en minutos). El que mira las dos cosas con calma se da cuenta de que **no compiten**: Copilot autocompleta mientras tecleas; Claude Code hace las tareas de 20-60 minutos que ni intentarías escribir línea a línea. La respuesta es "los dos" en el 80% de los casos, y la heurística del ejemplo te lleva ahí en una llamada al endpoint.

Los tres casos los aborda el ejemplo. `ProjectConfigBuilder` te da el `settings.json` con hooks y exclude patterns desde el primer commit. `ToolComparison` zanja el debate Copilot vs Claude Code con criterios objetivos. Y `FeatureRecommender` te dice, para una tarea concreta, qué modo usar y qué subagent/skill/hook activar.

---

## 3. Por qué esto importa en tu stack

Si tu equipo va a adoptar Claude Code (y si no lo está haciendo ya, lo va a hacer en los próximos meses), las tres preguntas que te van a llegar son siempre las mismas:

- **¿Qué modo uso para esta tarea?** Para análisis de logs, ¿pipe? Para changelogs, ¿one-shot? Para arquitectura, ¿interactivo con extended thinking? La heurística del `FeatureRecommender` te da la respuesta en cada caso y, lo que es más útil, te explica el porqué con la slide concreta detrás.
- **¿Claude Code reemplaza a Copilot?** No. Y la conversación honesta con el equipo es: Copilot mientras tecleas (suscripción fija, predecible), Claude Code para tareas largas (coste variable, ajustable). El presupuesto suma pero el output multiplica.
- **¿Qué le dejo tocar al agente?** `settings.json` con allowed tools mínimas (`Read`, `Glob`, `Grep`, `Edit`, `Write`; `Bash` solo si toca infra), exclude patterns para los secretos, y hooks `PreToolUse` y `PostToolUse` que convierten heurísticas en automatización determinística.

Si llegas a producción con las tres respuestas claras, el agente acelera el equipo sin sustos. Sin ellas, vas a tener un caso 1, un caso 2 o un debate eterno.

---

## 4. La analogía vertebradora: el contratista de obras y el manitas

Imagina dos formas distintas de mejorar tu casa.

La primera: **el manitas con destornillador**. Tú mismo cambias el enchufe que falla, tú mismo pintas la pared del salón, tú mismo montas la estantería del IKEA. Y en cada paso, alguien que sabe del oficio te susurra al oído: "ese tornillo va con cabeza Phillips, no Torx", "si pintas a esa hora se va a secar mal", "deja un milímetro de holgura para la balda". No hace el trabajo por ti; te ayuda a hacerlo bien. Eso es **GitHub Copilot**: autocompletado mientras tecleas, sugerencias rápidas, contexto reducido al archivo donde estás. Útil constantemente, pero la decisión y el músculo los pones tú.

La segunda: **el contratista de obras**. Cuando lo que toca es la reforma de la cocina entera, no llamas al manitas. Llamas a un contratista que viene con su equipo, mira los planos, habla contigo del alcance, levanta paredes, instala tuberías, prueba la instalación eléctrica y te entrega la cocina funcionando tres semanas después. No te ayuda a hacer el trabajo; **lo hace él**, mientras tú revisas y apruebas. Eso es **Claude Code**: tarea grande, contexto multi-archivo, ejecuta comandos por sí mismo, devuelve el resultado terminado.

La pregunta no es "¿manitas o contratista?". La pregunta es **qué obra estás haciendo**. Para cambiar el enchufe, no llames al contratista: es overhead y coste innecesario. Para reformar la cocina entera, no le pidas al manitas que lo haga línea por línea, vas a tardar tres meses y va a salir peor. La mayoría de hogares funcionan con los dos según el día.

Y como con cualquier contratista, **lo primero que firmas es el contrato**. ¿A qué partes de la casa puede entrar? ¿Cuáles están vedadas? ¿Qué herramientas usa? ¿Hay un supervisor que verifica antes de cada decisión grande? Eso es exactamente lo que hace `settings.json`: allowed tools (qué puede ejecutar el agente), exclude patterns (qué no debe ver ni tocar), hooks (los controles intermedios). Sin contrato firmado, el contratista entra y hace lo que cree que quieres: el caso 1 del `rm -rf`.

Dentro del trabajo del contratista hay además **distintas formas de contratar**:

- **Interactivo** (el contratista en tu casa todo el día): vais hablando, decides sobre la marcha "este azulejo no, prefiero el otro", se ajusta. Para diseño y arquitectura.
- **One-shot** (presupuesto cerrado): "necesito esto, así, con estas condiciones, dame el resultado". Para changelogs, refactors acotados, generación de boilerplate.
- **Pipe** (le pasas un dossier técnico y vuelve con el diagnóstico): le entregas el log de errores y te devuelve "aquí está el patrón, esto es lo que falla". Para análisis de logs.
- **Headless** (sin presencia humana, integrado en CI/CD): el contratista forma parte del pipeline de obra; ejecuta su parte sin que tú estés. Para AI code review automatizado, validaciones programadas.

Mantén la imagen: manitas vs contratista, contrato firmado antes de la obra, cuatro formas de contratar según el trabajo. Toda la mecánica del submódulo cabe en esa metáfora.

---

## 5. Recorrido por el código: las tres piezas

### `ToolComparison.Recomendar` — Copilot, Claude Code o los dos

La función central es honesta: en el 80% de los casos donde el alumno tiene señales fuertes a la vez de Claude Code (necesidad de agente, MCP, multi-archivo) y de Copilot (autocompletado en IDE), la respuesta es **`Combinacion`**. Mira el código:

```csharp
bool senalesClaudeCode = e.NecesitasAgenteQueEjecuta
    || e.NecesitasMcp
    || e.ProyectoMultiArchivo;
bool senalesCopilot = e.QuieresAutocompletadoEnIde;

if (senalesClaudeCode && senalesCopilot)
    return new RecomendacionHerramienta(HerramientaIa.Combinacion, [
        "Copilot para autocompletado mientras tecleas (slide 5).",
        "Claude Code para tareas grandes (generar módulos, IaC, debugging).",
        "No son excluyentes; el coste suma pero se compensa con productividad.",
    ]);
```

Las razones no son ideológicas, son prácticas: el manitas (Copilot) ahorra minutos cien veces al día; el contratista (Claude Code) ahorra horas en tareas concretas. Sumarlos cuesta dinero pero el saldo neto suele compensar. Si el alumno marca solo señales de Copilot (quiere autocompletado y nada más, presupuesto fijo, sin necesidad de agente), la función devuelve `GithubCopilot` solo. Si marca solo Claude Code (necesita agente, MCP o multi-archivo, sin IDE de por medio), devuelve `ClaudeCode` solo. Los tres caminos son legítimos según contexto.

La `Tabla` canónica del slide 5 está en el código como dato, no como prosa. Ocho filas: Modelo, Interfaz, Contexto, Agente, MCP, Multi-archivo, Tipo de coste, Mejor para. Útil para llevar a una reunión de equipo cuando el debate se eterniza: la tabla cierra la conversación con datos.

### `FeatureRecommender.Recomendar` — qué modo y qué features para esta tarea

La pieza más densa del ejemplo. Recibe un escenario (tipo de tarea, si es recurrente, si es compleja, si va en pipeline CI/CD, si requiere contexto aislado) y devuelve **modo + extended thinking + features complementarias (subagent, skill, hook)**. El flujo de decisión:

```csharp
ModoEjecucion modo;
if (e.EnPipelineCiCd)
    modo = ModoEjecucion.Headless;            // CI/CD
else if (e.Tarea == TipoTarea.AnalisisLogs)
    modo = ModoEjecucion.Pipe;                // cat log | claude
else if (e.Tarea == TipoTarea.ChangelogODocs)
    modo = ModoEjecucion.OneShot;             // un solo prompt
else if (e.EsCompleja || e.Tarea == TipoTarea.Arquitectura)
    modo = ModoEjecucion.Interactive;         // diálogo
else
    modo = ModoEjecucion.Interactive;         // default seguro
```

Cuatro reglas que cubren los casos típicos. **Pipeline CI/CD siempre es headless**: no hay nadie delante de la terminal a las 3 de la madrugada cuando el build se ejecuta. **Análisis de logs es pipe**, porque el flujo natural es `cat error.log | claude -p "diagnostica esto"` y obtienes el diagnóstico sin abrir conversación. **Changelogs son one-shot**: sabes exactamente qué quieres, un prompt cerrado y el resultado. **Arquitectura y refactors complejos son interactivos**, porque el valor está en el diálogo, en las preguntas que el agente te hace y en los matices que descubres mientras lo conversáis.

Y luego las features complementarias. **Extended thinking** se activa para arquitectura y para tareas complejas de refactor o debugging: son problemas donde "pensar más" antes de actuar es claramente mejor que "responder rápido y luego corregir". **Subagents** se sugieren cuando el contexto debe vivir aislado del main thread: code review, análisis de logs, debugging. El subagent recoge el ruido (logs grandes, diffs largos) y devuelve solo el resumen útil, protegiendo tu contexto principal. **Skills** se sugieren si la tarea es recurrente: si haces lo mismo cada semana, mete un skill en `.claude/skills/<nombre>/SKILL.md` y a partir de ahí lo invocas con `/<nombre>`. **Hooks PreToolUse** son obligatorios en pipelines CI/CD — son el quality gate determinístico que decide bloquear (`exit 2`) o permitir (`exit 0`) antes de cualquier ejecución.

La lógica `SubagentSugerido` y `SkillSugerido` son tablas pequeñas que mapean cada `TipoTarea` a su nombre canónico:

- `CodeReview` → `code-reviewer subagent` / `ai-code-review skill`.
- `AnalisisLogs` → `log-analyst subagent`.
- `DepurarError` → `debugger subagent`.
- `GenerarCodigo` → `new-service / new-endpoint skill`.
- `GenerarIac` → `bicep-bootstrap skill`.
- `ChangelogODocs` → `changelog-from-commits skill`.

Estos nombres no son arbitrarios; son los que verás en repositorios de Claude Code maduros. Adoptarlos como convención de equipo te ahorra discusiones de naming y te alinea con el ecosistema.

### `ProjectConfigBuilder.Construir` — el contrato firmado

La función construye el `settings.json` que el equipo entrega versionado en git. Tres bloques que merecen explicación.

**Allowed tools** (slide 13):

```csharp
var allowed = new List<string> { "Read", "Glob", "Grep", "Edit", "Write" };
if (e.TocaInfraestructura) allowed.Add("Bash");
```

La lista mínima es claramente segura: `Read` y `Grep` son lectura pura, `Glob` enumera archivos, `Edit` y `Write` modifican el filesystem (con la red de seguridad del hook `PostToolUse` que viene después). **`Bash` se añade solo si el proyecto toca infraestructura** — IaC, Docker, despliegues. Sin `Bash`, el agente nunca ejecuta comandos arbitrarios; con `Bash`, el hook `PreToolUse(Bash)` filtra los destructivos.

**Exclude patterns** (slide 11):

```csharp
public static IReadOnlyList<string> ExcludePatternsBase { get; } = [
    "*.env",
    ".secrets/*",
    "*.pfx",
    "*.key",
    "*.pem",
    "local.settings.json",
    "appsettings.*.local.json",
];
```

Los siete patrones que el agente **nunca** debe ver. Cubre los anti-patterns clásicos: archivos de entorno con credenciales, certificados, claves privadas, settings locales que suelen llevar connection strings reales. Esta lista es el mínimo defensivo; tu equipo seguramente añada algunos más específicos (datasets de testing con datos clasificados, dumps de producción, lo que sea sensible en tu contexto).

**Hooks recomendados** (slide 19):

```csharp
var hooks = new List<string> {
    "PreToolUse(Bash) → scripts/block-dangerous.sh",
    "PostToolUse(Write|Edit) → scripts/auto-format.sh",
};
if (e.CursoEnProduccion || e.RequiereCompliance)
    hooks.Add("PreToolUse(Write|Edit) → scripts/block-secrets.sh");
if (e.CursoEnProduccion)
    hooks.Add("PreToolUse(Bash → git commit) → scripts/pre-commit-validation.sh");
```

Dos hooks base que cualquier proyecto debería tener: bloquear comandos destructivos antes de ejecutarlos y auto-formatear después de cada escritura. Si el proyecto va a producción o tiene compliance, se añade un hook que detecta secretos en los diffs antes de aplicarlos (busca palabras como `password=`, `api_key=`, `token=` en el contenido nuevo). Y para producción seria, un hook pre-commit que ejecuta build + test antes de permitir un `git commit`. Todo determinístico, todo en `exit 0` o `exit 2`.

El **system prompt** que el builder genera incluye lenguaje, framework y las convenciones del equipo (async/await, ILogger, records, Managed Identity, xUnit, nombres en inglés, comentarios en español). Es el equivalente al briefing que le das al contratista antes de empezar la obra: "estas son nuestras costumbres, respétalas". Sin este prompt, el agente trabaja "estándar de internet", que casi nunca coincide con el estándar de tu casa.

---

## 6. El cambio de modelo mental

El ejemplo cuenta también, entre líneas, un cambio cultural que merece atención. Los desarrolladores hemos pasado por tres olas de IA en pocos años. Primero el **autocompletado clásico** (IntelliSense): la IDE sugiere variables y métodos basándose en sintaxis. Después **Copilot**: la IA sugiere la siguiente línea o función basándose en contexto. Ahora **Claude Code y similares**: la IA hace la tarea entera mientras tú revisas.

Cada ola exige un cambio de costumbres distinto. La autocompletación clásica no requirió nada nuevo. Copilot exigió que el developer aprendiera a leer y validar las sugerencias en lugar de aceptar a ciegas. Claude Code exige mucho más: que el developer **delegue** trabajos enteros, **lea diffs** que él no escribió, **confíe** lo justo, **verifique** mucho. Es una habilidad distinta a teclear; es más parecida a gestionar a un junior espabilado.

El submódulo, sin decirlo explícitamente, entrena esa habilidad. Cuando el `FeatureRecommender` te dice "para esta tarea, modo interactivo con extended thinking y un subagent de code-review", lo que está educando es que **no todos los trabajos se delegan igual**. Algunos exigen tu presencia (interactivo), otros una conversación cerrada (one-shot), otros confianza ciega en una salida (pipe). El alumno que internaliza esa distinción usa Claude Code de forma sostenible. El que no, acaba o desconfiando del todo o confiando demasiado — ambos extremos son malos.

---

## 7. Cómo probarlo en local

Es un ejemplo offline al 100%. Claude Code no se invoca desde el ejemplo; el ejemplo modela las decisiones que harías al usarlo.

```bash
dotnet run --project src/ClaudeCode.Intro.Demo.Api
# http://localhost:5113
```

Endpoints:

```http
### Comparativa Claude Code vs Copilot
GET http://localhost:5113/cc/comparativa
# → 8 filas: Modelo, Interfaz, Contexto, Agente, MCP, Multi-archivo, Coste, Mejor para

### ¿Qué herramienta para mi escenario?
POST http://localhost:5113/cc/recomendar
Content-Type: application/json

{
  "quieresAutocompletadoEnIde": true,
  "necesitasAgenteQueEjecuta": true,
  "proyectoMultiArchivo": true,
  "necesitasMcp": true,
  "tienesPresupuestoFijo": false
}
# → Combinacion con tres razones

### ¿Qué modo + features para análisis de logs recurrente?
POST http://localhost:5113/cc/feature
Content-Type: application/json

{
  "tarea": "AnalisisLogs",
  "esRecurrente": true,
  "esCompleja": false,
  "enPipelineCiCd": false,
  "requiereContextoAislado": false
}
# → Pipe + log-analyst subagent + skill recurrente

### Generar settings.json del equipo
POST http://localhost:5113/cc/settings
Content-Type: application/json

{
  "lenguajePrincipal": "csharp",
  "framework": "net10.0",
  "cursoEnProduccion": true,
  "requiereCompliance": false,
  "tocaInfraestructura": true
}
# → SettingsRecomendados con allowed tools, exclude patterns, 3 hooks

### Plan completo de onboarding
POST http://localhost:5113/cc/plan
# → checklist de 10 puntos
```

Los 32 tests cubren cada rama del recomendador (los 8 tipos de tarea con sus modos correspondientes, el upgrade a Pipeline CI/CD, los subagents sugeridos), las tres salidas de la comparativa (ClaudeCode / Copilot / Combinacion según señales) y los settings con todas las combinaciones del escenario del equipo.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La conversación con seguridad y compliance

El primer momento incómodo de adoptar Claude Code en un equipo serio es la conversación con seguridad. Es razonable: estás dándole a una herramienta acceso al filesystem y, si activas `Bash`, capacidad de ejecutar comandos. Tres preguntas que el equipo de seguridad te va a hacer, con la respuesta que el ejemplo deja servida:

**"¿Qué archivos puede leer el agente?"** El `excludePatterns` del `settings.json` define la lista de archivos vetados. Por defecto, el ejemplo excluye los siete patrones del bloque de seguridad: `*.env`, `.secrets/*`, `*.pfx`, `*.key`, `*.pem`, `local.settings.json`, `appsettings.*.local.json`. Tu equipo seguramente añadirá patrones específicos (dumps de producción, datasets sensibles). Esta lista se versiona en git y se revisa cuando alguien la cambia — no es configuración personal, es contrato del equipo.

**"¿Qué comandos puede ejecutar?"** Si `Bash` no está en `allowedTools`, ninguno. Si está, el hook `PreToolUse(Bash)` filtra antes. El script `block-dangerous.sh` típico bloquea `rm -rf`, `git push --force`, modificaciones a archivos críticos del sistema, llamadas a `curl` con URLs sospechosas. El hook devuelve `exit 2` y el agente recibe explícitamente "este comando está bloqueado", sin ejecutarlo.

**"¿Qué pasa si el agente filtra secretos en un commit?"** Si el equipo está en producción o tiene compliance, el hook `PreToolUse(Write|Edit) → block-secrets.sh` revisa el contenido nuevo antes de aplicarlo. Detecta patrones como `password=`, `api_key=`, `token=`, claves de Azure Storage (`AccountKey=`), connection strings con `Password=`. Si encuentra alguno, bloquea. El agente ve "este diff contiene secretos" y reformula sin ellos.

La conversación termina típicamente con un acuerdo: el equipo de seguridad audita el `settings.json` y los scripts de hooks cada trimestre; el equipo de desarrollo se compromete a no modificarlos a la ligera. Es exactamente el mismo modelo que tienen los firewalls corporativos: política versionada, revisión periódica, sin excepciones casuales.

---

## 9. La conversación con el equipo: presupuesto y métricas

Otra conversación que aparece pronto: ¿cuánto cuesta Claude Code y cómo se justifica? Aquí Copilot lleva ventaja de comunicación: suscripción fija mensual, predecible, salud en una línea del Excel. Claude Code es API usage: pagas por tokens, varía con el uso, y los meses que el equipo está cargado de refactors el coste sube.

La forma honesta de tener esa conversación es con métricas, no con anécdotas. Tres números que valen el doble:

- **Tiempo ahorrado por tarea grande**: si un refactor cross-archivo que antes te llevaba 3 horas ahora son 30 minutos de tu tiempo + 30 minutos de revisión + 5 € de tokens, has cambiado 3 horas humanas por 1 hora humana + 5 €. Saca la cuenta a un mes y verás que sale a cuenta.
- **Calidad de los outputs**: si los diffs que Claude Code propone se mergean al primer intento el 80% de las veces, es señal de que el `settings.json` y el `systemPrompt` están bien afinados. Si se mergean al cuarto intento, hay trabajo de afinado pendiente.
- **Coste por developer y mes**: en proyectos reales, varía entre 15-150 € por developer dependiendo de uso intensivo. Compáralo con su sueldo y con las horas que recupera; es razonable.

El submódulo deja la conversación de presupuesto fuera del alcance — no es lógica testeable. Pero el `ToolComparison.Recomendar` reconoce honestamente la dimensión: "el coste suma pero se compensa con productividad" es una de las razones que devuelve cuando recomienda `Combinacion`.

---

## 10. La checklist de onboarding (los 10 puntos)

`ClaudeCodePlanner.Planificar` devuelve una checklist de 10 puntos que es exactamente el ritual de arranque para un equipo:

```
[ ] Node.js 18+ instalado y `claude --version` responde (slide 3)
[ ] API key configurada con `claude auth login` o ANTHROPIC_API_KEY (slide 3)
[ ] .claude/settings.json versionado con allowed tools + exclude patterns (slide 13)
[ ] .claude/config.yml o system prompt con las convenciones del equipo (slide 6)
[ ] Excluir *.env, *.pfx, local.settings.json, .secrets/* (slide 11)
[ ] Hook PreToolUse(Bash) para bloquear comandos destructivos (slide 19)
[ ] Hook PostToolUse(Write|Edit) para auto-format (slide 19)
[ ] Subagent code-reviewer en .claude/agents/ para PRs (slide 18)
[ ] Skill deploy-staging o equivalente en .claude/skills/ (slide 20)
[ ] Pipeline step claude -p ... --no-interactive para AI code review (slide 16)
```

Los dos primeros son setup técnico (Node + auth). Los cuatro siguientes son el "contrato firmado": el `settings.json` con su system prompt y sus exclude patterns. Los cuatro últimos son la "ampliación profesional": hooks, subagent, skill, integración en pipeline. Si arrancas con los seis primeros, ya tienes uso seguro de Claude Code en local. Los cuatro últimos los implementas progresivamente conforme el equipo gana confianza con la herramienta.

Una regla operativa que el ejemplo no codifica pero merece mención: **versionar `.claude/` en git desde el día uno**. Igual que `package.json`, `appsettings.json` o cualquier configuración crítica del proyecto, el `.claude/` es contrato compartido del equipo. Si un developer tiene un `settings.json` personal con permisos más abiertos, debe ser aparte (`.claude/settings.local.json` que va en `.gitignore`); el `settings.json` del equipo es ley.

---

## 11. Anti-patterns

Cinco prácticas que evitar al arrancar con Claude Code:

**Anti-pattern 1: empezar sin `settings.json`.** El primer día funciona, el segundo aparece el caso 1 (el `rm -rf`). Versiona `.claude/settings.json` en el primer commit, incluso si está casi vacío. Allowed tools mínimas, exclude patterns base, todo lo demás se añade después.

**Anti-pattern 2: `Bash` en allowed tools "por comodidad" sin hooks.** Si el proyecto no toca infra, no añadas `Bash`. Si lo toca, añade `Bash` **y** el hook `PreToolUse(Bash)` con `block-dangerous.sh`. Los dos juntos, nunca uno sin el otro.

**Anti-pattern 3: confundir Copilot con Claude Code.** Copilot autocompleta; Claude Code delega. Pedirle a Copilot un refactor cross-archivo te frustra; pedirle a Claude Code que te corrija el typo en la variable es desperdiciar tokens. Cada herramienta su tarea.

**Anti-pattern 4: `Interactive` para todo.** El modo interactivo es cómodo porque siempre puedes "preguntar". Pero hay tareas donde otros modos son mejores: análisis de logs en `Pipe` te da el diagnóstico sin abrir conversación; changelog en `OneShot` cierra rápido; CI/CD en `Headless` evita conversaciones a las 3 de la mañana. Aprende los cuatro modos y úsalos.

**Anti-pattern 5: `settings.json` que un developer modifica sin avisar al equipo.** El `.claude/` es contrato compartido. Modificar `allowedTools`, `excludePatterns` o `hooks` exige un PR como cualquier otro cambio. Si alguien añade `WebFetch` a las allowed tools sin revisión, el equipo se entera el día del incidente.

---

## 12. Glosario breve

- **Claude Code**: CLI de Anthropic que ejecuta a Claude como agente en tu terminal con acceso al filesystem, bash y MCP.
- **Modo Interactive**: `claude` sin argumentos abre conversación. Vais hablando, decides sobre la marcha.
- **Modo OneShot**: `claude -p "prompt"` envía un único prompt y devuelve la respuesta. Sin conversación.
- **Modo Pipe**: `cat fichero | claude -p "prompt"` envía el contenido por stdin junto al prompt. Para análisis de logs, parsear, transformar.
- **Modo Headless**: `claude --no-interactive ...` sin interacción humana. Para CI/CD.
- **Extended thinking**: feature que permite a Claude "pensar" más antes de responder. Útil en arquitectura y refactors complejos.
- **Subagent**: agente con contexto aislado del main thread. Recoge ruido (logs, diffs) y devuelve resumen.
- **Skill**: workflow recurrente declarado en `.claude/skills/<nombre>/SKILL.md`. Se invoca con `/<nombre>`.
- **Hook**: script que se ejecuta antes (`PreToolUse`) o después (`PostToolUse`) de que el agente use una herramienta. Decide bloquear o permitir.
- **MCP** (Model Context Protocol): protocolo que permite a Claude Code conectarse a fuentes externas (bases de datos, APIs, sistemas) como contexto.
- **`settings.json`**: configuración del proyecto en `.claude/settings.json` — model, maxTokens, systemPrompt, allowedTools, excludePatterns, hooks.
- **Allowed tools**: lista blanca de capacidades del agente (Read, Glob, Grep, Edit, Write, Bash, WebFetch...).
- **Exclude patterns**: lista de globs de archivos que el agente nunca debe leer ni tocar.
- **System prompt**: instrucciones constantes que se envían a Claude con cada llamada. Define personalidad y convenciones del equipo.
- **`ANTHROPIC_API_KEY`**: variable de entorno con la clave de API. En CI/CD se inyecta como secret.

---

## 13. Cierre

El primer submódulo de M09 cierra con una idea que no aparece explícita en ninguna slide pero atraviesa todo el material: **delegar trabajo a un agente exige firmar el contrato antes**. El `settings.json`, los exclude patterns, los hooks — no son burocracia; son el equivalente al protocolo de cualquier delegación seria entre humanos. Si invitas a un contratista a reformar la cocina, le enseñas la casa, le dices qué partes están vedadas, acuerdas el alcance y firmáis un papel. Con Claude Code es exactamente lo mismo, solo que el papel es JSON versionado en git.

El alumno que internaliza esto adopta Claude Code de forma sostenible: lo usa mucho, le saca productividad real, y tiene la conversación con seguridad lista para defender la decisión. El que se salta el contrato acaba con el caso 1, el caso 2 o el debate eterno del caso 3.

Lo siguiente es [`S9.2 — Casos de uso (refactor, IaC, debugging)`](../S9.2-claude-code-casos-uso/MANUAL.md), donde los modos y features del recomendador se aplican a las tareas más comunes del día a día: el refactor cross-archivo que antes te llevaba la tarde, la IaC generada en minutos, el debugging guiado.
