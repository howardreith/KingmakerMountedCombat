[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderPath){$ReaderPath=Join-Path $RepositoryRoot 'scripts/runtime/Chunk7WorldEvidence.ps1'}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. $ReaderPath
$results=New-Object 'System.Collections.Generic.List[object]'
function Trial([string]$Name,[bool]$Accept,[scriptblock]$Body){
    $accepted=$true;$errorText=$null
    try{& $Body}catch{$accepted=$false;$errorText=$_.Exception.Message}
    $pass=$accepted-eq$Accept
    $results.Add([pscustomobject]@{name=$Name;expectedAcceptance=$Accept;actualAcceptance=$accepted;passed=$pass;error=$errorText})
    Write-Host (('{0} {1}'-f $(if($pass){'PASS'}else{'FAIL'}),$Name))
}
function Window([double]$Move,[double]$Standard,[double]$Swift,[long]$Ticks){
    [pscustomobject]@{gameTicks=$Ticks;actors=@([pscustomobject]@{id='rider';move=$Move;standard=$Standard;swift=$Swift;prepared=$false;initiative=0;reactionAllowance=1})}
}
function Copy-Window($Value){$Value|ConvertTo-Json -Depth 10 -Compress|ConvertFrom-Json}
$before=Window 3.0 2.25 0.75 10000000L
$after=Window 1.5 0.75 0.0 25000000L
Trial 'fractional native cooldown decay conserves all three resources' $true {Assert-KmcChunk7Resources $before $after @('rider')}
Trial 'integer-rounded fractional debt must not be accepted' $false {Assert-KmcChunk7Resources (Window 3.0 0.0 0.0 10000000L) (Window 2.0 0.0 0.0 25000000L) @('rider')}
foreach($field in @('move','standard','swift')){
    $changed=Copy-Window $after;$changed.actors[0].$field+=0.5
    Trial ('rounded or extra native debt rejected: '+$field) $false {Assert-KmcChunk7Resources $before $changed @('rider')}
}
$changed=Copy-Window $after;$changed.actors[0].move=1.0
Trial 'premature fractional refund rejected' $false {Assert-KmcChunk7Resources $before $changed @('rider')}
$expired=Window 0.0 0.0 0.0 45000000L
Trial 'elapsed native time can expire each cooldown to zero' $true {Assert-KmcChunk7Resources $before $expired @('rider')}
Trial 'unchanged time requires unchanged native resources' $true {Assert-KmcChunk7Resources $before $before @('rider')}
$within=Copy-Window $after;$within.actors[0].move+=0.029
Trial 'existing 0.03 numerical tolerance retained' $true {Assert-KmcChunk7Resources $before $within @('rider')}
$outside=Copy-Window $after;$outside.actors[0].move+=0.031
Trial 'native debt outside existing tolerance refused' $false {Assert-KmcChunk7Resources $before $outside @('rider')}
$bad=Copy-Window $after;$bad.gameTicks=0L
Trial 'reversed native clock refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.gameTicks=310000001L
Trial 'native resource window beyond thirty seconds refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].move='1.5'
Trial 'string cooldown refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].move=[double]::NaN
Trial 'nonfinite cooldown refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].prepared=$true
Trial 'native preparation change refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].initiative=1
Trial 'native initiative change refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].reactionAllowance=2
Trial 'native reaction refund refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$bad=Copy-Window $after;$bad.actors[0].id='other'
Trial 'different native actor refused' $false {Assert-KmcChunk7Resources $before $bad @('rider')}
$passCount=@($results|Where-Object passed).Count;$failCount=$results.Count-$passCount
if($OutputDirectory){
    [void][IO.Directory]::CreateDirectory($OutputDirectory)
    [ordered]@{readerPath=[IO.Path]::GetFullPath($ReaderPath);readerSha256=(Get-FileHash -LiteralPath $ReaderPath).Hash.ToLowerInvariant();passed=$passCount;failed=$failCount;nativeProof=$false;cases=@($results.ToArray())}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
}
Write-Host ('TOTAL PASS='+$passCount+' FAIL='+$failCount+'; actual resource reader with synthetic native observations')
if($failCount-ne0){throw 'Native resource evidence regression failed.'}
