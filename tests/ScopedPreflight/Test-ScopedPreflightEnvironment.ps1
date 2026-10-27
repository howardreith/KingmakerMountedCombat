[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing platform environment evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$homeRoot=Split-Path $PSScriptRoot -Parent
$testOriginalTemp=$env:TEMP;$testOriginalTmp=$env:TMP;[void][IO.Directory]::CreateDirectory((Join-Path $OutputRoot 'compiler-temp'));$env:TEMP=Join-Path $OutputRoot 'compiler-temp';$env:TMP=$env:TEMP
Add-Type -Path (Join-Path $homeRoot 'src/ScopedPreflightObservation.cs')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1')
$script:pass=0;$script:fail=0;$rows=[Collections.Generic.List[object]]::new()
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;$rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Refuse([scriptblock]$Body,[string]$Pattern){$caught=$false;try{&$Body}catch{if($_.Exception.Message-cnotmatch$Pattern){throw};$caught=$true};Need $caught 'Expected refusal did not occur.'}
function Hash($Path){(Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant()}
$started=[DateTime]::UtcNow
$git='C:\Program Files\Git\cmd\git.exe';$platform=[pscustomobject]@{git=$git;gitAutoCrlf='true'}
$repo=Join-Path $OutputRoot 'repo';$frozen=Join-Path $OutputRoot 'frozen';$scratch=Join-Path $OutputRoot 'scratch'
foreach($p in @($repo,$scratch,(Join-Path $scratch 'temp'),(Join-Path $scratch 'no-hooks'))){[void][IO.Directory]::CreateDirectory($p)}
[IO.File]::WriteAllBytes((Join-Path $scratch 'git-empty.config'),[byte[]]@())
$owners=[Collections.Generic.List[object]]::new();$held=[Collections.Generic.List[IDisposable]]::new()
$originalGitOptional=[Environment]::GetEnvironmentVariable('GIT_OPTIONAL_LOCKS','Process')
$originalPath=$env:PATH;$originalTemp=$env:TEMP
$env:GIT_SCOPED_TEST='private-test-value';$env:GIT_CONFIG_PARAMETERS="'core.fsmonitor=synthetic-ignored-helper'"
$env:GIT_OPTIONAL_LOCKS='1'
try{
 $state=Enter-KmcScopedEnvironment $platform $repo $frozen $scratch $owners
 Check 'environment custody assigned before mutations' {Need ($owners.Count-eq1-and[object]::ReferenceEquals($owners[0],$state)) 'Owner absent.'}
 Check 'inherited Git override removed without recording its value' {Need ($null-eq[Environment]::GetEnvironmentVariable('GIT_SCOPED_TEST','Process')-and$null-eq[Environment]::GetEnvironmentVariable('GIT_CONFIG_PARAMETERS','Process')) 'Inherited Git input survived.'}
 Check 'optional index updates disabled' {Need ($env:GIT_OPTIONAL_LOCKS-ceq'0') 'Optional locks not disabled.'}
 Check 'fresh environment matches exact closed settings' {Assert-KmcScopedEnvironment $state}
 Check 'actual Git global/system isolation' {Need ((&$git config --get core.fsmonitor)-ceq'false') 'fsmonitor remains active.';Need ((&$git config --get core.autocrlf)-ceq'true') 'Text normalization differs.';Need ((&$git config --get protocol.allow)-ceq'never') 'Network transport not closed.'}
 &$git init --quiet $repo;if($LASTEXITCODE-ne0){throw 'Private Git init failed.'}
 [IO.File]::WriteAllText((Join-Path $repo 'a.txt'),('test'+[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
 &$git -C $repo add a.txt;if($LASTEXITCODE-ne0){throw 'Private Git add failed.'}
 &$git -C $repo -c user.name=ScopedTest -c user.email=scoped@example.invalid commit --quiet -m initial;if($LASTEXITCODE-ne0){throw 'Private Git commit failed.'}
 &$git -C $repo worktree add --quiet --detach $frozen HEAD;if($LASTEXITCODE-ne0){throw 'Private frozen worktree creation failed.'}
 $proof=[pscustomobject]@{documents=[pscustomobject]@{execution=[pscustomobject]@{repositoryRoot=$repo;frozenSourceRoot=$frozen}}}
 $metadata=New-KmcScopedGitLease $proof $platform $held
 Check 'actual repository and worktree metadata admitted under read leases' {Need ($held.Count-eq1-and$metadata.localConfigSha256-ceq(Hash (Join-Path $repo '.git/config'))) 'Metadata lease absent.'}
 Check 'actual status and reachability under held index do not write metadata' {
  $status=@(&$git -C $repo status --porcelain --untracked-files=all);Need ($LASTEXITCODE-eq0-and$status.Count-eq0) 'Actual readonly status failed.'
  $commit=(&$git -C $repo rev-parse HEAD).Trim();&$git -C $repo merge-base --is-ancestor $commit $commit;Need ($LASTEXITCODE-eq0) 'Actual ancestor test failed.'
  [void]$metadata.lease.VerifyUnchanged()
 }
 Check 'held config and index reject writes' {foreach($p in @((Join-Path $repo '.git/config'),(Join-Path $repo '.git/index'))){Refuse {[IO.File]::AppendAllText($p,'x')} 'used by another process|access|sharing'}}
 Check 'new Git environment input is refused' {$env:GIT_SCOPED_INTRUDER='x';try{Refuse {Assert-KmcScopedEnvironment $state} 'unreviewed Git'}finally{[Environment]::SetEnvironmentVariable('GIT_SCOPED_INTRUDER',$null,'Process')}}
 Check 'changed fixed setting is refused' {$env:GIT_OPTIONAL_LOCKS='1';try{Refuse {Assert-KmcScopedEnvironment $state} 'environment changed'}finally{$env:GIT_OPTIONAL_LOCKS='0'}}
 $metadata.lease.Dispose();$held.Clear()
 Check 'local include is refused without reading target config' {
  $config=Join-Path $repo '.git/config';$before=[IO.File]::ReadAllText($config)
  try{[IO.File]::AppendAllText($config,([Environment]::NewLine+'[include]'+[Environment]::NewLine+' path = nonexistent-secret-config'+[Environment]::NewLine));Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'execution-affecting or unreviewed'}
  finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::WriteAllText($config,$before,[Text.UTF8Encoding]::new($false))}
 }
 Check 'local fsmonitor helper disabled and admission still refuses it' {
  $config=Join-Path $repo '.git/config';$before=[IO.File]::ReadAllText($config);$marker=Join-Path $OutputRoot 'unexpected-helper-ran';$hook=Join-Path $OutputRoot 'fsmonitor.cmd'
  [IO.File]::WriteAllText($hook,('@echo invoked>"'+$marker+'"'),[Text.UTF8Encoding]::new($false))
  try{
   &$git -C $repo config core.fsmonitor $hook;if($LASTEXITCODE-ne0){throw 'Private config setup failed.'}
   $status=@(&$git -C $repo status --porcelain);Need ($LASTEXITCODE-eq0-and-not(Test-Path -LiteralPath $marker)) 'External helper ran.'
   Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'execution-affecting or unreviewed'
  }finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::WriteAllText($config,$before,[Text.UTF8Encoding]::new($false))}
 }
 Check 'object alternates are refused before Git consumes them' {
  $p=Join-Path $repo '.git/objects/info/alternates';[IO.File]::WriteAllText($p,'C:/unselected-object-store',[Text.UTF8Encoding]::new($false))
  try{Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'unsupported external'}finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::Delete($p)}
 }
 Check 'foreign frozen Git pointer is refused' {
  $p=Join-Path $frozen '.git';$old=[IO.File]::ReadAllText($p);$attributes=[IO.File]::GetAttributes($p);[IO.File]::SetAttributes($p,[IO.FileAttributes]::Normal);[IO.File]::WriteAllText($p,'gitdir: '+$repo,[Text.UTF8Encoding]::new($false))
  try{Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'escaped its declared root'}finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::WriteAllText($p,$old,[Text.UTF8Encoding]::new($false));[IO.File]::SetAttributes($p,$attributes)}
 }
 Check 'frozen common-directory indirection cannot escape the held Git store' {
  $p=Join-Path $metadata.frozenGitDirectory 'commondir';$old=[IO.File]::ReadAllText($p)
  try{[IO.File]::WriteAllText($p,'../../../unselected-store',[Text.UTF8Encoding]::new($false));Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'frozen common Git directory'}
  finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::WriteAllText($p,$old,[Text.UTF8Encoding]::new($false))}
 }
 Check 'frozen reverse pointer must resolve its exact admitted worktree' {
  $p=Join-Path $metadata.frozenGitDirectory 'gitdir';$old=[IO.File]::ReadAllText($p)
  try{[IO.File]::WriteAllText($p,(Join-Path $repo '.git'),[Text.UTF8Encoding]::new($false));Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'frozen Git reverse pointer'}
  finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::WriteAllText($p,$old,[Text.UTF8Encoding]::new($false))}
 }
 Check 'grafts and shallow interpretation are refused' {
  foreach($leaf in @('info/grafts','shallow')){
   $p=Join-Path $repo ('.git/'+$leaf);[IO.File]::WriteAllText($p,('a'*40),[Text.UTF8Encoding]::new($false))
   try{Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'unsupported external'}
   finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::Delete($p)}
  }
 }
 Check 'per-worktree config is refused before interpreter use' {
  $p=Join-Path $metadata.frozenGitDirectory 'config.worktree';[IO.File]::WriteAllText($p,'[core]',[Text.UTF8Encoding]::new($false))
  try{Refuse {New-KmcScopedGitLease $proof $platform $held|Out-Null} 'Frozen worktree configuration'}
  finally{foreach($lease in $held){$lease.Dispose()};$held.Clear();[IO.File]::Delete($p)}
 }
 Check 'replacement objects are disabled in the owned environment' {Need ($env:GIT_NO_REPLACE_OBJECTS-ceq'1') 'Git replacement objects still active.'}
}finally{
 foreach($lease in $held){$lease.Dispose()}
 foreach($s in $owners){Exit-KmcScopedEnvironment $s}
}
Check 'inherited settings restored exactly in finally' {Need ($env:PATH-ceq$originalPath-and$env:TEMP-ceq$originalTemp-and$env:GIT_OPTIONAL_LOCKS-ceq'1'-and$env:GIT_SCOPED_TEST-ceq'private-test-value'-and$env:GIT_CONFIG_PARAMETERS-ceq"'core.fsmonitor=synthetic-ignored-helper'") 'Original environment not restored.'}
Check 'restore is idempotent' {foreach($s in $owners){Exit-KmcScopedEnvironment $s};Need $owners[0].restored 'Restoration not marked.'}
Check 'partial admission owns restoration before failure' {
 $partial=[Collections.Generic.List[object]]::new()
 try{Refuse {Enter-KmcScopedEnvironment $platform $repo $frozen (Join-Path $OutputRoot 'missing') $partial|Out-Null} 'scratch environment';Need ($partial.Count-eq1) 'Partial owner lost.'}
 finally{foreach($s in $partial){Exit-KmcScopedEnvironment $s}}
}
[Environment]::SetEnvironmentVariable('GIT_OPTIONAL_LOCKS',$originalGitOptional,'Process')
[Environment]::SetEnvironmentVariable('GIT_SCOPED_TEST',$null,'Process');[Environment]::SetEnvironmentVariable('GIT_CONFIG_PARAMETERS',$null,'Process')
$env:TEMP=$testOriginalTemp;$env:TMP=$testOriginalTmp
$receipt=[ordered]@{kind='actual-private-filesystem-Git-environment-checks';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=([DateTime]::UtcNow-$started).TotalSeconds;sourceSha256=(Hash (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1'));rows=@($rows);nativeExecution=$false;archiveOperations=0;externalSubstitutions=@();privateGitOnly=$true}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ("Platform environment: {0} PASS / {1} FAIL"-f$script:pass,$script:fail)
if($script:fail-ne0){throw 'Private platform environment checks failed.'}
