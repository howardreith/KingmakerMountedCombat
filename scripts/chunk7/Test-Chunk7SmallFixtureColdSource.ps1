[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureEvidence.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureColdSource.ps1')
$root=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261020-c7b-world70-b52s1-small'
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
$artifact=Read-KmcJson (Join-Path $root 'chunk7-small-fixture-evidence.json')
$artifactHash=Get-KmcSha256 (Join-Path $root 'chunk7-small-fixture-evidence.json')
$archiveHash=[string]$artifact.observations.nativeWrite.sha256
$pass=0;$fail=0
function Check([string]$Name,[bool]$ShouldPass,[scriptblock]$Action){
 $ok=$false;$errorText=$null
 try{&$Action;$ok=$true}catch{$errorText=$_.Exception.Message}
 if($ok-eq$ShouldPass){$script:pass++;Write-Host ('PASS '+$Name)}
 else{$script:fail++;Write-Host ('FAIL '+$Name+': '+$errorText)}
}
Check 'actual batch52 F09 native Manual and complete producer receipts' $true {
 $s=Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request
 if($s.path-cne$artifact.observations.nativeWrite.path-or$s.descriptor.sha256-cne$archiveHash-or
    $s.sourceProcessId-ne$artifact.observations.processId-or$s.nativeAuthorityGranted-ne$false-or
    $s.producerAncestry-ne$false-or$s.receiptPins.Count-ne9){throw 'F09 cold source did not preserve actual producer identity.'}
}
$mediumRoot=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world117-b112s1-medium-paid'
$mediumRequest=Read-KmcJson (Join-Path $mediumRoot 'runtime-request.json')
$mediumArtifact=Read-KmcJson (Join-Path $mediumRoot 'chunk7-small-fixture-evidence.json')
$mediumArtifactHash=Get-KmcSha256 (Join-Path $mediumRoot 'chunk7-small-fixture-evidence.json')
$mediumArchiveHash=[string]$mediumArtifact.observations.nativeWrite.sha256
Check 'actual batch112 Medium native Manual and complete producer receipts' $true {
 $s=Get-KmcChunk7SmallFixtureColdSource $mediumRequest.runId $mediumArtifactHash $mediumArchiveHash $mediumRequest.fixture $mediumRequest 'owner-seed-default-human-horse-manual'
 if($s.path-cne$mediumArtifact.observations.nativeWrite.path-or$s.descriptor.sha256-cne$mediumArchiveHash-or$s.sourceProcessId-ne$mediumArtifact.observations.processId-or$s.receiptPins.Count-ne9){throw 'Medium cold source did not preserve actual producer identity.'}
}
Check 'Medium Manual cannot impersonate Small producer case' $false {
 Get-KmcChunk7SmallFixtureColdSource $mediumRequest.runId $mediumArtifactHash $mediumArchiveHash $mediumRequest.fixture $mediumRequest
}
Check 'Small Manual cannot impersonate Medium producer case' $false {
 Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request 'owner-seed-default-human-horse-manual'
}
Check 'wrong source artifact hash' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId ('0'*64) $archiveHash $request.fixture $request}
Check 'wrong source archive hash' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash ('0'*64) $request.fixture $request}
Check 'source run traversal' $false {Get-KmcChunk7SmallFixtureColdSource '../other' $artifactHash $archiveHash $request.fixture $request}
$wrongCandidate=[pscustomobject]@{commit=$request.commit;dllSha256=('0'*64);dllMvid=$request.dllMvid}
Check 'changed candidate DLL' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $wrongCandidate}
$wrongFixture=$request.fixture|ConvertTo-Json -Depth 12|ConvertFrom-Json;$wrongFixture.working.sha256='0'*64
Check 'different protected fixture anchor' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $wrongFixture $request}
$realRead=(Get-Command Read-KmcSmallFixturePinnedJson).ScriptBlock
$script:mutate=$null
function Read-KmcSmallFixturePinnedJson([string]$Path,[string]$ExpectedHash,[int]$Maximum=16777216){
 $pin=&$realRead $Path $ExpectedHash $Maximum
 if($script:mutate){&$script:mutate $Path $pin.value}
 $pin
}
$script:mutate={param($path,$value)if((Split-Path -Leaf $path)-ceq'chunk7-small-fixture-evidence.json'){$value.observations.cleanup.sourceUnchanged=$false}}
Check 'producer cleanup did not settle' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request}
$script:mutate={param($path,$value)if((Split-Path -Leaf $path)-ceq'runtime-result.json'){$value.modsRestored=$false}}
Check 'producer Mods not restored' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request}
$script:mutate={param($path,$value)if((Split-Path -Leaf $path)-ceq'base-game-isolation-result.json'){$value.modsRestored=$false}}
Check 'base-only isolation did not restore Mods' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request}
$script:mutate={param($path,$value)if((Split-Path -Leaf $path)-ceq'profile-cache-changes.json'){$value.policy='unreviewed-profile-churn'}}
Check 'unreviewed profile-change policy' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request}
$script:mutate={param($path,$value)if((Split-Path -Leaf $path)-ceq'chunk7-small-fixture-evidence.json'){$value.observations.nativeWrite.archive.Pairs=@([pscustomobject]@{id='foreign'})}}
Check 'nonempty mounted archive disguised as F09 source' $false {Get-KmcChunk7SmallFixtureColdSource $request.runId $artifactHash $archiveHash $request.fixture $request}
$script:mutate=$null
Write-Host ('F09 cold source PASS '+$pass+' FAIL '+$fail)
if($fail-ne0){exit 1}
