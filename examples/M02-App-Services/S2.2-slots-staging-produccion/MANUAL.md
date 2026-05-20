# Manual del alumno — S2.2 · Slots de despliegue: staging y producción

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: pasos exactos por Portal, scripts `az` opcionales, mapeo a slides. Este manual va antes: te cuenta por qué los slots cambian la forma en que despliegas y cuál es la decisión silenciosa (sticky vs no sticky) que la mayor parte de los equipos descubre cuando ya es tarde.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M02-S2.2](../../../doc/M02-App-Services/v4-actual/M02-S2.2-slots-staging-produccion-v4.md). Reutiliza la API de S2.1 y le añade lo específico de slots: `/warmup`, `/version`, settings sticky, swap multi-fase, traffic routing.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Hasta ahora cada deploy era "destruir y reconstruir": el nuevo ZIP sustituye al viejo, App Service reinicia, los primeros segundos hay 503s mientras el runtime arranca. En aplicaciones reales eso no se acepta. Slots cambian la regla: tu app tiene una **copia paralela** en la que despliegas y pruebas, y cuando estás satisfecho **redirige el tráfico instantáneamente** desde producción a la nueva versión, sin downtime y con un botón para deshacerlo si algo va mal.

Suena a fontanería corporativa cara, pero está disponible desde el tier S1 (~70 €/mes). Y la mecánica es exactamente la misma que verás en cualquier sistema de blue/green deployment serio. Esta práctica te la enseña con dos slots: producción y staging, una versión 1.0.0 en uno y 1.1.0 en el otro, swap, rollback, swap multi-fase y canary deployment al final.

---

## 2. El problema real que hay detrás

Un equipo desplegó un viernes a las cinco de la tarde. La versión nueva traía una mejora menor; nadie esperaba problemas. Pero la nueva versión esperaba una App Setting que faltaba en producción (estaba en el `appsettings.json` pero no en el portal), y la app empezó a tirar 500s en el primer endpoint. Sin slots, la única opción era redeplegar la versión anterior — buscar el ZIP correcto, esperar el publish, esperar el zip deploy, esperar el restart. Dos horas en pánico. Resultado: usuarios afectados, soporte saturado, sábado entero rehaciendo el deploy.

Con slots la historia es otra. Despliegas a **staging**, miras `/version`, lo pruebas, todo bien. Haces *swap*. Ahora producción sirve la 1.1.0 y staging tiene la 1.0.0 vieja. Y si algo va mal después del swap — un error que no viste en staging porque depende de la configuración real de prod — vuelves al portal, otro swap, treinta segundos, la versión anterior está de vuelta. Cero downtime, cero ZIP perdido, cero pánico.

Eso es lo que entrena esta práctica:

| Decisión que aprende el alumno | Para qué | Dónde la verás |
| --- | --- | --- |
| Dos slots: `production` (principal) + `staging` | Despliegue paralelo, sin afectar al tráfico actual | Portal → *Deployment slots* |
| Settings que **viajan** con el código vs **sticky** (no viajan) | Mantener la URL de DB y monitoring atadas al slot tras un swap | [`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) + columna *Deployment slot setting* en el portal |
| Endpoint `/warmup` para calentar el slot antes del swap | App Service aborta el swap si `/warmup` no responde 200 | [`WarmupEndpoints.cs`](src/AppService.Demo.Api/Endpoints/WarmupEndpoints.cs) + `WEBSITE_SWAP_WARMUP_PING_PATH` |
| Endpoint `/version` para verificar el swap visualmente | El contraste en `/version` antes/después es lo que hace la demo entendible | [`VersionEndpoints.cs`](src/AppService.Demo.Api/Endpoints/VersionEndpoints.cs) |
| Swap multi-fase (preview / complete / cancel) | Aplicar la config de prod a staging sin redirigir tráfico todavía | Portal → *Deployment slots → Swap → Perform swap with preview* |
| Traffic routing (canary deployment) | Mandar el 10% del tráfico a staging para ver cómo se comporta antes de promocionar | Portal → *Deployment slots → Slots traffic* |

---

## 3. Por qué esto importa en tu stack

Si vienes de un mundo donde el deploy es "FTP el ZIP y rezar", los slots son el cambio mental más grande que te ofrece App Service. Y son baratos: el coste real es subir del plan B1 (~10 €) al S1 (~70 €). Esa diferencia compra slots, custom domain (TLS gratis de Azure), backups automáticos y un poco más de RAM/CPU. Para una app de un cliente, esos 60 € de más se justifican el primer incidente que evitas.

Y hay un patrón mental que conviene fijar: **un swap no copia código, redirige tráfico**. Los dos slots tienen su propio sistema de archivos, su propio runtime, sus propios procesos. El swap intercambia los **mapeos de DNS interno** y las **variables de entorno no sticky**. Es por eso que es instantáneo y reversible. Si pensaras en "copiar el código del slot A al slot B", el coste sería tan alto como un deploy normal.

El cambio respecto a S2.1: ahora el plan tiene que ser **S1, no B1**. B1 no tiene slots — es la limitación más visible del tier "Basic". Si vienes con la app de S2.1, conviene crear un RG nuevo (`rg-curso-m02-s22`) con un plan S1 para no mezclar facturación.

---

## 4. El modelo mental: el escenario y el ensayo general

Imagina una compañía teatral con un truco curioso: tiene dos escenarios idénticos montados sobre una plataforma giratoria. El escenario A está delante del público y representa la obra actual; el escenario B está detrás, oculto por las cortinas. Mientras los actores del escenario A actúan, los del B ensayan la próxima obra con la misma escenografía, los mismos focos, la misma duración. Cuando llega el momento del estreno de la nueva obra, las cortinas se cierran un segundo, la plataforma gira, las cortinas se abren — y el público ve ahora la obra nueva sin haber notado la transición. Si el estreno va mal, otro giro, vuelve la anterior.

Cada escenario es un **slot**. Las cortinas que se cierran un segundo es el **swap**. Los actores ensayando son tu **deploy a staging**. Y los focos que dejan de mover entre escenarios — el cartel del teatro, los datos del programa, la dirección del backstage — son las **slot settings**: cosas que se quedan ancladas al escenario donde están y no se intercambian aunque la obra cambie.

```
App Service Plan S1
   │
   ├── Web App "app-curso-m02-s22-pedro"  (el escenario delante del público)
   │      ├── URL pública: https://app-curso-m02-s22-pedro.azurewebsites.net
   │      ├── AppOptions:Version = "1.0.0"   ← viaja con el código
   │      ├── AppOptions:EnvironmentLabel = "production"  ← sticky
   │      └── AppOptions:DbConnectionLabel = "prod-db"    ← sticky
   │
   └── Slot "staging"                       (el escenario detrás de las cortinas)
          ├── URL: https://app-curso-m02-s22-pedro-staging.azurewebsites.net
          ├── AppOptions:Version = "1.1.0"  ← viaja con el código (la versión nueva)
          ├── AppOptions:EnvironmentLabel = "staging"     ← sticky
          └── AppOptions:DbConnectionLabel = "staging-db" ← sticky
```

Tras un swap:

```
Web App "app-curso-m02-s22-pedro"
   ├── AppOptions:Version = "1.1.0"   ← cambió (la versión nueva está sirviendo)
   ├── AppOptions:EnvironmentLabel = "production"  ← NO cambió (sticky)
   └── AppOptions:DbConnectionLabel = "prod-db"    ← NO cambió (sticky)

Slot "staging"
   ├── AppOptions:Version = "1.0.0"   ← cambió (versión vieja queda lista para rollback)
   ├── AppOptions:EnvironmentLabel = "staging"     ← NO cambió
   └── AppOptions:DbConnectionLabel = "staging-db" ← NO cambió
```

Tres frases para fijar el modelo:

- **El código viaja, la configuración del entorno se queda.** Ese contraste es la decisión más sutil de los slots, y la que más equipos descubren cuando ya es tarde.
- **El swap es instantáneo y reversible.** Si la versión nueva en producción tiene un problema, otro swap te devuelve a la anterior en segundos. Esto cambia la psicología del deploy: ya no es "no nos podemos equivocar", es "si nos equivocamos, lo deshacemos".
- **El `/warmup` es el guardia de la puerta.** Si el slot al que vas a promocionar no responde sano a `/warmup`, App Service aborta el swap. Es lo que evita promocionar una app que arranca con un error de configuración y empieza a servir 500s al público.

Vuelve a la imagen del escenario teatral cada vez que dudes por qué los slots existen. La metáfora aguanta.

---

## 5. Settings sticky: la decisión sutil

[`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) tiene seis propiedades, divididas en dos grupos:

```csharp
// VIAJAN con el código (no sticky en Azure)
public string Version          { get; init; }   // → /version
public string Greeting         { get; init; }
public string[] AllowedOrigins { get; init; }

// STICKY (configurar como "Deployment slot setting" en Azure)
public string EnvironmentLabel  { get; init; }  // production / staging
public string DbConnectionLabel { get; init; }  // prod-db / staging-db
public string AppInsightsLabel  { get; init; }  // prod-insights / staging-insights
```

La regla de oro es esta: **lo que es parte del código (versión, mensajes, lógica) viaja con el swap; lo que es propiedad del entorno (URL de DB, instrumentación, claves del entorno) se queda anclado a su slot.**

¿Por qué importa tanto? Porque la alternativa (que las URLs de DB también se intercambien) es un incidente garantizado:

- Versión 1.1.0 en staging conectada a `staging-db`. Pruebas: todo bien.
- Haces swap. **Si las settings fueran todas no-sticky**, la app que ahora sirve en producción seguiría apuntando a `staging-db`. Empezaría a escribir datos de producción en la base de staging. Datos perdidos, base de producción intacta sin las nuevas escrituras, semana entera reconciliando.

Con la sticky setting, el swap intercambia el código pero **`DbConnectionLabel` se queda en su slot**: la app que ahora sirve en producción sigue apuntando a `prod-db`, que es lo que toca. La staging recupera la versión vieja con su `staging-db`. Todo en su sitio.

> 🧠 **La regla práctica para decidir sticky o no.** Pregúntate: "¿esta setting depende del **código** o del **entorno**?". Si depende del código (qué versión sirvo, qué saludo doy, qué features tengo activas), no debería ser sticky — debería viajar con el deploy. Si depende del entorno (a qué DB me conecto, qué App Insights uso, qué Key Vault, qué clave del entorno), tiene que ser sticky.

En el portal, esa decisión se marca con una columna llamada **"Deployment slot setting"** en *Configuration → Application settings*. Una casilla; pero esa casilla salva muchos viernes a las cinco.

---

## 6. El `/warmup`: el guardia de la puerta

[`WarmupEndpoints.cs`](src/AppService.Demo.Api/Endpoints/WarmupEndpoints.cs) implementa un endpoint que App Service llama **antes** del swap si lo configuras así:

```
WEBSITE_SWAP_WARMUP_PING_PATH = /warmup        (sticky)
WEBSITE_SWAP_WARMUP_PING_STATUSES = 200        (sticky)
```

Cuando le das al botón *Swap*, App Service:

1. Aplica al slot de origen (staging) toda la configuración que va a heredar el destino (las no-sticky de producción).
2. Lo arranca y le pide `GET /warmup`.
3. Si `/warmup` devuelve **200**, sigue con el swap.
4. Si devuelve **503** o cualquier otra cosa, **aborta**. La promoción no ocurre.

El código del endpoint en el ejemplo es deliberadamente simple: usa `DependencyChecks` (un placeholder) que devuelve todo OK. En una app real, ese mismo endpoint hace cosas útiles:

```csharp
// Pseudocódigo de lo que va en /warmup en producción
- Pingar Cosmos / SQL / Redis (¿el slot puede llegar a sus dependencias?)
- Precargar caches críticas (el primer cliente no paga el cold start)
- Verificar que las settings obligatorias están bien (el bug del Greeting vacío)
- Resolver tokens iniciales de Managed Identity (que la primera petición sea rápida)
```

> 🧠 **Por qué este guardia merece el esfuerzo.** Sin `/warmup`, el swap es ciego: App Service no sabe si la versión nueva arranca bien hasta que recibe el primer tráfico real. Con `/warmup` bien escrito, la versión rota se queda en staging y no afecta a producción. Es la diferencia entre "siempre habrá un incidente" y "los incidentes se quedan donde no hacen daño".

---

## 7. Recorrido guiado: el swap del minuto cero

Lanza la API en local primero (sección 11) y prueba los nuevos endpoints. La parte interesante es en Azure, pero entender qué responden ayuda a leer las respuestas reales.

| # | Petición / acción | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `GET /version` | `{ version: "1.0.0", slotName: "local", environmentLabel: "production" }` | Los tres campos. El `slotName` solo es interesante en Azure (`production`, `staging`). |
| 2 | Local: `GET /info` | JSON con `slotName`, `travelsWithCode`, `stickyToSlot` separados | El contraste entre los dos grupos de settings, en el mismo endpoint. |
| 3 | Local: `GET /warmup` | `{ status: "warm", checks: [...] }` con 200 | Los `DependencyChecks` simulados devuelven todo OK. En producción, esto pingaría dependencias reales. |
| 4 | Azure: deploy 1.0.0 al slot **production** | OK | La versión "actual" sirviendo en producción. |
| 5 | Azure: deploy 1.1.0 al slot **staging** (con `AppOptions__Version=1.1.0` en sus settings) | OK | La versión "nueva" en staging, lista para promocionar. |
| 6 | `curl https://<app>.azurewebsites.net/version` | `{ version: "1.0.0", slotName: "Production" }` | Producción sirve 1.0.0. |
| 7 | `curl https://<app>-staging.azurewebsites.net/version` | `{ version: "1.1.0", slotName: "staging" }` | Staging sirve 1.1.0. |
| 8 | Portal: *Deployment slots → Swap* (source: staging, target: production) | esperar ~30s | App Service llama a `/warmup` de staging; si pasa, intercambia. |
| 9 | Repite el paso 6 | `{ version: "1.1.0", slotName: "Production", environmentLabel: "production" }` | **Sin downtime**: la 1.1.0 ya está sirviendo en producción. `environmentLabel` sigue siendo `production` (sticky, no viajó). |
| 10 | Repite el paso 7 | `{ version: "1.0.0", slotName: "staging" }` | La versión vieja queda en staging, lista para rollback. |
| 11 | **Si algo va mal en producción**: vuelve a Portal → *Swap* | la 1.0.0 vuelve a producción | Rollback en treinta segundos. Esto es lo que cambia la psicología del deploy. |

Un experimento que aporta más que cualquier teoría: pon `staging = 10%` en *Slots traffic* del portal antes del swap. Llama a `/version` veinte veces seguidas en producción. Mira las respuestas: la mayoría serán 1.0.0, dos o tres serán 1.1.0. **Canary deployment en directo.** Si las 1.1.0 son sanas, sigue subiendo el porcentaje; si fallan, vuelves a 0. Es exactamente lo que hace cualquier sistema de deployment progresivo serio, pero en App Service viene gratis con el slot.

---

## 8. El swap multi-fase, cuando quieres más control

El swap normal es atómico: pulsas el botón, App Service hace todo el proceso (aplicar config, warmup, swap) y termina. Si algo falla en medio, queda en un estado intermedio que normalmente se resuelve solo.

El **swap con preview** (multi-fase) separa el proceso en dos pasos manuales:

1. **Preview**: App Service aplica al slot staging la configuración no-sticky del destino (producción). **No redirige tráfico**. Ahora staging está corriendo con la configuración exacta que tendría en producción.
2. Tú tienes tiempo para probar staging con su nueva configuración. Llamas a `/warmup`, a `/health`, a los endpoints que sospechas que pueden romper.
3. **Complete**: cuando estás satisfecho, completa el swap. App Service ahora sí redirige el tráfico.
4. **Cancel**: si en el paso 2 ves algo raro, cancelas. Staging vuelve a su configuración original. Producción no se enteró.

Es lo que conviene hacer cuando **la configuración de producción es muy distinta de la de staging** y sospechas que algún bug solo aparece con la config real. Una app con secretos diferentes, URLs externas distintas, escalado distinto — todos son escenarios donde el preview merece los dos minutos que añade al proceso.

---

## 9. Tests y por qué hay un test del `/warmup`

Ocho tests en `tests/AppService.Demo.Api.Tests/`. Cuatro nuevos respecto a S2.1:

- **`WarmupEndpointTests`** — verifica que `/warmup` devuelve 200 con la lista de checks. Esto es importante porque si `/warmup` rompiera, App Service abortaría todos los swaps de tu app — y descubrirlo en la primera promoción real es un mal sitio donde descubrirlo. Tener un test garantiza que el contrato del endpoint sigue cumpliéndose.
- **`VersionEndpointTests`** — verifica que `/version` lee la `Version` de configuración y reporta el `slotName`. En local `slotName` es `"local"`; en Azure será `"Production"` o `"staging"`.
- **`InfoEndpointTests`** ampliado — ahora separa `travelsWithCode` de `stickyToSlot` en la respuesta, así el tour de un swap se entiende mejor.

Sigue siendo todo `WebApplicationFactory<Program>`, en memoria, sin Azure. La validación del comportamiento real del swap se hace a mano en el portal — no hay forma sencilla de testear "App Service promociona mi slot" en una pipeline de CI; es uno de esos comportamientos que se prueba con un game day y se documenta.

---

## 10. La psicología del deploy con slots

Antes de cerrar la parte técnica, una observación que vale más que cualquier comando:

**Sin slots**, el deploy es un evento de riesgo. Se planifica, se anuncia, se hace fuera de horas, se vigila. Un error es un incidente caro. La psicología del equipo es de aversión al riesgo: "no toquemos esto que va bien".

**Con slots**, el deploy es rutina. Se hace a cualquier hora. Se prueba en staging con la configuración real. Si algo va mal, se hace swap inverso. Un error es un blip de minutos, no un incidente. La psicología cambia: "vamos a probar esta mejora en staging y la promocionamos esta tarde si va bien".

Esa diferencia de actitud, multiplicada a lo largo del año, es lo que separa equipos que evolucionan rápido de equipos que se quedan parados por miedo al deploy. Los 60 € de plan S1 vs B1 son el peaje más barato para entrar en esa cultura.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| Suscripción Azure activa | desplegar y crear slots | Sí (si vas a desplegar) |
| Tier Standard S1 mínimo | los slots requieren S1+ | Sí (B1 no permite slots) |
| VS Code con extensión **Azure App Service** | despliegue por UI | Recomendado |
| `az` CLI (en `bash`) | para los scripts del repo | Solo si vas por scripts |

### 11.2 Compilar y arrancar en local

```bash
cd examples/M02-App-Services/S2.2-slots-staging-produccion
dotnet build AppService.Demo.Slots.slnx       # 0 errores
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

En local todo es `slotName: "local"`. La gracia de los slots se ve en Azure, no aquí.

### 11.3 Pasar los tests

```bash
dotnet test
```

Resultado: **8 pass · 0 fail**. Sin Azure, sin Docker.

### 11.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Los pasos esenciales:

1. **Plan S1 + Web App** (no B1: B1 no tiene slots).
2. **Crear slot `staging`** (en *Deployment slots → Add slot*, clonando settings del principal).
3. **Configurar settings con la columna "Deployment slot setting"** correcta. Las no sticky (Version, Greeting) cambian entre slots; las sticky (EnvironmentLabel, DbConnectionLabel) son distintas por slot y se quedan en su slot al swap. Mira la tabla del README para los valores exactos.
4. **Configurar `/warmup`**: añade `WEBSITE_SWAP_WARMUP_PING_PATH=/warmup` y `WEBSITE_SWAP_WARMUP_PING_STATUSES=200` como settings sticky.
5. **Deploy 1.0.0 a producción**, **1.1.0 a staging**.
6. **Verifica con `/version`** en ambas URLs.
7. **Swap** desde el portal.

### 11.5 Scripts `az` (opcional, para escenificar la demo)

```bash
cd scripts
cp .env.demo.example .env.demo
bash demo.sh                    # menú interactivo con los 8 pasos
```

Los scripts `01-provision.sh` a `08-cleanup.sh` cubren la misma secuencia que el portal, pensados para clase (se pueden repetir).

### 11.6 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| No aparece la opción "Deployment slots" en el portal | tier B1; B1 no soporta slots | sube el plan a S1 en *Scale up (App Service plan)* |
| El swap falla con "Warmup ping failed" | `/warmup` no responde 200 | revisa que el endpoint existe y que `DependencyChecks` pasa |
| Tras el swap, los datos van a la DB equivocada | la setting de DB no estaba marcada como sticky | re-marca en *Configuration* y vuelve a swap |
| `/version` no cambia después del swap | la versión es la misma en ambos slots | confirma `AppOptions__Version` distinto en cada slot |
| Canary devuelve siempre la versión nueva | el navegador respeta cookies de affinity | usa `curl` o un endpoint con `?x-ms-routing-name=` para forzar |
| El swap tarda más de un minuto | normal en S1; en planes mayores es más rápido | espera; en planes P1V3+ son ~10-15s |

### 11.7 Limpieza

`Portal → Resource groups → rg-curso-m02-s22 → Delete resource group`. Borra plan + web app + slots + settings.

---

## 12. Ideas para llevarte

Lo más útil que sale de esta práctica es **la disciplina del par "version + slot"**. Cada deploy a staging cambia la versión. Cuando promocionas, la versión nueva queda en producción y la vieja en staging. Una mirada al `/version` de cada slot te dice en qué estado está el deploy: si las dos versiones son la misma, no hay nada que promocionar; si son distintas, hay algo en cola.

Sobre **sticky vs no-sticky**: si te quedas con una sola regla, que sea esta — "código viaja, entorno se queda". Versión, mensajes, features: no sticky. URLs de DB, App Insights, Key Vault: sticky. Si dudas con una setting concreta, pregúntate "¿esto cambiaría si solo cambia el código?". Si la respuesta es no (cambia con el entorno), es sticky.

Y sobre el **`/warmup`**: aunque tu app sea pequeña hoy, mete el endpoint desde el día uno. Aunque devuelva siempre 200 ahora, ya está disponible cuando un día necesites que verifique algo crítico antes del swap. Es el patrón que más te va a ayudar en el primer incidente del año donde el bug solo aparece con la config real de producción.

---

## 13. Comprueba que lo has entendido

1. ¿Por qué `EnvironmentLabel = "production"` es sticky y `Version = "1.0.0"` no lo es? *(sección 5)*
2. Haces swap y la nueva versión en producción empieza a tirar 500s. ¿Cuál es la operación de rollback y cuánto tarda? *(sección 4)*
3. App Service va a hacer un swap. ¿Qué pasa si el slot de origen no responde 200 a `/warmup`? ¿Por qué eso es una protección útil? *(sección 6)*
4. ¿Para qué sirve el "swap with preview"? Describe un escenario donde lo usarías y otro donde el swap directo es suficiente. *(sección 8)*
5. Configuras `staging = 20%` en *Slots traffic*. Llamas a la URL de producción cinco veces. ¿Qué esperas ver y para qué sirve esa funcionalidad? *(sección 7)*
6. Tu plan es B1 y no encuentras la opción "Deployment slots" en el portal. ¿Qué pasa y qué haces? *(sección 11.6)*

<details>
<summary>Respuestas</summary>

1. `EnvironmentLabel` depende del **entorno** (este slot **es** producción, da igual qué código corra dentro). `Version` depende del **código** (la versión del binario que estoy sirviendo, que cambia con cada deploy). Por eso al swap: `Version` viaja con el código (la versión nueva sirve en producción); `EnvironmentLabel` se queda en su slot (producción sigue siendo producción, staging sigue siendo staging). La regla mental: "código viaja, entorno se queda".
2. *Portal → Deployment slots → Swap* otra vez. Los slots quedan así tras el swap: producción tiene 1.1.0, staging tiene 1.0.0. Un segundo swap intercambia: producción vuelve a 1.0.0, staging recibe la 1.1.0 problemática. Tarda alrededor de treinta segundos (con `/warmup` configurado), prácticamente instantáneo respecto a redeplegar. Esto es lo que cambia la psicología del deploy: el error tiene un coste de minutos, no de incidente.
3. App Service **aborta el swap**. La promoción no ocurre y los slots quedan como estaban: la versión problemática se queda en staging, sin afectar a producción. Es una protección útil porque captura los problemas de arranque que solo aparecen con la configuración real (settings que faltan, dependencias inalcanzables, secretos mal configurados). Sin `/warmup`, el primer aviso del problema sería el primer error 500 en producción tras el swap; con `/warmup`, el aviso es "swap abortado" y nadie llega a verlo en producción.
4. **Swap with preview** aplica al slot de origen la configuración no-sticky del destino, pero **no redirige tráfico**. Te da tiempo para probar el slot con la config exacta que tendrá en producción antes de promocionarlo. Lo usaría cuando la configuración entre staging y producción es **muy distinta** (secretos diferentes, URLs externas distintas, scaling) y sospecho que algún bug solo aparece con la config real. **Swap directo** es suficiente cuando staging es un clon razonable de producción y confío en que el `/warmup` cogerá los problemas más comunes.
5. De las cinco peticiones, **aproximadamente una** debería devolver `version: "1.1.0"` (la de staging) y las otras cuatro `version: "1.0.0"` (la de producción). Es **canary deployment**: mandas un porcentaje pequeño de tráfico al slot nuevo para ver su comportamiento con tráfico real antes de promocionarlo del todo. Si la versión nueva tiene errores, los ves en una fracción pequeña de usuarios y puedes volver a 0% sin haber expuesto a la mayoría. Si todo va bien, subes el porcentaje progresivamente hasta el swap final.
6. **B1 no soporta slots** — es una limitación deliberada del tier. La opción simplemente no aparece. Hay que subir el plan a Standard S1 mínimo en *Scale up (App Service plan)*. Cuesta unos 60 € más al mes que B1, pero desbloquea slots, custom domains con TLS gratis de Azure, backups automáticos y más RAM/CPU. Para una app de un cliente, esa diferencia se justifica el primer incidente que evitas con el swap inverso.

</details>

---

## 14. Hasta aquí

Vuelve al escenario teatral de la sección 4: dos escenarios idénticos, plataforma giratoria, el público sin saber que el truco existe. Esa imagen captura todo lo que tienes que recordar de los slots — el resto es saber qué settings dejas marcadas como "Deployment slot setting" y qué endpoint pones en `WEBSITE_SWAP_WARMUP_PING_PATH`. El concepto, una vez visto, no se olvida.

Lo siguiente es [`S2.3 — Escalado automático`](../S2.3-escalado-automatico-planes/MANUAL.md). Tu app ya tiene un plan razonable y una forma segura de desplegar. Lo siguiente es enseñar a App Service a **crecer y encogerse solo** según la carga, con reglas que tú defines. Es la última pieza del "App Service en serio" antes de pasar a configuración segura, monitoring y la práctica final.
