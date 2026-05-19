# 02 - Inventaría las apps ClickOnce instaladas en este PC (slide 3).
# Aparecen en %LocalAppData%\Apps\2.0\. SOLO LECTURA.

. (Join-Path $PSScriptRoot '_lib.ps1')

$root = Get-ClickOnceRoot
Step "ClickOnce: ubicación = $root (slide 3/9 — %LocalAppData%\Apps\)"

if (-not (Test-Path $root)) {
    Warn "No hay carpeta ClickOnce: este usuario no tiene apps ClickOnce instaladas."
    exit 0
}

$manifests = Get-ChildItem $root -Recurse -Filter '*.application' -File `
    -ErrorAction SilentlyContinue

if (-not $manifests) {
    Ok "Sin manifiestos .application: este PC no tiene apps ClickOnce activas."
    Write-Host "Buena noticia para la migración (slide 18)."
    exit 0
}

$manifests |
    Select-Object @{Name='App'; Expression={ $_.BaseName }},
        @{Name='Manifest'; Expression={ $_.FullName.Substring($root.Length + 1) }},
        @{Name='Modificado'; Expression={ $_.LastWriteTime.ToString('yyyy-MM-dd') }} |
    Sort-Object App |
    Format-Table -AutoSize

Ok "$($manifests.Count) manifiestos ClickOnce detectados"
Write-Host "Candidatos a migrar a MSIX (slide 12 — escenario A o B)."
