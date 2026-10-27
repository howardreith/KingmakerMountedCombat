# Closed dependency discovery before any original runtime script is imported.
# Code identities are historical; the reviewed additive implementation/current
# data identities are separate. Neither is a substitute for fresh guards.
Set-StrictMode -Version Latest
function Assert-KmcScopedRelative($Value,[string]$Description){
 if($Value-isnot[string]-or[string]::IsNullOrEmpty($Value)-or$Value.Contains('\')-or$Value-cmatch'(^/|(^|/)\.\.?(/|$)|:)' ){throw ($Description+' is not a literal repository-relative path.')}
}
function Assert-KmcScopedRunId($Value,[string]$Description){
 # Reject Windows dot/device aliases before any derived tree is acquired.
 if($Value-isnot[string]-or$Value-cnotmatch'^[A-Za-z0-9_-](?:[A-Za-z0-9._-]{0,118}[A-Za-z0-9_-])?$'-or$Value-imatch'^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)'){throw ($Description+' run identity is not an exact unambiguous child name.')}
}
function Get-KmcScopedCurrentCode($Proof,$Implementation){
 Assert-KmcScopedMembers $Implementation @('schemaVersion','kind','inheritedProofSha256','expectedHarness','added','changedMarkdown','commandFiles') 'reviewed additive implementation'
 Assert-KmcScopedInteger $Implementation.schemaVersion 1 1 'implementation schema'
 Assert-KmcScopedString $Implementation.kind 'reviewed-closed-preflight-implementation' 'implementation kind'
 Assert-KmcScopedString $Implementation.inheritedProofSha256 $Proof.sha256 'implementation full proof'
 if($Implementation.expectedHarness-isnot[string]-or$Implementation.expectedHarness-cnotmatch'^[0-9a-f]{40}$'){throw 'Current reviewed harness commit is missing.'}
 foreach($key in @('added','changedMarkdown','commandFiles')){if($Implementation.$key-isnot[Array]-or$Implementation.$key.Count-gt128){throw 'Implementation changes must be bounded exact arrays.'}}
 $required=@('scripts/runtime/Invoke-Chunk7ScopedPreflight.ps1','scripts/runtime/ScopedPreflightProof.ps1','scripts/runtime/ScopedPreflightClosure.ps1','scripts/runtime/ScopedPreflightGuards.ps1','scripts/runtime/ScopedPreflightPlatform.ps1','scripts/runtime/ScopedPreflightObservation.cs','scripts/runtime/ScopedPreflightFullAuditReevaluation.ps1')
 if($Implementation.commandFiles.Count-ne$required.Count){throw 'The implementation executable closure is incomplete.'}
 $original=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
 foreach($row in $Proof.documents.beforeInputs.tracked){$original.Add($row.relativePath,$row)}
 $current=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
 foreach($row in $Proof.documents.beforeInputs.tracked){$current.Add($row.relativePath,[pscustomobject]@{relativePath=$row.relativePath;path=$row.path;sha256=$row.sha256;bytes=$row.bytes;provenance='unchanged-full-audit-input'})}
 $changed=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($row in $Implementation.changedMarkdown){
  Assert-KmcScopedMembers $row @('relativePath','beforeSha256','sha256','bytes') 'reviewed Markdown change'
  Assert-KmcScopedRelative $row.relativePath 'reviewed Markdown path';Assert-KmcScopedSha $row.beforeSha256 'previous Markdown';Assert-KmcScopedSha $row.sha256 'current Markdown';Assert-KmcScopedInteger $row.bytes 1 20MB 'current Markdown bytes'
  if([IO.Path]::GetExtension($row.relativePath)-cne'.md'-or-not$original.ContainsKey($row.relativePath)-or-not$changed.Add($row.relativePath)){throw 'Only explicitly reviewed existing Markdown data may change.'}
  Assert-KmcScopedString $row.beforeSha256 $original[$row.relativePath].sha256 'previous Markdown identity'
  $old=$original[$row.relativePath]
  $current[$row.relativePath]=[pscustomobject]@{relativePath=$row.relativePath;path=$old.path;sha256=$row.sha256;bytes=$row.bytes;provenance='separately-reviewed-current-Markdown-data'}
 }
 foreach($row in $Implementation.added){
  Assert-KmcScopedMembers $row @('relativePath','sha256','bytes') 'reviewed added file'
  Assert-KmcScopedRelative $row.relativePath 'added path';Assert-KmcScopedSha $row.sha256 'added file';Assert-KmcScopedInteger $row.bytes 1 20MB 'added bytes'
  if($original.ContainsKey($row.relativePath)-or-not$changed.Add($row.relativePath)){throw 'Added implementation path aliases an existing path.'}
  $allowed=$row.relativePath-cin$required-or$row.relativePath-cmatch'^(scripts/chunk7/Test-ScopedPreflight[A-Za-z0-9]*\.ps1|tests/ScopedPreflight/[A-Za-z0-9._/-]+\.(cs|ps1|json|md)|docs/CHUNK7-SCOPED-PREFLIGHT[A-Za-z0-9._-]*\.md)$'
  if(-not$allowed-or$row.relativePath-cmatch'(^|/)\.\.?(/|$)'){throw 'An unrelated new dependency cannot borrow this full proof.'}
  $path=Assert-KmcScopedChild (Join-Path $Proof.documents.execution.repositoryRoot $row.relativePath) $Proof.documents.execution.repositoryRoot 'added current input'
  $current.Add($row.relativePath,[pscustomobject]@{relativePath=$row.relativePath;path=$path;sha256=$row.sha256;bytes=$row.bytes;provenance='separately-reviewed-additive-implementation'})
 }
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($row in $Implementation.commandFiles){
  Assert-KmcScopedMembers $row @('relativePath','sha256','bytes') 'command input'
  if($row.relativePath-isnot[string]-or$row.relativePath-cnotin$required-or-not$seen.Add($row.relativePath)-or-not$current.ContainsKey($row.relativePath)){throw 'Command input is absent or ambiguous.'}
  $expected=$current[$row.relativePath]
  Assert-KmcScopedString $row.sha256 $expected.sha256 'command hash';Assert-KmcScopedInteger $row.bytes $expected.bytes $expected.bytes 'command bytes'
 }
 # A Markdown update is data for the unchanged source validator. No historical
 # executable, runtime reader, project, configuration or native source is waived.
 @($current.Values|Sort-Object -Property relativePath -CaseSensitive)
}
function Assert-KmcScopedTrackedPathSet([string[]]$Actual,$Expected){
 if($null-eq$Actual-or$Actual.Count-ne$Expected.Count){throw 'Current tracked dependency set differs from the reviewed additive closure.'}
 $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($path in $Actual){if(-not$names.Add($path)){throw 'Tracked dependency enumeration is ambiguous.'}}
 foreach($entry in $Expected){if(-not$names.Contains($entry.relativePath)){throw ('Unreviewed tracked dependency added, removed or renamed: '+$entry.relativePath)}}
}
function New-KmcScopedCodeLease($Proof,$Implementation,$Owned,$Platform=$null){
 $expected=@(Get-KmcScopedCurrentCode $Proof $Implementation)
 $lease=New-KmcScopedObservation $Owned
 foreach($row in $expected){$lease.AddFile($row.path)}
 # The validator reads recursively discovered native source as well as tracked
 # text. Holding these namespaces detects planted ignored/new source inputs.
 $repo=$Proof.documents.execution.repositoryRoot
 foreach($directory in @('src','scripts','planning')){$lease.AddTree((Join-Path $repo $directory))}
 foreach($row in $Proof.documents.beforeInputs.additional){
  $platformPin=@(if($null-ne$Platform){$Platform.plan.files|Where-Object{$_.path-ceq$row.path}})
  if($platformPin.Count-gt1){throw 'Historical platform dependency is ambiguous.'}
  if($platformPin.Count-eq1){$Platform.lease.RequirePinnedFile($row.path,$row.bytes,$row.sha256)}
  elseif(@($expected|Where-Object{$_.path-ceq$row.path}).Count-eq0){$lease.AddFile($row.path)}
 }
 $lease.Seal()
 foreach($row in $expected){$lease.RequirePinnedFile($row.path,$row.bytes,$row.sha256)}
 foreach($row in $Proof.documents.beforeInputs.additional){
  if($null-ne$Platform-and@($Platform.plan.files|Where-Object{$_.path-ceq$row.path}).Count-eq1){$Platform.lease.RequirePinnedFile($row.path,$row.bytes,$row.sha256)}else{$lease.RequirePinnedFile($row.path,$row.bytes,$row.sha256)}
 }
 $known=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($row in @($expected)+@($Proof.documents.beforeInputs.additional)){[void]$known.Add((Get-KmcScopedPath $row.path))}
 foreach($entry in $lease.Before){
  if($entry.Kind-cne'file'-or$known.Contains($entry.Path)){continue}
  $relative=$entry.Path.Substring($repo.Length+1).Replace('\','/')
  if($relative-cnotmatch'^(src|scripts|planning)/'){throw 'Unknown code observation escaped its namespace.'}
  # Only the native validator's explicit src bin/obj exclusion is allowed.
  # These bytes/names are still observed; all other unknown files are refused.
  if($relative-cmatch'^src/(?:.*/)?(bin|obj)/'){continue}
  throw ('Untracked or ignored executable/data dependency appeared: '+$relative)
 }
 [pscustomobject]@{lease=$lease;expected=$expected;expectedHarness=$Implementation.expectedHarness}
}
function Assert-KmcScopedCase($Plan){
 Assert-KmcScopedMembers $Plan @('schemaVersion','kind','runId','scenario','baseGameOnly','saveAccessAllowed','selection','suiteId','timeoutSeconds') 'closed preflight case'
 Assert-KmcScopedInteger $Plan.schemaVersion 1 1 'case schema';Assert-KmcScopedString $Plan.kind 'closed-chunk7-preflight-case' 'case kind'
 Assert-KmcScopedRunId $Plan.runId 'Destination'
 if($Plan.scenario-isnot[string]-or$Plan.scenario-cnotin@('chunk7-bootstrap-cold-inventory','chunk7-bootstrap-manual-seed')){throw 'Only cold inventory and Manual seed preflight are selected.'}
 Assert-KmcScopedBoolean $Plan.baseGameOnly $true 'base-only gate';Assert-KmcScopedBoolean $Plan.saveAccessAllowed $true 'explicit save-access gate'
 Assert-KmcScopedInteger $Plan.timeoutSeconds 600 600 'selected native timeout'
 if($Plan.suiteId-isnot[string]-or$Plan.suiteId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'){throw 'Exact suite identity is required.'}
 Assert-KmcScopedMembers $Plan.selection @('path','sha256','bytes') 'selection pin';Assert-KmcScopedSha $Plan.selection.sha256 'selection hash';Assert-KmcScopedInteger $Plan.selection.bytes 1 16384 'selection bytes'
}
function Get-KmcScopedDynamicInputs($Proof,$Plan,$Owned){
 Assert-KmcScopedCase $Plan
 $ex=$Proof.documents.execution;$lab=[IO.Path]::GetFullPath((Join-Path $ex.repositoryRoot '../..')).TrimEnd('\');$analysis=Join-Path $lab 'analysis-cache'
 $selection=Read-KmcScopedPinnedJson $Plan.selection $Owned $analysis
 # This initial pass authorizes only bounded dependency discovery; the original
 # exact selection/descriptor/source validators still run after every lease.
 $names=@('sourceRunId');if($Plan.scenario-ceq'chunk7-bootstrap-manual-seed'){$names+=('inventoryRunId')}
 $runIds=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($key in $names){Assert-KmcScopedRunId $selection.$key 'Source';if($selection.$key-ieq$Plan.runId-or-not$runIds.Add($selection.$key)){throw 'Case source identity is invalid or aliases its destination.'}}
 $suite=Read-KmcScopedPinnedJson $ex.suite $Owned $lab
 $intake=Read-KmcScopedPinnedJson $ex.environmentIntake $Owned $lab
 Assert-KmcScopedString $suite.suiteId $Plan.suiteId 'current selected suite'
 Assert-KmcScopedString $intake.requestedLayout.kingmakerModsRoot $ex.auditRoots[4] 'current Mods root';Assert-KmcScopedString $intake.requestedLayout.kingmakerSaveRoot $ex.saveRoot 'current save root'
 $game=Get-KmcScopedPath $intake.requestedLayout.kingmakerInstallDir
 Assert-KmcScopedString $intake.requestedLayout.kingmakerModsRoot (Join-Path $game 'Mods') 'base-only Mods layout'
 $lease=New-KmcScopedObservation $Owned
 $state=Join-Path $lab 'runtime-state';$runtime=Join-Path $ex.repositoryRoot 'scripts/runtime'
 $scopes=[Collections.Generic.List[object]]::new()
 function AddFileScope([string]$Path,[string]$Role,[bool]$Optional=$false){$p=Get-KmcScopedPath $Path;if($Optional){$lease.AddOptionalFile($p)}else{$lease.AddFile($p)};$scopes.Add([pscustomobject]@{kind=$(if($Optional){'optional-file'}else{'file'});path=$p;role=$Role})}
 function AddTreeScope([string]$Path,[string]$Role){$p=Get-KmcScopedPath $Path;$lease.AddTree($p);$scopes.Add([pscustomobject]@{kind='tree';path=$p;role=$Role})}
 AddFileScope (Join-Path $state 'active-transaction.lock') 'must-remain-absent-native-transaction-sentinel' $true
 AddFileScope (Join-Path $state 'fixture-qualification.json') 'permanent-fixture-qualification'
 $evidenceRoot=Join-Path $lab 'runtime-evidence';$profileRoot=Join-Path $lab 'runtime-staging'
 $destinationEvidence=Assert-KmcScopedChild (Join-Path $evidenceRoot $Plan.runId) $evidenceRoot 'destination evidence'
 $destinationProfile=Assert-KmcScopedChild (Join-Path $profileRoot ('persistence-'+$Plan.runId)) $profileRoot 'destination profile'
 foreach($target in @($destinationEvidence,$destinationProfile)){
  $lease.AddOptionalTree($target);$scopes.Add([pscustomobject]@{kind='optional-tree';path=$target;role='selected-destination-must-remain-absent'})
  if(Test-Path -LiteralPath $target){throw 'Requested destination evidence/profile already exists.'}
 }
 foreach($pin in @($ex.package,$ex.manifest,$ex.freeze)){AddFileScope $pin.path 'immutable-candidate'}
 foreach($id in $runIds){
  $sourceEvidence=Assert-KmcScopedChild (Join-Path $evidenceRoot $id) $evidenceRoot 'source evidence'
  $sourceProfile=Assert-KmcScopedChild (Join-Path $profileRoot ('persistence-'+$id)) $profileRoot 'source profile'
  AddTreeScope $sourceEvidence 'complete-selected-source-evidence';AddTreeScope $sourceProfile 'complete-selected-source-profile'
 }
 foreach($authority in $suite.historicalAuthorities.protectedSaveAuthorities){
  if($authority.path-isnot[string]){throw 'Protected authority path is not scalar.'}
  $p=Assert-KmcScopedChild $authority.path (Join-Path $state 'protected-save-authorities') 'suite protected authority'
  AddFileScope $p 'referenced-historical-save-authority'
 }
 AddTreeScope $ex.saveRoot 'all-current-save-bytes-and-names';AddTreeScope $ex.auditRoots[4] 'all-current-Mods-bytes-and-names'
 AddTreeScope (Join-Path $game 'Kingmaker_Data/Managed/UnityModManager') 'installed-loader-DLL-set-config-Params'
 AddFileScope (Join-Path $game 'Kingmaker_Data/Managed/Assembly-CSharp.dll') 'base-gameplay-assembly'
 # The unchanged frozen helper reads these source files through Git and product
 # identity checks. Current Git metadata is checked separately; native binaries
 # are not trusted merely because this worktree exists.
 foreach($row in $Proof.documents.beforeInputs.tracked){if($row.relativePath-cmatch'^(src/|Info\.json$|version\.json$|KingmakerMountedCombat\.sln$|Directory\.Build\.(props|targets)$)'){AddFileScope (Join-Path $ex.frozenSourceRoot $row.relativePath) 'frozen-product-input'}}
 $lease.Seal()
 foreach($pin in @($ex.package,$ex.manifest,$ex.freeze)){$lease.RequirePinnedFile($pin.path,$pin.bytes,$pin.sha256)}
 foreach($authority in $suite.historicalAuthorities.protectedSaveAuthorities){Assert-KmcScopedSha $authority.sha256 'suite authority hash';$row=@($lease.Before|Where-Object{$_.Path-ceq$authority.path});if($row.Count-ne1){throw 'Suite authority observation missing.'};$lease.RequirePinnedFile($authority.path,$row[0].Length,$authority.sha256)}

 if(Test-Path -LiteralPath (Join-Path $state 'active-transaction.lock')){throw 'An active or stale native transaction sentinel exists.'}
 [pscustomobject]@{lease=$lease;scopes=@($scopes);selection=$selection;intake=$intake;suite=$suite;labRoot=$lab;repositoryRoot=$ex.repositoryRoot;gameRoot=$game;saveRoot=$ex.saveRoot;modsRoot=$ex.auditRoots[4];stateRoot=$state;qualificationPath=(Join-Path $state 'fixture-qualification.json');historicalRootsNotReevaluated=@($ex.auditRoots[0..3]);case=$Plan}
}
