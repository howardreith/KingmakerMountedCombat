[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
$script:passed=0
$script:testRoot=Join-Path (Get-KmcRepositoryRoot) ('obj/chunk6c-persistence/'+[Guid]::NewGuid().ToString('N'))
# Only the external lab filesystem boundary is replaced; the same substantive
# reader and stage dispatch read real owned fixture files and hash their bytes.
function Get-KmcLabRoot { $script:testRoot }
function CopyJson($x){$x|ConvertTo-Json -Depth 70 -Compress|ConvertFrom-Json}
function Accept([string]$why,[scriptblock]$body){& $body;$script:passed++;Write-Host ('PASS '+$why)}
function Refuse([string]$why,[scriptblock]$body){$rejected=$false;try{& $body}catch{$rejected=$true};if(-not$rejected){throw ('Accepted invalid native observations: '+$why)};$script:passed++;Write-Host ('PASS refuses '+$why)}
function Trace([bool]$Closed=$false){[pscustomobject]@{faults=0;observationErrors=0;dropped=0;events=@();identityRegistry=[pscustomobject]@{faults=0;released=$Closed;retainedCount=$(if($Closed){0}else{9})}}}
function Actor([string]$Id){[pscustomobject]@{Id=$Id;Standard=0;Move=0;Swift=0}}
function Budget([string]$Id,[double]$Move=0,[double]$Swift=0){[pscustomobject]@{actor=$Id;standard=0;move=$Move;swift=$Swift}}
function Actual([bool]$Cold=$false){
 [pscustomobject]@{contract='settled-rider-spell-items-v1';rider='rider';mount='mount';mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';relationship='Mounted';riderCommandsEmpty=$true;mountCommandsEmpty=$true;pairCommand=$false;pairMovement=$false;abilitiesPending=$false;projectilesPending=$false;processesSettled=$true;tb=$false;slotBlueprint='9f10909f0be1f5141bf1c102041f93d9';slotAvailable=$false;riderDamage=0;mountDamage=0;inputs=$(if($Cold){0}else{2});snapshotCount=1;deferredSaves=0;ordinaryInputs=0;ordinaryResolved=0;castTrace=(Trace);costTrace=(Trace);rod=[pscustomobject]@{blueprint='55a059b32df920c4abe65b8ee8b56056';count=1;charges=2;slotIndex=0;exactCollection=$true;nativeSourceItemExact=$true;nativeActivatableOn=$true;sameBlueprintInventoryCount=1}}
}
function Fixture([bool]$Tb,[bool]$Cold=$false){
 $scenario=if($Tb){if($Cold){'persistence-p02-load'}else{'persistence-p02-save'}}else{if($Cold){'persistence-p04-load'}else{'persistence-p04-save'}}
 $case=if($Tb){'casting-items'}else{'mounted-casting-items'}
 $run='c6c-fixture-'+[Guid]::NewGuid().ToString('N');$process=if($Cold){72}else{71}
 $request=CopyJson @{runId=$run;scenario=$scenario;commit=('a'*40);dllSha256=('b'*64);persistenceCase=$case;evidenceRoot=(Join-Path $script:testRoot ('evidence/'+$run));fixture=@{working=@{gameId='campaign';area='area'}};persistenceLoad=@{fileName='Manual_300_KMC_P01.zks';sha256=''}}
 $archive=Join-Path $script:testRoot ('runtime-staging/persistence-'+$run+'/Saved Games/Manual_300_KMC_P01.zks')
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $archive));[IO.File]::WriteAllText($archive,'owned-disposable-archive-bytes')
 $hash=Get-KmcSha256 $archive;$request.persistenceLoad.sha256=$hash
 $a=Actual $Cold;$a.tb=$Tb
 $snapshot=CopyJson @{Mounted=$true;Rider=(Actor 'rider');Mount=(Actor 'mount');CampaignId='campaign';AreaId='area';Combat=@{TurnBased=$Tb;Round=1;Current=$null;Roster=@();Paired=$null}}
 if($Tb){$snapshot.Rider.Move=3;$snapshot.Rider.Swift=6;$snapshot.Combat.Current=CopyJson @{ActorId='rider'};$snapshot.Combat.Paired=CopyJson @{Activation=@{owner='rider'}}}else{$snapshot.Rider.Swift=4;$snapshot.Combat.Paired=CopyJson @{RiderId='rider';MountId='mount';Activation=$null;BoundaryIsCurrent=$false;Boundary=$null;Partner=$null;SplitReleaseRound=-1;PendingSplitId=$null;PendingSplitRound=-1;RenewalNotBeforeTicks=0}}
 $rows=New-Object 'Collections.Generic.List[object]'
 function Add([string]$Kind,$Detail){[void]$rows.Add((CopyJson @{runId=$run;scenario=$scenario;source=$request.commit;dll=$request.dllSha256;processId=$process;checkpoint=$case;rider=@{Id='rider'};mount=@{Id='mount'};relationship='Mounted';controls=@{DuplicateFactCount=0;NativeCastRequestCount=0};native=@{tbSetting=$Tb};kind=$Kind;detail=$Detail}))}
 if(-not$Cold){Add '6c-fixture-items-created' @{rod=@{blueprint='55a059b32df920c4abe65b8ee8b56056'}};$rows[0].relationship='Unmounted'}
 Add 'initial' $a
 if(-not$Cold){
  $before=CopyJson $a;$before.slotAvailable=$true
  $afterSpell=CopyJson $a
  $beforePotion=CopyJson $a;$beforePotion.riderDamage=3
  $afterPotion=CopyJson $a
  $events=@(
   @{kind='cast-after';actor='rider';ability=@{blueprint='9f10909f0be1f5141bf1c102041f93d9'};process=30;spellFailed=$false;arcaneFailed=$false},
   @{kind='cast-after';actor='rider';ability=@{blueprint='5590652e1c2225c4ca30c4a699ab3649'};process=31;spellFailed=$false;arcaneFailed=$false},
   @{kind='item-spend-after';actor='rider';blueprint='55a059b32df920c4abe65b8ee8b56056';identity=60;charges=2;count=1},
   @{kind='item-spend-after';actor='rider';blueprint='d52566ae8cbe8dc4dae977ef51c27d91';identity=61;charges=0;count=0},
   @{kind='heal';actor='rider';target='rider';value=3})
  $afterPotion.castTrace.events=@($events|ForEach-Object{CopyJson $_})
  $swift=if($Tb){6}else{4};$move=if($Tb){3}else{2}
  $costs=@(
   @{boundary='cost-before';command=100;state=(Budget 'rider')},
   @{boundary='cost-after';command=100;timeSinceStart=2;state=(Budget 'rider' 0 $swift)},
   @{boundary='cost-before';command=101;state=(Budget 'rider' 0 $swift)},
   @{boundary='cost-after';command=101;timeSinceStart=1;state=(Budget 'rider' $move $swift)})
  $inputs=@(@{ability=@{caster='rider';blueprint='9f10909f0be1f5141bf1c102041f93d9';runtimeActionType='Swift'};target='target';costShell=100;castShell=10},@{ability=@{caster='rider';blueprint='5590652e1c2225c4ca30c4a699ab3649';sourceItemBlueprint='d52566ae8cbe8dc4dae977ef51c27d91';runtimeActionType='Move'};target='rider';costShell=101;castShell=11})

  Add '6c-settled-use' @{before=$before;afterSpell=$afterSpell;beforePotion=$beforePotion;afterPotion=$afterPotion;inputs=$inputs;costWindow=$costs}
  Add '6c-save-request' $a
  Add 'native-write-complete' @{snapshot=$snapshot;actual=$a;path=$archive;sha256=$hash;length=(Get-Item -LiteralPath $archive).Length;nativeType='Manual';nativeCallback=$true;operation='None'}
 }else{
  Add '6c-cold-settled-observed' @{snapshot=$snapshot;actual=$a}
  Add '6c-cold-budget-observed' @{rider=$snapshot.Rider;mount=$snapshot.Mount}
  Add '6c-cold-native-items-observed' $a.rod
 }
 Add '6c-ordinary-continuation-input' $a
 $end=CopyJson $a;$end.ordinaryInputs=1;$end.ordinaryResolved=1
 Add 'usable-continuation-complete' $end
 Add '6c-observers-closed' @{nativeProcessesSettled=$true;riderCommandsEmpty=$true;mountCommandsEmpty=$true;coldItemWasReadOnly=$Cold;castTrace=(Trace $true);costTrace=(Trace $true);items=@($(if($Cold){@()}else{@(@{disposed=$true;slotRestored=$true;noOwnedItemResident=$true},@{disposed=$true;slotRestored=$true;noOwnedItemResident=$true})}))}
 [pscustomobject]@{request=$request;rows=@($rows.ToArray());game=[pscustomobject]@{processId=$process};archive=$archive}
}
function Row($f,[string]$Kind){@($f.rows|Where-Object kind -CEQ $Kind)[0]}
function Run($f){Assert-KmcCastingBaselinePersistenceEvidence $f.request $f.rows $f.game}
function Envelope($f){
 [void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
 $path=Join-Path $f.request.evidenceRoot 'persistence-observations.jsonl'
 [IO.File]::WriteAllLines($path,@($f.rows|ForEach-Object{$_|ConvertTo-Json -Depth 70 -Compress}),(New-Object Text.UTF8Encoding($false)))
 $manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='persistence-observations.jsonl';kind='persistence-evidence';sha256=(Get-KmcSha256 $path)})}
 Assert-KmcPersistenceScenarioEvidence $f.request $manifest 'PASS' $f.game
}
foreach($tb in @($false,$true)){
 $source=Fixture $tb;$cold=Fixture $tb $true
 Accept ('settled native use '+$tb) {Run $source}
 Accept ('cold native use '+$tb) {Run $cold}
 Accept ('source complete existing envelope '+$tb) {Envelope $source}
 Accept ('cold complete existing envelope '+$tb) {Envelope $cold}
 Accept ('exact immutable source/cold outcome '+$tb) {Assert-KmcCastingBaselineColdOutcome $source.rows $cold.rows $cold.request}
 if(-not$tb){
  foreach($mutation in @(
   {param($f)(Row $f 'native-write-complete').detail.snapshot.Combat.Paired.Activation=@{owner='rider'}},
   {param($f)(Row $f 'native-write-complete').detail.snapshot.Combat.Paired.Partner=@{actor='mount'}},
   {param($f)(Row $f 'native-write-complete').detail.snapshot.Combat.Paired.RiderId='foreign'}
  )){Refuse 'RT identity descriptor must not own turn state or a foreign pair' {$bad=CopyJson $source;& $mutation $bad;Run $bad}}
 }
 Refuse ('source creation must be pre-Mount '+$tb) {$bad=CopyJson $source;(Row $bad '6c-fixture-items-created').relationship='Mounted';Run $bad}
 Refuse ('settled source must remain Mounted '+$tb) {$bad=CopyJson $source;(Row $bad 'initial').relationship='Unmounted';Run $bad}
 foreach($mutation in @(
  {param($f)(Row $f 'native-write-complete').detail.actual.pairCommand=$true},
  {param($f)(Row $f 'native-write-complete').detail.actual.abilitiesPending=$true},
  {param($f)(Row $f 'native-write-complete').detail.actual.slotAvailable=$true},
  {param($f)(Row $f 'native-write-complete').detail.actual.rod.charges=3},
  {param($f)(Row $f 'native-write-complete').detail.actual.rod.sameBlueprintInventoryCount=2},
  {param($f)(Row $f 'native-write-complete').detail.nativeCallback=$false},
  {param($f)(Row $f '6c-settled-use').detail.costWindow[0].state.actor='mount'},
  {param($f)(Row $f '6c-settled-use').detail.costWindow[1].state.swift=0},
  {param($f)(Row $f '6c-settled-use').detail.costWindow[1].state.standard=6},
  {param($f)(Row $f '6c-settled-use').detail.costWindow+=@{boundary='prepare-before';state=@{actor='rider'}}},
  {param($f)(Row $f '6c-settled-use').detail.afterPotion.castTrace.events+=(Row $f '6c-settled-use').detail.afterPotion.castTrace.events[0]},
  {param($f)(Row $f '6c-settled-use').detail.afterPotion.castTrace.events+=(Row $f '6c-settled-use').detail.afterPotion.castTrace.events[3]},
  {param($f)(Row $f 'usable-continuation-complete').detail.ordinaryResolved=2},
  {param($f)(Row $f '6c-observers-closed').detail.items[0].disposed=$false},
  {param($f)(Row $f '6c-observers-closed').detail.castTrace.identityRegistry.released=$false},
  {param($f)(Row $f '6c-observers-closed').detail.costTrace.identityRegistry.retainedCount=9},
  {param($f)$f.rows[1].processId=72},
  {param($f)$f.rows[1].source=('c'*40)},
  {param($f)$f.rows[1].native.tbSetting=-not$f.rows[1].native.tbSetting}
 )){$bad=CopyJson $source;& $mutation $bad;Refuse ('source residue/identity/spending mutation '+$tb) {Run $bad}}
 foreach($mutation in @(
  {param($f)(Row $f '6c-cold-settled-observed').detail.actual.inputs=1},
  {param($f)(Row $f '6c-cold-settled-observed').detail.actual.castTrace.events=@(@{kind='item-spend-after'})},
  {param($f)(Row $f '6c-observers-closed').detail.coldItemWasReadOnly=$false},
  {param($f)$f.rows[0].controls.NativeCastRequestCount=1},
  {param($f)$f.rows[2].kind='6c-fixture-items-created'}
 )){$bad=CopyJson $cold;& $mutation $bad;Refuse ('cold replay/provision mutation '+$tb) {Run $bad}}
 foreach($mutation in @(
  {param($f)(Row $f '6c-cold-settled-observed').detail.actual.riderDamage=1},
  {param($f)(Row $f '6c-cold-settled-observed').detail.actual.rod.slotIndex=1},
  {param($f)(Row $f '6c-cold-settled-observed').detail.snapshot.Rider.Standard=6},
  {param($f)(Row $f '6c-cold-settled-observed').processId=71}
 )){$bad=CopyJson $cold;& $mutation $bad;Refuse ('cold source correspondence '+$tb) {Assert-KmcCastingBaselineColdOutcome $source.rows $bad.rows $bad.request}}
 $bad=CopyJson $cold;$bad.rows=@($bad.rows|Select-Object -First 5);Refuse ('incomplete whole envelope '+$tb) {Envelope $bad}
 [IO.File]::AppendAllText($cold.archive,'changed');Refuse ('selected archive tampering '+$tb) {Run $cold}
 [IO.File]::AppendAllText($source.archive,'changed');Refuse ('native source archive tampering '+$tb) {Run $source}
}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')