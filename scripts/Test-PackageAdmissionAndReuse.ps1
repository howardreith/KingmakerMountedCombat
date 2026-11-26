[CmdletBinding()]
param(
    # A released 0.2.15 package whose DLL was compiled against UMM 0.33.0.0 (read only; its DLL is the mismatched fixture).
    [string]$ReleasedPackage = 'C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.2.15-release-diagnostic.zip',
    [string]$ReportPath
)

# Behavior of the shipping package boundary in a disposable lab (a temporary clone of the committed HEAD under
# <temp>\lab\worktrees, with its own artifacts folder): the pin definition, both standalone validators on real packages
# and on variants, runtime admission binding the DLL inside the ZIP to its manifest, and reuse of compiled outputs only
# while the loader inputs are unchanged. Nothing is deployed or launched; this checkout and the owner's lab are untouched.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$commit = (& git -C $source rev-parse HEAD).Trim()
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = [string](@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1)
$cases = [Collections.Generic.List[object]]::new()
function Add-Case([string]$Name, [bool]$Passed, [string]$Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    Write-Host ("{0} {1} :: {2}" -f $(if ($Passed) { 'PASS' } else { 'FAIL' }), $Name, $Detail)
}
function Invoke-Native([string]$File, [string[]]$Arguments) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $output = (($null | & $File @Arguments 2>&1) | ForEach-Object { "$_" }) -join "`n" } finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -replace '\s+', ' ') }
}
function Invoke-PS([string[]]$Arguments) { Invoke-Native 'powershell.exe' (@('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass') + $Arguments) }
function Get-Sha([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Get-Reason([string]$Text) { $m = [regex]::Match($Text, '(?i)(must|not |differ|refus|exactly|already|cannot|unpinned|reference)[^.]{0,160}'); if ($m.Success) { $m.Value } else { $Text.Substring([Math]::Max(0, $Text.Length - 200)) } }
# A copy of a ZIP with entries replaced, removed or added (entries keep their original names and order).
function New-ZipVariant([string]$From, [string]$To, [hashtable]$Replace = @{}, [hashtable]$Add = @{}) {
    $in = [IO.Compression.ZipFile]::OpenRead($From)
    $out = [IO.Compression.ZipFile]::Open($To, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $in.Entries) {
            $target = $out.CreateEntry($entry.FullName)
            $writer = $target.Open()
            try {
                if ($Replace.ContainsKey($entry.FullName)) { $bytes = [byte[]]$Replace[$entry.FullName]; $writer.Write($bytes, 0, $bytes.Length) }
                else { $reader = $entry.Open(); try { $reader.CopyTo($writer) } finally { $reader.Dispose() } }
            } finally { $writer.Dispose() }
        }
        foreach ($name in $Add.Keys) { $target = $out.CreateEntry($name); $writer = $target.Open(); try { $bytes = [byte[]]$Add[$name]; $writer.Write($bytes, 0, $bytes.Length) } finally { $writer.Dispose() } }
    } finally { $out.Dispose(); $in.Dispose() }
}
function Get-ZipEntryBytes([string]$Zip, [string]$Name) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Zip)
    try { $entry = @($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -ceq $Name })[0]; $reader = $entry.Open(); $buffer = [IO.MemoryStream]::new(); try { $reader.CopyTo($buffer); return , $buffer.ToArray() } finally { $reader.Dispose(); $buffer.Dispose() } }
    finally { $archive.Dispose() }
}
function Get-DllEntryName([string]$Zip, [string]$Name) { $a = [IO.Compression.ZipFile]::OpenRead($Zip); try { return @($a.Entries | Where-Object { $_.FullName.Replace('\', '/') -like "*/$Name" })[0].FullName } finally { $a.Dispose() } }
function Get-InfoEntryName([string]$Zip) { $a = [IO.Compression.ZipFile]::OpenRead($Zip); try { return @($a.Entries | Where-Object { $_.FullName.Replace('\', '/') -like '*/Info.json' })[0].FullName } finally { $a.Dispose() } }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('kmc-package-boundary-' + [Guid]::NewGuid().ToString('N'))
$lab = Join-Path $temp 'lab'
$clone = Join-Path $lab 'worktrees\candidate clone'
$artifacts = Join-Path $lab 'artifacts'
try {
    New-Item -ItemType Directory -Path (Split-Path -Parent $clone), $artifacts -Force | Out-Null
    & git clone -q --no-hardlinks $source $clone
    & git -C $clone checkout -q --detach $commit
    if ((& git -C $clone rev-parse HEAD).Trim() -cne $commit) { throw 'The clone did not select the commit under test.' }
    # The clone keeps the branch name so package manifests bind a branch, as in a real checkout.
    & git -C $clone checkout -q -B (& git -C $source branch --show-current).Trim()
    Copy-Item -LiteralPath (Join-Path $source 'LocalGamePaths.props') -Destination $clone
    $verifier = Join-Path $clone 'scripts\Get-PinnedLoaderReferences.ps1'
    $acquire = Invoke-PS @('-File', $verifier)
    if ($acquire.ExitCode -ne 0) { throw "Pinned reference acquisition failed: $($acquire.Output)" }
    $pinsFile = Join-Path $clone 'scripts\loader-reference-pins.json'
    $pinsOriginal = [IO.File]::ReadAllText($pinsFile)

    # 1. The complete pin mapping, checked with an already populated cache.
    Add-Case 'pins-genuine-definition-accepted' ((Invoke-PS @('-File', $verifier, '-VerifyOnly')).ExitCode -eq 0) 'genuine pins and populated cache verified'
    foreach ($bad in @(
        @{ Name = 'pins-omitted-harmony12-rejected'; Edit = { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony12') }; Pattern = 'must define exactly' },
        @{ Name = 'pins-harmony12-naming-another-cache-file-rejected'; Edit = { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].File = '0Harmony.dll' }; Pattern = 'must define 0Harmony12 exactly once as 0Harmony12\.dll' },
        @{ Name = 'pins-digest-with-trailing-line-feed-rejected'; Edit = { param($j) $pin = @($j.references | Where-Object Name -eq 'UnityModManager')[0]; $pin.Sha256 = $pin.Sha256 + "`n" }; Pattern = 'Invalid or duplicate loader reference pin' })) {
        $j = $pinsOriginal | ConvertFrom-Json; & $bad.Edit $j
        [IO.File]::WriteAllText($pinsFile, ($j | ConvertTo-Json -Depth 6))
        $r = Invoke-PS @('-File', $verifier, '-VerifyOnly')
        Add-Case $bad.Name ($r.ExitCode -ne 0 -and $r.Output -match $bad.Pattern) (Get-Reason $r.Output)
        [IO.File]::WriteAllText($pinsFile, $pinsOriginal)
    }

    # 2. Real packages from the clone (both shipping products) from one combined qualification packaging build.
    $pkg = Invoke-PS @('-File', (Join-Path $clone 'scripts\Package.ps1'), '-IncludeRemovalObserver', '-ArtifactQualifier', 'boundary')
    $obsPkg = $pkg
    $mainZip = @(Get-ChildItem -LiteralPath $artifacts -Filter 'KingmakerMountedCombat-*-boundary-diagnostic.zip')[0].FullName
    $observerZip = @(Get-ChildItem -LiteralPath $artifacts -Filter 'KmcRemovalObserver-*-boundary-diagnostic.zip')[0].FullName
    Add-Case 'packages-built-by-combined-qualification-route' ($pkg.ExitCode -eq 0 -and $obsPkg.ExitCode -eq 0) "main $(Get-Sha $mainZip); observer $(Get-Sha $observerZip)"

    # 3. Both standalone validators: genuine packages pass; scalar-string minimum required; a DLL compiled against another
    #    loader and extra loader, Harmony or game payloads are refused.
    $validators = @{ main = (Join-Path $clone 'scripts\Validate-Package.ps1'); observer = (Join-Path $clone 'scripts\Validate-ObserverPackage.ps1') }
    $zips = @{ main = $mainZip; observer = $observerZip }
    foreach ($kind in @('main', 'observer')) {
        $r = Invoke-PS @('-File', $validators[$kind], '-PackagePath', $zips[$kind])
        Add-Case "$kind-validator-genuine-package-accepted" ($r.ExitCode -eq 0) "exit $($r.ExitCode)"
        $infoName = Get-InfoEntryName $zips[$kind]
        $info = [Text.Encoding]::UTF8.GetString((Get-ZipEntryBytes $zips[$kind] $infoName.Replace('\', '/'))).TrimStart([char]0xFEFF) | ConvertFrom-Json
        $info.ManagerVersion = @([string]$info.ManagerVersion)
        $variant = Join-Path $artifacts "$kind-array-manager.zip"
        New-ZipVariant $zips[$kind] $variant @{ $infoName = [Text.Encoding]::UTF8.GetBytes(($info | ConvertTo-Json -Depth 4)) }
        $r = Invoke-PS @('-File', $validators[$kind], '-PackagePath', $variant)
        Add-Case "$kind-validator-one-element-array-minimum-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'Info\.json identity is not exact') (Get-Reason $r.Output)
        $modFolder = $infoName.Substring(0, $infoName.LastIndexOfAny([char[]]@('/', '\')) + 1)
        foreach ($payload in @('UnityModManager.dll', '0Harmony12.dll', 'Assembly-CSharp.dll')) {
            $variant = Join-Path $artifacts "$kind-extra-$payload.zip"
            New-ZipVariant $zips[$kind] $variant @{} @{ ($modFolder + $payload) = [Text.Encoding]::ASCII.GetBytes('payload') }
            $r = Invoke-PS @('-File', $validators[$kind], '-PackagePath', $variant)
            Add-Case "$kind-validator-extra-$payload-rejected" ($r.ExitCode -ne 0) (Get-Reason $r.Output)
        }
    }
    $releasedDll = Get-ZipEntryBytes $ReleasedPackage 'KingmakerMountedCombat/KingmakerMountedCombat.dll'
    $mainDllEntry = Get-DllEntryName $mainZip 'KingmakerMountedCombat.dll'
    $variant = Join-Path $artifacts 'main-released-umm-0.33-dll.zip'
    New-ZipVariant $mainZip $variant @{ $mainDllEntry = $releasedDll }
    $r = Invoke-PS @('-File', $validators.main, '-PackagePath', $variant)
    Add-Case 'main-validator-dll-compiled-against-umm-0.33-rejected' ($r.ExitCode -ne 0 -and $r.Output -match 'UnityModManager') ("released 0.2.15 DLL (UnityModManager 0.33.0.0): " + (Get-Reason $r.Output))

    # 3b. Both standalone validators apply the pin definition before trusting it (offline; no download or cache): missing,
    #     duplicated or mismapped pins and a minimum/UMM-version disagreement are refused; genuine pins are accepted again.
    foreach ($bad in @(
        @{ Name = 'missing-harmony12-pin'; Edit = { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony12') } },
        @{ Name = 'duplicated-umm-pin'; Edit = { param($j) $j.references = @($j.references) + @(@($j.references | Where-Object Name -eq 'UnityModManager')[0]) } },
        @{ Name = 'mismapped-harmony12-pin'; Edit = { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].File = '0Harmony.dll' } },
        @{ Name = 'minimum-umm-version-disagreement'; Edit = { param($j) $j.minimumManagerVersion = '0.32.4' } })) {
        $j = $pinsOriginal | ConvertFrom-Json; & $bad.Edit $j
        [IO.File]::WriteAllText($pinsFile, ($j | ConvertTo-Json -Depth 6))
        foreach ($kind in @('main', 'observer')) {
            $r = Invoke-PS @('-File', $validators[$kind], '-PackagePath', $zips[$kind])
            Add-Case "$kind-validator-$($bad.Name)-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'pin file must define|Invalid or duplicate|must equal the declared minimum') (Get-Reason $r.Output)
        }
        [IO.File]::WriteAllText($pinsFile, $pinsOriginal)
    }
    foreach ($kind in @('main', 'observer')) {
        $r = Invoke-PS @('-File', $validators[$kind], '-PackagePath', $zips[$kind])
        Add-Case "$kind-validator-canonical-pins-accepted-again" ($r.ExitCode -eq 0) "exit $($r.ExitCode)"
    }

    # 4. Runtime admission binds the DLL inside the ZIP to the manifest (both products): matching pairs pass; an outer ZIP
    #    whose identity is correct but whose DLL belongs to another build is refused before staging.
    $harness = Join-Path $clone 'scripts\runtime\RuntimeHarness.Common.ps1'
    $admit = { param($zip, $manifest, $generator) Invoke-PS @('-Command', ". '$($harness.Replace("'", "''"))'; `$m = Assert-KmcPackageManifest -PackagePath '$($zip.Replace("'", "''"))' -ManifestPath '$($manifest.Replace("'", "''"))' -Generator '$generator'; 'ADMITTED ' + `$m.measuredPackage.dllSha256") }
    foreach ($kind in @('main', 'observer')) {
        $generator = if ($kind -eq 'main') { 'scripts/Package.ps1' } else { 'scripts/Package-Observer.ps1' }
        $r = & $admit $zips[$kind] ($zips[$kind] + '.manifest.json') $generator
        Add-Case "$kind-admission-matching-zip-and-manifest-accepted" ($r.ExitCode -eq 0 -and $r.Output -match 'ADMITTED [0-9a-f]{64}') "exit $($r.ExitCode)"
    }
    # Another build of the product: a source change compiled into a separate output, then reverted.
    $probe = @(& git -C $clone ls-files 'src/KingmakerMountedCombat/*.cs')[0]
    [IO.File]::AppendAllText((Join-Path $clone $probe), "`r`nnamespace KmcBoundaryProbe { internal static class Marker { internal const int Value = 7; } }`r`n")
    $otherOut = Join-Path $temp 'other build'
    $ob = Invoke-Native $msbuild @((Join-Path $clone 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'), '/nologo', '/v:m', '/t:Rebuild', '/p:Configuration=Release', "/p:OutDir=$otherOut\")
    & git -C $clone checkout -q -- $probe
    $otherDll = [IO.File]::ReadAllBytes((Join-Path $otherOut 'KingmakerMountedCombat.dll'))
    $swapped = Join-Path $artifacts 'main-other-build-dll.zip'
    New-ZipVariant $mainZip $swapped @{ $mainDllEntry = $otherDll }
    $manifest = Get-Content -LiteralPath ($mainZip + '.manifest.json') -Raw | ConvertFrom-Json
    $manifest.packagePath = $swapped; $manifest.packageSha256 = Get-Sha $swapped
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath ($swapped + '.manifest.json') -Encoding UTF8
    $r = & $admit $swapped ($swapped + '.manifest.json') 'scripts/Package.ps1'
    $swappedResult = $r
    # Common admission with only the receipt version changed.
    $versionOnly = Join-Path $artifacts 'main-version-only.manifest.json'
    $vm = Get-Content -LiteralPath ($mainZip + '.manifest.json') -Raw | ConvertFrom-Json; $vm.version = '9.9.9'
    $vm | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $versionOnly -Encoding UTF8
    $vr = & $admit $mainZip $versionOnly 'scripts/Package.ps1'
    Add-Case 'common-admission-receipt-version-only-changed-rejected' ($vr.ExitCode -ne 0 -and $vr.Output -match 'not the build its manifest names') (Get-Reason $vr.Output)
    # Normal-player observer admission: matching receipt accepted; correct outer hash with stale DLL fields refused.
    $np = Join-Path $clone 'scripts\runtime\Chunk7NormalPlayerEvidence.ps1'
    $npAdmit = { param($zip) Invoke-PS @('-Command', ". '$($harness.Replace("'", "''"))'; . '$($np.Replace("'", "''"))'; `$m = Assert-KmcNormalPlayerObserverManifest '$($zip.Replace("'", "''"))'; 'ADMITTED ' + `$m.measuredPackage.dllSha256") }
    $r = & $npAdmit $observerZip
    Add-Case 'normal-player-observer-matching-receipt-accepted' ($r.ExitCode -eq 0 -and $r.Output -match 'ADMITTED [0-9a-f]{64}') "exit $($r.ExitCode)"
    foreach ($stale in @(@('dll-hash', 'dllSha256', ('0' * 64)), @('mvid', 'dllMvid', ([Guid]::NewGuid().ToString())))) {
        $copy = Join-Path $artifacts ("observer-stale-$($stale[0]).zip")
        Copy-Item -LiteralPath $observerZip -Destination $copy
        $om = Get-Content -LiteralPath ($observerZip + '.manifest.json') -Raw | ConvertFrom-Json
        $om.packagePath = $copy; $om.packageSha256 = Get-Sha $copy; $om.($stale[1]) = $stale[2]
        $om | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath ($copy + '.manifest.json') -Encoding UTF8
        $r = & $npAdmit $copy
        Add-Case "normal-player-observer-stale-$($stale[0])-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'not the build its manifest names') (Get-Reason $r.Output)
    }
    $r = $swappedResult
    Add-Case 'admission-correct-outer-zip-with-another-builds-dll-rejected' ($ob.ExitCode -eq 0 -and $r.ExitCode -ne 0 -and $r.Output -match 'not the build its manifest names') ("other build exit $($ob.ExitCode); " + (Get-Reason $r.Output))

    # 5. Reuse of compiled outputs (-SkipBuild) only while the loader inputs that produced them are unchanged.
    $bl = Invoke-PS @('-File', (Join-Path $clone 'scripts\Build-Local.ps1'), '-SkipSourceValidation')
    $stampCheck = { Invoke-PS @('-Command', ". '$((Join-Path $clone 'scripts\LoaderInputStamp.ps1').Replace("'", "''"))'; Assert-KmcLoaderInputStamp '$($clone.Replace("'", "''"))' 'Release'; 'REUSABLE'") }
    $r = & $stampCheck
    Add-Case 'reuse-of-unchanged-verified-outputs-accepted' ($bl.ExitCode -eq 0 -and $r.ExitCode -eq 0 -and $r.Output -match 'REUSABLE') "Build-Local exit $($bl.ExitCode)"
    $projectFile = Join-Path $clone 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'
    $projectOriginal = [IO.File]::ReadAllText($projectFile)
    foreach ($change in @(
        @{ Name = 'pin'; Apply = { [IO.File]::WriteAllText($pinsFile, $pinsOriginal.Replace('"schemaVersion"', '"schemaVersion" ')) }; Undo = { [IO.File]::WriteAllText($pinsFile, $pinsOriginal) } },
        @{ Name = 'verifier'; Apply = { [IO.File]::AppendAllText($verifier, "`r`n# changed helper`r`n") }; Undo = { & git -C $clone checkout -q -- scripts/Get-PinnedLoaderReferences.ps1 } },
        @{ Name = 'project-reference'; Apply = { [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>')) }; Undo = { [IO.File]::WriteAllText($projectFile, $projectOriginal) } },
        @{ Name = 'build-bootstrap'; Apply = { [IO.File]::AppendAllText((Join-Path $clone 'scripts\Build-Local.ps1'), "`r`n# changed bootstrap option`r`n") }; Undo = { & git -C $clone checkout -q -- scripts/Build-Local.ps1 } },
        @{ Name = 'local-build-settings'; Apply = { $script:localPropsOriginal = [IO.File]::ReadAllText((Join-Path $clone 'LocalGamePaths.props')); [IO.File]::WriteAllText((Join-Path $clone 'LocalGamePaths.props'), $script:localPropsOriginal.Replace('</Project>', '<PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>')) }; Undo = { [IO.File]::WriteAllText((Join-Path $clone 'LocalGamePaths.props'), $script:localPropsOriginal) } },
        @{ Name = 'main-manifest-only'; Apply = { $script:infoOriginal = [IO.File]::ReadAllText((Join-Path $clone 'Info.json')); [IO.File]::WriteAllText((Join-Path $clone 'Info.json'), $script:infoOriginal.Replace('"Id"', ' "Id"')) }; Undo = { [IO.File]::WriteAllText((Join-Path $clone 'Info.json'), $script:infoOriginal) } },
        # The repository-level CoreCompile hook of Test-PinnedLoaderMsBuild.ps1 (it rewrites the compiler's UnityModManager
        # reference), added after the build and never staged in Git.
        @{ Name = 'untracked-tools-directory-build-targets'; Apply = { [IO.File]::WriteAllText((Join-Path $clone 'tools\Directory.Build.targets'), "<Project><Target Name=`"KmcTestLateRewrite`" BeforeTargets=`"CoreCompile`"><ItemGroup><_KmcTestUmm Include=`"@(ReferencePathWithRefAssemblies)`" Condition=`"'%(Filename)' == 'UnityModManager'`" /><ReferencePathWithRefAssemblies Remove=`"@(_KmcTestUmm)`" /><ReferencePathWithRefAssemblies Include=`"$([Security.SecurityElement]::Escape((Join-Path $temp 'stray copy\UnityModManager.dll')))`" /></ItemGroup></Target></Project>"); if (@(& git -C $clone ls-files -- tools/Directory.Build.targets).Count -ne 0) { throw 'The tools hook fixture must stay untracked.' } }; Undo = { Remove-Item -LiteralPath (Join-Path $clone 'tools\Directory.Build.targets') } },
        @{ Name = 'untracked-tools-directory-build-props'; Apply = { [IO.File]::WriteAllText((Join-Path $clone 'tools\Directory.Build.props'), '<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>') }; Undo = { Remove-Item -LiteralPath (Join-Path $clone 'tools\Directory.Build.props') } },
        @{ Name = 'untracked-root-directory-build-targets'; Apply = { [IO.File]::WriteAllText((Join-Path $clone 'Directory.Build.targets'), "<Project><Target Name=`"KmcTestLateRewrite`" BeforeTargets=`"CoreCompile`"><ItemGroup><_KmcTestUmm Include=`"@(ReferencePathWithRefAssemblies)`" Condition=`"'%(Filename)' == 'UnityModManager'`" /><ReferencePathWithRefAssemblies Remove=`"@(_KmcTestUmm)`" /><ReferencePathWithRefAssemblies Include=`"$([Security.SecurityElement]::Escape((Join-Path $temp 'stray copy\UnityModManager.dll')))`" /></ItemGroup></Target></Project>"); if (@(& git -C $clone ls-files -- Directory.Build.targets).Count -ne 0) { throw 'The root hook fixture must stay untracked.' } }; Undo = { Remove-Item -LiteralPath (Join-Path $clone 'Directory.Build.targets') } })) {
        & $change.Apply
        $r = & $stampCheck
        Add-Case "reuse-after-$($change.Name)-change-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'cannot reuse these outputs') (Get-Reason $r.Output)
        & $change.Undo
    }
    # Output bytes, the component runner and compiled sources are part of the receipt.
    $shipping = Join-Path $clone 'bin\Release\KingmakerMountedCombat.dll'
    $runner = Join-Path $clone 'bin\Tests\Release\KingmakerMountedCombat.Tests.exe'
    $shippingBytes = [IO.File]::ReadAllBytes($shipping); $runnerBytes = [IO.File]::ReadAllBytes($runner)
    $probeSource = Join-Path $clone $probe; $probeOriginal = [IO.File]::ReadAllText($probeSource)
    foreach ($change in @(
        @{ Name = 'replaced-shipping-dll'; Apply = { [IO.File]::WriteAllBytes($shipping, [byte[]]($shippingBytes + [byte[]](0))) }; Undo = { [IO.File]::WriteAllBytes($shipping, $shippingBytes) } },
        @{ Name = 'removed-shipping-dll'; Apply = { Remove-Item -LiteralPath $shipping }; Undo = { [IO.File]::WriteAllBytes($shipping, $shippingBytes) } },
        @{ Name = 'replaced-component-runner'; Apply = { [IO.File]::WriteAllBytes($runner, [byte[]]($runnerBytes + [byte[]](0))) }; Undo = { [IO.File]::WriteAllBytes($runner, $runnerBytes) } },
        @{ Name = 'changed-compiled-source'; Apply = { [IO.File]::WriteAllText($probeSource, $probeOriginal + "`r`n// changed`r`n") }; Undo = { [IO.File]::WriteAllText($probeSource, $probeOriginal) } })) {
        & $change.Apply
        $r = & $stampCheck
        Add-Case "reuse-after-$($change.Name)-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'cannot reuse these outputs') (Get-Reason $r.Output)
        & $change.Undo
    }
    $r = & $stampCheck
    Add-Case 'reuse-accepted-again-after-restoring-outputs-and-sources' ($r.ExitCode -eq 0 -and $r.Output -match 'REUSABLE') "exit $($r.ExitCode)"
    # Direct Test.ps1 -SkipBuild refuses stale outputs before any runner starts (the bootstrap edited after the build).
    [IO.File]::AppendAllText((Join-Path $clone 'scripts\Build-Local.ps1'), "`r`n# changed bootstrap option`r`n")
    $direct = Invoke-PS @('-File', (Join-Path $clone 'scripts\Test.ps1'), '-SkipBuild')
    & git -C $clone checkout -q -- scripts/Build-Local.ps1
    Add-Case 'direct-test-skipbuild-with-stale-outputs-refused-before-the-runner' ($direct.ExitCode -ne 0 -and $direct.Output -match 'cannot reuse these outputs' -and $direct.Output -notmatch 'Test runner missing') (Get-Reason $direct.Output)
    # Plan mode reports one verdict on both the reuse-check and the artifact line, for a current, a missing and a stale
    # receipt, and leaves the receipt unchanged.
    $tiersScript = Join-Path $clone 'scripts\Test-Tiers.ps1'
    $stampFile = Join-Path $clone 'bin\Release\.loader-input-stamp'
    $stampBefore = Get-Sha $stampFile
    $planCurrent = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-Plan')
    Move-Item -LiteralPath $stampFile -Destination ($stampFile + '.aside')
    $planMissing = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-Plan')
    Move-Item -LiteralPath ($stampFile + '.aside') -Destination $stampFile
    [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
    $planStale = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-Plan')
    [IO.File]::WriteAllText($projectFile, $projectOriginal)
    $planOk = $planCurrent.Output -match 'reuse check \[receipt current' -and $planCurrent.Output -match 'artifact: .* \(reused: build receipt current\)' -and
        $planMissing.Output -match 'reuse check \[MISSING build receipt' -and $planMissing.Output -match 'artifact: .* \(not reusable: build receipt missing' -and
        $planStale.Output -match 'reuse check \[STALE build receipt' -and $planStale.Output -match 'artifact: .* \(not reusable: build receipt stale' -and (Get-Sha $stampFile) -ceq $stampBefore
    Add-Case 'plan-mode-reports-one-reuse-verdict-for-each-receipt-state' $planOk ('current / missing / stale: ' + (@($planCurrent, $planMissing, $planStale) | ForEach-Object { [regex]::Match($_.Output, '\((reused|not reusable)[^)]*\)').Value }) -join ' / ')
    # Budget admission precedes any hashing of reused outputs: with a disposable budget whose recorded step cost exceeds
    # its allowance the run is refused with no receipt scan; an admitted run scans once, inside its reuse-check step.
    $scanLog = Join-Path $temp 'stamp-scans.log'
    $stampScript = Join-Path $clone 'scripts\LoaderInputStamp.ps1'
    $stampText = [IO.File]::ReadAllText($stampScript)
    $anchor = 'function Get-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration = ''Release'') {'
    if ($stampText.IndexOf($anchor) -lt 0) { throw 'LoaderInputStamp anchor not found.' }
    [IO.File]::WriteAllText($stampScript, $stampText.Replace($anchor, $anchor + "`r`n    [IO.File]::AppendAllText('$($scanLog.Replace("'", "''"))', ""scan`r`n"")"))
    $budgetRoot = Join-Path $temp 'fixture budgets'; New-Item -ItemType Directory -Path $budgetRoot -Force | Out-Null
    [ordered]@{ schemaVersion = 1; budgetId = 'fixture-denied'; allowanceMinutes = 1; blockCeilingMinutes = 30; createdUtc = (Get-Date).ToUniversalTime().ToString('o'); entries = @()
        stepHistory = [ordered]@{ 'reuse check' = [ordered]@{ seconds = 600; observedUtc = (Get-Date).ToUniversalTime().ToString('o') } } } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $budgetRoot 'fixture-denied.json') -Encoding UTF8
    # Both runs are top-level invocations of the disposable budgets: any enclosing workflow entry (for example the
    # controller gate running this suite) is set aside for them and restored afterwards.
    # Full stops at its first runner boundary: an instrumented Test.ps1 in the disposable clone exits where the component
    # runner would start, so the Full suite never runs. With a current receipt the Full parent and Test.ps1 check it
    # once; with a stale receipt Test.ps1 refuses before that boundary.
    $testScript = Join-Path $clone 'scripts\Test.ps1'
    $testText = [IO.File]::ReadAllText($testScript)
    if (([regex]::Matches($testText, [regex]::Escape('& $runner'))).Count -ne 1) { throw 'Test.ps1 runner anchor not found exactly once.' }
    $scanCount = { $n = if (Test-Path -LiteralPath $scanLog) { @(Get-Content -LiteralPath $scanLog | Where-Object { $_ }).Count } else { 0 }; Remove-Item -LiteralPath $scanLog -ErrorAction SilentlyContinue; $n }
    $previousRoot = $env:KMC_WORKFLOW_BUDGET_ROOT; $env:KMC_WORKFLOW_BUDGET_ROOT = $budgetRoot
    $previousEntry = $env:KMC_WORKFLOW_BUDGET_ENTRY; $env:KMC_WORKFLOW_BUDGET_ENTRY = $null
    $previousBudgetId = $env:KMC_WORKFLOW_BUDGET_ID; $env:KMC_WORKFLOW_BUDGET_ID = $null
    try {
        [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
        $denied = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-BudgetId', 'fixture-denied')
        $deniedScans = & $scanCount
        $admitted = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-BudgetId', 'fixture-admitted')
        $admittedScans = & $scanCount
        [IO.File]::WriteAllText($testScript, $testText.Replace('& $runner', "Write-Host 'RUNNER BOUNDARY: the fixture stops before the component runner'; exit 0"))
        [IO.File]::WriteAllText($projectFile, $projectOriginal)
        $fullCurrent = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full')
        $fullCurrentScans = & $scanCount
        # Empty Full history: a stale-receipt Full refusal is charged and leaves the Full duration unknown.
        [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
        $fullEmpty = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-empty')
        $fullEmptyScans = & $scanCount
        $emptyRecord = Get-Content -LiteralPath (Join-Path $budgetRoot 'fixture-full-empty.json') -Raw | ConvertFrom-Json
        $emptyEntries = @($emptyRecord.entries)
        $emptyCharged = if ($emptyEntries.Count -eq 1 -and $null -ne $emptyEntries[0].seconds) { [double]$emptyEntries[0].seconds } else { -1 }
        $emptyHistory = $null -ne $emptyRecord.PSObject.Properties['stepHistory'] -and $null -ne $emptyRecord.stepHistory -and $null -ne $emptyRecord.stepHistory.PSObject.Properties['FULL umbrella (scripts/Test.ps1)']
        [IO.File]::WriteAllText($projectFile, $projectOriginal)
        $fullEmptyPlan = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-empty', '-Plan')
        $fullEmptyPlanScans = & $scanCount
        $historyBudget = Join-Path $budgetRoot 'fixture-full-history.json'
        [ordered]@{ schemaVersion = 1; budgetId = 'fixture-full-history'; allowanceMinutes = 15; blockCeilingMinutes = 30; createdUtc = (Get-Date).ToUniversalTime().ToString('o'); entries = @()
            stepHistory = [ordered]@{ 'FULL umbrella (scripts/Test.ps1)' = [ordered]@{ seconds = 600; observedUtc = (Get-Date).ToUniversalTime().ToString('o') } } } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $historyBudget -Encoding UTF8
        [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
        $full = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-history')
        $fullStaleScans = & $scanCount
        $afterStale = Get-Content -LiteralPath $historyBudget -Raw | ConvertFrom-Json
        $staleEntries = @($afterStale.entries)
        $staleCharged = if ($staleEntries.Count -eq 1 -and $null -ne $staleEntries[0].seconds) { [double]$staleEntries[0].seconds } else { -1 }
        $keptDuration = [double]$afterStale.stepHistory.'FULL umbrella (scripts/Test.ps1)'.seconds
        # A later Full admission with less remaining time than the established duration: the allowance of the disposable
        # record is set to what was spent plus 5 minutes; the run is refused before any receipt scan or runner.
        [IO.File]::WriteAllText($projectFile, $projectOriginal)
        $afterStale.allowanceMinutes = [Math]::Round($staleCharged / 60 + 5, 2)
        $afterStale | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $historyBudget -Encoding UTF8
        $fullShort = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-history')
        $fullShortScans = & $scanCount
        # The successful Full run above recorded its measured duration.
        $fullRecord = Get-Content -LiteralPath (Join-Path $budgetRoot 'fixture-full.json') -Raw | ConvertFrom-Json
        $successDuration = if ($null -ne $fullRecord.stepHistory -and $null -ne $fullRecord.stepHistory.PSObject.Properties['FULL umbrella (scripts/Test.ps1)']) { [double]$fullRecord.stepHistory.'FULL umbrella (scripts/Test.ps1)'.seconds } else { -1 }
        # R7-F1: Full work starts and then fails. The instrumented Test.ps1 passes the receipt check, records that it
        # started, works for about two seconds and fails. The attempt is charged, its measured cost is recorded, and an
        # identical retry with less remaining allowance than that cost is refused before any receipt scan or work.
        $startedMarker = Join-Path $temp 'full-work-started.txt'
        [IO.File]::WriteAllText($testScript, $testText.Replace('& $runner', "[IO.File]::AppendAllText('$($startedMarker.Replace("'", "''"))', ""started`r`n""); Start-Sleep -Seconds 2; Write-Host 'FIXTURE: Full work started and failed'; exit 1"))
        $fullFailed = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-failed')
        $fullFailedScans = & $scanCount
        $failedRecord = Get-Content -LiteralPath (Join-Path $budgetRoot 'fixture-full-failed.json') -Raw | ConvertFrom-Json
        $failedEntries = @($failedRecord.entries)
        $failedCharged = if ($failedEntries.Count -eq 1 -and $null -ne $failedEntries[0].seconds) { [double]$failedEntries[0].seconds } else { -1 }
        $failedMeasured = if ($null -ne $failedRecord.stepHistory -and $null -ne $failedRecord.stepHistory.PSObject.Properties['FULL umbrella (scripts/Test.ps1)']) { [double]$failedRecord.stepHistory.'FULL umbrella (scripts/Test.ps1)'.seconds } else { -1 }
        $failedRecord.allowanceMinutes = [Math]::Round(($failedCharged + [Math]::Max(1, $failedMeasured / 2)) / 60, 4)
        $failedRecord | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $budgetRoot 'fixture-full-failed.json') -Encoding UTF8
        $fullRetry = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-failed')
        $fullRetryScans = & $scanCount
        $startedCount = if (Test-Path -LiteralPath $startedMarker) { @(Get-Content -LiteralPath $startedMarker | Where-Object { $_ }).Count } else { 0 }
        [IO.File]::WriteAllText($testScript, $testText.Replace('& $runner', "Write-Host 'RUNNER BOUNDARY: the fixture stops before the component runner'; exit 0"))
        # R7-F2: the Full receipt check is a visible prerequisite timed within the umbrella. With 120 s of standalone
        # receipt history and 200 s of inclusive Full history, the Full estimate is 200 s: an allowance of 270 s (between
        # 200 and the double-counted 320) admits the stub run, an allowance of 180 s refuses it; Fast and Candidate still
        # list the receipt step with its own duration.
        foreach ($inclusive in @(@{ Id = 'fixture-full-inclusive'; Minutes = 4.5 }, @{ Id = 'fixture-full-inclusive-low'; Minutes = 3 })) {
            [ordered]@{ schemaVersion = 1; budgetId = $inclusive.Id; allowanceMinutes = $inclusive.Minutes; blockCeilingMinutes = 30; createdUtc = (Get-Date).ToUniversalTime().ToString('o'); entries = @()
                stepHistory = [ordered]@{ 'reuse check' = [ordered]@{ seconds = 120; observedUtc = (Get-Date).ToUniversalTime().ToString('o') }; 'FULL umbrella (scripts/Test.ps1)' = [ordered]@{ seconds = 200; observedUtc = (Get-Date).ToUniversalTime().ToString('o') } } } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $budgetRoot ($inclusive.Id + '.json')) -Encoding UTF8
        }
        $inclusivePlan = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-inclusive', '-Plan')
        $fastPlan = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild', '-BudgetId', 'fixture-full-inclusive', '-Plan')
        $candidatePlan = Invoke-PS @('-File', $tiersScript, '-Tier', 'Candidate', '-SkipBuild', '-BudgetId', 'fixture-full-inclusive', '-Plan')
        $null = & $scanCount
        $inclusiveRun = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-inclusive')
        $inclusiveRunScans = & $scanCount
        $inclusiveLow = Invoke-PS @('-File', $tiersScript, '-Tier', 'Full', '-QualificationTask', 'boundary-reuse-check', '-SkipBuild', '-BudgetId', 'fixture-full-inclusive-low')
        $inclusiveLowScans = & $scanCount
    }
    finally {
        $env:KMC_WORKFLOW_BUDGET_ROOT = $previousRoot; $env:KMC_WORKFLOW_BUDGET_ENTRY = $previousEntry; $env:KMC_WORKFLOW_BUDGET_ID = $previousBudgetId
        [IO.File]::WriteAllText($projectFile, $projectOriginal)
        & git -C $clone checkout -q -- scripts/LoaderInputStamp.ps1 scripts/Test.ps1
    }
    Add-Case 'denied-execution-refused-by-budget-before-any-receipt-scan' ($denied.ExitCode -ne 0 -and $denied.Output -match 'Workflow budget refuses' -and $deniedScans -eq 0) ("exit $($denied.ExitCode); receipt scans $deniedScans; " + (Get-Reason $denied.Output))
    Add-Case 'admitted-execution-scans-the-receipt-once-inside-its-step' ($admitted.ExitCode -ne 0 -and $admitted.Output -match 'cannot reuse these outputs' -and $admittedScans -eq 1) ("exit $($admitted.ExitCode); receipt scans $admittedScans; " + (Get-Reason $admitted.Output))
    Add-Case 'full-reuse-with-current-receipt-scans-once-and-reaches-the-runner-boundary' ($fullCurrent.ExitCode -eq 0 -and $fullCurrent.Output -match 'RUNNER BOUNDARY' -and $fullCurrentScans -eq 1) ("exit $($fullCurrent.ExitCode); receipt scans $fullCurrentScans; runner boundary reached: $($fullCurrent.Output -match 'RUNNER BOUNDARY')" + $(if ($fullCurrent.ExitCode) { '; ' + (Get-Reason $fullCurrent.Output) } else { '' }))
    Add-Case 'full-reuse-with-old-outputs-refused-before-the-runner' ($full.ExitCode -ne 0 -and $full.Output -match 'cannot reuse these outputs' -and $full.Output -notmatch 'RUNNER BOUNDARY' -and $fullStaleScans -eq 1) ("exit $($full.ExitCode); receipt scans $fullStaleScans; " + (Get-Reason $full.Output))
    Add-Case 'first-full-receipt-refusal-charged-and-duration-left-unknown' ($fullEmpty.ExitCode -ne 0 -and $fullEmpty.Output -match 'cannot reuse these outputs' -and $fullEmpty.Output -notmatch 'RUNNER BOUNDARY' -and $fullEmptyScans -eq 1 -and $emptyEntries.Count -eq 1 -and [string]$emptyEntries[0].status -ceq 'failed' -and $emptyCharged -gt 0 -and -not $emptyHistory -and $fullEmptyPlan.Output -match 'FULL umbrella \(scripts/Test\.ps1\) \[[^\]]*\] last=unknown') ("empty Full history; refused exit $($fullEmpty.ExitCode), receipt scans $fullEmptyScans, runner boundary reached: $($fullEmpty.Output -match 'RUNNER BOUNDARY'); attempt $(if ($emptyEntries.Count) { [string]$emptyEntries[0].status + ' ' + $emptyCharged + ' s' } else { 'missing' }); Full duration recorded: $emptyHistory; later plan: " + [regex]::Match($fullEmptyPlan.Output, 'FULL umbrella \(scripts/Test\.ps1\) \[[^\]]*\] last=\S+').Value + " (plan scans $fullEmptyPlanScans)")
    Add-Case 'full-receipt-refusal-charged-and-established-duration-kept' ($staleEntries.Count -eq 1 -and [string]$staleEntries[0].status -ceq 'failed' -and $staleCharged -gt 0 -and $keptDuration -eq 600) ("seeded Full duration 600 s; refused attempt entry $(if ($staleEntries.Count) { [string]$staleEntries[0].status + ' ' + $staleCharged + ' s' } else { 'missing' }); recorded Full duration after it $keptDuration s")
    Add-Case 'successful-full-records-its-measured-duration' ($fullCurrent.ExitCode -eq 0 -and $successDuration -gt 0) ("Full duration recorded after the successful run: $successDuration s")
    Add-Case 'full-work-started-then-failed-is-charged-and-its-cost-recorded' ($fullFailed.ExitCode -ne 0 -and $fullFailed.Output -match 'FIXTURE: Full work started and failed' -and $fullFailedScans -eq 1 -and $failedEntries.Count -eq 1 -and [string]$failedEntries[0].status -ceq 'failed' -and $failedCharged -gt 0 -and $failedMeasured -ge 2) ("exit $($fullFailed.ExitCode); receipt scans $fullFailedScans; attempt $(if ($failedEntries.Count) { [string]$failedEntries[0].status + ' ' + $failedCharged + ' s' } else { 'missing' }); recorded Full cost $failedMeasured s")
    Add-Case 'identical-retry-with-less-than-the-measured-cost-refused' ($fullRetry.ExitCode -ne 0 -and $fullRetry.Output -match 'refuses oversized work' -and $fullRetryScans -eq 0 -and $startedCount -eq 1) ("allowance set to spent + $([Math]::Max(1, $failedMeasured / 2)) s; exit $($fullRetry.ExitCode); receipt scans $fullRetryScans; work started $startedCount time(s) in total; " + (Get-Reason $fullRetry.Output))
    $inclusivePlanOk = $inclusivePlan.Output -match 'estimate=200s from recorded durations' -and $inclusivePlan.Output -match 'reuse check \[receipt current: outputs reusable\] timed within FULL umbrella' -and
        $fastPlan.Output -match 'reuse check \[receipt current: outputs reusable\] last=120s' -and $candidatePlan.Output -match 'reuse check \[receipt current: outputs reusable\] last=120s'
    Add-Case 'full-estimate-counts-the-inclusive-duration-once' $inclusivePlanOk ("Full plan: " + [regex]::Match($inclusivePlan.Output, 'estimate=[^\r\n]*').Value + "; " + [regex]::Match($inclusivePlan.Output, 'reuse check \[[^\]]*\] [^:]*').Value.Trim() + "; Fast and Candidate list the receipt step with last=120s: $($fastPlan.Output -match 'reuse check \[[^\]]*\] last=120s')/$($candidatePlan.Output -match 'reuse check \[[^\]]*\] last=120s')")
    Add-Case 'full-admitted-between-inclusive-and-double-counted-cost' ($inclusiveRun.ExitCode -eq 0 -and $inclusiveRun.Output -match 'RUNNER BOUNDARY' -and $inclusiveRunScans -eq 1) ("allowance 270 s; exit $($inclusiveRun.ExitCode); receipt scans $inclusiveRunScans; runner boundary reached: $($inclusiveRun.Output -match 'RUNNER BOUNDARY')" + $(if ($inclusiveRun.ExitCode) { '; ' + (Get-Reason $inclusiveRun.Output) } else { '' }))
    Add-Case 'full-refused-below-the-inclusive-cost' ($inclusiveLow.ExitCode -ne 0 -and $inclusiveLow.Output -match 'refuses oversized work' -and $inclusiveLowScans -eq 0 -and $inclusiveLow.Output -notmatch 'RUNNER BOUNDARY') ("allowance 180 s; exit $($inclusiveLow.ExitCode); receipt scans $inclusiveLowScans; " + (Get-Reason $inclusiveLow.Output))
    Add-Case 'later-full-admission-with-less-time-than-the-established-duration-refused' ($fullShort.ExitCode -ne 0 -and $fullShort.Output -match 'refuses oversized work' -and $fullShort.Output -notmatch 'RUNNER BOUNDARY' -and $fullShortScans -eq 0) ("exit $($fullShort.ExitCode); receipt scans $fullShortScans; " + (Get-Reason $fullShort.Output))
    # The Fast reuse boundary refuses stale outputs before running anything.
    [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
    $tiers = Invoke-PS @('-File', $tiersScript, '-Tier', 'Fast', '-SkipBuild')
    [IO.File]::WriteAllText($projectFile, $projectOriginal)
    Add-Case 'test-tiers-skipbuild-with-old-outputs-refused' ($tiers.ExitCode -ne 0 -and $tiers.Output -match 'cannot reuse these outputs') (Get-Reason $tiers.Output)

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{ schema = 'kmc.package-admission-reuse-tests.v1'; commit = $commit; released_fixture_sha256 = Get-Sha $ReleasedPackage
        main_package_sha256 = Get-Sha $mainZip; observer_package_sha256 = Get-Sha $observerZip; passed = $cases.Count - $failed.Count; total = $cases.Count; cases = @($cases) }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("TOTAL PASS={0} FAIL={1}" -f $summary.passed, $failed.Count)
    if ($failed.Count -gt 0) { exit 1 }
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
