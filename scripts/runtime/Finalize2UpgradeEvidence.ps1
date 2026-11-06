# F2 on the UPG-01 chain (persistence-p05-load with finalize2Controls): the external reader. The observer only records
# native facts; PASS is decided here, strictly, from those facts, the archives on disk and the producer's own evidence.
function Get-KmcFinalize2UpgradeIdentities {
 [ordered]@{ publicControl='26151090ad4744ea96dbb7e0537a4a7f'; mountShell='f053faad986631688defa003cd7bda0e'; dismountShell='3af2b81f4d72bbb30501fa730fcdf36e'
  mountedChargeShell='d79eaec224a7a832e738eb81baef9d49'; legacyQuickSha='83e92e1ae1a38dda731a32aff6b4ff6ad424e5e5f6777f758db6832a8eb7122b'; keptLeaf='Manual_1_KMC_P01.zks'; removedLeaf='Manual_2_KMC_F2_REMOVED.zks'; removedName='KMC_F2_REMOVED'; mainBar=14 }
}
function Test-KmcFinalize2UpgradeRequest($Request) {
 $Request.scenario -ceq 'persistence-p05-load' -and $null -ne $Request.PSObject.Properties['finalize2Controls'] -and $Request.finalize2Controls -eq $true
}

# The producer run whose two Manuals the consumer staged: found by the exact archive bytes it recorded, never by name.
function Find-KmcFinalize2UpgradeProducer($Request,[string]$EvidenceParent) {
 $found=@()
 foreach($dir in @(Get-ChildItem -LiteralPath $EvidenceParent -Directory -Filter '*finalize-f2*' -ErrorAction SilentlyContinue)){
  $requestPath=Join-Path $dir.FullName 'runtime-request.json'; $rowsPath=Join-Path $dir.FullName 'persistence-observations.jsonl'
  if(-not(Test-Path -LiteralPath $requestPath -PathType Leaf) -or -not(Test-Path -LiteralPath $rowsPath -PathType Leaf)){ continue }
  $r=Get-Content -LiteralPath $requestPath -Raw | ConvertFrom-Json
  if(-not(Test-KmcFinalize2UpgradeRequest $r) -or [string]$r.persistenceCase -cne 'quick'){ continue }
  $resultPath=Join-Path $dir.FullName 'runtime-result.json'
  if(-not(Test-Path -LiteralPath $resultPath -PathType Leaf) -or [string](Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json).status -cne 'PASS'){ continue }
  $rows=@(Get-Content -LiteralPath $rowsPath | ForEach-Object { $_ | ConvertFrom-Json })
  $writes=@($rows | Where-Object kind -CEQ 'native-write-complete')
  if($writes.Count -eq 2 -and [string]$writes[0].detail.sha256 -ceq [string]$Request.persistenceLoad.sha256 -and [string]$writes[1].detail.sha256 -ceq [string]$Request.persistenceAlternate.sha256){
   $found+=[pscustomobject]@{ runId=$dir.Name; request=$r; rows=$rows; result=(Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json) }
  }
 }
 if($found.Count -ne 1){ throw ('F2 upgrade: the consumer inputs do not belong to exactly one recorded F2 producer ('+$found.Count+' found).') }
 $found[0]
}

# The one owner-selected producer/consumer bridge (F2 finishing mission, 2026-11-05): the consumer may run a corrected
# diagnostic build against exactly run 20261105-finalize-f2-r05's two Manuals. Every field is exact; the consumer's
# source must be exactly the pinned src tree, and its src may differ from the producer's only in the two diagnostics
# files named here. The consumer's own DLL/MVID/package are tied to that source by its frozen suite snapshot. Not a
# general compatibility rule: any other producer, archive, source or changed file uses the same-candidate rule.
function Get-KmcFinalize2ProducerBridge {
 [pscustomobject][ordered]@{
  producerRunId='20261105-finalize-f2-r05'; producerCommit='ff1f2e5a1200b3f2ecfe3be04304a08768e9e280'
  producerDllSha256='9e4180bacd6258a2d0a450d13faeb810b5ced2df82265eb5f92460fed2cbe81e'; producerDllMvid='59977cd3-163e-4641-b8d3-9d5a23a035fe'
  producerSuiteId='finalize-f2f-ff1f2e5a-20261105'; producerSuiteSha256='beafa8dd9bfd185982617e40ce5a90a11dc43370b2f58ddf45694921f5ba4964'
  producerPackageSha256='2b2c9699ffeec28cf2120e16e39d7f279d1fc379e51aef77d29e5ad0e61c68b2'
  riderId='b6628a77-4962-47a4-a17c-88d9836fc9d5'; mountId='d79a4f6c-b74e-4868-95bd-533899131acb'
  gameId='d41185be-edc1-47c0-b9d5-e1d7a9c8e65f'; gameName='cvb'; area='9d1278a2f599b2a4daab53abdfe88d2e'
  kept=[pscustomobject]@{fileName='Manual_1_KMC_P01.zks';internalName='KMC_P01';sha256='7b0ac4bcc3ce646063a1c0585a0ebc9d2eb3fe80332c5172bd1a336fad3dc703';length=870781}
  removed=[pscustomobject]@{fileName='Manual_2_KMC_F2_REMOVED.zks';internalName='KMC_F2_REMOVED';sha256='b323e108d66b290fbc580e5cff21e14a6eed3c70779a0ddec1c0e5d07b0a46dc';length=870363}
  consumerSourceTree='1a9965350dff0b5cda11260ca68943739b9e820c'
  changedSourceFiles=@('src/KingmakerMountedCombat/Diagnostics/PersistenceIsolationBootstrap.cs','src/KingmakerMountedCombat/Diagnostics/RuntimePersistenceFinalize2Scenario.cs')
 }
}

# Throws unless the consumer and its producer are exactly the declared bridge. Git and the suite snapshot are read, never
# a claimed string: the consumer commit's src tree and its src diff from the producer, and the suite that freezes its DLL.
function Assert-KmcFinalize2ProducerBridge($Request,$Source,$Bridge,[string]$RepoRoot,[string]$SuiteRoot) {
 function BFail([string]$Why){ throw ('F2 upgrade bridge: '+$Why) }
 if($Source.runId -cne $Bridge.producerRunId){ BFail 'the producer is not the declared run' }
 $pr=$Source.request
 if($pr.commit -cne $Bridge.producerCommit -or $pr.dllSha256 -cne $Bridge.producerDllSha256 -or $pr.dllMvid -cne $Bridge.producerDllMvid -or
    $pr.qualificationSuite.suiteId -cne $Bridge.producerSuiteId -or $pr.qualificationSuite.snapshotSha256 -cne $Bridge.producerSuiteSha256){ BFail 'the producer identity differs from the declaration' }
 $res=$Source.result
 foreach($f in 'modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed'){ if($null -eq $res.PSObject.Properties[$f] -or $res.$f -ne $true){ BFail ('the producer receipt lacks '+$f) } }
 if([string]$res.status -cne 'PASS'){ BFail 'the producer did not pass' }
 $writes=@($Source.rows | Where-Object kind -CEQ 'native-write-complete')
 if($writes.Count -ne 2 -or [string]$writes[0].detail.sha256 -cne $Bridge.kept.sha256 -or [string]$writes[1].detail.sha256 -cne $Bridge.removed.sha256 -or
    [long]$writes[0].detail.length -ne $Bridge.kept.length -or [long]$writes[1].detail.length -ne $Bridge.removed.length){ BFail 'the producer''s two native outputs differ from the declaration' }
 foreach($pair in @(@($Request.persistenceLoad,$Bridge.kept),@($Request.persistenceAlternate,$Bridge.removed))){
  $d=$pair[0]; $e=$pair[1]
  if($null -eq $d -or [string]$d.fileName -cne $e.fileName -or [string]$d.internalName -cne $e.internalName -or [string]$d.sha256 -cne $e.sha256 -or [long]$d.length -ne $e.length -or
     [string]$d.gameId -cne $Bridge.gameId -or [string]$d.gameName -cne $Bridge.gameName -or [string]$d.area -cne $Bridge.area){ BFail ('the consumer input '+$e.fileName+' is not the declared archive') }
 }
 $tree=(& git -C $RepoRoot rev-parse ([string]$Request.commit+':src') 2>$null); if($LASTEXITCODE -ne 0 -or [string]$tree -cne $Bridge.consumerSourceTree){ BFail 'the consumer source is not the declared src tree' }
 $changed=@(& git -C $RepoRoot diff --name-only $Bridge.producerCommit ([string]$Request.commit) -- src 2>$null | Sort-Object); if($LASTEXITCODE -ne 0){ BFail 'the producer/consumer source diff could not be read' }
 if(($changed -join '|') -cne (@($Bridge.changedSourceFiles | Sort-Object) -join '|')){ BFail ('the consumer source differs from the producer outside the declared files: '+($changed -join ', ')) }
 $suitePath=Join-Path $SuiteRoot ([string]$Request.qualificationSuite.suiteId+'.json')
 if(-not(Test-Path -LiteralPath $suitePath -PathType Leaf) -or (Get-KmcSha256 $suitePath) -cne [string]$Request.qualificationSuite.snapshotSha256){ BFail 'the consumer suite snapshot is missing or changed' }
 $suite=Get-Content -LiteralPath $suitePath -Raw | ConvertFrom-Json
 if([string]$suite.repository.commit -cne [string]$Request.commit -or [string]$suite.package.dllSha256 -cne [string]$Request.dllSha256 -or [string]$suite.package.dllMvid -cne [string]$Request.dllMvid){ BFail 'the consumer suite does not freeze this consumer''s commit, DLL and MVID' }
}

function Assert-KmcFinalize2UpgradeEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Rows,[Parameter(Mandatory=$true)]$GameResult,[string]$SaveRoot,[string]$EvidenceParent,[string]$LegacyInputSha256,$Bridge,[string]$RepoRoot,[string]$SuiteRoot)
 function Fail([string]$Why){ throw ('F2 upgrade: '+$Why) }
 function P($O,[string]$N){ if($null -ne $O -and $O -isnot [string] -and $O -isnot [ValueType] -and $null -ne $O.PSObject.Properties[$N]){ $O.$N } else { $null } }
 function Int($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long]){ Fail ($What+' is missing or not an integer') }; [long]$V }
 function Yes($V,[string]$What){ if($V -isnot [bool] -or $V -ne $true){ Fail $What } }
 function Text($V){ if($V -is [string]){ $V } else { $null } }
 function One([string]$Kind){ $m=@($Rows | Where-Object kind -CEQ $Kind); if($m.Count -ne 1){ Fail ('expected one '+$Kind+' row, found '+$m.Count) }; $m[0] }
 function RowKey($Row){ (Text (P $Row 'ActorId'))+'|'+[string](P $Row 'Index')+'|'+[string](P $Row 'Kind') }
 function RowsAre($List,[string[]]$Expected,[string]$What){
  $actual=@(@($List) | ForEach-Object { RowKey $_ } | Sort-Object); $want=@($Expected | Sort-Object)
  if(($actual -join ';') -cne ($want -join ';')){ Fail ($What+': rows are ['+($actual -join ';')+'], expected ['+($want -join ';')+']') }
 }
 function Watch($W,[string]$What,[int]$Minimum){
  if($null -eq $W){ Fail ($What+': no watch') }
  if((Int (P $W 'frames') ($What+' frames')) -lt $Minimum){ Fail ($What+': watched fewer than '+$Minimum+' frames') }
  foreach($n in 'movedFrames','notLiveFrames','factReplacedFrames','reinsertedFrames'){ if((Int (P $W $n) ($What+' '+$n)) -ne 0){ Fail ($What+': '+$n+' is not zero') } }
 }
 function Shown($View,[int]$Index,[string]$Key,[string]$What){
  if((P $View 'selectedIsRider') -ne $true){ Fail ($What+': the visible bar is not the rider''s') }
  $bar=@(P $View 'mainBar'); if($Index -lt 0 -or $Index -ge $bar.Count){ Fail ($What+': no visible slot '+$Index) }
  $e=$bar[$Index]
  if((P $e 'active') -ne $true -or (Text (P $e 'key')) -cne $Key -or (P $e 'settingsIndex') -ne $Index){ Fail ($What+': visible slot '+$Index+' does not draw '+$(if($Key){$Key}else{'an empty slot'})) }
 }
 function Click($C,[string]$What,[bool]$Mounting,[string]$Route){
  if($null -eq $C){ Fail ($What+': no click') }
  if((Text (P $C 'route')) -cne $Route -or (P $C 'mounting') -ne $Mounting){ Fail ($What+': the route differs') }
  Yes (P $C 'slotHeldLiveFact') ($What+': the clicked entry was not bound to the live public fact')
  # The click ran while the native slot admitted the action; a wait before it is explained only by a native Move cooldown.
  Yes (P $C 'possible') ($What+': the click ran while the native slot reported the action not possible')
  if((Int (P $C 'readyFrames') ($What+' ready frames')) -gt 0 -and [double](0+(P (P $C 'waitStart') 'riderMoveCooldown')) -le 0){ Fail ($What+': the click waited without a native Move cooldown to explain it') }
  Yes (P $C 'slotDataRestored') ($What+': the entry did not get its public AbilityData back')
  if((Text (P $C 'slotAbilityBefore')) -cne $ids.publicControl -or (Text (P $C 'slotAbilityAfter')) -cne $ids.publicControl){ Fail ($What+': the entry does not hold the public identity') }
  $shell=if($Mounting){$ids.mountShell}else{$ids.dismountShell}
  if($Mounting){
   if((Text (P $C 'handlerAfterSlot')) -cne $ids.mountShell){ Fail ($What+': the entry did not select the Mount shell') }
   Yes (P $C 'targetClicked') ($What+': the native target click did not run')
   if((Text (P $C 'target')) -cne $mountId){ Fail ($What+': the shell does not target the mount') }
  } elseif($null -ne (P $C 'unexpectedSelection')){ Fail ($What+': Dismount selected an ability instead of running') }
  if((Text (P $C 'commandBlueprint')) -cne $shell -or (Text (P $C 'caster')) -cne $riderId){ Fail ($What+': the entry did not create one exact '+$(if($Mounting){'Mount'}else{'Dismount'})+' shell for the rider') }
  # A Dismount shell is counted when it is delivered, after the click (r03 recorded 0 at the click); a Mount shell at its target click.
  $casts=Int (P $C 'castDelta') ($What+' cast delta')
  if(($Mounting -and $casts -ne 1) -or (-not $Mounting -and $casts -notin @(0,1)) -or (Int (P $C 'refusalDelta') ($What+' refusal delta')) -ne 0){ Fail ($What+': not one admitted native cast without a refusal') }
 }
 function Archive($W,[int]$Ordinal,[string]$Name,[string]$Leaf,[string[]]$ExpectRows,[int]$Index){
  $d=P $W 'detail'; $path=Join-Path $SaveRoot $Leaf
  if((Int (P $d 'ordinal') 'write ordinal') -ne $Ordinal -or (Text (P $d 'name')) -cne $Name -or (Text (P $d 'path')) -cne $path -or (Text (P $d 'nativeType')) -cne 'Manual' -or
     (P $d 'nativeCallback') -ne $true -or (Text (P $d 'operation')) -cne 'None' -or (Text (P $d 'sourceSha256')) -cne [string]$Request.persistenceLoad.sha256 -or
     (Int (P $d 'controlIndex') 'write control index') -ne $Index){ Fail ('write '+$Ordinal+' is not the exact native Manual '+$Leaf) }
  if(-not(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-KmcSha256 $path) -cne (Text (P $d 'sha256')) -or (Get-Item -LiteralPath $path).Length -ne (Int (P $d 'length') 'write length') -or
     (Text (P $d 'sha256')) -ceq [string]$Request.persistenceLoad.sha256){ Fail ('write '+$Ordinal+' bytes differ from its observation or alias the input') }
  $s=P $d 'snapshot'; $pairs=@(P $s 'Pairs')
  if((P $s 'SchemaVersion') -ne 3 -or (Text (P $s 'AreaId')) -cne [string]$Request.persistenceLoad.area -or (Text (P $s 'CampaignId')) -cne [string]$Request.persistenceLoad.gameId -or
     $pairs.Count -ne 1 -or (Text (P (P $pairs[0] 'Rider') 'Id')) -cne $riderId -or (Text (P (P $pairs[0] 'Mount') 'Id')) -cne $mountId){ Fail ('write '+$Ordinal+' does not carry the restored mounted pair in schema 3') }
  RowsAre (P $s 'Slots') $ExpectRows ('write '+$Ordinal+' archive')
  if((Int (P (P $d 'scan') 'publicHits') 'public hits') -ne 0 -or (Int (P (P $d 'scan') 'members') 'scanned members') -lt 1){ Fail ('write '+$Ordinal+': a native archive member names the public control, or nothing was scanned') }
  $d
 }
 function Shots($Complete,[string[]]$Names){
  $shots=@(P (P $Complete 'detail') 'shots')
  foreach($name in $Names){
   $m=@($shots | Where-Object { (Text (P $_ 'name')) -ceq $name })
   if($m.Count -ne 1 -or $null -ne (P $m[0] 'error')){ Fail ('screenshot '+$name+' is missing or failed') }
   $file=Text (P $m[0] 'file')
   if($file -cne ('movement-visuals/finalize-f2-p05-'+$name+'.png')){ Fail ('screenshot '+$name+' has an unexpected path') }
   $full=Join-Path $Request.evidenceRoot ($file.Replace('/','\'))
   if(-not(Test-Path -LiteralPath $full -PathType Leaf) -or (Get-KmcSha256 $full) -cne (Text (P $m[0] 'sha256'))){ Fail ('screenshot '+$name+' bytes differ from the record') }
  }
 }
 function Strings($S,[string]$What){
  if($null -eq $S -or (P $S 'tableFound') -ne $true -or (P $S 'packPresent') -ne $true){ Fail ($What+': no localization observation') }
  foreach($k in @(P $S 'keys')){ if((P $k 'present') -ne $true -or (Text (P $k 'actual')) -cne (Text (P $k 'expected'))){ Fail ($What+': key '+(Text (P $k 'key'))+' does not resolve') } }
  $c=@(P $S 'controls'); if($c.Count -ne 4){ Fail ($What+': control names missing') }
  if((Text (P $c[0] 'guid')) -cne $ids.publicControl -or (Text (P $c[0] 'name')) -cne 'Mount / Dismount' -or (P $c[0] 'hidden') -ne $false){ Fail ($What+': the public control is not the visible Mount / Dismount') }
  for($i=1;$i -lt 4;$i++){ if((P $c[$i] 'hidden') -ne $true){ Fail ($What+': a delivery shell is not hidden') } }
  if((Text (P $c[3] 'name')) -cne 'Charge'){ Fail ($What+': the mounted delivery is not named Charge') }
 }
 function NoExposure($Actor,[string]$What){ if(@(P (P $Actor 'exposure') 'hiddenExposed').Count -ne 0){ Fail ($What+': a hidden shell sits in a slot') } }
 function MainBar($Actor){ @(@(P $Actor 'bar') | Select-Object -First $ids.mainBar) }

 $ids=Get-KmcFinalize2UpgradeIdentities
 $producer=[string]$Request.persistenceCase -ceq 'quick'
 if(-not(Test-KmcFinalize2UpgradeRequest $Request) -or ($null -eq $Request.PSObject.Properties['upgradeChain']) -or $Request.upgradeChain -ne $true -or [string]$Request.persistenceCase -cnotin @('quick','manual')){ Fail 'request is not the F2 upgrade chain' }
 if(@($Rows | Where-Object { [string]$_.kind -cin @('assertion-failed','scenario-failed') }).Count -ne 0){ Fail 'the observer recorded a failure' }
 $initial=One 'initial'; $riderId=Text (P (P $initial 'rider') 'Id'); $mountId=Text (P (P $initial 'mount') 'Id')
 if([string]::IsNullOrEmpty($riderId) -or [string]::IsNullOrEmpty($mountId)){ Fail 'no initial pair' }
 foreach($row in $Rows){
  if($row.runId -cne $Request.runId -or $row.scenario -cne $Request.scenario -or $row.source -cne $Request.commit -or $row.dll -cne $Request.dllSha256 -or
     $row.processId -ne $GameResult.processId -or $row.checkpoint -cne $Request.persistenceCase -or (P $row.controls 'DuplicateFactCount') -ne 0){ Fail 'a row breaks the run identity or duplicates a control fact' }
  # A click row is written the frame the click ran; its shell may already have begun the transition.
  # Only the cycle rows may leave the mounted pair; every continuation, setup, save and load row is Mounted.
  $states=if([string]$row.kind -cmatch '-click$'){@('Mounted','Unmounted','Validating','Mounting','Dismounting')}elseif([string]$row.kind -ceq 'f2-cycle-dismounted'){@('Unmounted')}else{@('Mounted')}
  if([string]$row.relationship -cnotin $states -or ($null -ne $row.rider -and [string]$row.rider.Id -cne $riderId) -or ($null -ne $row.mount -and [string]$row.mount.Id -cne $mountId)){ Fail 'a row describes another pair' }
  if([string]$row.relationship -ceq 'Mounted' -and ($null -eq $row.rider -or $null -eq $row.mount)){ Fail 'a mounted row lacks its pair' }
 }
 foreach($kind in 'movement-dispatched','movement-completed','attack-dispatched','usable-continuation-complete'){ [void](One $kind) }
 $attack=One 'attack-delivered'; if((Int (P $attack.detail 'rules') 'attack rules') -lt 1 -or (Int (P $attack.detail 'rolls') 'attack rolls') -lt 1){ Fail 'the continuation attack did not happen' }
 $cold=One 'f2-cold-observed'; $c=$cold.detail; $complete=One 'f2-complete'
 if((Text (P $c 'contract')) -cne 'finalize-f2-upgrade-public-control-v1' -or (Text (P $c 'role')) -cne $(if($producer){'producer'}else{'consumer'})){ Fail 'cold observation contract/role differs' }
 if((P $c 'retiredHelpersSelected') -ne $false){ Fail 'the run selected the retired helpers instead of ordinary play' }
 $cid=P $c 'identities'
 if((Text (P $cid 'publicControl')) -cne $ids.publicControl -or (Text (P $cid 'mountShell')) -cne $ids.mountShell -or (Text (P $cid 'dismountShell')) -cne $ids.dismountShell){ Fail 'control identities differ' }
 NoExposure (P $c 'actor') 'cold load'
 $coldActor=P $c 'actor'
 if((Int (P (P $coldActor 'counters') 'initialPlacement') 'cold placement count') -ne 0){ Fail 'the cold load placed a control' }
 $disclosed=@(P $complete.detail 'disclosedSetup')
 $root=if([string]::IsNullOrEmpty($SaveRoot)){ Fail 'no save root' }else{ $SaveRoot }

 if($producer){
  # The authentic old row, mapped: one old Mount/Dismount row becomes the public control in place, retired rows clear.
  if((Int (P $c 'sourceSchema') 'source schema') -ge 3){ Fail 'the producer input is not an older-schema archive' }
  $pin=if([string]::IsNullOrEmpty($LegacyInputSha256)){$ids.legacyQuickSha}else{$LegacyInputSha256}
  if([string]$Request.persistenceLoad.sha256 -cne $pin -or [string]$Request.persistenceLoad.fileName -cne 'Quick_1.zks'){ Fail 'the producer input is not the authentic preview.211 Quick' }
  $old=@(@(P $c 'savedRows') | Where-Object { (Text (P $_ 'ActorId')) -ceq $riderId -and (P $_ 'Kind') -in @(1,2) })
  if($old.Count -ne 1){ Fail 'the input does not carry exactly one old Mount/Dismount row' }
  $legacy=[int](P $old[0] 'Index')
  if((Int (P $c 'legacyIndex') 'legacy index') -ne $legacy){ Fail 'the recorded legacy index differs from the archive row' }
  RowsAre (P $c 'liveRows') @(($riderId+'|'+$legacy+'|6'),($riderId+'|0|7')) 'cold live rows'
  if(@(P $coldActor 'indices').Count -ne 1 -or [int]@(P $coldActor 'indices')[0] -ne $legacy -or (Text @(P $coldActor 'bar')[$legacy]) -cne $ids.publicControl){ Fail 'the old row is not the public control in its own slot' }
  if((Int (P (P $coldActor 'counters') 'legacySlotRepair') 'legacy repairs') -ne 1){ Fail 'not exactly one old-row migration' }
  foreach($retired in @(P $c 'retiredRows')){ if((Text (P $retired 'now')) -cne ''){ Fail 'a retired primary row was restored in ordinary play' } }

  $removedInPlace=One 'f2-setup-removed-in-place'; Shown (P $removedInPlace.detail 'visibleBefore') 0 (Text @(P (P $removedInPlace.detail 'actor') 'bar')[0]) 'bar before setup'
  $freed=One 'f2-setup-freed-slot'; Watch (P $freed.detail 'watch') 'in-session removal' 30
  $freedIndex=[int](Int (P $freed.detail 'freedIndex') 'freed index')
  if($freedIndex -ne ($ids.mainBar-1)){ Fail 'the freed slot is not the main bar''s last' }
  $armed=One 'f2-placement-armed'; Yes (P $armed.detail 'autofill') 'the native autofill setting was not on before the placement event'; Shown (P $armed.detail 'visibleBefore') $freedIndex '' 'freed slot before placement'
  if(@(P (P $armed.detail 'actor') 'indices').Count -ne 0){ Fail 'a control was on the bar before the placement event' }
  # The placement row must be the next observation: no selection, drawer, refresh or mount between.
  $order=@($Rows | ForEach-Object { [string]$_.kind })
  $ai=[array]::IndexOf($order,'f2-placement-armed'); if($order[$ai+1] -cne 'f2-placement-visible'){ Fail 'another observation came between the placement event and the visible bar' }
  $visible=One 'f2-placement-visible'; $v=$visible.detail
  Yes (P $v 'shown') 'the first placement was not drawn without another refresh'
  $placements=@(P (P $v 'actor') 'placements'); if($placements.Count -ne 1){ Fail 'not exactly one placement' }
  $placed=[int](Int (P $placements[0] 'index') 'placement index'); $before=@(P $placements[0] 'mainBarBefore'); $after=MainBar (P $v 'actor')
  if($before.Count -ne $ids.mainBar -or $placed -ne [array]::IndexOf([string[]]$before,'') -or $placed -ne $freedIndex){ Fail 'the placement is not the first empty main-bar slot' }
  for($i=0;$i -lt $ids.mainBar;$i++){ $want=if($i -eq $placed){$ids.publicControl}else{[string]$before[$i]}; if((Text $after[$i]) -cne $want){ Fail ('placement changed main-bar slot '+$i) } }
  Shown (P $v 'visible') $placed $ids.publicControl 'first placement'
  if((Int (P $v 'framesObserved') 'frames to draw') -gt 30){ Fail 'the bar took longer than the bounded wait' }
  if((Int (P (P (P $v 'actor') 'counters') 'initialPlacement') 'placement count') -ne 1){ Fail 'placement count is not one' }

  Click (One 'f2-cycle-dismount-click').detail 'producer dismount' $false 'bar-slot'
  if((Text (P (One 'f2-cycle-dismounted').detail 'relationship')) -cne 'Unmounted'){ Fail 'the producer cycle did not dismount' }
  Click (One 'f2-cycle-mount-click').detail 'producer mount' $true 'bar-slot'
  $cycled=(One 'f2-cycle-mounted').detail; Watch (P $cycled 'watch') 'producer cycle' 10
  if((Text (P (P $cycled 'actor') 'relationship')) -cne 'Mounted'){ Fail 'the producer cycle did not remount' }

  $swap=(One 'f2-setup-swapped').detail; $chosen=[int](Int (P $swap 'to') 'swap target')
  if((Int (P $swap 'from') 'swap source') -ne $placed -or $chosen -eq $placed -or $chosen -lt 0 -or $chosen -ge $ids.mainBar){ Fail 'the swap is not from the placed slot to another main-bar slot' }
  $sb=@(P $swap 'barBefore'); $sa=@(P $swap 'barAfter')
  for($i=0;$i -lt $ids.mainBar;$i++){ $want=if($i -eq $chosen){$ids.publicControl}elseif($i -eq $placed){Text (P $swap 'movedKey')}else{[string]$sb[$i]}; if((Text $sa[$i]) -cne $want){ Fail ('the swap changed main-bar slot '+$i) } }
  if((Text $sb[$chosen]) -cne (Text (P $swap 'movedKey')) -or (Text $sb[$chosen]) -ceq ''){ Fail 'the swapped slot did not hold an ability' }
  $kept=(One 'f2-kept-before-save').detail; Watch (P $kept 'watch') 'kept before save' 30
  $keptRows=@(($riderId+'|'+$chosen+'|6'),($riderId+'|0|7')); RowsAre (P $kept 'rows') $keptRows 'kept rows before save'
  $writes=@($Rows | Where-Object kind -CEQ 'native-write-complete'); if($writes.Count -ne 2 -or @($Rows | Where-Object kind -CEQ 'f2-write-requested').Count -ne 2){ Fail 'not exactly two native Manual writes' }
  $w1=Archive $writes[0] 1 'KMC_P01' $ids.keptLeaf $keptRows $chosen
  if(((MainBar ([pscustomobject]@{bar=@(P $w1 'barBeforeSave')})) -join '|') -cne ($sa -join '|')){ Fail 'the kept archive''s bar differs from the swapped bar' }
  $removedRow=(One 'f2-removed-before-save').detail; Watch (P $removedRow 'watch') 'removed before save' 30
  RowsAre (P $removedRow 'rows') @($riderId+'|0|7') 'removed rows before save'
  $w2=Archive $writes[1] 2 $ids.removedName $ids.removedLeaf @($riderId+'|0|7') -1
  $removedBar=MainBar ([pscustomobject]@{bar=@(P $w2 'barBeforeSave')})
  if((Text $removedBar[$chosen]) -cne ''){ Fail 'the removed archive''s bar still holds the control slot' }
  $zks=@(Get-ChildItem -LiteralPath $root -File -Filter '*.zks'); if($zks.Count -ne 3){ Fail 'the producer root does not hold exactly the input and the two outputs' }
  if((Get-KmcSha256 (Join-Path $root $Request.persistenceLoad.fileName)) -cne [string]$Request.persistenceLoad.sha256){ Fail 'the producer changed its input archive' }
  Shots $complete @('placed','removed')
  $actions=@($disclosed | ForEach-Object { Text (P $_ 'action') })
  if($actions.Count -ne 5 -or $actions[2] -notmatch 'BeginLoadedWorld'){ Fail 'the disclosed setup is not exactly the five recorded edits' }
 } else {
  # The consumer: a new process, the kept archive cold, then the removed archive in the same process.
  $source=Find-KmcFinalize2UpgradeProducer $Request $EvidenceParent
  $pw=@($source.rows | Where-Object kind -CEQ 'native-write-complete')
  # Same candidate by default; otherwise only the one declared bridge (exact producer, archives, consumer source and suite).
  if($source.request.commit -cne $Request.commit -or $source.request.dllSha256 -cne $Request.dllSha256){
   $b=if($null -ne $Bridge){$Bridge}else{Get-KmcFinalize2ProducerBridge}
   $repo=if([string]::IsNullOrEmpty($RepoRoot)){[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))}else{$RepoRoot}
   $suites=if([string]::IsNullOrEmpty($SuiteRoot)){Join-Path (Get-KmcLabRoot) 'runtime-state\qualification-suite-snapshots'}else{$SuiteRoot}
   try { Assert-KmcFinalize2ProducerBridge $Request $source $b $repo $suites } catch { Fail ('the producer ran another candidate and the declared bridge does not admit it: '+$_.Exception.Message) }
   if($riderId -cne $b.riderId -or $mountId -cne $b.mountId){ Fail 'the bridged consumer loaded other actors' }
  }
  if(@($source.rows | Where-Object processId -EQ $GameResult.processId).Count -ne 0){ Fail 'the consumer is the producer''s process' }
  $chosen=[int](Int (P $pw[0].detail 'controlIndex') 'producer kept index')
  $keptRows=@(($riderId+'|'+$chosen+'|6'),($riderId+'|0|7'))
  if((Int (P $c 'sourceSchema') 'source schema') -ne 3){ Fail 'the consumer input is not the current schema' }
  RowsAre (P $c 'savedRows') $keptRows 'consumer saved rows'; RowsAre (P $c 'liveRows') $keptRows 'consumer live rows'
  if(@(P $coldActor 'indices').Count -ne 1 -or [int]@(P $coldActor 'indices')[0] -ne $chosen -or (P $coldActor 'placementRecorded') -ne $true){ Fail 'the control is not restored at its saved slot with its record' }
  $cc=P $coldActor 'counters'
  if((Int (P $cc 'legacySlotRepair') 'consumer repairs') -ne 0 -or (Int (P $cc 'initialPlacementObservations') 'consumer placement observations') -ne 0){ Fail 'the cold load migrated or attempted a placement' }
  if(((MainBar $coldActor) -join '|') -cne ((MainBar ([pscustomobject]@{bar=@(P $pw[0].detail 'barBeforeSave')})) -join '|')){ Fail 'the restored main bar differs from the saved one' }
  $cv=(One 'f2-cold-visible').detail; Shown (P $cv 'visible') $chosen $ids.publicControl 'restored control'; Strings (P $cv 'strings') 'cold strings'
  Click (One 'f2-cycle-dismount-click').detail 'consumer dismount' $false 'bar-slot'
  if((Text (P (One 'f2-cycle-dismounted').detail 'relationship')) -cne 'Unmounted'){ Fail 'the consumer cycle did not dismount' }
  Click (One 'f2-cycle-mount-click').detail 'consumer mount' $true 'bar-slot'
  $cycled=(One 'f2-cycle-mounted').detail; Watch (P $cycled 'watch') 'consumer cycle' 10; Strings (P $cycled 'strings') 'cycle strings'
  [void](One 'f2-load-requested')
  $rl=(One 'f2-removed-loaded').detail; $ra=P $rl 'actor'
  RowsAre (P $rl 'savedRows') @($riderId+'|0|7') 'removed saved rows'; RowsAre (P $rl 'liveRows') @($riderId+'|0|7') 'removed live rows'
  if(@(P $ra 'indices').Count -ne 0 -or (P $ra 'placementRecorded') -ne $true -or (P $ra 'factActive') -ne $true -or (Text (P $ra 'relationship')) -cne 'Mounted'){ Fail 'the removed control came back, or its record or pair was not restored' }
  $rc=P $ra 'counters'
  if((Int (P $rc 'initialPlacementObservations') 'removed placement observations') -ne 0 -or (Int (P $rc 'legacySlotRepair') 'removed repairs') -ne 0){ Fail 'the removed load attempted a placement or migration' }
  NoExposure $ra 'removed load'
  if(((MainBar $ra) -join '|') -cne ((MainBar ([pscustomobject]@{bar=@(P $pw[1].detail 'barBeforeSave')})) -join '|')){ Fail 'the removed archive''s bar differs from the saved one' }
  Watch (P (One 'f2-removed-ticks').detail 'watch') 'removed update ticks' 60
  if([string]::IsNullOrEmpty((Text (P (One 'f2-removed-ticks').detail 'reselectVia')))){ Fail 'no other party member was selected before the rider was reselected' }
  $rs=(One 'f2-removed-reselected').detail; Watch (P $rs 'watch') 'removed reselection' 70
  if((P (P $rs 'visible') 'selectedIsRider') -ne $true -or @(@(P (P $rs 'visible') 'mainBar') | Where-Object { (Text (P $_ 'key')) -ceq $ids.publicControl }).Count -ne 0){ Fail 'the reselected bar draws the removed control' }
  Click (One 'f2-removed-dismount-click').detail 'removed dismount' $false 'drawer-entry'
  Click (One 'f2-removed-mount-click').detail 'removed mount' $true 'drawer-entry'
  $rcy=(One 'f2-removed-cycled').detail; Watch (P $rcy 'watch') 'removed control cycle' 90; Strings (P $rcy 'strings') 'removed strings'
  if(@(P (P $rcy 'actor') 'indices').Count -ne 0){ Fail 'the control came back after a control cycle' }
  if(@($Rows | Where-Object kind -CEQ 'native-write-complete').Count -ne 0){ Fail 'the consumer wrote a save' }
  $zks=@(Get-ChildItem -LiteralPath $root -File -Filter '*.zks'); if($zks.Count -ne 2){ Fail 'the consumer root does not hold exactly its two inputs' }
  foreach($d in @($Request.persistenceLoad,$Request.persistenceAlternate)){ if((Get-KmcSha256 (Join-Path $root $d.fileName)) -cne [string]$d.sha256){ Fail 'the consumer changed an input archive' } }
  Shots $complete @('cold-kept','cold-removed')
  # The consumer's views must be readable: the mod-manager overlay closed at the captured frame.
  foreach($s in @(P $complete.detail 'shots')){ if((P $s 'ummOpenAtCapture') -ne $false){ Fail ('screenshot '+(Text (P $s 'name'))+' was not captured with the mod-manager overlay closed') } }
  if($disclosed.Count -ne 0){ Fail 'the consumer made a setup edit' }
 }
}
