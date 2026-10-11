[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$audit=Get-Content -Raw (Join-Path $repoRoot 'docs/chunk6a-evidence-history.json')|ConvertFrom-Json
function Assert-KmcHistoricalResultFacets($Run,$Game,$Result) {
    if($Result.status -ceq $Game.status){return}
    if($Result.status -cne 'FAIL' -or $Game.status -cne 'PASS' -or $Run.nativeStatus -cne $Game.status -or
        $Run.nativeAssertionPassCount -ne $Game.assertionPassCount -or $Run.nativeAssertionFailCount -ne $Game.assertionFailCount -or
        $Run.externalAssertionPassCount -ne $Result.assertionPassCount -or $Run.externalAssertionFailCount -ne $Result.assertionFailCount) {
        throw 'Historical native/external result facets or counts differ.'
    }
    $failed=@($Result.subscenarioResults|Where-Object status -CEQ 'FAIL')
    if($failed.Count -lt 1 -or @($Run.externalFailures).Count -ne $failed.Count){throw 'Historical external failure omitted.'}
    foreach($row in $failed){
        $saved=@($Run.externalFailures|Where-Object row -CEQ $row.name)
        if($saved.Count -ne 1 -or $saved[0].assertionFailCount -ne $row.assertionFailCount -or
            (ConvertTo-Json @($saved[0].assertions) -Compress) -cne (ConvertTo-Json @($row.errors) -Compress)) {
            throw 'Historical external failing row or assertion changed.'
        }
    }
}
$checks=0
foreach($id in @('c6a-approach-1','c6a-approach-2')) {
    if(@($audit.runs|Where-Object runId -CEQ $id).Count -ne 1){throw "Required immutable run omitted: $id"}
}
foreach($run in $audit.runs){
    foreach($file in $run.artifacts){
        $path=[IO.Path]::GetFullPath((Join-Path $labRoot $file.path))
        if(-not $path.StartsWith((Join-Path $labRoot ('runtime-evidence/'+$run.runId+'/')),[StringComparison]::OrdinalIgnoreCase)){throw 'Evidence path escaped the named run.'}
        if((Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant() -cne $file.sha256){throw "Historical artifact changed: $path"}
        $checks++
    }
    $root=Join-Path $labRoot ('runtime-evidence/'+$run.runId)
    $game=Get-Content -Raw (Join-Path $root 'runtime-game-result.json')|ConvertFrom-Json
    $result=Get-Content -Raw (Join-Path $root 'runtime-result.json')|ConvertFrom-Json
    if($result.status -cne $run.status -or $result.scenario -cne $run.scenario -or $game.commit -cne $run.observedPayload.commit -or
        $game.dllSha256 -cne $run.observedPayload.dllSha256 -or $game.dllMvid -cne $run.observedPayload.dllMvid -or $game.productVersion -cne $run.observedPayload.version){throw 'Historical run or payload identity differs.'}
    Assert-KmcHistoricalResultFacets $run $game $result
    if($result.status -cne $game.status){$checks++}
    if($run.qualificationAccepted -ne $false){throw 'Superseded diagnostic evidence cannot qualify the repaired candidate.'}
    $failed=@()
    if(@($game.PSObject.Properties.Name) -ccontains 'subscenarioResults'){$failed=@($game.subscenarioResults|Where-Object status -CEQ 'FAIL')}
    if($failed.Count -ne @($run.failures).Count){throw 'Historical failed row omitted.'}
    foreach($row in $failed){
        $saved=@($run.failures|Where-Object row -CEQ $row.name)
        if($saved.Count -ne 1 -or $saved[0].assertionFailCount -ne $row.assertionFailCount -or
            (ConvertTo-Json @($saved[0].assertions) -Compress) -cne (ConvertTo-Json @($row.errors) -Compress)){throw 'Historical assertion text or row count changed.'}
        $checks++
    }
    if(@($run.observedPayload.PSObject.Properties.Name) -ccontains 'packagePath'){
        $package=$run.observedPayload.packagePath
        if((Get-FileHash -Algorithm SHA256 -LiteralPath $package).Hash.ToLowerInvariant() -cne $run.observedPayload.packageSha256 -or
            (Get-FileHash -Algorithm SHA256 -LiteralPath ($package+'.manifest.json')).Hash.ToLowerInvariant() -cne $run.observedPayload.manifestSha256){throw 'Historical package or manifest changed.'}
        $checks++
    }
}
# A native PASS cannot erase the original overall FAIL or its external assertion.
$divergent=@($audit.runs|Where-Object runId -CEQ 'c6a-interruption-a-obstruction')
if($divergent.Count -ne 1){throw 'Required external-validator failure omitted.'}
$facetRoot=Join-Path $labRoot 'runtime-evidence/c6a-interruption-a-obstruction'
$facetGame=Get-Content -Raw (Join-Path $facetRoot 'runtime-game-result.json')|ConvertFrom-Json
$facetResult=Get-Content -Raw (Join-Path $facetRoot 'runtime-result.json')|ConvertFrom-Json
foreach($mutation in @('omit-external','change-assertion','change-native-count')){
    $copy=$divergent[0]|ConvertTo-Json -Depth 30|ConvertFrom-Json
    if($mutation -ceq 'omit-external'){$copy.externalFailures=@()}
    elseif($mutation -ceq 'change-assertion'){$copy.externalFailures[0].assertions=@('changed')}
    else{$copy.nativeAssertionPassCount++}
    $rejected=$false
    try{Assert-KmcHistoricalResultFacets $copy $facetGame $facetResult}catch{$rejected=$true}
    if(-not $rejected){throw "History accepted corrupt result facet: $mutation"}
    $checks++
}
Write-Host "CHUNK6A EVIDENCE HISTORY PASS=$checks FAIL=0; original artifacts and assertions preserved"
