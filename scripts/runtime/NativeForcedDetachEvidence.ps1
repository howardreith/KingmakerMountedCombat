Set-StrictMode -Version Latest
# External mirror of NativeForcedDetachEvidence.AssertComplete (CM05-forced-detach). A native death or
# incapacitation ends the mounted pair through lifecycle cleanup exactly once, under a cleanup trigger,
# booking no voluntary Mount or Dismount admission, no KMC control activity and no native action cost or
# preparation callback for either actor between the labelled native damage and the settled cleanup.
# Repeated deliveries for the same detach are suppressed by the transition ledger, never booked again.
# The synthetic fixture test runs both readers over the same evidence and mutations.
function Assert-KmcForcedDetach($Evidence) {
 function Fail([string]$reason){throw ('Forced detach: '+$reason)}
 function Int($value){if($value-isnot[int]-and$value-isnot[long]){Fail 'integer missing'};[long]$value}
 function Yes($value){$value-is[bool]-and$value}
 function No($value){$value-is[bool]-and-not$value}
 function Field($object,[string]$name) {
  if($null-eq$object){return [pscustomobject]@{present=$false;value=$null}}
  if($object-is[Collections.IDictionary]){
   if(-not$object.Contains($name)){return [pscustomobject]@{present=$false;value=$null}}
   return [pscustomobject]@{present=$true;value=$object[$name]}
  }
  $property=$object.PSObject.Properties[$name]
  if($null-eq$property){return [pscustomobject]@{present=$false;value=$null}}
  [pscustomobject]@{present=$true;value=$property.Value}
 }
 function Value($object,[string]$name){(Field $object $name).value}
 function Text($object,[string]$name){$f=Field $object $name;if(-not$f.present-or$null-eq$f.value-or$f.value-isnot[string]){return $null};[string]$f.value}
 function Items($object,[string]$name){$f=Field $object $name;if(-not$f.present-or$f.value-isnot[array]){Fail ($name+' missing')};@($f.value)}
 $ledgerFields=@('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach','duplicateSuppressed','concurrentSuppressed')
 $controlFields=@('targetSelectionStart','targetSelectionEnd','nativeCastRequest','nativeRefusal','dispatchAccepted','dispatchRejected','activationCount')
 $lifeDeliveries=@(
  @('UnitIncapacitated','Incapacitated','IUnitLifeStateChanged.HandleUnitLifeStateChanged'),
  @('UnitDeath','Death','IUnitHandler.HandleUnitDeath'),
  @('UnitFinallyDead','Death','IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead'))
 function Life($life,[string]$actor,[bool]$conscious,[bool]$dead){
  $c=Value $life 'conscious';$d=Value $life 'dead';$f=Value $life 'finallyDead'
  if($null-eq$life-or(Text $life 'actor')-cne$actor-or[string]::IsNullOrEmpty((Text $life 'lifeState'))-or
     $c-isnot[bool]-or$c-ne$conscious-or$d-isnot[bool]-or$d-ne$dead-or$f-isnot[bool]){Fail ('native life state of '+$actor+' differs')}
 }
 function Boundary($b,[string]$name,[string]$state,[string]$rider,[string]$mount) {
  if($null-eq$b-or(Text $b 'name')-cne$name){Fail ('boundary '+$name+' missing')}
  if(-not(Yes (Value $b 'traceComplete'))){Fail 'boundary lacks a complete native trace'}
  if(-not(Yes (Value $b 'turnBased'))){Fail 'boundary is not native turn-based combat'}
  if((Text $b 'relationshipState')-cne$state){Fail ('boundary relationship state is not '+$state)}
  if((Text (Value $b 'rider') 'actor')-cne$rider-or(Text (Value $b 'mount') 'actor')-cne$mount){Fail 'boundary resource snapshots name other actors'}
  $ledger=Value $b 'ledger';$controls=Value $b 'controls'
  if($null-eq$ledger-or$null-eq$controls){Fail 'boundary ledger or controls missing'}
  foreach($f in $ledgerFields){[void](Int (Value $ledger $f))}
  foreach($f in $controlFields){[void](Int (Value $controls $f))}
  foreach($f in @('frame','gameTicks','allocationSequence','generation','shellCount','processBindings','lifecycleSequence')){[void](Int (Value $b $f))}
  if((Value $b 'pairCommand')-isnot[bool]){Fail 'boundary pair command flag missing'}
 }
 if((Text $Evidence 'level')-cne'NATIVE INTEGRATION'-or(Text $Evidence 'caseId')-cne'CM05-forced-detach'-or
    (Text $Evidence 'contract')-cne'forced-detach-records-once-pays-no-voluntary-move'-or(Text $Evidence 'mode')-cne'TB'-or
    (Text $Evidence 'nativeStimulus')-cne'labelled-native-damage-effect'){Fail 'case identity differs'}
 $scenario=Text $Evidence 'scenario'
 if($scenario-cnotin@('chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-rider-incapacitation-tb')){Fail 'scenario is not a native life scenario'}
 $incapacitation=$scenario-ceq'chunk4-rider-incapacitation-tb'
 $rider=Text $Evidence 'riderId';$mount=Text $Evidence 'mountId'
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount){Fail 'pair identity missing'}
 $subject=if($scenario-ceq'chunk4-mount-death-tb'){$mount}else{$rider}
 $survivor=if($subject-ceq$rider){$mount}else{$rider}
 if((Text $Evidence 'subject')-cne$subject-or(Text $Evidence 'survivor')-cne$survivor){Fail 'life subject differs from the scenario'}

 $before=Value $Evidence 'before';$after=Value $Evidence 'after'
 Boundary $before 'before-damage' 'Mounted' $rider $mount
 Boundary $after 'after-cleanup' 'Unmounted' $rider $mount
 if(-not(Yes (Value $before 'inCombat'))-or-not(Yes (Value $before 'pairCommand'))){Fail 'the stimulus did not interrupt a live paired command in native combat'}
 Life (Value $before 'subjectLife') $subject $true $false
 Life (Value $before 'survivorLife') $survivor $true $false
 if(-not(No (Value $after 'pairCommand'))-or-not(Yes (Value $after 'riderCommandsEmpty'))-or-not(Yes (Value $after 'mountCommandsEmpty'))){Fail 'cleanup left a pair command'}
 Life (Value $after 'subjectLife') $subject $false (-not$incapacitation)
 Life (Value $after 'survivorLife') $survivor $true $false
 if((Int (Value $after 'frame'))-lt(Int (Value $before 'frame'))-or(Int (Value $after 'gameTicks'))-lt(Int (Value $before 'gameTicks'))-or
    (Int (Value $after 'allocationSequence'))-lt(Int (Value $before 'allocationSequence'))-or
    (Int (Value $after 'lifecycleSequence'))-lt(Int (Value $before 'lifecycleSequence'))){Fail 'window runs backwards'}
 if((Int (Value $after 'generation'))-ne(Int (Value $before 'generation'))){Fail 'a forced detach changed the pair generation'}
 $beforeControls=Value $before 'controls';$afterControls=Value $after 'controls'
 foreach($f in $controlFields){if((Int (Value $afterControls $f))-ne(Int (Value $beforeControls $f))){Fail ('native control count changed: '+$f)}}
 foreach($f in @('shellCount','processBindings')){if((Int (Value $after $f))-ne(Int (Value $before $f))){Fail ('native relationship shell state changed: '+$f)}}

 $deliveries=@(Items $Evidence 'deliveries')
 if($deliveries.Count-ne((Int (Value $after 'lifecycleSequence'))-(Int (Value $before 'lifecycleSequence')))){Fail 'lifecycle delivery window count differs'}
 $expectedSequence=Int (Value $before 'lifecycleSequence')
 foreach($item in $deliveries){
  $expectedSequence++
  if($null-eq$item-or(Int (Value $item 'sequence'))-ne$expectedSequence){Fail 'lifecycle deliveries are not consecutive'}
  $errorsField=Field $item 'cleanupErrors'
  if((Value $item 'cleanupAttempted')-isnot[bool]-or(Value $item 'cleanupSucceeded')-isnot[bool]-or-not$errorsField.present-or$errorsField.value-isnot[array]-or
     [string]::IsNullOrEmpty((Text $item 'boundary'))-or[string]::IsNullOrEmpty((Text $item 'source'))-or
     [string]::IsNullOrEmpty((Text $item 'stateBefore'))-or[string]::IsNullOrEmpty((Text $item 'stateAfter'))){Fail 'lifecycle delivery shape differs'}
 }
 $cleanups=@($deliveries|Where-Object{(Yes (Value $_ 'cleanupAttempted'))-and(Text $_ 'stateBefore')-ceq'Mounted'})
 if($cleanups.Count-ne1){Fail 'the forced detach was not attempted exactly once from the mounted state'}
 $cleanup=$cleanups[0]
 if(-not(Yes (Value $cleanup 'cleanupSucceeded'))-or(Text $cleanup 'stateAfter')-cne'Unmounted'-or@((Value $cleanup 'cleanupErrors')).Count-ne0){Fail 'the one cleanup did not succeed into the unmounted state'}
 $trigger=Text $cleanup 'cleanupTrigger'
 $allowedCount=if($incapacitation){1}else{$lifeDeliveries.Count}
 $matched=@(0..($allowedCount-1)|Where-Object{$d=$lifeDeliveries[$_];(Text $cleanup 'boundary')-ceq$d[0]-and$trigger-ceq$d[1]-and(Text $cleanup 'source')-ceq$d[2]})
 if($matched.Count-ne1){Fail 'the one cleanup was not delivered by a native life boundary with its cleanup trigger'}
 $attempted=0
 foreach($item in $deliveries){
  if(Yes (Value $item 'cleanupAttempted')){$attempted++}
  if([object]::ReferenceEquals($item,$cleanup)){continue}
  if((Text $item 'stateBefore')-cne(Text $item 'stateAfter')){Fail 'another delivery changed the relationship state'}
  if((Yes (Value $item 'cleanupAttempted'))-and-not((Text $item 'stateBefore')-ceq'Unmounted'-and(Yes (Value $item 'cleanupSucceeded')))){Fail 'a repeated cleanup delivery was not an idempotent success from the unmounted state'}
 }
 $beforeLedger=Value $before 'ledger';$afterLedger=Value $after 'ledger'
 foreach($f in $ledgerFields){
  $delta=(Int (Value $afterLedger $f))-(Int (Value $beforeLedger $f))
  $expected=if($f-ceq'forcedDetach'){1}elseif($f-ceq'duplicateSuppressed'){$attempted-1}else{0}
  if($delta-ne$expected){Fail ('transition ledger delta differs: '+$f)}
 }

 $t=Value $after 'lastTransition'
 if($null-eq$t-or(Text $t 'kind')-cne'ForcedDetach'-or(Text $t 'riderId')-cne$rider-or(Text $t 'mountId')-cne$mount-or
    (Int (Value $t 'generationBefore'))-ne(Int (Value $before 'generation'))-or-not(Yes (Value $t 'settled'))-or-not(Yes (Value $t 'accepted'))-or
    (Text $t 'trigger')-cne$trigger){Fail 'the last transition record is not the settled forced detach of this pair'}
 $r=Value $after 'lastTransitionResult'
 $resultErrors=Field $r 'errors'
 if($null-eq$r-or-not(Yes (Value $r 'succeeded'))-or(Text $r 'state')-cne'Unmounted'-or(Text $r 'trigger')-cne$trigger-or
    -not$resultErrors.present-or$resultErrors.value-isnot[array]-or@($resultErrors.value).Count-ne0-or
    -not(No (Value $r 'movementAuthorityResidual'))-or-not(No (Value $r 'presentationResidual'))){Fail 'the last relationship result is not a residue-free cleanup under the delivered trigger'}

 $events=@(Items $Evidence 'allocationEvents')
 if($events.Count-ne((Int (Value $after 'allocationSequence'))-(Int (Value $before 'allocationSequence')))){Fail 'allocation window count differs'}
 $sequence=Int (Value $before 'allocationSequence')
 foreach($item in $events){
  $sequence++
  if($null-eq$item-or(Int (Value $item 'sequence'))-ne$sequence){Fail 'allocation events are not the consecutive native window'}
  $actor=Text (Value $item 'state') 'actor'
  if($actor-cne$rider-and$actor-cne$mount){continue}
  $boundary=[string](Text $item 'boundary')
  if($boundary.Contains('cost')){Fail 'a native cost callback fired for a pair actor during the forced detach'}
  if($boundary.StartsWith('prepare-')){Fail 'a native preparation fired for a pair actor during the forced detach'}
 }
}

# Artifact-level binding: the row carries the observation verbatim, the window opens in the same
# native tick as the C4-LIFE damage baseline, and it is an exact slice of the complete native
# allocation trace that the scenario's own C4-LIFE row publishes.
function Assert-KmcForcedDetachEnvelope($Request,$Artifact) {
 $e=$Artifact.observations.chunk6aForcedDetach
 if($null-eq$e-or[string]$e.scenario-cne[string]$Request.scenario-or[string]$Artifact.scenario-cne[string]$Request.scenario){throw 'Forced detach request binding differs'}
 Assert-KmcForcedDetach $e
 $rows=@($Artifact.rows|Where-Object name -CEQ 'CM05-forced-detach')
 if($rows.Count-ne1-or[string]$rows[0].status-cne'PASS'){throw 'Forced detach exact mandatory row absent'}
 if((ConvertTo-Json -InputObject $rows[0].evidence -Depth 100 -Compress)-cne(ConvertTo-Json -InputObject $e -Depth 100 -Compress)){throw 'Forced detach row evidence differs from the observation'}
 $life=@($Artifact.rows|Where-Object {[string]$_.name-clike'C4-LIFE-*'})
 if($life.Count-ne1-or[string]$life[0].status-cne'PASS'){throw 'Forced detach lacks its native life row'}
 $l=$life[0].evidence
 if([string]$l.subject-cne[string]$e.subject-or[string]$l.survivor-cne[string]$e.survivor){throw 'Forced detach subject differs from the native life row'}
 if($l.allocationSequenceBeforeDamage-ne$e.before.allocationSequence-or$l.beforeDamage.frame-ne$e.before.frame){throw 'Forced detach window did not open at the native life damage baseline'}
 $trace=$l.allocationTrace
 if($null-eq$trace-or$trace.dropped-ne0-or$trace.observationErrors-ne0){throw 'Forced detach complete native trace missing'}
 $slice=@($trace.events|Where-Object{$_.sequence-gt$e.before.allocationSequence-and$_.sequence-le$e.after.allocationSequence})
 if((ConvertTo-Json -InputObject @($e.allocationEvents) -Depth 100 -Compress)-cne(ConvertTo-Json -InputObject $slice -Depth 100 -Compress)){throw 'Forced detach window differs from the full native trace'}
}
