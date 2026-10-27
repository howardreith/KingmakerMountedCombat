[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'};if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/acquired-identity-'+[Guid]::NewGuid().ToString('N'))}
$identityOutput=$OutputDirectory;$testEntry=Join-Path $PSScriptRoot 'Test-Chunk7AcquiredManualEvidence.ps1'
. $testEntry -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixtureDirectory $FixtureDirectory -OutputDirectory $identityOutput -FixturesOnly
[void][IO.Directory]::CreateDirectory($identityOutput);$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Accept){$ok=$true;try{&$Body}catch{$ok=$false};if($ok-eq$Accept){$script:pass++;Write-Host ('PASS '+$Name)}else{$script:fail++;Write-Host ('FAIL '+$Name)}}
function Relabel($f,[bool]$Negative){$o=$f.artifact.observations
 function Map([int]$n){if($n-eq0){return 0};if($Negative){return -$n}else{return $n+1000}}
 foreach($w in @($o.loadedWorld,$o.fieldWorld,$o.savedWorld,$o.settledWorld)){
  foreach($k in @('game','saveManager','player','state','areaState','mainState')){$w.$k=Map $w.$k};foreach($a in $w.party){foreach($k in @('object','descriptor','view')){$a.$k=Map $a.$k}}
 }
 foreach($k in @('game','saveManager','menuPlayer')){$o.beforeLoad.$k=Map $o.beforeLoad.$k}
 foreach($e in $o.nativeLoad.events){foreach($k in @('game','saveManager','player')){$e.$k=Map $e.$k}}
 $o.nativeLoad.identities.ids=@($o.nativeLoad.identities.ids|ForEach-Object{Map $_})
 foreach($row in @($o.loadedOwner,$o.fieldOwner,$o.savedOwner)){foreach($k in @('actorObject','actorDescriptor','actorView','petObject','petDescriptor','petView','featureObject','rankObject')){$row.$k=Map $row.$k}}
 foreach($e in $o.nativeSave.events){foreach($k in @('game','saveManager','player','requested','descriptor','scope','iterator','worker')){if($null-ne$e.PSObject.Properties[$k]){$e.$k=Map $e.$k}}}
 $o.saveRequested.descriptor=Map $o.saveRequested.descriptor
 foreach($k in @('requestedDescriptor','writtenDescriptor')){$o.nativeWrite.$k=Map $o.nativeWrite.$k}
 foreach($row in $o.settledDescriptors){$row.descriptor=Map $row.descriptor}
 $o.reservation.sourceDescriptor=Map $o.reservation.sourceDescriptor;foreach($row in $o.reservation.sourceDescriptors){$row.descriptor=Map $row.descriptor}
}
foreach($paid in @($false,$true)){
 foreach($negative in @($false,$true)){Check ('complete signed native graph paid='+$paid+' negative='+$negative) {$f=New-AcquiredManualFixture $paid;Relabel $f $negative;Check-AcquiredManual $f} $true}
 foreach($bad in @('zero','overflow','numeric-text','collision','capacity','missing')){Check ('refuse invalid identity '+$bad+' paid='+$paid) {
  $f=New-AcquiredManualFixture $paid
  switch($bad){'zero'{$f.artifact.observations.savedOwner.featureObject=0};'overflow'{$f.artifact.observations.savedOwner.featureObject=[long]2147483648};'numeric-text'{$f.artifact.observations.savedOwner.featureObject='23'};'collision'{$f.artifact.observations.savedOwner.featureObject=$f.artifact.observations.savedOwner.rankObject};'capacity'{$f.artifact.observations.nativeLoad.identities.capacity=129};'missing'{$f.artifact.observations.nativeLoad.identities.ids=@(1..23);$f.artifact.observations.nativeLoad.identities.retainedCount=23}}
  Check-AcquiredManual $f
 } $false}
}
$watch.Stop();[ordered]@{status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=$watch.Elapsed.TotalSeconds;readerSha256=(Get-FileHash -LiteralPath (Join-Path $ReaderDirectory 'Chunk7AcquiredManualEvidence.ps1')).Hash.ToLowerInvariant();nativeProof=$false;archiveReads=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $identityOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail-gt0){throw 'Acquired Manual signed identity failed.'}
