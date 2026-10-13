[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$CleanupSourceDirectory,[string]$LeaseSourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$CleanupSourceDirectory){$CleanupSourceDirectory=$DiagnosticSourceDirectory}
if(-not$LeaseSourcePath){$LeaseSourcePath=Join-Path $DiagnosticSourceDirectory 'MovementScreenshotCaptureState.cs'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OverlayCustodyTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/scenario-overlay-custody-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing caller-custody evidence.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$started=[Diagnostics.Stopwatch]::StartNew()
$classes=[Collections.Generic.List[string]]::new();$calls=[Collections.Generic.List[string]]::new();$inputs=[Collections.Generic.List[object]]::new()
$leaves=@('Chunk7CombatScenario.cs','Chunk7PersistenceScenario.cs','Chunk7ProgressionScenario.cs','Chunk7SmallCalibrationScenario.cs','Chunk7OrdinaryFieldScenario.cs','Chunk7OrdinaryAcquisitionScenario.cs','Chunk7WorldScenarioEngine.cs')
foreach($leaf in $leaves){
 $path=Join-Path $DiagnosticSourceDirectory $leaf;$source=[IO.File]::ReadAllText($path)
 $field=if($leaf-ceq'Chunk7CombatScenario.cs'){'umm'}else{'ummLease'}
 $fields=[regex]::Matches($source,('private\s+(?:IDisposable|MovementCaptureOverlayLease)\s+'+$field+'\s*;'))
 $acquires=[regex]::Matches($source,('(?:\b'+$field+'\s*=\s*MovementScreenshotCaptureCoordinator\.AcquireClosedUmmLease\(\)|\bMovementScreenshotCaptureCoordinator\.AcquireClosedUmmLeaseRetained\(out\s+'+$field+'\));'))
 $cleanupLeaf=if($leaf-ceq'Chunk7CombatScenario.cs'){'Chunk7CombatCleanup.cs'}elseif($leaf-ceq'Chunk7SmallCalibrationScenario.cs'){'Chunk7SmallCalibrationCleanup.cs'}else{$leaf}
 $cleanupPath=Join-Path $CleanupSourceDirectory $cleanupLeaf;$cleanup=[IO.File]::ReadAllText($cleanupPath)
 $releases=[regex]::Matches($cleanup,($field+'\?\.Dispose\(\);\s*'+$field+'\s*=\s*null;'))
 if($fields.Count-ne1-or$acquires.Count-ne1-or$releases.Count-ne1){throw ('Exact caller acquisition/release statement is absent or ambiguous: '+$leaf)}
 $name='Caller'+$classes.Count
 $classes.Add('internal sealed class '+$name+' : ICallerProbe { '+$fields[0].Value+' public bool HasCustody {get{return '+$field+'!=null;}} public void Capture(){'+$acquires[0].Value+'} public void Cleanup(){'+$releases[0].Value+'} }')
 $calls.Add('Run("'+$leaf+'",()=>new '+$name+'());')
 $inputs.Add([pscustomobject]@{source=$path;sourceSha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();cleanup=$cleanupPath;cleanupSha256=(Get-FileHash -LiteralPath $cleanupPath).Hash.ToLowerInvariant();field=$fields[0].Value;acquire=$acquires[0].Value;release=$releases[0].Value})
}
$template=[IO.File]::ReadAllText($TemplatePath)
foreach($marker in @('//__ACTUAL_CALLERS__','//__CALL_TESTS__')){if(([regex]::Matches($template,[regex]::Escape($marker))).Count-ne1){throw 'Caller test template marker is absent or ambiguous.'}}
$body=$template.Replace('//__ACTUAL_CALLERS__',([string]::Join([Environment]::NewLine,$classes))).Replace('//__CALL_TESTS__',([string]::Join([Environment]::NewLine,$calls)))
$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'actual.exe'
$referenceRoot='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $referenceRoot $_)}
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ /nostdlib+ /deterministic+ "/out:$exe" $references $LeaseSourcePath $generated *> (Join-Path $OutputDirectory 'compile.log')
$compileExit=$LASTEXITCODE;$testExit=$null
if($compileExit-eq0){&$exe *> (Join-Path $OutputDirectory 'test.log');$testExit=$LASTEXITCODE}
$started.Stop()
[ordered]@{schemaVersion=1;status=$(if($compileExit-eq0-and$testExit-eq0){'PASS'}else{'FAIL'});compileExit=$compileExit;testExit=$testExit;seconds=$started.Elapsed.TotalSeconds;callers=@($inputs);lease=$LeaseSourcePath;leaseSha256=(Get-FileHash -LiteralPath $LeaseSourcePath).Hash.ToLowerInvariant();template=$TemplatePath;templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();nativeProof=$false;boundary='Actual seven field/acquire/release statements and actual production lease; substituted external UMM adapter. No complete scenario or native game execution.'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($compileExit-ne0){throw 'Actual caller-custody compile failed.'}
Get-Content -LiteralPath (Join-Path $OutputDirectory 'test.log')
if($testExit-ne0){throw 'Actual caller-custody regression failed.'}
