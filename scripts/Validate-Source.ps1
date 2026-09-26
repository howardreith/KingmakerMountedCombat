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
