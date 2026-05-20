# Manual del alumno — S2.P · Práctica: deployment slots y swap

Esto **no** es el [`README.md`](README.md). El README es el guion paso a paso por Portal y por scripts, con la lista exacta de App Settings y los checks de smoke tests. Este manual va antes: te cuenta qué demuestras al terminar la práctica y por qué este es el flujo profesional de deploy a partir de aquí.

Tiempo de lectura: ~20 min. Práctica de referencia: [M02-S2.P](../../../doc/M02-App-Services/v4-actual/M02-S2.P-practica-slots-swap-v4.md). Es la **primera práctica integradora del módulo M02** — junta lo aprendido en S2.1 (configuración base), S2.2 (slots) y un toque de S2.5 (smoke tests automatizados) en un ciclo completo.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Esta práctica te lleva del "deploy es destruir y reconstruir" al "deploy es promocionar y, si algo va mal, deshacerlo". El ejercicio: provisionas una Web App en B1 con la versión 1, subes el plan a S1 para ganar slots, configuras el slot staging, despliegas la versión 2 ahí, pasas smoke tests, haces swap y (lo más importante) practicas el **rollback con swap inverso**. Sesenta minutos, coste real menos de un euro.

El truco didáctico: para que la práctica se centre en el flujo y no en mantener dos códigos distintos, las "versiones" se simulan con App Settings (`Practica:Version`, `Practica:Novedad`). En tu trabajo real cambiará el código entre slots; aquí el código es uno solo. La metáfora pedagógica se mantiene: lo que viaja con la "v" es App Setting normal; lo que es del entorno se queda como **slot setting** sticky.

---

## 2. El problema real que hay detrás

Un equipo planeaba un deploy del jueves a las 17:00. La política era "viernes no se sube"; el jueves se podía. La versión nueva traía mejoras menores. Sin slots, el procedimiento fue: ZIP, publish, esperar a que App Service reiniciara, primer test... 500. El cambio dependía de una App Setting que no estaba en producción y la app no arrancaba. Con la versión vieja **borrada** del slot, las opciones se reducían: o reconstruyes el ZIP de la anterior y rezas (cuarenta minutos), o restauras desde un backup (que no estaba configurado). Cuatro horas más tarde, jueves a medianoche, producción volvía a funcionar. El equipo no se fue a casa hasta el viernes.

Con el flujo de esta práctica, esa misma situación es otra historia. Despliegas la v2 al slot **staging**, no a producción. Smoke tests sobre staging fallan: vuelves a tocar config, redesplegar a staging, smoke otra vez. Cuando pasan, swap. Si producción al recibir tráfico real falla por algo que solo aparece con la config real, swap inverso — staging tiene la v1 viva, treinta segundos y producción vuelve. El equipo se va a casa a la hora.

La diferencia entre los dos finales no es técnica: es **el orden de los pasos** y un slot extra. Eso es lo que esta práctica te enseña a interiorizar.

---

## 3. Lo que entrega la práctica

Doce casillas en el checklist (sección 11). Las cuatro que demuestran que dominas el flujo:

| Casilla | Lo que demuestras |
| --- | --- |
| **Plan subido a S1, slot creado** | Conoces el tier mínimo para slots y la operación de upgrade sin downtime |
| **Sticky settings configurados** (NotaEntorno, ASPNETCORE_ENVIRONMENT) | Sabes qué viaja y qué se queda en su slot tras el swap |
| **Smoke tests sobre staging antes del swap** | No promocionas a ciegas — verificas que la versión nueva está sana antes de redirigir tráfico |
| **Rollback ejecutado y producción volvió a v1** | Tienes el reflejo del swap inverso ante un problema en producción |

Las ocho restantes son los pasos operativos: provisión, deploys correctos, verificación post-swap, limpieza ordenada. Y los tres retos opcionales (canary, swap con preview, tres slots) son lo que te lleva del "sé hacer slots" al "sé montar una estrategia de deploy seria".

---

## 4. El modelo mental: el ensayo general antes del estreno

La metáfora del [`MANUAL.md` de S2.2`](../S2.2-slots-staging-produccion/MANUAL.md) — el escenario y el ensayo general — aplica también aquí, pero con un detalle nuevo: **el ensayo no es de mentira**. Cuando pruebas la versión 2 en el slot `staging`, no estás probando en un entorno parecido a producción: estás probando **en una infraestructura idéntica a producción**, con las mismas dependencias, el mismo runtime, el mismo plan (después del upgrade a S1). La única diferencia son las settings sticky (`NotaEntorno`, `ASPNETCORE_ENVIRONMENT`), que se quedan en su slot para que cada uno sepa quién es.

```
Plan S1
   ├── Web App "tu-app"  (PRODUCCIÓN, el escenario delante del público)
   │      ├── Practica:Version = "1.0"        ← no sticky
   │      ├── Practica:NotaEntorno = "Entorno de producción"   ← sticky
   │      └── ASPNETCORE_ENVIRONMENT = "Production"             ← sticky
   │
   └── Slot "staging"     (el escenario detrás de las cortinas)
          ├── Practica:Version = "2.0"        ← no sticky (la versión nueva)
          ├── Practica:NotaEntorno = "Entorno de staging — solo QA"   ← sticky
          └── ASPNETCORE_ENVIRONMENT = "Staging"                       ← sticky
```

Cuando haces swap, **el código y la `Version` se intercambian**; **`NotaEntorno` y `ASPNETCORE_ENVIRONMENT` se quedan**. Después del swap:

- Producción: `Version = "2.0"`, `NotaEntorno = "Entorno de producción"`, `ASPNETCORE_ENVIRONMENT = "Production"`. La versión nueva, con la nota correcta de producción.
- Staging: `Version = "1.0"`, `NotaEntorno = "Entorno de staging — solo QA"`, `ASPNETCORE_ENVIRONMENT = "Staging"`. La versión vieja, lista para rollback, con su nota de staging.

La pregunta a aplicar mentalmente cada vez que decides si una setting es sticky: **¿esto define lo que es el slot, o define lo que es el código?**. Si lo del slot, sticky. Si lo del código, normal. La práctica te lo hace decidir con cinco settings concretas para que el patrón quede claro.

---

## 5. El warmup: el guardia de la puerta que evita promocionar una versión rota

[`Program.cs`](src/AppService.Practica.Api/Program.cs) implementa `/warmup` y la configuración (en el script `03-upgrade-plan-and-create-slot.sh`) registra `WEBSITE_SWAP_WARMUP_PING_PATH=/warmup` y `WEBSITE_SWAP_WARMUP_PING_STATUSES=200` como settings sticky.

¿Qué hace eso? Antes de promocionar el slot staging a producción, App Service:

1. Aplica al slot staging la configuración no sticky de producción (cosa importante: la app de staging arranca con la configuración exacta que va a tener en producción).
2. Espera a que `/warmup` responda 200.
3. Si responde 200, hace el swap.
4. Si **no responde 200 (timeout, 503, error)**, **aborta el swap**. Staging se queda como estaba. Producción no se entera.

Esa protección es lo que evita promocionar una versión que arranca con un error de configuración silencioso. Sin `/warmup`, App Service haría el swap y descubrirías el problema con el primer cliente. Con `/warmup`, el problema se queda en staging — donde no le hace daño a nadie — y tienes tiempo para diagnosticar.

> 🧠 **El `/warmup` del ejemplo es trivial — devuelve siempre 200.** En tu trabajo real, ese endpoint hace cosas útiles: pinga la base de datos, verifica que llega a sus dependencias, precarga cachés. La idea es que **si esa app no puede servir tráfico real, el `/warmup` falla**. Configurarlo desde el primer proyecto te ahorra el día que un secreto rotado en Key Vault deja a tu app sin acceso a SQL — el `/warmup` lo caza antes de la promoción.

---

## 6. Smoke tests antes del swap

`scripts/05-smoke-test.sh production|staging [version]` ejecuta cuatro checks sobre uno de los dos slots: que `/health` responda 200, que `/` responda 200, que la `version` reportada sea la esperada, que la latencia esté en rango razonable.

La disciplina que entrena: **ejecutas el smoke sobre staging antes del swap, no sobre producción después**. Si el smoke en staging falla, no haces swap. Si pasa, haces swap y, después, ejecutas el smoke sobre producción. Si producción falla, swap inverso.

Esa secuencia (smoke staging → swap → smoke producción → rollback si falla) es el patrón de promoción profesional. La diferencia con "ZIP a producción y rezar" es que cada paso tiene un punto de control y un mecanismo de reversión inmediato. Tu equipo se va a casa cuando dice. El cliente no se entera de versiones rotas porque nunca llegaron a estar delante de él.

---

## 7. Recorrido guiado: el ciclo completo

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `bash 01-provision.sh` | RG + plan B1 + Web App creada, `Always On` y `HTTPS Only` activados, healthCheckPath `/health` | El punto de partida: app en B1, sin slots aún. |
| 2 | `bash 02-deploy-as-v1.sh` | `curl https://<app>.azurewebsites.net/` → `version: "1.0"` | La versión inicial sirviendo en producción. |
| 3 | `bash 03-upgrade-plan-and-create-slot.sh` | plan ahora es S1, slot `staging` existe, sticky settings configurados, warmup configurado | El paso clave: B1 → S1 desbloquea slots; las settings sticky decididas se aplican a los dos slots. |
| 4 | `bash 04-deploy-v2-to-staging.sh` | `curl https://<app>-staging.azurewebsites.net/` → `version: "2.0", novedad: "Slots...", nota_entorno: "...staging..."` | La versión nueva en staging, lista para promocionar. |
| 5 | `curl https://<app>.azurewebsites.net/` | sigue `version: "1.0"` | Producción intacta. Staging es un sitio aparte. |
| 6 | `bash 05-smoke-test.sh staging 2.0` | 4 checks verdes | Validación pre-swap. Si esto falla, **no hagas swap**. |
| 7 | `bash 06-swap.sh` | tarda ~30 s (warmup + swap) | App Service llama a `/warmup` de staging; si 200, intercambia. |
| 8 | `curl https://<app>.azurewebsites.net/` | `version: "2.0", novedad: "Slots...", nota_entorno: "Entorno de producción", entorno: "Production"` | Producción ahora sirve la v2. La `nota_entorno` y `entorno` **no cambiaron** (sticky). |
| 9 | `curl https://<app>-staging.azurewebsites.net/` | `version: "1.0", nota_entorno: "...staging..."` | La versión vieja queda en staging — lista para rollback. |
| 10 | `bash 05-smoke-test.sh production 2.0` | 4 checks verdes | Validación post-swap sobre la URL de producción. |
| 11 | `bash 07-rollback.sh` | otro swap, producción vuelve a `version: "1.0"` | El reflejo crucial: si algo va mal, el rollback es treinta segundos. |
| 12 | `bash 08-slot-diff.sh` | diff de settings entre los dos slots | Útil cuando llevas tiempo y dudas si las settings drift-eron. |
| 13 | `bash 09-cleanup.sh` | slot borrado, plan vuelto a B1 (o RG borrado entero) | Limpieza ordenada. |

Un experimento que aporta más que la teoría: tras el paso 8, dispara `for i in $(seq 1 20); do curl -s https://<app>.azurewebsites.net/ \| jq .version; done`. Ves veinte respuestas, **todas son `"2.0"`**. Sin downtime visible. Ese silencio es lo que te ahorras pagar en incidentes el día que rompas algo en producción y necesites volver atrás.

---

## 8. Los tres retos opcionales (y por qué merecen los minutos)

### Reto 1 — Canary deployment (slides 19, 21)

```bash
az webapp traffic-routing set --name $APP -g $RG --distribution staging=10
```

Manda el 10% del tráfico de producción a staging antes del swap completo. Si abres `/` veinte veces en modo incógnito (sin cookies de affinity), aproximadamente dos respuestas serán de la v2. Las otras dieciocho, de la v1.

Para qué sirve: **probar la versión nueva con tráfico real** sin comprometerte. Si los 10% fallan más que los 90%, está claro que algo va mal y no haces swap. Si los 10% se comportan bien, subes a 50%, después al 100% con swap. Es el patrón estándar de "rollout progresivo" — el mismo que usan Netflix, Google y demás cuando lanzan funcionalidades nuevas.

### Reto 2 — Swap con preview (multi-fase)

```bash
az webapp deployment slot swap --action preview ...    # fase 1: aplica config de prod a staging
# ... pruebas manualmente ...
az webapp deployment slot swap --action swap ...       # fase 2: completa el swap
# o
az webapp deployment slot swap --action reset ...      # cancela
```

Te da control manual entre las dos fases. Aplica al slot staging la **configuración no sticky de producción** sin redirigir tráfico, te deja probar staging con esa config exacta, y solo entonces completas (o cancelas).

Cuándo merece: cuando staging y producción tienen configuraciones muy distintas y sospechas que un bug puede aparecer solo con la config real. Una app con secretos diferentes, URLs externas distintas, escalado distinto. El preview te da una "última oportunidad" antes de comprometer.

### Reto 3 — Tres slots (dev → staging → producción)

Añadir un tercer slot `dev` cambia el flujo:

```
dev (devs trabajan aquí)
  ↓ swap
staging (QA aprueba aquí)
  ↓ swap
production
```

Cada slot tiene sus settings sticky propias. Los smoke tests se ejecutan en `dev` antes del swap a `staging`, y en `staging` antes del swap a producción. Es overkill para una app pequeña, pero útil para sistemas serios donde QA es un paso formal antes de producción.

---

## 9. Tests del proyecto

Cuatro tests, `WebApplicationFactory<Program>`:

- **`HomeEndpointTests` (2)** — verifica que `/` lee `Practica:Version` y `Practica:Novedad` desde configuración, con defaults razonables si no están. Sirve para confirmar el truco didáctico: las "versiones" son App Settings, no código.
- **`HealthEndpointTests` (1)** — `/health` responde 200 con `status: healthy`. Es el que App Service consulta cuando configuras Health check path.
- **`WarmupEndpointTests` (1)** — `/warmup` responde 200 con `status: warm`. Necesario porque si este endpoint se cae, **todos tus swaps se abortan**. Tenerlo testeado garantiza que nadie lo rompe sin querer.

Sin Azure, sin Docker. Lo demás se valida con `05-smoke-test.sh` contra una URL real, como parte del flujo.

---

## 10. Puesta en marcha y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear en local | Sí |
| Azure CLI ≥ 2.65.0 | `az login` y todos los scripts | Sí |
| Login activo (`az account show`) | crear recursos | Sí |
| Rol Contributor sobre la suscripción | crear RG, plan, web app, slots | Sí |

Coste: B1 (~13 €/mes prorrateado) durante todo el flujo + S1 (~70 €/mes prorrateado) durante la parte de slots. **Si terminas en menos de un día, el coste real es menos de 1 €**. Limpia con `09-cleanup.sh` al acabar.

### 10.2 Compilar y testear en local

```bash
cd examples/M02-App-Services/S2.P-practica-slots-swap
dotnet build AppService.Practica.Slots.slnx     # 0 errores
dotnet test                                       # 4 pass · 0 fail
dotnet run --project src/AppService.Practica.Api --launch-profile http
# → http://localhost:5080
```

En local `slot=local`. Los slots se ven cuando despliegas a Azure (sección 7).

### 10.3 Práctica con scripts (recomendado)

```bash
cd scripts
cp .env.demo.example .env.demo
# edita SUBSCRIPTION_ID y APP único globalmente

bash demo.sh                # menú interactivo con todos los pasos numerados
```

El menú lleva los pasos en orden y permite repetir cualquiera. Útil para escenificar la práctica en clase.

### 10.4 Práctica paso a paso por Portal

El detalle exacto (lista de App Settings, qué marcar como Slot setting, qué deploy va a qué slot, etc.) está en el [`README.md`](README.md). Los pasos canónicos son:

1. RG + plan **B1** + Web App con `/health` configurado.
2. Deploy versión 1 a producción (con `Practica__Version=1.0`).
3. Plan **B1 → S1** (Scale up, sin downtime).
4. Crear slot `staging`, clonando settings del principal.
5. Configurar **sticky settings** (`NotaEntorno`, `ASPNETCORE_ENVIRONMENT`, `WEBSITE_SWAP_WARMUP_PING_PATH`).
6. Deploy versión 2 al slot staging (con `Practica__Version=2.0`).
7. Smoke tests sobre staging.
8. Swap staging → production desde *Deployment slots → Swap*.
9. Smoke tests sobre producción.
10. **Rollback** ejecutando otro swap (esta es la práctica que más enseña).
11. Cleanup: borrar slot y bajar plan a B1.

### 10.5 Problemas frecuentes

| Síntoma | Causa típica | Solución |
| --- | --- | --- |
| `Operation 'Slot' is not supported` | el plan es F1/D1/B1, no admite slots | sube a S1 (paso 3) |
| Swap colgado > 2 min | el `/warmup` no responde 200 | `curl https://<app>-staging.azurewebsites.net/warmup` para diagnosticar |
| Sticky setting "desaparece" tras swap | la setting estaba marcada como Application setting, no como Slot setting | re-márcala como Slot setting en el portal o con `--slot-settings` |
| Después del swap, `version` sigue siendo 1.0 | el warmup falló y el swap se abortó | revisa Activity Log del recurso para ver el motivo |
| `nota_entorno` cambió tras el swap | NotaEntorno no estaba marcada como sticky | revisa con `08-slot-diff.sh` y re-marca |

### 10.6 Cleanup

`bash 09-cleanup.sh` te ofrece dos opciones: (a) borrar solo el slot y bajar el plan a B1 (útil si vas a continuar con otras prácticas que reutilizan B1), (b) borrar el RG entero. Para terminar limpio del todo, opción (b).

---

## 11. Checklist del entregable

| # | Paso | Verifica con |
| --- | --- | --- |
| 1 | Plan subido a S1 | `Scale up` muestra S1 activo |
| 2 | Slot staging creado | aparece en *Deployment slots* |
| 3 | Sticky settings configurados | columna *Deployment slot setting* marcada en *Configuration* |
| 4 | Warmup configurado | `WEBSITE_SWAP_WARMUP_PING_PATH=/warmup` en App Settings sticky |
| 5 | v1 desplegada y verificada en producción | `curl /` devuelve `version: "1.0"` |
| 6 | v2 desplegada y verificada en staging | `curl /` sobre `-staging` devuelve `version: "2.0"` |
| 7 | Smoke tests pasados sobre staging antes del swap | `05-smoke-test.sh staging 2.0` en verde |
| 8 | Swap ejecutado sin downtime aparente | varios `curl /` durante el swap siguen respondiendo |
| 9 | Post-swap: `nota_entorno` y `ASPNETCORE_ENVIRONMENT` **no** viajaron | producción dice `"Entorno de producción"` aunque la version sea 2.0 |
| 10 | Post-swap: `Version` y `Novedad` **sí** viajaron | producción dice `version: "2.0"`, staging dice `version: "1.0"` |
| 11 | Rollback ejecutado y producción volvió a v1 | otro swap, `curl /` devuelve `version: "1.0"` otra vez |
| 12 | Slot eliminado y plan bajado a B1 | `09-cleanup.sh` o portal — el RG queda limpio |

---

## 12. Ideas para llevarte

Lo más útil de esta práctica no es ningún paso técnico — es **el reflejo del rollback**. Cuando algo va mal en producción tras un deploy, la pregunta correcta no es "¿cómo arreglo esto?" sino "¿cuánto tardo en volver a la versión que funcionaba?". Con slots, la respuesta es treinta segundos. Sin slots, son horas. La diferencia compra muchas horas de sueño a lo largo del año.

Sobre **smoke tests**: aunque tu equipo no tenga pipeline formal aún, ese script `05-smoke-test.sh` —cuatro `curl` con `set -e` y umbrales razonables— es lo mínimo razonable. Ejecutarlo sobre staging antes de cada swap te ahorra el 80% de los rollbacks porque cazas los problemas obvios antes de tocar producción. Y ejecutarlo sobre producción después del swap es la confirmación objetiva de "se promocionó bien".

Sobre **canary** (reto 1): si tu negocio puede permitirse el patrón "manda 10% a la versión nueva, mide, sube si todo bien", es la forma más segura de desplegar funcionalidades grandes. Tres minutos en *Slots traffic* y ya tienes deployment progresivo. La mayoría de equipos no lo usan porque "es complicado"; en realidad son tres clics.

Y un último consejo, pragmático: **practica el rollback antes de necesitarlo**. La primera vez que ejecutes un swap inverso bajo presión en producción **no quieres que sea la primera vez que lo haces**. Repite el ejercicio de esta práctica algunas veces. El paso 11 — el rollback con `07-rollback.sh` — es el que más vale, porque genera el reflejo.

---

## 13. Comprueba que lo has entendido

1. ¿Por qué tienes que subir el plan de B1 a S1 antes de crear el slot? ¿Qué ocurriría si intentases crear el slot con B1 activo? *(secciones 10.5, slide 4)*
2. Configuras `Practica__NotaEntorno` como App Setting normal (no sticky) en los dos slots. Haces swap. ¿Qué ves en producción y por qué es problemático? *(sección 4)*
3. El swap se queda en "Pending" durante 5 minutos y luego se aborta. ¿Cuál es la causa más probable y dónde lo diagnosticas? *(sección 5, sección 10.5)*
4. Acabas de hacer swap. Producción sirve la v2. El cliente reporta que en una pantalla concreta hay un bug. ¿Cuál es la operación de rollback y cuánto tarda? *(secciones 2, 7 paso 11)*
5. ¿Qué diferencia hay entre el smoke test sobre `staging` antes del swap y el smoke test sobre `production` después del swap? ¿Hace falta hacer los dos? *(sección 6)*
6. En el canary (reto 1), pones `staging=10%` y abres `/` veinte veces. Aproximadamente dos respuestas son v2. ¿Para qué sirve ese setup y qué te dice si las dos respuestas v2 fallan pero las dieciocho v1 son sanas? *(sección 8 reto 1)*

<details>
<summary>Respuestas</summary>

1. Porque **el tier B1 no soporta deployment slots** — es una limitación deliberada del tier "Basic" de App Service. Si intentas crear el slot con B1, el portal devuelve `Operation 'Slot' is not supported on the current SKU`. Hay que subir a S1 (Standard) que es el mínimo con slots. El upgrade B1 → S1 es **instantáneo y sin downtime** (los recursos se reasignan sin parar la app), así que es seguro hacerlo a mitad de la práctica.
2. En producción verás `nota_entorno: "Entorno de staging — solo QA"` porque la setting **no sticky viajó con el código** al hacer swap. La nota de staging acabó en producción. Es problemático porque revela un patrón más grave: si una connection string a una DB de staging no estuviera marcada como sticky, después del swap **tu app en producción estaría escribiendo en la DB de staging**. Datos perdidos, base de producción intacta sin las nuevas escrituras. La sticky setting no es decoración, es la separación de planos.
3. El `/warmup` no está respondiendo 200. Posibles causas: (a) el endpoint no existe en la app desplegada (¿la versión 2 lo implementa?), (b) la app no arranca porque falta una App Setting obligatoria, (c) la app tarda más del timeout en estar lista. Lo diagnosticas con `curl https://<app>-staging.azurewebsites.net/warmup` para ver qué responde, y `az webapp log tail` para ver si la app tiró excepciones. El Activity Log del recurso muestra el motivo concreto del abort del swap.
4. **Otro swap en sentido inverso**: *Deployment slots → Swap → Source: staging, Target: production*. La v1 que se quedó en staging vuelve a producción. **Tarda 30 segundos**. Mientras tanto, producción sigue sirviendo la v2 hasta el momento del redirect. Por eso esta práctica te entrena el reflejo: la primera vez que tienes que hacerlo bajo presión en producción **no quieres que sea la primera vez que lo haces**.
5. El smoke pre-swap sobre **staging** verifica que la versión nueva funciona en su slot — antes de redirigir tráfico de producción. El smoke post-swap sobre **producción** verifica que el swap funcionó correctamente y que la versión nueva sirve bien con la configuración real de producción. Sí, hacen falta los dos: el primero te ahorra promocionar una versión rota; el segundo confirma que la promoción funcionó y la activa para alertar si algo va mal (por ejemplo, si tienes monitorización configurada con el smoke como base).
6. **Canary deployment**: probar la versión nueva con un porcentaje pequeño de tráfico real, antes de comprometerte al 100%. Si las dos respuestas v2 fallan pero las dieciocho v1 son sanas, **está claro que la v2 tiene un problema solo visible con tráfico real** (config de producción, dependencias externas, datos reales). No haces swap; vuelves a `staging=0` y diagnosticas con la v2 aislada en staging. Es la red de seguridad final antes del swap completo y la pieza que más diferencia un deploy "valiente" de un deploy "profesional".

</details>

---

## 14. Hasta aquí

Has cerrado la práctica integradora del módulo M02. Cuando lo hayas hecho una vez con calma y otra vez "como si fuera viernes a las cinco", el reflejo del rollback se queda. El próximo deploy real al que asistas en tu trabajo lo vas a vivir distinto: no como un evento de riesgo, sino como un trámite con punto de control y mecanismo de reversión.

Lo siguiente del módulo es [`S2.P2 — Práctica deploy básico`](../S2.P2-practica-deploy-basico/MANUAL.md), que es la versión "concentrada" del primer deploy (más útil cuando ya dominas estos conceptos, como referencia rápida sin pre-flight extenso ni retos opcionales). Después se cierra el módulo M02 y empieza **M03 — Azure Functions**, que cambia el paradigma a serverless pero mantiene los principios que has aprendido aquí.
