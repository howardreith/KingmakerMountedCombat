[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-archive-adapter-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair archive adapter test'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve earlier six-pair archive checks.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Method([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal)
 if($start-lt0-or$start-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw ('Actual method absent or ambiguous: '+$Signature)}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual archive method'};$Text.Substring($start,$end-$start)
}
$integration=Join-Path $repo 'src/KingmakerMountedCombat/Integration';$domain=Join-Path $repo 'src/KingmakerMountedCombat/Domain'
$worldPath=Join-Path $integration 'MountedPersistenceService.World.cs';$servicePath=Join-Path $integration 'MountedPersistenceService.cs'
$combatPath=Join-Path $integration 'MountedCombatPersistenceService.cs';$nativeWorldPath=Join-Path $integration 'MountedWorldService.cs'
$world=[IO.File]::ReadAllText($worldPath);$service=[IO.File]::ReadAllText($servicePath);$combat=[IO.File]::ReadAllText($combatPath);$nativeWorld=[IO.File]::ReadAllText($nativeWorldPath)
$template=Join-Path $chunk7TestSource 'SixPairArchiveAdapterTemplate.cs';$actual=[IO.File]::ReadAllText($template)
foreach($pair in @(@('CAPTURE','private MountedArchiveData CaptureArchive()'),@('RESOLVE','private bool ResolveArchiveGraph()'),
 @('IDENTITY','private void ValidateArchiveActorIdentity()'),@('PREPARE','private bool PrepareArchiveOwners()'),@('PRESENTATION','private bool RestoreArchivePresentation()'))){
 $actual=$actual.Replace(('/*ACTUAL_'+$pair[0]+'*/'),(Method $world $pair[1]))
}
$actual=$actual.Replace('/*ACTUAL_BLOCK*/',(Method $combat 'private void BlockCombatRestoration(string reason)')).Replace('/*ACTUAL_UPDATE*/',(Method $service 'internal void Update()'))
$start=$combat.IndexOf('private bool CombatSemanticRestorationPending =>',[StringComparison]::Ordinal);$end=$combat.IndexOf(';',$start)
if($start-lt0-or$end-le$start){throw 'Actual restoration pending property absent.'}
$actual=$actual.Replace('/*ACTUAL_PENDING*/',$combat.Substring($start,$end-$start+1))
$actual=$actual.Replace('/*ACTUAL_WORLD_VISIT*/',(Method $nativeWorld 'internal Exception[] Visit(Action<MountedPairServices> action)')).Replace('/*ACTUAL_WORLD_DISPOSE*/',(Method $nativeWorld 'public void Dispose()'))
$actualPath=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual,[Text.UTF8Encoding]::new($false))
$sources=@($actualPath)
$sources+=@('MountedSaveData.cs','SavedCombatData.cs','MountedArchiveData.cs','MountedArchiveCodec.cs')|ForEach-Object{Join-Path $integration $_}
$sources+=@('MountedPairRegistry.cs','RelationshipState.cs','PairedActivationSnapshot.cs','DeclaredSmallRiderBody.cs','MountedRiderProfileCatalog.cs','MountedRiderPoseProfile.cs','PoseVector3.cs')|ForEach-Object{Join-Path $domain $_}
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'six-archive-adapter.exe'
$pins=@(@($template,$worldPath,$servicePath,$combatPath,$nativeWorldPath)+$sources|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}})
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" "/reference:$json" $sources
if($LASTEXITCODE-ne0){throw 'Actual six-pair archive adapter compile failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
# Use the existing bounded Unity-only resolver for installed Newtonsoft's
# vector converter. Proprietary dependencies stay read-only in the install.
$pins+=@($json,(Join-Path (Split-Path $json -Parent) 'UnityEngine.dll'),(Join-Path (Split-Path $json -Parent) 'UnityEngine.CoreModule.dll'))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}}
$output=@(&$exe $json);$code=$LASTEXITCODE;$output|ForEach-Object{Write-Output $_}
[ordered]@{nativeInvoked=$false;sourcePins=$pins;exitCode=$code;results=$output}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual six-pair archive adapter behavior failed.'}
