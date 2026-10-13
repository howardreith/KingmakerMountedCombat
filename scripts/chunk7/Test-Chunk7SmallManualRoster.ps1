[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$ScenarioPath,[string]$RosterSource,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$SourcePath){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationScenario.Manual.cs'}
if(-not$ScenarioPath){$ScenarioPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationScenario.cs'}
if(-not$RosterSource){$RosterSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeFieldRoster.cs'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualRosterAdapterTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-roster-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Exact([string]$text,[string]$signature){$start=$text.IndexOf($signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing exact method '+$signature)};$open=$text.IndexOf('{',$start);$end=$open+1;$depth=1;while($depth-and$end-lt$text.Length){if($text[$end]-ceq'{'){$depth++}elseif($text[$end]-ceq'}'){$depth--};$end++};if($depth){throw 'Unbalanced method'};$text.Substring($start,$end-$start)}
$source=[IO.File]::ReadAllText($SourcePath);$scenario=[IO.File]::ReadAllText($ScenarioPath)
if($source.Contains('        private UnitEntityData[] CaptureManualFieldRoster()')){$capture=Exact $source '        private UnitEntityData[] CaptureManualFieldRoster()';$boundary='actual full CaptureManualFieldRoster method'}else{
 $a=$source.IndexOf('            var sourceWorld = (JObject)manualSource["savedWorld"];',[StringComparison]::Ordinal);$b=$source.IndexOf('            if (current.Any',$a,[StringComparison]::Ordinal);$c=$source.IndexOf('            actor = Exact',$b,[StringComparison]::Ordinal);$d=$source.IndexOf('            var body = NativeSmallRiderBody.Capture',$c,[StringComparison]::Ordinal);if($a-lt0-or$b-le$a-or$c-le$b-or$d-le$c){throw 'Original roster predicates missing'}
 $capture='private UnitEntityData[] CaptureManualFieldRoster(){'+$source.Substring($a,$b-$a)+$source.Substring($c,$d-$c)+'return current;}';$boundary='actual pre-fix two roster predicate spans, view-readiness block excluded'
}
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*CAPTURE*/',$capture).Replace('/*IDENTITY*/',(Exact $source '        private void RequireManualFieldIdentity()')).Replace('/*EXACT*/',(Exact $scenario '        private UnitEntityData Exact(string id)'))
$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false));$dll=Join-Path $OutputDirectory 'actual.dll'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
&$compiler /nologo /target:library /langversion:7.3 /nowarn:0649 "/reference:$json" /reference:Microsoft.CSharp.dll "/out:$dll" $generated $RosterSource;if($LASTEXITCODE-ne0){throw 'Actual roster adapter compile failed.'}
[void][Reflection.Assembly]::LoadFrom($json);[void][Reflection.Assembly]::LoadFrom($dll);$failed=[ActualSmallRoster]::Verify()
@{sourceSha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();scopeHelperSha256=(Get-FileHash -LiteralPath $RosterSource).Hash.ToLowerInvariant();extractedSha256=(Get-FileHash -LiteralPath $generated).Hash.ToLowerInvariant();fail=$failed;boundary=$boundary;external='native Game/Player/actor metadata and immutable arrangement snapshot are substituted; shared capture and exact actor lookup/identity methods are real';nativeInvoked=$false;archiveReads=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($failed){throw 'Actual Small Manual roster checks failed.'}
