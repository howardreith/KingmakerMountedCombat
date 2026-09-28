param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPausedQueueEvidence.ps1')
$paths=[xml](Get-Content -Raw (Join-Path $repoRoot 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePausedMountEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-Paused($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
$resource=[pscustomobject]@{standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0;initiativeCooldown=0.0;initiativeOrder=7;reactionsPerRound=1;nativePrepareCount=1}
$position=[pscustomobject]@{x=1.0;y=2.0;z=3.0}
$identity=[pscustomobject]@{commandObject=101;controlIdentity='shell:3:mount:rider';casterId='rider';targetId='mount';generationAtInit=1;commandType='Move';abilityGuid='f053faad986631688defa003cd7bda0e';processObject=0;contextObject=0}
$sample=[pscustomobject]@{frame=100;gameTicks=1000000L;paused=$true;turnBased=$false;allocationSequence=10;traceComplete=$true;inMoveSlot=$true;createdByPlayer=$false;started=$false;acted=$false;finished=$false;
 state=[pscustomobject]@{rider=$resource;mount=(Copy-Paused $resource);selectedIds=@('rider');ledger=[pscustomobject]@{forcedDetach=1;duplicateSuppressed=1;acceptedMount=1};relationshipState='Unmounted';generation=1;geometry=[pscustomobject]@{riderPosition=$position;horsePosition=(Copy-Paused $position)}}}
foreach($p in $identity.PSObject.Properties){$sample|Add-Member -NotePropertyName $p.Name -NotePropertyValue $p.Value}
$samples=@(for($i=0;$i -lt 11;$i++){$s=Copy-Paused $sample;$s.frame+=$i;$s})
$released=Copy-Paused $samples[-1];$released.paused=$false
$hold=[pscustomobject]@{contract='one-native-rt-mount-held-paused-then-unpaused-once';samples=$samples;events=@();beforeUnpause=(Copy-Paused $samples[-1]);afterUnpause=$released;unpauseCount=1;complete=$true}
$proof=[pscustomobject]@{pass=$true;identity=(Copy-Paused $identity);resourceWindow=[pscustomobject]@{events=@()};samples=@([pscustomobject]@{boundary='approach-start';frame=111;gameTicks=1000001L})}
$proof|Add-Member -NotePropertyName preClick -NotePropertyValue ([pscustomobject]@{state=(Copy-Paused $sample.state);gameTicks=$sample.gameTicks;frame=$sample.frame;allocationSequence=$sample.allocationSequence})
$proof.samples+=@([pscustomobject]@{boundary='click-admission';frame=$sample.frame;gameTicks=$sample.gameTicks;allocationSequence=$sample.allocationSequence})
$proof.identity.processObject=201;$proof.identity.contextObject=202
$script:checks=0
function Check-Paused($h,$p,[bool]$expected){
 $producer=$true
 try{$args=[object[]]::new(2);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($h|ConvertTo-Json -Depth 50 -Compress));$args[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 50 -Compress));$null=$method.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcPausedQueue $h $p}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Paused producer/external result differs: '+$producer+'/'+$external+' expected '+$expected)};$script:checks+=2
}
function Reject-Paused([scriptblock]$mutate){$h=Copy-Paused $hold;$p=Copy-Paused $proof;& $mutate $h $p;Check-Paused $h $p $false}
Check-Paused $hold $proof $true
foreach($flag in @('paused','traceComplete','inMoveSlot')){Reject-Paused {param($h,$p)$h.samples[5].$flag=$false}}
foreach($flag in @('turnBased','started','acted','finished','createdByPlayer')){Reject-Paused {param($h,$p)$h.samples[5].$flag=$true}}
foreach($field in @('commandObject','processObject','contextObject','generationAtInit','gameTicks')){Reject-Paused {param($h,$p)$h.samples[5].$field++}}
foreach($field in @('controlIdentity','casterId','targetId','commandType','abilityGuid')){Reject-Paused {param($h,$p)$h.samples[5].$field='foreign'}}
foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
 Reject-Paused {param($h,$p)$h.samples[5].state.$actor.$field++}
 Reject-Paused {param($h,$p)$h.samples[5].state.$actor.$field=$null}
}}
Reject-Paused {param($h,$p)$h.samples[5].state.selectedIds=@('mount')}
Reject-Paused {param($h,$p)$h.samples[5].state.selectedIds=@('rider','mount')}
Reject-Paused {param($h,$p)$h.samples[5].state.ledger.forcedDetach++}
Reject-Paused {param($h,$p)$h.samples[5].state.generation++}
Reject-Paused {param($h,$p)$h.samples[5].state.relationshipState='Mounted'}
foreach($actor in @('riderPosition','horsePosition')){foreach($axis in @('x','y','z')){Reject-Paused {param($h,$p)$h.samples[5].state.geometry.$actor.$axis+=0.01}}}
Reject-Paused {param($h,$p)$h.samples[5].frame=$h.samples[4].frame}
Reject-Paused {param($h,$p)$h.samples=@($h.samples|Select-Object -First 10)}
Reject-Paused {param($h,$p)$h.beforeUnpause.commandObject++}
Reject-Paused {param($h,$p)$h.afterUnpause.paused=$true}
Reject-Paused {param($h,$p)$h.afterUnpause.frame++}
Reject-Paused {param($h,$p)$h.unpauseCount=2}
Reject-Paused {param($h,$p)$h.contract='other'}
Reject-Paused {param($h,$p)$h.afterUnpause.allocationSequence++}
Reject-Paused {param($h,$p)$h.samples[5].allocationSequence=9}
Reject-Paused {param($h,$p)$h.samples[5].allocationSequence=11}
foreach($field in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid')){Reject-Paused {param($h,$p)if($p.identity.$field -is [string]){$p.identity.$field='foreign'}else{$p.identity.$field++}}}
Reject-Paused {param($h,$p)$p.pass=$false}
Reject-Paused {param($h,$p)$p.samples[0].frame=110}
Reject-Paused {param($h,$p)$p.samples[0].gameTicks=999999L}
foreach($boundary in @('cost-before','actor-cost-after','prepare-before','clear-before','opportunity-before')){
 Reject-Paused {param($h,$p)$h.events=@([pscustomobject]@{sequence=11;boundary=$boundary;state=[pscustomobject]@{actor='rider'}});$h.afterUnpause.allocationSequence=11;$p.resourceWindow.events=$h.events}
}
$h=Copy-Paused $hold;$p=Copy-Paused $proof;$h.events=@([pscustomobject]@{sequence=11;boundary='movement-tick';state=[pscustomobject]@{actor='rider'}});$p.resourceWindow.events=@(Copy-Paused $h.events);$h.afterUnpause.allocationSequence=11
Check-Paused $h $p $true
$p.resourceWindow.events=@();Check-Paused $h $p $false
Reject-Paused {param($h,$p)$p.preClick.state.rider.reactions--}
Reject-Paused {param($h,$p)$p.preClick.state.mount.reactionCooldown++}
Reject-Paused {param($h,$p)$p.preClick.gameTicks--}
Reject-Paused {param($h,$p)$p.preClick.frame++}
Reject-Paused {param($h,$p)$p.preClick.allocationSequence++}
foreach($field in @('frame','gameTicks','allocationSequence')){Reject-Paused {param($h,$p)$p.samples[1].$field++}}
Write-Output ('PAUSED QUEUE PASS='+$script:checks+' FAIL=0; shared producer/external synthetic checks only')
