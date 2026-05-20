# Manual del alumno — S6.6 · Azure Key Vault

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta cuándo se usa Key Vault y cuándo Managed Identity (no son lo mismo), cómo se referencia un secreto desde App Settings sin tocarlo en código, qué roles RBAC mínimos asignar a cada caso, y por qué la rotación automática con Event Grid es lo único razonable cuando hay decenas de secretos.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.6](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.6-key-vault-v3.md). Tres piezas de lógica pura (decisor MI/KV, parser de Key Vault Reference, política de rotación) más un planificador.

*Creado: 2026-05-20 18:10 +0200*

---

## 1. La idea en una frase

Key Vault es **el sitio donde van todos los secretos que no pueden ser Managed Identity**. Si Azure tiene una identidad para tu app y le puedes asignar RBAC sobre el recurso destino (Cosmos, SQL, Storage), no metas connection string ni client secret en ningún sitio: usa Managed Identity. Si el secreto es de un proveedor externo (Stripe, SendGrid, una API de terceros), o es un certificado SSL, o es una clave criptográfica de tu organización, va a Key Vault con el rol RBAC adecuado, y se referencia desde App Settings con la sintaxis `@Microsoft.KeyVault(...)`.

El submódulo modela cuatro decisiones: dónde guardar cada cosa, qué rol mínimo asignar, cómo construir/parsear la referencia, y cuándo toca rotar.

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen una y otra vez:

**Caso 1 — el client secret de Stripe en `appsettings.json`.** Un equipo metió la API key de producción de Stripe directamente en `appsettings.Production.json`. El archivo estaba en git (en una rama protegida, pero en git). Un colaborador externo hizo un clone, vio el archivo, y la conversación post-incidente fue larga. La solución correcta es: subir la API key a Key Vault como Secret; configurar la App Setting de App Service con valor `@Microsoft.KeyVault(VaultName=...;SecretName=stripe-api-key)`; App Service lo resuelve en runtime usando su Managed Identity contra el Key Vault. **El secreto nunca está en código, nunca en git, nunca en variables de entorno expuestas.**

**Caso 2 — el certificado SSL renovado a mano cada año.** Un servicio web tenía un certificado SSL para `*.empresa.com` que un sysadmin renovaba a mano cada doce meses descargando el .pfx, importándolo a IIS, reiniciando, etcétera. El sysadmin se cambió de empresa; el certificado caducó; el servicio cayó tres horas. La solución: subir el certificado a Key Vault como Certificate, configurar el App Service para usarlo desde el Key Vault, y activar **renovación automática** con la Certificate Authority correspondiente. Cero intervenciones manuales en lo sucesivo.

**Caso 3 — el rol "Key Vault Administrator" para todo el equipo.** Un equipo configuró Key Vault con RBAC, y el responsable técnico, por comodidad, asignó "Key Vault Administrator" a todos los devs. Auditoría de seguridad seis meses después: cualquiera podía leer todos los secretos, modificarlos, borrarlos. Tuvieron que reasignar a "Key Vault Secrets User" (solo lectura) y solo a "Key Vault Secrets Officer" a quien efectivamente tuviera que rotar. **La regla de mínimo privilegio aplica a Key Vault tanto como a cualquier otro recurso.**

Los tres casos se previenen con tres reglas: usar referencias desde App Settings (no leer Key Vault desde código si puedes evitarlo), automatizar renovaciones con Event Grid, asignar el rol mínimo necesario por tipo de operación.

---

## 3. Por qué esto importa en tu stack

Si tu sistema tiene cualquier secreto que no sea una connection string a recurso Azure (y todos los sistemas tienen al menos uno: una API key externa, un secret de App Reg, un certificado), Key Vault es donde va. Tres preguntas que conviene dejar zanjadas pronto:

- **¿Puedo evitar el secreto entero usando Managed Identity?** Es la primera pregunta. Si tu App Service llama a Cosmos, la respuesta es sí: Managed Identity del App Service + rol "Cosmos DB Built-in Data Contributor" sobre el container. Sin secreto, sin Key Vault.
- **Cuando el secreto es inevitable, ¿cómo lo manejo?** Subirlo a Key Vault, referenciarlo desde App Settings, dar el rol mínimo a la Managed Identity. Tu código lee `Environment.GetEnvironmentVariable("StripeApiKey")` sin saber que viene de Key Vault.
- **¿Cómo me entero antes de que caduque?** Event Grid con la suscripción `SecretNearExpiry` (30 días antes por defecto) dispara una función o un email a oncall. **Antes**, no después.

Si tienes estas tres respuestas, gestionar secretos en Azure es directo. Sin ellas, vas a tener incidentes evitables.

---

## 4. La analogía vertebradora: la caja fuerte del banco y las llaves bajo el felpudo

Imagina dos formas de gestionar las llaves de una casa:

**Forma A — La caja fuerte del banco (Key Vault)**:

- Tienes una caja fuerte en una sucursal del banco. El banco controla quién puede acceder con un sistema de roles bien definido:
  - **Quien puede ver la lista** de cajas y meter cosas: rol "Officer". Aquí asignas a los devops que gestionan los secretos.
  - **Quien solo puede sacar** lo que ya hay: rol "User". Aquí asignas a las aplicaciones que consumen los secretos.
  - **Quien gestiona todo el sistema** (crear cajas, configurar el banco): rol "Administrator". Aquí no asignes a casi nadie.
- Cuando una aplicación necesita un secreto, va al banco con su identificación corporativa (Managed Identity), el banco verifica que tiene rol "User", saca el secreto, lo entrega. La aplicación no guarda copia.
- El banco te avisa 30 días antes de que un objeto guardado caduque (Event Grid `SecretNearExpiry`).
- Si alguien intenta borrar una caja entera, el banco la pone en cuarentena 90 días antes de destruirla (purge protection). Si fue por error, puedes recuperarla.

**Forma B — La llave debajo del felpudo (secret en config)**:

- Pones la llave de casa debajo del felpudo. Es accesible para ti.
- También es accesible para cualquiera que pase por delante, mire el felpudo, encuentre la llave.
- Si se te pierde, no sabes quién la tiene ni cuándo la cogió.
- No sabes cuándo caduca la cerradura porque no hay sistema que te avise.

Lo mismo aplica a la diferencia entre **Managed Identity y secreto en Key Vault**:

- **Managed Identity** es como tener tu propia llave en el bolsillo, vinculada a tu identidad biométrica. Solo tú la puedes usar. No la pones bajo el felpudo. No la guardas en una caja fuerte. **No hay nada que rotar porque no hay nada que pueda perderse.** Es el ideal.
- **Key Vault** es la caja fuerte del banco para los secretos que **no pueden ser biométricos** — la llave del coche que prestas al servicio técnico, las llaves del local de tu socio, etcétera. Cosas que existen como objeto y tienen que estar guardadas en algún sitio que no sea tu bolsillo.

Y luego está la **referencia bajo el portero**:

- `@Microsoft.KeyVault(VaultName=...;SecretName=...)` es como dejar una nota en el cuadro de tu portería que dice "la llave de la caja 14 del banco X está reservada para mí". Cuando la app pasa por la portería (App Service arranca o se reconfigura), el portero (App Service Runtime) llama al banco con la identidad de la app y recoge el secreto del momento, sin que nadie tenga que copiarlo a ningún sitio intermedio. El secreto **nunca toca el papel**.

Mantén la imagen. El submódulo no es complejo cuando interiorizas: MI primero, Key Vault para lo que no puede ser MI, referencias desde config, rotación con Event Grid.

---

## 5. Recorrido por el código

### `KeyVaultItemAdvisor` — dónde va cada cosa y con qué rol

Dos funciones clave. La primera mapea "qué tipo de secreto" a "dónde guardarlo":

```csharp
public static Destino Donde(QueGuardar que) => que switch
{
    QueGuardar.ConexionAzureAAzure   => Destino.ManagedIdentity,   // ¡NO va a KV!
    QueGuardar.ApiKeyExterna or
    QueGuardar.ClientSecretAppReg    => Destino.KeyVaultSecret,
    QueGuardar.CertificadoSsl        => Destino.KeyVaultCertificate,
    QueGuardar.ClaveCifrado          => Destino.KeyVaultKey,
    _ => throw new ArgumentOutOfRangeException(nameof(que)),
};
```

La primera rama es la más importante: **conexión Azure-a-Azure no va a Key Vault**. Va a Managed Identity. Si tu App Service llama a Cosmos, no hay secreto que guardar; le das a la identidad del App Service el rol "Cosmos DB Built-in Data Contributor" sobre el container y se acabó. Mucha gente arranca con MI mal entendida y pone connection strings con account keys en Key Vault como "buena práctica". No es buena práctica; es estar en el siglo XIX cuando MI ya existe.

Las otras tres ramas distinguen los tres tipos de objeto que Key Vault gestiona:

- **Secrets**: cualquier cadena de texto opaca (API keys, client secrets de App Registrations, passwords de terceros). Se guarda, se versiona, se lee, se rota.
- **Certificates**: certificados X.509 con clave pública/privada. Key Vault los gestiona como un objeto compuesto (cert + private key) y puede automatizar la renovación con Certificate Authorities integradas.
- **Keys**: claves criptográficas RSA o EC para operaciones (cifrar/descifrar, firmar/verificar). Lo importante: **la clave privada nunca sale de Key Vault**. Tu app pide "fírmame esto" o "descíframe aquello" y Key Vault ejecuta la operación con la clave guardada. La firma JWT con claves de KV se hace así.

La segunda función recomienda el **rol RBAC mínimo** por tipo + acceso:

```csharp
public static string RolMinimo(Destino destino, AccesoKv acceso) => destino switch
{
    Destino.KeyVaultSecret => acceso == AccesoKv.Lectura
        ? "Key Vault Secrets User"      // solo leer
        : "Key Vault Secrets Officer",  // gestionar

    Destino.KeyVaultKey => acceso == AccesoKv.UsoCripto
        ? "Key Vault Crypto User"       // firmar/cifrar con la clave
        : "Key Vault Crypto Officer",   // gestionar la clave

    Destino.KeyVaultCertificate => "Key Vault Certificates Officer",
    Destino.ManagedIdentity => "(no aplica: RBAC del recurso destino)",
    _ => throw new ArgumentOutOfRangeException(nameof(destino)),
};
```

Tres roles "User" para la operación normal (leer secret, usar clave, cualquier uso de cert) y tres "Officer" para gestión (crear, modificar, rotar). **No hay "Administrator" recomendado en ningún caso**: ese rol da control absoluto incluyendo gestionar quién más tiene acceso, y solo lo necesita el equipo que opera el propio Key Vault, no las aplicaciones.

Y la constante final:

```csharp
public const bool RbacRecomendadoSobreAccessPolicies = true;
```

Key Vault tiene dos modelos de autorización: **Access Policies** (el legacy, una lista de "quién puede hacer qué") y **RBAC** (el moderno, roles asignables como en el resto de Azure). **RBAC es el recomendado**. Si encuentras un Key Vault todavía con Access Policies, migrar a RBAC es trivial desde el portal y te abre el mismo modelo de gestión que tienes para storage, SQL, etcétera.

### `KeyVaultReference` — la sintaxis que App Service entiende

Una `GeneratedRegex` que parsea y construye la sintaxis:

```csharp
[GeneratedRegex(
    @"^@Microsoft\.KeyVault\(VaultName=(?<v>[^;]+);SecretName=(?<s>[^;)]+)(;SecretVersion=(?<ver>[^;)]+))?\)$",
    RegexOptions.IgnoreCase)]
private static partial Regex Patron();

public static string Construir(string vault, string secret, string? version = null) =>
    $"@Microsoft.KeyVault(VaultName={vault};SecretName={secret}" +
    (version is null ? "" : $";SecretVersion={version}") + ")";
```

La sintaxis es `@Microsoft.KeyVault(VaultName=mivault;SecretName=stripe-api-key)`. Cuando metes esto en un App Setting de App Service (o de Function App), pasa lo siguiente:

1. App Service arranca y lee la lista de App Settings.
2. Para cada setting con valor que empieza por `@Microsoft.KeyVault(`, lo trata como referencia.
3. Usa la Managed Identity del App Service para conectarse al Key Vault especificado.
4. Pide el secreto al Key Vault y lo guarda en memoria, indexado por el nombre del setting.
5. Tu código lee `Environment.GetEnvironmentVariable("StripeApiKey")` y recibe **el valor real**, no la referencia.

Beneficios concretos:

- El secreto nunca está en `appsettings.json` ni en archivos de configuración.
- El secreto nunca está en variables de entorno expuestas (App Service las cifra at-rest).
- Rotar el secreto en Key Vault → reiniciar el App Service → la nueva versión se aplica. (Sin reiniciar, App Service refresca con cierta cadencia pero el reinicio es la garantía).
- Si especificas una versión concreta (`;SecretVersion=abc123`), te quedas en esa versión hasta que cambies la referencia. Sin versión, App Service usa siempre la última.

El método `Parsear` te permite hacer análisis: dada una App Setting, decir "esto es una referencia a tal vault, tal secret, tal versión". Útil para scripts que auditan toda tu suscripción.

### `SecretRotationPolicy` — vigente, próximo a expirar, expirado

La función pura que define los tres estados:

```csharp
public const int VentanaDiasPorDefecto = 30;

public static EvaluacionRotacion Evaluar(
    DateTimeOffset expira, DateTimeOffset ahora, int ventanaDias = VentanaDiasPorDefecto)
{
    var dias = (int)Math.Floor((expira - ahora).TotalDays);

    if (dias < 0)
        return new EvaluacionRotacion(EstadoSecreto.Expirado, dias, true);

    if (dias <= ventanaDias)
        return new EvaluacionRotacion(EstadoSecreto.ProximoAExpirar, dias, true);

    return new EvaluacionRotacion(EstadoSecreto.Vigente, dias, false);
}
```

Tres reglas:

1. **Días negativos**: ya caducó. `DebeRotar = true` ya tarde — alguien debería haber actuado antes.
2. **Días entre 0 y la ventana** (30 por defecto): próximo a expirar. `DebeRotar = true`. Es la zona donde **Event Grid emite `SecretNearExpiry`** y deberías tener una función que reaccione.
3. **Días mayores que la ventana**: vigente. No hay nada que hacer.

El reloj se inyecta como parámetro (`ahora`), no se lee de `DateTimeOffset.UtcNow`. Así los tests pueden congelar el tiempo y validar los tres casos en milisegundos. Esto es testabilidad básica, pero en código real es muy fácil olvidarlo y acabar con tests que dependen del reloj real.

### `KeyVaultPlanner` — el plan completo

Combina los anteriores. Dado un escenario ("guardar la API key de Stripe en mi App Service"), devuelve:

- Dónde va: KeyVaultSecret.
- Qué rol mínimo asignar a la Managed Identity del App Service: "Key Vault Secrets User".
- Qué App Setting configurar: `@Microsoft.KeyVault(VaultName=...;SecretName=stripe-api-key)`.
- Y qué hacer con la rotación: configurar Event Grid `SecretNearExpiry` con ventana de 30 días.

Para casos Azure-a-Azure el plan es distinto: sin KV, sin App Setting con referencia, solo "asigna a la MI del App Service el rol X sobre el recurso destino".

---

## 6. La regla operativa de oro: MI primero, KV después

Vale la pena dedicarle una sección porque es la fuente número uno de over-engineering en Key Vault. La regla:

**Por cada secreto que vayas a guardar, antes de meterlo en Key Vault, pregúntate: ¿este secreto puede ser una Managed Identity?**

- Conexión App Service → Cosmos: **sí**. Da rol al App Service sobre el container. No metas la connection string en KV.
- Conexión App Service → Storage: **sí**. Da rol al App Service sobre el storage account. No metas el storage account key en KV.
- Conexión App Service → SQL: **sí, con Entra ID auth**. Da rol al App Service sobre la BD. No metas la SQL password en KV.
- Conexión App Service → Service Bus: **sí**. Da rol al App Service sobre el namespace. No metas el shared access key en KV.
- API key de Stripe para tu App Service: **no**. Stripe no tiene Managed Identity. Va a KV.
- Client secret de una App Registration para auth contra otra API: **no, pero puedes mejorarlo**. Va a KV. Pero también puedes considerar federación OIDC o certificado en lugar de secret.
- Certificado SSL para `*.empresa.com`: **no**. Va a KV.
- Claves RSA para firmar JWTs custom: **no**. Van a KV (como Keys, no como Secrets).

Si aplicas la regla, descubrirás que **el 70% de los "secretos" que metías a Key Vault no tenían que estar ahí**. Son conexiones a recursos Azure que admiten Managed Identity. Migrar a MI te quita un sitio donde rotar, te quita un secreto que un atacante puede pillar, y simplifica la operación.

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/KeyVault.Demo.Api
# http://localhost:5093
```

Endpoints:

```http
### Dónde guardar una API key de Stripe
GET http://localhost:5093/kv/donde?que=ApiKeyExterna
# → KeyVaultSecret, rol mínimo: "Key Vault Secrets User"

### Construir una Key Vault Reference
GET http://localhost:5093/kv/referencia?vault=mivault&secret=stripe-api-key
# → @Microsoft.KeyVault(VaultName=mivault;SecretName=stripe-api-key)

### Parsear una referencia existente
POST http://localhost:5093/kv/referencia
Content-Type: application/json

"@Microsoft.KeyVault(VaultName=miv;SecretName=ss;SecretVersion=abc)"
# → { vault: "miv", secret: "ss", version: "abc" }

### Evaluar rotación
POST http://localhost:5093/kv/rotacion
Content-Type: application/json

{
  "expira": "2026-06-10T00:00:00Z",
  "ahora":  "2026-05-20T00:00:00Z",
  "ventanaDias": 30
}
# → { estado: "ProximoAExpirar", diasRestantes: 21, debeRotar: true }

### Plan completo para guardar API key de Stripe
POST http://localhost:5093/kv/plan
Content-Type: application/json

{
  "que": "ApiKeyExterna",
  "vault": "mivault",
  "secretName": "stripe-api-key"
}
```

Los 27 tests cubren todas las combinaciones: cada tipo de "qué guardar" mapeado a su destino, los tres roles mínimos según acceso, construcción/parseo de referencias (incluyendo round-trip y case-insensitive), las tres rutas de la política de rotación con reloj inyectable.

Para inventariar el Key Vault real:

- `scripts/01-kv-inventory.sh` — lista los Key Vaults de la suscripción, comprueba que tienen RBAC (no Access Policies), valida que tienen purge protection habilitada en producción, y lista los **nombres** de los secretos con su fecha de caducidad. **Nunca lee valores**; ese es un principio operativo del script. Requiere rol `Key Vault Reader` o `Key Vault Secrets User`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué este submódulo tampoco tiene CAPA de integración

El emulador "oficial" de Key Vault no existe, y los mocks de la SDK `SecretClient` son complejos de mantener. Lo que sí podemos probar al 100% es la **decisión y la sintaxis**:

- ¿Dónde va este tipo de secreto? (función pura).
- ¿Qué rol asignar? (función pura).
- ¿Es esta cadena una Key Vault Reference válida? (función pura).
- ¿Toca rotar este secreto con la fecha de hoy? (función pura).

La validación real con Key Vault de verdad se hace con la práctica S6.P, que sí monta un Key Vault de verdad en Azure (cuesta unos céntimos, hay cleanup) y prueba el round-trip end-to-end. Aquí, en el submódulo conceptual, nos quedamos con la lógica pura más el grafo DI.

---

## 9. La conversación con el equipo: "¿secretos o referencias?"

Pregunta que aparece a menudo: "¿tu app debería leer Key Vault directamente con `SecretClient`, o debería usar Key Vault References en App Settings?". Las dos opciones tienen su uso:

**Key Vault References** (preferible para la mayoría de casos):

- Simple: tu código lee variables de entorno. No conoce Key Vault.
- App Service gestiona el ciclo de vida y el cacheo.
- Los secretos no llegan a `Console.WriteLine` accidentales en tu código.
- Funciona transparentemente con cualquier librería que lea config de ASP.NET Core / .NET Generic Host.

**`SecretClient` directo** (necesario en algunos casos):

- Cuando el valor cambia mid-execution y necesitas refrescar sin reiniciar.
- Cuando el secreto es muy grande o binario (App Settings tiene límites de tamaño).
- Cuando necesitas acceso programático a metadatos (versión, fecha de creación, tags).
- En aplicaciones que no corren bajo App Service / Function App (worker desktop, contenedor en AKS sin Workload Identity, etcétera).

La regla pragmática: **empieza con referencias, pasa a `SecretClient` solo cuando una limitación específica te obligue**.

---

## 10. Las cinco trampas más comunes con Key Vault

**Trampa 1 — Meter en KV cosas que deberían ser MI**. Ya cubierto. La pregunta antes de cada secreto: "¿puede ser MI?".

**Trampa 2 — Asignar Key Vault Administrator a las aplicaciones**. El rol User basta para leer; Officer para gestionar. Administrator es solo para administradores del Key Vault, no para apps.

**Trampa 3 — Olvidar purge protection en producción**. Sin purge protection, un actor malicioso (o un script con bug) puede borrar y purgar tus secretos en horas. Con purge protection, hay un periodo de 7-90 días donde están en estado "deleted" pero recuperables.

**Trampa 4 — No configurar Event Grid para `SecretNearExpiry`**. Sin eso, te enteras de que un secreto ha caducado cuando algo falla. Con eso, te avisan 30 días antes y rotas con calma.

**Trampa 5 — Subir un nuevo secret cada vez que rotas en lugar de añadir una versión**. Key Vault versiona automáticamente: subes el mismo secret-name con un valor nuevo y tienes versión nueva, la anterior sigue accesible si te equivocas. Si en lugar de eso creas `stripe-api-key-v2`, `stripe-api-key-v3`, etcétera, pierdes la trazabilidad del versionado nativo.

---

## 11. Glosario breve

- **Secret**: cualquier cadena opaca guardada en Key Vault. Tiene nombre, valor, versión, fecha de creación, fecha de caducidad opcional, tags.
- **Key**: clave criptográfica (RSA o EC) guardada en Key Vault. La clave privada **nunca sale**; las operaciones (sign, decrypt) se hacen dentro del Vault.
- **Certificate**: certificado X.509 con clave privada. Key Vault lo gestiona como un compuesto y puede automatizar la renovación con Certificate Authorities integradas (DigiCert, GlobalSign).
- **Access Policy**: el modelo legacy de autorización. Una lista de "quién puede hacer qué" por usuario/app. Reemplazado por RBAC.
- **RBAC en Key Vault**: el modelo moderno. Roles asignables como en el resto de Azure: Secrets User, Secrets Officer, Crypto User, Crypto Officer, Certificates Officer, Administrator.
- **Key Vault Reference**: sintaxis `@Microsoft.KeyVault(VaultName=...;SecretName=...)` que App Service y Function App entienden en App Settings.
- **Managed Identity**: identidad del recurso de Azure que se autentica contra otros servicios sin secret. La forma correcta de hacer "App Service llama a Key Vault" sin guardar credenciales.
- **Purge protection**: característica que impide eliminar permanentemente un secret durante un periodo (7-90 días). Habilítala en producción siempre.
- **SecretNearExpiry**: evento de Event Grid que Key Vault emite 30 días antes de que un secret expire. Punto de enganche para automatizar rotaciones.
- **`SecretClient`**: la clase de la SDK `Azure.Security.KeyVault.Secrets` para acceso programático.
- **`DefaultAzureCredential`**: clase que intenta autenticarse con la mejor opción disponible (MI del recurso, Visual Studio, Azure CLI, etcétera). Cero secretos en tu código.

---

## 12. Cierre

Key Vault es donde van los secretos que no pueden ser Managed Identity. La regla operativa es: MI primero, KV después; rol mínimo siempre; referencias desde App Settings cuando puedas; rotación con Event Grid. Si interiorizas esto, tu sistema operativo de secretos en Azure es robusto y poco mantenible.

Lo siguiente es [`S6.P — Práctica OAuth2 + Key Vault`](../S6.P-practica-oauth2-keyvault/MANUAL.md), donde se integran este submódulo y S6.3 en una aplicación que autentica usuarios con Entra ID y guarda sus tokens cifrados en Key Vault con Managed Identity. Es donde la teoría conceptual del módulo se convierte en código que se despliega.
