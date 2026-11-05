[CmdletBinding()] param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot
$source=Join-Path $lab 'analysis-cache\finalize-f1-contracts\scroll-fix\KingmakerLevelUpSelectionScrollFix'
$manifest=Get-KmcRetainedDependencyManifest $source
$row=[ordered]@{id='KingmakerLevelUpSelectionScrollFix';version='1.0.0';sourceRunId='owner-handoff-finalize-f1';sourcePath=$source;dllName='KingmakerLevelUpSelectionScrollFix.dll';dllSha256=Get-KmcSha256 (Join-Path $source 'KingmakerLevelUpSelectionScrollFix.dll');digest=$manifest.digest;fileCount=[int]$manifest.fileCount}
$selection=[ordered]@{contract='owner-handoff-finalize-f1-scroll-v1';dependencies=@($row)}
$path=Join-Path $lab ('analysis-cache\finalize-f1-contracts\staging-test-'+[Guid]::NewGuid().ToString('N')+'.json')
$passes=0
function Write-Selection { $selection|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $path -Encoding UTF8 }
function Refused([string]$label) {
    Write-Selection
    $refused=$false
    try { $null=Read-KmcRetainedDependencySelection $path } catch { $refused=$true }
    if(!$refused){throw "Accepted invalid input: $label"}
    $script:passes++; Write-Host "PASS $label"
}
try {
    Write-Selection
    $actual=@(Read-KmcRetainedDependencySelection $path)
    if($actual.Count-ne1-or$actual[0].dllSha256-cne$row.dllSha256){throw 'Exact supplied input not admitted'}
    $passes++; Write-Host 'PASS exact supplied read-only scroll-fix input admitted'
    $selection.contract='retained-dependency-staging-v1'; Refused 'owner download cannot masquerade as a producer run'; $selection.contract='owner-handoff-finalize-f1-scroll-v1'
    $row.sourceRunId='some-other-run'; Refused 'wrong provenance refused'; $row.sourceRunId='owner-handoff-finalize-f1'
    $old=$row.dllSha256; $row.dllSha256='0'*64; Refused 'wrong DLL identity refused'; $row.dllSha256=$old
    $row.fileCount++; Refused 'folder mutation refused'; $row.fileCount--
    $old=$row.sourcePath; $row.sourcePath=Join-Path $lab 'handoffs'; Refused 'different input root refused'; $row.sourcePath=$old
    $row.id='KingmakerMountedCombat'; Refused 'reserved mod cannot enter compatibility input'; $row.id='KingmakerLevelUpSelectionScrollFix'
    Write-Host "TOTAL PASS=$passes FAIL=0"
} finally { if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path} }
. (Join-Path $PSScriptRoot 'runtime\FinalizeF1LaunchAdmission.ps1')
$record=[pscustomobject]@{budgetId='finalize-f1';entries=@([pscustomobject]@{launch=$true;actualGameStartAttemptUtc='2026-11-04T00:00:00Z'},[pscustomobject]@{launch=$true;actualGameStartAttemptUtc='2026-11-04T00:01:00Z'},[pscustomobject]@{launch=$true;status='failed';seconds=17})}
Assert-KmcFinalizeF1LaunchAllowance $record $false
$record.entries+= [pscustomobject]@{launch=$true;status='failed';actualGameStartAttemptUtc='2026-11-04T00:02:00Z'}
Assert-KmcFinalizeF1LaunchAllowance $record $false
Write-Host 'PASS three attempts admit the authorized fourth launch'
$record.entries+= [pscustomobject]@{launch=$true;status='failed';actualGameStartAttemptUtc='2026-11-04T00:03:00Z'}
$refused=$false; try { Assert-KmcFinalizeF1LaunchAllowance $record $false } catch { $refused=$true }; if(!$refused){throw 'Fifth F1 launch admitted'}
Write-Host 'PASS four attempts including failure refuse a fifth launch'
Assert-KmcFinalizeF1LaunchAllowance $record $true
Write-Host 'PASS WhatIf remains available without a new launch'
