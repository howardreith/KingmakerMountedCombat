[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-host-terminal-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing host terminal boundary output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{};$compositionEntries=@{}
function Pin([string]$path){$inputs[$path]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();return $path}
[void](Pin $PSCommandPath)
if(-not[string]::IsNullOrWhiteSpace($SourceCompositionPath)){
    [void](Pin $SourceCompositionPath)
    foreach($entry in @((Get-Content -LiteralPath $SourceCompositionPath -Raw|ConvertFrom-Json).files)){
        if($compositionEntries.ContainsKey($entry.path)){throw ('Duplicate composition source '+$entry.path)}
        $compositionEntries[$entry.path]=$entry
    }
}
function Source([string]$relative){
    $path=$null
    if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){$path=$candidate}}
    if($null-eq$path-and$compositionEntries.ContainsKey($relative)){$entry=$compositionEntries[$relative];$path=$entry.sourcePath;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$entry.sha256){throw ('Changed composition input '+$relative)}}
    if($null-eq$path){$path=Join-Path $RepositoryRoot $relative}
    return (Pin $path)
}
function Member([string]$text,[string]$signature){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing/ambiguous exact host member '+$signature)}
    $open=$text.IndexOf('{',$start);$depth=0
    for($i=$open;$i-lt$text.Length;$i++){if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}}
    throw ('Unclosed exact host member '+$signature)
}
$hostPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/HostCompletionBoundaryTemplate.cs'
$settingsPath=Source 'src/KingmakerMountedCombat/Diagnostics/DiagnosticSettings.cs'
$gatePath=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7HostCompletionGate.cs'
$hostText=[IO.File]::ReadAllText($hostPath);$template=[IO.File]::ReadAllText($templatePath)
$signatures=@('private void ActivateSaveBackedBoundary(','private void BeginChunk7Completion(','private void CompleteSaveBackedWhenIdle(','private void CompleteSaveBackedCore(','private static IReadOnlyList<RuntimeSubscenarioResult> ReplaceFirstWithFailure(')
$methods=(@($signatures|ForEach-Object{Member $hostText $_}))-join[Environment]::NewLine
$dtoSignature='private sealed class RuntimeGameResultV2'
$actual=$template.Replace('__METHODS__',$methods).Replace('__RESULT_DTO__',(Member $hostText $dtoSignature))
if($actual.Contains('__METHODS__')-or$actual.Contains('__RESULT_DTO__')){throw 'Unresolved host terminal template.'}
$actualPath=Join-Path $OutputRoot 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual)
[IO.File]::Copy($hostPath,(Join-Path $OutputRoot 'source.cs'),$false)
[IO.File]::Copy($templatePath,(Join-Path $OutputRoot 'template.cs'),$false)
$settingsCopy=Join-Path $OutputRoot 'DiagnosticSettings.cs';[IO.File]::Copy($settingsPath,$settingsCopy,$false)
$gateCopy=Join-Path $OutputRoot 'Chunk7HostCompletionGate.cs';[IO.File]::Copy($gatePath,$gateCopy,$false)
$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$referenceRoot='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib','System','System.Core'|ForEach-Object{Pin (Join-Path $referenceRoot ($_+'.dll'))})
$exe=Join-Path $OutputRoot 'ActualHost.exe'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/nowarn:0649','/nostdlib+','/deterministic+',('/out:'+$exe))+@($references|ForEach-Object{'/reference:'+$_})+@($actualPath,$settingsCopy,$gateCopy)
$rsp=Join-Path $OutputRoot 'compile.rsp';[IO.File]::WriteAllText($rsp,(($arguments|ForEach-Object{'"'+$_+'"'})-join[Environment]::NewLine))
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=0
$compileOutput=@(& $compiler ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE;$compileSeconds=$clock.Elapsed.TotalSeconds
[IO.File]::WriteAllText((Join-Path $OutputRoot 'compile.log'),(($compileOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$testExit=$null;$testSeconds=0.0;$passed=0;$failed=0;$shapeValid=$false;$testOutput=@()
if($compileExit-eq0){
    $clock.Restart();$global:LASTEXITCODE=0
    $testOutput=@(& $exe 2>&1);$testExit=$global:LASTEXITCODE;$testSeconds=$clock.Elapsed.TotalSeconds
    $lines=@($testOutput|ForEach-Object{$_.ToString()});$totals=@($lines|Where-Object{$_-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'})
    if($totals.Count-eq1-and$totals[0]-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'){
        $passed=[int]$Matches[1];$failed=[int]$Matches[2]
        $shapeValid=$passed-gt0-and(@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$passed)-and(@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq$failed)
    }
    $lines|ForEach-Object{Write-Host $_}
}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'run.log'),(($testOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$ok=$compileExit-eq0-and$testExit-eq0-and$shapeValid-and$failed-eq0-and$changed.Count-eq0
$inputRows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
$outputRows=@('actual.cs','source.cs','template.cs','DiagnosticSettings.cs','Chunk7HostCompletionGate.cs','compile.rsp','compile.log','run.log'|ForEach-Object{$p=Join-Path $OutputRoot $_;[ordered]@{path=$p;sha256=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{kind='actual-host-terminal-boundary-regression';status=$(if($ok){'PASS'}else{'FAIL'});compileExit=$compileExit;exitCode=$testExit;passed=$passed;failed=$failed;resultShapeValid=$shapeValid;compileDurationSeconds=$compileSeconds;testDurationSeconds=$testSeconds;sourceMethods=$signatures;dto=$dtoSignature;actualGate=$true;actualDiagnosticSettings=$true;inputs=$inputRows;outputs=$outputRows;changedInputs=$changed;substitutions=@('Native Game/SaveManager/mode are inert facades','Family classification is a selected boolean facade','Engine finalization and custody callbacks are external boundaries; actual terminal gate is unchanged','Metadata hashes, result publisher and Application.Quit are inert counters/capture');nativeInvoked=$false;archiveOperations=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
if(-not$ok){exit 1};exit 0
