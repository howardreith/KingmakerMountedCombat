[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/chunk7cumulativecopy-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7CumulativeAcquisitionFixtures.ps1')
$script:fixtureLab=Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) 'fixture-lab'
function Get-KmcLabRoot {$script:fixtureLab}
function Json([string]$Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))}
$script:pass=0
function Check([bool]$Ok,[string]$Name){if(-not$Ok){throw $Name};$script:pass++;Write-Host ('PASS '+$Name)}
$cases=@('valid','foreign owner token','foreign scenario','wrong run','wrong source authority','source hash','source length','changed source bytes','destination exists','outside profile root','closed lock')
foreach($name in $cases){
 $run='copy-'+[Guid]::NewGuid().ToString('N');$runRoot=Join-Path $script:fixtureLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory($runRoot)
 $sourcePath=Join-Path $OutputDirectory ($run+'-source.json');Json $sourcePath @{synthetic=$true;purpose='actual JSON copy guard only'}
 $source=[pscustomobject]@{artifactPath=$sourcePath;artifactSha256=(Get-KmcSha256 $sourcePath);artifactLength=[long](Get-Item -LiteralPath $sourcePath).Length;descriptor=[pscustomobject]@{sha256=('a'*64)}}
 $state=Join-Path $script:fixtureLab ('locks/'+$run);$lock=Open-KmcRuntimeLock -StateRoot $state -RunId $run;$closed=$false
 try {
  $owner=[pscustomobject]@{scenario='chunk7-cumulative-acquisition';runId=$run;transactionToken=$lock.Token;sourceSha256=$source.descriptor.sha256}
  switch($name){
   'foreign owner token'{$owner.transactionToken='f'*64}
   'foreign scenario'{$owner.scenario='chunk7-manual-derived-acquisition'}
   'wrong run'{$owner.runId='other'}
   'wrong source authority'{$owner.sourceSha256='f'*64}
   'source hash'{$source.artifactSha256='f'*64}
   'source length'{$source.artifactLength++}
   'changed source bytes'{[IO.File]::AppendAllText($sourcePath,' ')}
   'destination exists'{Json (Join-Path $runRoot 'cumulative-acquisition-source.json') @{preexisting=$true}}
   'outside profile root'{$runRoot=Join-Path $script:fixtureLab ('foreign/'+$run);[void][IO.Directory]::CreateDirectory($runRoot)}
   'closed lock'{Close-KmcRuntimeLock $lock;$closed=$true}
  }
  Json (Join-Path $runRoot 'owner.json') $owner
  $errorText=$null;try{Copy-KmcChunk7CumulativeSourceArtifact $source $runRoot $lock}catch{$errorText=$_.Exception.Message}
  if($name-ceq'valid'){
   Check ($null-eq$errorText-and(Get-KmcSha256 (Join-Path $runRoot 'cumulative-acquisition-source.json'))-ceq$source.artifactSha256) 'actual held-lock JSON copy preserves exact bytes'
   $refused=$false;try{Copy-KmcChunk7CumulativeSourceArtifact $source $runRoot $lock}catch{$refused=$true};Check $refused 'actual second copy refuses existing leaf'
  }else{Check (-not[string]::IsNullOrWhiteSpace($errorText)) ('actual copy refuses '+$name)}
 } finally {if(-not$closed){Close-KmcRuntimeLock $lock}}
 Check (-not(Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock'))) ('actual owned lock released '+$name)
}
@{schemaVersion=1;pass=$script:pass;fail=0;filesystem='actual private files and actual lock ownership';substitutions='Get-KmcLabRoot only maps generated fixture paths';archivesCreated=0;archivesRead=0;nativeExecution=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL=0')
