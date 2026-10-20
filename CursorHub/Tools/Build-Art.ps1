# Development only: requires Inkscape on PATH. Assets are already included in releases.
$ErrorActionPreference = 'Stop'
$assets = Join-Path $PSScriptRoot '../Assets'
Get-ChildItem (Join-Path $PSScriptRoot 'Art') -Filter '*.svg' | ForEach-Object {
    $scale = if ($_.BaseName -eq 'frame') { 2 } else { 4 }
    [xml]$svg = Get-Content $_.FullName -Raw
    $width = [int]$svg.svg.width * $scale
    $out = Join-Path $assets ($_.BaseName + '.png')
    & inkscape $_.FullName --export-type=png "--export-filename=$out" "--export-width=$width"
    if ($LASTEXITCODE -ne 0) { throw "Failed to rasterize $($_.Name)" }
}
