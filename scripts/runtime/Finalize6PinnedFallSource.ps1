# F6: the one pinned post-fall producer a cold consumer may read although its recorded outer verdict is FAIL. Producer
# 20261107-finalize-f6c-producer-03 passed natively (game result PASS, persistence evidence PASS, restored) and wrote its
# Manual; the harness refused it only because the runtime-result reader did not yet know the new subscenario name
# F6-rider-fall-rt-producer (reader-only; the row is now registered). As the F09 sixth-hire source does, the original
# FAIL is never rewritten: every receipt is pinned byte-exact, the single recorded error is pinned, and the corrected
# readers re-evaluate the immutable evidence on every admission. The consumer may run on a later commit only with the
# identical DLL and MVID. Nothing else is relaxed, and no other run or case is admitted this way.
$script:KmcF6PinnedFallSource = [pscustomobject]@{
    runId='20261107-finalize-f6c-producer-03'; case='rider-fall-integration-manual'
    manualSha256='b33eda46d65ce242e77a7cb105e8daf18f07134459f59a954025460b93ac2edc'; manualLength=857840
    commit='c7ca59895e0b40c38ce518921e8eef19b0eb35c1'; branch='codex/kmc-finalize-f6-falls-final-acceptance'
    dllSha256='b18cc0560b98d213abd542a48e0d7781eb1859ff86298cee16a8030ea72d7742'; dllMvid='2a9d61d7-99b2-4995-be43-3210a087ccae'
    error='Runtime game-result contains an unknown subscenario: F6-rider-fall-rt-producer'
    restoredSaveInventoryDigest='60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1'
    receipts=[ordered]@{
        'runtime-request.json'='df1fbc66c1592d1072296e53ed51d67ed8d9df058ff09cdee88f08e7313f34db'
        'runtime-game-result.json'='074bbdf239d28072b89b86ce27adddb0535b5228b0fa0a835e5acb99cf15e309'
        'runtime-result.json'='edc7016b8d906f4f93d3abe36a0ba416d39dde55aea6dfaaad1dd008a4163eb7'
        'runtime-artifacts.json'='39de4fccd0846ae6f29a4f44b8473c709932f42090e100ab5c1fc321545623fb'
        'base-game-isolation-result.json'='cfdbbe91aaef6df3e377ecf38d8729a3c9fa3209ec1527bfb611c8696a33643e'
        'chunk7-persistence-evidence.json'='3007479932277db76ca2706b9dc24e534b340523d59a6150f8e48b87dcdb5afc' } }

function Test-KmcFinalize6PinnedFallSource([string]$SourceRunId,[string]$ExpectedSha256,[string]$Case){
    $p=$script:KmcF6PinnedFallSource
    $SourceRunId-ceq$p.runId-and$ExpectedSha256-ceq$p.manualSha256-and$Case-ceq$p.case
}

# The pinned run's own receipts, its one preserved error, and the corrected re-evaluation of the condition that failed.
function Assert-KmcFinalize6PinnedFallSourceReceipts([string]$EvidenceRoot,$Request,$Result,$Isolation,$Game,[string]$ExpectedDllSha256){
    $p=$script:KmcF6PinnedFallSource
    foreach($leaf in $p.receipts.Keys){
        if((Get-KmcSha256 (Join-Path $EvidenceRoot $leaf))-cne$p.receipts[$leaf]){throw ('F6 pinned source receipt differs: '+$leaf)}
    }
    foreach($pair in @(@('runId',$p.runId),@('commit',$p.commit),@('branch',$p.branch),@('dllSha256',$p.dllSha256),@('dllMvid',$p.dllMvid))){
        if([string]$Request.($pair[0])-cne$pair[1]-or[string]$Result.($pair[0])-cne$pair[1]-or[string]$Game.($pair[0])-cne$pair[1]){throw ('F6 pinned source identity differs: '+$pair[0])}
    }
    if($ExpectedDllSha256-cne$p.dllSha256){throw 'F6 pinned source is admitted only for its identical DLL.'}
    if([string]$Request.persistenceCase-cne$p.case-or[string]$Result.status-cne'FAIL'-or$Result.errors-isnot[Array]-or$Result.errors.Count-ne1-or[string]$Result.errors[0]-cne$p.error-or
       [string]$Result.restoredSaveInventoryDigest-cne$p.restoredSaveInventoryDigest){throw 'F6 pinned source is not the preserved reader-only outer failure.'}
    if([string]$Game.status-cne'PASS'-or[long]$Game.assertionFailCount-ne0-or[long]$Game.subscenarioFailCount-ne0){throw 'F6 pinned source did not pass natively.'}
    foreach($field in @('gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $Isolation.$field $false}
    # Re-evaluation of the failed condition: every native subscenario is now a registered row.
    $known=@(Get-KmcChunk7PersistenceRows)
    foreach($item in @($Game.subscenarioResults)){if([string]$item.name-cnotin$known){throw ('F6 pinned source still has an unregistered subscenario: '+[string]$item.name)}}
}
