# Manual del alumno — S6.P2 · Práctica Easy Auth

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue por Portal, scripts. Este manual va antes: te cuenta por qué Easy Auth es la pieza con mejor relación esfuerzo/protección de Azure, en qué se diferencia de la práctica anterior (S6.P) y por qué un sitio web protegido con Easy Auth no tiene ni una línea de auth en su código.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M06-S6.P2](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P2-practica-easy-auth-v1.md). Tres piezas de lógica pura (selector de comportamiento web vs API, generador de rutas `/.auth/*`, lector de cabeceras) más la app protegida que se prueba E2E.

*Creado: 2026-05-20 18:55 +0200*

---

## 1. La idea en una frase

Easy Auth es **middleware de App Service que vive antes de tu código** y se ocupa de toda la autenticación: validación de tokens, redirect al login, gestión del token store, endpoints integrados `/.auth/me`, `/.auth/login/...`, `/.auth/logout`. Tú activas un toggle en el portal, eliges el identity provider, y tu app se vuelve segura sin que escribas una línea relacionada con auth. Esta práctica cierra el módulo M06 dando una variante sobre la S6.P: en lugar de una API protegida (que devuelve 401), aquí es un **sitio web** que **redirige al login con 302** cuando llega un usuario sin sesión.

---

## 2. El problema real que hay detrás

La pregunta de fondo: si Easy Auth hace todo, ¿por qué hay tanto código de Microsoft.Identity.Web en proyectos del mundo real?

La respuesta honesta: porque a veces el equipo no sabe que Easy Auth existe, o lo descartó por una mala razón. Tres mitos comunes:

**Mito 1 — "Easy Auth es solo para apps simples".** Falso. Easy Auth cubre **el 95% de los escenarios** corporativos: webs internas, dashboards, herramientas administrativas, APIs protegidas por token de Entra. Lo único que NO cubre bien son los casos con OBO (encadenamiento de APIs en nombre del usuario) o autorización fina por App Roles dentro de la app. Para el resto, gana.

**Mito 2 — "Easy Auth me ata a App Service".** Cierto pero matizado. Easy Auth solo existe en App Service y Function App. Si te mueves a contenedores en AKS o a una VM, hay que reescribir esa parte. Pero si tienes la app en App Service, no usarlo es trabajo extra sin valor.

**Mito 3 — "Es mejor tener el código de auth bajo control".** Esta es la más peligrosa. La gente cree que "controlar el código de auth" es bueno. En realidad significa **ser responsable de bugs de seguridad** que la librería de Microsoft no tendría. La validación de tokens es trabajo de gente especializada, no de la mayoría de equipos de aplicación. Delegarlo a Easy Auth o a Microsoft.Identity.Web es la decisión profesionalmente responsable.

La práctica desmonta los tres mitos al hacer que el alumno **construya una app protegida en cinco clicks**. Es la mejor forma de aprender que sí, es así de simple.

---

## 3. Por qué esto importa en tu stack

Para cualquier app interna corporativa que vivas en App Service, Easy Auth es probablemente la respuesta correcta. Tres preguntas:

- **¿La app autentica usuarios humanos o procesos automatizados?** Humanos → Easy Auth con `Redirect302` en una app web; o `Return401` en una API. Procesos → Managed Identity (no Easy Auth).
- **¿Hay autorización fina dentro de la app por roles de aplicación?** Si solo autorizas a "los que tienen sesión", Easy Auth basta. Si necesitas distinguir Admin/Customer/Auditor con `[Authorize(Roles = ...)]`, plantea Microsoft.Identity.Web.
- **¿Hay encadenamiento de llamadas a otras APIs en nombre del usuario?** Si la app solo consume sus propios datos, Easy Auth basta. Si tienes que llamar a Graph o a una API tercera en nombre del usuario actual, necesitas OBO → Microsoft.Identity.Web.

Con esas tres preguntas eliminas el debate. La respuesta más común en proyectos de empresa es "Easy Auth basta", y la práctica es la forma de comprobarlo.

---

## 4. La analogía vertebradora: el conserje del edificio que ya hace todo

Imagina que alquilas una oficina en un edificio corporativo. Antes de mudarte, el dueño te ofrece dos opciones:

**Opción A — Oficina sin conserjería**:

- Tú instalas tu propia recepción.
- Compras tu propio sistema de control de accesos.
- Contratas a un guardia de seguridad.
- Mantienes la base de datos de empleados acreditados.
- Cuando hay cambios en la legislación de seguridad laboral, te informas y adaptas tu sistema.

**Opción B — Oficina con conserjería del edificio incluida**:

- El conserje de la entrada del edificio gestiona el acceso.
- Mira el carnet de empleado de cualquier visitante.
- Lo verifica contra el sistema central de Recursos Humanos.
- Solo deja pasar a quien tiene autorización para tu planta.
- A los visitantes autenticados les pone una pegatina con su nombre que llevas en la solapa.
- Si cambian las normativas, el dueño del edificio actualiza el sistema sin que tú te enteres.

Tu oficina, en la opción B, recibe a las personas con la pegatina puesta. Lees el nombre y haces tu trabajo. **Tu oficina no tiene mostrador, no tiene control de accesos, no necesita guardia**. Si llega alguien sin pegatina, el conserje no lo deja subir; tu oficina ni se entera.

Eso es Easy Auth respecto a tu app. Microsoft (el dueño del edificio) mantiene el conserje, las actualizaciones de seguridad llegan transparentes, y tu app solo lee la pegatina (las cabeceras `X-MS-CLIENT-PRINCIPAL-*`) para saber quién entra. La diferencia respecto a S6.P es el comportamiento ante visitantes sin pegatina:

- **Sitio web** (S6.P2, esta práctica): el conserje le indica al visitante "vaya a la entrada principal, regístrese ahí, y vuelva con la pegatina puesta" — redirige al login con HTTP 302.
- **API** (S6.P, anterior): el conserje le da al visitante un sobre estandarizado con "401: no estás autorizado, intenta de nuevo con tu carnet" — devuelve HTTP 401 sin redirigir.

Es el mismo conserje haciendo dos cosas distintas según el tipo de oficina. Mantén la imagen.

---

## 5. Recorrido por el código

### `EasyAuthConfigAdvisor` — web vs API

La función central decide el comportamiento ante usuarios no autenticados:

```csharp
public static string AccionNoAutenticado(TipoApp tipo) => tipo switch
{
    TipoApp.SitioWeb => "RedirectToLoginPage",   // HTTP 302
    TipoApp.Api      => "Return401",              // HTTP 401
    _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
};

public static int CodigoHttp(TipoApp tipo) =>
    tipo == TipoApp.SitioWeb ? 302 : 401;
```

Para un sitio web: el usuario llega con su navegador, no tiene sesión, Easy Auth lo redirige (HTTP 302) a `login.microsoftonline.com`. El navegador sigue el redirect, el usuario se autentica, vuelve a tu app con la sesión establecida. Es la experiencia natural para una persona.

Para una API: el cliente llega con `curl` o desde una SPA, sin token, Easy Auth devuelve HTTP 401. El cliente (programado) reacciona: muestra error, ejecuta su propio flujo de login, retry. **No tendría sentido devolver 302 a una API** — el cliente programático no sigue redirects al login HTML.

La práctica es **sitio web**, por eso `Redirect302`. La S6.P era API, por eso `Return401`. Es exactamente la misma infraestructura cambiando un toggle.

La constante:

```csharp
public const bool TokenStorePorDefecto = true;
```

Easy Auth tiene un "token store" que guarda los tokens del usuario (incluido el `refresh_token`) en su propio almacén cifrado. Por defecto está habilitado. Te permite, desde tu código, **acceder al access_token del usuario** para llamar a otras APIs (por ejemplo Microsoft Graph) sin tener que pedirle al usuario login otra vez. Si lo desactivas, solo tienes las cabeceras y nada más.

La lista de proveedores soportados:

```csharp
public static IReadOnlyList<Proveedor> Proveedores { get; } =
    [.. Enum.GetValues<Proveedor>()];

// MicrosoftEntra, Google, Facebook, X, GitHub, Apple
```

Seis identity providers integrados. Cambiar de uno a otro es cambiar la configuración en el portal — el código de tu app no se entera. Si decides ofrecer "login con Google" además de "login con Entra", configuras ambos en Easy Auth y los dos quedan disponibles en `/.auth/login/google` y `/.auth/login/aad`.

### `AuthEndpoints` — las rutas integradas

Easy Auth expone tres familias de URLs en tu dominio que no las gestiona tu app, las gestiona el middleware:

```csharp
public const string Prefijo = "/.auth/";

public const string Me = "/.auth/me";
// Devuelve los claims del usuario actual como JSON

public static string Login(string proveedor = "aad", string? postLoginRedirect = null) =>
    /.auth/login/{proveedor}[?post_login_redirect_url=...]

public static string LoginCallback(string proveedor = "aad") =>
    /.auth/login/{proveedor}/callback

public static string Logout(string? postLogoutRedirect = null) =>
    /.auth/logout[?post_logout_redirect_uri=...]
```

Cuatro rutas que valen oro:

- **`/.auth/me`**: devuelve un JSON con los claims del usuario actual. Útil para que el frontend pinte "Hola, Pedro" sin tener que parsear el JWT.
- **`/.auth/login/aad`**: inicia el flujo de login. Si añades `?post_login_redirect_url=/dashboard`, te lleva ahí tras autenticar.
- **`/.auth/login/aad/callback`**: el callback que Entra ID llama tras el login. Lo gestiona Easy Auth, no tu app.
- **`/.auth/logout`**: cierra la sesión. Con `?post_logout_redirect_uri=/bye` te lleva a esa página tras cerrar.

El método `EsRutaEasyAuth(path)` te permite filtrar: si una ruta empieza por `/.auth/`, la gestiona Easy Auth, **no la mires en tu código**. Es la línea divisoria entre "lo que es tuyo" y "lo que es de Microsoft".

### `EasyAuthHeaders` — leer la chapa del usuario

Igual que en S6.P pero con un campo más:

```csharp
public const string Nombre = "X-MS-CLIENT-PRINCIPAL-NAME";
public const string Id     = "X-MS-CLIENT-PRINCIPAL-ID";        // ← nuevo aquí
public const string Idp    = "X-MS-CLIENT-PRINCIPAL-IDP";

public static PrincipalEasyAuth Desde(IReadOnlyDictionary<string, string?> headers)
{
    headers.TryGetValue(Nombre, out var nombre);
    headers.TryGetValue(Id, out var id);
    headers.TryGetValue(Idp, out var idp);

    var autenticado = !string.IsNullOrWhiteSpace(nombre);
    return new PrincipalEasyAuth(
        autenticado,
        autenticado ? nombre : null,
        autenticado ? id : null,                               // ← Object ID del usuario en Entra
        autenticado ? (string.IsNullOrWhiteSpace(idp) ? "aad" : idp) : null);
}
```

`X-MS-CLIENT-PRINCIPAL-ID` lleva el **Object ID** del usuario en Entra ID — un GUID estable que no cambia aunque el usuario cambie de email. Es la clave correcta para identificar al usuario en tu BD; el `Name` (que suele ser email) puede cambiar.

Para una app de empresa, la regla operativa: **almacena el `Id` en tu BD para asociar registros al usuario; muestra el `Name` en la UI; usa el `Idp` para distinguir proveedores si tienes varios**.

### `EasyAuthSetupPlanner` — el checklist del entregable

El servicio inyectable que orienta sobre la configuración completa: tipo de app, proveedor, acción no-autenticado, rutas a usar, y checklist de validación post-deploy. Lo usa el script `01-verify-easyauth.sh` para certificar el entregable.

---

## 6. Diferencia con S6.P: web vs API

La práctica anterior (S6.P) y esta (S6.P2) son **la misma idea** —proteger algo con Easy Auth— con dos comportamientos distintos:

| | S6.P (API) | S6.P2 (Web) |
| --- | --- | --- |
| **Tipo de app** | API REST | Sitio web |
| **Cliente típico** | Postman, SPA, otra API | Navegador |
| **Acción Easy Auth** | `Return401` | `RedirectToLoginPage` |
| **Código HTTP sin sesión** | 401 | 302 |
| **Test E2E clave** | `/api/perfil` 401 sin token | `/` 302 → URL del login |
| **Cabeceras inyectadas** | Las mismas (`X-MS-CLIENT-PRINCIPAL-*`) | Las mismas |
| **Token store recomendado** | Solo si llamas a otras APIs | Sí (para `/.auth/me` y refresh) |

Y un detalle pedagógico del test E2E que vale la pena nombrar:

```csharp
var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder => { ... });

var client = factory.CreateClient(new WebApplicationFactoryClientOptions
{
    AllowAutoRedirect = false   // ← clave: no seguir el 302
});

var response = await client.GetAsync("/");
Assert.Equal(HttpStatusCode.Found, response.StatusCode);   // 302
Assert.Contains("login.microsoftonline.com", response.Headers.Location!.ToString());
```

El test desactiva el auto-redirect del cliente HTTP para poder afirmar que el primer response es 302 con `Location: https://login.microsoftonline.com/...`. Si dejas el auto-redirect, el cliente seguiría el 302, intentaría conectarse a Microsoft, y el test se rompería por motivos ajenos. Es un detalle de carpintería de testing pero útil de tener apuntado.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/EasyAuth.Demo.Api
# http://localhost:5095
```

Requests:

```http
### /health público
GET http://localhost:5095/health
# → 200

### / sin cabeceras Easy Auth → 302 al login
GET http://localhost:5095/
# Si AllowAutoRedirect = false:
# 302 con Location: https://login.microsoftonline.com/...

### / con cabeceras (Easy Auth nos autenticó)
GET http://localhost:5095/
X-MS-CLIENT-PRINCIPAL-NAME: pedro@empresa.com
X-MS-CLIENT-PRINCIPAL-ID: 12345-...-67890
X-MS-CLIENT-PRINCIPAL-IDP: aad
# → 200 con HTML mostrando el nombre

### /.auth/me sin sesión devuelve []
GET http://localhost:5095/.auth/me
# → 200 con []

### /.auth/me con sesión devuelve los claims
GET http://localhost:5095/.auth/me
X-MS-CLIENT-PRINCIPAL-NAME: pedro@empresa.com
# → 200 con [{ name, idp, ... }]
```

Los 16 tests cubren todas las combinaciones: las decisiones del advisor, la construcción de rutas con encoding correcto, las cabeceras con casos límite (campo ausente, idp vacío, todo vacío), y la suite E2E con `WebApplicationFactory`.

Para el despliegue real:

1. Resource Group + App Service Plan F1 (gratis) + Web App.
2. Deploy del código (sin tocar nada en `Program.cs`).
3. Portal → App Service → Authentication → Add identity provider → Microsoft.
4. En el wizard: "Create new app registration" (Easy Auth la crea por ti); "Restrict access" = Require; "Unauthenticated requests" = HTTP 302.
5. Save. En 30 segundos, navega a la URL de tu Web App: te redirige al login de Entra; tras login vuelves a tu app con la sesión activa.

Verifica con `scripts/01-verify-easyauth.sh`:
- Easy Auth habilitado.
- Acción no-auth = `RedirectToLoginPage`.
- `/.auth/me` responde.

Tras la práctica, borra el Resource Group. Coste 0 € (F1 gratis, Easy Auth gratis, Entra gratis).

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La métrica clave: ¿cuánto código de auth hay en tu repo?

Para certificar que la práctica está bien hecha, abre tu repo y busca:

```bash
# Ninguna de estas búsquedas debe devolver nada de auth en tu código
grep -r "AddAuthentication"     src/
grep -r "AddAuthorization"      src/
grep -r "Microsoft.Identity"    src/
grep -r "[Authorize]"           src/
grep -r "JwtBearer"             src/
```

Si las cinco búsquedas vienen vacías —o solo aparecen en tests, no en `src/`—, tu app está usando Easy Auth como debe. **Cero código de auth** es la métrica.

Lo único que tu código sí hace, y vale la pena verlo:

```bash
grep -r "X-MS-CLIENT-PRINCIPAL" src/
# Debe aparecer en los puntos donde lees el principal
```

Esa es la API contractual entre Easy Auth (que vive antes que tu código) y tu app (que vive después). Es minimal, estable, y portable: si mañana Easy Auth cambia internamente, las cabeceras siguen siendo las mismas y tu código no se entera.

---

## 9. Cuándo NO usar Easy Auth

Para que la conversación esté completa, los casos donde Easy Auth no es la respuesta correcta:

- **Necesitas OBO**: tu app llama a Microsoft Graph o a una API tuya en nombre del usuario actual, y necesitas un token con esa audience. Easy Auth te puede dar el access_token del usuario (vía token store), pero el OBO con tokens delegados es trabajo de Microsoft.Identity.Web.
- **App Roles con autorización fina**: si necesitas `[Authorize(Roles = "Admin")]` en endpoints concretos y los roles vienen del claim `roles` del token, lo más limpio es leer el JWT con Microsoft.Identity.Web. Easy Auth te da las cabeceras pero el claim roles tienes que sacarlo del `X-MS-CLIENT-PRINCIPAL` JSON, lo cual es menos cómodo.
- **Multi-tenancy avanzado**: si tu app sirve a varios tenants y necesitas lógica específica por tenant (issuer dinámico, claims map distinto), Easy Auth se queda corto.
- **Despliegues fuera de App Service / Function**: AKS sin sidecars de auth, contenedores en otros sitios, VMs. Easy Auth solo existe en App Service.

Para todo lo demás —el 95% de las apps de empresa— Easy Auth es la respuesta correcta.

---

## 10. Glosario breve

- **Easy Auth** (App Service Authentication): middleware integrado en App Service y Function App que se ocupa de validar tokens y gestionar sesión, sin código en tu app.
- **`/.auth/*`**: prefijo de rutas que Easy Auth expone automáticamente. Tu código no las maneja.
- **`/.auth/me`**: endpoint que devuelve los claims del usuario actual como JSON.
- **`/.auth/login/{provider}`**: endpoint para iniciar el flujo de login con un provider concreto.
- **`/.auth/logout`**: endpoint para cerrar la sesión.
- **Token store**: almacén cifrado donde Easy Auth guarda los tokens del usuario (access, id, refresh) y los pone a disposición de tu app vía API integrada.
- **`X-MS-CLIENT-PRINCIPAL-NAME`**: cabecera con el nombre del usuario (típicamente email).
- **`X-MS-CLIENT-PRINCIPAL-ID`**: cabecera con el Object ID del usuario en Entra (GUID estable).
- **`X-MS-CLIENT-PRINCIPAL-IDP`**: cabecera con el identity provider (`aad`, `google`, etcétera).
- **`X-MS-CLIENT-PRINCIPAL`**: cabecera con JSON base64 que lleva todos los claims (incluido `roles`).
- **`RedirectToLoginPage`**: comportamiento de Easy Auth para sitios web — devuelve 302 al login cuando llega request sin sesión.
- **`Return401`**: comportamiento de Easy Auth para APIs — devuelve 401 cuando llega request sin token.

---

## 11. Cierre del módulo M06

Con S6.P2 completas el módulo de seguridad. Lo que has visto:

- **S6.1** — Modelo de responsabilidad compartida, STRIDE, secret scanning, Secure Score. Las cuatro tablas mentales.
- **S6.2** — Microsoft Entra ID: tipos de identidad, sistemas de roles, anatomía del JWT, App Roles.
- **S6.3** — OAuth2/OIDC: los seis flujos, PKCE, parámetros de `/authorize`.
- **S6.4** — Auth desktop y MSIX: WAM, system browser, broker plugin URI, ciclo del token.
- **S6.5** — Seguridad de datos: cifrado at-rest e in-transit, CMK, Always Encrypted, CORS.
- **S6.6** — Azure Key Vault: secretos, keys, certificates, references, rotación.
- **S6.P** — Práctica integradora: OAuth2 + Key Vault en una API.
- **S6.P2** — Práctica final: Easy Auth en un sitio web.

Si te quedas con una sola cosa de todo el módulo, que sea esta: **las decisiones de seguridad son tres o cuatro tablas que tienes que tener claras desde el día uno; los productos de Azure (Entra ID, Key Vault, Easy Auth) implementan las decisiones; tu código consume el resultado sin reinventar nada**. Esa es la arquitectura sana.

El siguiente módulo es **M07 — Integración y MSIX**, donde el foco vuelve al desktop con empaquetado moderno (lo que has visto desde la óptica de auth en S6.4).
