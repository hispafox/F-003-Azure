# Menú interactivo para los scripts de S7.6. SOLO LECTURA.

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.6 - MSIX auto-update (solo lectura)'
    Write-Host '==========================================================='
    Write-Host ' 1) Inspeccionar un .appinstaller remoto (slide 3/7)'
    Write-Host ' 2) Listar versiones MSIX instaladas (slide 12)'
    Write-Host ' 0) Salir'
    Write-Host ''
    Write-Host ' (No actualiza ni instala -> sin cleanup)'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' {
            $url = Read-Host 'URL del .appinstaller'
            & "$PSScriptRoot\01-inspect-appinstaller.ps1" -Url $url
        }
        '2' {
            $name = Read-Host 'Identity.Name (ej. MiEmpresa.VentasDesktop)'
            & "$PSScriptRoot\02-installed-versions.ps1" -IdentityName $name
        }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
