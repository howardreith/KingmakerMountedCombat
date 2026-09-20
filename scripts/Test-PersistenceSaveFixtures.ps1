[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
$script:ownedTestLab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/persistence-fixture-tests/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { return $script:ownedTestLab }
$sourceId='owned-source'
$root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$sourceId)
$saveRoot=Join-Path $root 'Saved Games'
$evidence=Join-Path $script:ownedTestLab ('runtime-evidence/'+$sourceId)
[void][IO.Directory]::CreateDirectory($saveRoot);[void][IO.Directory]::CreateDirectory($evidence)
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p01-save';transactionToken=('a'*64)})
$resultPath=Join-Path $evidence 'runtime-result.json'
$result=[ordered]@{runId=$sourceId;scenario='persistence-p01-save';transactionToken=('a'*64);status='PASS';modsRestored=$true;workingRestored=$true}
Write-KmcJsonAtomic $resultPath $result
$fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64)};working=[pscustomobject]@{
    gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';gameName='owned fixture';area=('a'*32)}}
$path=Join-Path $saveRoot 'Manual_300_KMC_P01.zks'
Add-Type -AssemblyName System.IO.Compression
$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    $header=[ordered]@{Name='KMC_P01';Type='Manual';CompatibilityVersion=1;GameId=$fixture.working.gameId;GameName=$fixture.working.gameName;Area=$fixture.working.area}
    foreach($name in @('header.json','kmc-mounted-state')){
        $writer=[IO.StreamWriter]::new($archive.CreateEntry($name).Open())
        try{$writer.Write($(if($name-ceq'header.json'){$header|ConvertTo-Json -Compress}else{'{}'}))}finally{$writer.Dispose()}
    }
}finally{$archive.Dispose();$stream.Dispose()}
$hash=Get-KmcSha256 $path
$passes=0
function Must-Reject([scriptblock]$Action,[string]$Message){
    $rejected=$false;try{& $Action|Out-Null}catch{$rejected=$true}
    if(-not$rejected){throw $Message}
    $script:passes++
}
$read=Get-KmcPersistenceSource -SourceRunId $sourceId -ExpectedSha256 $hash -Fixture $fixture
if($read.path-cne$path-or$read.descriptor.sha256-cne$hash){throw 'Actual archive selection failed'};$passes++
Must-Reject {Get-KmcPersistenceSource '../human' $hash $fixture} 'Traversal admitted'
Must-Reject {Get-KmcPersistenceSource '..' $hash $fixture} 'Parent alias admitted'
Must-Reject {Get-KmcPersistenceSource $sourceId ('a'*64) $fixture} 'Wrong hash admitted'
$fixture.working.gameId='e3afcc98-b337-4f99-99d3-e1ab9f893bc2'
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Foreign campaign admitted'
$fixture.working.gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'
$result.status='FAIL';Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Failed native source admitted'
$result.status='PASS';$result.workingRestored=$false;Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Unrestored native source admitted'
$result.status='PASS';$result.workingRestored=$true;$result.scenario='persistence-p02-save'
Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Mismatched P02 ownership admitted'
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p02-save';transactionToken=('a'*64)})
$p02=Get-KmcPersistenceSource -SourceRunId $sourceId -ExpectedSha256 $hash -Fixture $fixture
if($p02.descriptor.sha256-cne$hash){throw 'P02 actual archive identity changed'};$passes++
$result.scenario='unrecognized-save';Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Unrecognized save scenario admitted'
if((Get-KmcSha256 $path)-cne$hash){throw 'Read-only source inspection mutated archive'};$passes++
Write-Host "PERSISTENCE OWNED FIXTURE PASS=$passes FAIL=0"
# Preserve only owned synthetic evidence in ignored obj; no external fixture touched.
