Set-StrictMode -Version Latest

# Bounded P01 archive intake. The source must be the exact completed archive
# from another restored owned process; no gameplay state is taken from evidence.
function Get-KmcPersistenceSource {
    param([Parameter(Mandatory=$true)][string]$SourceRunId,
        [Parameter(Mandatory=$true)][string]$ExpectedSha256,
        [Parameter(Mandatory=$true)]$Fixture,
        [AllowNull()][ValidateSet('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','manual','quick','auto','alternating','queued','unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting','condition','condition-preparing','suspended')][string]$NativeCase,
        [ValidatePattern('^[0-9a-f]{32}$')][string]$ExpectedArea,
        # A cross-area source run produces two distinct artifacts: the separate
        # destination manual archive and the engine's own transition autosave.
        # A prepare-removal run's cleanup archive and a death run's no-pair
        # archive are their own roles.
        [ValidateSet('destination-manual','transition-auto','cleanup-manual','death-manual','eligibility-manual','campaign-b-manual')][string]$ArtifactRole='destination-manual',
        [switch]$Alternate)
    if($SourceRunId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or $SourceRunId -in @('.','..') -or
        $ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$'){throw 'Persistence source identity is invalid.'}
    $lab=Get-KmcLabRoot
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path $lab 'runtime-staging') 'owned persistence source'
    Assert-KmcDirectoryTreeCloneable $root 'owned persistence source'
    $owner=Read-KmcJson (Join-Path $root 'owner.json')
    $result=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$SourceRunId+'/runtime-result.json'))
    if($owner.runId-cne$SourceRunId-or$owner.scenario-cnotin @('persistence-p07-save','persistence-p01-save','persistence-p02-save','persistence-p03-save','persistence-p04-save','persistence-p05-save')-or$result.status-cne'PASS'-or
        $result.runId-cne$SourceRunId-or$result.scenario-cne$owner.scenario-or
        $result.modsRestored-ne$true-or$result.workingRestored-ne$true-or
        $owner.transactionToken-cnotmatch'^[0-9a-f]{64}$'-or$owner.transactionToken-cne$result.transactionToken){throw 'Source is not a completed restored P01 save process.'}
    $isSlot=$owner.scenario-ceq'persistence-p05-save'
    if($owner.scenario-ceq'persistence-p07-save'){
        if($NativeCase-cnotin @('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit')-or$owner.persistenceCase-cne$NativeCase){throw 'P07 source recovery case differs.'}
    }elseif($isSlot){
        if([string]::IsNullOrEmpty($NativeCase)-or$owner.persistenceCase-cne$NativeCase){throw 'Source native slot category differs.'}
    }elseif($owner.scenario-ceq'persistence-p04-save'){
        if($NativeCase-cnotin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting')-or$owner.persistenceCase-cne$NativeCase){throw 'P04 source RT checkpoint differs.'}
    }elseif($NativeCase-cin @('condition','condition-preparing','suspended')){
        if($owner.scenario-cne'persistence-p03-save'-or$owner.persistenceCase-cne$NativeCase){throw 'P03 condition source case differs.'}
    }elseif(-not[string]::IsNullOrEmpty($NativeCase)){throw 'Declared native case requires an exact P03/P04/P05 source.'}
    if($Alternate-and$NativeCase-cne'alternating'){throw 'Second archive is restricted to the exact alternating source.'}
    $type=if($ArtifactRole-ceq'transition-auto'){'Auto'}elseif($NativeCase-ceq'quick'){'Quick'}elseif($NativeCase-ceq'auto'){'Auto'}else{'Manual'}
    if($ArtifactRole-ceq'transition-auto'-and$NativeCase-cnotin @('area-cross-entry','area-cross-exit')){throw 'Only a cross-area source run produces a transition autosave.'}
    if($ArtifactRole-ceq'cleanup-manual'-and($NativeCase-cne'prepare-removal'-or$Alternate)){throw 'Only a prepare-removal source run produces a cleanup archive.'}
    if($ArtifactRole-ceq'death-manual'-and($NativeCase-cnotin @('rider-death','mount-death')-or$Alternate)){throw 'Only a death source run produces a no-pair death archive.'}
    if($ArtifactRole-ceq'eligibility-manual'-and($NativeCase-cne'rider-size-change'-or$Alternate)){throw 'Only an eligibility source run produces a no-pair size archive.'}
    if($ArtifactRole-ceq'campaign-b-manual'-and($NativeCase-cne'campaign-b'-or$Alternate)){throw 'Only a campaign-b source run produces B own manual archive.'}
    $manualName=if($ArtifactRole-ceq'cleanup-manual'){'KMC_CLEANUP'}elseif($ArtifactRole-ceq'death-manual'){'KMC_DEATH'}elseif($ArtifactRole-ceq'eligibility-manual'){'KMC_SIZE'}elseif($ArtifactRole-ceq'campaign-b-manual'){'KMC_B'}elseif($Alternate){'KMC_P05_UNMOUNTED'}else{'KMC_P01'}
    $leaf=if($ArtifactRole-ceq'cleanup-manual'){'Manual_301_KMC_CLEANUP.zks'}elseif($ArtifactRole-ceq'death-manual'){'Manual_301_KMC_DEATH.zks'}elseif($ArtifactRole-ceq'eligibility-manual'){'Manual_301_KMC_SIZE.zks'}elseif($ArtifactRole-ceq'campaign-b-manual'){'Manual_302_KMC_B.zks'}elseif($ArtifactRole-ceq'transition-auto'){'Auto_1.zks'}elseif($Alternate){'Manual_301_KMC_P05_UNMOUNTED.zks'}elseif($NativeCase-ceq'queued'){'Manual_302_KMC_P01.zks'}elseif($type-ceq'Manual'){'Manual_300_KMC_P01.zks'}else{$type+'_1.zks'}
    # Campaign B's archive carries the identity the engine minted for B, read from
    # the source run's own frozen observation, never the fixture's.
    $expectedGameId=[string]$Fixture.working.gameId;$expectedGameName=[string]$Fixture.working.gameName
    $expectedHeaderArea=if([string]::IsNullOrEmpty($ExpectedArea)){[string]$Fixture.working.area}else{$ExpectedArea}
    if($ArtifactRole-ceq'campaign-b-manual'){
        $bRows=@(Get-Content -LiteralPath (Join-Path $lab ('runtime-evidence/'+$SourceRunId+'/persistence-observations.jsonl')) -Encoding UTF8|ForEach-Object{$_|ConvertFrom-Json})
        $frozen=@($bRows|Where-Object kind -CEQ 'campaign-b-frozen')
        if($frozen.Count-ne1-or$frozen[0].detail.manualSaved-ne$true){throw 'Campaign B source run has no frozen manual archive.'}
        $expectedGameId=[string]$frozen[0].detail.gameId;$expectedGameName=[string]$frozen[0].detail.gameName;$expectedHeaderArea=[string]$frozen[0].detail.area
        if($expectedGameId-ceq[string]$Fixture.working.gameId-or$expectedGameId-cnotmatch'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'-or$expectedGameId-ceq'00000000-0000-0000-0000-000000000000'-or
            [string]::IsNullOrEmpty($expectedGameName)-or$expectedHeaderArea-cnotmatch'^[0-9a-f]{32}$'-or$expectedHeaderArea-ceq[string]$Fixture.working.area){throw 'Campaign B identity is not B own.'}
    }
    $path=Join-Path $root ('Saved Games/'+$leaf)
    Assert-KmcNotReparsePoint $path 'owned persistence source archive'
    Assert-KmcNotHardLink $path 'owned persistence source archive'
    if((Get-KmcSha256 $path)-cne$ExpectedSha256-or$ExpectedSha256-ceq$Fixture.baseline.sha256){throw 'Owned source archive hash differs.'}
    $file=Get-Item -LiteralPath $path
    if($file.Length-le0-or$file.Length-gt256MB){throw 'Owned archive size is outside the native fixture bound.'}
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$archive=$null;$reader=$null
    try{
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        $entries=@($archive.Entries|Where-Object FullName -CEQ 'header.json')
        $metadata=@($archive.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($entries.Count-ne1-or$entries[0].Length-le0-or$entries[0].Length-gt1MB-or
            $metadata.Count-ne1-or$metadata[0].Length-le0-or$metadata[0].Length-gt128KB){throw 'Owned archive members are missing, ambiguous or oversized.'}
        $reader=[IO.StreamReader]::new($entries[0].Open(),[Text.UTF8Encoding]::new($false,$true))
        $json=$reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'owned persistence header'
        $header=$json|ConvertFrom-Json
        $nameOk=if($type-ceq'Manual'){$header.Name-ceq$manualName}else{$header.Name-is[string]-and$header.Name.Length-gt0-and$header.Name.Length-le256-and$header.Name-cnotmatch'[\x00-\x1f\x7f]'}
        if(-not$nameOk-or$header.Type-cne$type-or$header.CompatibilityVersion-ne1-or
            $header.GameId-cne$expectedGameId-or$header.GameName-cne$expectedGameName-or
            $header.Area-cne$expectedHeaderArea){throw 'Owned archive native campaign/type/name differs.'}
    }finally{if($reader){$reader.Dispose()};if($archive){$archive.Dispose()};if($stream){$stream.Dispose()}}
    if((Get-KmcSha256 $path)-cne$ExpectedSha256){throw 'Owned archive changed during inspection.'}
    return [pscustomobject]@{path=$path;descriptor=[ordered]@{
        internalName=[string]$header.Name;fileName=$file.Name;sha256=$ExpectedSha256;length=[long]$file.Length
        lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks
        gameId=[string]$header.GameId;gameName=[string]$header.GameName;area=[string]$header.Area
    }}
}

function Assert-KmcP02Snapshot {
    param($Snapshot,[string]$Checkpoint)
    $c=$Snapshot.Combat
    if($Checkpoint-ceq'reaction'){
        $actor=@($c.Actors|Where-Object {$_.Native.Id-ceq$Snapshot.Mount.Id})
        if($null-eq$c-or$c.Round-lt1-or$null-eq$c.Paired.Activation-or$null-eq$c.Current-or
            $Snapshot.Mount.ReactionsRemaining-ne0-or$actor.Count-ne1-or
            $c.Current.ActorId-cin @($Snapshot.Rider.Id,$Snapshot.Mount.Id)-or
            -not$c.Paired.Activation.Rider.Ended-or-not$c.Paired.Activation.Mount.Ended-or
            $actor[0].DisengageTargets-cnotcontains$c.Current.ActorId){throw 'P03 reaction snapshot lost its native count/participation/target.'}
        return
    }
    if($null-eq$c-or$c.Round-lt1-or$null-eq$c.Paired-or$null-eq$c.Paired.Activation-or$c.Paired.Activation.Sequence-lt1){
        throw 'P02 snapshot lacks native round and paired participation.'
    }
    $r=$Snapshot.Rider.Standard-gt0;$m=$Snapshot.Mount.Standard-gt0
    $valid=switch -CaseSensitive ($Checkpoint){
        'partial-movement' { -not$r-and-not$m-and$Snapshot.Mount.Move-gt0-and$Snapshot.Mount.Move-lt3 }
        'rider-spent' { $r-and-not$m-and$Snapshot.Mount.Move-gt0-and$Snapshot.Mount.Move-lt3 }
        'between-partner-orders' { -not$r-and$m-and$Snapshot.Mount.Move-ge3 }
        'exhausted' { $r-and$m-and$Snapshot.Mount.Move-ge3 }
        'explicit-end' { $r-and$m-and$c.Paired.Activation.Ending-eq$true }
        default { $false }
    }
    if(-not$valid-or($Checkpoint-cne'explicit-end'-and($null-eq$c.Current-or$c.Current.ActorId-cne$Snapshot.Rider.Id))){
        throw 'P02 actual archive does not match its declared native checkpoint.'
    }
}

function Assert-KmcP03Snapshot {
    param($Snapshot,[string]$Checkpoint)
    if($Checkpoint-ceq'suspended'){Assert-KmcSuspendedSnapshot $Snapshot;return}
    $c=$Snapshot.Combat
    if($Checkpoint-ceq'reaction'){
        $actor=@($c.Actors|Where-Object {$_.Native.Id-ceq$Snapshot.Mount.Id})
        if($null-eq$c-or$c.Round-lt1-or$null-eq$c.Paired.Activation-or$null-eq$c.Current-or
            $Snapshot.Mount.ReactionsRemaining-ne0-or$actor.Count-ne1-or
            $c.Current.ActorId-cin @($Snapshot.Rider.Id,$Snapshot.Mount.Id)-or
            -not$c.Paired.Activation.Rider.Ended-or-not$c.Paired.Activation.Mount.Ended-or
            $actor[0].DisengageTargets-cnotcontains$c.Current.ActorId){throw 'P03 reaction snapshot lost its native count/participation/target.'}
        return
    }
    if($null-eq$c-or$c.Round-lt1-or$null-eq$c.Paired-or$null-eq$c.Paired.Activation-or
        $c.Paired.Activation.Sequence-lt1-or$c.Current.ActorId-cne$Snapshot.Rider.Id-or
        $Snapshot.Rider.Standard-ne0-or$Snapshot.Rider.Move-ne0){throw 'P03 snapshot lost the native rider grant/remainder.'}
    $alloc=@($c.Allocations|Where-Object ActorId -CEQ $Snapshot.Mount.Id)
    if($alloc.Count-ne1){throw 'P03 snapshot lost its mount movement commitment.'}
    $a=$alloc[0]
    $valid=switch -CaseSensitive ($Checkpoint){
        'step' { $Snapshot.Mount.Standard-eq0-and$Snapshot.Mount.Move-eq0-and$a.Movement.MetresStepped-gt0-and$a.Movement.TimeStepped-gt0 }
        'conversion' { $Snapshot.Mount.Standard-eq0-and$Snapshot.Mount.Move-gt3-and$Snapshot.Mount.Move-lt6-and$a.Movement.TimeMoved-gt3 }
        'round-effect' { $Snapshot.Mount.Standard-eq0-and$Snapshot.Mount.Move-eq0 }
        default { $false }
    }
    if(-not$valid){throw 'P03 actual archive does not match its declared native commitment.'}
}

function Assert-KmcRealtimePersistenceEvidence {
    param($Request,$Rows,$GameResult)
    $source=$Request.scenario-ceq'persistence-p04-save'
    $projectile=$Request.persistenceCase.EndsWith('-projectile',[StringComparison]::Ordinal)
    $approach=$Request.persistenceCase.EndsWith('-approach',[StringComparison]::Ordinal)
    $casting=$Request.persistenceCase.EndsWith('-casting',[StringComparison]::Ordinal)
    $mounted=$Request.persistenceCase.StartsWith('mounted-',[StringComparison]::Ordinal)
    $state=if($mounted){'Mounted'}else{'Unmounted'}
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P04 lacks an initial native world.'}
    foreach($row in $Rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.checkpoint-cne$Request.persistenceCase-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.relationship-cne$state-or$row.controls.DuplicateFactCount-ne0-or$row.native.tbSetting-ne$false-or
            $row.native.tbInitialized-ne$false){throw 'P04 native identity, mode or control invariant differs.'}
    }
    $kind=if($source){'native-write-complete'}elseif($approach){'rt-cold-approach-restored'}else{'rt-cold-debt-restored'}
    $saved=@($Rows|Where-Object kind -CEQ $kind)
    if($saved.Count-ne1){throw 'P04 lacks actual selected native save metadata.'}
    $d=$saved[0].detail;$snapshot=$d.snapshot
    $activation=if($null-ne$snapshot.Combat.Paired){$snapshot.Combat.Paired.Activation}else{$null}
    $debtActor=if($casting){$d.actual.casting.caster}else{$initial[0].rider.Id}
    $actor=@($snapshot.Combat.Actors|Where-Object {$_.Native.Id-ceq$debtActor})
    if($snapshot.Mounted-ne$mounted-or$snapshot.Combat.TurnBased-ne$false-or$actor.Count-ne1-or
        $(if($approach){$actor[0].Native.Standard-ne0}else{$actor[0].Native.Standard-le0.1})-or$null-ne$snapshot.Combat.Current-or$snapshot.Combat.Roster.Count-ne0-or
        $null-ne$activation-or$snapshot.CampaignId-cne$Request.fixture.working.gameId-or
        $snapshot.AreaId-cne$Request.fixture.working.area){throw 'P04 snapshot lost real-time spent state or invented a turn.'}
    if($source){
        $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
        $path=Join-Path $root 'Manual_300_KMC_P01.zks'
        if($d.path-cne$path-or$d.nativeType-cne'Manual'-or$d.nativeCallback-ne$true-or$d.operation-cne'None'-or
            (Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length){
            throw 'P04 did not complete the actual native manual archive.'
        }
        $setup=if($casting){@('rt-casting-input','rt-casting-save-request')}elseif($approach){@('rt-approach-dispatched','rt-approach-save-request','rt-native-snapshot')}else{@('rt-repeated-attack-requested','rt-repeated-attack-resolved')}
        foreach($required in ($setup+@('rt-before-save','rt-native-save-requested'))){
            if(@($Rows|Where-Object kind -CEQ $required).Count-ne1){throw 'P04 native action setup is incomplete.'}
        }
        if($casting){
            Assert-KmcCastingSaveOutcome $Rows $saved[0]
        }elseif($approach){
            $requestRow=@($Rows|Where-Object kind -CEQ 'rt-approach-save-request')[0]
            $barrier=@($Rows|Where-Object kind -CEQ 'rt-native-snapshot')[0]
            foreach($row in @($requestRow,$barrier)){
                if($row.detail.approach.moving-ne$true-or$row.detail.approach.travelled-lt0.5-or$row.detail.approach.remaining-le4-or
                    $row.detail.resolved-ne0-or$row.detail.ordinaryAttacks-ne0-or$row.detail.inputRequests-ne1){
                    throw 'P04 did not snapshot partial movement before its native attack started.'
                }
            }
            if($barrier.gameTicks-ne$snapshot.GameTimeTicks-or$barrier.detail.snapshotCount-ne1-or
                $barrier.detail.deferredSaves-ne0-or$barrier.controls.SerializationSuspended-ne$true-or
                $requestRow.detail.snapshotCount-ne0-or$d.actual.resolved-ne0){
                throw 'P04 approach did not retain the native partial-position snapshot boundary.'
            }
        }else{
            $attacks=@($Rows|Where-Object kind -CEQ 'rt-repeated-attack-resolved')[0].detail
            if($attacks.ordinaryAttacks-lt2-or$attacks.resolved-lt2-or$attacks.forcedD20-ne0){throw 'P04 requires two naturally rolled native attacks.'}
        }
        if($projectile-or$Request.persistenceCase.EndsWith('-attack',[StringComparison]::Ordinal)){
            $activeKind=if($projectile){'rt-projectile-save-request'}else{'rt-active-attack-save-request'}
            $active=@($Rows|Where-Object kind -CEQ $activeKind)
            $deferred=@($Rows|Where-Object kind -CEQ 'rt-native-wait-started')
            if($active.Count-ne1-or$deferred.Count-ne1){throw 'P04 active save lacks its actual pre-delivery wait.'}
            if($projectile){
                $running=@($active[0].detail.projectiles|Where-Object {$_.actor-ceq$initial[0].rider.Id-and
                    $_.target-ceq$active[0].detail.target-and$_.arrived-eq$false-and$_.weapon-eq$true-and$_.resolve-eq$true})
                if($active[0].detail.riderRanged-ne$true-or$active[0].detail.ordinaryAttacks-ne3-or
                    $active[0].detail.unresolvedProjectiles-ne$true){throw 'P04 projectile request lacks native ranged launch state.'}
            }else{
                $running=@($active[0].detail.nativeCommands|Where-Object {$_.actor-ceq$initial[0].rider.Id}|
                    ForEach-Object {$_.raw}|Where-Object {$_.started-eq$true-and$_.acted-eq$false-and$_.finished-eq$false})
            }
            if($running.Count-ne1-or$active[0].detail.resolved-ne2-or$active[0].detail.snapshotCount-ne0-or
                $deferred[0].detail.nativeSaveWaiting-ne$true-or$deferred[0].detail.deferredSaves-ne1-or
                $deferred[0].detail.snapshotCount-ne0-or$d.actual.deferredSaves-ne1-or$d.actual.snapshotCount-ne1-or
                $d.actual.resolved-ne3-or$d.actual.unresolvedProjectiles-ne$false-or
                $snapshot.GameTimeTicks-le$active[0].gameTicks){
                throw 'P04 active save did not settle exactly one native attack before the actual snapshot.'
            }
        }
    }else{
        if($initial[0].persistence.semantics-ne$snapshot.Combat.Actors.Count-or
            $initial[0].persistence.presentation-ne$(if($mounted){1}else{0})-or$initial[0].controls.NativeCastRequestCount-ne0){
            throw 'P04 cold restoration duplicated semantic state or replayed Mount.'
        }
    }
    if($casting){
        $continuation=@($Rows|Where-Object kind -CEQ 'rt-casting-continuation')
        $first=@($Rows|Where-Object kind -CEQ 'rt-casting-first-delivery')
        if($continuation.Count-ne1-or$first.Count-ne1-or$continuation[0].detail.resolved-ne0-or
            $first[0].detail.resolved-ne1-or$first[0].detail.inputRequests-ne1-or
            $continuation[0].detail.casting.slotAvailable-ne$false-or$first[0].detail.casting.slotAvailable-ne$false-or
            $first[0].detail.casting.heals-ne$(if($source){1}else{0})){
            throw 'P04 casting continuation replayed a spell or replenished its slot.'
        }
    }
    if($approach){
        $first=@($Rows|Where-Object kind -CEQ 'rt-approach-first-delivery')
        $continue=@($Rows|Where-Object kind -CEQ 'rt-approach-continuation')
        if($first.Count-ne1-or$continue.Count-ne1-or$first[0].detail.resolved-ne1-or
            $continue[0].detail.resolved-ne0-or$continue[0].detail.ordinaryAttacks-ne0-or
            $first[0].detail.inputRequests-ne1-or$continue[0].detail.inputRequests-ne1-or
            $first[0].detail.riderRounds-ne$continue[0].detail.riderRounds){
            throw 'P04 approach continuation replayed input, attack or preparation.'
        }
    }
    $queued=@($Rows|Where-Object kind -CEQ 'rt-spent-attack-queued')
    $wait=@($Rows|Where-Object kind -CEQ 'rt-native-debt-wait')
    $later=@($Rows|Where-Object kind -CEQ 'rt-later-attack')
    $end=@($Rows|Where-Object kind -CEQ 'usable-continuation-complete')
    if($queued.Count-ne1-or$wait.Count-ne1-or$later.Count-ne2-or$end.Count-ne1-or
        $queued[0].rider.Standard-le0-or$wait[0].gameTicks-ge$queued[0].detail.readyTicks){
        throw 'P04 lacks a real wait for spent native debt and two later attacks.'
    }
    for($i=0;$i-lt2;$i++){
        if($later[$i].gameTicks+100000-lt$queued[0].detail.readyTicks-or
            $later[$i].detail.resolved-ne($queued[0].detail.resolved+$i+1)-or
            $later[$i].detail.riderRounds-ne($queued[0].detail.riderRounds+$i+1)-or
            $later[$i].detail.forcedD20-ne0){throw 'P04 loaded work fired early, duplicated or refreshed incorrectly.'}
    }
}

# Evidence-only comparison after the cold native result. No expected gameplay
# values are sent to the game or written into an archive.
function Assert-KmcProjectileColdOutcome {
    param($SourceRows,$ColdRows)
    $written=@($SourceRows|Where-Object kind -CEQ 'native-write-complete')
    $loaded=@($ColdRows|Where-Object kind -CEQ 'rt-cold-debt-restored')
    if($written.Count-ne1-or$loaded.Count-ne1){throw 'Projectile comparison lacks exact native source/cold observations.'}
    $a=$written[0].detail.actual;$b=$loaded[0].detail.actual
    if($written[0].processId-eq$loaded[0].processId-or$a.target-cne$b.target-or
        $a.targetDamage-ne$b.targetDamage-or$a.riderWeapon-cne$b.riderWeapon-or
        $a.riderRanged-ne$true-or$b.riderRanged-ne$true-or
        $a.unresolvedProjectiles-ne$false-or$b.unresolvedProjectiles-ne$false-or
        $b.resolved-ne0-or$b.ordinaryAttacks-ne0-or$b.rules.pairDamageRules-ne0-or
        $b.rules.pairDamage-ne0){
        throw 'Cold projectile outcome changed native health/equipment or replayed delivery.'
    }
}

function Assert-KmcRealtimeColdSource {
    param([string]$SourceRunId,$Request)
    [void](Get-KmcPersistenceSource -SourceRunId $SourceRunId -ExpectedSha256 $Request.persistenceLoad.sha256 -Fixture $Request.fixture -NativeCase $Request.persistenceCase)
    $root=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$SourceRunId)
    Assert-KmcDirectoryTreeCloneable $root 'completed projectile evidence'
    $result=Read-KmcJson (Join-Path $root 'runtime-result.json')
    $manifestPath=Join-Path $root 'runtime-artifacts.json'
    if((Get-KmcSha256 $manifestPath)-cne$result.evidenceManifestSha256){throw 'Source projectile manifest changed.'}
    $manifest=Read-KmcJson $manifestPath
    $artifact=@($manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    $path=Join-Path $root 'persistence-observations.jsonl'
    if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'-or
        (Get-KmcSha256 $path)-cne$artifact[0].sha256-or(Get-Item -LiteralPath $path).Length-ne$artifact[0].length){
        throw 'Source projectile observations changed.'
    }
    $sourceRows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    $coldRows=@(Get-Content -LiteralPath (Join-Path $Request.evidenceRoot 'persistence-observations.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
    if($Request.persistenceCase-ceq'suspended'){
        Assert-KmcSuspendedColdOutcome $sourceRows $coldRows
    }elseif($Request.persistenceCase-cin @('condition','condition-preparing')){
        Assert-KmcConditionColdOutcome $sourceRows $coldRows
    }elseif($Request.persistenceCase.EndsWith('-approach',[StringComparison]::Ordinal)){
        Assert-KmcApproachColdOutcome $sourceRows $coldRows
    }elseif($Request.persistenceCase.EndsWith('-casting',[StringComparison]::Ordinal)){
        Assert-KmcCastingColdOutcome $sourceRows $coldRows
    }else{Assert-KmcProjectileColdOutcome $sourceRows $coldRows}
}

function Assert-KmcCastingSaveOutcome {
    param($Rows,$Written)
    $request=@($Rows|Where-Object kind -CEQ 'rt-casting-save-request')
    $wait=@($Rows|Where-Object kind -CEQ 'rt-native-wait-started')
    if($request.Count-ne1-or$wait.Count-ne1){throw 'Casting save lacks its actual live-action wait.'}
    $before=$request[0].detail;$after=$Written.detail.actual
    $a=$before.casting;$b=$after.casting
    $commands=@($a.commands|Where-Object {$_.blueprint-ceq$a.blueprint-and$_.started-eq$true-and$_.acted-eq$false-and$_.finished-eq$false})
    if($commands.Count-ne1-or$a.caster-cne$b.caster-or$a.subject-cne$b.subject-or$a.blueprint-cne$b.blueprint-or
        $a.slotCount-ne1-or$b.slotCount-ne1-or$a.availableSlots-ne1-or$b.availableSlots-ne0-or
        $a.slotAvailable-ne$true-or$b.slotAvailable-ne$false-or$b.spellAvailable-ne$false-or
        $a.inputs-ne1-or$b.inputs-ne1-or$a.heals-ne0-or$b.heals-ne1-or$a.damage-le0-or$b.damage-ge$a.damage-or
        $before.snapshotCount-ne0-or$wait[0].detail.nativeSaveWaiting-ne$true-or$wait[0].detail.snapshotCount-ne0-or
        $wait[0].detail.deferredSaves-ne1-or$after.deferredSaves-ne1-or$after.snapshotCount-ne1-or
        $after.unresolvedAbilities-ne$false-or$after.unresolvedProjectiles-ne$false-or$b.standard-le0.1-or
        $Written.detail.snapshot.GameTimeTicks-le$request[0].gameTicks){
        throw 'Casting save lost or replayed native effects, slot expenditure or the settlement barrier.'
    }
}

function Assert-KmcCastingColdOutcome {
    param($SourceRows,$ColdRows)
    $written=@($SourceRows|Where-Object kind -CEQ 'native-write-complete')
    $loaded=@($ColdRows|Where-Object kind -CEQ 'rt-cold-debt-restored')
    if($written.Count-ne1-or$loaded.Count-ne1-or$written[0].processId-eq$loaded[0].processId){
        throw 'Casting comparison lacks a completed source and fresh cold process.'
    }
    $a=$written[0].detail.actual.casting;$actual=$loaded[0].detail.actual;$b=$actual.casting
    if($a.caster-cne$b.caster-or$a.subject-cne$b.subject-or$a.blueprint-cne$b.blueprint-or
        $a.damage-ne$b.damage-or$a.slotCount-ne$b.slotCount-or$a.availableSlots-ne0-or$b.availableSlots-ne0-or
        $a.slotAvailable-ne$false-or$b.slotAvailable-ne$false-or$b.spellAvailable-ne$false-or
        $a.heals-ne1-or$b.heals-ne0-or$b.inputs-ne0-or$actual.inputRequests-ne0-or
        $actual.resolved-ne0-or$actual.unresolvedAbilities-ne$false-or$actual.unresolvedProjectiles-ne$false){
        throw 'Cold casting outcome changed native health/slots or replayed an effect.'
    }
}

function Assert-KmcApproachColdOutcome {
    param($SourceRows,$ColdRows)
    $barrier=@($SourceRows|Where-Object kind -CEQ 'rt-native-snapshot')
    $loaded=@($ColdRows|Where-Object kind -CEQ 'rt-cold-approach-restored')
    if($barrier.Count-ne1-or$loaded.Count-ne1-or$barrier[0].processId-eq$loaded[0].processId){
        throw 'Approach comparison lacks its native snapshot and fresh cold process.'
    }
    $a=$barrier[0].detail;$b=$loaded[0].detail.actual
    if($a.target-cne$b.target-or$a.targetDamage-ne$b.targetDamage-or$b.resolved-ne0-or$b.ordinaryAttacks-ne0-or
        $b.inputRequests-ne0-or$b.unresolvedProjectiles-ne$false){
        throw 'Cold approach lost its native outcome or replayed transient intent.'
    }
    foreach($name in @('riderPosition','mountPosition')){
        if($a.$name.Count-ne3-or$b.$name.Count-ne3){throw 'Approach native positions are incomplete.'}
        $dx=[double]$a.$name[0]-[double]$b.$name[0];$dz=[double]$a.$name[2]-[double]$b.$name[2]
        if([double]::IsNaN($dx)-or[double]::IsInfinity($dx)-or[double]::IsNaN($dz)-or[double]::IsInfinity($dz)-or
            [Math]::Sqrt($dx*$dx+$dz*$dz)-gt0.2){throw 'Cold approach lost the native saved horizontal position.'}
    }
}

function Assert-KmcAlternatingPersistenceEvidence {
    param($Request,$Rows,$GameResult)
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'Alternating fixture lacks its initial native world.'}
    foreach($row in $Rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.checkpoint-cne'alternating'-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.relationship-cnotin @('Mounted','Unmounted')-or$row.controls.DuplicateFactCount-ne0){
            throw 'Alternating native identity/control invariant differs.'
        }
    }
    $source=$Request.scenario-ceq'persistence-p05-save'
    $writes=@($Rows|Where-Object kind -CEQ 'alternate-native-write-complete')
    $expectedWrites=if($source){2}else{1}
    if($writes.Count-ne$expectedWrites-or@($Rows|Where-Object kind -CEQ 'alternate-write-requested').Count-ne$expectedWrites){
        throw 'Alternating fixture lacks its actual native write requests/completions.'
    }
    for($i=0;$i-lt$writes.Count;$i++){
        $d=$writes[$i].detail
        $mounted=-not($source-and$i-eq1)
        $label=if(-not$source){'post-cold'}elseif($i-eq0){'A'}else{'B'}
        $leaf=if(-not$source){'Manual_302_KMC_P05_POST.zks'}elseif($i-eq0){'Manual_300_KMC_P01.zks'}else{'Manual_301_KMC_P05_UNMOUNTED.zks'}
        $path=Join-Path $root $leaf
        if($d.label-cne$label-or$d.ordinal-ne($i+1)-or$d.path-cne$path-or$d.nativeType-cne'Manual'-or
            $d.nativeCallback-ne$true-or$d.operation-cne'None'-or$d.snapshot.Mounted-ne$mounted-or
            (Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length-or
            $d.snapshot.CampaignId-cne$Request.fixture.working.gameId-or$d.snapshot.AreaId-cne$Request.fixture.working.area){
            throw 'Alternating actual native archive differs from its observed snapshot.'
        }
        if($mounted){
            if($d.snapshot.Rider.Id-cne$initial[0].rider.Id-or$d.snapshot.Mount.Id-cne$initial[0].mount.Id){throw 'Alternating mounted archive lost actor identity.'}
        }elseif($null-ne$d.snapshot.Rider-or$null-ne$d.snapshot.Mount-or$null-ne$d.snapshot.ProfileId){
            throw 'Unmounted B contains a stale pair.'
        }
    }
    if(@(Get-ChildItem -LiteralPath $root -File -Filter '*.zks').Count-ne3){throw 'Alternating fixture archive inventory differs.'}
    if($source){
        foreach($kind in @('alternate-voluntary-dismount','alternating-source-complete')){
            $row=@($Rows|Where-Object kind -CEQ $kind)
            if($row.Count-ne1-or$row[0].relationship-cne'Unmounted'){throw 'B lacks real native dismount/continuation.'}
        }
        if($writes[0].detail.sha256-ceq$writes[1].detail.sha256){throw 'A and B were not distinct native archives.'}
    }else{
        foreach($descriptor in @($Request.persistenceLoad,$Request.persistenceAlternate)){
            if((Get-KmcSha256 (Join-Path $root $descriptor.fileName))-cne$descriptor.sha256){throw 'Alternating cold input was changed.'}
        }
        $worlds=@($Rows|Where-Object kind -CEQ 'alternate-world-loaded')
        if($worlds.Count-ne3-or@($Rows|Where-Object kind -CEQ 'alternate-load-requested').Count-ne2){throw 'A/B/A native loads are incomplete.'}
        for($i=0;$i-lt3;$i++){
            $d=$worlds[$i].detail;$mounted=$i-ne1
            $label=if($mounted){'A'}else{'B'}
            $state=if($mounted){'Mounted'}else{'Unmounted'}
            if($d.index-ne$i-or$d.label-cne$label-or$d.mounted-ne$mounted-or$worlds[$i].relationship-cne$state-or
                $d.semanticCount-ne$(if($i-eq2){4}else{2})-or$d.presentationCount-ne$(if($i-eq2){2}else{1})-or
                $d.freshNativeObjects-ne$true){throw 'Alternating load carried a previous world/pair or replayed restoration.'}
        }
        foreach($kind in @('alternating-cycle-complete','movement-dispatched','movement-completed','attack-dispatched','attack-delivered','usable-continuation-complete')){
            $row=@($Rows|Where-Object kind -CEQ $kind)
            if($row.Count-ne1-or$row[0].relationship-cne'Mounted'){throw 'Alternating cold play is incomplete.'}
        }
        $attack=@($Rows|Where-Object kind -CEQ 'attack-delivered')[0]
        if($attack.detail.rules-lt1-or$attack.detail.rolls-lt1){throw 'Alternating cold attack did not resolve natively.'}
    }
}

function Assert-KmcPersistenceScenarioEvidence {
    param($Request,$Manifest,[string]$Status,$GameResult)
    if($Request.scenario-ceq'persistence-p07-load'-and(Get-KmcOptionalMember $Request 'persistenceCase')-ceq'removal-no-dll'){
        # The genuine no-DLL load: KMC wrote nothing in that process. The removal
        # observer's own result was validated by Test-KmcObserverResult during the
        # run and composed into this game result; the binding is re-checked here.
        if($Status-cne'PASS'){return}
        if($null-eq$GameResult-or[string](Get-KmcOptionalMember $GameResult 'evidenceKind')-cne'kmc-removal-observer'-or
            (Get-KmcOptionalMember $GameResult 'kmcDllInstalled')-ne$false-or$GameResult.observations.absence.kmcAssemblyLoaded-ne$false-or
            $GameResult.observations.absence.kmcModsDirectoryPresent-ne$false-or@($GameResult.observations.absence.kmcHarmonyOwners).Count-ne0-or
            [string]$GameResult.observerResultSha256-cnotmatch'^[0-9a-f]{64}$'-or
            (Get-KmcSha256 (Join-Path $Request.evidenceRoot 'observer-result.json'))-cne[string]$GameResult.observerResultSha256-or
            [int]$GameResult.checkFailCount-ne0-or[int]$GameResult.checkPassCount-lt12){
            throw 'The no-DLL observation is not a genuine bound removal-observer result.'
        }
        return
    }
    if($Request.scenario -cnotin @('persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load') -or $Status-cne'PASS'){return}
    $artifact=@($Manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'){throw 'P01 has no exact observation artifact.'}
    $path=Join-Path $Request.evidenceRoot 'persistence-observations.jsonl'
    if((Get-KmcSha256 $path)-cne$artifact[0].sha256){throw 'P01 observations changed.'}
    $rows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    $absentKmc=$Request.scenario-ceq'persistence-p07-load'-and$Request.persistenceCase-ceq'absent-kmc'
    $hasCase=@($Request.PSObject.Properties.Name)-ccontains'persistenceCase'
    $deathCold=$Request.scenario-ceq'persistence-p07-load'-and$hasCase-and$Request.persistenceCase-cin @('rider-death','mount-death')
    $deathSave=$Request.scenario-ceq'persistence-p07-save'-and$hasCase-and$Request.persistenceCase-cin @('rider-death','mount-death')
    $eligibilitySave=$Request.scenario-ceq'persistence-p07-save'-and$hasCase-and$Request.persistenceCase-ceq'rider-size-change'
    $eligibilityCold=$Request.scenario-ceq'persistence-p07-load'-and$hasCase-and$Request.persistenceCase-ceq'rider-size-change'
    $campaignBCold=$Request.scenario-ceq'persistence-p07-load'-and$hasCase-and$Request.persistenceCase-ceq'campaign-b'
    if($rows.Count-lt$(if($deathCold-or$eligibilityCold){2}elseif($absentKmc-or$campaignBCold){4}else{6})-or$rows.Count-gt20){throw 'Persistence observation count is invalid.'}
    if($Request.scenario-ceq'persistence-p06-load'){
        Assert-KmcValidationPersistenceEvidence $Request $rows $GameResult
        return
    }
    if($Request.scenario-cin @('persistence-p04-save','persistence-p04-load')){
        Assert-KmcRealtimePersistenceEvidence $Request $rows $GameResult
        return
    }
    if($Request.scenario-cin @('persistence-p05-save','persistence-p05-load')-and$Request.persistenceCase-ceq'alternating'){
        Assert-KmcAlternatingPersistenceEvidence $Request $rows $GameResult
        return
    }
    if($Request.scenario-cin @('persistence-p03-save','persistence-p03-load')-and$Request.persistenceCase-cin @('condition','condition-preparing')){
        Assert-KmcConditionPersistenceEvidence $Request $rows $GameResult
        return
    }
    $isP03=$Request.scenario-cin @('persistence-p03-save','persistence-p03-load')
    $isSlot=$Request.scenario-cin @('persistence-p05-save','persistence-p05-load')
    $isQueued=$isSlot-and$Request.persistenceCase-ceq'queued'
    $isCombat=$Request.scenario-cin @('persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load')
    $checkpoint=if(@($Request.PSObject.Properties.Name)-ccontains'persistenceCase'){[string]$Request.persistenceCase}else{'partial-movement'}
    $isSuspended=$isP03-and$checkpoint-ceq'suspended'
    $isRoundEffect=$isP03-and$checkpoint-cin @('round-effect','suspended')
    $isReaction=$isP03-and$checkpoint-ceq'reaction'
    $isCommitment=$isP03-and-not$isRoundEffect-and-not$isReaction
    $isWrite=$Request.scenario-cin @('persistence-p07-save','persistence-p01-save','persistence-p02-save','persistence-p03-save','persistence-p04-save','persistence-p05-save')
    $initial=@($rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P01 has no unique initial state.'}
    $isDisable=$checkpoint-ceq'disable-reenable'
    # Every case whose walk legitimately leaves A's pair for a while: rows
    # outside A carry no actor at all, and every mounted row names A's exact two.
    $isCampaignB=$Request.scenario-ceq'persistence-p07-save'-and$checkpoint-cin @('campaign-b','prepare-removal','disable-during-load','rider-death','mount-death','rider-size-change')
    if($absentKmc-or$deathCold-or$eligibilityCold-or$campaignBCold){$isCampaignB=$true}
    foreach($row in $rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or
            $row.controls.DuplicateFactCount-ne0){throw 'P01 native state identity/relationship/control invariant differs.'}
        # The disable case is the only one whose relationship legitimately leaves
        # Mounted, because that transition is the behaviour under test. Its rows
        # must still name the same two actors whenever a pair exists at all.
        # Campaign B leaves A entirely for a while: its rows outside A carry no
        # pair at all, and every mounted row still names A's exact two actors.
        if($isDisable-or$isCampaignB){
            if($row.relationship-cnotin @('Mounted','Unmounted')){
                throw 'P07 disable row reports a relationship state outside mounted and unmounted.'
            }
            if($row.relationship-ceq'Mounted'-and
                ($row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id)){
                throw 'P07 disable row changed the owned pair actors.'
            }
            # A dismounted row may still describe A's own two actors (they exist,
            # only the pair is gone); it may never describe foreign ones. Rows
            # outside A's world carrying no actor at all are the campaign-B
            # validator's own stricter contract.
            if($isCampaignB-and$row.relationship-cne'Mounted'-and(
                ($null-ne$row.rider-and($null-eq$initial[0].rider-or$row.rider.Id-cne$initial[0].rider.Id))-or
                ($null-ne$row.mount-and($null-eq$initial[0].mount-or$row.mount.Id-cne$initial[0].mount.Id)))){
                throw 'P07 row outside the pair describes foreign actors.'
            }
        }
        elseif($row.relationship-cne'Mounted'-or$row.rider.Id-cne$initial[0].rider.Id-or
            $row.mount.Id-cne$initial[0].mount.Id){
            throw 'P01 native state identity/relationship/control invariant differs.'
        }
        if(($isCombat-or$isSlot)-and$row.checkpoint-cne$checkpoint){throw 'P02 observation checkpoint differs from its bounded request.'}
    }
    $required=@('usable-continuation-complete')
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent','suspended')){$required+=@('movement-dispatched','movement-completed')}
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent','between-partner-orders','suspended')){$required+=@('attack-dispatched','attack-delivered')}
    # The integration-absent process has no pair to continue with: its whole
    # claim is the clean native load itself.
    if($absentKmc){$required=@('absent-load-complete','absent-moved','absent-saved')}
    if($eligibilitySave){$required=@('eligibility-reloaded')}
    if($eligibilityCold){$required=@('eligibility-cold-complete')}
    if($campaignBCold){$required=@('campaign-b-cold-loaded','campaign-b-cold-moved','campaign-b-cold-saved')}
    # A death boundary ends with a dead rider or mount: there is no pair to
    # continue with, so the claim is the no-pair save and its reloads alone.
    if($deathSave){$required=@('death-reloaded')}
    if($deathCold){$required=@('death-cold-complete')}
    if($isCombat-and-not$isRoundEffect-and-not$isReaction-and$checkpoint-cne'partial-movement'){$required+=@('spent-work-input-before','spent-work-rejected')}
    if($isCombat-and$isWrite-and-not$isP03){
        $required+=@('partial-movement-dispatched','partial-movement-completed')
        if($checkpoint-cin @('rider-spent','exhausted')){$required+=@('setup-rider-attack-dispatched','setup-rider-attack-delivered')}
        if($checkpoint-cin @('between-partner-orders','exhausted')){$required+=@('setup-mount-attack-dispatched','setup-mount-attack-delivered')}
        if($checkpoint-ceq'explicit-end'){$required+='explicit-end-requested'}
    }
    if($isCommitment){
        if($isWrite){$required+=@('commitment-dispatched','commitment-created')}
        if($checkpoint-ceq'step'){$required+=@('ordinary-movement-input-before','ordinary-movement-rejected','step-remainder-dispatched','step-remainder-completed')}
        else{$required+=@('converted-standard-rejected','conversion-remainder-dispatched','conversion-remainder-completed')}
    }
    if($isReaction){
        $required+=@('reaction-return-dispatched','reaction-repeat-dispatched','reaction-repeat-rejected')
        if($isWrite){$required+=@('reaction-approach-dispatched','reaction-approach-completed','reaction-dispatched','reaction-consumed')}
        else{$required+='reaction-loaded'}
    }
    if($isRoundEffect){
        $required+='round-effect-retained'
        if($isWrite){$required+=@('round-effect-provisioned','round-effect-applied')}
        else{$required+='round-effect-loaded'}
    }
    foreach($kind in $required){
        if(@($rows|Where-Object kind -CEQ $kind).Count-ne1){throw "Persistence is missing native outcome: $kind"}
    }
    foreach($attack in @($rows|Where-Object kind -CMatch '^(attack|setup-rider-attack|setup-mount-attack)-delivered$')){
        if($attack.detail.rules-lt1-or$attack.detail.rolls-lt1){throw 'Persistence has no actual native attack outcome.'}
    }
    if($isCombat){
        $refresh=@($rows|Where-Object kind -CEQ 'next-paired-activation')
        if($refresh.Count-ne2-or$refresh[1].detail.sequence-ne($refresh[0].detail.sequence+1)){
            throw 'P02 did not observe two successive real paired activations.'
        }
        if($isReaction){
            $repeated=@($rows|Where-Object kind -CEQ 'reaction-repeat-rejected')[0]
            $expected=if($isWrite){1}else{0}
            if($repeated.mount.ReactionsRemaining-ne0-or
                $repeated.detail.rules.mountOpportunityAttackRules-ne$expected){throw 'P03 consumed reaction repeated or refreshed.'}
            foreach($next in $refresh){
                if($next.mount.ReactionsRemaining-ne1-or$next.mount.Reaction-ne0){throw 'P03 next native reaction refresh missing.'}
            }
        }
        if($isRoundEffect){
            for($i=0;$i-lt2;$i++){
                foreach($actor in @('rider','mount')){
                    $effect=$refresh[$i].detail.roundEffects.$actor
                    if($null-eq$effect-or$effect.rounds-ne($i+2)-or$effect.damage-ne(1-$i)-or
                        -not$effect.active-or$effect.suppressed){throw 'P03 next native round effect was duplicated or missing.'}
                }
            }
        }
        if($checkpoint-cin @('partial-movement','rider-spent','suspended')){
            $continuation=@($rows|Where-Object kind -CEQ 'movement-completed')[0]
            if($continuation.mount.Move-le0-or$continuation.rider.Move-ne0){throw 'P02 lost transport expenditure or taxed the rider.'}
        }
        $final=@($rows|Where-Object kind -CEQ 'usable-continuation-complete')[0]
        if(@($final.detail.turnVisits).Count-lt4){throw 'P02 lacks observed native unrelated participation.'}
    }
    if($Request.scenario-ceq'persistence-p07-save'){
        if($Request.persistenceCase-cin @('area-reload','area-cross-entry','area-cross-exit')){Assert-KmcAreaPersistenceEvidence $Request $rows}
        else{Assert-KmcRecoveryPersistenceEvidence $Request $rows}
    }
    if($Request.scenario-ceq'persistence-p07-load'-and$Request.persistenceCase-cin @('area-cross-entry-auto','area-cross-exit-auto')){
        Assert-KmcTransitionAutoColdEvidence $Request $rows
    }
    if($absentKmc){Assert-KmcAbsentLoadEvidence $Request $rows}
    if($deathCold){Assert-KmcDeathColdEvidence $Request $rows}
    if($eligibilityCold){Assert-KmcEligibilityColdEvidence $Request $rows}
    if($campaignBCold){Assert-KmcCampaignBColdEvidence $Request $rows}
    if($isSuspended){Assert-KmcSuspendedEvidence $rows $isWrite}
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    if($isWrite){
        $written=@($rows|Where-Object kind -CEQ 'native-write-complete')
        if($written.Count-ne1){throw 'P01 save has no real completion observation.'}
        $d=$written[0].detail
        if($isP03){Assert-KmcP03Snapshot $d.snapshot $checkpoint}
        elseif($isCombat){Assert-KmcP02Snapshot $d.snapshot $checkpoint}
        $type=if($isSlot-and$checkpoint-ceq'quick'){'Quick'}elseif($isSlot-and$checkpoint-ceq'auto'){'Auto'}else{'Manual'}
        $leaf=if($isQueued){'Manual_302_KMC_P01.zks'}elseif($type-ceq'Manual'){'Manual_300_KMC_P01.zks'}else{$type+'_1.zks'}
        $archive=Join-Path $root $leaf
        if($isQueued){Assert-KmcQueuedSaveEvidence $rows $root}
        if($isSlot-and-not$isQueued){
            $requests=@($rows|Where-Object kind -CEQ 'native-slot-write-requested')
            $commits=@($rows|Where-Object kind -CIn @('native-slot-write-complete','native-write-complete'))
            if($requests.Count-ne3-or$commits.Count-ne3){throw 'P05 lacks all three native requests/completions.'}
            for($i=0;$i-lt3;$i++){
                $r=$requests[$i].detail;$w=$commits[$i].detail
                if($r.ordinal-ne($i+1)-or$r.overwrite-ne($i-gt0)-or$r.nativeType-cne$type-or
                    ($type-cne'Manual'-and$r.slotLimit-ne1)-or($type-ceq'Auto'-and$r.autosaveEnabled-ne$true)-or
                    $w.ordinal-ne($i+1)-or$w.path-cne$archive-or$w.nativeType-cne$type-or
                    $w.nativeCallback-ne$true-or$w.operation-cne'None'-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
                    $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$initial[0].rider.Id-or
                    $w.snapshot.Mount.Id-cne$initial[0].mount.Id){throw 'P05 lost actual native rotation/metadata semantics.'}
            }
            if(@($commits|ForEach-Object{$_.detail.sha256}|Select-Object -Unique).Count-ne3){throw 'P05 did not write three distinct native archives.'}
            if(@(Get-ChildItem -LiteralPath $root -File -Filter '*.zks').Count-ne2){throw 'P05 retained an unexpected archive or temporary leaf.'}
        }
        if($d.path-cne$archive-or$d.nativeType-cne$type-or$d.nativeCallback-ne$true-or$d.operation-cne'None'-or
            (Get-KmcSha256 $archive)-cne$d.sha256-or(Get-Item $archive).Length-ne$d.length){
            throw 'P01 real archive/metadata/completion differs.'
        }
        # The disable case writes its archive after cleanup on purpose, so the
        # recorded snapshot must carry no pair at all rather than the mounted one
        # every other write case records.
        if($isDisable){
            if($d.snapshot.Mounted-ne$false-or$null-ne$d.snapshot.Rider-or$null-ne$d.snapshot.Mount){
                throw 'P07 cleanup-state archive recorded a mounted pair.'
            }
        }
        elseif($d.snapshot.Mounted-ne$true-or$d.snapshot.Rider.Id-cne$initial[0].rider.Id-or
            $d.snapshot.Mount.Id-cne$initial[0].mount.Id){throw 'P01 real archive/metadata/completion differs.'}
    }else{
        # Only a transition autosave cold case makes an ordinary subsequent
        # write, to prove the restored world still supports one. Every other
        # cold process still writes nothing, and neither may disturb the
        # archive it loaded.
        $autoCold=$Request.scenario-ceq'persistence-p07-load'-and$Request.persistenceCase-cin @('area-cross-entry-auto','area-cross-exit-auto')
        $coldWrites=@($rows|Where-Object kind -CEQ 'native-write-complete')
        if($coldWrites.Count-ne$(if($autoCold){1}else{0})){throw 'P01 cold process unexpectedly wrote a save.'}
        $archive=Join-Path $root $Request.persistenceLoad.fileName
        if((Get-KmcSha256 $archive)-cne$Request.persistenceLoad.sha256){throw 'P01 cold process did not retain its selected archive.'}
        if($autoCold-and($coldWrites[0].detail.path-ceq$archive-or$coldWrites[0].detail.sha256-ceq$Request.persistenceLoad.sha256)){
            throw 'P07 transition autosave cold write replaced the archive it loaded.'
        }
    }
}

function Assert-KmcConditionSnapshot {
    param($Snapshot)
    $c=$Snapshot.Combat;$p=$c.Paired;$a=$p.Activation
    $r=@($c.Actors|Where-Object {$_.Native.Id-ceq$p.RiderId})
    $m=@($c.Actors|Where-Object {$_.Native.Id-ceq$p.MountId})
    if($Snapshot.Mounted-ne$false-or$c.TurnBased-ne$true-or$c.Round-lt1-or$a.Split-ne$true-or
        $a.Mount.Ended-ne$true-or$a.Rider.Ended-ne$false-or$a.Mount.ForfeitRecorded-ne$true-or$a.Mount.ForfeitAdded-le0-or
        $p.BoundaryIsCurrent-ne$true-or$c.Current.ActorId-cne$p.RiderId-or$r.Count-ne1-or$m.Count-ne1-or
        $r[0].Native.Standard-ne0-or$r[0].Native.Move-ne0-or$m[0].Native.Standard-le0){
        throw 'P03 condition snapshot lost native forfeit, split or principal remainder.'
    }
}

function Assert-KmcConditionColdOutcome {
    param($SourceRows,$ColdRows)
    $source=@($SourceRows|Where-Object kind -CEQ 'native-write-complete')
    $cold=@($ColdRows|Where-Object kind -CEQ 'initial')
    if($source.Count-ne1-or$cold.Count-ne1){throw 'P03 condition source/cold identity is missing.'}
    $s=$source[0];$c=$cold[0];$a=$s.detail.condition;$b=$c.detail
    if($s.processId-eq$c.processId-or$s.rider.Id-cne$c.rider.Id-or$s.mount.Id-cne$c.mount.Id-or
        $s.native.targetId-cne$c.native.targetId-or$a.damage-le0-or$a.damage-ne$b.damage-or
        $b.nativeRiderPreparations-ne0-or$b.nativeMountPreparations-ne0-or
        $b.conditionActive-ne$false-or$b.nativePartPresent-ne$false-or$null-ne$b.stimulus-or$null-ne$b.fact-or
        $c.controls.NativeCastRequestCount-ne0-or$c.relationship-cne'Unmounted'){
        throw 'P03 cold condition replayed preparation/stimulus, lost native harm, or used another world.'
    }
}

function Assert-KmcConditionPreparationEvidence {
    param($Rows)
    $request=@($Rows|Where-Object kind -CEQ 'condition-preparation-save-request')
    $wait=@($Rows|Where-Object kind -CEQ 'condition-preparation-wait')
    $barrier=@($Rows|Where-Object kind -CEQ 'condition-preparation-barrier')
    if($request.Count-ne1-or$wait.Count-ne1-or$barrier.Count-ne1){
        throw 'P03 preparation save lacks its unique request, live wait or barrier.'
    }
    $r=$request[0].detail;$w=$wait[0].detail;$b=$barrier[0].detail
    if($r.nativePreparing-ne$true-or$r.commandPresent-ne$false-or$r.snapshotCount-ne0-or
        $w.waiting-ne$true-or$w.ownedPreparationStart-ne$true-or$w.unownedOrdinaryStart-ne$false-or
        $w.deferredSaves-ne1-or$w.snapshotCount-ne0-or
        $w.condition.command.type-cne'Kingmaker.UnitLogic.Commands.UnitSelfHarm'-or
        $w.condition.command.ignoreCooldown-ne$false-or$b.snapshotCount-ne0-or$b.deferredSaves-ne1-or
        $b.condition.command.started-ne$true-or$b.condition.command.finished-ne$true-or
        $b.condition.command.result-cne'Success'-or$b.condition.damage-le0-or
        $b.condition.mountEnded-ne$true-or$b.condition.riderEnded-ne$false){
        throw 'P03 preparation save did not preserve its exact native completion and costs.'
    }
}

function Assert-KmcConditionPersistenceEvidence {
    param($Request,$Rows,$GameResult)
    $write=$Request.scenario-ceq'persistence-p03-save'
    if($write-and$Request.persistenceCase-ceq'condition-preparing'){Assert-KmcConditionPreparationEvidence $Rows}
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    $before=@($Rows|Where-Object kind -CEQ 'condition-remainder-before-input')
    $final=@($Rows|Where-Object kind -CEQ 'usable-continuation-complete')
    if($initial.Count-ne1-or$before.Count-ne1-or$final.Count-ne1){throw 'P03 condition lacks unique initial/remainder/final states.'}
    foreach($row in $Rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.checkpoint-cne$Request.persistenceCase-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or$row.controls.DuplicateFactCount-ne0-or
            ($row.kind-cnotin @('initial','condition-command-observed','condition-preparation-save-request','condition-preparation-wait')-and$row.relationship-cne'Unmounted')){
            throw 'P03 condition native identity or control invariant differs.'
        }
    }
    if($before[0].detail.mountEnded-ne$true-or$before[0].detail.riderEnded-ne$false-or
        $before[0].rider.Standard-ne0-or$before[0].mount.Standard-le0-or$before[0].detail.damage-le0-or
        $before[0].detail.conditionActive-ne$false-or$before[0].detail.nativePartPresent-ne$false){
        throw 'P03 condition remainder invents actions or loses native harm.'
    }
    $attack=@($Rows|Where-Object kind -CEQ 'attack-delivered')
    $next=@($Rows|Where-Object kind -CEQ 'next-independent-activation')
    if($attack.Count-ne1-or$attack[0].detail.rules-lt1-or$attack[0].detail.rolls-lt1-or$next.Count-ne4-or
        $final[0].detail.riderPreparations-ne2-or$final[0].detail.mountPreparations-ne2-or
        @($final[0].detail.turnVisits).Count-lt4){throw 'P03 condition lacks legal attack and two later native refreshes.'}
    foreach($id in @($initial[0].rider.Id,$initial[0].mount.Id)){
        $turns=@($next|Where-Object {$_.detail.actor-ceq$id})
        if($turns.Count-ne2-or$turns[0].detail.count-ne1-or$turns[1].detail.count-ne2-or
            $turns[0].detail.round-le$before[0].detail.combat.round-or
            $turns[1].detail.round-le$turns[0].detail.round){throw 'P03 split actor repeated same-round work or missed a real refresh.'}
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    if($write){
        $retained=@($Rows|Where-Object kind -CEQ 'condition-forfeit-retained')
        $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
        if($retained.Count-ne1-or$written.Count-ne1){throw 'P03 condition lacks actual source outcome/write.'}
        $c=$retained[0].detail;$d=$written[0].detail
        if($c.stimulus.nativeSelfDamageRules-ne1-or$c.stimulus.choiceOverrides-ne1-or
            $c.stimulus.cleanupResourcesUnchanged-ne$true-or$c.stimulus.directControlRestored-ne$true-or
            $c.fact.damageRestorationEnabled-ne$false-or$c.fact.healPerRound-ne0-or$c.fact.restored-ne$true-or
            $c.damage-le$c.originalDamage-or$c.damage-ne$d.condition.damage){
            throw 'P03 condition fixture erased costs, harm, or serialized its diagnostic stimulus.'
        }
        Assert-KmcConditionSnapshot $d.snapshot
        $path=Join-Path $root 'Manual_300_KMC_P01.zks'
        if($d.path-cne$path-or$d.nativeType-cne'Manual'-or$d.operation-cne'None'-or$d.nativeCallback-ne$true-or
            (Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length){
            throw 'P03 condition did not complete the actual owned native archive.'
        }
    }else{
        if(@($Rows|Where-Object kind -CEQ 'native-write-complete').Count-ne0-or
            (Get-KmcSha256 (Join-Path $root $Request.persistenceLoad.fileName))-cne$Request.persistenceLoad.sha256-or
            $initial[0].relationship-cne'Unmounted'-or$initial[0].persistence.presentation-ne0){
            throw 'P03 condition cold load changed its source or invented a pair.'
        }
    }
}

function Assert-KmcSuspendedSnapshot {
    param($Snapshot)
    $c=$Snapshot.Combat;$p=$c.Paired;$a=$p.Activation
    if($null-eq$c-or$c.Round-lt1-or$null-eq$a-or$a.Sequence-lt1-or
        -not$a.Suspended-or$a.Split-or$a.Ending-or$a.Rider.Ended-or$a.Mount.Ended-or
        $p.BoundaryIsCurrent-or$p.Boundary.ActorId-cne$Snapshot.Rider.Id-or$p.Boundary.Status-ne4-or
        ($null-ne$c.Current-and$c.Current.ActorId-cin @($Snapshot.Rider.Id,$Snapshot.Mount.Id))-or
        $Snapshot.Rider.Move-ne0-or$Snapshot.Mount.Standard-ne0-or$Snapshot.Mount.Move-ne0){
        throw 'P03 suspended archive lost its unused grant or actual delayed boundary.'
    }
}

function Assert-KmcSuspendedEvidence {
    param($Rows,[bool]$Write)
    $retained=@($Rows|Where-Object kind -CEQ 'suspended-retained')
    $resumed=@($Rows|Where-Object kind -CEQ 'suspended-grant-resumed')
    $rejected=@($Rows|Where-Object kind -CEQ 'cross-round-delay-rejected')
    $refresh=@($Rows|Where-Object kind -CEQ 'next-paired-activation')
    if($retained.Count-ne1-or$resumed.Count-ne1-or$rejected.Count-ne1-or$refresh.Count-ne2){
        throw 'P03 suspended grant lacks its real resume/rejection/refresh observations.'
    }
    $before=$retained[0].detail
    $samples=@($resumed[0],$rejected[0],$refresh[0],$refresh[1])
    for($i=0;$i-lt4;$i++){
        $d=$samples[$i].detail;$increment=[Math]::Max(0,$i-1)
        if($d.sequence-ne($before.sequence+$increment)-or$d.round-ne($before.round+$increment)){
            throw 'P03 Delay invented a grant or resumed across a native round.'
        }
        foreach($actor in @('rider','mount')){
            foreach($boundary in @('clear-after','round-state-after')){
                if($null-eq$d.delayNativeCounts-or
                    $d.delayNativeCounts.$actor.$boundary-ne($before.delayNativeCounts.$actor.$boundary+$increment)){
                    throw 'P03 Delay repeated or suppressed native preparation.'
                }
            }
        }
        if($i-lt2-and($d.roundEffects.rider.rounds-ne1-or$d.roundEffects.mount.rounds-ne1-or
            $d.roundEffects.rider.damage-ne2-or$d.roundEffects.mount.damage-ne2)){
            throw 'P03 Delay replayed or removed native healing.'
        }
    }
    if($Write){
        $input=@($Rows|Where-Object kind -CEQ 'delay-request-before')
        $after=@($Rows|Where-Object kind -CEQ 'delay-request-after')
        if($input.Count-ne1-or$after.Count-ne1-or$input[0].detail.targetWait-ge$input[0].detail.nextRoundWait-or
            $input[0].detail.delayTarget-cin @($input[0].rider.Id,$input[0].mount.Id)-or
            $after[0].detail.current-ceq$input[0].rider.Id-or
            $after[0].detail.activation-cne$before.activation){
            throw 'P03 lacks an actual pending same-round native Delay.'
        }
    }
}

function Assert-KmcSuspendedColdOutcome {
    param($SourceRows,$ColdRows)
    $source=@($SourceRows|Where-Object kind -CEQ 'suspended-retained')
    $cold=@($ColdRows|Where-Object kind -CEQ 'suspended-retained')
    if($source.Count-ne1-or$cold.Count-ne1-or$source[0].processId-eq$cold[0].processId-or
        $source[0].detail.activation-cne$cold[0].detail.activation-or
        $source[0].rider.Id-cne$cold[0].rider.Id-or$source[0].mount.Id-cne$cold[0].mount.Id){
        throw 'P03 suspended cold outcome lacks the same saved actors/grant in a fresh process.'
    }
    foreach($actor in @('rider','mount')){
        foreach($field in @('actor','damage','rounds','nextEventTicks','endTicks','active','suppressed')){
            if($source[0].detail.roundEffects.$actor.$field-cne$cold[0].detail.roundEffects.$actor.$field){
                throw 'P03 suspended cold outcome changed a native buff or its timer.'
            }
        }
    }
}

function Assert-KmcQueuedSaveEvidence {
    param($Rows,[string]$Root)
    $requests=@($Rows|Where-Object kind -CEQ 'queued-native-requests')
    $callbacks=@($Rows|Where-Object kind -CEQ 'queued-native-callback')
    $writes=@($Rows|Where-Object kind -CIn @('queued-native-write-complete','native-write-complete'))
    $names=@('KMC_P05_QUEUE_1','KMC_P05_QUEUE_2','KMC_P01')
    if($requests.Count-ne1-or$requests[0].detail.count-ne3-or$requests[0].detail.snapshots-ne0-or
        $requests[0].detail.callbacks-ne0-or($requests[0].detail.names-join'|')-cne($names-join'|')-or
        $callbacks.Count-ne3-or$writes.Count-ne3){
        throw 'P05 native queue lacks three actual deferred requests, callbacks and committed archives.'
    }
    for($i=0;$i-lt3;$i++){
        $callback=$callbacks[$i].detail;$w=$writes[$i].detail
        $path=Join-Path $Root ('Manual_'+(300+$i)+'_'+$names[$i]+'.zks')
        if($callback.ordinal-ne($i+1)-or$callback.snapshots-ne($i+1)-or$w.ordinal-ne($i+1)-or
            $w.path-cne$path-or$w.nativeType-cne'Manual'-or$w.nativeCallback-ne$true-or$w.operation-cne'None'-or
            $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$requests[0].rider.Id-or
            $w.snapshot.Mount.Id-cne$requests[0].mount.Id-or
            (Get-KmcSha256 $path)-cne$w.sha256-or(Get-Item -LiteralPath $path).Length-ne$w.length){
            throw 'P05 queue reordered, duplicated or lost an actual native snapshot/write.'
        }
    }
    if(@($writes|ForEach-Object{$_.detail.sha256}|Select-Object -Unique).Count-ne3-or
        @(Get-ChildItem -LiteralPath $Root -File -Filter '*.zks').Count-ne4){
        throw 'P05 native queue must retain three distinct archives and the protected input only.'
    }
}

# Cancellation requested while the owned archive worker is provably still able to
# commit. Nothing in the engine can abort that worker, so the only correct
# outcomes are to defer and then drain. This proves the deferral held every
# protection, that the interruption was reported truthfully only after the worker
# settled, and that the settlement matches the archive bytes either way.
function Assert-KmcWorkerDrainEvidence {
    param($Request,$Rows)
    $opened=@($Rows|Where-Object kind -CEQ 'drain-initial-write')
    $flight=@($Rows|Where-Object kind -CEQ 'worker-in-flight-observed')
    $deferred=@($Rows|Where-Object kind -CEQ 'drain-cancellation-deferred')
    $settled=@($Rows|Where-Object kind -CEQ 'drain-settled')
    $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
    if($opened.Count-ne1-or$flight.Count-ne1-or$deferred.Count-ne1-or$settled.Count-ne1-or$written.Count-ne1){
        throw 'P07 drain lacks its opening write, in-flight observation, deferral, settlement and subsequent write.'
    }
    $kinds=@($Rows|ForEach-Object{$_.kind})
    $previous=-1
    foreach($kind in @('drain-initial-write','worker-in-flight-observed','drain-cancellation-deferred','drain-settled','native-write-complete')){
        $at=[Array]::IndexOf($kinds,$kind)
        if($at-le$previous){throw "P07 drain evidence is out of order at $kind."}
        $previous=$at
    }
    $f=$flight[0].detail
    # The boundary must be THIS operation's own worker, observed unfinished.
    if([string]::IsNullOrEmpty([string]$f.preparedLeaf)-or$f.workerTaskId-lt0-or
        $f.workerRunning-ne$true-or$f.workerHeld-ne$true-or$f.workerHolds-ne1-or
        $f.heldLeaf-cne$f.preparedLeaf){
        throw 'P07 drain did not identify one exact in-flight owned save worker.'
    }
    if($f.workerEntries-lt1-or$f.draining-eq$true-or$f.deferredCancellations-ne0-or$f.drains-ne0){
        throw 'P07 drain observed its boundary after the save had already been interrupted.'
    }
    if($f.saveSuspended-ne$true-or$f.serializationSuspended-ne$true-or$f.activeScope-ne$true){
        throw 'P07 in-flight save did not hold its serialization leases.'
    }
    $d=$deferred[0].detail
    if($d.workerTaskId-ne$f.workerTaskId){throw 'P07 deferral describes a different save worker.'}
    if($d.draining-ne$true-or$d.activeScope-ne$true-or$d.deferredCancellations-ne1-or$d.drains-ne0){
        throw 'P07 cancellation released the scope instead of deferring it.'
    }
    if($d.saveSuspended-ne$true-or$d.serializationSuspended-ne$true-or$d.saveCallback-ne$false){
        throw 'P07 deferral dropped a lease or reported a cancellation while the worker could still commit.'
    }
    foreach($probe in @('overlapRefused','loadRefused','repeatedStopSafe','disableRefused')){
        if($d.$probe-ne$true){throw "P07 deferral failed its $probe boundary."}
    }
    # The held world while the worker can still commit: the main-menu reset --
    # the only gameplay caller of StopAll -- is deferred with the world intact,
    # the world is paused the moment the abandonment defers, no unit's command
    # is queued (owned pair or an unrelated party member), the clock does not
    # advance, and the user's prior pause state is restored at settlement.
    # The engine applies the pause request asynchronously (set_IsPaused starts the
    # Pause mode; get_IsPaused reads IsModeActive), so the held state is judged at
    # the simulation probe below, not on the frame the abandonment deferred.
    if($d.resetDeferred-ne$true-or$d.resetPending-ne$false){
        throw 'P07 drain did not defer the main-menu reset with its replay discarded.'
    }
    $sim=@($Rows|Where-Object kind -CEQ 'drain-simulation-probe')
    if($sim.Count-ne1){throw 'P07 drain lacks its exact simulation probe.'}
    $p=$sim[0].detail
    if($p.simWorkerStillHeld-ne$true-or$p.heldPaused-ne$true-or$p.simCommandQueued-ne$false-or$p.unrelatedCommandQueued-ne$false-or
        [string]::IsNullOrEmpty([string]$p.unrelatedActorId)-or$p.simTicksAdvanced-ne0-or$p.simCommandStarted-ne$false){
        throw 'P07 held world still admitted a unit command or advanced the clock under the live serializer.'
    }
    if($d.nativeWorldDisposals-ne$f.nativeWorldDisposals){
        throw 'P07 refused load still disposed a world.'
    }
    if($d.currentSha256-ne$null-and$d.currentSha256-cne$d.lastGoodSha256){
        throw 'P07 last-good archive changed while the worker was held.'
    }
    $s=$settled[0].detail
    if($s.pauseRestored-ne$true){throw 'P07 settlement did not restore the user prior pause state.'}
    if($s.drains-ne1-or$s.deferredCancellations-ne1-or$s.draining-ne$false-or$s.activeScope-ne$false){
        throw 'P07 settlement did not release exactly once.'
    }
    if($s.saveSuspended-ne$false-or$s.serializationSuspended-ne$false){
        throw 'P07 settlement left a serialization lease held.'
    }
    # Measured: the commit replaces the target archive in place and rebinds the
    # path. So unchanged old bytes are only correct when the commit did NOT
    # happen; claiming them after a real commit would be false.
    if($s.drainCommitted-eq$true){
        if($s.failedSaves-ne0){throw 'P07 reported a committed interrupted save as failed.'}
        if([string]::IsNullOrEmpty([string]$s.interruptedPath)){
            throw 'P07 reported a committed interrupted save with no archive path.'
        }
        if($s.replacedInPlace-eq$true){
            if($s.currentSha256-ceq$s.lastGoodSha256){
                throw 'P07 claimed a committed in-place replacement whose bytes never changed.'
            }
        }elseif($s.currentSha256-cne$s.lastGoodSha256){
            throw 'P07 committed elsewhere yet the last-good archive changed.'
        }
    }else{
        if($s.failedSaves-ne1){throw 'P07 did not report the uncommitted interrupted save as failed.'}
        if($s.currentSha256-cne$s.lastGoodSha256){
            throw 'P07 reported a failed interruption but the last-good archive changed.'
        }
    }
    if($written[0].detail.ordinal-ne2-or[string]::IsNullOrEmpty([string]$written[0].detail.sha256)){
        throw 'P07 drain lacks its real subsequent write.'
    }
    # The -output variant stops at settlement so the interrupted archive itself
    # survives the run. Its recorded write must BE that archive, not a later one,
    # and it must still be a complete mounted archive on disk at that hash.
    if($Request.persistenceCase-ceq'serialization-cancel-output'){
        $out=@($Rows|Where-Object kind -CEQ 'drain-interrupted-output')
        if($out.Count-ne1){throw 'P07 interrupted-output case lacks its exact preserved-output observation.'}
        $o=$out[0].detail
        if($o.committed-ne$true-or$o.drains-ne1-or$o.deferrals-ne1){
            throw 'P07 interrupted-output case did not preserve a committed, once-drained operation.'
        }
        if($o.path-cne$s.interruptedPath-or$o.sha256-cne$s.currentSha256-or$o.length-le0){
            throw 'P07 preserved output is not the interrupted operation own committed archive.'
        }
        if($written[0].detail.path-cne$o.path-or$written[0].detail.sha256-cne$o.sha256){
            throw 'P07 interrupted-output case recorded a later archive instead of the interrupted one.'
        }
        if(-not(Test-Path -LiteralPath $o.path -PathType Leaf)){
            throw 'P07 interrupted output did not survive its own run.'
        }
        if((Get-KmcSha256 $o.path)-cne$o.sha256){
            throw 'P07 interrupted output bytes changed after the run recorded them.'
        }
    }elseif(@($Rows|Where-Object kind -CEQ 'drain-interrupted-output').Count-ne0){
        throw 'P07 plain drain case must not claim a preserved interrupted output.'
    }
}

# Disable/re-enable lifecycle. The re-enable must rebuild exactly one pair with
# no duplicated control, the refusal must have been taken against a worker that
# was measurably still running, and the cleanup-state save must record no pair.
function Assert-KmcDisableLifecycleEvidence {
    param($Request,$Rows)
    # The refusal is probed on the mounted save, before the second cleanup cycle
    # and the unmounted save, because only a mounted save opens the owned scope
    # whose serialization lease the refusal reads.
    $order=@('disable-initial','disable-cleaned','disable-re-enabled','disable-refused-during-save','disable-cleared','disable-saved-unmounted')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 disable lifecycle lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
    if($written.Count-ne1){throw 'P07 disable lifecycle lacks its exact cleanup-state write.'}
    # Ordering, not exact stage numbers: the stages carry their own identity and
    # renumbering the walk must not silently pass or silently fail this contract.
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne'disable-reenable'){throw "P07 disable row $kind is not the declared case."}
        if($row.detail.stage-le$previous){throw "P07 disable row $kind is out of its measured order."}
        $previous=$row.detail.stage
    }
    $initial=$stages['disable-initial']; $cleaned=$stages['disable-cleaned']
    $reenabled=$stages['disable-re-enabled']; $cleared=$stages['disable-cleared']
    $refused=$stages['disable-refused-during-save']
    $saved=$stages['disable-saved-unmounted']
    # The refusal is observed while MOUNTED: that is the save whose live pair
    # graphs a disable would mutate, and the only one holding an owned scope.
    if($initial.detail.relationship-cne'Mounted'-or$cleaned.detail.relationship-cne'Unmounted'-or
        $reenabled.detail.relationship-cne'Mounted'-or$refused.detail.relationship-cne'Mounted'-or
        $cleared.detail.relationship-cne'Unmounted'-or
        $saved.detail.relationship-cne'Unmounted'){
        throw 'P07 disable lifecycle did not traverse mounted, cleaned, re-enabled and cleaned again.'
    }
    # The second disable/re-enable cycle must land back on the cleaned state
    # exactly, with the services present and nothing resurrected.
    if($cleared.detail.secondDisable-ne$true-or$cleared.detail.secondReEnable-ne$true-or
        $cleared.detail.stateAfterSecondDisable-cne'Unmounted'-or
        $cleared.detail.stateAfterSecondReEnable-cne'Unmounted'-or
        $cleared.detail.factsAfterSecondDisable-ne$cleaned.detail.factsUnmounted-or
        $cleared.detail.factsAfterSecondReEnable-ne$reenabled.detail.factsEnabledUnmounted){
        throw 'P07 second disable/re-enable cycle did not return to the cleaned state.'
    }
    # An enabled mod legitimately offers its unmounted-state control, so the
    # anti-duplication property is that this count is stable across cycles and
    # strictly above the disabled count, never that it is zero.
    if($reenabled.detail.factsEnabledUnmounted-le$cleaned.detail.factsUnmounted){
        throw 'P07 re-enable did not restore the unmounted-state owned control.'
    }
    if([string]::IsNullOrEmpty([string]$initial.detail.riderId)-or
        [string]::IsNullOrEmpty([string]$initial.detail.mountId)-or
        $reenabled.detail.riderId-cne$initial.detail.riderId-or
        $reenabled.detail.mountId-cne$initial.detail.mountId){
        throw 'P07 re-enable did not reuse the same exact native actors.'
    }
    if($initial.detail.factsMounted-le0-or$cleaned.detail.factsUnmounted-ge$initial.detail.factsMounted){
        throw 'P07 disable did not actually release owned controls.'
    }
    foreach($row in @($initial,$cleaned,$reenabled,$cleared,$refused,$saved)){
        if($row.detail.duplicateFactCount-ne0){throw "P07 disable lifecycle duplicated an owned control at $($row.kind)."}
    }
    # Bounded, not equal: re-enabling must grant nothing extra and nothing twice,
    # but a save-restored baseline also reinstates persisted state that a fresh
    # pair has no reason to recreate. The exact observed counts stay recorded.
    if($reenabled.detail.factsReEnabled-lt$reenabled.detail.factsEnabledUnmounted-or
        $reenabled.detail.factsReEnabled-gt$initial.detail.factsMounted-or
        $reenabled.detail.slotsReEnabled-gt$initial.detail.slotsMounted-or
        $reenabled.detail.nativeCastRequests-ne$initial.detail.castsBefore-or
        $reenabled.detail.secondMountRejected-ne$true-or
        -not[string]::IsNullOrEmpty([string]$reenabled.detail.invariants)){
        throw 'P07 re-enable added controls, cast a fresh Mount, allowed a second pair, or broke its invariants.'
    }
    # The refusal only means something if the write really was still in flight.
    if($refused.detail.refusedDuringSave-ne$true-or$refused.detail.suspendedAtProbe-ne$true-or
        $refused.detail.workerRunningAtProbe-ne$true-or$refused.detail.enabledAfterProbe-ne$true){
        throw 'P07 disable refusal was not taken against a running owned save worker.'
    }
    if($refused.detail.workerEntriesAtProbe-ne($refused.detail.workerEntriesBefore+1)-or
        [string]::IsNullOrEmpty([string]$refused.detail.heldLeaf)-or
        $refused.detail.heldLeaf-cne$refused.detail.probeLeaf-or
        $refused.detail.probeWorkerId-lt0){
        throw 'P07 disable refusal did not identify this exact save operation and its own worker.'
    }
    if($refused.detail.failedSaves-ne0-or$saved.detail.failedSaves-ne0){
        throw 'P07 refused disable damaged the save it declined to interrupt.'
    }
    # The registered unload at the same boundary: refused after its bounded
    # teardown wait, having unpatched and released nothing. A wait that expired
    # is recorded as NOT settled, never as permission.
    if($refused.detail.unloadRefused-ne$true-or$refused.detail.patchesIntactAfterUnload-ne$true-or
        $refused.detail.teardownSettledAtProbe-ne$false-or
        $refused.detail.teardownDrainsAtProbe-ne($refused.detail.teardownDrainsBefore+1)){
        throw 'P07 registered unload was not refused with the root, patches and leases intact.'
    }
    # The serializer may omit default or null members, so absent and explicitly
    # empty both count as "no pair"; anything present and populated does not.
    $snapshot=$written[0].detail.snapshot
    $mounted=$null-ne$snapshot.PSObject.Properties['Mounted']-and$snapshot.Mounted-ne$false
    $rider=$null-ne$snapshot.PSObject.Properties['Rider']-and$null-ne$snapshot.Rider
    $mount=$null-ne$snapshot.PSObject.Properties['Mount']-and$null-ne$snapshot.Mount
    if($written[0].detail.ordinal-ne2-or$written[0].detail.nativeCallback-ne$true-or
        [string]::IsNullOrEmpty([string]$written[0].detail.sha256)-or$written[0].detail.length-le0-or
        $mounted-or$rider-or$mount){
        throw 'P07 cleanup-state save did not record a real archive with no mounted pair.'
    }
}

# One native archive's own members, read from its bytes: the engine's header and
# whatever KMC wrote beside it. Nothing is inferred from evidence rows here.
function Read-KmcCampaignArchiveMembers {
    param([Parameter(Mandatory=$true)][string]$Path)
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$archive=$null
    try{
        $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        $headers=@($archive.Entries|Where-Object FullName -CEQ 'header.json')
        $members=@($archive.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($headers.Count-ne1-or$headers[0].Length-le0-or$headers[0].Length-gt1MB-or$members.Count-gt1){throw 'Campaign B archive members are missing, ambiguous or oversized.'}
        $reader=[IO.StreamReader]::new($headers[0].Open(),[Text.UTF8Encoding]::new($false,$true))
        try{$headerJson=$reader.ReadToEnd()}finally{$reader.Dispose()}
        Assert-KmcJsonObjectMembersUnique -Json $headerJson -Description 'campaign B archive header'
        $kmc=$null
        if($members.Count-eq1){
            if($members[0].Length-le0-or$members[0].Length-gt128KB){throw 'Campaign B archive KMC member is empty or oversized.'}
            $reader=[IO.StreamReader]::new($members[0].Open(),[Text.UTF8Encoding]::new($false,$true))
            try{$kmcJson=$reader.ReadToEnd()}finally{$reader.Dispose()}
            Assert-KmcJsonObjectMembersUnique -Json $kmcJson -Description 'campaign B archive KMC member'
            $kmc=$kmcJson|ConvertFrom-Json
        }
        return [pscustomobject]@{header=($headerJson|ConvertFrom-Json);kmc=$kmc}
    }finally{if($archive){$archive.Dispose()};if($stream){$stream.Dispose()}}
}

# A campaign-B archive as recorded and as it lies on disk: a real native save of
# the engine-minted campaign alone. Its header is B's; anything KMC wrote beside
# it records no pair, no bindings and B's own identity, never A's.
function Assert-KmcCampaignBArchive {
    param($Recorded,[string]$Root,[string]$Leaf,[string]$Type,[string]$GameId,[string]$GameName,[string]$Area,[string]$FixtureGameId)
    if($null-eq$Recorded){throw "P07 campaign B lacks its $Leaf archive observation."}
    if($Recorded.leaf-cne$Leaf-or$Recorded.nativeType-cne$Type-or$Recorded.operation-cne'None'-or
        $Recorded.gameId-cne$GameId-or$Recorded.gameName-cne$GameName-or$Recorded.area-cne$Area-or
        $Recorded.sha256-cnotmatch'^[0-9a-f]{64}$'-or$Recorded.length-le0-or
        $Recorded.path-cne(Join-Path $Root $Leaf)){
        throw "P07 campaign B $Leaf is not the exact declared archive of the minted campaign."
    }
    if($Recorded.kmcMember-cnotin @('Missing','Current')){throw "P07 campaign B $Leaf carries unreadable KMC metadata."}
    # The serializer may omit default or null members, so absent and explicitly
    # empty both count as "no pair"; anything present and populated does not.
    if($Recorded.kmcMember-ceq'Current'){
        $s=$Recorded.snapshot
        if($null-eq$s-or(Get-KmcOptionalMember $s 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $s 'Rider')-or
            $null-ne(Get-KmcOptionalMember $s 'Mount')-or@(@(Get-KmcOptionalMember $s 'Slots')|Where-Object{$null-ne$_}).Count-ne0-or
            (Get-KmcOptionalMember $s 'CampaignId')-cne$GameId-or(Get-KmcOptionalMember $s 'AreaId')-cne$Area){
            throw "P07 campaign B $Leaf recorded a pair, bindings or a foreign campaign."
        }
    }elseif($null-ne$Recorded.snapshot){throw "P07 campaign B $Leaf claims no KMC member yet records a snapshot."}
    $path=Join-Path $Root $Leaf
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "P07 campaign B $Leaf did not survive its own run."}
    if((Get-KmcSha256 $path)-cne$Recorded.sha256-or(Get-Item -LiteralPath $path).Length-ne$Recorded.length){
        throw "P07 campaign B $Leaf bytes changed after the run recorded them."
    }
    $members=Read-KmcCampaignArchiveMembers -Path $path
    $h=$members.header
    if($h.GameId-cne$GameId-or$h.GameName-cne$GameName-or$h.Area-cne$Area-or$h.Type-cne$Type-or
        $h.GameId-ceq$FixtureGameId){
        throw "P07 campaign B $Leaf native header is not the minted campaign's own."
    }
    if(($null-ne$members.kmc)-ne($Recorded.kmcMember-ceq'Current')){throw "P07 campaign B $Leaf KMC member presence differs from the recorded read."}
    if($null-ne$members.kmc){
        $k=$members.kmc
        if((Get-KmcOptionalMember $k 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $k 'Rider')-or$null-ne(Get-KmcOptionalMember $k 'Mount')-or
            @(@(Get-KmcOptionalMember $k 'Slots')|Where-Object{$null-ne$_}).Count-ne0-or
            (Get-KmcOptionalMember $k 'CampaignId')-cne$GameId-or(Get-KmcOptionalMember $k 'CampaignId')-ceq$FixtureGameId-or
            (Get-KmcOptionalMember $k 'AreaId')-cne$Area){
            throw "P07 campaign B $Leaf on-disk KMC member leaks a pair, bindings or A's campaign."
        }
    }
}

function Get-KmcOptionalMember { param($Object,[string]$Name)
    if($null-eq$Object){return $null}
    $property=$Object.PSObject.Properties[$Name]
    if($null-eq$property){return $null}
    return $property.Value
}

# Campaign B: A mounted with real expenditure and two archives; the engine's own
# new game with an identity it minted and KMC froze on B's first write; clean B
# archives; then A restored exactly, every archive byte-identical throughout.
function Assert-KmcCampaignBEvidence {
    param($Request,$Rows)
    $order=@('native-write-complete','campaign-b-expenditure','campaign-b-departed','campaign-b-started','campaign-b-frozen','campaign-b-returned')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 campaign B lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne'campaign-b'){throw "P07 campaign B row $kind is not the declared case."}
        if($row.stage-lt$previous){throw "P07 campaign B row $kind is out of its measured order."}
        if($row.relationship-cne'Mounted'-and($null-ne$row.rider-or$null-ne$row.mount)){
            throw "P07 campaign B row $kind outside A still describes A actors."
        }
        $previous=$row.stage
    }
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P07 campaign B lacks its initial A state.'}
    $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
    $fixtureGameId=[string]$Request.fixture.working.gameId
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $w=$stages['native-write-complete'].detail
    $e=$stages['campaign-b-expenditure'].detail
    $d=$stages['campaign-b-departed'].detail
    $s=$stages['campaign-b-started'].detail
    $f=$stages['campaign-b-frozen'].detail
    $r=$stages['campaign-b-returned'].detail
    # A's two archives: the opening write and the post-expenditure write.
    if($w.ordinal-ne1-or$w.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$riderId-or$w.snapshot.Mount.Id-cne$mountId-or
        $w.snapshot.CampaignId-cne$fixtureGameId){
        throw 'P07 campaign B first A archive is not the exact mounted opening write.'
    }
    $second=$e.secondArchive
    if($stages['campaign-b-expenditure'].relationship-cne'Mounted'-or$e.moved-le1-or$e.bindings-le0-or$e.snapshots-ne2-or
        $null-eq$second-or$second.path-cne(Join-Path $root 'Manual_301_KMC_P01B.zks')-or$second.leaf-cne'Manual_301_KMC_P01B.zks'-or
        $second.sha256-cnotmatch'^[0-9a-f]{64}$'-or$second.sha256-ceq$w.sha256-or$second.nativeType-cne'Manual'-or
        $second.gameId-cne$fixtureGameId-or$second.snapshot.Mounted-ne$true-or
        $second.snapshot.Rider.Id-cne$riderId-or$second.snapshot.Mount.Id-cne$mountId-or
        $second.snapshot.CampaignId-cne$fixtureGameId-or$e.aFirstHash-cne$w.sha256){
        throw 'P07 campaign B expenditure did not move the pair and record it in a second distinct A archive.'
    }
    if(-not(Test-Path -LiteralPath $second.path -PathType Leaf)-or(Get-KmcSha256 $second.path)-cne$second.sha256){
        throw 'P07 campaign B second A archive did not survive the run byte-identical.'
    }
    # Leaving A: no pair, no lease, nothing minted yet.
    if($stages['campaign-b-departed'].relationship-ceq'Mounted'-or$null-ne$d.loadedArea-or$d.saveSuspended-ne$false-or
        $d.activeScope-ne$false-or$d.draining-ne$false-or$d.resetDeferrals-ne0-or
        $d.aFirstHash-cne$w.sha256-or$d.aSecondHash-cne$second.sha256-or
        $d.bootstrapDeclared-ne$true-or$d.bootstrapWindowOpen-ne$false-or$d.bootstrapFreezes-ne0-or$null-ne$d.bootstrapGameId){
        throw 'P07 campaign B departure left A state, a lease, or a premature bootstrap identity behind.'
    }
    # Starting B: the engine's own authored preset, window opened immediately
    # before the native new game, nothing frozen before it.
    if($s.presetSource-cnotin @('dlc-endless','main-campaign')-or$s.presetArea-cnotmatch'^[0-9a-f]{32}$'-or
        $s.enterPointArea-cne$s.presetArea-or$s.windowOpened-ne$true-or$s.frozenBefore-ne$false-or
        $s.autosaveEnabled-ne$true-or$s.bootstrapFreezes-ne0-or$stages['campaign-b-started'].relationship-ceq'Mounted'){
        throw 'P07 campaign B did not start from the engine preset with the bootstrap window opened first.'
    }
    if($s.presetSource-ceq'dlc-endless'-and$s.dlcEnabled-ne$true){throw 'P07 campaign B used DLC content without the installed license.'}
    # Frozen: the engine minted a fresh identity; the authority froze exactly
    # that, once, on B's first write; KMC restored nothing into B.
    $parsed=[Guid]::Empty
    if(-not[Guid]::TryParse([string]$f.gameId,[ref]$parsed)-or$parsed-eq[Guid]::Empty-or$f.gameId-ceq$fixtureGameId-or
        -not($f.gameName-is[string])-or$f.gameName.Length-eq0-or$f.gameName-cmatch'[\x00-\x1f\x7f]'-or
        $f.freezeCount-ne1-or$f.windowOpen-ne$false-or$f.bootstrapGameId-cne$f.gameId-or$f.bootstrapGameName-cne$f.gameName-or
        $f.area-cne$s.presetArea-or$f.loadedArea-cne$f.area-or$f.bindings-ne0-or
        $stages['campaign-b-frozen'].relationship-ceq'Mounted'-or
        $f.semantics-ne$d.semantics-or$f.presentation-ne$d.presentation-or
        $f.aFirstSha256-cne$w.sha256-or$f.aSecondSha256-cne$second.sha256){
        throw 'P07 campaign B identity was not minted by the engine and frozen exactly once, or KMC carried A state into B.'
    }
    if($f.gameName-ceq$Request.fixture.working.gameName-and$f.gameId-ceq$fixtureGameId){throw 'P07 campaign B reused the fixture campaign.'}
    Assert-KmcCampaignBArchive -Recorded $f.autosave -Root $root -Leaf 'Auto_1.zks' -Type 'Auto' -GameId $f.gameId -GameName $f.gameName -Area $f.area -FixtureGameId $fixtureGameId
    if($f.manualSaved-eq$true){
        if($f.manualAllowed-ne$true){throw 'P07 campaign B wrote a manual save it reported as disallowed.'}
        Assert-KmcCampaignBArchive -Recorded $f.manual -Root $root -Leaf 'Manual_302_KMC_B.zks' -Type 'Manual' -GameId $f.gameId -GameName $f.gameName -Area $f.area -FixtureGameId $fixtureGameId
        if($f.manual.internalName-cne'KMC_B'-or$f.manual.sha256-ceq$f.autosave.sha256){throw 'P07 campaign B manual save is not its own distinct archive.'}
    }elseif($null-ne$f.manual-or$f.manualAllowed-ne$false){
        throw 'P07 campaign B manual save state is inconsistent.'
    }elseif(Test-Path -LiteralPath (Join-Path $root 'Manual_302_KMC_B.zks')){
        throw 'P07 campaign B has an unrecorded manual archive on disk.'
    }
    # Returned: A restored once from its own expended archive, exact pair,
    # bindings, position and debt; every archive byte-identical; B's world
    # disposed exactly once on the way back.
    if($stages['campaign-b-returned'].relationship-cne'Mounted'-or$r.gameId-cne$fixtureGameId-or$r.worldIsA-ne$false-or
        $r.semantics-ne($d.semantics+2)-or$r.presentation-ne($d.presentation+1)-or
        $r.mountDelta-ge0.5-or$r.riderDelta-ge1.5-or$r.bindingsSaved-le0-or$r.bindingsRestored-ne$r.bindingsSaved-or
        $r.aFirstSha256-cne$w.sha256-or$r.aSecondSha256-cne$second.sha256-or$r.bAutoSha256-cne$f.autosave.sha256-or
        ($f.manualSaved-eq$true-and$r.bManualSha256-cne$f.manual.sha256)-or
        $r.disposals-ne($d.disposals+1)-or$r.bootstrapGameId-cne$f.gameId-or$r.failedSaves-ne0-or$r.rejections-ne0-or
        $r.saveSuspended-ne$false-or$r.activeScope-ne$false){
        throw 'P07 campaign B return did not restore A exactly once with every archive intact.'
    }
    if($r.riderId-cne$riderId-or$r.mountId-cne$mountId-or$stages['campaign-b-returned'].rider.Id-cne$riderId-or
        $stages['campaign-b-returned'].mount.Id-cne$mountId){
        throw 'P07 campaign B return changed the owned pair actors.'
    }
}

# A cleanup archive as recorded and as it lies on disk: the fixture campaign's
# own native save, in its own area, whose KMC member records no pair at all.
function Assert-KmcCleanupArchive {
    param($Recorded,[string]$Root,[string]$GameId,[string]$Area)
    if($null-eq$Recorded){throw 'P07 removal lacks its cleanup archive observation.'}
    if($Recorded.leaf-cne'Manual_301_KMC_CLEANUP.zks'-or$Recorded.internalName-cne'KMC_CLEANUP'-or$Recorded.nativeType-cne'Manual'-or
        $Recorded.operation-cne'None'-or$Recorded.gameId-cne$GameId-or$Recorded.area-cne$Area-or$Recorded.kmcMember-cne'Current'-or
        $Recorded.sha256-cnotmatch'^[0-9a-f]{64}$'-or$Recorded.length-le0-or$Recorded.path-cne(Join-Path $Root $Recorded.leaf)){
        throw 'P07 cleanup archive is not the exact declared clean save of the fixture campaign.'
    }
    $s=$Recorded.snapshot
    if($null-eq$s-or(Get-KmcOptionalMember $s 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $s 'Rider')-or
        $null-ne(Get-KmcOptionalMember $s 'Mount')-or(Get-KmcOptionalMember $s 'CampaignId')-cne$GameId){
        throw 'P07 cleanup archive recorded a pair or a foreign campaign.'
    }
    # A removal-ready archive carries nothing only KMC can restore: no combat
    # participation supplement and no owned control binding.
    $slots=Get-KmcOptionalMember $s 'Slots'
    if($null-ne(Get-KmcOptionalMember $s 'Combat')-or($null-ne$slots-and@($slots).Count-ne0)){
        throw 'P07 cleanup archive recorded combat participation or a KMC control binding.'
    }
    $path=Join-Path $Root $Recorded.leaf
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw 'P07 cleanup archive did not survive its own run.'}
    if((Get-KmcSha256 $path)-cne$Recorded.sha256-or(Get-Item -LiteralPath $path).Length-ne$Recorded.length){
        throw 'P07 cleanup archive bytes changed after the run recorded them.'
    }
    $members=Read-KmcCampaignArchiveMembers -Path $path
    if($members.header.Name-cne'KMC_CLEANUP'-or$members.header.Type-cne'Manual'-or$members.header.GameId-cne$GameId-or
        $members.header.Area-cne$Area-or$null-eq$members.kmc){
        throw 'P07 cleanup archive native header or KMC member on disk is not the recorded clean save.'
    }
    $k=$members.kmc
    $diskSlots=Get-KmcOptionalMember $k 'Slots'
    if((Get-KmcOptionalMember $k 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $k 'Rider')-or$null-ne(Get-KmcOptionalMember $k 'Mount')-or
        (Get-KmcOptionalMember $k 'CampaignId')-cne$GameId-or$null-ne(Get-KmcOptionalMember $k 'Combat')-or($null-ne$diskSlots-and@($diskSlots).Count-ne0)){
        throw 'P07 cleanup archive on-disk KMC member still records a pair, combat participation or a control binding.'
    }
}

# Prepare-to-Disable/removal: refused, naming the exact permanent KMC Horse
# reference, while one exists; then dismounted through cleanup, a NEW native
# cleanup save verified from its bytes, the registered disable, and re-enable.
function Assert-KmcRemovalEvidence {
    param($Request,$Rows)
    $order=@('native-write-complete','removal-refused','removal-refused-combat','removal-requested','removal-prepared','removal-disabled-reenabled')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 removal lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne'prepare-removal'){throw "P07 removal row $kind is not the declared case."}
        if($row.stage-lt$previous){throw "P07 removal row $kind is out of its measured order."}
        $previous=$row.stage
    }
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P07 removal lacks its initial state.'}
    $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
    foreach($kind in $order){
        $row=$stages[$kind]
        if(($null-ne$row.rider-and$row.rider.Id-cne$riderId)-or($null-ne$row.mount-and$row.mount.Id-cne$mountId)){throw "P07 removal row $kind describes foreign actors."}
    }
    $gameId=[string]$Request.fixture.working.gameId;$area=[string]$Request.fixture.working.area
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $w=$stages['native-write-complete'].detail
    $x=$stages['removal-refused'].detail
    $q=$stages['removal-requested'].detail
    $p=$stages['removal-prepared'].detail
    $d=$stages['removal-disabled-reenabled'].detail
    if($w.ordinal-ne1-or$w.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$riderId-or$w.snapshot.Mount.Id-cne$mountId){
        throw 'P07 removal first archive is not the exact mounted opening write.'
    }
    # Refused against a real KMC Horse unit in the loaded world, with nothing
    # dismounted and nothing saved.
    if($stages['removal-refused'].relationship-cne'Mounted'-or$x.beganRefused-ne$true-or$x.state-cne'Refused'-or$x.refusals-ne1-or
        $x.cleanupSaves-ne0-or$x.snapshots-ne1-or$x.saveSuspended-ne$false-or$x.activeScope-ne$false-or
        $x.spawnedBlueprint-cne'4016c7db400ab721ff125aef9e65e202'-or[string]::IsNullOrEmpty([string]$x.spawnedId)-or
        @($x.references).Count-lt1-or@($x.references|Where-Object { ([string]$_).Contains([string]$x.spawnedId) }).Count-ne1-or
        @($x.reasons).Count-lt1-or$x.riderId-cne$riderId-or$x.mountId-cne$mountId){
        throw 'P07 unsafe removal was not refused against the exact permanent KMC Horse reference without side effects.'
    }
    # Refused again in real native combat by the contract's own rule, whether or
    # not the engine would have admitted a save, with nothing dismounted or saved.
    $c=$stages['removal-refused-combat'].detail
    if($stages['removal-refused-combat'].relationship-cne'Mounted'-or$c.combatRefused-ne$true-or$c.state-cne'Refused'-or$c.refusals-ne2-or
        $c.cleanupSaves-ne0-or$c.snapshots-ne1-or$c.saveSuspended-ne$false-or$c.activeScope-ne$false-or$c.partyCombat-ne$true-or
        [string]::IsNullOrEmpty([string]$c.targetId)-or@($c.reasons|Where-Object { ([string]$_) -cmatch 'in combat' }).Count-lt1-or
        $c.riderId-cne$riderId-or$c.mountId-cne$mountId){
        throw 'P07 removal in real combat was not refused by the contract own rule without side effects.'
    }
    # Admitted once the reference and the combat are gone: cleanup first, then the save.
    if($stages['removal-requested'].relationship-ceq'Mounted'-or$q.began-ne$true-or$q.state-cne'Saving'-or$q.refusals-ne2-or$q.cleanupSaves-ne0-or$q.completedSaves-ne1-or$q.commits-ne0){
        throw 'P07 prepare-to-disable did not dismount through cleanup before requesting its save.'
    }
    if($stages['removal-prepared'].relationship-ceq'Mounted'-or$p.state-cne'Ready'-or$p.cleanupSaves-ne1-or$p.refusals-ne2-or$p.unconfirmed-ne0-or
        $p.snapshots-ne2-or$p.failedSaves-ne0-or$p.saveSuspended-ne$false-or$p.activeScope-ne$false-or
        $p.cleanupLeaf-cne'Manual_301_KMC_CLEANUP.zks'-or$p.cleanupSha256-cne$p.cleanup.sha256-or$p.firstSha256-cne$w.sha256){
        throw 'P07 prepared state did not come from exactly one new cleanup save with the first archive untouched.'
    }
    # Readiness is bound to the persistence service's completed-write record for
    # this request (both archives of this walk are first-ever saves, so KMC's
    # replacement-commit record stays at zero) and to a scan of every archive
    # member for KMC-registered blueprint identities.
    if($p.binding-cne'bound'-or$p.cleanupCampaign-cne$gameId-or$p.completedSaves-ne2-or$p.commits-ne0-or$p.scannedMembers-lt1-or$p.scannedBytes-le0-or@($p.referenceHits).Count-ne0){
        throw 'P07 prepared state was not bound to its own completed write with every archive member scanned clean.'
    }
    Assert-KmcCleanupArchive -Recorded $p.cleanup -Root $root -GameId $gameId -Area $area
    if((Get-KmcSha256 (Join-Path $root 'Manual_300_KMC_P01.zks'))-cne$w.sha256){throw 'P07 cleanup save changed the first archive on disk.'}
    # The user's next step: the exact registered disable succeeds, and
    # re-enable plus remount reuse the same actors without a fresh Mount cast.
    if($stages['removal-disabled-reenabled'].relationship-cne'Mounted'-or$d.disabled-ne$true-or$d.reEnabled-ne$true-or$d.remounted-ne$true-or
        $d.factsMounted-le0-or$d.factsDisabled-ge$d.factsMounted-or$d.factsReEnabled-le$d.factsDisabled-or$d.factsReEnabled-gt$d.factsMounted-or
        $d.nativeCastRequests-ne0-or$d.riderId-cne$riderId-or$d.mountId-cne$mountId-or
        $stages['removal-disabled-reenabled'].rider.Id-cne$riderId-or$stages['removal-disabled-reenabled'].mount.Id-cne$mountId){
        throw 'P07 registered disable and re-enable from the prepared state did not return the same pair once.'
    }
}

# The registered disable during a real native load of a mounted save: refused
# at every frame the load owns, including the semantic-restored /
# presentation-pending boundary; the equivalent-state cycle at rest; and a
# same-frame pre-routine disable judged by its outcome.
function Assert-KmcDisableLoadEvidence {
    param($Request,$Rows)
    $order=@('native-write-complete','disable-load-requested','disable-load-probed','disable-load-rest-cycle','disable-load-preroutine-probe','disable-load-second-load')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 disable-during-load lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne'disable-during-load'){throw "P07 disable-during-load row $kind is not the declared case."}
        if($row.stage-lt$previous){throw "P07 disable-during-load row $kind is out of its measured order."}
        $previous=$row.stage
    }
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P07 disable-during-load lacks its initial state.'}
    $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
    foreach($kind in $order){
        $row=$stages[$kind]
        if(($null-ne$row.rider-and$row.rider.Id-cne$riderId)-or($null-ne$row.mount-and$row.mount.Id-cne$mountId)){throw "P07 disable-during-load row $kind describes foreign actors."}
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $w=$stages['native-write-complete'].detail
    $p=$stages['disable-load-probed'].detail
    $r=$stages['disable-load-rest-cycle'].detail
    $pp=$stages['disable-load-preroutine-probe'].detail
    $s=$stages['disable-load-second-load'].detail
    if($w.ordinal-ne1-or$w.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$riderId-or$w.snapshot.Mount.Id-cne$mountId){
        throw 'P07 disable-during-load archive is not the exact mounted opening write.'
    }
    $pending=Get-KmcOptionalMember $p.probes 'semantic-restored-presentation-pending'
    if($null-eq$pending-or[int]$pending-lt1){throw 'P07 disable-during-load never probed the semantic-restored/presentation-pending boundary.'}
    foreach($phase in @($p.probes.PSObject.Properties.Name)){
        $probed=[int]$p.probes.$phase;$refused=Get-KmcOptionalMember $p.refusals $phase
        if($null-eq$refused-or[int]$refused-ne$probed){throw "P07 registered disable was accepted during the live load at $phase."}
    }
    if($stages['disable-load-probed'].relationship-cne'Mounted'-or$p.enabledThroughout-ne$true-or$p.enabled-ne$true-or
        $p.semantics-ne($p.semanticsBefore+2)-or$p.presentation-ne($p.presentationBefore+1)-or$p.nativeCastRequests-ne0-or
        $p.archiveSha256-cne$w.sha256-or$p.riderId-cne$riderId-or$p.mountId-cne$mountId){
        throw 'P07 refused disables did not leave the load to restore the pair exactly once.'
    }
    if($stages['disable-load-rest-cycle'].relationship-cne'Mounted'-or$r.restDisabled-ne$true-or$r.stateDisabled-cne'Unmounted'-or
        $r.restReEnabled-ne$true-or$r.stateReEnabled-cne'Unmounted'-or$r.restRemounted-ne$true-or$null-ne$r.restInvariants-or
        $r.factsMounted-le0-or$r.factsDisabled-ge$r.factsMounted-or$r.factsReEnabled-le$r.factsDisabled-or$r.factsReEnabled-gt$r.factsMounted-or
        $r.nativeCastRequests-ne0-or$stages['disable-load-rest-cycle'].rider.Id-cne$riderId-or$stages['disable-load-rest-cycle'].mount.Id-cne$mountId){
        throw 'P07 equivalent-state disable/re-enable cycle at rest did not return the same pair once.'
    }
    # The pre-routine probe is recorded as what production did; an accepted
    # disable is admissible only when the load did not yet own the world, and
    # its outcome must then be a clean load with no restoration.
    if($pp.preRoutineAccepted-eq$true-and$pp.preRoutineInFlight-ne$false){throw 'P07 a disable was accepted while the load owned the world.'}
    if($pp.preRoutineAccepted-eq$true){
        if($s.stateAfterSecond-cne'Unmounted'-or$s.semanticsDelta-ne0-or$s.presentationDelta-ne0-or$s.secondReEnabled-ne$true){
            throw 'P07 a load under a disabled KMC did not open cleanly with no restoration.'
        }
    }else{
        if($s.stateAfterSecond-cne'Mounted'-or$s.semanticsDelta-ne2-or$s.presentationDelta-ne1-or$s.secondDisabled-ne$true-or$s.secondReEnabled-ne$true){
            throw 'P07 a refused pre-routine disable did not leave the load to restore the pair once.'
        }
    }
    # The remount is judged after native frames have run the production
    # invariant check: a same-frame remount that the next frame invalidates
    # would leave the relationship Unmounted or its invariants broken here.
    if($stages['disable-load-second-load'].relationship-cne'Mounted'-or$s.secondRemounted-ne$true-or$null-ne$s.secondInvariants-or$s.nativeCastRequests-ne0-or
        $stages['disable-load-second-load'].rider.Id-cne$riderId-or$stages['disable-load-second-load'].mount.Id-cne$mountId){
        throw 'P07 remount after the second load did not reuse the same pair once.'
    }
    if((Get-KmcSha256 (Join-Path $root 'Manual_300_KMC_P01.zks'))-cne$w.sha256){throw 'P07 disable-during-load changed its archive on disk.'}
}

# The integration-absent cold load: every KMC guard removed and services off
# before the native load, the clean cleanup archive opened by the engine alone,
# no KMC restoration, admission or snapshot, and the archive byte-identical.
function Assert-KmcAbsentLoadEvidence {
    param($Request,$Rows)
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    $done=@($Rows|Where-Object kind -CEQ 'absent-load-complete')
    if($initial.Count-ne1-or$done.Count-ne1){throw 'P07 integration-absent load lacks its exact initial and completion observations.'}
    foreach($row in @($initial[0],$done[0])){
        if($row.checkpoint-cne'absent-kmc'-or$row.relationship-ceq'Mounted'-or$null-ne$row.rider-or$null-ne$row.mount){
            throw 'P07 integration-absent rows must carry no pair and the declared case.'
        }
    }
    $d=$done[0].detail
    $load=$Request.persistenceLoad
    if($d.integrationDetached-ne$true-or$d.bridgeInstalled-ne$false-or$d.enabled-ne$false-or$d.loadedData-ne$false-or
        $d.semantics-ne0-or$d.presentation-ne0-or$d.nativeCastRequests-ne0-or$d.activeScope-ne$false-or
        $d.gameId-cne$load.gameId-or$d.gameId-cne$Request.fixture.working.gameId-or$d.area-cne$load.area-or
        $d.expectedGameId-cne$load.gameId-or$d.expectedArea-cne$load.area-or$d.party-lt1-or$d.kmcHorseUnits-ne0-or
        $d.sourceFileName-cne'Manual_301_KMC_CLEANUP.zks'-or$d.sourceFileName-cne$load.fileName-or
        $d.archiveSha256-cne$load.sha256-or$d.expectedSha256-cne$load.sha256-or$d.mode-cne'Default'){
        throw 'P07 integration-absent load did not open the clean archive with KMC detached and nothing restored.'
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $members=Read-KmcCampaignArchiveMembers -Path (Join-Path $root $load.fileName)
    if($members.header.Name-cne'KMC_CLEANUP'-or$members.header.GameId-cne$load.gameId-or$null-eq$members.kmc-or
        (Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false){
        throw 'P07 integration-absent load did not open a clean cleanup archive.'
    }
    # Ordinary play without KMC: a real ground click for the main character and a
    # NEW native save the engine wrote alone, carrying no KMC member at all.
    $moved=@($Rows|Where-Object kind -CEQ 'absent-moved')
    $saved=@($Rows|Where-Object kind -CEQ 'absent-saved')
    if($moved.Count-ne1-or$saved.Count-ne1){throw 'P07 integration-absent load lacks its movement and save observations.'}
    foreach($row in @($moved[0],$saved[0])){
        if($row.checkpoint-cne'absent-kmc'-or$row.relationship-ceq'Mounted'-or$null-ne$row.rider-or$null-ne$row.mount){throw 'P07 integration-absent rows must carry no pair and the declared case.'}
    }
    if($done[0].stage-gt$moved[0].stage-or$moved[0].stage-gt$saved[0].stage){throw 'P07 integration-absent rows are out of their measured order.'}
    $m=$moved[0].detail;$s=$saved[0].detail;$ar=$s.archive
    if([string]::IsNullOrEmpty([string]$m.mover)-or$m.displacement-lt1.5-or$m.relationship-cne'Unmounted'-or$m.partyCombat-ne$false-or$m.bridgeInstalled-ne$false-or$m.enabled-ne$false){
        throw 'P07 main character did not move through ordinary native input with KMC detached.'
    }
    if($null-eq$ar-or$ar.leaf-cne'Manual_302_KMC_ABSENT2.zks'-or$ar.internalName-cne'KMC_ABSENT2'-or$ar.nativeType-cne'Manual'-or$ar.operation-cne'None'-or
        $ar.gameId-cne$load.gameId-or$ar.area-cne$load.area-or$ar.kmcMember-cne'Missing'-or$ar.sha256-cnotmatch'^[0-9a-f]{64}$'-or$ar.length-le0-or
        $s.sourceSha256-cne$load.sha256-or$s.expectedSourceSha256-cne$load.sha256-or$s.snapshots-ne0-or$s.bridgeInstalled-ne$false){
        throw 'P07 integration-absent save is not a new engine-only archive over an intact cleanup archive.'
    }
    $second=Join-Path $root $ar.leaf
    if(-not(Test-Path -LiteralPath $second -PathType Leaf)-or(Get-KmcSha256 $second)-cne$ar.sha256-or$ar.path-cne$second){throw 'P07 integration-absent second archive did not survive its own run.'}
    if((Get-KmcSha256 (Join-Path $root $load.fileName))-cne$load.sha256){throw 'P07 integration-absent load changed the cleanup archive on disk.'}
    $written=Read-KmcCampaignArchiveMembers -Path $second
    if($written.header.Name-cne'KMC_ABSENT2'-or$written.header.GameId-cne$load.gameId-or$null-ne$written.kmc){throw 'P07 integration-absent second archive on disk carries a KMC member or another identity.'}
}

# A death archive as recorded and as it lies on disk: the fixture campaign's own
# native save whose KMC member records no pair at all.
function Assert-KmcDeathArchive {
    param($Recorded,[string]$Root,[string]$GameId,[string]$Area)
    if($null-eq$Recorded){throw 'P07 death lacks its archive observation.'}
    if($Recorded.leaf-cne'Manual_301_KMC_DEATH.zks'-or$Recorded.internalName-cne'KMC_DEATH'-or$Recorded.nativeType-cne'Manual'-or
        $Recorded.operation-cne'None'-or$Recorded.gameId-cne$GameId-or$Recorded.area-cne$Area-or$Recorded.kmcMember-cne'Current'-or
        $Recorded.sha256-cnotmatch'^[0-9a-f]{64}$'-or$Recorded.length-le0-or$Recorded.path-cne(Join-Path $Root $Recorded.leaf)){
        throw 'P07 death archive is not the exact declared no-pair save of the fixture campaign.'
    }
    $s=$Recorded.snapshot
    if($null-eq$s-or(Get-KmcOptionalMember $s 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $s 'Rider')-or
        $null-ne(Get-KmcOptionalMember $s 'Mount')-or(Get-KmcOptionalMember $s 'CampaignId')-cne$GameId){
        throw 'P07 death archive recorded a pair or a foreign campaign.'
    }
    $path=Join-Path $Root $Recorded.leaf
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw 'P07 death archive did not survive its own run.'}
    if((Get-KmcSha256 $path)-cne$Recorded.sha256-or(Get-Item -LiteralPath $path).Length-ne$Recorded.length){
        throw 'P07 death archive bytes changed after the run recorded them.'
    }
    $members=Read-KmcCampaignArchiveMembers -Path $path
    if($members.header.Name-cne'KMC_DEATH'-or$members.header.Type-cne'Manual'-or$members.header.GameId-cne$GameId-or
        $members.header.Area-cne$Area-or$null-eq$members.kmc-or(Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false-or
        $null-ne(Get-KmcOptionalMember $members.kmc 'Rider')-or$null-ne(Get-KmcOptionalMember $members.kmc 'Mount')){
        throw 'P07 death archive on disk is not the recorded no-pair save.'
    }
}

# The harmful lifecycle boundary across persistence: real native enemy damage
# kills the subject while mounted, the lifecycle cleanup ends the pair, a NEW
# native save records no pair, and the in-process reload invents nothing while
# the native death state persists.
function Assert-KmcDeathEvidence {
    param($Request,$Rows)
    $subjectIsMount=$Request.persistenceCase-ceq'mount-death'
    $order=@('native-write-complete','death-dispatched','death-cleanup','death-save-admission','death-saved','death-reloaded','death-policy-restored')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 death lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P07 death lacks its initial state.'}
    $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne$Request.persistenceCase){throw "P07 death row $kind is not the declared case."}
        if($row.stage-lt$previous){throw "P07 death row $kind is out of its measured order."}
        if(($null-ne$row.rider-and$row.rider.Id-cne$riderId)-or($null-ne$row.mount-and$row.mount.Id-cne$mountId)){throw "P07 death row $kind describes foreign actors."}
        $previous=$row.stage
    }
    $gameId=[string]$Request.fixture.working.gameId;$area=[string]$Request.fixture.working.area
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $w=$stages['native-write-complete'].detail
    $d=$stages['death-dispatched'].detail
    $c=$stages['death-cleanup'].detail
    $s=$stages['death-saved'].detail
    $r=$stages['death-reloaded'].detail
    if($w.ordinal-ne1-or$w.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$riderId-or$w.snapshot.Mount.Id-cne$mountId){
        throw 'P07 death first archive is not the exact mounted opening write.'
    }
    foreach($x in @($d,$c,$s,$r)){
        if($x.riderId-cne$riderId-or$x.mountId-cne$mountId-or$x.subjectIsMount-ne$subjectIsMount){throw 'P07 death rows do not name the exact pair and subject.'}
    }
    # The stimulus: a real native enemy, lethal under the unchanged difficulty,
    # while the pair was still mounted.
    if($stages['death-dispatched'].relationship-cne'Mounted'-or[string]::IsNullOrEmpty([string]$d.sourceId)-or$d.sourceId-ceq$riderId-or$d.sourceId-ceq$mountId-or
        $d.requestedDamage-le0-or$d.damageToParty-le0-or$d.deathThreshold-le0-or$d.partyCombat-ne$true){
        throw 'P07 death stimulus was not real native enemy damage delivered while mounted.'
    }
    # The death policy: a non-essential subject dies permanently under the scoped
    # policy for this process only; an essential subject (a final death is the
    # engine's own game over) dies under the campaign's live policy, unchanged.
    # Either way the damage multiplier and the persisted settings are untouched
    # and the cached settings are verified restored at the end.
    $pol=Get-KmcOptionalMember $d 'deathPolicy'
    $dec=Get-KmcOptionalMember $d 'policyDecision'
    if($null-eq$pol-or$null-eq$dec-or$pol.permanentDeathFixture-ne$dec.permanent-or$dec.conscious-ne$true-or$dec.immortal-ne$false-or
        $pol.effective.damageToParty-ne$pol.before.damageToParty-or$pol.effective.riseAfterCombat.persisted-cne$pol.before.riseAfterCombat.persisted-or
        $pol.effective.deathDoor.persisted-cne$pol.before.deathDoor.persisted){
        throw 'P07 death policy is not recorded as one decision with the difficulty multiplier and persisted settings untouched.'
    }
    $permanent=$dec.permanent-eq$true
    if($permanent){
        if($pol.effective.trueDeath-ne$true-or$pol.effective.deathDoorCondition-ne$false-or$dec.essential-eq$true-or$dec.mainCharacter-eq$true){
            throw 'P07 permanent death was not delivered under the scoped policy for a non-essential subject.'
        }
    }else{
        if(($dec.essential-ne$true-and$dec.mainCharacter-ne$true)-or$pol.effective.trueDeath-ne$pol.before.trueDeath-or
            $pol.effective.deathDoorCondition-ne$pol.before.deathDoorCondition){
            throw 'P07 live death policy is admitted only for an essential subject and must be the campaign own.'
        }
    }
    $pr=Get-KmcOptionalMember $stages['death-policy-restored'].detail 'deathPolicy'
    $res=if($null-ne$pr){Get-KmcOptionalMember $pr 'restoration'}else{$null}
    if($null-eq$res-or$res.restored-ne$true){throw 'P07 death did not restore the scoped death policy exactly.'}
    # Cleanup: the subject is dead, the pair is gone, the partner is unharmed.
    if($stages['death-cleanup'].relationship-ceq'Mounted'-or$c.subjectDead-ne$true-or$c.survivorConscious-ne$true-or
        $c.survivorDamage-ne$c.survivorDamageBefore-or$c.saveSuspended-ne$false-or$c.activeScope-ne$false-or$c.relationship-cne'Unmounted'){
        throw 'P07 native death did not end the pair with the partner unharmed.'
    }
    # The admission: the engine's own allowance after the encounter, recorded
    # component by component, with the native death intact and no game over.
    $a=$stages['death-save-admission'].detail
    $adm=Get-KmcOptionalMember $a 'admission'
    if($null-eq$adm-or$adm.saveAllowed-ne$true-or$adm.partyCombat-ne$false-or$null-ne$adm.gameOverReason-or$adm.mode-cne'Default'-or
        $adm.areaLoaded-ne$true-or$adm.survivorLifeState-cne'Conscious'-or$adm.subjectLifeState-cnotin @('Conscious','Unconscious','Dead')){
        throw 'P07 death save admission was not the engine own allowance with the partner intact.'
    }
    # Permanent: the death persists to the admission. Live policy: the subject
    # is alive again only through the engine's own rule (TrueDeath false), and
    # whatever native life state it left is the one that must persist.
    $life=[string]$adm.subjectLifeState
    if($permanent-and($life-cne'Dead'-or$adm.subjectDead-ne$true)){throw 'P07 permanent death did not persist to the save admission.'}
    if(-not$permanent-and$life-cne'Dead'-and$pol.effective.trueDeath-ne$false){throw 'P07 the subject came back to life without the engine own rule.'}
    # The save: a NEW no-pair archive, first archive untouched, native life state intact.
    if($stages['death-saved'].relationship-ceq'Mounted'-or$s.subjectLifeState-cne$life-or$s.lifeStateAtSave-cne$life-or$s.survivorConscious-ne$true-or$s.snapshots-ne2-or
        $s.failedSaves-ne0-or$s.partyCombat-ne$false-or$s.firstHash-cne$w.sha256-or$s.archiveHash-cne$s.archive.sha256){
        throw 'P07 death save is not one new archive over an intact first archive with the native life state intact.'
    }
    Assert-KmcDeathArchive -Recorded $s.archive -Root $root -GameId $gameId -Area $area
    if((Get-KmcSha256 (Join-Path $root 'Manual_300_KMC_P01.zks'))-cne$w.sha256){throw 'P07 death save changed the first archive on disk.'}
    # The reload: nothing invented, nothing restored, the native life state persisted.
    if($stages['death-reloaded'].relationship-ceq'Mounted'-or$r.subjectPresent-ne$true-or$r.subjectReloadedLifeState-cne$life-or$r.expectedLifeState-cne$life-or
        $r.survivorPresent-ne$true-or$r.survivorConscious-ne$true-or$r.loadedDataMounted-ne$false-or$r.semanticsDelta-ne0-or$r.presentationDelta-ne0-or
        $r.nativeCastRequests-ne0-or$r.archiveHash-cne$s.archive.sha256){
        throw 'P07 reloading the death save invented a pair, restored something, or changed the native life state.'
    }
}

# The fresh-process control of the death archive.
function Assert-KmcDeathColdEvidence {
    param($Request,$Rows)
    $subjectIsMount=$Request.persistenceCase-ceq'mount-death'
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    $done=@($Rows|Where-Object kind -CEQ 'death-cold-complete')
    if($initial.Count-ne1-or$done.Count-ne1){throw 'P07 death cold load lacks its exact initial and completion observations.'}
    foreach($row in @($initial[0],$done[0])){
        if($row.checkpoint-cne$Request.persistenceCase-or$row.relationship-ceq'Mounted'-or$null-ne$row.rider-or$null-ne$row.mount){
            throw 'P07 death cold rows must carry no pair and the declared case.'
        }
    }
    $d=$done[0].detail;$load=$Request.persistenceLoad
    # The world is read from the loaded state (the engine drops a finally dead
    # pet from the party): exactly one supported mount, dead exactly when it was
    # the subject; a dead rider is only ever a non-mount and never more than one
    # player-faction unit is dead. Which life state the rider must carry is bound
    # by the ledger to the source run's recorded admission.
    $lifeStates=Get-KmcOptionalMember $d 'lifeStates'
    $deadIds=@($d.deadIds)
    if($d.case-cne$Request.persistenceCase-or$d.party-lt1-or$d.supportedMounts-ne1-or[string]::IsNullOrEmpty([string]$d.mountId)-or$d.mountDead-ne$subjectIsMount-or
        $d.deadPlayerFaction-gt1-or$deadIds.Count-ne$d.deadPlayerFaction-or
        ($subjectIsMount-and($d.deadPlayerFaction-ne1-or$deadIds-cnotcontains[string]$d.mountId-or$d.mountLifeState-cne'Dead'))-or
        (-not$subjectIsMount-and($deadIds-ccontains[string]$d.mountId-or$d.mountLifeState-cne'Conscious'))-or
        $null-eq$lifeStates-or$null-eq(Get-KmcOptionalMember $lifeStates ([string]$d.mountId))-or$lifeStates.([string]$d.mountId).lifeState-cne$d.mountLifeState-or
        $d.loadedDataPresent-ne$true-or$d.loadedDataMounted-ne$false-or$d.semantics-ne0-or$d.presentation-ne0-or$d.nativeCastRequests-ne0-or
        $d.gameId-cne$load.gameId-or$d.gameId-cne$Request.fixture.working.gameId-or$d.area-cne$load.area-or
        $d.archiveSha256-cne$load.sha256-or$d.expectedSha256-cne$load.sha256){
        throw 'P07 death cold load did not open the no-pair archive with exactly the recorded native death and nothing invented.'
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $members=Read-KmcCampaignArchiveMembers -Path (Join-Path $root $load.fileName)
    if($members.header.Name-cne'KMC_DEATH'-or$members.header.GameId-cne$load.gameId-or$null-eq$members.kmc-or
        (Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false){
        throw 'P07 death cold load did not open a no-pair death archive.'
    }
}

# The eligibility archive as recorded and as it lies on disk: the fixture
# campaign's own native save whose KMC member records no pair at all.
function Assert-KmcEligibilityArchive {
    param($Recorded,[string]$Root,[string]$GameId,[string]$Area)
    if($null-eq$Recorded){throw 'P07 eligibility lacks its archive observation.'}
    if($Recorded.leaf-cne'Manual_301_KMC_SIZE.zks'-or$Recorded.internalName-cne'KMC_SIZE'-or$Recorded.nativeType-cne'Manual'-or
        $Recorded.operation-cne'None'-or$Recorded.gameId-cne$GameId-or$Recorded.area-cne$Area-or$Recorded.kmcMember-cne'Current'-or
        $Recorded.sha256-cnotmatch'^[0-9a-f]{64}$'-or$Recorded.length-le0-or$Recorded.path-cne(Join-Path $Root $Recorded.leaf)){
        throw 'P07 eligibility archive is not the exact declared no-pair save of the fixture campaign.'
    }
    $s=$Recorded.snapshot
    if($null-eq$s-or(Get-KmcOptionalMember $s 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $s 'Rider')-or
        $null-ne(Get-KmcOptionalMember $s 'Mount')-or(Get-KmcOptionalMember $s 'CampaignId')-cne$GameId){
        throw 'P07 eligibility archive recorded a pair or a foreign campaign.'
    }
    $path=Join-Path $Root $Recorded.leaf
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw 'P07 eligibility archive did not survive its own run.'}
    if((Get-KmcSha256 $path)-cne$Recorded.sha256-or(Get-Item -LiteralPath $path).Length-ne$Recorded.length){
        throw 'P07 eligibility archive bytes changed after the run recorded them.'
    }
    $members=Read-KmcCampaignArchiveMembers -Path $path
    if($members.header.Name-cne'KMC_SIZE'-or$members.header.Type-cne'Manual'-or$members.header.GameId-cne$GameId-or
        $members.header.Area-cne$Area-or$null-eq$members.kmc-or(Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false-or
        $null-ne(Get-KmcOptionalMember $members.kmc 'Rider')-or$null-ne(Get-KmcOptionalMember $members.kmc 'Mount')){
        throw 'P07 eligibility archive on disk is not the recorded no-pair save.'
    }
}

# The live eligibility change across persistence: the engine's own size effect
# makes the mounted rider Large, KMC's invariant ends the pair, a NEW native
# save records no pair while the effect persists natively, and the in-process
# reload invents nothing while the enlarged rider and its effect persist.
function Assert-KmcEligibilityEvidence {
    param($Request,$Rows)
    $order=@('native-write-complete','eligibility-dispatched','eligibility-cleanup','eligibility-save-admission','eligibility-saved','eligibility-reloaded')
    $stages=@{}
    foreach($kind in $order){
        $matched=@($Rows|Where-Object kind -CEQ $kind)
        if($matched.Count-ne1){throw "P07 eligibility lacks its exact $kind observation."}
        $stages[$kind]=$matched[0]
    }
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P07 eligibility lacks its initial state.'}
    $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
    $previous=-1
    foreach($kind in $order){
        $row=$stages[$kind]
        if($row.checkpoint-cne'rider-size-change'){throw "P07 eligibility row $kind is not the declared case."}
        if($row.stage-lt$previous){throw "P07 eligibility row $kind is out of its measured order."}
        if(($null-ne$row.rider-and$row.rider.Id-cne$riderId)-or($null-ne$row.mount-and$row.mount.Id-cne$mountId)){throw "P07 eligibility row $kind describes foreign actors."}
        $previous=$row.stage
    }
    $gameId=[string]$Request.fixture.working.gameId;$area=[string]$Request.fixture.working.area
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $w=$stages['native-write-complete'].detail
    $d=$stages['eligibility-dispatched'].detail
    $c=$stages['eligibility-cleanup'].detail
    $a=$stages['eligibility-save-admission'].detail
    $s=$stages['eligibility-saved'].detail
    $r=$stages['eligibility-reloaded'].detail
    if($w.ordinal-ne1-or$w.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.Rider.Id-cne$riderId-or$w.snapshot.Mount.Id-cne$mountId){
        throw 'P07 eligibility first archive is not the exact mounted opening write.'
    }
    foreach($x in @($d,$c,$a,$s,$r)){
        if($x.riderId-cne$riderId-or$x.mountId-cne$mountId-or$x.buffName-cne'EnlargePersonBuff'-or$x.buffGuid-cnotmatch'^[0-9a-f]{32}$'-or$x.buffGuid-cne$d.buffGuid){
            throw 'P07 eligibility rows do not name the exact pair and the engine own size effect.'
        }
    }
    # The stimulus: the engine's size effect applied to a Medium mounted rider.
    if($stages['eligibility-dispatched'].relationship-cne'Mounted'-or$d.sizeBefore-ne4-or$d.riderCarriesEffect-ne$true-or$d.partyCombat-ne$false){
        throw 'P07 eligibility stimulus was not the native size effect on a Medium mounted rider outside combat.'
    }
    # Cleanup: KMC's own invariant ended the pair; both actors alive; effect in place.
    if($stages['eligibility-cleanup'].relationship-cne'Unmounted'-or$c.sizeAfter-le4-or$c.riderSize-ne$c.sizeAfter-or$c.riderCarriesEffect-ne$true-or
        $c.mountConscious-ne$true-or$c.saveSuspended-ne$false-or$c.activeScope-ne$false){
        throw 'P07 eligibility change did not end the pair by the invariant with both actors alive.'
    }
    # Admission: the engine's own allowance, outside combat, effect still there.
    if($stages['eligibility-save-admission'].relationship-cne'Unmounted'-or$a.saveAllowed-ne$true-or$a.partyCombat-ne$false-or$a.mode-cne'Default'-or
        $a.riderSize-ne$c.sizeAfter-or$a.riderCarriesEffect-ne$true){
        throw 'P07 eligibility save admission was not the engine own allowance with the effect intact.'
    }
    # The save: a NEW no-pair archive, first archive untouched, effect intact.
    if($stages['eligibility-saved'].relationship-cne'Unmounted'-or$s.riderSize-ne$c.sizeAfter-or$s.riderCarriesEffect-ne$true-or$s.snapshots-ne2-or
        $s.failedSaves-ne0-or$s.partyCombat-ne$false-or$s.firstHash-cne$w.sha256-or$s.archiveHash-cne$s.archive.sha256){
        throw 'P07 eligibility save is not one new archive over an intact first archive with the effect intact.'
    }
    Assert-KmcEligibilityArchive -Recorded $s.archive -Root $root -GameId $gameId -Area $area
    if((Get-KmcSha256 (Join-Path $root 'Manual_300_KMC_P01.zks'))-cne$w.sha256){throw 'P07 eligibility save changed the first archive on disk.'}
    # The reload: nothing invented, nothing restored, enlarged rider and effect persisted.
    if($stages['eligibility-reloaded'].relationship-cne'Unmounted'-or$r.riderPresent-ne$true-or$r.riderSize-ne$c.sizeAfter-or$r.riderCarriesEffect-ne$true-or
        $r.mountPresent-ne$true-or$r.mountConscious-ne$true-or$r.loadedDataMounted-ne$false-or$r.semanticsDelta-ne0-or$r.presentationDelta-ne0-or
        $r.nativeCastRequests-ne0-or$r.archiveHash-cne$s.archive.sha256){
        throw 'P07 reloading the eligibility save invented a pair, restored something, or lost the native effect.'
    }
}

# The fresh-process control of the eligibility archive: exactly one party
# member is Large and carries the engine's size effect, it is not the mount,
# no pair is restored and nothing is invented.
function Assert-KmcEligibilityColdEvidence {
    param($Request,$Rows)
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    $done=@($Rows|Where-Object kind -CEQ 'eligibility-cold-complete')
    if($initial.Count-ne1-or$done.Count-ne1){throw 'P07 eligibility cold load lacks its exact initial and completion observations.'}
    foreach($row in @($initial[0],$done[0])){
        if($row.checkpoint-cne'rider-size-change'-or$row.relationship-ceq'Mounted'-or$null-ne$row.rider-or$null-ne$row.mount){
            throw 'P07 eligibility cold rows must carry no pair and the declared case.'
        }
    }
    $d=$done[0].detail;$load=$Request.persistenceLoad
    $states=Get-KmcOptionalMember $d 'states'
    $affectedIds=@($d.affectedIds)
    if($d.case-cne'rider-size-change'-or$d.buffName-cne'EnlargePersonBuff'-or$d.buffGuid-cnotmatch'^[0-9a-f]{32}$'-or
        $d.party-lt1-or$d.affected-ne1-or$affectedIds.Count-ne1-or$d.large-ne1-or$d.supportedMounts-ne1-or[string]::IsNullOrEmpty([string]$d.mountId)-or
        $affectedIds-ccontains[string]$d.mountId-or$null-eq$states-or$null-eq(Get-KmcOptionalMember $states $affectedIds[0])-or
        $states.($affectedIds[0]).size-le4-or$states.($affectedIds[0]).carriesEffect-ne$true-or$states.($affectedIds[0]).conscious-ne$true-or
        $null-eq(Get-KmcOptionalMember $states ([string]$d.mountId))-or$states.([string]$d.mountId).carriesEffect-ne$false-or$states.([string]$d.mountId).conscious-ne$true-or
        $d.loadedDataPresent-ne$true-or$d.loadedDataMounted-ne$false-or$d.semantics-ne0-or$d.presentation-ne0-or$d.nativeCastRequests-ne0-or
        $d.gameId-cne$load.gameId-or$d.gameId-cne$Request.fixture.working.gameId-or$d.area-cne$load.area-or
        $d.archiveSha256-cne$load.sha256-or$d.expectedSha256-cne$load.sha256){
        throw 'P07 eligibility cold load did not open the no-pair archive with exactly the enlarged rider it recorded and nothing invented.'
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $members=Read-KmcCampaignArchiveMembers -Path (Join-Path $root $load.fileName)
    if($members.header.Name-cne'KMC_SIZE'-or$members.header.GameId-cne$load.gameId-or$null-eq$members.kmc-or
        (Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false){
        throw 'P07 eligibility cold load did not open a no-pair size archive.'
    }
}

# Campaign B's own manual archive opened in a fresh process: B's identity and
# area, no pair, nothing restored or invented, ordinary movement of B's main
# character and a NEW native save in B, with B's source byte-identical.
function Assert-KmcCampaignBColdEvidence {
    param($Request,$Rows)
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    $loaded=@($Rows|Where-Object kind -CEQ 'campaign-b-cold-loaded')
    $moved=@($Rows|Where-Object kind -CEQ 'campaign-b-cold-moved')
    $saved=@($Rows|Where-Object kind -CEQ 'campaign-b-cold-saved')
    if($initial.Count-ne1-or$loaded.Count-ne1-or$moved.Count-ne1-or$saved.Count-ne1){throw 'P07 campaign B cold load lacks its exact observations.'}
    foreach($row in @($initial[0],$loaded[0],$moved[0],$saved[0])){
        if($row.checkpoint-cne'campaign-b'-or$row.relationship-ceq'Mounted'-or$null-ne$row.rider-or$null-ne$row.mount){
            throw 'P07 campaign B cold rows must carry no pair and the declared case.'
        }
    }
    if($loaded[0].stage-gt$moved[0].stage-or$moved[0].stage-gt$saved[0].stage){throw 'P07 campaign B cold rows are out of their measured order.'}
    $l=$loaded[0].detail;$m=$moved[0].detail;$s=$saved[0].detail;$load=$Request.persistenceLoad
    $fixtureId=[string]$Request.fixture.working.gameId
    if($l.case-cne'campaign-b'-or$l.gameId-cne$load.gameId-or$l.gameId-ceq$fixtureId-or$l.expectedGameId-cne$load.gameId-or$l.fixtureGameId-cne$fixtureId-or
        $l.area-cne$load.area-or$l.expectedArea-cne$load.area-or$l.area-ceq$Request.fixture.working.area-or$l.party-lt1-or$l.supportedMounts-ne0-or
        [string]::IsNullOrEmpty([string]$l.mainCharacter)-or$l.loadedDataPresent-ne$true-or$l.loadedDataMounted-ne$false-or$l.loadedDataCampaign-cne$load.gameId-or
        $l.bindings-ne0-or$l.semantics-ne0-or$l.presentation-ne0-or$l.nativeCastRequests-ne0-or$l.archiveSha256-cne$load.sha256-or$l.expectedSha256-cne$load.sha256){
        throw 'P07 campaign B cold load did not open B own world with nothing restored or invented.'
    }
    if($m.mover-cne$l.mainCharacter-or$m.displacement-lt1.5-or$m.relationship-cne'Unmounted'-or$m.partyCombat-ne$false){
        throw 'P07 campaign B main character did not move through ordinary native input.'
    }
    $ar=$s.archive
    if($null-eq$ar-or$ar.leaf-cne'Manual_303_KMC_B2.zks'-or$ar.internalName-cne'KMC_B2'-or$ar.nativeType-cne'Manual'-or$ar.operation-cne'None'-or
        $ar.gameId-cne$load.gameId-or$ar.area-cne$load.area-or$ar.kmcMember-cne'Current'-or$ar.sha256-cnotmatch'^[0-9a-f]{64}$'-or$ar.length-le0-or
        $null-eq$ar.snapshot-or(Get-KmcOptionalMember $ar.snapshot 'Mounted')-ne$false-or$null-ne(Get-KmcOptionalMember $ar.snapshot 'Rider')-or
        (Get-KmcOptionalMember $ar.snapshot 'CampaignId')-cne$load.gameId-or(Get-KmcOptionalMember $ar.snapshot 'AreaId')-cne$load.area-or
        $s.sourceSha256-cne$load.sha256-or$s.expectedSourceSha256-cne$load.sha256-or$s.snapshots-ne1-or$s.failedSaves-ne0){
        throw 'P07 campaign B second save is not B own new clean archive over an intact source.'
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $second=Join-Path $root $ar.leaf
    if(-not(Test-Path -LiteralPath $second -PathType Leaf)-or(Get-KmcSha256 $second)-cne$ar.sha256-or$ar.path-cne$second){throw 'P07 campaign B second archive did not survive its own run.'}
    if((Get-KmcSha256 (Join-Path $root $load.fileName))-cne$load.sha256){throw 'P07 campaign B cold load changed its source archive on disk.'}
    $members=Read-KmcCampaignArchiveMembers -Path $second
    if($members.header.Name-cne'KMC_B2'-or$members.header.GameId-cne$load.gameId-or$members.header.Area-cne$load.area-or$null-eq$members.kmc-or
        (Get-KmcOptionalMember $members.kmc 'Mounted')-ne$false-or(Get-KmcOptionalMember $members.kmc 'CampaignId')-cne$load.gameId){
        throw 'P07 campaign B second archive on disk is not B own clean save.'
    }
    $source=Read-KmcCampaignArchiveMembers -Path (Join-Path $root $load.fileName)
    if($source.header.Name-cne'KMC_B'-or$source.header.GameId-cne$load.gameId-or$source.header.GameId-ceq$fixtureId){throw 'P07 campaign B cold load did not open B own manual archive.'}
}

function Assert-KmcRecoveryPersistenceEvidence {
    param($Request,$Rows)
    if($Request.persistenceCase-cin @('serialization-cancel','serialization-cancel-output')){
        Assert-KmcWorkerDrainEvidence $Request $Rows; return
    }
    if($Request.persistenceCase-ceq'disable-reenable'){ Assert-KmcDisableLifecycleEvidence $Request $Rows; return }
    if($Request.persistenceCase-ceq'campaign-b'){ Assert-KmcCampaignBEvidence $Request $Rows; return }
    if($Request.persistenceCase-ceq'prepare-removal'){ Assert-KmcRemovalEvidence $Request $Rows; return }
    if($Request.persistenceCase-ceq'disable-during-load'){ Assert-KmcDisableLoadEvidence $Request $Rows; return }
    if($Request.persistenceCase-cin @('rider-death','mount-death')){ Assert-KmcDeathEvidence $Request $Rows; return }
    if($Request.persistenceCase-ceq'rider-size-change'){ Assert-KmcEligibilityEvidence $Request $Rows; return }
    $initial=@($Rows|Where-Object kind -CEQ 'recovery-initial-write')
    $wait=@($Rows|Where-Object kind -CEQ 'recovery-wait-started')
    $commit=$Request.persistenceCase-ceq'locked-replace'
    $failedKind=if($commit){'recovery-failed-commit'}else{'recovery-unwritten-operation'}
    $failed=@($Rows|Where-Object kind -CEQ $failedKind)
    $loaded=@($Rows|Where-Object kind -CEQ 'recovery-last-good-loaded')
    $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
    if($initial.Count-ne1-or$wait.Count-ne1-or$failed.Count-ne1-or$loaded.Count-ne1-or$written.Count-ne1){
        throw 'P07 lacks its exact real write, wait, failure/cancel, prior-save reload and subsequent write observations.'
    }
    $archive=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games/Manual_300_KMC_P01.zks')
    $hash=$initial[0].detail.sha256
    foreach($row in @($initial[0],$wait[0],$failed[0],$loaded[0])){
        if($row.checkpoint-cne$Request.persistenceCase-or$row.detail.path-cne$archive-or
            $row.detail.sha256-cne$hash-or
            $row.detail.snapshots-ne$(if($commit-and$row.kind-cin @('recovery-failed-commit','recovery-last-good-loaded')){2}else{1})-or
            $row.detail.failedSaveCallback-ne$false-or$row.detail.canceledLoadCallback-ne$false){
            throw 'P07 unwritten request changed the previous archive or claimed success.'
        }
    }
    $failures=if($Request.persistenceCase-ceq'cancel-wait'){0}else{1}
    if($commit-and($wait[0].detail.replacementFailures-ne0-or$failed[0].detail.replacementFailures-ne1)){
        throw 'P07 lacks the actual failed replacement of its completed staging archive.'
    }
    if($failed[0].detail.failedSaves-ne$failures-or
        $wait[0].persistence.semantics-ne2-or$failed[0].persistence.semantics-ne2-or
        $wait[0].persistence.presentation-ne1-or$failed[0].persistence.presentation-ne1-or
        $failed[0].detail.nativeWorldDisposals-ne1-or$loaded[0].detail.nativeWorldDisposals-ne2-or
        $loaded[0].persistence.semantics-ne4-or$loaded[0].persistence.presentation-ne2-or
        ($commit-and$failed[0].gameTicks-lt$wait[0].gameTicks)-or
        (-not$commit-and$failed[0].gameTicks-le$wait[0].gameTicks)-or$failed[0].native.paused-ne$false-or
        $failed[0].native.mode-cne'Default'-or$failed[0].controls.SerializationSuspended-ne$false-or
        $loaded[0].detail.nativeCallback-ne$true-or$written[0].detail.ordinal-ne2-or
        $written[0].detail.sha256-ceq$hash){
        throw 'P07 lost recovery, completed-world ownership, real native reload or subsequent nonempty write semantics.'
    }
}

# One actor's native view evidence. A disposition label alone proves nothing:
# it must agree with the measured instance IDs, and the view must still be the
# live, bound, singly owned view of that exact actor.
function Assert-KmcAreaViewEvidence {
    param($Actor,[string]$Expected,[string]$Label)
    if($null-eq$Actor){throw "P07 area evidence has no $Label native view observation."}
    if($Actor.nativeActorCount-ne1-or$Actor.viewAlive-ne$true-or$Actor.viewBound-ne$true-or
        $Actor.viewExactForPair-ne$true-or$Actor.boundToRelationship-ne$true-or
        [string]::IsNullOrEmpty([string]$Actor.id)-or$null-eq$Actor.viewId){
        throw "P07 area $Label view is missing, duplicated, unbound or not the owned pair view."
    }
    if($Actor.viewDisposition-cne$Expected){
        throw "P07 area $Label native view was $($Actor.viewDisposition), not the verified $Expected contract."
    }
    if((([int]$Actor.viewId-eq[int]$Actor.baselineViewId))-ne($Expected-ceq'retained')){
        throw "P07 area $Label view disposition label contradicts its measured native instance IDs."
    }
}

# A cold load reconstructs the world, so it claims validity and unique binding
# only: instance identity is never compared across processes.
function Assert-KmcColdViewEvidence {
    param($Actor,[string]$Label)
    if($null-eq$Actor){throw "P07 cold evidence has no $Label native view observation."}
    if($Actor.nativeActorCount-ne1-or$Actor.viewAlive-ne$true-or$Actor.viewBound-ne$true-or
        $Actor.viewExactForPair-ne$true-or$Actor.boundToRelationship-ne$true-or
        [string]::IsNullOrEmpty([string]$Actor.id)-or$null-eq$Actor.viewId-or
        $Actor.viewDisposition-cne'cold-world'){
        throw "P07 cold $Label view is missing, duplicated, unbound or not the owned pair view."
    }
}

# The transition autosave each cross-area source actually produced, cold-loaded
# in its own process. AfterEntry committed in the destination after restoration;
# BeforeExit committed in the departure area before suspension.
function Assert-KmcTransitionAutoColdEvidence {
    param($Request,$Rows)
    $loaded=@($Rows|Where-Object kind -CEQ 'auto-cold-loaded')
    $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
    if($loaded.Count-ne1-or$written.Count-ne1){
        throw 'P07 transition autosave cold load lacks its exact load and subsequent write observations.'
    }
    $d=$loaded[0].detail
    $mode=if($Request.persistenceCase-ceq'area-cross-entry-auto'){'AfterEntry'}else{'BeforeExit'}
    $expected=if($mode-ceq'AfterEntry'){[string]$Request.persistenceAreaTarget.area}else{[string]$Request.fixture.working.area}
    if($d.case-cne$Request.persistenceCase-or$d.autoSaveMode-cne$mode-or$d.expectedArea-cne$expected-or
        $d.loadedArea-cne$expected-or$d.archiveArea-cne$expected-or
        $d.archiveCampaign-cne$Request.fixture.working.gameId-or
        $d.sourceFileName-cne'Auto_1.zks'-or$d.sourceSha256-cne$Request.persistenceLoad.sha256){
        throw 'P07 transition autosave cold load did not open the exact world its own archive captured.'
    }
    if($d.suspensions-ne0-or$d.resumes-ne0-or$d.pending-ne$false){
        throw 'P07 transition autosave cold load inherited transfer state from its source process.'
    }
    Assert-KmcColdViewEvidence $d.riderActor 'rider'
    Assert-KmcColdViewEvidence $d.mountActor 'mount'
    $w=$written[0].detail
    if($w.ordinal-ne1-or$w.sha256-ceq$Request.persistenceLoad.sha256-or$w.nativeType-cne'Manual'-or
        $w.snapshot.Mounted-ne$true-or$w.snapshot.AreaId-cne$expected-or
        $w.snapshot.CampaignId-cne$Request.fixture.working.gameId){
        throw 'P07 transition autosave cold load lacks a distinct subsequent write in its own area.'
    }
}

function Assert-KmcAreaPersistenceEvidence {
    param($Request,$Rows)
    $before=@($Rows|Where-Object kind -CEQ 'area-reload-requested')
    $observed=@($Rows|Where-Object kind -CEQ 'area-reload-observed')
    $after=@($Rows|Where-Object kind -CEQ 'area-reload-complete')
    $initial=@($Rows|Where-Object kind -CEQ 'area-initial-write')
    $autosave=@($Rows|Where-Object kind -CEQ 'area-native-autosave')
    $written=@($Rows|Where-Object kind -CEQ 'native-write-complete')
    $crossArea=$Request.persistenceCase-cin @('area-cross-entry','area-cross-exit')
    # A same-area reload opens with its own manual write; a cross-area transfer
    # instead uses the engine's authored autosave as its first native archive.
    if($Request.persistenceCase-cnotin @('area-reload','area-cross-entry','area-cross-exit')-or
        $before.Count-ne1-or$observed.Count-ne1-or$after.Count-ne1-or$written.Count-ne1-or
        $initial.Count-ne$(if($crossArea){0}else{1})-or$autosave.Count-ne$(if($crossArea){1}else{0})){
        throw 'P07 area case lacks its exact native request, pre-qualification view observation and two write completions.'
    }
    $kinds=@($Rows|ForEach-Object{$_.kind})
    if([Array]::IndexOf($kinds,'area-reload-observed')-le[Array]::IndexOf($kinds,'area-reload-requested')-or
        [Array]::IndexOf($kinds,'area-reload-observed')-ge[Array]::IndexOf($kinds,'area-reload-complete')){
        throw 'P07 area view observation must be recorded after the native request and before its qualification.'
    }
    $a=$before[0];$o=$observed[0];$b=$after[0];$d=$b.detail
    # Installed Game.LoadArea 06000CD5 passes (saveInfo != null) as
    # SceneLoader.UnloadEntitiesCoroutine 06008096's unloadCrossScene, and an
    # ordinary transfer passes none, so CrossSceneRoot and the party pair's
    # exact native views survive. Replacement is the save-load contract.
    $expectedDisposition='retained'
    if($d.expectedViewDisposition-cne$expectedDisposition-or$o.detail.expectedViewDisposition-cne$expectedDisposition-or
        $a.detail.expectedViewDisposition-cne$expectedDisposition){
        throw 'P07 area case must qualify the verified retained native cross-scene view contract.'
    }
    foreach($row in @($a,$o,$b)){
        Assert-KmcAreaViewEvidence $row.detail.riderActor $expectedDisposition 'rider'
        Assert-KmcAreaViewEvidence $row.detail.mountActor $expectedDisposition 'mount'
        if(-not[string]::IsNullOrEmpty([string]$row.detail.mountedInvariant)){
            throw ('P07 area left a broken mounted attachment invariant: '+$row.detail.mountedInvariant)
        }
    }
    if($o.detail.loadingInProcess-ne$false-or$o.detail.suspensions-ne1-or$o.detail.resumes-ne1-or
        $o.detail.sameWorld-ne$true-or$o.detail.area-cne$d.area-or$o.detail.nativeCastRequests-ne0-or
        $d.loadingInProcess-ne$false-or$d.queuedLoads-ne0-or$d.deferredSaveWaiting-ne$false){
        throw 'P07 area was qualified before the native world, suspension or loading queue actually settled.'
    }
    # The destination is whatever the case declared: the loaded fixture area for
    # a same-area reload, the declared native target for a real transfer.
    $expectedArea=if($crossArea){[string]$Request.persistenceAreaTarget.area}else{[string]$Request.fixture.working.area}
    $firstArchiveSha=if($crossArea){[string]$autosave[0].detail.sha256}else{[string]$initial[0].detail.sha256}
    if($a.detail.suspensions-ne0-or$a.detail.resumes-ne0-or$d.suspensions-ne1-or$d.resumes-ne1-or
        $d.pending-ne$false-or$d.suspensionObserved-ne$true-or$d.loadingFrames-lt1-or$d.sameWorld-ne$true-or
        $d.area-cne$expectedArea-or$d.expectedArea-cne$expectedArea-or$a.detail.expectedArea-cne$expectedArea-or
        $d.riderView-ne$a.detail.riderView-or$d.mountView-ne$a.detail.mountView-or$d.nativeCastRequests-ne0-or
        $b.persistence.semantics-ne0-or$b.persistence.presentation-ne0-or$b.native.paused-ne$false-or
        $b.controls.ExactFactCount-ne$a.controls.ExactFactCount-or
        $b.controls.ManagedHotbarSlotCount-ne$a.controls.ManagedHotbarSlotCount-or
        $b.controls.SerializationSuspended-ne$false-or$written[0].detail.ordinal-ne2-or
        $firstArchiveSha-ceq$written[0].detail.sha256){
        throw 'P07 area transfer lost native identity, exactly-once controls or actual post-area save.'
    }
    if(-not$crossArea-and$a.detail.area-cne$d.area){throw 'P07 same-area reload changed its native area.'}
    if($crossArea){
        if($a.detail.area-cne$Request.fixture.working.area-or$a.detail.sourceArea-cne$Request.fixture.working.area-or
            $d.sourceArea-cne$Request.fixture.working.area-or$expectedArea-ceq$Request.fixture.working.area){
            throw 'P07 cross-area transfer did not actually leave the loaded fixture area.'
        }
        $mode=[string]$Request.persistenceAreaTarget.autoSaveMode
        $x=$autosave[0].detail
        # BeforeExit autosaves the departure area before suspension; AfterEntry
        # autosaves the destination only once restoration has already happened.
        $autosaveArea=if($mode-ceq'AfterEntry'){$expectedArea}else{[string]$Request.fixture.working.area}
        $expectedResumes=if($mode-ceq'AfterEntry'){1}else{0}
        if($x.mode-cne$mode-or$x.expectedArea-cne$autosaveArea-or$x.nativeType-cne'Auto'-or
            $x.sha256-cnotmatch'^[0-9a-f]{64}$'-or$x.length-le0-or
            $x.snapshot.Mounted-ne$true-or$x.snapshot.AreaId-cne$autosaveArea-or
            $x.snapshot.CampaignId-cne$Request.fixture.working.gameId-or
            $x.snapshot.Rider.Id-cne$a.rider.Id-or$x.snapshot.Mount.Id-cne$a.mount.Id){
            throw 'P07 cross-area autosave is not the exact native mounted archive for its authored mode.'
        }
        $barrier=$x.barrier
        if($null-eq$barrier-or$barrier.relationship-cne'Mounted'-or$barrier.area-cne$autosaveArea-or
            $barrier.snapshots-ne1-or$barrier.resumes-ne$expectedResumes-or$barrier.suspensions-ne$expectedResumes-or
            $barrier.riderId-cne$a.rider.Id-or$barrier.mountId-cne$a.mount.Id){
            throw 'P07 cross-area autosave barrier did not follow the native restoration order.'
        }
        if([Array]::IndexOf($kinds,'area-native-autosave')-le[Array]::IndexOf($kinds,'area-reload-observed')-or
            [Array]::IndexOf($kinds,'area-native-autosave')-ge[Array]::IndexOf($kinds,'native-write-complete')){
            throw 'P07 cross-area autosave evidence is outside its native transfer window.'
        }
    }
    $elapsed=[Math]::Max(0,([double]$b.gameTicks-[double]$a.gameTicks)/10000000)
    foreach($actor in @('rider','mount')){
        foreach($cost in @('Standard','Move','Swift','Initiative','Reaction')){
            $expected=[Math]::Max(0,[double]$a.$actor.$cost-$elapsed)
            if([Math]::Abs([double]$b.$actor.$cost-$expected)-gt0.075){throw 'P07 area changed legitimate current debt.'}
        }
        if($a.$actor.ReactionsRemaining-ne$b.$actor.ReactionsRemaining){throw 'P07 area refreshed native reactions.'}
    }
}
