# S1.P2 — Práctica: explorar Azure desde Cloud Shell

> **Práctica de referencia:** [M01-S1.P2](../../../doc/M01-Intro-Azure/v5-actual/M01-S1.P2-practica-cloud-shell-v1.md)
> **Tipo:** primera toma de contacto con Azure CLI · **Duración estimada:** 45-60 min
> **Coste:** 0 € (Storage LRS dentro de free tier; Cloud Shell ~0.10 €/mes despreciable)

> ℹ️ **Este ejemplo es la única excepción a la convención del repo "un proyecto
> .NET con tests xUnit"**: la práctica es **puramente CLI** (Azure CLI + JMESPath),
> no hay código .NET. La validación se hace con `06-smoke-tests.sh`, que
> reemplaza al `dotnet test` que verías en el resto de ejemplos.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: por qué Cloud Shell es prerrequisito mental del curso, las cuatro herramientas que ganas (RG con tags, RBAC, JMESPath, costes) y cuándo elegir Cloud Shell vs `az` local.

## Qué vas a hacer

Esta es la **práctica más simple del curso**: un primer contacto con Azure
desde el navegador, sin instalar nada en tu máquina:

1. Abrir Cloud Shell desde el portal o `https://shell.azure.com`.
2. Crear un Resource Group con tags de governance.
3. Crear un Storage Account `Standard_LRS` / `StorageV2`.
4. Subir y descargar un blob usando `--auth-mode login` (RBAC).
5. Filtrar recursos con queries **JMESPath**.
6. Consultar costes con `az consumption`.
7. Ejecutar smoke tests automatizados.
8. Limpiar el Resource Group entero.

> 🎯 Es **prerrequisito mental** del resto del curso: una vez te familiarizas
> con `az group`, `az resource`, `az storage` y JMESPath, los M02-M11 dejan de
> parecer magia.

## Mapeo a slides

| Concepto | Slide(s) | Dónde |
| --- | --- | --- |
| Pre-flight (cuenta Azure, navegador) | 3 | README → "Antes de empezar" |
| Abrir Cloud Shell + storage one-time | 4 | README → "Paso 1" |
| Verificar identidad (`az account show`) | 5 | parte de `_lib.sh` |
| Conceptos: Subscription, Location, RG | 6 | README → "Conceptos" |
| Crear RG con tags | 7 | [`scripts/01-provision-rg.sh`](scripts/01-provision-rg.sh) |
| Crear Storage Account | 8 | [`scripts/02-create-storage.sh`](scripts/02-create-storage.sh) |
| Container + blob upload/download + RBAC | 9 | [`scripts/03-upload-blob.sh`](scripts/03-upload-blob.sh) |
| JMESPath queries | 10 | [`scripts/04-jmespath-queries.sh`](scripts/04-jmespath-queries.sh) |
| Costes con `az consumption` | 11 | [`scripts/05-show-costs.sh`](scripts/05-show-costs.sh) |
| Smoke tests automatizados | 13 | [`scripts/06-smoke-tests.sh`](scripts/06-smoke-tests.sh) |
| Cleanup | 15 | [`scripts/07-cleanup.sh`](scripts/07-cleanup.sh) |
| Cloud Shell vs az CLI local | 16 | README → "Cloud Shell vs az CLI local" |
| Tips de productividad | 17 | README → "Tips pro" |
| Troubleshooting | 18 | README → "Troubleshooting" |
| Retos opcionales | 20 | [`scripts/extras/`](scripts/extras/) |

## Estructura

```
S1.P2-practica-cloud-shell/
├── README.md
├── .gitattributes
└── scripts/
    ├── .env.demo.example
    ├── .gitignore
    ├── _lib.sh                          carga .env.demo + helpers (step/ok/warn/confirm)
    ├── 01-provision-rg.sh               RG + 4 tags (proyecto, entorno, propietario, fecha)
    ├── 02-create-storage.sh             Standard_LRS + StorageV2
    ├── 03-upload-blob.sh                container + auto-asignación de "Storage Blob Data Contributor"
    ├── 04-jmespath-queries.sh           6 queries demostración + cheat-sheet impreso
    ├── 05-show-costs.sh                 mes actual + top servicios (degrada limpio sin permisos)
    ├── 06-smoke-tests.sh                5 checks + exit code para CI
    ├── 07-cleanup.sh                    az group delete --no-wait
    ├── demo.sh                          menú interactivo con todos los pasos
    └── extras/                          retos opcionales (slide 20)
        ├── reto-1-multiple-rgs.sh       3 RGs con tags distintos + filtro por tag
        ├── reto-2-markdown-report.sh    genera azure-report.md con tabla + total coste
        ├── reto-3-clone-repo.sh         git clone Azure-Samples/azure-cli-samples
        └── reto-4-sas-token.sh          user-delegation SAS de 1 h con --as-user
```

## Antes de empezar (slide 3)

Solo necesitas:

- ✅ **Cuenta Azure activa** (free trial vale: <https://azure.microsoft.com/free>)
- ✅ **Navegador moderno** (Chrome / Edge / Firefox / Safari)
- ✅ Acceso al portal en `https://portal.azure.com`

No necesitas instalar nada en local — **todo está dentro de Cloud Shell**:
`az` CLI, `dotnet`, `git`, `nano`, `vim`, `code` (VS Code en navegador).

## Conceptos antes de tocar comandos (slide 6)

```
Tenant (Microsoft Entra)
  └── Suscripción
       └── Resource Group "rg-cloudshell-<tu-nombre>"   (contenedor lógico, no cuesta)
            └── Storage Account                          (lo que pagas, ~0.02 €/mes vacío)
                 └── Container "pruebas"                 (carpeta lógica de blobs)
                      └── Blob "saludo.txt"              (un archivo)
```

Borrar el Resource Group elimina cascada todo lo que contiene. Es la operación
"nuclear" de cleanup — y la usamos en el paso 7.

## Práctica paso a paso por Cloud Shell (canónico)

> Si prefieres terminal local con scripts, salta a la siguiente sección.

### Paso 1 — Abrir Cloud Shell (slide 4)

1. Ir a `https://shell.azure.com` (o pulsar el icono `>_` en la barra superior
   de `https://portal.azure.com`).
2. **Primera vez**: Cloud Shell pide configurar un Storage para persistir tus
   archivos.
   - Subscription: la tuya
   - Region: `westeurope`
   - Click **Create storage** → ~30 s

Una vez dentro, prueba:

```bash
az --version          # >= 2.65
dotnet --version      # 8.x o 10.x
git --version
az account show -o table
```

### Paso 2 — Crear Resource Group con tags (slide 7)

```bash
RG=rg-cloudshell-<tu-nombre>
LOC=westeurope

az group create --name "$RG" --location "$LOC"

az group update --name "$RG" --tags \
  proyecto="curso-az204" \
  entorno="practica-cloud-shell" \
  propietario="<tu-nombre>" \
  fecha-creacion="$(date -u +%Y-%m-%d)"

az group show --name "$RG" --query "{name:name, tags:tags}" -o jsonc
```

### Paso 3 — Storage Account (slide 8)

```bash
# El nombre debe ser único globalmente, 3-24 chars, solo a-z y 0-9
STORAGE=stcloudshell$(date +%s | tail -c 6)
echo $STORAGE

az storage account create \
  --name "$STORAGE" \
  --resource-group "$RG" \
  --location "$LOC" \
  --sku Standard_LRS \
  --kind StorageV2

az storage account show -n "$STORAGE" -g "$RG" \
  --query "{name:name, sku:sku.name, kind:kind}" -o table
```

### Paso 4 — Container + blob (slide 9)

```bash
# RBAC: tu usuario necesita "Storage Blob Data Contributor"
USER_ID=$(az ad signed-in-user show --query id -o tsv)
STORAGE_ID=$(az storage account show -n "$STORAGE" -g "$RG" --query id -o tsv)
az role assignment create \
  --assignee "$USER_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "$STORAGE_ID"

# Esperar 30 s a que RBAC propague
sleep 30

# Container y blob
az storage container create --name pruebas --account-name "$STORAGE" --auth-mode login

echo "Hola desde Cloud Shell" > /tmp/saludo.txt
az storage blob upload \
  --account-name "$STORAGE" --container-name pruebas \
  --name saludo.txt --file /tmp/saludo.txt --auth-mode login

az storage blob list \
  --account-name "$STORAGE" --container-name pruebas \
  --auth-mode login -o table
```

### Paso 5 — JMESPath (slide 10)

```bash
# Lista de nombres
az resource list -g "$RG" --query "[].name" -o tsv

# Solo Storage Accounts
az resource list -g "$RG" \
  --query "[?type=='Microsoft.Storage/storageAccounts'].{name:name, loc:location}" \
  -o table

# Filtrar RGs por tag
az group list \
  --query "[?tags.proyecto=='curso-az204'].{name:name, owner:tags.propietario}" \
  -o table

# Recursos por tipo (count)
az resource list -g "$RG" --query "[].type" -o tsv | sort | uniq -c | sort -rn
```

JMESPath cheat-sheet básico:

| Patrón | Significado |
| --- | --- |
| `[]` | todos los elementos |
| `[0]` | primer elemento |
| `[].name` | proyección a una propiedad |
| `[?prop=='val']` | filtro por igualdad |
| `[?contains(name, 'x')]` | filtro por substring |
| `[].{X:propA, Y:propB}` | proyección custom (renombrar) |
| `length(@)` | contar elementos |
| `sort_by([], &name)` | ordenar |

Probador online: <https://jmespath.org/>.

### Paso 6 — Costes (slide 11)

```bash
START=$(date -u +%Y-%m-01)
END=$(date -u +%Y-%m-%d)

az consumption usage list \
  --start-date "$START" --end-date "$END" \
  --query "[].pretaxCost" -o tsv \
  | awk '{s+=$1} END {printf "Total mes: %.2f EUR\n", s}'
```

Nota: en algunas suscripciones de cliente esto requiere rol "Cost Management
Reader". Si falla, el portal en `Cost Management + Billing` siempre funciona.

### Paso 7 — Smoke tests (slide 13)

Validación rápida de que todo lo creado existe y está correcto. El script
`06-smoke-tests.sh` del repo lo automatiza con 5 checks: RG provisioned, ≥3
tags, storage existe, container `pruebas` existe, blob `saludo.txt` existe.

### Paso 8 — Cleanup (slide 15)

```bash
az group delete --name "$RG" --yes --no-wait
```

Borra cascada todo lo que contiene. El Storage de Cloud Shell
(`cloud-shell-storage-westeurope`) **no se borra** — es tu almacenamiento
persistente para futuras sesiones.

## Práctica alternativa con scripts (local o Cloud Shell)

Los scripts del repo automatizan los 8 pasos para que los puedas ejecutar
secuencialmente o desde el menú `demo.sh`:

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo con tu SUBSCRIPTION_ID, RG y STORAGE únicos

bash 01-provision-rg.sh        # RG + tags
bash 02-create-storage.sh      # Storage LRS V2
bash 03-upload-blob.sh         # container + RBAC + upload + download
bash 04-jmespath-queries.sh    # 6 queries sobre tus recursos + cheat-sheet
bash 05-show-costs.sh          # mes actual + top servicios
bash 06-smoke-tests.sh         # 5 checks
bash 07-cleanup.sh             # az group delete --no-wait

# Retos opcionales:
bash extras/reto-1-multiple-rgs.sh       # crear 3 RGs y filtrar por tag
bash extras/reto-2-markdown-report.sh    # genera azure-report.md
bash extras/reto-3-clone-repo.sh         # clona azure-cli-samples
bash extras/reto-4-sas-token.sh          # SAS user-delegation 1h
```

`bash demo.sh` para el menú interactivo con todos los pasos.

> Los scripts funcionan **igual en Cloud Shell que en bash local**. Para
> usarlos en Cloud Shell: pega el contenido de cada script o clónate el repo
> con `git clone`.

## Verificación final (slide 19)

Si terminaste, este checklist te confirma que cubriste todo:

| # | Verificación | OK |
| --- | --- | --- |
| 1 | `az account show` devuelve la suscripción correcta | ☐ |
| 2 | RG creado con tags (`az group show -n $RG --query tags`) | ☐ |
| 3 | Storage Account creado (Standard_LRS / StorageV2) | ☐ |
| 4 | Container `pruebas` existe | ☐ |
| 5 | Blob `saludo.txt` subido y se descarga correctamente | ☐ |
| 6 | Al menos 3 queries JMESPath ejecutadas | ☐ |
| 7 | `az consumption usage list` da output (o falla con permiso, lo cual también es info) | ☐ |
| 8 | `06-smoke-tests.sh` da 5/5 OK | ☐ |
| 9 | RG borrado al final (cleanup) | ☐ |
| 10 | Coste verificado < 0.10 € | ☐ |

## Cloud Shell vs `az` CLI local (slide 16)

| Aspecto | Cloud Shell | `az` CLI local |
| --- | --- | --- |
| Setup | cero | instalación + login |
| Latencia | media (round-trip a Azure) | baja |
| Editor | VS Code en navegador | VS Code nativo |
| Persistencia | `$HOME` en Storage de Cloud Shell | filesystem local |
| Multi-tab | limitado | sin límite |
| Coste | ~0.10 €/mes (storage) | 0 |

**Recomendación práctica del curso**: aprende Cloud Shell **primero** (sin
barrera de entrada para nadie), luego instala `az` CLI en local para tu día a
día. Mantén Cloud Shell como "plan B" cuando estés en una máquina que no es la
tuya.

## Tips pro de productividad (slide 17)

```bash
# Aliases utiles en Cloud Shell — añade a ~/.bashrc
echo "alias rgs='az group list -o table'" >> ~/.bashrc
echo "alias mystorage='az storage account list -o table'" >> ~/.bashrc
source ~/.bashrc

# Output por defecto en tabla (mas legible)
az config set core.output=table

# Función rápida para cambiar de subscription
sub() {
  if [ -z "$1" ]; then
    az account list --query "[].{Name:name, Id:id}" -o table
  else
    az account set --subscription "$1"
  fi
}
```

## Troubleshooting (slide 18)

| Síntoma | Causa típica | Fix |
| --- | --- | --- |
| Cloud Shell se desconecta tras 20 min | Inactividad (comportamiento normal) | F5 / Reload — los archivos en `$HOME` persisten |
| Comandos `az` lentos (5-10 s) | Cloud Shell vive en una región concreta; round-trip alto si estás lejos | Aceptarlo o cambiar región del Cloud Shell |
| `Sin storage configurado` | Nunca configuraste el storage del Cloud Shell | Reload → diálogo "Mount storage" → Create |
| `AuthorizationFailed` al crear RG | Tu cuenta no es Contributor en la suscripción | Pedir Contributor al admin o usar tu free trial |
| `AuthorizationPermissionMismatch` al subir blob | Falta rol "Storage Blob Data Contributor" en el storage | El script `03-upload-blob.sh` lo asigna automáticamente; manualmente: `az role assignment create --assignee <user-id> --role "Storage Blob Data Contributor" --scope <storage-id>` |
| Nombre del Storage rechazado | Debe ser único globalmente, 3-24 chars, solo `a-z` y `0-9` | Añadir sufijo numérico: `stcloudshellpedro42` |
| `az consumption usage list` devuelve vacío o error | Suscripción de cliente sin Cost Management Reader | Cost Management en Portal funciona igual |

## Retos opcionales (slide 20)

Cada `extras/reto-*.sh` está implementado y listo para ejecutar:

- **Reto 1** — crea 3 RGs (`dev`, `qa`, `prod`) con tags distintos, filtra por
  tag y limpia. Útil para entender governance por etiqueta.
- **Reto 2** — genera `azure-report.md` con tabla de RGs, recursos del RG
  actual y total de coste. El output es Markdown válido (puedes pegarlo en
  Confluence/Notion).
- **Reto 3** — clona `Azure-Samples/azure-cli-samples` y muestra los
  ejemplos relacionados con App Service. Demuestra que puedes hacer Git
  desde dentro de Cloud Shell sin tener Git en local.
- **Reto 4** — genera una SAS user-delegation (`--as-user`) válida durante 1
  hora sobre el blob `saludo.txt`. Imprime una URL `curl`-eable. Patrón típico
  para compartir archivos con un cliente externo de forma temporal.

## Hand-off al siguiente paso

Esta práctica es **prerequisito mental** del resto del curso. Con ella ya
sabes:

- Crear y borrar **Resource Groups** con tags.
- Crear **Storage Accounts** y manipular blobs por **RBAC** (`--auth-mode login`).
- Filtrar y formatear output con **JMESPath**.
- Consultar **costes** desde CLI.
- Hacer **cleanup** disciplinado (la operación que más se olvida).

[`M01-S1.P — Hello World end-to-end`](../S1.P-practica-helloworld/README.md)
es la siguiente práctica natural: añade una API .NET, una Web App y un deploy
real a Azure App Service usando los mismos comandos `az` que has visto aquí.
