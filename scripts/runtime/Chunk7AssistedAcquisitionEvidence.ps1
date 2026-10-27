Set-StrictMode -Version Latest
# Only this explicit source mode joins assisted setup to later ordinary choices.
function Test-KmcAssistedCumulativeMode($Selection){
 $null-ne$Selection-and$null-ne$Selection.PSObject.Properties['sourceMode']-and$Selection.sourceMode-is[string]-and$Selection.sourceMode-ceq'assisted-producer-and-cold'
}
function Assert-KmcAssistedCumulativePins($S,[string]$RunId){
 Assert-KmcBootstrapInventoryString $S.sourceMode 'assisted-producer-and-cold'
 if($S.assistedColdRunId-isnot[string]-or$S.assistedColdRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$S.assistedColdRunId-ceq$RunId-or$S.assistedColdRunId-ceq$S.sourceRunId-or$S.assistedColdArtifactSha256-isnot[string]-or$S.assistedColdArtifactSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted cumulative source requires distinct complete cold receipt pins.'}
 Assert-KmcBootstrapInventoryString $S.readiness.mainActorId $S.priorOwnerId
}
function Assert-KmcConnectionObject($Value,[string]$Name){if($null-eq$Value-or$Value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw ('Assisted connection requires one JSON object: '+$Name)}}
function Assert-KmcAssistedConnectionEnvelope($Request,$Artifact,[string]$RunId,[string]$Scenario,[string]$Kind){
 Assert-KmcConnectionObject $Artifact 'artifact';Assert-KmcConnectionObject $Artifact.observations 'observations'
 foreach($pair in @(@('runId',$RunId),@('scenario',$Scenario),@('evidenceKind',$Kind),@('status','PASS'))){Assert-KmcBootstrapInventoryString $Artifact.($pair[0]) $pair[1]}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $Artifact.$key $Request.$key}
 if($Artifact.errors-isnot[Array]-or$Artifact.errors.Count-ne0){throw 'Assisted connection requires two successful source artifacts.'}
}
function Assert-KmcAssistedCumulativeSource($Request,$Producer,$ColdArtifact){
 Assert-KmcChunk7CumulativeDescriptor $Request;$s=$Request.cumulativeAcquisition
 if(-not(Test-KmcAssistedCumulativeMode $s)){throw 'Assisted source projection requires its exact source mode.'}
 Assert-KmcAssistedConnectionEnvelope $Request $Producer $s.sourceRunId 'chunk7-assisted-fixture-preparation' 'chunk7-assisted-fixture-evidence'
 Assert-KmcAssistedConnectionEnvelope $Request $ColdArtifact $s.assistedColdRunId 'chunk7-assisted-cold-inventory' 'chunk7-assisted-cold-inventory-evidence'
 $p=$Producer.observations;$c=$ColdArtifact.observations;$d=$Request.persistenceLoad;$write=$p.finalManual
 foreach($obj in @($write,$p.settledWorld,$p.fixtureReadiness,$p.nativeProfileInventory,$c.selection,$c.source,$c.loadedWorld,$c.settledWorld,$c.fixtureReadiness,$c.nativeProfileInventory,$p.cleanup,$c.cleanup)){Assert-KmcConnectionObject $obj 'source member'}
 foreach($key in @('fileName','internalName','sha256','length','gameId','gameName','area')){Assert-KmcAssistedSame $write.$key $d.$key ('assisted Manual '+$key)}
 Assert-KmcBootstrapInventoryString $write.type 'Manual';Assert-KmcBootstrapInventoryString $write.operation 'None';Assert-KmcChunk7Boolean $write.autoLevelup $false
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased')){Assert-KmcChunk7Boolean $p.cleanup.$key $true}
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$s.assistedColdRunId;assistedColdInventory=$c.selection})
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $c.selection.$key $s.$key}
 foreach($pair in @(@('runId',$s.sourceRunId),@('artifactSha256',$s.sourceArtifactSha256),@('archiveSha256',$s.sourceArchiveSha256),@('archiveLeaf',$d.fileName))){Assert-KmcBootstrapInventoryString $c.source.($pair[0]) $pair[1]}
 foreach($key in @('custodySettled','sourceUnchanged','worldAdmitted','observersReleased','nativeWorkerSettled')){Assert-KmcChunk7Boolean $c.cleanup.$key $true}
 foreach($loader in @($c.loader,$c.settledLoader)){Assert-KmcConnectionObject $loader 'cold loader';Assert-KmcBootstrapInventoryString $loader.state 'LoadedAndVerified';Assert-KmcChunk7Boolean $loader.callbackObserved $true}
 Assert-KmcAssistedSame $c.sourceNativeWorld $p.settledWorld 'assisted producer world copied by cold'
 Assert-KmcAssistedSame $c.sourceFixtureReadiness $p.fixtureReadiness 'assisted producer readiness copied by cold'
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $c.$key 0 0}
 foreach($key in @('profileAdmissionChanged','ordinaryAcquisitionProven','ordinaryProgressionProven','sixPairFixtureQualified')){Assert-KmcChunk7Boolean $c.$key $false}
 $saved=$p.settledWorld;$party=$saved.party;$inventory=$c.nativeProfileInventory;$readiness=$c.fixtureReadiness
 if($party-isnot[Array]-or$party.Count-lt2-or$party.Count-gt12){throw 'Assisted full actor scope is missing or unbounded.'}
 Assert-KmcBootstrapInventoryString $saved.mainActorId $s.priorOwnerId;Assert-KmcBootstrapInventoryString $saved.gameId $d.gameId;Assert-KmcBootstrapInventoryString $saved.area $d.area
 Assert-KmcBootstrapInventoryString $readiness.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $inventory.captureStatus 'PASS'
 Assert-KmcOrdinaryInteger $c.sourceNativeMoney 0 ([long]::MaxValue)
 Assert-KmcAssistedSame $c.sourceNativeMoney $inventory.ordinaryAcquisitionPrerequisites.nativeMoney 'cold native money'
 Assert-KmcAssistedSame $c.sourceNativeMoney $p.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney 'producer native money'
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($a in $party){Assert-KmcConnectionObject $a 'producer actor';Assert-KmcOrdinaryGuid $a.id;if(-not$ids.Add($a.id)){throw 'Assisted producer repeats an actor.'};Assert-KmcConnectionObject $a.actions 'producer actor resources';if($a.classes-isnot[Array]){throw 'Assisted actor classes must be an array.'}}
 $mainProgression=$p.fixtureReadiness.mainProgression;Assert-KmcConnectionObject $mainProgression 'source main progression'
 Assert-KmcBootstrapInventoryString $mainProgression.actorId $s.priorOwnerId;Assert-KmcOrdinaryInteger $mainProgression.experience 0 ([int]::MaxValue)
 Assert-KmcAssistedSame $mainProgression $readiness.mainProgression 'separately captured source main progression'
 foreach($world in @($c.loadedWorld,$c.settledWorld)){
  foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $world.$key $d.$key};Assert-KmcBootstrapInventoryString $world.mainActorId $s.priorOwnerId;Assert-KmcBootstrapInventoryString $world.mode 'Default'
  foreach($key in @('cutscene','inCombat','nativeLoading','dialogScheduled')){Assert-KmcChunk7Boolean $world.$key $false}
  Assert-KmcAssistedSame $world.nativeMoney $c.sourceNativeMoney 'cold world native money'
  if($world.party-isnot[Array]-or$world.party.Count-ne$party.Count){throw 'Assisted cold actor scope differs.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($a in $world.party){Assert-KmcConnectionObject $a 'cold actor';Assert-KmcOrdinaryGuid $a.id;if(-not$seen.Add($a.id)-or-not$ids.Contains($a.id)){throw 'Assisted cold actor is duplicate or foreign.'};$prior=@($party|Where-Object id -CEQ $a.id)[0]
   foreach($key in @('blueprint','petId','masterId','characterLevel','classes')){Assert-KmcAssistedSame $a.$key $prior.$key ('cold actor '+$key)}
   $priorXp=if($a.id-ceq$s.priorOwnerId){$mainProgression.experience}else{$prior.experience};Assert-KmcAssistedSame $a.experience $priorXp 'cold actor experience'
  }
 }
 $owners=@($party|Where-Object{$null-ne$_.petId});$pets=@($party|Where-Object{$null-ne$_.masterId})
 if($owners.Count-ne1-or$pets.Count-ne1){throw 'Assisted source requires exactly one reciprocal native pair.'}
 $owner=$owners[0];$pet=$pets[0];Assert-KmcBootstrapInventoryString $owner.id $s.priorOwnerId;Assert-KmcBootstrapInventoryString $owner.petId $s.priorPetId;Assert-KmcBootstrapInventoryString $pet.id $s.priorPetId;Assert-KmcBootstrapInventoryString $pet.masterId $s.priorOwnerId
 Assert-KmcBootstrapInventoryString $pet.blueprint '4016c7db400ab721ff125aef9e65e202'
 if($owner.features-isnot[Array]-or$owner.features.Count-gt128){throw 'Assisted prior owner feature inventory is absent or unbounded.'}
 $feature=@($owner.features|Where-Object guid -CEQ '7db7c50677e39f09feef56f3831fc723');$rank=@($owner.features|Where-Object guid -CEQ '1670990255e4fe948a863bafd5dbda5d')
 if($feature.Count-ne1-or$rank.Count-ne1){throw 'Assisted prior owner must retain one exact Horse and rank fact.'}
 if($feature[0].source-isnot[string]-or$feature[0].source-cnotmatch'^[a-f0-9]{32}$'){throw 'Assisted prior feature source is absent.'};Assert-KmcOrdinaryInteger $rank[0].rank 1 20
 [string[]]$ordered=@($party.id);[Array]::Sort($ordered,[StringComparer]::Ordinal)
 $arranged=@();$resources=@()
 foreach($id in $ordered){$a=@($party|Where-Object id -CEQ $id)[0];$classes=@();[string[]]$classIds=@($a.classes.guid);[Array]::Sort($classIds,[StringComparer]::Ordinal)
  foreach($classId in $classIds){$row=@($a.classes|Where-Object guid -CEQ $classId);if($row.Count-ne1){throw 'Assisted actor repeats a class.'};$classes+=,[pscustomobject]@{classGuid=$row[0].guid;level=$row[0].level}}
  $actorXp=if($a.id-ceq$s.priorOwnerId){$mainProgression.experience}else{$a.experience}
  $arranged+=,[pscustomobject]@{actorId=$a.id;blueprint=$a.blueprint;experience=$actorXp;characterLevel=$a.characterLevel;classes=$classes}
  Assert-KmcExactProperties $a.actions @('move','standard','swift','prepared','reactions','reactionAllowance','initiative') 'saved assisted action state'
  $resources+=,[pscustomobject]@{actorId=$a.id;move=$a.actions.move;standard=$a.actions.standard;swift=$a.actions.swift;prepared=$a.actions.prepared;reactions=$a.actions.reactions;reactionAllowance=$a.actions.reactionAllowance;initiative=$a.actions.initiative}
 }
 $projection=[pscustomobject]@{contract='assisted-native-source-projection-v1';fixtureReadiness=$readiness;nativeProfileInventory=$inventory;
  savedOwner=[pscustomobject]@{actorId=$owner.id;petId=$pet.id;actorBlueprint=$owner.blueprint;petBlueprint=$pet.blueprint;featureGuid=$feature[0].guid;sourceGuid=$feature[0].source;rank=$rank[0].rank};
  afterSaveArrangement=[pscustomobject]@{money=$c.sourceNativeMoney;experienceRatePercent=$readiness.experienceRatePercent;actors=$arranged};resourcesAfterSave=[pscustomobject]@{actors=$resources}}
 Assert-KmcCumulativeResources $projection.resourcesAfterSave $ordered
 Assert-KmcManualDerivedSourcePins $s $projection
 $projection
}
function Assert-KmcAssistedCumulativeCopiedSource($Request,$O,$ColdArtifact,$Projection){
 $s=$Request.cumulativeAcquisition;$row=$O.assistedSource
 Assert-KmcAssistedObjectValue $row @('contract','mode','producerRunId','producerArtifactSha256','coldRunId','coldArtifactSha256','archiveSha256','setupWasAssisted','setupIsOrdinaryAcquisitionProof','coldObservations','derivedInputs') 'assisted cumulative copied source'
 foreach($pair in @(@('contract','assisted-producer-cold-cumulative-v1'),@('mode',$s.sourceMode),@('producerRunId',$s.sourceRunId),@('producerArtifactSha256',$s.sourceArtifactSha256),@('coldRunId',$s.assistedColdRunId),@('coldArtifactSha256',$s.assistedColdArtifactSha256),@('archiveSha256',$s.sourceArchiveSha256))){Assert-KmcBootstrapInventoryString $row.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $row.setupWasAssisted $true;Assert-KmcChunk7Boolean $row.setupIsOrdinaryAcquisitionProof $false
 Assert-KmcAssistedSame $row.coldObservations $ColdArtifact.observations 'copied assisted cold observations';Assert-KmcAssistedSame $row.derivedInputs $Projection 'explicit derived source inputs'
 Assert-KmcOrdinaryInteger $ColdArtifact.observations.processId 1 ([int]::MaxValue)
 if($O.processId-eq$ColdArtifact.observations.processId){throw 'Assisted cumulative acquisition must perform its own cold native load.'}
}
