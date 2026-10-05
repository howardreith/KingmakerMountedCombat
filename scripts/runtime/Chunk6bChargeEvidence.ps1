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
        @('C6B-CHARGE-default-off','C6B-CHARGE-positive','C6B-CHARGE-below-minimum','C6B-CHARGE-spent-standard','C6B-CHARGE-stock-rejected','C6B-CHARGE-interrupted','C6B-CHARGE-combat-ended','C6B-CHARGE-obstructed-line','C6B-CHARGE-blocked-clearance','C6B-CHARGE-cancelled','C6B-CHARGE-exception-cleanup','C6B-CHARGE-target-moved','C6B-CHARGE-target-lost','C6B-CHARGE-rider-incapacitated','C6B-CHARGE-mount-incapacitated')
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

# What the charge transaction recorded about itself. The order is not cosmetic: the transition
# revalidation asks whether the exact carrier still owns the mount Move slot, so it must have run while
# the carrier owned it; the attack-start revalidation asks the opposite, so it must have run after the
# release was proven; and the lease may not exist before exact carrier ownership does. A charge that
# struck must show that whole order, and no charge may end owing cleanup.
function Assert-KmcChunk6bChargeTransaction($Evidence,[string]$Row,[bool]$ExpectAttack) {
    $t=ChargeProp $Evidence 'transaction'
    if($null-eq$t){ChargeFail ('row '+$Row+' recorded no charge transaction')}
    if((ChargeProp $t 'chargeMode')-ne$true){ChargeFail ('row '+$Row+' the admitted command was not in charge mode')}
    if((ChargeProp $t 'sequenceLawful')-ne$true){ChargeFail ('row '+$Row+' took its charge steps out of order: '+[string](ChargeProp $t 'sequence'))}
    $sequence=[string](ChargeProp $t 'sequence')
    if($sequence-eq''){ChargeFail ('row '+$Row+' recorded no charge step order')}
    if((ChargeProp $t 'leaseRestored')-ne$true){ChargeFail ('row '+$Row+' the charge lease was not restored')}
    if((ChargeProp $t 'cleanupComplete')-ne$true){ChargeFail ('row '+$Row+' the charge lease cleanup is incomplete')}
    foreach($field in @('cleanupDebt','cleanupDebtAtEnd')){ if([string](ChargeProp $t $field)-ne''){ChargeFail ('row '+$Row+' left charge cleanup debt: '+$field+'='+[string](ChargeProp $t $field))} }
    if((ChargeProp $t 'leaseApplicationFailed')-ne$false){ChargeFail ('row '+$Row+' the charge lease application failed')}
    if([long](ChargeProp $t 'revalidationCount')-lt1){ChargeFail ('row '+$Row+' the charge never revalidated its own conditions')}
    if([string](ChargeProp $t 'revalidationPhases')-eq''){ChargeFail ('row '+$Row+' recorded no revalidation phases')}
    if(-not$ExpectAttack){
        if($sequence-like'*AttackStarted*'){ChargeFail ('row '+$Row+' started a native attack on a terminated charge')}
        return
    }
    if((ChargeProp $t 'carrierReleaseProven')-ne$true){ChargeFail ('row '+$Row+' struck without proving the carrier release')}
    if((ChargeProp $t 'revalidationFailed')-ne$false){ChargeFail ('row '+$Row+' struck after a failed revalidation')}
    if([long](ChargeProp $t 'revalidationCount')-lt3){ChargeFail ('row '+$Row+' struck with fewer than three revalidations')}
    foreach($step in @('CarrierAdmitted','CarrierOwnershipProven','InitialRevalidation','LeaseApplied','TransitionRevalidation','CarrierReleasedForAttack','CarrierReleaseProven','Arrived','AttackStartRevalidation','AttackStarted')){
        if(-not($sequence-like('*'+$step+'*'))){ChargeFail ('row '+$Row+' the charge step order is missing '+$step)}
    }
    # The three orderings the repair batch exists for, read out of the recorded order itself.
    if($sequence.IndexOf('TransitionRevalidation')-ge$sequence.IndexOf('CarrierReleasedForAttack')){ChargeFail ('row '+$Row+' released the carrier before the transition revalidation')}
    if($sequence.IndexOf('CarrierReleaseProven')-ge$sequence.IndexOf('AttackStartRevalidation')){ChargeFail ('row '+$Row+' revalidated the attack start before proving the carrier release')}
    if($sequence.IndexOf('CarrierOwnershipProven')-ge$sequence.IndexOf('LeaseApplied')){ChargeFail ('row '+$Row+' applied the charge lease before proving exact carrier ownership')}
}

# One real native RuleDealDamage into the exact nonlethal unconscious window, the charge terminating at
# once without an attack, and the fixture returning exactly the damage it dealt so that no later case
# starts from a state this row created.
function Assert-KmcChunk6bChargeIncapacityRow($Evidence,[string]$Row,[string]$Kind,[string]$InterventionKind) {
    $before=ChargeProp $Evidence 'before';$input=ChargeProp $Evidence 'input'
    if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail ('row '+$Row+' was not available or targetable')}
    if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail ('row '+$Row+' did not admit exactly one native shell')}
    Assert-KmcChunk6bChargeBoundedTermination $Evidence $Row $InterventionKind
    Assert-KmcChunk6bChargeTransaction $Evidence $Row $false
    $incapacity=ChargeProp $Evidence 'incapacity'
    if($null-eq$incapacity){ChargeFail ('row '+$Row+' recorded no incapacity')}
    if([string](ChargeProp $incapacity 'contract')-cne'one-native-ruledeal-damage-to-incapacitation-window-mid-charge'){ChargeFail ('row '+$Row+' names another incapacity contract')}
    if([string](ChargeProp $incapacity 'window')-cne'present'){ChargeFail ('row '+$Row+' claims a delivered incapacity without a window')}
    if([string](ChargeProp $incapacity 'subjectKind')-cne$Kind){ChargeFail ('row '+$Row+' incapacitated the other actor')}
    if([string](ChargeProp $incapacity 'subjectId')-eq''){ChargeFail ('row '+$Row+' did not identify its incapacity subject')}
    if([long](ChargeProp $incapacity 'damageDispatchCount')-ne1){ChargeFail ('row '+$Row+' dispatched more than one native damage rule')}
    if((ChargeProp $incapacity 'difficultyUnchanged')-ne$true){ChargeFail ('row '+$Row+' did not keep the native difficulty unchanged')}
    $window=ChargeProp $incapacity 'measurement'
    if($null-eq$window){ChargeFail ('row '+$Row+' recorded no incapacity window')}
    foreach($field in @('difficulty','hitPoints','constitution','temporaryHitPoints','damageBefore','desiredDamage','deathThreshold','requestedDamage','projectedDamage')){
        if(-not(ChargeNumber (ChargeProp $window $field))){ChargeFail ('row '+$Row+' incapacity window field is not a number: '+$field)}
    }
    if([double]$window.desiredDamage-le[double]$window.hitPoints){ChargeFail ('row '+$Row+' the requested window was not past the subject hit points')}
    if([double]$window.desiredDamage-ge[double]$window.deathThreshold){ChargeFail ('row '+$Row+' the requested window reached the death threshold')}
    if([long](ChargeProp $incapacity 'nativeDamageBeforeDifficulty')-ne[long]$window.requestedDamage){ChargeFail ('row '+$Row+' the native damage rule did not deal what was requested')}
    if([long](ChargeProp $incapacity 'damageAfter')-lt[long]$window.desiredDamage){ChargeFail ('row '+$Row+' the native damage did not reach the incapacity window')}
    if([long](ChargeProp $incapacity 'damageAfter')-ge[long]$window.deathThreshold){ChargeFail ('row '+$Row+' the native damage reached the death threshold')}
    # The subject is observably unconscious and alive, and the other actor of the pair is untouched.
    $afterDamage=ChargeProp $incapacity 'stateAfterDamage'
    if($null-eq$afterDamage){ChargeFail ('row '+$Row+' recorded no state after its damage')}
    $otherKind=if($Kind-ceq'mount'){'rider'}else{'mount'}
    $subjectAfter=ChargeProp $afterDamage $Kind
    $otherAfter=ChargeProp $afterDamage $otherKind
    if($null-eq$subjectAfter-or$null-eq$otherAfter){ChargeFail ('row '+$Row+' recorded no pair state after its damage')}
    if((ChargeProp $subjectAfter 'conscious')-ne$false){ChargeFail ('row '+$Row+' the subject was never unconscious')}
    if((ChargeProp $subjectAfter 'dead')-ne$false-or(ChargeProp $subjectAfter 'finallyDead')-ne$false){ChargeFail ('row '+$Row+' the subject was killed rather than incapacitated')}
    if((ChargeProp $otherAfter 'conscious')-ne$true-or(ChargeProp $otherAfter 'dead')-ne$false){ChargeFail ('row '+$Row+' the other actor of the pair was affected as well')}
    # Exactly one native life-state boundary for the subject, Conscious to Unconscious.
    $events=@((ChargeProp (ChargeProp $incapacity 'lifeEventsAtSettle') 'events')|Where-Object {$null-ne$_-and[string](ChargeProp $_ 'kind')-ceq'native-life-state'})
    $subjectEvents=@($events|Where-Object {[string](ChargeProp $_ 'actor')-ceq[string](ChargeProp $incapacity 'subjectId')})
    if($subjectEvents.Count-ne1){ChargeFail ('row '+$Row+' did not record exactly one native life-state boundary for its subject')}
    if([string](ChargeProp $subjectEvents[0] 'detail')-cne'Conscious'-or[string](ChargeProp $subjectEvents[0] 'lifeState')-cne'Unconscious'){ChargeFail ('row '+$Row+' the native life-state boundary was not Conscious to Unconscious')}
    $source=@(ChargeProp $subjectEvents[0] 'nativeSource')
    if($source.Count-lt1-or[string](ChargeProp $source[0] 'type')-cne'Kingmaker.Controllers.Units.UnitLifeController'){ChargeFail ('row '+$Row+' the life-state boundary did not come from the native life controller')}
    # The fixture returns exactly the damage it dealt, and waits for the engine to confirm it.
    $restore=ChargeProp $incapacity 'restore'
    if($null-eq$restore){ChargeFail ('row '+$Row+' recorded no restoration of the damage it dealt')}
    if((ChargeProp $restore 'restored')-ne$true){ChargeFail ('row '+$Row+' did not restore the damage it dealt')}
    if([long](ChargeProp $restore 'damageAfterRestore')-ne[long](ChargeProp $restore 'damageToRestore')){ChargeFail ('row '+$Row+' restored another damage value')}
    if((ChargeProp $restore 'conscious')-ne$true-or[string](ChargeProp $restore 'lifeState')-cne'Conscious'){ChargeFail ('row '+$Row+' left its subject unconscious')}
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
    # buffApplied is the historical fact that the native Charge buff was installed; buffOutstanding is
    # whether the lease still owns it. A restored lease must show both: applied, and no longer owned.
    if((ChargeProp $lease 'buffOutstanding')-ne$false){ChargeFail ('row '+$Row+' the charge lease still owns the native charge buff')}
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
    # A terminated charge still has to have taken its own steps in order and owed nothing at the end.
    Assert-KmcChunk6bChargeTransaction $Evidence $Row $false
}

function Assert-KmcChunk6bChargeRow($Row,[string]$Mode) {
    $e=$Row.evidence
    $name=[string]$Row.name
    if($null-eq$e){ChargeFail ('row '+$name+' has no evidence')}
    if([string](ChargeProp $e 'level')-cne'NATIVE DELIVERY'-or[string](ChargeProp $e 'mode')-cne$Mode-or[string](ChargeProp $e 'case')-cne$name){ChargeFail ('row '+$name+' level, mode or case differs')}
    if((ChargeProp $e 'mounted')-ne$true){ChargeFail ('row '+$name+' was not the mounted pair')}
    # A case that had to deliver and admitted nothing says so in its own evidence. Whatever else the
    # row contains, it did not observe what it set out to observe.
    if(-not[string]::IsNullOrEmpty([string](ChargeProp $e 'nonDelivery'))){ChargeFail ('row '+$name+' did not deliver: '+[string](ChargeProp $e 'nonDelivery'))}
    if((ChargeProp $e 'strandedShellInterrupted')-eq$true){ChargeFail ('row '+$name+' left a native shell the fixture had to interrupt itself')}
    if([long](ChargeProp $e 'clickRetries')-ne0){ChargeFail ('row '+$name+' needed '+[string](ChargeProp $e 'clickRetries')+' re-click(s): the first native shell never started')}

    if($name-ceq'C6B-CHARGE-default-off'){
        $off=ChargeProp $e 'settingOff';$on=ChargeProp $e 'settingOn'
        if((ChargeProp $off 'setting')-ne$false){ChargeFail 'the default-off row did not start with the setting off'}
        Assert-KmcChunk6bChargeIdentity $off $name $false
        if((ChargeProp $on 'setting')-ne$true){ChargeFail 'the default-off row did not turn the setting on'}
        Assert-KmcChunk6bChargeIdentity $on $name $true
        if([string](ChargeProp $e 'abilityGuid')-cne(ChargeKmcAbilityGuid)){ChargeFail 'the default-off row names another ability'}
        return
    }

    # A row the fixture area could not present is a measurement, and it must be explicit: the row carries
    # a limitation from a closed set, the sweep that proves it, and nothing else. A limitation is never a
    # delivered row, and each one has to prove the fixture did not simply pass over a usable candidate.
    $limitation=[string](ChargeProp $e 'limitation')
    if($limitation-ne''){
        switch -CaseSensitive ($limitation) {
            'no-obstructed-line-in-fixture-area' {
                if($name-cne'C6B-CHARGE-obstructed-line'){ChargeFail ('row '+$name+' claims the obstructed-line limitation')}
                if((ChargeProp $e 'obstructedLineReachable')-ne$false){ChargeFail 'the obstructed-line limitation does not record the geometry as unreachable'}
                $attempts=@(ChargeProp (ChargeProp $e 'placement') 'attempts')
                if($attempts.Count-lt1){ChargeFail 'the obstructed-line limitation records no placement sweep'}
                foreach($attempt in $attempts){ if((ChargeProp $attempt 'straightRoute')-ne$true){ChargeFail 'the obstructed-line limitation recorded an obstructed candidate it did not use'} }
            }
            'no-blocked-landing-in-fixture-area' {
                if($name-cne'C6B-CHARGE-blocked-clearance'){ChargeFail ('row '+$name+' claims the clearance limitation')}
                if((ChargeProp $e 'blockedClearanceReachable')-ne$false){ChargeFail 'the clearance limitation does not record the geometry as unreachable'}
                # The fixture places its own blocker, so the only lawful limitation is that the native
                # graph or the authorized spawn envelope refused the landing point, with its exact reason.
                $blocker=ChargeProp $e 'blocker'
                if($null-eq$blocker){ChargeFail 'the clearance limitation records no blocker placement attempt'}
                if((ChargeProp $blocker 'placed')-ne$false){ChargeFail 'the clearance limitation claims the blocker was placed after all'}
                if([string](ChargeProp $blocker 'reason')-eq''){ChargeFail 'the clearance limitation names no reason'}
                if($null-eq(ChargeProp $blocker 'wantedLanding')){ChargeFail 'the clearance limitation records no landing point it tried'}
            }
            'blocked-landing-not-reproducible-after-spawn' {
                if($name-cne'C6B-CHARGE-blocked-clearance'){ChargeFail ('row '+$name+' claims the clearance spawn limitation')}
                # The body was spawned and then the landing point was clear after all, or the line was
                # not straight: either way the row is not the clearance row and says so.
                $blockers=@(ChargeProp $e 'landingBlockers')
                $straight=(ChargeProp $e 'straightRouteAfterSpawn')-eq$true
                if($blockers.Count-gt0-and$straight){ChargeFail 'the clearance spawn limitation had a usable blocked landing after all'}
            }
            'native-incapacity-window-absent' {
                if($name-cnotin@('C6B-CHARGE-rider-incapacitated','C6B-CHARGE-mount-incapacitated')){ChargeFail ('row '+$name+' claims the incapacity-window limitation')}
                $incapacity=ChargeProp $e 'incapacity'
                if($null-eq$incapacity){ChargeFail 'the incapacity limitation records no measurement'}
                if([string](ChargeProp $incapacity 'window')-cne'absent'){ChargeFail 'the incapacity limitation does not record the window as absent'}
                if([string](ChargeProp $incapacity 'windowReason')-eq''){ChargeFail 'the incapacity limitation names no reason'}
                if($null-ne(ChargeProp $incapacity 'nativeDamage')){ChargeFail 'the incapacity limitation dealt damage after all'}
            }
            default { ChargeFail ('row '+$name+' names an unknown limitation: '+$limitation) }
        }
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
            if((ChargeProp (ChargeProp $before 'geometry') 'straightRoute')-ne$true){ChargeFail 'the lawful charge line was not a straight native route'}

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
            if((ChargeProp $lease 'buffOutstanding')-ne$false){ChargeFail 'the delivered charge lease still owns the native charge buff'}
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
            # And the order in which it did all of that.
            Assert-KmcChunk6bChargeTransaction $e $name $true
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
        'C6B-CHARGE-cancelled' {
            # Cancellation before commitment. The charge must genuinely have been on offer over a lawful
            # geometry - otherwise the row proves nothing - the selection must have been taken through the
            # real player surface and then released, no click may have been issued, and the engine must
            # have taken nothing at all.
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the cancelled charge was not on offer over a lawful geometry'}
            if((ChargeProp (ChargeProp $before 'geometry') 'straightRoute')-ne$true){ChargeFail 'the cancelled charge line was not a straight native route'}
            if((ChargeProp $input 'selectedAfterSet')-ne$true){ChargeFail 'the cancelled charge was never selected'}
            if((ChargeProp $input 'selectedAfterCancel')-ne$false){ChargeFail 'the cancelled charge was still selected after the cancel'}
            if((ChargeProp $input 'clicked')-ne$false){ChargeFail 'the cancelled charge was clicked after all'}
            if((ChargeProp $input 'hoverPure')-ne$true){ChargeFail 'hovering the cancelled charge changed live state'}
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-obstructed-line' {
            $geometry=ChargeProp $before 'geometry'
            if($null-eq$geometry){ChargeFail 'the obstructed-line row recorded no geometry'}
            if((ChargeProp $geometry 'straightRoute')-ne$false){ChargeFail 'the obstructed-line row target had a clear native line'}
            # The refusal must be attributable to the line alone, so neither the landing point nor the
            # minimum charge distance may explain it.
            if((ChargeProp $geometry 'landingBlocked')-ne$false){ChargeFail 'the obstructed-line row landing point was blocked as well'}
            $state=ChargeProp $before 'state'
            if(-not(ChargeNumber (ChargeProp $state 'distanceToTarget'))-or-not(ChargeNumber (ChargeProp $before 'minRangeMeters'))){ChargeFail 'the obstructed-line row recorded no geometry distance'}
            if([double]$state.distanceToTarget-lt[double]$before.minRangeMeters){ChargeFail 'the obstructed-line row target was inside the minimum charge distance'}
            if((ChargeProp $before 'canTarget')-ne$false){ChargeFail 'a target whose charge line is obstructed was targetable'}
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-blocked-clearance' {
            # The clearance gate, which is distinct from the line: the native route is straight and the
            # distance lawful, and the only thing wrong is that another awake actor stands on the landing
            # point one weapon reach short of the target.
            $geometry=ChargeProp $before 'geometry'
            if($null-eq$geometry){ChargeFail 'the clearance row recorded no geometry'}
            if((ChargeProp $geometry 'landingBlocked')-ne$true){ChargeFail 'the clearance row landing point was not blocked'}
            if((ChargeProp $geometry 'straightRoute')-ne$true){ChargeFail 'the clearance row line was obstructed as well'}
            $state=ChargeProp $before 'state'
            if(-not(ChargeNumber (ChargeProp $state 'distanceToTarget'))-or-not(ChargeNumber (ChargeProp $before 'minRangeMeters'))){ChargeFail 'the clearance row recorded no geometry distance'}
            if([double]$state.distanceToTarget-lt[double]$before.minRangeMeters){ChargeFail 'the clearance row target was inside the minimum charge distance'}
            if((ChargeProp $before 'canTarget')-ne$false){ChargeFail 'a target whose charge landing point is occupied was targetable'}
            # Named blockers rather than a bare flag, each one actually inside the threshold the gate uses.
            $blockers=@(ChargeProp $e 'landingBlockers')
            if($blockers.Count-lt1){ChargeFail 'the clearance row named no blocking actor'}
            # The blocker is the fixture own diagnostic body, placed on the landing point through its own
            # service, and it must be one of the actors the gate counted.
            $placed=ChargeProp $e 'landingBlocker'
            if($null-eq$placed){ChargeFail 'the clearance row recorded no placed blocker'}
            if([string](ChargeProp $placed 'contract')-cne'diagnostic-blocker-occupies-the-exact-charge-landing-point'){ChargeFail 'the clearance blocker names another contract'}
            if((ChargeProp $placed 'placed')-ne$true){ChargeFail 'the clearance row claims a blocked landing without having placed its blocker'}
            if([string](ChargeProp $placed 'blockerId')-eq''){ChargeFail 'the placed clearance blocker has no identity'}
            if(-not(ChargeNumber (ChargeProp $placed 'separation'))-or[double]$placed.separation-le0){ChargeFail 'the clearance blocker recorded no weapon-reach separation'}
            if(@($blockers|Where-Object {[string](ChargeProp $_ 'actorId')-ceq[string](ChargeProp $placed 'blockerId')}).Count-ne1){
                ChargeFail 'the placed clearance blocker is not among the actors the clearance gate counted'
            }
            foreach($blocker in $blockers){
                if([string](ChargeProp $blocker 'actorId')-eq''){ChargeFail 'a clearance blocker has no identity'}
                if(-not(ChargeNumber (ChargeProp $blocker 'distanceToLanding'))-or-not(ChargeNumber (ChargeProp $blocker 'threshold'))){ChargeFail 'a clearance blocker recorded no distance'}
                if([double]$blocker.distanceToLanding-ge[double]$blocker.threshold){ChargeFail 'a clearance blocker stood outside the clearance threshold'}
            }
            Assert-KmcChunk6bChargeNothingHappened $e $name
        }
        'C6B-CHARGE-exception-cleanup' {
            # A charge that failed immediately after entering the rider command queue. The compensation
            # this row measures does not exist until the charge has genuinely been queued, so the charge
            # must first have been on offer over a lawful geometry.
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the admission-fault charge was not on offer over a lawful geometry'}
            if((ChargeProp (ChargeProp $before 'geometry') 'straightRoute')-ne$true){ChargeFail 'the admission-fault charge line was not a straight native route'}
            $fault=ChargeProp $e 'admissionFault'
            if($null-eq$fault){ChargeFail 'the admission-fault row recorded no fault'}
            if([string](ChargeProp $fault 'contract')-cne'post-queue-charge-admission-fault-compensated-exactly'){ChargeFail 'the admission-fault row names another contract'}
            if((ChargeProp $fault 'armed')-ne$true-or(ChargeProp $fault 'fired')-ne$true){ChargeFail 'the admission fault was not armed and fired'}
            # The native full-round shell delivers on a later frame than the click, so the seam is read
            # at the settle point. A row whose seam never fired measured an ordinary charge, not a
            # compensated admission failure.
            if((ChargeProp $fault 'seamClearedAfterFire')-ne$true){ChargeFail 'the one-shot admission seam stayed armed after the attempt settled'}
            $atClick=ChargeProp $fault 'atClick'
            if($null-eq$atClick){ChargeFail 'the admission-fault row recorded no state at its click'}
            if((ChargeProp $atClick 'seamStillArmed')-ne$true){ChargeFail 'the admission seam was already gone at the click, so it could not fire during delivery'}
            if([long](ChargeProp $fault 'compensationCount')-ne1){ChargeFail 'the faulted admission did not compensate exactly once'}
            if((ChargeProp $fault 'compensationComplete')-ne$true){ChargeFail ('the admission compensation did not complete: '+[string](ChargeProp $fault 'compensation'))}
            if([string](ChargeProp $fault 'unmetPostconditions')-ne''){ChargeFail ('the admission compensation left an unmet postcondition: '+[string](ChargeProp $fault 'unmetPostconditions'))}
            if((ChargeProp $fault 'commandResident')-ne$false){ChargeFail 'the compensated charge command was still resident on the rider'}
            if((ChargeProp $fault 'leaseRestored')-ne$true){ChargeFail 'the compensated charge did not restore its lease'}
            if((ChargeProp $fault 'activeCommandCleared')-ne$true){ChargeFail 'the controller kept its reference to the compensated command'}
            if((ChargeProp $fault 'faultedCleanupOwner')-ne$false){ChargeFail 'the controller retained a faulted charge cleanup owner'}
            foreach($flag in @('riderCommandsEmpty','mountCommandsEmpty')){ if((ChargeProp $fault $flag)-ne$true){ChargeFail ('the compensated charge left a native command behind: '+$flag)} }
            # The charge was refused rather than admitted, and nothing of the pair own ran.
            $delivery=ChargeProp $e 'delivery'
            if($null-eq$delivery){ChargeFail 'the admission-fault row recorded no delivery section'}
            if([long](ChargeProp $delivery 'chargeAdmitted')-ne0){ChargeFail 'the faulted admission admitted a charge'}
            if([long](ChargeProp $delivery 'chargeRefused')-lt1){ChargeFail 'the faulted admission recorded no refusal'}
            if([long](ChargeProp $e 'attackRules')-ne0){ChargeFail 'the faulted admission delivered an attack'}
            if($null-ne(ChargeProp $e 'lease')){ChargeFail 'the faulted admission published a lease it never owned'}
            if($null-ne(ChargeProp $e 'transaction')){ChargeFail 'the faulted admission published a transaction it never ran'}
            if(-not(ChargeNumber (ChargeProp $movement 'mountDistance'))-or[double]$movement.mountDistance-gt1.0){ChargeFail 'the faulted admission carried the pair'}
            foreach($field in @('mountStandardMax','mountMoveMax')){ if(-not(ChargeNumber (ChargeProp $economy $field))-or[double]$economy.$field-gt0.001){ChargeFail ('the faulted admission charged the mount: '+$field)} }
            # Whatever the native shell already took stays taken; nothing here writes a resource back.
            if(-not(ChargeNumber (ChargeProp $economy 'riderStandardNow'))-or[double]$economy.riderStandardNow-lt-0.001){ChargeFail 'the faulted admission recorded a negative rider standard cooldown'}
            foreach($flag in @('mountCharging','mountMoving','riderStateCharging','mountStateCharging','pairCommandActive')){ if((ChargeProp $after $flag)-ne$false){ChargeFail ('the faulted admission left residue: '+$flag)} }
            if($null-ne(ChargeProp $after 'mountSpeedOverride')){ChargeFail 'the faulted admission left a mount speed override behind'}
        }
        'C6B-CHARGE-target-moved' {
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the moving-target charge was not available or targetable'}
            if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail 'the moving-target charge did not admit exactly one native shell'}
            $delivery=ChargeProp $e 'delivery'
            if($null-eq$delivery-or[long](ChargeProp $delivery 'chargeAdmitted')-ne1){ChargeFail 'the moving-target row did not admit exactly one charge'}
            $move=ChargeProp $e 'targetMove'
            if($null-eq$move){ChargeFail 'the moving-target row recorded no target move'}
            if([string](ChargeProp $move 'contract')-cne'native-target-move-forces-charge-revalidation-and-repath'){ChargeFail 'the moving-target row names another contract'}
            if((ChargeProp $move 'issued')-ne$true){ChargeFail 'the moving-target row never issued its native move'}
            if(-not(ChargeNumber (ChargeProp $move 'targetMovedDistance'))-or[double]$move.targetMovedDistance-lt1.0){ChargeFail 'the moving-target row target did not actually move'}
            $terminal=ChargeProp $e 'terminal'
            if($null-eq$terminal){ChargeFail 'the moving-target row recorded no terminal outcome'}
            # Three lawful outcomes, decided by what the engine started and completed rather than by the
            # rule count alone: the charge struck and resolved; it reached its target and started its one
            # native attack which the engine then interrupted before the attack resolved; or it never
            # reached, because its own revalidation refused the changed geometry.
            $startedAttacks=[long](ChargeProp $terminal 'childAttackStartCount')
            $completedAttacks=[long](ChargeProp $terminal 'nativeCompletedAttackCount')
            $deliberateRules=[long](ChargeProp $rules 'pairNonOpportunityAttackRules')
            if($startedAttacks-gt1){ChargeFail 'the moving-target charge started more than one native attack'}
            Assert-KmcChunk6bChargeTransaction $e $name ($startedAttacks-eq1)
            if($startedAttacks-eq1){
                # It reached: the whole lawful order is present, and a repath really happened.
                if([long](ChargeProp $terminal 'repathCount')-lt1){ChargeFail 'the moving-target charge reached its target without repathing'}
                if($completedAttacks-ge1){
                    # And it resolved: exactly one deliberate attack, carrying the native charge rule.
                    $events=@((ChargeProp $rules 'attackRuleEvents')|Where-Object {$null-ne$_-and(ChargeProp $_ 'attackOfOpportunity')-eq$false})
                    if($events.Count-ne1-or(ChargeProp $events[0] 'charge')-ne$true){ChargeFail 'the moving-target charge did not deliver exactly one native charge attack'}
                    if($deliberateRules-ne1){ChargeFail 'the resolved moving-target charge did not record exactly one deliberate attack rule'}
                } else {
                    # Or the engine interrupted the strike before it resolved. Then there must be no
                    # deliberate attack rule at all, and the terminal must say it was interrupted: a
                    # charge that quietly produced a rule without completing would be a different thing.
                    if($deliberateRules-ne0){ChargeFail 'an unresolved moving-target charge attack still produced a deliberate attack rule'}
                    if([string](ChargeProp $terminal 'result')-cne'Interrupt'){ChargeFail ('an unresolved moving-target charge attack did not end interrupted: '+[string](ChargeProp $terminal 'result'))}
                }
            } else {
                # It never reached: the changed geometry failed the charge own revalidation, so it ended
                # without starting an attack and named its own reason.
                $t=ChargeProp $e 'transaction'
                if((ChargeProp $t 'revalidationFailed')-ne$true){ChargeFail 'the moving-target charge neither reached its target nor failed a revalidation'}
                if([string](ChargeProp $t 'revalidationFailurePhase')-eq''-or[string](ChargeProp $t 'revalidationFailureReason')-eq''){ChargeFail 'the moving-target charge did not name its revalidation failure'}
                if($deliberateRules-ne0){ChargeFail 'the moving-target charge delivered an attack after failing its revalidation'}
            }
            # Either way: the mount paid nothing, the lease came back and nothing was left behind.
            foreach($field in @('mountStandardMax','mountMoveMax')){ if(-not(ChargeNumber (ChargeProp $economy $field))-or[double]$economy.$field-gt0.001){ChargeFail ('the moving-target charge charged the mount: '+$field)} }
            $movedLease=ChargeProp $e 'lease'
            if($null-eq$movedLease){ChargeFail 'the moving-target charge recorded no lease'}
            foreach($flag in @('applied','buffApplied','restored','chargingRestoredExactly','speedOverrideRestoredExactly','riderChargingRestoredExactly')){ if((ChargeProp $movedLease $flag)-ne$true){ChargeFail ('the moving-target charge lease flag is not set: '+$flag)} }
            if((ChargeProp $movedLease 'buffOutstanding')-ne$false){ChargeFail 'the moving-target charge lease still owns the native charge buff'}
            foreach($flag in @('mountCharging','mountMoving','riderStateCharging','mountStateCharging','pairCommandActive')){ if((ChargeProp $after $flag)-ne$false){ChargeFail ('the moving-target charge left residue: '+$flag)} }
            if($null-ne(ChargeProp $after 'mountSpeedOverride')){ChargeFail 'the moving-target charge left a mount speed override behind'}
        }
        'C6B-CHARGE-target-lost' {
            if((ChargeProp $before 'available')-ne$true-or(ChargeProp $before 'canTarget')-ne$true){ChargeFail 'the target-lost charge was not available or targetable'}
            if([long](ChargeProp $input 'shellCount')-ne1){ChargeFail 'the target-lost charge did not admit exactly one native shell'}
            Assert-KmcChunk6bChargeBoundedTermination $e $name 'native-target-removed'
            $loss=ChargeProp $e 'targetLoss'
            if($null-eq$loss){ChargeFail 'the target-lost row recorded no target loss'}
            if([string](ChargeProp $loss 'contract')-cne'native-target-body-removed-mid-approach-shared-service-retained'){ChargeFail 'the target-lost row names another contract'}
            if([string](ChargeProp $loss 'lostTargetId')-eq''){ChargeFail 'the target-lost row did not record the body it removed'}
            if((ChargeProp $loss 'destroyConfirmed')-ne$true){ChargeFail 'the target-lost row did not confirm the native destroy'}
            if((ChargeProp $loss 'targetEntityRemoved')-ne$true){ChargeFail 'the target-lost row body was still in state'}
            # The failure this row was withdrawn for: the shared diagnostic target service must stay
            # alive for the fixture own cleanup, because the tranche still holds it.
            if((ChargeProp $loss 'serviceRetained')-ne$true-or(ChargeProp $loss 'serviceDisposedByRow')-ne$false){ChargeFail 'the target-lost row disposed the shared diagnostic target service'}
            # And the termination must be attributable to the lost target rather than to combat ending.
            if((ChargeProp $loss 'riderInCombat')-ne$true-or(ChargeProp $loss 'mountInCombat')-ne$true){ChargeFail 'the target-lost row pair had already left combat'}
            if((ChargeProp (ChargeProp $e 'intervention') 'riderInCombatAfter')-ne$true){ChargeFail 'the target-lost row left combat instead of losing its target'}
        }
        'C6B-CHARGE-rider-incapacitated' {
            Assert-KmcChunk6bChargeIncapacityRow $e $name 'rider' 'native-rider-incapacity'
        }
        'C6B-CHARGE-mount-incapacitated' {
            Assert-KmcChunk6bChargeIncapacityRow $e $name 'mount' 'native-mount-incapacity'
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
