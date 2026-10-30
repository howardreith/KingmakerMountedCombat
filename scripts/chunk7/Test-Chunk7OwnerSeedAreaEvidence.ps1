[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/owner-seed-area-reader-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Owner-seed area reader output must be fresh.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
$fixtures=([IO.File]::ReadAllText((Join-Path $FixtureDirectory 'OwnedPairsEvidenceFixtures.json'))|ConvertFrom-Json).fixtures
# Reuse only the named existing generated-evidence constructors, without running
# their complete test campaigns or replacing any production reader boundary.
function Import-TestFunctions([string]$Path,[string[]]$Names){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors.Count){throw 'Generated evidence helper did not parse.'}
 foreach($name in $Names){$nodes=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($nodes.Count-ne1){throw ('Expected one named fixture function: '+$name)};. ([scriptblock]::Create($nodes[0].Extent.Text))}
}
# Dot-sourcing the importer keeps the selected constructors in this script scope.
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedAreaAutosaveEvidence.ps1') @('Clone-Area','Put-Area','New-AreaCase')
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1') @('Copy-Value','New-KmcOwnerSeedCheckpointSelection','Set-KmcOwnerSeedDerivedLabels','New-KmcOwnerSeedCheckpointSaveFixture','New-KmcOwnerSeedCheckpointLoadFixture')
$script:areaLab=Join-Path $OutputDirectory 'virtual-lab';function Get-KmcLabRoot{$script:areaLab}
$sourceArea='b10e86516b6437d45a629931c30936fe';$destination='ead426a6c23d39548a670ee515d77df4';$entry='fd225a93828d26449803a4847947f8c1'
function New-SeedArea {
 $f=New-AreaCase;$r=$f.request;$o=$f.artifact.observations;$seed=New-KmcOwnerSeedCheckpointSaveFixture 'multi-exploration-auto'
 $old=$r.persistenceLoad.fileName;$s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-checkpoint-save' $r.persistenceLoad.sha256 'synthetic-owner-seed-cold';Put-Area $s sourceArea $sourceArea;$r.ownedPairs=$s
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks';$r.persistenceLoad.area=$sourceArea;$r.persistenceAreaTarget=[pscustomobject]@{enterPoint=$entry;area=$destination;autoSaveMode='AfterEntry'}
 foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){if($row.fileName-ceq$old){$row.fileName=$r.persistenceLoad.fileName;$row.nativeName=$r.persistenceLoad.internalName;$row.area=$sourceArea};if($row.nativeType-ceq'Auto'){$row.area=$destination}}
 Set-KmcOwnerSeedDerivedLabels $o $s;$o.inputs=@();$o.PSObject.Properties.Remove('acquisition');$o.nativeWrite.archive.AreaId=$destination
 Put-Area $o mountedStart (Copy-Value $seed.artifact.observations.mountedStart);$o.mountedStart.loadedArchive.AreaId=$sourceArea
 Put-Area $o ownerSeedCheckpointSource (Copy-Value $seed.artifact.observations.ownerSeedCheckpointSource);$o.ownerSeedCheckpointSource.checkpoint='multi-area-autosave'
 foreach($snap in @($o.snapshots)+@($o.areaTransfer.before,$o.areaTransfer.headerStarting,$o.areaTransfer.headerStaged,$o.areaTransfer.after)){$snap.semanticRestores=4;$snap.presentationRestores=2;$snap.nativePostLoads=4}
 $before=Copy-Value $o.snapshots[0];$before.label='before-native-save';$o.snapshots=@($before)+@($o.snapshots)
 $a=$o.areaTransfer;$a.enterPoint=$entry;$a.sourceArea=$sourceArea;$a.destinationArea=$destination
 for($i=0;$i-lt4;$i++){$p=@($a.before,$a.headerStarting,$a.headerStaged,$a.after)[$i];$p.area=if($i-eq0){$sourceArea}else{$destination};$p.ownedField.area=$p.area;$custody=Copy-Value $o.inheritedFixture;$custody.phase='replay-field';$p.ownedField.custody=$custody;if($i-cin@(1,2)){$p.nativeSaveBoundary.preparedArea=$destination}}
 Put-Area $o areaEntryPrerequisite ([pscustomobject][ordered]@{contract='owner-seed-visited-area-entry-v1';sourceArea=$sourceArea;destinationArea=$destination;locationGuid='758559f44d15fc844bf30a10a83154d5';locationName='OlegsTradingPost';seen=$true;revealed=$true;explored=$true;closed=$false;entryGuid=$entry;entryAreaGuid=$destination;entryName='EnterFromGlobalMap';locationCountBefore=7;locationCountAfter=7;destinationIsCapital=$false;existingLocationOnly=$true;entryEqualsRegisteredBlueprint=$true;navigationAssisted=$true;ordinaryTravelProof=$false;authoredExitProof=$false;nativeMode='AfterEntry';transferRequests=0;saveRequests=0})
 return (Copy-Value $f)
}
function New-SeedAreaCold {
 $source=New-SeedArea;$cold=New-KmcOwnerSeedCheckpointLoadFixture 'multi-exploration-auto';$r=$cold.request;$o=$cold.artifact.observations
 $r.runId='synthetic-owner-seed-area-cold';$cold.artifact.runId=$r.runId;$r.persistenceCase='multi-area-autosave';$o.checkpoint=$r.persistenceCase;$o.processId=124
 $r.ownedPairs=Copy-Value $source.request.ownedPairs;$r.ownedPairs.case='owner-seed-checkpoint-load';$r.ownedPairs.expectedNativeWriteCount=0;$r.ownedPairs.checkpointSourceRunId=$source.request.runId;$r.ownedPairs.sourceArchiveSha256='7'*64
 Put-Area $r persistenceAreaTarget (Copy-Value $source.request.persistenceAreaTarget);$r.persistenceLoad.area=$destination;$o.loadedDescriptor=Copy-Value $r.persistenceLoad;$o.loadedArchive=Copy-Value $source.artifact.observations.nativeWrite.archive
 $o.inheritedFixture=Copy-Value $source.artifact.observations.inheritedFixture;$o.inheritedCleanup=Copy-Value $source.artifact.observations.inheritedCleanup;Set-KmcOwnerSeedDerivedLabels $o $r.ownedPairs
 # The cold area consumer re-plans both continuation rays, and its source pins the producer's recorded candidate (as the sibling generic area cold fixture does).
 Put-Area $o areaContinuationPlans (Copy-Value $source.artifact.observations.areaContinuationPlans)
 $source.artifact.observations.ownerSeedCheckpointSource.manualSha256=$r.ownedPairs.sourceArchiveSha256
 $dir=Join-Path $script:areaLab ('runtime-evidence/'+$source.request.runId);[void][IO.Directory]::CreateDirectory($dir);$p=Join-Path $dir 'chunk7-persistence-evidence.json'
 [IO.File]::WriteAllText($p,($source.artifact|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false));$sha=Get-KmcSha256 $p;$r.ownedPairs.checkpointSourceArtifactSha256=$sha
 [pscustomobject]@{parent=$source;request=$r;artifact=$cold.artifact;source=[pscustomobject]@{checkpointArtifactPath=$p;checkpointArtifactSha256=$sha;checkpointArtifactLength=[long](Get-Item -LiteralPath $p).Length;checkpointCandidate=[pscustomobject]@{commit=$source.artifact.commit;dllSha256=$source.artifact.dllSha256;dllMvid=$source.artifact.dllMvid}}}
}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Reason=''){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and(-not$Reason-or$errorText-match$Reason)};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
Check 'old acquired controlled area keeps zero restoration baseline' {$f=New-AreaCase;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
Check 'owner-seed restored field controlled entry completes genuine destination Auto' {$f=New-SeedArea;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
Check 'owner-seed exact destination Auto cold restores all pairs in separate process' {$f=New-SeedAreaCold;Assert-KmcChunk7OwnedAreaColdSource $f.source $f.request $f.artifact;Assert-KmcChunk7OwnerSeedCheckpointColdSource $f.request $f.parent.artifact $f.artifact}
$bad=[ordered]@{
 'missing visited prerequisite'={param($f)$f.artifact.observations.PSObject.Properties.Remove('areaEntryPrerequisite')}
 'unexplored destination'={param($f)$f.artifact.observations.areaEntryPrerequisite.explored=$false}
 'closed destination'={param($f)$f.artifact.observations.areaEntryPrerequisite.closed=$true}
 'capital destination'={param($f)$f.artifact.observations.areaEntryPrerequisite.destinationIsCapital=$true}
 'wrong state entrance'={param($f)$f.artifact.observations.areaEntryPrerequisite.entryGuid='e'*32}
 'map row added'={param($f)$f.artifact.observations.areaEntryPrerequisite.locationCountAfter++}
 'prerequisite after transfer'={param($f)$f.artifact.observations.areaEntryPrerequisite.transferRequests=1}
 'navigation mislabelled ordinary'={param($f)$f.artifact.observations.areaEntryPrerequisite.ordinaryTravelProof=$true}
 'navigation mislabelled authored'={param($f)$f.artifact.observations.areaEntryPrerequisite.authoredExitProof=$true}
 'unknown prerequisite field'={param($f)Put-Area $f.artifact.observations.areaEntryPrerequisite extra $true}
 'missing original source'={param($f)$f.request.ownedPairs.PSObject.Properties.Remove('sourceArea')}
 'producer input already destination'={param($f)$f.request.persistenceLoad.area=$destination}
 'different controlled entry'={param($f)$f.request.persistenceAreaTarget.enterPoint='e'*32}
 'different controlled destination'={param($f)$f.request.persistenceAreaTarget.area='f'*32}
 'before exit selected'={param($f)$f.request.persistenceAreaTarget.autoSaveMode='BeforeExit'}
 'restoration baseline zeroed'={param($f)$f.artifact.observations.areaTransfer.before.semanticRestores=0}
 'restoration semantic replay'={param($f)$f.artifact.observations.areaTransfer.headerStarting.semanticRestores++}
 'restoration presentation replay'={param($f)$f.artifact.observations.areaTransfer.after.presentationRestores++}
 'restoration native postload replay'={param($f)$f.artifact.observations.areaTransfer.headerStaged.nativePostLoads++}
 'native suspension absent'={param($f)$f.artifact.observations.areaTransfer.headerStarting.suspensions=0}
 'second owner not resumed'={param($f)$f.artifact.observations.areaTransfer.headerStarting.resumes=1}
 'old mover retained'={param($f)$m=$f.artifact.observations.areaTransfer.headerStarting.movementAuthorities[0];$m.runtimeOverride=$m.baselineOverride;$m.viewOverride=$m.runtimeOverride;$m.overrideComponents=@($m.runtimeOverride)}
 'field view replaced'={param($f)$f.artifact.observations.areaTransfer.after.ownedField.actors[4].view++}
 'full party omitted'={param($f)$f.artifact.observations.areaTransfer.headerStaged.ownedField.actors=@($f.artifact.observations.areaTransfer.headerStaged.ownedField.actors|Select-Object -First 4)}
 'native Auto header source area'={param($f)$f.artifact.observations.areaTransfer.headerStarting.nativeSaveBoundary.preparedArea=$sourceArea}
 'explicit SaveGame dispatched'={param($f)$f.artifact.observations.areaTransfer.explicitSaveGameDispatches=1}
}
foreach($m in $bad.GetEnumerator()){Check $m.Key {$f=New-SeedArea;&$m.Value $f;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'} $true}
# Sprint 1 (SAVE-02): the owner-seed area producer whose genuine after-entry autosave overwrote an admitted
# pre-existing destination Auto (pinned by run, evidence SHA256 and archive SHA256) through the engine's own
# replacement: the prepared copy lands at the reserved staging leaf and is moved onto the existing leaf.
function New-SeedAreaOverwrite {
 $f=New-SeedArea;$r=$f.request;$o=$f.artifact.observations;$s=$r.ownedPairs
 Put-Area $s existingAutoRunId 'synthetic-area-auto-producer';Put-Area $s existingAutoArtifactSha256 ('5'*64);Put-Area $s existingAutoSha256 ('3'*64)
 $w=$o.nativeWrite;$a=$o.ownedOutputAdmission
 $existing=Copy-Value (@($o.ownedOutputCompletion.nativeInventory|Where-Object nativeType -CEQ 'Auto')[0]);$a.nativeInventory=@($a.nativeInventory)+@($existing)
 foreach($p in @($o.areaTransfer.headerStarting,$o.areaTransfer.headerStaged)){$p.nativeSaveBoundary.preparedLeaf='Auto_2.zks';$p.nativeSaveBoundary.preparedPath=(Join-Path (Split-Path -Parent $w.path) 'Auto_2.zks')}
 Put-Area $o areaOverwrite ([pscustomobject][ordered]@{contract='sprint1-area-auto-overwrite-v1';eventDispatch='Game.LoadArea(enterPoint, AutoSaveMode.AfterEntry): the engine''s own after-entry branch requests SaveRoutine(GetNextAutoslot())';existingRunId=$s.existingAutoRunId;existingArtifactSha256=$s.existingAutoArtifactSha256;existingSha256=$s.existingAutoSha256;existingLeaf='Auto_1.zks';existingName=$w.nativeName;existingArea=$r.persistenceAreaTarget.area;existingFolderName=$w.path;existingLength=1000;previousSha256=$s.existingAutoSha256;stagingLeaf='Auto_2.zks';stagingNumber=2;commitCountBefore=0;replacementFailuresBefore=0;replacementSiteInvokedDirectly=$false;explicitSaveGameDispatches=0;preseededByFixture=$true;existingLoaded=$false;engineShapedRoot=$false;nativeSlotCapacity=1.0;savedOfCategory=1;nextAutoslotIsExisting=$true;requestedIsExistingSlot=$true;commitCountAfter=1;commitRecorded=$true;committedDestination=$w.path;path=$w.path;landedFolderName=$w.path;sha256=$w.sha256;length=$w.length;bytesChanged=$true;stagingAbsent=$true;writtenIsNewDescriptor=$true;requestedDescriptorRetired=$true;holdsReleased=$true;failedSaves=0;replacementFailures=0;nativeWrites=1})
 return (Copy-Value $f)
}
Check 'owner-seed area overwrite: genuine after-entry autosave replaces the admitted pre-existing slot' {$f=New-SeedAreaOverwrite;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
Check 'owner-seed area overwrite: engine-shaped prepared and committed paths compare by identity' {$f=New-SeedAreaOverwrite;$o=$f.artifact.observations;Put-Area $f.request engineShapedSaveRoot $true;$o.areaOverwrite.engineShapedRoot=$true;$mixed=$o.nativeWrite.path -replace '\\','/';$o.areaOverwrite.committedDestination=$mixed;$o.areaOverwrite.landedFolderName=$mixed;$o.ownedOutputCompletion.areaSaveIdentity.path=$mixed;foreach($p in @($o.areaTransfer.headerStarting,$o.areaTransfer.headerStaged)){$p.nativeSaveBoundary.preparedPath=$p.nativeSaveBoundary.preparedPath -replace '\\','/'};Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
$badOverwrite=[ordered]@{
 'overwrite receipt missing'={param($f)$f.artifact.observations.PSObject.Properties.Remove('areaOverwrite')}
 'overwrite commit not recorded'={param($f)$f.artifact.observations.areaOverwrite.commitCountAfter=0}
 'overwrite staging retained'={param($f)$f.artifact.observations.areaOverwrite.stagingAbsent=$false}
 'overwrite bytes unchanged'={param($f)$f.artifact.observations.areaOverwrite.sha256=$f.artifact.observations.areaOverwrite.previousSha256}
 'overwrite existing pin differs'={param($f)$f.artifact.observations.areaOverwrite.existingSha256='4'*64}
 'overwrite selector minted a slot'={param($f)$f.artifact.observations.areaOverwrite.nextAutoslotIsExisting=$false}
 'overwrite two Autos of category'={param($f)$f.artifact.observations.areaOverwrite.savedOfCategory=2}
 'overwrite replacement invoked directly'={param($f)$f.artifact.observations.areaOverwrite.replacementSiteInvokedDirectly=$true}
 'overwrite explicit save dispatched'={param($f)$f.artifact.observations.areaOverwrite.explicitSaveGameDispatches=1}
 'overwrite landed elsewhere'={param($f)$f.artifact.observations.areaOverwrite.committedDestination=(Join-Path (Split-Path -Parent $f.artifact.observations.nativeWrite.path) 'Auto_2.zks')}
 'overwrite admission omits the existing slot'={param($f)$f.artifact.observations.ownedOutputAdmission.nativeInventory=@($f.artifact.observations.ownedOutputAdmission.nativeInventory[0])}
 'overwrite capacity not one'={param($f)$f.artifact.observations.areaOverwrite.nativeSlotCapacity=3.0}
 'overwrite existing run aliases consumer'={param($f)$f.request.ownedPairs.existingAutoRunId=$f.request.ownedPairs.checkpointSourceRunId}
 'overwrite prepared copy at a foreign leaf'={param($f)foreach($p in @($f.artifact.observations.areaTransfer.headerStarting,$f.artifact.observations.areaTransfer.headerStaged)){$p.nativeSaveBoundary.preparedLeaf='Auto_3.zks'}}
}
foreach($m in $badOverwrite.GetEnumerator()){Check $m.Key {$f=New-SeedAreaOverwrite;&$m.Value $f;$f=Copy-Value $f;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'} $true}
Check 'first-ever area Auto refuses an overwrite receipt' {$f=New-SeedArea;Put-Area $f.artifact.observations areaOverwrite ([pscustomobject]@{contract='sprint1-area-auto-overwrite-v1'});Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'} $true
Check 'area cold consumer refuses pre-existing slot pins' {$f=New-SeedAreaCold;Put-Area $f.request.ownedPairs existingAutoRunId 'synthetic-area-auto-producer';Put-Area $f.request.ownedPairs existingAutoArtifactSha256 ('5'*64);Put-Area $f.request.ownedPairs existingAutoSha256 ('3'*64);Assert-KmcChunk7OwnedPairsSelection $f.request} $true
function New-ManticoreReturnEntry {
 $f=New-SeedArea;$s=$f.request.ownedPairs;$s|Add-Member NoteProperty areaRoute 'manticore-to-spider-v1'
 $s.sourceArea='314e8c3b15b896a4f847c85f9250f02b'
 $s.sourceArchiveSha256='86a5ff4bc18852f48388156f627ddebf1c9047ecf69f73a1b4b3b92b231f1b30'
 $f.request.persistenceAreaTarget.enterPoint='00becd5302e6d1d4a8ad93b6c612b927'
 $f.request.persistenceAreaTarget.area='b10e86516b6437d45a629931c30936fe'
 $v=$f.artifact.observations.areaEntryPrerequisite
 $v.contract='manticore-visited-spider-return-v1';$v.sourceArea=$s.sourceArea
 $v.destinationArea=$f.request.persistenceAreaTarget.area
 $v.locationGuid='1ee73aa52dab0264ca0b671d9a593c02'
 $v.entryGuid=$f.request.persistenceAreaTarget.enterPoint;$v.entryAreaGuid=$v.destinationArea
 $v|Add-Member NoteProperty sourceManualSha256 $s.sourceArchiveSha256
 return $f
}
Check 'distinct Manticore return admits only the existing visited Spider entry' {
 $f=New-ManticoreReturnEntry
 Assert-KmcChunk7OwnerSeedAreaEntryObservation $f.request $f.artifact.observations.areaEntryPrerequisite
}
Check 'distinct Manticore return refuses array route token before source admission' {
 $f=New-ManticoreReturnEntry;$f.request.ownedPairs.areaRoute=@('manticore-to-spider-v1')
 Assert-KmcChunk7OwnedPairsSelection $f.request
} $true
foreach($fault in @('old-entry','wrong-source-manual','closed-location')){
 Check ('distinct Manticore return refuses '+$fault) {
  $f=New-ManticoreReturnEntry;$v=$f.artifact.observations.areaEntryPrerequisite
  switch($fault){'old-entry'{$v.entryGuid='fd225a93828d26449803a4847947f8c1'}
   'wrong-source-manual'{$v.sourceManualSha256='a'*64}
   'closed-location'{$v.closed=$true}}
  Assert-KmcChunk7OwnerSeedAreaEntryObservation $f.request $v
 } $true
}
foreach($fault in @('same-process','source-area-load','wrong-owner-generation','wrong-target','write-quota','wrong-manual-lineage','extra-native-prepare')){Check ('destination cold refuses '+$fault){$f=New-SeedAreaCold;switch($fault){'same-process'{$f.artifact.observations.processId=$f.parent.artifact.observations.processId};'source-area-load'{$f.request.persistenceLoad.area=$sourceArea};'wrong-owner-generation'{$f.artifact.observations.loadedArchive.Pairs[1].Generation++};'wrong-target'{$f.request.persistenceAreaTarget.enterPoint='a'*32};'write-quota'{$f.request.ownedPairs.expectedNativeWriteCount=1};'wrong-manual-lineage'{$f.request.ownedPairs.sourceArchiveSha256='a'*64};'extra-native-prepare'{$e=Copy-Value $f.artifact.observations.coldRestoration.events[0];$e.kind='prepare-before';$f.artifact.observations.coldRestoration.events+=@($e)}};Assert-KmcChunk7OwnedAreaColdSource $f.source $f.request $f.artifact;Assert-KmcChunk7OwnerSeedCheckpointColdSource $f.request $f.parent.artifact $f.artifact} $true}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . Import-TestFunctions (Join-Path $ReaderDirectory $leaf) @('Assert-SubscenarioResults')
 foreach($cold in @($false,$true)){
  $rows=if($cold){@('chunk7-owned-two-pair-campaign','C7-persistence-cold-all-pair-identity-and-no-prepare','C7-persistence-cleanup')}else{@('chunk7-owned-two-pair-campaign','C7-persistence-owner-seed-restored-start','C7-persistence-all-pair-area-before-autosave')}
  $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;subscenarioResults=@($rows|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
  Check ('complete controlled area reader plus external rows '+$cold+' '+$leaf){$f=if($cold){New-SeedAreaCold}else{New-SeedArea};Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS';Assert-SubscenarioResults $runtime}
  Check ('controlled area unknown external row '+$cold+' '+$leaf){$badRuntime=Copy-Value $runtime;$badRuntime.subscenarioResults[2].name+='-unknown';Assert-SubscenarioResults $badRuntime} $true
  Check ('controlled area duplicate external row '+$cold+' '+$leaf){$badRuntime=Copy-Value $runtime;$badRuntime.subscenarioResults[2].name=$badRuntime.subscenarioResults[1].name;Assert-SubscenarioResults $badRuntime} $true
 }
}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archivesRead=0;scope='Unchanged complete persistence/area/cold readers on generated evidence; native dispatch is not exercised';results=@($results)}|ConvertTo-Json -Depth 14),[Text.UTF8Encoding]::new($false))
Write-Output ('Owner-seed controlled area readers '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
