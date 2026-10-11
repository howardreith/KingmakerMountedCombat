$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$verify=$repo;$runner=Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1'
$kmcTestRoot=Join-Path $lab ('analysis-cache/unrelated-ledger-tests/'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($kmcTestRoot)|Out-Null
. (Join-Path $verify 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $verify 'scripts/runtime/Chunk6aRegressionEvidence.ps1')
$roles=@(Get-KmcChunk6aRegressionRoles 'CM06-unrelated-actor')
$bindings=@(Get-KmcChunk6aRegressionBinding 'charge-rt' '20260920-chunk4-GJ' $roles[0].rows $lab;Get-KmcChunk6aRegressionBinding 'charge-tb' '20260920-chunk4-GK' $roles[1].rows $lab)
$r=Get-KmcBoundJson (Join-Path $lab ('runtime-evidence/'+$bindings[0].runId+'/runtime-request.json')) $bindings[0].requestSha256
$suite=Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/qualification-suite-snapshots/'+$r.qualificationSuite.suiteId+'.json'))|ConvertFrom-Json
$ledger=Get-Content -LiteralPath (Join-Path $repo 'docs/chunk6a-ledger.json') -Raw|ConvertFrom-Json
$ledger.entries=@($ledger.entries|ForEach-Object {[pscustomobject]@{id=$_.id;family=$_.family;claim=$_.claim;status='BLOCKED';reason='Historical parser fixture only; no current qualification'}})
$ledger.payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256;packagePath=$suite.package.path;packageSha256=$suite.package.sha256;manifestSha256=$suite.package.manifestSha256}
$ledger.note='SYNTHETIC historical parser fixture only; no qualification'
$entry=@($ledger.entries|Where-Object id -CEQ 'CM06-unrelated-actor')[0];$entry.status='PASS';$entry.reason='Synthetic full reader and ledger fixture only'
foreach($key in @('runId','scenario','passCount','failCount','rows','evidenceLeaf','evidenceSha256')){$entry|Add-Member -NotePropertyName $key -NotePropertyValue $bindings[0].$key}
$entry|Add-Member evidenceBinding $bindings[0];$entry|Add-Member supportingRuns $bindings
$checks=0;$results=@();$scratch=Join-Path $kmcTestRoot ('ledger-synthetic-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($scratch)|Out-Null
foreach($case in @('baseline','primary-hash','native-hash','suite','payload','omitted-tb','duplicate-rt','wrong-row','borrowed-claim','completion')){
 $v=$ledger|ConvertTo-Json -Depth 100|ConvertFrom-Json;$e=@($v.entries|Where-Object id -CEQ 'CM06-unrelated-actor')[0]
 switch($case){
  'primary-hash' {$e.evidenceSha256='0'*64}
  'native-hash' {$e.supportingRuns[1].gameResultSha256='0'*64}
  'suite' {$v.payload.suiteSha256='0'*64}
  'payload' {$v.payload.dllMvid='00000000-0000-0000-0000-000000000000'}
  'omitted-tb' {$e.supportingRuns=@($e.supportingRuns[0])}
  'duplicate-rt' {$e.supportingRuns[1]=$e.supportingRuns[0]}
  'wrong-row' {$e.rows=@('foreign');$e.evidenceBinding.rows=@('foreign')}
  'borrowed-claim' {$other=@($v.entries|Where-Object id -CEQ 'CM03-rider-other-action')[0];$other.id=$e.id;$e.id='CM03-rider-other-action'}
 }
 $p=Join-Path $scratch ($case+'.json');[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false));$errorText=$null
 if($case-ceq'completion'){
  $process=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$runner+'"'),'-LedgerPath',('"'+$p+'"'),'-LabRoot',('"'+$lab+'"'),'-Completion') -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $scratch 'completion.log') -RedirectStandardError (Join-Path $scratch 'completion.err')
  if($process.ExitCode-ne0){$errorText='Completion child exit '+$process.ExitCode}
 }else{try{& $runner -LedgerPath $p -LabRoot $lab}catch{$errorText=$_.Exception.Message}}
 $expected=$case-ceq'baseline';if(($null-eq$errorText)-ne$expected){throw ('Unexpected unrelated ledger '+$case+': '+$errorText)}
 $checks++;$results+=@([pscustomobject]@{case=$case;expectedPass=$expected;error=$errorText});Write-Host ('PASS unrelated full ledger '+$case)
}
[pscustomobject]@{status='PASS';checks=$checks;atUtc=[DateTime]::UtcNow.ToString('o');scope='Historical exact RT+TB full-reader/ledger test only; no current qualification';results=$results}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $kmcTestRoot 'ledger-test-receipt.json') -Encoding UTF8
Write-Host ('UNRELATED FULL LEDGER PASS='+$checks+' FAIL=0; draft only')
