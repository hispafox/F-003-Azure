# Manual del alumno — S5.P · Práctica: Cosmos DB con Managed Identity

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Este manual va antes: te cuenta qué se pone a prueba, cuál es el entregable y cómo demostrar que de verdad funciona keyless.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M05-S5.P](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P-practica-v3.md). Este es el **primer cierre práctico** del módulo: integra **S5.3** (Cosmos: partición, RU, modelo) y **S5.4** (Managed Identity: cero secretos).

*Creado: 2026-05-20 00:02 +0200*

---

## 1. La idea en una frase

S5.3 te enseñó a diseñar bien con Cosmos. S5.4 te enseñó a conectarte sin secretos. Esta práctica es la prueba de que sabes hacer las dos cosas **a la vez**, en una app desplegada en Azure. Un CRUD pequeño, una decisión de partition key justificada, cero `AccountKey` en ningún App Setting. Y un test al final que decide si lo conseguiste o no.

---

## 2. La prueba definitiva: regenera la key

Aquí está el contrato de la práctica, en una sola frase de la slide 13: **si regeneras la AccountKey de tu Cosmos DB y la app sigue funcionando, de verdad usa Managed Identity.**

Es brutalmente simple. En Azure, la AccountKey se rota con un clic. Si tu app la estaba usando por debajo —aunque tu código declare `DefaultAzureCredential` por todas partes—, dejará de conectarse el segundo siguiente. Si no la estaba usando, no se entera. Esa diferencia es el examen de S5.P, y se hace en treinta segundos al final del entregable.

| Lo que se entrega | Cómo se mide |
| --- | --- |
| App con `CosmosClient` construido con `DefaultAzureCredential`, sin key | Inspección del código + auditoría de App Settings |
| Web App con System-Assigned Managed Identity | `webapp identity show` devuelve `principalId` |
| Rol *Cosmos DB Built-in Data Contributor* asignado al `principalId` | `az cosmosdb sql role assignment list` |
| App Settings solo con `CosmosEndpoint` (la URL) | `GET /practica/auditoria-secretos` → `todoLimpio: true` |
| CRUD funcionando contra el Cosmos real | `POST/GET /productos` desde la app desplegada |
| Pasar la prueba de la key rotada | Regenerar key en Portal → la app sigue respondiendo |

Las cinco primeras se preparan. La sexta cierra el círculo. Todas o ninguna.

---

## 3. Lo que reutilizas de S5.3 y S5.4

Esta práctica monta sobre los dos submódulos previos sin reinventar:

- **De S5.4 (`CredentialFactory.cs`)** — la misma factory de `DefaultAzureCredential` con `UserAssignedClientId`, `TenantId` y `LocalDev`. Registrada como `AddSingleton<TokenCredential>` y compartida por el `CosmosClient` (slide 21 de S5.4).
- **De S5.3 (`CosmosClient` con `Container`)** — el patrón estricto: el `CosmosClient` es singleton, `GetContainer` es singleton, el repo se inyecta singleton. CamelCase de serialización, retry de 429 configurado.
- **De S5.3 (`PartitionKeyAdvisor`)** — la misma tabla de decisión con una regla más (`Estable` — la partition key no debería cambiar nunca para un documento). Aquí la partition key es `/categoria`, no `/clienteId`: la práctica te obliga a justificar por qué.

Y dos cosas nuevas, propias de la práctica:

- **`ZeroSecretsAuditor.cs`** — variante del `ConnectionSecretScanner` de S5.4 que recorre los App Settings activos y dice si **todos** están limpios. Es lo que demuestra el entregable sin tener que mirar a ojo.
- **El endpoint `/practica/auditoria-secretos`** — expone esa auditoría sobre la propia configuración de la app que está corriendo. En Azure, esa respuesta debería decir `todoLimpio: true` con la única entrada visible siendo `CosmosEndpoint` (que es una URL, no un secreto).

---

## 4. Por qué `/categoria` y no `/id`

La decisión de partition key que pide la práctica es deliberada y distinta de la de S5.3. Allí era `/clienteId` porque la query principal era "dame los pedidos de un cliente". Aquí, en un catálogo de productos, la query principal es "dame los productos de una categoría". Por eso `/categoria` cumple las reglas (slide 11 + la "estable" de la práctica):

- **Alta cardinalidad** — tu catálogo tiene decenas o cientos de categorías distintas, no diez.
- **Distribución uniforme** — los productos están repartidos razonablemente; ninguna categoría tiene el 80% del catálogo.
- **Alineada con la query frecuente** — el listado típico filtra por categoría.
- **Estable** — un producto no salta de categoría. La partition key no cambia para un documento dado.

[`PartitionKeyAdvisor.cs`](src/Cosmos.Mi.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) codifica las cuatro reglas y el endpoint `/practica/partition-key` te deja probarlas. Si cambiaras a `/id` (cardinalidad altísima pero cada documento en su propia partición → ningún listado tiene ventaja) o `/marca` (concentración en pocas marcas → hot partition), la regla cazaría el problema.

> 🧠 **La cuarta regla, "estable", merece comentario aparte.** En Cosmos, mover un documento entre particiones no es un `UPDATE`: tienes que **borrarlo de la antigua e insertarlo en la nueva**. Si una propiedad del documento es candidato a partition key pero puede cambiar con el tiempo (un estado, una fecha, una región del usuario), no es buena partition key aunque cumpla las otras tres. Cosmos no tiene `ALTER PARTITION`. La estabilidad es un requisito implícito del diseño.

---

## 5. La auditoría zero-secrets explicada

Mira [`ZeroSecretsAuditor.cs`](src/Cosmos.Mi.Demo.Api/Security/ZeroSecretsAuditor.cs):

```csharp
private static readonly string[] Indicadores =
[
    "password=", "pwd=", "accountkey=", "sharedaccesskey=",
    "accesskey=", "sig=", "secret=",
];

public static bool TieneSecreto(string? valor)
{
    if (string.IsNullOrEmpty(valor)) return false;
    if (valor.Contains("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase))
        return false;
    return Indicadores.Any(i => valor.Contains(i, StringComparison.OrdinalIgnoreCase));
}
```

Es el mismo principio que el `ConnectionSecretScanner` de S5.4, simplificado para auditar una colección de App Settings de golpe. El endpoint `/practica/auditoria-secretos` recorre las entradas cuyo nombre contiene `Cosmos`, `Connection` o `Key` (los sospechosos habituales) y devuelve la lista entera con `tieneSecreto: true/false` por cada uno.

En tu entregable, la respuesta debería ser:

```json
{
  "todoLimpio": true,
  "entradas": [
    { "clave": "CosmosEndpoint", "tieneSecreto": false }
  ]
}
```

Una sola entrada, sin secretos. Si aparece un `CosmosConnection` con `AccountKey=...` o un `CosmosKey` cualquiera, `todoLimpio` será `false` y la práctica no está aprobada. Sencillo y verificable.

---

## 6. Recorrido guiado (en local primero, en Azure después)

Lanza la API (sección 8) y abre [`api.http`](src/Cosmos.Mi.Demo.Api/api.http). Los `/practica/*` funcionan offline; los `/productos/*` necesitan un Cosmos accesible (emulador local con key, o Azure real con `az login` y RBAC).

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `GET /practica/partition-key?cardinalidad=5000&uniforme=true&enQueries=true&estable=true` | `{ veredicto: "Buena" }` | Las cuatro reglas cumplidas (sección 4). |
| 2 | Cambia a `cardinalidad=10` | `{ veredicto: "Mala" }` | Baja cardinalidad rompe la primera regla. |
| 3 | Vuelve a buena pero `estable=false` | `{ veredicto: "Mala" }` | Una partition key cambiante no vale. |
| 4 | `POST /productos` con `categoria: "electronica"` | `201`, `ruConsumidas: ~5-7` | CRUD keyless contra Cosmos (sección 3). |
| 5 | `GET /productos?categoria=electronica` | lista, `crossPartition: false`, `ru: ~3` | Single-partition: filtras por la PK. |
| 6 | `GET /productos` (sin categoria) | lista, `crossPartition: true`, `ru: alto` | Sin PK: cross-partition. La diferencia visible. |
| 7 | `GET /practica/auditoria-secretos` | `todoLimpio: true` con solo `CosmosEndpoint` | El entregable resumido en una respuesta JSON. |

El paso 7 es lo que mira un revisor para validar la práctica. Y la prueba final, la de regenerar la key (sección 9.5), es lo que **demuestra** que el paso 7 no era postureo.

---

## 7. Tests y por qué hay una capa con key dentro de la práctica keyless

Tres capas, **18 pass · 1 skip · 0 fail** sin Docker, **19 pass · 0 skip** con Docker (emulador funcionando):

- **CAPA 1 · Unit** — `PartitionKeyAdvisor` (las cuatro reglas), `ZeroSecretsAuditor` (secreto sí / no / Key Vault reference). Pura.
- **CAPA 0 · DI** — resuelve el grafo **keyless** real: `TokenCredential` + `CosmosClient` + `Container` + repo. **Sin Docker, sin red**, porque ambos SDK (Azure.Identity y Microsoft.Azure.Cosmos) son lazy: construir credencial y cliente no abre conexión. Verifica además que `TokenCredential` es la misma instancia singleton. Es la lección DI con el patrón S5.4 cruzado.
- **CAPA 2 · Integration** — `SkippableFact` contra el emulador de Cosmos en Docker. Aquí hay un detalle sutil:

> 🎓 **El emulador de Cosmos no soporta Managed Identity — usa una key fija pública, no Entra ID.** El test de integración no puede probar el camino keyless realmente; lo que hace es **sustituir** el `CosmosClient` registrado en DI por uno construido con la key del emulador (mediante `ConfigureTestServices` + `IServiceCollection.RemoveAll<CosmosClient>()`), y a partir de ahí ejercita el CRUD y la partition key igual que S5.3. El camino keyless **se valida en CAPA 0 (DI) y a mano contra Azure** (paso de la prueba de la key rotada, sección 9.5). Esta es una limitación del emulador, no del diseño; el manual lo cuenta para que entiendas por qué el test es así.

---

## 8. Poner en marcha la práctica

### 8.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| `az login` | que `DefaultAzureCredential` use tu identidad en local | Sí (para `/productos` contra Azure) |
| Suscripción Azure con permisos | crear Cosmos + Web App + asignación de rol | Sí (para el entregable) |
| Docker | levantar el emulador de Cosmos (opcional, para CAPA 2) | Recomendado |

### 8.2 Compilar y lanzar en local

```bash
cd examples/M05-Almacenamiento-BBDD/S5.P-practica-cosmos-managed-identity
dotnet build Cosmos.Mi.Demo.slnx     # 0 errores, 0 warnings
dotnet run --project src/Cosmos.Mi.Demo.Api
# → http://localhost:5086
```

Los `/practica/*` ya responden. Para el CRUD: levanta el emulador (`docker run -d -p 8081:8081 -p 10250-10255:10250-10255 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest`) o configura `CosmosEndpoint` apuntando a un Cosmos real al que tu `az login` tenga rol *Cosmos DB Built-in Data Contributor*.

### 8.3 Pasar los tests

```bash
dotnet test Cosmos.Mi.Demo.slnx
# Sin Docker: 18 pass · 1 skip · 0 fail
# Con Docker y emulador OK: 19 pass · 0 skip · 0 fail
```

### 8.4 Desplegar el entregable a Azure

Los pasos están en el [`README.md`](README.md), sección *Despliegue por Portal*, y automatizados en `scripts/01-provision.sh`. Resumido:

1. **Cosmos serverless** + database `tienda` + container `productos` con PK `/categoria`.
2. **Web App** con System-Assigned Managed Identity habilitada.
3. **Asignar rol** *Cosmos DB Built-in Data Contributor* al `principalId` de la Web App (no es IAM normal — es role assignment de plano de datos de Cosmos).
4. **App Setting** `CosmosEndpoint` solo con la URL — **sin** `AccountKey`.
5. Desplegar la app, validar `POST/GET /productos`.

### 8.5 La prueba definitiva

```bash
# Antes de regenerar: la app debe responder
curl https://<tu-webapp>.azurewebsites.net/productos?categoria=electronica
# → 200 OK con la lista

# Regenerar la key primaria de Cosmos (en Portal o por CLI)
az cosmosdb keys regenerate --name <tu-cosmos> --resource-group <rg> --key-kind primary

# Esperar 1-2 minutos para que se propague

# Repetir la llamada
curl https://<tu-webapp>.azurewebsites.net/productos?categoria=electronica
# → si sigue 200 OK: APROBADO. La app de verdad usa MI.
# → si pasa a 401/403: la app dependía de la key. Revisa.
```

Si la llamada sigue funcionando tras rotar la key, has demostrado el entregable. Y de paso has practicado un procedimiento real de seguridad: **rotación de claves**. En producción, esto debería ser un calendario, no una sorpresa.

### 8.6 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Forbidden` (403) en Cosmos tras desplegar | RBAC sin propagar (5-10 min normales) o scope mal | espera unos minutos; verifica con `az cosmosdb sql role assignment list` |
| `DefaultAzureCredential failed` en local | falta `az login` | `az login` y reintenta |
| `403` también después de propagación | la MI no está habilitada o rol no asignado | revisa con `webapp identity show` + role assignment |
| RU altísimas en `GET /productos` sin filtro | cross-partition esperado | añade `?categoria=...` |
| Auditoría devuelve `todoLimpio: false` | quedó algún App Setting con `AccountKey=...` | borra el setting; deja solo `CosmosEndpoint` |

---

## 9. Comprueba que lo has entendido

1. ¿Por qué la partition key es `/categoria` y no `/id` para este catálogo? ¿Qué cuatro reglas tiene que cumplir? *(sección 4)*
2. ¿Qué pasa exactamente si regeneras la AccountKey de Cosmos mientras tu app está corriendo? Si sigue funcionando, ¿qué demuestra? *(sección 2)*
3. ¿Por qué el test de integración (CAPA 2) sustituye el `CosmosClient` keyless por uno con key del emulador? ¿Por qué no es trampa? *(sección 7)*
4. ¿Por qué la CAPA 0 puede ejercitar el grafo completo —`TokenCredential` + `CosmosClient` + `Container`— sin Docker ni Azure? *(sección 7)*
5. ¿Qué debería ver un revisor en `GET /practica/auditoria-secretos` para considerar el entregable aprobado? *(secciones 5, 6)*

<details>
<summary>Respuestas</summary>

1. Porque la query frecuente es "productos por categoría". Las cuatro reglas: alta cardinalidad (decenas/cientos de categorías), distribución uniforme (ninguna categoría domina), alineada con la query frecuente (filtrar por `categoria`), **estable** (un producto no cambia de categoría). `/id` rompe la tercera (cada documento en su propia partición; ningún listado se beneficia) y opcionalmente la segunda (depende del volumen).
2. Si la app usaba la key por debajo —aunque el código declarase `DefaultAzureCredential`—, dejará de conectarse: 401/403 inmediato. Si de verdad usaba Managed Identity, la rotación es transparente: el token sigue siendo válido, el rol RBAC sigue asignado. **Que siga funcionando demuestra que el código y la configuración no tienen ninguna dependencia oculta de la key.** Es el examen del entregable.
3. Porque el **emulador de Cosmos no implementa Managed Identity**: usa una key fija pública, no Entra ID. Un test real keyless contra el emulador es imposible. La sustitución se hace explícita en el test, así no hay engaño. El camino keyless se valida en otro sitio: CAPA 0 (que el grafo resuelve bien) + la prueba manual de la key rotada en Azure real. No es trampa porque cada parte se prueba donde se puede probar.
4. Porque **los dos SDK son lazy**. `new DefaultAzureCredential()` no contacta con Entra ID. `new CosmosClient(uri, credential)` no contacta con Cosmos. `client.GetContainer(...)` no contacta con Cosmos. La primera llamada real (`ReadItemAsync`, `CreateItemAsync`...) es la que abre conexión y pide token. Construir el grafo entero solo verifica que las dependencias están bien registradas y que se pueden instanciar — exactamente lo que necesitas para cazar errores de DI en runtime sin Docker.
5. `{ todoLimpio: true, entradas: [{ clave: "CosmosEndpoint", tieneSecreto: false }] }`. Una sola entrada, una URL, sin marcadores de secreto. Si aparecen `CosmosConnection` con `AccountKey=...`, `CosmosKey`, o cualquier otra entrada con `tieneSecreto: true`, el entregable no está aprobado — independientemente de que la app funcione. La auditoría es objetiva.

</details>

---

## 10. Hasta aquí

Esta práctica integra las dos decisiones grandes de M05 que aún no se habían cruzado entre sí: cómo diseñar bien con Cosmos (S5.3) y cómo conectarse sin secretos (S5.4). Cuando rotas la key al final y la app sigue respondiendo, has cerrado los dos hilos a la vez.

Lo que queda del módulo es **S5.P2**, la segunda práctica: Table Storage CRUD aplicado a fondo, con su propia partición y sus consultas OData. Más pequeña y centrada en el servicio más simple de Storage, que cierra el círculo con el primer submódulo del módulo.
