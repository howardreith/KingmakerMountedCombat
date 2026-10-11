$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk6aPrimaryClaimEvidence.ps1')
$checks=0
foreach($spec in @(@('RuntimeHarness.Common.ps1',@('Get-KmcPhase3dHorseRuntimeRows')),@('Chunk6aSupportingEvidence.ps1',@('Assert-KmcIsolatedScenarioRows')),@('Chunk6aPrimaryClaimEvidence.ps1',@('Get-KmcChunk6aPrimaryContracts','Assert-KmcChunk6aPrimaryClaim')))){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot ('runtime/'+$spec[0])),[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw $errors[0]};$checks++
 foreach($name in $spec[1]){
  $defs=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true))
  if($defs.Count-ne1){throw 'Missing unique function'};$checks++
  . ([scriptblock]::Create($defs[0].Extent.Text))
 }
}
foreach($row in @('CM04-command-replacement','chunk6a-command-replacement')){
 if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$row}).Count-ne1){throw 'Exact runtime row missing'};$checks++
}
$b=[pscustomobject]@{scenario='chunk6a-command-replacement';rows=@('CM04-command-replacement');evidenceLeaf='phase3d-horse-scenario-evidence.json'}
if(-not(Assert-KmcIsolatedScenarioRows 'CM04-command-replacement' $b)){throw 'Exact isolated mapping missing'};$checks++
Assert-KmcChunk6aPrimaryClaim 'CM04-command-replacement' $b;$checks++
foreach($field in @('scenario','rows','evidenceLeaf')){
 $c=$b|ConvertTo-Json|ConvertFrom-Json
 if($field-ceq'rows'){$c.rows=@('CM04-stop-during-approach')}else{$c.$field='borrowed'}
 $refused=$false;try{Assert-KmcChunk6aPrimaryClaim 'CM04-command-replacement' $c}catch{$refused=$true}
 if(-not$refused){throw 'Borrowed primary accepted'};$checks++
}

Write-Host ("REPLACEMENT BINDINGS PASS="+$checks+" FAIL=0")
