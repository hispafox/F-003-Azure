# Manual del alumno — S6.P · Práctica OAuth2 + Key Vault

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue por Portal, scripts. Este manual va antes: te cuenta qué se entrega exactamente al final de la práctica, por qué Easy Auth elimina el código de auth de tu app, qué papel juegan las cabeceras `X-MS-CLIENT-PRINCIPAL-*` y cómo verificar de un vistazo que no quedan secretos en claro.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.P](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.P-practica-oauth2-keyvault-v3.md). Tres piezas de lógica pura (selector de Easy Auth, constructor de App Settings con referencias, parser de principal) más una API protegida que se prueba end-to-end simulando lo que Easy Auth inyecta.

*Creado: 2026-05-20 18:35 +0200*

---

## 1. La idea en una frase

La práctica integra los dos submódulos más operativos del módulo —S6.3 (OAuth2 con Entra ID) y S6.6 (Key Vault)— en una App Service mínima que protege `/api/perfil` con Easy Auth y guarda sus secretos en Key Vault con referencias. El resultado entregable: **una API en Azure cuyo código no contiene ni una línea de auth, ni un secreto, ni una llamada a Microsoft.Identity.Web**. Easy Auth se encarga de la validación de tokens delante de la app; Key Vault custodia los secretos; la Managed Identity de la app les da acceso. Tu código solo lee variables de entorno y cabeceras estándar.

---

## 2. El problema real que hay detrás

La pregunta clásica al diseñar una API nueva en Azure: "¿meto Microsoft.Identity.Web en el proyecto y configuro la auth en código?". La respuesta para apps simples es **no**: Easy Auth (App Service Authentication) hace exactamente lo mismo —valida tokens contra Entra ID, intercepta requests no autenticados— **sin que tu código sepa que existe**. Lo configuras en el portal en cinco clicks y tu app se queda completamente desnuda: lee la cabecera `X-MS-CLIENT-PRINCIPAL-NAME` que Easy Auth ha inyectado y sabe quién es el usuario.

Tres ventajas operativas concretas:

1. **Cero dependencias de librerías de auth** en el proyecto. Tu `csproj` no tiene `Microsoft.Identity.Web` ni `Microsoft.Identity.Client`. Tu `Program.cs` no llama a `AddAuthentication`. Cuando Microsoft actualice algo en su validación de tokens, no tienes que actualizar nada.
2. **El equipo nuevo entiende la app sin saber OAuth**. Un nuevo dev lee el código y ve `HttpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"]`. Es transparente. No tiene que aprender flows ni anatomía de JWT para entender la app.
3. **El secret de Entra está en Key Vault, no en el proyecto**. La configuración de Easy Auth refiere `@Microsoft.KeyVault(...)` para el client secret. Cuando rota, lo rotas en el Vault sin tocar la app.

Hay casos donde Microsoft.Identity.Web es necesario (apps multiapp con tokens delegados, OBO, autorización fina por scope dentro de la app), pero para una API protegida estándar Easy Auth es claramente la opción correcta.

---

## 3. Por qué esto importa en tu stack

Si tienes una API en App Service que protege endpoints contra Entra ID, este patrón te ahorra **mantenimiento operativo** y **código de seguridad escrito por ti**. Tres preguntas que la práctica resuelve:

- **¿Dónde valida la app los tokens?** En Easy Auth, antes de que el request llegue a tu código.
- **¿Dónde está el client secret de la App Registration?** En Key Vault, referenciado desde la App Setting `AzureAd__ClientSecret` con `@Microsoft.KeyVault(...)`.
- **¿Cómo accede mi app al Key Vault?** Con su Managed Identity, sin contraseña, sin connection string. El rol "Key Vault Secrets User" se le asigna manualmente una vez.

La práctica entrega un sistema en producción donde estas tres respuestas son visibles y verificables con un script de auditoría.

---

## 4. La analogía vertebradora: el guardia del portal y la oficina dentro

Imagina un edificio corporativo de varias plantas:

- **El guardia del portal** (Easy Auth) está en la entrada principal. Mira el carnet de identidad de cada visitante, lo valida contra el registro de la empresa (Entra ID), y solo deja pasar a los empleados con un pase válido. Los que no tienen carnet —reciben un sobre estandarizado que dice "401: trae el carnet" y se vuelven a casa.
- **Una vez dentro**, el visitante autenticado ya no tiene que volver a enseñar el carnet. Lleva una **chapa estándar** con su nombre y el nombre de su empresa de origen (eso son las cabeceras `X-MS-CLIENT-PRINCIPAL-NAME` y `X-MS-CLIENT-PRINCIPAL-IDP`). Cada oficina del edificio mira la chapa para saber quién es y de dónde viene.
- **La oficina** (tu app) **no tiene mostrador de recepción propio**. No comprueba carnets, no hace login, no sabe siquiera qué es Entra ID. Solo lee la chapa: "Hola Pedro de la empresa Acme, ¿en qué te puedo ayudar?".
- **El armario con los suministros caros** (Key Vault) está en otra zona del edificio. La oficina necesita un objeto del armario (el client secret para reportar al guardia, una API key externa, un certificado). Pero la oficina **no tiene la llave del armario** — tiene una **credencial corporativa** (Managed Identity) que el armario reconoce. La oficina dice "déjame el objeto X", el armario verifica que la oficina tiene rol "Lector", y se lo entrega.

Hay algo importante en esta arquitectura: **la oficina no sabe nada de seguridad**. No tiene cerradura propia (Easy Auth la pone delante). No tiene caja fuerte propia (Key Vault las gestiona aparte). No tiene una lista de quién puede entrar (Entra ID la mantiene). La oficina solo hace su trabajo: lee la chapa del visitante, decide qué hacer.

Esa es la arquitectura que entrega la práctica. Y la verificación final del entregable es justamente eso: comprobar que la oficina está **realmente desnuda**, sin secretos ni código de auth.

---

## 5. Recorrido por el código

### `EasyAuthAdvisor` — el comportamiento del guardia

Easy Auth tiene dos comportamientos posibles para un request no autenticado:

```csharp
public static string AccionNoAutenticado(TipoApp tipo) => tipo switch
{
    TipoApp.Api    => "Return401",                          // API: devuelve 401 limpio
    TipoApp.WebApp => "LoginWithAzureActiveDirectory",      // Web: redirige al login
    _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
};
```

- **API**: cuando un cliente sin token llama a `/api/perfil`, Easy Auth devuelve `401 Unauthorized`. El cliente (Postman, una SPA, otra API) interpreta el 401 y reacciona como corresponda (típicamente "ejecuta el flujo de login, obtén un token, retry"). **Esta es la opción correcta para una API**.
- **Web App**: cuando un usuario sin sesión visita `/dashboard`, Easy Auth lo redirige a `login.microsoftonline.com`, completa el flujo OAuth, y vuelve a la URL original con la sesión establecida. Para una web con login interactivo, este es el comportamiento natural.

La diferencia está en el flag `--action` cuando configuras Easy Auth con `az webapp auth update` (o el equivalente en el portal). Confundir el flag te lleva a comportamientos raros: una API que redirige a un login HTML (cliente confundido), o una web que devuelve 401 (usuario confundido).

La segunda función construye el issuer correcto:

```csharp
public static string Issuer(string tenantId) =>
    $"https://login.microsoftonline.com/{tenantId}/v2.0";
```

Importante: **`/v2.0` al final**. Es el endpoint de Entra ID v2.0, el moderno. El endpoint v1.0 (sin `/v2.0`) sigue funcionando pero está en deprecación gradual. Para apps nuevas, siempre v2.0.

### `KeyVaultRefAppSettings` — las App Settings del entregable

La función que construye el diccionario:

```csharp
public static IReadOnlyDictionary<string, string> Construir(
    string tenantId, string clientId, string vault) =>
    new Dictionary<string, string>
    {
        ["AzureAd__TenantId"]     = tenantId,
        ["AzureAd__ClientId"]     = clientId,                              // público
        ["AzureAd__ClientSecret"] = Referencia(vault, "AzureAd-ClientSecret"),
        ["ExternalApiKey"]        = Referencia(vault, "ExternalApiKey"),
    };
```

Dos categorías de App Settings:

- **Datos públicos** (`TenantId`, `ClientId`): valores literales. El `ClientId` no es secreto — aparece en cualquier captura de tráfico HTTP del login. El `TenantId` es público por definición.
- **Secretos** (`ClientSecret`, `ExternalApiKey`): valores con la sintaxis `@Microsoft.KeyVault(VaultName=...;SecretName=...)`. App Service los resuelve en runtime usando su Managed Identity contra el Key Vault.

La función verificadora es la pieza más importante del entregable:

```csharp
public static bool SoloReferencias(IReadOnlyDictionary<string, string> settings)
{
    foreach (var (k, v) in settings)
    {
        if (k.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || k.Contains("ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            if (!v.StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase))
                return false;
        }
    }
    return true;
}
```

Recorre todas las settings, identifica las que parecen secretos por el nombre (`Secret`, `ApiKey`), y verifica que el valor empieza por `@Microsoft.KeyVault(`. Si alguna setting "sospechosa" tiene valor literal, la función devuelve `false`. **Esta es la verificación que el script `01-verify-practica.sh` ejecuta contra tu App Service real** para certificar que el entregable está bien.

Es el tipo de auditoría que en proyectos serios va al pipeline de CD: bloquea el deploy si detecta valores literales en App Settings que deberían ser referencias.

### `EasyAuthPrincipal` — leer las cabeceras de la chapa

Una vez Easy Auth ha autenticado al usuario, inyecta cabeceras en el request:

```csharp
public const string HeaderNombre = "X-MS-CLIENT-PRINCIPAL-NAME";
public const string HeaderIdp    = "X-MS-CLIENT-PRINCIPAL-IDP";

public static Principal Desde(IReadOnlyDictionary<string, string?> headers)
{
    headers.TryGetValue(HeaderNombre, out var nombre);
    headers.TryGetValue(HeaderIdp, out var idp);

    var autenticado = !string.IsNullOrWhiteSpace(nombre);
    return new Principal(
        autenticado,
        autenticado ? nombre : null,
        autenticado ? (string.IsNullOrWhiteSpace(idp) ? "aad" : idp) : null);
}
```

Tres campos de la chapa que importan al endpoint:

- **`X-MS-CLIENT-PRINCIPAL-NAME`**: el `preferred_username` del usuario en Entra. Normalmente su email (`ana@empresa.com`).
- **`X-MS-CLIENT-PRINCIPAL-IDP`**: el identity provider. Para Entra ID es `aad`. Easy Auth soporta también `google`, `facebook`, etcétera, si configuraste otros providers.
- **`X-MS-CLIENT-PRINCIPAL`** (no usada aquí, pero existe): un JSON base64 con todos los claims del token. Útil si necesitas roles o grupos.

Lo más importante: **estas cabeceras solo vienen si Easy Auth las inyecta**. Si llegan en un request sin estar Easy Auth delante, alguien las está spoofeando. Por eso esta práctica funciona solo desplegada en Azure detrás de Easy Auth — en local hay que **simular las cabeceras** explícitamente para que la API responda 200.

Los tests E2E del proyecto lo hacen así: usan `WebApplicationFactory` para arrancar la API en memoria, configuran un `HttpClient` que añade las cabeceras `X-MS-CLIENT-PRINCIPAL-*` con valores fake, y comprueban que `/api/perfil` responde 200 con el nombre simulado. Sin las cabeceras, el mismo cliente recibe 401.

### `PracticaPlanner` y el checklist del entregable

El servicio inyectable que une todo. Su método principal devuelve un plan con:

- Tipo de app a usar: API (con `Return401`).
- Issuer correcto basado en el TenantId.
- App Settings que deberías tener (tenant/client en claro, secretos como referencias).
- Rol RBAC mínimo a asignar a la Managed Identity de la app sobre el Key Vault: "Key Vault Secrets User".
- Checklist de verificación del entregable: Easy Auth on, App Settings solo referencias, MI con rol asignado, `/health` devuelve 200, `/api/perfil` sin token devuelve 401 y con token devuelve 200.

Es la pieza que el `01-verify-practica.sh` ejecuta tras el deploy para certificar que el entregable está completo.

---

## 6. La pieza más educativa: la API desnuda

Mira el `PracticaEndpoints.cs` (la API real, no las funciones de la práctica). Lo que **no hay**:

- No hay `AddAuthentication(...)`.
- No hay `AddAuthorization(...)`.
- No hay `UseAuthentication()` ni `UseAuthorization()`.
- No hay `[Authorize]` en ningún sitio.
- No hay paquete `Microsoft.Identity.Web` en el `csproj`.
- No hay `IConfiguration` leyendo "AzureAd:ClientSecret" desde ningún sitio.

Lo que **sí hay**:

- Un endpoint `/health` que es público (Easy Auth lo deja pasar porque le decimos en su config que `/health` está excluido).
- Un endpoint `/api/perfil` que lee la cabecera `X-MS-CLIENT-PRINCIPAL-NAME` y, si está ahí, devuelve un perfil con ese nombre. Si no está, devuelve 401.
- Lectura de App Settings vía `IConfiguration` para datos públicos (tenant, client id) cuando los necesite mostrar.

Esa es la diferencia entre "implementar auth a mano" y "delegar auth a Easy Auth". En la versión Easy Auth, el código es trivial; en la versión Microsoft.Identity.Web, el código tiene veinte líneas más de configuración y dependencias.

¿Cuándo necesitas la versión Microsoft.Identity.Web en vez de Easy Auth? Tres casos:

- Cuando necesitas **OBO** (On-Behalf-Of) para llamar a otras APIs en nombre del usuario.
- Cuando necesitas **autorización fina** dentro de la app con `[Authorize(Roles = "...")]` por App Roles, y Easy Auth no te basta con las cabeceras.
- Cuando despliegas fuera de App Service (a un contenedor en AKS sin sidecars de auth, a una VM, a un servicio que no tenga Easy Auth disponible).

En el resto de los casos, Easy Auth gana en simplicidad.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Practica.Demo.Api
# http://localhost:5094
```

Tres requests en `api.http`:

```http
### /health es público
GET http://localhost:5094/health
# → 200

### /api/perfil sin cabeceras Easy Auth → 401
GET http://localhost:5094/api/perfil
# → 401

### /api/perfil simulando que Easy Auth nos autenticó como Pedro
GET http://localhost:5094/api/perfil
X-MS-CLIENT-PRINCIPAL-NAME: pedro@empresa.com
X-MS-CLIENT-PRINCIPAL-IDP: aad
# → 200 con { autenticado: true, nombre: "pedro@empresa.com", idp: "aad" }
```

Los 12 tests cubren las tres partes:

- Unit: el advisor, las app settings, el parser de principal con casos límite (sin cabecera, con cabecera vacía, con idp ausente).
- DI: el grafo resuelve correctamente.
- E2E con `WebApplicationFactory`: la API completa con las cabeceras simuladas. Es la prueba que más se acerca al comportamiento en Azure.

Para el despliegue real (entregable):

1. **Resource Group** `rg-curso-m06-s6p`.
2. **Key Vault** con RBAC habilitado y purge protection. Crea dos secretos: `AzureAd-ClientSecret` y `ExternalApiKey`.
3. **App Registration** en Entra: client + secret. Guarda el secret en el Key Vault.
4. **App Service** con Managed Identity. Asígnale "Key Vault Secrets User" sobre el Key Vault.
5. **Configura las App Settings** del App Service con la sintaxis `@Microsoft.KeyVault(...)` para los secretos.
6. **Activa Easy Auth**: portal → Authentication → Add identity provider → Microsoft → Configure → Restrict access = "Require authentication" + Unauthenticated requests = "Return 401".
7. **Deploy** la API desde VS Code.
8. **Verifica** con `scripts/01-verify-practica.sh`:
   - `/health` devuelve 200.
   - `/api/perfil` sin token devuelve 401.
   - Con `az account get-access-token --resource <app-uri>` y enviando ese token en el header `Authorization: Bearer ...`, devuelve 200.
   - App Settings solo referencias (sin valores literales para los `*Secret*` o `*ApiKey*`).
   - La MI tiene rol "Key Vault Secrets User" sobre el Vault.

Tras la práctica, borra el RG. Coste total: menos de 0,10 €.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y `func start` cuando aplique.

---

## 8. La rúbrica del entregable

Para que la práctica cuente como completada, los seis puntos deben estar verdes:

```
[x] Easy Auth activado en la App Service con identity provider Microsoft
[x] Restrict access = "Require authentication"
[x] Unauthenticated requests = "Return 401" (porque es API, no web)
[x] App Settings sin secretos literales (solo @Microsoft.KeyVault references)
[x] Managed Identity del App Service con rol "Key Vault Secrets User" en el KV
[x] Verificación end-to-end: /health 200, /api/perfil 401 sin token y 200 con token válido
```

El script `01-verify-practica.sh` los comprueba uno a uno y emite un diagnóstico. Es la forma en que el formador valida tu entregable sin tener que pasar por los seis lugares del portal manualmente.

---

## 9. Por qué este ejemplo sí tiene CAPA E2E (cuando los conceptuales no la tenían)

Diferencia importante respecto a los seis submódulos anteriores: aquí hay tests **end-to-end** que arrancan la API en `WebApplicationFactory` y le mandan requests reales. ¿Por qué ahora sí, si en S6.1–S6.6 dijimos que no había nada emulable?

Porque lo que se testea aquí es la **lógica de la app, no Easy Auth**. La app de la práctica tiene un comportamiento claro: si llega la cabecera `X-MS-CLIENT-PRINCIPAL-NAME`, responde 200 con un perfil; si no, devuelve 401. Esa lógica vive **dentro del código** y se puede probar con `WebApplicationFactory` simulando las cabeceras que Easy Auth inyectaría en Azure.

Lo que **no se testea** sigue siendo lo de antes: el flujo OAuth real contra Entra ID. Para validar eso, hay que desplegar y probar a mano con `az account get-access-token`. Pero la parte de tu código que reacciona a las cabeceras —que es la parte que tú escribes— sí se cubre con tests rápidos en memoria. Es exactamente el patrón "tu código se prueba, lo de Azure se valida manualmente" que vimos en M04-S4.5.

---

## 10. La conversación con el equipo: "¿Easy Auth o Microsoft.Identity.Web?"

Pregunta que aparece cuando arrancas una nueva API:

**Argumentos a favor de Easy Auth**:

- Configuración en cinco clicks. Sin escribir código de auth.
- Sin paquetes de NuGet, sin actualizaciones de librería que mantener.
- El equipo nuevo entiende la app sin conocer OAuth.
- Funciona idéntico con cualquier proveedor (Microsoft, Google, GitHub, Facebook) — solo cambias la configuración del portal.
- Easy Auth se actualiza sin que tu app se entere; las mejoras de seguridad llegan transparentemente.

**Argumentos a favor de Microsoft.Identity.Web**:

- Necesitas tokens delegados para llamar a otras APIs (Microsoft Graph, otra API tuya): OBO requiere código.
- Necesitas autorización fina por App Roles dentro de la app: `[Authorize(Roles = "Admin")]` funciona mejor con la librería que solo con cabeceras Easy Auth.
- Despliegas fuera de App Service (AKS sin sidecar, contenedores en otros sitios). Easy Auth solo existe en App Service y Functions.
- Quieres controlar la validación de token tú mismo (caso muy raro, normalmente innecesario).

La regla práctica: **empieza con Easy Auth siempre que puedas; pasa a Microsoft.Identity.Web cuando una limitación específica te obligue**. La mayoría de APIs estándar de empresa funcionan perfectamente con Easy Auth.

---

## 11. Glosario breve

- **Easy Auth** (App Service Authentication): mecanismo de App Service y Function App que valida tokens delante de la app, sin código en la app. Configuración en el portal.
- **`X-MS-CLIENT-PRINCIPAL-NAME`**: cabecera que Easy Auth inyecta con el nombre del usuario autenticado (típicamente su email).
- **`X-MS-CLIENT-PRINCIPAL-IDP`**: cabecera que Easy Auth inyecta con el identity provider (`aad` para Entra, `google`, etcétera).
- **`X-MS-CLIENT-PRINCIPAL`**: cabecera que lleva un JSON base64 con todos los claims del token. Útil para roles y grupos.
- **`Return401` vs `LoginWith...`**: las dos opciones de Easy Auth cuando llega un request sin token. Para API, `Return401`; para Web App con UI, `LoginWith...`.
- **Issuer v2.0**: `https://login.microsoftonline.com/{tenantId}/v2.0`, el endpoint moderno de Entra ID. Siempre con `/v2.0` en apps nuevas.
- **Key Vault Reference**: sintaxis `@Microsoft.KeyVault(VaultName=...;SecretName=...)` en App Settings. App Service la resuelve usando su Managed Identity.
- **Managed Identity del App Service**: identidad del recurso, sin password. Se le asigna RBAC sobre el Key Vault. Es lo que permite a Easy Auth y a las referencias de KV resolver secretos sin tener credenciales en el código.

---

## 12. Cierre

La práctica entrega una API segura sin código de seguridad. Easy Auth se ocupa de la validación, Key Vault custodia los secretos, la Managed Identity les da acceso, y tu código se queda limpio leyendo cabeceras estándar. Es la arquitectura por defecto para APIs de App Service que protegen recursos contra Entra ID — sencilla, mantenible, y robusta operativamente.

Lo siguiente es [`S6.P2 — Práctica Easy Auth`](../S6.P2-practica-easy-auth/MANUAL.md), que profundiza específicamente en Easy Auth con un escenario adicional (login interactivo de Web App, no solo API protegida). Cierra el módulo M06.
