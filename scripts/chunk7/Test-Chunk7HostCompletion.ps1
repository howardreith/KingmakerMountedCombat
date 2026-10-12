$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe)
$hostSource=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'))
$start=$hostSource.IndexOf('        private void CompleteSaveBackedWhenIdle(')
$end=$hostSource.IndexOf('        private static bool IsLoadingProcessActive()',$start)
if($start-lt0-or$end-le$start){throw 'Host terminal adapter boundary changed.'}
$method=$hostSource.Substring($start,$end-$start)
$adapter=@'
using System;using System.Collections.Generic;using System.IO;using KingmakerMountedCombat.Diagnostics;
static class Application{internal static int Quits;internal static void Quit(){Quits++;}}
sealed class Log{internal void Exception(string text,Exception error){}}
sealed class HostAdapter {
 internal const int CheckCount=5;bool completionStarted,completed;string resultPath="absent";Log logger=new Log();int actorCleanup;bool worker,owner;double now;bool failed;
 bool IsChunk7NativeFamily=>true;readonly Chunk7HostCompletionGate chunk7Completion=new Chunk7HostCompletionGate();
 static bool IsLoadingProcessActive(){return false;}
 void CompleteSaveBackedCore(string status,IReadOnlyList<string> errors){actorCleanup++;if(!worker||!owner)throw new Exception("Premature actor mutation");}
 void TryWriteEmergencySaveBackedFailure(Exception error){}
 void BeginChunk7Completion(string status,IReadOnlyList<string> errors){chunk7Completion.Request(now);}
 void DrainChunk7Completion(){if(chunk7Completion.Poll(now,false,()=>worker,()=>owner,()=>true,()=>true,error=>failed=true))CompleteSaveBackedWhenIdle(failed?"FAIL":"PASS",new string[0]);}
 __METHOD__
 internal void Run(){
  CompleteSaveBackedWhenIdle("FAIL",new[]{"native save worker pending"});Assert(actorCleanup==0&&Application.Quits==0&&!completionStarted&&!completed,"actual terminal adapter holds pending save worker");
  worker=true;now=1;DrainChunk7Completion();Assert(actorCleanup==0&&Application.Quits==0&&!completionStarted,"actual terminal adapter holds pending engine cleanup");
  now=46;DrainChunk7Completion();Assert(failed&&actorCleanup==0&&Application.Quits==0&&!completed,"actual terminal adapter deadline fails without quitting");
  owner=true;now=47;DrainChunk7Completion();Assert(actorCleanup==1&&Application.Quits==1&&completed,"actual terminal adapter quits only after cleanup proof");
  CompleteSaveBackedWhenIdle("FAIL",new string[0]);Assert(actorCleanup==1&&Application.Quits==1,"actual terminal adapter completion is idempotent");
 }
 static void Assert(bool condition,string text){if(!condition)throw new Exception(text);Console.WriteLine("PASS "+text);}
}
'@
$adapter=$adapter.Replace('__METHOD__',$method)
[IO.File]::WriteAllText((Join-Path $probe 'HostAdapter.cs'),$adapter)
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'Chunk7HostCompletionTests.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7HostCompletionGate.cs') (Join-Path $chunk7TestSource 'Chunk7HostCompletionTests.cs') (Join-Path $probe 'HostAdapter.cs')
if($LASTEXITCODE-ne0){throw 'Chunk 7 native custody gate compilation failed.'}
& $exe
if($LASTEXITCODE-ne0){throw 'Chunk 7 native custody gate regression failed.'}
