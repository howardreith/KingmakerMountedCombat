[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')]
    [string]$ArtifactQualifier
)

# Packages the removal observer (a separate minimal UMM mod) from the same
# clean commit as the candidate. The launcher binds the two manifests by commit
# before the genuine no-DLL cleanup-save observation.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($SkipBuild) { throw '-SkipBuild is prohibited for qualification packages.' }
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$artifactsRoot=[IO.Path]::GetFullPath((Join-Path $labRoot 'artifacts'))
$stageRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\package\KmcRemovalObserver'))
$safeStageParent=[IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\package')).TrimEnd('\')
if (-not $stageRoot.StartsWith($safeStageParent+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Package staging escaped repository obj/package.' }
if (-not $artifactsRoot.StartsWith($labRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Artifacts path escaped KMC lab root.' }
$headBefore=(& git -C $repoRoot rev-parse HEAD).Trim(); $branchBefore=(& git -C $repoRoot branch --show-current).Trim()
$statusBefore=@(& git -C $repoRoot status --porcelain --untracked-files=all)
if ($statusBefore.Count -ne 0) { throw 'Qualification packaging requires a clean worktree before build.' }
& (Join-Path $PSScriptRoot 'Build-Local.ps1') -Configuration Release
$headAfter=(& git -C $repoRoot rev-parse HEAD).Trim(); $branchAfter=(& git -C $repoRoot branch --show-current).Trim()
$statusAfter=@(& git -C $repoRoot status --porcelain --untracked-files=all)
if ($headAfter -cne $headBefore -or $branchAfter -cne $branchBefore -or $statusAfter.Count -ne 0) { throw 'Source identity changed during qualification build.' }
$identityText=Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'tools\KmcRemovalObserver\ObserverIdentity.cs')
$identityMatch=[Regex]::Match($identityText,'internal const string ProductVersion = "([^"]+)";')
if (-not $identityMatch.Success) { throw 'Observer identity source lacks its product version.' }
$observerVersion=$identityMatch.Groups[1].Value
$qualifiedName = if ([string]::IsNullOrWhiteSpace($ArtifactQualifier)) {
    "KmcRemovalObserver-{0}-diagnostic.zip" -f $observerVersion
}
else {
    "KmcRemovalObserver-{0}-{1}-diagnostic.zip" -f $observerVersion,$ArtifactQualifier
}
$packagePath=Join-Path $artifactsRoot $qualifiedName
$manifestPath=$packagePath+'.manifest.json'
if (-not [string]::IsNullOrWhiteSpace($ArtifactQualifier) -and
    ((Test-Path -LiteralPath $packagePath) -or (Test-Path -LiteralPath $manifestPath))) {
    throw "Qualified artifact already exists and is immutable: $packagePath"
}
if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageRoot -Force|Out-Null; New-Item -ItemType Directory -Path $artifactsRoot -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools\KmcRemovalObserver\Info.json') -Destination (Join-Path $stageRoot 'Info.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'bin\Observer\Release\KmcRemovalObserver.dll') -Destination (Join-Path $stageRoot 'KmcRemovalObserver.dll')
Compress-Archive -LiteralPath $stageRoot -DestinationPath $packagePath -CompressionLevel Optimal -Force
$validated=& (Join-Path $PSScriptRoot 'Validate-ObserverPackage.ps1') -PackagePath $packagePath -PassThru
$looseDll=Join-Path $repoRoot 'bin\Observer\Release\KmcRemovalObserver.dll'
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $looseDll).Hash.ToLowerInvariant() -cne [string]$validated.dllSha256) { throw 'Packaged observer DLL differs from the clean build output.' }
$manifest=[ordered]@{
    schemaVersion=2; generator='scripts/Package-Observer.ps1'; generatedAtUtc=[DateTime]::UtcNow.ToString('o')
    branch=$branchAfter; commit=$headAfter; worktreeClean=$true; qualificationEligible=$true; version=$observerVersion
    packagePath=[IO.Path]::GetFullPath($packagePath); packageSha256=[string]$validated.packageSha256
    dllSha256=[string]$validated.dllSha256; dllMvid=[string]$validated.dllMvid; entries=@($validated.entries)
}
[IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))
Write-Host "PASS observer package $packagePath"
Write-Host "MANIFEST=$manifestPath"
