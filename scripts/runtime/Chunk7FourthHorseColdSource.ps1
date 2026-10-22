Set-StrictMode -Version Latest

# Batch178's native producer passed, but its original outer result remains FAIL:
# the old external reader rejected the new Manual leaf. Admit only that exact
# retained archive plus the separately pinned, reader-only correction receipt.
function Get-KmcChunk7FourthHorseColdSource($Selection,$Fixture,$Candidate){
 $producer='20261022-c7b-world178-b178s1-fourthhorse-manual'
 $worldHash='453c3c590a444807f9fba42e9e0eea6e44d8db42b6e3194437c71c8c9547ea02'
 $archiveHash='e82f7bb0775535ac2d9b782c6e53fa92a0893ab71bdf375a1e7cc06d50188759'
 $outerHash='6f47a0044e813cd42cd29b08040b7de1cb6b72517dd3dd5c163222ac7291c88d'
 $gameHash='1260427c02e4c51e7495dc3c53c665311ee2b17950492800ac0ef45acaaaba1a'
 $reevaluationHash='c9bc4ed9acc977112d540fb5c6c71acb5bcc6e668a8ba31d086c4d2f6ae5768f'
 $run='unassigned-fourth-horse-cold';if($Selection.sourceRunId-ceq$run){$run='unassigned-fourth-horse-cold-2'}
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection})
 if(-not(Test-KmcChunk7FourthHorseSourceSelection $Selection)-or$Selection.sourceRunId-cne$producer-or
  $Selection.sourceArtifactSha256-cne$worldHash-or$Selection.sourceArchiveSha256-cne$archiveHash){throw 'Fourth-Horse cold input is not the exact retained batch178 source.'}
 $set=Get-KmcChunk7OwnedReceiptSet $producer 'chunk7-world-evidence.json';$r=$set.request
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $r.runId $producer
 Assert-KmcBootstrapInventoryString $r.ownedPairs.case 'third-horse-fixture-fourth-horse-persistence-save'
 Assert-KmcBootstrapInventoryString $r.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $r
 $ancestry=Get-KmcChunk7ThirdHorseColdSource ([pscustomobject]@{case='third-horse-manual-readonly-inventory';sourceRunId=$r.ownedPairs.sourceRunId;
  sourceArtifactSha256=$r.ownedPairs.sourceArtifactSha256;sourceArchiveSha256=$r.ownedPairs.sourceArchiveSha256;
  baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}) $Fixture (Get-KmcChunk7RecordedCandidate $r)
 if((ConvertTo-KmcChunk7Canonical $ancestry.descriptor)-cne(ConvertTo-KmcChunk7Canonical $r.persistenceLoad)){throw 'Fourth-Horse producer substituted its three-Horse source.'}
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $r.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $set.owner.$key $r.$key}
 Assert-KmcBootstrapInventoryString $set.owner.sourceSha256 $r.ownedPairs.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.saveRoot (Join-Path $set.profile 'Saved Games')
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'multi-exploration-manual'
 foreach($receipt in @($set.result,$set.game)){foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $r.$key}}
 Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root 'runtime-result.json')] $outerHash
 Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root 'runtime-game-result.json')] $gameHash
 Assert-KmcBootstrapInventoryString $set.result.status 'FAIL'
 Assert-KmcAssistedSame $set.result.errors @('Inherited Manual source numbering is not canonical.') 'batch178 original outer reader failure'
 Assert-KmcOrdinaryInteger $set.result.subscenarioFailCount 1 1
 Assert-KmcOrdinaryInteger $set.result.assertionFailCount 1 1
 Assert-KmcBootstrapInventoryString $set.result.gameResultSha256 $gameHash
 Assert-KmcBootstrapInventoryString $set.game.status 'PASS'
 if($set.game.errors-isnot[Array]-or$set.game.errors.Count-ne0){throw 'Fourth-Horse native producer reported a game error.'}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $set.game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $set.game.$key 1 1}
 Assert-KmcChunk7Boolean $set.game.fixtureIdentityVerified $true
 if($r.transactionToken-isnot[string]-or$r.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$set.result.restoredSaveInventoryDigest-isnot[string]-or$set.result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Fourth-Horse producer lacks transaction/restoration identity.'}
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $set.result.$key $true}
 Assert-KmcBootstrapInventoryString $set.isolation.runId $producer
 Assert-KmcBootstrapInventoryString $set.isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed')){Assert-KmcChunk7Boolean $set.isolation.$key $true}
 foreach($key in @('gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $set.isolation.$key $false}
 foreach($receipt in @($set.result,$set.game)){Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $set.pins[(Join-Path $set.root 'runtime-artifacts.json')]}
 Assert-KmcReadOnlyArtifactManifest $r $set.game.evidenceManifestSha256 -GameResult
 Assert-KmcChunk7WorldArtifact $r $set.manifest 'PASS'
 $readerPath=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-continuation/b178-reader-only-reevaluation.json'
 Assert-KmcNotReparsePoint $readerPath 'fourth-Horse reader-only receipt';Assert-KmcNotHardLink $readerPath 'fourth-Horse reader-only receipt'
 Assert-KmcBootstrapInventoryString (Get-KmcSha256 $readerPath) $reevaluationHash
 Assert-KmcManifestJsonMembersUnique $readerPath 'fourth-Horse reader-only receipt';$reader=Read-KmcJson $readerPath
 foreach($pair in @(@('kind','fourth-horse-b178-immutable-reader-only-reevaluation'),@('status','PASS'),@('originalOuterStatus','FAIL'),
  @('runId',$producer),@('commit',$r.commit),@('dllSha256',$r.dllSha256),@('originalOuterSha256',$outerHash),
  @('gameResultSha256',$gameHash),@('worldArtifactSha256',$worldHash),@('manifestSha256',$set.game.evidenceManifestSha256),
  @('outputArchiveSha256',$archiveHash),@('outputLeaf','Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'))){Assert-KmcBootstrapInventoryString $reader.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $reader.nativeInvoked $false
 Assert-KmcBootstrapInventoryString $reader.requestSha256 $set.pins[(Join-Path $set.root 'runtime-request.json')]
 $path=Join-Path $set.root 'chunk7-world-evidence.json'
 Assert-KmcBootstrapInventoryString $set.pins[$path] $worldHash
 $artifact=Read-KmcJson $path;$w=$artifact.observations.nativeWrite
 Assert-KmcBootstrapInventoryString $w.sha256 $archiveHash
 Assert-KmcBootstrapInventoryString $w.fileName 'Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'
 Assert-KmcBootstrapInventoryString $w.nativeName 'KMC_C7_FOUR_HORSE_WORLD'
 $saveRoot=Join-Path $set.profile 'Saved Games';$archive=Assert-KmcChildPath $w.path $saveRoot 'fourth-Horse native Manual'
 Assert-KmcBootstrapInventoryString $archive (Join-Path $saveRoot $w.fileName)
 $descriptor=[pscustomobject][ordered]@{internalName=$w.nativeName;fileName=$w.fileName;sha256=$w.sha256;
  length=[long]$w.length;lastWriteTimeUtcTicks=[long]$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcAssistedSettledFile $archive $descriptor 'fourth-Horse immutable native Manual'
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;
  assistedColdInventory=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 $pins=@(foreach($receipt in $set.pins.Keys){$file=Get-Item -LiteralPath $receipt
  [pscustomobject]@{path=$receipt;sha256=$set.pins[$receipt];length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}})
 foreach($receipt in @((Join-Path $set.root 'profile-cache-changes.json'),(Join-Path $set.root 'runtime-loader-inventory.json'),$readerPath)){
  Assert-KmcNotReparsePoint $receipt 'fourth-Horse producer receipt';Assert-KmcNotHardLink $receipt 'fourth-Horse producer receipt'
  $file=Get-Item -LiteralPath $receipt;$pins+=,[pscustomobject]@{path=$receipt;sha256=(Get-KmcSha256 $receipt);length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}
 }
 if($pins.Count-ne10){throw 'Fourth-Horse producer and reader-only receipt set is incomplete.'}
 foreach($pin in $pins){Assert-KmcAssistedSettledFile $pin.path $pin 'fourth-Horse producer receipt'}
 Assert-KmcChunk7OwnedReceiptPins $set
 $result=[pscustomobject]@{path=$archive;descriptor=$descriptor;artifactPath=$path;artifactSha256=$worldHash;
  artifactLength=[long](Get-Item -LiteralPath $path).Length;sourceRunId=$producer;
  sourceProcessId=[int]$set.game.processId;receiptPins=$pins;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 if(Test-KmcChunk7FourthHorseSelectorSelection $Selection){
  # The optionality experiment starts from the nine-actor source whose separate
  # b179 original game/outer cold PASS has already been admitted. The older
  # fourth-Horse cold case keeps its own unchanged producer-only gate.
  $nine=Get-KmcChunk7F09NineActorSource $Fixture
  Assert-KmcAssistedSame $nine.path $result.path 'selector source accepted nine-actor Manual'
  Assert-KmcAssistedSame $nine.descriptor $result.descriptor 'selector source accepted native descriptor'
  Assert-KmcChunk7Boolean $nine.nativeAuthorityGranted $false
 }
 $result
}
