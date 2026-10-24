[CmdletBinding()]
param([string]$OutputDirectory)
# Matrix rows 58-59 readers: the capital-arrival producer (cold two-owner Manual, one transfer into the
# reviewed capital, one Manual there) and its separate-process cold consumer. Synthetic artifacts are
# derived from the accepted owned cold-load fixture; each exact refusal mutates one observation.
# Offline reader evidence only: no game, no native transaction, no archive.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('capital-arrival-readers-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'capital arrival reader checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior capital arrival reader evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){$errorText=$null;$pass=$false;try{&$Body;$pass=-not$Refuse}catch{$errorText=$_.Exception.Message;$pass=$Refuse-and(-not$Pattern-or$errorText-match$Pattern)};$results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Refuse;error=$errorText});if($pass){Write-Output ('PASS '+$Name)}else{Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Copy-Value($v){$v|ConvertTo-Json -Depth 90|ConvertFrom-Json}
$fixtures=(Get-Content -LiteralPath (Join-Path $chunk7TestSource 'OwnedPairsEvidenceFixtures.json') -Raw|ConvertFrom-Json).fixtures
$base=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]
$pin=Get-KmcChunk7CapitalArrivalPin;$capital=[string]$pin.area
$roles=@(Get-KmcChunk7OwnedPairIds $base.request)
$loaded=$base.artifact.observations.loadedArchive
$mainPair=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[0]})[0];$retiredPair=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]})[0]
$mainSlots=@($loaded.Slots|Where-Object ActorId -CEQ $roles[0])
function OwnerRow($Pair){[pscustomobject][ordered]@{ownerId=$Pair.PairId;generation=$Pair.Generation;riderId=$Pair.Rider.Id;mountId=$Pair.Mount.Id;state='Active'}}
function ViewRow([string]$Id,[int]$View,[bool]$Control,[int]$Agent,[int]$Agents){[pscustomobject][ordered]@{id=$Id;viewId=$View;baselineViewId=$View;directlyControllable=$Control;inGame=$true;masterId=$null;petId=$null;agentOverride=$Agent;riderAgents=$Agents;stockAgentEnabled=$true}}
function WorldRow([string]$Label,[string]$Area,[bool]$Capital,[int[]]$Counts,$Owners,$Slots,$Views,[string]$Feedback){
 $row=Copy-Value $base.artifact.observations.snapshots[0];$row.label=$Label;$row.owners=@($Owners)
 $extra=[ordered]@{area=$Area;capital=$Capital;mode='Default';suspensions=$Counts[0];resumes=$Counts[1];retired=$Counts[2];failedPairs=0;refusedTransfers=0;activeOwners=$Counts[3];saveAllowed=$true;feedback=$Feedback;slots=@($Slots);views=@($Views)};foreach($p in $extra.GetEnumerator()){$row|Add-Member -NotePropertyName $p.Key -NotePropertyValue $p.Value}
 $row
}
$umm=Get-KmcChunk7CapitalFeedback 'Synthetic Rider' 'Synthetic Horse' 1 2
# Row 58: the exact producer request and its complete synthetic PASS artifact.
function New-Producer{
 $q=Copy-Value $base.request;$q.persistenceCase='multi-exploration-capital-arrival-manual';$q.ownedPairs.expectedNativeWriteCount=1
 $q|Add-Member persistenceAreaTarget ([pscustomobject][ordered]@{enterPoint=$pin.enterPoint;area=$pin.area;autoSaveMode='None'})
 $s=$base.artifact.observations;$a=Copy-Value $base.artifact
 $cold=Copy-Value @($s.snapshots|Where-Object label -CEQ 'cold-restored-before-input')[0]
 $settled=Copy-Value $cold;$settled.label='capital-arrival-settled';$settled.owners=@(OwnerRow $mainPair);$settled.controls.ManagedHotbarSlotCount=2
 $written=Copy-Value $settled;$written.label='native-write-complete';$written.snapshots=1
 $before=WorldRow 'before-native-transfer' $q.persistenceLoad.area $false @(0,0,0,2) @((OwnerRow $mainPair),(OwnerRow $retiredPair)) $loaded.Slots @((ViewRow $roles[0] 11 $true 101 1),(ViewRow $roles[1] 12 $true 0 0),(ViewRow $roles[2] 13 $true 201 1),(ViewRow $roles[3] 14 $true 0 0)) 'Saved mounted world restored all 2 attachments.'
 $after=WorldRow 'after-native-transfer' $capital $true @(1,1,1,1) @(OwnerRow $mainPair) $mainSlots @((ViewRow $roles[0] 11 $true 102 1),(ViewRow $roles[1] 12 $true 0 0),(ViewRow $roles[2] 13 $false 0 0),(ViewRow $roles[3] 14 $false 0 0)) $umm
 $n=[int]([regex]::Match($q.persistenceLoad.fileName,'^Manual_([0-9]+)_').Groups[1].Value)+1;$leaf='Manual_'+$n+'_KMC_C7_CAPITAL.zks'
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $q.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$q.runId)) 'Saved Games'
 $inputRow=[pscustomobject][ordered]@{fileName=$q.persistenceLoad.fileName;nativeName=$q.persistenceLoad.internalName;nativeType='Manual';gameId=$q.persistenceLoad.gameId;gameName=$q.persistenceLoad.gameName;area=$q.persistenceLoad.area;hasFileOnDisk=$true;isActuallySaved=$true;operation='None';autoLevelup=$false}
 $output=Copy-Value $inputRow;$output.fileName=$leaf;$output.nativeName='KMC_C7_CAPITAL';$output.area=$capital
 function Authority([int]$Writes){[pscustomobject][ordered]@{root=$root;loadCap=1;writeCap=1;nativeLoads=1;nativeWrites=$Writes;authorizedLoads=1;authorizedWrites=$Writes;reservationCount=1;number=$n;leaf=$leaf;nativeName='KMC_C7_CAPITAL';nativeType='Manual'}}
 $archive=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$loaded.CampaignId;AreaId=$capital;GameTimeTicks=$loaded.GameTimeTicks+20000000;Policy=$loaded.Policy;RulesId=$loaded.RulesId;Pairs=@(Copy-Value $mainPair);Participations=@();Slots=@($mainSlots|ForEach-Object{Copy-Value $_});Combat=$null}
 $c=[pscustomobject][ordered]@{contract='chunk7-capital-arrival-v1';enterPoint=$pin.enterPoint;sourceArea=$q.persistenceLoad.area;destinationArea=$capital;autoSaveMode='None';destinationIsCapital=$true;mainPairId=$mainPair.PairId;retiredPairId=$retiredPair.PairId;retiredRiderName='Synthetic Rider';retiredMountName='Synthetic Horse';transferRequests=1;nativeAutosaveRequested=$false;before=$before;dispatchReturned=$true;awaitingNativeReady=[pscustomobject]@{frame=10};arrival=[pscustomobject]@{frame=11};restoration=(Copy-Value $s.coldRestoration);umm=$umm;after=$after;loadingTicks=3;suspensionObserved=$true}
 $o=[ordered]@{checkpoint=$q.persistenceCase;cold=$true;sourceWorkingSha256=$null;inheritedManualSha256=$s.inheritedManualSha256;processId=1111;snapshots=@($cold,$settled,$written);inputs=@();movements=@();nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;combatDebtProof=$false;nativeSchema=$s.nativeSchema;nativeCapacity=$s.nativeCapacity
  inheritedFixture=$s.inheritedFixture;coldRestoration=$s.coldRestoration;loadedArchive=$loaded;loadedDescriptor=$s.loadedDescriptor;capitalArrival=$c
  ownedOutputAdmission=[pscustomobject][ordered]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';number=$n;leaf=$leaf;nativeInventory=@($inputRow);authority=(Authority 0)}
  saveRequested=[pscustomobject][ordered]@{nativeType='Manual';nativeName='KMC_C7_CAPITAL';gameTicks=$archive.GameTimeTicks;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true}
  ownedOutputCompletion=[pscustomobject][ordered]@{completionSequence=1;callbackCount=1;nativeWorkerSettled=$true;nativeInventory=@($inputRow,$output);authority=(Authority 1)}
  nativeWrite=[pscustomobject][ordered]@{path=(Join-Path $root $leaf);sha256=('9'*64);length=2345;nativeType='Manual';nativeName='KMC_C7_CAPITAL';nativeCallback=$true;operation='None';snapshotCount=1;area=$capital;saveAllowedAfterWrite=$true;archive=$archive}
  inheritedCleanup=$s.inheritedCleanup;cleanup=$s.cleanup;elapsedSeconds=42.5;nativeShellLifecycle=[pscustomobject]@{settled=$true}}
 $a.observations=Copy-Value ([pscustomobject]$o)
 [pscustomobject]@{request=$q;artifact=$a}
}
# Row 59: the exact consumer of the producer's capital Manual.
function New-Consumer($Producer){
 $p=$Producer.artifact.observations;$w=$p.nativeWrite
 $r=Copy-Value $base.request;$r.persistenceCase='multi-exploration-capital-arrival-cold-manual';$r.ownedPairs.checkpointSourceRunId='synthetic-capital-arrival';$r.ownedPairs.checkpointSourceArtifactSha256='3'*64
 $r.persistenceLoad=[pscustomobject][ordered]@{internalName='KMC_C7_CAPITAL';fileName=$p.ownedOutputAdmission.leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=2;gameId=$base.request.persistenceLoad.gameId;gameName=$base.request.persistenceLoad.gameName;area=$capital}
 $a=Copy-Value $base.artifact;$s=$base.artifact.observations
 $probe=Copy-Value $s.coldRestoration;$probe.events=@($probe.events|Where-Object{$_.actorId-cin@($roles[0],$roles[1])})
 $snapshot=Copy-Value @($s.snapshots|Where-Object label -CEQ 'cold-restored-before-input')[0];$snapshot.owners=@(OwnerRow $mainPair);$snapshot.semanticRestores=2;$snapshot.presentationRestores=1;$snapshot.nativePostLoads=2;$snapshot.controls.ManagedHotbarSlotCount=2
 $snapshot.actors=@($snapshot.actors|Where-Object{$_.native.Id-cin@($roles[0],$roles[1])})
 $world=WorldRow 'cold-restored-in-capital' $capital $true @(0,0,0,1) @(OwnerRow $mainPair) $mainSlots @((ViewRow $roles[0] 21 $true 301 1),(ViewRow $roles[1] 22 $true 0 0),(ViewRow $roles[2] 23 $false 0 0),(ViewRow $roles[3] 24 $false 0 0)) 'Saved mounted world restored all 1 attachments.'
 foreach($v in $world.views){$v.baselineViewId=0}
 $c=[pscustomobject][ordered]@{contract='chunk7-capital-arrival-cold-v1';producerRunId=$r.ownedPairs.checkpointSourceRunId;producerArtifactSha256=$r.ownedPairs.checkpointSourceArtifactSha256;mainPairId=$mainPair.PairId;mainGeneration=$mainPair.Generation;unmountedRiderId=$roles[2];unmountedMountId=$roles[3];registryOwners=1;activeOwners=1;world=$world}
 $o=[ordered]@{checkpoint=$r.persistenceCase;cold=$true;sourceWorkingSha256=$null;inheritedManualSha256=$s.inheritedManualSha256;processId=2222;snapshots=@($snapshot);inputs=@();movements=@();nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;combatDebtProof=$false;nativeSchema=$s.nativeSchema;nativeCapacity=$s.nativeCapacity
  inheritedFixture=$s.inheritedFixture;coldRestoration=$probe;loadedArchive=(Copy-Value $w.archive);loadedDescriptor=(Copy-Value $r.persistenceLoad);capitalCold=$c;inheritedCleanup=$s.inheritedCleanup;cleanup=$s.cleanup;elapsedSeconds=21.5;nativeShellLifecycle=[pscustomobject]@{settled=$true}}
 $a.observations=Copy-Value ([pscustomobject]$o)
 [pscustomobject]@{request=$r;artifact=$a}
}
function Producer-Artifact($Producer){$v=Copy-Value $Producer.artifact;$v.runId='synthetic-capital-arrival';$v}

Check 'capital producer request is admitted by the owned descriptor' {Assert-KmcChunk7OwnedPairsDescriptor (New-Producer).request}
Check 'capital producer complete PASS through the persistence dispatch' {$p=New-Producer;Assert-KmcChunk7PersistenceObservation $p.request $p.artifact 'PASS'}
Check 'capital producer without its optional awaiting boundary' {$p=New-Producer;$p.artifact.observations.capitalArrival.PSObject.Properties.Remove('awaitingNativeReady');Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact}
Check 'capital consumer request is admitted by the owned descriptor' {Assert-KmcChunk7OwnedPairsDescriptor (New-Consumer (New-Producer)).request}
Check 'capital consumer complete PASS through the persistence dispatch' {$c=New-Consumer (New-Producer);Assert-KmcChunk7PersistenceObservation $c.request $c.artifact 'PASS'}
Check 'capital consumer restores exactly the producer read-back in a separate process' {$p=New-Producer;$c=New-Consumer $p;Assert-KmcChunk7CapitalColdSource $c.request (Producer-Artifact $p) $c.artifact}
Check 'a failed capital artifact keeps its FAIL envelope only' {$p=New-Producer;$p.artifact.status='FAIL';Assert-KmcChunk7PersistenceObservation $p.request $p.artifact 'FAIL'}

$producerMutations=[ordered]@{
 'artifact status FAIL'=@({param($o,$a)$a.status='FAIL'},'artifact failed')
 'extra observation'=@({param($o,$a)$o|Add-Member movementsAfter @()},'property set')
 'missing native write'=@({param($o,$a)$o.PSObject.Properties.Remove('nativeWrite')},'property set')
 'Mount input issued'=@({param($o,$a)$o.inputs=@([pscustomobject]@{label='mount-horse'})},'Mount, Dismount or movement')
 'movement issued'=@({param($o,$a)$o.movements=@([pscustomobject]@{label='first-pair'})},'Mount, Dismount or movement')
 'warm start'=@({param($o,$a)$o.cold=$false},'boolean')
 'input already in the capital'=@({param($o,$a)$o.loadedArchive.AreaId='ead426a6c23d39548a670ee515d77df4'},'')
 'cold start one pair'=@({param($o,$a)$o.loadedArchive.Pairs=@($o.loadedArchive.Pairs[0])},'')
 'cold start replayed a Mount'=@({param($o,$a)$o.snapshots[0].nativeCastRequests=1},'')
 'cold preparation event'=@({param($o,$a)$o.coldRestoration.events+=@([pscustomobject]@{actorId=$roles[0];kind='prepare-before'})},'preparation')
 'other enter point'=@({param($o,$a)$o.capitalArrival.enterPoint='1'*32},'scalar identity')
 'native autosave'=@({param($o,$a)$o.capitalArrival.autoSaveMode='AfterEntry'},'scalar identity')
 'autosave requested'=@({param($o,$a)$o.capitalArrival.nativeAutosaveRequested=$true},'boolean')
 'not a capital'=@({param($o,$a)$o.capitalArrival.destinationIsCapital=$false},'boolean')
 'two transfers'=@({param($o,$a)$o.capitalArrival.transferRequests=2},'counter')
 'swapped retired pair'=@({param($o,$a)$o.capitalArrival.retiredPairId=$mainPair.PairId},'scalar identity')
 'UMM omits the dismounted pair'=@({param($o,$a)$o.capitalArrival.umm='All 2 mounted relationships restored before area autosave without Mount or preparation.'},'scalar identity')
 'UMM counts two restored'=@({param($o,$a)$o.capitalArrival.umm=(Get-KmcChunk7CapitalFeedback 'Synthetic Rider' 'Synthetic Horse' 2 2)},'scalar identity')
 'arrival preparation event'=@({param($o,$a)$o.capitalArrival.restoration.events+=@([pscustomobject]@{actorId=$roles[0];kind='round-before'})},'preparation')
 'arrival extra debt restore'=@({param($o,$a)$o.capitalArrival.restoration.events+=@([pscustomobject]@{actorId=$roles[2];kind='restore-debt-before'})},'debt restore')
 'arrival kept both pairs'=@({param($o,$a)$o.capitalArrival.after.retired=0},'counter')
 'arrival second restored pair'=@({param($o,$a)$o.capitalArrival.after.resumes=2},'counter')
 'arrival failed pair'=@({param($o,$a)$o.capitalArrival.after.failedPairs=2},'counter')
 'arrival two owners'=@({param($o,$a)$o.capitalArrival.after.activeOwners=2},'counter')
 'arrival orphan owner row'=@({param($o,$a)$o.capitalArrival.after.owners+=@(OwnerRow $retiredPair)},'exactly one mounted owner')
 'arrival owner generation'=@({param($o,$a)$o.capitalArrival.after.owners[0].generation=99},'main pair')
 'saving blocked'=@({param($o,$a)$o.capitalArrival.after.saveAllowed=$false},'boolean')
 'retired controls kept'=@({param($o,$a)$o.capitalArrival.after.slots=@($o.loadedArchive.Slots)},'main pair bindings')
 'retired pair controllable'=@({param($o,$a)$o.capitalArrival.after.views[2].directlyControllable=$true},'boolean')
 'main pair uncontrollable'=@({param($o,$a)$o.capitalArrival.after.views[0].directlyControllable=$false},'boolean')
 'retired rider kept an agent'=@({param($o,$a)$o.capitalArrival.after.views[2].riderAgents=1},'movement authority')
 'retired rider kept an override'=@({param($o,$a)$o.capitalArrival.after.views[2].agentOverride=201},'movement authority')
 'main rider lost authority'=@({param($o,$a)$o.capitalArrival.after.views[0].riderAgents=0},'movement authority')
 'main rider reused authority'=@({param($o,$a)$o.capitalArrival.after.views[0].agentOverride=101},'reused')
 'view replaced'=@({param($o,$a)$o.capitalArrival.after.views[3].viewId=99;$o.capitalArrival.after.views[3].baselineViewId=99},'replaced')
 'settled snapshot two owners'=@({param($o,$a)$o.snapshots[1].owners=@((OwnerRow $mainPair),(OwnerRow $retiredPair))},'exactly one mounted owner')
 'settled snapshot replayed a snapshot'=@({param($o,$a)$o.snapshots[1].snapshots=1},'replayed')
 'Manual two pairs'=@({param($o,$a)$o.nativeWrite.archive.Pairs=@($o.loadedArchive.Pairs)},'exactly one mounted')
 'Manual outside the capital'=@({param($o,$a)$o.nativeWrite.archive.AreaId=$o.loadedArchive.AreaId},'exactly one mounted')
 'Manual retired pair'=@({param($o,$a)$o.nativeWrite.archive.Pairs=@(Copy-Value $retiredPair)},'scalar identity')
 'Manual generation changed'=@({param($o,$a)$o.nativeWrite.archive.Pairs[0].Generation=99},'stable custody')
 'Manual retired controls'=@({param($o,$a)$o.nativeWrite.archive.Slots=@(@($o.loadedArchive.Slots|Where-Object ActorId -CEQ $roles[2]))},'controls differ')
 'Manual native name'=@({param($o,$a)$o.nativeWrite.nativeName='KMC_C7_WORLD'},'scalar identity')
 'Manual saving blocked afterwards'=@({param($o,$a)$o.nativeWrite.saveAllowedAfterWrite=$false},'boolean')
 'Manual aliases its input'=@({param($o,$a)$o.nativeWrite.sha256=$base.request.persistenceLoad.sha256},'aliases')
 'Manual no callback'=@({param($o,$a)$o.nativeWrite.nativeCallback=$false},'boolean')
 'save refused'=@({param($o,$a)$o.saveRequested.nativeSaveAllowed=$false},'boolean')
 'second native write'=@({param($o,$a)$o.ownedOutputCompletion.authority.nativeWrites=2},'counter')
 'write snapshot owners'=@({param($o,$a)$o.snapshots[2].owners=@()},'exactly one mounted owner')
 'cleanup retained an owner'=@({param($o,$a)$o.cleanup.world.owners=@(OwnerRow $mainPair)},'retained native custody')
}
foreach($m in $producerMutations.GetEnumerator()){Check ('capital producer refuses '+$m.Key) {$p=New-Producer;&$m.Value[0] $p.artifact.observations $p.artifact;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true $m.Value[1]}
$producerRequestMutations=[ordered]@{
 'consumer case'={param($q)$q.persistenceCase='multi-exploration-capital-arrival-cold-manual'}
 'save selection'={param($q)$q.ownedPairs.case='persistence-save'}
 'missing target'={param($q)$q.PSObject.Properties.Remove('persistenceAreaTarget')}
 'other area target'={param($q)$q.persistenceAreaTarget.area='b'*32}
 'zero writes'={param($q)$q.ownedPairs.expectedNativeWriteCount=0}
}
foreach($m in $producerRequestMutations.GetEnumerator()){Check ('capital producer request refuses '+$m.Key) {$p=New-Producer;&$m.Value $p.request;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true}

$consumerMutations=[ordered]@{
 'two mounted pairs'=@({param($o)$o.loadedArchive.Pairs=@($o.loadedArchive.Pairs[0],(Copy-Value $retiredPair))},'exactly one mounted pair')
 'retired pair mounted instead'=@({param($o)$o.loadedArchive.Pairs=@(Copy-Value $retiredPair)},'main character')
 'archive outside the capital'=@({param($o)$o.loadedArchive.AreaId='a'*32},'exactly one mounted')
 'retired debt restored'=@({param($o)$o.coldRestoration.events+=@([pscustomobject]@{actorId=$roles[2];kind='restore-debt-before'})},'debt restore')
 'cold preparation'=@({param($o)$o.coldRestoration.events+=@([pscustomobject]@{actorId=$roles[1];kind='prepare-before'})},'preparation')
 'two owners'=@({param($o)$o.snapshots[0].owners=@((OwnerRow $mainPair),(OwnerRow $retiredPair))},'exactly one mounted owner')
 'restored the other actors'=@({param($o)$o.snapshots[0].semanticRestores=4},'replayed input')
 'retired controls restored'=@({param($o)$o.snapshots[0].controls.ManagedHotbarSlotCount=4},'replayed input')
 'orphan registry owner'=@({param($o)$o.capitalCold.registryOwners=2},'counter')
 'second active owner'=@({param($o)$o.capitalCold.activeOwners=2},'counter')
 'unmounted ids swapped'=@({param($o)$o.capitalCold.unmountedRiderId=$roles[0]},'scalar identity')
 'producer pin'=@({param($o)$o.capitalCold.producerArtifactSha256='0'*64},'scalar identity')
 'retired pair controllable'=@({param($o)$o.capitalCold.world.views[2].directlyControllable=$true},'boolean')
 'retired rider authority'=@({param($o)$o.capitalCold.world.views[2].riderAgents=1},'movement authority')
 'main rider without authority'=@({param($o)$o.capitalCold.world.views[0].agentOverride=0},'movement authority')
 'world not a capital'=@({param($o)$o.capitalCold.world.capital=$false},'boolean')
 'world retired an arrival pair'=@({param($o)$o.capitalCold.world.retired=1},'counter')
 'world retired controls'=@({param($o)$o.capitalCold.world.slots=@($o.capitalCold.world.slots+(Copy-Value $mainSlots[0]))},'main pair bindings')
 'extra native write'=@({param($o)$o|Add-Member nativeWrite ([pscustomobject]@{})},'property set')
 'Mount input'=@({param($o)$o.inputs=@([pscustomobject]@{label='mount-horse'})},'Mount, Dismount or movement')
 'other Manual loaded'=@({param($o)$o.loadedDescriptor.sha256='0'*64},'another Manual')
}
foreach($m in $consumerMutations.GetEnumerator()){Check ('capital consumer refuses '+$m.Key) {$c=New-Consumer (New-Producer);&$m.Value[0] $c.artifact.observations;Assert-KmcChunk7CapitalColdObservation $c.request $c.artifact} $true $m.Value[1]}
$crossMutations=[ordered]@{
 'same process'={param($p,$c)$c.artifact.observations.processId=$p.observations.processId}
 'archive differs from read-back'={param($p,$c)$p.observations.nativeWrite.archive.GameTimeTicks++}
 'archive bytes differ'={param($p,$c)$p.observations.nativeWrite.sha256='8'*64}
 'archive length differs'={param($p,$c)$p.observations.nativeWrite.length++}
 'producer failed'={param($p,$c)$p.status='FAIL'}
 'producer other run'={param($p,$c)$p.runId='synthetic-other-run'}
 'producer other checkpoint'={param($p,$c)$p.observations.checkpoint='multi-exploration-manual'}
 'producer progression changed'={param($p,$c)$p.observations.inheritedCleanup.arrangement.money++}
 'producer main pair differs'={param($p,$c)$p.observations.capitalArrival.mainPairId=$retiredPair.PairId}
}
foreach($m in $crossMutations.GetEnumerator()){Check ('capital cross-process proof refuses '+$m.Key) {$p=New-Producer;$c=New-Consumer $p;$a=Producer-Artifact $p;&$m.Value $a $c;Assert-KmcChunk7CapitalColdSource $c.request $a $c.artifact} $true}

# The UMM summary contract: the product line, the in-game diagnostic expectation and the reader agree.
Check 'UMM capital summary text agrees across product, engine and reader' {
 $product=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedAreaPersistence.cs'))
 $engine=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.CapitalArrival.cs'))
 foreach($fragment in @('"Dismounted on arrival because the companion is not directly controllable in this area: "','" and "','". Mount again after leaving. "','" of "','" mounted relationships restored without Mount or preparation."')){
  if(-not$product.Contains($fragment)-or-not$engine.Contains($fragment)){throw ('Summary fragment differs: '+$fragment)}
 }
 if($umm-cne'Dismounted on arrival because the companion is not directly controllable in this area: Synthetic Rider and Synthetic Horse. Mount again after leaving. 1 of 2 mounted relationships restored without Mount or preparation.'){throw 'Reader summary composition differs.'}
}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;gameLaunches=0;archivesRead=0;scope='capital-arrival producer, cold consumer and cross-process readers over synthetic artifacts derived from the owned cold fixture';results=@($results)}|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ('CAPITAL ARRIVAL READERS PASS='+$passed+' FAIL='+$failed+'; synthetic reader evidence only, native rows 58-59 not run.')
if($failed-ne0){exit 1}
