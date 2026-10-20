[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-wiring-'+[Guid]::NewGuid().ToString('N'))}
$wiringRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$wiringOutput=[IO.Path]::GetFullPath($OutputDirectory);[void][IO.Directory]::CreateDirectory($wiringOutput)
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $wiringRuntime -FixturesOnly
. (Join-Path $wiringRuntime 'Chunk7OrdinaryFieldEvidence.ps1')
. (Join-Path $wiringRuntime 'ScenarioDispatchEvidence.ps1')
. (Join-Path $wiringRuntime 'RuntimeArtifactManifestEvidence.ps1')
. (Join-Path $wiringRuntime 'Chunk7CleanupEvidence.ps1')
function Ast([string]$Path){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $ast}
function Exact-Function($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Exact function missing: '+$Name)};$v[0].Extent.Text}
function Commands($Ast,[string]$Name){@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}
$script:passes=0
foreach($file in Get-ChildItem -LiteralPath $wiringRuntime -File -Filter '*.ps1'){Good ('parse '+$file.Name) {[void](Ast $file.FullName)}}
. ([scriptblock]::Create((Exact-Function (Ast (Join-Path $wiringRuntime 'RuntimeHarness.Common.ps1')) 'Get-KmcSaveBackedRuntimeScenarios')))
Good 'progression admission registered once in exact save-backed and compound lists' {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-native-progression'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-native-progression')){throw 'Progression family missing or duplicated.'}}
foreach($name in @('chunk7-progression-any','CHUNK7-NATIVE-PROGRESSION')){Good ('closed family refuses '+$name){if(Test-KmcChunk7ProgressionScenario $name){throw 'Inexact family admitted.'}}}
$metadata=Get-Command (Join-Path $wiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1')
Good 'explicit progression selection runner parameter and exact enum value' {if(-not$metadata.Parameters.ContainsKey('ProgressionSelectionPath')-or'chunk7-native-progression'-cnotin@($metadata.Parameters['Scenario'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Progression runner admission absent.'}}
$body=[IO.File]::ReadAllText((Join-Path $wiringRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed.'}
$validator=[scriptblock]::Create($body.Replace($import,'# Existing Common loaded by caller; exact validator body follows.'))
function Descriptor([string]$Name,[string]$Hash){[pscustomobject]@{internalName=$Name;fileName='Manual_1_'+$Name+'.zks';sha256=$Hash;length=1024L;lastWriteTimeUtcTicks=1L;gameId='88888888-1111-4222-8333-aaaaaaaaaaaa';gameName='Synthetic';area='3'*32}}
function Request([string]$Case='horse-native-level') {
    $q=(New-ProgressionFixture $Case).request;$q.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion
    $q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId)
    $b=Descriptor 'KMC_AUTOMATION_BASELINE' ('4'*64);$w=Descriptor 'KMC_AUTOMATION_WORKING' $q.fixture.working.sha256
    $q.fixture=[pscustomobject]@{baseline=$b;working=$w;writeAuthorization=[pscustomobject]@{mode='working-only';allowedInternalName=$w.internalName;allowedFileName=$w.fileName;baselineImmutable=$true}}
    $q|Add-Member -NotePropertyName transactionToken -NotePropertyValue ('6'*64)
    $q|Add-Member -NotePropertyName qualificationSuite -NotePropertyValue ([pscustomobject]@{suiteId='synthetic-progression-suite';snapshotSha256='7'*64});return $q
}
function Validate($Q){$path=Join-Path $wiringOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
foreach($case in @(Get-KmcChunk7ProgressionCases|Where-Object{-not(Test-KmcChunk7SmallProgressionCase $_)})){Good ('full actual progression request validator accepts '+$case) {Validate (Request $case)}}
# The runner holds its request as an ordered dictionary until serialization; the progression selection assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
Good 'progression selection assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7ProgressionSelection (ConvertTo-RunnerRequest (Request))}
Bad 'ordered-dictionary request without its selection' {$r=ConvertTo-RunnerRequest (Request);$r.Remove('progression');Assert-KmcChunk7ProgressionSelection $r}
Bad 'ordered-dictionary foreign family carrying a selection' {$r=ConvertTo-RunnerRequest (Request);$r['scenario']='fixture-intake';Assert-KmcChunk7ProgressionSelection $r}
$cases=[ordered]@{
 'missing field selection'={param($q)$q.PSObject.Properties.Remove('progression')}
 'missing source archive'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'historical family borrowing selection'={param($q)$q.scenario='persistence-p05-load'}
 'unexpected native write case'={param($q)$q|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-auto'}
 'extra source permission'={param($q)$q.progression|Add-Member -NotePropertyName nativeWrites -NotePropertyValue 1}
 'extra remote actor'={param($q)$q.progression|Add-Member -NotePropertyName secondActorId -NotePropertyValue $q.progression.actorId}
 'wrong native leaf'={param($q)$q.persistenceLoad.fileName='Auto_1.zks'}
 'wrong source bytes'={param($q)$q.persistenceLoad.sha256='9'*64}
 'source Working alias'={param($q)$q.persistenceLoad.sha256=$q.fixture.working.sha256;$q.progression.sourceArchiveSha256=$q.fixture.working.sha256}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($case in $cases.GetEnumerator()){Bad ('full request '+$case.Key){$q=Request;&$case.Value $q;Validate $q}}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
    . ([scriptblock]::Create((Exact-Function (Ast (Join-Path $wiringRuntime $leaf)) 'Assert-SubscenarioResults')))
    $names=@('C7-progression-cold-source','C7-progression-native-operation','C7-progression-cleanup')
    $v=[pscustomobject]@{scenario='chunk7-native-progression';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0}
    Good ($leaf+' exact progression rows'){Assert-SubscenarioResults $v}
    Bad ($leaf+' foreign manufactured row'){$q=Copy-O $v;$q.subscenarioResults[0].name='C7-progression-manufactured';Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $wiringRuntime 'Test-RuntimeGameResult.ps1')
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){
    $assign=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($assign.Count-ne1){throw 'Exact native quota boundary differs.'}
    $quota=[scriptblock]::Create($assign[0].Extent.Text+"`nreturn `$"+$name);$game=[pscustomobject]@{scenario='chunk7-native-progression'};$request=Request;$assistedFixture=$false;$assistedWriteCount=0;$alternating=$false;$campaignB=$false;$absentKmc=$false;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $game.scenario
    $wantedQuota=if($name-ceq'expectedNativeWrites'){1}else{1}
    Good ('progression actual native quota '+$name){if((&$quota)-ne$wantedQuota){throw 'Progression request quota changed.'}}
    $request=Request 'horse-native-dismiss';$wantedQuota=if($name-ceq'expectedNativeWrites'){0}else{1}
    Good ('dismissal actual native quota '+$name){if((&$quota)-ne$wantedQuota){throw 'Dismissal request quota changed.'}}
}
$runnerAst=Ast (Join-Path $wiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1');$sources=@(Commands $runnerAst 'Get-KmcChunk7ProgressionSource');$copies=@(Commands $runnerAst 'Copy-KmcChunk7ProgressionInputs')
$lock=@(Commands $runnerAst 'Open-KmcRuntimeLock');$snapshot=@(Commands $runnerAst 'New-KmcPersistenceProfileSnapshot');$entry=@(Commands $runnerAst 'Enter-KmcWorkingSaveTransaction');$stage=@(Commands $runnerAst 'Enter-KmcModsTransaction')
$approval=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
Good 'source proven before approval and again under lock before snapshot then rechecked after native' {
    if($sources.Count-ne3-or$approval.Count-ne1-or$lock.Count-ne1-or$snapshot.Count-ne1-or$stage.Count-ne1-or
       $sources[0].Extent.StartOffset-ge$approval[0].Extent.StartOffset-or$approval[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or
       $lock[0].Extent.StartOffset-ge$sources[1].Extent.StartOffset-or$sources[1].Extent.StartOffset-ge$snapshot[0].Extent.StartOffset-or
       $stage[0].Extent.StartOffset-ge$sources[2].Extent.StartOffset){throw 'Progression source transaction ordering differs.'}
}
Good 'immutable source copy remains inside owned Working transaction before Mods staging' {
    if($copies.Count-ne1-or$entry.Count-ne1-or$entry[0].Extent.StartOffset-ge$copies[0].Extent.StartOffset-or$copies[0].Extent.StartOffset-ge$stage[0].Extent.StartOffset){throw 'Progression immutable copy escaped owned transaction.'}
}
function ManifestFixture([bool]$Pending=$false){
    $f=New-ProgressionFixture;$f.request.evidenceRoot=Join-Path $wiringOutput ('manifest-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
    $r=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($f.request.evidenceRoot))) ('runtime-staging\persistence-'+$f.request.runId+'\Saved Games')
    $f.artifact.observations.authorityBefore.root=$r;$f.artifact.observations.cleanup.authority.root=$r
    $f.artifact.observations.session.nativeWrite.path=Join-Path $r 'Auto_1.zks'
    $kind='chunk7-progression-evidence'
    if($Pending){$kind='chunk7-progression-cleanup-pending';$f.artifact.evidenceKind=$kind;$f.artifact.status='FAIL';$f.artifact.errors=@('native UI retained');$f.artifact.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true}}
    $path=Join-Path $f.request.evidenceRoot ($kind+'.json');[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $f.artifact),[Text.UTF8Encoding]::new($false))
    $m=[pscustomobject]@{schemaVersion=1;runId=$f.request.runId;scenario=$f.request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$kind+'.json';kind=$kind;length=[long](Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})}
    $f|Add-Member -NotePropertyName manifest -NotePropertyValue $m;return $f
}
function Manifest($F){$path=Join-Path $F.request.evidenceRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $F.manifest),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $F.request (Get-KmcSha256 $path) -GameResult}
Good 'exact progression native artifact registered with strict hashed envelope' {$f=ManifestFixture;Manifest $f;Assert-KmcChunk7ProgressionArtifact $f.request $f.manifest 'PASS'}
Bad 'progression artifact wrong registered kind' {$f=ManifestFixture;$f.manifest.artifacts[0].kind='chunk7-ordinary-acquisition-evidence';Manifest $f}
Bad 'progression artifact under arbitrary leaf' {$f=ManifestFixture;$f.manifest.artifacts[0].relativePath='field-any.json';Manifest $f}
Bad 'progression artifact belongs to foreign scenario' {$f=ManifestFixture;$f.request.scenario='chunk7-ordinary-acquisition';Assert-KmcChunk7ProgressionArtifact $f.request $f.manifest}
Good 'retained progression custody receipt is admitted only as FAIL' {$f=ManifestFixture $true;Manifest $f;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Bad 'retained progression custody cannot be promoted to PASS' {$f=ManifestFixture $true;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'PASS'}
Bad 'foreign scenario cannot borrow progression custody receipt' {$f=ManifestFixture $true;$f.request.scenario='chunk7-two-pair-exploration';Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; exact AST/request/reader/quota only; no guarded runner invocation')
