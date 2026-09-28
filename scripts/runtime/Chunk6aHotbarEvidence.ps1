# Native input-origin proof; the complete positive command/resource proof remains required.
Set-StrictMode -Version Latest
function Assert-KmcHotbarInput($InputProof,$CommandProof) {
    $p=$InputProof;$id=$CommandProof.identity
    if($p.contract -cne 'live-action-bar-origin-for-one-exact-mount' -or $p.complete -ne $true -or @($p.errors).Count -ne 0){throw 'Hotbar observation incomplete.'}
    if($CommandProof.pass -ne $true -or $id.commandObject -eq 0 -or $id.abilityGuid -cne 'f053faad986631688defa003cd7bda0e' -or $id.commandType -cne 'Move'){throw 'Exact positive Mount proof required.'}
    foreach($key in @('slotObject','mechanicObject','abilityObject','handlerObject')){if($p.$key -eq 0){throw 'Missing live UI identity.'}}
    $reg=$p.registration
    if($reg.managerObject -eq 0 -or $reg.ownerObject -eq 0 -or $reg.slotObject -ne $p.slotObject -or $reg.occurrences -ne 1 -or $reg.active -ne $true -or $reg.casterId -cne $id.casterId -or $reg.route -cnotin @('main','group') -or ($p.groupSlot -and $reg.route -cne 'group') -or (!$p.groupSlot -and $reg.route -cne 'main')){throw 'Slot is not uniquely registered in the live native action bar.'}
    $hooks=@(
        @('060044BA','Kingmaker.UI.ActionBar.ActionBarGroupSlot.OnClick','GroupBefore','GroupAfter'),
        @('06004504','Kingmaker.UI.ActionBar.ActionBarSlot.OnClick','SlotBefore','SlotAfter'),
        @('06002F5D','Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility.OnClick','MechanicBefore','MechanicAfter'),
        @('060093F8','Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.SetAbility','AbilityBefore','AbilityAfter'),
        @('060093F6','Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.OnClick','ClickBefore','ClickAfter'))
    if(@($p.observerHooks).Count -ne 5){throw 'Missing native input hook.'}
    foreach($h in $hooks){$actual=@($p.observerHooks|Where-Object token -CEQ $h[0]);if($actual.Count -ne 1 -or $actual[0].method -cne $h[1] -or $actual[0].prefix -cne $h[2] -or $actual[0].postfix -cne $h[3] -or $actual[0].moduleMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native input hook identity differs.'}}
    $expected=@('slot-before','mechanic-before','ability-before','ability-after','mechanic-after','slot-after')
    if($p.groupSlot -eq $true){$expected=@('group-before')+$expected+@('group-after')}
    $expected+=@('click-before','click-after');$events=@($p.events)
    if($events.Count -ne $expected.Count){throw 'Input callback count differs.'}
    $pre=$p.baseline
    if(@($pre.selectedIds).Count -ne 1 -or $pre.selectedIds[0] -cne $id.casterId -or $pre.relationshipState -cne 'Unmounted' -or $pre.generation -ne $id.generationAtInit){throw 'Input baseline lacks exact selected unmounted rider.'}
    $admission=@($CommandProof.samples|Where-Object boundary -CEQ 'click-admission')
    if($admission.Count -ne 1){throw 'Positive proof lacks one click admission.'}
    for($i=0;$i -lt $events.Count;$i++) {
        $e=$events[$i];$n=$expected[$i]
        if($e.sequence -ne $i+1 -or $e.boundary -cne $n -or $e.frame -ne $admission[0].frame -or $e.gameTicks -ne $admission[0].gameTicks -or $e.liveSlotUnchanged -ne $true){throw 'Input callback order, clock or live slot differs.'}
        foreach($key in @('slotObject','mechanicObject','abilityObject','handlerObject')){if($e.$key -ne $p.$key){throw 'Mixed live UI identity.'}}
        if($e.casterId -cne $id.casterId -or $e.targetId -cne $id.targetId -or $e.abilityGuid -cne $id.abilityGuid){throw 'Mixed actor or ability identity.'}
        if(@($e.state.selectedIds).Count -ne 1 -or $e.state.selectedIds[0] -cne $id.casterId -or $e.state.generation -ne $id.generationAtInit -or $e.state.relationshipState -cne 'Unmounted'){throw 'Input selection or generation changed.'}
        $instance=if($n -cmatch '^(group|slot)-'){$p.slotObject}elseif($n -cmatch '^mechanic-'){$p.mechanicObject}else{$p.handlerObject}
        if($e.instanceObject -ne $instance){throw 'Wrong callback object.'}
        $groupDepth=if($p.groupSlot -and $n -cnotmatch '^click-'){1}else{0}
        $slotDepth=if($n -cmatch '^(slot|mechanic|ability)-'){1}else{0}
        $mechanicDepth=if($n -cmatch '^(mechanic|ability)-'){1}else{0}
        if($e.groupDepth -ne $groupDepth -or $e.slotDepth -ne $slotDepth -or $e.mechanicDepth -ne $mechanicDepth){throw 'Native callback nesting differs.'}
        $selected=if($n -cin @('ability-after','mechanic-after','slot-after','group-after','click-before')){$p.abilityObject}else{0}
        $argument=if($n -cmatch '^ability-'){$p.abilityObject}else{0}
        if($e.selectedAbilityObject -ne $selected -or $e.argumentAbilityObject -ne $argument){throw 'Selected ability did not originate in the live native mechanic callback.'}
        if($n -ceq 'click-after'){
            if($e.clicked -ne $true -or $e.commandObject -ne $id.commandObject -or $e.commandAbilityObject -ne $p.abilityObject -or $e.commandTargetId -cne $id.targetId){throw 'Hotbar click admitted another command or target.'}
        } elseif($e.commandObject -ne 0 -or $e.commandAbilityObject -ne 0 -or $null -ne $e.clicked){throw 'Command existed before the native target click.'}
        if($n -cne 'click-after'){
            foreach($a in @('rider','mount','ledger')){if(($e.state.$a|ConvertTo-Json -Depth 40 -Compress) -cne ($pre.$a|ConvertTo-Json -Depth 40 -Compress)){throw 'Input activation changed actor resources or ledger before target click.'}}
        }
    }
}

# Exact group restoration replay; call beside the immutable command and input-origin proofs.
function Assert-KmcHotbarUi($Ui,$InputProof,$CommandProof) {
    if($Ui.contract -cne 'native-action-bar-group-input-and-exact-restoration' -or $Ui.restored -ne $true -or
        $Ui.casterId -cne $CommandProof.identity.casterId -or $Ui.managerObject -eq 0 -or $Ui.managerObject -ne $InputProof.registration.managerObject -or $Ui.abilityGroupObject -eq 0) {throw 'Native hotbar UI lease identity/restoration is incomplete.'}
    $before=@($Ui.before);$opened=@($Ui.opened);$after=@($Ui.after)
    if($before.Count -lt 1 -or $opened.Count -ne $before.Count -or $after.Count -ne $before.Count){throw 'Native group inventory changed.'}
    if(@($before|Group-Object groupObject|Where-Object Count -NE 1).Count -ne 0){throw 'Native group identities are not unique.'}
    for($i=0;$i -lt $before.Count;$i++){
        foreach($item in @($before[$i],$opened[$i],$after[$i])){
            if($item.index -ne $i -or $item.groupObject -eq 0 -or $item.groupObject -ne $before[$i].groupObject -or $item.type -cne $before[$i].type -or $item.toggle -isnot [bool]){throw 'Native group identity, order or toggle type changed.'}
        }
        if($after[$i].toggle -ne $before[$i].toggle){throw 'Native group toggles were not restored exactly.'}
    }
    $ability=@($opened|Where-Object groupObject -EQ $Ui.abilityGroupObject)
    if($ability.Count -ne 1 -or $ability[0].type -cne 'ActivatableAbility' -or $ability[0].toggle -ne $true){throw 'The actual native ability group did not open.'}
    if($InputProof.registration.route -ceq 'group' -and $InputProof.registration.ownerObject -ne $Ui.abilityGroupObject){throw 'Hotbar input came from another group.'}
}