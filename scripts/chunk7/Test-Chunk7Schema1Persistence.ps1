param([string]$SourceRoot=(Join-Path $PSScriptRoot '../..'),[string]$HarnessRoot=$SourceRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $HarnessRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $SourceRoot 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
. (Join-Path $SourceRoot 'scripts/runtime/Chunk7Schema1PersistenceFixtures.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Body){$refused=$false;try{& $Body}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-Test($value){return ($value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
$case='legacy-schema1-resave-manual';$rider='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mount='d79a4f6c-b74e-4868-95bd-533899131acb'
$fixture=[pscustomobject]@{working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6';gameId='11111111-1111-4111-8111-111111111111';gameName='owned';area='a'*32};baseline=[pscustomobject]@{sha256='4'*64}}
$d=[pscustomobject]@{internalName='KMC_P01';fileName='Manual_812_KMC_P06.zks';sha256='5'*64;length=1234L;lastWriteTimeUtcTicks=1L;gameId=$fixture.working.gameId;gameName='owned';area=$fixture.working.area}
$q=[pscustomobject]@{schemaVersion=2;runId='synthetic-schema1';scenario='chunk7-persistence-load';persistenceCase=$case;branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';schema1DerivationSha256='9'*64;fixture=$fixture;persistenceLoad=$d}
function Native($Id,$Debt){[pscustomobject]@{Id=$Id;Standard=[double]$Debt;Move=0.5;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0L}}
$old=[pscustomobject]@{SchemaVersion=1;CampaignId=$fixture.working.gameId;AreaId=$fixture.working.area;GameTimeTicks=10000000L;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Mounted=$true;ProfileId='medium-humanoid-mammoth-v1';Rider=(Native $rider 2);Mount=(Native $mount 4);Slots=@([pscustomobject]@{ActorId=$rider;Index=12;Kind=2},[pscustomobject]@{ActorId=$rider;Index=13;Kind=3})}
$owner=Get-KmcChunk7Schema1Owner $old
$pair=[pscustomobject]@{PairId=$owner;Generation=1L;ProfileId=$old.ProfileId;Rider=$old.Rider;Mount=$old.Mount}
$archive=[pscustomobject]@{SchemaVersion=3;CampaignId=$old.CampaignId;AreaId=$old.AreaId;GameTimeTicks=$old.GameTimeTicks;Policy=$old.Policy;RulesId=$old.RulesId;Pairs=@($pair);Participations=@();Slots=$old.Slots;Combat=$null}
$owners=@([pscustomobject]@{ownerId=$owner;generation=1L;riderId=$rider;mountId=$mount;state='Active'})
$armed=[pscustomobject]@{OwnerId=$owner;Generation=1L;MountedPairId=$owner;State=[pscustomobject]@{RiderId=$rider;MountId=$mount;Activation=$null;BoundaryIsCurrent=$false;Boundary=$null;Partner=$null;SplitReleaseRound=-1;PendingSplitId=$null;PendingSplitRound=-1;RenewalNotBeforeTicks=0L}}
$world=[pscustomobject]@{gameTicks=$old.GameTimeTicks;turnBased=$false;playerInCombat=$false;actors=@([pscustomobject]@{Native=$old.Rider;InCombat=$false;Prepared=$false},[pscustomobject]@{Native=$old.Mount;InCombat=$false;Prepared=$false});combat=$null;participations=@($armed);owners=$owners;nativeControls=$old.Slots}
$hooks=@([pscustomobject]@{declaringType='TurnBased.Controllers.TurnController';method='Prepare';token='06000C3C';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{declaringType='Kingmaker.Controllers.Combat.UnitCombatState';method='OnNewRound';token='0600939D';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){$hooks+=[pscustomobject]@{declaringType=$(if($method-cin@('RestoreActor','RestoreDebt')){'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'}else{'KingmakerMountedCombat.Integration.MountedPersistenceService'});method=$method;token='06001000';moduleMvid=$q.dllMvid}}
$events=@();foreach($actor in @($old.Rider,$old.Mount)){foreach($kind in @('restore-debt-before','restore-debt-after')){$events+=[pscustomobject]@{kind=$kind;actorId=$actor.Id;frame=1;gameTicks=$old.GameTimeTicks;restorePhase='validated-before-native-mutation';restorationPending=$false;debt=$actor;prepared=$false;inCombat=$false}}}
$events+=[pscustomobject]@{kind='all-owner-presentation-restored';frame=2;restorePhase='all-pair-presentation-restored';snapshot=$world}
$probe=[pscustomobject]@{contract='chunk7-native-restoration-entry-observer-v1';events=$events;hooks=$hooks;maximumEvents=2048;dropped=0;errors=0;complete=$true;installedBeforeWorldLoad=$true;prepareMetric='native-method-entries-including-production-refused-attempts'}
$snapshot=[pscustomobject]@{label='schema1-native-restored-before-resave';owners=$owners;actors=@([pscustomobject]@{native=$old.Rider;prepared=$false;inCombat=$false},[pscustomobject]@{native=$old.Mount;prepared=$false;inCombat=$false});gameTicks=$old.GameTimeTicks;controls=[pscustomobject]@{DuplicateFactCount=0;SerializationSuspended=$false;ManagedHotbarSlotCount=2};semanticRestores=2;presentationRestores=1;nativePostLoads=2;snapshots=0;nativeCastRequests=0;archivePhase='all-pair-presentation-restored'}
$after=Copy-Test $snapshot;$after.label='schema1-native-resave-complete';$after.snapshots=1
$o=[pscustomobject]@{checkpoint=$case;cold=$true;sourceWorkingSha256=$fixture.working.sha256;processId=123;nativeSchema=3;nativeCapacity=2;nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;combatDebtProof=$false;inputs=@();movements=@();loadedArchive=$archive;loadedDescriptor=$d;snapshots=@($snapshot,$after);coldRestoration=$probe;
 schema1Migration=[pscustomobject]@{inputSchema=1;derivationReceiptSha256=$q.schema1DerivationSha256;nativeInputMetadata=$old;migratedArchive=$archive;restoredWorld=$world;resavedWorld=$world;inputSha256=$d.sha256;inputLength=$d.length};
 nativeWrite=[pscustomobject]@{path='synthetic-output';sha256='6'*64;length=1334L;nativeType='Manual';nativeName='KMC_C7_SCHEMA1';nativeCallback=$true;sourceSchema=3;operation='None';snapshotCount=1;archive=$archive};
 saveRequested=[pscustomobject]@{nativeType='Manual';nativeName='KMC_C7_SCHEMA1';gameTicks=$old.GameTimeTicks;snapshotCount=0;nativeSaveAllowed=$true;newSlot=$true};
 cleanup=[pscustomobject]@{pairsClear=$true;sourceHorseRemoved=$true;controlsSettled=$true;saveCustodyClear=$true;selectionRestored=$true;nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;world=[pscustomobject]@{owners=@()}}}
$a=[ordered]@{schemaVersion=1;evidenceKind='chunk7-persistence-evidence';createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$o};foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$a[$field]=$q.$field};$good=Copy-Test ([pscustomobject]$a)
Check 'complete exact schema-1 migration/native resave synthetic evidence' {Assert-KmcChunk7PersistenceObservation $q $good 'PASS'}
Check 'exact derived descriptor and distinct output leaf' {Assert-KmcChunk7PersistenceLoadDescriptor $q;if((Get-KmcChunk7PersistenceLeaf $case)-cne'Manual_813_KMC_C7_SCHEMA1.zks'){throw 'Wrong native output leaf.'}}
foreach($mutation in @('schema2-input','dropped-slot','new-owner','resave-owner','activation','pending','context','two-owners','combat','prepare','foreign-prepare','round','full-restore','missing-debt','wrong-debt','prepared-grant','early-presentation','missing-hook','foreign-hook','late-observer','no-native-write','wrong-writer','wrong-name','callback','write-twice','replayed-mount','input-hash','receipt-hash','baseline-alias','debt-refund','cleanup')){
 Reject $mutation {$x=Copy-Test $good;$r=Copy-Test $q;$v=$x.observations;switch($mutation){
 'schema2-input'{$v.schema1Migration.nativeInputMetadata.SchemaVersion=2}
 'dropped-slot'{$v.nativeWrite.archive.Slots=@($v.nativeWrite.archive.Slots[0])}
 'new-owner'{$v.schema1Migration.migratedArchive.Pairs[0].PairId='a'*32}
 'resave-owner'{$v.nativeWrite.archive.Pairs[0].PairId='a'*32}
 'activation'{$v.schema1Migration.restoredWorld.participations[0].State.Activation=[pscustomobject]@{Sequence=1}}
 'pending'{$v.schema1Migration.restoredWorld.participations[0].State.PendingSplitId=$mount}
 'context'{$v.schema1Migration.restoredWorld.participations[0].State.Boundary=[pscustomobject]@{Id=1}}
 'two-owners'{$v.schema1Migration.restoredWorld.owners+=Copy-Test $owners[0]}
 'combat'{$v.nativeWrite.archive.Combat=[pscustomobject]@{Round=1}}
 'prepare'{$v.coldRestoration.events+=[pscustomobject]@{kind='prepare-before';actorId=$rider}}
 'foreign-prepare'{$v.coldRestoration.events+=[pscustomobject]@{kind='prepare-before';actorId='99999999-9999-4999-8999-999999999999'}}
 'round'{$v.coldRestoration.events+=[pscustomobject]@{kind='round-before';actorId=$mount}}
 'full-restore'{$v.coldRestoration.events+=[pscustomobject]@{kind='restore-actor-before';actorId=$mount}}
 'missing-debt'{$v.coldRestoration.events=@($v.coldRestoration.events|Select-Object -Skip 1)}
 'wrong-debt'{$v.coldRestoration.events[1].debt.Standard=0.0}
 'prepared-grant'{$v.coldRestoration.events[1].prepared=$true}
 'early-presentation'{$v.coldRestoration.events=@($v.coldRestoration.events[4]) + @($v.coldRestoration.events|Select-Object -First 4)}
 'missing-hook'{$v.coldRestoration.hooks=@($v.coldRestoration.hooks|Select-Object -First 5)}
 'foreign-hook'{$v.coldRestoration.hooks[4].moduleMvid='99999999-9999-4999-8999-999999999999'}
 'late-observer'{$v.coldRestoration.installedBeforeWorldLoad=$false}
 'no-native-write'{$v.nativeWrite=$null}
 'wrong-writer'{$v.nativeWrite.sourceSchema=2}
 'wrong-name'{$v.nativeWrite.nativeName='KMC_P01'}
 'callback'{$v.nativeWrite.nativeCallback=$false}
 'write-twice'{$v.snapshots[1].snapshots=2}
 'replayed-mount'{$v.snapshots[0].nativeCastRequests=1}
 'input-hash'{$v.schema1Migration.inputSha256='0'*64}
 'receipt-hash'{$v.schema1Migration.derivationReceiptSha256='0'*64}
 'baseline-alias'{$r.persistenceLoad.sha256=$r.fixture.baseline.sha256;Assert-KmcChunk7PersistenceLoadDescriptor $r}
 'debt-refund'{$v.nativeWrite.archive.Pairs[0].Rider.Standard=0.0}
 'cleanup'{$v.cleanup.saveCustodyClear=$false}
 };Assert-KmcChunk7PersistenceObservation $r $x 'PASS'}
}
foreach($path in @('nativeSchema','nativeCapacity','processId','schema1Migration.inputSchema','schema1Migration.inputLength','nativeWrite.sourceSchema','nativeWrite.snapshotCount','nativeWrite.length','snapshots.0.semanticRestores','snapshots.0.controls.DuplicateFactCount','snapshots.0.controls.ManagedHotbarSlotCount','coldRestoration.maximumEvents','coldRestoration.dropped','coldRestoration.errors')){
 foreach($wrong in @('1',$true,1.25,-1)) {Reject ('noninteger counter '+$path+' '+$wrong.GetType().Name){$x=Copy-Test $good;$parts=$path.Split('.');$at=$x.observations;for($i=0;$i-lt$parts.Count-1;$i++){if($at-is[Array]){$at=$at[[int]$parts[$i]]}else{$at=$at.($parts[$i])}};$at.($parts[-1])=$wrong;Assert-KmcChunk7PersistenceObservation $q $x 'PASS'}}
}
$receipt=[pscustomobject]@{schemaVersion=1;purpose='P06 offline metadata-only validation copy';case='schema1';sourceRun='qualified-prior';sourceSha256='a'*64;foreignSourceRun=$null;foreignSha256=$null;descriptor=$d;nativeMembersVerified=1;members=[pscustomobject]@{'header.json'=('b'*64);'kmc-mounted-state'=('c'*64)}}
Check 'unchanged P06 derivative receipt accepted' {Assert-KmcChunk7Schema1Derivation $receipt $d 'qualified-prior' ('a'*64)}
foreach($field in @('case','sourceRun','sourceSha256','foreignSha256','nativeMembersVerified','descriptor','members')){Reject ('changed P06 provenance '+$field){$x=Copy-Test $receipt;switch($field){'case'{$x.case='legacy'};'sourceRun'{$x.sourceRun='another'};'sourceSha256'{$x.sourceSha256='d'*64};'foreignSha256'{$x.foreignSha256='e'*64};'nativeMembersVerified'{$x.nativeMembersVerified='1'};'descriptor'{$x.descriptor.sha256='f'*64};'members'{$x.members.PSObject.Properties.Remove('header.json')}};Assert-KmcChunk7Schema1Derivation $x $d 'qualified-prior' ('a'*64)}}
# Actual immutable artifact dispatch; substitute only manifest/file I/O boundaries.
$archiveChecks=[Collections.Generic.List[object]]::new();$manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-persistence-evidence.json';kind='chunk7-persistence-evidence'})}
$boundArtifact=Copy-Test $good
$boundArtifact.observations.nativeWrite.path=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$q.runId+'/Saved Games/Manual_813_KMC_C7_SCHEMA1.zks')
function Read-KmcChunk7ManifestArtifact {param($Request,$Manifest,$Leaf,$Kind);return $boundArtifact}
function Assert-KmcNotReparsePoint {param($Path,$Description)}
function Assert-KmcNotHardLink {param($Path,$Description)}
function Assert-KmcChargeArchiveSnapshot {param($Path,$Hash,$Length,$Metadata);$archiveChecks.Add([pscustomobject]@{path=$Path;hash=$Hash;length=$Length;metadata=$Metadata})}
Check 'schema-1 cold resave uses existing exact native output snapshot helper' {
 Assert-KmcChunk7PersistenceArtifact $q $manifest 'PASS'
 if($archiveChecks.Count-ne1-or$archiveChecks[0].path-cne$boundArtifact.observations.nativeWrite.path-or$archiveChecks[0].hash-cne('6'*64)-or
    (ConvertTo-KmcChunk7Canonical $archiveChecks[0].metadata)-cne(ConvertTo-KmcChunk7Canonical $archive)){throw 'Native output validation helper lost exact metadata/leaf/hash.'}
}
Reject 'native output cannot overwrite read-only schema1 input' {$oldPath=$boundArtifact.observations.nativeWrite.path;try{$boundArtifact.observations.nativeWrite.path=$oldPath.Replace('Manual_813_KMC_C7_SCHEMA1.zks','Manual_812_KMC_P06.zks');Assert-KmcChunk7PersistenceArtifact $q $manifest 'PASS'}finally{$boundArtifact.observations.nativeWrite.path=$oldPath}}
# Actual wrapper, external qualified helper and filesystem substitutions only.
$helperCalls=[Collections.Generic.List[string]]::new();$qualifierRefusal=$false;$changedOriginal=$false
function Get-KmcPersistenceValidationSource($SourceRunId,$ExpectedSha256,$Fixture,$Case){$helperCalls.Add('qualify:'+ $Case);if($qualifierRefusal){throw 'qualified historical source unavailable'};return [pscustomobject]@{path='original-synthetic';descriptor=$d}}
function New-KmcPersistenceValidationCopy($SourceRunId,$ExpectedSha256,$Fixture,$Case){$helperCalls.Add('derive:'+ $Case);return [pscustomobject]@{path='C:/synthetic/Manual_812_KMC_P06.zks';descriptor=$d}}
function Assert-KmcNotReparsePoint {param($Path,$Description)}
function Assert-KmcNotHardLink {param($Path,$Description)}
function Assert-KmcManifestJsonMembersUnique {param($Path,$Description)}
function Read-KmcJson {param($Path);return $receipt}
function Get-KmcSha256 {param($Path);if($Path-ceq'original-synthetic'){if($changedOriginal){return ('0'*64)};return ('a'*64)};if($Path.EndsWith('owner.json')){return ('9'*64)};return $d.sha256}
Check 'source wrapper calls only qualified P06 schema1 helpers' {$v=New-KmcChunk7Schema1Input 'qualified-prior' ('a'*64) $fixture;if(($helperCalls-join',')-cne'qualify:schema1,derive:schema1'-or$v.descriptor.fileName-cne'Manual_812_KMC_P06.zks'-or$v.receiptSha256-cne('9'*64)){throw 'Wrong derivative adapter calls.'}}
Reject 'qualifier refusal precedes derivative' {$helperCalls.Clear();$qualifierRefusal=$true;try{New-KmcChunk7Schema1Input 'qualified-prior' ('a'*64) $fixture}finally{$qualifierRefusal=$false;if(($helperCalls-join',')-cne'qualify:schema1'){throw 'Derivative ran after refused source.'}}}
Reject 'original source outer hash changed' {$changedOriginal=$true;try{New-KmcChunk7Schema1Input 'qualified-prior' ('a'*64) $fixture}finally{$changedOriginal=$false}}
Write-Host ('CHUNK7 SCHEMA1 PASS='+$passed+' FAIL=0; synthetic evidence/helper contracts only; no actual save/archive access or native claim.')
