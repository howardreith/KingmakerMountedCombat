Set-StrictMode -Version Latest

# Read-only admission of the genuine batch171 eight-actor Manual. The historical
# batch53 seven-actor Manual remains its ancestry, never the cold input itself.
function Get-KmcChunk7ThirdHorseColdSource($Selection,$Fixture,$Candidate){
 $run='unassigned-third-horse-cold';if($Selection.sourceRunId-ceq$run){$run='unassigned-third-horse-cold-2'}
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection})
 if(-not(Test-KmcChunk7ThirdHorseColdSelection $Selection)){throw 'Third-Horse cold source requires its distinct case.'}
 $set=Get-KmcChunk7OwnedReceiptSet $Selection.sourceRunId 'chunk7-world-evidence.json'
 $r=$set.request
 Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-owned-two-pair-campaign'
 Assert-KmcBootstrapInventoryString $r.ownedPairs.case 'small-fixture-third-horse-persistence-save'
 Assert-KmcBootstrapInventoryString $r.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7OwnedPairsDescriptor $r
 $ancestry=Get-KmcChunk7OwnedPairsSmallSource $r.ownedPairs $r.persistenceCase $Fixture (Get-KmcChunk7RecordedCandidate $r)
 if((ConvertTo-KmcChunk7Canonical $ancestry.descriptor)-cne(ConvertTo-KmcChunk7Canonical $r.persistenceLoad)){
  throw 'Third-Horse Manual substituted its qualified seven-actor input.'
 }
 Assert-KmcChunk7OwnedSourceCompletion $set $Selection.sourceRunId 'chunk7-owned-two-pair-campaign' $Fixture (Get-KmcChunk7RecordedCandidate $r) $r.ownedPairs.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $set.owner.persistenceCase 'multi-exploration-manual'
 Assert-KmcChunk7WorldArtifact $r $set.manifest 'PASS'
 $path=Join-Path $set.root 'chunk7-world-evidence.json'
 if($set.pins[$path]-cne$Selection.sourceArtifactSha256){throw 'Third-Horse world evidence differs from its exact cold pin.'}
 $artifact=Read-KmcJson $path;$o=$artifact.observations;$w=$o.nativeWrite
 Assert-KmcBootstrapInventoryString $w.sha256 $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString $w.fileName 'Manual_305_KMC_C7_SMALL_WORLD.zks'
 Assert-KmcBootstrapInventoryString $w.nativeName 'KMC_C7_SMALL_WORLD'
 $saveRoot=Join-Path $set.profile 'Saved Games'
 $archive=Assert-KmcChildPath $w.path $saveRoot 'third-Horse native Manual'
 Assert-KmcBootstrapInventoryString $archive (Join-Path $saveRoot $w.fileName)
 $descriptor=[pscustomobject][ordered]@{internalName=$w.nativeName;fileName=$w.fileName;sha256=$w.sha256;
  length=[long]$w.length;lastWriteTimeUtcTicks=[long]$w.lastWriteTimeUtcTicks;
  gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcAssistedSettledFile $archive $descriptor 'third-Horse immutable native Manual'
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;
  assistedColdInventory=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 $pins=@(foreach($receipt in $set.pins.Keys){$file=Get-Item -LiteralPath $receipt
  [pscustomobject]@{path=$receipt;sha256=$set.pins[$receipt];length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}})
 foreach($leaf in @('profile-cache-changes.json','runtime-loader-inventory.json')){
  $receipt=Join-Path $set.root $leaf
  Assert-KmcNotReparsePoint $receipt 'third-Horse producer receipt';Assert-KmcNotHardLink $receipt 'third-Horse producer receipt'
  $file=Get-Item -LiteralPath $receipt
  $pins+=,[pscustomobject]@{path=$receipt;sha256=(Get-KmcSha256 $receipt);length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}
 }
 if($pins.Count-ne9){throw 'Third-Horse producer receipt set is incomplete.'}
 foreach($pin in $pins){Assert-KmcAssistedSettledFile $pin.path $pin 'third-Horse producer receipt'}
 Assert-KmcChunk7OwnedReceiptPins $set
 [pscustomobject]@{path=$archive;descriptor=$descriptor;artifactPath=$path;artifactSha256=$Selection.sourceArtifactSha256;
  artifactLength=[long](Get-Item -LiteralPath $path).Length;sourceRunId=$Selection.sourceRunId;
  sourceProcessId=[int]$set.game.processId;receiptPins=$pins;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
