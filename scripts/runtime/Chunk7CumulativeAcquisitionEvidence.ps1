Set-StrictMode -Version Latest
function Test-KmcChunk7CumulativeScenario([string]$Scenario){$Scenario-ceq'chunk7-cumulative-acquisition'}
function Get-KmcChunk7CumulativeRows {@('chunk7-cumulative-acquisition','C7-cumulative-source','C7-cumulative-native-load','C7-cumulative-eligibility','C7-cumulative-native-choices','C7-cumulative-native-autosave','C7-cumulative-native-field','C7-cumulative-cleanup','C7-cumulative-runtime','C7-cumulative-interrupted')}
function Assert-KmcChunk7CumulativeSelection($Request){
 $has=$null-ne$Request.PSObject.Properties['cumulativeAcquisition']
 if(-not(Test-KmcChunk7CumulativeScenario $Request.scenario)){if($has){throw 'Manual-derived selection is forbidden under another family.'};return}
 if(-not$has){throw 'Manual-derived acquisition requires exact explicit selection.'};$s=$Request.cumulativeAcquisition
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount','nativeWriteLeaf','readiness','existingActorId','existingLevel','existingExperience','existingRangerLevel','smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded','priorOwnerId','priorPetId') 'cumulative selection'
 if($s.case-isnot[string]-or$s.case-cnotin@('existing-ranger-horse','paid-small-ranger-horse')-or$s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Manual-derived case/source run differs.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual-derived source requires exact SHA256 pins.'}}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcBootstrapInventoryString $s.nativeWriteLeaf 'Auto_1.zks'
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount')){Assert-KmcOrdinaryInteger $s.$key 1 1}
 $p=$s.readiness;Assert-KmcExactProperties $p @('mainActorId','mainLevel','mainExperience','money','experienceRatePercent','nativeHireCost','defaultHireExperience','partyCharacterIds') 'native readiness pins'
 Assert-KmcOrdinaryGuid $p.mainActorId;Assert-KmcOrdinaryInteger $p.mainLevel 1 20;Assert-KmcOrdinaryInteger $p.mainExperience 0 ([int]::MaxValue)
 Assert-KmcOrdinaryInteger $p.money 0;Assert-KmcOrdinaryInteger $p.experienceRatePercent 100 100;Assert-KmcOrdinaryInteger $p.nativeHireCost 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $p.defaultHireExperience 0 ([int]::MaxValue)
 if($p.partyCharacterIds-isnot[Array]-or$p.partyCharacterIds.Count-lt1-or$p.partyCharacterIds.Count-gt6){throw 'Readiness needs bounded native field characters.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$last=''
 foreach($id in $p.partyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)-or[StringComparer]::Ordinal.Compare($last,$id)-ge0){throw 'Readiness party IDs must be unique and ordinally sorted.'};$last=$id}
 if(-not$ids.Contains($p.mainActorId)){throw 'Readiness main character is absent from the native party.'}
 if($s.case-ceq'existing-ranger-horse'){
  Assert-KmcOrdinaryGuid $s.existingActorId;if(-not$ids.Contains($s.existingActorId)){throw 'Existing actor is absent from observed field party.'}
  Assert-KmcOrdinaryInteger $s.existingLevel 1 19;Assert-KmcOrdinaryInteger $s.existingExperience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $s.existingRangerLevel 0 3
  if($s.existingRangerLevel-gt$s.existingLevel){throw 'Existing Ranger ranks exceed character level.'}
  foreach($key in @('smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded')){if($null-ne$s.$key){throw 'Existing actor selection cannot alter its body.'}}
 }else{
  foreach($key in @('existingActorId','existingLevel','existingExperience','existingRangerLevel')){if($null-ne$s.$key){throw 'Paid hire cannot borrow an existing actor.'}}
  foreach($key in @('smallRaceGuid','smallPresetGuid')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Paid body blueprint must be exact.'}}
  if($s.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($s.skeletonName)-or$s.skeletonName.Length-gt160-or$s.skeletonName-match'[\x00-\x1f\x7f]'-or$s.gender-isnot[string]-or$s.gender-cnotin@('Male','Female')-or$s.leftHanded-isnot[bool]){throw 'Paid body selection is malformed.'}
 }
 Assert-KmcOrdinaryGuid $s.priorOwnerId;Assert-KmcOrdinaryGuid $s.priorPetId
 if($s.priorOwnerId-ceq$s.priorPetId-or-not$ids.Contains($s.priorOwnerId)-or$ids.Contains($s.priorPetId)-or$s.existingActorId-ceq$s.priorOwnerId-or$s.existingActorId-ceq$s.priorPetId){throw 'Cumulative acquisition must preserve a distinct prior owner/pet and select another target.'}

}

function Assert-KmcChunk7CumulativeDescriptor($Request){
 Assert-KmcChunk7CumulativeSelection $Request
 if(-not(Test-KmcChunk7CumulativeScenario $Request.scenario)){throw 'Cumulative descriptor belongs to its exact family only.'}
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'cumulative immutable descriptor'
 Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_ACQUIRED_FIELD';Assert-KmcBootstrapInventoryString $d.sha256 $Request.cumulativeAcquisition.sourceArchiveSha256
 if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_ACQUIRED_FIELD\.zks$'){throw 'Cumulative source must retain its actual native acquired Manual number.'}
 $number=0;if(-not[int]::TryParse($Matches[1],[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ACQUIRED_FIELD.zks')){throw 'Cumulative native Manual number is malformed or out of range.'}
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Cumulative input must preserve its separately minted campaign.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcCumulativePriorSource($Selection,$O){
 Assert-KmcBootstrapInventoryString $O.selection.case 'preserve-acquired-field';Assert-KmcBootstrapInventoryString $O.selection.actorId $Selection.priorOwnerId;Assert-KmcBootstrapInventoryString $O.selection.petId $Selection.priorPetId
 $p=$O.savedOwner;Assert-KmcBootstrapInventoryString $p.actorId $Selection.priorOwnerId;Assert-KmcBootstrapInventoryString $p.petId $Selection.priorPetId
 foreach($key in @('reciprocal','actorInGame','petInGame','actorInState','petInState','actorAgentReady','petAgentReady','actorCommandsEmpty','petCommandsEmpty')){Assert-KmcChunk7Boolean $p.$key $true}
 Assert-KmcBootstrapInventoryString $p.petBlueprint '4016c7db400ab721ff125aef9e65e202';Assert-KmcBootstrapInventoryString $p.featureGuid '7db7c50677e39f09feef56f3831fc723'
 foreach($key in @('actorBlueprint','sourceGuid')){if($p.$key-isnot[string]-or$p.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Prior owner blueprint/source must be exact.'}};Assert-KmcOrdinaryInteger $p.rank 1 20
 if($O.nativeProfileInventory.party-isnot[Array]-or$O.savedWorld.party-isnot[Array]-or$O.nativeProfileInventory.party.Count-ne$O.savedWorld.party.Count-or$O.savedWorld.party.Count-lt2-or$O.savedWorld.party.Count-gt24){throw 'Prior native graph is absent or unbounded.'}
 $owners=@();$pets=@();$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($a in $O.nativeProfileInventory.party){Assert-KmcOrdinaryGuid $a.id;if(-not$seen.Add($a.id)){throw 'Prior graph repeats an actor.'};$w=@($O.savedWorld.party|Where-Object id -CEQ $a.id);if($w.Count-ne1){throw 'Prior graph has foreign actor.'};Assert-KmcBootstrapInventoryString $a.blueprintGuid $w[0].blueprint
  if($null-ne$a.petId){Assert-KmcOrdinaryGuid $a.petId;$owners+=,$a};if($null-ne$a.masterId){Assert-KmcOrdinaryGuid $a.masterId;$pets+=,$a}
 }
 if($owners.Count-ne1-or$pets.Count-ne1-or$owners[0].id-cne$Selection.priorOwnerId-or$owners[0].petId-cne$Selection.priorPetId-or$pets[0].id-cne$Selection.priorPetId-or$pets[0].masterId-cne$Selection.priorOwnerId){throw 'Exactly one reciprocal prior owner/pet is admitted; conflicting or cumulative input refuses.'}
 Assert-KmcBootstrapInventoryString $owners[0].blueprintGuid $p.actorBlueprint;Assert-KmcBootstrapInventoryString $pets[0].blueprintGuid $p.petBlueprint
 if($p.partyCharacters-isnot[Array]-or@($p.partyCharacters|Where-Object{$_-ceq$Selection.priorOwnerId}).Count-ne1-or$p.remoteCompanions-isnot[Array]-or$Selection.priorOwnerId-cin$p.remoteCompanions){throw 'Prior owner is not the actual settled field character.'}
 if((ConvertTo-KmcChunk7Canonical $p.progression)-cne(ConvertTo-KmcChunk7Canonical $O.afterSaveArrangement)){throw 'Prior saved progression disagrees with native Manual output.'}
 Assert-KmcCumulativeResources $O.resourcesAfterSave @($O.savedWorld.party.id)
}
function Assert-KmcCumulativeResources($Row,[string[]]$ActorIds){
 Assert-KmcExactProperties $Row @('actors') 'cumulative resources'
 if($Row.actors-isnot[Array]-or$Row.actors.Count-ne$ActorIds.Count){throw 'Cumulative native resource roster changed.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$last=''
 foreach($a in $Row.actors){Assert-KmcExactProperties $a @('actorId','move','standard','swift','prepared','reactions','reactionAllowance','initiative') 'cumulative actor resource'
  Assert-KmcOrdinaryGuid $a.actorId;if(-not$seen.Add($a.actorId)-or$a.actorId-cnotin$ActorIds-or[StringComparer]::Ordinal.Compare($last,$a.actorId)-ge0){throw 'Resource actor is foreign, repeated or unordered.'};$last=$a.actorId
  foreach($key in @('move','standard','swift','initiative')){if(-not(Test-KmcFiniteJsonNumber $a.$key)){throw 'Native resource must be a finite numeric token.'}}
  if($a.prepared-isnot[bool]){throw 'Native preparation must be an actual boolean.'}
  foreach($key in @('reactions','reactionAllowance')){Assert-KmcOrdinaryInteger $a.$key ([int]::MinValue) ([int]::MaxValue)}
 }
}
function Assert-KmcCumulativeRetention($Request,$O,$Source){
 $s=$Request.cumulativeAcquisition;$c=$O.cumulative;$prior=$Source.observations.savedOwner
 Assert-KmcExactProperties $c @('contract','priorOwnerCount','maximumOwnerCount','sourceOwner','loadedPriorOwner','resourcesLoaded','unrelatedResourcesBefore','relationshipsBefore','progressionBefore','noAcquisitionGrants','generalizedCumulativeAdmission','settledPriorOwner','unrelatedResourcesAfter','relationshipsAfter','progressionAfter','finalOwnerCount','capturedBeforeObserverRelease') 'cumulative retention'
 Assert-KmcBootstrapInventoryString $c.contract 'native-one-owner-to-two-v1';Assert-KmcOrdinaryInteger $c.priorOwnerCount 1 1;Assert-KmcOrdinaryInteger $c.maximumOwnerCount 2 2;Assert-KmcOrdinaryInteger $c.finalOwnerCount 2 2
 foreach($key in @('noAcquisitionGrants','capturedBeforeObserverRelease')){Assert-KmcChunk7Boolean $c.$key $true};Assert-KmcChunk7Boolean $c.generalizedCumulativeAdmission $false
 if((ConvertTo-KmcChunk7Canonical $c.sourceOwner)-cne(ConvertTo-KmcChunk7Canonical $prior)){throw 'Cumulative source owner receipt changed.'}
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $O.nativeLoad.identities.ids){[void]$labels.Add([int]$id)}
 $used=[Collections.Generic.HashSet[int]]::new();foreach($key in @('game','saveManager','player','state','areaState','mainState')){[void]$used.Add([int]$O.loadedWorld.$key)}
 foreach($a in $O.loadedWorld.party){foreach($key in @('object','descriptor','view')){[void]$used.Add([int]$a.$key)}}
 $l=$c.loadedPriorOwner;Assert-KmcExactProperties $l @('actorId','petId','actorObject','actorDescriptor','actorView','petObject','petDescriptor','petView','featureObject','sourceObject','rankObject','actorBlueprint','petBlueprint','featureGuid','sourceGuid','rank','reciprocal') 'loaded prior owner'
 foreach($key in @('actorId','petId','actorBlueprint','petBlueprint','featureGuid','sourceGuid')){Assert-KmcBootstrapInventoryString $l.$key $prior.$key};Assert-KmcOrdinaryInteger $l.rank $prior.rank $prior.rank;Assert-KmcChunk7Boolean $l.reciprocal $true
 foreach($role in @('actor','pet')){$nativeId=if($role-ceq'actor'){$s.priorOwnerId}else{$s.priorPetId};$w=@($O.loadedWorld.party|Where-Object id -CEQ $nativeId);if($w.Count-ne1){throw 'Prior owner/pet is missing from admitted world.'}
  foreach($pair in @(@('Object','object'),@('Descriptor','descriptor'),@('View','view'))){$key=$role+$pair[0];Assert-KmcManualSeedObjectIdentity $l.$key;Assert-KmcOrdinaryInteger $l.$key $w[0].($pair[1]) $w[0].($pair[1]);if(-not$labels.Contains([int]$l.$key)){throw 'Prior object label is not retained.'}}
 }
 foreach($key in @('featureObject','sourceObject','rankObject')){Assert-KmcManualSeedObjectIdentity $l.$key;if(-not$labels.Contains([int]$l.$key)-or-not$used.Add([int]$l.$key)){throw 'Prior feature/source/rank identity is foreign or aliases another native role.'}}
 if((ConvertTo-KmcChunk7Canonical $l)-cne(ConvertTo-KmcChunk7Canonical $c.settledPriorOwner)){throw 'Prior exact owner/pet feature, source, rank or view changed.'}
 if($O.field.actorId-cin@($s.priorOwnerId,$s.priorPetId)-or$O.field.petId-cin@($s.priorOwnerId,$s.priorPetId)){throw 'Second native pair reuses prior owner/pet.'}
 foreach($key in @('actorObject','actorDescriptor','actorView','petObject','petDescriptor','petView')){if($O.field.$key-cin@($l.actorObject,$l.actorDescriptor,$l.actorView,$l.petObject,$l.petDescriptor,$l.petView,$l.featureObject,$l.sourceObject,$l.rankObject)){throw 'New native pair aliases a retained prior role.'}}
 $original=@($O.loadedWorld.party.id);$unrelated=@($original|Where-Object{$_-cne$s.existingActorId})
 Assert-KmcCumulativeResources $c.resourcesLoaded $original
 if((ConvertTo-KmcChunk7Canonical $c.resourcesLoaded)-cne(ConvertTo-KmcChunk7Canonical $Source.observations.resourcesAfterSave)){throw 'Cold cumulative resources differ from saved source.'}
 foreach($name in @('unrelatedResourcesBefore','unrelatedResourcesAfter')){Assert-KmcCumulativeResources $c.$name $unrelated}
 $expectedResources=[pscustomobject]@{actors=@($c.resourcesLoaded.actors|Where-Object{$_.actorId-cin$unrelated})}
 foreach($name in @('unrelatedResourcesBefore','unrelatedResourcesAfter')){if((ConvertTo-KmcChunk7Canonical $c.$name)-cne(ConvertTo-KmcChunk7Canonical $expectedResources)){throw 'Unrelated action/reaction/preparation state changed.'}}
 $expectedRelations=[pscustomobject]@{actors=@($unrelated|Sort-Object -CaseSensitive|ForEach-Object{$id=$_;$native=@($Source.observations.nativeProfileInventory.party|Where-Object id -CEQ $id);if($native.Count-ne1){throw 'Source relation is missing.'};[pscustomobject]@{actorId=$id;petId=$native[0].petId;masterId=$native[0].masterId}})}
 foreach($name in @('relationshipsBefore','relationshipsAfter')){if((ConvertTo-KmcChunk7Canonical $c.$name)-cne(ConvertTo-KmcChunk7Canonical $expectedRelations)){throw 'Unrelated native relationship changed.'}}
 $expectedProgression=[pscustomobject]@{actors=@($Source.observations.afterSaveArrangement.actors|Where-Object{$_.actorId-cin$unrelated})}
 foreach($name in @('progressionBefore','progressionAfter')){if((ConvertTo-KmcChunk7Canonical $c.$name)-cne(ConvertTo-KmcChunk7Canonical $expectedProgression)){throw 'Unrelated native progression changed.'}}
}
function Assert-KmcChunk7CumulativeEvidence($Request,$Artifact,$SourceArtifact,[string]$Status='PASS'){
 Assert-KmcChunk7CumulativeDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-cumulative-acquisition-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS cumulative result requires its PASS native evidence.'};if($Artifact.status-cne'PASS'){return}
 Assert-KmcBootstrapInventoryString $SourceArtifact.scenario 'chunk7-acquired-manual-derivative';Assert-KmcBootstrapInventoryString $SourceArtifact.evidenceKind 'chunk7-acquired-manual-evidence'
 Assert-KmcCumulativePriorSource $Request.cumulativeAcquisition $SourceArtifact.observations
 Assert-KmcNativeAcquisitionProof $Request $Artifact $SourceArtifact $Request.cumulativeAcquisition -Cumulative
 Assert-KmcCumulativeRetention $Request $Artifact.observations $SourceArtifact
}
function Assert-KmcChunk7CumulativeArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-cumulative-acquisition-evidence'-or$_.relativePath-ceq'chunk7-cumulative-acquisition-evidence.json'})
 if(-not(Test-KmcChunk7CumulativeScenario $Request.scenario)){if($records.Count){throw 'Foreign request supplied cumulative evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-cumulative-acquisition-evidence.json' 'chunk7-cumulative-acquisition-evidence';$source=$null
 if($artifact.status-ceq'PASS'){
  $path=Join-Path (Join-Path (Split-Path $Request.evidenceRoot -Parent) $Request.cumulativeAcquisition.sourceRunId) 'chunk7-acquired-manual-evidence.json'
  Assert-KmcNotReparsePoint $path 'cumulative Manual lineage';Assert-KmcNotHardLink $path 'cumulative Manual lineage';$file=Get-Item -LiteralPath $path
  if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $path)-cne$Request.cumulativeAcquisition.sourceArtifactSha256){throw 'Cumulative Manual lineage is absent or changed.'}
  Assert-KmcManifestJsonMembersUnique $path 'cumulative Manual lineage';$source=Read-KmcJson $path
  if((Get-KmcSha256 $path)-cne$Request.cumulativeAcquisition.sourceArtifactSha256){throw 'Cumulative Manual lineage changed while reading.'}
 }
 Assert-KmcChunk7CumulativeEvidence $Request $artifact $source $Status
}
