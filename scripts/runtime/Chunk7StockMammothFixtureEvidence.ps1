Set-StrictMode -Version Latest
function Test-KmcChunk7StockMammothFixtureScenario([string]$Scenario){$Scenario-ceq'chunk7-stock-mammoth-fixture-preparation'}
function Get-KmcChunk7StockMammothFixtureRows {@('chunk7-stock-mammoth-fixture-preparation','C7-stock-mammoth-source','C7-stock-mammoth-native-load','C7-stock-mammoth-native-choices','C7-stock-mammoth-native-manual','C7-stock-mammoth-cleanup','C7-stock-mammoth-runtime','C7-stock-mammoth-interrupted')}
function Get-KmcChunk7StockMammothFixturePins {
 [pscustomobject][ordered]@{case='owner-seed-ranger-ten-stock-mammoth-manual';sourceSeedId='KMC_C7_NO_MODS_OWNER_SEED';sourceSeedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f';
  sourceQualificationRunId='20261017-c7b-world18-ownerseed-s4-cold';sourceQualificationSha256='c0a4bf3ed11f1bc8d80ffa6f999491562f1896d2f3b38f4518067ad87a9ff74a';
  sourceInventoryRunId='20261018-c7b-world26-ownerseed-b3s1d-save';sourceInventorySha256='01438210fbc9cdf8f8bc228b3d973ffff7220eaa32449bf43f4462fb355723a8';
  baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1;targetRangerLevel=10;nativeXpAssistance=96000}
}
function Assert-KmcChunk7StockMammothFixtureSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['stockMammothFixture']
 if($Request.scenario-isnot[string]){throw 'Stock Mammoth scenario must be one exact scalar string.'}
 if(-not(Test-KmcChunk7StockMammothFixtureScenario $Request.scenario)){if($has){throw 'Stock Mammoth fixture selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'Exact stock Mammoth fixture selection is missing.'}
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Stock Mammoth fixture run ID is not canonical.'}
 $expected=Get-KmcChunk7StockMammothFixturePins;$s=$Request.stockMammothFixture
 Assert-KmcAssistedObjectValue $s @($expected.PSObject.Properties.Name) 'closed stock Mammoth fixture selection'
 foreach($key in @('case','sourceSeedId','sourceSeedSha256','sourceQualificationRunId','sourceQualificationSha256','sourceInventoryRunId','sourceInventorySha256')){Assert-KmcBootstrapInventoryString $s.$key $expected.$key}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 foreach($key in @('expectedNativeLoadCount','expectedNativeWriteCount','targetRangerLevel','nativeXpAssistance')){Assert-KmcOrdinaryInteger $s.$key $expected.$key $expected.$key}
 if($Request.runId-ceq$s.sourceQualificationRunId-or$Request.runId-ceq$s.sourceInventoryRunId){throw 'Stock Mammoth fixture run aliases its native provenance.'}
}
function Assert-KmcStockMammothSubscenarioSet($Result){
 if(-not(Test-KmcChunk7StockMammothFixtureScenario $Result.scenario)-or$Result.status-cne'PASS'){return}
 $expected=@('C7-stock-mammoth-source','C7-stock-mammoth-native-load','C7-stock-mammoth-native-choices','C7-stock-mammoth-native-manual','C7-stock-mammoth-cleanup')
 $actual=@($Result.subscenarioResults|ForEach-Object{[string]$_.name})
 if($actual.Count-ne$expected.Count){throw 'PASS stock Mammoth fixture lacks its exact five native phases.'}
 foreach($name in $expected){if(@($actual|Where-Object{$_-ceq$name}).Count-ne1){throw ('PASS stock Mammoth fixture lacks required phase '+$name+'.')}}
 foreach($row in $Result.subscenarioResults){if($row.status-cne'PASS'){throw 'PASS stock Mammoth fixture retains a failed phase.'}}
}
function ConvertTo-KmcStockMammothSeedSelection($Selection){
 [pscustomobject][ordered]@{case='owner-seed-default-halfling-horse-manual';seedId=$Selection.sourceSeedId;seedSha256=$Selection.sourceSeedSha256;
  seedQualificationRunId=$Selection.sourceQualificationRunId;seedQualificationArtifactSha256=$Selection.sourceQualificationSha256;
  nativeInventoryRunId=$Selection.sourceInventoryRunId;nativeInventoryArtifactSha256=$Selection.sourceInventorySha256;
  baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1;targetRangerLevel=4}
}
function Assert-KmcChunk7StockMammothFixtureDescriptor($Request){
 Assert-KmcChunk7StockMammothFixtureSelection $Request
 $f09=[pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId=$Request.runId;
  smallFixture=(ConvertTo-KmcStockMammothSeedSelection $Request.stockMammothFixture);
  persistenceLoad=$Request.persistenceLoad;fixture=$Request.fixture}
 Assert-KmcChunk7SmallFixtureDescriptor $f09
}
function Assert-KmcStockMammothAuthority($Row,$Request,[long]$Loads,[long]$Writes,[long]$Generation){
 Assert-KmcAssistedObjectValue $Row @('root','generation','maximumNativeLoads','maximumNativeWrites','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites','reservations','manualNumber','outputLeaf') 'stock Mammoth exact authority'
 Assert-KmcBootstrapInventoryString $Row.root (Get-KmcAssistedSaveRoot $Request)
 Assert-KmcOrdinaryInteger $Row.generation $Generation $Generation
 foreach($key in @('maximumNativeLoads','maximumNativeWrites')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
 foreach($key in @('admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key $Loads $Loads}
 foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 if($Writes-eq0){Assert-KmcOrdinaryInteger $Row.reservations 0 0;Assert-KmcOrdinaryInteger $Row.manualNumber 0 0;if($null-ne$Row.outputLeaf){throw 'Stock Mammoth builder opened a writable output early.'}}
 else{Assert-KmcOrdinaryInteger $Row.reservations 1 1;Assert-KmcOrdinaryInteger $Row.manualNumber 1 ([int]::MaxValue-1);Assert-KmcBootstrapInventoryString $Row.outputLeaf ('Manual_'+$Row.manualNumber.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_STOCK_MAMMOTH.zks')}
}
function Assert-KmcStockMammothWorld($World,$Request,$Labels,[long]$Count,[long]$Writes,[long]$Generation){
 Assert-KmcBootstrapInventoryString $World.mode 'Default';Assert-KmcChunk7Boolean $World.inCombat $false
 foreach($key in @('owners','slots','casts')){Assert-KmcOrdinaryInteger $World.$key 0 0}
 foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $World.$key $Writes $Writes}
 foreach($key in @('game','saveManager','player','state','areaState','mainState')){Assert-KmcBootstrapInventoryObject $World.$key;if(-not$Labels.Contains([int]$World.$key)){throw 'Stock Mammoth world identity is foreign to the native PostLoad observer.'}}
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $World.arrangement.$key $Request.persistenceLoad.$key}
 Assert-KmcAssistedArray $World.arrangement.party $Count $Count 'stock Mammoth exact field roster'
 foreach($row in $World.arrangement.party){foreach($key in @('object','descriptor','view')){Assert-KmcBootstrapInventoryObject $row.$key;if(-not$Labels.Contains([int]$row.$key)){throw 'Stock Mammoth actor identity is foreign to the native PostLoad observer.'}}}
 Assert-KmcStockMammothAuthority $World.authority $Request 1 $Writes $Generation
}
function Assert-KmcStockMammothBuild($Observations,$Request,$Rider,$Pet){
 $b=$Observations.nativeBuild
 Assert-KmcBootstrapInventoryString $b.actorId $Rider.id
 Assert-KmcBootstrapInventoryString $b.petId $Pet.id
 Assert-KmcBootstrapInventoryString $b.petBlueprint 'e7aa96d15a45238438ae4cfb476f6bb9'
 foreach($pair in @(@('actorObject',$Rider.object),@('descriptorObject',$Rider.descriptor),@('petObject',$Pet.object))){Assert-KmcOrdinaryInteger $b.($pair[0]) $pair[1] $pair[1]}
 foreach($key in @('level','rangerLevel')){Assert-KmcOrdinaryInteger $b.$key 10 10}
 Assert-KmcOrdinaryInteger $b.xp 105000 105000;Assert-KmcOrdinaryInteger $b.money $Observations.initialMoney $Observations.initialMoney
 Assert-KmcChunk7Boolean $b.nativeCanLevelUp $false
 $s=$Observations.stockMammoth
 Assert-KmcBootstrapInventoryString $s.riderId $Rider.id;Assert-KmcBootstrapInventoryString $s.petId $Pet.id
 foreach($pair in @(@('riderObject',$Rider.object),@('petObject',$Pet.object))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
 foreach($pair in @(@('petBlueprint','e7aa96d15a45238438ae4cfb476f6bb9'),@('featureGuid','6adc3aab7cde56b40aa189a797254271'),@('rankGuid','1670990255e4fe948a863bafd5dbda5d'),@('upgradeGuid','6a23d16a4476af644af89d91f9f96790'),@('riderSize','Medium'),@('petSize','Large'))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $pair[1]}
 Assert-KmcAssistedGuid32 $s.featureSource;Assert-KmcOrdinaryInteger $s.rank 7 20;Assert-KmcOrdinaryInteger $s.rangerLevel 10 10
 Assert-KmcChunk7Boolean $s.upgradePresent $true;Assert-KmcBootstrapInventoryString $s.petMasterId $Rider.id
 Assert-KmcAssistedArray $Observations.choices 16 1400 'seven native Ranger levels and sourced Mammoth choices'
 foreach($choice in $Observations.choices){Assert-KmcBootstrapInventoryString $choice.runId $Request.runId}
 $commits=@($Observations.choices|Where-Object event -CEQ 'native-level-committed')
 if($commits.Count-ne7){throw 'Stock Mammoth builder lacks seven actual native level commits.'}
 for($i=0;$i-lt7;$i++){$c=$commits[$i];Assert-KmcOrdinaryInteger $c.commitCount ($i+1) ($i+1);Assert-KmcOrdinaryInteger $c.before.level ($i+3) ($i+3);Assert-KmcOrdinaryInteger $c.after.level ($i+4) ($i+4);Assert-KmcOrdinaryInteger $c.after.xp 105000 105000;foreach($step in @($c.before,$c.after)){Assert-KmcBootstrapInventoryString $step.actorId $Rider.id;Assert-KmcOrdinaryInteger $step.actorObject $Rider.object $Rider.object;Assert-KmcOrdinaryInteger $step.descriptorObject $Rider.descriptor $Rider.descriptor}}
 $rank=@($Observations.choices|Where-Object event -CEQ 'native-stock-mammoth-rank-seven')
 if($rank.Count-ne1){throw 'Stock Mammoth native rank-seven upgrade observation is missing.'}
 foreach($pair in @(@('rangerLevel',10),@('companionRank',$s.rank))){Assert-KmcOrdinaryInteger $rank[0].($pair[0]) $pair[1] $pair[1]}
 foreach($pair in @(@('riderSize','Medium'),@('mountSize','Large'),@('upgradeGuid',$s.upgradeGuid),@('featureGuid',$s.featureGuid),@('petGuid',$s.petBlueprint))){Assert-KmcBootstrapInventoryString $rank[0].($pair[0]) $pair[1]}
 $complete=@($Observations.choices|Where-Object event -CEQ 'native-builder-complete')
 if($complete.Count-ne1){throw 'Stock Mammoth native builder completion observation is missing.'}
 Assert-KmcChunk7Boolean $complete[0].ordinaryChoicesComplete $true
 Assert-KmcBootstrapInventoryString $complete[0].actor.actorId $Rider.id
 foreach($key in @('actorObject','descriptorObject','petObject','level','xp','rangerLevel','petId','petBlueprint')){if([string]$complete[0].actor.$key-cne[string]$b.$key){throw ('Stock Mammoth builder completion differs from captured native build '+$key+'.')}}
 foreach($key in @('nativeFieldAdmissionProven','nativeSaveSettlementProven','poseQualified')){Assert-KmcChunk7Boolean $complete[0].$key $false}
}
function Assert-KmcStockMammothNativeSave($Save,$Request,$World,$Write,$Labels){
 Assert-KmcAssistedObjectValue $Save @('contract','installed','installationAttempted','complete','requests','prepares','starts','stages','completions','callbacks','events','hooks','errors') 'stock Mammoth installed native save observer'
 Assert-KmcBootstrapInventoryString $Save.contract 'stock-mammoth-fixture-native-save-v1'
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $Save.$key $true}
 foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $Save.$key 1 1}
 Assert-KmcAssistedArray $Save.errors 0 0 'stock Mammoth save observer errors'
 Assert-KmcAssistedArray $Save.hooks 3 3 'stock Mammoth native save hooks'
 $expected=@(@('Kingmaker.EntitySystem.Persistence.SaveManager','SaveRoutine','06008029','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('KingmakerMountedCombat.Integration.MountedPersistenceService','RecordCompletedSave',$null,$Request.dllMvid))
 for($i=0;$i-lt3;$i++){$h=$Save.hooks[$i];Assert-KmcAssistedObjectValue $h @('type','method','token','moduleMvid') 'stock Mammoth native save hook';Assert-KmcBootstrapInventoryString $h.type $expected[$i][0];Assert-KmcBootstrapInventoryString $h.method $expected[$i][1];Assert-KmcBootstrapInventoryString $h.moduleMvid $expected[$i][3];if($i-lt2){Assert-KmcBootstrapInventoryString $h.token $expected[$i][2]}elseif($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Stock Mammoth production completion hook token malformed.'}}
 Assert-KmcAssistedArray $Save.events 6 6 'stock Mammoth genuine native save events'
 $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback')
 $requested=$null;$prepared=$null;$prior=0
 for($i=0;$i-lt6;$i++){$e=$Save.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior ([int]::MaxValue);$prior=$e.frame
  foreach($key in @('game','saveManager','player','requested','descriptor')){Assert-KmcAssistedLabel $e.$key $Labels}
  foreach($key in @('game','saveManager','player')){Assert-KmcOrdinaryInteger $e.$key $World.$key $World.$key}
  if($i-eq0){$requested=$e.requested;Assert-KmcOrdinaryInteger $e.descriptor $requested $requested}
  else{Assert-KmcOrdinaryInteger $e.requested $requested $requested;if($i-eq1){$prepared=$e.descriptor;if($prepared-eq$requested){throw 'Native prepared descriptor aliases requested descriptor.'}}else{Assert-KmcOrdinaryInteger $e.descriptor $prepared $prepared}}
  Assert-KmcBootstrapInventoryString $e.internalName $Write.internalName;Assert-KmcBootstrapInventoryString $e.type 'Manual'
  foreach($key in @('gameName','area')){Assert-KmcBootstrapInventoryString $e.$key $Request.persistenceLoad.$key}
  if($i-eq0){if($null-ne$e.gameId-or$null-ne$e.fileName-or$null-ne$e.path){throw 'Unprepared native Manual already carried prepared identity.'}}
  else{Assert-KmcBootstrapInventoryString $e.gameId $Request.persistenceLoad.gameId;Assert-KmcBootstrapInventoryString $e.fileName $Write.fileName;Assert-KmcBootstrapInventoryString $e.path $Write.path}
 }
 $done=$Save.events[4];foreach($key in @('scope','iterator','worker')){Assert-KmcAssistedLabel $done.$key $Labels}
 $distinct=[Collections.Generic.HashSet[int]]::new()
 foreach($id in @($World.game,$World.saveManager,$World.player,$requested,$prepared,$done.scope,$done.iterator,$done.worker)){if(-not$distinct.Add([int]$id)){throw 'Stock Mammoth native save observer aliases distinct retained objects.'}}
 Assert-KmcOrdinaryInteger $done.workerId 1 ([int]::MaxValue)
 Assert-KmcBootstrapInventoryString $done.workerStatus 'RanToCompletion'
 foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $done.$key $true}
 foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $done.$key $false}
}
function Assert-KmcChunk7StockMammothFixtureArtifact($Request,$Manifest,[string]$Status='PASS'){
 if(-not(Test-KmcChunk7StockMammothFixtureScenario $Request.scenario)){return}
 Assert-KmcChunk7StockMammothFixtureDescriptor $Request
 $a=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-stock-mammoth-fixture-evidence.json' 'chunk7-stock-mammoth-fixture-evidence'
 Assert-KmcBootstrapInventoryString $a.status $Status
 if($Status-ceq'PASS'){Assert-KmcChunk7StockMammothFixtureEvidence $Request $a}
 elseif($Status-cne'FAIL'){throw 'Stock Mammoth fixture artifact status is not admitted.'}
}
function Assert-KmcChunk7StockMammothFixtureEvidence($Request,$Artifact){
 Assert-KmcChunk7StockMammothFixtureDescriptor $Request
 Assert-KmcAssistedColdArtifactEnvelope $Request $Artifact 'chunk7-stock-mammoth-fixture-evidence'
 Assert-KmcBootstrapInventoryString $Artifact.status 'PASS';$o=$Artifact.observations
 Assert-KmcBootstrapInventoryString $o.contract 'owner-seed-reference-free-stock-mammoth-producer-v1'
 Assert-KmcAssistedSame $o.selection $Request.stockMammothFixture 'stock Mammoth closed selection'
 Assert-KmcAssistedSame $o.source $Request.persistenceLoad 'stock Mammoth immutable seed descriptor'
 Assert-KmcChunk7Boolean $o.fixtureSetupOnly $true
 foreach($key in @('ordinaryAcquisitionQualified','positiveRemovalQualified','dllAbsentLoadQualified')){Assert-KmcChunk7Boolean $o.$key $false}
 Assert-KmcOrdinaryInteger $o.xpAssistance.before 9000 9000
 Assert-KmcOrdinaryInteger $o.xpAssistance.after 105000 105000
 Assert-KmcOrdinaryInteger $o.xpAssistance.delta 96000 96000
 foreach($key in @('levelBefore')){Assert-KmcOrdinaryInteger $o.xpAssistance.$key 3 3}
 Assert-KmcOrdinaryInteger $o.xpAssistance.levelAfter 3 3
 Assert-KmcChunk7Boolean $o.xpAssistance.once $true
 Assert-KmcChunk7Boolean $o.xpAssistance.ordinaryAcquisitionProof $false
 Assert-KmcOrdinaryInteger $o.nativeCommitCount 7 7
 $labels=Get-KmcAssistedColdLabels $o.nativeLoad
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $Request
 $generation=$o.beforeLoadAuthority.generation
 Assert-KmcOrdinaryInteger $generation 1 ([int]::MaxValue)
 Assert-KmcStockMammothAuthority $o.beforeLoadAuthority $Request 0 0 $generation
 Assert-KmcStockMammothWorld $o.loadedWorld $Request $labels 4 0 $generation
 Assert-KmcStockMammothWorld $o.savedWorld $Request $labels 5 1 $generation
 Assert-KmcStockMammothWorld $o.settledWorld $Request $labels 5 1 $generation
 $worldRoles=@('game','saveManager','player','state','areaState','mainState')
 $uniqueWorld=[Collections.Generic.HashSet[int]]::new()
 foreach($key in $worldRoles){if(-not$uniqueWorld.Add([int]$o.loadedWorld.$key)){throw 'Stock Mammoth native world roles alias distinct objects.'};foreach($phase in @('savedWorld','settledWorld')){Assert-KmcOrdinaryInteger $o.$phase.$key $o.loadedWorld.$key $o.loadedWorld.$key}}
 Assert-KmcAssistedSame $o.savedWorld $o.settledWorld 'stock Mammoth settled native world'
 Assert-KmcAssistedSame $o.savedWorld.arrangement $o.afterSaveArrangement 'stock Mammoth native save world/arrangement'
 Assert-KmcAssistedSame $o.beforeSaveArrangement $o.savedWorld.arrangement 'stock Mammoth pre-save versus settled native world'
 $original=@('73681ef2-b281-4bc6-bc05-2de68c1616cb','5dd7bd1d-ae4c-4df7-9687-50fceb027344','8265f8cf-0969-4ec7-a266-2448de91c8c4','fd213210-71cd-41b0-b35b-68e1b18e9b43')
 $loadedIds=@($o.loadedWorld.arrangement.party|ForEach-Object{[string]$_.id})
 foreach($id in $original){if(@($loadedIds|Where-Object{$_-ceq$id}).Count-ne1){throw ('Stock Mammoth clean seed lacks original actor '+$id+'.')}}
 foreach($actor in $o.loadedWorld.arrangement.party){if($null-ne$actor.pet-or$null-ne$actor.master){throw 'Stock Mammoth clean seed contains a pet or master.'}}
 $loadedMain=@($o.loadedWorld.arrangement.party|Where-Object id -CEQ $original[0])[0]
 Assert-KmcOrdinaryInteger $loadedMain.level 3 3;Assert-KmcOrdinaryInteger $loadedMain.xp 9000 9000
 Assert-KmcOrdinaryInteger $o.loadedWorld.arrangement.money $o.initialMoney $o.initialMoney
 Assert-KmcOrdinaryInteger $o.savedWorld.arrangement.money $o.initialMoney $o.initialMoney
 foreach($actor in $o.loadedWorld.arrangement.party){$saved=@($o.savedWorld.arrangement.party|Where-Object id -CEQ $actor.id);if($saved.Count-ne1){throw 'Stock Mammoth saved actor lacks its loaded identity.'};foreach($key in @('object','descriptor','view','blueprint')){Assert-KmcBootstrapInventoryString ([string]$saved[0].$key) ([string]$actor.$key)}}
 $load=$o.nativeLoad
 for($i=0;$i-lt2;$i++){$e=$load.events[$i];Assert-KmcBootstrapInventoryString $e.kind $(if($i-eq0){'native-player-postload-before'}else{'native-player-postload-after'});foreach($key in @('game','saveManager','player')){Assert-KmcAssistedLabel $e.$key $labels;Assert-KmcOrdinaryInteger $e.$key $o.loadedWorld.$key $o.loadedWorld.$key};if($null-ne$e.gameId){throw 'Native PostLoad already carried a late campaign ID.'}}
 Assert-KmcBootstrapInventoryString $load.events[1].mainActorId $original[0]
 Assert-KmcOrdinaryInteger $load.events[0].frame $load.events[1].frame $load.events[1].frame
 Assert-KmcAssistedSame $o.neighboursBefore $o.neighboursAfter 'unchanged stock Mammoth neighbours'
 Assert-KmcAssistedArray $o.neighboursBefore.actors 3 3 'stock Mammoth conserved companions'
 foreach($neighbor in $o.neighboursBefore.actors){if($original[1..3] -cnotcontains [string]$neighbor.id){throw 'Stock Mammoth neighbor is foreign to owner seed.'};foreach($key in @('object','descriptor','view')){Assert-KmcAssistedLabel $neighbor.$key $labels};if($null-ne$neighbor.pet-or$null-ne$neighbor.master){throw 'Stock Mammoth neighbor contains a pet/master.'};Assert-KmcAssistedArray $neighbor.equipment 12 12 'stock Mammoth neighbor equipment';Assert-KmcAssistedArray $neighbor.classes 1 20 'stock Mammoth neighbor classes';Assert-KmcAssistedArray $neighbor.facts 1 128 'stock Mammoth neighbor facts'}
 foreach($id in $original[1..3]){if(@($o.neighboursBefore.actors|Where-Object id -CEQ $id).Count-ne1){throw ('Stock Mammoth neighbor missing '+$id+'.')}}
 foreach($neighbor in $o.neighboursBefore.actors){$loaded=@($o.loadedWorld.arrangement.party|Where-Object id -CEQ $neighbor.id)[0];$saved=@($o.savedWorld.arrangement.party|Where-Object id -CEQ $neighbor.id)[0];foreach($actor in @($loaded,$saved)){foreach($key in @('object','descriptor','view','blueprint','level','xp')){if([string]$actor.$key-cne[string]$neighbor.$key){throw ('Stock Mammoth neighbor projection differs: '+$key+'.')}}}}
 Assert-KmcOrdinaryInteger $o.initialMoney $o.afterSaveArrangement.money $o.afterSaveArrangement.money
 Assert-KmcAssistedSame $o.beforeSaveArrangement $o.afterSaveArrangement 'stock Mammoth native Manual changed arrangement'
 foreach($phase in @('loadedWorld','savedWorld','settledWorld')){
  $w=$o.$phase;Assert-KmcBootstrapInventoryString $w.arrangement.gameId $Request.persistenceLoad.gameId
  Assert-KmcBootstrapInventoryString $w.arrangement.gameName $Request.persistenceLoad.gameName
  Assert-KmcBootstrapInventoryString $w.arrangement.area $Request.persistenceLoad.area
  Assert-KmcOrdinaryInteger $w.owners 0 0;Assert-KmcOrdinaryInteger $w.slots 0 0;Assert-KmcOrdinaryInteger $w.casts 0 0
 }
 Assert-KmcOrdinaryInteger $o.loadedWorld.snapshots 0 0;Assert-KmcOrdinaryInteger $o.savedWorld.snapshots 1 1
 Assert-KmcOrdinaryInteger $o.savedWorld.completedSaves 1 1
 Assert-KmcAssistedArray $o.afterSaveArrangement.party 5 5 'four original characters and genuine stock Mammoth'
 $r=@($o.afterSaveArrangement.party|Where-Object id -CEQ '73681ef2-b281-4bc6-bc05-2de68c1616cb')
 if($r.Count-ne1){throw 'Stock Mammoth main actor is absent or duplicated.'}
 Assert-KmcOrdinaryInteger $r[0].level 10 10;Assert-KmcOrdinaryInteger $r[0].xp 105000 105000
 Assert-KmcBootstrapInventoryString $r[0].size 'Medium';Assert-KmcOrdinaryGuid $r[0].pet
 $pet=@($o.afterSaveArrangement.party|Where-Object id -CEQ $r[0].pet)
 if($pet.Count-ne1){throw 'Stock Mammoth reciprocal pet is absent or duplicated.'}
 Assert-KmcBootstrapInventoryString $pet[0].blueprint 'e7aa96d15a45238438ae4cfb476f6bb9'
 Assert-KmcBootstrapInventoryString $pet[0].size 'Large';Assert-KmcBootstrapInventoryString $pet[0].master $r[0].id
 $afterIds=@($o.afterSaveArrangement.party|ForEach-Object{[string]$_.id})
 foreach($id in $original){if(@($afterIds|Where-Object{$_-ceq$id}).Count-ne1){throw ('Stock Mammoth saved roster lacks '+$id+'.')}}
 if(@($afterIds|Where-Object{$_-ceq$pet[0].id}).Count-ne1){throw 'Stock Mammoth native pet is duplicated.'}
 Assert-KmcStockMammothBuild $o $Request $r[0] $pet[0]
 $w=$o.nativeWrite
 Assert-KmcBootstrapInventoryString $w.internalName 'KMC_C7_STOCK_MAMMOTH'
 Assert-KmcBootstrapInventoryString $w.type 'Manual';Assert-KmcBootstrapInventoryString $w.readKind 'Current'
 Assert-KmcOrdinaryInteger $w.sourceSchema 3 3;Assert-KmcOrdinaryInteger $w.callbackCount 1 1
 Assert-KmcOrdinaryInteger $w.completionSequence 1 1;Assert-KmcChunk7Boolean $w.nativeWorkerSettled $true
 if($w.fileName-cnotmatch'^Manual_[1-9][0-9]*_KMC_C7_STOCK_MAMMOTH\.zks$'){throw 'Stock Mammoth native Manual leaf is not canonical.'}
 Assert-KmcBootstrapInventoryString $w.fileName $o.savedWorld.authority.outputLeaf
 Assert-KmcBootstrapInventoryString $w.path (Join-Path (Get-KmcAssistedSaveRoot $Request) $w.fileName)
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $w.$key $Request.persistenceLoad.$key}
 Assert-KmcSmallFixtureEmptyArchive $w.archive $Request.persistenceLoad
 Assert-KmcBootstrapInventoryString $o.nativeSave.contract 'stock-mammoth-fixture-native-save-v1'
 foreach($key in @('installed','complete')){Assert-KmcChunk7Boolean $o.nativeSave.$key $true}
 foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $o.nativeSave.$key 1 1}
 Assert-KmcAssistedArray $o.nativeSave.errors 0 0 'stock Mammoth save observer errors'
 Assert-KmcStockMammothNativeSave $o.nativeSave $Request $o.savedWorld $w $labels
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','sourceUnchanged','selectionRestored','pauseRestored','ummLeaseReleased')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('manualRefunds','manualCompanionRemovals','manualFactRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
 Assert-KmcStockMammothAuthority $o.cleanup.authority $Request 1 1 $generation
}
