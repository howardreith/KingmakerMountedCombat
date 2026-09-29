param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
function Copy-Ownership($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=Join-Path $lab 'runtime-evidence/c6a-envelope-a-obstruction/phase3d-horse-scenario-evidence.json';$originalHash=(Get-FileHash $original).Hash
$artifact=Get-Content -Raw $original|ConvertFrom-Json;$proof=Copy-Ownership $artifact.observations.chunk6aObstruction.commandProof
$proof.contract='unacted-native-ownership-loss-no-cost-or-transition';$terminal=$proof.samples[-1]
$geometry=Copy-Ownership $proof.preClick.state.geometry
$dx=[double]$geometry.horsePosition.x-[double]$geometry.riderPosition.x;$dz=[double]$geometry.horsePosition.z-[double]$geometry.riderPosition.z;$length=[Math]::Sqrt($dx*$dx+$dz*$dz)
$geometry.riderPosition.x=[double]$geometry.riderPosition.x+0.5*$dx/$length;$geometry.riderPosition.z=[double]$geometry.riderPosition.z+0.5*$dz/$length
$geometry.horizontalDistance=$length-0.5;$geometry.centerDistance=$length-0.5;$terminal.state.geometry=Copy-Ownership $geometry
$group=[pscustomobject]@{id='party';object=910;members=@([pscustomobject]@{id=$proof.identity.casterId;object=201},[pscustomobject]@{id=$proof.identity.targetId;object=202})}
$faction=[pscustomobject]@{guid='faction-guid';object=920};$attacks=@([pscustomobject]@{guid='enemy-guid';object=921})
$before=[pscustomobject]@{riderId=$proof.identity.casterId;riderObject=201;mountId=$proof.identity.targetId;mountObject=202;
 riderPetId=$proof.identity.targetId;riderPetObject=202;riderPetUniqueId=$proof.identity.targetId;masterId=$proof.identity.casterId;masterObject=201;mountIsPet=$true;
 riderGroup=(Copy-Ownership $group);mountGroup=(Copy-Ownership $group);riderFaction=(Copy-Ownership $faction);mountFaction=(Copy-Ownership $faction);
 riderFactionAttackSource=(Copy-Ownership $attacks);mountAttackFactions=(Copy-Ownership $attacks);mountAttackFactionsPlayerEnemy=$false;
 riderPlayerFaction=$true;mountPlayerFaction=$true;weather=[pscustomobject]@{present=$true;ownerMatches=$true;ownerObject=930;lastBuffPresent=$false;lastBuffObject=0}}
$detached=Copy-Ownership $before;$detached.riderPetId=$null;$detached.riderPetObject=0;$detached.riderPetUniqueId=$null;$detached.masterId=$null;$detached.masterObject=0;$detached.mountIsPet=$false
$detached.weather=[pscustomobject]@{present=$false;ownerMatches=$false;ownerObject=0;lastBuffPresent=$false;lastBuffObject=0}
$method=[pscustomobject]@{declaringType='Kingmaker.UnitLogic.UnitDescriptor';method='SetMaster';token='06001F17';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}
$command=[pscustomobject]@{id=$proof.identity.commandObject;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';executor=$proof.identity.casterId;started=$false;acted=$false;finished=$false;result='None'}
$case=[pscustomobject]@{contract='native-ownership-loss-during-exact-mount-approach';noResidue=$true;restored=$true;commandTerminalBeforeRestoration=$true;inputsUnchangedAfterDetach=$true;
 stimulusCount=1;restorationCount=1;diagnosticInterruptCount=0;nativeMethod=(Copy-Ownership $method);commandProof=$proof;ownershipBefore=$before;ownershipAfterDetach=$detached;
 ownershipBeforeRestoration=(Copy-Ownership $detached);ownershipAfterRestoration=(Copy-Ownership $before);
 trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;gameTicks=$terminal.gameTicks;frame=$terminal.frame;commandObject=$proof.identity.commandObject;moveSlotObject=$proof.identity.commandObject;started=$false;acted=$false;finished=$false;geometry=$geometry;riderDisplacement=0.5};
 stimulus=[pscustomobject]@{contract='one-native-unit-descriptor-set-master-null';method=(Copy-Ownership $method);before=(Copy-Ownership $before);after=(Copy-Ownership $detached);commandBefore=(Copy-Ownership $command);commandAfter=(Copy-Ownership $command);frameBefore=$terminal.frame;frameAfter=$terminal.frame;gameTicksBefore=$terminal.gameTicks;gameTicksAfter=$terminal.gameTicks;count=1};
 restoration=[pscustomobject]@{cleanup=$false;before=(Copy-Ownership $detached);vacantReciprocalReferences=$true;inputsUnchanged=$true;method=(Copy-Ownership $method);attempted=$true;count=1;after=(Copy-Ownership $before);pass=$true}}
$script:checks=0
function Reject-Ownership([scriptblock]$mutate,[string]$reason){$copy=Copy-Ownership $case;&$mutate $copy;$rejected=$false;try{Assert-KmcOwnershipChange $copy}catch{if($_.Exception.Message.IndexOf($reason,[StringComparison]::Ordinal)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid ownership proof admitted'};$script:checks++}
Assert-KmcOwnershipChange $case;$script:checks++

# Exercise the same external full-scenario dispatcher that validates runtime-result.json.
# Ownership is an isolated unacted command and therefore owns only the two exploration windows.
$envelope=Copy-Ownership $artifact
$envelope.rows=@($envelope.rows|Where-Object {$_.name -cne 'CM02-obstruction'})+@([pscustomobject]@{name='CM02-ownership-change';status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()})
[void]$envelope.observations.PSObject.Properties.Remove('chunk6aDoorFixture')
[void]$envelope.observations.PSObject.Properties.Remove('chunk6aObstruction')
$envelope.observations|Add-Member -NotePropertyName chunk6aOwnershipChange -NotePropertyValue (Copy-Ownership $case) -Force
Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-ownership-change'}) $envelope 'PASS';$script:checks++
foreach($field in @('noResidue','restored','commandTerminalBeforeRestoration','inputsUnchangedAfterDetach')){Reject-Ownership {param($c)$c.$field=$false} 'undeclared, unrestored'}
foreach($field in @('stimulusCount','restorationCount')){Reject-Ownership {param($c)$c.$field++} 'one stimulus'}
Reject-Ownership {param($c)$c.diagnosticInterruptCount=1} 'zero measured interrupts'
foreach($field in @('declaringType','method','token','moduleMvid')){Reject-Ownership {param($c)$c.nativeMethod.$field='wrong'} 'another native method'}
Reject-Ownership {param($c)$c.commandProof.contract='unacted-native-stop-no-cost-or-transition'} 'Missing unacted ownership-loss contract'
Reject-Ownership {param($c)$c.commandProof.samples[3].identity.commandObject++} 'Mixed ownership command identity'
Reject-Ownership {param($c)$e=Copy-Ownership $c.commandProof.resourceWindow.events[0];$e.boundary='cost-before';$e.state.actor=$c.commandProof.identity.casterId;$c.commandProof.resourceWindow.events+=@($e)} 'native cost'
Reject-Ownership {param($c)$c.trigger.riderDisplacement=0} 'measured rider approach displacement'
Reject-Ownership {param($c)$c.trigger.acted=$true} 'pending native approach'
Reject-Ownership {param($c)$c.ownershipBefore.riderPetId='other'} 'Original reciprocal ownership differs'
Reject-Ownership {param($c)$c.ownershipBefore.mountGroup.object++} 'Original group identity differs'
Reject-Ownership {param($c)$c.ownershipBefore.mountFaction.object++} 'Original faction identity differs'
Reject-Ownership {param($c)$c.ownershipBefore.mountAttackFactions[0].object++} 'attack-faction rebuild source differs'
Reject-Ownership {param($c)$c.ownershipBefore.weather.lastBuffPresent=$true;$c.ownershipBefore.weather.lastBuffObject=999} 'weather-part/null-buff precondition differs'
Reject-Ownership {param($c)$c.ownershipAfterDetach.masterId='rider'} 'clear exact reciprocal references'
Reject-Ownership {param($c)$c.ownershipAfterDetach.mountGroup.object++} 'restoration input'
Reject-Ownership {param($c)$c.ownershipAfterDetach.weather.present=$true} 'weather-part state'
Reject-Ownership {param($c)$c.ownershipBeforeRestoration.mountFaction.object++} 'between detach terminal'
Reject-Ownership {param($c)$c.ownershipAfterRestoration.mountIsPet=$false} 'restore every captured side effect'
Reject-Ownership {param($c)$c.stimulus.count=2} 'stimulus boundary differs'
Reject-Ownership {param($c)$c.stimulus.commandAfter.acted=$true} 'exact pending Mount'
Reject-Ownership {param($c)$c.restoration.vacantReciprocalReferences=$false} 'restoration boundary differs'
Reject-Ownership {param($c)$c.restoration.cleanup=$true} 'restoration boundary differs'

# Source, protocol and qualification registration contracts.
if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq'chunk6a-ownership-change'}).Count-ne1){throw 'Ownership scenario registration missing or duplicate'};$script:checks++
if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq'CM02-ownership-change'}).Count-ne1){throw 'Ownership mandatory row missing or duplicate'};$script:checks++
$binding=[pscustomobject]@{scenario='chunk6a-ownership-change';rows=@('CM02-ownership-change');evidenceLeaf='phase3d-horse-scenario-evidence.json'}
if(-not(Assert-KmcIsolatedScenarioRows 'CM02-ownership-change' $binding)){throw 'Ownership isolated mapping missing'};$script:checks++
Assert-KmcChunk6aPrimaryClaim 'CM02-ownership-change' $binding;$script:checks++
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aOwnershipChangeScenario.cs')
if([regex]::Matches($source,'horse[.]Descriptor[.]SetMaster[(]null[)];').Count-ne1-or[regex]::Matches($source,'horse[.]Descriptor[.]SetMaster[(]rider[)];').Count-ne1){throw 'Ownership stimulus/restoration call count differs'};$script:checks++
$selection=$source.IndexOf('EnsureChunk6aRiderSelection(Chunk6aOwnershipRow)');$baseline=$source.IndexOf('chunk6aOwnershipOriginal = CaptureChunk6aOwnership()');$probe=$source.IndexOf('new NativeRelationshipCommandProbe');$click=$source.IndexOf('TryNativeAbilityTargetClick')
if($selection-lt0-or$selection-ge$baseline-or$baseline-ge$probe-or$probe-ge$click){throw 'Ownership selection/baseline/click order differs'};$script:checks++
if($source.IndexOf('FinishUnacted("unacted-native-ownership-loss-no-cost-or-transition")')-ge$source.IndexOf('RestoreChunk6aOwnership(false)')){throw 'Ownership restoration precedes measured terminal'};$script:checks++
$tick=$source.Substring(0,$source.IndexOf('private void CleanupChunk6aOwnershipChange()'))
if($tick.Contains('.Interrupt()')){throw 'Ownership measured window contains a diagnostic interrupt'};$script:checks++
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
if($tranche.IndexOf('CleanupChunk6aOwnershipChange();')-gt$tranche.IndexOf('chunk6aCommandWindow?.Dispose()')){throw 'Ownership abort restoration lost its exact command observer'};$script:checks++
if((Get-FileHash $original).Hash-cne$originalHash){throw 'Original obstruction artifact changed'}
Write-Host "OWNERSHIP PASS=$script:checks FAIL=0; synthetic/source contracts only, no native qualification."
