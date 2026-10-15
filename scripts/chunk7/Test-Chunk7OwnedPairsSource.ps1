param([string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'};if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $repo 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($repo))) ('analysis-cache/chunk7-owned-source-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$testOutput=$OutputDirectory;$testFixtures=$FixtureDirectory
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew();$script:virtualLab=Join-Path $testOutput 'virtual-lab'
# Execute the actual new qualifier. Only OS file observation, the established
# full ancestor validator and strict manifest/output validators are substituted.
# No archive file is created, opened or decoded. JSON fixtures remain ordinary files.
function Get-KmcLabRoot{$script:virtualLab}
function Boundary([string]$Name){$script:calls.Add($Name);if($script:failure-ceq$Name){throw ('external boundary refused: '+$Name)}}
function Assert-KmcChildPath($Path,$Parent,$Description){$p=[IO.Path]::GetFullPath($Path);$q=[IO.Path]::GetFullPath($Parent).TrimEnd('\')+'\';if(-not$p.StartsWith($q,[StringComparison]::OrdinalIgnoreCase)){throw 'foreign path'};$p}
function Assert-KmcDirectoryTreeCloneable($Path,$Description){Boundary 'cloneable'}
function Assert-KmcNotReparsePoint($Path,$Description){Boundary 'not-reparse'}
function Assert-KmcNotHardLink($Path,$Description){Boundary 'not-hardlink'}
function Assert-KmcManifestJsonMembersUnique($Path,$Description){Boundary 'unique-json'}
function Read-KmcJson($Path){if(-not$script:documents.ContainsKey($Path)){throw ('unadmitted receipt '+$Path)};$script:documents[$Path]}
function Get-KmcSha256($Path){if(-not$script:hashes.ContainsKey($Path)){throw ('unadmitted hash '+$Path)};if(-not$script:readCounts.ContainsKey($Path)){$script:readCounts[$Path]=0};$script:readCounts[$Path]++;if($script:drift-ceq$Path-and$script:readCounts[$Path]-gt1){return ('0'*64)};$script:hashes[$Path]}
function Get-Item([string]$LiteralPath){if(-not$script:hashes.ContainsKey($LiteralPath)){throw 'unknown virtual metadata'};[pscustomobject]@{Length=[long]100;LastWriteTimeUtc=[DateTime]::new(638960000000000000,[DateTimeKind]::Utc)}}
function Get-KmcChunk7AcquiredManualSource($Selection,$Fixture,$Candidate){Boundary 'complete-same-candidate-lineage';[pscustomobject]@{descriptor=$script:parentInput;bootstrapRunId='bootstrap';coldInventoryRunId='cold';manualRunId='manual'}}
function Assert-KmcReadOnlyArtifactManifest($Request,$Hash,[switch]$GameResult){Boundary 'strict-manifest';if($Hash-cne('b'*64)-or-not$GameResult){throw 'manifest bound incorrectly'}}
function Assert-KmcChunk7AcquiredManualArtifact($Request,$Manifest,$Status){Boundary 'strict-two-owner-artifact';if($Status-cne'PASS'){throw 'source failure misclassified'}}
function Assert-KmcChunk7AcquiredManualOutput($Request,$Artifact){Boundary 'settled-native-manual-output'}
function Assert-KmcChunk7PersistenceArtifact($Request,$Manifest,$Status){Boundary 'strict-checkpoint-artifact';if($Status-cne'PASS'){throw 'source failure misclassified'}}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,$Leaf,$Kind){if($Leaf-cne($Kind+'.json')-or$Kind-cnotin@('chunk7-acquired-manual-evidence','chunk7-persistence-evidence')){throw 'foreign artifact role'};return $script:documents[(Join-Path $Request.evidenceRoot $Leaf)]}
function Add-Set($Request,$Artifact,[string]$ArtifactLeaf,[string]$ArtifactHash,[string]$SourceHash,$PersistenceCase){
 $run=$Request.runId;$root=Join-Path $script:virtualLab ('runtime-evidence/'+$run);$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$run);$Request.evidenceRoot=$root
 $result=[ordered]@{status='PASS';errors=@();evidenceManifestSha256=('b'*64);gameResultSha256=('c'*64);restoredSaveInventoryDigest=('d'*64);modsRestored=$true;workingRestored=$true;baselineImmutable=$true;saveProtectionPassed=$true;saveWriteAllowlistPassed=$true;assertionFailCount=0;subscenarioFailCount=0;baselineLoadRequestCount=0;unauthorizedLoadRequestCount=0;unauthorizedSaveRequestCount=0;loadRequestCount=1;workingLoadRequestCount=1;saveRequestCount=1;workingSaveRequestCount=1;fixtureIdentityVerified=$true}
 foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$result[$key]=$Request.$key};$result=[pscustomobject]$result;$game=$result|ConvertTo-Json -Depth 20|ConvertFrom-Json
 $isolation=[pscustomobject]@{runId=$run;stagingMode='base-game-only-kmc';stageVerified=$true;dependenciesUnchanged=$true;processExited=$true;modsRestored=$true;saveProtectionPassed=$true;gameReportedPass=$true;nativeInventoryObserved=$true;nativeCompatibilityQualified=$true}
 $owner=[pscustomobject]@{runId=$run;scenario=$Request.scenario;transactionToken=$Request.transactionToken;sourceSha256=$SourceHash;saveRoot=(Join-Path $profile 'Saved Games');persistenceCase=$PersistenceCase}
 foreach($p in @(@('runtime-request.json',$Request),@('runtime-result.json',$result),@('runtime-game-result.json',$game),@('base-game-isolation-result.json',$isolation),@('runtime-artifacts.json',[pscustomobject]@{artifacts=@()}),@($ArtifactLeaf,$Artifact))){$script:documents[(Join-Path $root $p[0])]=$p[1]};$script:documents[(Join-Path $profile 'owner.json')]=$owner
 foreach($p in $script:documents.Keys){if(-not$script:hashes.ContainsKey($p)){$script:hashes[$p]='e'*64}};$script:hashes[(Join-Path $root 'runtime-artifacts.json')]='b'*64;$script:hashes[(Join-Path $root 'runtime-game-result.json')]='c'*64;$script:hashes[(Join-Path $root $ArtifactLeaf)]=$ArtifactHash
 [pscustomobject]@{request=$Request;artifact=$Artifact;root=$root;profile=$profile;owner=$owner;game=$game;result=$result;isolation=$isolation}
}
function New-Case([bool]$Paid=$false,[bool]$Cold=$false){
 $script:calls=[Collections.Generic.List[string]]::new();$script:failure=$null;$script:drift=$null;$script:documents=@{};$script:hashes=@{};$script:readCounts=@{}
 $tag=if($Paid){'Paid'}else{'Existing'};$f=[IO.File]::ReadAllText((Join-Path $testFixtures ('TwoOwnerManual'+$tag+'Fixture.json')))|ConvertFrom-Json;$r=$f.request;$a=$f.artifact;$w=$a.observations.nativeWrite
 $s=[pscustomobject]@{case='exploration';sourceRunId=$r.runId;sourceArtifactSha256=('6'*64);sourceArchiveSha256=$w.sha256;checkpointSourceRunId=$null;checkpointSourceArtifactSha256=$null;riderAId=$r.acquiredManual.priorOwnerId;mountAId=$r.acquiredManual.priorPetId;riderBId=$r.acquiredManual.actorId;mountBId=$r.acquiredManual.petId;partyIds=@($a.observations.fieldWorld.party|ForEach-Object id);baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $script:parentInput=$r.persistenceLoad|ConvertTo-Json -Depth 20|ConvertFrom-Json
 $parent=Add-Set $r $a 'chunk7-acquired-manual-evidence.json' $s.sourceArtifactSha256 $r.acquiredManual.sourceArchiveSha256 $null
 $w.path=Join-Path (Join-Path $parent.profile 'Saved Games') $w.fileName;$script:hashes[$w.path]=$w.sha256
 $candidate=$r|ConvertTo-Json -Depth 20|ConvertFrom-Json
 $case=[pscustomobject]@{selection=$s;fixture=$r.fixture;candidate=$candidate;parent=$parent;checkpoint=$null;persistenceCase=$null}
 if($Cold){
  $s.case='persistence-load';$s.checkpointSourceRunId='synthetic-owned-save';$s.checkpointSourceArtifactSha256='8'*64;$case.persistenceCase='multi-exploration-manual'
  $sourceSelection=$s|ConvertTo-Json -Depth 20|ConvertFrom-Json;$sourceSelection.case='persistence-save';$sourceSelection.expectedNativeWriteCount=1;$sourceSelection.checkpointSourceRunId=$null;$sourceSelection.checkpointSourceArtifactSha256=$null
  $descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
  $sourceRequest=[ordered]@{scenario='chunk7-owned-two-pair-campaign';runId=$s.checkpointSourceRunId;evidenceRoot='';fixture=$r.fixture;ownedPairs=$sourceSelection;persistenceCase=$case.persistenceCase;persistenceLoad=$descriptor}
  foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$sourceRequest[$key]=$r.$key};$sourceRequest=[pscustomobject]$sourceRequest
  $n=[int]([regex]::Match($w.fileName,'^Manual_([0-9]+)_').Groups[1].Value)+1;$leaf='Manual_'+$n+'_KMC_C7_WORLD.zks'
  $artifact=[pscustomobject]@{observations=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=$n;leaf=$leaf};nativeWrite=[pscustomobject]@{path='';nativeName='KMC_C7_WORLD';nativeType='Manual';sha256=('9'*64);length=1234;archive=[pscustomobject]@{AreaId=$w.area}}}}
  $checkpoint=Add-Set $sourceRequest $artifact 'chunk7-persistence-evidence.json' $s.checkpointSourceArtifactSha256 $s.sourceArchiveSha256 $case.persistenceCase;$artifact.observations.nativeWrite.path=Join-Path (Join-Path $checkpoint.profile 'Saved Games') $leaf;$script:hashes[$artifact.observations.nativeWrite.path]='9'*64;$case.checkpoint=$checkpoint
 };return $case
}
function Qualify($C){Get-KmcChunk7OwnedPairsSource $C.selection $C.persistenceCase $C.fixture $C.candidate}
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){$errorText=$null;$pass=$false;try{&$Body;$pass=-not$Refuse}catch{$errorText=$_.Exception.Message;$pass=$Refuse-and(-not$Pattern-or$errorText-match$Pattern)};$results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Refuse;error=$errorText});if(-not$pass){Write-Output ('FAIL '+$Name+': '+$errorText)}}
foreach($paid in @($false,$true)){foreach($cold in @($false,$true)){Check ('qualified exact source '+$paid+'/'+$cold) {$c=New-Case $paid $cold;$v=Qualify $c;if($v.descriptor.sha256-cne$(if($cold){'9'*64}else{$c.selection.sourceArchiveSha256})-or$v.artifactSha256-cne$c.selection.sourceArtifactSha256){throw 'returned descriptor/lineage differs'};foreach($gate in @('complete-same-candidate-lineage','strict-manifest','strict-two-owner-artifact','settled-native-manual-output')){if(-not$script:calls.Contains($gate)){throw ('required guard absent '+$gate)}};if($cold-and-not$script:calls.Contains('strict-checkpoint-artifact')){throw 'cold output skipped actual artifact guard'}}}}
foreach($gate in @('cloneable','not-reparse','not-hardlink','unique-json','complete-same-candidate-lineage','strict-manifest','strict-two-owner-artifact','settled-native-manual-output','strict-checkpoint-artifact')){Check ('required boundary '+$gate){$c=New-Case $false $true;$script:failure=$gate;[void](Qualify $c)} $true 'external boundary refused'}
$mutations=[ordered]@{
 'candidate SHA'={param($c)$c.candidate.dllSha256='0'*64}
 'candidate MVID'={param($c)$c.candidate.dllMvid='11111111-1111-4111-8111-111111111111'}
 'source status FAIL'={param($c)$c.parent.result.status='FAIL'}
 'source errors'={param($c)$c.parent.game.errors=@('failed')}
 'Mods not restored'={param($c)$c.parent.result.modsRestored=$false}
 'protected save not restored'={param($c)$c.parent.result.saveProtectionPassed=$false}
 'source second load'={param($c)$c.parent.game.loadRequestCount=2}
 'source unauthorized write'={param($c)$c.parent.game.unauthorizedSaveRequestCount=1}
 'source native identity absent'={param($c)$c.parent.game.fixtureIdentityVerified=$false}
 'base-only not qualified'={param($c)$c.parent.isolation.nativeCompatibilityQualified=$false}
 'foreign owner token'={param($c)$c.parent.owner.transactionToken='0'*64}
 'foreign scenario'={param($c)$c.parent.request.scenario='chunk7-manual-seed-derivative'}
 'one-owner case not enough'={param($c)$c.parent.request.acquiredManual.case='preserve-acquired-field'}
 'wrong prior rider'={param($c)$c.selection.riderAId='aaaaaaaa-1111-4111-8111-111111111111';$c.selection.partyIds[0]=$c.selection.riderAId}
 'wrong new pet'={param($c)$c.selection.mountBId='aaaaaaaa-2222-4222-8222-222222222222';$c.selection.partyIds[3]=$c.selection.mountBId}
 'partial full roster'={param($c)$c.selection.partyIds+=@('aaaaaaaa-3333-4333-8333-333333333333')}
 'changed manual content pin'={param($c)$c.parent.artifact.observations.nativeWrite.sha256='0'*64}
 'wrong metadata name'={param($c)$c.parent.artifact.observations.nativeWrite.internalName='KMC_AUTOMATION_WORKING'}
 'foreign source path receipt'={param($c)$c.parent.owner.saveRoot='C:\foreign'}
 'unbound result body'={param($c)$c.parent.result.gameResultSha256='0'*64}
 'receipt drift'={param($c)$script:drift=Join-Path $c.parent.root 'runtime-result.json'}
}
foreach($m in $mutations.GetEnumerator()){Check $m.Key {$c=New-Case;&$m.Value $c;[void](Qualify $c)} $true}
$coldMutations=[ordered]@{
 'checkpoint case differs'={param($c)$c.checkpoint.request.persistenceCase='multi-tb-debt-manual'}
 'checkpoint not source'={param($c)$c.checkpoint.request.ownedPairs.case='persistence-load'}
 'checkpoint changes ancestor'={param($c)$c.checkpoint.request.ownedPairs.sourceArtifactSha256='0'*64}
 'checkpoint changes input descriptor'={param($c)$c.checkpoint.request.persistenceLoad.length++}
 'checkpoint unfinished'={param($c)$c.checkpoint.result.status='FAIL'}
 'checkpoint second write'={param($c)$c.checkpoint.game.saveRequestCount=2}
 'checkpoint wrong artifact'={param($c)$c.selection.checkpointSourceArtifactSha256='0'*64}
 'checkpoint invents next slot'={param($c)$c.checkpoint.artifact.observations.ownedOutputAdmission.number=99}
}
foreach($m in $coldMutations.GetEnumerator()){Check $m.Key {$c=New-Case $false $true;&$m.Value $c;[void](Qualify $c)} $true}

foreach($paid in @($false,$true)){foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual')){
 Check ('owned replay maps only to qualified exploration Manual '+$paid+'/'+$case){$c=New-Case $paid $true;$c.persistenceCase=$case;$c.selection.expectedNativeLoadCount=if($case-ceq'multi-exploration-three-loads-manual'){3}else{1};$v=Qualify $c;if($v.descriptor.sha256-cne('9'*64)-or$v.checkpointArtifactSha256-cne$c.selection.checkpointSourceArtifactSha256-or$c.checkpoint.request.persistenceCase-cne'multi-exploration-manual'-or$c.selection.expectedNativeLoadCount-ne$(if($case-ceq'multi-exploration-three-loads-manual'){3}else{1})){throw 'Replay qualification changed parent identity or real selection quota'};foreach($gate in @('complete-same-candidate-lineage','strict-manifest','strict-two-owner-artifact','settled-native-manual-output','strict-checkpoint-artifact')){if(-not$script:calls.Contains($gate)){throw ('replay omitted original gate '+$gate)}}}
 foreach($wrong in @('multi-tb-debt-manual','multi-exploration-quick',$case)){Check ('replay refuses differently selected source '+$paid+'/'+$case+'/'+$wrong){$c=New-Case $paid $true;$c.persistenceCase=$case;$c.selection.expectedNativeLoadCount=if($case-ceq'multi-exploration-three-loads-manual'){3}else{1};$c.checkpoint.request.persistenceCase=$wrong;$c.checkpoint.owner.persistenceCase=$wrong;[void](Qualify $c)} $true}
}}
# New owned-area lineage uses the same exact qualifier with its established
# native artifact/ancestor boundaries still explicitly substituted above.
function Add-AreaProfile($Set){$p=Join-Path $Set.root 'profile-cache-changes.json';$v=[pscustomobject]@{schemaVersion=1;runId=$Set.request.runId;policy='native-achievement-cache-settled-size';snapshotProfileDigest=('f'*64);profileBytesRestoredExactly=$true;acceptedNativeCacheChanges=@()};$script:documents[$p]=$v;$script:hashes[$p]='a'*64;return $v}
function New-AreaSource([bool]$Cold){
 $c=New-Case $false $Cold;$c.persistenceCase='multi-area-autosave';$c.selection|Add-Member -NotePropertyName sourceArea -NotePropertyValue $c.parent.artifact.observations.nativeWrite.area
 $target=[pscustomobject]@{enterPoint=('e'*32);area=('f'*32);autoSaveMode='AfterEntry'};$c|Add-Member -NotePropertyName areaTarget -NotePropertyValue $target
 $c|Add-Member -NotePropertyName areaProfile -NotePropertyValue (Add-AreaProfile $c.parent)
 if(-not$Cold){$c.selection.case='persistence-save';$c.selection.expectedNativeWriteCount=1}
 else{
  $r=$c.checkpoint.request;$r.persistenceCase='multi-area-autosave';$c.checkpoint.owner.persistenceCase=$r.persistenceCase
  $r.ownedPairs|Add-Member -NotePropertyName sourceArea -NotePropertyValue $c.selection.sourceArea;$r|Add-Member -NotePropertyName persistenceAreaTarget -NotePropertyValue $target
  $w=$c.checkpoint.artifact.observations.nativeWrite;$w.nativeType='Auto';$w.nativeName='Autosave';$w.archive.AreaId=$target.area;$w.path=Join-Path (Join-Path $c.checkpoint.profile 'Saved Games') 'Auto_1.zks';$script:hashes[$w.path]=$w.sha256
  $c.checkpoint.artifact.observations.ownedOutputAdmission.number=1;$c.checkpoint.artifact.observations.ownedOutputAdmission.leaf='Auto_1.zks'
  $c|Add-Member -NotePropertyName checkpointProfile -NotePropertyValue (Add-AreaProfile $c.checkpoint)
 };return $c
}
foreach($cold in @($false,$true)){Check ('owned area original and destination qualification '+$cold){$c=New-AreaSource $cold;$v=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.areaTarget;if($v.descriptor.area-cne$(if($cold){$c.areaTarget.area}else{$c.selection.sourceArea})-or$v.descriptor.fileName-cne$(if($cold){'Auto_1.zks'}else{$c.parent.artifact.observations.nativeWrite.fileName})){throw 'Area descriptor substituted original or destination'};foreach($gate in @('complete-same-candidate-lineage','strict-manifest','strict-two-owner-artifact','settled-native-manual-output')){if(-not$script:calls.Contains($gate)){throw 'Area lineage skipped an existing required boundary'}}}}
$areaBad=[ordered]@{
 'area source pin substituted'={param($c)$c.selection.sourceArea='d'*32}
 'area target omitted'={param($c)$c.areaTarget=$null}
 'area native mode wrong'={param($c)$c.areaTarget.autoSaveMode='BeforeExit'}
 'area original profile unrestored'={param($c)$c.areaProfile.profileBytesRestoredExactly=$false}
 'area original cache accepted'={param($c)$c.areaProfile.acceptedNativeCacheChanges=@('accepted')}
 'area checkpoint profile unrestored'={param($c)$c.checkpointProfile.profileBytesRestoredExactly=$false}
 'area checkpoint source area changed'={param($c)$c.checkpoint.request.ownedPairs.sourceArea='e'*32}
 'area checkpoint destination changed'={param($c)$c.checkpoint.request.persistenceAreaTarget=Clone-AreaSource $c.areaTarget;$c.checkpoint.request.persistenceAreaTarget.enterPoint='d'*32}
 'area output from original area'={param($c)$c.checkpoint.artifact.observations.nativeWrite.archive.AreaId=$c.selection.sourceArea}
 'area original profile hash drift'={param($c)$script:drift=Join-Path $c.parent.root 'profile-cache-changes.json'}
 'area cold parent pin drift'={param($c)$script:drift=Join-Path $c.checkpoint.root 'chunk7-persistence-evidence.json'}
}
function Clone-AreaSource($v){return ($v|ConvertTo-Json -Depth 30|ConvertFrom-Json)}
foreach($m in $areaBad.GetEnumerator()){Check $m.Key {$c=New-AreaSource $true;&$m.Value $c;[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.areaTarget)} $true}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $testOutput 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archiveRead=$false;scope='actual owned qualifier; established lineage/artifact validators and OS observations substituted';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Owned source: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
