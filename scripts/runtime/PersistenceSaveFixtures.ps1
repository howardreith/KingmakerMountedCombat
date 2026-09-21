Set-StrictMode -Version Latest

# Bounded P01 archive intake. The source must be the exact completed archive
# from another restored owned process; no gameplay state is taken from evidence.
function Get-KmcPersistenceSource {
    param([Parameter(Mandatory=$true)][string]$SourceRunId,
        [Parameter(Mandatory=$true)][string]$ExpectedSha256,
        [Parameter(Mandatory=$true)]$Fixture,
        [AllowNull()][ValidateSet('manual','quick','auto','alternating','unmounted-spent','mounted-spent','unmounted-attack','mounted-attack')][string]$NativeCase,
        [switch]$Alternate)
    if($SourceRunId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or $SourceRunId -in @('.','..') -or
        $ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$'){throw 'Persistence source identity is invalid.'}
    $lab=Get-KmcLabRoot
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path $lab 'runtime-staging') 'owned persistence source'
    Assert-KmcDirectoryTreeCloneable $root 'owned persistence source'
    $owner=Read-KmcJson (Join-Path $root 'owner.json')
    $result=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$SourceRunId+'/runtime-result.json'))
    if($owner.runId-cne$SourceRunId-or$owner.scenario-cnotin @('persistence-p01-save','persistence-p02-save','persistence-p03-save','persistence-p04-save','persistence-p05-save')-or$result.status-cne'PASS'-or
        $result.runId-cne$SourceRunId-or$result.scenario-cne$owner.scenario-or
        $result.modsRestored-ne$true-or$result.workingRestored-ne$true-or
        $owner.transactionToken-cnotmatch'^[0-9a-f]{64}$'-or$owner.transactionToken-cne$result.transactionToken){throw 'Source is not a completed restored P01 save process.'}
    $isSlot=$owner.scenario-ceq'persistence-p05-save'
    if($isSlot){
        if([string]::IsNullOrEmpty($NativeCase)-or$owner.persistenceCase-cne$NativeCase){throw 'Source native slot category differs.'}
    }elseif($owner.scenario-ceq'persistence-p04-save'){
        if($NativeCase-cnotin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack')-or$owner.persistenceCase-cne$NativeCase){throw 'P04 source RT checkpoint differs.'}
    }elseif(-not[string]::IsNullOrEmpty($NativeCase)){throw 'Declared native case requires an exact P04/P05 source.'}
    if($Alternate-and$NativeCase-cne'alternating'){throw 'Second archive is restricted to the exact alternating source.'}
    $type=if($NativeCase-ceq'quick'){'Quick'}elseif($NativeCase-ceq'auto'){'Auto'}else{'Manual'}
    $manualName=if($Alternate){'KMC_P05_UNMOUNTED'}else{'KMC_P01'}
    $leaf=if($Alternate){'Manual_301_KMC_P05_UNMOUNTED.zks'}elseif($type-ceq'Manual'){'Manual_300_KMC_P01.zks'}else{$type+'_1.zks'}
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
            $header.GameId-cne$Fixture.working.gameId-or$header.GameName-cne$Fixture.working.gameName-or
            $header.Area-cne$Fixture.working.area){throw 'Owned archive native campaign/type/name differs.'}
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
    $kind=if($source){'native-write-complete'}else{'rt-cold-debt-restored'}
    $saved=@($Rows|Where-Object kind -CEQ $kind)
    if($saved.Count-ne1){throw 'P04 lacks actual selected native save metadata.'}
    $d=$saved[0].detail;$snapshot=$d.snapshot
    $activation=if($null-ne$snapshot.Combat.Paired){$snapshot.Combat.Paired.Activation}else{$null}
    $actor=@($snapshot.Combat.Actors|Where-Object {$_.Native.Id-ceq$initial[0].rider.Id})
    if($snapshot.Mounted-ne$mounted-or$snapshot.Combat.TurnBased-ne$false-or$actor.Count-ne1-or
        $actor[0].Native.Standard-le0.1-or$null-ne$snapshot.Combat.Current-or$snapshot.Combat.Roster.Count-ne0-or
        $null-ne$activation-or$snapshot.CampaignId-cne$Request.fixture.working.gameId-or
        $snapshot.AreaId-cne$Request.fixture.working.area){throw 'P04 snapshot lost real-time spent state or invented a turn.'}
    if($source){
        $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
        $path=Join-Path $root 'Manual_300_KMC_P01.zks'
        if($d.path-cne$path-or$d.nativeType-cne'Manual'-or$d.nativeCallback-ne$true-or$d.operation-cne'None'-or
            (Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length){
            throw 'P04 did not complete the actual native manual archive.'
        }
        foreach($required in @('rt-repeated-attack-requested','rt-repeated-attack-resolved','rt-before-save','rt-native-save-requested')){
            if(@($Rows|Where-Object kind -CEQ $required).Count-ne1){throw 'P04 native spent-action setup is incomplete.'}
        }
        $attacks=@($Rows|Where-Object kind -CEQ 'rt-repeated-attack-resolved')[0].detail
        if($attacks.ordinaryAttacks-lt2-or$attacks.resolved-lt2-or$attacks.forcedD20-ne0){throw 'P04 requires two naturally rolled native attacks.'}
        if($Request.persistenceCase.EndsWith('-attack',[StringComparison]::Ordinal)){
            $active=@($Rows|Where-Object kind -CEQ 'rt-active-attack-save-request')
            $deferred=@($Rows|Where-Object kind -CEQ 'rt-native-wait-started')
            if($active.Count-ne1-or$deferred.Count-ne1){throw 'P04 active save lacks its actual pre-delivery wait.'}
            $running=@($active[0].detail.nativeCommands|Where-Object {$_.actor-ceq$initial[0].rider.Id}|
                ForEach-Object {$_.raw}|Where-Object {$_.started-eq$true-and$_.acted-eq$false-and$_.finished-eq$false})
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
    if($Request.scenario -cnotin @('persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load') -or $Status-cne'PASS'){return}
    $artifact=@($Manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'){throw 'P01 has no exact observation artifact.'}
    $path=Join-Path $Request.evidenceRoot 'persistence-observations.jsonl'
    if((Get-KmcSha256 $path)-cne$artifact[0].sha256){throw 'P01 observations changed.'}
    $rows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    if($rows.Count-lt6-or$rows.Count-gt20){throw 'Persistence observation count is invalid.'}
    if($Request.scenario-cin @('persistence-p04-save','persistence-p04-load')){
        Assert-KmcRealtimePersistenceEvidence $Request $rows $GameResult
        return
    }
    if($Request.scenario-cin @('persistence-p05-save','persistence-p05-load')-and$Request.persistenceCase-ceq'alternating'){
        Assert-KmcAlternatingPersistenceEvidence $Request $rows $GameResult
        return
    }
    $isP03=$Request.scenario-cin @('persistence-p03-save','persistence-p03-load')
    $isSlot=$Request.scenario-cin @('persistence-p05-save','persistence-p05-load')
    $isCombat=$Request.scenario-cin @('persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load')
    $checkpoint=if(@($Request.PSObject.Properties.Name)-ccontains'persistenceCase'){[string]$Request.persistenceCase}else{'partial-movement'}
    $isRoundEffect=$isP03-and$checkpoint-ceq'round-effect'
    $isReaction=$isP03-and$checkpoint-ceq'reaction'
    $isCommitment=$isP03-and-not$isRoundEffect-and-not$isReaction
    $isWrite=$Request.scenario-cin @('persistence-p01-save','persistence-p02-save','persistence-p03-save','persistence-p04-save','persistence-p05-save')
    $initial=@($rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P01 has no unique initial state.'}
    foreach($row in $rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.relationship-cne'Mounted'-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.controls.DuplicateFactCount-ne0){throw 'P01 native state identity/relationship/control invariant differs.'}
        if(($isCombat-or$isSlot)-and$row.checkpoint-cne$checkpoint){throw 'P02 observation checkpoint differs from its bounded request.'}
    }
    $required=@('usable-continuation-complete')
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent')){$required+=@('movement-dispatched','movement-completed')}
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent','between-partner-orders')){$required+=@('attack-dispatched','attack-delivered')}
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
        if($checkpoint-cin @('partial-movement','rider-spent')){
            $continuation=@($rows|Where-Object kind -CEQ 'movement-completed')[0]
            if($continuation.mount.Move-le0-or$continuation.rider.Move-ne0){throw 'P02 lost transport expenditure or taxed the rider.'}
        }
        $final=@($rows|Where-Object kind -CEQ 'usable-continuation-complete')[0]
        if(@($final.detail.turnVisits).Count-lt4){throw 'P02 lacks observed native unrelated participation.'}
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    if($isWrite){
        $written=@($rows|Where-Object kind -CEQ 'native-write-complete')
        if($written.Count-ne1){throw 'P01 save has no real completion observation.'}
        $d=$written[0].detail
        if($isP03){Assert-KmcP03Snapshot $d.snapshot $checkpoint}
        elseif($isCombat){Assert-KmcP02Snapshot $d.snapshot $checkpoint}
        $type=if($isSlot-and$checkpoint-ceq'quick'){'Quick'}elseif($isSlot-and$checkpoint-ceq'auto'){'Auto'}else{'Manual'}
        $leaf=if($type-ceq'Manual'){'Manual_300_KMC_P01.zks'}else{$type+'_1.zks'}
        $archive=Join-Path $root $leaf
        if($isSlot){
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
            (Get-KmcSha256 $archive)-cne$d.sha256-or(Get-Item $archive).Length-ne$d.length-or
            $d.snapshot.Mounted-ne$true-or$d.snapshot.Rider.Id-cne$initial[0].rider.Id-or
            $d.snapshot.Mount.Id-cne$initial[0].mount.Id){throw 'P01 real archive/metadata/completion differs.'}
    }else{
        if(@($rows|Where-Object kind -CEQ 'native-write-complete').Count-ne0){throw 'P01 cold process unexpectedly wrote a save.'}
        $archive=Join-Path $root $Request.persistenceLoad.fileName
        if((Get-KmcSha256 $archive)-cne$Request.persistenceLoad.sha256){throw 'P01 cold process did not retain its selected archive.'}
    }
}
