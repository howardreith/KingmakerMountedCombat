param([string]$ReaderDirectory,[string]$FixtureDirectory=$PSScriptRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $FixtureDirectory 'Initialize-Chunk7Tests.ps1')
if([string]::IsNullOrEmpty($ReaderDirectory)){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
. (Join-Path $FixtureDirectory 'Test-Chunk7PersistenceSource.ps1')
. (Join-Path $ReaderDirectory 'Chunk7PersistenceEvidence.ps1')
. (Join-Path $ReaderDirectory 'Chunk7PersistenceSaveFixtures.ps1')
$typedPassed=0;$typedFailed=0
function Expect-Rejected([string]$Name,$Context){$caught=$false;try{Read-TestSource $Context|Out-Null}catch{$caught=$true};if($caught){$script:typedPassed++;Write-Host ('PASS refuses '+$Name)}else{$script:typedFailed++;Write-Host ('FAIL accepted '+$Name)}}
$c=New-TestSource 'tp';Read-TestSource $c|Out-Null;$typedPassed++;Write-Host 'PASS actual ZIP and exact typed restoration receipts'
foreach($field in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){foreach($form in @('text','number','single-array')){$c=New-TestSource ('t'+[Guid]::NewGuid().ToString('N').Substring(0,8));$c.result.$field=switch($form){'text'{'true'};'number'{1};'single-array'{,@($true)}};Pin-TestReceipts $c;Expect-Rejected ($field+' '+$form) $c}}
foreach($field in @('assertionFailCount','subscenarioFailCount')){foreach($form in @('text','boolean','single-array')){$c=New-TestSource ('t'+[Guid]::NewGuid().ToString('N').Substring(0,8));$c.game.$field=switch($form){'text'{'0'};'boolean'{$false};'single-array'{,@(0)}};Pin-TestReceipts $c;Expect-Rejected ($field+' '+$form) $c}}
$c=New-TestSource 'th';$header=[pscustomobject]@{Name='KMC_C7_WORLD';Type='Manual';CompatibilityVersion='1';GameId=$c.request.fixture.working.gameId;GameName=$c.request.fixture.working.gameName;Area=$c.request.fixture.working.area};Write-TestZip $c.path $header $c.artifact.observations.nativeWrite.archive;$c.sha=Get-KmcSha256 $c.path;$c.artifact.observations.nativeWrite.sha256=$c.sha;$c.artifact.observations.nativeWrite.length=[long](Get-Item -LiteralPath $c.path).Length;Pin-TestReceipts $c;Expect-Rejected 'numeric text native header version with exact repinned archive' $c
Write-Host ('CHUNK7 SOURCE RECEIPT TYPES PASS='+$typedPassed+' FAIL='+$typedFailed+'; actual private filesystem/ZIP and completed-source intake')
if($typedFailed-ne0){throw 'Source receipt type coercion admitted.'}
