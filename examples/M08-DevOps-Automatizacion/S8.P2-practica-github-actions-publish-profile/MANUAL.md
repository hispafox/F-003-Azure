# Manual del alumno — S8.P2 · Práctica GitHub Actions + publish profile

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta por qué la versión "rápida" de un pipeline tiene su sitio (side-projects, MVP, aprendizaje), qué es exactamente un publish profile y cómo se parsea, y cuándo migrar a OIDC.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M08-S8.P2](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md). Tres piezas de lógica pura (parser del XML publish profile con detección de placeholders, generador de workflow GitHub Actions con knobs, recomendador Publish Profile vs OIDC vs Environment).

*Creado: 2026-05-21 01:15 +0200*

---

## 1. La idea en una frase

S8.P (la práctica anterior) monta el pipeline "profesional" en Azure DevOps con OIDC — setup de 30-60 minutos, robusto, sin secretos. **Esta práctica monta lo mismo en GitHub Actions con publish profile** — setup de 5 minutos, ideal para side-projects, MVPs, aprender CI/CD, repos personales o forks donde no controlas Entra ID. La pregunta no es "cuál es mejor": es **cuál encaja con tu contexto hoy**. Y el ejemplo te da el recomendador con tres salidas posibles (PublishProfile / Oidc / EnvironmentSecret) según señales objetivas.

Cierra M08 (8/8) y prepara la transición al M09 (IA como herramienta de desarrollo).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican que esta práctica exista junto a S8.P:

**Caso 1 — el side-project que se atascó en setup.** Un developer quería montar CI/CD para su proyecto personal (una web pequeña, un MVP, una herramienta interna). Empezó con la "configuración profesional": OIDC, Federated Credentials, App Registration, role assignments. **Una hora después, sigue peleando con permisos** en Entra ID que no tiene porque no es el admin del tenant. Frustrado, abandona. La versión "publish profile" del slide 7-8 le habría llevado **5 minutos**: descargar XML, crear secret, push. Hubiera tenido CI/CD funcionando antes de la merienda.

**Caso 2 — el repo personal que no es de la empresa.** Otro caso: un developer mantiene un fork público de una herramienta open source. Quiere CI/CD para su fork. **No controla Entra ID corporativo** (el tenant pertenece a otra empresa que ni siquiera está relacionada). OIDC no es viable. Publish profile sí: la Web App F1 puede vivir en su propia suscripción de pruebas, con su propio publish profile, sin tocar Entra. La práctica se hace en 30 minutos.

**Caso 3 — el publish profile con password rotada.** Un equipo migró un proyecto antiguo a publish profile. **El secret de GitHub se quedó obsoleto** cuando alguien rotó las credenciales del App Service. El workflow llevaba semanas fallando silenciosamente. La regla operativa: **rotar publish profile cada 90 días** y refrescar el secret de GitHub. El parser del ejemplo detecta cuando el password está vacío o es un placeholder; útil para detectar el caso antes del deploy.

Los tres casos los enseña el ejemplo: el recomendador clasifica el contexto, el parser caza placeholders, y el generator monta el workflow correcto.

---

## 3. Por qué esto importa en tu stack

Si vas a montar CI/CD en cualquier contexto, las tres preguntas clave:

- **¿Qué método de auth me conviene?** No es "OIDC porque es lo moderno"; es "en mi contexto, con mis permisos, con la criticidad del proyecto, ¿qué encaja?". El recomendador te da una respuesta defendible.
- **¿Cómo detectar publish profile roto antes del deploy?** El parser identifica password vacío, placeholders, falta de perfil MSDeploy. Cinco segundos de validación local que evitan un fallo a las 3 de la madrugada en producción.
- **¿Cómo crece el workflow cuando madura el proyecto?** Empiezas con un job; cuando necesitas tests, dos jobs con `needs`; cuando necesitas deploy controlado, environment con reviewers. El builder modela los tres niveles.

---

## 4. La analogía vertebradora: dos llaves de la casa

Imagina que necesitas dar acceso a tu casa a una persona de servicio. Tienes dos opciones:

**Opción 1 — La llave física** (publish profile): copias una llave estándar y se la das. La persona puede entrar cuando quiera. Si pierdes la llave o la robas, **alguien puede usarla durante meses** hasta que cambies la cerradura. Es **sencillo, rápido, conocido**. Lo usas para el portero del edificio que viene una vez al mes a revisar el portal, o para la chica que viene tres veces a la semana a limpiar — **gente conocida con bajo riesgo**.

**Opción 2 — El sistema de tarjetas inteligentes** (OIDC): la persona se identifica con su DNI cada vez que viene; un sistema digital genera una tarjeta temporal de 1 hora; pasa, hace su trabajo, la tarjeta caduca sola. **No hay llave física que pueda perderse**. Es más complejo de configurar (necesitas el sistema instalado, la persona dada de alta) pero **más seguro** para casos críticos: el técnico de la calefacción que viene una vez al año pero accede a la sala de máquinas; el inspector de hacienda que tiene que entrar al despacho con documentos.

Y luego hay una **opción intermedia**: la llave física pero **el portero también vigila** (Environment Secret con reviewers). El visitante usa la llave, pero el portero comprueba el DNI antes de dejarle subir. Útil cuando tienes que dar acceso al técnico pero quieres saber cuándo entra.

La pregunta en el caso de tu pipeline:

- **Side-project, aprendizaje, MVP**: llave física (Publish Profile). Setup de 5 minutos. Si se pierde, rotas y arreglado.
- **Producción crítica, equipo grande, auditoría obligatoria**: sistema de tarjetas (OIDC). Setup de 30-60 minutos. Cero riesgo de filtración.
- **Producción no crítica sin Entra controlado**: llave física + portero (Publish Profile + Environment con reviewers). Mejor que Profile solo; menos cómodo que OIDC.

Las tres opciones son legítimas según contexto. Mantén la imagen mientras lees el código.

---

## 5. Recorrido por el código

### `PublishProfileParser.Parsear` — el XML que descargas del Portal

Cuando vas a Portal → Web App → **Get publish profile**, descargas un XML como este:

```xml
<publishData>
  <publishProfile profileName="miapp - Web Deploy"
                  publishMethod="MSDeploy"
                  publishUrl="miapp.scm.azurewebsites.net:443"
                  msdeploySite="miapp"
                  userName="$miapp"
                  userPWD="ABcdEF1234..."
                  destinationAppUrl="http://miapp.azurewebsites.net"
                  controlPanelLink="https://portal.azure.com" />
  <publishProfile profileName="miapp - FTP"
                  publishMethod="FTP"
                  publishUrl="ftps://waws-prod-...ftp.azurewebsites.windows.net/site/wwwroot"
                  ftpPassiveMode="True"
                  userName="miapp\$miapp"
                  userPWD="ABcdEF1234..."
                  destinationAppUrl="http://miapp.azurewebsites.net" />
  <publishProfile profileName="miapp - Zip Deploy"
                  publishMethod="ZipDeploy"
                  publishUrl="miapp.scm.azurewebsites.net:443"
                  userName="$miapp"
                  userPWD="ABcdEF1234..."
                  destinationAppUrl="http://miapp.azurewebsites.net" />
</publishData>
```

El parser extrae cada `<publishProfile>` y lo clasifica:

```csharp
var metodo = metodoRaw switch
{
    "MSDeploy" => MSDeploy,
    "FTP" => Ftp,
    "ZipDeploy" => Zip,
    _ => Otro,
};

bool passwordPresente = !string.IsNullOrWhiteSpace(password)
    && !PareceUnPlaceholder(password);
```

Y detecta dos anti-patterns:

1. **Password ausente o placeholder**: si el `userPWD` está vacío o contiene `changeme`, `xxxxxxxx`, `password-larguísima` o `...`, marca `PasswordPresente = false`. Razón: alguien copió el XML al pasarlo por chat/email y limpió el password, o aún no lo descargó realmente del portal.
2. **Falta el perfil MSDeploy**: `azure/webapps-deploy@v3` necesita el perfil MSDeploy. Si solo hay FTP o Zip, el deploy falla. El parser avisa: "regenera el publish profile en Deployment Center".

El método `PareceUnPlaceholder` cubre los placeholders más comunes:

```csharp
private static bool PareceUnPlaceholder(string p)
{
    var lower = p.Trim().ToLowerInvariant();
    return lower.Contains("password-larguísima")
        || lower.Contains("password-larguisima")
        || lower.Contains("changeme")
        || lower.Contains("xxxxxxxx")
        || lower.Contains("...");
}
```

Útil cuando alguien te pasa un XML "de ejemplo" para verificar y resulta que tiene placeholders. El parser lo detecta inmediatamente.

### `WorkflowBuilder.Construir` — los tres niveles de workflow

El builder genera distintos workflows según las opciones:

**Nivel 1 — Workflow mínimo** (un solo job):

```yaml
name: Deploy a Azure
on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    - run: dotnet restore
    - run: dotnet build --configuration Release
    - run: dotnet publish -c Release -o ./publish
    - uses: azure/webapps-deploy@v3
      with:
        app-name: 'mi-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

Cinco minutos de setup. Build + publish + deploy en el mismo job. El que aprende CI/CD por primera vez sale con esto.

**Nivel 2 — Workflow con tests** (dos jobs con `needs:`):

```yaml
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
    - run: dotnet restore
    - run: dotnet build --configuration Release
    - run: dotnet test --no-build
    - uses: actions/upload-artifact@v4
      with: { name: app, path: ./publish }

  deploy:
    runs-on: ubuntu-latest
    needs: build-test           # ← solo deploy si build-test pasa
    steps:
    - uses: actions/download-artifact@v4
      with: { name: app, path: ./publish }
    - uses: azure/webapps-deploy@v3
      with: { ... }
```

Tests verdes obligatorios antes del deploy. Es el patrón profesional.

**Nivel 3 — Workflow con environment + smoke + solo tags**:

```yaml
on:
  push:
    tags: ['v*']                  # solo deploy en tags

jobs:
  deploy:
    environment: production       # required reviewers en GitHub
    steps:
    - ...
    - uses: azure/webapps-deploy@v3
    - name: Smoke test
      run: |
        curl -f https://mi-app.azurewebsites.net/health
```

El `environment: production` con required reviewers configurado en GitHub (Settings → Environments → New → production → Required reviewers) **bloquea el deploy hasta que un reviewer apruebe**. Mismo concepto que el environment de ADO de S8.P.

Las **knobs** del builder:

```csharp
public sealed record OpcionesWorkflow(
    string AppName,
    string DotNetVersion = "8.0.x",
    bool IncluirTests = false,
    bool SoloEnTags = false,
    bool SmokeAlFinal = false,
    bool EnvironmentProduccion = false);
```

Cuatro flags que se combinan para escalar del nivel 1 al 3.

### `MetodoAuthRecomendador.Recomendar` — tres salidas posibles

La función de decisión:

```csharp
public static RecomendacionAuth Recomendar(EscenarioAuth e)
{
    // 1) Producción crítica + Entra controlado → OIDC.
    if (e.ControlaEntraId &&
        (e.AuditoriaRequerida || e.MultiEnvironment ||
         e.EquipoGrande || e.ProyectoEnProduccion))
    {
        return new RecomendacionAuth(MetodoAuth.Oidc, ...);
    }

    // 2) Side-project o sin Entra controlado → Publish Profile.
    if (e.SideProjectPersonal || !e.ControlaEntraId)
    {
        return new RecomendacionAuth(MetodoAuth.PublishProfile, ...);
    }

    // 3) Caso intermedio: Environment Secret con reviewers.
    return new RecomendacionAuth(MetodoAuth.EnvironmentSecret, ...);
}
```

Tres salidas con sus razones y sus riesgos:

**OIDC**:
- ✅ Razones: tokens minutos, audit, nada que rotar.
- ⚠ Riesgos: setup 30-60 min, requiere permisos para crear App Registration.

**Publish Profile**:
- ✅ Razones: setup 5 min, fácil migrar luego, basta para side-projects.
- ⚠ Riesgos: password longeva, si se filtra acceso permanente, sin audit por pipeline.

**Environment Secret** (intermedio):
- ✅ Razones: añade reviewers + branch policy al Publish Profile.
- ⚠ Riesgos: sigue siendo password en GitHub Secrets, rotar cada 90 días.

La función no impone una respuesta; **da una recomendación razonada según contexto**. La conversación con el equipo es la importante:

- "¿Vas a usar esto en producción con datos críticos?" → OIDC.
- "¿Es un side-project o estás aprendiendo?" → Publish Profile.
- "¿Producción pero sin permisos en Entra?" → Environment Secret.

### `PracticaGhActionsPlanner` — el plan + checklist

El servicio inyectable que une los anteriores. Compone: análisis del publish profile, workflow generado según opciones, recomendación de método de auth, checklist de 12 puntos del entregable.

Checklist del entregable:

```
[ ] Web App F1 creada en Azure (Linux, .NET 8/10)
[ ] Repo de GitHub creado y pusheado
[ ] Publish profile descargado y XML guardado en local
[ ] Parser valida XML: MSDeploy presente, password real, no placeholder
[ ] Secret AZURE_WEBAPP_PUBLISH_PROFILE en GitHub Settings
[ ] XML local borrado tras crear el secret
[ ] Workflow .github/workflows/deploy.yml con los 6 steps canónicos
[ ] App-name en el workflow corresponde a la Web App real
[ ] Push a main dispara el workflow
[ ] Build verde y deploy completado
[ ] /health responde 200 desde la Web App
[ ] Cleanup (az group delete + gh repo delete + gh secret delete)
```

Los 12 puntos cubren del paso 1 (Web App creada) al paso 12 (cleanup). Cuando los 12 están verdes, la práctica está completa.

---

## 6. Las dos opciones, en una tabla

| Característica | Publish Profile (esta práctica) | OIDC (S8.P y S8.P principal) |
| --- | --- | --- |
| **Tiempo de setup** | 5 minutos | 30-60 minutos |
| **Permisos requeridos** | Acceso a la Web App | Permisos en Entra ID + RBAC |
| **Tipo de credencial** | Password de larga duración | Token federado de 1 hora |
| **Riesgo si se filtra** | Acceso permanente hasta rotar | Tokens caducan en 1 hora |
| **Rotación** | Manual cada 90 días | Automática |
| **Audit** | Limitado | Microsoft Entra audita cada auth |
| **Sirve para** | Side-projects, MVPs, aprender | Producción crítica, equipos serios |

La elección depende del contexto, no de la moda. **Empieza con el que te aplique hoy y migra si las condiciones cambian**.

---

## 7. La migración de Publish Profile a OIDC

Si empezaste con Publish Profile y ahora quieres OIDC, **el cambio es trivial**:

1. **En Azure**: crea App Registration; bajo "Federated credentials", añade una federated credential apuntando a `repo:miusuario/mirepo:ref:refs/heads/main`.
2. **Asigna RBAC** a la App Registration (Contributor sobre el RG del App Service).
3. **En GitHub Secrets**: borra `AZURE_WEBAPP_PUBLISH_PROFILE`. Añade variables públicas (no secrets): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
4. **En el workflow**: añade `permissions: id-token: write` al job. Reemplaza el step de deploy:

```yaml
permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - uses: azure/login@v2
      with:
        client-id: ${{ vars.AZURE_CLIENT_ID }}
        tenant-id: ${{ vars.AZURE_TENANT_ID }}
        subscription-id: ${{ vars.AZURE_SUBSCRIPTION_ID }}
    - uses: azure/webapps-deploy@v3
      with:
        app-name: 'mi-app'
        # nota: sin publish-profile, autenticación viene del login anterior
        package: ./publish
```

15 minutos de trabajo, sin tocar Azure (la App Registration permanece). Cuando el proyecto crece, migras sin reescribir todo el pipeline.

---

## 8. Cómo probarlo en local

```bash
dotnet run --project src/Practica.GhActions.Demo.Api
# http://localhost:5112
```

Endpoints:

```http
### Parsear un publish profile XML
POST http://localhost:5112/ghactions/profile/parsear
Content-Type: application/xml

<publishData>
  <publishProfile profileName="miapp" publishMethod="MSDeploy"
                  publishUrl="miapp.scm.azurewebsites.net:443"
                  userName="$miapp" userPWD="..." ... />
</publishData>
# → { esValido: false, advertencias: ["Password parece placeholder..."] }

### Generar workflow nivel 2 (con tests)
POST http://localhost:5112/ghactions/workflow
Content-Type: application/json

{
  "appName": "mi-app",
  "dotNetVersion": "10.0.x",
  "incluirTests": true,
  "smokeAlFinal": true
}
# → workflow con 2 jobs (build-test → deploy) + step de smoke test

### Recomendar método de auth
POST http://localhost:5112/ghactions/auth/recomendar
Content-Type: application/json

{
  "sideProjectPersonal": false,
  "controlaEntraId": true,
  "proyectoEnProduccion": true,
  "auditoriaRequerida": true
}
# → Oidc con razones y riesgos

### Plan completo
POST http://localhost:5112/ghactions/plan
```

Los 32 tests cubren el parsing del XML con MSDeploy + FTP + Zip + placeholders + sin nodo raíz, la generación del workflow con cada combinación de knobs, y las tres salidas del recomendador con todos los escenarios.

Para verificar contra tu Web App + repo real:

```bash
./scripts/demo.sh
# 1) 01-publish-profile.sh → descarga el XML con la password enmascarada
# 2) 02-runs.sh            → lista runs del workflow + smoke a la URL
```

`publish-profile.xml` queda en `scripts/` y está en `.gitignore` — nunca llega a git. **Solo lectura**: no crea ni modifica recursos.

> Yo no lanzo apps. Tú haces `dotnet run`, `dotnet test`, `az` y `gh`.

---

## 9. La rotación obligatoria del publish profile (slide 18)

Si te quedas con Publish Profile en producción —no migras a OIDC—, **rotar cada 90 días** es la práctica operativa mínima:

1. Portal → Web App → **Get publish profile** (vuelve a descargar).
2. GitHub → Settings → Secrets → `AZURE_WEBAPP_PUBLISH_PROFILE` → **Update**.
3. Verifica con un deploy: push trivial a `main`, ver que el workflow pasa.

Por qué importa: el publish profile **funciona indefinidamente** mientras no se rote. Si se filtró un día (porque alguien lo pasó por chat, lo subió a un gist por error, hizo un screenshot), **el atacante tiene acceso para siempre**. La rotación cada 90 días limita la ventana de exposición.

Pon una tarea recurrente en tu calendario: "Rotar publish profile de mi-app — primer lunes de cada trimestre". Cinco minutos, te mantiene seguro.

---

## 10. Glosario breve

- **Publish Profile**: XML descargable desde Portal → Web App → Get publish profile. Contiene credenciales (MSDeploy + FTP + Zip Deploy) para desplegar a la Web App.
- **GitHub Secret**: variable encriptada en Settings → Secrets and variables → Actions. Solo legible desde el workflow, no desde el repo.
- **GitHub Environment**: agrupación de configuración (secrets, vars, protection rules) para un entorno concreto (production, staging).
- **Required reviewers** (en Environment): personas cuya aprobación es necesaria para que el workflow despliegue al environment.
- **OIDC en GitHub Actions**: federación entre GitHub y Entra ID sin secret almacenado. Mismo concepto que Workload Identity Federation en ADO.
- **`azure/webapps-deploy@v3`**: action oficial de Microsoft para desplegar a Azure App Service. Soporta publish profile y OIDC.
- **`azure/login@v2`**: action oficial para autenticar contra Azure. Necesaria para OIDC.
- **`actions/setup-dotnet@v4`**: action oficial para instalar .NET SDK en el runner.
- **`actions/upload-artifact@v4` / `download-artifact@v4`**: acciones para compartir artefactos entre jobs.
- **`needs:`**: declaración de dependencia entre jobs en GitHub Actions (equivalente a `dependsOn` de ADO).
- **`if: success()` / `if: failure()`**: condiciones para ejecutar steps según resultado anterior.
- **`gh` CLI**: cliente de línea de comandos oficial de GitHub. Útil para automatizar setup de secrets, environments, etcétera.

---

## 11. Cierre del módulo M08

Con S8.P2 completas el módulo de DevOps y Automatización. Resumen del recorrido:

- **S8.1** — Azure DevOps: Repos, Boards, Artifacts.
- **S8.2** — Pipelines CI/CD en YAML.
- **S8.3** — Despliegue automatizado (estrategias + health + rollback).
- **S8.4** — ADO vs GitHub Actions.
- **S8.5** — IaC con Bicep.
- **S8.6** — Application Insights y monitoring.
- **S8.P** — Práctica Pipeline CI/CD profesional (OIDC + auto-rollback).
- **S8.P2** — Práctica GitHub Actions + publish profile (esta).

Si te quedas con una sola cosa de todo M08, que sea esta: **un pipeline serio tiene tres puertas (build, smoke, aprobación) y dos redes de seguridad (auto-rollback con `condition: failed()` + observabilidad con App Insights)**. Sin esas piezas, los deploys son un acto de fe. Con ellas, son rutina segura.

Lo siguiente sería el módulo **M09 — IA como Herramienta de Desarrollo** (Claude Code + Copilot + MCP), donde la conversación se mueve a cómo la IA cambia la forma de programar en 2026.
