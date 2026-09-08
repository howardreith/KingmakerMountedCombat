$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/PairedMammothEvidence.ps1')
$script:passed=0;$script:failed=0
$script:events=[Collections.ArrayList]::new();$script:sequence=0
$identity='aabbccddeeff00112233445566778899'
function Add-Event($boundary,$actor,$activation,$current='rider') {
    $script:sequence++
    $e=@{sequence=$script:sequence;boundary=$boundary;frame=$script:sequence;gameTicks=$script:sequence*100
        currentActor=$current;activationIdentity=$activation;state=@{actor=$actor}}
    [void]$script:events.Add($e);return $e
}
function Sample($key,$activation,$grant,$standard=0) {
    $e=Add-Event $key 'rider' $activation
    return @{kind=$key;traceSequence=$e.sequence;frame=$e.frame;gameTicks=$e.gameTicks;identity=$activation;currentActor='rider'
        riderPreparations=$grant;mountPreparations=$grant;rider=@{actor='rider';standard=0;move=0;prepared=$true}
        mount=@{actor='mount';standard=$standard;move=0;prepared=$true}}
}
function Prepare($activation) {
    foreach($actor in @('rider','mount')) {foreach($boundary in @('prepare-before','clear-after','round-state-after','prepare-after')) {
        $null=Add-Event $boundary $actor $activation
    }}
}
$before=Sample 'before' $null 0
Prepare ($identity+':1')
$first=Sample 'first' ($identity+':1') 1
$attack=Sample 'attack' ($identity+':1') 1 6
$kmcEndInput=Add-Event 'mammoth-native-end-input' 'rider' ($identity+':1')
foreach($actor in @('rider','mount')) {foreach($boundary in @('turn-end-before','turn-end-after')) {
    $null=Add-Event $boundary $actor ($identity+':1')
}}
$visit=Add-Event 'mammoth-turn-observed' 'friendly' ($identity+':1') 'friendly'
Prepare ($identity+':2')
$last=Sample 'next' ($identity+':2') 2
$record=@{schemaVersion=57;scenario='mounted-mammoth-primary-hit-tb';riderId='rider';mountId='mount'
    pairedActivation=@{level='NATIVE INTEGRATION';passed=$true;outsideCombat=$true;enablePairedActivation=$true
        enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;rider='rider';mount='mount'
        beforeEncounter=$before;firstGrant=$first;afterAttack=$attack;nextGrant=$last
        trace=@{events=@($script:events);dropped=0;observationErrors=0}
        visits=@(@{actor='rider';frame=$first.frame},@{actor='friendly';frame=$visit.frame},@{actor='rider';frame=$last.frame})
        endInputs=@(@{actor='rider';frame=$kmcEndInput.frame;kind='Game.PauseBind'})}}
$json=$record|ConvertTo-Json -Depth 15
function Check($name,[scriptblock]$mutation,$reject) {
    $copy=$json|ConvertFrom-Json
    & $mutation $copy
    $caught=$false
    try {Assert-KmcPairedMammothEvidence $copy} catch {$caught=$true;if(!$reject){Write-Host $_.Exception.Message;Write-Host $_.ScriptStackTrace}}
    if($caught -eq $reject){$script:passed++}else{$script:failed++;Write-Host "FAIL $name"}
}
Check 'valid original envelope only' {} $false
Check 'old scheduler' {param($r)$r.pairedActivation.enablePairedCommandScheduler=$true} $true
Check 'wrong profile scenario' {param($r)$r.scenario='mounted-mammoth-primary-hit-rt'} $true
Check 'detached sample' {param($r)$r.pairedActivation.firstGrant.traceSequence=999} $true
Check 'stale identity' {param($r)$r.pairedActivation.nextGrant.identity=$r.pairedActivation.firstGrant.identity} $true
Check 'refund attack' {param($r)$r.pairedActivation.afterAttack.mount.standard=0} $true
Check 'rider transport cost' {param($r)$r.pairedActivation.afterAttack.rider.move=1} $true
Check 'stale grant' {param($r)$r.pairedActivation.nextGrant.mount.standard=6} $true
Check 'duplicate preparation' {param($r)$r.pairedActivation.trace.events+=@($r.pairedActivation.trace.events|Where-Object {$_.boundary -ceq 'prepare-after'}|Select-Object -First 1)} $true
Check 'missing End' {param($r)$r.pairedActivation.trace.events=@($r.pairedActivation.trace.events|Where-Object {$_.boundary -cne 'turn-end-before'})} $true
Check 'extra mount turn' {param($r)$r.pairedActivation.visits[1].actor='mount'} $true
Check 'unrelated actor absent' {param($r)$r.pairedActivation.visits[1].actor='rider'} $true
Check 'invented native input' {param($r)$r.pairedActivation.endInputs[0].frame=999} $true
Check 'dropped trace' {param($r)$r.pairedActivation.trace.dropped=1} $true
# Reuse the established combat envelope fixture without running the full catalog.
# This exercises schema registration and all existing outer native-cost guards.
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$kmcAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Harness.ps1'),[ref]$null,[ref]$null)
foreach($name in @('New-TestCombatEvidenceRecord','New-TestArtifactManifest','Write-TestCombatEvidence')) {
    $nodes=@($kmcAst.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true))
    if($nodes.Count -ne 1){throw 'Existing combat protocol fixture is not unique'}
    . ([ScriptBlock]::Create($nodes[0].Extent.Text))
}
function Set-ProtocolActorIds($value) {
    if($value -is [Array]) {foreach($item in $value){Set-ProtocolActorIds $item};return}
    if($value -isnot [PSCustomObject]){return}
    foreach($p in $value.PSObject.Properties) {
        if($p.Value -is [string] -and $p.Value -cin @('rider','mount')) {$p.Value='combat-'+$p.Value}
        else {Set-ProtocolActorIds $p.Value}
    }
}
$kmcCase=$json|ConvertFrom-Json
Set-ProtocolActorIds $kmcCase
$kmcParent=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
$kmcRoot=Assert-KmcChildPath (Join-Path $kmcParent ('harness-test-mammoth-'+[Guid]::NewGuid().ToString('N'))) $kmcParent 'Mammoth protocol fixture'
$kmcRequest=[PSCustomObject]@{runId='mammoth-paired-protocol';scenario='mounted-mammoth-primary-hit-tb';evidenceRoot=$kmcRoot
    branch='codex/mounted-combat-phase3f-playable-core';commit=('a'*40);productVersion='0.1.0-paired-preview.25'
    dllSha256=('b'*64);dllMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}
$kmcRecord=New-TestCombatEvidenceRecord $kmcRequest
$kmcRecord.schemaVersion=57;$kmcRecord.Remove('pairedScheduler')
$kmcRecord['pairedActivation']=$kmcCase.pairedActivation
$kmcRecord.turnBased.unifiedMountedTurn=$false
$kmcFullJson=$kmcRecord|ConvertTo-Json -Depth 20
try {
    foreach($caseIndex in 0..3) {
        $kmcRecord=$kmcFullJson|ConvertFrom-Json
        if($caseIndex -eq 1){$kmcRecord.turnBased.unifiedMountedTurn=$true}
        if($caseIndex -eq 2){$kmcRecord.pairedActivation.nextGrant.mount.standard=6}
        if($caseIndex -eq 3){$kmcRecord.resources.riderStandardAfter=6}
        [void](Write-TestCombatEvidence -EvidenceRoot $kmcRoot -Request $kmcRequest -Record $kmcRecord)
        $kmcManifest=Read-KmcJson (Join-Path $kmcRoot 'runtime-artifacts.json')
        $caught=$false
        try {Assert-KmcCombatScenarioEvidence -Request $kmcRequest -Manifest $kmcManifest -Status PASS}
        catch {$caught=$true;if($caseIndex -eq 0){Write-Host $_.Exception.Message}}
        if($caught -eq ($caseIndex -ne 0)){$script:passed++}else{$script:failed++;Write-Host "FAIL outer Mammoth case $caseIndex"}
    }
} finally {
    $kmcVerified=Assert-KmcChildPath ([IO.Path]::GetFullPath($kmcRoot)) $kmcParent 'owned Mammoth protocol fixture cleanup'
    if(Test-Path -LiteralPath $kmcVerified){Remove-Item -LiteralPath $kmcVerified -Recurse -Force}
}
Write-Host "MAMMOTH PAIRED PROTOCOL PASS=$script:passed FAIL=$script:failed; no native gameplay qualification"
if($script:failed -ne 0){exit 1}
