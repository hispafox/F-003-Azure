# Manual del alumno — S7.P2 · Práctica MSIX wizard

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell, despliegue por Portal. Este manual va antes: te cuenta qué hace por debajo el wizard "Create App Packages" de Visual Studio, dónde tiene sentido usarlo y dónde no, y cómo diagnosticar los seis errores típicos que aparecen la primera vez que empaquetas con UI.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M07-S7.P2](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P2-practica-msix-wizard-v1.md). Tres piezas de lógica pura (expansor de comandos CLI equivalentes al wizard, troubleshooter con catálogo de seis errores, advisor Wizard vs CLI).

*Creado: 2026-05-20 22:15 +0200*

---

## 1. La idea en una frase

S7.P empaqueta una app MSIX con CLI manual (`makeappx`, `signtool`, `Add-AppxPackage`). S7.P2 hace lo mismo desde la UI del wizard de Visual Studio: cero línea de comandos, treinta minutos, pensado para quien empieza. Pero el conocimiento sigue siendo el mismo: el wizard ejecuta exactamente los mismos comandos por debajo, solo te los oculta. El submódulo materializa esa equivalencia (qué CLI ejecuta el wizard, paso a paso), un catálogo de los seis errores que aparecen casi siempre la primera vez (con código, causa y fix), y la decisión de cuándo seguir con el wizard y cuándo migrar a CLI para casos avanzados (CI/CD, Key Vault, multi-arch).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican que la práctica final del módulo sea con wizard:

**Caso 1 — el "no entendí qué hizo el wizard"**. Un alumno completó la práctica S7.P (CLI manual) y le funcionó. Cuando intentó hacer un cambio menor unas semanas después, no recordaba qué comando estaba pulsando ni por qué. El wizard de Visual Studio le permitió **repetir el flujo sin recordar el CLI**, pero quería entender qué pasaba por debajo. El expansor del ejemplo le dice exactamente: cuatro comandos (`makeappx`, `signtool`, `Import-Certificate`, `Add-AppPackage`), en este orden, con estos parámetros.

**Caso 2 — `0x80073CFD` la primera vez que se instala**. Otro alumno generó el `.msix` con el wizard, intentó instalarlo con `Add-AppxPackage`, y recibió "**deployment failed with HRESULT 0x80073CFD**". Quince minutos buscando en Google sin resultado claro. El troubleshooter del ejemplo te dice en milisegundos: "0x80073CFD = el cert no está en `TrustedPeople` de LocalMachine. Diagnóstico: `Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object Subject -like '*MiEmpresa*'`. Fix: `Import-Certificate -FilePath cert.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople`". Treinta segundos en vez de quince minutos.

**Caso 3 — "el wizard ya no me vale, ¿qué hago?"**. Un alumno completó S7.P2 y montó un CI/CD para automatizar futuras releases. **El wizard no se puede invocar desde un pipeline**. Tampoco soporta firmar con un cert en Azure Key Vault, ni generar bundles multi-arquitectura. La conversación correcta: **el wizard es para aprendizaje y casos simples; para CI/CD y casos avanzados, pasa al CLI** que aprendiste en S7.P. El advisor del ejemplo te ayuda a tomar esa decisión con criterios objetivos.

Los tres casos los aborda el ejemplo: el expansor enseña el "detrás", el troubleshooter cataloga errores, y el advisor decide flujo.

---

## 3. Por qué esto importa en tu stack

Si tu primer contacto con MSIX es ahora, S7.P2 es probablemente el camino correcto: dos clicks, treinta minutos, app instalada. Pero saber qué hace el wizard por debajo te prepara para:

- **El día que necesites automatizar el flujo**: pasarse a CLI no será un salto traumático, será "ya conozco los cuatro comandos".
- **El día que algo falle**: en vez de mirar un mensaje genérico de Visual Studio, sabrás traducirlo a "el comando X falló por Y".
- **El día que la app crezca**: cuando empieces a necesitar multi-arch, Key Vault, AppInstaller, el wizard se queda corto y tienes que conocer los límites.

Tres preguntas a tener claras:

- **¿Cuándo wizard, cuándo CLI?** Aprendizaje + app simple → wizard. CI/CD, Key Vault, multi-arch, equipo grande, corporativo → CLI. Sin señales claras hacia ninguno → empieza con wizard y baja al CLI si te toca.
- **¿Qué hace el wizard por mí?** `makeappx pack` para empaquetar, `signtool sign` para firmar, `Import-Certificate` para confiar en el cert, `Add-AppPackage` para instalar. Si entiendes los cuatro, entiendes el wizard.
- **¿Cómo diagnosticar errores?** Los seis del catálogo cubren el 90% de los casos. Resto: Event Viewer → AppXDeploymentClient.

---

## 4. La analogía vertebradora: el mando del coche automático

Imagina dos coches que hacen lo mismo: van del punto A al B. El primero es de cambio automático, el segundo de cambio manual.

- **El automático** (wizard de Visual Studio): tres pedales (acelerador, freno, el wizard pone la marcha solo), un volante, listo. Te enseñas a conducir en una hora. Para tráfico urbano sencillo y trayectos cortos, perfecto.
- **El manual** (CLI: `makeappx`, `signtool`, etcétera): cinco marchas, embrague, decisiones sobre cuándo cambiar, control completo. Te enseñas a conducir en una semana. Para montaña, conducción deportiva, llevar carga pesada: mejor.

**Por debajo, los dos coches tienen el mismo motor, el mismo embrague, las mismas marchas**. La diferencia es la interfaz: el automático esconde el embrague y el cambio; el manual te los pone delante.

Hay dos preguntas naturales que ayudan a decidir cuál usar:

- **¿Estás aprendiendo a conducir?** El automático te quita complejidad inicial. Cuando ya conduces, puedes plantearte el manual.
- **¿Qué tipo de trayecto haces?** Tráfico urbano simple → automático va bien. Montaña, carga pesada, casos atípicos → manual da más control.

Eso es exactamente el debate Wizard vs CLI. **Aprendizaje + app simple → wizard.** **CI/CD, Key Vault, multi-arch → CLI.** Y siempre conviene saber que el motor es el mismo: si un día te baja la transmisión del automático, saber cómo funciona el manual te permite diagnosticar el problema.

Mantén la imagen: el wizard es el cambio automático que esconde el embrague; el CLI es manual que te lo pone delante. Conoce el motor para diagnosticar incidentes; usa el cambio que tu trayecto justifique.

---

## 5. Recorrido por el código

### `WizardComandosExpander.Expandir` — qué CLI ejecuta el wizard

La función central:

```csharp
public static IReadOnlyList<ComandoCli> Expandir(ParametrosWizard p) =>
[
    new(MakeAppx,
        $"makeappx.exe pack /d \"{p.BuildOutputDir}\" /p \"{p.OutputMsix}\"",
        "Empaqueta los artefactos de Release/x64 en un .msix."),
    new(SignTool,
        $"signtool.exe sign /fd SHA256 /a /f \"{p.CertPfx}\" \"{p.OutputMsix}\"",
        "Firma el .msix con el cert self-signed; Subject debe coincidir con Publisher."),
    new(ImportCertificate,
        $"Import-Certificate -FilePath \"{Path.ChangeExtension(p.CertPfx, ".cer")}\" " +
        "-CertStoreLocation Cert:\\LocalMachine\\TrustedPeople",
        "Marca el cert como trusted para que Windows acepte el .msix."),
    new(AddAppPackage,
        $"Add-AppPackage -Path \"{p.OutputMsix}\"",
        "Instala el .msix en el PC del usuario."),
];
```

Cuatro comandos, cuatro razones. Mirar este código con detenimiento es lo más cercano a entender el wizard. Y la primera vez que un alumno **automatice esto en un script PowerShell** (porque el wizard no se puede llamar desde batch), va a copiar exactamente estos cuatro comandos.

Detalle importante: **`signtool.exe sign /fd SHA256`** especifica que el digest del firmador es SHA-256. Sin el `/fd`, signtool usa el algoritmo por defecto que en versiones recientes ya es SHA-256, pero algunas configuraciones legacy usan SHA-1, que está deprecado para code signing. **Siempre `/fd SHA256` para no llevarse sorpresas**.

### `MsixErrorTroubleshooter.Diagnosticar` — los seis errores típicos

El catálogo:

| Código / mensaje | Causa | Fix |
| --- | --- | --- |
| **`0x80073CFD`** | El cert no está en `TrustedPeople` de LocalMachine | `Import-Certificate -FilePath cert.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople` |
| **`Add-AppPackage`** falla | Sideloading no habilitado | Settings → Privacy & security → For developers → activar Developer Mode |
| **`MSB3325`** | El `.pfx` no encuentra la password o la clave privada | Borrar cert del proyecto y re-crear con password vacía |
| **`NotSigned`** | Olvidaste firmar el `.msix` | Volver al wizard y seleccionar el cert en Signing |
| **`CannotRegister`** | Ya existe una versión con publisher distinto | `Get-AppPackage -Name '<n>' \| Remove-AppPackage`, luego `Add-AppPackage` |
| **`NoStartMenu`** | Instalación con error silencioso o icono no renderizado | `Remove-AppPackage` + `Add-AppPackage`; revisar Event Viewer → AppXDeploymentClient |

La función acepta **tanto el código exacto (`0x80073CFD`) como una cadena que lo contenga** (`"0x80073CFD: The current user has not consented..."`). Esto es deliberado: cuando un alumno te pasa el mensaje de error completo, no quieres pedirle que extraiga el código.

```csharp
public static DiagnosticoError? Diagnosticar(string codigoOMensaje)
{
    if (Catalogo.TryGetValue(clave, out var exacto))
        return exacto;

    foreach (var (k, v) in Catalogo)
        if (codigoOMensaje.Contains(k, StringComparison.OrdinalIgnoreCase))
            return v;

    return null;   // código desconocido
}
```

Si el código es desconocido, devuelve `null`. El endpoint `/wizard/troubleshoot` devuelve 404 en ese caso, indicando "no tengo entrada para esto, mira Event Viewer".

### `WizardVsCliAdvisor.Recomendar` — la decisión Wizard vs CLI

La función de decisión:

```csharp
if (c.PipelineCiCd) razones.Add("Pipeline CI/CD → todo CLI versionable.");
if (c.CertDesdeKeyVault) razones.Add("Cert desde Azure Key Vault → wizard no lo soporta.");
if (c.MultiArquitectura) razones.Add("Multi-arch → wizard limitado.");
if (c.EquipoGrande) razones.Add("Equipo grande → CLI reproducible y revisable.");
if (c.DistribucionCorporativa) razones.Add("Distribución corporativa con AppInstaller → CLI.");

if (razones.Count > 0)
    return new RecomendacionFlujo(FlujoEmpaquetado.Cli, razones);

// ... resto: si no hay razones para CLI, wizard
```

**Cualquier señal "senior"** (CI/CD, Key Vault, multi-arch, equipo grande, corporativo) empuja directamente a CLI. La razón: el wizard tiene **límites concretos** que el ejemplo enumera:

```csharp
public static IReadOnlyList<string> LimitacionesWizard { get; } =
[
    "Cert: solo self-signed o de cert store; no Azure Key Vault ni HSM externo.",
    "Multi-arch: un .msix por arquitectura, sin bundle .msixbundle.",
    "Sin AppInstaller con auto-update integrado.",
    "Sin firma con timestamping RFC 3161 personalizado.",
    "Sin modificación avanzada del manifest (capabilities restringidas, extensiones).",
];
```

Si tu proyecto pega contra cualquiera de estas cinco limitaciones, no insistas con el wizard. Migra al CLI de S7.P y modifica lo que haga falta.

### `PracticaMsixWizardPlanner` — el plan + checklist

El servicio inyectable que combina los anteriores. Recibe el contexto del alumno, recomienda Wizard o CLI con razones, expande los comandos CLI equivalentes (útil para alumnos curiosos), incluye el catálogo de errores como referencia y la checklist de 11 ítems.

---

## 6. Los 8 pasos del wizard (slides 4-10)

Para quien va a hacer la práctica:

1. **Crear WPF mínima** en VS 2022.
2. **Add → New Project → Windows Application Packaging Project**. Set as Startup.
3. **Inspeccionar `Package.appxmanifest`**: el wizard ha generado `Empresa.App` como Identity, `CN=` como Publisher de prueba. Personaliza si quieres.
4. **Right-click Packaging → Publish → Create App Packages**: Sideloading → Generate self-signed → Build.
5. **Importar el cert** generado en `Cert:\LocalMachine\TrustedPeople` (un PowerShell elevado lo hace en una línea).
6. **`Add-AppPackage`** del `.msix` generado en `bin\Release\AppPackages\...`.
7. **App en Start Menu**: arranca y debería mostrar la versión configurada.
8. **Cambiar versión a 1.0.1.0**, rebuild, reinstalar (in-place: los datos del usuario se mantienen).

A la hora de hacer la práctica, ten **el endpoint `/wizard/expandir`** abierto: cuando completes el paso 4 (Build), el wizard genera el `.msix`. La función te muestra qué comandos CLI son equivalentes a lo que pulsaste, así internalizas el flujo.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/WizardMsix.Demo.Api
# http://localhost:5104
```

Endpoints:

```http
### Expandir los comandos CLI que el wizard ejecuta
POST http://localhost:5104/wizard/expandir
Content-Type: application/json

{
  "empresa": "Acme",
  "app": "MiDemo",
  "version": "1.0.0.0",
  "buildOutputDir": "bin/x64/Release/MiDemo.Package",
  "certPfx": "MiDemo.pfx",
  "outputMsix": "MiDemo_1.0.0.0_x64.msix"
}
# → [4 comandos con razones]

### Diagnosticar un error
GET http://localhost:5104/wizard/troubleshoot?codigoOMensaje=0x80073CFD
# → { codigo, causa, diagnostico, fix }

GET http://localhost:5104/wizard/troubleshoot?codigoOMensaje=foo-no-existe
# → 404

### Listar todos los errores del catálogo
GET http://localhost:5104/wizard/errores

### Decidir Wizard vs CLI
POST http://localhost:5104/wizard/elegir
Content-Type: application/json

{
  "aprendizajeInicial": true,
  "appSimpleSingleArch": true,
  "pipelineCiCd": false,
  "certDesdeKeyVault": false
}
# → Wizard con razones

POST http://localhost:5104/wizard/elegir
{ "pipelineCiCd": true, "certDesdeKeyVault": true }
# → Cli con razones

### Limitaciones del wizard
GET http://localhost:5104/wizard/limitaciones
```

Los 31 tests cubren los cuatro comandos exactos del expansor (con espacios y comillas), el lookup del troubleshooter por código exacto y por contención, la decisión Wizard vs CLI con cada combinación, las cinco limitaciones.

Para preflight y cleanup en tu Windows:

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-check-vs-components.ps1 → verifica VS 2022 + workload de empaquetado
# 2) 02-cleanup.ps1 -PackageName MiPrimeraMSIX.Package -CertSubjectContiene MsixDemo
#                  → INTERACTIVO con confirmación: Remove-AppPackage + borra cert
```

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y PowerShell `pwsh`. La práctica real (WPF + WAP en Visual Studio) la haces tú.

---

## 8. La excepción a la regla "scripts solo lectura"

Una particularidad de este submódulo: el script `02-cleanup.ps1` es **la única excepción de M07** a la regla "scripts solo lectura". La razón: al terminar la práctica, el alumno tiene en su Windows un paquete instalado y un certificado en `TrustedPeople`. Ambos son artefactos de práctica que conviene quitar. El script lo hace, pero:

- **Es interactivo**: pide confirmación antes de cada `Remove-AppPackage` y antes de cada borrado de cert.
- **Es opcional**: el alumno puede no ejecutarlo y limpiar manualmente.
- **Es local**: solo afecta al PC del alumno, no a Azure.

En el resto de M07 (e M06, M05...) los scripts no crean ni borran nada en Azure. La instalación de un paquete MSIX y la importación de un cert son los únicos efectos "modificadores" que la práctica deja en el PC del alumno.

---

## 9. La decisión Wizard vs CLI, en una tabla

Para tener clara la decisión:

| Característica | Wizard de VS | CLI manual |
| --- | --- | --- |
| **Conocimiento requerido** | Cero. Es UI. | Saber `makeappx`, `signtool`, `Add-AppPackage`. |
| **Tiempo primera vez** | 30-45 min | 75-90 min |
| **Reproducible en pipeline** | No | Sí (PowerShell o GitHub Actions) |
| **Cert desde Azure Key Vault** | No | Sí (con `AzureSignTool`) |
| **Multi-arquitectura (bundle)** | Un `.msix` por arch | Sí (con `MakeAppx bundle`) |
| **Timestamping RFC 3161** | Por defecto, sin opciones | Configurable |
| **AppInstaller** | Manual posterior | Integrable en el script |
| **Revisable en PR** | No (es UI) | Sí (script en repo) |
| **Cuándo usarlo** | Aprendizaje, app simple, demos | Producción, CI/CD, casos avanzados |

---

## 10. Glosario breve

- **Wizard "Create App Packages"**: la UI de Visual Studio que empaqueta una app como MSIX desde el menú contextual del Packaging Project.
- **`makeappx.exe pack`**: comando del Windows SDK que empaqueta una carpeta en un `.msix`.
- **`signtool.exe sign`**: comando del Windows SDK que firma un `.msix` con un certificado.
- **`Import-Certificate`**: cmdlet de PowerShell que importa un cert (`.cer`) a un cert store de Windows.
- **`Add-AppPackage` / `Add-AppxPackage`**: cmdlet que instala un `.msix` en el Windows actual.
- **`Remove-AppPackage`**: cmdlet que desinstala una app MSIX.
- **`Get-AppPackage`**: cmdlet que lista las apps MSIX instaladas.
- **HRESULT**: códigos de error de Windows en formato `0x80073CFD`. El catálogo del troubleshooter los traduce a causa+fix.
- **Sideloading**: instalación de un MSIX desde un archivo (no desde Store).
- **Developer Mode**: ajuste en Settings → Privacy & security → For developers que habilita sideloading.
- **`AzureSignTool`**: utilidad open source que reemplaza a `signtool` para firmar con cert en Azure Key Vault.

---

## 11. Cierre del módulo M07

Con S7.P2 completas el módulo de integración y MSIX. Resumen de lo aprendido:

- **S7.1 a S7.3**: integración backend (Service Bus avanzado, event-driven, API Management).
- **S7.4 a S7.7**: distribución desktop (ClickOnce vs MSIX, empaquetado, auto-update, migración).
- **S7.P y S7.P2**: prácticas integradoras (CLI manual y wizard de VS).

Si te quedas con una sola cosa de M07: **para integración entre servicios, escoge bien el tipo de mensajería (cola, topic, event grid, hubs) según el caso; para distribución desktop, MSIX es el futuro y el wizard de VS es la entrada amable, pero el CLI manual te da el control para CI/CD y casos avanzados**.

Lo siguiente es M08 — DevOps y Automatización (Azure DevOps, pipelines YAML, IaC con Bicep, Application Insights), donde verás cómo automatizar todo lo que has hecho a mano en los siete módulos anteriores.
