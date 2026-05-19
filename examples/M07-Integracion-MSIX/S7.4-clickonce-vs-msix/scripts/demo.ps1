# Menú interactivo para los scripts de S7.4. Solo lectura: no instala,
# no desinstala, no toca el sistema.

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.4 - ClickOnce vs MSIX (solo lectura)'
    Write-Host '==========================================================='
    Write-Host ' 1) Inventariar paquetes MSIX/AppX instalados (slide 5/14)'
    Write-Host ' 2) Inventariar apps ClickOnce instaladas (slide 3)'
    Write-Host ' 0) Salir'
    Write-Host ''
    Write-Host ' (No instala ni desinstala nada -> sin cleanup)'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' { & "$PSScriptRoot\01-inventory-msix.ps1" }
        '2' { & "$PSScriptRoot\02-inventory-clickonce.ps1" }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
