[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve closure tests.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$ownedRoot=Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $ownedRoot 'src/ScopedPreflightObservation.cs')
. (Join-Path $ownedRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. (Join-Path $ownedRoot 'overlays/scripts/runtime/ScopedPreflightClosure.ps1')
$script:pass=0;$script:fail=0;$script:index=0;$script:rows=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;$script:rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$script:rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Put([string]$Path,[string]$Text){[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path));[IO.File]::WriteAllText($Path,$Text,[Text.UTF8Encoding]::new($false))}
function Pin([string]$Path){[pscustomobject]@{path=$Path;sha256=(Get-FileHash $Path).Hash.ToLowerInvariant();bytes=[long](Get-Item $Path).Length}}
function Fixture {
 $script:index++;$lab=Join-Path $OutputRoot ('lab'+$script:index);$repo=Join-Path $lab 'repo/KingmakerMountedCombat'
 $names=@('.gitignore','CURRENT.md','planning/static.json','scripts/runtime/Original.ps1','src/Original.cs');$original=@()
 foreach($name in $names){$path=Join-Path $repo $name;Put $path ('original '+$name);$pin=Pin $path;$original+=[pscustomobject]@{relativePath=$name;path=$path;sha256=$pin.sha256;bytes=$pin.bytes}}
 $original=@($original|Sort-Object relativePath -CaseSensitive)
 $required=@('scripts/runtime/Invoke-Chunk7ScopedPreflight.ps1','scripts/runtime/ScopedPreflightProof.ps1','scripts/runtime/ScopedPreflightClosure.ps1','scripts/runtime/ScopedPreflightGuards.ps1','scripts/runtime/ScopedPreflightPlatform.ps1','scripts/runtime/ScopedPreflightObservation.cs','scripts/runtime/ScopedPreflightFullAuditReevaluation.ps1');$added=@()
 foreach($name in $required){$path=Join-Path $repo $name;Put $path ('reviewed '+$name);$pin=Pin $path;$added+=[pscustomobject]@{relativePath=$name;sha256=$pin.sha256;bytes=$pin.bytes}}
 $extra=Join-Path $lab 'extra.json';Put $extra '{}'
 $proof=[pscustomobject]@{sha256=('f'*64);documents=[pscustomobject]@{execution=[pscustomobject]@{repositoryRoot=$repo};beforeInputs=[pscustomobject]@{tracked=$original;additional=@((Pin $extra))}}}
 $implementation=[pscustomobject]@{schemaVersion=1;kind='reviewed-closed-preflight-implementation';inheritedProofSha256=('f'*64);expectedHarness=('a'*40);added=$added;changedMarkdown=@();commandFiles=$added}
 [pscustomobject]@{lab=$lab;repo=$repo;proof=$proof;implementation=$implementation;expected=@($names+$required)}
}
function WithLease($Fixture,[scriptblock]$Action){$owned=[Collections.Generic.List[IDisposable]]::new();try{$context=New-KmcScopedCodeLease $Fixture.proof $Fixture.implementation $owned;if($Action){&$Action $context};[void]$context.lease.VerifyUnchanged()}finally{foreach($l in $owned){$l.Dispose()}}}
function Reject([string]$Name,[scriptblock]$Mutation,[string]$Pattern){Check $Name {$f=Fixture;&$Mutation $f;$rejected=$false;try{WithLease $f}catch{if($_.Exception.Message-cnotmatch$Pattern){throw};$rejected=$true};Need $rejected 'Invalid code closure was accepted.'}}
$start=[DateTime]::UtcNow
Check 'reviewed additive descendant retains original code and leases all inputs' {$f=Fixture;WithLease $f {param($c)Assert-KmcScopedTrackedPathSet $f.expected $c.expected;Need ($c.expected.Count-eq12) 'Code closure count'} }
Check 'exact Markdown data update is independently pinned' {$f=Fixture;$old=$f.proof.documents.beforeInputs.tracked|Where-Object relativePath -CEQ 'CURRENT.md';Put $old.path 'new checkpoint';$new=Pin $old.path;$f.implementation.changedMarkdown=@([pscustomobject]@{relativePath='CURRENT.md';beforeSha256=$old.sha256;sha256=$new.sha256;bytes=$new.bytes});WithLease $f {param($c)Need (($c.expected|Where-Object relativePath -CEQ 'CURRENT.md').provenance-ceq'separately-reviewed-current-Markdown-data') 'Markdown was silently historical'} }
Reject 'unlisted old Markdown drift is refused' {param($f)Put (Join-Path $f.repo 'CURRENT.md') 'unreviewed'} 'immutable pin'
Reject 'historical executable edit is refused' {param($f)Put (Join-Path $f.repo 'scripts/runtime/Original.ps1') 'changed guard'} 'immutable pin'
Reject 'executable disguised as Markdown allowance is refused' {param($f)$r=$f.proof.documents.beforeInputs.tracked|Where-Object relativePath -CEQ 'scripts/runtime/Original.ps1';$f.implementation.changedMarkdown=@([pscustomobject]@{relativePath=$r.relativePath;beforeSha256=$r.sha256;sha256=$r.sha256;bytes=$r.bytes})} 'Only explicitly reviewed existing Markdown'
Reject 'wrong previous data hash is refused' {param($f)$r=$f.proof.documents.beforeInputs.tracked|Where-Object relativePath -CEQ 'CURRENT.md';$f.implementation.changedMarkdown=@([pscustomobject]@{relativePath=$r.relativePath;beforeSha256=('a'*64);sha256=$r.sha256;bytes=$r.bytes})} 'previous Markdown identity'
Reject 'unrelated new runtime code cannot borrow proof' {param($f)$f.implementation.added+=([pscustomobject]@{relativePath='scripts/runtime/Bypass.ps1';sha256=('b'*64);bytes=4})} 'unrelated new dependency'
Reject 'new native source cannot borrow proof' {param($f)$f.implementation.added+=([pscustomobject]@{relativePath='src/KingmakerMountedCombat/Bypass.cs';sha256=('b'*64);bytes=4})} 'unrelated new dependency'
Reject 'declared additive file cannot replace existing path' {param($f)$r=$f.proof.documents.beforeInputs.tracked[0];$f.implementation.added+=([pscustomobject]@{relativePath=$r.relativePath;sha256=$r.sha256;bytes=$r.bytes})} 'aliases an existing path'
Reject 'relative traversal cannot add code' {param($f)$f.implementation.added[0].relativePath='../outside.ps1'} 'literal repository-relative'
Reject 'command closure omission refuses' {param($f)$f.implementation.commandFiles=@($f.implementation.commandFiles|Select-Object -Skip 1)} 'executable closure is incomplete'
Reject 'unknown command role refuses' {param($f)$copy=$f.implementation.commandFiles|ConvertTo-Json|ConvertFrom-Json;$copy[0].relativePath='scripts/runtime/Unknown.ps1';$f.implementation.commandFiles=$copy} 'absent or ambiguous'
Reject 'wrong inherited proof reference refuses' {param($f)$f.implementation.inheritedProofSha256='d'*64} 'implementation full proof'
Reject 'numeric-text added size refuses' {param($f)$f.implementation.added[0].bytes=[string]$f.implementation.added[0].bytes} 'added bytes'
Reject 'added dependency hash drift refuses before imports' {param($f)Put (Join-Path $f.repo $f.implementation.added[0].relativePath) 'drift'} 'immutable pin'
Reject 'planted ignored native source refuses before consumer' {param($f)Put (Join-Path $f.repo 'src/Hidden.cs') 'planted'} 'Untracked or ignored'
Reject 'planted runtime import refuses before consumer' {param($f)Put (Join-Path $f.repo 'scripts/runtime/Hidden.ps1') 'planted'} 'Untracked or ignored'
Reject 'planted planning dependency refuses before consumer' {param($f)Put (Join-Path $f.repo 'planning/Hidden.json') '{}'} 'Untracked or ignored'
Reject 'unknown extension cannot evade code namespace' {param($f)Put (Join-Path $f.repo 'scripts/runtime/Hidden.payload') 'planted'} 'Untracked or ignored'
Reject 'scripts obj namespace is not a native source exclusion' {param($f)Put (Join-Path $f.repo 'scripts/obj/Hidden.ps1') 'planted'} 'Untracked or ignored'
Check 'native src obj exclusion remains observed but does not join compiler closure' {$f=Fixture;Put (Join-Path $f.repo 'src/obj/generated.txt') 'not-consumed';WithLease $f {param($c)Need (@($c.lease.Before|Where-Object{$_.Path.EndsWith('generated.txt')}).Count-eq1) 'Generated native subtree lost observation'}}
Check 'new code path during consumer is caught' {$f=Fixture;$rejected=$false;try{WithLease $f {param($c)Put (Join-Path $f.repo 'scripts/runtime/New.ps1') 'new'}}catch{Need ($_.Exception.Message-cmatch'New observed file') 'Wrong new-file refusal';$rejected=$true};Need $rejected 'Runtime dependency growth accepted'}
Check 'metadata-restored same-size source mutation before consumer is caught' {$f=Fixture;$p=Join-Path $f.repo 'src/Original.cs';$time=(Get-Item $p).LastWriteTimeUtc;$text=[IO.File]::ReadAllText($p).Replace('original','mutation');Put $p $text;(Get-Item $p).LastWriteTimeUtc=$time;$rejected=$false;try{WithLease $f}catch{Need ($_.Exception.Message-cmatch'immutable pin') 'Wrong same-size refusal';$rejected=$true};Need $rejected 'Hash bypassed'}
Check 'tracked set addition is refused independently' {$f=Fixture;$e=@(Get-KmcScopedCurrentCode $f.proof $f.implementation);$rejected=$false;try{Assert-KmcScopedTrackedPathSet ($f.expected+'new.txt') $e}catch{$rejected=$true};Need $rejected 'Tracked addition accepted'}
Check 'same-count tracked rename is refused independently' {$f=Fixture;$e=@(Get-KmcScopedCurrentCode $f.proof $f.implementation);$names=[string[]]$f.expected.Clone();$names[0]='renamed.txt';$rejected=$false;try{Assert-KmcScopedTrackedPathSet $names $e}catch{$rejected=$true};Need $rejected 'Tracked rename accepted'}
Check 'duplicate tracked spelling is refused' {$f=Fixture;$e=@(Get-KmcScopedCurrentCode $f.proof $f.implementation);$names=[string[]]$f.expected.Clone();$names[0]=$names[1];$rejected=$false;try{Assert-KmcScopedTrackedPathSet $names $e}catch{$rejected=$true};Need $rejected 'Duplicate accepted'}
function Case { [pscustomobject]@{schemaVersion=1;kind='closed-chunk7-preflight-case';runId='future-cold';scenario='chunk7-bootstrap-cold-inventory';baseGameOnly=$true;saveAccessAllowed=$true;selection=[pscustomobject]@{path='C:\Private\selection.json';sha256=('a'*64);bytes=100};suiteId='same-suite';timeoutSeconds=600} }
Check 'only exact cold case is admitted' {Assert-KmcScopedCase (Case)}
Check 'exact Manual case is admitted' {$p=Case;$p.scenario='chunk7-bootstrap-manual-seed';Assert-KmcScopedCase $p}
foreach($mutation in @('old-scenario','no-base','no-save','text-boolean','text-timeout','unknown-key','run-traversal')){Check ('closed case refuses '+$mutation){$p=Case;switch($mutation){'old-scenario'{$p.scenario='chunk7-persistence-load'}'no-base'{$p.baseGameOnly=$false}'no-save'{$p.saveAccessAllowed=$false}'text-boolean'{$p.baseGameOnly='true'}'text-timeout'{$p.timeoutSeconds='600'}'unknown-key'{$p|Add-Member NoteProperty skipGuards $true}'run-traversal'{$p.runId='../outside'}};$rejected=$false;try{Assert-KmcScopedCase $p}catch{$rejected=$true};Need $rejected 'Foreign case admitted'}}
foreach($id in @('.','..','source.','.hidden','CON','nul.txt','LPT1')){Check ('unambiguous native child rejects '+$id){$p=Case;$p.runId=$id;$refused=$false;try{Assert-KmcScopedCase $p}catch{Need ($_.Exception.Message-cmatch'run identity') 'Wrong lexical refusal';$refused=$true};Need $refused 'Native alias accepted'}}
$end=[DateTime]::UtcNow
[ordered]@{kind='actual-bounded-filesystem-code-closure-tests';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=($end-$start).TotalSeconds;rows=@($script:rows);sourceSha256=(Get-FileHash (Join-Path $ownedRoot 'overlays/scripts/runtime/ScopedPreflightClosure.ps1')).Hash.ToLowerInvariant();scope='Actual code-closure and case-discovery methods over generated records and real private files; no shared guard, Git, native or archive substitution/execution';fullPreflightProven=$false}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ("Closure: {0} PASS / {1} FAIL"-f$script:pass,$script:fail)
if($script:fail-ne0){throw 'Private code closure checks failed.'}
