# Manual del alumno — S9.3 · Claude Code para infraestructura Azure

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué hace bien Claude Code al generar Bicep, qué te ahorra de verdad en una "reverse engineering" de infra existente, y por qué el audit checker es la pieza que evita que pase a producción algo con HTTPS desactivado.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.3](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.3-cc-infraestructura-v3.md). Tres piezas de lógica pura (parser de requisitos en lenguaje natural, generador de 7 prompts canónicos para IaC, audit checker contra reglas mínimas) más un planificador que las une.

*Creado: 2026-05-21 01:10 +0200*

---

## 1. La idea en una frase

Generar Bicep desde cero es una de las tareas donde Claude Code brilla más, porque combina tres cosas que ya viste en submódulos previos: contexto multi-archivo (S9.1), prompts canónicos con los cuatro ingredientes (S9.2) y disciplina IaC de M08-S8.5 (linter, what-if obligatorio, `Delete:` como alarma roja). Este submódulo no inventa nada nuevo: empaqueta esos tres mundos en un flujo operacional con siete escenarios concretos que cubren el 95% del trabajo IaC real de un equipo Azure.

El alumno entrena dos decisiones reales del día a día: **describir requisitos en lenguaje natural** de forma que el parser detecte recursos y banderas no funcionales sin tener que enumerar nada a mano, y **aplicar el audit checker** antes de cualquier merge para que las reglas mínimas (HTTPS only, Managed Identity, TLS 1.2, tags obligatorios, sin acceso público en Storage, firewall en SQL) no se cuelen como hallazgos en producción.

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier equipo Azure que se plantea adoptar Claude Code para IaC:

**Caso 1: el Bicep "perfecto" sin Managed Identity.** Un equipo le pide a Claude Code "genera el Bicep para un App Service que conecte a Cosmos DB". El agente devuelve un Bicep impecable, modular, con tags, con parámetros, con `@secure()`. El alumno lo aplica y funciona. Pasa una semana y el equipo de seguridad ejecuta su revisión: el App Service usa **connection string con account key** de Cosmos en lugar de Managed Identity. Es un anti-pattern de S6.1 que el alumno no mencionó en el prompt, por lo que Claude eligió la opción "estándar de internet" en vez de la "estándar moderna de tu equipo". El parser del ejemplo lo evita: si la descripción no menciona Managed Identity y hay App Service, **el aviso aparece automáticamente** ("Sin Managed Identity declarada — usa MI en vez de connection strings con password").

**Caso 2: el reverse engineering que tardó 12 horas a mano.** Otro equipo hereda un resource group con 40 recursos creados a mano durante dos años por gente que ya no está en la empresa. Necesitan reconstruir la infra en Bicep para poder gestionarla con GitOps. El developer hace `az group export`, mira el JSON de 8000 líneas, y se desespera. La opción tradicional es **dos días de trabajo manual** ordenando módulos. Con el prompt `ReverseArmABicep` del ejemplo, son **30 minutos**: `az bicep decompile` te da el Bicep crudo (feo, monolítico, con nombres de recurso quemados), Claude Code lo reorganiza en módulos por dominio, le pone parámetros, AVM modules cuando aplica, tags obligatorios y termina con un `az deployment group what-if` de verificación. El factor 24x no es marketing; es el caso real.

**Caso 3: el deploy que pasó CI verde pero rompió compliance.** Tercer equipo. El Bicep que generaron compila, el `what-if` no muestra deletes inesperados, el pipeline mergea limpio. A las dos semanas, auditoría externa: **el SQL Server estaba sin firewall configurado**. Resultado: incidente menor pero conversación incómoda con seguridad. El audit checker del ejemplo lo coge antes del merge: pasas la lista de recursos por `InfraAuditChecker.Auditar` y devuelve hallazgos clasificados por severidad con el comando `az` exacto para arreglar cada uno. Como hook `PreToolUse` en el pipeline IaC, es la red de seguridad final.

Los tres casos los aborda el ejemplo. `InfraRequirementsParser` añade los avisos que faltan en la descripción; `InfraPromptBuilder.ReverseArmABicep` automatiza el factor 24x del reverse engineering; `InfraAuditChecker` cierra el bucle con el audit pre-merge.

---

## 3. Por qué esto importa en tu stack

Si tu equipo usa Bicep (o Terraform; los conceptos se traducen) y está empezando a usar Claude Code, tres preguntas que conviene tener resueltas:

- **¿Cómo describo los requisitos para que Claude genere Bicep correcto a la primera?** El parser del ejemplo es la respuesta: usa estas palabras clave en la descripción (App Service, Cosmos DB, multi-region, GDPR, slots, autoscale, HTTPS only, Managed Identity) y los avisos te dicen qué te falta antes de mandar el prompt.
- **¿Qué hago con la infra creada a mano antes de adoptar IaC?** El prompt `ReverseArmABicep` con `az bicep decompile` como input. No es perfecto en el primer intento, pero parte del 95% hecho y tú revisas el 5% restante.
- **¿Cómo evito que se cuelen anti-patterns en producción?** Audit checker antes del merge. Cinco reglas binarias (HTTPS, MI, tags, TLS, público, firewall) que tu pipeline IaC puede ejecutar automáticamente como gate.

Las tres respuestas reducen el tiempo de adopción de IaC de "tres meses peleando" a "tres semanas con red de seguridad".

---

## 4. La analogía vertebradora: el aparejador que te dibuja los planos del chalet

Quieres construir un chalet. No sabes dibujar planos. No conoces el Código Técnico de la Edificación. Tienes claros tus deseos: tres dormitorios, dos baños, cocina abierta al salón, orientación sur, garaje para dos coches. Tampoco tienes prisa en aprender AutoCAD para mañana.

Lo que haces es contratar un **aparejador**. El aparejador no es el arquitecto que firma el proyecto entero (eso es lo que vimos como "el plano del arquitecto" en M08-S8.5, donde **tú** ya escribías el Bicep a mano siguiendo las reglas). El aparejador es un nivel intermedio: **toma tus deseos en lenguaje natural y los traduce a planos formales**. Tú dices "quiero tres dormitorios"; él dibuja las habitaciones con sus medidas, las puertas, los enchufes, las tuberías, las orientaciones. Y de paso aplica el Código Técnico sin que tú tengas que conocerlo: que las habitaciones tengan ventana exterior, que la altura mínima sea de 2.50 m, que la cocina tenga salida de humos.

Eso es Claude Code aplicado a IaC. Tú describes "necesito un App Service con Cosmos DB, multi-region en Europa, con GDPR, slots de staging, autoscale en horas pico"; él genera el Bicep formal con la estructura modular, los AVM modules, los tags obligatorios, los parámetros con `@secure()`, las regiones correctas. El equivalente al Código Técnico son las reglas de tu equipo: HTTPS only, Managed Identity, TLS 1.2, sin acceso público en Storage.

El estudio del aparejador ofrece **siete servicios distintos**, uno por necesidad típica del cliente:

- **El plano del chalet** (`BicepDesdeRequirements`): los planos completos a partir de tus deseos.
- **Las instalaciones de máquinas** (`DockerfileMultiStage`): el cuarto técnico con caldera, cuadro eléctrico, todo optimizado.
- **El permiso del ayuntamiento** (`GhActionsPipeline`): los papeles del Ayuntamiento para que la obra esté legalizada.
- **Los planos antiguos en CAD** (`ReverseArmABicep`): si tu casa ya está construida pero los planos se perdieron, reconstruirlos en formato moderno.
- **La inspección final de obra** (`AuditarRecursos`): verificar que la construcción cumple normativa antes de la cédula de habitabilidad.
- **La guía de mantenimiento** (`RunbookOperaciones`): qué hacer cuando se rompe la caldera o gotea una tubería.
- **El manual de uso del propietario** (`ScriptOps`): cómo manejar el sistema de calefacción, las persianas eléctricas, los rociadores del jardín.

Y por encima de todo, está el **inspector del ayuntamiento** que pasa antes de dar el visto bueno: comprueba que las ventanas cumplen aislamiento térmico, que el ascensor tiene certificado, que la instalación eléctrica está al día. Si falla alguna comprobación, no firma. Eso es exactamente `InfraAuditChecker`: pasa por cada recurso, lo verifica contra las reglas mínimas, y devuelve hallazgos con severidad (Crítico, Alto, Medio, Bajo) y el comando `az` para arreglar.

Mantén la imagen: aparejador que traduce deseos a planos, siete servicios del estudio para distintas necesidades, inspector del ayuntamiento que valida antes del visto bueno. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El parser de requisitos (`InfraRequirementsParser.Parsear`)

La función más pragmática del submódulo. Recibe una descripción libre como "necesito un App Service con Cosmos DB en West Europe y North Europe, slots de staging, GDPR, autoscale" y devuelve un `RequisitosInfra` con la lista de recursos detectados, las banderas no funcionales y, sobre todo, los **avisos** de lo que el alumno olvidó mencionar.

La detección de recursos funciona por matching de patrones con deduplicación: si la descripción menciona "App Service" y "Web App", cuenta como un solo `TipoRecurso.AppService` (la lista `tiposVistos` lo garantiza). Patrones cubren los 10 recursos típicos (App Service, Functions, Cosmos, SQL, Storage, Service Bus, Key Vault, Redis, App Insights, Log Analytics) con sinónimos en español e inglés:

```csharp
("cosmos db", TipoRecurso.CosmosDb),
("cosmosdb", TipoRecurso.CosmosDb),
("cosmos", TipoRecurso.CosmosDb),
```

Lo más interesante son las **banderas no funcionales** y los avisos asociados:

```csharp
bool multiRegion = lower.Contains("multi-region", StringComparison.Ordinal)
    || lower.Contains("multi region", StringComparison.Ordinal)
    || (lower.Contains("west europe", StringComparison.Ordinal)
        && lower.Contains("north europe", StringComparison.Ordinal));
```

Multi-region se detecta de tres formas: literal "multi-region", literal "multi region", o **inferencia** por mencionar dos regiones europeas concretas. El alumno que escribe naturalmente "el sistema debe correr en West Europe y North Europe" no necesita decir además "multi-region". El parser lo entiende.

Y los avisos automáticos resuelven el caso 1 de la sección 2:

```csharp
var avisos = new List<string>();
if (!httpsOnly)
    avisos.Add("No se mencionó HTTPS only — por defecto añade `httpsOnly: true` "
        + "en el Bicep de App Service (slide 9/15).");
if (!mi && recursos.Any(r =>
        r.Tipo is TipoRecurso.AppService or TipoRecurso.Functions))
    avisos.Add("Sin Managed Identity declarada — usa MI en vez de connection "
        + "strings con password (slide 15).");
if (multiRegion && europa)
    avisos.Add("Multi-region + GDPR: confirma que las dos regiones están en la UE "
        + "(slide 17).");
if (recursos.Any(r => r.Tipo == TipoRecurso.Storage)
    && !lower.Contains("private endpoint", StringComparison.Ordinal))
    avisos.Add("Storage detectado: cierra el acceso público y usa Private Endpoint "
        + "(slide 15).");
```

Cuatro avisos clave: HTTPS faltante, Managed Identity faltante cuando hay App Service o Functions, recordatorio de regiones UE cuando hay multi-region + GDPR, recordatorio de Private Endpoint cuando hay Storage. Ninguno requiere experticia técnica: son recordatorios de los anti-patterns más comunes. **El aparejador te avisa de lo que olvidaste pedir**.

### El generador de prompts canónicos (`InfraPromptBuilder.ParaEscenario`)

Siete escenarios, siete prompts canónicos. El más importante es `BicepDesdeRequirements`, que toma los `RequisitosInfra` parseados y los compone en un prompt que ya trae los cuatro ingredientes (contexto: lista de recursos; constraints: no funcional con MI y HTTPS; formato: Bicep modular; criterio éxito: `bicep build` sin warnings + `what-if` sin Delete inesperados):

```csharp
return
    "Necesito infraestructura Azure con estos requisitos:\n\n" +
    $"Recursos: {recursos}.\n" +
    $"No funcional: {nf}.\n\n" +
    "Genera:\n" +
    "1) Bicep modular: `main.bicep` + `main.dev.bicepparam` + " +
    "`main.prod.bicepparam` + `modules/` por tipo de recurso.\n" +
    "2) Tags obligatorios: env, costCenter, owner, app.\n" +
    "3) AVM modules (`br/public:avm/...`) donde aplique.\n" +
    "4) `@secure()` en todo lo sensible; Key Vault Reference en params.\n" +
    "5) `uniqueString()` para nombres.\n" +
    "6) RBAC por Managed Identity (sin connection strings con password).\n" +
    "Criterio éxito: `az bicep build` sin warnings y " +
    "`az deployment group what-if` sin Delete inesperados.";
```

Seis instrucciones que el Bicep generado debe respetar. **Si el alumno copia este prompt a Claude Code**, el resultado sale ya con la estructura del repo de infra que viste en M08-S8.5: `main.bicep` + `params.{env}.json` + `modules/`. No tiene que reordenarlo después.

Los otros seis escenarios tienen patrones similares. Vale la pena destacar dos:

**`ReverseArmABicep`** (slide 16) es la palanca del caso 2:

```csharp
"Lee `exported-arm.bicep` (auto-generado por `az bicep decompile`) y " +
"reescríbelo siguiendo nuestras convenciones:\n" +
"1) Modulariza por tipo de recurso en `modules/`\n" +
"2) Usa AVM modules (`br/public:avm/...`) cuando aplique\n" +
"3) Hardcoded values → `param` con `@description`\n" +
"4) Tags obligatorios: env, costCenter, owner, app\n" +
"5) `@secure()` en todo lo sensible\n" +
"6) `uniqueString()` para nombres\n" +
// ...
"Verificación obligatoria al final: `az deployment group what-if` " +
"y reporta CAMBIOS INESPERADOS."
```

El truco está en la **verificación obligatoria al final**: tras la modularización, ejecutar `what-if` contra la infra existente y comprobar que **no hay cambios inesperados** (lo único que debería cambiar son cosas cosméticas de naming). Si aparece un `Delete:` o un `Modify:` raro, el reverse engineering ha desviado algo y hay que arreglarlo antes de mergear.

**`GhActionsPipeline`** (slide 12/17) integra todo lo que viste en M08:

```csharp
"- Job 1: build + tests + coverage\n" +
"- Job 2 (`needs: build`): deploy a slot staging con OIDC (`azure/login@v2`)\n" +
"- Smoke test contra `https://<app>-staging.azurewebsites.net/health`\n" +
"- Job 3 (Environment `production`, requires approval): swap a producción\n" +
"- Auto-rollback (`condition: failure()`) que hace swap inverso.\n" +
"Usa `vars.AZURE_CLIENT_ID`, `vars.AZURE_TENANT_ID`, " +
"`vars.AZURE_SUBSCRIPTION_ID` (no secretos)."
```

OIDC sin secretos (lección S8.P), tres jobs con `needs:` (lección S8.2), aprobación humana en producción (lección S8.3), auto-rollback con `condition: failure()` (lección S8.3). Es M08-S8.P reescrito como prompt para Claude Code: lo que antes tenías que pedir explícitamente ingrediente a ingrediente, aquí viene en el template.

### El audit checker (`InfraAuditChecker.Auditar`)

La pieza que cierra el bucle: la inspección final de obra. Recibe una lista de `EstadoRecurso` (cada recurso con sus banderas reales: `HttpsOnly`, `TieneManagedIdentity`, `TieneTags`, `AccesoPublico`, `TlsVersion`, `FirewallConfigurado`) y devuelve un `InformeAudit` con hallazgos clasificados:

```csharp
if (esWebApp && !r.HttpsOnly)
    hallazgos.Add(new(Severidad.Critico,
        "Web App sin HTTPS forzado",
        r.Nombre,
        $"az webapp update -n {r.Nombre} --set httpsOnly=true"));

if (esStorage && r.AccesoPublico)
    hallazgos.Add(new(Severidad.Critico,
        "Storage con acceso público",
        r.Nombre,
        $"az storage account update -n {r.Nombre} "
        + "--allow-blob-public-access false"));

if (esSql && !r.FirewallConfigurado)
    hallazgos.Add(new(Severidad.Alto,
        "SQL Server sin firewall configurado",
        r.Nombre,
        $"az sql server firewall-rule create --server {r.Nombre} ..."));
```

Tres detalles importantes del diseño:

**Cada hallazgo trae el comando exacto para arreglarlo**. No es "el SQL Server no tiene firewall, mira la documentación". Es "ejecuta `az sql server firewall-rule create --server {nombre} ...`". El alumno copia, ajusta el parámetro final, lo ejecuta. El feedback es directamente accionable.

**La severidad sigue una jerarquía operativa clara**: Crítico (bloquea producción, vulnerable inmediato), Alto (debe arreglarse en el sprint), Medio (en el siguiente), Bajo (informativo). El `InformeAudit` cuenta cuántos hay de cada nivel para que el pipeline pueda bloquear según política: por ejemplo, "si hay Críticos, pipeline rojo; si hay Altos, pipeline amarillo".

**La función no llama a Azure; recibe los estados**. Eso permite testearla sin tokens ni suscripción. En producción, una capa intermedia hace `az resource list` y rellena los `EstadoRecurso`; en tests, los `EstadoRecurso` se construyen a mano con los datos que quieras. Misma función pura, dos contextos de uso.

---

## 6. La integración con M08-S8.5: dos manos en el mismo problema

El submódulo es deliberadamente complementario a M08-S8.5 (IaC con Bicep). Vale la pena tener clara la división:

| Capa | M08-S8.5 | M09-S9.3 |
| --- | --- | --- |
| Quién escribe el Bicep | El developer a mano | Claude Code lo genera |
| Cómo se valida | Linter de Bicep + `what-if` | Audit checker pre-merge + `what-if` |
| Cuándo se usa | Adopción inicial de IaC, control fino | Aceleración del trabajo IaC en equipo maduro |
| Riesgo principal | Curva de aprendizaje de Bicep | Bicep generado sin convenciones del equipo |
| Mitigación | Linter de S8.5 captura anti-patterns | Audit checker + prompts canónicos con tags y MI |

La forma honesta de adoptar las dos: **empieza con M08-S8.5 para entender Bicep a fondo** (sintaxis, módulos, AVM, what-if), **luego usa M09-S9.3 para acelerar** una vez que dominas el formato. Si arrancas directamente con Claude Code generando Bicep sin entender lo que produce, te encuentras con código que funciona hasta que un día deja de funcionar y no sabes por qué.

Y los dos se cruzan en un punto concreto: el `bicep build` que viste como integración real con `SkippableFact` en M08-S8.5 es la herramienta natural para validar el output de Claude Code en este submódulo. El pipeline IaC del equipo termina con: Claude genera → `bicep build` valida sintaxis → audit checker valida reglas → `what-if` valida cambios → humano aprueba → deploy.

---

## 7. Cómo probarlo en local

Es un ejemplo offline al 100%. Claude Code no se invoca desde el ejemplo; el ejemplo modela las decisiones que harías al usarlo.

```bash
dotnet run --project src/ClaudeCode.Infra.Demo.Api
# http://localhost:5115
```

Endpoints:

```http
### Parsear requisitos en lenguaje natural
POST http://localhost:5115/infra/requisitos
Content-Type: application/json

"Necesito un App Service con Cosmos DB en West Europe y North Europe, GDPR, slots de staging, autoscale en horas pico. Storage para los uploads."
# → recursos: [AppService, CosmosDb, Storage]
#   multiRegion: true, complianceEuropa: true, conSlots: true, conAutoscale: true
#   avisos: ["No se mencionó HTTPS only...", "Sin Managed Identity declarada...",
#            "Multi-region + GDPR: confirma...", "Storage detectado: cierra acceso público..."]

### Obtener el prompt canónico de un escenario
GET http://localhost:5115/infra/prompt/BicepDesdeRequirements
# → prompt con placeholder para requisitos

GET http://localhost:5115/infra/prompt/ReverseArmABicep
# → prompt para reverse engineering con verificación what-if obligatoria

### Auditar una lista de recursos
POST http://localhost:5115/infra/audit
Content-Type: application/json

[
  {
    "nombre": "miapp-web",
    "tipo": "Microsoft.Web/sites",
    "httpsOnly": false,
    "tieneManagedIdentity": false,
    "tieneTags": true,
    "tlsVersion": "1.0"
  }
]
# → 3 hallazgos: Crítico (HTTPS), Alto (MI), Alto (TLS 1.0)

### Plan completo (parser + dos prompts + audit + checklist)
POST http://localhost:5115/infra/plan
Content-Type: application/json

{
  "descripcionRequisitos": "App Service con Cosmos en UE",
  "recursosExistentes": []
}
```

Los 36 tests cubren cada rama del parser (detección de recursos sin duplicar, multi-region por inferencia, GDPR, avisos automáticos por cada caso), cada uno de los 7 escenarios del builder (con el contenido característico esperado), y el audit checker con cada regla y la severidad correcta.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La conversación con el equipo: ¿genera Claude el Bicep entero o partes?

Hay dos formas legítimas de adoptar este patrón en un equipo, y conviene que el alumno entienda los trade-offs:

**Adopción tipo "Claude genera todo, yo reviso"**: el alumno describe los requisitos, copia el prompt canónico, Claude devuelve `main.bicep` + módulos + `params`. El developer revisa el diff completo, ejecuta `bicep build` + `what-if`, audit checker, mergea si todo verde. **Ventaja**: velocidad. Un proyecto IaC nuevo en pocas horas en vez de varios días. **Desventaja**: si el developer no entiende a fondo lo que Claude generó, la primera vez que haya que modificar el Bicep, va a pedir a Claude que lo modifique sin entender la decisión original. Acumulación de deuda cognitiva.

**Adopción tipo "Claude para piezas concretas, yo coso"**: el developer escribe el `main.bicep` y los `modules/` que entiende, y pide a Claude solo las piezas concretas (un módulo nuevo, un parámetro complejo, un Bicep AVM que no recuerda la sintaxis). **Ventaja**: el developer mantiene el modelo mental del proyecto. Cualquier modificación futura la entiende. **Desventaja**: la aceleración es menor, especialmente al arrancar un proyecto desde cero.

La elección no es ideológica; depende del nivel de madurez del equipo con Bicep. Equipos que ya dominan Bicep (terminaron M08-S8.5 con soltura) pueden adoptar la primera. Equipos que están aprendiendo deberían empezar por la segunda y migrar conforme ganan confianza. El submódulo no impone una decisión, pero el flujo `requisitos → prompt → Bicep → audit` está pensado para que las dos sean viables.

---

## 9. La conversación con seguridad: el audit checker como gate

Si tu equipo tiene un proceso de revisión de seguridad antes de cada deploy a producción, el audit checker es la pieza que te ahorra reuniones. El patrón típico:

1. El alumno hace un PR con cambios de Bicep.
2. El pipeline de CI ejecuta `bicep build` + `az deployment group what-if` + audit checker.
3. Si hay hallazgos Críticos, **el pipeline falla** y el PR no se puede mergear.
4. Si hay hallazgos Altos, el pipeline pasa pero genera un comentario en el PR listando los hallazgos para que el reviewer los discuta.
5. Si solo hay Medios o Bajos, el reviewer los valora caso por caso.

Esta política convierte el audit en un **gate determinístico**, no en una conversación opinable. Cuando seguridad reciba un PR para revisar, ya viene con la garantía de que las cinco reglas mínimas están cumplidas. La revisión humana se centra en lo que la herramienta no puede automatizar (lógica de negocio, arquitectura, decisiones de coste).

Y un detalle operativo: el audit checker NO está pensado para auditar la infra existente en producción una vez al mes (para eso está Microsoft Defender for Cloud con su Secure Score, viste en S6.1). Está pensado para **auditar los cambios antes de aplicarlos**, como hook en el pipeline. La granularidad es por PR, no por suscripción.

---

## 10. Anti-patterns

Cinco prácticas que evitar:

**Anti-pattern 1: pedir Bicep sin convenciones del equipo en el prompt.** Si el prompt no menciona Managed Identity, tags obligatorios, AVM modules o naming convention, Claude usará el estándar de internet, que casi nunca coincide con el de tu equipo. Empieza por el prompt canónico del ejemplo; añade detalles específicos de tu organización.

**Anti-pattern 2: aceptar el Bicep generado sin `what-if`.** Es la misma regla de oro de M08-S8.5: nunca aplicar Bicep sin ejecutar primero `az deployment group what-if`. Da igual quién lo escribió (humano o Claude); la verificación es la misma. Si aparece un `Delete:` de un recurso stateful, paras.

**Anti-pattern 3: reverse engineering sin verificación final.** El prompt `ReverseArmABicep` incluye la verificación obligatoria con `what-if`. Saltársela equivale a confiar ciegamente en que la modularización de Claude no ha cambiado semánticamente el Bicep. En la práctica, sí cambia (suele ser una decisión de naming que parece cosmética y mueve un recurso). Verifica siempre.

**Anti-pattern 4: ignorar los avisos del parser.** Si describes "App Service con Cosmos DB" y el parser te dice "Sin Managed Identity declarada", añadirla al prompt cuesta cinco segundos. Mandar el prompt sin ese aviso te va a generar el caso 1 de la sección 2 dos semanas después.

**Anti-pattern 5: audit checker como auditoría puntual en lugar de gate de pipeline.** Si ejecutas el audit una vez al mes "para ver cómo va la infra", funciona pero llega tarde. Como hook en el pipeline IaC, **bloquea las regresiones antes de que entren**. La diferencia es operacionalmente enorme.

---

## 11. Glosario breve

- **IaC** (Infrastructure as Code): infraestructura declarada en archivos versionados. Reproducible, auditable, revisable en PRs.
- **Bicep**: DSL de Microsoft para Azure. Sucesor de ARM Templates. Sintaxis más limpia, módulos nativos, soporte oficial.
- **AVM** (Azure Verified Modules): catálogo oficial de módulos Bicep mantenidos por Microsoft. Referencia con `br/public:avm/...`.
- **`az bicep decompile`**: comando que convierte un ARM Template JSON a Bicep. Punto de partida para reverse engineering.
- **`az deployment group what-if`**: previsualización de los cambios que un deployment aplicaría. Obligatorio antes de cada apply.
- **`@secure()`**: decorador de Bicep que marca un parámetro como sensible. No se logea, no se almacena en plain text.
- **Key Vault Reference**: sintaxis `@Microsoft.KeyVault(...)` que App Service y Functions resuelven en runtime contra Key Vault.
- **Managed Identity (MI)**: identidad del recurso de Azure, sin connection string ni secreto. RBAC asignado directamente al recurso.
- **OIDC** (en pipelines): federated credentials entre GitHub Actions y Entra ID. Reemplaza al Service Principal con secret.
- **Auto-rollback**: swap inverso automatizado con `condition: failure()` que recupera la versión anterior si el smoke test post-deploy falla.
- **Severidad del hallazgo**: Crítico (bloquea producción), Alto (sprint), Medio (siguiente sprint), Bajo (informativo).
- **Reverse engineering** (de infra): reconstruir el código IaC a partir de recursos existentes creados a mano. `az group export` + `az bicep decompile` + Claude Code para modularizar.

---

## 12. Cierre

Si te quedas con una sola idea de S9.3: **el aparejador no sustituye al inspector**. Claude Code te genera el Bicep en minutos, pero la responsabilidad de que ese Bicep cumpla las reglas mínimas sigue siendo tuya, y la herramienta para verificarlo en automático es el audit checker. Sin ese gate en el pipeline, la velocidad que ganas en generación la pierdes en incidentes de compliance dos semanas después.

Lo siguiente es [`S9.4 — MCP y herramientas externas`](../S9.4-mcp-herramientas/MANUAL.md), donde Claude Code deja de operar solo sobre tu filesystem y empieza a hablar con GitHub, Notion, bases de datos y otros sistemas via Model Context Protocol.
