Set-StrictMode -Version Latest
function Read-KmcChunk7ProgressionSelectionFile([string]$Path,[string]$RunId){
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'explicit progression selection'
    Assert-KmcNotReparsePoint $path 'progression selection';Assert-KmcNotHardLink $path 'progression selection'
    $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt16KB){throw 'Progression selection is empty or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'progression selection';$selection=Read-KmcJson $path
    Assert-KmcChunk7ProgressionSelection ([pscustomobject]@{scenario='chunk7-native-progression';runId=$RunId;progression=$selection})
    if((Get-KmcSha256 $path)-cne$hash-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Progression selection changed during intake.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}
function Get-KmcChunk7ProgressionSource($Selection,$Fixture,$Candidate){
    Assert-KmcChunk7ProgressionSelection ([pscustomobject]@{scenario='chunk7-native-progression';runId='unassigned-progression-run';progression=$Selection})
    $sourceRoot=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'progression source receipts'
    $hashes=@{}
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-persistence-evidence.json')){
        $p=Join-Path $sourceRoot $leaf;Assert-KmcNotReparsePoint $p 'progression source receipt';Assert-KmcNotHardLink $p 'progression source receipt'
        Assert-KmcManifestJsonMembersUnique $p 'progression source receipt';$hashes[$leaf]=Get-KmcSha256 $p
    }
    if($hashes['chunk7-persistence-evidence.json']-cne$Selection.sourceArtifactSha256){throw 'Progression source artifact differs from explicit immutable pin.'}
    # Existing strict intake verifies a completed native, restored, base-only,
    # same-candidate Manual300 process before any bounded archive metadata read.
    $source=Get-KmcChunk7PersistenceSource -SourceRunId $Selection.sourceRunId -ExpectedSha256 $Selection.sourceArchiveSha256 -Fixture $Fixture -Case 'multi-exploration-manual' -ExpectedDllSha256 $Candidate.dllSha256 -ExpectedCommit $Candidate.commit
    $request=Read-KmcJson (Join-Path $sourceRoot 'runtime-request.json')
    if($request.dllMvid-isnot[string]-or$request.dllMvid-cne$Candidate.dllMvid-or$request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'){throw 'Progression source candidate module/transaction differs.'}
    Assert-KmcReadOnlyArtifactManifest $request $hashes['runtime-artifacts.json'] -GameResult
    $artifactPath=Join-Path $sourceRoot 'chunk7-persistence-evidence.json';$artifact=Read-KmcJson $artifactPath;$file=Get-Item -LiteralPath $artifactPath
    if($file.Length-le0-or$file.Length-gt16MB){throw 'Progression source artifact is oversized.'}
    $archive=$artifact.observations.nativeWrite.archive;Assert-KmcChunk7PersistenceArchive $archive $request
    $target=@($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.actorId});$control=@($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.controlActorId})
    if($target.Count-ne1-or$control.Count-ne1-or$target[0].Mount.Id-cne$Selection.petId-or$control[0].Mount.Id-cne$Selection.controlPetId-or
       $target[0].ProfileId-cne'medium-humanoid-horse-v1'-or$control[0].ProfileId-cne'medium-humanoid-mammoth-v1'){throw 'Progression source lacks its exact two settled owner roles.'}
    # The original world inventory captured this actor before controlled Horse
    # fixture acquisition. It is provenance, not a claim of current XP or money.
    $inventorySelection=[pscustomobject]@{case='existing-ranger-horse';inventoryRunId=$Selection.inventoryRunId;inventoryArtifactSha256=$Selection.inventoryArtifactSha256;
        existingActorId=$Selection.actorId;minimumRangerLevel=4;expectedNativeWriteCount=1;nativeWriteLeaf='Auto_1.zks';smallRaceGuid=$null;smallPresetGuid=$null;skeletonName=$null;gender=$null;leftHanded=$null}
    $inventory=Get-KmcChunk7OrdinaryInventorySource $inventorySelection $Fixture $Candidate
    $inventoryArtifact=Read-KmcJson $inventory.path;$actor=@($inventoryArtifact.observations.nativeProfileInventory.party|Where-Object id -CEQ $Selection.actorId)[0]
    if($Selection.case-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')){
        Assert-KmcProgressionTexts $actor @('raceGuid','originalSize','effectiveSize','dollPresetGuid','dollGender');Assert-KmcProgressionText $actor.character.skeleton.name
        if($actor.raceGuid-cne$Selection.raceGuid-or$actor.originalSize-cne'Medium'-or$actor.effectiveSize-cne'Medium'-or
           $actor.dollPresetGuid-cne$Selection.presetGuid-or$actor.dollGender-cne$Selection.gender-or$actor.character.skeleton.name-cne$Selection.skeletonName){throw 'Retrain body selection lacks exact original native inventory provenance.'}
        Assert-KmcChunk7Boolean $actor.dollLeftHanded $Selection.leftHanded;Assert-KmcChunk7Boolean $actor.character.skeleton.present $true
    }
    foreach($leaf in $hashes.Keys){if((Get-KmcSha256 (Join-Path $sourceRoot $leaf))-cne$hashes[$leaf]){throw 'Progression source receipt changed during admission.'}}
    if((Get-KmcSha256 $inventory.path)-cne$Selection.inventoryArtifactSha256){throw 'Progression inventory changed during admission.'}
    [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
        inventory=$inventory;sourceProcessId=$artifact.observations.processId}
}
function Copy-KmcChunk7ProgressionInputs($Source,[string]$ProfileRoot,$Lock){
    [void](Assert-KmcRuntimeLockOwner $Lock)
    $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'progression owned profile';Assert-KmcDirectoryTreeCloneable $run 'progression owned profile'
    $owner=Read-KmcJson (Join-Path $run 'owner.json')
    Assert-KmcProgressionTexts $owner @('scenario','runId','transactionToken','sourceSha256')
    if($owner.scenario-cne'chunk7-native-progression'-or$owner.runId-cne$Lock.RunId-or$owner.transactionToken-cne$Lock.Token-or$owner.sourceSha256-cne$Source.descriptor.sha256-or
       $run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Progression input copy lacks its exact locked source profile.'}
    $inputs=@([pscustomobject]@{path=$Source.artifactPath;sha256=$Source.artifactSha256;length=$Source.artifactLength;leaf='progression-source.json'},
        [pscustomobject]@{path=$Source.inventory.path;sha256=$Source.inventory.sha256;length=$Source.inventory.length;leaf='progression-inventory.json'})
    foreach($item in $inputs){
        $destination=Join-Path $run $item.leaf;if(Test-Path -LiteralPath $destination){throw 'Progression immutable input destination already exists.'}
        Assert-KmcNotReparsePoint $item.path 'progression source input';Assert-KmcNotHardLink $item.path 'progression source input'
        if((Get-KmcSha256 $item.path)-cne$item.sha256-or(Get-Item -LiteralPath $item.path).Length-ne$item.length){throw 'Progression input changed before locked copy.'}
        [IO.File]::Copy($item.path,$destination,$false)
        if((Get-KmcSha256 $destination)-cne$item.sha256-or(Get-KmcSha256 $item.path)-cne$item.sha256){throw 'Progression input changed during locked copy.'}
    }
}
function Assert-KmcChunk7ProgressionNativeHeader($Header,$Write){
    Assert-KmcProgressionTexts $Header @('Name','Type','GameId','GameName','Area')
    if($Header.Name-cne$Write.name-or$Header.Type-cne'Auto'-or$Header.GameId-cne$Write.gameId-or$Header.GameName-cne$Write.gameName-or$Header.Area-cne$Write.area){throw 'Native progression autosave header differs.'}
    Assert-KmcOrdinaryInteger $Header.CompatibilityVersion 1 1;Assert-KmcChunk7Boolean $Header.IsAutoLevelupSave $true
}
function Assert-KmcChunk7ProgressionNativeArchive($Request,$Artifact){
    Assert-KmcChunk7ProgressionEvidence $Request $Artifact 'PASS'
    if($Request.progression.case-ceq'horse-native-dismiss'){return}
    $w=$Artifact.observations.session.nativeWrite;$path=$w.path
    Assert-KmcNotReparsePoint $path 'progression native autosave';Assert-KmcNotHardLink $path 'progression native autosave'
    $file=Get-Item -LiteralPath $path
    if($file.Length-ne$w.length-or(Get-KmcSha256 $path)-cne$w.sha256){throw 'Native progression archive differs from completed observation.'}
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$zip=$null;$reader=$null
    try{
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        $header=@($zip.Entries|Where-Object FullName -CEQ 'header.json');$mounted=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($header.Count-ne1-or$mounted.Count-ne1-or$header[0].Length-le0-or$header[0].Length-gt1MB-or$mounted[0].Length-le0-or$mounted[0].Length-gt128KB){throw 'Bounded native progression metadata members missing, duplicated or oversized.'}
        $reader=[IO.StreamReader]::new($header[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd();$reader.Dispose();$reader=$null
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'progression native header';$h=$json|ConvertFrom-Json
        Assert-KmcChunk7ProgressionNativeHeader $h $w
        $reader=[IO.StreamReader]::new($mounted[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'progression native mounted metadata';$saved=$json|ConvertFrom-Json
        if((ConvertTo-KmcChunk7Canonical $saved)-cne(ConvertTo-KmcChunk7Canonical $w.archive)){throw 'Native progression mounted metadata differs from actual observer capture.'}
    }finally{if($reader){$reader.Dispose()};if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
    if((Get-KmcSha256 $path)-cne$w.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Native progression autosave changed during verification.'}
}
