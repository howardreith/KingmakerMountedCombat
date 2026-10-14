Set-StrictMode -Version Latest

# This is the new assisted-fixture family only. Its metadata shape does not
# broaden historical bootstrap, ordinary acquisition, or save restoration.
function Assert-KmcChunk7AssistedFixtureMetadata($Descriptor, $Header, $Archive) {
    if ($Header -isnot [pscustomobject] -or $Archive -isnot [pscustomobject]) {
        throw 'Assisted fixture header and mounted metadata require JSON object roots.'
    }
    foreach ($pair in @(@('Name','internalName'), @('GameId','gameId'), @('GameName','gameName'), @('Area','area'))) {
        Assert-KmcBootstrapInventoryString $Header.($pair[0]) $Descriptor.($pair[1])
    }
    Assert-KmcBootstrapInventoryString $Descriptor.internalName 'KMC_C7_ASSISTED_FIXTURE'
    Assert-KmcBootstrapInventoryString $Header.Type 'Manual'
    Assert-KmcOrdinaryInteger $Header.CompatibilityVersion 1 1
    Assert-KmcChunk7Boolean $Header.IsAutoLevelupSave $false
    Assert-KmcOrdinaryGuid $Descriptor.gameId
    if ($Descriptor.area -isnot [string] -or $Descriptor.area -cnotmatch '^[a-f0-9]{32}$' -or
        $Descriptor.gameName -isnot [string] -or [string]::IsNullOrWhiteSpace($Descriptor.gameName) -or
        $Descriptor.gameName.Length -gt 256 -or $Descriptor.gameName -match '[\x00-\x1f\x7f]') {
        throw 'Assisted fixture native campaign name or area is malformed.'
    }
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'assisted fixture mounted metadata'
    Assert-KmcOrdinaryInteger $Archive.SchemaVersion 3 3
    Assert-KmcBootstrapInventoryString $Archive.CampaignId $Descriptor.gameId
    Assert-KmcBootstrapInventoryString $Archive.AreaId $Descriptor.area
    Assert-KmcOrdinaryInteger $Archive.GameTimeTicks 0 ([long]::MaxValue)
    Assert-KmcBootstrapInventoryString $Archive.Policy 'rider-principal-distinct-native-v1'
    Assert-KmcBootstrapInventoryString $Archive.RulesId 'crpg-transport-v1'
    foreach ($key in @('Pairs','Participations','Slots')) {
        if ($Archive.$key -isnot [Array] -or $Archive.$key.Count -ne 0) {
            throw 'Assisted fixture source contains mounted, retained, or slot custody.'
        }
    }
    if ($null -ne $Archive.Combat) { throw 'Assisted fixture source contains combat custody.' }
}

function Read-KmcChunk7AssistedMetadataEntry($Zip, [string]$Name, [int]$MaximumBytes) {
    # Case aliases cannot become a second possible native reader target. Other
    # entries are never opened, decoded, copied, extracted, or rewritten.
    $entries = @($Zip.Entries | Where-Object { [string]::Equals($_.FullName, $Name, [StringComparison]::OrdinalIgnoreCase) })
    if ($entries.Count -ne 1 -or $entries[0].FullName -cne $Name -or
        $entries[0].Length -le 0 -or $entries[0].Length -gt $MaximumBytes) {
        throw ('Assisted fixture metadata member is absent, ambiguous, or oversized: ' + $Name)
    }
    $stream = $null
    $memory = $null
    try {
        $stream = $entries[0].Open()
        $memory = [IO.MemoryStream]::new()
        $buffer = [byte[]]::new(4096)
        while (($count = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            if ($memory.Length + $count -gt $MaximumBytes) { throw 'Assisted fixture decompressed metadata exceeds its bound.' }
            $memory.Write($buffer, 0, $count)
        }
        if ($memory.Length -ne $entries[0].Length) { throw 'Assisted fixture metadata length differs from its ZIP entry.' }
        $bytes = $memory.ToArray()
        $offset = 0
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191) { $offset = 3 }
        $json = [Text.UTF8Encoding]::new($false, $true).GetString($bytes, $offset, $bytes.Length - $offset)
        if ($json -cnotmatch '^\s*\{') { throw 'Assisted fixture metadata member requires one JSON object root.' }
        Assert-KmcJsonObjectMembersUnique -Json $json -Description ('assisted fixture ' + $Name)
        $json | ConvertFrom-Json
    }
    finally {
        if ($memory) { $memory.Dispose() }
        if ($stream) { $stream.Dispose() }
    }
}

function Assert-KmcChunk7AssistedFixtureArchive($Request, $Artifact) {
    # The full native envelope, closed quota, descriptor/worker identities and
    # terminal cleanup must pass before this bounded read is reachable.
    Assert-KmcChunk7AssistedFixtureOutput $Request $Artifact
    $write = $Artifact.observations.finalManual
    $file = Get-Item -LiteralPath $write.path
    if ($file.Length -le 0 -or $file.Length -gt 256MB) { throw 'Assisted fixture archive exceeds the existing native fixture bound.' }
    Assert-KmcNotReparsePoint $write.path 'assisted fixture Manual archive'
    Assert-KmcNotHardLink $write.path 'assisted fixture Manual archive'
    $stream = $null
    $zip = $null
    try {
        Add-Type -AssemblyName System.IO.Compression
        $stream = [IO.FileStream]::new($write.path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Read, $false)
        if ($zip.Entries.Count -gt 4096) { throw 'Assisted fixture archive entry inventory is unbounded.' }
        $header = Read-KmcChunk7AssistedMetadataEntry $zip 'header.json' 1MB
        $archive = Read-KmcChunk7AssistedMetadataEntry $zip 'kmc-mounted-state' 128KB
        Assert-KmcChunk7AssistedFixtureMetadata $write $header $archive
    }
    finally {
        if ($zip) { $zip.Dispose() }
        if ($stream) { $stream.Dispose() }
    }
    $after = Get-Item -LiteralPath $write.path
    if ($after.Length -ne $file.Length -or $after.LastWriteTimeUtc.Ticks -ne $file.LastWriteTimeUtc.Ticks -or
        (Get-KmcSha256 $write.path) -cne $write.sha256) {
        throw 'Assisted fixture archive changed during read-only metadata qualification.'
    }
}

# The following input/output helpers qualify immutable evidence only. They do
# not load a save, grant gameplay eligibility, copy a source or issue authority.
function Assert-KmcChunk7AssistedSourceSelection($Selection,$RunId=$null) {
    Assert-KmcAssistedObjectValue $Selection @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256') 'assisted fixture source selection'
    if($Selection.sourceRunId-isnot[string]-or$Selection.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Assisted fixture source run ID is malformed.'}
    foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){
        if($Selection.$key-isnot[string]-or$Selection.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted fixture source hash must be an exact scalar SHA256.'}
    }
    if($null-ne$RunId){
        if($RunId-isnot[string]-or$RunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$RunId-ceq$Selection.sourceRunId){throw 'Assisted fixture source cannot be the consumer run.'}
    }
}
function Read-KmcChunk7AssistedFixtureSelectionFile($Path,$RunId) {
    if($Path-isnot[string]-or[string]::IsNullOrWhiteSpace($Path)-or$RunId-isnot[string]){throw 'Assisted fixture selection path and consumer run must be explicit scalar strings.'}
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'assisted fixture source selection'
    Assert-KmcNotReparsePoint $path 'assisted fixture source selection';Assert-KmcNotHardLink $path 'assisted fixture source selection'
    $file=Get-Item -LiteralPath $path
    if($file.PSIsContainer-or$file.Length-le0-or$file.Length-gt16KB){throw 'Assisted fixture source selection is empty, not a file or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'assisted fixture source selection';$selection=Read-KmcJson $path
    Assert-KmcChunk7AssistedSourceSelection $selection $RunId
    $after=Get-Item -LiteralPath $path
    if($after.Length-ne$file.Length-or$after.LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $path)-cne$hash){throw 'Assisted fixture source selection changed during intake.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}
function Assert-KmcAssistedSettledFile($Path,$Value,[string]$Description) {
    if($Path-isnot[string]-or$Value.sha256-isnot[string]-or$Value.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted settled path/hash is malformed.'}
    Assert-KmcOrdinaryInteger $Value.length 1 268435456;Assert-KmcOrdinaryInteger $Value.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
    Assert-KmcNotReparsePoint $Path $Description;Assert-KmcNotHardLink $Path $Description
    $file=Get-Item -LiteralPath $Path
    if($file.PSIsContainer-or$file.Length-ne$Value.length-or$file.LastWriteTimeUtc.Ticks-ne$Value.lastWriteTimeUtcTicks){throw ($Description+' metadata differs from its exact receipt.')}
    $hash=Get-KmcSha256 $Path;$after=Get-Item -LiteralPath $Path
    if($hash-cne$Value.sha256-or$after.Length-ne$file.Length-or$after.LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw ($Description+' bytes changed or differ from its exact receipt.')}
}
function Assert-KmcChunk7AssistedFixtureOutput($Request,$Artifact) {
    # Standalone output verification still requires the complete native artifact.
    Assert-KmcChunk7AssistedFixtureEvidence $Request $Artifact 'PASS'
    $root=Get-KmcAssistedSaveRoot $Request;Assert-KmcDirectoryTreeCloneable $root 'assisted settled save root'
    $o=$Artifact.observations;$writes=$o.nativeWrites
    Assert-KmcAssistedArray $writes 2 3 'assisted actual native writes'
    $phases=if($writes.Count-eq3){@('initial-auto','levelup-auto','final-manual')}else{@('initial-auto','final-manual')}
    for($i=0;$i-lt$writes.Count;$i++){Assert-KmcBootstrapInventoryString $writes[$i].phase $phases[$i]}
    Assert-KmcAssistedSame $o.finalManual $writes[-1] 'assisted final Manual write'
    $manual=$o.finalManual;$auto=$writes[$writes.Count-2]
    Assert-KmcOrdinaryInteger $manual.authority.manualNumber 1 ([int]::MaxValue)
    $manualLeaf='Manual_'+$manual.authority.manualNumber.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ASSISTED_FIXTURE.zks'
    Assert-KmcBootstrapInventoryString $manual.fileName $manualLeaf
    Assert-KmcBootstrapInventoryString $manual.internalName 'KMC_C7_ASSISTED_FIXTURE';Assert-KmcBootstrapInventoryString $manual.type 'Manual'
    Assert-KmcChunk7Boolean $manual.autoLevelup $false
    Assert-KmcBootstrapInventoryString $auto.fileName 'Auto_1.zks';Assert-KmcBootstrapInventoryString $auto.type 'Auto'
    Assert-KmcChunk7Boolean $auto.autoLevelup ($writes.Count-eq3)
    foreach($write in @($manual,$auto)){
        Assert-KmcBootstrapInventoryString $write.operation 'None'
        if($write.path-isnot[string]){throw 'Assisted output path is not a scalar string.'}
        $path=Assert-KmcChildPath $write.path $root 'assisted native output'
        Assert-KmcBootstrapInventoryString $path (Join-Path $root $write.fileName)
        foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $write.$key $manual.$key}
        if($write.sha256-ceq$Request.fixture.working.sha256-or$write.sha256-ceq$Request.fixture.baseline.sha256){throw 'Assisted output aliases a protected fixture anchor.'}
        Assert-KmcAssistedSettledFile $path $write 'assisted native output'
    }
    $anchor=$Request.fixture.working
    if($anchor.fileName-isnot[string]-or[IO.Path]::GetFileName($anchor.fileName)-cne$anchor.fileName-or$anchor.fileName-ceq$manualLeaf-or$anchor.fileName-ceq'Auto_1.zks'){throw 'Assisted Working anchor leaf is invalid or aliases an output.'}
    $anchorPath=Assert-KmcChildPath (Join-Path $root $anchor.fileName) $root 'assisted immutable Working anchor'
    Assert-KmcAssistedSettledFile $anchorPath $anchor 'assisted immutable Working anchor'
}
function Get-KmcChunk7AssistedFixtureSource($Selection,$Fixture,$Candidate) {
    Assert-KmcChunk7AssistedSourceSelection $Selection
    foreach($key in @('commit','dllSha256')){$width=if($key-ceq'commit'){40}else{64};if($Candidate.$key-isnot[string]-or$Candidate.$key-cnotmatch('^[a-f0-9]{'+$width+'}$')){throw 'Assisted source candidate pin is malformed.'}}
    Assert-KmcOrdinaryGuid $Candidate.dllMvid
    $lab=Get-KmcLabRoot
    $profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'assisted source profile'
    Assert-KmcDirectoryTreeCloneable $profile 'assisted source profile'
    $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'assisted source evidence'
    Assert-KmcNotReparsePoint $root 'assisted source evidence'
    $pins=[Collections.Generic.List[object]]::new();$hashes=@{}
    $artifactLeaf='chunk7-assisted-fixture-evidence.json';$ownerPath=Join-Path $profile 'owner.json'
    $paths=@('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','profile-cache-changes.json',$artifactLeaf)|ForEach-Object{Join-Path $root $_}
    foreach($path in @($paths)+@($ownerPath)){
        Assert-KmcNotReparsePoint $path 'assisted source receipt';Assert-KmcNotHardLink $path 'assisted source receipt'
        $file=Get-Item -LiteralPath $path
        if($file.PSIsContainer-or$file.Length-le0-or$file.Length-gt16MB){throw 'Assisted source receipt is empty, not a file or oversized.'}
        $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'assisted source receipt';$hashes[$path]=$hash
        $pins.Add([pscustomobject]@{path=$path;sha256=$hash;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks})
    }
    $request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json')
    $owner=Read-KmcJson $ownerPath;$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json');$profileReceipt=Read-KmcJson (Join-Path $root 'profile-cache-changes.json')
    Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-assisted-fixture-preparation';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
    Assert-KmcChunk7AssistedFixtureSelection $request
    foreach($key in @('working','baseline')){Assert-KmcAssistedSame $request.fixture.$key $Fixture.$key ('assisted source exact '+$key+' descriptor')}
    foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
    Assert-KmcAssistedObjectValue $owner @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'assisted source owner'
    foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
    Assert-KmcBootstrapInventoryString $owner.sourceSha256 $Fixture.working.sha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
    if($null-ne$owner.persistenceCase-and($owner.persistenceCase-isnot[string]-or$owner.persistenceCase-cne'')){throw 'Assisted source owner contains a foreign persistence case.'}
    foreach($receipt in @($result,$game)){
        foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
        Assert-KmcOrdinaryInteger $receipt.schemaVersion 2 2;Assert-KmcBootstrapInventoryString $receipt.status 'PASS'
        if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Assisted source is not an error-free completed native result.'}
        foreach($key in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $receipt.$key 0 0}
        Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $hashes[(Join-Path $root 'runtime-artifacts.json')]
    }
    if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted source transaction/restoration identity is missing.'}
    Assert-KmcBootstrapInventoryString $result.gameResultSha256 $hashes[(Join-Path $root 'runtime-game-result.json')]
    foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
    foreach($key in @('loadRequestCount','workingLoadRequestCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
    Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $false
    Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
    foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
    Assert-KmcOrdinaryInteger $profileReceipt.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $profileReceipt.runId $request.runId
    Assert-KmcBootstrapInventoryString $profileReceipt.policy 'native-achievement-cache-settled-size'
    if($profileReceipt.snapshotProfileDigest-isnot[string]-or$profileReceipt.snapshotProfileDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Assisted source profile restoration digest is malformed.'}
    Assert-KmcChunk7Boolean $profileReceipt.profileBytesRestoredExactly $true
    if($profileReceipt.acceptedNativeCacheChanges-isnot[Array]-or$profileReceipt.acceptedNativeCacheChanges.Count-ne0){throw 'Assisted source profile restoration is not byte exact.'}
    $manifestHash=$hashes[(Join-Path $root 'runtime-artifacts.json')]
    Assert-KmcReadOnlyArtifactManifest $request $manifestHash -GameResult
    $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7AssistedFixtureArtifact $request $manifest 'PASS'
    $artifact=Read-KmcChunk7ManifestArtifact $request $manifest $artifactLeaf 'chunk7-assisted-fixture-evidence'
    $artifactPath=Join-Path $root $artifactLeaf
    if($hashes[$artifactPath]-cne$Selection.sourceArtifactSha256){throw 'Assisted source artifact differs from its caller pin.'}
    $writes=$artifact.observations.nativeWrites;Assert-KmcAssistedArray $writes 2 3 'assisted native source writes'
    foreach($key in @('saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key $writes.Count $writes.Count}
    $write=$artifact.observations.finalManual
    Assert-KmcBootstrapInventoryString $write.sha256 $Selection.sourceArchiveSha256
    # Exact metadata-only archive read follows complete native completion proof.
    Assert-KmcChunk7AssistedFixtureArchive $request $artifact
    foreach($pin in $pins){$file=Get-Item -LiteralPath $pin.path;if($file.Length-ne$pin.length-or$file.LastWriteTimeUtc.Ticks-ne$pin.lastWriteTimeUtcTicks-or(Get-KmcSha256 $pin.path)-cne$pin.sha256){throw 'Assisted source receipt changed during qualification.'}}
    Assert-KmcChunk7AssistedFixtureOutput $request $artifact
    $file=Get-Item -LiteralPath $artifactPath
    $descriptor=[pscustomobject]@{internalName=$write.internalName;fileName=$write.fileName;sha256=$write.sha256;length=$write.length;lastWriteTimeUtcTicks=$write.lastWriteTimeUtcTicks;gameId=$write.gameId;gameName=$write.gameName;area=$write.area}
    [pscustomobject]@{path=$write.path;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
        sourceRunId=$request.runId;requestPath=(Join-Path $root 'runtime-request.json');requestSha256=$hashes[(Join-Path $root 'runtime-request.json')];manifestSha256=$manifestHash;receiptPins=$pins.ToArray();fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
