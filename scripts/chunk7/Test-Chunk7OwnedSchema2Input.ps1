param([string]$ReaderDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1')
$fixtures=(Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json')).fixtures
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1'),[ref]$null,[ref]$null)
foreach($node in $ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]},$true)){. ([scriptblock]::Create($node.Extent.Text))}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action,[bool]$Reject=$false){$why=$null;$ok=$false;try{&$Action;$ok=-not$Reject}catch{$why=$_.Exception.Message;$ok=$Reject};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$why});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$why}else{''}))}
Check 'native source3 preserves exact original owner generation7' {$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';Invoke-OwnedReader $v;$o=$v.artifact.observations;if($o.nativeWrite.sourceSchema-ne3-or$o.nativeWrite.archive.Pairs[0].Generation-ne7-or$o.nativeWrite.archive.Pairs[0].PairId-cne$o.mountedStart.owners[0].ownerId){throw 'Source custody changed.'}}
Check 'historical derivative2 cold load and native resave3 reader' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';Invoke-OwnedReader $v}
Check 'source3 and actual derivative2 bind through deterministic historical migration' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';Assert-KmcChunk7OwnerSeedCheckpointColdSource $v.request (New-KmcOwnerSeedCombatCheckpointProducer $v) $v.artifact}
Check 'third-process current3 child remains load-only and exact parent-backed' {$v=New-KmcOwnerSeedCurrentFixture 'owned-schema2-pending-current-manual';Invoke-OwnedReader $v;Assert-KmcChunk7CurrentMigrationParent $v.request (New-KmcOwnerSeedCurrentParent $v) $v.artifact}
Check 'current3 child rejects historical derivative property' {$v=New-KmcOwnerSeedCurrentFixture 'owned-schema2-pending-current-manual';$v.request|Add-Member schema2DerivationSha256 ('a'*64);Invoke-OwnedReader $v} $true
foreach($fault in @('native2-writer-claim','lost-source-contract','source-owner-id','source-generation','metadata-source-differs')){Check ('current pending source refuses '+$fault){$v=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$o=$v.artifact.observations
 switch($fault){'native2-writer-claim'{$o.nativeWrite.sourceSchema=2}'lost-source-contract'{$o.PSObject.Properties.Remove('nativeSourceContract')}'source-owner-id'{$o.nativeWrite.archive.Pairs[0].PairId='abcdefabcdef4def8defabcdefabcdef'}'source-generation'{$o.nativeWrite.archive.Participations[0].Generation=1}'metadata-source-differs'{$o.nativeWrite.nativeMetadata.GameTimeTicks++}}
 Invoke-OwnedReader $v} $true}
foreach($fault in @('missing-receipt','string-receipt','synthetic-unlabelled','original-aliased','pending-debt','pending-round','invented-activation','native-metadata-omitted','wrong-derived-owner')){Check ('historical cold refuses '+$fault){$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$m=$v.artifact.observations.schema2Migration
 switch($fault){'missing-receipt'{$v.request.PSObject.Properties.Remove('schema2DerivationSha256')}'string-receipt'{$v.request.schema2DerivationSha256=@('a'*64)}'synthetic-unlabelled'{$m.syntheticMetadata=$false}'original-aliased'{$m.originalNativeArchiveSha256=$v.request.persistenceLoad.sha256}'pending-debt'{$m.nativeInputMetadata.Mount.Move=0}'pending-round'{$m.nativeInputMetadata.Combat.Paired.PendingSplitRound++}'invented-activation'{$m.nativeInputMetadata.Combat.Paired.Activation=[pscustomobject]@{Sequence=1}}'native-metadata-omitted'{$m.PSObject.Properties.Remove('nativeInputMetadata')}'wrong-derived-owner'{$v.artifact.observations.loadedArchive.Pairs[0].PairId='abcdefabcdef4def8defabcdefabcdef'}}
 Invoke-OwnedReader $v} $true}
Check 'old acquired native2 source remains separate' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-save-legacy-mounted-pending-tb-manual')[0];Invoke-OwnedReader $v}
Check 'old acquired historical2 cold/resave remains separate' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-legacy-mounted-pending-tb-manual')[0];Invoke-OwnedReader $v}
$requestAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $ReaderDirectory 'Test-RuntimeRequest.ps1'),[ref]$null,[ref]$null)
$extraNode=@($requestAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$extra'-and$n.Operator.ToString()-ceq'Equals'},$true))
$refusalNode=@($requestAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$loadRefusal'-and$n.Operator.ToString()-ceq'Equals'},$true))
$keysNode=@($requestAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$commonRequired'},$true))
$shapeNode=@($requestAst.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcExactProperties'-and$n.Extent.Text.Contains("'runtime request v2'")},$true))
if($extraNode.Count-ne1-or$refusalNode.Count-ne1-or$keysNode.Count-ne1-or$shapeNode.Count-ne1){throw 'Actual request optional-field/shape authority is missing or ambiguous.'}
function Assert-ActualSchema2RequestShape($request){
 $alternating=$false;$validation=$false;$crossArea=$false;$chunk7Area=$false;$hasPersistenceCase=$true
 Assert-KmcOwnedSchema2Request $request
 . ([scriptblock]::Create($keysNode[0].Extent.Text));. ([scriptblock]::Create($refusalNode[0].Extent.Text));. ([scriptblock]::Create($extraNode[0].Extent.Text));. ([scriptblock]::Create($shapeNode[0].Extent.Text))
}
Check 'actual runtime request required/optional property shape admits only selected derivative' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$v.request|Add-Member transactionToken ('f'*64);$v.request|Add-Member qualificationSuite ([pscustomobject]@{suiteId='generated';snapshotSha256=('e'*64)});Assert-ActualSchema2RequestShape $v.request}
foreach($fault in @('absent','array','uppercase','source-direction','current-child-direction','acquired-direction')){Check ('actual runtime request shape refuses '+$fault){$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual'
 switch($fault){'absent'{$v.request.PSObject.Properties.Remove('schema2DerivationSha256')}'array'{$v.request.schema2DerivationSha256=@('a'*64)}'uppercase'{$v.request.schema2DerivationSha256='A'*64}'source-direction'{$v.request.ownedPairs.case='owner-seed-checkpoint-save'}'current-child-direction'{$v.request.persistenceCase='owned-schema2-pending-current-manual'}'acquired-direction'{$v.request.ownedPairs.case='persistence-load'}}
 Assert-ActualSchema2RequestShape $v.request} $true}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh private output is required.'};[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null
$script:virtualLab=Join-Path $OutputDirectory 'lab';[IO.Directory]::CreateDirectory($script:virtualLab)|Out-Null
function Get-KmcLabRoot{return $script:virtualLab}
function Get-KmcChunk7OwnedPairsSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){if($PersistenceCase-cne'legacy-mounted-pending-tb-manual'){throw 'Facade qualifier cannot select another case.'};$script:qualifiedSource}
function Assert-KmcRuntimeLockOwner($Lock){if($null-eq$Lock-or$Lock.Token-cne('1'*64)){throw 'Facade lock is not owned.'};$true}
function Assert-KmcNoGameProcesses{}
function Require([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
function Read-Members([string]$Path){$s=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);try{Get-KmcOwnedSchema1Members $s}finally{$s.Dispose()}}
function New-TestZip([string]$Path,$Metadata){
 Add-Type -AssemblyName System.IO.Compression;$s=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$z=$null
 try{$z=[IO.Compression.ZipArchive]::new($s,[IO.Compression.ZipArchiveMode]::Create,$false);$rows=[ordered]@{'header.json'='{"Name":"KMC_C7_SEED_CHECKPOINT","Type":"Manual"}';'kmc-mounted-state'=($Metadata|ConvertTo-Json -Depth 64 -Compress);'player.json'='{"synthetic":true,"unchanged":1}';'areas/stashed.json'='{"synthetic":true,"unchanged":2}'}
  foreach($key in $rows.Keys){$w=[IO.StreamWriter]::new($z.CreateEntry($key).Open(),[Text.UTF8Encoding]::new($false));try{$w.Write($rows[$key])}finally{$w.Dispose()}}
 }finally{if($z){$z.Dispose()};$s.Dispose()}
}
$native=New-KmcOwnerSeedCombatCheckpointSaveFixture 'legacy-mounted-pending-tb-manual';$cold=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$selection=Copy-Value $cold.request.ownedPairs
$saveRoot=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$selection.checkpointSourceRunId+'/Saved Games');[IO.Directory]::CreateDirectory($saveRoot)|Out-Null
$path=Join-Path $saveRoot $cold.request.persistenceLoad.fileName;New-TestZip $path $native.artifact.observations.nativeWrite.archive;$file=Get-Item -LiteralPath $path
$descriptor=Copy-Value $cold.request.persistenceLoad;$descriptor.sha256=Get-KmcSha256 $path;$descriptor.length=$file.Length;$descriptor.lastWriteTimeUtcTicks=$file.LastWriteTimeUtc.Ticks
$checkpointPath=Join-Path $OutputDirectory 'chunk7-persistence-evidence.json';[IO.File]::WriteAllText($checkpointPath,($native.artifact|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false))
$sourcePath=Join-Path $OutputDirectory 'owned-two-pair-source.json';[IO.File]::WriteAllText($sourcePath,'{"synthetic":true,"role":"original Manual source receipt"}',[Text.UTF8Encoding]::new($false))
$selection.checkpointSourceArtifactSha256=Get-KmcSha256 $checkpointPath
$script:qualifiedSource=[pscustomobject]@{path=$path;descriptor=$descriptor;artifactPath=$sourcePath;artifactSha256=(Get-KmcSha256 $sourcePath);checkpointArtifactPath=$checkpointPath;checkpointArtifactSha256=$selection.checkpointSourceArtifactSha256}
$source=$script:qualifiedSource;$fixture=$cold.request.fixture;$candidate=[pscustomobject]@{commit=$cold.request.commit;dllSha256=$cold.request.dllSha256;dllMvid=$cold.request.dllMvid};$lock=[pscustomobject]@{RunId='synthetic-owned-schema2-input';Token=('1'*64)};$derivative=$null
Check 'actual guarded CREATE_NEW copy replaces only metadata' {$script:derivative=New-KmcOwnedSchema2Input $source $selection $fixture $candidate $lock;$d=$script:derivative;Require ($d.descriptor.sha256-cne$descriptor.sha256-and$d.descriptor.fileName-ceq$descriptor.fileName) 'Derivative aliases or renames native source.'
 $before=Read-Members $path;$after=Read-Members $d.path;Assert-KmcOwnedSchema1MemberPreservation $before.members $after.members
 Require ($after.metadata.SchemaVersion-eq2-and$after.metadata.Combat.Paired.PendingSplitId-ceq$selection.mountAId-and$null-eq$after.metadata.Combat.Paired.Activation) 'Historical projection lost exact pending A obligation.'
 Require ((Get-KmcSha256 $path)-ceq$descriptor.sha256-and(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-eq$descriptor.lastWriteTimeUtcTicks) 'Native source was changed.'
}
$request=Copy-Value $cold.request;$request.runId=$lock.RunId;$request.ownedPairs=$selection
if($null-ne$derivative){$request.persistenceLoad=$derivative.descriptor;$request.schema2DerivationSha256=$derivative.receiptSha256}
Check 'exact eleven-field receipt is source-bound without claiming restoration' {Require ($null-ne$derivative) 'No derivative.';$receipt=Read-KmcOwnedSchema1PinnedJson $derivative.receiptPath $derivative.receiptSha256 256KB;Assert-KmcOwnedSchema2Derivation $receipt $request $source;Require (-not$receipt.nativeRestorationProven-and$receipt.nativeMembersVerified-eq3) 'Receipt overclaims native proof or omits native members.'}
Check 'qualified source drift is refused before copy' {$changed=Copy-Value $source;$changed.descriptor.sha256='e'*64;$script:qualifiedSource=$changed;try{New-KmcOwnedSchema2Input $source $selection $fixture $candidate $lock|Out-Null}finally{$script:qualifiedSource=$source}} $true
Check 'foreign selection cannot create a historical derivative' {$bad=Copy-Value $selection;$bad.case='persistence-load';New-KmcOwnedSchema2Input $source $bad $fixture $candidate $lock|Out-Null} $true
$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$lock.RunId);$saves=Join-Path $profile 'Saved Games';[IO.Directory]::CreateDirectory($saves)|Out-Null
$owner=[pscustomobject]@{runId=$lock.RunId;transactionToken=$lock.Token;scenario='chunk7-owned-two-pair-campaign';persistenceCase='legacy-mounted-pending-tb-manual';sourceSha256=$request.persistenceLoad.sha256;saveRoot=$saves}
[IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Check 'lock-owned staged provenance uses exact three leaves and hashes' {Require ($null-ne$derivative) 'No derivative.';Copy-KmcOwnedSchema2Inputs $source $derivative $profile $lock $request
 foreach($row in @(@('owned-two-pair-source.json',$source.artifactSha256),@('owned-schema2-checkpoint.json',$source.checkpointArtifactSha256),@('owned-schema2-derivation.json',$derivative.receiptSha256))){Require ((Get-KmcSha256 (Join-Path $profile $row[0]))-ceq$row[1]) 'Staged provenance changed.'}}
Check 'CREATE_NEW refuses a second provenance copy' {Copy-KmcOwnedSchema2Inputs $source $derivative $profile $lock $request} $true
Check 'foreign token is refused without an overwrite' {$owner.transactionToken='2'*64;[IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false));try{Copy-KmcOwnedSchema2Inputs $source $derivative $profile $lock $request}finally{$owner.transactionToken=$lock.Token;[IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))}} $true
Check 'foreign profile is refused' {Copy-KmcOwnedSchema2Inputs $source $derivative ($profile+'-foreign') $lock $request} $true
$artifact=Copy-Value $cold.artifact
if($null-ne$derivative){$receipt=Read-KmcOwnedSchema1PinnedJson $derivative.receiptPath $derivative.receiptSha256 256KB;$artifact.observations.schema2Migration.nativeInputMetadata=$receipt.metadata;$artifact.observations.loadedArchive=ConvertFrom-KmcOwnedSchema2Metadata $receipt.metadata $request;$artifact.observations.schema2Migration.derivationReceiptSha256=$request.schema2DerivationSha256;$artifact.observations.schema2Migration.originalNativeArchiveSha256=$descriptor.sha256;[IO.File]::Copy($derivative.path,(Join-Path $saves $descriptor.fileName),$false)}
Check 'cold-source qualification rechecks actual original and derivative member inventories' {Require ($null-ne$derivative) 'No derivative.';Assert-KmcOwnedSchema2ColdSource $request $source $artifact}
Check 'cold qualifier refuses stale derivation receipt bytes' {$p=Join-Path $profile 'owned-schema2-derivation.json';$old=[IO.File]::ReadAllBytes($p);try{[IO.File]::AppendAllText($p,' ');Assert-KmcOwnedSchema2ColdSource $request $source $artifact}finally{[IO.File]::WriteAllBytes($p,$old)}} $true
Check 'cold qualifier refuses changed immutable derivative bytes' {$p=Join-Path $saves $descriptor.fileName;$old=[IO.File]::ReadAllBytes($p);try{[IO.File]::WriteAllBytes($p,($old+[byte[]]@(1)));Assert-KmcOwnedSchema2ColdSource $request $source $artifact}finally{[IO.File]::WriteAllBytes($p,$old)}} $true
Check 'cold qualifier refuses a foreign original current source claim' {$bad=Copy-Value $artifact;$bad.observations.schema2Migration.originalNativeArchiveSha256='e'*64;Assert-KmcOwnedSchema2ColdSource $request $source $bad} $true
Check 'cold qualifier refuses projected pending-state drift' {$bad=Copy-Value $artifact;$bad.observations.schema2Migration.nativeInputMetadata.Combat.Paired.PendingSplitRound++;Assert-KmcOwnedSchema2ColdSource $request $source $bad} $true
$clock.Stop();$summary=[ordered]@{passed=@($results|Where-Object pass -EQ $true).Count;failed=@($results|Where-Object pass -EQ $false).Count;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;scope='actual schema2 projection, reader, held-source metadata copy and lock-owned provenance behavior over generated files; completed source/lock/process boundaries are facades';results=$results.ToArray()}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($summary|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('RESULT '+$summary.passed+' PASS / '+$summary.failed+' FAIL');if($summary.failed){exit 1}
