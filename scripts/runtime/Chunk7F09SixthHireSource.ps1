Set-StrictMode -Version Latest

function Assert-KmcSixthHireReceiptPins($Source){
 Assert-KmcBootstrapInventoryString $Source.sourceRunId '20261022-c7b-world183-b183s1-linzi-departure'
 Assert-KmcBootstrapInventoryString $Source.artifactSha256 '978220bfa22f13f77f8ac827c8c2fa33abd0d9e437cc7196961217490fd61487'
 Assert-KmcBootstrapInventoryString $Source.descriptor.sha256 'afbeaa5bc749d91bd969581977f396ff4b0c2716691625a9648b2bb827c2318e'
 if($Source.receiptPins-isnot[Array]-or$Source.receiptPins.Count-ne28-or@($Source.receiptPins.path|Sort-Object -Unique).Count-ne28){throw 'Sixth-hire source requires its exact 28-receipt closure.'}
 foreach($pin in $Source.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'sixth-hire receipt closure'}
}

# Read-only provenance for the next paid sixth-Horse derivative. Batch183 made
# no save: the immutable input remains batch181 Manual_307. Its original outer
# FAIL and the two distinct accepted reader re-evaluations are never rewritten.
function Assert-KmcChunk7F09SixthHireOriginalReceipts($Set,$Fixture){
 $run='20261022-c7b-world183-b183s1-linzi-departure';$r=$Set.request
 Assert-KmcBootstrapInventoryString $r.runId $run
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-assisted-cold-inventory'
 Assert-KmcBootstrapInventoryString $r.evidenceRoot $Set.root
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $r.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($pair in @(@('commit','633f7bf1fb3da3d72fe9144fedfb17fe8dddae61'),
  @('dllSha256','2a972906060d410a233c90676417da45c21a6d18185b18acc0b6a2972944ed29'),
  @('dllMvid','1c7a6c63-ca56-45a3-adbe-91d34fe470b0'))){Assert-KmcBootstrapInventoryString $r.($pair[0]) $pair[1]}
 Assert-KmcChunk7AssistedColdInventoryDescriptor $r
 Assert-KmcBootstrapInventoryString $r.assistedColdInventory.case 'fifth-horse-native-field-departure'
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $Set.owner.$key $r.$key}
 Assert-KmcBootstrapInventoryString $Set.owner.sourceSha256 $r.persistenceLoad.sha256
 Assert-KmcBootstrapInventoryString $Set.owner.saveRoot (Join-Path $Set.profile 'Saved Games')
 foreach($receipt in @($Set.game,$Set.result)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $r.$key}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $Set.pins[(Join-Path $Set.root 'runtime-artifacts.json')]
 }
 Assert-KmcBootstrapInventoryString $Set.game.status 'PASS'
 Assert-KmcAssistedArray $Set.game.errors 0 0 'sixth-hire original native game errors'
 foreach($key in @('assertionPassCount','subscenarioPassCount','subscenarioTotal')){Assert-KmcOrdinaryInteger $Set.game.$key 5 5}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','suppressedWorkingSaveRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount')){Assert-KmcOrdinaryInteger $Set.game.$key 1 1}
 Assert-KmcChunk7Boolean $Set.game.fixtureIdentityVerified $true
 Assert-KmcBootstrapInventoryString $Set.result.status 'FAIL'
 Assert-KmcAssistedArray $Set.result.errors 1 1 'preserved native-departure original outer failure'
 Assert-KmcBootstrapInventoryString $Set.result.errors[0] 'Runtime game-result contains an unknown subscenario: C7-assisted-cold-native-departure'
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $Set.result.$key $true}
 Assert-KmcBootstrapInventoryString $Set.result.restoredSaveInventoryDigest 'df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab'
 Assert-KmcBootstrapInventoryString $Set.result.gameResultSha256 $Set.pins[(Join-Path $Set.root 'runtime-game-result.json')]
 Assert-KmcBootstrapInventoryString $Set.isolation.runId $run
 Assert-KmcBootstrapInventoryString $Set.isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed')){Assert-KmcChunk7Boolean $Set.isolation.$key $true}
 foreach($key in @('gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $Set.isolation.$key $false}
 foreach($pair in @(@('runtime-request.json','93c53cc9a6b16e76ae5005017a03f008316f88b547d95b3f82fe6cf2f4573853'),
  @('runtime-game-result.json','4b2673108fea1c1a89e5f71ec1700c01b091f332b6a3d4387f4e58f7f168c3ff'),
  @('runtime-result.json','21b7c6a1250d37cd4f2f567759cabd5c6e45e4f973ec0c93f447984f700d3985'),
  @('runtime-artifacts.json','c57ab387ce3a4e93f9cedf68c7a393d04fc68fcfd15b94d0b5332dd57a794219'),
  @('base-game-isolation-result.json','f17356a3da2c95c29c8f0792e240a428c1fb86e6f51a0a7a4e62e1822466da70'),
  @('chunk7-assisted-cold-inventory-evidence.json','978220bfa22f13f77f8ac827c8c2fa33abd0d9e437cc7196961217490fd61487'))){
  Assert-KmcBootstrapInventoryString $Set.pins[(Join-Path $Set.root $pair[0])] $pair[1]
 }
 Assert-KmcChunk7OwnedReceiptPins $Set
}

function Get-KmcChunk7F09SixthHireSource($Fixture){
 $lab=Get-KmcLabRoot;$run='20261022-c7b-world183-b183s1-linzi-departure'
 $set=Get-KmcChunk7OwnedReceiptSet $run 'chunk7-assisted-cold-inventory-evidence.json'
 Assert-KmcChunk7F09SixthHireOriginalReceipts $set $Fixture
 $source=Get-KmcChunk7FifthHorseDepartureSource $set.request.assistedColdInventory $Fixture (Get-KmcChunk7RecordedCandidate $set.request)
 Assert-KmcAssistedSame $set.request.persistenceLoad $source.descriptor 'sixth-hire unchanged cold-qualified five-Horse Manual'
 Assert-KmcAssistedColdSourcePins $source $true $false $true
 $proofRoot=Join-Path $lab 'analysis-cache/chunk7-continuation'
 $gameProof=Read-KmcSmallFixturePinnedJson (Join-Path $proofRoot 'b183-reader-only-reevaluation.json') '2899d3def39734bcc535658684bdf317ff9e609944b3e713b7ffab66edd719ea' 1048576
 $baseProof=Read-KmcSmallFixturePinnedJson (Join-Path $proofRoot 'b183-base-only-reader-reevaluation.json') 'a62f9a600d16f7fd1295b3c2cd8197844b15748f65a750267dfc156852db8b63' 1048576
 foreach($proof in @($gameProof,$baseProof)){
  Assert-KmcBootstrapInventoryString $proof.value.runId $run;Assert-KmcBootstrapInventoryString $proof.value.status 'PASS'
  foreach($key in @('commit','dllSha256')){Assert-KmcBootstrapInventoryString $proof.value.$key $set.request.$key}
  Assert-KmcChunk7Boolean $proof.value.nativeInvoked $false
 }
 Assert-KmcBootstrapInventoryString $gameProof.value.kind 'native-departure-b183-immutable-reader-only-reevaluation'
 Assert-KmcBootstrapInventoryString $gameProof.value.originalOuterStatus 'FAIL'
 Assert-KmcOrdinaryInteger $gameProof.value.assertionPassCount 5 5;Assert-KmcOrdinaryInteger $gameProof.value.assertionFailCount 0 0
 Assert-KmcBootstrapInventoryString $baseProof.value.kind 'native-departure-b183-immutable-base-only-reevaluation'
 $profile=Read-KmcSmallFixturePinnedJson (Join-Path $set.root 'profile-cache-changes.json') '949a9858d6a73470baac0eb79fb7ed0001dd0e022990663eeee5f89350b0a129'
 Assert-KmcChunk7Boolean $profile.value.profileBytesRestoredExactly $true
 Assert-KmcAssistedArray $profile.value.acceptedNativeCacheChanges 0 0 'native departure exact profile restoration'
 $admission=Read-KmcSmallFixturePinnedJson (Join-Path $set.root 'base-game-isolation-admission.json') '20edcabafa72718d19f10551c782f513a013278ebd9e0ab43a596639fba3ba5f'
 Assert-KmcBootstrapInventoryString $admission.value.runId $run
 Assert-KmcBootstrapInventoryString $admission.value.transactionToken $set.request.transactionToken
 Assert-KmcBaseGameDependencyReceipt $admission.value.dependencies -VerifyFiles
 $inventory=Read-KmcChunk7ManifestArtifact $set.request $set.manifest 'runtime-loader-inventory.json' 'runtime-loader-inventory'
 Assert-KmcBaseGameNativeInventory $set.request $inventory $admission.value.dependencies
 Assert-KmcBootstrapInventoryString (Get-KmcSha256 (Join-Path $set.root 'runtime-loader-inventory.json')) '9c692d1dd171eb4860bbca3356b8daae7e49abb27dbb925caeec3da8ca834df7'
 $artifactPath=Join-Path $set.root 'chunk7-assisted-cold-inventory-evidence.json'
 $artifact=Read-KmcChunk7ManifestArtifact $set.request $set.manifest 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence'
 $o=$artifact.observations
 Assert-KmcChunk7NativeFieldDeparture $o $o.loadedWorld $o.settledWorld $o.fifthHorseFacts
 Assert-KmcAssistedSame $o.fifthHorseFacts (Get-KmcChunk7FifthHorseColdExpected (Read-KmcJson $source.artifactPath)).owners 'sixth-hire five sourced Horse owners'
 Assert-KmcAssistedColdNoPreparation $o.noPreparation $set.request
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Assert-KmcOrdinaryInteger $o.$key 0 0}
 Assert-KmcChunk7OwnedReceiptPins $set
 foreach($proof in @($gameProof,$baseProof,$profile,$admission)){Assert-KmcAssistedSettledFile $proof.path $proof 'sixth-hire immutable qualification receipt'}
 Assert-KmcAssistedColdSourcePins $source $true $false $true
 $pins=@($source.receiptPins)
 foreach($path in $set.pins.Keys){$file=Get-Item -LiteralPath $path
  $pins+=,[pscustomobject]@{path=$path;sha256=$set.pins[$path];length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}
 }
 foreach($proof in @($gameProof,$baseProof,$profile,$admission)){
  $pins+=,[pscustomobject]@{path=$proof.path;sha256=$proof.sha256;length=$proof.length;lastWriteTimeUtcTicks=$proof.lastWriteTimeUtcTicks}
 }
 $loaderPath=Join-Path $set.root 'runtime-loader-inventory.json';$loaderFile=Get-Item -LiteralPath $loaderPath
 $pins+=,[pscustomobject]@{path=$loaderPath;sha256='9c692d1dd171eb4860bbca3356b8daae7e49abb27dbb925caeec3da8ca834df7';
  length=[long]$loaderFile.Length;lastWriteTimeUtcTicks=[long]$loaderFile.LastWriteTimeUtc.Ticks}
 if($pins.Count-ne28-or@($pins.path|Sort-Object -Unique).Count-ne28){throw 'Sixth-hire receipt closure is incomplete or duplicated.'}
 foreach($pin in $pins){Assert-KmcAssistedSettledFile $pin.path $pin 'sixth-hire receipt closure'}
 [pscustomobject][ordered]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$artifactPath;
  artifactSha256='978220bfa22f13f77f8ac827c8c2fa33abd0d9e437cc7196961217490fd61487';
  artifactLength=[long](Get-Item -LiteralPath $artifactPath).Length;sourceRunId=$run;
  sourceProcessId=[int]$set.game.processId;originalOuterStatus='FAIL';nativeGameStatus='PASS';readerOnlyStatus='PASS';
  sourceArchiveProducerRunId='20261022-c7b-world181-b181s1-fifthhorse-manual';
  nativeDepartureActorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';
  sourceFieldActorIds=@($o.loadedWorld.party.id);retainedFieldActorIds=@($o.settledWorld.party.id);
  retainedCharacterIds=@($o.settledWorld.partyCharacterIds);horseFacts=@($o.fifthHorseFacts);
  receiptPins=$pins;
  nativeRosterTransitionProven=$true;fixtureSetupOnly=$true;ordinaryAcquisitionProven=$false;nativeAuthorityGranted=$false}
}
