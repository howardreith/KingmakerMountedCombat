[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'};if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/owner-seed-repeat-'+[Guid]::NewGuid().ToString('N'))};if(Test-Path -LiteralPath $OutputDirectory){throw 'Repeated reader output must be fresh.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
function Import-TestFunctions([string]$Path,[string[]]$Names){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors.Count){throw 'Generated evidence helper parse failed.'};foreach($name in $Names){$nodes=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($nodes.Count-ne1){throw ('Fixture function must be unique: '+$name)};. ([scriptblock]::Create($nodes[0].Extent.Text))}}
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1') @('Copy-Test','New-Artifact')
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7RepeatedWorldEvidence.ps1') @('ReplayWorld','ReplayRestoration','New-ReplayArtifact')
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedReplayEvidence.ps1') @('Add-OwnedField','New-OwnedReplay','Change-OwnedReplayEpoch')
. Import-TestFunctions (Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1') @('New-KmcOwnerSeedCheckpointSelection')
$seed=@(([IO.File]::ReadAllText((Join-Path $FixtureDirectory 'OwnedPairsEvidenceFixtures.json'))|ConvertFrom-Json).fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]
$co=Copy-Test $seed.artifact.observations;$archive=Copy-Test $co.loadedArchive;$native=@($archive.Pairs|ForEach-Object{$_.Rider;$_.Mount});$slots=Copy-Test $archive.Slots;$first=$native[0].Id;$ids=@($native|ForEach-Object Id);$request=$seed.request;$hooks=Copy-Test $co.coldRestoration.hooks
$owners=@($archive.Pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id;state='Active'}})
$subs=[pscustomobject]@{assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';executing=$false;uniqueOwnedSubscribers=1;ownedInterfaceEntries=1;entries=@([pscustomobject]@{interface='Kingmaker.PubSubSystem.IUnitLifeStateChanged';type='KingmakerMountedCombat.Integration.MountedLifecycleSubscriber';identity=-999})}
$extra='50505050-5050-4050-8050-505050505050';$baseCustody=Copy-Test $co.inheritedFixture;$baseCustody.partyIds+=@($extra);$baseCustody.currentPartyIds+=@($extra);$baseCustody.currentCharacterIds+=@($extra);$other=Copy-Test $baseCustody.arrangement.actors[0];$other.actorId=$extra;$other.blueprint='c'*32;$baseCustody.arrangement.actors+=@($other)
function New-SeedRepeat {
 $f=New-OwnedReplay $false;$r=$f.request;$o=$f.artifact.observations;$s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-persistence-load' $r.persistenceLoad.sha256 'unused';$s.PSObject.Properties.Remove('checkpointSourceRunId');$s.PSObject.Properties.Remove('checkpointSourceArtifactSha256');$s.expectedNativeLoadCount=3;$r.ownedPairs=$s
 $r.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$r.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks';$r.persistenceLoad.area='b10e86516b6437d45a629931c30936fe';$o.loadedDescriptor=Copy-Test $r.persistenceLoad;$o.loadedArchive.AreaId=$r.persistenceLoad.area;$o.inheritedManualSha256=$s.sourceArchiveSha256
 foreach($c in @($o.inheritedFixture,$o.inheritedCleanup)+@($o.repeatedWorlds.worlds|ForEach-Object{$_.ownedField.custody})+@($o.repeatedWorlds.requests|ForEach-Object{$_.before.ownedField.custody})+@($o.repeatedWorlds.settled.ownedField.custody)){$c.contract='owner-seed-derived-two-horse-native-field-v1';$c.sourceCase='owner-seed-persistence-save';$c.sourceRunId=$s.sourceRunId;$c.sourceArtifactSha256=$s.sourceArtifactSha256;$c.sourceArchiveSha256=$s.sourceArchiveSha256}
 foreach($w in @($o.repeatedWorlds.worlds)+@($o.repeatedWorlds.requests|ForEach-Object{$_.before})+@($o.repeatedWorlds.retention.events|ForEach-Object{$_.state})+@($o.repeatedWorlds.settled)){$w.area=$r.persistenceLoad.area;if($null-ne$w.PSObject.Properties['ownedField']){$w.ownedField.area=$w.area}}
 foreach($q in $o.repeatedWorlds.requests){$q.fileName=$r.persistenceLoad.fileName};$o.repeatedWorlds.finalRetention=Copy-Test $o.repeatedWorlds.retention
 $parent=[pscustomobject]@{runId=$s.sourceRunId;status='PASS';commit=('f'*40);dllSha256=('f'*64);dllMvid='ffffffff-ffff-4fff-8fff-ffffffffffff';observations=[pscustomobject]@{nativeWrite=[pscustomobject]@{sha256=$s.sourceArchiveSha256;archive=(Copy-Test $o.loadedArchive)};ownerSeedManual=[pscustomobject]@{ancestry='owner-seed-derived';seedId=$s.seedId;seedSha256=$s.seedSha256;processId=4242;arrangement=(Copy-Test $o.inheritedFixture.arrangement);fieldWorld=[pscustomobject]@{party=@($s.partyIds|ForEach-Object{[pscustomobject]@{id=$_}})}}}}
 [pscustomobject]@{request=$r;artifact=$f.artifact;parent=$parent}
}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Reason=''){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and(-not$Reason-or$errorText-match$Reason)};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Read-Repeat($F){Assert-KmcChunk7PersistenceObservation $F.request $F.artifact 'PASS';Assert-KmcChunk7OwnerSeedDerivedColdSource $F.request $F.parent $F.artifact}
Check 'three native loads preserve original Manual lineage and fresh all-party epochs' {Read-Repeat (New-SeedRepeat)}
Check 'old acquired repeated source remains accepted' {$f=New-OwnedReplay $false;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
$faults=[ordered]@{
 'one-load quota'={param($f)$f.request.ownedPairs.expectedNativeLoadCount=1}
 'four-load quota'={param($f)$f.request.ownedPairs.expectedNativeLoadCount=4}
 'write entitlement'={param($f)$f.request.ownedPairs.expectedNativeWriteCount=1}
 'checkpoint parent'={param($f)$f.request.ownedPairs|Add-Member checkpointSourceRunId 'other';$f.request.ownedPairs|Add-Member checkpointSourceArtifactSha256 ('8'*64)}
 'area campaign alias'={param($f)$f.request.persistenceCase='multi-exploration-three-area-transfers-manual'}
 'source from same process'={param($f)$f.parent.observations.ownerSeedManual.processId=$f.artifact.observations.processId}
 'producer status fail'={param($f)$f.parent.status='FAIL'}
 'producer ancestry ordinary'={param($f)$f.parent.observations.ownerSeedManual.ancestry='ordinary'}
 'producer seed changed'={param($f)$f.parent.observations.ownerSeedManual.seedSha256='e'*64}
 'producer graph changed'={param($f)$f.parent.observations.nativeWrite.archive.Pairs[1].Generation++}
 'producer arrangement changed'={param($f)$f.parent.observations.ownerSeedManual.arrangement.money++}
 'producer roster missing'={param($f)$f.parent.observations.ownerSeedManual.fieldWorld.party=@($f.parent.observations.ownerSeedManual.fieldWorld.party|Select-Object -First 4)}
 'native fourth load'={param($f)$f.artifact.observations.repeatedWorlds.nativeLoadRequests=4}
 'native write'={param($f)$f.artifact.observations.repeatedWorlds.nativeWriteRequests=1}
 'stale unrelated actor'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].object=$f.artifact.observations.repeatedWorlds.worlds[0].ownedField.actors[4].object}
 'cross-role previous epoch feature'={param($f)Change-OwnedReplayEpoch $f 'feature'}
 'native Prepare observed'={param($f)$f.artifact.observations.repeatedWorlds.restoration.events+=@([pscustomobject]@{kind='prepare-before';actorId=$f.request.ownedPairs.riderAId})}
 'restored control omitted'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].nativeSlots=@($f.artifact.observations.repeatedWorlds.worlds[1].nativeSlots|Select-Object -First 3)}
 'local epoch retained'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].retainedAfterRelease=1}
 'subscriber growth'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].subscriptions.ownedInterfaceEntries++}
 'input copy changed'={param($f)$f.artifact.observations.repeatedWorlds.inputUnchanged=$false}
}
foreach($m in $faults.GetEnumerator()){Check $m.Key {$f=New-SeedRepeat;&$m.Value $f;Read-Repeat $f} $true}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . Import-TestFunctions (Join-Path $ReaderDirectory $leaf) @('Assert-SubscenarioResults')
 $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;subscenarioResults=@(@('chunk7-owned-two-pair-campaign','C7-persistence-three-native-loads','C7-persistence-cleanup')|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
 Check ('complete repeated reader plus external runtime rows '+$leaf){Read-Repeat (New-SeedRepeat);Assert-SubscenarioResults $runtime}
 Check ('repeated unknown external row '+$leaf){$badRuntime=Copy-Test $runtime;$badRuntime.subscenarioResults[2].name+='-unknown';Assert-SubscenarioResults $badRuntime} $true
 Check ('repeated duplicate external row '+$leaf){$badRuntime=Copy-Test $runtime;$badRuntime.subscenarioResults[2].name=$badRuntime.subscenarioResults[1].name;Assert-SubscenarioResults $badRuntime} $true
}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archivesRead=0;scope='Complete actual repeated/owned/lineage readers on generated evidence; native load and event dispatch unexercised';results=@($results)}|ConvertTo-Json -Depth 14),[Text.UTF8Encoding]::new($false));Write-Output ('Owner-seed repeated readers '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
