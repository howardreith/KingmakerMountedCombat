[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$SourcePath){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationScenario.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-readiness-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$text=[IO.File]::ReadAllText($SourcePath)
$start=$text.IndexOf('        internal void Update()',[StringComparison]::Ordinal);$stop=$text.IndexOf('                AssertAuthority(); if (Busy()) return;',$start,[StringComparison]::Ordinal);$catch=$text.IndexOf('            catch (Exception error) { Fail("C7-small-runtime", error.ToString()); BeginCleanup(); }',$stop,[StringComparison]::Ordinal)
if($start-lt0-or$stop-lt0-or$catch-lt0){throw 'Actual Update prefix/catch boundary missing.'}
$prefix=$text.Substring($start,$stop-$start);$catchText=$text.Substring($catch,$text.IndexOf([char]10,$catch)-$catch);if(-not$catchText){throw 'Original catch not captured.'}
$method=$prefix+'            }'+[Environment]::NewLine+$catchText+[Environment]::NewLine+'        }'
$template=@'
using System;
public class ActualReadiness {
 bool completed,disposed,cleanup,captured,makeReady;int starts,cleanupPolls,failures; Clock clock=new Clock();
 class Clock {public TimeSpan Elapsed=TimeSpan.FromSeconds(299);}
 void Start(){starts++;captured=makeReady;}void PollCleanup(){cleanupPolls++;}void Fail(string name,string e){failures++;}void BeginCleanup(){cleanup=true;}static void Require(bool b,string s){if(!b)throw new InvalidOperationException(s);}
 /*METHOD*/
 static int pass,fail;
 static void Check(string n,Func<ActualReadiness,bool> body){var a=new ActualReadiness();if(body(a)){pass++;Console.WriteLine("PASS "+n);}else{fail++;Console.WriteLine("FAIL "+n);}}
 public static int Verify(){
 Check("ready poll before deadline may wait without cleanup",a=>{a.Update();return a.starts==1&&!a.captured&&!a.cleanup&&a.failures==0;});
 Check("exact deadline remains admitted",a=>{a.clock.Elapsed=TimeSpan.FromSeconds(300);a.Update();return a.starts==1&&!a.cleanup;});
 Check("expired uncaptured readiness fails before another native start",a=>{a.clock.Elapsed=TimeSpan.FromSeconds(301);a.Update();return a.cleanup&&a.failures==1&&a.starts==0;});
 Check("captured expired state fails",a=>{a.captured=true;a.clock.Elapsed=TimeSpan.FromSeconds(301);a.Update();return a.cleanup&&a.failures==1&&a.starts==0;});
 Check("cleanup first remains available beyond deadline",a=>{a.cleanup=true;a.clock.Elapsed=TimeSpan.FromSeconds(301);a.Update();return a.cleanupPolls==1&&a.failures==0&&a.starts==0;});
 Check("settled disposed state never reacquires",a=>{a.disposed=true;a.clock.Elapsed=TimeSpan.FromSeconds(301);a.Update();return a.starts==0&&a.failures==0;});
 Check("completed state never reacquires",a=>{a.completed=true;a.Update();return a.starts==0;});
 Check("ready native start still progresses",a=>{a.makeReady=true;a.Update();return a.starts==1&&a.captured&&!a.cleanup;});
 Console.WriteLine("ACTUAL SMALL READINESS PASS "+pass+" FAIL "+fail);return fail;
 }
}
'@
$generated=Join-Path $OutputDirectory 'Actual.cs';[IO.File]::WriteAllText($generated,$template.Replace('/*METHOD*/',$method),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$dll=Join-Path $OutputDirectory 'actual.dll'
&$compiler /nologo /target:library /langversion:7.3 /nowarn:0649 "/out:$dll" $generated;if($LASTEXITCODE-ne0){throw 'Actual readiness extraction compile failed.'}
[void][Reflection.Assembly]::LoadFrom($dll);$failed=[ActualReadiness]::Verify()
@{sourceSha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();extractedSha256=(Get-FileHash -LiteralPath $generated).Hash.ToLowerInvariant();fail=$failed;boundary='Exact Update prefix until first post-start AssertAuthority plus original catch; native readiness/start/cleanup calls substituted';nativeInvoked=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($failed){throw 'Local readiness deadline checks failed.'}
