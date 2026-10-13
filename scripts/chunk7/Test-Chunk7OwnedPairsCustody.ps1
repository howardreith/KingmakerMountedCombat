[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsCustodyTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-pairs-custody-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualMethod([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing actual method '+$Signature)};$open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced actual method.'};$Text.Substring($start,$end-$start)}
$fixturePath=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.cs';$areaPath=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.Area.cs'
$fixture=[IO.File]::ReadAllText($fixturePath);$area=[IO.File]::ReadAllText($areaPath)
$require=Get-ActualMethod $fixture '        internal void RequirePreserved()'
$start=$fixture.IndexOf('        private JObject Arrangement() =>',[StringComparison]::Ordinal);$end=$fixture.IndexOf('        internal JObject Capture(', $start,[StringComparison]::Ordinal);if($start-lt0-or$end-le$start){throw 'Actual arrangement boundary absent.'};$arrangement=$fixture.Substring($start,$end-$start)
$areaProperties=@($area -split '\r?\n'|Where-Object{$_-match '^\s*private (bool AreaAutosaveCase|string ExpectedArchiveArea) =>'})-join[Environment]::NewLine
if(@($areaProperties -split '\r?\n').Count-ne2){throw 'Actual area property count changed.'}
$actual=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL_REQUIRE*/',$require).Replace('/*ACTUAL_ARRANGEMENT*/',$arrangement).Replace('/*AREA_PROPERTIES*/',$areaProperties)
$actualPath=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath $fixturePath -Destination (Join-Path $OutputDirectory 'FixtureSource.cs');Copy-Item -LiteralPath $areaPath -Destination (Join-Path $OutputDirectory 'AreaSource.cs')
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'owned-pairs-custody.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$arguments=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$arguments+=$actualPath
[IO.File]::WriteAllLines($rsp,@($arguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$watch=[Diagnostics.Stopwatch]::StartNew();&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual inherited-custody compile failed.'};$output=@(&$exe);$code=$LASTEXITCODE;$output|ForEach-Object{Write-Output $_};$watch.Stop()
@{fixtureSha256=(Get-FileHash -LiteralPath $fixturePath).Hash.ToLowerInvariant();areaSha256=(Get-FileHash -LiteralPath $areaPath).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();exitCode=$code;durationSeconds=$watch.Elapsed.TotalSeconds;results=$output;externalBoundaries='Native world/scope/authority and receipt filesystem substituted; exact RequirePreserved, Arrangement and area properties executed.';archivesRead=0;nativeActions=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual inherited-custody regression failed.'}
