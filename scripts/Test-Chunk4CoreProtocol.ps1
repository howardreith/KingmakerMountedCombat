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
        rider=@{id='rider';conscious=$true;dead=$false;damage=0;inState=$true;enabledRenderers=1};
        mount=@{id='mount';conscious=$true;dead=$false;damage=0;inState=$true;enabledRenderers=1}}
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
                liveCommandBefore=@{started=$true;finished=$false};unrelatedTurns=@(@{actor='other1'},@{actor='other2'});unrelatedOrderBefore=@('other1','other2');
                eligibleRosterBeforeDamage=@('other2','mount','rider','other1');principalRosterIndex=2;
                nativeLifeEvents=@{events=@(@{kind='native-life-state';actor=$subject})};nativeRules=@{dropped=0;events=@(@{kind='damage-after';target=$subject;damage=130})}}
            $e.afterCleanup=New-CoreLifeState $subject $incap $true
            $e.nativeEncounterExit=New-CoreLifeState $subject $incap $true
            $e.nativeEncounterExit.identity=$null;$e.nativeEncounterExit.actorRecords=0
            foreach($flag in @('playerCombat','riderCombat','mountCombat','tbActive','tbInitialized')){$e.nativeEncounterExit[$flag]=$false}
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
        if(!$rejected){throw "Invalid core envelope accepted for $root"};$passed++
    }
}
Write-Host "COMPONENT parser-only TOTAL PASS=$passed FAIL=0"
