[CmdletBinding()]
param([string]$OutputDirectory)
# The unattended six-pair FAIL exit's terminal result through the complete,
# unchanged Test-RuntimeGameResult.ps1 and the runner's base-game manifest check.
# Each lineage's real native PASS evidence is transformed exactly as the engine's
# RetainCleanup and the host's CreateRetainedSixPairFailure would leave the
# evidence root; every mirrored statement is pinned to the product source first.
# Fixtures live in one run-scoped runtime-evidence/harness-test-sixexit-* root (the
# harness's own pattern) and are removed afterwards. Offline; no native process.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-exit-reader-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair exit reader checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior six-pair exit reader evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$evidenceParent=[IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'runtime-evidence'))
$reader=Join-Path $repo 'scripts/runtime/Test-RuntimeGameResult.ps1'
$fingerprint=Join-Path $repo 'planning/ENVIRONMENT-FINGERPRINT.json'
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
# A refusal counts only with the reader's own expected message, never an unrelated error.
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){
 if($Refuse-and-not$Pattern){throw ('Refusal check lacks its expected message: '+$Name)}
 $errorText=$null;$ok=$false
 try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and$errorText-cmatch$Pattern}
 $results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText})
 Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$errorText}elseif($Refuse){' (refused: '+$errorText+')'}else{''}))
}

# --- Mirrored product statements -------------------------------------------------
$sources=[ordered]@{
 exit='src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.SixPairFailure.cs'
 host='src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
 world='src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs'
 persistence='src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.cs'
 writer='src/KingmakerMountedCombat/Diagnostics/RuntimeLoaderInventory.cs'
}
$text=@{};foreach($k in $sources.Keys){$text[$k]=[IO.File]::ReadAllText((Join-Path $repo $sources[$k]),[Text.UTF8Encoding]::new($false))}
function Region([string]$Text,[string]$Start,[string]$End){$a=$Text.IndexOf($Start,[StringComparison]::Ordinal);if($a-lt0-or$Text.IndexOf($Start,$a+1,[StringComparison]::Ordinal)-ge0){throw ('Mirrored region is absent or ambiguous: '+$Start)};$b=$Text.IndexOf($End,$a,[StringComparison]::Ordinal);if($b-lt0){throw ('Mirrored region end absent: '+$End)};$Text.Substring($a,$b-$a)}
$bootstrap=Region $text.host 'private static RuntimeGameResultV2 CreateBootstrapFailureV2(' 'private void TryCompleteFailure('
$retainWorld=Region $text.world 'private void RetainCleanup(string reason)' 'internal void RequireRetainedSixFailureScope()'
$retainPersistence=Region $text.persistence 'private void RetainCleanup(string message, double deadline)' 'private bool PerformCleanup()'
$publisher=Region $text.host 'private static string PublishRuntimeArtifactManifest(RuntimeRequest request)' 'private static void AddRuntimeArtifactIfPresent('
$pins=@(
 @('exit','harmony, DateTimeOffset.UtcNow, stress ? "C7-world-cleanup" : "C7-persistence-cleanup", exitReason);'),
 @('exit','result.StartupTrace = startupTrace.Capture();'),@('exit','result.StartedAtUtc = startedAt.ToString("o");'),
 @('exit','result.SaveRequestCount = saveRequestCount;'),@('exit','result.LoadRequestCount = loadRequestCount;'),
 @('exit','try { result.RelationshipState = relationshipStateProvider() ?? "Unavailable"; } catch { result.RelationshipState = "Unavailable"; }'),
 @('exit','result.Errors = engineErrors.Concat(chunk7CompletionErrors ?? new string[0]).Concat(scenarioEngineErrors)'),
 @('exit','.Distinct(StringComparer.Ordinal).Concat(new[] { exitReason }).ToArray();'),
 @('exit','var exitReason = sixPair ? reason : twoPairReason;'),
 @('exit','result.SubscenarioResults = engineResults.ToArray();'),
 @('exit','result.SubscenarioPassCount = result.SubscenarioResults.Count(row => row.Status == "PASS");'),
 @('exit','result.AssertionFailCount = result.SubscenarioResults.Sum(row => row.AssertionFailCount);'),
 @('exit','result.EvidenceManifestSha256 = PublishRuntimeArtifactManifest();'),
 @('exit','try { WriteJsonAtomic(resultPath, result); }'),
 @('bootstrap','Status = "FAIL",'),@('bootstrap','CompletedAtUtc = now.ToString("o"),'),@('bootstrap','Fixture = request.Fixture,'),
 @('retainWorld','Fail("C7-world-cleanup", reason + " Fifteen-second cleanup budget expired; custody remains retained for host polling.");'),
 @('retainWorld','evidence["cleanupPendingAtHostBoundary"] = true;'),@('retainWorld','evidence["pendingCleanupReason"] = reason;'),
 @('retainWorld','evidence["combatOrPersistenceProof"] = false;'),
 @('retainWorld','RuntimeLoaderInventory.WriteArtifact(request, CleanupPendingFileName, "chunk7-world-cleanup-pending", "FAIL", evidence, errors);'),
 @('world','RetainCleanup("A native execution process, pair or preparation still owns cleanup.");'),
 @('retainPersistence','if (newFailure) Fail("C7-persistence-cleanup", message);'),
 @('retainPersistence','pendingHostCustody = true; evidence["cleanupPendingAtHostBoundary"] = true;'),
 @('retainPersistence','"chunk7-persistence-cleanup-pending", "FAIL", evidence, errors);'),
 @('persistence','RetainCleanup("Native commands, pair cleanup or owned progression facts have not settled.", 45);'),
 @('writer','["dllMvid"] = assembly.ManifestModule.ModuleVersionId.ToString(), ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),'),
 @('writer','["status"] = status, ["observations"] = observations, ["errors"] = new JArray(errors) };')
)
# Every exit assignment and bootstrap initializer the fixture inherits from the
# native result: dropping or changing one would leave a bootstrap default behind.
foreach($line in @('result.FrameCount = frameCount;','result.ElapsedSeconds = elapsedSeconds;','result.FixtureIdentityVerified = fixtureIdentityVerified;',
 'result.BaselineLoadRequestCount = saveAuthorization.BaselineLoadRequestCount;','result.WorkingLoadRequestCount = saveAuthorization.AuthorizedLoadCount;',
 'result.WorkingSaveRequestCount = saveAuthorization.AuthorizedWriteCount;','result.SuppressedWorkingSaveRequestCount = saveAuthorization.SuppressedWorkingWriteCount;',
 'result.UnauthorizedLoadRequestCount = saveAuthorization.UnauthorizedLoadCount;','result.UnauthorizedSaveRequestCount = saveAuthorization.UnauthorizedWriteCount;',
 'try { result.MovementExperimentEnabled = movementExperimentProvider(); } catch { result.MovementExperimentEnabled = false; }',
 'result.SubscenarioTotal = result.SubscenarioResults.Count;','result.SubscenarioFailCount = result.SubscenarioResults.Count(row => row.Status != "PASS");',
 'result.AssertionPassCount = result.SubscenarioResults.Sum(row => row.AssertionPassCount);')){$pins+=,@('exit',$line)}
foreach($line in @('SchemaVersion = RuntimeRequest.SaveBackedSchemaVersion,','RunId = request.RunId,','Scenario = request.Scenario,','Branch = request.Branch,',
 'Commit = request.Commit,','ProductVersion = request.ProductVersion,','DllSha256 = ComputeSha256(modAssembly.Location),',
 'DllMvid = modAssembly.ManifestModule.ModuleVersionId.ToString(),','TransactionToken = request.TransactionToken,','LoadedModId = loadedModId,',
 'GameVersion = Kingmaker.GameVersion.GetVersion(),','GameAssemblySha256 = ComputeSha256(gameAssembly.Location),',
 'GameAssemblyMvid = gameAssembly.ManifestModule.ModuleVersionId.ToString(),','UmmVersion = ummAssembly.GetName().Version.ToString(),',
 'UmmSha256 = ComputeSha256(ummAssembly.Location),','Harmony12Version = File.Exists(harmonyPath) ? AssemblyName.GetAssemblyName(harmonyPath).Version.ToString() : "Unavailable",',
 'Harmony12Sha256 = File.Exists(harmonyPath) ? ComputeSha256(harmonyPath) : new string(''0'', 64),','ProcessId = Process.GetCurrentProcess().Id,',
 'CurrentGameMode = game == null ? "Unavailable" : game.CurrentMode.ToString(),','LoadedAreaPresent = game != null && game.CurrentlyLoadedArea != null,')){$pins+=,@('bootstrap',$line)}
Check 'the bootstrap result sets no field the exit leaves unset beyond the pinned list' {
 $resultStart=$bootstrap.IndexOf('return new RuntimeGameResultV2',[StringComparison]::Ordinal);if($resultStart-lt0){throw 'Bootstrap result construction changed shape.'}
 $set=@([regex]::Matches($bootstrap.Substring($resultStart),'(?m)^\s{16}([A-Za-z0-9]+) = ')|ForEach-Object{$_.Groups[1].Value})
 $exitSet=@([regex]::Matches($text.exit,'result\.([A-Za-z0-9]+) = ')|ForEach-Object{$_.Groups[1].Value}|Select-Object -Unique)
 $expected=@('SchemaVersion','RunId','Scenario','Status','Branch','Commit','ProductVersion','DllSha256','DllMvid','TransactionToken','StartedAtUtc','CompletedAtUtc','LoadedModId','GameVersion','GameAssemblySha256','GameAssemblyMvid','UmmVersion','UmmSha256','Harmony12Version','Harmony12Sha256','RelationshipState','MovementExperimentEnabled','ProcessId','CurrentGameMode','LoadedAreaPresent','SaveRequestCount','LoadRequestCount','FrameCount','ElapsedSeconds','Errors','Fixture','FixtureIdentityVerified','BaselineLoadRequestCount','WorkingLoadRequestCount','WorkingSaveRequestCount','SuppressedWorkingSaveRequestCount','UnauthorizedLoadRequestCount','UnauthorizedSaveRequestCount','SubscenarioTotal','SubscenarioPassCount','SubscenarioFailCount','AssertionPassCount','AssertionFailCount','EvidenceManifestSha256','SubscenarioResults')
 if((@($set|Sort-Object)-join',')-cne(@($expected|Sort-Object)-join',')){throw ('Bootstrap result initializer set changed: '+($set-join','))}
 $exitExpected=@('StartupTrace','EngineLogTail','StartedAtUtc','FrameCount','ElapsedSeconds','SaveRequestCount','LoadRequestCount','FixtureIdentityVerified','BaselineLoadRequestCount','WorkingLoadRequestCount','WorkingSaveRequestCount','SuppressedWorkingSaveRequestCount','UnauthorizedLoadRequestCount','UnauthorizedSaveRequestCount','RelationshipState','MovementExperimentEnabled','Errors','SubscenarioResults','SubscenarioTotal','SubscenarioPassCount','SubscenarioFailCount','AssertionPassCount','AssertionFailCount','EvidenceManifestSha256')
 if((@($exitSet|Sort-Object)-join',')-cne(@($exitExpected|Sort-Object)-join',')){throw ('Exit result assignment set changed: '+($exitSet-join','))}
}
$regions=@{exit=$text.exit;bootstrap=$bootstrap;retainWorld=$retainWorld;retainPersistence=$retainPersistence;world=$text.world;persistence=$text.persistence;writer=$text.writer}
Check 'every statement the fixture mirrors is still the exact product source' {
 foreach($pin in $pins){if($regions[$pin[0]].IndexOf($pin[1],[StringComparison]::Ordinal)-lt0){throw ('Mirrored product statement changed in '+$pin[0]+': '+$pin[1])}}
}
Check 'the manifest publisher records the loader inventory first and each retained receipt under its own kind' {
 $inventory=$publisher.IndexOf('RuntimeLoaderInventory.EvidenceFileName, RuntimeLoaderInventory.EvidenceKind);',[StringComparison]::Ordinal)
 $worldPending=$publisher.IndexOf('"chunk7-world-cleanup-pending.json", "chunk7-world-cleanup-pending");',[StringComparison]::Ordinal)
 $persistencePending=$publisher.IndexOf('"chunk7-persistence-cleanup-pending.json", "chunk7-persistence-cleanup-pending");',[StringComparison]::Ordinal)
 if($inventory-lt0-or$worldPending-le$inventory-or$persistencePending-le$worldPending){throw 'Manifest record order or retained kinds changed.'}
}
$reasonMatch=[regex]::Match($text.exit,'const string reason = "([^"]*)" \+\s*"([^"]*)";')
if(-not$reasonMatch.Success){throw 'The exit reason constant changed shape.'}
$exitReason=$reasonMatch.Groups[1].Value+$reasonMatch.Groups[2].Value

# --- Real native inputs (read-only, pinned) ---------------------------------------
$worldReason='A native execution process, pair or preparation still owns cleanup.'
$persistenceReason='Native commands, pair cleanup or owned progression facts have not settled.'
$lineages=[ordered]@{
 stress=@{run='20261022-c7b-world190-b190s1-sixmeasure';evidence='chunk7-world-evidence.json';pending='chunk7-world-cleanup-pending.json';kind='chunk7-world-cleanup-pending';row='C7-world-cleanup'
  reason=$worldReason;engineError=($worldReason+' Fifteen-second cleanup budget expired; custody remains retained for host polling.');world=$true
  pins=[ordered]@{'runtime-request.json'='55a955e489b1539be0a6dc917020a47570a8535007b2ef4ec058b3efa61c3351';'runtime-game-result.json'='ad0dccdf0810c72d556496fafde2743b4cae9224469923e1cb7fa2c3c1935815'
   'runtime-loader-inventory.json'='77f3c9aee4f2a87e5f9301a44dd74701ad03eaa77d5b0ea87a370108df1e3a5a';'base-game-isolation-admission.json'='eed90be4454e523b4bc7ed5e18b4a5b37799d46edc6ca771c2cc624bd58f5476'
   'chunk7-world-evidence.json'='5f6fb2d29f0585503fcab7d98393addfe1a00f761520d664586c66f578d7a74e'}}
 producer=@{run='20261022-c7b-world189-b189s1-sixsave';evidence='chunk7-persistence-evidence.json';pending='chunk7-persistence-cleanup-pending.json';kind='chunk7-persistence-cleanup-pending';row='C7-persistence-cleanup'
  reason=$persistenceReason;engineError=$persistenceReason;world=$false
  pins=[ordered]@{'runtime-request.json'='aa93fa3d9d8ba798b52315d564f48af6b8a5fa2b5f021a48656529c2c738194b';'runtime-game-result.json'='d64939e224f36bb6841b9ef34c472a040ffd0f888f52275f363ea0e1f01cd8f2'
   'runtime-loader-inventory.json'='758983b5fc94708d2185ce10e7606da8b4f2f34bc432194080f92502ac074047';'base-game-isolation-admission.json'='a8fe4a1a00b542cf30374099f1805f586e8206d37ab176a45468a86bb45263c3'
   'chunk7-persistence-evidence.json'='065f37c5113d9d60dbe1f67c5b85ff977d47bbcc3f5917fbd1a2f02189a67356'}}
 cold=@{run='20261022-c7b-world189-b189s2-sixcold';evidence='chunk7-persistence-evidence.json';pending='chunk7-persistence-cleanup-pending.json';kind='chunk7-persistence-cleanup-pending';row='C7-persistence-cleanup'
  reason=$persistenceReason;engineError=$persistenceReason;world=$false
  pins=[ordered]@{'runtime-request.json'='f4b359c34b60edc7fa0152f32d1e41a669f79f8e1b25811bd2243257754929e2';'runtime-game-result.json'='3c06451c631b609ba6a4e7c8cf5e7de2149a994870bce46cc94e4df3e82ada79'
   'runtime-loader-inventory.json'='309361691a571ebb05a4ea23daad33f9ee33883f9bce347cf7560bb49e2ec015';'base-game-isolation-admission.json'='3fbfa7a402dbf6f18d1e9fdce6c7e322af943b295e0c39af46670604d7cf93d7'
   'chunk7-persistence-evidence.json'='eaaacc1b53a19e791afb26a828569316178429c0b9820e6e7d8a3f8131bf48ed'}}
}
foreach($name in $lineages.Keys){$l=$lineages[$name];$l.name=$name;$l.source=Join-Path $evidenceParent $l.run
 foreach($pin in $l.pins.GetEnumerator()){if((Get-KmcSha256 (Join-Path $l.source $pin.Key))-cne$pin.Value){throw ('Real '+$name+' native input changed: '+$pin.Key)}}}

function Read-Utf8Json([string]$Path){[IO.File]::ReadAllText($Path,[Text.UTF8Encoding]::new($false))|ConvertFrom-Json}
function Write-Utf8Json([string]$Path,$Value){
 $json=$Value|ConvertTo-Json -Depth 100 -Compress
 # Windows PowerShell stringifies anything deeper than -Depth instead of failing.
 if($json.Contains('"@{')-or$json.Contains('System.Object[]')-or$json.Contains('System.Collections')){throw ('JSON depth exceeded while writing '+$Path)}
 [IO.File]::WriteAllText($Path,$json,[Text.UTF8Encoding]::new($false))
}
function Set-Member($Object,[string]$Name,$Value){if($null-ne$Object.PSObject.Properties[$Name]){$Object.$Name=$Value}else{$Object|Add-Member -NotePropertyName $Name -NotePropertyValue $Value}}
function Row([string]$Name,[string]$Status,[int]$Pass,[int]$Fail,[string[]]$Errors){[pscustomobject][ordered]@{name=$Name;status=$Status;assertionPassCount=$Pass;assertionFailCount=$Fail;errors=@($Errors)}}
function Set-Totals($Game){
 $rows=@($Game.subscenarioResults);$Game.subscenarioTotal=$rows.Count
 $Game.subscenarioPassCount=@($rows|Where-Object{$_.status-ceq'PASS'}).Count;$Game.subscenarioFailCount=@($rows|Where-Object{$_.status-cne'PASS'}).Count
 $Game.assertionPassCount=[int](($rows|Measure-Object assertionPassCount -Sum).Sum);$Game.assertionFailCount=[int](($rows|Measure-Object assertionFailCount -Sum).Sum)
}

$fixtureRoot=Assert-KmcChildPath (Join-Path $evidenceParent ('harness-test-sixexit-'+[Guid]::NewGuid().ToString('N'))) $evidenceParent 'six-pair exit reader fixture root'
$fixtureCount=0
# One evidence root as the exit leaves it: the runner's request and admission, the
# native loader inventory, the engine's retained receipt (no completed engine
# evidence), the republished manifest and the exit's atomic FAIL result.
function New-ExitFixture($L,[scriptblock]$PendingEdit,[scriptblock]$ResultEdit,[scriptblock]$AfterEdit){
 $script:fixtureCount++
 $root=Join-Path $fixtureRoot ($script:fixtureCount.ToString('00')+'-'+$L.name);[void][IO.Directory]::CreateDirectory($root)
 $request=Read-Utf8Json (Join-Path $L.source 'runtime-request.json');$request.evidenceRoot=$root
 Write-Utf8Json (Join-Path $root 'runtime-request.json') $request
 foreach($leaf in @('runtime-loader-inventory.json','base-game-isolation-admission.json')){Copy-Item -LiteralPath (Join-Path $L.source $leaf) -Destination (Join-Path $root $leaf)}
 $source=Read-Utf8Json (Join-Path $L.source 'runtime-game-result.json')
 $exitAt=[DateTimeOffset]::Parse($source.completedAtUtc).AddSeconds(90)
 # Engine RetainCleanup: Fail(row, error), the boundary fields, one atomic receipt.
 $receipt=Read-Utf8Json (Join-Path $L.source $L.evidence)
 $receipt.evidenceKind=$L.kind;$receipt.status='FAIL';$receipt.createdAtUtc=$exitAt.AddSeconds(-30).ToString('o');$receipt.errors=@($L.engineError)
 Set-Member $receipt.observations 'cleanupPendingAtHostBoundary' $true
 if($L.world){Set-Member $receipt.observations 'pendingCleanupReason' $L.reason;Set-Member $receipt.observations 'combatOrPersistenceProof' $false}
 $context=[pscustomobject]@{lineage=$L;root=$root;request=$request;receipt=$receipt;receiptLeaf=$L.pending;receiptKind=$L.kind;game=$null}
 if($PendingEdit){&$PendingEdit $context}
 Write-Utf8Json (Join-Path $root $context.receiptLeaf) $context.receipt
 # Host PublishRuntimeArtifactManifest: every present record, loader inventory first.
 $records=@(foreach($pair in @(@('runtime-loader-inventory.json','runtime-loader-inventory'),@($context.receiptLeaf,$context.receiptKind))){
  $path=Join-Path $root $pair[0];[pscustomobject][ordered]@{relativePath=$pair[0];kind=$pair[1];length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)}})
 Write-Utf8Json (Join-Path $root 'runtime-artifacts.json') ([pscustomobject][ordered]@{schemaVersion=1;runId=$request.runId;scenario=$request.scenario;createdAtUtc=$exitAt.AddSeconds(-1).ToString('o');artifacts=$records})
 # Host CreateRetainedSixPairFailure over CreateBootstrapFailureV2.
 $result=$source;$result.status='FAIL';$result.completedAtUtc=$exitAt.ToString('o')
 $result.subscenarioResults=@(@($source.subscenarioResults|Where-Object{$_.name-cne$L.row})+@(Row $L.row 'FAIL' 0 1 @($L.engineError)))
 $result.errors=@($L.engineError,$exitReason);Set-Totals $result
 $result.evidenceManifestSha256=Get-KmcSha256 (Join-Path $root 'runtime-artifacts.json')
 $context.game=$result
 if($ResultEdit){&$ResultEdit $context}
 Write-Utf8Json (Join-Path $root 'runtime-game-result.json') $context.game
 if($AfterEdit){&$AfterEdit $context}
 $context
}
function Invoke-Reader($Context,[switch]$RequirePass){
 # Like the runner, bind the native process and its launch bound, never values read
 # back from the result under test. Not exercised here: -VerifyLiveWorkingIdentity
 # (boundary evidence only) and Assert-KmcBaseGameNativeInventory (byte-copied input).
 $native=Read-Utf8Json (Join-Path $Context.lineage.source 'runtime-game-result.json')
 $inventoryProcess=(Read-Utf8Json (Join-Path $Context.lineage.source 'runtime-loader-inventory.json')).observations.processId
 if($inventoryProcess-ne$native.processId){throw 'Real lineage loader inventory and result name different native processes.'}
 $arguments=@{GameResultPath=(Join-Path $Context.root 'runtime-game-result.json');RequestPath=(Join-Path $Context.root 'runtime-request.json');FingerprintPath=$fingerprint
  ExpectedProcessId=[int]$inventoryProcess;NotBeforeUtc=[DateTimeOffset]::Parse([string]$native.startedAtUtc)}
 $lines=@(& $reader @arguments -RequirePass:$RequirePass 6>&1|ForEach-Object{[string]$_})
 if($lines.Count-eq0-or$lines[$lines.Count-1]-cne'TOTAL PASS=34 FAIL=0'){throw ('The full reader did not complete its FAIL branch: '+($lines-join' | '))}
 $game=Read-Utf8Json (Join-Path $Context.root 'runtime-game-result.json')
 # The runner's base-game check after the reader: the exit's republished manifest is
 # the result's manifest and the loader inventory came from the same native process.
 $manifestPath=Join-Path $Context.root 'runtime-artifacts.json'
 if((Get-KmcSha256 $manifestPath)-cne$game.evidenceManifestSha256){throw 'Base-game inventory manifest is not the native result manifest.'}
 $inventory=Read-KmcChunk7ManifestArtifact $Context.request (Read-KmcJson $manifestPath) 'runtime-loader-inventory.json' 'runtime-loader-inventory'
 if($inventory.observations.processId-ne$game.processId){throw 'Loader inventory came from a different native process.'}
}

try{
 # --- Honest exits ----------------------------------------------------------------
 foreach($name in $lineages.Keys){$l=$lineages[$name]
  Check ('honest '+$name+' exit FAIL passes the complete reader and the runner base-game check'){Invoke-Reader (New-ExitFixture $l)}
 }
 Check 'honest stress exit after an earlier causal row failure passes the complete reader' {
  Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)
   $cause='Six-pair sampling lost its declared population or settled native custody.'
   foreach($row in @($c.game.subscenarioResults|Where-Object{$_.name-ceq'C7-six-sampling'})){$row.status='FAIL';$row.assertionPassCount=0;$row.assertionFailCount=1;$row.errors=@($cause)}
   $c.game.errors=@($cause,$c.lineage.engineError,$exitReason);Set-Totals $c.game})
 }
 Check 'honest stress exit with the unavailable relationship fallback passes the complete reader' {
  Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.relationshipState='Unavailable';$c.game.movementExperimentEnabled=$false})
 }
 Check 'honest stress exit with retained mounted custody passes the complete reader' {
  Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.relationshipState='Mounted';$c.game.movementExperimentEnabled=$true})
 }
 Check 'a FAIL exit result can never qualify under RequirePass' {Invoke-Reader (New-ExitFixture $lineages.stress) -RequirePass} $true 'Runtime game result did not qualify as PASS\.'

 # --- Refusals: the reader actually examines the exit's evidence ------------------
 $receiptRefusal='Retained cleanup receipt must be a versioned FAIL with nonempty errors\.'
 Check 'retained stress receipt promoted to a clean PASS is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -PendingEdit {param($c)$c.receipt.status='PASS';$c.receipt.errors=@()})} $true $receiptRefusal
 Check 'retained producer receipt promoted to a clean PASS is refused' {Invoke-Reader (New-ExitFixture $lineages.producer -PendingEdit {param($c)$c.receipt.status='PASS';$c.receipt.errors=@()})} $true $receiptRefusal
 Check 'retained receipt labelled PASS over its errors is refused by the envelope' {Invoke-Reader (New-ExitFixture $lineages.cold -PendingEdit {param($c)$c.receipt.status='PASS'})} $true 'PASS artifact contains errors\.'
 Check 'retained cold receipt without errors is refused' {Invoke-Reader (New-ExitFixture $lineages.cold -PendingEdit {param($c)$c.receipt.errors=@()})} $true $receiptRefusal
 Check 'retained receipt without host-boundary custody is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -PendingEdit {param($c)$c.receipt.observations.cleanupPendingAtHostBoundary=$false})} $true 'Native evidence boolean is missing, mistyped or differs\.'
 Check 'a foreign-family retained receipt is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -PendingEdit {param($c)$c.receiptLeaf='chunk7-persistence-cleanup-pending.json';$c.receiptKind='chunk7-persistence-cleanup-pending';$c.receipt.evidenceKind='chunk7-persistence-cleanup-pending'})} $true 'Foreign scenario supplied retained cleanup evidence\.'
 Check 'a FAIL result without structured errors is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.errors=@()})} $true 'FAIL runtime game result does not contain a structured error\.'
 Check 'a FAIL result whose rows all pass is refused' {Invoke-Reader (New-ExitFixture $lineages.producer -ResultEdit {param($c)
  foreach($row in @($c.game.subscenarioResults|Where-Object{$_.name-ceq$c.lineage.row})){$row.status='PASS';$row.assertionPassCount=1;$row.assertionFailCount=0;$row.errors=@()};Set-Totals $c.game})} $true 'FAIL runtime game result lacks a failed subscenario assertion\.'
 Check 'a PASS-labelled result without completed engine evidence is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.status='PASS';$c.game.errors=@()})} $true 'Exact native artifact manifest binding is required\.'
 Check 'mismatched totals are refused' {Invoke-Reader (New-ExitFixture $lineages.cold -ResultEdit {param($c)$c.game.subscenarioFailCount=0})} $true 'Runtime game-result subscenario totals do not match the named results\.'
 Check 'an unknown exit row is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.subscenarioResults=@(@($c.game.subscenarioResults)+@(Row 'C7-six-exit' 'FAIL' 0 1 @('exit')));Set-Totals $c.game})} $true 'Runtime game-result contains an unknown subscenario: C7-six-exit'
 Check 'a duplicated cleanup row is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -ResultEdit {param($c)$c.game.subscenarioResults=@(@($c.game.subscenarioResults)+@(Row $c.lineage.row 'FAIL' 0 1 @('again')));Set-Totals $c.game})} $true 'Runtime game-result contains a duplicate subscenario: C7-world-cleanup'
 Check 'a manifest republished after the result is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -AfterEdit {param($c)
  $path=Join-Path $c.root 'runtime-artifacts.json';$m=Read-Utf8Json $path;$m.createdAtUtc=[DateTimeOffset]::Parse($m.createdAtUtc).AddSeconds(1).ToString('o');Write-Utf8Json $path $m})} $true 'Runtime artifact manifest hash does not match the runtime game result\.'
 Check 'a retained receipt changed after the manifest is refused' {Invoke-Reader (New-ExitFixture $lineages.stress -AfterEdit {param($c)
  $path=Join-Path $c.root $c.receiptLeaf;$r=Read-Utf8Json $path;$r.errors=@('changed');Write-Utf8Json $path $r})} $true 'Runtime artifact bytes do not match the manifest: chunk7-world-cleanup-pending\.json'
 Check 'a result from another process is refused' {Invoke-Reader (New-ExitFixture $lineages.cold -ResultEdit {param($c)$c.game.processId=[int]$c.game.processId+1})} $true 'Runtime game-result process, timing, mode, or error shape is invalid\.'
}
finally{
 if(Test-Path -LiteralPath $fixtureRoot){
  $resolved=[IO.Path]::GetFullPath($fixtureRoot)
  if(-not$resolved.StartsWith($evidenceParent+'\',[StringComparison]::OrdinalIgnoreCase)-or(Split-Path -Leaf $resolved)-cnotmatch'^harness-test-sixexit-[0-9a-f]{32}$'){throw 'Six-pair exit fixture cleanup target escaped its run-scoped root.'}
  Remove-Item -LiteralPath $resolved -Recurse -Force
 }
}
$failures=@($results|Where-Object{-not$_.pass}).Count
[ordered]@{nativeInvoked=$false;fixtureRootRemoved=(-not(Test-Path -LiteralPath $fixtureRoot));checks=$results.Count;failures=$failures;elapsedSeconds=[math]::Round($clock.Elapsed.TotalSeconds,1)
 inputs=@(@($sources.Values|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 (Join-Path $repo $_))}})+@(foreach($name in $lineages.Keys){foreach($pin in $lineages[$name].pins.GetEnumerator()){[ordered]@{path=('runtime-evidence/'+$lineages[$name].run+'/'+$pin.Key);sha256=$pin.Value}}})+@([ordered]@{path='scripts/runtime/Test-RuntimeGameResult.ps1';sha256=(Get-KmcSha256 $reader)},[ordered]@{path='planning/ENVIRONMENT-FINGERPRINT.json';sha256=(Get-KmcSha256 $fingerprint)}))
 results=$results;limits='Real batch190/batch189 native PASS evidence for each six-pair lineage, transformed exactly as the pinned RetainCleanup and CreateRetainedSixPairFailure statements leave the evidence root, then read by the complete unchanged Test-RuntimeGameResult.ps1 and the runner base-game manifest check. The retained observations are the completed runs (a real retained failure stops earlier), host completion errors are modelled empty, and the persistence engine''s optional checkpoint/area captures are absent; the FAIL reader does not inspect them. No native exit was exercised.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('SIX-PAIR EXIT READER PASS='+($results.Count-$failures)+' FAIL='+$failures)
if($failures-ne0-or(Test-Path -LiteralPath $fixtureRoot)){throw 'Six-pair exit reader checks failed.'}
