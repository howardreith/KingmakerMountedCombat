[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$SourceRoot,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve prior guard-effect evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$homeRoot=Split-Path $PSScriptRoot -Parent
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1')
$script:pass=0;$script:fail=0;$rows=[Collections.Generic.List[object]]::new();$sources=[Collections.Generic.List[object]]::new()
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;$rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Refuse([scriptblock]$Body,[string]$Pattern){$caught=$false;try{&$Body}catch{if($_.Exception.Message-cnotmatch$Pattern){throw};$caught=$true};Need $caught 'Expected refusal did not occur.'}
function Hash([string]$Path){(Get-FileHash -LiteralPath $Path).Hash.ToLowerInvariant()}
function PinCopy([string]$Relative){$from=Join-Path $SourceRoot $Relative;$to=Join-Path $repo $Relative;[void][IO.Directory]::CreateDirectory((Split-Path $to -Parent));[IO.File]::Copy($from,$to,$false);$sources.Add([pscustomobject]@{relativePath=$Relative;originalPath=$from;fixturePath=$to;sha256=(Hash $from)});return $to}
function Invoke-FixtureGit([string[]]$Arguments){
 $number=$script:gitInvocations.Count+1;$stem=Join-Path $OutputRoot ('git-'+$number.ToString('D2'))
 $stdoutPath=$stem+'.stdout.bin';$stderrPath=$stem+'.stderr.bin';$recordPath=$stem+'.json'
 $quoted=@($Arguments|ForEach-Object{if($null-eq$_-or$_-match'[\r\n\x00]'){throw 'Invalid private Git argument.'};'"'+[regex]::Replace([regex]::Replace($_,'(\\*)"','$1$1\"'),'(\\+)$','$1$1')+'"'})
 $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=$git;$start.Arguments=[string]::Join(' ',$quoted);$start.WorkingDirectory=$repo;$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
 $process=[Diagnostics.Process]::new();$process.StartInfo=$start;$stdout=$null;$stderr=$null;$started=$false
 $record=[pscustomobject]@{executable=$git;arguments=$Arguments;workingDirectory=$repo;processId=$null;exitCode=$null;stdoutPath=$stdoutPath;stderrPath=$stderrPath;error=$null}
 $script:gitInvocations.Add($record)
 try{
  $stdout=[IO.FileStream]::new($stdoutPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
  $stderr=[IO.FileStream]::new($stderrPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
  if(-not$process.Start()){throw 'Private Git did not start.'};$started=$true;$record.processId=$process.Id
  $stdoutCopy=$process.StandardOutput.BaseStream.CopyToAsync($stdout);$stderrCopy=$process.StandardError.BaseStream.CopyToAsync($stderr)
  if(-not$process.WaitForExit(30000)){try{$process.Kill();if(-not$process.WaitForExit(5000)){throw 'Owned Git process is still active.'}}catch{throw 'Private Git timed out and remains unsettled.'};throw 'Private Git exceeded its fixture deadline.'}
  $record.exitCode=$process.ExitCode;[void]$stdoutCopy.GetAwaiter().GetResult();[void]$stderrCopy.GetAwaiter().GetResult();$stdout.Flush();$stderr.Flush()
  if($record.exitCode-ne0){throw ('Private Git fixture failed with exit '+$record.exitCode+'; raw stderr retained at '+$stderrPath)}
 }catch{$record.error=$_.Exception.ToString();throw}
 finally{
  $releaseErrors=[Collections.Generic.List[Exception]]::new()
  foreach($handle in @($stdout,$stderr,$process)){if($null-ne$handle){try{$handle.Dispose()}catch{$releaseErrors.Add($_.Exception)}}}
  [IO.File]::WriteAllText($recordPath,($record|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
  if($releaseErrors.Count-ne0){throw [AggregateException]::new('Private Git fixture release failed.',$releaseErrors)}
 }
 return $record
}
function New-Entry([string]$Name,[byte[]]$Bytes,[long]$Length=-1){if($Length-lt0){$Length=$Bytes.Length};$o=[pscustomobject]@{FullName=$Name;Bytes=$Bytes;Length=$Length};$o|Add-Member ScriptMethod Open {return [IO.MemoryStream]::new([byte[]]$this.Bytes,$false)};return $o}
function Open-ScopedTestArchive([string]$Path){Need ($Path-ceq$script:opaquePath) 'Unexpected external archive request.';$script:archiveCalls++;return $script:archive}
function Set-Archive([object[]]$Entries){$script:archive=[pscustomobject]@{Entries=$Entries;Disposed=$false};$script:archive|Add-Member ScriptMethod Dispose {$this.Disposed=$true}}
function Run-Package([object[]]$Entries){Set-Archive $Entries;return (&$script:validator -PackagePath $script:opaquePath -PassThru)}
$repo=Join-Path $OutputRoot 'repo';$scratch=Join-Path $OutputRoot 'scratch';$frozen=Join-Path $OutputRoot 'frozen'
foreach($p in @($repo,$scratch,(Join-Path $scratch 'temp'),(Join-Path $scratch 'no-hooks'),(Join-Path $scratch 'compile-observation'),(Join-Path $scratch 'compile-package'),(Join-Path $scratch 'compile-wrong'),(Join-Path $repo 'obj/package-validation'))){[void][IO.Directory]::CreateDirectory($p)}
[IO.File]::WriteAllBytes((Join-Path $scratch 'git-empty.config'),[byte[]]@())
$git='C:\Program Files\Git\cmd\git.exe';$platform=[pscustomobject]@{git=$git;gitAutoCrlf='true'}
$runtime=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory().TrimEnd('\');$refs=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{Join-Path $runtime $_});$compiler=Join-Path $runtime 'csc.exe'
$compilerPlan=[pscustomobject]@{compiler=[pscustomobject]@{path=$compiler;configurationPath=($compiler+'.config');references=$refs;timeoutSeconds=30};files=@([pscustomobject]@{path=$compiler;role='csharp-compiler'},[pscustomobject]@{path=($compiler+'.config');role='platform-config'})+@($refs|ForEach-Object{[pscustomobject]@{path=$_;role='framework'}})}
$compilers=[Collections.Generic.List[object]]::new();$environments=[Collections.Generic.List[object]]::new();$leases=[Collections.Generic.List[IDisposable]]::new();$started=[DateTime]::UtcNow
$script:gitInvocations=[Collections.Generic.List[object]]::new();$script:archiveCalls=0;$script:opaquePath=Join-Path $OutputRoot 'opaque-package-bytes.bin';[IO.File]::WriteAllText($script:opaquePath,'Generated opaque marker; no ZIP is created or read.',[Text.UTF8Encoding]::new($false))
try{
 $environment=Enter-KmcScopedEnvironment $platform $repo $frozen $scratch $environments
 $validatorPath=PinCopy 'scripts/Validate-Package.ps1';$identityPath=PinCopy 'scripts/Get-AssemblyIdentity.ps1';$commonPath=PinCopy 'scripts/runtime/RuntimeHarness.Common.ps1';$versionPath=PinCopy 'version.json';$infoPath=PinCopy 'Info.json'
 [void](Invoke-FixtureGit @('init','--quiet',$repo))
 $warningFixture=Join-Path $repo 'git-warning-fixture.txt';[IO.File]::WriteAllText($warningFixture,"LF fixture`n",[Text.UTF8Encoding]::new($false))
 Check 'actual zero-exit Git warning is preserved and does not terminate fixture' {$r=Invoke-FixtureGit @('-C',$repo,'add','git-warning-fixture.txt');Need ($r.exitCode-eq0-and[IO.File]::ReadAllText($r.stderrPath)-cmatch'LF will be replaced by CRLF') 'Expected actual Git warning was not retained.';Need ([IO.File]::ReadAllText($warningFixture)-ceq"LF fixture`n") 'Git changed fixture working bytes.'}
 [void](Invoke-FixtureGit @('-C',$repo,'add','scripts','version.json','Info.json'))
 [void](Invoke-FixtureGit @('-C',$repo,'-c','user.name=ScopedGuard','-c','user.email=guard@example.invalid','commit','--quiet','-m','private-guard-fixture'))
 [void](Invoke-FixtureGit @('-C',$repo,'worktree','add','--quiet','--detach',$frozen,'HEAD'))
 Check 'actual nonzero Git exit remains failure with raw stderr retained' {$before=$script:gitInvocations.Count;Refuse {Invoke-FixtureGit @('-C',$repo,'rev-parse','--verify','KMC_SCOPED_MISSING_REF')|Out-Null} 'Private Git fixture failed with exit';$r=$script:gitInvocations[$before];Need ($r.exitCode-ne0-and[IO.File]::ReadAllBytes($r.stderrPath).Length-gt0-and-not[string]::IsNullOrEmpty($r.error)) 'Nonzero Git failure evidence was lost.'}
 $tokens=$null;$parseErrors=$null;$common=[Management.Automation.Language.Parser]::ParseInput([IO.File]::ReadAllText($commonPath),$commonPath,[ref]$tokens,[ref]$parseErrors)
 $function=@($common.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Get-KmcRepositoryRoot'},$true));Need ($parseErrors.Count-eq0-and$function.Count-eq1) 'Exact original repository guard absent.'
 $bound=[Management.Automation.Language.Parser]::ParseInput(($function[0].Extent.Text+[Environment]::NewLine+'Get-KmcRepositoryRoot'),$commonPath,[ref]$tokens,[ref]$parseErrors).GetScriptBlock()
 Check 'actual unchanged bare-git repository guard resolves admitted executable' {Assert-KmcScopedEnvironment $environment;Need ((&$bound)-ceq$repo) 'Original guard resolved foreign root.';Need ((Get-Command git -CommandType Application).Source-ceq$git) 'Bare git resolution differs.'}
 $observationSource=Join-Path $homeRoot 'src/ScopedPreflightObservation.cs';$observationOwner=New-KmcScopedCompilerOwner $compilers
 [void](Invoke-KmcScopedCompile $compilerPlan ([IO.File]::ReadAllBytes($observationSource)) (Hash $observationSource) (Join-Path $scratch 'compile-observation') 'ScopedObservation.dll' $observationOwner)
 $proof=[pscustomobject]@{documents=[pscustomobject]@{execution=[pscustomobject]@{repositoryRoot=$repo;frozenSourceRoot=$frozen}}};$gitMetadata=New-KmcScopedGitLease $proof $platform $leases
 Check 'original bare-git guard succeeds under exact held Git metadata' {Need ((&$bound)-ceq$repo) 'Original guard failed under leases.';[void]$gitMetadata.lease.VerifyUnchanged()}
 $version=[IO.File]::ReadAllText($versionPath)|ConvertFrom-Json
 $source='using System.Reflection;using System.Runtime.Versioning;[assembly:AssemblyVersion("'+$version.assemblyVersion+'")][assembly:AssemblyInformationalVersion("'+$version.productVersion+'")][assembly:TargetFramework(".NETFramework,Version=v4.7")]public sealed class ScopedPackageFixture{}'
 $bytes=[Text.Encoding]::UTF8.GetBytes($source);$sha=[Security.Cryptography.SHA256]::Create();try{$sourceHash=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 $dllOwner=New-KmcScopedCompilerOwner $compilers;[void](Invoke-KmcScopedCompile $compilerPlan $bytes $sourceHash (Join-Path $scratch 'compile-package') 'KingmakerMountedCombat.dll' $dllOwner);$dll=[IO.File]::ReadAllBytes($dllOwner.output)
 $wrongOwner=New-KmcScopedCompilerOwner $compilers;[void](Invoke-KmcScopedCompile $compilerPlan $bytes $sourceHash (Join-Path $scratch 'compile-wrong') 'WrongFixture.dll' $wrongOwner);$wrongDll=[IO.File]::ReadAllBytes($wrongOwner.output)
 $program=[IO.File]::ReadAllText($validatorPath);$needle='[IO.Compression.ZipFile]::OpenRead($resolvedPackage)';Need (([regex]::Matches($program,[regex]::Escape($needle))).Count-eq1) 'External archive seam is not exact.'
 $program=$program.Replace($needle,'(Open-ScopedTestArchive $resolvedPackage)');$ast=[Management.Automation.Language.Parser]::ParseInput($program,$validatorPath,[ref]$tokens,[ref]$parseErrors);Need ($parseErrors.Count-eq0) 'External archive-only adapter failed parsing.';$script:validator=$ast.GetScriptBlock()
 $info=[IO.File]::ReadAllBytes($infoPath);$infoEntry=New-Entry 'KingmakerMountedCombat/Info.json' $info;$dllEntry=New-Entry 'KingmakerMountedCombat/KingmakerMountedCombat.dll' $dll
 $parent=Join-Path $repo 'obj/package-validation';$scratchLease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(128,16777216,10000);$leases.Add($scratchLease);$scratchLease.AddTree($parent);$scratchLease.Seal()
 Check 'unchanged package body extracts only owned DLL and invokes actual exact child metadata guard' {$r=Run-Package @($infoEntry,$dllEntry);Need ($r.entries.Count-eq2-and$r.packageSha256-ceq(Hash $script:opaquePath)-and$r.dllSha256-ceq(Hash $dllOwner.output)-and$r.references.Count-eq1-and$r.references[0]-ceq'mscorlib') 'Actual package identity result differs.';Need $script:archive.Disposed 'External boundary not disposed.';[void]$scratchLease.VerifyUnchanged()}
 Check 'duplicate entry fails unchanged allowlist before scratch and child' {Refuse {Run-Package @($infoEntry,$infoEntry)|Out-Null} 'allowlist/duplicate';Need $script:archive.Disposed 'Duplicate archive not disposed.';[void]$scratchLease.VerifyUnchanged()}
 Check 'extra entry fails unchanged exact allowlist' {Refuse {Run-Package @($infoEntry,$dllEntry,(New-Entry 'other' ([byte[]]@(1))))|Out-Null} 'allowlist/duplicate';[void]$scratchLease.VerifyUnchanged()}
 Check 'empty DLL fails unchanged size guard' {Refuse {Run-Package @($infoEntry,(New-Entry $dllEntry.FullName ([byte[]]@())))|Out-Null} 'unsafe, empty, or oversized';[void]$scratchLease.VerifyUnchanged()}
 Check 'oversize DLL metadata fails unchanged limit without allocating data' {Refuse {Run-Package @($infoEntry,(New-Entry $dllEntry.FullName ([byte[]]@(1)) (8MB+1)))|Out-Null} 'unsafe, empty, or oversized';[void]$scratchLease.VerifyUnchanged()}
 Check 'Info identity mismatch fails exact original identity guard' {$bad=[Text.Encoding]::UTF8.GetBytes(([Text.Encoding]::UTF8.GetString($info)).Replace('KingmakerMountedCombat.Main.Load','Foreign.Main.Load'));Refuse {Run-Package @((New-Entry $infoEntry.FullName $bad),$dllEntry)|Out-Null} 'Info.json identity is not exact';[void]$scratchLease.VerifyUnchanged()}
 Check 'actual foreign assembly metadata fails then owned scratch settles' {Refuse {Run-Package @($infoEntry,(New-Entry $dllEntry.FullName $wrongDll))|Out-Null} 'DLL assembly identity is not exact';Need $script:archive.Disposed 'Failed identity boundary not disposed.';[void]$scratchLease.VerifyUnchanged()}
 Check 'planted undeclared package scratch cannot inherit empty-tree proof' {$planted=Join-Path $parent 'foreign.txt';[IO.File]::WriteAllText($planted,'planted');try{Refuse {[void]$scratchLease.VerifyUnchanged()} 'New observed file|Observation differs'}finally{[IO.File]::Delete($planted)}}
 Check 'restored empty namespace is only before-after equality' {[void]$scratchLease.VerifyUnchanged();Need (@(Get-ChildItem -LiteralPath $parent -Force).Count-eq0) 'Scratch residue exists.'}
 Check 'all original source bytes and scoped command environment remain unchanged' {foreach($pin in $sources){Need ((Hash $pin.fixturePath)-ceq$pin.sha256) 'Guard source changed.'};Assert-KmcScopedEnvironment $environment;[void]$gitMetadata.lease.VerifyUnchanged()}
}catch{$script:fail++;$rows.Add([pscustomobject]@{name='fixture setup or terminal failure';status='FAIL';error=$_.Exception.ToString()})}
finally{foreach($lease in $leases){$lease.Dispose()};foreach($c in $compilers){Close-KmcScopedCompiler $c};foreach($state in $environments){Exit-KmcScopedEnvironment $state}}
$receipt=[ordered]@{kind='actual-original-guard-bounded-effects';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=([DateTime]::UtcNow-$started).TotalSeconds;sourceSha256=(Hash (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightPlatform.ps1'));sources=@($sources);gitInvocations=@($script:gitInvocations);rows=@($rows);archiveBoundaryCalls=$script:archiveCalls;archiveOperations=0;externalSubstitutions=@('Only ZipFile.OpenRead returns in-memory generated entries; no archive created or read.');actualChildGuard='Unchanged Get-AssemblyIdentity.ps1 invoked by original Validate-Package through exact PSHOME powershell.exe';actualChildArguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File','original copied Get-AssemblyIdentity.ps1','-AssemblyPath','own GUID scratch DLL');policyLimit='Child vector is unchanged historical package guard; new scoped entry remains NoProfile/NonInteractive/File without a new policy flag.';nativeExecution=$false;freshGuardChainSuccess=$false;scope='Exact source-bound repository and package body behavior only; not real package validation or complete Option B qualification.'}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Original guard effects: {0} PASS / {1} FAIL'-f$script:pass,$script:fail);if($script:fail-ne0){throw 'Original guard effect tests failed.'}
