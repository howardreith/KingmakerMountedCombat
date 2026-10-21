[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$ReaderPath,[Parameter(Mandatory=$true)][string]$FixtureManifestPath,[Parameter(Mandatory=$true)][string]$OutputRoot,[string[]]$CaseNames=@())
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$m=Get-Content -LiteralPath $FixtureManifestPath -Raw|ConvertFrom-Json
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixturesOnly
. $m.choiceHelper.path;. $ReaderPath;. $m.fixtures.path;. $m.builder.path
$output=Assert-KmcChildPath $OutputRoot (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated full evidence output composition'
if(Test-Path -LiteralPath $output){throw 'Preserve prior generated output composition.'};[void][IO.Directory]::CreateDirectory($output)
Assert-KmcDirectoryTreeCloneable $output 'generated full evidence output composition'
foreach($p in @($m.host,$m.core,$m.choiceHelper,$m.fixtures,$m.builder)){if((Get-KmcSha256 $p.path)-cne$p.sha256-or(Get-Item -LiteralPath $p.path).Length-ne$p.length){throw 'Generated composition input changed.'}}
$script:hostFragment=Read-KmcJson $m.host.path;$script:coreFragment=Read-KmcJson $m.core.path
$script:compositionLab=$null
# Only the laboratory root is rebound to this fresh generated directory. Full
# Evidence, Output and Archive execute unchanged. The complete native observations
# are controlled fixtures, not native proof or qualification for a real source.
function Get-KmcLabRoot {$script:compositionLab}
function Hash-CompositionText([string]$Text){$sha=[Security.Cryptography.SHA256]::Create();try{[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false,$true).GetBytes($Text))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}
function Set-CompositionFile($Value,[string]$Path){$file=Get-Item -LiteralPath $Path;$Value.sha256=Get-KmcSha256 $Path;$Value.length=[long]$file.Length;if($Value.PSObject.Properties['lastWriteTimeUtcTicks']){$Value.lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}else{$Value|Add-Member NoteProperty lastWriteTimeUtcTicks ([long]$file.LastWriteTimeUtc.Ticks)}}
function Write-CompositionArchive($Write,[bool]$ForeignHeader){
 Add-Type -AssemblyName System.IO.Compression
 $header=[pscustomobject]@{Name=$Write.internalName;Type='Manual';GameId=$(if($ForeignHeader){'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'}else{$Write.gameId});GameName=$Write.gameName;Area=$Write.area;CompatibilityVersion=1;IsAutoLevelupSave=$false}
 $archive=[pscustomobject]@{SchemaVersion=3;CampaignId=$Write.gameId;AreaId=$Write.area;GameTimeTicks=1000L;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=@();Participations=@();Slots=@();Combat=$null}
 $stream=$null;$zip=$null
 try{
  $stream=[IO.FileStream]::new($Write.path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
  foreach($pair in @(@('header.json',$header),@('kmc-mounted-state',$archive))){$entry=$null;try{$entry=$zip.CreateEntry($pair[0]).Open();$bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes(($pair[1]|ConvertTo-Json -Depth 20 -Compress));$entry.Write($bytes,0,$bytes.Length)}finally{if($entry){$entry.Dispose()}}}
 }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
}
$clock=[Diagnostics.Stopwatch]::StartNew();$cases=[Collections.Generic.List[object]]::new();$script:passed=0;$script:failed=0;$script:caseNumber=0
function Run-OutputComposition([string]$Name,[int]$Writes,[bool]$Expected,[scriptblock]$Mutation=$null,[bool]$ForeignHeader=$false,[bool]$Built=$false){
 if($CaseNames.Count-gt0-and$Name-cnotin$CaseNames){return}
 $script:caseNumber++;$script:compositionLab=Join-Path $output ('case-'+$script:caseNumber);[void][IO.Directory]::CreateDirectory($script:compositionLab)
 $ready=$false;$accepted=$true;$errorText=$null;$stack=$null
 try{
  $f=if($Writes-eq2){if($Built){New-AssistedTwoWriteBuiltFixture}else{New-AssistedTwoWriteFixture}}else{New-AssistedCompositionFixture};$o=$f.artifact.observations;$root=Get-KmcAssistedSaveRoot $f.request
  [void][IO.Directory]::CreateDirectory($root)
  $anchorPath=Join-Path $root $f.request.fixture.working.fileName;[IO.File]::WriteAllText($anchorPath,'generated immutable Working anchor',[Text.UTF8Encoding]::new($false));Set-CompositionFile $f.request.fixture.working $anchorPath
  $auto=$o.nativeWrites[$Writes-2];[IO.File]::WriteAllText($auto.path,'generated current native Auto boundary',[Text.UTF8Encoding]::new($false));Set-CompositionFile $auto $auto.path
  $manual=$o.nativeWrites[-1];Write-CompositionArchive $manual $ForeignHeader;Set-CompositionFile $manual $manual.path;$o.finalManual=Copy-O $manual
  # Establish that the generated file projection did not invalidate the full
  # controlled native artifact before applying the single selected mutation.
  Assert-KmcChunk7AssistedFixtureEvidence $f.request $f.artifact;$ready=$true
  if($Mutation){&$Mutation $f $root}
  Assert-KmcChunk7AssistedFixtureArchive $f.request $f.artifact
 }catch{$accepted=$false;$errorText=$_.Exception.Message;$stack=$_.ScriptStackTrace}
 $ok=$ready-and($accepted-eq$Expected);if($ok){$script:passed++}else{$script:failed++}
 $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Expected;actualAccepted=$accepted;fixtureReady=$ready;error=$errorText;stack=$stack})
 Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok-and$errorText){': '+$errorText}else{''}))
}
Run-OutputComposition 'complete two-write artifact and actual generated Manual archive' 2 $true
Run-OutputComposition 'native Ranger build two-write artifact and actual generated Manual archive' 2 $true $null $false $true
Run-OutputComposition 'complete three-write artifact accepts replaced initial Auto bytes' 3 $true
Run-OutputComposition 'coherently pinned Manual with foreign header campaign refused' 3 $false $null $true
Run-OutputComposition 'current Auto byte drift refused after full evidence passes' 3 $false {param($f,$root)[IO.File]::AppendAllText((Join-Path $root 'Auto_1.zks'),'drift')}
Run-OutputComposition 'Working anchor byte drift refused after full evidence passes' 2 $false {param($f,$root)[IO.File]::AppendAllText((Join-Path $root $f.request.fixture.working.fileName),'drift')}
Run-OutputComposition 'valid file bytes cannot authorize unsettled cleanup' 3 $false {param($f,$root)$f.artifact.observations.cleanup.custodySettled=$false}
if($cases.Count-eq0){throw 'No output composition cases selected.'}
foreach($p in @($m.host,$m.core,$m.choiceHelper,$m.fixtures,$m.builder)){if((Get-KmcSha256 $p.path)-cne$p.sha256-or(Get-Item -LiteralPath $p.path).Length-ne$p.length){throw 'Generated composition input changed after checks.'}}
$receipt=[ordered]@{kind='generated-assisted-full-output-composition';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;durationSeconds=$clock.Elapsed.TotalSeconds;cases=$cases.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;scope='Real full Evidence, Output and bounded Archive joined with generated composition and generated file bytes; laboratory root and native observations are explicitly controlled; no native qualification'}
$receipt|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'receipt.json') -Encoding UTF8
if($script:failed-ne0){exit 1};exit 0
