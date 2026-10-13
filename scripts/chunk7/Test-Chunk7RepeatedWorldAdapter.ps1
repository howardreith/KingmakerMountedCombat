$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
function Get-ActualReplayMethod([string]$Text,[string]$Signature){$a=$Text.IndexOf($Signature);if($a-lt0){throw ('Missing exact method '+$Signature)};$o=$Text.IndexOf('{',$a);$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-eq'{'){$d++};if($Text[$e]-eq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced exact method'};$Text.Substring($a,$e-$a)}
$scenario=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.cs'
$replay=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.Replay.cs'
$text=[IO.File]::ReadAllText($replay)
$methods=(Get-ActualReplayMethod ([IO.File]::ReadAllText($scenario)) 'internal void BeginBeforeLoad()')
foreach($signature in @('private void InitializeReplayAuthority()','private void InstallReplayObservation()','private void RequireReplayAuthority(','private void AdmitOwnedRepeatedLoad(','private void DispatchRepeatedLoad()','private void DispatchRepeatedArea()','private void RequireRepeatedCleanupWorld()','private void CaptureRepeatedSettlement()','private void EndReplayObservation()')){$methods+=[Environment]::NewLine+(Get-ActualReplayMethod $text $signature)}
$probe=Join-Path $chunk7AnalysisRoot ('replay-adapter-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$retention=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldRetentionProbe.cs'))
$retentionMethods=(Get-ActualReplayMethod $retention 'internal void Install()')+[Environment]::NewLine+(Get-ActualReplayMethod $retention 'public void Dispose()')
$adapter=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'RepeatedWorldAdapterTemplate.cs')).Replace('__METHODS__',$methods).Replace('__RETENTION_METHODS__',$retentionMethods)
[IO.File]::WriteAllText((Join-Path $probe 'RepeatedWorldAdapter.cs'),$adapter)
[ordered]@{scenarioSha256=(Get-FileHash -LiteralPath $scenario -Algorithm SHA256).Hash.ToLowerInvariant();replaySha256=(Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant();source='Actual methods; mock only native external boundary';nativeEvidence=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'source-receipt.json')
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'RepeatedWorldAdapter.exe'
& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 /reference:Microsoft.CSharp.dll "/out:$exe" (Join-Path $probe 'RepeatedWorldAdapter.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs')
if($LASTEXITCODE-ne0){throw 'Actual repeated-world adapter compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Actual repeated-world boundary regression failed.'}
