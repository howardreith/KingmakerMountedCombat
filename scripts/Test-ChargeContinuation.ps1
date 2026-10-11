param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/ChargeContinuationEvidence.ps1')
. (Join-Path $PSScriptRoot 'Test-ChargeContinuationData.ps1')
$checks=0
function Pass([string]$Name,[scriptblock]$Body){& $Body;$script:checks++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Change){
 $f=New-ChargeContinuationFixture;& $Change $f
 $failed=$false;try{Assert-KmcChargeContinuation $f.proof $f.queued $f.done}catch{$failed=$true}
 if(-not$failed){throw ('Accepted '+$Name)};$script:checks++;Write-Host ('PASS refuses '+$Name)
}
Pass 'two ordinary attacks follow actual native debt expiry and native costs' {
 $f=New-ChargeContinuationFixture;Assert-KmcChargeContinuation $f.proof $f.queued $f.done
}
Pass 'rounded clock prediction cannot reject legitimate float cooldown expiry' {
 $f=New-ChargeContinuationFixture 3.58069754 0.0164
 $expiry=@($f.proof.trace.events|Where-Object {$_.boundary-ceq'round-state-before'-and$_.state.actor-ceq'rider'})[0]
 if(($expiry.gameTicks-$f.proof.before.gameTicks)/10000000.0+0.01-ge$f.proof.before.rider.standard){throw 'Counterexample no longer distinguishes the historical prediction'}
 Assert-KmcChargeContinuation $f.proof $f.queued $f.done
}
Pass 'mount ordinary full attacks pay their own Standard once after first delivery' {
 $f=New-ChargeContinuationFixture -MountAttacks
 if(@($f.proof.attacks|Where-Object actor -CEQ 'mount').Count-ne12){throw 'Missing four complete mount attack observations'}
 Assert-KmcChargeContinuation $f.proof $f.queued $f.done
}
Pass 'pending mount initiative decays through exact native ticks without a preparation grant' {
 $f=New-ChargeContinuationFixture -MountInitiative 3.25 -MountAttacks
 Assert-KmcChargeContinuation $f.proof $f.queued $f.done
}
function RejectPair([string]$Name,[scriptblock]$Change){
 $f=New-ChargeContinuationFixture -MountInitiative 3.25 -MountAttacks;& $Change $f
 $failed=$false;try{Assert-KmcChargeContinuation $f.proof $f.queued $f.done}catch{$failed=$true}
 if(-not$failed){throw ('Accepted '+$Name)};$script:checks++;Write-Host ('PASS refuses '+$Name)
}
RejectPair 'initiative refund inside an ordinary tick' {param($f)
 @($f.proof.trace.events|Where-Object {$_.state.actor-ceq'mount'-and$_.boundary-ceq'cooldown-tick-after'})[0].state.initiativeCooldown=0
}
RejectPair 'initiative decay attributed to action debt' {param($f)
 @($f.proof.trace.events|Where-Object {$_.state.actor-ceq'mount'-and$_.boundary-ceq'cooldown-tick-after'})[0].state.standard=1
}
RejectPair 'unobserved initiative grant at the endpoint' {param($f)$f.proof.after.mount.initiativeCooldown=3;$f.proof.after.mount.waitingInitiative=$true}
RejectPair 'mount ordinary command charges the rider' {param($f)
 @($f.proof.trace.events|Where-Object {$_.state.actor-ceq'mount'-and$_.boundary-ceq'cost-before'})[0].commandActor='rider'
}
RejectPair 'mount first attack has already fabricated its acted transition' {param($f)
 @($f.proof.trace.events|Where-Object {$_.state.actor-ceq'mount'-and$_.boundary-ceq'continuation-attack-before'})[0].acted=$true
}
RejectPair 'duplicate full-attack member' {param($f)
 foreach($a in @($f.proof.attacks|Where-Object {$_.actor-ceq'mount'-and$_.rule-eq411})){$a.attackNumber=0}
}
RejectPair 'full-attack count changes between callbacks' {param($f)
 @($f.proof.attacks|Where-Object actor -CEQ 'mount')[1].attacksCount=3
}
RejectPair 'mount cost disappears after delivery' {param($f)
 @($f.proof.trace.events|Where-Object {$_.state.actor-ceq'mount'-and$_.boundary-ceq'cost-before'})[0].boundary='omitted'
}
RejectPair 'second mount command reuses the first paid identity' {param($f)
 foreach($e in @($f.proof.trace.events|Where-Object command -EQ 302)){$e.command=301}
}
RejectPair 'mount continuation resumes Charge' {param($f)@($f.proof.attacks|Where-Object actor -CEQ 'mount')[0].charge=$true}
Reject 'unknown observer status' {param($f)$f.proof.closed=$null}
Reject 'old hash-only observer contract' {param($f)$f.proof.contract='native-rt-charge-continuation-v1'}
Reject 'identity collision detected by the producer' {param($f)$f.proof.trace.identityRegistry.faults=1}
Reject 'identity references left alive after closure' {param($f)$f.proof.trace.identityRegistry.retainedCount=1}
Reject 'identity registry never closed' {param($f)$f.proof.trace.identityRegistry.released=$false}
Reject 'identity release status inferred from a string' {param($f)$f.proof.trace.identityRegistry.released='true'}
Reject 'duplicate identity label in the retained set' {param($f)$f.proof.trace.identityRegistry.ids+=@(1)}
Reject 'nonnull reference labeled as null' {param($f)$f.proof.trace.identityRegistry.ids+=@(0)}
Reject 'unretained actor reference' {param($f)$f.proof.trace.identityRegistry.ids=@($f.proof.trace.identityRegistry.ids|Where-Object {$_-ne1})}
Reject 'unretained native rule reference' {param($f)$f.proof.trace.identityRegistry.ids=@($f.proof.trace.identityRegistry.ids|Where-Object {$_-ne201})}
Reject 'unretained native command reference' {param($f)$f.proof.trace.identityRegistry.ids=@($f.proof.trace.identityRegistry.ids|Where-Object {$_-ne101})}
Reject 'identity capacity differs' {param($f)$f.proof.trace.identityRegistry.capacity=16001}
Reject 'distinct rider rounds alias one command identity' {param($f)
 foreach($e in @($f.proof.trace.events|Where-Object command -EQ 102)){$e.command=101}
}
Reject 'missing exact native tick hook' {param($f)$f.proof.trace.observerHooks=@($f.proof.trace.observerHooks|Where-Object token -CNE '0600934A')}
Reject 'overflowed trace' {param($f)$f.proof.trace.dropped=1}
Reject 'observer exception' {param($f)$f.proof.errors=@('native callback observation failed')}
Reject 'actual refund inside native tick' {param($f)$f.proof.trace.events[1].state.standard=0}
Reject 'tick arithmetic differs' {param($f)$f.proof.trace.events[0].gameDeltaTime=0.5}
Reject 'unobserved tick entry' {param($f)$f.proof.trace.events[0].boundary='omitted'}
Reject 'unobserved tick exit' {param($f)$f.proof.trace.events[1].boundary='omitted'}
Reject 'forged preparation' {param($f)$f.proof.trace.events[1].state.grantSequence=1}
Reject 'native clear replay' {param($f)$f.proof.trace.events[0].boundary='clear-before'}
Reject 'round before native zero' {param($f)$f.proof.trace.events[1].boundary='round-state-before'}
Reject 'missing round end' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'round-state-after')[0].boundary='omitted'}
Reject 'second actor substituted' {param($f)$f.proof.trace.events[1].state.actorObject=2}
Reject 'native cost omitted' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'cost-after')[0].boundary='omitted'}
Reject 'extra Standard charge' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'cost-after')[0].state.standard=12}
Reject 'direct TB actor cost in RT' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'cost-before')[0].boundary='actor-cost-before'}
Reject 'cost paid by another command' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'cost-after')[0].command=999}
Reject 'wrong native command family' {param($f)@($f.proof.trace.events|Where-Object boundary -CEQ 'cost-before')[0].commandType='foreign'}
Reject 'duplicate attack callback' {param($f)$f.proof.attacks[1].boundary='attack-before';$f.proof.trace.events[$f.proof.attacks[1].allocationSequence-1].boundary='continuation-attack-before'}
Reject 'foreign target' {param($f)$f.proof.attacks[0].target='foreign'}
Reject 'charge resumed on continuation' {param($f)$f.proof.attacks[0].charge=$true}
Reject 'unobserved attack rule object' {param($f)$f.proof.attacks[0].rule=999}
Reject 'missing attack completion' {param($f)$f.proof.attacks=@($f.proof.attacks|Select-Object -Skip 1)}
Reject 'unexplained terminal debt' {param($f)$f.proof.after.rider.standard=0}
Write-Host ('CHARGE CONTINUATION PASS='+$checks+' FAIL=0; synthetic only')
