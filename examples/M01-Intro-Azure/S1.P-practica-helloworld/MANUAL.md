# Manual del alumno — S1.P · Hello World end-to-end

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica de la práctica: scripts paso a paso, mapeo a slides, comandos de despliegue por Portal. Este manual va antes: te cuenta por qué esta es la primera práctica del curso, qué demuestras al terminar y cómo leerla.

Tiempo de lectura: ~20 min. Práctica de referencia: [M01-S1.P](../../../doc/M01-Intro-Azure/v5-actual/M01-S1.P-practica-helloworld-v5.md). Esta es la **práctica más importante del módulo M01** porque es la que rompe la barrera de "lo veo en diapositivas" y te lleva por primera vez a "tengo una URL pública con mi nombre dentro respondiendo desde Irlanda".

*Creado: 2026-05-20 08:30 +0200*

---

## 1. La idea en una frase

Tu primer encuentro con Azure no debería ser una hora de configuración antes de ver nada. Debería ser un `curl https://app-curso-tunombre.azurewebsites.net/` que te devuelve tu nombre, el entorno (`Production`), y un nombre de servidor que claramente no es tu portátil. En menos de una hora, sin tarjeta de crédito, sin tier de pago.

Eso es S1.P. La primera práctica del curso entero. El primer cierre del bucle "código en mi máquina → producción real". Todo lo que viene después —Functions, Storage, Cosmos, Managed Identity— monta sobre la misma base: un Resource Group, un App Service Plan, un recurso que paga (en este caso F1, que es gratis). Si entiendes este recorrido en una sentada, los diez módulos restantes dejan de parecer un mar de servicios y se convierten en variaciones de la misma plantilla.

---

## 2. El problema real que hay detrás

Llevas años haciendo `dotnet run`, abriendo `http://localhost:5000` y viendo cómo responde tu API. Funciona, es satisfactorio, pero hay un detalle: en localhost siempre eres el único usuario, en una máquina cuyo IP no sirve para nada fuera de tu casa. Cuando llega el día de que alguien más use tu código, "darle el repo y que lo lance en su portátil" deja de ser respuesta.

La diferencia entre tu localhost y un sitio público viene con su propia familia de preguntas. ¿Dónde vive el binario? ¿Qué máquina lo arranca? ¿Quién la enciende a las 8:00 si se cayó por la noche? ¿Quién se entera si tarda mucho? ¿Cómo cambio una configuración sin redesplegar todo? ¿Cómo lo apago cuando no lo necesite para que no me pase factura?

App Service en Azure responde a todas esas preguntas con un servicio gestionado. Tú subes el binario; Azure se encarga del OS, del runtime, del reinicio cuando algo falla, de las métricas, del despliegue continuo, del log streaming, del autoscaling cuando creces. Y, en el tier F1 (gratis), Microsoft incluso te lo regala una hora de CPU al día para que practiques sin coste.

Esta práctica pone todas esas piezas delante. En orden, sin atajos, con tus manos.

| Pieza | Para qué | Dónde la verás |
| --- | --- | --- |
| **Resource Group** | Contenedor lógico que agrupa todos los recursos del proyecto | `01-provision.sh` o Portal → *Resource groups* |
| **App Service Plan F1** | "Hardware" gratuito que ejecuta tu app | mismo script o Portal → *App Service plans* |
| **Web App .NET** | Tu hosting concreto, con URL pública y stack runtime configurado | mismo script o Portal → *App Services* |
| **`Program.cs` con 5 endpoints** | El código que sirve | [`src/hello-world/Program.cs`](src/hello-world/Program.cs) |
| **App Settings** | Configuración sin redesplegar (lo que ahorra tiempo en producción) | endpoint `/api/info` + Portal → *Configuration* |
| **Application Insights** *(opcional)* | Telemetría: métricas, trazas, errores | `05-setup-app-insights.sh` |

---

## 3. Por qué esto importa en tu stack

Si haces .NET y tu trabajo no incluye nube todavía, probablemente lo incluirá pronto. Y la primera vez que abres el portal de Azure sin contexto, es paralizante: cientos de servicios, jerga propia, naming convention extraña. Esta práctica te da la **plantilla mental mínima**: cuatro o cinco conceptos, en orden, sin distracciones.

Y hay una decisión deliberada que conviene marcar antes de empezar: el tier F1 es **gratis** pero tiene límites estrictos. 60 minutos de CPU al día, sin slots de despliegue, sin custom domains, comparte recurso con otras apps F1 en el mismo plan. Eso significa que la app te puede tardar 30-60 s en responder la primera vez (cold start) y que pasadas las 24 h de tope de CPU empieza a devolver 403s. **En producción real no usarías F1**. En una práctica de curso es lo correcto: cero coste, todo lo demás funciona igual.

El hilo conductor: el RG y la app que creas aquí los **vas a reutilizar** en M02-S2.P (slots y swap). El cleanup de la práctica te pregunta si quieres conservar el RG para continuar — di que sí. Esa continuidad es una decisión del curso: cada módulo monta sobre la infraestructura del anterior y no te hace empezar de cero.

---

## 4. El modelo mental: tres capas, una URL pública

Antes de teclear un solo comando, fija esta imagen en la cabeza:

```
Suscripción Azure   (la cuenta donde se factura todo)
   │
   ▼
Resource Group "rg-curso-azure-<tu-nombre>"     (contenedor lógico, no cuesta)
   ├── App Service Plan F1                       (el "hardware", gratis)
   │       └── Web App                            (tu hosting concreto)
   │              └── https://app-curso-<tu-nombre>.azurewebsites.net
   └── (opcional) Log Analytics + App Insights   (telemetría)
```

Tres frases para fijar las decisiones que vienen detrás:

- **El Resource Group no cuesta dinero.** Es una etiqueta. Lo que cuesta son los recursos que viven dentro. Borrar el RG borra cascada todo. Es la operación "nuclear" para limpiar al terminar — y por eso conviene crearlo con un nombre claro y agruparlo todo dentro.
- **El App Service Plan es el hardware, la Web App es la aplicación.** Un mismo plan puede alojar varias web apps (mientras el plan dé para todas). En F1 prácticamente solo cabe una. Cuando subas a tiers de pago en M02, el plan dejará de ser un detalle administrativo y empezará a ser una decisión de coste.
- **La URL es global y única.** `app-curso-pedro.azurewebsites.net` no la puedes tener si "pedro" ya estaba cogido. El sufijo numérico (`app-curso-pedro-2`) o el sufijo personal (`app-curso-pedro-2026`) son las salidas habituales. Esta restricción es la primera vez que el alumno se topa con el concepto "nombre global" en Azure, y conviene contarlo antes de que aparezca como error oscuro.

Vuelve a esta imagen cuando algún script falle con "el nombre ya existe" o cuando no encuentres un recurso en el portal: el orden es siempre Suscripción → RG → Plan → App.

---

## 5. Tour del código

[`Program.cs`](src/hello-world/Program.cs) es **intencionalmente plano**. Sin DI customizada, sin Options pattern, sin servicios extra. La razón es didáctica: es la primera práctica del curso, queremos que se lea como el material lectivo, sin elementos que distraigan del flujo. Cuando llegues a M03 o M05 verás Program.cs con grafos de DI complejos; este aún no es ese sitio.

Cinco endpoints, los tres últimos son los retos opcionales pero **ya están implementados** para que los puedas probar sin escribir código adicional:

```csharp
GET /              → JSON con asistente, entorno, servidor, hora_utc, runtime, os
GET /health        → { status: "healthy" }                          (slide 50)
GET /api/info      → lee CURSO_MODULO/CURSO_SESION/CURSO_FECHA      (reto 1, slide 69)
GET /api/echo?msg= → eco con validación (400 si falta msg)          (reto 2, slide 70)
GET /api/version   → metadatos del Assembly                          (reto 3, slide 71)
```

Lo importante de la respuesta del endpoint raíz son los **campos diagnósticos**: `entorno`, `servidor`, `hora_utc`, `runtime`, `os`. Esos cinco cambian cuando pasas de local a Azure y son tu primera prueba visual de que el deploy funcionó.

En local:
- `entorno: "Development"` — el `ASPNETCORE_ENVIRONMENT` que setea el launchProfile.
- `servidor: "TU-PORTATIL"` — `Environment.MachineName` de tu máquina.
- `runtime: ".NET 10.x"` con la versión exacta del SDK.

Después de desplegar a Azure:
- `entorno: "Production"` — App Service lo setea por defecto.
- `servidor: "DW1SDWK0012DF"` (o algo parecido) — un nombre de máquina virtual de Microsoft que claramente no es tu portátil.
- `runtime` y `os` con la plataforma de Azure (Linux, en este caso, por el stack `DOTNETCORE:10.0`).

Esa diferencia entre los dos JSON es la prueba más visceral de que has cruzado la línea. Y no necesita nada sofisticado para verla: un `curl` y mirar el campo `servidor`.

> 🧠 **La decisión "leer config con `IConfiguration` en lugar de variables de entorno".** El endpoint `/api/info` lee `CURSO_MODULO` etc. desde `config["CURSO_MODULO"]`, no desde `Environment.GetEnvironmentVariable("CURSO_MODULO")`. ¿Por qué? Porque en local lee de `appsettings.json` y en Azure lee de App Settings, **sin cambiar el código**. App Service inyecta cada App Setting como variable de entorno, y `IConfiguration` lo recoge transparentemente. Lo que ganas: una sola línea de código y dos comportamientos correctos.

---

## 6. La idea pedagógica de App Settings

Hay un momento en esta práctica que merece subrayado. Después de desplegar la app, el endpoint `/api/info` devuelve `"modulo": "no definido"`. Vas al portal, *Configuration → Application settings → New application setting*, añades `CURSO_MODULO = 1`, le das a *Save*. La app se reinicia en ~30 segundos, vuelves a hacer `curl` al endpoint y ahora dice `"modulo": "1"`.

Eso es App Settings. **Cambiar configuración sin redesplegar el código.** Suena obvio escrito así, pero es la mitad del valor de App Service. En producción, cuando descubras que el connection string apuntaba al ambiente equivocado, no quieres redesplegar quince minutos: quieres cambiar una variable y reiniciar treinta segundos. Esa diferencia, multiplicada por incidentes al año, vale el tier de App Service entero.

Y por eso este paso es opcional en la guía pero recomendable hacerlo siempre: practicas el patrón que vas a usar en cada proyecto Azure de tu vida profesional.

---

## 7. Recorrido guiado: del repo al endpoint público

Esta práctica tiene dos caminos: paso a paso por **Portal** (canónico, está en el README) y con **scripts `az`** (rápido, también en el README). El recorrido conceptual es el mismo:

| # | Paso | Qué demuestra |
| --- | --- | --- |
| 1 | Pre-flight: `dotnet --list-sdks`, `az --version`, `az account show` | El alumno tiene las herramientas mínimas y está logueado en la suscripción correcta. La mitad de los problemas se cazan aquí. |
| 2 | Crear Resource Group con tags | El contenedor lógico. Las tags (`curso=AZ-204`, `owner=...`) son para gobernanza — luego en S1.P2 las usarás para filtrar. |
| 3 | Crear App Service Plan F1 | El hardware. F1 es gratis y suficiente. Linux es el OS estándar para .NET en App Service en 2026. |
| 4 | Crear Web App .NET 10 con `healthCheckPath=/health` | La aplicación concreta. La URL ya es pública aunque no hayas desplegado nada. El health check le dice a App Service "estoy vivo". |
| 5 | Deploy: `dotnet publish` + zip + zip deploy (o desde VS Code) | El primer build y subida real. Si hay error de compilación, lo cazas aquí; si hay error de runtime, en el siguiente paso. |
| 6 | `curl https://<app>.azurewebsites.net/` | El momento. El JSON con tu nombre, `entorno: "Production"` y un servidor que no es tu portátil. Si llegaste hasta aquí, has cerrado el bucle. |
| 7 | Añadir App Settings y verificar `/api/info` | El patrón de configuración sin redesplegar (sección 6). |
| 8 | *(Opcional)* Application Insights | Telemetría real: cada petición se ve en *Live Metrics* del portal. Lo opcional aquí no es por importancia, es por tiempo. |
| 9 | *(Opcional)* Security defaults: HTTPS only + TLS 1.2 + FTPS Disabled | El primer paso hacia "esto está en producción". Tres clics. |
| 10 | Cleanup: conservar RG o borrar todo | Si vas a continuar con M02-S2.P, conserva el RG. Si no, borra. |

Un experimento que aporta más que la teoría: tras desplegar, abre `Live Metrics` (si configuraste App Insights) y haz cinco `curl` desde tu terminal. Verás las peticiones aparecer en tiempo real con su latencia, su payload de respuesta, el código de estado. Es la primera vez que ves tu propio tráfico en una herramienta de observabilidad cloud. Treinta segundos de "ah, esto era".

---

## 8. Por qué los tests están así

```bash
dotnet test    # 10 tests, todos verdes
```

Diez tests xUnit que cubren los cinco endpoints. Hay dos patrones que merecen comentario porque se repetirán en el curso:

- **`WebApplicationFactory<Program>`** — el patrón estándar para testear una API ASP.NET de extremo a extremo, levantando el host real en memoria. Aquí cubre los 5 endpoints reales con peticiones HTTP completas. Por eso `Program.cs` termina con `public partial class Program;` — para que el TestHost pueda hacer referencia al tipo.
- **`[Theory]` con `[InlineData]`** — `ApiEchoTests` lo usa para probar tres valores inválidos (`null`, `""`, `" "`) contra el mismo test, obteniendo cobertura sin duplicar código. Verás este patrón cada vez que el curso valide casos límite.

No hay aún capas (unit / component / integration) como en M05: es una práctica introductoria, todo se prueba con `WebApplicationFactory`. Esa diferenciación llega en M05.

---

## 9. Puesta en marcha

### 9.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x (`dotnet --list-sdks`) | compilar y ejecutar | Sí |
| Azure CLI (`az --version`) ≥ 2.65 | scripts y verificaciones | Sí |
| Suscripción Azure activa (`az account show`) | desplegar el recurso real | Sí |
| Tenant correcto y rol Contributor | crear recursos en la suscripción | Sí |
| VS Code con extensión **Azure App Service** | despliegue por UI (alternativa a `az`) | Recomendado |

Errores típicos de pre-flight:
- `No subscriptions found` → `az login --tenant <tenant-id>`.
- `Forbidden` al crear el RG → no tienes rol Contributor; pídelo o usa tu propia subscription (la del Free Trial vale).

### 9.2 Compilar y arrancar en local

```bash
cd examples/M01-Intro-Azure/S1.P-practica-helloworld
dotnet build HelloWorld.slnx         # 0 errores
dotnet run --project src/hello-world --launch-profile http
# → http://localhost:5000
```

Prueba `curl http://localhost:5000/` y comprueba que el JSON dice `entorno: "Development"`, `servidor: TU-PORTATIL` y `runtime: ".NET 10.x"`. Ese es el "antes" — el "después" lo verás cuando despliegues a Azure.

### 9.3 Pasar los tests

```bash
dotnet test         # 10 pass · 0 fail
```

Sin Azure, sin Docker. Es una suite de extremo a extremo en memoria.

### 9.4 Desplegar a Azure (resumen)

Sigue los pasos del [`README.md`](README.md) — la práctica está documentada en detalle ahí, tanto por Portal como con scripts. Los pasos esenciales son:

```bash
cd scripts
cp .env.demo.example .env.demo       # editar SUBSCRIPTION_ID, iniciales, ASISTENTE

bash 01-provision.sh                 # RG + plan F1 + web app
bash 02-deploy.sh                    # dotnet publish + zip + zip deploy
bash 03-app-settings.sh              # Asistente + CURSO_*
bash 04-smoke-test.sh                # verifica los 5 endpoints en la URL pública

# Opcionales:
bash 05-setup-app-insights.sh        # Log Analytics + Application Insights
bash 06-secure-defaults.sh           # HTTPS Only + TLS 1.2 + FTPS Disabled
```

El smoke test (`04-smoke-test.sh`) es la versión automatizada del recorrido guiado de la sección 7. Si los cinco endpoints responden 200 con el contenido esperado, la práctica está en verde.

### 9.5 Problemas frecuentes (los reales)

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `The webapp name is already taken` | Otro alumno tiene ese nombre global | Añade un sufijo: `app-curso-pedro-2` |
| 503 durante 1-2 min tras el deploy | Cold start del F1 | Espera 30-60 s más y refresca |
| 503 persistente >3 min | La app crasheó al arrancar | `az webapp log tail -n $APP -g $RG`, lee la excepción |
| `entorno: "Development"` en Azure | Variable hardcoded o App Setting sobrescrita | Comprueba *Configuration*; Azure mete `Production` por defecto |
| 403s aleatorios tras mucho uso | F1 superó 60 min de CPU/día | Espera al reset UTC 00:00 o sube a B1 (~10 €/mes) |
| Cambios no aparecen tras deploy | Caché del navegador | `curl` directo o `Ctrl+Shift+R` |

Si nada funciona, **abre Kudu** (`https://<app>.scm.azurewebsites.net` → *Debug Console* → `D:\home\site\wwwroot`) para ver los archivos realmente desplegados. Esa herramienta resuelve el 90% de los misterios.

### 9.6 Cleanup (conservar o borrar)

```bash
bash 07-cleanup.sh        # te pregunta: conservar RG (para M02) o borrar todo
```

Si vas a hacer M02-S2.P, conserva el RG y borra solo la Web App y el Plan. Si terminaste el curso o no continúas, borra el RG entero — Azure deja de cobrarte por todo lo que estaba dentro.

---

## 10. Ideas para llevarte

Lo más valioso que sale de esta práctica no es el código (es trivial), es el **mapa mental**: Suscripción → RG → Plan → App → URL. Esa secuencia es la misma para cualquier servicio de Azure que toques en el resto del curso. En M03 será RG → Function App → Function. En M05 será RG → Storage Account → Container. En M06 será RG → App Service → autenticación. La capa más alta nunca cambia.

La segunda lección, menos obvia, es **App Settings sin redesplegar**. Es lo más cercano que tiene App Service a "una variable de entorno bien hecha", y es lo que evita despliegues innecesarios cuando solo cambia configuración. En producción te ahorras horas de pipeline; en desarrollo te ahorras frustración. Practícalo cada vez que pongas algo configurable.

Y una recomendación honesta: **no te saltes los opcionales** (Application Insights, security defaults). Son tres clics adicionales y te enseñan dos cosas que vas a necesitar todos los días en proyectos reales: ver qué hace tu app cuando no la estás mirando, y endurecer una app web con tres ajustes que la elevan de "demo de aula" a "primera versión que se puede defender".

---

## 11. Comprueba que lo has entendido

1. ¿Para qué sirve un Resource Group y por qué crear uno con tags? *(sección 4)*
2. Tras desplegar, el JSON de `/` dice `entorno: "Development"`. ¿Qué pasó y dónde lo arreglas? *(sección 9.5)*
3. ¿Por qué F1 no se usa en producción real, aunque sea suficiente para esta práctica? *(sección 3)*
4. Cambias el valor de un App Setting en el portal. ¿Tienes que redesplegar la app? ¿Cómo lo verifica el endpoint `/api/info`? *(sección 6)*
5. ¿Cuál es la diferencia operativa entre **App Service Plan** y **Web App**? ¿Puede haber varias web apps en un mismo plan? *(sección 4)*
6. ¿Por qué los nombres tipo `app-curso-pedro` tienen que ser únicos a nivel global? *(sección 4)*

<details>
<summary>Respuestas</summary>

1. Un Resource Group es un contenedor lógico que agrupa recursos relacionados. No cuesta dinero por sí mismo; lo que cuesta son los recursos que contiene. Las tags (`curso=AZ-204`, `owner=...`) sirven para gobernanza: filtrar recursos por proyecto, asignar costes, identificar al responsable. La operación "borrar el RG" elimina cascada todo lo que hay dentro — es la limpieza "nuclear" al terminar.
2. Algo está sobrescribiendo el `ASPNETCORE_ENVIRONMENT`. Probablemente un App Setting con `ASPNETCORE_ENVIRONMENT=Development` que se quedó por error, o un valor hardcoded en `Program.cs`. Lo arreglas en *Configuration → Application settings*: borra esa entrada si existe; Azure setea `Production` por defecto y el JSON debería mostrarlo.
3. Porque F1 tiene límites duros: 60 min de CPU/día, sin slots de despliegue, sin custom domains, comparte recursos con otras apps F1, cold start largo. Cualquier app real necesita uno o más de esos límites superados (slots para deploy sin downtime, domain propio, CPU continuo). El tier de entrada razonable para producción pequeña es B1 (~10 €/mes); para producción real, S1 o P1V3.
4. No, no tienes que redesplegar. Cambiar un App Setting reinicia la app (~30 s) y los nuevos valores están disponibles cuando termina el reinicio. `/api/info` los lee con `IConfiguration` (no con `Environment.GetEnvironmentVariable`), así que en local lee de `appsettings.json` y en Azure lee de los App Settings — la misma línea de código sirve para los dos entornos.
5. El **App Service Plan** es el hardware (CPU/RAM/disco) que ejecuta una o más web apps. La **Web App** es la aplicación concreta, con su URL, su stack runtime, sus settings. Sí, un plan puede alojar varias web apps si tiene capacidad — en F1 prácticamente cabe una, pero en S1 o superior puedes meter varias y compartir el coste. Es la diferencia entre "un servidor" y "una aplicación que corre en ese servidor".
6. Porque la URL `<app>.azurewebsites.net` es un subdominio del dominio público de Azure, y los DNS no permiten dos registros idénticos. Tu `app-curso-pedro` se traduce literal en `app-curso-pedro.azurewebsites.net`. Si "pedro" ya estaba cogido, Azure rechaza la creación. Los sufijos (numéricos, año, iniciales adicionales) son la solución habitual. En producción real, esto se evita con un *custom domain* — tu propio dominio apuntando vía CNAME a la app.

</details>

---

## 12. Hasta aquí

S1.P es la barrera de entrada del curso. Cuando termines, tienes la confianza de que el ciclo "código → Azure → URL pública" no es magia: son cuatro pasos en orden, un script que los automatiza y un patrón mental que se repite en todos los módulos.

Lo siguiente natural es [`S1.P2 — Práctica desde Cloud Shell`](../S1.P2-practica-cloud-shell/MANUAL.md). No tiene código .NET — es puramente CLI desde el navegador, sin instalar nada. Sirve para que aterrices el otro extremo: cómo gestionar Azure sin tu portátil. Las dos prácticas juntas son el cinturón de herramientas mínimo del curso. A partir de M02, sobre esta misma infraestructura, empiezas a meter slots, swap, deploy slots, monitoring serio y todo lo que diferencia "tengo algo en Azure" de "tengo algo en Azure que se puede mantener".
