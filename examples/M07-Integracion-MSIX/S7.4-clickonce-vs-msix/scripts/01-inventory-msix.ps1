# 01 - Inventaría las apps MSIX/AppX instaladas en este PC (slide 5/14).
# Filtra por Publisher si se pasa el parámetro -PublisherFilter (ej. "CN=MiEmpresa").
# SOLO LECTURA — usa Get-AppxPackage.

param([string]$PublisherFilter = '')

. (Join-Path $PSScriptRoot '_lib.ps1')

Step "Apps MSIX/AppX del usuario (slide 5 — contenedor + sandbox)"

if (-not (Get-Command Get-AppxPackage -ErrorAction SilentlyContinue)) {
    Warn "Get-AppxPackage no está disponible (necesita Windows + PowerShell)."
    exit 0
}

$pkgs = Get-AppxPackage
if ($PublisherFilter) {
    $pkgs = $pkgs | Where-Object Publisher -Like "*$PublisherFilter*"
}

$pkgs |
    Select-Object Name, Version, PublisherId,
        @{Name='Arch'; Expression={ $_.Architecture }},
        @{Name='Tipo'; Expression={
            if ($_.SignatureKind -eq 'Store') { 'Store' }
            elseif ($_.IsFramework) { 'Framework' }
            else { 'Sideloaded' }
        }} |
    Sort-Object Name |
    Format-Table -AutoSize

$total = ($pkgs | Measure-Object).Count
Ok "$total paquetes MSIX/AppX instalados"
Write-Host "Recordatorio: instalación limpia + desinstalación sin residuos (slide 5)."
