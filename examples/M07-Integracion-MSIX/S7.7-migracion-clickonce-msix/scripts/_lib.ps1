# Utilidades compartidas. Scripts SOLO LECTURA: verifican el estado de
# migración en este PC (slide 17). No instalan ni desinstalan nada.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" -ForegroundColor Green }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }
