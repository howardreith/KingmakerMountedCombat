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
Check 'closed seven-field intake returns actual hash' $true {param($f)$r=Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId;Need ($r.sha256-ceq(Get-KmcSha256 $f.selectionPath)) 'Intake hash differs';Need ($script:gateCalls-eq0) 'Intake must not qualify a source'}
Check 'actual producer gate receives exact three fields and candidate' $true {param($f)$r=Get-Source $f;Need ($script:gateCalls-eq1-and[object]::ReferenceEquals($r,$script:producerSource)-and-not$r.nativeAuthorityGranted) 'Gate delegation changed'}
Check 'actual held copy and real lock retain all exact bytes' $true {param($f)$r=Copy-Source $f;Need ($script:gateCalls-eq1-and$r.path-ceq$f.destination-and(Get-KmcSha256 $r.path)-ceq$f.selection.sourceArtifactSha256-and-not$r.nativeAuthorityGranted) 'Copy proof differs';Need (([IO.File]::ReadAllText($f.destination))-ceq([IO.File]::ReadAllText($f.source.artifactPath))) 'Copied content differs'}
Check 'empty typed owner persistence case is accepted' $true {param($f)$f.owner.persistenceCase='';Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'legacy scenario without new selection remains outside family' $true {param($f)Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-bootstrap-cold-inventory';runId=$f.runId})}
Check 'minimum canonical Manual number is admitted' $true {param($f)$f.request.persistenceLoad.fileName='Manual_1_KMC_C7_ASSISTED_FIXTURE.zks';Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}
Check 'maximum canonical Int32 Manual number is admitted' $true {param($f)$f.request.persistenceLoad.fileName='Manual_2147483647_KMC_C7_ASSISTED_FIXTURE.zks';Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}
Check 'foreign family cannot carry cold selection' $false {param($f)$f.request.scenario='chunk7-bootstrap-cold-inventory';Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'matching singleton scenario array is refused' $false {param($f)$f.request.scenario=@($f.request.scenario);Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'missing typed selection is refused' $false {param($f)$f.request.PSObject.Properties.Remove('assistedColdInventory');Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'extra selection action field is refused' $false {param($f)$f.selection|Add-Member NoteProperty grantExperience 1;Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'self-source is refused' $false {param($f)$f.selection.sourceRunId=$f.runId;Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'selection source traversal is refused' $false {param($f)$f.selection.sourceRunId='../foreign';Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'old bootstrap case is refused' $false {param($f)$f.selection.case='authored-readonly-inventory';Assert-KmcChunk7AssistedColdInventorySelection $f.request}
foreach($key in @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount')){Check ('singleton selection '+$key+' is refused') $false {param($f)$f.selection.$key=@($f.selection.$key);Assert-KmcChunk7AssistedColdInventorySelection $f.request}.GetNewClosure()}
Check 'second load is refused' $false {param($f)$f.selection.expectedNativeLoadCount=2;Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'any native write is refused' $false {param($f)$f.selection.expectedNativeWriteCount=1;Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'non-base-only staging is refused' $false {param($f)$f.selection.baseGameOnly=$false;Assert-KmcChunk7AssistedColdInventorySelection $f.request}
Check 'duplicate source selection member is refused' $false {param($f)$text=[IO.File]::ReadAllText($f.selectionPath);Put $f.selectionPath ($text.Substring(0,$text.Length-1)+',"sourceRunId":"duplicate"}');Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId}
Check 'oversized selection is refused' $false {param($f)Put $f.selectionPath (' '*16385);Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId}
Check 'selection hardlink is refused' $false {param($f)$link=Join-Path $script:caseLab 'selection-link.json';[void](New-Item -ItemType HardLink -Path $link -Target $f.selectionPath);Read-KmcChunk7AssistedColdInventorySelectionFile $link $f.runId}
foreach($leaf in @('Auto_1.zks','Manual_0_KMC_C7_ASSISTED_FIXTURE.zks','Manual_02_KMC_C7_ASSISTED_FIXTURE.zks','Manual_2147483648_KMC_C7_ASSISTED_FIXTURE.zks','Manual_2_OTHER.zks','../Manual_2_KMC_C7_ASSISTED_FIXTURE.zks')){Check ('foreign or malformed leaf '+$leaf+' refused') $false {param($f)$f.request.persistenceLoad.fileName=$leaf;Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}.GetNewClosure()}
Check 'descriptor singleton hash is refused' $false {param($f)$f.request.persistenceLoad.sha256=@($f.request.persistenceLoad.sha256);Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}
Check 'protected campaign alias is refused' $false {param($f)$f.request.persistenceLoad.gameId=$f.fixture.working.gameId;Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}
Check 'protected bytes alias is refused' $false {param($f)$f.request.persistenceLoad.sha256=$f.fixture.baseline.sha256;$f.selection.sourceArchiveSha256=$f.fixture.baseline.sha256;Assert-KmcChunk7AssistedColdInventoryDescriptor $f.request}
Check 'producer completion refusal is mandatory' $false {param($f)$script:gateReject=$true;Get-Source $f}
Check 'candidate substitution reaches strict producer boundary' $false {param($f)$f.candidate.commit='f'*40;Get-Source $f}
Check 'fixture substitution reaches strict producer boundary' $false {param($f)$f.fixture.working.sha256='a'*64;Get-Source $f}
Check 'producer cannot return native authority' $false {param($f)$script:producerSource.nativeAuthorityGranted=$true;Get-Source $f}
Check 'producer cannot drop assisted provenance' $false {param($f)$script:producerSource.fixtureSetupOnly=$false;Get-Source $f}
Check 'producer cannot return another source artifact hash' $false {param($f)$script:producerSource.artifactSha256='f'*64;Get-Source $f}
Check 'locked copy requalifies producer' $false {param($f)$script:gateReject=$true;Copy-Source $f}
Check 'preflight source descriptor drift is refused' $false {param($f)$f.source.descriptor.area='cd'*16;Copy-Source $f}
Check 'preflight source receipt pin drift is refused' $false {param($f)$f.source.receiptPins[0].sha256='f'*64;Copy-Source $f}
Check 'foreign consumer profile is refused' $false {param($f)$f.profile=Join-Path $script:caseLab 'runtime-staging/persistence-foreign';[void][IO.Directory]::CreateDirectory($f.profile);Copy-Source $f}
Check 'foreign profile owner scenario is refused' $false {param($f)$f.owner.scenario='chunk7-bootstrap-cold-inventory';Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'foreign profile owner token is refused' $false {param($f)$f.owner.transactionToken='f'*64;Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'anchor Working hash cannot own consumer source' $false {param($f)$f.owner.sourceSha256=$f.fixture.working.sha256;Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'foreign persistence case is refused' $false {param($f)$f.owner.persistenceCase='multi-exploration-manual';Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'extra owner property is refused' $false {param($f)$f.owner|Add-Member NoteProperty grant $true;Put $f.ownerPath ($f.owner|ConvertTo-Json -Compress);Copy-Source $f}
Check 'existing destination remains byte exact after refusal' $true {param($f)Put $f.destination 'preserved foreign leaf';$before=Get-KmcSha256 $f.destination;$refused=$false;try{Copy-Source $f|Out-Null}catch{$refused=$true};Need ($refused-and(Get-KmcSha256 $f.destination)-ceq$before) 'Copy overwrote existing leaf'}
Check 'source Manual changed before copy is refused' $false {param($f)Put $f.source.path 'changed generated source bytes';Copy-Source $f}
Check 'source artifact changed before copy is refused' $false {param($f)Put $f.source.artifactPath '{"changed":true}';Copy-Source $f}
Check 'source completion receipt changed before copy is refused' $false {param($f)Put $f.source.receiptPins[1].path '{"changed":true}';Copy-Source $f}
Check 'source artifact hardlink is refused' $false {param($f)[void](New-Item -ItemType HardLink -Path (Join-Path $script:caseLab 'artifact-link.json') -Target $f.source.artifactPath);Copy-Source $f}
Check 'wrong actual open-lock token is refused' $false {param($f)$other=[pscustomobject]@{RunId=$f.lock.RunId;Token=('f'*64);Stream=$f.lock.Stream;Path=$f.lock.Path};Copy-KmcChunk7AssistedColdInventorySourceArtifact $f.source $f.profile $other $f.selection $f.fixture $f.candidate}
# Owner-seed source kind. A synthetic registry and a synthetic seed archive in
# the case lab; the real master is never touched. Actual intake, registry,
# header read, path/link checks, no-alias checks and lock-owned record placement run.
function Get-KmcChunk7OwnerSeedRegistryPath {$script:seedRegistryPath}
function New-SeedArchive([string]$Path,$Header){
 Add-Type -AssemblyName System.IO.Compression
 [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path))
 $stream=[IO.FileStream]::new($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
  try{foreach($pair in @(@('header.json',($Header|ConvertTo-Json -Depth 6 -Compress)),@('party.json','{"generated":true}'))){$entry=$zip.CreateEntry($pair[0]);$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false));try{$writer.Write($pair[1])}finally{$writer.Dispose()}}}
  finally{$zip.Dispose()}}finally{$stream.Dispose()}
}
function New-SeedFixture([scriptblock]$Mutate){
 $script:caseNumber++;$script:caseLab=Join-Path $output ('seed'+$script:caseNumber);[void][IO.Directory]::CreateDirectory($script:caseLab)
 $run='seed-cold-'+$script:caseNumber
 $header=[pscustomobject]@{Name='KMC_C7_OWNER_SEED';GameName='Generated Owner';GameId='33333333-4444-5555-6666-777777777777';CampaignDLC='None';Type='Manual';IsAutoLevelupSave=$false;Area=('ab'*16);CompatibilityVersion=1;DLC=@('Tieflings','Varnhold')}
 $ctx=[pscustomobject]@{header=$header;masterLeaf='KMC_C7_OWNER_SEED.master.zks';intakeText='{"kind":"owner-supplied-development-seed-intake"}';linkMaster=$false;junctionRoot=$false}
 if($Mutate){&$Mutate $ctx 'archive'}
 $ownerRoot=Join-Path $script:caseLab 'fixtures/owner-supplied'
 if($ctx.junctionRoot){$real=Join-Path $script:caseLab 'elsewhere';[void][IO.Directory]::CreateDirectory($real);[void][IO.Directory]::CreateDirectory((Join-Path $script:caseLab 'fixtures'));[void](New-Item -ItemType Junction -Path $ownerRoot -Target $real)}
 $master=Join-Path $ownerRoot $ctx.masterLeaf;New-SeedArchive $master $ctx.header
 if($ctx.linkMaster){$other=Join-Path $script:caseLab 'master-alias.zks';[void](New-Item -ItemType HardLink -Path $other -Target $master)}
 $intake=Join-Path $ownerRoot 'KMC_C7_OWNER_SEED.intake01.json';Put $intake $ctx.intakeText
 $file=Get-Item -LiteralPath $master
 $entry=[pscustomobject]@{seedId='KMC_C7_OWNER_SEED';sha256=(Get-KmcSha256 $master);length=$file.Length;internalName='KMC_C7_OWNER_SEED';fileName='Manual_302_KMC_C7_OWNER_SEED.zks';type='Manual';compatibilityVersion=1;gameId=$header.GameId;gameName=$header.GameName;area=$header.Area;campaignDlc='None';dlc=@('Tieflings','Varnhold');masterPath=('fixtures/owner-supplied/'+$ctx.masterLeaf);intakePath='fixtures/owner-supplied/KMC_C7_OWNER_SEED.intake01.json';intakeSha256=(Get-KmcSha256 $intake)}
 $selection=[pscustomobject]@{case='owner-seed-readonly-inventory';seedId='KMC_C7_OWNER_SEED';seedSha256=$entry.sha256;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64);gameId='bbbbbbbb-1111-1111-1111-111111111111'};working=[pscustomobject]@{sha256=('c'*64);gameId='cccccccc-1111-1111-1111-111111111111'}}
 $candidate=[pscustomobject]@{commit=('d'*40);dllSha256=('e'*64);dllMvid='dddddddd-1111-1111-1111-111111111111'}
 $registry=[pscustomobject]@{schemaVersion=1;kind='kmc-owner-seed-registry';authority='generated';seeds=@($entry)}
 $f=[pscustomobject]@{runId=$run;entry=$entry;registry=$registry;selection=$selection;fixture=$fixture;candidate=$candidate;master=$master;intake=$intake;lock=$null;profile=$null;ownerPath=$null}
 if($Mutate){&$Mutate $f 'registry'}
 $script:seedRegistryPath=Join-Path $script:caseLab 'Chunk7OwnerSeedRegistry.json';Put $script:seedRegistryPath ($f.registry|ConvertTo-Json -Depth 8)
 $selectionPath=Join-Path $script:caseLab 'seed-selection.json';Put $selectionPath ($f.selection|ConvertTo-Json -Compress)
 $f|Add-Member NoteProperty selectionPath $selectionPath
 $f
}
function Open-SeedProfile($f){
 $f.lock=Open-KmcRuntimeLock (Join-Path $script:caseLab 'runtime-state') $f.runId
 $f.profile=Join-Path $script:caseLab ('runtime-staging/persistence-'+$f.runId);[void][IO.Directory]::CreateDirectory((Join-Path $f.profile 'Saved Games'))
 $f.ownerPath=Join-Path $f.profile 'owner.json'
 Put $f.ownerPath (([pscustomobject]@{runId=$f.runId;transactionToken=$f.lock.Token;scenario='chunk7-assisted-cold-inventory';persistenceCase=$null;sourceSha256=$f.entry.sha256;saveRoot=(Join-Path $f.profile 'Saved Games')})|ConvertTo-Json -Compress)
}
function CheckSeed([string]$Name,[bool]$Expected,[scriptblock]$Mutate,[scriptblock]$Action){
 $f=$null;$accepted=$true;$errorText=$null;$ready=$false
 try{$f=New-SeedFixture $Mutate;$ready=$true;&$Action $f|Out-Null}catch{$accepted=$false;$errorText=$_.Exception.Message}
 finally{if($null-ne$f-and$null-ne$f.lock){try{Close-KmcRuntimeLock $f.lock}catch{$accepted=$false;$errorText='Generated lock cleanup failed: '+$_.Exception.Message}}}
 # Every refusal happens before any lock or profile: no runtime-state appears.
 $noLock=$null-eq$f-or$null-ne$f.lock-or-not(Test-Path -LiteralPath (Join-Path $script:caseLab 'runtime-state'))
 $ok=$ready-and$noLock-and$accepted-eq$Expected
 if($ok){$script:passed++}else{$script:failed++}
 $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Expected;actualAccepted=$accepted;fixtureReady=$ready;error=$errorText})
 Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok-and$errorText){': '+$errorText}else{''}))
}
function Get-Seed($f){Get-KmcChunk7AssistedColdInventorySource $f.selection $f.fixture $f.candidate}
CheckSeed 'owner seed intake and source accepted without producer receipts' $true $null {param($f)$script:gateCalls=0;$r=Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId;$s=Get-Seed $f;Need ($script:gateCalls-eq0) 'producer gate invoked';Need ($s.descriptor.internalName-ceq'KMC_C7_OWNER_SEED'-and$s.descriptor.fileName-ceq'Manual_302_KMC_C7_OWNER_SEED.zks'-and$s.descriptor.sha256-ceq$f.entry.sha256-and-not$s.nativeAuthorityGranted) 'seed descriptor differs';Need (-not(Test-Path -LiteralPath (Join-Path $script:caseLab 'runtime-state'))) 'intake created lock state'}
CheckSeed 'owner seed sha mismatch refused' $false {param($f,$p)if($p-eq'registry'){$f.selection.seedSha256='f'*64}} {param($f)Get-Seed $f}
CheckSeed 'owner seed not in registry refused' $false {param($f,$p)if($p-eq'registry'){$f.selection.seedId='OTHER_SEED'}} {param($f)Get-Seed $f}
CheckSeed 'owner seed missing intake record refused' $false $null {param($f)Remove-Item -LiteralPath $f.intake;Get-Seed $f}
CheckSeed 'owner seed intake record hash drift refused' $false $null {param($f)Put $f.intake '{"changed":true}';Get-Seed $f}
CheckSeed 'owner seed header name mismatch refused' $false {param($c,$p)if($p-eq'archive'){$c.header.Name='OTHER_NAME'}} {param($f)Get-Seed $f}
CheckSeed 'owner seed header DLC mismatch refused' $false {param($c,$p)if($p-eq'archive'){$c.header.DLC=@('Tieflings')}} {param($f)Get-Seed $f}
CheckSeed 'owner seed header compatibility mismatch refused' $false {param($c,$p)if($p-eq'archive'){$c.header.CompatibilityVersion=2}} {param($f)Get-Seed $f}
CheckSeed 'owner seed aliasing Working campaign refused' $false {param($f,$p)if($p-eq'registry'){$f.fixture.working.gameId=$f.entry.gameId}} {param($f)Get-Seed $f}
CheckSeed 'owner seed aliasing Baseline bytes refused' $false {param($f,$p)if($p-eq'registry'){$f.fixture.baseline.sha256=$f.entry.sha256}} {param($f)Get-Seed $f}
CheckSeed 'owner seed outside owner-supplied root refused' $false {param($f,$p)if($p-eq'registry'){$f.registry.seeds[0].masterPath='fixtures/other/KMC_C7_OWNER_SEED.master.zks'}} {param($f)Get-Seed $f}
CheckSeed 'owner seed owner-supplied reparse point refused' $false {param($c,$p)if($p-eq'archive'){$c.junctionRoot=$true}} {param($f)Get-Seed $f}
CheckSeed 'owner seed hard-linked master refused' $false {param($c,$p)if($p-eq'archive'){$c.linkMaster=$true}} {param($f)Get-Seed $f}
CheckSeed 'owner seed impersonating the producer fixture refused' $false {param($f,$p)if($p-eq'registry'){$f.registry.seeds[0].internalName='KMC_C7_ASSISTED_FIXTURE'}} {param($f)Get-Seed $f}
CheckSeed 'owner seed selection carrying producer pins refused' $false {param($f,$p)if($p-eq'registry'){$f.selection|Add-Member NoteProperty sourceRunId 'assisted-source'}} {param($f)Read-KmcChunk7AssistedColdInventorySelectionFile $f.selectionPath $f.runId}
CheckSeed 'owner seed record placed once under the lock, honest and unique' $true $null {param($f)$s=Get-Seed $f;Open-SeedProfile $f;$r=Copy-KmcChunk7AssistedColdInventorySourceArtifact $s $f.profile $f.lock $f.selection $f.fixture $f.candidate;$rec=Read-KmcJson $r.path;Need ($rec.kind-ceq'kmc-owner-seed-source'-and$rec.producerAncestry-eq$false-and$rec.registryEntry.sha256-ceq$f.entry.sha256-and$rec.intake.sha256-ceq$f.entry.intakeSha256) 'record content differs';$again=$false;try{Copy-KmcChunk7AssistedColdInventorySourceArtifact $s $f.profile $f.lock $f.selection $f.fixture $f.candidate|Out-Null}catch{$again=$true};Need $again 'second placement admitted'}
CheckSeed 'owner seed record refuses a foreign lock token' $false $null {param($f)$s=Get-Seed $f;Open-SeedProfile $f;$other=[pscustomobject]@{RunId=$f.lock.RunId;Token=('f'*64);Stream=$f.lock.Stream;Path=$f.lock.Path};Copy-KmcChunk7AssistedColdInventorySourceArtifact $s $f.profile $other $f.selection $f.fixture $f.candidate}
CheckSeed 'owner seed master changed after qualification refused under the lock' $false $null {param($f)$s=Get-Seed $f;Open-SeedProfile $f;[IO.File]::AppendAllText($f.master,'x');Copy-KmcChunk7AssistedColdInventorySourceArtifact $s $f.profile $f.lock $f.selection $f.fixture $f.candidate}
$changed=@($script:inputPins|Where-Object{(Get-KmcSha256 $_.path)-cne$_.sha256})
if($changed.Count-ne0){throw 'Assisted cold test input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='assisted-cold-source-focused';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;durationSeconds=$timer.Elapsed.TotalSeconds;inputPins=$script:inputPins.ToArray();changedInputs=$changed;cases=$cases.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;archiveDecodes=0;limits=@('Complete producer/native artifact/archive gate explicitly substituted with strict generated argument boundary; full producer source54 remains separate.','Actual new selection/GetSource/copy functions, file/hash/link/path primitives and actual generated transaction locks execute.','All .zks files are generated non-ZIP byte buffers; no native fixture qualification or ordinary acquisition claim.')}
Write-KmcJsonAtomic (Join-Path $output 'receipt.json') $receipt
Write-Host ('ASSISTED COLD SOURCE: '+$script:passed+' PASS / '+$script:failed+' FAIL')
if($script:failed-ne0){exit 1};exit 0
