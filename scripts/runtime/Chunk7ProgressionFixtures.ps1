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
    if(Test-KmcChunk7ProgressionColdSelection $Selection){return Get-KmcChunk7ProgressionColdSource $Selection $Fixture $Candidate}
    if(Test-KmcChunk7SmallProgressionCase $Selection.case){return Get-KmcChunk7SmallProgressionSource $Selection $Fixture $Candidate}
    if(Test-KmcChunk7OwnedProgressionCase $Selection.case){return Get-KmcChunk7OwnedProgressionSource $Selection $Fixture $Candidate}
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
function Get-KmcChunk7ProgressionColdSource($Selection,$Fixture,$Candidate){
    if($Selection.case-ceq'small-horse-native-level-cold'){return Get-KmcChunk7LevelColdSource $Selection $Fixture $Candidate}
    $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-progression-evidence.json'
    Assert-KmcChunk7OwnedSourceCompletion $set $Selection.sourceRunId 'chunk7-native-progression' $Fixture $Candidate $set.request.progression.sourceArchiveSha256 1
    $base=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
    $root=Assert-KmcChildPath (Join-Path $base $Selection.sourceRunId) $base 'retrain producer evidence'
    $hashes=@{}
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-progression-evidence.json')){
        $path=Assert-KmcChildPath (Join-Path $root $leaf) $base 'retrain producer receipt'
        Assert-KmcNotReparsePoint $path 'retrain producer receipt';Assert-KmcNotHardLink $path 'retrain producer receipt'
        Assert-KmcManifestJsonMembersUnique $path 'retrain producer receipt';$hashes[$leaf]=Get-KmcSha256 $path
    }
    if($hashes['chunk7-progression-evidence.json']-cne$Selection.sourceArtifactSha256){throw 'Cold retrain producer artifact differs from exact pin.'}
    $request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
    $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');$artifact=Read-KmcJson (Join-Path $root 'chunk7-progression-evidence.json')
    if($request.scenario-cne'chunk7-native-progression'-or$request.runId-cne$Selection.sourceRunId-or$request.progression.case-cne'small-horse-native-retrain-horse'-or
       $request.commit-cne$Candidate.commit-or$request.dllSha256-cne$Candidate.dllSha256-or$request.dllMvid-cne$Candidate.dllMvid-or
       $result.status-cne'PASS'-or$result.modsRestored-ne$true-or$result.saveProtectionPassed-ne$true-or$result.workingRestored-ne$true-or
       $artifact.status-cne'PASS'-or$artifact.observations.processId-le0-or$artifact.observations.selection.actorId-cne$Selection.actorId-or
       $artifact.observations.selection.petId-cne$Selection.formerPetId-or$artifact.observations.selection.controlActorId-cne$Selection.controlActorId-or
       $artifact.observations.selection.controlPetId-cne$Selection.controlPetId-or$artifact.observations.selection.inventoryRunId-cne$Selection.inventoryRunId-or
       $request.progression.inventoryArtifactSha256-cne$Selection.inventoryArtifactSha256){throw 'Cold retrain source is not the exact same-candidate completed Small producer.'}
    Assert-KmcChunk7ProgressionLoadDescriptor $request
    Assert-KmcReadOnlyArtifactManifest $request $hashes['runtime-artifacts.json'] -GameResult
    Assert-KmcChunk7ProgressionNativeArchive $request $artifact
    $write=$artifact.observations.session.nativeWrite;$archive=$write.archive;$roster=$artifact.observations.ownedRosterAfter
    if($write.sha256-cne$Selection.sourceArchiveSha256-or$write.type-cne'Manual'-or$archive.SchemaVersion-ne3-or$archive.Pairs.Count-ne1-or
       @($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.controlActorId-and$_.Mount.Id-ceq$Selection.controlPetId-and$_.ProfileId-ceq'medium-humanoid-horse-v1'}).Count-ne1-or
       @($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.actorId-or$_.Mount.Id-ceq$Selection.petId-or$_.Mount.Id-ceq$Selection.formerPetId}).Count-ne0-or
       $roster.currentPetId-cne$Selection.petId-or$roster.originalPet.id-cne$Selection.formerPetId-or$roster.originalPet.destroyed-ne$true-or
       $roster.originalPet.presentInNativeState-ne$false-or$roster.nativeTargetTransitionObserved-ne$true){throw 'Cold retrain source lost its exact surviving control, retired old pet or new target pet.'}
    $inventoryPath=Assert-KmcChildPath (Join-Path (Join-Path $base $Selection.inventoryRunId) 'chunk7-persistence-evidence.json') $base 'retrain mounted cold inventory'
    Assert-KmcNotReparsePoint $inventoryPath 'retrain mounted cold inventory';Assert-KmcNotHardLink $inventoryPath 'retrain mounted cold inventory'
    $inventory=Get-Item -LiteralPath $inventoryPath
    if($inventory.Length-le0-or$inventory.Length-gt16MB-or(Get-KmcSha256 $inventoryPath)-cne$Selection.inventoryArtifactSha256){throw 'Retrain mounted cold inventory differs from its immutable source.'}
    $path=Assert-KmcChildPath $write.path (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'native retrain Manual'
    Assert-KmcNotReparsePoint $path 'native retrain Manual';Assert-KmcNotHardLink $path 'native retrain Manual'
    $file=Get-Item -LiteralPath $path
    if($file.Length-ne$write.length-or(Get-KmcSha256 $path)-cne$write.sha256){throw 'Native retrain Manual bytes differ from accepted producer.'}
    $descriptor=[pscustomobject]@{internalName=$write.name;fileName=$write.leaf;sha256=$write.sha256;length=[long]$file.Length;
        lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$write.gameId;gameName=$write.gameName;area=$write.area}
    Assert-KmcChunk7ProgressionLoadDescriptor ([pscustomobject]@{scenario='chunk7-native-progression';runId='unassigned-progression-cold';progression=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
    foreach($leaf in $hashes.Keys){if((Get-KmcSha256 (Join-Path $root $leaf))-cne$hashes[$leaf]){throw 'Retrain producer receipt changed during cold source admission.'}}
    Assert-KmcChunk7OwnedReceiptPins $set
    [pscustomobject]@{path=$path;descriptor=$descriptor;artifactPath=(Join-Path $root 'chunk7-progression-evidence.json');artifactSha256=$Selection.sourceArtifactSha256;
        artifactLength=[long](Get-Item -LiteralPath (Join-Path $root 'chunk7-progression-evidence.json')).Length;
        inventory=[pscustomobject]@{path=$inventoryPath;sha256=$Selection.inventoryArtifactSha256;length=[long]$inventory.Length};sourceProcessId=$artifact.observations.processId}
}
function Get-KmcChunk7LevelColdSource($Selection,$Fixture,$Candidate){
    $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-progression-evidence.json'
    Assert-KmcChunk7OwnedSourceCompletion $set $Selection.sourceRunId 'chunk7-native-progression' $Fixture $Candidate $set.request.progression.sourceArchiveSha256 1
    $base=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
    $root=Assert-KmcChildPath (Join-Path $base $Selection.sourceRunId) $base 'Small level producer evidence'
    $hashes=@{}
    foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-progression-evidence.json')){
        $path=Assert-KmcChildPath (Join-Path $root $leaf) $base 'Small level producer receipt'
        Assert-KmcNotReparsePoint $path 'Small level producer receipt';Assert-KmcNotHardLink $path 'Small level producer receipt'
        Assert-KmcManifestJsonMembersUnique $path 'Small level producer receipt';$hashes[$leaf]=Get-KmcSha256 $path
    }
    if($hashes['chunk7-progression-evidence.json']-cne$Selection.sourceArtifactSha256){throw 'Cold level producer artifact differs from exact pin.'}
    $request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
    $artifact=Read-KmcJson (Join-Path $root 'chunk7-progression-evidence.json')
    if($request.scenario-cne'chunk7-native-progression'-or$request.runId-cne$Selection.sourceRunId-or$request.progression.case-cne'small-horse-native-level'-or
       $request.commit-cne$Candidate.commit-or$request.dllSha256-cne$Candidate.dllSha256-or$request.dllMvid-cne$Candidate.dllMvid-or
       $result.status-cne'PASS'-or$result.modsRestored-ne$true-or$result.saveProtectionPassed-ne$true-or$result.workingRestored-ne$true-or
       $artifact.status-cne'PASS'-or$artifact.observations.processId-le0-or$artifact.observations.selection.actorId-cne$Selection.actorId-or
       $artifact.observations.selection.petId-cne$Selection.petId-or$artifact.observations.selection.controlActorId-cne$Selection.controlActorId-or
       $artifact.observations.selection.controlPetId-cne$Selection.controlPetId-or$artifact.observations.selection.inventoryRunId-cne$Selection.inventoryRunId-or
       $request.progression.inventoryArtifactSha256-cne$Selection.inventoryArtifactSha256){throw 'Cold level source is not the exact same-candidate completed Small producer.'}
    Assert-KmcChunk7ProgressionLoadDescriptor $request
    Assert-KmcReadOnlyArtifactManifest $request $hashes['runtime-artifacts.json'] -GameResult
    Assert-KmcChunk7ProgressionNativeArchive $request $artifact
    $write=$artifact.observations.session.nativeWrite;$archive=$write.archive
    if($write.sha256-cne$Selection.sourceArchiveSha256-or$write.type-cne'Manual'-or$archive.SchemaVersion-ne3-or$archive.Pairs.Count-ne2-or
       @($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.actorId-and$_.Mount.Id-ceq$Selection.petId-and$_.ProfileId-ceq'small-default-halfling-horse-v1'}).Count-ne1-or
       @($archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.controlActorId-and$_.Mount.Id-ceq$Selection.controlPetId-and$_.ProfileId-ceq'medium-humanoid-horse-v1'}).Count-ne1){
        throw 'Cold level source lost its exact two settled mounted owners.'
    }
    $inventoryPath=Assert-KmcChildPath (Join-Path (Join-Path $base $Selection.inventoryRunId) 'chunk7-persistence-evidence.json') $base 'Small level mounted cold inventory'
    Assert-KmcNotReparsePoint $inventoryPath 'Small level mounted cold inventory';Assert-KmcNotHardLink $inventoryPath 'Small level mounted cold inventory'
    $inventory=Get-Item -LiteralPath $inventoryPath
    if($inventory.Length-le0-or$inventory.Length-gt16MB-or(Get-KmcSha256 $inventoryPath)-cne$Selection.inventoryArtifactSha256){throw 'Small level mounted inventory differs from its immutable source.'}
    $path=Assert-KmcChildPath $write.path (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'native Small level Manual'
    Assert-KmcNotReparsePoint $path 'native Small level Manual';Assert-KmcNotHardLink $path 'native Small level Manual'
    $file=Get-Item -LiteralPath $path
    if($file.Length-ne$write.length-or(Get-KmcSha256 $path)-cne$write.sha256){throw 'Native Small level Manual bytes differ from accepted producer.'}
    $descriptor=[pscustomobject]@{internalName=$write.name;fileName=$write.leaf;sha256=$write.sha256;length=[long]$file.Length;
        lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$write.gameId;gameName=$write.gameName;area=$write.area}
    Assert-KmcChunk7ProgressionLoadDescriptor ([pscustomobject]@{scenario='chunk7-native-progression';runId='unassigned-level-cold';progression=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
    foreach($leaf in $hashes.Keys){if((Get-KmcSha256 (Join-Path $root $leaf))-cne$hashes[$leaf]){throw 'Small level producer receipt changed during cold source admission.'}}
    Assert-KmcChunk7OwnedReceiptPins $set
    [pscustomobject]@{path=$path;descriptor=$descriptor;artifactPath=(Join-Path $root 'chunk7-progression-evidence.json');artifactSha256=$Selection.sourceArtifactSha256;
        artifactLength=[long](Get-Item -LiteralPath (Join-Path $root 'chunk7-progression-evidence.json')).Length;
        inventory=[pscustomobject]@{path=$inventoryPath;sha256=$Selection.inventoryArtifactSha256;length=[long]$inventory.Length};sourceProcessId=$artifact.observations.processId}
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
    if($Header.Name-cne$Write.name-or$Header.Type-cne$Write.type-or$Header.GameId-cne$Write.gameId-or$Header.GameName-cne$Write.gameName-or$Header.Area-cne$Write.area){throw 'Native progression save header differs.'}
    Assert-KmcOrdinaryInteger $Header.CompatibilityVersion 1 1
    Assert-KmcChunk7Boolean $Header.IsAutoLevelupSave ($Write.type-ceq'Auto')
}
function Assert-KmcChunk7ProgressionNativeArchive($Request,$Artifact){
    Assert-KmcChunk7ProgressionEvidence $Request $Artifact 'PASS'
    if(Test-KmcChunk7ProgressionColdSelection $Request.progression){return}
    if((Get-KmcChunk7ProgressionOperation $Request.progression.case)-ceq'horse-native-dismiss'){return}
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

function Get-KmcChunk7OwnedProgressionSource($Selection,$Fixture,$Candidate){
    # The existing qualifier owns complete same-candidate acquired Manual and
    # mounted checkpoint ancestry, restoration and native-output validation.
    # This caller selects no native action, writer target, actor, grant or restore.
    $source=Get-KmcChunk7OwnedPairsSource $Selection.ownedSource 'multi-exploration-manual' $Fixture $Candidate
    $request=[pscustomobject]@{scenario='chunk7-native-progression';runId='unassigned-owned-progression';progression=$Selection;persistenceLoad=$source.descriptor;fixture=$Fixture}
    Assert-KmcChunk7ProgressionLoadDescriptor $request
    $files=@([pscustomobject]@{path=$source.checkpointArtifactPath;hash=$Selection.sourceArtifactSha256;length=$source.checkpointArtifactLength;maximum=16MB},[pscustomobject]@{path=$source.artifactPath;hash=$Selection.inventoryArtifactSha256;length=$source.artifactLength;maximum=8MB})
    foreach($item in $files){
        Assert-KmcNotReparsePoint $item.path 'owned progression source';Assert-KmcNotHardLink $item.path 'owned progression source'
        $file=Get-Item -LiteralPath $item.path
        if($file.Length-le0-or$file.Length-gt$item.maximum-or$file.Length-ne$item.length-or(Get-KmcSha256 $item.path)-cne$item.hash){throw 'Qualified owned progression source changed before projection.'}
    }
    $artifact=Read-KmcJson $source.checkpointArtifactPath;$inventory=Read-KmcJson $source.artifactPath
    foreach($row in @($artifact,$inventory)){
        Assert-KmcBootstrapInventoryString $row.status 'PASS'
        foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $row.$key $Candidate.$key}
    }
    Assert-KmcBootstrapInventoryString $artifact.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $artifact.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $artifact.evidenceKind 'chunk7-persistence-evidence'
    Assert-KmcBootstrapInventoryString $artifact.observations.checkpoint 'multi-exploration-manual'
    Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
    Assert-KmcBootstrapInventoryString $inventory.runId $Selection.inventoryRunId;Assert-KmcBootstrapInventoryString $inventory.scenario 'chunk7-acquired-manual-derivative';Assert-KmcBootstrapInventoryString $inventory.evidenceKind 'chunk7-acquired-manual-evidence'
    Assert-KmcBootstrapInventoryString $inventory.observations.selection.case 'preserve-two-acquired-field'
    Assert-KmcBootstrapInventoryString $inventory.observations.nativeWrite.sha256 $Selection.ownedSource.sourceArchiveSha256
    $archive=$artifact.observations.nativeWrite.archive;Assert-KmcChunk7PersistenceArchive $archive (Get-KmcChunk7ProgressionOwnedRequest $request)
    $roles=@(@($Selection.actorId,$Selection.petId),@($Selection.controlActorId,$Selection.controlPetId))
    foreach($role in $roles){
        $pair=@($archive.Pairs|Where-Object{$_.Rider.Id-ceq$role[0]-and$_.Mount.Id-ceq$role[1]})
        if($pair.Count-ne1){throw 'Owned progression source omits an exact declared pair.'};Assert-KmcBootstrapInventoryString $pair[0].ProfileId 'medium-humanoid-horse-v1'
        $actor=@($inventory.observations.nativeProfileInventory.party|Where-Object{$_.id-ceq$role[0]})
        if($actor.Count-ne1){throw 'Owned progression source omits exact native actor inventory.'}
        foreach($key in @('originalSize','effectiveSize')){Assert-KmcBootstrapInventoryString $actor[0].$key 'Medium'}
        Assert-KmcOrdinaryInteger $actor[0].nativeLevelUpReadiness.characterLevel 1 20;Assert-KmcOrdinaryInteger $actor[0].nativeLevelUpReadiness.experience 0 ([int]::MaxValue)
    }
    Assert-KmcChunk7ProgressionIdSet @($inventory.observations.fieldWorld.party|ForEach-Object{$_.id}) $Selection.ownedSource.partyIds 'owned progression acquired field actor scope'
    Assert-KmcOrdinaryGuid $inventory.observations.fieldWorld.mainActorId
    if($inventory.observations.fieldWorld.mainActorId-ceq$Selection.actorId){throw 'Owned progression cannot use the native main character.'}
    $actor=@($inventory.observations.nativeProfileInventory.party|Where-Object{$_.id-ceq$Selection.actorId})[0]
    if((Get-KmcChunk7ProgressionOperation $Selection.case)-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')){
        foreach($pair in @(@('raceGuid','raceGuid'),@('dollPresetGuid','presetGuid'),@('dollGender','gender'))){Assert-KmcBootstrapInventoryString $actor.($pair[0]) $Selection.($pair[1])}
        Assert-KmcBootstrapInventoryString $actor.character.skeleton.name $Selection.skeletonName
        Assert-KmcChunk7Boolean $actor.dollLeftHanded $Selection.leftHanded;Assert-KmcChunk7Boolean $actor.character.skeleton.present $true
    }
    foreach($item in $files){if((Get-KmcSha256 $item.path)-cne$item.hash-or(Get-Item -LiteralPath $item.path).Length-ne$item.length){throw 'Owned progression source changed during exact read-only projection.'}}
    [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.checkpointArtifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$source.checkpointArtifactLength;
        inventory=[pscustomobject]@{path=$source.artifactPath;sha256=$Selection.inventoryArtifactSha256;length=[long]$source.artifactLength};sourceProcessId=$artifact.observations.processId}
}
function Get-KmcChunk7SmallProgressionSource($Selection,$Fixture,$Candidate){
    $source=Get-KmcChunk7OwnedPairsSource $Selection.ownedSource 'multi-exploration-manual' $Fixture $Candidate
    $request=[pscustomobject]@{scenario='chunk7-native-progression';runId='unassigned-small-progression';progression=$Selection;persistenceLoad=$source.descriptor;fixture=$Fixture}
    Assert-KmcChunk7ProgressionLoadDescriptor $request
    $set=Get-KmcChunk7OwnedReceiptSet $Selection.inventoryRunId 'chunk7-persistence-evidence.json'
    Assert-KmcChunk7OwnedSourceCompletion $set $Selection.inventoryRunId 'chunk7-owned-two-pair-campaign' $Fixture $Candidate $Selection.sourceArchiveSha256 0
    Assert-KmcBootstrapInventoryString $set.request.ownedPairs.case 'small-fixture-persistence-load'
    Assert-KmcBootstrapInventoryString $set.request.persistenceCase 'multi-exploration-manual'
    Assert-KmcChunk7OwnedPairsDescriptor $set.request
    if((ConvertTo-KmcChunk7Canonical $set.request.ownedPairs)-cne(ConvertTo-KmcChunk7Canonical $Selection.ownedSource)-or
       (ConvertTo-KmcChunk7Canonical $set.request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $source.descriptor)){
        throw 'Small progression cold consumer changed its mounted producer or qualified fixture lineage.'
    }
    Assert-KmcChunk7PersistenceArtifact $set.request $set.manifest 'PASS'
    $producer=Read-KmcJson $source.checkpointArtifactPath
    $coldPath=Join-Path $set.root 'chunk7-persistence-evidence.json';$cold=Read-KmcJson $coldPath
    Assert-KmcChunk7SmallMountedColdSource $set.request $producer $cold
    $files=@([pscustomobject]@{path=$source.checkpointArtifactPath;hash=$Selection.sourceArtifactSha256;length=$source.checkpointArtifactLength;maximum=16MB},
        [pscustomobject]@{path=$coldPath;hash=$Selection.inventoryArtifactSha256;length=(Get-Item -LiteralPath $coldPath).Length;maximum=16MB})
    foreach($item in $files){
        Assert-KmcNotReparsePoint $item.path 'Small progression source';Assert-KmcNotHardLink $item.path 'Small progression source'
        $file=Get-Item -LiteralPath $item.path
        if($file.Length-le0-or$file.Length-gt$item.maximum-or$file.Length-ne$item.length-or(Get-KmcSha256 $item.path)-cne$item.hash){throw 'Small progression producer/cold consumer differs from immutable pins.'}
    }
    Assert-KmcChunk7OwnedReceiptPins $set
    [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.checkpointArtifactPath;
        artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$source.checkpointArtifactLength;
        inventory=[pscustomobject]@{path=$coldPath;sha256=$Selection.inventoryArtifactSha256;length=[long]$files[1].length};
        sourceProcessId=$producer.observations.processId}
}
function Get-KmcChunk7ProgressionInputLeaf($Request){
    Assert-KmcChunk7ProgressionLoadDescriptor $Request
    if(Test-KmcChunk7ProgressionColdSelection $Request.progression){return $Request.persistenceLoad.fileName}
    if(Test-KmcChunk7OwnedProgressionCase $Request.progression.case){return $Request.persistenceLoad.fileName}
    return 'Manual_300_KMC_C7_WORLD.zks'
}
