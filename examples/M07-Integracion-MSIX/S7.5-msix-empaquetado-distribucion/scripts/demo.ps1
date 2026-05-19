# Menú interactivo para los scripts de S7.5. SOLO LECTURA: valida un
# manifest local + comprueba herramientas. No firma ni instala nada.

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.5 - MSIX empaquetado y distribución (solo lectura)'
    Write-Host '==========================================================='
    Write-Host ' 1) Validar Package.appxmanifest (slide 3) — pide la ruta'
    Write-Host ' 2) Comprobar herramientas (signtool, makeappx, AzureSignTool)'
    Write-Host ' 0) Salir'
    Write-Host ''
    Write-Host ' (No firma ni instala -> sin cleanup)'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' {
            $path = Read-Host 'Ruta al Package.appxmanifest'
            & "$PSScriptRoot\01-validate-manifest.ps1" -ManifestPath $path
        }
        '2' { & "$PSScriptRoot\02-tooling-check.ps1" }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
