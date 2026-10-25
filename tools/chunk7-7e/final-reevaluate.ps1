[CmdletBinding()]
param(
 [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$')][string]$RunId,
 [Parameter(Mandatory)][ValidateSet('New','Old')][string]$Mode,
 [Parameter(Mandatory)][string]$ScriptsRoot,
 [Parameter(Mandatory)][string]$FingerprintPath,
 [string]$ExpectedError,
 [string]$LabRoot='C:\Dev\KingmakerMountedCombatLab'
)
# Chunk 7 7E plan section 8: the offline verdict path of a run whose runner verdict FAILed on a reader-only defect.
# A read-only verifier for the deriver's Test-ReaderReevaluation (it never launches, stages, restores or writes).
#
# Old: the run commit's own scripts (-ScriptsRoot from a git archive outside the run directory) must throw exactly the
#   runner's recorded error from Test-RuntimeGameResult, the point where the runner stopped.
# New: the corrected scripts run every offline-replayable remaining check of the runner's verdict path on the immutable
#   files: the whole of Test-RuntimeGameResult.ps1 with -RequirePass (process id and start bound to orchestration.json,
#   the platform fingerprint passed by the caller), the base-game native inventory against the run's own admission
#   receipt, and the runner's owned-pairs post-game checks on the retained run profile. Checks whose inputs no longer
#   exist are named in the output with the retained evidence that stands in for them; anything else is refused.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
function Fail([string]$m){'REEVALUATION-FAIL '+$m;exit 1}
$runDir=[IO.Path]::GetFullPath((Join-Path $LabRoot ('runtime-evidence\'+$RunId)))
if(-not(Test-Path -LiteralPath $runDir -PathType Container)){Fail 'run directory is missing'}
$requestPath=Join-Path $runDir 'runtime-request.json';$gamePath=Join-Path $runDir 'runtime-game-result.json'
$orchestrationPath=Join-Path $runDir 'orchestration.json'
foreach($p in @($requestPath,$gamePath,$orchestrationPath,$FingerprintPath)){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){Fail ('missing input '+$p)}}
$utf8=[Text.UTF8Encoding]::new($false,$true)
$request=[IO.File]::ReadAllText($requestPath,$utf8)|ConvertFrom-Json
if([IO.Path]::GetFullPath([string]$request.evidenceRoot).TrimEnd('\')-cne$runDir.TrimEnd('\')){Fail 'request evidenceRoot is not the run directory'}
$orchestration=[IO.File]::ReadAllText($orchestrationPath,$utf8)|ConvertFrom-Json
if($orchestration.runId-cne$RunId-or-not($orchestration.kingmakerProcessId-is[int]-or$orchestration.kingmakerProcessId-is[long])){Fail 'orchestration does not bind the native process'}
$processId=[int]$orchestration.kingmakerProcessId;$notBefore=[DateTimeOffset]::Parse([string]$orchestration.startedAtUtc)
$validator=Join-Path $ScriptsRoot 'runtime\Test-RuntimeGameResult.ps1'
if($Mode-ceq'Old'){
 if([string]::IsNullOrEmpty($ExpectedError)){Fail 'old mode needs the recorded error'}
 $message=$null
 try{$null=& $validator -GameResultPath $gamePath -RequestPath $requestPath -FingerprintPath $FingerprintPath -ExpectedProcessId $processId -NotBeforeUtc $notBefore *>&1}catch{$message=$_.Exception.Message}
 if($message-cne$ExpectedError){Fail ('old reader did not throw exactly the recorded error: '+[string]$message)}
 'REEVALUATION-OLD-FAIL-REPRODUCED';exit 0
}
# New: the whole game-result validator, as the runner calls it, with -RequirePass.
try{$null=& $validator -GameResultPath $gamePath -RequestPath $requestPath -FingerprintPath $FingerprintPath -ExpectedProcessId $processId -NotBeforeUtc $notBefore -RequirePass *>&1}
catch{Fail ('Test-RuntimeGameResult: '+$_.Exception.Message)}
. (Join-Path $ScriptsRoot 'runtime\RuntimeHarness.Common.ps1')
$game=Read-KmcJson $gamePath;$named=@()
if(Test-KmcBoundaryRuntimeScenario ([string]$request.scenario)){Fail 'boundary scenarios need the live terminal Working identity; not replayable'}
$named+='VerifyLiveWorkingIdentity: applies only to boundary scenarios (Assert-KmcBoundaryScenarioEvidence returns before it for this scenario)'
# Base-game native inventory (runner Invoke-KingmakerRuntimeScenario.ps1, base-game branch) against the run's own admission receipt.
$admissionPath=Join-Path $runDir 'base-game-isolation-admission.json'
if(Test-Path -LiteralPath $admissionPath -PathType Leaf){
 $admission=Read-KmcJson $admissionPath
 if($admission.runId-cne$RunId-or$admission.transactionToken-cne$request.transactionToken){Fail 'base-game admission belongs to another transaction'}
 $manifestPath=Join-Path $runDir 'runtime-artifacts.json'
 if((Get-KmcSha256 $manifestPath)-cne[string]$game.evidenceManifestSha256){Fail 'base-game inventory manifest is not the native result manifest'}
 $inventory=Read-KmcChunk7ManifestArtifact $request (Read-KmcJson $manifestPath) 'runtime-loader-inventory.json' 'runtime-loader-inventory'
 if($inventory.observations.processId-ne$processId){Fail 'loader inventory came from another native process'}
 try{Assert-KmcBaseGameNativeInventory $request $inventory $admission.dependencies}catch{Fail ('base-game native inventory: '+$_.Exception.Message)}
 $isolation=Read-KmcJson (Join-Path $runDir 'base-game-isolation-result.json')
 if($isolation.runId-cne$RunId-or$isolation.dependenciesUnchanged-ne$true-or$isolation.processExited-ne$true){Fail 'base-game dependency receipt does not show unchanged dependencies'}
 $named+='Assert-KmcBaseGameDependencyReceipt -VerifyFiles (live game files): stood in by the run''s own base-game-isolation-result dependenciesUnchanged=true'
}
if(-not(Test-KmcChunk7OwnedPairsScenario ([string]$request.scenario))){Fail ('the runner post-game checks of scenario '+$request.scenario+' are not replayed offline')}
$case=[string]$request.persistenceCase
if($case-ceq'multi-area-autosave'-or(Test-KmcChunk7CapitalArrivalCase $case)){Fail 'Areas routes need the runner''s area target parameters; not replayable'}
# Owned-pairs post-game checks (runner lines after the game result), on the retained run profile.
$profileRoot=[IO.Path]::GetFullPath((Join-Path $LabRoot ('runtime-staging\persistence-'+$RunId)));$isolatedSaves=Join-Path $profileRoot 'Saved Games'
if(-not(Test-Path -LiteralPath $isolatedSaves -PathType Container)){Fail 'the retained run profile is missing'}
$candidate=[pscustomobject]@{commit=[string]$request.commit;dllSha256=[string]$request.dllSha256;dllMvid=[string]$request.dllMvid}
try{$verifiedOwnedSource=Get-KmcChunk7OwnedPairsSource $request.ownedPairs $case $request.fixture $candidate $null}catch{Fail ('owned source lineage: '+$_.Exception.Message)}
$named+='inherited lineage unchanged across native execution: the pre-run object is not retained; stood in by the lineage recomputed now from the immutable source receipts (Get-KmcChunk7OwnedPairsSource with the request''s fixture and candidate)'
$r=[pscustomobject]$request
try{
 $seedManualRun=Test-KmcChunk7OwnedPairsSeedManual $r
 if((Test-KmcChunk7OwnedPairsOwnerSeed $r)-or$seedManualRun){
  $seedRecord=Read-KmcJson (Join-Path $profileRoot 'owned-two-pair-source.json')
  if($seedRecord.kind-cne'kmc-owner-seed-source'-or$seedRecord.seedId-cne$request.ownedPairs.seedId-or$seedRecord.registrySha256-cne$verifiedOwnedSource.registrySha256-or
   $seedRecord.producerAncestry-ne$false-or$seedRecord.nativeAuthorityGranted-ne$false-or
   (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$request.ownedPairs.seedSha256){throw 'Owner-seed source record or immutable native copy changed during execution.'}
 }elseif((Get-KmcSha256 (Join-Path $profileRoot 'owned-two-pair-source.json'))-cne$request.ownedPairs.sourceArtifactSha256-or
  (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$request.persistenceLoad.sha256){throw 'Inherited source receipt or immutable native input changed during execution.'}
 $cold=Join-Path $runDir 'chunk7-persistence-evidence.json'
 if(Test-KmcChunk7OwnedPairsSeedDerivedLoad $r){
  if((Get-KmcSha256 $verifiedOwnedSource.artifactPath)-cne$request.ownedPairs.sourceArtifactSha256){throw 'Owner-seed-derived producer receipt changed.'}
  if(Test-KmcChunk7ManticoreManualCold $r){Assert-KmcChunk7ManticoreManualColdSource $r (Read-KmcJson $verifiedOwnedSource.artifactPath) (Read-KmcJson $cold)}
  else{Assert-KmcChunk7OwnerSeedDerivedColdSource $r (Read-KmcJson $verifiedOwnedSource.artifactPath) (Read-KmcJson $cold)}
 }
 if(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $r){
  if((Get-KmcSha256 $verifiedOwnedSource.checkpointArtifactPath)-cne$request.ownedPairs.checkpointSourceArtifactSha256){throw 'Owner-seed checkpoint producer receipt changed.'}
  Assert-KmcChunk7OwnerSeedCheckpointColdSource $r (Read-KmcJson $verifiedOwnedSource.checkpointArtifactPath) (Read-KmcJson $cold)
 }
 if((Test-KmcChunk7OwnedPairsSmallLoad $r)-or(Test-KmcChunk7OwnedPairsMediumLoad $r)){
  if((Get-KmcSha256 $verifiedOwnedSource.checkpointArtifactPath)-cne$request.ownedPairs.checkpointSourceArtifactSha256){throw 'Paid mounted checkpoint producer receipt changed.'}
  Assert-KmcChunk7PaidMountedColdSource $r (Read-KmcJson $verifiedOwnedSource.checkpointArtifactPath) (Read-KmcJson $cold)
 }
 if(Test-KmcChunk7CapitalColdCase $case){throw 'capital cold cases are not replayed offline'}
 if(Test-KmcChunk7SixPairLoad $r){
  if((Get-KmcSha256 $verifiedOwnedSource.checkpointArtifactPath)-cne$request.ownedPairs.checkpointSourceArtifactSha256){throw 'Six-mounted Manual producer receipt changed.'}
  Assert-KmcChunk7SixPairMountedColdSource $r (Read-KmcJson $verifiedOwnedSource.checkpointArtifactPath) (Read-KmcJson $cold)
 }
 $nativeLeaves=@(Get-ChildItem -LiteralPath $isolatedSaves -Filter '*.zks' -File|ForEach-Object Name)
 $allLeaves=@(Get-ChildItem -LiteralPath $isolatedSaves -Force|ForEach-Object Name)
 $writes=Get-KmcChunk7OwnedWriteCount $request
 if(Test-KmcChunk7OwnedPairsOwnerSeed $r){Assert-KmcChunk7OwnerSeedFinalSaveRoot $r $allLeaves}
 elseif($writes-eq0){Assert-KmcChunk7OwnedFinalSaveRoot $r $allLeaves $null}
 if($writes-gt0-and-not(Test-KmcChunk7OwnedPairsOwnerSeed $r)){
  $ownedArtifact=Read-KmcJson (Join-Path $runDir (Get-KmcChunk7OwnedWriteArtifactLeaf $r))
  if($case-ceq'owned-schema1-resave-manual'){Assert-KmcOwnedSchema1ColdSource $request $verifiedOwnedSource $ownedArtifact}
  if(Test-KmcOwnedSchema2ColdRequest $r){Assert-KmcOwnedSchema2ColdSource $request $verifiedOwnedSource $ownedArtifact}
  Assert-KmcChunk7OwnedOutputEvidence $r $ownedArtifact.observations
  Assert-KmcChunk7OwnedFinalSaveRoot $r $allLeaves $ownedArtifact.observations
  $outputLeaf=Get-KmcChunk7OwnedOutputLeaf $r $ownedArtifact.observations
  if($outputLeaf-cnotin$nativeLeaves-or(Get-KmcSha256 (Join-Path $isolatedSaves $outputLeaf))-cne$ownedArtifact.observations.nativeWrite.sha256){throw 'Inherited fresh native output differs from its observed write.'}
  if($seedManualRun){Assert-KmcChunk7OwnerSeedManualFinalSaveRoot $r $allLeaves $outputLeaf}
 }
}catch{Fail ('owned post-game: '+$_.Exception.Message)}
$named+='the retained run profile is read as the runner left it (runtime-staging); its final save-root listing and output hash were rechecked'
foreach($n in $named){'NAMED '+$n}
'REEVALUATION-PASS'
