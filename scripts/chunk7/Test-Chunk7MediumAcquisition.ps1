[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$ReaderDirectory,[string]$TestDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$TestDirectory){$TestDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/medium-acquisition-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing focused output.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$newtonsoft='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$sources=@();foreach($leaf in @('Chunk7ManualDerivedSelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7MediumHireBodySource.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7EvidenceDto.cs')){
 $source=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $source)){$source=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};$sources+=,$source
}
$sources+=,(Join-Path $TestDirectory 'MediumAcquisitionTests.cs');$dll=Join-Path $OutputDirectory 'medium-tests.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$compilerArgs=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/nostdlib+',('/out:'+$dll),('/reference:'+$newtonsoft))
$compilerArgs+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArgs+$sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual Medium DTO/source compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft);[void][Reflection.Assembly]::LoadFrom($dll)
# The frozen fixture predates the reader correction that records the round hook by its actual native
# name and type (UnitCombatState.OnNewRound in Kingmaker.Controllers.Combat), as in retained evidence.
$mediumPath=Join-Path $TestDirectory 'ManualDerivedMediumFixture.json';$raw=[IO.File]::ReadAllText($mediumPath) -replace '("method":\s*)"NewRound"','$1"OnNewRound"' -replace '("declaringType":\s*)"Kingmaker\.UnitLogic\.UnitCombatState"','$1"Kingmaker.Controllers.Combat.UnitCombatState"'
$small=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualDerivedPaidFixture.json'));$existing=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualDerivedExistingFixture.json'))
$compiled=[KingmakerMountedCombat.Tests.MediumAcquisitionTests]::Run($raw,$small,$existing)
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ManualDerivedEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixtureDirectory (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics') -OutputDirectory $OutputDirectory -FixturesOnly
$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Mutate){$f=$raw|ConvertFrom-Json;$rejected=$false;try{&$Mutate $f;Check-ManualDerived $f}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS refuses '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
Good 'complete synthetic Medium proof' {$f=$raw|ConvertFrom-Json;Check-ManualDerived $f}
Good 'actual DTO projection passes complete strict reader' {$f=$raw|ConvertFrom-Json;$f.artifact.observations.selection=[KingmakerMountedCombat.Tests.MediumAcquisitionTests]::Project($raw)|ConvertFrom-Json;Check-ManualDerived $f}
$cases=[ordered]@{
 'legacy Small case cannot borrow Medium pins'={param($f)$f.request.manualDerived.case='paid-small-ranger-horse';$f.artifact.observations.selection.case='paid-small-ranger-horse'}
 'missing typed body'={param($f)$f.request.manualDerived.mediumBody=$null}
 'borrowed existing actor'={param($f)$f.request.manualDerived.existingActorId=$f.request.manualDerived.readiness.mainActorId}
 'borrowed legacy Small pin'={param($f)$f.request.manualDerived.smallRaceGuid='a'*32}
 'unknown body field'={param($f)$f.request.manualDerived.mediumBody|Add-Member -NotePropertyName ignoredAuthority -NotePropertyValue $true}
 'template outside field characters'={param($f)$f.request.manualDerived.mediumBody.templateActorId='99999999-1111-4111-8111-111111111111'}
 'coerced handedness'={param($f)$f.request.manualDerived.mediumBody.leftHanded='false'}
 'unqualified live body'={param($f)$f.artifact.observations.liveEligibility.exactMediumBodyMember=$false}
 'source body original Small'={param($f)$f.source.observations.nativeProfileInventory.party[0].originalSize='Small';$f.artifact.observations.sourceManualObservations.nativeProfileInventory.party[0].originalSize='Small'}
 'source native rig differs'={param($f)$f.source.observations.nativeProfileInventory.party[0].character.skeleton.name='Foreign';$f.artifact.observations.sourceManualObservations.nativeProfileInventory.party[0].character.skeleton.name='Foreign'}
 'current template effective Small'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].effectiveSize='Small'}
 'current template substituted'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].raceGuid='a'*32}
 'current doll identity differs'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].dollRaceId='Elf'}
 'current template polymorphed'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].polymorphed=$true}
 'current malformed character array'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].character=@()}
 'current malformed skeleton array'={param($f)$f.artifact.observations.nativeProfileInventory.party[0].character.skeleton=@()}
 'paid field changed size'={param($f)$f.artifact.observations.field.bodyObservation.EffectiveSizeOrdinal=3}
 'paid field changed race'={param($f)$f.artifact.observations.field.bodyObservation.RaceGuid='a'*32}
 'paid field changed sex'={param($f)$f.artifact.observations.field.bodyObservation.Gender='Female'}
 'native field membership removed main'={param($f)$f.artifact.observations.field.partyCharacters=@($f.artifact.observations.field.actorId)}
 'native cost refunded'={param($f)$f.artifact.observations.afterChoices.money++}
 'native XP manufactured'={param($f)$f.artifact.observations.liveEligibility.defaultHireExperience++}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key $case.Value}
@{schemaVersion=1;compiledPass=$compiled;readerPass=$script:passes;fail=$script:failures;nativeExecution=$false;archiveAccess=$false;fixtureKind='synthetic-json-only';sources=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});fixtureSha256=(Get-FileHash -LiteralPath $mediumPath).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL compiled='+$compiled+' reader='+$script:passes+' FAIL='+$script:failures)
if($compiled-lt1-or$script:passes-lt1-or$script:failures-ne0){throw 'Focused Medium acquisition checks failed.'}
