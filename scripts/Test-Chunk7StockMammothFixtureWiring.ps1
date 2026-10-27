[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
$analysis=[IO.Path]::GetFullPath('C:/Dev/KingmakerMountedCombatLab/analysis-cache')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/stock-mammoth-wiring-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'A fresh private stock Mammoth test directory is required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7StockMammothFixtureFixtures.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeArtifactManifestEvidence.ps1')
$script:pass=0;$script:fail=0
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$caught=$false;try{&$Body}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
$case='chunk7-stock-mammoth-fixture-preparation';$selection=Join-Path $OutputDirectory 'selection.json'
[IO.File]::WriteAllText($selection,((Get-KmcChunk7StockMammothFixturePins)|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$bound=@{Scenario=$case;BaseGameOnly=$true;TimeoutSeconds=1200;StockMammothFixtureSelectionPath=$selection}
Good 'exact base-only stock Mammoth launch' {Assert-KmcChunk7StockMammothFixtureLaunchArguments $bound $case $true 1200 $selection}
Bad 'installed-mod stock Mammoth launch' {Assert-KmcChunk7StockMammothFixtureLaunchArguments $bound $case $false 1200 $selection}
foreach($seconds in @(360,900,1050,1199)){Bad ('premature outer timeout '+$seconds) {Assert-KmcChunk7StockMammothFixtureLaunchArguments $bound $case $true $seconds $selection}}
foreach($foreign in @('SmallFixtureSelectionPath','OwnedPairsSelectionPath','ObserverPackagePath','PersistenceCase','ObserveWorkingDeserialization')){Bad ('foreign launch '+$foreign) {$b=$bound.Clone();$b[$foreign]=$null;Assert-KmcChunk7StockMammothFixtureLaunchArguments $b $case $true 1200 $selection}}
Good 'exact selection file pins' {$s=Read-KmcChunk7StockMammothFixtureSelectionFile $selection 'synthetic-stock-mammoth';Assert-KmcChunk7StockMammothFixtureSelection ([pscustomobject]@{scenario=$case;runId='synthetic-stock-mammoth';stockMammothFixture=$s.selection})}
Bad 'selector under historical launcher' {Assert-KmcChunk7StockMammothFixtureLaunchArguments $bound 'fixture-intake' $true 1200 $selection}
foreach($reader in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 $path=Join-Path $RepositoryRoot ('scripts/runtime/'+$reader);$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors)
 if($errors.Count){throw 'Composed stock Mammoth reader syntax invalid.'}
 $functions=@($ast.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq'Assert-SubscenarioResults'},$true))
 if($functions.Count-ne1){throw 'Composed stock Mammoth row gate is absent.'};. ([scriptblock]::Create($functions[0].Extent.Text))
 $rows=@(Get-KmcChunk7StockMammothFixtureRows|Where-Object{$_-notin@($case,'C7-stock-mammoth-runtime','C7-stock-mammoth-interrupted')}|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})
 $result=[pscustomobject]@{scenario=$case;status='PASS';subscenarioResults=$rows;subscenarioTotal=$rows.Count;subscenarioPassCount=$rows.Count;subscenarioFailCount=0;assertionPassCount=$rows.Count;assertionFailCount=0}
 Good ($reader+' admits exact stock Mammoth rows') {Assert-SubscenarioResults $result}
 Bad ($reader+' refuses invented grant row') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults[0].name='C7-stock-mammoth-grant';Assert-SubscenarioResults $x}
 Bad ($reader+' refuses cleanup-only PASS') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults=@($x.subscenarioResults[4]);$x.subscenarioTotal=1;$x.subscenarioPassCount=1;$x.assertionPassCount=1;Assert-SubscenarioResults $x}
 Bad ($reader+' refuses missing native-load PASS') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults=@($x.subscenarioResults|Where-Object name -CNE 'C7-stock-mammoth-native-load');$x.subscenarioTotal=4;$x.subscenarioPassCount=4;$x.assertionPassCount=4;Assert-SubscenarioResults $x}
 Bad ($reader+' refuses foreign valid row in stock PASS') {$x=$result|ConvertTo-Json -Depth 8|ConvertFrom-Json;$x.subscenarioResults[1].name='C7-small-fixture-native-load';Assert-SubscenarioResults $x}
}
$original=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261017-c7b-world18-ownerseed-s4-cold/runtime-request.json')
$request=$original|ConvertTo-Json -Depth 40|ConvertFrom-Json
foreach($name in @('assistedColdInventory','observeWorkingDeserialization')){$request.PSObject.Properties.Remove($name)}
$request.runId='synthetic-stock-mammoth-public-request';$request.scenario=$case
$request.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$request.runId)
$request.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion
$request|Add-Member stockMammothFixture (Get-KmcChunk7StockMammothFixturePins)
$observedPath=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261020-c7b-world70-b52s1-small/chunk7-small-fixture-evidence.json'
if((Get-FileHash -LiteralPath $observedPath -Algorithm SHA256).Hash.ToLowerInvariant()-cne'3db387ac58bcf86339cef46823fa7a873e2d91eba34f9056e14b2494a0c780c2'){throw 'Pinned F09 native observer example changed.'}
$observed=Read-KmcJson $observedPath
$observedRequest=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$observedRequest.dllMvid=$observed.dllMvid
$actualLoad=$observed.observations.nativeLoad;$actualSave=$observed.observations.nativeSave|ConvertTo-Json -Depth 40|ConvertFrom-Json
$actualSave.contract='stock-mammoth-fixture-native-save-v1'
$actualLabels=Get-KmcAssistedColdLabels $actualLoad
$actualWorld=[pscustomobject]@{game=$actualSave.events[0].game;saveManager=$actualSave.events[0].saveManager;player=$actualSave.events[0].player}
Good 'pinned actual F09 PostLoad and zero-preparation observer shapes' {Assert-KmcAssistedColdNoPreparation $observed.observations.noPreparation $observedRequest;if($null-eq$actualLabels){throw 'Actual retained labels absent.'}}
Good 'stock save reader admits pinned actual native event sequence under synthetic stock contract' {Assert-KmcStockMammothNativeSave $actualSave $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Bad 'stock save reader refuses missing completion event' {$x=$actualSave|ConvertTo-Json -Depth 40|ConvertFrom-Json;$x.events=@($x.events[0..3]+$x.events[5]);Assert-KmcStockMammothNativeSave $x $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Bad 'stock save reader refuses fabricated worker settlement' {$x=$actualSave|ConvertTo-Json -Depth 40|ConvertFrom-Json;$x.events[4].nativeWorkerMatches=$false;Assert-KmcStockMammothNativeSave $x $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Bad 'stock save reader refuses worker aliased to Game' {$x=$actualSave|ConvertTo-Json -Depth 40|ConvertFrom-Json;$x.events[4].worker=$x.events[4].game;Assert-KmcStockMammothNativeSave $x $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Bad 'stock save reader refuses scope aliased to iterator' {$x=$actualSave|ConvertTo-Json -Depth 40|ConvertFrom-Json;$x.events[4].scope=$x.events[4].iterator;Assert-KmcStockMammothNativeSave $x $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Bad 'stock save reader refuses absent installed hook' {$x=$actualSave|ConvertTo-Json -Depth 40|ConvertFrom-Json;$x.hooks=@($x.hooks[0..1]);Assert-KmcStockMammothNativeSave $x $observedRequest $actualWorld $observed.observations.nativeWrite $actualLabels}
Good 'stock build reader binds seven native commits and sourced rank-seven upgrade' {& (Join-Path $PSScriptRoot 'Test-Chunk7StockMammothBuildProof.ps1') -RepositoryRoot $RepositoryRoot;if($LASTEXITCODE-ne0){throw 'Stock Mammoth build proof regression failed.'}}
$script:requestNumber=0
function Public-Request($Value){$script:requestNumber++;$p=Join-Path $OutputDirectory ('request-'+$script:requestNumber+'.json');[IO.File]::WriteAllText($p,($Value|ConvertTo-Json -Depth 40),[Text.UTF8Encoding]::new($false));& (Join-Path $RepositoryRoot 'scripts/runtime/Test-RuntimeRequest.ps1') -RequestPath $p}
Good 'actual external request accepts stock Mammoth clean seed' {Public-Request $request}
Bad 'external request rejects singleton-array scenario' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.scenario=@($case);Public-Request $r}
Bad 'external request rejects extra XP' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.stockMammothFixture.nativeXpAssistance++;Public-Request $r}
Bad 'external request rejects another action family' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r|Add-Member smallFixture (Get-KmcChunk7SmallFixturePins);Public-Request $r}
Bad 'external request rejects extra selection field' {$r=$request|ConvertTo-Json -Depth 40|ConvertFrom-Json;$r.stockMammothFixture|Add-Member fabricatedPrepared $true;Public-Request $r}
$manifestRoot=Join-Path $OutputDirectory 'artifacts';[void][IO.Directory]::CreateDirectory($manifestRoot)
$manifestRequest=[pscustomobject]@{runId='synthetic-stock-mammoth-manifest';scenario=$case;evidenceRoot=$manifestRoot}
$records=@();foreach($kind in @('chunk7-stock-mammoth-fixture-evidence','chunk7-stock-mammoth-fixture-cleanup-pending')){$leaf=$kind+'.json';$p=Join-Path $manifestRoot $leaf;[IO.File]::WriteAllText($p,'{"synthetic":true}',[Text.UTF8Encoding]::new($false));$records+=[pscustomobject]@{relativePath=$leaf;kind=$kind;length=[long](Get-Item -LiteralPath $p).Length;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}}
function Manifest($Rows,[bool]$Game){$m=[pscustomobject]@{schemaVersion=1;runId=$manifestRequest.runId;scenario=$case;createdAtUtc='2026-10-20T06:00:00Z';artifacts=@($Rows)};$p=Join-Path $manifestRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($p,($m|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $manifestRequest ((Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()) -GameResult:$Game}
foreach($game in @($false,$true)){Good ('manifest accepts exact stock Mammoth leaves, game='+$game) {Manifest $records $game};Bad ('manifest refuses swapped stock Mammoth kind, game='+$game) {$r=$records|ConvertTo-Json -Depth 8|ConvertFrom-Json;$r[0].kind=$r[1].kind;Manifest $r $game}}
$zipPath=Join-Path $OutputDirectory 'reference-scan.zks';Add-Type -AssemblyName System.IO.Compression
function Write-ReferenceFixture([string]$Text){$f=[IO.File]::Open($zipPath,[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);try{$z=[IO.Compression.ZipArchive]::new($f,[IO.Compression.ZipArchiveMode]::Create,$false);try{foreach($pair in @(@('header.json','{}'),@('player.json',$Text))){$e=$z.CreateEntry($pair[0]);$w=[IO.StreamWriter]::new($e.Open(),[Text.UTF8Encoding]::new($false));try{$w.Write($pair[1])}finally{$w.Dispose()}}}finally{$z.Dispose()}}finally{$f.Dispose()}}
Good 'actual ZIP reference scanner admits native stock Mammoth with no KMC Horse reference' {Write-ReferenceFixture '{"pet":"e7aa96d15a45238438ae4cfb476f6bb9"}';$scan=Get-KmcCurrentRemovalReferenceScan $zipPath;if($scan.scannedMembers-ne2-or$scan.referenceHits.Count-ne0){throw 'Unexpected KMC reference scan result.'}}
Good 'actual ZIP reference scanner reports permanent KMC Horse reference' {Write-ReferenceFixture '{"pet":"4016c7db400ab721ff125aef9e65e202"}';$scan=Get-KmcCurrentRemovalReferenceScan $zipPath;if($scan.referenceHits.Count-ne1){throw 'Reference hit was not preserved.'}}
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){exit 1}
