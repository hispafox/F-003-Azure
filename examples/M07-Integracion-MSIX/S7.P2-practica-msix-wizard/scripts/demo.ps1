# Menú interactivo para la práctica S7.P2.
# 01 SOLO LECTURA · 02 INTERACTIVO con confirmación (única excepción
# de M07: el cleanup necesita borrar cert + paquete del PC del alumno).

. (Join-Path $PSScriptRoot '_lib.ps1')
Set-Location $PSScriptRoot

while ($true) {
    Write-Host ''
    Write-Host '==========================================================='
    Write-Host ' M07-S7.P2 - Práctica MSIX wizard'
    Write-Host '==========================================================='
    Write-Host ' 1) Comprobar componentes del wizard de VS (slide 3) [solo lectura]'
    Write-Host ' 2) Cleanup tras la práctica (slide 14) [pide confirmación]'
    Write-Host ' 0) Salir'
    Write-Host ''
    $opt = Read-Host 'Opcion'
    switch ($opt) {
        '1' { & "$PSScriptRoot\01-check-vs-components.ps1" }
        '2' {
            $name = Read-Host 'Nombre del paquete (ej. MiPrimeraMSIX.Package)'
            $subj = Read-Host 'Fragmento del Subject del cert (vacío = saltar cert)'
            & "$PSScriptRoot\02-cleanup.ps1" -PackageName $name -CertSubjectContiene $subj
        }
        '0' { exit 0 }
        default { Write-Host 'Opcion no valida' }
    }
}
