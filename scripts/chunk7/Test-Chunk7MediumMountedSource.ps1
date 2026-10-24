[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7MediumMountedSource.ps1')

$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world118-b114s2-medium-cold/runtime-request.json')
$consumer=[pscustomobject]@{commit='cd1f3714483f7c1b36a6fea26bfb6adcf27afae9';
 dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}
$pass=0;$fail=0
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $actual=$false;$detail=$null
 try{&$Action;$actual=$true}catch{$detail=$_.Exception.Message}
 if($actual-eq$Expected){$script:pass++;Write-Host ('PASS '+$Name)}
 else{$script:fail++;Write-Host ('FAIL '+$Name+': '+$detail)}
}
Check 'exact paid Medium producer plus separate cold PASS admitted as fixture ancestry only' $true {
 $source=Get-KmcChunk7MediumMountedSource $request.fixture $consumer
 Assert-KmcBootstrapInventoryString $source.sourceRunId '20261021-c7b-world118-b114s1-medium-paid'
 Assert-KmcBootstrapInventoryString $source.coldRunId '20261021-c7b-world118-b114s2-medium-cold'
 Assert-KmcBootstrapInventoryString $source.descriptor.sha256 'd25aa931174027b1f1f661db8f5a0db3054fcbe89e930ada1747f327e41a65e3'
 Assert-KmcBootstrapInventoryString $source.descriptor.internalName 'KMC_C7_MEDIUM_FIXTURE'
 Assert-KmcChunk7Boolean $source.fixtureSetupOnly $true
 Assert-KmcChunk7Boolean $source.producerAncestry $false
 Assert-KmcChunk7Boolean $source.sameCandidate $false
 Assert-KmcChunk7Boolean $source.nativeAuthorityGranted $false
 if(@($source.coldReceiptPins).Count-lt5){throw 'Medium cold receipt pins were not retained.'}
}
Check 'malformed consumer identity refused before admission' $false {
 $bad=[pscustomobject]@{commit='not-a-commit';dllSha256=$consumer.dllSha256;dllMvid=$consumer.dllMvid}
 [void](Get-KmcChunk7MediumMountedSource $request.fixture $bad)
}
Check 'foreign protected fixture identity refused' $false {
 $bad=$request.fixture|ConvertTo-Json -Depth 10|ConvertFrom-Json
 $bad.working.sha256='0'*64
 [void](Get-KmcChunk7MediumMountedSource $bad $consumer)
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail)
if($fail-ne0){exit 1}
