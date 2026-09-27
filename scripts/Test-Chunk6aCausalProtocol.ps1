[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$script:passed=0
function Copy-Value($value){$value|ConvertTo-Json -Depth 80 -Compress|ConvertFrom-Json}
function Test-Case([string]$name,[scriptblock]$body){& $body;$script:passed++;Write-Host "PASS $name"}
function Reject([scriptblock]$body,[string]$reason){
    $caught=$false
    try{& $body}catch{if($_.Exception.Message -notlike ('*'+$reason+'*')){throw};$caught=$true}
    if(-not $caught){throw "Validator accepted corrupt evidence: $reason"}
}
function New-CommandProof([bool]$combat=$true,[bool]$tb=$false,[int]$partner=0){
    $identity=@{commandObject=101;controlIdentity='shell-1';processObject=201;contextObject=301
        casterId='rider';targetId='mount';generationAtInit=4;commandType='Move';abilityGuid='mount-guid'}
    $state=@{generation=4;selectedIds=@('rider');rider=@{standard=4.0;move=1.0;swift=3.0;initiative=0};mount=@{standard=5.0;move=4.0;swift=3.0;initiative=0}}
    $pre=@{gameTicks=1000000000;state=$state}
    $before=@{boundary='cost-before';command=101;commandActor='rider';actionType='Move';acted=$true;timeSinceStart=0.25;gameTicks=1010000000
        state=@{actor='rider';inCombat=$combat;standard=4.0;move=1.0;swift=3.0}}
    if($combat -and -not $tb){$before.state.standard=3.0;$before.state.move=0.0;$before.state.swift=2.0}
    $after=Copy-Value $before;$after.boundary='cost-after'
    if($combat){$after.state.move=if($tb){4.0}else{2.75}}
    $end=Copy-Value $state
    if($combat -and -not $tb){foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift')){$end.$actor.$field=[Math]::Max(0,$end.$actor.$field-2)}}}
    $end.rider.move=if(-not $combat){1.0}elseif($tb){4.0}else{1.75}
    $samples=@()
    foreach($name in @('init','click-admission','move-slot-installation','approach-start','process-binding','acted','cost-before','cost-after','deliver','relationship-transition','terminal')){
        $id=Copy-Value $identity
        if($name -cin @('init','click-admission','move-slot-installation','approach-start')){$id.processObject=0;$id.contextObject=0}
        $samples+=@{boundary=$name;identity=$id;acted=($name -cin @('acted','cost-before','cost-after','deliver','relationship-transition','terminal'))
            nativeProcessBinding=($name -ceq 'process-binding');deliveryContext=$(if($name -ceq 'deliver'){301}else{0})
            gameTicks=$(if($name -ceq 'terminal'){1020000000}else{1000000000});state=$(if($name -ceq 'terminal'){$end}else{$state})
            finished=($name -ceq 'terminal');processEnded=($name -ceq 'terminal');result='Success'}
    }
    $events=@($before)
    if($combat -and $tb){$nb=Copy-Value $before;$nb.boundary='actor-cost-before';$na=Copy-Value $after;$na.boundary='actor-cost-after';$events+=@($nb,$na)}
    $events+=@($after)
    if($partner -eq 1){
        foreach($name in @('prepare-before','clear-before','clear-after','prepare-after')){
            $events+=@{boundary=$name;state=@{actor='mount';standard=0.0;move=0.0;swift=0.0};preparingTurn=12;gameTicks=1010000000}
        }
        foreach($field in @('standard','move','swift')){$end.mount.$field=0.0}
    }
    Copy-Value @{pass=$true;identityComplete=$true;sameCommandAtEveryBoundary=$true;exactActedObserved=$true
        nativeTerminal=$true;traceComplete=$true;initCount=1;errors=@();nativeResult='Success';identity=$identity;mountId='mount'
        preClick=$pre;samples=$samples;resourceWindow=@{inCombat=$combat;turnBased=$tb;events=$events}}
}
Test-Case 'one RT command with normal decay' {Assert-KmcRelationshipCommandProof (New-CommandProof) $true $false 0 $true}
Test-Case 'one TB command with exact endpoints' {Assert-KmcRelationshipCommandProof (New-CommandProof $true $true) $true $true 0 $true}
Test-Case 'one declared pending-partner grant' {Assert-KmcRelationshipCommandProof (New-CommandProof $true $true 1) $true $true 1 $true}
Test-Case 'exploration callback writes no native cost' {Assert-KmcRelationshipCommandProof (New-CommandProof $false) $false $false 0 $false}
foreach($field in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid')){
    Test-Case "reject mixed $field" {$p=New-CommandProof;$p.samples[8].identity.$field='different';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Mixed causal identity'}
}
foreach($field in @('processObject','contextObject')){
    Test-Case "reject mixed $field" {$p=New-CommandProof;$p.samples[8].identity.$field=999;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Mixed process/context'}
}
Test-Case 'reject cooldown standing in for missing acted observation' {$p=New-CommandProof;$p.samples=@($p.samples|Where-Object boundary -CNE 'acted');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Missing or repeated exact acted'}
Test-Case 'reject false acted with correct charged endpoint' {$p=New-CommandProof;$p.samples[5].acted=$false;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Native acted was not observed'}
Test-Case 'reject another Move cost' {$p=New-CommandProof;$p.resourceWindow.events+=@($p.resourceWindow.events[0]);Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'callback count'}
foreach($field in @('Standard','Swift')){
    Test-Case "reject rider $field callback" {$p=New-CommandProof;$p.resourceWindow.events[0].actionType=$field;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
}
Test-Case 'reject mount native cost' {$p=New-CommandProof;$p.resourceWindow.events[0].commandActor='mount';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
Test-Case 'reject unobserved native process terminal' {$p=New-CommandProof;$p.samples[-1].processEnded=$false;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'terminal state'}
Test-Case 'reject native clear' {$p=New-CommandProof;$p.resourceWindow.events+=@{boundary='clear-before';state=@{actor='rider'};preparingTurn=0};Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Unexpected preparation'}
Test-Case 'reject combat reset' {$p=New-CommandProof;$p.resourceWindow.events+=@{boundary='combat-clear-before';state=@{actor='rider'}};Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'Unexpected preparation'}
Test-Case 'reject principal preparation' {$p=New-CommandProof $true $true 1;$p.resourceWindow.events[-4].state.actor='rider';Reject {Assert-KmcRelationshipCommandProof $p $true $true 1 $true} 'declared partner grant'}
Test-Case 'reject RT refund below native decay' {$p=New-CommandProof;$p.samples[-1].state.rider.standard=0;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'refunded or added rider standard'}
Test-Case 'reject RT extra charge even below pre-click endpoint' {$p=New-CommandProof;$p.samples[-1].state.rider.standard=3;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'refunded or added rider standard'}
Test-Case 'reject TB second cost' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=7;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject TB refund' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=0;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject TB endpoint drift beyond exact tolerance' {$p=New-CommandProof $true $true;$p.samples[-1].state.rider.move=4.001;Reject {Assert-KmcRelationshipCommandProof $p $true $true 0 $true} 'refunded or added rider move'}
Test-Case 'reject exploration action write' {$p=New-CommandProof $false;$p.resourceWindow.events[-1].state.move=4;Reject {Assert-KmcRelationshipCommandProof $p $false $false 0 $false} 'native Move cost differs'}
Test-Case 'reject lost pre-click baseline' {$p=New-CommandProof;$p.preClick=$null;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'pre-click baseline'}
Test-Case 'reject debt write before acted callback' {$p=New-CommandProof;$p.resourceWindow.events[0].state.move=1;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'before the exact acted'}
Test-Case 'reject wrong single rider selection' {$p=New-CommandProof;$p.preClick.state.selectedIds=@('mount');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'exact single rider'}
Test-Case 'reject multiple selection' {$p=New-CommandProof;$p.preClick.state.selectedIds=@('rider','mount');Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'exact single rider'}
Test-Case 'reject generation changed before Init' {$p=New-CommandProof;$p.preClick.state.generation=3;Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'generation differs'}
Test-Case 'reject mount cost charged using rider command' {$p=New-CommandProof;$p.resourceWindow.events[0].state.actor='mount';Reject {Assert-KmcRelationshipCommandProof $p $true $false 0 $true} 'foreign actor, command or action'}
function New-CompensationEnvelope {
    $proofs=@()
    foreach($name in @('exploration-mount','exploration-dismount','compensation')) {
        $p=New-CommandProof ($name -ceq 'compensation')
        $p|Add-Member -NotePropertyName window -NotePropertyValue $name
        $id=$p.identity;$id.abilityGuid=if($name -ceq 'exploration-dismount'){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'}
        if($name -ceq 'exploration-dismount'){$id.targetId='rider'}
        foreach($sample in $p.samples){$sample.identity.abilityGuid=$id.abilityGuid;$sample.identity.targetId=$id.targetId}
        $proofs+=@($p)
    }
    Copy-Value @{rows=@(@('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing','CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases')|ForEach-Object {@{name=$_;status='PASS'}})
        observations=@{chunk6aCombatMount=@();chunk6aCommandProofs=$proofs;phase3fActualConfiguration=@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}}}
}
$request=[pscustomobject]@{scenario='chunk6a-adoption-compensation-rt'}
Test-Case 'isolated compensation envelope requires all three exact windows' {Assert-KmcChunk6aCombatMountEvidence $request (New-CompensationEnvelope) 'PASS'}
Test-Case 'reject positive Mount in compensation allocation' {$a=New-CompensationEnvelope;$a.rows+=@{name='CM02-approach-arrival';status='PASS'};Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'cannot contain a positive Mount'}
Test-Case 'reject missing exploration Mount window' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs=@($a.observations.chunk6aCommandProofs|Where-Object window -CNE 'exploration-mount');Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'window count differs'}
Test-Case 'reject shared exploration Mount and Dismount evidence' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs[1]=$a.observations.chunk6aCommandProofs[0];Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'exactly one exploration-mount'}
Test-Case 'reject mislabeled native ability window' {$a=New-CompensationEnvelope;$a.observations.chunk6aCommandProofs[0].identity.abilityGuid='foreign';Reject {Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS'} 'wrong ability or target'}
Write-Host "CHUNK6A CAUSAL PROTOCOL PASS=$script:passed FAIL=0 (synthetic validator tests; no runtime qualification)"
