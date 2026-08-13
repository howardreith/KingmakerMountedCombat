[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf=[bool]$WhatIfPreference;$WhatIfPreference=$false
$repoRoot=Get-KmcRepositoryRoot;$labRoot=Get-KmcLabRoot;$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$stateRoot=Join-Path $labRoot 'runtime-state';$backupRoot=Join-Path $labRoot 'runtime-backups';$stagingRoot=Join-Path $labRoot 'runtime-staging';$liveMods=[string]$intake.requestedLayout.kingmakerModsRoot
Assert-KmcNoGameProcesses
$lockPath=Join-Path $stateRoot 'active-transaction.lock';if(-not(Test-Path -LiteralPath $lockPath)){throw 'No stale KMC runtime transaction exists.'}
$raw=Read-KmcJson $lockPath;$statePath=Get-KmcTransactionStatePath $stateRoot ([string]$raw.runId)
if(-not(Test-Path -LiteralPath $statePath)){throw 'Stale lock has no matching durable transaction state; automatic recovery is unsafe.'}
$before=Get-KmcDirectoryManifest $liveMods
$WhatIfPreference=$requestedWhatIf
if(-not$PSCmdlet.ShouldProcess($liveMods,"recover exact KMC transaction $($raw.runId)")){$WhatIfPreference=$false;if((Get-KmcDirectoryManifest $liveMods).digest-cne$before.digest){throw 'Recovery WhatIf purity failed.'};Write-Host 'Recovery WhatIf purity PASS; no external state changed.';return}
$WhatIfPreference=$false
$lock=$null
try{$lock=Adopt-KmcStaleRuntimeLock $stateRoot;$restored=Restore-KmcModsTransaction -Lock $lock -StatePath $statePath -LiveModsRoot $liveMods -BackupRoot $backupRoot -StagingRoot $stagingRoot;Close-KmcRuntimeLock $lock;$lock=$null;Write-Host "PASS recovered transaction $($raw.runId); live Mods digest=$($restored.digest)"}
finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
