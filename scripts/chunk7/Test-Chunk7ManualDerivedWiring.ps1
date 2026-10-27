[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixtureDirectory')){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-derived-wiring-'+[Guid]::NewGuid().ToString('N'))}
$bridgeRoot=[IO.Path]::GetFullPath($RepositoryRoot);$bridgeRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$bridgeFixture=[IO.Path]::GetFullPath($FixtureDirectory);$bridgeOutput=[IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($bridgeOutput)
. (Join-Path $PSScriptRoot 'Test-Chunk7ManualDerivedEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $RuntimeDirectory -FixtureDirectory $FixtureDirectory -OutputDirectory $OutputDirectory -FixturesOnly
function Get-KmcRepositoryRoot {$bridgeRoot};function Get-KmcLabRoot {[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($bridgeRoot))}
function Ast([string]$Path){$tokens=$null;$parseErrors=$null;$v=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$parseErrors);if($parseErrors){throw ($parseErrors|Out-String)};$v}
function ExactFunction($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Missing exact function '+$Name)};$v[0].Extent.Text}
function CopyValue($Value){$Value|ConvertTo-Json -Depth 64|ConvertFrom-Json}
$script:passes=0
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passes++;Write-Host ('PASS rejects '+$Name)}
function Request {$f=New-ManualDerivedFixture;$q=$f.request;$q.productVersion=(Read-KmcJson (Join-Path $bridgeRoot 'version.json')).productVersion;$q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId);$q}
$leaves=@('Invoke-KingmakerRuntimeScenario.ps1','RuntimeHarness.Common.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','RuntimeArtifactManifestEvidence.ps1','ScenarioDispatchEvidence.ps1','Chunk7CleanupEvidence.ps1','Chunk7ManualDerivedEvidence.ps1','Chunk7ManualDerivedFixtures.ps1')
foreach($leaf in $leaves){Good ('parse '+$leaf){[void](Ast (Join-Path $bridgeRuntime $leaf))}}
Good 'closed Manual-derived acquisition registered once as save-backed compound' {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-manual-derived-acquisition'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-manual-derived-acquisition')){throw 'Closed registration differs'}}
foreach($alias in @('CHUNK7-BOOTSTRAP-MANUAL-SEED','chunk7-cold-inventory','chunk7-manual-derived-acquisition-any')){Good ('inexact family unavailable '+$alias){if(Test-KmcChunk7ManualDerivedScenario $alias){throw 'Alias accepted'}}}
$body=[IO.File]::ReadAllText((Join-Path $bridgeRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed'}
$validator=[scriptblock]::Create($body.Replace($import,'# Common already loaded; actual validator body follows.'))
function Validate($Q){$path=Join-Path $bridgeOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
Good 'full request accepts exact one-Manual-load one-Manual-write family' {Validate (Request)}
# The runner holds its request as an ordered dictionary until serialization; the Manual-derived selection assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
Good 'Manual-derived selection assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7ManualDerivedSelection (ConvertTo-RunnerRequest (Request))}
Bad 'ordered-dictionary request without its selection' {$r=ConvertTo-RunnerRequest (Request);$r.Remove('manualDerived');Assert-KmcChunk7ManualDerivedSelection $r}
Bad 'ordered-dictionary foreign family carrying a selection' {$r=ConvertTo-RunnerRequest (Request);$r['scenario']='fixture-intake';Assert-KmcChunk7ManualDerivedSelection $r}
$negative=[ordered]@{
 'missing selection'={param($q)$q.PSObject.Properties.Remove('manualDerived')}
 'foreign family'={param($q)$q.scenario='fixture-intake'}
 'case alias'={param($q)$q.manualDerived.case='any-inventory'}
 'base-only false'={param($q)$q.manualDerived.baseGameOnly=$false}
 'load two'={param($q)$q.manualDerived.expectedNativeLoadCount=2}
 'load text'={param($q)$q.manualDerived.expectedNativeLoadCount='1'}
 'write zero'={param($q)$q.manualDerived.expectedNativeWriteCount=0}
 'write text'={param($q)$q.manualDerived.expectedNativeWriteCount='0'}
 'foreign native leaf'={param($q)$q.persistenceLoad.fileName='Auto_2.zks'}
 'array native leaf'={param($q)$q.persistenceLoad.fileName=@('Auto_1.zks')}
 'old Working campaign'={param($q)$q.persistenceLoad.gameId=$q.fixture.working.gameId}
 'missing source'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'extra alternate'={param($q)$q|Add-Member -NotePropertyName persistenceAlternate -NotePropertyValue $q.persistenceLoad}
 'extra persistence case'={param($q)$q|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-auto'}
 'extra route'={param($q)$q|Add-Member -NotePropertyName persistenceReplayTargets -NotePropertyValue @()}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($n in $negative.GetEnumerator()){Bad ('full request '+$n.Key){$q=Request;&$n.Value $q;Validate $q}}
Good 'historical fixture request unchanged' {$q=Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('manualDerived');$q.PSObject.Properties.Remove('persistenceLoad');Validate $q}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $bridgeRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-manual-derived-source','C7-manual-derived-native-load','C7-manual-derived-eligibility','C7-manual-derived-native-choices','C7-manual-derived-native-field','C7-manual-derived-native-manual-save','C7-manual-derived-cleanup')
 $result=[pscustomobject]@{scenario='chunk7-manual-derived-acquisition';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=7;subscenarioPassCount=7;subscenarioFailCount=0;assertionPassCount=7;assertionFailCount=0}
 Good ($leaf+' exact closed rows'){Assert-SubscenarioResults $result}
 Bad ($leaf+' arbitrary row'){$q=CopyValue $result;$q.subscenarioResults[0].name='C7-manual-derived-any';Assert-SubscenarioResults $q}
 Bad ($leaf+' duplicated row'){$q=CopyValue $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $bridgeRuntime 'Test-RuntimeGameResult.ps1')
foreach($quotaName in @('expectedNativeWrites','expectedWorkingLoads')){
 $nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$quotaName)},$true));if($nodes.Count-ne1){throw 'Exact quota assignment changed'}
 $quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$quotaName)
 foreach($family in @('chunk7-manual-derived-acquisition','fixture-intake')){
  $game=[pscustomobject]@{scenario=$family};$request=Request;$request.scenario=$family;$nativeBootstrap=$assistedFixture=$alternating=$campaignB=$absentKmc=$false;$assistedWriteCount=0
  $wanted=if($quotaName-ceq'expectedNativeWrites'-and$family-ceq'fixture-intake'){0}else{1};Good ('actual '+$family+' '+$quotaName){if((&$quota)-ne$wanted){throw 'Native quota differs'}}
 }
}
$runnerPath=Join-Path $bridgeRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runnerAst=Ast $runnerPath;$metadata=Get-Command $runnerPath
Good 'runner exact family and selection path registered' {if(-not$metadata.Parameters.ContainsKey('ManualDerivedSelectionPath')-or'chunk7-manual-derived-acquisition'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Closed runner parameter absent'}}
$selection=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7ManualDerivedScenario $Scenario'-and$n.Extent.Text.Contains('requires explicit base-only staging and exact successful Manual lineage plus observed readiness pins')},$true))
if($selection.Count-ne1){throw 'Exact explicit selection guard missing'}
function Read-KmcChunk7ManualDerivedSelectionFile($Path,$RunId){[pscustomobject]@{path=$Path;selection=@{}}}
$selectionGuard=[scriptblock]::Create('param($Family,$Bindings,$Base,$InputPath) $Scenario=$Family;$BaseGameOnly=$Base;$ManualDerivedSelectionPath=$InputPath;$RunId="cold";$PSBoundParameters=$Bindings;'+[Environment]::NewLine+$selection[0].Extent.Text)
function Selected([string]$Family,[hashtable]$Bindings,[bool]$Base=$true,[string]$InputPath='selection.json'){&$selectionGuard $Family $Bindings $Base $InputPath}
Good 'explicit base-only source selection admitted' {Selected 'chunk7-manual-derived-acquisition' @{ManualDerivedSelectionPath='selection.json'}}
Bad 'selection missing' {Selected 'chunk7-manual-derived-acquisition' @{} $true ''}
Bad 'base-only missing' {Selected 'chunk7-manual-derived-acquisition' @{ManualDerivedSelectionPath='selection.json'} $false}
Bad 'foreign selection family' {Selected 'fixture-intake' @{ManualDerivedSelectionPath='selection.json'}}
foreach($permission in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){Bad ('extra runner permission '+$permission){$b=@{ManualDerivedSelectionPath='selection.json'};$b[$permission]='foreign';Selected 'chunk7-manual-derived-acquisition' $b}}
$commands=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true))
$sourceCalls=@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcChunk7ManualDerivedSource'});$lock=@($commands|Where-Object{$_.GetCommandName()-ceq'Open-KmcRuntimeLock'});$copy=@($commands|Where-Object{$_.GetCommandName()-ceq'Copy-KmcChunk7ManualDerivedSourceArtifact'});$working=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcWorkingSaveTransaction'});$staging=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcModsTransaction'})
Good 'source requalified before locked staging and unchanged source after native run' {if($sourceCalls.Count-ne3-or$lock.Count-ne1-or$copy.Count-ne1-or$working.Count-ne1-or$staging.Count-ne1-or$selection[0].Extent.EndOffset-ge$sourceCalls[0].Extent.StartOffset-or$sourceCalls[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-le$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-ge$copy[0].Extent.StartOffset-or$copy[0].Extent.StartOffset-le$working[0].Extent.StartOffset-or$sourceCalls[2].Extent.StartOffset-le$staging[0].Extent.StartOffset){throw 'Exact guarded source ordering changed'}}
function ManifestFixture([bool]$Pending=$false){
 $f=New-ManualDerivedFixture;$f.request.evidenceRoot=Join-Path $bridgeOutput ('manifest-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
 $nativeRoot=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($f.request.evidenceRoot))) ('runtime-staging/persistence-'+$f.request.runId+'/Saved Games');$o=$f.artifact.observations
 foreach($row in @($o.beforeLoad,$o.loadedWorld,$o.settledWorld,$o.saveRequested,$o.cleanup)){$row.authority.root=$nativeRoot}
 $o.fixtureReadiness.isolatedSaveDescriptors.root=$nativeRoot;$o.nativeWrite.path=Join-Path $nativeRoot $o.nativeWrite.leaf
 foreach($event in @($o.nativeSave.events|Where-Object{$_.kind-cne'native-save-request'})){$event.path=$o.nativeWrite.path}
 $sourceDir=Join-Path $bridgeOutput $f.request.manualDerived.sourceRunId;[void][IO.Directory]::CreateDirectory($sourceDir);$sourcePath=Join-Path $sourceDir 'chunk7-manual-seed-evidence.json';[IO.File]::WriteAllText($sourcePath,(ConvertTo-KmcChunk7Canonical $f.source),[Text.UTF8Encoding]::new($false))
 $sha=Get-KmcSha256 $sourcePath;$f.request.manualDerived.sourceArtifactSha256=$sha;$o.selection.sourceArtifactSha256=$sha;$o.source.artifactSha256=$sha;$o.choices[0].nativeInventorySha256=$sha
 $kind='chunk7-manual-derived-evidence'
 if($Pending){$kind='chunk7-manual-derived-cleanup-pending';$f.artifact.evidenceKind=$kind;$f.artifact.status='FAIL';$f.artifact.errors=@('native worker retained');$f.artifact.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true}}
 $artifactPath=Join-Path $f.request.evidenceRoot ($kind+'.json');[IO.File]::WriteAllText($artifactPath,(ConvertTo-KmcChunk7Canonical $f.artifact),[Text.UTF8Encoding]::new($false))
 $m=[pscustomobject]@{schemaVersion=1;runId=$f.request.runId;scenario=$f.request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$kind+'.json';kind=$kind;length=[long](Get-Item -LiteralPath $artifactPath).Length;sha256=(Get-KmcSha256 $artifactPath)})}
 $f|Add-Member -NotePropertyName manifest -NotePropertyValue $m;return $f
}
function Manifest($F){$path=Join-Path $F.request.evidenceRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $F.manifest),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $F.request (Get-KmcSha256 $path) -GameResult|Out-Null}
Good 'strict native cold artifact and manifest' {$f=ManifestFixture;Manifest $f;Assert-KmcChunk7ManualDerivedArtifact $f.request $f.manifest 'PASS'}
Bad 'arbitrary cold kind' {$f=ManifestFixture;$f.manifest.artifacts[0].kind='cold-any';Manifest $f}
Bad 'arbitrary cold leaf' {$f=ManifestFixture;$f.manifest.artifacts[0].relativePath='cold-any.json';Manifest $f}
Bad 'foreign scenario borrows native evidence' {$f=ManifestFixture;$f.request.scenario='fixture-intake';Assert-KmcChunk7ManualDerivedArtifact $f.request $f.manifest 'PASS'}
Good 'pending native custody stays FAIL-only' {$f=ManifestFixture $true;Manifest $f;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Bad 'pending cannot become PASS' {$f=ManifestFixture $true;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'PASS'}
Bad 'foreign pending custody' {$f=ManifestFixture $true;$f.request.scenario='fixture-intake';Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
@{passes=$script:passes;failures=0;gameLaunches=0;archivesCreated=0;archivesRead=0;scope='actual request/result/runner guard AST and strict JSON artifacts'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bridgeOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL=0')
