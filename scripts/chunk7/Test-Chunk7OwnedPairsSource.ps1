param([string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'};if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $repo 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($repo))) ('analysis-cache/chunk7-owned-source-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$testOutput=$OutputDirectory;$testFixtures=$FixtureDirectory
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew();$script:virtualLab=Join-Path $testOutput 'virtual-lab'
# Execute the actual new qualifier. Only OS file observation, the established
# full ancestor validator and strict manifest/output validators are substituted.
# Earlier cases use virtual OS observations; the current-format extension below
# reads tiny generated ZIPs and exercises guarded parent copies in this private lab.
function Get-KmcLabRoot{$script:virtualLab}
function Boundary([string]$Name){$script:calls.Add($Name);if($script:failure-ceq$Name){throw ('external boundary refused: '+$Name)}}
function Assert-KmcChildPath($Path,$Parent,$Description){$p=[IO.Path]::GetFullPath($Path);$q=[IO.Path]::GetFullPath($Parent).TrimEnd('\')+'\';if(-not$p.StartsWith($q,[StringComparison]::OrdinalIgnoreCase)){throw 'foreign path'};$p}
function Assert-KmcDirectoryTreeCloneable($Path,$Description){Boundary 'cloneable'}
function Assert-KmcNotReparsePoint($Path,$Description){Boundary 'not-reparse'}
function Assert-KmcNotHardLink($Path,$Description){Boundary 'not-hardlink'}
function Assert-KmcManifestJsonMembersUnique($Path,$Description){Boundary 'unique-json'}
function Read-KmcJson($Path){if(-not$script:documents.ContainsKey($Path)){throw ('unadmitted receipt '+$Path)};$script:documents[$Path]}
function Get-KmcSha256($Path){if(-not$script:hashes.ContainsKey($Path)){throw ('unadmitted hash '+$Path)};if(-not$script:readCounts.ContainsKey($Path)){$script:readCounts[$Path]=0};$script:readCounts[$Path]++;if($script:drift-ceq$Path-and$script:readCounts[$Path]-gt1){return ('0'*64)};$script:hashes[$Path]}
$script:realMetadata=@{}
function Get-Item([string]$LiteralPath){if(-not$script:hashes.ContainsKey($LiteralPath)){throw 'unknown virtual metadata'};if($script:realMetadata.ContainsKey($LiteralPath)){return $script:realMetadata[$LiteralPath]};[pscustomobject]@{Length=[long]100;LastWriteTimeUtc=[DateTime]::new(638960000000000000,[DateTimeKind]::Utc)}}
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
Check 'unselected owner checkpoint serialized empty' {$c=New-Case $false $false;$c.parent.owner.persistenceCase='';[void](Qualify $c)}
Check 'named owner checkpoint refused' {$c=New-Case $false $false;$c.parent.owner.persistenceCase='multi-exploration-manual';[void](Qualify $c)} $true 'unexpectedly selects a persistence checkpoint'
Check 'array owner checkpoint refused' {$c=New-Case $false $false;$c.parent.owner.persistenceCase=@();[void](Qualify $c)} $true 'unexpectedly selects a persistence checkpoint'
Check 'single-empty-string array owner checkpoint refused' {$c=New-Case $false $false;$c.parent.owner.persistenceCase=@('');[void](Qualify $c)} $true 'unexpectedly selects a persistence checkpoint'
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
# Matrix rows 58-59: row 58 is an owned cold consumer of the x4s-shaped mounted Manual with the scoped
# capital target; row 59 consumes row 58's fresh capital Manual through row 58's own re-derived lineage.
$capitalPin=[pscustomobject][ordered]@{enterPoint='fd225a93828d26449803a4847947f8c1';area='ead426a6c23d39548a670ee515d77df4';autoSaveMode='None'}
function New-CapitalSource([bool]$ColdConsumer){
 $c=New-Case $false $true;$c.persistenceCase='multi-exploration-capital-arrival-manual';$c.selection.expectedNativeWriteCount=1
 $c|Add-Member target (Clone-AreaSource $capitalPin)
 if(-not$ColdConsumer){return $c}
 $v58=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.target
 $r58=[ordered]@{scenario='chunk7-owned-two-pair-campaign';runId='synthetic-capital-arrival';evidenceRoot='';fixture=$c.fixture;ownedPairs=(Clone-AreaSource $c.selection);persistenceCase=$c.persistenceCase;persistenceLoad=$v58.descriptor;persistenceAreaTarget=(Clone-AreaSource $c.target)}
 foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$r58[$key]=$c.candidate.$key};$r58=[pscustomobject]$r58
 $n=[int]([regex]::Match($v58.descriptor.fileName,'^Manual_([0-9]+)_').Groups[1].Value)+1;$leaf='Manual_'+$n+'_KMC_C7_CAPITAL.zks'
 $a58=[pscustomobject]@{observations=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=$n;leaf=$leaf};capitalArrival=[pscustomobject]@{sourceArea=$v58.descriptor.area};nativeWrite=[pscustomobject]@{path='';nativeName='KMC_C7_CAPITAL';nativeType='Manual';sha256=('7'*64);length=2345;archive=[pscustomobject]@{AreaId=$capitalPin.area}}}}
 $set58=Add-Set $r58 $a58 'chunk7-persistence-evidence.json' ('3'*64) $v58.descriptor.sha256 $c.persistenceCase
 $a58.observations.nativeWrite.path=Join-Path (Join-Path $set58.profile 'Saved Games') $leaf;$script:hashes[$a58.observations.nativeWrite.path]='7'*64
 [void](Add-AreaProfile $set58)
 $s59=Clone-AreaSource $c.selection;$s59.checkpointSourceRunId=$r58.runId;$s59.checkpointSourceArtifactSha256='3'*64;$s59.expectedNativeWriteCount=0
 # Batch198 r59: the lineage Manual area is pinned as the area row 58 departed from.
 $s59|Add-Member sourceArea $v58.descriptor.area
 $c|Add-Member capital ([pscustomobject]@{set=$set58;selection=$s59;leaf=$leaf});return $c
}
function Qualify-Capital($C){Get-KmcChunk7OwnedPairsSource $C.capital.selection 'multi-exploration-capital-arrival-cold-manual' $C.fixture $C.candidate}
Check 'capital producer qualifies its x4s-shaped Manual with the exact capital target'{$c=New-CapitalSource $false;$v=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.target;if($v.descriptor.sha256-cne('9'*64)-or$v.checkpointArtifactSha256-cne$c.selection.checkpointSourceArtifactSha256-or$c.checkpoint.request.persistenceCase-cne'multi-exploration-manual'){throw 'Capital producer source changed parent identity.'};if(-not$script:calls.Contains('strict-checkpoint-artifact')){throw 'Capital producer skipped the checkpoint artifact guard.'}}
foreach($bad in @('no-target','other-entry','other-area','after-entry')){Check ('capital producer refuses target '+$bad){$c=New-CapitalSource $false;switch($bad){'no-target'{$c.target=$null}'other-entry'{$c.target.enterPoint='1'*32}'other-area'{$c.target.area='b'*32}'after-entry'{$c.target.autoSaveMode='AfterEntry'}};[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.target)} $true}
Check 'capital cold consumer qualifies row 58 Manual through row 58 lineage'{$c=New-CapitalSource $true;$v=Qualify-Capital $c;$d=$v.descriptor
 if($d.sha256-cne('7'*64)-or$d.fileName-cne$c.capital.leaf-or$d.internalName-cne'KMC_C7_CAPITAL'-or$d.area-cne$capitalPin.area-or$v.checkpointArtifactSha256-cne('3'*64)-or$v.artifactSha256-cne$c.selection.sourceArtifactSha256){throw 'Capital cold source differs from row 58 output or lineage.'}
 if(@($script:calls|Where-Object{$_-ceq'strict-checkpoint-artifact'}).Count-lt2){throw 'Capital cold source skipped a producer artifact guard.'}}
$capitalColdMutations=[ordered]@{
 'consumer declares a target'={param($c)$c|Add-Member coldTarget $capitalPin}
 'producer not a cold consumer'={param($c)$c.capital.set.request.ownedPairs.case='persistence-save'}
 'producer other checkpoint'={param($c)$c.capital.set.request.persistenceCase='multi-exploration-manual'}
 'producer owner other checkpoint'={param($c)$c.capital.set.owner.persistenceCase='multi-exploration-manual'}
 'producer autosave target'={param($c)$c.capital.set.request.persistenceAreaTarget.autoSaveMode='AfterEntry'}
 'producer other Manual'={param($c)$c.capital.set.request.persistenceLoad.length++}
 'producer second write'={param($c)$c.capital.set.game.saveRequestCount=2}
 'producer failed'={param($c)$c.capital.set.result.status='FAIL'}
 'producer profile not restored'={param($c)($script:documents[(Join-Path $c.capital.set.root 'profile-cache-changes.json')]).profileBytesRestoredExactly=$false}
 'producer artifact pin'={param($c)$c.capital.selection.checkpointSourceArtifactSha256='0'*64}
 'producer output outside the capital'={param($c)$c.capital.set.artifact.observations.nativeWrite.archive.AreaId='a'*32}
 'producer output name'={param($c)$c.capital.set.artifact.observations.nativeWrite.nativeName='KMC_C7_WORLD'}
 'producer changes ancestor'={param($c)$c.capital.set.request.ownedPairs.sourceArtifactSha256='0'*64}
 'consumer roster'={param($c)$c.capital.selection.partyIds+=@('aaaaaaaa-3333-4333-8333-333333333333')}
 'consumer write'={param($c)$c.capital.selection.expectedNativeWriteCount=1}
}
foreach($m in $capitalColdMutations.GetEnumerator()){Check ('capital cold source refuses '+$m.Key){$c=New-CapitalSource $true;&$m.Value $c;$t=if($null-ne$c.PSObject.Properties['coldTarget']){$c.coldTarget}else{$null};[void](Get-KmcChunk7OwnedPairsSource $c.capital.selection 'multi-exploration-capital-arrival-cold-manual' $c.fixture $c.candidate $t)} $true}
# Batch198 r59: the consumer's lineage area pin is bound to row 58's departure and the staged receipt's own areas.
$capitalLineageMutations=[ordered]@{
 'consumer without lineage area'=@({param($c)$c.capital.selection.PSObject.Properties.Remove('sourceArea')},'inherited native pair selection')
 'consumer capital lineage area'=@({param($c)$c.capital.selection.sourceArea=$capitalPin.area},'non-capital acquired lineage')
 'consumer other lineage area'=@({param($c)$c.capital.selection.sourceArea='a'*32},'not exactly the producer departure area')
 'producer recorded another departure area'=@({param($c)$c.capital.set.artifact.observations.capitalArrival.sourceArea='a'*32},'not exactly the producer departure area')
 'producer omits its departure area'=@({param($c)$c.capital.set.artifact.observations.PSObject.Properties.Remove('capitalArrival')},'not exactly the producer departure area')
 'lineage receipt field in another area'=@({param($c)$c.parent.artifact.observations.fieldWorld.area='a'*32},'not written in its pinned area')
 # Refused first by the re-derived row 58 lineage pin; the later capital-cold re-hash is defence in depth.
 'lineage receipt bytes differ from the pin (earlier lineage refusal)'=@({param($c)$script:hashes[(Join-Path $c.parent.root 'chunk7-acquired-manual-evidence.json')]='0'*64},'Two-owner Manual source receipt differs')
}
foreach($m in $capitalLineageMutations.GetEnumerator()){Check ('capital cold source refuses '+$m.Key){$c=New-CapitalSource $true;&$m.Value[0] $c;[void](Qualify-Capital $c)} $true $m.Value[1]}
foreach($m in $areaBad.GetEnumerator()){Check $m.Key {$c=New-AreaSource $true;&$m.Value $c;[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.areaTarget)} $true}
# Owner-seed kind: the owned qualifier delegates to the established cold owner-seed
# source (registry/master/intake/header gate, substituted here) with an exact cold
# selection, then applies the owned-family descriptor; no producer receipt is read.
function Get-KmcChunk7OwnerSeedSource($Selection,$Fixture,$Candidate){
 Boundary 'owner-seed-registry-master-intake-header';$script:coldSelection=$Selection
 [pscustomobject]@{path='C:\virtual\fixtures\owner-supplied\KMC_C7_OWNER_SEED.master.zks';descriptor=$script:seedDescriptor;seedId=$Selection.seedId;registryPath='registry';registrySha256=('a'*64);registryEntry=[pscustomobject]@{intakePath='fixtures/owner-supplied/x.json'};intakePath='C:\virtual\intake.json';intakeSha256=('b'*64);fixtureSetupOnly=$script:seedSetupOnly;nativeAuthorityGranted=$script:seedAuthority}
}
function New-OwnerSeedSource{
 $script:calls=[Collections.Generic.List[string]]::new();$script:failure=$null;$script:coldSelection=$null;$script:seedSetupOnly=$true;$script:seedAuthority=$false
 $f=[IO.File]::ReadAllText((Join-Path $testFixtures 'TwoOwnerManualExistingFixture.json'))|ConvertFrom-Json;$r=$f.request
 $script:seedDescriptor=[pscustomobject][ordered]@{internalName='KMC_C7_OWNER_SEED';fileName='Manual_302_KMC_C7_OWNER_SEED.zks';sha256=('9'*64);length=1401014;lastWriteTimeUtcTicks=638960000000000000;gameId='ce9e3801-6f92-4f6b-8738-b990fc5111bf';gameName='Testor The Reliable';area='b10e86516b6437d45a629931c30936fe'}
 $a='10101010-1010-4010-8010-101010101010';$b='30303030-3030-4030-8030-303030303030'
 $s=[pscustomobject][ordered]@{case='owner-seed-exploration';seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$a;riderBId=$b;partyIds=@($a,$b);xpAssistance=[pscustomobject]@{unitId=$b;targetCharacterLevel=8};baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 [pscustomobject]@{selection=$s;fixture=$r.fixture;candidate=($r|ConvertTo-Json -Depth 20|ConvertFrom-Json)}
}
Check 'owner-seed source delegates to the exact cold owner-seed gate' {$c=New-OwnerSeedSource;$v=Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate
 $cold=$script:coldSelection;if($null-eq$cold-or(@($cold.PSObject.Properties.Name)-join',')-cne'case,baseGameOnly,expectedNativeLoadCount,expectedNativeWriteCount,seedId,seedSha256'-or$cold.case-cne'owner-seed-readonly-inventory'-or$cold.seedId-cne$c.selection.seedId-or$cold.seedSha256-cne$c.selection.seedSha256-or$cold.expectedNativeWriteCount-ne0){throw 'Cold owner-seed gate selection differs'}
 if($v.ownerSeed-ne$true-or$v.nativeAuthorityGranted-ne$false-or$v.descriptor.sha256-cne('9'*64)-or@($script:calls|Where-Object{$_-ceq'complete-same-candidate-lineage'}).Count-ne0){throw 'Owner-seed source read producer lineage or lost its identity'}}
Check 'owner-seed source refuses an area target' {$c=New-OwnerSeedSource;[void](Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate ([pscustomobject]@{enterPoint=('e'*32);area=('f'*32);autoSaveMode='AfterEntry'}))} $true 'no area target'
Check 'owner-seed source refuses a registry SHA that differs from the selection' {$c=New-OwnerSeedSource;$script:seedDescriptor.sha256='8'*64;[void](Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate)} $true
Check 'owner-seed source refuses a gate result that grants authority' {$c=New-OwnerSeedSource;$script:seedAuthority=$true;[void](Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate)} $true 'claims native authority'
Check 'owner-seed source propagates the cold gate refusal' {$c=New-OwnerSeedSource;$script:failure='owner-seed-registry-master-intake-header';[void](Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate)} $true 'external boundary refused'
Check 'owner-seed source refuses a checkpoint' {$c=New-OwnerSeedSource;[void](Get-KmcChunk7OwnedPairsSource $c.selection 'multi-exploration-manual' $c.fixture $c.candidate)} $true
Check 'registry no-mods seed with a spaced internal name qualifies' {$c=New-OwnerSeedSource;$c.selection.seedId='KMC_C7_NO_MODS_OWNER_SEED';$script:seedDescriptor.internalName='KMC C7 NO MODS OWNER SEED';$script:seedDescriptor.fileName='Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks';$v=Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate;if($v.descriptor.internalName-cne'KMC C7 NO MODS OWNER SEED'-or$script:coldSelection.seedId-cne'KMC_C7_NO_MODS_OWNER_SEED'){throw 'Spaced registry seed was not qualified'}}
Check 'producer selection never takes the owner-seed route' {$c=New-Case $false $false;$script:coldSelection=$null;[void](Qualify $c);if($null-ne$script:coldSelection){throw 'Producer selection reached the owner-seed gate'}}
# Owner-seed-derived persistence. The producer reuses the owner-seed gate with its Manual checkpoint;
# the consumer is pinned to the producer run, its world evidence and the written Manual bytes, and its
# producer must have loaded the owner's unchanged registered seed.
Check 'owner-seed Manual producer source delegates to the exact cold owner-seed gate' {$c=New-OwnerSeedSource;$c.selection.case='owner-seed-persistence-save';$c.selection.expectedNativeWriteCount=1;$v=Get-KmcChunk7OwnedPairsSource $c.selection 'multi-exploration-manual' $c.fixture $c.candidate;if($v.ownerSeed-ne$true-or$v.descriptor.sha256-cne('9'*64)-or$script:coldSelection.case-cne'owner-seed-readonly-inventory'){throw 'Producer did not qualify its owner seed through the cold gate'}}
Check 'owner-seed Manual producer source refuses a missing checkpoint' {$c=New-OwnerSeedSource;$c.selection.case='owner-seed-persistence-save';$c.selection.expectedNativeWriteCount=1;[void](Get-KmcChunk7OwnedPairsSource $c.selection $null $c.fixture $c.candidate)} $true 'multi-exploration-manual'
function Assert-KmcChunk7WorldArtifact($Request,$Manifest,$Status){Boundary 'strict-producer-world-artifact';if($Status-cne'PASS'){throw 'source failure misclassified'}}
function Assert-KmcMatchedPoolArtifact($Request,$Manifest,$Status){Boundary 'producer-no-matched-pool'}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,$Leaf,$Kind){if($Leaf-cne($Kind+'.json')-or$Kind-cnotin@('chunk7-acquired-manual-evidence','chunk7-persistence-evidence','chunk7-world-evidence')){throw 'foreign artifact role'};return $script:documents[(Join-Path $Request.evidenceRoot $Leaf)]}
function New-SeedDerivedSource{
 $o=New-OwnerSeedSource;$script:calls=[Collections.Generic.List[string]]::new();$script:documents=@{};$script:hashes=@{};$script:readCounts=@{};$script:drift=$null
 $f=[IO.File]::ReadAllText((Join-Path $testFixtures 'TwoOwnerManualExistingFixture.json'))|ConvertFrom-Json;$r=$f.request
 $a=$o.selection.riderAId;$b=$o.selection.riderBId;$ma='20202020-2020-4020-8020-202020202020';$mb='40404040-4040-4040-8040-404040404040';$party=@($a,$b,$ma,$mb)
 $ps=$o.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json;$ps.case='owner-seed-persistence-save';$ps.expectedNativeWriteCount=1;$ps.partyIds=@($a,$b)
 $pr=[ordered]@{scenario='chunk7-owned-two-pair-campaign';runId='synthetic-owner-seed-manual';evidenceRoot='';fixture=$r.fixture;ownedPairs=$ps;persistenceCase='multi-exploration-manual';persistenceLoad=($script:seedDescriptor|ConvertTo-Json -Depth 5|ConvertFrom-Json)}
 foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$pr[$key]=$r.$key};$pr=[pscustomobject]$pr
 $leaf='Manual_303_KMC_C7_SEED_WORLD.zks'
 $artifact=[pscustomobject]@{observations=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=303;leaf=$leaf};nativeWrite=[pscustomobject]@{path='';nativeName='KMC_C7_SEED_WORLD';nativeType='Manual';sha256=('5'*64);length=2048}
  ownerSeedManual=[pscustomobject]@{twoOwners=@([pscustomobject]@{actorId=$a;petId=$ma},[pscustomobject]@{actorId=$b;petId=$mb});fieldWorld=[pscustomobject]@{party=@($party|ForEach-Object{[pscustomobject]@{id=$_}})}}}}
 $producer=Add-Set $pr $artifact 'chunk7-world-evidence.json' ('6'*64) ('9'*64) 'multi-exploration-manual'
 $artifact.observations.nativeWrite.path=Join-Path (Join-Path $producer.profile 'Saved Games') $leaf;$script:hashes[$artifact.observations.nativeWrite.path]='5'*64
 $s=[pscustomobject][ordered]@{case='owner-seed-persistence-load';sourceRunId=$pr.runId;sourceArtifactSha256=('6'*64);sourceArchiveSha256=('5'*64);seedId=$o.selection.seedId;seedSha256=$o.selection.seedSha256;riderAId=$a;mountAId=$ma;riderBId=$b;mountBId=$mb;partyIds=$party;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 [pscustomobject]@{selection=$s;fixture=$r.fixture;candidate=$o.candidate;producer=$producer;persistenceCase='multi-exploration-manual'}
}
Check 'owner-seed-derived consumer qualifies exactly its producer Manual' {$c=New-SeedDerivedSource;$v=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate
 if($v.ownerSeedDerived-ne$true-or$v.descriptor.internalName-cne'KMC_C7_SEED_WORLD'-or$v.descriptor.fileName-cne'Manual_303_KMC_C7_SEED_WORLD.zks'-or$v.descriptor.sha256-cne('5'*64)-or$v.artifactSha256-cne('6'*64)-or$v.descriptor.gameId-cne$script:seedDescriptor.gameId-or$v.seed.descriptor.sha256-cne('9'*64)){throw 'Consumer source substituted the producer Manual, its evidence or the seed'}
 foreach($gate in @('owner-seed-registry-master-intake-header','strict-manifest','strict-producer-world-artifact','producer-no-matched-pool')){if(-not$script:calls.Contains($gate)){throw ('consumer omitted required gate '+$gate)}}}
$derivedFaults=[ordered]@{
 'artifact hash mismatch'={param($c)$c.selection.sourceArtifactSha256='e'*64}
 'archive hash mismatch'={param($c)$c.selection.sourceArchiveSha256='e'*64}
 'written bytes changed'={param($c)$script:hashes[$c.producer.artifact.observations.nativeWrite.path]='e'*64}
 'producer is exploration only'={param($c)$c.producer.request.ownedPairs.case='owner-seed-exploration';$c.producer.request.ownedPairs.expectedNativeWriteCount=0}
 'producer acquired ancestry'={param($c)$c.producer.request.ownedPairs=$null}
 'producer failed'={param($c)$c.producer.result.status='FAIL'}
 'producer foreign candidate'={param($c)$c.candidate.dllSha256='0'*64}
 'producer other seed'={param($c)$c.producer.request.ownedPairs.seedSha256='8'*64}
 'producer loaded another seed'={param($c)$c.producer.request.persistenceLoad.lastWriteTimeUtcTicks=1}
 'producer rider roles'={param($c)$c.selection.mountAId='aaaaaaaa-2222-4222-8222-222222222222';$c.selection.partyIds[2]=$c.selection.mountAId}
 'producer roster'={param($c)$c.selection.partyIds+=@('aaaaaaaa-3333-4333-8333-333333333333')}
 'Manual outside producer root'={param($c)$c.producer.artifact.observations.nativeWrite.path='C:\foreign\Manual_303_KMC_C7_SEED_WORLD.zks';$script:hashes['C:\foreign\Manual_303_KMC_C7_SEED_WORLD.zks']='5'*64}
 'producer checkpoint'={param($c)$c.producer.request.persistenceCase='multi-exploration-quick'}
 'producer owner checkpoint'={param($c)$c.producer.owner.persistenceCase=$null}
 'consumer checkpoint'={param($c)$c.persistenceCase='multi-tb-debt-manual'}
 'consumer is its own producer'={param($c)$c.selection.sourceRunId='unassigned-owned-seed-derived'}
 'producer world artifact refused'={param($c)$script:failure='strict-producer-world-artifact'}
}
foreach($derivedFault in $derivedFaults.Keys){Check ('owner-seed-derived consumer refuses '+$derivedFault) {$c=New-SeedDerivedSource;&$derivedFaults[$derivedFault] $c;[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate)} $true}
# Owner-seed-derived combat (scenarios 3-5): the producer chain above plus the ACCEPTED cold consumer
# receipt (checkpointSource*), each pinned to its own recorded candidate. The consumer's full cold-proof
# comparison is the reader's established validator, substituted here as a bound boundary.
function Assert-KmcChunk7OwnerSeedDerivedColdSource($Request,$ProducerArtifact,$ColdArtifact){Boundary 'consumer-cold-proof';if(-not[object]::ReferenceEquals($ProducerArtifact,$script:producerArtifact)-or-not[object]::ReferenceEquals($ColdArtifact,$script:consumerArtifact)){throw 'cold proof bound to the wrong receipts'}}
$script:companion='c0c0c0c0-c0c0-40c0-80c0-c0c0c0c0c0c0'
function New-SeedCombatSource([string]$Case='owner-seed-combat-rider-first'){
 $c=New-SeedDerivedSource;$load=$c.selection;$load.partyIds=@($load.partyIds)+@($script:companion)
 $c.producer.artifact.observations.ownerSeedManual.fieldWorld.party=@($load.partyIds|ForEach-Object{[pscustomobject]@{id=$_}});$script:producerArtifact=$c.producer.artifact
 $descriptor=(Get-KmcChunk7OwnedPairsSource $load $c.persistenceCase $c.fixture $c.candidate).descriptor;$script:readCounts=@{};$script:calls=[Collections.Generic.List[string]]::new()
 $s=[ordered]@{case=$Case;sourceRunId=$load.sourceRunId;sourceArtifactSha256=$load.sourceArtifactSha256;sourceArchiveSha256=$load.sourceArchiveSha256;checkpointSourceRunId='synthetic-owner-seed-cold';checkpointSourceArtifactSha256=('7'*64);seedId=$load.seedId;seedSha256=$load.seedSha256;riderAId=$load.riderAId;mountAId=$load.mountAId;riderBId=$load.riderBId;mountBId=$load.mountBId;partyIds=$load.partyIds}
 if($Case-ceq'owner-seed-realtime-core'){$s['companionId']=$script:companion};foreach($key in @('baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount')){$s[$key]=$load.$key};$s=[pscustomobject]$s
 $cr=[ordered]@{scenario='chunk7-owned-two-pair-campaign';runId=$s.checkpointSourceRunId;evidenceRoot='';fixture=$c.fixture;ownedPairs=($load|ConvertTo-Json -Depth 20|ConvertFrom-Json);persistenceCase='multi-exploration-manual';persistenceLoad=($descriptor|ConvertTo-Json -Depth 5|ConvertFrom-Json)}
 foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$cr[$key]=$c.producer.request.$key};$cr=[pscustomobject]$cr
 $script:consumerArtifact=[pscustomobject]@{status='PASS';observations=[pscustomobject]@{ownerSeedDerivedCold=[pscustomobject]@{contract='owner-seed-derived-cold-v1'}}}
 $consumer=Add-Set $cr $script:consumerArtifact 'chunk7-persistence-evidence.json' ('7'*64) $s.sourceArchiveSha256 'multi-exploration-manual'
 foreach($receipt in @($consumer.result,$consumer.game)){$receipt.saveRequestCount=0;$receipt.workingSaveRequestCount=0}
 [pscustomobject]@{selection=$s;fixture=$c.fixture;candidate=$c.candidate;producer=$c.producer;consumer=$consumer;persistenceCase=$null}
}
function Qualify-Combat($C){Get-KmcChunk7OwnedPairsSource $C.selection $C.persistenceCase $C.fixture $C.candidate}
foreach($combatCase in @('owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-combat-disable','owner-seed-native-life-cleanup','owner-seed-native-death-cleanup')){
 Check ('owner-seed-derived combat qualifies the producer and the accepted consumer '+$combatCase) {$c=New-SeedCombatSource $combatCase;$v=Qualify-Combat $c
  if($v.ownerSeedDerived-ne$true-or$v.descriptor.sha256-cne('5'*64)-or$v.artifactSha256-cne('6'*64)-or$v.consumerRunId-cne'synthetic-owner-seed-cold'-or$v.consumerArtifactSha256-cne('7'*64)-or$v.candidates.sameCandidate-ne$true-or$v.candidates.producer.commit-cne$c.producer.request.commit-or$v.candidates.consumer.dllSha256-cne$c.consumer.request.dllSha256){throw 'Combat source lost the producer/consumer chain or the candidate fact'}
  foreach($gate in @('owner-seed-registry-master-intake-header','strict-producer-world-artifact','strict-checkpoint-artifact','consumer-cold-proof')){if(-not$script:calls.Contains($gate)){throw ('combat source omitted required gate '+$gate)}}}
}
Check 'owner-seed-derived combat records a later current candidate without relabelling the chain' {$c=New-SeedCombatSource;$c.candidate.commit='f'*40;$c.candidate.dllSha256='f'*64;$c.candidate.dllMvid='ffffffff-ffff-4fff-8fff-ffffffffffff';$v=Qualify-Combat $c
 if($v.candidates.sameCandidate-ne$false-or$v.candidates.current.commit-cne('f'*40)-or$v.candidates.producer.commit-ceq('f'*40)-or$v.candidates.consumer.commit-ceq('f'*40)-or$v.ownerSeedDerived-ne$true){throw 'Cross-candidate fact was not recorded honestly'}}
$combatFaults=[ordered]@{
 'missing producer receipt'={param($c)$c.selection.sourceRunId='synthetic-absent-producer'}
 'wrong producer receipt'={param($c)$c.selection.sourceArtifactSha256='e'*64}
 'producer FAIL'={param($c)$c.producer.result.status='FAIL'}
 'producer malformed recorded candidate'={param($c)$c.producer.request.dllMvid='not-a-guid'}
 'producer receipt from another candidate than it records'={param($c)$c.producer.result.dllSha256='0'*64}
 'Manual hash mismatch'={param($c)$c.selection.sourceArchiveSha256='e'*64}
 'written Manual bytes changed'={param($c)$script:hashes[$c.producer.artifact.observations.nativeWrite.path]='e'*64}
 'missing consumer receipt'={param($c)$c.selection.checkpointSourceRunId='synthetic-absent-consumer'}
 'wrong consumer receipt'={param($c)$c.selection.checkpointSourceArtifactSha256='e'*64}
 'consumer FAIL'={param($c)$c.consumer.result.status='FAIL'}
 'consumer game FAIL'={param($c)$c.consumer.game.status='FAIL'}
 'consumer errors'={param($c)$c.consumer.game.errors=@('failed')}
 'consumer wrote'={param($c)$c.consumer.game.saveRequestCount=1;$c.consumer.game.workingSaveRequestCount=1}
 'consumer second load'={param($c)$c.consumer.game.loadRequestCount=2}
 'consumer is another kind'={param($c)$c.consumer.request.ownedPairs.case='persistence-load'}
 'consumer other checkpoint'={param($c)$c.consumer.request.persistenceCase='multi-exploration-quick'}
 'consumer loaded another Manual'={param($c)$c.consumer.request.persistenceLoad.sha256='e'*64}
 'consumer pinned another producer'={param($c)$c.consumer.request.ownedPairs.sourceRunId='synthetic-other-producer'}
 'consumer other seed'={param($c)$c.consumer.request.ownedPairs.seedSha256='8'*64}
 'consumer other roster'={param($c)$c.consumer.request.ownedPairs.partyIds=@($c.consumer.request.ownedPairs.partyIds|Select-Object -First 4)}
 'consumer owner input is not the Manual'={param($c)$c.consumer.owner.sourceSha256='9'*64}
 'consumer receipt from another candidate than it records'={param($c)$c.consumer.result.dllSha256='0'*64}
 'consumer malformed recorded candidate'={param($c)$c.consumer.request.commit='x'}
 'consumer artifact refused'={param($c)$script:failure='strict-checkpoint-artifact'}
 'consumer cold proof refused'={param($c)$script:failure='consumer-cold-proof'}
 'consumer receipt drift'={param($c)$script:drift=Join-Path $c.consumer.root 'chunk7-persistence-evidence.json'}
 'combat selects a checkpoint'={param($c)$c.persistenceCase='multi-exploration-manual'}
 'TB carries a companion'={param($c)$c.selection|Add-Member -NotePropertyName companionId -NotePropertyValue $script:companion}
}
foreach($combatFault in $combatFaults.Keys){Check ('owner-seed-derived combat refuses '+$combatFault) {$c=New-SeedCombatSource;&$combatFaults[$combatFault] $c;[void](Qualify-Combat $c)} $true}
foreach($disableFault in @('missing consumer receipt','consumer FAIL','consumer wrote','consumer second load','combat selects a checkpoint','TB carries a companion')){Check ('registered disable source refuses '+$disableFault) {$c=New-SeedCombatSource 'owner-seed-combat-disable';&$combatFaults[$disableFault] $c;[void](Qualify-Combat $c)} $true}
Check 'registered disable source retains exact zero-write selection' {$c=New-SeedCombatSource 'owner-seed-combat-disable';$c.selection.expectedNativeWriteCount=1;[void](Qualify-Combat $c)} $true
Check 'owner-seed-derived RT refuses a missing companion' {$c=New-SeedCombatSource 'owner-seed-realtime-core';$c.selection.PSObject.Properties.Remove('companionId');[void](Qualify-Combat $c)} $true
Check 'owner-seed-derived RT refuses a pair actor as companion' {$c=New-SeedCombatSource 'owner-seed-realtime-core';$c.selection.companionId=$c.selection.mountBId;[void](Qualify-Combat $c)} $true
# Owner-seed checkpoint pair (Directive 4). The producer reuses the derived-combat chain (Manual
# producer plus ACCEPTED cold consumer) with one write and a Quick/Auto checkpoint; the consumer
# reads the producer's own receipt set and builds its input from the producer's nativeWrite.
function New-SeedCheckpointSaveSource([string]$Checkpoint='multi-exploration-quick'){
 $c=New-SeedCombatSource;$c.selection.case='owner-seed-checkpoint-save';$c.selection.expectedNativeWriteCount=if($Checkpoint-ceq'multi-owned-worker-cancel-manual'){2}else{1};$c.persistenceCase=$Checkpoint;$c
}
function New-SeedCheckpointLoadSource([string]$Checkpoint='multi-exploration-quick'){
 $sourceCase=Get-KmcChunk7PersistenceSourceCase $Checkpoint
 $p=New-SeedCheckpointSaveSource $sourceCase;$manual=Qualify-Combat $p;$script:readCounts=@{};$script:calls=[Collections.Generic.List[string]]::new()
 # A combat checkpoint is a genuine native Manual numbered after the derived Manual (303 -> 304) under its distinct name.
 $type=if($Checkpoint-ceq'multi-exploration-quick'){'Quick'}elseif($Checkpoint-ceq'multi-exploration-auto'){'Auto'}else{'Manual'}
 $number=if($type-ceq'Manual'){304}else{1};$name=if($type-ceq'Manual'){'KMC_C7_SEED_CHECKPOINT'}else{$type+' save'};$leaf=if($type-ceq'Manual'){'Manual_304_KMC_C7_SEED_CHECKPOINT.zks'}else{$type+'_1.zks'}
 $pr=[ordered]@{scenario='chunk7-owned-two-pair-campaign';runId='synthetic-owner-seed-checkpoint';evidenceRoot='';fixture=$p.fixture;ownedPairs=($p.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json);persistenceCase=$sourceCase;persistenceLoad=($manual.descriptor|ConvertTo-Json -Depth 5|ConvertFrom-Json)}
 foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){$pr[$key]=$p.producer.request.$key};$pr=[pscustomobject]$pr
 $script:checkpointArtifact=[pscustomobject]@{status='PASS';observations=[pscustomobject]@{ownedOutputAdmission=[pscustomobject]@{number=$number;leaf=$leaf};nativeWrite=[pscustomobject]@{path='';nativeName=$name;nativeType=$type;sha256=('3'*64);length=4096;archive=[pscustomobject]@{AreaId=$manual.descriptor.area}}}}
 $parentSet=Add-Set $pr $script:checkpointArtifact 'chunk7-persistence-evidence.json' ('2'*64) $p.selection.sourceArchiveSha256 $sourceCase
 $script:checkpointArtifact.observations.nativeWrite.path=Join-Path (Join-Path $parentSet.profile 'Saved Games') $leaf;$script:hashes[$script:checkpointArtifact.observations.nativeWrite.path]='3'*64
 $s=$p.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json;$s.case='owner-seed-checkpoint-load';$s.expectedNativeWriteCount=if($Checkpoint-cin@('legacy-mounted-pending-tb-manual','owned-schema1-resave-manual')){1}else{0};$s.checkpointSourceRunId=$pr.runId;$s.checkpointSourceArtifactSha256='2'*64
 [pscustomobject]@{selection=$s;fixture=$p.fixture;candidate=$p.candidate;producer=$p.producer;consumer=$p.consumer;checkpoint=$parentSet;persistenceCase=$Checkpoint}
}
foreach($checkpointCase in @('multi-exploration-quick','multi-exploration-auto')){
 Check ('owner-seed checkpoint producer qualifies the Manual producer and the accepted consumer '+$checkpointCase) {$c=New-SeedCheckpointSaveSource $checkpointCase;$v=Qualify-Combat $c
  if($v.ownerSeedDerived-ne$true-or$v.descriptor.internalName-cne'KMC_C7_SEED_WORLD'-or$v.descriptor.sha256-cne('5'*64)-or$v.artifactSha256-cne('6'*64)-or$v.consumerRunId-cne'synthetic-owner-seed-cold'-or$v.consumerArtifactSha256-cne('7'*64)-or$v.candidates.sameCandidate-ne$true){throw 'Checkpoint producer lost the Manual producer/consumer chain'}
  foreach($gate in @('owner-seed-registry-master-intake-header','strict-producer-world-artifact','strict-checkpoint-artifact','consumer-cold-proof')){if(-not$script:calls.Contains($gate)){throw ('checkpoint producer omitted required gate '+$gate)}}}
 Check ('owner-seed checkpoint consumer qualifies exactly its producer checkpoint '+$checkpointCase) {$c=New-SeedCheckpointLoadSource $checkpointCase;$v=Qualify-Combat $c;$type=if($checkpointCase-ceq'multi-exploration-quick'){'Quick'}else{'Auto'}
  if($v.ownerSeedDerived-ne$true-or$v.descriptor.fileName-cne($type+'_1.zks')-or$v.descriptor.internalName-cne($type+' save')-or$v.descriptor.sha256-cne('3'*64)-or$v.descriptor.length-ne4096-or$v.descriptor.gameId-cne$script:seedDescriptor.gameId-or$v.descriptor.area-cne$script:seedDescriptor.area-or
     $v.artifactSha256-cne('6'*64)-or$v.checkpointRunId-cne'synthetic-owner-seed-checkpoint'-or$v.checkpointArtifactSha256-cne('2'*64)-or$v.manual.consumerRunId-cne'synthetic-owner-seed-cold'-or$v.manual.descriptor.sha256-cne('5'*64)){throw 'Checkpoint consumer substituted the producer checkpoint, its evidence or the Manual lineage'}
  foreach($gate in @('owner-seed-registry-master-intake-header','strict-producer-world-artifact','strict-checkpoint-artifact','consumer-cold-proof')){if(-not$script:calls.Contains($gate)){throw ('checkpoint consumer omitted required gate '+$gate)}}}
}
Check 'owner-seed checkpoint producer records a later current candidate without relabelling the chain' {$c=New-SeedCheckpointSaveSource;$c.candidate.commit='f'*40;$c.candidate.dllSha256='f'*64;$c.candidate.dllMvid='ffffffff-ffff-4fff-8fff-ffffffffffff';$v=Qualify-Combat $c
 if($v.candidates.sameCandidate-ne$false-or$v.candidates.current.commit-cne('f'*40)-or$v.candidates.producer.commit-ceq('f'*40)){throw 'Cross-candidate fact was not recorded honestly'}}
# P2 combat checkpoints: the producer chain is unchanged; the consumer's input is the producer's genuine native
# Manual under KMC_C7_SEED_CHECKPOINT, numbered after the derived Manual it loaded.
foreach($checkpointCase in @('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual')){
 Check ('owner-seed combat checkpoint producer qualifies the Manual producer and the accepted consumer '+$checkpointCase) {$c=New-SeedCheckpointSaveSource $checkpointCase;$v=Qualify-Combat $c
  if($v.ownerSeedDerived-ne$true-or$v.descriptor.internalName-cne'KMC_C7_SEED_WORLD'-or$v.descriptor.sha256-cne('5'*64)-or$v.consumerRunId-cne'synthetic-owner-seed-cold'){throw 'Combat checkpoint producer lost the Manual producer/consumer chain'}}
 Check ('owner-seed combat checkpoint consumer qualifies exactly its producer Manual '+$checkpointCase) {$c=New-SeedCheckpointLoadSource $checkpointCase;$v=Qualify-Combat $c
  if($v.descriptor.fileName-cne'Manual_304_KMC_C7_SEED_CHECKPOINT.zks'-or$v.descriptor.internalName-cne'KMC_C7_SEED_CHECKPOINT'-or$v.descriptor.sha256-cne('3'*64)-or$v.checkpointRunId-cne'synthetic-owner-seed-checkpoint'-or$v.manual.descriptor.sha256-cne('5'*64)){throw 'Combat checkpoint consumer substituted the producer Manual or its lineage'}}
 $combatCheckpointFaults=[ordered]@{
  'a Manual written under the acquired-ancestry name'={param($c)$w=$c.checkpoint.artifact.observations;$w.nativeWrite.nativeName='KMC_C7_WORLD';$w.ownedOutputAdmission.leaf='Manual_304_KMC_C7_WORLD.zks';$w.nativeWrite.path=Join-Path (Join-Path $c.checkpoint.profile 'Saved Games') 'Manual_304_KMC_C7_WORLD.zks';$script:hashes[$w.nativeWrite.path]='3'*64}
  'a Manual written under the derived Manual name'={param($c)$w=$c.checkpoint.artifact.observations;$w.nativeWrite.nativeName='KMC_C7_SEED_WORLD';$w.ownedOutputAdmission.leaf='Manual_304_KMC_C7_SEED_WORLD.zks';$w.nativeWrite.path=Join-Path (Join-Path $c.checkpoint.profile 'Saved Games') 'Manual_304_KMC_C7_SEED_WORLD.zks';$script:hashes[$w.nativeWrite.path]='3'*64}
  'a Manual that skips the native next number'={param($c)$w=$c.checkpoint.artifact.observations;$w.ownedOutputAdmission.number=305;$w.ownedOutputAdmission.leaf='Manual_305_KMC_C7_SEED_CHECKPOINT.zks';$w.nativeWrite.path=Join-Path (Join-Path $c.checkpoint.profile 'Saved Games') 'Manual_305_KMC_C7_SEED_CHECKPOINT.zks';$script:hashes[$w.nativeWrite.path]='3'*64}
  'cross-ancestry (an acquired-ancestry producer)'={param($c)$c.checkpoint.request.ownedPairs.case='persistence-save'}
  'a slot checkpoint for a combat case'={param($c)$c.persistenceCase='multi-exploration-quick'}
 }
 foreach($fault in $combatCheckpointFaults.Keys){Check ('owner-seed combat checkpoint consumer refuses '+$fault+' '+$checkpointCase) {$c=New-SeedCheckpointLoadSource $checkpointCase;&$combatCheckpointFaults[$fault] $c;[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $null)} $true}
}
Check 'schema-3 pending control selects the unchanged pending-only producer Manual' {$c=New-SeedCheckpointLoadSource 'pending-schema3-control-manual';$v=Qualify-Combat $c
 if($c.selection.expectedNativeWriteCount-ne0-or$v.descriptor.internalName-cne'KMC_C7_SEED_CHECKPOINT'-or$v.descriptor.fileName-cne'Manual_304_KMC_C7_SEED_CHECKPOINT.zks'-or$v.descriptor.sha256-cne('3'*64)-or$c.checkpoint.request.persistenceCase-cne'legacy-mounted-pending-tb-manual'){throw 'Schema-3 pending control lost its exact unconverted source or zero-write quota'}}
$checkpointSaveFaults=[ordered]@{
 'missing consumer receipt'={param($c)$c.selection.checkpointSourceRunId='synthetic-absent-consumer'}
 'wrong consumer receipt'={param($c)$c.selection.checkpointSourceArtifactSha256='e'*64}
 'wrong ancestry (producer is exploration only)'={param($c)$c.producer.request.ownedPairs.case='owner-seed-exploration';$c.producer.request.ownedPairs.expectedNativeWriteCount=0}
 'wrong ancestry (consumer is another kind)'={param($c)$c.consumer.request.ownedPairs.case='persistence-load'}
 'Manual hash mismatch'={param($c)$c.selection.sourceArchiveSha256='e'*64}
 'a Manual written instead of a Quick'={param($c)$c.persistenceCase='multi-exploration-manual'}
 'an unconnected checkpoint'={param($c)$c.persistenceCase='multi-area-autosave'}
 'no checkpoint'={param($c)$c.persistenceCase=$null}
 'a zero write quota'={param($c)$c.selection.expectedNativeWriteCount=0}
 'a two-write quota'={param($c)$c.selection.expectedNativeWriteCount=2}
 'a companion'={param($c)$c.selection|Add-Member -NotePropertyName companionId -NotePropertyValue $script:companion}
 'consumer pinned another producer'={param($c)$c.consumer.request.ownedPairs.sourceRunId='synthetic-other-producer'}
}
foreach($fault in $checkpointSaveFaults.Keys){Check ('owner-seed checkpoint producer refuses '+$fault) {$c=New-SeedCheckpointSaveSource;&$checkpointSaveFaults[$fault] $c;[void](Qualify-Combat $c)} $true}
$checkpointLoadFaults=[ordered]@{
 'missing producer receipt'={param($c)$c.selection.checkpointSourceRunId='synthetic-absent-checkpoint'}
 'wrong producer evidence pin'={param($c)$c.selection.checkpointSourceArtifactSha256='e'*64}
 'producer FAIL'={param($c)$c.checkpoint.result.status='FAIL'}
 'producer wrote twice'={param($c)$c.checkpoint.game.saveRequestCount=2;$c.checkpoint.game.workingSaveRequestCount=2}
 'producer wrote nothing'={param($c)$c.checkpoint.game.saveRequestCount=0;$c.checkpoint.game.workingSaveRequestCount=0}
 'wrong ancestry (producer is derived combat)'={param($c)$c.checkpoint.request.ownedPairs.case='owner-seed-combat-rider-first'}
 'wrong ancestry (producer pinned another Manual producer)'={param($c)$c.checkpoint.request.ownedPairs.sourceRunId='synthetic-other-producer'}
 'producer lost its accepted consumer receipt'={param($c)$c.checkpoint.request.ownedPairs.checkpointSourceRunId='synthetic-absent-consumer'}
 'producer loaded something other than the Manual'={param($c)$c.checkpoint.request.persistenceLoad.sha256='e'*64}
 'producer other checkpoint'={param($c)$c.checkpoint.request.persistenceCase='multi-exploration-auto'}
 'producer owner checkpoint'={param($c)$c.checkpoint.owner.persistenceCase='multi-exploration-manual'}
 'producer other roster'={param($c)$c.checkpoint.request.ownedPairs.partyIds=@($c.checkpoint.request.ownedPairs.partyIds|Select-Object -First 4)}
 'checkpoint outside producer root'={param($c)$c.checkpoint.artifact.observations.nativeWrite.path='C:\foreign\Quick_1.zks';$script:hashes['C:\foreign\Quick_1.zks']='3'*64}
 'checkpoint bytes changed'={param($c)$script:hashes[$c.checkpoint.artifact.observations.nativeWrite.path]='e'*64}
 'checkpoint aliases the Manual'={param($c)$c.checkpoint.artifact.observations.nativeWrite.sha256='5'*64;$script:hashes[$c.checkpoint.artifact.observations.nativeWrite.path]='5'*64}
 'a Manual checkpoint'={param($c)$c.persistenceCase='multi-exploration-manual';$c.checkpoint.request.persistenceCase='multi-exploration-manual';$c.checkpoint.owner.persistenceCase='multi-exploration-manual'}
 'a write quota'={param($c)$c.selection.expectedNativeWriteCount=1}
 'producer artifact refused'={param($c)$script:failure='strict-checkpoint-artifact'}
 'producer receipt drift'={param($c)$script:drift=Join-Path $c.checkpoint.root 'chunk7-persistence-evidence.json'}
 'an area target'={param($c)$c|Add-Member -NotePropertyName areaTarget -NotePropertyValue $true}
}
foreach($fault in $checkpointLoadFaults.Keys){Check ('owner-seed checkpoint consumer refuses '+$fault) {$c=New-SeedCheckpointLoadSource;&$checkpointLoadFaults[$fault] $c;$area=if($null-ne$c.PSObject.Properties['areaTarget']){[pscustomobject]@{enterPoint=('e'*32);area=('f'*32);autoSaveMode='AfterEntry'}}else{$null};[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $area)} $true}
# The controlled owner-seed cross-entry retains the original Manual lineage,
# and its cold child consumes only the producer's genuine destination Auto.
function New-SeedAreaSource([bool]$Cold){
 $c=if($Cold){New-SeedCheckpointLoadSource 'multi-exploration-auto'}else{New-SeedCheckpointSaveSource 'multi-exploration-auto'}
 $c.persistenceCase='multi-area-autosave';$c.selection|Add-Member sourceArea 'b10e86516b6437d45a629931c30936fe'
 $t=[pscustomobject]@{enterPoint='fd225a93828d26449803a4847947f8c1';area='ead426a6c23d39548a670ee515d77df4';autoSaveMode='AfterEntry'};$c|Add-Member areaTarget $t
 if($Cold){$r=$c.checkpoint.request;$r.persistenceCase=$c.persistenceCase;$c.checkpoint.owner.persistenceCase=$c.persistenceCase;$r.ownedPairs|Add-Member sourceArea $c.selection.sourceArea;$r|Add-Member persistenceAreaTarget ($t|ConvertTo-Json|ConvertFrom-Json);$c.checkpoint.artifact.observations.nativeWrite.archive.AreaId=$t.area;$c|Add-Member checkpointProfile (Add-AreaProfile $c.checkpoint)}
 return $c
}
foreach($cold in @($false,$true)){Check ('owner-seed controlled source and actual destination Auto '+$cold){$c=New-SeedAreaSource $cold;$v=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.areaTarget
 if($v.descriptor.area-cne$(if($cold){$c.areaTarget.area}else{$c.selection.sourceArea})-or$v.descriptor.fileName-cne$(if($cold){'Auto_1.zks'}else{'Manual_303_KMC_C7_SEED_WORLD.zks'})-or$v.artifactSha256-cne('6'*64)){throw 'Controlled entry substituted source, destination or original Manual lineage.'}
 foreach($gate in @('owner-seed-registry-master-intake-header','strict-manifest','strict-producer-world-artifact','strict-checkpoint-artifact','consumer-cold-proof')){if(-not$script:calls.Contains($gate)){throw ('Controlled entry skipped required lineage/artifact gate '+$gate)}}}}
$seedAreaFaults=[ordered]@{
 'source area override'={param($c)$c.selection.sourceArea='e'*32}
 'destination entry override'={param($c)$c.areaTarget.enterPoint='e'*32}
 'destination area override'={param($c)$c.areaTarget.area='f'*32}
 'before exit mode'={param($c)$c.areaTarget.autoSaveMode='BeforeExit'}
 'target missing'={param($c)$c.areaTarget=$null}
 'parent original area override'={param($c)$c.checkpoint.request.ownedPairs.sourceArea='d'*32}
 'parent entry override'={param($c)$c.checkpoint.request.persistenceAreaTarget.enterPoint='c'*32}
 'parent area override'={param($c)$c.checkpoint.request.persistenceAreaTarget.area='a'*32}
 'native Auto still source area'={param($c)$c.checkpoint.artifact.observations.nativeWrite.archive.AreaId=$c.selection.sourceArea}
 'profile bytes unrestored'={param($c)$c.checkpointProfile.profileBytesRestoredExactly=$false}
 'profile accepted cache change'={param($c)$c.checkpointProfile.acceptedNativeCacheChanges=@('changed')}
 'profile receipt drifts'={param($c)$script:drift=Join-Path $c.checkpoint.root 'profile-cache-changes.json'}
 'producer had second load'={param($c)$c.checkpoint.game.loadRequestCount=2;$c.checkpoint.game.workingLoadRequestCount=2}
 'producer had second write'={param($c)$c.checkpoint.game.saveRequestCount=2;$c.checkpoint.game.workingSaveRequestCount=2}
 'producer artifact rejected'={param($c)$script:failure='strict-checkpoint-artifact'}
}
foreach($m in $seedAreaFaults.GetEnumerator()){Check ('owner-seed controlled source refuses '+$m.Key){$c=New-SeedAreaSource $true;&$m.Value $c;[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate $c.areaTarget)} $true}
Check 'owner-seed three-load source admits original Manual from its recorded prior candidate' {$c=New-SeedDerivedSource;$c.persistenceCase='multi-exploration-three-loads-manual';$c.selection.expectedNativeLoadCount=3;$c.candidate.commit='f'*40;$c.candidate.dllSha256='f'*64;$c.candidate.dllMvid='ffffffff-ffff-4fff-8fff-ffffffffffff';$v=Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate
 if($v.descriptor.fileName-cne'Manual_303_KMC_C7_SEED_WORLD.zks'-or$v.descriptor.sha256-cne$c.selection.sourceArchiveSha256-or$c.producer.request.commit-ceq$c.candidate.commit){throw 'Repeated load source changed native Manual or relabelled producer candidate.'};foreach($gate in @('owner-seed-registry-master-intake-header','strict-producer-world-artifact','strict-manifest')){if(-not$script:calls.Contains($gate)){throw ('Repeated load omitted source gate '+$gate)}}}
foreach($fault in @('one-load','four-loads','write','checkpoint-parent','area-transfer-family','wrong-input-pin','source-wrote-twice','source-failed','source-unrestored','source-candidate-mismatch')){Check ('owner-seed three-load source refuses '+$fault){$c=New-SeedDerivedSource;$c.persistenceCase='multi-exploration-three-loads-manual';$c.selection.expectedNativeLoadCount=3;switch($fault){'one-load'{$c.selection.expectedNativeLoadCount=1};'four-loads'{$c.selection.expectedNativeLoadCount=4};'write'{$c.selection.expectedNativeWriteCount=1};'checkpoint-parent'{$c.selection|Add-Member checkpointSourceRunId 'unexpected-parent';$c.selection|Add-Member checkpointSourceArtifactSha256 ('8'*64)};'area-transfer-family'{$c.persistenceCase='multi-exploration-three-area-transfers-manual'};'wrong-input-pin'{$c.selection.sourceArchiveSha256='e'*64};'source-wrote-twice'{$c.producer.game.saveRequestCount=2;$c.producer.game.workingSaveRequestCount=2};'source-failed'{$c.producer.result.status='FAIL'};'source-unrestored'{$c.producer.result.modsRestored=$false};'source-candidate-mismatch'{$c.producer.result.dllSha256='f'*64}};[void](Get-KmcChunk7OwnedPairsSource $c.selection $c.persistenceCase $c.fixture $c.candidate)} $true}
foreach($checkpointCase in @('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual')){
 Check ('owner-seed boundary producer qualifies its exact Manual lineage '+$checkpointCase){$c=New-SeedCheckpointSaveSource $checkpointCase;$v=Qualify-Combat $c;if($v.descriptor.internalName-cne'KMC_C7_SEED_WORLD'-or$v.consumerRunId-cne'synthetic-owner-seed-cold'){throw 'Save boundary changed its accepted Manual/cold lineage.'}}
 Check ('owner-seed boundary cold selection remains refused '+$checkpointCase){$c=New-SeedCheckpointSaveSource $checkpointCase;$c.selection.case='owner-seed-checkpoint-load';$c.selection.expectedNativeWriteCount=0;[void](Qualify-Combat $c)} $true
 Check ('owner-seed boundary producer wrong quota refused '+$checkpointCase){$c=New-SeedCheckpointSaveSource $checkpointCase;$c.selection.expectedNativeWriteCount=3;[void](Qualify-Combat $c)} $true
}
Check 'owner-seed legacy cold requires its one current-format resave' {$c=New-SeedCheckpointLoadSource 'legacy-mounted-pending-tb-manual';$c.selection.expectedNativeWriteCount=0;[void](Qualify-Combat $c)} $true
Check 'owner-seed schema1 source uses the accepted Manual and cold lineage' {$c=New-SeedCheckpointSaveSource 'owned-schema1-manual';$v=Qualify-Combat $c;if($v.descriptor.internalName-cne'KMC_C7_SEED_WORLD'){throw 'Schema1 source changed its original Manual.'}}
Check 'owner-seed historical schema1 intake resolves its exact native one-owner producer' {$c=New-SeedCheckpointLoadSource 'owned-schema1-resave-manual';$v=Qualify-Combat $c;if($v.descriptor.internalName-cne'KMC_C7_SEED_CHECKPOINT'-or$c.checkpoint.request.persistenceCase-cne'owned-schema1-manual'){throw 'Historical schema1 intake did not resolve its native source.'}}
# These established evidence validators are tested with complete generated graphs
# by Test-Chunk7OwnedPairsEvidence. Here their boundary is explicit; the public
# source intake, receipt completion, actual synthetic ZIP read and hashing remain real.
. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1')
function Assert-KmcChunk7OwnerSeedCheckpointColdSource($Request,$Producer,$Cold){Boundary 'historical-native-cold-and-resave';if($Request.persistenceCase-cnotin@('owned-schema1-resave-manual','legacy-mounted-pending-tb-manual')-or$Cold.observations.cold-ne$true-or$Cold.observations.nativeWrite.sourceSchema-ne3){throw 'Historical cold proof bound incorrectly.'}}
function Assert-KmcOwnedSchema1ColdSource($Request,$Source,$Artifact){Boundary 'schema1-actual-derivation-qualified';if($Request.persistenceLoad.sha256-ceq$Source.descriptor.sha256-or$Request.schema1DerivationSha256-cne('a'*64)){throw 'Schema1 historical input is not its receipted derivative.'}}
function Assert-KmcOwnedSchema2ColdSource($Request,$Source,$Artifact){Boundary 'schema2-actual-derivation-qualified';if($Request.persistenceLoad.sha256-ceq$Source.descriptor.sha256-or$Request.schema2DerivationSha256-cne('a'*64)){throw 'Schema2 historical input is not its receipted derivative.'}}
function New-CurrentSource([string]$Case){
 $historical=Get-KmcChunk7OwnerSeedCurrentParentCase $Case;$c=New-SeedCheckpointLoadSource $historical;$original=Qualify-Combat $c
 $pr=$c.checkpoint.request|ConvertTo-Json -Depth 30|ConvertFrom-Json;$pr.runId='synthetic-current-parent-'+[Guid]::NewGuid().ToString('N');$pr.ownedPairs=$c.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json;$pr.persistenceCase=$historical;$pr.persistenceLoad=$original.descriptor|ConvertTo-Json -Depth 10|ConvertFrom-Json
 if($historical-ceq'owned-schema1-resave-manual'){$pr.persistenceLoad.sha256='1'*64;$pr|Add-Member schema1DerivationSha256 ('a'*64)}
 else{$pr.persistenceLoad.sha256='1'*64;$pr|Add-Member schema2DerivationSha256 ('a'*64)}
 $name=if($historical-ceq'owned-schema1-resave-manual'){'KMC_C7_SCHEMA1'}else{'KMC_C7_SCHEMA2'};$leaf='Manual_305_'+$name+'.zks'
 $graph=[pscustomobject]@{SchemaVersion=3;AreaId=$original.descriptor.area;syntheticSourceOnly=$true}
 $artifact=[pscustomobject]@{runId=$pr.runId;status='PASS';observations=[pscustomobject]@{checkpoint=$historical;cold=$true;processId=700;ownedOutputAdmission=[pscustomobject]@{number=305;leaf=$leaf};nativeWrite=[pscustomobject]@{path='';nativeName=$name;nativeType='Manual';sourceSchema=3;sha256='';length=0;archive=$graph}}}
 $set=Add-Set $pr $artifact 'chunk7-persistence-evidence.json' ('4'*64) $pr.persistenceLoad.sha256 $historical
 $saveDir=Join-Path $set.profile 'Saved Games';[void][IO.Directory]::CreateDirectory($saveDir);$path=Join-Path $saveDir $leaf
 Add-Type -AssemblyName System.IO.Compression
 $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$zip=$null
 try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false);foreach($entrySpec in @(@('header.json','{}'),@('kmc-mounted-state',($graph|ConvertTo-Json -Compress)))){$entry=$zip.CreateEntry($entrySpec[0]);$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false));try{$writer.Write($entrySpec[1])}finally{$writer.Dispose()}}}finally{if($zip){$zip.Dispose()};$stream.Dispose()}
 $sha=[Security.Cryptography.SHA256]::Create();$stream=[IO.File]::OpenRead($path);try{$digest=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose();$stream.Dispose()}
 $actual=[IO.FileInfo]::new($path);$script:hashes[$path]=$digest;$script:realMetadata[$path]=$actual;$artifact.observations.nativeWrite.path=$path;$artifact.observations.nativeWrite.sha256=$digest;$artifact.observations.nativeWrite.length=$actual.Length
 $s=$c.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json;$s.checkpointSourceRunId=$pr.runId;$s.checkpointSourceArtifactSha256='4'*64;$s.expectedNativeWriteCount=0
 [pscustomobject]@{selection=$s;persistenceCase=$Case;fixture=$c.fixture;candidate=$c.candidate;parent=$set;original=$original}
}
foreach($case in @('owned-schema1-current-manual','owned-schema2-pending-current-manual')){
 Check ('public current-format source intake qualifies accepted native resave '+$case){$c=New-CurrentSource $case;$v=Qualify-Combat $c;if($v.descriptor.sha256-cne$c.parent.artifact.observations.nativeWrite.sha256-or$v.descriptor.internalName-cnotin@('KMC_C7_SCHEMA1','KMC_C7_SCHEMA2')-or$v.checkpointRunId-cne$c.parent.request.runId){throw 'Current source lost its exact native output or parent.'};if(-not$script:calls.Contains('historical-native-cold-and-resave')-or($case-ceq'owned-schema1-current-manual'-and-not$script:calls.Contains('schema1-actual-derivation-qualified'))){throw 'Current source omitted historical requalification.'}}
 Check ('public current-format source preserves recorded parent candidate '+$case){$c=New-CurrentSource $case;$c.candidate.commit='f'*40;$c.candidate.dllSha256='f'*64;$c.candidate.dllMvid='ffffffff-ffff-4fff-8fff-ffffffffffff';$v=Qualify-Combat $c;if($v.checkpointCandidate.commit-ceq$c.candidate.commit){throw 'Current child relabelled historical evidence as the current candidate.'}}
 foreach($fault in @('wrong-parent-direction','wrong-parent-case','failed-parent','wrong-parent-owner-input-hash','wrong-parent-write-quota','wrong-parent-pin','changed-native-output','noncurrent-metadata','actual-metadata-differs','escaped-native-output','missing-historical-proof')){
  Check ('public current-format source refuses '+$fault+' '+$case){$c=New-CurrentSource $case
   switch($fault){'wrong-parent-direction'{$c.parent.request.ownedPairs.case='owner-seed-checkpoint-save'}'wrong-parent-case'{$c.parent.request.persistenceCase='multi-tb-debt-manual'}'failed-parent'{$c.parent.result.status='FAIL'}'wrong-parent-owner-input-hash'{$c.parent.owner.sourceSha256=$c.selection.sourceArchiveSha256}'wrong-parent-write-quota'{$c.parent.game.saveRequestCount=0}'wrong-parent-pin'{$c.selection.checkpointSourceArtifactSha256='e'*64}'changed-native-output'{$c.parent.artifact.observations.nativeWrite.sha256='e'*64}'noncurrent-metadata'{$c.parent.artifact.observations.nativeWrite.archive.SchemaVersion=2}'actual-metadata-differs'{$c.parent.artifact.observations.nativeWrite.archive.AreaId='f'*32}'escaped-native-output'{$c.parent.artifact.observations.nativeWrite.path='C:\foreign\Manual_305_KMC_C7_SCHEMA1.zks'}'missing-historical-proof'{$script:failure='historical-native-cold-and-resave'}}
   [void](Qualify-Combat $c)
  } $true
 }
}
$copyPrototypes=@(foreach($case in @('owned-schema1-current-manual','owned-schema2-pending-current-manual')){$c=New-CurrentSource $case;$source=Qualify-Combat $c;[pscustomobject]@{case=$case;selection=$c.selection;source=$source;parent=$c.parent.artifact;request=$c.parent.request}})
# The parent-copy checks use real generated files and hashes. Only the already
# tested lock owner and link/directory observations remain external facades.
function Read-KmcJson($Path){[IO.File]::ReadAllText($Path)|ConvertFrom-Json}
function Get-Item([string]$LiteralPath){[IO.FileInfo]::new($LiteralPath)}
function Get-KmcSha256($Path){$stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose();$stream.Dispose()}}
function Assert-KmcRuntimeLockOwner($Lock){if($null-eq$Lock-or$Lock.Token-cne('1'*64)-or$Lock.RunId-isnot[string]){throw 'Generated copy lock is not owned.'};$true}
$script:failure=$null;$script:calls=[Collections.Generic.List[string]]::new()
function New-CurrentParentCopyRig($Prototype){
 $source=$Prototype.source|ConvertTo-Json -Depth 30|ConvertFrom-Json
 [void][IO.Directory]::CreateDirectory((Split-Path $source.checkpointArtifactPath -Parent));[IO.File]::WriteAllText($source.checkpointArtifactPath,($Prototype.parent|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
 $source.checkpointArtifactSha256=Get-KmcSha256 $source.checkpointArtifactPath;$source.checkpointArtifactLength=([IO.FileInfo]::new($source.checkpointArtifactPath)).Length
 $request=$Prototype.request|ConvertTo-Json -Depth 30|ConvertFrom-Json;$request.runId='synthetic-current-copy-'+[Guid]::NewGuid().ToString('N');$request.persistenceCase=$Prototype.case;$request.persistenceLoad=$source.descriptor;$request.ownedPairs=$Prototype.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json;$request.ownedPairs.checkpointSourceArtifactSha256=$source.checkpointArtifactSha256;$request.PSObject.Properties.Remove('schema1DerivationSha256');$request.PSObject.Properties.Remove('schema2DerivationSha256')
 $lock=[pscustomobject]@{RunId=$request.runId;Token=('1'*64)};$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$request.runId);[void][IO.Directory]::CreateDirectory((Join-Path $profile 'Saved Games'))
 $owner=[pscustomobject]@{runId=$request.runId;scenario=$request.scenario;transactionToken=$lock.Token;persistenceCase=$request.persistenceCase;sourceSha256=$request.persistenceLoad.sha256;saveRoot=(Join-Path $profile 'Saved Games')}
 $ownerPath=Join-Path $profile 'owner.json';[IO.File]::WriteAllText($ownerPath,($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
 [pscustomobject]@{source=$source;request=$request;lock=$lock;profile=$profile;owner=$owner;ownerPath=$ownerPath;destination=(Join-Path $profile 'owner-seed-current-parent.json')}
}
foreach($prototype in $copyPrototypes){
 $rig=New-CurrentParentCopyRig $prototype
 Check ('guarded current parent CREATE_NEW and byte hashes '+$prototype.case){Copy-KmcChunk7OwnerSeedCurrentParent $rig.source $rig.profile $rig.lock $rig.request;if((Get-KmcSha256 $rig.destination)-cne$rig.source.checkpointArtifactSha256-or(Get-KmcSha256 $rig.source.checkpointArtifactPath)-cne$rig.source.checkpointArtifactSha256){throw 'Guarded parent copy changed source or destination bytes.'}}
 Check ('guarded current parent refuses existing destination '+$prototype.case){Copy-KmcChunk7OwnerSeedCurrentParent $rig.source $rig.profile $rig.lock $rig.request} $true
 foreach($fault in @('owner-token','foreign-profile','unselected-current-case','wrong-parent-pin','changed-parent-bytes')){
  Check ('guarded current parent refuses '+$fault+' '+$prototype.case){$bad=New-CurrentParentCopyRig $prototype;$originalBytes=[IO.File]::ReadAllBytes($bad.source.checkpointArtifactPath)
   try{
    switch($fault){'owner-token'{$bad.owner.transactionToken='0'*64;[IO.File]::WriteAllText($bad.ownerPath,($bad.owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))}'foreign-profile'{$bad.profile=Join-Path $script:virtualLab 'runtime-staging/persistence-foreign';[void][IO.Directory]::CreateDirectory($bad.profile)}'unselected-current-case'{$bad.request.ownedPairs.case='owner-seed-checkpoint-save';$bad.request.ownedPairs.expectedNativeWriteCount=1}'wrong-parent-pin'{$bad.request.ownedPairs.checkpointSourceArtifactSha256='f'*64}'changed-parent-bytes'{[IO.File]::AppendAllText($bad.source.checkpointArtifactPath,' ',[Text.UTF8Encoding]::new($false))}}
    $errorText=$null;try{Copy-KmcChunk7OwnerSeedCurrentParent $bad.source $bad.profile $bad.lock $bad.request}catch{$errorText=$_.Exception.Message}
    if($null-eq$errorText-or[IO.File]::Exists($bad.destination)){throw 'Guarded parent copy accepted an invalid authority or wrote before refusal.'}
   }finally{[IO.File]::WriteAllBytes($bad.source.checkpointArtifactPath,$originalBytes)}
  }
 }
}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $testOutput 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archiveRead=$true;scope='actual public owned qualifier; generated current-format ZIPs only; established lineage/artifact validators and remaining OS observations substituted';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Owned source: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
