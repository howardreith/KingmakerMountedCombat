# Separate future 6A evidence reader. No current-candidate qualification or runtime mutation.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'UnrelatedChargeTerminalEvidence.ps1')
function Get-KmcChunk6aRegressionClaims {
 @('CM06-unrelated-actor','CM08-mounted-charge-rejected-rt','CM08-mounted-charge-rejected-tb','CM08-unmounted-charge','CM08-ordinary-attack-controls-tb','CM08-chunk4-sustained-tb','CM08-sustained-rt','CM08-incoming-targeting','CM08-rider-death-cleanup','CM08-mount-death-cleanup')
}
function Get-KmcChunk6aRegressionRoles([string]$Id) {
 $charge=@('C4-CHARGE-mounted-rider','C4-CHARGE-unmounted-rider','C4-CHARGE-mounted-mount','C4-CHARGE-unrelated-actor','C4-CHARGE-queued-state-change')
 $specs=@()
 switch -CaseSensitive ($Id) {
  'CM08-mounted-charge-rejected-rt' {$specs=@(,@('charge-rt','chunk4-charge-safety-rt',$charge))}
  'CM08-mounted-charge-rejected-tb' {$specs=@(,@('charge-tb','chunk4-charge-safety-tb',$charge))}
  'CM06-unrelated-actor' {$specs=@(@('charge-rt','chunk4-charge-safety-rt',$charge),@('charge-tb','chunk4-charge-safety-tb',$charge))}
  'CM08-unmounted-charge' {$specs=@(@('charge-rt','chunk4-charge-safety-rt',$charge),@('charge-tb','chunk4-charge-safety-tb',$charge))}
  'CM08-ordinary-attack-controls-tb' {
   $rows=@('C01-B','C01-C','C01-D','C03-rapid-off-B','C03-rapid-off-C','C03-bab-B','C03-bab-C','C03-haste-B','C03-haste-C','C02-restricted-B','C02-restricted-C','C03-single-B','C03-single-C','C03-spent-standard-B','C03-spent-standard-C','C03-rider-move-B','C03-carried-move-C','C03-mixed-range-B','C03-mixed-range-C')
   $specs=@(,@('ordinary-tb','ordinary-attack-controls-tb',$rows))
  }
  'CM08-chunk4-sustained-tb' {$specs=@(,@('sustained-tb','chunk4-sustained-tb',@('C4-SUSTAINED-TB-rider-first','C4-SUSTAINED-TB-mount-first','C4-SUSTAINED-TB-rider-exhausted','C4-SUSTAINED-TB-mount-exhausted','C4-SUSTAINED-TB-early-end','C4-SUSTAINED-TB-after-early-end')))}
  'CM08-sustained-rt' {
   $specs=@(@('melee-rt','chunk4-sustained-melee-rt',@('C4-SUSTAINED-melee-adjacent-held','C4-SUSTAINED-melee-adjacent-repeat','C4-SUSTAINED-melee-approach-held','C4-SUSTAINED-melee-approach-repeat')),
    @('ranged-rt','chunk4-sustained-ranged-rt',@('C4-SUSTAINED-ranged-adjacent-held','C4-SUSTAINED-ranged-adjacent-repeat','C4-SUSTAINED-ranged-approach-held','C4-SUSTAINED-ranged-approach-repeat')))
  }
  'CM08-incoming-targeting' {$specs=@(@('target-rider','chunk4-targeting-rider-rt',@('C4-TARGETING-rider-heal','C4-TARGETING-area-both','C4-TARGETING-rider-hostile')),
   @('target-mount','chunk4-targeting-mount-rt',@('C4-TARGETING-mount-heal','C4-TARGETING-mount-hostile')),@('target-area-control','chunk4-targeting-area-unmounted-rt',@('C4-TARGETING-area-unmounted')))}
  'CM08-rider-death-cleanup' {$specs=@(,@('rider-death','chunk4-rider-death-tb',@('C4-LIFE-rider-death-live-command')))}
  'CM08-mount-death-cleanup' {$specs=@(,@('mount-death','chunk4-mount-death-tb',@('C4-LIFE-mount-death-live-command')))}
 }
 foreach($spec in $specs){[pscustomobject]@{name=$spec[0];scenario=$spec[1];rows=@($spec[2]);evidenceLeaf='phase3d-horse-scenario-evidence.json'}}
}
function Assert-KmcChunk6aRegressionBindings([string]$Id,$Primary,$Bindings) {
 $roles=@(Get-KmcChunk6aRegressionRoles $Id)
 if($roles.Count-eq0){throw 'No implemented regression contract for this claim'}
 $items=@($Bindings)
 if($items.Count-ne$roles.Count-or@($items|ForEach-Object runId|Select-Object -Unique).Count-ne$items.Count){throw 'Regression requires every distinct native transaction'}
 foreach($role in $roles){
  $found=@($items|Where-Object role -CEQ $role.name)
  if($found.Count-ne1-or$found[0].scenario-cne$role.scenario-or$found[0].evidenceLeaf-cne$role.evidenceLeaf){throw 'Regression role, scenario or artifact differs'}
  $rows=@($found[0].rows)
  if($rows.Count-ne$role.rows.Count-or@($rows|Select-Object -Unique).Count-ne$rows.Count){throw 'Regression row count or uniqueness differs'}
  foreach($row in $role.rows){if(@($rows|Where-Object {$_-ceq$row}).Count-ne1){throw 'Regression omits an exact mandatory native row'}}
 }
 if($null-eq$Primary){throw 'Regression lacks primary binding'}
 $primaryItems=@($items|Where-Object runId -CEQ $Primary.runId)
 if($primaryItems.Count-ne1){throw 'Regression primary is outside its native transactions'}
 foreach($field in @('runId','scenario','evidenceLeaf','evidenceSha256','resultSha256','gameResultSha256','requestSha256','orchestrationSha256','transactionSha256','artifactManifestSha256','passCount','failCount')){
  if([string]$Primary.$field-cne[string]$primaryItems[0].$field){throw ('Regression primary binding differs: '+$field)}
 }
 if(($Primary.rows -join '|')-cne($primaryItems[0].rows -join '|')){throw 'Regression primary rows differ'}
}
function Get-KmcChunk6aRegressionBinding([string]$Role,[string]$RunId,[string[]]$Rows,[string]$LabRoot) {
 $binding=Get-KmcSupportingBinding $Role $RunId $Rows $LabRoot
 if($binding.evidenceLeaf-cne'phase3d-horse-scenario-evidence.json'){throw 'Regression requires the exact Horse artifact'}
 $manifest=Join-Path $LabRoot ('runtime-evidence/'+$RunId+'/runtime-artifacts.json')
 $binding|Add-Member -MemberType NoteProperty -Name artifactManifestSha256 -Value ((Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant())
 $binding
}
function Assert-KmcChunk6aRegressionRun($Payload,$Binding,[string]$LabRoot) {
 Assert-KmcSupportingRun $Payload $Binding $LabRoot
 $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
 $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
 $game=Get-KmcBoundJson (Join-Path $root 'runtime-game-result.json') $Binding.gameResultSha256
 $result=Get-KmcBoundJson (Join-Path $root 'runtime-result.json') $Binding.resultSha256
 if([IO.Path]::GetFullPath($request.evidenceRoot).TrimEnd('\')-cne[IO.Path]::GetFullPath($root).TrimEnd('\')){throw 'Regression request evidence root differs'}
 foreach($item in @($game,$result)){if($item.evidenceManifestSha256-cne$Binding.artifactManifestSha256){throw 'Regression manifest binding differs'}}
 Assert-KmcReadOnlyArtifactManifest -Request $request -ExpectedSha256 $Binding.artifactManifestSha256
 $manifest=Get-KmcBoundJson (Join-Path $root 'runtime-artifacts.json') $Binding.artifactManifestSha256
 $records=@($manifest.artifacts|Where-Object relativePath -CEQ $Binding.evidenceLeaf)
 if($records.Count-ne1-or$records[0].sha256-cne$Binding.evidenceSha256){throw 'Regression evidence hash differs from manifest'}
 Assert-KmcPhase3dHorseScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS' -SubscenarioResults $game.subscenarioResults
 Assert-KmcPhase3dHorseScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS' -SubscenarioResults $result.subscenarioResults
 $artifact=Get-KmcBoundJson (Join-Path $root $Binding.evidenceLeaf) $Binding.evidenceSha256
 foreach($rowName in $Binding.rows){
  $native=@($game.subscenarioResults|Where-Object name -CEQ $rowName);$outer=@($result.subscenarioResults|Where-Object name -CEQ $rowName)
  $rows=@($artifact.rows|Where-Object name -CEQ $rowName)
  if($native.Count-ne1-or$outer.Count-ne1-or$rows.Count-ne1){throw 'Regression exact row missing from one result'}
  foreach($row in @($native)+@($outer)){
   if($row.status-cne'PASS'-or-not(Test-KmcExactJsonInteger $row.assertionPassCount)-or$row.assertionPassCount-ne1-or
      -not(Test-KmcExactJsonInteger $row.assertionFailCount)-or$row.assertionFailCount-ne0-or$row.errors-isnot[Array]-or@($row.errors).Count-ne0){throw 'Regression native/overall row is not an exact single PASS'}
  }
  # Artifact rows hold observations, not result counters; the complete original
  # scenario validator above checks their native behavior before status is bound.
  if($rows[0].status-cne'PASS'){throw 'Regression artifact observation row is not PASS'}
 }
}
function Assert-KmcChunk6aRegressionQualification([string]$Id,$Payload,$Primary,$Bindings,[string]$LabRoot) {
 Assert-KmcChunk6aRegressionBindings $Id $Primary $Bindings
 Assert-KmcChunk6aFrozenPayload $Payload $LabRoot
 Assert-KmcChunk6aRegressionRun $Payload $Primary $LabRoot
 foreach($binding in $Bindings){Assert-KmcChunk6aRegressionRun $Payload $binding $LabRoot}
 if($Id-ceq'CM06-unrelated-actor'){
  foreach($binding in $Bindings){
   $artifact=Get-KmcBoundJson (Join-Path $LabRoot ('runtime-evidence/'+$binding.runId+'/'+$binding.evidenceLeaf)) $binding.evidenceSha256
   Assert-KmcUnrelatedChargeTerminal $artifact $(if($binding.scenario-ceq'chunk4-charge-safety-rt'){'RT'}else{'TB'})
  }
 }

 $roles=@(Get-KmcChunk6aRegressionRoles $Id)
 if($roles.Count-gt1){Assert-KmcCompositeRunOrder $Bindings $roles $LabRoot}
}