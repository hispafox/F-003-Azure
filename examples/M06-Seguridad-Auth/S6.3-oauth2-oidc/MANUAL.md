# Manual del alumno — S6.3 · OAuth2 y OpenID Connect

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta qué se reparten OAuth2 y OIDC, qué flujo elegir según el tipo de cliente, por qué PKCE no es opcional para SPAs y móviles, y por qué tu código nunca debe implementar OAuth a mano aunque "parezca trivial".

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.3](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.3-oauth2-openid-connect-v3.md). Tres piezas de lógica pura (selector de flujo, generador PKCE, constructor de URL de autorización) más un planificador de login que las une.

*Creado: 2026-05-20 16:55 +0200*

---

## 1. La idea en una frase

OAuth2 es un protocolo de **autorización**: el usuario consiente delegar acceso a un cliente sobre un recurso protegido. OpenID Connect (OIDC) añade encima una capa de **autenticación**: además de autorizar, el cliente recibe pruebas firmadas de quién es el usuario. En Azure los dos protocolos se hablan contra Entra ID y son la base de cualquier login moderno: SPA, móvil, web tradicional, daemon, CLI o API que llama a otra API.

El submódulo no enseña a implementar el protocolo (eso lo hace `Microsoft.Identity.Web` por ti, y mejor que tú). Enseña a **decidir qué flujo usar**, a **construir los parámetros correctos** y a **generar el PKCE bien**. Esas tres decisiones son lógica pura y se prueban al milisegundo.

---

## 2. El problema real que hay detrás

Tres incidentes que demuestran por qué la decisión "qué flujo OAuth uso" no es una cuestión secundaria:

**Caso 1 — la SPA con flujo Implicit.** Un equipo arrancó una SPA en 2020 siguiendo un tutorial antiguo que recomendaba el flujo Implicit (el access token se devuelve directamente en el fragmento de la URL del callback). Funcionaba, pero tenía un agujero conocido: cualquier extensión maliciosa del navegador, cualquier referrer-header indebidamente expuesto, cualquier log que registrara URLs completas, exponía el access token. Cuando Microsoft anunció la deprecación oficial de Implicit y el equipo intentó migrar a Authorization Code + PKCE, tuvieron que reescribir todo el flujo de autenticación porque la SPA asumía estructuralmente que el token venía en el fragment de la URL. Coste: tres semanas de un developer senior.

**Caso 2 — el daemon que pedía login interactivo.** Un servicio batch que tenía que correr de madrugada estaba configurado para pedir "login con un usuario". El primer mes pasaba el batch porque alguien dejaba un token de refresh válido. A los 90 días, el token expiró, nadie estaba a esa hora, y los datos no se procesaron durante tres días hasta que alguien lo notó. La solución: cambiar a flujo **Client Credentials** —el daemon se autentica como sí mismo, no como un usuario— y dejar de tener un humano en el ciclo de un proceso automatizado. No es un detalle teórico: es lo que distingue un sistema robusto de uno frágil.

**Caso 3 — el `code_verifier` recodificado.** Un developer implementó PKCE a mano para una app móvil. Generaba el code_verifier como string aleatorio, pero al calcular el code_challenge hacía `SHA256(verifier).Base64String()` en lugar de `BASE64URL(SHA256(ASCII(verifier)))`. Funcionaba en su máquina porque el código no tenía caracteres especiales, pero en algunas ejecuciones el `+` o `/` del base64 normal hacían que el match contra el verifier fallara en el servidor. Bug intermitente al login, imposible de reproducir en local. Tres días de debugging para descubrir que el formato base64url no era opcional. **Lección clara**: las RFCs de OAuth tienen detalles que cuestan tiempo descubrir cuando los implementas tú.

Los tres casos resuelven mejor: usar el flujo correcto (Authorization Code + PKCE para SPA, Client Credentials para daemon), generar el PKCE según la RFC 7636 (incluyendo el vector de prueba), y dejar la implementación del protocolo a una librería seria como Microsoft.Identity.Web.

---

## 3. Por qué esto importa en tu stack

Cualquier sistema con login pasa por OAuth2/OIDC en Azure. Tres preguntas que vas a tener que responder pronto:

- **¿Qué flujo uso?** Hay seis principales y solo uno por tipo de cliente. La elección equivocada te lleva al primer escenario (la SPA con Implicit) o al segundo (el daemon con auth interactiva).
- **¿Necesita mi cliente guardar un secreto?** Si la respuesta es no (porque es público —SPA, móvil, CLI— o porque es Managed Identity), no metas secret en su config. Si la respuesta es sí (web tradicional, daemon, OBO), guárdalo en Key Vault o en federación OIDC, nunca en código.
- **¿Cómo configuro la App Registration?** Los redirect URIs, los scopes, qué flujos están habilitados — son la diferencia entre que el flujo funcione o no. El script `01-oauth-config.sh` te enseña a auditar esto.

Si tienes claras estas tres preguntas y la respuesta a cada una, estás a 90% del camino. El 10% restante es dejar que `Microsoft.Identity.Web` se ocupe del protocolo.

---

## 4. La analogía vertebradora: el portero del edificio y la entrega del paquete

Imagina un edificio donde quieres acceder a una sala restringida. Hay dos figuras involucradas:

- **El portero** comprueba quién eres. Te pide tu DNI, lo verifica contra un registro, y si todo cuadra te entrega un **pase laminado** que dice quién eres, tu nombre, tu foto, y la fecha de caducidad. Este pase es el **id_token** de OpenID Connect: prueba tu identidad.
- **El responsable de la sala** comprueba qué puedes hacer. Mira tu pase laminado y, si tu rol está en la lista de permitidos, te entrega además una **tarjeta perforada** que abre la puerta de esa sala por un tiempo limitado. La tarjeta perforada es el **access_token** de OAuth2: te autoriza a acceder.

Y luego está la mecánica de obtener esos pases. Distintos tipos de visitantes la hacen distinta:

- **Un visitante adulto en persona** llega al edificio, le piden DNI, le dan el pase y la tarjeta. Es el flujo **Authorization Code** clásico, para una web tradicional con backend.
- **Un visitante que llega con una app de mensajería** (SPA, móvil) recibe primero un papelito con un **código de verificación** (el `code`) y por debajo se intercambia ese código por la tarjeta. Por el camino se enseña una **prueba criptográfica** (el code_verifier de PKCE) para que nadie pueda robar el código y usarlo en su lugar. Es **Authorization Code + PKCE**.
- **Un proveedor de servicios** (un robot del catering, una empresa de limpieza nocturna) no es una persona — no le piden DNI. Le piden la **credencial corporativa de su empresa** y le dan acceso directo. Es **Client Credentials**: no hay usuario humano detrás.
- **Un visitante que llega con un televisor de la sala de espera** y quiere autenticarse pero no tiene teclado: el portero le dice "ve a una pantalla con teclado en la otra puerta y escribe este código de seis dígitos en la web mostrada en pantalla". Es **Device Code**, para CLIs y dispositivos sin entrada cómoda.
- **Una API interna** que necesita actuar en nombre de un usuario que ya se autenticó en una API anterior: ya tiene su tarjeta perforada pero quiere convertirla en una válida para otra sala. Es **On-Behalf-Of** (OBO), para cadenas de APIs.

La regla operativa: **un tipo de visitante = un flujo**. No se mezcla, no se inventa. Y existen dos pases viejos que ya no se usan y nadie debería pedir:

- El **pase con el access token escrito en la parte de atrás del DNI** (Implicit) — cualquiera que viera el DNI lo robaba.
- El **pase que se daba con la contraseña dicha al portero** (ROPC) — el portero acababa sabiendo todas las contraseñas, y el portero no debe saber contraseñas.

Mantén la imagen mientras lees el código.

---

## 5. Recorrido por el código

### `OAuthFlowAdvisor` — un tipo de cliente, un flujo

La función principal es trivial pero importante:

```csharp
public static OAuthFlow Recomendar(TipoCliente cliente) => cliente switch
{
    TipoCliente.Spa or TipoCliente.Movil => OAuthFlow.AuthorizationCodePkce,
    TipoCliente.WebAppServidor           => OAuthFlow.AuthorizationCode,
    TipoCliente.DaemonOServicio          => OAuthFlow.ClientCredentials,
    TipoCliente.Cli                      => OAuthFlow.DeviceCode,
    TipoCliente.ApiLlamaApi              => OAuthFlow.OnBehalfOf,
    _ => throw new ArgumentOutOfRangeException(nameof(cliente)),
};
```

Cinco mapeos, una sola decisión por tipo. Y dos propiedades derivadas que te ahorran pensar:

```csharp
public static bool TieneUsuario(OAuthFlow f) => f != OAuthFlow.ClientCredentials;

public static bool NecesitaSecreto(OAuthFlow f) => f is
    OAuthFlow.AuthorizationCode or
    OAuthFlow.ClientCredentials or
    OAuthFlow.OnBehalfOf;
```

- `TieneUsuario`: ¿hay un humano interactuando? Todos los flujos menos Client Credentials lo tienen. Si tu daemon batch necesita "usuario", estás usando el flujo equivocado.
- `NecesitaSecreto`: ¿el cliente necesita guardar un secret? Solo los confidenciales — web tradicional, daemon, OBO. SPAs y móviles son **clientes públicos** (no pueden guardar secreto fiablemente — están en el dispositivo del usuario) y por eso usan PKCE en su lugar.

Y el método que protege del pasado:

```csharp
public static bool EstaDeprecado(string flujo) =>
    flujo.Equals("Implicit", StringComparison.OrdinalIgnoreCase)
    || flujo.Equals("ROPC", StringComparison.OrdinalIgnoreCase);
```

Si alguien en una review propone usar Implicit o ROPC, este método devuelve `true` y la conversación se termina ahí. Ambos están oficialmente deprecados por Microsoft; las apps nuevas no deben usarlos. Implicit por el problema del token en el fragment; ROPC porque le entregas la contraseña del usuario directamente a la app, lo cual rompe el principio fundamental de OAuth.

### `PkceGenerator` — el detalle de la RFC

PKCE (Proof Key for Code Exchange, RFC 7636) es el mecanismo que protege el flujo Authorization Code cuando el cliente es público. La idea: el cliente genera un secret aleatorio (`code_verifier`) y manda su hash (`code_challenge`) en la URL de `/authorize`. Cuando intercambia el `code` por el token en `/token`, manda también el verifier original. El servidor recalcula el hash y comprueba que coincide. Si alguien robó el `code` por el camino, no podrá usarlo sin el verifier.

La implementación es delicada porque el detalle del encoding importa:

```csharp
public static string GenerarVerifier(int bytes = 32)
{
    if (bytes is < 32 or > 96) throw new ArgumentOutOfRangeException(nameof(bytes));
    return Base64Url(RandomNumberGenerator.GetBytes(bytes));
}

public static string Challenge(string verifier)
{
    if (verifier.Length is < 43 or > 128)
        throw new ArgumentException("code_verifier debe tener 43-128 chars (RFC 7636)");
    var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
    return Base64Url(hash);
}

private static string Base64Url(byte[] data) =>
    Convert.ToBase64String(data)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
```

Tres detalles que las implementaciones a mano olvidan:

- **Base64URL, no Base64 normal**. `+` se sustituye por `-`, `/` por `_`, y se quita el `=` final. Si usas Base64 normal, los caracteres especiales rompen URLs y comparaciones.
- **ASCII, no UTF-8**, para los bytes del verifier antes de hashear. La RFC lo especifica así. Es trivial pero "obvio" hacerlo mal.
- **El verifier en sí debe tener entre 43 y 128 caracteres**. Y solo los del set unreserved (`[A-Za-z0-9-._~]`). Si tu generador produce algo más corto, Microsoft lo rechaza con un error críptico.

El test del ejemplo usa **el vector de prueba del anexo B de la RFC 7636** para validar que la implementación es correcta a la letra. Ese vector es la "piedra de Rosetta" del PKCE: si tu generador produce ese challenge desde ese verifier, está bien; si no, hay un detalle que cambiar.

### `AuthorizeUrlBuilder` — los parámetros que importan

La URL de `/authorize` lleva nueve parámetros que cada uno tiene su razón:

```csharp
("client_id",            r.ClientId),
("response_type",        "code"),                      // siempre "code" en AuthCode+PKCE
("redirect_uri",         r.RedirectUri),               // debe coincidir EXACTAMENTE con el de la App Reg
("response_mode",        "query"),                     // que el code venga en query, no en fragment
("scope",                string.Join(' ', scopes)),    // qué permisos pido
("state",                r.State),                     // protección anti-CSRF (slide 6)
("nonce",                r.Nonce),                     // protección anti-replay del id_token (OIDC)
("code_challenge",       r.CodeChallenge),             // PKCE
("code_challenge_method", PkceGenerator.Method),       // siempre "S256"
```

Y una sutileza importante: el builder **fuerza el scope `openid`** si no está presente:

```csharp
if (!r.Scopes.Contains("openid")) scopes.Add("openid");
```

¿Por qué? Porque sin `openid` recibes solo un access_token (OAuth2 puro), no un id_token (OIDC). Si quieres saber quién es el usuario, necesitas el id_token, y ese solo se emite con scope `openid`. Olvidarse del `openid` es uno de los errores más comunes en las primeras integraciones; el builder lo previene.

### `LoginPlanner` — la pieza que une todo

`ILoginPlanner` es el servicio inyectable que genera un "plan de login" completo: el flujo recomendado, los parámetros PKCE (si aplica), la URL de authorize (si aplica), y notas sobre los pasos siguientes. La razón de existir es la testabilidad: cuando otro servicio dependa del planificador, lo mockeas; cuando el planificador se prueba a sí mismo, recibe `IDateTimeProvider` o entradas controladas y verifica las salidas.

Para un SPA, el plan incluye URL de authorize + PKCE; para un daemon, no incluye URL (porque el flujo Client Credentials se hace contra `/token` directamente, no contra `/authorize`).

---

## 6. La pregunta más importante: ¿cliente público o confidencial?

Cada cliente OAuth2 cae en una de dos categorías, y la categoría determina casi todo lo demás:

**Cliente confidencial**: puede guardar un secret con seguridad. Típicamente porque corre en un servidor que controlas (backend de una web tradicional, daemon, API). El secret se guarda en Key Vault o como variable de entorno protegida. Flujos: Authorization Code (sin PKCE), Client Credentials, On-Behalf-Of.

**Cliente público**: no puede guardar un secret. El cliente se distribuye al usuario final (SPA en el navegador, app móvil instalada, CLI ejecutado en su máquina). Cualquier "secret" embebido se puede extraer con reverse engineering en minutos. Flujos: Authorization Code + PKCE, Device Code.

¿Por qué importa? Porque cambia drásticamente la configuración en Entra:

- Cliente confidencial: en la App Registration marcas "Web" como tipo de plataforma, configuras el client secret o certificado, y los flujos que estén habilitados pueden usar ese secret.
- Cliente público: en la App Registration marcas "Single-page application" o "Mobile and desktop applications". **No configures client secret aunque te lo deje**. Y habilita explícitamente "Allow public client flows" si vas a usar Device Code.

Confundir esto te lleva a un sitio peligroso: una SPA que tenía un client secret embebido, descubierto por un investigador, rotado de urgencia, y replanteado para usar PKCE como debía haber sido desde el principio.

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Oauth.Demo.Api
# http://localhost:5090
```

Y juegas con `api.http`:

```http
### Qué flujo para una SPA
GET http://localhost:5090/oauth/flujo?cliente=Spa
# → AuthorizationCodePkce, TieneUsuario=true, NecesitaSecreto=false

### ¿Es Implicit deprecado?
GET http://localhost:5090/oauth/deprecado/Implicit
# → true

### Generar un par PKCE
GET http://localhost:5090/oauth/pkce
# → { codeVerifier, codeChallenge, method: "S256" }

### Planificar el login completo para una SPA
POST http://localhost:5090/oauth/plan
Content-Type: application/json

{
  "cliente": "Spa",
  "tenantId": "common",
  "clientId": "abc-123-...",
  "redirectUri": "https://miapp.com/callback",
  "scopes": ["api://miapi/.default"]
}
```

Los 27 tests cubren cada decisión y el vector RFC 7636 entre ellos. Si alguien refactoriza el `Base64Url` y rompe el encoding, ese test salta — y te ahorra el bug intermitente del caso 3 de arriba.

Para auditar tus App Registrations reales:

- `scripts/01-oauth-config.sh` — lista redirect URIs, audiencias, permisos y avisa si alguna app tiene **Implicit habilitado** (deprecado). Es la auditoría más útil después de una migración. Requiere `Directory Readers`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué este submódulo tampoco tiene CAPA de integración

Tres veces el mismo razonamiento porque es exactamente el mismo motivo: **no hay un emulador fiable de Entra ID** (ni de Auth0, ni de Okta...). El flujo OAuth completo requiere un IdP real que emita tokens reales con firmas reales. Cualquier "mock IdP" que escribieras estaría incompleto y no te ayudaría a detectar problemas reales.

Lo que sí podemos testear, y testeamos al 100%:

- La decisión de qué flujo usar (`OAuthFlowAdvisor`).
- Que el cálculo de PKCE es correcto (vector RFC 7636).
- Que la URL de authorize se construye con los parámetros correctos y bien encodeados.
- Que el grafo DI compone bien el `ILoginPlanner`.

Para validar el flujo end-to-end, lo correcto es desplegar una app pequeña con `Microsoft.Identity.Web` contra un tenant real (puede ser uno gratuito) y verificar que login y refresh funcionan. Esa validación es manual y se hace una vez, no en cada commit.

---

## 9. La conversación con el equipo: "vamos a hacer OAuth a mano"

Esta conversación aparece en algún punto de muchos proyectos. Alguien con buena voluntad propone "no usemos Microsoft.Identity.Web, total son cuatro llamadas HTTP". La respuesta sosegada:

- **No son cuatro llamadas**: hay descubrimiento de metadatos OIDC, gestión de claves rotadas (JWKs), validación de firma con el algoritmo correcto, comprobación de audience, comprobación de issuer, comprobación de expiración con clock skew, refresh tokens, single sign-on, single sign-out, errores de red, errores de tenant deshabilitado...
- **Cada bug es un agujero de seguridad**: si validas tokens mal, dejas pasar tokens falsificados. Si manejas refresh mal, dejas sesiones abiertas. Si no validas audience, una app cualquiera de tu tenant puede acceder a la tuya con su token.
- **Microsoft mantiene la librería**: cuando Microsoft cambia algo (rota una key, despliega un endpoint nuevo, anuncia un nuevo algoritmo), la librería se actualiza. Tu código a mano no.

Este ejemplo modela la decisión y los parámetros precisamente para que tengas claro que el código que sí escribes es la parte de negocio (qué scopes pides, qué App Roles defines, cómo autorizas) — la parte del protocolo va por librería siempre.

---

## 10. Glosario breve

- **OAuth2**: protocolo de autorización delegada. Permite que un cliente actúe en nombre del usuario sobre un recurso, sin que el cliente conozca las credenciales del usuario.
- **OpenID Connect (OIDC)**: capa de autenticación sobre OAuth2. Añade el id_token, que prueba la identidad del usuario.
- **Authorization Code**: flujo donde el IdP devuelve un código intermedio (no el token) que el backend del cliente intercambia por el token. El más seguro y el estándar de hoy.
- **PKCE**: extensión que añade una prueba criptográfica al flujo Authorization Code, eliminando la necesidad de client secret y protegiendo contra el robo del código. RFC 7636.
- **Client Credentials**: flujo para clientes sin usuario (daemon, servicio, batch). El cliente se autentica con su propio credential (secret o cert), no en nombre de un usuario.
- **Device Code**: flujo para dispositivos sin entrada cómoda (CLI, TV, IoT). El usuario completa el login en otro dispositivo introduciendo un código.
- **On-Behalf-Of (OBO)**: flujo donde una API actúa en nombre del usuario que ya se autenticó contra otra API. Encadenamiento de APIs.
- **Implicit / ROPC**: flujos deprecados. Implicit por exponer el token; ROPC por requerir la contraseña del usuario. Ambos están fuera de uso en apps nuevas.
- **Scope**: el permiso concreto que el cliente pide. Ejemplos: `openid`, `profile`, `email`, `api://miapi/.default`, `User.Read`.
- **State**: parámetro anti-CSRF en el flujo. El cliente lo genera, lo manda en /authorize, lo recibe de vuelta en el callback, y verifica que coincide.
- **Nonce**: parámetro anti-replay para el id_token. Similar al state pero protege contra el reuso de tokens antiguos.
- **id_token**: token firmado que prueba la identidad del usuario (claims `sub`, `name`, `email`). Tu app lo lee para "saber quién es" el usuario.
- **access_token**: token firmado que autoriza al cliente a llamar al recurso protegido. Lleva los scopes concedidos.
- **refresh_token**: token de larga duración que sirve para obtener nuevos access_tokens sin re-pedir login al usuario.

---

## 11. Cierre

Tres piezas: la decisión correcta del flujo, el PKCE bien calculado, la URL de authorize bien construida. Es todo lo que tu código debe controlar de OAuth2/OIDC; el resto lo hace una librería seria. Si tienes claros los seis flujos y por qué cada uno existe, ya entiendes la mayor parte de lo que ves en proyectos con login moderno.

Lo siguiente es [`S6.4 — Autenticación desktop / MSIX`](../S6.4-auth-desktop-msix/MANUAL.md), donde el mismo OAuth se aplica a apps de escritorio y MSIX con los matices del Web Account Manager (WAM) y los redirect URIs de tipo `ms-appx-web://`.
