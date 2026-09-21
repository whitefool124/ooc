# OCC bulk pixel-asset fetcher (CC0 / commercial-use, no attribution)
#
# Downloads verified-compliant pixel sources into a staging folder OUTSIDE the
# repo, so the project tree stays clean.
#
# Usage (Windows PowerShell 5.1 or PowerShell 7):
#   .\fetch_cc0_bulk.ps1
#   .\fetch_cc0_bulk.ps1 -Target 'E:\OCC_assets'
#   .\fetch_cc0_bulk.ps1 -Only denzi,dcss
#   .\fetch_cc0_bulk.ps1 -IncludeClone      # adds git clones (~1.1 GB+)
#
# All sources below are CC0 (no attribution, commercial OK, no copyleft).
# itch.io / CraftPix are NOT included: their licences differ per pack and must
# be reviewed by hand before use.

[CmdletBinding()]
param(
    [string]   $Target = 'E:\OCC_assets',
    [string[]] $Only   = @(),
    [switch]   $IncludeClone
)

$ErrorActionPreference = 'Continue'
$ProgressPreference    = 'SilentlyContinue'
$UA = @{ 'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' }

function Want($name) { ($Only.Count -eq 0) -or ($Only -contains $name) }

function Get-File($name, $url, $outDir) {
    $dest = Join-Path $outDir $name
    if (Test-Path $dest) { Write-Host "  [skip] $name already present"; return }
    try {
        Invoke-WebRequest -Uri $url -OutFile $dest -TimeoutSec 600 -Headers $UA
        $mb = [math]::Round((Get-Item $dest).Length / 1MB, 2)
        Write-Host "  [ok]   $name  ($mb MB)"
    } catch {
        Write-Host "  [FAIL] $name : $($_.Exception.Message)"
    }
}

function Expand-Zip($zipPath, $destDir) {
    if (-not (Test-Path $zipPath)) { return }
    if (Test-Path $destDir) { Write-Host "  [skip] already extracted: $(Split-Path $destDir -Leaf)"; return }
    try {
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        Expand-Archive -Path $zipPath -DestinationPath $destDir -Force
        $n = (Get-ChildItem $destDir -Recurse -File).Count
        Write-Host "  [ok]   extracted -> $destDir  ($n files)"
    } catch { Write-Host "  [FAIL] extract $zipPath : $($_.Exception.Message)" }
}

function Get-ZipLinks($pageUrl) {
    try {
        $h = (Invoke-WebRequest -Uri $pageUrl -UseBasicParsing -TimeoutSec 90 -Headers $UA).Content
        return [regex]::Matches($h, 'href="(https://opengameart\.org/sites/default/files/[^"]+\.(?:zip|7z))"') |
               ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    } catch { Write-Host "  [warn] page read failed: $pageUrl"; return @() }
}

New-Item -ItemType Directory -Force -Path $Target | Out-Null
$zipDir = Join-Path $Target '_archives'
New-Item -ItemType Directory -Force -Path $zipDir | Out-Null

Write-Host "Target: $Target"
Write-Host ""

# ---------- Tier A: small, dense, high-value CC0 sets ----------

if (Want 'denzi') {
    Write-Host '[1/4] DENZI public domain art (measured: 1361 PNG = 1248 x 32x32 + 113 x 32x48)'
    Get-File 'DENZI_CC0_individual_organized_tiles_sprites.zip' `
        'https://opengameart.org/sites/default/files/DENZI_CC0_individual_organized_tiles_sprites.zip' $zipDir
    Get-File 'DENZI_CC0_0.zip' `
        'https://opengameart.org/sites/default/files/DENZI_CC0_0.zip' $zipDir
    Expand-Zip (Join-Path $zipDir 'DENZI_CC0_individual_organized_tiles_sprites.zip') (Join-Path $Target 'DENZI_individual')
    Expand-Zip (Join-Path $zipDir 'DENZI_CC0_0.zip') (Join-Path $Target 'DENZI_sheets')
}

if (Want 'dcss') {
    Write-Host '[2/4] Dungeon Crawl Stone Soup 32x32 (CC0)'
    # Take the OGA zip only. The GitHub crawl/tiles master branch contains about
    # 1230 files with unclear provenance (see TILES_UNDER_UNKNOWN_LICENSE.md).
    $links = Get-ZipLinks 'https://opengameart.org/content/dungeon-crawl-32x32-tiles'
    if (-not $links) { Write-Host '  [warn] no archive link on DCSS page; download manually' }
    foreach ($l in $links) { Get-File (Split-Path $l -Leaf) $l $zipDir }

    $links2 = Get-ZipLinks 'https://opengameart.org/content/dungeon-crawl-32x32-tiles-supplemental'
    foreach ($l in $links2) { Get-File ('supplemental_' + (Split-Path $l -Leaf)) $l $zipDir }

    Get-ChildItem $zipDir -Filter '*.zip' | Where-Object { $_.Name -notmatch 'DENZI' } | ForEach-Object {
        Expand-Zip $_.FullName (Join-Path $Target ('DCSS_' + $_.BaseName))
    }
}

if (Want 'kenney') {
    Write-Host '[3/4] Kenney pixel packs'
    # NOTE (measured 2026-09-17): kenney.nl listing pages AND pack pages are
    # JS-rendered shells when fetched over plain HTTP -- no pack slugs and no
    # .zip href are present in the raw HTML. Scripted bulk download from the
    # site does NOT work. Use one of these instead:
    #   a) GitHub CC0 mirror:  git clone --depth 1 https://github.com/iwenzhou/kenney
    #   b) Paid all-in-one:    https://kenney.itch.io/kenney-game-assets  (60,000+ assets)
    #   c) Manual per-pack download from https://kenney.nl/assets?q=pixel
    Write-Host '  [skip] kenney.nl is JS-gated for scripting -- use the GitHub mirror or manual download.'
    Write-Host '         mirror: git clone --depth 1 https://github.com/iwenzhou/kenney'
}

if (Want 'oga_cc0') {
    Write-Host '[4/4] OpenGameArt CC0 2D Art index (list only, no auto-download)'
    $out = Join-Path $Target 'oga_cc0_index.txt'
    "OpenGameArt CC0 2D Art listing (licence tid=4). Paged, de-duplicated by slug." |
        Set-Content $out -Encoding UTF8
    $seen = New-Object 'System.Collections.Generic.HashSet[string]'
    for ($p = 0; $p -lt 10; $p++) {
        $u = "https://opengameart.org/art-search-advanced?keys=&title=&field_art_tags_tid_op=or&field_art_tags_tid=&name=&field_art_type_tid%5B%5D=9&field_art_licenses_tid%5B%5D=4&sort_by=count&sort_order=DESC&page=$p"
        try {
            $h = (Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 120 -Headers $UA).Content
            $s = [regex]::Matches($h, 'href="/content/([a-z0-9\-]+)"') | ForEach-Object { $_.Groups[1].Value }
            foreach ($x in $s) { if ($seen.Add($x)) { "https://opengameart.org/content/$x" | Add-Content $out -Encoding UTF8 } }
            Write-Host "  page $p -> running total $($seen.Count)"
        } catch { Write-Host "  [warn] page $p failed"; break }
        Start-Sleep -Seconds 1
    }
    Write-Host "  [ok]   index -> $out  ($($seen.Count) entries)"
}

# ---------- Tier B: bulk git clones (opt-in) ----------

if ($IncludeClone) {
    Write-Host ''
    Write-Host '[Tier B] git clone of large CC0 repositories'
    $repos = @(
        @{ n = 'Tiddybub_2d-assets';   u = 'https://github.com/Tiddybub/2d-assets.git';                note = '1101 packs / ~1.09 GB, 100% CC0' },
        @{ n = 'Papyszoo_CC0-Sprites'; u = 'https://github.com/Papyszoo/CC0-Public-Domain-Sprites.git'; note = '172 MB, CC0' },
        @{ n = 'iwenzhou_kenney';      u = 'https://github.com/iwenzhou/kenney.git';                    note = '21 MB, CC0 Kenney mirror' },
        @{ n = 'doficia_cordon';       u = 'https://github.com/doficia/project-cordon-sprites.git';     note = 'CC0' }
    )
    foreach ($r in $repos) {
        $d = Join-Path $Target $r.n
        if (Test-Path $d) { Write-Host "  [skip] $($r.n) already present"; continue }
        Write-Host "  [git]  $($r.n) - $($r.note)"
        & git clone --depth 1 $r.u $d 2>&1 | Select-Object -Last 3 | ForEach-Object { "         $_" }
    }
} else {
    Write-Host ''
    Write-Host '[Tier B] skipped git clones. Add -IncludeClone to pull Tiddybub/2d-assets (1101 CC0 packs / ~1.09 GB) and others.'
}

Write-Host ''
Write-Host "Done. Staging folder: $Target"
Write-Host 'Note: CC0 sources only. itch.io / CraftPix / Japanese sources have different terms and need per-pack review.'
