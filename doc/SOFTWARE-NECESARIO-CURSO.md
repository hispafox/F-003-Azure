# Software necesario para el curso Azure AZ-204 + Bonus Claude Code

> **Curso:** Azure AZ-204 con .NET 8 + Claude Code  
> **Duración:** 11 módulos · 22 prácticas hands-on  
> **Fecha:** Abril 2026  
> **Plataforma soportada:** Windows / macOS / Linux (con notas específicas donde aplica)

---

## Resumen rápido

Si quieres llegar al primer día con todo listo, instala estos elementos:

| Categoría | Software | Obligatorio | Coste |
|---|---|---|---|
| **Runtime** | .NET 8 SDK (LTS) | ✅ Sí | Gratis |
| **CLI Azure** | Azure CLI v2.65+ | ✅ Sí | Gratis |
| **Functions** | Azure Functions Core Tools v4 | ✅ Sí | Gratis |
| **Editor** | Visual Studio Code (con extensiones) | ✅ Sí | Gratis |
| **Editor (Windows)** | Visual Studio 2022 Community | ⚠️ Solo M07 (MSIX) | Gratis |
| **Control de versiones** | Git | ✅ Sí | Gratis |
| **Cliente HTTP** | curl + jq | ✅ Sí | Gratis |
| **Containers** | Docker Desktop | ⚠️ Recomendado (Azurite, Cosmos local) | Gratis para uso personal |
| **Storage local** | Azurite | ⚠️ Recomendado (M03, M04, M05) | Gratis |
| **Cuenta cloud** | Suscripción Azure (Free Trial vale) | ✅ Sí | Gratis los primeros 200$ |
| **Cuenta GitHub** | GitHub Free | ✅ Sí | Gratis |
| **IA Coding** | Claude Code CLI | ⚠️ Solo M09, M11 | Plan Pro €20/mes o API |

**Tiempo estimado de setup completo:** 2-3 horas (descarga + instalación + verificación).

---

## 1. Runtime y SDK

### 1.1 .NET 8 SDK (LTS) — OBLIGATORIO

El curso usa **.NET 8** porque es la versión LTS actual con soporte hasta noviembre 2026.

**Instalación:**

```bash
# Verificar si ya lo tienes
dotnet --version
# Esperado: 8.0.x

# Si no lo tienes:
# Windows / Mac / Linux
# Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0

# Mac (con Homebrew)
brew install dotnet@8

# Linux Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Windows (con winget)
winget install Microsoft.DotNet.SDK.8
```

**Verificación:**

```bash
dotnet --version
# Debe mostrar: 8.0.x (donde x es cualquier número)

dotnet --list-sdks
# Lista todos los SDKs instalados, busca el 8.x

# Test rápido:
dotnet new webapi -n TestApp
cd TestApp
dotnet build
# Debe compilar sin errores
```

**Workloads adicionales** (no obligatorios pero útiles):

```bash
# Para apps WPF (M07):
dotnet workload install wpf

# Para apps Windows (M07):
dotnet workload install windowsdesktop
```

### 1.2 Node.js 18+ (LTS) — OBLIGATORIO

Necesario para Functions Core Tools y Claude Code (ambos via npm).

**Instalación:**

```bash
# Verificar
node --version
# Esperado: v18.x.x o superior

# Mac (Homebrew)
brew install node

# Windows (winget)
winget install OpenJS.NodeJS.LTS

# Linux (NodeSource)
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt-get install -y nodejs
```

**Verificación:**

```bash
node --version  # >= 18.0.0
npm --version   # >= 9.0.0
```

---

## 2. Azure tooling

### 2.1 Azure CLI — OBLIGATORIO

La herramienta clave para interactuar con Azure desde la terminal. Usada en TODAS las prácticas.

**Instalación:**

```bash
# Mac
brew update && brew install azure-cli

# Windows
winget install -e --id Microsoft.AzureCLI

# Linux Ubuntu/Debian
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Linux genérico
curl -L https://aka.ms/InstallAzureCli | bash
```

**Verificación:**

```bash
az --version
# Esperado: azure-cli >= 2.65.0

# Login a vuestra suscripción
az login
# Abre el navegador, autenticación OAuth

# Verificar suscripción activa
az account show -o table
```

**Si tenéis varias suscripciones:**

```bash
# Listar todas
az account list -o table

# Cambiar la default
az account set --subscription "Nombre o ID de la suscripción"
```

### 2.2 Azure Functions Core Tools v4 — OBLIGATORIO

Necesario para desarrollar y probar Azure Functions en local (M03, M04, M11).

**Instalación:**

```bash
# Mac
brew tap azure/functions
brew install azure-functions-core-tools@4

# Windows (winget)
winget install Microsoft.AzureFunctionsCoreTools

# Windows (npm)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Linux (npm)
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

**Verificación:**

```bash
func --version
# Esperado: 4.x.x

# Test rápido
mkdir test-func && cd test-func
func init . --worker-runtime dotnet-isolated --target-framework net8.0
func new --name HelloWorld --template "HTTP trigger" --authlevel anonymous
# Debe crear el proyecto sin errores
```

### 2.3 Azurite (emulador de Storage) — RECOMENDADO

Emulador local de Azure Storage. Permite desarrollar Storage / Queues / Tables / Blobs sin consumir Azure real (M03, M04, M05).

**Instalación (vía npm):**

```bash
npm install -g azurite

# Verificar
azurite --version
# Esperado: 3.x.x
```

**Uso típico:**

```bash
# Arrancar Azurite (en una terminal separada)
azurite --location ./.azurite --silent

# Connection string para apps:
# UseDevelopmentStorage=true
```

**Alternativa Docker:**

```bash
# Si preferís Docker
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

### 2.4 Cosmos DB Emulator — OPCIONAL

Solo si vais a hacer la práctica principal de M05 con Cosmos DB.

**Windows (recomendado):**

```bash
# Descargar de:
# https://aka.ms/cosmosdb-emulator

# Instalación: ejecutable normal
# Una vez instalado, arranca al iniciar Windows
```

**Mac/Linux:**

```bash
# Versión Docker (más nueva, preview)
docker run -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview

# La versión Linux/Mac es preview, puede tener limitaciones
```

**Alternativa:** crear cuenta de Cosmos en Azure con free tier (1000 RU/s gratis, ~25 GB gratis primer año).

### 2.5 Storage Explorer — OPCIONAL

Aplicación desktop con UI para inspeccionar Storage Accounts (Tables, Blobs, Queues).

**Instalación:**

```bash
# Descargar de:
# https://azure.microsoft.com/products/storage/storage-explorer/

# Disponible para Windows / Mac / Linux
```

**Útil para:**
- Ver datos de Table Storage (M05)
- Inspeccionar blobs subidos por Functions (M03)
- Debug de queues con problemas

---

## 3. Editor / IDE

### 3.1 Visual Studio Code — OBLIGATORIO

El editor recomendado para todo el curso (excepto M07-MSIX que requiere Visual Studio).

**Instalación:**

```bash
# Mac
brew install --cask visual-studio-code

# Windows
winget install Microsoft.VisualStudioCode

# Linux
sudo snap install code --classic
# o desde https://code.visualstudio.com/download
```

#### Extensiones para VS Code — categorización completa

Las extensiones están organizadas en 4 categorías según prioridad:

1. **Esenciales** (obligatorias para el curso)
2. **Recomendadas** (mejoran significativamente la experiencia)
3. **Productividad** (opcionales pero muy útiles)
4. **Específicas por tema** (según módulos)

##### Categoría 1: Esenciales (obligatorias)

```bash
# === .NET / C# ===
code --install-extension ms-dotnettools.csdevkit              # C# Dev Kit (oficial Microsoft)
code --install-extension ms-dotnettools.csharp                # C# language support
code --install-extension ms-dotnettools.vscode-dotnet-runtime # .NET runtime auto-install
code --install-extension ms-dotnettools.vscodeintellicode-csharp # IntelliCode IA

# === Azure (oficial Microsoft) ===
code --install-extension ms-vscode.vscode-node-azure-pack     # Pack con todo lo de Azure
# ↑ Este pack incluye automáticamente:
#   - Azure Account
#   - Azure Resources
#   - Azure App Service
#   - Azure Functions
#   - Azure Storage
#   - Azure Databases (Cosmos)
#   - Azure CLI Tools

# Si prefieres instalar solo lo que necesitas en lugar del pack:
code --install-extension ms-azuretools.vscode-azureappservice
code --install-extension ms-azuretools.vscode-azurefunctions
code --install-extension ms-azuretools.vscode-azurestorage
code --install-extension ms-azuretools.vscode-cosmosdb
code --install-extension ms-azuretools.vscode-azureresourcegroups
code --install-extension ms-vscode.azurecli                   # Azure CLI Tools
code --install-extension ms-azuretools.vscode-bicep           # Bicep IaC (M08-S8.5)

# === Git / GitHub ===
code --install-extension github.vscode-github-actions         # Workflows YAML
code --install-extension github.vscode-pull-request-github    # PRs desde VS Code
code --install-extension eamodio.gitlens                      # Git superpoderes

# === Testing / API testing ===
code --install-extension humao.rest-client                    # Probar APIs (.http files)

# === Soporte de archivos ===
code --install-extension redhat.vscode-yaml                   # YAML validation
code --install-extension davidanson.vscode-markdownlint       # Markdown lint
code --install-extension editorconfig.editorconfig            # .editorconfig support
```

**Detalles de cada extensión esencial:**

| Extensión | Editor.x | Para qué |
|---|---|---|
| **C# Dev Kit** | ms-dotnettools.csdevkit | IntelliSense premium, debugger, Test Explorer, Solution Explorer |
| **C# (legacy)** | ms-dotnettools.csharp | Required base de C# Dev Kit |
| **.NET Runtime Install** | ms-dotnettools.vscode-dotnet-runtime | Auto-instala runtimes que faltan |
| **IntelliCode for C#** | ms-dotnettools.vscodeintellicode-csharp | Sugerencias IA contextual |
| **Azure Tools (pack)** | ms-vscode.vscode-node-azure-pack | Conjunto oficial de extensiones Azure |
| **Azure App Service** | ms-azuretools.vscode-azureappservice | Deploy de Web Apps, ver logs, slots |
| **Azure Functions** | ms-azuretools.vscode-azurefunctions | Crear/debug/deploy Functions, F5 corre local |
| **Azure Storage** | ms-azuretools.vscode-azurestorage | Explorar blobs, tables, queues, files |
| **Azure Cosmos DB** | ms-azuretools.vscode-cosmosdb | Queries SQL/Mongo, gestión de containers |
| **Azure Resources** | ms-azuretools.vscode-azureresourcegroups | Vista unificada de todos los recursos |
| **Azure CLI Tools** | ms-vscode.azurecli | IntelliSense para .azcli scripts |
| **Bicep** | ms-azuretools.vscode-bicep | IaC con sintaxis Bicep |
| **GitHub Actions** | github.vscode-github-actions | YAML workflows + visualizador |
| **GitHub PRs** | github.vscode-pull-request-github | Crear/revisar PRs sin salir de VS Code |
| **GitLens** | eamodio.gitlens | Blame, history, comparisons avanzados |
| **REST Client** | humao.rest-client | Archivos .http en lugar de Postman |
| **YAML** | redhat.vscode-yaml | Validation + IntelliSense YAML |
| **Markdownlint** | davidanson.vscode-markdownlint | Lint del CLAUDE.md y READMEs |
| **EditorConfig** | editorconfig.editorconfig | Formato consistente cross-team |

##### Categoría 2: Recomendadas (alta calidad de vida)

```bash
# === Calidad de código .NET ===
code --install-extension formulahendry.dotnet-test-explorer  # Test runner UI
code --install-extension kreativ-software.csharpextensions   # Snippets útiles
code --install-extension fudge.auto-using                    # Imports automáticos
code --install-extension jchannon.csharpextensions           # Refactorings extra
code --install-extension jorgeserrano.vscode-csharp-snippets # Snippets de código

# === Productividad general ===
code --install-extension streetsidesoftware.code-spell-checker  # Corrector ortográfico
code --install-extension streetsidesoftware.code-spell-checker-spanish # ES también
code --install-extension shardulm94.trailing-spaces             # Detecta espacios sobrantes
code --install-extension christian-kohler.path-intellisense     # Autocompletado de paths
code --install-extension naumovs.color-highlight                # Resalta colores hex

# === Visualización ===
code --install-extension PKief.material-icon-theme           # Iconos por tipo archivo
code --install-extension oderwat.indent-rainbow              # Indentación de colores
code --install-extension coenraads.bracket-pair-colorizer-2  # Brackets colored (legacy, ya built-in)
code --install-extension wayou.vscode-todo-highlight         # TODO/FIXME highlight

# === Productividad cuando programáis solo ===
code --install-extension wakatime.vscode-wakatime           # Time tracking automático
```

##### Categoría 3: Productividad (opcionales pero útiles)

```bash
# === Markdown ===
code --install-extension yzhang.markdown-all-in-one           # Comandos markdown completos
code --install-extension shd101wyy.markdown-preview-enhanced  # Preview con diagramas Mermaid
code --install-extension bierner.markdown-mermaid             # Mermaid en preview

# === Diagramas ===
code --install-extension hediet.vscode-drawio                # Draw.io embebido
code --install-extension jock.svg                            # SVG editor
code --install-extension pomdtr.excalidraw-editor            # Excalidraw embedded

# === DevOps / IaC ===
code --install-extension hashicorp.terraform                 # Terraform (alternativa a Bicep)
code --install-extension ms-kubernetes-tools.vscode-kubernetes-tools # K8s
code --install-extension ms-azuretools.vscode-docker         # Docker management

# === REST y APIs ===
code --install-extension rangav.vscode-thunder-client        # Postman alternative más ligero
code --install-extension 42crunch.vscode-openapi             # OpenAPI editor

# === Otros lenguajes (por si se necesitan) ===
code --install-extension dbaeumer.vscode-eslint              # JS/TS linting
code --install-extension esbenp.prettier-vscode              # Formateador universal
code --install-extension ms-python.python                    # Python (algunos scripts)
```

##### Categoría 4: Específicas por módulo

```bash
# === Para Claude Code (M09, M11) ===
code --install-extension anthropic.claude-code               # Extensión oficial Claude Code
code --install-extension anthropic.claude-vscode             # Si está disponible (ver docs)

# === Para BBDD (M05) ===
code --install-extension ms-mssql.mssql                      # SQL Server / Azure SQL
code --install-extension cweijan.vscode-database-client2     # Multi-DB client
code --install-extension cweijan.vscode-mysql-client2        # MySQL si lo usáis

# === Para Service Bus / Event Grid (M07) ===
code --install-extension ms-azuretools.vscode-azureservicebus  # Service Bus

# === Para frontend si añadís Blazor (M10) ===
code --install-extension ms-dotnettools.blazorwasm-companion # Blazor WASM debug
code --install-extension ms-vscode.live-server               # Servidor local rápido

# === Para MSIX (raro en VS Code, normalmente VS 2022) ===
# No hay extensión específica de MSIX en VS Code
# → Usar Visual Studio 2022 para MSIX (M07)
```

##### Instalación rápida (todo de una vez)

Si queréis instalar TODAS las esenciales + recomendadas con un solo comando:

```bash
# Crear archivo con la lista
cat > vscode-extensions.txt << 'EOF'
ms-dotnettools.csdevkit
ms-dotnettools.csharp
ms-dotnettools.vscode-dotnet-runtime
ms-dotnettools.vscodeintellicode-csharp
ms-vscode.vscode-node-azure-pack
ms-azuretools.vscode-bicep
ms-vscode.azurecli
github.vscode-github-actions
github.vscode-pull-request-github
eamodio.gitlens
humao.rest-client
redhat.vscode-yaml
davidanson.vscode-markdownlint
editorconfig.editorconfig
formulahendry.dotnet-test-explorer
fudge.auto-using
streetsidesoftware.code-spell-checker
streetsidesoftware.code-spell-checker-spanish
PKief.material-icon-theme
oderwat.indent-rainbow
yzhang.markdown-all-in-one
ms-azuretools.vscode-docker
EOF

# Instalar todas
cat vscode-extensions.txt | xargs -L1 code --install-extension
```

##### Listar y exportar extensiones

```bash
# Listar todas las extensiones instaladas
code --list-extensions

# Exportar para compartir con tu equipo
code --list-extensions > my-extensions.txt

# Compañero importa
cat my-extensions.txt | xargs -L1 code --install-extension
```

##### Settings.json recomendado del curso

Crear/editar `~/.config/Code/User/settings.json` (Linux), `~/Library/Application Support/Code/User/settings.json` (Mac), o `%APPDATA%/Code/User/settings.json` (Windows):

```json
{
  // Editor
  "editor.fontSize": 14,
  "editor.fontFamily": "'JetBrains Mono', 'Cascadia Code', Consolas, monospace",
  "editor.fontLigatures": true,
  "editor.tabSize": 4,
  "editor.formatOnSave": true,
  "editor.formatOnPaste": true,
  "editor.rulers": [120],
  "editor.bracketPairColorization.enabled": true,
  "editor.guides.bracketPairs": true,
  "editor.linkedEditing": true,
  "editor.suggestSelection": "first",
  
  // C# Dev Kit
  "dotnet.defaultSolution": "auto",
  "dotnet.codeLens.enableReferencesCodeLens": true,
  "dotnet.codeLens.enableTestsCodeLens": true,
  "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true,
  "dotnet.inlayHints.enableInlayHintsForParameters": true,
  
  // C# Specific
  "csharp.format.enable": true,
  "csharp.semanticHighlighting.enabled": true,
  "csharp.suppressDotnetInstallWarning": false,
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp",
    "editor.tabSize": 4
  },
  
  // Azure
  "azureFunctions.deploySubpath": "publish",
  "azureFunctions.scmDoBuildDuringDeployment": true,
  "azureFunctions.projectLanguage": "C#",
  "azureFunctions.projectRuntime": "~4",
  "azure.tenant": "",  // dejar vacío salvo multi-tenant
  
  // Bicep
  "[bicep]": {
    "editor.defaultFormatter": "ms-azuretools.vscode-bicep",
    "editor.tabSize": 2
  },
  
  // YAML (workflows GitHub)
  "[yaml]": {
    "editor.defaultFormatter": "redhat.vscode-yaml",
    "editor.tabSize": 2,
    "editor.insertSpaces": true
  },
  "yaml.schemas": {
    "https://json.schemastore.org/github-workflow.json": ".github/workflows/*.{yml,yaml}",
    "https://json.schemastore.org/azure-pipelines.json": "azure-pipelines.yml"
  },
  
  // Markdown
  "[markdown]": {
    "editor.defaultFormatter": "yzhang.markdown-all-in-one",
    "editor.wordWrap": "on",
    "editor.quickSuggestions": {
      "comments": "off",
      "strings": "off",
      "other": "off"
    }
  },
  
  // Terminal
  "terminal.integrated.fontSize": 13,
  "terminal.integrated.fontFamily": "'JetBrains Mono', monospace",
  "terminal.integrated.defaultProfile.osx": "zsh",
  "terminal.integrated.defaultProfile.linux": "bash",
  "terminal.integrated.defaultProfile.windows": "PowerShell",
  
  // Git
  "git.autofetch": true,
  "git.confirmSync": false,
  "git.enableSmartCommit": true,
  "git.suggestSmartCommit": false,
  "gitlens.codeLens.enabled": true,
  
  // Files
  "files.autoSave": "afterDelay",
  "files.autoSaveDelay": 1000,
  "files.trimTrailingWhitespace": true,
  "files.insertFinalNewline": true,
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/.vs": true,
    "**/node_modules": true,
    "**/.azurite": true
  },
  "files.watcherExclude": {
    "**/bin/**": true,
    "**/obj/**": true,
    "**/node_modules/**": true
  },
  
  // Search
  "search.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/.vs": true,
    "**/publish": true,
    "**/node_modules": true,
    "**/dist": true
  },
  
  // Spell checker
  "cSpell.language": "en,es",
  "cSpell.userWords": [
    "Anthropic",
    "Azurite",
    "Bicep",
    "Cosmos",
    "Entra",
    "msix",
    "OIDC",
    "webapp"
  ],
  
  // REST Client
  "rest-client.environmentVariables": {
    "$shared": {
      "baseUrl": "http://localhost:5000"
    },
    "local": {
      "baseUrl": "http://localhost:5000"
    },
    "azure": {
      "baseUrl": "https://your-app.azurewebsites.net"
    }
  },
  
  // Workbench
  "workbench.iconTheme": "material-icon-theme",
  "workbench.editor.enablePreview": false,
  "workbench.startupEditor": "newUntitledFile",
  
  // Telemetry (opcional, podéis desactivarlo)
  "telemetry.telemetryLevel": "off"
}
```

##### Keybindings recomendados

`~/.config/Code/User/keybindings.json`:

```json
[
  {
    "key": "ctrl+shift+r",
    "command": "workbench.action.reloadWindow"
  },
  {
    "key": "ctrl+shift+y",
    "command": "workbench.action.terminal.toggleTerminal"
  },
  {
    "key": "f5",
    "command": "workbench.action.debug.start",
    "when": "!inDebugMode"
  },
  {
    "key": "ctrl+f5",
    "command": "workbench.action.debug.run"
  }
]
```

### 3.2 Visual Studio 2022 / 2026 — SOLO PARA M07

**Solo necesario** si vais a hacer las prácticas de MSIX (M07-S7.P o M07-S7.P2). MSIX requiere Visual Studio en Windows.

**Plataforma:** **Windows-only**

#### Versión recomendada

A fecha de abril 2026:

| Versión | Estado | Recomendación |
|---|---|---|
| **Visual Studio 2026** | Más nueva, Insiders/RC | ✅ Si quieres lo último |
| **Visual Studio 2022** | LTS (mainstream) | ✅ Lo más estable y compatible |
| **Visual Studio 2019** | EOL | ❌ No usar |

**Para el curso:** cualquiera de las dos vale. **VS 2022** es lo más universal. **VS 2026** trae mejoras de IA-assisted coding y rendimiento, pero algunas extensiones de terceros pueden tardar en actualizarse.

#### Edition recomendada

- **Community** (gratis) — ✅ suficiente para todo el curso
- **Professional** (~€600/año) — Solo si lo necesitáis por compliance corporate
- **Enterprise** (~€2.500/año) — Solo para enterprises con casos muy específicos

**Community Edition es gratis para:**
- Uso personal
- Open source
- Educación / aprendizaje
- Empresas <250 empleados con <$1M revenue

#### Instalación

```bash
# Opción A: descargar instalador desde web
# URL: https://visualstudio.microsoft.com/downloads/

# Opción B: con winget (recomendado)
# Visual Studio 2022:
winget install Microsoft.VisualStudio.2022.Community

# Visual Studio 2026 (cuando sea GA):
winget install Microsoft.VisualStudio.2026.Community

# Opción C: si ya lo tienes, solo abrir Visual Studio Installer y modificar
```

#### Workloads necesarios

Durante la instalación, en la pestaña **Workloads**:

```
☑ ASP.NET and web development
   - Necesario para .NET 8 web apps
   - Incluye: ASP.NET Core, MVC, Blazor

☑ .NET desktop development
   - Necesario para WPF (M07)
   - Incluye: WPF, WinForms, .NET 8 desktop

☑ Universal Windows Platform development  (opcional)
   - Recomendado para apps UWP modernas
   - Útil si exploráis WinUI 3

☑ Azure development  (recomendado)
   - Tools de Azure integrados en VS
   - Cosmos DB Emulator extension
   - Service Fabric, Service Bus
```

#### Componentes individuales (CRÍTICOS para M07)

En la pestaña **Individual components**, marcar:

```
=== MSIX (CLAVE para M07) ===
☑ Windows Application Packaging Tools
☑ MSIX Packaging Tool                       (opcional pero útil)
☑ MSIX Bundle Signing Tools

=== .NET ===
☑ .NET 8 SDK (LTS)
☑ .NET 8.0 Runtime
☑ NuGet package manager

=== Git ===
☑ Git for Windows
☑ GitHub extension for Visual Studio

=== Otros útiles ===
☑ Live Share
☑ IntelliCode
☑ Class Designer (visualizar diagramas de clases)
☑ T4 Templates
☑ Code Analysis tools
```

**Tiempo de descarga total:** 5-15 GB según workloads.

#### Extensiones recomendadas para Visual Studio 2022 / 2026

Visual Studio tiene un Marketplace propio (distinto al de VS Code). Estas son las extensiones recomendadas para el curso:

##### Categoría 1: Esenciales (productividad core)

```
1. Productivity Power Tools 2022
   - URL: https://marketplace.visualstudio.com/items?itemName=VisualStudioPlatformTeam.ProductivityPowerPack2022
   - Pack de mejoras de Microsoft
   - Incluye: Solution Error Visualizer, Match Margin, Custom Document Well, etc.

2. Visual Studio IntelliCode
   - Suele venir preinstalado
   - IA para autocompletado contextual
   - Free

3. CodeMaid
   - URL: https://marketplace.visualstudio.com/items?itemName=SteveCadwallader.CodeMaid
   - Auto-formatea, organiza using, limpia código
   - Acción rápida: Ctrl+M, Space

4. ReSharper o Rider Features  (DE PAGO, ~€300/año)
   - URL: https://www.jetbrains.com/resharper/
   - El "santo grial" de productividad .NET
   - Refactorings avanzados, navigation, code analysis
   - Alternativa free: las built-in de VS son cada vez mejores
```

##### Categoría 2: Azure / Cloud

```
5. Azure SDK for Visual Studio
   - Suele venir con el workload "Azure development"
   - Cosmos DB Explorer integrado
   - Storage Explorer integrado

6. Cosmos DB SQL API Tooling
   - URL: https://marketplace.visualstudio.com/items?itemName=ms-azuretools.cosmosdb-sql-tooling
   - Queries Cosmos desde VS

7. Azure Functions Tools  
   - Auto-incluido en workload Azure
   - Crear, debug, publish Functions

8. Azure App Service Tools
   - Deploy directo desde VS al App Service
```

##### Categoría 3: MSIX (CRÍTICO para M07)

```
9. MSIX Packaging Tools  (Microsoft)
   - Incluido en componentes individuales (ver arriba)
   - NO es una extensión del Marketplace, es un componente

10. Advanced Installer for Visual Studio  (free version)
    - URL: https://marketplace.visualstudio.com/items?itemName=Caphyon.AdvancedInstaller
    - UI más visual para configurar MSIX que el wizard built-in
    - Útil para casos avanzados
    - Versión Pro: ~€500 (no necesaria para el curso)

11. Windows Community Toolkit
    - URL: https://marketplace.visualstudio.com/items?itemName=...
    - Plantillas y templates útiles para WPF/UWP
```

##### Categoría 4: Git / GitHub

```
12. GitHub Extension for Visual Studio
    - URL: https://marketplace.visualstudio.com/items?itemName=GitHub.GitHubExtensionforVisualStudio
    - Free, Microsoft maintained
    - Crear repos, PRs, issues desde VS
    - YA incluido en workload "ASP.NET and web development"

13. GitHub Copilot
    - URL: https://marketplace.visualstudio.com/items?itemName=GitHub.copilot
    - €10/mes individual, €19/mes business
    - Code completion con IA
    - Recomendado si lo usáis en GitHub
    - Alternativa: IntelliCode (gratis, menos potente)

14. GitHub Copilot Chat
    - Add-on para GitHub Copilot
    - Chat dentro de VS para code questions
```

##### Categoría 5: Calidad de código

```
15. SonarLint
    - URL: https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2022
    - Static analysis on-the-fly
    - Detecta bugs, code smells, security issues
    - Free para detección local

16. Roslynator 2022
    - URL: https://marketplace.visualstudio.com/items?itemName=josefpihrt-vscode.roslynator2022
    - 500+ analyzers + refactorings
    - Excelente para .NET avanzado

17. CodeRush
    - URL: https://marketplace.visualstudio.com/items?itemName=DevExpress.CodeRushforVS2022
    - Free desde DevExpress
    - Refactorings + navigation rápida
    - Alternativa free a ReSharper
```

##### Categoría 6: Productividad

```
18. VsVim
    - URL: https://marketplace.visualstudio.com/items?itemName=JaredParMSFT.VsVim
    - Para fans de Vim
    - Modo Vim dentro de VS

19. Output enhancer
    - URL: https://marketplace.visualstudio.com/items?itemName=NikolayBalakin.Outputenhancer
    - Colorea el output window (errors rojos, warnings amarillos)

20. Solution Colors
    - URL: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.SolutionColors
    - Color por solución (útil si tenéis muchas)

21. File Icons
    - URL: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.FileIcons
    - Iconos por tipo de archivo en Solution Explorer

22. Markdown Editor v2
    - URL: https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor2
    - Editor markdown + preview en VS

23. SwitchStartupProject
    - URL: https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectforVS2022
    - Cambiar startup project rápidamente en multi-project solutions
```

##### Categoría 7: Testing

```
24. NUnit Test Adapter
    - Auto-detecta tests NUnit
    - Suele venir con el workload .NET

25. xUnit Test Adapter
    - Auto-detecta tests xUnit
    - Suele venir con el workload .NET

26. Live Unit Testing  (Enterprise only)
    - Tests automáticos al guardar
    - Solo en VS Enterprise
```

##### Categoría 8: Específicas para Claude Code (M09, M11)

```
27. Claude for Visual Studio  (cuando esté disponible)
    - URL: TBD (Anthropic está trabajando en una)
    - Por ahora: usar Claude Code en terminal externa
    - VS Code tiene mejor integración Claude actualmente
```

#### Cómo instalar extensiones en Visual Studio

**Opción A: desde dentro de Visual Studio (recomendado)**

```
1. Extensions menu → Manage Extensions
2. Click "Online" en el panel izquierdo
3. Buscar la extensión por nombre
4. Click "Download"
5. Cerrar Visual Studio (la instalación se ejecuta al cerrar)
6. VSIX Installer se ejecuta
7. Click "Modify"
8. Reabrir Visual Studio
```

**Opción B: descargar VSIX manualmente**

```
1. Ir a https://marketplace.visualstudio.com/
2. Buscar la extensión
3. Click "Download"
4. Doble-click sobre el .vsix
5. Seguir el wizard
```

**Opción C: Visual Studio Installer**

```
1. Abrir Visual Studio Installer
2. Click "Modify" en VS 2022/2026
3. Pestaña "Individual components"
4. Buscar y marcar
5. Click "Modify"
```

#### Settings recomendados para el curso

`Tools → Options` y configurar:

```
=== Text Editor ===
- Tabs: Insert spaces (4)
- Save documents as Unicode (UTF-8)
- Trim trailing whitespace on save: ON

=== Environment ===
- General → Color theme: Dark / Blue (vuestra preferencia)
- AutoRecover: Save AutoRecover info every 1 min

=== Projects and Solutions ===
- Track Active Item in Solution Explorer: ON
- Save changes before build: Save All

=== Source Control ===
- Plug-in Selection: Git
- Default fetch interval: 5 minutes

=== Debugging ===
- Enable .NET Framework source stepping: OFF (acelera debugging)
- Just My Code: ON (default)
- Enable Diagnostic Tools while debugging: ON
```

#### Configurar .editorconfig en proyectos

Crear archivo `.editorconfig` en la raíz del proyecto:

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{json,yml,yaml,xml,csproj,sln}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false  # markdown puede usar trailing spaces para line breaks

[*.{cs,csx,vb,vbx}]
# === .NET formatting rules ===
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# === C# formatting ===
csharp_using_directive_placement = outside_namespace:warning
csharp_prefer_braces = true:warning
csharp_style_namespace_declarations = file_scoped:warning
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_method_call_parameter_list_parentheses = false

# === Naming conventions ===
dotnet_naming_rule.types_should_be_pascal_case.severity = warning
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case_style

dotnet_naming_symbols.types.applicable_kinds = class,struct,interface,enum,delegate
dotnet_naming_style.pascal_case_style.capitalization = pascal_case
```

#### Visual Studio vs VS Code: comparativa para el curso

| Aspecto | Visual Studio 2022/2026 | **VS Code** |
|---|---|---|
| **Coste** | Community gratis | **Gratis siempre** |
| **Plataforma** | Windows-only | **Win/Mac/Linux** |
| **Tamaño instalación** | 5-30 GB | **~300 MB** |
| **Tiempo arranque** | 10-30s | **<3s** |
| **IntelliSense .NET** | Excelente (built-in) | **Bueno (con C# Dev Kit)** |
| **Debugging** | Excelente, profiler integrado | **Bueno, debugger universal** |
| **Designer WPF/WinForms** | Excelente (visual designer) | **Limitado (XAML solo) |
| **MSIX (M07)** | ✅ Wizard nativo | ❌ Solo CLI manual |
| **Solution Explorer** | Visual jerárquico premium | **Tree view simple** |
| **Refactoring** | Excelente (built-in + ReSharper) | **Bueno (con extensiones)** |
| **Para .NET** | El estándar de la industria | **Cada vez más popular** |
| **Para Azure** | Bueno | **Excelente (más tools dedicados)** |
| **Live debugging cloud** | Excelente | **Bueno** |
| **Para principiantes** | Curva más alta | **Más amigable** |

**Para el curso:**

```
Si usáis Mac/Linux:                        VS Code (forzosamente)
Si usáis Windows + venís de .NET clásico:  VS 2022/2026
Si usáis Windows + queréis lo moderno:     VS Code
Si vais a hacer M07 (MSIX):                VS 2022/2026 OBLIGATORIO

Mi recomendación general 2026:
- VS 2022/2026 para proyectos grandes empresariales o cuando MSIX
- VS Code para todo lo demás (más rápido, multiplataforma)
```

#### Si NO usáis Windows

Para M07 (MSIX) necesitáis:

```
Opción A: VM Windows
- Parallels Desktop (Mac, ~€100/año)
- VMware Fusion (Mac, free para uso personal)
- VirtualBox (multiplataforma, free)
- UTM (Mac M1/M2/M3, free)

Opción B: Azure VM con Windows
- Crear VM B2s con Windows 11
- ~€60-100/mes si la dejáis encendida
- Apagar cuando no la uséis: ~€5-15/mes
- Auto-shutdown a las 19:00

Opción C: Saltarse M07
- MSIX no es requisito de las certificaciones AZ-204
- El resto del curso no depende de M07
- Se puede hacer M07 al final con una VM temporal
```

### 3.3 Visual Studio para Mac — DEPRECATED

**No usar.** Microsoft descontinuó VS para Mac en agosto 2024.

**Alternativas en Mac:**
- **VS Code** (recomendado para .NET en Mac)
- **JetBrains Rider** (~€150/año, gratis para students/open source)
- **VS 2022/2026 en VM Windows** (si trabajáis con MSIX)

---

## 4. Cuentas y suscripciones

### 4.1 Suscripción Azure — OBLIGATORIO

**Opción 1: Free Trial (recomendado para empezar)**

- URL: https://azure.microsoft.com/free
- 200$ de crédito durante 30 días
- Servicios siempre gratis durante 12 meses
- Suficiente para TODO el curso

**Opción 2: Cuenta corporativa**

- Si tu empresa tiene Azure, pedid acceso a una sub
- Necesitáis rol mínimo `Contributor` en algún Resource Group
- Verificar que podéis crear App Registrations en Microsoft Entra (M06)

**Opción 3: Pay-As-You-Go**

- Pagar solo por lo que usáis
- Coste estimado del curso completo: ~€3-5 si no se olvidan recursos
- Coste si todos los recursos se dejan corriendo: ~€100-300/mes

**Verificar acceso:**

```bash
az login
az account show -o table
# Debe devolver datos de vuestra suscripción
```

**Configurar caps de gasto (recomendado):**

```
Portal Azure → Cost Management + Billing
→ Cost alerts → New alert
→ Set budget: €10/month
→ Alerts at: 50%, 80%, 100%
```

### 4.2 Cuenta GitHub — OBLIGATORIO

Necesario para:
- Repositorios del curso (M08)
- GitHub Actions (M08-S8.P2)
- Codespaces si no tenéis máquina local

**Crear cuenta:**

1. https://github.com/signup (gratis)
2. Verificar email
3. Configurar 2FA (recomendado)

**GitHub Free incluye:**

- ✅ Repos públicos ilimitados
- ✅ Repos privados ilimitados
- ✅ 2.000 minutos/mes de Actions (en repos públicos: ilimitados)
- ✅ 500 MB de Packages

**GitHub CLI (recomendado):**

```bash
# Mac
brew install gh

# Windows
winget install GitHub.cli

# Linux
sudo apt-get install gh

# Login
gh auth login
```

### 4.3 Cuenta Anthropic / Claude — SOLO PARA M09, M11

Necesario para Claude Code (módulos 9 y 11).

**Opciones:**

| Plan | Coste | Adecuado para |
|---|---|---|
| **Free** | €0 | Probar Claude Code básico (limitado) |
| **Pro** | ~€20/mes | Uso personal sostenido |
| **Max** | ~€100/mes | Uso intensivo profesional |
| **API key** | Pay-per-use (~€0.01-0.50 por sesión) | Pipelines / scripts |

**Crear cuenta:**

1. https://claude.ai/signup
2. Verificar email
3. Para Claude Code: link de instalación https://docs.claude.com/en/docs/claude-code

**Instalación de Claude Code:**

```bash
# Vía npm (más universal)
npm install -g @anthropic-ai/claude-code

# Verificar
claude --version
# Esperado: 1.x.x

# Login (primera vez)
claude
# → abre navegador para login en claude.ai
# → autorizar Claude Code
# → vuelve a la terminal
```

**Plan recomendado para el curso:** Pro (€20/mes durante el mes que hagáis M09 y M11). Puedes cancelar después.

---

## 5. Herramientas de soporte

### 5.1 Git — OBLIGATORIO

Probablemente ya lo tienes, pero verifica:

```bash
git --version
# Esperado: 2.30+ (cualquier versión moderna vale)

# Configurar (si es la primera vez)
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
git config --global init.defaultBranch main
```

**Instalación si falta:**

```bash
# Mac
brew install git

# Windows
winget install Git.Git

# Linux
sudo apt-get install git
```

### 5.2 curl + jq — OBLIGATORIO

Para probar APIs y parsear JSON desde la terminal.

**curl:** suele venir preinstalado en Mac/Linux. En Windows está en Windows 10+.

```bash
curl --version
# Verificar que existe
```

**jq:** parser de JSON.

```bash
# Mac
brew install jq

# Windows
winget install jqlang.jq

# Linux
sudo apt-get install jq

# Verificar
jq --version
# Esperado: jq-1.6 o superior
```

### 5.3 PowerShell 7 — RECOMENDADO en Windows

Si estás en Windows, PowerShell 7 (cross-platform) es mejor que el built-in PowerShell 5.1.

```bash
# Windows
winget install Microsoft.PowerShell

# Mac
brew install powershell

# Linux
# https://learn.microsoft.com/powershell/scripting/install/installing-powershell
```

**Verificar:**

```bash
pwsh --version
# Esperado: 7.x
```

### 5.4 Postman / Insomnia / Bruno — OPCIONAL

Si preferís UI a curl para testing de APIs.

**Postman:** https://www.postman.com/downloads/  
**Insomnia:** https://insomnia.rest/download  
**Bruno:** https://www.usebruno.com/ (open source, recomendado)

**Para esta curso:** curl + REST Client extension de VS Code es suficiente.

### 5.5 Apache Bench (ab) — OPCIONAL

Para tests de carga ligeros.

```bash
# Mac
brew install httpd  # incluye ab

# Linux
sudo apt-get install apache2-utils

# Windows
# Viene con Apache HTTP Server: https://httpd.apache.org/
```

---

## 6. Containers (Docker)

### 6.1 Docker Desktop — RECOMENDADO

Necesario para:

- Azurite vía Docker (alternativa a npm)
- Cosmos DB Emulator en Mac/Linux
- Testcontainers para integration tests
- Algunos pipelines CI/CD locales

**Instalación:**

```bash
# Mac
brew install --cask docker

# Windows
winget install Docker.DockerDesktop

# Linux (Docker Engine, no Docker Desktop)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
```

**Verificar:**

```bash
docker --version
# Esperado: 24.x o superior

docker run hello-world
# Test que docker funciona
```

**Coste de Docker Desktop:**

- ✅ **Gratis** para uso personal y empresas pequeñas (<250 empleados, <$10M revenue)
- ⚠️ **De pago** para empresas más grandes (~$5/mes/usuario)
- ✅ **Alternativa free**: usar Docker Engine + CLI (sin Docker Desktop)

### 6.2 Alternativas a Docker Desktop

Si tu empresa restringe Docker Desktop o prefieres alternativas:

**Mac:** Colima, OrbStack, Rancher Desktop  
**Windows:** Rancher Desktop, Podman Desktop  
**Linux:** Docker Engine directo (sin Desktop)

```bash
# Mac con Colima (recomendado, free, ligero)
brew install colima
colima start

# Ahora docker CLI funciona contra Colima
docker run hello-world
```

---

## 7. Configuración por sistema operativo

### 7.1 macOS

**Setup mínimo recomendado:**

```bash
# 1. Homebrew (gestor de paquetes)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 2. Todo lo del curso de una vez
brew install dotnet@8 node azure-cli git jq powershell
brew install --cask visual-studio-code docker

# 3. Functions Core Tools
brew tap azure/functions
brew install azure-functions-core-tools@4

# 4. Tools npm globales
npm install -g azurite @anthropic-ai/claude-code

# 5. Login
az login
gh auth login
claude  # primera vez para login Claude
```

**Total tiempo:** ~30-45 min con buena conexión.

### 7.2 Windows

**Setup mínimo recomendado:**

```powershell
# Abrir PowerShell como Administrator

# 1. Habilitar winget (suele venir en Windows 10+)
# Si no lo tenéis: instalar "App Installer" desde Microsoft Store

# 2. Instalar todo el stack
winget install Microsoft.DotNet.SDK.8
winget install OpenJS.NodeJS.LTS
winget install Microsoft.AzureCLI
winget install Microsoft.AzureFunctionsCoreTools
winget install Microsoft.VisualStudioCode
winget install Microsoft.PowerShell
winget install Git.Git
winget install GitHub.cli
winget install jqlang.jq
winget install Docker.DockerDesktop

# 3. Para M07 (MSIX): Visual Studio 2022 Community
winget install Microsoft.VisualStudio.2022.Community

# 4. Tools npm globales (en una nueva terminal)
npm install -g azurite @anthropic-ai/claude-code

# 5. Login
az login
gh auth login
claude
```

**Configuración adicional Windows:**

```powershell
# Habilitar Developer Mode (para MSIX en M07)
Settings → Privacy & security → For developers
→ Developer Mode: ON

# Habilitar WSL2 (recomendado para algunas herramientas Linux)
wsl --install

# PowerShell execution policy (para scripts)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Total tiempo:** ~45-90 min (Visual Studio 2022 es lo que más tarda).

### 7.3 Linux (Ubuntu / Debian)

**Setup mínimo recomendado:**

```bash
# 1. Update sistema
sudo apt-get update && sudo apt-get upgrade -y

# 2. .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# 3. Node.js 18+ (NodeSource)
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt-get install -y nodejs

# 4. Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# 5. Git, jq, curl, etc.
sudo apt-get install -y git jq curl unzip apache2-utils

# 6. VS Code
sudo snap install code --classic

# 7. Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# 8. PowerShell (opcional)
sudo snap install powershell --classic

# 9. Functions Core Tools (vía npm tras tener Node)
sudo npm install -g azure-functions-core-tools@4 azurite @anthropic-ai/claude-code

# 10. GitHub CLI
sudo apt-get install gh

# 11. Login
az login
gh auth login
claude
```

**Limitación Linux:**

- ❌ Visual Studio 2022 (M07) NO tiene versión Linux
- → Para M07 necesitaréis VM Windows o saltarse el módulo
- ✅ Todo lo demás funciona perfectamente en Linux

---

## 8. Configuración recomendada de cuentas

### 8.1 Azure: setup inicial

Tras login, configurad lo siguiente para evitar sorpresas:

**1. Verificar suscripción:**

```bash
az account show -o table
# Debe mostrar la sub correcta
```

**2. Configurar región default:**

```bash
az config set defaults.location=westeurope
# Para que `az` use westeurope cuando no especifiquéis región
```

**3. Configurar alertas de gasto:**

```
Portal Azure → Cost Management + Billing
→ Cost Alerts → Create alert
- Threshold: €10/mes
- Notify: vuestro email
```

**4. Habilitar features útiles:**

```bash
# Verificar si están habilitadas las providers necesarias
az provider show --namespace Microsoft.Web --query registrationState -o tsv
az provider show --namespace Microsoft.Storage --query registrationState -o tsv
az provider show --namespace Microsoft.DocumentDB --query registrationState -o tsv

# Si alguna sale NotRegistered:
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.DocumentDB
```

### 8.2 GitHub: setup inicial

```bash
# 1. Configurar identidad git
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"

# 2. Generar SSH key (recomendado vs HTTPS)
ssh-keygen -t ed25519 -C "tu@email.com"

# 3. Añadir SSH key a GitHub
cat ~/.ssh/id_ed25519.pub
# Copiar el output → GitHub Settings → SSH and GPG keys → New SSH key

# 4. Test SSH
ssh -T git@github.com
# Esperado: "Hi <username>! You've successfully authenticated"

# 5. Login con gh CLI
gh auth login
# → seleccionar GitHub.com
# → seleccionar SSH
# → autenticar

# 6. Verificar
gh auth status
```

### 8.3 VS Code: settings recomendados

Crear archivo `~/.config/Code/User/settings.json` (o equivalente):

```json
{
  // Editor
  "editor.fontSize": 14,
  "editor.tabSize": 4,
  "editor.formatOnSave": true,
  "editor.rulers": [120],
  
  // Terminal
  "terminal.integrated.fontSize": 13,
  "terminal.integrated.defaultProfile.osx": "zsh",
  "terminal.integrated.defaultProfile.linux": "bash",
  "terminal.integrated.defaultProfile.windows": "PowerShell",
  
  // Git
  "git.autofetch": true,
  "git.confirmSync": false,
  
  // C# Dev Kit
  "dotnet.defaultSolution": "auto",
  "dotnet.codeLens.enableReferencesCodeLens": true,
  
  // Azure
  "azureFunctions.deploySubpath": "publish",
  "azureFunctions.scmDoBuildDuringDeployment": true,
  "azureFunctions.pythonVenv": ".venv",
  
  // Files
  "files.autoSave": "afterDelay",
  "files.autoSaveDelay": 1000,
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/.vs": true
  }
}
```

---

## 9. Verificación: smoke test del entorno

Una vez instalado todo, ejecutad este script para validar:

```bash
#!/bin/bash
# save as: verify-environment.sh
# Run: chmod +x verify-environment.sh && ./verify-environment.sh

echo "🔍 Verificación del entorno del curso AZ-204"
echo ""

PASS=0
FAIL=0

check() {
  local label="$1"
  local cmd="$2"
  local expected="$3"
  
  echo -n "  $label... "
  
  if eval "$cmd" 2>&1 | grep -qE "$expected"; then
    echo "✓"
    PASS=$((PASS+1))
  else
    echo "✗ (esperado: $expected)"
    FAIL=$((FAIL+1))
  fi
}

echo "Runtime:"
check ".NET 8 SDK"        "dotnet --version"  "^8\."
check "Node.js 18+"       "node --version"    "^v(1[8-9]|[2-9][0-9])\."

echo ""
echo "Azure tooling:"
check "Azure CLI"         "az --version | head -1"  "azure-cli\s+2\.(6[5-9]|[7-9][0-9])"
check "Azure Functions Core Tools"  "func --version"  "^4\."
check "Azurite (npm)"     "npm list -g azurite 2>&1"  "azurite@"

echo ""
echo "Editor / Git:"
check "VS Code"           "code --version | head -1"  "^[0-9]"
check "Git"               "git --version"  "git version"

echo ""
echo "Tools de soporte:"
check "curl"              "curl --version | head -1"  "curl"
check "jq"                "jq --version"  "jq-1\."
check "GitHub CLI"        "gh --version | head -1"   "^gh"

echo ""
echo "Cuentas y login:"
check "Azure logged in"   "az account show -o tsv 2>&1"  "."
check "GitHub logged in"  "gh auth status 2>&1"  "Logged in"

echo ""
echo "Opcional:"
check "Docker"            "docker --version"  "Docker version"
check "Claude Code"       "claude --version 2>&1"  "^[0-9]"
check "PowerShell 7"      "pwsh --version"  "^PowerShell 7"

echo ""
echo "─────────────────────────────────────────"
echo "✅ Pasados: $PASS"
echo "❌ Fallidos: $FAIL"
echo ""

if [ $FAIL -eq 0 ]; then
  echo "🎉 Entorno listo para el curso"
else
  echo "⚠️  Revisad los items marcados con ✗ antes de empezar"
fi
```

**Output esperado:**

```
🔍 Verificación del entorno del curso AZ-204

Runtime:
  .NET 8 SDK... ✓
  Node.js 18+... ✓

Azure tooling:
  Azure CLI... ✓
  Azure Functions Core Tools... ✓
  Azurite (npm)... ✓

Editor / Git:
  VS Code... ✓
  Git... ✓

Tools de soporte:
  curl... ✓
  jq... ✓
  GitHub CLI... ✓

Cuentas y login:
  Azure logged in... ✓
  GitHub logged in... ✓

Opcional:
  Docker... ✓
  Claude Code... ✓
  PowerShell 7... ✓

─────────────────────────────────────────
✅ Pasados: 13
❌ Fallidos: 0

🎉 Entorno listo para el curso
```

---

## 10. Software por módulo

### Mapa de qué necesitas para cada módulo

| Módulo | Tema | Software adicional necesario |
|---|---|---|
| **M01** | Intro Azure | Solo el básico (CLI, .NET, VS Code) |
| **M02** | App Services | Solo el básico |
| **M03** | Functions I | + Functions Core Tools + Azurite |
| **M04** | Functions II (Durable) | + Azurite (obligatorio) |
| **M05** | Storage + BBDD | + Azurite. Cosmos Emulator opcional |
| **M06** | Seguridad + Auth | Solo el básico (cuenta Microsoft Entra incluida en Azure) |
| **M07** | Integración + MSIX | **+ Visual Studio 2022 (Windows)** + Workload MSIX Tools |
| **M08** | DevOps | + GitHub CLI + cuenta GitHub |
| **M09** | IA + Claude Code | **+ Claude Code CLI** + Plan Anthropic |
| **M10** | Proyecto Integrador | Lo del resto del curso (es integrador) |
| **M11** | Bonus Claude + Azure | + Claude Code + cuenta Pro/API key |

### Si solo te interesan ciertos módulos

**Quiero hacer M01-M06 (parte 1):**
- Suficiente con: .NET 8 + Azure CLI + VS Code + Git + curl + jq
- Tiempo setup: 30-45 min

**Quiero también M07 (MSIX):**
- Añadir Visual Studio 2022 Community en Windows
- Tiempo setup: +60 min

**Quiero M08 (DevOps):**
- Añadir GitHub CLI
- Tiempo setup: +5 min

**Quiero M09 + M11 (Claude Code):**
- Añadir Claude Code CLI + cuenta Anthropic
- Tiempo setup: +10 min

---

## 11. Costes estimados del curso

### Coste del software (todo gratis)

```
.NET 8 SDK:                €0
Azure CLI:                 €0
Functions Core Tools:      €0
VS Code:                   €0
Visual Studio Community:   €0 (gratis para uso personal)
Git:                       €0
GitHub Free:               €0
Azurite:                   €0
Docker Desktop:            €0 (uso personal)
─────────────────────────────
Total software:            €0
```

### Coste de cuentas y servicios

```
Azure Free Trial:          €0 (200$ crédito 30 días)
Cuenta GitHub:             €0
Microsoft Entra:           €0 (incluida en Azure)
─────────────────────────────
Total cuentas:             €0
```

### Coste de uso de Azure durante el curso

**Si haces solo prácticas P2 (las nuevas, simples):**

```
Web App F1: GRATIS
Storage Account: ~€0.05/mes
Function Apps Consumption: GRATIS (1M ejecs/mes)
Cosmos DB: GRATIS (free tier)

Total: ~€1-3 si no se olvidan recursos
```

**Si haces también las prácticas principales (P):**

```
App Service Plan S1 (M02 slots): ~€70/mes (€2.30/día)
Cosmos DB provisioned: ~€20/mes
APIM Consumption: ~€3/mes
─────────────────────────────
Total: ~€10-20 si haces el curso en 2-3 semanas y limpias bien
```

**Si te olvidas de limpiar todo:**

```
~€100-300/mes
→ POR ESO el curso enfatiza cleanup en CADA práctica
```

### Coste opcional Claude Code

```
Plan Free: €0 (limitado, ~10 sesiones)
Plan Pro: €20/mes (recomendado durante M09 + M11)
API key: ~€0.05-0.30 por sesión

Total recomendado: €20 (1 mes Pro durante M09+M11)
```

### Total estimado del curso

```
Software:                      €0
Cuentas:                       €0
Azure (con cleanup correcto):  €5-20
Claude Code (opcional):        €20
─────────────────────────────────
TOTAL CURSO:                   €5-40
```

---

## 12. Solución de problemas comunes

### "az: command not found"

```bash
# Verificar instalación
which az
# Si vacío, reinstalar Azure CLI

# Mac: añadir al PATH si lo instalaste manualmente
echo 'export PATH=/usr/local/bin:$PATH' >> ~/.zshrc
source ~/.zshrc
```

### "dotnet: command not found"

```bash
# Verificar
which dotnet

# Si no aparece, añadir al PATH
# Mac/Linux:
echo 'export PATH=$PATH:/usr/local/share/dotnet' >> ~/.zshrc
source ~/.zshrc

# Windows: reiniciar PowerShell tras instalación
```

### "func: command not found"

```bash
# Si instalaste vía npm, verificar npm global path
npm config get prefix
# Asegurar que <prefix>/bin está en PATH

# Reinstalar:
npm uninstall -g azure-functions-core-tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### "EACCES permission denied" al instalar npm globals

```bash
# Mac/Linux: usar npm con prefix de usuario (no requiere sudo)
mkdir -p ~/.npm-global
npm config set prefix ~/.npm-global
echo 'export PATH=~/.npm-global/bin:$PATH' >> ~/.zshrc
source ~/.zshrc

# Reintentar instalación
npm install -g azurite @anthropic-ai/claude-code
```

### "Login failed" en Azure CLI

```bash
# Limpiar caché
az logout
az cache purge

# Login con browser específico
az login --use-device-code

# Si hay MFA loop, usar service principal en lugar de user account
```

### Functions Core Tools no encuentra .NET 8

```bash
# Verificar versiones
dotnet --list-sdks
# Si .NET 8 no aparece, reinstalar SDK

# Si Functions sigue sin verlo:
export DOTNET_ROOT=$(dirname $(which dotnet))
```

### "Docker daemon is not running" en Mac

```bash
# Abrir Docker Desktop manualmente
open -a Docker

# Esperar al icono ✅ en la barra de menú
docker ps
# Debe ejecutar sin error
```

### "Azurite cannot bind to port 10000"

```bash
# Otro proceso usando el puerto
lsof -i :10000
kill <PID>

# O cambiar el puerto de Azurite
azurite --blobPort 10010 --queuePort 10011 --tablePort 10012
```

---

## 13. Recursos adicionales

### Documentación oficial

- **.NET 8:** https://learn.microsoft.com/dotnet/
- **Azure:** https://learn.microsoft.com/azure/
- **Azure CLI:** https://learn.microsoft.com/cli/azure/
- **Azure Functions:** https://learn.microsoft.com/azure/azure-functions/
- **Claude Code:** https://docs.claude.com/en/docs/claude-code
- **GitHub Actions:** https://docs.github.com/actions

### Cheatsheets recomendados

- **Azure CLI cheatsheet:** https://learn.microsoft.com/cli/azure/azure-cli-reference-for-az
- **dotnet CLI cheatsheet:** https://learn.microsoft.com/dotnet/core/tools/
- **Git cheatsheet:** https://education.github.com/git-cheat-sheet-education.pdf

### Comunidad

- **Stack Overflow:** [tag: azure], [tag: .net-8.0]
- **Reddit:** r/AZURE, r/dotnet
- **Discord Anthropic:** https://www.anthropic.com/discord (Claude Code)
- **Discord .NET:** https://aka.ms/dotnet-discord

---

## 14. Preparación para el primer día

**Checklist final antes del primer día de curso:**

```
☐ .NET 8 SDK instalado (dotnet --version → 8.0.x)
☐ Azure CLI instalado y logueado (az account show OK)
☐ Functions Core Tools instalado (func --version → 4.x)
☐ VS Code con todas las extensiones del curso
☐ Cuenta Azure activa con crédito disponible
☐ Cuenta GitHub creada y SSH key añadida
☐ Git configurado (user.name, user.email)
☐ curl + jq funcionando
☐ Script verify-environment.sh pasando 100%
☐ Cap de gasto Azure configurado (€10/mes)
☐ Para M07: Visual Studio 2022 Community en Windows
☐ Para M09/M11: Claude Code instalado + cuenta Pro/API
☐ ~5 GB libre en disco (para proyectos + dependencias)
```

**Si algo falla en el primer día:**

```
1. No bloqueéis al resto del grupo
2. Anotad el error exacto
3. Comparadlo con la sección "Solución de problemas comunes"
4. Si persiste: usad Cloud Shell como fallback (M01-S1.P2)
   → no requiere instalar nada en local
```

**Tiempo estimado de setup completo:** 2-3 horas con buena conexión.

---

> **Nota final:** este documento se actualizará conforme cambien las herramientas o salgan nuevas versiones. La versión más actual está en el repositorio del curso.
> 
> **Cualquier duda:** preguntad en clase, en Slack del curso, o por email. Mejor preguntar antes que llegar al primer día con el entorno roto.

---

**Versión:** 1.0  
**Última actualización:** Abril 2026  
**Curso:** Azure AZ-204 con .NET 8 + Bonus Claude Code  
**Idioma:** Español
