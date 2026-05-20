# Utilidades compartidas para los scripts de la práctica S7.P2.
# SOLO LECTURA: verifica los componentes que el wizard de VS necesita
# (slide 3) y limpia los artefactos al final (slide 14). No instala.

$ErrorActionPreference = 'Stop'

function Step($msg)  { Write-Host "[>] $msg" }
function Ok($msg)    { Write-Host "[OK] $msg" -ForegroundColor Green }
function Warn($msg)  { Write-Host "[!] $msg" -ForegroundColor Yellow }
