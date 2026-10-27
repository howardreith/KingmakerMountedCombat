param([string]$SourceDirectory,[string]$OverlaySourceDirectory,[string]$TestSourceDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$initializer=Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1'
if(Test-Path -LiteralPath $initializer){
 . $initializer
 if(-not $PSBoundParameters.ContainsKey('SourceDirectory')){$SourceDirectory=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'}
 if(-not $PSBoundParameters.ContainsKey('TestSourceDirectory')){$TestSourceDirectory=$chunk7TestSource}
 if(-not $PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('progression-private-'+[Guid]::NewGuid().ToString('N'))}
}
if(-not $PSBoundParameters.ContainsKey('OverlaySourceDirectory')){$OverlaySourceDirectory=$SourceDirectory}
foreach($path in @($SourceDirectory,$OverlaySourceDirectory,$TestSourceDirectory,$OutputDirectory)){if([string]::IsNullOrWhiteSpace($path)){throw 'Exact progression source, template and private output paths are required.'}}
$privateRoot=$SourceDirectory
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ExactMethod([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw "Missing exact method $Signature"}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced method body.'};$Text.Substring($start,$end-$start)
}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$uiSource=Join-Path $privateRoot 'Chunk7NativeProgressionUi.cs';$driverSource=Join-Path $OverlaySourceDirectory 'Chunk7OrdinaryBuildDriver.cs';$pairSource=Join-Path $privateRoot 'Chunk7ProgressionPairCustody.cs'
$template=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionUiCustodyTemplate.cs'))
$ui=Get-ExactMethod ([IO.File]::ReadAllText($uiSource)) '        internal bool DrainUi()'
$adopt=Get-ExactMethod ([IO.File]::ReadAllText($driverSource)) '        private void TryAdoptNativeRespecRebind()'
$generated=Join-Path $OutputDirectory 'ActualUiAndRebind.cs'
[IO.File]::WriteAllText($generated,$template.Replace('/*ACTUAL_UI_DRAIN*/',$ui).Replace('/*ACTUAL_RESPEC_ADOPT*/',$adopt),[Text.UTF8Encoding]::new($false))
$cases=@(@{name='pair';sources=@($pairSource,(Join-Path $TestSourceDirectory 'ProgressionPairCustodyBoundary.cs'));json=$true},@{name='ui-rebind';sources=@($generated);json=$false})
$receipts=@();$timer=[Diagnostics.Stopwatch]::StartNew()
foreach($case in $cases){
 $exe=Join-Path $OutputDirectory ($case.name+'.exe');$log=Join-Path $OutputDirectory ($case.name+'.log')
 $arguments=@('/nologo','/target:exe','/langversion:7.3','/deterministic+','/optimize+','/nowarn:0649,0169,0414','/nostdlib+',('/out:'+$exe))
 $arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
 if($case.json){$arguments+='/reference:'+$json;[IO.File]::Copy($json,(Join-Path $OutputDirectory 'Newtonsoft.Json.dll'),$true)}
 $rsp=Join-Path $OutputDirectory ($case.name+'.rsp');[IO.File]::WriteAllLines($rsp,@(($arguments+$case.sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
 & $compiler ('@'+$rsp) 2>&1|Tee-Object -FilePath $log;$compile=$LASTEXITCODE;$test=-1
 if($compile-eq0){& $exe 2>&1|Tee-Object -FilePath $log -Append;$test=$LASTEXITCODE}
 $receipts+=@{name=$case.name;compile=$compile;test=$test;sources=@($case.sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})}
}
$timer.Stop();$receipt=@{elapsedSeconds=$timer.Elapsed.TotalSeconds;cases=$receipts;actualInputs=@($uiSource,$driverSource,$pairSource|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}});nativeProof=$false}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if(@($receipts|Where-Object{$_.compile-ne0-or$_.test-ne0}).Count-ne0){throw ('Private progression custody tests failed: '+$OutputDirectory)}
[ordered]@{status='PASS';elapsedSeconds=$timer.Elapsed.TotalSeconds;output=$OutputDirectory;nativeProof=$false}|ConvertTo-Json
