$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$draft=Join-Path $lab 'analysis-cache/chunk6a-area-tests'
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
# The current claim is the qualified P07 save/area-reload lifecycle. Legacy54
# clean-Dismount parsing remains covered by the area reader/row tests, including
# its explicit refusal as current qualification. Do not relax the primary claim.
$activePath=Join-Path $repo 'docs/chunk6a-ledger.json'
$ledger=Get-Content -Raw -LiteralPath $activePath|ConvertFrom-Json
$areas=@($ledger.entries|Where-Object id -CEQ 'CM08-area-restoration')
if($areas.Count-ne1-or$areas[0].status-cne'PASS'-or$areas[0].scenario-cne'persistence-p07-save'){throw 'Area ledger regression requires the accepted P07 area-restoration binding.'}
$area=$areas[0]
$ledger.entries=@($ledger.entries|ForEach-Object {
 if($_.id-ceq'CM08-area-restoration'){$area}
 else{[pscustomobject]@{id=$_.id;family=$_.family;claim=$_.claim;status='BLOCKED';reason='Synthetic isolated area reader fixture; no candidate qualification.'}}
})
$ledger.note='SYNTHETIC READ-ONLY LEDGER FIXTURE: accepted P07 area binding only; no full candidate qualification.'
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
[pscustomobject]@{status='PASS';checks=$results.Count;scope='Synthetic isolated P07 area ledger regression; no native qualification';sourceRun=$area.runId;sourceLedgerSha256=(Get-FileHash -LiteralPath $activePath).Hash.ToLowerInvariant();atUtc=[DateTime]::UtcNow.ToString('o');results=$results}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $scratch 'ledger-test-receipt.json') -Encoding UTF8
'AREA FULL LEDGER PASS='+$results.Count+' FAIL=0; draft only'
