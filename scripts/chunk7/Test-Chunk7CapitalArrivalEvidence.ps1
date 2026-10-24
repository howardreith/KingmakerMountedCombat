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
# Batch196 r58 observed shape: the uncontrollable companion pair in the capital is out of the scene (inGame false).
$script:Shape='remote';$script:ColdViews=$true
# Batch197 r58 observed shape: the unpaired story companions also move to the native remote party (in game, uncontrollable).
$script:Remote=$false
# The owned cold fixture holds only the two pairs; the remote mode adds three unpaired story companions to its
# complete roster (request, inherited custody and progression) exactly as a seven-actor field records them.
[string[]]$companions=@('70707070-7070-4070-8070-707070707070','50505050-5050-4050-8050-505050505050','60606060-6060-4060-8060-606060606060');[Array]::Sort($companions,[StringComparer]::Ordinal)
$remoteBase=Copy-Value $base;$remoteBase.request.ownedPairs.partyIds=@(@($remoteBase.request.ownedPairs.partyIds)+$companions)
foreach($snapshot in @($remoteBase.artifact.observations.inheritedFixture,$remoteBase.artifact.observations.inheritedCleanup)){
 $snapshot.partyIds=@(@($snapshot.partyIds)+$companions);$snapshot.currentPartyIds=@(@($snapshot.currentPartyIds)+$companions|Sort-Object -CaseSensitive)
 $snapshot.currentCharacterIds=@(@($snapshot.currentCharacterIds)+$companions|Sort-Object -CaseSensitive)
 $template=@($snapshot.arrangement.actors|Where-Object actorId -CEQ $roles[0])[0]
 $snapshot.arrangement.actors=@(@($snapshot.arrangement.actors)+@(foreach($id in $companions){$row=Copy-Value $template;$row.actorId=$id;$row}))
}
function With-Remote([scriptblock]$Body){$saved=$script:base;$script:base=$script:remoteBase;$script:Remote=$true;try{&$Body}finally{$script:base=$saved;$script:Remote=$false}}
function RemoteIds{if($script:Remote){@($companions)}else{@()}}
function RemoteRecord{,@(foreach($id in (RemoteIds)){[pscustomobject][ordered]@{id=$id;inParty=$false;character=$false;remote=$true;inGame=$true;inState=$true;directlyControllable=$false;viewPresent=$true;mainCharacter=$false;pairMember=$false;skippedChecks=@(Get-KmcChunk7CapitalRemoteSkippedChecks)}})}
function OutRecord([string]$Shape,[bool]$Views){[pscustomobject][ordered]@{ids=@($roles[2],$roles[3]);shape=$Shape;actors=@(foreach($id in @($roles[2],$roles[3])){[pscustomobject][ordered]@{id=$id;inGame=$false;inState=$true;directlyControllable=$false;viewPresent=$Views;liveRigValidated=$false}});admittedNativeRemoteCompanions=(RemoteRecord)}}
function RosterRows([AllowNull()][string]$Shape){@(foreach($id in $base.request.ownedPairs.partyIds){$out=-not[string]::IsNullOrEmpty($Shape)-and$id-cin@($roles[2],$roles[3]);$rem=-not[string]::IsNullOrEmpty($Shape)-and$id-cin(RemoteIds)
 if($rem){[pscustomobject][ordered]@{id=$id;inParty=$false;character=$false;remote=$true;inGame=$true;inState=$true;directlyControllable=$false;viewPresent=$true};continue}
 [pscustomobject][ordered]@{id=$id;inParty=(-not$out-or$Shape-ceq'field');character=($id-cnotin@($roles[1],$roles[3])-and(-not$out-or$Shape-ceq'field'));remote=($out-and$Shape-ceq'remote'-and$id-ceq$roles[2]);inGame=(-not$out);inState=$true;directlyControllable=(-not$out-and([string]::IsNullOrEmpty($Shape)-or$id-cin@($roles[0],$roles[1])));viewPresent=$true}})}
function CapitalInherited($Snapshot,[string]$Shape,[bool]$Views){$v=Copy-Value $Snapshot;$field=$Shape-ceq'field';$rem=@(RemoteIds)
 $v.currentPartyIds=@($v.partyIds|Where-Object{($field-or$_-cnotin@($roles[2],$roles[3]))-and$_-cnotin$rem}|Sort-Object -CaseSensitive)
 $v.currentCharacterIds=@($v.currentCharacterIds|Where-Object{($field-or$_-cne$roles[2])-and$_-cnotin$rem})
 [string[]]$remote=@(@($(if($Shape-ceq'remote'){$roles[2]}))+$rem);[Array]::Sort($remote,[StringComparer]::Ordinal);$v.remoteCapturedIds=@($remote)
 $v|Add-Member capitalOutOfScene (OutRecord $Shape $Views);$v}
function ViewRow([string]$Id,[int]$View,[bool]$Control,[int]$Agent,[int]$Agents){[pscustomobject][ordered]@{id=$Id;viewId=$View;baselineViewId=$View;directlyControllable=$Control;inGame=$Control;masterId=$null;petId=$null;agentOverride=$Agent;riderAgents=$Agents;stockAgentEnabled=$true}}
function WorldRow([string]$Label,[string]$Area,[bool]$Capital,[int[]]$Counts,$Owners,$Slots,$Views,[string]$Feedback,[AllowNull()][string]$Shape=$null){
 $row=Copy-Value $base.artifact.observations.snapshots[0];$row.label=$Label;$row.owners=@($Owners)
 $extra=[ordered]@{area=$Area;capital=$Capital;mode='Default';suspensions=$Counts[0];resumes=$Counts[1];retired=$Counts[2];failedPairs=0;refusedTransfers=0;activeOwners=$Counts[3];saveAllowed=$true;feedback=$Feedback;slots=@($Slots);views=@($Views);roster=@(RosterRows $Shape)};foreach($p in $extra.GetEnumerator()){$row|Add-Member -NotePropertyName $p.Key -NotePropertyValue $p.Value}
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
 $written=Copy-Value $settled;$written.label='native-write-complete';$written.snapshots=1;$written.gameTicks=$loaded.GameTimeTicks+20000000
 $before=WorldRow 'before-native-transfer' $q.persistenceLoad.area $false @(0,0,0,2) @((OwnerRow $mainPair),(OwnerRow $retiredPair)) $loaded.Slots @((ViewRow $roles[0] 11 $true 101 1),(ViewRow $roles[1] 12 $true 0 0),(ViewRow $roles[2] 13 $true 201 1),(ViewRow $roles[3] 14 $true 0 0)) 'Saved mounted world restored all 2 attachments.'
 $after=WorldRow 'after-native-transfer' $capital $true @(1,1,1,1) @(OwnerRow $mainPair) $mainSlots @((ViewRow $roles[0] 11 $true 102 1),(ViewRow $roles[1] 12 $true 0 0),(ViewRow $roles[2] 13 $false 0 0),(ViewRow $roles[3] 14 $false 0 0)) $umm $script:Shape
 $n=[int]([regex]::Match($q.persistenceLoad.fileName,'^Manual_([0-9]+)_').Groups[1].Value)+1;$leaf='Manual_'+$n+'_KMC_C7_CAPITAL.zks'
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $q.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$q.runId)) 'Saved Games'
 $inputRow=[pscustomobject][ordered]@{fileName=$q.persistenceLoad.fileName;nativeName=$q.persistenceLoad.internalName;nativeType='Manual';gameId=$q.persistenceLoad.gameId;gameName=$q.persistenceLoad.gameName;area=$q.persistenceLoad.area;hasFileOnDisk=$true;isActuallySaved=$true;operation='None';autoLevelup=$false}
 $output=Copy-Value $inputRow;$output.fileName=$leaf;$output.nativeName='KMC_C7_CAPITAL';$output.area=$capital
 function Authority([int]$Writes){[pscustomobject][ordered]@{root=$root;loadCap=1;writeCap=1;nativeLoads=1;nativeWrites=$Writes;authorizedLoads=1;authorizedWrites=$Writes;reservationCount=1;number=$n;leaf=$leaf;nativeName='KMC_C7_CAPITAL';nativeType='Manual'}}
 $archive=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$loaded.CampaignId;AreaId=$capital;GameTimeTicks=$loaded.GameTimeTicks+20000000;Policy=$loaded.Policy;RulesId=$loaded.RulesId;Pairs=@(Copy-Value $mainPair);Participations=@();Slots=@($mainSlots|ForEach-Object{Copy-Value $_});Combat=$null}
 $c=[pscustomobject][ordered]@{contract='chunk7-capital-arrival-v1';enterPoint=$pin.enterPoint;sourceArea=$q.persistenceLoad.area;destinationArea=$capital;autoSaveMode='None';destinationIsCapital=$true;mainPairId=$mainPair.PairId;retiredPairId=$retiredPair.PairId;retiredRiderName='Synthetic Rider';retiredMountName='Synthetic Horse';retiredOutOfScene=(OutRecord $script:Shape $true);transferRequests=1;nativeAutosaveRequested=$false;before=$before;dispatchReturned=$true;awaitingNativeReady=[pscustomobject]@{frame=10};arrival=[pscustomobject]@{frame=11};restoration=(Copy-Value $s.coldRestoration);umm=$umm;after=$after;loadingTicks=3;suspensionObserved=$true}
 $o=[ordered]@{checkpoint=$q.persistenceCase;cold=$true;sourceWorkingSha256=$null;inheritedManualSha256=$s.inheritedManualSha256;processId=1111;snapshots=@($cold,$settled,$written);inputs=@();movements=@();nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;combatDebtProof=$false;nativeSchema=$s.nativeSchema;nativeCapacity=$s.nativeCapacity
  inheritedFixture=$s.inheritedFixture;coldRestoration=$s.coldRestoration;loadedArchive=$loaded;loadedDescriptor=$s.loadedDescriptor;capitalArrival=$c
  ownedOutputAdmission=[pscustomobject][ordered]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';number=$n;leaf=$leaf;nativeInventory=@($inputRow);authority=(Authority 0)}
  saveRequested=[pscustomobject][ordered]@{nativeType='Manual';nativeName='KMC_C7_CAPITAL';gameTicks=$archive.GameTimeTicks;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true}
  ownedOutputCompletion=[pscustomobject][ordered]@{completionSequence=1;callbackCount=1;nativeWorkerSettled=$true;nativeInventory=@($inputRow,$output);authority=(Authority 1)}
  nativeWrite=[pscustomobject][ordered]@{path=(Join-Path $root $leaf);sha256=('9'*64);length=2345;nativeType='Manual';nativeName='KMC_C7_CAPITAL';nativeCallback=$true;operation='None';snapshotCount=1;area=$capital;saveAllowedAfterWrite=$true;archive=$archive}
  inheritedCleanup=(CapitalInherited $s.inheritedCleanup $script:Shape $true);cleanup=$s.cleanup;elapsedSeconds=42.5;nativeShellLifecycle=[pscustomobject]@{settled=$true}}
 $a.observations=Copy-Value ([pscustomobject]$o)
 [pscustomobject]@{request=$q;artifact=$a}
}
# Row 59: the exact consumer of the producer's capital Manual.
function New-Consumer($Producer){
 $p=$Producer.artifact.observations;$w=$p.nativeWrite
 $r=Copy-Value $base.request;$r.persistenceCase='multi-exploration-capital-arrival-cold-manual';$r.ownedPairs.checkpointSourceRunId='synthetic-capital-arrival';$r.ownedPairs.checkpointSourceArtifactSha256='3'*64
 # Batch198 r59: the lineage Manual's area is pinned as the area row 58 departed from.
 $r.ownedPairs|Add-Member sourceArea ([string]$p.capitalArrival.sourceArea)
 $r.persistenceLoad=[pscustomobject][ordered]@{internalName='KMC_C7_CAPITAL';fileName=$p.ownedOutputAdmission.leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=2;gameId=$base.request.persistenceLoad.gameId;gameName=$base.request.persistenceLoad.gameName;area=$capital}
 $a=Copy-Value $base.artifact;$s=$base.artifact.observations
 $probe=Copy-Value $s.coldRestoration;$probe.events=@($probe.events|Where-Object{$_.actorId-cin@($roles[0],$roles[1])})
 $snapshot=Copy-Value @($s.snapshots|Where-Object label -CEQ 'cold-restored-before-input')[0];$snapshot.owners=@(OwnerRow $mainPair);$snapshot.semanticRestores=2;$snapshot.presentationRestores=1;$snapshot.nativePostLoads=2;$snapshot.controls.ManagedHotbarSlotCount=2
 $snapshot.actors=@($snapshot.actors|Where-Object{$_.native.Id-cin@($roles[0],$roles[1])});$snapshot.gameTicks=$w.archive.GameTimeTicks
 $world=WorldRow 'cold-restored-in-capital' $capital $true @(0,0,0,1) @(OwnerRow $mainPair) $mainSlots @((ViewRow $roles[0] 21 $true 301 1),(ViewRow $roles[1] 22 $true 0 0),(ViewRow $roles[2] $(if($script:ColdViews){23}else{0}) $false 0 0),(ViewRow $roles[3] $(if($script:ColdViews){24}else{0}) $false 0 0)) 'Saved mounted world restored all 1 attachments.' $script:Shape
 foreach($v in $world.views){$v.baselineViewId=0}
 $c=[pscustomobject][ordered]@{contract='chunk7-capital-arrival-cold-v1';producerRunId=$r.ownedPairs.checkpointSourceRunId;producerArtifactSha256=$r.ownedPairs.checkpointSourceArtifactSha256;mainPairId=$mainPair.PairId;mainGeneration=$mainPair.Generation;unmountedRiderId=$roles[2];unmountedMountId=$roles[3];registryOwners=1;outOfScene=(OutRecord $script:Shape $script:ColdViews);activeOwners=1;world=$world}
 $o=[ordered]@{checkpoint=$r.persistenceCase;cold=$true;sourceWorkingSha256=$null;inheritedManualSha256=$s.inheritedManualSha256;processId=2222;snapshots=@($snapshot);inputs=@();movements=@();nativeProgressionRequiresExternalWorkingRestoration=$false;ordinaryRespecProven=$false;combatDebtProof=$false;nativeSchema=$s.nativeSchema;nativeCapacity=$s.nativeCapacity
  inheritedFixture=(CapitalInherited $s.inheritedFixture $script:Shape $script:ColdViews);coldRestoration=$probe;loadedArchive=(Copy-Value $w.archive);loadedDescriptor=(Copy-Value $r.persistenceLoad);capitalColdBoundary=[pscustomobject]@{frame=31};capitalCold=$c;inheritedCleanup=(CapitalInherited $s.inheritedCleanup $script:Shape $script:ColdViews);cleanup=$s.cleanup;elapsedSeconds=21.5;nativeShellLifecycle=[pscustomobject]@{settled=$true}}
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
# Batch198 r59: the consumer pins its acquired lineage Manual area exactly (never the capital load area).
Check 'capital consumer pins the producer departure area as its lineage area' {$p=New-Producer;$c=New-Consumer $p;if($c.request.ownedPairs.sourceArea-cne$p.request.persistenceLoad.area-or$c.request.ownedPairs.sourceArea-ceq$c.request.persistenceLoad.area){throw 'Synthetic lineage area is not the producer departure area.'};Assert-KmcChunk7OwnedPairsSelection $c.request}
$lineageAreaMutations=[ordered]@{
 'no lineage area'=@({param($s)$s.PSObject.Properties.Remove('sourceArea')},'')
 'capital lineage area'=@({param($s)$s.sourceArea=$capital},'non-capital acquired lineage')
 'zero lineage area'=@({param($s)$s.sourceArea='0'*32},'non-capital acquired lineage')
 'malformed lineage area'=@({param($s)$s.sourceArea='B10E86516B6437D45A629931C30936FE'},'non-capital acquired lineage')
 'non-string lineage area'=@({param($s)$s.sourceArea=@('b10e86516b6437d45a629931c30936fe')},'non-capital acquired lineage')
}
foreach($m in $lineageAreaMutations.GetEnumerator()){Check ('capital consumer selection refuses '+$m.Key) {$c=New-Consumer (New-Producer);&$m.Value[0] $c.request.ownedPairs;Assert-KmcChunk7OwnedPairsSelection $c.request} $true $m.Value[1]}
Check 'capital cross-process proof refuses a lineage area other than the producer departure area' {$p=New-Producer;$c=New-Consumer $p;$c.request.ownedPairs.sourceArea='c'*32;if($c.request.ownedPairs.sourceArea-ceq$p.request.persistenceLoad.area){throw 'Mutation is not distinct.'};Assert-KmcChunk7CapitalColdSource $c.request (Producer-Artifact $p) $c.artifact} $true 'producer departure area'
Check 'capital producer selection still refuses a lineage area' {$p=New-Producer;$p.request.ownedPairs|Add-Member sourceArea $p.request.persistenceLoad.area;Assert-KmcChunk7OwnedPairsSelection $p.request} $true
Check 'ordinary cold consumer selection still refuses a lineage area' {$q=Copy-Value $base.request;$q.ownedPairs|Add-Member sourceArea $q.persistenceLoad.area;Assert-KmcChunk7OwnedPairsSelection $q} $true

# Review N3/N4: the capital-specific input check is reachable, and both readers check per-actor debt
# and compare the archive only with independent references.
Check 'capital producer refuses a loaded archive already in the capital' {$p=New-Producer;$p.artifact.observations.loadedArchive.AreaId=$capital;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true 'already in the capital'
Check 'capital producer refuses a declared input already in the capital' {$p=New-Producer;$p.request.persistenceLoad.area=$capital;$p.artifact.observations.loadedDescriptor.area=$capital;$p.artifact.observations.loadedArchive.AreaId=$capital;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true 'must start outside the capital'
$debtMutations=[ordered]@{
 'producer settled debt above the archive'=@({param($o)$o.snapshots[1].actors[0].native.Standard=[double]$o.snapshots[1].actors[0].native.Standard+5},'beyond elapsed')
 'producer settled actor missing'=@({param($o)$o.snapshots[1].actors=@($o.snapshots[1].actors|Select-Object -Skip 1)},'omitted or duplicated')
 'producer settled reaction history'=@({param($o)$o.snapshots[1].actors[2].native.ReactionsRemaining=0},'reaction or surprise')
 'producer settled clock before archive'=@({param($o)$o.snapshots[1].gameTicks=[long]$o.loadedArchive.GameTimeTicks-1},'precedes')
 'producer written debt above the Manual'=@({param($o)$o.snapshots[2].actors[1].native.Move=[double]$o.snapshots[2].actors[1].native.Move+5},'beyond elapsed')
}
foreach($m in $debtMutations.GetEnumerator()){Check ('capital producer refuses '+$m.Key) {$p=New-Producer;&$m.Value[0] $p.artifact.observations;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true $m.Value[1]}
$independentMutations=[ordered]@{
 'archive owner differs from the live owner'=@({param($o)$o.snapshots[0].owners[0].ownerId='99999999999949998999999999999999'},'live restored owner')
 'archive generation differs from the live owner'=@({param($o)$o.loadedArchive.Pairs[0].Generation=7},'live restored owner')
 'archive profile differs from inherited ownership'=@({param($o)$o.loadedArchive.Pairs[0].ProfileId='small-default-halfling-horse-v1'},'scalar identity')
 'archive control index differs from the live controls'=@({param($o)$o.loadedArchive.Slots[0].Index=40},'controls differ')
 'archive policy differs'=@({param($o)$o.loadedArchive.Policy='other-policy-v1'},'exactly one mounted')
 'live controls include another actor'=@({param($o)$o.capitalCold.world.slots[1].ActorId=$roles[2]},'live controls')
 'snapshot debt above the archive'=@({param($o)$o.snapshots[0].actors[1].native.Swift=[double]$o.snapshots[0].actors[1].native.Swift+5},'beyond elapsed')
 'snapshot omits the main mount'=@({param($o)$o.snapshots[0].actors=@($o.snapshots[0].actors|Where-Object{$_.native.Id-cne$roles[1]})},'omitted or duplicated')
 'post-restore debt differs from the archive'=@({param($o)@($o.coldRestoration.events|Where-Object kind -CEQ 'restore-debt-after')[0].debt.Standard=9},'post-restore debt')
}
foreach($m in $independentMutations.GetEnumerator()){Check ('capital consumer refuses '+$m.Key) {$c=New-Consumer (New-Producer);&$m.Value[0] $c.artifact.observations;Assert-KmcChunk7CapitalColdObservation $c.request $c.artifact} $true $m.Value[1]}

# Review N2: the actual cleanup selection rule, extracted from the engine and run against stand-ins.
Check 'capital cleanup selection is exactly the original main character and its pet' {
 $engine=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.CapitalArrival.cs'))
 $start=$engine.IndexOf('private UnitEntityData[] CapitalRestoredSelection()',[StringComparison]::Ordinal);if($start-lt0){throw 'Actual capital selection rule is absent.'}
 $open=$engine.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0){if($engine[$end]-ceq'{'){$depth++}elseif($engine[$end]-ceq'}'){$depth--};$end++}
 $method=$engine.Substring($start,$end-$start)
 $source=@'
using System;using System.Linq;
namespace Kingmaker{class Ref{public UnitEntityData Value;}class Descriptor{public Ref Master=new Ref();}class UnitEntityData{public Descriptor Descriptor=new Descriptor();public bool IsDirectlyControllable=true;}class Player{public Ref MainCharacter=new Ref();}class Game{public static Game Instance=new Game();public Player Player=new Player();}}
namespace KingmakerMountedCombat.Diagnostics{using Kingmaker;
class Engine{public UnitEntityData[] originalSelection;static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
/* METHOD */
public UnitEntityData[] Run(){return CapitalRestoredSelection();}}
static class Program{static int Main(){
 var main=new UnitEntityData();var pet=new UnitEntityData();pet.Descriptor.Master.Value=main;var rider=new UnitEntityData{IsDirectlyControllable=false};var horse=new UnitEntityData{IsDirectlyControllable=false};horse.Descriptor.Master.Value=rider;var other=new UnitEntityData{IsDirectlyControllable=false};
 Game.Instance.Player.MainCharacter.Value=main;int fail=0;
 Func<UnitEntityData[],UnitEntityData[]> run=s=>new Engine{originalSelection=s}.Run();
 Func<Action,bool> refused=a=>{try{a();return false;}catch(InvalidOperationException){return true;}};
 if(!run(new[]{rider,main,horse,pet,other}).SequenceEqual(new[]{main,pet}))fail++;
 if(!run(new[]{main}).SequenceEqual(new[]{main}))fail++;
 if(run(new[]{rider,horse}).Length!=0)fail++;
 var lookalike=new UnitEntityData();if(!run(new[]{main,lookalike}).SequenceEqual(new[]{main}))fail++;
 main.IsDirectlyControllable=false;if(!refused(()=>run(new[]{main,pet})))fail++;main.IsDirectlyControllable=true;
 pet.IsDirectlyControllable=false;if(!refused(()=>run(new[]{main,pet})))fail++;pet.IsDirectlyControllable=true;
 Game.Instance.Player.MainCharacter.Value=null;if(!refused(()=>run(new[]{rider})))fail++;
 Console.WriteLine("SELECTION FAIL="+fail);return fail;}}}
'@
 $file=Join-Path $OutputDirectory 'CapitalSelection.cs';[IO.File]::WriteAllText($file,$source.Replace('/* METHOD */',$method),[Text.UTF8Encoding]::new($false))
 $vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
 $compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
 $exe=Join-Path $OutputDirectory 'CapitalSelection.exe';& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" $file|Out-Null
 if($LASTEXITCODE-ne0){throw 'Actual capital selection rule did not compile.'}
 $output=@(& $exe);if($LASTEXITCODE-ne0-or($output-join'')-cne'SELECTION FAIL=0'){throw ('Capital selection rule regressed: '+($output-join' '))}
}
# Batch196 r58: native capital mode takes the companion pair out of the scene. Each closed roster shape
# passes end to end; the row 59 companion may have no spawned view; pair B may be the Small profile.
foreach($shape in @('field','remote','absent')){
 Check ('capital rows 58-59 pass with out-of-scene shape '+$shape) {$script:Shape=$shape;try{$p=New-Producer;$c=New-Consumer $p;Assert-KmcChunk7PersistenceObservation $p.request $p.artifact 'PASS';Assert-KmcChunk7CapitalColdSource $c.request (Producer-Artifact $p) $c.artifact}finally{$script:Shape='remote'}}
}
Check 'capital row 59 passes with no spawned companion view' {$script:ColdViews=$false;try{$c=New-Consumer (New-Producer);Assert-KmcChunk7CapitalColdObservation $c.request $c.artifact}finally{$script:ColdViews=$true}}
Check 'capital rows 58-59 pass with a Small Halfling Horse pair B' {
 $pairB=@($base.artifact.observations.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]})[0];$prior=$pairB.ProfileId
 $rows=@(@($base.artifact.observations.inheritedFixture.pairs)[1],@($base.artifact.observations.inheritedCleanup.pairs)[1])
 try{$pairB.ProfileId='small-default-halfling-horse-v1';foreach($r in $rows){$r.profileId='small-default-halfling-horse-v1'}
  $p=New-Producer;$c=New-Consumer $p
  if(@($p.artifact.observations.loadedArchive.Pairs|Where-Object ProfileId -CEQ 'small-default-halfling-horse-v1').Count-ne1){throw 'Synthetic Small pair B is absent.'}
  Assert-KmcChunk7PersistenceObservation $p.request $p.artifact 'PASS';Assert-KmcChunk7PersistenceObservation $c.request $c.artifact 'PASS';Assert-KmcChunk7CapitalColdSource $c.request (Producer-Artifact $p) $c.artifact
 }finally{$pairB.ProfileId=$prior;foreach($r in $rows){$r.profileId=$prior}}
}
$outOfSceneProducer=[ordered]@{
 'companion still in the scene'=@({param($o)$o.capitalArrival.after.views[2].inGame=$true},'boolean')
 'main rider out of the scene'=@({param($o)$o.capitalArrival.after.views[0].inGame=$false},'boolean')
 'unclosed membership shape'=@({param($o)$o.capitalArrival.retiredOutOfScene.shape='detached'},'not closed')
 'record names the main pair'=@({param($o)$o.capitalArrival.retiredOutOfScene.ids=@($roles[0],$roles[1])},'companion pair')
 'record unit in the scene'=@({param($o)$o.capitalArrival.retiredOutOfScene.actors[1].inGame=$true},'boolean')
 'record without arrival views'=@({param($o)$o.capitalArrival.retiredOutOfScene.actors[0].viewPresent=$false},'boolean')
 'record claims a validated rig'=@({param($o)$o.capitalArrival.retiredOutOfScene.actors[0].liveRigValidated=$true},'boolean')
 'another actor left the field'=@({param($o)@($o.capitalArrival.after.roster|Where-Object id -CEQ $roles[1])[0].inParty=$false},'boolean')
 'companion remote under another shape'=@({param($o)@($o.capitalArrival.after.roster|Where-Object id -CEQ $roles[2])[0].remote=$false},'boolean')
 'roster omits an actor'=@({param($o)$o.capitalArrival.after.roster=@($o.capitalArrival.after.roster|Select-Object -Skip 1)},'missing or incomplete')
 'cleanup custody differs from arrival'=@({param($o)$o.inheritedCleanup.capitalOutOfScene.shape='absent';$o.inheritedCleanup.remoteCapturedIds=@()},'changed between arrival and cleanup')
 'cleanup roster keeps the companion'=@({param($o)$o.inheritedCleanup.currentPartyIds=@($o.inheritedCleanup.partyIds|Sort-Object -CaseSensitive)},'beyond the out-of-scene pair')
 'cleanup remote list differs'=@({param($o)$o.inheritedCleanup.remoteCapturedIds=@()},'remote companions')
 'admitted custody already out of scene'=@({param($o)$o.inheritedFixture|Add-Member capitalOutOfScene $o.capitalArrival.retiredOutOfScene},'lacks or invents')
}
foreach($m in $outOfSceneProducer.GetEnumerator()){Check ('capital producer refuses '+$m.Key) {$p=New-Producer;&$m.Value[0] $p.artifact.observations;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact} $true $m.Value[1]}
$outOfSceneConsumer=[ordered]@{
 'companion in the scene'=@({param($o)$o.capitalCold.world.views[3].inGame=$true},'boolean')
 'bind lacks its out-of-scene custody'=@({param($o)$o.inheritedFixture.PSObject.Properties.Remove('capitalOutOfScene')},'lacks or invents')
 'cold record differs from the bind'=@({param($o)$o.capitalCold.outOfScene.actors[0].inState=$false},'differs from its bind')
 'view presence differs from custody'=@({param($o)$o.capitalCold.world.views[2].viewId=0},'view presence')
 'cleanup custody differs from the bind'=@({param($o)$o.inheritedCleanup.capitalOutOfScene.actors[1].viewPresent=$false},'changed across execution and cleanup')
}
foreach($m in $outOfSceneConsumer.GetEnumerator()){Check ('capital consumer refuses '+$m.Key) {$c=New-Consumer (New-Producer);&$m.Value[0] $c.artifact.observations;Assert-KmcChunk7CapitalColdObservation $c.request $c.artifact} $true $m.Value[1]}
# The arrival portion of the native batch196 r58 evidence (read-only) passes the new shape check.
$r58=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261024-c7e-b196-r58-capital-arrival'
if(Test-Path -LiteralPath (Join-Path $r58 'chunk7-persistence-cleanup-pending.json')){
 $nativeRequest=Read-KmcJson (Join-Path $r58 'runtime-request.json');$native=(Read-KmcJson (Join-Path $r58 'chunk7-persistence-cleanup-pending.json')).observations
 Check 'batch196 r58 native arrival passes the observed-shape reader' {Assert-KmcChunk7CapitalArrivalShape $nativeRequest $native.capitalArrival $native.loadedArchive $native.coldRestoration.events}
 Check 'batch196 r58 native arrival with the companion in the scene is refused' {$v=Copy-Value $native.capitalArrival;$v.after.views[2].inGame=$true;Assert-KmcChunk7CapitalArrivalShape $nativeRequest $v $native.loadedArchive $native.coldRestoration.events} $true 'boolean'
 Check 'batch196 r58 native arrival restored without Mount: debt observer unchanged' {$v=Copy-Value $native.capitalArrival;$v.restoration.events+=@([pscustomobject]@{actorId=$v.after.views[0].id;kind='prepare-before'});Assert-KmcChunk7CapitalArrivalShape $nativeRequest $v $native.loadedArchive $native.coldRestoration.events} $true 'preparation'
}else{Check 'batch196 r58 native evidence is present' {throw 'Batch196 r58 evidence is absent.'}}
# Batch197 r58: native capital mode also moves the unpaired story companions to the remote party. Synthetic
# rows 58-59 pass with that admission recorded; each recorded or observed deviation refuses.
foreach($shape in @('field','remote','absent')){
 Check ('capital rows 58-59 pass with native remote story companions and out-of-scene shape '+$shape) {$script:Shape=$shape;try{With-Remote{$p=New-Producer;$c=New-Consumer $p
  if(@($p.artifact.observations.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions).Count-ne$companions.Count-or$companions.Count-lt1){throw 'Synthetic native remote companions are absent.'}
  Assert-KmcChunk7PersistenceObservation $p.request $p.artifact 'PASS';Assert-KmcChunk7PersistenceObservation $c.request $c.artifact 'PASS';Assert-KmcChunk7CapitalColdSource $c.request (Producer-Artifact $p) $c.artifact}}finally{$script:Shape='remote'}}
}
$remoteProducer=[ordered]@{
 'record omits an observed remote companion'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions=@($o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions|Select-Object -Skip 1)},'differ from the observed roster')
 'record admits a companion still in the field'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions[0].inParty=$true},'boolean')
 'record admits the main character'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions[0].id=$roles[0]},'main character')
 'record admits a paired actor'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions[0].id=$roles[1]},'paired actor')
 'record claims a paired actor'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions[0].pairMember=$true},'boolean')
 'record unnamed skip'=@({param($o)$o.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions[0].skippedChecks=@('field-party-membership')},'exactly named')
 'record without its admission field'=@({param($o)$o.capitalArrival.retiredOutOfScene.PSObject.Properties.Remove('admittedNativeRemoteCompanions')},'property set')
 'remote companion out of the game'=@({param($o)@($o.capitalArrival.after.roster|Where-Object id -CEQ $companions[0])[0].inGame=$false},'out of the game')
 'remote companion controllable'=@({param($o)@($o.capitalArrival.after.roster|Where-Object id -CEQ $companions[0])[0].directlyControllable=$true},'directly controllable')
 'main mount remote'=@({param($o)$r=@($o.capitalArrival.after.roster|Where-Object id -CEQ $roles[1])[0];$r.inParty=$false;$r.remote=$true},'paired actor')
 'main character remote'=@({param($o)$r=@($o.capitalArrival.after.roster|Where-Object id -CEQ $roles[0])[0];$r.inParty=$false;$r.character=$false;$r.remote=$true},'main character')
 'cleanup keeps a remote companion in the field'=@({param($o)$o.inheritedCleanup.currentPartyIds=@(@($o.inheritedCleanup.currentPartyIds)+$companions[0]|Sort-Object -CaseSensitive)},'admitted native remote companions')
 'cleanup remote list omits the companions'=@({param($o)$o.inheritedCleanup.remoteCapturedIds=@($roles[2])},'remote companions differ')
}
foreach($m in $remoteProducer.GetEnumerator()){Check ('capital producer with native remote companions refuses '+$m.Key) {With-Remote{$p=New-Producer;&$m.Value[0] $p.artifact.observations;Assert-KmcChunk7CapitalArrivalObservation $p.request $p.artifact}} $true $m.Value[1]}
Check 'capital cold refuses native remote companions that differ from the producer admission' {With-Remote{$p=New-Producer;$c=New-Consumer $p;$a=Producer-Artifact $p;$a.observations.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions=@();Assert-KmcChunk7CapitalColdSource $c.request $a $c.artifact}} $true 'differ from the producer admission'
Check 'capital consumer refuses a remote companion out of the game' {With-Remote{$c=New-Consumer (New-Producer);@($c.artifact.observations.capitalCold.world.roster|Where-Object id -CEQ $companions[0])[0].inGame=$false;Assert-KmcChunk7CapitalColdObservation $c.request $c.artifact}} $true 'out of the game'
Check 'product and reader name the same skipped field checks' {
 $rule=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CapitalRemoteCompanionRule.cs'))
 $listed=('SkippedChecks = { '+((Get-KmcChunk7CapitalRemoteSkippedChecks|ForEach-Object{'"'+$_+'"'})-join', ')+' };')
 if(-not$rule.Contains($listed)-or-not$rule.Contains('RecordField = "admittedNativeRemoteCompanions"')){throw 'Product skipped-check names or record field differ from the reader.'}
}
# The real batch197 r58 arrival record (read-only): the roster rule admits exactly the three story companions,
# and each mutation refuses for its own reason, both in the reader and in the compiled product rule.
$r58b197=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261024-c7e-b197-r58-capital-arrival'
if(Test-Path -LiteralPath (Join-Path $r58b197 'chunk7-persistence-cleanup-pending.json')){
 $q197=Read-KmcJson (Join-Path $r58b197 'runtime-request.json');$n197=(Read-KmcJson (Join-Path $r58b197 'chunk7-persistence-cleanup-pending.json')).observations
 $roles197=@(Get-KmcChunk7OwnedPairIds $q197);$after197=$n197.capitalArrival.after;$loaded197=$n197.loadedArchive
 $story197=@('5dd7bd1d-ae4c-4df7-9687-50fceb027344','8265f8cf-0969-4ec7-a266-2448de91c8c4','fd213210-71cd-41b0-b35b-68e1b18e9b43')
 Check 'batch197 r58 native arrival passes the observed-shape reader' {Assert-KmcChunk7CapitalArrivalShape $q197 $n197.capitalArrival $loaded197 $n197.coldRestoration.events}
 Check 'batch197 r58 roster admits exactly the three story companions' {
  $d=@(Get-KmcChunk7CapitalNativeRemoteCompanions $after197 $q197 $loaded197)
  if((@($d|ForEach-Object id)-join'|')-cne($story197-join'|')){throw ('Admitted '+(@($d|ForEach-Object id)-join','))}
  Assert-KmcChunk7CapitalRoster $after197 $q197 'remote' $d $loaded197
 }
 Check 'batch197 r58 pre-transfer roster keeps every actor in the field' {Assert-KmcChunk7CapitalRoster $n197.capitalArrival.before $q197 $null}
 $record197=@(Get-KmcChunk7CapitalNativeRemoteCompanions $after197 $q197 $loaded197)
 $roster197=[ordered]@{
  'a paired actor remote'=@({param($r)$m=@($r.roster|Where-Object id -CEQ $roles197[1])[0];$m.inParty=$false;$m.remote=$true;$m.directlyControllable=$false},'paired actor')
  'a remote companion out of the game'=@({param($r)@($r.roster|Where-Object id -CEQ $story197[0])[0].inGame=$false},'out of the game')
  'the main character remote'=@({param($r)$m=@($r.roster|Where-Object id -CEQ $roles197[0])[0];$m.inParty=$false;$m.character=$false;$m.remote=$true;$m.directlyControllable=$false},'main character')
  'an unknown new actor'=@({param($r)$r.roster=@(@($r.roster)+[pscustomobject][ordered]@{id='11111111-2222-4333-8444-555555555555';inParty=$false;character=$false;remote=$true;inGame=$true;inState=$true;directlyControllable=$false;viewPresent=$true})},'missing or incomplete')
  'the same shape outside a capital'=@({param($r)$r.capital=$false},'outside a capital')
  'the same shape in another area'=@({param($r)$r.area='0'*32},'outside a capital')
  'a story companion missing from the field and remote lists'=@({param($r)$m=@($r.roster|Where-Object id -CEQ $story197[1])[0];$m.remote=$false},'boolean')
 }
 foreach($m in $roster197.GetEnumerator()){Check ('batch197 r58 roster refuses '+$m.Key) {$v=Copy-Value $after197;&$m.Value[0] $v;Assert-KmcChunk7CapitalRoster $v $q197 'remote' $record197 $loaded197} $true $m.Value[1]}
 Check 'batch197 r58 roster refuses a record that omits a remote companion' {Assert-KmcChunk7CapitalRoster $after197 $q197 'remote' @($record197|Select-Object -Skip 1) $loaded197} $true 'differ from the observed roster'
 # The compiled product rule over the same real roster and the same mutations.
 $pairs197=@(@($roles197)+@($loaded197.Pairs|ForEach-Object{$_.Rider.Id;$_.Mount.Id}))
 function RuleCase([string]$Name,[bool]$Capital,$Rows,[string[]]$ExtraField=@()){
  $members=@(foreach($r in @($Rows|Where-Object{$_.id-cnotin@($roles197[2],$roles197[3])})){'M("'+$r.id+'",'+((@($r.inParty,$r.character,$r.remote,$r.inGame,$r.inState,$r.directlyControllable,$r.viewPresent,($r.id-ceq$q197.ownedPairs.riderAId),($r.id-cin$pairs197),($r.id-cin@($roles197[1],$roles197[3])),($r.id-cin@($roles197[0],$roles197[2])))|ForEach-Object{if($_){'true'}else{'false'}})-join',')+')'})
  $field=@(@($Rows|Where-Object inParty|ForEach-Object id)+$ExtraField|ForEach-Object{'"'+$_+'"'})
  ' Run("'+$Name+'",'+$(if($Capital){'true'}else{'false'})+',new[]{'+($members-join',')+'},new[]{"'+$roles197[2]+'","'+$roles197[3]+'"},new string[]{'+($field-join',')+'});'
 }
 function RuleRows([scriptblock]$Mutate){$v=Copy-Value $after197;if($Mutate){&$Mutate $v};@($v.roster)}
 $ruleCases=[ordered]@{
  'real'=@((RuleCase 'real' $true (RuleRows $null)),('ADMIT '+($story197-join',')))
  'paired-remote'=@((RuleCase 'paired-remote' $true (RuleRows $roster197['a paired actor remote'][0])),'ERROR A KMC paired actor moved to the remote party')
  'companion-out-of-game'=@((RuleCase 'companion-out-of-game' $true (RuleRows $roster197['a remote companion out of the game'][0])),'ERROR A remote companion is out of the game')
  'main-remote'=@((RuleCase 'main-remote' $true (RuleRows $roster197['the main character remote'][0])),'ERROR The main character moved to the remote party')
  'unknown-actor'=@((RuleCase 'unknown-actor' $true (RuleRows $null) @('11111111-2222-4333-8444-555555555555')),'ERROR Capital native field roster gained an actor outside the producer roster')
  'non-capital'=@((RuleCase 'non-capital' $false (RuleRows $null)),'ERROR Native remote companion outside a capital')
  'companion-missing'=@((RuleCase 'companion-missing' $true (RuleRows $roster197['a story companion missing from the field and remote lists'][0])),'ERROR Capital native field roster lost an actor that is not a native remote companion')
  'companion-controllable'=@((RuleCase 'companion-controllable' $true (RuleRows {param($r)@($r.roster|Where-Object id -CEQ $story197[2])[0].directlyControllable=$true})),'ERROR A remote companion is directly controllable')
  'field-actor-out-of-game'=@((RuleCase 'field-actor-out-of-game' $true (RuleRows {param($r)@($r.roster|Where-Object id -CEQ $roles197[1])[0].inGame=$false})),'ERROR Capital field actor left the scene')
 }
 $ruleSource=@'
using System;using System.Linq;
namespace KingmakerMountedCombat.Diagnostics{static class Program{
static Chunk7CapitalRemoteCompanionRule.Member M(string id,bool p,bool c,bool r,bool g,bool s,bool d,bool v,bool main,bool pair,bool pet,bool hasPet){return new Chunk7CapitalRemoteCompanionRule.Member{Id=id,InParty=p,Character=c,Remote=r,InGame=g,InState=s,DirectlyControllable=d,ViewPresent=v,MainCharacter=main,PairMember=pair,Pet=pet,HasPet=hasPet};}
static void Run(string name,bool capital,Chunk7CapitalRemoteCompanionRule.Member[] roster,string[] pairB,string[] field){
 try{Console.WriteLine(name+"|ADMIT "+string.Join(",",Chunk7CapitalRemoteCompanionRule.Admit(capital,roster,pairB,field)));}
 catch(InvalidOperationException error){Console.WriteLine(name+"|ERROR "+error.Message);}}
static int Main(){
/* CASES */
return 0;}}}
'@
 $ruleOutput=$null
 Check 'compiled product rule over the batch197 r58 roster compiles' {
  $file=Join-Path $OutputDirectory 'CapitalRemoteRule.cs';[IO.File]::WriteAllText($file,$ruleSource.Replace('/* CASES */',(@($ruleCases.Values|ForEach-Object{$_[0]})-join[char]10)),[Text.UTF8Encoding]::new($false))
  $vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
  $compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
  $exe=Join-Path $OutputDirectory 'CapitalRemoteRule.exe';& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" $file (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CapitalRemoteCompanionRule.cs')|Out-Null
  if($LASTEXITCODE-ne0){throw 'Actual capital remote companion rule did not compile.'}
  $script:ruleOutput=@(& $exe);if($LASTEXITCODE-ne0-or$script:ruleOutput.Count-ne$ruleCases.Count){throw 'Compiled capital remote companion rule did not run every case.'}
 }
 foreach($case in $ruleCases.GetEnumerator()){Check ('compiled product rule over the batch197 r58 roster: '+$case.Key) {
  $line=@($script:ruleOutput|Where-Object{$_.StartsWith($case.Key+'|',[StringComparison]::Ordinal)})
  if($line.Count-ne1){throw 'Compiled rule case is missing.'};$got=$line[0].Substring($case.Key.Length+1)
  if(-not$got.StartsWith($case.Value[1],[StringComparison]::Ordinal)){throw ('Compiled rule returned '+$got)}
 }}
}else{Check 'batch197 r58 native evidence is present' {throw 'Batch197 r58 evidence is absent.'}}
# Batch198 r59 failed closed in ReadSource: the staged c5 receipt records Spider Cave while row 59 loads in the
# capital. The real shapes: the r59 request with its lineage area pinned from the r58 record passes selection,
# and that pin, not the capital load area, is exactly the staged receipt's write and field area.
$b198=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
$b198c5=Join-Path $b198 '20261024-c7e-b198-c5-acquired-postfield/chunk7-acquired-manual-evidence.json'
$b198r58=Join-Path $b198 '20261024-c7e-b198-r58-capital-arrival/chunk7-persistence-evidence.json'
$b198r59=Join-Path $b198 '20261024-c7e-b198-r59-capital-cold/runtime-request.json'
if((Test-Path -LiteralPath $b198c5)-and(Test-Path -LiteralPath $b198r58)-and(Test-Path -LiteralPath $b198r59)){
 Check 'batch198 r59 real shapes: the r58-pinned lineage area admits the staged c5 receipt area exactly' {
  $c5=Read-KmcJson $b198c5;$a58=Read-KmcJson $b198r58;$q59=Read-KmcJson $b198r59
  if((Get-KmcSha256 $b198c5)-cne$q59.ownedPairs.sourceArtifactSha256){throw 'Batch198 c5 receipt is not the r59 staged lineage.'}
  $q59.ownedPairs|Add-Member sourceArea ([string]$a58.observations.capitalArrival.sourceArea)
  Assert-KmcChunk7OwnedPairsSelection $q59
  if($q59.ownedPairs.sourceArea-cne'b10e86516b6437d45a629931c30936fe'-or$q59.persistenceLoad.area-cne$capital){throw 'Batch198 r59 areas differ from the recorded failure.'}
  foreach($v in @($c5.observations.nativeWrite.area,$c5.observations.fieldWorld.area)){Assert-KmcBootstrapInventoryString $v $q59.ownedPairs.sourceArea;if($v-ceq$q59.persistenceLoad.area){throw 'Batch198 lineage area equals the capital load area.'}}
 }
 Check 'batch198 r59 real request without its lineage area is refused' {$q59=Read-KmcJson $b198r59;Assert-KmcChunk7OwnedPairsSelection $q59} $true
 Check 'batch198 r59 real request refuses the capital as its lineage area' {$q59=Read-KmcJson $b198r59;$q59.ownedPairs|Add-Member sourceArea $q59.persistenceLoad.area;Assert-KmcChunk7OwnedPairsSelection $q59} $true 'non-capital acquired lineage'
}else{Check 'batch198 r58/r59 native evidence is present' {throw 'Batch198 r58/r59 evidence is absent.'}}
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
