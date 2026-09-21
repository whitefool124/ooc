# OCC 技能表 ↔ 代码卡面数值审计
#
# 用途：在改动 OCC_技能配置表_v1.0.csv 或 FireSpellCatalog.cs 之后复验两者是否仍然一致。
# 依据：总案 7.1 —— 个人术式与法宝只在 OCC_技能配置表_v1.0.csv 中维护，它是技能内容的唯一活动来源。
#
# 用法（仓库根目录）：powershell -File Tools/OCCData/audit_skill_table.ps1
#
# 已覆盖字段：ID／名称／稀有度／AP／魔力／冷却／射程／伤害／燃烧强度与持续／行动条调整／
#             火场伤害与持续／护盾／恢复魔力／推离格数／自损生命／目标与形状关键词。
# 已知假阳性（脚本会照旧报出，需人工判读）：
#   1. 火场单次伤害（CreateFireground）会被文档正则当成直伤报出；
#   2. Rows using alternate 结构（M10 的 12/20、U20 的 20/28）集合相同、顺序不同；
#   3. 通过别的规则实现的护盾（M11／U10 的 GrantShieldBeforeRanged）与回流回报（U04 的标记摧毁回报）
#      不在 RestoreShield／RestoreMana 规则里；
#   4. “直线空格路径”（U01／U18）在代码里是 Shape=Path ＋ 运行时轴线与阻挡校验。

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$catalogPath = Join-Path $root 'UnityProject/Assets/Game/Runtime/Combat/FireSpellCatalog.cs'
$tablePath = Join-Path $root 'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv'
foreach ($path in @($catalogPath, $tablePath)) { if (-not (Test-Path $path)) { throw "缺少文件：$path" } }

$lines = Get-Content -LiteralPath $catalogPath -Encoding UTF8
$header = '^\s*S\("(F-P-[MRU]\d\d)","([^"]+)",FireSpellRarity\.(\w+),FireSpellGroup\.\w+,\w+,FireDeliveryMode\.\w+,([\w.]+),([\w.]+),([\w.]+),(\d+),(\d+),(\d+),(\d+),(\d+),FireTargetKind\.(\w+),FireSelectionShape\.(\w+),(\d+),'
$code = @{}
foreach ($line in $lines) {
    if ($line -notmatch $header) { continue }
    $numbers = { param($pattern) ([regex]::Matches($line, $pattern) | ForEach-Object { $_.Groups[1].Value }) -join ',' }
    $code[$Matches[1]] = [pscustomobject]@{
        Name = $Matches[2]; Rarity = $Matches[3]; Weapon = $Matches[4]; Window = $Matches[5]; Consumption = $Matches[6]
        Ap = [int]$Matches[7]; Mana = [int]$Matches[8]; Cooldown = [int]$Matches[9]; Delay = [int]$Matches[10]; Range = [int]$Matches[11]
        Target = $Matches[12]; Shape = $Matches[13]; Length = [int]$Matches[14]
        Damage = (& $numbers 'R\(FireRuleKind\.(?:Damage|WeaponDamage),(\d+)')
        BurnStrength = (& $numbers 'R\(FireRuleKind\.ApplyBurning,(\d+)')
        BurnTurns = (& $numbers 'R\(FireRuleKind\.ApplyBurning,\d+,(\d+)')
        Fireground = (& $numbers 'R\(FireRuleKind\.CreateFireground,(\d+)')
        FiregroundTurns = (& $numbers 'R\(FireRuleKind\.CreateFireground,\d+,(\d+)')
        Shield = (& $numbers 'R\(FireRuleKind\.RestoreShield,(\d+)')
        ManaRestore = (& $numbers 'R\(FireRuleKind\.RestoreMana,(\d+)')
        Push = (& $numbers 'R\(FireRuleKind\.Push(?:AllUnits)?,(\d+)')
        SelfLoss = (& $numbers 'R\(FireRuleKind\.LoseHealth,(\d+)')
    }
}

$rows = Import-Csv -LiteralPath $tablePath -Encoding UTF8
$findings = New-Object System.Collections.Generic.List[string]
foreach ($row in $rows) {
    if (-not $code.ContainsKey($row.技能ID)) { continue }
    $c = $code[$row.技能ID]
    $effect = $row.效果链
    $scope = $row.'目标与范围'
    $unique = { param($pattern, $text) ([regex]::Matches($text, $pattern) | ForEach-Object { if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } }) | Select-Object -Unique }
    $docDamage = (& $unique '(\d+)\s*点(?:武器|火焰|以太|物理)?伤害' $effect) -join ','
    $docShield = (& $unique '(\d+)\s*(?:点)?(?:普通|临时)?护盾|得(\d+)盾' $effect) -join ','
    $docManaRestore = (& $unique '恢复\s*(\d+)\s*(?:点)?(?:个人)?魔力' $effect) -join ','
    $docPush = (& $unique '(?:推离|推开|震退|向外推|被推|推)\s*(\d+)\s*格' $effect) -join ','
    $docSelfLoss = (& $unique '失去\s*(\d+)\s*(?:点)?生命' $effect) -join ','
    $docDelay = (& $unique '行动条\s*\+\s*(\d+)' $effect) -join ','
    if ($docDamage -and $docDamage -ne $c.Damage) { $findings.Add("$($row.技能ID)`t$($row.名称)`t伤害`t文档=$docDamage`t代码=$($c.Damage)") }
    if ($docShield -and $docShield -ne $c.Shield) { $findings.Add("$($row.技能ID)`t$($row.名称)`t护盾`t文档=$docShield`t代码=$($c.Shield)") }
    if ($docManaRestore -and $docManaRestore -ne $c.ManaRestore) { $findings.Add("$($row.技能ID)`t$($row.名称)`t回魔`t文档=$docManaRestore`t代码=$($c.ManaRestore)") }
    if ($docPush -and $docPush -ne $c.Push) { $findings.Add("$($row.技能ID)`t$($row.名称)`t推离`t文档=$docPush`t代码=$($c.Push)") }
    if ($docSelfLoss -and $docSelfLoss -ne $c.SelfLoss) { $findings.Add("$($row.技能ID)`t$($row.名称)`t自损`t文档=$docSelfLoss`t代码=$($c.SelfLoss)") }
    if ($docDelay -and $docDelay -ne "$($c.Delay)") { $findings.Add("$($row.技能ID)`t$($row.名称)`t行动条`t文档=$docDelay`t代码=$($c.Delay)") }
    $docRange = if ($scope -match '射程(\d+)') { $Matches[1] } else { '' }
    if ($docRange -and [int]$docRange -ne $c.Range) { $findings.Add("$($row.技能ID)`t$($row.名称)`t射程`t文档=$docRange`t代码=$($c.Range)") }
    if ($scope -match '锥形' -and $c.Shape -ne 'Cone') { $findings.Add("$($row.技能ID)`t$($row.名称)`t形状`t文档=锥形`t代码=$($c.Shape)") }
    if ($scope -match '十字' -and $c.Shape -notin @('Cross', 'CenterAndOrthogonal', 'OrthogonalRing')) { $findings.Add("$($row.技能ID)`t$($row.名称)`t形状`t文档=十字`t代码=$($c.Shape)") }
    if ($scope -match '3x3|3×3' -and $c.Shape -ne 'Square3') { $findings.Add("$($row.技能ID)`t$($row.名称)`t形状`t文档=3x3`t代码=$($c.Shape)") }
    if ($scope -match '燃烧敌' -and $c.Target -notmatch 'Burning') { $findings.Add("$($row.技能ID)`t$($row.名称)`t目标`t文档=燃烧敌`t代码=$($c.Target)") }
}

"审计卡面：$($code.Count) 张；发现差异 $($findings.Count) 条"
$findings | ForEach-Object { $_ }
