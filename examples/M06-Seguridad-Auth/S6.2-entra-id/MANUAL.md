# Manual del alumno — S6.2 · Microsoft Entra ID

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta qué es exactamente Entra ID (más allá de "el Active Directory de Azure"), por qué Managed Identity es la primera opción que deberías intentar siempre, y por qué tu aplicación nunca debe validar tokens JWT a mano aunque sea trivial decodificarlos.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.2](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.2-entra-id-v3.md). Cuatro piezas de lógica pura (tipos de identidad, clasificador de roles, inspector de JWT, autorización por App Roles) más scripts `az` de solo lectura para auditar tu directorio real.

*Creado: 2026-05-20 16:35 +0200*

---

## 1. La idea en una frase

Entra ID (antes conocido como Azure Active Directory) es **el directorio único** sobre el que descansan todas las identidades de tu organización en Azure: usuarios humanos, grupos, aplicaciones, identidades de los recursos y identidades de los pipelines. Cuando alguien dice "tengo acceso a esa Function" o "esa aplicación llama a Cosmos" o "el deploy pipeline sube cosas al storage", lo que está pasando por debajo es siempre lo mismo: una identidad de Entra ID con un rol asignado.

El submódulo enseña a distinguir las tres formas de identidad para aplicaciones (Managed Identity, Service Principal, App Registration), a saber dónde se gestionan los roles (que NO es el mismo sitio para roles de Azure y roles de Entra), a inspeccionar un JWT cuando algo no funciona, y a autorizar por App Roles. Todo como funciones puras testeables; el directorio real se audita con scripts de solo lectura.

---

## 2. El problema real que hay detrás

Tres preguntas que llegan continuamente a un equipo que arranca con Azure:

**Pregunta 1 — "¿Por qué la app no puede leer Cosmos? Le he dado permisos y sigue fallando con 403."**

Casi siempre la respuesta es: le han dado permisos al rol equivocado, o han confundido el sitio donde se asignan. Le dieron rol *Cosmos DB Built-in Data Contributor* desde *Entra ID → Roles and administrators*, pero ese rol es un rol RBAC de Azure y se asigna en *el recurso → Access Control (IAM)*. Son dos sistemas distintos que comparten nombre pero no nada más.

**Pregunta 2 — "¿Cómo se autentica el pipeline de GitHub Actions contra Azure? ¿Es un usuario? ¿Es una API key?"**

Ninguna de las dos. Es un **Service Principal**: una identidad de aplicación creada en Entra ID, sin contraseña humana, que se autentica con secreto o certificado. Y a partir de cierta versión moderna, mejor todavía: federación OIDC con GitHub, así no hay siquiera secreto que rotar.

**Pregunta 3 — "El usuario dice que su token tiene rol Admin pero la app le devuelve 403. ¿Quién miente?"**

Nadie miente. Probablemente lo que pasa es uno de tres casos:

- El usuario tiene rol Admin en *Entra ID* (administrador del directorio), pero la app espera el claim `roles` del token con valor "Admin" (un App Role definido en la App Registration de la app). Son cosas distintas.
- El token está caducado y la app lo está rechazando antes de mirar el rol.
- La app está mirando el campo equivocado del token (`groups` vs `roles`, `app_displayname` vs `name`).

Para responder bien a estas tres preguntas, hay que tener clarísimas cuatro tablas: tipos de identidad, sistemas de roles, anatomía de un JWT y mecánica de App Roles. Es lo que enseña este submódulo.

---

## 3. Por qué esto importa en tu stack

Si tu sistema tiene **más de un componente que se autentica** —y todos los sistemas modernos lo tienen— necesitas a Entra ID como referencia mental. Tres situaciones donde se nota:

- **Un App Service necesita leer un Key Vault**. La forma correcta es Managed Identity: das el rol "Key Vault Secrets User" a la identidad del App Service. Sin connection string, sin secreto, sin rotación. La forma incorrecta es generar un Service Principal manual y meter su secreto en una variable de entorno — funciona, pero es deuda técnica desde el día uno.
- **Un pipeline de CI/CD necesita desplegar a Azure**. La forma correcta es un Service Principal con federación OIDC (GitHub Actions o Azure DevOps). Sin secretos almacenados. La forma incorrecta es un Service Principal con secreto que caduca en seis meses — y todos los pipelines caen el día que caduca.
- **Una app multitenant necesita autenticar usuarios**. La forma correcta es App Registration con OAuth2/OIDC, Microsoft.Identity.Web validando los tokens. La forma incorrecta es validar el JWT tú con una librería cualquiera — vas a tener fallos de seguridad sutiles (no verificar la firma, no comprobar audience, no rotar claves).

La regla operativa es sencilla: **Entra ID es la fuente única; productos de Microsoft validan los tokens; tu código consume las decisiones**.

---

## 4. La analogía vertebradora: el carnet de identidad y los pases del edificio

Imagina un edificio corporativo con guardias en la entrada y áreas restringidas. Hay dos tipos de credenciales:

- **El carnet de identidad de la empresa**: identifica quién eres dentro de la organización. Lo emite Recursos Humanos. Te abre puertas por defecto (recepción, cafetería, baños) y te permite identificarte ante cualquier sistema interno. **Esto es Entra ID** — el directorio que dice "este usuario es Ana López, del departamento de finanzas, miembro del grupo Contadores".
- **Los pases para áreas concretas**: cada área del edificio (la sala de servidores, el archivo, la planta de dirección) tiene su propio sistema de pases. Tener carnet de empleado no te abre automáticamente la sala de servidores; necesitas un pase específico para esa sala, emitido por el responsable de la sala. **Esto es RBAC de Azure** — los roles que se asignan recurso a recurso desde el panel IAM del recurso.

Además del personal humano, hay otras identidades en el edificio:

- **Los robots de servicio**: las Roombas que limpian de noche, los carritos automáticos de la cafetería. No tienen carnet humano; tienen una identidad propia emitida por Mantenimiento. Pueden tener pases para áreas específicas (cocina, almacén) pero nunca para la sala de servidores. **Esto son las Managed Identities** — identidades emitidas por Azure a los recursos, sin contraseña, gestionadas por la plataforma.
- **El servicio de mensajería**: cuando contratas a una empresa externa de mensajería, sus repartidores tienen un pase de visitante que les da acceso solo a la zona de cargas. Su empresa puede tener varios mensajeros; cada uno usa el mismo tipo de pase. **Esto son los Service Principals** — identidades de aplicaciones que se autentican con secreto o certificado, típicamente usadas por pipelines y herramientas.
- **El sistema de aprobación de la sala VIP**: cuando un evento usa la sala VIP, los organizadores configuran qué tipos de invitado pueden entrar (oradores, asistentes, prensa). Cada invitado lleva una pegatina con su tipo. El portero mira la pegatina y decide. **Esto son los App Roles** — roles definidos por una aplicación en su App Registration, que se incluyen en el token del usuario y que la app inspecciona.

Mantén la imagen mientras lees el resto. Cada pieza del submódulo encaja en este edificio.

---

## 5. Recorrido por el código

### `IdentityTypeAdvisor` — qué identidad usar y cuándo

Tres escenarios, una recomendación clara para cada uno:

```csharp
public static TipoIdentidad Recomendar(Escenario escenario) => escenario switch
{
    Escenario.RecursoAzureAccedeAOtro => TipoIdentidad.ManagedIdentity,
    Escenario.ScriptOPipeline        => TipoIdentidad.ServicePrincipal,
    Escenario.AppAutenticaUsuarios   => TipoIdentidad.AppRegistration,
    _ => throw new ArgumentOutOfRangeException(nameof(escenario)),
};
```

- Si un recurso de Azure necesita acceder a otro (App Service → Cosmos, Function → Key Vault, Logic App → SQL): **Managed Identity**. Es lo más simple y lo más seguro: sin secretos, gestionado por la plataforma, asociado al ciclo de vida del recurso.
- Si un script o pipeline necesita autenticarse (GitHub Actions, Azure DevOps, una utilidad de migración manual): **Service Principal**. Es lo que se acerca más a "una API key" pero con auditoría y gestión de ciclo de vida adecuada.
- Si una aplicación necesita autenticar usuarios humanos (Web App con login, app móvil, SPA): **App Registration**. Es la pieza configurable que define cómo tu app interactúa con OAuth2/OIDC contra Entra ID.

Y luego está la prioridad cuando hay dudas:

```csharp
public static IReadOnlyList<TipoIdentidad> Prioridad { get; } =
[
    TipoIdentidad.ManagedIdentity,
    TipoIdentidad.ServicePrincipal,
    TipoIdentidad.AppRegistration,
];
```

La regla práctica: **siempre que sea posible, Managed Identity**. Es la única opción que no tiene secreto que rotar. Cuando no es posible (porque el cliente no es un recurso de Azure), Service Principal con **certificado** (no con secret-string, que es la opción peor de las tres). Solo cuando ninguna de las dos sirve, App Registration con su client secret.

### `RoleClassifier` — dos sistemas de roles con nombres parecidos

La fuente más común de confusión en Azure. Hay **dos sistemas de roles distintos**:

- **RBAC de Azure**: roles que aplican sobre recursos de Azure. Ejemplos: *Owner*, *Contributor*, *Reader*, *Storage Blob Data Contributor*, *Cosmos DB Built-in Data Contributor*. Se asignan en el panel "Access Control (IAM)" del recurso.
- **Roles de Entra ID**: roles que aplican sobre el directorio en sí. Ejemplos: *Global Administrator*, *User Administrator*, *Application Administrator*, *Security Administrator*. Se asignan en "Entra ID → Roles and administrators".

```csharp
public static SistemaDeRoles Clasificar(string rol) { ... }
public static string DondeSeAsigna(SistemaDeRoles s) => s switch
{
    SistemaDeRoles.AzureRbac => "Portal → Recurso → Access Control (IAM)",
    SistemaDeRoles.EntraId   => "Portal → Entra ID → Roles and administrators",
    _ => "Desconocido",
};
```

Ejemplos prácticos para grabarse:

- *Owner* (RBAC de Azure) significa "puede hacer todo en este recurso, incluyendo gestionar permisos". No tiene nada que ver con ser administrador del tenant.
- *Global Administrator* (Entra ID) significa "puede gestionar todo el directorio, crear usuarios, ver auditorías de Entra". Por defecto NO te da permisos sobre recursos de Azure — incluso si eres GA del tenant, no puedes leer un blob storage si no tienes rol Storage Blob Data Reader sobre ese storage.

La consecuencia práctica: cuando un usuario te diga "no puedo acceder a X", lo primero es preguntar **qué tipo de X es** y **dónde está su rol asignado**. Si es un recurso de Azure y el rol está en Entra ID, mira en el sitio equivocado.

### `JwtInspector` — leer claims, no validar tokens

Cuando un sistema te devuelve 401 o 403, lo primero que necesitas es ver qué hay dentro del token que mandó el cliente. Esa es la función de `JwtInspector`:

```csharp
public static ClaimsResumen Inspeccionar(string jwt, DateTimeOffset? ahora = null)
{
    var partes = jwt.Split('.');
    using var doc = JsonDocument.Parse(DecodeBase64Url(partes[1]));
    // ... extracción de sub, name, email, roles, groups, aud, iss, exp
}
```

Recibe un JWT como string, divide en sus tres partes (header, payload, signature), decodifica el payload de base64url, y te devuelve un resumen de los claims:

- `sub`: identificador único del sujeto del token (usuario o app).
- `name` / `preferred_username` / `email`: identificadores legibles.
- `roles`: los App Roles del token (lo que veremos en el siguiente apartado).
- `groups`: los grupos de Entra ID a los que pertenece el usuario.
- `aud`: audience — para qué aplicación está pensado este token.
- `iss`: issuer — quién emitió el token (debería ser tu tenant de Entra ID).
- `exp`: cuándo caduca el token.

Y la pregunta que va por delante de todas: **¿está caducado el token?** El método inyecta un reloj opcional (`ahora`) para que los tests puedan congelar el tiempo y comprobar tanto el caso "vivo" como el "expirado".

> **Aviso crítico que vale el doble repetir**: `JwtInspector` **solo decodifica**, NO valida la firma. Sirve para **inspección**, nunca como mecanismo de auth. Si en tu app real escribes algo como "leo el token, miro el rol, devuelvo 200/403", **estás haciéndolo mal**. La validación real (firma, audience, issuer, expiración, claves rotadas) la hace `Microsoft.Identity.Web` —u otra librería equivalente para tu plataforma— y nunca tu código a mano.

¿Por qué no validar tokens a mano? Porque hay docenas de cosas que un atacante puede explotar si lo intentas: algoritmo `none`, claves desactualizadas, audience errónea, claims mal interpretados. Las librerías de Microsoft hacen las catorce validaciones correctas; tu código va a olvidar tres y te van a entrar tokens falsificados.

### `AppRolesAuthorizer` — autorizar por el claim `roles`

Cuando defines App Roles en tu App Registration (por ejemplo "Admin", "Customer", "Auditor"), Entra ID incluye esos roles en el token de cualquier usuario que tenga asignado uno de ellos. Tu app los recibe en el claim `roles` y los puede usar para autorizar:

```csharp
public DecisionAutorizacion Autorizar(
    IEnumerable<string> rolesDelToken, string rolRequerido)
{
    var tiene = rolesDelToken.Any(r =>
        string.Equals(r?.Trim(), rolRequerido.Trim(),
            StringComparison.OrdinalIgnoreCase));
    return tiene
        ? new DecisionAutorizacion(true, $"El token incluye el rol '{rolRequerido}'")
        : new DecisionAutorizacion(false,
            $"403 Forbidden: falta el rol '{rolRequerido}' en el token");
}
```

Lo importante es la comparación **case-insensitive**: si el rol se llama "Admin" y el token trae "admin", debe autorizarse. La razón es que el casing puede variar entre el portal, los tokens y el código; forzar uno solo es pedir un bug nocturno cuando alguien renombra el rol.

En tu app de producción esto se materializa con `[Authorize(Roles = "Admin")]` sobre los endpoints — el atributo hace exactamente esta comparación contra el claim `roles` del usuario actual.

---

## 6. Las decisiones de identidad, en cinco preguntas

Cuando tengas que decidir qué identidad usar en un escenario nuevo, este es el árbol mental:

**Pregunta 1 — ¿La identidad es de un recurso de Azure (App Service, Function, VM, Logic App...)?**

- Sí → Managed Identity. Punto. No hay nada que pensar.
- No → siguiente pregunta.

**Pregunta 2 — ¿La identidad va a ser usada por un humano que se autentica?**

- Sí → App Registration con OAuth2/OIDC.
- No (es para un script, pipeline, herramienta) → siguiente pregunta.

**Pregunta 3 — ¿La identidad va a vivir mucho tiempo y necesita rotación gestionada?**

- Sí, es para un pipeline → Service Principal con federación OIDC (sin secreto que rotar).
- Sí, pero es una herramienta interna → Service Principal con certificado.
- Es para algo muy puntual y de corta vida → Service Principal con secret (último recurso).

**Pregunta 4 — ¿La identidad necesita permisos sobre recursos de Azure o sobre el directorio?**

- Sobre recursos → RBAC de Azure (panel IAM del recurso).
- Sobre el directorio (crear usuarios, gestionar otras apps) → Rol de Entra ID.
- Ambos → asignaciones separadas en cada sitio.

**Pregunta 5 — ¿La autorización es por pertenencia a grupo (mira el claim `groups`) o por rol de aplicación (mira el claim `roles`)?**

- Si es algo administrativo de la organización (todos los del grupo "Finanzas" tienen acceso) → grupos.
- Si es algo específico de la aplicación (en mi app hay Admin, Customer, Auditor) → App Roles.

Con estas cinco preguntas resuelves el 95% de los casos en proyectos reales.

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Entra.Demo.Api
# http://localhost:5089
```

Los endpoints del `api.http` te permiten jugar con cada pieza:

```http
### Qué identidad usar para "App Service → Cosmos DB"
GET http://localhost:5089/entra/identidad?escenario=RecursoAzureAccedeAOtro

### Clasificar "Storage Blob Data Contributor"
GET http://localhost:5089/entra/rol?nombre=Storage%20Blob%20Data%20Contributor

### Inspeccionar un JWT (cópialo de jwt.io o de un browser)
POST http://localhost:5089/entra/token
Content-Type: text/plain

eyJ0eXAi...etcétera

### Autorizar contra un rol de App Roles
POST http://localhost:5089/entra/autorizar
Content-Type: application/json

{
  "rolesDelToken": ["Customer", "Auditor"],
  "rolRequerido": "Admin"
}
```

Los 29 tests cubren cada escenario en milisegundos. El helper `JwtBuilder` de los tests construye tokens fake con los claims que quieras, así puedes simular un token con rol Admin, otro con rol Customer, otro caducado, otro con claims raros — todo sin tocar Entra ID.

Para auditar tu directorio **real** tienes dos scripts en el directorio `scripts/`:

- `01-directory-inventory.sh` — usuarios, grupos y guests del tenant. Útil para limpiar invitados antiguos.
- `02-app-registrations.sh` — todas las App Registrations y sus secretos. **El más importante en operaciones reales**: detecta secretos que están a punto de caducar antes de que rompan tu pipeline.

Ambos son de solo lectura. Solo necesitan rol `Directory Readers`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La cosa más rara de Entra: dos sistemas de roles con nombres parecidos

Vale la pena dedicarle una sección entera porque es donde más se equivoca la gente.

**Hay dos sistemas de roles en Azure que se gestionan en sitios distintos y aplican a cosas distintas:**

| | Roles RBAC de Azure | Roles de Entra ID |
| --- | --- | --- |
| **Sobre qué aplican** | Recursos de Azure (storage, SQL, App Service...) | El directorio en sí (usuarios, apps, grupos...) |
| **Dónde se asignan** | Portal → Recurso → Access Control (IAM) | Portal → Entra ID → Roles and administrators |
| **Ejemplos** | Owner, Contributor, Reader, Storage Blob Data Contributor | Global Administrator, User Administrator, Application Administrator |
| **Quién los puede asignar** | El Owner del recurso (o User Access Administrator) | Global Administrator, Privileged Role Administrator |

¿La consecuencia práctica? Confusiones típicas:

- *Owner* y *Global Administrator* son **roles diferentes**. Owner sobre una suscripción no te hace Global Admin de Entra. Global Admin de Entra no te hace Owner de ningún recurso (aunque sí te permite asignarte tú mismo el rol Owner).
- Una persona puede ser *Global Administrator* del directorio y, sin embargo, no poder leer un blob storage. Necesita además el rol "Storage Blob Data Reader" sobre ese recurso concreto.
- Cuando configuras un Service Principal para un pipeline, le asignas roles RBAC de Azure (Contributor sobre tal RG). Le asignas roles de Entra ID solo si el pipeline necesita modificar el directorio (caso raro).

Esto va a aparecer una y otra vez en preguntas reales. Tener la tabla mental clara te ahorra horas.

---

## 9. Por qué este submódulo tampoco tiene CAPA de integración

Misma razón que en S6.1: **Entra ID no se emula**. No hay un "Entra ID emulator" como hay Cosmos emulator o Azurite. Lo único que existe es:

- Un tenant real de pruebas (puedes crear uno gratuito con un email cualquiera, pero ya es Azure real, no emulación).
- Mocks/fakes en tu código (que es exactamente lo que hace el `JwtBuilder` de los tests).

Forzar una CAPA de integración aquí significaría tener un tenant real al que el test se conecta, con usuarios fake, apps fake, secretos rotados... un coste operativo enorme para validar que `Microsoft.Identity.Web` valida tokens. Eso ya lo testea Microsoft; no es tu trabajo.

La aproximación correcta del submódulo: testear la **lógica que sí tienes que escribir** (cuándo recomendar MI vs SP, cómo distinguir roles RBAC de Entra, qué claims sacar de un JWT, cómo autorizar por App Roles) con unit tests rápidos. Y para inspeccionar el directorio real, los scripts `az` de solo lectura.

---

## 10. Las cinco trampas más comunes

Cinco cosas que vas a ver constantemente en proyectos y que ya están explicadas en el ejemplo:

**Trampa 1 — Asignar permisos a usuarios en lugar de grupos**. Pedro tiene acceso a X. Ana también. Cuando entra Luis, alguien tiene que acordarse de darle los mismos accesos. Cuando sale Pedro, alguien tiene que acordarse de retirar los suyos. **Asigna a grupos** y gestiona los grupos. La pertenencia es mucho más auditable y reversible.

**Trampa 2 — Service Principal con secret que vence en 6 meses**. El día que caduca, el pipeline falla. Documentado mil veces. Solución: federación OIDC con GitHub/Azure DevOps cuando se pueda, certificado cuando no, secret como último recurso y con calendario de rotación en el equipo.

**Trampa 3 — Tu app valida tokens a mano**. Ya lo dijimos arriba. `JwtInspector` es para inspección, no para auth. La validación real va por `Microsoft.Identity.Web`.

**Trampa 4 — Confundir el claim `groups` con el claim `roles`**. `groups` son los grupos de Entra ID (organizativos); `roles` son los App Roles definidos en tu App Registration (de aplicación). Si tu app quiere distinguir entre Admin y Customer, usa App Roles, no grupos. Los grupos te dan la organización a la que pertenece el usuario; los App Roles te dan los privilegios que tiene en tu app.

**Trampa 5 — Olvidar revisar invitados B2B**. Cuando contratas un partner externo, lo invitas a tu tenant como guest. Cuando el partner termina su trabajo, alguien tiene que retirar la invitación. Si no, ese guest sigue teniendo acceso seis meses después. El script `01-directory-inventory.sh` lista todos los guests; revísalo trimestralmente.

---

## 11. Glosario breve

- **Tenant**: instancia de Entra ID. Cada organización tiene uno (o varios). Identificado por un GUID (Tenant ID) y un dominio (`contoso.onmicrosoft.com` por defecto).
- **App Registration**: configuración de una aplicación en Entra ID. Define cómo la app se autentica, sus redirect URIs, sus App Roles, sus API permissions.
- **Service Principal**: la instancia "viva" de una App Registration dentro de un tenant. Es la identidad sobre la que se asignan roles. Una App Registration puede tener Service Principals en varios tenants (apps multitenant).
- **Managed Identity**: identidad de Entra ID asociada al ciclo de vida de un recurso de Azure (App Service, Function, VM...). Sin secreto que gestionar; sin password; sin client_id que poner en config. Hay dos sabores: *System-assigned* (ligada al recurso) y *User-assigned* (recurso independiente que puedes compartir).
- **App Roles**: roles definidos por una aplicación en su App Registration. Aparecen en el claim `roles` del token. Sirven para autorizar dentro de la app.
- **OAuth2 / OIDC**: protocolos que se ven en S6.3. OAuth2 es para autorización (delegar acceso); OIDC añade autenticación encima. Entra ID los implementa.
- **JWT**: JSON Web Token. El formato de token que usa Entra ID. Tres partes separadas por punto: header (algoritmo), payload (claims) y signature (firma criptográfica).
- **Claim**: campo dentro del payload de un JWT. `sub`, `name`, `email`, `roles`, `groups`, `aud`, `iss`, `exp` son los más comunes.
- **Federación OIDC**: mecanismo donde un Service Principal se autentica con un token de identidad emitido por GitHub (o Azure DevOps), sin secreto almacenado. Mejor opción para pipelines.

---

## 12. Cierre

Entra ID es la capa transversal sobre la que reposa toda la seguridad de Azure. Si tienes claras las cuatro tablas del submódulo —tipos de identidad, sistemas de roles, anatomía del JWT, App Roles— estás preparado para entender el resto del módulo. Los siguientes submódulos (OAuth2, auth desktop, Key Vault) construyen encima de Entra ID dando flujos concretos a lo que aquí se queda como modelo mental.

Lo siguiente es [`S6.3 — OAuth2 / OpenID Connect`](../S6.3-oauth2-oidc/MANUAL.md), que te muestra los flujos de autenticación (Authorization Code, Client Credentials, PKCE) y cómo se mapean a los tres tipos de identidad que has visto aquí.
