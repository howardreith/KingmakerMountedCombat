# Native observation boundaries are synthetic; collector envelope/rows come from actual compiled code.
function Invoke-MatchedPoolReaderChecks([string]$ActualCollector,[string]$OutputRoot) {
 $script:matchedChecks=0
 function Copy-Matched($v){$v|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json}
 function Check-Matched([string]$name,[scriptblock]$body){& $body;$script:matchedChecks++;Write-Host ('PASS '+$name)}
 function Reject-Matched([string]$name,[scriptblock]$body){$refused=$false;try{& $body}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$name)};$script:matchedChecks++;Write-Host ('PASS refuses '+$name)}
 $ids=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7','77777777-7777-4777-8777-777777777777')
 $pins=@(1000..1021)
 $request=[pscustomobject]@{scenario='chunk7-two-pair-exploration';runId='synthetic-matched-pool';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';evidenceRoot=$OutputRoot}
 function Artifact([string]$kind,$observations){
  $v=[ordered]@{schemaVersion=1;evidenceKind=$kind;createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$observations}
  foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$v[$field]=$request.$field}
  return [pscustomobject]$v
 }
 $subs=[pscustomobject]@{assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';executing=$false;uniqueOwnedSubscribers=1;ownedInterfaceEntries=1;entries=@([pscustomobject]@{interface='native-interface';type='KmcSubscriber';identity=-77})}
 # Owner records carry the producer's registry key (MountedPairRegistry.Key: Guid "N"); actor ids are Guid "D".
 $keys=@($ids|ForEach-Object{[Guid]::ParseExact($_,'D').ToString('N')})
 $owners=@([pscustomobject]@{ownerId='11111111-1111-4111-8111-111111111111';riderId=$keys[0];mountId=$keys[1];generation=3000000001L;state='Active';relationshipState='Mounted';retainsObligations=$false;saveReady=$true;teardownReady=$false},
  [pscustomobject]@{ownerId='22222222-2222-4222-8222-222222222222';riderId=$keys[2];mountId=$keys[3];generation=3000000002L;state='Active';relationshipState='Mounted';retainsObligations=$false;saveReady=$true;teardownReady=$false})
 $active=@(0,1,2,0)
 function Boundary([int]$index,[bool]$after){
  $frame=1000+500*$index;if($after){$frame+=151}
  $ticks=100000000L+50000000L*$index;if($after){$ticks+=15000000L}
  $liveOwners=@();if($active[$index]-ge1){$liveOwners+=Copy-Matched $owners[0]};if($active[$index]-eq2){$liveOwners+=Copy-Matched $owners[1]}
  [pscustomobject]@{identityPins=@($pins);actorIds=@($ids);expectedActivePairs=$active[$index];commandsSettled=$true;preparationPending=$false;relationshipTransition=$false;serializationBusy=$false;pendingNativeWrites=$false;allocationActors=4;
   world=[pscustomobject]@{label='matched-pool-boundary';frame=$frame;selection=@($ids[0]);resources=[pscustomobject]@{gameTicks=$ticks;actors=@($ids|ForEach-Object{[pscustomobject]@{id=$_;move=0.0;standard=0.0;swift=0.0;prepared=$false;reactionAllowance=3;initiative=0;reactions=1}})};
    boundaryCount=2;participantCount=2;subscriptions=(Copy-Matched $subs);owners=@($liveOwners)}}
 }
 function Sample([int]$index,[string]$label,$before,$after){
  $ownership=[pscustomobject]@{boundaries=2;failedConstructions=0;owners=$active[$index];active=$active[$index];allocationActors=4;participants=2;preparedOrViewResidue=$active[$index];liveControlBoundaries=0}
  [pscustomobject]@{label=$label;warmupFrames=30;sampleFrames=120;startFrame=($before.world.frame+31);endFrame=$after.world.frame;stopwatchFrequency=1000000L;
   serviceTicks=@(1..120|ForEach-Object{100L});frameSeconds=@(1..120|ForEach-Object{0.01});serviceMeanMs=0.1;serviceP50Ms=0.1;serviceP95Ms=0.1;frameP50Ms=10.0;frameP95Ms=10.0;
   managedHeapBefore=100000L;managedHeapAfter=99000L;managedHeapDelta=-1000L;forcedCollection=$false;collectionsBefore=@(1,1,1);collectionsAfter=@(2,1,1);kmcLogMessages=2L;kmcLogCharacters=30L;
   ownersBefore=(Copy-Matched $ownership);ownersAfter=(Copy-Matched $ownership);subscriptionsBefore=(Copy-Matched $subs);subscriptionsAfter=(Copy-Matched $subs);limits='Synthetic native boundary only; no performance proof.'}
 }
 $o=Get-Content -Raw -LiteralPath $ActualCollector|ConvertFrom-Json
 for($i=0;$i-lt4;$i++){$row=$o.rows[$i];$row.before=Boundary $i $false;$row.after=Boundary $i $true;$row.sample=Sample $i $row.label $row.before $row.after}
 $party=@();for($i=0;$i-lt4;$i++){$party+=[pscustomobject]@{id=$ids[$i];object=$pins[10+3*$i];masterId=$(if($i-eq3){$ids[2]}else{$null})}}
 $cycles=@();for($i=0;$i-lt3;$i++){$cycles+=[pscustomobject]@{label=('cycle-'+($i+1)+'-mounted');frame=(500+1000*$i);owners=@($owners|ForEach-Object{Copy-Matched $_});subscriptions=(Copy-Matched $subs)};$cycles+=[pscustomobject]@{label=('cycle-'+($i+1)+'-settled');frame=(900+800*$i);owners=@();subscriptions=(Copy-Matched $subs)}}
 $cycles[2].frame=1999;$cycles[3].frame=2499
 $world=Artifact 'chunk7-world-evidence' ([pscustomobject]@{partyWithHorse=$party;cycles=$cycles;cycleInputs=@(1..12);snapshots=@([pscustomobject]@{label='cycle-2-mount-mammoth-settled';frame=1499;owners=@((Copy-Matched $owners[0]));subscriptions=(Copy-Matched $subs)})})
 $worldPath=Join-Path $OutputRoot 'chunk7-world-evidence.json'
 [IO.File]::WriteAllText($worldPath,($world|ConvertTo-Json -Depth 90 -Compress),[Text.UTF8Encoding]::new($false))
 $worldHash=(Get-FileHash -LiteralPath $worldPath).Hash.ToLowerInvariant()
 $o|Add-Member worldEvidenceSha256 $worldHash
 $artifact=Artifact 'chunk7-matched-pool-measurements' $o
 Check-Matched 'actual collector shape accepts matched native external boundaries, Int64 generations and naturally decreasing heap occupancy' {Assert-KmcMatchedPoolEvidence $request $artifact $world $worldHash 'PASS'}
 Check-Matched 'elapsed native cooldown decay is permitted without refund' {$v=Copy-Matched $artifact;$v.observations.rows[0].before.world.resources.actors[0].standard=3.0;$v.observations.rows[0].after.world.resources.actors[0].standard=1.5;Assert-KmcMatchedPoolEvidence $request $v $world $worldHash 'PASS'}
 Check-Matched 'signed native reference identities remain valid' {$v=Copy-Matched $artifact;$w=Copy-Matched $world;foreach($row in $v.observations.rows){foreach($side in @($row.before,$row.after)){for($j=0;$j-lt22;$j++){$side.identityPins[$j]=-1*$side.identityPins[$j]}}};foreach($p in $w.observations.partyWithHorse){$p.object=-1*$p.object};Assert-KmcMatchedPoolEvidence $request $v $w $worldHash 'PASS'}
 $mutations=[ordered]@{
  'missing version'={param($v)$v.observations.contract='legacy'}
  'invented performance acceptance'={param($v)$v.observations.performanceAccepted=$true}
  'invented allocation rate'={param($v)$v.observations.managedAllocationRateMeasured=$true}
  'fully inert diagnostic claim'={param($v)$v.observations.fullyInertDiagnostics=$true}
  'text boolean'={param($v)$v.observations.observerReleased='true'}
  'array contract'={param($v)$v.observations.contract=@($v.observations.contract)}
  'extra native input'={param($v)$v.observations.additionalNativeInputs=1}
  'wrong warmup cycle'={param($v)$v.observations.warmupNativeCycle=0}
  'missing settled final window'={param($v)$v.observations.rows=@($v.observations.rows[0..2])}
  'reordered window'={param($v)$v.observations.rows[1].index=2}
  'array active count'={param($v)$v.observations.rows[1].activePairs=@(1)}
  'one resident boundary'={param($v)$v.observations.rows[0].sample.ownersBefore.boundaries=1}
  'retained extra owner'={param($v)$v.observations.rows[0].sample.ownersAfter.owners=1}
  'pending control'={param($v)$v.observations.rows[2].sample.ownersBefore.liveControlBoundaries=1}
  'unexpected prepared/view residue'={param($v)$v.observations.rows[3].sample.ownersAfter.preparedOrViewResidue=1}
  'failed service construction'={param($v)$v.observations.rows[0].sample.ownersBefore.failedConstructions=1}
  'changed participant object'={param($v)$v.observations.rows[1].before.identityPins[7]=9001}
  'changed actor view'={param($v)$v.observations.rows[2].after.identityPins[21]=9002}
  'null native identity'={param($v)$v.observations.rows[0].before.identityPins[0]=0}
  'aliased native roles'={param($v)$v.observations.rows[0].before.identityPins[7]=$v.observations.rows[0].before.identityPins[9]}
  'object identity overflow'={param($v)$v.observations.rows[0].before.identityPins[0]=2147483648L}
  'array actor identity'={param($v)$v.observations.rows[0].before.actorIds[0]=@($v.observations.rows[0].before.actorIds[0])}
  'unfinished commands'={param($v)$v.observations.rows[0].before.commandsSettled=$false}
  'pending native preparation'={param($v)$v.observations.rows[0].before.preparationPending=$true}
  'pending relationship transition'={param($v)$v.observations.rows[0].before.relationshipTransition=$true}
  'pending native writer'={param($v)$v.observations.rows[0].before.pendingNativeWrites=$true}
  'native serialization'={param($v)$v.observations.rows[0].before.serializationBusy=$true}
  'changed allocation history cardinality'={param($v)$v.observations.rows[3].before.allocationActors=0}
  'owner-generation substitution'={param($v)$v.observations.rows[1].after.world.owners[0].generation++}
  'conflicting duplicate owner'={param($v)$v.observations.rows[2].after.world.owners[1]=Copy-Matched $v.observations.rows[2].after.world.owners[0]}
  'owner rider in actor D form'={param($v)$v.observations.rows[1].after.world.owners[0].riderId=$ids[0]}
  'owner rider uppercase N'={param($v)$v.observations.rows[1].after.world.owners[0].riderId=$keys[0].ToUpperInvariant()}
  'string owner-generation'={param($v)$v.observations.rows[1].after.world.owners[0].generation=[string]$v.observations.rows[1].after.world.owners[0].generation}
  'double service ticks'={param($v)$v.observations.rows[0].sample.serviceTicks[0]=[double]100}
  'short sample'={param($v)$v.observations.rows[0].sample.sampleFrames=119}
  'nonfinite frame duration'={param($v)$v.observations.rows[0].sample.frameSeconds[0]=[double]::NaN}
  'derived timing mismatch'={param($v)$v.observations.rows[0].sample.serviceP95Ms=100.0}
  'heap arithmetic mismatch'={param($v)$v.observations.rows[0].sample.managedHeapDelta=1000L}
  'forced GC'={param($v)$v.observations.rows[0].sample.forcedCollection=$true}
  'backwards GC observation'={param($v)$v.observations.rows[0].sample.collectionsAfter[0]=0}
  'string log counter'={param($v)$v.observations.rows[0].sample.kmcLogMessages='2'}
  'timer outside boundary'={param($v)$v.observations.rows[0].sample.endFrame++}
  'changed native subscription'={param($v)$v.observations.rows[0].after.world.subscriptions.entries[0].identity++}
  'timer subscription from foreign native assembly'={param($v)$v.observations.rows[0].sample.subscriptionsBefore.assemblyMvid='other'}
  'array subscriber type'={param($v)$v.observations.rows[0].sample.subscriptionsBefore.entries[0].type=@('KmcSubscriber')}
  'array resource actor'={param($v)$v.observations.rows[0].before.world.resources.actors[0].id=@($v.observations.rows[0].before.world.resources.actors[0].id)}
  'resource refund'={param($v)$v.observations.rows[0].after.world.resources.actors[0].standard=1.0}
  'reaction spending'={param($v)$v.observations.rows[0].after.world.resources.actors[0].reactions++}
  'reaction allowance change'={param($v)$v.observations.rows[0].after.world.resources.actors[0].reactionAllowance++}
  'prepared initiative change'={param($v)$v.observations.rows[0].after.world.resources.actors[0].prepared=$true}
  'selection change during idle sample'={param($v)$v.observations.rows[0].after.world.selection=@($ids[2])}
  'changed original artifact hash'={param($v)$v.observations.worldEvidenceSha256='0'*64}
  'incomplete observer release'={param($v)$v.observations.observerReleased=$false}
  'aborted window in PASS'={param($v)$v.observations.abortedWindow=[pscustomobject]@{index=3}}
 }
 foreach($name in $mutations.Keys){$v=Copy-Matched $artifact;& $mutations[$name] $v;Reject-Matched $name {Assert-KmcMatchedPoolEvidence $request $v $world $worldHash 'PASS'}}
 Reject-Matched 'old native cycle count changed' {$w=Copy-Matched $world;$w.observations.cycleInputs=@(1..13);Assert-KmcMatchedPoolEvidence $request $artifact $w $worldHash 'PASS'}
 Reject-Matched 'warmup native subscription changed' {$w=Copy-Matched $world;$w.observations.cycles[1].subscriptions.entries[0].identity++;Assert-KmcMatchedPoolEvidence $request $artifact $w $worldHash 'PASS'}
 Check-Matched 'failed partial observations preserve failure without promotion' {$v=Copy-Matched $artifact;$v.status='FAIL';$v.errors=@('native custody changed');$v.observations.rows=@();Assert-KmcMatchedPoolEvidence $request $v $world $worldHash 'FAIL'}
 Reject-Matched 'failed observation attached to PASS' {$v=Copy-Matched $artifact;$v.status='FAIL';$v.errors=@('native custody changed');Assert-KmcMatchedPoolEvidence $request $v $world $worldHash 'PASS'}
 $leaf='chunk7-matched-pool-measurements.json';$path=Join-Path $OutputRoot $leaf
 [IO.File]::WriteAllText($path,($artifact|ConvertTo-Json -Depth 90 -Compress),[Text.UTF8Encoding]::new($false))
 $manifest=[pscustomobject]@{schemaVersion=1;runId=$request.runId;scenario=$request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$leaf;kind='chunk7-matched-pool-measurements';sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();length=(Get-Item -LiteralPath $path).Length},[pscustomobject]@{relativePath='chunk7-world-evidence.json';kind='chunk7-world-evidence';sha256=$worldHash;length=(Get-Item -LiteralPath $worldPath).Length})}
 function Validate-SharedManifest($value){$file=Join-Path $OutputRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($file,($value|ConvertTo-Json -Depth 90 -Compress),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $request (Get-FileHash -LiteralPath $file).Hash.ToLowerInvariant()}
 Check-Matched 'shared exact manifest allowlist admits only the selected sidecar role' {Validate-SharedManifest $manifest}
 Reject-Matched 'shared manifest refuses a similar unselected sidecar role' {$m=Copy-Matched $manifest;$m.artifacts[0].relativePath='chunk7-matched-pool-other.json';Validate-SharedManifest $m}
 Reject-Matched 'shared manifest refuses mismatched sidecar kind' {$m=Copy-Matched $manifest;$m.artifacts[0].kind='chunk7-world-evidence';Validate-SharedManifest $m}
 Reject-Matched 'shared manifest refuses sidecar length mismatch' {$m=Copy-Matched $manifest;$m.artifacts[0].length++;Validate-SharedManifest $m}
 Validate-SharedManifest $manifest
 Check-Matched 'exact immutable sidecar and original artifact bytes are bound' {Assert-KmcMatchedPoolArtifact $request $manifest 'PASS'}
 Reject-Matched 'sidecar missing from successful run' {$m=Copy-Matched $manifest;$m.artifacts=@($m.artifacts[1]);Assert-KmcMatchedPoolArtifact $request $m 'PASS'}
 Check-Matched 'early failed run has no invented sidecar' {$m=Copy-Matched $manifest;$m.artifacts=@($m.artifacts[1]);Assert-KmcMatchedPoolArtifact $request $m 'FAIL'}
 Reject-Matched 'sidecar from another scenario' {$r=Copy-Matched $request;$r.scenario='chunk7-persistence-save';Assert-KmcMatchedPoolArtifact $r $manifest 'FAIL'}
 Reject-Matched 'duplicate manifest role' {$m=Copy-Matched $manifest;$m.artifacts+=Copy-Matched $m.artifacts[0];Assert-KmcMatchedPoolArtifact $request $m 'PASS'}
 Reject-Matched 'wrong exact artifact kind' {$m=Copy-Matched $manifest;$m.artifacts[0].kind='other';Assert-KmcMatchedPoolArtifact $request $m 'PASS'}
 Reject-Matched 'changed immutable artifact bytes' {$m=Copy-Matched $manifest;$m.artifacts[0].sha256='0'*64;Assert-KmcMatchedPoolArtifact $request $m 'PASS'}
 Write-Host ('MATCHED POOL READER PASS='+$script:matchedChecks+' FAIL=0; synthetic native observations, no archive/gameplay/performance proof')
}
