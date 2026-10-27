[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7RepeatedWorldEvidence.ps1')
$f=@((Get-Content -LiteralPath (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json') -Raw|ConvertFrom-Json).fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]
$archive=$f.artifact.observations.loadedArchive
function Clone($v){$v|ConvertTo-Json -Depth 25|ConvertFrom-Json}
function NativeOwners {@($archive.Pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=([Guid]$_.Rider.Id).ToString('N');mountId=([Guid]$_.Mount.Id).ToString('N');state='Active';retainsObligations=$true}})}
$pass=0;$fail=0;$checks=[Collections.Generic.List[object]]::new();$timer=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Reject=$false,[string]$Reason=''){
 $errorText=$null;try{& $Body;$ok=-not$Reject}catch{$errorText=$_.Exception.Message;$ok=$Reject-and(-not$Reason-or$errorText.Contains($Reason))}
 if($ok){$script:pass++;'PASS '+$Name}else{$script:fail++;'FAIL '+$Name+': '+$errorText}
 $checks.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$errorText})
}
Check 'native owner N roles match exact archive D actors' {Assert-KmcChunk7ReplayOwners (NativeOwners) $archive $true}
Check 'settled zero-owner cleanup remains strict' {Assert-KmcChunk7ReplayOwners @() $archive $false}
foreach($fault in @('wrong-actor','role-swap','array-owner','array-archive','invalid','uppercase','whitespace','owner-D','archive-N','generation','owner-id','duplicate','missing','state')){
 $change=$fault
 Check ('strict owner identity rejects '+$change) {
  $owners=NativeOwners;$a=Clone $archive
  switch($change){
   'wrong-actor'{$owners[0].riderId=$owners[1].riderId}
   'role-swap'{$v=$owners[0].riderId;$owners[0].riderId=$owners[0].mountId;$owners[0].mountId=$v}
   'array-owner'{$owners[0].riderId=@($owners[0].riderId)}
   'array-archive'{$a.Pairs[0].Rider.Id=@($a.Pairs[0].Rider.Id)}
   'invalid'{$owners[0].riderId='not-an-id'}
   'uppercase'{$a.Pairs[0].Rider.Id='abcdefab-cdef-4abc-8def-abcdefabcdef';$owners[0].riderId='ABCDEFABCDEF4ABC8DEFABCDEFABCDEF'}
   'whitespace'{$owners[0].riderId=' '+$owners[0].riderId}
   'owner-D'{$owners[0].riderId=$a.Pairs[0].Rider.Id}
   'archive-N'{$a.Pairs[0].Rider.Id=$owners[0].riderId}
   'generation'{$owners[0].generation++}
   'owner-id'{$owners[0].ownerId=$owners[1].ownerId}
   'duplicate'{$owners=@($owners[0],$owners[0])}
   'missing'{$owners=@($owners[0])}
   'state'{$owners[0].state='Retired'}
  }
  Assert-KmcChunk7ReplayOwners $owners $a $true
 } $true
}
Check 'cleanup-only adoption cannot be borrowed by repeated-world PASS' {
 $r=[pscustomobject]@{scenario='chunk7-persistence-load';persistenceCase='multi-exploration-three-loads-manual'}
 $a=[pscustomobject]@{status='PASS';observations=[pscustomobject]@{repeatedWorlds=[pscustomobject]@{};repeatedLoadCleanupAdoption=[pscustomobject]@{qualifiesMeasuredLoad=$false}}}
 Assert-KmcChunk7RepeatedWorldObservation $r $a
} $true 'cleanup-only'
$timer.Stop()
$receipt=[ordered]@{status=$(if($fail-eq0){'PASS'}else{'FAIL'});passed=$pass;failed=$fail;seconds=$timer.Elapsed.TotalSeconds;readerSha256=(Get-KmcSha256 (Join-Path $ReaderDirectory 'Chunk7RepeatedWorldEvidence.ps1'));testSha256=(Get-KmcSha256 $PSCommandPath);checks=$checks.ToArray();nativeInvoked=$false;buildInvoked=$false;limits='Actual strict repeated-owner reader and cleanup-only PASS refusal over generated identity fixtures. No native world, cleanup/adoption, load, archive or source qualification.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
'REPEATED OWNER IDENTITY PASS='+$pass+' FAIL='+$fail
if($fail-gt0){throw 'Repeated owner identity regression failed; preserve outputs.'}
