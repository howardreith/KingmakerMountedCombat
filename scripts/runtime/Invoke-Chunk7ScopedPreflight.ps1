[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$LabRoot,
 [Parameter(Mandatory=$true)][string]$FullProofPath,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$FullProofSha256,
 [Parameter(Mandatory=$true)][string]$ImplementationPath,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ImplementationSha256,
 [Parameter(Mandatory=$true)][string]$PlatformPath,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$PlatformSha256,
 [Parameter(Mandatory=$true)][string]$CasePath,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CaseSha256,
 [Parameter(Mandatory=$true)][string]$OutputRoot
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# A separately closed qualification, never a WhatIf cache or live-run authority.
# The exact Windows PowerShell/CLR standard bootstrap is trusted as a platform
# boundary, not retroactively called a pre-execution byte witness.
$started=[DateTimeOffset]::UtcNow
$rawOwners=[Collections.Generic.List[IDisposable]]::new()
$owned=[Collections.Generic.List[IDisposable]]::new()
$outputOwners=[Collections.Generic.List[object]]::new()
$compilerOwners=[Collections.Generic.List[object]]::new()
$environments=[Collections.Generic.List[object]]::new()
$rows=[Collections.Generic.List[object]]::new()
$generated=[Collections.Generic.HashSet[Reflection.Assembly]]::new()
$errors=[Collections.Generic.List[string]]::new()
$cleanupErrors=[Collections.Generic.List[string]]::new()
$bootstrapSaved=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($name in @('PATH','PATHEXT','PSModulePath','TEMP','TMP')){$bootstrapSaved.Add($name,[Environment]::GetEnvironmentVariable($name,'Process'))}
$oldAutoload=$ExecutionContext.SessionState.PSVariable.Get('PSModuleAutoLoadingPreference')
$hadAutoload=$null-ne$oldAutoload;$oldAutoloadValue=if($hadAutoload){$oldAutoload.Value}else{$null}
$originalDirectory=[Environment]::CurrentDirectory;$originalLocation=$null
$receipt=$null;$outputOwner=$null;$verified=$false;$released=$false;$actualExecutor=$null
function BootstrapPath([string]$Path,[string]$Parent){
 if($Path-cnotmatch'^[A-Za-z]:[\\/]' ){throw 'Bootstrap requires an absolute local path.'}
 $p=[IO.Path]::GetFullPath($Path).TrimEnd('\');$r=[IO.Path]::GetFullPath($Parent).TrimEnd('\')
 if(-not$p.StartsWith($r+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Bootstrap path escaped its declared parent.'}
 BootstrapNoReparse $p
 $p
}
function BootstrapNoReparse([string]$Path){
 if($Path-cnotmatch'^[A-Za-z]:[\\/]' -or$Path.Substring(2).Contains(':')){throw 'Bootstrap requires a literal local path.'}
 $current=[IO.Path]::GetFullPath($Path)
 while($current){
  if([IO.File]::Exists($current)-or[IO.Directory]::Exists($current)){if(([IO.File]::GetAttributes($current)-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Bootstrap reparse path refused.'}}
  $parent=[IO.Path]::GetDirectoryName($current);if($parent-eq$current){break};$current=$parent
 }
}
function BootstrapHold([string]$Path,[string]$Sha,[long]$Maximum){
 BootstrapNoReparse $Path
 $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 [void]$rawOwners.Add($stream)
 if($stream.Length-le0-or$stream.Length-gt$Maximum){throw 'Bootstrap input is empty or oversized.'}
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$actual=[BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
 if($actual-cne$Sha){throw 'Bootstrap immutable input differs.'}
 $stream.Position=0;$buffer=New-Object byte[] ([int]$stream.Length);$offset=0
 while($offset-lt$buffer.Length){$count=$stream.Read($buffer,$offset,$buffer.Length-$offset);if($count-eq0){throw 'Bootstrap input ended early.'};$offset+=$count}
 ,$buffer
}
function BootstrapRead([string]$Path,[string]$Sha,[long]$Maximum){
 $buffer=BootstrapHold $Path $Sha $Maximum
 $skip=if($buffer.Length-ge3-and$buffer[0]-eq239-and$buffer[1]-eq187-and$buffer[2]-eq191){3}else{0}
 [Text.UTF8Encoding]::new($false,$true).GetString($buffer,$skip,$buffer.Length-$skip)
}
function Get-KmcScopedSelectedHistoricalProof([string]$Path,[string]$Sha256,[string]$Root,$Owners){
 $analysis=Join-Path (Get-KmcScopedPath $Root) 'analysis-cache'
 $pin=[pscustomobject]@{path=(Assert-KmcScopedChild $Path $analysis 'selected historical proof');sha256=$Sha256;bytes=[long](Get-Item -LiteralPath $Path).Length}
 $header=Read-KmcScopedPinnedJson $pin $Owners $analysis
 if($header.kind-isnot[string]){throw 'Historical proof requires an exact envelope kind.'}
 if($header.kind-ceq'closed-preflight-inherited-full-proof'){
  $selected=Get-KmcScopedInheritedProof $Path $Sha256 $Root $Owners
  $basis=[pscustomobject]@{kind='original-successful-full-audit';originalSupervisorStatus=$selected.documents.observation.status;originalCallerExitCode=$selected.documents.callerExit.exitCode;readerOnlyReevaluation=$false}
 }elseif($header.kind-ceq'full-whatif-formatting-reevaluation-proof'){
  $selected=Get-KmcReevaluatedFullAuditProof $Path $Sha256 $Root $Owners
  $basis=[pscustomobject]@{kind='separate-immutable-formatting-reevaluation';originalSupervisorStatus=$selected.documents.observation.status;originalCallerExitCode=$selected.documents.callerExit.exitCode;readerOnlyReevaluation=$true;reevaluationStatus=$selected.reevaluation.status;rereadCallerExitCode=$selected.rereadCallerExit.exitCode;reevaluationPins=$selected.proof.pins}
 }else{throw 'Historical proof envelope kind is outside the two selected contracts.'}
 $selected|Add-Member NoteProperty evidenceProvenance $basis
 $selected
}

try{
 # No inherited module/compiler search path may run before the compiler guard.
 # These exact installed standard cmdlets/CLR are the explicit bootstrap trust
 # boundary. Their actual bytes/loaded assemblies are subsequently pinned before
 # the owned compiler or any original project guard can execute.
 foreach($name in [Environment]::GetEnvironmentVariables('Process').Keys){
  if($name-like'COR_*'-or$name-like'COMPLUS_*'-or$name-ceq'DEVPATH'){throw 'Unreviewed CLR startup override is present.'}
 }
 [Environment]::SetEnvironmentVariable('PATH',($PSHOME+';'+[Environment]::SystemDirectory),'Process')
 [Environment]::SetEnvironmentVariable('PATHEXT','.EXE','Process')
 [Environment]::SetEnvironmentVariable('PSModulePath',($PSHOME+'\Modules'),'Process')
 $global:PSModuleAutoLoadingPreference='None'
 Import-Module -Name ($PSHOME+'\Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop
 Import-Module -Name ($PSHOME+'\Modules\Microsoft.PowerShell.Management\Microsoft.PowerShell.Management.psd1') -ErrorAction Stop
 $originalLocation=(Get-Location).Path
 $LabRoot=[IO.Path]::GetFullPath($LabRoot).TrimEnd('\');$analysis=Join-Path $LabRoot 'analysis-cache';$repo=Join-Path $LabRoot 'repo/KingmakerMountedCombat'
 $ImplementationPath=BootstrapPath $ImplementationPath $analysis
 $PlatformPath=BootstrapPath $PlatformPath $analysis;$CasePath=BootstrapPath $CasePath $analysis;$FullProofPath=BootstrapPath $FullProofPath $analysis
 $OutputRoot=BootstrapPath $OutputRoot $analysis
 $entry=[IO.Path]::GetFullPath($PSCommandPath)
 if($entry-cne(Join-Path $repo 'scripts/runtime/Invoke-Chunk7ScopedPreflight.ps1')){throw 'Entry must run from its exact reviewed repository path.'}
 if($PSVersionTable.PSVersion.Major-ne5-or$PSVersionTable.PSVersion.Minor-ne1-or-not[Environment]::Is64BitProcess){throw 'Only the reviewed Windows PowerShell5.1 x64 platform is selected.'}
 $rawArgs=[Environment]::GetCommandLineArgs()
 $expectedArgs=@($rawArgs[0],'-NoProfile','-NonInteractive','-File',$entry)
 foreach($name in @('LabRoot','FullProofPath','FullProofSha256','ImplementationPath','ImplementationSha256','PlatformPath','PlatformSha256','CasePath','CaseSha256','OutputRoot')){$expectedArgs+=('-'+$name);$expectedArgs+=(Get-Variable -Name $name -ValueOnly)}
 if($rawArgs.Count-ne$expectedArgs.Count){throw 'The entry requires its exact explicit NoProfile argument vector.'}
 for($i=0;$i-lt$rawArgs.Count;$i++){if($rawArgs[$i]-cne$expectedArgs[$i]){throw 'The entry argument vector differs from its closed invocation.'}}
 $implementationText=BootstrapRead $ImplementationPath $ImplementationSha256 1MB
 $platformText=BootstrapRead $PlatformPath $PlatformSha256 1MB
 $caseText=BootstrapRead $CasePath $CaseSha256 16KB
 $initial=$implementationText|ConvertFrom-Json
 if($initial.commandFiles-isnot[Array]-or$initial.commandFiles.Count-ne7){throw 'The reviewed bootstrap executable pin set is incomplete.'}
 $required=@('Invoke-Chunk7ScopedPreflight.ps1','ScopedPreflightProof.ps1','ScopedPreflightClosure.ps1','ScopedPreflightGuards.ps1','ScopedPreflightPlatform.ps1','ScopedPreflightObservation.cs','ScopedPreflightFullAuditReevaluation.ps1')
 $sources=@{};$sourcePins=@{}
 foreach($leaf in $required){
  $relative='scripts/runtime/'+$leaf;$pins=@($initial.commandFiles|Where-Object{$_.relativePath-is[string]-and$_.relativePath-ceq$relative})
  if($pins.Count-ne1-or$pins[0].sha256-isnot[string]-or$pins[0].sha256-cnotmatch'^[0-9a-f]{64}$'){throw 'The reviewed bootstrap code role is missing or ambiguous.'}
  $path=BootstrapPath (Join-Path $repo $relative) $repo
  $sources[$leaf]=BootstrapRead $path $pins[0].sha256 1MB;$sourcePins[$leaf]=$pins[0]
 }
 # No imported source body or compiler runs until all of its selected platform
 # files have read-sharing byte leases. Native file/ancestor IDs are then checked
 # by the owned observer before original guard imports or source qualification.
 $preliminaryPlatform=$platformText|ConvertFrom-Json
 if($preliminaryPlatform.files-isnot[Array]-or$preliminaryPlatform.files.Count-lt4-or$preliminaryPlatform.files.Count-gt2048){throw 'Bootstrap platform closure is not bounded.'}
 $platformBytes=0L
 foreach($pin in $preliminaryPlatform.files){
  if($pin.path-isnot[string]-or$pin.sha256-isnot[string]-or$pin.sha256-cnotmatch'^[0-9a-f]{64}$'-or($pin.bytes-isnot[int]-and$pin.bytes-isnot[long])-or$pin.bytes-lt1-or$pin.bytes-gt1GB){throw 'Bootstrap platform pin differs.'}
  $platformBytes+=$pin.bytes;if($platformBytes-gt1GB){throw 'Bootstrap platform byte bound exceeded.'}
  $bytes=BootstrapHold $pin.path $pin.sha256 $pin.bytes
  if($bytes.Length-ne$pin.bytes){throw 'Bootstrap platform length differs.'}
 }
 foreach($leaf in @('ScopedPreflightProof.ps1','ScopedPreflightClosure.ps1','ScopedPreflightPlatform.ps1','ScopedPreflightGuards.ps1','ScopedPreflightFullAuditReevaluation.ps1')){
  $tokens=$null;$parseErrors=$null;$a=[Management.Automation.Language.Parser]::ParseInput($sources[$leaf],(Join-Path $repo ('scripts/runtime/'+$leaf)),[ref]$tokens,[ref]$parseErrors)
  if($parseErrors.Count-ne0){throw 'Reviewed bootstrap helper parse failed.'};. ($a.GetScriptBlock())
 }
 $implementation=ConvertFrom-KmcScopedJson $implementationText 'reviewed implementation'
 $platformPlan=ConvertFrom-KmcScopedJson $platformText 'reviewed platform'
 $plan=ConvertFrom-KmcScopedJson $caseText 'selected case';Assert-KmcScopedCase $plan
 $platformChecked=Assert-KmcScopedPlatformPlan $platformPlan
 Assert-KmcScopedString ([Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) $platformPlan.powerShell 'current executor'
 Assert-KmcScopedString $rawArgs[0] $platformPlan.powerShell 'raw executor'
 Assert-KmcScopedString $originalDirectory $repo 'entry working directory'
 Assert-KmcScopedString $originalLocation $repo 'entry PowerShell location'
 $currentProcess=[Diagnostics.Process]::GetCurrentProcess()
 try{$actualExecutor=[pscustomobject]@{path=$platformPlan.powerShell;sha256=(@($platformPlan.files|Where-Object{$_.path-ceq$platformPlan.powerShell})[0].sha256);rawArguments=@($rawArgs);workingDirectory=$originalDirectory;powerShellLocation=$originalLocation;processId=$currentProcess.Id;startedAtUtc=$currentProcess.StartTime.ToUniversalTime().ToString('o');psVersion=$PSVersionTable.PSVersion.ToString();clrVersion=[Environment]::Version.ToString()}}finally{$currentProcess.Dispose()}
 foreach($a in [AppDomain]::CurrentDomain.GetAssemblies()){if($a.IsDynamic){if($a.GetName().Name-cne'Anonymously Hosted DynamicMethods Assembly'){throw 'Unexpected initial dynamic assembly.'};[void]$generated.Add($a)}}
 $bootstrapAssemblies=Assert-KmcScopedLoadedAssemblies $platformChecked $generated
 $outputOwner=New-KmcScopedOutputOwner $OutputRoot $outputOwners
 $scratch=Join-Path $OutputRoot 'scratch'
 foreach($p in @($scratch,(Join-Path $scratch 'temp'),(Join-Path $scratch 'no-hooks'),(Join-Path $scratch 'compile-observation'),(Join-Path $scratch 'compile-inventory'))){[void][IO.Directory]::CreateDirectory($p)}
 [IO.File]::WriteAllBytes((Join-Path $scratch 'git-empty.config'),[byte[]]@())
 # This early scope uses the repository in both safe-directory roles. No Git
 # command executes until the real frozen root is proved and a fresh scope binds it.
 $bootstrapEnvironment=Enter-KmcScopedEnvironment $platformPlan $repo $repo $scratch $environments
 if('KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation'-as[type]){throw 'Observation type was already present before the owned compile.'}
 $observerCompiler=New-KmcScopedCompilerOwner $compilerOwners
 $observerBytes=[IO.File]::ReadAllBytes((Join-Path $repo 'scripts/runtime/ScopedPreflightObservation.cs'))
 $observerAssembly=Invoke-KmcScopedCompile $platformPlan $observerBytes $sourcePins['ScopedPreflightObservation.cs'].sha256 (Join-Path $scratch 'compile-observation') 'ScopedObservation.dll' $observerCompiler
 [void]$generated.Add($observerAssembly)
 if($null-eq$observerAssembly.GetType('KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation',$false)){throw 'Owned observer compile has no exact type.'}
 Seal-KmcScopedOutputOwner $outputOwner
 $platform=New-KmcScopedPlatformLease $platformPlan $owned
 $argumentPins=New-KmcScopedObservation $owned 3 3MB
 foreach($path in @($ImplementationPath,$PlatformPath,$CasePath)){$argumentPins.AddFile($path)}
 $argumentPins.Seal()
 $argumentPins.RequirePinnedFile($ImplementationPath,([IO.FileInfo]$ImplementationPath).Length,$ImplementationSha256)
 $argumentPins.RequirePinnedFile($PlatformPath,([IO.FileInfo]$PlatformPath).Length,$PlatformSha256)
 $argumentPins.RequirePinnedFile($CasePath,([IO.FileInfo]$CasePath).Length,$CaseSha256)
 $proof=Get-KmcScopedSelectedHistoricalProof $FullProofPath $FullProofSha256 $LabRoot $owned
 if($proof.evidenceProvenance.readerOnlyReevaluation){
  foreach($pair in @(@('reader','ScopedPreflightFullAuditReevaluation.ps1'),@('proofHelper','ScopedPreflightProof.ps1'),@('observationSource','ScopedPreflightObservation.cs'))){Assert-KmcScopedString $proof.plan.($pair[0]).sha256 $sourcePins[$pair[1]].sha256 ('shared reread dependency '+$pair[0])}
 }
 $code=New-KmcScopedCodeLease $proof $implementation $owned $platform
 $namespaces=New-KmcScopedObservation $owned
 $namespaces.AddNamespaceTree($repo);$namespaces.AddNamespaceTree($proof.documents.execution.frozenSourceRoot);$namespaces.Seal()
 $inputs=Get-KmcScopedDynamicInputs $proof $plan $owned
 foreach($scope in $inputs.scopes){
  if($OutputRoot-ieq$scope.path-or$OutputRoot.StartsWith($scope.path+'\',[StringComparison]::OrdinalIgnoreCase)-or$scope.path.StartsWith($OutputRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output/scratch overlaps an admitted input scope.'}
 }
 Exit-KmcScopedEnvironment $bootstrapEnvironment
 $environment=Enter-KmcScopedEnvironment $platformPlan $repo $proof.documents.execution.frozenSourceRoot $scratch $environments
 $git=New-KmcScopedGitLease $proof $platformPlan $owned
 $tracked=@(& $platformPlan.git -C $repo ls-files);if($LASTEXITCODE-ne0){throw 'Current tracked input enumeration failed.'};Assert-KmcScopedTrackedPathSet $tracked $code.expected
 # Original package extraction is declared. Baseline/final empty parents do not
 # purport to observe every transient write inside those owned scratch spaces.
 $packageScratch=Join-Path $repo 'obj/package-validation'
 Assert-KmcScopedString $proof.documents.execution.packageValidationScratchRoot $packageScratch 'package scratch'
 if(-not[IO.Directory]::Exists($packageScratch)-or@(Get-ChildItem -LiteralPath $packageScratch -Force).Count-ne0){throw 'Original package-validation scratch must already exist and be empty.'}
 $scratchLease=New-KmcScopedObservation $owned 128 16MB
 $scratchLease.AddTree($packageScratch);$scratchLease.AddTree((Join-Path $scratch 'temp'));$scratchLease.AddTree((Join-Path $scratch 'no-hooks'));$scratchLease.AddFile((Join-Path $scratch 'git-empty.config'));$scratchLease.Seal()
 if('KingmakerMountedCombat.RuntimeTooling.InventoryHasher'-as[type]){throw 'Original hasher was present before its owned exact-source compile.'}
 $hasherPath=Join-Path $repo 'scripts/runtime/InventoryHasher.cs'
 $hasherPin=@($code.expected|Where-Object{$_.path-ceq$hasherPath})
 if($hasherPin.Count-ne1-or$hasherPin[0].provenance-cne'unchanged-full-audit-input'){throw 'Original hasher lacks its unchanged source pin.'}
 $hasherBytes=[IO.File]::ReadAllBytes($hasherPath)
 $hasherCompiler=New-KmcScopedCompilerOwner $compilerOwners
 $hasherAssembly=Invoke-KmcScopedCompile $platformPlan $hasherBytes $hasherPin[0].sha256 (Join-Path $scratch 'compile-inventory') 'ScopedInventory.dll' $hasherCompiler
 [void]$generated.Add($hasherAssembly)
 if($null-eq$hasherAssembly.GetType('KingmakerMountedCombat.RuntimeTooling.InventoryHasher',$false)){throw 'Owned original hasher compile has no exact type.'}
 $beforeAssemblies=Assert-KmcScopedLoadedAssemblies $platform $generated
 [Environment]::CurrentDirectory=$repo;Set-Location -LiteralPath $repo
 Assert-KmcScopedEnvironment $environment
 $result=Invoke-KmcScopedFreshGuards $proof $code $inputs $owned $rows
 Assert-KmcScopedEnvironment $environment
 [void](Assert-KmcScopedPlatformPlan $platformPlan)
 $afterAssemblies=Assert-KmcScopedLoadedAssemblies $platform $generated
 Assert-KmcScopedLeases $owned
 [void]$outputOwner.lease.VerifyUnchanged()
 $verified=$true
 $compilers=@($compilerOwners|ForEach-Object{[pscustomobject]@{source=$_.source;output=$_.output;processStarted=$_.processStarted;processId=$_.processId;arguments=$_.arguments;exitCode=$_.exitCode;outputSha256=$_.outputSha256;outputProvenance=$_.outputProvenance}})
 $receipt=[ordered]@{schemaVersion=1;kind='closed-chunk7-scoped-preflight';status='IN PROGRESS';runId=$plan.runId;scenario=$plan.scenario;startedUtc=$started.ToString('o');nativeExecution=$false;freshWholeRootPurity=$false;historicalRootsStatus='NOT RE-EVALUATED';historicalRoots=$inputs.historicalRootsNotReevaluated;inheritedProofSha256=$proof.sha256;inheritedEvidence=$proof.evidenceProvenance;historicalHarness=$proof.documents.execution.harness;currentHarness=$code.expectedHarness;implementationSha256=$ImplementationSha256;platformSha256=$PlatformSha256;caseSha256=$CaseSha256;guards=@($rows);code=$code.expected;inputScopes=$inputs.scopes;observations=@($owned|ForEach-Object{@($_.Before)});platformBootstrap=$bootstrapAssemblies;platformBefore=$beforeAssemblies;platformAfter=$afterAssemblies;compilerProcesses=$compilers;platformAssumptions=@('Windows kernel, CLR loader and exact standard PowerShell bootstrap','Windows system native DLL resolution, not an all-module image scan','Exact owned compiler and exclusive private scratch until generated-output read lease; no OS per-writer witness','initial PowerShell DLR assembly instance','per-path observations, not an atomic global snapshot or transient-write witness');scratchEffects=@('private caller output and exact explicit compiler outputs','original package validator GUID extraction and finally deletion');result=$result;errors=@()}
}catch{$errors.Add($_.Exception.ToString())}
finally{
 # Each owner is attempted independently. A failed run can still report complete
 # resource release; prior guard failures are never erased by successful cleanup.
 $compilerSettled=$true
 foreach($compiler in $compilerOwners){try{Close-KmcScopedCompiler $compiler}catch{$compilerSettled=$false;$cleanupErrors.Add($_.Exception.ToString())}}
 for($i=$environments.Count-1;$i-ge0;$i--){try{Exit-KmcScopedEnvironment $environments[$i]}catch{$cleanupErrors.Add($_.Exception.ToString())}}
 if($compilerSettled){
  foreach($lease in $owned){try{$lease.Dispose()}catch{$cleanupErrors.Add($_.Exception.ToString())}}
  foreach($lease in $rawOwners){try{$lease.Dispose()}catch{$cleanupErrors.Add($_.Exception.ToString())}}
 }else{$cleanupErrors.Add('Unsettled compiler retains its dependency handles; no qualification is eligible.')}
 foreach($name in $bootstrapSaved.Keys){try{[Environment]::SetEnvironmentVariable($name,$bootstrapSaved[$name],'Process')}catch{$cleanupErrors.Add($_.Exception.ToString())}}
 try{if($hadAutoload){$global:PSModuleAutoLoadingPreference=$oldAutoloadValue}else{$ExecutionContext.SessionState.PSVariable.Remove('PSModuleAutoLoadingPreference')}}catch{$cleanupErrors.Add($_.Exception.ToString())}
 try{[Environment]::CurrentDirectory=$originalDirectory;if($null-ne$originalLocation){Set-Location -LiteralPath $originalLocation}}catch{$cleanupErrors.Add($_.Exception.ToString())}
 $released=$cleanupErrors.Count-eq0
 foreach($error in $cleanupErrors){$errors.Add($error)}
}
try{
 if($null-ne$outputOwner-and$null-ne$outputOwner.receiptHandle){
  if($null-eq$receipt){$receipt=[ordered]@{schemaVersion=1;kind='closed-chunk7-scoped-preflight';status='FAIL';startedUtc=$started.ToString('o');nativeExecution=$false;freshWholeRootPurity=$false;historicalRootsStatus='NOT RE-EVALUATED';guards=@($rows);errors=@()}}
  $receipt.status=if($verified-and$released-and$errors.Count-eq0){'PASS'}else{'FAIL'}
  $receipt['actualExecutor']=$actualExecutor
  $receipt['compilerProcesses']=@($compilerOwners|ForEach-Object{[pscustomobject]@{source=$_.source;output=$_.output;processStarted=$_.processStarted;processId=$_.processId;arguments=$_.arguments;exitCode=$_.exitCode;outputSha256=$_.outputSha256;outputProvenance=$_.outputProvenance}})
  $receipt.errors=@($errors);$receipt['finishedUtc']=[DateTimeOffset]::UtcNow.ToString('o');$receipt['ownedInputHandlesReleased']=$released
  Publish-KmcScopedOutput $outputOwner $receipt
 }
}finally{
 foreach($owner in $outputOwners){try{Close-KmcScopedOutput $owner}catch{$released=$false;$errors.Add($_.Exception.ToString())}}
}
if(-not$verified-or-not$released-or$errors.Count-ne0){throw ('Closed scoped preflight FAIL: '+[string]::Join([Environment]::NewLine,$errors))}
Write-Output 'Closed scoped preflight PASS; exact selected guards ran freshly. Unreferenced historical roots NOT RE-EVALUATED. No native execution authorized.'
