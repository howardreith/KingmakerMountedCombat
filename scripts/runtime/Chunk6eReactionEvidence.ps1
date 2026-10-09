# Read-only substantive acceptance for the bounded Chunk 6E native immediate/Swift reaction
# feasibility rows. The exact Swift-typed instrument is the equipped Lesser Quicken rod on a memorized
# level-1 slot; the engine decides admission out of turn and the reader requires the fact to be
# recorded consistently: an admitted Swift command is charged exactly once through native
# UpdateCooldowns, a refused one commits nothing. Nothing here grants, writes or consumes Swift debt.
Set-StrictMode -Version Latest
function Get-KmcChunk6eReactionCases {
 @('C6E-swift-on-own-turn','C6E-swift-out-of-turn','C6E-attack-on-mount-observed','C6E-reaction-window')
}
function Get-KmcChunk6eReactionPlan([string]$Case) {
 switch -CaseSensitive($Case) {
  'C6E-swift-on-own-turn' { @('cast-swift') }
  'C6E-swift-out-of-turn' { @('foreign-window-swift') }
  'C6E-attack-on-mount-observed' { @('hostile-attack-mount') }
  'C6E-reaction-window' { @('hostile-attack-swift') }
  default { throw ('6E unknown case '+$Case) }
 }
}
$script:KmcChunk6eSwiftInstruments=@{'5590652e1c2225c4ca30c4a699ab3649'='rider';'9f10909f0be1f5141bf1c102041f93d9'='hostile';'0fd00984a2c0e0a429cf1a911b4ec5ca'='ground'}
# Returns 'admitted' or 'refused' after requiring the record to be internally consistent.
function Assert-KmcStagedSwiftCast($Swift,$After,$StepEvents,$StepCosts,[string]$Rider,[string]$Mount,[string]$Hostile,[bool]$Tb,[bool]$RequireAdmission,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 Need ($null-ne$Swift-and$null-ne$Swift.before-and$null-ne$After) 'swift record or states missing'
 $before=$Swift.before;$events=@($StepEvents);$costs=@($StepCosts)
 $instrument=[string]$Swift.instrument
 Need ($script:KmcChunk6eSwiftInstruments.ContainsKey($instrument)-and$Swift.ability.blueprint-ceq$instrument-and$Swift.ability.caster-ceq$Rider-and$Swift.ability.available-eq$true-and$Swift.ability.sourceItem-eq0) 'Swift instrument is not an available memorized slot of the rider'
 Need ($Swift.ability.runtimeActionType-ceq'Swift') 'quickened instrument did not resolve to a native Swift-typed command'
 Need ($Swift.rod.exactSlot-eq$true-and$Swift.rod.activatableOn-eq$true-and$Swift.rod.charges-ge1) 'exact rod activation is not live'
 $expectedTarget=$script:KmcChunk6eSwiftInstruments[$instrument]
 if($expectedTarget-ceq'rider'){Need ($Swift.target-ceq$Rider) 'self instrument targeted another unit'}
 elseif($expectedTarget-ceq'hostile'){Need ($Swift.target-ceq$Hostile) 'hostile instrument targeted another unit'}
 else {Need ($null-eq$Swift.target-and(Get-KmcStagedNumber (Get-KmcStagedProp $Swift 'groundDistanceToRider') 'groundDistanceToRider')-ge8.0-and(Get-KmcStagedNumber (Get-KmcStagedProp $Swift 'groundDistanceToMount') 'groundDistanceToMount')-ge8.0) 'ground instrument targeted a unit or its area reached the pair'}
 Need ($Swift.inputCount-eq1-and$Swift.clicked-is[bool]-and$Swift.admittedShellCount-in@(0,1)) 'Swift input not issued exactly once'
 $riderCosts=@($costs|Where-Object {$_.state.actor-ceq$Rider-and$_.boundary-cin@('cost-before','cost-after')})
 Need (@($costs|Where-Object {$_.state.actor-ceq$Mount-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after')}).Count-eq0) 'mount charged for the rider Swift input'
 $cast=@($events|Where-Object {$_.kind-ceq'cast-after'-and$_.actor-ceq$Rider-and$_.ability.blueprint-ceq$instrument})
 $spend=@($events|Where-Object {$_.kind-ceq'spell-spend-after'-and$_.actor-ceq$Rider-and$_.ability.blueprint-ceq$instrument})
 if($Swift.admittedShellCount-eq1) {
  Need ($null-ne$Swift.shell-and$Swift.shell.executor-ceq$Rider-and$Swift.costShell-ne0) 'admitted Swift shell lacks its identity'
  $pairs=Get-KmcStagedCostPairs $costs $Rider $Swift.costShell
  Need ($pairs.before.Count-eq1-and$pairs.after.Count-eq1-and$pairs.after[0].actionType-ceq'Swift') 'admitted Swift command not charged exactly once as Swift'
  Need ($riderCosts.Count-eq(@($costs|Where-Object {$_.state.actor-ceq$Rider-and$_.boundary-cin@('cost-before','cost-after')-and$_.command-eq$Swift.costShell}).Count)) 'rider cost outside the exact Swift shell'
  Need ($cast.Count-eq1-and$cast[0].process-ne0-and$cast[0].spellFailed-eq$false-and$spend.Count-eq1) 'admitted Swift cast/spend not exactly once'
  if($Tb){Need (Test-KmcStagedClose ((Get-KmcStagedNumber $After.rider.swift 'swift')-(Get-KmcStagedNumber $before.rider.swift 'swift')) 6.0 'swift') 'native additive Swift debt differs'}
  else {
   $acted=@($events|Where-Object {$_.kind-ceq'action-after'-and$_.identity-eq$Swift.shell.identity})
   Need ($acted.Count-eq1-and$acted[0].shell.ignoreCooldown-eq$false) 'native Swift action timing absent'
   Need (Test-KmcStagedClose $After.rider.swift (6.0-(Get-KmcStagedNumber $acted[0].shell.timeSinceStart 'timeSinceStart')) 'swift') 'native real-time Swift cost differs'
  }
  Need ($After.slotAvailable-eq$false) 'admitted quickened slot was not spent'
  return 'admitted'
 }
 Need ($RequireAdmission-eq$false) 'the rider''s own-turn Swift cast was refused'
 Need ($null-eq$Swift.shell-and$riderCosts.Count-eq0-and$cast.Count-eq0-and$spend.Count-eq0) 'refused Swift input still committed, cast or spent'
 if($Tb){Need (Test-KmcStagedClose $After.rider.swift $before.rider.swift 'swift') 'refused Swift input changed Swift debt'}
 else {Need ((Get-KmcStagedNumber $After.rider.swift 'swift')-le(Get-KmcStagedNumber $before.rider.swift 'swift')+.001) 'refused Swift input increased Swift debt'}
 Need ($After.slotAvailable-eq$true) 'refused Swift input spent the memorized slot'
 return 'refused'
}
function Assert-KmcStagedHostileAttack($Step,$RuleEvents,[string]$Case,[string]$Rider,[string]$Mount,[string]$Why) {
 function Need([bool]$ok,[string]$what){if(-not$ok){throw ($Why+': '+$what)}}
 $hostile=[string]$Step.hostileActor
 Need (-not[string]::IsNullOrEmpty($hostile)-and$hostile-cne$Rider-and$hostile-cne$Mount-and$Step.hostileAttackInputCount-eq1) 'hostile attack input not exactly once by the exact hostile'
 Need ($Step.hostileAttack.type-ceq'UnitAttack'-and$Step.hostileAttack.executor-ceq$hostile-and$Step.hostileAttackTerminal.finished-eq$true-and$Step.hostileAttackTerminal.executor-ceq$hostile) 'hostile stock attack on the mount did not run to its terminal'
 $events=@($RuleEvents.events|Where-Object {$_.caseId-ceq$Case})
 $attacks=@($events|Where-Object {$_.kind-ceq'attack-before'-and$_.actor-ceq$hostile-and$_.target-ceq$Mount})
 $rolls=@($events|Where-Object {$_.kind-ceq'attack-roll'-and$_.actor-ceq$hostile-and$_.target-ceq$Mount})
 $resolved=@($events|Where-Object {$_.kind-ceq'weapon-resolved'-and$_.actor-ceq$hostile-and$_.target-ceq$Mount})
 Need ($attacks.Count-ge1-and$rolls.Count-ge1-and$resolved.Count-ge1) 'native pre-consequence attack seam (RuleAttackWithWeapon, roll, resolve) not observed on the mount'
 # The seam order is the native one: the attack rule opens before its roll and its resolution.
 Need ((Get-KmcStagedNumber $attacks[0].frame 'frame')-le(Get-KmcStagedNumber $rolls[0].frame 'frame')-and(Get-KmcStagedNumber $rolls[0].frame 'frame')-le(Get-KmcStagedNumber $resolved[0].frame 'frame')) 'native attack seam order differs'
 Need (@($RuleEvents.attacks|Where-Object {$_.actor-ceq$hostile-and$_.target-ceq$Mount-and$_.resolved-eq$true}).Count-ge1-and$RuleEvents.dropped-eq0) 'hostile attack on the mount not resolved'
 # No product reaction: nothing negates the roll and no rider check appears at the seam.
 Need (@($events|Where-Object {$_.kind-ceq'saving-throw'-and$_.actor-ceq$Rider}).Count-eq0) 'a rider check appeared at the attack seam'
 Need (@($rolls|Where-Object {$_.autoMiss-eq$true}).Count-eq0) 'an attack roll was negated'
}
function Assert-KmcChunk6eReactionRow($Row,[string]$Rider,[string]$Mount,[bool]$Tb,$Items) {
 function Need([bool]$ok,[string]$why){if(-not$ok){throw ('6E '+$Row.name+': '+$why)}}
 $e=$Row.evidence
 Need ($Row.status-ceq'PASS'-and$e.case-ceq$Row.name) 'row structure/status differs'
 $plan=@(Get-KmcChunk6eReactionPlan $Row.name);$steps=@($e.steps)
 Need ((@($e.plan)-join',')-ceq($plan-join',')-and$steps.Count-eq1-and$steps[0].kind-ceq$plan[0]) 'registered step plan differs'
 Need ($e.before.rider.actor-ceq$Rider-and$e.before.mount.actor-ceq$Mount-and$e.before.relationship-ceq'Mounted') 'row did not start on the exact mounted pair'
 Assert-KmcStagedSettledState $e.after $Rider $Mount ('6E '+$Row.name+' row end')
 Need ($e.after.generation-eq$e.before.generation) 'relationship generation changed'
 Need ((Get-KmcChunk6cPairReplayCount @($e.costEvents) $Rider $Mount)-eq0) 'pair preparation replayed inside the row'
 Need (@(@($e.events)|Where-Object actor -CEQ $Mount).Count-eq0) 'mount cast/spend observed'
 Need ($e.rulesComplete-eq$true) 'native attack rules left unresolved'
 # Swift debt moves only through a real Swift-typed command: every rider cost-after in the row is a
 # shell the row itself admitted, and its actionType is the native command type.
 $step=$steps[0];$why='6E '+$Row.name+' '+$step.kind
 $admittedShells=@()
 foreach($key in @('costShell')){ if($null-ne(Get-KmcStagedProp $step $key)){$admittedShells+=@($step.$key)} }
 foreach($sub in @('swift','primaryInput')){ $r=Get-KmcStagedProp $step $sub; if($null-ne$r-and$null-ne(Get-KmcStagedProp $r 'costShell')-and$r.costShell-ne0){$admittedShells+=@($r.costShell)} }
 foreach($cost in @($e.costEvents|Where-Object {$_.state.actor-ceq$Rider-and$_.boundary-ceq'cost-after'})){Need ($admittedShells-contains$cost.command) 'rider debt written outside a shell this row admitted'}
 switch -CaseSensitive([string]$step.kind) {
  'cast-swift' {
   Need ($step.swift.turnActor-ceq$(if($Tb){$Rider}else{$null})-or-not$Tb) 'own-turn Swift cast did not run on the rider''s native turn'
   $null=Assert-KmcStagedSwiftCast $step.swift $step.after @($step.events) @($step.costEvents) $Rider $Mount $e.before.turnActor $Tb $true $why
   Need ($step.terminal.finished-eq$true-and$step.terminal.executor-ceq$Rider) 'Swift shell did not finish on the rider'
  }
  'foreign-window-swift' {
   if($Tb) {
    Need (-not[string]::IsNullOrEmpty([string]$step.foreignTurnActor)-and$step.foreignTurnActor-cne$Rider-and$step.foreignTurnActor-cne$Mount-and$step.swift.turnActor-ceq$step.foreignTurnActor) 'Swift input was not issued on a foreign native turn'
    Need ($step.swift.riderCanAct-is[bool]-and$step.swift.ignoreClick-is[bool]) 'native out-of-turn admission facts missing'
    $null=Assert-KmcStagedSwiftCast $step.swift $step.after @($step.events) @($step.costEvents) $Rider $Mount $e.hostileActor $Tb $false $why
   } else {
    $primary=$step.primaryInput
    Need ($null-ne$primary-and$primary.ability.blueprint-ceq'5590652e1c2225c4ca30c4a699ab3649'-and$primary.ability.sourceItem-ne0-and$primary.admittedShellCount-eq1-and$primary.costShell-ne0) 'RT window lacks the admitted Standard scroll cast'
    Need ($step.primaryShellAtInput.started-eq$true-and$step.primaryShellAtInput.finished-eq$false) 'Swift input was not issued while the Standard cast was running'
    $pairs=Get-KmcStagedCostPairs @($step.costEvents) $Rider $primary.costShell
    Need ($pairs.before.Count-eq1-and$pairs.after.Count-eq1) 'Standard cast not charged exactly once'
    # The window's own Standard scroll cast (item-sourced shell) is separated from the Swift input by
    # exact identities before the Swift consistency rules run.
    $swiftEvents=@(@($step.events)|Where-Object {$_.kind-notlike'item-spend-*'-and-not(($_.kind-ceq'cast-before'-or$_.kind-ceq'cast-after')-and(Get-KmcStagedProp (Get-KmcStagedProp $_ 'ability') 'sourceItem')-ne0)-and-not($_.kind-clike'action-*'-and(Get-KmcStagedProp $_ 'identity')-eq$primary.shell.identity)})
    $swiftCosts=@(@($step.costEvents)|Where-Object {$_.command-ne$primary.costShell})
    $null=Assert-KmcStagedSwiftCast $step.swift $step.after $swiftEvents $swiftCosts $Rider $Mount $e.hostileActor $Tb $false $why
    Need ($step.terminal.finished-eq$true) 'RT window commands did not finish'
   }
  }
  'hostile-attack-mount' {
   Assert-KmcStagedHostileAttack $step $e.ruleEvents $Row.name $Rider $Mount $why
   Need (@(@($step.costEvents)|Where-Object {$_.state.actor-ceq$Rider-and$_.boundary-cin@('cost-before','cost-after')}).Count-eq0-and@(@($step.events)|Where-Object {$_.kind-clike'cast-*'-or$_.kind-clike'*spend*'}).Count-eq0) 'rider acted during the observed hostile attack'
   if($Tb){Need (Test-KmcStagedClose $step.after.rider.swift $step.before.rider.swift 'swift') 'rider Swift debt changed at the attack seam without any Swift command'}
   else {Need ((Get-KmcStagedNumber $step.after.rider.swift 'swift')-le(Get-KmcStagedNumber $step.before.rider.swift 'swift')+.001) 'rider Swift debt rose at the attack seam without any Swift command'}
  }
  'hostile-attack-swift' {
   Assert-KmcStagedHostileAttack $step $e.ruleEvents $Row.name $Rider $Mount $why
   Need ($step.hostileAttackAtInput.started-eq$true-and$step.hostileAttackAtInput.finished-eq$false) 'Swift input was not issued while the hostile attack was live'
   $null=Assert-KmcStagedSwiftCast $step.swift $step.after @($step.events) @($step.costEvents) $Rider $Mount $step.hostileActor $Tb $false $why
  }
  default { throw ('6E '+$Row.name+': unknown step '+$step.kind) }
 }
}
function Assert-KmcChunk6eReactionEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status)
 if($Request.scenario-cnotin@('chunk6e-reaction-rt','chunk6e-reaction-tb')-or$Artifact.schemaVersion-ne46){throw '6E scenario/schema mismatch'}
 if($Artifact.status-ceq'FAIL') {
  if($Status-ceq'PASS'-or@($Artifact.errors).Count-eq0-or$Artifact.subscenarioFailCount-lt1){throw '6E incomplete observation cannot become PASS'}
  return
 }
 if($Artifact.status-cne'PASS'-or@($Artifact.errors).Count-ne0-or@($Artifact.rows|Where-Object status -CNE 'PASS').Count-ne0-or$Artifact.subscenarioFailCount-ne0-or$Artifact.subscenarioPassCount-ne@($Artifact.rows).Count){throw '6E failed/incomplete native envelope'}
 $o=$Artifact.observations.chunk6eReaction
 if($null-eq$o-or$o.contract-cne'native-mounted-reaction-feasibility-v1'-or$o.rider-cne$Artifact.observations.riderId-or$o.mount-cne$Artifact.observations.horseId-or$o.mountBlueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'){throw '6E exact original-pair observation missing'}
 $tb=$Request.scenario.EndsWith('-tb')
 if($o.mode-cne$(if($tb){'TB'}else{'RT'})-or$o.mounted-ne$true){throw '6E mode/surface differs'}
 $expected=@(Get-KmcChunk6eReactionCases)
 if(($o.cases|ConvertTo-Json -Compress)-cne($expected|ConvertTo-Json -Compress)){throw '6E registered row order differs'}
 foreach($name in $expected){if((@($o.plans.$name)-join',')-cne(@(Get-KmcChunk6eReactionPlan $name)-join',')){throw ('6E registered plan differs: '+$name)}}
 foreach($name in $expected){$rows=@($Artifact.rows|Where-Object name -CEQ $name);if($rows.Count-ne1){throw ('6E required row missing or duplicated: '+$name)};Assert-KmcChunk6eReactionRow $rows[0] $o.rider $o.mount $tb @($o.items)}
 if($null-eq$o.ruleTrace-or$o.ruleTrace.dropped-ne0){throw '6E attack rule trace missing or lossy'}
 Assert-KmcCastingFixtureClosure $o $Artifact '6E'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw '6E failed native observation cannot be promoted'}
}
