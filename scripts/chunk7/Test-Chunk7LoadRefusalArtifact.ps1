[CmdletBinding()]
param(
 [string]$RepositoryRoot,
 [string]$ReaderDirectory,
 [string]$FixturePath,
 [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Run-scoped synthetic archives only. Existing output directories are preserved.
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve previous refusal artifact test.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1')
Add-Type -AssemblyName System.IO.Compression
function Import-Functions([string]$Path,[string[]]$Names){$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$null,[ref]$null);foreach($name in $Names){$node=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($node.Count-ne1){throw ('Expected one test helper '+$name)};. ([scriptblock]::Create($node[0].Extent.Text))}}
. Import-Functions (Join-Path $PSScriptRoot 'Test-Chunk7LoadRefusalCopies.ps1') @('Clone','Put','Get-KmcLabRoot','Assert-KmcRuntimeLockOwner','Assert-KmcNoGameProcesses','Get-KmcChunk7OwnedPairsSource','Write-Zip','New-Rig','Derive','Stage-Rig','Copy-Rig','Verify-Rig')
function Copy-Value($x){Clone $x}
. Import-Functions (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OwnedPairsEvidence.ps1') @('New-KmcOwnerSeedDerivedColdFixture')
$fixtures=([IO.File]::ReadAllText($FixturePath)|ConvertFrom-Json).fixtures
function Probe {
 $hooks=@();foreach($r in @(@('Kingmaker.Blueprints.ResourcesLibrary','TryGetBlueprint','06009708','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('Newtonsoft.Json.Serialization.DefaultSerializationBinder','BindToType','060004C8','4f213745-d60e-425f-b763-df319c826ddf'),@('UberDebug','LogException','06000664','57f03756-55de-42f5-8bb3-e983306082b2'),@('UberDebug','LogExceptionChannel','06000665','57f03756-55de-42f5-8bb3-e983306082b2'))){$hooks+=@([pscustomobject]@{declaringType=$r[0];method=$r[1];token=$r[2];moduleMvid=$r[3]})}
 [pscustomobject]@{contract='owner-seed-native-load-observation-v1';installed=$true;capacity=256;blueprintFailureCount=0;dropped=0;unresolvedBlueprintIds=@();failedTypeBindCount=0;failedTypeBinds=@();loggedExceptionCount=0;loggedExceptions=@();hooks=$hooks;observerFaultCount=0;observerFaults=@()}
}
function State($F,[string]$Label,[int]$Rejected,[int]$Frame){
 $archive=$F.rig.metadata;$world=Clone $F.artifact.observations.snapshots[0];$world.label=$Label;Put $world frame $Frame;$world.semanticRestores=4;$world.presentationRestores=2;$world.nativePostLoads=4;$world.snapshots=0;$world.nativeCastRequests=0;$world.archivePhase='all-pair-presentation-restored'
 $actors=@();$party=@();$prepares=@();$index=10
 foreach($a in @($archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){$actors+=@([pscustomobject]@{id=$a.Id;object=$index});$party+=@([pscustomobject]@{native=(Clone $a);prepared=$false;inCombat=$false});$prepares+=@([pscustomobject]@{actorId=$a.Id;count=0});$index++}
 $party+=@([pscustomobject]@{native=[pscustomobject]@{Id='50505050-5050-4050-8050-505050505050';Standard=0.25;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0};prepared=$true;inCombat=$false})
 [pscustomobject]@{world=$world;paused=$true;selection=@();objects=[pscustomobject]@{player=1;state=2;areaState=3;area=4;loadedArchive=5;actors=$actors};loadedArchive=(Clone $archive);slots=(Clone $archive.Slots);party=$party;nativePrepares=$prepares;rejectedLoads=$Rejected;worldDisposals=0;nativeLoads=1;nativeWrites=0;authorizedLoads=1;authorizedWrites=0;unauthorizedLoads=0;unauthorizedWrites=0;fatalViolations=0;loadProbe=(Probe)}
}
function New-Full([string]$Checkpoint='multi-malformed-refusal-manual'){
 $rig=New-Rig $Checkpoint;$extra='50505050-5050-4050-8050-505050505050';$rig.selection.partyIds+=@($extra);[void](Stage-Rig $rig);Copy-Rig $rig
 $f=New-KmcOwnerSeedDerivedColdFixture;$r=$rig.request;$o=$f.artifact.observations
 foreach($key in @('schemaVersion','branch','commit','productVersion','dllSha256','dllMvid')){Put $r $key $f.request.$key};Put $r evidenceRoot (Join-Path $rig.lab ('runtime-evidence/'+$r.runId))
 foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$f.artifact.$key=$r.$key};$f.request=$r;$o.checkpoint=$Checkpoint;$o.loadedDescriptor=Clone $r.persistenceLoad;$o.loadedArchive=Clone $rig.metadata;$o.inheritedManualSha256=$r.ownedPairs.sourceArchiveSha256
 foreach($c in @($o.inheritedFixture,$o.inheritedCleanup)){$c.sourceRunId=$r.ownedPairs.sourceRunId;$c.sourceArtifactSha256=$r.ownedPairs.sourceArtifactSha256;$c.sourceArchiveSha256=$r.ownedPairs.sourceArchiveSha256;$c.partyIds+=@($extra);$c.currentPartyIds+=@($extra);$c.currentCharacterIds+=@($extra);$actor=Clone $c.arrangement.actors[0];$actor.actorId=$extra;$actor.blueprint='c'*32;$c.arrangement.actors+=@($actor)}
 $cold=$o.ownerSeedDerivedCold;foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedSha256')){$cold.$key=$r.ownedPairs.$key};$unrelated=[pscustomobject]@{Id=$extra;Standard=0.25;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0};$cold.unrelated=@([pscustomobject]@{actorId=$extra;before=(Clone $unrelated);after=(Clone $unrelated)})
 foreach($key in @('nativeWrite','saveRequested','ownedOutputAdmission','ownedOutputCompletion','schema1Migration','schema2Migration')){$o.PSObject.Properties.Remove($key)}
 Put $f rig $rig
 $base=State $f 'refusal-baseline' 0 100;$entries=@();$names=@('Game.LoadGameFromMainMenu','Game.LoadGame','Game.LoadGameForSmokeTest')
 for($i=0;$i-lt3;$i++){$entries+=@([pscustomobject]@{entry=$names[$i];before=(State $f 'entry-before' $i 100);after=(State $f 'entry-after' ($i+1) 100);rejectedDelta=$i+1;earlyIdentityCount=$i+1;realRejectedDecisionCount=$i+1})}
 Put $o loadRefusal ([pscustomobject]@{contract='owner-seed-real-entry-load-refusal-v1';derivationSha256=$r.loadRefusalDerivationSha256;derivation=(Read-KmcOwnedSchema1PinnedJson $rig.derivative.receiptPath $rig.derivative.receiptSha256 256KB);fixturePauseBefore=$false;baseline=$base;entries=$entries;alternateReadKind=$(if($Checkpoint-ceq'multi-malformed-refusal-manual'){'Invalid'}else{'Future'});complete=$true;menuRefresh=[pscustomobject]@{before=(State $f 'menu-refresh-before' 3 100);dispatchFrame=100;after=(State $f 'menu-refresh-complete' 3 103);completeFrame=103;areSavesUpToDate=$true};finalProbe=(Probe)})
 return $f
}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and(-not$Pattern-or$errorText-match$Pattern)};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Read-Full($F){Assert-KmcChunk7PersistenceObservation $F.request $F.artifact 'PASS'}
foreach($checkpoint in @('multi-malformed-refusal-manual','multi-future-refusal-manual')){Check ('complete actual reader, unrelated debt/prepared retained and initial empty selection '+$checkpoint){Read-Full (New-Full $checkpoint)}}
foreach($checkpoint in @('multi-malformed-refusal-manual','multi-future-refusal-manual')){Check ('signed nonzero retained object hashes are accepted '+$checkpoint){
 $f=New-Full $checkpoint;$refusal=$f.artifact.observations.loadRefusal
 $states=@($refusal.baseline,$refusal.menuRefresh.before,$refusal.menuRefresh.after)+@($refusal.entries|ForEach-Object{$_.before;$_.after})
 foreach($state in $states){
  $state.objects.player=-1159028736;$state.objects.state=1014596864;$state.objects.areaState=1764059904
  $state.objects.area=380367616;$state.objects.loadedArchive=2097024768
  $hashes=@(68234240,-126786304,-1665499136,726958336)
  for($i=0;$i-lt4;$i++){$state.objects.actors[$i].object=$hashes[$i]}
 }
 Read-Full $f
}}
$signedFaults=[ordered]@{
 'null retained object hash'={param($f)$f.artifact.observations.loadRefusal.baseline.objects.player=0}
 'out of int32 retained object hash'={param($f)$f.artifact.observations.loadRefusal.baseline.objects.player=[long]::MaxValue}
 'fractional retained object hash'={param($f)$f.artifact.observations.loadRefusal.baseline.objects.player=1.5}
 'retained actor hash aliases world object'={param($f)$f.artifact.observations.loadRefusal.baseline.objects.actors[0].object=1}
}
foreach($fault in $signedFaults.GetEnumerator()){Check $fault.Key {$f=New-Full;&$fault.Value $f;Read-Full $f} $true}
$faults=[ordered]@{
 'missing actual entry'={param($f)$f.artifact.observations.loadRefusal.entries=@($f.artifact.observations.loadRefusal.entries|Select-Object -First 2)}
 'wrong entry method'={param($f)$f.artifact.observations.loadRefusal.entries[1].entry='direct-validator'}
 'unearned early identity'={param($f)$f.artifact.observations.loadRefusal.entries[1].earlyIdentityCount=1}
 'missing native rejection'={param($f)$f.artifact.observations.loadRefusal.entries[2].realRejectedDecisionCount=2}
 'extra native load'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.nativeLoads=2}
 'write granted'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.authorizedWrites=1}
 'native disposal'={param($f)$f.artifact.observations.loadRefusal.entries[1].after.worldDisposals=1}
 'changed player object'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.objects.player=20}
 'changed archive object'={param($f)$f.artifact.observations.loadRefusal.entries[1].after.objects.loadedArchive=20}
 'changed owner generation'={param($f)$f.artifact.observations.loadRefusal.entries[2].after.world.owners[1].generation++}
 'changed unrelated debt'={param($f)$f.artifact.observations.loadRefusal.entries[1].after.party[4].native.Move=0.0}
 'changed unrelated prepared'={param($f)$f.artifact.observations.loadRefusal.entries[1].after.party[4].prepared=$false}
 'prepared type forged'={param($f)$f.artifact.observations.loadRefusal.baseline.party[4].prepared='True'}
 'unrelated actor omitted'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.party=@($f.artifact.observations.loadRefusal.entries[0].after.party|Select-Object -First 4)}
 'native Prepare entry'={param($f)$f.artifact.observations.loadRefusal.entries[2].after.nativePrepares[0].count=1}
 'selection changed'={param($f)$f.artifact.observations.loadRefusal.entries[1].after.selection=@($f.request.ownedPairs.riderAId)}
 'pause changed'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.paused=$false}
 'native clock advanced'={param($f)$f.artifact.observations.loadRefusal.entries[2].after.world.gameTicks++}
 'exception loop growth'={param($f)$p=$f.artifact.observations.loadRefusal.menuRefresh.after.loadProbe;$p.loggedExceptionCount=1;$p.loggedExceptions=@('new exception')}
 'blueprint failure growth'={param($f)$p=$f.artifact.observations.loadRefusal.entries[0].after.loadProbe;$p.blueprintFailureCount=1;$p.unresolvedBlueprintIds=@('e'*32)}
 'binder failure growth'={param($f)$p=$f.artifact.observations.loadRefusal.entries[1].after.loadProbe;$p.failedTypeBindCount=1;$p.failedTypeBinds=@('missing type')}
 'wrong native observer hook'={param($f)$f.artifact.observations.loadRefusal.baseline.loadProbe.hooks[0].token='06000000'}
 'observer dropped records'={param($f)$f.artifact.observations.loadRefusal.entries[0].after.loadProbe.dropped=1}
 'observer fault hidden'={param($f)$f.artifact.observations.loadRefusal.finalProbe.observerFaults=@('fault')}
 'final observer exception changed'={param($f)$p=$f.artifact.observations.loadRefusal.finalProbe;$p.loggedExceptionCount=1;$p.loggedExceptions=@('loop')}
 'refresh not later frame'={param($f)$r=$f.artifact.observations.loadRefusal.menuRefresh;$r.completeFrame=102;$r.after.world.frame=102}
 'refresh not complete'={param($f)$f.artifact.observations.loadRefusal.menuRefresh.areSavesUpToDate=$false}
 'unexpected native writing evidence'={param($f)Put $f.artifact.observations nativeWrite ([pscustomobject]@{sha256='e'*64})}
 'wrong derived-byte pin'={param($f)$f.request.loadRefusalDerivationSha256='e'*64}
 'claimed native fixture restoration'={param($f)$f.artifact.observations.loadRefusal.derivation.nativeRestorationProven=$true}
 'actual alternate bytes drifted'={param($f)[IO.File]::AppendAllText((Join-Path $f.rig.profile ('Saved Games/'+$f.request.persistenceAlternate.fileName)),' ')}
 'actual primary time drifted'={param($f)$p=Join-Path $f.rig.profile ('Saved Games/'+$f.request.persistenceLoad.fileName);[IO.File]::SetLastWriteTimeUtc($p,[IO.File]::GetLastWriteTimeUtc($p).AddSeconds(1))}
 'unexpected third save file'={param($f)[IO.File]::WriteAllText((Join-Path $f.rig.profile 'Saved Games/Manual_999_foreign.zks'),'foreign')}
 'unexpected child directory'={param($f)[void][IO.Directory]::CreateDirectory((Join-Path $f.rig.profile 'Saved Games/foreign-directory'))}
 'old derived cold proof omitted'={param($f)$f.artifact.observations.PSObject.Properties.Remove('ownerSeedDerivedCold')}
 'cold restoration Prepare event'={param($f)$f.artifact.observations.coldRestoration.events+=@([pscustomobject]@{kind='prepare-before';actorId=$f.request.ownedPairs.riderAId})}
 'cleanup dropped'={param($f)$f.artifact.observations.cleanup.controlsSettled=$false}
}
foreach($m in $faults.GetEnumerator()){Check $m.Key {$f=New-Full;&$m.Value $f;Read-Full $f} $true}
foreach($leaf in @('Test-RuntimeResult.ps1','Test-RuntimeGameResult.ps1')){
 . Import-Functions (Join-Path $ReaderDirectory $leaf) @('Assert-SubscenarioResults')
 $runtime=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;subscenarioResults=@(@('chunk7-owned-two-pair-campaign','C7-persistence-owner-seed-derived-cold-identity','C7-persistence-real-entry-load-refusal')|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})}
 Check ('complete refusal artifact plus external rows '+$leaf){Read-Full (New-Full);Assert-SubscenarioResults $runtime}
 Check ('external refusal unknown row '+$leaf){$bad=Clone $runtime;$bad.subscenarioResults[2].name+='-unknown';Assert-SubscenarioResults $bad} $true
 Check ('external refusal duplicate row '+$leaf){$bad=Clone $runtime;$bad.subscenarioResults[2].name=$bad.subscenarioResults[1].name;Assert-SubscenarioResults $bad} $true
}
Check 'old owned descriptor cannot carry refusal provenance' {$plain=Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0];Put $plain.request loadRefusalDerivationSha256 ('e'*64);Assert-KmcChunk7OwnedPairsDescriptor $plain.request} $true 'refusal'
Check 'old persistence artifact cannot carry refusal observations' {$plain=Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0];Put $plain.artifact.observations loadRefusal ([pscustomobject]@{});Read-Full $plain} $true 'refusal'
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;seconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;scope='Actual complete generic/refusal/derived-cold/state/ZIP readers and both result-row validators; generated evidence and synthetic ZIPs; only verified source completion/lock/process/root are facades';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Independent complete refusal artifacts '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
