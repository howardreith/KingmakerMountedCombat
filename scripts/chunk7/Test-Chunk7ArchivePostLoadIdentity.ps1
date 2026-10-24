[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not $PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-postload-identity-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'PostLoad identity output exists; preserve prior evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
function Source([string]$relative){
    if(-not [string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){return $candidate}}
    return Join-Path $RepositoryRoot $relative
}
function Member([string]$text,[string]$signature){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing or ambiguous exact member: '+$signature)}
    $open=$text.IndexOf('{',$start);$depth=0
    for($i=$open;$i-lt$text.Length;$i++){
        if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}
    }
    throw ('Unclosed exact member: '+$signature)
}
$paths=@((Source 'src/KingmakerMountedCombat/Integration/MountedPersistenceService.World.cs'),(Source 'src/KingmakerMountedCombat/Domain/NativeLoadWorld.cs'),(Source 'tests/Chunk7Diagnostics/ArchivePostLoadIdentityTemplate.cs'))
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$inputs=@(@($paths)+@($PSCommandPath,$compiler)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
$source=[IO.File]::ReadAllText($paths[0]);$template=[IO.File]::ReadAllText($paths[2])
$marker='archivePostLoadObserved =';$field=$source.IndexOf($marker,[StringComparison]::Ordinal)
if($field-lt0-or$source.IndexOf($marker,$field+$marker.Length,[StringComparison]::Ordinal)-ge0){throw 'PostLoad observation declaration is missing or ambiguous.'}
$fieldStart=$source.LastIndexOf('private readonly ',$field,[StringComparison]::Ordinal);$fieldEnd=$source.IndexOf(';',$field)
if($fieldStart-lt0-or$fieldEnd-le$field){throw 'PostLoad observation declaration boundary changed.'}
$methods=@('private void ClearArchiveRestoration()','private bool ResolveArchiveGraph()','private void RestoreArchiveActorAfterPostLoad(UnitEntityData unit)','private void ValidateArchiveActorIdentity()')|ForEach-Object{Member $source $_}
$code=$template.Replace('__POSTLOAD_FIELD__',$source.Substring($fieldStart,$fieldEnd-$fieldStart+1)).Replace('__METHODS__',($methods-join[Environment]::NewLine))
if($code-match'__[A-Z_]+__'){throw 'Unexpanded exact-method template marker.'}
$actual=Join-Path $OutputRoot 'ArchivePostLoadIdentityActual.cs'
$loadWorld=Join-Path $OutputRoot 'NativeLoadWorld.cs'
[IO.File]::WriteAllText($actual,$code,(New-Object Text.UTF8Encoding($false)))
[IO.File]::WriteAllText((Join-Path $OutputRoot 'ArchivePostLoadIdentityTemplate.cs'),$template,(New-Object Text.UTF8Encoding($false)))
[IO.File]::WriteAllBytes($loadWorld,[IO.File]::ReadAllBytes($paths[1]))
$exe=Join-Path $OutputRoot 'ArchivePostLoadIdentityActual.exe'
$clock=[Diagnostics.Stopwatch]::StartNew()
$compileOutput=@(& $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0414 "/out:$exe" $actual $loadWorld 2>&1)
$compileExitCode=$LASTEXITCODE
$compileOutput|ForEach-Object{Write-Output $_}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'compile.log'),[string[]]$compileOutput)
$exitCode=1;$passed=0;$failed=1;$runOutput=@()
if($compileExitCode-eq0){
    $runOutput=@(& $exe 2>&1);$exitCode=$LASTEXITCODE
    $runOutput|ForEach-Object{Write-Output $_}
    $summary=@($runOutput|Where-Object{$_-is[string]-and$_-match'^ARCHIVE POSTLOAD IDENTITY PASS=([0-9]+) FAIL=([0-9]+)$'})
    if($summary.Count-eq1-and$summary[0]-match'^ARCHIVE POSTLOAD IDENTITY PASS=([0-9]+) FAIL=([0-9]+)$'){
        $passed=[int]$Matches[1];$failed=[int]$Matches[2]
        if($passed-eq0-or$failed-ne0){$exitCode=1}
    }else{$exitCode=1}
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'run.log'),[string[]]$runOutput)
$clock.Stop()
foreach($inputPin in $inputs){if((Get-FileHash -LiteralPath $inputPin.path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputPin.sha256){throw ('Executed input changed: '+$inputPin.path)}}
[ordered]@{kind='chunk7-archive-postload-identity-actual';status=$(if($compileExitCode-eq0-and$exitCode-eq0-and$failed-eq0){'PASS'}else{'FAIL'});compileExitCode=$compileExitCode;exitCode=$exitCode;passed=$passed;failed=$failed;durationSeconds=$clock.Elapsed.TotalSeconds;durationBasis='Stopwatch around compiler and actual adapter child processes';inputs=$inputs;actual=[ordered]@{path=$actual;sha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()};actualSourceMethods=$true;actualLoadWorldDomain=$true;externalNativeGraphAndRestoreBoundariesStubbed=$true;archiveDecoded=$false;nativeProof=$false}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
exit $exitCode
