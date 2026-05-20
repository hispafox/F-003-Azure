# Manual del alumno — S1.P2 · Explorar Azure desde Cloud Shell

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica de la práctica: scripts paso a paso, mapeo a slides, comandos CLI completos. Este manual va antes: te cuenta por qué esta práctica es prerrequisito mental del curso entero y qué llevarte aunque luego acabes prefiriendo `az` en local.

Tiempo de lectura: ~15 min. Práctica de referencia: [M01-S1.P2](../../../doc/M01-Intro-Azure/v5-actual/M01-S1.P2-practica-cloud-shell-v1.md). Es la **única práctica del curso sin código .NET**: puramente Azure CLI desde el navegador, con `JMESPath` para filtrar y `az consumption` para mirar la factura. Cuarenta y cinco minutos, cero euros, máquina prestada.

*Creado: 2026-05-20 08:30 +0200*

---

## 1. La idea en una frase

Hay días en que te toca tocar Azure desde un portátil que no es el tuyo. Sin tu setup, sin tu CLI instalado, sin tus aliases. Cloud Shell es la respuesta de Microsoft a ese escenario: un terminal completo con `az`, `dotnet`, `git` y un editor —incluyendo VS Code en el navegador— montado en un Storage que persiste entre sesiones. Lo abres, te identifica con la sesión del portal y ya estás dentro. Cero instalación.

Esa idea —tener todas las herramientas en cualquier ordenador con navegador— es el lado humano de la nube. Y antes de poder discutir si te conviene `az` en local o Cloud Shell para tu día a día, conviene haber visto el segundo funcionando una vez. Esa es la práctica.

---

## 2. El problema real que hay detrás

Imagina dos escenarios reales que pasan más de lo que parece.

El primero: estás en casa del cliente para una sesión de revisión y necesitas mirar un recurso en Azure. Tu portátil se ha quedado en la oficina. Te sientas delante de un ordenador del cliente: tiene Chrome y poco más. ¿Cómo entras a Azure a hacer algo útil? Sin instalar nada, sin pedir permisos de admin, sin descargar herramientas que luego haya que desinstalar.

El segundo: empiezas en un curso o un proyecto nuevo, y tu trainer dice "vamos a hacer una cosa rápida en Azure". Tienes que decidir si la mejor inversión de tiempo es instalar `az` CLI, configurar Python, dar permisos al PATH, hacer login… o si hay un atajo. El atajo se llama Cloud Shell. Para esa primera vez, no hay nada más rápido.

Y luego hay una tercera razón, menos comentada y más útil: **Cloud Shell tiene tu Azure CLI con la última versión, siempre**. Si tu CLI local lleva tres meses sin actualizar, te puedes saltar comandos nuevos sin saberlo. La versión de Cloud Shell la mantiene Microsoft. Para probar algo que sospechas que es bug de tu versión vs feature nueva, Cloud Shell es la respuesta.

Estas tres situaciones —máquina prestada, primera vez, versión actualizada— son exactamente las que esta práctica entrena.

| Necesidad | Cómo se resuelve en la práctica |
| --- | --- |
| Empezar sin instalar nada | Abrir `https://shell.azure.com` y configurar Storage al primer arranque (~30 s) |
| Crear y listar recursos | `az group`, `az resource`, `az storage` |
| Acceder a un blob sin manejar AccountKey | `--auth-mode login` (RBAC con tu usuario) |
| Filtrar la salida JSON sin abrir un editor | JMESPath (`--query`) |
| Mirar cuánto va costando este mes | `az consumption usage list` |
| Limpiar sin dejar nada colgando | `az group delete --no-wait` |

---

## 3. Por qué esto importa en tu stack

El resto del curso usa scripts `az` como complemento de los pasos por Portal. Si nunca has tocado el CLI, esos scripts parecen una pared. Si has pasado una hora de tu vida con Cloud Shell, los entiendes a la primera. La práctica está aquí, al principio del módulo M01, por esa razón pedagógica: bajar la barrera de entrada al CLI antes de que aparezca de verdad en M02 y siguientes.

Y la decisión deliberada respecto a S1.P: aquí no hay .NET, no hay tests `dotnet test`, no hay `Program.cs`. La validación se hace con un script `06-smoke-tests.sh` que verifica cinco hechos sobre la infraestructura creada. Es la única práctica del curso con esa forma; el resto siempre tiene un proyecto .NET con su `dotnet test`. Lo mencionamos para que no busques lo que no hay.

---

## 4. El modelo mental: el portátil prestado

Imagina que llegas a casa de un amigo y necesitas trabajar dos horas. No has llevado tu portátil. El amigo te presta el suyo: Windows recién instalado, sin tu Visual Studio, sin tu CLI, sin nada. Y aún así, en cinco minutos estás escribiendo emails, accediendo a tus servicios cloud, editando documentos. ¿Cómo? Porque las herramientas que necesitas viven en el navegador.

Cloud Shell aplica esa misma idea al trabajo con Azure. Un terminal completo, un editor (incluso VS Code), `az`, `dotnet`, `git`, todo, dentro del navegador. Lo abres, te identifica con la sesión del portal y entras a tu cuenta. Los archivos que crees viven en un Storage Account (creado la primera vez, ~0,10 €/mes) y persisten entre sesiones. Si cierras el navegador y vuelves mañana, tu `~/.bashrc` y los scripts que hayas guardado siguen ahí.

```
Tenant (Microsoft Entra)
  └── Suscripción
       └── Resource Group "rg-cloudshell-<tu-nombre>"   (contenedor lógico, no cuesta)
            └── Storage Account                          (~0,02 €/mes vacío)
                 └── Container "pruebas"                 (carpeta lógica de blobs)
                      └── Blob "saludo.txt"              (un archivo)
```

Tres frases para fijar la imagen:

- **Cloud Shell vive en una región concreta.** Si estás en Madrid y la Cloud Shell está en West Europe (Países Bajos), los comandos se ejecutan ahí: hay un round-trip de red en cada `az`. Es invisible para comandos rápidos, notable para listados grandes. Aceptarlo (es la región más razonable) o cambiarla en *Settings*.
- **El Storage de Cloud Shell es tuyo y persiste.** Cuesta unos céntimos al mes (Standard LRS con muy pocos KB de tus archivos). No lo borres por accidente — si lo haces, pierdes tu `~` y la próxima vez que abras Cloud Shell te pide configurar uno nuevo.
- **Cloud Shell se desconecta tras 20 min sin actividad.** Es comportamiento normal. Un F5 / reload reconecta y tus archivos siguen. Si estás en medio de un script largo y te desconectas, lo pierdes — buena razón para tenerlos como ficheros (`bash script.sh`) en lugar de pegarlos línea a línea.

---

## 5. Las cuatro herramientas que ganas

### 5.1 Resource Group con tags de gobernanza

```bash
az group create --name "$RG" --location westeurope

az group update --name "$RG" --tags \
  proyecto="curso-az204" \
  entorno="practica-cloud-shell" \
  propietario="<tu-nombre>" \
  fecha-creacion="$(date -u +%Y-%m-%d)"
```

Crear el RG no cuesta nada — es solo un contenedor. Lo importante son las **tags**. Cuatro etiquetas: `proyecto`, `entorno`, `propietario`, `fecha-creacion`. Cuando tu organización tenga cientos de Resource Groups, las tags son cómo encuentras "todo lo que es del proyecto X" o "todo lo que pertenece a Pedro". En la práctica las usas más tarde con JMESPath para filtrar.

> 🧠 **Tags como contrato organizativo.** En proyectos reales, una de las primeras decisiones de gobernanza es "qué tags son obligatorias en cada recurso". Las que aparecen aquí —proyecto, entorno, propietario, fecha-creación— son un mínimo razonable. Sin ellas, en un año tienes recursos huérfanos sin saber de quién son y nadie los borra "por si acaso". Empezar con disciplina aquí te ahorra esos huérfanos.

### 5.2 Storage Account + blob por RBAC

```bash
STORAGE=stcloudshell$(date +%s | tail -c 6)
az storage account create --name "$STORAGE" --resource-group "$RG" \
  --sku Standard_LRS --kind StorageV2

# El usuario que está logueado necesita el rol de datos
USER_ID=$(az ad signed-in-user show --query id -o tsv)
STORAGE_ID=$(az storage account show -n "$STORAGE" -g "$RG" --query id -o tsv)
az role assignment create \
  --assignee "$USER_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "$STORAGE_ID"

# Esperar a que RBAC propague (30 s típico)
sleep 30

# Container y blob — todos los comandos con --auth-mode login
az storage container create --name pruebas --account-name "$STORAGE" --auth-mode login
echo "Hola desde Cloud Shell" > /tmp/saludo.txt
az storage blob upload --account-name "$STORAGE" --container-name pruebas \
  --name saludo.txt --file /tmp/saludo.txt --auth-mode login
```

Lo importante de este flujo es `--auth-mode login`. Significa: "no uses la AccountKey, usa mi identidad". Tu usuario tiene asignado el rol *Storage Blob Data Contributor* sobre el Storage, y el CLI le pide el token a Entra ID en tu nombre. **Cero AccountKey en tu terminal, en variables, en ningún sitio.**

Eso es **exactamente el patrón de Managed Identity** que verás en M05-S5.4. Lo que cambia en producción es quién pone el `assignee`: en tu portátil eres tú con tu `az login`; en una App Service, es la identidad de la app. La mecánica RBAC es idéntica. Por eso esta práctica importa: aprendes el patrón en su forma más simple antes de que aparezca en producción real.

> ⚠️ **El `sleep 30` no es decoración.** Cuando asignas un rol RBAC, Azure tarda entre 5 y 30 segundos en propagar la asignación a todos los componentes del servicio. Si intentas usar el rol inmediatamente, recibirás `AuthorizationPermissionMismatch`. El primer instinto es pensar "el comando está mal"; la realidad es "Azure aún no se ha enterado". El `sleep 30` es la salida pragmática en scripts. En interactivo, si te pasa, espera medio minuto y reintenta.

### 5.3 JMESPath: filtrar JSON sin abrir un editor

Todos los comandos `az` devuelven JSON enorme. Mirar 200 líneas a ojo para encontrar un campo es la receta del aburrimiento. JMESPath es el lenguaje de filtrado del CLI:

```bash
# Solo nombres
az resource list -g "$RG" --query "[].name" -o tsv

# Filtrar por tipo + proyección custom
az resource list -g "$RG" \
  --query "[?type=='Microsoft.Storage/storageAccounts'].{name:name, loc:location}" \
  -o table

# Filtrar RGs por tag
az group list \
  --query "[?tags.proyecto=='curso-az204'].{name:name, owner:tags.propietario}" \
  -o table
```

Los cuatro patrones que cubren el 80% de los casos:

| Patrón | Para qué |
| --- | --- |
| `[].propA` | proyectar una propiedad |
| `[?prop=='val']` | filtrar por igualdad |
| `[?contains(name, 'x')]` | filtrar por substring |
| `[].{X:propA, Y:propB}` | proyección custom (renombrar campos) |

> 🧠 **El probador interactivo es <https://jmespath.org>.** Pega tu JSON, escribe la query, ves el resultado. Cuando llevas tres meses con `az` y todavía dudas con una query compleja, esa página vale más que cualquier documentación. Y los conceptos son los mismos que `jq` para JSON o XPath para XML — un día que aprendes, se queda.

### 5.4 Costes en tiempo real

```bash
START=$(date -u +%Y-%m-01)
END=$(date -u +%Y-%m-%d)

az consumption usage list \
  --start-date "$START" --end-date "$END" \
  --query "[].pretaxCost" -o tsv \
  | awk '{s+=$1} END {printf "Total mes: %.2f EUR\n", s}'
```

Esto te da el coste del mes en curso, en una sola línea. Es una herramienta de higiene: cada cierto tiempo —semanalmente está bien— miras cuánto vas gastando. En proyectos personales, te avisa de la cuenta que se quedó sin borrar. En proyectos corporativos, te mete el hábito de pensar en coste mientras tocas Azure.

Nota: en algunas suscripciones corporativas, `az consumption` requiere el rol *Cost Management Reader*. Si tu suscripción no te deja, el portal en *Cost Management + Billing* siempre funciona y enseña lo mismo con gráficos.

---

## 6. Recorrido guiado: tu primer Storage en cinco minutos

Abre Cloud Shell (`https://shell.azure.com`) y sigue estos pasos. El [`README.md`](README.md) tiene cada paso con su comando exacto; aquí está el guion conceptual.

| # | Paso | Qué demuestra |
| --- | --- | --- |
| 1 | Abrir Cloud Shell · `az --version` · `az account show -o table` | El terminal funciona, estás logueado en la suscripción correcta. |
| 2 | Crear RG con cuatro tags | El contenedor lógico con la disciplina de gobernanza. |
| 3 | Crear Storage Account `Standard_LRS` / `StorageV2` | Storage real, ~0,02 €/mes vacío. El "Hola Azure" del almacenamiento. |
| 4 | Asignar rol *Storage Blob Data Contributor* a tu usuario · esperar 30 s · crear container · subir blob (`--auth-mode login`) | RBAC en acción: cero AccountKey en tu terminal. Lo verás de nuevo en M05-S5.4. |
| 5 | Listar y filtrar recursos con JMESPath (4-6 queries) | Filtrar JSON desde el CLI sin abrir editor. La herramienta que más vas a usar. |
| 6 | `az consumption usage list` con `awk` para totalizar | Cuánto vas gastando este mes. Higiene financiera. |
| 7 | `06-smoke-tests.sh` (5 checks automáticos) | Validación rápida: RG, tags, storage, container, blob. Devuelve exit code para CI. |
| 8 | `az group delete --name "$RG" --yes --no-wait` | Cleanup nuclear. Treinta segundos. Cero residuo. |

Un experimento que aporta más que cualquier explicación: ejecuta el paso 2 con `--tags proyecto=otra-cosa` en lugar del valor por defecto, y luego prueba el filtro de JMESPath del paso 5 buscando `proyecto=='curso-az204'`. No aparece. Cambia la tag al valor correcto y aparece. Esos quince segundos hacen tangible para qué sirven las tags y por qué importa la disciplina al ponerlas.

Y otro experimento, más sutil: prueba el paso 4 **sin** el `sleep 30`. Es bastante probable que recibas `AuthorizationPermissionMismatch` en el primer intento. Espera medio minuto y reintenta. Ya no falla. La propagación RBAC se siente en los huesos cuando la sufres una vez. En scripts profesionales, ese `sleep 30` es la salida pragmática; en código de aplicación serio, se reintenta con backoff.

---

## 7. Cloud Shell vs `az` CLI local: cuándo cada uno

| Aspecto | Cloud Shell | `az` CLI local |
| --- | --- | --- |
| Setup | cero | instalación + login (10-30 min la primera vez) |
| Latencia | media (round-trip a la región de Cloud Shell) | baja |
| Editor | VS Code en navegador | VS Code nativo, con tus extensiones |
| Persistencia | `$HOME` en el Storage de Cloud Shell | tu filesystem local |
| Multi-tab | limitado (la sesión tiene quotas) | sin límite |
| Coste | ~0,10 €/mes por el Storage | 0 |
| Versión `az` | siempre la última, mantenida por Microsoft | la que tú actualices |

**Recomendación práctica del curso**: aprende Cloud Shell **primero** porque no tiene barrera de entrada para nadie. Después instala `az` en local para tu día a día —es más rápido, integra con tu editor, no depende del navegador—. Y deja Cloud Shell como plan B: para máquinas prestadas, para probar comandos en la última versión del CLI, para sesiones cortas donde no quieres tocar tu setup local. Las dos opciones son válidas y se complementan; no es elegir una para siempre.

---

## 8. Puesta en marcha

### 8.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| Cuenta Azure activa | crear recursos, consultar costes | Sí |
| Navegador moderno (Chrome / Edge / Firefox / Safari) | abrir Cloud Shell | Sí |
| Acceso a `https://portal.azure.com` y `https://shell.azure.com` | la práctica entera | Sí |
| Rol Contributor en la suscripción | crear RG, Storage y asignación de rol RBAC | Sí |

No instalas nada en local. Si vas a usar los scripts del repo en local en lugar de pegarlos en Cloud Shell, necesitas `bash` y `az` CLI ≥ 2.65 — pero la práctica canónica es por Cloud Shell.

### 8.2 La práctica desde Cloud Shell (canónico)

Los pasos completos con sus comandos están en el [`README.md`](README.md), sección *Práctica paso a paso por Cloud Shell*. Aquí solo el resumen: abre `https://shell.azure.com`, configura el Storage la primera vez (~30 s), y sigue del paso 1 al 8 del recorrido de la sección 6.

### 8.3 La práctica con scripts (local o Cloud Shell)

Si prefieres scripts en lugar de comandos sueltos, los del repo automatizan los ocho pasos:

```bash
cd scripts
cp .env.demo.example .env.demo       # editar SUBSCRIPTION_ID, RG, STORAGE

bash 01-provision-rg.sh        # RG + tags
bash 02-create-storage.sh      # Storage LRS V2
bash 03-upload-blob.sh         # container + RBAC + upload + download
bash 04-jmespath-queries.sh    # 6 queries demostración + cheat-sheet
bash 05-show-costs.sh          # mes actual + top servicios
bash 06-smoke-tests.sh         # 5 checks de validación
bash 07-cleanup.sh             # az group delete --no-wait

# Retos opcionales:
bash extras/reto-1-multiple-rgs.sh       # 3 RGs con tags distintos
bash extras/reto-2-markdown-report.sh    # genera azure-report.md
bash extras/reto-3-clone-repo.sh         # git clone azure-cli-samples
bash extras/reto-4-sas-token.sh          # SAS user-delegation 1h
```

`bash demo.sh` te da un menú interactivo con todos los pasos.

### 8.4 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| Cloud Shell se desconecta tras 20 min | inactividad (comportamiento normal) | F5 / Reload — los archivos en `$HOME` persisten |
| Comandos `az` lentos (5-10 s) | round-trip a la región de Cloud Shell | aceptarlo o cambiar región en *Settings* |
| `Sin storage configurado` | nunca configuraste el Storage de Cloud Shell | Reload → diálogo *Mount storage* → Create |
| `AuthorizationFailed` al crear RG | tu cuenta no es Contributor | pide Contributor al admin o usa tu free trial |
| `AuthorizationPermissionMismatch` al subir blob | rol RBAC sin propagar todavía | espera 30 s y reintenta; en scripts, `sleep 30` ya lo hace |
| Nombre del Storage rechazado | tiene que ser único globalmente, 3-24 chars, `a-z` y `0-9` | añade sufijo numérico: `stcloudshellpedro42` |
| `az consumption usage list` vacío | suscripción corporativa sin *Cost Management Reader* | usa *Cost Management* en el portal — el mismo dato con gráficos |

### 8.5 Los cuatro retos opcionales

Cada `extras/reto-*.sh` está implementado:

- **Reto 1** — crea tres RGs (`dev`, `qa`, `prod`) con tags distintos y filtra por tag. Hace tangible el concepto de gobernanza por etiqueta.
- **Reto 2** — genera `azure-report.md` con tabla de RGs, recursos del RG actual y total de coste. El output es Markdown válido para pegar en Confluence/Notion.
- **Reto 3** — clona `Azure-Samples/azure-cli-samples` y muestra los ejemplos relacionados con App Service. Demuestra que puedes hacer `git` desde dentro de Cloud Shell sin tener Git en local.
- **Reto 4** — genera una **SAS user-delegation** (`--as-user`) válida durante una hora sobre el blob `saludo.txt`. Imprime una URL `curl`-eable. Patrón típico para compartir archivos con un cliente externo de forma temporal y segura.

Los cuatro son cinco minutos cada uno y consolidan conceptos que se repetirán en el curso.

---

## 9. Ideas para llevarte

Lo más útil de esta práctica no es Cloud Shell en sí — es que se te quede grabado el **modelo CLI de Azure**. Tres patrones de comando se repiten en absolutamente todo: `az <servicio> <accion>` para crear/listar/borrar, `--query "..."` para filtrar, `--output table` (o tu favorito) para presentar. Una vez interiorizas esos tres, navegar Azure desde el terminal es cuestión de buscar el comando correcto en `az <servicio> --help`. Y el `--help` de `az` es uno de los mejores que existen.

La segunda lección que conviene fijar es **RBAC con `--auth-mode login`** y la propagación lenta. La primera vez que te encuentras el `AuthorizationPermissionMismatch` justo después de asignar un rol parece un bug. No lo es. Es la propagación. Esperar 30 segundos lo arregla. Esta cabezonería de Azure se repite en muchos sitios — cualquier asignación de rol o cambio de identidad lleva su tiempo. Saberlo de antemano te ahorra abrir tickets de soporte por nada.

Y una recomendación pragmática: **mete `--output table` por defecto** (`az config set core.output=table`). El JSON crudo de `az` es difícil de leer en pantalla; la tabla es mucho más amable. Para queries complejas siempre puedes pasar a `jsonc` o `tsv`, pero el default tabla te quita ruido en el 90% de los casos.

---

## 10. Comprueba que lo has entendido

1. ¿Para qué sirve Cloud Shell exactamente y qué tres situaciones reales lo justifican? *(sección 2)*
2. Subes un blob con `--auth-mode login` y recibes `AuthorizationPermissionMismatch`. ¿Qué pasó y cómo lo arreglas? *(sección 5.2)*
3. ¿Qué cuatro tags pone la práctica al RG y para qué sirven en proyectos reales? *(sección 5.1)*
4. ¿Cuál es la diferencia entre `--auth-mode login` y usar la AccountKey en `az storage blob upload`? ¿Por qué se prefiere `login`? *(sección 5.2)*
5. ¿Cuándo elegirías Cloud Shell sobre `az` CLI local y cuándo al revés? *(sección 7)*
6. La query `az group list --query "[?tags.proyecto=='curso-az204'].name"`. ¿Qué hace, qué patrón JMESPath usa y dónde lo probarías sin tocar Azure? *(sección 5.3)*

<details>
<summary>Respuestas</summary>

1. Es un terminal completo (`az`, `dotnet`, `git`, editor) que vive en el navegador y persiste tu `$HOME` entre sesiones. Las tres situaciones que lo justifican: **máquina prestada** (no llevaste tu portátil), **primera vez con Azure** (cero barrera de entrada, no instalas nada), **versión `az` siempre actualizada** (Microsoft mantiene la última; útil cuando dudas si lo que ves es un bug de tu versión local). Es la respuesta cuando "instalar `az` local" no es una opción razonable.
2. Acabas de asignar un rol RBAC y todavía no ha propagado. Azure tarda entre cinco y treinta segundos en hacer efectiva una asignación. Lo arreglas esperando medio minuto y reintentando. En scripts, un `sleep 30` después de `az role assignment create` es la salida pragmática. En aplicaciones serias, se reintenta con backoff exponencial.
3. `proyecto`, `entorno`, `propietario`, `fecha-creacion`. Sirven para gobernanza: filtrar recursos por proyecto cuando hay cientos, asignar costes por área, identificar al responsable de un recurso "huérfano", auditar cuándo se creó algo. Son el mínimo razonable; muchas organizaciones añaden `criticidad`, `dataclassification`, `costcenter`. Sin tags consistentes, en un año tienes recursos que nadie sabe de quién son.
4. `--auth-mode login` usa el token de Entra ID de tu sesión `az login` (RBAC: tu usuario tiene rol *Storage Blob Data Contributor*). La AccountKey usa la clave maestra del Storage Account — la misma que da acceso total a cualquiera que la tenga. Se prefiere `login` porque la identidad es la tuya (auditable, revocable, sin secretos que filtrar). La AccountKey es la "llave maestra" y debería estar reservada para situaciones donde Managed Identity no funciona (y son cada vez menos). Es el patrón de M05-S5.4.
5. Cloud Shell cuando: máquina prestada, primera vez, sesión corta, quieres la última versión `az`. CLI local cuando: tu portátil habitual, sesiones largas, integración con tu editor, múltiples tabs, latencia baja. No es una elección excluyente — los profesionales suelen tener `az` local instalado **y** usan Cloud Shell como plan B. La práctica te entrena el segundo, asumiendo que aún no tienes el primero instalado.
6. Lista los Resource Groups cuya tag `proyecto` tenga el valor `curso-az204` y proyecta solo el campo `name`. El patrón JMESPath es **filtro por propiedad anidada**: `[?tags.proyecto=='valor'].campo`. Lo pruebas sin tocar Azure copiando un JSON de ejemplo (lo puedes generar con `az group list -o json`) en <https://jmespath.org/> y escribiendo la query en el campo correspondiente. Esa página es la mejor herramienta para iterar queries complejas antes de aplicarlas en `az --query`.

</details>

---

## 11. Hasta aquí

S1.P2 cierra el módulo M01. Con las dos prácticas juntas tienes el cinturón mínimo del curso: por un lado, una API .NET desplegada a un App Service real con su URL pública (S1.P); por otro, control completo de los recursos en Azure desde el navegador, sin instalar nada (S1.P2). Las dos son **prerrequisito mental** del resto: cuando en M02 aparezcan scripts con `az appservice`, `az webapp`, `az role assignment`, no son cajas negras — son comandos que ya has visto, en su forma más simple, aquí.

A partir de M02 cambia el ritmo. Empiezan a aparecer slots de despliegue, swap entre staging y producción, scaling, deployment slots con sticky settings, y la práctica reutilizará el RG que dejaste en S1.P. La curva sube, pero ya tienes la base.
