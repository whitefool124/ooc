param([string]$ProjectRoot = "UnityProject")

$ErrorActionPreference = "Stop"
$magick = (Get-Command magick -ErrorAction Stop).Source
$assetDir = Join-Path $ProjectRoot "Assets/Game/Resources/Art/FormalVfx32/fire_smoke"
$qaDir = Join-Path $ProjectRoot "Assets/Game/QA/FireSpells/VFX"
New-Item -ItemType Directory -Force -Path $assetDir, $qaDir | Out-Null

$frames = @(
    @("rectangle 14,25 18,28", "rectangle 11,22 14,25", "rectangle 18,21 21,24"),
    @("rectangle 13,23 18,27", "rectangle 9,20 14,24", "rectangle 18,18 23,23"),
    @("rectangle 12,21 17,25", "rectangle 7,18 13,22", "rectangle 17,15 24,21"),
    @("rectangle 11,19 16,23", "rectangle 6,15 12,20", "rectangle 16,12 23,18"),
    @("rectangle 10,17 15,21", "rectangle 5,12 11,17", "rectangle 15,9 22,15"),
    @("rectangle 9,15 14,19", "rectangle 4,9 10,14", "rectangle 14,6 21,12")
)

for ($i = 0; $i -lt $frames.Count; $i++) {
    $name = "frame_{0:D2}.png" -f $i
    $path = Join-Path $assetDir $name
    $draw = $frames[$i] -join " "
    & $magick -size 32x32 xc:none -alpha on -channel RGBA -fill "#5A6365FF" -draw $draw -fill "#879092FF" -draw "rectangle $((12-$i)), $((24-$i*2)) $((15-$i)), $((26-$i*2))" -fill "#B7BDB8FF" -draw "rectangle $((17-$i)), $((19-$i*2)) $((19-$i)), $((21-$i*2))" -define png:color-type=6 $path
}

$framePaths = 0..5 | ForEach-Object { Join-Path $assetDir ("frame_{0:D2}.png" -f $_) }
& $magick @framePaths +append (Join-Path $qaDir "fire_smoke_strip.png")
& $magick montage @framePaths -tile 3x2 -geometry 32x32+4+4 -background "#101619" (Join-Path $qaDir "fire_smoke_contact_sheet.png")
& $magick -delay 10 -dispose background -loop 0 @framePaths (Join-Path $qaDir "fire_smoke.gif")

$checks = foreach ($path in $framePaths) {
    $dimensions = (& $magick identify -format "%wx%h" $path)
    $colors = [int](& $magick identify -format "%k" $path)
    $alphaLevels = [int](& $magick $path -alpha extract -format "%k" info:)
    [ordered]@{
        file = [IO.Path]::GetFileName($path)
        width = [int]($dimensions -split "x")[0]
        height = [int]($dimensions -split "x")[1]
        palette_colors = $colors
        alpha_levels = $alphaLevels
        pass = ($dimensions -eq "32x32" -and $colors -le 8 -and $alphaLevels -le 2)
    }
}
$report = [ordered]@{
    schema_version = "fire-vfx-qa-v0.1"
    effect = "fire_smoke"
    source = "Tools/Art/build_fire_smoke_vfx.ps1"
    requirements = [ordered]@{ dimensions = "32x32"; maximum_palette_colors = 8; maximum_alpha_levels = 2; importer = "Point/Clamp/no mipmap/uncompressed" }
    frames = $checks
    summary = [ordered]@{ total = $checks.Count; passed = @($checks | Where-Object pass).Count; failed = @($checks | Where-Object { -not $_.pass }).Count }
}
$report | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $qaDir "fire_smoke_qa_v01.json")

Write-Output "Generated fire_smoke: 6 frames, strip, contact sheet, GIF."
