# Utilidades compartidas para la práctica S7.P. SOLO LECTURA:
# verifica el estado del PC del alumno antes de empezar (slide 3) y
# valida la firma del .msix construido (slide 13). No instala nada.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" -ForegroundColor Green }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }
