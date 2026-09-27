[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$script:passed=0
function Copy-Value($value){$value|ConvertTo-Json -Depth 80 -Compress|ConvertFrom-Json}
function Test-Case([string]$name,[scriptblock]$body){& $body;$script:passed++;Write-Host "PASS $name"}
function Reject([scriptblock]$body,[string]$reason){
    $caught=$false
    try{& $body}catch{if($_.Exception.Message -notlike ('*'+$reason+'*')){throw};$caught=$true}
    if(-not $caught){throw "Validator accepted corrupt evidence: $reason"}
}
function Put-Value($object,$name,$value){if($object -is [System.Collections.IDictionary]){$object[$name]=$value}else{$object|Add-Member -NotePropertyName $name -NotePropertyValue $value -Force}}
function New-CommandProof([bool]$combat=$true,[bool]$tb=$false,[int]$partner=0){
    $identity=@{commandObject=101;controlIdentity='shell-1';processObject=201;contextObject=301
        casterId='rider';targetId='mount';generationAtInit=4;commandType='Move';abilityGuid='mount-guid'}
    $state=@{generation=4;selectedIds=@('rider');rider=@{standard=4.0;move=1.0;swift=3.0;initiative=0;initiativeCooldown=0.0;initiativeOrder=12;reactionCooldown=0.0;reactions=1;reactionsPerRound=1};mount=@{standard=5.0;move=4.0;swift=3.0;initiative=0;initiativeCooldown=0.0;initiativeOrder=12;reactionCooldown=0.0;reactions=1;reactionsPerRound=1}}
    $pre=@{gameTicks=1000000000;state=$state}
    $before=@{boundary='cost-before';command=101;commandActor='rider';actionType='Move';acted=$true;timeSinceStart=0.25;gameTicks=1010000000
        state=@{actor='rider';inCombat=$combat;standard=4.0;move=1.0;swift=3.0}}
    if($combat -and -not $tb){$before.state.standard=3.0;$before.state.move=0.0;$before.state.swift=2.0}
    $after=Copy-Value $before;$after.boundary='cost-after'
    if($combat){$after.state.move=if($tb){4.0}else{2.75}}
    $end=Copy-Value $state
    if($combat -and -not $tb){foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift')){$end.$actor.$field=[Math]::Max(0,$end.$actor.$field-2)}}}
    $end.rider.move=if(-not $combat){1.0}elseif($tb){4.0}else{1.75}
    $samples=@()
    foreach($name in @('init','click-admission','move-slot-installation','approach-start','process-binding','acted','cost-before','cost-after','deliver','relationship-transition','terminal')){
        $id=Copy-Value $identity
        if($name -cin @('init','click-admission','move-slot-installation','approach-start')){$id.processObject=0;$id.contextObject=0}
        $samples+=@{boundary=$name;identity=$id;acted=($name -cin @('acted','cost-before','cost-after','deliver','relationship-transition','terminal'))
            nativeProcessBinding=($name -ceq 'process-binding');deliveryContext=$(if($name -ceq 'deliver'){301}else{0})
            gameTicks=$(if($name -ceq 'terminal'){1020000000}else{1000000000});state=$(if($name -ceq 'terminal'){$end}else{$state})
            finished=($name -ceq 'terminal');processEnded=($name -ceq 'terminal');result='Success'}
    }
    $events=@($before)
    if($combat -and $tb){$nb=Copy-Value $before;$nb.boundary='actor-cost-before';$na=Copy-Value $after;$na.boundary='actor-cost-after';$events+=@($nb,$na)}
    $events+=@($after)
    if($partner -eq 1){
        foreach($name in @('prepare-before','clear-before','clear-after','prepare-after')){
            $events+=@{boundary=$name;state=@{actor='mount';standard=0.0;move=0.0;swift=0.0};preparingTurn=12;gameTicks=1010000000}
        }
        foreach($field in @('standard','move','swift')){$end.mount.$field=0.0}
    }
    $sequence=0
    foreach($event in $events){
        $sequence++;Put-Value $event sequence $sequence
        foreach($field in @('initiativeCooldown','initiativeOrder','reactionCooldown','reactions','reactionsPerRound')){
            Put-Value $event.state $field $state[$event.state.actor][$field]
        }
    }
    $pre['allocationSequence']=0
    foreach($sample in $samples){$sample['allocationSequence']=if($sample.boundary -ceq 'terminal'){$sequence}else{0}}
    Copy-Value @{pass=$true;identityComplete=$true;sameCommandAtEveryBoundary=$true;exactActedObserved=$true
        nativeTerminal=$true;traceComplete=$true;initCount=1;errors=@();nativeResult='Success';identity=$identity;mountId='mount'
        preClick=$pre;samples=$samples;resourceWindow=@{reactionResources=@{contract='native-time-and-declared-partner-preparation-only';pass=$true};inCombat=$combat;turnBased=$tb;events=$events}}
}
Test-Case 'one RT command with normal decay' {Assert-KmcRelationshipCommandProof (New-CommandProof) $true $false 0 $true}
Test-Case 'one TB command with exact endpoints' {Assert-KmcRelationshipCommandProof (New-CommandProof $true $true) $true $true 0 $true}
Test-Case 'one declared pending-partner grant' {Assert-KmcRelationshipCommandProof (New-CommandProof $true $true 1) $true $true 1 $true}
Test-Case 'exploration callback writes no native cost' {Assert-KmcRelationshipCommandProof (New-CommandProof $false) $false $false 0 $false}
foreach($field in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid')){
    Test-Case "reject mixed $field" {$p=New-CommandProof;$p.samples[8].identity.$field='different';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Mixed causal identity'}
}
foreach($field in @('processObject','contextObject')){
    Test-Case "reject mixed $field" {$p=New-CommandProof;$p.samples[8].identity.$field=999;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Mixed process/context'}
}
Test-Case 'reject cooldown standing in for missing acted observation' {$p=New-CommandProof;$p.samples=@($p.samples|Where-Object boundary -CNE 'acted');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Missing or repeated exact acted'}
Test-Case 'reject false acted with correct charged endpoint' {$p=New-CommandProof;$p.samples[5].acted=$false;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Native acted was not observed'}
Test-Case 'reject another Move cost' {$p=New-CommandProof;$p.resourceWindow.events+=@($p.resourceWindow.events[0]);Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'callback count'}
foreach($field in @('Standard','Swift')){
    Test-Case "reject rider $field callback" {$p=New-CommandProof;$p.resourceWindow.events[0].actionType=$field;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
}
Test-Case 'reject mount native cost' {$p=New-CommandProof;$p.resourceWindow.events[0].commandActor='mount';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
Test-Case 'reject unobserved native process terminal' {$p=New-CommandProof;$p.samples[-1].processEnded=$false;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'terminal state'}
Test-Case 'reject native clear' {$p=New-CommandProof;$p.resourceWindow.events+=@{boundary='clear-before';state=@{actor='rider'};preparingTurn=0};Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Unexpected preparation'}
Test-Case 'reject combat reset' {$p=New-CommandProof;$p.resourceWindow.events+=@{boundary='combat-clear-before';state=@{actor='rider'}};Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Unexpected preparation'}
Test-Case 'reject principal preparation' {$p=New-CommandProof $true $true 1;$p.resourceWindow.events[-4].state.actor='rider';Reject {Assert-KmcRelationshipCommandProof $p $true $true 1 $true} 'declared partner grant'}
Test-Case 'reject RT refund below native decay' {$p=New-CommandProof;$p.samples[-1].state.rider.standard=0;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'refunded or added rider standard'}
Test-Case 'reject RT extra charge even below pre-click endpoint' {$p=New-CommandProof;$p.samples[-1].state.rider.standard=3;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'refunded or added rider standard'}
Test-Case 'reject TB second cost' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=7;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject TB refund' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=0;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject TB endpoint drift beyond exact tolerance' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=4.001;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject exploration action write' {$p=New-CommandProof $false;$p.resourceWindow.events[-1].state.move=4;Reject {Assert-KmcRelationshipCommandProof $p $false $false 0 $false} 'native Move cost differs'}
Test-Case 'reject lost pre-click baseline' {$p=New-CommandProof;$p.preClick=$null;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'pre-click baseline'}
Test-Case 'reject debt write before acted callback' {$p=New-CommandProof;$p.resourceWindow.events[0].state.move=1;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'before the exact acted'}
Test-Case 'reject wrong single rider selection' {$p=New-CommandProof;$p.preClick.state.selectedIds=@('mount');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'exact single rider'}
Test-Case 'reject multiple selection' {$p=New-CommandProof;$p.preClick.state.selectedIds=@('rider','mount');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'exact single rider'}
Test-Case 'reject generation changed before Init' {$p=New-CommandProof;$p.preClick.state.generation=3;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'generation differs'}
Test-Case 'reject mount cost charged using rider command' {$p=New-CommandProof;$p.resourceWindow.events[0].state.actor='mount';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
function New-CompensationEnvelope {
    $proofs=@()
    foreach($name in @('exploration-mount','exploration-dismount','compensation')) {
        $p=New-CommandProof ($name -ceq 'compensation')
        $p|Add-Member -NotePropertyName window -NotePropertyValue $name
        $id=$p.identity;$id.abilityGuid=if($name -ceq 'exploration-dismount'){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'}
        if($name -ceq 'exploration-dismount'){$id.targetId='rider'}
        foreach($sample in $p.samples){$sample.identity.abilityGuid=$id.abilityGuid;$sample.identity.targetId=$id.targetId}
        $proofs+=@($p)
    }
    Copy-Value @{rows=@(@('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing','CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases')|ForEach-Object {@{name=$_;status='PASS'}})
        observations=@{chunk6aCombatMount=@();chunk6aCommandProofs=$proofs;phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}}}
}
$request=[pscustomobject]@{scenario='chunk6a-adoption-compensation-rt'}
Test-Case 'isolated compensation envelope requires all three exact windows' {Assert-KmcChunk6aCombatMountEvidence $request (New-CompensationEnvelope) 'PASS'}
Test-Case 'reject positive Mount in compensation allocation' {$a=New-CompensationEnvelope;$a.rows+=@{name='CM02-approach-arrival';status='PASS'};Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'cannot contain a positive Mount'}
Test-Case 'reject missing exploration Mount window' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs=@($a.observations.chunk6aCommandProofs|Where-Object window -CNE 'exploration-mount');Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'window count differs'}
Test-Case 'reject shared exploration Mount and Dismount evidence' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs[1]=$a.observations.chunk6aCommandProofs[0];Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'exactly one exploration-mount'}
Test-Case 'reject mislabeled native ability window' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs[0].identity.abilityGuid='foreign';Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'wrong ability or target'}

# A real ground order enters Acting on the SAME rider turn. Its existing Move
# debt is carried into the separately validated Mount command window.
function New-ActingSetupCase {
    $p=New-CommandProof $true $true
    foreach($event in $p.resourceWindow.events){Put-Value $event turn 501}
    $before=@{currentTurnActor='rider';currentTurnStatus='Preparing';turnBased=$true;relationshipState='Unmounted';relationshipGeneration=4
        dispatchAccepted=2;dispatchRejected=1;relationshipShells=2;transitionLedger='carried exploration counts'
        acceptedMountCount=1;acceptedDismountCount=1;forcedDetachCount=0;adoptionCount=0
        rider=@{standard=4.0;move=0.0;swift=3.0;nativePrepareCount=1};mount=@{standard=5.0;move=4.0;swift=3.0;nativePrepareCount=1}}
    $after=Copy-Value $before;$after.currentTurnStatus='Acting';$after.rider.move=1.0
    $command=@{id=401;type='Kingmaker.UnitLogic.Commands.UnitMoveTo';executor='rider';finished=$false;result='None'}
    $terminal=Copy-Value $command;$terminal.finished=$true;$terminal.result='Success'
    $setup=@{contract='native-rider-ground-order-before-mount-baseline';beforeTurnObject=501;afterTurnObject=501;sameNativeTurn=$true
        before=$before;after=$after;admittedCommand=$command;terminalCommand=$terminal;createdByPlayer=$true
        displacement=0.6;residual=0.01;placementTolerance=0.06;traceComplete=$true
        events=@(@{boundary='admission-after';turn=501;command=401;commandActor='rider'})}
    Copy-Value @{setup=$setup;proof=$p}
}
Test-Case 'native TB setup carries debt on the exact rider turn into a valid Mount proof' {
    $c=New-ActingSetupCase
    Assert-KmcRelationshipCommandProof $c.proof $true $true 0 $true
    Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof
}
Test-Case 'reject missing TB native Acting setup' {$c=New-ActingSetupCase;Reject {Assert-KmcChunk6aNativeActingSetup $null $c.proof} 'omitted its native Acting setup'}
Test-Case 'reject replacing the Preparing rider turn' {$c=New-ActingSetupCase;$c.setup.afterTurnObject=502;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'exact rider Preparing-to-Acting turn'}
Test-Case 'reject a different native ground command terminal' {$c=New-ActingSetupCase;$c.setup.terminalCommand.id=402;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'exact successful native ground command'}
Test-Case 'reject terminal ground failure' {$c=New-ActingSetupCase;$c.setup.terminalCommand.result='Fail';Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'exact successful native ground command'}
Test-Case 'reject increased setup arrival tolerance' {$c=New-ActingSetupCase;$c.setup.placementTolerance=0.1;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'native ground destination'}
Test-Case 'reject setup without real displacement' {$c=New-ActingSetupCase;$c.setup.displacement=0;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'native ground destination'}
Test-Case 'reject setup relationship generation change' {$c=New-ActingSetupCase;$c.setup.after.relationshipGeneration=5;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'changed relationship generation'}
Test-Case 'reject setup hidden relationship shell' {$c=New-ActingSetupCase;$c.setup.after.relationshipShells=3;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'relationship counter relationshipShells'}
foreach($actor in @('rider','mount')) {
    foreach($field in @('standard','swift')) {
        Test-Case "reject setup $actor $field charge with valid Mount proof" {
            $c=New-ActingSetupCase;$c.setup.after.$actor.$field++
            Assert-KmcRelationshipCommandProof $c.proof $true $true 0 $true
            Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} "unrelated or refunded $actor $field"
        }
    }
}
Test-Case 'reject setup mount Move charge' {$c=New-ActingSetupCase;$c.setup.after.mount.move++;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'unrelated or refunded mount move'}
Test-Case 'reject clearing carried setup Move debt before Mount' {$c=New-ActingSetupCase;$c.setup.after.rider.move=2;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'carry setup rider move debt'}
Test-Case 'reject rider preparation during setup' {$c=New-ActingSetupCase;$c.setup.after.rider.nativePrepareCount++;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'added native preparation'}
Test-Case 'reject incomplete setup allocation trace' {$c=New-ActingSetupCase;$c.setup.traceComplete=$false;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'complete native allocation trace'}
Test-Case 'reject ground command without its exact admission event' {$c=New-ActingSetupCase;$c.setup.events[0].command=402;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'exact native ground admission event'}
Test-Case 'reject intermediate preparation despite identical endpoints' {$c=New-ActingSetupCase;$c.setup.events+=@{boundary='prepare-before';turn=501};Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'preparation, clear or turn end'}
Test-Case 'reject intermediate turn replacement despite identical endpoints' {$c=New-ActingSetupCase;$c.setup.events[0].turn=502;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'trace changed native turn'}
Test-Case 'reject Mount cost from a later native turn' {$c=New-ActingSetupCase;$c.proof.resourceWindow.events[0].turn=502;Reject {Assert-KmcChunk6aNativeActingSetup $c.setup $c.proof} 'another native setup turn'}

function New-TbCompensationEnvelope {
    $a=New-CompensationEnvelope;$c=New-ActingSetupCase
    $c.proof.identity.abilityGuid='f053faad986631688defa003cd7bda0e'
    foreach($sample in $c.proof.samples){$sample.identity.abilityGuid=$c.proof.identity.abilityGuid}
    Put-Value $c.proof window 'compensation';$a.observations.chunk6aCommandProofs[2]=$c.proof
    Put-Value $a.observations chunk6aNativeActingSetup $c.setup
    $a.rows+=@{name='CM01-combat-mount-preparing-refused';status='PASS'}
    return $a
}
Test-Case 'TB compensation envelope requires exact native setup and all command windows' {
    Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-adoption-compensation-tb'}) (New-TbCompensationEnvelope) 'PASS'
}
Test-Case 'TB envelope rejects omitted setup despite valid command windows' {
    $a=New-TbCompensationEnvelope;$a.observations.chunk6aNativeActingSetup=$null
    Reject {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-adoption-compensation-tb'}) $a 'PASS'} 'omitted its native Acting setup'
}

# Leave action endpoints, cost callbacks and causal identity valid; corrupt only reactions.
foreach($mode in @('exploration','rt','tb')) {
    foreach($actor in @('rider','mount')) {
        foreach($change in @(@('reactions',0),@('reactions',2),@('reactionCooldown',1.0),@('initiativeCooldown',1.0),@('initiativeOrder',13))) {
            Test-Case "reject $mode $actor reaction-only $($change[0])=$($change[1])" {
                $combat=$mode -cne 'exploration';$tb=$mode -ceq 'tb';$p=New-CommandProof $combat $tb
                $p.samples[-1].state.$actor.($change[0])=$change[1]
                Reject {Assert-KmcRelationshipCommandProof $p $combat $tb 0 $false} 'reaction'
            }
        }
    }
}
function Set-ReactionBaseline($p,$actor,$field,$value) {
    $p.preClick.state.$actor.$field=$value
    foreach($sample in $p.samples){$sample.state.$actor.$field=$value}
    foreach($event in $p.resourceWindow.events){if($event.state.actor -ceq $actor){$event.state.$field=$value}}
}
function Add-ReactionTick($p,$actor,[double]$delta,[bool]$tb=$false,[bool]$passing=$false,[bool]$waiting=$false) {
    $n=@($p.resourceWindow.events).Count
    $before=@{boundary='cooldown-tick-before';sequence=($n+1);gameTicks=1020000000;gameDeltaTime=$delta;nativeTurnBased=$tb;nativePassing=$passing;nativeSurprised=$false;state=(Copy-Value $p.preClick.state.$actor)}
    $before.state|Add-Member -NotePropertyName actor -NotePropertyValue $actor
    $before.state|Add-Member -NotePropertyName inCombat -NotePropertyValue $p.resourceWindow.inCombat
    $before.state|Add-Member -NotePropertyName waitingInitiative -NotePropertyValue $waiting
    $after=Copy-Value $before;$after.boundary='cooldown-tick-after';$after.sequence=$n+2
    $p.resourceWindow.events+=@($before,$after);$p.samples[-1].allocationSequence=$n+2
}
Test-Case 'observed RT timer expiry lawfully refreshes discrete reactions' {
    $p=New-CommandProof;Set-ReactionBaseline $p rider reactions 0;Set-ReactionBaseline $p rider reactionCooldown 0.25
    Add-ReactionTick $p rider 0.25
    $p.resourceWindow.events[-1].state.reactions=1;$p.resourceWindow.events[-1].state.reactionCooldown=0
    $p.samples[-1].state.rider.reactions=1;$p.samples[-1].state.rider.reactionCooldown=0
    Assert-KmcRelationshipCommandProof $p $true $false 0 $true
}
Test-Case 'reject reaction refresh without the exact native tick' {
    $p=New-CommandProof;Set-ReactionBaseline $p rider reactions 0
    $p.samples[-1].state.rider.reactions=1
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'reaction allowance'
}
Test-Case 'reject AoO refund below permitted native decay' {
    $p=New-CommandProof;Set-ReactionBaseline $p mount reactionCooldown 3.0
    Add-ReactionTick $p mount 0.25;$p.resourceWindow.events[-1].state.reactionCooldown=0;$p.samples[-1].state.mount.reactionCooldown=0
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'reaction effect'
}
Test-Case 'observed initiative cooldown decay preserves initiative ordering' {
    $p=New-CommandProof;Set-ReactionBaseline $p rider initiativeCooldown 1.0
    Add-ReactionTick $p rider 0.25 $false $false $true
    $p.resourceWindow.events[-1].state.initiativeCooldown=0.75;$p.samples[-1].state.rider.initiativeCooldown=0.75
    Assert-KmcRelationshipCommandProof $p $true $false 0 $true
}
Test-Case 'TB passing cooldown expiry does not refresh reactions' {
    $p=New-CommandProof $true $true;Set-ReactionBaseline $p rider reactions 0;Set-ReactionBaseline $p rider reactionCooldown 0.25
    Add-ReactionTick $p rider 0.25 $true $true
    $p.resourceWindow.events[-1].state.reactionCooldown=0;$p.samples[-1].state.rider.reactionCooldown=0
    Assert-KmcRelationshipCommandProof $p $true $true 0 $true
    $p.resourceWindow.events[-1].state.reactions=1;$p.samples[-1].state.rider.reactions=1
    Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'reaction effect'
}
Test-Case 'reject a missing native reaction callback end' {
    $p=New-CommandProof;Add-ReactionTick $p rider 0.25
    $p.resourceWindow.events=@($p.resourceWindow.events|Where-Object boundary -CNE 'cooldown-tick-after')
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Reaction terminal sequence'
}
Test-Case 'reject consumed then restored reactions during delivery' {
    $p=New-CommandProof;$p.samples[8].state.rider.reactions=0
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'reaction allowance'
}
Test-Case 'declared partner preparation restores spent allowance only once' {
    $p=New-CommandProof $true $true 1;Set-ReactionBaseline $p mount reactions 0
    $p.resourceWindow.events[-1].state.reactions=1;$p.samples[-1].state.mount.reactions=1
    Assert-KmcRelationshipCommandProof $p $true $true 1 $true
    $p.resourceWindow.events[-1].state.reactions=2;$p.samples[-1].state.mount.reactions=2
    Reject {Assert-KmcRelationshipCommandProof $p $true $true 1 $true} 'reaction effect'
}
Test-Case 'reject reordered native reaction event evidence' {
    $p=New-CommandProof;$p.resourceWindow.events[0].sequence=2
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Reaction event sequence'
}
Test-Case 'reject fractional reaction allowance' {
    $p=New-CommandProof;$p.samples[-1].state.rider.reactions=1.1
    Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Reaction discrete field'
}
# Replay the immutable native trace that exposed the legacy CM03 timer assertion.
# This verifies instrumentation/validator behavior only; the original overall FAIL
# and every original PASS/FAIL row stay unchanged and unqualified.
$replayPath=Join-Path $PSScriptRoot '../../../runtime-evidence/c6a-cleanup-a-approach/phase3d-horse-scenario-evidence.json'
if((Get-FileHash -Algorithm SHA256 -LiteralPath $replayPath).Hash.ToLowerInvariant() -cne 'e23573b6a1056c741c77c98fe0c3489828cb0f9e7b09e48df11ec1b6ba4fe015'){throw 'Immutable native clock regression trace changed.'}
$replay=Get-Content -Raw -LiteralPath $replayPath|ConvertFrom-Json
foreach($nativeProof in $replay.observations.chunk6aCommandProofs){
    Test-Case "immutable Unity observer and resource replay $($nativeProof.window)" {
        $combat=$nativeProof.window -ceq 'positive-mount'
        Assert-KmcRelationshipCommandProof $nativeProof $combat $false 0 $combat
    }
}
foreach($actor in @('rider','mount')){
    foreach($change in @(@('reactions',0),@('reactions',2),@('reactionCooldown',1.0),@('initiativeOrder',99))){
        Test-Case "native trace rejects isolated $actor $($change[0])=$($change[1])" {
            $p=Copy-Value @($replay.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
            $p.samples[-1].state.$actor.($change[0])=$change[1]
            Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'reaction'
        }
    }
}
Write-Host "CHUNK6A CAUSAL PROTOCOL PASS=$script:passed FAIL=0 (synthetic validator tests; no runtime qualification)"
