param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path)

Add-Type -AssemblyName System.Drawing
Remove-Item Alias:R -Force -ErrorAction SilentlyContinue
$out = Join-Path $ProjectRoot "UnityProject\Assets\Game\Resources\Art\FormalItemIcons32"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$C = @{
    Ink=[Drawing.Color]::FromArgb(255,22,29,31); Steel=[Drawing.Color]::FromArgb(255,72,88,91)
    Light=[Drawing.Color]::FromArgb(255,214,224,218); Cyan=[Drawing.Color]::FromArgb(255,67,190,199)
    Amber=[Drawing.Color]::FromArgb(255,232,166,61); Rust=[Drawing.Color]::FromArgb(255,174,67,52)
}
function Canvas { [Drawing.Bitmap]::new(32,32,[Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function P($b,$x,$y,$c) { if($x -ge 0 -and $x -lt 32 -and $y -ge 0 -and $y -lt 32){$b.SetPixel($x,$y,$c)} }
function DrawRect($b,$x,$y,$w,$h,$c) { for($iy=$y;$iy-lt $y+$h;$iy++){for($ix=$x;$ix-lt $x+$w;$ix++){P $b $ix $iy $c}} }
function L($b,$x0,$y0,$x1,$y1,$c) { $dx=[Math]::Abs($x1-$x0);$sx=if($x0-lt$x1){1}else{-1};$dy=-[Math]::Abs($y1-$y0);$sy=if($y0-lt$y1){1}else{-1};$e=$dx+$dy;while($true){P $b $x0 $y0 $c;if($x0-eq$x1-and$y0-eq$y1){break};$e2=2*$e;if($e2-ge$dy){$e+=$dy;$x0+=$sx};if($e2-le$dx){$e+=$dx;$y0+=$sy}} }
function Save($name,[scriptblock]$draw) { $b=Canvas; & $draw $b; $path=Join-Path $out ($name+".png"); $b.Save($path,[Drawing.Imaging.ImageFormat]::Png); $b.Dispose() }

Save "fire_scroll" { param($b) DrawRect $b 8 5 16 22 $C.Ink; DrawRect $b 10 7 12 18 $C.Light; DrawRect $b 7 4 18 4 $C.Steel; DrawRect $b 7 24 18 4 $C.Steel; L $b 16 10 12 18 $C.Rust; L $b 12 18 17 16 $C.Amber; L $b 17 16 15 22 $C.Rust; P $b 18 12 $C.Amber }
Save "demolition_canister" { param($b) DrawRect $b 7 9 18 16 $C.Ink; DrawRect $b 9 11 14 12 $C.Steel; DrawRect $b 12 5 8 6 $C.Light; DrawRect $b 14 3 4 3 $C.Amber; DrawRect $b 5 14 4 6 $C.Rust; DrawRect $b 23 14 4 6 $C.Rust; DrawRect $b 12 14 8 5 $C.Rust; DrawRect $b 14 15 4 3 $C.Amber; L $b 18 6 24 10 $C.Cyan }
Save "category_weapon" { param($b) L $b 5 25 25 5 $C.Light; L $b 7 27 27 7 $C.Steel; DrawRect $b 4 23 8 5 $C.Rust; DrawRect $b 20 4 8 4 $C.Cyan }
Save "category_armor" { param($b) DrawRect $b 7 6 18 18 $C.Steel; DrawRect $b 10 8 12 13 $C.Ink; L $b 10 21 16 27 $C.Light; L $b 22 21 16 27 $C.Light; DrawRect $b 14 10 4 8 $C.Cyan }
Save "category_consumable" { param($b) DrawRect $b 9 7 14 20 $C.Steel; DrawRect $b 12 4 8 5 $C.Light; DrawRect $b 12 13 8 4 $C.Rust; DrawRect $b 14 11 4 8 $C.Rust }
Save "category_scroll" { param($b) DrawRect $b 8 6 16 20 $C.Light; DrawRect $b 6 5 20 4 $C.Steel; DrawRect $b 6 23 20 4 $C.Steel; L $b 12 12 20 12 $C.Cyan; L $b 12 17 20 17 $C.Amber }
Save "category_artifact" { param($b) DrawRect $b 8 9 16 15 $C.Steel; DrawRect $b 11 12 10 9 $C.Ink; DrawRect $b 14 5 4 6 $C.Amber; DrawRect $b 14 14 4 5 $C.Cyan; DrawRect $b 5 13 4 7 $C.Rust; DrawRect $b 23 13 4 7 $C.Rust }
Save "category_material" { param($b) L $b 6 24 16 5 $C.Steel; L $b 16 5 26 24 $C.Light; L $b 6 24 26 24 $C.Amber; DrawRect $b 13 13 6 7 $C.Cyan }
Save "category_quest" { param($b) DrawRect $b 8 5 16 22 $C.Steel; DrawRect $b 11 8 10 16 $C.Ink; DrawRect $b 14 10 4 8 $C.Amber; DrawRect $b 14 20 4 3 $C.Rust }
Save "category_container" { param($b) DrawRect $b 5 10 22 16 $C.Steel; DrawRect $b 7 13 18 11 $C.Ink; DrawRect $b 12 7 8 5 $C.Light; DrawRect $b 14 15 4 5 $C.Amber }
Save "inventory_search" { param($b) DrawRect $b 6 6 14 14 $C.Steel; DrawRect $b 9 9 8 8 $C.Ink; L $b 18 18 27 27 $C.Light; L $b 19 17 28 26 $C.Cyan }
Save "inventory_filter" { param($b) L $b 5 6 27 6 $C.Light; L $b 5 6 14 17 $C.Cyan; L $b 27 6 18 17 $C.Cyan; DrawRect $b 14 16 4 11 $C.Amber }
Save "inventory_sort" { param($b) L $b 8 7 8 25 $C.Cyan; L $b 5 22 8 25 $C.Cyan; L $b 11 22 8 25 $C.Cyan; L $b 24 25 24 7 $C.Amber; L $b 21 10 24 7 $C.Amber; L $b 27 10 24 7 $C.Amber }
Save "inventory_autoplace" { param($b) DrawRect $b 5 5 22 22 $C.Steel; for($i=8;$i-lt25;$i+=6){L $b $i 6 $i 26 $C.Ink;L $b 6 $i 26 $i $C.Ink}; DrawRect $b 8 8 9 9 $C.Cyan; DrawRect $b 19 19 5 5 $C.Amber }
Save "inventory_quickbar" { param($b) DrawRect $b 5 18 22 8 $C.Steel; for($i=7;$i-lt27;$i+=5){L $b $i 19 $i 25 $C.Ink}; L $b 9 16 22 5 $C.Cyan; L $b 22 5 26 9 $C.Cyan }
Save "inventory_use" { param($b) DrawRect $b 13 4 6 24 $C.Light; DrawRect $b 4 13 24 6 $C.Light; DrawRect $b 15 7 2 18 $C.Cyan; DrawRect $b 7 15 18 2 $C.Cyan }
Save "inventory_salvage" { param($b) L $b 6 7 25 26 $C.Rust; L $b 25 7 6 26 $C.Rust; DrawRect $b 13 4 6 24 $C.Steel; DrawRect $b 4 13 24 6 $C.Steel; DrawRect $b 14 14 4 4 $C.Amber }
Save "inventory_discard" { param($b) DrawRect $b 9 9 14 18 $C.Steel; DrawRect $b 7 7 18 4 $C.Rust; DrawRect $b 12 4 8 4 $C.Light; L $b 12 13 12 23 $C.Ink; L $b 16 13 16 23 $C.Ink; L $b 20 13 20 23 $C.Ink }
Save "inventory_rotate" { param($b) L $b 8 9 13 5 $C.Cyan; L $b 13 5 17 5 $C.Cyan; L $b 8 9 8 14 $C.Cyan; L $b 24 23 19 27 $C.Amber; L $b 19 27 15 27 $C.Amber; L $b 24 23 24 18 $C.Amber; DrawRect $b 11 10 12 12 $C.Steel; DrawRect $b 14 13 6 6 $C.Ink }
Save "inventory_clear" { param($b) L $b 7 7 25 25 $C.Rust; L $b 25 7 7 25 $C.Rust; DrawRect $b 13 4 6 24 $C.Steel; DrawRect $b 4 13 24 6 $C.Steel; DrawRect $b 14 14 4 4 $C.Light }
Save "inventory_weight" { param($b) DrawRect $b 7 12 18 14 $C.Steel; DrawRect $b 10 9 12 5 $C.Light; DrawRect $b 13 5 6 6 $C.Steel; DrawRect $b 10 15 12 8 $C.Ink; L $b 16 16 12 21 $C.Amber; L $b 16 16 20 21 $C.Amber }
Save "loot_unknown" { param($b) DrawRect $b 5 11 22 15 $C.Steel; DrawRect $b 7 13 18 11 $C.Ink; DrawRect $b 11 7 10 6 $C.Light; DrawRect $b 14 14 4 5 $C.Amber; DrawRect $b 15 20 2 2 $C.Amber; DrawRect $b 14 22 4 2 $C.Amber }
Save "loot_searching" { param($b) DrawRect $b 4 12 19 14 $C.Steel; DrawRect $b 6 14 15 10 $C.Ink; DrawRect $b 9 8 8 6 $C.Light; DrawRect $b 22 5 5 5 $C.Cyan; DrawRect $b 24 10 3 3 $C.Cyan; DrawRect $b 25 15 2 2 $C.Amber; DrawRect $b 10 17 7 4 $C.Amber }
Save "loot_empty" { param($b) DrawRect $b 5 13 22 13 $C.Steel; DrawRect $b 7 15 18 9 $C.Ink; L $b 7 11 12 6 $C.Light; L $b 12 6 24 10 $C.Light; L $b 24 10 25 13 $C.Light; L $b 10 18 22 18 $C.Rust }

Get-ChildItem $out -Filter "*.png" | Where-Object { $_.BaseName -in @("fire_scroll","demolition_canister","category_weapon","category_armor","category_consumable","category_scroll","category_artifact","category_material","category_quest","category_container","inventory_search","inventory_filter","inventory_sort","inventory_autoplace","inventory_quickbar","inventory_use","inventory_salvage","inventory_discard","inventory_rotate","inventory_clear","inventory_weight","loot_unknown","loot_searching","loot_empty") } | Select-Object Name,Length

