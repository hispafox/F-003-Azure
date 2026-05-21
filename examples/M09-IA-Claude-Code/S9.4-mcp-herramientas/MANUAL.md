# Manual del alumno — S9.4 · MCP: Model Context Protocol y herramientas externas

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, estructura del proyecto, endpoints, tests. Este manual va antes: te cuenta qué problema operativo resuelve MCP, por qué la analogía del cinturón de llaves del técnico encaja con todo lo que hay que decidir al instalar un server, y dónde se cuelan los anti-patterns de seguridad que el ejemplo intercepta antes de que toques producción.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.4](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.4-mcp-herramientas-v3.md). Tres piezas de lógica pura (parser del `claude_desktop_config.json`, recomendador de servers MCP por escenario del equipo, security checker contra los tres riesgos del slide 9) más un planificador que las une en un onboarding repetible.

*Creado: 2026-05-21 21:42 +0200*

---

## 1. La idea en una frase

MCP convierte a Claude Code de "un agente que solo ve tu filesystem" en "un agente que también lee work items de Azure DevOps, comenta PRs en GitHub, consulta una colección de Cosmos, actualiza una página de Notion y avisa al canal de Slack del equipo" sin que tengas que escribir un solo cliente. El precio de esa potencia tiene un nombre concreto: cada server MCP es un proceso local con credenciales reales que viven en tu `claude_desktop_config.json`, y la diferencia entre un setup correcto y un incidente serio se mide en cuatro decisiones binarias del slide 9 (credenciales por variable de entorno, permisos mínimos, paths restringidos, rotación documentada).

El alumno entrena dos decisiones del día a día: **describir qué herramientas usa su equipo** para que el recomendador devuelva la lista de servers MCP con permisos mínimos ya escritos, y **pasar el config por el security checker antes de commitear** para que los tres anti-patterns más comunes (tokens en plano, filesystem en `/`, servers de Git sin política de rotación) salten antes de que entren al repositorio.

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier equipo que adopta Claude Code con MCP:

**Caso 1: el token de GitHub que se filtró al hacer pair con un compañero.** Una developer instala el server `github` siguiendo un tutorial que vio en Reddit. Pega su Personal Access Token directamente en el campo `env: { GITHUB_TOKEN: "ghp_abc123..." }` del config, lo guarda en el filesystem del portátil y sigue trabajando. Una semana después comparte pantalla con un compañero junior para enseñarle Claude Code; abre el archivo de configuración para mostrarle cómo se añade un server nuevo. El compañero ve el token, no se da cuenta de la importancia, no dice nada. Tres meses después el token aparece en una incidencia de seguridad: alguien lo usó desde una IP polaca a las 3 AM. El security checker del ejemplo lo detecta de un vistazo: cualquier `env` con un patrón tipo `ghp_…`, `github_pat_…` o un secret `sk-…` salta como hallazgo Crítico con la mitigación exacta ("sustituye por `${VAR}` o `$env:VAR`").

**Caso 2: el `filesystem` server en `/` que borró por accidente medio `$HOME`.** Otro alumno instala el server `filesystem` con `args: ["/"]` "para que Claude pueda ver todo el portátil sin tener que dar permisos cada vez". Trabaja sin problemas durante semanas. Un día le pide a Claude "limpia los logs viejos del proyecto" y Claude, interpretando el contexto, ejecuta un borrado recursivo que toca también una carpeta de `~/Documents` con cosas personales no versionadas. La pérdida es de unas horas de trabajo y una tarde de espabilarse con copias en el correo, pero el aprendizaje es contundente: el server `filesystem` debe ir restringido a la carpeta del proyecto. El security checker del ejemplo lo coge: `args` con `/`, `~`, `$HOME`, `C:\`, `/home` o `/Users` se marca como Crítico con la mitigación "Restringe a la carpeta del proyecto".

**Caso 3: la PAT de Azure DevOps que llevaba 18 meses sin rotar.** Tercer equipo. Tiene MCP de Azure DevOps configurado con un PAT con scope amplio (`vso.code_full`, `vso.work_full`, `vso.build_execute`) creado por el tech lead anterior al adoptarlo. El tech lead se fue de la empresa, nadie revocó el PAT ni rotó la credencial. Pasa año y medio. Un audit de seguridad ISO detecta el patrón ("PAT activo, no rotada, asignada a un usuario que no está en el directorio") y la compañía recibe un hallazgo de no conformidad. El security checker del ejemplo no resuelve el problema retroactivamente, pero **lo previene a futuro**: cada vez que detecta un server llamado `github` o `azure-devops` añade un aviso Medio con la mitigación "documenta el calendario de rotación en `docs/mcp-rotation.md`" y la regla operativa de 90 días.

Los tres casos los ataca el ejemplo. `McpSecurityChecker` detecta los dos primeros como hallazgos Críticos y el tercero como recordatorio Medio; `McpServerRecommender` ya devuelve la lista de permisos mínimos por server para que ni siquiera te lo plantees mal desde el primer commit.

---

## 3. Por qué esto importa en tu stack

Si tu equipo usa Claude Code en serio y empieza a pensar en MCP, tres preguntas que conviene resolver antes de plantear el primer PR con `claude_desktop_config.json`:

- **¿Qué servers debería habilitar y con qué permisos exactos?** El recomendador del ejemplo te lo dice por escenario. Marca las casillas de tu realidad (ADO sí, GitHub no, Cosmos sí, Notion no) y devuelve la lista de servers con permisos mínimos ya escritos por server: `repo (read)` para GitHub si solo lees issues, `pull_requests (write)` solo si Claude crea PRs, `db_datareader (read-only)` para SQL Server. Te ahorra la pelea de "qué scopes le pongo al token" en el GUI del proveedor.
- **¿Cómo versionamos el config en el repo sin filtrar credenciales?** Plantilla con `${VAR}` o `$env:VAR` en todo lo sensible, valor real exportado desde el entorno del usuario (`~/.bashrc`, `$PROFILE` de PowerShell, key vault local). El security checker rechaza cualquier desviación.
- **¿Cómo evitamos que el `filesystem` server se vuelva una bomba?** Una sola regla: restringido a la carpeta del proyecto, jamás a `/`, `$HOME`, `~`, `C:\`. El security checker bloquea las seis variantes peligrosas.

Si tu equipo tiene las tres respuestas claras, MCP es una palanca neta de productividad. Sin las respuestas claras, MCP es la puerta de entrada al incidente del caso 1 o 2.

---

## 4. La analogía vertebradora: el cinturón de llaves del técnico de mantenimiento

Un técnico de mantenimiento de un grupo hotelero entra cada mañana a cuatro o cinco hoteles distintos. Cumple turnos rotatorios entre tres edificios de oficinas y dos centros logísticos. No abre puertas con la espalda ni rompe cerraduras: lleva un cinturón portallaves al que están enganchadas las llaves específicas que necesita para su jornada. Cada llave tiene una etiqueta de papel grapada con el nombre del edificio y el alcance ("Hotel Madrid · planta técnica", "Almacén Barajas · sala de cuadros", "Oficina Atocha · solo zona común"). El cinturón no es el llavero maestro del grupo. El llavero maestro vive en la caja fuerte de la sede central y no sale de ahí.

Cada llave del cinturón es un servidor MCP. El cinturón es el `claude_desktop_config.json`, donde cada entrada del bloque `mcpServers` engancha una llave con su etiqueta. La llave del Hotel Madrid es el server `filesystem` apuntando al directorio del proyecto: abre solo esa zona, no toda la finca. La del Almacén Barajas es el server `azure-devops` con permisos de Work Items en lectura y Code en escritura solo cuando Claude crea PRs. La de Atocha es el `github` con scope mínimo, solo para issues que el agente comente. Las llaves de los servicios auxiliares (Notion, Slack, Linear) están todas etiquetadas con su alcance y la fecha de la última renovación.

El detalle operativo importante es cómo se gestiona la entrega de las llaves al técnico. No se las guarda en el cinturón "para siempre"; cada mañana las recoge en el control de seguridad de la oficina central, firma el parte, y al final del turno las devuelve. Eso es exactamente lo que hace el patrón `${VAR}` o `$env:VAR`: la llave (el token, el PAT, la API key) **no vive en el cinturón** (el archivo de configuración que se commitea), vive en el control de seguridad del usuario (variables de entorno, key vault local, password manager), y se inyecta cada vez que arranca Claude Code. El cinturón versionado en git lleva solo el nombre de la etiqueta (`${GITHUB_TOKEN}`), no la llave de verdad.

Y hay un protocolo de rotación de llaves cada 90 días: el control de seguridad invalida las antiguas, emite copias nuevas, y reemite con la misma etiqueta. Si un técnico ha perdido una llave o tiene sospecha de copia, lo reporta y se rota inmediatamente. Los servers de Git (GitHub, Azure DevOps) son los que mueven más cosas críticas, por eso el security checker siempre añade su recordatorio de rotación aunque no detecte nada malo en el config: la rotación regular es disciplina, no reacción.

Mantén la imagen: técnico con cinturón portallaves, llaves etiquetadas con alcance, control de seguridad que entrega y rota, llavero maestro que nunca sale de la caja fuerte. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El parser del config (`McpConfigParser.Parsear`)

La función más mecánica del submódulo. Recibe el JSON literal del `claude_desktop_config.json` y devuelve un `McpConfig` con la lista de `McpServer` (nombre, command, args, env) y los `Avisos` de problemas estructurales que detecta sobre la marcha.

La gracia está en cómo trata los errores sin lanzar excepciones. Un JSON malformado no rompe el endpoint, devuelve un aviso:

```csharp
JsonDocument doc;
try { doc = JsonDocument.Parse(json); }
catch (JsonException ex)
{
    return new McpConfig([], [$"JSON inválido: {ex.Message}"]);
}
```

Lo mismo si falta la clave `mcpServers` en la raíz, si está pero no es un objeto, o si un server individual no es un objeto JSON:

```csharp
if (!raiz.TryGetProperty("mcpServers", out var serversEl)
    || serversEl.ValueKind != JsonValueKind.Object)
{
    avisos.Add(
        "Falta la clave `mcpServers` en la raíz, o no es un objeto (slide 3).");
    return new McpConfig(servers, avisos);
}
```

Esta tolerancia es intencional: cuando un alumno está aprendiendo MCP, lo normal es que rompa el JSON varias veces antes de tener un config válido. El parser le dice qué está mal con frase concreta, no con un stack trace. Y para el security checker, recibir un `McpConfig` con `Servers=[]` es perfectamente operable: simplemente no genera hallazgos sobre lo que no entendió.

El detalle del recorrido es que cada server se extrae con sus tres campos relevantes (command, args, env) y nada más. Si el JSON original tiene metadatos extra (descripciones, comentarios, campos custom), el parser los ignora. Es una decisión de simplicidad coherente con el objetivo pedagógico: el alumno aprende qué tres campos importan en MCP, no a parsear el JSON Schema completo del estándar.

### El recomendador de servers (`McpServerRecommender.Recomendar`)

La función con el conocimiento de dominio del submódulo. Recibe un `EscenarioMcp` con once banderas booleanas (UsaAzureDevOps, UsaGitHub, UsaCosmosDb, UsaSqlServer, UsaPostgres, UsaNotionODocs, UsaSlackOTeams, UsaJiraOLinear, NecesitaBrowserAutomation, NecesitaObservabilidad, EquipoEnM365) y devuelve la lista de `ServerSugerido` con la categoría, el slide de referencia, el porqué y los **permisos mínimos** de cada uno.

La pieza clave es que `filesystem` está siempre incluido sin condicional. Es la base de cualquier setup MCP:

```csharp
lista.Add(new(
    Nombre: "filesystem",
    Categoria: CategoriaMcp.Desarrollo,
    Slide: "3",
    Porque: "Necesario para que Claude lea y edite el código.",
    PermisosMinimos: ["leer y escribir SOLO en el path del proyecto"]));
```

Es la llave del cinturón que no se quita nunca, equivalente al pase de acceso del técnico a la planta donde está su taquilla. Sin esa, Claude Code no puede ni siquiera empezar a hacer nada.

Cada server condicional añade su entrada con permisos mínimos escritos como si los fuera a copiar el alumno literalmente al portal del proveedor:

```csharp
if (e.UsaAzureDevOps)
    lista.Add(new(
        Nombre: "azure-devops",
        // ...
        PermisosMinimos: [
            "Work Items: Read",
            "Code: Read",
            "Build: Read",
            "Code: Write SOLO si Claude crea PRs",
        ]));
```

El "Code: Write SOLO si Claude crea PRs" es el patrón didáctico clave: **read por defecto, write solo cuando hay un caso justificado**. Para GitHub repite la misma forma: `repo (read)` siempre, `pull_requests (write SOLO si Claude crea PRs)`. Para Postgres recomienda `usuario read-only por defecto`, para Cosmos `Cosmos DB Built-in Data Reader`. La estructura mental que el alumno se lleva es: "si vas a habilitar un server de write, tienes que poder justificar qué crea Claude que merezca esa escalada". Si no puedes justificarlo, no lo habilites.

Once banderas dan 2^11 = 2048 combinaciones posibles, pero en la práctica los escenarios reales son media docena: el equipo de backend (ADO + Cosmos), el equipo de frontend (GitHub + Notion + Sentry), el SRE (GitHub + observabilidad + Slack), el data engineer (ADO + Postgres + browser automation). El recomendador no limita las combinaciones, simplemente devuelve lo que pidas. La decisión de qué activar es del alumno.

### El security checker (`McpSecurityChecker.Comprobar`)

La pieza que cierra el bucle. Recibe el `McpConfig` parseado y devuelve un `InformeSeguridad` con `Seguro=true/false`, lista de hallazgos clasificados (Crítico, Alto, Medio, Bajo) y los contadores de Críticos y Altos. La regla de aceptación es estricta: `Seguro = criticos == 0 && altos == 0`. Cualquier Crítico o Alto bloquea.

Tres familias de comprobaciones, cada una atacando un anti-pattern concreto del slide 9:

**Tokens hardcoded.** Para cada `env` cuya clave contenga una palabra del bote sensible (`TOKEN`, `PAT`, `API_KEY`, `SECRET`, `PASSWORD`, `CONNECTION_STRING`, `WEBHOOK`, `BEARER`), el checker mira el valor y aplica cuatro regex de tokens reales:

```csharp
[GeneratedRegex(@"^(ghp_|github_pat_|ghu_|ghs_|ghr_)[A-Za-z0-9_]{20,}$")]
private static partial Regex TokenGithubRegex();

[GeneratedRegex(@"^[A-Za-z0-9]{52}$")] // PAT de ADO clásico (~52 chars base64ish)
private static partial Regex TokenAdoRegex();

[GeneratedRegex(@"^xoxb-[A-Za-z0-9-]+$")] // Slack bot tokens
private static partial Regex TokenSlackRegex();

[GeneratedRegex(@"^sk-[A-Za-z0-9_\-]{20,}$")]
private static partial Regex SecretKeyRegex();
```

Y antes de aplicar las regex, descarta los falsos positivos legítimos: cualquier valor que empiece por `${`, `$env:` o `$<nombre>` no es un secreto en plano, es una referencia a variable de entorno. La lógica es:

```csharp
if (trimmed.StartsWith("${", StringComparison.Ordinal)) return false;
if (trimmed.StartsWith("$env:", StringComparison.Ordinal)) return false;
if (trimmed.StartsWith("$", StringComparison.Ordinal) && !trimmed.Contains(' ', StringComparison.Ordinal))
    return false;
```

Eso te permite escribir el config con `${GITHUB_TOKEN}` y que no salte alarma; pero pega un `ghp_abc123...` y salta como Crítico con la mitigación exacta.

**Filesystem con paths amplios.** Para el server llamado `filesystem`, comprueba que ningún `arg` sea `/`, `~`, `$HOME`, `C:\`, `/home` o `/Users`. Las seis variantes están en la lista negra explícita:

```csharp
private static bool PareceRaiz(string path)
{
    if (string.IsNullOrWhiteSpace(path)) return false;
    var p = path.Trim();
    return p == "/" || p == "~" || p == "$HOME"
        || p.Equals("C:\\", StringComparison.Ordinal)
        || p.Equals("/home", StringComparison.Ordinal)
        || p.Equals("/Users", StringComparison.Ordinal);
}
```

Cualquier match es Crítico. La mitigación dice exactamente qué hacer: "Restringe a la carpeta del proyecto (`/home/dev/projects/<repo>`)".

**Recordatorio de rotación para servers de Git.** Cada vez que aparece un server cuyo nombre contiene `github` o `azure-devops`, el checker añade un hallazgo Medio:

```csharp
hallazgos.Add(new(
    NivelRiesgo.Medio,
    sv.Nombre,
    "Servers de Git con tokens deben rotarse cada 90 días.",
    "Documenta el calendario de rotación en `docs/mcp-rotation.md`."));
```

Este es el único hallazgo "preventivo" del checker: no detecta un problema, recuerda una buena práctica. El alumno puede preguntarse "si es Medio y no bloquea, ¿por qué está?". La razón es operacional: cuando un equipo audita su config, el informe va a tener siempre al menos este hallazgo Medio si tiene servers de Git, lo cual obliga a abrir el documento de rotación y verificar que está al día. Es un nudge sostenible, no un ruido.

---

## 6. El catálogo de servers MCP por escenario del equipo

El recomendador cubre once tipos de server. Vale la pena ver el catálogo como tabla para visualizar de un golpe qué cubre cada categoría:

| Server | Categoría | Slide | Cuándo habilitarlo | Permisos mínimos |
| --- | --- | --- | --- | --- |
| `filesystem` | Desarrollo | 3 | Siempre | leer/escribir en el path del proyecto |
| `azure-devops` | Desarrollo | 4 | Equipo en ADO | Work Items Read + Code Read + Build Read (+ Code Write si crea PRs) |
| `github` | Desarrollo | 5 | Equipo en GitHub | repo read (+ pull_requests write si crea PRs) |
| `azure-cosmos` | BaseDatos | 6/11 | Diagnóstico/análisis Cosmos | Cosmos DB Built-in Data Reader |
| `sql-server` | BaseDatos | 11 | Queries SQL desde terminal | db_datareader |
| `postgres` | BaseDatos | 3 | Queries Postgres | usuario read-only |
| `notion` | Productividad | 7/11 | Docs y bases en Notion | read+write en workspace del equipo |
| `slack` | Productividad | 7/14 | Avisos a canal del equipo | chat:write SOLO en canales del bot |
| `linear` | Productividad | 15 | Linear/Jira/Asana | read + comment (+ write si crea issues) |
| `puppeteer` | Desarrollo | 11 | E2E o smoke browser | localhost o dominios concretos |
| `sentry` | Observabilidad | 15 | Troubleshooting errores | read-only del proyecto |

Hay tres lecturas operativas de esta tabla:

La primera es que **read-only es el default real**, no una recomendación blanda. Nueve de los once servers tienen read-only por defecto; solo dos (Notion para docs, Slack para avisar) tienen write inherente. Y los servers que abren a write opcional (ADO, GitHub, Linear) llevan la condicional escrita literal: "solo si Claude crea PRs/issues". Si no tienes un workflow donde Claude crea PRs, deja el write fuera.

La segunda es que **el server `filesystem` es la base universal**. Está siempre, en cualquier escenario, porque sin acceso al código local Claude Code no puede ni leer el archivo que vas a tocar. Por eso el security checker pone tanto énfasis en restringir su scope: es la primera entrada del cinturón, la que más temprano sale mal si la dejas con `/`.

La tercera es que **los servers de BD piden read-only directamente**. Cosmos, SQL Server y Postgres están todos con scope de solo lectura. Si necesitas que Claude haga inserts o updates en producción, hay otras herramientas (Functions con su patrón de revisión, scripts versionados) que dan trazabilidad. MCP con BD es para diagnóstico y exploración, no para mutar datos.

---

## 7. La conversación con seguridad: el config como template versionado

Si tu equipo tiene un proceso formal de revisión de seguridad (DPIA, ISO 27001, SOC 2), el patrón que entrega este submódulo es el que pasa esas revisiones limpio:

1. El `claude_desktop_config.json` se versiona en el repo como **template**, con `${VAR}` o `$env:VAR` en todo lo sensible. El archivo en git no tiene credenciales.
2. Cada developer exporta sus tokens desde el entorno del usuario, no del repo (`~/.bashrc`, `$PROFILE`, key vault local, 1Password CLI). Cuando arranca Claude Code, las variables se inyectan en runtime.
3. Hay un `docs/mcp-rotation.md` con el calendario de rotación de tokens (90 días por defecto), responsable de la rotación, y procedimiento si un token se compromete.
4. El CI ejecuta el security checker contra el template como parte del pipeline. Si aparece un hallazgo Crítico o Alto, el build falla.
5. El `filesystem` server tiene un path explícito relativo al directorio del proyecto, no `/` ni `$HOME`. El security checker lo verifica.

La pregunta que va a hacer seguridad en la primera reunión es: "¿cómo evitáis que un developer ponga un token en plano por error?". La respuesta tiene tres capas: gitignore protege los archivos de variables locales, el security checker bloquea cualquier match de regex de tokens reales en el `env`, y la cultura de equipo (este manual + el documento de rotación) refuerza la práctica. Tres capas independientes; el incidente requiere que las tres fallen.

La segunda pregunta es: "¿qué pasa si un developer deja Claude Code abierto y se va a comer?". La respuesta no es del submódulo (es de M09-S9.5: buenas prácticas y limitaciones), pero conviene saberla: Claude Code no acumula sesión persistente con tokens cacheados a nivel sistema, cada arranque relee el entorno. Si el developer cierra la terminal y el shell padre limpia las variables, los tokens dejan de estar disponibles. Es una capa adicional de defensa que el alumno conviene mencione en la conversación.

---

## 8. La conversación con el equipo: ¿quién es dueño del config?

Hay dos formas legítimas de tener el `claude_desktop_config.json` en un equipo, y conviene que el alumno entienda los trade-offs antes de adoptar una u otra:

**Modelo "config compartido en el repo"**: el template del config está en el repo del proyecto. Cada developer tiene las mismas entradas de servers (mismos nombres, mismos paths, mismas variables de entorno). Lo que cambia entre developers son solo los valores de las variables, exportadas localmente. Ventaja: onboarding fácil ("clona el repo, exporta estas cuatro variables, arranca Claude"). Desventaja: cualquier server experimental que un developer quiera probar afecta a todo el equipo si modifica el config compartido.

**Modelo "config por developer"**: cada developer mantiene su `~/Library/Application Support/Claude/claude_desktop_config.json` con sus servers. El repo del proyecto solo documenta cuáles se recomiendan. Ventaja: máxima flexibilidad individual, prueba servers nuevos sin tocar al equipo. Desventaja: dos developers pueden tener experiencias muy distintas con Claude Code en el mismo proyecto, y debugar "por qué a mí me funciona y a ti no" es una pesadilla.

El submódulo está pensado para el modelo compartido, que es el que escala mejor en equipos de 5+ personas. El recomendador genera la lista canónica, el security checker valida el template antes del commit, el documento de rotación vive con el equipo. Para una startup de tres developers, el modelo individual también funciona; lo importante es que la decisión se tome consciente y se documente.

---

## 9. Cómo probarlo en local

Es un ejemplo offline al 100%. No invoca servers MCP reales; modela las decisiones que harías al configurarlos.

```bash
dotnet run --project src/ClaudeCode.Mcp.Demo.Api
# http://localhost:5116
```

Cuatro endpoints útiles, todos POST con JSON:

```http
### Parsear un claude_desktop_config.json
POST http://localhost:5116/mcp/config/parsear
Content-Type: application/json

{
  "json": "{\"mcpServers\":{\"filesystem\":{\"command\":\"npx\",\"args\":[\"-y\",\"@modelcontextprotocol/server-filesystem\",\"/home/dev/projects/miapp\"]},\"github\":{\"command\":\"npx\",\"args\":[\"-y\",\"@modelcontextprotocol/server-github\"],\"env\":{\"GITHUB_TOKEN\":\"${GITHUB_TOKEN}\"}}}}"
}
# → 2 servers: filesystem (path restringido), github (token por env var)

### Recomendar servers según escenario del equipo
POST http://localhost:5116/mcp/recomendar
Content-Type: application/json

{
  "usaGitHub": true,
  "usaCosmosDb": true,
  "usaNotionODocs": true,
  "necesitaObservabilidad": true
}
# → 5 servers: filesystem, github, azure-cosmos, notion, sentry
#   cada uno con su permiso mínimo

### Comprobar la seguridad del config
POST http://localhost:5116/mcp/seguridad
Content-Type: application/json

{
  "json": "{\"mcpServers\":{\"filesystem\":{\"command\":\"npx\",\"args\":[\"/\"]},\"github\":{\"command\":\"npx\",\"env\":{\"GITHUB_TOKEN\":\"ghp_abc123abc123abc123abc123abc123abc12\"}}}}"
}
# → Seguro=false, 2 Críticos: filesystem en `/`, token en plano
#   1 Medio: rotación 90 días para github

### Plan completo (recomendar + parsear + seguridad + checklist)
POST http://localhost:5116/mcp/plan
Content-Type: application/json

{
  "escenario": { "usaGitHub": true, "usaSlackOTeams": true },
  "configJson": "{\"mcpServers\":{\"filesystem\":{\"command\":\"npx\",\"args\":[\"/home/dev/proj\"]}}}"
}
# → recomendados (3 servers), config actual parseada, seguridad evaluada,
#   checklist de 8 puntos
```

Los 33 tests cubren:

- Capa 1 (unit): parser tolerante a JSON inválido, recomendador con cada bandera individual, security checker con todos los anti-patterns (regex de tokens reales, paths de root, falsos positivos con `${VAR}`).
- Capa 0 (DI): `IMcpPlanner` resoluble del contenedor real como singleton.
- Capa E2E: los cuatro endpoints via `WebApplicationFactory`.

No hay capa de integración real porque arrancar servers MCP reales (ADO, GitHub, Cosmos) consume credenciales, segundos de startup y rate limits. El valor pedagógico está en **diseñar el config bien** y en **detectar los anti-patterns antes de ejecutar**, que es lógica pura testeable sin Internet.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 10. Anti-patterns

Cinco prácticas que evitar:

**Anti-pattern 1: token de GitHub o PAT de ADO pegado literalmente en `env`.** Es el caso 1 de la sección 2 esperando a pasar. Aunque el repo sea privado, aunque el portátil esté cifrado, aunque nunca compartas pantalla: la regla es absoluta. Cualquier credencial real va a una variable de entorno; el config solo conoce el nombre de la variable. El security checker lo detecta con regex específicas por proveedor y devuelve la mitigación exacta.

**Anti-pattern 2: `filesystem` server en `/`, `$HOME` o `~`.** Es el caso 2 de la sección 2. Aunque parezca cómodo "para que Claude vea todo el portátil", multiplica el blast radius de cualquier comando destructivo por todo el sistema de archivos. La regla es: un server `filesystem` por proyecto, con `args` apuntando al directorio del proyecto exactamente. Si tienes que trabajar con varios proyectos, lanza Claude Code en cada uno con su propio config.

**Anti-pattern 3: write permissions sin caso justificado.** Si habilitas `pull_requests (write)` en GitHub "por si acaso Claude tiene que crear un PR en el futuro", ya estás en mal sitio. Habilita write cuando tengas un workflow concreto que lo necesite (Claude que recoge cambios y abre el PR de release semanal, por ejemplo). Si no, mantén todo en read-only; el día que necesites write, expandes los scopes con tres clicks.

**Anti-pattern 4: olvidar el calendario de rotación.** Es el caso 3 de la sección 2. Un PAT activo durante 18 meses es un riesgo acumulado: la probabilidad de que esa credencial se haya filtrado en algún log, en algún error, en alguna pantalla compartida, sube con el tiempo. La regla operativa son 90 días. Documenta en `docs/mcp-rotation.md` el calendario, el responsable, y el procedimiento de emergencia si un token se compromete.

**Anti-pattern 5: ejecutar el security checker como auditoría puntual en lugar de gate de CI.** Si pasas el security checker una vez al mes "para ver cómo va", funciona pero llega tarde. Como gate de pipeline en cada PR que toca el `claude_desktop_config.json`, **bloquea las regresiones antes de que entren al repositorio**. La diferencia operacional es enorme: en el primer modelo, te enteras del problema cuando ya está commiteado; en el segundo, el PR ni se abre.

---

## 11. Glosario breve

- **MCP** (Model Context Protocol): protocolo abierto que permite a un LLM hablar con sistemas externos mediante servers especializados.
- **MCP server**: proceso local que expone capacidades de un sistema externo (GitHub, ADO, Cosmos, Notion) al cliente MCP. Se invoca por `command` + `args`.
- **`claude_desktop_config.json`**: archivo de configuración de Claude Code donde se declaran los servers MCP a habilitar.
- **`filesystem` server**: server MCP base que da a Claude acceso de lectura y escritura a un directorio del filesystem. Restricción obligatoria al directorio del proyecto.
- **PAT** (Personal Access Token): credencial con scopes específicos para Azure DevOps, GitHub, etc. Sustituye al password en flujos automatizados.
- **Scope de un token**: conjunto de operaciones que el token autoriza. La regla del submódulo es "read-only por defecto, write solo cuando hay caso".
- **Variable de entorno** (en MCP): mecanismo de inyección de credenciales en tiempo de ejecución. Sintaxis aceptada por el security checker: `${VAR}`, `$env:VAR`, `$VAR`.
- **Rotación de credenciales**: práctica de invalidar y reemitir tokens periódicamente (90 días por defecto en este submódulo).
- **Severidad del hallazgo**: Crítico (bloquea), Alto (bloquea), Medio (recordatorio sostenible), Bajo (informativo).
- **Template del config**: versión del `claude_desktop_config.json` versionada en el repo con `${VAR}` en todos los valores sensibles. Sin credenciales reales.
- **OIDC**: federated credentials. No aparece en MCP directamente pero es el patrón equivalente para pipelines (M09-S9.3 y M08-S8.P).
- **Least privilege**: principio de seguridad por el que se otorgan permisos mínimos necesarios. El recomendador lo aplica server por server.

---

## 12. Cierre

La conclusión que se gana este manual no es teórica: cada vez que añadas un MCP server al cinturón, anota en tres líneas qué etiqueta tiene (read-only o write, con caso justificado), de dónde sale la credencial (variable de entorno, nunca plano), y cuándo le toca rotación. Si las tres líneas no salen sin pensar, el server no está listo para producción todavía. El security checker captura los dos primeros automáticamente; el tercero depende de disciplina del equipo.

Lo siguiente es [`S9.5 — Buenas prácticas y limitaciones`](../S9.5-cc-buenas-practicas/MANUAL.md), donde se habla de cuándo NO usar Claude Code, qué riesgos asume el patrón de agente con herramientas externas, y cómo revisar lo que Claude propone antes de aceptarlo.
