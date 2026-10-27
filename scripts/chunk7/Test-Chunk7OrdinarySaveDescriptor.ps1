[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OrdinarySaveDescriptorTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/ordinary-save-descriptor-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Output must be fresh'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$diag=$DiagnosticSourceDirectory
$paths=@('NativeOrdinaryAcquisitionProbe.cs','Chunk7OrdinaryAcquisitionScenario.cs','Chunk7ManualDerivedScenario.Build.cs','Chunk7NativeProgressionSession.cs')|ForEach-Object{Join-Path $diag $_}
$templatePath=$TemplatePath;$receiptSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs'
$inputs=@($paths)+@($templatePath,$receiptSource,$PSCommandPath);$pins=@($inputs|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
$probe=[IO.File]::ReadAllText($paths[0]);$actual=$probe.Replace('typeof(Game).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")','false').Replace('method == null || method.MetadataToken != token','method == null')
if($actual-ceq$probe){throw 'Exact metadata substitution absent'}
$template=[IO.File]::ReadAllText($templatePath)
function Prefix([string]$Text,[string]$Signature,[string]$Name){$s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);$e=$Text.IndexOf('            NativePersistenceIsolation.RequireOwnedWrite(save);',$s,[StringComparison]::Ordinal);if($s-lt0-or$e-le$s){throw 'Actual predicate prefix missing'};return ($Text.Substring($s,$e-$s).Replace($Signature,('        internal bool '+$Name+'()'))+'            return true;'+[Environment]::NewLine+'        }')}
$template=$template.Replace('/*ACTUAL_ORDINARY*/',(Prefix ([IO.File]::ReadAllText($paths[1])) '        private bool ObserveCompletedAutosave()' 'Ordinary'))
# The Manual-derived family no longer has a level-up Auto completion: it completes with its guarded
# native Manual (ObserveManualDerivedSave), covered by the Manual-derived checks. Only the two retained
# Auto observers (ordinary acquisition and legacy progression, both refused before launch) remain here.
$template=$template.Replace('/*ACTUAL_MANUAL*/','')
$template=$template.Replace('/*ACTUAL_PROGRESSION*/',(Prefix ([IO.File]::ReadAllText($paths[3])) '        private bool ObserveTerminalAutosave()' 'Progression'))
$receiptText=[IO.File]::ReadAllText($receiptSource);$start=$receiptText.IndexOf('    internal sealed class NativeSaveCompletion',[StringComparison]::Ordinal);$body=$receiptText.Substring($start);$body=$body.Substring(0,$body.LastIndexOf('}'));$template=$template.Replace('/*ACTUAL_RECEIPT*/',$body)
$a=Join-Path $OutputDirectory 'ActualProbe.cs';$b=Join-Path $OutputDirectory 'ActualCompletionTests.cs';[IO.File]::WriteAllText($a,$actual,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText($b,$template,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'DescriptorTests.exe';$args=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/nowarn:0162,0649,0169,0414',('/out:'+$exe),('/reference:'+$json));$args+=@('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$args+=@($a,$b)
$rsp=Join-Path $OutputDirectory 'compile.rsp';[IO.File]::WriteAllLines($rsp,@($args|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));$watch=[Diagnostics.Stopwatch]::StartNew();& $compiler ('@'+$rsp);$compileExit=$LASTEXITCODE
$testExit=$null;$output=@();if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$output=@(& $exe 2>&1);$testExit=$LASTEXITCODE;$output|ForEach-Object{Write-Host $_}}
$watch.Stop();$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'output.log') -Encoding UTF8
$unchanged=$true;foreach($p in $pins){if((Get-FileHash -LiteralPath $p.path).Hash.ToLowerInvariant()-cne$p.sha256){$unchanged=$false}}
$result=[ordered]@{schemaVersion=1;compileExit=$compileExit;testExit=$testExit;pass=@($output|Where-Object{$_-cmatch'^PASS '}).Count;fail=@($output|Where-Object{$_-cmatch'^FAIL '}).Count;elapsedSeconds=$watch.Elapsed.TotalSeconds;inputs=$pins;sourceUnchanged=$unchanged;generated=@($a,$b,$rsp)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}};compiler=@{path=$compiler;sha256=(Get-FileHash -LiteralPath $compiler).Hash.ToLowerInvariant()};nativeInvoked=$false;archiveRead=$false;scope='Actual complete probe except native metadata token/MVID seams; actual three consumer method prefixes through completed-save predicate; external native/Harmony/game objects only.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($result|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false));if($compileExit-ne0-or$testExit-ne0-or-not$unchanged){throw 'Actual ordinary descriptor regression failed; evidence preserved.'}
