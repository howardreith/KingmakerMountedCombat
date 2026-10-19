[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/manticore-fixture-reader-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Manticore fixture checks require a fresh output directory.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
$fixtures=([IO.File]::ReadAllText((Join-Path $FixtureDirectory 'OwnedPairsEvidenceFixtures.json'))|ConvertFrom-Json).fixtures
function Import-Functions([string]$Path,[string[]]$Names){
 $tokens=$null;$parse=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$parse);if($parse.Count){throw 'Fixture constructor did not parse.'}
 foreach($name in $Names){$n=@($ast.FindAll({param($x)$x-is[Management.Automation.Language.FunctionDefinitionAst]-and$x.Name-ceq$name},$true));if($n.Count-ne1){throw ('Missing or ambiguous fixture constructor '+$name)};. ([scriptblock]::Create($n[0].Extent.Text))}
}
. Import-Functions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1') @('Copy-Value','New-KmcOwnerSeedCheckpointSelection','Set-KmcOwnerSeedDerivedLabels','New-KmcOwnerSeedCheckpointSaveFixture')
. Import-Functions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedAreaAutosaveEvidence.ps1') @('Clone-Area','Put-Area','New-AreaCase')
. Import-Functions (Join-Path $PSScriptRoot 'Test-Chunk7OwnerSeedAreaEvidence.ps1') @('New-SeedArea')
function Copy-Value($v){ConvertTo-KmcChunk7Canonical $v|ConvertFrom-Json}
$script:areaLab=Join-Path $OutputDirectory 'virtual-lab';function Get-KmcLabRoot{$script:areaLab}
$sourceArea='b10e86516b6437d45a629931c30936fe';$destination='ead426a6c23d39548a670ee515d77df4';$entry='fd225a93828d26449803a4847947f8c1'
function New-ManticoreFacts([string[]]$Roles,[int]$Mounted){
 $guids=@('f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49');$rows=@()
 foreach($id in $Roles){for($kind=1;$kind-le5;$kind++){
  $count=if($id-cin@($Roles[0],$Roles[2])-and$(if($Mounted-eq2){$kind-in@(2,3,4)}else{$kind-eq1})){1}else{0}
  $facts=@();if($count){$facts=@([pscustomobject]@{active=$true;factBlueprintExact=$true;dataPresent=$true;dataBlueprintExact=$true;casterExact=$true;nativeLookupExact=$true})}
  $rows+=[pscustomobject]@{actorId=$id;kind=$kind;blueprintGuid=$guids[$kind-1];totalFacts=$count;activeFacts=$count;facts=$facts}
 }}
 [pscustomobject]@{contract='owner-seed-control-facts-v1';rows=$rows;totalFacts=$(if($Mounted-eq2){6}else{2});activeFacts=$(if($Mounted-eq2){6}else{2})}
}
function New-ManticoreFixture{
 $f=New-SeedArea;$r=$f.request;$o=$f.artifact.observations
 $r.persistenceCase='owner-seed-manticore-fixture-manual';$o.checkpoint=$r.persistenceCase
 $r.ownedPairs.PSObject.Properties.Remove('sourceArea');$r.PSObject.Properties.Remove('persistenceAreaTarget')
 $r.ownedPairs.sourceArchiveSha256='b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003';$r.persistenceLoad.sha256=$r.ownedPairs.sourceArchiveSha256
 $oldSource=$r.persistenceLoad.fileName;$r.persistenceLoad.fileName='Manual_304_KMC_C7_SEED_WORLD.zks'
 $oldOutput=Split-Path -Leaf $o.nativeWrite.path;$leaf='Manual_305_KMC_C7_AREA_FIXTURE.zks'
 Set-KmcOwnerSeedDerivedLabels $o $r.ownedPairs;$o.ownerSeedCheckpointSource.manualSha256=$r.ownedPairs.sourceArchiveSha256;$o.ownerSeedCheckpointSource.checkpoint=$r.persistenceCase
 $o.nativeWrite.path=Join-Path (Split-Path -Parent $o.nativeWrite.path) $leaf;$o.nativeWrite.nativeType='Manual';$o.nativeWrite.nativeName='KMC_C7_AREA_FIXTURE';$o.nativeWrite.nativeCallback=$true
 $o.nativeWrite.archive.AreaId='314e8c3b15b896a4f847c85f9250f02b';$o.nativeWrite.archive.GameTimeTicks=55000000
 $o.mountedStart.loadedArchive.GameTimeTicks=10000000
 $o.ownedOutputAdmission.number=305;$o.ownedOutputAdmission.leaf=$leaf;$o.ownedOutputCompletion.callbackCount=1;$o.ownedOutputCompletion.PSObject.Properties.Remove('areaSaveIdentity')
 foreach($a in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$a.number=305;$a.leaf=$leaf;$a.nativeName='KMC_C7_AREA_FIXTURE';$a.nativeType='Manual'}
 foreach($d in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){
  if($d.fileName-ceq$oldSource){$d.fileName=$r.persistenceLoad.fileName;$d.area=$sourceArea}
  elseif($d.fileName-ceq$oldOutput){$d.fileName=$leaf;$d.nativeName='KMC_C7_AREA_FIXTURE';$d.nativeType='Manual';$d.area=$o.nativeWrite.archive.AreaId}
 }
 $baseNative=Copy-Value $o.areaTransfer.before
 foreach($key in @('area','sameWorld','suspensions','resumes','pending','activeOwners','views','movementAuthorities','ownedField','ownedWorld','nativeSaveBoundary')){$baseNative.PSObject.Properties.Remove($key)}
 $baseNative.controls.ManagedHotbarSlotCount=4
 $field=Copy-Value $o.areaTransfer.before.ownedField;$field.custody=Copy-Value $o.inheritedFixture;$field.custody.phase='replay-field'
 $o.PSObject.Properties.Remove('areaTransfer')
 $oldOwners=Copy-Value $baseNative.owners
 $fresh=@('a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1','b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2')
 for($i=0;$i-lt2;$i++){$o.nativeWrite.archive.Pairs[$i].PairId=$fresh[$i];$o.nativeWrite.archive.Pairs[$i].Generation=3+$i}
 $newOwners=@($o.nativeWrite.archive.Pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id;state='Active'}})
 $roles=Get-KmcChunk7OwnedPairIds $r
 $hotbars=@();for($i=0;$i-lt$r.ownedPairs.partyIds.Count;$i++){
  $id=$r.ownedPairs.partyIds[$i];$elements=@(0..139|ForEach-Object{12000+$i*200+$_});$managed=@(0..139|ForEach-Object{$false})
  foreach($s in @($o.mountedStart.loadedArchive.Slots|Where-Object ActorId -CEQ $id)){$managed[$s.Index]=$true}
  $hotbars+=[pscustomobject]@{actorId=$id;owner=10000+$i;collection=11000+$i;elements=$elements;managed=$managed}
 }
 $worlds=@();$labels=@('source-mounted','source-unmounted','destination-unmounted','destination-remounted','native-manual-complete')
 for($i=0;$i-lt5;$i++){
  $native=Copy-Value $baseNative;$native.label=$labels[$i];$native.gameTicks=@(20000000,30000000,40000000,50000000,60000000)[$i]
  $native.owners=if($i-eq0){Copy-Value $oldOwners}elseif($i-in@(1,2)){,@()}else{Copy-Value $newOwners}
  $native.semanticRestores=4;$native.presentationRestores=2;$native.nativePostLoads=4;$native.snapshots=if($i-eq4){1}else{0};$native.nativeCastRequests=@(0,2,2,4,4)[$i]
  $native.controls|Add-Member NativeCastRequestCount $native.nativeCastRequests -Force
  $native.controls|Add-Member ExactFactCount $(if($i-in@(1,2)){2}else{6}) -Force
  $currentField=Copy-Value $field;$currentField.area=if($i-lt2){$sourceArea}else{$o.nativeWrite.archive.AreaId}
  $currentHotbars=Copy-Value $hotbars
  if($i-ge3){foreach($h in $currentHotbars){for($j=0;$j-lt$h.elements.Count;$j++){if($h.managed[$j]){$h.elements[$j]+=2000}}}}
  $worlds+=[pscustomobject]@{label=$labels[$i];area=$currentField.area;game=1;player=2;state=4;areaState=$(if($i-lt2){5}else{9});mainState=$(if($i-lt2){6}else{10});worldScope=$(if($i-lt2){7}else{8});mode='Default';capital=$false;directlyControllable=(Copy-Value $r.ownedPairs.partyIds);field=$currentField;native=$native;activeSlots=$(if($i-in@(1,2)){,@()}else{Copy-Value $o.nativeWrite.archive.Slots});controlFacts=(New-ManticoreFacts $roles $(if($i-in@(1,2)){0}else{2}));authority=(Copy-Value $(if($i-eq4){$o.ownedOutputCompletion.authority}else{$o.ownedOutputAdmission.authority}));hotbars=$currentHotbars}
 }
 $before=Copy-Value $worlds[3].native;$before.label='before-native-save'
 $complete=Copy-Value $worlds[4].native;$complete.label='native-write-complete'
 $continuation=Copy-Value $complete;$continuation.label='continuation-complete';$continuation.gameTicks=70000000
 $o.snapshots=@($before,$complete,$continuation)
 $o|Add-Member saveRequested ([pscustomobject]@{nativeType='Manual';nativeName='KMC_C7_AREA_FIXTURE';gameTicks=50000000;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true}) -Force
 $inputs=@();$names=@('manticore-dismount-first-pair','manticore-dismount-second-pair','manticore-mount-first-pair','manticore-mount-second-pair')
 $phases=@($oldOwners,@($oldOwners[1]),@(),@($newOwners[0]),$newOwners)
 for($i=0;$i-lt4;$i++){
  $b=Copy-Value $baseNative;$a=Copy-Value $baseNative;$b.nativeCastRequests=$i;$a.nativeCastRequests=$i+1;$b.snapshots=0;$a.snapshots=0;$b.owners=Copy-Value $phases[$i];$a.owners=Copy-Value $phases[$i+1]
  $inputs+=[pscustomobject]@{label=$names[$i];before=$b;after=$a;casterId=$roles[@(0,2,0,2)[$i]];targetId=$roles[@(0,2,1,3)[$i]];abilityGuid=$(if($i-lt2){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'});clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;generationAtInit=1;control=$(if($i-lt2){'Dismount'}else{'Mount'});selectionAfterInput=$roles[@(2,0,2,0)[$i]];terminal=$true}
 }
 $o.inputs=$inputs
 $prerequisite=[pscustomobject]@{contract='owner-seed-manticore-entry-v1';sourceArea=$sourceArea;sourceSha256=$r.persistenceLoad.sha256;locationGuid='3cf6c925d3fca1943b538db776f90b20';entryGuid='dfbc24234b2f33f4f888bb6f5f838c6b';destinationArea=$o.nativeWrite.archive.AreaId;destinationIsCapital=$false;seen=$true;revealed=$true;closed=$false;explored=$false;locationCountBefore=611;locationCountAfter=611;existingLocationOnly=$true;actualEntranceExact=$true;navigationAssisted=$true;ordinaryTravelProof=$false;nativeMode='None';transferRequests=0;saveRequests=0}
 $registry=[Collections.Generic.HashSet[int]]::new()
 foreach($w in $worlds){
  foreach($key in @('game','player','state','areaState','mainState','worldScope')){[void]$registry.Add([int]$w.$key)}
  foreach($a in $w.field.actors){foreach($key in @('object','descriptor','viewObject')){[void]$registry.Add([int]$a.$key)}}
  foreach($id in $w.field.custody.identities.ids){[void]$registry.Add([int]$id)}
  foreach($h in $w.hotbars){[void]$registry.Add([int]$h.owner);[void]$registry.Add([int]$h.collection);foreach($id in $h.elements){[void]$registry.Add([int]$id)}}
 }
 $p=[pscustomobject]@{contract='owner-seed-manticore-fixture-v1';checkpoint=$r.persistenceCase;prerequisite=$prerequisite;transferRequests=1;nativeMode='None';navigationAssisted=$true;ordinaryTravelProof=$false;nativeAreaCallbackExpected=$false;explicitSaveGameDispatches=1;source=$worlds[0];originalSlots=(Copy-Value $o.nativeWrite.archive.Slots);sourceUnmounted=$worlds[1];dispatchReturned=$true;destinationUnmounted=$worlds[2];loadingTicks=2;remounted=$worlds[3];reboundSlots=(Copy-Value $o.nativeWrite.archive.Slots);controlRebindings=4;afterSave=$worlds[4];completedArchiveSha256=$o.nativeWrite.sha256;identityRetention=[pscustomobject]@{capacity=4096;ids=@($registry|Sort-Object);faults=0;released=$true;retainedAfterRelease=0}}
 $o|Add-Member manticoreFixturePreparation $p -Force
 Copy-Value $f
}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false){$ok=$false;$errorText=$null;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Read-Manticore($f){try{Assert-KmcChunk7OwnedPairsDescriptor $f.request;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}catch{throw ($_.Exception.Message+' at '+$_.ScriptStackTrace)}}
$baseline=New-ManticoreFixture
Check 'closed complete native Manticore fixture preserves source and writes genuine destination Manual' {Read-Manticore $baseline}
if(-not$results[0].pass){$results|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'baseline-failure.json') -Encoding UTF8;[IO.File]::WriteAllText((Join-Path $OutputDirectory 'baseline.json'),($baseline|ConvertTo-Json -Depth 90));throw 'Positive baseline must pass before any negative mutation is credited.'}
$bad=[ordered]@{
 'different mounted source bytes'={param($f)$f.request.ownedPairs.sourceArchiveSha256='e'*64}
 'cold direction'={param($f)$f.request.ownedPairs.case='owner-seed-checkpoint-load'}
 'acquired direction'={param($f)$f.request.ownedPairs.case='persistence-save'}
 'generic direction'={param($f)$f.request.scenario='chunk7-persistence-save'}
 'second native load'={param($f)$f.request.ownedPairs.expectedNativeLoadCount=2}
 'second native write'={param($f)$f.request.ownedPairs.expectedNativeWriteCount=2}
 'caller supplies target'={param($f)$f.request|Add-Member persistenceAreaTarget ([pscustomobject]@{enterPoint='e'*32;area='f'*32;autoSaveMode='None'})}
 'capital destination'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.destinationIsCapital=$true}
 'already explored source row'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.explored=$true}
 'closed source row'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.closed=$true}
 'unrevealed source row'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.revealed=$false}
 'wrong native entrance'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.entryGuid='e'*32}
 'map record inserted'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.locationCountAfter++}
 'precondition after transfer'={param($f)$f.artifact.observations.manticoreFixturePreparation.prerequisite.transferRequests=1}
 'BeforeExit setup save'={param($f)$f.artifact.observations.manticoreFixturePreparation.nativeMode='BeforeExit'}
 'AfterEntry setup save'={param($f)$f.artifact.observations.manticoreFixturePreparation.nativeMode='AfterEntry'}
 'second native transfer'={param($f)$f.artifact.observations.manticoreFixturePreparation.transferRequests=2}
 'missing loading boundary'={param($f)$f.artifact.observations.manticoreFixturePreparation.loadingTicks=0}
 'claimed ordinary travel'={param($f)$f.artifact.observations.manticoreFixturePreparation.ordinaryTravelProof=$true}
 'invented area callback'={param($f)$f.artifact.observations.manticoreFixturePreparation.nativeAreaCallbackExpected=$true}
 'first source owner never retired'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.sourceUnmounted.native.owners=@($p.source.native.owners[0])}
 'second owner active in destination'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.destinationUnmounted.native.owners=@($p.source.native.owners[1])}
 'active control survives unmounted entry'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.destinationUnmounted.activeSlots=@($p.originalSlots[0])}
 'live dismount fact leak'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.destinationUnmounted.controlFacts.rows[1].totalFacts=1}
 'unrelated actor lacks direct control'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.destinationUnmounted.directlyControllable=@($p.destinationUnmounted.directlyControllable|Select-Object -First 4)}
 'remote actor omitted'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.destinationUnmounted.field.actors=@($p.destinationUnmounted.field.actors|Select-Object -First 4)}
 'pet replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.field.actors[1].object++}
 'Horse fact replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.field.custody.pairs[1].feature++}
 'class XP changed'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.field.custody.arrangement.actors[0].experience++}
 'same source world scope'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;foreach($w in @($p.destinationUnmounted,$p.remounted,$p.afterSave)){$w.worldScope=$p.source.worldScope}}
 'Player replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.player=1}
 'State replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.state=1}
 'combat at entry'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.field.actors[0].inCombat=$true}
 'dialogue at entry'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.mode='Dialog'}
 'unrelated hotbar collection replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.hotbars[4].collection++}
 'unrelated hotbar element replaced'={param($f)$f.artifact.observations.manticoreFixturePreparation.remounted.hotbars[4].elements[139]++}
 'unrelated null hotbar manufactured'={param($f)$f.artifact.observations.manticoreFixturePreparation.remounted.hotbars[4].elements=$null}
 'native restoration replay'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.native.semanticRestores++}
 'write before fixture ready'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.authority.nativeWrites=1}
 'native control replay'={param($f)$f.artifact.observations.manticoreFixturePreparation.destinationUnmounted.native.nativeCastRequests=3}
 'source output area reused'={param($f)$f.artifact.observations.nativeWrite.archive.AreaId=$sourceArea}
 'source owner reused'={param($f)$p=$f.artifact.observations.manticoreFixturePreparation;$p.remounted.native.owners[0].ownerId=$p.source.native.owners[0].ownerId}
 'archive generation differs'={param($f)$f.artifact.observations.nativeWrite.archive.Pairs[0].Generation++}
 'different control position'={param($f)$f.artifact.observations.manticoreFixturePreparation.reboundSlots[0].Index++}
 'missing native Dismount'={param($f)$f.artifact.observations.inputs=@($f.artifact.observations.inputs|Select-Object -Skip 1)}
 'Mount before entry replaces Dismount'={param($f)$f.artifact.observations.inputs[0].abilityGuid='f053faad986631688defa003cd7bda0e'}
 'foreign Mount caster'={param($f)$f.artifact.observations.inputs[2].casterId=$f.request.ownedPairs.riderBId}
 'native input replay'={param($f)$f.artifact.observations.inputs[3].nativeCastDelta=2}
 'native input refusal'={param($f)$f.artifact.observations.inputs[2].nativeRefusalDelta=1}
 'missing native Manual callback'={param($f)$f.artifact.observations.nativeWrite.nativeCallback=$false}
 'duplicate native Manual callback'={param($f)$f.artifact.observations.ownedOutputCompletion.callbackCount=2}
 'claimed inherited old name'={param($f)$f.artifact.observations.nativeWrite.nativeName='KMC_C7_SEED_CHECKPOINT'}
 'unfinished native save'={param($f)$f.artifact.observations.nativeWrite.operation='Saving'}
 'retained observation objects'={param($f)$f.artifact.observations.manticoreFixturePreparation.identityRetention.retainedAfterRelease=1}
 'identity collision'={param($f)$f.artifact.observations.manticoreFixturePreparation.identityRetention.faults=1}
 'unknown preparation field'={param($f)$f.artifact.observations.manticoreFixturePreparation|Add-Member extra $true}
}
foreach($mutation in $bad.GetEnumerator()){Check $mutation.Key {$f=Copy-Value $baseline;&$mutation.Value $f;Read-Manticore $f} $true}
Check 'native control slot order is not actor custody' {$f=New-ManticoreFixture;$p=$f.artifact.observations.manticoreFixturePreparation;$p.originalSlots=@($p.originalSlots|Sort-Object ActorId -Descending);$p.reboundSlots=@($p.reboundSlots|Sort-Object ActorId -Descending);Read-Manticore $f}
foreach($reader in @('Test-RuntimeResult.ps1','Test-RuntimeGameResult.ps1')){
 . Import-Functions (Join-Path $ReaderDirectory $reader) @('Assert-SubscenarioResults')
 Check ('actual external result rows '+$reader) {
  $names=@('chunk7-owned-two-pair-campaign','C7-persistence-owner-seed-restored-start','C7-persistence-native-category-all-pair-archive','C7-persistence-manticore-fixture-native-manual','C7-persistence-native-continuation','C7-persistence-cleanup')
  $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=6;subscenarioPassCount=6;subscenarioFailCount=0;assertionPassCount=6;assertionFailCount=0;subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
  Assert-SubscenarioResults $runtime
 }
}
Check 'old ordinary owner-seed Auto keeps its no-input restored-start contract' {$f=New-KmcOwnerSeedCheckpointSaveFixture 'multi-exploration-auto';Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
Check 'old owner-seed case rejects Manticore observation' {$m=New-ManticoreFixture;$f=New-KmcOwnerSeedCheckpointSaveFixture 'multi-exploration-auto';$f.artifact.observations|Add-Member manticoreFixturePreparation $m.artifact.observations.manticoreFixturePreparation;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'} $true
$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
$receipt=[ordered]@{status=$(if($failed-eq0-and$passed-gt0){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;seconds=$clock.Elapsed.TotalSeconds;results=@($results);inputs=@(@($PSCommandPath,(Join-Path $ReaderDirectory 'Chunk7ManticoreFixtureEvidence.ps1'),(Join-Path $ReaderDirectory 'Chunk7OwnedPairsEvidence.ps1'),(Join-Path $ReaderDirectory 'Chunk7PersistenceEvidence.ps1'))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeInvoked=$false;limits='Generated JSON through actual complete persistence and owned readers and result-row validators. No product build, game, native fixture, ZIP output or route qualification.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ("Manticore fixture reader: $passed PASS / $failed FAIL; receipt="+(Join-Path $OutputDirectory 'receipt.json'))
if($failed){throw 'Manticore fixture reader checks failed; preserve this output.'}
