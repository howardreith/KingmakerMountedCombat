[CmdletBinding()] param()
# F6 pinned post-fall source: the real immutable r03 receipts (read-only) admit exactly that producer with its identical
# DLL; every other failed or mismatched source is still refused by the unchanged source gate.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime\Chunk7PersistenceSaveFixtures.ps1')
$passes=0
function Check([bool]$Condition,[string]$Message){ if(-not$Condition){throw ('FAIL '+$Message)}; $script:passes++; Write-Host ('PASS '+$Message) }
function Refuses([scriptblock]$Body,[string]$Message){ $threw=$false; try{ & $Body }catch{ $threw=$true }; Check $threw $Message }
$lab=Get-KmcLabRoot
$run='20261107-finalize-f6c-producer-03'; $sha='b33eda46d65ce242e77a7cb105e8daf18f07134459f59a954025460b93ac2edc'
$dll='b18cc0560b98d213abd542a48e0d7781eb1859ff86298cee16a8030ea72d7742'; $case='rider-fall-integration-manual'
$evidence=Join-Path $lab ('runtime-evidence\'+$run)
if(-not(Test-Path -LiteralPath $evidence)){ Write-Host 'SKIP the pinned source evidence is not present on this machine'; Write-Host 'TOTAL PASS=0 FAIL=0'; return }
$fixture=(Read-KmcJson (Join-Path $evidence 'runtime-request.json')).fixture
$later='9999999999999999999999999999999999999999'
function Source([string]$Run,[string]$Sha,[string]$Case,[string]$Dll,[string]$Commit){ Get-KmcChunk7PersistenceSource -SourceRunId $Run -ExpectedSha256 $Sha -Fixture $fixture -Case $Case -ExpectedDllSha256 $Dll -ExpectedCommit $Commit }

Check (Test-KmcFinalize6PinnedFallSource $run $sha $case) 'the pin names exactly the r03 run, Manual and case'
Check (-not(Test-KmcFinalize6PinnedFallSource $run $sha 'companion-names-tack-manual') -and -not(Test-KmcFinalize6PinnedFallSource $run ('0'*64) $case) -and
    -not(Test-KmcFinalize6PinnedFallSource ($run+'x') $sha $case)) 'any other run, Manual or case is not the pin'
$s=Source $run $sha $case $dll $later
Check ($s.descriptor.sha256 -ceq $sha -and $s.descriptor.length -eq 857840 -and $s.descriptor.fileName -ceq 'Manual_300_KMC_C7_WORLD.zks') 'the pinned producer is admitted on a later commit with its identical DLL, after its receipts and corrected re-evaluation'
Refuses { Source $run $sha $case ('a'*64) $later } 'the pinned producer is refused for another DLL'
Refuses { Source $run ('c'*64) $case $dll $later } 'the r03 run under another Manual hash falls back to the ordinary gate and is refused as a failed source'
$r06='20261107-finalize-f5g-producer-06'
if(Test-Path -LiteralPath (Join-Path $lab ('runtime-evidence\'+$r06))){
    Refuses { Get-KmcChunk7PersistenceSource -SourceRunId $r06 -ExpectedSha256 'c673673c44cd2dcb3665fdde5cb8141ddd9434cd4f3ced3b03fca7ba4d8f3f1e' -Fixture $fixture -Case 'companion-names-tack-manual' -ExpectedDllSha256 '8459126ce033443bda4fad9a4444984fa3217ca528aad63d723a5818ee8f9361' -ExpectedCommit 'fd8bb82018d70c93732c7a1bedcf4a9a60c2c2af' } 'another reader-only outer FAIL (F5 r06) is still refused: no general failed-source route'
}
# The receipt pins themselves: a changed single error or digest is refused.
$q=Read-KmcJson (Join-Path $evidence 'runtime-request.json'); $res=Read-KmcJson (Join-Path $evidence 'runtime-result.json')
$iso=Read-KmcJson (Join-Path $evidence 'base-game-isolation-result.json'); $game=Read-KmcJson (Join-Path $evidence 'runtime-game-result.json')
Assert-KmcFinalize6PinnedFallSourceReceipts $evidence $q $res $iso $game $dll; Check $true 'the receipts, single preserved error and native PASS hold'
$bad=$res|ConvertTo-Json -Depth 20|ConvertFrom-Json; $bad.errors=@('another failure')
Refuses { Assert-KmcFinalize6PinnedFallSourceReceipts $evidence $q $bad $iso $game $dll } 'a different recorded failure is refused'
$badGame=$game|ConvertTo-Json -Depth 20|ConvertFrom-Json; $badGame.status='FAIL'
Refuses { Assert-KmcFinalize6PinnedFallSourceReceipts $evidence $q $res $iso $badGame $dll } 'a native FAIL is refused'
Write-Host ('TOTAL PASS='+$passes+' FAIL=0')
