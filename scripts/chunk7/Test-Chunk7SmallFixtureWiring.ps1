[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$analysis='C:/Dev/KingmakerMountedCombatLab/analysis-cache'
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/f09-wiring-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith([IO.Path]::GetFullPath($analysis)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private wiring output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureFixtures.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeArtifactManifestEvidence.ps1')
$paths=@('scripts/runtime/Chunk7SmallFixtureFixtures.ps1','scripts/runtime/Chunk7SmallFixtureEvidence.ps1','scripts/runtime/Test-RuntimeGameResult.ps1','scripts/runtime/Test-RuntimeResult.ps1','scripts/runtime/Test-RuntimeRequest.ps1','scripts/runtime/ScenarioDispatchEvidence.ps1','scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1','scripts/chunk7/Test-Chunk7SmallFixtureWiring.ps1','version.json')
$paths+=@('scripts/runtime/RuntimeArtifactManifestEvidence.ps1','scripts/Test-RuntimeArtifactManifestContract.ps1')
$pins=@($paths|ForEach-Object{$p=Join-Path $RepositoryRoot $_;[pscustomobject]@{path=$p;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}})
$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$caught=$false;try{&$Body}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
$case='chunk7-small-fixture-preparation';$selection='C:/Dev/KingmakerMountedCombatLab/analysis-cache/f09-explicit-selection.json'
$bound=@{Scenario=$case;BaseGameOnly=$true;TimeoutSeconds=900;SmallFixtureSelectionPath=$selection}
Good 'closed KMC-alone launch with cleanup deadlines inside outer 900' {Assert-KmcChunk7SmallFixtureLaunchArguments $bound $case $true 900 $selection}
Bad 'installed-clone launch' {Assert-KmcChunk7SmallFixtureLaunchArguments $bound $case $false 900 $selection}
foreach($seconds in @(360,720,780,899)){Bad ('premature outer timeout '+$seconds) {Assert-KmcChunk7SmallFixtureLaunchArguments $bound $case $true $seconds $selection}}
Bad 'missing explicit selection' {Assert-KmcChunk7SmallFixtureLaunchArguments $bound $case $true 900 ''}
foreach($foreign in @('OwnedPairsSelectionPath','ProgressionSelectionPath','ObserverPackagePath','PersistenceCase','PersistenceSourceRunId','PersistenceAreaEnterPoint','ObserveWorkingDeserialization')){Bad ('foreign launch flag '+$foreign) {$b=$bound.Clone();$b[$foreign]=$null;Assert-KmcChunk7SmallFixtureLaunchArguments $b $case $true 900 $selection}}
Good 'unselected historical launcher remains unchanged' {Assert-KmcChunk7SmallFixtureLaunchArguments @{} 'fixture-intake' $false 360 ''}
Bad 'F09 selector on historical launcher' {Assert-KmcChunk7SmallFixtureLaunchArguments $bound 'fixture-intake' $true 900 $selection}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 $path=Join-Path $RepositoryRoot ('scripts/runtime/'+$leaf);$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors)
 if($errors.Count){throw 'Composed reader parse failed.'}
 $functions=@($ast.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq'Assert-SubscenarioResults'},$true));if($functions.Count-ne1){throw 'Exact composed row function absent.'}
 . ([scriptblock]::Create($functions[0].Extent.Text))
 $rows=@(Get-KmcChunk7SmallFixtureRows|Where-Object{$_-notin@($case,'C7-small-fixture-runtime','C7-small-fixture-interrupted')}|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})
 $result=[pscustomobject]@{scenario=$case;subscenarioResults=$rows;subscenarioTotal=$rows.Count;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;assertionPassCount=$rows.Count;assertionFailCount=0}
 Good ($leaf+' actual composed F09 row admission') {Assert-SubscenarioResults $result}
 Bad ($leaf+' unknown row') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults[0].name='C7-small-fixture-grant';Assert-SubscenarioResults $x}
 Bad ($leaf+' duplicate row') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults[0].name=$x.subscenarioResults[1].name;Assert-SubscenarioResults $x}
 Bad ($leaf+' mismatched totals') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioPassCount++;Assert-SubscenarioResults $x}
}
$manifestRoot=Join-Path $OutputDirectory 'private-artifacts';[void][IO.Directory]::CreateDirectory($manifestRoot)
$manifestRequest=[pscustomobject]@{runId='synthetic-f09-manifest';scenario=$case;evidenceRoot=$manifestRoot}
$script:records=@();foreach($kind in @('chunk7-small-fixture-evidence','chunk7-small-fixture-cleanup-pending')){
 $leaf=$kind+'.json';$p=Join-Path $manifestRoot $leaf;[IO.File]::WriteAllText($p,'{"synthetic":true}',[Text.UTF8Encoding]::new($false))
 $script:records+=[pscustomobject]@{relativePath=$leaf;kind=$kind;length=[long](Get-Item -LiteralPath $p).Length;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}
}
function Private-Manifest($Records,[bool]$Game){$m=[pscustomobject]@{schemaVersion=1;runId=$manifestRequest.runId;scenario=$case;createdAtUtc='2026-10-19T12:00:00Z';artifacts=@($Records)};$p=Join-Path $manifestRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($p,($m|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $manifestRequest ((Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()) -GameResult:$Game}
foreach($game in @($false,$true)){
 Good ('actual shared manifest accepts both exact F09 leaves, game='+$game) {Private-Manifest $script:records $game}
 Bad ('actual shared manifest refuses swapped kinds, game='+$game) {$r=$script:records|ConvertTo-Json -Depth 8|ConvertFrom-Json;$r[0].kind=$r[1].kind;Private-Manifest $r $game}
 Bad ('actual shared manifest refuses unknown F09 leaf, game='+$game) {$r=$script:records|ConvertTo-Json -Depth 8|ConvertFrom-Json;$r[0].relativePath='chunk7-small-fixture-grant.json';Private-Manifest $r $game}
 Bad ('actual shared manifest refuses F09 hash substitution, game='+$game) {$r=$script:records|ConvertTo-Json -Depth 8|ConvertFrom-Json;$r[0].sha256='f'*64;Private-Manifest $r $game}
 Bad ('actual shared manifest refuses duplicate F09 record, game='+$game) {Private-Manifest @($script:records[0],$script:records[0]) $game}
}
$original=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261017-c7b-world18-ownerseed-s4-cold/runtime-request.json')
$request=$original|ConvertTo-Json -Depth 40|ConvertFrom-Json
foreach($name in @('assistedColdInventory','observeWorkingDeserialization')){$request.PSObject.Properties.Remove($name)}
$request.runId='synthetic-f09-public-request';$request.scenario=$case;$request.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$request.runId)
$request.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion
$request|Add-Member smallFixture (Get-KmcChunk7SmallFixturePins)
$script:requestNumber=0
function Public-Request($Value){$script:requestNumber++;$p=Join-Path $OutputDirectory ('request-'+$script:requestNumber+'.json');[IO.File]::WriteAllText($p,($Value|ConvertTo-Json -Depth 40),[Text.UTF8Encoding]::new($false));& (Join-Path $RepositoryRoot 'scripts/runtime/Test-RuntimeRequest.ps1') -RequestPath $p}
Good 'actual external request validator accepts closed F09 seed input' {Public-Request $request}
Good 'actual external request validator accepts distinct paid Medium case' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.smallFixture.case='owner-seed-default-human-horse-manual';Public-Request $r}
Bad 'actual external request rejects unknown paid body case' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.smallFixture.case='owner-seed-unknown-body-manual';Public-Request $r}
Bad 'actual external request rejects installed clone' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.smallFixture.baseGameOnly=$false;Public-Request $r}
Bad 'actual external request rejects foreign selection' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r|Add-Member ownedPairs ([pscustomobject]@{});Public-Request $r}
Bad 'actual external request rejects extra selection property' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.smallFixture|Add-Member moneyGrant 1;Public-Request $r}
$unchanged=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false;$script:fail++}}
$watch.Stop();[ordered]@{status=$(if($script:fail){'FAIL'}else{'PASS'});pass=$script:pass;fail=$script:fail;elapsedSeconds=$watch.Elapsed.TotalSeconds;inputsUnchanged=$unchanged;inputs=$pins;nativeInvoked=$false;
 limits='Actual public request script, actual composed game/result row functions and actual closed launcher helper. No full outer result/filesystem restoration simulation, product compile, installed hook installation or native fixture qualification.'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){exit 1}
