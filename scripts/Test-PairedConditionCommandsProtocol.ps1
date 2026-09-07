[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ActorAllocationEvidence.ps1')
# These synthetic envelopes test rejection rules, not native gameplay.
function New-ConditionEnvelope {
    $ledger=New-Object Collections.ArrayList
    function Event([string]$boundary,[string]$actor,[string]$identity) {
        $n=$ledger.Count+1
        $e=@{sequence=$n;boundary=$boundary;frame=$n;gameTicks=100*$n;activationIdentity=$identity;state=@{actor=$actor}
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
        $admit=Event 'admission-after' 'mount' $id
        $admit.command=$commandId;$admit.commandActor='mount';$admit.commandType=$type
        foreach($actor in @('rider','mount')) { [void](Event 'prepare-after' $actor $id) }
        $admission=Sample 'condition-admission' $id 0 0 0 0
        $ended=Sample 'condition-ended' $id 0 0 6 3
        $split=Sample 'condition-split' $id 0 0 6 3
        $beforeEnd=Sample 'before-end' $id 6 0 6 3
        foreach($actor in @('rider','mount')) { foreach($kind in @('turn-end-before','turn-end-after')) { [void](Event $kind $actor $id) } }
        $afterEnd=Sample 'after-end' $id 6 3 6 3 'friend'
        $command=@{id=$commandId;type=$type;executor='mount';finished=$true;result='Success'}
        $cases+=@{name=$(if($i-eq0){'mount-do-nothing'}else{'mount-self-harm'});passed=$true;outsideCombat=$true;mountedBeforeCombat=$true;activation=$id
            stimulus=@{inputKind='native-round-fact-condition-stimulus';actor='mount';activation=$id;conditionApplications=1;choiceOverrides=1;choice=30+30*$i
                nativeSelfDamageRules=$i;nativeSelfDamage=3*$i;ownedConditionRestored=$true;before=@{standard=0;move=0};frame=$stimulusEvent.frame;gameTicks=$stimulusEvent.gameTicks}
            beforeEncounter=$before;admission=$admission;ended=$ended;forcedSplit=$split;beforeEndInput=$beforeEnd;afterEnd=$afterEnd
            samePrincipal=$true;mountEnded=$true;riderEnded=$false;relationshipAfter='Unmounted';commandAtAdmission=$command;commandAtEnd=$command
            operations=@(@{actor='rider';full=$false;hoverPure=$true;clicked=$true;nativeFull=$false;nativeSinglePrimary=$false;nativePlan=1;completed=1;nativeRules=1
                command=@{type='Kingmaker.UnitLogic.Commands.UnitAttack';result='Success'};after=$beforeEnd})
            nextActor='friend';nextRound=1;round=1;visits=@(@{actor='rider';round=1},@{actor='friend';round=1})}
    }
    return (@{artifact=@{observations=@{riderId='rider';horseId='mount';actorAllocationTrace=@{events=@($ledger)}}}
        evidence=@{level='NATIVE INTEGRATION';passed=$true;cases=$cases}}|ConvertTo-Json -Depth 20|ConvertFrom-Json)
}
$passes=0
$d=New-ConditionEnvelope
Assert-KmcPairedConditionCommandEvidence $d.artifact $d.evidence
$passes++
foreach($mutation in @(
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
Write-Host "PAIRED CONDITION COMMAND PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
