[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing proof test output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$homeRoot=Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $homeRoot 'src/ScopedPreflightObservation.cs')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
$script:pass=0;$script:fail=0;$script:rows=[Collections.Generic.List[object]]::new();$script:index=0
function Check([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;$script:rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$script:rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant()}
function Put([string]$Path,$Value){[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))}
function Pin([string]$Path){[pscustomobject]@{path=$Path;sha256=(Hash $Path);bytes=[long](Get-Item -LiteralPath $Path).Length}}
function Fixture([scriptblock]$Mutate){
 $script:index++;$lab=Join-Path $OutputRoot ('case'+$script:index);$audit=Join-Path $lab 'analysis-cache/audit';[void][IO.Directory]::CreateDirectory($audit)
 $repo=Join-Path $lab 'repo/KingmakerMountedCombat';$sha='a'*64;$commit='b'*40;$source='c'*40
 $start='2026-10-12T10:00:00.0000000+00:00';$end='2026-10-12T10:01:00.0000000+00:00';$done='2026-10-12T10:01:01.0000000+00:00'
 $supervisor=Join-Path $audit 'Supervisor.ps1';$marker='<this-file-actual-sha256>'
 $template=@('-NoProfile','-NonInteractive','-File',$supervisor,'-ExecutionSha256',$marker)
 $executor=[pscustomobject]@{path='C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe';sha256=$sha;powerShellVersion='5.1';clrVersion='4.0';rawArguments=@('C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe')+$template;workingDirectory=$repo;powerShellLocation=$repo;temp='C:\Temp';tmp='C:\Temp'}
 $roots=@((Join-Path $lab 'runtime-state'),(Join-Path $lab 'runtime-backups'),(Join-Path $lab 'runtime-staging'),(Join-Path $lab 'runtime-evidence'),'C:\InstalledGame\Mods')
 $ex=[pscustomobject]@{schemaVersion=1;kind='preview5-fresh-full-whatif-execution';source=$source;sourceTree=$commit;harness=$commit;harnessTree=$source;repositoryRoot=$repo;auditRoots=$roots;saveRoot='C:\UserSaves';frozenSourceRoot=(Join-Path $lab 'worktrees/frozen');packageValidationScratchRoot=(Join-Path $repo 'obj/package-validation');arguments=[pscustomobject]@{sha256=$sha};executor=[pscustomobject]@{path=$executor.path;sha256=$sha;arguments=$template.Clone();workingDirectory=$repo;actualProcessArgumentsVersionsAndTempCapturedBySupervisor=$true};supervisor=[pscustomobject]@{path=$supervisor;sha256=$sha;bytes=12}}
 $tracked=@([pscustomobject]@{relativePath='a.ps1';path=(Join-Path $repo 'a.ps1');sha256=$sha;bytes=12})
 $additional=@([pscustomobject]@{path=(Join-Path $lab 'environment-intake.json');sha256=$sha;bytes=10})
 $before=[pscustomobject]@{schemaVersion=1;kind='actual-audit-input-byte-inventory';phase='before';repositoryRoot=$repo;head=$commit;tracked=$tracked;additional=$additional}
 $after=$before|ConvertTo-Json -Depth 12|ConvertFrom-Json;$after.phase='after'
 $pr=[pscustomobject]@{schemaVersion=1;processId=1234;startedAtUtc=$start;executionSha256='';argumentsSha256=$sha;executor=$executor;runId='bootstrap'}
 $ob=[pscustomobject]@{schemaVersion=1;kind='full-whatif-supervisor-observation';phase='full-whatif';status='PASS';startedUtc=$start;endedUtc=$end;seconds=60.0;error=$null;source=$source;harness=$commit;executionSha256='';argumentsSha256=$sha;purityLinePresent=$true;purityLineCount=1;assertionKind='launcher-code-mediated-per-root-before-after-equality';perRootDigestsCaptured=$false;beforeInputs=$null;afterInputs=$null;actualProcessExitRequiresCallerReceipt=$true;beforePinsVerified=$true;afterPinsVerified=$true;executor=$executor;auditRoots=$roots;log='';logSha256='';nativeExecution=$false}
 $exitRecord=[pscustomobject]@{schemaVersion=1;kind='full-whatif-caller-exit';executionSha256='';processId=1234;startedAtUtc=$start;exitCode=0;completed=$true;supervisorReceiptSha256='';observedCompletionUtc=$done;toolSessionId=12345;toolChunkId='abc';attestation='caller-observed-tool-exit'}
 $f=[pscustomobject]@{lab=$lab;root=$audit;execution=$ex;process=$pr;observation=$ob;callerExit=$exitRecord;beforeInputs=$before;afterInputs=$after;log="Runtime WhatIf purity PASS; exact fixture descriptors were validated when required, and no evidence, lock, transaction, Mods, process, game, or save mutation occurred.\n";bindingMutation=$null;postBindMutation=$null;logEncoding='utf8'}
 if($Mutate){&$Mutate $f}
 $pins=@{}
 foreach($name in @('execution','beforeInputs','afterInputs')){$path=Join-Path $audit ($name+'.json');Put $path $f.$name;$pins[$name]=Pin $path}
 # Resolve only the declared placeholder in the actual observed fixture after the
 # execution bytes are pinned. Deliberately wrong literal hashes remain wrong.
 for($i=0;$i-lt$f.process.executor.rawArguments.Count;$i++){if($f.process.executor.rawArguments[$i]-is[string]-and$f.process.executor.rawArguments[$i]-ceq$marker){$f.process.executor.rawArguments[$i]=$pins.execution.sha256}}
 $f.process.executionSha256=$pins.execution.sha256;$f.observation.executionSha256=$pins.execution.sha256;$f.callerExit.executionSha256=$pins.execution.sha256
 foreach($name in @('beforeInputs','afterInputs')){$f.observation.$name=[pscustomobject]@{path=$pins[$name].path;sha256=$pins[$name].sha256;trackedCount=$f.$name.tracked.Count;additionalCount=$f.$name.additional.Count}}
 $log=Join-Path $audit 'full.log';$logText=$f.log.Replace('\n',[Environment]::NewLine);switch($f.logEncoding){
  'utf8'{[IO.File]::WriteAllText($log,$logText,[Text.UTF8Encoding]::new($false))}
  'utf8-bom'{[IO.File]::WriteAllText($log,$logText,[Text.UTF8Encoding]::new($true))}
  'utf16le'{[IO.File]::WriteAllText($log,$logText,[Text.UnicodeEncoding]::new($false,$true,$true))}
  'utf16be'{[IO.File]::WriteAllText($log,$logText,[Text.UnicodeEncoding]::new($true,$true,$true))}
  'utf16le-odd'{[IO.File]::WriteAllBytes($log,[byte[]]@(255,254,80))}
  'utf16le-surrogate'{[IO.File]::WriteAllBytes($log,[byte[]]@(255,254,0,216))}
  'utf8-malformed'{[IO.File]::WriteAllBytes($log,[byte[]]@(239,187,191,255))}
  default{throw 'Unknown test log encoding'}
 };$pins.log=Pin $log;$f.observation.log=$log;$f.observation.logSha256=$pins.log.sha256
 foreach($name in @('process','observation')){$path=Join-Path $audit ($name+'.json');Put $path $f.$name;$pins[$name]=Pin $path}
 $f.callerExit.supervisorReceiptSha256=$pins.observation.sha256;$path=Join-Path $audit 'callerExit.json';Put $path $f.callerExit;$pins.callerExit=Pin $path
 $binding=[pscustomobject]@{schemaVersion=1;kind='closed-preflight-inherited-full-proof';status='PASS';nativeExecution=$false;assertionKind=$f.observation.assertionKind;perRootDigestsCaptured=$false;execution=$pins.execution;observation=$pins.observation;process=$pins.process;callerExit=$pins.callerExit;beforeInputs=$pins.beforeInputs;afterInputs=$pins.afterInputs;log=$pins.log}
 if($f.bindingMutation){&$f.bindingMutation $binding}
 $path=Join-Path $audit 'proof.json';Put $path $binding;$pin=Pin $path
 if($f.postBindMutation){&$f.postBindMutation $f $pin}
 [pscustomobject]@{lab=$lab;path=$path;sha256=$pin.sha256;binding=$binding}
}
function ReadProof($Fixture,[scriptblock]$After){
 $owned=[Collections.Generic.List[IDisposable]]::new()
 try{$actual=Get-KmcScopedInheritedProof $Fixture.path $Fixture.sha256 $Fixture.lab $owned;if($After){&$After $actual $owned};foreach($lease in $owned){[void]$lease.VerifyUnchanged()}}
 finally{foreach($lease in $owned){$lease.Dispose()}}
}
function Reject([string]$Name,[scriptblock]$Mutation,[string]$Expected){Check $Name {$f=Fixture $Mutation;$rejected=$false;try{ReadProof $f}catch{if($_.Exception.Message-cnotmatch$Expected){throw};$rejected=$true};Need $rejected 'Invalid inherited proof was accepted.'}}
$started=[DateTime]::UtcNow
Check 'actual caller exit plus pinned complete full proof is accepted' {$f=Fixture;ReadProof $f {param($p,$owned)Need ($owned.Count-eq8) 'All proof handles must remain held.';Need (-not$p.rootDigestsAvailable-and-not$p.callerObservationIsOsAttestation) 'Receipt overclaims proof.'}}
Reject 'supervisor FAIL cannot be inherited' {param($f)$f.observation.status='FAIL'} 'observation status'
Reject 'numeric-text exit code is refused' {param($f)$f.callerExit.exitCode='0'} 'exact bounded JSON integer'
Reject 'nonzero tool exit is refused' {param($f)$f.callerExit.exitCode=1} 'actual caller exit'
Reject 'caller incomplete is refused' {param($f)$f.callerExit.completed=$false} 'actual caller completion'
Reject 'caller boolean text is refused' {param($f)$f.callerExit.completed='true'} 'actual caller completion'
Reject 'caller process substitution is refused' {param($f)$f.callerExit.processId=4321} 'caller process'
Reject 'caller start substitution is refused' {param($f)$f.callerExit.startedAtUtc='2026-10-12T09:59:00.0000000+00:00'} 'caller process start'
Reject 'caller completion before supervisor is refused' {param($f)$f.callerExit.observedCompletionUtc='2026-10-12T09:59:00.0000000+00:00'} 'observation ordering'
Reject 'missing actual caller receipt cannot be log-inferred' {param($f)$f.bindingMutation={param($b)$b.PSObject.Properties.Remove('callerExit')}} 'property set'
Reject 'claimed native audit is refused' {param($f)$f.observation.nativeExecution=$true} 'observation nativeExecution'
Reject 'invented root digests are refused' {param($f)$f.observation.perRootDigestsCaptured=$true} 'observation perRootDigestsCaptured'
Reject 'missing pin validation cannot be inherited' {param($f)$f.observation.afterPinsVerified=$false} 'observation afterPinsVerified'
Reject 'numeric-text purity count is refused' {param($f)$f.observation.purityLineCount='1'} 'full purity line count'
Reject 'observed error is refused' {param($f)$f.observation.error='failed'} 'contains an error'
Reject 'different assertion claim is refused' {param($f)$f.observation.assertionKind='global-no-write'} 'full proof assertion'
Reject 'changed audit file bytes are refused' {param($f)$f.afterInputs.tracked[0].sha256='d'*64} 'input inventories differ'
Reject 'changed audit dependency set is refused' {param($f)$f.afterInputs.tracked+=([pscustomobject]@{relativePath='new.ps1';path=(Join-Path $f.execution.repositoryRoot 'new.ps1');sha256='d'*64;bytes=3})} 'input inventories differ'
Reject 'input path traversal is refused' {param($f)$f.beforeInputs.tracked[0].relativePath='../escape.ps1'} 'invalid relative path'
Reject 'input numeric-text size is refused' {param($f)$f.beforeInputs.tracked[0].bytes='12'} 'audit input bytes'
Reject 'duplicate dependency labels are refused' {param($f)$f.beforeInputs.tracked+=($f.beforeInputs.tracked[0])} 'ambiguous'
Reject 'extra historical root is refused' {param($f)$f.execution.auditRoots+=('C:\Other')} 'exactly five'
Reject 'different historical root identity is refused' {param($f)$f.execution.auditRoots[0]='C:\Other'} 'historical root identity'
Reject 'profile execution is refused' {param($f)$f.process.executor.rawArguments=@('powershell.exe','-File','Supervisor.ps1')} 'NoProfile'
Reject 'raw log without actual single sentence is refused' {param($f)$f.log='PASS'} 'exact single'
Reject 'two full assertion lines are refused' {param($f)$f.log=$f.log+$f.log} 'exact single'
Reject 'cross-record path alias is refused' {param($f)$f.bindingMutation={param($b)$b.afterInputs=$b.beforeInputs}} 'paths alias'
Reject 'unknown proof property is refused' {param($f)$f.bindingMutation={param($b)$b|Add-Member NoteProperty skipSource $true}} 'property set'
Reject 'post-pinning receipt mutation is refused' {param($f)$f.postBindMutation={param($x,$pin)[IO.File]::AppendAllText((Join-Path $x.root 'observation.json'),' ')}} 'immutable pin'
Reject 'same-length restored-time receipt substitution is refused' {param($f)$f.postBindMutation={param($x,$pin)$p=Join-Path $x.root 'observation.json';$t=(Get-Item $p).LastWriteTimeUtc;$s=[IO.File]::ReadAllText($p).Replace('PASS','FAIL');[IO.File]::WriteAllText($p,$s,[Text.UTF8Encoding]::new($false));(Get-Item $p).LastWriteTimeUtc=$t}} 'immutable pin'
Check 'held proof remains read-only until caller finally' {$f=Fixture;ReadProof $f {param($p,$owned)$blocked=$false;try{[IO.File]::AppendAllText($p.binding.log.path,'changed')}catch{$blocked=$true};Need $blocked 'Proof file was writable.'};[IO.File]::AppendAllText($f.binding.log.path,'after-release')}
Check 'duplicate case-ambiguous JSON is refused before semantic parsing' {$rejected=$false;try{ConvertFrom-KmcScopedJson '{"schemaVersion":1,"SchemaVersion":1}' 'duplicate'}catch{Need ($_.Exception.Message-cmatch'duplicate or case-ambiguous') 'Wrong refusal';$rejected=$true};Need $rejected 'Ambiguous JSON accepted.'}
Check 'failed proof owns and releases partially acquired files' {$f=Fixture {$args[0].observation.status='FAIL'};$owned=[Collections.Generic.List[IDisposable]]::new();$failed=$false;try{Get-KmcScopedInheritedProof $f.path $f.sha256 $f.lab $owned}catch{$failed=$true}finally{foreach($lease in $owned){$lease.Dispose()}};Need ($failed-and$owned.Count-eq8) 'Failed proof custody was lost.';[IO.File]::AppendAllText($f.binding.log.path,'after-failed-release')}
Check 'actual WindowsPS redirected UTF16LE BOM log is accepted' {$f=Fixture {param($f)$f.logEncoding='utf16le'};ReadProof $f}
Check 'explicit UTF8 BOM log is accepted' {$f=Fixture {param($f)$f.logEncoding='utf8-bom'};ReadProof $f}
Reject 'odd-length UTF16LE raw log is refused' {param($f)$f.logEncoding='utf16le-odd'} 'decode|Unicode|UTF|translate'
Reject 'malformed UTF16LE surrogate is refused' {param($f)$f.logEncoding='utf16le-surrogate'} 'decode|Unicode|UTF|translate'
Reject 'UTF16BE outside selected log contract is refused' {param($f)$f.logEncoding='utf16be'} 'decode|Unicode|UTF|translate'
Reject 'malformed UTF8 raw log is refused' {param($f)$f.logEncoding='utf8-malformed'} 'decode|Unicode|UTF|translate'

Reject 'different raw executable is refused despite matching executor records' {param($f)$f.process.executor.rawArguments[0]='C:\Other\powershell.exe'} 'argument|executor'
Reject 'wrong actual supervisor script is refused' {param($f)$f.process.executor.rawArguments[4]='C:\Other\Supervisor.ps1'} 'argument|supervisor'
Reject 'wrong actual execution hash is refused' {param($f)$f.process.executor.rawArguments[6]='d'*64} 'argument|execution'
Reject 'extra actual command arguments are refused' {param($f)$f.process.executor.rawArguments+=@('-Command','Write-Output bypass')} 'argument'
Reject 'actual argument numeric token is refused' {param($f)$f.process.executor.rawArguments[4]=12} 'argument'
Reject 'actual argument boolean token is refused' {param($f)$f.process.executor.rawArguments[4]=$true} 'argument'
Reject 'actual argument null token is refused' {param($f)$f.process.executor.rawArguments[4]=$null} 'argument'
Reject 'actual argument one-element array is refused' {param($f)$f.process.executor.rawArguments[4]=@($f.process.executor.rawArguments[4])} 'argument'
Reject 'wrong actual working directory is refused' {param($f)$f.process.executor.workingDirectory='C:\Other'} 'working directory'
Reject 'wrong actual PowerShell location is refused' {param($f)$f.process.executor.powerShellLocation='C:\Other'} 'PowerShell location'
Reject 'actual working directory array is refused' {param($f)$f.process.executor.workingDirectory=@($f.execution.repositoryRoot)} 'working directory'
Reject 'actual PowerShell location array is refused' {param($f)$f.process.executor.powerShellLocation=@($f.execution.repositoryRoot)} 'PowerShell location'
Reject 'wrong planned working directory is refused even when actual matches' {param($f)$f.execution.executor.workingDirectory='C:\Other';$f.process.executor.workingDirectory='C:\Other';$f.process.executor.powerShellLocation='C:\Other'} 'working directory'
Reject 'template argument numeric token is refused' {param($f)$f.execution.executor.arguments[3]=12} 'argument'
Reject 'template unknown hash marker is refused' {param($f)$f.execution.executor.arguments[5]='<other-marker>'} 'template|argument'
Reject 'template duplicate hash markers are refused' {param($f)$f.execution.executor.arguments[3]='<this-file-actual-sha256>'} 'template|argument'
Reject 'template extra command arguments are refused' {param($f)$f.execution.executor.arguments+=@('-Command','Write-Output bypass')} 'template|argument'
Reject 'template script substitution is refused even when actual matches' {param($f)$f.execution.executor.arguments[3]='C:\Other\Supervisor.ps1';$f.process.executor.rawArguments[4]='C:\Other\Supervisor.ps1'} 'template|argument|supervisor'
Reject 'planned cwd one-element array is refused' {param($f)$f.execution.executor.workingDirectory=@($f.execution.repositoryRoot)} 'working directory'
Reject 'template shell mode changed in both records is refused' {param($f)$f.execution.executor.arguments[1]='-NoExit';$f.process.executor.rawArguments[2]='-NoExit'} 'template|argument'
Reject 'missing captured-executor promise is refused' {param($f)$f.execution.executor.actualProcessArgumentsVersionsAndTempCapturedBySupervisor=$false} 'executor observation'

$ended=[DateTime]::UtcNow
[ordered]@{kind='private-generated-json-inherited-proof-tests';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=($ended-$started).TotalSeconds;rows=@($script:rows);actualAuditOrArchiveRead=$false;externalBoundarySubstitutions=@();fixture='generated JSON receipts only, never audit evidence';sourceSha256=(Hash (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1'))}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ("Proof reader: {0} PASS / {1} FAIL"-f$script:pass,$script:fail)
if($script:fail-ne0){throw 'Private inherited proof checks failed.'}
