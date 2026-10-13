param([string]$HomeRoot,[string]$SourcePath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve copied-source admission evidence.'};[void][IO.Directory]::CreateDirectory($OutputRoot)
. (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. $SourcePath
$text=[IO.File]::ReadAllText($SourcePath);$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseInput($text,$SourcePath,[ref]$tokens,[ref]$errors)
$method=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Invoke-KmcScopedCompile'},$false))[0].Extent.Text
$old=' $Owner.sourceHandle=[IO.FileStream]::new($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)'
$other=' if(Test-Path -LiteralPath $output){throw ''Compiler target already exists.''}'
$anchor=if($method.Contains($old)){$old}else{$other}
if(-not$method.Contains($anchor)){throw 'Actual source-copy boundary is absent.'}
$instrumented=$method.Replace($anchor,(' Invoke-PrivateSourceCopyMutation $source'+[Environment]::NewLine+$anchor))
. ([scriptblock]::Create($instrumented))
function Invoke-PrivateSourceCopyMutation([string]$Path){
 if($script:mode-ceq'none'){return}
 try{
  if($script:mode-ceq'write'){[IO.File]::WriteAllText($Path,$script:replacement,[Text.UTF8Encoding]::new($false))}
  elseif($script:mode-ceq'writer'){$script:intruder=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)}
  elseif($script:mode-ceq'replace'){[IO.File]::Move($Path,$Path+'.original');[IO.File]::WriteAllText($Path,$script:replacement,[Text.UTF8Encoding]::new($false))}
  else{throw 'Unknown deterministic filesystem mutation'}
  $script:mutationSucceeded=$true
 }catch{$script:mutationRefused=$true}
}
$compiler=Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe';$refs=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) $_})
$plan=[pscustomobject]@{compiler=[pscustomobject]@{path=$compiler;configurationPath=($compiler+'.config');references=$refs;timeoutSeconds=30};files=@([pscustomobject]@{path=$compiler;role='csharp-compiler'},[pscustomobject]@{path=($compiler+'.config');role='platform-config'})+@($refs|ForEach-Object{[pscustomobject]@{path=$_;role='framework'}})}
$script:replacement='namespace PrivateSourceWitness { public static class Value { public static string Get() { return "modified"; } } }'
$original=[Text.Encoding]::UTF8.GetBytes('namespace PrivateSourceWitness { public static class Value { public static string Get() { return "original"; } } }')
$alg=[Security.Cryptography.SHA256]::Create();try{$sha=[BitConverter]::ToString($alg.ComputeHash($original)).Replace('-','').ToLowerInvariant()}finally{$alg.Dispose()}
$rows=@();$pass=0;$fail=0
foreach($kind in @('none','write','replace','writer')){
 $owners=[Collections.Generic.List[object]]::new();$owner=New-KmcScopedCompilerOwner $owners;$script:mode=$kind;$script:mutationSucceeded=$false;$script:mutationRefused=$false;$where=Join-Path $OutputRoot $kind;[void][IO.Directory]::CreateDirectory($where)
 $value=$null;$errorValue=$null;$script:intruder=$null
 try{$asm=Invoke-KmcScopedCompile $plan $original $sha $where ($kind+'Witness.dll') $owner;$value=$asm.GetType('PrivateSourceWitness.Value').GetMethod('Get').Invoke($null,@())}catch{$errorValue=$_.Exception.ToString()}
 finally{if($null-ne$script:intruder){$script:intruder.Dispose()};foreach($o in $owners){Close-KmcScopedCompiler $o}}
 $blockedMutation=$script:mutationRefused-and-not$script:mutationSucceeded-and$value-ceq'original'-and$null-eq$errorValue
 $refusedBeforeCompiler=$script:mutationSucceeded-and$null-eq$value-and-not$owner.processStarted-and$errorValue-cmatch'Owned compiler source copy differs before consumption'
 $refusedOutstandingWriter=$kind-ceq'writer'-and$script:mutationSucceeded-and$null-eq$value-and-not$owner.processStarted-and$errorValue-match'used by another process|sharing'
 $ok=if($kind-ceq'none'){$value-ceq'original'-and$null-eq$errorValue}else{$blockedMutation-or$refusedBeforeCompiler-or$refusedOutstandingWriter}
 if($ok){$pass++}else{$fail++};$rows+=[pscustomobject]@{name=$kind;status=$(if($ok){'PASS'}else{'FAIL'});mutationSucceeded=$script:mutationSucceeded;mutationRefused=$script:mutationRefused;observedCompiledValue=$value;compilerStarted=$owner.processStarted;error=$errorValue}
}
$receipt=[ordered]@{schemaVersion=1;status=$(if($fail){'FAIL'}else{'PASS'});pass=$pass;fail=$fail;sourcePath=$SourcePath;sourceSha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();instrumentation='One test-only scheduling call at the actual source-copy write-to-consumption boundary; original method body otherwise unchanged. Real filesystem mutations and actual exact Framework csc. This deterministically tests capability, not frequency of an unscheduled OS race.';rows=$rows;nativeExecution=$false;archiveOperations=0}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 9),[Text.UTF8Encoding]::new($false));Write-Output ('Copied source '+$pass+' PASS / '+$fail+' FAIL');if($fail){throw 'Copied source mutation regression failed'}
