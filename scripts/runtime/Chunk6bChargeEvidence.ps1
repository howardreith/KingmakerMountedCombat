Set-StrictMode -Version Latest
# Chunk 6B increment 6B.2: the real-time delivery of the pair-owned Mounted Charge (chunk6b-charge-rt).
# The compiled scenario records facts and checks its own structure only; this reader is the one acceptance
# authority for the five delivery rows. Read-only over the immutable artifact; no runtime mutation.
#
# The contract it enforces, in the owner's terms: the rider owns the native full-round Standard shell and
# therefore the cost; the mount owns the forced path and is never charged for carrying; exactly one
# rider-owned attack carries the native charge rule; every refusal happens before any cost, path or attack;
# the feature is absent while its setting is off; the stock Charge stays rejected while mounted; and the
# charge lease is restored exactly with no residue beyond the native buff duration.
function Get-KmcChunk6bChargeScenarios { @('chunk6b-charge-rt','chunk6b-charge-tb') }
function Get-KmcChunk6bChargeMode([string]$Scenario) { if([string]$Scenario-ceq'chunk6b-charge-tb'){'TB'}else{'RT'} }
# Real time carries the whole 6B.2 row set. Turn-based carries the delivery and refusal core: the
# repeated request and the two lifecycle interventions belong to a live real-time path.
function Get-KmcChunk6bChargeRows([string]$Mode) {
    if([string]$Mode-ceq'TB'){
        @('C6B-CHARGE-default-off','C6B-CHARGE-positive','C6B-CHARGE-below-minimum','C6B-CHARGE-stock-rejected')
    } else {
        @('C6B-CHARGE-default-off','C6B-CHARGE-positive','C6B-CHARGE-below-minimum','C6B-CHARGE-spent-standard','C6B-CHARGE-stock-rejected','C6B-CHARGE-interrupted','C6B-CHARGE-combat-ended')
    }
}
function Test-KmcChunk6bChargeScenario([string]$Scenario) { [string]$Scenario -cin (Get-KmcChunk6bChargeScenarios) }
function ChargeProp($Object,[string]$Name) { if($null-ne$Object-and$null-ne$Object.PSObject.Properties[$Name]){$Object.$Name}else{$null} }
function ChargeFail([string]$Message) { throw ('Chunk 6B charge: '+$Message) }
function ChargeNumber($Value) { if($null-eq$Value){return $false}; try{$d=[double]$Value}catch{return $false}; -not([double]::IsNaN($d)-or[double]::IsInfinity($d)) }
function ChargeKmcAbilityGuid { 'd79eaec224a7a832e738eb81baef9d49' }
function ChargeStockAbilityGuid { 'c78506dd0e14f7c45a599990e4e65038' }

# The KMC charge ability must be the original one: the exact asset id, a Standard full-round action whose
# component is the KMC logic and never the stock AbilityCustomCharge, and the stock Charge must still exist
# on the rider with its own stock logic, because Chunk 6B must not replace or patch it.
function Assert-KmcChunk6bChargeIdentity($Identity,[string]$Row,[bool]$ExpectPresent) {
    if($null-eq$Identity){ChargeFail ('row '+$Row+' has no ability identity')}
    if((ChargeProp $Identity 'kmcChargePresent')-ne$ExpectPresent){ChargeFail ('row '+$Row+' KMC charge presence differs from the setting')}
    if((ChargeProp $Identity 'stockChargePresent')-ne$true-or(ChargeProp $Identity 'stockChargeIsStockLogic')-ne$true){ChargeFail ('row '+$Row+' the stock Charge ability is absent or no longer stock')}
    if(-not$ExpectPresent){ return }
    if([string](ChargeProp $Identity 'kmcChargeBlueprint')-cne(ChargeKmcAbilityGuid)){ChargeFail ('row '+$Row+' the KMC charge asset id differs')}
    if([string](ChargeProp $Identity 'kmcChargeActionType')-cne'Standard'){ChargeFail ('row '+$Row+' the KMC charge is not a standard action')}
    if((ChargeProp $Identity 'kmcChargeFullRound')-ne$true-or(ChargeProp $Identity 'kmcChargeRequiresFullRound')-ne$true){ChargeFail ('row '+$Row+' the KMC charge is not a full-round action')}
    if([string](ChargeProp $Identity 'kmcChargeComponent')-cne'KingmakerMountedCombat.Integration.MountedChargeAbilityLogic'){ChargeFail ('row '+$Row+' the KMC charge component is not the KMC charge logic')}
    if((ChargeProp $Identity 'kmcChargeIsStockLogic')-ne$false){ChargeFail ('row '+$Row+' the KMC charge carries the stock charge component')}
    if(-not(ChargeNumber (ChargeProp $Identity 'kmcChargeMinRange'))-or[double]$Identity.kmcChargeMinRange-le0){ChargeFail ('row '+$Row+' the KMC charge reports no minimum range')}
}

# Nothing happened: no shell was admitted, the pair did not move, no cooldown was spent and no pair attack
# rule was initiated. Every economy figure is the increase the row own attempt caused above its own baseline,
# so a repeated request made while an earlier charge cost still stands reports zero rather than that cost.
# Every refusal row requires exactly this.
function Assert-KmcChunk6bChargeNothingHappened($Evidence,[string]$Row) {
    $input=ChargeProp $Evidence 'input';$movement=ChargeProp $Evidence 'movement';$economy=ChargeProp $Evidence 'economy'
    if($null-eq$input-or$null-eq$movement-or$null-eq$economy){ChargeFail ('row '+$Row+' lacks a delivery section')}
    # Refusals never reach the controller delivery: no admission, and no dispatch refusal either, because the
    # request is refused by availability or targeting before a native shell exists.
    $refusalDelivery=ChargeProp $Evidence 'delivery'
    if($null-eq$refusalDelivery){ChargeFail ('row '+$Row+' recorded no post-settlement delivery section')}
    foreach($name in @('chargeAdmitted','chargeRefused')){ if([long](ChargeProp $refusalDelivery $name)-ne0){ChargeFail ('row '+$Row+' reached the controller delivery on a refusal: '+$name)} }
    if([long](ChargeProp $input 'shellCount')-ne0){ChargeFail ('row '+$Row+' admitted a native ability shell')}
    $shell=ChargeProp $input 'shell'
    if($null-ne$shell-and(ChargeProp $shell 'present')-eq$true){ChargeFail ('row '+$Row+' admitted a native ability shell')}
    foreach($name in @('mountDistance','riderDistance')){ if(-not(ChargeNumber (ChargeProp $movement $name))-or[double]$movement.$name-gt0.25){ChargeFail ('row '+$Row+' moved the pair on a refusal')} }
    foreach($name in @('riderStandardMax','riderMoveMax','mountStandardMax','mountMoveMax')){ if(-not(ChargeNumber (ChargeProp $economy $name))-or[double]$economy.$name-gt0.001){ChargeFail ('row '+$Row+' spent a native action on a refusal: '+$name)} }
    if([long](ChargeProp $Evidence 'attackRules')-ne0){ChargeFail ('row '+$Row+' initiated a pair attack on a refusal')}
    if($null-ne(ChargeProp $Evidence 'lease')){ChargeFail ('row '+$Row+' applied a charge lease on a refusal')}
    if((ChargeProp (ChargeProp $Evidence 'after') 'mountCharging')-ne$false-or(ChargeProp (ChargeProp $Evidence 'after') 'riderStateCharging')-ne$false){ChargeFail ('row '+$Row+' left charge state behind on a refusal')}
}

# A charge that was lawfully admitted, genuinely carried and then ended through a native surface. The
# engine keeps what it has already taken: the rider standard action stays spent, the mount is charged
# nothing, every leased value is restored exactly and the pair delivers no attack of its own.
function Assert-KmcChunk6bChargeBoundedTermination($Evidence,[string]$Row,[string]$Kind) {
    $delivery=ChargeProp $Evidence 'delivery';$movement=ChargeProp $Evidence 'movement';$economy=ChargeProp $Evidence 'economy'
    $lease=ChargeProp $Evidence 'lease';$rules=ChargeProp $Evidence 'rules';$after=ChargeProp $Evidence 'after'
    $intervention=ChargeProp $Evidence 'intervention';$terminal=ChargeProp $Evidence 'terminal'
    foreach($part in @($delivery,$movement,$economy,$lease,$rules,$after,$intervention,$terminal)){ if($null-eq$part){ChargeFail ('row '+$Row+' lacks a termination section')} }
    if([long](ChargeProp $delivery 'chargeAdmitted')-ne1){ChargeFail ('row '+$Row+' did not admit exactly one charge')}
    if([long](ChargeProp $delivery 'chargeRefused')-ne0){ChargeFail ('row '+$Row+' refused the charge it admitted')}
    if($null-ne(ChargeProp $delivery 'lastRefusal')){ChargeFail ('row '+$Row+' left a charge refusal reason behind')}
    # The pair committed before the intervention: the mount carried the forced path at charge speed.
    if(-not(ChargeNumber (ChargeProp $movement 'mountDistance'))-or[double]$movement.mountDistance-lt1.0){ChargeFail ('row '+$Row+' the mount never carried the charge before the intervention')}
    if((ChargeProp $movement 'chargingObserved')-ne$true){ChargeFail ('row '+$Row+' the mount agent was never observed charging')}
    if(-not(ChargeNumber (ChargeProp $movement 'peakSpeedMps'))-or-not(ChargeNumber (ChargeProp $movement 'mountCombatSpeedMps'))){ChargeFail ('row '+$Row+' the charge speed was not recorded')}
    if([double]$movement.peakSpeedMps-lt([double]$movement.mountCombatSpeedMps*1.2)){ChargeFail ('row '+$Row+' the charge never exceeded the mount walking combat speed')}
    # The engine owns the cost and keeps it: the shell paid, nothing was refunded, the mount paid nothing.
    if(-not(ChargeNumber (ChargeProp $economy 'riderStandardMax'))-or[double]$economy.riderStandardMax-le0.001){ChargeFail ('row '+$Row+' the rider standard action was never spent')}
    if(-not(ChargeNumber (ChargeProp $economy 'riderStandardNow'))-or[double]$economy.riderStandardNow-lt-0.001){ChargeFail ('row '+$Row+' recorded a negative rider standard cooldown')}
    foreach($name in @('mountStandardMax','mountMoveMax')){ if(-not(ChargeNumber (ChargeProp $economy $name))-or[double]$economy.$name-gt0.001){ChargeFail ('row '+$Row+' the mount was charged for carrying the charge: '+$name)} }
    # Exact restoration of every leased value, and the rider agent never touched.
    foreach($flag in @('applied','buffApplied','restored','chargingRestoredExactly','speedOverrideRestoredExactly','riderChargingRestoredExactly')){ if((ChargeProp $lease $flag)-ne$true){ChargeFail ('row '+$Row+' the charge lease flag is not set: '+$flag)} }
    if((ChargeProp $lease 'riderAgentTouched')-ne$false){ChargeFail ('row '+$Row+' the charge lease touched the rider agent')}
    if([long](ChargeProp $lease 'forcedPathCount')-lt1){ChargeFail ('row '+$Row+' the charge lease forced no path')}
    # No attack of the pair own: the intervention came before the strike.
    if([long](ChargeProp $rules 'pairNonOpportunityAttackRules')-ne0){ChargeFail ('row '+$Row+' delivered a pair attack after the intervention')}
    if([long](ChargeProp $rules 'mountAttackRules')-ne0){ChargeFail ('row '+$Row+' the mount initiated an attack')}
    if([long](ChargeProp $terminal 'childAttackStartCount')-ne0){ChargeFail ('row '+$Row+' the terminal records a started child attack')}
    # No residue of any kind.
    foreach($flag in @('mountCharging','mountMoving','riderStateCharging','mountStateCharging','pairCommandActive')){ if((ChargeProp $after $flag)-ne$false){ChargeFail ('row '+$Row+' left residue: '+$flag)} }
    if($null-ne(ChargeProp $after 'mountSpeedOverride')){ChargeFail ('row '+$Row+' left a mount speed override behind')}
    # The intervention is recorded exactly, and it really was an intervention into a live committed charge.
    if([string](ChargeProp $intervention 'kind')-cne$Kind){ChargeFail ('row '+$Row+' recorded another intervention kind: '+(ChargeProp $intervention 'kind'))}
    if((ChargeProp $intervention 'pairCommandActiveBefore')-ne$true){ChargeFail ('row '+$Row+' intervened with no active pair command')}
    if(-not(ChargeNumber (ChargeProp $intervention 'mountDistanceAtIntervention'))-or[double]$intervention.mountDistanceAtIntervention-lt1.0){ChargeFail ('row '+$Row+' intervened before the charge had carried the pair')}
    if(-not(ChargeNumber (ChargeProp $intervention 'riderStandardBefore'))-or[double]$intervention.riderStandardBefore-le0.001){ChargeFail ('row '+$Row+' the rider standard action was not already spent at the intervention')}
    if(-not(ChargeNumber (ChargeProp $intervention 'riderStandardAfter'))){ChargeFail ('row '+$Row+' recorded no rider standard cooldown after the intervention')}
    # While combat continues nothing may give the spent action back. When combat itself ends, Kingmaker
    # clears its own combat cooldowns, which preview.162 measured directly (5.799 s to 0 as the rider left
    # combat, against 5.798 s unchanged for the interrupt), so the assertable fact there is that the rider
    # really did leave combat rather than that the cooldown survived.
    if((ChargeProp $intervention 'riderInCombatAfter')-eq$true){
        if([double]$intervention.riderStandardAfter-lt([double]$intervention.riderStandardBefore-0.2)){ChargeFail ('row '+$Row+' the intervention refunded the rider standard action while combat continued')}
    } else {
        if((ChargeProp $intervention 'riderInCombatBefore')-ne$true){ChargeFail ('row '+$Row+' the intervention left combat without having been in combat')}
    }
}

function Assert-KmcChunk6bChargeRow($Row,[string]$Mode) {
    $e=$Row.evidence
    $name=[string]$Row.name
    if($null-eq$e){ChargeFail ('row '+$name+' has no evidence')}
    if([string](ChargeProp $e 'level')-cne'NATIVE DELIVERY'-or[string](ChargeProp $e 'mode')-cne$Mode-or[string](ChargeProp $e 'case')-cne$name){ChargeFail ('row '+$name+' level, mode or case differs')}
    if((ChargeProp $e 'mounted')-ne$true){ChargeFail ('row '+$name+' was not the mounted pair')}

    if($name-ceq'C6B-CHARGE-default-off'){
        $off=ChargeProp $e 'settingOff';$on=ChargeProp $e 'settingOn'
        if((ChargeProp $off 'setting')-ne$false){ChargeFail 'the default-off row did not start with the setting off'}
        Assert-KmcChunk6bChargeIdentity $off $name $false
        if((ChargeProp $on 'setting')-ne$true){ChargeFail 'the default-off row did not turn the setting on'}
        Assert-KmcChunk6bChargeIdentity $on $name $true
        if([string](ChargeProp $e 'abilityGuid')-cne(ChargeKmcAbilityGuid)){ChargeFail 'the default-off row names another ability'}
        return
    }

    $before=ChargeProp $e 'before';$input=ChargeProp $e 'input';$after=ChargeProp $e 'after'
    $movement=ChargeProp $e 'movement';$economy=ChargeProp $e 'economy';$rules=ChargeProp $e 'rules'
    foreach($part in @($before,$input,$after,$movement,$economy,$rules)){ if($null-eq$part){ChargeFail ('row '+$name+' lacks a measurement section')} }
    Assert-KmcChunk6bChargeIdentity (ChargeProp $before 'identity') $name $true
    Assert-KmcChunk6bChargeIdentity (ChargeProp $e 'identityAfter') $name $true

    switch -CaseSensitive ($name) {
        'C6B-CHARGE-positive' {
            if($Mode-ceq'TB'){
                # Increment 6B.3 is deferred with evidence, so a turn-based charge over a lawful geometry
                # must be refused outright, with the exact reason, before any cost, path or attack.
                if((ChargeProp $before 'available')-ne$false){ChargeFail 'the turn-based charge was available while increment 6B.3 is deferred'}
                if((ChargeProp $before 'canTarget')-ne$false){ChargeFail 'the turn-based charge was targetable while increment 6B.3 is deferred'}
                if([string](ChargeProp $before 'kmcAvailabilityReason')-cne'Mounted Charge is not yet supported in turn-based mode.'){ChargeFail ('the turn-based refusal reason differs: '+(ChargeProp $before 'kmcAvailabilityReason'))}
                $beforeTurn=ChargeProp (ChargeProp $before 'state') 'turn'
                if($null-eq$beforeTurn-or(ChargeProp $beforeTurn 'isRider')-ne$true){ChargeFail 'the turn-based refusal was not recorded on the rider own turn'}
                Assert-KmcChunk6bChargeNothingHappened $e $name
                return
            }
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the lawful charge was not available or targetable'}
            if((ChargeProp $before 'requireFullRound')-ne$true-or[string](ChargeProp $before 'commandType')-cne'Standard'){ChargeFail 'the lawful charge was not a full-round standard action'}

            if((ChargeProp $input 'clicked')-ne$true-or(ChargeProp $input 'hoverPure')-ne$true){ChargeFail 'the lawful charge was not admitted by a pure player click'}
            if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail 'the lawful charge did not admit exactly one native shell'}
            # The native shell delivers on a later frame than the click, so admission is a post-settlement
            # fact: exactly one admitted charge for this attempt and no dispatch refusal.
            $delivery=ChargeProp $e 'delivery'
            if($null-eq$delivery){ChargeFail 'the lawful charge recorded no post-settlement delivery section'}
            if([long](ChargeProp $delivery 'chargeAdmitted')-ne1){ChargeFail 'the controller did not admit exactly one charge'}
            if([long](ChargeProp $delivery 'chargeRefused')-ne0){ChargeFail 'the controller refused the charge it was asked to deliver'}
            # LastRejectionCodes and LastFeedback are the controller's shared last-values for every mounted
            # interaction, so they are recorded as context and never asserted per row: preview.160 measured
            # the same stale code on rows where no charge was attempted at all. The charge-specific
            # counters and the charge-specific refusal reason above are the assertable facts.
            # An empty array cannot survive a function return in PowerShell, so the context is proven by the
            # property's presence rather than by its value.
            if($null-eq$delivery.PSObject.Properties['rejectionCodes']){ChargeFail 'the delivered charge recorded no rejection-code context'}
            if($null-ne(ChargeProp $delivery 'lastRefusal')){ChargeFail 'the delivered charge left a refusal reason behind'}
            if(@(ChargeProp $input 'rejectionCodes').Count-ne0){ChargeFail 'the lawful charge reported a rejection code'}
            # The mount is the mover, at charge speed, and the rider never moves under its own agent.
            if(-not(ChargeNumber (ChargeProp $movement 'mountDistance'))-or[double]$movement.mountDistance-lt1.0){ChargeFail 'the mount did not carry the charge'}
            if(-not(ChargeNumber (ChargeProp $movement 'mountCombatSpeedMps'))-or-not(ChargeNumber (ChargeProp $movement 'peakSpeedMps'))){ChargeFail 'the charge speed was not recorded'}
            if([double]$movement.peakSpeedMps-lt([double]$movement.mountCombatSpeedMps*1.2)){ChargeFail 'the charge never exceeded the mount walking combat speed'}
            if((ChargeProp $movement 'chargingObserved')-ne$true){ChargeFail 'the mount agent was never observed charging'}
            # The lease: the stock calls on the mount, the buff on the rider, exact restoration.
            $lease=ChargeProp $e 'lease'
            if($null-eq$lease){ChargeFail 'the lawful charge recorded no lease'}
            foreach($flag in @('applied','buffApplied','restored','chargingRestoredExactly','speedOverrideRestoredExactly','riderChargingRestoredExactly','chargingObservedThroughout')){ if((ChargeProp $lease $flag)-ne$true){ChargeFail ('the charge lease flag is not set: '+$flag)} }
            if((ChargeProp $lease 'riderAgentTouched')-ne$false){ChargeFail 'the charge lease touched the rider agent'}
            if([long](ChargeProp $lease 'forcedPathCount')-lt1){ChargeFail 'the charge lease forced no path'}
            if(-not(ChargeNumber (ChargeProp $lease 'speedOverrideApplied'))-or[double]$lease.speedOverrideApplied-lt([double]$movement.mountCombatSpeedMps*2-0.001)){ChargeFail 'the charge lease did not double the mount speed'}
            # The engine owns the cost: the rider's full-round shell spent the rider's action, and the mount
            # was never charged for carrying the rider.
            if(-not(ChargeNumber (ChargeProp $economy 'riderStandardMax'))-or[double]$economy.riderStandardMax-le0.001){ChargeFail 'the rider standard action was never spent'}
            foreach($name2 in @('mountStandardMax','mountMoveMax')){ if(-not(ChargeNumber (ChargeProp $economy $name2))-or[double]$economy.$name2-gt0.001){ChargeFail ('the mount was charged for carrying the charge: '+$name2)} }
            # Exactly one rider-owned attack, carrying the native charge rule.
            if([long](ChargeProp $rules 'pairNonOpportunityAttackRules')-ne1){ChargeFail 'the charge did not deliver exactly one pair attack'}
            if([long](ChargeProp $rules 'mountAttackRules')-ne0){ChargeFail 'the mount initiated an attack during the charge'}
            $events=@((ChargeProp $rules 'attackRuleEvents')|Where-Object {$null-ne$_-and(ChargeProp $_ 'attackOfOpportunity')-eq$false})
            if($events.Count-ne1){ChargeFail 'the charge attack-rule events do not describe exactly one deliberate attack'}
            if((ChargeProp $events[0] 'charge')-ne$true){ChargeFail 'the charge attack did not carry the native charge rule'}
            if((ChargeProp $events[0] 'fullAttack')-ne$false){ChargeFail 'the charge delivered a full attack'}
            $terminal=ChargeProp $e 'terminal'
            if($null-eq$terminal){ChargeFail 'the charge recorded no terminal outcome'}
            if([long](ChargeProp $terminal 'childAttackStartCount')-ne1-or(ChargeProp $terminal 'singleAttackMode')-ne$true-or(ChargeProp $terminal 'nativeFullAttack')-ne$false){ChargeFail 'the charge terminal is not one single rider attack'}
            # No residue beyond the native buff duration, which is the engine's own and is not shortened.
            foreach($flag in @('mountCharging','mountMoving','riderStateCharging','mountStateCharging','pairCommandActive')){ if((ChargeProp $after $flag)-ne$false){ChargeFail ('the charge left residue: '+$flag)} }
            if($null-ne(ChargeProp $after 'mountSpeedOverride')){ChargeFail 'the charge left a mount speed override behind'}
        }
        'C6B-CHARGE-below-minimum' {
            if((ChargeProp $before 'canTarget')-ne$false){ChargeFail 'a target inside the minimum charge distance was targetable'}
            $state=ChargeProp $before 'state'
            if(-not(ChargeNumber (ChargeProp $state 'distanceToTarget'))-or-not(ChargeNumber (ChargeProp $before 'minRangeMeters'))){ChargeFail 'the minimum-range row recorded no geometry'}
            if([double]$state.distanceToTarget-ge[double]$before.minRangeMeters){ChargeFail 'the minimum-range row target was not inside the minimum charge distance'}
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-spent-standard' {
            $state=ChargeProp $before 'state'
            $rider=ChargeProp $state 'rider'
            if(-not(ChargeNumber (ChargeProp $rider 'standard'))-or[double]$rider.standard-le0.001){ChargeFail 'the repeated-charge row did not start with a spent standard action'}
            if((ChargeProp $before 'available')-ne$false-and(ChargeProp $before 'canTarget')-ne$false){ChargeFail 'a charge without the rider standard action was both available and targetable'}
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-stock-rejected' {
            if([string](ChargeProp $input 'stockBlueprint')-cne(ChargeStockAbilityGuid)){ChargeFail 'the stock rejection row did not click the stock Charge'}
            $feedback=[string](ChargeProp $input 'feedback');$safety=[string](ChargeProp $input 'safetyFeedback')
            if((ChargeProp $input 'stockAvailable')-ne$false-and$feedback-cne$safety){ChargeFail 'the stock Charge was neither unavailable nor refused with its exact mounted reason'}
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-interrupted' {
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the interrupted charge was not available or targetable'}
            if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail 'the interrupted charge did not admit exactly one native shell'}
            Assert-KmcChunk6bChargeBoundedTermination $e $name 'native-command-interrupt'
            $intervention=ChargeProp $e 'intervention'
            if((ChargeProp $intervention 'riderInCombatAfter')-ne$true){ChargeFail 'the interrupted charge left combat instead of being interrupted inside it'}
        }
        'C6B-CHARGE-combat-ended' {
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the combat-end charge was not available or targetable'}
            if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail 'the combat-end charge did not admit exactly one native shell'}
            Assert-KmcChunk6bChargeBoundedTermination $e $name 'native-combat-end'
            $intervention=ChargeProp $e 'intervention'
            if((ChargeProp $intervention 'riderInCombatBefore')-ne$true){ChargeFail 'the combat-end row did not start in combat'}
            if((ChargeProp $intervention 'riderInCombatAfter')-ne$false){ChargeFail 'the combat-end row did not actually end the combat'}
        }
        default { ChargeFail ('unknown row '+$name) }
    }
}

function Assert-KmcChunk6bChargeEvidence {
    param($Request,$Artifact,[AllowNull()][string]$Status)
    if([long]$Artifact.schemaVersion-ne34-or-not(Test-KmcChunk6bChargeScenario ([string]$Request.scenario))){ChargeFail 'requires schema 34 and a chunk6b charge scenario'}
    $mode=Get-KmcChunk6bChargeMode ([string]$Request.scenario)
    $measurement=ChargeProp $Artifact.observations 'chunk6bCharge'
    if($null-eq$measurement-or[string](ChargeProp $measurement 'contract')-cne'chunk6b-pair-charge-delivery'-or[string](ChargeProp $measurement 'mode')-cne$mode){ChargeFail 'the delivery contract or mode is absent or differs'}
    if([string](ChargeProp $measurement 'abilityGuid')-cne(ChargeKmcAbilityGuid)){ChargeFail 'the delivery contract names another ability'}
    $required=Get-KmcChunk6bChargeRows $mode
    $failureOnly=@('phase3d-horse-tranche-cleanup','phase3d-horse-scenario-deadline','phase3d-horse-leaf-deadline','phase3d-horse-runtime-exception')
    $names=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $pass=0;$fail=0
    foreach($row in @($Artifact.rows)){
        if([string]$row.name-cnotin($required+$failureOnly)-or-not$names.Add([string]$row.name)-or[string]$row.status-cnotin@('PASS','FAIL')){ChargeFail ('invalid or duplicate row '+$row.name)}
        if([string]$row.status-ceq'FAIL'){$fail++;continue}
        $pass++
        if([string]$row.name-cin$failureOnly){ChargeFail ('failure-only row claimed PASS: '+$row.name)}
        Assert-KmcChunk6bChargeRow $row $mode
    }
    if($Status-ceq'PASS'){
        foreach($name in $required){ if(-not$names.Contains($name)){ChargeFail ('required row absent: '+$name)} }
        if($fail-ne0){ChargeFail 'a PASS artifact carries a failed row'}
        # The mod setting the fixture turned on is restored exactly, or the artifact is not a PASS.
        if((ChargeProp $measurement 'settingRestored')-ne$true-or(ChargeProp $measurement 'settingAfter')-ne(ChargeProp $measurement 'settingBefore')){ChargeFail 'the Mounted Charge setting was not restored exactly'}
    }
    if([long]$Artifact.subscenarioPassCount-ne$pass-or[long]$Artifact.subscenarioFailCount-ne$fail){ChargeFail 'row counts differ from the artifact summary'}
}
