# 02 - Comprueba que las herramientas de empaquetado MSIX están en
# este PC (signtool, makeappx, AzureSignTool). SOLO LECTURA.

. (Join-Path $PSScriptRoot '_lib.ps1')

Step "signtool (Windows SDK) — necesario para firmar (slide 5)"
$signtool = Find-SignTool
if ($signtool) { Ok "signtool: $signtool" }
else { Warn "signtool.exe no encontrado en C:\Program Files (x86)\Windows Kits\10\bin\" }

Step "makeappx (Windows SDK) — bundles multi-arch (slide 10)"
$makeappx = Get-ChildItem -Path 'C:\Program Files (x86)\Windows Kits\10\bin' `
    -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue |
    Where-Object FullName -Like '*x64*' |
    Select-Object -First 1
if ($makeappx) { Ok "makeappx: $($makeappx.FullName)" }
else { Warn "makeappx.exe no encontrado" }

Step "AzureSignTool (dotnet tool) — firma desde Key Vault (slide 6)"
$ast = (Get-Command AzureSignTool -ErrorAction SilentlyContinue)
if ($ast) { Ok "AzureSignTool: $($ast.Source)" }
else { Warn "AzureSignTool no instalado (dotnet tool install --global AzureSignTool)" }

Write-Host ''
Write-Host "Recordatorio: la clave privada vive en Azure Key Vault (slide 6/28);"
Write-Host "el pipeline firma con AzureSignTool — la clave NUNCA sale del KV."
