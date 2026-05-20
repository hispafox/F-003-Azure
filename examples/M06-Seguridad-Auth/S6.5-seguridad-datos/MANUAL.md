# Manual del alumno — S6.5 · Seguridad de datos

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, estructura. Este manual va antes: te cuenta qué hace Azure por defecto en cifrado, cuándo merece la pena pasar a CMK o Always Encrypted, qué significan TLS 1.2 mínimo y Encrypt=true en la práctica, y por qué la combinación `AllowAnyOrigin + AllowCredentials` es una vulnerabilidad real.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M06-S6.5](../../../doc/M06-Seguridad-Auth/v3-actual/M06-S6.5-seguridad-datos-v3.md). Tres validadores puros (cifrado at-rest, TLS in-transit, CORS) más un assessor que evalúa el checklist completo.

*Creado: 2026-05-20 17:45 +0200*

---

## 1. La idea en una frase

Azure cifra **at-rest** (AES-256) e **in-transit** (TLS 1.2) por defecto, sin que tengas que configurar nada en el 90% de los casos. Lo que sí depende de ti es: forzar HTTPS-only y TLS 1.2 mínimo en tus servicios, usar connection strings que exijan canal cifrado (`Encrypt=true` en SQL, HTTPS en Storage), decidir si tu negocio requiere Customer-Managed Keys (CMK) o Always Encrypted en lugar de las claves de Microsoft, y configurar CORS sin la combinación prohibida (wildcard + credenciales).

El submódulo materializa esas decisiones como funciones puras. El cifrado en sí lo hace Azure; tú validas la **postura de configuración**.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican el submódulo:

**Caso 1 — la connection string sin Encrypt.** Un equipo configuró Azure SQL desde el portal y usó la connection string "como venía", con `Encrypt=False` (era el default antiguo). La app conectaba. Las consultas iban en texto claro entre el App Service y SQL Server (dentro del backbone de Azure, así que no era catastrófico pero sí imprudente). Un escáner de cumplimiento detectó la configuración seis meses después y obligó a una migración: cambiar a `Encrypt=True` y reiniciar las apps. Tres horas de ventana de mantenimiento por una opción que debería haber sido el default desde el día uno.

**Caso 2 — Always Encrypted sin pensar.** Un cliente bancario, "para estar seguros", configuró Always Encrypted sobre todas las columnas de la BD. Funcionó bien durante el desarrollo. Al pasar a producción descubrieron que **no podían hacer `WHERE` ni `ORDER BY`** sobre las columnas cifradas, salvo con cifrado determinista (que tiene sus propias limitaciones). El reporting nocturno tardaba horas en vez de minutos porque hacía table scans sin índices útiles. Tuvieron que revertir 80% de las columnas a TDE estándar. **Lección**: Always Encrypted es para columnas ultra-sensibles concretas (tarjeta, SSN), no para todo.

**Caso 3 — el CORS de "lo abro todo".** Una API tenía CORS configurado con `AllowAnyOrigin()` y `AllowCredentials()` "para que sea fácil de probar desde cualquier sitio". Era una combinación silenciosamente prohibida por la especificación de CORS y la mayoría de browsers la rechazaban, pero un investigador de seguridad encontró que **un usuario autenticado que visitara una página maliciosa veía su sesión usable desde esa página**. Reporte responsable, parche rápido (cambio a orígenes explícitos), y un susto.

Los tres casos los previenen las tres tablas del ejemplo: `EncryptionAdvisor` te orienta sobre cuándo CMK o Always Encrypted; `TlsTransitValidator` audita las connection strings; `CorsPolicyValidator` detecta la combinación prohibida.

---

## 3. Por qué esto importa en tu stack

Si tienes datos en Azure —y todos los sistemas tienen datos— tres preguntas que debes responder antes de pasar a producción:

- **¿Mi servicio fuerza HTTPS y TLS 1.2?** App Service tiene un toggle "HTTPS Only" y una opción de "Minimum Inbound TLS Version". Storage tiene "Secure transfer required" y "Minimum TLS Version". Si no están en sus valores correctos, te llegan conexiones inseguras desde clientes legacy.
- **¿Mis conexiones a BD y Storage usan el canal cifrado?** Las connection strings sin `Encrypt=true` o sin `https://` son vulnerabilidad. El validador del ejemplo es exactamente lo que el linter de tu equipo debería ejecutar sobre cada PR que toque configuración.
- **¿Mi CORS está bien configurado?** Es de las cosas más mal configuradas en proyectos reales. La regla es simple: orígenes explícitos en producción, nunca `*` con credenciales.

Para el cifrado at-rest, la respuesta corta es "Azure lo hace por defecto y normalmente eso basta". Solo cuando la regulación de tu sector (banca, sanidad, gobierno) exige controlar las claves, hay que ir a CMK. Solo cuando hay columnas ultra-sensibles que ni Azure debe poder leer, hay que ir a Always Encrypted.

---

## 4. La analogía vertebradora: el almacén y los camiones

Imagina un negocio que almacena mercancía en un almacén externo (Azure) y la transporta entre clientes y proveedores (in-transit). Hay dos preguntas de seguridad:

**Cómo guardas la mercancía en el almacén (at-rest):**

- **Cajas con candado del almacén** (MMK, Microsoft-managed keys): el almacén te garantiza que todas las cajas tienen candado y solo ellos tienen la llave maestra. Es lo que pasa por defecto: AES-256 gestionado por Microsoft. Cumple para el 90% de los negocios.
- **Cajas con tu propio candado** (CMK, Customer-managed keys): cuando tu sector lo exige (banca, sanidad, sector público), aportas tú el candado y la llave maestra. Si en algún momento revocas la llave (por ruptura de contrato, por incidente), el almacén ya no puede leer tus cajas — ni siquiera ellos. Es más control, pero también más responsabilidad.
- **Cajas selladas que solo abre el destinatario** (Always Encrypted): la caja viaja sellada desde el remitente hasta el cliente final. Ni el almacén ni el transportista pueden ver el contenido. Pero como el almacén no ve dentro, no puede ordenar las cajas por contenido ni hacerte un inventario detallado — solo puede contarlas y buscarlas por etiqueta exterior.

**Cómo transportas la mercancía entre puntos (in-transit):**

- **Camiones con remolque cerrado y precinto** (TLS 1.2+): nadie puede ver ni manipular la carga durante el trayecto. Es lo mínimo aceptable hoy.
- **Camiones con remolque abierto pero un trayecto controlado** (TLS 1.0/1.1): obsoleto. Ya no se usa en empresas serias.
- **Camiones que reciben pedidos solo de la lista oficial de clientes** (CORS bien configurado): el almacén tiene una lista de clientes con los que opera. Si llega un pedido de un cliente fuera de la lista, lo rechaza. Si la lista es "todos los clientes del mundo" y además "acepto pedidos a nombre de cualquier cliente" (`*` + credentials), cualquier estafador puede simular ser un cliente legítimo.

Mantén la imagen mientras lees el código. El cifrado at-rest es candados en el almacén; el cifrado in-transit es camiones cerrados; CORS es la lista de clientes autorizados.

---

## 5. Recorrido por el código

### `EncryptionAdvisor` — qué cifrado at-rest usar

La función es simple porque la decisión es simple:

```csharp
public static RecomendacionCifrado Recomendar(
    Sensibilidad sensibilidad, bool regulacionExigeControlarClaves)
{
    if (sensibilidad == Sensibilidad.AltamenteConfidencial)
        return new(EstrategiaCifrado.AlwaysEncrypted, true, "...");

    if (regulacionExigeControlarClaves)
        return new(EstrategiaCifrado.CmkAtRest, true, "...");

    return new(EstrategiaCifrado.MmkAtRest, true, "...");
}

public const bool AtRestSiempreActivo = true;
```

Tres reglas, en orden:

1. Si los datos son **altamente confidenciales** (tarjetas, SSN, datos médicos sensibles): **Always Encrypted**. Cifrado client-side: la app cifra antes de mandar a SQL, descifra al recibir. SQL Server ve solo bytes opacos. Ventaja: ni el DBA con acceso completo puede ver el contenido. Limitación: sin `WHERE` ni `ORDER BY` salvo cifrado determinista (que cifra siempre el mismo valor al mismo bytes), que a su vez tiene riesgos de inferencia.
2. Si la **regulación** exige controlar las claves (PCI DSS, HIPAA, ISO 27001 estricto, sector público): **CMK**. Tú generas la clave maestra en Key Vault con purge protection, Azure usa esa clave para cifrar tus datos at-rest. Si revocas la clave, Azure no puede acceder. Requiere Managed Identity del recurso con permisos sobre el Key Vault.
3. **El 90% de los casos**: **MMK** (Microsoft-managed keys). AES-256, gestionado por Microsoft, **activo por defecto en todos los servicios de Azure**, sin configuración necesaria. Es lo que protege automáticamente a tu storage, tu SQL, tu Cosmos, tu blob, sin que toques nada.

Y la constante final, `AtRestSiempreActivo = true`, lo deja claro: **at-rest está siempre cifrado en Azure**. No hay opción de "desactivarlo". Solo eliges qué clave se usa.

### `TlsTransitValidator` — TLS 1.2+ y connection strings

Tres funciones de auditoría que cubren el cifrado en tránsito:

```csharp
public static bool VersionPermitida(string version)
{
    // Acepta "1.2", "TLS 1.2", "v1.2", "TLS1_2"...
    // Devuelve true solo si >= 1.2
}

public static bool SqlCifradoEnTransito(string connectionString) =>
    connectionString.Contains("Encrypt=true", ...)
    || connectionString.Contains("Encrypt=Mandatory", ...)
    || connectionString.Contains("Encrypt=Strict", ...);

public static bool StorageCifradoEnTransito(string connectionString) =>
    connectionString.Contains("DefaultEndpointsProtocol=https", ...)
    || connectionString.StartsWith("https://", ...);
```

La regla de TLS es clara: **TLS 1.0 y 1.1 están deprecados oficialmente**. Apple, Google, Microsoft y el resto del ecosistema los han dejado caer. Si tu servicio acepta TLS 1.0 o 1.1, tienes una puerta abierta para clientes legacy que no necesitas. App Service y Storage tienen una opción "Minimum TLS Version" — ponlo en 1.2 (o 1.3 cuando esté disponible).

Para las connection strings:

- **SQL**: `Encrypt=true` es el default desde versiones recientes de la librería, pero las connection strings copiadas de proyectos antiguos siguen llevando `Encrypt=False`. El validador detecta esto. Los valores `Mandatory` y `Strict` (de versiones modernas) son aún más seguros — exigen no solo cifrado sino también validación estricta del certificado.
- **Storage**: o `DefaultEndpointsProtocol=https` (en connection strings completas) o que el endpoint empiece por `https://` (en configuraciones con Managed Identity). Cualquier `http://` debe rechazarse.

Este validador es exactamente el tipo de regla que un linter de configuración puede aplicar en CI. En proyectos serios, lo conectas a un pre-commit hook que mire los `appsettings.json` y bloquee cualquier connection string sin cifrado.

### `CorsPolicyValidator` — la combinación prohibida y otras señales

CORS (Cross-Origin Resource Sharing) es uno de los temas donde se ven más bugs sutiles. La función central:

```csharp
public static VeredictoCors Validar(
    IReadOnlyList<string> origenes, bool allowCredentials)
{
    var problemas = new List<string>();
    var tieneWildcard = origenes.Any(o => o?.Trim() == "*");

    if (tieneWildcard && allowCredentials)
        problemas.Add("AllowAnyOrigin + AllowCredentials: ...");
    if (tieneWildcard)
        problemas.Add("Origen '*': usa orígenes explícitos en producción.");
    if (origenes.Count == 0)
        problemas.Add("Sin orígenes definidos.");

    foreach (var o in origenes.Where(o => !string.IsNullOrWhiteSpace(o) && o != "*"))
    {
        if (o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !o.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            problemas.Add($"Origen no-TLS en producción: {o}");
    }

    return new VeredictoCors(problemas.Count == 0, problemas);
}
```

Cuatro reglas que aplica:

1. **`*` + credenciales = vulnerabilidad**. La especificación de CORS impide esta combinación, pero algunas configuraciones la sortean (`SetIsOriginAllowed(_ => true)` con `AllowCredentials()`). Una página maliciosa puede entonces hacer peticiones autenticadas con la sesión del usuario. Es exactamente el caso 3 de la sección 2.
2. **`*` solo** ya es problemático en producción. No es tan grave como el anterior (sin credenciales, el servidor no asume identidad), pero te abre a ataques DDoS y a probes desde cualquier sitio. Pon orígenes explícitos.
3. **Lista vacía** suele ser un olvido. Si tu API no se llama desde el navegador, no configures CORS — no es lo mismo "lista vacía" que "no configurar CORS".
4. **Orígenes `http://`** en producción son una contradicción. Solo permite `https://`, excepto en `localhost` para desarrollo.

### `DataProtectionAssessor` — el termómetro

Combina los tres validadores en un assessment global:

```csharp
var items = new (bool ok, string nombre)[]
{
    (c.HttpsForzado, "HTTPS forzado..."),
    (TlsTransitValidator.VersionPermitida(c.TlsMinimo), "TLS 1.2 mínimo"),
    (TlsTransitValidator.SqlCifradoEnTransito(c.SqlConnectionString), "..."),
    (TlsTransitValidator.StorageCifradoEnTransito(c.StorageConnectionString), "..."),
    (c.TdeHabilitado, "TDE habilitado en Azure SQL"),
    (EncryptionAdvisor.AtRestSiempreActivo, "Cifrado at-rest..."),
    (cors.Segura, "CORS con orígenes explícitos..."),
};
```

Siete preguntas, una puntuación 0-100 y una lista de hallazgos. Un sistema con todos los toggles bien configurados saca 100; un sistema con HTTPS forzado pero SQL sin Encrypt saca ~85 y te dice exactamente qué falta. Es la versión "datos" del Secure Score de S6.1.

---

## 6. La pregunta "¿necesito CMK?"

Es la decisión que más confunde en proyectos. Tres criterios para responder:

**Necesitas CMK si**:

- Tu sector lo exige por regulación. Ejemplos: PCI DSS para tarjetas (recomendado, no obligatorio en todos los niveles); HIPAA para datos médicos en USA; ENS Alto en sector público español; ISO 27001 en su nivel más estricto.
- Tu cliente lo pide contractualmente. Algunas grandes empresas exigen CMK para servicios que les afecten, aunque no sea por regulación pública.
- Necesitas poder **revocar el acceso de Azure** a los datos en algún escenario hipotético. Con CMK, si revocas la clave, Azure ya no descifra nada.

**No necesitas CMK si**:

- Eres un negocio normal sin requisitos regulatorios específicos. Los datos van protegidos con AES-256 igualmente; solo cambia quién custodia la clave.
- No tienes proceso para gestionar el ciclo de vida de la clave (rotación, backup, recuperación tras incidente). CMK sin proceso es peor que MMK, porque el día que pierdas la clave pierdes los datos.

**Coste oculto de CMK**:

- Requiere Key Vault con purge protection.
- Requiere Managed Identity del recurso con permisos sobre la clave.
- Si revocas o caduca la clave por accidente, **pierdes acceso a los datos** hasta restaurarla. La gestión del ciclo de vida no es opcional.
- Algunos servicios soportan CMK solo en SKUs Premium (Cosmos Premium, SQL Premium...). Coste extra de plan.

En proyectos donde la respuesta a "¿necesito CMK?" no es obviamente sí, la respuesta correcta es **MMK por defecto y migrar a CMK cuando alguien lo justifique**. No es una decisión que mejores tomando "por si acaso".

---

## 7. Cómo probarlo en local

Es un ejemplo offline:

```bash
dotnet run --project src/Datos.Demo.Api
# http://localhost:5092
```

Endpoints:

```http
### Qué cifrado para datos confidenciales con regulación
GET http://localhost:5092/datos/cifrado?sensibilidad=Confidencial&regulacionExigeClaves=true
# → CmkAtRest

### ¿TLS 1.1 está permitido?
GET http://localhost:5092/datos/tls/1.1
# → false

### Validar política CORS prohibida
POST http://localhost:5092/datos/cors
Content-Type: application/json

{
  "origenes": ["*"],
  "allowCredentials": true
}
# → { segura: false, problemas: ["AllowAnyOrigin + AllowCredentials: ..."] }

### Checklist completo
POST http://localhost:5092/datos/checklist
Content-Type: application/json

{
  "httpsForzado": true,
  "tlsMinimo": "1.2",
  "sqlConnectionString": "Server=...;Database=...;Encrypt=true;...",
  "storageConnectionString": "DefaultEndpointsProtocol=https;...",
  "tdeHabilitado": true,
  "sensibilidadMaxima": "Confidencial",
  "regulacionExigeClaves": false,
  "corsOrigenes": ["https://miapp.com"],
  "corsAllowCredentials": true
}
# → { puntuacion: 100, cifradoRecomendado: "MmkAtRest", hallazgos: [] }
```

Los 30 tests cubren cada combinación: nueve casos de cifrado at-rest, los cinco patrones de connection string (Encrypt=true / Mandatory / Strict / False / sin Encrypt), la combinación prohibida de CORS, varios orígenes mixtos.

Para auditar la postura real:

- `scripts/01-data-security-check.sh` — recorre tu suscripción y verifica que App Services tienen HTTPS-only y TLS 1.2 mínimo, Storage tiene secure transfer y TLS mínimo, Azure SQL tiene TDE habilitado. Solo lectura. Requiere rol `Reader`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué este submódulo tampoco tiene CAPA de integración

Misma lógica que en los anteriores. El cifrado at-rest, el cifrado in-transit, el TDE, las CMK, el CORS son **configuración del recurso**, no algo que se emule. No existe "emulador de cifrado at-rest". Lo que sí podemos hacer (y hacemos al 100%):

- Probar la decisión: dado un escenario, qué estrategia recomendamos.
- Probar la auditoría: dada una connection string, ¿exige cifrado o no?
- Probar el detector de configuraciones prohibidas (el `*` + credentials).
- Verificar que el grafo DI compone bien el assessor.

La validación real es manual: configurar un App Service y un Storage en una suscripción de pruebas, ejecutar el script de auditoría, comprobar que detecta los toggles. Esa validación se hace una vez por entorno, no en cada commit.

---

## 9. La lista de verificación pre-producción

Antes de pasar un servicio a producción, ejecuta mentalmente esta lista:

- [ ] **HTTPS Only activo** en App Service / Function App.
- [ ] **Minimum Inbound TLS Version** = 1.2 (o 1.3 si está disponible) en App Service.
- [ ] **Secure transfer required** activo en Storage Account.
- [ ] **Minimum TLS Version** = 1.2 en Storage.
- [ ] **TDE** habilitado en Azure SQL (por defecto sí, comprobar que no está apagado).
- [ ] **Connection strings** de SQL llevan `Encrypt=true` (o Mandatory/Strict).
- [ ] **Connection strings** de Storage usan `https://` o `DefaultEndpointsProtocol=https`.
- [ ] **CORS** configurado con orígenes explícitos (no `*`), idealmente sin `AllowCredentials` si no es necesario.
- [ ] **CMK / Always Encrypted** solo si tu regulación o sector lo exige (no "por si acaso").
- [ ] **Auditoría de Key Vault** activada cuando uses CMK.

Es la lista corta. La versión larga incluye Defender for SQL, Microsoft Defender for Storage, alertas configuradas, etcétera. Pero esta lista corta detecta el 80% de los problemas reales.

---

## 10. Glosario breve

- **At-rest**: cuando los datos están almacenados en disco/Blob/BD, no en movimiento. Cifrado por Azure por defecto con AES-256.
- **In-transit**: cuando los datos se mueven entre servicios o entre cliente y servicio. Cifrado por TLS.
- **MMK** (Microsoft-Managed Keys): Azure genera y custodia las claves. La opción por defecto, transparente al usuario.
- **CMK** (Customer-Managed Keys): tú aportas la clave (típicamente desde Key Vault). Más control, más responsabilidad.
- **Always Encrypted**: cifrado client-side en SQL. Ni Azure ni el DBA con permisos pueden leer las columnas cifradas.
- **TDE** (Transparent Data Encryption): cifrado at-rest de Azure SQL. Activo por defecto. Diferente de Always Encrypted.
- **TLS** (Transport Layer Security): protocolo que cifra el canal de comunicación. Versiones 1.2 y 1.3 son las únicas vivas hoy.
- **HSTS** (HTTP Strict Transport Security): cabecera que indica al navegador "habla siempre conmigo en HTTPS". Se configura con `app.UseHsts()` en ASP.NET Core.
- **CORS** (Cross-Origin Resource Sharing): mecanismo que controla qué orígenes pueden hacer peticiones a tu API desde un navegador. Mal configurado, es vulnerabilidad.

---

## 11. Cierre

Los datos en Azure están cifrados por defecto, tanto en reposo como en tránsito. Lo que sí depende de ti es asegurarte de que las opciones por defecto están activas (HTTPS-only, TLS 1.2 mínimo, Encrypt=true en las connection strings) y de que CORS no tiene la combinación prohibida. CMK y Always Encrypted son herramientas para casos concretos, no para "por si acaso".

Lo siguiente es [`S6.6 — Azure Key Vault`](../S6.6-key-vault/MANUAL.md), el servicio donde se guardan los secretos, claves y certificados — la pieza que cierra el módulo de seguridad y te permite tener cero secretos en código y en configuración.
