# Manual del alumno — S8.5 · IaC con Bicep

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta por qué Bicep es la opción correcta para infra Azure-only, qué linter aplicar antes de subir, qué hace exactamente `az deployment what-if` y por qué un `Delete:` de un recurso stateful es la alarma roja del modelo.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M08-S8.5](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.5-iac-bicep-v3.md). Tres piezas de lógica pura (linter de `.bicep`, parser del output de `what-if`, comparativa Bicep/ARM/Terraform) más una **CAPA de integración real con `bicep build`** — el primer submódulo de M08 que la incluye.

*Creado: 2026-05-21 00:05 +0200*

---

## 1. La idea en una frase

Infrastructure as Code es **declarar la infra en un archivo versionado** y dejar que la herramienta la ejecute reproducible y idempotentemente. Bicep es el lenguaje IaC oficial de Microsoft para Azure: un DSL legible (sin verbosidad de ARM JSON), módulos nativos, VS Code extension de Microsoft, sin "state file" porque Azure es el estado. La conversación de S8.5 son tres decisiones operativas: elegir herramienta (Bicep para Azure-only, Terraform para multi-cloud, ARM solo para mantenimiento legacy), validar el archivo con linter antes de subir (no exponer secretos en código, no permitir passwords literales), y **siempre `what-if` antes de `create`** — si ves un `Delete:` de un recurso con datos, paras.

El submódulo incluye una integración real: si tienes `bicep` en PATH, los tests invocan `bicep build` contra un Bicep de prueba y validan el ARM JSON resultante. Si no tienes Bicep, el test se salta limpiamente (`SkippableFact`).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican el linter, el what-if y el plan de migración:

**Caso 1 — el password literal en Bicep.** Un equipo escribió un Bicep para SQL Server con `administratorLoginPassword: 'Pa$$w0rd!'` en línea. Subieron el archivo a git. Cuando se dieron cuenta dos semanas después de que el password estaba en el repo, **rotaron la contraseña** y actualizaron 12 Bicep en cinco repos. La validación de `BicepFileValidator.Validar` del ejemplo detecta esto inmediatamente: regla "Password= literal en el código → ERROR".

**Caso 2 — el `Delete:` que casi borra Storage.** Otro equipo modificó un Bicep "para limpiar configuración redundante". El cambio quitó accidentalmente un parámetro que controlaba un `existing: true` en una storage account. Ejecutaron `az deployment what-if` antes de aplicar y vieron: "**Delete: storageAccounts/dbbackups [Microsoft.Storage/storageAccounts]**". **El parser del ejemplo lo marca como riesgo alto** porque Storage está en la lista de recursos stateful: "⚠ DELETE de recurso STATEFUL: dbbackups. Revisa que tienes backup antes de aplicar". Sin el what-if, hubieran perdido los backups de la BD.

**Caso 3 — la migración de Terraform a Bicep "por modernizar".** Una empresa con Terraform funcionando para 30 recursos Azure decidió migrar todo a Bicep "porque es oficial de Microsoft". Tres meses para reescribir, formar al equipo, perder funcionalidad de state que tenían en Terraform. Resultado final: misma infra, distinta sintaxis, equipo desorientado. El advisor del ejemplo da la recomendación honesta: "Equipo ya en Terraform → mantener". **No migrar por modernizar**.

Los tres casos los resuelve el ejemplo: linter detecta passwords literales, parser de what-if avisa de Deletes stateful, advisor da recomendaciones sin sesgo de moda.

---

## 3. Por qué esto importa en tu stack

Si tu infra Azure crece más allá de un par de recursos, IaC empieza a ser obligatorio. Tres preguntas a tener claras:

- **¿Qué herramienta?** Solo Azure y empezando → Bicep. Multi-cloud o equipo ya en Terraform → Terraform. ARM legacy en mantenimiento → no migrar. El advisor te lo dice con criterio.
- **¿Cómo valido el Bicep antes de subir?** Pre-commit hook con el linter del ejemplo: rechaza passwords literales, exige `@secure()` en params de secretos, avisa de outputs con nombres sospechosos.
- **¿Cómo aplico cambios a producción sin romper nada?** Pipeline IaC con tres stages: validate (`bicep build`), preview (`az deployment what-if`), deploy (con aprobación humana del environment). Si en el preview ves `Delete:` de stateful, paras y revisas.

---

## 4. La analogía vertebradora: el plano del arquitecto

Imagina construir una casa con dos formas distintas:

- **Sin plano** (clicks en Portal): el albañil va construyendo según le indicas en cada momento. "Aquí una pared", "ahora una ventana", "más alta esta". Funciona para casas pequeñas. Para una casa de tres pisos, **nadie sabe exactamente cómo es la casa** sin recorrerla. Reconstruir o reproducir es imposible.
- **Con plano** (IaC): hay un documento escrito que describe la casa. Lo lees, lo entiendes, lo revisas, lo apruebas. Cuando construyes, el albañil sigue el plano. Si quieres otra casa idéntica, vuelves a usar el plano. Si quieres cambiar algo, modificas el plano antes y luego lo reconstruyes.

Y al modificar el plano, **el arquitecto NO empieza a tirar paredes**. Hace un **estudio de cambios**: "voy a quitar esta pared, abrir esta ventana, mover este tabique". El cliente revisa el estudio antes de aprobar la obra. Si en el estudio aparece "demoler la cocina entera", el cliente para porque sabe que la cocina tiene datos importantes (la calefacción central, las tuberías de gas). Eso es `what-if`.

**Tres tipos de planos disponibles**:

- **El plano del fabricante de la casa Azure** (Bicep): específico para casas Azure. Sintaxis cómoda, módulos nativos, soportado por el fabricante. La opción por defecto si tu casa solo es Azure.
- **El plano antiguo del mismo fabricante** (ARM JSON): existía antes de Bicep. Mismo concepto, sintaxis verbose de JSON. Mantienes si lo tienes; no migras desde Bicep.
- **El plano universal** (Terraform): sirve para casas de cualquier fabricante (Azure, AWS, GCP). Más flexible, requiere mantener tu propio cuaderno de notas de qué casas has construido (state file). Útil si construyes en varios sitios.

Y la regla operativa: **nunca empezar a construir sin estudio de cambios firmado**. El estudio cuesta 5 minutos y te ahorra demoliciones por accidente.

Mantén la imagen: plano, estudio de cambios, alarma roja ante demoliciones de habitaciones con datos.

---

## 5. Recorrido por el código

### `BicepFileValidator.Validar` — el linter pre-commit

La función central:

```csharp
foreach line in lineas:
    if line.Contains("Password=") && !esComentario:
        errores.Add("Connection string con 'Password=' literal");

    if line matches "param <nombre> string" and nombre parece secreto:
        if no hay @secure() en las dos líneas anteriores:
            errores.Add($"Parámetro '{nombre}' parece secreto: añade @secure()");

if !bicepTexto.Contains("targetScope"):
    avisos.Add("Sin `targetScope` declarado: por defecto es `resourceGroup`");

foreach output line:
    if nombre del output contiene "password|secret|key|token|connection":
        avisos.Add("`output` parece exponer un secreto: revísalo");
```

Cuatro reglas que cubren los anti-patterns del slide 11:

1. **Password literal en código → ERROR**. `linea.Contains("Password=")` detecta connection strings. El validador ignora líneas comentadas para no asustar con `// Password=...`.
2. **Parámetro string con nombre sospechoso sin `@secure()` → ERROR**. Si tu param se llama `dbPassword`, `apiSecret`, `accessKey`, `authToken` o `connectionString`, debe llevar `@secure()` en la línea anterior. Sin decorador, el valor aparece **en plano en los logs de despliegue** y en el ARM JSON generado.
3. **Sin `targetScope` → AVISO**. Por defecto es `resourceGroup`. Si tu Bicep es para subscription o managementGroup, decláralo explícitamente.
4. **`output` con nombre sospechoso → AVISO**. Los outputs de un Bicep se quedan persistidos en el deployment de Azure. Si un output se llama `dbPassword`, el password queda accesible para cualquiera con acceso al deployment.

Los nombres "sospechosos":

```csharp
private static readonly string[] SecretosEnNombre =
[
    "password", "secret", "key", "token", "connection",
];
```

La detección no es perfecta (puede haber falsos positivos: un param llamado `tokenLifetime` no es un secreto), pero el ratio señal/ruido es muy bueno. Como pre-commit hook salva incidentes reales.

### Ejemplo válido vs inválido

**❌ Bicep que falla el validador:**

```bicep
param dbPassword string = 'Pa$$w0rd!'         // sin @secure ⇒ ERROR
param tenantId string

resource sql 'Microsoft.Sql/servers@2023-08-01' = {
  name: 'mi-sql'
  properties: {
    administratorLoginPassword: 'Pa$$w0rd!'   // Password= ⇒ ERROR
  }
}

output adminPassword string = dbPassword       // output con secreto ⇒ AVISO
```

**✅ Bicep que pasa el validador:**

```bicep
targetScope = 'resourceGroup'                  // sin AVISO

@secure()
param dbPassword string                         // @secure correcto

param tenantId string

resource sql 'Microsoft.Sql/servers@2023-08-01' = {
  name: 'mi-sql'
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: dbPassword     // referencia, no literal
  }
}

output sqlFqdn string = sql.properties.fullyQualifiedDomainName  // output limpio
```

El password se pasa al deployment desde un `params.prod.json` con `@Microsoft.KeyVault(...)` reference. **El secreto nunca pasa por el repo**.

### `WhatIfDiffParser.Parsear` — la alarma del `Delete:`

`az deployment group what-if` devuelve un texto con marcadores en cada línea:

- `+ <recurso>` → recurso nuevo (Create).
- `~ <recurso>` → modificación (Modify).
- `- <recurso>` → recurso a borrar (Delete).
- `= <recurso>` → sin cambios.

El parser identifica el marcador y extrae el tipo de recurso (entre corchetes o inferido del `resourceId` con prefijo `Microsoft.`).

La pieza más importante:

```csharp
private static readonly string[] TiposStateful =
[
    "Microsoft.Storage/storageAccounts",
    "Microsoft.DocumentDB/databaseAccounts",   // Cosmos
    "Microsoft.Sql/servers",
    "Microsoft.Sql/servers/databases",
    "Microsoft.KeyVault/vaults",
    "Microsoft.ServiceBus/namespaces",
];

if (tipo == Delete && TiposStateful.Any(s => tipoAzure.StartsWith(s, ...)))
    avisos.Add($"⚠ DELETE de recurso STATEFUL: {recurso}. Revisa backup antes.");
```

**Un `Delete:` de App Service no es alarma** (sin estado persistente; lo reconstruyes en minutos).
**Un `Delete:` de Storage, Cosmos, SQL, Key Vault, Service Bus es alarma**. Esos recursos tienen **datos que no se reconstruyen**: el delete los pierde permanentemente.

La regla operativa: cualquier ejecución de `what-if` debe revisarse y, si hay `Delete:` de un tipo stateful, **se para el deploy hasta verificar**:

1. ¿El delete es intencional?
2. ¿Hay backup de los datos?
3. ¿Está aprobado por el dueño del recurso?

Sin esas tres respuestas en verde, no se aprueba.

### `ToolingComparison.Recomendar` — Bicep, ARM o Terraform

La función decide:

```csharp
if (e.MultiCloud || e.EquipoYaUsaTerraform)
    return Terraform;

if (e.LegacyArmJson && !e.SoloAzure)
    return ArmTemplates;     // mantener legacy, no migrar por modernizar

return Bicep;                 // default: solo Azure, equipo sin estado existente
```

Las tres recomendaciones:

- **Terraform**: si tu infra es multi-cloud O tu equipo ya conoce Terraform. La opción multi-cloud habla por sí sola; el "equipo ya en Terraform" evita la migración del caso 3 (cambiar por moda).
- **ARM Templates**: solo para mantenimiento de templates legacy. No migres `bicep decompile` puede ayudar si quieres pasar a Bicep gradualmente.
- **Bicep**: la opción por defecto para proyectos nuevos en Azure. Sintaxis cómoda, módulos nativos, soporte de Microsoft, sin state file.

La tabla `Comparativa` del ejemplo:

| Feature | ARM | Bicep | Terraform |
| --- | --- | --- | --- |
| Formato | JSON verbose | DSL legible | HCL legible |
| Proveedor | Solo Azure | Solo Azure | Multi-cloud |
| State | No (Azure es state) | No (Azure es state) | terraform.tfstate |
| Módulos | Linked templates | Nativos | Maduros |
| Learning curve | Alta | Baja | Media |
| Microsoft support | Legacy | Recomendado | 3rd-party |
| What-if | Sí | Sí | terraform plan |
| VS Code | Limitado | Bicep extension | Terraform extension |

### `IacPlanner` — el plan + checklist

El servicio inyectable que une los anteriores. Recibe el contexto del proyecto, recomienda herramienta, valida un Bicep si se lo pasas, parsea un what-if si se lo pasas, y emite checklist completa (módulos por dominio, params dev/staging/prod, Key Vault Reference, AVM, pipeline IaC validate→what-if→deploy).

---

## 6. La CAPA de integración con `bicep build`

Primera vez en M08 que hay integración real:

```csharp
[SkippableFact]
public async Task BicepBuild_CompilaBicepValidoAArmJson()
{
    var bicep = WhichBicep();
    Skip.If(bicep is null, "bicep no está en PATH — skip.");

    // Escribe un Bicep mínimo en %TEMP%
    var bicepFile = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.bicep");
    await File.WriteAllTextAsync(bicepFile, """
        targetScope = 'resourceGroup'

        param location string = resourceGroup().location

        resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
          name: 'plan-test'
          location: location
          sku: { name: 'F1' }
        }
        """);

    // Ejecuta `bicep build <archivo>` → genera <archivo>.json
    var psi = new ProcessStartInfo(bicep!, $"build \"{bicepFile}\"") { ... };
    var proc = Process.Start(psi)!;
    await proc.WaitForExitAsync();
    Assert.Equal(0, proc.ExitCode);

    // Lee el ARM JSON resultante y verifica
    var jsonFile = Path.ChangeExtension(bicepFile, ".json");
    var json = await File.ReadAllTextAsync(jsonFile);
    Assert.Contains("Microsoft.Web/serverfarms", json);
}
```

Tres ideas que enseña este test:

1. **`bicep build` es local e idempotente**: convierte un `.bicep` en `.json` (ARM Template) sin tocar Azure. Útil para validar sintaxis en pre-commit o en CI sin desplegar.
2. **`SkippableFact`** (que viste en M04-S4.5): si `bicep` no está en PATH, el test no falla, se salta. La suite queda verde en cualquier máquina.
3. **El test prueba el flujo real**: escribe Bicep en disco, invoca el CLI, lee el ARM JSON resultante, verifica que contiene el tipo de recurso esperado. Es lo más cercano a "validar Bicep" sin Azure.

En tu pipeline, este patrón se usa para **validar que el Bicep compila** antes de intentar desplegar. Una compilación exitosa garantiza sintaxis válida y referencias resueltas; un `bicep build` que falla muestra el error con línea y columna.

Para instalar Bicep en local: `az bicep install`. El script `_lib.sh` del demo lo hace automáticamente la primera vez.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Iac.Bicep.Demo.Api
# http://localhost:5109
```

Endpoints:

```http
### Validar un Bicep
POST http://localhost:5109/iac/validar
Content-Type: application/json

"@secure() param dbPassword string\nparam admin string"
# → { valido: true, errores: [], avisos: ["Sin targetScope..."] }

### Parsear output de what-if
POST http://localhost:5109/iac/whatif/parsear
Content-Type: application/json

"+ Microsoft.Web/sites/mi-app\n- Microsoft.Storage/storageAccounts/dbbackups [Microsoft.Storage/storageAccounts]"
# → { cambios: [...], riesgoAlto: true,
#     avisos: ["⚠ DELETE de recurso STATEFUL: dbbackups..."] }

### Comparativa de herramientas
GET http://localhost:5109/iac/comparativa

### Recomendar herramienta
POST http://localhost:5109/iac/recomendar
Content-Type: application/json

{ "soloAzure": false, "multiCloud": true, "equipoYaUsaTerraform": false }
# → Terraform con razones

### Plan completo
POST http://localhost:5109/iac/plan
```

Los 24 tests cubren el linter (cuatro casos), el parser (el reconocimiento de Delete stateful en cada tipo), el advisor (todas las combinaciones). El test 25 (integración con `bicep build`) se salta si Bicep no está instalado.

Para validar tu Bicep real:

```bash
./scripts/demo.sh
# 1) 01-validate-bicep.sh → az bicep build + az validate + az what-if
```

Compila Bicep, valida contra Azure (sin desplegar), ejecuta what-if contra el RG configurado. Solo lectura: nunca aplica.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test` y `az`.

---

## 8. La estructura típica de un repo de infra

El submódulo lo menciona en el checklist; vale la pena tenerlo claro:

```
infrastructure/
├── main.bicep                  # entry point, llama a módulos
├── modules/
│   ├── app-service.bicep       # un módulo por dominio
│   ├── cosmos.bicep
│   ├── storage.bicep
│   ├── keyvault.bicep
│   └── service-bus.bicep
├── params.dev.json             # parámetros para dev
├── params.staging.json
└── params.prod.json
```

Tres reglas operativas:

1. **`main.bicep` no contiene recursos directos**, solo llama a módulos. Mantiene el archivo principal corto y legible.
2. **Cada módulo es un dominio**: `app-service.bicep` solo declara el plan + sites; `cosmos.bicep` solo Cosmos; etcétera. Sin mezclar dominios.
3. **`params.{env}.json` lleva los secretos como Key Vault Reference**:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "dbPassword": {
      "reference": {
        "keyVault": { "id": "/subscriptions/.../vaults/mi-vault" },
        "secretName": "SqlAdminPassword"
      }
    }
  }
}
```

App Service o el pipeline resuelven la referencia al desplegar; el password real nunca está en el repo.

---

## 9. El pipeline IaC del slide 19

El flujo recomendado:

```yaml
stages:
- stage: Validate
  jobs:
  - job: Build
    steps:
    - script: az bicep build --file infrastructure/main.bicep
    - script: az deployment group validate \
        --resource-group $(rgName) \
        --template-file infrastructure/main.bicep \
        --parameters @infrastructure/params.prod.json

- stage: Preview
  dependsOn: Validate
  jobs:
  - job: WhatIf
    steps:
    - script: az deployment group what-if \
        --resource-group $(rgName) \
        --template-file infrastructure/main.bicep \
        --parameters @infrastructure/params.prod.json
    # Output del what-if visible en logs; reviewer humano antes de aprobar.

- stage: Deploy
  dependsOn: Preview
  jobs:
  - deployment: Apply
    environment: 'production-infra'   # ← aprobación manual aquí
    strategy:
      runOnce:
        deploy:
          steps:
          - script: az deployment group create \
              --resource-group $(rgName) \
              --template-file infrastructure/main.bicep \
              --parameters @infrastructure/params.prod.json
```

Tres ideas:

- **Validate** compila el Bicep y valida contra Azure (sin aplicar). Catch syntactic errors.
- **Preview** ejecuta what-if y muestra el resultado en logs. **Aquí es donde el operador humano revisa los `Delete:`**.
- **Deploy** aplica con aprobación manual del environment. Sin aprobación, no aplica.

El pipeline nunca aplica sin pasar por la revisión del what-if. Es la diferencia entre IaC defensivo (cero accidentes) y "vamos a aplicar y ver qué pasa" (caso 2 de la sección 2).

---

## 10. Glosario breve

- **IaC** (Infrastructure as Code): infraestructura declarada en archivos versionados.
- **Bicep**: DSL de Microsoft para infra Azure. Sucesor moderno de ARM JSON.
- **ARM Template**: el formato JSON anterior a Bicep. Legacy pero soportado.
- **Terraform**: IaC multi-cloud de HashiCorp. Requiere state file (`terraform.tfstate`).
- **`bicep build`**: comando local que convierte `.bicep` a ARM JSON. Sin tocar Azure.
- **`az deployment group validate`**: valida un deployment contra Azure sin aplicar. Detecta referencias rotas, permisos faltantes.
- **`az deployment group what-if`**: previsualiza qué cambios aplicará un deployment. **Obligatorio antes de `create`**.
- **`az deployment group create`**: aplica el Bicep. Idempotente: re-ejecutar con el mismo archivo no cambia nada.
- **`@secure()`**: decorador de Bicep para marcar un param como sensible. No aparece en logs ni outputs.
- **`existing: true`**: modifier en Bicep para referenciar un recurso existente sin recrearlo.
- **`targetScope`**: declaración de a qué nivel se aplica el Bicep (resourceGroup, subscription, managementGroup, tenant).
- **Módulo Bicep**: archivo `.bicep` que se invoca desde otro como un componente reutilizable.
- **AVM** (Azure Verified Modules): catálogo oficial de Microsoft con módulos Bicep curados y mantenidos.
- **State file** (Terraform): archivo que Terraform mantiene con el estado de la infra. Bicep no lo necesita porque Azure es el state.

---

## 11. Cierre

S8.5 te da las tres piezas operativas de IaC en Azure: linter pre-commit que captura los anti-patterns (passwords literales, params sin `@secure`, outputs con secretos), parser de what-if que avisa de Deletes stateful, y el pipeline IaC validate→what-if→deploy con aprobación humana en el medio. Plus el comparador honesto Bicep vs ARM vs Terraform que evita el caso 3 (migrar por modernizar).

Lo siguiente sería S8.6 (Application Insights y monitoring) que está aún en construcción por el otro chat. Cuando termine, cerramos M08 con ese submódulo.
