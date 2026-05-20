# 01 - Verifica el estado de migración ClickOnce → MSIX en este PC
# (slide 17). Comprueba si está la versión MSIX y si queda la ClickOnce.
# SOLO LECTURA — no instala ni desinstala nada.

param(
    [Parameter(Mandatory=$true)]
    [string]$IdentityName,
    [string]$ClickOnceExeName = ''
)

. (Join-Path $PSScriptRoot '_lib.ps1')

# 1) ¿Está MSIX instalado?
Step "¿MSIX instalado? (Identity = $IdentityName)"
if (Get-Command Get-AppxPackage -ErrorAction SilentlyContinue) {
    $msix = Get-AppxPackage -Name "$IdentityName*" -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($msix) {
        Ok "MSIX presente: v$($msix.Version)  arch=$($msix.Architecture)"
    } else {
        Warn 'MSIX NO instalado.'
    }
} else {
    Warn 'Get-AppxPackage no disponible (no es Windows).'
}

# 2) ¿Queda la versión ClickOnce?
Step "ClickOnce residual en %LocalAppData%\Apps\2.0"
$clickOnceRoot = Join-Path $env:LOCALAPPDATA 'Apps\2.0'
if (Test-Path $clickOnceRoot) {
    $filter = if ($ClickOnceExeName) { $ClickOnceExeName } else { '*.application' }
    $found = Get-ChildItem $clickOnceRoot -Recurse -Filter $filter `
        -ErrorAction SilentlyContinue
    if ($found) {
        Warn "Aún hay $($found.Count) elemento(s) ClickOnce. Desinstalar desde Panel de control."
        $found | Select-Object FullName | Format-Table -AutoSize
    } else {
        Ok 'Sin residuos ClickOnce.'
    }
} else {
    Ok 'Sin carpeta ClickOnce en este usuario.'
}

# 3) ¿Marker de migración de datos?
if ($msix) {
    Step 'Marker de migración de datos en LocalState (slide 14)'
    $local = Join-Path $env:LOCALAPPDATA "Packages\$($msix.PackageFamilyName)\LocalState"
    if (Test-Path $local) {
        $marker = Join-Path $local '.clickonce-migrated'
        if (Test-Path $marker) {
            Ok "Marker presente: $marker"
        } else {
            Warn 'Sin .clickonce-migrated → la app aún no ha migrado los datos del usuario.'
        }
    } else {
        Warn "LocalState aún no existe: $local"
    }
}

Write-Host ''
Write-Host 'Recordatorio: regla slide 18 — no apagar ClickOnce hasta'
Write-Host 'que TODOS los usuarios estén en MSIX y haya pasado 1 semana'
Write-Host 'sin incidencias.'
