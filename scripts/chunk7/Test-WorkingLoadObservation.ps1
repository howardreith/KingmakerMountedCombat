[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot,[ValidateSet('All','Connection','Reader','Adapter','Matcher','Patch','Rewrite','Wiring')][string]$Scope='All')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/working-load-observer-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing Working observation test output.'}
$inputPins=[Collections.Generic.List[object]]::new();$compositionEntries=@{}
$inputPins.Add([ordered]@{path=$PSCommandPath;sha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash.ToLowerInvariant()})
if(-not[string]::IsNullOrWhiteSpace($SourceCompositionPath)){foreach($entry in @((Get-Content -LiteralPath $SourceCompositionPath -Raw|ConvertFrom-Json).files)){$compositionEntries[$entry.path]=$entry}}
function Source([string]$relative){
    $source=$null
    if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){$source=$candidate}}
    if($null-eq$source-and$compositionEntries.ContainsKey($relative)){$entry=$compositionEntries[$relative];$source=$entry.sourcePath;if((Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant()-cne$entry.sha256){throw ('Changed composition input '+$relative)}}
    if($null-eq$source){$source=Join-Path $RepositoryRoot $relative}
    $inputPins.Add([ordered]@{path=$source;sha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant()});return $source
}
$stage=Join-Path $OutputRoot 'focused';$tree=Join-Path $stage 'source-tree';[void][IO.Directory]::CreateDirectory($tree)
function Stage([string]$relative,[string]$destination){$source=Source $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination));Copy-Item -LiteralPath $source -Destination $destination}
$newTypes=@('WorkingLoadObservation','NativeWorkingLoadContract','NativeWorkingLoadRewrite','NativeWorkingLoadObserver','WorkingLoadDiagnosticSession','RuntimeAutomationHost.WorkingLoadObservation')
foreach($name in $newTypes){Stage ('src/KingmakerMountedCombat/Diagnostics/'+$name+'.cs') (Join-Path $stage ('src/'+$name+'.cs'))}
$dependencies=@('src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs','src/KingmakerMountedCombat/Diagnostics/WorkingFixtureLoader.cs','src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs','src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs','src/KingmakerMountedCombat/Domain/HorseCompanionScenarioDeadlinePolicy.cs','src/KingmakerMountedCombat/KingmakerMountedCombat.csproj','scripts/runtime/RuntimeHarness.Common.ps1','scripts/runtime/RuntimeArtifactManifestEvidence.ps1','scripts/runtime/WorkingLoadDeserializationEvidence.ps1','scripts/runtime/Test-RuntimeGameResult.ps1','scripts/runtime/Test-RuntimeRequest.ps1','scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1')
foreach($relative in $dependencies){Stage $relative (Join-Path $tree $relative)}
$testFiles=@('NativeCompileInputs.ps1','Test-Connection01.ps1','Test-WorkingReader01.ps1','Test-ReaderBindings01.ps1','Test-ReaderDispatch01.ps1','Test-Wiring01.ps1','Test-AdapterBoundaries01.ps1','Test-InstalledMatcher01.ps1','Test-InstalledPatch01.ps1','Test-GeneratedRewrite01.ps1','WorkingLoadConnectionTests.cs.template','WorkingLoadAdapterBoundaryTests.cs','InstalledWorkingLoadMatcherTests.cs','InstalledWorkingLoadPatchTests.cs','GeneratedWorkingLoadRewriteTests.cs')
foreach($name in $testFiles){Stage ('tests/WorkingLoadDiagnostics/'+$name) (Join-Path $stage $name)}
$executor=Join-Path $PSHOME 'powershell.exe';$inputPins.Add([ordered]@{path=$executor;sha256=(Get-FileHash -LiteralPath $executor).Hash.ToLowerInvariant()})
$stages=[Collections.Generic.List[object]]::new()
function AddStage([string]$name,[string]$script,[string[]]$arguments){$stages.Add([ordered]@{name=$name;script=$script;arguments=$arguments})}
if($Scope-cin@('All','Connection','Reader')){AddStage 'connection' 'Test-Connection01.ps1' @()}
if($Scope-cin@('All','Reader')){
    AddStage 'reader' 'Test-WorkingReader01.ps1' @('-FixtureRoot',(Join-Path $stage 'connection'))
    AddStage 'reader-bindings' 'Test-ReaderBindings01.ps1' @('-ReaderPath',(Join-Path $tree 'scripts/runtime/WorkingLoadDeserializationEvidence.ps1'))
    AddStage 'reader-dispatch' 'Test-ReaderDispatch01.ps1' @('-ReaderRoot',(Join-Path $tree 'scripts/runtime'))
}
if($Scope-cin@('All','Adapter')){AddStage 'adapter' 'Test-AdapterBoundaries01.ps1' @()}
if($Scope-cin@('All','Matcher')){AddStage 'matcher' 'Test-InstalledMatcher01.ps1' @()}
if($Scope-cin@('All','Patch')){AddStage 'patch' 'Test-InstalledPatch01.ps1' @()}
if($Scope-cin@('All','Rewrite')){AddStage 'rewrite' 'Test-GeneratedRewrite01.ps1' @()}
if($Scope-cin@('All','Wiring')){AddStage 'wiring' 'Test-Wiring01.ps1' @('-RepositoryRoot',$tree,'-SourceOverlayRoot',$tree)}
if($stages.Count-eq0){throw 'No Working observation checks selected.'}
$results=[Collections.Generic.List[object]]::new();$failure=$null
try {
    foreach($step in $stages){
        $childOut=Join-Path $stage $step.name
        $argv=@('-NoProfile','-NonInteractive','-File',(Join-Path $stage $step.script))+@($step.arguments)+@('-OutputDirectory',$childOut)
        foreach($a in $argv){if($a.Contains('"')-or$a.Contains("`r")-or$a.Contains("`n")){throw 'Invalid focused child argument.'}}
        $info=[Diagnostics.ProcessStartInfo]::new();$info.FileName=$executor;$info.Arguments=($argv|ForEach-Object{'"'+$_+'"'})-join' ';$info.WorkingDirectory=$stage;$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true;$info.EnvironmentVariables['TEMP']=$stage;$info.EnvironmentVariables['TMP']=$stage
        $child=[Diagnostics.Process]::new();$child.StartInfo=$info;$watch=[Diagnostics.Stopwatch]::StartNew()
        try{if(-not$child.Start()){throw 'Focused child did not start.'};$pidObserved=$child.Id;$started=$child.StartTime.ToUniversalTime().ToString('o');$so=$child.StandardOutput.ReadToEndAsync();$se=$child.StandardError.ReadToEndAsync();$child.WaitForExit();$watch.Stop();$code=$child.ExitCode;$stdout=$so.Result;$stderr=$se.Result}finally{$child.Dispose()}
        $stdoutPath=Join-Path $OutputRoot ($step.name+'.stdout.log');$stderrPath=Join-Path $OutputRoot ($step.name+'.stderr.log');[IO.File]::WriteAllText($stdoutPath,$stdout);[IO.File]::WriteAllText($stderrPath,$stderr)
        $receiptPath=Join-Path $childOut 'receipt.json';$receipt=$null;if(Test-Path -LiteralPath $receiptPath -PathType Leaf){$receipt=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json}
        $ok=$code-eq0-and$null-ne$receipt-and$receipt.status-ceq'PASS'-and$receipt.inputsUnchanged-eq$true-and[int]$receipt.passed-gt0-and[int]$receipt.failed-eq0
        $results.Add([ordered]@{name=$step.name;status=$(if($ok){'PASS'}else{'FAIL'});executable=$executor;arguments=$argv;requestedRawArguments=$info.Arguments;workingDirectory=$stage;processId=$pidObserved;startedAtUtc=$started;exitCode=$code;seconds=$watch.Elapsed.TotalSeconds;stdout=$stdoutPath;stdoutSha256=(Get-FileHash -LiteralPath $stdoutPath).Hash.ToLowerInvariant();stderr=$stderrPath;stderrSha256=(Get-FileHash -LiteralPath $stderrPath).Hash.ToLowerInvariant();receipt=$receiptPath;receiptSha256=$(if($null-ne$receipt){(Get-FileHash -LiteralPath $receiptPath).Hash.ToLowerInvariant()}else{$null})})
        Write-Host $stdout
        if(-not$ok){throw ('Focused Working observation child failed: '+$step.name)}
    }
}catch{$failure=$_.Exception.Message}
$same=$true;foreach($pin in $inputPins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$ok=$null-eq$failure-and$same-and$results.Count-eq$stages.Count
[ordered]@{schemaVersion=1;status=$(if($ok){'PASS'}else{'FAIL'});scope=$Scope;failure=$failure;selectedStageCount=$stages.Count;completedStageCount=$results.Count;inputPins=@($inputPins.ToArray());inputsUnchanged=$same;stages=@($results.ToArray());limits='Finite original observer/host/reader checks with explicit native substitutes. Matcher reads installed metadata; Patch installs and removes wrappers on pinned game methods in disposable processes without invoking native methods. Generated rewrite executes only mapped original test members. No game launch, archive, live guard, candidate or compatibility qualification. Assertions overlap across stages.'}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
if(-not$ok){throw ('Working observation focused checks failed: '+$failure)}
Write-Host ('WORKING LOAD OBSERVATION '+$stages.Count+' stages PASS')
