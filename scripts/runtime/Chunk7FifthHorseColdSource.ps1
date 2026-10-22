Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7ManualDerivedFixtures.ps1')

# Read-only admission of the one genuine batch181 paid fifth-Horse Manual.
# Its b180 selector and b178 four-Horse ancestry are requalified by the
# existing Manual-derived source gate; this gate grants no new build authority.
function Get-KmcChunk7FifthHorseColdSource($Selection,$Fixture,$Candidate){
 $producer='20261022-c7b-world181-b181s1-fifthhorse-manual'
 $artifactHash='e05fdd3cfb16c344dcb821a57476cefdfb91978aa8a39aca9322e5b4fc24a47b'
 $archiveHash='afbeaa5bc749d91bd969581977f396ff4b0c2716691625a9648b2bb827c2318e'
 $outerHash='69678b673cb5b6bf6f2b1fda7617d98766a42b33797a7cc4d8d783b26aecdbeb'
 $gameHash='fa30d9c65d7bab05e8e70a1ce3b248adae5f45621c73ffee9ea131189f3f09c3'
 $run='unassigned-fifth-horse-cold'
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection})
 if(-not(Test-KmcChunk7FifthHorseColdSelection $Selection)-or$Selection.sourceRunId-cne$producer-or
  $Selection.sourceArtifactSha256-cne$artifactHash-or$Selection.sourceArchiveSha256-cne$archiveHash){throw 'Fifth-Horse cold input differs from the retained batch181 producer.'}
 $set=Get-KmcChunk7OwnedReceiptSet $producer 'chunk7-manual-derived-evidence.json';$r=$set.request
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-manual-derived-acquisition'
 Assert-KmcBootstrapInventoryString $r.runId $producer
 Assert-KmcBootstrapInventoryString $r.manualDerived.case 'paid-small-ranger-horse'
 Assert-KmcBootstrapInventoryString $r.manualDerived.nativeWriteLeaf 'Manual_307_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 Assert-KmcChunk7ManualDerivedDescriptor $r
 $ancestry=Get-KmcChunk7ManualDerivedSource $r.manualDerived $Fixture (Get-KmcChunk7RecordedCandidate $r)
 if((ConvertTo-KmcChunk7Canonical $ancestry.descriptor)-cne(ConvertTo-KmcChunk7Canonical $r.persistenceLoad)){throw 'Fifth-Horse producer substituted its qualified four-Horse Manual.'}
 Assert-KmcChunk7OwnedSourceCompletion $set $producer 'chunk7-manual-derived-acquisition' $Fixture (Get-KmcChunk7RecordedCandidate $r) $r.manualDerived.sourceArchiveSha256 1
 Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root 'runtime-result.json')] $outerHash
 Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root 'runtime-game-result.json')] $gameHash
 Assert-KmcBootstrapInventoryString $set.pins[(Join-Path $set.root 'chunk7-manual-derived-evidence.json')] $artifactHash
 Assert-KmcChunk7ManualDerivedArtifact $r $set.manifest 'PASS'
 $artifactPath=Join-Path $set.root 'chunk7-manual-derived-evidence.json';$artifact=Read-KmcJson $artifactPath
 $w=$artifact.observations.nativeWrite
 Assert-KmcBootstrapInventoryString $w.sha256 $archiveHash
 Assert-KmcBootstrapInventoryString $w.fileName 'Manual_307_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 Assert-KmcBootstrapInventoryString $w.internalName 'KMC_C7_MANUAL_DERIVED_FIELD'
 $saveRoot=Join-Path $set.profile 'Saved Games';$archive=Assert-KmcChildPath $w.path $saveRoot 'fifth-Horse native Manual'
 Assert-KmcBootstrapInventoryString $archive (Join-Path $saveRoot $w.fileName)
 $descriptor=[pscustomobject][ordered]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;
  length=[long]$w.length;lastWriteTimeUtcTicks=[long]$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcAssistedSettledFile $archive $descriptor 'fifth-Horse immutable native Manual'
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;
  assistedColdInventory=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 $pins=@(foreach($receipt in $set.pins.Keys){$file=Get-Item -LiteralPath $receipt
  [pscustomobject]@{path=$receipt;sha256=$set.pins[$receipt];length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}})
 foreach($receipt in @((Join-Path $set.root 'profile-cache-changes.json'),(Join-Path $set.root 'runtime-loader-inventory.json'))){
  Assert-KmcNotReparsePoint $receipt 'fifth-Horse producer receipt';Assert-KmcNotHardLink $receipt 'fifth-Horse producer receipt'
  $file=Get-Item -LiteralPath $receipt
  $pins+=,[pscustomobject]@{path=$receipt;sha256=(Get-KmcSha256 $receipt);length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}
 }
 if($pins.Count-ne9){throw 'Fifth-Horse producer receipt set is incomplete.'}
 foreach($pin in $pins){Assert-KmcAssistedSettledFile $pin.path $pin 'fifth-Horse producer receipt'}
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$archive;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$artifactHash;
  artifactLength=[long](Get-Item -LiteralPath $artifactPath).Length;sourceRunId=$producer;
  sourceProcessId=[int]$set.game.processId;receiptPins=$pins;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
