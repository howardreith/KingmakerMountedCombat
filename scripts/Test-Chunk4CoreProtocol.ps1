$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
# Parser fixtures only. These envelopes never substitute for native gameplay.
function New-CoreState {
    return @{relationship='Mounted';rider=@{id='rider';standard=0;move=0;raw=@();queue=@()};mount=@{id='mount';standard=0;move=0;raw=@();queue=@()}}
}
function New-CoreCost($caster,$spell) {
    return @(@{boundary='cost-before';actor=$caster;spellBlueprint=$spell;command=1;frame=1;standard=0},
        @{boundary='cost-after';actor=$caster;spellBlueprint=$spell;command=1;frame=1;standard=6})
}
function New-CoreLifeState($subject,$incap,$after) {
    $state=@{live=(New-CoreState);identity='activation';privatePartner='mount';pairCommand=$true;pairIntent=$false;pairMovement=$false;
        attachmentResidue=$true;attachmentRestoreVerified=$false;
        rider=@{id='rider';conscious=$true;dead=$false;damage=0;inState=$true;enabledRenderers=1};
        mount=@{id='mount';conscious=$true;dead=$false;damage=0;inState=$true;enabledRenderers=1}}
    if($after){$state.live.relationship='Unmounted';$state.identity=$null;$state.privatePartner=$null;$state.pairCommand=$false;
        $state.attachmentResidue=$false;$state.attachmentRestoreVerified=$true;$state[$subject].conscious=$false;$state[$subject].dead=(!$incap);$state[$subject].damage=130}
    return $state
}
function New-CoreEnvelope([string]$root) {
    $rows=@()
    foreach($id in @(Get-KmcChunk4CoreLeaves $root)){
        $e=@{level='NATIVE INTEGRATION';caseId=$id;mode='RT'}
        if($id.StartsWith('C4-LIFE-')){
            $subject=if($id -eq 'C4-LIFE-mount-death-live-command'){'mount'}else{'rider'};$other=if($subject -eq 'rider'){'mount'}else{'rider'}
            $incap=$id -eq 'C4-LIFE-rider-incapacitation'
            $e.mode='TB';$e+=@{subject=$subject;survivor=$other;incapacitation=$incap;damageDispatches=1;nativeDamage=130;
                beforeDamage=(New-CoreLifeState $subject $incap $false);finalLife=(New-CoreLifeState $subject $incap $true);
                liveCommandBefore=@{started=$true;finished=$false};unrelatedTurns=@(@{actor='other1'},@{actor='other2'});unrelatedOrderBefore=@('other1','other2');
                nativeLifeEvents=@{events=@(@{kind='native-life-state';actor=$subject})};nativeRules=@{dropped=0;events=@(@{kind='damage-after';target=$subject;damage=130})}}
        }elseif($id.StartsWith('C4-HORSE-')){
            $count=if($id -eq 'C4-HORSE-mounted-three-primaries'){3}else{1}
            $e.routines=@(1..$count|ForEach-Object {@{planned=1;completed=1;resolved=1;command=@{finished=$true;acted=$true;result='Success'};
                nativeRecovery=$null;rulesAfter=@{pairForcedD20=0};after=(New-CoreState);beforeStop=(New-CoreState);afterStopInput=(New-CoreState)}})
        }elseif($id -eq 'C4-RANGED-native-mixed-range'){
            $before=New-CoreState;$before.relationship='Unmounted'
            $e+=@{before=$before;command=@{type='Kingmaker.UnitLogic.Commands.UnitAttack';result='Interrupt';acted=$true};planned=4;completed=3;
                nativePlan=@(@{ranged=$true},@{ranged=$true},@{ranged=$true},@{ranged=$false});
                rules=@{riderResolved=3;pairForcedD20=0;mountNonOpportunityAttackRules=0};nativeRangeRejection=@{rangeOriginDistance=10;approachRadius=2};
                beforeStop=$before;afterStopInput=$before}
        }elseif($id.EndsWith('-heal')){
            $subject=if($id.Contains('-mount-')){'mount'}else{'rider'};$other=if($subject -eq 'rider'){'mount'}else{'rider'}
            $state=@{casterStandard=0;casterMove=0;casterPosition=@(0,0,0);pair=(New-CoreState);subjectDamage=3;otherDamage=0;healSlotAvailable=$true}
            $e+=@{subject=$subject;other=$other;caster='caster';healBlueprint='5590652e1c2225c4ca30c4a699ab3649';nativeWound=3;pauseDuration=.4;
                healSlotInitiallyAvailable=$true;pausedBegin=$state;pausedEnd=$state;afterHeal=@{healSlotAvailable=$false;subjectDamage=0;otherDamage=0};afterWound=@{subjectDamage=3;otherDamage=0};
                healClick=@{clicked=$true;queryPure=$true;canTarget=$true;available=$true;resolvedActor=$subject;before=$state;after=$state};
                healRules=@(@{actor='caster';target=$subject;value=4});nativeTrace=(New-CoreCost 'caster' '5590652e1c2225c4ca30c4a699ab3649')}
        }elseif($id -eq 'C4-TARGETING-area-both'){
            $saves=@('rider','mount')|ForEach-Object {@{actor=$_;type='Reflex';dc=14;passed=$true;stat=4;roll=12;gameTicks=10}}
            $e+=@{entity='area1';blueprint='native-area';caster='caster';state=@{pair=(New-CoreState);areaSlotAvailable=$false};unitsInside=@('rider','mount');
                firstSaves=@($saves);allSaves=@($saves);nativeTrace=(New-CoreCost 'caster' '0fd00984a2c0e0a429cf1a911b4ec5ca')}
        }else{
            $subject=if($id.Contains('-mount-')){'mount'}else{'rider'};$other=if($subject -eq 'rider'){'mount'}else{'rider'}
            $e+=@{subject=$subject;other=$other;caster='caster';beforeHostile=@{pair=(New-CoreState)};hostileCommand=@{type='Kingmaker.UnitLogic.Commands.UnitAttack';executor='enemy';started=$true;acted=$true;finished=$true};
                nativeRules=@{dropped=0;attacks=@(@{resolved=$true});events=@(@{kind='attack-before';actor='enemy';target=$subject},@{kind='attack-roll';actor='enemy';target=$subject;nativeAC=20})}}
        }
        $rows+=@{name=$id;status='PASS';evidence=$e}
    }
    return (@{schemaVersion=22;status='PASS';rows=$rows;errors=@();subscenarioPassCount=$rows.Count;subscenarioFailCount=0;
        observations=@{phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false};
            ordinaryAttackTrace=@{dropped=0;events=@()}}}|ConvertTo-Json -Depth 30|ConvertFrom-Json)
}
foreach($root in @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-targeting-rider-rt',
    'chunk4-targeting-mount-rt','chunk4-horse-strike-comparison-rt','chunk4-ranged-native-control-rt')){
    $request=@{scenario=$root};$native=New-CoreEnvelope $root
    Assert-KmcChunk4CoreEvidence $request $native 'PASS';$passed++
    $mutations=@(
        {param($e) $e.schemaVersion=21},
        {param($e) $e.rows=@()},
        {param($e) $e.rows=@($e.rows)+@($e.rows[0]);$e.subscenarioPassCount++},
        {param($e) $e.subscenarioPassCount++},
        {param($e) $e.rows[0].evidence.level='COMPONENT'},
        {param($e) $e.rows[0].evidence.caseId='wrong-case'},
        {param($e) $e.observations.phase3fActualConfiguration.enablePairedActivation=$false},
        {param($e) $e.observations.phase3fActualConfiguration.enableUnifiedMountedTurn=$true},
        {param($e) $e.observations.ordinaryAttackTrace.dropped=1}
    )
    foreach($row in $native.rows){
        $case=$row.name
        if($case.StartsWith('C4-LIFE-')){
            $mutations+=@({param($e) $e.rows[0].evidence.nativeDamage=0},{param($e) $e.rows[0].evidence.liveCommandBefore.finished=$true},
                {param($e) $e.rows[0].evidence.finalLife.privatePartner='stale'},{param($e) $e.rows[0].evidence.unrelatedTurns[1].actor='mount'},
                {param($e) $e.rows[0].evidence.nativeLifeEvents.events=@()},{param($e) $e.rows[0].evidence.nativeRules.events=@()})
        }elseif($case.StartsWith('C4-HORSE-')){
            $mutations+=@({param($e) $e.rows[0].evidence.routines[0].resolved=0},{param($e) $e.rows[0].evidence.routines[0].afterStopInput.mount.move=3})
        }elseif($case -eq 'C4-RANGED-native-mixed-range'){
            $mutations+=@({param($e) $e.rows[0].evidence.completed=4},{param($e) $e.rows[0].evidence.nativePlan[3].ranged=$true},
                {param($e) $e.rows[0].evidence.rules.riderResolved=2})
        }elseif($case.EndsWith('-heal')){
            $mutations+=@({param($e) $e.rows[0].evidence.healClick.resolvedActor='wrong'},{param($e) $e.rows[0].evidence.afterHeal.healSlotAvailable=$true},
                {param($e) $e.rows[0].evidence.nativeTrace=@()},{param($e) $e.rows[0].evidence.healRules=@()})
        }elseif($case -eq 'C4-TARGETING-area-both'){
            $mutations+=@({param($e) $area=@($e.rows|Where-Object {$_.name -eq 'C4-TARGETING-area-both'})[0].evidence;$area.unitsInside=@('rider')},
                {param($e) $area=@($e.rows|Where-Object {$_.name -eq 'C4-TARGETING-area-both'})[0].evidence;$area.firstSaves[1].actor='rider'},
                {param($e) $area=@($e.rows|Where-Object {$_.name -eq 'C4-TARGETING-area-both'})[0].evidence;$area.allSaves+=@($area.firstSaves[0])})
        }else{
            $mutations+=@({param($e) $hostile=@($e.rows|Where-Object {$_.name.EndsWith('-hostile')})[0].evidence;$hostile.hostileCommand.acted=$false},
                {param($e) $hostile=@($e.rows|Where-Object {$_.name.EndsWith('-hostile')})[0].evidence;$hostile.nativeRules.attacks[0].resolved=$false})
        }
    }
    foreach($mutation in $mutations){
        $changed=New-CoreEnvelope $root; & $mutation $changed; $rejected=$false
        try{Assert-KmcChunk4CoreEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw "Invalid core envelope accepted for $root"};$passed++
    }
}
Write-Host "COMPONENT parser-only TOTAL PASS=$passed FAIL=0"
