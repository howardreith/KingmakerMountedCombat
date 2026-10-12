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
    [pscustomobject]@{gameTicks=$Archive.GameTimeTicks;turnBased=$Archive.Combat.TurnBased;playerInCombat=$true;actors=(Copy-Test $Archive.Combat.Actors);combat=(Copy-Test $Archive.Combat);participations=(Copy-Test $Archive.Participations);owners=$ownerRows;nativeControls=(Copy-Test $Archive.Slots)}
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
Write-Host ('CHUNK7 COMBAT PERSISTENCE READER PASS='+$passed+' FAIL=0; synthetic validator contracts only, no native gameplay claim.')
