# Manual del alumno — S2.P2 · Práctica: deploy básico a Azure App Service

Esto **no** es el [`README.md`](README.md). El README es el guion paso a paso por Portal y por scripts, con la lista exacta de App Settings, comandos `curl` y checklist. Este manual va antes: te cuenta por qué esta práctica es la versión "concentrada" del primer deploy y cómo verla con respecto a S1.P (que es similar) y a S2.P (que es la práctica seria con slots).

Tiempo de lectura: ~15 min. Práctica de referencia: [M02-S2.P2](../../../doc/M02-App-Services/v4-actual/M02-S2.P2-practica-deploy-basico-v1.md). Tier F1 (gratuito), coste real **cero euros**. El primer deploy end-to-end sin slots, sin pipelines, sin secretos — solo código local → URL pública.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Esta práctica entrena el flujo más simple posible: **código en tu portátil → publicación en Azure → URL pública respondiendo**. Sin slots (eso es S2.P), sin Application Insights (eso es S2.5), sin Key Vault (eso es S2.4). Tres endpoints, una App Setting que cambias en el portal y se refleja sin redesplegar, un health check, un cleanup ordenado.

¿Por qué existe esta práctica si M01 ya tiene S1.P haciendo prácticamente lo mismo? Por dos razones. La primera, porque a estas alturas del curso ya dominas los conceptos y conviene tener una versión **concentrada** del flujo, sin el pre-flight tan extenso de S1.P, para usar como referencia rápida. La segunda, porque sirve como **alternativa pedagógica** para alumnos que se incorporan tarde al curso y no han hecho M01 — pueden empezar aquí sin tener slots ni complejidad encima.

---

## 2. El problema real que hay detrás

Cuando se enseña Azure a un equipo que no lo ha tocado nunca, hay dos formas de hacerlo. La fácil es empezar por algo grande: arquitectura, microservicios, eventos, Functions. El resultado es un equipo abrumado que copia comandos sin entenderlos. La buena es **empezar por el deploy más simple**: una API minimal, un plan F1 gratis, un `dotnet run` que primero responde en `localhost:5000` y después en `https://app-curso-...azurewebsites.net/`. El momento "ah, esto era" sucede ahí, no en la arquitectura compleja del módulo 10.

S2.P2 es esa práctica buena del primer día. Y aunque la hagas en orden tras S2.5 (cuando ya conoces todo lo demás), su valor pedagógico sigue ahí: **es el flujo mínimo viable** que funciona en cualquier proyecto pequeño y que conviene tener como reflejo.

Lo que entrega:

| Paso | Lo que demuestras |
| --- | --- |
| **Crear RG + plan F1 + Web App** | Conoces la jerarquía Suscripción → RG → Plan → App |
| **Deploy desde VS Code (o `az webapp deploy`)** | Pasas de localhost a URL pública |
| **App Settings cambiados sin redesplegar** | Sabes el patrón que ahorra horas en producción |
| **Logs en streaming** | Puedes ver lo que pasa en directo |
| **Smoke tests automatizados** | Tienes una validación objetiva del deploy |
| **Cleanup completo** | No dejas recursos consumiendo cuota gratuita |

Sesenta minutos. Cero euros. Y se acaba con una API real funcionando en internet con tu nombre.

---

## 3. Qué cambia respecto a S1.P y por qué importa

[`S1.P`](../../M01-Intro-Azure/S1.P-practica-helloworld/MANUAL.md) y **S2.P2** hacen casi lo mismo. La diferencia está en el contexto pedagógico y en los detalles:

| Aspecto | S1.P | S2.P2 |
| --- | --- | --- |
| Cuándo se hace | Primera práctica del curso entero | Segunda práctica del módulo M02 |
| Tier | F1 (gratis) | F1 (gratis) |
| Tests | 10 (incluye echo, version, api-info) | 7 (más concentrados) |
| Retos | 4 (echo, version, api-info, App Insights) | 4 (POST /usuarios, error handling, health avanzado, `az webapp up`) |
| Continúa con... | M02-S2.P (reutilizando el RG) | El módulo M02 ya está cerrado; esto es referencia |
| Audiencia ideal | alumno que llega sin tocar Azure | alumno que ya ha hecho M02 y quiere el flujo "concentrado" |

La regla práctica: **si vas a hacer las dos, empieza por S1.P** (tiene más contexto pre-flight y la transición es más amable). **Si solo vas a hacer una porque ya conoces los fundamentos, S2.P2 es más rápida**.

---

## 4. El modelo mental: el ciclo más corto posible

```
1. Código en tu portátil
   dotnet run → http://localhost:5080

2. Crear infraestructura en Azure
   Suscripción ── RG ── Plan F1 ── Web App
                                       │
                                       └── https://<app>.azurewebsites.net (vacía)

3. Deploy
   VS Code → Deploy to Web App      o      bash 02-deploy.sh
   (publish + zip + zip deploy a través de Kudu)

4. Verificar
   curl https://<app>.azurewebsites.net/   →  JSON con entorno: "Production"

5. App Settings sin redesplegar
   Configuration → Application settings → Saludo__Base → Save
   curl /saludo/Pedro  →  refleja el cambio en ~30 segundos

6. Cleanup
   az group delete  o  Portal → RG → Delete
```

Las cinco acciones cubren el 80% del trabajo diario con App Service. Cuando dominas este ciclo, todo lo demás (slots, monitoring, secretos) son variaciones sobre el mismo patrón.

---

## 5. Lo que ya sabes y lo que la práctica te recuerda

Si vienes del módulo M02 entero, la mayoría de cosas son repaso. Las que conviene tener frescas:

- **La jerarquía Suscripción → RG → Plan → App** es invariante. Cambia el tier (F1 vs B1 vs S1), cambia el runtime (.NET vs Node vs Python), pero los cuatro niveles son siempre los mismos.
- **F1 tiene cuota CPU diaria** (60 minutos/día). Si bombardeas la app, a media tarde devuelve 403 hasta el reset UTC de medianoche. Es la limitación más visible del plan gratuito.
- **F1 no tiene Always On**. La app se duerme tras ~20 minutos sin tráfico. El primer `curl` después de eso puede tardar 5-30 s en responder (cold start). El segundo es rápido. Es **lo esperado** en F1.
- **El healthCheckPath se configura** aunque no haya health check del lado de la app — pero conviene tener `/health` implementado para que el path apunte a algo real. App Service lo consulta para decidir si la instancia está sana.
- **App Settings se separan por `__`** cuando llegan como variable de entorno. `Saludo__Base` en el portal se traduce a `Saludo:Base` en `IConfiguration`.

---

## 6. El truco que muchos saltan: `App Settings` sin redesplegar

El paso más subestimado del flujo: cambiar una App Setting en el portal y ver que `/saludo/Pedro` responde con el nuevo texto **sin haber redesplegado**. Treinta segundos de espera (App Service reinicia el proceso) y el cambio está aplicado.

¿Por qué importa? Porque es **el atajo más útil** de Azure App Service en producción. Cuando descubras que un valor de configuración es incorrecto en un sistema en marcha, no quieres redeplegar quince minutos: quieres cambiar una variable y reiniciar treinta segundos. Y App Service hace exactamente eso.

La regla mental: **lo que cambia con frecuencia y no depende del código va en App Settings, no en `appsettings.json`**. `appsettings.json` lo subes con el ZIP; cambiarlo es redeploy. App Settings lo cambias en el portal sin tocar nada más.

> 🧠 **Y el caso especial: `appsettings.json` siempre con valores razonables por defecto.** Si una App Setting falta en producción, la app debería arrancar con el valor del JSON. Si la App Setting está, lo sobrescribe. Esa precedencia (JSON < env vars < App Settings de Azure) es lo que permite que tu app corra igual en local con `dotnet run` y en Azure con la configuración real.

---

## 7. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `dotnet run --launch-profile http` en local | `curl http://localhost:5080/` → `entorno: "Development", servidor: "tu-portátil"` | El punto de partida: app sirviendo desde tu máquina. |
| 2 | `bash 01-provision.sh` (o pasos por Portal) | RG + plan F1 + Web App con `healthCheckPath=/health` | La infraestructura mínima creada. URL pública existe pero está vacía. |
| 3 | `bash 02-deploy.sh` (o VS Code Deploy to Web App) | `curl https://<app>.azurewebsites.net/` → `entorno: "Production", servidor: <hash>` | El momento "ah, esto era". Mismo JSON, otra máquina. |
| 4 | `curl /health` | `{ status: "healthy", timestamp: "..." }` | El endpoint que App Service consulta cada cierto tiempo. |
| 5 | `curl /saludo/Madrid` | `{ mensaje: "Hola, Madrid", hora: "..." }` (con la base por defecto) | Endpoint con parámetro y lógica simple. |
| 6 | `bash 03-app-settings.sh` (o configura `Saludo__Base` en el portal) | espera ~30 s | App Settings cambiados. |
| 7 | Repite `curl /saludo/Pedro` | `{ mensaje: "Hola desde Azure App Service, Pedro", ... }` | **El cambio se reflejó sin redesplegar.** Sección 6. |
| 8 | *Log stream* en el portal mientras haces `curl /saludo/test` | la línea `"Saludando a {Nombre}"` aparece en tiempo real con `Nombre=test` | Logging estructurado funcionando — el `{Nombre}` no es texto, es un placeholder que el logger rellena. |
| 9 | `bash 04-smoke-test.sh` | 4 checks verdes (raíz, health, saludo, latencia) | Validación automatizada del deploy. Útil para CI/CD en M08. |
| 10 | `bash 05-cleanup.sh` | RG borrado en ~5 minutos | No dejas cuota gratuita consumiéndose. |

Un experimento que aporta más que la teoría: tras el paso 5, **mete a propósito un nombre largo** como `/saludo/un_nombre_extremadamente_largo_que_no_cabe_en_el_maximo_configurado_porque_supera_los_80_caracteres`. La respuesta es **400 Bad Request** con un mensaje explicando que excede `Saludo:MaxLength`. Esa validación está en `Program.cs`; es la primera vez que ves que tu App Setting (`Saludo:MaxLength`) está cumpliendo una función real, no solo decorativa.

---

## 8. Los cuatro retos opcionales

### Reto 1 — `POST /usuarios` con validación

Ya implementado en `Program.cs` con sus tres tests. Pruébalo:

```bash
curl -X POST "$URL/usuarios" -H "Content-Type: application/json" \
  -d '{"nombre":"Pedro","email":"pedro@example.com"}'
# → 201 Created con id GUID

curl -X POST "$URL/usuarios" -H "Content-Type: application/json" \
  -d '{"nombre":"Pedro","email":"sin-arroba"}'
# → 400 Bad Request
```

Es la primera vez que tu API valida input antes de procesarlo. El patrón se generaliza a cualquier endpoint con cuerpo JSON.

### Reto 2 — Custom error handling

Por defecto, una excepción no controlada devuelve una página HTML de error. En APIs serias eso es feo: quieres JSON estructurado con un código de error que el cliente pueda parsear. `app.UseExceptionHandler(...)` o un middleware custom es la solución. **M08** lo cubre con patrones más completos; el reto aquí es plantar la semilla.

### Reto 3 — Health check más elaborado

Cambia `/health` para que verifique uptime, working set o tiempo desde el último request, y devuelva `status: degraded` si algo no es óptimo. El ejemplo de S2.5 (`/health/details`) tiene un response writer JSON detallado que puedes adaptar.

### Reto 4 — Deploy con `az webapp up` (Slide 21)

```bash
cd src/MiPrimeraWebApp
az webapp up --runtime "DOTNETCORE:10.0" --name <tu-app> --resource-group $RG
```

Compara con el flujo de zip deploy. `az webapp up` es más cómodo para iteración rápida (un comando hace publish + zip + upload + restart). Para producción, el zip explícito es más predecible (controlas cada paso).

### Reto avanzado — GitHub Actions

Configurar un workflow que despliegue al hacer push a `main`. **M08** lo cubre en profundidad; si quieres adelantarte, el slide 22 de S2.P tiene un esquema básico.

---

## 9. Tests del proyecto

Siete tests, `WebApplicationFactory<Program>`:

- **`RootEndpointTests`** (1): GET `/` devuelve los cuatro campos esperados (`aplicacion`, `version`, `entorno`, `hora_servidor`).
- **`HealthEndpointTests`** (1): GET `/health` → 200 con `status: healthy`.
- **`SaludoEndpointTests`** (2): respeta `Saludo:Base` desde configuración; 400 si el nombre supera `Saludo:MaxLength`.
- **`UsuariosEndpointTests`** (3): POST con email válido → 201 con `id`; `[Theory]` con dos emails inválidos → 400.

Los siete son rápidos, en memoria, sin Azure. Sirven para verificar que la app funciona localmente antes de desplegar — si los tests están rojos, no deplegues.

---

## 10. Puesta en marcha y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure CLI ≥ 2.65 | `az login` y scripts | Sí |
| Cuenta Azure activa | la práctica entera | Sí |
| VS Code con extensión **Azure App Service** | deploy por UI | Recomendado |

Coste real: **0 €**. F1 es gratis. Storage del Cloud Shell (si lo usas) son céntimos al mes.

### 10.2 Compilar, testear y arrancar en local

```bash
cd examples/M02-App-Services/S2.P2-practica-deploy-basico
dotnet build MiPrimeraWebApp.slnx    # 0 errores
dotnet test                            # 7 pass · 0 fail
dotnet run --project src/MiPrimeraWebApp --launch-profile http
# → http://localhost:5080
```

En local, prueba los cuatro endpoints (raíz, health, saludo, POST /usuarios) para confirmar que todo funciona antes de desplegar.

### 10.3 Práctica con scripts (recomendado, ~10 minutos)

```bash
cd scripts
cp .env.demo.example .env.demo
# edita SUBSCRIPTION_ID y APP único globalmente

bash 01-provision.sh         # RG + plan F1 + web app + healthCheckPath
bash 02-deploy.sh            # publish + zip + zip deploy
bash 03-app-settings.sh      # Saludo__Base + Saludo__MaxLength
bash 04-smoke-test.sh        # 4 checks
bash 05-cleanup.sh           # borra el RG entero
```

`bash demo.sh` para el menú interactivo (incluye opción de log stream).

### 10.4 Práctica paso a paso por Portal

El detalle (qué crear, dónde, con qué valores exactos) está en el [`README.md`](README.md). Resumen:

1. **RG** en West Europe.
2. **App Service Plan F1** Linux.
3. **Web App** runtime *.NET 10 (LTS)*, Linux, con healthCheckPath `/health`.
4. **Deploy** desde VS Code (Deploy to Web App).
5. **Verificar** con `curl /`, `curl /health`, `curl /saludo/Madrid`.
6. **App Settings**: añade `Saludo__Base` y `Saludo__MaxLength`. Espera ~30 s y comprueba que `/saludo/Pedro` refleja el cambio.
7. **Log stream**: deja la pestaña abierta y haz peticiones — verás cada request en directo.
8. **Smoke test** con `04-smoke-test.sh` apuntando a la URL pública.
9. **Cleanup**: borrar el RG.

### 10.5 Problemas frecuentes

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| 502 / 503 al primer request | cold start del F1 | espera 30 s y reintenta |
| 503 sostenido tras varios deploys | la app no arranca (excepción al inicio) | `az webapp log tail` para ver el error |
| Cambios de App Settings no se reflejan | el reinicio aún está en curso | `az webapp restart` y espera ~30 s |
| 503 sin razón aparente tras horas de uso | cuota CPU diaria del F1 agotada (60 min/día) | espera al reset UTC de medianoche o sube a B1 |
| Deploy "OK" pero código viejo sigue corriendo | caché de App Service | `az webapp restart` y vuelve a probar |
| `AuthorizationFailed` en `az` | suscripción incorrecta seleccionada | `az account list -o table` y `az account set --subscription <correcta>` |

**Cuando nada funciona** abre Kudu (consola web administrativa) en `https://<app>.scm.azurewebsites.net`. Permite explorar archivos desplegados, ver variables de entorno reales y hacer SSH a la instancia Linux.

### 10.6 Métricas básicas

`Web App → Monitoring → Metrics` y crea un gráfico con:

- `Http2xx`, `Http4xx`, `Http5xx` — éxito y errores.
- `AverageResponseTime` — latencia.
- `CpuTime` — importante en F1 (cuota 60 min/día).
- `MemoryWorkingSet` — RAM (hay 1 GB en F1).

Para esta práctica no configuramos alertas; eso lo cubre S2.5 con Application Insights.

---

## 11. Ideas para llevarte

Lo más útil que sale de esta práctica no es ningún concepto técnico nuevo — es **interiorizar el ciclo mínimo viable**: crear, desplegar, configurar sin redesplegar, ver logs, limpiar. Si lo tienes como reflejo, cualquier proyecto Azure nuevo que arranques empieza con esta misma plantilla. Lo demás (slots, secretos, monitoring) son capas que añades cuando importa.

Sobre **F1 como tier de aprendizaje**: aprovéchalo para experimentar sin coste. Cuando empieces algo serio, sube directamente a **B1** (~10 €/mes). La diferencia desbloquea Always On y elimina el cold start; merece la pena al primer cliente real.

Sobre **Kudu** (la consola SCM): la mayoría de developers en Azure App Service no la conocen y se pierden. Cuando algo no funciona, `https://<app>.scm.azurewebsites.net` te da acceso al filesystem real, a las variables de entorno efectivas y a un terminal SSH. Es la herramienta que más rápido resuelve "¿pero está realmente desplegado lo que yo creo que está desplegado?".

Y sobre **el patrón "App Settings sin redesplegar"**: aplícalo desde el primer proyecto. Cualquier valor que cambie con frecuencia (URL de una API externa, feature flag, umbral configurable) va en App Settings, no en `appsettings.json`. La frase que conviene memorizar: "lo que cambia con frecuencia y no depende del código va en App Settings".

---

## 12. Comprueba que lo has entendido

1. Tu app responde `entorno: "Development"` cuando estás en Azure. ¿Qué pasó y dónde se arregla? *(sección 7, paso 3; concepto general)*
2. Cambias `Saludo__Base` en el portal. Pasas un `curl /saludo/Pedro` inmediatamente y aún ves el valor anterior. ¿Por qué y cuánto tienes que esperar? *(sección 6)*
3. La primera petición tras 20 minutos sin tráfico tarda 25 segundos. ¿Es un bug? ¿Cómo se soluciona? *(sección 5, sección 10.5)*
4. Despliegas, todo dice OK, pero el navegador sigue mostrando la versión vieja. ¿Cuáles son las dos causas más probables y cómo las distingues? *(sección 10.5)*
5. Acabas la práctica y dejas el RG sin borrar. Una semana después aún no pagas nada. ¿Por qué? ¿Cuándo se rompe esa premisa? *(sección 5, primer punto)*
6. En el reto 4, `az webapp up` hace el deploy con un solo comando. ¿En qué se diferencia del flujo de `02-deploy.sh` (publish + zip + zip deploy) y cuándo usarías cada uno? *(sección 8 reto 4)*

<details>
<summary>Respuestas</summary>

1. Alguna App Setting está poniendo `ASPNETCORE_ENVIRONMENT=Development` o tu `appsettings.json` lo tiene hardcoded. En *Configuration → Application settings*, revisa que esa variable no exista (Azure setea `Production` por defecto si no la pones). Si está, bórrala y guarda — la app reinicia y vuelve a `Production`.
2. App Service reinicia el proceso después de un cambio de App Settings, lo que tarda **unos 30 segundos** típicamente (hasta 2 minutos si el plan está muy cargado). Durante ese reinicio, la app puede servir aún el valor viejo o devolver 503 brevemente. Tras el reinicio, los nuevos valores están aplicados. Si tienes prisa: `az webapp restart` para forzar y esperar ~30 s.
3. **No es un bug**, es **cold start del tier F1**. F1 no tiene Always On (que requiere B1 o superior), así que tras unos 20 minutos sin tráfico la app se "duerme" y la siguiente petición paga el coste de despertar el runtime: cargar el DLL, abrir el puerto, inicializar el proceso. La segunda petición es rápida porque ya está caliente. Soluciones: (a) subir a B1 con Always On activado (~10 €/mes), (b) ponerle un Application Insights Availability Test que la pinge cada 5 minutos (mantiene la app caliente "barato"), (c) aceptar el cold start si es una app de pruebas.
4. Dos causas más probables: **(a) caché del navegador** — el browser sirve la respuesta cacheada. Distingue con `curl` directo o `Ctrl+Shift+R` (hard reload). **(b) App Service no aplicó el deploy correctamente** — ZIP corrupto, cache de Kudu, o el ZIP no incluía lo que pensabas. Distingue con `az webapp restart` (si tras reiniciar sigue viejo, el problema es el deploy) o entrando a Kudu (`https://<app>.scm.azurewebsites.net → Debug Console → wwwroot`) para ver los archivos reales desplegados.
5. **F1 es gratis** — no se paga aunque dejes el RG existiendo. El plan F1 cobra 0 € y el Storage de Cloud Shell asociado son céntimos al mes. **La premisa se rompe si subes el plan a B1 o S1** (pasarías a pagar ~10 €/mes o ~70 €/mes prorrateado) o si añades recursos de pago (App Insights con mucho volumen de datos, Key Vault con muchas transacciones, etc.). La regla: F1 + Storage básico es gratuito de verdad; cualquier upgrade empieza a pagar desde el minuto en que está provisionado, no desde que lo uses.
6. **`az webapp up`** hace todo en un comando: detecta el proyecto, hace publish, crea el ZIP, lo sube por Kudu, reinicia. Útil para **iteración rápida** durante desarrollo. **`02-deploy.sh`** descompone cada paso (publish → zip → upload) en comandos explícitos. Útil para **producción** porque controlas cada paso, puedes auditar el ZIP antes de subirlo, integra mejor con CI/CD. Regla práctica: `az webapp up` cuando estás explorando o desarrollando localmente; zip deploy explícito cuando es código que va a producción.

</details>

---

## 13. Hasta aquí

Has hecho el deploy más simple posible a Azure App Service. Si has llegado de M01-S1.P, este es repaso concentrado. Si has empezado directamente aquí, es la base sobre la que descansan todos los demás ejemplos del módulo M02.

Con esto cierras el **módulo M02 entero**: aprendiste a crear y configurar una Web App (S2.1), a desplegar sin downtime con slots (S2.2), a escalar bajo demanda (S2.3), a guardar secretos en Key Vault (S2.4), a monitorizar con App Insights (S2.5), y a hacer el ciclo completo con slots y rollback (S2.P) o el ciclo simple (S2.P2).

Lo siguiente es **M03 — Azure Functions**, que cambia el paradigma a serverless. El código deja de ser "una app que recibe peticiones" y pasa a ser "funciones que se ejecutan ante eventos". Los principios de configuración, monitorización y despliegue que has aprendido aquí siguen valiendo igual; lo que cambia es **el modelo de ejecución**. Te lo encontrarás natural.
