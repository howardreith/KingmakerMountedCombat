$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
$checks=0
function Accept([string]$Name,[scriptblock]$Action){& $Action;$script:checks++;Write-Output ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Action){$refused=$false;try{& $Action}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:checks++;Write-Output ('PASS rejects '+$Name)}
$contracts=@(Get-KmcChunk6aPrimaryContracts)
foreach($contract in $contracts){
 $binding=[pscustomobject]@{scenario=$contract.scenario;rows=@($contract.rows);evidenceLeaf=$contract.evidenceLeaf}
 Accept ($contract.id+' exact mapping') {Assert-KmcChunk6aPrimaryClaim $contract.id $binding}
 foreach($row in $contract.rows){
  $changed=[pscustomobject]@{scenario=$binding.scenario;rows=@($binding.rows|Where-Object {$_-cne$row});evidenceLeaf=$binding.evidenceLeaf}
  Reject ($contract.id+' omits '+$row) {Assert-KmcChunk6aPrimaryClaim $contract.id $changed}
 }
 $changed=[pscustomobject]@{scenario=$binding.scenario.ToUpperInvariant();rows=$binding.rows;evidenceLeaf=$binding.evidenceLeaf}
 Reject ($contract.id+' scenario case') {Assert-KmcChunk6aPrimaryClaim $contract.id $changed}
 $changed=[pscustomobject]@{scenario=$binding.scenario;rows=$binding.rows;evidenceLeaf='runtime-result.json'}
 Reject ($contract.id+' wrong leaf') {Assert-KmcChunk6aPrimaryClaim $contract.id $changed}
 $changed=[pscustomobject]@{scenario=$binding.scenario;rows=@($binding.rows)+@($binding.rows[0]);evidenceLeaf=$binding.evidenceLeaf}
 Reject ($contract.id+' duplicate row') {Assert-KmcChunk6aPrimaryClaim $contract.id $changed}
 Reject ($contract.id+' claim case') {Assert-KmcChunk6aPrimaryClaim $contract.id.ToLowerInvariant() $binding}
 foreach($other in $contracts){
  if($other.scenario-ceq$contract.scenario-or@($contracts|Where-Object { $_.id-ceq$contract.id -and $_.scenario-ceq$other.scenario }).Count-ne0){continue}
  $changed=[pscustomobject]@{scenario=$other.scenario;rows=$binding.rows;evidenceLeaf=$binding.evidenceLeaf}
  Reject ($contract.id+' borrowed scenario '+$other.scenario) {Assert-KmcChunk6aPrimaryClaim $contract.id $changed}
 }
}
$source=Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1'
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($source,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Ledger parser failed'}
$assignment=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left-is[Management.Automation.Language.VariableExpressionAst]-and$n.Left.VariablePath.UserPath-ceq'mandatory'},$true))
if($assignment.Count-ne1){throw 'Mandatory source catalog missing'}
$ids=@($assignment[0].Right.FindAll({param($n)$n-is[Management.Automation.Language.StringConstantExpressionAst]},$true)|ForEach-Object Value)
if($ids.Count-ne87-or@($ids|Select-Object -Unique).Count-ne87){throw 'Mandatory list changed'}
$ledger=[pscustomobject]@{entries=@($ids|ForEach-Object {[pscustomobject]@{id=$_}})}
foreach($entry in $ledger.entries){
 if(@($contracts|Where-Object id -CEQ $entry.id).Count-ne0){continue}
 $binding=[pscustomobject]@{scenario='chunk6a-mount-approach';rows=@('CM01-exploration-free');evidenceLeaf='phase3d-horse-scenario-evidence.json'}
 Reject ($entry.id+' cannot borrow exploration PASS') {Assert-KmcChunk6aPrimaryClaim $entry.id $binding}
}
Write-Output ('PRIMARY CLAIM CONTRACT PASS='+$checks+' FAIL=0; no native qualification')
