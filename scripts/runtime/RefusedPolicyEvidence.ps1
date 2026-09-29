Set-StrictMode -Version Latest
function Assert-KmcRefusedPolicy($E) {
 $p=$E.policy;$input=$E.input;$id=$E.identity
 if($p.contract-cne'synchronous-paired-policy-disabled-refusal-restored'){throw 'Policy contract missing'}
 $resources=$p.resources
 if($resources.pass-isnot[bool]-or$resources.pass-ne$true){throw 'Policy resources failed'}
 Assert-KmcNativePassiveResources $resources
 if($resources.riderId-cne$id.riderId-or$resources.mountId-cne$id.mountId-or$resources.turnBased-ne$false){throw 'Policy resources belong to another pair or mode'}
 foreach($name in @('before','disabled','restored')){
  $b=$p.$name;$s=$b.settings
  foreach($f in @('movement','paired','unified','scheduler','overlay')){
   $expected=($f-ceq'movement')-or($f-ceq'paired'-and$name-cne'disabled')
   if($s.$f-isnot[bool]-or$s.$f-ne$expected){throw 'Only paired policy may differ'}
  }
  foreach($field in @('frame','gameTicks')){if(($b.$field-isnot[int]-and$b.$field-isnot[long])-or$b.$field-ne$input.before.$field){throw 'Policy clock differs'}}
  if($b.state.selectedIds-isnot[Array]-or$b.state.selectedIds.Count-ne1-or$b.state.selectedIds[0]-cne$id.riderId){throw 'Policy exact selection differs'}
  if(($b.state|ConvertTo-Json -Depth 50 -Compress)-cne($input.before.state|ConvertTo-Json -Depth 50 -Compress)){throw 'Policy state changed outside its exact refused input'}
 }
 if(($p.before.state|ConvertTo-Json -Depth 50 -Compress)-cne($E.legal.state|ConvertTo-Json -Depth 50 -Compress)){throw 'Policy legal baseline differs'}
 if(($p.disabled.allocationSequence-isnot[int]-and$p.disabled.allocationSequence-isnot[long])-or$p.disabled.allocationSequence-ne$input.before.allocationSequence){throw 'Disabled allocation boundary differs'}
 if($p.before.allocationSequence-gt$p.disabled.allocationSequence-or$input.after.allocationSequence-gt$p.restored.allocationSequence){throw 'Policy allocation order differs'}
 foreach($end in @('before','after')){
  $b=if($end-ceq'before'){$p.before}else{$p.restored};$r=$resources.$end
  foreach($field in @('frame','gameTicks','allocationSequence')){if(($b.$field-isnot[int]-and$b.$field-isnot[long])-or$b.$field-ne$r.$field){throw 'Policy resource boundary differs'}}
  foreach($actor in @('rider','mount')){
   if($r.$actor.actor-cne$id.($actor+'Id')){throw 'Policy resource actor differs'}
   foreach($field in @('standard','move','swift','reactions','reactionsPerRound','reactionCooldown','initiativeCooldown','initiativeOrder')){
    if($null-eq$r.$actor.$field-or$r.$actor.$field-ne$b.state.$actor.$field){throw 'Policy resource boundary is not bound'}
   }
   if($r.$actor.grantSequence-ne$b.state.$actor.nativePrepareCount){throw 'Policy native preparation differs'}
  }
 }
}
