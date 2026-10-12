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
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw "Missing exact method: $Signature"}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced actual-method extraction.'};$Text.Substring($start,$end-$start)
}
$authorityPath=Join-Path $OverlaySourceDirectory 'PersistenceSaveAuthorization.cs';$sessionPath=Join-Path $privateRoot 'Chunk7NativeProgressionSession.cs';$builderPath=Join-Path $privateRoot 'Chunk7NativeBuilderCancellation.cs';$uiPath=Join-Path $privateRoot 'Chunk7NativeProgressionUi.cs'
$plan=Get-ExactMethod ([IO.File]::ReadAllText($authorityPath)) '        internal bool MatchesProgressionPlan('
$session=[IO.File]::ReadAllText($sessionPath);$drain=Get-ExactMethod $session '        internal bool TryDrain()';$dispose=Get-ExactMethod $session '        public void Dispose()'
$cases=@(@{name='plan';body=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionProgressionPlanTemplate.cs')).Replace('/*ACTUAL_PLAN*/',$plan)},@{name='session';body=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionSessionDrainTemplate.cs')).Replace('/*ACTUAL_SESSION_DRAIN*/',$drain).Replace('/*ACTUAL_SESSION_DISPOSE*/',$dispose)})
$cases+=@(@{name='builder';body=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionBuilderCancellationTemplate.cs')).Replace('/*ACTUAL_BUILDER_DRAIN*/',(Get-ExactMethod ([IO.File]::ReadAllText($builderPath)) '        internal bool TryDrain()'))})
$cases+=@(@{name='ui-proof';body=[IO.File]::ReadAllText((Join-Path $TestSourceDirectory 'ProgressionUiProofRetentionTemplate.cs')).Replace('/*ACTUAL_UI_DISPOSE*/',(Get-ExactMethod ([IO.File]::ReadAllText($uiPath)) '        public void Dispose()'))})
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$receipts=@();$timer=[Diagnostics.Stopwatch]::StartNew()
foreach($case in $cases){
 $cs=Join-Path $OutputDirectory ($case.name+'.cs');$exe=Join-Path $OutputDirectory ($case.name+'.exe');$log=Join-Path $OutputDirectory ($case.name+'.log')
 [IO.File]::WriteAllText($cs,$case.body,[Text.UTF8Encoding]::new($false))
 & $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/out:$exe" $cs 2>&1|Tee-Object -FilePath $log;$compile=$LASTEXITCODE;$test=-1
 if($compile-eq0){& $exe 2>&1|Tee-Object -FilePath $log -Append;$test=$LASTEXITCODE};$receipts+=@{name=$case.name;compile=$compile;test=$test}
}
$timer.Stop()
$result=@{cases=$receipts;elapsedSeconds=$timer.Elapsed.TotalSeconds;inputs=@($authorityPath,$sessionPath,$builderPath,$uiPath|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}});nativeProof=$false;scope='Actual read-only authority predicate, owning session cleanup/disposal, native builder cancellation and released UI proof retention; external native/UI/service boundaries stubbed.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($result|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
if(@($receipts|Where-Object{$_.compile-ne0-or$_.test-ne0}).Count-ne0){throw ('Private progression session regression failed: '+$OutputDirectory)}
[ordered]@{status='PASS';elapsedSeconds=$timer.Elapsed.TotalSeconds;output=$OutputDirectory;nativeProof=$false}|ConvertTo-Json
