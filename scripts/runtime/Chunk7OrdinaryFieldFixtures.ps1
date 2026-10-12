Set-StrictMode -Version Latest
function Read-KmcChunk7FieldSelectionFile([string]$Path,[string]$RunId){
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'field explicit selection'
    Assert-KmcNotReparsePoint $path 'field selection';Assert-KmcNotHardLink $path 'field selection'
    $file=Get-Item -LiteralPath $path
    if($file.Length-le0-or$file.Length-gt16KB){throw 'Field selection is empty or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'field selection';$selection=Read-KmcJson $path
    Assert-KmcChunk7FieldSelection ([pscustomobject]@{scenario='chunk7-ordinary-field-admission';runId=$RunId;ordinaryField=$selection})
    if((Get-KmcSha256 $path)-cne$hash-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Field selection changed during intake.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}
function Get-KmcChunk7FieldSource($Selection,$Fixture,$Candidate){
    Assert-KmcChunk7FieldSelection ([pscustomobject]@{scenario='chunk7-ordinary-field-admission';runId='unassigned-field-run';ordinaryField=$Selection})
    $lab=Get-KmcLabRoot
    $profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'paid acquisition profile'
    Assert-KmcDirectoryTreeCloneable $profile 'paid acquisition profile'
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'paid acquisition evidence'
    Assert-KmcNotReparsePoint $root 'paid acquisition evidence'
    $files=@{}
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json')){
        $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'field source receipt';Assert-KmcNotHardLink $path 'field source receipt'
        Assert-KmcManifestJsonMembersUnique $path 'field source receipt';$files[$path]=Get-KmcSha256 $path
    }
    $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotHardLink $ownerPath 'source profile owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'source profile owner'
    $files[$ownerPath]=Get-KmcSha256 $ownerPath
    $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
    $game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
    if($request.runId-cne$Selection.sourceRunId-or$request.scenario-cne'chunk7-ordinary-acquisition'-or$request.evidenceRoot-cne$root-or
       $request.ordinaryAcquisition.case-cne'paid-small-ranger-horse'-or$request.fixture.working.sha256-cne$Fixture.working.sha256-or
       $request.fixture.baseline.sha256-cne$Fixture.baseline.sha256-or$Fixture.working.sha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
       $owner.runId-cne$request.runId-or$owner.scenario-cne$request.scenario-or$owner.transactionToken-cne$request.transactionToken-or
       $owner.sourceSha256-cne$Fixture.working.sha256-or$owner.saveRoot-cne(Join-Path $profile 'Saved Games')-or$null-ne$owner.persistenceCase){throw 'Field source is not the exact owned paid acquisition fixture.'}
    foreach($key in @('commit','dllSha256','dllMvid')){if($request.$key-isnot[string]-or$request.$key-cne$Candidate.$key){throw 'Field source came from another candidate.'}}
    foreach($receipt in @($result,$game)){
        foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){
            if($receipt.$key-isnot[string]-or$receipt.$key-cne$request.$key){throw 'Field source result identity differs.'}
        }
        if($receipt.status-cne'PASS'-or$receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0-or
           $receipt.evidenceManifestSha256-cne$files[(Join-Path $root 'runtime-artifacts.json')]){throw 'Field source is not an immutable PASS native result.'}
    }
    if($request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.gameResultSha256-cne$files[(Join-Path $root 'runtime-game-result.json')]-or
       $result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Field source native/restoration receipt is missing.'}
    foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
    foreach($key in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
    if($isolation.runId-cne$request.runId-or$isolation.stagingMode-cne'base-game-only-kmc'){throw 'Field source was not a selected base-only native process.'}
    foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
    Assert-KmcReadOnlyArtifactManifest $request $files[(Join-Path $root 'runtime-artifacts.json')] -GameResult
    $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7OrdinaryArtifact $request $manifest 'PASS'
    $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-ordinary-acquisition-evidence.json' 'chunk7-ordinary-acquisition-evidence'
    $artifactPath=Join-Path $root 'chunk7-ordinary-acquisition-evidence.json';$artifactFile=Get-Item -LiteralPath $artifactPath
    if((Get-KmcSha256 $artifactPath)-cne$Selection.sourceArtifactSha256-or$artifactFile.Length-le0-or$artifactFile.Length-gt16MB-or
       $artifact.observations.field.actorId-cne$Selection.actorId-or$artifact.observations.field.petId-cne$Selection.petId){throw 'Caller-pinned paid acquisition actor/pet/artifact differs.'}
    $write=$artifact.observations.nativeWrite;$path=Join-Path $profile 'Saved Games/Auto_1.zks'
    if($write.path-cne$path-or$write.sha256-cne$Selection.sourceArchiveSha256-or$write.sha256-ceq$Fixture.baseline.sha256-or$write.sha256-ceq$Fixture.working.sha256){throw 'Field source archive escaped its exact owned Auto_1.'}
    Assert-KmcChunk7Boolean $write.saveBeforeFieldSelection $true
    # Only now read the two bounded metadata members of this expressly qualified
    # disposable archive. This never inspects arbitrary or protected save paths.
    Assert-KmcChunk7OrdinaryNativeArchive $request $artifact
    $file=Get-Item -LiteralPath $path
    foreach($p in $files.Keys){if((Get-KmcSha256 $p)-cne$files[$p]){throw 'Field source receipt changed while reading.'}}
    if((Get-KmcSha256 $artifactPath)-cne$Selection.sourceArtifactSha256){throw 'Field source artifact changed during intake.'}
    [pscustomobject]@{path=$path;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$artifactFile.Length;
        descriptor=[ordered]@{internalName=$write.name;fileName='Auto_1.zks';sha256=$Selection.sourceArchiveSha256;length=[long]$file.Length;
            lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$Fixture.working.gameId;gameName=$Fixture.working.gameName;area=$Fixture.working.area}}
}
function Copy-KmcChunk7FieldSourceArtifact($Source,[string]$ProfileRoot,$Lock){
    [void](Assert-KmcRuntimeLockOwner $Lock)
    $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'field owned profile'
    Assert-KmcDirectoryTreeCloneable $run 'field owned profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
    if($owner.scenario-cne'chunk7-ordinary-field-admission'-or$owner.runId-cne$Lock.RunId-or$owner.transactionToken-cne$Lock.Token-or
       $owner.sourceSha256-cne$Source.descriptor.sha256-or$run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Field source copy lacks its exact locked isolated profile.'}
    $destination=Join-Path $run 'ordinary-acquisition-source.json'
    if(Test-Path -LiteralPath $destination){throw 'Field source destination already exists.'}
    Assert-KmcNotReparsePoint $Source.artifactPath 'field source artifact';Assert-KmcNotHardLink $Source.artifactPath 'field source artifact'
    if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Field source changed before locked copy.'}
    [IO.File]::Copy($Source.artifactPath,$destination,$false)
    if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Field source changed during locked copy.'}
}
