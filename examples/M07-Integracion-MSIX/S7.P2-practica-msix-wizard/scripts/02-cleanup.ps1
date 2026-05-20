# 02 - Limpia tras la práctica (slide 14): desinstala el paquete y
# quita el cert self-signed de TrustedPeople. INTERACTIVO: pide
# confirmación antes de eliminar.

param(
    [Parameter(Mandatory=$true)]
    [string]$PackageName,
    [string]$CertSubjectContiene = ''
)

. (Join-Path $PSScriptRoot '_lib.ps1')

# 1) Paquete instalado.
Step "Buscando paquete: $PackageName"
$pkg = Get-AppPackage -Name "$PackageName*" -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $pkg) {
    Ok 'No hay paquete instalado con ese nombre. Nada que desinstalar.'
} else {
    Write-Host "  Encontrado: $($pkg.PackageFullName)"
    $resp = Read-Host '¿Desinstalar? (s/N)'
    if ($resp -eq 's') {
        Remove-AppPackage -Package $pkg.PackageFullName
        Ok "Paquete desinstalado: $($pkg.PackageFullName)"
    } else {
        Warn 'Cancelado por el usuario.'
    }
}

# 2) Cert en TrustedPeople (CurrentUser por defecto, slide 14).
if (-not [string]::IsNullOrWhiteSpace($CertSubjectContiene)) {
    Step "Buscando cert con Subject que contenga '$CertSubjectContiene' en TrustedPeople"
    foreach ($store in 'Cert:\CurrentUser\TrustedPeople', 'Cert:\LocalMachine\TrustedPeople') {
        $certs = Get-ChildItem $store -ErrorAction SilentlyContinue |
            Where-Object Subject -Like "*$CertSubjectContiene*"
        if ($certs) {
            Write-Host "  $store"
            $certs | Select-Object Thumbprint, Subject | Format-Table -AutoSize
            $resp = Read-Host "¿Borrar $($certs.Count) cert(s) de $store ? (s/N)"
            if ($resp -eq 's') {
                $certs | ForEach-Object { Remove-Item "$store\$($_.Thumbprint)" -Force }
                Ok 'Borrado.'
            } else {
                Warn 'Cancelado.'
            }
        }
    }
}

Write-Host ''
Write-Host 'Recordatorio: la práctica termina con desinstalación limpia'
Write-Host '(slide 14). Settings → Apps debe NO mostrar la app después.'
