param([string]$SourceDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if([string]::IsNullOrWhiteSpace($SourceDirectory)){$SourceDirectory=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'}
$body=@'
using System;
class HarmonyBoundary{public bool Fail;public int Calls;public void UnpatchAll(string id){Calls++;if(Fail)throw new Exception("unpatch boundary failed");}}
class IdentityBoundary{public int Calls;public void Dispose(){Calls++;}}
'@
$classes=@()
foreach($name in @('NativeActorAllocationTrace','NativeRoundBoundaryProbe','NativeTurnCompletionProbe','NativeLifeDeliveryProbe')){
 $text=[IO.File]::ReadAllText((Join-Path $SourceDirectory ($name+'.cs')));$start=$text.IndexOf('        public void Dispose()');$open=$text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$text.Length){if($text[$end]-ceq'{'){$depth++}elseif($text[$end]-ceq'}'){$depth--};$end++}
 if($start-lt0-or$depth-ne0){throw 'Exact observer Dispose extraction failed.'}
 $method=$text.Substring($start,$end-$start);$classes+=$name
 $body+='class '+$name+' { static '+$name+' active;bool disposed;int tickDepth,setDepth;Action AfterNativeRound;const string Id="probe",HarmonyId="probe";HarmonyBoundary harmony=new HarmonyBoundary();IdentityBoundary identities=new IdentityBoundary();'+[Environment]::NewLine+$method+[Environment]::NewLine
 $body+='public static int Verify(){int passed=0;var p=new '+$name+'();active=p;'
 if($name-ceq'NativeLifeDeliveryProbe'){$body+='p.tickDepth=1;try{p.Dispose();throw new Exception("active callback accepted");}catch(InvalidOperationException){}if(p.harmony.Calls!=0||active!=p)throw new Exception("active callback ownership escaped");p.tickDepth=0;passed++;'}
 $body+='p.harmony.Fail=true;bool refused=false;try{p.Dispose();}catch{refused=true;}if(!refused||active!=p||p.harmony.Calls!=1'+$(if($name-cne'NativeActorAllocationTrace'){'||p.disposed'}else{'||p.identities.Calls!=0'})+')throw new Exception("'+$name+' failed unpatch lost retry ownership");passed++;p.harmony.Fail=false;p.Dispose();if(active!=null||p.harmony.Calls!=2'+$(if($name-cne'NativeActorAllocationTrace'){'||!p.disposed'}else{'||p.identities.Calls!=1'})+')throw new Exception("'+$name+' retry failed");passed++;'
 if($name-cne'NativeActorAllocationTrace'){$body+='p.Dispose();if(p.harmony.Calls!=2)throw new Exception("disposed observer repeated native cleanup");passed++;'}
 $body+='Console.WriteLine("PASS '+$name+' actual Dispose retry ownership="+passed);return passed;}}'+[Environment]::NewLine
}
$body+='static class Program{static int Main(){try{int total=0;'+(($classes|ForEach-Object{'total+='+$_+'.Verify();'})-join'')+'Console.WriteLine("CHUNK7 OBSERVER DISPOSAL PASS="+total+" FAIL=0; actual methods with external unpatch boundary stub");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}'
$probe=Join-Path $chunk7AnalysisRoot ('probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($probe);$path=Join-Path $probe 'ObserverDisposalAdapter.cs';[IO.File]::WriteAllText($path,$body)
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe';$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $probe 'ObserverDisposalAdapter.exe'; & $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169 "/out:$exe" $path
if($LASTEXITCODE-ne0){throw 'Observer actual-method adapter compilation failed.'}; & $exe
if($LASTEXITCODE-ne0){throw 'Observer actual-method retry regression failed.'}
