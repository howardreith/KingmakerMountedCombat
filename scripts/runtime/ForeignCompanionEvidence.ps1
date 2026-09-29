Set-StrictMode -Version Latest
function Assert-KmcForeignCompanion($E) {
 $id=$E.identity;$b=$E.foreignCompanionBefore;$a=$E.foreignCompanionAfter
 if($E.case-cne'foreign-companion'){throw 'Foreign companion case differs'}
 foreach($field in @('unrelatedIsPet','unrelatedSupportedMount')){if($id.$field-isnot[bool]-or$id.$field-ne$true){throw 'Supported foreign pet missing'}}
 if($null-eq$b-or$null-eq$a-or($b|ConvertTo-Json -Depth 50 -Compress)-cne($a|ConvertTo-Json -Depth 50 -Compress)){throw 'Foreign pair changed across synchronous refused input'}
 $pre=$E.foreignCompanionPreCombat
 if($pre.inCombat-isnot[bool]-or$pre.inCombat-or
    ($pre.frame-isnot[int]-and$pre.frame-isnot[long])-or($pre.gameTicks-isnot[int]-and$pre.gameTicks-isnot[long])-or
    $pre.frame-ge$E.legal.frame-or$pre.gameTicks-gt$E.legal.gameTicks){throw 'Foreign pre-combat capture absent'}
 foreach($field in @('ownerId','ownerObject','targetId','targetObject','masterId','masterObject','ownerPetId','ownerPetObject','targetBlueprint','targetProfile')){
  if($null-eq$pre.pair.$field-or($pre.pair.$field|ConvertTo-Json -Compress)-cne($b.$field|ConvertTo-Json -Compress)){throw ('Pre-combat foreign identity changed '+$field)}
 }
 $ids=@($b.ownerId,$id.riderId,$id.mountId,$id.unrelatedId)
 if(@($ids|Where-Object {$_-isnot[string]-or[string]::IsNullOrWhiteSpace($_)}).Count-ne0-or@($ids|Select-Object -Unique).Count-ne4){throw 'Distinct foreign owner missing'}
 $objects=@($b.ownerObject,$id.riderObject,$id.mountObject,$id.unrelatedObject)
 if(@($objects|Where-Object {($_-isnot[int]-and$_-isnot[long])-or$_-eq0}).Count-ne0-or@($objects|Select-Object -Unique).Count-ne4){throw 'Distinct foreign owner object missing'}
 foreach($field in @('targetObject','masterObject','ownerPetObject')){if($b.$field-isnot[int]-and$b.$field-isnot[long]){throw 'Foreign object identity malformed'}}
 if($b.targetId-cne$id.unrelatedId-or$b.targetObject-ne$id.unrelatedObject-or$b.masterId-cne$b.ownerId-or$b.masterObject-ne$b.ownerObject-or$b.ownerPetId-cne$b.targetId-or$b.ownerPetObject-ne$b.targetObject){throw 'Foreign reciprocal native ownership differs'}
 foreach($field in @('ownerLiveParty','targetLiveParty','targetIsInGame','targetDirectlyControllable','targetViewPresent','ownerCommandsEmpty','targetCommandsEmpty')){if($b.$field-isnot[bool]-or$b.$field-ne$true){throw ('Foreign native fixture missing '+$field)}}
 if($b.membershipSource-cne'native-reciprocal-party-owner'-or$b.targetInPartyList-isnot[bool]){throw 'Owner-derived native party membership evidence missing'}
 if($b.targetBlueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'-or$b.targetProfile-cne'Mammoth'){throw 'Foreign native profile differs'}
 foreach($role in @('owner','target')){
  $res=$b.($role+'Resources');if($null-eq$res-or$res.actor-cne$b.($role+'Id')){throw 'Foreign resource actor differs'}
  foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
   $v=$res.$field;if($null-eq$v-or($v-isnot[int]-and$v-isnot[long]-and$v-isnot[double]-and$v-isnot[decimal])-or[double]::IsNaN([double]$v)-or[double]::IsInfinity([double]$v)){throw 'Foreign native resource missing or nonfinite'}
  }
 }
}