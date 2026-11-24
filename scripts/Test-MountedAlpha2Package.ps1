[CmdletBinding()]
param()
# Review R1-F14: a reviewable receipt for the actual private-alpha ZIP. Reads docs/mounted-melee/package-identity.json,
# locates that exact archive in the lab artifacts directory, runs the repository's Validate-Package.ps1 on it, and reads
# back its hash, size, entries, enclosed DLL hash and MVID, Info.json version and UMM minimum, the embedded Mounted Shield
# icon, and the manifest's source commit; every value must equal the committed record. Run inside the controller's
# focused gate, its output is captured with the gate receipt. It does not build, install or launch anything.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$record = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'docs\mounted-melee\package-identity.json') | ConvertFrom-Json
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 package: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }
function Sha([byte[]]$Bytes) { $h = [Security.Cryptography.SHA256]::Create(); try { -join ($h.ComputeHash($Bytes) | ForEach-Object { $_.ToString('x2') }) } finally { $h.Dispose() } }

$zip = Join-Path (Join-Path $lab 'artifacts') ([string]$record.zip)
Check ([IO.Path]::GetFileName($zip) -ceq [string]$record.zip -and (Test-Path -LiteralPath $zip -PathType Leaf)) ('archive present: ' + $zip)
$bytes = [IO.File]::ReadAllBytes($zip)
$zipSha = Sha $bytes
Write-Host ('RECEIPT zip=' + $record.zip + ' sha256=' + $zipSha + ' bytes=' + $bytes.Length)
Check ($zipSha -ceq [string]$record.sha256 -and $bytes.Length -eq [long]$record.bytes) 'archive SHA-256 and size equal the record'
$manifest = Get-Content -Raw -LiteralPath ($zip + '.manifest.json') | ConvertFrom-Json
Check ([string]$manifest.packageSha256 -ceq $zipSha -and [string]$manifest.commit -ceq [string]$record.sourceCommit -and $manifest.worktreeClean -eq $true -and
    [string]$manifest.version -ceq [string]$record.version) ('manifest: commit ' + $manifest.commit + ', clean worktree, version ' + $manifest.version)

Add-Type -AssemblyName System.IO.Compression
$archive = New-Object IO.Compression.ZipArchive((New-Object IO.MemoryStream(, $bytes)), [IO.Compression.ZipArchiveMode]::Read)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName } | Sort-Object)
    Write-Host ('RECEIPT entries=' + ($entries -join ','))
    Check (($entries -join ',') -ceq 'KingmakerMountedCombat/Info.json,KingmakerMountedCombat/KingmakerMountedCombat.dll') 'exactly Info.json and the DLL'
    function EntryBytes([string]$Name) { $s = $archive.GetEntry($Name).Open(); try { $m = New-Object IO.MemoryStream; $s.CopyTo($m); , $m.ToArray() } finally { $s.Dispose() } }
    $info = [Text.Encoding]::UTF8.GetString((EntryBytes 'KingmakerMountedCombat/Info.json')) | ConvertFrom-Json
    Write-Host ('RECEIPT Info.json Version=' + $info.Version + ' ManagerVersion=' + $info.ManagerVersion)
    Check ([string]$info.Version -ceq [string]$record.version -and [string]$info.ManagerVersion -ceq [string]$record.ummMinimum) 'Info.json version and UMM minimum'
    $dll = EntryBytes 'KingmakerMountedCombat/KingmakerMountedCombat.dll'
    $dllSha = Sha $dll
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($dll)
    $mvid = $assembly.ManifestModule.ModuleVersionId.ToString()
    Write-Host ('RECEIPT dll sha256=' + $dllSha + ' mvid=' + $mvid)
    Check ($dllSha -ceq [string]$record.dllSha256 -and $mvid -ceq [string]$record.dllMvid -and $dllSha -ceq [string]$manifest.dllSha256) 'enclosed DLL hash and MVID'
    $iconStream = $assembly.GetManifestResourceStream('KingmakerMountedCombat.Assets.MountedShieldFeat.png')
    Check ($null -ne $iconStream) 'embedded Mounted Shield icon resource present'
    $icon = New-Object IO.MemoryStream; $iconStream.CopyTo($icon)
    $iconSha = Sha $icon.ToArray()
    Write-Host ('RECEIPT icon sha256=' + $iconSha)
    Check ($iconSha -ceq [string]$record.iconSha256) 'embedded Mounted Shield icon identity'
} finally { $archive.Dispose() }

& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $zip
Check ($LASTEXITCODE -eq 0) 'repository Validate-Package.ps1 on this exact archive'
Write-Host ('Mounted alpha 2 package receipt PASS=' + $script:passed + ' FAIL=0')
