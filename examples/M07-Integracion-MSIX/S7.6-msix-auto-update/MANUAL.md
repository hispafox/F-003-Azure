# Manual del alumno — S7.6 · MSIX auto-update

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell, despliegue por Portal. Este manual va antes: te cuenta qué es exactamente un `.appinstaller`, cómo funciona el staged rollout 5/25/50/100 con cohortes deterministas por usuario, y cuál es la opción más limpia para hacer rollback (la que el slide 8 llama "republicar la previa con build+1").

Tiempo de lectura: ~25 min. Submódulo de teoría: [M07-S7.6](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.6-msix-auto-update-v3.md). Tres piezas de lógica pura (builder/parser del XML del `.appinstaller`, política canary con cohortes monotónicas, advisor de comparación de versiones y rollback).

*Creado: 2026-05-20 21:15 +0200*

---

## 1. La idea en una frase

Una vez tu app MSIX está distribuida, tienes un problema operativo nuevo: **¿cómo se actualiza?** El `.appinstaller` es un XML pequeño que vive en un servidor (típicamente Azure Blob), apunta a la última versión del `.msix` y configura el comportamiento del auto-update (cada cuánto comprobar, si bloquea la activación, si fuerza downgrade). Pero un release real no es "subo y todos lo reciben de golpe": es **staged rollout** —primero el 5% de los usuarios, después el 25%, etcétera—, con cohortes deterministas (el mismo usuario siempre en la misma cohorte) y la capacidad de no avanzar si la telemetría detecta problemas.

El submódulo materializa el `.appinstaller` como builder/parser puro, el canary rollout como política determinista basada en SHA-256, y el rollback como una operación bien definida: **republicar la versión previa buena con `build+1` en la etiqueta** (la versión sube pero el código es el bueno).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican la cadencia de "5/25/50/100 con rollback fácil":

**Caso 1 — el big-bang release que rompió a todo el mundo.** Un equipo publicó la versión 3.0 de su app desktop a 8.000 usuarios el lunes a las 9. A las 9:30, el equipo de soporte recibía decenas de llamadas: la nueva versión crasheaba al arrancar contra cierta configuración del SO. **A las 9:30 todos los usuarios ya tenían la versión rota** porque el `.appinstaller` se aplica al abrir la app. El rollback consistió en publicar el `.msix` antiguo a las 12, pero el daño estaba hecho: tres horas con la app caída y 200 incidentes abiertos. La lección: nunca publiques a todos a la vez. Empieza con el 5% (400 usuarios), observa 24 horas, sube al 25% (2.000), observa otras 24, etcétera. Si algo va mal, no afecta a todos.

**Caso 2 — el usuario que cae unas veces sí, otras no.** Otro equipo configuró el canary mal: cada vez que un usuario abría la app, una función random decidía si recibía la versión nueva. **El mismo usuario veía la app vieja por la mañana y la nueva por la tarde** según el azar. Confusión, bugs intermitentes, soporte loco. La solución correcta: cohortes deterministas basadas en SHA-256 del userId. Un usuario en el 5% queda en el 5%; cuando subes al 25%, el grupo del 25% incluye al del 5% más nuevos; cuando subes al 50%, etcétera. Es lo que el ejemplo llama "monotónico".

**Caso 3 — el rollback que descontó la versión.** Versión 2.4.5.0 publicada, falla. El equipo decidió "volver a la 2.4.4.0". Subieron el .msix antiguo al servidor cambiando solo la URL. **Windows no instaló: la versión es menor que la instalada (2.4.5.0)**. Tuvieron que activar `ForceUpdateFromAnyVersion` y al hacerlo, los usuarios que NO tenían el bug también bajaron a 2.4.4.0 (con sus propios bugs ya corregidos). La opción correcta del slide 8: **etiquetar el rollback como 2.4.6.0** (build+1) con el código de 2.4.4.0. La etiqueta sube, los usuarios "actualizan" sin saberlo, y el código es el bueno. Cero downgrade en términos de versión, problema resuelto.

Los tres casos los previene el ejemplo: staged rollout con cohortes deterministas y plan de rollback automatizable.

---

## 3. Por qué esto importa en tu stack

Si tu app MSIX se actualiza más de una vez al mes, las cuatro preguntas del staged rollout van a ser parte de tu operación:

- **¿Cuándo comprobar updates?** El `.appinstaller` define `HoursBetweenUpdateChecks`. Una hora es agresivo (cada apertura de sesión); más es conservador.
- **¿Qué hago cuando la nueva versión falla en producción?** Si tienes telemetría de versión por usuario, sabes en horas que el 5% que recibió la nueva tiene un crash rate más alto. El plan de rollback debe estar diseñado antes del primer release.
- **¿Cómo se reparten los usuarios entre canales (stable/beta/dev)?** Tres `.appinstaller` distintos en URLs distintas. Tu pipeline publica a `beta` primero y a `stable` 48h después si todo va bien.
- **¿Cuándo es legítimo forzar la actualización?** `UpdateBlocksActivation = true` impide que el usuario abra la app vieja. Reservado para vulnerabilidades de seguridad o bugs que corrompan datos. **Nunca como práctica habitual**.

Si tienes las respuestas, las actualizaciones de tu app dejan de ser un evento de riesgo y se convierten en una operación rutinaria.

---

## 4. La analogía vertebradora: las actualizaciones del coche conectado

Imagina que conduces un coche eléctrico moderno que recibe actualizaciones de software por aire. El fabricante quiere desplegar una versión nueva del firmware al millón de coches que tiene en circulación. ¿Cómo lo hace sin que un bug deje a un millón de coches parados al mismo tiempo?

**No publica de golpe.** Hace **staged rollout**:

- Día 1: el 5% de los coches recibe la nueva versión. **Solo coches concretos**, no aleatorios — los mismos coches que estaban en el grupo de pruebas anterior, identificados por su VIN. Eso es **cohorte determinista**: si tu coche entra en el "early adopter 5%", siempre entra.
- Día 2-3: monitoriza telemetría. ¿Hay coches que reportan errores? ¿Crash rate inusual? ¿Quejas en redes sociales? Si todo va bien, sube al 25%.
- Día 4-5: si todo bien, sube al 50%. Después al 100%.
- Si en cualquier momento detecta un problema, **no avanza** y publica un fix.

**Los canales también existen**:

- **Canal Stable**: la versión bien probada que reciben todos. Conservadora.
- **Canal Beta**: la versión candidata. Los usuarios que se apuntan voluntariamente reciben la beta una semana antes del release general. **Cazadores de bugs**.
- **Canal Dev**: la versión en desarrollo, semanal, con features experimentales. **Solo el equipo de QA y entusiastas**.

Cada canal apunta a un **manifest distinto** (en MSIX, un `.appinstaller` por canal). Mismo código compilado, distinto canal según la URL del manifest que el coche consulta.

Y luego está el **rollback**. ¿Qué pasa si la versión nueva tiene un bug crítico?

- **Opción ingenua**: publicar la versión vieja como "actualización". Pero la versión vieja **tiene número menor**, así que el sistema de actualización no la acepta — para él, "actualizar" significa subir, no bajar.
- **Opción del slide 8 (la limpia)**: publicar la versión vieja **con un número mayor**. Si la versión rota era 2.4.5, la vieja buena (2.4.4) se reempaqueta como 2.4.6 manteniendo el código. El sistema ve "hay 2.4.6 disponible" y actualiza. **Los usuarios "actualizan" sin saberlo, y el código instalado es el bueno**.

Mantén la imagen: el coche conectado es tu app MSIX, el `.appinstaller` es la radio que escucha al fabricante, los canales son las flotas piloto, las cohortes son los números VIN. Es exactamente el mismo modelo mental que enseña el submódulo.

---

## 5. Recorrido por el código

### `AppInstallerBuilder` — el XML del auto-update

El builder construye un `.appinstaller` válido a partir de un modelo plano y testeable:

```csharp
public sealed record UpdateSettingsConfig(
    int HoursBetweenUpdateChecks = 1,
    bool ShowPrompt = true,
    bool UpdateBlocksActivation = false,   // true = obligatoria
    bool AutomaticBackgroundTask = true,
    bool ForceUpdateFromAnyVersion = true);

public sealed record AppInstallerConfig(
    string AppInstallerUri,                 // URL pública del .appinstaller
    string Version,                          // versión del propio .appinstaller
    MainPackageConfig MainPackage,
    UpdateSettingsConfig UpdateSettings);
```

El XML resultante:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018"
              Uri="https://miapp.blob.core.windows.net/msix-stable/MiApp-stable.appinstaller"
              Version="2.4.5.0">
  <MainPackage Name="Acme.MiApp"
               Version="2.4.5.0"
               Publisher="CN=Acme Corp"
               ProcessorArchitecture="x64"
               Uri="https://miapp.blob.core.windows.net/msix-stable/Acme.MiApp_2.4.5.0_x64.msix" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="1"
              ShowPrompt="True"
              UpdateBlocksActivation="False" />
    <AutomaticBackgroundTask />
    <ForceUpdateFromAnyVersion>True</ForceUpdateFromAnyVersion>
  </UpdateSettings>
</AppInstaller>
```

Cinco flags de `UpdateSettings` con efecto operativo claro:

- **`HoursBetweenUpdateChecks`** (slide 3): cada cuántas horas comprueba updates al abrir. 0 = cada apertura; 1 = como máximo cada hora; 24 = una vez al día. **Default razonable: 1**.
- **`ShowPrompt`** (slide 3): si hay update, ¿mostrar diálogo al usuario? `true` es la opción profesional (transparencia); `false` es para actualizaciones silenciosas (kioscos, dispositivos no-asistidos).
- **`UpdateBlocksActivation`** (slide 13): si hay update disponible, ¿bloquear que el usuario abra la versión vieja? `true` = forzar actualización antes de uso. **Solo para releases críticos** (seguridad, corrupción de datos); usado a diario es opresivo.
- **`AutomaticBackgroundTask`** (slide 3): ¿el SO comprueba updates en background, sin requerir que el usuario abra la app? Útil para apps que se abren raramente.
- **`ForceUpdateFromAnyVersion`** (slide 7/8): ¿permitir "downgrades" (instalar una versión menor que la actual)? Necesario para el rollback de "manual y feo" — la opción limpia del slide 8 lo evita.

El parser hace el round-trip: lee un `.appinstaller` y lo convierte de vuelta al modelo. Los tests verifican que **`Parsear(Construir(x)) == x`**.

### `CanaryRolloutPolicy.Cohorte` — la cohorte determinista

```csharp
public static int Cohorte(string userId)
{
    byte[] sha = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
    uint n = BitConverter.ToUInt32(sha, 0);
    return (int)(n % 100);   // [0..99]
}

public static DecisionRollout RecibeActualizacion(string userId, int porcentaje) =>
    new DecisionRollout(
        RecibeNueva: Cohorte(userId) < porcentaje,
        PorcentajeUmbral: porcentaje,
        Hash: Cohorte(userId));
```

Tres ideas cruciales:

1. **Determinista**: el mismo `userId` siempre cae en la misma cohorte. SHA-256 es estable, así que la función es pura.
2. **Distribuida uniformemente**: SHA-256 produce hashes distribuidos uniformemente. Si tienes 1.000 usuarios, ~50 caen en cada cohorte de 0-99 (estadísticamente).
3. **Monotónica**: si un usuario está en el 5% (su cohorte es 0-4), también está en el 25% (su cohorte es < 25) y en el 50% y en el 100%. **Un usuario que recibió la beta nunca pierde la beta**.

Esto último es importante: cuando subes el porcentaje de rollout, los usuarios nuevos se añaden a los anteriores; nadie sale del grupo. **Sin estabilidad, los usuarios de la cohorte 5 verían la nueva versión un día y la vieja al siguiente**. Con SHA-256 estable, eso no pasa.

### `CanaryRolloutPolicy.SiguienteEtapa` — cuándo avanzar

```csharp
public static int? SiguienteEtapa(int etapaActual, bool saludOk)
{
    if (!EtapasCanary.Porcentajes.Contains(etapaActual))
        throw new ArgumentOutOfRangeException(nameof(etapaActual), ...);
    if (!saludOk) return etapaActual;          // no avanzar
    return EtapasCanary.Porcentajes
        .SkipWhile(p => p != etapaActual)
        .Skip(1)
        .Cast<int?>()
        .FirstOrDefault();
}
```

Las etapas: **5, 25, 50, 100**. La función dice cuál es la siguiente, **solo si la salud está bien**:

- Estás en 5 con salud OK → siguiente es 25.
- Estás en 25 con salud OK → siguiente es 50.
- Estás en 25 con salud KO → siguiente es 25 (no avanzar; investigar; posiblemente rollback).
- Estás en 100 → no hay siguiente (`null`).

Esta es la lógica del pipeline. En CI/CD se programa algo así: tras 24 horas en una etapa, llama a esta función con la telemetría de salud, y si dice "avanza", actualizas el `.appinstaller` con el nuevo porcentaje.

### `UpdateVersionAdvisor.Comparar` — la regla de la versión mayor

```csharp
public static DecisionActualizar Comparar(
    string actual, string disponible, bool forceFromAnyVersion = false)
{
    int cmp = ParseVersion(disponible).CompareTo(ParseVersion(actual));
    return cmp switch
    {
        > 0 => new(true, "mayor", "Versión disponible es mayor."),
        0   => new(false, "igual", "Misma versión: no actualizar."),
        _   => forceFromAnyVersion
            ? new(true, "menor", "ForceUpdateFromAnyVersion permite el downgrade.")
            : new(false, "menor", "Versión disponible es menor: bloqueada..."),
    };
}
```

La regla del slide 7: **la nueva versión debe ser mayor que la instalada**. Si es menor, Windows no actualiza salvo que `ForceUpdateFromAnyVersion = true` esté activo. Es la regla que rompió al equipo del caso 3: pensar que "actualizar a la versión vieja" era posible por defecto.

### `UpdateVersionAdvisor.PlanificarRollback` — el truco del build+1

```csharp
public static PlanRollback? PlanificarRollback(
    string versionMala, IReadOnlyList<string> historial)
{
    var mala = ParseVersion(versionMala);
    var ordenado = historial.OrderBy(v => Version.Parse(v)).ToList();

    int idx = ordenado.IndexOf(versionMala);
    if (idx <= 0) return null;                  // no hay previa

    return new PlanRollback(
        VersionPreviaBuena: ordenado[idx - 1],
        EtiquetaRollback: IncrementarBuild(versionMala));   // 2.4.5.0 → 2.4.6.0
}
```

Esta es la magia del slide 8:

- Tienes en producción la 2.4.5.0 (rota). El historial es `[2.4.3.0, 2.4.4.0, 2.4.5.0]`.
- La función devuelve: **VersionPreviaBuena = 2.4.4.0** y **EtiquetaRollback = 2.4.6.0**.
- En la práctica: coges el código de 2.4.4.0, cambias **solo el manifest** poniendo `Version="2.4.6.0"`, empaquetas, firmas, publicas.
- Windows ve "hay 2.4.6.0 disponible" y actualiza desde 2.4.5.0. Los usuarios reciben código de la 2.4.4.0 con etiqueta de 2.4.6.0.
- Sin `ForceUpdateFromAnyVersion`, sin downgrade, sin que nadie pierda funcionalidad ya estable.

Es la opción de rollback más limpia que existe en MSIX. Vale la pena interiorizarla porque el día que tengas un release malo en producción, vas a querer esta receta lista.

---

## 6. La fleet telemetry: saber qué versión tiene cada usuario

Sin telemetría de versión, el staged rollout es ciego. ¿Cómo sabes si la etapa del 5% va bien o mal si no sabes quién está en ella y cómo le va?

La receta estándar:

```csharp
// En App.cs / Program.cs de la app desktop, al arrancar:
var version = Package.Current.Id.Version;
var versionStr = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
telemetryClient.TrackEvent("AppStarted", new Dictionary<string, string>
{
    ["AppVersion"] = versionStr,
    ["UserId"] = userId,                  // si tienes auth
    ["Cohort"] = CanaryRolloutPolicy.Cohorte(userId).ToString(),
});
```

Application Insights de Azure es la opción natural: cada `AppStarted` aparece con su versión. Queries en KQL te dicen:

- **Crash rate por versión**: `customEvents | where name == "AppCrashed" | summarize count() by tostring(customDimensions.AppVersion)`.
- **Cuántos usuarios en cada cohorte**: `customEvents | where name == "AppStarted" | summarize count() by tostring(customDimensions.Cohort)`.
- **% de usuarios actualizados**: `customEvents | where timestamp > ago(24h) | summarize cnt = count() by AppVersion`.

Sin telemetría, no hay staged rollout. Es la pareja inseparable. El pipeline que avanza al 25% sin haber mirado el crash rate del 5% es un pipeline que confía en la suerte.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/AutoUpdate.Demo.Api
# http://localhost:5101
```

Endpoints:

```http
### Construir un .appinstaller
POST http://localhost:5101/update/appinstaller
Content-Type: application/json

{
  "appInstallerUri": "https://miapp.blob.core.windows.net/msix-stable/MiApp-stable.appinstaller",
  "version": "2.4.5.0",
  "mainPackage": {
    "name": "Acme.MiApp",
    "version": "2.4.5.0",
    "publisher": "CN=Acme Corp",
    "processorArchitecture": "x64",
    "packageUri": "https://miapp.blob.core.windows.net/msix-stable/Acme.MiApp_2.4.5.0_x64.msix"
  },
  "updateSettings": {
    "hoursBetweenUpdateChecks": 1,
    "showPrompt": true,
    "updateBlocksActivation": false,
    "automaticBackgroundTask": true,
    "forceUpdateFromAnyVersion": true
  }
}
# → XML del .appinstaller

### Cohorte de un userId (siempre la misma)
GET http://localhost:5101/update/canary?userId=user-123&porcentaje=25
# → { recibeNueva: true|false, porcentajeUmbral: 25, hash: 17 }

### Siguiente etapa si la salud está bien
GET http://localhost:5101/update/siguiente-etapa?etapaActual=25&saludOk=true
# → 50

### Comparar versiones
GET http://localhost:5101/update/comparar?actual=2.4.4.0&disponible=2.4.5.0
# → { debeActualizar: true, comparacion: "mayor", razon: "..." }

### Plan de rollback
POST http://localhost:5101/update/rollback
Content-Type: application/json

{
  "versionMala": "2.4.5.0",
  "historial": ["2.4.3.0", "2.4.4.0", "2.4.5.0"]
}
# → { versionPreviaBuena: "2.4.4.0", etiquetaRollback: "2.4.6.0" }
```

Los 37 tests cubren el round-trip del builder/parser, la monotonía del canary (`user en 5% → en 25% → en 50%`), el bloqueo de avance con salud KO, las cuatro comparaciones de versión (mayor, igual, menor sin force, menor con force), el rollback con historial ordenado y desordenado, y la validación de versiones mal formadas.

Para inspeccionar tu Windows real:

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-inspect-appinstaller.ps1 → descarga un .appinstaller y lo parsea
# 2) 02-installed-versions.ps1   → versiones MSIX instaladas por Identity
```

El primer script es útil cuando te conectas a un servidor de partner para auditar su política de updates. El segundo es la versión rudimentaria de la fleet telemetry: enumera lo instalado en tu máquina.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y PowerShell `pwsh`.

---

## 8. Los anti-patterns del slide 24

Cinco errores caros en sistemas de auto-update reales:

**Anti-pattern 1 — Big-bang release**. Publicar al 100% directamente, sin canary. Es lo del caso 1. Siempre staged rollout.

**Anti-pattern 2 — Sin plan de rollback**. Si la única forma de revertir es "publicamos la vieja con `ForceUpdateFromAnyVersion`", tienes un problema. Diseña el rollback como build+1 desde el día uno.

**Anti-pattern 3 — Sin telemetría de versión**. Sin saber qué versión tiene cada usuario, no sabes si el rollout va bien. Antes de tu primer release, configura `AppStarted` con `AppVersion`.

**Anti-pattern 4 — `UpdateBlocksActivation = true` como práctica habitual**. Bloquear que el usuario abra la app vieja es agresivo. Reservado para vulnerabilidades de seguridad o corrupción de datos. Una versión nueva con features nuevas no justifica bloquear.

**Anti-pattern 5 — Cohorte aleatoria por sesión**. El mismo usuario va saltando entre versiones según el random. Confusión, soporte loco. SHA-256 del userId, siempre.

---

## 9. La deprecation de AppInstaller en 2026 (slide 18)

Nota operativa importante: Microsoft ha anunciado que **el `.appinstaller` está en deprecation roadmap para 2026**. La razón: winget se está convirtiendo en el mecanismo unificado de instalación y actualización para Windows.

Esto **no significa que tu app vaya a dejar de funcionar mañana**. Significa que durante los próximos 18-24 meses la conversación sobre auto-update va a empezar a moverse hacia winget. Tres pistas:

- Winget puede instalar paquetes MSIX firmados.
- Winget puede actualizar paquetes ya instalados.
- Winget tiene un protocolo para "manifests de paquete" en YAML que reemplaza al `.appinstaller` XML.

La forma correcta de prepararse: **mantén tus releases en MSIX como hoy, sigue publicando con `.appinstaller` mientras funcione, pero diseña el pipeline para que añadir un publish a winget sea una línea más**. Cuando llegue el momento, no será una migración traumática.

---

## 10. Glosario breve

- **`.appinstaller`**: XML que apunta a un `.msix` y configura su auto-update.
- **`UpdateSettings`**: sección del `.appinstaller` con cinco flags (cuándo comprobar, mostrar prompt, bloquear activación, background, force downgrade).
- **`OnLaunch`**: elemento dentro de `UpdateSettings` que define el comportamiento al abrir la app.
- **`UpdateBlocksActivation`**: si está activo, Windows no deja abrir la versión vieja mientras haya update pendiente.
- **`ForceUpdateFromAnyVersion`**: permite "downgrades" (instalar versión menor que la actual).
- **`HoursBetweenUpdateChecks`**: cada cuántas horas Windows comprueba el `.appinstaller` para ver si hay versión nueva.
- **Canary release**: liberar una versión nueva solo a un subset de usuarios (5%, luego 25%...).
- **Staged rollout**: secuencia de etapas en el canary (5/25/50/100).
- **Cohorte**: grupo de usuarios identificados por SHA-256 de su userId, módulo 100.
- **Monotónica**: si un usuario está en el 5%, también está en el 25%, 50% y 100%.
- **Canal stable/beta/dev**: distintos `.appinstaller` con distintas audiencias y cadencias.
- **`PackageManager`**: API de Windows que aplica los `.appinstaller` y gestiona instalaciones.
- **Fleet telemetry**: telemetría que captura qué versión tiene cada usuario, base para decidir avance del canary.

---

## 11. Cierre

S7.6 te da las tres piezas operativas del auto-update de MSIX: el `.appinstaller` como contrato declarativo, el canary rollout determinista con cohortes monotónicas, y el rollback como "build+1 de la previa buena". Si tu pipeline las implementa correctamente, los releases dejan de ser un riesgo y pasan a ser una operación rutinaria que puedes hacer cada semana sin sustos.

Lo siguiente es [`S7.7 — Migración ClickOnce → MSIX`](../S7.7-migracion-clickonce-msix/MANUAL.md), el cierre teórico del bloque de distribución desktop: el plan paso a paso para migrar una app legacy con todos los detalles que aprendiste en S7.4 a S7.6.
