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
