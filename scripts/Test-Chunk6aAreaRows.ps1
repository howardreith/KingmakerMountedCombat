$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aArtifactRowsEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$draft=Join-Path $lab 'analysis-cache/chunk6a-area-tests';$source=Join-Path $lab 'runtime-evidence/20260920-chunk4-GZ'
$original=[IO.File]::ReadAllText((Join-Path $source 'boundary-scenario-evidence.jsonl'))
$originalGame=Get-Content -Raw -LiteralPath (Join-Path $source 'runtime-game-result.json')
$scratch=Join-Path $draft ('row-synthetic-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($scratch)|Out-Null
$path=Join-Path $scratch 'boundary-scenario-evidence.jsonl';$results=@();$assertion='Exact retained assertion: native actor restoration failed (rider).'
$fields=@('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')
foreach($case in @('baseline','retained-failure','missing-terminal','duplicate-terminal','string-schema','wrong-kind','wrong-row','fractional-pass','negative-fail','string-errors','unknown-status')+@($fields|ForEach-Object {'identity-'+$_})){
 $records=@($original-split'\r?\n'|Where-Object {-not[string]::IsNullOrWhiteSpace($_)}|ForEach-Object {$_|ConvertFrom-Json});$game=$originalGame|ConvertFrom-Json
 switch($case){
  'retained-failure' {$records[-1].rowStatus='FAIL';$records[-1].assertionFailCount=1;$records[-1].recordErrors=@($assertion)}
  'missing-terminal' {$records=@($records|Where-Object phase -CNE 'row-result')}
  'duplicate-terminal' {$records+=@($records[-1])}
  'string-schema' {$records[-1].schemaVersion='2'}
  'wrong-kind' {$records[-1].artifactKind='foreign'}
  'wrong-row' {$records[-1].row='foreign'}
  'fractional-pass' {$records[-1].assertionPassCount=47.5}
  'negative-fail' {$records[-1].assertionFailCount=-1}
  'string-errors' {$records[-1].recordErrors='not-array'}
  'unknown-status' {$records[-1].rowStatus='UNKNOWN'}
 }
 if($case.StartsWith('identity-')){$records[-1].($case.Substring(9))='foreign'}
 [IO.File]::WriteAllText($path,(($records|ForEach-Object {$_|ConvertTo-Json -Depth 100 -Compress})-join[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
 $errorText=$null;$rows=$null;try{$rows=@(Get-KmcChunk6aArtifactRows $path 'chunk4-area-cleanup' $game)}catch{$errorText=$_.Exception.Message}
 $expected=$case-cin@('baseline','retained-failure');if(($null-eq$errorText)-ne$expected){throw ('Unexpected raw row '+$case+': '+$errorText)}
 if($case-ceq'retained-failure'-and($rows.Count-ne1-or$rows[0].status-cne'FAIL'-or$rows[0].assertionFailCount-ne1-or$rows[0].errors.Count-ne1-or$rows[0].errors[0]-cne$assertion)){throw 'Exact failed assertion was not retained'}
 $results+=@([pscustomobject]@{case=$case;expectedPass=$expected;error=$errorText});Write-Host ('PASS area row '+$case)
}
if([IO.File]::ReadAllText((Join-Path $source 'boundary-scenario-evidence.jsonl'))-cne$original){throw 'Original archive changed'}
[pscustomobject]@{status='PASS';checks=$results.Count;atUtc=[DateTime]::UtcNow.ToString('o');scope='Synthetic raw row and retained failure projection; no native or current qualification';results=$results}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $scratch 'row-test-receipt.json') -Encoding UTF8
'AREA ROW READER PASS='+$results.Count+' FAIL=0; draft only'
