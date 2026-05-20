# Manual del alumno — S2.4 · Variables, connection strings y configuración segura

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: pasos por Portal, scripts `az`, mapeo a slides, lista exacta de App Settings. Este manual va antes: te cuenta por qué hasta ahora la configuración era "inocente" y por qué a partir de aquí cambia.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M02-S2.4](../../../doc/M02-App-Services/v4-actual/M02-S2.4-variables-conexion-config-segura-v4.md). Mismo plan S1 que S2.3, ahora con secretos de verdad (`ApiKey`, `ConnectionString`) y la pieza que cierra "App Service en serio": **Key Vault references**.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Hasta S2.3 las settings eran inocentes: greetings, versiones, umbrales, listas de orígenes. Si una de esas se filtra en un repo, hay risas y un commit de corrección. A partir de S2.4 entran las settings de verdad: `ApiKey`, `ConnectionString` con `Password=...`. Si una de estas se filtra, es un incidente de seguridad. La práctica te enseña la disciplina mínima para tratarlas: validación al arrancar, scrubbing en respuestas y logs, y sobre todo el patrón que cambia el juego — **Key Vault references**, secretos que viven en Key Vault y la app los lee con Managed Identity, sin pasar nunca por un App Setting visible.

Si te llevas una sola cosa: **App Settings con secretos en claro son una bomba de relojería**. Key Vault references son la salida estándar, y en App Service están a tres clics de distancia.

---

## 2. El problema real que hay detrás

Una empresa pidió a un proveedor externo hacer un quick fix sobre su API. Le mandaron el repo. El developer del proveedor abrió `appsettings.json`, vio el `ConnectionString` completo con su `Password=Prod1234!;` y se lo guardó "por si acaso lo necesito". Tres meses después, ese developer ya no trabajaba en el proveedor. La connection string seguía intacta en producción. La rotación de claves de la empresa era anual, así que el password seguía valiendo. Nada catastrófico ocurrió — que sepamos. Pero el riesgo estaba ahí, gratuito, durante meses.

La autopsia no es técnica, es **disciplina**: la connection string nunca debió estar en `appsettings.json`. Debió estar en Key Vault, referenciada desde App Settings con `@Microsoft.KeyVault(...)`, leída por la app a través de su Managed Identity. El día que el developer se fuera (o el repo se filtrase, o alguien copiase el JSON a una conversación de Slack), el secreto no estaría ahí.

Esta práctica entrena esa disciplina con cuatro piezas:

| Pieza | Para qué | Dónde la verás |
| --- | --- | --- |
| **Options con validación al arrancar** | La app no arranca si la config es inválida o si una Key Vault reference no se resolvió | [`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) + [`AppOptionsValidator.cs`](src/AppService.Demo.Api/Configuration/AppOptionsValidator.cs) |
| **Scrubbing por nombre de clave** | Ningún log, ningún endpoint expone valores sensibles | [`ConfigScrubber.cs`](src/AppService.Demo.Api/Configuration/ConfigScrubber.cs) + `/config` + `/info` |
| **Inspección segura de connection string** | Saber a qué DB conectas sin filtrar el password | [`ConnectionStringInspector.cs`](src/AppService.Demo.Api/Configuration/ConnectionStringInspector.cs) + `/connection` |
| **Key Vault references** | El secreto vive en Key Vault, no en App Settings | App Setting con `@Microsoft.KeyVault(VaultName=...;SecretName=...)` |

Y para verificar todo el tinglado, **`/secrets/api-key/check`** devuelve metadatos del secreto (longitud, fingerprint SHA-256, origen) **pero nunca el valor**. Eso te permite confirmar "el secreto está bien cargado, viene de Key Vault, tiene 32 chars" sin filtrarlo en la respuesta.

---

## 3. Por qué esto importa en tu stack

Hay tres niveles de seriedad cuando hablamos de secretos en config, y la práctica te lleva por los tres:

1. **Nivel "demo de aula"**: el secreto está en `appsettings.json`, commiteado, conocido por todos. No pasa nada porque nadie lo usa, pero genera hábito malo.
2. **Nivel "primer proyecto en producción"**: el secreto está en App Settings del portal. Mejor que el JSON commiteado, pero cualquier persona con permiso de *Contributor* en la suscripción lo ve.
3. **Nivel "App Service en serio"**: el secreto vive en Key Vault, App Settings tiene solo una **referencia** (`@Microsoft.KeyVault(...)`). Para verlo hay que ir explícitamente a Key Vault y tener rol *Key Vault Secrets User*. La separación de privilegios es real.

Esta práctica te lleva del 1 al 3 con un paso intermedio (el 2) que se ve en `03-configure-app-settings.sh`. Y la diferencia entre el 2 y el 3 son tres clics y un rol asignado, no un proyecto entero. Es uno de los retornos de inversión más altos de Azure.

Cambio respecto a S2.3: misma infraestructura (RG, plan S1, web app), añades **un Key Vault** en el mismo RG y le das a la web app el rol *Key Vault Secrets User* sobre ese vault. El resto es configurar App Settings con la sintaxis `@Microsoft.KeyVault(...)`.

---

## 4. El modelo mental: el mayordomo y el cofre

Imagina una casa con un cofre cerrado en el sótano. Dentro del cofre están los documentos importantes — la escritura, los testamentos, los seguros, los pasaportes. La llave del cofre no la tiene el dueño de la casa. La tiene un **mayordomo de confianza** contratado para custodiarla. Cuando el dueño necesita un documento, se lo pide al mayordomo. El mayordomo baja al sótano, abre el cofre, saca el documento, se lo da. Lo guarda otra vez. Cierra. Sube.

Si un día el dueño deja la casa abierta y entra alguien, ese alguien revisa los cajones del dueño y encuentra... agendas, papeles del día a día, pero **no los documentos importantes**. Los documentos están en el cofre, custodiado por el mayordomo, en el sótano.

Esa es exactamente la arquitectura de **Key Vault + Managed Identity + Key Vault references**.

```
                  App Service (la casa del dueño)
                  ┌─────────────────────────────────────────┐
                  │ App Settings (los cajones visibles)     │
                  │ ├── AppOptions__Greeting = "Hola"       │
                  │ ├── AppOptions__ApiKey =                │
                  │ │   @Microsoft.KeyVault(                │  ← la nota que dice
                  │ │     VaultName=kv;SecretName=ApiKey)   │     "el dueño del cofre"
                  │ └── AppOptions__ConnectionString =      │
                  │     @Microsoft.KeyVault(...)            │
                  │                                          │
                  │ Managed Identity (el mayordomo)         │
                  │   • principalId: abc-123                │
                  │   • rol: Key Vault Secrets User         │
                  └─────────────────────────────────────────┘
                                  │
                  (el mayordomo pide al cofre con su identidad)
                                  ▼
                  Key Vault (el cofre en el sótano)
                  ┌─────────────────────────────────────────┐
                  │ Secrets/                                 │
                  │   ├── ApiKey = "real-32-chars-secret"   │
                  │   └── ConnectionString = "Server=...;"  │
                  └─────────────────────────────────────────┘
```

Tres frases para fijar el modelo:

- **App Settings tiene una referencia, no el secreto.** Lo que ve un *Contributor* curioso es `@Microsoft.KeyVault(VaultName=kv;SecretName=ApiKey)`. Sabe que hay un secreto, sabe dónde vive, pero no lo lee.
- **La identidad de la app abre el cofre.** App Service tiene una *System-Assigned Managed Identity*; le asignas el rol *Key Vault Secrets User* sobre tu vault. Cuando arranca, App Service llama a Key Vault usando esa identidad, resuelve la referencia y la inyecta como variable de entorno normal en el proceso. **Tu código no toca Key Vault.** Para tu código sigue siendo `Configuration["AppOptions:ApiKey"]`.
- **El secreto se rota sin tocar la app.** En Key Vault haces "New Version" del secreto. App Service tiene cache (~5-10 minutos por defecto), tras refrescar la app lee el nuevo valor. Sin redeploy, sin reinicio manual, sin tocar el código.

Vuelve a esta imagen cuando dudes "¿esto en App Settings o en Key Vault?". La regla: si verlo en claro sería un incidente, va al cofre.

---

## 5. Validar al arrancar (y por qué Key Vault references mal configuradas se cazan ahí)

[`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) tiene atributos estándar:

```csharp
[Required(AllowEmptyStrings = false)] public string Greeting { get; init; }
[Required] public string ConnectionString { get; init; }
[Required] public string ApiKey { get; init; }
[Range(1, 60)] public int RequestTimeoutSeconds { get; init; }
[Url] public string ExternalApiBaseUrl { get; init; }
```

Y `Program.cs` lo registra con `ValidateDataAnnotations().ValidateOnStart()` (el patrón que aprendiste en S2.1). Pero hay validaciones cross-field que las DataAnnotations no expresan, y por eso aparece [`AppOptionsValidator.cs`](src/AppService.Demo.Api/Configuration/AppOptionsValidator.cs) — un `IValidateOptions<AppOptions>` con tres reglas que **DataAnnotations no puede capturar**:

1. **`ApiKey.Length < 8`** → falla. Un secreto corto es muchas veces el placeholder que se quedó sin sustituir.
2. **`ConnectionString` contiene `Password=` pero no `Encrypt=true`** → falla. Conectar a SQL sin cifrado en producción es bug serio; el validador lo detecta antes de que la app sirva una sola petición.
3. **`ApiKey` empieza por `@Microsoft.KeyVault`** → falla con mensaje específico. Esto detecta el caso **"la Key Vault reference no se resolvió"**: el valor literal del App Setting llegó a tu código en lugar del secreto real. Causa típica: a la MI le falta el rol, o el secret no existe en KV, o el `VaultName` está mal escrito. El error claro te ahorra horas de pensar "¿por qué la API rechaza mi clave si la puse bien?".

> 🧠 **La validación al arrancar como red de seguridad.** Una de las maneras más sutiles en las que un sistema de Key Vault references falla es **silenciosamente**: por algún motivo App Service no resolvió la referencia y dejó el valor literal `@Microsoft.KeyVault(...)` como string. Tu app arranca, lee `Configuration["AppOptions:ApiKey"]`, y manda esa cadena literal a la API externa. La API rechaza la clave. El log dice "401 Unauthorized". Tú piensas que la clave es incorrecta cuando lo que pasa es que **tu MI no tiene el rol sobre el vault**. La regla 3 del validador caza este caso antes de servir nada. Es una de las cosas más útiles de tener desde el día uno.

---

## 6. Scrubbing: ningún secreto sale por el endpoint equivocado

[`ConfigScrubber.cs`](src/AppService.Demo.Api/Configuration/ConfigScrubber.cs) tiene una lista cerrada de tokens sensibles:

```csharp
private static readonly string[] SensibleTokens = [
    "password", "secret", "key", "token", "connectionstring", "credential"
];
```

Cualquier clave de configuración cuyo nombre contenga alguno de esos tokens (case-insensitive) se devuelve como `***REDACTED***`. Las claves no sensibles pasan intactas:

```csharp
ConfigScrubber.Scrub("AppOptions:ApiKey", "real-value")   // → "***REDACTED***"
ConfigScrubber.Scrub("AppOptions:Greeting", "hola")        // → "hola"
```

¿Por qué el filtro es por nombre de clave y no por contenido? Porque buscar patrones en valores (regex de connection strings, detección de formatos de API keys) es frágil y siempre se equivoca: o pasa de largo un secret válido con formato raro, o redacta un valor inocente que casualmente parece un secret. **Por nombre es robusto**: si la clave se llama `ApiKey`, `ConnectionString`, `CustomerSecret`, `OAuthToken`, sale redactada. El que pone el nombre decide qué es sensible.

El endpoint `/config` devuelve **toda la configuración** del proceso con scrubbing aplicado. Útil para debugging en producción sin riesgo: ves qué claves están cargadas, qué valores tienen las no sensibles, y las sensibles aparecen como `***REDACTED***`. Si querías ver un valor sensible, no se filtra; tienes que ir al sitio correcto (Key Vault o User Secrets) con tus credenciales.

Y `/info` (heredado de S2.1) aplica el mismo scrubber a su respuesta. La diferencia con S2.1: ahora `connectionString` y `apiKey` aparecen en `appOptions` pero salen redactados. Antes no aparecían porque no eran sensibles; ahora aparecen pero se ocultan. Es la pieza de información operativa (sé que están cargados) sin la pieza peligrosa (no veo el valor).

---

## 7. Inspección segura de connection strings

[`ConnectionStringInspector.cs`](src/AppService.Demo.Api/Configuration/ConnectionStringInspector.cs) hace algo distinto al scrubber: en lugar de redactar la cadena entera, **extrae solo los campos seguros**. El endpoint `/connection` devuelve:

```json
{
  "isPresent": true,
  "server": "tcp:demo-sql.database.windows.net,1433",
  "database": "demo",
  "encrypt": true,
  "trustServerCertificate": false,
  "multipleActiveResultSets": false,
  "isKeyVaultReferenceLiteral": false
}
```

Los campos `Password`, `User ID`, `Authentication` se ignoran a propósito. Lo que devuelve es exactamente lo que necesitas saber para diagnosticar problemas de conexión: a qué servidor apunta, qué DB, si va cifrada. No filtrar el usuario ni el password es deliberado — son los valores que querrías reemplazar al rotar, y no hay que verlos para diagnosticar.

El flag `isKeyVaultReferenceLiteral` es la otra pieza didáctica: detecta si el valor literal empieza por `@Microsoft.KeyVault`, lo que significa que **App Service no resolvió la referencia**. Lo que vería tu código es una cadena literal en lugar de la connection string real, y la conexión a SQL fallaría con un mensaje confuso. El flag lo enciende un check explícito, así sabes inmediatamente si el problema es "no se resuelve la referencia" o "la referencia se resolvió pero la cadena está mal".

---

## 8. Verificar un secret sin filtrarlo

El endpoint `/secrets/api-key/check` es uno de los más útiles del ejemplo. No devuelve el secret. Devuelve **metadatos verificables**:

```json
{
  "isPresent": true,
  "length": 32,
  "fingerprint": "a4f2c8e1",
  "source": "explicit"
}
```

- **`isPresent`**: el secret está cargado.
- **`length`**: cuántos chars tiene. Si esperabas 32 y te aparece 22, hay algo raro.
- **`fingerprint`**: SHA-256 del secret, truncado a los primeros 8 chars. Te permite **comparar** sin revelar: si rotas el secret y el fingerprint cambia, sabes que el cambio se propagó. Si dos entornos deberían tener el mismo secret y los fingerprints son distintos, hay desalineación.
- **`source`**: `default-appsettings` (el placeholder de `appsettings.json`), `key-vault-reference-unresolved` (la referencia no se resolvió), o `explicit` (el secret real, viene de KV o de User Secrets).

> 🧠 **El truco del fingerprint.** SHA-256 truncado no revela el secret pero permite confirmaciones operativas: "rotamos la clave hace cinco minutos; el fingerprint en producción debería empezar por `f8a2`". Si lo es, el cambio se aplicó. Si no, la cache de App Service aún no refrescó y conviene reiniciar manualmente. Esto convierte la verificación de un cambio en algo objetivo en lugar de "espero que funcione".

Y la lección de fondo: **ningún endpoint debería devolver un secret jamás, ni siquiera por accidente**. La forma de saber que un secret está bien cargado es a través de sus metadatos. Si descubres un endpoint que devuelve secretos en claro en producción, tienes un incidente, incluso si "es solo para debugging".

---

## 9. Recorrido guiado

Tres fases: local (con User Secrets), Azure sin Key Vault (App Settings en claro), Azure con Key Vault references.

### Local con User Secrets (slide 20)

```bash
cd src/AppService.Demo.Api
dotnet user-secrets init
dotnet user-secrets set "AppOptions:ApiKey"           "mi-clave-de-dev-32-chars"
dotnet user-secrets set "AppOptions:ConnectionString" "Server=localhost;Database=demo;Encrypt=true"
```

Los User Secrets viven en `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>` (fuera del repo). Solo se cargan en `Development`. La precedencia es: `appsettings.json` < `appsettings.Development.json` < User Secrets < env vars. En Azure ignora los User Secrets — usa los App Settings/Key Vault references.

| # | Petición | Respuesta | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `GET /config` | JSON con claves; `AppOptions:ApiKey` y `:ConnectionString` aparecen como `***REDACTED***` | Scrubbing en endpoints (sección 6). |
| 2 | Local: `GET /connection` | `{ server: "localhost", database: "demo", encrypt: true, isKeyVaultReferenceLiteral: false }` | Inspector sin password (sección 7). |
| 3 | Local: `GET /secrets/api-key/check` | `{ length: 23, fingerprint: "...", source: "explicit" }` | Verificación sin filtrar (sección 8). |

### Azure con Key Vault references

| # | Acción | Verificación |
| --- | --- | --- |
| 4 | Crea Key Vault con RBAC, asigna *Key Vault Secrets User* a la MI de la web app | la web app puede leer secrets de ese KV |
| 5 | Crea secrets `ApiKey` y `ConnectionString` en el vault | `kv.secrets.list` muestra los dos |
| 6 | Configura App Settings con `@Microsoft.KeyVault(VaultName=kv;SecretName=ApiKey)` (lo mismo para ConnectionString) | en *Configuration → Application settings*, columna *Source* dice "Key Vault Reference (Healthy)" |
| 7 | Deploy + `curl /config` | `AppOptions:ApiKey` aparece como `***REDACTED***` pero está presente |
| 8 | `curl /secrets/api-key/check` | `source: "explicit"`, `length` = la longitud del secret real, `fingerprint` válido |
| 9 | Rota el secret en KV (Generate/Import → New Version) | espera 5-10 min (cache) o reinicia la app; `/secrets/api-key/check` muestra el nuevo `fingerprint` |
| 10 | A propósito, quita el rol "Key Vault Secrets User" a la MI y reinicia | la app **no arranca** — `AppOptionsValidator` caza la referencia no resuelta con mensaje claro (sección 5) |

El paso 10 es el más didáctico: en producción real, ese mensaje en *Log stream* es lo que te ahorra "horas buscando por qué la API responde 401" — el problema no es la API, es que tu app está mandando el literal `@Microsoft.KeyVault(...)` como si fuera el ApiKey.

---

## 10. Tests: la separación entre Unit y Integration aparece

Cuarenta y un tests, primer ejemplo de M02 con separación clara:

- **Unit tests** sin host (rápidos, sin `WebApplicationFactory`):
  - `ConfigScrubberTests` (10) — claves sensibles vs no sensibles, helper `ScrubAll`, valores null/vacíos.
  - `ConnectionStringInspectorTests` (3) — extracción de campos seguros, ignorar password, soportar `Data Source`/`Initial Catalog`.
  - `AppOptionsValidatorTests` (4) — baseline válido, ApiKey corto, Password sin Encrypt, KV ref no resuelta.

- **Integration tests** con `WebApplicationFactory<Program>`:
  - Todos los heredados de S2.3 (health, hello, info, version, warmup, load, cors, static).
  - `ConfigEndpointTests` (3) — `/config` redacta sensibles, `/connection` muestra Server/Database, `/connection` detecta KV ref literal.
  - `FeatureFlagEndpointTests` (2) — feature OFF → payload v1, feature ON → payload v2.
  - `SecretsEndpointTests` (1) — `/secrets/api-key/check` devuelve metadatos pero **nunca** el valor del secret.

La separación importa porque las unit tests cubren la lógica pura (los tres "advisors" / "inspectors" / "validators") y corren en milisegundos. Las integration tests cubren el contrato HTTP completo. Esa separación es el patrón que vas a ver intensificado en M03 y siguientes.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| Suscripción Azure con plan Standard S1 | desplegar | Sí (si vas a desplegar) |
| Permisos para asignar roles RBAC | dar a la MI el rol *Key Vault Secrets User* | Sí (para Key Vault) |
| `az` CLI, `jq`, `openssl` | scripts y verificación con fingerprints | Solo si usas scripts |

### 11.2 Compilar y arrancar en local

```bash
cd examples/M02-App-Services/S2.4-variables-conexion-config-segura
dotnet build AppService.Demo.Config.slnx          # 0 errores

# Configura los secrets locales antes de arrancar (o la validación falla)
cd src/AppService.Demo.Api
dotnet user-secrets init
dotnet user-secrets set "AppOptions:ApiKey"           "mi-clave-de-dev-32-chars"
dotnet user-secrets set "AppOptions:ConnectionString" "Server=localhost;Database=demo;Encrypt=true"

dotnet run --launch-profile http
# → http://localhost:5080
```

Si la app no arranca: el validador está cazando algo. Lee `Log stream` o el output de `dotnet run` — te dice exactamente qué regla falló.

### 11.3 Pasar los tests

```bash
dotnet test
```

Resultado: **41 pass · 0 fail**. Sin Azure, sin Docker.

### 11.4 Desplegar a Azure con Key Vault references (resumen)

El detalle está en el [`README.md`](README.md). Pasos clave:

1. **RG + plan S1 + Web App** (mismo patrón que S2.3).
2. **Crear Key Vault** con *Permission model: Azure RBAC*.
3. **Habilitar System-Assigned MI** en la web app; copiar el principal ID.
4. **En el Key Vault → IAM**, asignar rol *Key Vault Secrets User* a ese principal ID.
5. **Crear los secrets** `ApiKey` y `ConnectionString` en el vault.
6. **Configurar App Settings** con `@Microsoft.KeyVault(VaultName=<kv>;SecretName=ApiKey)` y lo mismo para ConnectionString.
7. **Deploy** desde VS Code.
8. **Verificar** con `/secrets/api-key/check` y `/config`.

### 11.5 Scripts `az` (recomendado para escenificar)

```bash
cd scripts
cp .env.demo.example .env.demo
bash demo.sh           # menú interactivo con los 8 pasos
```

El menú lleva los pasos en orden y permite **escenificar fallos a propósito** (quitar el rol y ver que la app no arranca) que son lo que más enseña.

### 11.6 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| La app no arranca: "Key Vault reference not resolved" | la MI no tiene rol *Key Vault Secrets User* | asígnalo en *Key Vault → IAM* y reinicia la app |
| `/config` muestra `AppOptions:ApiKey: "***REDACTED***"` pero la app falla con 401 contra una API externa | la referencia está bien (length > 0) pero el secret es incorrecto | rota el secret en KV con el valor correcto |
| El fingerprint no cambia tras rotar | cache de App Service (5-10 min) | espera o reinicia la app desde el portal |
| La columna *Source* en App Settings dice "Key Vault Reference (Not resolved)" | falta el rol, el secret no existe, el VaultName está mal | verifica los tres uno por uno |
| `/secrets/api-key/check` dice `source: "key-vault-reference-unresolved"` | el literal `@Microsoft.KeyVault(...)` llegó al código | mismo diagnóstico que arriba: rol, secret, VaultName |
| User Secrets no se carga en local | falta `<UserSecretsId>` en el csproj, o estás en `Production` | `dotnet user-secrets init` para añadirlo; confirma que el entorno es `Development` |

### 11.7 Limpieza

`Portal → Resource groups → rg-curso-m02-s24 → Delete`. Si quieres recrear el KV con el mismo nombre, hace falta **purge** después del borrado (Key Vault tiene soft delete por defecto): *Key vaults → Manage deleted vaults → Purge*.

---

## 12. Ideas para llevarte

Lo más útil de esta práctica no es Key Vault en sí — es la **disciplina de tres capas**: el secret nunca está en el repo (User Secrets para dev), nunca está en App Settings en claro (Key Vault references en Azure), nunca se filtra por un endpoint (scrubber por nombre de clave). Las tres se aplican el mismo día que empiezas un proyecto, no "cuando llegue el momento". El día que se filtre un secret porque "todavía no habíamos llegado a Key Vault" es el día del incidente.

Sobre **Key Vault references**: aunque tu proyecto sea pequeño, mete Key Vault desde el día uno. El coste de un vault es prácticamente cero (~0,03 € por 10.000 transacciones). El coste de no tenerlo cuando se filtra una clave es un incidente. Tres clics, un rol, dos App Settings con la sintaxis `@Microsoft.KeyVault(...)`, y ya no tienes secretos en claro en tu infraestructura.

Sobre **validación al arrancar**: cuanto más estricta la validación, más rápido se cazan los errores de configuración. Las tres reglas cross-field del ejemplo (`ApiKey.Length < 8`, `Password` sin `Encrypt`, KV ref no resuelta) son el tipo de cosas que sin validar se cazan en producción a las 5 de la tarde de un viernes. Una clase de 40 líneas implementando `IValidateOptions<T>` te ahorra esos viernes.

Y un consejo pragmático sobre el **scrubber**: añade tu propia lista de tokens si tu dominio tiene secretos con nombres no estándar (`oauth_id`, `signing_cert`, `webhook_url` si es privado). La lista por defecto cubre los obvios; ampliarla es trivial y bien gastado.

---

## 13. Comprueba que lo has entendido

1. ¿Por qué `appsettings.json` con `Password=...` es peor que App Settings en el portal? ¿Y por qué App Settings es peor que Key Vault references? *(sección 3)*
2. La app no arranca, `Log stream` dice `"AppOptions.ApiKey starts with @Microsoft.KeyVault — reference not resolved"`. ¿Qué pasa y en qué orden lo diagnosticas? *(secciones 5, 11.6)*
3. ¿Por qué el scrubber redacta por **nombre de clave** y no por **contenido del valor**? *(sección 6)*
4. ¿Para qué sirve el `fingerprint` del endpoint `/secrets/api-key/check`? Da un caso de uso concreto. *(sección 8)*
5. Rotas el secret `ApiKey` en Key Vault. ¿Cuánto tarda en propagarse a tu app? ¿Cómo forzarlo si tienes prisa? *(sección 4)*
6. ¿Qué diferencia hay entre `IOptions<T>`, `IOptionsSnapshot<T>` e `IOptionsMonitor<T>`? ¿Por qué este ejemplo usa el primero? *(sección "Tour del código" en README)*

<details>
<summary>Respuestas</summary>

1. `appsettings.json` con secretos en el repo es lo peor: cualquiera con acceso al repo lo ve, queda en el historial de git para siempre, se filtra a developers externos. App Settings del portal es mejor (solo lo ven los que tienen *Reader+* en la suscripción) pero sigue siendo visible para cualquier *Contributor*. **Key Vault references** son la mejor separación: el App Setting solo dice "consulta el cofre", el cofre tiene un sistema de roles separado (*Key Vault Secrets User* lo lee; *Contributor* en la suscripción **no**). La separación de privilegios es real: para ver el secret tienes que tener rol específico sobre el vault, y eso aparece en logs de auditoría.
2. El literal `@Microsoft.KeyVault(...)` llegó a tu código en lugar del secret real. Diagnóstico en orden: **(a)** verifica que la MI de la web app está habilitada (*Identity → System assigned: On*); **(b)** verifica que esa MI tiene rol *Key Vault Secrets User* sobre el vault (*Key Vault → IAM*); **(c)** verifica que el secret existe con ese nombre exacto (case-sensitive) en el vault; **(d)** verifica que el `VaultName` en la App Setting coincide con el nombre real del vault. Causa típica: la MI se olvidó de asignar el rol o el secret tiene un nombre con typo.
3. Porque buscar patrones por valor es frágil. Una regex de "detecta connection strings" deja pasar las que no encajen exactamente y rompe los logs cuando un valor inocente casualmente parece un secret. Por nombre de clave es objetivo: el que pone el nombre decide qué es sensible. Si la clave se llama `ApiKey`, el valor se redacta; si se llama `Greeting`, el valor pasa. Robusto, predecible, fácil de extender (añade tokens a la lista).
4. SHA-256 del secret truncado a 8 chars. **No revela** el secret pero permite **comparar** sin tenerlo en claro. Caso de uso: rotas la clave en Key Vault. Apuntas el fingerprint que devuelve `/secrets/api-key/check` antes y después. Si cambia, la rotación se propagó a la app. Si no, la cache de App Service aún no refrescó y conviene reiniciar manualmente. Convierte "espero que funcione" en una verificación objetiva.
5. App Service cachea las Key Vault references unos **5-10 minutos** por defecto. La forma de forzar refresh inmediato es reiniciar la app desde el portal (*Restart*) o hacer cualquier cambio en App Settings que dispare un reinicio. En sistemas con SLA estricto de rotación, conviene tener un runbook de rotación que incluya el reinicio explícito para garantizar propagación inmediata en lugar de esperar a la cache.
6. **`IOptions<T>`** lee la config al primer uso y la cachea para siempre (la vida del proceso). **`IOptionsSnapshot<T>`** relee la config en cada request (scoped). **`IOptionsMonitor<T>`** relee la config cuando cambia el provider subyacente y permite suscribirse a cambios. Este ejemplo usa `IOptions<T>` porque las opciones que maneja (connection string, ApiKey, feature flags básicos) **no cambian durante la vida de la app**: si cambian, App Service reinicia. Para configuración que cambia en runtime sin reinicio (típicamente con **Azure App Configuration**), `IOptionsMonitor<T>` es la opción correcta.

</details>

---

## 14. Hasta aquí

Vuelve a la imagen del mayordomo y el cofre de la sección 4. El dueño no tiene la llave; el mayordomo sí; el cofre vive aparte. Esa separación de privilegios — App Service no almacena el secret, Key Vault lo custodia, la MI hace de puente — es lo que vale el día que alguien quiere ver lo que hay en App Settings. Lo que ven es una nota que dice "está en el cofre", no el secret.

Lo siguiente es [`S2.5 — Monitorización y diagnóstico`](../S2.5-monitorizacion-diagnostico/MANUAL.md). Tu app ya está bien configurada, bien escalada y con secretos en su sitio. Falta lo que pasa cuando algo va mal **a pesar de todo**: cómo verlo, cómo diagnosticarlo, cómo alertarte antes de que el cliente se queje. Application Insights, Log Analytics, alertas de Azure Monitor, Live Metrics. La última pieza antes de las prácticas.
