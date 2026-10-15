[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/bootstrap-inventory-wiring-'+[Guid]::NewGuid().ToString('N'))}
$bridgeRoot=[IO.Path]::GetFullPath($RepositoryRoot);$bridgeRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$bridgeFixture=[IO.Path]::GetFullPath($FixturePath);$bridgeOutput=[IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($bridgeOutput)
. (Join-Path $bridgeRuntime 'RuntimeHarness.Common.ps1')
function Get-KmcRepositoryRoot {$bridgeRoot};function Get-KmcLabRoot {[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($bridgeRoot))}
function Ast([string]$Path){$tokens=$null;$parseErrors=$null;$v=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$parseErrors);if($parseErrors){throw ($parseErrors|Out-String)};$v}
function ExactFunction($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Missing exact function '+$Name)};$v[0].Extent.Text}
function CopyValue($Value){$Value|ConvertTo-Json -Depth 64|ConvertFrom-Json}
$script:passes=0
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passes++;Write-Host ('PASS rejects '+$Name)}
function Request {$f=[IO.File]::ReadAllText($bridgeFixture)|ConvertFrom-Json;$q=$f.request;$q.productVersion=(Read-KmcJson (Join-Path $bridgeRoot 'version.json')).productVersion;$q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId);$q}
$leaves=@('Invoke-KingmakerRuntimeScenario.ps1','RuntimeHarness.Common.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','RuntimeArtifactManifestEvidence.ps1','ScenarioDispatchEvidence.ps1','Chunk7CleanupEvidence.ps1','Chunk7BootstrapInventoryEvidence.ps1','Chunk7BootstrapInventoryFixtures.ps1')
foreach($leaf in $leaves){Good ('parse '+$leaf){[void](Ast (Join-Path $bridgeRuntime $leaf))}}
Good 'closed cold inventory registered once as save-backed compound' {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-bootstrap-cold-inventory'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-bootstrap-cold-inventory')){throw 'Closed registration differs'}}
foreach($alias in @('CHUNK7-BOOTSTRAP-COLD-INVENTORY','chunk7-cold-inventory','chunk7-bootstrap-cold-inventory-any')){Good ('inexact family unavailable '+$alias){if(Test-KmcChunk7BootstrapInventoryScenario $alias){throw 'Alias accepted'}}}
$body=[IO.File]::ReadAllText((Join-Path $bridgeRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed'}
$validator=[scriptblock]::Create($body.Replace($import,'# Common already loaded; actual validator body follows.'))
function Validate($Q){$path=Join-Path $bridgeOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
Good 'full request accepts exact one-load zero-write bridge' {Validate (Request)}
$negative=[ordered]@{
 'missing selection'={param($q)$q.PSObject.Properties.Remove('bootstrapInventory')}
 'foreign family'={param($q)$q.scenario='fixture-intake'}
 'case alias'={param($q)$q.bootstrapInventory.case='any-inventory'}
 'base-only false'={param($q)$q.bootstrapInventory.baseGameOnly=$false}
 'load two'={param($q)$q.bootstrapInventory.expectedNativeLoadCount=2}
 'load text'={param($q)$q.bootstrapInventory.expectedNativeLoadCount='1'}
 'write one'={param($q)$q.bootstrapInventory.expectedNativeWriteCount=1}
 'write text'={param($q)$q.bootstrapInventory.expectedNativeWriteCount='0'}
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
Good 'historical fixture request unchanged' {$q=Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('bootstrapInventory');$q.PSObject.Properties.Remove('persistenceLoad');Validate $q}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $bridgeRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-bootstrap-inventory-source','C7-bootstrap-inventory-native-load','C7-bootstrap-inventory-readonly','C7-bootstrap-inventory-cleanup')
 $result=[pscustomobject]@{scenario='chunk7-bootstrap-cold-inventory';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=4;subscenarioPassCount=4;subscenarioFailCount=0;assertionPassCount=4;assertionFailCount=0}
 Good ($leaf+' exact closed rows'){Assert-SubscenarioResults $result}
 Bad ($leaf+' arbitrary row'){$q=CopyValue $result;$q.subscenarioResults[0].name='C7-bootstrap-inventory-any';Assert-SubscenarioResults $q}
 Bad ($leaf+' duplicated row'){$q=CopyValue $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $bridgeRuntime 'Test-RuntimeGameResult.ps1')
foreach($quotaName in @('expectedNativeWrites','expectedWorkingLoads')){
 $nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$quotaName)},$true));if($nodes.Count-ne1){throw 'Exact quota assignment changed'}
 $quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$quotaName)
 foreach($family in @('chunk7-bootstrap-cold-inventory','fixture-intake')){
  $game=[pscustomobject]@{scenario=$family};$request=Request;$request.scenario=$family;$nativeBootstrap=$assistedFixture=$alternating=$campaignB=$absentKmc=$false;$assistedWriteCount=0
  $wanted=if($quotaName-ceq'expectedNativeWrites'){0}else{1};Good ('actual '+$family+' '+$quotaName){if((&$quota)-ne$wanted){throw 'Native quota differs'}}
 }
}
$runnerPath=Join-Path $bridgeRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runnerAst=Ast $runnerPath;$metadata=Get-Command $runnerPath
Good 'runner exact family and selection path registered' {if(-not$metadata.Parameters.ContainsKey('BootstrapInventorySelectionPath')-or'chunk7-bootstrap-cold-inventory'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Closed runner parameter absent'}}
$selection=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7BootstrapInventoryScenario $Scenario'-and$n.Extent.Text.Contains('requires explicit base-only staging and exact completed bootstrap source pins')},$true))
if($selection.Count-ne1){throw 'Exact explicit selection guard missing'}
function Read-KmcChunk7BootstrapInventorySelectionFile($Path,$RunId){[pscustomobject]@{path=$Path;selection=@{}}}
$selectionGuard=[scriptblock]::Create('param($Family,$Bindings,$Base,$InputPath) $Scenario=$Family;$BaseGameOnly=$Base;$BootstrapInventorySelectionPath=$InputPath;$RunId="cold";$PSBoundParameters=$Bindings;'+[Environment]::NewLine+$selection[0].Extent.Text)
function Selected([string]$Family,[hashtable]$Bindings,[bool]$Base=$true,[string]$InputPath='selection.json'){&$selectionGuard $Family $Bindings $Base $InputPath}
Good 'explicit base-only source selection admitted' {Selected 'chunk7-bootstrap-cold-inventory' @{BootstrapInventorySelectionPath='selection.json'}}
Bad 'selection missing' {Selected 'chunk7-bootstrap-cold-inventory' @{} $true ''}
Bad 'base-only missing' {Selected 'chunk7-bootstrap-cold-inventory' @{BootstrapInventorySelectionPath='selection.json'} $false}
Bad 'foreign selection family' {Selected 'fixture-intake' @{BootstrapInventorySelectionPath='selection.json'}}
foreach($permission in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){Bad ('extra runner permission '+$permission){$b=@{BootstrapInventorySelectionPath='selection.json'};$b[$permission]='foreign';Selected 'chunk7-bootstrap-cold-inventory' $b}}
$commands=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true))
$sourceCalls=@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcChunk7BootstrapInventorySource'});$lock=@($commands|Where-Object{$_.GetCommandName()-ceq'Open-KmcRuntimeLock'});$copy=@($commands|Where-Object{$_.GetCommandName()-ceq'Copy-KmcChunk7BootstrapInventorySourceArtifact'});$working=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcWorkingSaveTransaction'});$staging=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcModsTransaction'})
Good 'source requalified before locked staging and unchanged source after native run' {if($sourceCalls.Count-ne3-or$lock.Count-ne1-or$copy.Count-ne1-or$working.Count-ne1-or$staging.Count-ne1-or$selection[0].Extent.EndOffset-ge$sourceCalls[0].Extent.StartOffset-or$sourceCalls[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-le$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-ge$copy[0].Extent.StartOffset-or$copy[0].Extent.StartOffset-le$working[0].Extent.StartOffset-or$sourceCalls[2].Extent.StartOffset-le$staging[0].Extent.StartOffset){throw 'Exact guarded source ordering changed'}}
function ManifestFixture([bool]$Pending=$false){
 $f=[IO.File]::ReadAllText($bridgeFixture)|ConvertFrom-Json;$f.request.evidenceRoot=Join-Path $bridgeOutput ('manifest-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
 $nativeRoot=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($f.request.evidenceRoot))) ('runtime-staging/persistence-'+$f.request.runId+'/Saved Games')
 foreach($row in @($f.artifact.observations.beforeLoad.authority,$f.artifact.observations.loadedWorld.authority,$f.artifact.observations.settledWorld.authority,$f.artifact.observations.cleanup.authority)){$row.root=$nativeRoot}
 $f.artifact.observations.fixtureReadiness.isolatedSaveDescriptors.root=$nativeRoot
 $sourceDir=Join-Path $bridgeOutput $f.request.bootstrapInventory.sourceRunId;[void][IO.Directory]::CreateDirectory($sourceDir);$sourcePath=Join-Path $sourceDir 'chunk7-native-bootstrap-evidence.json';[IO.File]::WriteAllText($sourcePath,(ConvertTo-KmcChunk7Canonical $f.source),[Text.UTF8Encoding]::new($false))
 $sha=Get-KmcSha256 $sourcePath;$f.request.bootstrapInventory.sourceArtifactSha256=$sha;$f.artifact.observations.selection.sourceArtifactSha256=$sha;$f.artifact.observations.source.artifactSha256=$sha
 $kind='chunk7-bootstrap-inventory-evidence'
 if($Pending){$kind='chunk7-bootstrap-inventory-cleanup-pending';$f.artifact.evidenceKind=$kind;$f.artifact.status='FAIL';$f.artifact.errors=@('native callback retained');$f.artifact.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true}}
 $artifactPath=Join-Path $f.request.evidenceRoot ($kind+'.json');[IO.File]::WriteAllText($artifactPath,(ConvertTo-KmcChunk7Canonical $f.artifact),[Text.UTF8Encoding]::new($false))
 $m=[pscustomobject]@{schemaVersion=1;runId=$f.request.runId;scenario=$f.request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$kind+'.json';kind=$kind;length=[long](Get-Item -LiteralPath $artifactPath).Length;sha256=(Get-KmcSha256 $artifactPath)})}
 $f|Add-Member -NotePropertyName manifest -NotePropertyValue $m;return $f
}
function Manifest($F){$path=Join-Path $F.request.evidenceRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $F.manifest),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $F.request (Get-KmcSha256 $path) -GameResult|Out-Null}
Good 'strict native cold artifact and manifest' {$f=ManifestFixture;Manifest $f;Assert-KmcChunk7BootstrapInventoryArtifact $f.request $f.manifest 'PASS'}
Bad 'arbitrary cold kind' {$f=ManifestFixture;$f.manifest.artifacts[0].kind='cold-any';Manifest $f}
Bad 'arbitrary cold leaf' {$f=ManifestFixture;$f.manifest.artifacts[0].relativePath='cold-any.json';Manifest $f}
Bad 'foreign scenario borrows native evidence' {$f=ManifestFixture;$f.request.scenario='fixture-intake';Assert-KmcChunk7BootstrapInventoryArtifact $f.request $f.manifest 'PASS'}
Good 'pending native custody stays FAIL-only' {$f=ManifestFixture $true;Manifest $f;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Bad 'pending cannot become PASS' {$f=ManifestFixture $true;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'PASS'}
Bad 'foreign pending custody' {$f=ManifestFixture $true;$f.request.scenario='fixture-intake';Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
@{passes=$script:passes;failures=0;gameLaunches=0;archivesCreated=0;archivesRead=0;scope='actual request/result/runner guard AST and strict JSON artifacts'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bridgeOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL=0')
