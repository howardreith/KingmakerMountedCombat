$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk6aLegacyTestFixtures.ps1')
$suite=Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/qualification-suite-snapshots/'+$payload.suiteId+'.json'))|ConvertFrom-Json
foreach($item in @(@('packagePath',$suite.package.path),@('packageSha256',$suite.package.sha256),@('manifestSha256',$suite.package.manifestSha256))){$payload|Add-Member -NotePropertyName $item[0] -NotePropertyValue $item[1]}
$ledger=Get-Content -Raw -LiteralPath (Join-Path $repo 'docs/chunk6a-ledger.json')|ConvertFrom-Json
$ledger.entries=@($ledger.entries|ForEach-Object {[pscustomobject]@{id=$_.id;family=$_.family;claim=$_.claim;status='BLOCKED';reason='Synthetic historical reader fixture; no native qualification.'}})
$ledger.payload=$payload;$ledger.note='SYNTHETIC READER REGRESSION ONLY: synthetic modern Mammoth binding, no active candidate qualification.'
$entry=@($ledger.entries|Where-Object id -CEQ 'CM08-mounted-mammoth-primary-hit-tb')[0]
$entry.status='PASS';$entry.reason='Reader regression against synthetic modern envelope; never current qualification.'
foreach($name in @('runId','scenario','passCount','failCount','rows','evidenceLeaf','evidenceSha256')){$entry|Add-Member -Force -NotePropertyName $name -NotePropertyValue $b.$name}
$entry|Add-Member -NotePropertyName evidenceBinding -NotePropertyValue $b
$testRoot=Join-Path $scratch 'ledger-binding-fixtures';[IO.Directory]::CreateDirectory($testRoot)|Out-Null
function Write-LedgerFixture($v,[string]$name){$p=Join-Path $testRoot ($name+'.json');[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false));$p}
$baseline=Write-LedgerFixture $ledger 'synthetic-modern-baseline'
& (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1') -LedgerPath $baseline -LabRoot $lab
Write-Host 'Full ledger reader synthetic modern binding PASS; no current qualification.'
$checks=1
foreach($case in @('primary-hash','native-hash','wrong-suite','wrong-payload','wrong-row','completion')){
 $v=$ledger|ConvertTo-Json -Depth 100|ConvertFrom-Json;$e=@($v.entries|Where-Object id -CEQ 'CM08-mounted-mammoth-primary-hit-tb')[0]
 switch($case){
  'primary-hash' {$e.evidenceSha256='0'*64}
  'native-hash' {$e.evidenceBinding.gameResultSha256='0'*64}
  'wrong-suite' {$v.payload.suiteSha256='0'*64}
  'wrong-payload' {$v.payload.dllMvid='00000000-0000-0000-0000-000000000000'}
  'wrong-row' {$e.rows=@('unrelated');$e.evidenceBinding.rows=@('unrelated')}
 }
 $path=Write-LedgerFixture $v $case;$rejected=$false
 if($case-ceq'completion'){
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1') -LedgerPath $path -LabRoot $lab -Completion
  $rejected=$LASTEXITCODE-ne0
  if($rejected){Write-Host 'PASS full ledger rejects completion: actual child exit is nonzero'}
 }else{try{& (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1') -LedgerPath $path -LabRoot $lab}catch{$rejected=$true;Write-Host ('PASS full ledger rejects '+$case+': '+$_.Exception.Message)}}
 if(-not$rejected){throw ('Full ledger accepted '+$case)};$checks++
}
[ordered]@{status='PASS';scope='Synthetic reader regression only';checks=$checks;currentQualification=$false;originalRun=$run;originalEvidenceSha256=$b.evidenceSha256;atUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $scratch 'Ledger-Binding-DRAFT-receipt.json') -Encoding UTF8
Write-Host ('LEGACY FULL LEDGER BINDING READER PASS='+$checks+' FAIL=0; no current qualification.')

Remove-LegacyTestFixture
