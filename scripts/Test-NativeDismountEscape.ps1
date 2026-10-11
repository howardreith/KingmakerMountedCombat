param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativeDismountEscapeEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeDismountEscapeEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')
function Copy-Escape($x){$x|ConvertTo-Json -Depth 80|ConvertFrom-Json}
$ledger=[pscustomobject]@{admittedMount=2;acceptedMount=2;admittedDismount=1;acceptedDismount=1;refusedVoluntary=0;forcedDetach=3;duplicateSuppressed=2;concurrentSuppressed=0}
function New-Boundary($snapshot,[bool]$movement,[bool]$paired,[string]$state='Mounted'){
 $r=Copy-Escape $snapshot.rider;$m=Copy-Escape $snapshot.mount
 $r|Add-Member -NotePropertyName nativePrepareCount -NotePropertyValue $r.grantSequence;$m|Add-Member -NotePropertyName nativePrepareCount -NotePropertyValue $m.grantSequence
 [pscustomobject]@{frame=$snapshot.frame;gameTicks=$snapshot.gameTicks;allocationSequence=$snapshot.allocationSequence;
 state=[pscustomobject]@{rider=$r;mount=$m;selectedIds=@('rider');ledger=(Copy-Escape $ledger);relationshipState=$state;generation=2;
 geometry=[pscustomobject]@{seconds=0.01;shellState=('lastShellRefusal=<none>;mountAbilityFactPresent='+$movement.ToString()+';registeredShells=0')}};
 riderResources=(Copy-Escape $snapshot.rider);mountResources=(Copy-Escape $snapshot.mount);
 settings=[pscustomobject]@{movement=$movement;paired=$paired;legacyUnified=$false;legacyScheduler=$false;overlay=$false};
 abilityPresent=$true;abilityObject=301;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';abilityCasterId='rider';visible=$true;enabled=$true;reason='';pairIdle=$true;inCombat=$true;turnBased=$false}
}
function New-Proof([bool]$Dismount,$before,$terminal){
 $delta=Copy-Escape $ledger;foreach($f in $delta.PSObject.Properties.Name){$delta.$f=0}
 if($Dismount){$delta.admittedDismount=1;$delta.acceptedDismount=1}else{$delta.admittedMount=1;$delta.acceptedMount=1}
 $guid=if($Dismount){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'}
 $target=if($Dismount){'rider'}else{'mount'};$offset=if($Dismount){10}else{0};$gen=if($Dismount){2}else{1}
 [pscustomobject]@{pass=$true;identityComplete=$true;sameCommandAtEveryBoundary=$true;exactActedObserved=$true;nativeTerminal=$true;nativeResult='Success';initCount=1;errors=@();
 mountId='mount';identity=[pscustomobject]@{commandObject=(101+$offset);processObject=(201+$offset);contextObject=(301+$offset);controlIdentity=('shell-'+$offset);generationAtInit=$gen;casterId='rider';targetId=$target;commandType='Move';abilityGuid=$guid};
 resourceWindow=[pscustomobject]@{pass=$true;reactionResources=[pscustomobject]@{pass=$true}};ledgerDelta=$delta;
 preClick=[pscustomobject]@{frame=$before.frame;gameTicks=$before.gameTicks;allocationSequence=$before.allocationSequence;state=(Copy-Escape $before.state)};
 samples=@([pscustomobject]@{boundary='terminal';state=(Copy-Escape $terminal.state)})}
}
function Add-EscapeBridge($Proof,$Boundary,$Hooks){
 $terminal=@($Proof.samples|Where-Object boundary -CEQ 'terminal')[0]
 foreach($field in @('frame','gameTicks','allocationSequence')){$terminal|Add-Member $field $Boundary.$field -Force}
 foreach($pair in @(@('identity',(Copy-Escape $Proof.identity)),@('acted',$true),@('finished',$true),@('processEnded',$true),@('result','Success'),@('simulatingClick',$false),@('nativeAllocation',([pscustomobject]@{rider=(Copy-Escape $Boundary.riderResources);mount=(Copy-Escape $Boundary.mountResources)})))){$terminal|Add-Member $pair[0] $pair[1] -Force}
 $snapshot=[pscustomobject]@{frame=$Boundary.frame;gameTicks=$Boundary.gameTicks;allocationSequence=$Boundary.allocationSequence;rider=(Copy-Escape $Boundary.riderResources);mount=(Copy-Escape $Boundary.mountResources)}
 [pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';riderId='rider';mountId='mount';turnBased=$false;traceComplete=$true;observerHooks=@($Hooks);commandIdentity=(Copy-Escape $Proof.identity);before=$snapshot;after=(Copy-Escape $snapshot);events=@()}
}
function New-Escape([string]$Case='feature'){
 $w=New-Passive;$w|Add-Member -NotePropertyName pass -NotePropertyValue $true;$w.after.frame=110
 $before=New-Boundary $w.before $true $true;$disabled=New-Boundary $w.before ($Case -ceq 'policy') ($Case -ceq 'feature')
 $click=New-Boundary $w.after ($Case -ceq 'policy') ($Case -ceq 'feature')
 $last=Copy-Escape $w.after;$last.frame++;$last.gameTicks+=10000L;$last.allocationSequence+=4;$last.rider.move+=3.0
 $after=New-Boundary $last ($Case -ceq 'policy') ($Case -ceq 'feature') 'Unmounted';$after.state.ledger.admittedDismount++;$after.state.ledger.acceptedDismount++
 $restored=Copy-Escape $after;$restored.settings.movement=$true;$restored.settings.paired=$true
 if($Case-ceq'feature'){$restored.state.geometry.shellState=$restored.state.geometry.shellState.Replace(';mountAbilityFactPresent=False;',';mountAbilityFactPresent=True;')}
 $e=[pscustomobject]@{contract='settled-positive-rt-mount-disabled-setting-native-dismount';case=$Case;scenario=('chunk6a-dismount-'+$Case+'-disabled-rt');
 mountProof=(New-Proof $false $before $before);beforeDisable=$before;afterDisable=$disabled;beforeClick=$click;waitResources=$w;
 clicked=$true;input=[pscustomobject]@{clicked=$true;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';clickedTargetId='rider';resolvedTargetId='rider'};
 dismountProof=(New-Proof $true $click $after);afterDismount=$after;restored=$restored}
 $e|Add-Member mountTerminalBridge (Add-EscapeBridge $e.mountProof $before $w.observerHooks)
 $e|Add-Member dismountTerminalBridge (Add-EscapeBridge $e.dismountProof $after $w.observerHooks)
 $e
}
$script:checks=0
function Check-Escape($e,[bool]$expected){
 $producer=$true;try{$a=[object[]]::new(1);$a[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 80 -Compress));$null=$method.Invoke($null,$a)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcDismountEscape $e}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Escape producer/external '+$producer+'/'+$external+' expected '+$expected)};$script:checks+=2
}
$p=New-Escape;Check-Escape $p $true;Check-Escape (New-Escape 'policy') $true
function Reject-Escape([scriptblock]$mutate){$x=Copy-Escape $p;& $mutate $x;try{Check-Escape $x $false}catch{throw ('Mutation '+$mutate.ToString()+': '+$_)}}
foreach($b in @('beforeDisable','afterDisable','beforeClick','afterDismount','restored')){
 foreach($f in @('pairIdle','inCombat')){Reject-Escape {param($x)$x.$b.$f=$false}}
 Reject-Escape {param($x)$x.$b.turnBased=$true}
 Reject-Escape {param($x)$x.$b.state.generation++}
 Reject-Escape {param($x)$x.$b.state.selectedIds=@('mount')}
 foreach($actor in @('rider','mount')){foreach($f in @('standard','move','swift','reactions','reactionCooldown','reactionsPerRound','initiativeCooldown','initiativeOrder','nativePrepareCount')){Reject-Escape {param($x)$x.$b.state.$actor.$f++}}}
 foreach($f in @('legacyUnified','legacyScheduler','overlay')){Reject-Escape {param($x)$x.$b.settings.$f=$true}}
}
foreach($b in @('beforeDisable','afterDisable','beforeClick')){
 foreach($f in @('abilityPresent','visible')){Reject-Escape {param($x)$x.$b.$f=$false}}
 foreach($f in @('abilityObject','abilityGuid','abilityCasterId')){Reject-Escape {param($x)$x.$b.$f=$null}}
 Reject-Escape {param($x)$x.$b.state.ledger.forcedDetach++}
}
foreach($b in @('afterDisable','beforeClick','afterDismount')){Reject-Escape {param($x)$x.$b.settings.movement=$true};Reject-Escape {param($x)$x.$b.settings.paired=$false}}
Reject-Escape {param($x)$x.restored.settings.movement=$false}
Reject-Escape {param($x)$x.restored.state.geometry.shellState=$x.afterDismount.state.geometry.shellState}
Reject-Escape {param($x)$x.restored.state.geometry.shellState+=';registeredShells=1'}
Reject-Escape {param($x)$x.afterDismount.state.geometry.shellState+=';mountAbilityFactPresent=False;';$x.restored.state.geometry.shellState=$x.afterDismount.state.geometry.shellState.Replace(';mountAbilityFactPresent=False;',';mountAbilityFactPresent=True;')}
Reject-Escape {param($x)$x.afterDismount.state.geometry.shellState+=';mountAbilityFactPresent=True;';$x.restored.state.geometry.shellState=$x.afterDismount.state.geometry.shellState.Replace(';mountAbilityFactPresent=False;',';mountAbilityFactPresent=True;')}
$clockOnly=Copy-Escape $p;$clockOnly.restored.state.geometry.seconds+=0.001;Check-Escape $clockOnly $true
Reject-Escape {param($x)$x.beforeClick.enabled=$false}
Reject-Escape {param($x)$x.beforeClick.frame=109}
Reject-Escape {param($x)$x.afterDisable.frame++}
Reject-Escape {param($x)$x.waitResources.pass=$false}
Reject-Escape {param($x)$x.waitResources.events[0].boundary='clear-before'}
Reject-Escape {param($x)$x.waitResources.events[0].boundary='cost-before'}
foreach($actor in @('rider','mount')){foreach($f in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){Reject-Escape {param($x)$x.waitResources.after.$actor.$f++}}}
foreach($name in @('mountProof','dismountProof')){
 foreach($f in @('pass','identityComplete','sameCommandAtEveryBoundary','exactActedObserved','nativeTerminal')){Reject-Escape {param($x)$x.$name.$f=$false}}
 foreach($f in @('commandObject','processObject','contextObject')){Reject-Escape {param($x)$x.$name.identity.$f=0}}
 Reject-Escape {param($x)$x.$name.resourceWindow.reactionResources.pass=$false}
 Reject-Escape {param($x)$x.$name.initCount=2}
 Reject-Escape {param($x)$x.$name.errors=@('missing callback')}
 Reject-Escape {param($x)$x.$name.ledgerDelta.acceptedMount++}
}
Reject-Escape {param($x)$x.clicked=$false}
Reject-Escape {param($x)$x.input.clicked=$false}
Reject-Escape {param($x)$x.input.clickedTargetId='mount'}
Reject-Escape {param($x)$x.dismountProof.identity.commandObject=$x.mountProof.identity.commandObject}
Reject-Escape {param($x)$x.dismountProof.preClick.state.rider.reactions++}
Reject-Escape {param($x)$x.afterDismount.state.ledger.acceptedDismount++}
foreach($name in @('mountTerminalBridge','dismountTerminalBridge')){
 Reject-Escape {param($x)$x.$name=$null}
 foreach($role in @('rider','mount')){foreach($f in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){Reject-Escape {param($x)$x.$name.after.$role.$f++}}}
}
# Only diagnostic wall seconds may differ between consecutive captures.
$x=Copy-Escape $p
foreach($b in @('beforeClick','afterDismount','restored')){$x.$b.state.geometry|Add-Member riderPosition ([pscustomobject]@{x=1;y=0;z=0});$x.$b.state.geometry.seconds=1.0}
$x.dismountProof.preClick.state.geometry|Add-Member riderPosition ([pscustomobject]@{x=1;y=0;z=0});$x.dismountProof.preClick.state.geometry.seconds=1.1
$x.restored.state.geometry.seconds=1.2;Check-Escape $x $true
$x.restored.state.geometry.riderPosition.x++;Check-Escape $x $false
Write-Output ('DISMOUNT ESCAPE PRODUCER+EXTERNAL PASS='+$script:checks+' FAIL=0; synthetic model only; no native qualification')
