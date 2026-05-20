# Manual del alumno — S2.3 · Escalado automático y planes de servicio

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: pasos por Portal, scripts `az`, mapeo a slides. Este manual va antes: te cuenta por qué el escalado se diseña antes del pico, no durante, y qué decisiones nuevas aparecen tras tener slots.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M02-S2.3](../../../doc/M02-App-Services/v4-actual/M02-S2.3-escalado-automatico-planes-v4.md). Construye sobre S2.2: misma API con slots, ahora con un `/load/cpu` que quema CPU real, autoscale por métrica y por horario, y graceful shutdown de 30 segundos para que el scale-in no mate peticiones en vuelo.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Tu app va perfecta con una instancia hasta el día que no va. Lo que separa "se cae el viernes a las 17:00 y nadie está mirando" de "Azure añadió dos instancias y nadie se enteró" son **reglas de autoscale** definidas de antemano. Reglas pequeñas, fáciles de entender, que dicen "si la CPU media de los últimos 5 minutos supera el 70%, añade una instancia; si baja del 30%, quita una". El resto lo hace el plan: monitoriza, decide, escala, balancea.

Esta práctica te enseña a configurar esas reglas y, lo que es más importante, a **disparar el escalado en directo** para verlo funcionar. El endpoint `/load/cpu` quema CPU real (busca primos en bucle, no `Thread.Sleep`) — bombardeándolo, la métrica del plan sube y el autoscale dispara. Cinco minutos después tienes dos o tres instancias sirviendo el tráfico. Cuando paras la carga, Azure las retira con calma. Es la primera vez que la mayoría ve Azure crecer y encogerse en vivo, y se queda grabado.

---

## 2. El problema real que hay detrás

Una tienda online lanzó campaña de Black Friday con confianza: su API había aguantado el último año con una instancia B1. La campaña empezó a las 9:00. A las 9:15 los primeros 503s, a las 9:30 la CPU de la única instancia al 100 % y respuestas que tardaban 30 segundos. El operador de guardia subió a mano a tres instancias — escala manual, dos minutos en aplicar —, pero ya era tarde: el pico se había llevado por delante a varios miles de carritos. La cola de soporte de esa mañana fue infame.

La autopsia técnica: la app aguantaba esa carga con tres instancias. El plan podía escalar. **La regla nunca se configuró.** En tres clics se habría puesto "si CPU > 70 %, añade una instancia hasta máximo 5". Tres clics que valen una mañana de incidente. Esa es exactamente la disciplina que entrena este ejemplo: **diseñar las reglas antes del pico**, no durante.

Lo que se aprende, en concreto:

| Decisión | Para qué | Dónde la verás |
| --- | --- | --- |
| **Scale up** (vertical, cambiar SKU) | Más RAM/CPU por instancia, sin downtime | Portal → *Scale up (App Service plan)* |
| **Scale out manual** (fijar N instancias) | Para incidentes o eventos planeados | Portal → *Scale out → Manual scale* |
| **Autoscale por CPU** | Crecer y encoger según carga real | Portal → *Scale out → Custom autoscale* |
| **Autoscale por horario** | Crecer L-V 09:00-19:00, encoger por la noche | mismo sitio, segundo perfil |
| **`/health/details` con JSON detallado** | Dashboards y monitoring serio | [`HealthEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs) |
| **Graceful shutdown de 30 segundos** | Que el scale-in no mate peticiones en vuelo | [`Program.cs`](src/AppService.Demo.Api/Program.cs) → `ConfigureHostOptions` |
| **`Cache-Control` headers** | Reducir carga delegando a CDN / Front Door | [`StaticEndpoints.cs`](src/AppService.Demo.Api/Endpoints/StaticEndpoints.cs) |
| **`/load/cpu?ms=N`** | Generador de carga real para escenificar la demo | [`LoadEndpoints.cs`](src/AppService.Demo.Api/Endpoints/LoadEndpoints.cs) + [`CpuLoadGenerator.cs`](src/AppService.Demo.Api/Services/CpuLoadGenerator.cs) |

Cada una es una piedra del mismo edificio: que la app sobreviva al pico sin que tú estés mirando.

---

## 3. Por qué esto importa en tu stack

Hay dos formas de fallar con el escalado, y la práctica te las enseña al revés de como suele aparecer en la documentación. La primera es **no escalar cuando hace falta** — la historia del Black Friday —. La segunda es **escalar mal**: añadir una instancia cuando no toca, quitarla cuando aún tiene peticiones en vuelo, no precalentar la instancia nueva. La segunda es más sutil y costosa de cazar porque no aparece como un 503 visible; aparece como timeouts intermitentes, latencias que suben sin motivo, errores en producción que en local nunca pasan.

El cambio respecto a S2.2: el plan sigue siendo **Standard S1** (que ya tenías para slots). Autoscale viene incluido. No hay que subir tier para esta práctica, lo que hay que hacer es **configurar reglas** y aprender a **observarlas**.

Una idea que conviene fijar antes de empezar: **autoscale no es magia, es un termómetro y un interruptor**. El termómetro mide una métrica (CPU, RAM, requests, cola de mensajes). El interruptor cambia el número de instancias cuando el termómetro supera un umbral. Tú diseñas el termómetro (qué métrica, qué umbral, qué ventana de tiempo) y el interruptor (cuántas instancias añadir, cuánto esperar entre cambios). Las decisiones que tomas aquí se van a vivir durante meses sin que nadie las toque, así que conviene tomarlas con cuidado.

---

## 4. El modelo mental: el restaurante con camareros que entran y salen

Imagina un restaurante con una norma simple: cuando un camarero se ocupa de menos de tres mesas, tira para casa. Cuando un camarero tiene más de seis mesas a la vez, llama por radio a un compañero y le pide que entre. El encargado no decide nada — solo cuenta mesas por camarero y aplica la regla. A las dos del mediodía hay un camarero. A las dos y media, llegan veinte clientes a la vez y el camarero pide refuerzos: a las dos y treinta y cinco hay tres camareros. A las cuatro, la sala se vacía: dos se van a casa, queda uno hasta la noche.

Eso es **autoscale por métrica**. El restaurante es tu App Service Plan. Cada camarero es una **instancia**. La regla es la **scale-out / scale-in rule**: "cuando la CPU media de cinco minutos supera el 70%, añade un camarero; cuando baja del 30%, retira uno". Los umbrales y la ventana son el termómetro; "añade/retira uno" es el interruptor. El número máximo de camareros (5 en el ejemplo) es el techo, el mínimo (1) es el suelo.

Y hay un detalle que separa restaurantes serios de chiringuitos: cuando un camarero termina su turno, **acaba el servicio de la mesa que tiene en la mano** antes de irse. No deja la cuenta a medias ni el café sin servir. Eso es el **graceful shutdown**. Si el sistema le dice "te vas ya, abandona lo que estás haciendo", los clientes se quejan. Si el sistema le da treinta segundos para terminar, los clientes ni se enteran del cambio.

```
App Service Plan S1
   │
   ├── Default profile (siempre activo)
   │      ├── Min instances: 1
   │      ├── Max instances: 5
   │      └── Rules:
   │             ├── CPU > 70% (5 min)  → +1 instancia (cooldown 5 min)
   │             └── CPU < 30% (10 min) → −1 instancia (cooldown 10 min)
   │
   └── Profile horario (opcional)
          ├── Cuándo: lun-vie 09:00–19:00
          ├── Min: 2, Max: 8, Default: 3
          └── (sobreescribe el default profile en su ventana)
```

Tres frases para fijar el modelo:

- **Cada instancia es independiente.** Comparten el plan (la "sala") y el storage, pero cada una tiene su propio proceso y su propia memoria. El balanceador reparte las peticiones; ninguna instancia sabe lo que están sirviendo las otras.
- **Las reglas son baratas de cambiar, los umbrales son caros de ajustar bien.** Empieza con los valores del ejemplo (70% / 30%, 5 min / 10 min) y ajusta después de tener métricas reales. Cambiar un umbral a 60% no rompe nada; cambiar el código sí.
- **Cooldown no es decoración.** Tras un scale-out, espera 5 minutos antes de poder escalar otra vez. Sin cooldown, una métrica oscilante (CPU 75% un minuto, 65% el siguiente) provocaría que Azure añada y quite instancias en bucle. El cooldown te ahorra el **thrashing**.

Vuelve a la imagen del restaurante cada vez que dudes por qué se hizo una decisión de escalado. La metáfora aguanta.

---

## 5. Las tres formas de escalar (cuándo cada una)

App Service da tres palancas distintas. La práctica las muestra las tres porque cada una tiene su sitio:

### 5.1 Scale up (vertical): cambiar el SKU

`Portal → tu Web App → Scale up (App Service plan)` y eliges otro SKU: de S1 (1 vCore, 1.75 GB) a P1V3 (2 vCore, 8 GB), por ejemplo. **Cero downtime, instantáneo**. Útil cuando la app necesita más RAM o más CPU por instancia, no más instancias.

Cuándo usarla: tu app empieza a consumir muchísima memoria por instancia (cachés grandes, datasets en memoria), o necesitas features de tier superior (P1V3+ para zone redundancy, por ejemplo). Si lo que falta es capacidad para servir más peticiones simultáneas, **scale out (horizontal) es mejor**: dos instancias S1 son más baratas y más resilientes que una P1V3.

### 5.2 Scale out manual: fijar N instancias

`Portal → Scale out (App Service plan) → Manual scale → Instance count → 3`. Espera uno o dos minutos y tienes tres instancias sirviendo. Útil para **eventos planeados** (campaña conocida que empieza a las 9:00) o para **incidentes** (escalas de golpe mientras investigas).

Cuándo NO usarla en producción normal: cualquier carga que tenga picos y valles. Si pones manualmente 5 instancias y la mayor parte del día están al 10%, estás pagando cinco instancias para usar una. Manual es estable y predecible, pero caro.

### 5.3 Autoscale por métrica o por horario: lo que de verdad usas

Aquí está el meollo. La mayoría de apps tiene:

- **Reglas por métrica** para reaccionar a picos imprevistos (CPU > 70% → +1).
- **Perfil por horario** para curva diaria conocida (L-V 09:00–19:00 mínimo 2 instancias, fines de semana mínimo 1).

Los dos se combinan: el perfil horario establece el rango (min/max/default), las reglas por métrica deciden dentro de ese rango. Es **conservar lo predecible y adaptarse a lo imprevisible**.

> 🧠 **La regla práctica para los umbrales.** Empieza con los del ejemplo (70% / 30%, ventanas 5 min / 10 min). Son conservadores y casi nunca dan thrashing. Cuando tengas un mes de métricas reales, mira si la CPU media del peor cuarto de hora estaba por encima o por debajo de tu umbral. Si nunca pasó del 50%, baja el umbral a 60% (o sube el "min" del perfil); si pasó del 70% varias veces sin escalar, sube la sensibilidad (ventana más corta, umbral más bajo).

---

## 6. El `/load/cpu`: CPU real, no `Thread.Sleep`

[`CpuLoadGenerator.cs`](src/AppService.Demo.Api/Services/CpuLoadGenerator.cs) tiene un detalle didáctico crítico: busca primos en bucle hasta agotar el tiempo solicitado. **No usa `Thread.Sleep`**. Y es importante:

```csharp
// MAL: Thread.Sleep no consume CPU
Thread.Sleep(2000);   // El thread duerme; CpuPercentage del plan no sube.

// BIEN: busca primos en bucle
while (sw.Elapsed.TotalMilliseconds < ms)
{
    candidate++;
    if (EsPrimo(candidate)) primesFound++;
}
```

¿Por qué importa? Porque el autoscale se dispara con `CpuPercentage` del plan — la métrica que reporta Azure según el uso **real** de CPU. Si haces `Sleep`, el thread está parado y la CPU no sube. Si bombardes con `Sleep` durante diez minutos, el autoscale no se entera de nada porque la métrica no se mueve. Es uno de esos errores didácticos que se cazan haciendo demos y descubriendo que "el autoscale no funciona" — cuando lo que no funciona es la carga sintética que estabas usando.

[`LoadEndpoints.cs`](src/AppService.Demo.Api/Endpoints/LoadEndpoints.cs) expone el endpoint con validación honesta:

```
GET /load/cpu?ms=2000
→ 200 OK { "generatedMs": 2000, "primesFound": 12345, "instanceId": "..." }

GET /load/cpu?ms=999999
→ 400 Bad Request   (fuera del rango 1..60000)
```

El rango 1..60000 ms (60 segundos máximo por petición) es una pequeña salvaguarda: evita que un cliente despistado deje una instancia al 100% durante 24 horas con una sola llamada. En una demo real, bombardeas con cientos de peticiones de 2 segundos en paralelo (es lo que hace `07-load-test.sh`).

---

## 7. El graceful shutdown que no se ve

[`Program.cs`](src/AppService.Demo.Api/Program.cs) tiene cuatro líneas que parecen administrativas y son la diferencia entre scale-in limpio y scale-in con 502s:

```csharp
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

Cuando el autoscale decide "quita una instancia", App Service:

1. Marca la instancia como "en proceso de baja".
2. El balanceador deja de enviarle peticiones nuevas.
3. **Espera el `ShutdownTimeout`** a que las peticiones en vuelo terminen.
4. Mata el proceso.

Sin el `ShutdownTimeout=30s`, el host de ASP.NET Core tiene su default (5 segundos en muchos casos). Si una petición está en medio de un `await` largo cuando llega la señal de shutdown, **se corta**. El cliente ve un 502, retry, lentitud. Treinta segundos cubren la mayoría de peticiones razonables, y el cliente que tira una petición de un minuto sabe que está al borde de lo aceptable de todas formas.

En tu producción, **mete `ShutdownTimeout=30s` desde el primer día**. Es una de esas cosas que solo aparece como problema cuando tu app empieza a tener tráfico real y autoscale en marcha — momento en el cual estás demasiado ocupado para depurar el motivo.

---

## 8. `Cache-Control`: cómo no escalar (porque no llegan las peticiones)

[`StaticEndpoints.cs`](src/AppService.Demo.Api/Endpoints/StaticEndpoints.cs) muestra dos endpoints idempotentes con headers de caché distintos:

```csharp
// /api/products?limit=N   → Cache-Control: public, max-age=60      (cambia poco)
// /api/categorias         → Cache-Control: public, max-age=3600    (casi inmutable)
```

¿Para qué sirve esto en un módulo de escalado? Porque la mejor forma de escalar es **no recibir la petición**. Si pones un CDN (Azure Front Door, Cloudflare, AWS CloudFront) delante de tu App Service y tus endpoints responden con `Cache-Control: public, max-age=3600`, el CDN te ahorra hasta el 99% del tráfico en endpoints idempotentes. Lo que tu App Service ve son los **misses de caché**: la primera petición de cada minuto (o hora), no las cinco mil que vienen detrás.

Es la palanca más infrautilizada de las apps web modernas y la que más impacto tiene en tu factura cuando aplica. Para que aplique, tus endpoints **tienen que ser idempotentes y cacheables** (`GET` sin secretos por usuario, sin parámetros que cambien por sesión, sin dependencias temporales agresivas). Cuando no aplica (un endpoint depende del usuario logueado, devuelve datos en tiempo real), no se cachea. Pero los endpoints que sí aplican son muchos más de los que la gente piensa: catálogos, configuraciones, contenido estático generado dinámicamente, respuestas de búsqueda agregadas.

---

## 9. Recorrido guiado: del primer pico al scale-in tranquilo

Lanza la API en local primero (sección 11) y prueba los endpoints nuevos. La demo de verdad es en Azure, pero la parte local te sirve para entender qué responde cada cosa.

| # | Petición / acción | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `GET /load/cpu?ms=500` | `200` con `generatedMs: 500, primesFound: ..., instanceId: "local"` | El generador de carga real. |
| 2 | Local: `GET /load/cpu?ms=99999` | `400 Bad Request` con mensaje claro | Validación: el rango 1..60000 evita disparos en el pie. |
| 3 | Local: `GET /health/details` | JSON con `status`, `checks: [...]`, `totalDurationMs` | Health check enriquecido para dashboards (no el que App Service pinga). |
| 4 | Local: `GET /api/products?limit=10` | Array con `Cache-Control: public, max-age=60` | El header que delegará el cacheo al CDN si lo pones delante. |
| 5 | Azure: deploy + configura **autoscale por CPU** (regla 70%/30%, min 1 / max 5) | El plan tiene la regla activa | Antes de generar carga, la red de seguridad está puesta. |
| 6 | Azure: `for i in $(seq 1000); do curl /load/cpu?ms=2000 & done` (script `07-load-test.sh`) | CPU del plan sube por encima del 70% | El bombardeo. *Portal → tu plan → Metrics → CpuPercentage* sube en directo. |
| 7 | Espera 5-7 minutos, mira *Scale out → Run history* | Aparece "Scale out by 1" | Azure aplica la regla y añade una instancia. |
| 8 | `curl /info` repetidamente | `instanceId` empieza a rotar entre las instancias | El balanceador reparte peticiones entre las que hay. |
| 9 | Para la carga, espera 10-15 minutos | "Scale in by 1" en *Run history* | La métrica baja, el cooldown pasa, Azure retira instancias. Las peticiones en vuelo terminan gracias al `ShutdownTimeout`. |
| 10 | Añade el **perfil horario** (lun-vie 09:00-19:00, min 2 / max 8 / default 3) | Aplica solo en la ventana definida | Curva diaria conocida + reacción a picos imprevistos. |

Un experimento que aporta más que cualquier explicación: ejecuta el bombardeo con el script `07-load-test.sh 7 10 2000` (7 minutos, 10 paralelos, 2000ms cada uno) y en otra terminal `08-watch-instances.sh`, que hace polling de `/info` y muestra los `instanceId` distintos. Verás en directo cómo aparecen nuevos identificadores cuando Azure añade instancias. La primera vez que ves a Azure crecer en vivo y luego encoger tranquilamente, el concepto se queda.

---

## 10. Por qué los tests están así

Dieciséis tests en `tests/AppService.Demo.Api.Tests/`. Los nuevos respecto a S2.2 cubren los endpoints nuevos:

- **`LoadEndpointTests` (5 tests)** — happy path + cuatro `[Theory]`/`[InlineData]` con valores fuera de rango (0, -1, 60001, 999999) → 400. Esta es la suite que más se entiende como "test del contrato": el endpoint **promete** aceptar 1..60000 y rechazar todo lo demás, los tests verifican que cumple esa promesa.
- **`HealthDetailsTests` (1)** — `/health/details` devuelve JSON con `status` y `checks`. No prueba el contenido exacto (depende del check registrado), prueba la forma.
- **`StaticEndpointsTests` (2)** — `/api/products` con `max-age=60`, `/api/categorias` con `max-age=3600`. Esto es importante porque los headers de caché son fáciles de cambiar accidentalmente sin que nadie lo note hasta que se ve en una métrica de tráfico. Tener un test que los pin-pone es valor real.

Sigue siendo todo `WebApplicationFactory<Program>`. Para validar el autoscale real, no hay forma sencilla de testear en CI — eso es comportamiento de Azure, y se valida con un game day en el portal.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| Suscripción Azure con plan **Standard S1** mínimo | autoscale requiere Standard+ | Sí (B1 no permite autoscale) |
| `az` CLI en `bash` | scripts de demo | Solo si usas scripts |
| `curl` y `zip` | scripts | Solo si usas scripts |

### 11.2 Compilar y arrancar en local

```bash
cd examples/M02-App-Services/S2.3-escalado-automatico-planes
dotnet build AppService.Demo.Scale.slnx       # 0 errores
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

En local prueba `GET /load/cpu?ms=500` y `GET /health/details`. En local todo es una instancia, claro — la rotación de `instanceId` solo aparece en Azure con autoscale.

### 11.3 Pasar los tests

```bash
dotnet test
```

Resultado: **16 pass · 0 fail**. Sin Azure, en memoria.

### 11.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Los pasos esenciales:

1. **RG + plan S1 + Web App .NET 10** (mismo patrón que S2.2).
2. **Configuration → General**: Always On On, HTTPS Only On, Health check path `/health`.
3. **Configuration → App settings**: `WEBSITE_RUN_FROM_PACKAGE=1`, `WEBSITE_WARMUP_PATH=/health`.
4. Deploy desde VS Code.
5. *Scale out → Custom autoscale*: regla de CPU 70%/30%, min 1, max 5.
6. *(Opcional)* Añadir perfil horario.

### 11.5 Disparar la demo

```bash
# En una terminal, vigila las instancias:
bash scripts/08-watch-instances.sh

# En otra terminal, lanza la carga:
bash scripts/07-load-test.sh 7 10 2000   # 7 min · 10 paralelos · 2000 ms cada uno
```

A los 5-7 minutos, el `08-watch` empezará a mostrar `instanceId` distintos. Cuando pares la carga, espera 10-15 minutos y verás cómo desaparecen los IDs extra.

### 11.6 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| No aparece "Custom autoscale" | tier B1; autoscale requiere S1+ | sube el plan en *Scale up* |
| La carga corre pero no se añaden instancias | la regla mira `CpuPercentage` y la app no consume CPU real (¿usaste `Sleep` en lugar del endpoint?) | confirma que estás llamando a `/load/cpu`, no a otro endpoint |
| Las instancias se añaden y se quitan en bucle | thrashing por cooldown demasiado corto | sube los cooldowns a 5 min (out) y 10 min (in) |
| Veo 502s al hacer scale-in | el `ShutdownTimeout` no está configurado o es muy corto | verifica que `ConfigureHostOptions` está en `Program.cs` con 30 s |
| `instanceId` no rota aunque hay 3 instancias | affinity cookie del navegador | usa `curl` o añade `?x-ms-routing-name=` |
| El bombardeo no sube la CPU media | bombardeas pocas peticiones o `ms` demasiado bajo | sube paralelismo (10-20) y `ms` (2000-3000) |

### 11.7 Limpieza

`Portal → Resource groups → rg-curso-m02-s23 → Delete`. Borra plan + web app + reglas + perfiles.

---

## 12. Ideas para llevarte

Lo más útil de esta práctica no es ningún comando suelto, es **la disciplina de configurar autoscale antes de necesitarlo**. Si tu app va a producción sin reglas de escalado, el primer pico real te encuentra desprevenido. Las reglas básicas (70%/30%, min 1, max 5) tardan tres minutos en configurarse y cubren la mayoría de los casos. Ajustarlas después con métricas reales es trivial; tenerlas vacías el día del incidente es caro.

Sobre las **tres palancas de escalado**, la regla práctica: **scale up cuando necesitas más por instancia** (más RAM, más CPU dedicada, features de tier superior), **scale out automático cuando necesitas más instancias para servir más tráfico**, **scale out manual para incidentes o eventos planeados con minutos de antelación**. Dos instancias S1 son casi siempre mejor opción que una P1V3 — más resilientes (si una se cae, la otra sirve) y más baratas.

Y un consejo que pocos cuentan: **mete `ShutdownTimeout=30s` y `Cache-Control` en endpoints idempotentes desde el primer día**. Ninguno de los dos rompe nada si no hay tráfico — solo se notan cuando llega el tráfico, y entonces son la diferencia entre "scale-in limpio" y "scale-in con 502s", o entre "1000 peticiones/segundo en tu app" y "10 peticiones/segundo en tu app porque el CDN absorbió el resto". Las dos cosas son una línea de código cada una.

---

## 13. Comprueba que lo has entendido

1. Tu app aguanta picos con tres instancias pero con una se cae. ¿Eliges scale up o scale out? ¿Por qué? *(sección 5)*
2. ¿Por qué `/load/cpu` usa una búsqueda de primos en bucle y no `Thread.Sleep`? *(sección 6)*
3. ¿Qué pasa si configuras una regla de scale-in con cooldown de 30 segundos y la métrica oscila justo en el umbral? *(sección 4)*
4. Tu app empieza a tirar 502s justo cuando autoscale hace scale-in. ¿Qué falta y dónde se configura? *(sección 7)*
5. ¿Para qué sirve `Cache-Control: public, max-age=3600` en `/api/categorias` desde el punto de vista del escalado? *(sección 8)*
6. ¿En qué se diferencia un perfil horario de una regla por métrica, y por qué suelen usarse juntos? *(sección 5)*

<details>
<summary>Respuestas</summary>

1. **Scale out**, no scale up. Si la app aguanta con tres instancias, lo que falta es **capacidad horizontal**: poder servir más peticiones simultáneas. Dos o tres instancias S1 son más baratas y más resilientes que una instancia P1V3 con el mismo presupuesto (si una se cae, las otras siguen). Scale up es la palanca correcta cuando una sola instancia no tiene suficiente RAM o CPU para una sola petición (cachés grandes, procesos pesados), no cuando faltan instancias para repartir tráfico.
2. Porque el autoscale por CPU mira la métrica `CpuPercentage`, que reporta **uso real de CPU**. `Thread.Sleep` deja el thread dormido sin consumir CPU — la métrica no sube, autoscale no se entera. La búsqueda de primos hace trabajo real y sube la métrica. Es un error didáctico que se caza haciendo demos: "la regla no funciona" cuando lo que no funciona es la carga sintética.
3. **Thrashing**: la métrica oscila por encima/debajo del umbral cada pocos segundos (CPU 72%, 68%, 71%, 69%...), Azure añade una instancia, espera 30s, ve que la CPU bajó al añadir capacidad, quita instancia, ve que sube otra vez, añade, quita... Las instancias se crean y destruyen en bucle, los clientes ven errores 502 en cada scale-in (no hay tiempo de graceful shutdown), la factura sube. **Cooldowns largos** (5 min scale-out, 10 min scale-in) evitan esto: tras un cambio, no se puede volver a tocar hasta que pase el cooldown y la métrica se estabilice.
4. Falta el **graceful shutdown** configurado a 30 segundos: `builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30))` en `Program.cs`. Sin esto, el host de ASP.NET Core usa su default (típicamente 5 s), las peticiones en vuelo se cortan abruptamente cuando llega la señal de shutdown del scale-in, el cliente recibe 502s. Treinta segundos cubren la mayoría de peticiones razonables y App Service espera a que la instancia termine antes de matarla.
5. Sirve para que un CDN (Azure Front Door, Cloudflare) **delante** de tu App Service cachee la respuesta en su edge durante 3600 s (una hora). Las peticiones siguientes en esa hora **no llegan a tu app** — las sirve el CDN. Reduces drásticamente la carga que tu App Service ve y, por tanto, el número de instancias que necesitas. Es la mejor forma de "escalar" cuando aplica: no escalando, sino delegando. Aplica a endpoints idempotentes (sin contexto por usuario, datos que cambian con periodicidad conocida).
6. **Una regla por métrica** reacciona a algo medible en tiempo real (CPU > 70%): es para picos imprevistos. **Un perfil horario** establece min/max/default según el reloj (L-V 09:00-19:00 min 2 / max 8): es para curva diaria predecible. Se usan juntos porque cubren cosas distintas: el perfil garantiza que en horario laboral hay un suelo razonable (la primera petición del día no espera al cold start de la instancia 2), y las reglas reaccionan al pico imprevisto dentro de ese rango. El default profile siempre está activo; los profiles adicionales lo sobrescriben en sus ventanas.

</details>

---

## 14. Hasta aquí

Vuelve a la imagen del restaurante con camareros entrando y saliendo. Una regla simple, aplicada por el encargado sin discutir, y el restaurante sobrevive a la hora punta sin caída de servicio ni camareros agotados. Ese es el mismo principio que aplicado a Azure se llama autoscale, y tu app puede tenerlo configurado en tres clics. Hacerlo el primer día evita el incidente que casi siempre llega más tarde.

Lo siguiente es [`S2.4 — Variables de conexión y configuración segura`](../S2.4-variables-conexion-config-segura/MANUAL.md). Hasta aquí las settings han sido inocentes: greetings, versiones, listas de orígenes. En S2.4 entran los secretos de verdad: connection strings, claves de APIs, certificados. Y aparece **Key Vault references** como el patrón estándar para tenerlos fuera de App Settings. Es la pieza que cierra "App Service en serio" antes de pasar a monitoring y la práctica final.
