[CmdletBinding()]
param(
    # The product DLL whose actual replacement-commit code is exercised. Defaults to the
    # current build output; pass the frozen preview.96 DLL to reproduce the owner's failure.
    [string]$DllPath = '',
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    # Reproduction mode: the exercised DLL is expected to REFUSE the engine-shaped paths.
    [switch]$ExpectRefusal
)
# Detached engine-type check of the real NativeMountedArchiveCommit replacement branch:
# the actual installed ZipSaver/SaveInfo types, the product's own PreservePrevious and
# Replace methods, real temporary archives, and archive paths built exactly as
# SaveManager.PrepareSave builds them from Unity's forward-slash persistent data path.
# No game is launched, no human save root is touched, nothing is patched.
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$intake = Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json') | ConvertFrom-Json
$managed = Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data\Managed'
if ([string]::IsNullOrWhiteSpace($DllPath)) { $DllPath = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll') }
$DllPath = [IO.Path]::GetFullPath($DllPath)
if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) { throw "Product DLL missing: $DllPath" }
$dllSha = (Get-FileHash -LiteralPath $DllPath -Algorithm SHA256).Hash.ToLowerInvariant()
$script:passes = 0; $script:failures = New-Object 'System.Collections.Generic.List[string]'
function Assert-Check([bool]$Condition, [string]$Message) {
    if ($Condition) { $script:passes++; Write-Host "PASS $Message" } else { $script:failures.Add($Message); Write-Host "FAIL $Message" }
}
$handler = [ResolveEventHandler]{ param($sender, $eventArgs)
    $name = ($eventArgs.Name -split ',')[0] + '.dll'
    foreach ($directory in @($managed, (Join-Path $managed 'UnityModManager'))) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null }
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('kmc-commit-paths-' + [Guid]::NewGuid().ToString('N'))
try {
    $engine = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
    $product = [Reflection.Assembly]::LoadFrom($DllPath)
    $zipSaver = $engine.GetType('Kingmaker.EntitySystem.Persistence.ZipSaver', $true)
    $saveInfo = $engine.GetType('Kingmaker.EntitySystem.Persistence.SaveInfo', $true)
    $commit = $product.GetType('KingmakerMountedCombat.Integration.NativeMountedArchiveCommit', $true)
    $flags = [Reflection.BindingFlags]'Static,NonPublic,Public'
    $preserve = $commit.GetMethod('PreservePrevious', $flags); $replace = $commit.GetMethod('Replace', $flags)
    $commitCount = $commit.GetProperty('CommitCount', $flags); $failureCount = $commit.GetProperty('ReplacementFailureCount', $flags)
    Assert-Check ($null -ne $preserve -and $null -ne $replace -and $null -ne $commitCount -and $null -ne $failureCount) 'product replacement-commit members resolved'
    # Build the archives exactly where the engine would: Unity persistent data path form
    # (forward slashes) + Path.Combine("Saved Games") + leaf + ".zks".
    New-Item -ItemType Directory -Path (Join-Path $temporary 'Saved Games') -Force | Out-Null
    $unityParent = $temporary.Replace('\', '/')
    $savedGames = [IO.Path]::Combine($unityParent, 'Saved Games')
    $stagedPath = [IO.Path]::Combine($savedGames, 'Quick_2') + '.zks'
    $originalPath = [IO.Path]::Combine($savedGames, 'Quick_1') + '.zks'
    Assert-Check ($stagedPath.Contains('/') -and $stagedPath.Contains('\') -and [IO.Path]::GetFullPath($stagedPath) -cne $stagedPath) 'fixture paths mix separators like the engine (GetFullPath differs)'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($pair in @(@($stagedPath, 'staged'), @($originalPath, 'original'))) {
        $folder = Join-Path $temporary ('src-' + $pair[1]); New-Item -ItemType Directory -Path $folder -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $folder 'header.json') -Value ('{"slot":"' + $pair[1] + '"}') -Encoding ASCII
        $zip = [IO.Path]::GetFullPath($pair[0]); if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip }
        [IO.Compression.ZipFile]::CreateFromDirectory($folder, $zip)
    }
    $stagedBytes = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($stagedPath))
    $stagedSaver = [Activator]::CreateInstance($zipSaver, @([object]$stagedPath))
    $originalSaver = [Activator]::CreateInstance($zipSaver, @([object]$originalPath))
    $stagedInfo = [Activator]::CreateInstance($saveInfo); $originalInfo = [Activator]::CreateInstance($saveInfo)
    $saveInfo.GetProperty('Saver').SetValue($stagedInfo, $stagedSaver, $null); $saveInfo.GetProperty('FolderName').SetValue($stagedInfo, $stagedPath, $null)
    $saveInfo.GetProperty('Saver').SetValue($originalInfo, $originalSaver, $null); $saveInfo.GetProperty('FolderName').SetValue($originalInfo, $originalPath, $null)
    $commitsBefore = [int]$commitCount.GetValue($null, $null)
    $refusal = $null
    try {
        # The transpiled worker calls these two in this order with these arguments
        # (SerializeAndSaveThread 0x0600802A: originalSave.Saver.Clear(); saveInfo.Saver.RenameFile(originalSave.FolderName)).
        $preserve.Invoke($null, @([object]$originalSaver, [object]$stagedInfo))
        $replace.Invoke($null, @([object]$stagedSaver, [object]$originalPath, [object]$originalInfo))
    } catch { $refusal = $_.Exception; while ($refusal.InnerException) { $refusal = $refusal.InnerException } }
    $commitsAfter = [int]$commitCount.GetValue($null, $null)
    $originalFull = [IO.Path]::GetFullPath($originalPath); $stagedFull = [IO.Path]::GetFullPath($stagedPath)
    $landed = (Test-Path -LiteralPath $originalFull) -and (-not (Test-Path -LiteralPath $stagedFull)) -and
        ([Convert]::ToBase64String([IO.File]::ReadAllBytes($originalFull)) -ceq [Convert]::ToBase64String($stagedBytes))
    if ($ExpectRefusal) {
        $refusalText = if ($null -ne $refusal) { $refusal.Message } else { 'no exception' }
        Assert-Check ($null -ne $refusal -and $refusal -is [IO.IOException]) ('reproduction: the exercised DLL refused the engine-shaped replacement (' + $refusalText + ')')
        Assert-Check ($commitsAfter -eq $commitsBefore -and (Test-Path -LiteralPath $originalFull) -and (Test-Path -LiteralPath $stagedFull)) 'reproduction: no commit recorded; previous archive and staged archive both still present'
    } else {
        $refusalText = if ($null -ne $refusal) { ': ' + $refusal.Message } else { '' }
        Assert-Check ($null -eq $refusal) ('the product accepted the engine-shaped replacement' + $refusalText)
        Assert-Check $landed 'the staged archive replaced the previous slot in place and the staged leaf is gone'
        Assert-Check ($commitsAfter -eq $commitsBefore + 1) 'exactly one commit was recorded'
        Assert-Check ([int]$failureCount.GetValue($null, $null) -eq 0) 'no replacement failure was counted'
        $rebound = [string]$zipSaver.GetProperty('FolderName').GetValue($stagedSaver, $null)
        Assert-Check ($rebound -ceq $originalPath) 'the staged saver was rebound to the engine-owned destination string'
    }
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($handler)
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue }
}
$mode = if ($ExpectRefusal) { 'reproduction' } else { 'verification' }
Write-Host ("NATIVE ARCHIVE COMMIT PATHS dll=" + $dllSha + " mode=" + $mode + " PASS=" + $script:passes + " FAIL=" + $script:failures.Count)
if ($script:failures.Count -ne 0) { exit 1 }
