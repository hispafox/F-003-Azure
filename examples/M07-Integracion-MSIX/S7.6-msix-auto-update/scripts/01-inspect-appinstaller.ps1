# 01 - Descarga e inspecciona un .appinstaller remoto (slide 3/7).
# Extrae versión, MainPackage URI y UpdateSettings. SOLO LECTURA.

param(
    [Parameter(Mandatory=$true)]
    [string]$Url
)

. (Join-Path $PSScriptRoot '_lib.ps1')

Step ".appinstaller: $Url"
try {
    $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing
} catch {
    Warn "No se pudo descargar: $($_.Exception.Message)"
    exit 1
}

[xml]$xml = $resp.Content
$ai = $xml.AppInstaller
$mp = $ai.MainPackage
$us = $ai.UpdateSettings

[pscustomobject]@{
    AppInstallerVersion = $ai.Version
    MainPackageName     = $mp.Name
    MainPackageVersion  = $mp.Version
    Architecture        = $mp.ProcessorArchitecture
    PackageUri          = $mp.Uri
    Publisher           = $mp.Publisher
} | Format-List

if ($us) {
    Step 'UpdateSettings (slide 3/13)'
    $onLaunch = $us.OnLaunch
    [pscustomobject]@{
        HoursBetweenChecks      = $onLaunch.HoursBetweenUpdateChecks
        ShowPrompt              = $onLaunch.ShowPrompt
        UpdateBlocksActivation  = $onLaunch.UpdateBlocksActivation
        AutomaticBackgroundTask = [bool]$us.AutomaticBackgroundTask
        ForceUpdateFromAnyVer   = $us.ForceUpdateFromAnyVersion
    } | Format-List

    if ($onLaunch.UpdateBlocksActivation -eq 'true') {
        Warn 'UpdateBlocksActivation=true: la app NO abre hasta actualizar (slide 13). Solo para releases críticas.'
    }
} else {
    Warn 'Sin <UpdateSettings>: el .appinstaller no auto-actualiza.'
}

Ok 'Inspección completada'
