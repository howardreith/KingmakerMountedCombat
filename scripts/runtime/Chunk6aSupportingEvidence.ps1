. (Join-Path $PSScriptRoot 'AreaRegressionProjectionEvidence.ps1')
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

. (Join-Path $PSScriptRoot 'LegacyCombatProjectionEvidence.ps1')
. (Join-Path $PSScriptRoot 'PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk6aFoundationEvidence.ps1')
# Fixed same-candidate supporting-run contract for Chunk 6A qualification.
# Original KMC ledger validation; reads only project-owned settled evidence.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk6aHarnessIdentity.ps1')
function Get-KmcBoundJson([string]$Path,[string]$ExpectedSha256) {
    if($ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Evidence requires an exact SHA-256.' }
    $actual=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if($actual -cne $ExpectedSha256) { throw "Bound evidence bytes differ: $Path" }
    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}
# The run-time overall facet is the earlier harness's verdict. When it is a FAIL whose every
# error is a dedicated external-reader refusal while the native facet is an exact PASS, a pure
# reader correction may re-evaluate the immutable artifact under the new harness identity:
# the binding records the original refusal and the re-evaluating reader digest, and the complete
# scenario validator must accept the immutable bytes under the current readers every time the
# binding is validated. Nothing is inferred: a native failure, a harness failure or any error
# outside the dedicated readers is never re-evaluated.
function Test-KmcExternalReaderRefusal([string]$Message) {
    # The prefixed messages are the external readers' own; the prediction-rule messages are raised at run time by
    # Assert-KmcNativePredictionCommands (the external mirror of the compiled prediction rule). A compiled refusal of
    # the same text never reaches a re-evaluation: the game result must be a PASS with no errors first.
    [string]$Message -cmatch '^(Action economy|Child entry preamble|Additional Chunk6A binding differs|Chunk 6A foundation): ' -or
    # The dedicated pre-combat positioning reader (Chunk6aPreCombatPositioningEvidence.ps1) refuses with its own
    # "TB positioning ..." messages; frozen 205 final consolidation: six native-PASS runs refused only by its stale
    # forward-route bound, re-qualified under the corrected harness identity.
    [string]$Message -cmatch '^TB positioning ' -or
    [string]$Message -cmatch '^(Missing bounded native prediction evidence\.|Prediction (lacks one actual Init|omitted an exact native clock or sequence|shares or omits command/shell identity|has another [A-Za-z]+|was not observed before the real Init|live object identity differs|executed or bound a native process|Init already bound a process|lacks its exact temporary admission|escaped temporary admission or had a native side effect)\.|Committed observation was native speculation\.|Native speculation added a resource or movement callback\.)$'
}
function Get-KmcChunk6aReevaluation($Result,$Game,[string]$RepoRoot) {
    if([string]$Game.status -cne 'PASS' -or [int]$Game.assertionFailCount -ne 0 -or @($Game.errors).Count -ne 0) { return $null }
    if([string]$Result.status -cne 'FAIL' -or [int]$Result.assertionFailCount -lt 1) { return $null }
    $errors=@($Result.errors)
    if($errors.Count -lt 1 -or @($errors | Where-Object { -not (Test-KmcExternalReaderRefusal ([string]$_)) }).Count -ne 0) { return $null }
    $harness=Get-KmcChunk6aHarnessIdentity $RepoRoot
    [pscustomobject][ordered]@{
        contract='immutable-artifact-reevaluated-under-new-harness-identity'
        originalOverall=[pscustomobject][ordered]@{status=[string]$Result.status;assertionPassCount=[int]$Result.assertionPassCount;assertionFailCount=[int]$Result.assertionFailCount;errors=@($errors|ForEach-Object {[string]$_})}
        readerRevision=[string]$harness.revision
        readerDigest=[string]$harness.readerDigest
    }
}
function Assert-KmcSupportingRun($Payload,$Binding,[string]$LabRoot) {
    $run=[string]$Binding.runId
    if($run -cnotmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Invalid supporting run ID.' }
    $reevaluation=if($null -ne $Binding.PSObject.Properties['reevaluation']){$Binding.reevaluation}else{$null}
    $area=[string]$Binding.scenario -ceq 'chunk4-area-cleanup'
    $legacy=[string]$Binding.scenario -ceq 'mounted-mammoth-primary-hit-tb'
    $foundation=Test-KmcChunk6aFoundationScenario ([string]$Binding.scenario)
    if($legacy) {
        if([string]$Binding.evidenceLeaf -cne 'combat-scenario-evidence.jsonl' -or @($Binding.rows).Count -ne 1 -or [string]$Binding.rows[0] -cne 'mounted-mammoth-primary-hit-tb') { throw 'Legacy Mammoth requires its exact scenario, JSONL leaf and row.' }
    } elseif($area) {
        if($Binding.evidenceLeaf-cne'boundary-scenario-evidence.jsonl'-or@($Binding.rows).Count-ne1-or$Binding.rows[0]-cne'native-area-clean-dismount'){throw 'Area requires exact scenario, JSONL and native row'}
    } elseif($foundation) {
        if($Binding.evidenceLeaf-cne'persistence-observations.jsonl'-or@($Binding.rows).Count-ne1){throw 'Foundation persistence requires the exact scenario, JSONL leaf and one row'}
    } elseif([string]$Binding.evidenceLeaf -cnotmatch '^[A-Za-z0-9._-]+\.json$') { throw 'Evidence must name one JSON leaf.' }
    $root=Join-Path $LabRoot ('runtime-evidence/'+$run)
    $result=Get-KmcBoundJson (Join-Path $root 'runtime-result.json') $Binding.resultSha256
    $game=Get-KmcBoundJson (Join-Path $root 'runtime-game-result.json') $Binding.gameResultSha256
    $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
    $orchestration=Get-KmcBoundJson (Join-Path $root 'orchestration.json') $Binding.orchestrationSha256
    $transaction=Get-KmcBoundJson (Join-Path $LabRoot ('runtime-state/run-transactions/'+$run+'.json')) $Binding.transactionSha256
    $artifact=if($legacy){Get-KmcLegacyCombatProjection $Binding $request $game $result $root}elseif($area){Get-KmcAreaRegressionProjection $Binding $request $game $result $root}elseif($foundation){Get-KmcChunk6aFoundationProjection $Binding $request $game $result $root}else{Get-KmcBoundJson (Join-Path $root $Binding.evidenceLeaf) $Binding.evidenceSha256}
    foreach($item in @($result,$game,$request)) {
        if([string]$item.runId -cne $run -or [string]$item.scenario -cne [string]$Binding.scenario) { throw 'Supporting run or scenario differs.' }
        if([string]$item.commit -cne [string]$Payload.commit -or [string]$item.branch -cne [string]$Payload.branch -or
           [string]$item.productVersion -cne [string]$Payload.version -or [string]$item.dllSha256 -cne [string]$Payload.dllSha256 -or
           [string]$item.dllMvid -cne [string]$Payload.dllMvid) { throw 'Supporting run executed another payload.' }
        if([string]$item.transactionToken -cne [string]$transaction.token) { throw 'Supporting transaction token differs.' }
    }
    if($null -eq $reevaluation) {
        foreach($item in @($result,$game)) {
            if([string]$item.status -cne 'PASS' -or [int]$item.assertionFailCount -ne 0 -or [int]$item.assertionPassCount -lt 1 -or
               [int]$item.assertionPassCount -ne [int]$Binding.passCount -or [int]$Binding.failCount -ne 0 -or @($item.errors).Count -ne 0) {
                throw 'Supporting native or overall run is not an exact PASS.'
            }
        }
    } else {
        if([string]$reevaluation.contract -cne 'immutable-artifact-reevaluated-under-new-harness-identity') { throw 'Supporting re-evaluation contract differs.' }
        if([string]$game.status -cne 'PASS' -or [int]$game.assertionFailCount -ne 0 -or [int]$game.assertionPassCount -lt 1 -or
           [int]$game.assertionPassCount -ne [int]$Binding.passCount -or [int]$Binding.failCount -ne 0 -or @($game.errors).Count -ne 0) {
            throw 'Re-evaluated supporting run is not an exact native PASS.'
        }
        $original=$reevaluation.originalOverall
        if([string]$result.status -cne 'FAIL' -or [string]$original.status -cne 'FAIL' -or [int]$result.assertionPassCount -ne [int]$original.assertionPassCount -or
           [int]$result.assertionFailCount -ne [int]$original.assertionFailCount -or [int]$result.assertionFailCount -lt 1 -or
           (ConvertTo-Json @($result.errors|ForEach-Object {[string]$_}) -Compress) -cne (ConvertTo-Json @($original.errors|ForEach-Object {[string]$_}) -Compress)) {
            throw 'Re-evaluated supporting run does not record its original overall refusal exactly.'
        }
        if(@(@($result.errors) | Where-Object { -not (Test-KmcExternalReaderRefusal ([string]$_)) }).Count -ne 0) { throw 'Re-evaluated supporting run failed outside the dedicated external readers.' }
        $repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
        $current=Get-KmcChunk6aHarnessIdentity $repoRoot
        if([string]$reevaluation.readerDigest -cne [string]$current.readerDigest -or [string]$reevaluation.readerRevision -cnotmatch '^[0-9a-f]{40}$') { throw 'Re-evaluated supporting run names another reader digest than the current harness.' }
    }
    if([string]$result.gameResultSha256 -cne [string]$Binding.gameResultSha256) { throw 'Overall result does not bind the native result.' }
    if([string]$request.qualificationSuite.suiteId -cne [string]$Payload.suiteId -or
       [string]$request.qualificationSuite.snapshotSha256 -cne [string]$Payload.suiteSha256 -or
       [string]$transaction.qualificationSuiteId -cne [string]$Payload.suiteId -or
       [string]$transaction.qualificationSuiteSnapshotSha256 -cne [string]$Payload.suiteSha256) { throw 'Supporting run used another qualification suite.' }
    # The orchestration records the run-time overall verdict: PASS, or FAIL for a re-evaluated run whose overall facet was the earlier reader's refusal.
    if([string]$orchestration.runId -cne $run -or [string]$orchestration.scenario -cne [string]$Binding.scenario -or
       [string]$orchestration.status -cne $(if($null -eq $reevaluation){'PASS'}else{'FAIL'}) -or [string]$orchestration.stage -cne 'restored' -or
       [string]$transaction.runId -cne $run -or [string]$transaction.phase -cne 'restored') { throw 'Supporting transaction is not settled and restored.' }
    foreach($field in @('modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed')) {
        if($result.$field -ne $true -or $transaction.$field -ne $true) { throw "Supporting restoration failed: $field" }
    }
    if(@($transaction.restorationErrors).Count -ne 0 -or
       [string]$transaction.modsDigestBefore -cne [string]$transaction.restoredModsDigest -or
       [string]$transaction.saveInventoryDigestBefore -cne [string]$transaction.restoredSaveInventoryDigest) { throw 'Supporting restoration inventory differs.' }
    foreach($field in @('runId','scenario','commit','branch','productVersion','dllSha256','dllMvid')) {
        if([string]$artifact.$field -cne [string]$game.$field) { throw 'Supporting artifact payload or run differs.' }
    }
    if($artifact.status -cne 'PASS' -or @($artifact.errors).Count -ne 0) { throw 'Supporting artifact is not PASS.' }
    if($null -ne $reevaluation) {
        # The complete scenario validator must accept the immutable bytes under the current readers.
        . (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
        if([string]$Binding.evidenceLeaf -ceq 'phase3d-horse-scenario-evidence.json') { Assert-KmcChunk6aCombatMountEvidence $request $artifact 'PASS' }
        elseif(-not $foundation) { throw 'Re-evaluation is defined only for Phase 3D Horse artifacts and foundation persistence projections.' }
        # A foundation persistence projection already re-ran the complete persistence validator above.
    }
    $rows=@($Binding.rows)
    if($rows.Count -lt 1 -or @($rows | Select-Object -Unique).Count -ne $rows.Count) { throw 'Supporting rows must be nonempty and unique.' }
    foreach($rowName in $rows) {
        $matched=@($artifact.rows | Where-Object { [string]$_.name -ceq [string]$rowName })
        if($matched.Count -ne 1 -or [string]$matched[0].status -cne 'PASS') { throw "Supporting artifact lacks exact PASS row: $rowName" }
    }
}
function Assert-KmcCompositeRuns($Payload,$Bindings,$RequiredRoles,[string]$LabRoot) {
    # RequiredRoles is owned by the fixed mandatory claim contract, not supplied by a ledger entry.
    $items=@($Bindings);$roles=@($RequiredRoles)
    if($roles.Count -lt 2 -or @($roles | ForEach-Object { [string]$_.name } | Select-Object -Unique).Count -ne $roles.Count -or $items.Count -ne $roles.Count) { throw 'Composite evidence lacks required roles.' }
    Assert-KmcCompositeRoleSet $items $roles
    foreach($role in $roles) {
        $matches=@($items | Where-Object { [string]$_.role -ceq [string]$role.name })
        if([string]$matches[0].scenario -cne [string]$role.scenario) { throw 'Supporting role requires its declared scenario.' }
        foreach($requiredRow in @($role.rows)) {
            if(@($matches[0].rows) -cnotcontains [string]$requiredRow) { throw 'Supporting role omits a required row.' }
        }
        Assert-KmcSupportingRun $Payload $matches[0] $LabRoot
    }
}

function Get-KmcChunk6aCampaignRoles([bool]$IncludeTurnBased) {
    @(
        @{name='positive';scenario='chunk6a-mount-approach';rows=@('CM01-exploration-free','CM02-approach-arrival')}
        @{name='compensation-rt';scenario='chunk6a-adoption-compensation-rt';rows=@('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases')}
        @{name='compensation-tb';scenario='chunk6a-adoption-compensation-tb';rows=@('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases','CM01-combat-mount-preparing-refused')}
        @{name='geometry';scenario='chunk6a-geometry-change';rows=@('CM02-geometry-change')}
        @{name='obstruction';scenario='chunk6a-obstruction';rows=@('CM02-obstruction')}
        @{name='full-rt';scenario='chunk6a-combat-mount-rt';rows=@('CM01-combat-mount-accepted','CM02-approach-arrival','CM05-combat-dismount-accepted','CM05-combat-dismount-conserves-debt','CM05-no-duplicate-mount-turn')}
    )
    if($IncludeTurnBased) {
        @{name='full-tb';scenario='chunk6a-combat-mount-tb';rows=@('CM01-combat-mount-accepted','CM01-combat-mount-preparing-refused','CM02-approach-arrival','CM05-combat-dismount-accepted','CM05-combat-dismount-conserves-debt','CM05-no-duplicate-mount-turn')}
    }
}
function Assert-KmcChunk6aCampaign($Payload,$Bindings,[bool]$IncludeTurnBased,[string]$LabRoot) {
    $roles=@(Get-KmcChunk6aCampaignRoles $IncludeTurnBased)
    Assert-KmcCompositeRuns $Payload $Bindings $roles $LabRoot
}
# A composite requires an exact set of independent role bindings: each declared role exactly
# once, no missing, duplicate or foreign role, separate native transactions, and the accepted
# bindings sorted into the canonical declared order. It never forces the native transactions to
# have executed chronologically in declaration order.
function Assert-KmcCompositeRoleSet($Bindings,$Roles) {
    $items=@($Bindings);$roles=@($Roles)
    $names=@($roles | ForEach-Object { [string]$_.name })
    if($names.Count -lt 1 -or @($names | Select-Object -Unique).Count -ne $names.Count) { throw 'Composite contract roles are not unique.' }
    if($items.Count -ne $names.Count) { throw 'Composite evidence lacks required roles.' }
    foreach($item in $items) {
        if([string]$item.role -cnotin $names) { throw ('Composite evidence names a foreign role: '+[string]$item.role) }
    }
    foreach($name in $names) {
        if(@($items | Where-Object { [string]$_.role -ceq $name }).Count -ne 1) { throw "Composite evidence needs exactly one $name run." }
    }
    $runIds=@($items | ForEach-Object { [string]$_.runId })
    if(@($runIds | Select-Object -Unique).Count -ne $items.Count) { throw 'Composite roles require separate native transactions.' }
    for($index=0;$index -lt $names.Count;$index++) {
        if([string]$items[$index].role -cne $names[$index]) { throw 'Composite bindings are not sorted into the canonical declared role order.' }
    }
}
function Sort-KmcCompositeBindings($Bindings,$Roles) {
    $items=@($Bindings);$names=@(@($Roles) | ForEach-Object { [string]$_.name })
    $sorted=@(foreach($name in $names) { $found=@($items | Where-Object { [string]$_.role -ceq $name }); if($found.Count -ne 1) { throw "Composite evidence needs exactly one $name run." }; $found[0] })
    if($sorted.Count -ne $items.Count) { throw 'Composite evidence names a foreign role.' }
    Assert-KmcCompositeRoleSet $sorted $Roles
    ,$sorted
}

function Assert-KmcChunk6aFrozenPayload($Payload,[string]$LabRoot) {
    if([string]$Payload.suiteId -cnotmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Invalid qualification suite ID.' }
    $suite=Get-KmcBoundJson (Join-Path $LabRoot ('runtime-state/qualification-suite-snapshots/'+$Payload.suiteId+'.json')) $Payload.suiteSha256
    if((Get-FileHash -LiteralPath $Payload.packagePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Payload.packageSha256) { throw 'Frozen package bytes differ.' }
    $manifest=Get-KmcBoundJson ($Payload.packagePath+'.manifest.json') $Payload.manifestSha256
    if($manifest.worktreeClean -ne $true -or $manifest.qualificationEligible -ne $true) { throw 'Frozen package is not qualification eligible.' }
    foreach($field in @('commit','branch','version','dllSha256','dllMvid','packageSha256')) {
        if([string]$manifest.$field -cne [string]$Payload.$field) { throw 'Frozen manifest payload differs.' }
    }
    if($suite.suiteId -cne $Payload.suiteId -or $suite.repository.commit -cne $Payload.commit -or
        $suite.repository.branch -cne $Payload.branch -or $suite.package.sha256 -cne $Payload.packageSha256 -or
        $suite.package.manifestSha256 -cne $Payload.manifestSha256 -or $suite.package.productVersion -cne $Payload.version -or
        $suite.package.dllSha256 -cne $Payload.dllSha256 -or $suite.package.dllMvid -cne $Payload.dllMvid) { throw 'Frozen suite payload differs.' }
}
function Get-KmcSupportingBinding([string]$Role,[string]$RunId,[string[]]$Rows,[string]$LabRoot) {
    if($RunId -cnotmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Invalid supporting run ID.' }
    $root=Join-Path $LabRoot ('runtime-evidence/'+$RunId)
    $result=Get-Content -Raw (Join-Path $root 'runtime-result.json')|ConvertFrom-Json
    $leaf=if($result.scenario -ceq 'chunk4-area-cleanup'){'boundary-scenario-evidence.jsonl'}elseif($result.scenario -ceq 'mounted-mammoth-primary-hit-tb'){'combat-scenario-evidence.jsonl'}elseif(Test-KmcChunk6aFoundationScenario ([string]$result.scenario)){'persistence-observations.jsonl'}else{'phase3d-horse-scenario-evidence.json'}
    $game=Get-Content -Raw (Join-Path $root 'runtime-game-result.json')|ConvertFrom-Json
    $reevaluation=Get-KmcChunk6aReevaluation $result $game ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..')))
    $binding=[ordered]@{role=$Role;runId=$RunId;scenario=$result.scenario;passCount=$(if($null -ne $reevaluation){$game.assertionPassCount}else{$result.assertionPassCount});failCount=$(if($null -ne $reevaluation){0}else{$result.assertionFailCount});
        rows=$Rows;evidenceLeaf=$leaf}
    if($null -ne $reevaluation) { $binding['reevaluation']=$reevaluation }
    foreach($pair in @(@('resultSha256','runtime-result.json'),@('gameResultSha256','runtime-game-result.json'),
        @('requestSha256','runtime-request.json'),@('orchestrationSha256','orchestration.json'),@('evidenceSha256',$leaf))) {
        $binding[$pair[0]]=(Get-FileHash -LiteralPath (Join-Path $root $pair[1]) -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $binding['transactionSha256']=(Get-FileHash -LiteralPath (Join-Path $LabRoot ('runtime-state/run-transactions/'+$RunId+'.json')) -Algorithm SHA256).Hash.ToLowerInvariant()
    if($result.scenario -cin @('mounted-mammoth-primary-hit-tb','chunk4-area-cleanup') -or (Test-KmcChunk6aFoundationScenario ([string]$result.scenario))) {
        $binding['artifactManifestSha256']=(Get-FileHash -LiteralPath (Join-Path $root 'runtime-artifacts.json')).Hash.ToLowerInvariant()
    }
    if($result.scenario -cin @('chunk6a-mammoth-mount-rt','chunk6a-mammoth-mount-tb')) {
        $binding['artifactManifestSha256']=(Get-FileHash -LiteralPath (Join-Path $root 'runtime-artifacts.json')).Hash.ToLowerInvariant()
        $binding['profileSha256']=(Get-FileHash -LiteralPath (Join-Path $root 'chunk6a-native-mammoth-profile.json')).Hash.ToLowerInvariant()
    }
    [pscustomobject]$binding
}
function Assert-KmcIsolatedScenarioRows([string]$Id,$Binding) {
    $requirements=@{
        'CM08-mounted-mammoth-primary-hit-tb'=@('mounted-mammoth-primary-hit-tb','mounted-mammoth-primary-hit-tb')
        'CM01-mammoth-rt'=@('chunk6a-mammoth-mount-rt','CM01-combat-mount-accepted','CM02-approach-arrival')
        'CM01-mammoth-tb'=@('chunk6a-mammoth-mount-tb','CM01-combat-mount-accepted','CM02-approach-arrival','CM01-combat-mount-preparing-refused')
        'CM04-command-replacement'=@('chunk6a-command-replacement','CM04-command-replacement')
        'CM06-repeated-request'=@('chunk6a-repeated-mount-request','CM06-repeated-request')
        'CM02-ownership-change'=@('chunk6a-ownership-change','CM02-ownership-change')
        'CM02-size-form-change'=@('chunk6a-size-form-change','CM02-size-form-change')
        'CM02-lost-direct-control'=@('chunk6a-lost-direct-control','CM02-lost-direct-control')
        'CM02-rider-incapacitated'=@('chunk6a-rider-incapacitated','CM02-rider-incapacitated')
        'CM02-mount-incapacitated'=@('chunk6a-mount-incapacitated','CM02-mount-incapacitated')
        'CM04-stop-during-approach'=@('chunk6a-stop-approach','CM04-stop-during-approach')
        'CM06-hotbar-path'=@('chunk6a-hotbar-approach','CM06-hotbar-path')
        'CM03-rider-before-mount-slot'=@('chunk6a-allocation-rider-first-tb','CM03-rider-before-mount-slot','CM03-next-round-activation')
        'CM03-mount-slot-before-rider'=@('chunk6a-allocation-mount-first-tb','CM03-mount-slot-before-rider','CM03-next-round-activation')
        'CM06-paused-queue'=@('chunk6a-paused-queue','CM06-paused-queue')
        'CM06-combat-mount-requires-qualified-paired-policy'=@('chunk6a-refused-policy-disabled','CM06-combat-mount-requires-qualified-paired-policy')
        'CM02-foreign-companion'=@('chunk6a-refused-foreign-companion','CM02-foreign-companion')
        'CM02-wrong-creature-target'=@('chunk6a-refused-wrong-creature-target','CM02-wrong-creature-target')
        'CM06-mount-selected'=@('chunk6a-refused-mount-selected','CM06-mount-selected')
        'CM06-multiple-selection'=@('chunk6a-refused-multiple-selection','CM06-multiple-selection')
        'CM03-mount-spent-move'=@('chunk6a-mount-spent-move-tb','CM03-mount-spent-move')
        'CM03-mount-spent-standard'=@('chunk6a-mount-spent-standard-tb','CM03-mount-spent-standard')
        'CM03-mount-spent-all'=@('chunk6a-mount-spent-all-tb','CM03-mount-spent-all')
        'CM03-rider-without-move'=@('chunk6a-rider-without-move-tb','CM03-rider-without-move')
        'CM03-rider-other-action'=@('chunk6a-rider-other-action-tb','CM03-rider-other-action')
        'CM03-unrelated-candidate-between'=@('chunk6a-unrelated-candidate-between-tb','CM03-unrelated-candidate-between')
        'CM05-after-rider-expenditure'=@('chunk6a-dismount-after-rider-expenditure-tb','CM05-after-rider-expenditure')
        'CM05-after-mount-expenditure'=@('chunk6a-dismount-after-mount-expenditure-tb','CM05-after-mount-expenditure')
        'CM05-immediately-after-mount'=@('chunk6a-dismount-immediately-after-mount-tb','CM05-immediately-after-mount')
        'CM04-combat-end'=@('chunk6a-combat-end-approach','CM04-combat-end')
        'CM04-disable-unload'=@('chunk6a-disable-approach','CM04-disable-unload')
        'CM07-mount-save-rt'=@('persistence-p04-save','P04-save-combat-mount-rt')
        'CM07-mount-load-rt'=@('persistence-p04-load','P04-load-combat-mount-rt')
        'CM07-dismount-save'=@('persistence-p04-save','P04-save-combat-dismount-rt')
        'CM07-dismount-load'=@('persistence-p04-load','P04-load-combat-dismount-rt')
        'CM07-mount-save-tb'=@('persistence-p02-save','P02-save-combat-mount-tb')
        'CM07-mount-load-tb'=@('persistence-p02-load','P02-load-combat-mount-tb')
    }
    if(-not $requirements.ContainsKey($Id)){return $false}
    $required=$requirements[$Id]
    if($Binding.scenario -cne $required[0]){throw 'Isolated qualification requires its exact native scenario.'}
    foreach($row in @($required|Select-Object -Skip 1)) {
        if(@($Binding.rows|Where-Object {$_ -ceq $row}).Count -ne 1){throw 'Isolated qualification omitted its mandatory native row.'}
    }
    return $true
}
function Assert-KmcIsolatedQualification([string]$Id,$Binding,[string]$LabRoot) {
    if(-not (Assert-KmcIsolatedScenarioRows $Id $Binding)){return}
    if($Id -ceq 'CM08-mounted-mammoth-primary-hit-tb') {
        $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
        $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
        $game=Get-KmcBoundJson (Join-Path $root 'runtime-game-result.json') $Binding.gameResultSha256
        $result=Get-KmcBoundJson (Join-Path $root 'runtime-result.json') $Binding.resultSha256
        $null=Get-KmcLegacyCombatProjection $Binding $request $game $result $root
        return
    }
    if($Id -clike 'CM07-*') {
        # The complete persistence validator already ran inside the supporting-run projection;
        # the isolated step binds the id to its exact scenario and checkpoint.
        Assert-KmcChunk6aFoundationIsolated $Id $Binding $LabRoot
        return
    }
    . (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
    $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
    $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
    $artifact=Get-KmcBoundJson (Join-Path $root $Binding.evidenceLeaf) $Binding.evidenceSha256
    if([IO.Path]::GetFullPath($root).TrimEnd('\') -cne [IO.Path]::GetFullPath($request.evidenceRoot).TrimEnd('\')){throw 'Isolated qualification evidence root differs.'}
    Assert-KmcChunk6aCombatMountEvidence $request $artifact 'PASS'
    if($Id -cin @('CM01-mammoth-rt','CM01-mammoth-tb')) {
        Assert-KmcBoundMammothProfile $Binding $request $root
    }
}

function Assert-KmcBoundMammothProfile($Binding,$Request,[string]$Root) {
    $manifest=Get-KmcBoundJson (Join-Path $Root 'runtime-artifacts.json') $Binding.artifactManifestSha256
    $null=Get-KmcBoundJson (Join-Path $Root 'chunk6a-native-mammoth-profile.json') $Binding.profileSha256
    Assert-KmcNativeMammothArtifact $Request $manifest 'PASS'
}

# The four CM08 set rows are strict composite gates: every member id must itself be a PASS
# entry on the same frozen payload, and the persistence/removal sets additionally require the
# current Chunk 5 persistence machinery on that payload (its own ledger, checker and, for the
# persistence suite, its completion gate). They are never new gameplay scenarios.
function Get-KmcChunk6aCompositeSets {
    @(
        [pscustomobject]@{id='CM08-horse-smoke';members=@('CM01-horse-rt','CM01-horse-tb','CM05-rt','CM05-tb','CM06-hotbar-path','CM06-pointer-target');chunk5Completion=$false;chunk5Entries=@()}
        [pscustomobject]@{id='CM08-mammoth-smoke';members=@('CM01-mammoth-rt','CM01-mammoth-tb','CM08-mounted-mammoth-primary-hit-tb');chunk5Completion=$false;chunk5Entries=@()}
        [pscustomobject]@{id='CM08-persistence-suite';members=@('CM07-mount-save-rt','CM07-mount-load-rt','CM07-mount-save-tb','CM07-mount-load-tb','CM07-dismount-save','CM07-dismount-load','CM07-cold-load','CM07-save-slot-routes','CM07-unsettled-save-deferred','CM07-area-reload','CM07-schema-unchanged');chunk5Completion=$true;chunk5Entries=@()}
        [pscustomobject]@{id='CM08-disable-removal-readiness';members=@('CM05-dismount-survives-feature-policy-disable','CM04-disable-unload');chunk5Completion=$false;chunk5Entries=@('P07-disable-reenable','P07-prepare-removal','P07-absent-kmc','P07-removal-no-dll')}
    )
}
function Get-KmcChunk6aCompositeSet([string]$Id) { $found=@(Get-KmcChunk6aCompositeSets | Where-Object { $_.id -ceq $Id }); if($found.Count -eq 1) { $found[0] } else { $null } }
function Test-KmcChunk6aCompositeId([string]$Id) { $null -ne (Get-KmcChunk6aCompositeSet $Id) }
# A composite PASS entry: compositeOf names exactly the declared member set, every member is a
# PASS entry of the same ledger (same frozen payload), and when the set binds the Chunk 5
# machinery the bound Chunk 5 ledger is byte-exact, executes the same product payload, passes
# the Chunk 5 checker (with -Completion where required) and carries every required P07 entry
# as PASS. The disable/removal set thereby binds the current removal/no-DLL readiness, the
# re-enable readiness and the restored human installation through the Chunk 5 bindings.
function Assert-KmcChunk6aCompositeEntry([string]$Id,$Entry,$Ledger,[string]$LabRoot,[string]$RepoRoot) {
    $set=Get-KmcChunk6aCompositeSet $Id
    if($null -eq $set) { throw "Composite qualification requested for a non-composite id: $Id" }
    $declared=@($Entry.compositeOf)
    if($declared.Count -ne @($set.members).Count -or @($declared | Select-Object -Unique).Count -ne $declared.Count) { throw "Composite entry $Id does not declare its exact member set." }
    foreach($member in $set.members) {
        if($member -cnotin $declared) { throw "Composite entry $Id omits member $member." }
        $memberEntries=@($Ledger.entries | Where-Object { [string]$_.id -ceq $member })
        if($memberEntries.Count -ne 1 -or [string]$memberEntries[0].status -cne 'PASS') { throw "Composite entry $Id requires member $member to be PASS on the same frozen payload." }
    }
    foreach($member in $declared) { if($member -cnotin @($set.members)) { throw "Composite entry $Id names a foreign member $member." } }
    $needsChunk5=$set.chunk5Completion -or @($set.chunk5Entries).Count -gt 0
    $binding=if($null -ne $Entry.PSObject.Properties['chunk5Ledger']) { $Entry.chunk5Ledger } else { $null }
    if(-not $needsChunk5) { if($null -ne $binding) { throw "Composite entry $Id binds Chunk 5 evidence it does not require." }; return }
    if($null -eq $binding) { throw "Composite entry $Id requires its current Chunk 5 persistence ledger binding." }
    $path=[string]$binding.path
    if([string]::IsNullOrEmpty($path) -or $path -cmatch '(^|[\\/])\.\.([\\/]|$)') { throw "Composite entry $Id names an invalid Chunk 5 ledger path." }
    $full=if([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $LabRoot $path }
    $chunk5=Get-KmcBoundJson $full ([string]$binding.sha256)
    $payload=$Ledger.payload
    foreach($field in @('commit','dllSha256','dllMvid','version','packageSha256')) {
        if([string]$chunk5.payload.$field -cne [string]$payload.$field) { throw "Composite entry $($Id): the bound Chunk 5 ledger executes another product payload ($field)." }
    }
    $checker=Join-Path (Join-Path $RepoRoot 'scripts') 'Test-Chunk5Ledger.ps1'
    $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',$checker,'-LedgerPath',$full,'-LabRoot',$LabRoot)
    if($set.chunk5Completion) { $arguments+='-Completion' }
    $null=& powershell.exe @arguments
    if($LASTEXITCODE -ne 0) { throw "Composite entry $($Id): the bound Chunk 5 ledger did not pass its checker"+$(if($set.chunk5Completion){' with the completion gate'}else{''})+'.' }
    foreach($required in @($set.chunk5Entries)) {
        $found=@($chunk5.entries | Where-Object { [string]$_.id -ceq $required })
        if($found.Count -ne 1 -or [string]$found[0].status -cne 'PASS') { throw "Composite entry $($Id): Chunk 5 entry $required is not PASS on the current payload." }
    }
}

# Each combined claim requires two independent restored transactions on the frozen payload.
function Get-KmcChunk6aAdditionalRoles([string]$Id) {
 if($Id -cin @('CM03-next-round-activation','CM03-early-end-turn')) {
  foreach($order in @('rider-first','mount-first')) {
   [pscustomobject]@{name=$order;scenario=('chunk6a-allocation-'+$order+'-tb');rows=@('CM03-next-round-activation',$(if($order-ceq'rider-first'){'CM03-rider-before-mount-slot'}else{'CM03-mount-slot-before-rider'}))+$(if($Id-ceq'CM03-early-end-turn'){@('CM03-early-end-turn')}else{@()})}
  }
 } elseif($Id -ceq 'CM06-ai-auto-use') {
  foreach($case in @('mount','dismount')) {[pscustomobject]@{name=('auto-use-'+$case);scenario=('chunk6a-auto-use-'+$case+'-rt');rows=@($Id)}}
 } elseif($Id -ceq 'CM05-dismount-survives-feature-policy-disable') {
  foreach($case in @('feature','policy')) {[pscustomobject]@{name=$case;scenario=('chunk6a-dismount-'+$case+'-disabled-rt');rows=@($Id)}}
 } elseif($Id -ceq 'CM05-forced-detach') {
  foreach($case in @(@('rider-death','chunk4-rider-death-tb'),@('mount-death','chunk4-mount-death-tb'),@('rider-incapacitation','chunk4-rider-incapacitation-tb'))) {[pscustomobject]@{name=$case[0];scenario=$case[1];rows=@($Id)}}
 }
}
function Assert-KmcChunk6aAdditionalBindings([string]$Id,$Primary,$Bindings) {
 $roles=@(Get-KmcChunk6aAdditionalRoles $Id);if($roles.Count-eq0){return}
 $items=@($Bindings)
 if($items.Count-ne$roles.Count-or@($items|ForEach-Object runId|Select-Object -Unique).Count-ne$roles.Count){throw 'Additional qualification needs every fresh native allocation'}
 foreach($role in $roles){
  $found=@($items|Where-Object role -CEQ $role.name)
  if($found.Count-ne1-or$found[0].scenario-cne$role.scenario){throw 'Additional qualification role/scenario differs'}
  foreach($row in $role.rows){if(@($found[0].rows|Where-Object {$_-ceq$row}).Count-ne1){throw 'Additional qualification exact row missing'}}
 }
 $primaryRun=@($items|Where-Object runId -CEQ $Primary.runId)
 if($primaryRun.Count-ne1-or$primaryRun[0].scenario-cne$Primary.scenario-or$primaryRun[0].evidenceSha256-cne$Primary.evidenceSha256){throw 'Additional primary does not bind its supporting transaction'}
 if(@($Primary.rows|Where-Object {$_-ceq$Id}).Count-ne1){throw 'Additional primary omits its exact claim row'}
}
function Assert-KmcChunk6aAdditionalQualification([string]$Id,$Payload,$Primary,$Bindings,[string]$LabRoot) {
 $roles=@(Get-KmcChunk6aAdditionalRoles $Id);if($roles.Count-eq0){return}
 Assert-KmcChunk6aAdditionalBindings $Id $Primary $Bindings
 Assert-KmcCompositeRuns $Payload $Bindings $roles $LabRoot
 . (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
 foreach($binding in $Bindings){
  $root=Join-Path $LabRoot ('runtime-evidence/'+$binding.runId)
  $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $binding.requestSha256
  $artifact=Get-KmcBoundJson (Join-Path $root $binding.evidenceLeaf) $binding.evidenceSha256
  if([IO.Path]::GetFullPath($root).TrimEnd('\')-cne[IO.Path]::GetFullPath($request.evidenceRoot).TrimEnd('\')){throw 'Additional evidence root differs'}
  if(Test-KmcChunk4CoreScenario ([string]$request.scenario)){
   if($Id-cne'CM05-forced-detach'-or$artifact.schemaVersion-ne32){throw 'Additional native life qualification requires the schema-32 forced-detach artifact'}
   Assert-KmcChunk4CoreEvidence $request $artifact 'PASS'
  } else {
   Assert-KmcChunk6aCombatMountEvidence $request $artifact 'PASS'
  }
 }
}
