# Menú interactivo para la práctica S7.P. SOLO LECTURA.

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.P - Práctica MSIX (solo lectura)'
    Write-Host '==========================================================='
    Write-Host ' 1) Pre-flight: SO, sideloading, tooling (slide 3)'
    Write-Host ' 2) Verificar un .msix: firma + Publisher↔Cert (slide 7/13)'
    Write-Host ' 0) Salir'
    Write-Host ''
    Write-Host ' (No instala ni firma -> sin cleanup)'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' { & "$PSScriptRoot\01-preflight.ps1" }
        '2' {
            $path = Read-Host 'Ruta del .msix'
            & "$PSScriptRoot\02-verify-msix.ps1" -MsixPath $path
        }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
