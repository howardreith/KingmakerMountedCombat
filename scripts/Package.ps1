[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')]
    [string]$ArtifactQualifier
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($SkipBuild) { throw '-SkipBuild is prohibited for qualification packages.' }
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$artifactsRoot=[IO.Path]::GetFullPath((Join-Path $labRoot 'artifacts'))
$stageRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\package\KingmakerMountedCombat'))
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
$version=Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'version.json')|ConvertFrom-Json
$qualifiedName = if ([string]::IsNullOrWhiteSpace($ArtifactQualifier)) {
    "KingmakerMountedCombat-{0}-diagnostic.zip" -f $version.productVersion
}
else {
    "KingmakerMountedCombat-{0}-{1}-diagnostic.zip" -f $version.productVersion,$ArtifactQualifier
}
$packagePath=Join-Path $artifactsRoot $qualifiedName
$manifestPath=$packagePath+'.manifest.json'
if (-not [string]::IsNullOrWhiteSpace($ArtifactQualifier) -and
    ((Test-Path -LiteralPath $packagePath) -or (Test-Path -LiteralPath $manifestPath))) {
    throw "Qualified artifact already exists and is immutable: $packagePath"
}
if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageRoot -Force|Out-Null; New-Item -ItemType Directory -Path $artifactsRoot -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'Info.json') -Destination (Join-Path $stageRoot 'Info.json')
Copy-Item -LiteralPath (Join-Path $repoRoot 'bin\Release\KingmakerMountedCombat.dll') -Destination (Join-Path $stageRoot 'KingmakerMountedCombat.dll')
Compress-Archive -LiteralPath $stageRoot -DestinationPath $packagePath -CompressionLevel Optimal -Force
$validated=& (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $packagePath -PassThru
$looseDll=Join-Path $repoRoot 'bin\Release\KingmakerMountedCombat.dll'
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $looseDll).Hash.ToLowerInvariant() -cne [string]$validated.dllSha256) { throw 'Packaged DLL differs from the clean build output.' }
$manifest=[ordered]@{
    schemaVersion=2; generator='scripts/Package.ps1'; generatedAtUtc=[DateTime]::UtcNow.ToString('o')
    branch=$branchAfter; commit=$headAfter; worktreeClean=$true; qualificationEligible=$true; version=[string]$version.productVersion
    packagePath=[IO.Path]::GetFullPath($packagePath); packageSha256=[string]$validated.packageSha256
    dllSha256=[string]$validated.dllSha256; dllMvid=[string]$validated.dllMvid; entries=@($validated.entries)
}
[IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 10),(New-Object Text.UTF8Encoding($false)))
Write-Host "PASS diagnostic package $packagePath"
Write-Host "MANIFEST=$manifestPath"
