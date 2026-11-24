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

    # 2. Real packages from the clone (both shipping products), built by the qualification packaging routes.
    $pkg = Invoke-PS @('-File', (Join-Path $clone 'scripts\Package.ps1'), '-ArtifactQualifier', 'boundary')
    $obsPkg = Invoke-PS @('-File', (Join-Path $clone 'scripts\Package-Observer.ps1'), '-ArtifactQualifier', 'boundary')
    $mainZip = @(Get-ChildItem -LiteralPath $artifacts -Filter 'KingmakerMountedCombat-*-boundary-diagnostic.zip')[0].FullName
    $observerZip = @(Get-ChildItem -LiteralPath $artifacts -Filter 'KmcRemovalObserver-*-boundary-diagnostic.zip')[0].FullName
    Add-Case 'packages-built-by-qualification-routes' ($pkg.ExitCode -eq 0 -and $obsPkg.ExitCode -eq 0) "main $(Get-Sha $mainZip); observer $(Get-Sha $observerZip)"

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
    $mainDllEntry = @([IO.Compression.ZipFile]::OpenRead($mainZip).Entries | Where-Object { $_.FullName -like '*KingmakerMountedCombat.dll' })[0].FullName
    $variant = Join-Path $artifacts 'main-released-umm-0.33-dll.zip'
    New-ZipVariant $mainZip $variant @{ $mainDllEntry = $releasedDll }
    $r = Invoke-PS @('-File', $validators.main, '-PackagePath', $variant)
    Add-Case 'main-validator-dll-compiled-against-umm-0.33-rejected' ($r.ExitCode -ne 0 -and $r.Output -match 'UnityModManager') ("released 0.2.15 DLL (UnityModManager 0.33.0.0): " + (Get-Reason $r.Output))

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
        @{ Name = 'project-reference'; Apply = { [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>')) }; Undo = { [IO.File]::WriteAllText($projectFile, $projectOriginal) } })) {
        & $change.Apply
        $r = & $stampCheck
        Add-Case "reuse-after-$($change.Name)-change-rejected" ($r.ExitCode -ne 0 -and $r.Output -match 'cannot reuse these outputs') (Get-Reason $r.Output)
        & $change.Undo
    }
    [IO.File]::WriteAllText($projectFile, $projectOriginal.Replace('</Project>', '<!-- changed reference --></Project>'))
    $tiers = Invoke-PS @('-File', (Join-Path $clone 'scripts\Test-Tiers.ps1'), '-Tier', 'Fast', '-SkipBuild')
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
