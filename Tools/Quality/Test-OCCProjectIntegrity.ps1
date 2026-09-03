[CmdletBinding()]
param(
    [int]$LargeSourceWarningLines = 800,
    [int]$CriticalSourceWarningLines = 1500
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$expectedRoot = [IO.Path]::GetFullPath('E:\database-placeholder').TrimEnd('\')
$expectedRoot = $repoRoot.Substring(0, 3) + [char]0x6570 + [char]0x636e + [char]0x5e93 + '\OCC_Codex'
$failures = [Collections.Generic.List[string]]::new()
$warnings = [Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) { $failures.Add($Message) }
function Add-Warning([string]$Message) { $warnings.Add($Message) }

if (-not [string]::Equals($repoRoot.TrimEnd('\'), $expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Add-Failure "Wrong repository root: $repoRoot (expected $expectedRoot)"
}

$todoPath = Join-Path $repoRoot ('Worldbuilding\03_' + [char]0x5f00 + [char]0x53d1 + [char]0x7ba1 + [char]0x7406 + '\OCC_' + [char]0x5f53 + [char]0x524d + [char]0x5f85 + [char]0x529e + '.md')
if (-not (Test-Path -LiteralPath $todoPath)) {
    Add-Failure 'Missing current task document.'
}
else {
    $todo = Get-Content -Raw -Encoding utf8 -LiteralPath $todoPath
    $recentMarker = '## ' + [char]0x6700 + [char]0x8fd1 + [char]0x5b8c + [char]0x6210
    $recentIndex = $todo.IndexOf($recentMarker, [StringComparison]::Ordinal)
    $currentSection = if ($recentIndex -ge 0) { $todo.Substring(0, $recentIndex) } else { $todo }
    $activeTasks = [regex]::Matches($currentSection, '(?m)^### .+--? IN PROGRESS(?:\([^\r\n]+\))?\s*$|^### .+' + [char]0x2014 + ' IN PROGRESS(?:' + [char]0xff08 + '[^\r\n]+' + [char]0xff09 + ')?\s*$')
    if ($activeTasks.Count -gt 1) {
        Add-Failure "The task document may contain at most one IN PROGRESS heading; found $($activeTasks.Count)."
    }
    elseif ($activeTasks.Count -eq 0) {
        Add-Warning 'There is no active main task. Register one before starting the next implementation.'
    }
    else {
        $taskStart = $activeTasks[0].Index
        $nextHeading = $currentSection.IndexOf("`n### ", $taskStart + $activeTasks[0].Length, [StringComparison]::Ordinal)
        $taskBlock = if ($nextHeading -ge 0) { $currentSection.Substring($taskStart, $nextHeading - $taskStart) } else { $currentSection.Substring($taskStart) }
        $fieldPattern = '(?m)^- \*\*[^\r\n]+(?::|' + [char]0xff1a + ')\*\*'
        $contractFields = [regex]::Matches($taskBlock, $fieldPattern).Count
        if ($contractFields -lt 5) {
            Add-Failure "The active task contract has only $contractFields bold fields; expected ownership, goal, scope, acceptance, and next step."
        }
    }
}

$assetsRoot = Join-Path $repoRoot 'UnityProject\Assets'
if (Test-Path -LiteralPath $assetsRoot) {
    Get-ChildItem -LiteralPath $assetsRoot -Recurse -File |
        Where-Object { $_.Extension -ne '.meta' } |
        ForEach-Object {
            if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) {
                Add-Failure "Unity asset is missing its Meta file: $($_.FullName.Substring($repoRoot.Length + 1))"
            }
        }
}

Push-Location $repoRoot
try {
    $tracked = @(git ls-files)
    foreach ($path in $tracked) {
        if ($path -match '(^|/)(Library|Temp|Obj|Logs|UserSettings|Artifacts|_Recovery)(/|$)' -or
            $path -match '\.(csproj|sln|slnx|user|tmp|bak)$') {
            Add-Failure "Generated file is tracked: $path"
        }
    }

    $changed = @(git status --short)
    if ($changed.Count -gt 80) {
        Add-Warning "The worktree has $($changed.Count) changed entries. Split runtime, UI, art, tests, and documentation before committing."
    }

    $untracked = @(git ls-files --others --exclude-standard)
    if ($untracked.Count -gt 100) {
        Add-Warning "There are $($untracked.Count) untracked files. Classify source art, QA output, and formal Unity assets first."
    }
}
finally {
    Pop-Location
}

$sourceRoot = Join-Path $repoRoot 'UnityProject\Assets\Game\Runtime'
if (Test-Path -LiteralPath $sourceRoot) {
    Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.cs' -File | ForEach-Object {
        $lineCount = (Get-Content -LiteralPath $_.FullName).Count
        $relative = $_.FullName.Substring($repoRoot.Length + 1)
        if ($lineCount -ge $CriticalSourceWarningLines) {
            Add-Warning "Critical responsibility-overload candidate ($lineCount lines; do not add another responsibility): $relative"
        }
        elseif ($lineCount -ge $LargeSourceWarningLines) {
            Add-Warning "Large source candidate ($lineCount lines; extract a boundary before adding responsibilities): $relative"
        }
    }
}

Write-Output "OCC project integrity: $($failures.Count) failure(s), $($warnings.Count) warning(s)"
foreach ($failure in $failures) { Write-Output "[FAIL] $failure" }
foreach ($warning in $warnings) { Write-Output "[WARN] $warning" }

if ($failures.Count -gt 0) { exit 1 }
exit 0
