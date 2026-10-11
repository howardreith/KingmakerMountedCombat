$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aRegressionEvidence.ps1')
$runner=Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1'
$ledger=Get-Content -Raw -LiteralPath (Join-Path $repo 'docs/chunk6a-ledger.json')|ConvertFrom-Json
$ledger.entries=@($ledger.entries|ForEach-Object {[pscustomobject]@{id=$_.id;family=$_.family;claim=$_.claim;status='BLOCKED';reason='Historical reader fixture only; no current native qualification.'}})
$ledger.note='SYNTHETIC READER TEST: immutable archived105 bindings. Never active qualification.'
$bindings=@()
foreach($case in @(@('CM08-ordinary-attack-controls-tb','final105-p08-ordinary-tb'),@('CM08-chunk4-sustained-tb','final105-p08-sustained-tb'))){
 $role=@(Get-KmcChunk6aRegressionRoles $case[0])[0]
 $b=Get-KmcChunk6aRegressionBinding $role.name $case[1] $role.rows $lab
 $bindings+=@($b)
 $entry=@($ledger.entries|Where-Object id -CEQ $case[0])[0]
 $entry.status='PASS';$entry.reason='Immutable archived105 native reader regression only.'
 foreach($name in @('runId','scenario','passCount','failCount','rows','evidenceLeaf','evidenceSha256')){$entry|Add-Member -Force -NotePropertyName $name -NotePropertyValue $b.$name}
 $entry|Add-Member -NotePropertyName evidenceBinding -NotePropertyValue $b
}
$r=Get-KmcBoundJson (Join-Path $lab ('runtime-evidence/'+$bindings[0].runId+'/runtime-request.json')) $bindings[0].requestSha256
$suite=Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/qualification-suite-snapshots/'+$r.qualificationSuite.suiteId+'.json'))|ConvertFrom-Json
$ledger.payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256;packagePath=$suite.package.path;packageSha256=$suite.package.sha256;manifestSha256=$suite.package.manifestSha256}
$scratch=Join-Path $lab ('analysis-cache/chunk6a-regression-tests/ledger-synthetic-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($scratch)|Out-Null
$checks=0;$results=@()
foreach($case in @('baseline','primary-hash','native-hash','wrong-suite','wrong-payload','wrong-row','manifest-hash','borrowed-claim','completion')){
 $v=$ledger|ConvertTo-Json -Depth 100|ConvertFrom-Json;$e=@($v.entries|Where-Object id -CEQ 'CM08-ordinary-attack-controls-tb')[0]
 switch($case){
  'primary-hash' {$e.evidenceSha256='0'*64}
  'native-hash' {$e.evidenceBinding.gameResultSha256='0'*64}
  'wrong-suite' {$v.payload.suiteSha256='0'*64}
  'wrong-payload' {$v.payload.dllMvid='00000000-0000-0000-0000-000000000000'}
  'wrong-row' {$e.rows=@('unrelated');$e.evidenceBinding.rows=@('unrelated')}
  'manifest-hash' {$e.evidenceBinding.artifactManifestSha256='0'*64}
  'borrowed-claim' {$other=@($v.entries|Where-Object id -CEQ 'CM08-rider-death-cleanup')[0];$other.id=$e.id;$e.id='CM08-rider-death-cleanup'}
 }
 $p=Join-Path $scratch ($case+'.json');[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
 $errorText=$null
 if($case-ceq'completion'){
  $pLog=Join-Path $scratch 'completion.log';$pErr=Join-Path $scratch 'completion.err'
  $child=Start-Process powershell.exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"'+$runner+'"'),'-LedgerPath',('"'+$p+'"'),'-LabRoot',('"'+$lab+'"'),'-Completion') -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $pLog -RedirectStandardError $pErr
  if($child.ExitCode-ne0){$errorText='Completion child exit '+$child.ExitCode}
 }else{try{& $runner -LedgerPath $p -LabRoot $lab}catch{$errorText=$_.Exception.Message}}
 $wantPass=$case-ceq'baseline'
 if(($null-eq$errorText)-ne$wantPass){throw ('Unexpected ledger case '+$case+': '+$errorText)}
 $checks++;$results+=@([pscustomobject]@{case=$case;expectedPass=$wantPass;assertion=$errorText});Write-Host ('PASS ledger '+$case+': '+$errorText)
}
[pscustomobject]@{status='PASS';checks=$checks;nativeQualification=$false;scope='Historical105 full ledger regression only';scratch=$scratch;results=$results}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $scratch 'ledger-receipt.json') -Encoding UTF8
'REGRESSION FULL LEDGER PASS='+$checks+' FAIL=0; no current qualification'