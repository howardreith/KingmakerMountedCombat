[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$')][string]$SourceRunId,
    [Parameter(Mandatory=$true)][string]$SelectionPath,
    [string]$RepositoryRoot
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$reader=Join-Path $RepositoryRoot 'scripts/runtime'
. (Join-Path $reader 'RuntimeHarness.Common.ps1')
. (Join-Path $reader 'Chunk7ManualDerivedFixtures.ps1')
. (Join-Path $reader 'Chunk7AcquiredManualFixtures.ps1')
$admission=Read-KmcChunk7AcquiredManualSelectionFile $SelectionPath 'qualifier-test-acquired'
if($SourceRunId-cne$admission.selection.sourceRunId){throw 'Requested source run differs from the selected acquisition receipt.'}
$evidenceRoot=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$SourceRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'owner-field source evidence'
Assert-KmcNotReparsePoint $evidenceRoot 'owner-field source evidence'
$requestPath=Join-Path $evidenceRoot 'runtime-request.json'
Assert-KmcNotReparsePoint $requestPath 'owner-field source request'
Assert-KmcNotHardLink $requestPath 'owner-field source request'
$request=Read-KmcJson $requestPath
$candidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}
function Copy-Selection {$admission.selection|ConvertTo-Json -Depth 20|ConvertFrom-Json}
$passed=0
function Accept([string]$Name,[scriptblock]$Action){&$Action;$script:passed++;Write-Host ('PASS '+$Name)}
function Refuse([string]$Name,[string]$ExpectedMessage,[scriptblock]$Action){
 $rejected=$false
 try{&$Action|Out-Null}catch{
  if($_.Exception.Message-cne$ExpectedMessage-or$_.ScriptStackTrace-notmatch'Get-KmcChunk7AcquiredManualSource'){throw}
  $rejected=$true
 }
 if(-not$rejected){throw ('FAIL accepted '+$Name)}
 $script:passed++;Write-Host ('PASS refuses '+$Name)
}
Accept 'actual completed owner field Manual and receipt' {
 $source=Get-KmcChunk7AcquiredManualSource $admission.selection $request.fixture $candidate
 if($source.descriptor.fileName-cne'Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks'-or
    $source.descriptor.sha256-cne$admission.selection.sourceArchiveSha256-or
    $source.acquisitionRunId-cne$SourceRunId){throw 'Owner-field source lost exact native Manual lineage.'}
}
Refuse 'source archive hash drift' 'Bootstrap inventory exact scalar identity differs.' {$s=Copy-Selection;$s.sourceArchiveSha256='0'*64;Get-KmcChunk7AcquiredManualSource $s $request.fixture $candidate}
Refuse 'candidate MVID drift' 'Bootstrap inventory exact scalar identity differs.' {$wrong=[pscustomobject]@{commit=$candidate.commit;dllSha256=$candidate.dllSha256;dllMvid='11111111-1111-4111-8111-111111111111'};Get-KmcChunk7AcquiredManualSource $admission.selection $request.fixture $wrong}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
