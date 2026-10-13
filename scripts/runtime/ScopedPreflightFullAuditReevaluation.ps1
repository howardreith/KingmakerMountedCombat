# Separate Policy 5 reread of one known supervisor's formatting-only refusal.
# This never changes a raw record or the existing successful-proof reader.
Set-StrictMode -Version Latest
function Get-KmcMeasuredWrappedPurityAssertion([string]$Text){
 if($null-eq$Text){throw 'A raw decoded log is required.'}
 $prefix='Runtime WhatIf purity PASS; exact fixture descriptors were validated when required, and no evidence, lock, '
 $suffix='transaction, Mods, process, game, or save mutation occurred.'
 $logical=$prefix+$suffix;$physical=$prefix+[string][char]13+[char]10+$suffix+[char]13+[char]10
 # The actual recorded representation is two contiguous CRLF lines at EOF. No
 # whitespace normalization, unrelated-line joining or assertion substring pass.
 if([regex]::Matches($Text,[regex]::Escape('Runtime WhatIf purity')).Count-ne1-or
    [regex]::Matches($Text,('(?m)^'+[regex]::Escape($logical)+'\r?$')).Count-ne0-or
    -not$Text.EndsWith($physical,[StringComparison]::Ordinal)){
  throw 'Exactly one measured two-line terminal purity assertion is required.'
 }
 $offset=$Text.Length-$physical.Length
 if($offset-ne0-and($offset-lt2-or$Text.Substring($offset-2,2)-cne([string][char]13+[char]10))){throw 'Wrapped assertion does not begin at a physical-line boundary.'}
 $first=[regex]::Matches($Text.Substring(0,$offset),'\r\n').Count+1
 $sha=[Security.Cryptography.SHA256]::Create();try{$hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($logical))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 [pscustomobject]@{kind='exact-measured-WindowsPS-information-wrap';originalPhysicalLineCount=0;logicalAssertionCount=1;firstPhysicalLine=$first;lastPhysicalLine=$first+1;characterOffset=$offset;characterLength=$physical.Length;logicalTextSha256=$hash;fold='Remove only the one measured CRLF after the retained space following lock,; every other character is exact.'}
}
function Get-KmcFullAuditFormattingReevaluation([string]$ContractPath,[string]$ContractSha256,[string]$LabRoot,$Owned,[switch]$IncludeDocuments){
 if($null-eq$Owned){throw 'Reread input handles must be caller-owned.'}
 $lab=Get-KmcScopedPath $LabRoot;$analysis=Join-Path $lab 'analysis-cache'
 $contractPath=Assert-KmcScopedChild $ContractPath $analysis 'reread contract'
 $contract=Read-KmcScopedPinnedJson ([pscustomobject]@{path=$contractPath;sha256=$ContractSha256;bytes=[long](Get-Item -LiteralPath $contractPath).Length}) $Owned $analysis
 Assert-KmcScopedMembers $contract @('schemaVersion','kind','failedAudit','supervisor','launcher','common','inventoryHasher','validationPolicy','expectedExecutionSha256','expectedSource','expectedHarness') 'reader-only reevaluation contract'
 Assert-KmcScopedInteger $contract.schemaVersion 1 1 'reread schema';Assert-KmcScopedString $contract.kind 'preview5-measured-purity-wrap-reevaluation' 'reread kind'
 Assert-KmcScopedString $contract.expectedExecutionSha256 'ad9ae664b97a049f9590180d621ccf9e01ea4f30267e61b3aa82b6807d17db2d' 'selected audited execution'
 Assert-KmcScopedString $contract.expectedSource 'd506ce4185e0116d5f365317f0775bc28c83b768' 'selected product source'
 Assert-KmcScopedString $contract.expectedHarness '9c9cf6723385f3fac2d34a02a468a18793c13f42' 'selected historical harness'
 $known=@{supervisor='20983c2c3684e5cbf9d8b33ffb4ef9a77cfa8fa4eaad65ff30b65ef617aff033';launcher='74a49a2113f0de99f32cb998a4e367ce9431b8fa3e381c66e2d2b93258609fb0';common='fe877f3b571206d14aa389a2697e8821ef8193c31fb7e15a96e8cb4453233049';inventoryHasher='ae0e8c561c2753318497317f3c2415ae2d655917847146dca650cc4788af5c3f';validationPolicy='c3c43609dcd49268ca93681a20f516cd42c8c71dcd13bf4c072cb3397c348351'}
 foreach($name in $known.Keys){Assert-KmcScopedString $contract.$name.sha256 $known[$name] ('known source '+$name);[void](Read-KmcScopedPinnedBytes $contract.$name $Owned $lab 4MB)}
 $failed=Read-KmcScopedPinnedJson $contract.failedAudit $Owned $analysis
 Assert-KmcScopedMembers $failed @('schemaVersion','status','nativeAdmission','originalSeconds','actualCallerExit','pins') 'original failed-audit binding'
 Assert-KmcScopedInteger $failed.schemaVersion 1 1 'failed binding schema';Assert-KmcScopedString $failed.status 'FAIL' 'original binding status';Assert-KmcScopedBoolean $failed.nativeAdmission $false 'original native admission';Assert-KmcScopedInteger $failed.actualCallerExit 1 1 'original binding caller exit'
 Assert-KmcScopedMembers $failed.pins @('execution','observation','process','callerExit','beforeInputs','afterInputs','log') 'failed raw bindings'
 $binding=$failed.pins;$documents=@{};$unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($name in @('execution','observation','process','callerExit','beforeInputs','afterInputs','log')){
  if(-not$unique.Add((Get-KmcScopedPath $binding.$name.path))){throw 'Original raw record paths alias.'}
  if($name-ceq'log'){$raw=Read-KmcScopedPinnedBytes $binding.$name $Owned $analysis 64MB;$documents[$name]=ConvertFrom-KmcScopedLogBytes $raw}
  else{$documents[$name]=Read-KmcScopedPinnedJson $binding.$name $Owned $analysis}
 }
 Assert-KmcScopedString $binding.execution.sha256 $contract.expectedExecutionSha256 'reread exact execution'
 # The following orthogonal checks retain the existing c249579d proof reader's
 # exact identities, input equality, argv/cwd and chronology. Only the original
 # failure verdict/count/exit expectations and measured formatting rule differ.
 $ex=$documents.execution;$ob=$documents.observation;$pr=$documents.process;$exit=$documents.callerExit
 Assert-KmcScopedInteger $ex.schemaVersion 1 1 'execution schema';Assert-KmcScopedString $ex.kind 'preview5-fresh-full-whatif-execution' 'execution kind'
 foreach($key in @('source','sourceTree','harness','harnessTree')){if($ex.$key-isnot[string]-or$ex.$key-cnotmatch'^[0-9a-f]{40}$'){throw 'Full execution lacks exact commit/tree identity.'}}
 Assert-KmcScopedString $ex.repositoryRoot (Join-Path (Get-KmcScopedPath $LabRoot) 'repo\KingmakerMountedCombat') 'execution repository'
 if($ex.auditRoots-isnot[Array]-or$ex.auditRoots.Count-ne5){throw 'Full execution lacks exactly five audit roots.'}
 $roots=@((Join-Path $LabRoot 'runtime-state'),(Join-Path $LabRoot 'runtime-backups'),(Join-Path $LabRoot 'runtime-staging'),(Join-Path $LabRoot 'runtime-evidence'))
 for($i=0;$i-lt4;$i++){Assert-KmcScopedString $ex.auditRoots[$i] (Get-KmcScopedPath $roots[$i]) 'historical root identity'}
 foreach($rootPath in @($ex.auditRoots[4],$ex.saveRoot,$ex.frozenSourceRoot,$ex.packageValidationScratchRoot)){if($rootPath-isnot[string]){throw 'Full execution root is not a scalar path.'};[void](Get-KmcScopedPath $rootPath)}
 Assert-KmcScopedInteger $ob.schemaVersion 1 1 'observation schema';Assert-KmcScopedString $ob.kind 'full-whatif-supervisor-observation' 'observation kind';Assert-KmcScopedString $ob.phase 'full-whatif' 'observation phase';Assert-KmcScopedString $ob.status 'FAIL' 'original observation status'
 foreach($key in @('source','harness')){Assert-KmcScopedString $ob.$key $ex.$key ('observation '+$key)}
 Assert-KmcScopedString $ob.executionSha256 $binding.execution.sha256 'observation execution';Assert-KmcScopedString $ob.argumentsSha256 $ex.arguments.sha256 'observation arguments'
 foreach($key in @('actualProcessExitRequiresCallerReceipt','beforePinsVerified','afterPinsVerified')){Assert-KmcScopedBoolean $ob.$key $true ('observation '+$key)}
 foreach($key in @('nativeExecution','perRootDigestsCaptured')){Assert-KmcScopedBoolean $ob.$key $false ('observation '+$key)}
 Assert-KmcScopedBoolean $ob.purityLinePresent $false 'original physical purity verdict'
 Assert-KmcScopedInteger $ob.purityLineCount 0 0 'original physical purity count';Assert-KmcScopedString $ob.assertionKind 'launcher-code-mediated-per-root-before-after-equality' 'observation assertion'
 if($null-ne$ob.error){throw 'Full observation contains an error.'}
 if(($ob.auditRoots|ConvertTo-Json -Compress)-cne($ex.auditRoots|ConvertTo-Json -Compress)){throw 'Full observation root identity differs.'}
 Assert-KmcScopedString $ob.log $binding.log.path 'full log path';Assert-KmcScopedString $ob.logSha256 $binding.log.sha256 'full log hash'
 $formatting=Get-KmcMeasuredWrappedPurityAssertion $documents.log
 foreach($phase in @('before','after')){
  $name=$phase+'Inputs';$inventory=$documents[$name];Assert-KmcScopedInventory $inventory $phase $ex
  Assert-KmcScopedString $ob.$name.path $binding.$name.path 'inventory path';Assert-KmcScopedString $ob.$name.sha256 $binding.$name.sha256 'inventory hash'
  Assert-KmcScopedInteger $ob.$name.trackedCount $inventory.tracked.Count $inventory.tracked.Count 'tracked count'
  Assert-KmcScopedInteger $ob.$name.additionalCount $inventory.additional.Count $inventory.additional.Count 'additional count'
 }
 foreach($name in @('tracked','additional')){if(($documents.beforeInputs.$name|ConvertTo-Json -Depth 8 -Compress)-cne($documents.afterInputs.$name|ConvertTo-Json -Depth 8 -Compress)){throw 'Actual full-audit input inventories differ.'}}
 Assert-KmcScopedInteger $pr.schemaVersion 1 1 'process schema';Assert-KmcScopedInteger $pr.processId 1 2147483647 'audit process id'
 Assert-KmcScopedString $pr.executionSha256 $binding.execution.sha256 'process execution';Assert-KmcScopedString $pr.argumentsSha256 $ex.arguments.sha256 'process arguments'
 if(($pr.executor|ConvertTo-Json -Depth 8 -Compress)-cne($ob.executor|ConvertTo-Json -Depth 8 -Compress)){throw 'Actual executor receipts differ.'}
 Assert-KmcScopedExecutor $ex $pr.executor $binding.execution.sha256
 Assert-KmcScopedInteger $exit.schemaVersion 1 1 'caller exit schema';Assert-KmcScopedString $exit.kind 'full-whatif-caller-exit' 'caller exit kind';Assert-KmcScopedString $exit.attestation 'caller-observed-tool-exit' 'caller exit attestation'
 Assert-KmcScopedString $exit.executionSha256 $binding.execution.sha256 'caller execution';Assert-KmcScopedInteger $exit.processId $pr.processId $pr.processId 'caller process'
 Assert-KmcScopedString $exit.startedAtUtc $pr.startedAtUtc 'caller process start';Assert-KmcScopedString $exit.supervisorReceiptSha256 $binding.observation.sha256 'caller supervisor'
 Assert-KmcScopedInteger $exit.exitCode 1 1 'original actual caller exit';Assert-KmcScopedBoolean $exit.completed $true 'actual caller completion'
 foreach($key in @('toolSessionId','toolChunkId')){if(($exit.$key-isnot[string]-and$exit.$key-isnot[int]-and$exit.$key-isnot[long])-or[string]::IsNullOrWhiteSpace([string]$exit.$key)){throw 'Caller tool observation identity is missing.'}}
 $stamps=@($pr.startedAtUtc,$ob.startedUtc,$ob.endedUtc,$exit.observedCompletionUtc)|ForEach-Object{$stamp=[DateTimeOffset]::MinValue;if($_-isnot[string]-or-not[DateTimeOffset]::TryParseExact($_,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$stamp)){throw 'Full proof timestamps are invalid.'};$stamp}
 if($stamps[0]-ne$stamps[1]-or$stamps[2]-lt$stamps[1]-or$stamps[3]-lt$stamps[2]){throw 'Full proof observation ordering differs.'}
 Assert-KmcScopedString $ex.source $contract.expectedSource 'reread source';Assert-KmcScopedString $ex.harness $contract.expectedHarness 'reread harness'
 foreach($name in @('path','sha256','bytes')){if($name-ceq'bytes'){Assert-KmcScopedInteger $ex.supervisor.$name $contract.supervisor.$name $contract.supervisor.$name 'known supervisor length'}else{Assert-KmcScopedString $ex.supervisor.$name $contract.supervisor.$name 'known supervisor pin'}}
 foreach($name in @('launcher','common','inventoryHasher','validationPolicy')){
  $pin=$contract.$name
  foreach($phase in @('beforeInputs','afterInputs')){
   $found=@($documents[$phase].tracked|Where-Object{$_.path-ceq$pin.path})
   if($found.Count-ne1){throw ('Known control source is absent from captured inputs: '+$name)}
   Assert-KmcScopedString $found[0].sha256 $pin.sha256 'captured known control bytes';Assert-KmcScopedInteger $found[0].bytes $pin.bytes $pin.bytes 'captured known control size'
  }
 }
 $expectedPins=$ex.harnessFiles.Count+$ex.additionalDependencies.Count+7
 Assert-KmcScopedInteger $ob.inputPinCount $expectedPins $expectedPins 'original completed pin count'
 foreach($number in @($failed.originalSeconds,$ob.seconds)){
  if(($number-isnot[int]-and$number-isnot[long]-and$number-isnot[double]-and$number-isnot[decimal])-or[double]::IsNaN([double]$number)-or[double]::IsInfinity([double]$number)-or$number-lt0){throw 'Original duration is not a finite nonnegative numeric value.'}
 }
 if([Math]::Abs([double]$failed.originalSeconds-[double]$ob.seconds)-gt0.000001-or[Math]::Abs(($stamps[2]-$stamps[1]).TotalSeconds-[double]$ob.seconds)-gt0.000001){throw 'Original observed duration differs.'}
 foreach($lease in $Owned){[void]$lease.VerifyUnchanged()}
 $evaluation=[pscustomobject]@{schemaVersion=1;kind='full-whatif-reader-only-reevaluation';status='PASS';contractPath=$contractPath;contractSha256=$ContractSha256;failedAudit=$contract.failedAudit;rawPins=$binding;source=$ex.source;harness=$ex.harness;executionSha256=$binding.execution.sha256;originalSupervisorStatus=$ob.status;originalCallerExitCode=$exit.exitCode;originalPhysicalLineCount=$ob.purityLineCount;originalError=$ob.error;originalSeconds=$ob.seconds;beforePinsVerified=$ob.beforePinsVerified;afterPinsVerified=$ob.afterPinsVerified;inputPinCount=$ob.inputPinCount;trackedCount=$documents.beforeInputs.tracked.Count;additionalCount=$documents.beforeInputs.additional.Count;reevaluatedAssertion=$formatting;assertionKind=$ob.assertionKind;perRootDigestsCaptured=$false;nativeAdmission=$false;nativeExecution=$false;freshAuditExecuted=$false;sourcePins=[pscustomobject]@{supervisor=$contract.supervisor;launcher=$contract.launcher;common=$contract.common;inventoryHasher=$contract.inventoryHasher;validationPolicy=$contract.validationPolicy};reason='Only the exact known supervisor physical-line predicate misclassified its measured two-line information record. Original records and actual exit1 are retained.';completionRequiresSeparateRereadCallerExit=$true}
 if($IncludeDocuments){return [pscustomobject]@{evaluation=$evaluation;binding=$binding;documents=$documents}}
 $evaluation
}

function Read-KmcReevaluationBootstrapPin($Pin,$Owned,[string]$LabRoot){
 if($null-eq$Owned-or$null-eq$Pin-or$Pin.path-isnot[string]-or$Pin.sha256-isnot[string]-or$Pin.sha256-cnotmatch'^[0-9a-f]{64}$'-or($Pin.bytes-isnot[int]-and$Pin.bytes-isnot[long])-or$Pin.bytes-lt1-or$Pin.bytes-gt4MB){throw 'An exact bounded reread code pin and external owner are required.'}
 $path=[IO.Path]::GetFullPath($Pin.path);$analysis=[IO.Path]::GetFullPath((Join-Path $LabRoot 'analysis-cache')).TrimEnd('\')
 if(-not$path.StartsWith($analysis+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Reread bootstrap code escaped analysis-cache.'}
 for($current=$path;$null-ne$current;$current=[IO.Path]::GetDirectoryName($current)){if(([IO.File]::GetAttributes($current)-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Reread bootstrap code has a reparse ancestor.'};if($current-ceq[IO.Path]::GetPathRoot($current)){break}}
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);[void]$Owned.Add($stream)
 if($stream.Length-ne$Pin.bytes){throw 'Reread bootstrap code length differs.'}
 $bytes=New-Object byte[] $stream.Length;$offset=0;while($offset-lt$bytes.Length){$read=$stream.Read($bytes,$offset,$bytes.Length-$offset);if($read-le0){throw 'Reread bootstrap code is truncated.'};$offset+=$read}
 $sha=[Security.Cryptography.SHA256]::Create();try{$actual=[BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 if($actual-cne$Pin.sha256){throw 'Reread bootstrap code hash differs.'}
 ,$bytes
}
function Import-KmcFullAuditReevaluationRuntime($Plan,$BootstrapOwned,[string]$PlanJson){
 # The caller holds the exact plan and this reader before calling. These two
 # established source dependencies are deliberately not configurable code roles.
 if($Plan.proofHelper.sha256-cne'c249579db1ce8a912840be95dfa8d3cb173ee74eeec25aa69c00b0cb5501729e'-or$Plan.observationSource.sha256-cne'99bab1811c817bd15a13d78a06f004fbbdc9548f125144ff3c5bfc32d842d2e9'){throw 'Reread established dependency differs.'}
 $proofBytes=Read-KmcReevaluationBootstrapPin $Plan.proofHelper $BootstrapOwned $Plan.labRoot
 $proofFunctions=[scriptblock]::Create([Text.UTF8Encoding]::new($false,$true).GetString($proofBytes));. $proofFunctions
 if([string]::IsNullOrEmpty($PlanJson)){throw 'Exact held plan JSON is required before assembly admission.'}
 $strictPlan=ConvertFrom-KmcScopedJson $PlanJson 'reread bootstrap plan'
 if(($strictPlan|ConvertTo-Json -Depth 20 -Compress)-cne($Plan|ConvertTo-Json -Depth 20 -Compress)){throw 'Parsed reread plan differs from its exact JSON.'}
 $Plan=$strictPlan;Assert-KmcReevaluationPlan $Plan
 [void](Read-KmcReevaluationBootstrapPin $Plan.observationSource $BootstrapOwned $Plan.labRoot)
 $assemblyBytes=Read-KmcReevaluationBootstrapPin $Plan.observationAssembly $BootstrapOwned $Plan.labRoot
 if($null-ne('KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation'-as[type])){throw 'Reread lease type was loaded before its exact assembly admission.'}
 $assembly=[Reflection.Assembly]::Load($assemblyBytes)
 if($null-eq$assembly.GetType('KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation',$false)){throw 'Pinned reread assembly lacks the reviewed lease type.'}
 [pscustomobject]@{proofFunctions=$proofFunctions;assemblyMvid=$assembly.ManifestModule.ModuleVersionId.ToString();assemblyLoadedFromPinnedBytes=$true}
}
function Assert-KmcReevaluationPlan($Plan){
 Assert-KmcScopedMembers $Plan @('schemaVersion','kind','labRoot','contract','wrapper','reader','proofHelper','observationSource','observationAssembly','executor','outputRoot') 'reread execution plan'
 Assert-KmcScopedInteger $Plan.schemaVersion 1 1 'reread plan schema';Assert-KmcScopedString $Plan.kind 'full-whatif-formatting-reevaluation-execution' 'reread plan kind'
 foreach($name in @('contract','wrapper','reader','proofHelper','observationSource','observationAssembly')){Assert-KmcScopedMembers $Plan.$name @('path','sha256','bytes') ('reread plan '+$name);Assert-KmcScopedSha $Plan.$name.sha256 ('reread plan '+$name);Assert-KmcScopedInteger $Plan.$name.bytes 1 4MB ('reread plan '+$name);if($Plan.$name.path-isnot[string]){throw 'Reread pin path is not a string.'};[void](Assert-KmcScopedChild $Plan.$name.path (Join-Path (Get-KmcScopedPath $Plan.labRoot) 'analysis-cache') 'reread plan input')}
 [void](Assert-KmcScopedChild $Plan.outputRoot (Join-Path (Get-KmcScopedPath $Plan.labRoot) 'analysis-cache') 'reread output')
 Assert-KmcScopedMembers $Plan.executor @('path','sha256','workingDirectory','arguments') 'reread executor plan'
 Assert-KmcScopedSha $Plan.executor.sha256 'reread executor pin';[void](Get-KmcScopedPath $Plan.executor.path);[void](Assert-KmcScopedChild $Plan.executor.workingDirectory (Join-Path $Plan.labRoot 'analysis-cache') 'reread working directory')
 $fixed=@('-NoProfile','-NonInteractive','-File',$Plan.wrapper.path,'-PlanSha256','<this-file-actual-sha256>')
 if($Plan.executor.arguments-isnot[Array]-or$Plan.executor.arguments.Count-ne$fixed.Count){throw 'Reread executor template length differs.'}
 for($i=0;$i-lt$fixed.Count;$i++){Assert-KmcScopedString $Plan.executor.arguments[$i] $fixed[$i] ('reread planned argument '+$i)}
}
function Assert-KmcReevaluationExecutor($Plan,$Actual,[string]$PlanSha256){
 Assert-KmcScopedSha $PlanSha256 'reread plan hash'
 Assert-KmcScopedString $Actual.path $Plan.executor.path 'reread executable';Assert-KmcScopedString $Actual.sha256 $Plan.executor.sha256 'reread executable bytes'
 Assert-KmcScopedString $Actual.workingDirectory $Plan.executor.workingDirectory 'reread current directory';Assert-KmcScopedString $Actual.powerShellLocation $Plan.executor.workingDirectory 'reread PowerShell location'
 $expected=@($Plan.executor.path)+@($Plan.executor.arguments[0..4])+@($PlanSha256)
 if($Actual.rawArguments-isnot[Array]-or$Actual.rawArguments.Count-ne$expected.Count){throw 'Reread actual argument vector length differs.'}
 for($i=0;$i-lt$expected.Count;$i++){Assert-KmcScopedString $Actual.rawArguments[$i] $expected[$i] ('reread actual argument '+$i)}
 foreach($name in @('powerShellVersion','clrVersion')){if($Actual.$name-isnot[string]-or[string]::IsNullOrEmpty($Actual.$name)){throw 'Reread actual platform version is missing.'}}
}
function Get-KmcReevaluatedFullAuditProof([string]$Path,[string]$ExpectedSha256,[string]$LabRoot,$Owned){
 $analysis=Join-Path (Get-KmcScopedPath $LabRoot) 'analysis-cache';$path=Assert-KmcScopedChild $Path $analysis 'corrected proof'
 $proof=Read-KmcScopedPinnedJson ([pscustomobject]@{path=$path;sha256=$ExpectedSha256;bytes=[long](Get-Item -LiteralPath $path).Length}) $Owned $analysis
 Assert-KmcScopedMembers $proof @('schemaVersion','kind','status','nativeAdmission','freshAuditExecuted','pins') 'corrected proof envelope'
 Assert-KmcScopedInteger $proof.schemaVersion 1 1 'corrected proof schema';Assert-KmcScopedString $proof.kind 'full-whatif-formatting-reevaluation-proof' 'corrected proof kind';Assert-KmcScopedString $proof.status 'PASS' 'corrected proof status';Assert-KmcScopedBoolean $proof.nativeAdmission $false 'corrected native admission';Assert-KmcScopedBoolean $proof.freshAuditExecuted $false 'corrected fresh audit'
 Assert-KmcScopedMembers $proof.pins @('plan','result','process','callerExit') 'corrected proof records'
 $records=@{};$unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($name in @('plan','result','process','callerExit')){if(-not$unique.Add((Get-KmcScopedPath $proof.pins.$name.path))){throw 'Reread record paths alias.'};$records[$name]=Read-KmcScopedPinnedJson $proof.pins.$name $Owned $analysis}
 $plan=$records.plan;Assert-KmcReevaluationPlan $plan;Assert-KmcScopedString $plan.labRoot (Get-KmcScopedPath $LabRoot) 'reread lab root'
 foreach($name in @('contract','wrapper','reader','proofHelper','observationSource','observationAssembly')){[void](Read-KmcScopedPinnedBytes $plan.$name $Owned $analysis 4MB)}
 $r=$records.result;$p=$records.process;$c=$records.callerExit
 Assert-KmcScopedInteger $r.schemaVersion 1 1 'reread result schema';Assert-KmcScopedString $r.kind 'full-whatif-reevaluation-supervisor-observation' 'reread result kind';Assert-KmcScopedString $r.status 'PASS' 'reread result status'
 Assert-KmcScopedString $r.planSha256 $proof.pins.plan.sha256 'reread result plan';Assert-KmcScopedString $r.process.path $proof.pins.process.path 'reread process path';Assert-KmcScopedString $r.process.sha256 $proof.pins.process.sha256 'reread process hash';Assert-KmcScopedInteger $r.process.bytes $proof.pins.process.bytes $proof.pins.process.bytes 'reread process bytes'
 foreach($name in @('actualProcessExitRequiresCallerReceipt','inputsVerified','cleanupComplete')){Assert-KmcScopedBoolean $r.$name $true ('reread '+$name)}
 foreach($name in @('nativeAdmission','nativeExecution','freshAuditExecuted')){Assert-KmcScopedBoolean $r.$name $false ('reread '+$name)}
 Assert-KmcScopedString $r.cleanupScope 'input-and-bootstrap-leases; final output release requires separate actual caller exit0' 'reread cleanup claim'
 Assert-KmcScopedString $r.originalSupervisorStatus 'FAIL' 'retained original status';Assert-KmcScopedInteger $r.originalCallerExitCode 1 1 'retained original exit';if($null-ne$r.error){throw 'Reread result contains an error.'}
 Assert-KmcScopedInteger $p.schemaVersion 1 1 'reread process schema';Assert-KmcScopedString $p.kind 'full-whatif-reevaluation-process' 'reread process kind';Assert-KmcScopedString $p.planSha256 $proof.pins.plan.sha256 'reread process plan';Assert-KmcScopedInteger $p.processId 1 2147483647 'reread process ID';Assert-KmcReevaluationExecutor $plan $p.executor $proof.pins.plan.sha256
 Assert-KmcScopedString $c.kind 'full-whatif-reevaluation-caller-exit' 'reread caller kind';Assert-KmcScopedInteger $c.schemaVersion 1 1 'reread caller schema';Assert-KmcScopedString $c.attestation 'caller-observed-tool-exit' 'reread caller attestation'
 Assert-KmcScopedString $c.planSha256 $proof.pins.plan.sha256 'reread caller plan';Assert-KmcScopedInteger $c.processId $p.processId $p.processId 'reread caller process';Assert-KmcScopedString $c.startedAtUtc $p.startedAtUtc 'reread caller start';Assert-KmcScopedString $c.resultSha256 $proof.pins.result.sha256 'reread caller result';Assert-KmcScopedInteger $c.exitCode 0 0 'reread actual caller exit';Assert-KmcScopedBoolean $c.completed $true 'reread caller completion'
 foreach($name in @('toolSessionId','toolChunkId')){if(($c.$name-isnot[string]-and$c.$name-isnot[int]-and$c.$name-isnot[long])-or[string]::IsNullOrWhiteSpace([string]$c.$name)){throw 'Reread caller lacks tool observation identity.'}}
 $stampValues=@($p.startedAtUtc,$r.startedUtc,$r.endedUtc,$c.observedCompletionUtc);$stamps=@($stampValues|ForEach-Object{$value=[DateTimeOffset]::MinValue;if($_-isnot[string]-or-not[DateTimeOffset]::TryParseExact($_,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$value)){throw 'Reread chronology contains an invalid timestamp.'};$value})
 if($stamps[0]-ne$stamps[1]-or$stamps[2]-lt$stamps[1]-or$stamps[3]-lt$stamps[2]){throw 'Reread chronology differs.'}
 $original=Get-KmcFullAuditFormattingReevaluation $plan.contract.path $plan.contract.sha256 $LabRoot $Owned -IncludeDocuments
 if(($r.reevaluation|ConvertTo-Json -Depth 30 -Compress)-cne($original.evaluation|ConvertTo-Json -Depth 30 -Compress)){throw 'Reread result differs from fresh recognition of its unchanged raw records.'}
 if($stamps[0]-le[DateTimeOffset]::ParseExact($original.documents.callerExit.observedCompletionUtc,'o',[Globalization.CultureInfo]::InvariantCulture)){throw 'Reread predates the observed original completion.'}
 foreach($lease in $Owned){[void]$lease.VerifyUnchanged()}
 [pscustomobject]@{binding=$original.binding;documents=$original.documents;reevaluation=$original.evaluation;rereadProcess=$p;rereadCallerExit=$c;proof=$proof;plan=$plan;path=$path;sha256=$ExpectedSha256;historicalAssertion=$original.evaluation.assertionKind;rootDigestsAvailable=$false;callerObservationIsOsAttestation=$false;originalFailurePreserved=$true;nativeAdmission=$false;freshAuditExecuted=$false}
}
