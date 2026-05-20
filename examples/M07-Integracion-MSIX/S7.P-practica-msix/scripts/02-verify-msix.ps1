# 02 - Verifica un .msix construido por el alumno (slide 13): firma
# válida + Subject del cert coincide con el Publisher del manifest.
# SOLO LECTURA.

param(
    [Parameter(Mandatory=$true)]
    [string]$MsixPath
)

. (Join-Path $PSScriptRoot '_lib.ps1')

if (-not (Test-Path $MsixPath)) {
    Warn "No existe: $MsixPath"
    exit 1
}

Step "Verificando .msix: $MsixPath"

# 1) Firma Authenticode.
$sig = Get-AuthenticodeSignature -FilePath $MsixPath
[pscustomobject]@{
    Status      = $sig.Status
    SignerCN    = $sig.SignerCertificate.Subject
    NotBefore   = $sig.SignerCertificate.NotBefore
    NotAfter    = $sig.SignerCertificate.NotAfter
} | Format-List

if ($sig.Status -eq 'Valid') {
    Ok 'Firma válida.'
} else {
    Warn "Firma con estado '$($sig.Status)' — instala el cert en TrustedPeople (slide 9)."
}

# 2) Subject del cert vs Publisher del manifest dentro del .msix.
# El .msix es un .zip; usamos System.IO.Compression para extraer el
# AppxManifest.xml sin instalar nada.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tmp = Join-Path $env:TEMP "msix-verify-$(Get-Random)"
[System.IO.Compression.ZipFile]::ExtractToDirectory($MsixPath, $tmp)

try {
    $manifestPath = Join-Path $tmp 'AppxManifest.xml'
    if (-not (Test-Path $manifestPath)) {
        Warn 'AppxManifest.xml no encontrado en el paquete.'
        return
    }

    [xml]$manifest = Get-Content $manifestPath -Raw
    $publisher = $manifest.Package.Identity.Publisher
    $signerSubject = $sig.SignerCertificate.Subject

    Step 'Slide 7 — Publisher del manifest vs Subject del cert'
    Write-Host "  Publisher: $publisher"
    Write-Host "  Subject:   $signerSubject"

    if ($publisher -ceq $signerSubject) {
        Ok 'Coinciden — Windows aceptará el paquete.'
    } else {
        Warn 'NO coinciden — Windows rechazará "package signature hash validation failed".'
    }
} finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
