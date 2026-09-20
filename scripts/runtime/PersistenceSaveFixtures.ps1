Set-StrictMode -Version Latest

# Bounded P01 archive intake. The source must be the exact completed archive
# from another restored owned process; no gameplay state is taken from evidence.
function Get-KmcPersistenceSource {
    param([Parameter(Mandatory=$true)][string]$SourceRunId,
        [Parameter(Mandatory=$true)][string]$ExpectedSha256,
        [Parameter(Mandatory=$true)]$Fixture)
    if($SourceRunId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or $SourceRunId -in @('.','..') -or
        $ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$'){throw 'Persistence source identity is invalid.'}
    $lab=Get-KmcLabRoot
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path $lab 'runtime-staging') 'owned persistence source'
    Assert-KmcDirectoryTreeCloneable $root 'owned persistence source'
    $owner=Read-KmcJson (Join-Path $root 'owner.json')
    $result=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$SourceRunId+'/runtime-result.json'))
    if($owner.runId-cne$SourceRunId-or$owner.scenario-cnotin @('persistence-p01-save','persistence-p02-save','persistence-p03-save')-or$result.status-cne'PASS'-or
        $result.runId-cne$SourceRunId-or$result.scenario-cne$owner.scenario-or
        $result.modsRestored-ne$true-or$result.workingRestored-ne$true-or
        $owner.transactionToken-cnotmatch'^[0-9a-f]{64}$'-or$owner.transactionToken-cne$result.transactionToken){throw 'Source is not a completed restored P01 save process.'}
    $path=Join-Path $root 'Saved Games/Manual_300_KMC_P01.zks'
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
        if($header.Name-cne'KMC_P01'-or$header.Type-cne'Manual'-or$header.CompatibilityVersion-ne1-or
            $header.GameId-cne$Fixture.working.gameId-or$header.GameName-cne$Fixture.working.gameName-or
            $header.Area-cne$Fixture.working.area){throw 'Owned archive native campaign/type/name differs.'}
    }finally{if($reader){$reader.Dispose()};if($archive){$archive.Dispose()};if($stream){$stream.Dispose()}}
    if((Get-KmcSha256 $path)-cne$ExpectedSha256){throw 'Owned archive changed during inspection.'}
    return [pscustomobject]@{path=$path;descriptor=[ordered]@{
        internalName='KMC_P01';fileName=$file.Name;sha256=$ExpectedSha256;length=[long]$file.Length
        lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks
        gameId=[string]$header.GameId;gameName=[string]$header.GameName;area=[string]$header.Area
    }}
}

function Assert-KmcP02Snapshot {
    param($Snapshot,[string]$Checkpoint)
    $c=$Snapshot.Combat
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
    if($null-eq$c-or$c.Round-lt1-or$null-eq$c.Paired-or$null-eq$c.Paired.Activation-or
        $c.Paired.Activation.Sequence-lt1-or$c.Current.ActorId-cne$Snapshot.Rider.Id-or
        $Snapshot.Rider.Standard-ne0-or$Snapshot.Rider.Move-ne0){throw 'P03 snapshot lost the native rider grant/remainder.'}
    $alloc=@($c.Allocations|Where-Object ActorId -CEQ $Snapshot.Mount.Id)
    if($alloc.Count-ne1){throw 'P03 snapshot lost its mount movement commitment.'}
    $a=$alloc[0]
    $valid=switch -CaseSensitive ($Checkpoint){
        'step' { $Snapshot.Mount.Standard-eq0-and$Snapshot.Mount.Move-eq0-and$a.Movement.MetresStepped-gt0-and$a.Movement.TimeStepped-gt0 }
        'conversion' { $Snapshot.Mount.Standard-eq6-and$Snapshot.Mount.Move-gt3-and$Snapshot.Mount.Move-lt6-and$a.StandardCommitted-eq$true }
        default { $false }
    }
    if(-not$valid){throw 'P03 actual archive does not match its declared native commitment.'}
}

function Assert-KmcPersistenceScenarioEvidence {
    param($Request,$Manifest,[string]$Status,$GameResult)
    if($Request.scenario -cnotin @('persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load') -or $Status-cne'PASS'){return}
    $artifact=@($Manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'){throw 'P01 has no exact observation artifact.'}
    $path=Join-Path $Request.evidenceRoot 'persistence-observations.jsonl'
    if((Get-KmcSha256 $path)-cne$artifact[0].sha256){throw 'P01 observations changed.'}
    $rows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    if($rows.Count-lt6-or$rows.Count-gt20){throw 'Persistence observation count is invalid.'}
    $isCommitment=$Request.scenario-cin @('persistence-p03-save','persistence-p03-load')
    $isCombat=$Request.scenario-cin @('persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load')
    $checkpoint=if(@($Request.PSObject.Properties.Name)-ccontains'persistenceCase'){[string]$Request.persistenceCase}else{'partial-movement'}
    $isWrite=$Request.scenario-cin @('persistence-p01-save','persistence-p02-save','persistence-p03-save')
    $initial=@($rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P01 has no unique initial state.'}
    foreach($row in $rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.relationship-cne'Mounted'-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.controls.DuplicateFactCount-ne0){throw 'P01 native state identity/relationship/control invariant differs.'}
        if($isCombat-and$row.checkpoint-cne$checkpoint){throw 'P02 observation checkpoint differs from its bounded request.'}
    }
    $required=@('usable-continuation-complete')
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent')){$required+=@('movement-dispatched','movement-completed')}
    if(-not$isCombat-or$checkpoint-cin @('partial-movement','rider-spent','between-partner-orders')){$required+=@('attack-dispatched','attack-delivered')}
    if($isCombat-and$checkpoint-cne'partial-movement'){$required+=@('spent-work-input-before','spent-work-rejected')}
    if($isCombat-and$isWrite-and-not$isCommitment){
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
        if($checkpoint-cin @('partial-movement','rider-spent')){
            $continuation=@($rows|Where-Object kind -CEQ 'movement-completed')[0]
            if($continuation.mount.Move-le0-or$continuation.rider.Move-ne0){throw 'P02 lost transport expenditure or taxed the rider.'}
        }
        $final=@($rows|Where-Object kind -CEQ 'usable-continuation-complete')[0]
        if(@($final.detail.turnVisits).Count-lt4){throw 'P02 lacks observed native unrelated participation.'}
    }
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    if($Request.scenario-cin @('persistence-p01-save','persistence-p02-save','persistence-p03-save')){
        $written=@($rows|Where-Object kind -CEQ 'native-write-complete')
        if($written.Count-ne1){throw 'P01 save has no real completion observation.'}
        $d=$written[0].detail
        if($isCommitment){Assert-KmcP03Snapshot $d.snapshot $checkpoint}
        elseif($isCombat){Assert-KmcP02Snapshot $d.snapshot $checkpoint}
        $archive=Join-Path $root 'Manual_300_KMC_P01.zks'
        if($d.path-cne$archive-or$d.nativeType-cne'Manual'-or$d.nativeCallback-ne$true-or$d.operation-cne'None'-or
            (Get-KmcSha256 $archive)-cne$d.sha256-or(Get-Item $archive).Length-ne$d.length-or
            $d.snapshot.Mounted-ne$true-or$d.snapshot.Rider.Id-cne$initial[0].rider.Id-or
            $d.snapshot.Mount.Id-cne$initial[0].mount.Id){throw 'P01 real archive/metadata/completion differs.'}
    }else{
        if(@($rows|Where-Object kind -CEQ 'native-write-complete').Count-ne0){throw 'P01 cold process unexpectedly wrote a save.'}
        $archive=Join-Path $root $Request.persistenceLoad.fileName
        if((Get-KmcSha256 $archive)-cne$Request.persistenceLoad.sha256){throw 'P01 cold process did not retain its selected archive.'}
    }
}
