# Manual del alumno — S6.1 · Responsabilidad compartida y defense in depth

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta por qué el primer submódulo de seguridad no enseña a configurar nada en Azure sino a tener cuatro tablas claras en la cabeza, y por qué esas cuatro tablas previenen el 80% de las brechas reales.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.1](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.1-responsabilidad-compartida-v3.md). Cuatro piezas de lógica pura sin Azure (matriz de responsabilidad, STRIDE, secret scanner, Secure Score) más un grafo DI verificado.

*Creado: 2026-05-20 16:10 +0200*

---

## 1. La idea en una frase

El módulo de seguridad arranca con un giro intencionado: **no se enseñan productos, se enseñan modelos mentales**. Antes de saber configurar Entra ID, OAuth2 o Key Vault, hay que tener clarísimo dónde acaba la responsabilidad de Azure y empieza la tuya, qué amenazas hay que analizar en cada superficie nueva y qué controles esperar tener encendidos a fin de mes. Esas cuatro tablas —responsabilidad compartida, STRIDE, secret scanning y Secure Score— son la base sobre la que se apoyan todos los submódulos siguientes.

El código del ejemplo materializa cada tabla como una función pura. Cero llamadas a Azure. Cero secretos. Cero infraestructura. Solo cuatro estructuras de datos con sus reglas de decisión y los tests que las cubren. Es probablemente el ejemplo del curso con menor superficie de código y mayor densidad conceptual.

---

## 2. El problema real que hay detrás

Tres incidentes reales que el módulo de seguridad podría haber prevenido si la organización hubiera tenido estos modelos mentales en la cabeza:

**Caso 1 — storage abierto.** Un equipo migró un blob de imágenes de producto a Azure Storage. Por comodidad, dejaron el container con acceso público. "Son imágenes públicas, total." Lo que no anticiparon: el mismo storage account tenía otro container privado con backups de la base de datos. Una herramienta de auditoría externa encontró los containers públicos, listó todos, y descubrió que el container "private-backups" también estaba indexado (porque alguien lo había marcado como público durante una prueba el mes anterior). El equipo culpó "a Azure" en la reunión post-incidente. **Azure no era responsable.** El modelo de responsabilidad compartida dice claramente que la **clasificación de datos** y la **configuración de la aplicación** son del cliente, no del proveedor.

**Caso 2 — connection string en git.** Un developer añadió una línea de `appsettings.json` con la connection string de prod a SQL "para probar algo en local". Hizo push. Pasó por revisión. Se fusionó. Tres meses después, un escáner de un investigador de seguridad detectó el patrón `Password=...; Initial Catalog=prod...` en un repo público de la empresa. La empresa no lo supo hasta que el investigador les avisó. Coste: rotar la credencial, auditar accesos, declarar incidente al regulador. **El escáner de secretos lo habría detectado en el pre-commit** si lo hubieran tenido configurado.

**Caso 3 — el rol Owner de quincena.** Para "agilizar el sprint", el responsable técnico le dio rol Owner sobre la suscripción a tres developers. Tras el sprint, nadie lo retiró. Seis meses después, un developer dejó la empresa con un acceso `az login` en su portátil personal y un token de refresh válido por 90 días. No pasó nada catastrófico, pero pudo haber pasado. **El control "RBAC de mínimo privilegio"** que aparece en el checklist del Secure Score se inventó precisamente por estos casos.

Los tres incidentes tienen un patrón común: **no son fallos de Azure**. Son configuraciones del cliente. Y son patrones que se repiten en muchísimas organizaciones. Por eso el módulo arranca aquí.

---

## 3. Por qué esto importa en tu stack

Si te haces responsable de un sistema en Azure —backend, función, app desktop, lo que sea—, vas a tener que tomar decisiones de seguridad desde el primer día. No las puedes posponer al final del proyecto. Tres preguntas que debes saber responder de memoria:

- **¿De qué soy responsable yo y de qué es Azure?** Si crees que Azure se ocupa de tus datos por estar "en la nube", vas a llevar un susto el día del primer incidente. Esa línea está clara y no cambia.
- **¿Qué amenazas tengo que considerar al diseñar mi sistema?** No es magia ni reza pidiendo. Es un análisis sistemático con un marco como STRIDE: para cada componente, las seis amenazas posibles y las mitigaciones que conocemos.
- **¿Qué controles dejo encendidos por defecto?** Hay una lista de unos diez puntos que cualquier sistema serio en Azure debería cumplir. No es opcional; es el mínimo profesional.

Este submódulo te da las cuatro tablas para responder esas preguntas. Los submódulos siguientes —Entra ID, OAuth2, Key Vault— te dan los productos para implementar las respuestas.

---

## 4. La analogía vertebradora: el contrato de inquilino del hotel

Imagina que vives en un hotel de larga estancia. El edificio es propiedad del hotel; tu habitación es tu espacio. El contrato deja claro **quién se ocupa de qué**:

- **El hotel se ocupa del edificio**: la estructura, las paredes maestras, el ascensor, la luz comunitaria, la red de fontanería, la seguridad de la entrada con su recepción. Si el edificio se cae, es problema del hotel. Si el ascensor se rompe, lo arregla el hotel. Esa es la parte que en Azure cubren la red física, los hosts, los datacenters, el sistema operativo del host.
- **Tú te ocupas de tu habitación**: tus pertenencias, tus llaves, qué metes en la caja fuerte, a quién dejas entrar. **El hotel no se mete en esto.** Aunque te roben las joyas porque dejaste la puerta abierta, no es problema del hotel. Esa es la parte que en Azure cubre tus datos, tus identidades, tus dispositivos.

Entre medias hay zonas mixtas:

- **La caja fuerte de la habitación** la pone el hotel, pero tú decides el código y qué guardas. Es la zona de los "controles de red en PaaS": Azure te da el firewall, tú decides las reglas.
- **El servicio de limpieza** lo hace el hotel, pero tú decides si dejas que entren y a qué hora. Es la zona del "sistema operativo en PaaS/SaaS": Azure parchea, pero tú configuras qué accesos hay.

Y luego está la regla operativa del hotel:

- **El registro de entradas y salidas siempre lo lleva el hotel.** Tú puedes pedir un extracto para saber quién ha entrado a tu habitación, qué noches, a qué hora. **Pero si nunca lo pides, no te enteras del problema.** Esa es la "auditoría": Azure registra todo, pero solo tienes el control si vas a leer los logs.

Hay tres cosas que, en el contrato del hotel, **nunca cambian**:

1. Tus pertenencias son tuyas (los **datos**).
2. Tus llaves son tuyas (las **identidades**).
3. Lo que conectas a la corriente de la habitación —portátil, móvil, cámara— es tuyo (los **dispositivos**).

Por mucho que el hotel sea de cinco estrellas y tenga el mejor servicio, esas tres cosas siempre son tu responsabilidad. Es lo que en la matriz de responsabilidad se llama "la línea que nunca cambia" — y es la primera función pura del ejemplo: `ResponsibilityMatrix.SiempreTuya(capa)`.

---

## 5. Recorrido por el código

### `ResponsibilityMatrix` — la línea que nunca cambia

Es la pieza más pequeña y más fundamental. Una función que dada una capa de seguridad y un modelo de servicio te dice quién es responsable:

```csharp
public static bool SiempreTuya(Capa capa) => capa is
    Capa.DatosYClasificacion or
    Capa.CuentasEIdentidades or
    Capa.DispositivosCliente;
```

Esas tres capas son tuyas en cualquier modelo: OnPrem, IaaS, PaaS o SaaS. No hay excepciones. Es la línea roja del contrato.

Y luego el resto del switch que distribuye responsabilidad según el modelo:

```csharp
Capa.Aplicacion => modelo == ModeloServicio.SaaS
    ? Responsable.Azure : Responsable.Tu,

Capa.ControlesDeRed => modelo switch
{
    ModeloServicio.OnPrem or ModeloServicio.IaaS => Responsable.Tu,
    ModeloServicio.PaaS => Responsable.Mixto,
    _ => Responsable.Azure,
},
```

La aplicación es Azure si es SaaS (es código de Microsoft), pero es tuya en cualquier otro modelo. Los controles de red son tuyos en OnPrem/IaaS, mixtos en PaaS, de Azure en SaaS. Y así para cada capa.

¿Por qué materializar esto como código? Porque te obliga a tomar la decisión explícita. Cuando alguien del equipo te pregunta "¿quién se ocupa del cifrado de la base de datos en PaaS?", la respuesta no es "creo que Azure" — es ejecutar `ResponsibilityMatrix.Responsable(Capa.SistemaOperativo, ModeloServicio.PaaS)` mentalmente y responder "Azure". Y si no te queda claro, tienes un test que lo cubre.

### `StrideAnalyzer` — seis amenazas y sus contramedidas

STRIDE es un acrónimo que estructura el threat modeling. Una letra por cada categoría de amenaza:

| Letra | Categoría | Ejemplo en una API de pedidos |
| --- | --- | --- |
| **S** | Spoofing (suplantación) | Un atacante usa el token de otro usuario |
| **T** | Tampering (manipulación) | Modificar el body para cambiar el precio del pedido |
| **R** | Repudiation (repudio) | El usuario niega haber hecho el pedido |
| **I** | Information Disclosure | Un usuario ve los pedidos de otro |
| **D** | Denial of Service | Bombardeo del endpoint /orders |
| **E** | Elevation of Privilege | Un customer accede a /admin |

Cada categoría tiene sus mitigaciones esperadas. Para Spoofing: OAuth2 con Entra ID, MFA, Conditional Access. Para Tampering: HTTPS only, validar schema en servidor, recalcular totales (nunca confiar en lo que viene del cliente). Para Information Disclosure: Row-Level Security, `[Authorize]` con filtro por usuario actual.

Lo importante no es memorizar las mitigaciones específicas — están todas en el código y en las slides. Lo importante es **interiorizar el método**: cuando diseñes un endpoint nuevo, pásale STRIDE. Las seis preguntas, una por una. ¿Cómo me suplantan? ¿Cómo me manipulan? ¿Cómo me niegan? La mitad de los problemas de seguridad se previenen simplemente por haber hecho la pregunta a tiempo.

### `SecretScanner` — el escáner que no debería tener trabajo

Cinco expresiones regulares que detectan los patrones más comunes de secretos en código y configuración:

```csharp
new("azure-storage-key", "Azure Storage Account Key",
    new Regex(@"AccountKey=[A-Za-z0-9+/=]{40,}", ...)),
new("shared-access-key", "Shared Access Key (SAS / Service Bus)",
    new Regex(@"SharedAccessKey=[^;\s]+", ...)),
new("password", "Password en connection string",
    new Regex(@"(?:password|pwd)\s*=\s*[^;\s""']+", ...)),
// ...
```

Y una excepción importante:

```csharp
if (contenido.Contains("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase))
    return [];
```

Una `app setting` con valor `@Microsoft.KeyVault(SecretUri=...)` **no es un secreto**: es una referencia. App Service la resuelve en runtime usando su Managed Identity contra Key Vault. El secreto real nunca está en la config; nunca pasa por git; nunca está en logs. Solo App Service y Key Vault lo ven, y solo durante la ejecución.

Esta clase es una versión simplificada de lo que hacen herramientas como `gitleaks` o `trufflehog`. En un equipo serio, un escáner así corre en pre-commit hook o en CI sobre cada push. La frase que vale la pena recordar:

> **El mejor escáner de secretos es el que nunca encuentra nada porque ya hay un proceso (Key Vault references, Managed Identity, variables de entorno controladas) que evita meter el secreto a mano.**

### `SecureScoreCalculator` — el termómetro del equipo

Once preguntas tipo sí/no que cualquier equipo debe poder contestar:

- ¿Tenemos MFA en todos los administradores?
- ¿Tenemos RBAC de mínimo privilegio? (¿Hay alguien con Owner que no lo necesita?)
- ¿Usamos Managed Identity? (¿Hay connection strings con password sin necesidad?)
- ¿Los secretos están en Key Vault?
- ¿HTTPS forzado en todas las apps?
- ¿Storage con acceso público deshabilitado?
- ¿SQL con firewall y Entra ID auth, no SQL auth?
- ¿Azure Policy aplicada para asegurar mínimos?
- ¿Logs y auditoría habilitados?
- ¿Dependencias auditadas regularmente?
- ¿Plan de respuesta a incidentes documentado?

El servicio devuelve una puntuación 0-100, lo que falta, y un veredicto:

- ≥90: Excelente
- ≥70: Aceptable (el objetivo mínimo de la slide 17)
- ≥40: Riesgo: prioriza las recomendaciones
- <40: Crítico: superficie de ataque amplia

¿Por qué nueve preguntas y no veinte? Porque diez es la cantidad que un equipo recuerda. Una lista de cincuenta puntos no se mira nunca; una de diez se mira mensualmente. Defender for Cloud en el Portal te da una versión mucho más completa (cientos de controles), pero la lista corta del equipo es la que dispara la conversación operativa.

---

## 6. La regla "siempre tuya", aplicada al día a día

La regla de las tres responsabilidades "siempre tuyas" es la frase más importante del submódulo. Hagamos ejercicio mental con tres situaciones reales:

**Situación A — Subes tu app a App Service y delegas la base de datos a Azure SQL.**

- Datos: tuyos. Tú decides qué guardar, cómo cifrar, qué exponer.
- Identidades: tuyas. Tú decides qué usuarios entran y con qué roles.
- Dispositivos: tuyos. Los portátiles del equipo donde alguien se conecta a `az login` son tuyos.

¿Qué cubre Azure? El SO del host, los hosts físicos, la red física, el datacenter. Y, en este caso de PaaS, también el SO del App Service y el motor de SQL Server. Pero tus datos —el contenido de las tablas— y tus identidades —los usuarios de Entra ID y los logins de SQL— siguen siendo enteramente tuyos.

**Situación B — Migras a Microsoft 365 (SaaS).**

- Datos: tuyos. Los emails de tu organización, los documentos en SharePoint, son tuyos.
- Identidades: tuyas. Los usuarios de Entra ID son tuyos.
- Dispositivos: tuyos. Los portátiles donde alguien abre Outlook son tuyos.

¿Qué cubre Microsoft? Casi todo lo demás: la aplicación, la red, los hosts, los datacenters. Pero si un usuario de tu organización filtra accidentalmente un documento de SharePoint compartiéndolo con "todo el mundo" —no es problema de Microsoft. Es problema de **tu configuración de DLP**, de **tu formación al usuario**, de **tu política de etiquetado**.

**Situación C — Tienes un servidor en tu CPD on-premise.**

- Datos, identidades y dispositivos: tuyos (obviamente).
- Pero también: aplicación, controles de red, sistema operativo, hosts físicos, red física, datacenter. **Todo tuyo.**

Por eso en On-Prem el equipo de operaciones es enorme. En PaaS muchas de esas cargas se delegan a Azure. La línea sigue donde estaba.

Si interiorizas esta regla, lo demás del módulo se entiende con más claridad. Cuando lleguemos a Entra ID o Key Vault, no son productos para "que Azure haga la seguridad por ti" — son productos para **que tú implementes mejor la parte de seguridad que sigue siendo tuya**.

---

## 7. Cómo probarlo en local

Es un ejemplo offline al 100%. No necesitas suscripción de Azure, ni emuladores, ni nada:

```bash
dotnet run --project src/Security.Demo.Api
# http://localhost:5088
```

Y luego juegas con `api.http`:

```http
### Matriz de responsabilidad para PaaS
GET http://localhost:5088/seguridad/responsabilidad?modelo=PaaS

### STRIDE para la amenaza S (Spoofing)
GET http://localhost:5088/seguridad/stride/S

### Escanear una connection string
POST http://localhost:5088/seguridad/scan
Content-Type: application/json

"Server=tcp:mydb.database.windows.net,1433;Password=Pa$$w0rd!;..."

### Calcular Secure Score
POST http://localhost:5088/seguridad/secure-score
Content-Type: application/json

{
  "mfaAdmins": true,
  "rbacMinimoPrivilegio": true,
  "managedIdentity": false,
  "keyVaultSecretos": true,
  ...
}
```

El test `dotnet test` cubre 46 escenarios en milisegundos: cada celda de la matriz, cada amenaza de STRIDE, cada patrón del secret scanner (incluyendo el caso "esto es una Key Vault reference, no me marques como secreto"), y la suma de Secure Score con varios checklists.

Para inspeccionar la postura **real** de tu suscripción (no de este ejemplo) tienes dos opciones en el directorio `scripts/`:

- `01-posture-check.sh` — recorre tus storage accounts, SQL servers y web apps con `az` y te dice cuáles están mal configurados (públicos, firewall abierto, sin HTTPS).
- `02-secure-score.sh` — pide a Defender for Cloud tu Secure Score real y las recomendaciones pendientes.

Ambos son **de solo lectura** — no crean ni modifican nada. Solo necesitan rol `Security Reader`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué este submódulo no tiene CAPA de integración

Lo notarás al mirar la estructura de tests: hay `Unit_*Tests` (la lógica pura) y `DiContainer_Tests` (el grafo DI con `WebApplicationFactory`), pero no hay tests de integración. Esto es deliberado y vale la pena explicarlo.

Los tests de integración tienen sentido cuando hay un componente externo emulable que vale la pena ejercitar: Azurite para storage, Cosmos emulator, una BD en Testcontainers. Pero la "responsabilidad compartida" no es un servicio que se emule. Es un **modelo mental**. STRIDE no es una API. Secret scanning no requiere Azure. Secure Score se calcula con un checklist.

Forzar una CAPA de integración aquí sería inventar un sistema externo para tener algo que testear. Y eso no es lo que enseña el submódulo. Lo que enseña son cuatro estructuras de datos con sus reglas, y la mejor forma de cubrirlas es con unit tests rápidos y un test que verifique que el grafo DI compone bien las dependencias.

Verás el mismo criterio en S6.2 (Entra ID — modelo mental de identidades), S6.4 (auth desktop — flujos sin servidor real) y S6.5 (cifrado at-rest — conceptos). En S6.3 (OAuth2 — flujo con servidor mock), S6.6 (Key Vault — hay emulador) y S6.P (práctica con KV real) sí hay integración. La regla es: **integración solo donde hay algo emulable que merezca la pena**.

---

## 9. La trampa de los "nice to have" en seguridad

Una observación que se repite en proyectos: la seguridad se mete al final del proyecto, "cuando ya esté la funcionalidad". Y entonces no hay tiempo. Y entonces se va a producción con MFA opcional, sin Key Vault, con storage públicos, sin auditoría. Y a los seis meses hay un incidente.

La conversación correcta no es "vamos a meter seguridad al final" — es "vamos a definir qué controles van encendidos desde el día uno y qué controles son negociables". El checklist del `ISecureScore` es exactamente ese punto de partida:

- MFA, RBAC, HTTPS, storage privado, SQL con firewall: **no negociables**. Encendidos desde el primer despliegue.
- Managed Identity, Key Vault, Azure Policy, logs/auditoría, dependencias auditadas: **objetivo razonable**. Encendidos en el primer trimestre.
- Plan de respuesta a incidentes documentado: **objetivo del primer semestre**.

Si llegas a producción sin los cinco primeros, el proyecto tiene un agujero de seguridad estructural que va a salir caro. Si llegas con los cinco encendidos, tienes un sistema razonable que evoluciona bien.

---

## 10. Glosario breve

- **Defense in depth**: defensa por capas. Si una capa falla, la siguiente te protege. Las cuatro clásicas en Azure: identidad → red → aplicación → datos.
- **STRIDE**: marco de threat modeling con seis categorías de amenaza (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).
- **Secure Score**: métrica 0-100 de Microsoft Defender for Cloud que evalúa la postura de seguridad de tu suscripción frente a docenas de controles.
- **Modelo de responsabilidad compartida**: contrato implícito entre Azure y cliente que define quién es responsable de qué capa en cada modelo de servicio (OnPrem, IaaS, PaaS, SaaS).
- **Managed Identity** (anticipo de S6.6): identidad de Azure asociada a un recurso (App Service, Function, VM) que reemplaza el uso de connection strings con password.
- **Key Vault reference** (anticipo de S6.6): App Setting con valor `@Microsoft.KeyVault(SecretUri=...)` que App Service resuelve en runtime con su Managed Identity. No es un secreto; es un puntero a uno.
- **RBAC de mínimo privilegio**: principio de dar a cada identidad solo los roles estrictamente necesarios para su función, no más.
- **Defender for Cloud**: el centro de mando de seguridad de Azure. Ofrece recomendaciones, calcula Secure Score, detecta amenazas activas.

---

## 11. Para ir más allá del ejemplo

Tres frentes naturales para extender lo aprendido:

- **Conectar el `SecretScanner` a un pre-commit hook** en tus repos. Con `husky` (Node) o un git hook directo, ejecutas el escáner sobre los archivos modificados antes de cada commit. El primer secreto que detecta ahorra un incidente.
- **Documentar el modelo STRIDE para tu sistema**. Por cada componente principal (frontend, API, BD, BLOB storage, queue), un párrafo con las seis amenazas y las mitigaciones que tienes activas. Es un documento vivo, no de archivo; revísalo cuando añadas un componente nuevo.
- **Programar la revisión mensual del Secure Score** del equipo. Sin esa cadencia, el checklist se queda colgado. Con cadencia, las recomendaciones de Defender for Cloud se cierran progresivamente y la postura mejora.

---

## 12. Cierre

S6.1 no te enseña a configurar Azure. Te enseña a **pensar en seguridad** antes de configurar cualquier cosa: dónde está la línea, qué amenazas considerar, qué controles esperar, qué nivel de postura tener. Las cuatro tablas del ejemplo son la base sobre la que se apoyan los seis submódulos siguientes — Entra ID, OAuth2, auth desktop, seguridad de datos y Key Vault. Cuando llegues a ellos, sabrás dónde colocar cada producto en el mapa.

Lo siguiente es [`S6.2 — Microsoft Entra ID`](../S6.2-entra-id/MANUAL.md), donde la conversación pasa de "modelo mental" a "el producto de identidad que vas a usar todos los días en Azure".
