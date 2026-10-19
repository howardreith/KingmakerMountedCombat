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
 Put-Area $o areaEntryPrerequisite ([pscustomobject][ordered]@{contract='owner-seed-visited-area-entry-v1';sourceArea=$sourceArea;destinationArea=$destination;locationGuid='758559f44d15fc844bf30a10a83154d5';locationName='OlegsTradingPost';seen=$true;revealed=$true;explored=$true;closed=$false;entryGuid=$entry;entryAreaGuid=$destination;entryName='EnterFromGlobalMap';locationCountBefore=7;locationCountAfter=7;existingLocationOnly=$true;entryEqualsRegisteredBlueprint=$true;navigationAssisted=$true;ordinaryTravelProof=$false;authoredExitProof=$false;nativeMode='AfterEntry';transferRequests=0;saveRequests=0})
 return (Copy-Value $f)
}
function New-SeedAreaCold {
 $source=New-SeedArea;$cold=New-KmcOwnerSeedCheckpointLoadFixture 'multi-exploration-auto';$r=$cold.request;$o=$cold.artifact.observations
 $r.runId='synthetic-owner-seed-area-cold';$cold.artifact.runId=$r.runId;$r.persistenceCase='multi-area-autosave';$o.checkpoint=$r.persistenceCase;$o.processId=124
 $r.ownedPairs=Copy-Value $source.request.ownedPairs;$r.ownedPairs.case='owner-seed-checkpoint-load';$r.ownedPairs.expectedNativeWriteCount=0;$r.ownedPairs.checkpointSourceRunId=$source.request.runId;$r.ownedPairs.sourceArchiveSha256='7'*64
 Put-Area $r persistenceAreaTarget (Copy-Value $source.request.persistenceAreaTarget);$r.persistenceLoad.area=$destination;$o.loadedDescriptor=Copy-Value $r.persistenceLoad;$o.loadedArchive=Copy-Value $source.artifact.observations.nativeWrite.archive
 $o.inheritedFixture=Copy-Value $source.artifact.observations.inheritedFixture;$o.inheritedCleanup=Copy-Value $source.artifact.observations.inheritedCleanup;Set-KmcOwnerSeedDerivedLabels $o $r.ownedPairs
 $source.artifact.observations.ownerSeedCheckpointSource.manualSha256=$r.ownedPairs.sourceArchiveSha256
 $dir=Join-Path $script:areaLab ('runtime-evidence/'+$source.request.runId);[void][IO.Directory]::CreateDirectory($dir);$p=Join-Path $dir 'chunk7-persistence-evidence.json'
 [IO.File]::WriteAllText($p,($source.artifact|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false));$sha=Get-KmcSha256 $p;$r.ownedPairs.checkpointSourceArtifactSha256=$sha
 [pscustomobject]@{parent=$source;request=$r;artifact=$cold.artifact;source=[pscustomobject]@{checkpointArtifactPath=$p;checkpointArtifactSha256=$sha;checkpointArtifactLength=[long](Get-Item -LiteralPath $p).Length}}
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
