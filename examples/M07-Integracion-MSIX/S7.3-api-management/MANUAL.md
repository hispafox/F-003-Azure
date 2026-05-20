# Manual del alumno — S7.3 · Azure API Management

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué hace exactamente un API gateway por ti, por qué tener una sola URL de entrada cambia tu sistema, y por qué la diferencia entre Consumption (gratis) y Premium (2200 € al mes) es el detalle que te va a tocar defender más veces ante el equipo.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M07-S7.3](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.3-api-management-v3.md). Tres piezas de lógica pura (evaluador de policies inbound en orden, resolvedor de versionado, advisor de tier) más un planificador con checklist del entregable.

*Creado: 2026-05-20 20:15 +0200*

---

## 1. La idea en una frase

Azure API Management es un **gateway** que se pone delante de tus APIs y se ocupa de las cosas transversales: autenticación con subscription keys u OAuth2, rate limiting y cuotas por consumidor, caching, transformaciones de request/response, versionado, alertas, developer portal automático. Tu app backend queda limpia: solo se ocupa de la lógica de negocio. APIM es el portero que mira los carnets, controla la cola, redirige al departamento correcto y archiva las visitas — todo lo que en proyectos sin gateway se acaba escribiendo a mano en cinco sitios distintos.

El ejemplo modela tres decisiones: el orden en que APIM evalúa las **policies inbound** (la sutileza que decide si tu petición devuelve 401, 403, 429 o 200), los tres **esquemas de versionado** (Segment, Query, Header — con Segment como la recomendación firme), y el **árbol de decisión de tier** con sus costes (de 0 € a 2200 € al mes según el escenario).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican meter un gateway al sistema:

**Caso 1 — el rate limiting copiado en cinco apps.** Un equipo tenía cinco APIs distintas (productos, pedidos, usuarios, pagos, envíos). Cada una había implementado rate limiting a mano con un middleware. Cuando llegó la integración con un partner externo y hubo que aplicar un límite específico para él (más generoso), tuvieron que cambiar el código de las cinco APIs. Cuando un developer cambió la lógica en cuatro pero olvidó la quinta, el partner descubrió el agujero y empezó a llover tráfico no contado. La migración a APIM: una política `rate-limit-by-key` aplicada a un product compartido. **Una sola configuración, cinco APIs protegidas.**

**Caso 2 — el OAuth2 al backend desde el frontend.** Otra empresa empezó moderna: cada API validaba JWTs con Microsoft.Identity.Web y el frontend mandaba el token directamente. Funcionaba. Pero quisieron exponer una de las APIs a un partner externo que no era usuario de Entra ID — tenía su propio sistema de auth con API keys. La opción "soportar dos sistemas de auth en cada API" era inmantenible. La solución: APIM delante. El partner manda su `Ocp-Apim-Subscription-Key`; APIM la valida, **inyecta el JWT de un Service Principal técnico** y el backend recibe lo de siempre. Sin tocar el código del backend, el partner externo trabaja contra el mismo sistema.

**Caso 3 — el versionado que se descontroló.** Una API REST tenía clientes en producción usando lo que ellos llamaban "v1, v2 y v3". Cuando alguien hacía un cambio, no estaba claro qué clientes seguían en v1 y cuáles habían migrado. La gestión de versiones consistía en hacer commits con cuidado y rezar. La migración a APIM con **version set**: cada cliente tiene un product asignado con una versión concreta. Cuando quieres deprecar v1, en el portal ves exactamente qué subscriptions siguen ahí, mandas un email a esos clientes, y dejas la versión en "retirada" hasta que migren. **Versionado como datos**, no como esperanza.

Los tres casos resuelven el mismo problema: cuando tienes más de una API o más de un consumidor, las cosas transversales (auth, rate limit, versionado, caching) se acaban duplicando. Un gateway las centraliza. APIM es el gateway de Azure.

---

## 3. Por qué esto importa en tu stack

Si tu sistema tiene **dos o más APIs** o **dos o más tipos de consumidor** (usuarios, partners, mobile, devices), un gateway empieza a tener sentido. Tres preguntas que conviene zanjar pronto:

- **¿Quién consume mi API y cómo se autentica?** APIM separa subscription key (para identificar al consumidor) de OAuth2 (para identificar al usuario). Puedes pedir ambos: app+usuario. Es lo que enseña la slide 8.
- **¿Cómo versiono?** Segment (`/v1/productos`) es el más claro y el recomendado. Query (`?api-version=v1`) y Header (`Api-Version: v1`) existen y tienen su nicho pero son menos visibles en logs y trazas.
- **¿Qué tier elegir?** Consumption (gratis hasta 1M llamadas/mes) cubre el 90% de los casos. Standard y Premium cuestan dinero serio y solo se justifican con SLA, VNet o multi-región.

Si tienes las respuestas, APIM es una herramienta poderosa. Sin ellas, te pueden vender Premium "por seguridad" cuando Consumption sirve, y te dejas 2000 € al mes en la mesa.

---

## 4. La analogía vertebradora: el control de aduanas

Imagina un aeropuerto internacional con varias líneas aéreas operando dentro:

- **La entrada al aeropuerto** es una sola: pasas por seguridad, te miran el pasaporte, te identifican como pasajero válido. Eso es **APIM**: una sola URL pública de entrada para todas tus APIs (`api.empresa.com`).
- **El control de pasaportes** verifica quién eres antes de dejarte entrar a la zona de embarque. Si no tienes pasaporte o está caducado, te paran ahí. Eso es **`validate-jwt`** o **`subscription-key`**: la primera barrera de auth.
- **El control de aduana** te dice qué puedes llevar contigo y cuánto. "Has comprado más de 1.000 € de licores en el último año, así que ya no puedes meter más" — eso es **`quota-by-key`**. "Has pasado por aquí tres veces hoy, espera un minuto antes de pasar otra vez" — eso es **`rate-limit-by-key`**.
- **El cartel de la blacklist** lista pasaportes prohibidos. Si tu IP está en la blacklist, no pasas. Eso es **`ip-filter`**.

Y luego, una vez dentro de la zona de embarque, **cada línea aérea tiene su mostrador** (los backends: tu Function App de pedidos, tu App Service de productos, tu API de usuarios). El pasajero ya pasó la seguridad, así que no tienen que volver a comprobarlo. Cada mostrador solo se ocupa de su lógica de negocio: dar la tarjeta de embarque, cobrar el equipaje extra, etcétera.

El **orden** del control es importante:

1. Primero el pasaporte (subscription key): si no lo tienes, no entras. **401**.
2. Después la blacklist (ip-filter): si estás en ella, no entras. **403**.
3. Después validate-jwt: si tu JWT no es válido o el audience no coincide, no entras. **401**.
4. Después rate-limit: si has pasado demasiadas veces en la ventana, espera. **429** con `Retry-After`.
5. Después quota: si has superado el límite total del periodo, espera más. **429** con `Retry-After` mayor.

Solo si pasas las cinco, llegas al mostrador (el backend). Y APIM además **inyecta cabeceras** al backend para que sepa quién eres ("este es el partner X, viene del país Y, tiene el rol Z") sin que el backend tenga que revalidar nada.

Mantén la imagen: APIM es el control de aduanas; los backends son los mostradores de las líneas aéreas; las policies inbound son las distintas comprobaciones en orden.

---

## 5. Recorrido por el código

### `ApimPolicyEvaluator.Evaluar` — el orden de las policies

La función más importante del submódulo. Recibe un `PolicyContext` (lo que APIM ve de la petición: subscription key, IP, JWT audience, contadores) y un `PolicyConfig` (qué reglas aplicar) y devuelve una `PolicyDecision` con status, razón y `RetryAfter` opcional:

```csharp
public static PolicyDecision Evaluar(PolicyContext ctx, PolicyConfig cfg)
{
    // 1) Subscription key (slide 8) — 401 si falta y es obligatoria.
    if (cfg.SubscriptionRequired && string.IsNullOrWhiteSpace(ctx.SubscriptionKey))
        return new PolicyDecision(401, "Falta Ocp-Apim-Subscription-Key (slide 8).");

    // 2) ip-filter (slide 6) — 403 si la IP está en la blacklist.
    if (cfg.IpBlacklist is { Count: > 0 } &&
        cfg.IpBlacklist.Contains(ctx.Ip, StringComparer.OrdinalIgnoreCase))
        return new PolicyDecision(403, $"IP {ctx.Ip} bloqueada por ip-filter (slide 6).");

    // 3) validate-jwt (slide 5) — 401 si el claim 'aud' no coincide.
    if (!string.IsNullOrWhiteSpace(cfg.RequiredAudience) &&
        !string.Equals(ctx.JwtAudience, cfg.RequiredAudience, StringComparison.Ordinal))
        return new PolicyDecision(401, "validate-jwt: claim 'aud' inválido o ausente (slide 5).");

    // 4) rate-limit-by-key con rama premium (slide 9) — 429.
    bool premium = string.Equals(ctx.UserTier, "premium", StringComparison.OrdinalIgnoreCase);
    int limite = premium ? cfg.RateLimitCallsPremium : cfg.RateLimitCalls;
    if (ctx.LlamadasEnVentana >= limite)
        return new PolicyDecision(429, $"Rate limit superado...", RetryAfter: cfg.RateLimitPeriodSeg);

    // 5) quota-by-key (slide 9) — 429 con Retry-After del período.
    if (ctx.LlamadasEnCuota >= cfg.QuotaCalls)
        return new PolicyDecision(429, $"Quota superada...", RetryAfter: cfg.QuotaPeriodSeg);

    return new PolicyDecision(200, "OK — petición reenviada al backend.");
}
```

El orden es exactamente el de APIM en producción y conviene saberlo de memoria:

1. **Subscription key**: ¿el cliente se ha identificado?
2. **IP filter**: ¿la IP de origen está permitida?
3. **Validate JWT**: ¿el usuario está bien autenticado?
4. **Rate limit**: ¿está dentro de la ventana corta (típicamente segundos)?
5. **Quota**: ¿está dentro del límite acumulado (típicamente día o mes)?

Tres detalles que diferencian gateways bien configurados:

- **El `Retry-After` debe coincidir con la unidad del límite**. Si tu rate-limit es 100 llamadas por 60 segundos, devuelve `Retry-After: 60`. Si la quota es 10.000 por día, devuelve `Retry-After: 86400`. Los clientes razonables lo respetan.
- **La rama premium** en el rate-limit (`RateLimitCallsPremium = 1000`) demuestra el patrón **choose**: una sola policy con dos comportamientos según el tier del usuario. APIM lo implementa con XML `<choose>` y `<when condition>`; el evaluador lo materializa como un `if`.
- **Subscription key + JWT no son mutuamente excluyentes**. Una API seria pide los dos: la subscription key identifica al **consumidor** (qué app llama), el JWT identifica al **usuario** (quién usa esa app). Las dos validaciones son útiles y caen en diferentes 401: la subscription key falla "no te identificas como app"; el JWT falla "no te identificas como usuario".

### `ApimPolicyEvaluator.DebeReintentar` — circuit breaker simple

```csharp
public static bool DebeReintentar(int statusBackend, int intentos, int maxIntentos) =>
    statusBackend >= 500 && intentos < maxIntentos;
```

La regla del slide 18: solo se reintentan errores 5xx (problemas del backend) y solo hasta `maxIntentos`. Los 4xx (errores del cliente) NO se reintentan — la petición es incorrecta, reintentar no va a arreglarla. Esta función pura representa lo que APIM hace en su policy `<retry>`.

### `ApimVersioningResolver` — Segment, Query, Header

Tres esquemas de versionado del slide 7. La función pura los resuelve y los unifica:

```csharp
public static EsquemaVersionado Recomendado => EsquemaVersionado.Segment;

public static VersionResuelta Resolver(
    EsquemaVersionado esquema, string apiPath, string entrada,
    IReadOnlySet<string> versionesValidas)
{
    string version = esquema switch
    {
        EsquemaVersionado.Segment => entrada.Trim('/').Split('/', ...) is [var v, ..] ? v : throw...,
        EsquemaVersionado.Query   => entrada.Trim(),
        EsquemaVersionado.Header  => entrada.Trim(),
        _ => throw new ArgumentOutOfRangeException(nameof(esquema)),
    };

    if (!versionesValidas.Contains(version))
        throw new ArgumentException($"Versión '{version}' no existe en el version set.");

    string ruta = esquema switch
    {
        EsquemaVersionado.Segment => $"/{version}/{apiPath}",
        EsquemaVersionado.Query   => $"/{apiPath}?api-version={version}",
        EsquemaVersionado.Header  => $"/{apiPath} (Api-Version: {version})",
        _ => throw new ArgumentOutOfRangeException(nameof(esquema)),
    };

    return new VersionResuelta(version, ruta);
}
```

Tres opciones, una recomendación firme: **Segment**. Las razones:

- **Visible en logs y trazas**: `/v1/productos` aparece tal cual en cada log de access. Con Query, hay que mirar el query string. Con Header, hay que mirar las cabeceras.
- **Cacheable por CDN sin sorpresas**: Segment es parte de la URL, los CDN cachean por URL completa. Query también, pero algunos CDN normalizan. Header es la peor opción para cache: requiere `Vary: Api-Version`, no todos los CDN lo respetan bien.
- **Más fácil de probar**: copias la URL al portapapeles y la mandas. Con Header, tienes que explicar al cliente que añada un header.

Query y Header tienen su nicho (cuando ya tienes APIs publicadas sin Segment y no quieres romper URLs, por ejemplo), pero para APIs nuevas, Segment.

### `ApimTierAdvisor.RecomendarTier` — el árbol de decisión por coste

Cinco tiers con costes muy distintos:

```csharp
private static string Coste(ApimTier t) => t switch
{
    ApimTier.Consumption => "0 € base · pago por llamada (1M gratis/mes)",
    ApimTier.Developer   => "~40 €/mes (dev/test, sin SLA)",
    ApimTier.Basic       => "~130 €/mes (producción pequeña)",
    ApimTier.Standard    => "~550 €/mes (producción media)",
    ApimTier.Premium     => "~2200 €/mes (enterprise, multi-región, VNet)",
    _ => "n/d",
};
```

El árbol va por prioridad:

1. **VNet, multi-región o self-hosted gateway** → Premium. Son las únicas cosas que solo Premium ofrece.
2. **Producción con > 1000 llamadas/segundo** → Premium. Por capacidad.
3. **Dev/test** → Developer. Features completas sin SLA. **Nunca en producción** (anti-pattern slide 31.1).
4. **Producción "normal"** → Standard. Sin SLA → 99,95%, sin frills.
5. **Resto** (típicamente bajo volumen, prototipos, MVPs) → Consumption. Gratis hasta 1M llamadas al mes.

La regla práctica: **empieza con Consumption**. Es lo que se usa en este curso. Solo subes a Standard cuando tengas un volumen sostenido > 1M/mes y necesites SLA. Subes a Premium solo si te exigen VNet (banca, sanidad), multi-región (HA cross-region) o self-hosted gateway (deploy del gateway en tu propia infra).

### `ApimTierAdvisor.EsBuenCaso` — ¿realmente necesitas APIM?

La pregunta honesta. Cuenta señales a favor y en contra:

A favor: múltiples APIs, rate limit o caching necesarios, expone a terceros, versionado central, analytics de uso.

En contra: una sola API simple, tráfico exclusivamente interno service-to-service, presupuesto que no permite tiers no-Consumption.

Si una API simple es lo único que tienes y el tráfico es service-to-service interno, APIM no aporta. Pones rate limit con AspNet middleware si lo necesitas y vas tirando. Cuando crezcas a más de una API o expongas a terceros, considera APIM en serio.

---

## 6. La conversación con seguridad: "¿APIM o Easy Auth?"

Pregunta que aparece cuando el equipo de seguridad mira la arquitectura. Las dos tecnologías hacen cosas parecidas pero distintas:

| | Easy Auth (S6.P/S6.P2) | APIM |
| --- | --- | --- |
| **Dónde vive** | App Service / Function App | Recurso independiente delante |
| **Para qué** | Autenticar usuarios en una app concreta | Gateway centralizado con muchas funciones |
| **Validación de token** | Sí | Sí (`validate-jwt`) |
| **Rate limit** | No | Sí (`rate-limit-by-key`) |
| **Versionado** | No | Sí (version sets) |
| **Subscription keys** | No | Sí |
| **Caching** | No | Sí |
| **Coste** | Gratis | 0 € (Consumption) a 2200 €/mes (Premium) |

Las dos pueden coexistir: APIM como portero global con rate limiting y subscription keys; Easy Auth en cada App Service como capa adicional de validación (defensa en profundidad). O elegir una: para apps internas con un solo backend, Easy Auth es suficiente; para sistemas con múltiples backends o consumidores externos, APIM aporta más.

Regla práctica:

- **Una App Service, login estándar** → Easy Auth.
- **Múltiples APIs detrás de una sola URL, partners externos, rate limit, versionado** → APIM.
- **Las dos cosas** → APIM + Easy Auth en cada backend (raro, pero defensa en profundidad si tu cliente lo pide).

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Apim.Demo.Api
# http://localhost:5098
```

Endpoints:

```http
### Evaluar una petición contra la política
POST http://localhost:5098/apim/policy
Content-Type: application/json

{
  "ctx": {
    "subscriptionKey": "abc-123",
    "ip": "10.0.0.5",
    "userTier": "estandar",
    "jwtAudience": "api://miapi",
    "llamadasEnVentana": 99,
    "llamadasEnCuota": 5000
  },
  "cfg": {
    "subscriptionRequired": true,
    "requiredAudience": "api://miapi",
    "rateLimitCalls": 100,
    "rateLimitPeriodSeg": 60
  }
}
# → { status: 200, razon: "OK..." }

### Misma petición pero con la ventana agotada
# llamadasEnVentana = 100
# → { status: 429, razon: "Rate limit...", retryAfter: 60 }

### Resolver versión Segment
GET http://localhost:5098/apim/version?esquema=Segment&apiPath=productos&entrada=/v2/productos
# → { version: "v2", rutaGateway: "/v2/productos" }

### Recomendar tier
POST http://localhost:5098/apim/tier
Content-Type: application/json

{ "produccion": true, "requiereVNet": true, "llamadasPorSegundo": 100 }
# → Premium con razones

### Plan completo
POST http://localhost:5098/apim/plan
# → tier + caso + policies + checklist del entregable
```

Los 31 tests cubren cada rama del evaluador (los cinco status posibles: 200, 401×2, 403, 429×2 — uno por rate-limit, otro por quota), los tres esquemas de versionado con casos límite, y el árbol del advisor con cada combinación.

Para validar contra una instancia real:

- **APIM Consumption** desde el portal (gratis hasta 1M llamadas/mes).
- Importa una API (un App Service o Function App tuyo) con OpenAPI.
- Crea un Version Set con esquema Segment.
- Crea un Product con `subscription-required = true`.
- Configura policies inbound: `validate-jwt`, `rate-limit-by-key`, `quota-by-key`, `cors`.

El script `01-verify-apim.sh` inventaría: tier, APIs publicadas, products, subscriptions, métricas básicas. Solo lectura.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Los anti-patterns del slide 31

Cinco errores que conviene tener en mente al configurar APIM:

**Anti-pattern 1 — Developer tier en producción**. Developer está pensado para dev/test y no tiene SLA. Si tu APIM cae, Microsoft no te debe nada. Para producción: Standard o superior.

**Anti-pattern 2 — `subscription-required = false` en producción**. Sin subscription key, cualquiera puede llamar a tu API. Para APIs públicas tiene su sitio (search, status), pero en general activa subscription keys y delega la cuestión "quién llama" a APIM.

**Anti-pattern 3 — Rate limiting global sin distinguir consumidores**. Si pones un rate-limit que cuenta TODAS las llamadas de TODOS los consumidores, un consumidor pesado te tira la API para los demás. Usa `rate-limit-by-key` con la subscription key como key.

**Anti-pattern 4 — Sin alertas de 4xx y 5xx**. Sin alertas, descubres los problemas cuando el cliente llama. Con alertas (4xx > 5% sostenido, 5xx > 0 sostenido, BackendDuration > 2s), ves degradación antes de que el cliente se queje.

**Anti-pattern 5 — Config en el portal sin GitOps**. Si configuras policies a mano en el portal y no las versiona en código (Bicep), el día que algo cambie por error no sabes qué tenías antes. **APIM como código** (Bicep + GitOps): cada cambio es un PR, cada deploy es trazable, cada rollback es `git revert`.

---

## 9. Glosario breve

- **API Gateway**: componente que se pone delante de tus APIs y se ocupa de las preocupaciones transversales (auth, rate limit, caching, versionado).
- **Subscription key**: token que identifica al consumidor (la app que llama). Se envía en el header `Ocp-Apim-Subscription-Key`.
- **Product**: agrupación de APIs con políticas comunes (rate limit, quota). Los consumidores se suscriben a products, no a APIs directamente.
- **Version set**: configuración del esquema de versionado de una API en APIM.
- **Policy inbound**: regla que se evalúa al recibir la petición, antes de reenviarla al backend.
- **Policy outbound**: regla que se evalúa al recibir la respuesta del backend, antes de devolverla al cliente.
- **Self-hosted gateway**: opción de Premium para desplegar el gateway de APIM en tu propia infra (on-prem, otro cloud).
- **Developer portal**: portal autogenerado por APIM donde tus consumidores ven la documentación de las APIs y se suscriben a products.
- **`validate-jwt`**: policy que verifica un JWT (firma, audience, issuer, expiración).
- **`rate-limit-by-key`**: policy que limita llamadas por unidad de tiempo (típicamente segundos).
- **`quota-by-key`**: policy que limita llamadas en un período largo (típicamente día o mes).
- **`set-backend-service`**: policy que reescribe la URL del backend (útil para A/B, blue/green, canary).

---

## 10. Cierre

APIM convierte un sistema con varias APIs y consumidores en algo gobernable: una URL de entrada, políticas centralizadas, versionado declarativo, developer portal automático, métricas integradas. El precio: configurar policies correctamente (el orden importa), elegir el tier apropiado (la diferencia entre Consumption y Premium es enorme), y meter la configuración bajo control de versiones (Bicep + GitOps). Con esas tres cosas claras, APIM se convierte en la pieza más útil de un sistema con varias APIs.

Lo siguiente es [`S7.4 — ClickOnce vs MSIX`](../S7.4-clickonce-vs-msix/MANUAL.md), donde el módulo se mueve del backend al desktop: cómo distribuir aplicaciones de escritorio Windows de forma moderna, qué hereda MSIX de ClickOnce y dónde gana cada uno.
