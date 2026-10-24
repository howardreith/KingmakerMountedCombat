$compiler=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$referenceRoot=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$native='C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed'
$references=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{Join-Path $referenceRoot $_}
$references+=@('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','Newtonsoft.Json.dll','UnityModManager/0Harmony12.dll','UnityModManager/0Harmony.dll')|ForEach-Object{Join-Path $native $_}
$sources=@('WorkingLoadObservation','NativeWorkingLoadContract','NativeWorkingLoadRewrite','NativeWorkingLoadObserver')|ForEach-Object{Join-Path $PSScriptRoot ('src/'+$_+'.cs')}
