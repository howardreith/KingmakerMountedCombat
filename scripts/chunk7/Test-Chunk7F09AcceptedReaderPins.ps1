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
# A refusal must carry its exact message (a specific refusal, not any exception).
function Refuses([string]$name,[string]$message,[scriptblock]$body){
 $actual=$null;try{&$body}catch{$actual=$_.Exception.Message}
 if($actual-ceq$message-or($message.EndsWith('*')-and$null-ne$actual-and$actual-clike$message)){$script:pass++;Write-Host ('PASS refuses '+$name)}
 else{$script:fail++;Write-Host ('FAIL '+$name+': expected "'+$message+'", got "'+[string]$actual+'"')}
}
function Copy-Pins{@(ConvertTo-Json -InputObject @($receipt.inputPins) -Depth 4|ConvertFrom-Json|ForEach-Object{$_})}
$changed='F09 cold reader input changed after accepted re-evaluation.'
foreach($pin in $pins){
 $p=$pin
 Good ('exact archived reader '+[IO.Path]::GetFileName($p.path)) {
  $archived=Resolve-KmcF09AcceptedReaderPin $p
  if($archived-ceq$p.path-or-not$archived.StartsWith((Join-Path $lab 'analysis-cache')+'\',[StringComparison]::OrdinalIgnoreCase)-or
   (Get-Item -LiteralPath $archived).Length-ne$p.length-or(Get-KmcSha256 $archived)-cne$p.sha256){throw 'Accepted historical bytes were not verified.'}
 }
 Refuses ('altered historical SHA '+[IO.Path]::GetFileName($p.path)) 'Historical F09 reader identity differs from the accepted receipt.' {$copy=$p|ConvertTo-Json|ConvertFrom-Json;$copy.sha256='0'*64;[void](Resolve-KmcF09AcceptedReaderPin $copy)}
 Refuses ('altered historical length '+[IO.Path]::GetFileName($p.path)) 'Historical F09 reader identity differs from the accepted receipt.' {$copy=$p|ConvertTo-Json|ConvertFrom-Json;$copy.length++;[void](Resolve-KmcF09AcceptedReaderPin $copy)}
}
Good 'unrelated immutable raw evidence retains its original path' {
 $raw=@($receipt.inputPins|Where-Object{$_.path-cmatch'\\runtime-evidence\\'})[0]
 if((Resolve-KmcF09AcceptedReaderPin $raw)-cne$raw.path){throw 'Raw evidence path was redirected.'}
}
Good 'all nine receipt pins verify (evidence at its recorded paths, readers at their hash-verified snapshot)' {Assert-KmcF09AcceptedInputPins @($receipt.inputPins)}
Refuses 'a changed evidence pin' $changed {$copy=Copy-Pins;$e=@($copy|Where-Object{$_.path-cmatch'\\runtime-evidence\\'})[0];if($null-eq$e){throw 'No evidence pin found.'};$e.sha256='0'*64;Assert-KmcF09AcceptedInputPins $copy}
Refuses 'a changed evidence pin at the final boundary' 'F09 cold input changed before qualification returned.' {$copy=Copy-Pins;$e=@($copy|Where-Object{$_.path-cmatch'\\runtime-evidence\\'})[0];$e.length=[long]$e.length+1;Assert-KmcF09AcceptedInputPins $copy 'F09 cold input changed before qualification returned.'}
Refuses 'a dropped pin' 'F09 cold reader input pin set changed.' {Assert-KmcF09AcceptedInputPins @(Copy-Pins|Select-Object -First 8)}
Refuses 'an added pin' 'F09 cold reader input pin set changed.' {$copy=Copy-Pins;Assert-KmcF09AcceptedInputPins @($copy+@($copy[0]))}
Refuses 'a duplicated pin in place of another' 'F09 cold reader input pin is missing or repeated.' {$copy=Copy-Pins;$copy[1]=$copy[0];Assert-KmcF09AcceptedInputPins $copy}
# An altered snapshot reader file: the snapshot is copied to a temporary root under analysis-cache and one byte changed.
Good 'an altered snapshot reader file refuses as changed input' {
 $temp=Join-Path $lab ('analysis-cache\chunk7-continuation\f09-snapshot-selftest-'+[guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($temp)
 try{
  foreach($leaf in @('Chunk7AssistedColdInventoryEvidence.ps1','Chunk7SmallFixtureColdSource.ps1')){Copy-Item -LiteralPath (Join-Path $lab ('analysis-cache\chunk7-continuation\f09-accepted-readers-6fb1eee8\'+$leaf)) -Destination (Join-Path $temp $leaf)}
  Assert-KmcF09AcceptedInputPins @($receipt.inputPins) -SnapshotRoot $temp
  $target=Join-Path $temp 'Chunk7SmallFixtureColdSource.ps1';$bytes=[IO.File]::ReadAllBytes($target);$bytes[0]=$bytes[0] -bxor 1;[IO.File]::WriteAllBytes($target,$bytes)
  $message=$null;try{Assert-KmcF09AcceptedInputPins @($receipt.inputPins) -SnapshotRoot $temp}catch{$message=$_.Exception.Message}
  if($message-cne$changed){throw ('Unexpected outcome: '+[string]$message)}
 }finally{Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue}
}
Good 'the accepted receipt loads at its pinned hash' {$loaded=Read-KmcF09AcceptedReceipt $receiptPath;if($loaded.receipt.sha256-cne'd188012fb1f9a19410a572b67381bfb15be5e813a215a79196ed734b49c59bfc'){throw 'Receipt hash differs.'}}
Good 'a receipt with a different hash refuses' {
 $temp=Join-Path $lab ('analysis-cache\chunk7-continuation\f09-receipt-selftest-'+[guid]::NewGuid().ToString('N')+'.json')
 try{[IO.File]::WriteAllText($temp,([IO.File]::ReadAllText($receiptPath)).Replace('"status":','"status" :'),[Text.UTF8Encoding]::new($false))
  $message=$null;try{[void](Read-KmcF09AcceptedReceipt $temp)}catch{$message=$_.Exception.Message}
  if($message-cne'F09 provenance SHA256 differs.'){throw ('Unexpected outcome: '+[string]$message)}}
 finally{Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue}
}
Refuses 'a tampered receipt path outside analysis-cache' 'F09 separate cold reader receipt escaped its permitted root:*' {[void](Read-KmcF09AcceptedReceipt (Join-Path $lab 'runtime-evidence\20261020-c7b-world71-b53s2-smallcold\runtime-request.json'))}
Refuses 'a traversal-style receipt path' 'F09 separate cold reader receipt escaped its permitted root:*' {[void](Read-KmcF09AcceptedReceipt (Join-Path $lab 'analysis-cache\..\runtime-evidence\x.json'))}
# F2 s1/r33: the harness runs from a frozen worktree, whose own repository root differs from the receipt's recorded root path.
Good 'reader pins bind to the archived snapshot from a frozen worktree as well' {
 $root=Join-Path $lab 'worktrees\chunk7-final-example'
 $original=(Get-Item function:Get-KmcRepositoryRoot).ScriptBlock
 Set-Item -Path function:script:Get-KmcRepositoryRoot -Value ([scriptblock]::Create("'"+$root.Replace("'","''")+"'"))
 try{foreach($pin in $pins){$archived=Resolve-KmcF09AcceptedReaderPin $pin;if($archived-ceq$pin.path-or-not$archived.StartsWith((Join-Path $lab 'analysis-cache')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'A worktree run read the live root reader.'}}}
 finally{Set-Item -Path function:script:Get-KmcRepositoryRoot -Value $original}
 if((Get-KmcRepositoryRoot)-cne$repo){throw 'Get-KmcRepositoryRoot was not restored at script scope.'}
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail)
if($fail){throw 'F09 accepted reader pin checks failed.'}
