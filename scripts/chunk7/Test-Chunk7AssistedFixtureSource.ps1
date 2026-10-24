[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/assisted-source-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh private generated-fixture output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$sourceTestOutput=$OutputDirectory
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedFixtureEvidence.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedFixtureFixtures.ps1')
$script:sourcePass=0;$script:sourceFail=0;$script:sourceCase=0;$script:sourceLines=[Collections.Generic.List[string]]::new()
$script:sourceCalls=[Collections.Generic.List[string]]::new();$script:sourceBoundaryFail=$null;$script:sourceMutation=$null
# These three completion/native boundaries are deliberately substituted. Real
# path, JSON duplicate-member, file metadata and whole-byte hash helpers run.
function Get-KmcLabRoot {$script:sourceLab}
function Assert-KmcReadOnlyArtifactManifest($Request,$ExpectedSha256,[switch]$GameResult){
 $script:sourceCalls.Add('manifest');if(-not$GameResult-or$ExpectedSha256-cne(Get-KmcSha256 (Join-Path $Request.evidenceRoot 'runtime-artifacts.json'))){throw 'Test boundary received wrong manifest binding.'}
 if($script:sourceBoundaryFail-ceq'manifest'){throw 'Explicit generated manifest boundary refusal'}
}
function Assert-KmcChunk7AssistedFixtureEvidence($Request,$Artifact,[string]$Status='PASS'){
 $script:sourceCalls.Add('evidence');if($Status-cne'PASS'-or$Request.runId-cne$Artifact.runId-or$Artifact.status-cne'PASS'){throw 'Test boundary received wrong native artifact binding.'}
 if($script:sourceBoundaryFail-ceq'evidence'){throw 'Explicit generated native evidence refusal'}
}
function Assert-KmcChunk7AssistedFixtureArtifact($Request,$Manifest,[string]$Status='PASS'){
 $script:sourceCalls.Add('artifact');if($Status-cne'PASS'-or$Manifest.runId-cne$Request.runId){throw 'Test boundary received wrong artifact manifest.'}
 if($script:sourceBoundaryFail-ceq'artifact'){throw 'Explicit generated full artifact refusal'}
 Assert-KmcChunk7AssistedFixtureEvidence $Request (Read-KmcJson (Join-Path $Request.evidenceRoot 'chunk7-assisted-fixture-evidence.json')) $Status
}
function Assert-KmcChunk7AssistedFixtureArchive($Request,$Artifact){
 $script:sourceCalls.Add('archive');Assert-KmcChunk7AssistedFixtureOutput $Request $Artifact
 if($script:sourceBoundaryFail-ceq'archive'){throw 'Explicit generated archive boundary refusal'}
 if($null-ne$script:sourceMutation){&$script:sourceMutation}
}
function Check-AssistedSource([string]$Label,[scriptblock]$Action,[bool]$Reject=$false){
 $caught=$null;try{&$Action}catch{$caught=$_};$ok=if($Reject){$null-ne$caught}else{$null-eq$caught}
 if($ok){$script:sourcePass++;$line='PASS '+$Label}else{$script:sourceFail++;$line='FAIL '+$Label+': '+$(if($null-eq$caught){'unexpected acceptance'}else{$caught.ToString()+' '+$caught.ScriptStackTrace})}
 $script:sourceLines.Add($line);Write-Host $line
}
function Need-AssistedSource([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
function Write-GeneratedJson([string]$Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))}
function Clone-Generated($Value){$Value|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Generated-Descriptor([string]$Path,[string]$Name,[string]$GameId,[string]$Area){$f=Get-Item -LiteralPath $Path;[pscustomobject]@{internalName=$Name;fileName=$f.Name;sha256=Get-KmcSha256 $Path;length=[long]$f.Length;lastWriteTimeUtcTicks=[long]$f.LastWriteTimeUtc.Ticks;gameId=$GameId;gameName='Generated disposable assisted fixture';area=$Area}}
function New-GeneratedSource([int]$Writes=2){
 $script:sourceCase++;$script:sourceLab=Join-Path $sourceTestOutput ('case'+$script:sourceCase);$script:sourceCalls.Clear();$script:sourceBoundaryFail=$null;$script:sourceMutation=$null
 $run='generated-assisted-source';$profile=Join-Path $script:sourceLab ('runtime-staging/persistence-'+$run);$saves=Join-Path $profile 'Saved Games';$evidence=Join-Path $script:sourceLab ('runtime-evidence/'+$run)
 [void][IO.Directory]::CreateDirectory($saves);[void][IO.Directory]::CreateDirectory($evidence)
 $gameId='11111111-2222-4333-8444-555555555555';$area='1'*32
 $anchorPath=Join-Path $saves 'Manual_300_KMC_AUTOMATION_WORKING.zks';$autoPath=Join-Path $saves 'Auto_1.zks';$manualPath=Join-Path $saves 'Manual_301_KMC_C7_ASSISTED_FIXTURE.zks'
 foreach($pair in @(@($anchorPath,'generated anchor bytes'),@($autoPath,('generated settled Auto '+$Writes)),@($manualPath,('generated settled Manual '+$Writes)))){[IO.File]::WriteAllBytes($pair[0],[Text.Encoding]::UTF8.GetBytes($pair[1]))}
 $anchor=Generated-Descriptor $anchorPath 'KMC_AUTOMATION_WORKING' '66666666-7777-4888-8999-aaaaaaaaaaaa' ('2'*32)
 $baseline=Clone-Generated $anchor;$baseline.internalName='KMC_AUTOMATION_BASELINE';$baseline.fileName='Manual_299_KMC_AUTOMATION_BASELINE.zks';$baseline.sha256='b'*64
 $fixture=[pscustomobject]@{working=$anchor;baseline=$baseline}
 $candidate=[pscustomobject]@{commit='a'*40;dllSha256='d'*64;dllMvid='88888888-9999-4aaa-8bbb-cccccccccccc'}
 $selection=[pscustomobject]@{case='authored-main-ranger4-manual';ownerAuthorizationSha256='052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e';baseGameOnly=$true;assistedSetupOnly=$true;storyPolicy='first-unused-eligible-row-else-root-system-button';assistancePolicy='native-main-ranger4-and-one-hire-shortfalls';expectedNativeLoadCount=0;maximumNativeWriteCount=3;targetRangerLevel=4;maximumTargetLevel=8;maximumStoryChoices=48;maximumStorySeconds=300;maximumExperienceShortfall=200000;maximumMoneyShortfall=100000;nativeInitialAutoLeaf='Auto_1.zks';nativeManualName='KMC_C7_ASSISTED_FIXTURE'}
 $request=[pscustomobject]@{schemaVersion=2;runId=$run;scenario='chunk7-assisted-fixture-preparation';evidenceRoot=$evidence;branch='codex/generated-boundary-only';commit=$candidate.commit;dllSha256=$candidate.dllSha256;dllMvid=$candidate.dllMvid;productVersion='generated-only';transactionToken='e'*64;fixture=$fixture;assistedFixture=$selection}
 $auto=Generated-Descriptor $autoPath 'Autosave' $gameId $area
 $auto|Add-Member NoteProperty path $autoPath;$auto|Add-Member NoteProperty phase $(if($Writes-eq3){'levelup-auto'}else{'initial-auto'});$auto|Add-Member NoteProperty type 'Auto';$auto|Add-Member NoteProperty autoLevelup $false;$auto|Add-Member NoteProperty operation 'None'
 $manual=Generated-Descriptor $manualPath 'KMC_C7_ASSISTED_FIXTURE' $gameId $area
 $manual|Add-Member NoteProperty path $manualPath;$manual|Add-Member NoteProperty phase 'final-manual';$manual|Add-Member NoteProperty type 'Manual';$manual|Add-Member NoteProperty autoLevelup $false;$manual|Add-Member NoteProperty operation 'None';$manual|Add-Member NoteProperty authority ([pscustomobject]@{manualNumber=301})
 $nativeWrites=if($Writes-eq3){$initial=Clone-Generated $auto;$initial.phase='initial-auto';$initial.autoLevelup=$false;$initial.sha256='c'*64;@($initial,$auto,$manual)}else{@($auto,$manual)}
 $artifact=[pscustomobject]@{schemaVersion=1;runId=$run;scenario=$request.scenario;branch=$request.branch;commit=$request.commit;productVersion=$request.productVersion;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid;evidenceKind='chunk7-assisted-fixture-evidence';createdAtUtc='2026-10-13T01:00:00Z';status='PASS';errors=@();observations=[pscustomobject]@{nativeWrites=$nativeWrites;finalManual=$manual}}
 $baseReceipt=[ordered]@{schemaVersion=2;runId=$run;scenario=$request.scenario;branch=$request.branch;commit=$request.commit;productVersion=$request.productVersion;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid;transactionToken=$request.transactionToken;status='PASS';errors=@();assertionFailCount=0;subscenarioFailCount=0;evidenceManifestSha256='0'*64}
 $game=Clone-Generated $baseReceipt
 foreach($key in @('loadRequestCount','workingLoadRequestCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){$game|Add-Member NoteProperty $key 0}
 $game|Add-Member NoteProperty fixtureIdentityVerified $false;foreach($key in @('saveRequestCount','workingSaveRequestCount')){$game|Add-Member NoteProperty $key $Writes}
 $result=Clone-Generated $baseReceipt
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){$result|Add-Member NoteProperty $key $true}
 $result|Add-Member NoteProperty gameResultSha256 ('0'*64);$result|Add-Member NoteProperty restoredSaveInventoryDigest ('f'*64)
 $owner=[pscustomobject]@{runId=$run;scenario=$request.scenario;transactionToken=$request.transactionToken;persistenceCase=$null;sourceSha256=$anchor.sha256;saveRoot=$saves}
 $isolation=[pscustomobject]@{schemaVersion=1;runId=$run;stagingMode='base-game-only-kmc';stageVerified=$true;dependenciesUnchanged=$true;processExited=$true;modsRestored=$true;saveProtectionPassed=$true;gameReportedPass=$true;nativeInventoryObserved=$true;nativeCompatibilityQualified=$true}
 $profileReceipt=[pscustomobject]@{schemaVersion=1;runId=$run;profileRoot='generated external-profile label only';snapshotProfileDigest='9'*64;policy='native-achievement-cache-settled-size';acceptedNativeCacheChanges=@();profileBytesRestoredExactly=$true}
 $f=[pscustomobject]@{request=$request;artifact=$artifact;game=$game;result=$result;owner=$owner;isolation=$isolation;profileReceipt=$profileReceipt;profile=$profile;root=$evidence;fixture=$fixture;candidate=$candidate;selection=[pscustomobject]@{sourceRunId=$run;sourceArtifactSha256='0'*64;sourceArchiveSha256=$manual.sha256}}
 Flush-GeneratedSource $f;$f
}
function Flush-GeneratedSource($F){
 $artifactPath=Join-Path $F.root 'chunk7-assisted-fixture-evidence.json';Write-GeneratedJson $artifactPath $F.artifact
 $hash=Get-KmcSha256 $artifactPath;$F.selection.sourceArtifactSha256=$hash
 $manifest=[pscustomobject]@{schemaVersion=1;runId=$F.request.runId;scenario=$F.request.scenario;createdAtUtc='2026-10-13T01:00:00Z';artifacts=@([pscustomobject]@{relativePath='chunk7-assisted-fixture-evidence.json';kind='chunk7-assisted-fixture-evidence';sha256=$hash;length=[long](Get-Item -LiteralPath $artifactPath).Length})}
 Write-GeneratedJson (Join-Path $F.root 'runtime-artifacts.json') $manifest;$manifestHash=Get-KmcSha256 (Join-Path $F.root 'runtime-artifacts.json')
 $F.game.evidenceManifestSha256=$manifestHash;$F.result.evidenceManifestSha256=$manifestHash
 Write-GeneratedJson (Join-Path $F.root 'runtime-game-result.json') $F.game;$F.result.gameResultSha256=Get-KmcSha256 (Join-Path $F.root 'runtime-game-result.json')
 foreach($pair in @(@('runtime-request.json',$F.request),@('runtime-result.json',$F.result),@('base-game-isolation-result.json',$F.isolation),@('profile-cache-changes.json',$F.profileReceipt))){Write-GeneratedJson (Join-Path $F.root $pair[0]) $pair[1]}
 Write-GeneratedJson (Join-Path $F.profile 'owner.json') $F.owner
}
function Validate-GeneratedSource($F){Get-KmcChunk7AssistedFixtureSource $F.selection $F.fixture $F.candidate}
foreach($writes in @(2,3)){
 Check-AssistedSource ('exact '+$writes+' writes returns observed Manual and receipt pins'){
  $f=New-GeneratedSource $writes;$v=Validate-GeneratedSource $f
  Need-AssistedSource ($v.path-ceq$f.artifact.observations.finalManual.path-and$v.descriptor.sha256-ceq$f.selection.sourceArchiveSha256-and$v.receiptPins.Count-eq8-and$v.nativeAuthorityGranted-eq$false-and$v.fixtureSetupOnly-eq$true) 'Observed descriptor/pins differ'
  Need-AssistedSource (($script:sourceCalls-join'|')-ceq'manifest|artifact|evidence|archive|evidence|evidence') 'Required completion/archive/output boundary order differs'
 }
}
Check-AssistedSource 'exact producer empty optional persistence case' {$f=New-GeneratedSource;$f.owner.persistenceCase='';Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f}
foreach($boundary in @('manifest','artifact','evidence','archive')){Check-AssistedSource ('required '+$boundary+' refusal propagated'){$f=New-GeneratedSource;$script:sourceBoundaryFail=$boundary;$null=Validate-GeneratedSource $f} $true}
$mutations=[ordered]@{
 'foreign candidate'={param($f)$f.candidate.commit='b'*40}
 'candidate hash array'={param($f)$f.candidate.dllSha256=@($f.candidate.dllSha256)}
 'foreign source scenario'={param($f)$f.request.scenario='chunk7-native-main-bootstrap'}
 'foreign evidence root'={param($f)$f.request.evidenceRoot=Join-Path $f.root 'other'}
 'foreign Working descriptor'={param($f)$f.request.fixture=Clone-Generated $f.fixture;$f.request.fixture.working.area='3'*32}
 'foreign owner token'={param($f)$f.owner.transactionToken='c'*64}
 'owner grants foreign case'={param($f)$f.owner.persistenceCase='multi-exploration-manual'}
 'owner case array'={param($f)$f.owner.persistenceCase=@('')}
 'owner replaces anchor hash'={param($f)$f.owner.sourceSha256=$f.selection.sourceArchiveSha256}
 'result FAIL'={param($f)$f.result.status='FAIL'}
 'result error'={param($f)$f.result.errors=@('native failure')}
 'game error'={param($f)$f.game.errors=@('native failure')}
 'unrestored Mods'={param($f)$f.result.modsRestored=$false}
 'unrestored Working'={param($f)$f.result.workingRestored=$false}
 'unrestored Baseline'={param($f)$f.result.baselineImmutable=$false}
 'missing protection'={param($f)$f.result.saveProtectionPassed=$false}
 'restoration digest array'={param($f)$f.result.restoredSaveInventoryDigest=@($f.result.restoredSaveInventoryDigest)}
 'one native load'={param($f)$f.game.loadRequestCount=1}
 'claimed Working load identity'={param($f)$f.game.fixtureIdentityVerified=$true}
 'wrong actual save count'={param($f)$f.game.saveRequestCount=1}
 'coerced actual save count'={param($f)$f.game.workingSaveRequestCount='2'}
 'foreign isolation mode'={param($f)$f.isolation.stagingMode='installed-mods'}
 'process not exited'={param($f)$f.isolation.processExited=$false}
 'compatibility absent'={param($f)$f.isolation.nativeCompatibilityQualified=$false}
 'profile not exact'={param($f)$f.profileReceipt.profileBytesRestoredExactly=$false}
 'accepted cache difference'={param($f)$f.profileReceipt.acceptedNativeCacheChanges=@([pscustomobject]@{path='generated'})}
 'wrong output hash pin'={param($f)$f.selection.sourceArchiveSha256='0'*64}
}
foreach($entry in $mutations.GetEnumerator()){Check-AssistedSource $entry.Key {$f=New-GeneratedSource;&$entry.Value $f;Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f} $true}
Check-AssistedSource 'settled native achievement cache is recorded but not byte exact' {
 $f=New-GeneratedSource;$f.profileReceipt.profileBytesRestoredExactly=$false
 $f.profileReceipt.acceptedNativeCacheChanges=@([pscustomobject]@{path='achievements.dat';change='created';length=12288;beforeSha256='';afterSha256=('a'*64)})
 Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f
}
Check-AssistedSource 'pinned UTF-8 native cache name retains its bounded characters' {
 $f=New-GeneratedSource;$f.profileReceipt.profileBytesRestoredExactly=$false
 $nativeSuffix=([string][char]0xfffd)+'H'+([string][char]0xfffd)+([string][char]0xfffd)+([string][char]0xfffd)+([string][char]0xfffd)
 $f.profileReceipt.acceptedNativeCacheChanges=@([pscustomobject]@{path=('achievements.dat'+$nativeSuffix);change='rewritten';length=12288;beforeSha256=('a'*64);afterSha256=('b'*64)})
 Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f
}
Check-AssistedSource 'unapproved profile path cannot borrow cache exception' {
 $f=New-GeneratedSource;$f.profileReceipt.profileBytesRestoredExactly=$false
 $f.profileReceipt.acceptedNativeCacheChanges=@([pscustomobject]@{path='Areas';change='created';length=12288;beforeSha256='';afterSha256=('a'*64)})
 Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f
} $true
Check-AssistedSource 'native cache change cannot claim byte exact profile' {
 $f=New-GeneratedSource
 $f.profileReceipt.acceptedNativeCacheChanges=@([pscustomobject]@{path='achievements.dat';change='created';length=12288;beforeSha256='';afterSha256=('a'*64)})
 Flush-GeneratedSource $f;$null=Validate-GeneratedSource $f
} $true
Check-AssistedSource 'caller artifact hash is exact' {$f=New-GeneratedSource;$f.selection.sourceArtifactSha256='0'*64;$null=Validate-GeneratedSource $f} $true
Check-AssistedSource 'receipt drift during archive boundary refused' {$f=New-GeneratedSource;$script:mutationTarget=$f.profile;$script:sourceMutation={ [IO.File]::AppendAllText((Join-Path $script:mutationTarget 'owner.json'),' ') };$null=Validate-GeneratedSource $f} $true
Check-AssistedSource 'Manual drift during archive boundary refused' {$f=New-GeneratedSource;$script:mutationTarget=$f.artifact.observations.finalManual.path;$script:sourceMutation={ [IO.File]::AppendAllText($script:mutationTarget,'x') };$null=Validate-GeneratedSource $f} $true
$hardFixture=New-GeneratedSource;$original=Join-Path $hardFixture.profile 'owner.json';$moved=Join-Path $hardFixture.profile 'owner-original.json';Move-Item -LiteralPath $original -Destination $moved;[void](New-Item -ItemType HardLink -Path $original -Value $moved)
Check-AssistedSource 'actual hard-linked owner refused' {$null=Validate-GeneratedSource $hardFixture} $true
foreach($target in @('manual','auto','anchor')){
 Check-AssistedSource ($target+' actual changed bytes refuse output'){$f=New-GeneratedSource;$w=switch($target){'manual'{$f.artifact.observations.finalManual}'auto'{$f.artifact.observations.nativeWrites[-2]}'anchor'{$f.fixture.working}};$p=if($target-ceq'anchor'){Join-Path $f.owner.saveRoot $w.fileName}else{$w.path};[IO.File]::AppendAllText($p,'changed');Assert-KmcChunk7AssistedFixtureOutput $f.request $f.artifact} $true
}
Check-AssistedSource 'standalone output cannot skip full native evidence' {$f=New-GeneratedSource;$script:sourceBoundaryFail='evidence';Assert-KmcChunk7AssistedFixtureOutput $f.request $f.artifact} $true
Check-AssistedSource 'output path outside owned run refused' {$f=New-GeneratedSource;$f.artifact.observations.finalManual.path=Join-Path $f.root $f.artifact.observations.finalManual.fileName;Assert-KmcChunk7AssistedFixtureOutput $f.request $f.artifact} $true
Check-AssistedSource 'current Auto must use final settled metadata' {$f=New-GeneratedSource 3;$f.artifact.observations.nativeWrites[1].sha256=$f.artifact.observations.nativeWrites[0].sha256;Assert-KmcChunk7AssistedFixtureOutput $f.request $f.artifact} $true
Check-AssistedSource 'final Manual is exact last write' {$f=New-GeneratedSource;$f.artifact.observations.finalManual=Clone-Generated $f.artifact.observations.finalManual;$f.artifact.observations.finalManual.internalName='other';Assert-KmcChunk7AssistedFixtureOutput $f.request $f.artifact} $true
Check-AssistedSource 'consumer selection real JSON intake' {$f=New-GeneratedSource;$p=Join-Path $script:sourceLab 'selection.json';Write-GeneratedJson $p $f.selection;$v=Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer';Need-AssistedSource ($v.sha256-ceq(Get-KmcSha256 $p)-and$v.selection.sourceRunId-ceq$f.request.runId) 'Selection binding differs'}
$selectionBad=[ordered]@{
 'same source and consumer'={param($f,$p)Read-KmcChunk7AssistedFixtureSelectionFile $p $f.request.runId}
 'source path traversal'={param($f,$p)$f.selection.sourceRunId='../foreign';Write-GeneratedJson $p $f.selection;Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'source scalar hash array'={param($f,$p)$f.selection.sourceArtifactSha256=@($f.selection.sourceArtifactSha256);Write-GeneratedJson $p $f.selection;Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'selection extra authority'={param($f,$p)$f.selection|Add-Member NoteProperty grantActions $true;Write-GeneratedJson $p $f.selection;Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'duplicate JSON property'={param($f,$p)[IO.File]::WriteAllText($p,'{"sourceRunId":"a","sourceRunId":"b","sourceArtifactSha256":"'+('a'*64)+'","sourceArchiveSha256":"'+('b'*64)+'"}');Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'oversized intake'={param($f,$p)[IO.File]::WriteAllText($p,'x'*16385);Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'selection array root'={param($f,$p)[IO.File]::WriteAllText($p,'[]');Read-KmcChunk7AssistedFixtureSelectionFile $p 'consumer'}
 'path argument array'={param($f,$p)Read-KmcChunk7AssistedFixtureSelectionFile @($p) 'consumer'}
}
foreach($entry in $selectionBad.GetEnumerator()){Check-AssistedSource $entry.Key {$f=New-GeneratedSource;$p=Join-Path $script:sourceLab 'selection.json';Write-GeneratedJson $p $f.selection;&$entry.Value $f $p|Out-Null} $true}
$terminal='ASSISTED FIXTURE SOURCE PASS='+$script:sourcePass+' FAIL='+$script:sourceFail;$script:sourceLines.Add($terminal);Write-Host $terminal
[IO.File]::WriteAllLines((Join-Path $sourceTestOutput 'output.log'),$script:sourceLines,[Text.UTF8Encoding]::new($false))
$receipt=[ordered]@{schemaVersion=1;pass=$script:sourcePass;fail=$script:sourceFail;testPath=$PSCommandPath;testSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash.ToLowerInvariant();repositoryRoot=$RepositoryRoot;readerDirectory=$ReaderDirectory;generatedFixtureCases=$script:sourceCase;nativeInvoked=$false;archiveMembersRead=$false;protectedSavesRead=$false;substitutions=@('Get-KmcLabRoot returns per-case generated private lab','completed artifact-manifest validation boundary','full native assisted Evidence and Artifact validation boundaries','archive metadata validation boundary calls actual Output but never opens ZIP');actual=@('new source/selection/output functions','producer selection and save-root helpers','canonical/exact scalar JSON predicates','ordinary file path/reparse/hard-link checks','duplicate JSON member parser','Get-Item and complete-byte SHA256 over generated fixtures')}
[IO.File]::WriteAllText((Join-Path $sourceTestOutput 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));if($script:sourceFail-ne0){throw 'Assisted source focused checks failed; retained.'}
