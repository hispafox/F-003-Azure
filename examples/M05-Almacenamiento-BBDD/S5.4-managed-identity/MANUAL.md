# Manual del alumno — S5.4 · Managed Identity

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Útil cuando vas a tocar código. Este manual va antes: te cuenta para qué existe el ejemplo, qué decisión quiere enseñarte y cómo leerlo. Cuando termines, abre el README y todo encajará más rápido.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M05-S5.4](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.4-managed-identity-v3.md) (~28 slides). Las primeras cuatro secciones son el marco mental; de la sección 5 a la sección 8 entras al detalle técnico; el resto es práctica, autoevaluación y un par de avisos antes de pasar a S5.5.

*Creado: 2026-05-20 00:02 +0200*

---

## 1. La idea en una frase

Hasta ahora todos los submódulos de M05 han dejado pendiente la misma pregunta. En S5.1 viste el `if` de `Program.cs` que ramifica entre connection string y Managed Identity. En S5.2, el `Authentication=Active Directory Default` sin password. En S5.3, el comentario "recomendado sin key" en la cuenta de Cosmos. Tres veces apareció la misma idea y tres veces te dije "lo profundizamos en S5.4". Pues aquí estamos.

S5.4 es el submódulo **transversal** del módulo. No te enseña un servicio de datos nuevo: te enseña una forma distinta de conectarte a los que ya conoces. Sin keys, sin passwords, sin connection strings con secretos. La misma app, el mismo código, autenticada por Azure por ti contra Entra ID. Es la pieza que separa un proyecto que funciona de uno que se puede dejar en producción y auditar.

---

## 2. El problema real que hay detrás

Hace un tiempo participé en un incidente clásico. Una storage account key se había filtrado en un commit a un repo privado tres años atrás. El developer responsable ya no trabajaba allí. Nadie hizo `git revert`, nadie rotó la clave. Tres años después, el mismo `appsettings.json` seguía en producción con esa misma key. La cazamos por casualidad, durante un audit no relacionado.

El primer paso, evidente: rotar. Cinco minutos. Lo único razonable. Y a continuación, la pregunta incómoda: ¿cómo evitamos que esto vuelva a pasar? La respuesta de Azure lleva años siendo la misma: **dejar de tener keys que rotar**. Si no hay una key viva en una variable de entorno, no hay nada que filtrar, nada que rotar, nada que olvidar.

Eso es Managed Identity. Tu app no se autentica con un password, se autentica con su propia identidad — una identidad que Azure le crea automáticamente, vinculada al recurso, sin password que viva en ningún lado. Entra ID emite un token temporal, la app lo usa, el token caduca, se pide otro. Sin secretos.

Ahora piensa en lo que el ejemplo va a hacer concreto:

| Necesidad real | Cómo se resuelve | Dónde lo verás |
| --- | --- | --- |
| Conectar la app a Blob Storage sin key | `BlobServiceClient(uri, DefaultAzureCredential)` | [`Program.cs`](src/ManagedIdentity.Demo.Api/Program.cs), endpoint `/blob/contenedores` |
| El mismo código en local (con `az login`) y en Azure (con MI) | `DefaultAzureCredential` con cadena de autenticación | [`CredentialFactory.cs`](src/ManagedIdentity.Demo.Api/Security/CredentialFactory.cs) |
| Detectar si una connection string lleva un secreto embebido | Escaneo de patrones (`password=`, `accountkey=`, `sig=`) | [`ConnectionSecretScanner.cs`](src/ManagedIdentity.Demo.Api/Security/ConnectionSecretScanner.cs) |
| Saber qué rol RBAC dar a la MI por cada servicio (mínimo, no Owner) | Tabla servicio → acceso → rol | [`RbacRoleAdvisor.cs`](src/ManagedIdentity.Demo.Api/Security/RbacRoleAdvisor.cs) |
| Lo que MI no cubre (APIs externas) | Key Vault Reference desde App Settings | `ConnectionSecretScanner` no la marca como secreto |

Las tres clases puras juntas componen el patrón: identidad sin secretos + escaneo de la config + rol mínimo. Es S5.4 en miniatura.

---

## 3. Por qué esto importa en tu stack

Aunque tu aplicación esté técnicamente cifrada, redundada y testeada, mientras dependa de una key viva en un appsettings está a un commit accidental de un incidente. Y los incidentes de keys filtradas son tan comunes que GitHub tiene un servicio entero (Secret Scanning) dedicado a cazarlas en los push. El motivo: pasa. Mucho.

El cambio mental respecto a los submódulos anteriores: aquí el código de aplicación apenas cambia. Cambia *la configuración*. Pasas de tener un `StorageConnection` con `AccountKey=...` a tener un `StorageBlobEndpoint` con solo la URL. Pasas de un `SqlConnection` con `Password=...` a uno con `Authentication=Active Directory Default;`. La línea de código que crea el cliente es prácticamente la misma — lo que desaparece es el secreto en el medio.

Y por eso este ejemplo es transversal. No es un servicio nuevo: es la forma correcta de hablar con los servicios que ya conoces de S5.1, S5.2 y S5.3. Cuando termines este submódulo, mirarás los `Program.cs` de los anteriores con otra cara.

---

## 4. El modelo mental: el pase de empleado, no la copia de las llaves

Imagina dos empresas en el mismo edificio.

La primera reparte llaves físicas. Cada empleado recibe una copia para entrar a su zona. Cuando alguien se va, le piden la llave (o no), y como las llaves ya se copiaron por ahí, el cerrajero tiene que cambiar las cerraduras. Si una llave se pierde, ya nadie sabe quién la tiene. Si alguien la fotocopió en su día, sigue sirviendo. La rotación de llaves es un proyecto pequeño pero permanente, y la única forma de saber que alguien usó una llave es preguntar al portero — si lo apuntó.

La segunda usa pases electrónicos. Cada empleado tiene su tarjeta personal vinculada a su identidad en el sistema. El portero (una centralita) verifica el pase en tiempo real cada vez que alguien quiere entrar. Si un empleado se va, le bajan el pase y al instante deja de funcionar — sin cambiar cerraduras. Si pierde la tarjeta, anulan esa concreta. Si alguien sospechoso intenta colarse con una tarjeta caducada o de otra zona, el sistema lo registra. Y el log queda: quién entró, dónde y cuándo.

Eso es la diferencia entre **connection strings con keys** y **Managed Identity**. La key es la llave física: una vez copiada, no la recuperas, no sabes quién la tiene, y rotar es cambiar la cerradura. La MI es el pase electrónico: la identidad la verifica el portero (Entra ID) cada vez, los permisos se revocan al instante, hay log de auditoría completo, y si alguien intenta entrar sin pase válido, no entra. Punto.

```
Sin Managed Identity (las llaves físicas):
  App  → "Hola Cosmos, aquí está mi password: abc123"
  Cosmos → "OK, password correcto, pasa"

Con Managed Identity (el pase electrónico):
  App  → "Hola Cosmos, soy la app — identidad verificada por Entra ID"
  Cosmos → "Confirmo con Entra ID... eres quien dices ser, y tienes
            permiso. Pasa."
```

Tres frases para fijar el modelo:

- **No hay password.** Entra ID emite un token temporal, la app lo usa, el token caduca. Sin secreto que rotar.
- **El mismo código funciona en local y en Azure.** En local usa tu `az login`; en Azure usa la MI del recurso. La línea que crea el cliente es idéntica.
- **El acceso es revocable al instante.** Si una MI deja de ser de confianza, quitas el rol o desactivas la identidad y al siguiente token la app deja de entrar. Sin "rotar keys", sin reiniciar.

Vuelve a esta imagen cuando aparezca `DefaultAzureCredential`. No es magia: es el pase electrónico de tu app.

---

## 5. DefaultAzureCredential: el pase universal de Azure

[`CredentialFactory.cs`](src/ManagedIdentity.Demo.Api/Security/CredentialFactory.cs) construye una `DefaultAzureCredential` a partir de configuración. Las opciones son tres y cubren los casos reales:

```csharp
public const string KeyUserAssignedClientId = "Azure:UserAssignedClientId";
public const string KeyTenantId             = "Azure:TenantId";
public const string KeyLocalDev             = "Azure:LocalDev";
```

`DefaultAzureCredential` no es una sola credencial: es una **cadena de autenticación** que prueba métodos en orden hasta que uno funciona (slide 7). El orden, simplificado:

1. Variables de entorno (`AZURE_CLIENT_ID`, etc.) — pipelines de CI/CD.
2. Workload Identity Federation — Kubernetes con identidad federada.
3. Managed Identity — App Service, Functions, VMs en Azure.
4. Caché compartida — sesiones de Visual Studio / VS Code.
5. CLI de Azure — `az login` en tu máquina local.
6. Azure PowerShell, Azure Developer CLI…

**En Azure**, la cadena se para en el paso 3: usa la MI del recurso. **En local**, baja hasta el paso 5 y usa tu `az login`. El mismo objeto `DefaultAzureCredential` se ocupa de los dos casos. Tu código no cambia. Esto es lo que hace que el patrón funcione en el día a día.

Tres opciones de configuración del ejemplo y por qué están ahí:

- **`Azure:UserAssignedClientId`** — si en lugar de System-Assigned usas una **User-Assigned Managed Identity** (slide 22), tienes que decirle al SDK *cuál* es la tuya. Una UAMI se crea como recurso aparte y se puede asignar a varias apps; el SDK necesita el `clientId` para saber a cuál refererirse.
- **`Azure:TenantId`** — para escenarios cross-tenant (slide 25): tu app vive en el tenant A pero tiene que hablar con un recurso del tenant B. El TenantId del recurso destino se pasa explícitamente.
- **`Azure:LocalDev = true`** — en desarrollo local, salta el intento de Managed Identity. Sin esto, el SDK intenta contactar con IMDS (el endpoint `169.254.169.254` que solo existe dentro de Azure), espera el timeout, y *después* prueba `az login`. Con esto, va directo a `az login` y ahorras 5-10 segundos en cada petición.

> 🧠 **El detalle de `LocalDev` parece insignificante y no lo es.** La primera vez que pruebes el ejemplo en local sin el flag, vas a pensar que está congelado. No lo está; está esperando un timeout que en local nunca se va a cumplir. `appsettings.Development.json` lleva `Azure:LocalDev = true` por defecto, así que no te vas a tropezar — pero ahora ya sabes por qué.

---

## 6. El singleton que mucha gente se salta

Mira [`Program.cs`](src/ManagedIdentity.Demo.Api/Program.cs):

```csharp
builder.Services.AddSingleton<TokenCredential>(_ => CredentialFactory.Crear(cfg));

builder.Services.AddSingleton(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    var endpoint = cfg["StorageBlobEndpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
        endpoint = "https://devstoreaccount1.blob.core.windows.net";
    return new BlobServiceClient(new Uri(endpoint), credential);
});
```

Una sola línea importante: **`AddSingleton<TokenCredential>`**. Una credencial, registrada una vez, compartida por todos los clientes que la necesiten. Esto es la slide 21 y es donde mucha gente se equivoca: crean una `DefaultAzureCredential` nueva cada vez que construyen un cliente. Funciona, pero pagan caro.

¿Por qué? Porque `DefaultAzureCredential` **cachea internamente el token**. Pedirle un token después del primero es prácticamente gratis: te devuelve el cacheado mientras siga vivo, y solo renueva cuando se acerca el vencimiento. Si creas una credencial nueva en cada construcción de cliente, cada una mantiene su propia caché vacía y todas piden token al iniciar. *Token thrashing*: latencia añadida en cada petición de cliente, picos de carga contra Entra ID, y en escenarios de alta concurrencia, errores 429 del propio Entra ID.

> 🧠 **Una credencial, todos los clientes.** Si tu app habla con Blob, Cosmos y Key Vault al mismo tiempo, los tres clientes comparten la misma `TokenCredential` inyectada. Misma identidad, misma caché, mismo token reutilizado donde se puede. Eso es exactamente lo que hace este ejemplo con el `AddSingleton<TokenCredential>` y lo que verifica el test `DiContainer_Tests`: que el `BlobServiceClient` resuelto del contenedor usa **la misma instancia** de credencial que el grafo. La lección DI con un seam concreto.

Y la otra mitad del singleton, el `BlobServiceClient`: se construye con la URL del endpoint y la credencial. **Sin key, en ningún sitio.** El `https://devstoreaccount1.blob.core.windows.net` que aparece como fallback es solo para que la app arranque sin configuración (la URL del emulador de Azurite); cualquier llamada real a `/blob/contenedores` exige que configures un endpoint real al que tu `az login` o tu MI tenga acceso. El endpoint devuelve **503 con mensaje claro** si no hay endpoint, en vez de petar.

---

## 7. Escanear la configuración: cazar los secretos antes de que se filtren

[`ConnectionSecretScanner.cs`](src/ManagedIdentity.Demo.Api/Security/ConnectionSecretScanner.cs) es una herramienta defensiva. Busca en una cadena de configuración los marcadores típicos de secretos embebidos:

```csharp
"password=", "pwd=", "accountkey=", "sharedaccesskey=",
"accesskey=", "sharedaccesskeyname=", "sig=", "secret="
```

Si alguno aparece (case-insensitive), la conexión es insegura. Si no aparece ninguno, está limpia. Y hay un caso especial: si el valor empieza por `@Microsoft.KeyVault(`, es una **Key Vault Reference** (slide 10) — el secreto real no vive en el App Setting, vive en Key Vault, y App Service lo resuelve usando su propia MI. Para el escáner, eso *no* es un secreto en config: es una referencia. Marca `EsKeyVaultReference = true` y deja `TieneSecreto = false`.

El endpoint `POST /seguridad/scan` lo expone para que juegues. Prueba estos tres valores y mira las respuestas:

```
Server=tcp:sql...;User ID=sa;Password=Secreto123;            → tiene secreto (password=)
Server=tcp:sql...;Authentication=Active Directory Default;   → limpio
@Microsoft.KeyVault(VaultName=kv-prod;SecretName=api-key)    → Key Vault ref (limpio)
```

Y el endpoint `GET /seguridad/checklist` da una vuelta más: recorre la sección `Conexiones` de `appsettings.json` y marca cada entrada como segura o no. Es un chequeo de un solo vistazo del estado de tu configuración. En un pipeline de CI/CD, esa misma función pura se podría enchufar como gate antes del deploy.

> 🎓 **¿Por qué un escáner casero y no una herramienta externa?** Porque la lección está en ver el patrón: los secretos en config tienen pinta concreta y son detectables con una lista corta de marcadores. Que esta clase exista no significa que sustituya a herramientas profesionales —GitHub Secret Scanning, Detect Secrets, gitleaks—; significa que entiendes lo que hacen por dentro. Si entiendes el patrón, sabes cuándo confiar de la herramienta y cuándo dudar.

---

## 8. RBAC mínimo: el rol correcto, no Owner

[`RbacRoleAdvisor.cs`](src/ManagedIdentity.Demo.Api/Security/RbacRoleAdvisor.cs) codifica la tabla servicio → acceso → rol. La lección es la **slide 23**: a una Managed Identity le das **el rol mínimo necesario**, scope mínimo, nunca Owner ni Contributor.

```csharp
ServicioDestino.BlobStorage => acceso switch
{
    Acceso.Lectura          => "Storage Blob Data Reader",
    Acceso.LecturaEscritura => "Storage Blob Data Contributor",
    _                       => "Storage Blob Data Owner",
},
```

Roles **de plano de datos** (acaban en *Data Reader*, *Data Contributor*, *Data Owner*) — son los correctos para una MI que va a leer blobs, escribir documentos, recibir mensajes. Te dejan tocar los datos.

Roles **de plano de control** (Owner, Contributor, User Access Administrator) — son los incorrectos para una MI. Permiten cambiar la configuración del recurso, asignar permisos a otros, borrar el recurso entero. Es como darle al portero las llaves de la cerrajería en vez de la lista de empleados autorizados. `RbacRoleAdvisor.EsRolPeligroso(rol)` los marca, y ninguno de los recomendados por `Recomendar` cae en esa categoría.

Por servicio:

- **Blob/Queue/Table** — `Storage Blob/Queue/Table Data Reader` o `Contributor`.
- **Cosmos DB** — `Cosmos DB Built-in Data Reader` o `Data Contributor`. Importante: no son roles RBAC normales de Azure; son roles específicos de Cosmos y se asignan con `az cosmosdb sql role assignment`, no con `az role assignment`.
- **Key Vault** — `Key Vault Secrets User` (leer secretos) o `Key Vault Secrets Officer` (gestionarlos).
- **Service Bus / Event Hubs** — `Azure Service Bus Data Receiver/Sender/Owner`.
- **Azure SQL** — *no* es un rol RBAC de Azure: dentro de la base de datos, se crea un usuario con `CREATE USER [<app>] FROM EXTERNAL PROVIDER` y se le añade a `db_datareader` o `db_datawriter` (slide 14). Mismo principio de mínimo privilegio, distinta forma técnica.

El método `SufijoAppSetting` cuenta la otra cara: el nombre del App Setting que le dice a la app o a Functions "usa MI, no connection string". `__blobServiceUri` en lugar de la connection string completa, `__accountEndpoint` para Cosmos, `__fullyQualifiedNamespace` para Service Bus, `Authentication=Active Directory Default` en SQL. La tabla completa está en [`RbacRoleAdvisor.SufijoAppSetting`](src/ManagedIdentity.Demo.Api/Security/RbacRoleAdvisor.cs).

> 🧠 **La regla de oro de la slide 27:** `MI = WHO` (identidad sin password), `RBAC = WHAT` (rol mínimo, scope mínimo), `Conditional Access = WHEN/WHERE` (defensa en profundidad). La mayoría de problemas de seguridad con MI no vienen de usarla mal: vienen de darle más permisos de los que necesita. Owner para "salir del paso" es la receta del incidente.

---

## 9. Recorrido guiado: configurando seguridad sin tocar Azure

Lanza la API (ver sección 11) y abre [`api.http`](src/ManagedIdentity.Demo.Api/api.http). Los endpoints `/seguridad/*` funcionan **offline** —son lógica pura—. El `/blob/contenedores` solo responde si configuras Azure de verdad.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /seguridad/scan` con `"Password=Secreto123;"` | `{ tieneSecreto: true, indicadoresEncontrados: ["password="] }` | El escáner caza el patrón del secreto (sección 7). |
| 2 | `POST /seguridad/scan` con `"Authentication=Active Directory Default;"` | `{ tieneSecreto: false }` | Cadena limpia: la conexión sin secreto. |
| 3 | `POST /seguridad/scan` con `"@Microsoft.KeyVault(VaultName=kv-prod;SecretName=api-key)"` | `{ tieneSecreto: false, esKeyVaultReference: true }` | El secreto vive en Key Vault, no en config (slide 10). |
| 4 | `GET /seguridad/rol?servicio=BlobStorage&acceso=Lectura` | `{ rol: "Storage Blob Data Reader", appSetting: "__blobServiceUri", esPeligroso: false }` | Rol mínimo + sufijo que activa MI (sección 8). |
| 5 | `GET /seguridad/rol?servicio=CosmosDb&acceso=LecturaEscritura` | `{ rol: "Cosmos DB Built-in Data Contributor", appSetting: "__accountEndpoint" }` | Rol específico de Cosmos. |
| 6 | `GET /seguridad/checklist` | `{ todoSeguro: true/false, filas: [...] }` | Revisa la sección `Conexiones` del propio `appsettings.json`. |
| 7 | `GET /blob/contenedores` (sin `StorageBlobEndpoint` configurado) | `503 Service Unavailable` con mensaje claro | El endpoint demo es honesto: no hay key, así que sin Azure no funciona. |
| 8 | `GET /blob/contenedores` (con `StorageBlobEndpoint` y `az login` activos) | lista de contenedores, mensaje "sin key" | Conexión real sin un solo secreto en config. |

Un experimento útil: en `appsettings.json` añade una sección `Conexiones` con tres entradas — una con `Password=...`, otra con `Authentication=Active Directory Default`, otra con `@Microsoft.KeyVault(...)` — y dispara `GET /seguridad/checklist`. Ves de un vistazo cuál de tus conexiones es segura y cuál es la que te puede meter en un incidente. Pequeño chequeo que un pipeline podría hacer antes de cada deploy.

Los pasos 1 a 6 son los únicos que corren sin Azure. Por eso los tests unitarios de `ConnectionSecretScanner`, `RbacRoleAdvisor` y `CredentialFactory` corren en milisegundos sin Docker (sección 10).

---

## 10. Por qué el código y los tests están así

La estructura sigue el patrón de M05:

- **`Security/` — lógica pura.** `CredentialFactory` (config → `DefaultAzureCredentialOptions`), `ConnectionSecretScanner` (¿lleva secreto?), `RbacRoleAdvisor` (rol mínimo + sufijo App Setting).
- **`Endpoints/`** — Minimal API fina que delega en las clases puras y, para el endpoint real, en el `BlobServiceClient` inyectado.
- **`Program.cs`** — el patrón de la slide 21: una `TokenCredential` singleton compartida por todos los clientes.

Los tests tienen **dos capas** y no tres:

- **CAPA 1 · Unit** — `Unit_CredentialFactoryTests` (mapeo de config a opciones), `Unit_ConnectionSecretScannerTests` (secreto sí / no / Key Vault), `Unit_RbacRoleAdvisorTests` (cada combinación servicio × acceso). Pura, sin Azure. Rápida.
- **CAPA 0 · DI** — `DiContainer_Tests`. Resuelve `TokenCredential` y `BlobServiceClient` del `WebApplicationFactory` real y **verifica que la credencial inyectada en el cliente es la misma instancia singleton** que la registrada en DI. Eso es la slide 21 con un test concreto: que no estás creando credenciales por accidente. Corre **siempre, sin Docker ni Azure**, porque construir `DefaultAzureCredential` y `BlobServiceClient` es lazy.

Y un detalle importante: **no hay capa de integración a propósito**.

> 🎓 **Por qué falta la integración (y por qué eso es lo correcto).** Managed Identity y Entra ID **no se pueden emular**. Azurite usa una clave fija pública. El emulador de Cosmos también. No hay un "emulador de Entra ID" que firme tokens válidos sin contactar con un tenant real. Un round-trip de integración exigiría una suscripción de Azure activa y `az login`, lo que dejaría `dotnet test` colgando de credenciales personales. La parte testable se aísla en lógica pura (CAPA 1) y en el grafo DI (CAPA 0); la demo real (`/blob/contenedores`) se prueba a mano contra Azure cuando quieres ver el patrón completo. Una `SkippableFact` que siempre se saltase sería deshonesta: mejor reconocer que esta capa **no existe** que fingir cobertura que no aporta nada.

Esto es el contraste con S5.1, S5.2 y S5.3, donde sí había capa de integración con `SkippableFact`. La regla: ¿se puede emular? Sí → integración con emulador. No → solo lógica pura más DI. Y el manual lo cuenta, en vez de esconder por qué falta una capa.

---

## 11. Puesta en marcha, ejecución y pruebas

Sección operativa. Datos verificados contra el repo.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) | compilar y ejecutar | Sí |
| `az login` | Azure CLI logueado en tu tenant | que `DefaultAzureCredential` use tu identidad en local | Solo para `/blob/contenedores` |
| Suscripción Azure con un Storage real y rol asignado | Storage Account + rol `Storage Blob Data Reader` a tu user | probar la demo real (sin key) | Solo para `/blob/contenedores` |
| Cliente REST | extensión *REST Client* de VS Code o `curl` | lanzar [`api.http`](src/ManagedIdentity.Demo.Api/api.http) | Recomendado |

Los endpoints `/seguridad/*` y los tests funcionan completamente **offline**. Solo `/blob/contenedores` exige Azure real.

### 11.2 Compilar

```bash
cd examples/M05-Almacenamiento-BBDD/S5.4-managed-identity
dotnet build ManagedIdentity.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings** (`TreatWarningsAsErrors=true`).

### 11.3 Lanzar la API

```bash
dotnet run --project src/ManagedIdentity.Demo.Api
```

- Escucha en **`http://localhost:5084`** ([`launchSettings.json`](src/ManagedIdentity.Demo.Api/Properties/launchSettings.json), perfil `http`).
- Prueba de vida: `GET http://localhost:5084/health` → `{ "status": "ok" }`.

Los `/seguridad/*` funcionan ya. Si pruebas `/blob/contenedores` sin configurar `StorageBlobEndpoint`, recibes un `503` con mensaje explicando exactamente qué falta. No es un error: es una negativa honesta.

### 11.4 Ejercitar el ejemplo (parte offline)

```bash
# Escanear una cadena con secreto
curl -X POST http://localhost:5084/seguridad/scan -H "Content-Type: application/json" \
  -d '{ "valor": "Server=tcp:sql...;User ID=sa;Password=Secreto123;" }'

# Rol RBAC mínimo para Cosmos lectura/escritura
curl "http://localhost:5084/seguridad/rol?servicio=CosmosDb&acceso=LecturaEscritura"

# Checklist de la sección Conexiones de appsettings.json
curl http://localhost:5084/seguridad/checklist
```

La sección 9 tiene la lista completa con qué demuestra cada paso.

### 11.5 Probar la demo real contra Azure (opcional)

```bash
# 1. Login en Azure con tu cuenta
az login

# 2. Configura StorageBlobEndpoint apuntando a un Storage Account real
#    Edita appsettings.Development.json o pasa por env var:
export StorageBlobEndpoint="https://<cuenta>.blob.core.windows.net"

# 3. Da a tu usuario el rol mínimo en ese Storage (una vez)
PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv)
az role assignment create --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<cuenta>"

# 4. Lanza la API y prueba
dotnet run --project src/ManagedIdentity.Demo.Api
curl http://localhost:5084/blob/contenedores
```

La respuesta lista tus contenedores y el campo `conexion` dice *"sin key — DefaultAzureCredential (slide 6)"*. Es el patrón que vas a usar en Azure: `Program.cs` no cambia, lo que cambia es el contexto (`az login` en local, MI en Azure).

### 11.6 Pasar los tests

```bash
dotnet test ManagedIdentity.Demo.slnx
```

Resultado esperado: **35 pass · 0 skip · 0 fail**. Ni con Docker ni sin Docker cambia: no hay capa de integración (sección 10).

- **CAPA 1 (unit)** — cada combinación de las tres clases puras.
- **CAPA 0 (DI container)** — incluye `Assert.Same(credencial, credencialDelCliente)`, que es la verificación literal de la slide 21.

Sin Azure, sin emulador, sin red. La suite siempre verde, sin condiciones.

### 11.7 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| Arranque lento de las llamadas a Azure en local | `Azure:LocalDev` no está en true | comprueba `appsettings.Development.json`; ya viene a `true` (sección 5) |
| `/blob/contenedores` devuelve 503 | falta `StorageBlobEndpoint` configurado | es esperado; configura el endpoint si quieres probar la demo real |
| `/blob/contenedores` da `Forbidden` | tu `az login` no tiene rol en ese Storage | asigna `Storage Blob Data Reader` a tu usuario (sección 11.5) |
| `/blob/contenedores` da `Unauthorized` con MI | la MI no está habilitada o no tiene el rol | en Portal: habilita Identity → asigna rol mínimo |
| Tests rojos sin Docker | no debería pasar (no hay integración) | revisa la cadena de compilación; `dotnet test` tiene que dar 35/0/0 |

### 11.8 Despliegue por Portal (resumen)

El detalle de despliegue por **Portal** (habilitar MI System-Assigned, asignar rol mínimo, App Setting con solo la URL, autenticación Entra-only en SQL, asignación Cosmos con `az cosmosdb sql role assignment`) está en el [`README.md`](README.md). Este manual no lo repite — para tocar Azure, el README es la referencia.

---

## 12. Checklist de producción (y de qué te protege cada línea)

| Casilla | De qué te protege |
| --- | --- |
| Managed Identity habilitada en la app (App Service / Function) | Tener que guardar passwords en config |
| App Setting con solo la URL del recurso (`__blobServiceUri`, `__accountEndpoint`, etc.) | Que aparezca una connection string con key en variables de entorno |
| Rol RBAC de **plano de datos** (no Owner/Contributor) | Que la app pueda borrar el recurso o reasignar permisos |
| Scope mínimo (a la cuenta concreta, no a la suscripción) | Que la app acceda a recursos que no son suyos |
| Una sola `TokenCredential` registrada como singleton | Token thrashing, caches duplicadas, 429 contra Entra ID |
| `Azure:LocalDev` en `appsettings.Development.json` | Timeouts de IMDS de 10 s en cada llamada local |
| Lo que no soporta MI (APIs externas) en Key Vault con referencia | Tener "una key suelta" para mantener |
| Conditional Access activado en la MI para escenarios sensibles | Acceso fuera de tu red corporativa con identidad robada |
| Diagnostic logs de Entra ID activados | No saber quién (o qué identidad) ha estado pidiendo tokens |
| Auditing del recurso destino activado | No saber qué hizo cada MI con su acceso |

---

## 13. Ideas para llevarte

Lo principal: la mayor parte de los incidentes de seguridad con secretos en Azure no son ataques sofisticados, son **descuidos**. Una key comiteada por error. Un App Setting copiado a Slack. Un becario que se fue hace dos años con un appsettings completo. Managed Identity no resuelve todos los problemas de seguridad — resuelve el más común y más caro, que es el de los secretos vivos que nadie rota. Si tienes que elegir una sola medida de defensa para tu proyecto Azure, esta es la que mejor relación coste-beneficio te da.

Una recomendación honesta: **System-Assigned por defecto**. Es lo más simple, vinculado 1:1 con el recurso, sin gestión adicional. User-Assigned (UAMI) tiene su sitio —cuando varios recursos comparten identidad o necesitas persistencia entre redeploys—, pero añade un recurso más que gestionar. Empieza con System-Assigned y migra a UAMI el día que tengas un caso de uso claro, no antes.

Sobre los **roles**, la regla de oro de la slide 27 cabe en un post-it: `MI = WHO, RBAC = WHAT, Conditional Access = WHEN/WHERE`. Si te paras a pensar cuál es el WHAT mínimo necesario antes de asignar, evitas el incidente más común: el "le doy Contributor para no perder tiempo y luego lo afino". Spoiler — nadie lo afina después. La primera vez es la que cuenta.

Y para lo que **no se puede MI**: Key Vault Reference desde App Settings. Es la pieza que cierra el círculo. APIs de terceros, claves de SaaS, tokens de servicios que no hablan Entra ID — todos pueden ir a Key Vault y la app accede con su MI. Sigues sin tener secretos vivos en tu configuración. Solo URLs y referencias.

---

## 14. Comprueba que lo has entendido

Sin mirar atrás. Si dudas, vuelve a la sección.

1. ¿Por qué `DefaultAzureCredential` se registra como singleton y compartido por todos los clientes? ¿Qué pasa si creas una credencial nueva por cliente? *(sección 6)*
2. Una connection string con `Authentication=Active Directory Default` y otra con `@Microsoft.KeyVault(...)`. ¿Cuál marca `ConnectionSecretScanner` como segura y por qué? *(sección 7)*
3. Te piden dar acceso a una MI para que escriba en Blob. ¿Qué rol asignas? ¿Por qué nunca `Storage Account Contributor`? *(sección 8)*
4. ¿Por qué `Azure:LocalDev = true` en `appsettings.Development.json` y qué pasa si lo quitas en tu portátil? *(sección 5)*
5. El ejemplo no tiene capa de integración con `SkippableFact`. Justifica por qué eso es lo correcto aquí (y no era lo correcto en S5.1/S5.2/S5.3). *(sección 10)*
6. ¿Cuál es la diferencia operativa entre System-Assigned y User-Assigned Managed Identity? ¿Cuándo elegirías cada una? *(sección 13)*
7. Un equipo tiene la storage key en su `appsettings.json` desde hace tres años. ¿Por qué es un problema independientemente de que el repo sea privado? *(sección 2)*

<details>
<summary>Respuestas</summary>

1. Porque `DefaultAzureCredential` cachea internamente los tokens. Una credencial singleton compartida reutiliza la caché y solo pide un token nuevo cuando el actual va a vencer. Crear credenciales por cliente significa cachés vacías por cada uno, pedir token en cada construcción, latencia añadida y picos contra Entra ID. Es la slide 21 — y el `DiContainer_Tests` lo verifica con un `Assert.Same` literal.
2. Las dos. La primera porque no contiene patrones de secreto (`password=`, `accountkey=`, `sig=`...) — usa Entra ID. La segunda porque empieza por `@Microsoft.KeyVault(`: el escáner reconoce que es una referencia y que el secreto real lo resuelve App Service contra Key Vault con su MI, no vive en el App Setting.
3. **`Storage Blob Data Contributor`** (o `Owner` si necesita gestionar ACLs). `Storage Account Contributor` es plano de control: permite cambiar la configuración del Storage, generar nuevas keys, asignar permisos a otros. Para escribir blobs no necesitas nada de eso. Es el anti-patrón 3 de la slide 27.
4. Para saltar el intento de Managed Identity desde local. Sin el flag, `DefaultAzureCredential` intenta contactar con el endpoint IMDS (`169.254.169.254`), que solo existe dentro de Azure. En local espera el timeout (5-10 segundos) y *después* prueba `az login`. Si lo quitas, cada primera llamada del día tarda mucho más, y tu aplicación parece colgada cuando en realidad está esperando un timeout. Pequeño detalle, gran cambio en la experiencia de desarrollo.
5. Porque Managed Identity / Entra ID **no se pueden emular** sin Azure real. Azurite usa una key fija pública; el emulador de Cosmos lo mismo. No existe un "emulador de Entra ID" que firme tokens válidos. Un test de integración exigiría una suscripción real y `az login`, lo que ataría `dotnet test` a credenciales personales. En S5.1/S5.2/S5.3 sí había emulador (Azurite / Testcontainers.MsSql / Cosmos Emulator) → integración con `SkippableFact` tiene sentido. Aquí no hay emulador → mejor reconocer que falta la capa que fingir cobertura inútil.
6. **System-Assigned** se crea automáticamente al habilitarla en el recurso, está vinculada 1:1, y se borra cuando borras el recurso. Más simple, ideal para el 90% de casos. **User-Assigned** se crea como recurso independiente y se asigna a uno o varios recursos; persiste aunque el recurso original se borre. Útil cuando múltiples recursos necesitan la misma identidad —p. ej., varias apps que acceden al mismo Cosmos— o cuando la identidad tiene que sobrevivir a redeploys.
7. Porque "privado" no significa "secreto". El historial de git guarda la key para siempre. Cualquiera con acceso al repo —incluidos exempleados que olvidaron quitar sus permisos, o atacantes que comprometen una cuenta de developer— la ve. Y aunque la key se cambiara hoy, el `git log` siguiría llevando la antigua: si esa key se reutilizó como base de otra, o si alguien dejó una copia en su disco local, el incidente sigue ahí. La única defensa real es no tener la key en absoluto. Por eso este submódulo existe.

</details>

---

## 15. Hasta aquí

Vuelve al edificio de los dos modelos de acceso de la sección 4. Llaves físicas frente a pases electrónicos. Tres años con la misma llave en un cajón frente a un sistema que registra cada entrada y revoca acceso al instante. Esa decisión, repetida a escala de Azure, es la diferencia entre un proyecto que pasa una auditoría y uno que no. Y cuando llega el día de la auditoría, no hay tiempo de empezar a quitar keys.

S5.5 cierra M05 con la otra cara de la moneda de la seguridad de datos: **qué pasa cuando las cosas se rompen**. Backups automáticos, point-in-time restore, replicación geo-redundante, planes de recuperación ante desastres. No para evitar incidentes, sino para que cuando ocurran —y van a ocurrir— tengas la operación "restaurar a las 14:59 del lunes" como una conversación de cinco minutos en lugar de una semana de pánico.
