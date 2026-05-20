# Menú interactivo para los scripts de S7.7. SOLO LECTURA.

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.7 - Migración ClickOnce → MSIX (solo lectura)'
    Write-Host '==========================================================='
    Write-Host ' 1) Verificar migración en este PC (slide 17)'
    Write-Host ' 0) Salir'
    Write-Host ''
    Write-Host ' (No instala ni desinstala -> sin cleanup)'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' {
            $name = Read-Host 'Identity.Name (ej. MiEmpresa.VentasDesktop)'
            $exe  = Read-Host 'Exe ClickOnce a buscar (vacio = *.application)'
            if ([string]::IsNullOrWhiteSpace($exe)) {
                & "$PSScriptRoot\01-verify-migration.ps1" -IdentityName $name
            } else {
                & "$PSScriptRoot\01-verify-migration.ps1" `
                    -IdentityName $name -ClickOnceExeName $exe
            }
        }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
