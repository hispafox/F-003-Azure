# Manual del alumno — S7.5 · MSIX empaquetado y distribución

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell, despliegue por Portal. Este manual va antes: te cuenta por qué el `Package.appxmanifest` es la identidad legal del paquete, cómo se calcula el nombre del archivo final, qué canal de distribución elegir según tu audiencia, y por qué la clave privada del certificado nunca debería salir de Azure Key Vault.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M07-S7.5](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.5-msix-empaquetado-distribucion-v3.md). Tres piezas de lógica pura (validador del manifest con regex de identity name y reglas de capabilities restringidas, generador de nombres y versionado de pipeline, advisor de canal según escenario).

*Creado: 2026-05-20 20:55 +0200*

---

## 1. La idea en una frase

Empaquetar una app como MSIX es **escribir un manifest XML que declara la identidad del paquete**, generar un `.msix` firmado, y subirlo a un canal de distribución (Microsoft Store, AppInstaller en Azure Blob, Intune, winget). Cada paso tiene reglas: el manifest debe cumplir un formato estricto (`Identity.Name = Empresa.NombreApp`, `Publisher = CN=...` coincidente con el certificado, `Version = Major.Minor.Build.Revision` siempre creciente), el nombre del archivo se compone determinísticamente (`Empresa.NombreApp_2.4.1.0_x64.msix`), y el canal se elige según el público (Store para masivo, Intune para corporativo gestionado, AppInstaller para sideload con auto-update).

El submódulo materializa estas tres decisiones como lógica pura. El empaquetado real (`msbuild`, `signtool`, `MakeAppx`) requiere Windows SDK y una clave privada — eso se valida a mano en una máquina con tooling instalado. Lo que aquí se prueba es la lógica que **debe ser correcta antes de empezar**.

---

## 2. El problema real que hay detrás

Tres situaciones típicas que justifican validar antes de empaquetar:

**Caso 1 — el manifest con `Identity.Name` mal escrito.** Un equipo empaquetó su app con `Identity.Name = MiEmpresa-MiApp`. El build pasó. El install en local funcionó. Cuando intentaron publicar a Microsoft Store, **rechazo**: el `Identity.Name` solo acepta caracteres alfanuméricos con puntos como separadores (`MiEmpresa.MiApp`, no guiones). Tuvieron que regenerar el manifest, reempaquetar, refirmar, regenerar el `.appinstaller`. Una validación pre-build de 30 segundos les habría ahorrado dos horas.

**Caso 2 — el `Publisher` que no coincidía con el certificado.** Otro equipo configuró el manifest con `Publisher = CN=MiEmpresa S.L.` pero el certificado emitido por la CA empresarial tenía `Subject = CN=MiEmpresa, O=MiEmpresa S.L.`. `signtool` firmó el paquete; al instalarlo, Windows rechazó con error críptico ("the publisher of an app package does not match the publisher of the certificate"). **Las dos cadenas deben ser idénticas, byte a byte.** El validador del ejemplo lo detecta antes de mandar el build.

**Caso 3 — la `Version` no incremental.** Una pipeline subió un build con `Version = 2.4.0.5` después de uno con `Version = 2.4.0.7`. Windows no actualizó: rechaza versiones menores. Los usuarios siguieron con la 2.4.0.7 sin saberlo, sin notificación. El bug se descubrió cuando alguien quiso reproducir un bug "que ya estaba arreglado" en la 2.4.0.5. La lección: **la versión siempre crece** (mejor todavía, los tres últimos componentes derivan del buildId, así nunca retrocedes).

Los tres casos los previene el validador del ejemplo. Y el cuarto, más sutil: capabilities restringidas (`runFullTrust`, `broadFileSystemAccess`) declaradas sin el namespace `rescap:` — el paquete pasa la firma pero falla en Store con un mensaje cualquiera.

---

## 3. Por qué esto importa en tu stack

Si vas a empaquetar como MSIX cualquier app —ahora o en los próximos seis meses—, tres preguntas que tu validación local debería responder antes de subir nada a Azure:

- **¿El `Package.appxmanifest` es válido contra las reglas reales?** Identity name con formato correcto, Publisher con CN correcto, Version Major.Minor.Build.Revision, ProcessorArchitecture soportada, TargetMinVersion >= 10.0.17763.0, capabilities restringidas declaradas con `rescap:`. Sin la validación pre-build, descubres los errores en sesión interactiva con `signtool`.
- **¿La nueva versión es mayor que la última publicada?** La regla más simple del mundo y la que más se rompe en pipelines mal montados. La validación `EsIncremental(anterior, nueva)` es una línea y previene horas de soporte.
- **¿Qué canal toca para mi audiencia?** Store, AppInstaller, Intune, winget — cada uno tiene un coste operativo y una experiencia de usuario distinta. Confundirlos es habitual hasta que has hecho una distribución completa con cada uno.

Si tienes claras las respuestas, MSIX es una herramienta sólida. Sin ellas, tu primera distribución va a ser una cadena de errores de "el manifest está mal", "el cert no coincide", "la versión no es válida", cada uno descubierto en un build distinto.

---

## 4. La analogía vertebradora: el pasaporte y el embarque

Imagina que vas a embarcar a un vuelo internacional. Tu pasaporte es la **identidad oficial** del paquete que eres tú:

- Tu **nombre** en el pasaporte tiene un formato exacto (`APELLIDOS, NOMBRE`). Si está mal, la aerolínea no te deja embarcar. Eso es el **Identity.Name** del manifest: `Empresa.NombreApp`, alfanumérico con puntos.
- El **país emisor** del pasaporte debe coincidir con el visado de entrada. Si el visado lo emitió USA pero tu pasaporte dice México, hay incoherencia. Eso es el **Publisher** del manifest contra el **Subject** del certificado: deben coincidir exactamente.
- La **fecha de vencimiento** y el **número** del pasaporte se actualizan cada renovación. Cada versión nueva del pasaporte tiene un número incremental. Eso es la **Version** del manifest: cuatro números crecientes.
- La **fecha de nacimiento** y la **nacionalidad** no cambian con renovaciones. Esos campos identifican estable mente al titular. Eso es **TargetDeviceFamily MinVersion**: la versión mínima del SO requerida (Windows 10 1809 / 10.0.17763).

Y al embarcar, hay **distintos terminales** según el destino:

- **Terminal Internacional** (Microsoft Store): para vuelos a cualquier parte. Tarifa estándar, controles estrictos, mucha visibilidad. **Audiencia pública**.
- **Terminal Corporativo** (Intune): para empleados de empresas con acuerdos especiales. Embarque silencioso, gestión centralizada por IT. **Audiencia interna gestionada**.
- **Terminal Sideload** (AppInstaller): para grupos pequeños con acceso a un hangar privado. Funciona si la aerolínea (tu empresa) te lo permite (sideloading habilitado). **Distribución manual controlada**.
- **Terminal Comercial** (winget): para viajeros frecuentes con su propio sistema de descuentos. Bypass de filas largas. **Power users y developers**.

Las cuatro terminales coexisten. Cada vuelo (cada release) elige una o varias según el destino (la audiencia). Mantén la imagen: el manifest es el pasaporte; los canales son las terminales; el certificado es el visado que la terminal de seguridad valida antes de dejarte pasar.

---

## 5. Recorrido por el código

### `AppxManifestValidator.Validar` — el inspector de pasaportes

La función central. Recibe un `AppxManifest` parseado y devuelve un `ResultadoValidacion` con todos los problemas:

```csharp
// Identity.Name — formato Empresa.NombreApp.
if (!IdentityNameRegex().IsMatch(m.IdentityName))
    p.Add($"Identity.Name '{m.IdentityName}' no cumple el formato 'Empresa.NombreApp'.");

// Publisher — debe empezar por CN= (Subject del certificado).
if (!m.Publisher.StartsWith("CN=", StringComparison.Ordinal))
    p.Add($"Publisher '{m.Publisher}' debe empezar por 'CN='...");

// Version — Major.Minor.Build.Revision.
if (!VersionRegex().IsMatch(m.Version))
    p.Add($"Version '{m.Version}' no es Major.Minor.Build.Revision.");

// ProcessorArchitecture — x64 / arm64 / neutral / x86.
if (m.ProcessorArchitecture is not ("x64" or "arm64" or "neutral" or "x86"))
    p.Add($"ProcessorArchitecture '{m.ProcessorArchitecture}' no soportada...");

// TargetDeviceFamily MinVersion ≥ Windows 10 1809.
if (v < Version.Parse("10.0.17763.0"))
    p.Add($"TargetDeviceFamily MinVersion '{m.TargetMinVersion}' < 10.0.17763.0...");

// Capabilities restringidas → exigen namespace rescap:.
foreach (var cap in m.Capabilities)
    if (CapacidadesRestringidas.Contains(cap))
        p.Add($"Capability '{cap}' es restringida: declárala con 'rescap:'.");
```

Las seis comprobaciones que aprendes a leer de carrerilla:

1. **Identity.Name** debe cumplir la regex `^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$`. Lo que esto significa: empieza por letra, contiene alfanuméricos, hay al menos un punto. Lo que NO vale: guiones, espacios, empezar por número, sin punto.
2. **Publisher** debe empezar por `CN=`. Y la cadena entera debe coincidir **byte a byte** con el `Subject` del certificado que vas a usar para firmar. Si en el cert ves `CN=Acme Corp` y en el manifest pones `CN=Acme corp` (minúscula), no firma.
3. **Version** = cuatro componentes separados por puntos, todos enteros. `2.4.1.0` vale; `2.4.1` no vale; `2.4.1.0-rc1` no vale.
4. **ProcessorArchitecture** ∈ {`x64`, `arm64`, `neutral`, `x86`}. La moderna por defecto es `x64`; para Surface Pro X y similares, `arm64`. `neutral` es para apps puramente .NET sin código nativo.
5. **TargetMinVersion** ≥ Windows 10 versión 1809 (`10.0.17763.0`). Es el mínimo que soporta MSIX bien. Para apps modernas, suele ponerse 1903 o más reciente.
6. **Capabilities** restringidas (`runFullTrust`, `broadFileSystemAccess`, `allAppMods`, `enterpriseDataPolicy`) requieren el namespace `rescap:` en el XML. Sin él, el paquete pasa la firma pero falla al validarse contra Microsoft Store.

`runFullTrust` merece mención especial: significa "esta app NO corre en sandbox; tiene acceso completo al sistema". Es lo que necesitan las apps WPF/WinForms empaquetadas con MSIX (el código se diseñó sin restricciones). Está permitida pero **el Microsoft Store la audita más estrictamente**.

### `AppxManifestValidator.Parsear` — leer el XML

```csharp
public static AppxManifest Parsear(string xml)
{
    var doc = XDocument.Parse(xml);
    var root = doc.Root!;
    var identity = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Identity")!;
    var dependencies = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Dependencies");
    var capabilities = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Capabilities");

    return new AppxManifest(
        IdentityName: identity.Attribute("Name")?.Value ?? "",
        Publisher: identity.Attribute("Publisher")?.Value ?? "",
        Version: identity.Attribute("Version")?.Value ?? "",
        ProcessorArchitecture: identity.Attribute("ProcessorArchitecture")?.Value ?? "neutral",
        TargetMinVersion: dependencies?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "TargetDeviceFamily")?
            .Attribute("MinVersion")?.Value ?? "",
        Capabilities: capabilities?.Elements()
            .Where(e => e.Name.LocalName == "Capability")
            .Select(e => e.Attribute("Name")?.Value ?? "")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList() ?? []);
}
```

Detalle importante: el manifest XML usa **varios namespaces** (`foundation`, `uap`, `rescap`, `desktop`...). El parser **busca por LocalName** y no por namespace, lo que lo hace robusto a variaciones. Si un día Microsoft añade un namespace nuevo, el parser sigue funcionando.

### `PackageNamingResolver` — el nombre del archivo

Tres funciones que cubren tres necesidades del pipeline:

```csharp
public static string NombreArchivo(AppxManifest m) =>
    $"{m.IdentityName}_{m.Version}_{m.ProcessorArchitecture}.msix";

public static string NombreBundle(string identityName, string version) =>
    $"{identityName}_{version}.msixbundle";

public static string SiguienteVersion(string actual, int buildId)
{
    var partes = actual.Split('.');
    return $"{partes[0]}.{partes[1]}.{buildId}.0";   // 2.4.{buildId}.0
}

public static bool EsIncremental(string anterior, string nueva) =>
    Version.Parse(nueva) > Version.Parse(anterior);
```

Tres conceptos clave:

- **Nombre del archivo `.msix`**: estrictamente `{IdentityName}_{Version}_{Arch}.msix`. Si tu cert firma con `Publisher = CN=Acme` y el `Identity.Name = Acme.MiApp`, el archivo es `Acme.MiApp_2.4.1.0_x64.msix`. Microsoft Store y AppInstaller esperan exactamente este formato.
- **`.msixbundle` multi-arquitectura**: cuando empaquetas para x64 y arm64 en el mismo release, los combinas en un bundle. El nombre lleva versión pero no arch (porque el bundle contiene varias). El cliente baja del bundle solo la arch que necesita.
- **Versionado de pipeline**: el patrón típico es `Major.Minor.{buildId}.0`. `Major.Minor` los mantiene el equipo (release notes); `buildId` viene del CI (número del pipeline, GitHub run ID, lo que sea); `Revision` queda en 0 (se usa solo en hotfixes). Así nunca te peleas con números a mano, y siempre son crecientes.
- **`EsIncremental`**: la red de seguridad. Antes de subir una nueva versión, comprueba que es mayor que la anterior. Si no, la pipeline aborta.

### `DistributionChannelAdvisor.Recomendar` — qué canal para tu escenario

La función decide entre los cuatro canales según las banderas del escenario:

```csharp
if (e.AudienciaPublica)
{
    canales.Add(MicrosoftStore);
    if (e.DeveloperPowerUsers) canales.Add(Winget);
}
else
{
    if (e.MdmIntune) canales.Add(Intune);
    if (e.HostingAzureBlob && e.AutoUpdateNecesario) canales.Add(AppInstaller);
    if (canales.Count == 0) canales.Add(AppInstaller);   // default corporativo
}
```

Cuatro casos típicos:

- **App pública para todo el mundo** → Microsoft Store. Distribución masiva, gestión de versiones por Store, ratings, reviews. Tarda una semana en publicarse pero luego es automático.
- **App pública para developers/power users** → Store + winget. Mismo paquete, dos caminos: Store para usuarios normales, winget para los que prefieren CLI. **Winget hereda del paquete del Store**, no hay que mantener dos versiones.
- **App corporativa con Intune** → Intune. Despliegue silencioso, inventario, asignación por grupos AAD. La opción correcta cuando IT tiene Intune funcionando.
- **App corporativa sin Intune** → AppInstaller en Azure Blob. Sideload manual o por GPO, auto-update gestionado por el `.appinstaller`.

### `DistributionChannelAdvisor.PoliticaPorDefecto` — el `.appinstaller`

La política de auto-update para AppInstaller:

```csharp
public static PoliticaAutoUpdate PoliticaPorDefecto() =>
    new(HoursBetweenUpdateChecks: 1,
        ShowPrompt: true,
        AutomaticBackgroundTask: true,
        ForceUpdateFromAnyVersion: true);
```

Cuatro flags que se traducen a propiedades del `<UpdateSettings>` del `.appinstaller`:

- **`HoursBetweenUpdateChecks = 1`**: al abrir la app, comprueba si hay actualización (con un grace de una hora). El default razonable: una comprobación por sesión.
- **`ShowPrompt = true`**: si hay actualización, muestra al usuario un diálogo "¿actualizar ahora?". Sin él, la actualización es silenciosa (más cómoda pero menos transparente).
- **`AutomaticBackgroundTask = true`**: el SO comprueba en background cada cierto tiempo, no solo al abrir. Útil para apps que el usuario abre raramente.
- **`ForceUpdateFromAnyVersion = true`**: salta versiones. Si el usuario está en 2.4.1.0 y la última es 2.4.5.0, instala directamente la 2.4.5.0 sin pasar por las intermedias.

Los detalles se cubren a fondo en S7.6.

---

## 6. La clave privada vive en Key Vault

Una decisión operativa que merece sección propia: **la clave privada del certificado de firma nunca debería estar en el repositorio**. Tres caminos posibles, ordenados de peor a mejor:

**Camino malo — clave en el repo**. El `.pfx` con la clave privada está en `src/certs/firma.pfx`. La contraseña del PFX está en `.env` o en una variable de pipeline. **Cualquier developer con acceso al repo tiene la clave**. Si se filtra, hay que rotar el certificado y republicar todas las versiones.

**Camino intermedio — clave en agente de pipeline**. La clave está en el agente del CI (variable secret de GitHub Actions, Azure DevOps secret), no en el repo. Mejor, pero la clave aún sale del Key Vault al agente cuando se firma. Un agente comprometido = clave comprometida.

**Camino bueno — clave vive en Key Vault, firma con `AzureSignTool`**. La clave nunca sale del Key Vault. El pipeline llama a `AzureSignTool sign --file MiApp.msix --kv-uri https://...` y AzureSignTool firma usando la Managed Identity para autenticarse contra Key Vault. La clave hace su trabajo dentro del Vault; la firma resultante se devuelve al agente. **La clave es inextraíble.**

El pipeline real:

```yaml
- name: Build MSIX
  run: msbuild MiApp.wapproj /p:Configuration=Release

- name: Sign MSIX with AzureSignTool
  run: |
    AzureSignTool sign \
      --file MiApp_2.4.1.0_x64.msix \
      --kv-uri https://mivault.vault.azure.net \
      --kv-cert-name FirmaMsix \
      --azure-managed-identity \
      --timestamp-rfc3161 http://timestamp.digicert.com

- name: Upload to Blob
  run: az storage blob upload --account-name miapp ...
```

Tres líneas que ahorran muchos incidentes futuros. Y bastante coherente con la lección de S6.6: **secretos en Key Vault, MI accede, código limpio**.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Msix.Demo.Api
# http://localhost:5100
```

Endpoints:

```http
### Parsear un manifest
POST http://localhost:5100/msix/parsear
Content-Type: application/xml

<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="...">
  <Identity Name="Acme.MiApp" Publisher="CN=Acme Corp"
            Version="2.4.1.0" ProcessorArchitecture="x64" />
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0"
                        MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
# → AppxManifest parseado

### Validar
POST http://localhost:5100/msix/validar
Content-Type: application/json

{
  "identityName": "Acme.MiApp",
  "publisher": "CN=Acme Corp",
  "version": "2.4.1.0",
  "processorArchitecture": "x64",
  "targetMinVersion": "10.0.17763.0",
  "capabilities": ["runFullTrust"]
}
# → { valido: false, problemas: ["Capability 'runFullTrust' es restringida..."] }

### Nombre del archivo
GET http://localhost:5100/msix/nombre?identityName=Acme.MiApp&version=2.4.1.0&arch=x64
# → "Acme.MiApp_2.4.1.0_x64.msix"

### Siguiente versión del pipeline
GET http://localhost:5100/msix/version-siguiente?actual=2.4.0.0&buildId=42
# → "2.4.42.0"

### Recomendar canal
POST http://localhost:5100/msix/distribucion
Content-Type: application/json

{ "audienciaPublica": true, "developerPowerUsers": true }
# → { canales: ["MicrosoftStore", "Winget"], razones: [...] }
```

Los 34 tests cubren todas las reglas del validator (cada campo del manifest, capabilities restringidas, target version mínima), el versionado incremental, todos los escenarios del advisor.

Para validar tu Windows local:

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-validate-manifest.ps1 → valida un Package.appxmanifest local
# 2) 02-tooling-check.ps1     → comprueba signtool, MakeAppx, AzureSignTool
```

Te dice si tienes el tooling necesario para empaquetar de verdad. Si no, el script te orienta a instalar Windows SDK + `AzureSignTool` por `dotnet tool install`.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y PowerShell `pwsh` para los scripts.

---

## 8. Los anti-patterns del slide 28

Cinco errores caros que conviene tener en mente:

**Anti-pattern 1 — Escrituras a `HKLM` o `C:\Program Files`**. El sandbox de MSIX redirige las escrituras a estas rutas a un **VFS (Virtual File System)** dentro del contenedor. Tu app cree que está escribiendo, pero los cambios no persisten al desinstalar y otras apps no los ven. La forma correcta: `ApplicationData.Current.LocalFolder` para datos por usuario, `Package.Current.InstalledLocation` para leer archivos de la propia app.

**Anti-pattern 2 — Cambiar `Identity.Name` o `Publisher` entre versiones**. Para Windows, eso es **una app nueva**, no una actualización. Los usuarios verán la app vieja Y la nueva instaladas. Mantén estos campos estables por la vida del producto.

**Anti-pattern 3 — No firmar el paquete (ni en dev)**. Un MSIX sin firma no se puede instalar en Windows con UAC restrictivo. El warning rojo de SmartScreen es solo el principio. Firma siempre, incluso en dev con self-signed.

**Anti-pattern 4 — Empaquetar solo para x64**. Hoy en día hay Surface Pro X y otros Windows on ARM. Si tu app es solo x64, no se puede instalar ahí. La opción correcta: empaqueta x64 + arm64 en un `.msixbundle`. Si tu app es .NET sin código nativo, usa `neutral` y un único paquete.

**Anti-pattern 5 — Distribuir por OneDrive o share casero**. El `.appinstaller` y el `.msix` deben servirse con **Content-Type correcto** y **HTTPS**. OneDrive y SharePoint añaden cabeceras que rompen la descarga. Usa Azure Blob (acceso público de blob), CDN de Azure por encima, o Intune.

---

## 9. Glosario breve

- **`Package.appxmanifest`**: el XML que declara la identidad del paquete. Se genera por el WAP y se firma con el resto del contenido.
- **`Identity.Name`**: identificador único de la app (`Empresa.NombreApp`). Inmutable por la vida del producto.
- **`Publisher`**: emisor de la firma. Debe coincidir byte a byte con el `Subject` del certificado.
- **`Version`**: cuatro números crecientes. La regla "siempre incremental" es absoluta.
- **`ProcessorArchitecture`**: x64, arm64, neutral, x86. Define para qué arquitectura está compilado.
- **`TargetDeviceFamily MinVersion`**: la versión mínima del SO. Estándar: 10.0.17763 (Windows 10 1809).
- **Capability**: permiso declarado en el manifest. Las **restringidas** (`runFullTrust`, etc.) requieren namespace `rescap:`.
- **`.msix`**: archivo final de un paquete para una sola arquitectura.
- **`.msixbundle`**: archivo que combina varios `.msix` (x64 + arm64). El cliente baja solo el que necesita.
- **`.appinstaller`**: XML que apunta a un `.msix` en un servidor y configura auto-update.
- **WAP** (Windows Application Packaging): tipo de proyecto en VS para empaquetar como MSIX.
- **`MakeAppx`**: herramienta del Windows SDK para crear `.msix` desde una carpeta o un bundle desde varios `.msix`.
- **`signtool`**: herramienta de Microsoft para firmar paquetes con un certificado local.
- **`AzureSignTool`**: alternativa para firmar usando claves en Azure Key Vault (sin extraer la clave).
- **VFS** (Virtual File System): redirección que el sandbox aplica a escrituras en rutas protegidas (`HKLM`, `Program Files`).
- **AppInstaller (la app)**: app integrada en Windows que abre `.msix` y `.appinstaller` con una UI estándar.

---

## 10. Cierre

S7.5 es donde la teoría de S7.4 se vuelve operativa: hay un manifest que validar con reglas precisas, hay un nombre de archivo que respetar, hay canales de distribución que elegir según la audiencia, y hay una clave privada que **vive en Key Vault**, no en el repo. Si tu pipeline implementa estas cuatro cosas, la primera distribución de tu app MSIX va a salir sin sustos.

Lo siguiente es [`S7.6 — MSIX auto-update`](../S7.6-msix-auto-update/MANUAL.md), donde el `.appinstaller` se vuelve protagonista: políticas de actualización, canary releases, rollback ante incidentes.
