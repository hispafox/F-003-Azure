# 01 - Valida un Package.appxmanifest contra las reglas de la slide 3:
# Identity.Name con formato Empresa.NombreApp, Publisher con CN=,
# Version Major.Minor.Build.Revision, TargetDeviceFamily MinVersion
# ≥ 10.0.17763.0. SOLO LECTURA.

param(
    [Parameter(Mandatory=$true)]
    [string]$ManifestPath
)

. (Join-Path $PSScriptRoot '_lib.ps1')

if (-not (Test-Path $ManifestPath)) {
    Warn "No existe: $ManifestPath"
    exit 1
}

Step "Manifest: $ManifestPath (slide 3)"
[xml]$xml = Get-Content $ManifestPath -Raw

$identity = $xml.Package.Identity
$problems = @()

if (-not ($identity.Name -match '^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$')) {
    $problems += "Identity.Name '$($identity.Name)' no es 'Empresa.NombreApp'"
}
if (-not $identity.Publisher.StartsWith('CN=')) {
    $problems += "Publisher '$($identity.Publisher)' no empieza por 'CN='"
}
if (-not ($identity.Version -match '^\d+\.\d+\.\d+\.\d+$')) {
    $problems += "Version '$($identity.Version)' no es Major.Minor.Build.Revision"
}

$tdf = $xml.Package.Dependencies.TargetDeviceFamily
$minVer = [Version]$tdf.MinVersion
if ($minVer -lt [Version]'10.0.17763.0') {
    $problems += "TargetDeviceFamily MinVersion '$minVer' < 10.0.17763.0 (Windows 10 1809)"
}

[pscustomobject]@{
    IdentityName = $identity.Name
    Publisher    = $identity.Publisher
    Version      = $identity.Version
    Architecture = $identity.ProcessorArchitecture
    MinVersion   = $tdf.MinVersion
} | Format-List

if ($problems.Count -eq 0) {
    Ok "Manifest válido (slide 3)"
} else {
    foreach ($p in $problems) { Warn $p }
    Write-Host "Reconstruir el manifest en VM limpia tras corregir (slide 28.5)."
}
