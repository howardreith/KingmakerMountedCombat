# Chunk 7 7E plan section 8: settling a predecessor whose runner verdict FAILed on a reader-only defect.
# Dot-sourced by final-derive.ps1 (Final mode only) and by final-reevaluation-selftest.ps1. Read-only towards the run:
# its only writes are a temporary archive of the run commit under <lab>\worktrees\reeval-old-* (outside every run
# directory), removed afterwards.
#
# The run-map entry adds "reevaluation":{"receipt":"<leaf in analysis-cache/chunk7-continuation>","receiptSha256":"<sha256>"}.
# The receipt is checked against the run: run id, result/evidence/game-result hashes, exactly one runner error, game PASS,
# every restoration flag, the request's evidence root, old reader = the run's commit. The rest is proven here:
#  - the corrected reader commit exists, is in this tree's clean history and descends from the run's commit with no
#    product or platform change (src/, Info.json, version.json, KingmakerMountedCombat.sln, the environment fingerprint);
#  - the run commit's own scripts throw exactly the recorded error (Old), and this tree's scripts pass every
#    offline-replayable remaining check of the runner's verdict path (New), both through final-reevaluate.ps1.
function Invoke-KmcReevaluationGit([string]$Repo,[string[]]$Arguments){
 # Native git under a local Continue: a non-zero exit must reach the explicit refusal, not a PowerShell 5.1 stderr error.
 $prior=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';$out=@(& git -C $Repo @Arguments 2>$null);$code=$LASTEXITCODE}
 finally{$ErrorActionPreference=$prior}
 [pscustomobject]@{code=$code;text=((@($out|ForEach-Object{[string]$_})-join"`n").Trim())}
}
function Invoke-KmcReevaluationChild([string[]]$Arguments){
 $prior=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';$out=@(& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File @Arguments 2>$null)}
 finally{$ErrorActionPreference=$prior}
 @($out|ForEach-Object{[string]$_})
}
# The default verifier: New runs this tree's final-reevaluate.ps1 with this tree's scripts; Old archives the run commit
# into a temporary git-initialised tree two levels below the lab root (the harness locates its lab from there) and runs
# the same verifier with that tree's scripts and fingerprint.
function Invoke-KmcDefaultReevaluationVerifier([string]$Mode,[string]$RunId,[string]$RepoRoot,[string]$LabRoot,[string]$Commit,[string]$ExpectedError){
 $verifier=Join-Path $RepoRoot 'tools\chunk7-7e\final-reevaluate.ps1'
 if($Mode-ceq'New'){return Invoke-KmcReevaluationChild @($verifier,'-RunId',$RunId,'-Mode','New','-ScriptsRoot',(Join-Path $RepoRoot 'scripts'),'-FingerprintPath',(Join-Path $RepoRoot 'planning\ENVIRONMENT-FINGERPRINT.json'),'-LabRoot',$LabRoot)}
 $tmp=Join-Path $LabRoot ('worktrees\reeval-old-'+$Commit.Substring(0,8)+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
 if(Test-Path -LiteralPath $tmp){throw 'Temporary old-reader tree already exists.'}
 [void][IO.Directory]::CreateDirectory($tmp)
 try{
  $tar=Join-Path $tmp '.src.tar'
  # Byte fidelity: LF as the frozen tree materialised it (core.autocrlf off), and the Windows tar (Git's GNU tar reads C: as a host).
  $a=Invoke-KmcReevaluationGit $RepoRoot @('-c','core.autocrlf=false','archive','--format=tar','-o',$tar,$Commit);if($a.code-ne0){throw 'The run commit cannot be archived.'}
  $prior=$ErrorActionPreference;try{$ErrorActionPreference='Continue';& (Join-Path $env:SystemRoot 'System32\tar.exe') -x -f $tar -C $tmp 2>$null;$x=$LASTEXITCODE}finally{$ErrorActionPreference=$prior}
  if($x-ne0){throw 'The run commit archive cannot be extracted.'};[IO.File]::Delete($tar)
  if((Invoke-KmcReevaluationGit $tmp @('init','-q')).code-ne0){throw 'The temporary old-reader tree cannot be initialised.'}
  return Invoke-KmcReevaluationChild @($verifier,'-RunId',$RunId,'-Mode','Old','-ScriptsRoot',(Join-Path $tmp 'scripts'),'-FingerprintPath',(Join-Path $tmp 'planning\ENVIRONMENT-FINGERPRINT.json'),'-ExpectedError',$ExpectedError,'-LabRoot',$LabRoot)
 }finally{Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue;if(Test-Path -LiteralPath $tmp){Write-Warning ('Temporary old-reader tree was not removed: '+$tmp)}}
}
function Test-KmcFinalReaderReevaluation{
 param([string]$Pred,$Entry,$Run,[string]$EvidenceRoot,[string]$CacheRoot,[string]$RepoRoot,[string]$LabRoot,[scriptblock]$Verifier={param($m,$id,$repo,$lab,$c,$e)Invoke-KmcDefaultReevaluationVerifier $m $id $repo $lab $c $e})
 if($Entry-is[string]-or$null-eq$Entry.PSObject.Properties['reevaluation']){return $false}
 $r=$Entry.reevaluation
 if([string]$r.receipt-cnotmatch('^final[0-9]*-B[0-9]+-'+[regex]::Escape($Pred)+'r?-reader-reevaluation\.json$')-or[string]$r.receiptSha256-cnotmatch'^[0-9a-f]{64}$'){throw "Predecessor $Pred re-evaluation reference is malformed."}
 $path=Join-Path $CacheRoot ([string]$r.receipt)
 if(-not(Test-Path -LiteralPath $path -PathType Leaf)-or(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$r.receiptSha256){throw "Predecessor $Pred re-evaluation receipt differs from its recorded hash."}
 $k=[IO.File]::ReadAllText($path,[Text.UTF8Encoding]::new($false,$true))|ConvertFrom-Json
 $root=[IO.Path]::GetFullPath((Join-Path $EvidenceRoot $Run.runId));$res=$Run.result
 if([IO.Path]::GetFullPath([string]$Run.request.evidenceRoot).TrimEnd('\')-cne$root.TrimEnd('\')){throw "Predecessor $Pred request evidence root is not its run directory."}
 $gamePath=Join-Path $root 'runtime-game-result.json';$game=[IO.File]::ReadAllText($gamePath,[Text.UTF8Encoding]::new($false,$true))|ConvertFrom-Json
 $errors=@($res.errors);$compact={param($v)ConvertTo-Json -InputObject $v -Depth 8 -Compress}
 $fields=@('runId','resultSha256','evidenceSha256','runnerStatus','runnerErrors','gameStatus','oldReader','newReader','changedScripts')
 foreach($f in $fields){if($null-eq$k.PSObject.Properties[$f]){throw "Predecessor $Pred re-evaluation receipt lacks $f."}}
 if($k.runId-cne$Run.runId-or$k.resultSha256-cne[string]$Entry.resultSha256-or$null-eq$Run.artifactSha256-or$k.evidenceSha256-cne$Run.artifactSha256-or
    $k.runnerStatus-cne'FAIL'-or$res.status-cne'FAIL'-or$errors.Count-ne1-or(&$compact @($k.runnerErrors))-cne(&$compact $errors)-or
    $k.gameStatus-cne'PASS'-or$game.status-cne'PASS'-or(Get-FileHash -LiteralPath $gamePath -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$res.gameResultSha256-or
    @('modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed'|Where-Object{$res.$_-ne$true}).Count-ne0-or
    $k.oldReader.verdict-cne'FAIL'-or$k.oldReader.error-cne[string]$errors[0]-or$k.oldReader.commit-cne$Run.request.commit-or
    $k.newReader.verdict-cne'PASS'-or[string]$k.newReader.commit-cnotmatch'^[0-9a-f]{40}$'-or$k.newReader.commit-ceq$k.oldReader.commit){
  throw "Predecessor $Pred re-evaluation receipt does not settle its retained runner FAIL."}
 $old=[string]$k.oldReader.commit;$new=[string]$k.newReader.commit
 foreach($c in @($old,$new)){if((Invoke-KmcReevaluationGit $RepoRoot @('cat-file','-e',($c+'^{commit}'))).code-ne0){throw "Predecessor $Pred re-evaluation names a commit missing from this repository: $c"}}
 $head=(Invoke-KmcReevaluationGit $RepoRoot @('rev-parse','HEAD')).text
 $dirty=Invoke-KmcReevaluationGit $RepoRoot @('status','--porcelain','--','scripts','tools','src','planning/ENVIRONMENT-FINGERPRINT.json')
 if($head-cnotmatch'^[0-9a-f]{40}$'-or$dirty.code-ne0-or$dirty.text){throw "Predecessor $Pred re-evaluation needs this deriver's own clean tree."}
 if((Invoke-KmcReevaluationGit $RepoRoot @('merge-base','--is-ancestor',$new,$head)).code-ne0){throw "Predecessor $Pred corrected reader commit is not in this deriver's history."}
 if((Invoke-KmcReevaluationGit $RepoRoot @('merge-base','--is-ancestor',$old,$new)).code-ne0){throw "Predecessor $Pred corrected reader does not descend from the run's commit."}
 if((Invoke-KmcReevaluationGit $RepoRoot @('diff','--quiet',$old,$head,'--','src','Info.json','version.json','KingmakerMountedCombat.sln','planning/ENVIRONMENT-FINGERPRINT.json')).code-ne0){throw "Predecessor $Pred re-evaluation spans a product or platform change; it is not reader-only."}
 # The verified scripts are the receipt's corrected reader: nothing under scripts/ or the verifier changed since it.
 if((Invoke-KmcReevaluationGit $RepoRoot @('diff','--quiet',$new,$head,'--','scripts','tools/chunk7-7e/final-reevaluate.ps1')).code-ne0){throw "Predecessor $Pred scripts changed after the receipt's corrected reader; re-evaluate again."}
 # Every reader change from the run commit to the corrected reader is listed in the receipt by path and blob (the reviewed set).
 $changed=@((Invoke-KmcReevaluationGit $RepoRoot @('diff','--name-only',$old,$new,'--','scripts')).text-split"`n"|Where-Object{$_})
 $listed=@($k.changedScripts|ForEach-Object{[string]$_.path})
 if((&$compact @($changed|Sort-Object -CaseSensitive))-cne(&$compact @($listed|Sort-Object -CaseSensitive))){throw "Predecessor $Pred re-evaluation receipt does not list exactly the reader changes."}
 foreach($row in @($k.changedScripts)){$blob=(Invoke-KmcReevaluationGit $RepoRoot @('rev-parse',($new+':'+[string]$row.path))).text;if($blob-cne[string]$row.blob){throw "Predecessor $Pred re-evaluation receipt names another blob for $($row.path)."}}
 $oldOut=@(& $Verifier 'Old' $Run.runId $RepoRoot $LabRoot $old ([string]$errors[0]))
 if(@($oldOut|Where-Object{$_-ceq'REEVALUATION-OLD-FAIL-REPRODUCED'}).Count-ne1-or@($oldOut|Where-Object{$_-clike'REEVALUATION-FAIL*'}).Count-ne0){throw ("Predecessor $Pred run commit's reader did not reproduce its recorded error: "+($oldOut-join' '))}
 $newOut=@(& $Verifier 'New' $Run.runId $RepoRoot $LabRoot $head '')
 if(@($newOut|Where-Object{$_-ceq'REEVALUATION-PASS'}).Count-ne1-or@($newOut|Where-Object{$_-clike'REEVALUATION-FAIL*'}).Count-ne0){throw ("Predecessor $Pred corrected verdict path did not PASS its immutable evidence: "+($newOut-join' '))}
 return $true
}
