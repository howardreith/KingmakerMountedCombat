Set-StrictMode -Version Latest
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
 if($null-ne$set.owner.persistenceCase){throw 'Two-owner Manual owner unexpectedly selects a persistence checkpoint.'}
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
 if($null-ne$AreaTarget){throw 'Owner-seed-derived cold load carries no area target.'}
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-world-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-persistence-save';Assert-KmcBootstrapInventoryString $request.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('seedId','seedSha256','riderAId','riderBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 # The owner's immutable master is unchanged and is exactly the seed the producer loaded.
 $seed=Get-KmcChunk7OwnedPairsOwnerSeedSource $request.ownedPairs 'multi-exploration-manual' $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $seed.descriptor)-cne(ConvertTo-KmcChunk7Canonical $request.persistenceLoad)){throw 'Producer did not load the exact registered immutable owner seed.'}
 # Combat (scenarios 3-5) and the checkpoint producer may be a later candidate: each receipt is pinned to its own recorded candidate.
 $combat=$Selection.case-cin@('owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-checkpoint-save')
 $producerCandidate=if($combat){Get-KmcChunk7RecordedCandidate $request}else{$Candidate}
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
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-derived';ownedPairs=$Selection;persistenceCase=$r.persistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
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
# pinned by run and persistence-evidence hash, on its own recorded candidate. Its own source is
# re-derived (Manual producer plus accepted consumer) and must be exactly the Manual it loaded; the
# input is the producer's fresh native slot, described from its nativeWrite. The staged lineage
# receipt stays the Manual producer's world evidence.
function Get-KmcChunk7OwnedPairsOwnerSeedCheckpointSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 $r=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-checkpoint';ownedPairs=$Selection;persistenceCase=$(if([string]::IsNullOrEmpty($PersistenceCase)){$null}else{$PersistenceCase})}
 Assert-KmcChunk7OwnedPairsSelection $r
 if($null-ne$AreaTarget){throw 'Owner-seed checkpoint cold load carries no area target.'}
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'owner-seed-checkpoint-save';Assert-KmcBootstrapInventoryString $request.persistenceCase $PersistenceCase
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)){throw 'Checkpoint producer substituted the inherited field roster.'}
 $producerCandidate=Get-KmcChunk7RecordedCandidate $request
 $manual=Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource $request.ownedPairs $PersistenceCase $Fixture $producerCandidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $manual.descriptor)){throw 'Checkpoint producer did not load the exact owner-seed-derived Manual.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $producerCandidate $Selection.sourceArchiveSha256 1
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $PersistenceCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Owner-seed checkpoint differs from its exact successful producer pin.'}
 $w=$artifact.observations.nativeWrite;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations;$expected=Join-Path (Join-Path $set.profile 'Saved Games') $leaf
 if($w.path-isnot[string]-or$w.path-cne$expected){throw 'Owner-seed checkpoint escaped its producer run root.'}
 Assert-KmcNotReparsePoint $w.path 'owner-seed checkpoint';Assert-KmcNotHardLink $w.path 'owner-seed checkpoint'
 if((Get-KmcSha256 $w.path)-cne$w.sha256){throw 'Owner-seed checkpoint bytes changed after qualification.'}
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long](Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks;gameId=$manual.descriptor.gameId;gameName=$manual.descriptor.gameName;area=$w.archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-seed-checkpoint';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject][ordered]@{path=$w.path;descriptor=$descriptor;artifactPath=$manual.artifactPath;artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.sourceRunId;ownerSeedDerived=$true;seed=$manual.seed;checkpointRunId=$request.runId;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length;checkpointCandidate=$producerCandidate;manual=$manual}
}
function Get-KmcChunk7OwnedPairsSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){
 if($null-ne$Selection-and$null-ne$Selection.PSObject.Properties['case']-and$Selection.case-cin@('owner-seed-exploration','owner-seed-persistence-save')){return Get-KmcChunk7OwnedPairsOwnerSeedSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget}
 if($null-ne$Selection-and$null-ne$Selection.PSObject.Properties['case']-and$Selection.case-cin@('owner-seed-persistence-load','owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-checkpoint-save')){return Get-KmcChunk7OwnedPairsOwnerSeedDerivedSource $Selection $PersistenceCase $Fixture $Candidate $AreaTarget}
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
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$manual.artifactPath;artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.sourceRunId;ancestry=$manual.ancestry;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length}
}
function Copy-KmcChunk7OwnedPairsSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'inherited two-owner profile';Assert-KmcDirectoryTreeCloneable $run 'inherited two-owner profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-owned-two-pair-campaign'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Inherited copy lacks exact current lock/profile ownership.'}
 $destination=Join-Path $run 'owned-two-pair-source.json';if(Test-Path -LiteralPath $destination){throw 'Inherited lineage receipt destination already exists.'}
 if($null-ne$Source.PSObject.Properties['ownerSeed']-and$Source.ownerSeed-eq$true){
  # The honest owner-seed source record (cold kind's writer); no producer receipt exists to copy.
  $ownerPath=Join-Path $run 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'owner-seed profile owner';Assert-KmcNotHardLink $ownerPath 'owner-seed profile owner'
  Assert-KmcNotReparsePoint $Source.path 'owner seed master';Assert-KmcNotHardLink $Source.path 'owner seed master'
  if((Get-KmcSha256 $Source.path)-cne$Source.descriptor.sha256-or(Get-KmcSha256 $Source.intakePath)-cne$Source.intakeSha256){throw 'Owner seed master or intake changed before locked record placement.'}
  return Write-KmcChunk7OwnerSeedSourceRecord $Source $destination $ownerPath (Get-KmcSha256 $ownerPath) $Lock
 }
 Assert-KmcNotReparsePoint $Source.artifactPath 'inherited lineage';Assert-KmcNotHardLink $Source.artifactPath 'inherited lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Inherited lineage changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Inherited lineage changed during locked copy.'}
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
 if($Request.ownedPairs.case-cne'persistence-load'-or$Request.persistenceCase-cne'multi-area-autosave'){throw 'Owned area cold proof is valid only for its exact one-load/zero-write route.'}
 $path=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$Request.ownedPairs.checkpointSourceRunId+'/chunk7-persistence-evidence.json')
 Assert-KmcBootstrapInventoryString $Source.checkpointArtifactPath $path;Assert-KmcBootstrapInventoryString $Source.checkpointArtifactSha256 $Request.ownedPairs.checkpointSourceArtifactSha256
 Assert-KmcNotReparsePoint $path 'owned area checkpoint';Assert-KmcNotHardLink $path 'owned area checkpoint';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$file.Length-ne$Source.checkpointArtifactLength-or(Get-KmcSha256 $path)-cne$Source.checkpointArtifactSha256){throw 'Owned area cold source changed after completed qualification.'}
 Assert-KmcManifestJsonMembersUnique $path 'owned area checkpoint';$parent=Read-KmcJson $path
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $parent.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $parent.runId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $parent.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $parent.status 'PASS'
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
