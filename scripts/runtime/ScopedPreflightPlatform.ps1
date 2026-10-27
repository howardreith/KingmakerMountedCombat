# Bounded platform leases. OS/CLR remain explicit external assumptions.
Set-StrictMode -Version Latest
function Assert-KmcScopedPlatformPlan($Platform){
 Assert-KmcScopedMembers $Platform @('schemaVersion','kind','files','powerShell','git','gitAutoCrlf','loadedAssemblyPaths','compiler','culture','uiCulture','timeZoneId') 'platform closure'
 Assert-KmcScopedInteger $Platform.schemaVersion 1 1 'platform schema';Assert-KmcScopedString $Platform.kind 'reviewed-scoped-preflight-platform' 'platform kind'
 if($Platform.files-isnot[Array]-or$Platform.files.Count-lt4-or$Platform.files.Count-gt2048){throw 'Platform file closure must be an explicit bounded array.'}
 Assert-KmcScopedString $Platform.gitAutoCrlf 'true' 'reviewed Git text normalization'
 Assert-KmcScopedString $Platform.culture ([Globalization.CultureInfo]::CurrentCulture.Name) 'current culture'
 Assert-KmcScopedString $Platform.uiCulture ([Globalization.CultureInfo]::CurrentUICulture.Name) 'current UI culture'
 Assert-KmcScopedString $Platform.timeZoneId ([TimeZoneInfo]::Local.Id) 'current time zone'
 $paths=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($pin in $Platform.files){
  Assert-KmcScopedMembers $pin @('path','sha256','bytes','nativeIdentity','linkCount','role') 'platform file'
  if($pin.path-isnot[string]-or-not$paths.Add((Get-KmcScopedPath $pin.path))){throw 'Platform path is missing or duplicated.'}
  Assert-KmcScopedSha $pin.sha256 'platform bytes';Assert-KmcScopedInteger $pin.bytes 1 1GB 'platform byte length';Assert-KmcScopedInteger $pin.linkCount 1 1024 'platform link count'
  if($pin.nativeIdentity-isnot[string]-or$pin.nativeIdentity-cnotmatch'^[0-9a-f]{8}:[0-9a-f]{16}$'-or$pin.role-isnot[string]-or$pin.role-cnotin@('powershell','git-dispatcher','git-implementation','git-runtime','framework','platform-config','powershell-module','csharp-compiler')){throw 'Platform identity or role is outside the reviewed closure.'}
 }
 foreach($tool in @('powerShell','git')){if($Platform.$tool-isnot[string]-or-not$paths.Contains($Platform.$tool)){throw 'Exact tool is absent from the platform closure.'}}
 if(@($Platform.files|Where-Object{$_.path-ceq$Platform.powerShell-and$_.role-ceq'powershell'}).Count-ne1-or@($Platform.files|Where-Object{$_.path-ceq$Platform.git-and$_.role-ceq'git-dispatcher'}).Count-ne1){throw 'Platform tool roles are not exact.'}
 if($Platform.loadedAssemblyPaths-isnot[Array]-or$Platform.loadedAssemblyPaths.Count-lt1-or$Platform.loadedAssemblyPaths.Count-gt256){throw 'Platform managed-assembly closure is not bounded.'}
 $assemblies=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($p in $Platform.loadedAssemblyPaths){if($p-isnot[string]-or-not$paths.Contains($p)-or-not$assemblies.Add($p)){throw 'Loaded assembly is outside the pinned platform file closure.'}}
 Assert-KmcScopedCompilerPlan $Platform
 [pscustomobject]@{plan=$Platform;allowedAssemblyPaths=$assemblies}
}
function New-KmcScopedPlatformLease($Platform,$Owned){
 $checked=Assert-KmcScopedPlatformPlan $Platform
 $lease=New-KmcScopedObservation $Owned 4096 1GB
 foreach($pin in $Platform.files){$lease.AddPlatformFile($pin.path,$pin.nativeIdentity,[uint32]$pin.linkCount,$pin.bytes,$pin.sha256)}
 $lease.Seal()
 [pscustomobject]@{lease=$lease;plan=$Platform;allowedAssemblyPaths=$checked.allowedAssemblyPaths}
}
function Assert-KmcScopedLoadedAssemblies($Platform,$GeneratedAssemblies){
 $rows=[Collections.Generic.List[object]]::new()
 foreach($a in [AppDomain]::CurrentDomain.GetAssemblies()){
  # Generated assembly instances must have been captured immediately after a
  # declared compiler/LoadFrom return, never guessed from a type-name resemblance.
  if($GeneratedAssemblies.Contains($a)){continue}
  if($a.IsDynamic-or[string]::IsNullOrEmpty($a.Location)){throw 'Unexplained dynamic or fileless managed assembly is present.'}
  $location=$a.Location
  if(-not$Platform.allowedAssemblyPaths.Contains($location)){throw ('Managed platform dependency is outside the exact reviewed closure: '+$a.GetName().Name)}
  $rows.Add([pscustomobject]@{path=$location;name=$a.GetName().Name;mvid=$a.ManifestModule.ModuleVersionId.ToString('D')})
 }
 @($rows|Sort-Object path)
}

function Enter-KmcScopedEnvironment($Platform,[string]$RepositoryRoot,[string]$FrozenRoot,[string]$ScratchRoot,$Owners){
 if($null-eq$Owners){throw 'An external environment owner is required.'}
 # Never serialize inherited values: they exist solely for finally restoration.
 $saved=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
 $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($name in [Environment]::GetEnvironmentVariables('Process').Keys){if($name-like'GIT_*'){[void]$names.Add($name)}}
 foreach($name in @('PATH','PATHEXT','TEMP','TMP','PSModulePath','LANG','LANGUAGE','LC_ALL','VSLANG')){[void]$names.Add($name)}
 $settings=[ordered]@{GIT_OPTIONAL_LOCKS='0';GIT_CONFIG_NOSYSTEM='1';GIT_CONFIG_GLOBAL=(Join-Path $ScratchRoot 'git-empty.config');GIT_ATTR_NOSYSTEM='1';GIT_NO_LAZY_FETCH='1';GIT_NO_REPLACE_OBJECTS='1';GIT_TERMINAL_PROMPT='0';GIT_CONFIG_COUNT='13';LANG='C';LANGUAGE='C';LC_ALL='C';VSLANG='1033';PATH=((Split-Path $Platform.git -Parent)+';'+$PSHOME+';'+[Environment]::SystemDirectory);PATHEXT='.EXE';TEMP=(Join-Path $ScratchRoot 'temp');TMP=(Join-Path $ScratchRoot 'temp');PSModulePath=(Join-Path $PSHOME 'Modules')}
 $config=@(@('core.fsmonitor','false'),@('core.untrackedCache','false'),@('core.preloadIndex','false'),@('core.fscache','false'),@('gc.auto','0'),@('maintenance.auto','false'),@('core.hooksPath',(Join-Path $ScratchRoot 'no-hooks')),@('core.attributesFile',(Join-Path $ScratchRoot 'git-empty.config')),@('core.excludesFile',(Join-Path $ScratchRoot 'git-empty.config')),@('core.autocrlf',$Platform.gitAutoCrlf),@('safe.directory',$RepositoryRoot),@('safe.directory',$FrozenRoot),@('protocol.allow','never'))
 for($i=0;$i-lt$config.Count;$i++){$settings['GIT_CONFIG_KEY_'+$i]=$config[$i][0];$settings['GIT_CONFIG_VALUE_'+$i]=$config[$i][1]}
 foreach($name in $settings.Keys){[void]$names.Add($name)}
 foreach($name in $names){$saved.Add($name,[Environment]::GetEnvironmentVariable($name,'Process'))}
 $preference=Get-Variable -Name PSModuleAutoLoadingPreference -Scope Global -ErrorAction SilentlyContinue
 $state=[pscustomobject]@{saved=$saved;settings=$settings;restored=$false;hadPreference=($null-ne$preference);oldModulePreference=$(if($null-ne$preference){$preference.Value}else{$null})}
 [void]$Owners.Add($state)
 if(-not[IO.File]::Exists($settings.GIT_CONFIG_GLOBAL)-or([IO.File]::ReadAllBytes($settings.GIT_CONFIG_GLOBAL)).Length-ne0-or-not[IO.Directory]::Exists($settings.TEMP)-or-not[IO.Directory]::Exists((Join-Path $ScratchRoot 'no-hooks'))){throw 'Exact owned scratch environment is incomplete.'}
 foreach($name in $saved.Keys){if($name-like'GIT_*'){[Environment]::SetEnvironmentVariable($name,$null,'Process')}}
 foreach($name in $settings.Keys){[Environment]::SetEnvironmentVariable($name,$settings[$name],'Process')}
 $global:PSModuleAutoLoadingPreference='None'
 $commands=@(Get-Command git -All -ErrorAction Stop)
 if($commands.Count-ne1-or$commands[0].CommandType-ne[Management.Automation.CommandTypes]::Application-or$commands[0].Source-cne$Platform.git){throw 'Git command resolution differs from the pinned dispatcher.'}
 $state
}
function Assert-KmcScopedEnvironment($State){
 if($State.restored){throw 'The scoped environment was already released.'}
 foreach($key in $State.settings.Keys){if([Environment]::GetEnvironmentVariable($key,'Process')-cne$State.settings[$key]){throw ('Scoped execution environment changed: '+$key)}}
 $expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($key in $State.settings.Keys){if($key-like'GIT_*'){[void]$expected.Add($key)}}
 foreach($key in [Environment]::GetEnvironmentVariables('Process').Keys){if($key-like'GIT_*'-and-not$expected.Contains($key)){throw 'An unreviewed Git environment input appeared.'}}
 if($global:PSModuleAutoLoadingPreference-cne'None'){throw 'PowerShell module autoload changed.'}
}
function Exit-KmcScopedEnvironment($State){
 if($null-eq$State-or$State.restored){return}
 $errors=[Collections.Generic.List[Exception]]::new()
 foreach($key in [Environment]::GetEnvironmentVariables('Process').Keys){if($key-like'GIT_*'-and-not$State.saved.ContainsKey($key)){try{[Environment]::SetEnvironmentVariable($key,$null,'Process')}catch{$errors.Add($_.Exception)}}}
 foreach($key in $State.saved.Keys){try{[Environment]::SetEnvironmentVariable($key,$State.saved[$key],'Process')}catch{$errors.Add($_.Exception)}}
 try{if($State.hadPreference){$global:PSModuleAutoLoadingPreference=$State.oldModulePreference}else{Remove-Variable -Name PSModuleAutoLoadingPreference -Scope Global -ErrorAction SilentlyContinue}}catch{$errors.Add($_.Exception)}
 if($errors.Count-ne0){throw [AggregateException]::new('Scoped environment restoration is unfinished.',$errors)}
 $State.restored=$true
}
function New-KmcScopedGitLease($Proof,$Platform,$Owned){
 $repo=$Proof.documents.execution.repositoryRoot;$frozen=$Proof.documents.execution.frozenSourceRoot
 $git=Join-Path $repo '.git';if(-not[IO.Directory]::Exists($git)){throw 'Only the exact primary repository Git directory is selected.'}
 $lease=New-KmcScopedObservation $Owned 32768 1GB
 $lease.AddTree($git);$lease.AddFile((Join-Path $frozen '.git'));$lease.Seal()
 foreach($relative in @('objects/info/alternates','objects/info/http-alternates','commondir','config.worktree','info/grafts','shallow')){if(Test-Path -LiteralPath (Join-Path $git $relative)){throw 'Git metadata has unsupported external or per-worktree indirection.'}}
 $pointer=[IO.File]::ReadAllText((Join-Path $frozen '.git')).Trim()
 if($pointer-cnotmatch'^gitdir: (.+)$'){throw 'Frozen worktree Git pointer is invalid.'}
 $expected=Assert-KmcScopedChild $Matches[1] (Join-Path $git 'worktrees') 'frozen Git metadata'
 if(-not[IO.Directory]::Exists($expected)){throw 'Frozen Git metadata is absent.'}
 $common=[IO.File]::ReadAllText((Join-Path $expected 'commondir')).Trim();$back=[IO.File]::ReadAllText((Join-Path $expected 'gitdir')).Trim()
 Assert-KmcScopedString ([IO.Path]::GetFullPath((Join-Path $expected $common))) $git 'frozen common Git directory'
 Assert-KmcScopedString ([IO.Path]::GetFullPath($back)) (Join-Path $frozen '.git') 'frozen Git reverse pointer'
 if(Test-Path -LiteralPath (Join-Path $expected 'config.worktree')){throw 'Frozen worktree configuration is not selected.'}
 # Only key names are read, includes are disabled, and config bytes are held.
 # Never write or serialize remote/auth configuration values.
 $keys=@(& $Platform.git config --no-includes --file (Join-Path $git 'config') --name-only --list)
 if($LASTEXITCODE-ne0){throw 'Bounded local Git configuration parser failed.'}
 foreach($key in $keys){
  if($key-isnot[string]-or$key-cnotmatch'^(core\.(repositoryformatversion|filemode|bare|logallrefupdates|symlinks|ignorecase|autocrlf|safecrlf|quotepath|precomposeunicode|longpaths)|remote\.[^.]+\.(url|fetch|pushurl)|branch\..+\.(remote|merge|rebase))$'){throw 'Local Git configuration has an execution-affecting or unreviewed key.'}
 }
 [pscustomobject]@{lease=$lease;gitDirectory=$git;frozenGitDirectory=$expected;localConfigSha256=($lease.Before|Where-Object Path -CEQ (Join-Path $git 'config')).Sha256;localConfigNames=@($keys);isolatedGlobalSystemConfig=$true;optionalLocks=$false;fsmonitor=$false}
}

function Assert-KmcScopedCompilerPlan($Platform){
 Assert-KmcScopedMembers $Platform.compiler @('path','configurationPath','references','timeoutSeconds') 'explicit CSharp compiler'
 $runtime=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory().TrimEnd('\')
 Assert-KmcScopedString $Platform.compiler.path (Join-Path $runtime 'csc.exe') 'framework compiler'
 Assert-KmcScopedString $Platform.compiler.configurationPath ($Platform.compiler.path+'.config') 'compiler configuration'
 Assert-KmcScopedInteger $Platform.compiler.timeoutSeconds 30 30 'bounded compiler deadline'
 $expected=@('mscorlib.dll','System.dll','System.Core.dll')
 if($Platform.compiler.references-isnot[Array]-or$Platform.compiler.references.Count-ne3){throw 'Compiler reference closure differs.'}
 for($i=0;$i-lt$expected.Count;$i++){Assert-KmcScopedString $Platform.compiler.references[$i] (Join-Path $runtime $expected[$i]) 'compiler reference'}
 foreach($entry in @(@($Platform.compiler.path,'csharp-compiler'),@($Platform.compiler.configurationPath,'platform-config'))){
  if(@($Platform.files|Where-Object{$_.path-ceq$entry[0]-and$_.role-ceq$entry[1]}).Count-ne1){throw 'Compiler executable/configuration pin is absent or ambiguous.'}
 }
 foreach($p in $Platform.compiler.references){if(@($Platform.files|Where-Object{$_.path-ceq$p-and$_.role-ceq'framework'}).Count-ne1){throw 'Compiler reference pin is absent or ambiguous.'}}
}
function Assert-KmcScopedGeneratedCompilerFile([string]$Path){
 # This uses the exact admitted Windows PowerShell filesystem provider. Generated
 # output is trusted as the owned compiler's product until its read lease opens;
 # these checks do not purport to identify every OS process that could write it.
 $p=[IO.Path]::GetFullPath($Path)
 $current=$p
 while($current){
  if([IO.File]::Exists($current)-or[IO.Directory]::Exists($current)){if(([IO.File]::GetAttributes($current)-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Generated compiler file has a reparse ancestor or leaf.'}}
  $parent=[IO.Path]::GetDirectoryName($current);if($parent-eq$current){break};$current=$parent
 }
 $info=Get-Item -LiteralPath $p -Force
 $links=$info.PSObject.Properties['LinkType']
 if($info.PSIsContainer-or$info.Length-le0-or$info.Length-gt4MB-or$null-eq$links-or-not[string]::IsNullOrEmpty([string]$links.Value)){throw 'Generated compiler file is empty, oversized, linked or lacks exact native link classification.'}
}
function Invoke-KmcScopedCompile($Platform,[byte[]]$SourceBytes,[string]$SourceSha256,[string]$OutputDirectory,[string]$AssemblyLeaf,$Owner){
 # A caller owns this record before compiler dispatch. The entire platform byte
 # set and source are already held read-only; only the declared private output
 # files can be created. This does not use CodeDOM, inherited compiler providers,
 # response files, PATH resolution, or the PowerShell Add-Type compiler.
 if($null-eq$Owner){throw 'Compiler process/output custody must be caller-owned.'}
 Assert-KmcScopedCompilerPlan $Platform
 Assert-KmcScopedSha $SourceSha256 'compiled source'
 if($AssemblyLeaf-cnotmatch'^[A-Za-z][A-Za-z0-9_-]{0,60}\.dll$'){throw 'Compiler output leaf is outside the closed shape.'}
 if(-not[IO.Directory]::Exists($OutputDirectory)-or@(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count-ne0){throw 'Compiler output directory must be fresh and empty.'}
 if($null-eq$SourceBytes-or$SourceBytes.Length-eq0-or$SourceBytes.Length-gt1MB){throw 'Compiler source byte bound differs.'}
 $sourceBytes=$SourceBytes
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$actual=[BitConverter]::ToString($algorithm.ComputeHash($sourceBytes)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
 if($actual-cne$SourceSha256){throw 'Compiler source differs from its exact held bytes.'}
 $source=Join-Path $OutputDirectory 'owned-source.cs';$output=Join-Path $OutputDirectory $AssemblyLeaf
 $Owner.source=$source;$Owner.output=$output
 # Source writer ownership precedes every fallible write. The bridge is acquired
 # before closing it, so no interval permits deleting/replacing the file. Actual
 # Framework csc refuses a still-open writer (CS1504), so acquire its deny-write
 # read lease afterward and hash those held bytes before compiler dispatch.
 $Owner.sourceWriter=[IO.FileStream]::new($source,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 $Owner.sourceWriter.Write($sourceBytes,0,$sourceBytes.Length);$Owner.sourceWriter.Flush()
 $Owner.sourceBridge=[IO.FileStream]::new($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
 $Owner.sourceWriter.Dispose();$Owner.sourceWriter=$null
 $Owner.sourceHandle=[IO.FileStream]::new($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$copiedSha=[BitConverter]::ToString($algorithm.ComputeHash($Owner.sourceHandle)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
 if($Owner.sourceHandle.Length-ne$sourceBytes.Length-or$copiedSha-cne$SourceSha256){throw 'Owned compiler source copy differs before consumption.'}
 $Owner.sourceHandle.Position=0
 Assert-KmcScopedGeneratedCompilerFile $source
 if(Test-Path -LiteralPath $output){throw 'Compiler target already exists.'}
 $arguments=@('/nologo','/noconfig','/nostdlib+','/target:library','/platform:x64','/optimize+','/warnaserror+',('/out:"'+$output+'"'))
 foreach($reference in $Platform.compiler.references){$arguments+=('/reference:"'+$reference+'"')}
 $arguments+=('"'+$source+'"')
 $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=$Platform.compiler.path;$start.Arguments=[string]::Join(' ',$arguments);$start.WorkingDirectory=$OutputDirectory;$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
 $process=[Diagnostics.Process]::new();$process.StartInfo=$start;$Owner.process=$process
 if(-not$process.Start()){throw 'Exact compiler did not start.'}
 $Owner.processStarted=$true;$Owner.processId=$process.Id
 $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
 if(-not$process.WaitForExit($Platform.compiler.timeoutSeconds*1000)){
  # Only this exact owned child may be stopped. Failure to stop it retains
  # process/output custody and prevents qualification or dependency release.
  try{$process.Kill();$process.WaitForExit(5000)|Out-Null}catch{throw 'Owned compiler timed out and remains unsettled.'}
  throw 'Owned compiler exceeded its bounded deadline.'
 }
 $Owner.completed=$true;$Owner.exitCode=$process.ExitCode;$Owner.stdout=$stdout.Result;$Owner.stderr=$stderr.Result;$Owner.arguments=$arguments
 if($process.ExitCode-ne0){throw ('Exact private compiler failed: '+$Owner.stdout+' '+$Owner.stderr)}
 if(-not[IO.File]::Exists($output)){throw 'Exact compiler produced no assembly.'}
 $entries=@(Get-ChildItem -LiteralPath $OutputDirectory -Force)
 if($entries.Count-ne2-or@($entries|Where-Object{$_.Name-cnotin@('owned-source.cs',$AssemblyLeaf)}).Count-ne0){throw 'Compiler created an undeclared output.'}
 Assert-KmcScopedGeneratedCompilerFile $output
 $Owner.outputHandle=[IO.FileStream]::new($output,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 Assert-KmcScopedGeneratedCompilerFile $output
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$Owner.outputSha256=[BitConverter]::ToString($algorithm.ComputeHash($Owner.outputHandle)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
 $Owner.outputHandle.Position=0
 $Owner.outputProvenance='Exact owned compiler and exclusive private scratch are trusted until read-lease acquisition; no OS per-writer witness.'
 $Owner.assembly=[Reflection.Assembly]::LoadFrom($output)
 $Owner.assembly
}
function Close-KmcScopedCompiler($Owner){
 if($null-eq$Owner-or$Owner.released){return}
 if($null-ne$Owner.process-and$Owner.processStarted-and-not$Owner.process.HasExited){throw 'Owned compiler process remains live; retain its inputs and outputs.'}
 $failures=[Collections.Generic.List[Exception]]::new()
 foreach($name in @('sourceWriter','sourceBridge','sourceHandle','outputHandle','process')){if($null-ne$Owner.$name){try{$Owner.$name.Dispose();$Owner.$name=$null}catch{$failures.Add($_.Exception)}}}
 if($failures.Count-ne0){throw [AggregateException]::new('Compiler custody release failed.',$failures)}
 $Owner.released=$true
}
function New-KmcScopedCompilerOwner($Owners){
 $owner=[pscustomobject]@{source=$null;output=$null;sourceWriter=$null;sourceBridge=$null;sourceHandle=$null;outputHandle=$null;process=$null;processStarted=$false;processId=0;completed=$false;exitCode=$null;stdout=$null;stderr=$null;arguments=@();assembly=$null;outputSha256=$null;outputProvenance=$null;released=$false}
 [void]$Owners.Add($owner);$owner
}

function New-KmcScopedOutputOwner([string]$Root,$Owners){
 if($null-eq$Owners){throw 'An external output owner is required.'}
 $state=[pscustomobject]@{root=$Root;tokenPath=(Join-Path $Root 'owner.token');tokenHandle=$null;tokenSha256=$null;tokenBytes=0;lease=$null;receiptHandle=$null;created=$false;published=$false;publicationStarted=$false;released=$false}
 [void]$Owners.Add($state)
 if(Test-Path -LiteralPath $Root){throw 'Scoped output already exists; preserve it.'}
 [void][IO.Directory]::CreateDirectory($Root);$state.created=$true
 $writer=$null
 try{
  $writer=[IO.FileStream]::new($state.tokenPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
  $bytes=[Text.Encoding]::ASCII.GetBytes([Guid]::NewGuid().ToString('N'));$writer.Write($bytes,0,$bytes.Length);$writer.Flush()
  # This second no-delete handle is acquired before the writer releases. The
  # later native identity lease narrows sharing to read-only before consumers.
  $state.tokenHandle=[IO.FileStream]::new($state.tokenPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
  $algorithm=[Security.Cryptography.SHA256]::Create()
  try{$state.tokenSha256=[BitConverter]::ToString($algorithm.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose()}
  $state.tokenBytes=$bytes.Length
 }finally{if($null-ne$writer){$writer.Dispose()}}
 if(@(Get-ChildItem -LiteralPath $Root -Force).Count-ne1){throw 'Scoped output was planted during admission.'}
 $state
}
function Seal-KmcScopedOutputOwner($State){
 if($State.released-or-not$State.created-or$null-ne$State.lease-or$null-ne$State.receiptHandle){throw 'Output custody is not eligible for initial sealing.'}
 $State.lease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(1,4096,30000)
 $State.lease.AddFile($State.tokenPath);$State.lease.Seal();$State.lease.RequirePinnedFile($State.tokenPath,$State.tokenBytes,$State.tokenSha256)
 # CreateNew is deliberate. A preplanted receipt is never overwritten, including
 # failures before source or original guard admission. The exact file handle is
 # retained through publication; no late pathname WriteAllText is used.
 $State.receiptHandle=[IO.FileStream]::new((Join-Path $State.root 'scoped-preflight.json'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
}
function Publish-KmcScopedOutput($State,$Receipt){
 if($State.released-or$State.publicationStarted-or$null-eq$State.lease-or$null-eq$State.receiptHandle){throw 'Output is not owned for first publication.'}
 [void]$State.lease.VerifyUnchanged()
 $Receipt['outputOwnerSha256']=$State.tokenSha256
 $Receipt['completionRequiresSuccessfulCallerExit']=$true
 $bytes=[Text.UTF8Encoding]::new($false).GetBytes(($Receipt|ConvertTo-Json -Depth 30))
 $State.publicationStarted=$true
 $State.receiptHandle.Write($bytes,0,$bytes.Length);$State.receiptHandle.Flush();$State.published=$true
}
function Close-KmcScopedOutput($State){
 if($null-eq$State-or$State.released){return}
 $failures=[Collections.Generic.List[Exception]]::new()
 foreach($name in @('receiptHandle','tokenHandle','lease')){
  if($null-ne$State.$name){try{$State.$name.Dispose();$State.$name=$null}catch{$failures.Add($_.Exception)}}
 }
 if($failures.Count-ne0){throw [AggregateException]::new('Output publication custody remains unfinished.',$failures)}
 $State.released=$true
}
