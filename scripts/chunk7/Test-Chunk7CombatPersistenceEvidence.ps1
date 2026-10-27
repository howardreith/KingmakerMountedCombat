$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7CombatPersistenceEvidence.ps1')
$passed=0
function Movement([double]$Time=0.0){[pscustomobject]@{TimeMoved=$Time;TimeForced=0.0;TimeStepped=0.0;MetresStepped=0.0;StepImmune=$false;AiStep=$false;AutoStop=$false}}
function Turn([string]$Id,[int]$Status=3){[pscustomobject]@{ActorId=$Id;Status=$Status;Movement=(Movement);AttackMode=0;MovementLimit=0;GroundLimit=0;ModifyCurrent=$false;ModifyRequested=$false;SmartIndex=-1;SmartManual=$false;ActingSurprise=$false;WaitingGetUp=$false;WasProne=$false;CanGetUp=$false;WaitedToEnd=0.0;DelayTarget=$null}}
function ActorState($Debt){[pscustomobject]@{Native=$Debt;InCombat=$true;Prepared=$true;HitThisRound=$false;InitiativeRoll=20;CachedInitiative=20;InitiativeRandom=0;ExecutedAttacks=1;LastMoveTicks=0L;LastDeflectTicks=0L;StoryImmunity=0L;EnergyDrainImmunity=$false;PreventNextReaction=$false;ReturnPoint=$null;ReturnYaw=$null;DisengageTargets=@();AiActions=@();AiDelayTicks=0L}}
function Participation([double]$Debt,[bool]$Ended){[pscustomobject]@{Granted=$true;Prepared=$true;Ended=$Ended;StandardObserved=$Debt;MoveObserved=0.5;SwiftObserved=0.0;ForfeitRecorded=$Ended;ForfeitSettled=$Ended;ForfeitAdded=0.0}}
function New-CombatArchive([string]$Case='multi-tb-debt-manual') {
    $q=Copy-Test $request;$q.persistenceCase=$Case
    $a=Copy-Test $archive
    $enemy='99999999-9999-4999-8999-999999999999'
    $debt=@();for($i=0;$i-lt4;$i++){$d=Native $ids[$i] $(if($i-eq3){0.0}else{6.0});$debt+=$d};$debt+=Native $enemy 0.0
    $actorRows=@($debt|ForEach-Object{ActorState $_})
    for($i=0;$i-lt2;$i++){$a.Pairs[$i].Rider=$debt[$i*2];$a.Pairs[$i].Mount=$debt[$i*2+1]}
    $roster=@();for($i=0;$i-lt$debt.Count;$i++){$roster+=[pscustomobject]@{ActorId=$debt[$i].Id;InitiativeProcessed=$true;Surprising=$false;Surprised=$false;ActingSurprise=$false;InitiativeOverride=$null;Sequence=$i}}
    $parts=@()
    for($i=0;$i-lt2;$i++){
        $p=$a.Pairs[$i];$activation=[pscustomobject]@{EncounterId=$p.PairId;Sequence=1L;Ending=($i-eq0);Finalized=($i-eq0);Split=$false;Suspended=$false;Rider=(Participation 6.0 ($i-eq0));Mount=(Participation $(if($i-eq0){6.0}else{0.0}) ($i-eq0))}
        $state=[pscustomobject]@{RiderId=$p.Rider.Id;MountId=$p.Mount.Id;Activation=$activation;BoundaryIsCurrent=($i-eq1);Boundary=$(if($i-eq0){Turn $p.Rider.Id 6}else{$null});Partner=(Turn $p.Mount.Id);SplitReleaseRound=-1;PendingSplitId=$null;PendingSplitRound=-1;RenewalNotBeforeTicks=0L}
        $parts+=[pscustomobject]@{OwnerId=$p.PairId;Generation=$p.Generation;MountedPairId=$p.PairId;State=$state}
    }
    $alloc=@();foreach($p in $a.Pairs){$alloc+=[pscustomobject]@{ActorId=$p.Mount.Id;Round=3;RoundStartTicks=0L;GrantId=($p.PairId+':1');Prepared=$true;MoveObserved=0.5;StandardCommitted=$false;Movement=(Movement 0.5)}}
    $a.Participations=$parts
    $a.Combat=[pscustomobject]@{TurnBased=$true;Round=3;StartTicks=0L;RoundStartTicks=0L;TurnStartTicks=0L;TimeSinceStart=0.0;TimeToNextRound=6.0;HasSurpriseRound=$false;HasEnemy=$true;HadEnemy=$true;NextActor=$null;BeforeCombatSelected=$first;Actors=$actorRows;Roster=$roster;Engagements=@();Current=(Turn $second);Paired=$null;Allocations=$alloc}
    if($Case-ceq'multi-rt-debt-manual') {
        $a.Combat.TurnBased=$false;$a.Combat.Roster=@();$a.Combat.Current=$null;$a.Combat.Round=0
        $a.Combat.Actors[2].Native.Standard=0.0;$a.Combat.Actors[3].Native.Standard=4.0
        foreach($p in $a.Participations){$p.State.Activation=$null;$p.State.BoundaryIsCurrent=$false;$p.State.Boundary=$null;$p.State.Partner=$null}
    }
    if($Case-ceq'zero-mounted-tb-retained-manual') {
        $a.Pairs=@();$a.Slots=@();foreach($p in $a.Participations){$p.MountedPairId=$null;$p.State.Activation.Split=$true;$p.State.SplitReleaseRound=3}
    }
    return [pscustomobject]@{request=$q;archive=$a}
}
$tb=New-CombatArchive
Check 'two independent TB native expenditures and single shared world' {Assert-KmcChunk7CombatArchive $tb.archive $tb.request}
$rt=New-CombatArchive 'multi-rt-debt-manual'
Check 'RT actor-specific expenditures without a synthetic turn' {Assert-KmcChunk7CombatArchive $rt.archive $rt.request}
$zero=New-CombatArchive 'zero-mounted-tb-retained-manual'
Check 'zero attachments retain two detached release owners' {Assert-KmcChunk7CombatArchive $zero.archive $zero.request}
foreach($mutation in @('drop-owner','cross-owner-actor','duplicate-shared-actor','pair-debt-disagreement','duplicate-current-claim','lost-current','lost-A-expenditure','lost-B-expenditure','invented-B-mount-expenditure','singular-authority','lost-slot','duplicate-allocation','mistyped-prepared')) {
    Reject ('TB '+$mutation) {
        $a=Copy-Test $tb.archive
        switch($mutation){
          'drop-owner' {$a.Participations=@($a.Participations[0])}
          'cross-owner-actor' {$a.Participations[1].State.MountId=$first}
          'duplicate-shared-actor' {$a.Combat.Actors[4].Native.Id=$first}
          'pair-debt-disagreement' {$a.Pairs[1].Mount.Move=0.0}
          'duplicate-current-claim' {$a.Participations[0].State.BoundaryIsCurrent=$true}
          'lost-current' {$a.Combat.Current.ActorId=$first}
          'lost-A-expenditure' {$a.Combat.Actors[0].Native.Standard=0.0;$a.Pairs[0].Rider.Standard=0.0}
          'lost-B-expenditure' {$a.Combat.Actors[2].Native.Standard=0.0;$a.Pairs[1].Rider.Standard=0.0}
          'invented-B-mount-expenditure' {$a.Combat.Actors[3].Native.Standard=6.0;$a.Pairs[1].Mount.Standard=6.0}
          'singular-authority' {$a.Combat.Paired=$a.Participations[0].State}
          'lost-slot' {$a.Slots=@($a.Slots|Select-Object -First 3)}
          'duplicate-allocation' {$a.Combat.Allocations[1].ActorId=$a.Combat.Allocations[0].ActorId}
          'mistyped-prepared' {$a.Combat.Actors[0].Prepared='true'}
        };Assert-KmcChunk7CombatArchive $a $tb.request
    }
}
foreach($mutation in @('lost-RT-debt','invented-RT-turn')){Reject $mutation {$a=Copy-Test $rt.archive;if($mutation-ceq'lost-RT-debt'){$a.Pairs[1].Mount.Standard=0.0;$a.Combat.Actors[3].Native.Standard=0.0}else{$a.Combat.Current=Turn $first};Assert-KmcChunk7CombatArchive $a $rt.request}}
foreach($mutation in @('retired-detached-owner','restarted-split-round','attached-retained-owner')){Reject $mutation {$a=Copy-Test $zero.archive;switch($mutation){'retired-detached-owner' {$a.Participations=@($a.Participations[1])};'restarted-split-round' {$a.Participations[0].State.SplitReleaseRound=4};'attached-retained-owner' {$a.Participations[0].MountedPairId=$a.Participations[0].OwnerId}};Assert-KmcChunk7CombatArchive $a $zero.request}}
function NativeWorld($Archive,[bool]$Presentation) {
    $ownerRows=@();foreach($p in $Archive.Pairs){$ownerRows+=[pscustomobject]@{ownerId=$p.PairId;generation=$p.Generation;riderId=$p.Rider.Id;mountId=$p.Mount.Id;state=$(if($Presentation){'Active'}else{'Reserved'})}}
    foreach($p in $Archive.Participations|Where-Object{$null-eq$_.MountedPairId}){$ownerRows+=[pscustomobject]@{ownerId=$p.OwnerId;generation=$p.Generation;riderId=$p.State.RiderId;mountId=$p.State.MountId;state='Detached'}}
    [pscustomobject]@{gameTicks=$Archive.GameTimeTicks;turnBased=$Archive.Combat.TurnBased;playerInCombat=$true;actors=(Copy-Test $Archive.Combat.Actors);combat=(Copy-Test $Archive.Combat);participations=@(Copy-Test $Archive.Participations|ForEach-Object{if(-not$Presentation){$_.MountedPairId=$null};$_});owners=$ownerRows;nativeControls=(Copy-Test $Archive.Slots)}
}
Check 'immediate shared restore retains all actors, roster, contexts, allocations and ownership before presentation' {Assert-KmcChunk7NativeWorldRestore (NativeWorld $tb.archive $false) $tb.archive $false}
Check 'immediate all-pair presentation retains controls and ownership' {Assert-KmcChunk7NativeWorldRestore (NativeWorld $tb.archive $true) $tb.archive $true}
Check 'immediate zero-mounted restoration retains detached obligations' {Assert-KmcChunk7NativeWorldRestore (NativeWorld $zero.archive $true) $zero.archive $true}
foreach($mutation in @('actor-prepared','actor-debt','current-context','roster-order','grant-identity','participation-release','owner-generation','owner-state','owner-omission','slot-omission')) {
    Reject ('world '+$mutation) {$v=NativeWorld $tb.archive $true;switch($mutation){
      'actor-prepared' {$v.actors[0].Prepared=$false}
      'actor-debt' {$v.actors[0].Native.Standard=0.0}
      'current-context' {$v.combat.Current.ActorId=$first}
      'roster-order' {$v.combat.Roster[0].Sequence=99}
      'grant-identity' {$v.combat.Allocations[0].GrantId='native:'+('7'*32)}
      'participation-release' {$v.participations[0].State.SplitReleaseRound=4}
      'owner-generation' {$v.owners[0].generation=999}
      'owner-state' {$v.owners[0].state='Reserved'}
      'owner-omission' {$v.owners=@($v.owners[0])}
      'slot-omission' {$v.nativeControls=@($v.nativeControls|Select-Object -First 3)}
    };Assert-KmcChunk7NativeWorldRestore $v $tb.archive $true}
}
$legacy=New-CombatArchive 'legacy-mounted-pending-tb-manual';$la=$legacy.archive;$la.Pairs=@($la.Pairs[0]);$la.Slots=@($la.Slots|Where-Object ActorId -CEQ $first)
$la.Participations=@($la.Participations[0]);$ls=$la.Participations[0].State;$ls.RiderId=$null;$ls.MountId=$null;$ls.Activation=$null;$ls.BoundaryIsCurrent=$false;$ls.Boundary=$null;$ls.Partner=$null;$ls.PendingSplitId=$mammoth;$ls.PendingSplitRound=$la.Combat.Round
$la.Combat.Actors[0].InCombat=$false
$hash=[Security.Cryptography.SHA256]::Create();try{$bytes=$hash.ComputeHash([Text.Encoding]::UTF8.GetBytes('kmc-schema3|'+$la.CampaignId+'|'+$first+'|'+$mammoth));$migrationOwner=[Guid]::new([byte[]]$bytes[0..15]).ToString('N')}finally{$hash.Dispose()}
$la.Pairs[0].PairId=$migrationOwner;$la.Pairs[0].Generation=1;$la.Participations[0].OwnerId=$migrationOwner;$la.Participations[0].MountedPairId=$migrationOwner;$la.Participations[0].Generation=1
$schema2=[pscustomobject]@{SchemaVersion=2;CampaignId=$la.CampaignId;AreaId=$la.AreaId;GameTimeTicks=$la.GameTimeTicks;Policy=$la.Policy;RulesId=$la.RulesId;Mounted=$true;ProfileId=$la.Pairs[0].ProfileId;Rider=(Copy-Test $la.Pairs[0].Rider);Mount=(Copy-Test $la.Pairs[0].Mount);Slots=(Copy-Test $la.Slots);Combat=(Copy-Test $la.Combat)}
$schema2.Combat.Paired=Copy-Test $ls
Check 'native legacy mounted plus pending-only migration retains null activation, owner association, native debt and round' {Assert-KmcChunk7CombatArchive $la $legacy.request;Assert-KmcChunk7LegacyPendingMigration $schema2 $la}
foreach($mutation in @('invented-activation','invented-rider','dropped-pending','delayed-round','lost-pending-debt','conflicting-migration-owner','changed-native-world','changed-native-slot')) {
    Reject ('legacy '+$mutation) {$a=Copy-Test $la;switch($mutation){
      'invented-activation' {$a.Participations[0].State.Activation=Copy-Test $tb.archive.Participations[0].State.Activation}
      'invented-rider' {$a.Participations[0].State.RiderId=$first}
      'dropped-pending' {$a.Participations[0].State.PendingSplitId=$null}
      'delayed-round' {$a.Participations[0].State.PendingSplitRound=4}
      'lost-pending-debt' {$a.Pairs[0].Mount.Standard=0.0;$a.Combat.Actors[1].Native.Standard=0.0}
      'conflicting-migration-owner' {$a.Participations[0].OwnerId='88888888888848888888888888888888'}
      'changed-native-world' {$a.Combat.RoundStartTicks=1L}
      'changed-native-slot' {$a.Slots[0].Index=1}
    };Assert-KmcChunk7CombatArchive $a $legacy.request;Assert-KmcChunk7LegacyPendingMigration $schema2 $a}
}
# P2 owner-seed combat checkpoints (generated, labelled synthetic): the restored-start and derived-target readers
# belong only to the owner-seed checkpoint producer and accept only the owner-approved derived-copy basis.
Reject 'restored-start reader belongs only to the owner-seed checkpoint producer' {Assert-KmcChunk7OwnerSeedCheckpointStart $tb.request ([pscustomobject]@{inputs=@()}) $tb.archive}
$seedPins=[pscustomobject]@{seedSha256=('9'*64);sourceArchiveSha256=('7'*64);sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);checkpointSourceRunId='synthetic-owner-seed-cold';checkpointSourceArtifactSha256=('8'*64)}
$seedTargetRequest=[pscustomobject]@{ownedPairs=$seedPins;persistenceLoad=[pscustomobject]@{area=('a'*32)}}
function New-SeedTargetAuthorization{[pscustomobject][ordered]@{contract='chunk7-combat-target-authorization-v1';basis='owner-seed-derived-disposable-copy';ownerApproval='docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md, Owner decisions on the combat slice (October 17, 2026): combat target approved';workingSha256=$null;seedSha256=$seedPins.seedSha256;derivedManualSha256=$seedPins.sourceArchiveSha256;producerRunId=$seedPins.sourceRunId;producerArtifactSha256=$seedPins.sourceArtifactSha256;consumerRunId=$seedPins.checkpointSourceRunId;consumerArtifactSha256=$seedPins.checkpointSourceArtifactSha256;area=('a'*32);riderPosition=@(0.0,0.0,0.0);position=@(0.0,0.0,3.5);placementValidated=$true}}
Check 'owner-seed combat checkpoint target carries the owner-approved derived-copy basis' {Assert-KmcChunk7OwnerSeedTargetAuthorization $seedTargetRequest (New-SeedTargetAuthorization)}
foreach($mutation in @('working-basis','acquired-basis','no-approval','working-pin','seed-pin','manual-pin','consumer-pin','area','placement','missing')) {
    Reject ('owner-seed combat checkpoint target '+$mutation) {$t=New-SeedTargetAuthorization
        switch($mutation){'working-basis'{$t.basis='exact-working-fixture'}'acquired-basis'{$t.basis='owned-acquired-field-copy'}'no-approval'{$t.ownerApproval=$null}'working-pin'{$t.workingSha256='5'*64}'seed-pin'{$t.seedSha256='e'*64}'manual-pin'{$t.derivedManualSha256=$seedPins.seedSha256}'consumer-pin'{$t.consumerRunId='synthetic-other'}'area'{$t.area='b'*32}'placement'{$t.placementValidated=$false}'missing'{$t=$null}}
        Assert-KmcChunk7OwnerSeedTargetAuthorization $seedTargetRequest $t}
}
# RT debt checkpoint order (rt-debt-checkpoint-sequenced-v2, F5 r11): pair B first, pair A after pair B settled, the
# four measured debts recorded with their snapshot. v1 producer evidence (one update, A then B) keeps its path.
function RtSnap([string]$Label,[long]$Frame,[long]$Ticks,[double[]]$Std){[pscustomobject]@{label=$Label;frame=$Frame;gameTicks=$Ticks;actors=@(for($i=0;$i-lt4;$i++){[pscustomobject]@{id=$ids[$i];standard=$Std[$i];native=[pscustomobject]@{waitingInitiative=$false}}})}}
function RtInput([string]$Label,$Before,$After){[pscustomobject]@{label=$Label;before=$Before;after=$After}}
function New-RtSequenced{
    $std=@(4.1,0.0,0.0,3.8)
    $b=RtInput 'rt-pair-b-mount-primary' (RtSnap 'attack-before' 100 1000000 @(0,0,0,0)) (RtSnap 'rt-attack-settled' 180 68000000 @(0,0,0,5.9))
    $a=RtInput 'rt-pair-a-rider-primary' (RtSnap 'attack-before' 181 68100000 @(0,0,0,5.89)) (RtSnap 'rt-attack-settled' 240 89000000 $std)
    $record=[pscustomobject]@{contract='rt-debt-checkpoint-sequenced-v2';order=@('rt-pair-b-mount-primary','rt-pair-a-rider-primary');minimumDebt=0.1
        debts=@(for($i=0;$i-lt4;$i++){[pscustomobject]@{actorId=$ids[$i];standard=$std[$i]}});snapshot=(RtSnap 'rt-debt-checkpoint' 240 89000000 $std);holds=$true}
    [pscustomobject]@{source=[pscustomobject]@{inputs=@($b,$a);realtimeCheckpoint=$record};checkpoint=(RtSnap 'native-checkpoint-ready' 240 89000000 $std)}
}
function New-RtOneUpdate{
    $c=New-RtSequenced;$c.source.PSObject.Properties.Remove('realtimeCheckpoint')
    $c.source.inputs=@((RtInput 'rt-pair-a-rider-primary' (RtSnap 'attack-before' 100 1000000 @(0,0,0,0)) (RtSnap 'rt-attack-settled' 150 22000000 @(4,0,0,0))),
        (RtInput 'rt-pair-b-mount-primary' (RtSnap 'attack-before' 100 1000000 @(0,0,0,0)) (RtSnap 'rt-attack-settled' 170 30000000 @(3,0,0,5.9))));$c
}
Check 'RT checkpoint sequenced v2: B first, A after B settled, recorded debts recomputed' {Assert-KmcChunk7RealtimeCheckpointSequence (New-RtSequenced) $ids}
Check 'RT checkpoint v1 producer evidence (one update, A then B) keeps its path' {Assert-KmcChunk7RealtimeCheckpointSequence (New-RtOneUpdate) $ids}
Check 'RT checkpoint synthetic primaries without a record keep their path' {$c=New-RtOneUpdate;foreach($i in $c.source.inputs){$i.label='native-primary-fixture'};Assert-KmcChunk7RealtimeCheckpointSequence $c $ids}
foreach($mutation in @('v1-different-frames','v1-reversed-without-record','v2-reversed-order','v2-order-field','a-issued-before-b-settled','a-issued-before-b-clock','checkpoint-before-a-settled','a-debt-expired','a-debt-expired-claimed','b-debt-expired','rider-b-spent','mammoth-spent','debt-differs-from-snapshot','debt-actor-order','saved-frame-differs','saved-debt-differs','contract','minimum','missing-debt','extra-property','third-input','missing-frame','b-during-initiative-wait','a-during-initiative-wait','initiative-unrecorded')) {
    Reject ('RT checkpoint '+$mutation) {
        $c=New-RtSequenced;$r=$c.source.realtimeCheckpoint
        switch($mutation){
          'v1-different-frames' {$c=New-RtOneUpdate;$c.source.inputs[1].before.frame=101}
          'v1-reversed-without-record' {$c.source.PSObject.Properties.Remove('realtimeCheckpoint')}
          'v2-reversed-order' {$c.source.inputs=@($c.source.inputs[1],$c.source.inputs[0])}
          'v2-order-field' {$r.order=@('rt-pair-a-rider-primary','rt-pair-b-mount-primary')}
          'a-issued-before-b-settled' {$c.source.inputs[1].before.frame=170}
          'a-issued-before-b-clock' {$c.source.inputs[1].before.gameTicks=60000000}
          'checkpoint-before-a-settled' {$r.snapshot.frame=239;$c.checkpoint.frame=239}
          'a-debt-expired' {$r.debts[0].standard=0.05;$r.snapshot.actors[0].standard=0.05;$c.checkpoint.actors[0].standard=0.05;$r.holds=$false}
          'a-debt-expired-claimed' {$r.debts[0].standard=0.0;$r.snapshot.actors[0].standard=0.0;$c.checkpoint.actors[0].standard=0.0}
          'b-debt-expired' {$r.debts[3].standard=0.1;$r.snapshot.actors[3].standard=0.1;$c.checkpoint.actors[3].standard=0.1}
          'rider-b-spent' {$r.debts[2].standard=2.0;$r.snapshot.actors[2].standard=2.0;$c.checkpoint.actors[2].standard=2.0}
          'mammoth-spent' {$r.debts[1].standard=1.0;$r.snapshot.actors[1].standard=1.0;$c.checkpoint.actors[1].standard=1.0}
          'debt-differs-from-snapshot' {$r.debts[3].standard=5.0}
          'debt-actor-order' {$r.debts=@($r.debts[1],$r.debts[0],$r.debts[2],$r.debts[3])}
          'saved-frame-differs' {$c.checkpoint.frame=241}
          'saved-debt-differs' {$c.checkpoint.actors[0].standard=4.0}
          'contract' {$r.contract='rt-debt-checkpoint-sequenced-v1'}
          'minimum' {$r.minimumDebt=0.0}
          'missing-debt' {$r.debts=@($r.debts|Select-Object -First 3)}
          'extra-property' {$r|Add-Member grantedDebt $true}
          'third-input' {$c.source.inputs=@($c.source.inputs)+@($c.source.inputs[1])}
          'missing-frame' {$c.source.inputs[0].after.frame=$null}
          'b-during-initiative-wait' {$c.source.inputs[0].before.actors[3].native.waitingInitiative=$true}
          'a-during-initiative-wait' {$c.source.inputs[1].before.actors[1].native.waitingInitiative=$true}
          'initiative-unrecorded' {$c.source.inputs[0].before.actors[2].native=$null}
        };Assert-KmcChunk7RealtimeCheckpointSequence $c $ids
    }
}
Write-Host ('CHUNK7 COMBAT PERSISTENCE READER PASS='+$passed+' FAIL=0; synthetic validator contracts only, no native gameplay claim.')
