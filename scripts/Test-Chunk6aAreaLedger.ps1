$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$draft=Join-Path $lab 'analysis-cache/chunk6a-area-tests'
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
$run='20260920-chunk4-GZ';$binding=Get-KmcSupportingBinding 'area-regression' $run @('native-area-clean-dismount') $lab
$r=Get-KmcBoundJson (Join-Path $lab ('runtime-evidence/'+$run+'/runtime-request.json')) $binding.requestSha256
$suite=Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/qualification-suite-snapshots/'+$r.qualificationSuite.suiteId+'.json'))|ConvertFrom-Json
$ledger=Get-Content -Raw -LiteralPath (Join-Path $repo 'docs/chunk6a-ledger.json')|ConvertFrom-Json
$ledger.entries=@($ledger.entries|ForEach-Object {[pscustomobject]@{id=$_.id;family=$_.family;claim=$_.claim;status='BLOCKED';reason='Historical54 reader test only; no current qualification'}})
$ledger.payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256;packagePath=$suite.package.path;packageSha256=$suite.package.sha256;manifestSha256=$suite.package.manifestSha256}
$ledger.note='SYNTHETIC READ-ONLY HISTORICAL54 LEDGER FIXTURE: no current qualification'
$e=@($ledger.entries|Where-Object id -CEQ 'CM08-area-restoration')[0];$e.status='PASS';$e.reason='Historical archive reader test only'
foreach($name in @('runId','scenario','passCount','failCount','rows','evidenceLeaf','evidenceSha256')){$e|Add-Member -Force -NotePropertyName $name -NotePropertyValue $binding.$name}
$e|Add-Member -NotePropertyName evidenceBinding -NotePropertyValue $binding
$scratch=Join-Path $draft ('ledger-synthetic-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($scratch)|Out-Null
$runner=Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1';$results=@()
foreach($case in @('baseline','primary-hash','native-hash','suite','payload','borrowed-claim','wrong-row','completion')){
 $v=$ledger|ConvertTo-Json -Depth 100|ConvertFrom-Json;$entry=@($v.entries|Where-Object id -CEQ 'CM08-area-restoration')[0]
 switch($case){
  'primary-hash' {$entry.evidenceSha256='0'*64}
  'native-hash' {$entry.evidenceBinding.gameResultSha256='0'*64}
  'suite' {$v.payload.suiteSha256='0'*64}
  'payload' {$v.payload.dllMvid='00000000-0000-0000-0000-000000000000'}
  'borrowed-claim' {$other=@($v.entries|Where-Object id -CEQ 'CM08-horse-smoke')[0];$other.id=$entry.id;$entry.id='CM08-horse-smoke'}
  'wrong-row' {$entry.rows=@('unrelated');$entry.evidenceBinding.rows=@('unrelated')}
 }
 $path=Join-Path $scratch ($case+'.json');[IO.File]::WriteAllText($path,($v|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
 $errorText=$null
 if($case-ceq'completion'){
  $out=Join-Path $scratch 'completion.log';$err=Join-Path $scratch 'completion.err'
  $p=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$runner+'"'),'-LedgerPath',('"'+$path+'"'),'-LabRoot',('"'+$lab+'"'),'-Completion') -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $out -RedirectStandardError $err
  if($p.ExitCode-ne0){$errorText='Completion child exit '+$p.ExitCode}
 }else{try{& $runner -LedgerPath $path -LabRoot $lab}catch{$errorText=$_.Exception.Message}}
 if(($null-eq$errorText)-ne($case-ceq'baseline')){throw ('Unexpected '+$case+': '+$errorText)}
 $results+=@([pscustomobject]@{case=$case;assertion=$errorText});Write-Host ('PASS area ledger '+$case)
}
[pscustomobject]@{status='PASS';checks=$results.Count;scope='Historical54 full ledger reader regression; no native/current qualification';atUtc=[DateTime]::UtcNow.ToString('o');results=$results}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $scratch 'ledger-test-receipt.json') -Encoding UTF8
'AREA FULL LEDGER PASS='+$results.Count+' FAIL=0; draft only'
