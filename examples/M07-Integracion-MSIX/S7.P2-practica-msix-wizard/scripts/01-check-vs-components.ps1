# 01 - Comprueba los componentes del wizard de Visual Studio (slide 3):
# workload .NET desktop + Windows Application Packaging Tools + SDK.
# SOLO LECTURA — no instala nada.

. (Join-Path $PSScriptRoot '_lib.ps1')

$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    Warn 'vswhere.exe no encontrado. Instala Visual Studio 2022 (Community gratis vale).'
    exit 0
}

Step 'Instancias de Visual Studio 2022 detectadas'
& $vswhere -products '*' -version '[17.0,)' -format json |
    ConvertFrom-Json |
    Select-Object displayName, installationVersion, installationPath |
    Format-List

Step 'Componente "Windows Application Packaging Tools" (slide 3)'
$packaging = & $vswhere -products '*' `
    -requires 'Microsoft.VisualStudio.Component.WapProj' `
    -property installationPath
if ($packaging) {
    Ok "Packaging Tools instalado en: $packaging"
} else {
    Warn 'Falta el componente. VS Installer → Modify → Individual components → "Windows Application Packaging Tools".'
}

Step 'Workload ".NET desktop development" (WPF)'
$desktop = & $vswhere -products '*' `
    -requires 'Microsoft.VisualStudio.Workload.ManagedDesktop' `
    -property installationPath
if ($desktop) {
    Ok 'WPF workload instalado.'
} else {
    Warn 'Falta workload ".NET desktop development".'
}

Write-Host ''
Write-Host 'Recordatorio: tiempo estimado del wizard = 30-45 min'
Write-Host '(slide 2). Si esto es la primera vez con MSIX, lee también'
Write-Host 'S7.P (CLI manual) — ahí está el detalle de cada paso.'
