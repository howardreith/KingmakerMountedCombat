param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureSourceScript,[string]$MigrationFixturePath,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$BaselineOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$repo=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
if(-not$FixtureSourceScript){$FixtureSourceScript=Join-Path $repo 'scripts/chunk7/Test-Chunk7OwnedPairsEvidence.ps1'}
if(-not$MigrationFixturePath){$MigrationFixturePath=Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1'}
$analysis=[IO.Path]::GetFullPath((Join-Path $repo '../../analysis-cache')).TrimEnd('\')+'\'
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$output.StartsWith($analysis,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $output)){throw 'Fresh bounded analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($output)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
$ast=[Management.Automation.Language.Parser]::ParseFile($FixtureSourceScript,[ref]$null,[ref]$null)
# Load only actual top-level fixture functions, never the enclosing test walk.
foreach($definition in @($ast.EndBlock.Statements|Where-Object{$_-is[Management.Automation.Language.FunctionDefinitionAst]})){
 . ([scriptblock]::Create($definition.Extent.Text))
}
$fixtures=(Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json')).fixtures
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
. $MigrationFixturePath
$results=[Collections.Generic.List[object]]::new();$watch=[Diagnostics.Stopwatch]::StartNew()
function CheckFocused([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){
 $ok=$false;$errorMessage=$null
 try{&$Body;$ok=-not$Refuse}catch{$errorMessage=$_.Exception.Message;$ok=$Refuse-and(-not$Pattern-or$errorMessage-match$Pattern)}
 $results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorMessage})
 Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$errorMessage}else{''}))
}
CheckFocused 'full selected schema2 historical cold/resave artifact accepts its actual diagnostic shape' {Invoke-OwnedReader (New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual')}
if(-not$BaselineOnly){
 CheckFocused 'five ordered records bind original and freshly written native archives and actor rows' {
  $v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$o=$v.artifact.observations
  Invoke-OwnedReader $v
  if($o.combatDebtChecks.Count-ne5-or$o.coldRestoration.hooks.Count-ne8-or$o.coldRestoration.debtDiagnostics.events.Count-ne0){throw 'Generated fixture invented native calls or lost selected observer shape.'}
  for($j=0;$j-lt5;$j++){$a=if($j-eq4){$o.nativeWrite.archive}else{$o.loadedArchive};$record=$o.combatDebtChecks[$j]
   if($record.savedGameTicks-ne$a.GameTimeTicks-or$record.actors.Count-ne$a.Combat.Actors.Count-or$record.actorStates.Count-ne$a.Combat.Actors.Count){throw 'Generated record lost complete archive attribution.'}
  }
 }
 foreach($fault in @('missing-before-bind','extra-record','missing-hook','dropped-window','changed-request','wrong-baseline','refunded-debt','missing-field','reordered-actors','foreign-state')){
  CheckFocused ('full selected owner artifact refuses '+$fault) {
   $v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$o=$v.artifact.observations;$r=$o.combatDebtChecks[0]
   switch($fault){
    'missing-before-bind'{$o.combatDebtChecks=@($o.combatDebtChecks|Select-Object -Skip 1)}
    'extra-record'{$o.combatDebtChecks+=@(Copy-Value $r)}
    'missing-hook'{$o.coldRestoration.hooks=@($o.coldRestoration.hooks|Select-Object -First 7)}
    'dropped-window'{$o.coldRestoration.debtDiagnostics.dropped=1}
    'changed-request'{$o.coldRestoration.debtDiagnostics.requestUnchanged=$false}
    'wrong-baseline'{$r.actors[0].saved.Standard=5;$r.actors[0].fields[0].saved=5}
    'refunded-debt'{$r.actors[0].actual.Standard=0;$r.actors[0].fields[0].actual=0}
    'missing-field'{$r.actors[0].fields=@($r.actors[0].fields|Select-Object -First 6)}
    'reordered-actors'{$first=$r.actors[0];$r.actors[0]=$r.actors[1];$r.actors[1]=$first}
    'foreign-state'{$r.actorStates[0].actorId='foreign'}
   }
   Invoke-OwnedReader $v
  } $true
 }
 CheckFocused 'current schema2 child is a separate six-hook process without inherited historical diagnostics' {
  $parent=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual'
  $child=New-KmcOwnerSeedCurrentFixture 'owned-schema2-pending-current-manual';$o=$child.artifact.observations
  Invoke-OwnedReader $child
  if($o.coldRestoration.hooks.Count-ne6-or$null-ne$o.PSObject.Properties['combatDebtChecks']-or$null-ne$o.coldRestoration.PSObject.Properties['debtDiagnostics']-or$parent.artifact.observations.coldRestoration.hooks.Count-ne8){throw 'Fresh current child borrowed or removed parent observation authority.'}
 }
 foreach($borrow in @('combatDebtChecks','debtDiagnostics')){
  CheckFocused ('current schema2 child refuses borrowed '+$borrow) {
   $parent=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$child=New-KmcOwnerSeedCurrentFixture 'owned-schema2-pending-current-manual'
   if($borrow-ceq'combatDebtChecks'){$child.artifact.observations|Add-Member combatDebtChecks (Copy-Value $parent.artifact.observations.combatDebtChecks)}else{$child.artifact.observations.coldRestoration|Add-Member debtDiagnostics (Copy-Value $parent.artifact.observations.coldRestoration.debtDiagnostics)}
   Invoke-OwnedReader $child
  } $true
 }
 CheckFocused 'generic historical cold fixture preserves its prior six-hook no-diagnostic proof' {Invoke-OwnedReader (Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-legacy-mounted-pending-tb-manual')[0])}
 CheckFocused 'fixture diagnostic helper refuses an unselected generic historical request' {$v=Copy-Value @($fixtures|Where-Object name -CEQ 'persistence-load-legacy-mounted-pending-tb-manual')[0];Set-KmcOwnerSeedSchema2DebtDiagnosticFixture $v} $true 'exact fresh'
 CheckFocused 'fixture diagnostic helper refuses duplicated observer installation' {$v=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';Set-KmcOwnerSeedSchema2DebtDiagnosticFixture $v} $true 'exact fresh'
}
$watch.Stop();$failed=@($results|Where-Object{-not$_.pass}).Count
$receipt=[ordered]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$results.Count-$failed;failed=$failed;seconds=$watch.Elapsed.TotalSeconds;baselineOnly=[bool]$BaselineOnly;results=$results.ToArray();inputs=@($FixtureSourceScript,$MigrationFixturePath,(Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'),(Join-Path $ReaderDirectory 'Chunk7CombatDebtDiagnosticsEvidence.ps1'))|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}};scope='Actual complete owner-seed artifact reader with generated fixtures and actual fixture builder definitions. No filesystem source-lineage qualification, native behavior or restored-state credit.';nativeInvoked=$false;buildInvoked=$false}
[IO.File]::WriteAllText((Join-Path $output 'receipt.json'),($receipt|ConvertTo-Json -Depth 14),[Text.UTF8Encoding]::new($false))
'PASS='+$receipt.passed+' FAIL='+$failed
if($failed){exit 1}
