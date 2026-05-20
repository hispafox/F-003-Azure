# Manual del alumno — S7.P · Práctica MSIX end-to-end

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell, despliegue por Portal. Este manual va antes: te cuenta qué construye la práctica integradora del módulo, por qué el orden de los 8 pasos importa, dónde está el error #1 (el match Publisher↔Subject del certificado) y cómo se valida cada paso antes de avanzar al siguiente.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M07-S7.P](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P-practica-msix-v3.md). Tres piezas de lógica pura (máquina de los 8 pasos con criterios, validador de coincidencia Publisher/Cert, generador de manifest y `.appinstaller` canónicos).

*Creado: 2026-05-20 21:55 +0200*

---

## 1. La idea en una frase

La práctica integradora de M07 toma todo lo que aprendiste en S7.4 a S7.7 y lo aplica a una app WPF mínima: **crear el proyecto WPF + WAP, configurar el manifest, generar un certificado self-signed que coincida con el Publisher, firmar el `.msix`, instalarlo, simular una actualización, y configurar `.appinstaller` con auto-update**. Los 8 pasos están bien delimitados (25-30 minutos según las slides, 75-90 realistas para alguien que lo hace por primera vez), con criterios de validación testeables, y un error central que merece sección propia: si el `Subject` del certificado no coincide *byte a byte* con el `Publisher` del manifest, Windows rechaza el paquete con un mensaje críptico ("package signature hash validation failed"). La práctica te orienta para evitarlo.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican guiar la práctica con criterios testeables:

**Caso 1 — el certificado que no coincidía con el manifest.** Un alumno generó un cert self-signed con `Subject = CN=MiEmpresa Corp` y configuró el manifest con `Publisher = CN=MiEmpresa Corp.` (con un punto al final). El build pasó. La firma pasó. **`Add-AppxPackage` falló con "package signature hash validation failed"**. Dos horas de debugging para descubrir que el punto extra de `Corp.` era el problema. La validación de coincidencia exacta del ejemplo evita estos casos en milisegundos: si pasas los dos strings al check, te dice si coinciden y por qué no.

**Caso 2 — la actualización in-place que perdió datos del usuario.** Otro alumno simuló una actualización 1.0.0.0 → 1.0.1.0 sin tener en cuenta que la app guardaba su configuración en `ApplicationData.Current.LocalFolder`. Tras `Add-AppxPackage` del .msix nuevo, la app **perdió todas sus preferencias**. La causa: había cambiado el `Identity.Name` entre versiones por un descuido. Windows vio una app distinta y empezó desde cero. Cuando hubiera dejado el `Identity.Name` estable, el `LocalFolder` se preserva entre actualizaciones.

**Caso 3 — el `.appinstaller` que no descargaba.** Un equipo configuró un `.appinstaller` apuntando al `.msix` en un share de red interno. Funcionaba al abrirlo a mano (doble click instala). Pero cuando configuraban auto-update y la app comprobaba updates, **silenciosamente fallaba**. La causa: el share devolvía Content-Type `application/octet-stream`. Para `.appinstaller`, Windows exige `application/appinstaller`; para `.msix`, exige `application/msix`. Sin esos Content-Type, no se descarga. El ejemplo no resuelve esto (es un detalle de configuración del servidor) pero el README lo documenta.

Los tres casos los enseña la práctica: el orden de los pasos, los criterios de validación y los artefactos canónicos te dan la red de seguridad.

---

## 3. Por qué esto importa en tu stack

Si nunca has empaquetado una app como MSIX, esta práctica es la primera vez que vas a tocar **el manifest real, el cert real, `signtool` real, `Add-AppxPackage` real**. Y si ya has empaquetado, los criterios de validación de la práctica son la lista que conviene tener delante:

- **El manifest se valida antes de firmar**: con el validador de S7.5, en milisegundos. Si está mal, no malgastes tiempo firmando.
- **El cert se valida contra el manifest antes de instalar**: con el check de coincidencia del ejemplo. Si no cuadran, regenera uno y sigue.
- **La actualización in-place se valida con un cambio visible**: la versión que la app muestra en pantalla. Si tras `Add-AppxPackage` la app sigue mostrando la vieja, hay un problema (cache, Identity.Name cambió, lo que sea).

Tres validaciones que ahorran horas. La práctica las materializa en código y en pasos numerados.

---

## 4. La analogía vertebradora: la receta de cocina con tiempos

Imagina una receta de pastelería complicada. Tiene 8 pasos. Si uno se hace mal, los siguientes amplifican el error y al final el pastel sale como sale:

- **Paso 1 — Preparar el bol y los moldes** (crear solución WPF + WAP). Sin esto, no hay donde mezclar nada.
- **Paso 2 — Pesar y separar ingredientes** (personalizar la app, mostrar versión visible). Etiquetar bien lo que tienes.
- **Paso 3 — Mezclar los secos** (configurar el manifest). Identity.Name correcto, Publisher con CN=, capabilities declaradas.
- **Paso 4 — Preparar el almíbar** (generar certificado con Subject = Publisher del manifest). **El paso que más se equivoca**: si el almíbar tiene azúcar de más o de menos, todos los pasos siguientes saben raro.
- **Paso 5 — Hornear** (build del `.msix` firmado). Configuration Release, Platform x64, Sideloading habilitado.
- **Paso 6 — Sacar del horno y dejar enfriar** (`Add-AppxPackage`). Importar el cert a TrustedPeople primero; sin eso, la instalación falla.
- **Paso 7 — Decorar y reservar** (simular actualización 1.0.0.0 → 1.0.1.0). Cambio visible en pantalla, versión incrementada, actualización in-place.
- **Paso 8 — Servir** (configurar `.appinstaller` con auto-update). El reto opcional pero la pieza que diferencia una práctica académica de un flujo de producción.

Y la regla operativa del libro de recetas: **no avanzas al siguiente paso hasta que el actual cumple sus criterios**. Si el almíbar está mal, repite. Si el horneado no levanta, repite. Sin esa disciplina, sale un pastel comestible-pero-no-bueno; con ella, sale el pastel.

Eso es exactamente lo que hace `PracticaSteps.SiguientePaso`: solo avanza si todos los criterios del paso actual están en `true`. Sin criterios verdes, repites el paso.

---

## 5. Recorrido por el código

### `PracticaSteps` — los 8 pasos como máquina

Cada paso tiene una descripción y una lista de criterios. La máquina de estados:

```csharp
public static PasoPractica? SiguientePaso(
    PasoPractica actual, IReadOnlyList<bool> criteriosOk)
{
    var info = Info(actual);
    if (criteriosOk.Count != info.CriteriosValidacion.Count)
        throw new ArgumentException(...);
    if (!criteriosOk.All(x => x)) return null;     // no avanza

    int idx = Pasos.ToList().FindIndex(p => p.Paso == actual);
    return idx + 1 < Pasos.Count ? Pasos[idx + 1].Paso : null;
}
```

Mismo patrón que `MigrationRoadmap` de S7.7. La diferencia: estos son los 8 pasos de la receta concreta, no las fases generales de migración. Los criterios son específicos y verificables. Algunos ejemplos:

**Paso 3 — ConfigurarManifest**:
- `Identity.Name = Empresa.AppName`.
- `Publisher` con prefijo `CN=`.
- Capabilities: `internetClient` + `runFullTrust` (rescap).
- Visual assets generados (iconos en todos los tamaños).

**Paso 4 — GenerarCertificado**:
- `New-SelfSignedCertificate` con `KeyUsage DigitalSignature`.
- **Subject del cert COINCIDE con Publisher del manifest** (la regla crítica).
- Cert exportado a `.cer` para distribuirlo a `TrustedPeople`.

**Paso 6 — InstalarPaquete**:
- Certificado importado a `Cert:\LocalMachine\TrustedPeople`.
- `Add-AppxPackage` instala sin warnings.
- App aparece en Start Menu.
- App arranca y muestra la versión correcta.

Tener los criterios numerados convierte la práctica de "probemos a ver si funciona" en "verifiquemos cinco puntos concretos antes de seguir".

### `PracticaCertCheck.PublisherCoincide` — el error #1

La función más importante de la práctica:

```csharp
public static ResultadoCheck PublisherCoincide(
    string publisherManifest, string subjectCertificado)
{
    if (!publisherManifest.StartsWith("CN=", StringComparison.Ordinal))
        return new(false, $"Publisher '{publisherManifest}' no empieza por 'CN='...");

    if (!subjectCertificado.StartsWith("CN=", StringComparison.Ordinal))
        return new(false, $"Subject del cert '{subjectCertificado}' no empieza por 'CN='...");

    return string.Equals(publisherManifest, subjectCertificado, StringComparison.Ordinal)
        ? new(true, "Publisher del manifest coincide con el Subject del certificado.")
        : new(false,
            $"Publisher '{publisherManifest}' ≠ Subject '{subjectCertificado}'. " +
            "Windows rechazará el .msix (slide 7).");
}
```

Tres comprobaciones, en orden:

1. **El Publisher empieza por `CN=`**. Si no, ya está mal antes de comparar.
2. **El Subject empieza por `CN=`**. Mismo motivo.
3. **Coincidencia exacta ordinal**. **No se normalizan espacios, mayúsculas, ni puntos**. `CN=MiEmpresa` ≠ `CN=MiEmpresa.` ≠ `CN= MiEmpresa` ≠ `cn=MiEmpresa`.

Es lo que pasa en el caso 1 de la sección 2: un punto extra al final, dos horas perdidas. Pasar los dos strings al check te dice en milisegundos si van a coincidir o no.

Y el complemento — comprobar que el cert puede firmar código:

```csharp
public const string OidCodeSigning = "1.3.6.1.5.5.7.3.3";

public static ResultadoCheck UsoCorrecto(IReadOnlyList<string> ekus) =>
    ekus.Contains(OidCodeSigning, StringComparer.Ordinal)
        ? new(true, "EKU Code Signing presente.")
        : new(false, $"Falta el EKU '{OidCodeSigning}' (Code Signing)...");
```

Si tu cert self-signed no tiene el **Extended Key Usage** de Code Signing (OID `1.3.6.1.5.5.7.3.3`), no firma. `New-SelfSignedCertificate` lo añade si lo pides con `-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")`. Sin esto, el cert vale para HTTPS pero no para firmar binarios.

### `PracticaArtefactosBuilder` — los artefactos canónicos

Esta es la pieza pedagógica del ejemplo: **te genera el manifest y el `.appinstaller` "correctos" para tu caso, para que los uses como referencia**.

```csharp
public static string ConstruirManifest(string empresa, string app, string version) =>
    // genera el XML completo con Identity.Name = {empresa}.{app},
    // Publisher = CN={empresa}, Version = {version},
    // rescap:runFullTrust declarado.

public static string ConstruirAppInstaller(string empresa, string app, string version, string baseUrl) =>
    // genera el .appinstaller con MainPackage apuntando a
    // {empresa}.{app}_{version}_x64.msix
    // y OnLaunch HoursBetweenUpdateChecks=0 (comprueba siempre).
```

Cuando el alumno tiene problemas con su manifest, abre el endpoint `/practica/artefactos/manifest`, pega ahí su empresa/app/versión, recibe el manifest canónico, y lo compara con el suyo. Si el suyo tiene un detalle diferente, se evidencia inmediatamente.

Para el `.appinstaller`, lo mismo. Genera la forma canónica con todos los flags razonables (`HoursBetweenUpdateChecks=0` significa "comprueba siempre, sin caché", útil para demos), y el alumno compara.

### `PracticaMsixPlanner` — el plan + checklist

El servicio inyectable que une los anteriores y produce un plan completo: pasos numerados, check Publisher/Cert (si el alumno aporta los datos), artefactos canónicos, checklist de 11 ítems final. Es lo que sale en `/practica/plan` y representa el "entregable" para el formador.

---

## 6. La checklist de 11 ítems (slide 15)

El cierre operativo de la práctica:

```
[ ] Solución WPF + WAP creada y compila
[ ] Manifest con Identity.Name correcto, Publisher CN=, capabilities runFullTrust + internetClient
[ ] Certificado self-signed con Subject = Publisher del manifest
[ ] EKU Code Signing presente en el cert
[ ] Cert exportado y instalado en TrustedPeople de la máquina
[ ] .msix firmado correctamente (Get-AuthenticodeSignature OK)
[ ] Add-AppxPackage instala sin warnings
[ ] App arranca y muestra la versión en MainWindow
[ ] Versión incrementada (1.0.1.0) y nuevo .msix instalado in-place
[ ] Datos del usuario en LocalFolder preservados tras la actualización
[ ] .appinstaller configurado y actualización automática verificada (reto)
```

Si los 11 están verdes, la práctica está completa. Si alguno está rojo, sabes exactamente cuál y por qué.

---

## 7. Cómo probarlo en local

Es un ejemplo offline (la práctica real es manual en Visual Studio + PowerShell):

```bash
dotnet run --project src/PracticaMsix.Demo.Api
# http://localhost:5103
```

Endpoints:

```http
### Listar los 8 pasos
GET http://localhost:5103/practica/pasos

### ¿Puedo avanzar del paso 3 al 4?
POST http://localhost:5103/practica/avanzar
Content-Type: application/json

{
  "actual": "ConfigurarManifest",
  "criteriosOk": [true, true, true, true]
}
# → "GenerarCertificado"

### ¿Mi Publisher coincide con el Subject?
POST http://localhost:5103/practica/cert-coincide
Content-Type: application/json

{
  "publisherManifest": "CN=MsixDemoCurso",
  "subjectCertificado": "CN=MsixDemoCurso"
}
# → { ok: true, razon: "..." }

### Obtener el manifest canónico para mi caso
GET http://localhost:5103/practica/artefactos/manifest?empresa=Acme&app=MiDemo&version=1.0.0.0

### Obtener el .appinstaller canónico
GET http://localhost:5103/practica/artefactos/appinstaller?empresa=Acme&app=MiDemo&version=1.0.0.0&baseUrl=https://miapp.blob.core.windows.net/msix
```

Los 28 tests cubren los 8 pasos numerados, la coincidencia exacta ordinal (con casos límite: `CN=` falta, espacios extra, mayúsculas), los EKUs del cert, los artefactos canónicos generados.

Para el preflight y verificación real en Windows:

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-preflight.ps1 → Windows 10 1809+, Developer Mode, signtool, makeappx, admin
# 2) 02-verify-msix.ps1 -MsixPath ./MiApp.msix
#    → Get-AuthenticodeSignature + extrae AppxManifest.xml del paquete
#    → compara Publisher con Subject del cert (slide 7/13)
```

El primer script comprueba que tienes el tooling. El segundo, **dado un `.msix` ya construido**, lo abre (es un ZIP), extrae el manifest, y compara su Publisher con el Subject del cert firmante. Si no coinciden, te avisa antes de instalar. Esa es la versión real del `PublisherCoincide` aplicado a un paquete construido.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test`, y PowerShell `pwsh`. La práctica real (WPF + WAP en Visual Studio) la haces tú; el ejemplo te orienta.

---

## 8. Por qué este ejemplo (a diferencia de S6.P y S6.P2) no tiene Web App real

S6.P y S6.P2 desplegaban una API en App Service y probaban Easy Auth contra Entra ID. S7.P no despliega nada nuevo en Azure: la práctica produce un **.msix** que se instala en el Windows del alumno con `Add-AppxPackage`. No hay App Service, no hay backend nuevo. La parte Azure (subir el `.msix` a Blob Storage, configurar `.appinstaller`) es lo que se cubre en S7.5 y S7.6, y el alumno la añade después si quiere automatizar la distribución.

Para muchos alumnos, **el `.msix` queda en local** (con `file:///` en el `.appinstaller`). Es suficiente para entender el flujo. Si después quieres "publicar de verdad", subes a Blob siguiendo el patrón de S7.5.

---

## 9. La validación end-to-end del entregable

Si quieres que el formador (o tú mismo) certifique que la práctica está completa, el flujo es:

1. Compartir el `.msix` generado.
2. Compartir el `.cer` del certificado (público, no la clave privada).
3. Compartir el `.appinstaller` (si se hizo el reto).
4. El formador ejecuta `02-verify-msix.ps1` contra el `.msix`:
   - Verifica firma con `Get-AuthenticodeSignature`.
   - Extrae el manifest del `.msix` (es un ZIP, el manifest está en `AppxManifest.xml`).
   - Compara el Publisher del manifest con el Subject del cert firmante.
   - Reporta los 4 puntos críticos: firmado, Subject del cert, Publisher del manifest, coincidencia.

Si los 4 puntos pasan, los pasos 1-6 de la práctica están bien. El paso 7 (actualización) y el paso 8 (`.appinstaller`) requieren probarse en el Windows del alumno con `Add-AppxPackage` sucesivos.

---

## 10. Glosario breve

- **WAP** (Windows Application Packaging Project): tipo de proyecto en VS que empaqueta una app WPF/WinForms como MSIX.
- **`Add-AppxPackage`**: cmdlet de PowerShell que instala un `.msix` en el Windows actual.
- **`Get-AuthenticodeSignature`**: cmdlet que devuelve la firma de un binario o paquete.
- **`Cert:\LocalMachine\TrustedPeople`**: store de certificados donde se importa el cert firmante del `.msix` para que el sistema confíe en él.
- **Developer Mode** en Windows: ajuste que habilita sideloading e instalación de paquetes no firmados por Microsoft Store.
- **Sideloading**: instalación de MSIX desde un archivo (no desde Store).
- **`runFullTrust`** (capability rescap): permiso para que la app WPF/WinForms acceda al sistema sin restricciones de sandbox. Necesario para apps de escritorio normales.
- **EKU** (Extended Key Usage): atributo del certificado que indica para qué se puede usar (HTTPS, Code Signing, etcétera). Code Signing OID = `1.3.6.1.5.5.7.3.3`.
- **`New-SelfSignedCertificate`**: cmdlet de PowerShell para generar certs self-signed.
- **In-place update**: actualización que conserva los datos del usuario (siempre que Identity.Name y Publisher no cambien).

---

## 11. Cierre

S7.P es la primera vez que tocas el flujo completo: WPF, WAP, manifest, certificado, firma, instalación, actualización, AppInstaller. Cuando termines los 8 pasos con los 11 ítems de la checklist verdes, sabrás hacer una distribución MSIX simple en local. El siguiente paso natural es automatizar este flujo en un pipeline CI/CD (slide 11 de S7.5) y subirlo a Azure Blob.

Lo siguiente es [`S7.P2 — Práctica MSIX wizard`](../S7.P2-practica-msix-wizard/MANUAL.md), una versión guiada paso a paso con un wizard interactivo que cierra el módulo M07.
