# 01 - Pre-flight de la práctica (slide 3): SO, sideloading, tooling.
# SOLO LECTURA — no instala nada, sólo informa.

. (Join-Path $PSScriptRoot '_lib.ps1')

Step 'Windows version (≥ 10.0.17763 = Windows 10 1809)'
$v = [System.Environment]::OSVersion.Version
Write-Host "  $($v.Major).$($v.Minor) build $($v.Build)"
if ($v.Major -lt 10 -or ($v.Major -eq 10 -and $v.Build -lt 17763)) {
    Warn 'SO no soportado (necesita Windows 10 1809+ o Windows 11).'
} else {
    Ok 'SO soportado.'
}

Step 'Sideloading (Developer Mode)'
$key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
$dev = (Get-ItemProperty $key -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense
if ($dev -eq 1) {
    Ok 'Developer Mode habilitado.'
} else {
    Warn 'Developer Mode NO habilitado. Settings → For developers → ON.'
}

Step 'Tooling: signtool + makeappx (Windows SDK)'
foreach ($tool in 'signtool', 'makeappx') {
    $found = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' `
        -Recurse -Filter "$tool.exe" -ErrorAction SilentlyContinue |
        Where-Object FullName -Like '*x64*' |
        Select-Object -First 1
    if ($found) {
        Ok "$tool en $($found.FullName)"
    } else {
        Warn "$tool no encontrado en el SDK."
    }
}

Step 'Permisos de administrador (necesarios para Add-AppxPackage en LocalMachine)'
$adm = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole] 'Administrator')
if ($adm) { Ok 'Sesión como administrador.' }
else      { Warn 'Sesión NO admin. Para Cert:\LocalMachine necesitarás elevación.' }

Write-Host ''
Write-Host 'Recordatorio: tiempo estimado real de la práctica = 75-90 min'
Write-Host '(slide 3 — VS Packaging es lento; cert + install + update toman su tiempo).'
