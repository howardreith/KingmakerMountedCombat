# Read only row projection shared by PASS and retained FAIL paths in the future ledger.
# Callers first bind raw artifact SHA and the actual native/overall payload.
function Get-KmcChunk6aArtifactRows([string]$Path,[string]$Scenario,$Game,[switch]$PassRowsOnly) {
    if($Scenario-ceq'chunk4-area-cleanup') {Get-KmcAreaRegressionRows $Path $Game;return}
    # A foundation persistence run is projected from its JSONL leaf; a retained foundation failure binds the
    # failing facet's JSON leaf (runtime-game-result.json or runtime-result.json), which the generic reader projects.
    if((Test-KmcChunk6aFoundationScenario $Scenario) -and [IO.Path]::GetFileName($Path) -ceq 'persistence-observations.jsonl') {Get-KmcChunk6aFoundationRows $Path $Scenario $Game;return}
    if([IO.Path]::GetExtension($Path) -ceq '.jsonl') {
        if([IO.Path]::GetFileName($Path) -cne 'combat-scenario-evidence.jsonl' -or $Scenario -cne 'mounted-mammoth-primary-hit-tb' -or [string]$Game.scenario -cne $Scenario) {throw 'Legacy row reader requires the fixed Mammoth scenario and JSONL leaf.'}
        $lines=@(Get-Content -LiteralPath $Path|Where-Object {-not[string]::IsNullOrWhiteSpace([string]$_)})
        if($lines.Count -ne 1){throw 'Legacy row reader requires one native record.'}
        Assert-KmcJsonObjectMembersUnique $lines[0] 'legacy bound row'
        $record=$lines[0]|ConvertFrom-Json
        foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
            if([string]$record.$field -cne [string]$Game.$field){throw ('Legacy bound row identity differs: '+$field)}
        }
        if(-not(Test-KmcExactJsonInteger $record.schemaVersion) -or [int]$record.schemaVersion -ne 57 -or
           [string]$record.artifactKind -cne 'combat-scenario-evidence' -or [string]$record.row -cne $Scenario -or
           [string]$record.status -cnotin @('PASS','FAIL') -or
           -not(Test-KmcExactJsonInteger $record.assertionPassCount) -or -not(Test-KmcExactJsonInteger $record.assertionFailCount) -or
           [int]$record.assertionPassCount -lt 0 -or [int]$record.assertionFailCount -lt 0 -or $record.errors -isnot [Array]) {throw 'Legacy bound row shape differs.'}
        [pscustomobject]@{name=$record.row;status=$record.status;assertionPassCount=$record.assertionPassCount;assertionFailCount=$record.assertionFailCount;errors=$record.errors}
        return
    }
    if([IO.Path]::GetExtension($Path) -cne '.json'){throw 'Unsupported evidence row artifact.'}
    $artifact=Get-Content -LiteralPath $Path -Raw|ConvertFrom-Json
    foreach($name in $(if($PassRowsOnly){@('rows')}else{@('rows','subscenarioResults')})) {
        if(@($artifact.PSObject.Properties.Name) -ccontains $name){foreach($row in @($artifact.$name)){$row}}
    }
}
