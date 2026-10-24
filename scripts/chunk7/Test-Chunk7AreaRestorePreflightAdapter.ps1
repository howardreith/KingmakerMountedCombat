[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $chunk7AnalysisRoot ('area-preflight-' + [Guid]::NewGuid().ToString('N')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $OutputDirectory.StartsWith([IO.Path]::GetFullPath($chunk7AnalysisRoot) + [IO.Path]::DirectorySeparatorChar,
    [StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $OutputDirectory)) {
    throw 'A fresh private Chunk 7 analysis directory is required.'
}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source = Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedAreaPersistence.cs'
$template = Join-Path $repo 'tests/Chunk7Diagnostics/AreaRestorePreflightAdapterTemplate.cs'
$body = [IO.File]::ReadAllText($source)
$signature = 'internal void RestoreAreaPair()'
$start = $body.IndexOf($signature, [StringComparison]::Ordinal)
if ($start -lt 0 -or $start -ne $body.LastIndexOf($signature, [StringComparison]::Ordinal)) { throw 'Actual area restore method is absent or ambiguous.' }
$open = $body.IndexOf('{', $start)
$depth = 1; $end = $open + 1
while ($depth -gt 0 -and $end -lt $body.Length) {
    if ($body[$end] -ceq '{') { $depth++ }
    elseif ($body[$end] -ceq '}') { $depth-- }
    $end++
}
if ($depth -ne 0) { throw 'Actual area restore method has unbalanced braces.' }
$method = $body.Substring($start, $end - $start)
$actual = Join-Path $OutputDirectory 'ActualAreaRestorePreflight.cs'
[IO.File]::WriteAllText($actual, [IO.File]::ReadAllText($template).Replace('__METHOD__', $method), [Text.UTF8Encoding]::new($false))
$compiler = 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$domain = Join-Path $repo 'src/KingmakerMountedCombat/Domain'
$exe = Join-Path $OutputDirectory 'AreaRestorePreflightAdapter.exe'
$sources = @($actual,
    (Join-Path $domain 'MountedPairCandidate.cs'),
    (Join-Path $domain 'MountedRelationshipAdmission.cs'),
    (Join-Path $domain 'MountedRiderProfileCatalog.cs'),
    (Join-Path $domain 'DeclaredSmallRiderBody.cs'),
    (Join-Path $domain 'PoseVector3.cs'),
    (Join-Path $domain 'MountedRiderPoseProfile.cs'))
$compile = @(& $compiler /nologo /target:exe /langversion:7.3 '/nowarn:0649,0414' "/out:$exe" $sources 2>&1)
$compileExit = $LASTEXITCODE
$compile | Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log')
if ($compileExit -ne 0) { $compile | Write-Output; throw 'Actual area restore preflight adapter did not compile.' }
$test = @(& $exe 2>&1)
$testExit = $LASTEXITCODE
$test | Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log')
$test | Write-Output
$passed = @($test | Where-Object { $_ -cmatch '^PASS ' }).Count
$failed = @($test | Where-Object { $_ -cmatch '^FAIL ' }).Count
$ok = $testExit -eq 0 -and $passed -ge 6 -and $failed -eq 0
[ordered]@{ status = $(if ($ok) { 'PASS' } else { 'FAIL' }); passed = $passed; failed = $failed;
    sourceSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant();
    templateSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $template).Hash.ToLowerInvariant();
    actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $actual).Hash.ToLowerInvariant();
    nativeInvoked = $false; limits = 'Actual RestoreAreaPair and real saved-candidate policy; native game actor, profile, UI, ownership, and participation services are typed facades.'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if (-not $ok) { throw 'Actual area restore preflight behavior failed.' }
