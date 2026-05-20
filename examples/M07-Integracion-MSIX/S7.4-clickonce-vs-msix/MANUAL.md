# Manual del alumno — S7.4 · ClickOnce vs MSIX

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts PowerShell de inventario local, estructura. Este manual va antes: te cuenta por qué hay un módulo entero dedicado a "cómo se instala una app de escritorio Windows", qué ha cambiado en los últimos años, y cómo decidir si migrar de ClickOnce a MSIX ahora o esperar.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M07-S7.4](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.4-clickonce-vs-msix-v3.md). Tres piezas de lógica pura (comparador de formatos, advisor de migración con escenarios A/B/C, selector de certificado) y el inicio del bloque de distribución desktop de M07.

*Creado: 2026-05-20 20:35 +0200*

---

## 1. La idea en una frase

Durante quince años, **ClickOnce** fue la forma estándar de distribuir apps WPF/WinForms en .NET Framework: el usuario hacía clic en un enlace, descargaba un `.application`, la app se instalaba en su perfil, se actualizaba sola. Funcionó pero no escaló al mundo moderno: sin sandbox, sin Intune/MDM, sin .NET 8+, sin Microsoft Store. **MSIX** es el formato moderno —el sustituto oficial— y trae todo lo anterior por defecto: contenedor que aísla la app del sistema, identidad firme para Microsoft Store/Intune/winget, desinstalación limpia, AppInstaller para auto-update, soporte completo de .NET 8+. La pregunta del submódulo no es si migrar (Microsoft ya no evoluciona ClickOnce), sino **cuándo y por qué camino**.

---

## 2. El problema real que hay detrás

Tres situaciones que se repiten en empresas con apps de escritorio existentes:

**Caso 1 — la app ClickOnce que "siempre ha funcionado".** Una empresa lleva diez años distribuyendo una app WPF con ClickOnce. El equipo de IT despliega desde un share de red, los usuarios la abren, todo bien. El año pasado el equipo de seguridad pidió **inventario MDM con Intune**. ClickOnce no aparece en Intune. La app sigue ejecutándose pero IT no la puede gestionar centralizadamente. Y cuando IT preguntó a Microsoft, la respuesta fue: ClickOnce se mantiene pero no se evoluciona; para Intune necesitas MSIX. El equipo se vio obligado a migrar.

**Caso 2 — la migración a .NET 8 que no podía publicarse.** Otro equipo decidió modernizar su app de .NET Framework 4.8 a .NET 8 para aprovechar las nuevas APIs. Tras meses de trabajo, intentaron publicarla con ClickOnce y descubrieron que **ClickOnce solo soporta hasta .NET Framework 4.8**. El proyecto que iba a "modernizar la app" terminó haciendo además la migración del formato de distribución. **Si vas a tocar el código para .NET 8, ya casi gratis empaquetas MSIX**.

**Caso 3 — la app interna sin firma reconocida.** Una empresa distribuía su app interna como `.exe` directo o como MSI auto-firmado. SmartScreen de Windows mostraba el warning rojo ("Windows protegió tu PC") cada vez que un usuario nuevo la instalaba. Los usuarios llamaban a IT. IT mandaba la instrucción de "haz clic en Más información → Ejecutar de todas formas". Era ruido constante. La solución correcta era firmar con un certificado de Enterprise CA (Active Directory Certificate Services), que ya estaba en todos los PCs del dominio. Sin warning, sin llamadas a IT. Tres líneas de configuración del MSIX y un certificado emitido en el AD CS.

Los tres casos los resuelve el submódulo: cuándo migrar (slide 18), qué camino (A/B/C de la slide 12), qué certificado (slide 8).

---

## 3. Por qué esto importa en tu stack

Si tu organización distribuye **cualquier app de escritorio Windows**, este submódulo es relevante incluso si no la mantienes tú. Tres preguntas que conviene tener claras:

- **¿En qué formato distribuís hoy?** ClickOnce (`.application`), MSI (Windows Installer), `.exe` directo, MSIX. Cada uno tiene implicaciones distintas de seguridad, gestión y futuro.
- **¿Cuál es vuestro objetivo en los próximos dos años?** Intune para inventario, Microsoft Store para distribución pública, .NET 8+ para modernizar, multi-tenant para varias divisiones de la empresa. Cualquiera de estos cuatro empuja a MSIX.
- **¿Cómo firmáis las apps?** Self-signed (warning rojo en SmartScreen), Enterprise CA (interno, sin warning en el dominio), Public CA (caro pero global), Microsoft Store (gratis pero requiere publicar en la Store). El subsidio del certificado es muchas veces lo que decide.

Si las respuestas son ClickOnce/MSI legacy, sí a alguno de los cuatro objetivos, y firma errática, **vas a migrar a MSIX en los próximos 18 meses**. El submódulo te ayuda a planificarlo.

---

## 4. La analogía vertebradora: la mudanza de oficina

Imagina que llevas quince años en una oficina de un edificio antiguo. Funciona. La conoces. Pero hay tres cosas que la limitan:

- **No hay sistema centralizado de control de accesos**. Cada persona tiene su llave física. Cuando alguien deja la empresa, hay que cambiar la cerradura. Eso es ClickOnce sin Intune: gestión manual, sin inventario central.
- **No se puede ampliar más** porque el edificio es de antes del código técnico moderno. No se puede meter más capacidad, ni instalación eléctrica nueva. Eso es ClickOnce sin .NET 8+: te quedas en .NET Framework para siempre.
- **El sistema antiincendios es viejo y no certificado**. Si Inspección llega, te pone una sanción. Eso es ClickOnce sin sandbox: cualquier app accede a todo el sistema sin restricciones modernas.

**MSIX es el edificio nuevo**:

- Tarjetas RFID en lugar de llaves. Cuando alguien deja la empresa, su tarjeta deja de funcionar centralizadamente (Intune controla el inventario).
- Espacios modulares ampliables. Puedes instalar lo que quieras dentro de tu oficina sin afectar al edificio (sandbox de MSIX, .NET 8+, capabilities declaradas).
- Sistema antiincendios certificado y mantenido. Cumples las normativas modernas (firma con certificado moderno, distribución por canales reconocidos).

La mudanza no es trivial. Hay tres caminos:

- **Camino A — Mudar tal cual**: cogemos los muebles existentes, los movemos al edificio nuevo, los colocamos donde puedan. La app sigue siendo la misma (.NET Framework, mismo código), solo cambias el formato del paquete. **MSIX Packaging Tool** te empaqueta tu app actual sin reescribirla.
- **Camino B — Mudar modernizando**: ya que tenemos que mudar, aprovechamos para tirar los muebles viejos, comprar nuevos, repensar la distribución de las salas. La app se migra a .NET 8+ además de empaquetarse como MSIX. Más trabajo, mejor resultado.
- **Camino C — Empezar de cero**: una división nueva abre y le dan oficina en el edificio nuevo desde el día uno. Es una app nueva que se proyecta directamente como MSIX con WAP (Windows Application Packaging). No hay legado, no hay mudanza.

La pregunta no es "¿mudarse o quedarse?". El edificio antiguo cierra. La pregunta es "¿cuándo y por qué camino?".

---

## 5. Recorrido por el código

### `DistributionFormatComparator` — la matriz feature-by-feature

Es el código más declarativo del submódulo: una tabla de qué soporta cada formato y un cálculo de ventajas comparativas. La matriz:

| Característica | ClickOnce | MSIX | MSI | winget |
| --- | --- | --- | --- | --- |
| Sandbox | ❌ | ✅ | ❌ | ✅ (hereda de MSIX) |
| Sin admin para instalar | ✅ (por usuario) | ✅ | ❌ requiere admin | ✅ |
| Auto-update integrado | ✅ | ✅ (con .appinstaller) | ❌ (custom) | ✅ |
| Soporte Intune | ❌ | ✅ | ✅ | ✅ |
| Soporte Microsoft Store | ❌ | ✅ | ❌ | ✅ |
| .NET 8+ | ❌ | ✅ | ✅ | ✅ |
| Desinstalación limpia | parcial | ✅ | parcial | ✅ |
| Futuro evolutivo Microsoft | ❌ | ✅ | mantenimiento | ✅ |

Cuando comparas ClickOnce con MSIX feature por feature, MSIX gana en ≥7 de 8. Es lo que el submódulo llama "el roadmap de Microsoft": ClickOnce está congelado, MSIX está activo.

### `MigrationDecisionAdvisor.DebeMigrar` — la decisión "¿ahora o esperar?"

Cuatro factores que empujan a migrar **ya**, dos que justifican esperar:

```csharp
public static DecisionMigracion DebeMigrar(
    bool intunePlaneado, bool dotNet8Planeado,
    bool certAuthenticodeExpira, bool problemasActualizacion,
    bool clickOnceFuncionaBien, bool equipoSinBandwidth)
{
    var aFavor = new List<string>();
    if (intunePlaneado) aFavor.Add("Intune/MDM planeado: ClickOnce no se integra...");
    if (dotNet8Planeado) aFavor.Add(".NET 8+ planeado: ClickOnce solo .NET Framework...");
    if (certAuthenticodeExpira) aFavor.Add("Certificado caduca: aprovecha para mover a MSIX signing...");
    if (problemasActualizacion) aFavor.Add("Problemas recurrentes de actualización...");

    var enContra = new List<string>();
    if (clickOnceFuncionaBien && !problemasActualizacion)
        enContra.Add("ClickOnce funciona sin problemas: urgencia menor...");
    if (equipoSinBandwidth) enContra.Add("Equipo sin bandwidth para la migración ahora...");

    bool recomendado = aFavor.Count > enContra.Count;
    // ...
}
```

Los cuatro factores "ahora":

1. **Intune/MDM en el roadmap de IT**. ClickOnce no aparece en Intune. Si IT quiere inventario centralizado, MSIX.
2. **.NET 8+ planeado**. ClickOnce no soporta más allá de .NET Framework 4.8. Modernizar exige cambiar de formato.
3. **El certificado Authenticode caduca**. Renovar y reconfigurar firma cuesta el mismo trabajo. Aprovecha para mover a MSIX en una sola pasada.
4. **Problemas recurrentes de actualización**. ClickOnce tiene limitaciones conocidas (caché de descargas corrupta, problemas de proxy). Cada incidente es un argumento para migrar.

Los dos factores "esperar":

1. **ClickOnce funciona sin problemas**. Si nadie se queja y no hay objetivos nuevos, no urge. La migración seguirá disponible cuando llegue el momento.
2. **Equipo sin bandwidth**. Si el equipo ya está agobiado, meter una migración de formato encima es contraproducente. Prioriza otras cosas y migra cuando haya espacio.

La decisión es honesta: "migrad si pesan más a favor que en contra; si están equilibrados, empezad por apps nuevas en MSIX y migrad las existentes con calma".

### `MigrationDecisionAdvisor.RecomendarEscenario` — A, B o C

Tres caminos del slide 12:

```csharp
public static EscenarioMigracion RecomendarEscenario(
    bool esAppNueva, bool sobreDotNetFramework, bool tieneTiempoEquipo)
{
    if (esAppNueva) return EscenarioMigracion.C_AppNuevaDirectaMsix;
    if (sobreDotNetFramework && tieneTiempoEquipo)
        return EscenarioMigracion.B_DotNet8MasMsix;
    return EscenarioMigracion.A_EmpaquetarSinReescribir;
}
```

- **Escenario A — Empaquetar sin reescribir**: tu app actual es .NET Framework. Usas la **MSIX Packaging Tool** para empaquetar el `.exe` existente como MSIX. La app no cambia internamente. Es la forma más rápida de salir de ClickOnce.
- **Escenario B — .NET 8 + MSIX**: modernizas el código a .NET 8 (o superior) y empaquetas con WAP en Visual Studio. Más trabajo pero acceso a todas las novedades del runtime.
- **Escenario C — App nueva directamente en MSIX**: aplicación nueva, proyecto WAP desde el día uno. Sin legado.

La regla pragmática: para apps nuevas, **siempre C**. Para apps existentes, **A si urge, B si tienes tiempo**.

### `SigningCertAdvisor.Recomendar` — qué certificado por escenario

La decisión más ignorada y más cara si se hace mal:

```csharp
public static RecomendacionCert Recomendar(EscenarioFirma escenario) => escenario switch
{
    EscenarioFirma.Desarrollo => new(SelfSigned, "Gratis", "Warning",
        "Self-signed con New-SelfSignedCertificate; solo dev/test."),
    EscenarioFirma.DistribucionInterna => new(EnterpriseCa, "Incluido en AD", "Sin warning",
        "Enterprise CA (AD CS): de confianza en todos los PCs del dominio."),
    EscenarioFirma.DistribucionExterna => new(PublicCa, "~200-500 €/año", "Sin warning",
        "Public CA (DigiCert, Sectigo, Trusted Signing): firma reconocida globalmente."),
    EscenarioFirma.PublicacionStore => new(MicrosoftStore, "Gratis (con dev account)", "Sin warning",
        "Microsoft Store firma el paquete al publicarlo."),
};
```

Cuatro decisiones según el escenario:

- **Desarrollo**: **self-signed**. Gratis, en treinta segundos. **Warning de SmartScreen**, no es un problema en desarrollo pero es el motivo de que **nunca lo uses en producción**.
- **Distribución interna en la empresa**: **Enterprise CA**. Si tu organización tiene Active Directory Certificate Services (la mayoría lo tienen), puedes emitir certificados gratis que son **trusted en todos los PCs del dominio**. Sin warning, sin coste. Es la opción correcta para el 80% de los casos corporativos.
- **Distribución externa** (clientes, partners, internet abierto): **Public CA**. Compras un certificado de Code Signing a DigiCert, Sectigo o similar. Cuesta 200-500 €/año dependiendo del tipo. Sin warning, reconocido globalmente.
- **Microsoft Store**: **gratis**. Cuando publicas a través de Microsoft Store, Microsoft firma el paquete por ti. El requisito es tener una cuenta de desarrollador (~15-100 € de alta única).

La regla práctica: **empezad con Enterprise CA**. Si tu app va a ir más allá de tu dominio AD, considera Public CA. Microsoft Store solo si el flujo de publicación encaja con tu producto.

---

## 6. El roadmap de Microsoft, en honesto

La slide 11 lo dice claro: **ClickOnce está congelado**. Microsoft no anuncia depreciación oficial todavía, pero las señales son claras desde 2020:

- Sin soporte .NET 8+.
- Sin Intune.
- Sin Microsoft Store.
- Sin winget.
- Sin sandbox moderno.
- Sin firma compatible con Trusted Signing.

Por contraste, MSIX es donde Microsoft está invirtiendo:

- Soporte completo .NET 8+ (y posteriores).
- Integración nativa con Intune.
- Distribución por Microsoft Store, winget, AppInstaller.
- Sandbox con capabilities declaradas.
- Trusted Signing (servicio de firma de Azure).
- Soporte para PWA, WSL, contenedores.

Esto no significa que tu app ClickOnce vaya a dejar de funcionar mañana. Significa que cada año que pasa, ClickOnce es menos compatible con el ecosistema moderno. La cuestión es cuándo decides hacer la transición — y la respuesta correcta normalmente es "antes de que sea urgente".

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Distribution.Demo.Api
# http://localhost:5099
```

Endpoints:

```http
### Comparar ClickOnce vs MSIX
GET http://localhost:5099/distribution/comparar?a=ClickOnce&b=Msix
# → ventajas y desventajas de cada uno

### ¿Es buen momento para migrar?
POST http://localhost:5099/distribution/migrar
Content-Type: application/json

{
  "intunePlaneado": true,
  "dotNet8Planeado": true,
  "certAuthenticodeExpira": false,
  "problemasActualizacion": false,
  "clickOnceFuncionaBien": true,
  "equipoSinBandwidth": false
}
# → { recomendado: true, razones: ["Intune planeado...", ".NET 8 planeado..."] }

### Qué escenario para una app nueva
GET http://localhost:5099/distribution/escenario?esAppNueva=true
# → C_AppNuevaDirectaMsix

### Qué certificado para distribución interna
GET http://localhost:5099/distribution/cert?escenario=DistribucionInterna
# → EnterpriseCa, "Incluido en AD", "Sin warning (si CA es trusted)"

### Plan completo
POST http://localhost:5099/distribution/plan
# → migración + escenario + cert + ventajas + checklist
```

Los 30 tests cubren las combinaciones del comparador, todos los factores del advisor de migración, los tres escenarios A/B/C y los cuatro certificados.

Para inventariar tu propio Windows:

```powershell
pwsh -File scripts/demo.ps1
# 1) Get-AppxPackage → MSIX/AppX ya instalados (slide 5/14)
# 2) %LocalAppData%\Apps\2.0\*.application → ClickOnce existentes (slide 3)
```

Te dice qué hay en tu PC: si tienes ClickOnce activo (probablemente sí, si trabajas en empresa); si tienes MSIX (sí, Microsoft Store los usa).

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`. PowerShell `pwsh` es la versión 7+, no `powershell.exe` legacy.

---

## 8. Por qué este submódulo tampoco tiene CAPA de integración

S7.4 es **decisión**, no ejecución. Comparar formatos, elegir camino, elegir certificado — todo lógica pura. El empaquetado real (firmar, generar `.msix`, generar `.appinstaller`) llega en S7.5. Las prácticas S7.P y S7.P2 cierran con el flujo end-to-end real.

Forzar una "integración" aquí sería empaquetar una app de prueba para probar... el empaquetado, que es lo que enseña el siguiente submódulo. Mejor mantener la separación: aquí decides, en S7.5 ejecutas, en las prácticas verificas el flujo completo.

---

## 9. Glosario breve

- **ClickOnce**: tecnología de distribución de Microsoft para apps .NET Framework. Funcionó 2003-2020, ahora congelada.
- **MSIX**: formato moderno de empaquetado para apps Windows. Sustituto oficial de ClickOnce + MSI + AppX.
- **MSI** (Windows Installer): formato legacy de instaladores. Requiere admin, sin sandbox, mantenimiento pero sin nuevas features.
- **winget**: gestor de paquetes oficial de Microsoft. Distribuye MSIX (entre otros) por línea de comandos.
- **MSIX Packaging Tool**: utilidad gratuita de Microsoft que empaqueta una app existente como MSIX sin reescribirla.
- **WAP** (Windows Application Packaging): tipo de proyecto en Visual Studio para empaquetar una app .NET como MSIX desde el código.
- **AppInstaller**: archivo `.appinstaller` que apunta a un MSIX en un servidor y configura su auto-update. Visto en S7.6.
- **Sideloading**: instalación de un MSIX desde un share interno (no Microsoft Store). Requiere certificado de firma trusted.
- **Sandbox**: aislamiento del sistema operativo. MSIX corre en su propio container; ClickOnce no.
- **Enterprise CA**: Certificate Authority interna de la organización (AD CS). Emite certificados trusted en el dominio.
- **Public CA**: Certificate Authority pública (DigiCert, Sectigo). Certificados trusted globalmente.
- **Trusted Signing**: servicio nuevo de Azure (en preview) que firma MSIX en la nube con certs públicos.
- **SmartScreen**: protección de Windows que bloquea ejecutables no firmados o de reputación desconocida.
- **Authenticode**: tecnología de firma de Microsoft, base de la firma de MSIX y MSI.

---

## 10. Cierre

S7.4 te da las tres tablas mentales para abordar la migración ClickOnce → MSIX sin meter la pata: ¿está ClickOnce muriendo? (sí, lentamente), ¿qué camino tomar? (A/B/C según situación), ¿qué certificado? (Enterprise CA en el 80% de casos). Es la conversación que vas a tener con IT o con tu cliente al menos una vez en los próximos años.

Lo siguiente es [`S7.5 — MSIX empaquetado y distribución`](../S7.5-msix-empaquetado-distribucion/MANUAL.md), donde la teoría se materializa: manifest, capabilities, signing, canales de distribución (sideload, Microsoft Store, Intune, winget).
