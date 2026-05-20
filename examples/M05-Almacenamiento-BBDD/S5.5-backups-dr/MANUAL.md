# Manual del alumno — S5.5 · Backups, replicación y DR

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Útil cuando vas a tocar código. Este manual va antes: te cuenta para qué existe el ejemplo, qué decisiones quiere enseñarte y cómo leerlo. Cuando termines, abre el README y todo encajará más rápido.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M05-S5.5](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.5-backups-v3.md) (~27 slides). Las primeras cuatro secciones son el marco mental; de la sección 5 a la sección 8 entras al detalle técnico; el resto es práctica, autoevaluación y un par de avisos antes de pasar a la práctica S5.P.

*Creado: 2026-05-20 00:02 +0200*

---

## 1. La idea en una frase

S5.4 te enseñó a evitar incidentes de seguridad con identidad sin secretos. S5.5 es la otra cara de la moneda: **qué pasa cuando los incidentes ocurren igual**. Porque van a ocurrir. Tarde o temprano alguien borra un container, alguien hace `DROP TABLE` en producción, una región entera se cae, una storage account amanece corrupta. La pregunta que cierra M05 no es "¿cómo evitar el desastre?" — es "¿cómo recuperarte de él en cinco minutos en vez de en cinco días?".

Aquí ya no escribes mucho código de aplicación: lo importante son **tres decisiones de diseño** que se toman una vez y se prueban (esto es lo que más cuesta) una vez al trimestre. Backup por servicio, estrategia de DR según criticidad, retención según regulación. Las tres se codifican como advisors puros en el ejemplo, y un planificador las compone en un plan completo para tu sistema.

---

## 2. El problema real que hay detrás

Hace unos meses pasé una madrugada entera con un cliente que descubrió, en una auditoría, que sus "backups" no eran lo que creían. El equipo decía tener backups desde 2019. La auditoría pidió restaurar un punto concreto. Empezamos a buscar: el punto de restauración más reciente disponible era de **cuatro meses atrás**. Los snapshots automáticos se habían ido borrando por una lifecycle policy que nadie revisó cuando la cambiaron, y los manuales se hacían "cuando alguien se acordaba". Lo que llamaban backup era una esperanza con fecha.

La cita que cierra la slide 27 del submódulo lo resume perfecto:

> *"Un backup que nunca se ha restaurado no es un backup — es una esperanza."*

Y por eso este submódulo no va de configurar el backup, va de **diseñar el plan completo de DR** y, sobre todo, de **probarlo periódicamente**. La parte que casi nadie hace. La parte donde se cazan los problemas antes de que sean problemas.

El ejemplo pone tres clases puras y un servicio compositor que te ayudan a generar un plan razonado:

| Necesidad real | Cómo se resuelve | Dónde lo verás |
| --- | --- | --- |
| Saber qué backup trae cada servicio "de fábrica" y qué tienes que configurar tú | Tabla servicio → característica | [`BackupPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/BackupPolicyAdvisor.cs) |
| Elegir estrategia de DR según criticidad y comprobar si cumple SLA | Tabla criticidad → estrategia + verificación RPO/RTO | [`RpoRtoCalculator.cs`](src/Dr.Demo.Api/Dr/RpoRtoCalculator.cs) |
| Mapear cada regulación a sus años + WORM + derecho al olvido | Tabla régimen → requisito de retención | [`RetentionPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/RetentionPolicyAdvisor.cs) |
| Generar el plan completo de un sistema y avisar de los huecos | Servicio que compone los tres | [`IDrPlanner.cs`](src/Dr.Demo.Api/Dr/IDrPlanner.cs) |

Las tres clases puras son tablas de decisión codificadas. El planner solo orquesta. Y entre los avisos del plan está justamente lo que más se pasa por alto: *"este servicio no tiene backup automático, configúralo tú"*, *"esta estrategia no cumple tu RTO objetivo, sube de estrategia o ajusta el SLA"*.

---

## 3. Por qué esto importa en tu stack

DR es la parte aburrida de Azure. Configurar Continuous Backup en Cosmos, activar soft delete en Blob, definir LTR en SQL — todo eso son cinco clics que se hacen una vez y no vuelves a tocar. Y precisamente por eso se olvida: como no genera trabajo continuo, nadie le presta atención hasta el día que importa, y ese día el plan tiene huecos.

El cambio respecto a S5.4: si S5.4 te enseñaba **una sola decisión transversal** (cómo te conectas), S5.5 te enseña **un plan compuesto**. La pregunta deja de ser "¿qué hago yo?" y pasa a ser "¿qué hace Azure por mí en cada uno de los servicios que uso, qué tengo que configurar yo encima, y cómo orquesto la respuesta cuando algo falla?". El advisor + planner del ejemplo te da exactamente esa vista compuesta.

Y respecto a la mecánica del ejemplo: la API no toca Azure ni siquiera de manera opcional. Es 100% lógica pura — no hay endpoint "demo real" como en S5.4. El walkthrough de soft delete (subir un blob, borrarlo, recuperarlo con `undelete`) está en los scripts `az` del repo, no en endpoints. El valor del ejemplo es la **decisión razonada**, no el round-trip.

---

## 4. El modelo mental: el seguro del coche

Pagas todos los meses por el seguro de tu coche. Si nunca tienes un accidente, parece tirar el dinero. Y entonces un día, en la rotonda equivocada, te das contra otro vehículo. Te miras, miras al otro, y la conversación que tienes en la cabeza no es *"¿tengo seguro?"* — es *"¿qué cubre?"*. ¿Es a terceros o a todo riesgo? ¿Cuánta franquicia? ¿Tengo coche de sustitución? ¿En cuánto tiempo me lo arreglan? Los detalles importan, y los lees mientras esperas a la grúa.

Eso es backup y DR en Azure. La pregunta no es "¿tienes backup?" — la respuesta es casi siempre sí, porque Cosmos, SQL y Key Vault hacen backup automático por defecto. La pregunta real, la incómoda, es:

- **¿Qué cubre?** ¿Hasta cuándo puedo restaurar — siete días, treinta y cinco, diez años? ¿Qué incluye y qué no? Blob no tiene backup automático, Table tampoco — eso lo cubres tú o no lo cubre nadie.
- **¿En cuánto tiempo?** Esto es **RPO** (cuántos minutos de datos pierdes en el peor caso) y **RTO** (cuánto tiempo está caído el servicio mientras restauras). De segundos a horas según lo que pagues.
- **¿Cómo se reclama?** ¿Tienes un runbook escrito? ¿Quién lo ejecuta? ¿Lo has probado? Igual que con el seguro: la primera vez que vas a usarlo no debería ser el día del accidente.
- **¿Cuánto cuesta la cobertura?** Estrategias hay tres y el coste varía hasta el doble entre la mínima y la máxima. La criticidad del negocio decide.

Tres frases para fijar el modelo:

- **El backup automático de Azure es a terceros.** Cubre lo básico —el motor se cae, lo levantan—, pero no te protege de errores humanos (un `DELETE` mal) ni de pérdidas largas (más allá del límite de retención). Para eso necesitas configurar soft delete, versioning, LTR.
- **RPO es minutos perdidos, RTO es minutos caídos.** Los dos números son del negocio, no de Azure. Tienes que saber cuánto puedes asumir antes de elegir estrategia.
- **El plan se prueba, o no es plan.** Restaurar una vez al trimestre a un recurso temporal y verificar que los datos son correctos. Eso convierte una esperanza en un backup real.

Vuelve a la imagen del seguro cada vez que veas "PITR", "soft delete" o "DR strategy". Te ahorra clics y te ayuda a hacer las preguntas correctas.

---

## 5. Qué trae cada servicio "de fábrica"

[`BackupPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/BackupPolicyAdvisor.cs) codifica la slide 3 como tabla de decisión. Para cada servicio, qué hace Azure por ti y qué tienes que configurar encima. Resumen práctico:

| Servicio | Backup automático | Retención | PITR | Configuración manual |
| --- | :---: | --- | :---: | :---: |
| **Cosmos DB** | sí | Continuous 7/30 d (o periodic 4 h) | sí | no |
| **Azure SQL** | sí | 7-35 d + LTR opcional hasta 10 años | sí | no |
| **Blob Storage** | **no** | soft delete + versioning configurable | sí (con versioning) | **sí** |
| **Table Storage** | **no** | — | no | **sí** (export a mano) |
| **Queue Storage** | **no** | — | no | **sí** (mensajes efímeros por diseño) |
| **App Service** | **no** | configurable (Standard+) | no | **sí** o redeploy desde IaC |
| **Key Vault** | sí | soft delete 7-90 d + purge protection | no | no |

Léelo despacio. Lo importante es lo que aparece en **negrita**: los servicios donde **tú** eres responsable. Blob, Table, Queue y App Service no tienen "Azure ya hace tu backup automáticamente". Son los que más se olvidan y los que más sorpresas dan en una auditoría.

Cosmos y Azure SQL son los hermanos buenos: backup continuo desde el día uno, PITR a la resolución de segundos. Lo que tienes que configurar es la **retención larga** si compliance lo exige (Long-Term Retention de SQL hasta 10 años; Continuous Backup en Cosmos eligiendo 7 o 30 días). Key Vault tampoco pide nada — tiene soft delete y geo-redundancia incluidas; lo único que sí hay que activar en producción es **purge protection** para que ni siquiera un Owner pueda forzar el borrado definitivo dentro del periodo de soft delete.

> 🧠 **Blob es el que más sorpresas da.** Es el servicio con más uso casual ("guardo unas imágenes ahí") y, simultáneamente, el que menos protección trae de fábrica. Activa **soft delete** (30 días típicos) y **versioning** desde el día uno. Si trabajas con compliance, encima **immutability policies** (WORM) sobre los containers críticos. El walkthrough completo está en `scripts/02-smoke-test.sh` del propio ejemplo: subir blob → borrar → `az storage blob undelete` lo recupera. Pruébalo una vez y se queda grabado.

El endpoint `GET /dr/backup/{servicio}` te devuelve la ficha de cualquiera de los siete servicios. Útil cuando estás diseñando un sistema y necesitas saber rápido qué te va a venir incluido y qué tienes que añadir.

---

## 6. RPO, RTO y el coste real del DR

Aquí está la decisión que más dinero te cuesta —y la más mal entendida en proyectos reales. [`RpoRtoCalculator.cs`](src/Dr.Demo.Api/Dr/RpoRtoCalculator.cs) codifica las tres estrategias de la slide 24:

| Estrategia | RPO típico | RTO típico | Coste vs base |
| --- | --- | --- | --- |
| **Active-Active** | < 1 segundo | < 1 minuto | ~2× |
| **Warm Standby** | 5-15 min | 15-60 min | ~1.4× |
| **Cold Standby** | horas | 4-24 horas | ~1.05-1.1× |

**RPO** (*Recovery Point Objective*) es cuántos minutos de datos puedes asumir perder en el peor caso. **RTO** (*Recovery Time Objective*) es cuántos minutos puedes asumir estar caído mientras restauras. Los dos números son del **negocio**, no de Azure. Te los tiene que dar el dueño del producto, y suelen ser muy distintos de lo que el equipo técnico asume.

Para fijar lo que es cada estrategia con un escenario:

- **Active-Active** — Cosmos DB con escrituras multi-región, App Service en dos regiones tras Front Door. Si una región se cae, la otra sigue sirviendo sin que el usuario lo note. Carísimo y solo se justifica para servicios donde perder un minuto cuesta más que la factura mensual de DR — banca online, plataformas de trading, hospitales.
- **Warm Standby** — la región secundaria existe pero está dimensionada al mínimo. Cuando hay failover, escalas la secundaria y rediriges. Diez a treinta minutos de transición pero un coste muy razonable. Es la respuesta típica para producción seria sin presupuesto ilimitado.
- **Cold Standby** — el DR es "redeploy desde pipeline + IaC en otra región". La infraestructura no existe hasta que la creas. Coste casi cero hasta que la usas, pero el RTO se mide en horas. Apropiado para servicios internos o productos que pueden estar caídos un día.

`Recomendar` mapea criticidad → estrategia: *Misión Crítica* → Active-Active, *Importante* → Warm Standby, *Interno* → Cold Standby. Y `CumpleObjetivos` —la slide 22— responde a la pregunta que de verdad importa: *"con esta estrategia, ¿cumplo el RPO/RTO que me ha pedido el negocio?"*. Si te piden RTO de 10 minutos y tu estrategia es Cold Standby (RTO de horas), la función devuelve `false` y el planner mete un aviso en el plan. **Esa función pura es la diferencia entre un plan razonado y un plan optimista.**

> 🧠 **El número que casi nadie mide en producción.** La slide 22 lo dice sin rodeos: el RPO y RTO que pone tu documento de DR son **objetivos**. El RPO y RTO **reales** son los que mides cuando ejecutas un game day. Y casi siempre son peores que los del papel. Documentar RPO/RTO medido, no asumido, es una de las prácticas que más madurez señala en un equipo.

---

## 7. Compliance: retención, WORM y derecho al olvido

[`RetentionPolicyAdvisor.cs`](src/Dr.Demo.Api/Dr/RetentionPolicyAdvisor.cs) codifica los regímenes regulatorios más comunes (slide 20). La tabla resumida:

| Regulación | Años | WORM | Derecho al olvido |
| --- | :---: | :---: | :---: |
| SEC 17a-4 / FINRA | 6 | sí | no |
| HIPAA (sanitario US) | 6 | no | no |
| Sarbanes-Oxley (financiero) | 7 | no | no |
| FDA 21 CFR Part 11 | permanente | sí | no |
| PCI DSS (cardholder data) | 1 (mínimo) | no | no |
| RGPD (europeo) | 0 | no | **sí** |
| Tax España | 6 | no | no |
| Legal España | 30 | no | no |

Hay tres ideas distintas a separar:

- **Años de retención mínima**. Lo que la ley te obliga a guardar. Por debajo, multa.
- **WORM** — *Write Once, Read Many*. El documento, una vez escrito, no se puede modificar ni borrar durante el periodo. SEC y FDA lo exigen. En Azure se implementa con **immutability policies** sobre containers de Blob: una policy bloqueada no la salta ni un administrador. La función `DiasInmutabilidad(regimen)` te devuelve el periodo en días directamente listo para configurar la policy (`SEC = 7 años ≈ 2555 días`).
- **Derecho al olvido**. El opuesto de WORM. RGPD obliga a poder **borrar** los datos personales de un sujeto cuando lo solicita. Si tu sistema usa backups de retención larga, tienes un problema: cómo "olvidar" a alguien si el dato sigue vivo en backups de hace dos años. La recomendación práctica para RGPD es **retención corta de backups** (30-90 días típicos) más procedimientos documentados de re-anonimización.

> 🧠 **WORM y "derecho al olvido" son requisitos contradictorios.** Si tu sistema procesa datos personales de europeos (RGPD) y a la vez documentos financieros sujetos a SEC, **no puedes meterlos en el mismo container**. Los financieros van con immutability policy a 6 años; los personales con retención corta y proceso de borrado. Esa separación tiene que estar en el diseño de Storage, no como ocurrencia tardía. Lo verás más a fondo si trabajas en fintech europea — el cruce entre regulaciones es donde se complica.

El endpoint `GET /dr/retencion/{regimen}` te da el requisito de cualquier régimen y, si aplica, los días para la policy de inmutabilidad. Útil cuando defines el container de Blob de un sistema regulado.

---

## 8. El plan completo: componer los tres advisors

[`IDrPlanner.cs`](src/Dr.Demo.Api/Dr/IDrPlanner.cs) es el único servicio inyectable del ejemplo. No es lógica de negocio nueva: es la **orquestación** de los tres advisors anteriores para generar un plan de DR completo para un sistema. La firma:

```csharp
public interface IDrPlanner
{
    PlanDr Generar(
        Criticidad criticidad,
        IReadOnlyList<ServicioAzure> servicios,
        int rpoObjetivoMin,
        int rtoObjetivoMin);
}
```

Le pasas la criticidad del sistema, la lista de servicios que usa y los objetivos RPO/RTO del negocio. Te devuelve un plan con:

- La estrategia recomendada por la criticidad (Active-Active / Warm / Cold).
- El perfil con RPO/RTO reales de esa estrategia.
- Si **cumple** o no los objetivos del negocio (slide 22).
- Una línea por servicio con su característica de backup (qué hace Azure, qué tienes que configurar).
- Los **avisos** automáticos: cada servicio que requiere configuración manual y, si aplica, el aviso de "tu estrategia no cumple el SLA pedido".

Mira `DrPlanner.Generar` en [`IDrPlanner.cs`](src/Dr.Demo.Api/Dr/IDrPlanner.cs): es lo más sencillo del mundo, treinta líneas. Pero su valor está exactamente ahí — codifica lo que normalmente vive en la cabeza de un consultor caro o en un documento de Word que nadie actualiza. Y el resultado es estructurado, repetible y testeable.

> 🎓 **Por qué `IDrPlanner` es interfaz y no clase estática.** Las tres clases anteriores son `static` puras (sin estado). El planner es `interface + clase` registrada en DI. ¿Por qué la diferencia? Porque el planner es el **seam del test de contenedor**. La CAPA 0 resuelve `IDrPlanner` del `WebApplicationFactory` real, comprueba que es la misma instancia singleton y verifica que genera un plan coherente. Sin esa pieza, no tendrías nada que cruzar en el contenedor — solo clases estáticas que no requieren resolverse. La interfaz no aporta abstracción real (no hay otra implementación), aporta **seam para DI**, que es lo que valida el grafo en runtime.

El endpoint `POST /dr/plan` lo expone con un DTO simple. Prueba estos dos casos en `api.http` y compara las respuestas — uno cumple objetivos, otro no, y los avisos cuentan exactamente la diferencia:

```json
// Cumple:    Importante → WarmStandby (RTO 15-60 min) y pides RTO 60 → OK
{ "criticidad": "Importante", "servicios": [...], "rpoObjetivoMin": 15, "rtoObjetivoMin": 60 }

// No cumple: Interno → ColdStandby (RTO 4-24 h) y pides RTO 10 min → AVISO
{ "criticidad": "Interno",    "servicios": [...], "rpoObjetivoMin": 5,  "rtoObjetivoMin": 10 }
```

---

## 9. Recorrido guiado: tu primer plan de DR

Lanza la API (ver sección 11) y abre [`api.http`](src/Dr.Demo.Api/api.http). **Todo el ejemplo funciona offline** — no hay endpoints contra Azure.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `GET /dr/backup/CosmosDb` | `BackupAutomatico: true`, PITR, sin config manual | Cosmos viene cubierto de fábrica (sección 5). |
| 2 | `GET /dr/backup/TableStorage` | `BackupAutomatico: false`, sin PITR, requiere config manual | Table no tiene backup — eres tú o nadie (sección 5). |
| 3 | `GET /dr/backup/BlobStorage` | requiere config manual, soft delete + versioning | Blob es el "tienes que configurarlo" más común. |
| 4 | `GET /dr/rpo-rto/WarmStandby` | `Rpo: "5-15 min", Rto: "15-60 min", MultiplicadorCoste: 1.4` | Estrategia media — buena calidad-precio para producción (sección 6). |
| 5 | `GET /dr/rpo-rto/ColdStandby` | `Rpo: "horas", Rto: "4-24 h", MultiplicadorCoste: 1.05` | Estrategia barata pero RTO en horas. |
| 6 | `GET /dr/retencion/SecFinra` | 6 años, WORM, días inmutabilidad = 2190 | SEC exige WORM; el valor está listo para policy de Blob (sección 7). |
| 7 | `GET /dr/retencion/Rgpd` | derecho al olvido, días inmutabilidad = 0 | RGPD: el opuesto de WORM (sección 7). |
| 8 | `POST /dr/plan` con criticidad Importante, RPO 15, RTO 60 | `CumpleObjetivos: true`, sin avisos críticos | Plan coherente para un sistema importante. |
| 9 | `POST /dr/plan` con criticidad Interno, RPO 5, RTO 10 | `CumpleObjetivos: false`, aviso de slide 22 | El plan detecta el mismatch — sin esto, el documento sería optimista. |

Un experimento útil: prueba el paso 9 con criticidad `MisionCritica` (que recomienda Active-Active). Te dará `CumpleObjetivos: true` porque Active-Active cumple cualquier RPO/RTO razonable. Y ahora suma el `MultiplicadorCoste` — verás que estás pidiendo el doble de coste para "una app interna" sin que el negocio probablemente lo justifique. **El plan no decide por ti; te enseña los trade-offs.**

Y para el walkthrough operativo real, mira `scripts/02-smoke-test.sh` del propio ejemplo. Subes un blob, lo borras, lo recuperas con `az storage blob undelete`. Borras un container completo y lo restauras. Los cinco comandos que cierran la lección de la slide 19. Pruébalos una vez contra un Storage real con soft delete activado y se queda grabado.

---

## 10. Por qué el código y los tests están así

Estructura: tres advisors puros (sin estado) y un planner inyectable (compone los tres). Más cuatro endpoints finos en `DrEndpoints.cs`. Eso es todo. Sin clientes Azure, sin retry, sin singletons complejos — el ejemplo es de **decisión razonada**, no de round-trip.

Los tests tienen **dos capas**:

- **CAPA 1 · Unit** — `Unit_BackupPolicyAdvisorTests` (los siete servicios y sus características), `Unit_RpoRtoCalculatorTests` (las tres estrategias × perfil × verificación de objetivos), `Unit_RetentionPolicyAdvisorTests` (los nueve regímenes y sus días de inmutabilidad). Puro, sin red, sin Azure.
- **CAPA 0 · DI** — `DiContainer_Tests`. Resuelve `IDrPlanner` del `WebApplicationFactory` real, verifica el singleton y genera un plan completo comprobando que el aviso de la slide 22 aparece cuando la estrategia no cumple objetivos. Es la slide 22 *como test concreto*.

Como en S5.4, **no hay capa de integración a propósito**. Y por la misma razón:

> 🎓 **Backups, PITR y failover no son emulables.** No existe un emulador local que firme tokens RBAC, ejecute un point-in-time-restore real de Cosmos, replique un container de Blob a una región par y haga failover. Todo eso exige Azure real. Una `SkippableFact` que siempre se saltase sería deshonesta. Mejor reconocer que la capa no existe y desplazar la verificación real al walkthrough manual con `scripts/02-smoke-test.sh` contra un Storage real. La regla del módulo: ¿se puede emular? Sí → integración con SkippableFact (S5.1/S5.2/S5.3). No → CAPA 1 + CAPA 0, y el manual cuenta el porqué (S5.4 / S5.5).

El resultado: **31 pass, 0 skip, 0 fail**. La suite siempre verde, sin Docker, sin Azure, sin condiciones.

---

## 11. Puesta en marcha, ejecución y pruebas

Sección operativa. Datos verificados contra el repo.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) | compilar y ejecutar | Sí |
| Cliente REST | extensión *REST Client* de VS Code o `curl` | lanzar [`api.http`](src/Dr.Demo.Api/api.http) | Recomendado |
| Suscripción Azure | cualquier suscripción con permisos para crear Storage | el walkthrough de soft delete (`scripts/02-smoke-test.sh`) | Opcional (solo para el walkthrough) |

No hay emulador, no hay Docker, no hay dependencias externas para el ejemplo en sí. Es 100% offline.

### 11.2 Compilar

```bash
cd examples/M05-Almacenamiento-BBDD/S5.5-backups-dr
dotnet build Dr.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings** (`TreatWarningsAsErrors=true`).

### 11.3 Lanzar la API

```bash
dotnet run --project src/Dr.Demo.Api
```

- Escucha en **`http://localhost:5085`** ([`launchSettings.json`](src/Dr.Demo.Api/Properties/launchSettings.json), perfil `http`).
- Prueba de vida: `GET http://localhost:5085/health` → `{ "status": "ok" }`.

Todos los endpoints `/dr/*` funcionan sin configuración adicional.

### 11.4 Ejercitar el ejemplo

```bash
# Ficha de backup de Cosmos DB
curl http://localhost:5085/dr/backup/CosmosDb

# Perfil RPO/RTO de Warm Standby
curl http://localhost:5085/dr/rpo-rto/WarmStandby

# Requisito de retención de RGPD
curl http://localhost:5085/dr/retencion/Rgpd

# Plan de DR completo
curl -X POST http://localhost:5085/dr/plan -H "Content-Type: application/json" \
  -d '{ "criticidad":"Importante",
        "servicios":["CosmosDb","AzureSql","BlobStorage","TableStorage","AppService"],
        "rpoObjetivoMin":15, "rtoObjetivoMin":60 }'
```

La sección 9 tiene el guion completo con qué demuestra cada paso.

### 11.5 Walkthrough real de soft delete (opcional)

Cuando quieras tocar Azure de verdad, los scripts del ejemplo te llevan paso a paso:

```bash
cd scripts
cp .env.demo.example .env.demo   # edita con tu RG, ubicación, sufijo
./demo.sh                        # menú: provisionar → walkthrough → cleanup
```

`01-provision.sh` crea un Storage con soft delete + versioning activados. `02-smoke-test.sh` ejecuta el walkthrough de la slide 19: sube un blob → bórralo → `az storage blob undelete` lo recupera; crea un container → bórralo → restauralo. Cinco minutos, ~0 € de coste (StorageV2 con pocos KB). `03-cleanup.sh` borra el RG entero cuando termines.

### 11.6 Pasar los tests

```bash
dotnet test Dr.Demo.slnx
```

Resultado esperado: **31 pass · 0 skip · 0 fail**. Ni con Docker ni sin Docker cambia (sección 10).

### 11.7 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| El build falla por un warning | `TreatWarningsAsErrors=true` | corrige el warning; aquí no se silencian |
| El puerto 5085 está ocupado | otra app lo usa | ciérrala o cambia `applicationUrl` en `launchSettings.json` |
| Tests con conteo distinto a 31 | recientemente añadidos / fork | confirma contra el README |
| `scripts/02-smoke-test.sh` falla en `undelete` | soft delete no estaba activado al subir el blob | el script activa soft delete con `01-provision.sh` antes; ejecútalos en orden |

### 11.8 Despliegue por Portal

El detalle del despliegue —activar Continuous Backup en Cosmos, Long-Term Retention en SQL, soft delete + versioning en Blob, immutability policies WORM, App Service con backup o redeploy desde IaC— está en el [`README.md`](README.md). Este manual no lo repite — para tocar Azure, el README es la referencia.

---

## 12. Checklist de DR (y de qué te protege cada línea)

Adapta tu propio checklist a partir de este. Por cada servicio que use tu sistema:

| Casilla | De qué te protege |
| --- | --- |
| Cosmos DB: Continuous Backup activado (7 o 30 días) | Pérdida de datos por error humano dentro de la ventana |
| Azure SQL: PITR activo (por defecto) + LTR si compliance | Lo mismo en relacional + retención larga para auditoría |
| Blob: soft delete + versioning + GRS/GZRS | Errores humanos + pérdida de región |
| Blob (compliance): immutability policy WORM en containers críticos | Borrado o modificación de documentos legales |
| Key Vault: purge protection activado en producción | Borrado definitivo dentro del periodo de soft delete |
| Table / Queue: estrategia de export manual definida y automatizada | "No teníamos backup" — el clásico |
| App Service: redeploy desde IaC + pipeline funcionando | Tener que reconstruir a mano en otra región |
| Estrategia de DR (Active/Warm/Cold) elegida con criticidad real | Pagar 2× sin justificación o quedarse corto en el día del desastre |
| RPO/RTO objetivos del negocio documentados y firmados | Que cada uno tenga un número distinto en la cabeza |
| Game day trimestral con restore validado | "Un backup que nunca se ha restaurado no es un backup" |
| Alertas de fallo de backup configuradas | Enterarte el día del desastre de que llevas tres meses sin backups |
| Runbook escrito y guardado fuera del sistema afectado | Que el runbook esté en SharePoint que también está caído |

---

## 13. Ideas para llevarte

La regla de oro de este submódulo (slide 27) es la única que de verdad importa: **un backup que nunca se ha restaurado no es un backup, es una esperanza**. Si tu equipo no tiene un game day trimestral donde restauréis a un recurso temporal y validéis los datos, no tenéis plan de DR. Tenéis documentación. Y el día del incidente, la documentación no sirve de nada.

Sobre las **tres estrategias**, la recomendación que defiendo: empieza por **Warm Standby** para todo lo que sea producción seria. Active-Active solo si el negocio realmente paga la factura (banca, trading, sanidad crítica). Cold Standby para herramientas internas donde un día caído es asumible. El coste extra de pasar de Cold a Warm casi siempre se justifica con dos o tres incidentes evitados al año.

Sobre **backups**, presta atención asimétrica a Blob y Table. Cosmos y SQL te dan casi todo de fábrica; Blob y Table te dan poco. Y son los servicios con más uso casual ("guardo unas imágenes ahí", "una tabla de configuración rápida"). Activa soft delete y versioning en Blob desde el día uno de cada proyecto. En Table, define el export desde el inicio o asume que no tendrás recuperación.

Y sobre **compliance**, el cruce entre WORM y derecho al olvido es el que más sorpresas da. Si tu sistema procesa datos personales de europeos *y* documentos sujetos a SEC, sepáralos en containers distintos con políticas distintas desde el diseño. No "luego lo refactorizamos cuando llegue la auditoría". Refactorizar policies sobre containers que ya llevan años de datos es complicado.

---

## 14. Comprueba que lo has entendido

Sin mirar atrás. Si dudas, vuelve a la sección.

1. ¿Qué servicios de los siete del ejemplo **no** tienen backup automático? ¿Qué tienes que configurar tú en cada uno? *(sección 5)*
2. Tu sistema es "Importante" según el negocio. Te piden RPO ≤ 5 min y RTO ≤ 10 min. ¿Recomienda el planner una estrategia que cumpla? ¿Qué dirías al negocio? *(secciones 6, 8)*
3. Trabajas en una fintech europea con clientes que también caen bajo SEC. ¿Por qué no puedes meter todos los documentos en el mismo container de Blob? *(sección 7)*
4. ¿Cuál es la diferencia operativa entre RPO y RTO con un ejemplo concreto? *(sección 6)*
5. ¿Por qué `IDrPlanner` es interfaz inyectable y los advisors son clases estáticas? *(sección 8)*
6. Has activado Continuous Backup en Cosmos DB y soft delete en Blob hace 3 años. Hoy te avisan de un borrado por error de la semana pasada. ¿Cubierto? ¿Y si fue hace 35 días? *(sección 5)*
7. ¿Por qué el ejemplo no tiene capa de integración ni con `SkippableFact`? *(sección 10)*

<details>
<summary>Respuestas</summary>

1. **Blob, Table, Queue y App Service.** En Blob: soft delete + versioning + (si hay compliance) immutability policy. En Table: definir un proceso de export manual a otra cuenta o BD. En Queue: aceptar que los mensajes son efímeros por diseño; si necesitas auditar, persiste en otro sitio. En App Service: redeploy desde pipeline + IaC, o backup explícito si el tier lo permite.
2. **No** cumple. "Importante" recomienda Warm Standby (RTO 15-60 min), y el negocio pide RTO ≤ 10 min. El planner devuelve `CumpleObjetivos: false` con aviso de slide 22. La conversación correcta con el negocio: "tu RTO objetivo (10 min) exige Active-Active (RTO < 1 min), que cuesta ~2× la infraestructura base; ¿hay presupuesto, o relajamos el RTO objetivo a 60 min?". El plan no decide; pone los trade-offs sobre la mesa.
3. Porque los requisitos son **contradictorios**. SEC 17a-4 exige WORM (no modificable, no borrable) durante 6 años. RGPD obliga a poder borrar datos personales al solicitarlo (derecho al olvido). Un mismo container no puede ser WORM y borrable simultáneamente. La solución: containers separados con políticas distintas — los documentos SEC con immutability policy de 6 años; los datos personales con retención corta de backups (30-90 días) y proceso de re-anonimización.
4. **RPO** = cuántos minutos de datos pierdes; **RTO** = cuánto tiempo el servicio está caído. Ejemplo: a las 14:00 un incidente borra la base. La última copia válida es de las 13:50. RPO real = 10 minutos (los datos de las 13:50-14:00 se perdieron). El equipo restaura y el servicio vuelve a las 14:45. RTO real = 45 minutos (caída desde las 14:00). RPO mide pérdida de datos; RTO mide tiempo caído. Suelen ir juntos pero son dos números distintos.
5. Porque los advisors no tienen estado, no interactúan entre sí y no necesitan resolverse del contenedor — son funciones puras invocables directamente. `IDrPlanner` tampoco tiene estado, pero existe como **seam de DI**: el test de contenedor (CAPA 0) lo resuelve para verificar que el grafo es correcto y para cruzar la lógica del aviso de slide 22. Sin esa interfaz, la CAPA 0 no tendría nada que cruzar. Funcionalmente la interfaz no aporta abstracción (no hay otra implementación), pero aporta verificabilidad del grafo de DI.
6. **Hace una semana: cubierto** — Continuous Backup de Cosmos cubre 7 o 30 días según configuración; soft delete de Blob, los días que hayas configurado (30 d típico). **Hace 35 días**: Cosmos con 7 días → fuera; Cosmos con 30 días → fuera; Blob con 30 d de soft delete → fuera. Por eso la **retención** es una decisión deliberada al configurar, no un valor por defecto. Si compliance exige más, sube el periodo o añade Long-Term Retention (SQL).
7. Porque backup, PITR, replicación y failover **no son emulables** sin Azure real. No hay emulador local que firme tokens RBAC o ejecute un point-in-time restore. Una `SkippableFact` que siempre se saltase sería trampa: ocupa una línea de test que nunca cubre nada. Mejor reconocer que la capa **no existe** y desplazar la verificación real al walkthrough manual del `scripts/02-smoke-test.sh`. Es la misma decisión que en S5.4 (Managed Identity tampoco se emula). La regla: ¿se puede emular? Sí → SkippableFact; no → lógica pura + DI, y el manual cuenta por qué.

</details>

---

## 15. Hasta aquí

Vuelve a la imagen del seguro del coche de la sección 4. La pregunta que importa nunca ha sido *"¿tengo seguro?"*. Es *"¿qué cubre, en cuánto tiempo, cómo lo reclamo, y lo he leído antes del accidente?"*. Esa pregunta, repetida a escala de Azure y disciplinada con un game day trimestral, es la diferencia entre un incidente de una hora y un incidente de una semana.

Y con esto cerramos M05. Cinco submódulos para entender qué guardar fuera de la base de datos (S5.1), cuándo SQL es la respuesta (S5.2), cuándo Cosmos lo justifica (S5.3), cómo conectarte sin secretos a los tres (S5.4) y cómo prepararte para cuando algo se rompa (S5.5). Lo que viene a continuación son **dos prácticas** que integran lo aprendido: **S5.P** (Cosmos DB con Managed Identity — S5.3 + S5.4) y **S5.P2** (Table Storage CRUD, S5.1 aplicado a fondo). Lo verás todo junto y con tus propias decisiones.
