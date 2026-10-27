[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-copy-'+[Guid]::NewGuid().ToString('N'))}
$script:copyLab=[IO.Path]::GetFullPath($OutputDirectory);if(-not$script:copyLab.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Generated JSON/lock fixtures must stay in analysis-cache.'};[void][IO.Directory]::CreateDirectory($script:copyLab)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7SmallCalibrationFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OrdinaryFieldFixtures.ps1')
foreach($name in @('Copy-KmcChunk7SmallManualSourceArtifact','Copy-KmcChunk7FieldSourceArtifact')){if(-not(Get-Command $name -CommandType Function -ErrorAction SilentlyContinue)){throw ('Missing actual source function '+$name)}}
function Get-KmcLabRoot {$script:copyLab}
function Write-Json($Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-KmcChunk7Canonical $Value),[Text.UTF8Encoding]::new($false))}
$script:pass=0;$script:fail=0;$script:legacyReason=$null
function Check([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Refuse([scriptblock]$Action){$errorText=$null;try{&$Action}catch{if($_.Exception-is[Management.Automation.CommandNotFoundException]){throw};$errorText=[string]$_};if(-not$errorText){throw 'Malformed copy was admitted.'};return $errorText}
function Case([string]$Mutation){
 $run='small-copy-'+[Guid]::NewGuid().ToString('N');$profile=Join-Path $script:copyLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory($profile)
 $artifact=Join-Path $script:copyLab ($run+'-source.json');Write-Json $artifact ([pscustomobject]@{synthetic=$true;role='plain-json-copy-only'})
 $source=[pscustomobject]@{artifactPath=$artifact;artifactSha256=(Get-KmcSha256 $artifact);artifactLength=[long](Get-Item -LiteralPath $artifact).Length;descriptor=[pscustomobject]@{sha256='a'*64}}
 $owner=[pscustomobject]@{runId=$run;scenario='chunk7-small-profile-calibration';transactionToken='';sourceSha256=$source.descriptor.sha256}
 $lock=Open-KmcRuntimeLock (Join-Path $script:copyLab 'private-lock') $run
 try{
  $owner.transactionToken=$lock.Token
  switch($Mutation){'foreign-family'{$owner.scenario='chunk7-ordinary-field-admission'}'wrong-run'{$owner.runId='foreign'}'wrong-token'{$owner.transactionToken='b'*64}'wrong-source'{$owner.sourceSha256='b'*64}'source-byte-drift'{[IO.File]::AppendAllText($artifact,'changed')}'wrong-length'{$source.artifactLength++}'wrong-lock-token'{$lock.Token='b'*64}'existing-output'{Write-Json (Join-Path $profile 'small-acquired-manual-source.json') @{foreign=$true}}}
  Write-Json (Join-Path $profile 'owner.json') $owner
  $before=Get-KmcSha256 $artifact
  if($Mutation-ceq'legacy-refusal'){$script:legacyReason=Refuse {Copy-KmcChunk7FieldSourceArtifact $source $profile $lock};if($script:legacyReason-cne'Field source copy lacks its exact locked isolated profile.'){throw ('Unexpected legacy refusal: '+$script:legacyReason)};if(Test-Path -LiteralPath (Join-Path $profile 'ordinary-acquisition-source.json')){throw 'Legacy helper copied under Small owner.'};return}
  if($Mutation){[void](Refuse {Copy-KmcChunk7SmallManualSourceArtifact $source $profile $lock});if($Mutation-cne'existing-output'-and(Test-Path -LiteralPath (Join-Path $profile 'small-acquired-manual-source.json'))){throw 'Refused intake left a copied source.'}}
  else{Copy-KmcChunk7SmallManualSourceArtifact $source $profile $lock;if((Get-KmcSha256 (Join-Path $profile 'small-acquired-manual-source.json'))-cne$source.artifactSha256){throw 'Copied source identity differs.'};[void](Refuse {Copy-KmcChunk7SmallManualSourceArtifact $source $profile $lock})}
  if((Get-KmcSha256 $artifact)-cne$before){throw 'Copy changed source bytes.'}
 }finally{if($Mutation-ceq'wrong-lock-token'){$lock.Token=$owner.transactionToken};Close-KmcRuntimeLock $lock}
}
Check 'exact actual private lock and Small owner copy plain JSON once' {Case ''}
foreach($mutation in @('foreign-family','wrong-run','wrong-token','wrong-source','source-byte-drift','wrong-length','wrong-lock-token','existing-output')){Check ('copy refuses '+$mutation) {Case $mutation}}
Check 'unchanged legacy Field copy helper refuses Small family' {Case 'legacy-refusal'}
@{pass=$script:pass;fail=$script:fail;sourceSha256=(Get-FileHash -LiteralPath (Join-Path $ReaderDirectory 'Chunk7SmallCalibrationFixtures.ps1')).Hash.ToLowerInvariant();legacyFieldHelperUnchanged=$true;legacyCalibrationCopyRefusal=$script:legacyReason;archiveReads=0;archivesCreated=0;nativeInvoked=$false;boundary='Actual lock/owner/hash/path/copy functions with plain generated JSON only'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $script:copyLab 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){throw 'Small Manual copy checks failed.'}
