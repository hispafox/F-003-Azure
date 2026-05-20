# Manual del alumno — S7.7 · Migración ClickOnce → MSIX

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell, despliegue por Portal. Este manual va antes: te cuenta cómo se planifica una migración ClickOnce → MSIX por fases con criterios de salida testeables (sin big-bang), qué comportamientos de la app son bloqueadores reales, qué se puede arreglar con PSF y por qué la coexistencia con ClickOnce es obligatoria durante semanas.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M07-S7.7](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.7-migracion-clickonce-msix-v3.md). Tres piezas de lógica pura (mapper de manifest ClickOnce → MSIX, roadmap por fases con criterios de salida, evaluador de compatibilidad con clasificación bloqueador/precaución/OK).

*Creado: 2026-05-20 21:35 +0200*

---

## 1. La idea en una frase

Migrar una app ClickOnce a MSIX no es "convertir un archivo a otro". Es un proyecto de **4-6 semanas** en cuatro fases con criterios de salida testeables: empaquetar (semana 1-2), piloto (semana 3), rollout completo (semana 4-6), opcionalmente modernizar a .NET 8 después. Antes de empezar hay que comprobar **bloqueadores reales** (drivers de kernel, escrituras a `C:\Program Files`) que impiden la migración hasta refactorizar; comportamientos que requieren **PSF** (Package Support Framework) como puente; y la **coexistencia** con ClickOnce durante semanas mientras el rollout avanza.

El ejemplo materializa estas tres decisiones: el mapper de manifest (`assemblyIdentity` ClickOnce → `AppxManifest` con identidad sanitizada, `CN=` en publisher, versión completada a 4 partes), el roadmap por fases (no avanzas a la siguiente si un criterio falla), y el evaluador de compatibilidad.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican que la migración sea por fases:

**Caso 1 — la migración que se hizo "por ahorrar tiempo".** Un equipo decidió migrar su app ClickOnce a MSIX en un fin de semana. El lunes envió un email a 500 usuarios: "Desinstalad la versión vieja desde Panel de Control e instalad esta otra". Soporte recibió 80 incidentes el lunes: usuarios que no sabían dónde estaba Panel de Control, usuarios que tenían datos no migrados (configuración del perfil, cachés locales), usuarios que necesitaban un certificado intermedio que no estaba en sus PCs. El siguiente release tuvo que hacerse por fases con grupo piloto, comunicación previa, migración explícita de datos del usuario, y mantener ClickOnce activo durante un mes en paralelo. **La forma correcta desde el día uno.**

**Caso 2 — el "bloqueador" descubierto a mitad de proyecto.** Otro equipo lleva tres semanas migrando una app. Cuando llegan a integración, descubren que la app tiene un componente que **escribe a `C:\Program Files\MiApp\Config`**. En MSIX, el sandbox redirige eso a un VFS y los cambios no persisten. La app sigue arrancando pero pierde configuración al reiniciar. Tienen dos opciones: refactorizar el código (cambiar a `LocalAppData`), o usar PSF para redirigir las escrituras transparentemente. **Si lo hubieran detectado en la semana 1 con un evaluador de compatibilidad**, habrían planificado mejor.

**Caso 3 — el versionado que rompió WAP.** Un equipo arrancó migrando manualmente. El archivo `.application` de ClickOnce tenía `version="2.4"`. Al crear el manifest MSIX, pusieron también `Version="2.4"`. **MSIX requiere 4 partes**: `Major.Minor.Build.Revision`. El build falló con un error críptico. Tuvieron que descubrir la regla, completar a `2.4.0.0`, recompilar, refirmar. Cinco minutos perdidos por una regla que el mapper aplica automáticamente.

Los tres casos los resuelve el ejemplo: roadmap por fases, evaluación de compatibilidad pre-empaquetado, mapper que sanitiza el manifest sin sorpresas.

---

## 3. Por qué esto importa en tu stack

Si tienes —o tu cliente tiene— **al menos una app ClickOnce activa**, la pregunta no es si vas a migrar, sino cuándo. Tres preguntas que conviene tener claras:

- **¿En qué estado está mi app respecto a MSIX?** ¿Hay bloqueadores reales (drivers, escrituras a Program Files) que exigen refactor? ¿Hay comportamientos que requieren PSF (HKLM, services, DLL en PATH global)? ¿O es una app limpia (WPF/WinForms con datos del usuario en `AppData`)? El evaluador del ejemplo lo dice en milisegundos.
- **¿Cuál es mi plan de fases?** Empaquetado, piloto, rollout, modernización. Cada una con su duración y sus criterios de salida. Sin un plan, la migración se vuelve un proyecto open-ended.
- **¿Qué hago con los usuarios durante la transición?** Coexistencia ClickOnce + MSIX al menos durante las semanas del rollout. Comunicación a usuarios. Migración explícita de datos. Plan de rollback si algo va mal.

Si las respuestas son sólidas, la migración es un proyecto controlado. Sin ellas, es un viaje a ciegas con sorpresas cada semana.

---

## 4. La analogía vertebradora: la mudanza por fases

Recupera la analogía de S7.4 (el edificio nuevo) pero amplíala: ahora la mudanza está en marcha. No mueves todos los muebles en un solo camión:

- **Fase 1 — Empaquetado (semana 1-2)**: arquitecto e interiorista preparan el plano. Empaquetas los muebles en cajas etiquetadas. Verificas que las dimensiones del armario nuevo encajan con el sofá viejo, que el ascensor del edificio nuevo aguanta el peso. **Criterios de salida**: la caja está bien hecha y firmada (MSIX firmado), se "instala" en una habitación vacía sin problemas (MSIX instala y desinstala limpiamente en PC de test), tiene los iconos y números de habitación correctos (manifest válido).
- **Fase 2 — Piloto (semana 3)**: tres familias del edificio se mudan primero. Tienen un canal directo de comunicación contigo para reportar problemas. Si descubren que la calefacción no calienta en el bloque sur, lo arreglas antes de la mudanza general. **Criterios de salida**: las tres familias llevan 48 h en su nueva casa sin tickets de soporte críticos, la migración de las mesas/sillas funcionó (datos del usuario migrados), el `.appinstaller` les actualiza solo cuando hay parches.
- **Fase 3 — Rollout completo (semana 4-6)**: el resto del edificio se muda en oleadas. Primero el 5% (un par de familias adicionales), después el 25% (un piso entero), después el 50%, finalmente el 100%. **Criterios de salida**: el pipeline de la empresa de mudanzas automatiza las cajas a cada vivienda nueva, has mandado emails informativos, el staged rollout 5→25→50→100 va sin incidencias, los health checks post-mudanza pasan en el 95% de las familias.
- **Fase 4 — Modernización (opcional)**: ahora que todos están en el edificio nuevo, te planteas si vale la pena comprar muebles nuevos también. Es la migración a .NET 8+. Puede esperar.

Y durante todas las fases: **el edificio viejo sigue habitado**. La gente que aún no se mudó vive ahí. No demueles el edificio viejo hasta que el último vecino se ha mudado **y has esperado una semana más para asegurarte de que nadie quiere volver**. Eso es la "ClickOnce activo ≥ 4 semanas" del slide 18.

Mantén la imagen: empaquetado, piloto, rollout, modernización; coexistencia durante la transición; criterios de salida verificables al final de cada fase.

---

## 5. Recorrido por el código

### `ClickOnceManifestMapper.Mapear` — del XML viejo al XML nuevo

ClickOnce tiene un manifest con `assemblyIdentity` que se parece a esto:

```xml
<assemblyIdentity name="VentasDesktop"
                  version="2.4.1"
                  publicKeyToken="..."
                  language="neutral"
                  processorArchitecture="msil"
                  xmlns="urn:schemas-microsoft-com:asm.v2" />
```

MSIX espera un `AppxManifest` distinto:

```xml
<Identity Name="MiEmpresa.VentasDesktop"
          Version="2.4.1.0"
          Publisher="CN=MiEmpresa S.L."
          ProcessorArchitecture="x64" />
```

El mapper hace cinco transformaciones:

1. **Construye el `Identity.Name`** combinando empresa + app (`MiEmpresa.VentasDesktop`). Si el nombre original tiene guiones u otros caracteres no válidos, los sanitiza.
2. **Asegura que `Publisher` lleva `CN=`** al principio. Si en ClickOnce era `MiEmpresa S.L.`, lo convierte a `CN=MiEmpresa S.L.`.
3. **Completa la versión a 4 partes** (Major.Minor.Build.Revision). `2.4` → `2.4.0.0`. `2.4.1` → `2.4.1.0`. `2.4.1.5` → `2.4.1.5`.
4. **Mapea `processorArchitecture`**: `msil` o `any` → `neutral`; `x86` queda; `amd64` → `x64`.
5. **Declara `runFullTrust`** en el namespace `rescap:` (la capability necesaria para que una app WPF/WinForms corra dentro de MSIX sin restricciones de sandbox).

El resultado es un `AppxManifest` que **pasa el validador de S7.5 sin más cambios**. Si tu pipeline incluye este mapper, no tienes que escribir el manifest a mano.

### `MigrationRoadmap.SiguienteFase` — la máquina de fases

La función clave:

```csharp
public static FaseMigracion? SiguienteFase(
    FaseMigracion actual, IReadOnlyList<bool> criteriosOk)
{
    var info = Info(actual);
    if (criteriosOk.Count != info.CriteriosSalida.Count)
        throw new ArgumentException(...);

    if (!criteriosOk.All(x => x)) return null;     // no avanza

    return actual switch
    {
        Empaquetado     => Piloto,
        Piloto          => RolloutCompleto,
        RolloutCompleto => ModernizarDotNet8,
        ModernizarDotNet8 => null,
    };
}
```

**Solo avanza si TODOS los criterios pasan**. Si alguno está en `false`, la función devuelve `null` y la pipeline sabe que tiene que esperar (o investigar) antes de avanzar. Esto es lo que diferencia un proyecto de migración profesional de uno casero:

- **En el casero**, "vamos a empaquetar... y ya pasaremos al piloto cuando tengamos un rato". Sin criterios objetivos, el proyecto se atasca.
- **En el profesional**, "criterios de salida de Fase 1: cinco puntos. Cuando los cinco están verdes, pasamos a Fase 2". Conversación con stakeholders es clara y trazable.

Los criterios de cada fase del ejemplo son los reales que se usan en proyectos serios:

**Empaquetado** (1-2 semanas):
- WAP creado y compila.
- `Package.appxmanifest` válido (Identity con formato, Publisher con CN=, Version 4 partes).
- Iconos de todos los tamaños generados (44x44, 71x71, 150x150, 310x310, 310x150 y similares).
- MSIX firmado (self-signed o Enterprise CA).
- Instala y desinstala limpiamente en PC de test.

**Piloto** (1 semana):
- AppInstaller configurado con auto-update.
- Subido a Azure Blob Storage.
- Grupo piloto (5-10 personas) instalado.
- Sin tickets de soporte críticos durante 48 horas.
- Migración de datos del usuario (ClickOnce → MSIX) funciona.

**Rollout completo** (2-3 semanas):
- Pipeline CI/CD publica MSIX automáticamente.
- Comunicación a usuarios enviada.
- Staged rollout 5 → 25 → 50 → 100 sin regresiones.
- Health checks post-update OK en ≥ 95% de instalaciones.
- ClickOnce file share marcado read-only (transición).

**Modernizar .NET 8+** (opcional):
- App migrada a .NET 8+ (usando `dotnet-upgrade-assistant`).
- Single-file + self-contained build.
- Soporte multi-arch x64 + ARM64 en `.msixbundle`.

Si tu proyecto los pasa todos en orden, has hecho una migración limpia. Si quieres saltarte algún criterio, la conversación es honesta: "¿es razonable saltar este criterio en este caso concreto?", no "vamos a saltar todo lo que estorbe".

### `MigrationCompatibilityCheck.Evaluar` — los tres niveles de riesgo

La función central:

```csharp
public static EvaluacionCompatibilidad Evaluar(
    IReadOnlyList<ComportamientoApp> comportamientos)
{
    var nivel = comportamientos.Any(Bloqueadores.Contains)
        ? NivelRiesgo.Bloqueador
        : comportamientos.Any(RequierenPsf.Contains)
            ? NivelRiesgo.Precaucion
            : NivelRiesgo.Ok;
    // ...
}
```

Doce comportamientos clasificados en tres grupos:

**Bloqueadores** — la migración no puede empezar sin refactorizar:

- **Drivers de kernel**: imposibles en MSIX. Si tu app es un antivirus, un sniffer de red o similar con componente kernel, MSIX no es el sitio.
- **Escrituras a `C:\Windows` o `C:\Program Files`**: incompatibles con el sandbox. El VFS redirige las escrituras pero la app no se da cuenta y se rompe.

**Precaución (PSF puede ayudar)**:

- **Windows service registrado por la app**: posible con PSF, pero añade complejidad significativa.
- **COM server no declarado en manifest**: declárarlo y se resuelve.
- **Escrituras a HKLM**: el sandbox las virtualiza por defecto, pero la app no las ve. PSF puede redirigir transparentemente.
- **Búsqueda de DLLs en PATH global**: PSF puede modificar la búsqueda de DLLs.

**OK (sin cambios)**:

- WPF, WinForms, console apps.
- Acceso al filesystem del usuario (`%AppData%`, `%LocalAppData%`).
- Acceso al registro del usuario (HKCU).
- Llamadas HTTP/API.

La regla operativa: **bloqueador gana sobre precaución gana sobre OK**. Si hay un solo bloqueador, el nivel es Bloqueador (no puedes empezar). Si hay precauciones pero ningún bloqueador, el nivel es Precaución (puedes empezar pero planifica PSF). Si todo es OK, adelante directamente.

---

## 6. PSF: el puente entre lo viejo y lo nuevo

PSF (Package Support Framework) merece sección propia. Es una herramienta open source de Microsoft que **redirige llamadas a APIs del sistema** dentro de un MSIX, permitiendo que apps con comportamientos "incompatibles" funcionen sin tocar su código.

Tres ejemplos típicos:

1. **Tu app escribe a `HKLM\Software\MiApp`**. PSF puede redirigir esa escritura a `HKCU\Software\MiApp` para que sea por usuario y funcione dentro del sandbox.
2. **Tu app busca un DLL en `C:\Tools\` mediante PATH global**. PSF puede añadir esa ruta al PATH del proceso al arrancar la app.
3. **Tu app tiene un timing race-condition al arrancar** (frecuente en WPF muy viejo). PSF tiene un "fixup" que puede inyectarse para esperar antes de cargar ciertos recursos.

Cómo se aplica: en el manifest del MSIX se declara que PSF está activo, se incluyen los DLLs de PSF en el paquete, y se configuran los fixups en un `config.json`. El resultado: tu `.exe` sigue siendo el mismo binario, pero corre con redirecciones que lo hacen compatible.

**Ventaja**: cero modificaciones al código original. **Desventaja**: una capa más de complejidad operativa, posibles bugs sutiles, peor performance en escrituras. **Cuándo usarlo**: como puente temporal mientras planificas refactorizar la app para que sea MSIX-friendly nativamente. **Cuándo no**: si tu app está activamente mantenida, prefiere refactorizar.

---

## 7. La coexistencia ClickOnce + MSIX (slide 10, 15, 18)

Una parte crítica del plan que muchas migraciones ignoran: durante las semanas del rollout, **las dos versiones tienen que coexistir**:

- El share de ClickOnce sigue activo (algunos usuarios todavía no se han migrado).
- El `.appinstaller` de MSIX está publicado (los usuarios migrados reciben updates).
- El pipeline publica AMBOS hasta que la migración esté completa.

Y luego viene la decisión más sutil: **cuándo apagar ClickOnce**. La regla del slide 18:

1. **Todos los usuarios deben estar en MSIX** (telemetría confirma 0 usuarios en versión ClickOnce).
2. **Mantener ClickOnce read-only ≥ 1 semana** sin incidencias. Si un usuario vuelve, lo encuentra y avisa.
3. **Después: archivar el `.application` y dejar de publicar**.

Hay equipos que dejan ClickOnce activo más tiempo "por seguridad". No es malo. Lo que no debe pasar es **apagarlo el mismo día que pasas al 100% de MSIX**: cualquier rollback urgente queda sin red de seguridad.

Y para la migración de datos del usuario (slide 9, 14): la app MSIX debe detectar al arrancar si hay datos de la versión ClickOnce (`%LocalAppData%\Apps\2.0\...`) y migrarlos a su nuevo `ApplicationData.Current.LocalFolder`. Un marker (`.clickonce-migrated`) evita migrarlos dos veces. El script `01-verify-migration.ps1` del ejemplo busca exactamente este marker para confirmar que la migración pasó.

---

## 8. Cómo probarlo en local

```bash
dotnet run --project src/Migration.Demo.Api
# http://localhost:5102
```

Endpoints:

```http
### Mapear un assemblyIdentity ClickOnce a AppxManifest
POST http://localhost:5102/migracion/mapear
Content-Type: application/json

{
  "empresa": "MiEmpresa S.L.",
  "appName": "VentasDesktop",
  "publisher": "MiEmpresa S.L.",
  "version": "2.4",
  "processorArchitecture": "msil"
}
# → AppxManifest con Identity sanitizado, Publisher CN=..., Version 2.4.0.0

### Evaluar compatibilidad
POST http://localhost:5102/migracion/compatibilidad
Content-Type: application/json

["Wpf", "EscribeHKLM", "LlamadasHttp"]
# → { riesgo: "Precaucion", hallazgos: [...], requierePsf: true }

### ¿Puedo avanzar a la siguiente fase?
POST http://localhost:5102/migracion/siguiente-fase
Content-Type: application/json

{
  "actual": "Empaquetado",
  "criteriosOk": [true, true, true, true, false]   // último falla
}
# → null (no avanzar)

### Cinco criterios todos OK
{
  "actual": "Empaquetado",
  "criteriosOk": [true, true, true, true, true]
}
# → Piloto
```

Los 32 tests cubren el sanitizado del Identity (con guiones, acentos, espacios), la normalización de versión (2.4 → 2.4.0.0, 2.4.1 → 2.4.1.0), el parseo del XML `.application` con namespace `asm.v2`, la clasificación de los doce comportamientos (incluyendo "bloqueador gana sobre precaución"), y el avance del roadmap con criterios desordenados.

Para verificar el estado de migración en un PC real:

```powershell
pwsh -File scripts/demo.ps1 -IdentityName MiEmpresa.VentasDesktop
# 1) ¿Está instalado el MSIX con ese Identity? (Get-AppxPackage)
# 2) ¿Queda ClickOnce residual en %LocalAppData%\Apps\2.0?
# 3) ¿Hay marker .clickonce-migrated en el LocalFolder del MSIX?
```

Los tres outputs te dicen si la migración pasó en ese PC, si quedó algo por limpiar, y si la app MSIX ya migró los datos del usuario.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y PowerShell `pwsh`.

---

## 9. El plan de rollback (slide 18)

La parte que evita desastres: tener un plan claro de qué hacer si el MSIX falla en mitad del rollout. Tres ingredientes:

**Ingrediente 1 — Mantener ClickOnce funcionando durante al menos 4 semanas tras el 100% de MSIX**. Si descubres un bug crítico en MSIX, los usuarios pueden volver al ClickOnce mientras lo arreglas.

**Ingrediente 2 — Rollback de MSIX como build+1**. Lo aprendiste en S7.6: si la versión 2.4.5.0 está rota, publicas la 2.4.4.0 con etiqueta 2.4.6.0. Sin downgrade, sin sorpresas.

**Ingrediente 3 — Comunicación clara**. "Si tienes problemas, vuelve a instalar la versión ClickOnce desde [URL]. Estamos investigando." El email plantilla debe estar redactado **antes de la primera migración**, no en pleno incidente.

Con los tres ingredientes, un incidente durante la migración es manejable. Sin ellos, un incidente es un fin de semana de trabajo.

---

## 10. Glosario breve

- **`assemblyIdentity`**: elemento del manifest ClickOnce que identifica la app. Equivalente conceptual al `Identity` del MSIX.
- **WAP** (Windows Application Packaging): tipo de proyecto en Visual Studio que empaqueta una app como MSIX sin tocar su código.
- **PSF** (Package Support Framework): herramienta open source de Microsoft para redirigir llamadas API y "parchear" apps incompatibles con el sandbox.
- **Bloqueador**: comportamiento de la app que impide la migración a MSIX sin refactorizar primero.
- **Precaución**: comportamiento que la app puede mantener con ayuda de PSF, con coste de complejidad.
- **Migración de datos del usuario**: copia de los archivos de configuración/cache del directorio ClickOnce (`%LocalAppData%\Apps\2.0\...`) al `LocalFolder` del MSIX.
- **Marker de migración**: archivo (`.clickonce-migrated`) en el LocalFolder del MSIX que indica que los datos ya se migraron. Evita migrarlos dos veces.
- **Coexistencia**: período en que ClickOnce y MSIX están ambos disponibles simultáneamente durante el rollout.
- **`dotnet-upgrade-assistant`**: herramienta de Microsoft para migrar apps .NET Framework a .NET 8+. La usas si haces la fase opcional de modernización.

---

## 11. Cierre

S7.7 cierra el bloque conceptual de distribución desktop de M07. Si has interiorizado S7.4 (decisión de migración), S7.5 (empaquetado), S7.6 (auto-update) y S7.7 (plan de migración), tienes el modelo mental completo para mover apps desktop de ClickOnce a MSIX en producción de forma profesional. Las prácticas siguientes (S7.P y S7.P2) materializan estos cuatro en flujos end-to-end concretos.

Lo siguiente es [`S7.P — Práctica MSIX end-to-end`](../S7.P-practica-msix/MANUAL.md), donde el roadmap de este submódulo se ejecuta paso a paso con una app WPF de prueba: crear WAP, validar manifest, firmar, subir a Blob, configurar `.appinstaller`, verificar instalación.
