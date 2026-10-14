[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-artifact-producer-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing artifact-producer output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$producerInputs=@{};$compositionEntries=@{}
if(-not[string]::IsNullOrWhiteSpace($SourceCompositionPath)){
    foreach($entry in @((Get-Content -LiteralPath $SourceCompositionPath -Raw|ConvertFrom-Json).files)){$compositionEntries[$entry.path]=$entry}
}
function Source([string]$relative){
    $path=$null
    if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){$path=$candidate}}
    if($null-eq$path-and$compositionEntries.ContainsKey($relative)){$entry=$compositionEntries[$relative];$path=$entry.sourcePath;if((Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()-cne$entry.sha256){throw ('Changed composition input '+$relative)}}
    if($null-eq$path){$path=Join-Path $RepositoryRoot $relative}
    $producerInputs[$path]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();return $path
}
function Member([string]$text,[string]$signature){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing/ambiguous exact member '+$signature)}
    $open=$text.IndexOf('{',$start);$depth=0
    for($i=$open;$i-lt$text.Length;$i++){if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}}
    throw ('Unclosed exact member '+$signature)
}
$hostText=[IO.File]::ReadAllText((Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'))
$loaderText=[IO.File]::ReadAllText((Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs'))
$template=[IO.File]::ReadAllText((Source 'tests/Chunk7Diagnostics/ArtifactProducerTemplate.cs'))
$publisher=(@('private static string PublishRuntimeArtifactManifest(RuntimeRequest request)','private static void AddRuntimeArtifactIfPresent(','private static void AddRuntimeArtifact(','private static string ComputeSha256(string path)','private static void WriteJsonReplacingAtomic(','private sealed class RuntimeArtifactManifest','private sealed class RuntimeArtifactRecord')|ForEach-Object{Member $hostText $_})-join[Environment]::NewLine
$settings=(Member $hostText 'private static readonly JsonSerializerSettings JsonSettings')+';'
$writer=(Member $loaderText 'internal static void WriteArtifact(')+[Environment]::NewLine+(Member $loaderText 'private static string Hash(string path)')
$classNames=@('Chunk7AssistedFixtureScenario','WorkingLoadDiagnosticSession','Chunk7AcquiredManualScenario','Chunk7ManualDerivedScenario','Chunk7ManualSeedScenario','Chunk7NativeBootstrapScenario','Chunk7BootstrapInventoryScenario','Chunk7SmallCalibrationScenario','Chunk7ProgressionScenario','Chunk7OrdinaryFieldScenario','Chunk7OrdinaryAcquisitionScenario','Chunk7WorldScenarioEngine','Chunk7MatchedPoolCollector','Chunk7PersistenceScenario','Chunk7CombatScenario','BoundaryScenarioEvidenceContract','HorseNativeAssetAuditService','HorseCompanionBlueprintRegistrationAuditService','HorseCompanionUnmountedScenarioEngine','Phase3dHorseScenarioTranche','Chunk6aMammothScenarioEngine')
$classNames+=@('Chunk7AssistedFixtureSelection','Chunk7AcquiredManualSelection','Chunk7ManualDerivedSelection','Chunk7ManualSeedSelection','Chunk7BootstrapInventorySelection','Chunk7SmallCalibrationSelection','Chunk7ProgressionSelection','Chunk7OrdinaryFieldSelection','Chunk7OrdinaryAcquisitionSelection')
$classes=''
foreach($name in $classNames){
    $sourceText=[IO.File]::ReadAllText((Source ('src/KingmakerMountedCombat/Diagnostics/'+$name+'.cs')))
    if($name-ceq'Chunk7ManualDerivedScenario'){$sourceText+=[Environment]::NewLine+[IO.File]::ReadAllText((Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedScenario.Cumulative.cs'))}
    $constants=@([regex]::Matches($sourceText,'(?:internal|public|private) const string \w+\s*=\s*"[^"]*";')|ForEach-Object{$_.Value -replace '^private ','internal '})
    if($constants.Count-eq0){throw ('No literal constants for '+$name)}
    $classes+='internal static class '+$name+' {'+[Environment]::NewLine+($constants-join[Environment]::NewLine)+[Environment]::NewLine+'}'+[Environment]::NewLine
}
$loaderConstants=(@([regex]::Matches($loaderText,'internal const string \w+\s*=\s*"[^"]*";')|ForEach-Object{$_.Value}))-join[Environment]::NewLine
$code=$template.Replace('__CONSTANT_CLASSES__',$classes).Replace('__LOADER_CONSTANTS__',$loaderConstants).Replace('__WRITER_METHODS__',$writer).Replace('__JSON_SETTINGS__',$settings).Replace('__PUBLISHER_METHODS__',$publisher)
$codePath=Join-Path $OutputRoot 'ArtifactProducerActual.cs';[IO.File]::WriteAllText($codePath,$code)
$nativeJson='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$jsonPath=Join-Path $OutputRoot 'Newtonsoft.Json.dll';[IO.File]::Copy($nativeJson,$jsonPath,$false)
$producerInputs[$nativeJson]=(Get-FileHash -LiteralPath $nativeJson -Algorithm SHA256).Hash.ToLowerInvariant()
$unityPath='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/UnityEngine.dll'
$producerInputs[$unityPath]=(Get-FileHash -LiteralPath $unityPath -Algorithm SHA256).Hash.ToLowerInvariant()
$exe=Join-Path $OutputRoot 'ArtifactProducerActual.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
& 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe' /nologo /target:exe /langversion:7.3 /nowarn:0649 "/r:$jsonPath" "/out:$exe" $codePath
if($LASTEXITCODE-ne0){throw 'Actual artifact producer adapter did not compile.'}
$casesRoot=Join-Path $OutputRoot 'generated-json'; & $exe $casesRoot $unityPath
$producerExit=$LASTEXITCODE

# Execute only the unchanged read-only functions needed for these generated JSON files.
# No runtime launcher, ZIP reader, archive helper or native process is invoked.
$commonPath=Source 'scripts/runtime/RuntimeHarness.Common.ps1';$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($commonPath,[ref]$tokens,[ref]$errors);if($errors){throw 'Common parse failed.'}
foreach($name in @('Assert-KmcNotReparsePoint','Assert-KmcChildPath','Assert-KmcNotHardLink','Get-KmcSha256','Read-KmcJson','Assert-KmcExactProperties','Assert-KmcKnownRuntimeArtifactsManifested')){
    $nodes=@($ast.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq$name},$true));if($nodes.Count-ne1){throw ('Missing exact helper '+$name)}
    . ([scriptblock]::Create($nodes[0].Extent.Text))
}
. (Source 'scripts/runtime/RuntimeArtifactManifestEvidence.ps1')
$readerPass=0;$readerFail=0;$readerRows=New-Object 'System.Collections.Generic.List[object]'
function Validate($request){
    $manifestPath=Join-Path $request.evidenceRoot 'runtime-artifacts.json';$manifest=Read-KmcJson $manifestPath
    Assert-KmcReadOnlyArtifactManifest $request (Get-KmcSha256 $manifestPath) -GameResult
    Assert-KmcKnownRuntimeArtifactsManifested $request.evidenceRoot $manifest
}
$caseRequests=Get-Content -LiteralPath (Join-Path $casesRoot 'cases.json') -Raw|ConvertFrom-Json
foreach($request in $caseRequests){
    $errorText=$null;try{Validate $request}catch{$errorText=$_.Exception.Message}
    if($null-eq$errorText){$readerPass++;Write-Host ('PASS strict actual manifest '+$request.runId)}else{$readerFail++;Write-Host ('FAIL strict actual manifest '+$request.runId+': '+$errorText)}
    $readerRows.Add([ordered]@{name=$request.runId;expected='accept structurally manifested generated evidence';error=$errorText;pass=($null-eq$errorText)})
}
$negative=@{
    'missing-known-bootstrap'={param($m)$m.artifacts=@($m.artifacts|Where-Object{$_.relativePath-cne'chunk7-native-bootstrap-evidence.json'})}
    'wrong-kind'={param($m)$m.artifacts[0].kind='arbitrary-artifact'}
    'wrong-hash'={param($m)$m.artifacts[0].sha256=('0'*64)}
    'wrong-length'={param($m)$m.artifacts[0].length=1}
    'duplicate-record'={param($m)$m.artifacts=@($m.artifacts)+@($m.artifacts[0])}
    'unknown-leaf'={param($m)$m.artifacts[0].relativePath='bootstrap-any.json'}
}
foreach($name in @($negative.Keys|Sort-Object)){
    $dir=Join-Path $casesRoot ('negative-'+$name);[void][IO.Directory]::CreateDirectory($dir)
    foreach($path in [IO.Directory]::GetFiles((Join-Path $casesRoot 'bootstrap-failure'))){[IO.File]::Copy($path,(Join-Path $dir ([IO.Path]::GetFileName($path))),$false)}
    $manifestPath=Join-Path $dir 'runtime-artifacts.json';$manifest=Read-KmcJson $manifestPath;&$negative[$name] $manifest
    [IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))
    $request=[pscustomobject]@{evidenceRoot=$dir;runId='bootstrap-failure';scenario='chunk7-native-main-bootstrap'}
    $errorText=$null;try{Validate $request}catch{$errorText=$_.Exception.Message}
    $expectedError=switch($name){
        'missing-known-bootstrap' {'Known runtime artifact exists without a manifest record: chunk7-native-bootstrap-evidence.json'}
        'duplicate-record' {'Runtime artifact manifest contains duplicate path: '+$manifest.artifacts[0].relativePath}
        'wrong-kind' {'Runtime artifact manifest record is outside the exact allowlist: '+$manifest.artifacts[0].relativePath+' ('+$manifest.artifacts[0].kind+')'}
        'unknown-leaf' {'Runtime artifact manifest record is outside the exact allowlist: '+$manifest.artifacts[0].relativePath+' ('+$manifest.artifacts[0].kind+')'}
        default {'Runtime artifact bytes do not match the manifest: '+$manifest.artifacts[0].relativePath}
    }
    $intendedGuard=$errorText-ceq$expectedError
    if($intendedGuard){$readerPass++;Write-Host ('PASS strict reader rejects '+$name)}else{$readerFail++;Write-Host ('FAIL strict reader wrong refusal '+$name+': '+$errorText)}
    $readerRows.Add([ordered]@{name=$name;expected=$expectedError;error=$errorText;pass=$intendedGuard})
}
$summary=Get-Content -LiteralPath (Join-Path $casesRoot 'producer-summary.json') -Raw|ConvertFrom-Json
$inputs=@($producerInputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$producerInputs[$_]}})
[ordered]@{status=$(if($producerExit-eq0-and$readerFail-eq0){'PASS'}else{'FAIL'});producerPass=$summary.pass;producerFail=$summary.fail;readerPass=$readerPass;readerFail=$readerFail;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputs;readerRows=@($readerRows.ToArray());actualPublisherAndWriter=$true;nativeLoaderBoundaryStubbed=$true;nativeProof=$false;archiveOperations=0}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Host ('STRICT MANIFEST GUARDS PASS='+$readerPass+' FAIL='+$readerFail)
if($producerExit-ne0-or$readerFail-ne0){exit 1};exit 0
