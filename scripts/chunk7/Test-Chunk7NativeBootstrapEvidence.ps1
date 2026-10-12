[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeBootstrapEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/native-bootstrap-reader-'+[Guid]::NewGuid().ToString('N'))}
$bootstrapReader=[IO.Path]::GetFullPath($ReaderDirectory);$bootstrapFixture=[IO.Path]::GetFullPath($FixturePath);$bootstrapOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $bootstrapReader 'RuntimeHarness.Common.ps1')
function New-BootstrapFixture { [IO.File]::ReadAllText($bootstrapFixture)|ConvertFrom-Json }
function Copy-Bootstrap($Value){$Value|ConvertTo-Json -Depth 64|ConvertFrom-Json}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($bootstrapOutput)
$script:bootstrapPasses=0;$script:bootstrapFailures=0
function Good-Bootstrap([string]$Name,[scriptblock]$Action){try{&$Action;$script:bootstrapPasses++;Write-Host ('PASS '+$Name)}catch{$script:bootstrapFailures++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad-Bootstrap([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action}catch{$refused=$true};if($refused){$script:bootstrapPasses++;Write-Host ('PASS rejects '+$Name)}else{$script:bootstrapFailures++;Write-Host ('FAIL accepted '+$Name)}}
Good-Bootstrap 'exact authored native mint and one Auto1 write' {$f=New-BootstrapFixture;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
$cases=[ordered]@{
 'missing selection'={param($f)$f.request.PSObject.Properties.Remove('nativeBootstrap')}
 'foreign scenario'={param($f)$f.request.scenario='fixture-intake';$f.artifact.scenario='fixture-intake'}
 'case substitution'={param($f)$f.request.nativeBootstrap.case='any-start'}
 'base-only string'={param($f)$f.request.nativeBootstrap.baseGameOnly='true'}
 'load quota'={param($f)$f.request.nativeBootstrap.expectedNativeLoadCount=1}
 'write quota'={param($f)$f.request.nativeBootstrap.expectedNativeWriteCount=2}
 'leaf substitution'={param($f)$f.request.nativeBootstrap.nativeWriteLeaf='Auto_2.zks'}
 'extra authority property'={param($f)$f.request.nativeBootstrap|Add-Member -NotePropertyName bypassLoader -NotePropertyValue $true}
 'working deserialized'={param($f)$f.artifact.observations.workingDeserialized=$true}
 'claim mounted combat'={param($f)$f.artifact.observations.mountedCombatProven=$true}
 'claim acquisition'={param($f)$f.artifact.observations.ordinaryAcquisitionProven=$true}
 'claim Small pose'={param($f)$f.artifact.observations.smallPoseQualified=$true}
 'Horse fault'={param($f)$f.artifact.observations.preflight.horseState='Faulted'}
 'unknown Ranger options'={param($f)$f.artifact.observations.preflight.rangerAllFeaturesCount=10}
 'unbounded Ranger list'={param($f)$f.artifact.observations.preflight.rangerFirst32Guids+=@('a'*32)}
 'registration GUID array'={param($f)$f.artifact.observations.preflight.horseFeatureGuid=@($f.artifact.observations.preflight.horseFeatureGuid)}
 'prior loaded area'={param($f)$f.artifact.observations.preflight.loadedAreaPresent=$true}
 'preflight write'={param($f)$f.artifact.observations.preflight.authorizedWrites=1}
 'foreign preset'={param($f)$f.artifact.observations.preset.source='DLC'}
 'preset area mismatch'={param($f)$f.artifact.observations.preset.enterPointArea='a'*32}
 'preset without autosave'={param($f)$f.artifact.observations.preset.makeAutosave=$false}
 'pre-minted world'={param($f)$f.artifact.observations.before.mainActor=5}
 'different Game'={param($f)$f.artifact.observations.nativeWorld.game=10}
 'different Player'={param($f)$f.artifact.observations.nativeWorld.player=10}
 'different SaveManager'={param($f)$f.artifact.observations.nativeWorld.saveManager=10}
 'world area array'={param($f)$f.artifact.observations.nativeWorld.area=@($f.artifact.observations.nativeWorld.area)}
 'world blueprint array'={param($f)$f.artifact.observations.nativeWorld.mainBlueprint=@($f.artifact.observations.nativeWorld.mainBlueprint)}
 'world mode array'={param($f)$f.artifact.observations.nativeWorld.mode=@('Default')}
 'main view absent'={param($f)$f.artifact.observations.nativeWorld.mainView=0}
 'foreign party main'={param($f)$f.artifact.observations.nativeWorld.party[0].id='55555555-1111-4111-8111-111111111111'}
 'mint equals Working'={param($f)$f.artifact.observations.nativeWorld.gameId=$f.request.fixture.working.gameId}
 'native load occurred'={param($f)$f.artifact.observations.nativeWorld.authority.authorizedLoads=1}
 'load cap nullable'={param($f)$f.artifact.observations.nativeWorld.authority.maximumNativeLoads=$null}
 'native write count text'={param($f)$f.artifact.observations.nativeWorld.authority.admittedNativeWrites='1'}
 'authority generation changed'={param($f)$f.artifact.observations.cleanup.authority.generation=3}
 'native restore occurred'={param($f)$f.artifact.observations.nativeWorld.semanticRestores=1}
 'unexpected cutscene'={param($f)$f.artifact.observations.nativeWorld.cutscene=$true}
 'native menu pending'={param($f)$f.artifact.observations.menuAdmission.alreadyEntering=$true}
 'native menu busy'={param($f)$f.artifact.observations.menuAdmission.nativeBusy=$true}
 'unexpected dialog'={param($f)$f.artifact.observations.menuAdmission.unexpectedDialog=$true}
 'menu scene absent'={param($f)$f.artifact.observations.menuAdmission.mainMenuSceneLoaded=$false}
 'observer partial install'={param($f)$f.artifact.observations.probe.installed=$false}
 'foreign hook module'={param($f)$f.artifact.observations.probe.hooks[0].moduleMvid=$f.request.dllMvid}
 'hook token array'={param($f)$f.artifact.observations.probe.hooks[1].token=@('06000DE3')}
 'observer errors'={param($f)$f.artifact.observations.probe.errors=@('native failure')}
 'observer duplicate mint'={param($f)$f.artifact.observations.probe.minted=2}
 'identity faults'={param($f)$f.artifact.observations.probe.identities.faults=1}
 'identity registry array'={param($f)$f.artifact.observations.probe.identities.contract=@('retained-native-object-identity-v1')}
 'unretained native object'={param($f)$f.artifact.observations.probe.identities.ids=@(1,2,3,4,5,6,7,8);$f.artifact.observations.probe.identities.retainedCount=8}
 'identity duplicate'={param($f)$f.artifact.observations.probe.identities.ids[1]=1}
 'event out of order'={param($f)$f.artifact.observations.probe.events[2].sequence=2}
 'foreign callback manager'={param($f)$f.artifact.observations.probe.events[4].saveManager=2}
 'mint outside newgame'={param($f)$f.artifact.observations.probe.events[1].insideNativeNewGame=$false}
 'event campaign array'={param($f)$f.artifact.observations.probe.events[1].gameId=@($f.artifact.observations.probe.events[1].gameId)}
 'event main array'={param($f)$f.artifact.observations.probe.events[1].mainActorId=@($f.artifact.observations.probe.events[1].mainActorId)}
 'event save category array'={param($f)$f.artifact.observations.probe.events[2].saveType=@('Auto')}
 'prepared descriptor replaced'={param($f)$f.artifact.observations.probe.events[4].save=7}
 'prepared write path mismatch'={param($f)$f.artifact.observations.probe.events[4].savePath='C:\foreign\Auto_1.zks'}
 'native artifact path escape'={param($f)$f.artifact.observations.nativeWrite.path='C:\foreign\Auto_1.zks'}
 'native artifact hash array'={param($f)$f.artifact.observations.nativeWrite.sha256=@('c'*64)}
 'output aliases protected source'={param($f)$f.artifact.observations.nativeWrite.sha256=$f.request.fixture.baseline.sha256}
 'cold load unproved'={param($f)$f.artifact.observations.nativeWrite.coldLoadQualified=$true}
 'callback not returned'={param($f)$f.artifact.observations.cleanup.callbackReturned=$false}
 'worker still live'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'observer retained'={param($f)$f.artifact.observations.cleanup.observerReleased=$false}
 'slot projection changed'={param($f)$f.artifact.observations.cleanup.slotProjectionAfter=$true}
 'identity window open'={param($f)$f.artifact.observations.cleanup.identityWindowOpen=$true}
}
foreach($case in $cases.GetEnumerator()){Bad-Bootstrap $case.Key {$f=New-BootstrapFixture;&$case.Value $f;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}}
Bad-Bootstrap 'historical mod-adjusted eight entries cannot prove base-only registration' {
 $f=New-BootstrapFixture;$p=$f.artifact.observations.preflight
 $p.rangerFirst32Guids=@('472091361cf118049a2b4339c4ea836a','f894e003d31461f48a02f5caec4e3359',
  'e992949eba096644784592dc7f51a5c7','aa92fea676be33d4dafd176d699d7996','2ee2ba60850dd064e8b98bf5c2c946ba',
  'ece6bde3dfc76ba4791376428e70621a','67a9dc42b15d0954ca4689b13e8dedea',$p.horseFeatureGuid)
 $p.rangerAllFeaturesCount=8;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact
}
for($index=0;$index-lt12;$index++){
 Bad-Bootstrap ('foreign native prefix entry '+$index) {$f=New-BootstrapFixture;$f.artifact.observations.preflight.rangerFirst32Guids[$index]='a'*32;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
 Bad-Bootstrap ('reordered native prefix entry '+$index) {$f=New-BootstrapFixture;$p=$f.artifact.observations.preflight;$next=($index+1)%12;$swap=$p.rangerFirst32Guids[$index];$p.rangerFirst32Guids[$index]=$p.rangerFirst32Guids[$next];$p.rangerFirst32Guids[$next]=$swap;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
}
Bad-Bootstrap 'equal-count duplicate Horse does not substitute for a native option' {$f=New-BootstrapFixture;$p=$f.artifact.observations.preflight;$p.rangerFirst32Guids[3]=$p.horseFeatureGuid;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
Good-Bootstrap 'preflight refusal preserves actual bounded list without claiming native start' {$f=New-BootstrapFixture;$f.artifact.status='FAIL';$f.artifact.errors=@('Ranger prerequisite missing');$f.artifact.observations=[pscustomobject]@{preflight=$f.artifact.observations.preflight};$f.artifact.observations.preflight.horseState='Faulted';$f.artifact.observations.preflight.rangerAllFeaturesCount=2;$f.artifact.observations.preflight.rangerFirst32Guids=@(('a'*32),$null);Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact 'FAIL'}
Good-Bootstrap 'prior enabled slot projection restores enabled state' {$f=New-BootstrapFixture;$f.artifact.observations.cleanup.slotProjectionBefore=$true;$f.artifact.observations.cleanup.slotProjectionAfter=$true;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
Write-Host ('TOTAL PASS='+$script:bootstrapPasses+' FAIL='+$script:bootstrapFailures)
@{status=if($script:bootstrapFailures-eq0){'PASS'}else{'FAIL'};pass=$script:bootstrapPasses;fail=$script:bootstrapFailures;readerSha256=(Get-FileHash -LiteralPath (Join-Path $bootstrapReader 'Chunk7NativeBootstrapEvidence.ps1')).Hash.ToLowerInvariant();archiveReads=0;nativeExecution=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bootstrapOutput 'receipt.json') -Encoding UTF8
if($script:bootstrapFailures-ne0){throw 'Native bootstrap strict evidence regressions failed.'}
