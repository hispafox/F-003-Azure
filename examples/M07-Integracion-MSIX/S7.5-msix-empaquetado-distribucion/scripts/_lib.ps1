# Utilidades compartidas. Scripts SOLO LECTURA: validan un manifest
# local + comprueban herramientas (signtool, makeappx). No firman ni
# instalan nada → no hay cleanup.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }

function Find-SignTool {
    $candidates = Get-ChildItem -Path 'C:\Program Files (x86)\Windows Kits\10\bin' `
        -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
        Where-Object FullName -Like '*x64*' |
        Sort-Object FullName -Descending
    if ($candidates) { return $candidates[0].FullName }
    return $null
}
