[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$parent=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\obj\inventory-hash-tests')).TrimEnd('\')
$root=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'inventory hash test root'
$passes=0
function Assert-HashTest([bool]$Condition,[string]$Name) {
    if(-not $Condition){throw "FAIL $Name"}
    $script:passes++;Write-Host "PASS $Name"
}
function Assert-HashThrows([scriptblock]$Body,[string]$Name) {
    $threw=$false;try{& $Body | Out-Null}catch{$threw=$true}
    Assert-HashTest $threw $Name
}
function Assert-ManifestTypes($Manifest,[string]$Name) {
    Assert-HashTest ($Manifest.GetType() -eq [System.Management.Automation.PSCustomObject]) "$Name returns PSCustomObject"
    Assert-HashTest ($Manifest.schemaVersion.GetType() -eq [int]) "$Name schema is Int32"
    Assert-HashTest ($Manifest.directoryCount.GetType() -eq [int]) "$Name directory count is Int32"
    Assert-HashTest ($Manifest.fileCount.GetType() -eq [int]) "$Name file count is Int32"
    Assert-HashTest ($Manifest.totalBytes.GetType() -eq [long]) "$Name total bytes is Int64"
    Assert-HashTest ($Manifest.root.GetType() -eq [string] -and $Manifest.digest.GetType() -eq [string]) "$Name root and digest remain strings"
    Assert-HashTest ($Manifest.entries.GetType() -eq [object[]]) "$Name entries remain object array"
    $lengthTypes=$true
    foreach($entry in $Manifest.entries){
        if($entry.kind -ceq 'directory'){$lengthTypes=$lengthTypes -and $entry.length.GetType() -eq [int]}
        elseif($entry.kind -ceq 'file'){$lengthTypes=$lengthTypes -and $entry.length.GetType() -eq [long]}
        else{$lengthTypes=$false}
    }
    Assert-HashTest $lengthTypes "$Name preserves each directory Int32 and file Int64 length"
}
function ReferenceManifest([string]$Root) {
    # Original serial implementation retained only as an independent test oracle.
    $fullRoot=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    $records=New-Object 'System.Collections.Generic.List[object]'
    foreach($directory in @(Get-ChildItem -LiteralPath $fullRoot -Directory -Recurse -Force | Sort-Object FullName)){
        $records.Add([pscustomobject]@{kind='directory';path=$directory.FullName.Substring($fullRoot.Length+1).Replace('\','/');length=0;sha256=$null})
    }
    foreach($file in @(Get-ChildItem -LiteralPath $fullRoot -File -Recurse -Force | Sort-Object FullName)){
        $records.Add([pscustomobject]@{kind='file';path=$file.FullName.Substring($fullRoot.Length+1).Replace('\','/');length=[long]$file.Length;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()})
    }
    $ordered=@($records | Sort-Object kind,path)
    $canonical=($ordered | ForEach-Object {'{0}|{1}|{2}|{3}' -f $_.kind,$_.path,$_.length,$_.sha256}) -join "`n"
    return [pscustomobject]@{digest=Get-KmcTextSha256 $canonical;entries=$ordered}
}
try {
    [void][IO.Directory]::CreateDirectory((Join-Path $root 'nested\empty'))
    $empty=Join-Path $root 'empty.bin'
    [IO.File]::WriteAllBytes($empty,(New-Object byte[] 0))
    $literal=Join-Path $root ('literal [x] & '+[char]0xE9+'.bin')
    [IO.File]::WriteAllBytes($literal,[Text.Encoding]::UTF8.GetBytes('literal-path bytes'))
    $large=Join-Path $root 'nested\large.bin'
    $bytes=New-Object byte[] (2MB+37)
    (New-Object Random(739)).NextBytes($bytes)
    [IO.File]::WriteAllBytes($large,$bytes)
    (Get-Item -LiteralPath $literal).IsReadOnly=$true
    $actual=Get-KmcDirectoryManifest $root
    $reference=ReferenceManifest $root
    Assert-HashTest ($actual.digest -ceq $reference.digest) 'complete canonical inventory digest matches original serial implementation'
    Assert-HashTest (($actual.entries | ConvertTo-Json -Depth 5 -Compress) -ceq ($reference.entries | ConvertTo-Json -Depth 5 -Compress)) 'all paths kinds lengths and SHA-256 values match exactly'
    Assert-HashTest ($actual.fileCount -eq 3 -and $actual.directoryCount -eq 2 -and $actual.totalBytes -eq ((2MB+37)+18)) 'empty directories empty files and exact byte counts are preserved'
    $single=Get-KmcDirectoryManifest (Join-Path $root 'nested')
    Assert-HashTest ($single.fileCount -eq 1 -and $single.digest -ceq (ReferenceManifest (Join-Path $root 'nested')).digest) 'single-file inventory retains the complete hash rather than scalar characters'
    $emptyDirectory=Get-KmcDirectoryManifest (Join-Path $root 'nested\empty')
    Assert-HashTest ($emptyDirectory.fileCount -eq 0 -and $emptyDirectory.directoryCount -eq 0 -and $emptyDirectory.digest -ceq (Get-KmcTextSha256 '')) 'empty inventory retains its exact empty-content digest'
    $originalTime=(Get-Item -LiteralPath $large).LastWriteTimeUtc
    $bytes[0]=$bytes[0] -bxor 255
    [IO.File]::WriteAllBytes($large,$bytes)
    (Get-Item -LiteralPath $large).LastWriteTimeUtc=$originalTime
    $changed=Get-KmcDirectoryManifest $root
    Assert-HashTest ($changed.digest -cne $actual.digest -and $changed.digest -ceq (ReferenceManifest $root).digest) 'same-length mutation with restored timestamp is detected by fresh byte hashing'
    Assert-HashThrows {[KingmakerMountedCombat.RuntimeTooling.InventoryHasher]::HashFiles(@((Join-Path $root 'missing.bin')))} 'missing file fails closed'
    Assert-HashThrows {[KingmakerMountedCombat.RuntimeTooling.InventoryHasher]::HashFiles(@($root))} 'directory cannot masquerade as a file'
    $locked=[IO.File]::Open($large,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try{Assert-HashThrows {[KingmakerMountedCombat.RuntimeTooling.InventoryHasher]::HashFiles(@($large))} 'exclusive writer prevents an unverified partial inventory'}finally{$locked.Dispose()}
    $afterFailure=Get-KmcDirectoryManifest $root
    Assert-HashTest ($afterFailure.digest -ceq $changed.digest) 'failed hashing releases handles and subsequent complete scan remains exact'
    Assert-ManifestTypes $emptyDirectory 'empty manifest'
    # A root with exactly one entry catches accidental scalar unwrapping.
    $oneRoot=Join-Path $root 'one-entry'
    [void][IO.Directory]::CreateDirectory($oneRoot)
    [IO.File]::WriteAllBytes((Join-Path $oneRoot 'one.bin'),[byte[]]@(7))
    $one=Get-KmcDirectoryManifest $oneRoot
    Assert-HashTest ($one.entries.Count -eq 1 -and $one.fileCount -eq 1 -and $one.directoryCount -eq 0) 'single-entry fixture is exact'
    Assert-ManifestTypes $one 'single-entry manifest'
    Assert-ManifestTypes $actual 'many-entry manifest with both kinds'

    # File content size does not exercise canonical UTF-8 buffering. These
    # bounded Unicode names make the manifest text span several writer buffers.
    $bufferRoot=Join-Path $root 'buffers'
    [void][IO.Directory]::CreateDirectory($bufferRoot)
    $unicode=[string][char]0xE9+[char]0xD83D+[char]0xDE80
    for($index=0;$index -lt 192;$index++){
        $leaf='r{0:D3}-{1}.bin' -f $index,$unicode
        [IO.File]::WriteAllBytes((Join-Path $bufferRoot $leaf),[byte[]]@($index))
    }
    $bufferBefore=Get-KmcDirectoryManifest $bufferRoot
    $bufferReference=ReferenceManifest $bufferRoot
    $bufferRows=@($bufferReference.entries | ForEach-Object {'{0}|{1}|{2}|{3}' -f $_.kind,$_.path,$_.length,$_.sha256})
    $bufferCanonical=$bufferRows -join [char]10
    Assert-HashTest ($bufferBefore.fileCount -eq 192 -and $bufferCanonical.Length -gt (3*4096)) 'generated Unicode manifest spans more than three canonical buffers'
    Assert-HashTest ($bufferBefore.digest -ceq $bufferReference.digest) 'multi-buffer canonical digest matches independent serial oracle'
    Assert-HashTest (($bufferBefore.entries | ConvertTo-Json -Depth 5 -Compress) -ceq ($bufferReference.entries | ConvertTo-Json -Depth 5 -Compress)) 'multi-buffer Unicode entries and order match independent serial oracle'
    $tailIndex=$bufferRows.Count-1
    $prefix=($bufferRows[0..($tailIndex-1)] -join [char]10)+[char]10
    Assert-HashTest ($prefix.Length -gt (3*4096)) 'mutated final record begins after three canonical buffers'
    $tailPath=Join-Path $bufferRoot $bufferReference.entries[$tailIndex].path
    $tailTime=[IO.File]::GetLastWriteTimeUtc($tailPath)
    $tailBytes=[IO.File]::ReadAllBytes($tailPath)
    $tailBytes[0]=$tailBytes[0] -bxor 255
    [IO.File]::WriteAllBytes($tailPath,$tailBytes)
    [IO.File]::SetLastWriteTimeUtc($tailPath,$tailTime)
    $bufferAfter=Get-KmcDirectoryManifest $bufferRoot
    $tailReference=ReferenceManifest $bufferRoot
    Assert-HashTest ($bufferAfter.digest -cne $bufferBefore.digest -and $bufferAfter.digest -ceq $tailReference.digest) 'same-size tail mutation participates beyond the first canonical buffers'
    Assert-HashTest (($bufferAfter.entries | ConvertTo-Json -Depth 5 -Compress) -ceq ($tailReference.entries | ConvertTo-Json -Depth 5 -Compress)) 'tail-mutated complete entries still match serial oracle'
    $afterRows=@($tailReference.entries | ForEach-Object {'{0}|{1}|{2}|{3}' -f $_.kind,$_.path,$_.length,$_.sha256})
    Assert-HashTest ((($afterRows[0..($tailIndex-1)] -join [char]10)+[char]10) -ceq $prefix) 'all records preceding the changed tail remain exact'
    Write-Host "TOTAL PASS=$passes FAIL=0"
}
finally {
    $resolved=[IO.Path]::GetFullPath($root).TrimEnd('\')
    if(-not $resolved.StartsWith($parent+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Test cleanup escaped its owned unique root.'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
