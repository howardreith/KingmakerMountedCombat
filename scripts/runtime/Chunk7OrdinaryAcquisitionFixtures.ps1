Set-StrictMode -Version Latest
# Read-only native inventory intake. No fixture, body, XP, money or save is fabricated.
function Read-KmcChunk7OrdinarySelectionFile([string]$Path,[string]$RunId) {
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'ordinary explicit selection'
    Assert-KmcNotReparsePoint $path 'ordinary selection';Assert-KmcNotHardLink $path 'ordinary selection'
    $file=Get-Item -LiteralPath $path
    if($file.Length-le0-or$file.Length-gt16KB){throw 'Ordinary selection file is empty or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'ordinary selection'
    $selection=Read-KmcJson $path
    Assert-KmcChunk7OrdinarySelection ([pscustomobject]@{scenario='chunk7-ordinary-acquisition';runId=$RunId;ordinaryAcquisition=$selection})
    if((Get-KmcSha256 $path)-cne$hash-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Ordinary selection changed while reading.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}
function Assert-KmcChunk7OrdinaryInventoryPins($Selection,$Artifact) {
    $v=$Artifact.observations.nativeProfileInventory
    Assert-KmcOrdinaryInteger $v.schemaVersion 1 1
    if($v.captureStatus-cne'PASS'-or$v.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact installed native Small inventory is unavailable.'}
    Assert-KmcChunk7Boolean $v.profileAdmissionChanged $false
    if($Selection.case-ceq'paid-small-ranger-horse'){
        if($v.smallRaces-isnot[Array]){throw 'Native Small catalogue is missing.'}
        $races=@($v.smallRaces|Where-Object raceGuid -CEQ $Selection.smallRaceGuid)
        if($races.Count-ne1-or$races[0].size-cne'Small'-or$races[0].presets-isnot[Array]){throw 'Exact observed native Small race is missing or duplicated.'}
        $presets=@($races[0].presets|Where-Object presetGuid -CEQ $Selection.smallPresetGuid)
        if($presets.Count-ne1-or$presets[0].raceId-cne$races[0].raceId){throw 'Exact observed native Small preset is missing or conflicts.'}
        Assert-KmcChunk7Boolean $presets[0].present $true
        $rig=if($Selection.gender-ceq'Male'){$presets[0].maleSkeleton}else{$presets[0].femaleSkeleton}
        Assert-KmcChunk7Boolean $rig.present $true
        if($rig.name-isnot[string]-or$rig.name-cne$Selection.skeletonName){throw 'Exact selected native Small skeleton is unavailable.'}
    }else{
        if($v.party-isnot[Array]){throw 'Observed native party inventory is missing.'}
        $actor=@($v.party|Where-Object id -CEQ $Selection.existingActorId)
        if($actor.Count-ne1-or$null-ne$actor[0].petId){throw 'Existing actor does not have exact observed pet-free provenance.'}
    }
}
function Get-KmcChunk7OrdinaryInventorySource($Selection,$Fixture,$Candidate) {
    Assert-KmcChunk7OrdinarySelection ([pscustomobject]@{scenario='chunk7-ordinary-acquisition';runId='unassigned-run';ordinaryAcquisition=$Selection})
    $root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$Selection.inventoryRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'ordinary inventory source run'
    Assert-KmcNotReparsePoint $root 'ordinary inventory source run'
    $files=@{}
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json')){
        $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'ordinary source receipt';Assert-KmcNotHardLink $path 'ordinary source receipt'
        Assert-KmcManifestJsonMembersUnique $path 'ordinary source receipt';$files[$leaf]=Get-KmcSha256 $path
    }
    $request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
    $game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
    if($request.runId-cne$Selection.inventoryRunId-or$request.scenario-cne'chunk7-two-pair-exploration'-or$request.evidenceRoot-cne$root-or
       $request.fixture.working.sha256-cne$Fixture.working.sha256-or$request.fixture.baseline.sha256-cne$Fixture.baseline.sha256-or
       $Fixture.working.sha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'){throw 'Ordinary source is not the exact qualified Working inventory.'}
    foreach($field in @('commit','dllSha256','dllMvid')){if($request.$field-isnot[string]-or$request.$field-cne$Candidate.$field){throw 'Ordinary native inventory came from another candidate.'}}
    foreach($receipt in @($result,$game)){
        foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){
            if($receipt.$field-isnot[string]-or$receipt.$field-cne$request.$field){throw 'Ordinary source receipts disagree on exact native run identity.'}
        }
        if($receipt.status-cne'PASS'-or$receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0-or$receipt.evidenceManifestSha256-cne$files['runtime-artifacts.json']){throw 'Ordinary source is not an immutable passing native run.'}
    }
    if($request.transactionToken-cnotmatch'^[0-9a-f]{64}$'-or$result.gameResultSha256-cne$files['runtime-game-result.json']-or
       $result.restoredSaveInventoryDigest-cnotmatch'^[0-9a-f]{64}$'){throw 'Ordinary source native/restoration hash receipts are missing.'}
    foreach($field in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$field $true}
    foreach($field in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $game.$field 0 0}
    if($isolation.runId-cne$request.runId-or$isolation.stagingMode-cne'base-game-only-kmc'){throw 'Ordinary inventory source was not the exact selected base-only native process.'}
    foreach($field in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$field $true}
    Assert-KmcReadOnlyArtifactManifest $request $files['runtime-artifacts.json'] -GameResult
    $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json')
    Assert-KmcChunk7WorldArtifact $request $manifest 'PASS'
    $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-world-evidence.json' 'chunk7-world-evidence'
    $path=Join-Path $root 'chunk7-world-evidence.json';$file=Get-Item -LiteralPath $path
    if((Get-KmcSha256 $path)-cne$Selection.inventoryArtifactSha256-or$file.Length-le0-or$file.Length-gt16MB){throw 'Ordinary caller-pinned native inventory bytes differ.'}
    Assert-KmcChunk7OrdinaryInventoryPins $Selection $artifact
    foreach($leaf in $files.Keys){if((Get-KmcSha256 (Join-Path $root $leaf))-cne$files[$leaf]){throw 'Native source receipt changed during ordinary inventory admission.'}}
    [pscustomobject]@{path=$path;sha256=$Selection.inventoryArtifactSha256;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;runId=$Selection.inventoryRunId}
}
function Copy-KmcChunk7OrdinaryInventory($Source,[string]$ProfileRoot,$Lock) {
    [void](Assert-KmcRuntimeLockOwner $Lock)
    $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'ordinary owned profile'
    Assert-KmcDirectoryTreeCloneable $run 'ordinary owned profile'
    $owner=Read-KmcJson (Join-Path $run 'owner.json')
    if($owner.scenario-cne'chunk7-ordinary-acquisition'-or$owner.transactionToken-cne$Lock.Token-or$owner.runId-cne$Lock.RunId-or
       $run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Ordinary inventory copy lacks its exact owned isolated profile.'}
    $destination=Join-Path $run 'ordinary-native-inventory.json'
    if(Test-Path -LiteralPath $destination){throw 'Ordinary inventory destination exists; immutable evidence cannot be overwritten.'}
    Assert-KmcNotReparsePoint $Source.path 'ordinary source inventory';Assert-KmcNotHardLink $Source.path 'ordinary source inventory'
    if((Get-KmcSha256 $Source.path)-cne$Source.sha256-or(Get-Item -LiteralPath $Source.path).Length-ne$Source.length){throw 'Ordinary admitted source inventory changed before copy.'}
    [IO.File]::Copy($Source.path,$destination,$false)
    if((Get-KmcSha256 $destination)-cne$Source.sha256-or(Get-KmcSha256 $Source.path)-cne$Source.sha256){throw 'Ordinary native inventory changed during guarded copy.'}
    [pscustomobject]@{path=$destination;sha256=$Source.sha256;length=[long](Get-Item -LiteralPath $destination).Length}
}
function Assert-KmcChunk7OrdinaryNativeArchive($Request,$Artifact) {
    Assert-KmcChunk7OrdinaryEvidence $Request $Artifact 'PASS'
    $w=$Artifact.observations.nativeWrite;$path=$w.path
    Assert-KmcNotReparsePoint $path 'ordinary native autosave';Assert-KmcNotHardLink $path 'ordinary native autosave'
    $file=Get-Item -LiteralPath $path
    if($file.Length-ne$w.length-or(Get-KmcSha256 $path)-cne$w.sha256){throw 'Ordinary native archive differs from the observed completed write.'}
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$zip=$null;$reader=$null
    try{
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        $headers=@($zip.Entries|Where-Object FullName -CEQ 'header.json');$states=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($headers.Count-ne1-or$headers[0].Length-le0-or$headers[0].Length-gt1MB-or$states.Count-ne1-or$states[0].Length-le0-or$states[0].Length-gt128KB){throw 'Ordinary native archive header/metadata is absent, duplicate or oversized.'}
        $reader=[IO.StreamReader]::new($headers[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'ordinary native archive header';$header=$json|ConvertFrom-Json
        if($header.Name-cne$w.name-or$header.Type-cne'Auto'-or$header.GameId-cne$Request.fixture.working.gameId-or$header.GameName-cne$Request.fixture.working.gameName-or$header.Area-cne$Request.fixture.working.area){throw 'Ordinary native autosave descriptor differs from the exact campaign/area/category.'}
        Assert-KmcOrdinaryInteger $header.CompatibilityVersion 1 1
        Assert-KmcChunk7Boolean $header.IsAutoLevelupSave $true
        $reader.Dispose();$reader=[IO.StreamReader]::new($states[0].Open(),[Text.UTF8Encoding]::new($false,$true));$metadata=$reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $metadata -Description 'ordinary native mounted metadata';$archive=$metadata|ConvertFrom-Json
        Assert-KmcOrdinaryInteger $archive.SchemaVersion 3 3
        if($archive.CampaignId-cne$Request.fixture.working.gameId-or$archive.AreaId-cne$Request.fixture.working.area-or
           $archive.Pairs-isnot[Array]-or$archive.Pairs.Count-ne0-or$archive.Participations-isnot[Array]-or$archive.Participations.Count-ne0-or$null-ne$archive.Combat){throw 'Ordinary unmounted autosave invented mounted/combat ownership or changed campaign/area.'}
    }finally{if($reader){$reader.Dispose()};if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
    if((Get-KmcSha256 $path)-cne$w.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Ordinary native archive changed during read-only validation.'}
}
