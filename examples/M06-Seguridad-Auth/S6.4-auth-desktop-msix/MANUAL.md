# Manual del alumno — S6.4 · Auth desktop y MSIX

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta por qué autenticar una app de escritorio es distinto a autenticar una web, qué hace WAM frente al system browser, cuál es el redirect URI correcto para una app MSIX y cómo se decide cuándo el token vale, refresca o exige interacción.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.4](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.4-auth-desktop-msix-v3.md). Tres piezas de lógica pura (selector de método, selector de redirect URI, máquina de estados del ciclo de token) más un planificador que las une.

*Creado: 2026-05-20 17:20 +0200*

---

## 1. La idea en una frase

Las apps de escritorio (WPF, WinForms, MSIX) son **clientes públicos**: no pueden guardar un secret porque viven en la máquina del usuario. Eso simplifica algunas cosas (PKCE obligatorio, sin client_secret) y complica otras (¿cómo se abre la ventana de login?, ¿dónde se cachea el token?, ¿qué pasa cuando Conditional Access exige MFA en mitad de la sesión?). Este submódulo modela esas decisiones como funciones puras: qué método de autenticación elegir según el contexto, qué redirect URI configurar, y qué hacer cuando el token está en uno de sus cuatro estados posibles.

La implementación real del login no se ejecuta aquí (MSAL `PublicClientApplication` con broker WAM no es emulable en un test verde). Lo que se prueba es la decisión: dado el contexto, ¿qué método uso?, ¿qué URI configuro?, ¿qué hago con el token ahora?.

---

## 2. El problema real que hay detrás

Tres situaciones reales que justifican el submódulo:

**Caso 1 — la app WPF con embedded browser.** Un equipo desarrolló una aplicación WPF que autenticaba con WebView2 embebido. Funcionaba en local, funcionaba en QA, funcionaba para 99 de cada 100 usuarios. El usuario 100 estaba en una empresa con Conditional Access que requería biométrica o un certificado de dispositivo Windows. La biométrica del sistema operativo no estaba accesible desde un WebView2 embebido. Resultado: el usuario veía un mensaje de error críptico, recargaba, volvía a fallar, llamaba a soporte. La solución correcta era cambiar a WAM (el broker nativo de Windows) que sí tiene acceso al hardware. Migración: dos días de un developer.

**Caso 2 — la app MSIX que no podía hacer login.** Un equipo empaquetó una app WinForms como MSIX. La app autenticaba contra Entra ID con un redirect URI `http://localhost`. En la versión no empaquetada funcionaba. En la versión MSIX el callback nunca llegaba: las MSIX viven en un sandbox y no pueden recibir tráfico en localhost. La solución era cambiar el redirect URI a `ms-appx-web://microsoft.aad.brokerplugin/{client-id}` (el broker plugin de WAM) y habilitar WAM en MSAL. Una vez configurado, la app autenticaba en tres clicks con SSO completo, como hace Outlook.

**Caso 3 — el token que caduca a media tarde.** Una app desktop autenticaba al usuario al abrir, cacheaba el token, y todo bien las primeras tres horas. A media tarde el access_token caducaba (lifetime típico: una hora) y la app empezaba a devolver 401. El developer pensaba que el ciclo era "vuelvo a pedir interactivo cuando caduque". La solución correcta: pedir `AcquireTokenSilent` primero — MSAL usa el refresh_token cacheado para obtener un access_token nuevo sin abrir ninguna ventana. Solo si el silent falla (porque el refresh también caducó, ~90 días, o porque Conditional Access exige claims nuevos), entonces vuelve interactivo. La diferencia entre la implementación ingenua y la correcta es 30 líneas de código y la diferencia entre una app frustrante y una usable.

Los tres casos los resuelven las tres tablas del ejemplo: qué método (WAM mejor que embedded), qué redirect URI (broker plugin para MSIX), y qué hacer con el token según su estado.

---

## 3. Por qué esto importa en tu stack

Si tienes —o vas a tener— una app de escritorio que se autentica contra Entra ID, las tres decisiones de este submódulo son las que vas a tener que tomar antes de escribir la primera línea de MSAL. Vale la pena tener claros los tradeoffs antes de quedarte atascado.

Tres preguntas que conviene poder responder de cabeza:

- **¿En qué entorno se ejecuta la app?** Windows con Entra Join → WAM. Windows sin join → system browser. Linux/macOS → system browser. Sin browser → device code. La decisión condiciona qué librería de MSAL usar y cómo configurarla.
- **¿Está la app empaquetada como MSIX?** Si sí, el redirect URI tiene que ser el del broker plugin (no localhost). Y necesitas habilitar la API del broker en MSAL.
- **¿La organización tiene Conditional Access que pueda exigir MFA o claims extra a mitad de sesión?** Si sí, el ciclo de token tiene que manejar el reto de claims, no solo el reto de "caducado".

Si ignoras estas preguntas, vas a tener bugs raros en producción. Si las tienes claras, la implementación es directa.

---

## 4. La analogía vertebradora: las llaves del piso compartido

Imagina un piso compartido por varias personas. Cada inquilino tiene su llavero con varias llaves: una de la calle, una del portal, una de su habitación, una del armario común. Para entrar y salir, hay tres formas distintas según el momento:

- **El inquilino que ya vive y tiene las llaves**: simplemente abre con su llavero. Es **`UsarCacheSilent`** — el access_token está en la cache de MSAL, válido, no hace falta ninguna interacción.
- **El inquilino que perdió la llave de su habitación pero sigue teniendo la de la calle**: pide al portero (Entra ID) una llave nueva de la habitación a cambio de mostrar la llave maestra (refresh_token) que el portero le dio el primer día. No tiene que volver a hacer el ingreso completo. Es **`RefrescarSilent`** — el refresh_token sigue válido, MSAL lo intercambia por un nuevo access_token sin abrir ventana.
- **El inquilino completamente nuevo o el que ha estado de viaje tres meses**: tiene que ir a recepción con su DNI, comprobar quién es, recibir el llavero completo desde cero. Es **`Interactive`** — primera vez o refresh caducado, MSAL abre la ventana de login.
- **El inquilino al que el portero le pide hoy una nueva prueba de identidad** (porque ha habido un cambio de política, porque el dueño del piso lo exige, porque hay sospechas): además del DNI, le piden la huella biométrica. Es **`InteractiveConClaims`** — Conditional Access exige claims extra (MFA, dispositivo conforme), MSAL fuerza una interacción específica con esos claims.

Y luego está la manera de entrar al edificio:

- **El portero con SSO nativo de Windows** (WAM): es el conserje que reconoce a Pedro porque le ve todos los días y lo conoce, le abre con un gesto, comparte el reconocimiento con todos los servicios del edificio (Outlook, Teams, esta app). Es la mejor opción cuando estás en Windows con Entra Join.
- **El navegador del sistema** (system browser): es entrar por la puerta principal del edificio donde hay un sistema de visita normalizado. Funciona en todas las plataformas (Windows, Linux, macOS), no es tan rápido como WAM pero es robusto y muy compatible.
- **El navegador embebido** (embedded WebView2): es un mostrador improvisado dentro de tu piso para que el visitante haga el check-in ahí. Funciona, pero tiene mil límites: la biométrica del edificio no llega ahí, las cookies no se comparten con el sistema general, las extensiones del browser no aplican. Microsoft lo califica como "aceptable" y nunca como recomendado.
- **El código en una pantalla** (device code): cuando el visitante está en una TV o un kiosko sin teclado, el portero le da un código de seis dígitos y le dice "ve a otra pantalla, abre login.microsoftonline.com/devicecode, mete este código". Es para CLIs y dispositivos sin entrada.

Mantén la imagen mientras lees el código: tres llaveros (cache, refresh, login), cuatro porteros (WAM, system, embedded, device code).

---

## 5. Recorrido por el código

### `DesktopFlowAdvisor` — qué método para qué contexto

Cuatro escenarios, una recomendación clara:

```csharp
public static MetodoAuthDesktop Recomendar(ContextoDesktop ctx) => ctx switch
{
    ContextoDesktop.WindowsEntraJoined => MetodoAuthDesktop.Wam,
    ContextoDesktop.WindowsGenerico    => MetodoAuthDesktop.SystemBrowser,
    ContextoDesktop.MultiPlataforma    => MetodoAuthDesktop.SystemBrowser,
    ContextoDesktop.KioscoOCli         => MetodoAuthDesktop.DeviceCode,
    _ => throw new ArgumentOutOfRangeException(nameof(ctx)),
};
```

WAM es la elección por defecto en Windows con Entra Join porque te da SSO con el resto del ecosistema de Microsoft. Si Outlook ya está logueado, tu app también lo está sin un click más. Si Conditional Access requiere biométrica, WAM la pide al SO directamente (Windows Hello). Si la app está empaquetada como MSIX, WAM funciona perfectamente con el redirect URI del broker plugin.

System browser es la red de seguridad para todo lo demás: Windows sin join, Linux, macOS. Funciona en todos lados, comparte cookies con el navegador principal del usuario (si ya está logueado en otra pestaña, login transparente), y es la única opción 100% multiplataforma.

Embedded browser (`EmbeddedBrowser`) es lo que **el código no recomienda** —la clase tiene una constante `EsRecomendado(...)` que lo marca como `false` siempre—. La razón: el WebView2 embebido tiene limitaciones de acceso al hardware (biométrica), las cookies no se comparten con el browser principal del sistema, y los problemas de Conditional Access que vimos en el caso 1. Si llegas a un proyecto que lo usa, plantea la migración a WAM o system browser.

Device code es para CLIs y dispositivos sin entrada cómoda. La librería Azure CLI lo usa por defecto cuando se ejecuta sin opciones especiales.

Y luego la constante que define todo lo demás:

```csharp
public const bool EsClientePublico = true;
```

Una app desktop **siempre** es cliente público. No hay forma fiable de guardar un client secret en una app que se distribuye al usuario final. Por eso PKCE es obligatorio y nunca se usa Client Credentials.

### `RedirectUriAdvisor` — los tres URIs que importan

Tres opciones, cada una con su contexto:

```csharp
public const string SystemBrowser = "http://localhost";
public const string LegacyOob     = "urn:ietf:wg:oauth:2.0:oob";

public static string Para(TipoApp tipo, string clientId) => tipo switch
{
    TipoApp.SystemBrowser => SystemBrowser,
    TipoApp.Wam or TipoApp.Msix =>
        $"ms-appx-web://microsoft.aad.brokerplugin/{clientId}",
    TipoApp.Legacy => LegacyOob,
    _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
};
```

- **`http://localhost`** es el redirect URI del system browser. MSAL abre un puerto aleatorio en localhost, el navegador hace el callback ahí, MSAL lo intercepta. Solo funciona si la app puede recibir tráfico en localhost (las MSIX no pueden por el sandbox).
- **`ms-appx-web://microsoft.aad.brokerplugin/{client-id}`** es el broker plugin URI de WAM/MSIX. Lo gestiona Windows internamente; tu app no recibe tráfico HTTP. **Imprescindible para MSIX** y la opción correcta para WAM.
- **`urn:ietf:wg:oauth:2.0:oob`** (out of band) es el redirect URI legacy. El callback no va a ninguna URL; el código de autorización se le muestra al usuario por pantalla y este lo copia/pega en la app. Era la opción antes del system browser. Está deprecado y nunca debe usarse en apps nuevas.

El método `EsLegacy(uri)` te ayuda a auditar configuraciones existentes para detectar `oob` y proponer migración.

### `TokenLifecycle` — la máquina de estados del token

La pieza más densa del submódulo. Cuatro estados posibles del token y una decisión limpia:

```csharp
public sealed record EstadoToken(
    bool HayCuentaEnCache,
    bool AccessTokenValido,
    bool RefreshTokenValido,
    bool RetoConditionalAccess);

public static AccionToken Siguiente(EstadoToken e)
{
    if (e.RetoConditionalAccess) return AccionToken.InteractiveConClaims;
    if (!e.HayCuentaEnCache)     return AccionToken.Interactive;
    if (e.AccessTokenValido)     return AccionToken.UsarCacheSilent;
    if (e.RefreshTokenValido)    return AccionToken.RefrescarSilent;
    return AccionToken.Interactive;
}
```

El orden de las comprobaciones importa:

1. **Conditional Access tiene precedencia sobre todo lo demás**. Si Entra te ha pedido claims extra (MFA, device compliant, location...), nada del cache vale. Tienes que volver a interactivo con esos claims específicos. Esto es el caso real más sutil: tu access_token es perfectamente válido, pero la organización ha cambiado una política y exige que el usuario apruebe de nuevo.
2. **Sin cuenta cacheada** → primera vez de uso, login completo.
3. **Cuenta en cache + access_token válido** → silent puro, sin tocar la red (lo más rápido).
4. **Access caducado + refresh válido** → silent con refresh (una llamada HTTP, sin UI).
5. **Refresh también caducado** (~90 días sin uso) → vuelta a interactivo.

El método `RequiereUi(accion)` te dice cuáles de las cuatro acciones implican abrir ventana: solo `Interactive` y `InteractiveConClaims`. Los dos "silent" son transparentes para el usuario.

¿Por qué importa modelarlo como máquina de estados pura? Porque en el código real de la app desktop, esto está enmarañado dentro de un `try/catch` con `MsalUiRequiredException`. La lógica "qué hago según el estado" se mezcla con "MSAL me lanzó tal excepción". Separarla en una función pura te deja:

- Testear las cinco rutas sin tocar MSAL.
- Verificar que el orden de prioridad es el correcto (especialmente Conditional Access > todo).
- Aplicar la misma decisión en un planificador (`DesktopAuthPlanner`) que oriente al equipo antes de implementar.

### `DesktopAuthPlanner` — el planificador completo

Combina los tres advisors anteriores. Dado un contexto, un tipo de app y un estado del token, devuelve un plan completo: método de auth a usar, redirect URI a configurar, y siguiente acción del ciclo de token. Es el servicio inyectable que materializa la decisión completa.

En el grafo DI se registra como Singleton (es state-less, todas las funciones internas son puras). Su test de DI verifica que el contenedor real resuelve la instancia y produce un plan razonable para un caso típico (Windows joined + cache → WAM + silent).

---

## 6. La conversación con el equipo: "¿WAM o no WAM?"

WAM es la opción correcta en Windows en casi todos los casos. Aun así, conviene anticipar las objeciones:

- **"WAM solo funciona en Windows 10+"**. Cierto, pero las apps desktop nuevas que se autentican contra Entra ID típicamente exigen Windows 10/11 como mínimo. Si tu base de usuarios incluye Windows 7 o 8.1, tienes un problema más grande que WAM (ese sistema está sin soporte de Microsoft).
- **"WAM requiere configurar el broker plugin en MSAL"**. Cierto, es una línea más en la inicialización del `PublicClientApplication.Builder`. A cambio recibes SSO con todo el ecosistema, soporte completo de Conditional Access (incluyendo biométrica), y el patrón "MSIX-friendly" desde el principio.
- **"En desarrollo no tengo Entra Join"**. Cierto, pero MSAL detecta si el broker está disponible y, si no lo está, cae a system browser automáticamente. Tu código no tiene que ramificar: declaras el broker y MSAL hace lo correcto en cada entorno.
- **"Embedded es más rápido para hacer pruebas"**. Pruebas como mucho. Para producción, los problemas con Conditional Access y el sandboxing de cookies no compensan.

La regla operativa final: **en Windows con Entra ID, WAM por defecto; system browser como fallback; embedded nunca en código nuevo**.

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Desktop.Demo.Api
# http://localhost:5091
```

Endpoints para jugar con las decisiones:

```http
### Qué método en Windows con Entra Join
GET http://localhost:5091/desktop/flujo?ctx=WindowsEntraJoined
# → Wam

### Qué redirect URI para una MSIX
GET http://localhost:5091/desktop/redirect-uri?tipo=Msix&clientId=abc-123
# → ms-appx-web://microsoft.aad.brokerplugin/abc-123

### Siguiente acción con token caducado y refresh válido
POST http://localhost:5091/desktop/token-accion
Content-Type: application/json

{
  "hayCuentaEnCache": true,
  "accessTokenValido": false,
  "refreshTokenValido": true,
  "retoConditionalAccess": false
}
# → RefrescarSilent

### Plan completo
POST http://localhost:5091/desktop/plan
Content-Type: application/json

{
  "contexto": "WindowsEntraJoined",
  "tipoApp": "Msix",
  "clientId": "abc-123",
  "estadoToken": { "hayCuentaEnCache": true, "accessTokenValido": true, ... }
}
# → { metodo: "Wam", redirectUri: "ms-appx-web://...", accion: "UsarCacheSilent" }
```

Los 26 tests cubren cada combinación: las cuatro recomendaciones de método, los tres tipos de redirect URI, las cinco rutas de la máquina de estados (incluido el reto de Conditional Access que manda sobre todo lo demás).

Para auditar las App Registrations desktop reales:

- `scripts/01-desktop-app-config.sh` — lista las apps marcadas como cliente público, comprueba si tienen el broker URI configurado y avisa si alguna sigue usando `oob`. Es la auditoría que ejecutas tras una migración de embedded a WAM para verificar que no quedan apps "a medio migrar".

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La integración con MSIX, paso a paso

Para que la app desktop empaquetada como MSIX autentique con WAM, hay cuatro cosas que tienen que estar bien:

1. **En la App Registration de Entra**:
   - Marca "Allow public client flows" en Authentication.
   - Añade el redirect URI `ms-appx-web://microsoft.aad.brokerplugin/{client-id}` (con tu client-id).
   - Si quieres soportar también dev sin empaquetar, añade `http://localhost` como redirect URI adicional.

2. **En el `Package.appxmanifest` del MSIX**:
   - Añade la capability `internetClient` (necesaria para llamar a Entra).
   - Asegúrate de que el `Identity` del package coincide con lo que esperan tus reglas de firma.

3. **En el código (`PublicClientApplication.Builder`)**:
   - `.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))` activa WAM.
   - `.WithRedirectUri("ms-appx-web://microsoft.aad.brokerplugin/{client-id}")` si la app es MSIX, o `.WithDefaultRedirectUri()` si confías en MSAL para elegirlo según el contexto.

4. **En el package de NuGet**:
   - Necesitas `Microsoft.Identity.Client` y `Microsoft.Identity.Client.Broker` (este último específicamente para WAM).

Si alguna de las cuatro cosas falla, el login no funciona y suele dar errores poco descriptivos. La auditoría con el script `01-desktop-app-config.sh` cubre la parte de App Registration; las otras tres son responsabilidad de tu proyecto.

---

## 9. Por qué este submódulo tampoco tiene CAPA de integración

Misma razón que en S6.1, S6.2 y S6.3: **no hay forma fiable de emular un IdP** con MSAL en un test verde. El login interactivo con MSAL necesita un usuario humano clickando en una ventana, o una máquina con WAM disponible. Cualquier "mock" sería un esqueleto incompleto.

Lo que sí podemos probar (y probamos al 100%):

- Las cuatro recomendaciones de método según contexto.
- Las tres opciones de redirect URI.
- Las cinco rutas de la máquina de estados del token.
- El grafo DI compone bien el `IDesktopAuthPlanner`.

La validación end-to-end se hace una vez, manualmente: una app WPF mínima con MSAL configurado, levanta el login WAM, verifica que se cachea bien, simula un access caducado para ver el silent refresh. Esa prueba no entra en el ciclo de CI; entra en el checklist de "está implementado correctamente".

---

## 10. Las cinco trampas más comunes en auth desktop

**Trampa 1 — Usar embedded browser por costumbre**. Funciona en local, falla en empresas con Conditional Access. Cámbialo a WAM o system browser desde el principio.

**Trampa 2 — Olvidar marcar "Allow public client flows"**. En la App Registration, este flag no está activo por defecto. Sin él, MSAL devuelve un error genérico difícil de diagnosticar.

**Trampa 3 — Usar `http://localhost` en una MSIX**. El sandbox no permite recibir tráfico en localhost. El callback nunca llega. Cambia al broker URI.

**Trampa 4 — Pedir `Interactive` siempre que no haya access válido**. Sin `AcquireTokenSilent` primero, cada hora abres una ventana al usuario aunque el refresh siga vivo. Frustrante.

**Trampa 5 — No manejar el `MsalUiRequiredException` con claims**. Cuando Conditional Access manda un reto a mitad de sesión, la excepción trae los `Claims` específicos a propagar. Si los ignoras y pides un interactivo normal, el reto se repite en bucle.

Las cinco se previenen con el mapeo claro entre contexto/tipo de app/estado de token y la decisión correcta — exactamente lo que materializa el ejemplo.

---

## 11. Glosario breve

- **MSAL**: Microsoft Authentication Library. La librería oficial de Microsoft para autenticación contra Entra ID en cualquier plataforma (.NET, JS, iOS, Android, Java, Python).
- **`PublicClientApplication`**: la clase de MSAL para clientes públicos (desktop, móvil, SPA, CLI). No requiere client secret.
- **WAM** (Web Account Manager): el broker de identidad nativo de Windows 10+. Comparte sesión con Outlook, Teams, Office. Acceso al hardware del sistema (biométrica, certificados de dispositivo).
- **System browser**: el navegador por defecto del sistema operativo (Edge, Chrome, Safari, Firefox). MSAL abre una ventana de login en él y recibe el callback en `http://localhost`.
- **Embedded browser**: un WebView2 dentro de la propia app. Microsoft lo califica de "aceptable" pero no recomendado.
- **Device code flow**: flujo donde el usuario introduce un código de seis dígitos en una URL desde otro dispositivo con teclado. Para CLIs y kioscos.
- **MSIX**: formato moderno de empaquetado de apps de Windows. Sandbox, instalación reversible, distribución por Store o sideload.
- **Broker plugin URI**: `ms-appx-web://microsoft.aad.brokerplugin/{client-id}`. El redirect URI que Windows entiende para apps WAM/MSIX.
- **`AcquireTokenSilent`**: método de MSAL que intenta obtener un token sin interacción (de cache o refresh). Tu primer intento siempre debe ser este.
- **`AcquireTokenInteractive`**: método de MSAL que abre la ventana de login. Solo se llama si silent falla.
- **`MsalUiRequiredException`**: la excepción que MSAL lanza cuando silent no puede continuar (refresh caducado o claims challenge). El bloque catch decide si pedir interactivo simple o con claims.

---

## 12. Cierre

Las apps de escritorio modernas con Entra ID son sencillas si tienes claras las tres decisiones: qué método de auth (WAM en Windows joined, system browser en el resto), qué redirect URI (localhost para system, broker plugin para WAM/MSIX) y qué acción de token según su estado. Las tres decisiones se modelan como funciones puras y se prueban en milisegundos; la implementación real es directa una vez tienes el plan.

Lo siguiente es [`S6.5 — Seguridad de datos`](../S6.5-seguridad-datos/MANUAL.md), donde el foco se mueve de identidad a datos: cifrado at-rest e in-transit, customer-managed keys, CORS, y los principios de protección que aplican a cualquier servicio que toque información sensible.
