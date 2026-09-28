# Future reader only. Caller retains payload, suite, overall/native and restoration checks.
function Get-KmcLegacyCombatProjection($Binding,$Request,$Game,$Result,[string]$Root) {
    if([string]$Binding.scenario -cne 'mounted-mammoth-primary-hit-tb' -or
       [string]$Binding.evidenceLeaf -cne 'combat-scenario-evidence.jsonl' -or
       @($Binding.rows).Count -ne 1 -or [string]$Binding.rows[0] -cne 'mounted-mammoth-primary-hit-tb') { throw 'Legacy Mammoth requires its exact scenario, JSONL leaf and row.' }
    $expectedRoot=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    if([IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\') -cne $expectedRoot) { throw 'Legacy request evidence root differs.' }
    foreach($value in @($Request,$Game,$Result)) {
        if([string]$value.runId -cne [string]$Binding.runId -or [string]$value.scenario -cne [string]$Binding.scenario) {throw 'Legacy native or overall run identity differs.'}
    }
    foreach($value in @($Game,$Result)) {
        if([string]$value.status -cne 'PASS' -or [int]$value.assertionFailCount -ne 0 -or @($value.errors).Count -ne 0) {throw 'Legacy native and overall results must both PASS.'}
        if([string]$value.evidenceManifestSha256 -cne [string]$Binding.artifactManifestSha256) {throw 'Legacy result manifest binding differs.'}
    }
    # Uses the unchanged read-only manifest validator from Test-RuntimeResult.
    Assert-KmcReadOnlyArtifactManifest -Request $Request -ExpectedSha256 $Binding.artifactManifestSha256
    $manifest=Get-KmcBoundJson (Join-Path $Root 'runtime-artifacts.json') $Binding.artifactManifestSha256
    $leaves=@($manifest.artifacts|Where-Object relativePath -CEQ 'combat-scenario-evidence.jsonl')
    if($leaves.Count -ne 1 -or [string]$leaves[0].sha256 -cne [string]$Binding.evidenceSha256) {throw 'Legacy artifact hash differs from its bound manifest.'}
    Assert-KmcCombatScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $Game.subscenarioResults
    Assert-KmcCombatScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $Result.subscenarioResults
    $record=Get-KmcBoundJson (Join-Path $Root $Binding.evidenceLeaf) $Binding.evidenceSha256
    if([string]$record.row -cne 'mounted-mammoth-primary-hit-tb' -or [string]$record.status -cne 'PASS' -or
       [int]$record.assertionFailCount -ne 0 -or [int]$record.assertionPassCount -ne [int]$Binding.passCount) {throw 'Legacy row or assertion totals differ.'}
    # Normalize only in memory. The source JSONL, manifest and all original hashes remain untouched.
    $projection=[ordered]@{}
    foreach($name in @('runId','scenario','commit','branch','productVersion','dllSha256','dllMvid','status','errors')) {$projection[$name]=$record.$name}
    $projection.rows=@([pscustomobject]@{name=$record.row;status=$record.status;assertionPassCount=$record.assertionPassCount;assertionFailCount=$record.assertionFailCount;errors=$record.errors})
    [pscustomobject]$projection
}
