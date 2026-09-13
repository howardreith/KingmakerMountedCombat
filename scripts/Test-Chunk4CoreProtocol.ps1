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
        attachmentResidue=$true;attachmentRestoreVerified=$false;split=$false;finalized=$false;riderEnded=$false;mountEnded=$false;
        currentActor='rider';riderGrants=2;mountGrants=2;actorRecords=2;playerCombat=$true;riderCombat=$true;mountCombat=$true;tbActive=$true;tbInitialized=$true;
        frame=10;gameTicks=10000000;round=2;
        rider=@{id='rider';conscious=$true;dead=$false;finallyDead=$false;damage=0;hp=103;characterLevel=11;nonLethalDamage=0;inState=$true;enabledRenderers=1};
        mount=@{id='mount';conscious=$true;dead=$false;finallyDead=$false;damage=0;hp=103;characterLevel=11;nonLethalDamage=0;inState=$true;enabledRenderers=1}}
    if($after){$state.live.relationship='Unmounted';$state.split=$true;$state.privatePartner=$null;$state.pairCommand=$false;
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
                liveCommandBefore=@{started=$true;finished=$false};unrelatedTurns=@(@{actor='other1';frame=20;round=2},@{actor='other2';frame=30;round=2});unrelatedOrderBefore=@('other1','other2');
                eligibleRosterBeforeDamage=@('other2','mount','rider','other1');principalRosterIndex=2;
                nativeLifeEvents=@{events=@(@{kind='native-life-state';actor=$subject;lifeState=$(if($incap){'Unconscious'}else{'Dead'});frame=15})};nativeRules=@{dropped=0;events=@(@{kind='damage-after';target=$subject;damage=130})}}
            $e.afterCleanup=New-CoreLifeState $subject $incap $true
            $e.successorOrderBefore=@('other1','other2','mount')|Where-Object {$_ -cne $subject}
            $e.sameRoundMountExcluded=$false
            $e.allocationSequenceBeforeDamage=0;$e.allocationTrace=@{dropped=0;observationErrors=0;events=(New-CoreNativeTurnEnd 'other1' 11 2 25 1)}
            $e.successorTurns=@(0..1|ForEach-Object {
                $state=New-CoreLifeState $subject $incap $true;$state.currentActor='other'+($_+1);$state.frame=20+10*$_
                @{actor=$state.currentActor;round=2;frame=(20+10*$_);turn=(11+$_);survivor=$false;endInput=($_ -eq 0);state=$state}
            })
            $e.nativeEncounterExit=New-CoreLifeState $subject $incap $true
            $e.nativeEncounterExit.identity=$null;$e.nativeEncounterExit.actorRecords=0
            foreach($flag in @('playerCombat','riderCombat','mountCombat','tbActive','tbInitialized')){$e.nativeEncounterExit[$flag]=$false}
            $permanent=$id -ceq 'C4-LIFE-rider-death-live-command'
            $e.nativeRecoveryExpected=!$permanent
            foreach($state in (@($e.afterCleanup,$e.finalLife,$e.nativeEncounterExit)+@($e.successorTurns|ForEach-Object {$_.state}))){$state[$subject].finallyDead=$permanent}
            if(!$permanent){
                $e.nativeEncounterExit[$subject].conscious=$true;$e.nativeEncounterExit[$subject].dead=$false;$e.nativeEncounterExit[$subject].damage=92
                $source=@(@{type='Kingmaker.Controllers.Units.UnitReturnToConsciousController';method='Tick';token='0600918e';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
                    @{type='Kingmaker.Controllers.Units.UnitReturnToConsciousController';method='MakeUnitConscious';token='06009191';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},
                    @{type='Kingmaker.Controllers.Units.UnitLifeController';method='SetLifeState';token='06009164';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
                $e.nativeLifeEvents.events+=@(@{kind='native-life-state';actor=$subject;lifeState='Conscious';frame=40;damage=92;nativeSource=$source})
            }
            $e.nativeEncounterExit.frame=45
            $e.afterPolicyRestore=$e.nativeEncounterExit|ConvertTo-Json -Depth 12|ConvertFrom-Json
            $e.afterPolicyRestore.frame=50;$e.afterPolicyRestore.gameTicks=15000000
            $beforePolicy=@{riseAfterCombat=@{raw=$null;value=$true;persisted='1'};deathDoor=@{raw=$false;value=$false;persisted='0'};
                trueDeath=$false;deathDoorCondition=$false;damageToParty=.2}
            $effectivePolicy=$beforePolicy|ConvertTo-Json -Depth 8|ConvertFrom-Json
            if($permanent){$effectivePolicy.riseAfterCombat.raw=$false;$effectivePolicy.riseAfterCombat.value=$false;$effectivePolicy.trueDeath=$true}
            $e.nativeDeathPolicy=@{permanentDeathFixture=$permanent;before=$beforePolicy;effective=$effectivePolicy;restoration=@{restored=$true;state=$beforePolicy}}
            $e.enemyDamageDispatches=1;$e.enemyNativeDamage=100;$e.enemyLifeTransitions=1;$e.enemyDamageSource='other1'
            $e.enemyBeforeDamage=@{id='enemy';conscious=$true};$e.enemyAfterDeath=@{id='enemy';dead=$true}
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
        }elseif($id -cin @('C4-TARGETING-area-both','C4-TARGETING-area-unmounted')){
            $mounted=$id -ceq 'C4-TARGETING-area-both';$state=New-CoreState
            if(!$mounted){$state.relationship='Unmounted'}
            $saves=@('rider','mount')|ForEach-Object {
                $stat=if($_ -ceq 'rider'){8}else{3};$identity=if($_ -ceq 'rider'){100}else{101}
                @{actor=$_;target=$_;kind='saving-throw';identity=$identity;frame=20;type='Reflex';dc=16;passed=$true;stat=$stat;roll=23;gameTicks=10;
                    nativeSource=@{area='area1';areaBlueprint='bcb6329cefc66da41b011299a43cc681';caster='caster';actorInside=$true;
                        sourceAbility='0fd00984a2c0e0a429cf1a911b4ec5ca';contextBlueprint='bcb6329cefc66da41b011299a43cc681';
                        callbacks=@(@{kind='unit-enter';token='06002ccd';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})}}
            }
            $round=($saves[1]|ConvertTo-Json -Depth 12|ConvertFrom-Json)
            $round.identity=102;$round.nativeSource.callbacks[0].kind='round';$round.nativeSource.callbacks[0].token='06002cd0'
            $e+=@{mounted=$mounted;ruleDrops=0;entity='area1';blueprint='bcb6329cefc66da41b011299a43cc681';caster='caster';
                state=@{pair=$state;areaSlotAvailable=$false;subjectDamage=0;otherDamage=0};
                before=@{pair=$state;areaSlotAvailable=$true;subjectDamage=0;otherDamage=0;riderReflex=8;mountReflex=3};
                definition=@(@{type='Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectRunAction';unitEnter=@{type='ActionList'};round=@{type='ActionList'}});
                unitsInside=@('rider','mount');firstSaves=@($saves);allSaves=@($saves)+@($round);
                nativeTrace=(New-CoreCost 'caster' '0fd00984a2c0e0a429cf1a911b4ec5ca')}
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
function Add-CoreNativeSurvivorTurn($envelope) {
    $e=$envelope.rows[0].evidence
    $e.eligibleRosterBeforeDamage=@('mount','other2','rider','other1');$e.principalRosterIndex=2
    $e.successorOrderBefore=@('other1','mount','other2')
    $turn=$e.successorTurns[0]|ConvertTo-Json -Depth 15|ConvertFrom-Json
    $turn.actor='mount';$turn.survivor=$true;$turn.frame=25;$turn.round=3;$turn.turn=13;$turn.endInput=$true
    $turn.state.currentActor='mount';$turn.state.mountGrants=3;$turn.state.round=3;$turn.state.frame=25
    $e.successorTurns=@($e.successorTurns[0],$turn,$e.successorTurns[1]);$e.successorTurns[2].state.mountGrants=3
    $e.successorTurns[2].round=3;$e.successorTurns[2].state.round=3;$e.unrelatedTurns[1].round=3
    foreach($state in @($e.finalLife,$e.nativeEncounterExit,$e.afterPolicyRestore)){$state.mountGrants=3}
    $e.allocationTrace.events=@(New-CoreNativeTurnEnd 'other1' 11 2 22 1)+@(0..1|ForEach-Object {
        [pscustomobject]@{sequence=($_+3);boundary=$(if($_ -eq 0){'prepare-before'}else{'prepare-after'});round=3;frame=24;turn=13;preparingTurn=13;currentActor='mount';activationIdentity=$null;simulatingClick=$false;
            state=[pscustomobject]@{actor='mount';grantSequence=3;pairedGrantIdentity=$null;prepared=$true;canAct=$true}}
    })+@(New-CoreNativeTurnEnd 'mount' 13 3 27 5)
}
function New-CoreNativeTurnEnd($actor,$turn,$round,$frame,$sequence) {
    return @(0..1|ForEach-Object {
        [pscustomobject]@{sequence=($sequence+$_);boundary=$(if($_ -eq 0){'turn-end-before'}else{'turn-end-after'});
            round=$round;frame=$frame;turn=$turn;currentActor=$actor;activationIdentity=$null;simulatingClick=$false;
            turnStatus=$(if($_ -eq 0){'Ending'}else{'Ended'});state=[pscustomobject]@{actor=$actor}}
    })
}
function Add-CoreNativeEnemyTurn($envelope) {
    $e=$envelope.rows[0].evidence
    $e|Add-Member -NotePropertyName source -NotePropertyValue 'other1' -Force
    $e.enemyBeforeDamage.id='other1';$e.enemyAfterDeath.id='other1';$e.enemyDamageSource='other2'
    $e.enemyBeforeDamage|Add-Member -NotePropertyName directlyControllable -NotePropertyValue $false -Force
    $e.enemyBeforeDamage|Add-Member -NotePropertyName effectiveAiEnabled -NotePropertyValue $true -Force
    $e.successorTurns[0].endInput=$false
    $e.allocationTrace.events=New-CoreNativeTurnEnd 'other1' 11 2 25 1
}
foreach($root in @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-targeting-rider-rt',
    'chunk4-targeting-area-unmounted-rt','chunk4-targeting-mount-rt','chunk4-horse-strike-comparison-rt','chunk4-ranged-native-control-rt')){
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
            $mutations+=@(
                {param($e) $life=$e.rows[0].evidence;$life.unrelatedOrderBefore=@('other2','other1');$life.unrelatedTurns[0].actor='other2';$life.unrelatedTurns[1].actor='other1'},
                {param($e) $e.rows[0].evidence.eligibleRosterBeforeDamage=@()},
                {param($e) $e.rows[0].evidence.principalRosterIndex=0},
                {param($e) $e.rows[0].evidence.eligibleRosterBeforeDamage[0]='mount'}
            )
            $mutations+=@(
                {param($e) $e.rows[0].evidence.nativeDeathPolicy.restoration.restored=$false},
                {param($e) $e.rows[0].evidence.nativeDeathPolicy.restoration.state.riseAfterCombat.raw=$true},
                {param($e) $e.rows[0].evidence.nativeDeathPolicy.effective.riseAfterCombat.persisted='changed'},
                {param($e) $e.rows[0].evidence.nativeDeathPolicy.effective.damageToParty=1},
                {param($e) $e.rows[0].evidence.nativeDeathPolicy.permanentDeathFixture=!$e.rows[0].evidence.nativeDeathPolicy.permanentDeathFixture},
                {param($e) $e.rows[0].evidence.nativeRecoveryExpected=!$e.rows[0].evidence.nativeRecoveryExpected},
                {param($e) $e.rows[0].evidence.afterPolicyRestore.actorRecords=1},
                {param($e) $e.rows[0].evidence.afterPolicyRestore.gameTicks=$e.rows[0].evidence.nativeEncounterExit.gameTicks}
            )
            if($case -ceq 'C4-LIFE-rider-death-live-command'){
                $mutations+=@(
                    {param($e) $e.rows[0].evidence.finalLife.rider.finallyDead=$false},
                    {param($e) $e.rows[0].evidence.afterPolicyRestore.rider.finallyDead=$false},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events+=@([pscustomobject]@{kind='native-life-state';actor='rider';lifeState='Conscious'})}
                )
            }else{
                $mutations+=@(
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].nativeSource=@()},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].nativeSource[0].assemblyMvid='wrong'},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].nativeSource[0].token='06009191'},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].frame=20},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].actor='other1'},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events[1].damage=0},
                    {param($e) $e.rows[0].evidence.nativeLifeEvents.events+=@($e.rows[0].evidence.nativeLifeEvents.events[1])},
                    {param($e) $s=if($e.rows[0].evidence.subject -ceq 'mount'){'mount'}else{'rider'};$e.rows[0].evidence.afterPolicyRestore.$s.damage=0}
                )
            }
            $mutations+=@({param($e) $e.rows[0].evidence.nativeDamage=0},{param($e) $e.rows[0].evidence.liveCommandBefore.finished=$true},
                {param($e) $e.rows[0].evidence.finalLife.privatePartner='stale'},{param($e) $e.rows[0].evidence.unrelatedTurns[1].actor='mount'},
                {param($e) $e.rows[0].evidence.nativeLifeEvents.events=@()},{param($e) $e.rows[0].evidence.nativeRules.events=@()},
                {param($e) $e.rows[0].evidence.finalLife.split=$false},
                {param($e) $e.rows[0].evidence.afterCleanup.identity='new-activation'},
                {param($e) $e.rows[0].evidence.finalLife.riderGrants=3},
                {param($e) $e.rows[0].evidence.finalLife.mountGrants=3},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.identity='stale'},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.actorRecords=1},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.playerCombat=$true},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.tbInitialized=$true},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.privatePartner='stale'},
                {param($e) $e.rows[0].evidence.nativeEncounterExit.attachmentResidue=$true},
                {param($e) $e.rows[0].evidence.enemyDamageDispatches=0},
                {param($e) $e.rows[0].evidence.enemyNativeDamage=0},
                {param($e) $e.rows[0].evidence.enemyLifeTransitions=0},
                {param($e) $e.rows[0].evidence.enemyAfterDeath.dead=$false},
                {param($e) $e.rows[0].evidence.enemyDamageSource=$e.rows[0].evidence.subject})
        }elseif($case.StartsWith('C4-HORSE-')){
            $mutations+=@({param($e) $e.rows[0].evidence.routines[0].resolved=0},{param($e) $e.rows[0].evidence.routines[0].afterStopInput.mount.move=3})
        }elseif($case -eq 'C4-RANGED-native-mixed-range'){
            $mutations+=@({param($e) $e.rows[0].evidence.completed=4},{param($e) $e.rows[0].evidence.nativePlan[3].ranged=$true},
                {param($e) $e.rows[0].evidence.rules.riderResolved=2})
        }elseif($case.EndsWith('-heal')){
            $mutations+=@({param($e) $e.rows[0].evidence.healClick.resolvedActor='wrong'},{param($e) $e.rows[0].evidence.afterHeal.healSlotAvailable=$true},
                {param($e) $e.rows[0].evidence.nativeTrace=@()},{param($e) $e.rows[0].evidence.healRules=@()})
        }elseif($case -cin @('C4-TARGETING-area-both','C4-TARGETING-area-unmounted')){
            $mutations+=@({param($e) $area=@($e.rows|Where-Object {$_.name.StartsWith('C4-TARGETING-area-')})[0].evidence;$area.unitsInside=@('rider')},
                {param($e) $area=@($e.rows|Where-Object {$_.name.StartsWith('C4-TARGETING-area-')})[0].evidence;$area.firstSaves[1].actor='rider'},
                {param($e) $area=@($e.rows|Where-Object {$_.name.StartsWith('C4-TARGETING-area-')})[0].evidence;$area.allSaves+=@($area.firstSaves[0])})
            $areaMutations=@(
                {param($a) $a.allSaves[2].nativeSource.callbacks[0].kind='unit-enter';$a.allSaves[2].nativeSource.callbacks[0].token='06002ccd'},
                {param($a) $a.allSaves[2].nativeSource.callbacks[0].kind='unknown'},
                {param($a) $a.allSaves[2].nativeSource.callbacks[0].token='06002ccd'},
                {param($a) $a.allSaves[2].nativeSource.callbacks[0].assemblyMvid='wrong'},
                {param($a) $a.allSaves[2].nativeSource.area='other-area'},
                {param($a) $a.allSaves[2].nativeSource.areaBlueprint='other-blueprint'},
                {param($a) $a.allSaves[2].nativeSource.caster='other-caster'},
                {param($a) $a.allSaves[2].nativeSource.actorInside=$false},
                {param($a) $a.allSaves[2].nativeSource.callbacks=@()},
                {param($a) $a.allSaves[2].nativeSource.callbacks+=@($a.allSaves[2].nativeSource.callbacks[0])},
                {param($a) $a.allSaves[2].identity=100},
                {param($a) $a.allSaves[2].target='rider'},
                {param($a) $a.allSaves[2].actor='unrelated'},
                {param($a) $a.allSaves[2].kind='attack-roll'},
                {param($a) $a.allSaves[2].stat=1.5},
                {param($a) $a.before.riderReflex=3},
                {param($a) $a.before.mountReflex=8},
                {param($a) $a.firstSaves[0].roll=1},
                {param($a) $a.firstSaves[1]=$a.allSaves[2]},
                {param($a) $a.before.areaSlotAvailable=$false},
                {param($a) $a.state.areaSlotAvailable=$true},
                {param($a) $a.state.subjectDamage=1},
                {param($a) $a.state.otherDamage=1},
                {param($a) $a.blueprint='other-blueprint'},
                {param($a) $a.mounted=!$a.mounted},
                {param($a) $a.definition=@()},
                {param($a) $a.ruleDrops=1},
                {param($a) $a.allSaves=@($a.allSaves[0],$a.allSaves[2])},
                {param($a) $duplicate=$a.allSaves[2]|ConvertTo-Json -Depth 12|ConvertFrom-Json;$duplicate.identity=103;$a.allSaves+=@($duplicate)}
            )
            foreach($areaMutation in $areaMutations){
                $changed=New-CoreEnvelope $root;$area=@($changed.rows|Where-Object {$_.name.StartsWith('C4-TARGETING-area-')})[0].evidence
                & $areaMutation $area;$rejected=$false
                try{Assert-KmcChunk4CoreEvidence $request $changed 'PASS'}catch{$rejected=$true}
                if(!$rejected){throw "Invalid callback-qualified area envelope accepted for $root"};$passed++
            }
        }else{
            $mutations+=@({param($e) $hostile=@($e.rows|Where-Object {$_.name.EndsWith('-hostile')})[0].evidence;$hostile.hostileCommand.acted=$false},
                {param($e) $hostile=@($e.rows|Where-Object {$_.name.EndsWith('-hostile')})[0].evidence;$hostile.nativeRules.attacks[0].resolved=$false})
        }
    }
    foreach($mutation in $mutations){
        $changed=New-CoreEnvelope $root; & $mutation $changed; $rejected=$false
        try{Assert-KmcChunk4CoreEvidence $request $changed 'PASS'}catch{$rejected=$true}
        if(!$rejected){throw "Invalid core envelope accepted for ${root}: $mutation"};$passed++
    }
}
Write-Host "COMPONENT parser-only TOTAL PASS=$passed FAIL=0"

# Native AE crossed a real round boundary through the surviving Horse before
# the second unrelated actor. Parser fixtures retain strict callback accounting.
$root='chunk4-rider-death-tb';$native=New-CoreEnvelope $root;Add-CoreNativeSurvivorTurn $native
Assert-KmcChunk4CoreEvidence @{scenario=$root} $native 'PASS';$passed++
$survivorMutations=@(
    {param($e) $e.successorOrderBefore=@('other1','other2','mount')},
    {param($e) $e.successorTurns[1].actor='rider'},
    {param($e) $e.successorTurns[1].survivor=$false},
    {param($e) $e.successorTurns[1].round=2;$e.successorTurns[1].state.round=2},
    {param($e) $e.successorTurns[1].state.mountGrants=2},
    {param($e) $e.successorTurns[1].state.riderGrants=3},
    {param($e) $e.successorTurns[1].state.privatePartner='rider'},
    {param($e) $e.successorTurns[1].state.identity='new-pair'},
    {param($e) $e.successorTurns[1].endInput=$false},
    {param($e) $e.successorTurns[1].turn=0},
    {param($e) $e.successorTurns[1].state.rider.conscious=$true},
    {param($e) $e.successorTurns[1].state.rider.finallyDead=$false},
    {param($e) $e.successorTurns[1].frame=20},
    {param($e) $e.unrelatedTurns[1].round=2},
    {param($e) $e.finalLife.mountGrants=2},
    {param($e) $e.afterPolicyRestore.mountGrants=4},
    {param($e) $e.allocationSequenceBeforeDamage=2},
    {param($e) $e.allocationSequenceBeforeDamage=$null},
    {param($e) $e.allocationTrace.dropped=1},
    {param($e) $e.allocationTrace.observationErrors=@('failed')},
    {param($e) $e.allocationTrace.observationErrors=1},
    {param($e) $e.allocationTrace.observationErrors=-1},
    {param($e) $e.allocationTrace.observationErrors=$null},
    {param($e) $e.allocationTrace.observationErrors='0'},
    {param($e) $e.allocationTrace.observationErrors=0.5},
    {param($e) $e.allocationTrace.observationErrors=@()},
    {param($e) $e.allocationTrace.events=@($e.allocationTrace.events[2])},
    {param($e) $e.allocationTrace.events+=@($e.allocationTrace.events[2])},
    {param($e) $e.allocationTrace.events[2].state.actor='rider'},
    {param($e) $e.allocationTrace.events[2].currentActor='rider'},
    {param($e) $e.allocationTrace.events[2].boundary='prepare-after'},
    {param($e) $e.allocationTrace.events[2].sequence=5},
    {param($e) $e.allocationTrace.events[2].turn=14},
    {param($e) $e.allocationTrace.events[2].preparingTurn=14},
    {param($e) $e.allocationTrace.events[2].round=2},
    {param($e) $e.allocationTrace.events[2].frame=9},
    {param($e) $e.allocationTrace.events[2].frame=26},
    {param($e) $e.allocationTrace.events[2].simulatingClick=$true},
    {param($e) $e.allocationTrace.events[2].activationIdentity='new-pair'},
    {param($e) $e.allocationTrace.events[2].state.pairedGrantIdentity='activation'},
    {param($e) $e.allocationTrace.events[2].state.grantSequence=4},
    {param($e) $e.allocationTrace.events[2].state.prepared=$false},
    {param($e) $e.allocationTrace.events[2].state.canAct=$false}
)
foreach($mutation in $survivorMutations){
    $changed=New-CoreEnvelope $root;Add-CoreNativeSurvivorTurn $changed
    & $mutation $changed.rows[0].evidence;$rejected=$false
    try{Assert-KmcChunk4CoreEvidence @{scenario=$root} $changed 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw "Invalid survivor preparation envelope accepted: $mutation"};$passed++
}
Write-Host "COMPONENT with native survivor-order fixture TOTAL PASS=$passed FAIL=0"

# AG's first unrelated actor was the native enemy, whose turn ended without
# player input. Its actual TurnController ending callbacks remain required.
$native=New-CoreEnvelope $root;Add-CoreNativeEnemyTurn $native
Assert-KmcChunk4CoreEvidence @{scenario=$root} $native 'PASS';$passed++
$enemyEndMutations=@(
    {param($e) $e.successorTurns[0].endInput=$true},
    {param($e) $e.enemyBeforeDamage.directlyControllable=$true},
    {param($e) $e.enemyBeforeDamage.effectiveAiEnabled=$false},
    {param($e) $e.source='rider'},
    {param($e) $e.enemyBeforeDamage.id='wrong-actor'},
    {param($e) $e.allocationTrace.events=@()},
    {param($e) $e.allocationTrace.events+=@($e.allocationTrace.events[0])},
    {param($e) $e.allocationTrace.events[0].state.actor='rider'},
    {param($e) $e.allocationTrace.events[0].currentActor='rider'},
    {param($e) $e.allocationTrace.events[0].turn=99},
    {param($e) $e.allocationTrace.events[0].round=3},
    {param($e) $e.allocationTrace.events[0].turnStatus='Acting'},
    {param($e) $e.allocationTrace.events[1].turnStatus='Ending'},
    {param($e) $e.allocationTrace.events[0].boundary='turn-end-after'},
    {param($e) $e.allocationTrace.events[0].sequence=3},
    {param($e) $e.allocationTrace.events[0].frame=19},
    {param($e) $e.allocationTrace.events[1].frame=31},
    {param($e) $e.allocationTrace.events[0].frame=29},
    {param($e) $e.allocationTrace.events[0].simulatingClick=$true},
    {param($e) $e.allocationTrace.events[0].activationIdentity='new-pair'}
)
foreach($mutation in $enemyEndMutations){
    $changed=New-CoreEnvelope $root;Add-CoreNativeEnemyTurn $changed
    & $mutation $changed.rows[0].evidence;$rejected=$false
    try{Assert-KmcChunk4CoreEvidence @{scenario=$root} $changed 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw "Invalid native enemy ending accepted: $mutation"};$passed++
}
Write-Host "COMPONENT with native enemy-turn fixture TOTAL PASS=$passed FAIL=0"

# AH confirmed the accepted same-round mount exclusion after native rider death.
# The survivor keeps its next-round eligibility and cannot receive another grant.
$native=New-CoreEnvelope $root
$life=$native.rows[0].evidence;$life.eligibleRosterBeforeDamage=@('other2','rider','mount','other1')
$life.principalRosterIndex=1;$life.successorOrderBefore=@('other1','other2');$life.sameRoundMountExcluded=$true
Assert-KmcChunk4CoreEvidence @{scenario=$root} $native 'PASS';$passed++
$sameRoundMutations=@(
    {param($e) $e.sameRoundMountExcluded=$false},
    {param($e) $e.beforeDamage.privatePartner=$null},
    {param($e) $e.beforeDamage.identity=$null},
    {param($e) $e.beforeDamage.mountGrants=0},
    {param($e) $e.successorOrderBefore=@('mount','other1','other2')},
    {param($e) $e.allocationTrace.events+=@([pscustomobject]@{boundary='prepare-before';sequence=3;state=[pscustomobject]@{actor='mount'}})},
    {param($e) $e.successorTurns[1].state.mountGrants=3}
)
foreach($mutation in $sameRoundMutations){
    $changed=$native|ConvertTo-Json -Depth 30|ConvertFrom-Json
    & $mutation $changed.rows[0].evidence;$rejected=$false
    try{Assert-KmcChunk4CoreEvidence @{scenario=$root} $changed 'PASS'}catch{$rejected=$true}
    if(!$rejected){throw "Invalid same-round mount participation accepted: $mutation"};$passed++
}
$changed=New-CoreEnvelope $root;Add-CoreNativeSurvivorTurn $changed
$changed.rows[0].evidence.sameRoundMountExcluded=$true;$rejected=$false
try{Assert-KmcChunk4CoreEvidence @{scenario=$root} $changed 'PASS'}catch{$rejected=$true}
if(!$rejected){throw 'Next-round survivor was incorrectly treated as an already consumed native slot.'};$passed++
Write-Host "COMPONENT with consumed mount-slot fixture TOTAL PASS=$passed FAIL=0"
