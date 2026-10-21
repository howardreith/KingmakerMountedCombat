Set-StrictMode -Version Latest

# The accepted F09 producer was built on preview.71. Requalify its immutable
# source with that producer identity, then retain the separate cold reader-only
# verdict. Neither historical identity is projected onto a later consumer.
# This is a read-only source gate; it grants no load or profile authority.
function Resolve-KmcF09AcceptedReaderPin($Pin){
 $prior=Join-Path (Get-KmcRepositoryRoot) 'scripts/runtime'
 $snapshot=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-continuation/f09-accepted-readers-6fb1eee8'
 foreach($row in @(
  @('Chunk7AssistedColdInventoryEvidence.ps1','0ecb969f676392a23bf5c9ab02bcad0ace20db7621d54af93bd8b89ca663e071',75040),
  @('Chunk7SmallFixtureColdSource.ps1','2e11048380afad6102e8a54dd4f10a31d0e5e74e360c49b5c9b587278cd4da0b',5531))){
  if($Pin.path-ceq(Join-Path $prior $row[0])){
   if($Pin.sha256-cne$row[1]-or$Pin.length-ne$row[2]){throw 'Historical F09 reader identity differs from the accepted receipt.'}
   return Assert-KmcChildPath (Join-Path $snapshot $row[0]) (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'immutable accepted F09 reader'
  }
 }
 return $Pin.path
}
function Get-KmcChunk7SmallF09CalibrationSource($Selection,$Fixture,$Consumer){
 Assert-KmcChunk7SmallF09SourceSelection $Selection 'unassigned-f09-calibration'
 $expected=[ordered]@{
  sourceRunId='20261020-c7b-world71-b53s1-small';sourceArtifactSha256='9d26bd146970fc53192bf2f28cef8c6655a7c79beb1326c075fec0a5c406e52f';
  sourceArchiveSha256='ddf2b9cf4c0c0a49c93a659d2628ae6622f02e0a400a2df287bd5713f12ed829';
  actorId='c4e2c853-4b72-4904-ad84-d0b7bfba7470';petId='0e0c7d2d-06b4-4510-9f81-d847340d1424'}
 foreach($key in $expected.Keys){Assert-KmcBootstrapInventoryString $Selection.$key $expected[$key]}
 Assert-KmcBootstrapInventoryString $Selection.coldReceiptSha256 'd188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc'
 $consumerMvid=[Guid]::Empty
 if($Consumer.commit-isnot[string]-or$Consumer.commit-cnotmatch'^[a-f0-9]{40}$'-or
    $Consumer.dllSha256-isnot[string]-or$Consumer.dllSha256-cnotmatch'^[a-f0-9]{64}$'-or
    $Consumer.dllMvid-isnot[string]-or-not[Guid]::TryParseExact($Consumer.dllMvid,'D',[ref]$consumerMvid)-or
    $consumerMvid-eq[Guid]::Empty-or$Consumer.dllMvid-cne$consumerMvid.ToString('D')){
  throw 'F09 consumer requires canonical commit, DLL SHA-256 and module MVID.'
 }
 $producer=[pscustomobject]@{commit='784a6296f9e0d91c44ad25dda826db57f609e929';dllSha256='8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89';
  dllMvid='bf98be50-898a-41b3-88f0-78959a0ea1a3';version='0.1.0-chunk7b-preview.71'}
 if($Consumer.commit-ceq$producer.commit-and$Consumer.dllSha256-ceq$producer.dllSha256-and$Consumer.dllMvid-ceq$producer.dllMvid){throw 'F09 cross-candidate source route cannot claim a same-candidate consumer.'}
 $source=Get-KmcChunk7SmallFixtureColdSource $expected.sourceRunId $expected.sourceArtifactSha256 $expected.sourceArchiveSha256 $Fixture $producer
 $lab=Get-KmcLabRoot
 $receiptPath=Assert-KmcChildPath (Join-Path $lab 'analysis-cache/chunk7-continuation/codex-takeover/group71-native-b53/b53s2-f09-reader-only-reevaluation.json') (Join-Path $lab 'analysis-cache') 'F09 separate cold reader receipt'
 $receipt=Read-KmcSmallFixturePinnedJson $receiptPath 'd188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc' 65536
 $r=$receipt.value
 Assert-KmcOrdinaryInteger $r.schemaVersion 1 1
 foreach($pair in @(@('kind','f09-small-fixture-cold-reader-only-reevaluation'),@('status','PASS'),@('originalOuterStatus','FAIL'),
   @('originalOuterError','Bootstrap inventory exact scalar identity differs.'),@('originalGameStatus','PASS'),@('originalArtifactStatus','PASS'),
   @('producerRunId',$expected.sourceRunId),@('consumerRunId','20261020-c7b-world71-b53s2-smallcold'),@('transactionPhase','restored'))){
  Assert-KmcBootstrapInventoryString $r.($pair[0]) $pair[1]
 }
 foreach($key in @('nativeRerun')){Assert-KmcChunk7Boolean $r.$key $false}
 foreach($key in @('modsRestored','profileBytesRestoredExactly')){Assert-KmcChunk7Boolean $r.$key $true}
 if($r.inputPins-isnot[Array]-or$r.inputPins.Count-ne9){throw 'F09 cold reader input pin set changed.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($pin in $r.inputPins){
  $path=Assert-KmcChildPath (Resolve-KmcF09AcceptedReaderPin $pin) $lab 'F09 cold reader input'
  if(-not$seen.Add($path)-or$pin.sha256-isnot[string]-or$pin.sha256-cnotmatch'^[a-f0-9]{64}$') {throw 'F09 cold reader input pin is missing or repeated.'}
  Assert-KmcNotReparsePoint $path 'F09 cold reader input';Assert-KmcNotHardLink $path 'F09 cold reader input'
  $file=Get-Item -LiteralPath $path
  if($file.PSIsContainer-or$file.Length-ne[long]$pin.length-or(Get-KmcSha256 $path)-cne$pin.sha256){throw 'F09 cold reader input changed after accepted re-evaluation.'}
 }
 $coldRoot=Join-Path (Join-Path $lab 'runtime-evidence') $r.consumerRunId
 $requestPath=Join-Path $coldRoot 'runtime-request.json';$artifactPath=Join-Path $coldRoot 'chunk7-assisted-cold-inventory-evidence.json'
 $requestPin=@($r.inputPins|Where-Object path -CEQ $requestPath);$artifactPin=@($r.inputPins|Where-Object path -CEQ $artifactPath)
 if($requestPin.Count-ne1-or$artifactPin.Count-ne1){throw 'F09 cold request/artifact pins are absent or ambiguous.'}
 $coldRequest=(Read-KmcSmallFixturePinnedJson $requestPath $requestPin[0].sha256 16384).value
 $coldArtifact=(Read-KmcSmallFixturePinnedJson $artifactPath $artifactPin[0].sha256 16777216).value
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $coldRequest.$key $producer.$key}
 Assert-KmcBootstrapInventoryString $coldRequest.scenario 'chunk7-assisted-cold-inventory'
 foreach($pair in @(@('runId',$expected.sourceRunId),@('artifactSha256',$expected.sourceArtifactSha256),@('archiveSha256',$expected.sourceArchiveSha256))){
  Assert-KmcBootstrapInventoryString $coldArtifact.observations.source.($pair[0]) $pair[1]
 }
 Assert-KmcBootstrapInventoryString $coldArtifact.status 'PASS'
 $party=@($coldArtifact.observations.loadedWorld.party)
 if($party.Count-ne7-or@($party|Where-Object id -CEQ $expected.actorId).Count-ne1-or@($party|Where-Object id -CEQ $expected.petId).Count-ne1){throw 'F09 separate cold roster lost its Small pair.'}
 Assert-KmcAssistedColdNoPreparation $coldArtifact.observations.noPreparation $coldRequest
 Assert-KmcAssistedSettledFile $source.path $source.descriptor 'F09 producer archive at final source boundary'
 foreach($pin in $source.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 producer receipt at final source boundary'}
 foreach($pin in $r.inputPins){
  $path=Assert-KmcChildPath (Resolve-KmcF09AcceptedReaderPin $pin) $lab 'F09 cold input at final source boundary'
  Assert-KmcNotReparsePoint $path 'F09 cold input at final source boundary';Assert-KmcNotHardLink $path 'F09 cold input at final source boundary'
  $file=Get-Item -LiteralPath $path
  if($file.PSIsContainer-or$file.Length-ne[long]$pin.length-or(Get-KmcSha256 $path)-cne$pin.sha256){throw 'F09 cold input changed before qualification returned.'}
 }
 if((Get-KmcSha256 $receiptPath)-cne$receipt.sha256){throw 'F09 accepted cold receipt changed during qualification.'}
 [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;artifactSha256=$source.artifactSha256;
  artifactLength=$source.artifactLength;sourceRunId=$source.sourceRunId;historicalProducer=$producer;coldReaderReceiptSha256=$receipt.sha256;
  producerReceiptPins=$source.receiptPins;coldInputPins=$r.inputPins;coldReaderReceiptPath=$receiptPath;
  sameCandidate=$false;nativeAuthorityGranted=$false;fixtureSetupOnly=$true}
}
function Copy-KmcChunk7SmallF09SourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 if($Source.nativeAuthorityGranted-ne$false-or$Source.sameCandidate-ne$false-or$Source.fixtureSetupOnly-ne$true){throw 'F09 source cannot grant load/profile authority or claim same-candidate acquisition.'}
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'F09 Small profile'
 Assert-KmcDirectoryTreeCloneable $run 'F09 Small profile'
 $owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-small-profile-calibration'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'F09 source copy lacks its exact locked profile.'}
 $destination=Join-Path $run 'small-owner-seed-fixture-source.json'
 if(Test-Path -LiteralPath $destination){throw 'F09 source destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'F09 producer artifact';Assert-KmcNotHardLink $Source.artifactPath 'F09 producer artifact'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'F09 source changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'F09 source changed during locked copy.'}
}
function Read-KmcChunk7SmallSelectionFile([string]$Path,[string]$RunId){
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Small explicit selection'
    Assert-KmcNotReparsePoint $path 'Small calibration selection';Assert-KmcNotHardLink $path 'Small calibration selection'
    $file=Get-Item -LiteralPath $path
    if($file.Length-le0-or$file.Length-gt16KB){throw 'Field selection is empty or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Small calibration selection';$selection=Read-KmcJson $path
    Assert-KmcChunk7SmallSelection ([pscustomobject]@{scenario='chunk7-small-profile-calibration';runId=$RunId;smallCalibration=$selection})
    if((Get-KmcSha256 $path)-cne$hash-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Field selection changed during intake.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}

function Get-KmcChunk7SmallManualSource($Selection,$Fixture,$Candidate,[switch]$Assisted){
 Assert-KmcChunk7SmallManualSourceSelection $Selection 'unassigned-small-manual'
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'qualified Manual profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified Manual profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'qualified Manual evidence';Assert-KmcNotReparsePoint $root 'qualified Manual evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-acquired-manual-evidence.json')){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified Manual receipt';Assert-KmcNotHardLink $path 'qualified Manual receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified Manual receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified Manual owner';Assert-KmcNotHardLink $ownerPath 'qualified Manual owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified Manual owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-acquired-manual-derivative';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 Assert-KmcChunk7AcquiredManualDescriptor $request
 Assert-KmcBootstrapInventoryString $request.acquiredManual.case $(if($Assisted){'preserve-two-acquired-field'}else{'preserve-acquired-field'})
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 # Complete bootstrap and cold source qualification is repeated unchanged. Decoded
 # metadata gives no load/write/restore authority; the separate runtime owns that.
 $lineage=Get-KmcChunk7AcquiredManualSource $request.acquiredManual $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $lineage.descriptor)){throw 'Manual source did not load the exact qualified bootstrap descriptor.'}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $request.acquiredManual.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Manual source owner unexpectedly selects another persistence case.'}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Small calibration requires an error-free completed acquired Manual.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $files[(Join-Path $root 'runtime-artifacts.json')]
 }
 if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual source exact restoration identity is missing.'}
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 $files[(Join-Path $root 'runtime-game-result.json')]
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 1 1}
 Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $true
 Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
 Assert-KmcReadOnlyArtifactManifest $request $files[(Join-Path $root 'runtime-artifacts.json')] -GameResult
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7AcquiredManualArtifact $request $manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-acquired-manual-evidence.json' 'chunk7-acquired-manual-evidence'
 $artifactPath=Join-Path $root 'chunk7-acquired-manual-evidence.json';$file=Get-Item -LiteralPath $artifactPath
 if($file.Length-le0-or$file.Length-gt16MB-or$files[$artifactPath]-cne$Selection.sourceArtifactSha256){throw 'Caller-pinned successful acquired Manual differs.'}
 Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
 Assert-KmcChunk7AcquiredManualOutput $request $artifact;Assert-KmcChunk7SmallManualSourceObservation $Selection $artifact.observations -Assisted:$Assisted
 $w=$artifact.observations.nativeWrite
 $descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcChunk7SmallManualDescriptor ([pscustomobject]@{scenario='chunk7-small-profile-calibration';runId='unassigned-small-manual';smallCalibration=[pscustomobject]@{manualSource=$Selection};persistenceLoad=$descriptor;fixture=$Fixture})
 foreach($path in $files.Keys){if((Get-KmcSha256 $path)-cne$files[$path]){throw 'Manual qualification receipt changed during intake.'}}
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Manual source bytes changed during qualification.'}
 $qualified=[pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
  acquiredManualRunId=$request.runId;firstAcquisitionRunId=$request.acquiredManual.sourceRunId;bootstrapRunId=$lineage.bootstrapRunId;coldInventoryRunId=$lineage.coldInventoryRunId;manualRunId=$lineage.manualRunId}
 if($Assisted){foreach($key in @('sourceMode','assistedProducerRunId','assistedColdRunId','fixtureSetupWasAssisted')){Add-Member -InputObject $qualified -MemberType NoteProperty -Name $key -Value $lineage.$key}}
 $qualified
}
function Copy-KmcChunk7SmallManualSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'Small Manual profile';Assert-KmcDirectoryTreeCloneable $run 'Small Manual profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-small-profile-calibration'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Small Manual copy lacks its exact locked profile.'}
 $destination=Join-Path $run 'small-acquired-manual-source.json';if(Test-Path -LiteralPath $destination){throw 'Small Manual source destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'Small Manual lineage';Assert-KmcNotHardLink $Source.artifactPath 'Small Manual lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Small Manual source changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Small Manual source changed during locked copy.'}
}
