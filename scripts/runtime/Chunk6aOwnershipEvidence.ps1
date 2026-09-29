# Exact native ownership-loss and guarded restoration evidence.
Set-StrictMode -Version Latest
function Test-KmcOwnershipJsonEqual($A,$B) {
    return ($A|ConvertTo-Json -Depth 40 -Compress)-ceq($B|ConvertTo-Json -Depth 40 -Compress)
}
function Assert-KmcOwnershipMethod($Method) {
    if($Method.declaringType-cne'Kingmaker.UnitLogic.UnitDescriptor'-or$Method.method-cne'SetMaster'-or
       $Method.token-cne'06001F17'-or$Method.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7') {
        throw 'Ownership evidence identifies another native method.'
    }
}
function Assert-KmcOwnershipCommandProof($Proof) {
    if($Proof.contract-cne'unacted-native-ownership-loss-no-cost-or-transition'){throw 'Missing unacted ownership-loss contract.'}
    foreach($flag in @('pass','sameCommandAtEveryBoundary','nativeTerminal','traceComplete')){if($Proof.$flag-ne$true){throw "Unacted ownership proof lacks true $flag."}}
    if($Proof.initCount-ne1-or@($Proof.errors).Count-ne0-or$Proof.nativeResult-cnotin@('Interrupt','Fail')){throw 'Ownership-invalidated Mount lacks a truthful native terminal.'}
    $id=$Proof.identity
    if($id.commandObject-eq0-or$id.processObject-ne0-or$id.contextObject-ne0-or$id.commandType-cne'Move'-or
       $id.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$id.targetId-cne$Proof.mountId-or[string]::IsNullOrEmpty($Proof.mountId)){
        throw 'Ownership proof has a process or invalid Mount identity.'
    }
    foreach($field in @('controlIdentity','casterId','targetId')){if([string]::IsNullOrEmpty($id.$field)){throw 'Ownership command identity is incomplete.'}}
    $names=@('init','click-admission','move-slot-installation','approach-start','unacted-terminal');$samples=@($Proof.samples)
    if($samples.Count-ne5){throw 'Ownership proof must contain exactly five unacted boundaries.'}
    $pre=$Proof.preClick
    if($null-eq$pre-or$pre.gameTicks-gt$samples[0].gameTicks-or@($pre.state.selectedIds).Count-ne1-or$pre.state.selectedIds[0]-cne$id.casterId){throw 'Ownership pre-click baseline is not the exact selected rider.'}
    if($pre.state.generation-ne$id.generationAtInit-or$pre.state.relationshipState-cne'Unmounted'){throw 'Ownership pre-click relationship differs.'}
    $previous=[long]$pre.gameTicks
    for($i=0;$i-lt5;$i++){
        $sample=$samples[$i]
        if($sample.boundary-cne$names[$i]-or$sample.acted-ne$false-or$sample.gameTicks-lt$previous){throw 'Ownership command boundary, acted flag or order differs.'};$previous=[long]$sample.gameTicks
        foreach($field in @('commandObject','controlIdentity','processObject','contextObject','casterId','targetId','generationAtInit','commandType','abilityGuid')){
            if($null-eq$sample.identity.$field-or$sample.identity.$field-cne$id.$field){throw "Mixed ownership command identity: $field."}
        }
        if($sample.state.relationshipState-cne'Unmounted'-or$sample.state.generation-ne$id.generationAtInit){throw 'Ownership window changed the relationship.'}
        foreach($field in @($pre.state.ledger.PSObject.Properties.Name)){
            if($sample.state.ledger.$field-ne$pre.state.ledger.$field-or$Proof.ledgerDelta.$field-ne0){throw 'Ownership window changed the transition ledger.'}
        }
    }
    $terminal=$samples[-1]
    if($terminal.finished-ne$true-or$terminal.result-cne$Proof.nativeResult-or$null-ne$terminal.processEnded){throw 'Ownership terminal is not a finished process-free native failure.'}
    foreach($event in @($Proof.resourceWindow.events|Where-Object {$_.state.actor-cin@($id.casterId,$Proof.mountId)})){
        if($event.boundary-cmatch'^(cost-|actor-cost-|prepare-|clear-|combat-clear)'){throw 'Ownership window contains native cost, preparation or reset.'}
    }
    $elapsed=([long]$terminal.gameTicks-[long]$pre.gameTicks)/10000000.0
    foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift')){
        $b=$pre.state.$actor.$field;$a=$terminal.state.$actor.$field
        if($null-eq$a-or$null-eq$b-or[double]::IsNaN([double]$a)-or[double]::IsInfinity([double]$a)-or
           [double]::IsNaN([double]$b)-or[double]::IsInfinity([double]$b)-or
           [Math]::Abs([double]$a-[Math]::Max(0.0,[double]$b-$elapsed))-gt0.05){throw "Ownership window refunded or added $actor $field debt."}
    }}
    Assert-KmcRelationshipReactionResources $Proof 0
}
function Assert-KmcOwnershipBefore($Before,$Proof) {
    $id=$Proof.identity
    if($Before.riderId-cne$id.casterId-or$Before.mountId-cne$id.targetId-or$Before.riderId-ceq$Before.mountId-or
       -not(Test-KmcExactJsonInteger $Before.riderObject)-or-not(Test-KmcExactJsonInteger $Before.mountObject)-or
       $Before.riderObject-eq0-or$Before.mountObject-eq0-or$Before.riderObject-eq$Before.mountObject){throw 'Ownership actor identity differs.'}
    if($Before.riderPetId-cne$Before.mountId-or$Before.riderPetObject-ne$Before.mountObject-or$Before.riderPetUniqueId-cne$Before.mountId-or
       $Before.masterId-cne$Before.riderId-or$Before.masterObject-ne$Before.riderObject-or$Before.mountIsPet-ne$true){throw 'Original reciprocal ownership differs.'}
    if(-not(Test-KmcOwnershipJsonEqual $Before.riderGroup $Before.mountGroup)-or$Before.riderGroup.object-eq0-or
       [string]::IsNullOrWhiteSpace([string]$Before.riderGroup.id)){throw 'Original group identity differs.'}
    $members=@($Before.riderGroup.members)
    foreach($actor in @(@($Before.riderId,$Before.riderObject),@($Before.mountId,$Before.mountObject))){
        $match=@($members|Where-Object {$_.id-ceq$actor[0]-and$_.object-eq$actor[1]});if($match.Count-ne1){throw 'Original group member identity differs.'}
    }
    if(-not(Test-KmcOwnershipJsonEqual $Before.riderFaction $Before.mountFaction)-or$Before.riderFaction.object-eq0-or
       [string]::IsNullOrWhiteSpace([string]$Before.riderFaction.guid)){throw 'Original faction identity differs.'}
    if(-not(Test-KmcOwnershipJsonEqual $Before.riderFactionAttackSource $Before.mountAttackFactions)-or$Before.mountAttackFactionsPlayerEnemy-isnot[bool]){throw 'Original attack-faction rebuild source differs.'}
    foreach($faction in @($Before.mountAttackFactions)){
        if([string]::IsNullOrWhiteSpace([string]$faction.guid)-or-not(Test-KmcExactJsonInteger $faction.object)-or$faction.object-eq0){throw 'Attack-faction identity is malformed.'}
    }
    if($Before.riderPlayerFaction-isnot[bool]-or$Before.mountPlayerFaction-isnot[bool]-or$Before.riderPlayerFaction-ne$Before.mountPlayerFaction){throw 'Original player-faction state differs.'}
    $weather=$Before.weather
    if($weather.present-isnot[bool]-or$weather.ownerMatches-isnot[bool]-or$weather.lastBuffPresent-isnot[bool]-or
       $weather.present-ne$Before.riderPlayerFaction-or$weather.ownerMatches-ne$weather.present-or$weather.lastBuffPresent-ne$false-or
       -not(Test-KmcExactJsonInteger $weather.ownerObject)-or-not(Test-KmcExactJsonInteger $weather.lastBuffObject)-or
       $weather.lastBuffObject-ne0-or($weather.present-and$weather.ownerObject-eq0)-or(-not$weather.present-and$weather.ownerObject-ne0)){
        throw 'Original weather-part/null-buff precondition differs.'
    }
}
function Assert-KmcOwnershipDetached($Before,$Detached) {
    foreach($field in @('riderId','riderObject','mountId','mountObject','riderGroup','mountGroup','riderFaction','mountFaction','riderFactionAttackSource','mountAttackFactions','mountAttackFactionsPlayerEnemy','riderPlayerFaction','mountPlayerFaction')){
        if(-not(Test-KmcOwnershipJsonEqual $Before.$field $Detached.$field)){throw 'Ownership detach changed a native restoration input.'}
    }
    if($null-ne$Detached.riderPetId-or$Detached.riderPetObject-ne0-or-not[string]::IsNullOrEmpty([string]$Detached.riderPetUniqueId)-or
       $null-ne$Detached.masterId-or$Detached.masterObject-ne0-or$Detached.mountIsPet-ne$false){throw 'Native ownership detach did not clear exact reciprocal references.'}
    $weather=$Detached.weather
    if($weather.present-ne$false-or$weather.ownerMatches-ne$false-or$weather.ownerObject-ne0-or$weather.lastBuffPresent-ne$false-or$weather.lastBuffObject-ne0){throw 'Native ownership detach retained weather-part state.'}
}
function Assert-KmcOwnershipGeometry($Trigger,$Proof) {
    $id=$Proof.identity;$approach=@($Proof.samples|Where-Object boundary -CEQ 'approach-start')[0];$terminal=@($Proof.samples)[-1]
    if($Trigger.approachObserved-ne$true-or$Trigger.riderReallyMoving-ne$true-or$Trigger.gameTicks-lt$approach.gameTicks-or
       $Trigger.gameTicks-gt$terminal.gameTicks-or$Trigger.commandObject-ne$id.commandObject-or$Trigger.moveSlotObject-ne$id.commandObject-or
       $Trigger.started-ne$false-or$Trigger.acted-ne$false-or$Trigger.finished-ne$false){throw 'Ownership trigger missed the exact pending native approach.'}
    $start=$Proof.preClick.state.geometry;$g=$Trigger.geometry
    foreach($v in @($start,$g)){
        foreach($f in @('centerDistance','horizontalDistance','riderCorpulence','horseCorpulence','legalAdjacencyEnvelope')){
            if($null-eq$v.$f-or[double]::IsNaN([double]$v.$f)-or[double]::IsInfinity([double]$v.$f)-or[double]$v.$f-lt0){throw 'Ownership geometry is not finite.'}
        }
        foreach($point in @($v.riderPosition,$v.horsePosition)){foreach($axis in @('x','y','z')){if($null-eq$point.$axis-or[double]::IsNaN([double]$point.$axis)-or[double]::IsInfinity([double]$point.$axis)){throw 'Ownership position is not finite.'}}}
        $dx=[double]$v.riderPosition.x-[double]$v.horsePosition.x;$dz=[double]$v.riderPosition.z-[double]$v.horsePosition.z
        $distance=[Math]::Sqrt($dx*$dx+$dz*$dz);$envelope=[double]$v.riderCorpulence+[double]$v.horseCorpulence+1.5
        if($v.isAdjacent-ne$false-or$v.centerDistance-le$envelope-or$distance-le$envelope-or[Math]::Abs($distance-[double]$v.horizontalDistance)-gt0.001-or[Math]::Abs($envelope-[double]$v.legalAdjacencyEnvelope)-gt0.0001){throw 'Ownership stimulus was not outside the transition envelope.'}
    }
    $dx=[double]$g.riderPosition.x-[double]$start.riderPosition.x;$dz=[double]$g.riderPosition.z-[double]$start.riderPosition.z;$moved=[Math]::Sqrt($dx*$dx+$dz*$dz)
    if($null-eq$Trigger.riderDisplacement-or[double]::IsNaN([double]$Trigger.riderDisplacement)-or[double]::IsInfinity([double]$Trigger.riderDisplacement)-or
       $moved-le0.25-or[Math]::Abs($moved-[double]$Trigger.riderDisplacement)-gt0.0001){throw 'Ownership stimulus lacks measured rider approach displacement.'}
}
function Assert-KmcOwnershipChange($Case) {
    if($Case.contract-cne'native-ownership-loss-during-exact-mount-approach'-or$Case.noResidue-ne$true-or$Case.restored-ne$true-or
       $Case.commandTerminalBeforeRestoration-ne$true-or$Case.inputsUnchangedAfterDetach-ne$true){throw 'Ownership case is undeclared, unrestored, or retained residue.'}
    if($Case.stimulusCount-ne1-or$Case.restorationCount-ne1-or$Case.diagnosticInterruptCount-ne0){throw 'Ownership case did not use one stimulus, one restoration, and zero measured interrupts.'}
    Assert-KmcOwnershipMethod $Case.nativeMethod
    $proof=$Case.commandProof;Assert-KmcOwnershipCommandProof $proof;Assert-KmcOwnershipGeometry $Case.trigger $proof
    Assert-KmcOwnershipBefore $Case.ownershipBefore $proof
    Assert-KmcOwnershipDetached $Case.ownershipBefore $Case.ownershipAfterDetach
    if(-not(Test-KmcOwnershipJsonEqual $Case.ownershipAfterDetach $Case.ownershipBeforeRestoration)){throw 'Ownership changed between detach terminal and guarded restoration.'}
    if(-not(Test-KmcOwnershipJsonEqual $Case.ownershipBefore $Case.ownershipAfterRestoration)){throw 'Guarded ownership restoration did not restore every captured side effect.'}
    $stimulus=$Case.stimulus;Assert-KmcOwnershipMethod $stimulus.method
    if($stimulus.contract-cne'one-native-unit-descriptor-set-master-null'-or$stimulus.count-ne1-or
       -not(Test-KmcOwnershipJsonEqual $stimulus.before $Case.ownershipBefore)-or-not(Test-KmcOwnershipJsonEqual $stimulus.after $Case.ownershipAfterDetach)-or
       $stimulus.frameAfter-lt$stimulus.frameBefore-or$stimulus.gameTicksAfter-lt$stimulus.gameTicksBefore){throw 'Native ownership stimulus boundary differs.'}
    foreach($command in @($stimulus.commandBefore,$stimulus.commandAfter)){
        if($command.id-ne$proof.identity.commandObject-or$command.executor-cne$proof.identity.casterId-or$command.started-ne$false-or
           $command.acted-ne$false-or$command.finished-ne$false-or$command.result-cne'None'){throw 'Native ownership stimulus did not surround the exact pending Mount.'}
    }
    $restore=$Case.restoration;Assert-KmcOwnershipMethod $restore.method
    if($restore.cleanup-ne$false-or$restore.vacantReciprocalReferences-ne$true-or$restore.inputsUnchanged-ne$true-or
       $restore.attempted-ne$true-or$restore.pass-ne$true-or$restore.count-ne1-or
       -not(Test-KmcOwnershipJsonEqual $restore.before $Case.ownershipBeforeRestoration)-or
       -not(Test-KmcOwnershipJsonEqual $restore.after $Case.ownershipAfterRestoration)){throw 'Guarded native ownership restoration boundary differs.'}
}
