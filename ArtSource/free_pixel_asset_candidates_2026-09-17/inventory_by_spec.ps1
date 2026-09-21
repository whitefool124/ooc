# OCC pixel-spec inventory
#
# Scans a folder tree, reads every PNG's real dimensions straight from the IHDR
# header (no image library, no decoding), and reports how many assets land on
# each OCC contract size.
#
# Usage:
#   .\inventory_by_spec.ps1
#   .\inventory_by_spec.ps1 -Root 'E:\OCC_assets'
#   .\inventory_by_spec.ps1 -Root 'E:\OCC_assets' -Csv 'E:\OCC_assets\inventory.csv'
#
# OCC contract sizes (see Tools/OCCArt/occ_art_contract_v1.json):
#   16x16 semantic icons / 24x24 material pickups / 32x32 ground + props + icons
#   32x40 directional board edge / 32x48 / 32x64 humanoid unit canvas
#   48x48 map node icons / 64x64 large props / 480x270 UI backdrop

[CmdletBinding()]
param(
    [string] $Root = 'E:\OCC_assets',
    [string] $Csv  = ''
)

if (-not (Test-Path $Root)) { Write-Host "Not found: $Root"; exit 1 }

function Get-PngSize($path) {
    # PNG layout: 8-byte signature | 4-byte length | 'IHDR' | width(4) | height(4)
    # So 24 bytes are enough -- keep the read minimal, it matters at 100k files.
    try {
        $fs = [IO.File]::OpenRead($path)
        $b  = New-Object byte[] 24
        $read = $fs.Read($b, 0, 24)
        $fs.Close()
        if ($read -lt 24) { return $null }
        if ($b[12] -ne 0x49 -or $b[13] -ne 0x48) { return $null }   # 'IH'
        # PNG stores IHDR width/height BIG-endian; assemble by hand to avoid
        # both a byte-array reversal allocation and an endianness bug.
        $w = ($b[16] -shl 24) -bor ($b[17] -shl 16) -bor ($b[18] -shl 8) -bor $b[19]
        $h = ($b[20] -shl 24) -bor ($b[21] -shl 16) -bor ($b[22] -shl 8) -bor $b[23]
        return "$w x $h"
    } catch { return $null }
}

Write-Host "Scanning: $Root"
$pngs = Get-ChildItem $Root -Recurse -File -Include *.png -ErrorAction SilentlyContinue
Write-Host ("PNG files: {0}" -f $pngs.Count)

$dist = @{}
$rows = New-Object System.Collections.Generic.List[object]
foreach ($p in $pngs) {
    $s = Get-PngSize $p.FullName
    if (-not $s) { continue }
    if (-not $dist.ContainsKey($s)) { $dist[$s] = 0 }
    $dist[$s] = $dist[$s] + 1
    if ($Csv) {
        $rows.Add([pscustomobject]@{ Size = $s; Path = $p.FullName.Substring($Root.Length).TrimStart('\') })
    }
}

$specs = [ordered]@{
    '16 x 16'   = 0; '16 x 24' = 0; '24 x 24' = 0
    '32 x 32'   = 0; '32 x 40' = 0; '32 x 48' = 0; '32 x 64' = 0
    '48 x 48'   = 0; '64 x 64' = 0; '480 x 270' = 0
}

Write-Host ''
Write-Host '=== OCC contract sizes ==='
foreach ($k in $specs.Keys) {
    $n = 0; if ($dist.ContainsKey($k)) { $n = $dist[$k] }
    Write-Host ("  {0,-11} {1,8}" -f $k, $n)
}
$hit = 0; foreach ($k in $specs.Keys) { if ($dist.ContainsKey($k)) { $hit += $dist[$k] } }
Write-Host ("  {0,-11} {1,8}" -f 'TOTAL HIT', $hit)

Write-Host ''
Write-Host '=== Top 20 sizes overall ==='
$dist.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 20 |
    ForEach-Object { Write-Host ("  {0,8}  {1}" -f $_.Value, $_.Key) }

if ($Csv) {
    $rows | Sort-Object Size, Path | Export-Csv -Path $Csv -NoTypeInformation -Encoding UTF8
    Write-Host ''
    Write-Host "CSV written: $Csv ($($rows.Count) rows)"
}
