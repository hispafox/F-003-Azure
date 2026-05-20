# 02 - Lista las versiones instaladas de un Identity.Name (slide 12).
# Útil para ver qué versión tiene la flota antes/después de un rollout.
# SOLO LECTURA.

param(
    [Parameter(Mandatory=$true)]
    [string]$IdentityName
)

. (Join-Path $PSScriptRoot '_lib.ps1')

if (-not (Get-Command Get-AppxPackage -ErrorAction SilentlyContinue)) {
    Warn 'Get-AppxPackage no está disponible (necesita Windows + PowerShell).'
    exit 0
}

Step "Versiones instaladas de '$IdentityName' (slide 12)"
$pkgs = Get-AppxPackage -Name "$IdentityName*" -AllUsers -ErrorAction SilentlyContinue

if (-not $pkgs) {
    Warn "No hay paquetes instalados con Identity.Name '$IdentityName' en este PC."
    exit 0
}

$pkgs |
    Select-Object Name, Version, PublisherId, Architecture, InstallLocation |
    Sort-Object Version |
    Format-Table -AutoSize

Ok "$($pkgs.Count) instalación(es) encontradas"
Write-Host 'Recordatorio: si tras un rollout el % de cada versión no'
Write-Host 'cambia en 48 h, el auto-update falla (slide 12).'
