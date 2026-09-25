[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$repo=Get-KmcRepositoryRoot
$intake=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'environment-intake.json')
$installed=Join-Path $intake.requestedLayout.kingmakerModsRoot 'KingmakerMountedCombat'
$testParent=Assert-KmcChildPath (Join-Path $repo ('obj/starting-installation-tests/'+[Guid]::NewGuid().ToString('N'))) (Join-Path $repo 'obj') 'test parent'
$clone=Join-Path $testParent 'KingmakerMountedCombat'
[void][IO.Directory]::CreateDirectory($clone)
[void](Assert-KmcPhase3fStartingInstallation $installed)
$leaves=@(Get-ChildItem -LiteralPath $installed -File -Force | ForEach-Object Name)
foreach($leaf in $leaves){[IO.File]::Copy((Join-Path $installed $leaf),(Join-Path $clone $leaf),$false)}
[void](Assert-KmcPhase3fStartingInstallation $clone)
$passes=1

function Assert-KmcRejected {
    param([Parameter(Mandatory=$true)][string]$Root,[Parameter(Mandatory=$true)][string]$What)
    $rejected=$false
    try { [void](Assert-KmcPhase3fStartingInstallation $Root) } catch { $rejected=$true }
    if(-not $rejected){throw "$What was accepted."}
}

# Mutated bytes, one leaf at a time.
foreach($leaf in $leaves) {
    $path=Join-Path $clone $leaf; $bytes=[IO.File]::ReadAllBytes($path)
    try {
        $changed=[byte[]]$bytes.Clone();$changed[0]=$changed[0] -bxor 1
        [IO.File]::WriteAllBytes($path,$changed)
        Assert-KmcRejected $clone "Changed starting-installation bytes ($leaf)";$passes++
    } finally {[IO.File]::WriteAllBytes($path,$bytes)}
}

# An extra unregistered file.
$extra=Join-Path $clone 'unregistered.cache'
[IO.File]::WriteAllText($extra,'unregistered')
Assert-KmcRejected $clone 'Extra starting-installation file';$passes++
[IO.File]::Delete($extra)
[void](Assert-KmcPhase3fStartingInstallation $clone);$passes++

# A missing registered file: the entry count no longer matches the pin set.
foreach($leaf in $leaves) {
    $path=Join-Path $clone $leaf; $bytes=[IO.File]::ReadAllBytes($path)
    try {
        [IO.File]::Delete($path)
        Assert-KmcRejected $clone "Missing registered starting-installation file ($leaf)";$passes++
    } finally {[IO.File]::WriteAllBytes($path,$bytes)}
}

# A renamed registered file: right bytes, wrong name, right count.
foreach($leaf in $leaves) {
    $path=Join-Path $clone $leaf; $renamed=Join-Path $clone ($leaf+'.renamed')
    try {
        [IO.File]::Move($path,$renamed)
        Assert-KmcRejected $clone "Renamed registered starting-installation file ($leaf)";$passes++
    } finally { if([IO.File]::Exists($renamed)){[IO.File]::Move($renamed,$path)} }
}

# A mixed payload: each leaf's bytes replaced by another registered payload's
# bytes. Every pin set is keyed on Info.json, so swapping the DLL for a different
# registered release must be rejected rather than matched by a fallback.
$foreignDll=[Text.Encoding]::UTF8.GetBytes('registered-elsewhere-but-not-this-payload')
$dllLeaf=@($leaves | Where-Object { $_ -ceq 'KingmakerMountedCombat.dll' })
if($dllLeaf.Count -eq 1) {
    $path=Join-Path $clone $dllLeaf[0]; $bytes=[IO.File]::ReadAllBytes($path)
    try {
        [IO.File]::WriteAllBytes($path,$foreignDll)
        Assert-KmcRejected $clone 'Mixed starting-installation payload (foreign DLL bytes)';$passes++
    } finally {[IO.File]::WriteAllBytes($path,$bytes)}
}

# A subdirectory is never a registered entry, whatever it contains.
$nested=Join-Path $clone 'nested'
[void][IO.Directory]::CreateDirectory($nested)
try { Assert-KmcRejected $clone 'Subdirectory inside the starting installation';$passes++ }
finally { [IO.Directory]::Delete($nested,$true) }

# The exact live installation is still accepted, unchanged, at the end.
[void](Assert-KmcPhase3fStartingInstallation $clone);$passes++
[void](Assert-KmcPhase3fStartingInstallation $installed);$passes++
Write-Host "REGISTERED STARTING INSTALLATION PASS=$passes FAIL=0; live bytes were read only."
