[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ActorAllocationEvidence.ps1')
# These synthetic envelopes test rejection rules, not native gameplay.
function New-ConditionEnvelope([switch]$NativeMountSuccessor) {
    $ledger=New-Object Collections.ArrayList
    $clockState=@{offset=0L}
    function Event([string]$boundary,[string]$actor,[string]$identity) {
        $n=$ledger.Count+1
        $e=@{sequence=$n;boundary=$boundary;frame=$n;gameTicks=$clockState.offset+100*$n;round=1;activationIdentity=$identity;state=@{actor=$actor}
            command=0;commandActor=$null;commandType=$null;ignoreCooldown=$false}
        [void]$ledger.Add($e); return $e
    }
    function Sample([string]$kind,[string]$id,[double]$rs,[double]$rm,[double]$ms,[double]$mm,[string]$current='rider') {
        $e=Event $kind 'rider' $id
        $s=@{kind=$kind;traceSequence=$e.sequence;frame=$e.frame;gameTicks=$e.gameTicks;identity=$id;currentActor=$current
            rider=@{actor='rider';standard=$rs;move=$rm};mount=@{actor='mount';standard=$ms;move=$mm}}
        foreach($actor in @('rider','mount')) {
            $s[$actor+'Clears']=@($ledger|Where-Object {$_.boundary -ceq 'clear-after' -and $_.state.actor -ceq $actor}).Count
            $s[$actor+'Effects']=@($ledger|Where-Object {$_.boundary -ceq 'round-state-after' -and $_.state.actor -ceq $actor}).Count
        }
        return $s
    }
    $cases=@()
    for($i=0;$i -lt 2;$i++) {
        $id=('a'*31)+$i+':1'
        $before=Sample 'before-encounter' '' 0 0 0 0
        foreach($actor in @('rider','mount')) {
            foreach($kind in @('prepare-before','clear-after','round-state-after')) { [void](Event $kind $actor $id) }
        }
        $stimulusEvent=Event 'native-condition-fact-stimulus' 'mount' $id
        $type=if($i-eq0){'Kingmaker.UnitLogic.Commands.UnitDoNothing'}else{'Kingmaker.UnitLogic.Commands.UnitSelfHarm'}
        $commandId=900+$i
        $adapterBefore=Event 'paired-confusion-adapter-before' 'mount' $id
        $adapterBefore.state.preparingPairedActor=$true
        $admit=Event 'admission-after' 'mount' $id
        $admit.command=$commandId;$admit.commandActor='mount';$admit.commandType=$type
        $adapterAfter=Event 'paired-confusion-adapter-after' 'mount' $id
        $adapterAfter.state.preparingPairedActor=$true;$adapterAfter.state.confusionPart=$true;$adapterAfter.state.confusionCommand=$commandId
        foreach($actor in @('rider','mount')) { [void](Event 'prepare-after' $actor $id) }
        $admission=Sample 'condition-admission' $id 0 0 0 0
        $terminalStandard=6+6*$i
        foreach($kind in @('actor-cost-before','actor-cost-after')) {
            $cost=Event $kind 'mount' $id
            $cost.command=$commandId; $cost.commandType=$type
            $cost.state.standard=if($kind -ceq 'actor-cost-before'){6}else{$terminalStandard}
            $cost.state.move=3
        }
        $ended=Sample 'condition-ended' $id 0 0 $terminalStandard 3
        $split=Sample 'condition-split' $id 0 0 $terminalStandard 3
        $beforeEnd=Sample 'before-end' $id 6 0 $terminalStandard 3
        foreach($actor in @('rider','mount')) { foreach($kind in @('turn-end-before','turn-end-after')) {
            $end=Event $kind $actor $id
            $end.state.standard=if($actor -ceq 'mount' -and $kind -ceq 'turn-end-before'){$terminalStandard}else{6}
            $end.state.move=3
        } }
        if($NativeMountSuccessor) {
            $clockState.offset+=6*[TimeSpan]::TicksPerSecond
            foreach($kind in @('prepare-before','clear-after','round-state-after','prepare-after')) {
                $renewal=Event $kind 'mount' $id;$renewal.round=2
                $renewal.state.standard=0;$renewal.state.move=0;$renewal.state.timeToNextNativeTurn=0
            }
        }
        $nextActor=if($NativeMountSuccessor){'mount'}else{'friend'}
        $nextRound=if($NativeMountSuccessor){2}else{1}
        $afterEnd=Sample 'after-end' $id 6 3 6 3 $nextActor
        $command=@{id=$commandId;type=$type;executor='mount';finished=$true;result='Success'}
        $cases+=@{name=$(if($i-eq0){'mount-do-nothing'}else{'mount-self-harm'});passed=$true;outsideCombat=$true;mountedBeforeCombat=$true;activation=$id
            stimulus=@{inputKind='native-round-fact-condition-stimulus';actor='mount';activation=$id;conditionApplications=1;choiceOverrides=1;choice=30+30*$i
                nativeSelfDamageRules=$i;nativeSelfDamage=3*$i;ownedConditionRestored=$true;before=@{standard=0;move=0};frame=$stimulusEvent.frame;gameTicks=$stimulusEvent.gameTicks
                conditionActiveAfterApplication=$true;conditionImmuneAfterApplication=$false
                nativePartAbsentBefore=$true;nativePartCreated=$true;nativePartRemoved=$true;directControlBefore=$true
                directControlAfter=$true;directControlRestored=$true;cleanupResourcesUnchanged=$true
                nativeFactVisits=1;preparingAtFact=$true;factBinding=@{actor='mount';activeFactCount=1;actionCount=2;activeComponent=11;templateComponent=12;exactActionBound=$true}}
            beforeEncounter=$before;admission=$admission;ended=$ended;forcedSplit=$split;beforeEndInput=$beforeEnd;afterEnd=$afterEnd
            samePrincipal=$true;mountEnded=$true;riderEnded=$false;relationshipAfter='Unmounted';commandAtAdmission=$command;commandAtEnd=$command
            operations=@(@{actor='rider';full=$false;hoverPure=$true;clicked=$true;nativeFull=$false;nativeSinglePrimary=$false;nativePlan=1;completed=1;nativeRules=1
                command=@{id=1200+$i;type='Kingmaker.UnitLogic.Commands.UnitAttack';result='Success'};before=$split;after=$beforeEnd
                traceCase='paired-rider-single';verified=$true;ruleObservationFrame=$beforeEnd.frame;nativeRecovery=$null})
            nextActor=$nextActor;nextRound=$nextRound;round=1;visits=@(@{actor='rider';round=1},@{actor=$nextActor;round=$nextRound})}
    }
    $resets=@()
    for($i=0;$i-lt2;$i++) {
        $reset=@{nativeTb=$false;controllerInitialized=$false;actorsIdle=$true;passed=$true}
        foreach($role in @('rider','mount')) {
            foreach($when in @('Before','After')) {
                $reset[$role+$when]=@{states=@(@{unitId=$role;rawAiBefore=$true;effectiveAiBefore=$true
                    commandsEmptyDuring=$true;rawAiDuring=$false;effectiveAiDuring=$false})}
            }
        }
        $resets+=$reset
    }
    return (@{artifact=@{observations=@{riderId='rider';horseId='mount';actorAllocationTrace=@{events=@($ledger)};ordinaryAttackTrace=@{events=@()}}}
        evidence=@{level='NATIVE INTEGRATION';passed=$true;cases=$cases;modeExitAiReassertions=$resets}}|ConvertTo-Json -Depth 20|ConvertFrom-Json)
}
$passes=0
$d=New-ConditionEnvelope
Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence
$passes++
$d=New-ConditionEnvelope -NativeMountSuccessor
Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence;$passes++
foreach($mutation in @(
    {param($d) $d.evidence.cases[0].nextRound=1},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'prepare-before'}|Select-Object -First 1).gameTicks=1},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'prepare-before'}|Select-Object -First 1).state.timeToNextNativeTurn=3},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'prepare-before'}|Select-Object -First 1).state.standard=6},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'clear-after'}|Select-Object -First 1).round=1},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'clear-after'}|Select-Object -First 1).boundary='observed-only'},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'prepare-after'}|Select-Object -First 1).state.actor='rider'},
    {param($d) $d.evidence.cases[0].afterEnd.mountClears--},
    {param($d) $d.evidence.cases[0].afterEnd.riderEffects++},
    {param($d) $d.evidence.cases[0].nextActor='friend'},
    {param($d) $d.artifact.observations.actorAllocationTrace.events+=($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.round -eq 2 -and $_.boundary -ceq 'prepare-before'}|Select-Object -First 1)}
)) {
    $d=New-ConditionEnvelope -NativeMountSuccessor;& $mutation $d;$rejected=$false
    try {Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence} catch {$rejected=$true}
    if(!$rejected){throw "Split successor evidence mutation was accepted: $mutation"};$passes++
}
foreach($mutation in @(
    {param($d) $d.evidence.cases[1].ended.mount.standard=6},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.boundary -ceq 'actor-cost-after' -and $_.command -eq 901}).state.standard=6},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.boundary -ceq 'turn-end-after' -and $_.activationIdentity -ceq $d.evidence.cases[1].activation -and $_.state.actor -ceq 'mount'}).state.standard=12},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.boundary -ceq 'turn-end-after' -and $_.activationIdentity -ceq $d.evidence.cases[1].activation -and $_.state.actor -ceq 'mount'}).state.standard=0},
    {param($d) $d.evidence.cases[0].stimulus.nativePartAbsentBefore=$false},
    {param($d) $d.evidence.cases[0].stimulus.nativePartRemoved=$false},
    {param($d) $d.evidence.cases[0].stimulus.directControlRestored=$false},
    {param($d) $d.evidence.cases[0].stimulus.cleanupResourcesUnchanged=$false},
    {param($d) $d.evidence.cases[0].operations[0].command.result='Interrupt'},
    {param($d) $d.evidence.cases[0].operations[0].ruleObservationFrame=0},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'paired-confusion-adapter-before'|Select-Object -First 1).boundary='observation'},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'paired-confusion-adapter-after'|Select-Object -First 1).state.preparingPairedActor=$false},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'paired-confusion-adapter-after'|Select-Object -First 1).state.confusionCommand=42},
    {param($d) $d.evidence.cases[0].stimulus.factBinding.exactActionBound=$false},
    {param($d) $d.evidence.cases[0].stimulus.factBinding.activeComponent=12},
    {param($d) $d.evidence.cases[0].stimulus.factBinding.activeFactCount=2},
    {param($d) $d.evidence.cases[0].stimulus.nativeFactVisits=0},
    {param($d) $d.evidence.cases[0].stimulus.conditionActiveAfterApplication=$false},
    {param($d) $d.evidence.cases[0].stimulus.conditionImmuneAfterApplication=$true},
    {param($d) $d.evidence.cases[0].stimulus.preparingAtFact=$false},
    {param($d) $d.evidence.modeExitAiReassertions=@()},
    {param($d) $d.evidence.modeExitAiReassertions[0].nativeTb=$true},
    {param($d) $d.evidence.modeExitAiReassertions[0].mountAfter.states[0].rawAiBefore=$false},
    {param($d) $d.evidence.modeExitAiReassertions[1].riderAfter.states[0].unitId='stranger'},
    {param($d) $d.evidence.modeExitAiReassertions[1].mountAfter.states[0].commandsEmptyDuring=$false},
    {param($d) $d.evidence.cases=@($d.evidence.cases[0])},
    {param($d) $d.evidence.cases[1].activation=$d.evidence.cases[0].activation},
    {param($d) $d.evidence.cases[0].outsideCombat=$false},
    {param($d) $d.evidence.cases[0].stimulus.conditionApplications=2},
    {param($d) $d.evidence.cases[1].stimulus.choiceOverrides=2},
    {param($d) $d.evidence.cases[1].stimulus.nativeSelfDamageRules=0},
    {param($d) $d.evidence.cases[1].stimulus.ownedConditionRestored=$false},
    {param($d) $d.evidence.cases[0].riderEnded=$true},
    {param($d) $d.evidence.cases[0].relationshipAfter='Mounted'},
    {param($d) $d.evidence.cases[0].mountEnded=$false},
    {param($d) $d.evidence.cases[0].samePrincipal=$false},
    {param($d) $d.evidence.cases[0].ended.rider.move=3},
    {param($d) $d.evidence.cases[0].ended.mount.standard=0},
    {param($d) $d.evidence.cases[0].admission.mountEffects++},
    {param($d) $d.evidence.cases[0].commandAtEnd.result='Interrupt'},
    {param($d) $d.evidence.cases[0].commandAtEnd.executor='rider'},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'admission-after'|Select-Object -First 1).ignoreCooldown=$true},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'prepare-before'|Select-Object -First 1).boundary='observation'},
    {param($d) $d.evidence.cases[0].operations[0].nativeRules=0},
    {param($d) $d.evidence.cases[0].operations[0].after.mount.move=0},
    {param($d) $d.evidence.cases[0].visits+= [pscustomobject]@{actor='mount';round=1}},
    {param($d) $d.evidence.cases[0].nextActor='rider'}
)) {
    $d=New-ConditionEnvelope;& $mutation $d
    $rejected=$false
    try {Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence} catch {$rejected=$true}
    if(!$rejected){throw "Condition evidence mutation was accepted: $mutation"}
    $passes++
}
function Set-NativeRecoveryEnvelope($d) {
    $op=$d.evidence.cases[0].operations[0]
    $op.command.result='Interrupt'
    $recovery=[pscustomobject]@{boundary='native-recovery-interrupt';commandType='Kingmaker.UnitLogic.Commands.UnitAttack'
        command=$op.command.id;caseId=$op.traceCase;result='Success';planned=1;completed=1;plannedWeapon=$null
        actor='rider';frame=$op.after.frame;detail='Kingmaker.UnitLogic.Commands.UnitAttack.OnTick'}
    $op.nativeRecovery=$recovery
    $d.artifact.observations.ordinaryAttackTrace.events=@($recovery)
}
$d=New-ConditionEnvelope; Set-NativeRecoveryEnvelope $d
Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence; $passes++
foreach($mutation in @(
    {param($d) $d.evidence.cases[0].operations[0].nativeRecovery.completed=0},
    {param($d) $d.evidence.cases[0].operations[0].nativeRecovery.result='Interrupt'},
    {param($d) $d.evidence.cases[0].operations[0].nativeRecovery.detail='fixture interrupt'},
    {param($d) $d.evidence.cases[0].operations[0].nativeRecovery.actor='mount'},
    {param($d) $d.artifact.observations.ordinaryAttackTrace.events=@()},
    {param($d) $d.evidence.cases[0].operations[0].nativeRules=0}
)) {
    $d=New-ConditionEnvelope; Set-NativeRecoveryEnvelope $d; & $mutation $d
    $rejected=$false
    try { Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence } catch { $rejected=$true }
    if(!$rejected){throw "Unproved recovery was accepted: $mutation"}
    $passes++
}
Write-Host "PAIRED CONDITION COMMAND PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
