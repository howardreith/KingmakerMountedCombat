Set-StrictMode -Version Latest

# Exact immutable producer admission. Original outer FAIL and original isolation
# flags remain FAIL/false; the two accepted reader-only proofs stay separate.
function Get-KmcChunk7SixthHorseColdSource($Selection,$Fixture,$Candidate){
 $run='20261022-c7b-world184-b184s1-sixthhorse-manual'
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId='unassigned-sixth-horse-cold';assistedColdInventory=$Selection})
 if(-not(Test-KmcChunk7SixthHorseColdSelection $Selection)){throw 'Sixth-Horse source requires its exact zero-write cold case.'}
 $set=Get-KmcChunk7OwnedReceiptSet $run 'chunk7-manual-derived-evidence.json';$r=$set.request
 $proofRoot=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-continuation'
 $gameProof=Read-KmcSmallFixturePinnedJson (Join-Path $proofRoot 'b184-reader-only-reevaluation.json') '6a625108153175d6004b7cd8258eb47d9d37a2efa8f057eff1e428773a3969c0' 1048576
 $baseProof=Read-KmcSmallFixturePinnedJson (Join-Path $proofRoot 'b184-base-only-reader-reevaluation.json') 'ed4ac7708fa0ad35aaba711c1765dc3d2424ea003d71bd391442b347c8fb1648' 1048576
 $originalPins=$gameProof.value.originalEvidencePins
 Assert-KmcAssistedArray $originalPins 10 10 'sixth-Horse original raw evidence closure'
 $paths=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($pin in $originalPins){
  Assert-KmcBootstrapInventoryString (Assert-KmcChildPath $pin.path $set.root 'sixth-Horse raw evidence') $pin.path
  if(-not$paths.Add($pin.path)){throw 'Sixth-Horse original evidence closure repeats a path.'}
  Assert-KmcNotReparsePoint $pin.path 'sixth-Horse raw evidence';Assert-KmcNotHardLink $pin.path 'sixth-Horse raw evidence'
  $file=Get-Item -LiteralPath $pin.path
  if($file.Length-ne$pin.length-or(Get-KmcSha256 $pin.path)-cne$pin.sha256){throw 'Sixth-Horse original raw evidence changed.'}
 }
 foreach($proof in @($gameProof,$baseProof)){
  Assert-KmcBootstrapInventoryString $proof.value.runId $run;Assert-KmcBootstrapInventoryString $proof.value.status 'PASS'
  foreach($key in @('commit','dllSha256')){Assert-KmcBootstrapInventoryString $proof.value.$key $r.$key}
  Assert-KmcChunk7Boolean $proof.value.nativeInvoked $false
 }
 Assert-KmcBootstrapInventoryString $gameProof.value.kind 'sixth-hire-b184-immutable-reader-only-reevaluation'
 Assert-KmcBootstrapInventoryString $gameProof.value.originalOuterStatus 'FAIL'
 Assert-KmcBootstrapInventoryString $gameProof.value.readerCommit 'fdf580d4a28f1df6edb5c0423f24972a20791040'
 Assert-KmcOrdinaryInteger $gameProof.value.assertionPassCount 7 7;Assert-KmcOrdinaryInteger $gameProof.value.assertionFailCount 0 0
 Assert-KmcBootstrapInventoryString $baseProof.value.kind 'sixth-hire-b184-immutable-base-only-reevaluation'
 foreach($pair in @(@('commit','b6d3b965a1de0653bdd2030ab7684f14201b2fca'),
  @('dllSha256','64175c597c938decb095677cad170a78e680231c0ae7eb55c36d2216753a64be'),
  @('dllMvid','93a12a80-8ea4-43c3-bb38-3983c609d1a2'))){Assert-KmcBootstrapInventoryString $r.($pair[0]) $pair[1]}
 Assert-KmcBootstrapInventoryString $r.runId $run;Assert-KmcBootstrapInventoryString $r.scenario 'chunk7-manual-derived-acquisition'
 Assert-KmcBootstrapInventoryString $r.evidenceRoot $set.root
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $r.fixture.$key.sha256 $Fixture.$key.sha256}
 Assert-KmcChunk7ManualDerivedDescriptor $r
 Assert-KmcBootstrapInventoryString $r.manualDerived.nativeWriteLeaf 'Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $set.owner.$key $r.$key}
 Assert-KmcBootstrapInventoryString $set.owner.sourceSha256 $r.persistenceLoad.sha256
 Assert-KmcBootstrapInventoryString $set.owner.saveRoot (Join-Path $set.profile 'Saved Games')
 foreach($receipt in @($set.game,$set.result)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $r.$key}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $set.pins[(Join-Path $set.root 'runtime-artifacts.json')]
 }
 Assert-KmcBootstrapInventoryString $set.game.status 'PASS';Assert-KmcAssistedArray $set.game.errors 0 0 'sixth-Horse native errors'
 foreach($key in @('assertionPassCount','subscenarioPassCount','subscenarioTotal')){Assert-KmcOrdinaryInteger $set.game.$key 7 7}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','suppressedWorkingSaveRequestCount')){Assert-KmcOrdinaryInteger $set.game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $set.game.$key 1 1}
 Assert-KmcChunk7Boolean $set.game.fixtureIdentityVerified $true
 Assert-KmcBootstrapInventoryString $set.result.status 'FAIL';Assert-KmcAssistedArray $set.result.errors 1 1 'preserved sixth-Horse outer failure'
 Assert-KmcBootstrapInventoryString $set.result.errors[0] 'Ordinary native counter is missing, mistyped or out of range.'
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $set.result.$key $true}
 Assert-KmcBootstrapInventoryString $set.result.gameResultSha256 $set.pins[(Join-Path $set.root 'runtime-game-result.json')]
 Assert-KmcBootstrapInventoryString $set.result.restoredSaveInventoryDigest 'df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab'
 Assert-KmcBootstrapInventoryString $set.isolation.runId $run;Assert-KmcBootstrapInventoryString $set.isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed')){Assert-KmcChunk7Boolean $set.isolation.$key $true}
 foreach($key in @('gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $set.isolation.$key $false}
 $ancestry=Get-KmcChunk7ManualDerivedSource $r.manualDerived $Fixture (Get-KmcChunk7RecordedCandidate $r)
 Assert-KmcAssistedSame $ancestry.descriptor $r.persistenceLoad 'sixth-Horse unchanged source Manual307'
 Assert-KmcSixthHireReceiptPins $ancestry
 $artifact=Read-KmcChunk7ManifestArtifact $r $set.manifest 'chunk7-manual-derived-evidence.json' 'chunk7-manual-derived-evidence'
 Assert-KmcChunk7ManualDerivedEvidence $r $artifact (Read-KmcJson $ancestry.artifactPath) 'PASS'
 $w=$artifact.observations.nativeWrite
 Assert-KmcBootstrapInventoryString $w.sha256 $Selection.sourceArchiveSha256
 Assert-KmcBootstrapInventoryString (Get-KmcSha256 (Join-Path $set.root 'chunk7-manual-derived-evidence.json')) $Selection.sourceArtifactSha256
 $saveRoot=Join-Path $set.profile 'Saved Games';$archive=Assert-KmcChildPath $w.path $saveRoot 'sixth-Horse immutable native Manual'
 Assert-KmcBootstrapInventoryString $archive (Join-Path $saveRoot $w.fileName)
 Assert-KmcBootstrapInventoryString $gameProof.value.nativeArchive.path $archive
 Assert-KmcBootstrapInventoryString $gameProof.value.nativeArchive.sha256 $w.sha256
 Assert-KmcOrdinaryInteger $gameProof.value.nativeArchive.length $w.length $w.length
 $descriptor=[pscustomobject][ordered]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=[long]$w.length;
  lastWriteTimeUtcTicks=[long]$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId='unassigned-sixth-horse-cold';assistedColdInventory=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 Assert-KmcAssistedSettledFile $archive $descriptor 'sixth-Horse immutable native Manual'
 $profile=Read-KmcJson (Join-Path $set.root 'profile-cache-changes.json')
 Assert-KmcChunk7Boolean $profile.profileBytesRestoredExactly $true;Assert-KmcAssistedArray $profile.acceptedNativeCacheChanges 0 0 'sixth-Horse exact profile restoration'
 $admission=Read-KmcJson (Join-Path $set.root 'base-game-isolation-admission.json')
 Assert-KmcBootstrapInventoryString $admission.runId $run;Assert-KmcBootstrapInventoryString $admission.transactionToken $r.transactionToken
 Assert-KmcBaseGameDependencyReceipt $admission.dependencies -VerifyFiles
 $inventory=Read-KmcChunk7ManifestArtifact $r $set.manifest 'runtime-loader-inventory.json' 'runtime-loader-inventory'
 Assert-KmcBaseGameNativeInventory $r $inventory $admission.dependencies
 $pinMap=@{}
 foreach($pin in $ancestry.receiptPins){$pinMap[$pin.path]=$pin}
 foreach($pin in $originalPins){$file=Get-Item -LiteralPath $pin.path
  $pinMap[$pin.path]=[pscustomobject]@{path=$pin.path;sha256=$pin.sha256;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}}
 foreach($path in $set.pins.Keys){$file=Get-Item -LiteralPath $path
  $pinMap[$path]=[pscustomobject]@{path=$path;sha256=$set.pins[$path];length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks}}
 foreach($proof in @($gameProof,$baseProof)){$pinMap[$proof.path]=[pscustomobject]@{path=$proof.path;sha256=$proof.sha256;length=$proof.length;lastWriteTimeUtcTicks=$proof.lastWriteTimeUtcTicks}}
 $artifactPath=Join-Path $set.root 'chunk7-manual-derived-evidence.json'
 $source=[pscustomobject]@{path=$archive;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;
  artifactLength=[long](Get-Item -LiteralPath $artifactPath).Length;sourceRunId=$run;sourceProcessId=[int]$set.game.processId;
  receiptPins=@($pinMap.Keys|Sort-Object|ForEach-Object{$pinMap[$_]});fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 Assert-KmcSixthHorseColdReceiptPins $source
 Assert-KmcChunk7OwnedReceiptPins $set
 return $source
}
function Assert-KmcSixthHorseColdReceiptPins($Source){
 Assert-KmcBootstrapInventoryString $Source.sourceRunId '20261022-c7b-world184-b184s1-sixthhorse-manual'
 Assert-KmcBootstrapInventoryString $Source.artifactSha256 '0f6aabe42d0aeaf63f60c6912c1a029c0175ebe87806768f1191df23a913fa13'
 Assert-KmcBootstrapInventoryString $Source.descriptor.sha256 'b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'
 if($Source.receiptPins-isnot[Array]-or$Source.receiptPins.Count-ne41-or@($Source.receiptPins.path|Sort-Object -Unique).Count-ne41){throw 'Sixth-Horse cold source requires its exact 41-receipt closure.'}
 foreach($pin in $Source.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'sixth-Horse cold receipt closure'}
}
