Set-StrictMode -Version Latest
# Source selection is read-only and separate from historical P01/P05 intake.
function Get-KmcChunk7PersistenceSource {
    param([Parameter(Mandatory=$true)][string]$SourceRunId,[Parameter(Mandatory=$true)][string]$ExpectedSha256,
        [Parameter(Mandatory=$true)]$Fixture,[Parameter(Mandatory=$true)][string]$Case,
        [Parameter(Mandatory=$true)][string]$ExpectedDllSha256,[Parameter(Mandatory=$true)][string]$ExpectedCommit,$AreaTarget=$null)
    if($SourceRunId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'-or$SourceRunId-cin@('.','..')-or$ExpectedSha256-cnotmatch'^[0-9a-f]{64}$'-or
       $ExpectedDllSha256-cnotmatch'^[0-9a-f]{64}$'-or$ExpectedCommit-cnotmatch'^[0-9a-f]{40}$'-or$Case-cnotin(Get-KmcChunk7PersistenceCases)){
        throw 'Chunk 7 persistence source identity is invalid.'
    }
    $Case=Get-KmcChunk7PersistenceSourceCase $Case
    $lab=Get-KmcLabRoot
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path $lab 'runtime-staging') 'Chunk 7 source profile'
    Assert-KmcDirectoryTreeCloneable $root 'Chunk 7 source profile'
    $evidence=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$SourceRunId)) (Join-Path $lab 'runtime-evidence') 'Chunk 7 source evidence'
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json')) {
        $file=Join-Path $evidence $leaf;Assert-KmcNotReparsePoint $file 'Chunk 7 source receipt';Assert-KmcNotHardLink $file 'Chunk 7 source receipt'
        Assert-KmcManifestJsonMembersUnique $file 'Chunk 7 source receipt'
    }
    $owner=Read-KmcJson (Join-Path $root 'owner.json');$request=Read-KmcJson (Join-Path $evidence 'runtime-request.json')
    $result=Read-KmcJson (Join-Path $evidence 'runtime-result.json');$isolation=Read-KmcJson (Join-Path $evidence 'base-game-isolation-result.json')
    foreach($field in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')) {
        Assert-KmcChunk7Boolean $result.$field $true
    }
    if($owner.runId-cne$SourceRunId-or$owner.scenario-cne'chunk7-persistence-save'-or$owner.persistenceCase-cne$Case-or
       $owner.transactionToken-cnotmatch'^[0-9a-f]{64}$'-or$owner.transactionToken-cne$result.transactionToken-or
       $result.status-cne'PASS'-or$result.runId-cne$SourceRunId-or$result.scenario-cne$owner.scenario-or$result.modsRestored-ne$true-or$result.workingRestored-ne$true-or
       $result.baselineImmutable-ne$true-or$result.saveProtectionPassed-ne$true-or$result.saveWriteAllowlistPassed-ne$true-or
       $result.errors-isnot[Array]-or$result.errors.Count-ne0-or$result.restoredSaveInventoryDigest-cnotmatch'^[0-9a-f]{64}$'-or
       $request.runId-cne$SourceRunId-or$request.scenario-cne$owner.scenario-or$request.persistenceCase-cne$Case-or
       $request.dllSha256-cne$ExpectedDllSha256-or$request.commit-cne$ExpectedCommit-or$request.evidenceRoot-cne$evidence-or
       $request.fixture.working.sha256-cne$Fixture.working.sha256-or$request.fixture.baseline.sha256-cne$Fixture.baseline.sha256){
        throw 'Chunk 7 source is not this exact case/candidate/fixture from a completed restored native process.'
    }
    if($Case-ceq'multi-area-autosave'){
        Assert-KmcChunk7AreaTarget $request
        if($null-eq$AreaTarget-or(ConvertTo-KmcChunk7Canonical $AreaTarget)-cne(ConvertTo-KmcChunk7Canonical $request.persistenceAreaTarget)){throw 'Chunk 7 area source does not match the caller-selected exact destination.'}
    }elseif($null-ne$AreaTarget){throw 'A non-area Chunk 7 source cannot declare a destination.'}
    $expectedArea=Get-KmcChunk7ExpectedArchiveArea $request
    if($isolation.runId-cne$SourceRunId-or$isolation.stagingMode-cne'base-game-only-kmc'){throw 'Chunk 7 source lacks its exact base-only receipt.'}
    foreach($field in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')) {
        Assert-KmcChunk7Boolean $isolation.$field $true
    }
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if($result.$field-cne$request.$field){throw 'Source orchestration and request candidate identities differ.'}
    }
    if($result.evidenceManifestSha256-cnotmatch'^[0-9a-f]{64}$'-or$result.gameResultSha256-cnotmatch'^[0-9a-f]{64}$'-or
       (Get-KmcSha256 (Join-Path $evidence 'runtime-artifacts.json'))-cne$result.evidenceManifestSha256-or
       (Get-KmcSha256 (Join-Path $evidence 'runtime-game-result.json'))-cne$result.gameResultSha256){throw 'Source immutable native result or manifest receipt differs.'}
    $game=Read-KmcJson (Join-Path $evidence 'runtime-game-result.json')
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','evidenceManifestSha256')) {
        if($game.$field-cne$result.$field){throw 'Source native and orchestration receipt identities differ.'}
    }
    Assert-KmcChunk7NativeCounters $game @('assertionFailCount','subscenarioFailCount')
    if($game.status-cne'PASS'-or$game.assertionFailCount-ne0-or$game.subscenarioFailCount-ne0){throw 'Source native result did not pass its selected assertions.'}
    if($Case-ceq'multi-owned-worker-cancel-manual'){Assert-KmcChunk7WorkerSourceCounts $game}
    $manifest=Read-KmcJson (Join-Path $evidence 'runtime-artifacts.json')
    Assert-KmcChunk7PersistenceArtifact $request $manifest 'PASS'
    $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
    $write=$artifact.observations.nativeWrite
    $leaf=Get-KmcChunk7PersistenceLeaf $Case;$type=Get-KmcChunk7PersistenceType $Case
    $path=Assert-KmcChildPath (Join-Path $root ('Saved Games/'+$leaf)) (Join-Path $root 'Saved Games') 'Chunk 7 source archive'
    Assert-KmcNotReparsePoint $path 'Chunk 7 source archive';Assert-KmcNotHardLink $path 'Chunk 7 source archive'
    $file=Get-Item -LiteralPath $path
    if($write.path-cne$path-or$write.sha256-cne$ExpectedSha256-or(Get-KmcSha256 $path)-cne$ExpectedSha256-or
       $file.Length-ne$write.length-or$file.Length-le0-or$file.Length-gt256MB-or$ExpectedSha256-ceq$Fixture.baseline.sha256){throw 'Native source archive identity differs.'}
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$zip=$null;$reader=$null
    try {
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        $headers=@($zip.Entries|Where-Object FullName -CEQ 'header.json');$metadata=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($headers.Count-ne1-or$headers[0].Length-le0-or$headers[0].Length-gt1MB-or$metadata.Count-ne1-or$metadata[0].Length-le0-or$metadata[0].Length-gt128KB){throw 'Native archive members are missing, duplicated or oversized.'}
        $reader=[IO.StreamReader]::new($headers[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'Chunk 7 native header';$header=$json|ConvertFrom-Json
        Assert-KmcChunk7NativeCounters $header @('CompatibilityVersion')
        $nameOk=if($type-ceq'Manual'){$header.Name-ceq'KMC_C7_WORLD'}else{$header.Name-is[string]-and$header.Name.Length-gt0-and$header.Name.Length-le256-and$header.Name-cnotmatch'[\x00-\x1f\x7f]'}
        if(-not$nameOk-or$header.Type-cne$type-or$header.CompatibilityVersion-ne1-or$header.GameId-cne$Fixture.working.gameId-or
           $header.GameName-cne$Fixture.working.gameName-or$header.Area-cne$expectedArea){throw 'Native source header differs from declared campaign/area/category.'}
    } finally {if($reader){$reader.Dispose()};if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
    if((Get-KmcSha256 $path)-cne$ExpectedSha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Native source changed during read-only inspection.'}
    return [pscustomobject]@{path=$path;descriptor=[ordered]@{internalName=[string]$header.Name;fileName=$file.Name;sha256=$ExpectedSha256;length=[long]$file.Length;
        lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=[string]$header.GameId;gameName=[string]$header.GameName;area=[string]$header.Area}}
}
function Assert-KmcChunk7PersistenceColdSource($SourceRunId,$Request,$ColdArtifact) {
    if($Request.persistenceCase-ceq'legacy-schema1-resave-manual'){Assert-KmcChunk7Schema1ColdSource $SourceRunId $Request $ColdArtifact;return}
    $sourceCase=Get-KmcChunk7PersistenceSourceCase $Request.persistenceCase
    $path=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$SourceRunId+'/chunk7-persistence-evidence.json')
    Assert-KmcNotReparsePoint $path 'Chunk 7 source observation';Assert-KmcNotHardLink $path 'Chunk 7 source observation'
    $source=Read-KmcJson $path
    if($source.runId-cne$SourceRunId-or$source.scenario-cne'chunk7-persistence-save'-or$source.status-cne'PASS'-or
       $source.dllSha256-cne$Request.dllSha256-or$source.commit-cne$Request.commit-or$source.observations.checkpoint-cne$sourceCase-or
       $source.observations.nativeWrite.sha256-cne$Request.persistenceLoad.sha256-or$source.observations.nativeWrite.length-ne$Request.persistenceLoad.length-or
       $source.observations.processId-eq$ColdArtifact.observations.processId-or
       (ConvertTo-KmcChunk7Canonical $source.observations.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $ColdArtifact.observations.loadedArchive)){
        throw 'Chunk 7 cold process did not restore the exact completed native source graph on the same candidate.'
    }
    if($Request.persistenceCase-ceq'multi-area-autosave'){
        Assert-KmcChunk7AreaTarget $Request
        $priorArea=$source.observations.areaTransfer
        if($priorArea.enterPoint-cne$Request.persistenceAreaTarget.enterPoint-or$priorArea.destinationArea-cne$Request.persistenceAreaTarget.area-or
           $priorArea.mode-cne$Request.persistenceAreaTarget.autoSaveMode){throw 'Cold area source replaced the exact admitted native transition.'}
    }
    if(Test-KmcChunk7CombatPersistenceCase $Request.persistenceCase){
        $prior=$source.observations.combatContinuation;$current=$ColdArtifact.observations.combatContinuation
        if($prior.targetId-cne$current.targetId-or$prior.targetGroupId-cne$current.targetGroupId-or
           $current.targetGroupId-cne('KMC.RuntimeHostile.'+$SourceRunId+':two-pair-target')-or
           (ConvertTo-KmcChunk7Canonical $prior.actorIds)-cne(ConvertTo-KmcChunk7Canonical $current.actorIds)){
            throw 'Cold combat substituted source-owned actors or its exact native diagnostic target/group.'
        }
    }
}

. (Join-Path $PSScriptRoot 'Chunk7Schema1PersistenceFixtures.ps1')

function Assert-KmcChunk7WorkerSourceCounts($Game){
 foreach($pair in @(@('saveRequestCount',2),@('workingSaveRequestCount',2),@('loadRequestCount',1),@('workingLoadRequestCount',1),@('unauthorizedSaveRequestCount',0),@('unauthorizedLoadRequestCount',0),@('suppressedWorkingSaveRequestCount',0))){Assert-KmcChunk7NativeCounters $Game @($pair[0]);if($Game.($pair[0])-ne$pair[1]){throw 'Worker source must retain its exact seed/replacement native request totals.'}}
}
