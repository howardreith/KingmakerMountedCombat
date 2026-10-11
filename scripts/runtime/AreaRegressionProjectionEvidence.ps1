# Exact area regression reader; callers retain frozen payload, suite and restoration gates.
Set-StrictMode -Version Latest
function Get-KmcAreaRegressionRecords([string]$Path,$Game) {
 if([IO.Path]::GetFileName($Path)-cne'boundary-scenario-evidence.jsonl'-or$Game.scenario-cne'chunk4-area-cleanup'){throw 'Area reader requires the exact scenario and JSONL artifact'}
 $records=@(foreach($line in Get-Content -LiteralPath $Path){
  if([string]::IsNullOrWhiteSpace($line)){continue}
  Assert-KmcJsonObjectMembersUnique $line 'bound area record'
  $r=$line|ConvertFrom-Json
  foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){
   if($r.$field-isnot[string]-or[string]::IsNullOrWhiteSpace($r.$field)-or$r.$field-cne$Game.$field){throw ('Area bound record identity differs: '+$field)}
  }
  if(-not(Test-KmcExactJsonInteger $r.schemaVersion)-or$r.schemaVersion-ne2-or$r.artifactKind-cne'boundary-scenario-evidence'-or$r.row-cne'native-area-clean-dismount'){throw 'Area record schema/kind/row differs'}
  $r
 })
 if($records.Count-eq0){throw 'Area artifact empty'}
 $records
}
function Get-KmcAreaRegressionRows([string]$Path,$Game) {
 $records=@(Get-KmcAreaRegressionRecords $Path $Game)
 $rows=@($records|Where-Object phase -CEQ 'row-result')
 if($rows.Count-ne1){throw 'Area artifact requires one exact native terminal row'}
 $r=$rows[0]
 if($r.rowStatus-cnotin@('PASS','FAIL')-or-not(Test-KmcExactJsonInteger $r.assertionPassCount)-or-not(Test-KmcExactJsonInteger $r.assertionFailCount)-or$r.assertionPassCount-lt0-or$r.assertionFailCount-lt0-or$r.recordErrors-isnot[Array]){throw 'Area terminal row shape differs'}
 [pscustomobject]@{name=$r.row;status=$r.rowStatus;assertionPassCount=$r.assertionPassCount;assertionFailCount=$r.assertionFailCount;errors=@($r.recordErrors)}
}
function Get-KmcAreaRegressionProjection($Binding,$Request,$Game,$Result,[string]$Root) {
 if($Binding.scenario-cne'chunk4-area-cleanup'-or$Binding.evidenceLeaf-cne'boundary-scenario-evidence.jsonl'-or@($Binding.rows).Count-ne1-or$Binding.rows[0]-cne'native-area-clean-dismount'){throw 'Area binding requires exact scenario, artifact and row'}
 if([IO.Path]::GetFullPath($Request.evidenceRoot).TrimEnd('\')-cne[IO.Path]::GetFullPath($Root).TrimEnd('\')){throw 'Area request evidence root differs'}
 foreach($item in @($Request,$Game,$Result)){if($item.runId-cne$Binding.runId-or$item.scenario-cne$Binding.scenario){throw 'Area run identity differs'}}
 foreach($item in @($Game,$Result)){
  if($item.status-cne'PASS'-or-not(Test-KmcExactJsonInteger $item.assertionPassCount)-or$item.assertionPassCount-lt1-or-not(Test-KmcExactJsonInteger $item.assertionFailCount)-or$item.assertionFailCount-ne0-or$item.errors-isnot[Array]-or@($item.errors).Count-ne0){throw 'Area native/overall result is not exact PASS'}
  if($item.evidenceManifestSha256-cne$Binding.artifactManifestSha256){throw 'Area result manifest binding differs'}
 }
 Assert-KmcReadOnlyArtifactManifest -Request $Request -ExpectedSha256 $Binding.artifactManifestSha256
 $manifest=Get-KmcBoundJson (Join-Path $Root 'runtime-artifacts.json') $Binding.artifactManifestSha256
 $leaves=@($manifest.artifacts|Where-Object relativePath -CEQ 'boundary-scenario-evidence.jsonl')
 if($leaves.Count-ne1-or$leaves[0].sha256-cne$Binding.evidenceSha256){throw 'Area artifact hash differs from manifest'}
 $path=Join-Path $Root $Binding.evidenceLeaf
 [void](Get-KmcAreaRegressionRecords $path $Game)
 Assert-KmcBoundaryScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $Game.subscenarioResults -GameResult $Game
 Assert-KmcBoundaryScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $Result.subscenarioResults -GameResult $Game
 $rows=@(Get-KmcAreaRegressionRows $path $Game)
 if($rows[0].status-cne'PASS'-or$rows[0].assertionPassCount-ne$Binding.passCount-or$rows[0].assertionFailCount-ne0-or@($rows[0].errors).Count-ne0){throw 'Area native row totals differ'}
 # Only an in-memory projection; all raw phases, native assertions and hashes remain authoritative.
 $projection=[ordered]@{}
 foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$projection[$field]=$Game.$field}
 $projection.status=$rows[0].status;$projection.errors=@($rows[0].errors);$projection.rows=$rows
 [pscustomobject]$projection
}
