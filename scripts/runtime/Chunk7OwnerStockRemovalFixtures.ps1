Set-StrictMode -Version Latest

# This source is the settled batch64 native Manual, not Working and not an
# owner save. No caller-selected archive or mutable ancestry is admitted.
function Get-KmcOwnerStockRemovalSource($Fixture){
 $run='20261020-c7b-world80-b64s1-stock-mammoth'
 $sha='e475d49c3590927c7f216d301aa8535f6dbbdc7af2066f4c01f58fff335866fa'
 $lab=Get-KmcLabRoot
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$run)) (Join-Path $lab 'runtime-evidence') 'stock Mammoth producer evidence'
 Assert-KmcDirectoryTreeCloneable $root 'stock Mammoth producer evidence'
 $request=Read-KmcJson (Join-Path $root 'runtime-request.json')
 $result=Read-KmcJson (Join-Path $root 'runtime-result.json')
 $sourceRoot=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$run)) (Join-Path $lab 'runtime-staging') 'stock Mammoth producer root'
 Assert-KmcDirectoryTreeCloneable $sourceRoot 'stock Mammoth producer root'
 $owner=Read-KmcJson (Join-Path $sourceRoot 'owner.json')
 $gamePath=Join-Path $root 'runtime-game-result.json';$manifestPath=Join-Path $root 'runtime-artifacts.json'
 $game=Read-KmcJson $gamePath;$manifest=Read-KmcJson $manifestPath
 if($request.runId-cne$run-or$request.scenario-cne'chunk7-stock-mammoth-fixture-preparation'-or
    $request.commit-cne'fbb1116a50e71edc54ba249369e35ccb48725e82'-or
    $request.dllSha256-cne'49d3aac8081ce0c9c2ef646c2cb13334774ccf2d11cd42e550512e674a30f5b3'-or
    (ConvertTo-KmcChunk7Canonical $request.fixture)-cne(ConvertTo-KmcChunk7Canonical $Fixture)-or
    $owner.runId-cne$run-or$owner.scenario-cne$request.scenario-or$owner.transactionToken-cne$request.transactionToken-or
    $owner.sourceSha256-cne$request.persistenceLoad.sha256-or
    $result.status-cne'PASS'-or$result.runId-cne$run-or$result.scenario-cne$request.scenario-or
    $result.transactionToken-cne$request.transactionToken-or$result.modsRestored-ne$true-or
    $result.workingRestored-ne$true-or$result.saveProtectionPassed-ne$true-or
    $result.baselineImmutable-ne$true-or$result.saveWriteAllowlistPassed-ne$true-or
    @($result.errors).Count-ne0-or$game.status-cne'PASS'-or$game.runId-cne$run-or
    (Get-KmcSha256 $gamePath)-cne$result.gameResultSha256-or
    (Get-KmcSha256 $manifestPath)-cne$result.evidenceManifestSha256-or
    (Get-KmcSha256 $manifestPath)-cne$game.evidenceManifestSha256){
  throw 'Stock Mammoth source is not its exact completed and restored batch64 native process.'
 }
 Assert-KmcChunk7StockMammothFixtureArtifact $request $manifest 'PASS'
 $artifact=Read-KmcJson (Join-Path $root 'chunk7-stock-mammoth-fixture-evidence.json')
 Assert-KmcChunk7StockMammothFixtureOutput $request $artifact
 $w=$artifact.observations.nativeWrite
 if($w.sha256-cne$sha-or$w.fileName-cne'Manual_304_KMC_C7_STOCK_MAMMOTH.zks'-or
    $w.internalName-cne'KMC_C7_STOCK_MAMMOTH'-or$w.gameId-cne'3f7381f7-aba6-43bf-bae6-39c6a66a7180'-or
    $w.gameName-cne'Testy the Modless'-or$w.area-cne'b10e86516b6437d45a629931c30936fe'){
  throw 'Stock Mammoth selected Manual differs from its exact reference-free native output.'
 }
 $path=Assert-KmcChildPath $w.path (Join-Path $lab 'runtime-staging') 'stock Mammoth source Manual'
 Assert-KmcNotReparsePoint $path 'stock Mammoth source Manual';Assert-KmcNotHardLink $path 'stock Mammoth source Manual'
 $file=Get-Item -LiteralPath $path
 if($file.Length-ne1257468-or(Get-KmcSha256 $path)-cne$sha){throw 'Stock Mammoth source Manual changed.'}
 [pscustomobject]@{path=$path;descriptor=[ordered]@{internalName=$w.internalName;fileName=$w.fileName;
  sha256=$sha;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;
  gameId=$w.gameId;gameName=$w.gameName;area=$w.area};producerRunId=$run;producerCommit=$request.commit}
}

# The distinct no-DLL process consumes one genuine mounted cleanup Manual: batch65's
# (accepted for history, exactly as before) or, for the 7E final campaign (run plan
# gate G7 (a)), the same candidate's own settled r31 prepare-removal run. Its archive
# stays untouched and is copied into that process's isolated root.
function Get-KmcOwnerStockMammothSha256 {'e475d49c3590927c7f216d301aa8535f6dbbdc7af2066f4c01f58fff335866fa'}
function Test-KmcOwnerStockFinalCleanupRunId([string]$RunId){$RunId-cmatch'^[0-9]{8}-c7e-final-B[0-9]+-r31r?$'}
function Get-KmcOwnerStockCleanupSource([string]$SourceRunId,[string]$ExpectedSha256,$Fixture,$Candidate=$null,[string]$SuiteId=$null,[string]$RunMapPath=$null){
 $run='20261020-c7b-world81-b65s1-owner-stock-removal'
 $sha='2c46c2700f117b3f0d50f688d2c89bd879de79d4b0b40eaf3b2502c1015bdfc2'
 if($SourceRunId-ceq$run){
  if($ExpectedSha256-cne$sha-or-not[string]::IsNullOrEmpty($RunMapPath)){throw 'Owner stock no-DLL cold source differs from the exact batch65 cleanup Manual.'}
  return Get-KmcOwnerStockBatch65CleanupSource $Fixture
 }
 if(-not(Test-KmcOwnerStockFinalCleanupRunId $SourceRunId)){throw 'Owner stock no-DLL cold source is neither the exact batch65 cleanup Manual nor a final-campaign r31 run.'}
 return Get-KmcOwnerStockFinalCleanupSource $SourceRunId $ExpectedSha256 $Fixture $Candidate $SuiteId $RunMapPath
}
function Get-KmcOwnerStockBatch65CleanupSource($Fixture){
 $run='20261020-c7b-world81-b65s1-owner-stock-removal'
 $sha='2c46c2700f117b3f0d50f688d2c89bd879de79d4b0b40eaf3b2502c1015bdfc2'
 $lab=Get-KmcLabRoot
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$run)) (Join-Path $lab 'runtime-evidence') 'owner stock cleanup evidence'
 Assert-KmcDirectoryTreeCloneable $root 'owner stock cleanup evidence'
 $request=Read-KmcJson (Join-Path $root 'runtime-request.json')
 $result=Read-KmcJson (Join-Path $root 'runtime-result.json')
 $sourceRoot=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$run)) (Join-Path $lab 'runtime-staging') 'owner stock cleanup root'
 Assert-KmcDirectoryTreeCloneable $sourceRoot 'owner stock cleanup root'
 $owner=Read-KmcJson (Join-Path $sourceRoot 'owner.json')
 if($request.ownerStockRemoval-cne$true-or$request.commit-cne'aac0b9f9a2a4da52818b52a3a3c58633744a4a62'-or
    $request.dllSha256-cne'c722118f2d57d2d31c4e8e82dc73c7514e44cec614be57cab473cc7f87c2cb76'-or
    $request.persistenceLoad.sha256-cne(Get-KmcOwnerStockMammothSha256)-or
    $owner.runId-cne$run-or$owner.scenario-cne'persistence-p07-save'-or$owner.persistenceCase-cne'prepare-removal'-or
    $owner.transactionToken-cne$request.transactionToken-or$owner.sourceSha256-cne$request.persistenceLoad.sha256-or$result.status-cne'PASS'-or
    (ConvertTo-KmcChunk7Canonical $request.fixture)-cne(ConvertTo-KmcChunk7Canonical $Fixture)){
  throw 'Owner stock cleanup is not its accepted completed batch65 native process.'
 }
 Assert-KmcCurrentRemovalSource $run $sha $Fixture 'cleanup-manual'
 $path=Assert-KmcChildPath (Join-Path $sourceRoot 'Saved Games/Manual_306_KMC_CLEANUP.zks') $sourceRoot 'owner stock cleanup Manual'
 Assert-KmcNotReparsePoint $path 'owner stock cleanup Manual';Assert-KmcNotHardLink $path 'owner stock cleanup Manual'
 $file=Get-Item -LiteralPath $path
 if($file.Length-ne1263594-or(Get-KmcSha256 $path)-cne$sha){throw 'Owner stock cleanup Manual changed.'}
 [pscustomobject]@{path=$path;descriptor=[ordered]@{internalName='KMC_CLEANUP';fileName=$file.Name;
  sha256=$sha;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;
  gameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180';gameName='Testy the Modless';area='b10e86516b6437d45a629931c30936fe'};
  producerRunId=$run;producerCommit=$request.commit}
}
# Run plan gate G7 (a): the final campaign's own r31 (persistence-p07-save/prepare-removal on the
# stock Mammoth fixture) feeds row 32 only when it is the same candidate as this run (commit, DLL,
# MVID and qualification suite) and its settled receipt in the executor's run map names it:
# external reader verdict PASS, restoration verified, and the SHA-256 of its runtime-result.json.
function Get-KmcOwnerStockFinalCleanupSource([string]$SourceRunId,[string]$ExpectedSha256,$Fixture,$Candidate,[string]$SuiteId,[string]$RunMapPath){
 if($ExpectedSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Final r31 cleanup source requires its exact expected archive SHA-256.'}
 if($null-eq$Candidate-or[string]$Candidate.commit-cnotmatch'^[0-9a-f]{40}$'-or[string]$Candidate.dllSha256-cnotmatch'^[0-9a-f]{64}$'-or
    [string]::IsNullOrEmpty([string]$Candidate.dllMvid)-or[string]::IsNullOrEmpty($SuiteId)){
  throw 'Final r31 cleanup source requires the consuming run''s exact candidate (commit, DLL, MVID) and qualification suite.'
 }
 if([string]::IsNullOrEmpty($RunMapPath)-or-not(Test-Path -LiteralPath $RunMapPath -PathType Leaf)){throw 'Final r31 cleanup source requires the executor''s settled run map.'}
 Assert-KmcNotReparsePoint $RunMapPath 'final run map'
 $map=Read-KmcJson $RunMapPath
 $entry=if($null-ne$map.PSObject.Properties['r31']){$map.r31}else{$null}
 if($null-eq$entry-or$entry-is[string]-or$entry.runId-cne$SourceRunId-or$entry.readerVerdict-isnot[string]-or$entry.readerVerdict-cne'PASS'-or$entry.restorationVerified-isnot[bool]-or$entry.restorationVerified-ne$true-or
    [string]$entry.resultSha256-cnotmatch'^[0-9a-f]{64}$'){
  throw 'Final r31 lacks its settled receipt (runId, resultSha256, readerVerdict PASS, restorationVerified true) in the run map.'
 }
 $lab=Get-KmcLabRoot
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$SourceRunId)) (Join-Path $lab 'runtime-evidence') 'final r31 evidence'
 Assert-KmcDirectoryTreeCloneable $root 'final r31 evidence'
 $resultPath=Join-Path $root 'runtime-result.json'
 if((Get-KmcSha256 $resultPath)-cne$entry.resultSha256){throw 'Final r31 result differs from its settled receipt.'}
 $request=Read-KmcJson (Join-Path $root 'runtime-request.json')
 $result=Read-KmcJson $resultPath
 $sourceRoot=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path $lab 'runtime-staging') 'final r31 root'
 Assert-KmcDirectoryTreeCloneable $sourceRoot 'final r31 root'
 $owner=Read-KmcJson (Join-Path $sourceRoot 'owner.json')
 $suite=if($null-ne$request.PSObject.Properties['qualificationSuite']){[string]$request.qualificationSuite.suiteId}else{$null}
 if($request.runId-cne$SourceRunId-or$request.ownerStockRemoval-cne$true-or$request.scenario-cne'persistence-p07-save'-or$request.persistenceCase-cne'prepare-removal'-or
    $request.commit-cne$Candidate.commit-or$request.dllSha256-cne$Candidate.dllSha256-or$request.dllMvid-cne$Candidate.dllMvid-or$suite-cne$SuiteId-or
    $request.persistenceLoad.sha256-cne(Get-KmcOwnerStockMammothSha256)-or
    $owner.runId-cne$SourceRunId-or$owner.scenario-cne'persistence-p07-save'-or$owner.persistenceCase-cne'prepare-removal'-or
    $owner.transactionToken-cne$request.transactionToken-or$owner.sourceSha256-cne$request.persistenceLoad.sha256-or$result.status-cne'PASS'-or$result.runId-cne$SourceRunId-or
    (ConvertTo-KmcChunk7Canonical $request.fixture)-cne(ConvertTo-KmcChunk7Canonical $Fixture)){
  throw 'Final r31 is not the same candidate''s completed owner stock prepare-removal process on the stock Mammoth fixture.'
 }
 Assert-KmcCurrentRemovalSource $SourceRunId $ExpectedSha256 $Fixture 'cleanup-manual'
 $path=Assert-KmcChildPath (Join-Path $sourceRoot 'Saved Games/Manual_306_KMC_CLEANUP.zks') $sourceRoot 'final r31 cleanup Manual'
 Assert-KmcNotReparsePoint $path 'final r31 cleanup Manual';Assert-KmcNotHardLink $path 'final r31 cleanup Manual'
 $file=Get-Item -LiteralPath $path
 if($file.Length-lt1-or$file.Length-gt256MB-or(Get-KmcSha256 $path)-cne$ExpectedSha256){throw 'Final r31 cleanup Manual differs from its recorded native write.'}
 if((Get-KmcSha256 $resultPath)-cne$entry.resultSha256){throw 'Final r31 result changed during qualification.'}
 [pscustomobject]@{path=$path;descriptor=[ordered]@{internalName='KMC_CLEANUP';fileName=$file.Name;
  sha256=$ExpectedSha256;length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;
  gameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180';gameName='Testy the Modless';area='b10e86516b6437d45a629931c30936fe'};
  producerRunId=$SourceRunId;producerCommit=$request.commit;settledResultSha256=$entry.resultSha256}
}
