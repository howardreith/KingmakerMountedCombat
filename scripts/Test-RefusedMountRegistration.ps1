$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checks=0
$rows=@('CM02-wrong-creature-target','CM06-mount-selected','CM06-multiple-selection','CM06-foreign-selection','CM02-foreign-companion')
$cases=@('wrong-creature-target','mount-selected','multiple-selection','foreign-selection','foreign-companion')
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count -ne 0){throw $errors[0]}
$defs=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Assert-KmcIsolatedScenarioRows'},$true))
if($defs.Count -ne 1){throw 'Isolated mapping missing'}
. ([scriptblock]::Create($defs[0].Extent.Text))
for($i=0;$i -lt $rows.Count;$i++){
 $name='chunk6a-refused-'+$cases[$i]
 foreach($file in @('src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs','scripts/runtime/RuntimeHarness.Common.ps1','scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1','scripts/runtime/Test-RuntimeRequest.ps1')) {
  $source=Get-Content -Raw (Join-Path $repo $file)
  if(-not $source.Contains($name)){throw ('Refusal registration missing from '+$file)};$checks++
 }
 if($i -eq 3){
  $binding=[pscustomobject]@{scenario=$name;rows=@($rows[$i])}
  if(Assert-KmcIsolatedScenarioRows 'CM06-unrelated-actor' $binding){throw 'Foreign selection incorrectly qualifies unrelated ability pass-through'};$checks++
  if(Assert-KmcIsolatedScenarioRows $rows[$i] $binding){throw 'Diagnostic foreign selection has a mandatory mapping'};$checks++
  continue
 }
 $binding=[pscustomobject]@{scenario=$name;rows=@($rows[$i])}
 if(-not (Assert-KmcIsolatedScenarioRows $rows[$i] $binding)){throw 'Refusal qualification mapping missing'};$checks++
 foreach($field in @('scenario','rows')){
  $bad=[pscustomobject]@{scenario=$name;rows=@($rows[$i])};if($field -ceq 'scenario'){$bad.scenario='chunk6a-mount-approach'}else{$bad.rows=@()}
  $refused=$false;try{$null=Assert-KmcIsolatedScenarioRows $rows[$i] $bad}catch{$refused=$true}
  if(-not $refused){throw 'Refusal qualification accepted a mismatched scenario or row'};$checks++
 }
}
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aRefusedMountScenario.cs')
if($source.IndexOf('EnsureChunk6aRiderSelection(row)') -ge $source.IndexOf('var legal = new JObject') -or $source.IndexOf('if (!exactSelection)') -ge $source.IndexOf('chunk6aRefusalProbe.Invoke()')){throw 'Refusal baseline or exact negative selection order regressed'};$checks++
if($source.Contains('TryNativeAbilityTargetClick') -or $source.Contains('BeginChunk6aCommandWindow')){throw 'Refusal flow added a positive Mount'};$checks++
if(-not $source.Contains('NativeRefusedMountCaseEvidence.AssertComplete(evidence)')){throw 'Producer does not use shared refusal case decision'};$checks++
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aCombatMountScenario.cs')
if(-not $tranche.Contains('Chunk6aRefusedOnly ? 24') -or -not $tranche.Contains('if (chunk6aStage == 24) { TickChunk6aRefusedMount(); return; }')){throw 'Refusal is not isolated from the positive/compensation flow'};$checks++
foreach($file in @('src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs','scripts/runtime/RuntimeHarness.Common.ps1')) {
 $text=Get-Content -Raw -LiteralPath (Join-Path $repo $file)
 if(-not$text.Contains('CM02-foreign-companion')){throw 'Exact foreign refusal row missing from native/external catalog'};$checks++
}
Write-Output ('REFUSAL REGISTRATION PASS='+$checks+' FAIL=0; lab source/component contract, not native qualification')
