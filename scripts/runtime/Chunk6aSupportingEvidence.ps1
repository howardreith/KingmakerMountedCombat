. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

. (Join-Path $PSScriptRoot 'LegacyCombatProjectionEvidence.ps1')
# Fixed same-candidate supporting-run contract for Chunk 6A qualification.
# Original KMC ledger validation; reads only project-owned settled evidence.
Set-StrictMode -Version Latest
function Get-KmcBoundJson([string]$Path,[string]$ExpectedSha256) {
    if($ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Evidence requires an exact SHA-256.' }
    $actual=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if($actual -cne $ExpectedSha256) { throw "Bound evidence bytes differ: $Path" }
    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}
function Assert-KmcSupportingRun($Payload,$Binding,[string]$LabRoot) {
    $run=[string]$Binding.runId
    if($run -cnotmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Invalid supporting run ID.' }
    $legacy=[string]$Binding.scenario -ceq 'mounted-mammoth-primary-hit-tb'
    if($legacy) {
        if([string]$Binding.evidenceLeaf -cne 'combat-scenario-evidence.jsonl' -or @($Binding.rows).Count -ne 1 -or [string]$Binding.rows[0] -cne 'mounted-mammoth-primary-hit-tb') { throw 'Legacy Mammoth requires its exact scenario, JSONL leaf and row.' }
    } elseif([string]$Binding.evidenceLeaf -cnotmatch '^[A-Za-z0-9._-]+\.json$') { throw 'Evidence must name one JSON leaf.' }
    $root=Join-Path $LabRoot ('runtime-evidence/'+$run)
    $result=Get-KmcBoundJson (Join-Path $root 'runtime-result.json') $Binding.resultSha256
    $game=Get-KmcBoundJson (Join-Path $root 'runtime-game-result.json') $Binding.gameResultSha256
    $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
    $orchestration=Get-KmcBoundJson (Join-Path $root 'orchestration.json') $Binding.orchestrationSha256
    $transaction=Get-KmcBoundJson (Join-Path $LabRoot ('runtime-state/run-transactions/'+$run+'.json')) $Binding.transactionSha256
    $artifact=if($legacy){Get-KmcLegacyCombatProjection $Binding $request $game $result $root}else{Get-KmcBoundJson (Join-Path $root $Binding.evidenceLeaf) $Binding.evidenceSha256}
    foreach($item in @($result,$game,$request)) {
        if([string]$item.runId -cne $run -or [string]$item.scenario -cne [string]$Binding.scenario) { throw 'Supporting run or scenario differs.' }
        if([string]$item.commit -cne [string]$Payload.commit -or [string]$item.branch -cne [string]$Payload.branch -or
           [string]$item.productVersion -cne [string]$Payload.version -or [string]$item.dllSha256 -cne [string]$Payload.dllSha256 -or
           [string]$item.dllMvid -cne [string]$Payload.dllMvid) { throw 'Supporting run executed another payload.' }
        if([string]$item.transactionToken -cne [string]$transaction.token) { throw 'Supporting transaction token differs.' }
    }
    foreach($item in @($result,$game)) {
        if([string]$item.status -cne 'PASS' -or [int]$item.assertionFailCount -ne 0 -or [int]$item.assertionPassCount -lt 1 -or
           [int]$item.assertionPassCount -ne [int]$Binding.passCount -or [int]$Binding.failCount -ne 0 -or @($item.errors).Count -ne 0) {
            throw 'Supporting native or overall run is not an exact PASS.'
        }
    }
    if([string]$result.gameResultSha256 -cne [string]$Binding.gameResultSha256) { throw 'Overall result does not bind the native result.' }
    if([string]$request.qualificationSuite.suiteId -cne [string]$Payload.suiteId -or
       [string]$request.qualificationSuite.snapshotSha256 -cne [string]$Payload.suiteSha256 -or
       [string]$transaction.qualificationSuiteId -cne [string]$Payload.suiteId -or
       [string]$transaction.qualificationSuiteSnapshotSha256 -cne [string]$Payload.suiteSha256) { throw 'Supporting run used another qualification suite.' }
    if([string]$orchestration.runId -cne $run -or [string]$orchestration.scenario -cne [string]$Binding.scenario -or
       [string]$orchestration.status -cne 'PASS' -or [string]$orchestration.stage -cne 'restored' -or
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
    $runIds=@($items | ForEach-Object { [string]$_.runId })
    if(@($runIds | Select-Object -Unique).Count -ne $items.Count) { throw 'Composite roles require separate native transactions.' }
    foreach($role in $roles) {
        $matches=@($items | Where-Object { [string]$_.role -ceq [string]$role.name })
        if($matches.Count -ne 1) { throw "Composite evidence needs exactly one $role run." }
        if([string]$matches[0].scenario -cne [string]$role.scenario) { throw 'Supporting role requires its declared scenario.' }
        foreach($requiredRow in @($role.rows)) {
            if(@($matches[0].rows) -cnotcontains [string]$requiredRow) { throw 'Supporting role omits a required row.' }
        }
        Assert-KmcSupportingRun $Payload $matches[0] $LabRoot
    }
    Assert-KmcCompositeRunOrder $items $roles $LabRoot
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
function Assert-KmcCompositeRunOrder($Bindings,$roles,[string]$LabRoot) {
    $previousEnd=[DateTimeOffset]::MinValue
    foreach($role in $roles) {
        $binding=@($Bindings|Where-Object role -CEQ $role.name)[0]
        $transaction=Get-KmcBoundJson (Join-Path $LabRoot ('runtime-state/run-transactions/'+$binding.runId+'.json')) $binding.transactionSha256
        $start=[DateTimeOffset]$transaction.preparedAtUtc
        $end=[DateTimeOffset]$transaction.restoredAtUtc
        if($start -le $previousEnd -or $end -le $start) { throw 'Campaign roles did not run in order as separate restored transactions.' }
        $previousEnd=$end
    }
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
    $leaf=if($result.scenario -ceq 'mounted-mammoth-primary-hit-tb'){'combat-scenario-evidence.jsonl'}else{'phase3d-horse-scenario-evidence.json'}
    $binding=[ordered]@{role=$Role;runId=$RunId;scenario=$result.scenario;passCount=$result.assertionPassCount;failCount=$result.assertionFailCount;
        rows=$Rows;evidenceLeaf=$leaf}
    foreach($pair in @(@('resultSha256','runtime-result.json'),@('gameResultSha256','runtime-game-result.json'),
        @('requestSha256','runtime-request.json'),@('orchestrationSha256','orchestration.json'),@('evidenceSha256',$leaf))) {
        $binding[$pair[0]]=(Get-FileHash -LiteralPath (Join-Path $root $pair[1]) -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $binding['transactionSha256']=(Get-FileHash -LiteralPath (Join-Path $LabRoot ('runtime-state/run-transactions/'+$RunId+'.json')) -Algorithm SHA256).Hash.ToLowerInvariant()
    if($result.scenario -ceq 'mounted-mammoth-primary-hit-tb') {
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
        'CM04-stop-during-approach'=@('chunk6a-stop-approach','CM04-stop-during-approach')
        'CM06-hotbar-path'=@('chunk6a-hotbar-approach','CM06-hotbar-path')
        'CM03-rider-before-mount-slot'=@('chunk6a-allocation-rider-first-tb','CM03-rider-before-mount-slot','CM03-next-round-activation')
        'CM03-mount-slot-before-rider'=@('chunk6a-allocation-mount-first-tb','CM03-mount-slot-before-rider','CM03-next-round-activation')
        'CM06-paused-queue'=@('chunk6a-paused-queue','CM06-paused-queue')
        'CM02-foreign-companion'=@('chunk6a-refused-foreign-companion','CM02-foreign-companion')
        'CM02-wrong-creature-target'=@('chunk6a-refused-wrong-creature-target','CM02-wrong-creature-target')
        'CM06-mount-selected'=@('chunk6a-refused-mount-selected','CM06-mount-selected')
        'CM06-multiple-selection'=@('chunk6a-refused-multiple-selection','CM06-multiple-selection')
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

# Each combined claim requires two independent restored transactions on the frozen payload.
function Get-KmcChunk6aAdditionalRoles([string]$Id) {
 if($Id -ceq 'CM03-next-round-activation') {
  foreach($order in @('rider-first','mount-first')) {
   [pscustomobject]@{name=$order;scenario=('chunk6a-allocation-'+$order+'-tb');rows=@('CM03-next-round-activation',$(if($order-ceq'rider-first'){'CM03-rider-before-mount-slot'}else{'CM03-mount-slot-before-rider'}))}
  }
 } elseif($Id -ceq 'CM06-ai-auto-use') {
  foreach($case in @('mount','dismount')) {[pscustomobject]@{name=('auto-use-'+$case);scenario=('chunk6a-auto-use-'+$case+'-rt');rows=@($Id)}}
 } elseif($Id -ceq 'CM05-dismount-survives-feature-policy-disable') {
  foreach($case in @('feature','policy')) {[pscustomobject]@{name=$case;scenario=('chunk6a-dismount-'+$case+'-disabled-rt');rows=@($Id)}}
 }
}
function Assert-KmcChunk6aAdditionalBindings([string]$Id,$Primary,$Bindings) {
 $roles=@(Get-KmcChunk6aAdditionalRoles $Id);if($roles.Count-eq0){return}
 $items=@($Bindings)
 if($items.Count-ne2-or@($items|ForEach-Object runId|Select-Object -Unique).Count-ne2){throw 'Additional qualification needs two fresh native allocations'}
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
  Assert-KmcChunk6aCombatMountEvidence $request $artifact 'PASS'
 }
}
