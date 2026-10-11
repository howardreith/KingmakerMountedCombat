# Exact native Stop input and unacted command evidence.
Set-StrictMode -Version Latest
function Assert-KmcStopCommandProof {
    param($Proof)
    if($Proof.contract -cne 'unacted-native-stop-no-cost-or-transition') {throw 'Missing unacted Stop contract.'}
    foreach($flag in @('pass','sameCommandAtEveryBoundary','nativeTerminal','traceComplete')) {
        if($Proof.$flag -ne $true) {throw "Unacted proof lacks true $flag."}
    }
    if($Proof.initCount -ne 1 -or @($Proof.errors).Count -ne 0 -or $Proof.nativeResult -cnotin @('Interrupt','Fail')) {throw 'Unacted proof has no truthful native failure terminal.'}
    $id=$Proof.identity
    if($id.commandObject -eq 0 -or $id.processObject -ne 0 -or $id.contextObject -ne 0 -or
        $id.commandType -cne 'Move' -or $id.abilityGuid -cne 'f053faad986631688defa003cd7bda0e' -or
        $id.targetId -cne $Proof.mountId -or [string]::IsNullOrEmpty($Proof.mountId)) {throw 'Unacted proof has a process or invalid Mount identity.'}
    foreach($field in @('controlIdentity','casterId','targetId')) {if([string]::IsNullOrEmpty($id.$field)){throw 'Unacted identity is incomplete.'}}
    $names=@('init','click-admission','move-slot-installation','approach-start','unacted-terminal')
    $samples=@($Proof.samples)
    if($samples.Count -ne 5){throw 'Unacted proof must contain exactly five boundaries and no acted, cost or delivery sample.'}
    $pre=$Proof.preClick
    if($null -eq $pre -or $pre.gameTicks -gt $samples[0].gameTicks -or @($pre.state.selectedIds).Count -ne 1 -or $pre.state.selectedIds[0] -cne $id.casterId) {throw 'Unacted pre-click selection/baseline is not the exact rider.'}
    if($pre.state.generation -ne $id.generationAtInit -or $pre.state.relationshipState -cne 'Unmounted') {throw 'Unacted pre-click relationship differs.'}
    $previous=[long]$pre.gameTicks
    for($i=0;$i -lt 5;$i++) {
        $sample=$samples[$i]
        if($sample.boundary -cne $names[$i] -or $sample.acted -ne $false -or $sample.gameTicks -lt $previous) {throw 'Unacted observation boundary, acted flag or order differs.'}
        $previous=[long]$sample.gameTicks
        foreach($field in @('commandObject','controlIdentity','processObject','contextObject','casterId','targetId','generationAtInit','commandType','abilityGuid')) {
            if($null -eq $sample.identity.$field -or $sample.identity.$field -cne $id.$field){throw "Mixed unacted identity: $field."}
        }
        if($sample.state.relationshipState -cne 'Unmounted' -or $sample.state.generation -ne $id.generationAtInit) {throw 'Unacted window changed relationship.'}
        foreach($field in @($pre.state.ledger.PSObject.Properties.Name)) {
            if($sample.state.ledger.$field -ne $pre.state.ledger.$field -or $Proof.ledgerDelta.$field -ne 0){throw 'Unacted window changed ledger.'}
        }
    }
    $terminal=$samples[-1]
    if($terminal.finished -ne $true -or $terminal.result -cne $Proof.nativeResult -or $null -ne $terminal.processEnded) {throw 'Unacted terminal is not a finished process-free native failure.'}
    foreach($event in @($Proof.resourceWindow.events|Where-Object {$_.state.actor -cin @($id.casterId,$Proof.mountId)})) {
        if($event.boundary -cmatch '^(cost-|actor-cost-|prepare-|clear-|combat-clear)') {throw 'Unacted window contains native cost, preparation or reset.'}
    }
    $elapsed=([long]$terminal.gameTicks-[long]$pre.gameTicks)/10000000.0
    foreach($actor in @('rider','mount')) {
        foreach($field in @('standard','move','swift')) {
            $b=$pre.state.$actor.$field;$a=$terminal.state.$actor.$field
            if($null -eq $a -or $null -eq $b -or [double]::IsNaN([double]$a) -or [double]::IsInfinity([double]$a) -or
                [double]::IsNaN([double]$b) -or [double]::IsInfinity([double]$b) -or
                [Math]::Abs([double]$a-[Math]::Max(0.0,[double]$b-$elapsed)) -gt 0.05) {throw "Unacted window refunded or added $actor $field debt."}
        }
    }
    Assert-KmcRelationshipReactionResources $Proof 0
}


function Assert-KmcStopInput($Case) {
    if($Case.contract -cne 'native-stop-during-exact-mount-approach' -or $Case.noResidue -ne $true) {throw 'Stop case is undeclared or retained residue.'}
    $proof=$Case.commandProof
    Assert-KmcStopCommandProof $proof
    if($proof.nativeResult -cne 'Interrupt') {throw 'Native Stop must end this Mount with Interrupt.'}
    $id=$proof.identity;$input=$Case.input;$trigger=$Case.trigger
    if($input.contract -cne 'native-stop-surrounds-exact-unacted-mount-terminal' -or $input.complete -ne $true -or
        @($input.errors).Count -ne 0 -or $input.commandObject -ne $id.commandObject -or $input.casterId -cne $id.casterId) {throw 'Stop instrumentation is incomplete or belongs to another command.'}
    if($input.allocationTraceComplete -ne $true) {throw 'Stop allocation trace is incomplete.'}
    foreach($event in @($input.allocationEvents|Where-Object {$_.state.actor -cin @($id.casterId,$proof.mountId)})) {
        if($event.boundary -cmatch '^(cost-|actor-cost-|prepare-|clear-|combat-clear|admission-)') {throw 'Stop input contains native cost, preparation, reset or another command admission.'}
    }
    if(@($input.observerHooks).Count -ne 2){throw 'Stop hook inventory differs.'}
    foreach($token in @('060000B9','060027B2')) {
        $hook=@($input.observerHooks|Where-Object token -CEQ $token)
        if($hook.Count -ne 1 -or $hook[0].moduleMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Stop hook identity differs.'}
    }
    $events=@($input.events);$names=@('stop-before','command-ended','stop-after')
    if($events.Count -ne 3){throw 'Stop requires exactly one input surrounding one native terminal.'}
    $terminal=@($proof.samples)[-1]
    for($i=0;$i -lt 3;$i++) {
        $e=$events[$i]
        if($e.boundary -cne $names[$i] -or $e.sequence -ne $i+1 -or $e.stopDepth -ne 1 -or
            $e.commandObject -ne $id.commandObject -or $e.casterId -cne $id.casterId -or
            $e.processObject -ne 0 -or $e.contextObject -ne 0 -or $e.started -ne $false -or $e.acted -ne $false -or
            $e.gameTicks -ne $terminal.gameTicks -or $e.frame -ne $terminal.frame) {throw 'Stop callback identity, state or synchronous order differs.'}
        if(@($e.state.selectedIds).Count -ne 1 -or $e.state.selectedIds[0] -cne $id.casterId -or
            $e.state.generation -ne $id.generationAtInit -or $e.state.relationshipState -cne 'Unmounted') {throw 'Stop selection or relationship differs.'}
        foreach($field in @($proof.preClick.state.ledger.PSObject.Properties.Name)) {
            if($e.state.ledger.$field -ne $proof.preClick.state.ledger.$field) {throw 'Stop changed the transition ledger.'}
        }
        if($i -eq 0) {
            if($e.finished -ne $false -or $e.moveSlotObject -ne $id.commandObject){throw 'Stop missed the pending Move slot.'}
        } elseif($e.finished -ne $true -or $e.result -cne 'Interrupt' -or $e.moveSlotObject -notin @(0,$id.commandObject)) {throw 'Stop lacks exact native Interrupt terminal.'}
        foreach($actor in @('rider','mount')) {
            foreach($field in @('standard','move','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder','nativePrepareCount')) {
                $a=$e.state.$actor.$field;$b=$terminal.state.$actor.$field
                if($null -eq $a -or $null -eq $b -or [double]::IsNaN([double]$a) -or [double]::IsInfinity([double]$a) -or
                    [Math]::Abs([double]$a-[double]$b) -gt 0.0001) {throw 'Stop changed a resource inside its native input callback.'}
            }
        }
    }
    $approach=@($proof.samples|Where-Object boundary -CEQ 'approach-start')[0]
    if($trigger.approachObserved -ne $true -or $trigger.riderReallyMoving -ne $true -or
        $trigger.gameTicks -lt $approach.gameTicks -or $trigger.gameTicks -ne $events[0].gameTicks -or $trigger.frame -ne $events[0].frame -or
        $trigger.commandObject -ne $id.commandObject -or $trigger.moveSlotObject -ne $id.commandObject -or
        $trigger.started -ne $false -or $trigger.acted -ne $false -or $trigger.finished -ne $false) {throw 'Stop trigger missed the exact pending native approach.'}
    $start=$proof.preClick.state.geometry;$g=$trigger.geometry
    foreach($field in @('riderPosition','horsePosition','centerDistance','horizontalDistance','riderCorpulence','horseCorpulence','legalAdjacencyEnvelope','isAdjacent')) {
        if(($g.$field|ConvertTo-Json -Depth 5 -Compress) -cne ($events[0].state.geometry.$field|ConvertTo-Json -Depth 5 -Compress)){throw 'Stop trigger geometry differs from its actual input callback.'}
    }
    foreach($v in @($start,$g)) {
        foreach($f in @('centerDistance','horizontalDistance','riderCorpulence','horseCorpulence','legalAdjacencyEnvelope')) {
            if($null -eq $v.$f -or [double]::IsNaN([double]$v.$f) -or [double]::IsInfinity([double]$v.$f) -or [double]$v.$f -lt 0){throw 'Stop geometry is not finite.'}
        }
        foreach($point in @($v.riderPosition,$v.horsePosition)){foreach($axis in @('x','y','z')) {
            if($null -eq $point.$axis -or [double]::IsNaN([double]$point.$axis) -or [double]::IsInfinity([double]$point.$axis)){throw 'Stop position is not finite.'}
        }}
        $dx=[double]$v.riderPosition.x-[double]$v.horsePosition.x;$dz=[double]$v.riderPosition.z-[double]$v.horsePosition.z
        $distance=[Math]::Sqrt($dx*$dx+$dz*$dz);$envelope=[double]$v.riderCorpulence+[double]$v.horseCorpulence+1.5
        if($v.isAdjacent -ne $false -or $v.centerDistance -le $envelope -or $distance -le $envelope -or
            [Math]::Abs($distance-[double]$v.horizontalDistance) -gt 0.001 -or [Math]::Abs($envelope-[double]$v.legalAdjacencyEnvelope) -gt 0.0001){throw 'Stop did not occur outside the native transition envelope.'}
    }
    $dx=[double]$g.riderPosition.x-[double]$start.riderPosition.x;$dz=[double]$g.riderPosition.z-[double]$start.riderPosition.z
    $moved=[Math]::Sqrt($dx*$dx+$dz*$dz)
    if($null -eq $trigger.riderDisplacement -or [double]::IsNaN([double]$trigger.riderDisplacement) -or [double]::IsInfinity([double]$trigger.riderDisplacement) -or $moved -le 0.25 -or [Math]::Abs($moved-[double]$trigger.riderDisplacement) -gt 0.0001){throw 'Stop lacks measured rider approach displacement.'}
}
