[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ActorAllocationEvidence.ps1')
$passes=0
# Synthetic envelopes exercise rejection rules; they are not native qualification.
function New-RestrictionFixture {
    $ledger=New-Object Collections.ArrayList
    $samples=New-Object Collections.ArrayList
    function Sample([string]$kind,[string]$id,[double]$rs,[double]$rm,[double]$ms,[double]$mm,[bool]$grant=$false) {
        if($grant) { foreach($actor in @('rider','mount')) { foreach($boundary in @('clear-after','round-state-after')) {
            [void]$ledger.Add(@{sequence=$ledger.Count+1;boundary=$boundary;state=@{actor=$actor}})
        } } }
        $seq=$ledger.Count+1
        [void]$ledger.Add(@{sequence=$seq;boundary=$kind;frame=$seq;gameTicks=$seq*100;state=@{actor='rider'}})
        $s=@{kind=$kind;traceSequence=$seq;frame=$seq;gameTicks=$seq*100;identity=$id;condition=$null;mountConditions=@()
            rider=@{actor='rider';standard=$rs;move=$rm;prone=$false;speedMps=5}
            mount=@{actor='mount';standard=$ms;move=$mm;prone=$false;hasStandard=($ms-eq0);speedMps=5}}
        foreach($actor in @('rider','mount')) {
            $s[$actor+'Clears']=@($ledger|Where-Object {$_.state.actor -ceq $actor -and $_.boundary -ceq 'clear-after'}).Count
            $s[$actor+'Effects']=@($ledger|Where-Object {$_.state.actor -ceq $actor -and $_.boundary -ceq 'round-state-after'}).Count
        }
        [void]$samples.Add($s);return $s
    }
    function Attack([string]$actor,[bool]$full,$before,$after) {
        $n=if($full){4}else{1}
        return @{actor=$actor;contextActor=$actor;selectedActor=$actor;full=$full;fullEnabled=$full;nativeFull=$full
            nativeSinglePrimary=$false;clicked=$true;hoverPure=$true;nativePlan=$n;completed=$n;nativeRules=$n
            command=@{executor=$actor;result='Success';finished=$true};before=$before;after=$after}
    }
    $begin=Sample 'restrictions-begin' 'g1' 0 0 0 0 $true
    $stagger=Sample 'staggered-before-move' 'g1' 0 0 0 0
    $stagger.condition='Staggered';$stagger.mountConditions=@('Staggered')
    $denied=Sample 'staggered-move-rejects-standard' 'g1' 0 0 0 0.1
    $denied.mount.hasStandard=$false;$denied.noNewAttackOrPendingIntent=$true;$denied.nativeRulesBefore=0;$denied.nativeRulesAfter=0
    $end1=Sample 'restriction-native-end-input' 'g1' 0 0 0 0.1
    $next1=Sample 'restriction-native-next-grant' 'g2' 0 0 0 0 $true
    $attackBefore=Sample 'restriction-single-before' 'g2' 0 0 0 0
    $attackAfter=Sample 'restriction-single-after' 'g2' 0 0 6 0
    $rejectMove=Sample 'staggered-standard-rejects-move' 'g2' 0 0 6 0
    $end2=Sample 'restriction-native-end-input' 'g2' 0 0 6 0
    $next2=Sample 'restriction-native-next-grant' 'g3' 0 0 0 0 $true
    $upBefore=Sample 'native-get-up-input-before' 'g3' 0 0 0 0
    $upBefore.mount.prone=$true;$upBefore.mountCanGetUp=$true
    $upAfter=Sample 'native-get-up-finished' 'g3' 0 0 0 3
    $end3=Sample 'restriction-native-end-input' 'g3' 0 0 0 3
    $next3=Sample 'restriction-native-next-grant' 'g4' 0 0 0 0 $true
    $disabled=Sample 'disabled-mount-before-rider-full' 'g4' 0 0 0 0
    $disabled.condition='Stunned';$disabled.mountConditions=@('Stunned');$disabled.mountAble=$false
    $fullBefore=Sample 'restriction-full-before' 'g4' 0 0 0 0
    $fullAfter=Sample 'restriction-full-after' 'g4' 6 3 0 0
    $end4=Sample 'disabled-auto-end-wait' 'g4' 6 3 6 3
    $next4=Sample 'restriction-native-next-grant' 'g5' 0 0 0 0 $true
    $restored=Sample 'native-restrictions-restored' 'g5' 0 0 0 0
    $modeBefore=Sample 'native-mode-standard-spend-before' 'g5' 0 0 0 0.1
    $modeAfter=Sample 'native-mode-standard-spend-after' 'g5' 0 0 6 3
    $moves=@(
        @{purpose='staggered-partial';admitted=$true;fiveFootStep=$false;selectedActor='mount';distance=0.5;travelledDistance=0.5
            nativeShiftDistance=0.5;nativeMoveCost=0.1;nativeAllowedTime=0.1;before=$stagger.mount;riderBefore=$stagger.rider;riderAfter=$denied.rider},
        @{purpose='staggered-standard-rejects-move';fiveFootStep=$false;distance=0;travelledDistance=0;nativeShiftDistance=0
            nativeMoveCost=0;nativeAllowedTime=0;after=$rejectMove.mount;riderBefore=$attackAfter.rider;riderAfter=$rejectMove.rider},
        @{purpose='native-get-up-input';admitted=$true;distance=0;travelledDistance=0;nativeShiftDistance=0;nativeMoveCost=3
            nativeAllowedTime=0;before=$upBefore.mount})
    $data=@{artifact=@{observations=@{riderId='rider';horseId='mount';actorAllocationTrace=@{events=@($ledger)};ordinaryAttackTrace=@{events=@()}}}
        evidence=@{level='NATIVE INTEGRATION';passed=$true;inputKind='scripted-native-control-integration';ownedConditionRestored=$true
            automaticEndSettingRestored=$true;automaticEndInputCount=0;events=@($samples)
            operations=@((Attack 'mount' $false $attackBefore $attackAfter),(Attack 'rider' $true $fullBefore $fullAfter));movements=$moves
            nativeConditionEvents=@{events=@(@{kind='native-get-up';actor='mount';frame=$upAfter.frame})}}
        transition=@{modeAttack=@{clicked=$true;nativeRule=$true;completedAttacks=1;result='Success';actor='mount';resourceOwner='mount';before=$modeBefore;after=$modeAfter}}
        mode=$modeAfter}
    return ($data|ConvertTo-Json -Depth 35|ConvertFrom-Json)
}
$valid=New-RestrictionFixture
Assert-KmcPairedRestrictionEvidence $valid.artifact $valid.evidence
Assert-KmcPairedSpentModeEvidence $valid.artifact $valid.transition $valid.mode
$passes+=2
foreach($mutation in @(
    {param($d) $d.evidence.ownedConditionRestored=$false},
    {param($d) $d.evidence.automaticEndSettingRestored=$false},
    {param($d) $d.evidence.automaticEndInputCount=1},
    {param($d) $d.evidence.operations[0].nativeRules=0},
    {param($d) $d.evidence.operations[0].after.mount.move=3},
    {param($d) $d.evidence.operations[1].selectedActor='mount'},
    {param($d) $d.evidence.operations[1].hoverPure=$false},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'staggered-move-rejects-standard').noNewAttackOrPendingIntent=$false},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'staggered-move-rejects-standard').nativeRulesAfter=1},
    {param($d) $d.artifact.observations.ordinaryAttackTrace.events=@(@{boundary='start-after';actor='mount';frame=6})},
    {param($d) $d.evidence.movements[0].nativeShiftDistance=0},
    {param($d) $d.evidence.movements[0].riderAfter.move=0.1},
    {param($d) $d.evidence.movements[1].distance=0.5},
    {param($d) $d.evidence.movements[2].nativeMoveCost=0},
    {param($d) $d.evidence.movements[2].nativeAllowedTime=1},
    {param($d) $d.evidence.nativeConditionEvents.events+=@($d.evidence.nativeConditionEvents.events[0])},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'native-get-up-finished').mount.prone=$true},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'disabled-mount-before-rider-full').mountAble=$true},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'restriction-native-next-grant'|Select-Object -First 1).mountClears++},
    {param($d) ($d.evidence.events|Where-Object kind -CEQ 'native-restrictions-restored').mountConditions=@('Stunned')}
)) {
    $d=New-RestrictionFixture; & $mutation $d;$rejected=$false
    try {Assert-KmcPairedRestrictionEvidence $d.artifact $d.evidence} catch {$rejected=$true}
    if(!$rejected){throw ('Invalid restriction envelope accepted: '+$mutation.ToString())};$passes++
}
foreach($mutation in @(
    {param($d) $d.transition.modeAttack.nativeRule=$false},
    {param($d) $d.transition.modeAttack.resourceOwner='rider'},
    {param($d) $d.transition.modeAttack.before.mount.move=0},
    {param($d) $d.transition.modeAttack.after.mount.standard=0},
    {param($d) $d.mode.mount.standard=0},
    {param($d) $d.transition.modeAttack.after.mountEffects++}
)) {
    $d=New-RestrictionFixture;& $mutation $d;$rejected=$false
    try {Assert-KmcPairedSpentModeEvidence $d.artifact $d.transition $d.mode} catch {$rejected=$true}
    if(!$rejected){throw 'Mode test accepted fabricated/erased Standard expenditure.'};$passes++
}
function New-OrderFixture {
    $order=@('rider','friend','enemy');$visits=@();$events=@()
    for($i=0;$i -lt 3;$i++) {
        $visits+=@{actor=$order[$i];round=1;frame=$i;gameTicks=$i;traceSequence=$i+1;nativeOrder=$order}
        $events+=@{sequence=$i+1;state=@{actor=$order[$i]};round=1;frame=$i;gameTicks=$i;boundary='turn-observed';detail='native-order=rider,friend,enemy'}
    }
    return (@{artifact=@{observations=@{riderId='rider';horseId='mount';actorAllocationTrace=@{events=$events}}}
        evidence=@{activations=@(@{round=1});traceEndSequence=3;turnVisits=$visits}}|ConvertTo-Json -Depth 15|ConvertFrom-Json)
}
$d=New-OrderFixture;Assert-KmcPairedNativeOrder $d.artifact $d.evidence;$passes++
foreach($mutation in @(
    {param($d) $d.evidence.turnVisits[1].actor='enemy'},
    {param($d) $d.evidence.turnVisits[1].nativeOrder=@('rider','enemy','friend')},
    {param($d) $d.evidence.turnVisits[1].traceSequence=1},
    {param($d) $d.evidence.traceEndSequence=2},
    {param($d) $d.artifact.observations.actorAllocationTrace.events[1].detail='native-order=rider,enemy,friend'}
)) {
    $d=New-OrderFixture;& $mutation $d;$rejected=$false
    try {Assert-KmcPairedNativeOrder $d.artifact $d.evidence} catch {$rejected=$true}
    if(!$rejected){throw 'Native unrelated-actor order was changed without rejection.'};$passes++
}
Write-Host "PAIRED RESTRICTION PROTOCOL PASS=$passes FAIL=0 (envelope validation only)"
