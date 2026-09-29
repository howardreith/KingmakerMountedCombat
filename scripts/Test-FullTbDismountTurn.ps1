param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
[Reflection.Assembly]::LoadFrom((Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed/Newtonsoft.Json.dll'))|Out-Null
$a=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$m=$a.GetType('KingmakerMountedCombat.Diagnostics.NativeDismountGroundEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-GroundCase($x){$x|ConvertTo-Json -Depth 90|ConvertFrom-Json}
function New-DismountGroundCase {
 $r=[pscustomobject]@{actor='rider';actorObject=11;inCombat=$true;grantSequence=2;standard=0.0;move=0.0;swift=0.0;reactions=1;reactionsPerRound=1;reactionCooldown=0.0;initiativeCooldown=0.0;initiativeOrder=10;measuredAllowedTime=1.0}
 $h=Copy-GroundCase $r;$h.actor='mount';$h.actorObject=12;$h.initiativeOrder=8
 $b=[pscustomobject]@{frame=100;gameTicks=1000000L;allocationSequence=0;turnObject=501;round=2;currentActor='rider';status='Preparing';turnBased=$true;pairedSequence=2;pairedSplit=$false;partnerActor='mount';partnerContextObject=502;controllerObject=91;sessionObject=92;adoptionCount=1;state=[pscustomobject]@{relationshipState='Mounted';generation=8;ledger=[pscustomobject]@{acceptedMount=1;forcedDetach=1};selectedIds=@('rider')};riderResources=$r;mountResources=$h}
 $after=Copy-GroundCase $b;$after.status='Acting';$after.frame=110;$after.allocationSequence=9;$after.mountResources.move=0.2;$after.mountResources.measuredAllowedTime=1.2
 $cmd=[pscustomobject]@{id=701;executor='mount';type='Kingmaker.UnitLogic.Commands.UnitMoveTo';acted=$false;finished=$false;result='None'}
 $terminal=Copy-GroundCase $cmd;$terminal.acted=$true;$terminal.finished=$true;$terminal.result='Success'
 $events=@();$seq=0
 foreach($boundary in @('admission-before','admission-after','native-movement-displacement','command-end-before','command-end-after','cost-before','actor-cost-before','actor-cost-after','cost-after')){
  $seq++;$state=Copy-GroundCase $h;if($seq-ge3){$state.move=0.2;$state.measuredAllowedTime=1.2}
  $events+=@([pscustomobject]@{sequence=$seq;frame=(100+$seq);gameTicks=1000000L;state=$state;nativeTurnBased=$true;nativePassing=$false;round=2;turn=501;currentActor='rider';boundary=$boundary;command=701;commandType=$cmd.type;actionType='Move';simulatingClick=$false;ignoreCooldown=$true;acted=($seq-ge3);finished=($seq-ge4);result=$(if($seq-ge4){'Success'}else{'None'})})
 }
 $hooks=@(foreach($token in @('060026B2','06009120','0600838F','060093A1','0600934A','06000C3C','0600C3BE','060018A9','06000C5E')){[pscustomobject]@{token=$token;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})
 [pscustomobject]@{before=$b;after=$after;admittedCommand=$cmd;terminalCommand=$terminal;traceComplete=$true;events=$events;observerHooks=$hooks}
}
$script:checks=0
function Check-GroundCase($p,[bool]$Expected){
 $ok=$true;try{$args=[object[]]::new(3);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 90 -Compress));$args[1]='rider';$args[2]='mount';$null=$m.Invoke($null,$args)}catch{$ok=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcDismountGroundSetup $p 'rider' 'mount'}catch{$external=$false;if($Expected){throw}}
 if($ok-ne$Expected-or$external-ne$Expected){throw ('Ground producer/external differs '+$ok+'/'+$external+' expected '+$Expected)};$script:checks+=2
}
$valid=New-DismountGroundCase;Check-GroundCase $valid $true
function Reject-Ground([scriptblock]$Mutate){$p=Copy-GroundCase $valid;& $Mutate $p;try{Check-GroundCase $p $false}catch{throw ('Mutation '+$Mutate+': '+$_)}}
foreach($f in @('reactions','reactionsPerRound','reactionCooldown','initiativeOrder','initiativeCooldown','standard','swift','grantSequence')){
 foreach($role in @('riderResources','mountResources')){Reject-Ground {param($p)$p.after.$role.$f++}}
 Reject-Ground {param($p)$p.events[4].state.$f++}
}
Reject-Ground {param($p)$p.after.riderResources.move=0.2}
Reject-Ground {param($p)$p.events[4].state.actor='rider';$p.events[4].state.actorObject=11;$p.events[4].state.initiativeOrder=10}
Reject-Ground {param($p)$p.terminalCommand.executor='rider'}
Reject-Ground {param($p)$p.terminalCommand.result='Interrupt'}
Reject-Ground {param($p)$p.terminalCommand.id++}
Reject-Ground {param($p)$p.after.turnObject++}
Reject-Ground {param($p)$p.before.status='Acting'}
Reject-Ground {param($p)$p.after.status='Preparing'}
Reject-Ground {param($p)$p.after.round++}
Reject-Ground {param($p)$p.before.state.selectedIds=@('mount')}
Reject-Ground {param($p)$p.after.state.selectedIds=@('rider','mount')}
Reject-Ground {param($p)$p.after.pairedSplit=$true}
Reject-Ground {param($p)$p.after.state.generation++}
Reject-Ground {param($p)$p.after.state.ledger.forcedDetach++}
Reject-Ground {param($p)$p.events[4].state.move=0.3}
Reject-Ground {param($p)$p.after.mountResources.measuredAllowedTime=1.4}
Reject-Ground {param($p)$p.events[4].boundary='clear-after'}
Reject-Ground {param($p)$p.events[4].boundary='opportunity-before'}
Reject-Ground {param($p)$p.events[4].boundary='prepare-before'}
Reject-Ground {param($p)$p.events[6].state.move=0.3}
Reject-Ground {param($p)$p.events[5].command++}
Reject-Ground {param($p)$p.events[5].ignoreCooldown=$false}
Reject-Ground {param($p)$p.events[5].boundary='actor-cost-before'}
Reject-Ground {param($p)$p.events[5].actionType='Standard'}
Reject-Ground {param($p)$p.events[0].simulatingClick=$true}
Reject-Ground {param($p)$p.events[4].nativePassing=$true}
Reject-Ground {param($p)$p.events[4].currentActor='mount'}
Reject-Ground {param($p)$p.events[4].sequence++}
Reject-Ground {param($p)$p.events[4].boundary='fixture-observation'}
Reject-Ground {param($p)$p.observerHooks=@()}
Reject-Ground {param($p)$p.traceComplete=$false}
$nextPair=$a.GetType('KingmakerMountedCombat.Diagnostics.NativeDismountGroundEvidence',$true).GetMethod('AssertNextPair',[Reflection.BindingFlags]'Static,NonPublic')
function Check-NextPair($boundary,[bool]$expected){
 $ok=$true;try{$args=[object[]]::new(3);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($boundary|ConvertTo-Json -Depth 90 -Compress));$args[1]='rider';$args[2]='mount';$null=$nextPair.Invoke($null,$args)}catch{$ok=$false;if($expected){throw}}
 if($ok-ne$expected){throw 'Idle next rider pairing refusal differed'};$script:checks++
}
Check-NextPair $valid.before $true
foreach($field in @('pairedSequence','partnerContextObject','turnObject')){
 $b=Copy-GroundCase $valid.before;$b.$field=0;Check-NextPair $b $false
}
foreach($field in @('currentActor','partnerActor')){
 $b=Copy-GroundCase $valid.before;$b.$field='unrelated';Check-NextPair $b $false
}
$b=Copy-GroundCase $valid.before;$b.partnerActor=$null;Check-NextPair $b $false
$b=Copy-GroundCase $valid.before;$b.pairedSplit=$true;Check-NextPair $b $false
$original=Join-Path $repo '../../runtime-evidence/c6a-precision-a-full-tb/phase3d-horse-scenario-evidence.json'
if((Get-FileHash -LiteralPath $original).Hash.ToLowerInvariant()-cne'f948067058709777fd8bce9d2ec7f04123032b4cbaeac44578b23a91c3e5fa72'){throw 'Original full TB failure changed'}
$failure=Get-Content -Raw $original|ConvertFrom-Json;$waiting=$failure.observations.chunk6aDismountAvailability
if($failure.status-cne'FAIL'-or$waiting.enabled-ne$false-or$waiting.reason-cne'The rider has no Move action available to dismount.'-or$waiting.state.round-ne1-or$waiting.state.currentTurnStatus-cne'Acting'-or$waiting.state.rider.nativeTurnObject-ne200452992-or$waiting.state.rider.move-le3-or@($failure.observations.chunk6aCommandProofs|Where-Object window -CEQ 'combat-dismount').Count-ne0){throw 'Original spent-turn failure not reproduced'}
$script:checks++
$reactionRoot=Join-Path $repo '../../runtime-evidence/c6a-later-turn-a-full-tb'
if((Get-FileHash -LiteralPath (Join-Path $reactionRoot 'phase3d-horse-scenario-evidence.json')).Hash.ToLowerInvariant()-cne'4ee42e8167723ab685f7838472d898471c463197c05fba13e1f66ca06ac7e460'){
 throw 'Original next-turn reaction failure artifact changed'
}
$reactionFailure=Get-Content -Raw (Join-Path $reactionRoot 'runtime-game-result.json')|ConvertFrom-Json
$row=@($reactionFailure.subscenarioResults|Where-Object name -CEQ 'phase3d-horse-runtime-exception')
if($reactionFailure.status-cne'FAIL'-or$reactionFailure.commit-cne'5e870d5f19f67b6fbce820ab0cfa71626dd11167'-or$row.Count-ne1-or
 @($row[0].errors).Count-ne1-or$row[0].errors[0]-cne'InvalidOperationException: Dismount ground setup: undeclared native event: opportunity-before'){
 throw 'Original next-turn reaction assertion changed'
}
$script:checks++
'DISMOUNT LATER-TURN GROUND PASS='+$script:checks+' FAIL=0; synthetic negatives and immutable failure binding, no native qualification'
