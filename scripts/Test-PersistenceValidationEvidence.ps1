[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedGameResultSha256
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceValidationFixtures.ps1')
$root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$RunId)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'P06 regression evidence'
Assert-KmcDirectoryTreeCloneable $root 'P06 regression evidence'
$gamePath=Join-Path $root 'runtime-game-result.json'
if((Get-KmcSha256 $gamePath)-cne$ExpectedGameResultSha256){throw 'Exact native regression evidence differs.'}
$game=Read-KmcJson $gamePath
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
if($game.scenario-cne'persistence-p06-load'-or$game.status-cne'PASS'-or$request.persistenceCase-cne'future'){throw 'Expected exact native future refusal regression.'}
# Exercise the real result services against immutable historical evidence.
# Full process/candidate admission remains the fresh native orchestrator's job.
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/Test-RuntimeGameResult.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count){throw 'Runtime result validator has syntax errors.'}
foreach($definition in @($ast.EndBlock.Statements|Where-Object {$_-is[Management.Automation.Language.FunctionDefinitionAst]})){
    . ([ScriptBlock]::Create($definition.Extent.Text))
}
Assert-SubscenarioResults $game
$rows=@(Get-Content -LiteralPath (Join-Path $root 'persistence-observations.jsonl')|ForEach-Object{$_|ConvertFrom-Json})
Assert-KmcValidationPersistenceEvidence $request $rows $game
$final=Read-KmcJson (Join-Path $root 'runtime-result.json')
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/Test-RuntimeResult.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count){throw 'Final result validator has syntax errors.'}
foreach($definition in @($ast.EndBlock.Statements|Where-Object {$_-is[Management.Automation.Language.FunctionDefinitionAst]})){
    . ([ScriptBlock]::Create($definition.Extent.Text))
}
Assert-SubscenarioResults $final
$passes=3
foreach($case in @('missing-entry','world-refresh','wrong-archive','no-attack')){
    $copy=($rows|ConvertTo-Json -Depth 32)|ConvertFrom-Json
    switch($case){
        'missing-entry' {$copy=@($copy|Where-Object {$_.kind-cne'validation-native-load-refused'-or$_.detail.entry-ne2})}
        'world-refresh' {($copy|Where-Object kind -CEQ 'validation-native-load-refused'|Select-Object -First 1).persistence.semantics=4}
        'wrong-archive' {($copy|Where-Object kind -CEQ 'validation-native-load-refused'|Select-Object -First 1).detail.sha256='a'*64}
        'no-attack' {($copy|Where-Object kind -CEQ 'attack-delivered').detail.rules=0}
    }
    $rejected=$false
    try{Assert-KmcValidationPersistenceEvidence $request $copy $game}catch{$rejected=$true}
    if(-not$rejected){throw ('Native P06 validator accepted '+$case)}
    $passes++
}
if((Get-KmcSha256 $gamePath)-cne$ExpectedGameResultSha256){throw 'Regression validation changed source evidence.'}
Write-Output ("P06 NATIVE RESULT REGRESSION PASS="+$passes+" FAIL=0; original outer failure remains unchanged.")
