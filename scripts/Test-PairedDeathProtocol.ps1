[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ActorAllocationEvidence.ps1')
# Synthetic envelopes test rejection of false claims, never native gameplay.
function New-DeathEnvelope {
    $ledger=New-Object Collections.ArrayList
    $identity=('a'*32)+':1'
    function Event([string]$kind,[string]$actor,[string]$grant=$identity) {
        $n=$ledger.Count+1
        $event=@{sequence=$n;boundary=$kind;frame=$n;gameTicks=100*$n;activationIdentity=$grant;state=@{actor=$actor;standard=6;move=3}}
        [void]$ledger.Add($event);return $event
    }
    function Sample([string]$kind,[string]$grant,[double]$ms=0,[double]$mm=0,[string]$current='rider') {
        $event=Event $kind 'rider' $grant
        $s=@{kind=$kind;traceSequence=$event.sequence;gameTicks=$event.gameTicks;frame=$event.frame;identity=$grant;currentActor=$current
            rider=@{actor='rider';standard=0;move=0};mount=@{actor='mount';standard=$ms;move=$mm}}
        foreach($actor in @('rider','mount')) {
            $s[$actor+'Clears']=@($ledger|Where-Object {$_.boundary -ceq 'clear-after' -and $_.state.actor -ceq $actor}).Count
            $s[$actor+'Effects']=@($ledger|Where-Object {$_.boundary -ceq 'round-state-after' -and $_.state.actor -ceq $actor}).Count
        }
        return $s
    }
    $beforeEncounter=Sample 'before-encounter' ''
    foreach($actor in @('rider','mount')) {
        foreach($boundary in @('prepare-before','clear-after','round-state-after','prepare-after')) {[void](Event $boundary $actor)}
    }
    $beforeMovement=Sample 'before-movement' $identity
    $beforeAttack=Sample 'before-attack' $identity 0 0.1
    $beforeDamage=Sample 'before-damage' $identity 6 3
    [void](Event 'remove-unit-before' 'mount')
    foreach($actor in @('rider','mount')) {
        foreach($boundary in @('turn-end-before','turn-end-after')) {[void](Event $boundary $actor)}
    }
    [void](Event 'remove-unit-after' 'mount' '')
    $returned=Sample 'damage-returned' '' 6 3
    $after=Sample 'next-actor' '' 6 3 'friend'
    $e=@{level='NATIVE INTEGRATION';passed=$true;mountedBeforeCombat=$true;inputKind='labelled-native-damage-effect-stimulus'
        damageDispatches=1;targetActor='mount';sourceActor='enemy';nativeDamage=100;mountDead=$true;relationshipAfter='Unmounted'
        lethalDamageThreshold=99;hitPointsBefore=80;constitutionBefore=18;temporaryHitPointsBefore=0
        damageToPartyBefore=0.2;damageToPartyAfter=0.2;requestedDamage=500;nativeDamageBeforeDifficulty=500
        sourceIsPlayersEnemy=$true;sourceIsPlayerFaction=$false
        riderDamageBefore=0;riderDamageAfter=0;beforeEncounter=$beforeEncounter;beforeMovement=$beforeMovement;beforeAttack=$beforeAttack
        beforeDamage=$beforeDamage;afterDamageDispatch=$returned;afterRemoval=$after
        nativeUnrelatedOrderBefore=@('friend','enemy');expectedNextActor='friend'
        attack=@{actor='mount';resourceOwner='mount';result='Success';nativeRule=$true;completedAttacks=1}
        movement=@{clicked=$true;admitted=$true;distance=0.5;travelledDistance=0.5;nativeAllowedTime=0.1;nativeMoveCost=0.1
            result='Success';riderAfter=@{standard=0;move=0}}
        nativeLifeEvents=@{events=@(@{kind='native-life-state';actor='mount';lifeState='Dead'})}}
    return (@{evidence=$e;artifact=@{observations=@{riderId='rider';horseId='mount';actorAllocationTrace=@{events=@($ledger)}}}}|
        ConvertTo-Json -Depth 20|ConvertFrom-Json)
}
$passes=0
$d=New-DeathEnvelope;Assert-KmcPairedDeathEvidence $d.artifact $d.evidence;$passes++
foreach($mutate in @(
    {param($d) $d.evidence.nativeDamage=98},
    {param($d) $d.evidence.requestedDamage=100},
    {param($d) $d.evidence.nativeDamageBeforeDifficulty=99},
    {param($d) $d.evidence.damageToPartyAfter=1},
    {param($d) $d.evidence.sourceIsPlayerFaction=$true},
    {param($d) $d.evidence.damageToPartyBefore=0},
    {param($d) $d.evidence.hitPointsBefore=100},
    {param($d) $d.evidence.mountDead=$false},
    {param($d) $d.evidence.expectedNextActor='enemy'},
    {param($d) $d.evidence.damageDispatches=0},
    {param($d) $d.evidence.nativeDamage=0},
    {param($d) $d.evidence.targetActor='rider'},
    {param($d) $d.evidence.riderDamageAfter=1},
    {param($d) $d.evidence.afterRemoval.identity=$d.evidence.beforeDamage.identity},
    {param($d) $d.evidence.afterRemoval.currentActor='mount'},
    {param($d) $d.evidence.beforeDamage.mount.standard=0},
    {param($d) $d.evidence.attack.nativeRule=$false},
    {param($d) $d.evidence.movement.nativeMoveCost=0},
    {param($d) $d.evidence.nativeLifeEvents.events=@()},
    {param($d) $d.evidence.nativeLifeEvents.events[0].actor='rider'},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'remove-unit-after').state.standard=0},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'remove-unit-before').boundary='observed-only'},
    {param($d) ($d.artifact.observations.actorAllocationTrace.events|Where-Object {$_.boundary -ceq 'turn-end-after' -and $_.state.actor -ceq 'mount'}).sequence=999},
    {param($d) $d.evidence.afterRemoval.mountEffects++},
    {param($d) $d.artifact.observations.actorAllocationTrace.events+=($d.artifact.observations.actorAllocationTrace.events|Where-Object boundary -CEQ 'prepare-after'|Select-Object -First 1)}
)) {
    $d=New-DeathEnvelope;& $mutate $d;$rejected=$false
    try {Assert-KmcPairedDeathEvidence $d.artifact $d.evidence} catch {$rejected=$true}
    if(!$rejected){throw "Death evidence mutation accepted: $mutate"};$passes++
}
Write-Host "PAIRED DEATH PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
