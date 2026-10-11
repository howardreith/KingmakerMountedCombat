$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6bFixtureEvidence.ps1')
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk6bFixtureData.ps1')
$script:passed=0
$origin=[pscustomobject]@{x=0.0;y=0.0;z=0.0}
function Accept([string]$label,[scriptblock]$body){&$body;$script:passed++;Write-Host ('PASS '+$label)}
function Reject([string]$label,[scriptblock]$mutation,[bool]$moved=$true){
 $p=New-ChargeFixtureReturn 'case' $moved;&$mutation $p
 $failed=$false;try{Assert-KmcChargeFixtureReturn $p $origin 'case'}catch{$failed=$true}
 if(-not$failed){throw ('Accepted invalid fixture: '+$label)};$script:passed++;Write-Host ('PASS refuses '+$label)
}
Accept 'unchanged original staging point needs no input' {Assert-KmcChargeFixtureReturn (New-ChargeFixtureReturn) $origin 'case'}
Accept 'mounted native return has exact command/path and no cost or preparation' {Assert-KmcChargeFixtureReturn (New-ChargeFixtureReturn 'case' $true) $origin 'case'}
Reject 'accumulated drift disguised as no input' {param($p)$p.before.mountPosition.x=7.0} $false
Reject 'different original destination' {param($p)$p.destination.x=7.0}
Reject 'failed native arrival' {param($p)$p.after.mountPosition.x=0.169}
Reject 'teleport with no input' {param($p)$p.before.mountPosition.x=4.0} $false
Reject 'foreign pair selected' {param($p)$p.selectedIds=@('foreign')}
Reject 'foreign command' {param($p)$p.terminalCommand.executor='rider'}
Reject 'command identity exchanged' {param($p)$p.admittedCommand.id=334}
Reject 'native move did not finish' {param($p)$p.terminalCommand.finished=$false}
Reject 'native interruption presented as success' {param($p)$p.terminalCommand.result='Interrupt'}
Reject 'non-player command' {param($p)$p.createdByPlayer=$false}
Reject 'cost observed on another command' {param($p)$p.resourceWindow.groundCommand.commandObject=334}
Reject 'clock window exchanged' {param($p)$p.resourceWindow.before.frame=1}
Reject 'refund during fixture return' {param($p)$p.resourceWindow.after.rider.standard=0.0}
Reject 'new Prepare during fixture return' {param($p)$p.resourceWindow.events[4].boundary='prepare-before'}
Reject 'mount cost during fixture return' {param($p)$p.resourceWindow.after.mount.move=2.0}
Reject 'path completion absent' {param($p)$p.path.events=@($p.path.events|Where-Object boundary -CNE 'path-complete-after')}
Reject 'foreign path actor' {param($p)$p.path.events[1].actorId='rider'}
Reject 'path observer failed' {param($p)$p.path.complete=$false}
foreach($field in @('inCombat','nativeTurnBased','controllerInitialized','chargeOwned','commandActive','mountMoving','mountPathPresent','mountCharging')){
 Reject ('unsettled '+$field) {param($p)$p.after.$field=$true}
}
Reject 'charge speed override remains' {param($p)$p.after.speedOverride=8.0}
Reject 'relationship generation changed' {param($p)$p.after.generation=2}
Write-Host ('TOTAL PASS='+$script:passed+' FAIL=0')
