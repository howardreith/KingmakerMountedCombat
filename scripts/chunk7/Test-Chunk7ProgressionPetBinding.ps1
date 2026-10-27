param([string]$SourceDirectory,[string]$TestSourceDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
$initializer=Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1'
if(Test-Path -LiteralPath $initializer){
 . $initializer
 if(-not $PSBoundParameters.ContainsKey('SourceDirectory')){$SourceDirectory=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'}
 if(-not $PSBoundParameters.ContainsKey('TestSourceDirectory')){$TestSourceDirectory=$chunk7TestSource}
 if(-not $PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('progression-pet-'+[Guid]::NewGuid().ToString('N'))}
}
foreach($path in @($SourceDirectory,$TestSourceDirectory,$OutputDirectory)){if([string]::IsNullOrWhiteSpace($path)){throw 'Exact source, template and private output paths are required.'}}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ExactMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing exact method '+$Signature)}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced method body.'};$Text.Substring($start,$end-$start)
}
$sessionPath=Join-Path $SourceDirectory 'Chunk7NativeProgressionSession.cs';$probePath=Join-Path $SourceDirectory 'NativeProgressionRespecProbe.cs'
$session=[IO.File]::ReadAllText($sessionPath);$probe=[IO.File]::ReadAllText($probePath)
$binding=Get-ExactMethod $session 'private void AssertTargetBinding'
$tick=Get-ExactMethod $session 'internal void Tick()'
$begin=$tick.IndexOf('                Require(started && elapsed.Elapsed.TotalSeconds <= 720,',[StringComparison]::Ordinal)
$end=$tick.IndexOf('                if (stage == 0)',[StringComparison]::Ordinal)
if($begin-lt0-or$end-le$begin){throw 'Missing exact native Tick admission boundary.'}
$admission=$tick.Substring($begin,$end-$begin)
$methods=@('internal void RequireObservedPetBinding','private void RequireOriginalPet','private void RequireSourcedPet','private void PinReboundPet','private void RequireCurrentPetBinding','private bool IsExactNativePetReapplyGap','private void BeginPetUpdate','private void CompletePetUpdate')|ForEach-Object{Get-ExactMethod $probe $_}
$template=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionPetBindingTemplate.cs'))
$resourceStart=$probe.IndexOf('private static JObject Resources(UnitDescriptor unit)',[StringComparison]::Ordinal);$resourceEnd=$probe.IndexOf('};',$resourceStart)+2
if($resourceStart-lt0-or$resourceEnd-le$resourceStart){throw 'Exact native Resources expression missing.'}
$resources=$probe.Substring($resourceStart,$resourceEnd-$resourceStart)
$source=$template.Replace('@TICK@',$admission).Replace('@BINDING@',$binding).Replace('@PROBE@',($methods-join [Environment]::NewLine)).Replace('@RESOURCES@',$resources)
$cs=Join-Path $OutputDirectory 'ActualPetBinding.cs';[IO.File]::WriteAllText($cs,$source,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'check.exe';$log=Join-Path $OutputDirectory 'result.log'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/nostdlib+','/nowarn:0649','/deterministic+',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
$rsp=Join-Path $OutputDirectory 'compile.rsp';[IO.File]::WriteAllLines($rsp,@(($arguments+$cs)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$timer=[Diagnostics.Stopwatch]::StartNew();& $compiler ('@'+$rsp) 2>&1|Tee-Object -FilePath $log;$compile=$LASTEXITCODE;$test=-1
if($compile-eq0){& $exe 2>&1|Tee-Object -FilePath $log -Append;$test=$LASTEXITCODE};$timer.Stop()
$receipt=@{compile=$compile;test=$test;elapsedSeconds=$timer.Elapsed.TotalSeconds;inputs=@($sessionPath,$probePath|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeProof=$false;boundary='Actual session target binding, exact Tick admission slice and eight actual native pet binding methods; native objects/callback dispatch are external stand-ins.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if($compile-ne0-or$test-ne0){throw ('Actual pet binding regression failed: '+$OutputDirectory)}
[ordered]@{status='PASS';elapsedSeconds=$timer.Elapsed.TotalSeconds;output=$OutputDirectory;nativeProof=$false}|ConvertTo-Json
