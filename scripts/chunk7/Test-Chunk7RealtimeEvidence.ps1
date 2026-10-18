$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7CombatEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7RealtimeEvidence.ps1')
$passed=0
function RtSnap([int]$Frame){$v=Snap $Frame;$v.currentActor=$null;return $v}
$rtCosts=@();$rtOrdinal=0
function Cost([string]$Actor,[long]$Command,[string]$Type,[int]$Frame,[bool]$Full=$false){
    foreach($kind in @('cost-before','cost-after')){$after=$kind-ceq'cost-after';$script:rtCosts+=[pscustomobject]@{sequence=$script:rtCosts.Count+1;boundary=$kind;frame=$Frame;command=$Command;commandActor=$Actor;commandType=$Type;actionType='Standard';state=[pscustomobject]@{actor=$Actor;standard=$(if($after){3.0}else{0.0});move=$(if($after-and$Full){3.0}else{0.0});swift=0.0;reactions=1;reactionCooldown=0.0}}}
}
function RtAttack([int]$Pair,[int]$Member,[int]$Frame){
    $script:rtOrdinal++;$actor=$ids[$Pair*2+$Member];$caster=$ids[$Pair*2];$before=RtSnap $Frame;$after=RtSnap ($Frame+4);$after.actors[$Pair*2+$Member].standard=2.5
    Cost $actor (1000+$script:rtOrdinal) 'KingmakerMountedCombat.Integration.MountedPairAttackCommand' ($Frame+1)
    [pscustomobject]@{casterId=$caster;executorId=$caster;selectionAfterInput=$ids[2-2*$Pair];nativeCastDelta=1;nativeRefusalDelta=0;command=2000+$script:rtOrdinal;clicked=$true;before=$before;after=$after;outcome=[pscustomobject]@{actorId=$actor;resourceOwnerId=$actor;targetId=$target;result='Success';completedAttacks=1;nativeAttackRule=$true;singleAttack=$true}}
}
function Shell([long]$Command,[string]$Actor,[string]$Target){[pscustomobject]@{identity=$Command;actorId=$Actor;targetId=$Target;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';finished=$false}}
function Trace {$v=Copy-Value $good.observations.allocationTrace;$v.events=@($script:rtCosts|ForEach-Object{Copy-Value $_});return $v}
function Base([string]$Scenario){
    $o=Copy-Value $good.observations;$o.scenario=$Scenario;$o.inputs=@($o.inputs|Where-Object label -CIN @('mount-a','mount-b'));$o.allocationTrace=Trace
    foreach($entry in @{realTime=$true;selectedRealtimeCaseComplete=$true;initiativeInputsChanged=$false;chargeSettingAtAdmission=($Scenario-ceq'chunk7-two-pair-realtime-charge');
        initiativeInputs=@($ids|ForEach-Object{[pscustomobject]@{actorId=$_;before=5;input=5}});nativeEffects=[pscustomobject]@{dropped=0;events=@()};realtimeWindows=@()}.GetEnumerator()){$o|Add-Member NoteProperty $entry.Key $entry.Value -Force}
    $v=Copy-Value $good;$v.evidenceKind='chunk7-realtime-evidence';$v.scenario=$Scenario;$v.observations=$o;return $v
}
function RequestFor($Artifact){$r=Copy-Value $request;$r.scenario=$Artifact.scenario;return $r}
function MoveRow([int]$Pair,[int]$Frame,[long]$Command){[pscustomobject]@{casterId=$ids[2*$Pair];executorId=$ids[2*$Pair+1];command=$Command;clicked=$true;before=(RtSnap $Frame)}}
function Stop([int]$Frame,[long]$NeighbourCommand){$before=RtSnap $Frame;$before.actors[1].position.x=0.5;$after=Copy-Value $before;$commands=@([pscustomobject]@{identity=$NeighbourCommand;actorId=$ids[3];finished=$false});[pscustomobject]@{before=$before;after=$after;neighbourOwnerBefore=$owners[1].id;neighbourOwnerAfter=$owners[1].id;neighbourGenerationBefore=1;neighbourGenerationAfter=1;neighbourCommandsBefore=$commands;neighbourCommandsAfter=@($commands|ForEach-Object{Copy-Value $_})}}
$rtCosts=@();$rtOrdinal=0
$a=RtAttack 0 0 10;$b=RtAttack 1 1 10;$neighbour=RtAttack 1 1 20
$windows=@([pscustomobject]@{kind='two-native-primary-inputs';a=$a;b=$b;before=(RtSnap 10);after=(RtSnap 15);admittedShells=@((Shell $a.command $ids[0] $target),(Shell $b.command $ids[2] $target))},
    [pscustomobject]@{kind='native-stop-local';neighbour=$neighbour;movement=(MoveRow 0 20 3000);stop=(Stop 21 $neighbour.command)})
$unmountedBefore=RtSnap 40;$unmountedBefore.owners=@($unmountedBefore.owners[1]);$unmountedAfter=Copy-Value $unmountedBefore;$unmountedAfter.frame=45
$u=[pscustomobject]@{before=$unmountedBefore;after=$unmountedAfter;actorId=$ids[0];commandType='Kingmaker.UnitLogic.Commands.UnitAttack';command=4000;selectionAfterInput=$ids[2];clicked=$true;nativeStopSent=$true;terminal=$true;neighbourOwner=$owners[1].id;
    effectsBefore=[pscustomobject]@{events=@()};effectsAfter=[pscustomobject]@{events=@([pscustomobject]@{kind='weapon-resolved';actor=$ids[0];target=$target})}}
Cost $ids[0] 4000 'Kingmaker.UnitLogic.Commands.UnitAttack' 41
$core=Base 'chunk7-two-pair-realtime-core';$core.observations.realtimeWindows=$windows
$core.observations|Add-Member NoteProperty defaultOffCharge @(@(0,2)|ForEach-Object{[pscustomobject]@{casterId=$ids[$_];reason='Enable Mounted Charge in the KMC mod settings.';castDelta=0;featureEnabled=$false;available=$false;visible=$false;nativeFactPresent=$false;clicked=$false;inputAttempted=$false;nativeAbilityFacts=@('3af2b81f4d72bbb30501fa730fcdf36e');stimulus='read-only native ability inventory and availability; default-off fact is not leased';before=(RtSnap 5);after=(RtSnap 5)}})
$core.observations|Add-Member NoteProperty unmountedControl $u
$d=Copy-Value @($good.observations.inputs|Where-Object label -CEQ 'r1-native-dismount-a')[0];$d.label='rt-native-dismount-a';$core.observations.inputs+=,$d
$core.observations.localFailure=[pscustomobject]@{failureCount=1;injectedPair='b';healthyNeighbourVisited=$true;before=(RtSnap 50);held=(RtSnap 50)}
$rtCosts=@();$rtOrdinal=0
$neighbour=RtAttack 1 1 10;Cost $ids[0] 5000 'Kingmaker.UnitLogic.Commands.UnitUseAbility' 11
$casting=Base 'chunk7-two-pair-realtime-casting';$c=[pscustomobject]@{before=(RtSnap 10);after=(RtSnap 20);casterId=$ids[0];targetId=$ids[1];blueprint='5590652e1c2225c4ca30c4a699ab3649';executorId=$ids[0];selectionAfterInput=$ids[2];damageBefore=0;wound=3;damageAfter=0;requestedNativeDamage=3;nativeDamageDifficulty=1.0;clicked=$true;slotAvailableBefore=$true;slotAvailableAfter=$false;shellFinished=$true;executionEnded=$true;command=5000;neighbour=$neighbour;
    admittedShells=@((Shell 5000 $ids[0] $ids[1]),(Shell $neighbour.command $ids[2] $target));effectsBefore=[pscustomobject]@{events=@()};effectsAfter=[pscustomobject]@{events=@([pscustomobject]@{kind='damage-after';actor=$target;target=$ids[1];damage=3},[pscustomobject]@{kind='heal-after';actor=$ids[0];target=$ids[1];value=3})}}
$casting.observations|Add-Member NoteProperty casting $c
$casting.observations|Add-Member NoteProperty castingInventory @([pscustomobject]@{actorId=$ids[0];preparedSpells=@([pscustomobject]@{blueprint=$c.blueprint;available=$true})},[pscustomobject]@{actorId=$ids[2];preparedSpells=@()})
$rtCosts=@();$rtOrdinal=0;$chargeWindows=@()
foreach($i in 0..1){$frame=10+40*$i;$shell=6000+$i;$move=7000+$i;Cost $ids[0] $shell 'Kingmaker.UnitLogic.Commands.UnitUseAbility' ($frame+1) $true
    $after=RtSnap ($frame+10);$after.actors[3].position.x=0.5
    $lease=[pscustomobject]@{applied=$true;restored=$true;chargingRestoredExactly=$true;speedOverrideRestoredExactly=$true;riderChargingRestoredExactly=$true;forcedPathOutstanding=$false;buffOutstanding=$false}
    $w=[pscustomobject]@{kind=$(if($i-eq0){'charge-success'}else{'charge-cancel'});before=(RtSnap $frame);after=$after;featureEnabled=$true;available=$true;targetAllowed=$true;straightRoute=$true;clicked=$true;landingBlocked=$false;casterId=$ids[0];targetId=$target;abilityGuid='d79eaec224a7a832e738eb81baef9d49';castDelta=1;refusalDelta=0;selectionAfterInput=$ids[2];command=8000+$i;shell=$shell;admittedShell=(Shell $shell $ids[0] $target);movement=(MoveRow 1 $frame $move);admittedNeighbourCommands=@([pscustomobject]@{identity=$move;actorId=$ids[3];finished=$false});neighbourOwner=$owners[1].id;settled=[pscustomobject]@{shellActed=$true;shellFinished=$true;executionEnded=$true;commandFinished=$true;lease=$lease};effectsBefore=[pscustomobject]@{events=@()};effectsAfter=[pscustomobject]@{events=@()}}
    if($i-eq0){$w.effectsAfter.events=@([pscustomobject]@{kind='attack-before';actor=$ids[0];target=$target;opportunity=$false});$w|Add-Member NoteProperty outcome ([pscustomobject]@{actorId=$ids[0];resourceOwnerId=$ids[0];targetId=$target;result='Success';completedAttacks=1;nativeAttackRule=$true})}
    else{$stop=Stop ($frame+3) $move;$stop|Add-Member NoteProperty charge ([pscustomobject]@{mountCharging=$true;mountMoving=$true;shellActed=$true});$w|Add-Member NoteProperty stop $stop}
    $chargeWindows+=,$w
}
$charge=Base 'chunk7-two-pair-realtime-charge';$charge.observations.realtimeWindows=$chargeWindows;$retreatAfter=RtSnap 40;$retreatAfter.actors[1].position.z=0.5
$charge.observations|Add-Member NoteProperty chargeRetreat ([pscustomobject]@{movement=(MoveRow 0 30 9000);after=$retreatAfter})
foreach($case in @($core,$casting,$charge)){Check ('complete synthetic '+$case.scenario) {Assert-KmcChunk7RealtimeEvidence (RequestFor $case) $case 'PASS'}}
$cases=@(
    @('RT mode is not proven',$core,{$args[0].observations.realTime=$false}),@('RT fixture initiative changed',$core,{$args[0].observations.initiativeInputs[0].input=99}),
    @('RT trace dropped',$core,{$args[0].observations.allocationTrace.dropped=1}),@('RT actor cost transferred',$core,{$args[0].observations.allocationTrace.events[1].commandActor=$ids[3]}),
    @('RT native cost omitted',$core,{$args[0].observations.allocationTrace.events[1].boundary='observation'}),@('RT no spent cost',$core,{$args[0].observations.allocationTrace.events[1].state.standard=0.0}),
    @('RT default-off Charge admitted',$core,{$args[0].observations.defaultOffCharge[0].castDelta=1}),
    @('RT default-off Charge native fact present',$core,{$args[0].observations.defaultOffCharge[0].nativeAbilityFacts+=,'d79eaec224a7a832e738eb81baef9d49'}),@('RT overlap shell already terminal',$core,{$args[0].observations.realtimeWindows[0].admittedShells[0].finished=$true}),
    @('RT inputs occur in different updates',$core,{$args[0].observations.realtimeWindows[0].b.before.frame=11}),@('RT move lacks physical displacement',$core,{$args[0].observations.realtimeWindows[1].stop.before.actors[1].position.x=0.0}),
    @('RT Stop changes neighbour resources',$core,{$args[0].observations.realtimeWindows[1].stop.after.actors[3].standard=3.0}),@('RT Stop changes generation',$core,{$args[0].observations.realtimeWindows[1].stop.neighbourGenerationAfter=2}),
    @('RT same-length neighbour command actor substitution',$core,{$args[0].observations.realtimeWindows[1].stop.neighbourCommandsAfter[0].actorId=$ids[0]}),
    @('RT same-length owner mount substitution',$core,{$args[0].observations.realtimeWindows[0].after.owners[1].mountId=$ids[1]}),
    @('unmounted native terminal omitted',$core,{$args[0].observations.unmountedControl.terminal=$false}),
    @('unmounted native Dismount omitted',$core,{$args[0].observations.inputs=@($args[0].observations.inputs|Where-Object label -CNE 'rt-native-dismount-a')}),
    @('RT Stop has no live neighbour',$core,{$args[0].observations.realtimeWindows[1].stop.neighbourCommandsBefore[0].finished=$true;$args[0].observations.realtimeWindows[1].stop.neighbourCommandsAfter[0].finished=$true}),
    @('ordinary unmounted command rerouted',$core,{$args[0].observations.unmountedControl.commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand'}),
    @('ordinary unmounted actor still mounted',$core,{$args[0].observations.unmountedControl.before.owners+=,(Copy-Value $owners[0])}),@('failed RT cleanup loses owner',$core,{$args[0].observations.localFailure.held.owners=@()}),
    @('native casting slot refunded',$casting,{$args[0].observations.casting.slotAvailableAfter=$true}),@('native heal omitted',$casting,{$args[0].observations.casting.effectsAfter.events=@($args[0].observations.casting.effectsAfter.events[0])}),
    @('native cast wrong actor',$casting,{$args[0].observations.casting.executorId=$ids[2]}),@('native cast missing prepared inventory',$casting,{$args[0].observations.castingInventory[0].preparedSpells=@()}),
    @('native cast gains other actor entitlement',$casting,{$args[0].observations.casting.neighbour.outcome.resourceOwnerId=$ids[0]}),
    @('Charge enabled after admission only',$charge,{$args[0].observations.chargeSettingAtAdmission=$false}),@('Charge not full native expenditure',$charge,{$args[0].observations.allocationTrace.events[1].state.move=0.0}),
    @('Charge path still outstanding',$charge,{$args[0].observations.realtimeWindows[0].settled.lease.forcedPathOutstanding=$true}),@('Charge buff still outstanding',$charge,{$args[0].observations.realtimeWindows[0].settled.lease.buffOutstanding=$true}),
    @('Charge neighbour paid standard cost',$charge,{$args[0].observations.realtimeWindows[0].after.actors[3].standard=3.0}),@('Charge cancellation already attacked',$charge,{$args[0].observations.realtimeWindows[1].effectsAfter.events=@([pscustomobject]@{kind='attack-before';actor=$ids[0];target=$target;opportunity=$false})}),
    @('Charge cancellation did not move',$charge,{$args[0].observations.realtimeWindows[1].stop.before.actors[1].position.x=0.0}),@('Charge stop not native charging',$charge,{$args[0].observations.realtimeWindows[1].stop.charge.mountCharging=$false}),
    @('RT cleanup incomplete',$charge,{$args[0].observations.cleanup.controlsSettled=$false}),@('RT breadth overclaimed',$core,{$args[0].observations.fullRealtimeCombatProof=$true})
)
foreach($case in $cases){$v=Copy-Value $case[1];& $case[2] $v;Reject $case[0] {Assert-KmcChunk7RealtimeEvidence (RequestFor $v) $v 'PASS'}}
Check 'RT partial failed evidence remains readable' {$v=Copy-Value $core;$v.status='FAIL';$v.errors=@('prepared native slot unavailable');$v.observations=[pscustomobject]@{};Assert-KmcChunk7RealtimeEvidence (RequestFor $v) $v 'FAIL'}
Reject 'RT partial FAIL cannot become PASS' {$v=Copy-Value $core;$v.status='FAIL';$v.errors=@('fixture failed');Assert-KmcChunk7RealtimeEvidence (RequestFor $v) $v 'PASS'}
Check 'RT exact rows and union' {$counts=@(14,9,10);$i=0;foreach($s in @($core.scenario,$casting.scenario,$charge.scenario)){if(@(Get-KmcChunk7RealtimeRows $s).Count-ne$counts[$i++]){throw 'RT case row count differs.'}};if(@(Get-KmcChunk7RealtimeRuntimeRows).Count-ne21){throw 'RT union differs.'}}
Reject 'foreign RT case' {Get-KmcChunk7RealtimeRows 'chunk7-two-pair-realtime-unbounded'}
$rtArtifactRoot=Join-Path $chunk7AnalysisRoot ('realtime-reader-artifact-fixture-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($rtArtifactRoot)|Out-Null
$rtArtifactPath=Join-Path $rtArtifactRoot 'chunk7-realtime-evidence.json';$rtRequest=RequestFor $core;$rtRequest.evidenceRoot=$rtArtifactRoot
[IO.File]::WriteAllText($rtArtifactPath,($core|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false))
$rtManifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-realtime-evidence.json';kind='chunk7-realtime-evidence';length=(Get-Item -LiteralPath $rtArtifactPath).Length;sha256=(Get-KmcSha256 $rtArtifactPath)})}
Check 'RT exact artifact bytes and manifest' {Assert-KmcChunk7RealtimeArtifact $rtRequest $rtManifest}
Reject 'RT artifact changed hash' {$m=Copy-Value $rtManifest;$m.artifacts[0].sha256='0'*64;Assert-KmcChunk7RealtimeArtifact $rtRequest $m}
Reject 'RT artifact duplicate role' {$m=Copy-Value $rtManifest;$m.artifacts+=,(Copy-Value $m.artifacts[0]);Assert-KmcChunk7RealtimeArtifact $rtRequest $m}
Reject 'RT missing PASS artifact' {Assert-KmcChunk7RealtimeArtifact $rtRequest ([pscustomobject]@{artifacts=@()})}
Check 'RT absent interrupted FAIL artifact' {Assert-KmcChunk7RealtimeArtifact $rtRequest ([pscustomobject]@{artifacts=@()}) 'FAIL'}
Reject 'TB request cannot carry RT artifact' {$r=Copy-Value $rtRequest;$r.scenario='chunk7-two-pair-combat-rider-first';Assert-KmcChunk7RealtimeArtifact $r $rtManifest}
Check 'TB no RT artifact remains separate' {$r=Copy-Value $rtRequest;$r.scenario='chunk7-two-pair-combat-rider-first';Assert-KmcChunk7RealtimeArtifact $r ([pscustomobject]@{artifacts=@()})}
# Native identities are retained RuntimeHelpers hashes: any nonzero int, often negative; zero is the null identity.
function Invert-Identities($v){if($v-is[pscustomobject]){foreach($p in $v.PSObject.Properties){if($p.Name-cin@('command','identity','shell')-and($p.Value-is[long]-or$p.Value-is[int])){$p.Value=-[long]$p.Value}else{Invert-Identities $p.Value}}}elseif($v-is[array]){foreach($x in $v){Invert-Identities $x}}}
Check 'RT accepts negative native identities' {$v=Copy-Value $core;Invert-Identities $v;if(@($v.observations.realtimeWindows)[0].a.command-ge0){throw 'Identity inversion did not apply.'};Assert-KmcChunk7RealtimeEvidence (RequestFor $v) $v 'PASS'}
Check 'RT refuses the null identity' {$v=Copy-Value $core;@($v.observations.realtimeWindows)[0].a.command=0;$message=$null;try{Assert-KmcChunk7RealtimeEvidence (RequestFor $v) $v 'PASS'}catch{$message=$_.Exception.Message};if($message-notmatch'RT primary exact caster/action-owner outcome differs'){throw ('Null command identity was not refused by the primary identity guard: '+$message)}}
Write-Host ('RESULT '+$passed+' passed; 0 failed (synthetic RT reader checks only)')
