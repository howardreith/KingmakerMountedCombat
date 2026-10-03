Set-StrictMode -Version Latest
# Chunk 6B increment 6B.1: the pair forced-path measurement (chunk6b-charge-path-rt / -tb). The
# compiled scenario records facts and checks its own structure only; this reader is the one acceptance
# authority for the two measurement rows. Read-only over the immutable artifact; no runtime mutation.
function Get-KmcChunk6bChargePathScenarios { @('chunk6b-charge-path-rt','chunk6b-charge-path-tb') }
function Get-KmcChunk6bChargePathRows { @('C6B-PATH-straight-arrival','C6B-PATH-interrupt-stop') }
function Test-KmcChunk6bChargePathScenario([string]$Scenario) { [string]$Scenario -cin (Get-KmcChunk6bChargePathScenarios) }
function ChargePathProp($Object,[string]$Name) { if($null-ne$Object-and$null-ne$Object.PSObject.Properties[$Name]){$Object.$Name}else{$null} }
function ChargePathFail([string]$Message) { throw ('Chunk 6B charge path: '+$Message) }
function ChargePathNumber($Value) { if($null-eq$Value){return $false}; try{$d=[double]$Value}catch{return $false}; -not([double]::IsNaN($d)-or[double]::IsInfinity($d)) }
function Assert-KmcChunk6bChargePathRow($Row,[string]$Mode) {
    $e=$Row.evidence
    if($null-eq$e){ChargePathFail ('row '+$Row.name+' has no evidence')}
    if([string](ChargePathProp $e 'level')-cne'NATIVE MEASUREMENT'-or[string](ChargePathProp $e 'mode')-cne$Mode-or[string](ChargePathProp $e 'case')-cne[string]$Row.name){ChargePathFail ('row '+$Row.name+' level, mode or case differs')}
    if((ChargePathProp $e 'mounted')-ne$true-or(ChargePathProp $e 'commandsObservedEmptyThroughout')-ne$true){ChargePathFail ('row '+$Row.name+' was not a mounted pair with empty command containers throughout')}
    $g=ChargePathProp $e 'geometry';$lease=ChargePathProp $e 'lease';$stop=ChargePathProp $e 'stop';$after=ChargePathProp $e 'after';$restoration=ChargePathProp $e 'restoration';$costs=ChargePathProp $e 'costs';$reach=ChargePathProp $e 'reach';$before=ChargePathProp $e 'before'
    foreach($part in @($g,$lease,$stop,$after,$restoration,$costs,$reach,$before)){ if($null-eq$part){ChargePathFail ('row '+$Row.name+' lacks a measurement section')} }
    # The fixture must be a lawful charge geometry read from the mount's origin: straight, clear, inside the
    # stock minimum and maximum range for the mount's speed and corpulence.
    foreach($name in @('distance3D','minimumRange','maximumRange','mountCorpulence','targetCorpulence','mountCombatSpeedMps')){ if(-not(ChargePathNumber (ChargePathProp $g $name))){ChargePathFail ('row '+$Row.name+' geometry '+$name+' is not a finite number')} }
    if((ChargePathProp $g 'straightRoute')-ne$true-or(ChargePathProp $g 'customCanTarget')-ne$true-or(ChargePathProp $g 'landingBlocked')-ne$false){ChargePathFail ('row '+$Row.name+' geometry is not a straight clear admissible charge line')}
    if([double]$g.distance3D-lt[double]$g.minimumRange-or[double]$g.distance3D-gt[double]$g.maximumRange){ChargePathFail ('row '+$Row.name+' distance is outside the charge range')}
    if([Math]::Abs([double]$g.maximumRange-[double]$g.mountCombatSpeedMps*6)-gt0.001){ChargePathFail ('row '+$Row.name+' maximum range is not six times the mount combat speed')}
    # The lease applied exactly the stock calls on the mount agent and nothing else.
    if((ChargePathProp $lease 'forcePathApplied')-ne$true-or(ChargePathProp $lease 'chargingApplied')-ne$true-or-not(ChargePathNumber (ChargePathProp $lease 'speedOverrideApplied'))){ChargePathFail ('row '+$Row.name+' lease did not apply the forced path, the charging flag and the speed override')}
    if([double]$lease.speedOverrideApplied-lt([double]$g.mountCombatSpeedMps*2-0.001)){ChargePathFail ('row '+$Row.name+' speed override is below twice the mount combat speed')}
    if((ChargePathProp $lease 'riderAgentTouched')-ne$false){ChargePathFail ('row '+$Row.name+' touched the rider agent')}
    $samples=@(ChargePathProp $e 'samples')
    if($samples.Count-lt3){ChargePathFail ('row '+$Row.name+' recorded fewer than three samples')}
    $previousForced=-1.0
    foreach($s in $samples){
        if((ChargePathProp $s 'riderCommandsEmpty')-ne$true-or(ChargePathProp $s 'mountCommandsEmpty')-ne$true){ChargePathFail ('row '+$Row.name+' a command appeared during the path')}
        if((ChargePathProp $s 'charging')-ne$true){ChargePathFail ('row '+$Row.name+' the mount agent lost its charging flag during the path')}
        if($Mode-ceq'TB'){
            $forced=ChargePathProp $s 'turnTimeForced'
            if(-not(ChargePathNumber $forced)-or[double]$forced-lt$previousForced){ChargePathFail ('row '+$Row.name+' forced turn time is missing or decreased')}
            $previousForced=[double]$forced
        }
    }
    # No native cost: the measurement is not an action. Every cooldown delta is zero on both actors.
    foreach($name in @('riderStandardDelta','riderMoveDelta','mountStandardDelta','mountMoveDelta')){ $v=ChargePathProp $costs $name; if(-not(ChargePathNumber $v)-or[Math]::Abs([double]$v)-gt0.0001){ChargePathFail ('row '+$Row.name+' charged a native cooldown: '+$name)} }
    # Exact restoration of every leased value and no force-mode residue on the mount agent.
    if((ChargePathProp $restoration 'chargingRestored')-ne$true-or(ChargePathProp $restoration 'speedOverrideRestored')-ne$true-or(ChargePathProp $restoration 'forceModeCleared')-ne$true){ChargePathFail ('row '+$Row.name+' did not restore the lease exactly')}
    if((ChargePathProp $after 'forceMode')-ne$false-or(ChargePathProp $after 'mountMoving')-ne$false-or(ChargePathProp $after 'descriptorCharging')-ne$false){ChargePathFail ('row '+$Row.name+' left force mode, movement or a charging state behind')}
    if((ChargePathProp $e 'attackRules')-ne0){ChargePathFail ('row '+$Row.name+' observed an attack rule')}
    foreach($name in @('movedDistance','maximumLateralDeviation','elapsedSeconds','peakSpeedMps','distanceToTarget')){ if(-not(ChargePathNumber (ChargePathProp $stop $name))){ChargePathFail ('row '+$Row.name+' stop '+$name+' is not a finite number')} }
    if([double]$stop.maximumLateralDeviation-gt0.75){ChargePathFail ('row '+$Row.name+' left the straight line by more than 0.75 m')}
    if($Mode-ceq'TB'){
        $bt=ChargePathProp $before 'turn';$at=ChargePathProp $after 'turn'
        if($null-eq$bt-or$null-eq$at-or(ChargePathProp $bt 'isRider')-ne$true-or(ChargePathProp $at 'isRider')-ne$true){ChargePathFail ('row '+$Row.name+' did not run on the rider turn')}
        # The Acting entry is a native five-foot step (no Move action); any other movement before the path is refused.
        if(-not(ChargePathNumber (ChargePathProp $bt 'timeMoved'))-or-not(ChargePathNumber (ChargePathProp $bt 'timeMovedByFiveFootStep'))-or[Math]::Abs([double]$bt.timeMoved-[double]$bt.timeMovedByFiveFootStep)-gt0.0001){ChargePathFail ('row '+$Row.name+' the rider turn had moved before the path other than by its five-foot step')}
        if(-not(ChargePathNumber (ChargePathProp $at 'timeMovedInForceMode'))-or[double]$at.timeMovedInForceMode-le0){ChargePathFail ('row '+$Row.name+' the rider turn recorded no forced movement')}
        if(-not(ChargePathNumber (ChargePathProp $at 'timeMoved'))-or[Math]::Abs(([double]$at.timeMoved-[double]$bt.timeMoved)-[double]$at.timeMovedInForceMode)-gt0.0001){ChargePathFail ('row '+$Row.name+' the rider turn recorded movement outside force mode during the path')}
    }
    switch -CaseSensitive ([string]$Row.name) {
        'C6B-PATH-straight-arrival' {
            if([string](ChargePathProp $stop 'reason')-cne'arrival'-or(ChargePathProp $reach 'arrivalWithinReach')-ne$true){ChargePathFail 'the straight path did not arrive within the pair reach'}
            if([double]$stop.movedDistance-lt1.0){ChargePathFail 'the straight path moved less than one metre'}
            if([double]$stop.peakSpeedMps-lt([double]$g.mountCombatSpeedMps*1.2)){ChargePathFail 'the straight path never exceeded the mount walking combat speed'}
        }
        'C6B-PATH-interrupt-stop' {
            if([string](ChargePathProp $stop 'reason')-cne'interrupt'-or(ChargePathProp $reach 'arrivalWithinReach')-ne$false){ChargePathFail 'the interrupted path was not stopped before arrival'}
            if([double]$stop.movedDistance-le0.0){ChargePathFail 'the interrupted path recorded no movement before the stop'}
            if(-not(ChargePathNumber (ChargePathProp $stop 'settleSeconds'))-or[double]$stop.settleSeconds-gt1.5){ChargePathFail 'the interrupted path did not settle within 1.5 s'}
        }
        default { ChargePathFail ('unknown row '+$Row.name) }
    }
}
function Assert-KmcChunk6bChargePathEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if([long]$Artifact.schemaVersion-ne33-or-not(Test-KmcChunk6bChargePathScenario ([string]$Request.scenario))){ChargePathFail 'requires schema 33 and a chunk6b-charge-path scenario'}
    $mode=if([string]$Request.scenario-ceq'chunk6b-charge-path-tb'){'TB'}else{'RT'}
    $measurement=ChargePathProp $Artifact.observations 'chunk6bChargePath'
    if($null-eq$measurement-or[string](ChargePathProp $measurement 'contract')-cne'chunk6b-pair-forced-path-measurement'-or[string](ChargePathProp $measurement 'mode')-cne$mode){ChargePathFail 'the measurement contract or mode is absent or differs'}
    $required=Get-KmcChunk6bChargePathRows
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in @($Artifact.rows)){
        if([string]$row.name-cnotin($required+$failureOnly)-or-not$names.Add([string]$row.name)-or[string]$row.status-cnotin@('PASS','FAIL')){ChargePathFail ('invalid or duplicate row '+$row.name)}
        if([string]$row.status-ceq'FAIL'){$fail++;continue}
        $pass++
        if([string]$row.name-cin$failureOnly){ChargePathFail ('failure-only row claimed PASS: '+$row.name)}
        Assert-KmcChunk6bChargePathRow $row $mode
    }
    if($Status-ceq'PASS'){
        foreach($name in $required){ if(-not$names.Contains($name)){ChargePathFail ('required row absent: '+$name)} }
        if($fail-ne0){ChargePathFail 'a PASS artifact carries a failed row'}
    }
    if([long]$Artifact.subscenarioPassCount-ne$pass-or[long]$Artifact.subscenarioFailCount-ne$fail){ChargePathFail 'row counts differ from the artifact summary'}
}
