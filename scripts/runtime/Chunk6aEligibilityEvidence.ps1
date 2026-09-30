# Exact native size/form and direct-control invalidation evidence.
Set-StrictMode -Version Latest

function Test-KmcEligibilityJsonEqual($A,$B) {
    return ($A|ConvertTo-Json -Depth 40 -Compress)-ceq($B|ConvertTo-Json -Depth 40 -Compress)
}
function Get-KmcEligibilityNumber($Value,[string]$Name) {
    if($Value -isnot [int] -and $Value -isnot [long] -and $Value -isnot [double] -and $Value -isnot [single] -and $Value -isnot [decimal]){throw "Eligibility evidence lacks numeric $Name."}
    $number=[double]$Value
    if([double]::IsNaN($number)-or[double]::IsInfinity($number)){throw "Eligibility evidence has nonfinite $Name."}
    return $number
}
function Assert-KmcEligibilityCommandSnapshot($Command,$Proof,[bool]$Terminal,[bool]$ExpectedTerminalStarted) {
    if($null-eq$Command-or-not(Test-KmcExactJsonInteger $Command.id)-or$Command.id-ne$Proof.identity.commandObject-or
       $Command.executor-cne$Proof.identity.casterId-or$Command.type-cne'Kingmaker.UnitLogic.Commands.UnitUseAbility'-or
       $Command.acted-ne$false){throw 'Eligibility command snapshot identifies another request.'}
    if($Terminal){
        if($Command.started-ne$ExpectedTerminalStarted-or$Command.finished-ne$true-or$Command.result-cne$Proof.nativeResult){throw 'Eligibility terminal snapshot differs from the exact native failure.'}
    } elseif($Command.started-ne$false-or$Command.finished-ne$false-or$Command.result-cne'None'){
        throw 'Eligibility stimulus did not surround the pending native Mount.'
    }
}
function Test-KmcEligibilityResourceEqual($Left,$Right,[string]$Name) {
    $first=Get-KmcEligibilityNumber $Left ($Name+' left')
    $second=Get-KmcEligibilityNumber $Right ($Name+' right')
    if([Math]::Abs($first-$second)-gt0.0001){return $false}
    return $true
}
function Assert-KmcIncapacityClearDisposition($Proof,[string]$Contract,$NativeLifeEvents) {
    $subjectId=if($Contract-ceq'unacted-native-rider-incapacitated-no-cost-or-transition'){$Proof.identity.casterId}
        elseif($Contract-ceq'unacted-native-mount-incapacitated-no-cost-or-transition'){$Proof.identity.targetId}
        else{throw 'Undeclared incapacity resource contract.'}
    $subjectKind=if($Contract-ceq'unacted-native-rider-incapacitated-no-cost-or-transition'){'rider'}else{'mount'}
    $lifeEvents=@($NativeLifeEvents.events)
    if($lifeEvents.Count-ne1){throw 'Exact native incapacity life event is missing or differs.'}
    $life=$lifeEvents[0];$sources=@($life.nativeSource)
    if($life.kind-cne'native-life-state'-or$life.actor-cne$subjectId-or$life.detail-cne'Conscious'-or
       $life.lifeState-cne'Unconscious'-or$life.commandRunning-ne$false-or$sources.Count-ne2-or
       $sources[0].type-cne'Kingmaker.Controllers.Units.UnitLifeController'-or$sources[0].method-cne'SetLifeState'-or
       $sources[0].token-cne'06009164'-or$sources[0].assemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or
       $sources[1].type-cne'Kingmaker.Controllers.Units.UnitLifeController'-or$sources[1].method-cne'TickOnUnit'-or
       $sources[1].token-cne'06009162'-or$sources[1].assemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){
        throw 'Exact native incapacity life event is missing or differs.'
    }
    $window=$Proof.resourceWindow
    $property=$window.PSObject.Properties['nativeIncapacityClearDisposition']
    if($null-eq$property-or$null-eq$property.Value){throw 'Native incapacity clear disposition is missing.'}
    $disposition=$property.Value
    if($disposition.contract-cne'native-incapacity-combat-removal-clears-initiative-only'-or
       $disposition.declaredCommandContract-cne$Contract-or$disposition.subjectId-cne$subjectId-or
       $disposition.lifeEventFrame-ne$life.frame-or$disposition.lifeEventGameTicks-ne$life.gameTicks-or
       $disposition.pass-ne$true-or@($disposition.errors).Count-ne0-or
       $disposition.actionResourcesUnchanged-ne$true-or$disposition.actionResourcesBridged-ne$true-or
       $disposition.reactionResourcesUnchanged-ne$true-or
       $disposition.initiativeCooldownCleared-ne$true-or$disposition.initiativeOrderCleared-ne$true-or
       $disposition.preparedCleared-ne$true){
        throw 'Native incapacity clear disposition is not exact.'
    }
    $expected=@('combat-clear-before','clear-before','clear-after','combat-clear-after')
    $declaredBoundaries=@($disposition.boundaries);$declaredSequences=@($disposition.sequences)
    if($declaredBoundaries.Count-ne4-or$declaredSequences.Count-ne4){throw 'Native incapacity clear boundary count differs.'}
    $pairIds=@($Proof.identity.casterId,$Proof.mountId)
    $clearEvents=@($window.events|Where-Object {
        $_.state.actor-cin$pairIds-and([string]$_.boundary-cmatch'^(clear-|combat-clear)')
    })
    if($clearEvents.Count-ne4){throw 'Native incapacity clear boundary count differs.'}
    for($i=0;$i-lt4;$i++){
        $event=$clearEvents[$i]
        if($declaredBoundaries[$i]-cne$expected[$i]-or$event.boundary-cne$expected[$i]-or
           $event.state.actor-cne$subjectId-or$declaredSequences[$i]-ne$event.sequence-or
           ($i-gt0-and$event.sequence-ne($clearEvents[$i-1].sequence+1))){
            throw 'Native incapacity clear actor, order, or sequence differs.'
        }
        if($event.frame-ne$life.frame-or$event.gameTicks-ne$life.gameTicks-or$event.state.inCombat-ne$false){
            throw 'Native incapacity clear is not bound to the exact life-state boundary.'
        }
    }
    $states=@($clearEvents|ForEach-Object {$_.state});$baseline=$states[0]
    $beforeCandidates=@()
    foreach($event in @($window.events|Where-Object {$_.state.actor-ceq$subjectId-and[int]$_.sequence-lt[int]$clearEvents[0].sequence})){
        $beforeCandidates+=@([pscustomobject]@{sequence=[int]$event.sequence;rank=1;gameTicks=[long]$event.gameTicks;state=$event.state})
    }
    foreach($sample in @($Proof.samples|Where-Object {[int]$_.allocationSequence-lt[int]$clearEvents[0].sequence})){
        $beforeCandidates+=@([pscustomobject]@{sequence=[int]$sample.allocationSequence;rank=2;gameTicks=[long]$sample.gameTicks;state=$sample.state.$subjectKind})
    }
    if([int]$Proof.preClick.allocationSequence-lt[int]$clearEvents[0].sequence){
        $beforeCandidates+=@([pscustomobject]@{sequence=[int]$Proof.preClick.allocationSequence;rank=0;gameTicks=[long]$Proof.preClick.gameTicks;state=$Proof.preClick.state.$subjectKind})
    }
    if($beforeCandidates.Count-eq0){throw 'Native incapacity clear lacks a preceding subject resource observation.'}
    $preceding=@($beforeCandidates|Sort-Object sequence,rank)[-1]
    $terminal=@($Proof.samples)[-1]
    if($null-eq$terminal-or[int]$terminal.allocationSequence-lt[int]$clearEvents[3].sequence-or
       [int]$disposition.actionBridgeBeforeSequence-ne$preceding.sequence-or
       [int]$disposition.actionBridgeAfterSequence-ne[int]$terminal.allocationSequence){
        throw 'Native incapacity clear action bridge sequence differs.'
    }
    foreach($field in @('standard','move','swift')){
        foreach($state in $states[1..3]){
            if(-not(Test-KmcEligibilityResourceEqual $baseline.$field $state.$field "incapacity $field")){
                throw 'Native incapacity clear changed Standard, Move, or Swift.'
            }
        }
        $beforeValue=Get-KmcEligibilityNumber $preceding.state.$field "incapacity preceding $field"
        $entryValue=Get-KmcEligibilityNumber $baseline.$field "incapacity entry $field"
        $exitValue=Get-KmcEligibilityNumber $states[3].$field "incapacity exit $field"
        $afterValue=Get-KmcEligibilityNumber $terminal.state.$subjectKind.$field "incapacity terminal $field"
        if([long]$clearEvents[0].gameTicks-lt$preceding.gameTicks-or[long]$terminal.gameTicks-lt[long]$clearEvents[0].gameTicks){
            throw 'Native incapacity clear action bridge clock is reversed.'
        }
        $beforeElapsed=([long]$clearEvents[0].gameTicks-$preceding.gameTicks)/10000000.0
        $afterElapsed=([long]$terminal.gameTicks-[long]$clearEvents[0].gameTicks)/10000000.0
        if([Math]::Abs($entryValue-[Math]::Max(0.0,$beforeValue-$beforeElapsed))-gt0.05-or
           [Math]::Abs($exitValue-$entryValue)-gt0.0001-or
           [Math]::Abs($afterValue-[Math]::Max(0.0,$exitValue-$afterElapsed))-gt0.05){
            throw 'Native incapacity clear action resources are not reconciled with surrounding command observations.'
        }
    }
    foreach($state in $states[1..3]){
        if($state.reactions-ne$baseline.reactions-or$state.reactionsPerRound-ne$baseline.reactionsPerRound-or
           -not(Test-KmcEligibilityResourceEqual $state.reactionCooldown $baseline.reactionCooldown 'incapacity reaction cooldown')){
            throw 'Native incapacity clear changed reaction allowance or cooldown.'
        }
    }
    $beforeCooldown=Get-KmcEligibilityNumber $baseline.initiativeCooldown 'incapacity initiative cooldown'
    if($beforeCooldown-le0-or
       -not(Test-KmcEligibilityResourceEqual $states[1].initiativeCooldown $baseline.initiativeCooldown 'clear-before initiative')-or
       -not(Test-KmcEligibilityResourceEqual $states[2].initiativeCooldown 0.0 'clear-after initiative')-or
       -not(Test-KmcEligibilityResourceEqual $states[3].initiativeCooldown 0.0 'combat-clear-after initiative')){
        throw 'Native incapacity clear did not perform the exact initiative-cooldown clear.'
    }
    if($baseline.initiativeOrder-eq0-or$states[1].initiativeOrder-ne$baseline.initiativeOrder-or
       $states[2].initiativeOrder-ne$baseline.initiativeOrder-or$states[3].initiativeOrder-ne0){
        throw 'Native incapacity combat clear did not perform the exact initiative-order clear.'
    }
    if($states[1].prepared-ne$baseline.prepared-or$states[2].prepared-ne$baseline.prepared-or$states[3].prepared-ne$false){
        throw 'Native incapacity combat clear changed preparation outside its terminal removal boundary.'
    }
    if(-not(Test-KmcEligibilityResourceEqual $life.standard $baseline.standard 'life-event standard')-or
       -not(Test-KmcEligibilityResourceEqual $life.move $baseline.move 'life-event move')){
        throw 'Native incapacity life event is not resource-identical to combat removal entry.'
    }
    return [string]$subjectId
}
function Assert-KmcEligibilityCommandProof($Proof,[string]$Contract,$NativeLifeEvents=$null) {
    if($Proof.contract-cne$Contract){throw 'Missing exact unacted eligibility contract.'}
    foreach($flag in @('pass','sameCommandAtEveryBoundary','nativeTerminal','traceComplete')){if($Proof.$flag-ne$true){throw "Unacted eligibility proof lacks true $flag."}}
    if($Proof.initCount-ne1-or@($Proof.errors).Count-ne0-or$Proof.nativeResult-cnotin@('Interrupt','Fail')){throw 'Eligibility-invalidated Mount lacks a truthful native terminal.'}
    $id=$Proof.identity
    if(-not(Test-KmcExactJsonInteger $id.commandObject)-or$id.commandObject-eq0-or$id.processObject-ne0-or$id.contextObject-ne0-or
       $id.commandType-cne'Move'-or$id.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or
       [string]::IsNullOrWhiteSpace([string]$id.controlIdentity)-or[string]::IsNullOrWhiteSpace([string]$id.casterId)-or
       [string]::IsNullOrWhiteSpace([string]$id.targetId)-or$id.targetId-cne$Proof.mountId-or$id.casterId-ceq$id.targetId){
        throw 'Eligibility proof has a process or invalid exact Mount identity.'
    }
    $samples=@($Proof.samples);$names=@('init','click-admission','move-slot-installation','approach-start','unacted-terminal')
    if($samples.Count-ne5){throw 'Eligibility proof must contain exactly five unacted boundaries.'}
    $pre=$Proof.preClick
    if($null-eq$pre-or$pre.gameTicks-gt$samples[0].gameTicks-or@($pre.state.selectedIds).Count-ne1-or$pre.state.selectedIds[0]-cne$id.casterId){throw 'Eligibility pre-click baseline is not the exact selected rider.'}
    if($pre.state.generation-ne$id.generationAtInit-or$pre.state.relationshipState-cne'Unmounted'){throw 'Eligibility pre-click relationship differs.'}
    $previous=[long]$pre.gameTicks
    for($i=0;$i-lt5;$i++){
        $sample=$samples[$i]
        if($sample.boundary-cne$names[$i]-or$sample.acted-ne$false-or$sample.gameTicks-lt$previous){throw 'Eligibility command boundary, acted flag or order differs.'}
        $previous=[long]$sample.gameTicks
        foreach($field in @('commandObject','controlIdentity','processObject','contextObject','casterId','targetId','generationAtInit','commandType','abilityGuid')){
            if($null-eq$sample.identity.$field-or$sample.identity.$field-cne$id.$field){throw "Mixed eligibility command identity: $field."}
        }
        if($sample.state.relationshipState-cne'Unmounted'-or$sample.state.generation-ne$id.generationAtInit){throw 'Eligibility window changed the relationship.'}
        foreach($field in @($pre.state.ledger.PSObject.Properties.Name)){
            if($sample.state.ledger.$field-ne$pre.state.ledger.$field-or$Proof.ledgerDelta.$field-ne0){throw 'Eligibility window changed the transition ledger.'}
        }
    }
    $terminal=$samples[-1]
    if($terminal.finished-ne$true-or$terminal.result-cne$Proof.nativeResult-or$null-ne$terminal.processEnded){throw 'Eligibility terminal is not a finished process-free native failure.'}
    $window=$Proof.resourceWindow
    $incapacityActor=$null
    if($Contract-cin@('unacted-native-rider-incapacitated-no-cost-or-transition','unacted-native-mount-incapacitated-no-cost-or-transition')){
        $incapacityActor=Assert-KmcIncapacityClearDisposition $Proof $Contract $NativeLifeEvents
        $nativeCallbackProperty=$window.PSObject.Properties['noNativeCostOrPreparationCallbacks']
        if($null-eq$nativeCallbackProperty-or$nativeCallbackProperty.Value-ne$true){throw 'Native incapacity window contains a cost or preparation callback.'}
    } elseif($null-ne$NativeLifeEvents){throw 'Native life evidence was supplied to a non-incapacity contract.'}
    if($window.pass-ne$true-or$window.noCostOrPreparationCallbacks-ne$true-or$window.endpointsConserved-ne$true){throw 'Eligibility resource window did not prove conservation.'}
    foreach($event in @($window.events|Where-Object {$_.state.actor-cin@($id.casterId,$Proof.mountId)})){
        $forbidden=if($null-ne$incapacityActor){'^(cost-|actor-cost-|prepare-)'}else{'^(cost-|actor-cost-|prepare-|clear-|combat-clear)'}
        if([string]$event.boundary-cmatch$forbidden){throw 'Eligibility window contains a native cost, preparation, or undeclared reset.'}
    }
    $elapsed=([long]$terminal.gameTicks-[long]$pre.gameTicks)/10000000.0
    foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift')){
        $before=Get-KmcEligibilityNumber $pre.state.$actor.$field "$actor $field before"
        $after=Get-KmcEligibilityNumber $terminal.state.$actor.$field "$actor $field after"
        if([Math]::Abs($after-[Math]::Max(0.0,$before-$elapsed))-gt0.05){throw "Eligibility window refunded or added $actor $field debt."}
    }}
    Assert-KmcRelationshipReactionResources $Proof 0 $incapacityActor
}
function Assert-KmcEligibilityGeometry($Start,$Trigger,$Proof) {
    $id=$Proof.identity;$approach=@($Proof.samples|Where-Object boundary -CEQ 'approach-start')[0];$terminal=@($Proof.samples)[-1]
    if($null-eq$Start-or$null-eq$Trigger-or$Trigger.approachObserved-ne$true-or$Trigger.riderReallyMoving-ne$true-or
       $Trigger.gameTicks-lt$approach.gameTicks-or$Trigger.gameTicks-gt$terminal.gameTicks-or
       $Trigger.commandObject-ne$id.commandObject-or$Trigger.moveSlotObject-ne$id.commandObject-or
       $Trigger.started-ne$false-or$Trigger.acted-ne$false-or$Trigger.finished-ne$false){throw 'Eligibility trigger missed the exact pending native approach.'}
    foreach($geometry in @($Start,$Trigger.geometry)){
        foreach($field in @('centerDistance','horizontalDistance','riderCorpulence','horseCorpulence','legalAdjacencyEnvelope')){$null=Get-KmcEligibilityNumber $geometry.$field "geometry $field"}
        foreach($point in @($geometry.riderPosition,$geometry.horsePosition)){foreach($axis in @('x','y','z')){$null=Get-KmcEligibilityNumber $point.$axis "geometry $axis"}}
        $dx=[double]$geometry.riderPosition.x-[double]$geometry.horsePosition.x;$dz=[double]$geometry.riderPosition.z-[double]$geometry.horsePosition.z
        $distance=[Math]::Sqrt($dx*$dx+$dz*$dz);$envelope=[double]$geometry.riderCorpulence+[double]$geometry.horseCorpulence+1.5
        if($geometry.isAdjacent-ne$false-or$geometry.centerDistance-le$envelope-or$distance-le$envelope-or
           [Math]::Abs($distance-[double]$geometry.horizontalDistance)-gt0.001-or[Math]::Abs($envelope-[double]$geometry.legalAdjacencyEnvelope)-gt0.0001){throw 'Eligibility stimulus was not outside the transition envelope.'}
    }
    $dx=[double]$Trigger.geometry.riderPosition.x-[double]$Start.riderPosition.x;$dz=[double]$Trigger.geometry.riderPosition.z-[double]$Start.riderPosition.z
    $moved=[Math]::Sqrt($dx*$dx+$dz*$dz)
    if($moved-le0.25-or[Math]::Abs($moved-[double]$Trigger.riderDisplacement)-gt0.0001){throw 'Eligibility stimulus lacks measured rider approach displacement.'}
}
function Assert-KmcEligibilitySizeState($State,$Proof,[string]$Phase,[int]$BuffObject) {
    if($State.riderId-cne$Proof.identity.casterId-or$State.mountId-cne$Proof.identity.targetId-or
       $State.buffName-cne'EnlargePersonBuff'-or[string]$State.buffGuid-cnotmatch'^[0-9a-f]{32}$'-or
       $State.changeUnitSizeComponents-ne1-or$State.mountSize-le4-or$State.riderPolymorphObject-ne0-or$State.mountPolymorphObject-ne0){throw "Size state identity differs at $Phase."}
    $objects=@($State.buffObjects)
    if($Phase-ceq'active'){
        if($State.buffCount-ne1-or$objects.Count-ne1-or$objects[0]-ne$BuffObject-or$BuffObject-eq0-or$State.riderSize-le4-or$State.riderSize-lt$State.mountSize){throw 'Authored size effect did not create the exact unsupported equal-or-larger rider state.'}
    } elseif($State.buffCount-ne0-or$objects.Count-ne0-or$State.riderSize-ne4){throw "Size state retained an owned buff at $Phase."}
}
function Assert-KmcSizeFormChange($Case) {
    if($Case.contract-cne'native-size-fact-invalidates-exact-mount-approach'-or$Case.restored-ne$true-or$Case.noResidue-ne$true-or
       $Case.stimulusCount-ne1-or$Case.removalCount-ne1-or$Case.diagnosticInterruptCount-ne0){throw 'Size/form case is undeclared, unrestored, or retained residue.'}
    $proof=$Case.commandProof;Assert-KmcEligibilityCommandProof $proof 'unacted-native-size-form-change-no-cost-or-transition'
    Assert-KmcEligibilityGeometry $Case.start $Case.trigger $proof
    Assert-KmcEligibilityCommandSnapshot $Case.terminal $proof $true $true
    $stimulus=$Case.stimulus
    if($stimulus.contract-cne'one-authored-enlarge-person-buff-through-native-rulebook'-or$stimulus.count-ne1-or
       $stimulus.frameAfter-lt$stimulus.frameBefore-or$stimulus.gameTicksAfter-lt$stimulus.gameTicksBefore-or$stimulus.buffObject-eq0){throw 'Size stimulus boundary differs.'}
    Assert-KmcEligibilityCommandSnapshot $stimulus.commandBefore $proof $false $false
    Assert-KmcEligibilityCommandSnapshot $stimulus.commandAfter $proof $false $false
    Assert-KmcEligibilitySizeState $Case.stateBefore $proof 'before' 0
    if(-not(Test-KmcEligibilityJsonEqual $Case.stateBefore $stimulus.before)){throw 'Size pre-stimulus state differs from the selected baseline.'}
    Assert-KmcEligibilitySizeState $stimulus.after $proof 'active' ([int]$stimulus.buffObject)
    foreach($state in @($Case.stateAfterApplication,$Case.stateBeforeRemoval)){
        if(-not(Test-KmcEligibilityJsonEqual $stimulus.after $state)){throw 'Owned size state changed before exact removal.'}
    }
    $removal=$Case.removal
    if($removal.contract-cne'remove-only-exact-owned-native-buff-after-command-terminal'-or$removal.count-ne1-or
       $removal.buffObject-ne$stimulus.buffObject-or$removal.gameTicks-lt@($proof.samples)[-1].gameTicks){throw 'Size removal did not follow the exact terminal command.'}
    if(-not(Test-KmcEligibilityJsonEqual $removal.after $Case.stateAfterRestoration)-or-not(Test-KmcEligibilityJsonEqual $Case.stateBefore $Case.stateAfterRestoration)){throw 'Size removal did not restore the exact original native state.'}
    Assert-KmcEligibilitySizeState $Case.stateAfterRestoration $proof 'restored' 0
}
function Assert-KmcDirectControlState($State,$Proof,[string]$Phase) {
    if($State.riderId-cne$Proof.identity.casterId-or$State.mountId-cne$Proof.identity.targetId-or$State.inGame-ne$true-or
       $State.frightenedImmune-ne$false-or$State.visibleConsciousEnemies-ne@($State.visibleEnemyIds).Count){throw "Direct-control state identity differs at $Phase."}
    if($Phase-ceq'before'){
        if($State.directlyControllable-ne$true-or$State.panicked-ne$false-or$State.frightened-ne$false-or$State.visibleConsciousEnemies-lt1){throw 'Direct-control fixture baseline differs.'}
    } elseif($Phase-ceq'immediate'){
        if($State.directlyControllable-ne$true-or$State.panicked-ne$false-or$State.frightened-ne$true){throw 'Frightened Fact bypassed or missed the native fear-controller boundary.'}
    } elseif($Phase-ceq'lost'){
        if($State.directlyControllable-ne$false-or$State.panicked-ne$true-or$State.frightened-ne$true){throw 'Native fear controller did not remove direct control.'}
    } elseif($Phase-ceq'removed'){
        if($State.directlyControllable-ne$false-or$State.panicked-ne$true-or$State.frightened-ne$false){throw 'Fact removal manufactured control restoration.'}
    } elseif($State.directlyControllable-ne$true-or$State.panicked-ne$false-or$State.frightened-ne$false){throw 'Native fear controller did not restore the owned control fields.'}
}
function Assert-KmcFearLease($Lease,$Proof,[string]$Phase) {
    if($Lease.contract-cne'owned-native-frightened-fact-awaits-unit-fear-controller'-or$Lease.actorId-cne$Proof.identity.casterId-or
       [string]$Lease.blueprintId-cnotmatch'^[0-9a-f]{32}$'-or[string]$Lease.templateId-cnotmatch'^[0-9a-f]{32}$'-or$Lease.blueprintId-ceq$Lease.templateId-or
       $Lease.factObject-eq0-or$Lease.templateComponentObject-eq0-or$Lease.activeComponentObject-eq0-or
       $Lease.componentType-cne'Kingmaker.UnitLogic.FactLogic.AddCondition'-or$Lease.condition-cne'Frightened'-or
       $Lease.conditionImmune-ne$false-or$Lease.onTurnOnToken-cne'06002448'-or$Lease.onTurnOffToken-cne'06002449'-or
       $Lease.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Owned native Frightened Fact identity differs.'}
    if($Phase-ceq'immediate'){
        if($Lease.disposed-ne$false-or$Lease.activeFactCount-ne1-or$Lease.conditionActive-ne$true-or$Lease.panicked-ne$false-or$Lease.directlyControllable-ne$true){throw 'Immediate Frightened Fact state differs.'}
    } elseif($Phase-ceq'lost'){
        if($Lease.disposed-ne$false-or$Lease.activeFactCount-ne1-or$Lease.conditionActive-ne$true-or$Lease.panicked-ne$true-or$Lease.directlyControllable-ne$false){throw 'Pre-removal Frightened Fact state differs.'}
    } elseif($Lease.disposed-ne$true-or$Lease.activeFactCount-ne0-or$Lease.conditionActive-ne$false-or$Lease.panicked-ne$true-or$Lease.directlyControllable-ne$false){throw 'Exact Frightened Fact removal state differs.'}
}
function Assert-KmcFearController($Probe,$Proof) {
    if($Probe.contract-cne'installed-unit-fear-controller-removes-and-restores-direct-control'-or$Probe.riderId-cne$Proof.identity.casterId-or
       $Probe.commandObject-ne$Proof.identity.commandObject-or$Probe.complete-ne$true-or$Probe.lossObserved-ne$true-or$Probe.restorationObserved-ne$true-or
       $Probe.lossCount-ne1-or$Probe.restorationCount-ne1-or@($Probe.errors).Count-ne0){throw 'Native fear-controller observation is incomplete.'}
    $hooks=@($Probe.observerHooks)
    if($hooks.Count-ne1-or$hooks[0].method-cne'Kingmaker.Controllers.Units.UnitFearController.TickOnUnit'-or$hooks[0].token-cne'06009138'-or
       $hooks[0].moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$hooks[0].prefix-cne'Before'-or$hooks[0].postfix-cne'After'){throw 'Native fear-controller hook identity differs.'}
    $events=@($Probe.events)
    if($events.Count-lt2-or$events.Count-gt24){throw 'Native fear-controller event bound differs.'}
    for($i=0;$i-lt$events.Count;$i++){if($events[$i].ordinal-ne$i+1){throw 'Native fear-controller event order differs.'}}
    $loss=@($events|Where-Object lossTransition -EQ $true);$restore=@($events|Where-Object restorationTransition -EQ $true)
    if($loss.Count-ne1-or$restore.Count-ne1){throw 'Native fear-controller transition count differs.'}
    $before=$loss[0].before;$after=$loss[0].after
    if($before.conditionActive-ne$true-or$before.panicked-ne$false-or$before.directlyControllable-ne$true-or
       $before.commandObject-ne$Proof.identity.commandObject-or$before.commandStarted-ne$false-or$before.commandActed-ne$false-or$before.commandFinished-ne$false-or
       $before.commandProcessObject-ne0-or$before.moveSlotObject-ne$Proof.identity.commandObject-or@($before.visibleConsciousEnemies).Count-lt1-or
       $after.conditionActive-ne$true-or$after.panicked-ne$true-or$after.directlyControllable-ne$false-or$after.commandObject-ne$Proof.identity.commandObject-or
       $after.commandStarted-ne$false-or$after.commandActed-ne$false-or$after.commandFinished-ne$true-or$after.commandResult-cnotin@('Interrupt','Fail')-or$after.commandProcessObject-ne0){throw 'Native fear-controller loss boundary differs.'}
    $before=$restore[0].before;$after=$restore[0].after
    if($before.conditionActive-ne$false-or$before.panicked-ne$true-or$before.directlyControllable-ne$false-or
       $after.conditionActive-ne$false-or$after.panicked-ne$false-or$after.directlyControllable-ne$true-or$after.commandsEmpty-ne$true-or$after.moveSlotObject-ne0){throw 'Native fear-controller restoration boundary differs.'}
}
function Assert-KmcLostDirectControl($Case) {
    if($Case.contract-cne'native-fear-controller-invalidates-exact-mount-approach'-or$Case.restored-ne$true-or$Case.noResidue-ne$true-or
       $Case.stimulusCount-ne1-or$Case.removalCount-ne1-or$Case.diagnosticInterruptCount-ne0){throw 'Lost-direct-control case is undeclared, unrestored, or retained residue.'}
    $proof=$Case.commandProof;Assert-KmcEligibilityCommandProof $proof 'unacted-native-lost-direct-control-no-cost-or-transition'
    Assert-KmcEligibilityGeometry $Case.start $Case.trigger $proof
    Assert-KmcEligibilityCommandSnapshot $Case.terminal $proof $true $false
    Assert-KmcDirectControlState $Case.stateBefore $proof 'before'
    $stimulus=$Case.stimulus
    if($stimulus.contract-cne'one-owned-native-frightened-fact'-or$stimulus.count-ne1-or$stimulus.gameTicks-gt@($proof.samples)[-1].gameTicks){throw 'Native fear stimulus boundary differs.'}
    Assert-KmcEligibilityCommandSnapshot $stimulus.commandBefore $proof $false $false
    Assert-KmcFearLease $stimulus.lease $proof 'immediate'
    Assert-KmcDirectControlState $stimulus.immediateState $proof 'immediate'
    Assert-KmcDirectControlState $Case.stateAtControlLoss $proof 'lost'
    Assert-KmcFearLease $Case.leaseBeforeRemoval $proof 'lost'
    Assert-KmcFearLease $Case.leaseAfterRemoval $proof 'removed'
    Assert-KmcDirectControlState $Case.stateAfterFactRemoval $proof 'removed'
    foreach($field in @('actorId','blueprintId','templateId','factObject','templateComponentObject','activeComponentObject','componentType','condition','onTurnOnToken','onTurnOffToken','moduleMvid')){
        if($Case.leaseBeforeRemoval.$field-cne$Case.leaseAfterRemoval.$field-or$stimulus.lease.$field-cne$Case.leaseBeforeRemoval.$field){throw "Owned Frightened Fact identity changed: $field."}
    }
    Assert-KmcFearController $Case.fearController $proof
    $restoration=$Case.restoration
    if($restoration.contract-cne'native-fear-controller-restores-control-after-owned-fact-removal'-or$restoration.removalCount-ne1-or
       $restoration.noCostOrPreparationCallbacks-ne$true){throw 'Native direct-control restoration contract differs.'}
    foreach($event in @($restoration.allocationEvents)){
        if([string]$event.boundary-cmatch'^(cost-|actor-cost-|prepare-|clear-|combat-clear)'){throw 'Direct-control restoration contains a native cost, preparation or reset.'}
    }
    Assert-KmcDirectControlState $restoration.state $proof 'restored'
    foreach($field in @('riderId','mountId','directlyControllable','inGame','panicked','frightened','frightenedImmune')){
        if($restoration.state.$field-cne$Case.stateBefore.$field){throw "Direct-control restoration changed owned field: $field."}
    }
}
function Assert-KmcPendingIncapacityActor($State,$Proof,[string]$Kind,[string]$Phase) {
    $expectedId=if($Kind-ceq'rider'){$Proof.identity.casterId}else{$Proof.identity.targetId}
    if($State.id-cne$expectedId-or$State.inState-ne$true-or$State.inGame-ne$true-or
       $State.allowDyingCondition-ne$true-or$State.immortal-ne$false-or$State.essential-ne$false-or$State.mainCharacter-ne$false){throw "Pending incapacity actor identity or policy differs at $Phase."}
    foreach($field in @('damage','hitPoints','temporaryHitPoints','constitution')){$null=Get-KmcEligibilityNumber $State.$field "$Kind $field at $Phase"}
    if($State.hitPoints-le0-or$State.constitution-le0-or$State.temporaryHitPoints-lt0-or$State.damage-lt0){throw "Pending incapacity actor stats are invalid at $Phase."}
    if($Phase-ceq'before'){
        if($State.lifeState-cne'Conscious'-or$State.conscious-ne$true-or$State.dead-ne$false-or$State.finallyDead-ne$false){throw 'Pending incapacity baseline is not conscious.'}
    } elseif($Phase-ceq'after-damage'){
        if($State.lifeState-cne'Conscious'-or$State.conscious-ne$true-or$State.dead-ne$false-or$State.finallyDead-ne$false-or
           $State.damage-le$State.hitPoints-or$State.damage-ge($State.hitPoints+$State.constitution)){
            throw 'Immediate native damage snapshot did not retain the exact nonlethal pre-life-tick window.'
        }
    } elseif($State.lifeState-cne'Unconscious'-or$State.conscious-ne$false-or$State.dead-ne$false-or$State.finallyDead-ne$false-or
             $State.damage-le$State.hitPoints-or$State.damage-ge($State.hitPoints+$State.constitution)){
        throw 'Native life tick did not retain the exact nonlethal Unconscious state.'
    }
}
function Assert-KmcPendingIncapacity([string]$Scenario,$Case,[string]$ExpectedKind) {
    if($Case.contract-cne'native-life-state-invalidates-exact-mount-approach'-or$Case.subjectKind-cne$ExpectedKind-or
       $Case.subjectRemainsIncapacitated-ne$true-or$Case.externalRestorationRequired-ne$true-or$Case.otherActorUnchanged-ne$true-or
       $Case.noResidue-ne$true-or$Case.stimulusCount-ne1-or$Case.damageDispatchCount-ne1-or$Case.diagnosticInterruptCount-ne0){
        throw 'Pending-Mount incapacity case is undeclared, changed its subject, or retained residue.'
    }
    $expectedScenario='chunk6a-'+$ExpectedKind+'-incapacitated'
    if($Scenario-cne$expectedScenario){throw 'Pending-Mount incapacity scenario and subject differ.'}
    $proof=$Case.commandProof
    $proofContract='unacted-native-'+$ExpectedKind+'-incapacitated-no-cost-or-transition'
    Assert-KmcEligibilityCommandProof $proof $proofContract $Case.nativeLifeEvents
    Assert-KmcEligibilityGeometry $Case.start $Case.trigger $proof
    $expectedTerminalStarted=($ExpectedKind-ceq'mount')
    Assert-KmcEligibilityCommandSnapshot $Case.terminal $proof $true $expectedTerminalStarted
    $subjectField=$ExpectedKind;$otherField=if($ExpectedKind-ceq'rider'){'mount'}else{'rider'}
    $beforeSubject=$Case.stateBefore.$subjectField;$beforeOther=$Case.stateBefore.$otherField
    $afterDamageSubject=$Case.stateAfterDamage.$subjectField;$afterDamageOther=$Case.stateAfterDamage.$otherField
    $terminalSubject=$Case.stateAfterTerminal.$subjectField;$terminalOther=$Case.stateAfterTerminal.$otherField
    if($Case.stateBefore.relationship-cne'Unmounted'-or$Case.stateAfterDamage.relationship-cne'Unmounted'-or$Case.stateAfterTerminal.relationship-cne'Unmounted'-or
       $Case.stateBefore.generation-ne$proof.identity.generationAtInit-or$Case.stateAfterDamage.generation-ne$proof.identity.generationAtInit-or
       $Case.stateAfterTerminal.generation-ne$proof.identity.generationAtInit){throw 'Pending incapacity changed relationship state or generation.'}
    Assert-KmcPendingIncapacityActor $beforeSubject $proof $subjectField 'before'
    Assert-KmcPendingIncapacityActor $beforeOther $proof $otherField 'before'
    Assert-KmcPendingIncapacityActor $afterDamageSubject $proof $subjectField 'after-damage'
    Assert-KmcPendingIncapacityActor $terminalSubject $proof $subjectField 'terminal'
    if(-not(Test-KmcEligibilityJsonEqual $beforeOther $afterDamageOther)-or-not(Test-KmcEligibilityJsonEqual $beforeOther $terminalOther)){
        throw 'Pending incapacity changed the independent actor.'
    }
    $stimulus=$Case.stimulus
    if($stimulus.contract-cne'one-native-ruledeal-damage-to-incapacitation-window'-or$stimulus.subjectKind-cne$ExpectedKind-or
       $stimulus.subjectId-cne$beforeSubject.id-or$Case.subjectId-cne$beforeSubject.id-or$Case.otherId-cne$beforeOther.id-or
       [string]::IsNullOrWhiteSpace([string]$stimulus.sourceId)-or$stimulus.sourceId-cin@($beforeSubject.id,$beforeOther.id)-or
       $stimulus.count-ne1-or$stimulus.damageDispatchCount-ne1-or-not(Test-KmcExactJsonInteger $stimulus.ruleObject)-or$stimulus.ruleObject-eq0){
        throw 'Native incapacity stimulus identity or dispatch count differs.'
    }
    foreach($field in @('difficulty','hitPoints','constitution','temporaryHitPoints','damageBefore','desiredDamage','deathThreshold','requestedDamage','projectedDamage','nativeDamage','nativeDamageBeforeDifficulty')){
        $null=Get-KmcEligibilityNumber $stimulus.$field "incapacity stimulus $field"
    }
    if($stimulus.difficulty-le0-or$stimulus.hitPoints-ne$beforeSubject.hitPoints-or$stimulus.constitution-ne$beforeSubject.constitution-or
       $stimulus.temporaryHitPoints-ne$beforeSubject.temporaryHitPoints-or$stimulus.damageBefore-ne$beforeSubject.damage-or
       $stimulus.desiredDamage-ne($stimulus.hitPoints+1)-or$stimulus.deathThreshold-ne($stimulus.hitPoints+$stimulus.constitution)-or
       $stimulus.requestedDamage-le0-or$stimulus.nativeDamageBeforeDifficulty-ne$stimulus.requestedDamage-or$stimulus.nativeDamage-le0-or
       $stimulus.projectedDamage-ge$stimulus.deathThreshold-or$stimulus.frameAfter-lt$stimulus.frameBefore-or
       $stimulus.gameTicksAfter-lt$stimulus.gameTicksBefore-or$stimulus.gameTicksAfter-gt@($proof.samples)[-1].gameTicks){
        throw 'Native RuleDealDamage did not prove the exact nonlethal window.'
    }
    $projected=[double]$stimulus.damageBefore+[double]$stimulus.requestedDamage*[double]$stimulus.difficulty-[double]$stimulus.temporaryHitPoints
    if([Math]::Abs($projected-[double]$stimulus.projectedDamage)-gt0.001){throw 'Recorded native incapacity projection differs from the declared inputs.'}
    if(-not(Test-KmcEligibilityJsonEqual $stimulus.before $Case.stateBefore)-or-not(Test-KmcEligibilityJsonEqual $stimulus.after $Case.stateAfterDamage)){
        throw 'Native incapacity stimulus did not surround the selected life-state baseline.'
    }
    Assert-KmcEligibilityCommandSnapshot $stimulus.commandBefore $proof $false $false
    $events=@($Case.nativeLifeEvents.events)
    if($events.Count-ne1){throw 'Native incapacity observer event count differs.'}
    $event=$events[0];$sources=@($event.nativeSource)
    if($event.kind-cne'native-life-state'-or$event.actor-cne$beforeSubject.id-or$event.detail-cne'Conscious'-or
       $event.lifeState-cne'Unconscious'-or$event.damage-ne$terminalSubject.damage-or$sources.Count-ne2){
        throw 'Native incapacitation life-state event differs.'
    }
    if($sources[0].type-cne'Kingmaker.Controllers.Units.UnitLifeController'-or$sources[0].method-cne'SetLifeState'-or
       $sources[0].token-cne'06009164'-or$sources[0].assemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or
       $sources[1].type-cne'Kingmaker.Controllers.Units.UnitLifeController'-or$sources[1].method-cne'TickOnUnit'-or
       $sources[1].token-cne'06009162'-or$sources[1].assemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){
        throw 'Native incapacitation source-frame identity differs.'
    }
}function Assert-KmcEligibilityChange([string]$Scenario,$Case) {
    if($Scenario-ceq'chunk6a-size-form-change'){Assert-KmcSizeFormChange $Case;return}
    if($Scenario-ceq'chunk6a-lost-direct-control'){Assert-KmcLostDirectControl $Case;return}
    if($Scenario-ceq'chunk6a-rider-incapacitated'){Assert-KmcPendingIncapacity $Scenario $Case 'rider';return}
    if($Scenario-ceq'chunk6a-mount-incapacitated'){Assert-KmcPendingIncapacity $Scenario $Case 'mount';return}
    throw 'Unknown Chunk 6A eligibility scenario.'
}
