# Utilidades compartidas. Scripts SOLO LECTURA: descargan e inspeccionan
# un .appinstaller remoto + listan versiones MSIX instaladas. No
# instalan ni actualizan nada → no hay cleanup.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }
