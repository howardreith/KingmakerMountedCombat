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
function Assert-KmcChunk7OwnedSourceCompletion($Set,[string]$RunId,[string]$Scenario,$Fixture,$Candidate,[string]$InputHash){
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
 foreach($key in @('loadRequestCount','workingLoadRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key 1 1}
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
 $w=$artifact.observations.nativeWrite;$descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 # Validate the immutable source descriptor as the source mode, without changing
 # the real request or selecting a native action. Cold source is checked below.
 $sourceSelection=$Selection|ConvertTo-Json -Depth 16|ConvertFrom-Json;$sourceSelection.case='exploration';$sourceSelection.checkpointSourceRunId=$null;$sourceSelection.checkpointSourceArtifactSha256=$null;$sourceSelection.expectedNativeWriteCount=0
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-manual';ownedPairs=$sourceSelection;persistenceLoad=$descriptor;fixture=$Fixture;persistenceCase=$null})
 Assert-KmcChunk7OwnedReceiptPins $set
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Two-owner native Manual bytes changed during qualification.'}
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$path;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;sourceRunId=$request.runId;ancestry=$ancestry;artifact=$artifact}
}
function Get-KmcChunk7OwnedPairsSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate){
 $manual=Get-KmcChunk7OwnedPairsManualSource $Selection $PersistenceCase $Fixture $Candidate
 if($Selection.case-cne'persistence-load'){return $manual}
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.checkpointSourceRunId 'chunk7-persistence-evidence.json';$request=$set.request
 Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $request.ownedPairs.case 'persistence-save';Assert-KmcBootstrapInventoryString $request.persistenceCase $PersistenceCase
 Assert-KmcChunk7OwnedPairsDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','riderAId','mountAId','riderBId','mountBId')){Assert-KmcBootstrapInventoryString $request.ownedPairs.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.ownedPairs.partyIds)-cne(ConvertTo-KmcChunk7Canonical $Selection.partyIds)-or(ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $manual.descriptor)){throw 'Checkpoint source substituted inherited ancestry or the immutable Manual.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.checkpointSourceRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase $PersistenceCase
 Assert-KmcChunk7PersistenceArtifact $request $set.manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $set.manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
 $path=Join-Path $set.root 'chunk7-persistence-evidence.json';$file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16MB-or$set.pins[$path]-cne$Selection.checkpointSourceArtifactSha256){throw 'Cold checkpoint differs from its exact successful source pin.'}
 $w=$artifact.observations.nativeWrite;$archive=$w.archive;$leaf=Get-KmcChunk7OwnedOutputLeaf $request $artifact.observations
 $descriptor=[pscustomobject]@{internalName=$w.nativeName;fileName=$leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=[long](Get-Item -LiteralPath $w.path).LastWriteTimeUtc.Ticks;gameId=$manual.descriptor.gameId;gameName=$manual.descriptor.gameName;area=$archive.AreaId}
 Assert-KmcChunk7OwnedPairsDescriptor ([pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId='unassigned-owned-cold';ownedPairs=$Selection;persistenceCase=$PersistenceCase;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$manual.artifactPath;artifactSha256=$manual.artifactSha256;artifactLength=$manual.artifactLength;sourceRunId=$manual.sourceRunId;ancestry=$manual.ancestry;checkpointArtifactPath=$path;checkpointArtifactSha256=$Selection.checkpointSourceArtifactSha256;checkpointArtifactLength=[long]$file.Length}
}
function Copy-KmcChunk7OwnedPairsSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'inherited two-owner profile';Assert-KmcDirectoryTreeCloneable $run 'inherited two-owner profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-owned-two-pair-campaign'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Inherited copy lacks exact current lock/profile ownership.'}
 $destination=Join-Path $run 'owned-two-pair-source.json';if(Test-Path -LiteralPath $destination){throw 'Inherited lineage receipt destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'inherited lineage';Assert-KmcNotHardLink $Source.artifactPath 'inherited lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Inherited lineage changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Inherited lineage changed during locked copy.'}
}
