Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7OwnedSchema2Fixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SmallCalibrationFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureColdSource.ps1')
. (Join-Path $PSScriptRoot 'Chunk7MediumMountedSource.ps1')
function Read-KmcChunk7OwnedPairsSelectionFile([string]$Path,[string]$RunId,[AllowNull()][string]$PersistenceCase){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'owned two-pair selection';Assert-KmcNotReparsePoint $path 'owned two-pair selection';Assert-KmcNotHardLink $path 'owned two-pair selection'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt32KB){throw 'Owned two-pair selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'owned two-pair selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$RunId;ownedPairs=$selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Owned two-pair selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
function Get-KmcChunk7OwnedReceiptSet([string]$RunId,[string]$ArtifactLeaf){
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$RunId)) (Join-Path $lab 'runtime-staging') 'qualified inherited profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified inherited profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$RunId)) (Join-Path $lab 'runtime-evidence') 'qualified inherited evidence';Assert-KmcNotReparsePoint $root 'qualified inherited evidence';$pins=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json',$ArtifactLeaf)){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified inherited receipt';Assert-KmcNotHardLink $path 'qualified inherited receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified inherited receipt';$pins[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified inherited owner';Assert-KmcNotHardLink $ownerPath 'qualified inherited owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified inherited owner';$pins[$ownerPath]=Get-KmcSha256 $ownerPath
 [pscustomobject]@{profile=$profile;root=$root;pins=$pins;owner=(Read-KmcJson $ownerPath);request=(Read-KmcJson (Join-Path $root 'runtime-request.json'));result=(Read-KmcJson (Join-Path $root 'runtime-result.json'));game=(Read-KmcJson (Join-Path $root 'runtime-game-result.json'));isolation=(Read-KmcJson (Join-Path $root 'base-game-isolation-result.json'));manifest=(Read-KmcJson (Join-Path $root 'runtime-artifacts.json'))}
}
function Assert-KmcChunk7OwnedSourceCompletion($Set,[string]$RunId,[string]$Scenario,$Fixture,$Candidate,[string]$InputHash,[int]$Writes=1){
 $r=$Set.request;Assert-KmcBootstrapInventoryString $r.runId $RunId;Assert-KmcBootstrapInventoryString $r.scenario $Scenario;Assert-KmcBootstrapInventoryString $r.evidenceRoot $Set.root
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $r.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $r.$key $Candidate.$key}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $Set.owner.$key $r.$key}
 Assert-KmcBootstrapInventoryString $Set.owner.sourceSha256 $InputHash;Assert-KmcBootstrapInventoryString $Set.owner.saveRoot (Join-Path $Set.profile 'Saved Games')
 foreach($receipt in @($Set.result,$Set.game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $r.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Inherited source must have an error-free completed native result.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $Set.pins[(Join-Path $Set.root 'runtime-artifacts.json')]
 }
 if($r.transactionToken-isnot[string]-or$r.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$Set.result.restoredSaveInventoryDigest-isnot[string]-or$Set.result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited source lacks exact transaction/restoration identity.'}
 Assert-KmcBootstrapInventoryString $Set.result.gameResultSha256 $Set.pins[(Join-Path $Set.root 'runtime-game-result.json')]
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $Set.result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key 1 1}
 foreach($key in @('saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key $Writes $Writes}
 Assert-KmcChunk7Boolean $Set.game.fixtureIdentityVerified $true
 Assert-KmcBootstrapInventoryString $Set.isolation.runId $r.runId;Assert-KmcBootstrapInventoryString $Set.isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $Set.isolation.$key $true}
 Assert-KmcReadOnlyArtifactManifest $r $Set.pins[(Join-Path $Set.root 'runtime-artifacts.json')] -GameResult
}
function Assert-KmcChunk7OwnedReceiptPins($Set){foreach($path in $Set.pins.Keys){if((Get-KmcSha256 $path)-cne$Set.pins[$path]){throw 'Inherited qualification receipt changed during intake.'}}}
function Get-KmcChunk7OwnedPairsManualSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate){
 $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-pairs';ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
 Assert-KmcChunk7OwnedPairsSelection $r
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-acquired-manual-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-acquired-manual-derivative';Assert-KmcBootstrapInventoryString $request.acquiredManual.case 'preserve-two-acquired-field'
 Assert-KmcChunk7AcquiredManualDescriptor $request
 # Reuse the complete cumulative -> first-owner Manual -> acquisition -> seed ->
 # cold -> bootstrap lineage. Receipt decoding grants no native restoration.
 $ancestry=Get-KmcChunk7AcquiredManualSource $request.acquiredManual $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $ancestry.descriptor)){throw 'Two-owner Manual did not load its exact qualified cumulative Auto.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.sourceRunId 'chunk7-acquired-manual-derivative' $Fixture $Candidate $request.acquiredManual.sourceArchiveSha256
 # The runner serializes an unselected string parameter as "" in owner.json.
 # Keep its wire type: an array/object must not impersonate an empty checkpoint.
 $ownerCheckpoint=$set.owner.persistenceCase
 if($null-ne$ownerCheckpoint-and($ownerCheckpoint-isnot[string]-or$ownerCheckpoint-cne'')){throw 'Two-owner Manual owner unexpectedly selects a persistence checkpoint.'}
 Assert-KmcChunk7AcquiredManualArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-acquired-manual-evidence.json' 'chunk7-acquired-manual-evidence'
 $path=Join-Path $set.root 'chunk7-acquired-manual-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt8MB-or$set.pins[$path]-cne$Selection.sourceArtifactSha256){throw 'Two-owner Manual source receipt differs from its explicit immutable pin.'}
 Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
 Assert-KmcChunk7AcquiredManualOutput $request $artifact
 $owners=@($artifact.observations.twoOwners);$roles=Get-KmcChunk7OwnedPairIds $r
 if($owners.Count-ne2){throw 'Qualified native Manual does not retain exactly two ordinary owners.'}
 for($i=0;$i-lt2;$i++){Assert-KmcBootstrapInventoryString $owners[$i].actorId $roles[$i*2];Assert-KmcBootstrapInventoryString $owners[$i].petId $roles[$i*2+1]}
 $party=@($artifact.observations.fieldWorld.party|ForEach-Object{$_.id})
 if((ConvertTo-KmcChunk7Canonical @($party|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Explicit inherited field roster differs from completed native selector/save evidence.'}
 if($PersistenceCase-ceq'multi-area-autosave'){Assert-KmcChunk7OwnedAreaProfileRestoration $set $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.area $Selection.sourceArea}
 $w=$artifact.observations.nativeWrite;$descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 # Validate the immutable source descriptor as the source mode, without changing
 # the real request or selecting a native action. Cold source is checked below.
 $sourceSelection=$Selection|ConvertTo-Json -Depth 16|ConvertFrom-Json;$sourceSelection.case='exploration';$sourceSelection.checkpointSourceRunId=$null;$sourceSelection.checkpointSourceArtifactSha256=$null;$sourceSelection.expectedNativeWriteCount=0;$sourceSelection.expectedNativeLoadCount=1
 if($sourceSelection.PSObject.Properties['sourceArea']){$sourceSelection.PSObject.Properties.Remove('sourceArea')}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-manual';ownedPairs=$sourceSelection;persistenceLoad=$descriptor;fixture=$Fixture;persistenceCase=$null})
 Assert-KmcChunk7OwnedReceiptPins $set
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Two-owner native Manual bytes changed during qualification.'}
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$path;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;sourceRunId=$request.runId;ancestry=$ancestry;artifact=$artifact}
}
# Owner-seed kind: reuse the cold owner-seed source unchanged (registry entry, lab
# owner-supplied master, intake hash, read-only header identity and the cold no-alias
# descriptor check), then the owned-family descriptor. No producer receipt is read.
function Get-KmcChunk7OwnedPairsOwnerSeedSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $run='unassigned-owned-owner-seed';if($Selection.seedId-ceq$run){$run='unassigned-owned-owner-seed-2'}
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})})
 if($null-ne$AreaTarget){throw 'Owner-seed exploration carries no area target.'}
 $cold=[pscustomobject][ordered]@{case='owner-seed-readonly-inventory';baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0;seedId=$Selection.seedId;seedSha256=$Selection.seedSha256}
 $source=Get-KmcChunk7OwnerSeedSource $cold $Fixture $Candidate
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase});persistenceLoad=$source.descriptor;fixture=$Fixture})
 if($source.fixtureSetupOnly-ne$true-or$source.nativeAuthorityGranted-ne$false){throw 'Owner seed source claims native authority.'}
 [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;seedId=$source.seedId;registryPath=$source.registryPath;registrySha256=$source.registrySha256;registryEntry=$source.registryEntry;
  intakePath=$source.intakePath;intakeSha256=$source.intakeSha256;ownerSeed=$true;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
# Owner-seed-derived cold source: exactly the Manual one successful same-candidate producer wrote,
# pinned by its run, world evidence and written bytes, with its seed proved to be the owner's
# unchanged registered master. Receipt decoding grants no native restoration.
function Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-derived';ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
 Assert-KmcChunk7OwnedPairsSelection $r
 Assert-KmcChunk7OwnerSeedAreaTarget $Selection $PersistenceCase $AreaTarget
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-world-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-persistence-save';Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('seedId','seedSha256','riderAId','riderBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 # The owner's immutable master is unchanged and is exactly the seed the producer loaded.
 $seed=Get-KmcChunk7OwnedPairsOwnerSeedSource $request.ownedPairs 'multi-exploration-manual' $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $seed.descriptor)-cne(ConvertTo-KmcChunk7Canonical $request.persistenceLoad)){throw 'Producer did not load the exact registered immutable owner seed.'}
 # Combat (scenarios 3-5) and the checkpoint producer may be a later candidate: each receipt is pinned to its own recorded candidate.
 $combat=$Selection.case-cin@('owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-combat-disable','owner-seed-checkpoint-save','owner-seed-load-refusal')
 $repeat=$Selection.case-ceq'owner-seed-persistence-load'-and$PersistenceCase-ceq'multi-exploration-three-loads-manual';$producerCandidate=if($combat-or$repeat){Get-KmcChunk7RecordedCandidate $request}else{$Candidate}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.sourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $producerCandidate $Selection.seedSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7WorldArtifact $request $set.manifest 'PASS';Assert-KmcMatchedPoolArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-world-evidence.json' 'chunk7-world-evidence'
 $path=Join-Path $set.root 'chunk7-world-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt8MB-or$set.pins[$path]-cne$Selection.sourceArtifactSha256){throw 'Owner-seed-derived producer receipt differs from its explicit immutable pin.'}
 $o=$artifact.observations;$w=$o.nativeWrite;Assert-KmcBootstrapInventoryString $w.sha256 $Selection.sourceArchiveSha256
 $roles=@($Selection.riderAId,$Selection.mountAId,$Selection.riderBId,$Selection.mountBId);$owners=@($o.ownerSeedManual.twoOwners)
 if($owners.Count-ne2){throw 'Owner-seed-derived Manual does not retain exactly two owners.'}
 for($i=0;$i-lt2;$i++){Assert-KmcBootstrapInventoryString $owners[$i].actorId $roles[$i*2];Assert-KmcBootstrapInventoryString $owners[$i].petId $roles[$i*2+1]}
 if((ConvertTo-KmcChunk7Canonical @($o.ownerSeedManual.fieldWorld.party|ForEach-Object{$_.id}|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Explicit owner-seed-derived field roster differs from the producer field.'}
 $leaf=Get-KmcChunk7OwnedOutputLeaf $request $o;$expected=Join-Path (Join-Path $set.profile 'Saved Games') $leaf
 if($w.path-isnot[string]-or$w.path-cne$expected){throw 'Owner-seed-derived Manual escaped its producer run root.'}
 Assert-KmcNotReparsePoint $w.path 'owner-seed-derived Manual';Assert-KmcNotHardLink $w.path 'owner-seed-derived Manual'
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Owner-seed-derived Manual bytes changed after qualification.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long](Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks;gameId=$request.persistenceLoad.gameId;gameName=$request.persistenceLoad.gameName;area=$request.persistenceLoad.area}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-derived';ownedPairs=$Selection;persistenceCase=$r.persistenceCase;persistenceLoad=$descriptor;fixture=$Fixture;persistenceAreaTarget=$AreaTarget})
 Assert-KmcChunk7OwnedReceiptPins $set
 $result=[ordered]@{path=$w.path;descriptor=$descriptor;artifactPath=$path;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;sourceRunId=$request.runId;ownerSeedDerived=$true;seed=$seed}
 if($combat){$consumer=Get-KmcChunk7OwnerSeedConsumerReceipt $Selection $Fixture $artifact $descriptor;foreach($key in $consumer.Keys){$result[$key]=$consumer[$key]}
  $result['candidates']=[pscustomobject][ordered]@{producer=$producerCandidate;consumer=$consumer.consumerCandidate;current=[pscustomobject][ordered]@{commit=$Candidate.commit;dllSha256=$Candidate.dllSha256;dllMvid=$Candidate.dllMvid};sameCandidate=(($producerCandidate.commit-ceq$Candidate.commit)-and($producerCandidate.dllSha256-ceq$Candidate.dllSha256)-and($producerCandidate.dllMvid-ceq$Candidate.dllMvid))}}
 [pscustomobject]$result
}
# A receipt's own recorded candidate identity (later-candidate combat reuse); never the current one.
function Get-KmcChunk7RecordedCandidate($Request){
 if($Request.commit-isnot[string]-or$Request.commit-cnotmatch'\A[a-f0-9]{40}\z'-or$Request.dllSha256-isnot[string]-or$Request.dllSha256-cnotmatch'\A[a-f0-9]{64}\z'-or
    $Request.dllMvid-isnot[string]-or$Request.dllMvid-cnotmatch'\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z'){throw 'Qualified receipt does not record its exact candidate identity.'}
 [pscustomobject][ordered]@{commit=$Request.commit;dllSha256=$Request.dllSha256;dllMvid=$Request.dllMvid}
}

function Assert-KmcChunk7ManticoreAreasCustody($Request,$Areas,$Snapshot,$Durable,$Transaction,
    [string]$SnapshotHash,[string]$GeneratedPath){
 Assert-KmcOrdinaryInteger $Areas.schemaVersion 1 1
 Assert-KmcChunk7Boolean $Areas.originalAreasRestoredExactly $true
 Assert-KmcOrdinaryInteger $Durable.schemaVersion 1 1
 foreach($value in @($Areas,$Snapshot,$Durable,$Transaction)){Assert-KmcBootstrapInventoryString $value.runId $Request.runId}
 foreach($value in @($Snapshot,$Durable,$Transaction)){Assert-KmcBootstrapInventoryString $value.token $Request.transactionToken}
 Assert-KmcBootstrapInventoryString $Durable.stage 'restored'
 Assert-KmcBootstrapInventoryString $Snapshot.profile $Areas.profileRoot
 Assert-KmcBootstrapInventoryString $Durable.profile $Areas.profileRoot
 Assert-KmcBootstrapInventoryString $Durable.profileDigest $Snapshot.profileDigest
 Assert-KmcBootstrapInventoryString $Durable.snapshotSha256 $SnapshotHash
 foreach($key in @('originalDigest','generatedDigest')){
  if($Areas.$key-isnot[string]-or$Areas.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Manticore Areas restoration lacks exact tree digests.'}
  Assert-KmcBootstrapInventoryString $Durable.$key $Areas.$key
 }
 Assert-KmcBootstrapInventoryString $Areas.generatedQuarantine $GeneratedPath
 Assert-KmcBootstrapInventoryString $Transaction.stagingMode 'base-game-only-kmc'
 Assert-KmcBootstrapInventoryString $Transaction.phase 'restored'
}

# The accepted cold receipt and every later return Auto must carry their own
# settled Areas transaction, not borrow the original Manual producer's proof.
function Assert-KmcChunk7ManticoreAreasReceipt($Set){
 $request=$Set.request;$run=$request.runId
 Assert-KmcChunk7OwnedAreaProfileRestoration $Set $run
 $areasPath=Join-Path $Set.root 'profile-areas-isolation.json'
 Assert-KmcNotReparsePoint $areasPath 'Manticore Areas restoration';Assert-KmcNotHardLink $areasPath 'Manticore Areas restoration'
 Assert-KmcManifestJsonMembersUnique $areasPath 'Manticore Areas restoration';$areasHash=Get-KmcSha256 $areasPath;$areas=Read-KmcJson $areasPath
 Assert-KmcExactProperties $areas @('schemaVersion','runId','profileRoot','originalDigest','generatedDigest','generatedQuarantine','originalAreasRestoredExactly') 'Manticore Areas restoration'
 $backup=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-backups/profile-'+$run)) (Join-Path (Get-KmcLabRoot) 'runtime-backups') 'Manticore profile backup'
 $snapshotPath=Join-Path $backup 'snapshot.json';$durablePath=Join-Path $backup 'Areas-isolation.json'
 foreach($item in @($snapshotPath,$durablePath)){
  Assert-KmcNotReparsePoint $item 'Manticore durable Areas state';Assert-KmcNotHardLink $item 'Manticore durable Areas state'
  Assert-KmcManifestJsonMembersUnique $item 'Manticore durable Areas state'
 }
 $snapshotHash=Get-KmcSha256 $snapshotPath;$durableHash=Get-KmcSha256 $durablePath
 $snapshot=Read-KmcJson $snapshotPath;$durable=Read-KmcJson $durablePath
 $generated=Join-Path $backup 'Areas-generated'
 $transaction=Join-Path (Get-KmcLabRoot) ('runtime-state/transactions/'+$run+'.json')
 Assert-KmcNotReparsePoint $transaction 'Manticore transaction';Assert-KmcNotHardLink $transaction 'Manticore transaction'
 Assert-KmcManifestJsonMembersUnique $transaction 'Manticore transaction';$transactionHash=Get-KmcSha256 $transaction;$state=Read-KmcJson $transaction
 Assert-KmcChunk7ManticoreAreasCustody $request $areas $snapshot $durable $state $snapshotHash $generated
 Assert-KmcDirectoryTreeCloneable $generated 'Manticore generated Areas quarantine'
 Assert-KmcBootstrapInventoryString (Get-KmcDirectoryManifest $generated).digest $durable.generatedDigest
 if((Get-KmcSha256 $areasPath)-cne$areasHash-or(Get-KmcSha256 $snapshotPath)-cne$snapshotHash-or
    (Get-KmcSha256 $durablePath)-cne$durableHash-or(Get-KmcSha256 $transaction)-cne$transactionHash-or
    (Get-KmcDirectoryManifest $generated).digest-cne$durable.generatedDigest){
  throw 'Manticore Areas or transaction custody changed during read-only admission.'
 }
 $Set.pins[$areasPath]=$areasHash;$Set.pins[$snapshotPath]=$snapshotHash
 $Set.pins[$durablePath]=$durableHash;$Set.pins[$transaction]=$transactionHash
 [pscustomobject]@{generatedPath=$generated;generatedDigest=$durable.generatedDigest}
}

# Read-only admission of the distinct non-capital Manual produced by the
# source-only Manticore fixture. This proves provenance and settled external
# state; it neither selects an area consumer nor grants a native load/write.
function Get-KmcChunk7ManticoreManualSource([string]$SourceRunId,[string]$ExpectedArtifactSha256,
    [string]$ExpectedArchiveSha256,$Fixture){
 if($SourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or
    $ExpectedArtifactSha256-cnotmatch'^[a-f0-9]{64}$'-or$ExpectedArchiveSha256-cnotmatch'^[a-f0-9]{64}$'-or
    $ExpectedArtifactSha256-ceq$ExpectedArchiveSha256){throw 'Manticore Manual requires distinct exact producer and archive pins.'}
 $set=Get-KmcChunk7OwnedReceiptSet $SourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-checkpoint-save'
 Assert-KmcBootstrapInventoryString $request.persistenceCase 'owner-seed-manticore-fixture-manual'
 $candidate=Get-KmcChunk7RecordedCandidate $request
 Assert-KmcChunk7OwnedSourceCompletion $set $SourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $candidate $request.ownedPairs.sourceArchiveSha256
 $parent=Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource $request.ownedPairs $request.persistenceCase $Fixture $candidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $parent.descriptor)){
  throw 'Manticore producer did not load its exact qualified owner-seed Manual.'
 }
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'owner-seed-manticore-fixture-manual'
 Assert-KmcChunk7OwnedAreaProfileRestoration $set $SourceRunId
 $areasPath=Join-Path $set.root 'profile-areas-isolation.json'
 Assert-KmcNotReparsePoint $areasPath 'Manticore Areas restoration';Assert-KmcNotHardLink $areasPath 'Manticore Areas restoration'
 Assert-KmcManifestJsonMembersUnique $areasPath 'Manticore Areas restoration';$areasHash=Get-KmcSha256 $areasPath;$areas=Read-KmcJson $areasPath
 Assert-KmcExactProperties $areas @('schemaVersion','runId','profileRoot','originalDigest','generatedDigest','generatedQuarantine','originalAreasRestoredExactly') 'Manticore Areas restoration'
 $backup=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-backups/profile-'+$SourceRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-backups') 'Manticore profile backup'
 $snapshotPath=Join-Path $backup 'snapshot.json';$durablePath=Join-Path $backup 'Areas-isolation.json'
 foreach($item in @($snapshotPath,$durablePath)){Assert-KmcNotReparsePoint $item 'Manticore durable Areas state';Assert-KmcNotHardLink $item 'Manticore durable Areas state';Assert-KmcManifestJsonMembersUnique $item 'Manticore durable Areas state'}
 $snapshotHash=Get-KmcSha256 $snapshotPath;$durableHash=Get-KmcSha256 $durablePath
 $snapshot=Read-KmcJson $snapshotPath;$durable=Read-KmcJson $durablePath
 $generated=Join-Path $backup 'Areas-generated'
 $transaction=Join-Path (Get-KmcLabRoot) ('runtime-state/transactions/'+$SourceRunId+'.json')
 Assert-KmcNotReparsePoint $transaction 'Manticore transaction';Assert-KmcNotHardLink $transaction 'Manticore transaction'
 Assert-KmcManifestJsonMembersUnique $transaction 'Manticore transaction';$transactionHash=Get-KmcSha256 $transaction;$state=Read-KmcJson $transaction
 Assert-KmcChunk7ManticoreAreasCustody $request $areas $snapshot $durable $state $snapshotHash $generated
 Assert-KmcDirectoryTreeCloneable $generated 'Manticore generated Areas quarantine'
 Assert-KmcBootstrapInventoryString (Get-KmcDirectoryManifest $generated).digest $durable.generatedDigest
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifactPath=Join-Path $set.root 'chunk7-persistence-evidence.json';$artifactFile=Get-Item -LiteralPath $artifactPath
 if($artifactFile.Length-le0-or$artifactFile.Length-gt16MB-or$set.pins[$artifactPath]-cne$ExpectedArtifactSha256){throw 'Manticore producer evidence differs from the caller pin.'}
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $write=$artifact.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations
 if($leaf-cnotmatch'^Manual_[1-9][0-9]*_KMC_C7_AREA_FIXTURE\.zks$'-or$write.sha256-cne$ExpectedArchiveSha256-or
    $ExpectedArchiveSha256-cin@($request.ownedPairs.sourceArchiveSha256,$request.ownedPairs.seedSha256)){
  throw 'Manticore Manual output identity differs from its distinct source.'
 }
 $path=Join-Path (Join-Path $set.profile 'Saved Games') $leaf
 if($write.path-cne$path){throw 'Manticore native Manual escaped its owned run root.'}
 Assert-KmcNotReparsePoint $path 'Manticore Manual';Assert-KmcNotHardLink $path 'Manticore Manual'
 $file=Get-Item -LiteralPath $path
 if($file.Length-ne$write.length-or$file.Length-le0-or$file.Length-gt256MB-or(Get-KmcSha256 $path)-cne$ExpectedArchiveSha256){
  throw 'Manticore Manual bytes differ from the completed native write.'
 }
 $header=Read-KmcChunk7OwnerSeedHeader $path
 foreach($pair in @(@('Name','KMC_C7_AREA_FIXTURE'),@('Type','Manual'),@('GameId',$request.persistenceLoad.gameId),
    @('GameName',$request.persistenceLoad.gameName),@('Area','314e8c3b15b896a4f847c85f9250f02b'))){
  Assert-KmcBootstrapInventoryString $header.($pair[0]) $pair[1]
 }
 Assert-KmcOrdinaryInteger $header.CompatibilityVersion 1 1
 if((Get-KmcSha256 $path)-cne$ExpectedArchiveSha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks-or
    (Get-KmcSha256 $areasPath)-cne$areasHash-or(Get-KmcSha256 $transaction)-cne$transactionHash-or
    (Get-KmcSha256 $snapshotPath)-cne$snapshotHash-or(Get-KmcSha256 $durablePath)-cne$durableHash-or
    (Get-KmcDirectoryManifest $generated).digest-cne$durable.generatedDigest){
  throw 'Manticore source, Areas or transaction receipt changed during read-only admission.'
 }
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$path;descriptor=[pscustomobject]@{internalName=[string]$header.Name;fileName=$leaf;sha256=$ExpectedArchiveSha256;
  length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=[string]$header.GameId;
  gameName=[string]$header.GameName;area=[string]$header.Area};producerRunId=$SourceRunId;artifactPath=$artifactPath;
  artifactSha256=$ExpectedArtifactSha256;artifactLength=[long]$artifactFile.Length;candidate=$candidate;parent=$parent;fixturePreparationAssisted=$true;nativeAuthorityGranted=$false}
}
# Owner-seed-derived combat: the ACCEPTED cold consumer receipt, pinned by run and evidence hash,
# PASS on exactly this producer's Manual (1 load, 0 writes, separate-process proof), on its own
# recorded candidate, which must equal the producer's (the consumer's own same-candidate rule).
function Get-KmcChunk7OwnerSeedConsumerReceipt($Selection,$Fixture,$ProducerArtifact,$Descriptor){
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-persistence-load';Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or(ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $Descriptor)){throw 'Consumer receipt did not cold-load this exact owner-seed-derived Manual.'}
 $candidate=Get-KmcChunk7RecordedCandidate $request
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $candidate $Selection.sourceArchiveSha256 0
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Owner-seed-derived consumer receipt differs from its explicit immutable pin.'}
 Assert-KmcChunk7OwnerSeedDerivedColdSource $request $ProducerArtifact $artifact
 Assert-KmcChunk7OwnedReceiptPins $set
 [ordered]@{consumerRunId=$request.runId;consumerArtifactPath=$path;consumerArtifactSha256=$Selection.checkpointSourceArtifactSha256;consumerArtifactLength=[long]$file.Length;consumerCandidate=$candidate}
}
# Owner-seed checkpoint consumer: the checkpoint producer's receipt set (PASS, one load, one write),
# The new area producer loads the exact Manticore Manual, with the independently
# accepted batch56 cold load as its checkpoint receipt. The source artifact
# staged for the game remains the Manticore producer, never the older seed world.
function Get-KmcChunk7ManticoreReturnSource($Selection,[string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget){
 $request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-manticore-return';ownedPairs=$Selection;persistenceCase=$PersistenceCase}
 Assert-KmcChunk7OwnedPairsSelection $request
 Assert-KmcChunk7OwnerSeedAreaTarget $Selection $PersistenceCase $AreaTarget
 if($Selection.case-cne'owner-seed-checkpoint-save'-or-not(Test-KmcChunk7ManticoreReturnArea $Selection)){
  throw 'Only the selected Manticore checkpoint producer may consume this source.'
 }
 $manual=Get-KmcChunk7ManticoreManualSource $Selection.sourceRunId $Selection.sourceArtifactSha256 $Selection.sourceArchiveSha256 $Fixture
 Assert-KmcBootstrapInventoryString $manual.descriptor.area $Selection.sourceArea
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$cold=$set.request
 Assert-KmcBootstrapInventoryString $cold.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $cold.ownedPairs.case 'owner-seed-persistence-load'
 Assert-KmcBootstrapInventoryString $cold.persistenceCase 'manticore-fixture-cold-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $cold
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId','sourceArea')){
  Assert-KmcBootstrapInventoryString $cold.ownedPairs.$key $Selection.$key
 }
 if((ConvertTo-KmcChunk7Canonical $cold.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or
    (ConvertTo-KmcChunk7Canonical $cold.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $manual.descriptor)){
  throw 'Accepted Manticore cold receipt substituted the native Manual or full field roster.'
 }
 $coldCandidate=Get-KmcChunk7RecordedCandidate $cold
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $coldCandidate $Selection.sourceArchiveSha256 0
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'manticore-fixture-cold-manual'
 $areaCustody=Assert-KmcChunk7ManticoreAreasReceipt $set
 Assert-KmcChunk7PersistenceArtifact $cold $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $cold $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){
  throw 'Accepted Manticore cold evidence differs from the explicit checkpoint pin.'
 }
 $producer=Read-KmcJson $manual.artifactPath
 Assert-KmcChunk7ManticoreManualColdSource $cold $producer $artifact
 Assert-KmcChunk7OwnedReceiptPins $set
 Assert-KmcBootstrapInventoryString (Get-KmcDirectoryManifest $areaCustody.generatedPath).digest $areaCustody.generatedDigest
 [pscustomobject][ordered]@{path=$manual.path;descriptor=$manual.descriptor;artifactPath=$manual.artifactPath;
  artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.producerRunId;
  ownerSeedDerived=$true;manticoreManual=$true;parent=$manual.parent;seed=$manual.parent.seed;producerCandidate=$manual.candidate;
  acceptedColdRunId=$cold.runId;acceptedColdArtifactSha256=$Selection.checkpointSourceArtifactSha256;
  acceptedColdCandidate=$coldCandidate;currentCandidate=[pscustomobject][ordered]@{
   commit=$Candidate.commit;dllSha256=$Candidate.dllSha256;dllMvid=$Candidate.dllMvid}}
}
# pinned by run and persistence-evidence hash, on its own recorded candidate. Its own source is
# re-derived (Manual producer plus accepted consumer) and must be exactly the Manual it loaded; the
# input is the producer's fresh native slot, described from its nativeWrite. The staged lineage
# receipt stays the Manual producer's world evidence.
function Get-KmcChunk7OwnedPairsOwnerSeedCheckpointSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-checkpoint';ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
 Assert-KmcChunk7OwnedPairsSelection $r
 Assert-KmcChunk7OwnerSeedAreaTarget $Selection $PersistenceCase $AreaTarget
 if(Test-KmcChunk7OwnerSeedCurrentCheckpoint $PersistenceCase){return Get-KmcChunk7OwnedPairsOwnerSeedCurrentSource $Selection $PersistenceCase $Fixture $Candidate}
 $sourceCase=Get-KmcChunk7PersistenceSourceCase $PersistenceCase
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-checkpoint-save';Assert-KmcBootstrapInventoryString $request.persistenceCase $sourceCase
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((Test-KmcChunk7ManticoreReturnArea $request.ownedPairs)-ne(Test-KmcChunk7ManticoreReturnArea $Selection)){throw 'Checkpoint producer and cold consumer selected different area routes.'}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)){throw 'Checkpoint producer substituted the inherited field roster.'}
 $producerCandidate=Get-KmcChunk7RecordedCandidate $request
 if($PersistenceCase-ceq'multi-area-autosave'){
  Assert-KmcBootstrapInventoryString $request.ownedPairs.sourceArea $Selection.sourceArea
  if((ConvertTo-KmcChunk7Canonical $request.persistenceAreaTarget)-cne(ConvertTo-KmcChunk7Canonical $AreaTarget)){throw 'Owner-seed area producer changed its exact controlled native destination.'}
 }
 $manual=if(Test-KmcChunk7ManticoreReturnArea $Selection){
  Get-KmcChunk7ManticoreReturnSource $request.ownedPairs $sourceCase $Fixture $producerCandidate $AreaTarget
 }else{Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource $request.ownedPairs $sourceCase $Fixture $producerCandidate $AreaTarget}
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $manual.descriptor)){throw 'Checkpoint producer did not load the exact owner-seed-derived Manual.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $producerCandidate $Selection.sourceArchiveSha256 1
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $sourceCase
 $areaCustody=$null
 if($PersistenceCase-ceq'multi-area-autosave'){
  if(Test-KmcChunk7ManticoreReturnArea $Selection){$areaCustody=Assert-KmcChunk7ManticoreAreasReceipt $set}
  else{Assert-KmcChunk7OwnedAreaProfileRestoration $set $Selection.checkpointSourceRunId}
 }
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Owner-seed checkpoint differs from its exact successful producer pin.'}
 $w=$artifact.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations;$expected=Join-Path (Join-Path $set.profile 'Saved Games') $leaf
 if($w.path-isnot[string]-or$w.path-cne$expected){throw 'Owner-seed checkpoint escaped its producer run root.'}
 Assert-KmcNotReparsePoint $w.path 'owner-seed checkpoint';Assert-KmcNotHardLink $w.path 'owner-seed checkpoint'
 if((Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Owner-seed checkpoint bytes changed after qualification.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long](Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks;gameId=$manual.descriptor.gameId;gameName=$manual.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-checkpoint';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture;persistenceAreaTarget=$AreaTarget})
 Assert-KmcChunk7OwnedReceiptPins $set
 if($null-ne$areaCustody){Assert-KmcBootstrapInventoryString (Get-KmcDirectoryManifest $areaCustody.generatedPath).digest $areaCustody.generatedDigest}
 [pscustomobject][ordered]@{path=$w.path;descriptor=$descriptor;artifactPath=$manual.artifactPath;artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.sourceRunId;ownerSeedDerived=$true;seed=$manual.seed;checkpointRunId=$request.runId;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length;checkpointCandidate=$producerCandidate;manual=$manual}
}
function Get-KmcChunk7OwnedPairsOwnerSeedCurrentSource($Selection,[string]$PersistenceCase,$Fixture,$Candidate){
 $parentCase=Get-KmcChunk7OwnerSeedCurrentParentCase $PersistenceCase
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-checkpoint-load';Assert-KmcBootstrapInventoryString $request.persistenceCase $parentCase
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)){throw 'Current-format parent substituted the inherited field roster.'}
 $parentCandidate=Get-KmcChunk7RecordedCandidate $request
 $original=Get-KmcChunk7OwnedPairsOwnerSeedCheckpointSource $request.ownedPairs $parentCase $Fixture $parentCandidate
 # The schema-1 parent's actual input is the separately receipted derivative;
 # its owner/save authority must bind that hash, never its native schema-3 ancestor.
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $parentCandidate $request.persistenceLoad.sha256 1
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $parentCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Current-format parent differs from its exact accepted cold/resave pin.'}
 Assert-KmcChunk7OwnerSeedCheckpointColdSource $request (Read-KmcJson $original.checkpointArtifactPath) $artifact
 if($parentCase-ceq'owned-schema1-resave-manual'){Assert-KmcOwnedSchema1ColdSource $request $original $artifact}
 if($parentCase-ceq'legacy-mounted-pending-tb-manual'){Assert-KmcOwnedSchema2ColdSource $request $original $artifact}
 $w=$artifact.observations.nativeWrite;Assert-KmcOrdinaryInteger $w.sourceSchema 3 3
 $leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations;$expected=Join-Path (Join-Path $set.profile 'Saved Games') $leaf
 Assert-KmcBootstrapInventoryString $w.path $expected
 Assert-KmcNotReparsePoint $w.path 'current-format native resave';Assert-KmcNotHardLink $w.path 'current-format native resave'
 $output=Get-Item -LiteralPath $w.path
 if($output.Length-ne$w.length-or(Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Current-format native resave bytes differ from the accepted parent.'}
 $stream=[IO.File]::Open($w.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{$actual=Get-KmcOwnedSchema1Members $stream}finally{$stream.Dispose()}
 if($actual.metadata.SchemaVersion-ne3-or(ConvertTo-KmcChunk7Canonical $actual.metadata)-cne(ConvertTo-KmcChunk7Canonical $w.archive)-or(Get-KmcSha256 $w.path)-cne$w.sha256-or(Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks-ne$output.LastWriteTimeUtc.Ticks){throw 'Current-format native metadata differs from its accepted readback or changed during qualification.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long]$output.LastWriteTimeUtc.Ticks;gameId=$original.descriptor.gameId;gameName=$original.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-current';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject][ordered]@{path=$w.path;descriptor=$descriptor;artifactPath=$original.artifactPath;artifactSha256=$original.artifactSha256;artifactLength=$original.artifactLength;sourceRunId=$original.sourceRunId;ownerSeedDerived=$true;seed=$original.seed;checkpointRunId=$request.runId;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length;checkpointCandidate=$parentCandidate;manual=$original.manual}
}
function Copy-KmcChunk7OwnerSeedCurrentParent($Source,[string]$ProfileRoot,$Lock,$Request){
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)-or-not(Test-KmcChunk7OwnerSeedCurrentCheckpoint $Request.persistenceCase)){throw 'Current-format parent copy requires its exact load-only selection.'}
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'current-format child profile';Assert-KmcDirectoryTreeCloneable $run 'current-format child profile'
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Lock.RunId))){throw 'Current-format child profile differs from the current lock.'}
 $owner=Read-KmcJson (Join-Path $run 'owner.json');foreach($spec in @(@('runId',$Request.runId),@('transactionToken',$Lock.Token),@('scenario','chunk7-owned-two-pair-campaign'),@('persistenceCase',$Request.persistenceCase),@('sourceSha256',$Request.persistenceLoad.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($spec[0]) $spec[1]}
 $path=$Source.checkpointArtifactPath;$sha=$Request.ownedPairs.checkpointSourceArtifactSha256
 [void](Read-KmcOwnedSchema1PinnedJson $path $sha 16MB)
 $input=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$output=$null;$destination=Join-Path $run 'owner-seed-current-parent.json'
 try{$output=[IO.File]::Open($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$input.CopyTo($output)}finally{if($output){$output.Dispose()};$input.Dispose()}
 if((Get-KmcSha256 $destination)-cne$sha-or(Get-KmcSha256 $path)-cne$sha){throw 'Accepted current-format parent changed during locked copy.'}
}
function Get-KmcChunk7OwnedPairsSmallSource($Selection,[string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $run='unassigned-small-persistence-source'
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$PersistenceCase})
 if($null-ne$AreaTarget){throw 'Small persistence carries no area-transfer target.'}
 $f09=[pscustomobject]@{sourceRunId=$Selection.sourceRunId;sourceArtifactSha256=$Selection.sourceArtifactSha256;
  sourceArchiveSha256=$Selection.sourceArchiveSha256;coldReceiptSha256=$Selection.coldReceiptSha256;
  actorId=$Selection.riderBId;petId=$Selection.mountBId}
 $source=Get-KmcChunk7SmallF09CalibrationSource $f09 $Fixture $Candidate
 Assert-KmcBootstrapInventoryString $source.descriptor.sha256 $Selection.sourceArchiveSha256
 $artifact=Read-KmcJson $source.artifactPath
 $o=$artifact.observations
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[0].riderId $Selection.riderAId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[0].horseId $Selection.mountAId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[1].riderId $Selection.riderBId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[1].horseId $Selection.mountBId
 if((ConvertTo-KmcChunk7Canonical @($o.fieldWorld.party.id|Sort-Object -CaseSensitive))-cne
    (ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Small persistence source substituted its seven-actor native roster.'}
 if((Test-KmcChunk7OwnedPairsSmallSave ([pscustomobject]@{ownedPairs=$Selection}))-or
    (Test-KmcChunk7OwnedPairsSmallThirdHorse ([pscustomobject]@{ownedPairs=$Selection}))-or
    (Test-KmcChunk7OwnedPairsSmallThirdHorseSave ([pscustomobject]@{ownedPairs=$Selection}))){
  Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$source.descriptor;fixture=$Fixture})
  return [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;
   artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;sourceRunId=$source.sourceRunId;
   historicalProducer=$source.historicalProducer;coldReaderReceiptSha256=$source.coldReaderReceiptSha256;
   fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 }
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'small-fixture-persistence-save'
 Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','coldReceiptSha256','riderAId','mountAId','riderBId','mountBId')){
  Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key
 }
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or
   (ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $source.descriptor)){
  throw 'Small mounted checkpoint producer substituted its qualified F09 input.'
 }
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $PersistenceCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $checkpoint=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $checkpointPath=Join-Path $set.root 'chunk7-persistence-evidence.json';$checkpointFile=Get-Item -LiteralPath $checkpointPath
 if($checkpointFile.Length-le0-or$checkpointFile.Length-gt16MB-or$set.pins[$checkpointPath]-cne$Selection.checkpointSourceArtifactSha256){throw 'Small mounted checkpoint receipt differs from its exact producer pin.'}
 $w=$checkpoint.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $checkpoint.observations
 Assert-KmcBootstrapInventoryString $w.nativeName 'KMC_C7_SMALL_WORLD'
 Assert-KmcBootstrapInventoryString $w.path (Join-Path (Join-Path $set.profile 'Saved Games') $leaf)
 Assert-KmcNotReparsePoint $w.path 'Small mounted checkpoint';Assert-KmcNotHardLink $w.path 'Small mounted checkpoint'
 $file=Get-Item -LiteralPath $w.path
 if($file.Length-ne$w.length-or(Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Small mounted checkpoint bytes differ from completed native write.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;
  lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$source.descriptor.gameId;
  gameName=$source.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;
  persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$source.artifactPath;
  artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;sourceRunId=$source.sourceRunId;
  checkpointArtifactPath=$checkpointPath;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;
  checkpointArtifactLength=[long]$checkpointFile.Length;checkpointCandidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid};
  fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
function Get-KmcChunk7OwnedPairsMediumSource($Selection,[string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $run='unassigned-medium-persistence-source'
 Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$PersistenceCase})
 if($null-ne$AreaTarget){throw 'Medium persistence carries no area-transfer target.'}
 $source=Get-KmcChunk7MediumMountedSource $Fixture $Candidate
 foreach($pair in @(@('sourceRunId',$source.sourceRunId),@('sourceArtifactSha256',$source.artifactSha256),
  @('sourceArchiveSha256',$source.descriptor.sha256),@('coldReceiptSha256',$source.coldArtifactSha256))){
  Assert-KmcBootstrapInventoryString $Selection.($pair[0]) $pair[1]
 }
 $artifact=Read-KmcJson $source.artifactPath;$o=$artifact.observations
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[0].riderId $Selection.riderAId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[0].horseId $Selection.mountAId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[1].riderId $Selection.riderBId
 Assert-KmcBootstrapInventoryString $o.acquiredOwners[1].horseId $Selection.mountBId
 if((ConvertTo-KmcChunk7Canonical @($o.fieldWorld.party.id|Sort-Object -CaseSensitive))-cne
    (ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Medium persistence source substituted its seven-actor native roster.'}
 if(Test-KmcChunk7OwnedPairsMediumSave ([pscustomobject]@{ownedPairs=$Selection})){
  Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$source.descriptor;fixture=$Fixture})
  return [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;
   artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;sourceRunId=$source.sourceRunId;
   producerCandidate=$source.producerCandidate;coldArtifactSha256=$source.coldArtifactSha256;
   fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 }
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'medium-fixture-persistence-save'
 Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','coldReceiptSha256','riderAId','mountAId','riderBId','mountBId')){
  Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key
 }
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or
   (ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $source.descriptor)){
  throw 'Medium mounted checkpoint producer substituted its qualified paid input.'
 }
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $PersistenceCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $checkpoint=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $checkpointPath=Join-Path $set.root 'chunk7-persistence-evidence.json';$checkpointFile=Get-Item -LiteralPath $checkpointPath
 if($checkpointFile.Length-le0-or$checkpointFile.Length-gt16MB-or$set.pins[$checkpointPath]-cne$Selection.checkpointSourceArtifactSha256){throw 'Medium mounted checkpoint receipt differs from its exact producer pin.'}
 $w=$checkpoint.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $checkpoint.observations
 Assert-KmcBootstrapInventoryString $w.nativeName 'KMC_C7_MEDIUM_WORLD'
 Assert-KmcBootstrapInventoryString $w.path (Join-Path (Join-Path $set.profile 'Saved Games') $leaf)
 Assert-KmcNotReparsePoint $w.path 'Medium mounted checkpoint';Assert-KmcNotHardLink $w.path 'Medium mounted checkpoint'
 $file=Get-Item -LiteralPath $w.path
 if($file.Length-ne$w.length-or(Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Medium mounted checkpoint bytes differ from completed native write.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;
  lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$source.descriptor.gameId;
  gameName=$source.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;ownedPairs=$Selection;
  persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$source.artifactPath;
  artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;sourceRunId=$source.sourceRunId;
  checkpointArtifactPath=$checkpointPath;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;
  checkpointArtifactLength=[long]$checkpointFile.Length;checkpointCandidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid};
  fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
function Get-KmcChunk7OwnedPairsSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 if($null-ne$Selection-and$Selection.case-ceq'six-horse-cold-fixture-lifecycle-stress'){
  # The runner's unselected string parameter binds as "". Normalize only at
  # this typed source boundary; wire selection still requires JSON null.
  $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-six-pair-stress';ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
  Assert-KmcChunk7OwnedPairsSelection $r
  if($null-ne$AreaTarget){throw 'Six-pair stress admits no Areas source or destination.'}
  $source=Get-KmcChunk7SixPairCheckpointSource $Fixture
  $r|Add-Member persistenceLoad $source.descriptor;$r|Add-Member fixture $Fixture;Assert-KmcChunk7OwnedPairsDescriptor $r
  $source|Add-Member sixPairCheckpoint $true;$source|Add-Member sourceRunId $source.qualificationRunId
  return $source
 }
 if($null-ne$Selection-and$Selection.case-cin@('six-horse-mounted-persistence-save','six-horse-mounted-persistence-load')){
  return Get-KmcChunk7SixPairMountedSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget
 }
 if($null-ne$Selection-and$Selection.case-cin@('third-horse-fixture-fourth-horse-setup','third-horse-fixture-fourth-horse-persistence-save')){
  $save=$Selection.case-ceq'third-horse-fixture-fourth-horse-persistence-save'
  if ($save) {
   if ($PersistenceCase -cne 'multi-exploration-manual') { throw 'Fourth-Horse Manual requires its exact checkpoint.' }
  } elseif (-not [string]::IsNullOrEmpty($PersistenceCase)) { throw 'Fourth-Horse setup requires no checkpoint.' }
  if ($null -ne $AreaTarget) { throw 'Fourth-Horse setup/Manual admits no area route.' }
  Assert-KmcChunk7OwnedPairsSelection ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-fourth-horse';ownedPairs=$Selection;persistenceCase=$PersistenceCase})
  $source=Get-KmcChunk7F09EightActorSource $Fixture
  $request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-fourth-horse';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$source.descriptor;fixture=$Fixture}
  Assert-KmcChunk7OwnedPairsDescriptor $request
  if($source.producerRunId-cne$Selection.sourceRunId-or$source.producerArtifactSha256-cne$Selection.sourceArtifactSha256-or
     $source.coldArtifactSha256-cne$Selection.coldReceiptSha256-or$source.descriptor.sha256-cne$Selection.sourceArchiveSha256-or
     (ConvertTo-KmcChunk7Canonical @($source.rosterIds|Sort-Object -CaseSensitive))-cne
      (ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Fourth-Horse source gate and selected immutable input differ.'}
  $artifact=Get-Item -LiteralPath $source.producerArtifactPath
  return [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.producerArtifactPath;
   artifactSha256=$source.producerArtifactSha256;artifactLength=[long]$artifact.Length;sourceRunId=$source.producerRunId;
   coldReaderReceiptSha256=$source.coldArtifactSha256;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 }
 if($null-ne$Selection-and$Selection.case-cin@('small-fixture-persistence-save','small-fixture-persistence-load','small-fixture-third-horse-setup','small-fixture-third-horse-persistence-save')){
  return Get-KmcChunk7OwnedPairsSmallSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget
 }
 if($null-ne$Selection-and$Selection.case-cin@('medium-fixture-persistence-save','medium-fixture-persistence-load')){
  return Get-KmcChunk7OwnedPairsMediumSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget
 }
 if((Test-KmcChunk7ManticoreReturnArea $Selection)-and$Selection.case-ceq'owner-seed-checkpoint-save'){
  return Get-KmcChunk7ManticoreReturnSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget
 }
 if($null-ne$Selection-and$Selection.case-ceq'owner-seed-persistence-load'-and$PersistenceCase-ceq'manticore-fixture-cold-manual'){
  $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-manticore-cold';ownedPairs=$Selection;persistenceCase=$PersistenceCase}
  Assert-KmcChunk7OwnedPairsSelection $r
  if($null-ne$AreaTarget){throw 'Manticore Manual cold load cannot request an area transfer.'}
  $source=Get-KmcChunk7ManticoreManualSource $Selection.sourceRunId $Selection.sourceArtifactSha256 $Selection.sourceArchiveSha256 $Fixture
  $parent=$source.parent
  $parentArtifact=Read-KmcJson $parent.artifactPath;$manual=$parentArtifact.observations.ownerSeedManual
  Assert-KmcBootstrapInventoryString $manual.fieldWorld.mainActorId $Selection.riderAId
  Assert-KmcBootstrapInventoryString $manual.seedId $Selection.seedId
  Assert-KmcBootstrapInventoryString $manual.seedSha256 $Selection.seedSha256
  $roles=@($Selection.riderAId,$Selection.mountAId,$Selection.riderBId,$Selection.mountBId)
  for($i=0;$i-lt2;$i++){Assert-KmcBootstrapInventoryString $manual.twoOwners[$i].actorId $roles[$i*2];Assert-KmcBootstrapInventoryString $manual.twoOwners[$i].petId $roles[$i*2+1]}
  if((ConvertTo-KmcChunk7Canonical @($manual.fieldWorld.party|ForEach-Object{$_.id}|Sort-Object -CaseSensitive))-cne
     (ConvertTo-KmcChunk7Canonical @($Selection.partyIds|Sort-Object -CaseSensitive))){throw 'Manticore Manual cold roster differs from its owner-seed parent.'}
  $producer=Read-KmcJson $source.artifactPath
  Assert-KmcBootstrapInventoryString $producer.observations.ownerSeedCheckpointSource.manualProducerRunId $parent.sourceRunId
  Assert-KmcBootstrapInventoryString $producer.observations.ownerSeedCheckpointSource.manualProducerArtifactSha256 $parent.artifactSha256
  Assert-KmcBootstrapInventoryString $producer.observations.ownerSeedCheckpointSource.manualSha256 $parent.descriptor.sha256
  if((ConvertTo-KmcChunk7Canonical $producer.observations.manticoreFixturePreparation.afterSave.field.custody.partyIds)-cne
     (ConvertTo-KmcChunk7Canonical $Selection.partyIds)){throw 'Manticore producer changed the selected field roster.'}
  Assert-KmcBootstrapInventoryString $Selection.sourceArea $source.descriptor.area
  Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-manticore-cold';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$source.descriptor;fixture=$Fixture})
  return [pscustomobject][ordered]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;
   artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;sourceRunId=$source.producerRunId;
   ownerSeedDerived=$true;manticoreManual=$true;parent=$parent;producerCandidate=$source.candidate;
   currentCandidate=[pscustomobject][ordered]@{commit=$Candidate.commit;dllSha256=$Candidate.dllSha256;dllMvid=$Candidate.dllMvid}}
 }
 if($null-ne$Selection-and$null-ne$Selection.PSObject.Properties['case']-and$Selection.case-cin@('owner-seed-exploration','owner-seed-persistence-save')){return Get-KmcChunk7OwnedPairsOwnerSeedSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget}
 if($null-ne$Selection-and$null-ne$Selection.PSObject.Properties['case']-and$Selection.case-cin@('owner-seed-persistence-load','owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-combat-disable','owner-seed-checkpoint-save','owner-seed-load-refusal')){return Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget}
 if($null-ne$Selection-and$null-ne$Selection.PSObject.Properties['case']-and$Selection.case-ceq'owner-seed-checkpoint-load'){return Get-KmcChunk7OwnedPairsOwnerSeedCheckpointSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget}
 if($PersistenceCase-ceq'multi-area-autosave'){Assert-KmcChunk7AreaTarget ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';persistenceCase=$PersistenceCase;ownedPairs=$Selection;persistenceAreaTarget=$AreaTarget})}elseif($null-ne$AreaTarget){throw 'Only owned area autosave may carry its exact destination.'}
 $manual=Get-KmcChunk7OwnedPairsManualSource $Selection $PersistenceCase $Fixture $Candidate
 if($Selection.case-cne'persistence-load'){return $manual}
 $sourceCase=Get-KmcChunk7PersistenceSourceCase $PersistenceCase
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'persistence-save';Assert-KmcBootstrapInventoryString $request.persistenceCase $sourceCase
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or(ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $manual.descriptor)){throw 'Checkpoint source substituted inherited ancestry or the immutable Manual.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $sourceCase
 if($PersistenceCase-ceq'multi-area-autosave'){
  Assert-KmcBootstrapInventoryString $request.ownedPairs.sourceArea $Selection.sourceArea
  if((ConvertTo-KmcChunk7Canonical $request.persistenceAreaTarget)-cne(ConvertTo-KmcChunk7Canonical $AreaTarget)){throw 'Owned cold area source changed its admitted native destination.'}
  Assert-KmcChunk7OwnedAreaProfileRestoration $set $Selection.checkpointSourceRunId
 }
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Cold checkpoint differs from its exact successful source pin.'}
 $w=$artifact.observations.nativeWrite;$archive=$w.archive;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long](Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks;gameId=$manual.descriptor.gameId;gameName=$manual.descriptor.gameName;area=$archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-cold';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture;persistenceAreaTarget=$AreaTarget})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$manual.artifactPath;artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.sourceRunId;ancestry=$manual.ancestry;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length;checkpointCandidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}}
}
function Copy-KmcChunk7OwnedPairsSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'inherited two-owner profile';Assert-KmcDirectoryTreeCloneable $run 'inherited two-owner profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-owned-two-pair-campaign'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Inherited copy lacks exact current lock/profile ownership.'}
 $destination=Join-Path $run 'owned-two-pair-source.json';if(Test-Path -LiteralPath $destination){throw 'Inherited lineage receipt destination already exists.'}
 # Select from the lock-owned archive before any other source-kind early return.
 $six=$Source.descriptor.sha256-ceq'b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'
 if($six-and$null-ne$Source.PSObject.Properties['ownerSeed']){throw 'Qualified six-pair Manual cannot impersonate an owner seed.'}
 if(-not$six-and$null-ne$Source.PSObject.Properties['sixPairCheckpoint']){throw 'Six-pair checkpoint marker is attached to another source archive.'}
 if($null-ne$Source.PSObject.Properties['ownerSeed']-and$Source.ownerSeed-eq$true){
  # The honest owner-seed source record (cold kind's writer); no producer receipt exists to copy.
  $ownerPath=Join-Path $run 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'owner-seed profile owner';Assert-KmcNotHardLink $ownerPath 'owner-seed profile owner'
  Assert-KmcNotReparsePoint $Source.path 'owner seed master';Assert-KmcNotHardLink $Source.path 'owner seed master'
  if((Get-KmcSha256 $Source.path)-cne$Source.descriptor.sha256-or(Get-KmcSha256 $Source.intakePath)-cne$Source.intakeSha256){throw 'Owner seed master or intake changed before locked record placement.'}
  return Write-KmcChunk7OwnerSeedSourceRecord $Source $destination $ownerPath (Get-KmcSha256 $ownerPath) $Lock
 }
 Assert-KmcNotReparsePoint $Source.artifactPath 'inherited lineage';Assert-KmcNotHardLink $Source.artifactPath 'inherited lineage'
 # The lock-owned archive identity selects this route. An omitted optional
 # marker must not demote Manual308 to the older generic receipt copier.
 if($six){
  if($null-eq$Source.PSObject.Properties['sixPairCheckpoint']){throw 'Six-pair source copy lacks its qualification marker.'}
  Assert-KmcChunk7Boolean $Source.sixPairCheckpoint $true
  Assert-KmcBootstrapInventoryString $Source.artifactPath (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/chunk7-assisted-cold-inventory-evidence.json')
  Assert-KmcBootstrapInventoryString $Source.artifactSha256 '012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241'
  Assert-KmcBootstrapInventoryString $Source.sourceRunId '20261022-c7b-world185-b185s1-sixthhorse-cold'
  Assert-KmcBootstrapInventoryString $Source.archiveProducerRunId '20261022-c7b-world184-b184s1-sixthhorse-manual'
  Assert-KmcChunk7SixPairCheckpointPins $Source.receiptPins
  Assert-KmcAssistedSettledFile $Source.path $Source.descriptor 'six-pair immutable copied Manual308'
 }
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Inherited lineage changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Inherited lineage changed during locked copy.'}
 if($six){Assert-KmcChunk7SixPairCheckpointPins $Source.receiptPins;Assert-KmcAssistedSettledFile $Source.path $Source.descriptor 'six-pair immutable copied Manual308'}
}

# Only the new owned-area chain requires this additional exact profile result.
# Legacy owned/Working cases retain their existing qualification contracts.
function Assert-KmcChunk7OwnedAreaProfileRestoration($Set,[string]$RunId){
 $path=Join-Path $Set.root 'profile-cache-changes.json';Assert-KmcNotReparsePoint $path 'owned area profile restoration';Assert-KmcNotHardLink $path 'owned area profile restoration'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt16MB){throw 'Owned area profile restoration receipt is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'owned area profile restoration';$v=Read-KmcJson $path
 if($null-eq$v-or$v.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'Owned area profile restoration must be a scalar object.'}
 Assert-KmcOrdinaryInteger $v.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $v.runId $RunId;Assert-KmcBootstrapInventoryString $v.policy 'native-achievement-cache-settled-size'
 if($v.snapshotProfileDigest-isnot[string]-or$v.snapshotProfileDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Owned area source lacks its current-profile preservation digest.'}
 Assert-KmcChunk7Boolean $v.profileBytesRestoredExactly $true
 if($v.acceptedNativeCacheChanges-isnot[Array]-or$v.acceptedNativeCacheChanges.Count-ne0){throw 'Owned area source profile or native caches were not restored exactly.'}
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).Length-ne$file.Length-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Owned area restoration evidence changed during qualification.'}
 $Set.pins[$path]=$sha
}
function Assert-KmcChunk7OwnedAreaColdSource($Source,$Request,$ColdArtifact){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if($Request.ownedPairs.case-cnotin@('persistence-load','owner-seed-checkpoint-load')-or$Request.persistenceCase-cne'multi-area-autosave'){throw 'Owned area cold proof is valid only for its exact one-load/zero-write route.'}
 $path=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$Request.ownedPairs.checkpointSourceRunId+'/chunk7-persistence-evidence.json')
 Assert-KmcBootstrapInventoryString $Source.checkpointArtifactPath $path;Assert-KmcBootstrapInventoryString $Source.checkpointArtifactSha256 $Request.ownedPairs.checkpointSourceArtifactSha256
 Assert-KmcNotReparsePoint $path 'owned area checkpoint';Assert-KmcNotHardLink $path 'owned area checkpoint';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$file.Length-ne$Source.checkpointArtifactLength-or(Get-KmcSha256 $path)-cne$Source.checkpointArtifactSha256){throw 'Owned area cold source changed after completed qualification.'}
 Assert-KmcManifestJsonMembersUnique $path 'owned area checkpoint';$parent=Read-KmcJson $path
 # The accepted producer is already pinned and revalidated against its own
 # request by Get-KmcChunk7OwnedPairsSource. A later read-only consumer may use
 # another candidate; compare each receipt to the candidate it actually ran.
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $parent.$key $Source.checkpointCandidate.$key}
 if($parent.commit-cnotmatch'^[a-f0-9]{40}$'-or$parent.dllSha256-cnotmatch'^[a-f0-9]{64}$'-or$parent.dllMvid-cnotmatch'^[a-f0-9-]{36}$'){throw 'Owned area producer candidate identity is malformed.'}
 Assert-KmcBootstrapInventoryString $parent.runId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $parent.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $parent.status 'PASS'
 if(($parent.observations.inheritedFixture.sourceCase-ceq'owner-seed-manticore-fixture-manual')-ne
    (Test-KmcChunk7ManticoreReturnArea $Request.ownedPairs)){
  throw 'Owned area cold source and consumer selected different native routes.'
 }
 Assert-KmcChunk7PersistenceObservation $Request $ColdArtifact 'PASS'
 $prior=$parent.observations;$cold=$ColdArtifact.observations
 Assert-KmcBootstrapInventoryString $prior.checkpoint 'multi-area-autosave';Assert-KmcChunk7Boolean $prior.cold $false
 Assert-KmcOrdinaryInteger $prior.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $cold.processId 1 ([int]::MaxValue)
 if($prior.processId-eq$cold.processId-or$prior.nativeWrite.length-ne$Request.persistenceLoad.length-or(ConvertTo-KmcChunk7Canonical $prior.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $cold.loadedArchive)){throw 'Owned area cold process did not restore the exact completed destination Auto graph.'}
 Assert-KmcBootstrapInventoryString $prior.nativeWrite.sha256 $Request.persistenceLoad.sha256
 foreach($spec in @(@('enterPoint',$Request.persistenceAreaTarget.enterPoint),@('destinationArea',$Request.persistenceAreaTarget.area),@('mode','AfterEntry'),@('sourceArea',$Request.ownedPairs.sourceArea))){Assert-KmcBootstrapInventoryString $prior.areaTransfer.($spec[0]) $spec[1]}
 foreach($key in @('partyIds','currentCharacterIds','remoteCapturedIds','arrangement')){if((ConvertTo-KmcChunk7Canonical $prior.inheritedCleanup.$key)-cne(ConvertTo-KmcChunk7Canonical $cold.inheritedFixture.$key)){throw 'Owned area cold source changed full-field ancestry or progression.'}}
 for($i=0;$i-lt2;$i++){foreach($key in @('riderId','mountId','sourceGuid','rank','profileId')){if((ConvertTo-KmcChunk7Canonical $prior.inheritedCleanup.pairs[$i].$key)-cne(ConvertTo-KmcChunk7Canonical $cold.inheritedFixture.pairs[$i].$key)){throw 'Owned area cold source changed inherited role/fact provenance.'}}}
 if((Get-KmcSha256 $path)-cne$Source.checkpointArtifactSha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Owned area source changed during cold graph comparison.'}
}
