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
$buildIdentityText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\BuildIdentity.cs')
$runtimeProtocolText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeProtocol.cs')
$mainText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Main.cs')
$assemblyInfoText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Properties\AssemblyInfo.cs')
$typeMap = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'planning\KINGMAKER-WRATH-TYPE-MAP.json') | ConvertFrom-Json
$fingerprint = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json') | ConvertFrom-Json
Assert-Kmc ($version.modId -ceq 'KingmakerMountedCombat') 'version source uses standalone ID'
Assert-Kmc ($info.Id -ceq $version.modId -and $info.Version -ceq $version.productVersion) 'Info.json matches version source'
Assert-Kmc (
    $buildIdentityText -match ('internal const string ProductVersion = "' + [Regex]::Escape([string]$version.productVersion) + '";') -and
    $runtimeProtocolText -match 'ProductVersion, BuildIdentity\.ProductVersion' -and
    $mainText -match 'BuildIdentity\.ProductVersion' -and
    $assemblyInfoText -match 'AssemblyInformationalVersion\(KingmakerMountedCombat\.BuildIdentity\.ProductVersion\)'
) 'compiled build identity matches version source'
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

# Disabling runs a full mounted cleanup over live graphs a native archive
# worker may be serializing, and disposal unpatches the commit transpiler that
# worker runs through. Both live-operation refusals must therefore stand ahead
# of the cleanup call in the disable branch, and disposal must take its one
# bounded drain before unpatching anything.
$compositionRoot = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\CompositionRoot.cs')
$disableBranch = [Regex]::Match($compositionRoot, '(?s)// Always execute idempotent cleanup.*?lifecycle\.HandleModDisable\(\)')
$beforeCleanup = if ($disableBranch.Success) { $compositionRoot.Substring(0, $disableBranch.Index) } else { '' }
Assert-Kmc ($disableBranch.Success -and
    $beforeCleanup -match 'persistence\.SaveSuspended' -and
    $beforeCleanup -match 'persistence\.LoadInFlight') 'disable refuses both live persistence operations before cleanup'
$disposeBody = [Regex]::Match($compositionRoot, '(?s)public void Dispose\(\).*?patches\.Dispose\(\)')
Assert-Kmc ($disposeBody.Success -and $disposeBody.Value -match 'persistence\.DrainForTeardown\(') 'disposal drains an owned archive worker before unpatching'

# The archive is committed the moment the native replacement returns. Recording
# that must happen before descriptor rebinding and ownership completion, either
# of which can throw afterwards without un-writing the bytes; otherwise a
# post-commit failure would be reported to the player as a lost save.
$commitText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativeMountedArchiveCommit.cs')
$replaceAt = $commitText.IndexOf('File.Replace(source, destination, null)', [StringComparison]::Ordinal)
$recordAt = if ($replaceAt -ge 0) { $commitText.IndexOf('RecordCommit(destination);', $replaceAt, [StringComparison]::Ordinal) } else { -1 }
$rebindAt = if ($replaceAt -ge 0) { $commitText.IndexOf('PathField.SetValue(staged, destination)', $replaceAt, [StringComparison]::Ordinal) } else { -1 }
$completeAt = if ($replaceAt -ge 0) { $commitText.IndexOf('ownership?.Complete()', $replaceAt, [StringComparison]::Ordinal) } else { -1 }
Assert-Kmc ($replaceAt -ge 0 -and $recordAt -gt $replaceAt -and $rebindAt -gt $recordAt -and
    $completeAt -gt $recordAt) 'the archive commit is recorded before descriptor rebinding and ownership completion'

# The per-frame drain runs on every ordinary frame with nothing draining.
# Resolving a null scope legitimately answers "owns no worker", so the early
# return must come first or the release runs against no scope at all.
$serviceText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPersistenceService.cs')
$drainBody = [Regex]::Match($serviceText, '(?s)private void DrainAbandonedSave\(\).*?\r?\n        \}')
Assert-Kmc ($drainBody.Success -and
    $drainBody.Value -match '(?s)var scope = drainingSave;\s*(//[^\r\n]*\r?\n\s*)*if \(scope == null\) return;') `
    'the per-frame drain returns before resolving when nothing is draining'

# The isolation's write leases must not depend on the persistence controller:
# the integration-absent load unpatches that controller before the engine's own
# save, and final103-p07-absent measured the fail-closed commit guard refusing
# that write while the lease seams lived in the controller.
$isolationText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativePersistenceIsolation.cs')
$controllerText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs')
Assert-Kmc ($isolationText.Contains('PatchWriteSeam(harmony, typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, "PreparedWritePostfix")') -and
    $isolationText.Contains('PatchWriteSeam(harmony, typeof(SaveManager), "SerializeAndSaveThread", 0x0600802A,') -and
    $isolationText.Contains('"WorkerCompletePostfix");') -and
    $controllerText -notmatch 'ObservePreparedWrite|ObserveWorkerComplete') 'the isolation owns its write lease seams independently of the persistence controller'

# Teardown must CONSUME the ownership verdict: a Refused verdict throws before
# any lifecycle cleanup or unpatching, and Main.OnUnload turns that throw into
# the false return the installed UMM honours (ModEntry.Reload aborts on it).
$disposeAll = [Regex]::Match($compositionRoot, '(?s)public void Dispose\(\).*?logger\.Info\("Composition root disposed')
$verdictAt = if ($disposeAll.Success) { $disposeAll.Value.IndexOf('var verdict = persistence.DrainForTeardown(', [StringComparison]::Ordinal) } else { -1 }
$refuseAt = if ($verdictAt -ge 0) { $disposeAll.Value.IndexOf('OwnedWorkerTeardownVerdict.Refused', $verdictAt, [StringComparison]::Ordinal) } else { -1 }
$throwAt = if ($refuseAt -ge 0) { $disposeAll.Value.IndexOf('throw new InvalidOperationException(', $refuseAt, [StringComparison]::Ordinal) } else { -1 }
$cleanupAt = if ($disposeAll.Success) { $disposeAll.Value.IndexOf('lifecycle.HandleModDisable()', [StringComparison]::Ordinal) } else { -1 }
$unpatchAt = if ($disposeAll.Success) { $disposeAll.Value.IndexOf('patches.Dispose()', [StringComparison]::Ordinal) } else { -1 }
Assert-Kmc ($verdictAt -ge 0 -and $refuseAt -gt $verdictAt -and $throwAt -gt $refuseAt -and
    $cleanupAt -gt $throwAt -and $unpatchAt -gt $cleanupAt) 'disposal consumes the teardown verdict and refuses before cleanup or unpatching'
$mainText2 = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Main.cs')
$onUnload = [Regex]::Match($mainText2, '(?s)private static bool OnUnload\(.*?\n        \}')
Assert-Kmc ($onUnload.Success -and $onUnload.Value -match 'root\?\.Dispose\(\);' -and
    $onUnload.Value -match '(?s)catch \(Exception exception\)\s*\{.*?return false;') 'a refused disposal is reported to UMM as a false unload'

$trackedTextFiles = @($tracked | Where-Object { [IO.Path]::GetExtension($_).ToLowerInvariant() -in @('.cs','.ps1','.md','.json','.xml','.props','.csproj','.sln','.gitignore') })
$trackedText = ($trackedTextFiles | ForEach-Object { Get-Content -Raw -LiteralPath (Join-Path $repoRoot $_) }) -join "`n"
Assert-Kmc ($trackedText -notmatch '(?i)BEGIN (RSA|OPENSSH|EC) PRIVATE KEY|gh[pousr]_[A-Za-z0-9_]{20,}|password\s*[:=]\s*[^\s`"'']+') 'tracked shippable text contains no recognized secret pattern'

if ($failures.Count -gt 0) {
    Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
    foreach ($failure in $failures) {
        Write-Host "  $failure"
    }
    throw ('Source validation failed: ' + ($failures -join '; '))
}

Write-Host "TOTAL PASS=$passes FAIL=0"
