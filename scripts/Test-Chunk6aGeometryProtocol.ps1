# Dot-sourced by the causal protocol runner; all fixtures are synthetic evidence.
function New-GeometryCase {
    $p=New-CommandProof
    Put-Value $p window 'geometry-change-mount'
    function Geometry([double]$rider,[double]$horse) {
        @{riderPosition=@{x=$rider;y=0.0;z=0.0};horsePosition=@{x=$horse;y=0.0;z=0.0};riderCorpulence=0.5;horseCorpulence=0.5
          centerDistance=$horse-$rider;horizontalDistance=$horse-$rider;legalAdjacencyEnvelope=2.5;isAdjacent=($horse-$rider -le 2.5)}
    }
    $start=Geometry 0 4;$triggerGeometry=Geometry 0.5 4;$arrival=Geometry 5 7
    $ledger=@{admittedMount=2;admittedDismount=1;acceptedMount=1;acceptedDismount=1;forcedDetach=2;duplicateSuppressed=3;concurrentSuppressed=4;refusedVoluntary=1}
    $endLedger=Copy-Value $ledger;$endLedger.admittedMount++;$endLedger.acceptedMount++
    Put-Value $p.preClick.state geometry (Copy-Value $start);Put-Value $p.preClick.state relationshipState 'Unmounted';Put-Value $p.preClick.state ledger (Copy-Value $ledger)
    foreach($sample in $p.samples) {
        Put-Value $sample.state geometry (Copy-Value $start);Put-Value $sample.state ledger (Copy-Value $ledger)
        Put-Value $sample.state relationshipState 'Unmounted'
        if($sample.boundary -cin @('process-binding','acted','cost-before','cost-after','deliver','relationship-transition')){$sample.gameTicks=1010000000}
        if($sample.boundary -ceq 'deliver'){$sample.state.geometry=Copy-Value $arrival}
        if($sample.boundary -ceq 'terminal'){$sample.state.generation=5;$sample.state.relationshipState='Mounted';$sample.state.ledger=$endLedger}
    }
    $delta=@{};foreach($key in $ledger.Keys){$delta[$key]=[long]$endLedger.$key-[long]$ledger.$key};Put-Value $p ledgerDelta $delta
    $aux=@{commandObject=401;actorId='mount';commandClass='Kingmaker.UnitLogic.Commands.UnitMoveTo';commandType='Move';createdByPlayer=$true;ignoreCooldown=$true;acted=$false;finished=$false;result='None'
        mountCommandObject=101;mountActed=$false;mountFinished=$false;gameTicks=1005000000;allocationSequence=1}
    $auxEnd=Copy-Value $aux;$auxEnd.acted=$true;$auxEnd.finished=$true;$auxEnd.result='Success'
    Put-Value $p auxiliaryGroundOrder @{contract='declared-rt-unit-move-to-native-no-write';admission=$aux;atMountTerminal=$auxEnd}
    Put-Value $p.resourceWindow auxiliaryGroundOrder @{contract='declared-rt-unit-move-to-native-no-write';pass=$true;commandObject=401;beforeCount=1;afterCount=1;expectedCountAtMountTerminal=1}
    $event=@{boundary='admission-after';command=401;commandActor='mount';commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo';actionType='Move';ignoreCooldown=$true;acted=$false;finished=$false;gameTicks=1005000000
        state=@{actor='mount';inCombat=$true;standard=4.5;move=3.5;swift=2.5;initiativeCooldown=0.0;initiativeOrder=12;reactionCooldown=0.0;reactions=1;reactionsPerRound=1}}
    $before=Copy-Value $event;$before.boundary='cost-before';$before.acted=$true;$before.gameTicks=1006000000
    foreach($field in @('standard','move','swift')){$before.state.$field-=0.1}
    $after=Copy-Value $before;$after.boundary='cost-after'
    $p.resourceWindow.events=@($event,$before,$after)+@($p.resourceWindow.events)
    $seq=0;foreach($e in $p.resourceWindow.events){Put-Value $e sequence (++$seq)};$p.samples[-1].allocationSequence=$seq
    $cmd=@{id=101;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';executor='rider';acted=$false;finished=$false}
    $ground=@{id=401;type='Kingmaker.UnitLogic.Commands.UnitMoveTo';executor='mount';acted=$false;finished=$false;result='None'}
    $groundEnd=Copy-Value $ground;$groundEnd.acted=$true;$groundEnd.finished=$true;$groundEnd.result='Success'
    $case=@{contract='native-target-motion-during-exact-mount-approach';noTransitionInFlight=$true;auxiliaryCommandId=401
        commandProof=$p;start=$start;arrival=$arrival;horseDisplacement=3.0;acceptedLawfully=$true;refusedWithCommittedCost=$false
        trigger=@{command=$cmd;moveSlot=(Copy-Value $cmd);gameTicks=1005000000;approachObserved=$true;riderReallyMoving=$true;geometry=$triggerGeometry;riderDisplacement=0.5}
        auxiliaryAdmission=$ground;auxiliaryTerminal=$groundEnd;after=@{relationshipState='Mounted';relationshipGeneration=5}}
    Copy-Value $case
}
function Assert-GeometryCase($c) {
    Assert-KmcRelationshipCommandProof $c.commandProof $true $false 0 $true $c.auxiliaryCommandId
    Assert-KmcChunk6aGeometryChange $c $c.commandProof
}
Test-Case 'geometry exact Mount and separately declared native no-write ground command' {Assert-GeometryCase (New-GeometryCase)}
Test-Case 'positive Mount still forbids a declared auxiliary command' {
    $c=New-GeometryCase;Reject {Assert-KmcRelationshipCommandProof $c.commandProof $true $false 0 $true} 'Auxiliary ground order is forbidden'
}
foreach($field in @('standard','move','swift','initiativeCooldown','initiativeOrder','reactionCooldown','reactions','reactionsPerRound')) {
    Test-Case "geometry ground callback rejects isolated $field write" {
        $c=New-GeometryCase;$c.commandProof.resourceWindow.events[2].state.$field++
        Reject {Assert-GeometryCase $c} "Geometry ground callback wrote $field"
    }
}
foreach($change in @(@('commandActor','rider'),@('commandType','Kingmaker.UnitLogic.Commands.UnitAttack'),@('actionType','Standard'),@('ignoreCooldown',$false),@('acted',$false))) {
    Test-Case "geometry rejects ground callback $($change[0]) mismatch" {
        $c=New-GeometryCase;$c.commandProof.resourceWindow.events[1].($change[0])=$change[1]
        Reject {Assert-GeometryCase $c} 'different actor, command or cost rule'
    }
}
Test-Case 'geometry rejects unobserved native ground admission' {$c=New-GeometryCase;$c.commandProof.resourceWindow.events[0].command=402;Reject {Assert-GeometryCase $c} 'omitted its exact native admission'}
Test-Case 'geometry rejects missing ground callback end' {$c=New-GeometryCase;$c.commandProof.resourceWindow.events=@($c.commandProof.resourceWindow.events|Where-Object { $_.boundary -cne 'cost-after' -or $_.command -ne 401 });Reject {Assert-GeometryCase $c} 'callback count differs'}
Test-Case 'geometry rejects extra ground callback' {$c=New-GeometryCase;$c.commandProof.resourceWindow.events+=@($c.commandProof.resourceWindow.events[1]);Reject {Assert-GeometryCase $c} 'callback count differs'}
Test-Case 'geometry rejects ground command changed at primary terminal' {$c=New-GeometryCase;$c.commandProof.auxiliaryGroundOrder.atMountTerminal.commandObject=402;Reject {Assert-GeometryCase $c} 'ground command identity differs'}
Test-Case 'geometry rejects ground admission after Mount acted' {$c=New-GeometryCase;$c.commandProof.auxiliaryGroundOrder.admission.gameTicks=1010000001;Reject {Assert-GeometryCase $c} 'did not precede the exact Mount acted'}
Test-Case 'geometry rejects ground command falsely marked unacted at primary terminal' {$c=New-GeometryCase;$c.commandProof.auxiliaryGroundOrder.atMountTerminal.acted=$false;Reject {Assert-GeometryCase $c} 'callback count differs'}
Test-Case 'geometry rejects trigger on another Mount' {$c=New-GeometryCase;$c.trigger.command.id=102;Reject {Assert-GeometryCase $c} 'exact unacted Mount'}
Test-Case 'geometry rejects already acted trigger' {$c=New-GeometryCase;$c.trigger.command.acted=$true;Reject {Assert-GeometryCase $c} 'exact unacted Mount'}
Test-Case 'geometry rejects Mount removed from Move slot' {$c=New-GeometryCase;$c.trigger.moveSlot.id=102;Reject {Assert-GeometryCase $c} 'exact unacted Mount'}
Test-Case 'geometry rejects missing actual movement' {$c=New-GeometryCase;$c.trigger.riderReallyMoving=$false;Reject {Assert-GeometryCase $c} 'lacks observed native approach'}
Test-Case 'geometry rejects a forged pre-attachment arrival' {$c=New-GeometryCase;$c.arrival.horsePosition.x=5;Reject {Assert-GeometryCase $c} 'not the exact pre-attachment delivery sample'}
Test-Case 'geometry rejects no actual target displacement' {
    $c=New-GeometryCase;$c.arrival.horsePosition.x=4;$c.arrival.riderPosition.x=2
    @($c.commandProof.samples|Where-Object boundary -CEQ 'deliver')[0].state.geometry=Copy-Value $c.arrival;$c.horseDisplacement=0
    Reject {Assert-GeometryCase $c} 'did not change during measured non-adjacent approach'
}
Test-Case 'geometry rejects forged adjacency verdict' {$c=New-GeometryCase;$c.trigger.geometry.isAdjacent=$true;Reject {Assert-GeometryCase $c} 'envelope and native adjacency differ'}
Test-Case 'geometry rejects generation increment without accepted transition' {$c=New-GeometryCase;$c.commandProof.samples[-1].state.generation=6;Reject {Assert-GeometryCase $c} 'did not revalidate lawfully'}
Test-Case 'geometry rejects cumulative ledger masquerading as window delta' {$c=New-GeometryCase;$c.commandProof.ledgerDelta.forcedDetach=2;Reject {Assert-GeometryCase $c} 'ledger delta differs'}
Test-Case 'geometry rejects command still in flight' {$c=New-GeometryCase;$c.noTransitionInFlight=$false;Reject {Assert-GeometryCase $c} 'terminal settlement differs'}
Test-Case 'geometry allows exact cost-retaining delivery refusal' {
    $c=New-GeometryCase;$c.acceptedLawfully=$false;$c.refusedWithCommittedCost=$true
    $c.commandProof.samples[-1].state.relationshipState='Unmounted';$c.commandProof.samples[-1].state.generation=4
    $c.commandProof.samples[-1].state.ledger.acceptedMount--;$c.commandProof.samples[-1].state.ledger.refusedVoluntary++
    $c.commandProof.ledgerDelta.acceptedMount=0;$c.commandProof.ledgerDelta.refusedVoluntary=1
    $c.after.relationshipState='Unmounted';$c.after.relationshipGeneration=4
    Assert-GeometryCase $c
}
function New-GeometryEnvelope {
    $a=New-CompensationEnvelope;$c=New-GeometryCase
    $id=$a.observations.chunk6aCommandProofs[2].identity
    $c.commandProof.identity.abilityGuid=$id.abilityGuid
    foreach($sample in $c.commandProof.samples){$sample.identity.abilityGuid=$id.abilityGuid}
    $a.observations.chunk6aCommandProofs[2]=$c.commandProof
    Put-Value $a.observations chunk6aGeometryChange $c
    $a.rows=@($a.rows|Where-Object { $_.name -cnotlike 'CM02-adoption-*' })+@(@{name='CM02-geometry-change';status='PASS'})
    Copy-Value $a
}
Test-Case 'isolated geometry envelope requires three distinct exact windows' {
    Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-geometry-change'}) (New-GeometryEnvelope) 'PASS'
}
Test-Case 'isolated geometry rejects unrelated positive qualification' {
    $a=New-GeometryEnvelope;$a.rows+=@{name='CM02-approach-arrival';status='PASS'}
    Reject {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-geometry-change'}) $a 'PASS'} 'cannot contain a positive Mount'
}
Test-Case 'isolated geometry rejects carried compensation' {
    $a=New-GeometryEnvelope;$a.rows+=@{name='CM02-adoption-plan-invalidated';status='PASS'}
    Reject {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-geometry-change'}) $a 'PASS'} 'cannot inherit compensation'
}

Test-Case 'geometry rejects admission attributed to wrong actor' {$c=New-GeometryCase;$c.commandProof.resourceWindow.events[0].state.actor='rider';Reject {Assert-GeometryCase $c} 'omitted its exact native admission'}
Test-Case 'geometry accepts a ground order interrupted unacted with no cost callbacks' {
    $c=New-GeometryCase;$p=$c.commandProof
    $p.auxiliaryGroundOrder.atMountTerminal.acted=$false;$p.auxiliaryGroundOrder.atMountTerminal.result='Interrupt'
    $c.auxiliaryTerminal.acted=$false;$c.auxiliaryTerminal.result='Interrupt'
    $p.resourceWindow.auxiliaryGroundOrder.beforeCount=0;$p.resourceWindow.auxiliaryGroundOrder.afterCount=0;$p.resourceWindow.auxiliaryGroundOrder.expectedCountAtMountTerminal=0
    $p.resourceWindow.events=@($p.resourceWindow.events|Where-Object {$_.command -ne 401 -or $_.boundary -ceq 'admission-after'})
    $seq=0;foreach($e in $p.resourceWindow.events){$e.sequence=++$seq};$p.samples[-1].allocationSequence=$seq
    Assert-GeometryCase $c
}
