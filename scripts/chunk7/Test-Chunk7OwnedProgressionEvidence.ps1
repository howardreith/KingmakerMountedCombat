[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$script:passes=0
foreach($case in Get-KmcChunk7OwnedProgressionCases){Good ('owned exact reader '+$case){$f=New-ProgressionFixture $case;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}}
Good 'post-process old input remains Manual300' {$f=New-ProgressionFixture;if((Get-KmcChunk7ProgressionInputLeaf $f.request)-cne'Manual_300_KMC_C7_WORLD.zks'){throw 'Historical leaf changed.'}}
Good 'post-process owned input derives exact native Manual leaf' {$f=New-ProgressionFixture 'owned-horse-native-level';if((Get-KmcChunk7ProgressionInputLeaf $f.request)-cne'Manual_404_KMC_C7_WORLD.zks'){throw 'Owned native leaf lost.'}}
Bad 'post-process foreign number remains refused' {$f=New-ProgressionFixture 'owned-horse-native-level';$f.request.persistenceLoad.fileName='Manual_0404_KMC_C7_WORLD.zks';[void](Get-KmcChunk7ProgressionInputLeaf $f.request)}
$mutations=[ordered]@{
 'missing nested source'={param($f)$f.request.progression.PSObject.Properties.Remove('ownedSource')}
 'outer dispatch source'={param($f)$f.request|Add-Member NoteProperty ownedPairs $f.request.progression.ownedSource}
 'nested source save case'={param($f)$f.request.progression.ownedSource.case='persistence-save'}
 'nested write entitlement'={param($f)$f.request.progression.ownedSource.expectedNativeWriteCount=1}
 'second nested native load'={param($f)$f.request.progression.ownedSource.expectedNativeLoadCount=2}
 'matching checkpoint array'={param($f)$f.request.progression.ownedSource.checkpointSourceRunId=@($f.request.progression.sourceRunId)}
 'matching owned case array'={param($f)$f.request.progression.case=@($f.request.progression.case)}
 'control Mammoth profile'={param($f)$f.artifact.observations.loadedArchive.Pairs[0].ProfileId='medium-humanoid-mammoth-v1'}
 'foreign source native campaign'={param($f)$f.request.persistenceLoad.gameId=$f.request.fixture.working.gameId}
 'unmounted source substituted'={param($f)$f.request.progression.sourceArchiveSha256=$f.request.progression.ownedSource.sourceArchiveSha256;$f.request.persistenceLoad.sha256=$f.request.progression.sourceArchiveSha256}
 'target is native main'={param($f)$f.artifact.observations.source.inventoryWorld.mainActorId=$f.request.progression.actorId}
 'Small target body'={param($f)$f.artifact.observations.source.inventoryActor.originalSize='Small'}
 'unearned source XP'={param($f)$f.artifact.observations.source.inventoryActor.nativeLevelUpReadiness.experience++}
 'source field actor missing'={param($f)$f.artifact.observations.source.inventoryWorld.party=@($f.artifact.observations.source.inventoryWorld.party|Select-Object -Skip 1)}
 'loaded actor object aliases control'={param($f)$f.artifact.observations.ownedRosterBefore.party[0].object=11}
 'matching loaded contract array'={param($f)$f.artifact.observations.ownedRosterBefore.contract=@('owned-progression-roster-v1')}
 'matching loaded actor blueprint array'={param($f)$a=$f.artifact.observations.source.inventoryWorld.party[0];$a.blueprint=@($a.blueprint)}
 'native field pet counted as character'={param($f)foreach($x in @($f.artifact.observations.ownedRosterBefore,$f.artifact.observations.ownedRosterAfter)){$x.originalCharacterIds+=@($f.request.progression.petId);$x.characterIds+=@($f.request.progression.petId)}}
 'remote unrelated captured actor'={param($f)$f.artifact.observations.ownedRosterAfter.remoteCapturedIds=@($f.artifact.observations.ownedRosterAfter.originalCharacterIds[0])}
 'unobserved target removal'={param($f)$f.artifact.observations.ownedRosterAfter.party=@($f.artifact.observations.ownedRosterAfter.party|Where-Object{$_.id-cne$f.request.progression.actorId})}
 'unrelated actor descriptor replaced'={param($f)$f.artifact.observations.ownedRosterAfter.party[0].descriptor++}
 'control fact replaced'={param($f)$f.artifact.observations.ownedRosterAfter.controlFeature++}
 'control generation replaced'={param($f)$f.artifact.observations.ownedRosterAfter.controlGeneration++}
 'native rest falsely immutable'={param($f)$f.artifact.observations.ownedRosterAfter.resourceEqualityAcrossNativeRestClaimed=$true}
 'text original pet boolean'={param($f)$f.artifact.observations.ownedRosterAfter.originalPet.destroyed='false'}
 'old pet object replaced'={param($f)$f.artifact.observations.ownedRosterAfter.originalPet.object++}
 'unexpected extra roster proof field'={param($f)$f.artifact.observations.ownedRosterAfter|Add-Member NoteProperty synthesizedParty $true}
}
foreach($item in $mutations.GetEnumerator()){Bad $item.Key {$f=New-ProgressionFixture 'owned-horse-native-level';&$item.Value $f;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}}
foreach($case in @('owned-horse-native-retrain-horse','owned-horse-native-retrain-mammoth')){
 foreach($mutation in @('pending-destroy','live-old-registration','retained-old-view','retained-old-master','unobserved-target-view','foreign-new-pet','unobserved-transition','pet-label-array')){
  Bad ($case+' '+$mutation){$f=New-ProgressionFixture $case;$o=$f.artifact.observations;$last=$o.ownedRosterAfter;switch($mutation){
   'pending-destroy'{$last.originalPet.destroyed=$false;$last.originalPet.isInState=$true}
   'live-old-registration'{$last.originalPet.presentInNativeState=$true}
   'retained-old-view'{$last.originalPet.view=204}
   'retained-old-master'{$last.originalPet.masterId=$f.request.progression.actorId}
   'unobserved-target-view'{@($last.party|Where-Object{$_.id-ceq$f.request.progression.actorId})[0].view++}
   'foreign-new-pet'{@($last.party|Where-Object{$_.id-ceq$last.currentPetId})[0].object++}
   'unobserved-transition'{$last.nativeTargetTransitionObserved=$false}
   'pet-label-array'{$last.originalPet.object=@(4)}
  };Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
 }
 Good ($case+' native disposed pet retains raw historical IsInGame flag'){$f=New-ProgressionFixture $case;$f.artifact.observations.ownedRosterAfter.originalPet.isInGame=$true;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
}
Good 'dismissal does not invent respec old-pet destruction' {$f=New-ProgressionFixture 'owned-horse-native-dismiss';$old=$f.artifact.observations.ownedRosterAfter.originalPet;if($old.destroyed-or-not$old.isInState-or$null-eq$old.view){throw 'Fixture lost independent native dismissal shape.'};Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
Good 'signed nonzero native identity labels remain admitted'{
 $f=New-ProgressionFixture 'owned-horse-native-retrain-horse'
 $keys=@('object','descriptor','view','actorObject','descriptorObject','viewObject','petObject','petDescriptorObject','petViewObject','riderObject','mountObject','controlFeature','controlRankFact','controlSource','sourceObject','ownerDescriptorObject','playerObject','nativeControllerObject','saveObject','previewObject')
 function Relabel($v){if($null-eq$v-or$v-is[string]-or$v.GetType().IsValueType){return};if($v-is[Array]){foreach($x in $v){Relabel $x};return};foreach($p in $v.PSObject.Properties){if($p.Name-cin$keys-and$null-ne$p.Value-and$p.Value-isnot[bool]){$p.Value=-[math]::Abs([int]$p.Value)}else{Relabel $p.Value}}}
 Relabel $f.artifact.observations;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; owned progression JSON-only evidence, no native or archive invocation')
