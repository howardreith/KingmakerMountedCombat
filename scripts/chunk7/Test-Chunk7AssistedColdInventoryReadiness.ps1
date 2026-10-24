[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$actualLab=Get-KmcLabRoot;$output=Assert-KmcChildPath $OutputRoot (Join-Path $actualLab 'analysis-cache') 'generated assisted cold source checks'
if(Test-Path -LiteralPath $output){throw 'Preserve earlier assisted cold source checks.'};[void][IO.Directory]::CreateDirectory($output)
$script:inputPins=[Collections.Generic.List[object]]::new()
function Source([string]$Relative){$p=Join-Path $RepositoryRoot $Relative;if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$o=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $o -PathType Leaf){$p=$o}};$script:inputPins.Add([pscustomobject]@{path=$p;sha256=(Get-KmcSha256 $p)});$p}
. (Source 'scripts/runtime/Chunk7AssistedFixtureEvidence.ps1')
. (Source 'scripts/runtime/Chunk7AssistedFixtureFixtures.ps1')
. (Source 'scripts/runtime/Chunk7AssistedColdInventoryEvidence.ps1')
. (Source 'scripts/runtime/Chunk7AssistedColdInventoryFixtures.ps1')
$script:inputPins.Add([pscustomobject]@{path=$PSCommandPath;sha256=(Get-KmcSha256 $PSCommandPath)})
foreach($p in @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -Filter '*.ps1' -File)){$script:inputPins.Add([pscustomobject]@{path=$p.FullName;sha256=(Get-KmcSha256 $p.FullName)})}
# Only the complete producer/native artifact/archive qualification boundary is
# replaced. New intake/copy methods, real private file metadata/hash, link/path
# checks and actual open-lock owner validation execute without substitution.
function Get-KmcLabRoot {$script:caseLab}
function Get-KmcChunk7AssistedFixtureSource($Selection,$Fixture,$Candidate){
 $script:gateCalls++
 if($script:gateReject){throw 'Generated complete producer boundary refused.'}
 Assert-KmcAssistedSame $Selection $script:expectedSourceSelection 'three-key producer selection delegation'
 Assert-KmcAssistedSame $Fixture $script:expectedFixture 'producer fixture delegation'
 Assert-KmcAssistedSame $Candidate $script:expectedCandidate 'producer candidate delegation'
 $script:producerSource
}
function Clone($Value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $Value -Depth 40 -Compress)}
function Put([string]$Path,[string]$Text){[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path));[IO.File]::WriteAllText($Path,$Text,[Text.UTF8Encoding]::new($false))}
function File-Pin([string]$Path){$i=Get-Item -LiteralPath $Path;[pscustomobject]@{path=$Path;sha256=(Get-KmcSha256 $Path);length=$i.Length;lastWriteTimeUtcTicks=$i.LastWriteTimeUtc.Ticks}}
function Need([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
$script:caseNumber=0;$script:passed=0;$script:failed=0;$cases=[Collections.Generic.List[object]]::new();$timer=[Diagnostics.Stopwatch]::StartNew()
function New-Fixture {
 $script:caseNumber++;$script:caseLab=Join-Path $output ('case'+$script:caseNumber);[void][IO.Directory]::CreateDirectory($script:caseLab)
 $run='cold-generated-'+$script:caseNumber;$sourceRun='assisted-generated-'+$script:caseNumber
 $lock=Open-KmcRuntimeLock (Join-Path $script:caseLab 'runtime-state') $run
 $profile=Join-Path $script:caseLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory((Join-Path $profile 'Saved Games'))
 $sourceRoot=Join-Path $script:caseLab ('runtime-evidence/'+$sourceRun);$sourceProfile=Join-Path $script:caseLab ('runtime-staging/persistence-'+$sourceRun)
 $archive=Join-Path $sourceProfile 'Saved Games/Manual_2_KMC_C7_ASSISTED_FIXTURE.zks';Put $archive ('generated non-ZIP source bytes '+$script:caseNumber)
 $file=Get-Item -LiteralPath $archive;$descriptor=[pscustomobject]@{internalName='KMC_C7_ASSISTED_FIXTURE';fileName='Manual_2_KMC_C7_ASSISTED_FIXTURE.zks';sha256=(Get-KmcSha256 $archive);length=$file.Length;lastWriteTimeUtcTicks=$file.LastWriteTimeUtc.Ticks;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC_C7_ASSISTED_GENERATED';area='abababababababababababababababab'}
 $pins=@();foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','profile-cache-changes.json','chunk7-assisted-fixture-evidence.json')){$p=Join-Path $sourceRoot $leaf;Put $p ('{"generated":true,"slot":"'+$leaf+'"}');$pins+=File-Pin $p}
 $sourceOwner=Join-Path $sourceProfile 'owner.json';Put $sourceOwner '{"generated":true}';$pins+=File-Pin $sourceOwner
 $artifact=Join-Path $sourceRoot 'chunk7-assisted-fixture-evidence.json'
 $selection=[pscustomobject]@{case='assisted-manual-readonly-inventory';sourceRunId=$sourceRun;sourceArtifactSha256=(Get-KmcSha256 $artifact);sourceArchiveSha256=$descriptor.sha256;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $script:expectedSourceSelection=[pscustomobject]@{sourceRunId=$sourceRun;sourceArtifactSha256=$selection.sourceArtifactSha256;sourceArchiveSha256=$selection.sourceArchiveSha256}
 $fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64);gameId='bbbbbbbb-1111-1111-1111-111111111111'};working=[pscustomobject]@{sha256=('c'*64);gameId='cccccccc-1111-1111-1111-111111111111'}}
 $candidate=[pscustomobject]@{commit=('d'*40);dllSha256=('e'*64);dllMvid='dddddddd-1111-1111-1111-111111111111'}
 $script:expectedFixture=Clone $fixture;$script:expectedCandidate=Clone $candidate
 $owner=[pscustomobject]@{runId=$run;transactionToken=$lock.Token;scenario='chunk7-assisted-cold-inventory';persistenceCase=$null;sourceSha256=$descriptor.sha256;saveRoot=(Join-Path $profile 'Saved Games')}
 $ownerPath=Join-Path $profile 'owner.json';Put $ownerPath ($owner|ConvertTo-Json -Compress)
 $script:producerSource=[pscustomobject]@{path=$archive;descriptor=$descriptor;artifactPath=$artifact;artifactSha256=$selection.sourceArtifactSha256;artifactLength=(Get-Item -LiteralPath $artifact).Length;sourceRunId=$sourceRun;requestPath=(Join-Path $sourceRoot 'runtime-request.json');requestSha256=(Get-KmcSha256 (Join-Path $sourceRoot 'runtime-request.json'));manifestSha256=(Get-KmcSha256 (Join-Path $sourceRoot 'runtime-artifacts.json'));receiptPins=$pins;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 $request=[pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$selection;persistenceLoad=$descriptor;fixture=$fixture}
 $selectionPath=Join-Path $script:caseLab 'selection.json';Put $selectionPath ($selection|ConvertTo-Json -Compress)
 $script:gateCalls=0;$script:gateReject=$false
 [pscustomobject]@{runId=$run;request=$request;selection=$selection;selectionPath=$selectionPath;fixture=$fixture;candidate=$candidate;source=(Clone $script:producerSource);profile=$profile;lock=$lock;owner=$owner;ownerPath=$ownerPath;destination=(Join-Path $profile 'assisted-cold-source.json')}
}
function Get-Source($f){Get-KmcChunk7AssistedColdInventorySource $f.selection $f.fixture $f.candidate}
function Copy-Source($f){Copy-KmcChunk7AssistedColdInventorySourceArtifact $f.source $f.profile $f.lock $f.selection $f.fixture $f.candidate}
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $f=$null;$accepted=$true;$errorText=$null;$fixtureReady=$false;$closed=$false
 try{$f=New-Fixture;$fixtureReady=$true;&$Action $f|Out-Null}catch{$accepted=$false;$errorText=$_.Exception.Message}
 finally{if($null-ne$f){try{Close-KmcRuntimeLock $f.lock;$closed=$true}catch{$accepted=$false;$errorText='Generated lock cleanup failed: '+$_.Exception.Message}}}
 $ok=$fixtureReady-and$closed-and$accepted-eq$Expected
 if($ok){$script:passed++}else{$script:failed++}
 $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Expected;actualAccepted=$accepted;fixtureReady=$fixtureReady;generatedLockClosed=$closed;error=$errorText})
 Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok-and$errorText){': '+$errorText}else{''}))
}

$readinessFixture=Source 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'
function New-Readiness($f){
 $old=Read-KmcJson $readinessFixture;$row=$old.artifact.observations.fixtureReadiness;$world=$old.artifact.observations.loadedWorld;$settled=$old.artifact.observations.settledWorld;$inventory=$old.artifact.observations.nativeProfileInventory
 $row.staticPartyFacts|Add-Member NoteProperty observationScope 'static-getters-only'
 $root=Join-Path $f.profile 'Saved Games';$row.isolatedSaveDescriptors.root=$root;$d=$row.isolatedSaveDescriptors.descriptors[0]
 foreach($key in @('fileName','internalName','gameId','gameName','area')){$d.$key=$f.request.persistenceLoad.$key};$d.type='Manual'
 [pscustomobject]@{row=$row;world=$world;settled=$settled;inventory=$inventory;root=$root}
}
function Check-Readiness($f,$n){Assert-KmcAssistedColdFixtureReadiness $n.row $f.request $n.world $n.settled $n.inventory $n.root}
function New-Envelope($f){
 $request=[pscustomobject]@{runId=$f.runId;scenario='chunk7-assisted-cold-inventory';branch='codex/generated-cold';commit=('d'*40);productVersion='0.1.0-generated';dllSha256=('e'*64);dllMvid='dddddddd-1111-1111-1111-111111111111'}
 $artifact=[pscustomobject]@{schemaVersion=1;evidenceKind='chunk7-assisted-cold-inventory-evidence';runId=$request.runId;scenario=$request.scenario;branch=$request.branch;commit=$request.commit;productVersion=$request.productVersion;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid;status='PASS';observations=[pscustomobject]@{};errors=@();createdAtUtc='2026-10-13T00:00:00.0000000Z'}
 [pscustomobject]@{request=$request;artifact=$artifact}
}
function Prepare-ArtifactRead($f){$f.request|Add-Member NoteProperty evidenceRoot (Join-Path $script:caseLab ('runtime-evidence/'+$f.runId))}
function Reset-ArtifactHash($f){$f.selection.sourceArtifactSha256=Get-KmcSha256 $f.source.artifactPath}
Check 'held producer artifact reads exact generated bytes only' $true {param($f)Prepare-ArtifactRead $f;$r=Read-KmcAssistedColdSourceArtifact $f.request;Need ($r.generated-is[bool]-and$r.generated-and$script:gateCalls-eq0) 'Raw read fabricated qualification or changed bytes'}
Check 'changed artifact hash remains refused' $false {param($f)Prepare-ArtifactRead $f;Put $f.source.artifactPath '{"changed":true}';Read-KmcAssistedColdSourceArtifact $f.request}
Check 'duplicate producer member remains refused with matching hash' $false {param($f)Prepare-ArtifactRead $f;Put $f.source.artifactPath '{"generated":true,"generated":false}';Reset-ArtifactHash $f;Read-KmcAssistedColdSourceArtifact $f.request}
Check 'root array remains refused with matching hash' $false {param($f)Prepare-ArtifactRead $f;Put $f.source.artifactPath '[{"generated":true}]';Reset-ArtifactHash $f;Read-KmcAssistedColdSourceArtifact $f.request}
Check 'foreign consumer evidence root remains refused' $false {param($f)Prepare-ArtifactRead $f;$f.request.evidenceRoot=Join-Path $script:caseLab 'runtime-evidence/foreign';Read-KmcAssistedColdSourceArtifact $f.request}
Check 'producer artifact hardlink remains refused' $false {param($f)Prepare-ArtifactRead $f;[void](New-Item -ItemType HardLink -Path (Join-Path $script:caseLab 'foreign.json') -Target $f.source.artifactPath);Read-KmcAssistedColdSourceArtifact $f.request}
Check 'empty producer artifact remains refused' $false {param($f)Prepare-ArtifactRead $f;Put $f.source.artifactPath '';Reset-ArtifactHash $f;Read-KmcAssistedColdSourceArtifact $f.request}
Check 'oversized producer artifact remains refused' $false {param($f)Prepare-ArtifactRead $f;Put $f.source.artifactPath (' '*16777217);Reset-ArtifactHash $f;Read-KmcAssistedColdSourceArtifact $f.request}
Check 'active source writer prevents a readonly artifact lease' $false {param($f)Prepare-ArtifactRead $f;$writer=[IO.FileStream]::new($f.source.artifactPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read);try{Read-KmcAssistedColdSourceArtifact $f.request}finally{$writer.Dispose()}}
Check 'new-family exact scalar native envelope is accepted' $true {param($f)$n=New-Envelope $f;Assert-KmcAssistedColdArtifactEnvelope $n.request $n.artifact 'chunk7-assisted-cold-inventory-evidence'}
foreach($key in @('evidenceKind','status','createdAtUtc')){Check ('new-family singleton envelope '+$key+' is refused') $false {param($f)$n=New-Envelope $f;$n.artifact.$key=@($n.artifact.$key);Assert-KmcAssistedColdArtifactEnvelope $n.request $n.artifact 'chunk7-assisted-cold-inventory-evidence'}.GetNewClosure()}
Check 'foreign native artifact envelope is refused' $false {param($f)$n=New-Envelope $f;$n.artifact.scenario='chunk7-bootstrap-cold-inventory';Assert-KmcAssistedColdArtifactEnvelope $n.request $n.artifact 'chunk7-assisted-cold-inventory-evidence'}
Check 'readiness accepts only actual selected Manual descriptor metadata' $true {param($f)$n=New-Readiness $f;Check-Readiness $f $n}
Check 'observed Manual number remains dynamic when descriptor agrees' $true {param($f)$f.request.persistenceLoad.fileName='Manual_2147483647_KMC_C7_ASSISTED_FIXTURE.zks';$n=New-Readiness $f;Check-Readiness $f $n}
Check 'historical Auto descriptor type cannot enter new Manual readiness' $false {param($f)$n=New-Readiness $f;$n.row.isolatedSaveDescriptors.descriptors[0].type='Auto';Check-Readiness $f $n}
Check 'another native Manual number is refused' $false {param($f)$n=New-Readiness $f;$n.row.isolatedSaveDescriptors.descriptors[0].fileName='Manual_3_KMC_C7_ASSISTED_FIXTURE.zks';Check-Readiness $f $n}
Check 'second descriptor is refused in readonly cold profile' $false {param($f)$n=New-Readiness $f;$n.row.isolatedSaveDescriptors.descriptors+=,$n.row.isolatedSaveDescriptors.descriptors[0];Check-Readiness $f $n}
Check 'singleton observed campaign is refused' $false {param($f)$n=New-Readiness $f;$n.row.isolatedSaveDescriptors.descriptors[0].gameId=@($n.row.isolatedSaveDescriptors.descriptors[0].gameId);Check-Readiness $f $n}
Check 'readiness class ranks cannot drift from native main level' $false {param($f)$n=New-Readiness $f;$n.row.mainProgression.classes[0].level++;Check-Readiness $f $n}
Check 'readiness main XP cannot drift from inventory' $false {param($f)$n=New-Readiness $f;$n.row.mainProgression.experience++;Check-Readiness $f $n}
Check 'static character roster cannot silently lose main' $false {param($f)$n=New-Readiness $f;$n.row.staticPartyFacts.characters=@();Check-Readiness $f $n}
Check 'singleton top-level selection array is refused' $false {param($f)$json=[IO.File]::ReadAllText($f.selectionPath);Put $f.selectionPath ('['+$json+']');Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId}
$changed=@($script:inputPins|Where-Object{(Get-KmcSha256 $_.path)-cne$_.sha256})
if($changed.Count-ne0){throw 'Assisted cold test input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='assisted-cold-artifact-readiness-focused';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;durationSeconds=$timer.Elapsed.TotalSeconds;inputPins=$script:inputPins.ToArray();changedInputs=$changed;cases=$cases.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;archiveDecodes=0;limits=@('No complete producer/full cold evidence gate is exercised. Readiness inputs are explicitly adapted from the retained generated bootstrap fixture, with only the new Manual descriptor metadata.','Actual new selection/GetSource/copy functions, file/hash/link/path primitives and actual generated transaction locks execute.','All .zks files are generated non-ZIP byte buffers; no native fixture qualification or ordinary acquisition claim.')}
Write-KmcJsonAtomic (Join-Path $output 'receipt.json') $receipt
Write-Host ('ASSISTED COLD ARTIFACT READINESS: '+$script:passed+' PASS / '+$script:failed+' FAIL')
if($script:failed-ne0){exit 1};exit 0
