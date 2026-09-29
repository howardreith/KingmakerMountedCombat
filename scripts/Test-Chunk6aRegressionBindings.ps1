$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aRegressionEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/OrdinaryAttackControlsEvidence.ps1')
$ids=@('CM08-mounted-charge-rejected-rt','CM08-mounted-charge-rejected-tb','CM08-unmounted-charge','CM08-ordinary-attack-controls-tb','CM08-chunk4-sustained-tb','CM08-sustained-rt','CM08-incoming-targeting','CM08-rider-death-cleanup','CM08-mount-death-cleanup')
$checks=0
function Copy-RegressionValue($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
function Reject-Regression([scriptblock]$Change){
 $p=Copy-RegressionValue $script:primary;$copied=Copy-RegressionValue $script:bindings;$b=@($copied);& $Change $p $b
 $refused=$false;try{Assert-KmcChunk6aRegressionBindings $script:claim $p $b}catch{$refused=$true}
 if(-not$refused){throw ('Regression mutation accepted: '+$Change.ToString())};$script:checks++
}
foreach($id in $ids){
 $script:claim=$id;$roles=@(Get-KmcChunk6aRegressionRoles $id)
 $expected=if($id-ceq'CM08-incoming-targeting'){3}elseif($id-cin@('CM08-unmounted-charge','CM08-sustained-rt')){2}else{1}
 if($roles.Count-ne$expected){throw ('Role cardinality differs: '+$id+' observed='+$roles.Count)};$checks++
 $script:bindings=@(for($i=0;$i-lt$roles.Count;$i++){
  $r=$roles[$i];$value=[ordered]@{role=$r.name;runId=('synthetic-'+$i);scenario=$r.scenario;rows=@($r.rows);evidenceLeaf=$r.evidenceLeaf;passCount=50;failCount=0}
  foreach($field in @('evidenceSha256','resultSha256','gameResultSha256','requestSha256','orchestrationSha256','transactionSha256','artifactManifestSha256')){$value[$field]='a'*64}
  [pscustomobject]$value
 })
 foreach($binding in $bindings){$script:primary=Copy-RegressionValue $binding;Assert-KmcChunk6aRegressionBindings $id $primary $bindings;$checks++}
 $script:primary=Copy-RegressionValue $bindings[0]
 foreach($field in @('runId','scenario','evidenceLeaf','evidenceSha256','resultSha256','gameResultSha256','requestSha256','orchestrationSha256','transactionSha256','artifactManifestSha256')){Reject-Regression {param($p,$b)$p.$field='foreign'}}
 foreach($field in @('passCount','failCount')){Reject-Regression {param($p,$b)$p.$field++}}
 Reject-Regression {param($p,$b)$p.rows=@('borrowed')}
 foreach($index in 0..($bindings.Count-1)){
  foreach($field in @('role','scenario','evidenceLeaf')){Reject-Regression {param($p,$b)$b[$index].$field='foreign'}}
  Reject-Regression {param($p,$b)$b[$index].rows=@()}
  Reject-Regression {param($p,$b)$b[$index].rows+=@($b[$index].rows[0])}
  foreach($rowIndex in 0..($bindings[$index].rows.Count-1)){Reject-Regression {param($p,$b)$b[$index].rows[$rowIndex]='wrong-row'}}
 }
 foreach($count in @(0,($bindings.Count-1),($bindings.Count+1))){$replacement=@(for($i=0;$i-lt$count;$i++){$bindings[$i%$bindings.Count]});$refused=$false;try{Assert-KmcChunk6aRegressionBindings $id $primary $replacement}catch{$refused=$true};if(-not$refused){throw 'Incomplete role set accepted'};$checks++}
 if($bindings.Count-gt1){Reject-Regression {param($p,$b)$b[1].runId=$b[0].runId}}
}
$ordinary=@(Get-KmcChunk6aRegressionRoles 'CM08-ordinary-attack-controls-tb')
if(($ordinary[0].rows -join '|')-cne(@(Get-KmcOrdinaryAttackControlCases|ForEach-Object id)-join'|')){throw 'Ordinary baseline row inventory differs'};$checks++
foreach($id in @('CM07-cold-load','CM08-persistence-suite','CM08-horse-smoke','CM06-ai-auto-use','unregistered')){$refused=$false;try{Assert-KmcChunk6aRegressionBindings $id $primary $bindings}catch{$refused=$true};if(-not$refused){throw 'Unimplemented claim accepted'};$checks++}
'REGRESSION BINDINGS PASS='+$checks+' FAIL=0; synthetic only, no qualification'