[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$failures = New-Object 'System.Collections.Generic.List[string]'
$passes = 0

function Assert-Kmc {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Condition) {
        $script:passes++
        Write-Host "PASS $Message"
    }
    else {
        $script:failures.Add($Message)
        Write-Host "FAIL $Message"
    }
}

$actualRoot = (& git -C $repoRoot rev-parse --show-toplevel 2>$null).Trim()
Assert-Kmc ([IO.Path]::GetFullPath($actualRoot) -eq $repoRoot) 'standalone repository root'

$version = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'version.json') | ConvertFrom-Json
$info = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'Info.json') | ConvertFrom-Json
$typeMap = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'planning\KINGMAKER-WRATH-TYPE-MAP.json') | ConvertFrom-Json
$fingerprint = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json') | ConvertFrom-Json
Assert-Kmc ($version.modId -ceq 'KingmakerMountedCombat') 'version source uses standalone ID'
Assert-Kmc ($info.Id -ceq $version.modId -and $info.Version -ceq $version.productVersion) 'Info.json matches version source'
Assert-Kmc ($info.AssemblyName -ceq 'KingmakerMountedCombat.dll' -and $info.EntryMethod -ceq 'KingmakerMountedCombat.Main.Load') 'UMM identity is standalone'
Assert-Kmc (@($info.Requirements).Count -eq 0) 'UMM metadata has no gameplay-mod dependency'
Assert-Kmc ($typeMap.authority.kingmakerMvid -ceq '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7') 'type map binds exact Kingmaker MVID'
Assert-Kmc ($fingerprint.kingmaker.displayVersion -ceq '2.1.7b') 'fingerprint binds Kingmaker 2.1.7b'

[xml]$project = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj')
$namespace = New-Object Xml.XmlNamespaceManager($project.NameTable)
$namespace.AddNamespace('m', 'http://schemas.microsoft.com/developer/msbuild/2003')
$targetFramework = $project.SelectSingleNode('//m:TargetFrameworkVersion', $namespace).InnerText
$languageVersion = $project.SelectSingleNode('//m:LangVersion', $namespace).InnerText
$prefer32 = $project.SelectSingleNode('//m:Prefer32Bit', $namespace).InnerText
Assert-Kmc ($targetFramework -ceq 'v4.7') 'production target is .NET Framework 4.7'
Assert-Kmc ($languageVersion -ceq '7.3') 'production language level is C# 7.3'
Assert-Kmc ($prefer32 -ceq 'false') 'production AnyCPU does not prefer 32-bit'

$hintReferences = @($project.SelectNodes('//m:Reference[m:HintPath]', $namespace))
$copyLocalDisabled = $true
foreach ($reference in $hintReferences) {
    $privateNode = $reference.SelectSingleNode('m:Private', $namespace)
    if ($null -eq $privateNode -or $privateNode.InnerText -cne 'False') {
        $copyLocalDisabled = $false
    }
}
Assert-Kmc $copyLocalDisabled 'all local game/tool references have Copy Local disabled'

$projectText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj')
Assert-Kmc ($projectText -match '0Harmony12\.dll' -and $projectText -notmatch '(?<!12)\\0Harmony\.dll') 'production references exact Harmony12 surface only'
Assert-Kmc ($projectText -notmatch '(?i)Wrath|Second Adventure|BuffPlanner|Gunslinger|Tabletop|CallOfTheWild') 'production project has no foreign gameplay reference'

$tracked = @(& git -C $repoRoot ls-files)
$ignoredLocalPaths = @(& git -C $repoRoot check-ignore 'LocalGamePaths.props' 2>$null)
Assert-Kmc ($tracked -cnotcontains 'LocalGamePaths.props' -and $ignoredLocalPaths.Count -eq 1) 'LocalGamePaths.props is ignored and untracked'
$prohibitedExtensions = @('.dll', '.exe', '.pdb', '.zip', '.7z', '.assets', '.ress', '.resource', '.bundle', '.sav')
$prohibitedTracked = @($tracked | Where-Object { $prohibitedExtensions -contains ([IO.Path]::GetExtension($_).ToLowerInvariant()) })
Assert-Kmc ($prohibitedTracked.Count -eq 0) 'Git contains no binary, game-asset, archive, or save payload'

$untracked = @(& git -C $repoRoot ls-files --others --exclude-standard)
$prohibitedUntracked = @($untracked | Where-Object { $prohibitedExtensions -contains ([IO.Path]::GetExtension($_).ToLowerInvariant()) })
Assert-Kmc ($prohibitedUntracked.Count -eq 0) 'untracked source tree contains no prohibited payload'

$prohibitedRoots = @('bin/', 'obj/', '.vs/', 'runtime-state/', 'runtime-staging/', 'runtime-evidence/', 'runtime-backups/', 'analysis-cache/', 'artifacts/')
$trackedGenerated = @($tracked | Where-Object {
    $path = $_.Replace('\', '/')
    @($prohibitedRoots | Where-Object { $path.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
})
Assert-Kmc ($trackedGenerated.Count -eq 0) 'Git contains no generated/runtime evidence tree'

$productionFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
$productionText = ($productionFiles | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
Assert-Kmc ($productionText -notmatch '(?i)HarmonyLib|UnitPartRider|UnitPartSaddled|SaddledUnitController') 'production source has no Wrath-only or Harmony2 API use'
Assert-Kmc ($productionText -notmatch '(?i)KingmakerBuffPlanner|TabletopAddedRules|KingmakerGunslinger|CallOfTheWild') 'production source has independent namespace and persistence identity'
Assert-Kmc ($productionText -notmatch '[A-Za-z]:\\') 'production source contains no absolute machine path'

$trackedTextFiles = @($tracked | Where-Object { [IO.Path]::GetExtension($_).ToLowerInvariant() -in @('.cs','.ps1','.md','.json','.xml','.props','.csproj','.sln','.gitignore') })
$trackedText = ($trackedTextFiles | ForEach-Object { Get-Content -Raw -LiteralPath (Join-Path $repoRoot $_) }) -join "`n"
Assert-Kmc ($trackedText -notmatch '(?i)BEGIN (RSA|OPENSSH|EC) PRIVATE KEY|gh[pousr]_[A-Za-z0-9_]{20,}|password\s*[:=]\s*[^\s`"'']+') 'tracked shippable text contains no recognized secret pattern'

if ($failures.Count -gt 0) {
    Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
    foreach ($failure in $failures) {
        Write-Host "  $failure"
    }
    exit 1
}

Write-Host "TOTAL PASS=$passes FAIL=0"
