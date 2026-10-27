Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot '../runtime/Chunk7OwnedPairsFixtures.ps1')

$passed=0
$request=[pscustomobject]@{runId='test-manticore';transactionToken=('a'*64)}
$areas=[pscustomobject]@{schemaVersion=1;runId=$request.runId;profileRoot='C:\test\profile';originalDigest=('b'*64);
    generatedDigest=('c'*64);generatedQuarantine='C:\test\backup\Areas-generated';originalAreasRestoredExactly=$true}
$snapshot=[pscustomobject]@{runId=$request.runId;token=$request.transactionToken;profile=$areas.profileRoot;profileDigest=('d'*64)}
$durable=[pscustomobject]@{schemaVersion=1;runId=$request.runId;token=$request.transactionToken;profile=$areas.profileRoot;
    profileDigest=$snapshot.profileDigest;snapshotSha256=('e'*64);originalDigest=$areas.originalDigest;
    generatedDigest=$areas.generatedDigest;stage='restored'}
$transaction=[pscustomobject]@{runId=$request.runId;token=$request.transactionToken;stagingMode='base-game-only-kmc';phase='restored'}
function Copy-Value($v){return $v|ConvertTo-Json -Depth 8 -Compress|ConvertFrom-Json}
function Admit($a,$s,$d,$t){Assert-KmcChunk7ManticoreAreasCustody $request $a $s $d $t ('e'*64) 'C:\test\backup\Areas-generated'}
function Check([string]$name,[scriptblock]$body,[bool]$reject=$false){
    $failed=$false;try{&$body}catch{$failed=$true}
    if($failed-ne$reject){throw ('Manticore custody '+$name+' did not '+$(if($reject){'refuse'}else{'admit'}))}
    $script:passed++
}
Check 'matching durable transaction' {Admit $areas $snapshot $durable $transaction}
Check 'wrong run' {$x=Copy-Value $durable;$x.runId='other-run';Admit $areas $snapshot $x $transaction} $true
Check 'wrong token' {$x=Copy-Value $transaction;$x.token=('f'*64);Admit $areas $snapshot $durable $x} $true
Check 'wrong profile' {$x=Copy-Value $areas;$x.profileRoot='C:\test\other';Admit $x $snapshot $durable $transaction} $true
Check 'wrong snapshot hash' {$x=Copy-Value $durable;$x.snapshotSha256=('f'*64);Admit $areas $snapshot $x $transaction} $true
Check 'wrong original digest' {$x=Copy-Value $areas;$x.originalDigest=('f'*64);Admit $x $snapshot $durable $transaction} $true
Check 'wrong generated digest' {$x=Copy-Value $durable;$x.generatedDigest=('f'*64);Admit $areas $snapshot $x $transaction} $true
Check 'wrong profile digest' {$x=Copy-Value $durable;$x.profileDigest=('f'*64);Admit $areas $snapshot $x $transaction} $true
Check 'wrong quarantine' {$x=Copy-Value $areas;$x.generatedQuarantine='C:\test\other';Admit $x $snapshot $durable $transaction} $true
Check 'original Areas not restored' {$x=Copy-Value $areas;$x.originalAreasRestoredExactly=$false;Admit $x $snapshot $durable $transaction} $true
Check 'unrestored Areas' {$x=Copy-Value $durable;$x.stage='quarantined';Admit $areas $snapshot $x $transaction} $true
Check 'unrestored transaction' {$x=Copy-Value $transaction;$x.phase='staged';Admit $areas $snapshot $durable $x} $true
Check 'wrong staging mode' {$x=Copy-Value $transaction;$x.stagingMode='installed-mods';Admit $areas $snapshot $durable $x} $true
Write-Host "CHUNK7 MANTICORE CUSTODY PASS=$passed FAIL=0; synthetic read-only source identity; nativeInvoked=false"
