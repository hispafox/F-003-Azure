# Manual del alumno — S8.3 · Despliegue automatizado

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué estrategia de despliegue elegir según el tipo de app, qué hace exactamente un health check post-deploy con retry, cuándo el rollback más limpio es un swap inverso de 5 segundos y cuándo apagar un feature flag es aún mejor.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M08-S8.3](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.3-despliegue-automatizado-v3.md). Tres piezas de lógica pura (advisor de estrategia por tipo de app, evaluador de health check con retry y smoke test, planificador de rollback con alternativa feature flag).

*Creado: 2026-05-20 23:20 +0200*

---

## 1. La idea en una frase

Un despliegue serio no es "subir el zip y rezar". Es **elegir la estrategia correcta según el tipo de app** (slot swap para App Service con slots, AppInstaller para MSIX, what-if + approve para infraestructura), **validar la salud post-deploy con retry** (5 intentos x 10 segundos para absorber cold-starts), y tener **plan de rollback** antes de empezar (swap inverso en 5 segundos para App Service, build+1 para MSIX, feature flag apagado cuando el código se puede aislar).

El submódulo materializa las tres decisiones. Lo importante: tener el rollback diseñado **antes del deploy**, no improvisado en pleno incidente.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican el "no hay deploy sin plan de rollback":

**Caso 1 — el direct deploy de viernes por la tarde.** Un equipo con App Service sin slots desplegaba directamente a producción. Viernes a las 17:00 subieron una versión nueva. Tardó 4 minutos en estar arriba (durante esos 4 minutos: HTTP 503 a todos los usuarios). Pasaron a verificar; uno de los endpoints devolvía 500 con ciertos payloads concretos. **Rollback consistió en reconstruir el artifact de la versión anterior (2 min), redesplegar (4 min), 6 minutos más de 500s.** Total: 10 minutos de caída. Con slots habilitados el flujo habría sido: deploy a staging (sin afectar producción), smoke test, swap (5 segundos de switch), si algo cae → swap inverso (5 segundos). **De 10 minutos a 10 segundos de caída.**

**Caso 2 — el `what-if` que detectó el `Delete:`.** Un equipo de infra modificó un manifest Bicep para "limpiar configuración redundante". El cambio quitó accidentalmente una propiedad que estaba como `prevent: false` en una cuenta de Storage. Ejecutaron `az deployment group what-if` antes de aplicar y vieron: "**Delete: storageAccounts/dbbackups**". Treinta segundos para darse cuenta de que el cambio iba a borrar la cuenta de backups de la BD. **El what-if salvó el incidente antes de que existiera.** El submódulo trata `WhatIfApprove` como obligatorio para Infra.

**Caso 3 — la feature crítica con feature flag.** Otro equipo desplegó una nueva funcionalidad de procesamiento de pagos a producción **con el feature flag apagado**. El código estaba ahí, no se ejecutaba. Activaron el flag al día siguiente con el equipo de soporte preparado. En la primera hora descubrieron un edge case con tarjetas de un emisor concreto. **Apagaron el flag en 30 segundos**: el código sigue desplegado, el comportamiento vuelve al anterior, sin redeploy, sin swap. Investigaron, corrigieron, redesplegaron, reactivaron al día siguiente. **El feature flag es el rollback más rápido**: ~segundos, sin tocar la infra.

Los tres casos los modela el ejemplo: el advisor recomienda la estrategia correcta, el evaluador de health check valida con retry, el planificador de rollback ofrece tanto el método "tradicional" (swap/redeploy/build+1/redeploy) como la alternativa con feature flag.

---

## 3. Por qué esto importa en tu stack

Si tu sistema tiene **cualquier despliegue automatizado** —y los modernos lo tienen—, las tres preguntas:

- **¿Qué estrategia uso para esta app concreta?** App Service con slots → slot swap. App Service sin slots → habilita slots o asume downtime. Functions Premium → slot swap; Consumption → cold start aceptable. MSIX → AppInstaller (ya viste en S7.6). Infra → what-if obligatorio.
- **¿Cómo valido que el deploy fue bien?** Health check con retry de 5×10s para absorber cold-starts; smoke test funcional contra 3-5 endpoints clave; auto-rollback con `condition: failed()` si algo falla.
- **¿Cómo deshago si algo va mal?** Plan ANTES del deploy. App Service con slots: swap inverso (5s). MSIX: build+1 (1-24h). Feature flag: segundos. Direct deploy sin slots: 2-5 minutos de mala suerte.

Sin las respuestas, cada incidente te coge desprevenido. Con ellas, el incidente tiene un runbook.

---

## 4. La analogía vertebradora: el cambio de neumáticos

Imagina dos coches que tienen que cambiar de neumáticos. Uno es un coche normal en tu garaje. El otro es un coche de F1 en boxes.

**Coche normal (direct deploy)**:

- Lo subes al gato, sacas la rueda, pones la nueva. **El coche está fuera de servicio durante 15 minutos**.
- Si descubres que el neumático nuevo está pinchado al bajarlo, vuelves a subir, sacas, pones el viejo. **15 minutos más**.
- Total downtime ante incidente: 30 minutos.

**Coche de F1 (slot swap)**:

- Hay un coche **idéntico** en boxes con los neumáticos nuevos ya puestos (el slot staging). El equipo verifica que funciona en boxes.
- En 5 segundos: el coche principal entra a boxes, el de boxes sale a la pista. **No hay downtime para el conductor.**
- Si el coche nuevo tiene un problema en pista, en 5 segundos lo vuelven a meter y sacan el anterior. **5 segundos de downtime ante incidente.**

Y luego está la situación del **interruptor inteligente** (feature flag):

- El coche normal tiene un interruptor en el salpicadero: "Modo cambio de neumáticos". Activado, usa los nuevos; desactivado, usa los anteriores.
- Cambiar los neumáticos = ponerlos físicamente Y activar el interruptor.
- Si los nuevos fallan, **giras el interruptor** (segundos) y vuelves a los anteriores sin volver al garaje.
- Solo cuando estés seguro, vas al garaje a quitar los anteriores físicamente.

**Por debajo, todos hacen lo mismo**: cambiar neumáticos. La diferencia es el tiempo de downtime ante incidente. El coche normal asume 30 minutos. El coche F1 asume 10 segundos. El coche con interruptor asume 5 segundos sin tocar el motor.

Mantén la imagen mientras lees el código: tipo de app define qué "coche" eres, estrategia define cómo cambias, plan de rollback define cuánto sufres si algo va mal.

---

## 5. Recorrido por el código

### `DeployStrategyAdvisor.Recomendar` — la estrategia por tipo de app

La función central:

```csharp
public static RecomendacionEstrategia Recomendar(EscenarioDeploy e) => e.TipoApp switch
{
    TipoApp.AppService when e.TieneSlots => new(
        SlotSwap, "Sin downtime", "~5 segundos (swap inverso)", "Bajo",
        "App Service con slots → swap inverso para rollback."),

    TipoApp.AppService => new(
        DirectDeploy, "Sí (reinicio)", "2-5 minutos (redesplegar)", "Alto",
        "App Service sin slots → considera habilitar staging."),

    TipoApp.Functions when e.PlanPremium => new(
        SlotSwap, "Sin downtime", "~5 segundos", "Bajo",
        "Functions Premium soporta slots."),

    TipoApp.Functions => new(
        DirectDeploy, "Pequeño (cold start)", "2-5 minutos", "Medio",
        "Consumption plan no soporta slots."),

    TipoApp.Msix => new(
        AppInstaller, "Sin downtime", "1-24 h (auto-update)", "Bajo",
        "MSIX: publicar nueva versión + AppInstaller actualiza."),

    TipoApp.Infra => new(
        WhatIfApprove, "Depende del recurso", "Re-deploy del estado anterior", "Variable",
        "Bicep: what-if obligatorio antes de aprobar. Si ves 'Delete: ...' algo va mal."),
};
```

Seis combinaciones cubren los casos típicos:

1. **App Service con slots** → `SlotSwap`. **Sin downtime, rollback en 5s, riesgo bajo**. Es la opción ideal. **Habilita slots desde el día uno si tu plan lo permite** (Standard o superior).
2. **App Service sin slots** → `DirectDeploy`. Riesgo alto. La recomendación inmediata: cambia a un plan con slots.
3. **Functions Premium** → `SlotSwap`. Igual que App Service.
4. **Functions Consumption** → `DirectDeploy`. El cold start ya da algo de margen, pero no es ideal.
5. **MSIX** → `AppInstaller`. Lo viste en S7.6. El rollback es build+1.
6. **Infra (Bicep)** → `WhatIfApprove`. Ejecuta `az deployment group what-if` antes de aplicar. Si ves un `Delete:` inesperado, **paras y revisas antes de seguir**.

La advertencia sobre el `what-if`: es la diferencia entre el caso 2 de la sección 2 (donde el what-if salvó al equipo) y un incidente. **Siempre what-if antes de aplicar a producción**.

### `HealthCheckEvaluator.Evaluar` — el bucle de retry

Modela el patrón típico del pipeline:

```yaml
- script: |
    for i in 1..5; do
      if curl -fs https://.../health; then exit 0; fi
      sleep 10
    done
    exit 1
```

La función pura:

```csharp
public static ResultadoHealthCheck Evaluar(
    int statusEsperado, int maxIntentos,
    IReadOnlyList<HealthAttempt> intentos)
{
    var ordenados = intentos.OrderBy(x => x.Intento).Take(maxIntentos).ToList();
    for (int i = 0; i < ordenados.Count; i++)
        if (ordenados[i].StatusObservado == statusEsperado)
            return new ResultadoHealthCheck(true, i + 1,
                $"✓ Health OK en el intento {i + 1}.");

    int ultimo = ordenados.Count > 0 ? ordenados[^1].StatusObservado : 0;
    return new ResultadoHealthCheck(false, ordenados.Count,
        $"✗ Health check falló tras {ordenados.Count} intentos...");
}
```

Recibe los códigos HTTP que el `curl` observó en cada intento. Si alguno fue 200 (o el `statusEsperado`), pasa indicando en qué intento. Si todos los `maxIntentos` fallaron, devuelve fallo con el último código observado.

Casos típicos:

- **App Service nuevo**: primer intento es 503 (warming up), segundo intento 200. Pasa con `IntentosUsados = 2`.
- **Function Premium**: primer intento 200. Pasa con `IntentosUsados = 1`.
- **App rota**: cinco intentos seguidos de 500. Falla.
- **Endpoint mal configurado**: cinco intentos de 404. Falla. (El health endpoint no existe; arréglalo).

Y el smoke test funcional:

```csharp
public static ResultadoSmoke EvaluarSmoke(IReadOnlyList<SmokeRequest> requests)
{
    foreach (var r in requests)
    {
        if (r.StatusObservado is >= 200 and < 300) ok.Add(r.Endpoint);
        else ko.Add($"{r.Endpoint} (HTTP {r.StatusObservado})");
    }
    return new ResultadoSmoke(ko.Count == 0, ok, ko);
}
```

Llama a 3-5 endpoints clave (`/health`, `/api/version`, `/api/productos/p1`, `/api/perfil`...) y verifica que todos respondan 2xx. Si alguno no, el smoke test falla → `condition: failed()` → rollback automático.

### `RollbackPlanner.Planificar` — el runbook ANTES de cada deploy

La función central:

```csharp
public static PlanRollback Planificar(TipoApp tipo, bool tieneSlots, bool planPremium) =>
    tipo switch
    {
        AppService when tieneSlots => new("Swap inverso", "~5 segundos",
            ["Verificar que el slot 'staging' aún tiene la versión anterior",
             "Ejecutar Swap Slots con SourceSlot=staging",
             "Comprobar health en el slot de producción tras el swap"]),
        // ... resto de casos
    };
```

Cada tipo de app tiene su plan detallado. Lo importante: **este plan se ejecuta automáticamente desde el pipeline**, no manualmente.

Ejemplo del pipeline para App Service:

```yaml
- task: AzureWebApp@1
  displayName: 'Deploy a staging'
  ...

- script: # health check con retry
  ...

- task: AzureAppServiceManage@0
  displayName: 'Swap staging→production'
  inputs:
    action: 'Swap Slots'

- script: # smoke test post-swap
  ...

# El rollback automático
- task: AzureAppServiceManage@0
  displayName: 'Rollback: swap inverso'
  condition: failed()    # ← clave
  inputs:
    action: 'Swap Slots'
    sourceSlot: 'production'
    targetSlot: 'staging'   # invierte la dirección
```

El `condition: failed()` es lo que hace el rollback **automático**: si cualquier paso anterior falla, el swap inverso se ejecuta. Sin intervención humana, sin "voy a hacer el rollback ahora", sin esperar a que alguien decida.

### `RollbackPlanner.PlanFeatureFlag` — la alternativa que evita el rollback

```csharp
public static PlanRollback PlanFeatureFlag(string flagName) =>
    new("Desactivar feature flag",
        "~segundos",
        [
            $"App Settings → poner {flagName}=false",
            "Reiniciar Workers / esperar a la siguiente lectura (~30s)",
            "Sin redeploy ni swap; el código sigue desplegado",
        ]);
```

La pieza más útil cuando el cambio se puede aislar tras un flag (como viste en S4.4 y S6.4). **El código nuevo está desplegado pero apagado**. Si va bien tras activarlo, se queda. Si va mal, apagar el flag = rollback en segundos sin tocar deploy. Recomendable para:

- Features de negocio nuevas que cambian comportamiento visible.
- Cambios de algoritmo de cálculo.
- Integraciones nuevas con sistemas externos.
- Migraciones de modelo de datos.

No aplicable a:

- Cambios estructurales internos (refactoring puro).
- Updates de dependencias.
- Fixes de bugs (que no se "apagan").

### `DeploymentPlanner` — el plan + checklist

El servicio inyectable. Combina los anteriores: dado un escenario, recomienda estrategia, define el plan de rollback completo (con la alternativa feature flag), y emite checklist con los puntos críticos (sticky settings, warmup post-deploy, smoke test definido).

---

## 6. La lección operativa: deploy → health → swap → smoke → rollback

La secuencia completa del pipeline serio:

1. **Deploy a staging** (sin afectar a producción).
2. **Health check sobre staging** con retry (5×10s).
3. **Swap staging→production** (5 segundos).
4. **Smoke test sobre producción** (3-5 endpoints, todos 2xx).
5. **Si algo falla en 2 o 4**: rollback automático (swap inverso) con `condition: failed()`.

Cinco pasos. Cada uno con su responsabilidad clara. El **swap inverso automatizado** es lo que distingue un pipeline operacional de uno casero: el "rollback humano" requiere que alguien lo decida, ejecute y verifique. El swap automatizado lo hace en 5 segundos sin esperar a nadie.

Y por encima de esto, el **feature flag como red final**: aunque el deploy haya salido bien, mantén el flag apagado los primeros días. Activa cuando estés seguro. Si algo falla en cualquier momento posterior, apagas el flag, no haces rollback.

---

## 7. Sticky settings y warmup (slides 14, 15)

Dos detalles operativos que el checklist incluye:

**Sticky settings**: cuando configuras settings de connection strings o app settings en App Service, por defecto **viajan con el slot durante el swap**. Eso significa que el slot `staging` tendría la connection string de `production` después del swap. **Mal**. La solución: marcar los settings como "sticky" (slot setting). Sticky settings **se quedan en el slot que las define**, no se mueven durante el swap.

Regla práctica: si la setting es **específica del entorno** (connection string de Cosmos prod vs staging, secreto distinto, URL distinta), márcala como sticky. Si la setting es **igual en ambos** (versión de la app, configuración global), no es sticky.

**Warmup post-deploy**: cuando App Service arranca una instancia, los primeros requests son lentos (cold start, JIT compilation). Para evitar que el swap exponga instancias frías, App Service permite configurar un **warm-up rule** que dispara requests automáticos a `/health` (u otra URL) antes de marcar la instancia como "lista para swap". Resultado: el swap no expone usuarios a 503/lento.

Ambos están en el checklist del planner. Sin ellos, el "zero downtime" del slot swap se rompe en la primera carga real.

---

## 8. Cómo probarlo en local

```bash
dotnet run --project src/Deploy.Demo.Api
# http://localhost:5107
```

Endpoints:

```http
### Recomendar estrategia para App Service con slots
POST http://localhost:5107/deploy/estrategia
Content-Type: application/json

{ "tipoApp": "AppService", "tieneSlots": true, "critico": true }
# → SlotSwap, "Sin downtime", rollback en ~5s

### Evaluar health check con retry
POST http://localhost:5107/deploy/healthcheck
Content-Type: application/json

{
  "statusEsperado": 200,
  "maxIntentos": 5,
  "intentos": [
    { "intento": 1, "statusObservado": 503 },
    { "intento": 2, "statusObservado": 200 }
  ]
}
# → { pasa: true, intentosUsados: 2, razon: "✓ Health OK en el intento 2" }

### Smoke test
POST http://localhost:5107/deploy/smoke
Content-Type: application/json

[
  { "endpoint": "/health", "statusObservado": 200 },
  { "endpoint": "/api/version", "statusObservado": 200 },
  { "endpoint": "/api/productos/p1", "statusObservado": 500 }
]
# → { pasa: false, endpointsOk: [2], endpointsFallidos: ["/api/productos/p1 (HTTP 500)"] }

### Plan de rollback para App Service con slots
GET http://localhost:5107/deploy/rollback?tipo=AppService&tieneSlots=true
# → método: "Swap inverso", tiempo: "~5 segundos", pasos: [...]

### Alternativa con feature flag
GET http://localhost:5107/deploy/rollback/feature-flag?flag=FEATURE_NUEVO_PROCESAMIENTO
# → método: "Desactivar feature flag", tiempo: "~segundos"

### Plan completo
POST http://localhost:5107/deploy/plan
```

Los 29 tests cubren los seis casos del advisor, el procesamiento ordenado de intentos (aunque lleguen desordenados), el smoke con casos mixtos, los planes de rollback por tipo (incluyendo MSIX build+1 que viste en S7.6).

Para auditar tu App Service real:

```bash
./scripts/demo.sh
# 1) 01-inventory-deploy.sh → slots + health check configurado +
#    últimos 3 deploys + sticky settings
```

Solo lectura. Te muestra qué slots tienes, qué health check probe está configurado en App Service (no es el de tu app, es el del propio Azure), los últimos deploys, y qué settings están marcados como sticky.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 9. Los anti-patterns del slide 31 (cuelan en el checklist)

Cinco prácticas que evitar:

**Anti-pattern 1 — Deploy a producción directo sin staging**. Tres minutos de 503 mínimo, sin red de seguridad. Habilita slots desde el plan que los permita.

**Anti-pattern 2 — Sin smoke test post-deploy**. Si `dotnet test` pasa en CI y el deploy "termina sin error", asumes que todo está bien. Hasta que un cliente te dice que su endpoint da 500. El smoke test contra 3-5 endpoints clave cuesta 10 segundos en el pipeline y ahorra el incidente.

**Anti-pattern 3 — Sin `condition: failed()` para rollback**. Si todo el rollback es manual ("alguien hace el swap inverso si algo falla"), el incidente dura lo que tarda ese alguien en notarse. Con `condition: failed()`, el rollback es automático.

**Anti-pattern 4 — Sticky settings mal configurados**. Después del primer swap, el slot `staging` tiene la connection string de `production`. Cualquier deploy futuro a staging conecta accidentalmente a producción. Anti-pattern operacional clásico.

**Anti-pattern 5 — Sin plan de rollback documentado antes del deploy**. Improvisar el rollback en pleno incidente es la receta para empeorar las cosas. El plan se escribe antes y se aplica durante.

---

## 10. Glosario breve

- **Deployment slot**: instancia paralela de App Service o Functions Premium donde despliegas y validas antes de hacer swap. Solo en Standard o superior.
- **Slot swap**: operación atómica (~5 segundos) que intercambia el contenido de dos slots. Habitualmente staging→production.
- **Sticky setting** / slot setting: app setting que se queda en su slot durante el swap.
- **Warm-up rule**: configuración que hace que App Service envíe requests a una URL al arrancar una instancia, antes de marcarla "lista".
- **Smoke test**: serie de requests post-deploy a endpoints clave que verifican que el sistema responde correctamente.
- **Health check / health endpoint**: endpoint que devuelve 200 si la app está sana, 503 si no. Usado por warmup, por load balancers y por scripts de deploy.
- **`condition: failed()`**: condición de Azure Pipelines que hace que un step se ejecute solo si algún step anterior falló. Útil para rollbacks automáticos.
- **`az deployment group what-if`**: comando que muestra qué cambios Bicep aplicará sin ejecutarlos. Obligatorio en Infra.
- **AppInstaller**: mecanismo de actualización para MSIX (S7.6).
- **Direct deploy**: deployment "tonto" que reemplaza el contenido en sitio. Con downtime.
- **Blue/green**: dos entornos completos paralelos (no slots, sino infra completa) con switch DNS.
- **Canary**: deployment progresivo, % de tráfico que aumenta gradualmente.
- **Rolling update**: actualización pod por pod (típico de Kubernetes).

---

## 11. Cierre

S8.3 te da las tres decisiones de un deploy moderno: estrategia por tipo de app (slot swap es el rey para App Service y Functions Premium), validación post-deploy con retry y smoke, plan de rollback antes de ejecutar (con feature flag como alternativa sin redeploy). El pipeline serio implementa la secuencia deploy→health→swap→smoke→rollback con `condition: failed()` para automatizar el caso de error.

Lo siguiente es [`S8.4 — ADO vs GitHub Actions`](../S8.4-ado-vs-github-actions/MANUAL.md), el submódulo que zanja la pregunta clásica: ¿pipelines en Azure DevOps o en GitHub Actions? — con criterios objetivos para elegir y un mapeo YAML uno a uno entre los dos.
