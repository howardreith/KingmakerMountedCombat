[CmdletBinding()]
param(
    [string]$ArchivePath,
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

# Execute the unchanged production helper in disposable repository layouts using genuine pinned archive bytes.
# Only the external download is stubbed. No network, game installation, product build or native qualification.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Join-Path $PSScriptRoot '..' }
$sourceRoot = [IO.Path]::GetFullPath($RepositoryRoot)
if ([string]::IsNullOrWhiteSpace($ArchivePath)) { $ArchivePath = Join-Path $sourceRoot 'obj\references\umm-0.31.1\unitymodmanager.0.31.1.nupkg' }
$sourceArchive = [IO.Path]::GetFullPath($ArchivePath)
if (-not (Test-Path -LiteralPath $sourceArchive -PathType Leaf)) {
    throw 'A genuine pinned UMM archive is required: supply -ArchivePath or restore the build reference first.'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $sourceRoot 'obj\umm-build-reference-tests' }
$trialRoot = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $trialRoot -Force | Out-Null
$ummTestTransport = [pscustomobject]@{archive=$sourceArchive;mode='refuse';downloads=0}
function Invoke-WebRequest {
    param([switch]$UseBasicParsing, [string]$Uri, [string]$OutFile, [int]$TimeoutSec)
    $ummTestTransport.downloads++
    if ($ummTestTransport.mode -ceq 'genuine') { [IO.File]::Copy($ummTestTransport.archive, $OutFile); return }
    if ($ummTestTransport.mode -ceq 'corrupt') { [IO.File]::WriteAllText($OutFile, 'not the archive'); return }
    throw 'Unexpected network request in an offline cache case.'
}
function Need([bool]$Value, [string]$Message) { if (-not $Value) { throw $Message } }
function New-Trial([string]$Name) {
    $root = Join-Path $trialRoot $Name
    foreach ($dir in @('scripts','planning','obj\references\umm-0.31.1')) { New-Item -ItemType Directory -Path (Join-Path $root $dir) -Force | Out-Null }
    foreach ($file in @('scripts\Restore-UmmBuildReference.ps1','planning\UMM-BUILD-REFERENCE.json','Info.json')) {
        Copy-Item -LiteralPath (Join-Path $sourceRoot $file) -Destination (Join-Path $root $file)
    }
    [pscustomobject]@{root=$root;helper=(Join-Path $root 'scripts\Restore-UmmBuildReference.ps1');cache=(Join-Path $root 'obj\references\umm-0.31.1')}
}
$rows = New-Object 'Collections.Generic.List[object]'
function Passed([string]$Name) { $rows.Add([pscustomobject]@{name=$Name;status='PASS'}); Write-Host "PASS $Name" }
function Refused([string]$Helper) {
    $caught = $false
    try { & $Helper } catch { Need ($_.Exception.Message -like '*hash mismatch*') $_.Exception.Message; $caught = $true }
    Need $caught 'Changed reference bytes were accepted.'
}
$pin = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot 'planning\UMM-BUILD-REFERENCE.json') | ConvertFrom-Json
$a = New-Trial 'cached-archive'
Copy-Item -LiteralPath $sourceArchive -Destination (Join-Path $a.cache 'unitymodmanager.0.31.1.nupkg')
& $a.helper
$dll = Join-Path $a.cache 'UnityModManager.dll'
Need (((Get-FileHash $dll).Hash.ToLowerInvariant()) -ceq $pin.sha256) 'Extracted DLL differs.'
Need ($ummTestTransport.downloads -eq 0) 'Cached extraction attempted a download.'
Passed 'genuine cached archive extracts the exact DLL without network'
$before = [IO.File]::GetLastWriteTimeUtc($dll)
& $a.helper
Need ([IO.File]::GetLastWriteTimeUtc($dll) -eq $before) 'Warm cache rewrote the DLL.'
Need ($ummTestTransport.downloads -eq 0) 'Warm cache attempted a download.'
Passed 'warm cache reuses verified DLL without writing or downloading'

[IO.File]::WriteAllText($dll, 'tampered DLL')
Refused $a.helper
Need ([IO.File]::ReadAllText($dll) -ceq 'tampered DLL') 'Tampered DLL was overwritten.'
Passed 'present changed DLL is refused and preserved'

$b = New-Trial 'changed-archive'
$badArchive = Join-Path $b.cache 'unitymodmanager.0.31.1.nupkg'
[IO.File]::WriteAllText($badArchive, 'tampered archive')
Refused $b.helper
Need ([IO.File]::ReadAllText($badArchive) -ceq 'tampered archive') 'Tampered archive was overwritten.'
Need (-not (Test-Path (Join-Path $b.cache 'UnityModManager.dll'))) 'Changed archive produced a DLL.'
Need ($ummTestTransport.downloads -eq 0) 'Tamper refusal attempted a download.'
Passed 'present changed archive is refused and preserved without download'

$c = New-Trial 'download-transport'
$ummTestTransport.mode = 'genuine'
& $c.helper
Need ($ummTestTransport.downloads -eq 1) 'Missing cache did not request exactly one download.'
Need (((Get-FileHash (Join-Path $c.cache 'UnityModManager.dll')).Hash.ToLowerInvariant()) -ceq $pin.sha256) 'Transport branch produced different DLL.'
Passed 'missing cache downloads once through a stubbed external transport and verifies genuine bytes'

Remove-Item -LiteralPath (Join-Path $c.cache 'unitymodmanager.0.31.1.nupkg')
$ummTestTransport.mode = 'refuse'
& $c.helper
Need ($ummTestTransport.downloads -eq 1) 'A verified DLL without archive was downloaded again.'
Need (-not (Test-Path (Join-Path $c.cache 'unitymodmanager.0.31.1.nupkg'))) 'Warm DLL recreated the archive.'
Passed 'verified DLL alone remains an offline reusable build reference'

$d = New-Trial 'bad-download'
$ummTestTransport.mode = 'corrupt'
$protocol = [Net.ServicePointManager]::SecurityProtocol
Refused $d.helper
Need (@(Get-ChildItem -LiteralPath $d.cache -File).Count -eq 0) 'Rejected download left a published cache file or temporary residue.'
Need ([Net.ServicePointManager]::SecurityProtocol -eq $protocol) 'TLS setting was not restored.'
Passed 'bad downloaded bytes are refused before publishing and temporary state is cleaned'

$e = New-Trial 'changed-manifest'
Copy-Item -LiteralPath $sourceArchive -Destination (Join-Path $e.cache 'unitymodmanager.0.31.1.nupkg')
& $e.helper
$changedPinPath = Join-Path $e.root 'planning\UMM-BUILD-REFERENCE.json'
$changedPin = Get-Content -Raw -LiteralPath $changedPinPath | ConvertFrom-Json
$changedPin.sha256 = '0' * 64
$changedPin | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $changedPinPath -Encoding utf8
$downloadsBefore = $ummTestTransport.downloads
Refused $e.helper
Need (((Get-FileHash (Join-Path $e.cache 'UnityModManager.dll')).Hash.ToLowerInvariant()) -ceq $pin.sha256) 'Changed manifest caused cached DLL replacement.'
Need ($ummTestTransport.downloads -eq $downloadsBefore) 'Changed manifest caused a download over the existing DLL.'
Passed 'changed manifest DLL hash is refused even with a previously valid warm cache'

$f = New-Trial 'changed-declared-minimum'
Copy-Item -LiteralPath (Join-Path $e.cache 'UnityModManager.dll') -Destination (Join-Path $f.cache 'UnityModManager.dll')
$changedInfoPath = Join-Path $f.root 'Info.json'
$changedInfo = Get-Content -Raw -LiteralPath $changedInfoPath | ConvertFrom-Json
$changedInfo.ManagerVersion = '0.33.0'
$changedInfo | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $changedInfoPath -Encoding utf8
$caught = $false
try { & $f.helper } catch { Need ($_.Exception.Message -like '*ManagerVersion must match*') $_.Exception.Message; $caught = $true }
Need $caught 'A declared minimum inconsistent with the build reference was accepted.'
Need ($ummTestTransport.downloads -eq $downloadsBefore) 'Declared-minimum refusal attempted a download.'
Passed 'declared minimum must match the build baseline before reusing a warm cache'

$receipt = [ordered]@{status='PASS';passed=$rows.Count;failed=0;powerShell=$PSVersionTable.PSVersion.ToString();nativeProof=$false;windowsBuildProof=$false;network='stubbed only; real upstream archive bytes independently SHA-256 verified';cases=@($rows.ToArray())}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $trialRoot 'receipt.json') -Encoding utf8
Write-Host ('RESTORE HELPER PASS ' + $rows.Count + ' FAIL 0')
Write-Host ('RECEIPT=' + (Join-Path $trialRoot 'receipt.json'))
