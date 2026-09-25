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
Assert-Kmc (([Regex]::Matches($evaluatorText, 'NativeMoveActionShellAdmitted').Count -eq 3) -and
    $evaluatorText -match 'context\.InCombat && !context\.RiderHasMoveAction &&\s*\r?\n?\s*!context\.NativeMoveActionShellAdmitted') `
    'the admitted native shell suppresses only the stale rider Move-resource predicate'
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
    'a relationship delivery must own its native Move shell and its original relationship generation'

# The Chunk 6A runtime scenario observes native accounting and must never create
# it: no resource write, no preparation, no turn forcing except the accepted
# idle-fixture end-turn input, and no direct position or state assignment.
$chunk6aScenarioText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Chunk6aCombatMountScenario.cs')
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
