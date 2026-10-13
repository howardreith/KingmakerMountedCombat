param([string]$HomeRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve earlier bootstrap ownership evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);[void][IO.Directory]::CreateDirectory((Join-Path $OutputRoot 'temp'))
$oldTemp=$env:TEMP;$oldTmp=$env:TMP;$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP
. (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1')
$script:pass=0;$script:fail=0;$rows=[Collections.Generic.List[object]]::new()
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;$rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Refuse([scriptblock]$Body,[string]$Pattern){$caught=$false;try{&$Body}catch{if($_.Exception.Message-cnotmatch$Pattern){throw};$caught=$true};Need $caught 'Expected refusal did not occur.'}
function ReadHeldReceipt($Path){$stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite);$reader=[IO.StreamReader]::new($stream);try{$reader.ReadToEnd()}finally{$reader.Dispose()}}
function Hash($Path){(Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant()}
$runtime=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory().TrimEnd('\')
$refs=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Join-Path $runtime $_})
$compiler=Join-Path $runtime 'csc.exe'
$plan=[pscustomobject]@{compiler=[pscustomobject]@{path=$compiler;configurationPath=($compiler+'.config');references=$refs;timeoutSeconds=30};files=@([pscustomobject]@{path=$compiler;role='csharp-compiler'},[pscustomobject]@{path=($compiler+'.config');role='platform-config'})+@($refs|ForEach-Object{[pscustomobject]@{path=$_;role='framework'}})}
$compilers=[Collections.Generic.List[object]]::new();$outputs=[Collections.Generic.List[object]]::new()
$started=[DateTime]::UtcNow
try{
 Check 'exact framework compiler and three explicit references admitted' {Assert-KmcScopedCompilerPlan $plan}
 Check 'unreviewed compiler path refused before any process' {$old=$plan.compiler.path;$plan.compiler.path=Join-Path $OutputRoot 'csc.exe';try{Refuse {Assert-KmcScopedCompilerPlan $plan} 'framework compiler'}finally{$plan.compiler.path=$old}}
 Check 'foreign reference refused before any process' {$old=$plan.compiler.references[1];$plan.compiler.references[1]=Join-Path $OutputRoot 'System.dll';try{Refuse {Assert-KmcScopedCompilerPlan $plan} 'compiler reference'}finally{$plan.compiler.references[1]=$old}}
 $compile=Join-Path $OutputRoot 'compiler';[void][IO.Directory]::CreateDirectory($compile)
 $owner=New-KmcScopedCompilerOwner $compilers
 Check 'owned explicit compiler builds actual observation source' {
  $source=Join-Path $HomeRoot 'src/ScopedPreflightObservation.cs'
  $assembly=Invoke-KmcScopedCompile $plan ([IO.File]::ReadAllBytes($source)) (Hash $source) $compile 'PrivateObservation.dll' $owner
  Need ($null-ne$assembly.GetType('KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation',$false)-and$owner.completed-and$owner.exitCode-eq0) 'Actual compile failed.'
 }
 Check 'native compile uses no response/config default or standard reference search' {Need ($owner.arguments[1]-ceq'/noconfig'-and$owner.arguments[2]-ceq'/nostdlib+'-and@($owner.arguments|Where-Object{$_-like'/reference:*'}).Count-eq3-and$owner.arguments.Count-eq12) 'Compiler closure changed.'}
 Check 'compiled source and assembly are retained against external writes' {foreach($p in @($owner.source,$owner.output)){Refuse {[IO.File]::AppendAllText($p,'x')} '(?i)used by another process|access|sharing'}}
 Check 'source hash mismatch owns but does not dispatch compiler' {
  $where=Join-Path $OutputRoot 'bad-source';[void][IO.Directory]::CreateDirectory($where);$o=New-KmcScopedCompilerOwner $compilers
  Refuse {Invoke-KmcScopedCompile $plan ([Text.Encoding]::UTF8.GetBytes('broken')) ('a'*64) $where 'Bad.dll' $o|Out-Null} 'exact held bytes'
  Need (-not$o.processStarted-and$null-eq$o.process) 'Hash failure dispatched compiler.'
 }
 Check 'compiler errors retain completed process/input proof and fail closed' {
  $where=Join-Path $OutputRoot 'compiler-failure';[void][IO.Directory]::CreateDirectory($where);$o=New-KmcScopedCompilerOwner $compilers
  $bytes=[Text.Encoding]::UTF8.GetBytes('not CSharp');$sha=[Security.Cryptography.SHA256]::Create()
  try{$pin=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
  Refuse {Invoke-KmcScopedCompile $plan $bytes $pin $where 'Bad.dll' $o|Out-Null} 'compiler failed'
  Need ($o.processStarted-and$o.completed-and$o.exitCode-ne0-and$null-ne$o.sourceHandle) 'Failed compiler custody lost.'
 }
 Check 'existing output directory is refused without changing its files' {
  $where=Join-Path $OutputRoot 'existing';[void][IO.Directory]::CreateDirectory($where);[IO.File]::WriteAllText((Join-Path $where 'foreign'),'unchanged')
  Refuse {New-KmcScopedOutputOwner $where $outputs|Out-Null} 'already exists'
  Need ([IO.File]::ReadAllText((Join-Path $where 'foreign'))-ceq'unchanged') 'Foreign file changed.'
 }
 Check 'preplanted receipt is never overwritten on output sealing failure' {
  $where=Join-Path $OutputRoot 'preplanted';$o=New-KmcScopedOutputOwner $where $outputs
  [IO.File]::WriteAllText((Join-Path $where 'scoped-preflight.json'),'foreign')
  Refuse {Seal-KmcScopedOutputOwner $o} '(?i)already exists|cannot create|file exists'
  Need ([IO.File]::ReadAllText((Join-Path $where 'scoped-preflight.json'))-ceq'foreign') 'Foreign receipt overwritten.'
  Close-KmcScopedOutput $o
 }
 $where=Join-Path $OutputRoot 'owned-publication';$o=New-KmcScopedOutputOwner $where $outputs;Seal-KmcScopedOutputOwner $o
 Check 'exact receipt/token/path remain owned through all input releases' {
  foreach($c in $compilers){Close-KmcScopedCompiler $c}
  Refuse {[IO.File]::WriteAllText((Join-Path $where 'scoped-preflight.json'),'foreign')} '(?i)used by another process|access|sharing'
  Refuse {[IO.File]::AppendAllText((Join-Path $where 'owner.token'),'x')} '(?i)used by another process|access|sharing'
  Refuse {[IO.Directory]::Move($where,$where+'-foreign')} '(?i)used by another process|access|sharing'
  [void]$o.lease.VerifyUnchanged()
 }
 Check 'actual publication uses retained create-new stream and explicit exit condition' {
  $r=[ordered]@{status='FAIL';reason='synthetic guard failure';nativeExecution=$false}
  Publish-KmcScopedOutput $o $r
  $parsed=ReadHeldReceipt (Join-Path $where 'scoped-preflight.json')|ConvertFrom-Json
  Need ($parsed.status-ceq'FAIL'-and$parsed.completionRequiresSuccessfulCallerExit-eq$true-and$parsed.outputOwnerSha256-ceq$o.tokenSha256) 'Receipt does not bind its owner/exit.'
 }
 Check 'second publication and foreign late paths are refused' {
  Refuse {Publish-KmcScopedOutput $o ([ordered]@{status='PASS'})} 'first publication'
  Need (((ReadHeldReceipt (Join-Path $where 'scoped-preflight.json'))|ConvertFrom-Json).status-ceq'FAIL') 'Failure was promoted.'
 }
 Check 'publication and compiler release are retry-idempotent' {Close-KmcScopedOutput $o;Close-KmcScopedOutput $o;foreach($c in $compilers){Close-KmcScopedCompiler $c;Need $c.released 'Compiler not released.'};Need $o.released 'Output not released.'}
 Check 'a released output cannot be used to write a replacement tree' {
  [IO.Directory]::Move($where,$where+'-retained');[void][IO.Directory]::CreateDirectory($where);[IO.File]::WriteAllText((Join-Path $where 'scoped-preflight.json'),'foreign')
  Refuse {Publish-KmcScopedOutput $o ([ordered]@{status='PASS'})} 'first publication'
  Need ([IO.File]::ReadAllText((Join-Path $where 'scoped-preflight.json'))-ceq'foreign') 'Replacement tree mutated.'
 }
}catch{$script:fail++;$rows.Add([pscustomobject]@{name='fixture terminal failure';status='FAIL';error=$_.Exception.ToString()})}
finally{
 foreach($c in $compilers){Close-KmcScopedCompiler $c}
 foreach($o in $outputs){Close-KmcScopedOutput $o}
 $env:TEMP=$oldTemp;$env:TMP=$oldTmp
}
$receipt=[ordered]@{kind='actual-private-bootstrap-compiler-output-checks';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=([DateTime]::UtcNow-$started).TotalSeconds;sourceSha256=(Hash (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1'));compilerPath=$compiler;compilerSha256=(Hash $compiler);rows=@($rows);nativeExecution=$false;archiveOperations=0;externalSubstitutions=@()}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ("Bootstrap ownership: {0} PASS / {1} FAIL"-f$script:pass,$script:fail)
if($script:fail-ne0){throw 'Private bootstrap ownership checks failed.'}
