[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-assisted-wiring-'+[Guid]::NewGuid().ToString('N'))}
$testRoot=[IO.Path]::GetFullPath($RepositoryRoot);$testRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$testOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $testOutput){throw 'Preserve existing assisted wiring output.'};[void][IO.Directory]::CreateDirectory($testOutput)
$testLab=Join-Path $testOutput 'synthetic-lab';[void][IO.Directory]::CreateDirectory($testLab)
. (Join-Path $testRuntime 'RuntimeHarness.Common.ps1')
. (Join-Path $testRuntime 'Chunk7AssistedPreparationInput.ps1')
function Get-KmcRepositoryRoot{return $testRoot};function Get-KmcLabRoot{return $testLab}
function Copy-Test($Value){return ($Value|ConvertTo-Json -Depth 80 -Compress|ConvertFrom-Json)}
function Ast([string]$Path){$tokens=$null;$errors=$null;$a=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $a}
function ExactFunction($Ast,[string]$Name){$nodes=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($nodes.Count-ne1){throw ('Missing exact function '+$Name)};return $nodes[0].Extent.Text}
$script:passed=0;$script:failed=0;$rows=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[bool]$Reject,[scriptblock]$Action){$errorText=$null;try{&$Action|Out-Null}catch{$errorText=$_.Exception.Message};$ok=($null-ne$errorText)-eq$Reject;if($ok){$script:passed++;Write-Host ('PASS '+$Name)}else{$script:failed++;Write-Host ('FAIL '+$Name+': '+$errorText)};$rows.Add([ordered]@{name=$Name;expectedRefusal=$Reject;pass=$ok;error=$errorText})}
function New-Selection {Copy-Test ([ordered]@{case='authored-main-ranger4-manual';ownerAuthorizationSha256='052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e';baseGameOnly=$true;assistedSetupOnly=$true;storyPolicy='first-cached-enabled-common-answer';assistancePolicy='native-main-ranger4-and-one-hire-shortfalls';expectedNativeLoadCount=0;maximumNativeWriteCount=3;targetRangerLevel=4;maximumTargetLevel=8;maximumStoryChoices=16;maximumStorySeconds=300;maximumExperienceShortfall=200000;maximumMoneyShortfall=100000;nativeInitialAutoLeaf='Auto_1.zks';nativeManualName='KMC_C7_ASSISTED_FIXTURE'})}
function New-Request {
 $b=[ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_298_KMC_AUTOMATION_BASELINE.zks';sha256=('a'*64);length=10;lastWriteTimeUtcTicks=638907120000000000L;gameId='11111111-1111-4111-8111-111111111111';gameName='Anchor';area=('a'*32)}
 $w=Copy-Test $b;$w.internalName='KMC_AUTOMATION_WORKING';$w.fileName='Manual_299_KMC_AUTOMATION_WORKING.zks';$w.sha256=('b'*64)
 Copy-Test ([ordered]@{schemaVersion=2;runId='generated-assisted';scenario='chunk7-assisted-fixture-preparation';branch='codex/mounted-combat-generated';commit=('a'*40);productVersion=(Read-KmcJson (Join-Path $testRoot 'version.json')).productVersion;dllSha256=('c'*64);dllMvid='22222222-1111-4111-8111-111111111111';transactionToken=('d'*64);evidenceRoot=(Join-Path $testLab 'runtime-evidence/generated-assisted');fixture=[ordered]@{baseline=$b;working=$w;writeAuthorization=[ordered]@{mode='working-only';allowedInternalName=$w.internalName;allowedFileName=$w.fileName;baselineImmutable=$true}};qualificationSuite=[ordered]@{suiteId='generated-suite';snapshotSha256=('e'*64)};assistedFixture=(New-Selection)})
}
$leaves=@('RuntimeHarness.Common.ps1','Invoke-KingmakerRuntimeScenario.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','ScenarioDispatchEvidence.ps1','RuntimeArtifactManifestEvidence.ps1','Chunk7CleanupEvidence.ps1','Chunk7AssistedPreparationInput.ps1','Chunk7AssistedFixtureEvidence.ps1')
foreach($leaf in $leaves){Check ('parse '+$leaf) $false {[void](Ast (Join-Path $testRuntime $leaf))}}
Check 'closed family appears once in save-backed and compound registrations' $false {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-assisted-fixture-preparation'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-assisted-fixture-preparation')){throw 'Family registration differs.'}}
foreach($alias in @('CHUNK7-ASSISTED-FIXTURE-PREPARATION','chunk7-assisted-fixture','chunk7-assisted-fixture-preparation-any')){Check ('inexact scenario rejected '+$alias) $false {if(Test-KmcChunk7AssistedFixtureScenario $alias){throw 'Inexact alias admitted.'}}}
$body=[IO.File]::ReadAllText((Join-Path $testRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual validator import boundary changed.'}
$validator=[scriptblock]::Create($body.Replace($import,'# Common already loaded; unchanged full validator below.'))
function Validate($Q){$p=Join-Path $testOutput 'request.json';[IO.File]::WriteAllText($p,($Q|ConvertTo-Json -Depth 80),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $p|Out-Null}
Check 'full actual request admits exact assisted setup' $false {Validate (New-Request)}
$mutations=[ordered]@{
 'missing selection'={param($q)$q.PSObject.Properties.Remove('assistedFixture')}
 'old family with assisted selection'={param($q)$q.scenario='fixture-intake'}
 'case singleton array'={param($q)$q.assistedFixture.case=@($q.assistedFixture.case)}
 'case alias'={param($q)$q.assistedFixture.case='any'}
 'owner authority'={param($q)$q.assistedFixture.ownerAuthorizationSha256=('f'*64)}
 'base only false'={param($q)$q.assistedFixture.baseGameOnly=$false}
 'assisted label false'={param($q)$q.assistedFixture.assistedSetupOnly=$false}
 'load one'={param($q)$q.assistedFixture.expectedNativeLoadCount=1}
 'write four'={param($q)$q.assistedFixture.maximumNativeWriteCount=4}
 'write text'={param($q)$q.assistedFixture.maximumNativeWriteCount='3'}
 'XP cap'={param($q)$q.assistedFixture.maximumExperienceShortfall++}
 'money cap'={param($q)$q.assistedFixture.maximumMoneyShortfall++}
 'story cap'={param($q)$q.assistedFixture.maximumStoryChoices++}
 'story policy'={param($q)$q.assistedFixture.storyPolicy='any'}
 'Auto leaf'={param($q)$q.assistedFixture.nativeInitialAutoLeaf='Auto_2.zks'}
 'Manual name'={param($q)$q.assistedFixture.nativeManualName='KMC_other'}
 'foreign saved source'={param($q)$q|Add-Member -NotePropertyName persistenceLoad -NotePropertyValue $q.fixture.working}
 'foreign case'={param($q)$q|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-manual'}
 'foreign route'={param($q)$q|Add-Member -NotePropertyName persistenceReplayTargets -NotePropertyValue @()}
 'other source selection'={param($q)$q|Add-Member -NotePropertyName nativeBootstrap -NotePropertyValue ([pscustomobject]@{})}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($entry in $mutations.GetEnumerator()){Check ('full request rejects '+$entry.Key) $true {$q=New-Request;&$entry.Value $q;Validate $q}}
Check 'untouched historical fixture request stays admitted' $false {$q=New-Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('assistedFixture');Validate $q}
$selectionPath=Join-Path $testLab 'selection.json';$selection=New-Selection
function Write-Selection($Value){[IO.File]::WriteAllText($selectionPath,($Value|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))}
Check 'real bounded producer selection file and exact hash' $false {Write-Selection $selection;$s=Read-KmcChunk7AssistedPreparationSelectionFile $selectionPath 'generated-assisted';if($s.sha256-cne(Get-KmcSha256 $selectionPath)-or(ConvertTo-KmcChunk7Canonical $s.selection)-cne(ConvertTo-KmcChunk7Canonical $selection)){throw 'Selection observation differs.'}}
Check 'consumer three-field lineage cannot become preparation permission' $true {Write-Selection ([pscustomobject]@{sourceRunId='other';sourceArtifactSha256=('a'*64);sourceArchiveSha256=('b'*64)});Read-KmcChunk7AssistedPreparationSelectionFile $selectionPath 'generated-assisted'}
Check 'selection duplicate property refuses' $true {[IO.File]::WriteAllText($selectionPath,'{"case":"one","case":"two"}');Read-KmcChunk7AssistedPreparationSelectionFile $selectionPath 'generated-assisted'}
Check 'selection oversized refuses before JSON parse' $true {[IO.File]::WriteAllText($selectionPath,(' '*16385));Read-KmcChunk7AssistedPreparationSelectionFile $selectionPath 'generated-assisted'}
Check 'selection empty refuses' $true {[IO.File]::WriteAllText($selectionPath,'');Read-KmcChunk7AssistedPreparationSelectionFile $selectionPath 'generated-assisted'}
Check 'selection outside synthetic lab refuses' $true {Read-KmcChunk7AssistedPreparationSelectionFile (Join-Path $testOutput 'not-admitted.json') 'generated-assisted'}
Write-Selection $selection
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $testRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-assisted-preflight','C7-assisted-native-start','C7-assisted-story','C7-assisted-setup','C7-assisted-native-choices','C7-assisted-manual','C7-assisted-cleanup')
 $result=[pscustomobject]@{scenario='chunk7-assisted-fixture-preparation';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=7;subscenarioPassCount=7;subscenarioFailCount=0;assertionPassCount=7;assertionFailCount=0}
 Check ($leaf+' exact closed rows') $false {Assert-SubscenarioResults $result}
 Check ($leaf+' arbitrary row refuses') $true {$q=Copy-Test $result;$q.subscenarioResults[0].name='C7-assisted-any';Assert-SubscenarioResults $q}
 Check ($leaf+' duplicate row refuses') $true {$q=Copy-Test $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $testRuntime 'Test-RuntimeGameResult.ps1')
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){
 $nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($nodes.Count-ne1){throw 'Actual quota assignment changed.'}
 $quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$name)
 foreach($count in @(2,3)){$game=[pscustomobject]@{scenario='chunk7-assisted-fixture-preparation'};$request=New-Request;$assistedFixture=$true;$assistedWriteCount=$count;$nativeBootstrap=$alternating=$campaignB=$absentKmc=$false;$expected=if($name-ceq'expectedNativeWrites'){$count}else{0};Check ('actual assisted quota '+$name+' writes'+$count) $false {if((&$quota)-ne$expected){throw 'Closed quota differs.'}}}
 $game=[pscustomobject]@{scenario='fixture-intake'};$request=New-Request;$request.scenario=$game.scenario;$assistedFixture=$false;$expected=if($name-ceq'expectedNativeWrites'){0}else{1};Check ('actual legacy quota '+$name) $false {if((&$quota)-ne$expected){throw 'Legacy quota differs.'}}
}
$runnerPath=Join-Path $testRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runner=Ast $runnerPath;$metadata=Get-Command $runnerPath
Check 'runner declares exact scenario and input path' $false {if(-not$metadata.Parameters.ContainsKey('AssistedFixtureSelectionPath')-or'chunk7-assisted-fixture-preparation'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Closed argument metadata differs.'}}
$guards=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7AssistedFixtureScenario $Scenario'-and$n.Extent.Text.Contains('requires explicit base-only staging')},$true));if($guards.Count-ne1){throw 'Exact assisted entry guard missing.'}
function Read-KmcChunk7AssistedPreparationSelectionFile($Path,$RunId){return [pscustomobject]@{path=$Path;sha256=('a'*64);selection=(New-Selection)}}
$guard=[scriptblock]::Create('param($Family,$Bindings,$Base,$Path) $Scenario=$Family;$PSBoundParameters=$Bindings;$BaseGameOnly=$Base;$AssistedFixtureSelectionPath=$Path;$RunId="generated-assisted";'+[Environment]::NewLine+$guards[0].Extent.Text)
function Select-Preparation($Family,$Bindings,[bool]$Base=$true,[string]$Path='selection.json'){&$guard $Family $Bindings $Base $Path}
Check 'actual runner selected preparation branch' $false {Select-Preparation 'chunk7-assisted-fixture-preparation' @{AssistedFixtureSelectionPath='selection.json'}}
Check 'actual runner missing selection refuses' $true {Select-Preparation 'chunk7-assisted-fixture-preparation' @{} $true ''}
Check 'actual runner non-base staging refuses' $true {Select-Preparation 'chunk7-assisted-fixture-preparation' @{AssistedFixtureSelectionPath='selection.json'} $false}
Check 'actual runner foreign family refuses selection' $true {Select-Preparation 'fixture-intake' @{AssistedFixtureSelectionPath='selection.json'}}
foreach($permission in @('NativeBootstrapCase','PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){Check ('actual runner extra permission refuses '+$permission) $true {$b=@{AssistedFixtureSelectionPath='selection.json'};$b[$permission]='other';Select-Preparation 'chunk7-assisted-fixture-preparation' $b}}
$script:intakeHash=('a'*64)
function Read-KmcChunk7AssistedPreparationSelectionFile($Path,$RunId){return [pscustomobject]@{path=$Path;sha256=$script:intakeHash;selection=(New-Selection)}}
$lockedNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7AssistedFixtureScenario $Scenario'-and$n.Extent.Text.Contains('Assisted fixture selection changed before locked admission.')},$true))
$postNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7AssistedFixtureScenario $Scenario'-and$n.Extent.Text.Contains('Assisted fixture selection changed during the native process.')},$true))
if($lockedNodes.Count-ne1-or$postNodes.Count-ne1){throw 'Actual repeated preparation check disappeared.'}
$lockedBody=[scriptblock]::Create($lockedNodes[0].Extent.Text);$postBody=[scriptblock]::Create($postNodes[0].Extent.Text)
$Scenario='chunk7-assisted-fixture-preparation';$actualRunId='generated-assisted';$assistedFixtureInput=[pscustomobject]@{path='selection.json';sha256=('a'*64)};$request=New-Request;$gamePassed=$false
Check 'actual locked unchanged selection revalidated' $false {&$lockedBody}
Check 'actual locked changed file refuses' $true {$script:intakeHash=('b'*64);try{&$lockedBody}finally{$script:intakeHash=('a'*64)}}
Check 'actual locked mutated selection refuses' $true {$request=New-Request;$request.assistedFixture.maximumNativeWriteCount=4;try{&$lockedBody}finally{$request=New-Request}}
Check 'actual post-process unchanged selection retained even on FAIL' $false {&$postBody}
Check 'actual post-process changed file refuses' $true {$script:intakeHash=('b'*64);try{&$postBody}finally{$script:intakeHash=('a'*64)}}
$script:outputOrder=[Collections.Generic.List[string]]::new()
function Assert-KmcChunk7AssistedFixtureOutput($Request,$Artifact){$script:outputOrder.Add('output')}
function Assert-KmcChunk7AssistedFixtureArchive($Request,$Artifact){$script:outputOrder.Add('archive')}
$evidenceRoot=Join-Path $testOutput 'generated-output-dispatch';[void][IO.Directory]::CreateDirectory($evidenceRoot);[IO.File]::WriteAllText((Join-Path $evidenceRoot 'chunk7-assisted-fixture-evidence.json'),'{}')
Check 'actual successful process dispatches output before archive gate' $false {$gamePassed=$true;&$postBody;if(($script:outputOrder-join',')-cne'output,archive'){throw 'Output/archive guard order differs.'};$gamePassed=$false}
$ownerNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.HashtableAst]-and$n.Extent.Text.Contains('sourceSha256=[string]$copyDescriptor.sha256')-and$n.Extent.Text.Contains('saveRoot=$isolatedSaves')},$true));if($ownerNodes.Count-ne1){throw 'Exact isolated owner formatter missing.'}
$ownerBody=[scriptblock]::Create('param([string]$PersistenceCase) '+$ownerNodes[0].Extent.Text)
$lock=[pscustomobject]@{Token=('d'*64)};$copyDescriptor=$request.fixture.working;$isolatedSaves=Join-Path $testLab 'runtime-staging/persistence-generated-assisted/Saved Games';$ownerRecord=&$ownerBody
$observedOwner=Copy-Test $ownerRecord
Check 'actual owner formatter binds exact Working anchor without a source load' $false {if($observedOwner.sourceSha256-cne$request.fixture.working.sha256-or$observedOwner.scenario-cne$Scenario-or$observedOwner.saveRoot-cne$isolatedSaves){throw 'Initial owner anchor binding changed.'}}

$commands=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true));$intake=@($commands|Where-Object{$_.GetCommandName()-ceq'Read-KmcChunk7AssistedPreparationSelectionFile'});$lock=@($commands|Where-Object{$_.GetCommandName()-ceq'Open-KmcRuntimeLock'});$working=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcWorkingSaveTransaction'});$staging=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcModsTransaction'});$output=@($commands|Where-Object{$_.GetCommandName()-ceq'Assert-KmcChunk7AssistedFixtureOutput'});$archive=@($commands|Where-Object{$_.GetCommandName()-ceq'Assert-KmcChunk7AssistedFixtureArchive'})
Check 'selection intake repeated under lock and after native before output checks' $false {if($intake.Count-ne3-or$lock.Count-ne1-or$working.Count-ne1-or$staging.Count-ne1-or$output.Count-ne1-or$archive.Count-ne1-or$intake[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or$intake[1].Extent.StartOffset-le$lock[0].Extent.StartOffset-or$intake[1].Extent.StartOffset-ge$working[0].Extent.StartOffset-or$intake[2].Extent.StartOffset-le$staging[0].Extent.StartOffset-or$output[0].Extent.StartOffset-le$intake[2].Extent.StartOffset-or$archive[0].Extent.StartOffset-le$output[0].Extent.StartOffset){throw 'Guarded preparation ordering changed.'}}
# Execute actual two result delegates against explicitly substituted evidence validation.
$script:artifactCalls=0
function Assert-KmcChunk7AssistedFixtureArtifact($Request,$Manifest,[string]$Status='PASS'){if($Request.scenario-cne'chunk7-assisted-fixture-preparation'-or$Status-cne'FAIL'){throw 'Result delegation changed.'};$script:artifactCalls++}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){$a=Ast (Join-Path $testRuntime $leaf);$nodes=@($a.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcChunk7AssistedFixtureArtifact'},$true));if($nodes.Count-ne1){throw 'Result evidence delegate missing.'};$request=New-Request;$validatedArtifactManifest=[pscustomobject]@{};$game=$result=[pscustomobject]@{status='FAIL'};Check ($leaf+' actual artifact delegate') $false {&([scriptblock]::Create($nodes[0].Extent.Text))}}
Check 'both result paths reached the declared artifact boundary' $false {if($script:artifactCalls-ne2){throw 'Result delegation incomplete.'}}
[ordered]@{kind='actual-assisted-fixture-wiring';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;rows=$rows.ToArray();observedUnboundOwnerPersistenceCase=$observedOwner.persistenceCase;ownerPersistenceCaseIsNull=($null-eq$observedOwner.persistenceCase);substitutions=@('GetRepositoryRoot/GetLabRoot select generated private JSON paths; no Git or real evidence reads','Runner selection file return is a controlled facade only after real preparation-file checks','Only extracted runner admission/ordering and result delegate commands execute; no guarded launcher/lock/archive operation','Full assisted Artifact/Output/Archive validation is an external boundary and is not certified by this test');nativeInvoked=$false;archivesRead=0}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $testOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passed+' FAIL='+$script:failed);if($script:failed-ne0){exit 1};exit 0
