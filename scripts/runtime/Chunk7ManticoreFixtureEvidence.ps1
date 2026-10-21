# Closed disclosed unmounted visit and genuine Manual fixture. This does not
# qualify ordinary travel or mark an unexplored GlobalMap location explored.
Set-StrictMode -Version Latest
function Test-KmcChunk7ManticoreFixture($Request){
 $null-ne$Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-is[string]-and$Request.persistenceCase-ceq'owner-seed-manticore-fixture-manual'-and
 $Request.scenario-is[string]-and$Request.scenario-ceq'chunk7-owned-two-pair-campaign'-and
 $null-ne$Request.PSObject.Properties['ownedPairs']-and$Request.ownedPairs.case-is[string]-and$Request.ownedPairs.case-ceq'owner-seed-checkpoint-save'
}
function Assert-KmcChunk7ManticoreSelection($Request){
 if(-not(Test-KmcChunk7ManticoreFixture $Request)){throw 'Manticore preparation requires its exact source-only inherited checkpoint.'}
 Assert-KmcBootstrapInventoryString $Request.ownedPairs.sourceArchiveSha256 'b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003'
 foreach($name in @('persistenceAreaTarget','persistenceReplayTargets','schema1DerivationSha256','schema2DerivationSha256','loadRefusalDerivationSha256')){
  if($null-ne$Request.PSObject.Properties[$name]-and$null-ne$Request.$name){throw 'Manticore preparation cannot carry another target or derivative route.'}
 }
}
function Test-KmcChunk7TombFixture($Request){
 $null-ne$Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-is[string]-and$Request.persistenceCase-ceq'owner-seed-tomb-door-fixture-manual'-and
 $Request.scenario-is[string]-and$Request.scenario-ceq'chunk7-owned-two-pair-campaign'-and
 $null-ne$Request.PSObject.Properties['ownedPairs']-and$Request.ownedPairs.case-is[string]-and$Request.ownedPairs.case-ceq'owner-seed-checkpoint-save'
}
function Assert-KmcChunk7TombSelection($Request){
 if(-not(Test-KmcChunk7TombFixture $Request)){throw 'Tomb door preparation requires its exact source-only inherited checkpoint.'}
 Assert-KmcBootstrapInventoryString $Request.ownedPairs.sourceArchiveSha256 'b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003'
 foreach($name in @('persistenceAreaTarget','persistenceReplayTargets','schema1DerivationSha256','schema2DerivationSha256','loadRefusalDerivationSha256')){
  if($null-ne$Request.PSObject.Properties[$name]-and$null-ne$Request.$name){throw 'Tomb preparation cannot carry another target or derivative route.'}
 }
}
function Test-KmcChunk7ThornRiverFixture($Request){
 $null-ne$Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-is[string]-and$Request.persistenceCase-ceq'owner-seed-thorn-river-door-fixture-manual'-and
 $Request.scenario-is[string]-and$Request.scenario-ceq'chunk7-owned-two-pair-campaign'-and
 $null-ne$Request.PSObject.Properties['ownedPairs']-and$Request.ownedPairs.case-is[string]-and$Request.ownedPairs.case-ceq'owner-seed-checkpoint-save'
}
function Assert-KmcChunk7ThornRiverSelection($Request){
 if(-not(Test-KmcChunk7ThornRiverFixture $Request)){throw 'Thorn River preparation requires its exact source-only inherited checkpoint.'}
 Assert-KmcBootstrapInventoryString $Request.ownedPairs.sourceArchiveSha256 'b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003'
 foreach($name in @('persistenceAreaTarget','persistenceReplayTargets','schema1DerivationSha256','schema2DerivationSha256','loadRefusalDerivationSha256')){
  if($null-ne$Request.PSObject.Properties[$name]-and$null-ne$Request.$name){throw 'Thorn River preparation cannot carry another target or derivative route.'}
 }
}
function Assert-KmcChunk7ManticorePrerequisite($Request,$Value){
 Assert-KmcChunk7ManticoreSelection $Request
 Assert-KmcExactProperties $Value @('contract','sourceArea','sourceSha256','locationGuid','entryGuid','destinationArea','destinationIsCapital','seen','revealed','closed','explored','locationCountBefore','locationCountAfter','existingLocationOnly','actualEntranceExact','navigationAssisted','ordinaryTravelProof','nativeMode','transferRequests','saveRequests') 'Manticore fixture prerequisite'
 foreach($pair in @(@('contract','owner-seed-manticore-entry-v1'),@('sourceArea','b10e86516b6437d45a629931c30936fe'),@('sourceSha256',$Request.persistenceLoad.sha256),@('locationGuid','3cf6c925d3fca1943b538db776f90b20'),@('entryGuid','dfbc24234b2f33f4f888bb6f5f838c6b'),@('destinationArea','314e8c3b15b896a4f847c85f9250f02b'),@('nativeMode','None'))){Assert-KmcBootstrapInventoryString $Value.($pair[0]) $pair[1]}
 foreach($key in @('seen','revealed','existingLocationOnly','actualEntranceExact','navigationAssisted')){Assert-KmcChunk7Boolean $Value.$key $true}
 foreach($key in @('closed','explored','destinationIsCapital','ordinaryTravelProof')){Assert-KmcChunk7Boolean $Value.$key $false}
 Assert-KmcOrdinaryInteger $Value.locationCountBefore 1 4096;Assert-KmcOrdinaryInteger $Value.locationCountAfter $Value.locationCountBefore $Value.locationCountBefore
 foreach($key in @('transferRequests','saveRequests')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
}
function Assert-KmcChunk7TombPrerequisite($Request,$Value){
 Assert-KmcChunk7TombSelection $Request
 Assert-KmcExactProperties $Value @('contract','sourceArea','sourceSha256','locationGuid','entryGuid','destinationArea','destinationIsCapital','seen','revealed','closed','explored','locationCountBefore','locationCountAfter','existingLocationOnly','actualEntranceExact','navigationAssisted','ordinaryTravelProof','nativeMode','transferRequests','saveRequests') 'Tomb fixture prerequisite'
 foreach($pair in @(@('contract','owner-seed-tomb-entry-v1'),@('sourceArea','b10e86516b6437d45a629931c30936fe'),@('sourceSha256',$Request.persistenceLoad.sha256),@('locationGuid','38638e43953432e42a037956f798f15e'),@('entryGuid','c23675e173391ad4297dbe3720122bca'),@('destinationArea','81b13ce8765469045864ffb878b0944b'),@('nativeMode','None'))){Assert-KmcBootstrapInventoryString $Value.($pair[0]) $pair[1]}
 foreach($key in @('seen','revealed','existingLocationOnly','actualEntranceExact','navigationAssisted')){Assert-KmcChunk7Boolean $Value.$key $true}
 foreach($key in @('closed','explored','destinationIsCapital','ordinaryTravelProof')){Assert-KmcChunk7Boolean $Value.$key $false}
 Assert-KmcOrdinaryInteger $Value.locationCountBefore 1 4096;Assert-KmcOrdinaryInteger $Value.locationCountAfter $Value.locationCountBefore $Value.locationCountBefore
 foreach($key in @('transferRequests','saveRequests')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
}
function Assert-KmcChunk7ThornRiverPrerequisite($Request,$Value){
 Assert-KmcChunk7ThornRiverSelection $Request
 Assert-KmcExactProperties $Value @('contract','sourceArea','sourceSha256','locationGuid','entryGuid','destinationArea','destinationIsCapital','seen','revealed','closed','explored','locationCountBefore','locationCountAfter','existingLocationOnly','actualEntranceExact','navigationAssisted','ordinaryTravelProof','nativeMode','transferRequests','saveRequests') 'Thorn River fixture prerequisite'
 foreach($pair in @(@('contract','owner-seed-thorn-river-entry-v1'),@('sourceArea','b10e86516b6437d45a629931c30936fe'),@('sourceSha256',$Request.persistenceLoad.sha256),@('locationGuid','e929000884b47804f9c026c873aaa57b'),@('entryGuid','1ab738bc965352a479b68d1bd325c810'),@('destinationArea','7d72372ade2f14448a3fec6d61e06bd8'),@('nativeMode','None'))){Assert-KmcBootstrapInventoryString $Value.($pair[0]) $pair[1]}
 foreach($key in @('seen','revealed','existingLocationOnly','actualEntranceExact','navigationAssisted')){Assert-KmcChunk7Boolean $Value.$key $true}
 foreach($key in @('closed','explored','destinationIsCapital','ordinaryTravelProof')){Assert-KmcChunk7Boolean $Value.$key $false}
 Assert-KmcOrdinaryInteger $Value.locationCountBefore 1 4096;Assert-KmcOrdinaryInteger $Value.locationCountAfter $Value.locationCountBefore $Value.locationCountBefore
 foreach($key in @('transferRequests','saveRequests')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
}
function Assert-KmcChunk7ManticoreFacts($Value,$Request,$Controls,[int]$Mounted){
 Assert-KmcExactProperties $Value @('contract','rows','totalFacts','activeFacts') 'Manticore native control facts'
 Assert-KmcBootstrapInventoryString $Value.contract 'owner-seed-control-facts-v1'
 $roles=Get-KmcChunk7OwnedPairIds $Request;$guids=@('f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
 if($Value.rows-isnot[Array]-or$Value.rows.Count-ne20){throw 'Manticore facts must observe each of four roles and five exact controls.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$sum=0
 foreach($row in $Value.rows){
  Assert-KmcExactProperties $row @('actorId','kind','blueprintGuid','totalFacts','activeFacts','facts') 'Manticore actor control fact'
  if($row.actorId-isnot[string]-or$row.actorId-cnotin$roles){throw 'Manticore control fact actor is outside the admitted roles.'}
  Assert-KmcOrdinaryInteger $row.kind 1 5;if(-not$seen.Add($row.actorId+':'+$row.kind)){throw 'Manticore control fact repeats an actor/kind.'}
  Assert-KmcBootstrapInventoryString $row.blueprintGuid $guids[$row.kind-1]
  $rider=$row.actorId-cin@($roles[0],$roles[2]);$expected=if($rider-and$(if($Mounted-eq2){$row.kind-in@(2,3,4)}else{$row.kind-eq1})){1}else{0}
  foreach($key in @('totalFacts','activeFacts')){Assert-KmcOrdinaryInteger $row.$key $expected $expected}
  if($row.facts-isnot[Array]-or$row.facts.Count-ne$expected){throw 'Manticore control fact inventory differs from its mounted/unmounted phase.'}
  foreach($fact in $row.facts){
   Assert-KmcExactProperties $fact @('active','factBlueprintExact','dataPresent','dataBlueprintExact','casterExact','nativeLookupExact') 'Manticore native control fact binding'
   foreach($key in @('active','factBlueprintExact','dataPresent','dataBlueprintExact','casterExact','nativeLookupExact')){Assert-KmcChunk7Boolean $fact.$key $true}
  }
  $sum+=$expected
 }
 foreach($key in @('totalFacts','activeFacts')){Assert-KmcOrdinaryInteger $Value.$key $sum $sum}
 Assert-KmcOrdinaryInteger $Controls.ExactFactCount $sum $sum
}
function ConvertTo-KmcChunk7ManticoreSlots($Slots){
 if($Slots-isnot[Array]-or$Slots.Count-gt4){throw 'Manticore live slots must be a bounded exact JSON array.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($slot in $Slots){Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'Manticore native control slot';Assert-KmcOrdinaryGuid $slot.ActorId;Assert-KmcOrdinaryInteger $slot.Index 0 127;Assert-KmcOrdinaryInteger $slot.Kind 2 3;if(-not$seen.Add($slot.ActorId+':'+$slot.Index)){throw 'Manticore live control position repeats.'}}
 ConvertTo-KmcChunk7Canonical @($Slots|Sort-Object ActorId,Index,Kind)
}
function Assert-KmcChunk7ManticoreHotbars($Current,$Initial,$Registry,$Request){
 if($Current-isnot[Array]-or$Current.Count-ne$Request.ownedPairs.partyIds.Count-or$Initial-isnot[Array]-or$Initial.Count-ne$Current.Count){throw 'Manticore hotbars must observe the complete original party.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($row in $Current){
  Assert-KmcExactProperties $row @('actorId','owner','collection','elements','managed') 'Manticore native hotbar'
  if($row.actorId-isnot[string]-or$row.actorId-cnotin$Request.ownedPairs.partyIds-or-not$seen.Add($row.actorId)){throw 'Manticore hotbar actor repeats or escapes the full party.'}
  $old=@($Initial|Where-Object actorId -CEQ $row.actorId);if($old.Count-ne1){throw 'Manticore hotbar lost its original actor.'};$old=$old[0]
  foreach($key in @('owner','collection')){
   Assert-KmcOrdinaryInteger $row.$key ([int]::MinValue) ([int]::MaxValue)
   if($row.$key-ne0-and-not$Registry.Contains([int]$row.$key)){throw 'Manticore native hotbar identity lacks retained custody.'}
   if($row.$key-ne$old.$key){throw 'Manticore replaced a native hotbar owner or collection.'}
  }
  if($null-eq$row.elements){
   if($null-ne$old.elements-or$null-ne$row.managed-or$row.collection-ne0){throw 'Manticore changed an uninitialized native hotbar.'};continue
  }
  if($row.owner-eq0-or$row.collection-eq0-or$row.elements-isnot[Array]-or$row.elements.Count-gt140-or$row.managed-isnot[Array]-or$row.managed.Count-ne$row.elements.Count-or$old.elements-isnot[Array]-or$old.elements.Count-ne$row.elements.Count-or(ConvertTo-KmcChunk7Canonical $row.managed)-cne(ConvertTo-KmcChunk7Canonical $old.managed)){throw 'Manticore native hotbar shape or managed-source mask differs.'}
  for($i=0;$i-lt$row.elements.Count;$i++){
   Assert-KmcOrdinaryInteger $row.elements[$i] ([int]::MinValue) ([int]::MaxValue)
   if($row.elements[$i]-ne0-and-not$Registry.Contains([int]$row.elements[$i])){throw 'Manticore native hotbar element lacks retained custody.'}
   if($row.managed[$i]-isnot[bool]-or(-not$row.managed[$i]-and$row.elements[$i]-ne$old.elements[$i])){throw 'Manticore changed an unrelated native hotbar element.'}
  }
 }
}
function Assert-KmcChunk7TombDoorInventory($Inventory,$Remounted,$Roles){
 Assert-KmcExactProperties $Inventory @('boundary','candidateCount','rowLimit','radius','mounts','truncated','rows','interactionTested') 'Tomb door inventory'
 Assert-KmcBootstrapInventoryString $Inventory.boundary 'after-fixture-remount'
 Assert-KmcOrdinaryInteger $Inventory.candidateCount 0 100000
 Assert-KmcOrdinaryInteger $Inventory.rowLimit 32 32
 if(-not(Test-KmcChunk7FiniteNumber $Inventory.radius)-or[double]$Inventory.radius-ne28.0){throw 'Tomb door scan radius differs.'}
 Assert-KmcChunk7Boolean $Inventory.truncated ($Inventory.candidateCount-gt32)
 Assert-KmcChunk7Boolean $Inventory.interactionTested $false
 if($Inventory.mounts-isnot[Array]-or$Inventory.mounts.Count-ne2-or$Inventory.rows-isnot[Array]-or
    $Inventory.rows.Count-ne[Math]::Min(32,[int]$Inventory.candidateCount)){throw 'Tomb door inventory shape differs.'}
 for($i=0;$i-lt2;$i++){
  $mount=$Inventory.mounts[$i]
  Assert-KmcExactProperties $mount @('id','position') 'Tomb mounted actor position'
  Assert-KmcBootstrapInventoryString $mount.id $Roles[2*$i+1]
  if(@($Remounted.native.actors|Where-Object{$_.native.Id-ceq$mount.id}).Count-ne1){throw 'Tomb door mount is absent from the remounted native pair.'}
  foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $mount.position.$axis)){throw 'Tomb mount position is nonfinite.'}}
 }
 $prior=-1.0
 foreach($row in $Inventory.rows){
  Assert-KmcExactProperties $row @('name','position','active','open','disableCutWhenOpen','cutPresent','proximityRadius','mountDistances','eligible') 'Tomb native door'
  if($row.name-isnot[string]-or[string]::IsNullOrWhiteSpace($row.name)-or$row.name.Length-gt256-or
     $row.mountDistances-isnot[Array]-or$row.mountDistances.Count-ne2-or
     -not(Test-KmcChunk7FiniteNumber $row.proximityRadius)-or[double]$row.proximityRadius-le0){throw 'Tomb door identity or radius is malformed.'}
  foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $row.position.$axis)){throw 'Tomb door position is nonfinite.'}}
  for($i=0;$i-lt2;$i++){
   $start=$Inventory.mounts[$i].position
   $distance=[Math]::Sqrt([Math]::Pow([double]$start.x-[double]$row.position.x,2)+[Math]::Pow([double]$start.z-[double]$row.position.z,2))
   if(-not(Test-KmcChunk7FiniteNumber $row.mountDistances[$i])-or[Math]::Abs($distance-[double]$row.mountDistances[$i])-gt0.001){throw 'Tomb door distance differs from mounted actor geometry.'}
  }
  $sum=[double]$row.mountDistances[0]+[double]$row.mountDistances[1]
  if($sum+0.001-lt$prior){throw 'Tomb door inventory is not nearest-first.'};$prior=$sum
  foreach($key in @('active','open','disableCutWhenOpen','cutPresent','eligible')){if($row.$key-isnot[bool]){throw 'Tomb door state is not Boolean.'}}
  $eligible=$row.active-and$row.open-and$row.disableCutWhenOpen-and$row.cutPresent-and
   @($row.mountDistances|Where-Object{[double]$_-le[double]$row.proximityRadius+0.5-or[double]$_-gt28.0}).Count-eq0
  Assert-KmcChunk7Boolean $row.eligible $eligible
 }
}
function Assert-KmcChunk7ManticoreFixtureObservation($Request,$Observations,$Archive){
 $tomb=Test-KmcChunk7TombFixture $Request;$thorn=Test-KmcChunk7ThornRiverFixture $Request
 if($tomb){Assert-KmcChunk7TombSelection $Request}elseif($thorn){Assert-KmcChunk7ThornRiverSelection $Request}else{Assert-KmcChunk7ManticoreSelection $Request}
 $o=$Observations;$p=if($tomb){$o.tombDoorFixturePreparation}elseif($thorn){$o.thornRiverDoorFixturePreparation}else{$o.manticoreFixturePreparation};$roles=Get-KmcChunk7OwnedPairIds $Request
 $destination=if($tomb){'81b13ce8765469045864ffb878b0944b'}elseif($thorn){'7d72372ade2f14448a3fec6d61e06bd8'}else{'314e8c3b15b896a4f847c85f9250f02b'}
 $prefix=if($tomb){'tomb'}elseif($thorn){'thorn-river'}else{'manticore'}
 $properties=@('contract','checkpoint','prerequisite','transferRequests','nativeMode','navigationAssisted','ordinaryTravelProof','nativeAreaCallbackExpected','explicitSaveGameDispatches','source','originalSlots','sourceUnmounted','dispatchReturned','destinationUnmounted','loadingTicks','remounted','reboundSlots','controlRebindings','afterSave','completedArchiveSha256','identityRetention')
 if($tomb-or$thorn){$properties+=@('areaDoorInventory','unityDevelopmentPlayer')}
 Assert-KmcExactProperties $p $properties 'native area fixture preparation'
 Assert-KmcBootstrapInventoryString $p.contract $(if($tomb){'owner-seed-tomb-door-fixture-v1'}elseif($thorn){'owner-seed-thorn-river-door-fixture-v1'}else{'owner-seed-manticore-fixture-v1'});Assert-KmcBootstrapInventoryString $p.checkpoint $Request.persistenceCase;Assert-KmcBootstrapInventoryString $p.nativeMode 'None'
 if($tomb-or$thorn){if($p.unityDevelopmentPlayer-isnot[bool]){throw 'Door-fixture Unity development-player observation is not Boolean.'};Assert-KmcChunk7TombDoorInventory $p.areaDoorInventory $p.remounted $roles}
 foreach($key in @('navigationAssisted','dispatchReturned')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($key in @('ordinaryTravelProof','nativeAreaCallbackExpected')){Assert-KmcChunk7Boolean $p.$key $false}
 foreach($key in @('transferRequests','explicitSaveGameDispatches')){Assert-KmcOrdinaryInteger $p.$key 1 1}
 Assert-KmcOrdinaryInteger $p.controlRebindings 4 4;Assert-KmcOrdinaryInteger $p.loadingTicks 1 1000000
 Assert-KmcBootstrapInventoryString $Request.persistenceLoad.area 'b10e86516b6437d45a629931c30936fe';Assert-KmcBootstrapInventoryString $Request.persistenceLoad.sha256 $Request.ownedPairs.sourceArchiveSha256
 if($tomb){Assert-KmcChunk7TombPrerequisite $Request $p.prerequisite}elseif($thorn){Assert-KmcChunk7ThornRiverPrerequisite $Request $p.prerequisite}else{Assert-KmcChunk7ManticorePrerequisite $Request $p.prerequisite}
 Assert-KmcBootstrapInventoryString $p.completedArchiveSha256 $o.nativeWrite.sha256;Assert-KmcBootstrapInventoryString $Archive.AreaId $destination
 $ret=$p.identityRetention;Assert-KmcExactProperties $ret @('capacity','ids','faults','released','retainedAfterRelease') 'Manticore retained native identities'
 Assert-KmcOrdinaryInteger $ret.capacity 4096 4096;foreach($key in @('faults','retainedAfterRelease')){Assert-KmcOrdinaryInteger $ret.$key 0 0};Assert-KmcChunk7Boolean $ret.released $true
 if($ret.ids-isnot[Array]-or$ret.ids.Count-lt1-or$ret.ids.Count-gt4096){throw 'Manticore retained identities exceed the bounded observer.'}
 $registry=[Collections.Generic.HashSet[int]]::new();foreach($id in $ret.ids){Assert-KmcChunk7ReplayObject $id;if(-not$registry.Add([int]$id)){throw 'Manticore native identity label repeats.'}}
 $worlds=@($p.source,$p.sourceUnmounted,$p.destinationUnmounted,$p.remounted,$p.afterSave)
 $labels=@('source-mounted','source-unmounted','destination-unmounted','destination-remounted','native-manual-complete')
 $casts=@(0,2,2,4,4);$counts=@(2,0,0,2,2)
 for($i=0;$i-lt5;$i++){
  $w=$worlds[$i]
  Assert-KmcExactProperties $w @('label','area','game','player','state','areaState','mainState','worldScope','mode','capital','directlyControllable','field','native','activeSlots','controlFacts','authority','hotbars') 'Manticore native fixture world'
  Assert-KmcBootstrapInventoryString $w.label $labels[$i];Assert-KmcBootstrapInventoryString $w.area $(if($i-lt2){'b10e86516b6437d45a629931c30936fe'}else{$destination})
  Assert-KmcBootstrapInventoryString $w.mode 'Default';Assert-KmcChunk7Boolean $w.capital $false
  foreach($key in @('game','player','state','areaState','mainState','worldScope')){Assert-KmcChunk7ReplayObject $w.$key;if(-not$registry.Contains([int]$w.$key)){throw 'Manticore native world is unretained.'}}
  foreach($key in @('game','player','state')){if($w.$key-ne$p.source.$key){throw 'Manticore native entry replaced the retained game/player/state.'}}
  if($w.worldScope-ne$(if($i-lt2){$p.source.worldScope}else{$p.destinationUnmounted.worldScope})){throw 'Manticore world scope changed outside exact destination adoption.'}
  if($i-ge2-and$w.worldScope-eq$p.source.worldScope){throw 'Manticore reused source-area mutation authority.'}
  if($w.directlyControllable-isnot[Array]-or(ConvertTo-KmcChunk7Canonical @($w.directlyControllable|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Request.ownedPairs.partyIds|Sort-Object -CaseSensitive))){throw 'Manticore omitted a directly controllable full-party actor.'}
  $wrapper=[pscustomobject]@{ownedField=$w.field;area=$w.area;gameObject=$w.game;playerObject=$w.player;stateObject=$w.state;areaStateObject=$w.areaState;mainStateObject=$w.mainState;actors=@()}
  Assert-KmcChunk7OwnedReplayField $Request $wrapper $registry 1 0
  foreach($key in @('custody','actors','characters')){if((ConvertTo-KmcChunk7Canonical $w.field.$key)-cne(ConvertTo-KmcChunk7Canonical $p.source.field.$key)){throw 'Manticore changed full-party actor/view/pet/fact/progression custody.'}}
  $authority=if($i-eq4){$o.ownedOutputCompletion.authority}else{$o.ownedOutputAdmission.authority}
  if((ConvertTo-KmcChunk7Canonical $w.authority)-cne(ConvertTo-KmcChunk7Canonical $authority)){throw 'Manticore acquired another load/write or lost reserved source custody.'}
  foreach($spec in @(@('semanticRestores',4),@('presentationRestores',2),@('nativePostLoads',4),@('snapshots',$(if($i-eq4){1}else{0})),@('nativeCastRequests',$casts[$i]))){Assert-KmcOrdinaryInteger $w.native.($spec[0]) $spec[1] $spec[1]}
  if($w.native.owners-isnot[Array]-or$w.native.owners.Count-ne$counts[$i]-or$w.native.actors-isnot[Array]-or$w.native.actors.Count-ne4){throw 'Manticore owner/actor cardinality differs at a measured boundary.'}
  $nativeActors=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($actor in $w.native.actors){Assert-KmcChunk7PersistenceNativeActor $actor.native;Assert-KmcChunk7Boolean $actor.inCombat $false;Assert-KmcChunk7Boolean $actor.prepared $false;if($actor.native.Id-cnotin$roles-or-not$nativeActors.Add($actor.native.Id)){throw 'Manticore native actor repeats or escapes the four exact roles.'}}
  Assert-KmcChunk7ManticoreFacts $w.controlFacts $Request $w.native.controls $counts[$i]
  Assert-KmcChunk7ManticoreHotbars $w.hotbars $p.source.hotbars $registry $Request
  $expectedSlots=if($counts[$i]-eq0){,@()}elseif($i-eq0){$o.mountedStart.loadedArchive.Slots}else{$Archive.Slots}
  if((ConvertTo-KmcChunk7ManticoreSlots $w.activeSlots)-cne(ConvertTo-KmcChunk7ManticoreSlots $expectedSlots)){throw 'Manticore active native control bindings differ from the exact mounted or unmounted phase.'}
 }
 foreach($key in @('originalSlots','reboundSlots')){if((ConvertTo-KmcChunk7ManticoreSlots $p.$key)-cne(ConvertTo-KmcChunk7ManticoreSlots $Archive.Slots)){throw 'Manticore changed the original four native control positions.'}}
 Assert-KmcChunk7PersistenceSnapshot $p.source.native $o.mountedStart.loadedArchive 'source-mounted'
 Assert-KmcChunk7PersistenceSnapshot $p.afterSave.native $Archive 'native-manual-complete'
 foreach($pair in $Archive.Pairs){$owner=@($p.remounted.native.owners|Where-Object ownerId -CEQ $pair.PairId);if($owner.Count-ne1-or$owner[0].generation-ne$pair.Generation-or$owner[0].riderId-cne$pair.Rider.Id-or$owner[0].mountId-cne$pair.Mount.Id-or$owner[0].state-cne'Active'){throw 'Manticore archive changed a freshly mounted owner or generation.'}}
 $saveElapsed=([double]$Archive.GameTimeTicks-[double]$p.remounted.native.gameTicks)/10000000
 if($saveElapsed-lt0-or$saveElapsed-gt30){throw 'Manticore Manual was not written after its bounded remounted observation.'}
 foreach($actor in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){$prior=@($p.remounted.native.actors|Where-Object{$_.native.Id-ceq$actor.Id});if($prior.Count-ne1-or-not(Test-KmcChunk7DebtWithinClock $prior[0].native $actor $saveElapsed)){throw 'Manticore native Manual changed remounted actor expenditure.'}}
 $old=@($p.source.native.owners|ForEach-Object ownerId);foreach($owner in $p.remounted.native.owners){if($owner.ownerId-cin$old){throw 'Manticore reused a retired source owner identity.'}}
 $elapsed=([double]$p.destinationUnmounted.native.gameTicks-[double]$p.sourceUnmounted.native.gameTicks)/10000000
 if($elapsed-lt0-or$elapsed-gt300){throw 'Manticore unmounted native transfer escaped its measured clock window.'}
 foreach($row in $p.sourceUnmounted.native.actors){$after=@($p.destinationUnmounted.native.actors|Where-Object{$_.native.Id-ceq$row.native.Id});if($after.Count-ne1-or-not(Test-KmcChunk7DebtWithinClock $row.native $after[0].native $elapsed)){throw 'Manticore unmounted entry changed native actor expenditure.'}}
 $inputs=@($o.inputs);if($inputs.Count-ne4){throw 'Manticore fixture requires exactly two native Dismounts and two native Mounts.'}
 $specs=@(@(($prefix+'-dismount-first-pair'),$roles[0],$roles[0],'3af2b81f4d72bbb30501fa730fcdf36e',$roles[2]),@(($prefix+'-dismount-second-pair'),$roles[2],$roles[2],'3af2b81f4d72bbb30501fa730fcdf36e',$roles[0]),@(($prefix+'-mount-first-pair'),$roles[0],$roles[1],'f053faad986631688defa003cd7bda0e',$roles[2]),@(($prefix+'-mount-second-pair'),$roles[2],$roles[3],'f053faad986631688defa003cd7bda0e',$roles[0]))
 for($i=0;$i-lt4;$i++){
  $row=$inputs[$i];$spec=$specs[$i]
  foreach($pair in @(@('label',$spec[0]),@('casterId',$spec[1]),@('targetId',$spec[2]),@('abilityGuid',$spec[3]),@('selectionAfterInput',$spec[4]))){Assert-KmcBootstrapInventoryString $row.($pair[0]) $pair[1]}
  Assert-KmcOrdinaryInteger $row.nativeCastDelta 1 1;Assert-KmcOrdinaryInteger $row.nativeRefusalDelta 0 0;Assert-KmcOrdinaryInteger $row.generationAtInit 0
  foreach($key in @('clicked','terminal')){Assert-KmcChunk7Boolean $row.$key $true}
  if($row.control-isnot[string]-or[string]::IsNullOrWhiteSpace($row.control)-or$row.control.Length-gt128){throw 'Manticore input lost its native relationship-control identity.'}
  Assert-KmcOrdinaryInteger $row.before.nativeCastRequests $i $i;Assert-KmcOrdinaryInteger $row.after.nativeCastRequests ($i+1) ($i+1)
 }
}
