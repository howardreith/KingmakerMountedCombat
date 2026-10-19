Set-StrictMode -Version Latest
function Test-KmcChunk7LoadRefusal($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-load-refusal'}
function Assert-KmcChunk7LoadRefusalSelection($Request,$Selection,$Checkpoint){
 if($Selection.case-cne'owner-seed-load-refusal'-or$Checkpoint-isnot[string]-or$Checkpoint-cnotin@('multi-malformed-refusal-manual','multi-future-refusal-manual')){throw 'Refusal selection and checkpoint are exact and closed.'}
 # The same immutable two-pair producer/accepted-consumer lineage and quotas;
 # this validates data only, not native combat or load permission.
 Assert-KmcChunk7OwnerSeedDerivedCombatSelection $Request $Selection $null
}
function Assert-KmcChunk7LoadRefusalDescriptor($Request){
 if(-not(Test-KmcChunk7LoadRefusal $Request)){throw 'Refusal alternate belongs only to its exact selection.'}
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 $p=$Request.persistenceLoad;$d=$Request.persistenceAlternate
 Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'refusal alternate descriptor'
 Assert-KmcBootstrapInventoryString $d.fileName 'Manual_812_KMC_C7_SEED_REFUSAL.zks'
 foreach($key in @('internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $d.$key $p.$key}
 foreach($key in @('sha256')){if($d.$key-isnot[string]-or$d.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Refusal alternate hash is invalid.'}}
 if($d.sha256-ceq$p.sha256-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or$d.sha256-ceq$Request.ownedPairs.seedSha256-or$d.fileName-ceq$p.fileName){throw 'Refusal alternate aliases a protected or primary archive.'}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
 if($Request.loadRefusalDerivationSha256-isnot[string]-or$Request.loadRefusalDerivationSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Refusal derivative provenance hash is required.'}
}
function Assert-KmcChunk7LoadRefusalDerivation($Receipt,$Request){
 Assert-KmcChunk7LoadRefusalDescriptor $Request
 Assert-KmcExactProperties $Receipt @('schemaVersion','contract','checkpoint','sourceRunId','sourceArtifactSha256','sourceDescriptor','descriptor','mutation','metadataSha256','metadataLength','nativeMembersVerified','sourceMembers','members','fixtureSetupOnly','nativeRestorationProven') 'refusal metadata derivation'
 Assert-KmcOrdinaryInteger $Receipt.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $Receipt.contract 'owner-seed-load-refusal-metadata-copy-v1'
 Assert-KmcBootstrapInventoryString $Receipt.checkpoint $Request.persistenceCase;Assert-KmcBootstrapInventoryString $Receipt.sourceRunId $Request.ownedPairs.sourceRunId;Assert-KmcBootstrapInventoryString $Receipt.sourceArtifactSha256 $Request.ownedPairs.sourceArtifactSha256
 Assert-KmcChunk7Boolean $Receipt.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $Receipt.nativeRestorationProven $false
 if((ConvertTo-KmcChunk7Canonical $Receipt.sourceDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or(ConvertTo-KmcChunk7Canonical $Receipt.descriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceAlternate)){throw 'Refusal receipt substituted primary or alternate identity.'}
 $mutation=if($Request.persistenceCase-ceq'multi-malformed-refusal-manual'){'truncated-json'}else{'future-schema-99'}
 Assert-KmcBootstrapInventoryString $Receipt.mutation $mutation
 foreach($set in @($Receipt.sourceMembers,$Receipt.members)){
  if($set-isnot[Array]-or$set.Count-lt2-or$set.Count-gt1024){throw 'Refusal receipt member array is unbounded.'}
  $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase);[long]$total=0
  foreach($row in $set){
   Assert-KmcExactProperties $row @('name','length','sha256') 'refusal archive member'
   if($row.name-isnot[string]-or[string]::IsNullOrEmpty($row.name)-or$row.name.Length-gt256-or$row.name-match'[:\\\x00-\x1f\x7f]'-or$row.name.StartsWith('/')-or-not$names.Add($row.name)-or@($row.name.TrimEnd('/').Split('/')|Where-Object{$_-cin@('','..','.')}).Count-ne0){throw 'Refusal receipt member is unsafe or ambiguous.'}
   Assert-KmcOrdinaryInteger $row.length 0 268435456;$total+=$row.length;if($total-gt512MB){throw 'Refusal receipt expanded bytes are unbounded.'}
   if($row.sha256-isnot[string]-or$row.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Refusal receipt member hash is invalid.'}
  }
  foreach($leaf in @('header.json','kmc-mounted-state')){if(@($set|Where-Object name -CEQ $leaf).Count-ne1){throw 'Refusal receipt lacks its exact header or metadata.'}}
 }
 Assert-KmcOwnedSchema1MemberPreservation $Receipt.sourceMembers $Receipt.members
 Assert-KmcOrdinaryInteger $Receipt.nativeMembersVerified ($Receipt.members.Count-1) ($Receipt.members.Count-1)
 $meta=@($Receipt.members|Where-Object name -CEQ 'kmc-mounted-state')[0];$prior=@($Receipt.sourceMembers|Where-Object name -CEQ 'kmc-mounted-state')[0]
 Assert-KmcBootstrapInventoryString $Receipt.metadataSha256 $meta.sha256;Assert-KmcOrdinaryInteger $Receipt.metadataLength 1 131072
 if($Receipt.metadataLength-ne$meta.length-or$meta.sha256-ceq$prior.sha256){throw 'Refusal mutation did not change exactly its declared metadata.'}
}
function Assert-KmcChunk7LoadRefusalInputBytes($Request,[string]$SaveRoot,[string]$ReceiptPath){
 Assert-KmcChunk7LoadRefusalDescriptor $Request
 $receipt=Read-KmcOwnedSchema1PinnedJson $ReceiptPath $Request.loadRefusalDerivationSha256 256KB
 Assert-KmcChunk7LoadRefusalDerivation $receipt $Request
 $observed=@()
 foreach($d in @($Request.persistenceLoad,$Request.persistenceAlternate)){
  $path=Join-Path $SaveRoot $d.fileName;Assert-KmcNotReparsePoint $path 'refusal immutable input';Assert-KmcNotHardLink $path 'refusal immutable input'
  $file=Get-Item -LiteralPath $path
  if($file.Length-ne$d.length-or$file.LastWriteTimeUtc.Ticks-ne$d.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$d.sha256){throw 'Refusal native input changed bytes or metadata.'}
  $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  try{$observed+=Get-KmcChunk7LoadRefusalMembers $stream}finally{$stream.Dispose()}
  if((Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$d.lastWriteTimeUtcTicks){throw 'Refusal input changed during member inspection.'}
 }
 if((ConvertTo-KmcChunk7Canonical $observed[0].members)-cne(ConvertTo-KmcChunk7Canonical $receipt.sourceMembers)-or(ConvertTo-KmcChunk7Canonical $observed[1].members)-cne(ConvertTo-KmcChunk7Canonical $receipt.members)){throw 'Refusal member receipt differs from the actual ZIP inputs.'}
 Assert-KmcJsonObjectMembersUnique -Json $observed[0].metadataText -Description 'refusal primary mounted metadata'
 $mutation=Get-KmcChunk7LoadRefusalMutation ($observed[0].metadataText|ConvertFrom-Json) $Request.persistenceCase
 if($mutation.mutation-cne$receipt.mutation-or$mutation.text-cne$observed[1].metadataText){throw 'Refusal input is not the exact declared metadata-only mutation.'}
}

function Assert-KmcChunk7LoadRefusalProbe($Probe,$Baseline=$null){
 Assert-KmcExactProperties $Probe @('contract','installed','capacity','blueprintFailureCount','dropped','unresolvedBlueprintIds','failedTypeBindCount','failedTypeBinds','loggedExceptionCount','loggedExceptions','hooks','observerFaultCount','observerFaults') 'refusal native resolution observer'
 Assert-KmcBootstrapInventoryString $Probe.contract 'owner-seed-native-load-observation-v1';Assert-KmcChunk7Boolean $Probe.installed $true;Assert-KmcOrdinaryInteger $Probe.capacity 256 256
 foreach($field in @('dropped','observerFaultCount')){Assert-KmcOrdinaryInteger $Probe.$field 0 0}
 foreach($field in @('blueprintFailureCount','failedTypeBindCount','loggedExceptionCount')){Assert-KmcOrdinaryInteger $Probe.$field 0 1000000}
 foreach($field in @('unresolvedBlueprintIds','failedTypeBinds','loggedExceptions','observerFaults')){if($Probe.$field-isnot[Array]-or$Probe.$field.Count-gt256){throw 'Refusal observer list is missing or exceeds its capacity.'}}
 if($Probe.observerFaults.Count-ne0-or$Probe.loggedExceptions.Count-ne$Probe.loggedExceptionCount-or$Probe.unresolvedBlueprintIds.Count-gt$Probe.blueprintFailureCount-or$Probe.failedTypeBinds.Count-gt$Probe.failedTypeBindCount){throw 'Refusal observer counts do not bind their captured records.'}
 if($Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne4){throw 'Refusal native observer hook inventory is incomplete.'}
 $expected=@(@('Kingmaker.Blueprints.ResourcesLibrary','TryGetBlueprint','06009708','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('Newtonsoft.Json.Serialization.DefaultSerializationBinder','BindToType','060004C8','4f213745-d60e-425f-b763-df319c826ddf'),@('UberDebug','LogException','06000664','57f03756-55de-42f5-8bb3-e983306082b2'),@('UberDebug','LogExceptionChannel','06000665','57f03756-55de-42f5-8bb3-e983306082b2'))
 for($i=0;$i-lt4;$i++){$hook=$Probe.hooks[$i];Assert-KmcExactProperties $hook @('declaringType','method','token','moduleMvid') 'refusal native observer hook';$keys=@('declaringType','method','token','moduleMvid');for($k=0;$k-lt4;$k++){Assert-KmcBootstrapInventoryString $hook.($keys[$k]) $expected[$i][$k]}}
 if($null-ne$Baseline-and(ConvertTo-KmcChunk7Canonical $Probe)-cne(ConvertTo-KmcChunk7Canonical $Baseline)){throw 'Refusal or menu refresh produced a new native exception, resolution failure or observer change.'}
}

function Assert-KmcChunk7LoadRefusalState($Request,$State,$Archive,[string]$Label,[int]$Rejected){
 Assert-KmcExactProperties $State @('world','paused','selection','objects','loadedArchive','slots','party','nativePrepares','rejectedLoads','worldDisposals','nativeLoads','nativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','loadProbe') 'refusal native state'
 Assert-KmcChunk7Boolean $State.paused $true;Assert-KmcOrdinaryInteger $State.rejectedLoads $Rejected $Rejected
 foreach($field in @('nativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $State.$field 1 1}
 foreach($field in @('worldDisposals','nativeWrites','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations')){Assert-KmcOrdinaryInteger $State.$field 0 0}
 Assert-KmcChunk7PersistenceSnapshot $State.world $Archive $Label
 foreach($row in @(@('semanticRestores',4),@('presentationRestores',2),@('nativePostLoads',4),@('snapshots',0),@('nativeCastRequests',0))){Assert-KmcOrdinaryInteger $State.world.($row[0]) $row[1] $row[1]}
 Assert-KmcBootstrapInventoryString $State.world.archivePhase 'all-pair-presentation-restored'
 if((ConvertTo-KmcChunk7Canonical $State.loadedArchive)-cne(ConvertTo-KmcChunk7Canonical $Archive)-or(ConvertTo-KmcChunk7Canonical $State.slots)-cne(ConvertTo-KmcChunk7Canonical $Archive.Slots)){throw 'Refusal changed the complete loaded graph or actual native slot bindings.'}
 $roles=Get-KmcChunk7OwnedPairIds $Request
 if($State.selection-isnot[Array]-or$State.selection.Count-gt$Request.ownedPairs.partyIds.Count-or@($State.selection|Select-Object -Unique).Count-ne$State.selection.Count-or@($State.selection|Where-Object{$_-cnotin$Request.ownedPairs.partyIds}).Count-ne0){throw 'Refusal native selection is foreign or duplicated.'}
 Assert-KmcExactProperties $State.objects @('player','state','areaState','area','loadedArchive','actors') 'refusal native object identities'
 $labels=[Collections.Generic.HashSet[int]]::new()
 foreach($field in @('player','state','areaState','area','loadedArchive')){Assert-KmcOrdinaryInteger $State.objects.$field 1 128;if(-not$labels.Add([int]$State.objects.$field)){throw 'Refusal native world object identities alias.'}}
 if($State.objects.actors-isnot[Array]-or$State.objects.actors.Count-ne4){throw 'Refusal native actor identities are incomplete.'}
 Assert-KmcChunk7ExactIds $State.objects.actors $roles 'id'
 foreach($row in $State.objects.actors){Assert-KmcExactProperties $row @('id','object') 'refusal native actor object';Assert-KmcOrdinaryInteger $row.object 1 128;if(-not$labels.Add([int]$row.object)){throw 'Refusal actor objects alias another retained native object.'}}
 if($State.party-isnot[Array]-or$State.party.Count-ne$Request.ownedPairs.partyIds.Count){throw 'Refusal omitted an unrelated native field actor.'}
 Assert-KmcChunk7ExactIds @($State.party|ForEach-Object{$_.native}) $Request.ownedPairs.partyIds 'Id'
 foreach($row in $State.party){Assert-KmcExactProperties $row @('native','prepared','inCombat') 'refusal native party';Assert-KmcChunk7PersistenceNativeActor $row.native;if($row.prepared-isnot[bool]){throw 'Refusal prepared observation must retain its actual native boolean.'};Assert-KmcChunk7Boolean $row.inCombat $false}
 if($State.nativePrepares-isnot[Array]-or$State.nativePrepares.Count-ne4){throw 'Refusal native preparation entry counts are incomplete.'}
 Assert-KmcChunk7ExactIds $State.nativePrepares $roles 'actorId'
 foreach($row in $State.nativePrepares){Assert-KmcExactProperties $row @('actorId','count') 'refusal actual native preparation entries';Assert-KmcOrdinaryInteger $row.count 0 0}
 Assert-KmcChunk7LoadRefusalProbe $State.loadProbe
}

function Assert-KmcChunk7LoadRefusalConservation($Baseline,$State){
 foreach($field in @('paused','selection','objects','loadedArchive','slots','party','nativePrepares','worldDisposals','nativeLoads','nativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations')){if((ConvertTo-KmcChunk7Canonical $Baseline.$field)-cne(ConvertTo-KmcChunk7Canonical $State.$field)){throw ('Refusal or native menu refresh changed '+$field+'.')}}
 $a=$Baseline.world|ConvertTo-Json -Depth 40|ConvertFrom-Json;$b=$State.world|ConvertTo-Json -Depth 40|ConvertFrom-Json
 foreach($value in @($a,$b)){$value.PSObject.Properties.Remove('label');$value.PSObject.Properties.Remove('frame')}
 if((ConvertTo-KmcChunk7Canonical $a)-cne(ConvertTo-KmcChunk7Canonical $b)){throw 'Refusal or native menu refresh changed clock, preparation, actor debt or mounted custody.'}
 Assert-KmcChunk7LoadRefusalProbe $State.loadProbe $Baseline.loadProbe
}

function Assert-KmcChunk7LoadRefusalObservation($Request,$Artifact){
 Assert-KmcChunk7LoadRefusalDescriptor $Request
 $o=$Artifact.observations;$r=$o.loadRefusal;$archive=$o.loadedArchive
 Assert-KmcExactProperties $r @('contract','derivationSha256','derivation','fixturePauseBefore','baseline','entries','alternateReadKind','complete','menuRefresh','finalProbe') 'real native load refusal'
 Assert-KmcBootstrapInventoryString $r.contract 'owner-seed-real-entry-load-refusal-v1';Assert-KmcBootstrapInventoryString $r.derivationSha256 $Request.loadRefusalDerivationSha256
 Assert-KmcChunk7Boolean $r.complete $true;if($r.fixturePauseBefore-isnot[bool]){throw 'Refusal fixture pause provenance is missing.'}
 $kind=if($Request.persistenceCase-ceq'multi-malformed-refusal-manual'){'Invalid'}else{'Future'};Assert-KmcBootstrapInventoryString $r.alternateReadKind $kind
 Assert-KmcChunk7LoadRefusalDerivation $r.derivation $Request
 foreach($field in @('nativeWrite','saveRequested','ownedOutputAdmission','ownedOutputCompletion','schema1Migration','schema2Migration')){if($null-ne$o.PSObject.Properties[$field]){throw 'Load-refusal supplied unexpected writing or migration evidence.'}}
 Assert-KmcChunk7LoadRefusalState $Request $r.baseline $archive 'refusal-baseline' 0
 if($r.entries-isnot[Array]-or$r.entries.Count-ne3){throw 'Exactly three real native load-entry refusals are required.'}
 $names=@('Game.LoadGameFromMainMenu','Game.LoadGame','Game.LoadGameForSmokeTest')
 for($i=0;$i-lt3;$i++){
  $entry=$r.entries[$i];Assert-KmcExactProperties $entry @('entry','before','after','rejectedDelta','earlyIdentityCount','realRejectedDecisionCount') 'actual native load refusal entry'
  Assert-KmcBootstrapInventoryString $entry.entry $names[$i]
  foreach($field in @('rejectedDelta','earlyIdentityCount','realRejectedDecisionCount')){Assert-KmcOrdinaryInteger $entry.$field ($i+1) ($i+1)}
  Assert-KmcChunk7LoadRefusalState $Request $entry.before $archive 'entry-before' $i;Assert-KmcChunk7LoadRefusalState $Request $entry.after $archive 'entry-after' ($i+1)
  Assert-KmcChunk7LoadRefusalConservation $r.baseline $entry.before;Assert-KmcChunk7LoadRefusalConservation $r.baseline $entry.after
 }
 $refresh=$r.menuRefresh;Assert-KmcExactProperties $refresh @('before','dispatchFrame','after','completeFrame','areSavesUpToDate') 'actual native save-list refresh'
 Assert-KmcOrdinaryInteger $refresh.dispatchFrame 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $refresh.completeFrame 1 ([int]::MaxValue);Assert-KmcChunk7Boolean $refresh.areSavesUpToDate $true
 if($refresh.completeFrame-lt$refresh.dispatchFrame+3-or$refresh.before.world.frame-ne$refresh.dispatchFrame-or$refresh.after.world.frame-ne$refresh.completeFrame){throw 'Save-list refresh lacks the actual bounded later-frame completion.'}
 Assert-KmcChunk7LoadRefusalState $Request $refresh.before $archive 'menu-refresh-before' 3;Assert-KmcChunk7LoadRefusalState $Request $refresh.after $archive 'menu-refresh-complete' 3
 Assert-KmcChunk7LoadRefusalConservation $r.baseline $refresh.before;Assert-KmcChunk7LoadRefusalConservation $r.baseline $refresh.after
 Assert-KmcChunk7LoadRefusalProbe $r.finalProbe $r.baseline.loadProbe
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId);$saves=Join-Path $profile 'Saved Games'
 Assert-KmcChunk7LoadRefusalInputBytes $Request $saves (Join-Path $profile 'owner-seed-load-refusal-derivation.json')
 $stream=[IO.File]::Open((Join-Path $saves $Request.persistenceLoad.fileName),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{$actual=Get-KmcChunk7LoadRefusalMembers $stream}finally{$stream.Dispose()}
 if((ConvertTo-KmcChunk7Canonical ($actual.metadataText|ConvertFrom-Json))-cne(ConvertTo-KmcChunk7Canonical $archive)){throw 'Refusal loaded graph differs from its actual immutable primary ZIP metadata.'}
 $inventory=@(Get-ChildItem -LiteralPath $saves -Force)
 if($inventory.Count-ne2-or@($inventory|Where-Object{$_.PSIsContainer-or$_.Name-cnotin@($Request.persistenceLoad.fileName,$Request.persistenceAlternate.fileName)}).Count-ne0){throw 'Refusal save-list inventory differs from its exact two immutable inputs.'}
}

# Definitions only: the common result readers also inspect exact native ZIP
# members and mutation bytes; these helpers perform no work merely by loading.
. (Join-Path $PSScriptRoot 'Chunk7LoadRefusalFixtures.ps1')
