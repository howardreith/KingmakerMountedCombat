param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-NativeJointGround.ps1') -JointFunctionsOnly -Configuration $Configuration
$script:checks=0
$proof=[pscustomobject]@{identity=@{casterId='rider';generationAtInit=1};mountId='mount'}
function New-GroundEnvelope([bool]$Joint){
 if($Joint){$fixture=New-JointFixture;$ground=$fixture.rider;$events=@($fixture.joint.ground.resourceWindow.events)+@($ground.resourceWindow.events)}
 else{$ground=New-GroundTransaction;$events=$ground.resourceWindow.events}
 $observation=[pscustomobject]@{chunk6aPreCombatPositioning=$ground;actorAllocationTrace=@{dropped=0;observationErrors=0;observerHooks=$ground.resourceWindow.observerHooks;events=$events}}
 if($Joint){$observation|Add-Member chunk6aPreCombatJointPositioning $fixture.joint};$observation
}
function Check-Envelope($Observation,[bool]$Expected){
 $pass=$true;try{Assert-KmcChunk6aGroundSetupEnvelope $Observation $proof}catch{$pass=$false;if($Expected){throw}}
 if($pass-ne$Expected){throw ('Ground envelope accepted corruption; expected '+$Expected)};$script:checks++
}
foreach($joint in @($false,$true)){
 $p=New-GroundEnvelope $joint;Check-Envelope $p $true
 function Reject-Envelope([scriptblock]$Mutation){$x=Copy-GroundPlan $p;& $Mutation $x;try{Check-Envelope $x $false}catch{throw ('Mutation '+$Mutation.ToString()+': '+$_)}}
 Reject-Envelope {param($x)$x.actorAllocationTrace.dropped=1}
 Reject-Envelope {param($x)$x.actorAllocationTrace.observationErrors=1}
 Reject-Envelope {param($x)$x.actorAllocationTrace.observationErrors=@()}
 Reject-Envelope {param($x)$x.actorAllocationTrace.observationErrors='0'}
 Reject-Envelope {param($x)$x.actorAllocationTrace.observerHooks=@()}
 Reject-Envelope {param($x)$x.actorAllocationTrace.events=@()}
 Reject-Envelope {param($x)$x.actorAllocationTrace.events[0].sequence++}
 Reject-Envelope {param($x)$x.chunk6aPreCombatPositioning.before.generation++}
 foreach($actor in @('rider','mount')){
  foreach($field in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){
   Reject-Envelope {param($x)@($x.actorAllocationTrace.events|Where-Object {$_.state.actor-ceq$actor})[-1].state.$field++}
   Reject-Envelope {param($x)$x.chunk6aPreCombatPositioning.resourceWindow.after.$actor.$field++}
  }
 }
}
$nativePath=Join-Path $repo '../../runtime-evidence/c6a-pointer-a-mammoth-tb/phase3d-horse-scenario-evidence.json'
if((Get-FileHash -LiteralPath $nativePath).Hash.ToLowerInvariant()-cne'941b1e0c2f9e4026c7706c64c9be352e5fa293165fa51f0b6964256dd9c86246'){throw 'Immutable native trace schema fixture changed'}
$native=Get-Content -Raw -LiteralPath $nativePath|ConvertFrom-Json
$p=New-GroundEnvelope $false;$p.actorAllocationTrace.dropped=$native.observations.actorAllocationTrace.dropped;$p.actorAllocationTrace.observationErrors=$native.observations.actorAllocationTrace.observationErrors
Check-Envelope $p $true
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aPreCombatPositioning.cs')
$planner=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativePreCombatGroundPlan.cs')
$causal=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aCausalEvidence.cs')
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
function Require-Source([bool]$Ok,[string]$Reason){if(-not$Ok){throw $Reason};$script:checks++}
Require-Source ($source.Contains('manager.SelectUnit(actor.View, true, true, false)')-and$source.Contains('selected.Count == 1 && selected[0] == actor')) 'Ground mover selection must call the exact native single-unit API and verify the exact actor'
$begin=$source.Substring($source.IndexOf('private void BeginChunk6aGroundPositioning'),$source.IndexOf('private bool FinishChunk6aGroundPositioning')-$source.IndexOf('private void BeginChunk6aGroundPositioning'))
Require-Source ($begin.IndexOf('Chunk6aPositioningSelection(mover)')-lt$begin.IndexOf('CaptureChunk6aCausalState()')-and$begin.IndexOf('CaptureChunk6aCausalState()')-lt$begin.IndexOf('new NativeOutsideCombatGroundProbe')-and$begin.IndexOf('new NativeOutsideCombatGroundProbe')-lt$begin.IndexOf('ClickGroundHandler.MoveSelectedUnitsToPoint')) 'Exact selection, baseline, resources and native input order changed'
Require-Source ($causal.Contains('if (!Chunk6aTurnBased && !EnsureChunk6aRiderSelection("CM01-combat-mount-setup")) return;')) 'TB polling must not change selection during the exact mount ground command'
Require-Source ($source.Contains('initialRiderFailure')-and$source.Contains('actualRiderSearchFailure')-and$source.Contains('var plan = FindChunk6aPreCombatPosition();')-and$source.Contains('NativePreCombatGroundEvidence.AssertJoint')) 'Original failures or actual post-arrival replanning is missing'
Require-Source ($planner.Contains('SearchLimit = 72')-and$planner.Contains('new[] { 2.3f, 2.65f, 2f }')-and$planner.Contains('index < 24')-and$planner.Contains('NativeGroundFixturePolicy.IsPreCombatPosition')) 'Bounded plan changed its original proposal count or geometry policy'
Require-Source ($source-notmatch '\.Translocate\(|\.Position\s*=|\.Prepare\(|Cooldown.*=|ForceToEnd'-and$planner-notmatch '\.Translocate\(|\.Position\s*=|\.Prepare\(|Cooldown.*=|ForceToEnd') 'Ground fixture introduced an actor or resource write'
Require-Source ($tranche.Contains('try { CleanupChunk6aPreCombatPositioning(); }')-and$source.Contains('finally { RestoreChunk6aGroundObservation(); }')-and$source.Contains('chunk6aPositioningCommand?.Interrupt()')) 'Owned command or observation restoration is not on the cleanup path'
Require-Source ($source.Contains('NativePreCombatGroundEvidence.AssertTransaction(chunk6aPositioningEvidence')-and$source.Contains('DiagnosticPlacementTolerance')) 'Producer transaction or unchanged arrival guard is missing'
$common=Get-Content -Raw (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
Require-Source ($common.Contains('Assert-KmcChunk6aGroundSetupEnvelope $observations $found[0]')) 'Mandatory TB envelope bypasses ground resource and causal evidence'
'NATIVE GROUND FULL ENVELOPE PASS='+$checks+' FAIL=0; synthetic contracts and source bindings only'
