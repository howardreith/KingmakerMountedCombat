# One synchronous clean-source build for the requested qualification package(s).
# There is no receipt, skip-build mode or loose-DLL entry point.
function Invoke-KmcQualificationPackaging {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('Main', 'Observer', 'Both')]
        [string]$PackageKind,
        [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')]
        [string]$ArtifactQualifier
    )
    $ErrorActionPreference = 'Stop'
    Set-StrictMode -Version Latest
    $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $labRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $labRoot 'artifacts'))
    $safeStageParent = [IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\package')).TrimEnd('\')
    if (-not $artifactsRoot.StartsWith($labRoot+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Artifacts path escaped KMC lab root.' }

    function Read-KmcPackageSourceState {
        $head = (& git -C $repoRoot rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read qualification source HEAD.' }
        $branch = (& git -C $repoRoot branch --show-current).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read qualification source branch.' }
        $status = @(& git -C $repoRoot status --porcelain --untracked-files=all)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect qualification source status.' }
        [pscustomobject]@{ head=$head; branch=$branch; status=$status }
    }
    function Assert-KmcPackageSourceState($Expected) {
        $actual = Read-KmcPackageSourceState
        if ($actual.head -cne $Expected.head -or $actual.branch -cne $Expected.branch -or $actual.status.Count -ne 0) {
            throw 'Source identity changed during qualification build or packaging.'
        }
        $actual
    }

    $before = Read-KmcPackageSourceState
    if ($before.status.Count -ne 0) { throw 'Qualification packaging requires a clean worktree before build.' }
    & (Join-Path $PSScriptRoot 'Build-Local.ps1') -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Qualification solution build failed.' }
    $after = Assert-KmcPackageSourceState $before

    $jobs = @()
    if ($PackageKind -eq 'Main' -or $PackageKind -eq 'Both') {
        $version = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'version.json') | ConvertFrom-Json
        $jobs += [pscustomobject]@{
            name='KingmakerMountedCombat'; version=[string]$version.productVersion
            info=(Join-Path $repoRoot 'Info.json')
            dll=(Join-Path $repoRoot 'bin\Release\KingmakerMountedCombat.dll')
            validator='Validate-Package.ps1'; generator='scripts/Package.ps1'; label='diagnostic'
        }
    }
    if ($PackageKind -eq 'Observer' -or $PackageKind -eq 'Both') {
        $identityText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'tools\KmcRemovalObserver\ObserverIdentity.cs')
        $identityMatch = [Regex]::Match($identityText, 'internal const string ProductVersion = "([^"]+)";')
        if (-not $identityMatch.Success) { throw 'Observer identity source lacks its product version.' }
        $jobs += [pscustomobject]@{
            name='KmcRemovalObserver'; version=$identityMatch.Groups[1].Value
            info=(Join-Path $repoRoot 'tools\KmcRemovalObserver\Info.json')
            dll=(Join-Path $repoRoot 'bin\Observer\Release\KmcRemovalObserver.dll')
            validator='Validate-ObserverPackage.ps1'; generator='scripts/Package-Observer.ps1'; label='observer'
        }
    }

    # Preflight every fixed output before writing either package. The DLL hashes
    # belong to this completed build invocation; callers cannot supply them.
    foreach ($job in $jobs) {
        $qualifiedName = if ([string]::IsNullOrWhiteSpace($ArtifactQualifier)) {
            '{0}-{1}-diagnostic.zip' -f $job.name, $job.version
        } else {
            '{0}-{1}-{2}-diagnostic.zip' -f $job.name, $job.version, $ArtifactQualifier
        }
        $packagePath = Join-Path $artifactsRoot $qualifiedName
        $manifestPath = $packagePath+'.manifest.json'
        $stageRoot = [IO.Path]::GetFullPath((Join-Path $safeStageParent $job.name))
        if (-not $stageRoot.StartsWith($safeStageParent+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Package staging escaped repository obj/package.' }
        if (-not [string]::IsNullOrWhiteSpace($ArtifactQualifier) -and
            ((Test-Path -LiteralPath $packagePath) -or (Test-Path -LiteralPath $manifestPath))) {
            throw "Qualified artifact already exists and is immutable: $packagePath"
        }
        if (-not (Test-Path -LiteralPath $job.dll -PathType Leaf)) { throw "Clean solution build did not produce $($job.name).dll." }
        $job | Add-Member -NotePropertyName packagePath -NotePropertyValue $packagePath
        $job | Add-Member -NotePropertyName manifestPath -NotePropertyValue $manifestPath
        $job | Add-Member -NotePropertyName stageRoot -NotePropertyValue $stageRoot
        $job | Add-Member -NotePropertyName buildDllSha256 -NotePropertyValue ((Get-FileHash -Algorithm SHA256 -LiteralPath $job.dll).Hash.ToLowerInvariant())
    }

    foreach ($job in $jobs) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $job.dll).Hash.ToLowerInvariant() -cne $job.buildDllSha256) { throw 'Clean build output changed before packaging.' }
        if (Test-Path -LiteralPath $job.stageRoot) { Remove-Item -LiteralPath $job.stageRoot -Recurse -Force }
        New-Item -ItemType Directory -Path $job.stageRoot -Force | Out-Null
        New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
        Copy-Item -LiteralPath $job.info -Destination (Join-Path $job.stageRoot 'Info.json')
        Copy-Item -LiteralPath $job.dll -Destination (Join-Path $job.stageRoot ($job.name+'.dll'))
        Compress-Archive -LiteralPath $job.stageRoot -DestinationPath $job.packagePath -CompressionLevel Optimal -Force
        $validated = & (Join-Path $PSScriptRoot $job.validator) -PackagePath $job.packagePath -PassThru
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $job.dll).Hash.ToLowerInvariant() -cne $job.buildDllSha256 -or
            $job.buildDllSha256 -cne [string]$validated.dllSha256) { throw 'Packaged DLL differs from this clean build output.' }
        $job | Add-Member -NotePropertyName validated -NotePropertyValue $validated
    }

    # Withhold both qualification manifests until all requested packages and the
    # source identity pass. Stable generator labels retain their existing contracts.
    $after = Assert-KmcPackageSourceState $before
    foreach ($job in $jobs) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $job.dll).Hash.ToLowerInvariant() -cne $job.buildDllSha256 -or
            (Get-FileHash -Algorithm SHA256 -LiteralPath $job.packagePath).Hash.ToLowerInvariant() -cne [string]$job.validated.packageSha256) {
            throw 'A verified build output or package changed before qualification manifests were written.'
        }
    }
    foreach ($job in $jobs) {
        $manifest = [ordered]@{
            schemaVersion=2; generator=$job.generator; generatedAtUtc=[DateTime]::UtcNow.ToString('o')
            branch=$after.branch; commit=$after.head; worktreeClean=$true; qualificationEligible=$true; version=$job.version
            packagePath=[IO.Path]::GetFullPath($job.packagePath); packageSha256=[string]$job.validated.packageSha256
            dllSha256=[string]$job.validated.dllSha256; dllMvid=[string]$job.validated.dllMvid; entries=@($job.validated.entries)
        }
        [IO.File]::WriteAllText($job.manifestPath, ($manifest | ConvertTo-Json -Depth 10), (New-Object Text.UTF8Encoding($false)))
        Write-Host "PASS $($job.label) package $($job.packagePath)"
        Write-Host "MANIFEST=$($job.manifestPath)"
    }
}
