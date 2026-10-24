[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$FieldRosterSource,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('FieldRosterSource')){$FieldRosterSource=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeFieldRoster.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedProgressionRosterTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-progression-roster-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualMethod([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0-or$Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Missing or repeated actual method '+$Signature)};$open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced actual method.'};$Text.Substring($start,$end-$start)}
$source=Join-Path $DiagnosticSourceDirectory 'Chunk7ProgressionScenario.Owned.cs';$session=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeProgressionSession.cs'
$text=[IO.File]::ReadAllText($source);$sessionText=[IO.File]::ReadAllText($session);$fieldText=[IO.File]::ReadAllText($FieldRosterSource)
$actual=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL_ROSTER*/',(Get-ActualMethod $text 'private void RequireOwnedRoster(bool final)')).Replace('/*ACTUAL_PROJECTION*/',(Get-ActualMethod $sessionText 'internal bool TryGetObservedTargetRoster')).Replace('/*ACTUAL_PET_SETTLEMENT*/',(Get-ActualMethod $text 'private bool OwnedRemovedPetSettled()'))
$field=(Get-ActualMethod $fieldText 'internal static UnitEntityData[] Capture(Player player)')+[Environment]::NewLine+(Get-ActualMethod $fieldText 'internal static T[] Capture<T>(')
$actual=$actual.Replace('/*ACTUAL_FIELD_CAPTURE*/',$field)
$actualPath=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual,[Text.UTF8Encoding]::new($false))
foreach($p in @($source,$session,$FieldRosterSource,$TemplatePath)){Copy-Item -LiteralPath $p -Destination (Join-Path $OutputDirectory (Split-Path -Leaf $p))}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'owned-progression-roster.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$arguments+=$actualPath
[IO.File]::WriteAllLines($rsp,@($arguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$watch=[Diagnostics.Stopwatch]::StartNew();&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual owned progression roster compilation failed.'};$output=@(&$exe);$code=$LASTEXITCODE;$output|ForEach-Object{Write-Output $_};$watch.Stop()
[ordered]@{status=$(if($code-eq0){'PASS'}else{'FAIL'});exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;results=$output;inputs=@(@($source,$session,$FieldRosterSource,$TemplatePath)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});actualSha256=(Get-FileHash -LiteralPath $actualPath).Hash.ToLowerInvariant();externalBoundaries='Game types and existing retained native callback probe represented at their external boundary; actual roster, lifecycle projection, settlement and shared native roster capture methods executed.';nativeInvoked=$false;archiveContentRead=$false}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual owned progression roster regression failed.'}
