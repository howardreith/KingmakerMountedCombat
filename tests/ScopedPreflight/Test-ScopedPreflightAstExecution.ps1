param([string]$HomeRoot,[string]$FixtureRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve actual AST execution evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$envKeys=@('TEMP','TMP');$saved=@{};foreach($key in $envKeys){$saved[$key]=[Environment]::GetEnvironmentVariable($key,'Process');[Environment]::SetEnvironmentVariable($key,$OutputRoot,'Process')}
$script:pass=0;$script:fail=0;$script:results=[Collections.Generic.List[object]]::new()
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;$script:results.Add([pscustomobject]@{name=$Name;status='PASS'});Write-Output ('PASS '+$Name)}catch{$script:fail++;$script:results.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()});Write-Output ('FAIL '+$Name+': '+$_)}}
function Refuses([scriptblock]$Action,[string]$Pattern){$errorSeen=$null;try{&$Action|Out-Null}catch{$errorSeen=$_.Exception.Message};if($null-eq$errorSeen-or$errorSeen-cnotmatch$Pattern){throw ('Required refusal absent/wrong: '+$errorSeen)}}
try{
 . (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
 . (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightGuards.ps1')
 $path=Join-Path $FixtureRoot 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1';$sha=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();$text=[IO.File]::ReadAllText($path)
 $program=Get-KmcScopedOriginalGuardProgram $path $sha
 $tokens=$null;$errors=$null;$original=[Management.Automation.Language.Parser]::ParseInput($text,$path,[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Original parse failed.'}
 $prefix=$text.Substring(0,$original.ParamBlock.Extent.EndOffset)
 $inspect=$prefix+[Environment]::NewLine+'[pscustomobject]@{root=$PSScriptRoot;path=$PSCommandPath;scenario=$Scenario;runId=$RunId;timeout=$TimeoutSeconds;save=[bool]$SaveAccessAllowed;baseOnly=[bool]$BaseGameOnly;steam=$SteamPath;whatIf=[bool]$WhatIfPreference;bound=@($PSBoundParameters.Keys|Sort-Object)}'
 $inspectAst=[Management.Automation.Language.Parser]::ParseInput($inspect,$path,[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Exact original parameter subset failed parse.'}
 $inspectBlock=$inspectAst.GetScriptBlock()
 Good 'eight admitted spans retain exact original byte text and original filename' {
  if($program.spans.Count-ne8-or$program.script.Ast.Extent.File-cne$path){throw 'Span/file binding differs'}
  foreach($span in $program.spans){$originalText=$text.Substring($span.startOffset,$span.endOffset-$span.startOffset);if(-not$program.script.ToString().Contains($originalText)){throw ('Original source span changed: '+$span.role)}}
 }
 Good 'original parameter block including attributes/defaults preserved exactly' {if($program.script.Ast.ParamBlock.Extent.Text-cne$original.ParamBlock.Extent.Text){throw 'Parameter contract differs'}}
 Good 'actual ParseInput GetScriptBlock preserves PSScriptRoot and PSCommandPath' {$r=&$inspectBlock;if($r.root-cne(Split-Path $path -Parent)-or$r.path-cne$path){throw 'Automatic original path binding lost'}}
 Good 'actual original defaults stay unchanged' {$r=&$inspectBlock;if($r.scenario-cne'mod-load-smoke'-or$r.timeout-ne360-or$r.save-or$r.baseOnly-or$r.steam-cne'C:\Program Files (x86)\Steam\steam.exe'-or$r.bound.Count-ne0){throw 'Original defaults differ'}}
 foreach($scenario in @('chunk7-bootstrap-cold-inventory','chunk7-bootstrap-manual-seed')){
  Good ('actual selected splat semantics '+$scenario) {$arguments=@{Scenario=$scenario;RunId='private.ast-01';TimeoutSeconds=360;SaveAccessAllowed=$true;BaseGameOnly=$true;ExpectedFrozenSourceFreezeSha256=('a'*64)};$r=&$inspectBlock @arguments;if($r.scenario-cne$scenario-or$r.runId-cne'private.ast-01'-or-not$r.save-or-not$r.baseOnly-or($r.bound-join'|')-cne(@($arguments.Keys|Sort-Object)-join'|')){throw 'Exact splatted binding differs'}}
 }
 Good 'common WhatIf binding executes no ShouldProcess' {$r=&$inspectBlock -WhatIf;if(-not$r.whatIf-or$r.bound.Count-ne1-or$r.bound[0]-cne'WhatIf'){throw 'Original common parameter binding differs'}}
 Good 'original Scenario ValidateSet refuses foreign family' {Refuses {&$inspectBlock -Scenario 'foreign'} 'ValidateSet|does not belong|Cannot validate'}
 Good 'original RunId ValidatePattern refuses separators' {Refuses {&$inspectBlock -RunId '../escape'} 'pattern|Cannot validate'}
 Good 'original TimeoutSeconds ValidateRange refuses changed lower bound' {Refuses {&$inspectBlock -TimeoutSeconds 359} 'range|Cannot validate'}
 Good 'original exact hash ValidatePattern refuses non-hex' {Refuses {&$inspectBlock -ExpectedFrozenSourceFreezeSha256 ('g'*64)} 'pattern|Cannot validate'}
 Good 'actual unchanged imports and cold base-only refusal execute' {Refuses {&$program.script -Scenario chunk7-bootstrap-cold-inventory -RunId private-ast-cold} 'Bootstrap cold inventory requires explicit base-only staging'}
 Good 'actual unchanged imports and Manual base-only refusal execute' {Refuses {&$program.script -Scenario chunk7-bootstrap-manual-seed -RunId private-ast-manual} 'Manual seed requires explicit base-only staging'}
 Good 'actual unchanged foreign native-bootstrap selection refusal executes' {Refuses {&$program.script -Scenario chunk7-bootstrap-cold-inventory -BaseGameOnly -NativeBootstrapCase authored-main-campaign} 'Native bootstrap selection is forbidden'}
 Good 'actual unchanged wrong-family Manual selection refusal executes' {Refuses {&$program.script -Scenario chunk7-bootstrap-cold-inventory -BaseGameOnly -ManualSeedSelectionPath 'private-unused.json'} 'Manual seed selection is forbidden'}
 Good 'actual unchanged wrong-family cold selection refusal executes' {Refuses {&$program.script -Scenario mod-load-smoke -BootstrapInventorySelectionPath 'private-unused.json'} 'Bootstrap inventory selection is forbidden'}
 Good 'exact original program excludes ShouldProcess and whole historical collections' {
  $ptext=$program.script.ToString();$nativeCalls=@($program.script.Ast.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true));if($nativeCalls.Count-ne0-or$ptext.Contains('$beforeRoots=@(')-or$ptext.Contains('$afterRoots=@(')-or$ptext.Contains('Runtime WhatIf purity PASS;')-or$ptext.Contains('$lock=Acquire-KmcRuntimeLock')){throw 'Excluded live/full-audit operation admitted'}
 }
 Good 'all exact fresh calls occur in reviewed program' {
  $required=@('Assert-KmcQualificationSuiteContinuity','Assert-KmcPackageManifest','Assert-KmcNoGameProcesses','Assert-KmcBaseGameDependencyReceipt','Get-KmcChunk7ManualSeedSource','Get-KmcChunk7BootstrapInventorySource','Assert-KmcSaveMetadataInventoriesEqual')
  $calls=@($program.script.Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true)|ForEach-Object{$_.GetCommandName()})
  foreach($name in $required){if($name-cnotin$calls){throw ('Lost original guard '+$name)}}
  if(@($calls|Where-Object{$_-ceq'Get-KmcChunk7ManualSeedSource'}).Count-ne2-or@($calls|Where-Object{$_-ceq'Get-KmcChunk7BootstrapInventorySource'}).Count-ne2){throw 'Source before/after validation not exact'}
 }
 Good 'historical source hash drift refuses before parsing' {Refuses {Get-KmcScopedOriginalGuardProgram $path ('0'*64)} 'Original launcher bytes changed'}
 foreach($case in @(@('missing cutoff','$WhatIfPreference=$requestedWhatIf','$WhatIfPreference = $requestedWhatIf','preflight boundary'),@('missing final loader','if($BaseGameOnly){Assert-KmcBaseGameDependencyReceipt','if ($BaseGameOnly){Assert-KmcBaseGameDependencyReceipt','final preflight guard'))){
  Good ('changed original AST boundary '+$case[0]) {$mutated=Join-Path $OutputRoot ($case[0].Replace(' ','-')+'.ps1');[IO.File]::WriteAllText($mutated,$text.Replace($case[1],$case[2]),[Text.UTF8Encoding]::new($false));Refuses {Get-KmcScopedOriginalGuardProgram $mutated (Get-FileHash -LiteralPath $mutated).Hash.ToLowerInvariant()} $case[3]}
 }
 $receipt=[ordered]@{schemaVersion=1;status=$(if($script:fail){'FAIL'}else{'PASS'});pass=$script:pass;fail=$script:fail;sourcePath=$path;sourceSha256=$sha;adapterSha256=(Get-FileHash -LiteralPath (Join-Path $HomeRoot 'overlays/scripts/runtime/ScopedPreflightGuards.ps1')).Hash.ToLowerInvariant();spans=$program.spans;checks=@($script:results);actualOriginalImportsAndEarlyRefusals=$true;actualOriginalParameterBinding=$true;completeSuccessfulGuardChainExecuted=$false;nativeExecution=$false;archiveOperations=0;externalBoundarySubstitutions=@();limits=@('Parameter-only observation subset proves binding semantics, not downstream guard success','Full original program executions intentionally refuse before source, Git, package, suite or native actions','Exact source span/call proof does not promote a future cold or Manual case to PASS')}
 [IO.File]::WriteAllText((Join-Path $OutputRoot 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
 Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
 if($script:fail){throw 'Actual original AST execution regression failed'}
}finally{foreach($key in $envKeys){[Environment]::SetEnvironmentVariable($key,$saved[$key],'Process')}}
