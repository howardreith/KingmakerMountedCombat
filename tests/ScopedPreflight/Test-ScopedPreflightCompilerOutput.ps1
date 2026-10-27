param([string]$HomeRoot,[string]$SourcePath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve compiler output admission evidence.'};[void][IO.Directory]::CreateDirectory($OutputRoot)
. (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1');. $SourcePath
$text=[IO.File]::ReadAllText($SourcePath);$t=$null;$e=$null;$ast=[Management.Automation.Language.Parser]::ParseInput($text,$SourcePath,[ref]$t,[ref]$e)
$method=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Invoke-KmcScopedCompile'},$false))[0].Extent.Text
$hook=' if(-not[IO.File]::Exists($output)){throw ''Exact compiler produced no assembly.''}'
$load=' $Owner.assembly=[Reflection.Assembly]::LoadFrom($output)'
if(-not$method.Contains($hook)-or-not$method.Contains($load)){throw 'Exact actual output boundary absent'}
$method=$method.Replace($hook,(' Invoke-PrivateOutputMutation $output'+[Environment]::NewLine+$hook)).Replace($load,(' $script:loadAttempted=$true'+[Environment]::NewLine+$load))
. ([scriptblock]::Create($method))
function Invoke-PrivateOutputMutation([string]$Path){
 switch($script:mode){
 'none'{}
 'hardlink'{[IO.File]::Delete($Path);[void](New-Item -ItemType HardLink -Path $Path -Value $script:originalOutput -ErrorAction Stop)}
 'empty'{[IO.File]::WriteAllBytes($Path,[byte[]]@())}
 'extra'{[IO.File]::WriteAllText((Join-Path (Split-Path $Path -Parent) 'undeclared.tmp'),'extra')}
 default{throw 'Unknown output mutation'}
 }
}
$runtime=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory();$compiler=Join-Path $runtime 'csc.exe';$refs=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Join-Path $runtime $_})
$plan=[pscustomobject]@{compiler=[pscustomobject]@{path=$compiler;configurationPath=($compiler+'.config');references=$refs;timeoutSeconds=30};files=@([pscustomobject]@{path=$compiler;role='csharp-compiler'},[pscustomobject]@{path=($compiler+'.config');role='platform-config'})+@($refs|ForEach-Object{[pscustomobject]@{path=$_;role='framework'}})}
$bytes=[Text.Encoding]::UTF8.GetBytes('namespace PrivateOutputWitness { public class Value { } }');$alg=[Security.Cryptography.SHA256]::Create();try{$sha=[BitConverter]::ToString($alg.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$alg.Dispose()}
$rows=@();$pass=0;$fail=0;$script:originalOutput=$null
foreach($mode in @('none','hardlink','empty','extra')){
 $owners=[Collections.Generic.List[object]]::new();$owner=New-KmcScopedCompilerOwner $owners;$script:mode=$mode;$script:loadAttempted=$false;$where=Join-Path $OutputRoot $mode;[void][IO.Directory]::CreateDirectory($where);$errorValue=$null;$assembly=$null
 try{$assembly=Invoke-KmcScopedCompile $plan $bytes $sha $where ($mode+'Witness.dll') $owner}catch{$errorValue=$_.Exception.Message}
 finally{foreach($o in $owners){Close-KmcScopedCompiler $o}}
 if($mode-ceq'none'){$script:originalOutput=$owner.output;$ok=$null-ne$assembly-and$script:loadAttempted-and$null-eq$errorValue}
 else{$ok=$null-eq$assembly-and-not$script:loadAttempted-and$errorValue-match'Generated compiler file|undeclared output'}
 if($ok){$pass++}else{$fail++};$rows+=[pscustomobject]@{name=$mode;status=$(if($ok){'PASS'}else{'FAIL'});loadAttempted=$script:loadAttempted;error=$errorValue}
}
$receipt=[ordered]@{schemaVersion=1;status=$(if($fail){'FAIL'}else{'PASS'});pass=$pass;fail=$fail;sourceSha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();rows=$rows;instrumentation='Two test-only scheduling/observation statements in otherwise exact Invoke method: after actual csc exit and immediately before actual LoadFrom. Private filesystem writes/hardlinks are real.';nativeExecution=$false;archiveOperations=0;limits=@('No OS per-writer provenance between compiler close and output lease','Does not claim arbitrary valid same-user replacement is detected')}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Write-Output ('Output admission '+$pass+' PASS / '+$fail+' FAIL');if($fail){throw 'Compiler output admission regression failed'}
