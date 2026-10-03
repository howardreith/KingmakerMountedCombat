# Chunk 6A qualification identities, bound separately:
#  - the exact product candidate (payload): product-source revision and source tree digest,
#    package, manifest, DLL SHA/MVID, qualification suite and purity proof;
#  - the exact qualification harness: the reader/validator revision and the hash of every
#    reader file that participates in ledger validation.
# A pure external-reader correction may revalidate immutable native artifacts under a new
# harness identity without a new product candidate, but only when the strict guard below
# proves that nothing on the native side changed. Read-only; no runtime mutation.
Set-StrictMode -Version Latest
function Get-KmcChunk6aReaderFiles([string]$RepoRoot) {
    $root=[IO.Path]::GetFullPath($RepoRoot)
    $files=@(Get-ChildItem -LiteralPath (Join-Path $root 'scripts\runtime') -File -Filter '*.ps1' | ForEach-Object { 'scripts/runtime/'+$_.Name })
    $files+=@('scripts/Test-Chunk6aLedger.ps1')
    @($files | Sort-Object -Unique)
}
function Get-KmcChunk6aReaderDigest($Readers) {
    $lines=@(foreach($property in @($Readers.PSObject.Properties)) { $property.Name+' '+[string]$property.Value })
    $bytes=[Text.Encoding]::UTF8.GetBytes(($lines -join "`n")+"`n")
    $sha=[Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-','').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-KmcChunk6aHarnessIdentity([string]$RepoRoot) {
    $root=[IO.Path]::GetFullPath($RepoRoot)
    $readers=[ordered]@{}
    foreach($relative in @(Get-KmcChunk6aReaderFiles $root)) {
        $path=Join-Path $root ($relative -replace '/','\')
        if(-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Reader file is missing: $relative" }
        $readers[$relative]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $revision=(& git -C $root rev-parse HEAD).Trim()
    if($revision -cnotmatch '^[0-9a-f]{40}$') { throw 'Reader revision is not an exact commit.' }
    $dirty=@(& git -C $root status --porcelain --untracked-files=all -- scripts/runtime scripts/Test-Chunk6aLedger.ps1)
    $record=[pscustomobject]$readers
    [ordered]@{
        schemaVersion=1
        revision=$revision
        readersClean=($dirty.Count -eq 0)
        readerDigest=(Get-KmcChunk6aReaderDigest $record)
        readers=$record
        computedAtUtc=[DateTime]::UtcNow.ToString('o')
    }
}
# The ledger's recorded harness must be the harness that validates it: every recorded reader
# file exists with the recorded bytes and no participating reader file is unrecorded.
function Assert-KmcChunk6aHarnessIdentity($Harness,[string]$RepoRoot) {
    if($null -eq $Harness) { throw 'Chunk 6A ledger records no qualification harness identity.' }
    if([int]$Harness.schemaVersion -ne 1) { throw 'Unknown Chunk 6A harness identity schema.' }
    if([string]$Harness.revision -cnotmatch '^[0-9a-f]{40}$') { throw 'Harness identity names no exact reader revision.' }
    $readers=$Harness.readers
    if($null -eq $readers) { throw 'Harness identity records no reader files.' }
    $recorded=@($readers.PSObject.Properties | ForEach-Object { $_.Name })
    if($recorded.Count -lt 1) { throw 'Harness identity records no reader files.' }
    if([string]$Harness.readerDigest -cne (Get-KmcChunk6aReaderDigest $readers)) { throw 'Harness reader digest does not match its recorded reader hashes.' }
    $current=(Get-KmcChunk6aHarnessIdentity $RepoRoot)
    $differences=@()
    foreach($relative in @(Get-KmcChunk6aReaderFiles $RepoRoot)) {
        if($relative -cnotin $recorded) { $differences+=('unrecorded '+$relative); continue }
        if([string]$readers.$relative -cne [string]$current.readers.$relative) { $differences+=('changed '+$relative) }
    }
    foreach($relative in $recorded) { if($relative -cnotin @(Get-KmcChunk6aReaderFiles $RepoRoot)) { $differences+=('removed '+$relative) } }
    if($differences.Count -ne 0) {
        throw ('Chunk 6A reader revision differs from the harness that qualified this ledger; re-qualify under a new harness identity: '+($differences -join '; '))
    }
}
# The separate identities are required of every Chunk 6A candidate from preview.150 and of every Chunk 6B candidate (a frozen
# payload's version is bound to its package manifest). Earlier candidates, the historical product
# lines and parser-only synthetic ledgers predate them; a recorded identity is always validated.
function Test-KmcChunk6aIdentitiesRequired([string]$ProductVersion) {
    ([string]$ProductVersion -cmatch '^0[.]1[.]0-chunk6[ab]-preview[.]([0-9]+)$' -and [long]$Matches[1] -ge 150)
}
function Get-KmcChunk6aSourceTreeDigest([string]$RepoRoot,[string]$Commit) {
    if($Commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Source tree digest requires an exact commit.' }
    $tree=(& git -C $RepoRoot rev-parse ($Commit+':src')).Trim()
    if($LASTEXITCODE -ne 0 -or $tree -cnotmatch '^[0-9a-f]{40}$') { throw 'Product source tree is not resolvable at the frozen commit.' }
    $tree
}
# Files whose change never alters native evidence production: dedicated external reader
# files, tests, documentation and planning. Everything else (product source, build identity,
# the shared harness with its launcher/safety/restoration logic, fixtures, packaging) is
# native-side and requires a new product candidate.
function Test-KmcChunk6aPureReaderPath([string]$Path) {
    $p=$Path -replace '\\','/'
    if($p -ceq 'scripts/runtime/RuntimeHarness.Common.ps1' -or $p -ceq 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1') { return $false }
    if($p -cmatch '^scripts/runtime/[^/]+Evidence\.ps1$') { return $true }
    if($p -ceq 'scripts/Test-Chunk6aLedger.ps1') { return $true }
    if($p -cmatch '^scripts/Test-[^/]+\.ps1$') { return $true }
    if($p -cmatch '^(docs|planning)/') { return $true }
    $false
}
function Get-KmcChunk6aChangedPaths([string]$RepoRoot,[string]$FromCommit,[string]$ToCommit) {
    foreach($commit in @($FromCommit,$ToCommit)) { if($commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Pure reader comparison requires exact commits.' } }
    @(& git -C $RepoRoot diff --name-only $FromCommit $ToCommit | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
function Test-KmcChunk6aPureReaderChange([string]$RepoRoot,[string]$FromCommit,[string]$ToCommit) {
    $changed=@(Get-KmcChunk6aChangedPaths $RepoRoot $FromCommit $ToCommit)
    $violations=@($changed | Where-Object { -not (Test-KmcChunk6aPureReaderPath $_) })
    [pscustomobject]@{ fromCommit=$FromCommit; toCommit=$ToCommit; changed=$changed; violations=$violations; pure=($violations.Count -eq 0 -and $changed.Count -gt 0) }
}
