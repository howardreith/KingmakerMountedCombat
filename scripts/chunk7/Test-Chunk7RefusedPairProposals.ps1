[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/refused-pair-proposals-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
# Compiles the actual RefusedPairProposals against the game's own Newtonsoft and feeds it trace-shaped rows.
$sourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatScenario.OwnerSeed.cs';$text=[IO.File]::ReadAllText($sourcePath)
$signature='        internal static JArray RefusedPairProposals(';$start=$text.IndexOf($signature,[StringComparison]::Ordinal)
if($start-lt0){throw 'Missing actual method RefusedPairProposals'}
$open=$text.IndexOf('{',$start);$depth=1;$end=$open+1
while($depth-gt0-and$end-lt$text.Length){if($text[$end]-ceq'{'){$depth++}elseif($text[$end]-ceq'}'){$depth--};$end++}
if($depth-ne0){throw 'Unbalanced RefusedPairProposals extraction'}
$templatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/RefusedPairProposalsTemplate.cs'
# F2 r09: the native attack-of-opportunity accounting block, extracted whole (constants and methods before FinishCompanionAction).
$aooStart=$text.IndexOf('        internal const string OpportunityAttackType',[StringComparison]::Ordinal);$aooEnd=$text.IndexOf('        private bool FinishCompanionAction()',[StringComparison]::Ordinal)
if($aooStart-lt0-or$aooEnd-le$aooStart){throw 'Missing actual native attack-of-opportunity accounting'}
$body=[IO.File]::ReadAllText($templatePath).Replace('/*ACTUAL_METHOD*/',$text.Substring($start,$end-$start)).Replace('/*ACTUAL_OPPORTUNITY*/',$text.Substring($aooStart,$aooEnd-$aooStart))
$adapter=Join-Path $OutputDirectory 'ActualRefusedPairProposals.cs';[IO.File]::WriteAllText($adapter,$body,[Text.UTF8Encoding]::new($false))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'RefusedPairProposals.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$compileArgs=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+',('/out:'+$exe),('/reference:'+$json))
$compileArgs+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$compileArgs+=@($adapter)
[IO.File]::WriteAllLines($rsp,@($compileArgs|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'RefusedPairProposals actual-method compile failed'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
&$exe;if($LASTEXITCODE-ne0){throw 'RefusedPairProposals actual-method regression failed'}
@{sourceSha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $templatePath -Algorithm SHA256).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'refused-pair-proposals-source.json')
