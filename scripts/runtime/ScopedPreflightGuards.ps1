# Execute source-bound original launcher spans. Historical-root purity is a
# different, inherited claim; this adapter never reaches ShouldProcess or native.
Set-StrictMode -Version Latest
function Assert-KmcScopedLeases($Owned){
 if($null-eq$Owned-or$Owned.Count-lt1){throw 'No admitted input/effect observation is owned.'}
 foreach($lease in $Owned){[void]$lease.VerifyUnchanged()}
}
function Get-KmcScopedOriginalGuardProgram([string]$LauncherPath,[string]$ExpectedSha256){
 Assert-KmcScopedSha $ExpectedSha256 'original launcher hash'
 if((Get-FileHash -LiteralPath $LauncherPath).Hash.ToLowerInvariant()-cne$ExpectedSha256){throw 'Original launcher bytes changed before AST admission.'}
 $text=[IO.File]::ReadAllText($LauncherPath);$tokens=$null;$errors=$null
 $ast=[Management.Automation.Language.Parser]::ParseInput($text,$LauncherPath,[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0-or$null-eq$ast.ParamBlock){throw 'Original launcher parse/parameter contract differs.'}
 $statements=@($ast.EndBlock.Statements)
 $cuts=@($statements|Where-Object{$_.Extent.Text-ceq'$WhatIfPreference=$requestedWhatIf'})
 if($cuts.Count-ne1){throw 'Original launcher preflight boundary is ambiguous.'}
 $cut=$cuts[0]
 $branches=@($statements|Where-Object{$_-is[Management.Automation.Language.IfStatementAst]-and$_.Extent.Text.StartsWith('if(-not $PSCmdlet.ShouldProcess(',[StringComparison]::Ordinal)})
 if($branches.Count-ne1-or$branches[0].Clauses.Count-ne1-or$null-ne$branches[0].ElseClause){throw 'Original launcher native-admission boundary differs.'}
 $whatIf=@($branches[0].Clauses[0].Item2.Statements)
 $repeat=@($whatIf|Where-Object{$_-is[Management.Automation.Language.IfStatementAst]-and$_.Extent.Text.StartsWith('if($isSaveBacked)',[StringComparison]::Ordinal)})
 $save=@($whatIf|Where-Object{$_.Extent.Text.StartsWith('if((Get-KmcSaveMetadataInventory $saveRoot)',[StringComparison]::Ordinal)})
 $process=@($whatIf|Where-Object{$_.Extent.Text-ceq'Assert-KmcNoGameProcesses'})
 $steam=@($whatIf|Where-Object{$_.Extent.Text.StartsWith('if($BootstrapOfflineCloudEvidence)',[StringComparison]::Ordinal)})
 $loader=@($whatIf|Where-Object{$_.Extent.Text.StartsWith('if($BaseGameOnly)',[StringComparison]::Ordinal)})
 foreach($part in @($repeat,$save,$process,$steam,$loader)){if($part.Count-ne1){throw 'Original final preflight guard span is absent or ambiguous.'}}
 if(-not$repeat[0].Extent.Text.Contains('KMC fixture identity changed during runtime WhatIf continuity validation.')){throw 'Original repeated-continuity guard differs.'}
 $sourceBlocks=@($statements|Where-Object{$_.Extent.Text.StartsWith('if(Test-KmcChunk7ManualSeedScenario $Scenario){$manualSeedSource=',[StringComparison]::Ordinal)-or$_.Extent.Text.StartsWith('if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){$bootstrapInventorySource=',[StringComparison]::Ordinal)})
 if($sourceBlocks.Count-ne2){throw 'Original closed source-admission spans differ.'}
 $spans=[Collections.Generic.List[object]]::new()
 $prefix=$text.Substring(0,$cut.Extent.StartOffset)
 $spans.Add([pscustomobject]@{role='unchanged-parameters-imports-and-complete-preflight';startOffset=0;endOffset=$cut.Extent.StartOffset;startLine=1;endLine=($cut.Extent.StartLineNumber-1)})
 $parts=[Collections.Generic.List[string]]::new();$parts.Add($prefix)
 # Exact original repeated continuity and final save/process/loader checks.
 foreach($pair in @(@('unchanged-repeated-continuity',$repeat[0]),@('unchanged-final-save-metadata',$save[0]),@('unchanged-final-process-refusal',$process[0]),@('unchanged-selected-steam-branch',$steam[0]),@('unchanged-final-loader-verification',$loader[0]))){
  $extent=$pair[1].Extent;$parts.Add($extent.Text);$spans.Add([pscustomobject]@{role=$pair[0];startOffset=$extent.StartOffset;endOffset=$extent.EndOffset;startLine=$extent.StartLineNumber;endLine=$extent.EndLineNumber})
 }
 $parts.Add('$scopedBeforeSource=if($Scenario-ceq''chunk7-bootstrap-cold-inventory''){$bootstrapInventorySource}else{$manualSeedSource}')
 foreach($block in $sourceBlocks){$parts.Add($block.Extent.Text);$spans.Add([pscustomobject]@{role='additional-repeated-unchanged-source-qualification';startOffset=$block.Extent.StartOffset;endOffset=$block.Extent.EndOffset;startLine=$block.Extent.StartLineNumber;endLine=$block.Extent.EndLineNumber})}
 $parts.Add('$scopedAfterSource=if($Scenario-ceq''chunk7-bootstrap-cold-inventory''){$bootstrapInventorySource}else{$manualSeedSource}')
 $parts.Add('if(($scopedBeforeSource|ConvertTo-Json -Depth 24 -Compress)-cne($scopedAfterSource|ConvertTo-Json -Depth 24 -Compress)){throw ''Selected source identity changed during scoped qualification.''}')
 $parts.Add('if(Test-Path -LiteralPath (Join-Path $runtimeState ''active-transaction.lock'')){throw ''A native transaction sentinel appeared during scoped qualification.''}')
 $parts.Add('[pscustomobject]@{manifest=$manifest;fixture=$fixturePayload;source=$scopedAfterSource;baseGameDependencies=$baseGameDependencies;currentModsDigest=$beforeMods.digest;currentSaveMetadataDigest=$beforeSaves.digest;harness=$KmcFrozenProductContext.harnessHead;sourceCommit=$KmcFrozenProductContext.source;originalScriptRoot=$PSScriptRoot;originalCommandPath=$PSCommandPath;originalBoundParameterNames=@($PSBoundParameters.Keys|Sort-Object);nativeExecution=$false;freshWholeRootPurity=$false}')
 $program=[string]::Join([Environment]::NewLine,$parts)
 $programAst=[Management.Automation.Language.Parser]::ParseInput($program,$LauncherPath,[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'The source-bound readonly original guard program did not parse.'}
 # Parser's file-name overload preserves automatic PSScriptRoot/PSCommandPath.
 [pscustomobject]@{script=$programAst.GetScriptBlock();sourcePath=$LauncherPath;sourceSha256=$ExpectedSha256;spans=@($spans);excluded=@('whole-historical-root before/after inventories and comparison','old whole-root purity PASS assertion','ShouldProcess and all live transaction/native body')}
}
function Invoke-KmcScopedFreshGuards($Proof,$Code,$Inputs,$Owned,$Rows){
 Assert-KmcScopedCase $Inputs.case
 if($null-eq$Rows){throw 'Fresh guard receipts must remain caller-owned on failure.'}
 $ex=$Proof.documents.execution;$repo=$Inputs.repositoryRoot;$plan=$Inputs.case
 $launcher=Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'
 $pin=@($Code.expected|Where-Object{$_.path-ceq$launcher})
 if($pin.Count-ne1-or$pin[0].provenance-cne'unchanged-full-audit-input'){throw 'Original launcher is not the exact historically audited executable.'}
 Assert-KmcScopedLeases $Owned
 $program=Get-KmcScopedOriginalGuardProgram $launcher $pin[0].sha256
 $args=@{Scenario=$plan.scenario;RunId=$plan.runId;SaveAccessAllowed=$true;BaseGameOnly=$true;TimeoutSeconds=$plan.timeoutSeconds;PackagePath=$ex.package.path;FrozenSourceRoot=$ex.frozenSourceRoot;FrozenSourceFreezePath=$ex.freeze.path;ExpectedFrozenSourceFreezeSha256=$ex.freeze.sha256;QualificationSuiteSnapshotPath=$ex.suite.path;ExpectedQualificationSuiteId=$plan.suiteId;ExpectedQualificationSuiteSnapshotSha256=$ex.suite.sha256}
 if($plan.scenario-ceq'chunk7-bootstrap-cold-inventory'){$args.BootstrapInventorySelectionPath=$plan.selection.path}else{$args.ManualSeedSelectionPath=$plan.selection.path}
 $started=[DateTimeOffset]::UtcNow
 try{
  $result=& $program.script @args
  if($null-eq$result-or$result-is[Array]){throw 'Original readonly guards returned an unexpected result stream.'}
  Assert-KmcScopedString $result.harness $Code.expectedHarness 'current reviewed harness';Assert-KmcScopedString $result.sourceCommit $ex.source 'immutable product source'
  Assert-KmcScopedString $result.originalScriptRoot (Split-Path $launcher -Parent) 'original script root';Assert-KmcScopedString $result.originalCommandPath $launcher 'original command path'
  Assert-KmcScopedLeases $Owned
  $Rows.Add([pscustomobject]@{name='fresh-unchanged-original-case-preflight';status='PASS';startedUtc=$started.ToString('o');finishedUtc=[DateTimeOffset]::UtcNow.ToString('o');sourcePath=$launcher;sourceSha256=$program.sourceSha256;spans=$program.spans;excluded=$program.excluded})
  $result
 }catch{
  $Rows.Add([pscustomobject]@{name='fresh-unchanged-original-case-preflight';status='FAIL';startedUtc=$started.ToString('o');finishedUtc=[DateTimeOffset]::UtcNow.ToString('o');sourcePath=$launcher;sourceSha256=$program.sourceSha256;spans=$program.spans;error=$_.Exception.Message})
  throw
 }
}
