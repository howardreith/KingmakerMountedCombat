param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
. (Join-Path $repoRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
function Copy-Case($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=Join-Path $labRoot 'runtime-evidence/c6a-precision-a-approach/phase3d-horse-scenario-evidence.json'
$originalHash=(Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash
$artifact=Get-Content -Raw -LiteralPath $original|ConvertFrom-Json
$proof=Copy-Case @($artifact.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
$id=$proof.identity;$approach=@($proof.samples|Where-Object boundary -CEQ 'approach-start')[0]
$initialControls=[pscustomobject]@{TargetSelectionStartCount=100;TargetSelectionEndCount=100;NativeCastRequestCount=20;NativeRefusalCount=5;DispatchAcceptedCount=7;DispatchRejectedCount=3;NativePrimaryShellPrepareCount=0}
$firstInput=[pscustomobject]@{abilityGuid='f053faad986631688defa003cd7bda0e';clickedTargetId=$id.targetId;resolvedTargetId=$id.targetId;priority='AbilityTarget';clicked=$true;targetSelectionStartDelta=1;targetSelectionEndDelta=1;nativeCastRequestDelta=1;nativeRefusalDelta=0;dispatchAcceptedDelta=0;dispatchRejectedDelta=0;nativePrimaryShellPrepareDelta=0;nativeShell=[pscustomobject]@{present=$true;inMoveSlot=$true;commandObject=$id.commandObject}}
$repeatControlsBefore=Copy-Case $initialControls
$repeatControlsBefore.TargetSelectionStartCount++;$repeatControlsBefore.TargetSelectionEndCount++;$repeatControlsBefore.NativeCastRequestCount++
$repeatControlsAfter=Copy-Case $repeatControlsBefore
$repeatControlsAfter.TargetSelectionStartCount++;$repeatControlsAfter.NativeRefusalCount++
$state=Copy-Case $approach.state
$repeatInput=[pscustomobject]@{abilityGuid='f053faad986631688defa003cd7bda0e';clickedTargetId=$id.targetId;resolvedTargetId=$id.targetId;priority='None';clicked=$false;targetSelectionStartDelta=1;targetSelectionEndDelta=0;nativeCastRequestDelta=0;nativeRefusalDelta=1;dispatchAcceptedDelta=0;dispatchRejectedDelta=0;nativePrimaryShellPrepareDelta=0;nativeShell=[pscustomobject]@{present=$true;inMoveSlot=$true;commandObject=$id.commandObject}}
$terminalControls=Copy-Case $initialControls
$terminalControls.TargetSelectionStartCount+=2;$terminalControls.TargetSelectionEndCount+=2;$terminalControls.NativeCastRequestCount++;$terminalControls.NativeRefusalCount++;$terminalControls.DispatchAcceptedCount++
$terminal=Copy-Case $proof.samples[-1].state
$case=[pscustomobject]@{
 contract='one-owned-native-mount-when-request-repeated-during-approach';start=(Copy-Case $proof.preClick.state.geometry)
 initial=[pscustomobject]@{relationshipState='Unmounted';generation=$id.generationAtInit;shellCount=10;processBindingCount=20;controls=$initialControls;ledger=(Copy-Case $proof.preClick.state.ledger)}
 firstInput=$firstInput
 trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;riderDisplacement=0.5;geometry=(Copy-Case $approach.state.geometry);commandObject=$id.commandObject;moveSlotObject=$id.commandObject;started=$false;acted=$false;finished=$false;processPresent=$false;shellOwned=$true;exactSingleRider=$true}
 repeat=[pscustomobject]@{preSelectionAvailability=[pscustomobject]@{visible=$true;enabled=$true;transitionReady=$false;reason='Approach admitted.'};availability=[pscustomobject]@{visible=$true;enabled=$false;transitionReady=$false;reason='Mount already has a pending native relationship command.'};clicked=$false;feedback='pending';input=$repeatInput;before=(Copy-Case $state);after=(Copy-Case $state);controlsBefore=$repeatControlsBefore;controlsAfter=$repeatControlsAfter;shellCountBefore=11;shellCountAfter=11;processBindingCountBefore=20;processBindingCountAfter=20;allocationEventCountBefore=55;allocationEventCountAfter=55;commandObjectBefore=$id.commandObject;moveSlotObjectBefore=$id.commandObject;moveSlotObjectAfter=$id.commandObject;sameMoveSlot=$true;firstCommandStillOwned=$true;firstCommandStarted=$false;firstCommandActed=$false;firstCommandFinished=$false;firstCommandProcessPresent=$false;selectedCountAfter=1;selectedIdsAfter=@($id.casterId);exactSingleRiderAfter=$true;rejectedBeforeSecondCommand=$true}
 commandProof=$proof;terminal=$terminal;terminalControls=$terminalControls;terminalShellCount=11;terminalProcessBindingCount=21;oneRequest=$true;oneTransition=$true
}
$script:checks=0
function Reject-Case([scriptblock]$mutate,[string]$reason){$copy=Copy-Case $case;&$mutate $copy;$rejected=$false;try{Assert-KmcRepeatedMountRequest $copy @($copy.commandProof)}catch{if($_.Exception.Message.IndexOf($reason,[StringComparison]::Ordinal)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid repeated-request proof admitted'};$script:checks++}
Assert-KmcRepeatedMountRequest $case @($case.commandProof);$script:checks++
Reject-Case {param($c)$c.contract='borrowed'} 'contract differs'
Reject-Case {param($c)$c.firstInput.nativeShell.commandObject++} 'first selected-ability input'
Reject-Case {param($c)$c.firstInput.nativeCastRequestDelta=0} 'first selected-ability input'
Reject-Case {param($c)$c.start.isAdjacent=$true} 'outside transition reach'
Reject-Case {param($c)$c.trigger.approachObserved=$false} 'lacks true approachObserved'
Reject-Case {param($c)$c.trigger.commandObject++} 'missed the exact unacted'
Reject-Case {param($c)$c.repeat.preSelectionAvailability.enabled=$false} 'pre-selection availability would invalidate'
Reject-Case {param($c)$c.repeat.availability.enabled=$true} 'availability did not name'
Reject-Case {param($c)$c.repeat.availability.reason=''} 'availability did not name'
Reject-Case {param($c)$c.repeat.clicked=$true} 'unexpected true clicked'
Reject-Case {param($c)$c.repeat.sameMoveSlot=$false} 'lacks true sameMoveSlot'
Reject-Case {param($c)$c.repeat.moveSlotObjectAfter++} 'changed exact command ownership'
Reject-Case {param($c)$c.repeat.shellCountAfter++} 'created a second shell'
Reject-Case {param($c)$c.repeat.processBindingCountAfter++} 'created a second shell'
Reject-Case {param($c)$c.repeat.allocationEventCountAfter++} 'created a second shell'
Reject-Case {param($c)$c.repeat.input.nativeCastRequestDelta=1} 'callback sequence differs'
Reject-Case {param($c)$c.repeat.input.nativeRefusalDelta=0} 'callback sequence differs'
Reject-Case {param($c)$c.repeat.input.targetSelectionEndDelta=1} 'callback sequence differs'
Reject-Case {param($c)$c.repeat.controlsAfter.DispatchAcceptedCount++} 'changed control counter'
Reject-Case {param($c)$c.repeat.controlsAfter.NativeRefusalCount--} 'lacks one exact native refusal'
Reject-Case {param($c)$c.repeat.after.ledger.acceptedMount++} 'changed relationship state or ledger'
Reject-Case {param($c)$c.repeat.after.rider.reactions--} 'changed rider reactions'
Reject-Case {param($c)$c.repeat.after.rider.reactionCooldown+=1} 'changed rider reactionCooldown'
Reject-Case {param($c)$c.repeat.after.mount.reactions--} 'changed mount reactions'
Reject-Case {param($c)$c.repeat.after.mount.reactionCooldown+=1} 'changed mount reactionCooldown'
Reject-Case {param($c)$c.repeat.after.geometry.riderPosition.x+=0.1} 'moved an actor'
Reject-Case {param($c)$c.oneRequest=$false} 'terminal did not contain exactly one request'
Reject-Case {param($c)$c.terminalControls.NativeCastRequestCount++} 'terminal did not contain exactly one request'
Reject-Case {param($c)$c.commandProof.initCount=2} 'exact acted native terminal'
Reject-Case {param($c)$c.commandProof.exactActedObserved=$false} 'exact acted native terminal'
$envelope=Copy-Case $artifact
$envelope.scenario='chunk6a-repeated-mount-request'
$envelope.rows=@($envelope.rows|Where-Object { $_.name -cin @('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing') })+@([pscustomobject]@{name='CM06-repeated-request';status='PASS';evidence=(Copy-Case $case)})
$envelope.observations|Add-Member -NotePropertyName chunk6aRepeatedRequest -NotePropertyValue (Copy-Case $case) -Force
$request=[pscustomobject]@{scenario='chunk6a-repeated-mount-request'}
Assert-KmcChunk6aCombatMountEvidence $request $envelope 'PASS';$script:checks++
$missingDisposition=Copy-Case $envelope
$missingDisposition.observations.PSObject.Properties.Remove('chunk6aAdoptionDisposition')
$rejected=$false
try{Assert-KmcChunk6aCombatMountEvidence $request $missingDisposition 'PASS'}catch{if($_.Exception.Message-cne'Positive Mount evidence omitted its declared adoption disposition.'){throw};$rejected=$true}
if(-not$rejected){throw 'Repeated-request envelope without adoption disposition was admitted'};$script:checks++
$invalidDisposition=Copy-Case $envelope
$invalidDisposition.observations.chunk6aAdoptionDisposition.disposition='Unavailable'
$rejected=$false
try{Assert-KmcChunk6aCombatMountEvidence $request $invalidDisposition 'PASS'}catch{if($_.Exception.Message-cne'Positive Mount evidence did not declare RealTimeOwnership for its real-time native command.'){throw};$rejected=$true}
if(-not$rejected){throw 'Repeated-request envelope with invalid RT adoption disposition was admitted'};$script:checks++
if((Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash-cne$originalHash){throw 'Original artifact changed'}
Write-Host "REPEATED REQUEST EVIDENCE PASS=$script:checks FAIL=0; synthetic contract only, offline regression, no native qualification."
