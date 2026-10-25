# Fixture-driven self-test of the deriver's reader-only re-evaluation admission (final-reevaluation.ps1).
# Synthetic run directories and receipts in a temporary root; the verdict-path verifier is stubbed (it is exercised
# natively by the r15 re-evaluation, not here); git queries run read-only against this repository and need a clean
# scripts/tools/src tree. Writes only under its own temporary root.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'final-reevaluation.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$runCommit='fa8191138118a61708e6fce9d02906f30dc2bf91'       # F2 freeze commit (product unchanged since)
$readerCommit='95a69cfc'                                     # the corrected reader
$productCommit='3b0e4f6a'                                    # before 2e48f452, which changed src/
$readerCommit=(Invoke-KmcReevaluationGit $repo @('rev-parse',$readerCommit)).text;$productCommit=(Invoke-KmcReevaluationGit $repo @('rev-parse',$productCommit)).text
$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-reeval-selftest-'+[guid]::NewGuid().ToString('N'));$ev=Join-Path $root 'runtime-evidence';$cache=Join-Path $root 'cache'
[void][IO.Directory]::CreateDirectory($cache);$passes=0;$fails=@()
$utf8=[Text.UTF8Encoding]::new($false)
function Sha([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function New-Fixture([string]$Name,[scriptblock]$Mutate={}){
 $id='20261025-c7e-final-B4-r15';$dir=Join-Path $ev $id;if(Test-Path $dir){Remove-Item $dir -Recurse -Force};[void][IO.Directory]::CreateDirectory($dir)
 $error0='Retained native actor was released before or after its first legitimate native round.'
 $f=[ordered]@{id=$id;dir=$dir;error0=$error0;errors=@($error0);gameStatus='PASS';flags=[ordered]@{modsRestored=$true;saveProtectionPassed=$true;baselineImmutable=$true;workingRestored=$true;saveWriteAllowlistPassed=$true}
  oldCommit=$runCommit;newCommit=$readerCommit;receiptLeaf='final2-B4-r15-reader-reevaluation.json';evidenceSha=('e'*64);receiptEvidenceSha=('e'*64)
  badReceiptHash=$false;evidenceRoot=$dir;verifierOld='REEVALUATION-OLD-FAIL-REPRODUCED';verifierNew='REEVALUATION-PASS'}
 &$Mutate $f
 [IO.File]::WriteAllText((Join-Path $dir 'runtime-game-result.json'),(ConvertTo-Json ([ordered]@{status=$f.gameStatus}) -Compress),$utf8)
 $result=[ordered]@{runId=$id;status='FAIL';errors=$f.errors;gameResultSha256=(Sha (Join-Path $dir 'runtime-game-result.json'))};foreach($k in $f.flags.Keys){$result[$k]=$f.flags[$k]}
 [IO.File]::WriteAllText((Join-Path $dir 'runtime-result.json'),(ConvertTo-Json $result -Depth 5 -Compress),$utf8)
 $resultSha=Sha (Join-Path $dir 'runtime-result.json')
 $receipt=[ordered]@{runId=$id;resultSha256=$resultSha;evidenceSha256=$f.receiptEvidenceSha;runnerStatus='FAIL';runnerErrors=@($f.error0);gameStatus='PASS'
  oldReader=[ordered]@{commit=$f.oldCommit;verdict='FAIL';error=$f.error0};newReader=[ordered]@{commit=$f.newCommit;verdict='PASS'}}
 $rp=Join-Path $cache 'final2-B4-r15-reader-reevaluation.json';[IO.File]::WriteAllText($rp,(ConvertTo-Json $receipt -Depth 5),$utf8)
 $entry=[pscustomobject]@{runId=$id;resultSha256=$resultSha;readerVerdict='PASS';restorationVerified=$true
  reevaluation=[pscustomobject]@{receipt=$f.receiptLeaf;receiptSha256=$(if($f.badReceiptHash){'0'*64}else{Sha $rp})}}
 $run=[pscustomobject]@{runId=$id;artifactSha256=$f.evidenceSha;request=[pscustomobject]@{commit=$f.oldCommit;evidenceRoot=$f.evidenceRoot}
  result=([IO.File]::ReadAllText((Join-Path $dir 'runtime-result.json'))|ConvertFrom-Json)}
 $vo=$f.verifierOld;$vn=$f.verifierNew
 [pscustomobject]@{name=$Name;entry=$entry;run=$run;verifier={param($m,$i,$r,$l,$c,$e)if($m-ceq'Old'){$vo}else{$vn}}.GetNewClosure()}
}
function Expect([string]$Name,[scriptblock]$Mutate,[bool]$Admit,[string]$Pattern=''){
 $x=New-Fixture $Name $Mutate;$ok=$false;$detail=''
 try{$v=Test-KmcFinalReaderReevaluation -Pred 'r15' -Entry $x.entry -Run $x.run -EvidenceRoot $ev -CacheRoot $cache -RepoRoot $repo -LabRoot $root -Verifier $x.verifier;$ok=$Admit-and$v-eq$true;$detail='returned '+$v}
 catch{$detail=$_.Exception.Message;$ok=-not$Admit-and(-not$Pattern-or$detail-match$Pattern)}
 if($ok){$script:passes++;"PASS $Name"}else{$script:fails+=$Name;"FAIL $Name : $detail"}
}
try{
 Expect 'a reader-only re-evaluation settles the predecessor' {} $true
 Expect 'two runner errors refuse' {param($f)$f.errors=@($f.error0,'Later failure.')} $false 'does not settle'
 Expect 'a false restoration flag refuses' {param($f)$f.flags.workingRestored=$false} $false 'does not settle'
 Expect 'a game FAIL refuses' {param($f)$f.gameStatus='FAIL'} $false 'does not settle'
 Expect 'a product change refuses' {param($f)$f.oldCommit=$productCommit} $false 'product or platform change'
 Expect 'an evidence-hash mismatch refuses' {param($f)$f.receiptEvidenceSha=('d'*64)} $false 'does not settle'
 Expect 'a bad receipt hash refuses' {param($f)$f.badReceiptHash=$true} $false 'differs from its recorded hash'
 Expect 'a traversal-style receipt name refuses' {param($f)$f.receiptLeaf='..\cache\final2-B4-r15-reader-reevaluation.json'} $false 'malformed'
 Expect 'a missing corrected commit refuses' {param($f)$f.newCommit=('a'*40)} $false 'missing from this repository'
 Expect 'a request evidence root elsewhere refuses' {param($f)$f.evidenceRoot='C:\elsewhere\20261025-c7e-final-B4-r15'} $false 'evidence root'
 Expect 'an old reader that does not reproduce the error refuses' {param($f)$f.verifierOld='REEVALUATION-FAIL old reader did not throw exactly the recorded error'} $false 'did not reproduce'
 Expect 'a corrected verdict path that fails refuses' {param($f)$f.verifierNew='REEVALUATION-FAIL owned post-game: x'} $false 'did not PASS'
 $x=New-Fixture 'no reference';$x.entry.PSObject.Properties.Remove('reevaluation')
 if((Test-KmcFinalReaderReevaluation -Pred 'r15' -Entry $x.entry -Run $x.run -EvidenceRoot $ev -CacheRoot $cache -RepoRoot $repo -LabRoot $root -Verifier $x.verifier)-eq$false){$passes++;'PASS an entry without a re-evaluation reference is not settled'}else{$fails+='no reference';'FAIL no reference'}
}finally{Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue}
"REEVALUATION SELFTEST PASS=$passes FAIL=$($fails.Count)"
if($fails.Count-ne0){exit 1}
