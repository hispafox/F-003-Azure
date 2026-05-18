# S5.4 — Managed Identity: conexión sin secretos

> **Submódulo de referencia:** [M05-S5.4](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.4-managed-identity-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API + `DefaultAzureCredential` · **Coste:** ≈ 0 € (App F1 + Storage)

> ℹ️ Este submódulo es **transversal**: no añade un servicio de datos
> nuevo, cambia *cómo* la app se conecta a los de S5.1–S5.3 — **sin
> keys ni passwords**, con la identidad del recurso.

## Objetivo

Mostrar el patrón keyless completo y la lógica que lo respalda:

| Concepto | Dónde |
| --- | --- |
| `DefaultAzureCredential` desde config (UAMI, tenant, local) | [`CredentialFactory.cs`](src/ManagedIdentity.Demo.Api/Security/CredentialFactory.cs) |
| Un único `TokenCredential` singleton compartido | [`Program.cs`](src/ManagedIdentity.Demo.Api/Program.cs) |
| Cliente (Blob) conectado solo con la credencial, sin key | `Program.cs` + `/blob/contenedores` |
| Detectar secretos en config / Key Vault refs | [`ConnectionSecretScanner.cs`](src/ManagedIdentity.Demo.Api/Security/ConnectionSecretScanner.cs) |
| Rol RBAC mínimo + sufijo de App Setting MI | [`RbacRoleAdvisor.cs`](src/ManagedIdentity.Demo.Api/Security/RbacRoleAdvisor.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| El problema de las connection strings con secretos | 2 | [`ConnectionSecretScanner.cs`](src/ManagedIdentity.Demo.Api/Security/ConnectionSecretScanner.cs) |
| System-assigned MI + dar permisos | 4-5, 8 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| `DefaultAzureCredential` en .NET (mismo código local/Azure) | 6-7 | [`CredentialFactory.cs`](src/ManagedIdentity.Demo.Api/Security/CredentialFactory.cs) + `Program.cs` |
| App Service → Storage con MI (solo la URL) | 8 | `Program.cs` (`StorageBlobEndpoint`) + `/blob/contenedores` |
| Key Vault References (`@Microsoft.KeyVault(...)`) | 10 | `ConnectionSecretScanner` (la trata como **sin** secreto) |
| Checklist de seguridad de datos | 13 | `/seguridad/checklist` (escanea sección `Conexiones`) |
| MI para Azure SQL (`Active Directory Default`) | 14 | `RbacRoleAdvisor.SufijoAppSetting(AzureSql)` |
| Troubleshooting (MI habilitada, rol, propagación) | 16, 24 | [`scripts/02-smoke-test.sh`](scripts/02-smoke-test.sh) |
| Mapa MI: App Setting + rol por servicio | 17 | [`RbacRoleAdvisor.cs`](src/ManagedIdentity.Demo.Api/Security/RbacRoleAdvisor.cs) |
| Token caching: credencial singleton compartida | 21 | `Program.cs` (`AddSingleton<TokenCredential>`) + test DI |
| User-Assigned MI (`ManagedIdentityClientId`) | 22 | `CredentialFactory` (`Azure:UserAssignedClientId`) |
| RBAC least privilege (no Owner/Contributor) | 23, 27 | `RbacRoleAdvisor` (`Recomendar` + `EsRolPeligroso`) |
| Cross-tenant (`TenantId`) | 25 | `CredentialFactory` (`Azure:TenantId`) |

## Estructura

```
S5.4-managed-identity/
├── src/ManagedIdentity.Demo.Api/
│   ├── Security/   CredentialFactory, ConnectionSecretScanner,
│   │               RbacRoleAdvisor   (lógica pura)
│   ├── Endpoints/  SeguridadEndpoints (scan / rol / checklist / blob)
│   └── Program.cs  TokenCredential singleton + BlobServiceClient keyless
├── tests/ManagedIdentity.Demo.Api.Tests/
│   ├── Unit_*            lógica pura (credential, scanner, rbac)
│   └── DiContainer_Tests resuelve el grafo real + verifica singleton
└── scripts/        01-provision (MI + rol mínimo) / 02-smoke / 03-cleanup
```

## Tests

```bash
dotnet test     # 35 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `CredentialFactory` (config → `DefaultAzureCredentialOptions`:
  UAMI, tenant, local-dev), `ConnectionSecretScanner` (secreto vs MI vs
  Key Vault ref), `RbacRoleAdvisor` (rol mínimo, sufijo App Setting,
  detección de roles peligrosos — y ninguno recomendado lo es).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve `TokenCredential` +
  `BlobServiceClient` y verifica que **es la misma credencial singleton**
  (slide 21). Construir credencial/cliente no autentica (lazy) → corre
  sin Azure ni red. Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).

> 🧠 **No hay CAPA de integración (a propósito)**: Managed Identity /
> Entra ID **no se puede emular** — Azurite y el emulador de Cosmos usan
> una key fija, no AAD. Un round-trip real exige Azure + `az login`, lo
> que dejaría `dotnet test` dependiente de credenciales. La parte
> testable se aísla en lógica pura (CAPA 1) + el grafo DI (CAPA 0); la
> demo real (`/blob/contenedores`) se prueba a mano contra Azure.

## Ejecución local

```bash
az login                       # DefaultAzureCredential usará tu identidad
dotnet run --project src/ManagedIdentity.Demo.Api
# http://localhost:5084  — usa src/ManagedIdentity.Demo.Api/api.http
```

`/seguridad/*` funcionan **offline** (lógica pura). `/blob/contenedores`
solo responde si configuras `StorageBlobEndpoint` a un Storage real al
que tu `az login` tenga el rol *Storage Blob Data Reader* — **no hay
key** en ningún sitio (ese es el punto). `appsettings.Development.json`
pone `Azure:LocalDev=true` para saltar el timeout de IMDS (slide 16).

## Despliegue por Portal (MI sin secretos)

1. **App Service** — créalo (o reutiliza el de M02). *Settings →
   Identity → System assigned →* **On** (slide 5).
2. **Permisos** — en el **Storage** destino: *Access Control (IAM) →
   Add role assignment →* rol **Storage Blob Data Reader** (mínimo
   necesario, slide 23 — *no* Contributor/Owner) → *Assign access to:
   Managed identity* → tu App Service.
3. **App Setting** — en la App: *Environment variables* →
   `StorageBlobEndpoint = https://<cuenta>.blob.core.windows.net`
   (solo la URL, **sin key**, slide 8).
4. **Azure SQL** — autenticación Entra ID: conéctate como admin y
   `CREATE USER [<app>] FROM EXTERNAL PROVIDER;` +
   `ALTER ROLE db_datareader/db_datawriter ADD MEMBER [<app>];`
   (slide 14). Connection string con
   `Authentication=Active Directory Default;` (sin password).
5. **Cosmos** — *Access control (IAM)* no aplica al plano de datos;
   usa `az cosmosdb sql role assignment` con *Cosmos DB Built-in Data
   Contributor* sobre el `principalId` (slide 5/8).
6. **Secretos de terceros** — lo que no soporta MI (APIs externas) va a
   **Key Vault** y se referencia desde App Settings con
   `@Microsoft.KeyVault(VaultName=...;SecretName=...)` (slide 10).
7. **Endurecer** — *HTTPS Only* On, *TLS 1.2* mínimo, firewall del
   Storage/SQL en Deny por defecto (slide 11, 13).

> Scripts `az` equivalentes en [`scripts/`](scripts) (`./demo.sh`):
> `01-provision.sh` crea App F1 + Storage, habilita MI system-assigned y
> asigna el rol **mínimo** scoped a la cuenta; `02-smoke-test.sh` sigue
> el flujo de troubleshooting de la slide 24; `03-cleanup.sh` borra el
> RG. Complemento de clase, no sustituto del Portal.

## La regla de oro (slide 27)

```
MI   = WHO   (identidad sin password)
RBAC = WHAT  (rol mínimo, scope mínimo — nunca Owner/Contributor)
CA   = WHEN/WHERE (Conditional Access, defensa en profundidad)
```

Cero connection strings con secretos en App Settings. El mismo código
en local (`az login`) y en Azure (MI), sin cambios.

## Próximo paso

[`S5.5 — Backups, replicación y DR`](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.5-backups-v3.md):
recuperación ante desastres y point-in-time restore.
