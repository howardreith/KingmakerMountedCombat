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

# ---------------------------------------------------------------------------
# Chunk 6A: voluntary combat Mount/Dismount. Kingmaker's native Move shell is
# the sole cost owner and TurnController.Prepare (0x06000C3C, which calls
# Cooldowns.Clear 0x0600C3BE) is the per-round grant, so no relationship
# transition may write a cooldown or repeat a preparation.
# ---------------------------------------------------------------------------
$playerActionText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPlayerActionController.cs')
$relationshipServiceText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\GameMountedRelationshipService.cs')
$adoptionText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\PairedActivationLifecycle.cs')
$candidateText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedPairCandidate.cs')
$nativeControlsText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativeMountedControlService.cs')

# One explicit voluntary combat admission, and no other way in.
Assert-Kmc ($candidateText -match 'public string Validate\(MountedRelationshipAdmission admission\)' -and
    $candidateText -match 'case MountedRelationshipAdmission\.VoluntaryCombat:' -and
    $candidateText -match 'Voluntary combat mounting requires a live encounter\.' -and
    $candidateText -notmatch 'private string Validate\(bool') `
    'the pair candidate admits combat only through the explicit voluntary combat mode'
Assert-Kmc ($relationshipServiceText -match 'admission != MountedRelationshipAdmission\.VoluntaryCombat' -and
    $relationshipServiceText -match 'MountRiderOn\(rider, mount, MountedRelationshipAdmission\.Exploration\)' -and
    $relationshipServiceText -match 'admission == MountedRelationshipAdmission\.SavedRestore[\s\S]{0,200}Saved restoration is not a voluntary mount admission') `
    'the relationship service keeps one voluntary combat path and defaults every other entry to exploration'

# The transition itself writes no resource and refreshes no readiness.
$voluntaryMountBody = [Regex]::Match($playerActionText, '(?s)internal bool TryExecuteNativeMount\(UnitEntityData caster, UnitEntityData target, string controlIdentity\).*?\n        \}\r?\n')
$voluntaryDismountBody = [Regex]::Match($playerActionText, '(?s)internal bool TryExecuteNativeDismount\(UnitEntityData caster, string controlIdentity\).*?\n        \}\r?\n')
$forbiddenResourceWrite = 'Cooldown\.(MoveAction|StandardAction|SwiftAction|Initiative|AttackOfOpportunity)\s*=|IgnoreCooldown\(\)|\.Clear\(\)\s*;\s*//\s*cooldown|SetIsActed|ForceToEnd\(|JoinCombat|StartTurn|ChooseNextUnit|OnNewRound|\.Prepare\(\)'
Assert-Kmc ($voluntaryMountBody.Success -and $voluntaryDismountBody.Success -and
    $voluntaryMountBody.Value -notmatch $forbiddenResourceWrite -and
    $voluntaryDismountBody.Value -notmatch $forbiddenResourceWrite) `
    'neither voluntary transition writes a native resource, forces a turn end or calls a preparation'
Assert-Kmc ($voluntaryMountBody.Value -match 'transitionLedger\.TryAdmitVoluntary\(\s*MountedTransitionKind\.VoluntaryMount' -and
    $voluntaryMountBody.Value -match '(?s)finally\s*\{\s*transitionLedger\.Settle\(record, accepted\);' -and
    $voluntaryDismountBody.Value -match 'transitionLedger\.TryAdmitVoluntary\(\s*MountedTransitionKind\.VoluntaryDismount' -and
    $voluntaryDismountBody.Value -match '(?s)finally\s*\{\s*transitionLedger\.Settle\(record, accepted\);') `
    'both voluntary transitions are admitted and settled exactly once through the transition ledger'
Assert-Kmc ($voluntaryMountBody.Value -match 'IsTransitionInCombat\(caster, target\)\s*\r?\n?\s*\? MountedRelationshipAdmission\.VoluntaryCombat\s*\r?\n?\s*: MountedRelationshipAdmission\.Exploration') `
    'the voluntary Mount admission mode is decided from live combat state at execution'
Assert-Kmc ($playerActionText -match 'trigger == CleanupTrigger\.Manual' -and
    $playerActionText -match 'transitionLedger\.RecordForcedDetach\(') `
    'forced detach is recorded as cleanup and never books a voluntary cost'

# Mid-encounter adoption: no principal preparation, no encounter-start re-entry,
# and the commit runs only with the immutable plan taken at admission.
$adoptBody = [Regex]::Match($adoptionText, '(?s)internal string AdoptRunningEncounter\(\s*\r?\n?\s*MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount\).*?\n        \}\r?\n')
Assert-Kmc ($adoptBody.Success -and
    $adoptBody.Value -notmatch 'JoinCombat|ChooseNextUnit|StartTurn|HandleCombatStart|BeginNativeEncounter|OnNewRound|NativeEnd|ForceToEnd' -and
    $adoptBody.Value -notmatch 'Cooldown\.(MoveAction|StandardAction|SwiftAction|Initiative)\s*=') `
    'adoption never re-enters encounter start, candidate selection or a resource write'
Assert-Kmc ($adoptBody.Success -and
    ([Regex]::Matches($adoptBody.Value, '\.Prepare\(\)').Count -eq 1) -and
    $adoptBody.Value -match 'partnerContext\.Prepare\(\);' -and
    $adoptBody.Value -match 'MidEncounterAdoption\.PreparePartnerThisRound') `
    'adoption performs exactly one native preparation and only for a pending partner slot'
Assert-Kmc ($adoptBody.Success -and
    $adoptBody.Value -match 'if \(disposition == MidEncounterAdoption\.Unavailable\)[\s\S]{0,80}return refusal;') `
    'adoption refuses an unresolvable transition round before changing any state'
Assert-Kmc ($adoptionText -match 'internal MidEncounterAdoption ResolveMidEncounterAdoption\(' -and
    $adoptionText -match 'private bool CanReplaceActivationForAdoption\(\)') `
    'the adoption disposition is resolvable without side effects and a split pair is not layered over'

Assert-Kmc ($adoptBody.Success -and $adoptBody.Value -notmatch 'BeginActorPreparation' -and
    $adoptBody.Value -match '!adopted\.State\(mount\)\.Prepared') `
    'the native preparation prefix owns the one partner grant and adoption requires its completion'

# R1: a partner whose native slot in this round is already behind the running turn
# is granted, prepared AND ENDED, so an allocation it has already taken can never
# become addressable again on the principal's adopted boundary.
$pairedActivationText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\PairedActivation.cs')
$retainBody = [Regex]::Match($pairedActivationText, '(?s)if \(partner == MidEncounterAdoption\.RetainPartnerParticipation\)\s*\r?\n\s*\{.*?\n            \}')
Assert-Kmc ($retainBody.Success -and
    $retainBody.Value -match 'Mount\.Granted = true;' -and
    $retainBody.Value -match 'Mount\.Prepared = true;' -and
    $retainBody.Value -match 'Mount\.Ended = true;' -and
    $pairedActivationText -match 'public bool CanAddress\(TActor actor, TBoundary boundary\)[\s\S]{0,200}!State\(actor\)\.Ended') `
    'a spent partner slot is ended at adoption and can never be addressed again on that boundary'
Assert-Kmc ($adoptBody.Success -and
    $adoptBody.Value -match 'A retained partner cannot own a private native turn context\.' -and
    $adoptBody.Value -match 'spent allocation was not closed\.' -and
    $adoptBody.Value -match ';partnerEnded=' -and
    $adoptBody.Value -match ';partnerAddressable=') `
    'a retained partner creates no private native context and its closed allocation is observable'

# R2: relationship attachment and encounter adoption are ONE transaction. The plan
# is typed, immutable and generation-bound; it is revalidated immediately before
# the commit; and a refused commit compensates the attachment instead of leaving a
# mounted relationship with no valid paired activation.
$planText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MidEncounterAdoptionPlan.cs')
Assert-Kmc ($planText -match 'public sealed class MidEncounterAdoptionPlan' -and
    $planText -notmatch 'get;\s*(internal |private |)set;' -and
    $planText -match 'disposition == MidEncounterAdoption\.Unavailable[\s\S]{0,200}never a plan' -and
    $planText -match 'public bool Matches\(MidEncounterAdoptionPlan other\)' -and
    $planText -match 'public MidEncounterAdoptionPlan WithCommittedGeneration\(long committedGeneration\)' -and
    $planText -match 'committedGeneration != RelationshipGeneration \+ 1') `
    'the adoption plan is immutable, generation-bound and never records an unavailable disposition'
Assert-Kmc ($adoptionText -match 'internal bool TryPlanMidEncounterAdoption\(' -and
    $adoptionText -match 'internal bool RevalidateMidEncounterAdoptionPlan\(' -and
    $adoptionText -match 'internal void RollbackMidEncounterAdoption\(string reason\)' -and
    $adoptionText -match 'private MidEncounterAdoption ObserveMidEncounterAdoption\(') `
    'the paired lifecycle exposes plan, revalidate, commit and rollback as separate operations'
$rollbackBody = [Regex]::Match($adoptionText, '(?s)internal void RollbackMidEncounterAdoption\(string reason\).*?\n        \}\r?\n')
Assert-Kmc ($rollbackBody.Success -and
    $rollbackBody.Value -notmatch 'Cooldown\.(MoveAction|StandardAction|SwiftAction|Initiative|AttackOfOpportunity)\s*=' -and
    $rollbackBody.Value -notmatch '\.Prepare\(\)|NativeEnd|ForceToEnd|JoinCombat|StartTurn|OnNewRound|SetIsActed' -and
    $rollbackBody.Value -match 'activation = null;' -and
    $rollbackBody.Value -match 'DisposePartnerContext\(\);' -and
    $rollbackBody.Value -match 'nativePreparationCommands\.Clear\(\);') `
    'the adoption rollback removes only KMC bookkeeping and never writes or refunds a native resource'
$mountRiderBody = [Regex]::Match($relationshipServiceText, '(?s)public TransitionResult MountRiderOn\(\s*\r?\n?\s*UnitEntityData rider, UnitEntityData mount, MountedRelationshipAdmission admission\).*?\n        \}\r?\n')
$planIndex = $mountRiderBody.Value.IndexOf('TryPlanMidEncounterAdoption')
$revalidateIndex = $mountRiderBody.Value.IndexOf('RevalidateMidEncounterAdoptionPlan')
$commitIndex = $mountRiderBody.Value.IndexOf('coordinator.Mount(runtime.CreateCandidate(), admission)')
$adoptIndex = $mountRiderBody.Value.IndexOf('AdoptRunningEncounter(committedPlan, rider, mount)')
Assert-Kmc ($mountRiderBody.Success -and $planIndex -ge 0 -and $revalidateIndex -gt $planIndex -and
    $commitIndex -gt $revalidateIndex -and $adoptIndex -gt $commitIndex -and
    $mountRiderBody.Value -match 'CompensateRefusedAdoption\(adoptionRefusal, committedPlan\)') `
    'a voluntary combat mount plans, revalidates, commits and only then adopts, compensating a refusal'
$compensateBody = [Regex]::Match($relationshipServiceText, '(?s)private TransitionResult CompensateRefusedAdoption\(string refusal, MidEncounterAdoptionPlan plan\).*?\n        \}\r?\n')
Assert-Kmc ($compensateBody.Success -and
    $compensateBody.Value -notmatch 'Cooldown\.(MoveAction|StandardAction|SwiftAction|Initiative|AttackOfOpportunity)\s*=' -and
    $compensateBody.Value -notmatch 'mountedPairGeneration\s*(=|--)' -and
    $compensateBody.Value -match 'RollbackMidEncounterAdoption\(refusal\)[\s\S]{0,400}Dismount\(CleanupTrigger\.AdoptionRefused\)' -and
    $compensateBody.Value -match 'new TransitionResult\(false,') `
    'the compensating path returns the relationship to unmounted, reports failure and never refunds or rewinds the generation'
$coordinatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\UnifiedMountedTurnCoordinator.cs')
Assert-Kmc ($coordinatorText -match 'IDisposable, IMidEncounterAdoptionAuthority' -and
    $coordinatorText -match 'relationship\.BindMidEncounterAdoptionAuthority\(this\);' -and
    $relationshipServiceText -match 'if \(adoptionAuthority != null \|\| authority == null\)') `
    'the paired lifecycle is bound exactly once as the single mid-encounter adoption authority'
$activatedBody = [Regex]::Match($coordinatorText, '(?s)private void HandleMountedPairActivated\(UnitEntityData rider, UnitEntityData mount\).*?\n        \}\r?\n')
Assert-Kmc ($activatedBody.Success -and
    $activatedBody.Value -notmatch 'AdoptRunningEncounter' -and
    $activatedBody.Value -match 'adoption-invariant-violated') `
    'the activation announcement never attempts adoption and only checks the transaction invariant'

# The admitted-shell bypass stays scoped to the stale Move-resource predicate.
$evaluatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedPlayerAction.cs')
Assert-Kmc (([Regex]::Matches($evaluatorText, 'NativeMoveActionShellAdmitted').Count -eq 4) -and
    $evaluatorText -match 'context\.InCombat && !context\.RiderHasMoveAction &&\s*\r?\n?\s*!context\.NativeMoveActionShellAdmitted' -and
    $evaluatorText -match 'context\.InCombat && context\.TurnBasedCombat && context\.RiderUsedStandardAction &&\s*\r?\n?\s*!context\.PairAdjacent && !context\.NativeMoveActionShellAdmitted') `
    'the admitted native shell suppresses only the rider Move-resource predicates (no Move; spent-Standard approach)'
Assert-Kmc ($evaluatorText -match 'context\.InCombat && !context\.PairedAdoptionAvailable' -and
    $evaluatorText -match 'context\.RelationshipTransitionInFlight' -and
    $evaluatorText -notmatch 'available only outside combat in this preview') `
    'combat Mount availability reports the paired disposition and in-flight gates instead of a blanket refusal'

# R3: a turn-based relationship transition requires the rider's turn to be ACTING.
# Preparing is refused with its own message rather than retained on an assumption.
Assert-Kmc ($evaluatorText -match 'public static bool IsTurnEligible\(\s*\r?\n\s*bool turnBasedCombat,\s*\r?\n\s*bool currentTurnIsExactRider,\s*\r?\n\s*bool turnActing\)' -and
    $evaluatorText -match 'return !turnBasedCombat \|\| currentTurnIsExactRider && turnActing;' -and
    $evaluatorText -match 'public static string DescribeTurnIneligibility\(' -and
    $evaluatorText -match 'if \(turnPreparing\)[\s\S]{0,160}finished preparing') `
    'a turn-based relationship transition requires an acting rider turn and names the preparing boundary'
Assert-Kmc ($playerActionText -match 'CombatMountDismountPolicy\.IsTurnEligible\(\s*\r?\n?\s*turnBased, currentTurnIsExactRider, turnActing\);' -and
    $playerActionText -match 'context\.CombatTurnIneligibilityReason = CombatMountDismountPolicy\.DescribeTurnIneligibility\(' -and
    $evaluatorText -match 'context\.CombatTurnIneligibilityReason') `
    'the controller decides turn eligibility from acting alone and surfaces the exact reason'

# R4: a Dismount shell must prove that its captured target and its delivery target
# are both the exact caster, and that the caster is still the live rider.
$resolveShellBody = [Regex]::Match($nativeControlsText, '(?s)private NativeRelationshipShell ResolveDeliveringShell\(.*?\n        \}\r?\n')
$dismountPolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\DismountTargetIdentityPolicy.cs')
Assert-Kmc ($resolveShellBody.Success -and
    $resolveShellBody.Value -match 'kind == NativeMountedControlKind\.Dismount' -and
    $resolveShellBody.Value -match 'DismountTargetIdentityPolicy\.Refuse\(' -and
    $resolveShellBody.Value -match 'target != null,' -and
    $resolveShellBody.Value -match 'target != null && ReferenceEquals\(target, caster\),' -and
    $resolveShellBody.Value -match 'shell\.TargetId,' -and
    $resolveShellBody.Value -match 'caster\.UniqueId,' -and
    $resolveShellBody.Value -match 'liveRider != null && ReferenceEquals\(liveRider, caster\),' -and
    $resolveShellBody.Value -match 'shell\.GenerationAtInit,' -and
    $resolveShellBody.Value -match 'shell\.GenerationAtInit != relationship\.MountedPairGeneration' -and
    $dismountPolicyText -match 'must target its own rider' -and
    $dismountPolicyText -match 'created for a different rider' -and
    $dismountPolicyText -match 'rider changed after this dismount' -and
    $dismountPolicyText -match 'shellGenerationAtInit != currentRelationshipGeneration') `
    'a dismount delivery revalidates its captured target, its delivery target, its rider and its generation'

# The Deliver-ownership repair. Proven from the installed assembly: OnAction sets
# ExecutionProcess and returns a terminal result unless IsEngageUnit, so the command
# completes and leaves the Move slot while its process delivers on later frames. The
# shell is therefore bound to the exact AbilityExecutionContext at OnAction and that
# exact binding is consumed at Deliver. It must stay a one-to-one binding: no
# recent-shell lookup, no caster-only lookup, and exactly-once on the shell itself.
$bindBody = [Regex]::Match($nativeControlsText, '(?s)internal void BindNativeRelationshipProcess\(UnitUseAbility command\).*?\n        \}\r?\n')
Assert-Kmc ($bindBody.Success -and
    $bindBody.Value -match 'command\.ExecutionProcess\?\.Context' -and
    $bindBody.Value -match 'relationshipShellContexts\.Add\(context, shell\)' -and
    $bindBody.Value -match 'relationshipShells\.TryGetValue\(command, out shell\)' -and
    $bindBody.Value -match '!ReferenceEquals\(bound, shell\)' -and
    $bindBody.Value -notmatch 'Cooldown\.|\.Prepare\(\)|ForceToEnd|JoinCombat|StartTurn') `
    'the relationship shell binds to its own command''s exact execution context at the OnAction boundary'
# The binding ORDER and the refusal set are one typed policy, so every combination is
# provable offline instead of only readable in the service.
$bindingPolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\NativeShellBindingPolicy.cs')
$bindingResolveBody = [Regex]::Match($bindingPolicyText, '(?s)public static NativeShellBindingOutcome Resolve\(.*?\n        \}')
Assert-Kmc ($bindingResolveBody.Success -and
    # Poisoned first, before the slot is even computed.
    $bindingResolveBody.Value.IndexOf('contextPresent && contextPoisoned') -ge 0 -and
    $bindingResolveBody.Value.IndexOf('contextPresent && contextPoisoned') -lt
        $bindingResolveBody.Value.IndexOf('AdmitsSynchronousMoveSlot(') -and
    # Then disagreement, and only then either binding.
    $bindingResolveBody.Value.IndexOf('contextShellPresent && slotAdmitted && !bindingsAgree') -lt
        $bindingResolveBody.Value.IndexOf('if (contextShellPresent)') -and
    $bindingResolveBody.Value.IndexOf('if (contextShellPresent)') -lt
        $bindingResolveBody.Value.IndexOf('if (slotAdmitted)') -and
    # The slot is admitted ONLY when its own process created this exact context.
    $bindingPolicyText -match 'return contextPresent && slotPresent && slotOwnsThisContext && slotShellPresent;' -and
    # Only a disagreement poisons the execution; every other refusal rides on the shell.
    $bindingPolicyText -match 'return outcome == NativeShellBindingOutcome\.Disagreement;' -and
    $bindingPolicyText -match 'PoisonedContextRefusal =' -and
    $bindingPolicyText -match 'DisagreementRefusal =\s*\r?\n?\s*"Two different mounted transitions claim this native execution\."' -and
    $bindingPolicyText -match 'NoOwnershipRefusal =' -and
    # A pure decision: it reaches no native state and performs no lookup of its own.
    $bindingPolicyText -notmatch 'UnitEntityData|AbilityExecutionContext|relationshipShells|OrderBy|\.Last\(|FirstOrDefault\(') `
    'the two-binding resolution order and its refusal set are one pure typed policy'
Assert-Kmc ($resolveShellBody.Success -and
    # The authoritative context binding, and it is never read from a poisoned context.
    $resolveShellBody.Value -match 'if \(context != null && !contextPoisoned\) \{ relationshipShellContexts\.TryGetValue\(context, out contextShell\); \}' -and
    # The Move-slot route is admitted ONLY for synchronous delivery: that slot command's
    # own execution process must have created this very context.
    $resolveShellBody.Value -match 'ReferenceEquals\(slot\.ExecutionProcess\?\.Context, context\)' -and
    $resolveShellBody.Value -match 'if \(slotOwnsThisContext\) \{ relationshipShells\.TryGetValue\(slot, out slotShell\); \}' -and
    # A poisoned context is detected FIRST, before the Move slot is consulted at all, so
    # an unresolvable conflict can never be salvaged by the slot's later contents.
    ($resolveShellBody.Value.IndexOf('poisonedContexts.TryGetValue(context, out poison)') -ge 0) -and
    ($resolveShellBody.Value.IndexOf('poisonedContexts.TryGetValue(context, out poison)') -lt
        $resolveShellBody.Value.IndexOf('GetCommand(UnitCommand.CommandType.Move)')) -and
    # The service asks the typed policy for the decision and passes exactly the observed
    # state -- it does not re-derive the ordering here.
    $resolveShellBody.Value -match '(?s)var outcome = NativeShellBindingPolicy\.Resolve\(\s*\r?\n\s*context != null,\s*\r?\n\s*contextPoisoned,\s*\r?\n\s*contextShell != null,\s*\r?\n\s*slot != null,\s*\r?\n\s*slotOwnsThisContext,\s*\r?\n\s*slotShell != null,\s*\r?\n\s*ReferenceEquals\(contextShell, slotShell\),\s*\r?\n\s*out refusal\)' -and
    # Each of the three refusal outcomes returns null, and disagreement also poisons the
    # context so it stays terminal after the Move slot disappears.
    $resolveShellBody.Value -match 'if \(outcome == NativeShellBindingOutcome\.PoisonedContext\)' -and
    $resolveShellBody.Value -match 'if \(outcome == NativeShellBindingOutcome\.Disagreement\)' -and
    $resolveShellBody.Value -match 'if \(outcome == NativeShellBindingOutcome\.NoExactOwnership\)' -and
    $resolveShellBody.Value -match 'PoisonExecutionContext\(context, contextShell, slotShell\)' -and
    ([Regex]::Matches($resolveShellBody.Value, 'NativeShellBindingOutcome\.Disagreement').Count -eq 1) -and
    # The accepted shell comes from whichever binding the policy named, never from a third source.
    $resolveShellBody.Value -match 'var shell = outcome == NativeShellBindingOutcome\.ExecutionContext \? contextShell : slotShell;' -and
    # A retired or consumed shell can never resolve again.
    $resolveShellBody.Value -match 'if \(shell\.Retired\)' -and
    $resolveShellBody.Value -match 'if \(shell\.Consumed\)' -and
    # Every permanent identity refusal retires the shell.
    ([Regex]::Matches($resolveShellBody.Value, 'RetireShell\(').Count -ge 5) -and
    # No loose lookup of any kind. Comment lines are stripped first so the guard tests
    # the code rather than the prose that describes it.
    (($resolveShellBody.Value -split "`n" | Where-Object { $_ -notmatch '^\s*//' }) -join "`n") -notmatch
        'OrderBy|\.Last\(|FirstOrDefault\(|MostRecent|\.Values') `
    'a delivery resolves only through an exact per-command binding, refuses disagreement, and retires a refused shell'
Assert-Kmc ($nativeControlsText -match 'deliveringShell\.Consumed = true;' -and
    $nativeControlsText -match 'private readonly System\.Runtime\.CompilerServices\.ConditionalWeakTable<AbilityExecutionContext, NativeRelationshipShell>') `
    'exactly-once is recorded on the delivering shell and the process binding is per-context'
$abilityLogicText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativeMountedAbilityLogic.cs')
Assert-Kmc ($abilityLogicText -match 'service\.TryDispatch\(Kind, context\?\.Caster, target\?\.Unit, context\)') `
    'Deliver passes its own exact execution context into the relationship dispatch'
$patchText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs')
Assert-Kmc ($patchText -match 'PatchExact\(typeof\(UnitUseAbility\), "OnAction", 0x06002737, Type\.EmptyTypes, null, nameof\(PatchMethods\.NativeAbilityActionPostfix\)\)' -and
    $patchText -match 'internal static void NativeAbilityActionPostfix\(UnitUseAbility __instance\)[\s\S]{0,200}BindNativeRelationshipProcess\(__instance\)') `
    'the exact OnAction boundary is patched once to establish the process binding'

# The typed shell lifecycle ledger: every early return names its failed predicate.
$prepareBody = [Regex]::Match($nativeControlsText, '(?s)internal void PrepareNativeMountApproach\(UnitUseAbility command\).*?\n        \}\r?\n')
Assert-Kmc ($prepareBody.Success -and
    $prepareBody.Value -match 'NativeShellStage\.InitRefused, null, "service-state"' -and
    $prepareBody.Value -match 'NativeShellStage\.InitRefused, null, "command-state"' -and
    $prepareBody.Value -match 'NativeShellStage\.InitRefused, null, "caster-identity"' -and
    $prepareBody.Value -match 'NativeShellStage\.InitRefused, null, "mount-target-ownership-view-profile"' -and
    $prepareBody.Value -match 'NativeShellStage\.InitRefused, null, "dismount-target-is-not-caster"' -and
    $prepareBody.Value -match 'NativeShellStage\.Registered' -and
    # Every return is accounted for: it either records its exact failed predicate or
    # is explicitly marked as not a refusal. Nothing may exit silently.
    ([Regex]::Matches($prepareBody.Value, 'return;').Count -eq
        ([Regex]::Matches($prepareBody.Value, 'RecordShellLifecycle\(NativeShellStage\.InitRefused').Count +
         [Regex]::Matches($prepareBody.Value, '// not-a-refusal:').Count))) `
    'every shell-registration early return records its exact failed predicate or is marked as no refusal'
Assert-Kmc ($nativeControlsText -match 'private string DescribeInitPredicates\(UnitUseAbility command\)' -and
    $nativeControlsText -match ';isMountBlueprint=' -and $nativeControlsText -match ';executorIsSpellCaster=' -and
    $nativeControlsText -match ';targetIsCasterPet=' -and $nativeControlsText -match ';targetMasterIsCaster=' -and
    $nativeControlsText -match ';targetProfileSupported=' -and $nativeControlsText -match ';serializationSuspended=' -and
    $nativeControlsText -match 'internal string DescribeNativeShellLifecycle\(\)') `
    'the shell lifecycle ledger publishes service, registration, serialization, command, blueprint, caster, target, ownership and profile state'

# One typed authority policy, asked by prediction AND by execution-time admission, so
# no diagnostic, automation, direct-service or queued route can reach a commitment on
# an unqualified authority. Mounting outside combat is untouched.
$authorityText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedAuthorityPolicy.cs')
Assert-Kmc ($authorityText -match 'public static bool IsQualifiedForCombatMount\(' -and
    $authorityText -match 'return pairedActivationEnabled && !unifiedMountedTurnEnabled && !pairedCommandSchedulerEnabled;' -and
    $authorityText -match 'public static string DescribeUnqualifiedCombatMount\(') `
    'the qualified combat-mount authority is one typed policy over paired activation and both retired authorities'
Assert-Kmc ($evaluatorText -match 'context\.InCombat && !context\.CombatMountAuthorityQualified' -and
    $evaluatorText -match 'public bool CombatMountAuthorityQualified \{ get; set; \} = true;' -and
    $playerActionText -match 'context\.CombatMountAuthorityQualified = MountedAuthorityPolicy\.IsQualifiedForCombatMount\(\s*\r?\n?\s*settings\.EnablePairedActivation, settings\.EnableUnifiedMountedTurn, settings\.EnablePairedCommandScheduler\)' -and
    $playerActionText -match 'context\.CombatMountAuthorityReason = MountedAuthorityPolicy\.DescribeUnqualifiedCombatMount\(') `
    'prediction gates combat Mount on the qualified authority and names the exact obstacle'
$executeMountBody = [Regex]::Match($playerActionText, '(?s)internal bool TryExecuteNativeMount\(UnitEntityData caster, UnitEntityData target, string controlIdentity\).*?\n        \}\r?\n')

# ONE TYPED ADMISSION PHASE SPLIT. Requiring adjacency at prediction and target selection
# is circular: Kingmaker's own Move-typed UnitUseAbility is what closes the distance, so
# refusing to create that command because the distance is not yet closed means the engine
# never approaches at all. That made CM02-approach-arrival unreachable. Distance is now
# DEFERRED to the approach and revalidated at delivery; nothing else is relaxed.
$admissionPhaseText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedAdmissionPhase.cs')
$admitsBody = [Regex]::Match($admissionPhaseText, '(?s)public static bool Admits\(.*?\n        \}')
Assert-Kmc ($admitsBody.Success -and
    $admissionPhaseText -match 'public enum MountedAdmissionPhase' -and
    $admissionPhaseText -match 'Approach,' -and $admissionPhaseText -match 'Transition' -and
    # Approach ignores deferred conditions; the transition does not.
    $admitsBody.Value -match 'if \(blockingReasonCount != 0\)\s*\r?\n\s*\{\s*\r?\n\s*return false;' -and
    $admitsBody.Value -match 'return phase == MountedAdmissionPhase\.Approach \|\| transitionDeferredCount == 0;' -and
    # Only the delivery boundary requires adjacency.
    $admissionPhaseText -match 'return phase == MountedAdmissionPhase\.Transition;' -and
    # The policy is pure: it reaches no engine type and no live state.
    (($admissionPhaseText -split "`n" | Where-Object { $_ -notmatch '^namespace ' }) -join "`n") -notmatch
        'using Kingmaker|UnitEntityData|Game\.Instance|UnitCommand') `
    'one typed policy separates native-approach admission from relationship-transition admission'

# Distance is the ONLY deferred condition, and it is deferred rather than dropped.
$evaluateBody = [Regex]::Match($evaluatorText, '(?s)public static MountedPlayerActionAvailability Evaluate\(MountedPlayerActionContext context\).*?\n        \}\r?\n')
Assert-Kmc ($evaluateBody.Success -and
    # Exactly one deferred-bucket add in the whole evaluator, and it is the distance one.
    ([Regex]::Matches($evaluateBody.Value, 'transitionDeferred\.Add\(').Count -eq 1) -and
    $evaluateBody.Value -match 'if \(context\.InCombat && !context\.PairAdjacent\)\s*\r?\n\s*\{\s*\r?\n\s*transitionDeferred\.Add\(MountedAdmissionPolicy\.DescribeDeferredDistance\(mountName\)\);' -and
    # Approach admission is computed through the typed policy, not by counting reasons here.
    $evaluateBody.Value -match 'MountedAdmissionPolicy\.Admits\(\s*\r?\n?\s*MountedAdmissionPhase\.Approach, reasons\.Count, transitionDeferred\.Count\)' -and
    # Non-adjacency must never be a blocking reason again.
    $evaluatorText -notmatch 'reasons\.Add\("Rider and " \+ mountName \+ " must be adjacent' -and
    # Every other combat gate still adds a BLOCKING reason.
    $evaluateBody.Value -match 'if \(context\.InCombat && !context\.CombatMountAuthorityQualified\)' -and
    $evaluateBody.Value -match 'if \(context\.InCombat && !context\.PairedAdoptionAvailable\)' -and
    $evaluateBody.Value -match 'if \(context\.InCombat && !context\.CombatTurnEligible\)' -and
    $evaluateBody.Value -match 'if \(context\.InCombat && !context\.RiderHasMoveAction &&' -and
    # The transition phase is the conjunction, so deferring can never widen into a waiver.
    $evaluatorText -match 'public bool TransitionReady => MountedAdmissionPolicy\.Admits\(\s*\r?\n?\s*MountedAdmissionPhase\.Transition, UnavailableReasons\.Count, TransitionDeferredReasons\.Count\) && IsEnabled;') `
    'distance is the single deferred condition and every other gate still blocks approach admission'

# Target selection admits on the APPROACH phase, delivery demands the TRANSITION phase and
# revalidates the measured envelope from live geometry.
Assert-Kmc ($playerActionText -match '(?s)internal bool CanNativeMountTarget\(UnitEntityData caster, UnitEntityData target\)\s*\r?\n\s*\{\s*\r?\n\s*if \(!GetNativeMountAvailability\(caster\)\.IsEnabled\)' -and
    # Availability publishes both phases from the one evaluation.
    $playerActionText -match 'return new NativeMountedControlAvailability\(\s*\r?\n\s*true, availability\.IsEnabled, availability\.TransitionReady, availability\.Feedback\);' -and
    # Delivery: the live envelope check, then the transition phase.
    $executeMountBody.Success -and
    $executeMountBody.Value -match '!CombatMountDismountPolicy\.IsAdjacent\(caster\.DistanceTo\(mount\), caster\.View\.Corpulence, mount\.View\.Corpulence\)' -and
    $executeMountBody.Value -match 'if \(!availability\.IsTransitionReady \|\| !exactTarget\)' -and
    ($executeMountBody.Value.IndexOf('CombatMountDismountPolicy.IsAdjacent') -lt
        $executeMountBody.Value.IndexOf('relationship.MountRiderOn')) -and
    # The envelope is the native one. Nothing widens the reach or the approach radius.
    $evaluatorText -match 'public const float NativeAdjacentReachMeters = 1\.5f;' -and
    $evaluatorText -match 'radius = Math\.Min\(nativeRadius, riderCorpulence \+ mountCorpulence \+' -and
    # And there is no second movement command or KMC movement engine for approach.
    $executeMountBody.Value -notmatch 'new UnitMoveTo|Commands\.Run\(') `
    'target selection admits the approach while delivery demands the transition phase and the measured envelope'
Assert-Kmc ($mountRiderBody.Success -and
    $mountRiderBody.Value -match '(?s)if \(inCombat\)[\s\S]{0,400}MountedAuthorityPolicy\.DescribeUnqualifiedCombatMount\([\s\S]{0,300}settings\.EnablePairedActivation' -and
    $mountRiderBody.Value -match 'if \(authorityRefusal != null\)' -and
    $mountRiderBody.Value.IndexOf('MountedAuthorityPolicy.DescribeUnqualifiedCombatMount') -lt
        $mountRiderBody.Value.IndexOf('coordinator.Mount(runtime.CreateCandidate(), admission)') -and
    # This gate runs AFTER Kingmaker committed the Move, so it protects the relationship,
    # never the cost. The refusal path must therefore refund, clear and zero nothing, and
    # must not book a cleanup in order to undo the committed action.
    $mountRiderBody.Value -notmatch 'Cooldown|Refund|RestoreAction|\.Prepare\(\)|ForceToEnd|RecordForcedDetach' -and
    # The refusal returns a failed TransitionResult; it does not throw the cost away.
    $mountRiderBody.Value -match 'if \(authorityRefusal != null\)[\s\S]{0,200}return Record\(new TransitionResult\(false,') `
    'execution-time admission refuses an unqualified authority before the relationship forms and refunds nothing'

# The Dismount escape hatch: a mounted or faulted rider is never stranded, and the escape
# precedes every feature gate on ALL FOUR surfaces -- leasing, availability, targeting and
# delivery -- through ONE typed policy. Mount and the mounted attack controls stay gated.
$leasePolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\NativeMountedControl.cs')
$escapePolicyBody = [Regex]::Match($leasePolicyText, '(?s)public static bool IsDismountEscape\(.*?\n        \}')
Assert-Kmc ($escapePolicyBody.Success -and
    $escapePolicyBody.Value -match 'return kind == NativeMountedControlKind\.Dismount &&\s*\r?\n\s*\(relationshipMounted \|\| relationshipFaulted\) && unitIsRider;' -and
    ([Regex]::Matches($leasePolicyText, 'public static bool IsDismountEscape\(').Count -eq 1)) `
    'the Dismount escape is exactly one typed policy over the mounted or faulted exact rider'

# Surface 1 of 4 -- leasing. The escape is asked before the feature gate, and the old
# inline copy of the predicate must be gone so there is only one decision to audit.
$leaseBody = [Regex]::Match($leasePolicyText, '(?s)public static bool ShouldLease\(\s*\r?\n\s*NativeMountedControlKind kind,\s*\r?\n\s*bool featureEnabled,\s*\r?\n\s*bool unifiedMountedTurn,.*?\n        \}')
$leaseEscapeIndex = $leaseBody.Value.IndexOf('IsDismountEscape(kind, relationshipMounted, relationshipFaulted, unitIsRider)')
$leaseGateIndex = $leaseBody.Value.IndexOf('if (!featureEnabled)')
Assert-Kmc ($leaseBody.Success -and $leaseEscapeIndex -ge 0 -and $leaseGateIndex -gt $leaseEscapeIndex -and
    $leaseBody.Value.IndexOf('(relationshipMounted || relationshipFaulted) && unitIsRider') -lt 0) `
    'a mounted or faulted rider keeps its native Dismount lease ahead of every feature gate'

# Surface 2 of 4 -- availability. Without this the fact stays VISIBLE but DISABLED once
# the movement feature is switched off, which strands the pair exactly as removing the
# lease would. The gate itself must be conditioned on the escape, not merely preceded by it.
$availabilityBody = [Regex]::Match($nativeControlsText, '(?s)internal NativeMountedControlAvailability Evaluate\(\s*\r?\n\s*NativeMountedControlKind kind,\s*\r?\n\s*UnitEntityData caster\).*?\n        \}\r?\n')
$availabilityEscapeIndex = $availabilityBody.Value.IndexOf('NativeMountedControlPolicy.IsDismountEscape(')
$availabilityGateIndex = $availabilityBody.Value.IndexOf('if (!escapeApplies && !settings.EnableUnsafeMovementExperiment)')
Assert-Kmc ($availabilityBody.Success -and $availabilityEscapeIndex -ge 0 -and
    $availabilityGateIndex -gt $availabilityEscapeIndex -and
    $availabilityBody.Value -match 'relationship\.State == RelationshipState\.Mounted,\s*\r?\n\s*relationship\.State == RelationshipState\.Faulted,\s*\r?\n\s*caster == relationship\.Rider\)' -and
    ([Regex]::Matches($availabilityBody.Value, 'if \(!settings\.EnableUnsafeMovementExperiment\)').Count -eq 0)) `
    'availability consults the same escape before the movement-feature gate, so a live pair is never visible-but-disabled'

# Surface 3 of 4 -- targeting inherits the one decision instead of re-deriving it.
$canTargetBody = [Regex]::Match($nativeControlsText, '(?s)internal bool CanTarget\(.*?\n        \}\r?\n')
Assert-Kmc ($canTargetBody.Success -and
    $canTargetBody.Value -match 'if \(!Evaluate\(kind, caster\)\.IsEnabled\)' -and
    $canTargetBody.Value.IndexOf('EnableUnsafeMovementExperiment') -lt 0 -and
    $canTargetBody.Value -match 'case NativeMountedControlKind\.Dismount:\s*\r?\n?\s*return target == caster;') `
    'targeting inherits the one escape decision through availability and never re-derives a feature gate'

# Surface 4 of 4 -- delivery. TryExecuteNativeDismount admits through the shared evaluator,
# whose Dismount branch must never consult FeatureEnabled: that branch is the live pair's
# only lawful way to separate. The Mount branch keeps its gate.
$dismountBranch = [Regex]::Match($evaluatorText, '(?s)var dismountReasons = new List<string>\(\);.*?MountedPlayerActionKind\.Dismount,\s*\r?\n\s*"Dismount",')
$faultedBranch = [Regex]::Match($evaluatorText, '(?s)if \(context\.RelationshipState == RelationshipState\.Faulted\).*?"Clear mounted state",')
Assert-Kmc ($dismountBranch.Success -and $faultedBranch.Success -and
    $dismountBranch.Value.IndexOf('FeatureEnabled') -lt 0 -and
    $faultedBranch.Value.IndexOf('FeatureEnabled') -lt 0 -and
    $evaluatorText.IndexOf('if (!context.FeatureEnabled)') -gt $dismountBranch.Index -and
    $playerActionText -match 'var availability = GetNativeDismountAvailability\(caster, true\);') `
    'delivery admits Dismount through an evaluator branch that never consults the movement feature, while Mount stays gated'

# R5: the save barrier queries the relationship transition state exactly instead of
# the documentation asserting that command settlement alone is sufficient.
$deferredSaveText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedDeferredSave.cs')
Assert-Kmc ($deferredSaveText -match 'internal bool SaveEffectsReady\(\)[\s\S]{0,2000}!controls\.HasUnsettledRelationshipTransition' -and
    $deferredSaveText -match 'Any\(controls\.OwnsUnsettledRelationshipShell\)' -and
    $nativeControlsText -match 'internal bool OwnsUnsettledRelationshipShell\(UnitCommand command\)' -and
    $nativeControlsText -match 'internal bool HasUnsettledRelationshipTransition => playerAction\.HasVoluntaryTransitionInFlight;' -and
    $nativeControlsText -match 'ability == null \|\| ability\.IsFinished') `
    'the save barrier defers on an unsettled relationship shell and on the transition ledger itself'

# Every relationship shell binds its own caster, target and generation.
Assert-Kmc ($nativeControlsText -match 'private sealed class NativeRelationshipShell' -and
    $nativeControlsText -match 'GenerationAtInit = relationship\.MountedPairGeneration' -and
    $nativeControlsText -match 'shell\.GenerationAtInit != relationship\.MountedPairGeneration' -and
    $nativeControlsText -match 'deliveringShell != null &&\s*\r?\n?\s*playerAction\.TryExecuteNativeMount' -and
    $nativeControlsText -match 'deliveringShell != null &&\s*\r?\n?\s*playerAction\.TryExecuteNativeDismount') `
    'a relationship delivery must own an exact resolved shell and its original relationship generation'

# A second player-facing relationship request is refused from the exact unsettled
# Move-slot shell. The admitted shell still delivers through the direct transition
# path, so the UI guard cannot reject its own later Deliver callback.
$repeatedRequestText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aRepeatedRequestScenario.cs')
$repeatGuardBody = [Regex]::Match($nativeControlsText, '(?s)private NativeMountedControlAvailability RefuseRepeatedRelationshipRequest\(.*?\n        \}\r?\n')
$relationshipDispatchBody = [Regex]::Match($nativeControlsText, '(?s)internal bool TryDispatch\(\s*\r?\n\s*NativeMountedControlKind kind,\s*\r?\n\s*UnitEntityData caster,\s*\r?\n\s*UnitEntityData target,\s*\r?\n\s*AbilityExecutionContext context\).*?\n        \}\r?\n')
Assert-Kmc ($leasePolicyText -match '(?s)IsRepeatedRelationshipRequest\(.*?targetSelectionActive && ownsUnsettledRelationshipShell &&\s*\r?\n\s*\(kind == NativeMountedControlKind\.MountCompanion \|\|\s*\r?\n\s*kind == NativeMountedControlKind\.Dismount\)' -and
    $availabilityBody.Value -match 'RefuseRepeatedRelationshipRequest\(\s*\r?\n\s*NativeMountedControlKind\.MountCompanion' -and
    $availabilityBody.Value -match 'RefuseRepeatedRelationshipRequest\(\s*\r?\n\s*NativeMountedControlKind\.Dismount' -and
    $repeatGuardBody.Success -and
    $repeatGuardBody.Value -match 'caster\?\.Commands\?\.GetCommand\(UnitCommand\.CommandType\.Move\)' -and
    $repeatGuardBody.Value -match 'kind,\s*\r?\n\s*targetSelectionMode,\s*\r?\n\s*OwnsUnsettledRelationshipShell\(moveSlot\)' -and
    $repeatGuardBody.Value -match 'new NativeMountedControlAvailability\(\s*\r?\n\s*true,\s*\r?\n\s*false,\s*\r?\n\s*false,' -and
    $repeatGuardBody.Value -match 'already has a pending native relationship command' -and
    $relationshipDispatchBody.Success -and
    $relationshipDispatchBody.Value -match 'deliveringShell != null &&\s*\r?\n\s*playerAction\.TryExecuteNativeMount\(caster, target, deliveringShell\.ControlIdentity\)' -and
    $relationshipDispatchBody.Value -match 'deliveringShell != null &&\s*\r?\n\s*playerAction\.TryExecuteNativeDismount\(caster, deliveringShell\.ControlIdentity\)' -and
    $relationshipDispatchBody.Value -notmatch 'RefuseRepeatedRelationshipRequest|Evaluate\(' -and
    $repeatedRequestText -match '(?s)EnsureChunk6aRiderSelection\("CM06-repeated-request"\).*?CaptureChunk6aGeometry\("repeated-request-pre-click"\).*?BeginChunk6aCommandWindow\(nativeControls\.MountAbility\.AssetGuid\).*?chunk6a-repeated-request-first-click' -and
    $repeatedRequestText -match '(?s)ApproachObserved.*?OwnsUnsettledRelationshipShell\(command\).*?availabilityBeforeSelection.*?chunk6a-repeated-request-second-click.*?availabilityDuringSelection.*?FinishChunk6aCommandWindow\("positive-mount"') `
    'repeated Mount or Dismount input is refused from the exact unsettled Move shell while that admitted shell retains direct delivery'
# The Chunk 6A runtime scenario observes native accounting and must never create
# it: no resource write, no preparation, no turn forcing except the accepted
# idle-fixture end-turn input, and no direct position or state assignment.
$chunk6aScenarioText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aCombatMountScenario.cs')
$chunk6aScenarioText += Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aCausalEvidence.cs')
$chunk6aScenarioText += Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aNativePointerScenario.cs')
Assert-Kmc ($chunk6aScenarioText -notmatch 'Cooldown\.(MoveAction|StandardAction|SwiftAction|Initiative|AttackOfOpportunity)\s*=' -and
    $chunk6aScenarioText -notmatch 'AttackOfOpportunityCount\s*=' -and
    $chunk6aScenarioText -notmatch '\.Prepare\(\)|OnNewRound|StartTurn\(|JoinCombat|ChooseNextUnit|SetIsActed|IgnoreCooldown' -and
    $chunk6aScenarioText -notmatch '\.Position\s*=' -and
    $chunk6aScenarioText -notmatch 'Translocate\(' -and
    $chunk6aScenarioText -notmatch 'EnablePairedActivation\s*=' -and
    ([regex]::Matches($chunk6aScenarioText, 'ForceToEnd\(').Count -eq 0)) `
    'the Chunk 6A scenario observes native accounting and never writes a resource, preparation, turn or position'
Assert-Kmc ($chunk6aScenarioText -match 'TryNativeAbilityTargetClick\(\s*\r?\n?\s*nativeControls\.MountAbility, horse' -and
    $chunk6aScenarioText -match 'TryNativeAbilityTargetClick\(\s*\r?\n?\s*nativeControls\.DismountAbility, rider' -and
    $chunk6aScenarioText -match 'allocationTrace\.GrantCount\(actor\)' -and
    $chunk6aScenarioText -match 'handler\.SetAbility\(data\);\s*\r?\n\s*handler\.DropAbility\(\);') `
    'the Chunk 6A scenario drives the normal native control path and reads native preparation counts'
Assert-Kmc ($chunk6aScenarioText -match 'if \(!settings\.EnablePairedActivation \|\| settings\.EnableUnifiedMountedTurn \|\|' -and
    $chunk6aScenarioText -match 'settings\.EnableDiagnosticOverlay \|\|\s*\r?\n?\s*playerAction\.OverlayPresent') `
    'the Chunk 6A scenario refuses to run outside the accepted single paired authority and without the overlay off'

# The diagnostic adoption fault exists only to make the compensating path
# observable in the running game. It must stay bounded, owned and incapable of
# widening anything: one at a time, only on an idle unmounted pair, bound to two
# exact distinct actor identities, consumed once, and writing nothing.
$armFaultBody = [Regex]::Match($adoptionText, '(?s)internal IDisposable ArmMidEncounterAdoptionFault\(UnitEntityData rider, UnitEntityData mount\).*?\n        \}\r?\n')
$consumeFaultBody = [Regex]::Match($adoptionText, '(?s)private string ConsumeAdoptionFault\(UnitEntityData rider, UnitEntityData mount\).*?\n        \}\r?\n')
Assert-Kmc ($armFaultBody.Success -and $consumeFaultBody.Success -and
    $armFaultBody.Value -match '!PairedLifecycleEnabled' -and
    $armFaultBody.Value -match 'relationship\.State != RelationshipState\.Unmounted \|\| activation != null \|\| partnerContext != null' -and
    $armFaultBody.Value -match 'ReferenceEquals\(rider, mount\)' -and
    $armFaultBody.Value -match 'adoptionFault != null' -and
    $armFaultBody.Value -notmatch 'Cooldown\.|\.Prepare\(\)|ForceToEnd|NativeEnd|StartTurn|JoinCombat' -and
    $consumeFaultBody.Value -match 'fault\.Consumed' -and
    $consumeFaultBody.Value -match 'fault\.Consumed = true;' -and
    $consumeFaultBody.Value -notmatch 'Cooldown\.|\.Prepare\(\)|ForceToEnd|NativeEnd|StartTurn|JoinCombat') `
    'the diagnostic adoption fault is bounded to one idle exact pair, consumed once and writes nothing'
Assert-Kmc ($adoptBody.Success -and
    $adoptBody.Value -match '(?s)var injected = ConsumeAdoptionFault\(rider, mount\);[\s\S]{0,120}return injected;[\s\S]{0,400}plan\.RelationshipGeneration != relationship\.MountedPairGeneration') `
    'an injected adoption fault refuses before any state change and before the partner preparation'
Assert-Kmc ($chunk6aScenarioText -match 'private void Chunk6aDisposeAdoptionFault\(\)' -and
    ([Regex]::Matches($chunk6aScenarioText, 'ArmMidEncounterAdoptionFault\(').Count -eq 1) -and
    $chunk6aScenarioText -match 'CM02-adoption-plan-invalidated' -and
    $chunk6aScenarioText -match 'CM01-combat-mount-preparing-refused' -and
    (Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Phase3dHorseScenarioTranche.cs')) -match 'Chunk6aDisposeAdoptionFault\(\); \}') `
    'the Chunk 6A scenario arms the adoption fault exactly once and disarms it on every cleanup path'

# THREE MEASUREMENT DEFECTS the first completed approach run exposed. None was a product
# defect: the transition behaved correctly every time and the scenario's own expectations
# were wrong. Each is pinned here so it cannot come back.
$chunk6aScenarioText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aCombatMountScenario.cs')
$chunk6aScenarioText += Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aCausalEvidence.cs')
$chunk6aScenarioText += Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aNativePointerScenario.cs')

# The committed command rows use the complete event-backed resource window. The old
# endpoint helper treated the initiative timer as ordering and rejected lawful RT decay.
Assert-Kmc ($chunk6aScenarioText -match 'var riderOtherResourcesHeld = \(bool\)compensationProof\["resourceWindow"\]\["pass"\];' -and
    $chunk6aScenarioText -match 'var riderOtherResourcesHeld = \(bool\)mountProof\["resourceWindow"\]\["pass"\];' -and
    $chunk6aScenarioText -match 'var mountResourcesHeld = \(bool\)mountProof\["resourceWindow"\]\["pass"\];') `
    'committed Mount and compensation conservation use exact event-backed resources including native initiative decay'

# 1. Resource conservation is MODE AWARE. In turn-based a cooldown is static between
# boundaries so exact equality is right; in real time cooldowns tick down continuously, and
# a run measured a rider's Standard falling 4.447 -> 3.084 across one correct mount. A
# CHARGE RAISES a cooldown, so the real-time invariant is "never increases". Reaction and
# initiative ordering stay exact; initiative cooldown is a separate native timer.
$resourcesHeldBody = [Regex]::Match($chunk6aScenarioText,
    '(?s)private static bool Chunk6aResourcesHeld\(.*?\n        \}')
Assert-Kmc ($resourcesHeldBody.Success -and
    $resourcesHeldBody.Value -match 'var exact = turnBased \|\| name == "reactions" \|\| name == "reactionsPerRound" \|\| name == "initiativeOrder";' -and
    $resourcesHeldBody.Value -match 'if \(!JToken\.DeepEquals\(before\[name\], after\[name\]\)\) \{ return false; \}' -and
    $resourcesHeldBody.Value -match 'if \(\(float\)after\[name\] > \(float\)before\[name\] \+ 0\.0001f\) \{ return false; \}' -and
    # The older exact-equality helper now delegates, so no caller can bypass the mode.
    $chunk6aScenarioText -match 'private bool Chunk6aUnchangedExcept\(JObject before, JObject after, params string\[\] allowedToRise\)\s*\r?\n\s*\{\s*\r?\n\s*return Chunk6aResourcesHeld\(before, after, Chunk6aTurnBased, allowedToRise\);' -and
    # Committed Mount/compensation rows additionally require the native event proof above.
    $resourcesHeldBody.Value -match '"initiativeCooldown", "initiativeOrder", "reactionCooldown", "reactions", "reactionsPerRound"') `
    'native resource conservation is measured per mode: exact in turn-based, never-increasing in real time'

# 2. Every transition ledger claim is a WINDOW DELTA. This scenario legitimately performs an
# exploration Mount and Dismount before exactly one combat request; compensation runs in
# its own fresh allocation. An absolute "acceptedMount == 1" describes an earlier design of the
# scenario rather than what any one transition did.
$absoluteLedgerClaims = @([Regex]::Matches($chunk6aScenarioText,
    'TransitionLedger\.(?:Accepted|Admitted|ForcedDetach|RefusedVoluntary|DuplicateControlSuppressed|ConcurrentControlSuppressed)[A-Za-z]*Count == \d'))
Assert-Kmc ($chunk6aScenarioText -match 'private JObject Chunk6aLedgerCounters\(\)' -and
    $chunk6aScenarioText -match 'private bool Chunk6aLedgerDelta\(JObject before, string name, long expected\)' -and
    $chunk6aScenarioText -match 'return \(long\)Chunk6aLedgerCounters\(\)\[name\] - \(long\)before\[name\] == expected;' -and
    # Each measured window captures its own baseline.
    $chunk6aScenarioText -match 'chunk6aExplorationLedgerBefore = Chunk6aLedgerCounters\(\);' -and
    $chunk6aScenarioText -match 'chunk6aMountLedgerBefore = Chunk6aLedgerCounters\(\);' -and
    $chunk6aScenarioText -match 'chunk6aCompensationLedgerBefore = Chunk6aLedgerCounters\(\);' -and
    $chunk6aScenarioText -match 'chunk6aRepeatLedgerBefore = Chunk6aLedgerCounters\(\);' -and
    $chunk6aScenarioText -match 'chunk6aDismountLedgerBefore = Chunk6aLedgerCounters\(\);' -and
    ([Regex]::Matches($chunk6aScenarioText, 'Chunk6aLedgerDelta\(').Count -ge 15) -and
    # The combat mount window pins exactly one admitted and one accepted mount, no forced
    # detach and no refusal of its own.
    $chunk6aScenarioText -match 'Chunk6aLedgerDelta\(chunk6aMountLedgerBefore, "acceptedMount", 1\)' -and
    $chunk6aScenarioText -match 'Chunk6aLedgerDelta\(chunk6aMountLedgerBefore, "forcedDetach", 0\)' -and
    # The compensation window pins its refusal and its single cleanup detach.
    $chunk6aScenarioText -match 'Chunk6aLedgerDelta\(chunk6aCompensationLedgerBefore, "acceptedMount", 0\)' -and
    $chunk6aScenarioText -match 'Chunk6aLedgerDelta\(chunk6aCompensationLedgerBefore, "refusedVoluntary", 1\)' -and
    # No absolute equality claim on a cumulative ledger counter survives anywhere, except the
    # exploration Mount, which really did happen exactly once before its window opened.
    ($absoluteLedgerClaims.Count -le 2)) `
    'every transition ledger claim is measured as a window delta rather than a cumulative total'

# The exact native Tick transition, never an endpoint surrogate, owns acted evidence.
$causalProbeText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\NativeRelationshipCommandProbe.cs')
Assert-Kmc ($chunk6aScenarioText -notmatch 'actedObserved\s*\|\|\s*moveCommitted' -and
    $causalProbeText -match 'ReferenceEquals\(active.Command, __instance\) && !__state && __instance.IsActed' -and
    $causalProbeText -match 'active.Observe\("acted", active.Command\)' -and
    $causalProbeText -match 'identity.Matches\(sample.Identity, early\)' -and
    $chunk6aScenarioText -match 'var actedObserved = \(bool\)mountProof\["exactActedObserved"\]' -and
    $chunk6aScenarioText -match 'oneRiderOwnedApproach && actedObserved && sameCommand && oneDelivery') `
    'one exact command owns approach, observed acted, native cost, process, delivery and terminal evidence'

$mountTurnWait = [Regex]::Match($chunk6aScenarioText, '(?s)if \(chunk6aStage == 1\)(.*?)var availability = nativeControls.Evaluate')
Assert-Kmc ($mountTurnWait.Value -match 'if \(TickChunk6aMountSlotExpenditure\(turn\)\) return;\s*if \(turn\?\.Unit != rider\)\s*\{\s*TryEndPhase3gFixtureTurn\(turn\);\s*return;\s*\}\s*if \(!PrepareChunk6aActingEntry\(turn\)\) return;' -and
    $mountTurnWait.Value -notmatch 'turn\?\.Unit != rider \|\| !turn.IsActing') `
    'Chunk 6A preserves the exact rider Preparing turn and ends only other fixture actors while waiting for Acting'

$actingSetupText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aTurnSetup.cs')
Assert-Kmc ($actingSetupText -match 'EnsureChunk6aRiderSelection' -and
    $actingSetupText -match '(?s)CaptureChunk6aState\("native-acting-setup-before"\).*?ClickGroundHandler.MoveSelectedUnitsToPoint.*?chunk6aActingSetupCommand = rider.Commands.Move as UnitMoveTo' -and
    $actingSetupText -match 'ReferenceEquals\(turn, chunk6aActingSetupTurn\)' -and
    $actingSetupText -match 'allocationTrace.EventsSince\(chunk6aActingSetupTraceStart\)' -and
    $actingSetupText -match 'chunk6aActingSetupCommand.Result != UnitCommand.ResultType.Success \|\| !turn.IsActing' -and
    $actingSetupText -notmatch 'ForceToEnd|Cooldown.*=|\.Status\s*=|\.Prepare\(|\.Clear\(|\.Position\s*=') `
    'native ground setup reaches Acting on the same rider turn and publishes actual carried debt without resource or turn writes'

$actingPolicyText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeGroundFixturePolicy.cs')
Assert-Kmc ($actingSetupText -match 'NativeGroundFixturePolicy.ActingProposals' -and
    $actingSetupText -match 'observations\["chunk6aActingDestinationSearch"\]' -and
    $actingSetupText -match 'CaptureFootprint\(rider, point\)' -and
    $actingSetupText -match 'NativeGroundFixturePolicy.IsActingStep' -and
    $actingSetupText -notmatch 'FindWalkablePoint\(' -and
    $actingPolicyText -match 'Math.Abs\(travel - 0.6\) <= 0.15' -and
    $actingPolicyText -match 'separation >= beforeSeparation && separation <= beforeSeparation \+ 0.15') `
    'TB native setup proposes points from exact pair geometry and retains unchanged bounds with every native rejection'
$fullDoorText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aDoorFixture.cs')
$fullObstructionText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aObstructionScenario.cs')
$campaignText=Get-Content -Raw (Join-Path $repoRoot 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
$ledgerText=Get-Content -Raw (Join-Path $repoRoot 'scripts/Test-Chunk6aLedger.ps1')
Assert-Kmc ($fullDoorText -match 'private bool Chunk6aNeedsDoor => Chunk6aObstructionOnly;' -and
    $fullObstructionText -match 'if \(!Chunk6aObstructionOnly\) throw' -and
    $fullObstructionText -notmatch 'CloseChunk6aDoor|chunk6aStage == 6|TickChunk6aGeometryChange' -and
    $chunk6aScenarioText -match 'one-positive-mount-and-dismount-with-separate-campaign-support' -and
    $ledgerText -match 'Assert-KmcChunk6aCampaign' -and
    $campaignText -match "name='geometry';scenario='chunk6a-geometry-change'" -and
    $campaignText -match "name='obstruction';scenario='chunk6a-obstruction'" -and
    $campaignText -match 'Assert-KmcCompositeRoleSet') `
    'full native cases retain mandatory geometry and obstruction through fixed ordered fresh-transaction campaign evidence'

$positiveFlow = [Regex]::Match($chunk6aScenarioText, '(?s)if \(chunk6aStage == 13\)(.*?)if \(chunk6aStage == 2\)')
Assert-Kmc ($chunk6aScenarioText -match 'manager.SelectUnit\(rider.View, true, true, false\)' -and
    $chunk6aScenarioText -match 'selectedUnits != null && selectedUnits.Count == 1 && selectedUnits\[0\] == rider' -and
    $chunk6aScenarioText -match 'Exact rider selection failed before SetAbility or OnClick' -and
    $positiveFlow.Value -match '(?s)if \(!EnsureChunk6aRiderSelection\("CM02-approach-arrival"\)\) return;.*?chunk6aPreMount = CaptureChunk6aState\("mount-before"\);.*?chunk6aApproachStart = CaptureChunk6aGeometry\("positive-pre-click"\);.*?chunk6aMountLedgerBefore = Chunk6aLedgerCounters\(\);.*?chunk6aMountClicked = Chunk6aHotbarOnly \? InvokeChunk6aHotbar\(\) : TryNativeAbilityTargetClick') `
    'positive Mount selects and verifies the exact single rider before resource ledger geometry baseline and native input'

Assert-Kmc ($chunk6aScenarioText -match 'chunk6aStage = Chunk6aAutoUseOnly && !Chunk6aAutoUseDismount \? 31 : Chunk6aCompensationOnly \? 11 : Chunk6aRefusedOnly \? 24 : Chunk6aStopOnly \? 22 : Chunk6aCombatEndOnly \? 70 : Chunk6aDisableOnly \? 70 : Chunk6aReplacementOnly \? 34 : Chunk6aRepeatedRequestOnly \? 45 : Chunk6aOwnershipOnly \? 36 : Chunk6aSizeFormOnly \? 38 : Chunk6aLostDirectControlOnly \? 40 : Chunk6aPendingIncapacityOnly \? 43 : Chunk6aGeometryOnly \? 16 : Chunk6aObstructionOnly \? 18 : Chunk6aRiderExhaustOnly \? 50 : 13;' -and
    $chunk6aScenarioText -match 'if \(!Chunk6aCompensationOnly\) throw' -and
    $positiveFlow.Value -match 'if \(Chunk6aCompensationOnly\) throw' -and
    [Regex]::Match($chunk6aScenarioText, '(?s)if \(chunk6aStage == 12\)(.*?)// Stage 13:').Value -match 'chunk6aStage = 99;\s*BeginCleanup\(\)' -and
    [Regex]::Match($chunk6aScenarioText, '(?s)if \(chunk6aStage == 12\)(.*?)// Stage 13:').Value -notmatch 'chunk6aStage = 13') `
    'compensation and positive Mount use separate fresh scenario allocations'

$setupGeometry = [Regex]::Match($chunk6aScenarioText, '(?s)if \(!Chunk6aCompensationOnly\)\s*\{(.*?)\n                \}\s*else\s*\{(.*?)\n                \}')
Assert-Kmc ($setupGeometry.Success -and
    $setupGeometry.Groups[1].Value -match 'Chunk6aSendHorseAway\(4f' -and
    $setupGeometry.Groups[1].Value -match 'chunk6aSeparationCommand.Result != UnitCommand.ResultType.Success' -and
    $setupGeometry.Groups[1].Value -match 'geometry\["isAdjacent"\]' -and
    $setupGeometry.Groups[2].Value -match 'compensation-fresh-allocation-geometry' -and
    $setupGeometry.Groups[2].Value -notmatch 'Chunk6aSendHorseAway|MoveSelectedUnits|TryNativeAbilityTargetClick' -and
    $positiveFlow.Value -match 'chunk6aApproachStart\["isAdjacent"\]') `
    'compensation keeps measured geometry without an unrelated separation Move while positive approach still requires non-adjacency'
Assert-Kmc ($setupGeometry.Groups[1].Value -match 'CaptureOrdinaryCommand\(chunk6aSeparationCommand\)' -and
    $setupGeometry.Groups[1].Value -match 'NativeGroundMovementObservation.Capture\(horse, chunk6aSeparationCommand\)' -and
    $setupGeometry.Groups[1].Value -match 'separation.ToString\(Formatting.None\)') `
    'pre-encounter separation failures retain exact command result destination geometry and native path observations'

Assert-Kmc ($chunk6aScenarioText -match 'BeginChunk6aCommandWindow\(nativeControls.MountAbility.AssetGuid\)' -and
    $chunk6aScenarioText -match 'FinishChunk6aCommandWindow\("exploration-mount", false, 0, false\)' -and
    $chunk6aScenarioText -match 'FinishChunk6aCommandWindow\("exploration-dismount", false, 0, false\)' -and
    (Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionUnmountedScenarioEngine.cs')) -match '(?s)private void BeginMountedAlpha\(\).*?IsChunk6aCombatMountScenario\(request.Scenario\).*?BeginPhase3dTranche\(false\);\s*return;' -and
    $chunk6aScenarioText -match 'carried counters are published independently') `
    'exploration Mount and Dismount each own a pre-click to terminal callback window'

$handoffText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/HorseCompanionUnmountedScenarioEngine.cs')
$admissionBody = [Regex]::Match($handoffText, '(?s)private void AwaitMountedAlphaAdmission\(\)(.*?)private void BeginMountedAlpha\(\)')
Assert-Kmc ($admissionBody.Value -match 'owner.Group.Any\(member => member.IsInCombat\)' -and
    $admissionBody.Value -match '(?s)chunk6aPartyHandoff.*?MountedAlphaAdmissionTimeoutSeconds.*?BeginCleanup\(\); return;.*?BeginMountedAlpha\(\)') `
    'Chunk 6A waits for the whole native party to leave combat before child observer construction'
$reactionEvidenceText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeRelationshipReactionEvidence.cs')
$resourceIdentityText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/RelationshipCommandIdentity.cs')
Assert-Kmc ($causalProbeText -match 'EvaluateReactionResources\(events, expectedPartnerPreparations\)' -and $causalProbeText -match '\(bool\)reactions\["pass"\] && oneSequence' -and $reactionEvidenceText -match 'native-time-and-declared-partner-preparation-only' -and $reactionEvidenceText -match 'actual.Matches\(current\)' -and $reactionEvidenceText -match 'expectedInitiativeOrder' -and $reactionEvidenceText -match 'actual.InitiativeOrder != expectedOrder' -and $reactionEvidenceText -match 'ClearInitiativeOnly\(' -and $reactionEvidenceText -match 'LeaveCombat\(' -and $reactionEvidenceText -match 'actual.Tick\(' -and $resourceIdentityText -match 'Allowance == other.Allowance &&\s*InitiativeOrder == other.InitiativeOrder' -and $resourceIdentityText -match 'ClearInitiativeOnly\(\) =>\s*new NativeReactionResources\(Allowance, Cooldown, 0, InitiativeOrder\)' -and $resourceIdentityText -match 'LeaveCombat\(\) =>\s*new NativeReactionResources\(Allowance, Cooldown, InitiativeCooldown, 0\)' -and $resourceIdentityText -match 'IncapacityActionBridge\(') 'resourceWindow PASS requires both actors reaction allowance and cooldown proof with separate initiative ordering'

# 4. A native resource the engine is still RESTORING is waited for, never written. The
# fresh encounter may still be draining native resource debt from its own earlier setup.
# A positive Mount is issued only after native readiness, and never after compensation; it becomes lawful
# once Kingmaker drains the Move cooldown it charged. A run clicked combat Mount with 0.045s
# of that cooldown left and was correctly refused with "The rider has no Move action
# available to mount." Waiting for the engine is the whole of the repair; zeroing, clearing
# or refunding the cooldown is exactly what the mounted-cost contract forbids.
$moveReadinessText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedNativeMoveReadiness.cs')
Assert-Kmc ($moveReadinessText -match 'public static MountedNativeMoveReadiness Decide\([^)]*bool hasMoveAction, float moveCooldownSeconds, bool turnBased\)' -and
    # Turn-based play never waits: the cooldown is static between boundaries and forcing a
    # turn boundary to recover a Move is prohibited outright.
    $moveReadinessText -match 'if \(turnBased\)\s*\{\s*return MountedNativeMoveReadiness\.Unavailable;' -and
    # Only a positive draining real-time cooldown counts as a restoration in progress, so a
    # drained, negative or not-a-number reading refuses instead of waiting for ever.
    $moveReadinessText -match 'return moveCooldownSeconds > 0f\s*\?\s*MountedNativeMoveReadiness\.RestoringOnNativeClock\s*:\s*MountedNativeMoveReadiness\.Unavailable;' -and
    # The decision is pure: it reads no live state and touches no native resource.
    $moveReadinessText -notmatch 'using Kingmaker\.' -and
    $moveReadinessText -notmatch 'Game\.Instance|UnitEntityData|CombatState|\.MoveAction' -and
    # The scenario consults it before the click, waits only while the engine is restoring,
    # and reports the exact refusal when nothing will restore the resource.
    $chunk6aScenarioText -match 'var moveReadiness = MountedNativeMoveReadinessPolicy\.Decide\(' -and
    $chunk6aScenarioText -match 'if \(MountedNativeMoveReadinessPolicy\.ShouldWait\(moveReadiness\)\)' -and
    $chunk6aScenarioText -match 'if \(moveReadiness == MountedNativeMoveReadiness\.Unavailable\)' -and
    # And the observed wait is published, so the evidence shows the resource came back on
    # Kingmaker's own clock rather than from anything the scenario did.
    $chunk6aScenarioText -match 'observations\["chunk6aMoveRestorationWait"\] = chunk6aMoveRestorationWait;') `
    'a native Move the engine is still restoring is waited for and never written'

# Genuine obstruction is a separate negative contract. Stop cannot manufacture its terminal.
$chunk6aObstruction = Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aObstructionScenario.cs')
$chunk6aUnacted = Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeRelationshipUnactedEvidence.cs')
Assert-Kmc ($chunk6aObstruction -match 'AddRow\("CM02-obstruction", pass' -and
    $chunk6aObstruction -match 'chunk6aPath.FailureObserved' -and
    $chunk6aObstruction -match 'Chunk6aDoorSettled\(false\)' -and
    $chunk6aObstruction -notmatch 'SelectionManager.Instance.Stop' -and
    $chunk6aUnacted -match '!Command.IsActed && Command.ExecutionProcess == null' -and
    $chunk6aUnacted -match 'EvaluateReactionResources\(events, 0, incapacityActor\)' -and
    $chunk6aUnacted -match '!hasClear \|\| incapacityContract && incapacityClearPass' -and
    $chunk6aUnacted -match 'ledger.Properties\(\).All\(p => \(long\)p.Value == 0\)') `
    'native obstruction requires path failure, exact unacted terminal, zero ledger/cost/preparation deltas and reaction proof'

# Geometry changes during a separately identified native Mount approach. Arrival
# is observed before attachment; cooldown endpoints never replace exact acted/cost.
$chunk6aRowRequirementText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1')
$obstructionValidatorBody = [Regex]::Match($chunk6aRowRequirementText, '(?s)function Assert-KmcChunk6aObstruction \{.*?\n\}').Value
Assert-Kmc ($evaluatorText.Contains('public const float NativeAdjacentReachMeters = 1.5f;') -and
    $obstructionValidatorBody.Contains('$envelope=[double]$g.riderCorpulence+[double]$g.horseCorpulence+1.5') -and
    $obstructionValidatorBody.Contains('[Math]::Abs($envelope-[double]$g.legalAdjacencyEnvelope) -gt 0.0001') -and
    $obstructionValidatorBody.Contains('$Case.closedDoorObservations -lt 1')) `
    'obstruction rederives the frozen native 1.5m reach with unchanged geometry and closed-door thresholds'
$geometryText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aGeometryChangeScenario.cs')
$groundProofText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\NativeRelationshipGroundOrder.cs')
$chunk6aTrancheText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Phase3dHorseScenarioTranche.cs')
Assert-Kmc ($geometryText -match 'AddRow\("CM02-geometry-change",' -and
    $geometryText -match 'command == null \|\| command.IsActed \|\| command.IsFinished' -and
    $geometryText -match '!chunk6aCommandWindow.ApproachObserved \|\| !rider.View.AgentASP.IsReallyMoving \|\| riderDisplacement <= 0.25f' -and
    $geometryText -match 'rider.Commands.GetCommand\(UnitCommand.CommandType.Move\)' -and
    $geometryText -match 'ReferenceEquals\(moveSlot, command\)' -and
    $geometryText -match 'CaptureOrdinaryCommand\(moveSlot\)' -and
    $geometryText -notmatch 'rider.Commands.Move' -and
    $geometryText.IndexOf('chunk6aGeometryChangeEvidence["trigger"]') -lt $geometryText.IndexOf('!ReferenceEquals(moveSlot, command)') -and
    $geometryText -match 'FinishChunk6aCommandWindow\("geometry-change-mount", true, 0, true\)' -and
    $geometryText -match '\(string\)sample\["boundary"\] == "deliver"' -and
    $geometryText -match 'horseDisplacement > Chunk6aStationaryToleranceMeters' -and
    $geometryText -match 'oneRequest && \(accepted \|\| refused\)' -and
    $geometryText -notmatch 'IsStarted|MoveCostRetained|MaxRiderMoveCooldown|\.Position\s*=[^=]|transform\.position\s*=[^=]' -and
    $geometryText.IndexOf('EnsureChunk6aRiderSelection("CM02-geometry-change")') -lt $geometryText.IndexOf('CaptureChunk6aState("geometry-change-before")') -and
    $chunk6aScenarioText -match 'destination = FindChunk6aGroundDestination\(extraMeters\)' -and
    $chunk6aScenarioText -match 'ClickGroundHandler\.MoveSelectedUnitsToPoint\(destination, false\);' -and
    $chunk6aTrancheText -match 'chunk6aGeometryChangeCommand\?\.Interrupt\(\);') `
    'geometry uses observed pre-acted native movement and pre-attachment delivery with its own exact command proof'
$mountInterruptionText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Integration/NativeMountedApproachInterruption.cs')
Assert-Kmc ($mountInterruptionText -match 'actor.Commands.GetCommand\(UnitCommand.CommandType.Move\)' -and
    $mountInterruptionText -match 'PointerController.SimulatingClick' -and
    $mountInterruptionText -match 'shell.Kind != NativeMountedControlKind.MountCompanion' -and
    $mountInterruptionText -match 'command.IsStarted \|\| command.IsActed \|\| command.IsFinished \|\| command.ExecutionProcess != null' -and
    $mountInterruptionText -match 'ReferenceEquals\(command, CaptureNativeMountApproachInterruption\(view\)\)' -and
    $mountInterruptionText.IndexOf('RetireShell(shell, "native-movement-interrupted")') -lt $mountInterruptionText.IndexOf('command.Interrupt();') -and
    $mountInterruptionText -notmatch 'ForceFinish|UpdateCooldowns|Cooldowns.Clear|\.(?:IsActed|IsFinished|Result)\s*=[^=]|\.Position\s*=[^=]' -and
    $patchText -match 'nameof\(PatchMethods.NativeMountMovementInterruptedPrefix\), nameof\(PatchMethods.NativeMountMovementInterruptedPostfix\)') `
    'native movement interruption retires only the exact captured unacted Mount and delegates terminal state to native Interrupt'
$groundSetupText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aGroundSetup.cs')
$pathContentsText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeCommandPathProbe.cs')
Assert-Kmc ($groundSetupText -match 'NativeGroundFixturePolicy.IsClear' -and
    $groundSetupText -match 'CaptureFootprint\(horse, point\)' -and
    $groundSetupText -match 'ObstacleAnalyzer.TraceAlongNavmesh\(origin, point\)' -and
    $groundSetupText -match 'unit != horse && unit.IsInState' -and
    $groundSetupText -match 'index < 24' -and
    $groundSetupText -notmatch '\.Position\s*=[^=]|transform\.position\s*=[^=]|UpdateCooldowns|Cooldowns.Clear') 'Chunk 6A ground setup selects a bounded clear route and full footprint without actor writes'
Assert-Kmc ($pathContentsText -match 'var contents = CapturePathContents\(path, boundary\)' -and
    $pathContentsText -match 'var ready = boundary == "path-complete-before"' -and
    $pathContentsText -match '!ready \|\| path\?\.vectorPath == null' -and
    $pathContentsText -match '\["points"\] = contents\["points"\]') 'native path requests capture identity only until the completion callback owns stable contents'

Assert-Kmc ($groundProofText -match 'command.GetType\(\) != typeof\(UnitMoveTo\) \|\| command.Executor != mount' -and
    $groundProofText -match '!command.CreatedByPlayer \|\| !command.IsIgnoreCooldown' -and
    $groundProofText -match 'CombatController.IsInTurnBasedCombat\(\)' -and
    $groundProofText -match 'JToken.DeepEquals\(before\[0\]\["state"\]\[field\], after\[0\]\["state"\]\[field\]\)' -and
    $groundProofText -match '"reactionCooldown", "reactions", "reactionsPerRound"' -and
    $chunk6aScenarioText -notmatch 'DeclareGeometryGroundOrder' -and
    $chunk6aRowRequirementText -match 'Auxiliary ground order is forbidden in this relationship window' -and
    $chunk6aRowRequirementText -match 'Geometry ground callback wrote' -and
    $chunk6aRowRequirementText -match 'Geometry arrival is not the exact pre-attachment delivery sample') `
    'only the geometry case declares one exact RT ground command and independently proves its callbacks write no resource'

# The harness RE-DERIVES resource conservation, so it has to use the same mode-aware
# instrument the scenario does. A run failed with "Chunk 6A control refunded rider standard
# debt" because the validator treated any fall as a refund. In real time Kingmaker drains
# every cooldown against its own clock, and one correct combat Mount was measured taking the
# rider's standard cooldown from 4.447 to 3.084. A refund is a fall FASTER than the clock,
# and a charge is a cooldown that ROSE.
Assert-Kmc ($chunk6aRowRequirementText -match 'function Assert-KmcRelationshipCommandProof' -and
    $chunk6aRowRequirementText -match 'Native cost callback count is not one exact Move sequence' -and
    $chunk6aRowRequirementText -match 'Mixed causal identity at' -and
    $chunk6aRowRequirementText -match 'Native acted was not observed on this exact command' -and
    $chunk6aRowRequirementText -match 'Unexpected preparation, cooldown clear or reset' -and
    $chunk6aRowRequirementText -match 'Resource window refunded or added') `
    'the harness rederives exact identity, native callback ownership and RT/TB resource windows'

# The diagnostic encounter stays alive only while the bidirectional combat-memory lease keeps
# refreshing, and the tick used to DISCARD that result. A run lost combat between the combat
# Mount and the combat Dismount with nothing recording why; the Dismount then ran out of
# combat, where Kingmaker correctly charges nothing, and the row failed on a derived
# move-commitment clause instead of the real cause. Liveness is now observed, and the
# encounter is a stated precondition of the combat Dismount.
Assert-Kmc ($chunk6aTrancheText -match 'var combatMemoryRefreshed = targetService\?\.RefreshBidirectionalCombatMemoryLease\(\);' -and
    $chunk6aTrancheText -match 'if \(IsChunk6aCombatMount\) \{ ObserveChunk6aEncounterLiveness\(combatMemoryRefreshed\); \}' -and
    $chunk6aScenarioText -match 'private void ObserveChunk6aEncounterLiveness\(bool\? combatMemoryRefreshed\)' -and
    # Bounded: the first lapse of each kind plus running counts, never a per-frame log.
    $chunk6aScenarioText -match '\["firstRefreshFailure"\] = Chunk6aLivenessMark\(live\);' -and
    $chunk6aScenarioText -match '\["firstCombatLoss"\] = Chunk6aLivenessMark\(false\);' -and
    # The mark names which lease stopped validating, so the cause is attributable.
    $chunk6aScenarioText -match '\["targetBrainLeaseReleased"\] = targetService\?\.TargetBrainLeaseReleased,' -and
    $chunk6aScenarioText -match '\["targetSleeplessLeaseReleased"\] = targetService\?\.TargetSleeplessLeaseReleased,' -and
    $chunk6aScenarioText -match '\["targetDurabilityLeaseReleased"\] = targetService\?\.TargetDurabilityLeaseReleased,' -and
    # And a combat Dismount is only claimed while the encounter is actually live.
    $chunk6aScenarioText -match 'if \(!rider\.IsInCombat \|\| !horse\.IsInCombat \|\| !Game\.Instance\.Player\.IsInCombat\)' -and
    $chunk6aScenarioText -match 'The encounter ended before the combat Dismount became available') `
    'the encounter liveness that keeps the diagnostic combat alive is observed, not assumed'
Assert-Kmc ($chunk6aScenarioText -match '(?s)private void ObserveChunk6aEncounterLiveness\(bool\? combatMemoryRefreshed\).*?if \(cleanupStarted\) \{ return; \}.*?var live =' -and
    $chunk6aTrancheText -match '\["firstRuntimeException"\]' -and $chunk6aTrancheText -match 'exception.ToString\(\)') `
    'released Chunk 6A observers stop before native access and preserve the first exception with its command evidence'

$approachPathText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeMountApproachPathProbe.cs')
$approachMovementText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeApproachMovementTrace.cs')
Assert-Kmc ($approachPathText -match 'ReferenceEquals\(__instance,p.command\)' -and
    $approachPathText -match 'unitView!=p.rider.View' -and
    $approachPathText -match 'var copy=points==null\?null:points.ToArray\(\)' -and
    $approachPathText -match '\["path"\]=consumed\?SnapshotConsumedPath\(path\):null' -and
    $approachPathText -notmatch '\.(?:ForcedPath|Path|NextApproachTime|Position)\s*=[^=]|\.ClearPath\(|\.FollowPrecomputedPath\(|\.PathTo\(|UpdateCooldowns|Cooldowns.Clear') 'TB Mount path observation binds exact command and native receivers, copies consumed geometry and performs no native path or resource write'
Assert-Kmc ($approachMovementText -match 'out ApproachMovementCall __state' -and
    $approachMovementText -match 'ReferenceEquals\(__instance, __state.Turn\)' -and
    $approachMovementText -match 'RecordApproachMovement\("approach-movement-after", __state, deltaTime\)' -and
    $approachMovementText -notmatch '(?:deltaTime|\.TimeMoved|\.MoveAction|\.StandardAction|\.SwiftAction)\s*=[^=]|UpdateCooldowns|Cooldowns.Clear|\.Prepare\(' -and
    $chunk6aRowRequirementText -match 'Assert-KmcNativeMovementEvidence' -and
    $chunk6aRowRequirementText -match 'Assert-KmcNativeMountApproachPath') 'TB approach debt is explained by exact native ref-delta callbacks and independently replayed without writes'
$pointerScenarioText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aNativePointerScenario.cs')
$pointerInputText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeMountPointerInput.cs')
Assert-Kmc ($positiveFlow.Value -match '(?s)CaptureChunk6aState.*?BeginChunk6aCommandWindow\(nativeControls.MountAbility.AssetGuid\).*?if \(Chunk6aTurnBased\) \{ BeginChunk6aNativePointer\(\); return; \}' -and
    $pointerScenarioText -match '(?s)PollReady\(\).*?if \(!ready\) return;.*?chunk6aApproachPath\?\.CaptureBeforeClick\(\);.*?chunk6aPointerInput.Click\(\).*?CompleteChunk6aPositiveClick\(\);' -and
    $pointerScenarioText -match '(?s)private void CompleteChunk6aPositiveClick\(\).*?ClickCompleted\(chunk6aMountClicked\).*?chunk6aApproachPath.Bind\(lastNativeAbilityShell\)' -and
    $chunk6aTrancheText -match '\["approachPathObserver"\] = chunk6aApproachPath\?\.Capture\(\)' -and
    $chunk6aTrancheText -match '(?s)private void BestEffortCleanup\(\).*?chunk6aApproachPath.Capture\(\).*?chunk6aApproachPath\?\.Dispose\(\).*?chunk6aCommandWindow\?\.Dispose\(\)') 'positive TB records pre-click preview and binds its admitted command, preserving observation before exception and cleanup'

Assert-Kmc ($pointerInputText -match 'selected.Count != 1 \|\| selected\[0\] != rider' -and
    $pointerInputText -match '(?s)AssertContext\(\);.*?Record\("before-set-ability", null\);.*?handler.SetAbility\(ability\)' -and
    $pointerInputText -match '(?s)turn.OnHoverObjectChanged\(null, target.View.gameObject\);.*?Predictions.Invoke\(turn, null\);.*?var ignored = turn.IgnoreClick\(\);' -and
    $pointerInputText -match 'disposed \|\| clicked \|\| !ready' -and
    $pointerInputText -match 'handler.OnClick\(target.View.gameObject, target.Position, 0, false, false\)' -and
    $pointerInputText -notmatch '\.(?:Position|ForcedPath|Path|NextApproachTime|IsActed)\s*=[^=]|ClearPath\(|FollowPrecomputedPath\(|PathTo\(|Cooldowns.Clear|UpdateCooldowns' -and
    $pointerScenarioText -match 'finally \{ CleanupChunk6aNativePointer\(\); \}' -and
    $chunk6aTrancheText -match '(?s)private void BestEffortCleanup\(\).*?CleanupChunk6aNativePointer\(\)') 'TB native pointer verifies exact selection, waits for native prediction admission, clicks once and restores its lease without resource or path writes'
$predictionProbeText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeRelationshipPredictionProbe.cs')
Assert-Kmc ($causalProbeText -match '(?s)if \(Kingmaker.Controllers.Clicks.PointerController.SimulatingClick\).*?RecordPredictionCommand\(command\); return;.*?initCount\+\+' -and
    $predictionProbeText -match 'NativePredictionCommandEvidence.AssertComplete' -and
    $chunk6aRowRequirementText -match 'Assert-KmcNativePredictionCommands' -and
    $chunk6aRowRequirementText -match 'Assert-KmcNativeMountPointer' -and
    $chunk6aRowRequirementText.Contains('$requireApproach = ($window -ceq ''positive-mount'') -and -not ($actionEconomyOnly -and $actionEconomyVariant.adjacentMount)') -and
    $chunk6aRowRequirementText.Contains('Assert-KmcRelationshipCommandProof $found[0] $isCombat ($turnBased -and $isCombat) $prepares $requireApproach 0 $requiresPredictionEvidence') -and
    $chunk6aRowRequirementText.Contains('$requiresPredictionEvidence=$true') -and
    $chunk6aRowRequirementText.Contains('[long]$Matches[1] -le 124') -and
    $causalProbeText -match 'var events = new JArray\(trace.EventsSince\(traceStart\)') 'native prediction has explicit separate identities while current envelopes require their proof and retain every resource event'

# Additional isolated input fixtures preserve the accepted positive command window.
$positioningText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aPreCombatPositioning.cs')
Assert-Kmc ($positioningText -match 'CaptureChunk6aOriginNavigation' -and
    $positioningText -match 'ClickGroundHandler.MoveSelectedUnitsToPoint' -and
    (Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativePreCombatGroundPlan.cs')) -match 'NativeGroundFixturePolicy.IsPreCombatPosition' -and
    $positioningText -match 'NativePreCombatGroundEvidence.AssertTransaction' -and
    $positioningText -match 'NativeOutsideCombatGroundProbe' -and
    $positioningText -match 'chunk6aPositioningCommand.Result == UnitCommand.ResultType.Success' -and
    $positioningText -notmatch '\.Translocate\(|\.Position\s*=|\.Prepare\(|Cooldown.*=|ForceToEnd' -and
    $chunk6aScenarioText -match 'if \(Chunk6aTurnBased && !TickChunk6aPreCombatPositioning\(\)\) return;' -and
    $chunk6aRowRequirementText -match 'Assert-KmcChunk6aPreCombatPositioning') 'TB positioning is a separately observed native ground order before the fresh encounter, with no actor or resource writes'
$hotbarInputText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeMountActionBarProbe.cs')
Assert-Kmc ($hotbarInputText -match 'slot.OnClick\(\)' -and $hotbarInputText -match 'handler.OnClick\(target.View.gameObject' -and
    $hotbarInputText -notmatch 'handler.SetAbility\(|new (MechanicActionBarSlotAbility|ActionBarSlot)' -and
    $hotbarInputText -match '0x060044BA' -and $hotbarInputText -match '0x060093F8' -and
    $positiveFlow.Value -match '(?s)EnsureChunk6aRiderSelection.*?PrepareChunk6aHotbar.*?CaptureChunk6aState.*?InvokeChunk6aHotbar') 'hotbar qualification invokes an actual native slot after exact selection and baseline, without a second ability selection'
$stopInputText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/NativeMountStopInputProbe.cs')
$stopScenarioText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aStopApproachScenario.cs')
Assert-Kmc ($stopInputText -match '0x060000B9' -and $stopInputText -match '0x060027B2' -and
    $stopScenarioText -match 'SelectionManager.Instance.Stop\(\)' -and
    $stopScenarioText -match 'moved <= 0.25f' -and $stopScenarioText -match 'UnitCommand.ResultType.Interrupt' -and
    $stopScenarioText -notmatch 'Cooldown.*=|\.Prepare\(|\.IsActed\s*=') 'Stop observes the exact pending approached Mount through native input and OnEnded without manufacturing acted state or cost'
$pausedText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aPausedQueueScenario.cs')
Assert-Kmc ($positiveFlow.Value -match '(?s)EnsureChunk6aRiderSelection.*?PrepareChunk6aPausedQueue.*?CaptureChunk6aState.*?CompleteChunk6aPositiveClick' -and
    $pointerScenarioText -match '(?s)private void CompleteChunk6aPositiveClick\(\).*?ClickCompleted.*?if \(!chunk6aMountClicked\).*?BeginChunk6aPausedHold\(\);' -and
    $pausedText -match 'chunk6aPausedSamples.Count < 11' -and $pausedText -match 'NativePausedMountEvidence.AssertComplete' -and
    $pausedText -notmatch 'Cooldown.*=|\.Prepare\(|\.Position\s*=') 'paused Mount retains one exact command across ten held frame boundaries before its single native release'
$mammothRunnerText=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk6aMammothScenarioEngine.cs')
Assert-Kmc ($mammothRunnerText -match 'TryResolveAutomationPair\(SupportedMountedProfiles.MammothBlueprintGuid' -and
    $mammothRunnerText -match 'SamePair\(before,CapturePair\(\)\)' -and $mammothRunnerText -match 'tranche.Start\(false\)' -and
    $mammothRunnerText -notmatch 'SpawnUnit|ReplaceCompanion|TryRegister' -and
    (Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs')) -match '(?s)if \(Chunk6aMammothScenarioEngine.SupportsScenario.*?else if \(HorseCompanionUnmountedScenarioEngine.SupportsScenario') 'native Mammoth scenarios retain original reciprocal ownership and dispatch before the temporary Horse fixture'

# Charge safety must remain exactly as accepted.
$chargeServiceText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedChargeSafetyService.cs')
$chargePolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedChargeSafetyPolicy.cs')
Assert-Kmc ($chargePolicyText -match 'state == RelationshipState\.Mounted && belongsToPair &&' -and
    $chargePolicyText -match 'blueprintId == ChargeBlueprintId && exactNativeChargeLogic && !alreadyActed' -and
    $chargeServiceText -match 'internal bool AllowClick\(' -and
    $chargeServiceText -match 'internal bool AllowAdmission\(' -and
    $chargeServiceText -match 'internal bool AllowExecution\(' -and
    $chargeServiceText -notmatch 'MountedRelationshipAdmission|MidEncounterAdoption|transitionLedger') `
    'mounted Charge safety keeps every boundary and is unchanged by the combat Mount work'

# The mod-load smoke is OBSERVATIONAL. It used to switch every experiment off for its
# own duration and then assert they were off, so the run produced the state it reported
# and could not describe the product's real defaults. These contracts hold the repair.
$smokePolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\ModLoadSmokeObservation.cs')
$automationText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs')
$observeBody = [Regex]::Match($automationText, '(?s)private ModLoadSmokeObservation ObserveModLoadSmoke\(\).*?\n        \}\r?\n')
$noSaveResultBody = [Regex]::Match($automationText, '(?s)private RuntimeGameResult CreateNoSaveResult\(.*?\n        \}\r?\n')
$smokeBranch = [Regex]::Match($automationText,
    '(?s)if \(!string\.Equals\(request\.Scenario, ModLoadSmokePolicy\.ScenarioId.*?Complete\(smokeErrors\.Count == 0 \? "PASS" : "FAIL", smokeErrors\);')
Assert-Kmc ($observeBody.Success -and
    # It reads settings and never writes one, and it reaches no mutating seam.
    $observeBody.Value -match 'ShippedMovementExperimentEnabled = diagnosticSettings\.EnableUnsafeMovementExperiment' -and
    $observeBody.Value -match 'ShippedPairedActivationEnabled = diagnosticSettings\.EnablePairedActivation' -and
    $observeBody.Value -match 'ShippedUnifiedMountedTurnEnabled = diagnosticSettings\.EnableUnifiedMountedTurn' -and
    $observeBody.Value -match 'ShippedPairedCommandSchedulerEnabled = diagnosticSettings\.EnablePairedCommandScheduler' -and
    $observeBody.Value -match 'ShippedDiagnosticOverlayEnabled = diagnosticSettings\.EnableDiagnosticOverlay' -and
    $observeBody.Value -match 'SettingsMutatedByScenario = false' -and
    $observeBody.Value -notmatch 'diagnosticSettings\.\w+ =' -and
    $observeBody.Value -notmatch 'SetEnabled|SetOverlayEnabled|AddFact|RemoveFact|RequestSave|LoadGame|\.Prepare\(\)|Cooldown' -and
    # The schema-v1 branch itself writes no setting and keeps no scope flag. Save-backed
    # scenarios legitimately configure the architecture they exercise; the smoke does not,
    # so the negative guard is scoped to the v1 path rather than to the whole host.
    $smokeBranch.Success -and
    $smokeBranch.Value -notmatch 'diagnosticSettings\.\w+ =' -and
    $automationText -notmatch 'noSaveSmokeScopeApplied' -and
    # The emitted v1 result is built from that one observation, not from a private scope.
    $noSaveResultBody.Success -and
    $noSaveResultBody.Value -notmatch 'diagnosticSettings\.\w+ =' -and
    $noSaveResultBody.Value -match 'var smoke = smokeObservation \?\? ObserveModLoadSmoke\(\);' -and
    # The verdict comes from the typed policy over that one observation.
    $automationText -match 'smokeObservation = ObserveModLoadSmoke\(\);' -and
    $automationText -match 'var smokeErrors = ModLoadSmokePolicy\.Validate\(smokeObservation\);' -and
    $automationText -match 'Complete\(smokeErrors\.Count == 0 \? "PASS" : "FAIL", smokeErrors\);' -and
    # The smoke asserts no overlay object, so it must never be the reason one is created.
    $automationText -match 'request\.Scenario != ModLoadSmokePolicy\.ScenarioId &&' -and
    ($automationText.IndexOf('request.Scenario != ModLoadSmokePolicy.ScenarioId &&') -lt
        $automationText.IndexOf('request.Scenario != PersistenceIsolationBootstrap.Scenario &&'))) `
    'the mod-load smoke observes live state, mutates no setting, and forces no legacy overlay'

# The decision is pure and total: it reaches no game type and every forbidden state has
# its own named refusal, so a missing observation can never read as a clean load.
Assert-Kmc ($smokePolicyText -match 'public static IReadOnlyList<string> Validate\(ModLoadSmokeObservation observation\)' -and
    # Pure: it imports no game or settings type and reads no live state of its own. The
    # file's own namespace is excluded, since every KMC type lives under it.
    (($smokePolicyText -split "`n" | Where-Object { $_ -notmatch '^namespace ' }) -join "`n") -notmatch
        'using Kingmaker|UnitEntityData|Game\.Instance|DiagnosticSettings|UnitCommand' -and
    $smokePolicyText -match 'if \(observation\.SettingsMutatedByScenario\)' -and
    $smokePolicyText -match 'the smoke must only observe' -and
    # A field that was never observed is a refusal, not a pass.
    $smokePolicyText -match 'private static void RequireObserved\(' -and
    $smokePolicyText -match 'private static void RequireAbsent\(' -and
    $smokePolicyText -match 'private static void RequireZero\(' -and
    $smokePolicyText -match 'did not observe whether ' -and
    $smokePolicyText -match 'did not count the ' -and
    $smokePolicyText -match 'did not record ') `
    'the mod-load smoke decision is pure, and an unobserved field refuses instead of passing'

# One field list, four consumers: the policy, the emitted v1 result, the bootstrap-failure
# result and the validator. They are compared here so none can drift.
$publishedBlock = [Regex]::Match($smokePolicyText, '(?s)public static readonly string\[\] PublishedFields =\s*\{(.*?)\};')
$publishedFields = @([Regex]::Matches($publishedBlock.Groups[1].Value, '"([a-zA-Z0-9]+)"') |
    ForEach-Object { $_.Groups[1].Value })
$validatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1')
$validatorBlock = [Regex]::Match($validatorText, "(?s)\`$v1Fields = @\((.*?)\)\r?\n")
$validatorFields = @([Regex]::Matches($validatorBlock.Groups[1].Value, "'([a-zA-Z0-9]+)'") |
    ForEach-Object { $_.Groups[1].Value })
$resultBlock = [Regex]::Match($automationText, '(?s)private sealed class RuntimeGameResult\r?\n        \{(.*?)\n        \}')
$noSaveBlock = [Regex]::Match($automationText, '(?s)private RuntimeGameResult CreateNoSaveResult\(.*?\n        \}\r?\n')
$bootstrapBlock = [Regex]::Match($automationText, '(?s)var result = new RuntimeGameResult\r?\n                \{(.*?)\n                \};')
$missingFromDto = @($publishedFields | Where-Object {
    $resultBlock.Groups[1].Value -notmatch ('public [\w\?<>\[\]]+ ' + [Regex]::Escape(($_.Substring(0,1).ToUpperInvariant() + $_.Substring(1))) + ' \{ get; set; \}') })
$missingFromEmit = @($publishedFields | Where-Object {
    $noSaveBlock.Value -notmatch ([Regex]::Escape(($_.Substring(0,1).ToUpperInvariant() + $_.Substring(1))) + ' =') })
$missingFromBootstrap = @($publishedFields | Where-Object {
    $bootstrapBlock.Groups[1].Value -notmatch ([Regex]::Escape(($_.Substring(0,1).ToUpperInvariant() + $_.Substring(1))) + ' =') })
Assert-Kmc ($publishedBlock.Success -and $validatorBlock.Success -and $resultBlock.Success -and
    $noSaveBlock.Success -and $bootstrapBlock.Success -and
    $publishedFields.Count -ge 23 -and
    $null -eq (Compare-Object -ReferenceObject ($publishedFields | Sort-Object) -DifferenceObject ($validatorFields | Sort-Object)) -and
    $missingFromDto.Count -eq 0 -and $missingFromEmit.Count -eq 0 -and $missingFromBootstrap.Count -eq 0 -and
    # The bootstrap failure never built the composition root, so it publishes NOT
    # OBSERVED rather than claiming defaults it never read.
    $bootstrapBlock.Groups[1].Value -match 'ShippedMovementExperimentEnabled = null' -and
    $bootstrapBlock.Groups[1].Value -match 'ShippedPairedActivationEnabled = null' -and
    $bootstrapBlock.Groups[1].Value -notmatch 'Shipped\w+Enabled = false') `
    'the mod-load smoke field set is identical across the policy, the emitted result, the bootstrap failure and the validator'

$harnessText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\Test-Harness.ps1')
$runtimeCommonText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1')

# The Mount preamble. Its evidence used to be one assertion on
# ClickWithSelectedAbilityHandler.OnClick returning true, described as proof that a
# native ability command had been created for KMC dispatch. The bool was never that
# proof, so the claim is replaced by sixteen staged causal assertions over what was
# actually observed, with state captured on both sides of DropAbility().
$unmountedEngineText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionUnmountedScenarioEngine.cs')
$clickChainBody = [Regex]::Match($unmountedEngineText, '(?s)private void AssertNativeMountClickChain\(NativeTargetClickCapture capture\).*?\n        \}\r?\n')
$stagedNames = @(
    'native-saddle-up-exact-ability-fact',
    'native-saddle-up-handler-holds-exact-ability',
    'native-saddle-up-priority-admits-horse',
    'native-saddle-up-resolved-target-is-exact-horse',
    'native-saddle-up-click-accepted',
    'native-saddle-up-native-command-created',
    'native-saddle-up-command-ability-is-exact',
    'native-saddle-up-command-executor-is-exact-rider',
    'native-saddle-up-command-target-is-exact-horse',
    'native-saddle-up-single-command-no-duplicate',
    'native-saddle-up-native-provenance',
    'native-saddle-up-shell-registered-once',
    'native-saddle-up-shell-identity-exact',
    'native-saddle-up-one-cast-request-no-refusal',
    'native-saddle-up-transition-not-yet-delivered',
    'native-saddle-up-drop-ability-preserves-command')
$missingStages = @($stagedNames | Where-Object { $clickChainBody.Value -notmatch [Regex]::Escape('"' + $_ + '"') })
$captureBody = [Regex]::Match($unmountedEngineText, '(?s)private NativeTargetClickCapture CaptureNativeAbilityTargetClick\(.*?\n        \}\r?\n')
Assert-Kmc ($clickChainBody.Success -and $captureBody.Success -and
    $stagedNames.Count -eq 16 -and $missingStages.Count -eq 0 -and
    ([Regex]::Matches($clickChainBody.Value, 'Check\(').Count -eq 16) -and
    # The overclaimed row is gone, everywhere.
    $unmountedEngineText -notmatch 'native-saddle-up-target-valid-horse' -and
    # Identity is observed on the command itself, not inferred from the click result.
    $captureBody.Value -match 'ReferenceEquals\(slot\.Spell\?\.Blueprint, blueprint\)' -and
    $captureBody.Value -match 'slot\.Executor == caster' -and
    $captureBody.Value -match 'slot\.Target\?\.Unit == clickedTarget' -and
    $captureBody.Value -match 'nativeControls\.TryDescribeRelationshipShell\(' -and
    # The before/after pair around the explicit release.
    $captureBody.Value -match 'capture\.AbilitySelectedBeforeDrop = handler\.Ability != null;' -and
    $captureBody.Value -match 'handler\.DropAbility\(\);' -and
    $captureBody.Value -match 'capture\.AbilitySelectedAfterDrop = handler\.Ability != null;' -and
    ($captureBody.Value.IndexOf('capture.ProcessPresentBeforeDrop') -lt
        $captureBody.Value.IndexOf('handler.DropAbility();')) -and
    ($captureBody.Value.IndexOf('handler.DropAbility();') -lt
        $captureBody.Value.IndexOf('capture.ProcessPresentAfterDrop')) -and
    # The capture only reads: it creates no command, writes no resource and forces nothing.
    $captureBody.Value -notmatch 'Commands\.Run\(|Cooldown|\.Prepare\(\)|ForceToEnd|JoinCombat|StartTurn|MountRiderOn|Dismount\(' -and
    # Both known-subscenario registries name all sixteen stages and no longer name the
    # single overclaiming row.
    $harnessText -notmatch 'native-saddle-up-target-valid-horse' -and
    $runtimeCommonText -notmatch 'native-saddle-up-target-valid-horse' -and
    ($null -eq (@($stagedNames | Where-Object { $harnessText -notmatch [Regex]::Escape("'" + $_ + "'") }) | Select-Object -First 1)) -and
    ($null -eq (@($stagedNames | Where-Object { $runtimeCommonText -notmatch [Regex]::Escape("'" + $_ + "'") }) | Select-Object -First 1))) `
    'the Mount preamble proves its native command through sixteen staged causal assertions'
$runtimeProtocolText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeProtocol.cs')
$registrationPolicyText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionRegistrationScenarioPolicy.cs')

# The narrow save-backed Mount preamble. Its whole claim is one native selected-ability
# click proved through the staged chain, so it must stop before mounting, keep its own
# evidence kind, and skip the broader native-controls lifecycle it does not assert.
Assert-Kmc ($unmountedEngineText -match 'internal const string PreambleScenarioName = "chunk6a-mount-preamble";' -and
    $unmountedEngineText -match 'internal const string PreambleEvidenceKind = "chunk6a-mount-preamble";' -and
    $unmountedEngineText -match 'internal const string PreambleEvidenceFileName = "chunk6a-mount-preamble\.json";' -and
    $unmountedEngineText -match 'private bool IsMountPreambleOnly =>\s*\r?\n\s*string\.Equals\(request\.Scenario, PreambleScenarioName, StringComparison\.Ordinal\);' -and
    # It borrows the native click path and nothing else: the pre-mount control lifecycle
    # is skipped, so the narrow ledger is the preamble alone.
    $unmountedEngineText -match 'if \(IncludesNativeControlsUx && !IsMountPreambleOnly\)\s*\r?\n\s*\{\s*\r?\n\s*ValidateNativeControlLifecycleBeforeMount\(\);' -and
    # It stops the moment the staged chain is proved; it never waits for a mounted pair.
    $unmountedEngineText -match '(?s)AssertNativeMountClickChain\(capture\);.{0,400}if \(IsMountPreambleOnly\)\s*\r?\n\s*\{.{0,300}BeginCleanup\(\);' -and
    # Its own evidence kind and schema, so it cannot be mistaken for either historical one.
    $unmountedEngineText -match '\["schemaVersion"\] = IsMountPreambleOnly \? 1 : IncludesNativeControlsUx \? 8 : 4,' -and
    $unmountedEngineText -match 'var leaf = IsMountPreambleOnly\s*\r?\n\s*\? PreambleEvidenceFileName' -and
    # Registered everywhere a save-backed scenario must be, on both sides of the harness.
    $runtimeProtocolText -match '"chunk6a-mount-preamble"' -and
    ([Regex]::Matches($runtimeProtocolText, '"chunk6a-mount-preamble"').Count -ge 2) -and
    $registrationPolicyText -match 'string\.Equals\(scenario, "chunk6a-mount-preamble", StringComparison\.Ordinal\)' -and
    $chunk6aRowRequirementText -match 'function Assert-KmcMountPreambleEvidence \{' -and
    $chunk6aRowRequirementText -match 'Assert-KmcMountPreambleEvidence -Request \$Request -Manifest \$manifestValue' -and
    ([Regex]::Matches($runtimeCommonText, "'chunk6a-mount-preamble'").Count -ge 5) -and
    $harnessText -match "'chunk6a-mount-preamble'") `
    'the narrow save-backed Mount preamble stops at the staged click and carries its own evidence kind'

# The staged click capture must actually reach the evidence. Newtonsoft serializes only
# PUBLIC PROPERTIES, so declaring these as fields published an empty observation object
# while every in-process assertion still passed: the behaviour was proved and the
# evidence was blank. A live run exposed that, and this contract keeps the shape.
$captureClassBody = [Regex]::Match($unmountedEngineText,
    '(?s)private sealed class NativeTargetClickCapture\r?\n        \{(.*?)\n        \}')
$captureMembers = @([Regex]::Matches($captureClassBody.Groups[1].Value, '(?m)^\s*public [A-Za-z0-9<>?\[\]]+ [A-Za-z0-9]+ \{ get; set; \}$'))
$captureBareFields = @([Regex]::Matches($captureClassBody.Groups[1].Value, '(?m)^\s*(?:internal|private|public|protected) [A-Za-z0-9<>?\[\]]+ [A-Za-z0-9]+;$'))
Assert-Kmc ($captureClassBody.Success -and
    $captureMembers.Count -ge 45 -and
    $captureBareFields.Count -eq 0 -and
    # It is serialized with the engine's own settings into the published observation.
    $captureBody.Value -match 'observations\[observationName\] = JObject\.FromObject\(capture, JsonSerializer\.Create\(JsonSettings\)\)' -and
    # And the narrow validator requires those very fields on a PASS, so an empty
    # observation can never be accepted as evidence again.
    $chunk6aRowRequirementText -match "foreach \(\`$field in @\('clicked','moveSlotHoldsUseAbility','abilitySelectedBeforeDrop'," -and
    $chunk6aRowRequirementText -match 'PASS narrow Mount preamble click capture omits') `
    'the staged click capture is published as public properties so the evidence is never blank'

# One registry, once. The known-subscenario check demands EXACTLY one match, so a name
# present in both the shared registry and a validator's own mission list is rejected at
# the end of a completed live run -- which is the most expensive possible place to learn it.
$resultValidatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeResult.ps1')
$gameResultValidatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1')
$sharedManifestValidatorText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\RuntimeArtifactManifestEvidence.ps1')
$sharedManifestImported = $runtimeCommonText.Contains('. (Join-Path $PSScriptRoot ''RuntimeArtifactManifestEvidence.ps1'')')
$resultManifestBound = $sharedManifestImported -and $resultValidatorText.Contains('. (Join-Path $PSScriptRoot ''RuntimeHarness.Common.ps1'')') -and $resultValidatorText.Contains('Assert-KmcReadOnlyArtifactManifest $request $result.evidenceManifestSha256')
$gameManifestBound = $sharedManifestImported -and $gameResultValidatorText.Contains('. (Join-Path $PSScriptRoot ''RuntimeHarness.Common.ps1'')') -and $gameResultValidatorText.Contains('Assert-KmcReadOnlyArtifactManifest $request $game.evidenceManifestSha256 -GameResult')
Assert-Kmc ($resultManifestBound -and $gameManifestBound -and $sharedManifestValidatorText.Contains('function Assert-KmcReadOnlyArtifactManifest') -and $resultValidatorText -notmatch 'function Assert-RuntimeArtifactManifest' -and $gameResultValidatorText -notmatch 'function Assert-RuntimeArtifactManifest') 'both runtime result gates call the same exact read-only manifest validator with the appropriate native/result context'

$sharedRowsBody = [Regex]::Match($runtimeCommonText, '(?s)function Get-KmcPhase3dHorseRuntimeRows \{.*?\n\}')
$sharedRowNames = @([Regex]::Matches($sharedRowsBody.Value, "'([A-Za-z0-9-]+)'") | ForEach-Object { $_.Groups[1].Value })
$duplicateRegistrations = @()
foreach ($validatorText in @($resultValidatorText, $gameResultValidatorText)) {
    $localBody = [Regex]::Match($validatorText, "(?s)\`$missionScenarios = @\((.*?)\)\r?\n")
    $localNames = @([Regex]::Matches($localBody.Groups[1].Value, "'([A-Za-z0-9-]+)'") | ForEach-Object { $_.Groups[1].Value })
    $duplicateRegistrations += @($localNames | Where-Object { $sharedRowNames -ccontains $_ })
}
Assert-Kmc ($sharedRowsBody.Success -and $sharedRowNames.Count -ge 20 -and
    $duplicateRegistrations.Count -eq 0 -and
    # The narrow preamble lives in the shared registry and only there.
    ($sharedRowNames -ccontains 'chunk6a-mount-preamble') -and
    $resultValidatorText -notmatch "'chunk6a-mount-preamble',\s*\r?\n?\s*'" -and
    # And the exactly-once rule is still the rule, so this contract keeps meaning something.
    $resultValidatorText -match "\`$missionScenarios \| Where-Object \{ \`$_ -ceq \[string\]\`$item\.name \}\)\.Count -ne 1" -and
    $gameResultValidatorText -match "\`$missionScenarios \| Where-Object \{ \`$_ -ceq \[string\]\`$item\.name \}\)\.Count -ne 1") `
    'every known subscenario name is registered exactly once across the shared registry and each validator'

# A row name is a registration too. The Chunk 6B charge reader decides which rows a charge artifact must
# carry, and every one of those names has to be in the shared subscenario registry, or the run completes,
# the native validator passes and the overall runtime-result gate then refuses the artifact for an unknown
# subscenario. Preview.170 spent a live campaign discovering exactly that, so the chain is walked offline.
$chargeReaderText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Chunk6bChargeEvidence.ps1')
$chargeRowsBody = [Regex]::Match($chargeReaderText, '(?s)function Get-KmcChunk6bChargeRows.*?\n\}')
$chargeRowNames = @([Regex]::Matches($chargeRowsBody.Value, "'(C6B-CHARGE-[A-Za-z0-9-]+)'") |
    ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$unregisteredChargeRows = @($chargeRowNames | Where-Object { $sharedRowNames -cnotcontains $_ })
$chargePathReaderText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Chunk6bChargePathEvidence.ps1')
$pathRowNames = @([Regex]::Matches($chargePathReaderText, "'(C6B-PATH-[A-Za-z0-9-]+)'") |
    ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$unregisteredPathRows = @($pathRowNames | Where-Object { $sharedRowNames -cnotcontains $_ })
Assert-Kmc ($chargeRowsBody.Success -and $chargeRowNames.Count -ge 7 -and $pathRowNames.Count -ge 2 -and
    $unregisteredChargeRows.Count -eq 0 -and $unregisteredPathRows.Count -eq 0) `
    'every Chunk 6B charge and carrier row the readers require is in the shared subscenario registry'

# And the last link of that chain: the fixture's own case arrays and the reader's row lists must agree, per
# mode. A row the fixture emits that the reader does not require is refused as unknown; a row the reader
# requires that the fixture never emits is refused as missing. Both refusals arrive only after a live run
# has finished, and the two lists live in different files and different languages, so they are compared
# here instead.
#
# The two sets are deliberately not identical. The repeat row named by Chunk6bChargeRepeatRow is emitted
# from inside the positive case rather than being a case of its own, and only in real time, so the reader
# requires it in real time while the fixture's real-time case array does not list it. That exact structure
# is what this contract pins.
$chargeScenarioText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6bChargeScenario.cs')
function Get-KmcDeclaredCaseNames([string]$Text, [string]$ArrayName, [string]$Prefix) {
    $body = [Regex]::Match($Text, ('(?s)string\[\] ' + [Regex]::Escape($ArrayName) + '\s*=\s*\{(.*?)\};'))
    if (-not $body.Success) { return @() }
    @([Regex]::Matches($body.Groups[1].Value, ('"(' + [Regex]::Escape($Prefix) + '[A-Za-z0-9-]+)"')) |
        ForEach-Object { $_.Groups[1].Value })
}
function Get-KmcReaderRowNames([string]$Text, [string]$Mode) {
    $body = [Regex]::Match($Text, '(?s)function Get-KmcChunk6bChargeRows.*?\n\}')
    if (-not $body.Success) { return @() }
    $branches = @([Regex]::Matches($body.Value, '(?s)@\((.*?)\)') | ForEach-Object { $_.Groups[1].Value })
    # The function reads: if TB { <first array> } else { <second array> }.
    if ($branches.Count -ne 2) { return @() }
    $chosen = if ($Mode -ceq 'TB') { $branches[0] } else { $branches[1] }
    @([Regex]::Matches($chosen, "'(C6B-CHARGE-[A-Za-z0-9-]+)'") | ForEach-Object { $_.Groups[1].Value })
}
$fixtureRt = @(Get-KmcDeclaredCaseNames $chargeScenarioText 'Chunk6bChargeRealTimeCases' 'C6B-CHARGE-')
$fixtureTb = @(Get-KmcDeclaredCaseNames $chargeScenarioText 'Chunk6bChargeTurnBasedCases' 'C6B-CHARGE-')
$readerRt = @(Get-KmcReaderRowNames $chargeReaderText 'RT')
$readerTb = @(Get-KmcReaderRowNames $chargeReaderText 'TB')
# The repeat row is declared once in the fixture and belongs to real time only.
$repeatRow = [string]([Regex]::Match($chargeScenarioText, 'Chunk6bChargeRepeatRow\s*=\s*"(C6B-CHARGE-[A-Za-z0-9-]+)"').Groups[1].Value)
$expectedRt = @($fixtureRt) + @($repeatRow | Where-Object { $_ })
$expectedTb = @($fixtureTb)
$caseListMismatches = @()
if ($repeatRow -and $fixtureRt -ccontains $repeatRow) {
    $caseListMismatches += 'the repeat row ' + $repeatRow + ' is also declared as a real-time case'
}
if ($repeatRow -and $expectedTb -ccontains $repeatRow) {
    $caseListMismatches += 'the repeat row ' + $repeatRow + ' is declared as a turn-based case'
}
foreach ($pair in @(@('real time', $expectedRt, $readerRt), @('turn-based', $expectedTb, $readerTb))) {
    $label = [string]$pair[0]; $fixtureNames = @($pair[1]); $readerNames = @($pair[2])
    $caseListMismatches += @($fixtureNames | Where-Object { $readerNames -cnotcontains $_ } |
        ForEach-Object { $label + ': the fixture emits ' + $_ + ' and the reader does not require it' })
    $caseListMismatches += @($readerNames | Where-Object { $fixtureNames -cnotcontains $_ } |
        ForEach-Object { $label + ': the reader requires ' + $_ + ' and the fixture does not emit it' })
}
Assert-Kmc ($fixtureRt.Count -ge 8 -and $fixtureTb.Count -ge 4 -and $repeatRow -and
    $readerRt.Count -eq ($fixtureRt.Count + 1) -and $readerTb.Count -eq $fixtureTb.Count -and
    $caseListMismatches.Count -eq 0) `
    'the Chunk 6B charge fixture case arrays, its repeat row and the reader row lists agree exactly, per mode'

# The carrier side of the same chain. Its fixture declares one case array used by both modes and its reader
# one row list, so the invariant is plain set equality with no repeat row to account for.
$pathScenarioText = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6bChargePathScenario.cs')
$fixturePathCases = @(Get-KmcDeclaredCaseNames $pathScenarioText 'Chunk6bChargePathCases' 'C6B-PATH-')
$readerPathBody = [Regex]::Match($chargePathReaderText, '(?s)function Get-KmcChunk6bChargePathRows.*?\n?\}')
$readerPathRows = @([Regex]::Matches($readerPathBody.Value, "'(C6B-PATH-[A-Za-z0-9-]+)'") |
    ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$pathListMismatches = @()
$pathListMismatches += @($fixturePathCases | Where-Object { $readerPathRows -cnotcontains $_ } |
    ForEach-Object { 'the carrier fixture emits ' + $_ + ' and the reader does not require it' })
$pathListMismatches += @($readerPathRows | Where-Object { $fixturePathCases -cnotcontains $_ } |
    ForEach-Object { 'the carrier reader requires ' + $_ + ' and the fixture does not emit it' })
Assert-Kmc ($readerPathBody.Success -and $fixturePathCases.Count -ge 2 -and
    $readerPathRows.Count -eq $fixturePathCases.Count -and $pathListMismatches.Count -eq 0) `
    'the Chunk 6B carrier fixture case array and the carrier reader row list agree exactly'

# A row that reaches the reader's dispatch without a rule of its own would satisfy only the generic section
# checks every row shares - identity, level, mode, case and the presence of its measurement sections - and
# then pass. That is a row which looks qualified and asserts almost nothing about what it measured, and
# nothing else in the suite would notice. Every row the readers require must therefore carry either a
# case-sensitive switch branch or an early branch keyed on its own name.
# The early branch must be the dispatch variable itself. An incidental comparison such as
# $Row.name-ceq'<row>' is not a rule, and accepting one would make this contract vacuous.
$ruleQuote = [char]39
$ruleless = @()
foreach ($rowName in @(@($readerRt) + @($readerTb) | Select-Object -Unique)) {
    $switchBranch = $chargeReaderText -match ("(?m)^\s*'" + [Regex]::Escape($rowName) + "'\s*\{")
    $earlyBranch = $chargeReaderText.Contains('$name-ceq' + $ruleQuote + $rowName + $ruleQuote)
    if (-not $switchBranch -and -not $earlyBranch) { $ruleless += $rowName }
}
$pathRuleless = @()
foreach ($rowName in @($readerPathRows)) {
    $switchBranch = $chargePathReaderText -match ("(?m)^\s*'" + [Regex]::Escape($rowName) + "'\s*\{")
    $earlyBranch = $chargePathReaderText.Contains('$name-ceq' + $ruleQuote + $rowName + $ruleQuote)
    if (-not $switchBranch -and -not $earlyBranch) { $pathRuleless += $rowName }
}
Assert-Kmc ($ruleless.Count -eq 0 -and $pathRuleless.Count -eq 0) `
    'every Chunk 6B charge and carrier row the readers require carries a rule of its own'


# THE REGISTRATION CHAIN. A new scenario's evidence leaf has to be registered in five
# places, and every one of them only complains AFTER a live run has finished: the
# producer's artifact manifest, the known-leaf sweep, the orchestration kind mapping, and
# both runtime-result allowlists. Three separate live runs were spent discovering that
# one link at a time. This contract walks the chain offline instead.
#
# Constants are resolved per DECLARING TYPE, because several diagnostics classes each
# declare their own EvidenceFileName and EvidenceKind; resolving by bare name silently
# collapses them onto whichever file was read last.
$diagnosticsFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics') -Filter '*.cs')
$typeConstants = @{}
foreach ($file in $diagnosticsFiles) {
    $fileText = Get-Content -Raw -LiteralPath $file.FullName
    $declaredTypes = @([Regex]::Matches($fileText, '(?:internal|public)(?: static| sealed| partial| abstract)* class ([A-Za-z0-9]+)') |
        ForEach-Object { $_.Groups[1].Value })
    $fileConstants = @{}
    foreach ($constMatch in [Regex]::Matches($fileText, '(?:internal|public) const string ([A-Za-z0-9]+)\s*=\s*"([^"]+)";')) {
        $fileConstants[$constMatch.Groups[1].Value] = $constMatch.Groups[2].Value
    }
    foreach ($typeName in $declaredTypes) {
        if (-not $typeConstants.ContainsKey($typeName)) { $typeConstants[$typeName] = @{} }
        foreach ($constName in $fileConstants.Keys) { $typeConstants[$typeName][$constName] = $fileConstants[$constName] }
    }
}
$registeredPairs = @()
$unresolvedRegistrations = @()
foreach ($registration in [Regex]::Matches($automationText,
    '(?s)AddRuntimeArtifactIfPresent\(\s*artifacts,\s*request\.EvidenceRoot,\s*([A-Za-z0-9]+)\.([A-Za-z0-9]+),\s*([A-Za-z0-9]+)\.([A-Za-z0-9]+)\);')) {
    $leafType = $registration.Groups[1].Value; $leafName = $registration.Groups[2].Value
    $kindType = $registration.Groups[3].Value; $kindName = $registration.Groups[4].Value
    if ($typeConstants.ContainsKey($leafType) -and $typeConstants[$leafType].ContainsKey($leafName) -and
        $typeConstants.ContainsKey($kindType) -and $typeConstants[$kindType].ContainsKey($kindName)) {
        $registeredPairs += [pscustomobject]@{
            leaf = $typeConstants[$leafType][$leafName]; kind = $typeConstants[$kindType][$kindName] }
    }
    else { $unresolvedRegistrations += ($leafType + '.' + $leafName + '/' + $kindType + '.' + $kindName) }
}
$knownLeafBlock = [Regex]::Match($runtimeCommonText, "(?s)foreach \(\`$leaf in @\((.*?)\)\) \{")
$unregisteredLinks = @()
foreach ($pair in $registeredPairs) {
    $leafLiteral = "'" + $pair.leaf + "'"
    $mappingLiteral = "(`$relative -ceq '" + $pair.leaf + "' -and `$kind -ceq '" + $pair.kind + "')"
    $resultLiteral = "(`$relativePath -ceq '" + $pair.leaf + "' -and `$kind -ceq '" + $pair.kind + "')"
    if ($knownLeafBlock.Groups[1].Value.IndexOf($leafLiteral, [StringComparison]::Ordinal) -lt 0) {
        $unregisteredLinks += ($pair.leaf + ': known-leaf sweep')
    }
    if ($runtimeCommonText.IndexOf($mappingLiteral, [StringComparison]::Ordinal) -lt 0) {
        $unregisteredLinks += ($pair.leaf + ': orchestration kind mapping')
    }
    if (-not $resultManifestBound -or $sharedManifestValidatorText.IndexOf($resultLiteral, [StringComparison]::Ordinal) -lt 0) {
        $unregisteredLinks += ($pair.leaf + ': runtime-result allowlist')
    }
    if (-not $gameManifestBound -or $sharedManifestValidatorText.IndexOf($resultLiteral, [StringComparison]::Ordinal) -lt 0) {
        $unregisteredLinks += ($pair.leaf + ': runtime-game-result allowlist')
    }
}
Assert-Kmc ($knownLeafBlock.Success -and
    $registeredPairs.Count -ge 5 -and
    $unresolvedRegistrations.Count -eq 0 -and
    $unregisteredLinks.Count -eq 0 -and
    # The narrow preamble is the pair that exposed the gap, so it is named explicitly.
    @($registeredPairs | Where-Object { $_.leaf -ceq 'chunk6a-mount-preamble.json' -and $_.kind -ceq 'chunk6a-mount-preamble' }).Count -eq 1) `
    'every evidence leaf the mod manifests is known to the leaf sweep, the kind mapping and both runtime-result allowlists'
# The reverse direction, which is the one that actually bit. A leaf the mod can WRITE but
# never records in its own artifact manifest is rejected as an unmanifested artifact after
# the run has already finished. Checking only producer-to-consumer misses it entirely,
# because an unregistered leaf contributes no pair to check.
#
# Registration is recognised either way: the manifest method names some leaves by string
# literal and others through a constant, so both spellings count.
$manifestMethodBody = [Regex]::Match($automationText,
    '(?s)private static string PublishRuntimeArtifactManifest\(RuntimeRequest request\).*?\n        \}\r?\n')
$writableLeaves = @()
foreach ($file in $diagnosticsFiles) {
    $fileText = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($constMatch in [Regex]::Matches($fileText,
        '(?:internal|public) const string ([A-Za-z0-9]*EvidenceFileName)\s*=\s*"([^"]+)";')) {
        $writableLeaves += [pscustomobject]@{ name = $constMatch.Groups[1].Value; leaf = $constMatch.Groups[2].Value }
    }
}
$unproducedLeaves = @($writableLeaves | Where-Object {
    $manifestMethodBody.Value.IndexOf('"' + $_.leaf + '"', [StringComparison]::Ordinal) -lt 0 -and
    $manifestMethodBody.Value.IndexOf('.' + $_.name, [StringComparison]::Ordinal) -lt 0
})
Assert-Kmc ($manifestMethodBody.Success -and
    @($writableLeaves).Count -ge 8 -and
    $unproducedLeaves.Count -eq 0 -and
    @($writableLeaves | Where-Object { $_.leaf -ceq 'chunk6a-mount-preamble.json' }).Count -ge 1 -and
    # The narrow preamble is registered through its constant, not a stray literal.
    $manifestMethodBody.Value -match 'HorseCompanionUnmountedScenarioEngine\.PreambleEvidenceFileName' -and
    $manifestMethodBody.Value -match 'HorseCompanionUnmountedScenarioEngine\.PreambleEvidenceKind') `
    'every evidence leaf a scenario can write is recorded in the mod''s own artifact manifest'

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
