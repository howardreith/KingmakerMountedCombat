# Sourced by the complete causal protocol suite; synthetic failures never qualify gameplay.
function New-UnactedProof {
    $p=New-CommandProof
    $p.identity.processObject=0;$p.identity.contextObject=0;$p.identity.abilityGuid='f053faad986631688defa003cd7bda0e'
    Put-Value $p contract 'unacted-native-obstruction-no-cost-or-transition'
    Put-Value $p.preClick.state relationshipState 'Unmounted'
    Put-Value $p.preClick.state ledger @{admittedMount=2;acceptedMount=1;acceptedDismount=1;forcedDetach=3;duplicateSuppressed=2;concurrentSuppressed=0;refusedVoluntary=1;admittedDismount=1}
    Put-Value $p ledgerDelta @{admittedMount=0;acceptedMount=0;acceptedDismount=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0;refusedVoluntary=0;admittedDismount=0}
    $samples=@();$n=0
    foreach($name in @('init','click-admission','move-slot-installation','approach-start','unacted-terminal')) {
        $state=Copy-Value $p.preClick.state;$last=$name -ceq 'unacted-terminal'
        if($last){foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift')){$state.$actor.$field=[Math]::Max(0.0,$state.$actor.$field-2.0)}}}
        $samples+=@{boundary=$name;identity=(Copy-Value $p.identity);acted=$false;finished=$last;result=$(if($last){'Interrupt'}else{'None'});processEnded=$null
            gameTicks=$(if($last){1020000000}else{1000000000});allocationSequence=0;state=$state}
    }
    $p.samples=$samples;$p.nativeResult='Interrupt';$p.resourceWindow.events=@();Copy-Value $p
}
function New-PathProof([long]$command=101,[string]$actor='rider',[bool]$failure=$true) {
    $hooks=@();foreach($token in @('060018A3','060018B9','0600184F','06001850','060027B2')){$hooks+=@{token=$token;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}}
    $names=@('command-bound','path-request','path-complete-before');if($failure){$names+='path-not-found'};$names+=@('path-complete-after','command-ended')
    $seq=0;$events=@();foreach($name in $names) {
        $end=$name -ceq 'command-ended'
        $events+=@{boundary=$name;sequence=(++$seq);gameTicks=$(if($end){1020000000}else{1000000000});frame=10;commandObject=$command;actorId=$actor
            pathObject=900;requestSequence=1;acted=($end -and -not $failure);finished=$end;result=$(if($end){if($failure){'Interrupt'}else{'Success'}}else{'None'})
            points=@(@{x=-3.5;y=0;z=0},@{x=3.5;y=0;z=0})}
    }
    Copy-Value @{complete=$true;errors=@();commandObject=$command;actorId=$actor;observerHooks=$hooks;events=$events}
}
function New-ObstructionCase {
    $p=New-UnactedProof
    $door=@{frame=12;open=$false;enabled=$false;cutEnabled=$true;clipTime=0.0;clipSpeed=-1.0;cutNeedsUpdate=$false;graphUpdatesQueued=$false;tileUpdateFrame=9;settledFrame=11}
    $open=Copy-Value $door;$open.open=$true;$open.cutEnabled=$false
    $case=@{contract='closed-native-door-unacted-mount';diagnosticStopIssued=$false;noResidue=$true;commandProof=$p;path=(New-PathProof)
        terminal=@{id=101;finished=$true;acted=$false;result='Interrupt'};after=@{relationshipState='Unmounted';generation=4}
        doorAtClick=(Copy-Value $door);doorAtTerminal=(Copy-Value $door);closedDoorObservations=1;start=@{isAdjacent=$false;centerDistance=7.0;horizontalDistance=7.0;legalAdjacencyEnvelope=2.9;riderCorpulence=0.5;horseCorpulence=1.4;riderPosition=@{x=-3.5;z=0};horsePosition=@{x=3.5;z=0}}}
    $fixture=@{contract='native-open-door-crossing-then-closed-cut';originalOpen=$true;originalEnabled=$false;originalCut=$false;disableNavmeshCutWhenOpen=$true
        restoration=@{exact=$true;ready=$true;state=$open};closedReady=(Copy-Value $door)
        center=@{x=0.0;y=0.0;z=0.0};near=@{x=-3.5;y=0.0;z=0.0};far=@{x=3.5;y=0.0;z=0.0}
        openHorseCrossing=@{command=@{id=401;finished=$true;result='Success';executor='mount'};door=$open;distanceToDestination=0.0;path=(New-PathProof 401 'mount' $false)}}
    Copy-Value @{case=$case;fixture=$fixture}
}
function Assert-Obstruction($value){Assert-KmcChunk6aObstruction $value.case $value.fixture}
Test-Case 'obstruction exact native path failure, terminal and no-cost reaction window' {Assert-Obstruction (New-ObstructionCase)}
Test-Case 'unacted contract cannot substitute a successful positive command' {$p=New-CommandProof;Reject {Assert-KmcChunk6aUnactedProof $p} 'contract'}
Test-Case 'unacted proof cannot qualify the positive contract' {$p=New-UnactedProof;$p.identityComplete=$false;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'identityComplete'}
foreach($field in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid','processObject','contextObject')) {
    Test-Case "unacted rejects mixed $field" {$c=New-ObstructionCase;$c.case.commandProof.samples[-1].identity.$field='other';Reject {Assert-Obstruction $c} 'Mixed unacted'}
}
foreach($actor in @('rider','mount')) {
    foreach($field in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')) {
        Test-Case "unacted rejects isolated $actor $field change with valid action proof" {
            $c=New-ObstructionCase;$c.case.commandProof.samples[-1].state.$actor.$field++
            Reject {Assert-Obstruction $c} 'reaction'
        }
    }
    foreach($field in @('standard','move','swift')) {
        Test-Case "unacted rejects extra $actor $field debt" {$c=New-ObstructionCase;$c.case.commandProof.samples[-1].state.$actor.$field++;Reject {Assert-Obstruction $c} 'debt'}
    }
    foreach($boundary in @('cost-before','cost-after','actor-cost-before','prepare-before','clear-before','combat-clear-before')) {
        Test-Case "unacted rejects $actor $boundary even with unchanged endpoints" {
            $c=New-ObstructionCase;$c.case.commandProof.resourceWindow.events+=@{boundary=$boundary;state=@{actor=$actor}}
            Reject {Assert-Obstruction $c} 'native cost'
        }
    }
}
Test-Case 'unacted rejects wrong rider baseline selection' {$c=New-ObstructionCase;$c.case.commandProof.preClick.state.selectedIds=@('mount');Reject {Assert-Obstruction $c} 'selection/baseline'}
Test-Case 'unacted rejects acted command before terminal' {$c=New-ObstructionCase;$c.case.commandProof.samples[3].acted=$true;Reject {Assert-Obstruction $c} 'acted flag'}
Test-Case 'unacted rejects ledger change despite carried nonzero counters' {$c=New-ObstructionCase;$c.case.commandProof.samples[-1].state.ledger.forcedDetach++;Reject {Assert-Obstruction $c} 'changed ledger'}
Test-Case 'unacted rejects fabricated Stop obstruction' {$c=New-ObstructionCase;$c.case.diagnosticStopIssued=$true;Reject {Assert-Obstruction $c} 'manufactured'}
Test-Case 'obstruction rejects foreign path callback' {$c=New-ObstructionCase;$c.case.path.events[3].pathObject=901;Reject {Assert-Obstruction $c} 'another request'}
Test-Case 'obstruction rejects wrong path command' {$c=New-ObstructionCase;$c.case.path.events[3].commandObject=102;Reject {Assert-Obstruction $c} 'command, actor or sequence'}
Test-Case 'obstruction rejects missing native failure callback' {$c=New-ObstructionCase;$c.case.path.events[3].boundary='path-observed';Reject {Assert-Obstruction $c} 'No native path failure'}
Test-Case 'obstruction rejects unfinished native terminal' {$c=New-ObstructionCase;$c.case.path.events[-1].finished=$false;Reject {Assert-Obstruction $c} 'unfinished'}
Test-Case 'obstruction rejects missing observer install' {$c=New-ObstructionCase;$c.case.path.observerHooks=@($c.case.path.observerHooks|Select-Object -Skip 1);Reject {Assert-Obstruction $c} 'hook installation'}
Test-Case 'obstruction rejects door state without finished animation' {$c=New-ObstructionCase;$c.case.doorAtClick.clipTime=0.5;Reject {Assert-Obstruction $c} 'readiness'}
Test-Case 'obstruction rejects pending navmesh tiles' {$c=New-ObstructionCase;$c.fixture.closedReady.graphUpdatesQueued=$true;Reject {Assert-Obstruction $c} 'readiness'}
Test-Case 'obstruction rejects same-frame closing readiness' {$c=New-ObstructionCase;$c.fixture.closedReady.settledFrame=12;Reject {Assert-Obstruction $c} 'subsequent settled frame'}
Test-Case 'obstruction rejects a route missing the open door' {$c=New-ObstructionCase;$c.fixture.openHorseCrossing.path.events[3].points[0].x=1;Reject {Assert-Obstruction $c} 'did not cross'}
Test-Case 'obstruction rejects unrestored fixture' {$c=New-ObstructionCase;$c.fixture.restoration.state.enabled=$true;Reject {Assert-Obstruction $c} 'exactly restored'}
function New-ObstructionEnvelope {
    $a=New-CompensationEnvelope;$v=New-ObstructionCase
    $a.observations.chunk6aCommandProofs=@($a.observations.chunk6aCommandProofs|Select-Object -First 2)
    Put-Value $a.observations chunk6aObstruction $v.case;Put-Value $a.observations chunk6aDoorFixture $v.fixture
    $a.rows=@($a.rows|Where-Object {$_.name -cnotlike 'CM02-adoption-*'})+@(@{name='CM02-obstruction';status='PASS'})
    Copy-Value $a
}
Test-Case 'isolated obstruction envelope retains two independent exploration windows' {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-obstruction'}) (New-ObstructionEnvelope) 'PASS'}
Test-Case 'isolated obstruction rejects carried positive Mount' {$a=New-ObstructionEnvelope;$a.rows+=@{name='CM02-approach-arrival';status='PASS'};Reject {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-obstruction'}) $a 'PASS'} 'cannot contain a positive Mount'}
Test-Case 'isolated obstruction rejects compensation in the same allocation' {$a=New-ObstructionEnvelope;$a.rows+=@{name='CM02-adoption-plan-invalidated';status='PASS'};Reject {Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-obstruction'}) $a 'PASS'} 'cannot inherit compensation'}
