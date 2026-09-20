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
    if($owner.runId-cne$SourceRunId-or$owner.scenario-cne'persistence-p01-save'-or$result.status-cne'PASS'-or
        $result.runId-cne$SourceRunId-or$result.scenario-cne'persistence-p01-save'-or
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

function Assert-KmcPersistenceScenarioEvidence {
    param($Request,$Manifest,[string]$Status,$GameResult)
    if($Request.scenario -cnotin @('persistence-p01-save','persistence-p01-load') -or $Status-cne'PASS'){return}
    $artifact=@($Manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'){throw 'P01 has no exact observation artifact.'}
    $path=Join-Path $Request.evidenceRoot 'persistence-observations.jsonl'
    if((Get-KmcSha256 $path)-cne$artifact[0].sha256){throw 'P01 observations changed.'}
    $rows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    if($rows.Count-lt6-or$rows.Count-gt20){throw 'P01 observation count is invalid.'}
    $initial=@($rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P01 has no unique initial state.'}
    foreach($row in $rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or$row.relationship-cne'Mounted'-or
            $row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.controls.DuplicateFactCount-ne0){throw 'P01 native state identity/relationship/control invariant differs.'}
    }
    foreach($kind in @('movement-dispatched','movement-completed','attack-dispatched','attack-delivered','usable-continuation-complete')){
        if(@($rows|Where-Object kind -CEQ $kind).Count-ne1){throw "P01 is missing native outcome: $kind"}
    }
    $attack=@($rows|Where-Object kind -CEQ 'attack-delivered')[0]
    if($attack.detail.rules-lt1-or$attack.detail.rolls-lt1){throw 'P01 has no actual ordinary attack outcome.'}
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    if($Request.scenario-ceq'persistence-p01-save'){
        $written=@($rows|Where-Object kind -CEQ 'native-write-complete')
        if($written.Count-ne1){throw 'P01 save has no real completion observation.'}
        $d=$written[0].detail
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
