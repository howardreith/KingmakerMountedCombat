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
foreach($row in @('CM06-repeated-request','chunk6a-repeated-mount-request')){
 if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$row}).Count-ne1){throw 'Exact runtime row missing'};$checks++
}
$b=[pscustomobject]@{scenario='chunk6a-repeated-mount-request';rows=@('CM06-repeated-request');evidenceLeaf='phase3d-horse-scenario-evidence.json'}
if(-not(Assert-KmcIsolatedScenarioRows 'CM06-repeated-request' $b)){throw 'Exact isolated mapping missing'};$checks++
Assert-KmcChunk6aPrimaryClaim 'CM06-repeated-request' $b;$checks++
foreach($field in @('scenario','rows','evidenceLeaf')){
 $c=$b|ConvertTo-Json|ConvertFrom-Json
 if($field-ceq'rows'){$c.rows=@('CM04-command-replacement')}else{$c.$field='borrowed'}
 $refused=$false;try{Assert-KmcChunk6aPrimaryClaim 'CM06-repeated-request' $c}catch{$refused=$true}
 if(-not$refused){throw 'Borrowed primary accepted'};$checks++
}
$registrations=@(
 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs',
 'src/KingmakerMountedCombat/Diagnostics/HorseCompanionRegistrationScenarioPolicy.cs',
 'scripts/runtime/Test-RuntimeRequest.ps1',
 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1',
 'scripts/runtime/RuntimeHarness.Common.ps1'
)
foreach($relative in $registrations){
 $text=[IO.File]::ReadAllText((Join-Path $repo $relative))
 if($text.IndexOf('chunk6a-repeated-mount-request',[StringComparison]::Ordinal)-lt0){throw "Scenario registration missing: $relative"};$checks++
}
$scenario=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aRepeatedRequestScenario.cs'))
foreach($token in @('EnsureChunk6aRiderSelection("CM06-repeated-request")','ApproachObserved','OwnsUnsettledRelationshipShell(command)','TryNativeAbilityTargetClick(','rejectedBeforeSecondCommand','FinishChunk6aCommandWindow("positive-mount"','CM06-repeated-request')){
 if($scenario.IndexOf($token,[StringComparison]::Ordinal)-lt0){throw "Scenario contract missing: $token"};$checks++
}
Write-Host ("REPEATED REQUEST BINDINGS PASS="+$checks+" FAIL=0")