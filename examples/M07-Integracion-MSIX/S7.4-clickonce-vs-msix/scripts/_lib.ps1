# Utilidades compartidas para los scripts de S7.4. Scripts SOLO LECTURA:
# inventarían paquetes AppX/MSIX y apps ClickOnce instaladas en este PC.
# No instalan ni desinstalan nada → no hay cleanup.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }

function Get-ClickOnceRoot {
    Join-Path $env:LOCALAPPDATA 'Apps\2.0'
}
