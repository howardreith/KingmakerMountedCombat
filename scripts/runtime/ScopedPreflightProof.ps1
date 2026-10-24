# Bootstrap for the separately closed, read-only cold/Manual preflight.
# This file grants no native or transaction authority. All returned observations
# remain owned by the caller until its final comparison and independent release.
Set-StrictMode -Version Latest
function Assert-KmcScopedString($Value,[string]$Expected,[string]$Description){
 if($Value-isnot[string]-or$Value-cne$Expected){throw ($Description+' differs.')}
}
function Assert-KmcScopedInteger($Value,[long]$Min,[long]$Max,[string]$Description){
 if(($Value-isnot[int]-and$Value-isnot[long])-or$Value-lt$Min-or$Value-gt$Max){throw ($Description+' must be an exact bounded JSON integer.')}
}
function Assert-KmcScopedBoolean($Value,[bool]$Expected,[string]$Description){
 if($Value-isnot[bool]-or$Value-ne$Expected){throw ($Description+' must be the exact boolean.')}
}
function Assert-KmcScopedMembers($Value,[string[]]$Names,[string]$Description){
 if($null-eq$Value-or$Value-is[Array]-or$Value-is[string]){throw ($Description+' must be an object.')}
 $actual=@($Value.PSObject.Properties.Name)
 if($actual.Count-ne$Names.Count){throw ($Description+' property set differs.')}
 foreach($name in $Names){if(@($actual|Where-Object{$_-ceq$name}).Count-ne1){throw ($Description+' property set differs: '+$name)}}
}
function Assert-KmcScopedSha($Value,[string]$Description){
 if($Value-isnot[string]-or$Value-cnotmatch'^[0-9a-f]{64}$'){throw ($Description+' is not SHA-256.')}
}
function Get-KmcScopedPath([string]$Path){
 if([string]::IsNullOrEmpty($Path)-or$Path-cnotmatch'^[A-Za-z]:[\\/]'-or$Path.Substring(2).Contains(':')){throw 'A literal local absolute path is required.'}
 [IO.Path]::GetFullPath($Path).TrimEnd('\')
}
function Assert-KmcScopedChild([string]$Path,[string]$Root,[string]$Description){
 $full=Get-KmcScopedPath $Path;$parent=Get-KmcScopedPath $Root
 if(-not$full.StartsWith($parent+'\',[StringComparison]::OrdinalIgnoreCase)){throw ($Description+' escaped its declared root.')}
 $full
}
function ConvertFrom-KmcScopedJson([string]$Json,[string]$Description){
 Add-Type -AssemblyName System.Runtime.Serialization
 $reader=$null
 try{
  $bytes=[Text.Encoding]::UTF8.GetBytes($Json)
  if($bytes.Length-eq0-or$bytes.Length-gt20MB){throw ($Description+' is empty or oversized.')}
  $quotas=[Xml.XmlDictionaryReaderQuotas]::new();$quotas.MaxDepth=128;$quotas.MaxArrayLength=20000000;$quotas.MaxBytesPerRead=65536;$quotas.MaxNameTableCharCount=20000000;$quotas.MaxStringContentLength=20000000
  $reader=[Runtime.Serialization.Json.JsonReaderWriterFactory]::CreateJsonReader($bytes,$quotas)
  $document=[Xml.XmlDocument]::new();$document.Load($reader)
  foreach($object in @($document.SelectNodes('//*[@type="object"]'))){
   $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
   foreach($member in @($object.ChildNodes|Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))){
    $name=if($member.LocalName-ceq'item'-and$null-ne$member.Attributes['item']){[string]$member.Attributes['item'].Value}else{[string]$member.LocalName}
    if(-not$names.Add($name)){throw ($Description+' has duplicate or case-ambiguous JSON members.')}
   }
  }
 }finally{if($null-ne$reader){$reader.Dispose()}}
 $Json|ConvertFrom-Json
}
function New-KmcScopedObservation($Owned,[int]$Entries=32768,[long]$Bytes=8589934592,[int]$Milliseconds=120000){
 if($null-eq$Owned){throw 'An external observation owner is required.'}
 $lease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new($Entries,$Bytes,$Milliseconds)
 # Assignment to the owner precedes every fallible acquisition.
 [void]$Owned.Add($lease)
 $lease
}
function Read-KmcScopedPinnedBytes($Pin,$Owned,[string]$Root,[long]$Maximum=20MB){
 Assert-KmcScopedMembers $Pin @('path','sha256','bytes') 'immutable file pin'
 Assert-KmcScopedSha $Pin.sha256 'immutable file pin'
 Assert-KmcScopedInteger $Pin.bytes 1 $Maximum 'immutable file length'
 if($Pin.path-isnot[string]){throw 'Pinned path must be a string.'}
 $path=Assert-KmcScopedChild $Pin.path $Root 'immutable file'
 $lease=New-KmcScopedObservation $Owned 1 $Maximum
 $lease.AddFile($path);$lease.Seal();$lease.RequirePinnedFile($path,$Pin.bytes,$Pin.sha256)
 # A second read is safe while the exact file object remains held FileShare.Read.
 ,([IO.File]::ReadAllBytes($path))
}
function Read-KmcScopedPinnedJson($Pin,$Owned,[string]$Root){
 $bytes=Read-KmcScopedPinnedBytes $Pin $Owned $Root
 $offset=if($bytes.Length-ge3-and$bytes[0]-eq239-and$bytes[1]-eq187-and$bytes[2]-eq191){3}else{0}
 $json=[Text.UTF8Encoding]::new($false,$true).GetString($bytes,$offset,$bytes.Length-$offset)
 ConvertFrom-KmcScopedJson $json $Pin.path
}
function ConvertFrom-KmcScopedLogBytes([byte[]]$Bytes){
 # Windows PowerShell5.1 redirection is UTF16LE with BOM; explicit callers may
 # emit UTF8. Decode only those two contracts, always from already hashed bytes.
 if($Bytes.Length-ge2-and$Bytes[0]-eq255-and$Bytes[1]-eq254){
  if(($Bytes.Length-2)%2-ne0){throw 'UTF16LE audit log has an odd byte length.'}
  return [Text.UnicodeEncoding]::new($false,$false,$true).GetString($Bytes,2,$Bytes.Length-2)
 }
 if($Bytes.Length-ge2-and$Bytes[0]-eq254-and$Bytes[1]-eq255){throw 'UTF16BE audit logs are outside the selected contract.'}
 $offset=if($Bytes.Length-ge3-and$Bytes[0]-eq239-and$Bytes[1]-eq187-and$Bytes[2]-eq191){3}else{0}
 [Text.UTF8Encoding]::new($false,$true).GetString($Bytes,$offset,$Bytes.Length-$offset)
}
function Assert-KmcScopedInventory($Value,[string]$Phase,$Execution){
 Assert-KmcScopedInteger $Value.schemaVersion 1 1 'audit inventory schema'
 Assert-KmcScopedString $Value.kind 'actual-audit-input-byte-inventory' 'audit inventory kind'
 Assert-KmcScopedString $Value.phase $Phase 'audit inventory phase'
 Assert-KmcScopedString $Value.repositoryRoot $Execution.repositoryRoot 'audit repository'
 Assert-KmcScopedString $Value.head $Execution.harness 'audit harness'
 foreach($list in @('tracked','additional')){
  $rows=$Value.$list
  if($rows-isnot[Array]-or$rows.Count-lt1-or$rows.Count-gt32768){throw 'Audit input inventory must be a bounded nonempty array.'}
  $paths=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase);$prior=$null
  foreach($row in $rows){
   Assert-KmcScopedMembers $row $(if($list-ceq'tracked'){@('relativePath','path','sha256','bytes')}else{@('path','sha256','bytes')}) 'audit input'
   Assert-KmcScopedSha $row.sha256 'audit input';Assert-KmcScopedInteger $row.bytes 0 2147483648 'audit input bytes'
   if($row.path-isnot[string]-or-not$paths.Add((Get-KmcScopedPath $row.path))){throw 'Audit input path is ambiguous.'}
   if($list-ceq'tracked'){
    if($row.relativePath-isnot[string]-or$row.relativePath.Contains('\')-or$row.relativePath-cmatch'(^/|(^|/)\.\.?(/|$)|:)'){throw 'Tracked input has an invalid relative path.'}
    $expected=Assert-KmcScopedChild (Join-Path $Execution.repositoryRoot $row.relativePath) $Execution.repositoryRoot 'tracked audit input'
    Assert-KmcScopedString $row.path $expected 'tracked absolute path'
    if($null-ne$prior-and[StringComparer]::Ordinal.Compare($prior,$row.relativePath)-ge0){throw 'Tracked input path set is not uniquely sorted.'}
    $prior=$row.relativePath
   }
  }
 }
}
function Assert-KmcScopedExecutor($Execution,$Actual,[string]$ExecutionSha256){
 $planned=$Execution.executor
 Assert-KmcScopedSha $ExecutionSha256 'actual execution hash'
 if($planned.path-isnot[string]){throw 'Planned executor path must be a scalar string.'}
 [void](Get-KmcScopedPath $planned.path);Assert-KmcScopedSha $planned.sha256 'planned executor bytes'
 Assert-KmcScopedBoolean $planned.actualProcessArgumentsVersionsAndTempCapturedBySupervisor $true 'executor observation promise'
 Assert-KmcScopedString $planned.workingDirectory $Execution.repositoryRoot 'planned working directory'
 if($Execution.supervisor.path-isnot[string]){throw 'Planned supervisor must be a scalar path.'}
 [void](Get-KmcScopedPath $Execution.supervisor.path)
 # This reader admits only the selected explicit Windows PowerShell invocation.
 # The sole self-hash marker is resolved from the already pinned execution bytes.
 $template=$planned.arguments
 if($template-isnot[Array]-or$template.Count-ne6){throw 'Executor argument template must contain the exact six NoProfile arguments.'}
 foreach($arg in $template){if($arg-isnot[string]-or[string]::IsNullOrEmpty($arg)){throw 'Every executor argument template item must be a nonempty scalar string.'}}
 $fixed=@('-NoProfile','-NonInteractive','-File',$Execution.supervisor.path,'-ExecutionSha256','<this-file-actual-sha256>')
 for($i=0;$i-lt$fixed.Count;$i++){Assert-KmcScopedString $template[$i] $fixed[$i] ('executor argument template '+$i)}
 $expected=@($planned.path)+@($template[0..4])+@($ExecutionSha256)
 Assert-KmcScopedString $Actual.path $planned.path 'executor image';Assert-KmcScopedString $Actual.sha256 $planned.sha256 'executor bytes'
 if($Actual.rawArguments-isnot[Array]-or$Actual.rawArguments.Count-ne$expected.Count){throw 'Actual executor argument vector differs from the exact NoProfile template.'}
 for($i=0;$i-lt$expected.Count;$i++){Assert-KmcScopedString $Actual.rawArguments[$i] $expected[$i] ('actual executor argument '+$i)}
 Assert-KmcScopedString $Actual.workingDirectory $planned.workingDirectory 'actual working directory'
 Assert-KmcScopedString $Actual.powerShellLocation $planned.workingDirectory 'actual PowerShell location'
}
function Get-KmcScopedInheritedProof([string]$Path,[string]$ExpectedSha256,[string]$LabRoot,$Owned){
 Assert-KmcScopedSha $ExpectedSha256 'full proof pin'
 $analysis=Join-Path (Get-KmcScopedPath $LabRoot) 'analysis-cache'
 $path=Assert-KmcScopedChild $Path $analysis 'full proof'
 $info=Get-Item -LiteralPath $path
 $binding=Read-KmcScopedPinnedJson ([pscustomobject]@{path=$path;sha256=$ExpectedSha256;bytes=[long]$info.Length}) $Owned $analysis
 Assert-KmcScopedMembers $binding @('schemaVersion','kind','status','nativeExecution','assertionKind','perRootDigestsCaptured','execution','observation','process','callerExit','beforeInputs','afterInputs','log') 'full proof binding'
 Assert-KmcScopedInteger $binding.schemaVersion 1 1 'full proof schema';Assert-KmcScopedString $binding.kind 'closed-preflight-inherited-full-proof' 'full proof kind'
 Assert-KmcScopedString $binding.status 'PASS' 'full proof status';Assert-KmcScopedBoolean $binding.nativeExecution $false 'full proof native execution'
 Assert-KmcScopedString $binding.assertionKind 'launcher-code-mediated-per-root-before-after-equality' 'full proof assertion';Assert-KmcScopedBoolean $binding.perRootDigestsCaptured $false 'full proof root digests'
 $documents=@{};$unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($name in @('execution','observation','process','callerExit','beforeInputs','afterInputs','log')){
  if(-not$unique.Add((Get-KmcScopedPath $binding.$name.path))){throw 'Full proof record paths alias.'}
  if($name-ceq'log'){$raw=Read-KmcScopedPinnedBytes $binding.$name $Owned $analysis 64MB;$documents[$name]=ConvertFrom-KmcScopedLogBytes $raw}
  else{$documents[$name]=Read-KmcScopedPinnedJson $binding.$name $Owned $analysis}
 }
 $ex=$documents.execution;$ob=$documents.observation;$pr=$documents.process;$exit=$documents.callerExit
 Assert-KmcScopedInteger $ex.schemaVersion 1 1 'execution schema';Assert-KmcScopedString $ex.kind 'preview5-fresh-full-whatif-execution' 'execution kind'
 foreach($key in @('source','sourceTree','harness','harnessTree')){if($ex.$key-isnot[string]-or$ex.$key-cnotmatch'^[0-9a-f]{40}$'){throw 'Full execution lacks exact commit/tree identity.'}}
 Assert-KmcScopedString $ex.repositoryRoot (Join-Path (Get-KmcScopedPath $LabRoot) 'repo\KingmakerMountedCombat') 'execution repository'
 if($ex.auditRoots-isnot[Array]-or$ex.auditRoots.Count-ne5){throw 'Full execution lacks exactly five audit roots.'}
 $roots=@((Join-Path $LabRoot 'runtime-state'),(Join-Path $LabRoot 'runtime-backups'),(Join-Path $LabRoot 'runtime-staging'),(Join-Path $LabRoot 'runtime-evidence'))
 for($i=0;$i-lt4;$i++){Assert-KmcScopedString $ex.auditRoots[$i] (Get-KmcScopedPath $roots[$i]) 'historical root identity'}
 foreach($rootPath in @($ex.auditRoots[4],$ex.saveRoot,$ex.frozenSourceRoot,$ex.packageValidationScratchRoot)){if($rootPath-isnot[string]){throw 'Full execution root is not a scalar path.'};[void](Get-KmcScopedPath $rootPath)}
 Assert-KmcScopedInteger $ob.schemaVersion 1 1 'observation schema';Assert-KmcScopedString $ob.kind 'full-whatif-supervisor-observation' 'observation kind';Assert-KmcScopedString $ob.phase 'full-whatif' 'observation phase';Assert-KmcScopedString $ob.status 'PASS' 'observation status'
 foreach($key in @('source','harness')){Assert-KmcScopedString $ob.$key $ex.$key ('observation '+$key)}
 Assert-KmcScopedString $ob.executionSha256 $binding.execution.sha256 'observation execution';Assert-KmcScopedString $ob.argumentsSha256 $ex.arguments.sha256 'observation arguments'
 foreach($key in @('purityLinePresent','actualProcessExitRequiresCallerReceipt','beforePinsVerified','afterPinsVerified')){Assert-KmcScopedBoolean $ob.$key $true ('observation '+$key)}
 foreach($key in @('nativeExecution','perRootDigestsCaptured')){Assert-KmcScopedBoolean $ob.$key $false ('observation '+$key)}
 Assert-KmcScopedInteger $ob.purityLineCount 1 1 'full purity line count';Assert-KmcScopedString $ob.assertionKind $binding.assertionKind 'observation assertion'
 if($null-ne$ob.error){throw 'Full observation contains an error.'}
 if(($ob.auditRoots|ConvertTo-Json -Compress)-cne($ex.auditRoots|ConvertTo-Json -Compress)){throw 'Full observation root identity differs.'}
 Assert-KmcScopedString $ob.log $binding.log.path 'full log path';Assert-KmcScopedString $ob.logSha256 $binding.log.sha256 'full log hash'
 $purity='Runtime WhatIf purity PASS; exact fixture descriptors were validated when required, and no evidence, lock, transaction, Mods, process, game, or save mutation occurred.'
 if([regex]::Matches($documents.log,('(?m)^'+[regex]::Escape($purity)+'\r?$')).Count-ne1){throw 'Pinned raw full log does not contain the exact single full assertion.'}
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
 Assert-KmcScopedInteger $exit.exitCode 0 0 'actual caller exit';Assert-KmcScopedBoolean $exit.completed $true 'actual caller completion'
 foreach($key in @('toolSessionId','toolChunkId')){if(($exit.$key-isnot[string]-and$exit.$key-isnot[int]-and$exit.$key-isnot[long])-or[string]::IsNullOrWhiteSpace([string]$exit.$key)){throw 'Caller tool observation identity is missing.'}}
 $stamps=@($pr.startedAtUtc,$ob.startedUtc,$ob.endedUtc,$exit.observedCompletionUtc)|ForEach-Object{$stamp=[DateTimeOffset]::MinValue;if($_-isnot[string]-or-not[DateTimeOffset]::TryParseExact($_,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$stamp)){throw 'Full proof timestamps are invalid.'};$stamp}
 if($stamps[0]-ne$stamps[1]-or$stamps[2]-lt$stamps[1]-or$stamps[3]-lt$stamps[2]){throw 'Full proof observation ordering differs.'}
 [pscustomobject]@{binding=$binding;documents=$documents;path=$path;sha256=$ExpectedSha256;historicalAssertion=$binding.assertionKind;rootDigestsAvailable=$false;callerObservationIsOsAttestation=$false}
}
