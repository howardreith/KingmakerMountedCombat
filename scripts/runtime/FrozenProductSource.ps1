# An explicit archived product source may be qualified by a newer, clean harness.
# Package and suite identities stay those of the original immutable freeze.
# This is not purity reuse: a changed launcher/harness needs its own WhatIf proof.
function Get-KmcFrozenProductSource {
    param(
        [Parameter(Mandatory=$true)][string]$RepositoryRoot,
        [Parameter(Mandatory=$true)][string]$SourceRoot,
        [Parameter(Mandatory=$true)][string]$FreezePath,
        [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedFreezeSha256
    )
    $repo=[IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $source=[IO.Path]::GetFullPath($SourceRoot).TrimEnd('\')
    $lab=[IO.Path]::GetFullPath((Join-Path $repo '..\..')).TrimEnd('\')
    $archiveRoot=Join-Path $lab 'worktrees'
    if(-not $source.StartsWith($archiveRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Frozen source must be a separate worktree in this lab.' }
    $freezeFile=[IO.Path]::GetFullPath($FreezePath)
    if(-not $freezeFile.StartsWith((Join-Path $lab 'analysis-cache')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Frozen source record is outside this lab.' }
    Assert-KmcNotReparsePoint $source 'archived product worktree'
    Assert-KmcRecoveryLeafNoLinks $freezeFile 'original product freeze'
    if((Get-FileHash -LiteralPath $freezeFile -Algorithm SHA256).Hash.ToLowerInvariant() -cne $ExpectedFreezeSha256) { throw 'Original frozen source record changed.' }
    $freeze=Get-Content -LiteralPath $freezeFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if([string]$freeze.source -cnotmatch '^[0-9a-f]{40}$' -or [string]$freeze.tree -cnotmatch '^[0-9a-f]{40}$' -or
       [string]$freeze.branch -cnotmatch '^codex/[A-Za-z0-9._/-]+$' -or $freeze.clean -ne $true -or
       [string]$freeze.remote -cne [string]$freeze.source) { throw 'Original freeze has no exact published clean source.' }
    foreach($root in @($repo,$source)) {
        $actual=@(& git -C $root rev-parse --show-toplevel)
        if($LASTEXITCODE -ne 0 -or $actual.Count -ne 1 -or [IO.Path]::GetFullPath($actual[0]).TrimEnd('\') -ine $root) { throw 'Frozen execution root is not an exact Git worktree.' }
        $dirty=@(& git -C $root status --porcelain --untracked-files=all)
        if($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Frozen execution requires both worktrees clean.' }
    }
    $sourceHead=(& git -C $source rev-parse HEAD).Trim()
    $sourceTree=(& git -C $source rev-parse 'HEAD^{tree}').Trim()
    if($sourceHead -cne [string]$freeze.source -or $sourceTree -cne [string]$freeze.tree) { throw 'Archived worktree differs from the exact frozen source.' }
    $harnessHead=(& git -C $repo rev-parse HEAD).Trim()
    & git -C $repo merge-base --is-ancestor $sourceHead $harnessHead
    if($LASTEXITCODE -ne 0) { throw 'Execution harness is not a descendant of the product source.' }
    # All native production and compilation inputs must remain identical. Harness
    # changes are separately identified and separately prove their live safety.
    $changed=@(& git -C $repo diff --name-only $sourceHead $harnessHead -- src Info.json version.json KingmakerMountedCombat.sln Directory.Build.props Directory.Build.targets)
    if($LASTEXITCODE -ne 0 -or $changed.Count -ne 0) { throw 'Changed production inputs require a new product candidate.' }
    foreach($binding in @($freeze.package,$freeze.manifest,$freeze.suite)) {
        if((Get-FileHash -LiteralPath $binding.path -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$binding.sha256) { throw 'Frozen package, manifest or suite bytes changed.' }
    }
    $manifest=Get-Content -LiteralPath $freeze.manifest.path -Raw -Encoding UTF8 | ConvertFrom-Json
    $suite=Get-Content -LiteralPath $freeze.suite.path -Raw -Encoding UTF8 | ConvertFrom-Json
    if([string]$manifest.commit -cne $sourceHead -or [string]$manifest.branch -cne [string]$freeze.branch -or
       [string]$manifest.packageSha256 -cne [string]$freeze.package.sha256 -or
       [string]$manifest.dllSha256 -cne [string]$freeze.product.dllSha256 -or [string]$manifest.dllMvid -cne [string]$freeze.product.dllMvid -or
       [string]$suite.repository.root -cne $repo -or [string]$suite.repository.commit -cne $sourceHead -or
       [string]$suite.repository.branch -cne [string]$freeze.branch -or [string]$suite.package.sha256 -cne [string]$freeze.package.sha256) { throw 'Frozen source, package and original suite are not the same candidate.' }
    [pscustomobject]@{sourceRoot=$source;source=$sourceHead;tree=$sourceTree;branch=[string]$freeze.branch;harnessHead=$harnessHead;freezePath=$freezeFile;freezeSha256=$ExpectedFreezeSha256;freeze=$freeze}
}

function Assert-KmcFrozenProductManifest {
    param($Context,$Manifest,[string]$PackagePath,[string]$ManifestPath,[string]$Generator,[string]$RepositoryRoot)
    # Revalidate at the package boundary; admission cannot outlive changed inputs.
    $verified=Get-KmcFrozenProductSource -RepositoryRoot $RepositoryRoot -SourceRoot $Context.sourceRoot -FreezePath $Context.freezePath -ExpectedFreezeSha256 $Context.freezeSha256
    if($verified.harnessHead -cne $Context.harnessHead) { throw 'Execution harness changed after frozen-source admission.' }
    $f=$verified.freeze
    if($Generator -ceq 'scripts/Package-Observer.ps1') { $packageBinding=$f.observer; $manifestBinding=$f.observerManifest }
    else { $packageBinding=$f.package; $manifestBinding=$f.manifest }
    if([IO.Path]::GetFullPath($PackagePath) -cne [string]$packageBinding.path -or
       [IO.Path]::GetFullPath($ManifestPath) -cne [string]$manifestBinding.path -or
       (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$packageBinding.sha256 -or
       (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$manifestBinding.sha256 -or
       [string]$Manifest.commit -cne $verified.source -or [string]$Manifest.branch -cne $verified.branch) { throw 'Runtime package is not the exact archived candidate.' }
    $verified
}
