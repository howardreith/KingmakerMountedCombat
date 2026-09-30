# Exact repeated native Mount input during one pending approach.
Set-StrictMode -Version Latest
function Assert-KmcRepeatedMountRequest($Case,$Proofs) {
    if($Case.contract -cne 'one-owned-native-mount-when-request-repeated-during-approach') {
        throw 'Repeated-request case contract differs.'
    }
    $proof=@($Proofs|Where-Object window -CEQ 'positive-mount')
    if($proof.Count-ne1-or$proof[0].pass-ne$true-or$Case.commandProof.pass-ne$true) {
        throw 'Repeated-request case requires one passing positive Mount command proof.'
    }
    $proof=$proof[0]
    foreach($field in @('commandObject','controlIdentity','processObject','contextObject','casterId','targetId','generationAtInit','commandType','abilityGuid')) {
        if($Case.commandProof.identity.$field-cne$proof.identity.$field) {throw "Repeated-request command proof identity differs: $field."}
    }
    if($Case.start.isAdjacent-ne$false-or-not(Test-KmcFiniteJsonNumber $Case.start.centerDistance)-or
        -not(Test-KmcFiniteJsonNumber $Case.start.legalAdjacencyEnvelope)-or
        [double]$Case.start.centerDistance-le[double]$Case.start.legalAdjacencyEnvelope) {
        throw 'Repeated-request first Mount did not start outside transition reach.'
    }
    if($Case.initial.relationshipState-cne'Unmounted'-or$Case.initial.generation-ne$proof.identity.generationAtInit) {
        throw 'Repeated-request initial relationship identity differs.'
    }
    $first=$Case.firstInput
    if($first.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or
        $first.clickedTargetId-cne$proof.identity.targetId-or$first.resolvedTargetId-cne$proof.identity.targetId-or
        $first.clicked-ne$true-or$first.targetSelectionStartDelta-ne1-or$first.targetSelectionEndDelta-ne1-or
        $first.nativeCastRequestDelta-ne1-or$first.nativeRefusalDelta-ne0-or
        $first.dispatchAcceptedDelta-ne0-or$first.dispatchRejectedDelta-ne0-or
        $first.nativeShell.present-ne$true-or$first.nativeShell.inMoveSlot-ne$true-or
        $first.nativeShell.commandObject-ne$proof.identity.commandObject) {
        throw 'Repeated-request first selected-ability input does not own the exact first Mount command.'
    }
    $trigger=$Case.trigger;$repeat=$Case.repeat;$id=$proof.identity
    foreach($flag in @('approachObserved','riderReallyMoving','shellOwned','exactSingleRider')) {
        if($trigger.$flag-ne$true){throw "Repeated-request trigger lacks true $flag."}
    }
    if(-not(Test-KmcFiniteJsonNumber $trigger.riderDisplacement)-or[double]$trigger.riderDisplacement-le0.25-or
        $trigger.geometry.isAdjacent-ne$false-or$trigger.commandObject-ne$id.commandObject-or
        $trigger.moveSlotObject-ne$id.commandObject-or$trigger.started-ne$false-or$trigger.acted-ne$false-or
        $trigger.finished-ne$false-or$trigger.processPresent-ne$false) {
        throw 'Repeated-request trigger missed the exact unacted pending Mount approach.'
    }
    if($repeat.preSelectionAvailability.visible-ne$true-or$repeat.preSelectionAvailability.enabled-ne$true) {
        throw 'Repeated-request pre-selection availability would invalidate the admitted command recheck.'
    }
    if($repeat.availability.visible-ne$true-or$repeat.availability.enabled-ne$false-or
        [string]::IsNullOrWhiteSpace([string]$repeat.availability.reason)-or
        [string]$repeat.availability.reason-cnotmatch '(?i)(pending|already|progress)') {
        throw 'Repeated-request availability did not name the pending native Mount.'
    }
    foreach($flag in @('clicked','firstCommandStarted','firstCommandActed','firstCommandFinished','firstCommandProcessPresent')) {
        if($repeat.$flag-ne$false){throw "Repeated-request input has unexpected true $flag."}
    }
    foreach($flag in @('sameMoveSlot','firstCommandStillOwned','exactSingleRiderAfter','rejectedBeforeSecondCommand')) {
        if($repeat.$flag-ne$true){throw "Repeated-request input lacks true $flag."}
    }
    if($repeat.commandObjectBefore-ne$id.commandObject-or$repeat.moveSlotObjectBefore-ne$id.commandObject-or
        $repeat.moveSlotObjectAfter-ne$id.commandObject-or$repeat.selectedCountAfter-ne1-or
        @($repeat.selectedIdsAfter).Count-ne1-or$repeat.selectedIdsAfter[0]-cne$id.casterId) {
        throw 'Repeated-request input changed exact command ownership or selection.'
    }
    if($repeat.shellCountBefore-ne($Case.initial.shellCount+1)-or
        $repeat.shellCountAfter-ne$repeat.shellCountBefore-or
        $repeat.processBindingCountBefore-ne$Case.initial.processBindingCount-or
        $repeat.processBindingCountAfter-ne$repeat.processBindingCountBefore-or
        $repeat.allocationEventCountAfter-ne$repeat.allocationEventCountBefore) {
        throw 'Repeated-request input created a second shell, process binding or allocation event.'
    }
    $input=$repeat.input
    if($input.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or
        $input.clickedTargetId-cne$id.targetId-or$input.clicked-ne$false-or
        $input.targetSelectionStartDelta-ne1-or$input.targetSelectionEndDelta-ne0-or
        $input.nativeCastRequestDelta-ne0-or$input.nativeRefusalDelta-ne1-or
        $input.dispatchAcceptedDelta-ne0-or$input.dispatchRejectedDelta-ne0-or
        $input.nativePrimaryShellPrepareDelta-ne0) {
        throw 'Repeated-request selected-ability input callback sequence differs.'
    }
    foreach($field in @('NativeCastRequestCount','DispatchAcceptedCount','DispatchRejectedCount','NativePrimaryShellPrepareCount')) {
        if($repeat.controlsAfter.$field-ne$repeat.controlsBefore.$field){throw "Repeated-request input changed control counter: $field."}
    }
    if($repeat.controlsAfter.NativeRefusalCount-ne($repeat.controlsBefore.NativeRefusalCount+1)) {
        throw 'Repeated-request input lacks one exact native refusal.'
    }
    if($repeat.before.relationshipState-cne'Unmounted'-or$repeat.after.relationshipState-cne'Unmounted'-or
        $repeat.before.generation-ne$id.generationAtInit-or$repeat.after.generation-ne$id.generationAtInit-or
        ($repeat.before.ledger|ConvertTo-Json -Depth 30 -Compress)-cne($repeat.after.ledger|ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Repeated-request input changed relationship state or ledger.'
    }
    foreach($actor in @('rider','mount')) {
        foreach($field in @('standard','move','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder','nativePrepareCount')) {
            $before=$repeat.before.$actor.$field;$after=$repeat.after.$actor.$field
            if($null-eq$before-or$null-eq$after-or-not(Test-KmcFiniteJsonNumber $before)-or-not(Test-KmcFiniteJsonNumber $after)-or
                [Math]::Abs([double]$before-[double]$after)-gt0.0001) {
                throw "Repeated-request input changed $actor $field."
            }
        }
    }
    foreach($actor in @('riderPosition','horsePosition')) {
        foreach($axis in @('x','y','z')) {
            if($repeat.before.geometry.$actor.$axis-ne$repeat.after.geometry.$actor.$axis) {
                throw 'Repeated-request synchronous refusal moved an actor.'
            }
        }
    }
    if($Case.oneRequest-ne$true-or$Case.oneTransition-ne$true-or
        $Case.terminal.relationshipState-cne'Mounted'-or
        $Case.terminal.generation-ne($Case.initial.generation+1)-or
        $Case.terminalShellCount-ne($Case.initial.shellCount+1)-or
        $Case.terminalProcessBindingCount-ne($Case.initial.processBindingCount+1)-or
        $Case.terminalControls.NativeCastRequestCount-ne($Case.initial.controls.NativeCastRequestCount+1)-or
        $Case.terminalControls.DispatchAcceptedCount-ne($Case.initial.controls.DispatchAcceptedCount+1)-or
        $Case.terminalControls.NativeRefusalCount-ne($Case.initial.controls.NativeRefusalCount+1)-or
        $Case.terminalControls.TargetSelectionStartCount-ne($Case.initial.controls.TargetSelectionStartCount+2)-or
        $Case.terminalControls.TargetSelectionEndCount-ne($Case.initial.controls.TargetSelectionEndCount+2)) {
        throw 'Repeated-request terminal did not contain exactly one request, process and transition.'
    }
    if($proof.initCount-ne1-or$proof.exactActedObserved-ne$true-or
        $proof.nativeTerminal-ne$true-or$proof.nativeResult-cne'Success'-or
        $proof.resourceWindow.pass-ne$true) {
        throw 'Repeated-request original command lacks its exact acted native terminal and cost proof.'
    }
}