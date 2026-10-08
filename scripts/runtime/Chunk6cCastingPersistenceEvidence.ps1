# Read-only 6C acceptance on existing P02/P04 native archive/transaction facts.
Set-StrictMode -Version Latest
function Assert-KmcCastingBaselinePersistenceEvidence($Request,$Rows,$GameResult) {
 function Need([bool]$condition,[string]$message){if(-not$condition){throw ('6C persistence: '+$message)}}
 function One([string]$kind){$found=@($Rows|Where-Object kind -CEQ $kind);Need ($found.Count-eq1) ('missing/duplicate '+$kind);$found[0]}
 function Settled($a){Need ($a.contract-ceq'settled-rider-spell-items-v1'-and$a.relationship-ceq'Mounted'-and$a.riderCommandsEmpty-eq$true-and$a.mountCommandsEmpty-eq$true-and$a.pairCommand-eq$false-and$a.pairMovement-eq$false-and$a.abilitiesPending-eq$false-and$a.projectilesPending-eq$false-and$a.processesSettled-eq$true) 'snapshot contains live native casting ownership'}
 function Rod($a){$rod=$a.rod;Need ($rod.blueprint-ceq'55a059b32df920c4abe65b8ee8b56056'-and$rod.count-eq1-and$rod.charges-eq2-and$rod.slotIndex-ge0-and$rod.exactCollection-eq$true-and$rod.nativeSourceItemExact-eq$true-and$rod.nativeActivatableOn-eq$true-and$rod.sameBlueprintInventoryCount-eq1) 'exact native saved rod changed or was provisioned twice'}
 $tb=$Request.persistenceCase-ceq'casting-items';$source=$Request.scenario.EndsWith('-save')
 Need ($Request.scenario-cin$(if($tb){@('persistence-p02-save','persistence-p02-load')}else{@('persistence-p04-save','persistence-p04-load')})) 'checkpoint/scenario differs'
 $initial=One 'initial';$rider=$initial.rider.Id;$mount=$initial.mount.Id
 foreach($row in $Rows){Need ($row.runId-ceq$Request.runId-and$row.scenario-ceq$Request.scenario-and$row.source-ceq$Request.commit-and$row.dll-ceq$Request.dllSha256-and$row.processId-eq$GameResult.processId-and$row.checkpoint-ceq$Request.persistenceCase-and$row.rider.Id-ceq$rider-and$row.mount.Id-ceq$mount-and$row.relationship-ceq$(if($source-and$row.kind-ceq'6c-fixture-items-created'){'Unmounted'}else{'Mounted'})-and$row.controls.DuplicateFactCount-eq0-and$row.native.tbSetting-eq$tb) 'native identity/control/mode differs'}
 $observed=if($source){One 'native-write-complete'}else{One '6c-cold-settled-observed'}
 $a=$observed.detail.actual;$snapshot=$observed.detail.snapshot
 Settled $a;Rod $a
 Need ($a.rider-ceq$rider-and$a.mount-ceq$mount-and$a.mountBlueprint-ceq'e7aa96d15a45238438ae4cfb476f6bb9'-and$a.tb-eq$tb-and$a.slotBlueprint-ceq'9f10909f0be1f5141bf1c102041f93d9'-and$a.slotAvailable-eq$false) 'exact rider slot or pair changed'
 Need ($snapshot.Mounted-eq$true-and$snapshot.Rider.Id-ceq$rider-and$snapshot.Mount.Id-ceq$mount-and$snapshot.Combat.TurnBased-eq$tb-and$snapshot.CampaignId-ceq$Request.fixture.working.gameId-and$snapshot.AreaId-ceq$Request.fixture.working.area) 'native snapshot/campaign differs'
 if($tb){Need ($snapshot.Combat.Current.ActorId-ceq$rider-and$snapshot.Combat.Round-ge1-and$null-ne$snapshot.Combat.Paired.Activation-and$snapshot.Rider.Standard-eq0-and$snapshot.Rider.Move-eq3-and$snapshot.Rider.Swift-eq6-and$snapshot.Mount.Standard-eq0-and$snapshot.Mount.Move-eq0) 'native TB spell/item debt or paired grant changed'}
 else {Need ($null-eq$snapshot.Combat.Current-and$snapshot.Combat.Roster.Count-eq0-and($null-eq$snapshot.Combat.Paired-or($snapshot.Combat.Paired.RiderId-ceq$rider-and$snapshot.Combat.Paired.MountId-ceq$mount-and$null-eq$snapshot.Combat.Paired.Activation-and$snapshot.Combat.Paired.BoundaryIsCurrent-eq$false-and$null-eq$snapshot.Combat.Paired.Boundary-and$null-eq$snapshot.Combat.Paired.Partner-and$snapshot.Combat.Paired.SplitReleaseRound-eq-1-and$null-eq$snapshot.Combat.Paired.PendingSplitId-and$snapshot.Combat.Paired.PendingSplitRound-eq-1-and$snapshot.Combat.Paired.RenewalNotBeforeTicks-eq0))-and$snapshot.Rider.Swift-gt0-and$snapshot.Mount.Standard-eq0-and$snapshot.Mount.Move-eq0) 'RT snapshot invented a turn or duplicate mount debt'}
 if($source){
  $use=(One '6c-settled-use').detail;$saveAttempt=(One '6c-save-request').detail;Settled $saveAttempt
  Need ($use.before.slotAvailable-eq$true-and$use.afterSpell.slotAvailable-eq$false-and$use.afterPotion.slotAvailable-eq$false-and$a.inputs-eq2-and$use.inputs.Count-eq2-and$a.snapshotCount-eq1-and$a.deferredSaves-eq0) 'settled native spending or snapshot count differs'
  $events=@($use.afterPotion.castTrace.events);$costs=@($use.costWindow)
  Need (@($costs|Where-Object {$_.state.actor-ceq$mount-and$_.boundary-cin@('cost-before','cost-after')}).Count-eq0) 'mount received rider spell/item cost'
  Need (@($costs|Where-Object {$_.state.actor-cin@($rider,$mount)-and$_.boundary-cin@('prepare-before','prepare-after','clear-before','clear-after')}).Count-eq0) 'pair preparation replayed between spell and item'
  foreach($nativeInput in $use.inputs){
   Need ($nativeInput.ability.caster-ceq$rider) 'caster is not exact rider'
   $cast=@($events|Where-Object {$_.kind-ceq'cast-after'-and$_.ability.blueprint-ceq$nativeInput.ability.blueprint-and$_.actor-ceq$rider})
   Need ($cast.Count-eq1-and$cast[0].process-ne0-and$cast[0].spellFailed-eq$false-and$cast[0].arcaneFailed-eq$false) 'native source cast missing, failed or duplicated'
   $before=@($costs|Where-Object {$_.boundary-ceq'cost-before'-and$_.command-eq$nativeInput.costShell-and$_.state.actor-ceq$rider});$after=@($costs|Where-Object {$_.boundary-ceq'cost-after'-and$_.command-eq$nativeInput.costShell-and$_.state.actor-ceq$rider})
   Need ($before.Count-eq1-and$after.Count-eq1) 'native input cost not once'
   $action=$nativeInput.ability.runtimeActionType;Need ($action-cin@('Swift','Move')) 'quickened spell or native potion action type differs'
   $field=if($action-ceq'Swift'){'swift'}else{'move'};$nominal=if($action-ceq'Swift'){6.0}else{3.0}
   if($tb){Need ([Math]::Abs($after[0].state.$field-$before[0].state.$field-$nominal)-le.001) 'native TB debt differs'}else{Need ([Math]::Abs($after[0].state.$field-($nominal-$after[0].timeSinceStart))-le.001) 'native RT cost differs'}
   foreach($other in @('standard','move','swift')|Where-Object {$_-cne$field}){Need ([Math]::Abs($after[0].state.$other-$before[0].state.$other)-le.001) 'duplicate rider action cost'}
  }
  Need ($use.inputs[0].ability.blueprint-ceq'9f10909f0be1f5141bf1c102041f93d9'-and$use.inputs[0].ability.runtimeActionType-ceq'Swift'-and$use.inputs[1].ability.sourceItemBlueprint-ceq'd52566ae8cbe8dc4dae977ef51c27d91'-and$use.inputs[1].target-ceq$rider) 'declared native spell/item inputs substituted'
  $spent=@($events|Where-Object kind -CEQ 'item-spend-after');Need ($spent.Count-eq2-and@($spent|Where-Object blueprint -CEQ '55a059b32df920c4abe65b8ee8b56056').Count-eq1-and@($spent|Where-Object blueprint -CEQ 'd52566ae8cbe8dc4dae977ef51c27d91').Count-eq1) 'rod/potion spending duplicated or missing'
  $heals=@($events|Where-Object {$_.kind-ceq'heal'-and$_.actor-ceq$rider-and$_.target-ceq$rider});Need ($heals.Count-eq1-and$heals[0].value-gt0-and$use.afterPotion.riderDamage-lt$use.beforePotion.riderDamage) 'potion effect missing or repeated'
  $path=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games/Manual_300_KMC_P01.zks');$d=$observed.detail
  Need ($d.path-ceq$path-and$d.nativeType-ceq'Manual'-and$d.nativeCallback-eq$true-and$d.operation-ceq'None'-and(Get-KmcSha256 $path)-ceq$d.sha256-and(Get-Item -LiteralPath $path).Length-eq$d.length) 'actual native archive/completion differs'
 }else{
  [void](One '6c-cold-budget-observed');[void](One '6c-cold-native-items-observed')
  Need ($a.inputs-eq0-and@($a.castTrace.events|Where-Object {$_.kind-cin@('cast-before','cast-after','spell-spend-after','item-spend-after','heal')}).Count-eq0-and$initial.controls.NativeCastRequestCount-eq0) 'cold spell/item/Mount replay observed'
  Need (@($Rows|Where-Object kind -CEQ 'native-write-complete').Count-eq0-and@($Rows|Where-Object kind -CEQ '6c-fixture-items-created').Count-eq0) 'cold process wrote or provisioned a fixture'
  $archive=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games/'+$Request.persistenceLoad.fileName)
  Need ((Get-KmcSha256 $archive)-ceq$Request.persistenceLoad.sha256) 'cold selected archive bytes changed'
 }
 $end=(One 'usable-continuation-complete').detail;Settled $end;Rod $end
 Need ($end.slotAvailable-eq$false-and$end.ordinaryInputs-eq1-and$end.ordinaryResolved-eq1) 'ordinary continuation replayed spending or duplicated attack'
 $closed=(One '6c-observers-closed').detail
 Need ($closed.nativeProcessesSettled-eq$true-and$closed.riderCommandsEmpty-eq$true-and$closed.mountCommandsEmpty-eq$true-and$closed.coldItemWasReadOnly-eq(-not$source)) 'terminal native ownership not drained'
 Need ($closed.items.Count-eq$(if($source){2}else{0})-and@($closed.items|Where-Object {$_.disposed-ne$true-or$_.slotRestored-ne$true-or$_.noOwnedItemResident-ne$true}).Count-eq0) 'disposable fixture item owner lost or not drained'
 foreach($trace in @($closed.castTrace,$closed.costTrace)){
  $faultProperty=if($trace.PSObject.Properties.Name-ccontains'faults'){'faults'}else{'observationErrors'}
  Need ($trace.$faultProperty-eq0-and$trace.dropped-eq0-and$trace.identityRegistry.faults-eq0-and$trace.identityRegistry.released-eq$true-and$trace.identityRegistry.retainedCount-eq0) 'terminal observer reference remains or evidence is incomplete'
 }
 foreach($trace in @($end.castTrace,$end.costTrace)){
  $faultProperty=if($trace.PSObject.Properties.Name-ccontains'faults'){'faults'}else{'observationErrors'}
  Need ($trace.$faultProperty-eq0-and$trace.dropped-eq0-and$trace.identityRegistry.faults-eq0) 'observer incomplete'
 }
}
function Assert-KmcCastingBaselineColdOutcome($SourceRows,$ColdRows,$Request) {
 $saved=@($SourceRows|Where-Object kind -CEQ 'native-write-complete');$loaded=@($ColdRows|Where-Object kind -CEQ '6c-cold-settled-observed')
 if($saved.Count-ne1-or$loaded.Count-ne1-or$saved[0].processId-eq$loaded[0].processId-or$saved[0].source-cne$Request.commit-or$saved[0].dll-cne$Request.dllSha256-or$saved[0].checkpoint-cne$Request.persistenceCase-or$saved[0].detail.sha256-cne$Request.persistenceLoad.sha256){throw '6C cold source archive/candidate/process binding differs'}
 $a=$saved[0].detail.actual;$b=$loaded[0].detail.actual
 if($a.rider-cne$b.rider-or$a.mount-cne$b.mount-or$a.riderDamage-ne$b.riderDamage-or$a.mountDamage-ne$b.mountDamage-or$a.slotAvailable-ne$false-or$b.slotAvailable-ne$false-or($a.rod|ConvertTo-Json -Compress)-cne($b.rod|ConvertTo-Json -Compress)){throw '6C cold native health/slot/rod ownership changed'}
 if($b.inputs-ne0-or@($b.castTrace.events|Where-Object {$_.kind-cin@('cast-before','cast-after','spell-spend-after','item-spend-after','heal')}).Count-ne0){throw '6C cold native spell/item delivery replayed'}
 $s=$saved[0].detail.snapshot;$c=$loaded[0].detail.snapshot
 if(($s|ConvertTo-Json -Depth 40 -Compress)-cne($c|ConvertTo-Json -Depth 40 -Compress)){throw '6C cold selected KMC snapshot differs from source archive'}
}