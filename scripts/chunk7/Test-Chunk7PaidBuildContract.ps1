[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$BuildSource,[string]$TestTemplate,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$BuildSource){$BuildSource=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedScenario.Build.cs'}
if(-not$TestTemplate){$TestTemplate=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/PaidBuildContractTestTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/paid-build-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve paid build test output.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$driver=Join-Path $DiagnosticSourceDirectory 'Chunk7OrdinaryBuildDriver.cs';$raw=[IO.File]::ReadAllText($driver)
$start=$raw.IndexOf('    internal enum Chunk7OrdinaryBuildMode',[StringComparison]::Ordinal);$end=$raw.IndexOf('    internal sealed class Chunk7OrdinaryBuildDriver',$start,[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Actual pure build-pin source boundaries missing.'}
$fragment='using System;using System.Text.RegularExpressions;using Kingmaker.Enums;using Kingmaker.Blueprints;using Kingmaker.UnitLogic;namespace KingmakerMountedCombat.Diagnostics {'+$raw.Substring($start,$end-$start)+'}'
$pinPath=Join-Path $OutputDirectory 'ActualBuildPins.cs';[IO.File]::WriteAllText($pinPath,$fragment,[Text.UTF8Encoding]::new($false))
$build=[IO.File]::ReadAllText($BuildSource);$matches=[regex]::Matches($build,'\["saveBeforeFieldSelection"\]\s*=\s*([^,\r\n]+)')
if($matches.Count-ne1){throw 'Actual emitted native write field is ambiguous.'};$expression=$matches[0].Groups[1].Value.Trim()
$template=[IO.File]::ReadAllText($TestTemplate);if(($template.Split(@('__ACTUAL_SAVE_ORDER_EXPRESSION__'),[StringSplitOptions]::None)).Length-ne2){throw 'Actual save order marker differs.'}
$testPath=Join-Path $OutputDirectory 'ActualPaidBuildContractTests.cs';[IO.File]::WriteAllText($testPath,$template.Replace('__ACTUAL_SAVE_ORDER_EXPRESSION__',$expression),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$dll=Join-Path $OutputDirectory 'paid-build-tests.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp';$sources=@($pinPath,$testPath,(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedSelection.cs'),(Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs'))
$compilerArgs=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/nostdlib+',('/out:'+$dll))
$compilerArgs+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$compilerArgs+=@('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll')|ForEach-Object{'/reference:'+(Join-Path $managed $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArgs+$sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false));$timer=[Diagnostics.Stopwatch]::StartNew()
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual paid build contract compile failed.'}
foreach($leaf in @('Assembly-CSharp-firstpass.dll','Assembly-CSharp.dll')){[void][Reflection.Assembly]::LoadFrom((Join-Path $managed $leaf))};[void][Reflection.Assembly]::LoadFrom($dll)
$originalWriter=[Console]::Out;$captured=[IO.StringWriter]::new()
try{[Console]::SetOut($captured);$fails=[KingmakerMountedCombat.Tests.PaidBuildContractTests]::Run()}finally{[Console]::SetOut($originalWriter)}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'test.log'),$captured.ToString(),[Text.UTF8Encoding]::new($false));Write-Host $captured.ToString();$timer.Stop()
@{schemaVersion=1;fail=$fails;seconds=$timer.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $BuildSource).Hash.ToLowerInvariant();driverSha256=(Get-FileHash -LiteralPath $driver).Hash.ToLowerInvariant();expression=$expression;nativeExecution=$false;archiveAccess=$false;substitutes='none for emitted expression/build pin class/enums; engine callbacks not invoked'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($fails-ne0){throw 'Actual paid build contract checks failed.'}
