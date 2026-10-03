Set-StrictMode -Version Latest
function Get-KmcPredictionEventField($Event,[string]$Name) {
    if($Event -is [System.Collections.IDictionary]){ if($Event.Contains($Name)){ return $Event[$Name] } else { return $null } }
    if($null -ne $Event.PSObject.Properties[$Name]){ return $Event.$Name }
    return $null
}
function Assert-KmcNativePredictionCommands($Prediction,$Proof) {
    if($null -eq $Prediction -or $Prediction.contract -cne 'native-speculative-init-separated-from-one-committed-request' -or $null -eq $Prediction.commands -or @($Prediction.commands).Count -gt 128){throw 'Missing bounded native prediction evidence.'}
    $identity=$Proof.identity;$actual=@($Proof.samples|Where-Object boundary -CEQ 'init')
    if($actual.Count -ne 1){throw 'Prediction lacks one actual Init.'}
    $ids=@([long]$identity.commandObject);$shells=@([string]$identity.controlIdentity)
    foreach($s in @($Proof.samples)){if($s.simulatingClick -ne $false){throw 'Committed observation was native speculation.'}}
    $events=@($Proof.resourceWindow.events)
    foreach($entry in @($Prediction.commands)){
        $first=$entry.init;$last=$entry.close;$id=$first.identity
        foreach($sample in @($first,$last,$actual[0],$Proof.preClick)){foreach($field in @('frame','gameTicks','allocationSequence')){
            if($null -eq $sample.$field -or $sample.$field -isnot [ValueType] -or [double]$sample.$field -ne [Math]::Truncate([double]$sample.$field)){throw 'Prediction omitted an exact native clock or sequence.'}
        }}
        if($null -eq $id -or $null -eq $id.commandObject -or $id.commandObject -eq 0 -or $id.commandObject -in $ids -or [string]::IsNullOrEmpty([string]$id.controlIdentity) -or $id.controlIdentity -cin $shells){throw 'Prediction shares or omits command/shell identity.'}
        $ids+=@([long]$id.commandObject);$shells+=@([string]$id.controlIdentity)
        foreach($field in @('casterId','targetId','generationAtInit','commandType','abilityGuid')){if($null -eq $id.$field -or $id.$field -cne $identity.$field){throw "Prediction has another $field."}}
        if($first.simulatingClick -ne $true -or $last.simulatingClick -ne $false -or $first.gameTicks -lt $Proof.preClick.gameTicks -or $first.gameTicks -gt $actual[0].gameTicks -or $first.allocationSequence -gt $actual[0].allocationSequence -or $last.gameTicks -lt $first.gameTicks){throw 'Prediction was not observed before the real Init.'}
        foreach($s in @($first,$last)){
            foreach($field in @('commandObject','casterId','targetId','commandType','abilityGuid')){if($null -eq $s.$field -or $s.$field -cne $id.$field){throw 'Prediction live object identity differs.'}}
            if($s.started -ne $false -or $s.acted -ne $false -or $s.processObject -ne 0 -or $s.contextObject -ne 0){throw 'Prediction executed or bound a native process.'}
        }
        if($id.processObject -ne 0 -or $id.contextObject -ne 0){throw 'Prediction Init already bound a process.'}
        $matching=@($events|Where-Object command -EQ $id.commandObject)
        if(@($matching|Where-Object boundary -CEQ 'admission-after').Count -ne 1){throw 'Prediction lacks its exact temporary admission.'}
        # Since preview.152 the allocation trace also records UnitCommand.Interrupt on the rider's commands. A speculative
        # command ends through its temporary container's disposal (UnitCommands+Temporary.Dispose, called by the native
        # hover prediction after the simulated click has ended), still unstarted and unacted: that disposal is the native
        # end of the speculation, not a side effect. Any other interrupt source remains an escape (preview.154 rule).
        $temporaryDisposal=@($matching|Where-Object {[string](Get-KmcPredictionEventField $_ 'boundary') -ceq 'command-interrupt-before' -and ([string](Get-KmcPredictionEventField $_ 'detail')).Contains('UnitCommands+Temporary.Dispose')}).Count -ge 1
        foreach($e in $matching){
            $boundary=[string](Get-KmcPredictionEventField $e 'boundary')
            $speculativeAdmission=(Get-KmcPredictionEventField $e 'simulatingClick') -eq $true -and $boundary -cin @('admission-before','admission-after','command-eligibility')
            $disposalInterrupt=$temporaryDisposal -and (($boundary -ceq 'command-interrupt-before' -and ([string](Get-KmcPredictionEventField $e 'detail')).Contains('UnitCommands+Temporary.Dispose')) -or ($boundary -ceq 'command-interrupt-after' -and [string](Get-KmcPredictionEventField $e 'result') -ceq 'Interrupt' -and (Get-KmcPredictionEventField $e 'finished') -eq $true))
            if((Get-KmcPredictionEventField $e 'started') -ne $false -or (Get-KmcPredictionEventField $e 'acted') -ne $false -or (Get-KmcPredictionEventField $e 'sequence') -gt $actual[0].allocationSequence -or -not($speculativeAdmission -or $disposalInterrupt)){throw 'Prediction escaped temporary admission or had a native side effect.'}
        }
    }
    foreach($e in $events){
        $sim=if($e -is [System.Collections.IDictionary]){$e['simulatingClick']}elseif($e.PSObject.Properties['simulatingClick']){$e.simulatingClick}else{$null}
        if($sim -eq $true -and $e.state.actor -cin @($identity.casterId,$Proof.mountId) -and $e.boundary -cmatch '^(cost-|actor-cost-|prepare-|clear-|combat-clear|opportunity-|approach-movement-|native-movement-displacement)'){throw 'Native speculation added a resource or movement callback.'}
    }
}
function Assert-KmcNativeMountPointer($InputProof,$Proof) {
    $p=$InputProof
    if($null -eq $p -or $p.contract -cne 'native-selected-ability-hover-prediction-and-ignore-click-before-one-commit' -or $p.clicked -ne $true -or $p.ready -ne $true -or $p.restored -ne $true -or @($p.samples).Count -lt 5 -or @($p.samples).Count -gt 128){throw 'Native Mount pointer admission/restoration incomplete.'}
    Assert-KmcNativePredictionCommands $Proof.predictionCommands $Proof
    if(@($Proof.predictionCommands.commands).Count -eq 0){throw 'Native TB pointer observed no speculative Init.'}
    $samples=@($p.samples);$id=$Proof.identity;$first=$samples[0];$before=$samples[-2];$after=$samples[-1];$admitted=$samples[-3]
    if($p.casterId -cne $id.casterId -or $p.targetId -cne $id.targetId -or $id.abilityGuid -cne 'f053faad986631688defa003cd7bda0e' -or $first.boundary -cne 'before-set-ability' -or $samples[1].boundary -cne 'after-set-ability' -or $before.boundary -cne 'before-real-click' -or $after.boundary -cne 'after-real-click'){throw 'Native Mount pointer order or actor differs.'}
    foreach($pair in @(@('riderPosition','riderPosition'),@('targetPosition','horsePosition'))){for($axis=0;$axis -lt 3;$axis++){
        $measured=$first.($pair[0])[$axis];$baseline=$Proof.preClick.state.geometry.($pair[1]).(@('x','y','z')[$axis])
        if($null -eq $measured -or $null -eq $baseline -or $measured -ne $baseline){throw 'Native pointer differs from the post-selection geometry baseline.'}
    }}
    if($first.frame -ne $Proof.preClick.frame -or $first.gameTicks -ne $Proof.preClick.gameTicks){throw 'Native pointer did not begin at the exact pre-click baseline.'}
    $frame=$Proof.preClick.frame;$ticks=$Proof.preClick.gameTicks
    for($i=0;$i -lt $samples.Count;$i++){
        $s=$samples[$i]
        if($null -eq $s.frame -or $null -eq $s.gameTicks -or $s.frame -lt $frame -or $s.gameTicks -lt $ticks -or $s.simulatingClick -ne $false -or $s.turnStatus -cne 'Acting' -or $s.turnActor -cne $id.casterId -or $s.casterId -cne $id.casterId -or $s.targetId -cne $id.targetId -or $s.abilityGuid -cne $id.abilityGuid -or @($s.selectedIds).Count -ne 1 -or $s.selectedIds[0] -cne $id.casterId){throw 'Native pointer lost exact selection, mode or time order.'}
        $frame=$s.frame;$ticks=$s.gameTicks
        foreach($field in @('turnObject','pointerObject','handlerObject','abilityObject')){if($null -eq $s.$field -or $s.$field -eq 0 -or $s.$field -ne $first.$field){throw "Native pointer replaced $field."}}
        foreach($actor in @('riderPosition','targetPosition')){
            if(@($s.$actor).Count -ne 3 -or ($s.$actor|ConvertTo-Json -Compress) -cne ($first.$actor|ConvertTo-Json -Compress)){throw 'Native prediction moved an actor.'}
            foreach($v in @($s.$actor)){if($null -eq $v -or [double]::IsNaN([double]$v) -or [double]::IsInfinity([double]$v)){throw 'Native prediction moved an actor.'}}
        }
        if($i -lt $samples.Count-1 -and ($s.riderCommandsEmpty -ne $true -or $s.moveSlotObject -ne 0)){throw 'Native pointer had a real command before its one click.'}
        if($i -gt 0 -and $i -lt $samples.Count-1 -and ($s.selectedHandlerObject -ne $s.handlerObject -or $s.selectedAbilityObject -ne $s.abilityObject)){throw 'Native prediction selected another handler or ability.'}
        if($i -ge 2 -and $i -lt $samples.Count-2 -and ($s.boundary -cne 'native-admission' -or $s.ignored -ne ($i -ne $samples.Count-3))){throw 'Native pointer bypassed IgnoreClick.'}
    }
    $init=@($Proof.samples|Where-Object boundary -CEQ 'init')[0]
    if($first.selectedAbilityObject -ne 0 -or $after.riderCommandsEmpty -ne $false -or $after.moveSlotObject -ne $id.commandObject -or $before.ignored -ne $false -or $after.ignored -ne $false -or $before.frame -ne $admitted.frame -or $after.frame -ne $before.frame -or $init.frame -ne $before.frame -or $admitted.gameTicks -ne $init.gameTicks -or $before.gameTicks -ne $init.gameTicks -or $after.gameTicks -ne $init.gameTicks){throw 'Native click did not commit its admitted frame and exact command.'}
    $pb=@($Proof.nativeApproachPath.events|Where-Object boundary -CEQ 'preview-before-click');$pr=@($Proof.nativeApproachPath.events|Where-Object boundary -CEQ 'preview-return')
    if($pb.Count -ne 1 -or $pr.Count -ne 1 -or $pb[0].frame -ne $before.frame){throw 'Native pointer lacks its synchronous path observation.'}
    foreach($path in @($admitted.preview,$before.preview,$pb[0].path,$pr[0].path)){
        if($null -eq $path -or $null -eq $path.pathObject -or $path.pathObject -eq 0 -or $path.pathState -cne 'Complete' -or $path.pathError -ne $false -or @($path.points).Count -eq 0 -or ($path.points|ConvertTo-Json -Depth 10 -Compress) -cne ($admitted.preview.points|ConvertTo-Json -Depth 10 -Compress) -or $path.pathObject -ne $admitted.preview.pathObject){throw 'Actual Mount consumed a different or incomplete admitted preview.'}
    }
    if($pr[0].turnObject -ne $first.turnObject){throw 'Native preview belongs to another turn.'}
}
