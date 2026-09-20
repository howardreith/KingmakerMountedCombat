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
    Write-Host "TOTAL PASS=$passes FAIL=0"
}
finally {
    $resolved=[IO.Path]::GetFullPath($root).TrimEnd('\')
    if(-not $resolved.StartsWith($parent+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Test cleanup escaped its owned unique root.'}
    if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
