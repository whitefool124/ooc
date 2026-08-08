param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path)

Add-Type -AssemblyName System.Drawing
$names = @("inventory_rotate","inventory_clear","inventory_weight","loot_unknown","loot_searching","loot_empty")
$source = Join-Path $ProjectRoot "UnityProject\Assets\Game\Resources\Art\FormalItemIcons32"
$artifact = Join-Path $ProjectRoot "UnityProject\Artifacts\Inventory"
New-Item -ItemType Directory -Force -Path $artifact | Out-Null
$rows = @()
foreach ($name in $names) {
    $path = Join-Path $source ($name + ".png")
    $bitmap = [Drawing.Bitmap]::FromFile($path)
    $colors = [Collections.Generic.HashSet[int]]::new()
    $alpha = [Collections.Generic.HashSet[int]]::new()
    for ($y=0; $y -lt $bitmap.Height; $y++) { for ($x=0; $x -lt $bitmap.Width; $x++) {
        $pixel = $bitmap.GetPixel($x,$y); [void]$alpha.Add($pixel.A); if ($pixel.A -gt 0) { [void]$colors.Add($pixel.ToArgb()) }
    } }
    $pass = $bitmap.Width -eq 32 -and $bitmap.Height -eq 32 -and ($alpha | Where-Object { $_ -ne 0 -and $_ -ne 255 }).Count -eq 0 -and $colors.Count -le 8
    $rows += [ordered]@{ name=$name; width=$bitmap.Width; height=$bitmap.Height; opaqueColors=$colors.Count; alphaValues=@($alpha | Sort-Object); pass=$pass }
    $bitmap.Dispose()
}

$sheet = [Drawing.Bitmap]::new(768,400,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($sheet); $graphics.Clear([Drawing.Color]::FromArgb(255,12,18,21)); $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor; $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
$font = [Drawing.Font]::new("Consolas",14,[Drawing.FontStyle]::Bold); $brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,214,224,218))
for ($i=0; $i -lt $names.Count; $i++) {
    $col=$i%3; $row=[Math]::Floor($i/3); $x=32+$col*250; $y=22+$row*190
    for($cy=0;$cy-lt128;$cy+=16){for($cx=0;$cx-lt128;$cx+=16){$c=if((($cx+$cy)/16)%2-eq0){[Drawing.Color]::FromArgb(255,38,47,50)}else{[Drawing.Color]::FromArgb(255,25,32,35)};$graphics.FillRectangle([Drawing.SolidBrush]::new($c),$x+$cx,$y+$cy,16,16)}}
    $icon=[Drawing.Bitmap]::FromFile((Join-Path $source ($names[$i]+".png"))); $graphics.DrawImage($icon,[Drawing.Rectangle]::new($x,$y,128,128),0,0,32,32,[Drawing.GraphicsUnit]::Pixel); $icon.Dispose(); $graphics.DrawString($names[$i],$font,$brush,$x,$y+138)
}
$sheet.Save((Join-Path $artifact "inventory_missing_icons_contact_sheet.png"),[Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $sheet.Dispose(); $font.Dispose(); $brush.Dispose()
$report=[ordered]@{ generatedAt=(Get-Date).ToString("s"); target="32x32 hard-alpha <=8 opaque colors"; total=$rows.Count; passed=@($rows|Where-Object pass).Count; items=$rows }
[IO.File]::WriteAllText((Join-Path $artifact "inventory_missing_icons_qa.json"),($report|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
$report | ConvertTo-Json -Depth 5
