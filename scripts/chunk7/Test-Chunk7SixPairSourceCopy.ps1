[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-copy-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair source copy checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior source-copy evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
Assert-KmcNoGameProcesses
if(Test-Path -LiteralPath (Join-Path (Get-KmcLabRoot) 'runtime-state/active-transaction.lock')){throw 'A live transaction prevents focused copy checks.'}
$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')
$source=Get-KmcChunk7SixPairCheckpointSource $request.fixture
$source|Add-Member sixPairCheckpoint $true;$source|Add-Member sourceRunId $source.qualificationRunId
$checks=[Collections.Generic.List[object]]::new()
foreach($fault in @('none','omitted-marker','owner-seed-bypass','false-marker','other-archive-marker','missing-pin','changed-pin','wrong-producer','wrong-owner','existing-destination')){
 $run='six-copy-test-'+[Guid]::NewGuid().ToString('N');$state=Join-Path $OutputDirectory $run
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$run)
 if(Test-Path -LiteralPath $profile){throw 'Fresh isolated copy-check root required.'}
 [void][IO.Directory]::CreateDirectory($profile);$lock=Open-KmcRuntimeLock $state $run
 try{
  $s=$source|ConvertTo-Json -Depth 32|ConvertFrom-Json
  $owner=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$run;transactionToken=$lock.Token;sourceSha256=$s.descriptor.sha256}
  switch($fault){
   'omitted-marker'{$s.PSObject.Properties.Remove('sixPairCheckpoint')}
   'owner-seed-bypass'{$s.PSObject.Properties.Remove('sixPairCheckpoint');$s|Add-Member ownerSeed $true}
   'false-marker'{$s.sixPairCheckpoint=$false}
   'other-archive-marker'{$s.descriptor.sha256='e'*64;$owner.sourceSha256=$s.descriptor.sha256}
   'missing-pin'{$s.receiptPins=@($s.receiptPins|Select-Object -Skip 1)}
   'changed-pin'{$s.receiptPins[0].sha256='e'*64}
   'wrong-producer'{$s.archiveProducerRunId=$s.qualificationRunId}
   'wrong-owner'{$owner.transactionToken='e'*64}
   'existing-destination'{[IO.File]::WriteAllText((Join-Path $profile 'owned-two-pair-source.json'),'preserved existing sentinel')}
  }
  [IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
  $accepted=$true;$errorText=$null;try{Copy-KmcChunk7OwnedPairsSourceArtifact $s $profile $lock}catch{$accepted=$false;$errorText=$_.Exception.Message}
  $destination=Join-Path $profile 'owned-two-pair-source.json';$ok=$accepted-eq($fault-ceq'none')
  if($accepted){$ok=$ok-and((Get-KmcSha256 $destination)-ceq$source.artifactSha256)}
  elseif($fault-ceq'existing-destination'){$ok=$ok-and([IO.File]::ReadAllText($destination)-ceq'preserved existing sentinel')}
  else{$ok=$ok-and-not(Test-Path -LiteralPath $destination)}
  $checks.Add([pscustomobject]@{name=$fault;pass=$ok;accepted=$accepted;error=$errorText;profile=$profile})
  Write-Output ($(if($ok){'PASS '}else{'FAIL '})+'actual held-lock copy '+$fault+$(if(-not$ok){': '+$errorText}else{''}))
 }finally{Close-KmcRuntimeLock $lock}
}
Assert-KmcChunk7SixPairCheckpointPins $source.receiptPins
$failed=@($checks|Where-Object {-not$_.pass}).Count
[ordered]@{nativeInvoked=$false;modsOrProfileTouched=$false;archiveWritten=$false;status=$(if($failed){'FAIL'}else{'PASS'});results=$checks.ToArray();limits='Real held private locks and unique project-owned staging directories. Actual sixteen-pin qualified source and copy helper; no installed Mods, user profile, protected save or game launch.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('SIX-PAIR COPY PASS='+($checks.Count-$failed)+' FAIL='+$failed)
if($failed){exit 1}
