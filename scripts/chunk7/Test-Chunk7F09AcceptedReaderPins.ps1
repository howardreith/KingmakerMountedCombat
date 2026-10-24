[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7SmallCalibrationFixtures.ps1')
$lab=Get-KmcLabRoot
$receiptPath=Join-Path $lab 'analysis-cache/chunk7-continuation/codex-takeover/group71-native-b53/b53s2-f09-reader-only-reevaluation.json'
$receipt=Get-Content -Raw -LiteralPath $receiptPath|ConvertFrom-Json
$pins=@($receipt.inputPins|Where-Object{$_.path-cmatch'\\scripts\\runtime\\(?:Chunk7AssistedColdInventoryEvidence|Chunk7SmallFixtureColdSource)\.ps1$'})
if($pins.Count-ne2){throw 'Accepted historical F09 reader pin set differs.'}
$pass=0;$fail=0
function Good([string]$name,[scriptblock]$body){try{&$body;$script:pass++;Write-Host ('PASS '+$name)}catch{$script:fail++;Write-Host ('FAIL '+$name+': '+$_)}}
function Bad([string]$name,[scriptblock]$body){try{&$body;$script:fail++;Write-Host ('FAIL accepted '+$name)}catch{$script:pass++;Write-Host ('PASS refuses '+$name)}}
foreach($pin in $pins){
 $p=$pin
 Good ('exact archived reader '+[IO.Path]::GetFileName($p.path)) {
  $archived=Resolve-KmcF09AcceptedReaderPin $p
  if($archived-ceq$p.path-or-not$archived.StartsWith((Join-Path $lab 'analysis-cache')+'\',[StringComparison]::OrdinalIgnoreCase)-or
   (Get-Item -LiteralPath $archived).Length-ne$p.length-or(Get-KmcSha256 $archived)-cne$p.sha256){throw 'Accepted historical bytes were not verified.'}
 }
 Bad ('altered historical SHA '+[IO.Path]::GetFileName($p.path)) {$copy=$p|ConvertTo-Json|ConvertFrom-Json;$copy.sha256='0'*64;[void](Resolve-KmcF09AcceptedReaderPin $copy)}
 Bad ('altered historical length '+[IO.Path]::GetFileName($p.path)) {$copy=$p|ConvertTo-Json|ConvertFrom-Json;$copy.length++;[void](Resolve-KmcF09AcceptedReaderPin $copy)}
}
Good 'unrelated immutable raw evidence retains its original path' {
 $raw=@($receipt.inputPins|Where-Object{$_.path-cmatch'\\runtime-evidence\\'})[0]
 if((Resolve-KmcF09AcceptedReaderPin $raw)-cne$raw.path){throw 'Raw evidence path was redirected.'}
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail)
if($fail){throw 'F09 accepted reader pin checks failed.'}
