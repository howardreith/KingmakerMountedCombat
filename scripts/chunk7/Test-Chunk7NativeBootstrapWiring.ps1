
[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeBootstrapEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/native-bootstrap-wiring-'+[Guid]::NewGuid().ToString('N'))}
$bootstrapWiringRoot=[IO.Path]::GetFullPath($RepositoryRoot);$bootstrapWiringRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$bootstrapWiringFixture=[IO.Path]::GetFullPath($FixturePath);$bootstrapWiringOutput=[IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($bootstrapWiringOutput)
. (Join-Path $bootstrapWiringRuntime 'RuntimeHarness.Common.ps1')
function Get-KmcRepositoryRoot {$bootstrapWiringRoot}
function Get-KmcLabRoot {[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($bootstrapWiringRoot))}
function Ast([string]$Path){$tokens=$null;$parseErrors=$null;$v=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$parseErrors);if($parseErrors){throw ($parseErrors|Out-String)};$v}
function ExactFunction($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Missing exact function '+$Name)};$v[0].Extent.Text}
function CopyValue($Value){$Value|ConvertTo-Json -Depth 64|ConvertFrom-Json}
$script:bootstrapWiringPasses=0
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:bootstrapWiringPasses++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:bootstrapWiringPasses++;Write-Host ('PASS rejects '+$Name)}
function Request {$f=[IO.File]::ReadAllText($bootstrapWiringFixture)|ConvertFrom-Json;$q=$f.request;$q.productVersion=(Read-KmcJson (Join-Path $bootstrapWiringRoot 'version.json')).productVersion;$q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId);$q}
$leaves=@('Invoke-KingmakerRuntimeScenario.ps1','RuntimeHarness.Common.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','RuntimeArtifactManifestEvidence.ps1','ScenarioDispatchEvidence.ps1','Chunk7CleanupEvidence.ps1','Chunk7NativeBootstrapEvidence.ps1')
foreach($leaf in $leaves){Good ('parse '+$leaf){[void](Ast (Join-Path $bootstrapWiringRuntime $leaf))}}
Good 'closed bootstrap registered once as save-backed compound' {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-native-main-bootstrap'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-native-main-bootstrap')){throw 'Closed registration differs'}}
foreach($name in @('CHUNK7-NATIVE-MAIN-BOOTSTRAP','chunk7-native-bootstrap','chunk7-native-main-bootstrap-any')){Good ('inexact family remains unavailable '+$name){if(Test-KmcChunk7NativeBootstrapScenario $name){throw 'Alias accepted'}}}
$body=[IO.File]::ReadAllText((Join-Path $bootstrapWiringRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed'}
$validator=[scriptblock]::Create($body.Replace($import,'# Already loaded Common; actual validator body follows.'))
function Validate($Q){$path=Join-Path $bootstrapWiringOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
Good 'full actual request accepts exact zero-load one-write bootstrap' {Validate (Request)}
# The runner holds its request as an ordered dictionary until serialization; the native bootstrap selection assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
Good 'native bootstrap selection assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7NativeBootstrapSelection (ConvertTo-RunnerRequest (Request))}
Bad 'ordered-dictionary request without its selection' {$r=ConvertTo-RunnerRequest (Request);$r.Remove('nativeBootstrap');Assert-KmcChunk7NativeBootstrapSelection $r}
Bad 'ordered-dictionary foreign family carrying a selection' {$r=ConvertTo-RunnerRequest (Request);$r['scenario']='fixture-intake';Assert-KmcChunk7NativeBootstrapSelection $r}
Good 'runner re-reads the validated request file before observer binding and launch, so nested ordered selections never reach a reader' {$runnerText=[IO.File]::ReadAllText((Join-Path $bootstrapWiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1'));$validator=$runnerText.IndexOf('Test-RuntimeRequest.ps1'') -RequestPath $requestPath');$roundTrip=$runnerText.IndexOf('$request=Read-KmcJson $requestPath');$launch=$runnerText.IndexOf('''-applaunch''');if(([regex]::Matches($runnerText,[regex]::Escape('$request=Read-KmcJson $requestPath'))).Count-ne1-or$validator-lt0-or$roundTrip-lt$validator-or$launch-lt$roundTrip){throw 'Runner does not re-read the validated request between validation and launch.'}}
$negative=[ordered]@{
 'missing bootstrap selection'={param($q)$q.PSObject.Properties.Remove('nativeBootstrap')}
 'foreign family borrowing bootstrap'={param($q)$q.scenario='fixture-intake'}
 'case alias'={param($q)$q.nativeBootstrap.case='Authored-main-campaign'}
 'base-only false'={param($q)$q.nativeBootstrap.baseGameOnly=$false}
 'base-only text'={param($q)$q.nativeBootstrap.baseGameOnly='true'}
 'load one'={param($q)$q.nativeBootstrap.expectedNativeLoadCount=1}
 'load text'={param($q)$q.nativeBootstrap.expectedNativeLoadCount='0'}
 'write two'={param($q)$q.nativeBootstrap.expectedNativeWriteCount=2}
 'write text'={param($q)$q.nativeBootstrap.expectedNativeWriteCount='1'}
 'foreign native leaf'={param($q)$q.nativeBootstrap.nativeWriteLeaf='Auto_2.zks'}
 'array native leaf'={param($q)$q.nativeBootstrap.nativeWriteLeaf=@('Auto_1.zks')}
 'extra source permission'={param($q)$q|Add-Member persistenceLoad ([pscustomobject]@{})}
 'extra persistence case'={param($q)$q|Add-Member persistenceCase 'multi-exploration-auto'}
 'extra route'={param($q)$q|Add-Member persistenceReplayAreas @()}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($n in $negative.GetEnumerator()){Bad ('full request '+$n.Key){$q=Request;&$n.Value $q;Validate $q}}
Good 'historical fixture intake retains ordinary request path' {$q=Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('nativeBootstrap');Validate $q}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $bootstrapWiringRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-bootstrap-preflight','C7-bootstrap-native-start','C7-bootstrap-native-world','C7-bootstrap-autosave','C7-bootstrap-cleanup')
 $result=[pscustomobject]@{scenario='chunk7-native-main-bootstrap';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=5;subscenarioPassCount=5;subscenarioFailCount=0;assertionPassCount=5;assertionFailCount=0}
 Good ($leaf+' exact bootstrap rows'){Assert-SubscenarioResults $result}
 Bad ($leaf+' arbitrary native start row'){$q=CopyValue $result;$q.subscenarioResults[0].name='C7-bootstrap-any';Assert-SubscenarioResults $q}
 Bad ($leaf+' duplicated named row'){$q=CopyValue $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $bootstrapWiringRuntime 'Test-RuntimeGameResult.ps1')
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){
 $nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($nodes.Count-ne1){throw 'Exact quota assignment changed'}
 $quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$name)
 foreach($family in @('chunk7-native-main-bootstrap','fixture-intake')){
  $game=[pscustomobject]@{scenario=$family};$request=Request;$request.scenario=$family;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $family;$assistedFixture=$alternating=$campaignB=$absentKmc=$false;$assistedWriteCount=0
  $wanted=if($name-ceq'expectedNativeWrites'){if($nativeBootstrap){1}else{0}}else{if($nativeBootstrap){0}else{1}}
  Good ('actual '+$family+' '+$name){if((&$quota)-ne$wanted){throw 'Native quota differs'}}
 }
}
$fixtureGuard=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-cmatch '^\$game\.fixtureIdentityVerified-isnot'},$true))
if($fixtureGuard.Count-ne1){throw 'Exact fixture-disposition guard missing'};$guard=[scriptblock]::Create($fixtureGuard[0].Extent.Text)
foreach($family in @('chunk7-native-main-bootstrap','fixture-intake')){
 $nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $family;$game=[pscustomobject]@{scenario=$family;fixtureIdentityVerified=(-not$nativeBootstrap);relationshipState='Unmounted'}
 Good ($family+' exact deserialization disposition'){&$guard}
 Bad ($family+' inverted deserialization disposition'){$game.fixtureIdentityVerified=-not$game.fixtureIdentityVerified;&$guard}
}
$runnerPath=Join-Path $bootstrapWiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runnerAst=Ast $runnerPath
$metadata=Get-Command $runnerPath
Good 'runner case has closed enum and exact scenario' {if(@($metadata.Parameters.NativeBootstrapCase.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues-cne'authored-main-campaign'-or'chunk7-native-main-bootstrap'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Closed runner parameter differs'}}
$selection=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7NativeBootstrapScenario $Scenario'-and$n.Extent.Text.Contains("Native main bootstrap requires explicit authored-main-campaign selection")},$true))
if($selection.Count-ne1){throw 'Exact pre-dispatch opt-in guard missing'};$selectionGuard=[scriptblock]::Create('param($Family,$Bindings,$Base) $Scenario=$Family;$BaseGameOnly=$Base;$NativeBootstrapCase="authored-main-campaign";$PSBoundParameters=$Bindings;'+[Environment]::NewLine+$selection[0].Extent.Text)
function Invoke-BootstrapSelection([string]$Family,[hashtable]$Bindings,[bool]$Base=$true){
 &$selectionGuard $Family $Bindings $Base
}
Good 'exact explicit runner selection admitted' {Invoke-BootstrapSelection 'chunk7-native-main-bootstrap' @{NativeBootstrapCase='authored-main-campaign'}}
Bad 'runner case omitted' {Invoke-BootstrapSelection 'chunk7-native-main-bootstrap' @{}}
Bad 'runner base-only omitted' {Invoke-BootstrapSelection 'chunk7-native-main-bootstrap' @{NativeBootstrapCase='authored-main-campaign'} $false}
Bad 'runner foreign family borrows bypass' {Invoke-BootstrapSelection 'fixture-intake' @{NativeBootstrapCase='authored-main-campaign'}}
foreach($permission in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
 Bad ('runner extra native permission '+$permission){$b=@{NativeBootstrapCase='authored-main-campaign'};$b[$permission]='foreign';Invoke-BootstrapSelection 'chunk7-native-main-bootstrap' $b}
}
$lock=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Open-KmcRuntimeLock'},$true))
Good 'exact opt-in precedes runtime lock and any transaction' {if($lock.Count-ne1-or$selection[0].Extent.EndOffset-ge$lock[0].Extent.StartOffset){throw 'Opt-in crossed lock'}}
function ManifestFixture([bool]$Pending=$false){
 $f=[IO.File]::ReadAllText($bootstrapWiringFixture)|ConvertFrom-Json;$f.request.evidenceRoot=Join-Path $bootstrapWiringOutput ('manifest-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
 $nativeRoot=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($f.request.evidenceRoot))) ('runtime-staging/persistence-'+$f.request.runId+'/Saved Games')
 foreach($row in @($f.artifact.observations.before.authority,$f.artifact.observations.nativeWorld.authority,$f.artifact.observations.cleanup.authority)){$row.root=$nativeRoot}
 $path=Join-Path $nativeRoot 'Auto_1.zks';$f.artifact.observations.nativeWrite.path=$path
 foreach($event in $f.artifact.observations.probe.events){if($event.save-gt0){$event.savePath=$path}}
 $kind='chunk7-native-bootstrap-evidence'
 if($Pending){$kind='chunk7-native-bootstrap-cleanup-pending';$f.artifact.evidenceKind=$kind;$f.artifact.status='FAIL';$f.artifact.errors=@('native callback retained');$f.artifact.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true}}
 $artifactPath=Join-Path $f.request.evidenceRoot ($kind+'.json');[IO.File]::WriteAllText($artifactPath,(ConvertTo-KmcChunk7Canonical $f.artifact),[Text.UTF8Encoding]::new($false))
 $m=[pscustomobject]@{schemaVersion=1;runId=$f.request.runId;scenario=$f.request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$kind+'.json';kind=$kind;length=[long](Get-Item -LiteralPath $artifactPath).Length;sha256=(Get-KmcSha256 $artifactPath)})}
 $f|Add-Member manifest $m;return $f
}
function Manifest($F){$path=Join-Path $F.request.evidenceRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $F.manifest),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $F.request (Get-KmcSha256 $path) -GameResult|Out-Null}
Good 'strict bootstrap manifest and native evidence accepted' {$f=ManifestFixture;Manifest $f;Assert-KmcChunk7NativeBootstrapArtifact $f.request $f.manifest 'PASS'}
Bad 'arbitrary bootstrap artifact kind' {$f=ManifestFixture;$f.manifest.artifacts[0].kind='bootstrap-any';Manifest $f}
Bad 'arbitrary bootstrap leaf' {$f=ManifestFixture;$f.manifest.artifacts[0].relativePath='bootstrap-any.json';Manifest $f}
Bad 'foreign scenario borrows bootstrap evidence' {$f=ManifestFixture;$f.request.scenario='fixture-intake';Assert-KmcChunk7NativeBootstrapArtifact $f.request $f.manifest 'PASS'}
Good 'pending custody remains separate FAIL-only artifact' {$f=ManifestFixture $true;Manifest $f;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Bad 'pending custody cannot become PASS' {$f=ManifestFixture $true;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'PASS'}
Bad 'foreign scenario cannot borrow pending custody' {$f=ManifestFixture $true;$f.request.scenario='fixture-intake';Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
@{pass=$script:bootstrapWiringPasses;fail=0;gameLaunches=0;archivesCreated=0;archivesRead=0;scope='actual request/result/runner guard AST and strict JSON manifest only'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bootstrapWiringOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:bootstrapWiringPasses+' FAIL=0; no runner invocation, native operation or archive access')
