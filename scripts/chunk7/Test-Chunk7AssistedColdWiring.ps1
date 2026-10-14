[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/assisted-cold-wiring-'+[Guid]::NewGuid().ToString('N'))}
$testRoot=[IO.Path]::GetFullPath($RepositoryRoot);$testRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$testOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $testOutput){throw 'Preserve existing cold wiring output.'};[void][IO.Directory]::CreateDirectory($testOutput)
$testLab=Join-Path $testOutput 'synthetic-lab';[void][IO.Directory]::CreateDirectory($testLab)
. (Join-Path $testRuntime 'RuntimeHarness.Common.ps1')
function Get-KmcRepositoryRoot{return $testRoot};function Get-KmcLabRoot{return $testLab}
function Copy-Test($Value){return ($Value|ConvertTo-Json -Depth 80 -Compress|ConvertFrom-Json)}
function Ast([string]$Path){$tokens=$null;$errors=$null;$a=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $a}
function ExactFunction($Ast,[string]$Name){$nodes=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($nodes.Count-ne1){throw ('Missing exact function '+$Name)};return $nodes[0].Extent.Text}
$script:passed=0;$script:failed=0;$rows=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[bool]$Reject,[scriptblock]$Action){$errorText=$null;try{&$Action|Out-Null}catch{$errorText=$_.Exception.Message};$ok=($null-ne$errorText)-eq$Reject;if($ok){$script:passed++;Write-Host ('PASS '+$Name)}else{$script:failed++;Write-Host ('FAIL '+$Name+': '+$errorText)};$rows.Add([ordered]@{name=$Name;expectedRefusal=$Reject;pass=$ok;error=$errorText})}
function New-Selection {Copy-Test ([ordered]@{case='assisted-manual-readonly-inventory';sourceRunId='generated-assisted-source';sourceArtifactSha256=('f'*64);sourceArchiveSha256=('c'*64);baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0})}
function New-Request {
 $b=[ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_298_KMC_AUTOMATION_BASELINE.zks';sha256=('a'*64);length=10;lastWriteTimeUtcTicks=638907120000000000L;gameId='11111111-1111-4111-8111-111111111111';gameName='Anchor';area=('a'*32)}
 $w=Copy-Test $b;$w.internalName='KMC_AUTOMATION_WORKING';$w.fileName='Manual_299_KMC_AUTOMATION_WORKING.zks';$w.sha256=('b'*64)
 $m=Copy-Test $b;$m.internalName='KMC_C7_ASSISTED_FIXTURE';$m.fileName='Manual_2_KMC_C7_ASSISTED_FIXTURE.zks';$m.sha256=('c'*64);$m.gameId='33333333-1111-4111-8111-111111111111';$m.gameName='Generated main';$m.area=('d'*32)
 Copy-Test ([ordered]@{schemaVersion=2;runId='generated-assisted-cold';scenario='chunk7-assisted-cold-inventory';branch='codex/mounted-combat-generated';commit=('a'*40);productVersion=(Read-KmcJson (Join-Path $testRoot 'version.json')).productVersion;dllSha256=('c'*64);dllMvid='22222222-1111-4111-8111-111111111111';transactionToken=('d'*64);evidenceRoot=(Join-Path $testLab 'runtime-evidence/generated-assisted-cold');fixture=[ordered]@{baseline=$b;working=$w;writeAuthorization=[ordered]@{mode='working-only';allowedInternalName=$w.internalName;allowedFileName=$w.fileName;baselineImmutable=$true}};qualificationSuite=[ordered]@{suiteId='generated-suite';snapshotSha256=('e'*64)};assistedColdInventory=(New-Selection);persistenceLoad=$m})
}
$leaves=@('RuntimeHarness.Common.ps1','Invoke-KingmakerRuntimeScenario.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','ScenarioDispatchEvidence.ps1','RuntimeArtifactManifestEvidence.ps1','Chunk7CleanupEvidence.ps1','Chunk7AssistedColdInventoryEvidence.ps1','Chunk7AssistedColdInventoryFixtures.ps1')
foreach($leaf in $leaves){Check ('parse '+$leaf) $false {[void](Ast (Join-Path $testRuntime $leaf))}}
Check 'closed cold family registered once as save-backed compound' $false {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-assisted-cold-inventory'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-assisted-cold-inventory')){throw 'Family registration differs.'}}
foreach($alias in @('CHUNK7-ASSISTED-COLD-INVENTORY','chunk7-assisted-cold','chunk7-assisted-cold-inventory-any')){Check ('inexact scenario unavailable '+$alias) $false {if(Test-KmcChunk7AssistedColdInventoryScenario $alias){throw 'Alias admitted.'}}}
$body=[IO.File]::ReadAllText((Join-Path $testRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual validator import boundary changed.'}
$validator=[scriptblock]::Create($body.Replace($import,'# Common already loaded; unchanged full validator follows.'))
function Validate($Q){$p=Join-Path $testOutput 'request.json';[IO.File]::WriteAllText($p,($Q|ConvertTo-Json -Depth 80),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $p|Out-Null}
Check 'full actual request admits exact cold Manual' $false {Validate (New-Request)}
$mutations=[ordered]@{
 'missing selection'={param($q)$q.PSObject.Properties.Remove('assistedColdInventory')}
 'historical family with cold selection'={param($q)$q.scenario='fixture-intake'}
 'case singleton'={param($q)$q.assistedColdInventory.case=@($q.assistedColdInventory.case)}
 'source same run'={param($q)$q.assistedColdInventory.sourceRunId=$q.runId}
 'source hash singleton'={param($q)$q.assistedColdInventory.sourceArtifactSha256=@($q.assistedColdInventory.sourceArtifactSha256)}
 'source archive hash mismatch'={param($q)$q.assistedColdInventory.sourceArchiveSha256=('e'*64)}
 'base false'={param($q)$q.assistedColdInventory.baseGameOnly=$false}
 'load two'={param($q)$q.assistedColdInventory.expectedNativeLoadCount=2}
 'load text'={param($q)$q.assistedColdInventory.expectedNativeLoadCount='1'}
 'write one'={param($q)$q.assistedColdInventory.expectedNativeWriteCount=1}
 'write text'={param($q)$q.assistedColdInventory.expectedNativeWriteCount='0'}
 'Auto input'={param($q)$q.persistenceLoad.fileName='Auto_1.zks'}
 'leading-zero Manual'={param($q)$q.persistenceLoad.fileName='Manual_02_KMC_C7_ASSISTED_FIXTURE.zks'}
 'overflow Manual'={param($q)$q.persistenceLoad.fileName='Manual_2147483648_KMC_C7_ASSISTED_FIXTURE.zks'}
 'leaf singleton'={param($q)$q.persistenceLoad.fileName=@($q.persistenceLoad.fileName)}
 'Working identity'={param($q)$q.persistenceLoad=$q.fixture.working}
 'protected campaign'={param($q)$q.persistenceLoad.gameId=$q.fixture.working.gameId}
 'missing descriptor'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'foreign case'={param($q)$q|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-manual'}
 'foreign route'={param($q)$q|Add-Member -NotePropertyName persistenceReplayTargets -NotePropertyValue @()}
 'foreign alternate'={param($q)$q|Add-Member -NotePropertyName persistenceAlternate -NotePropertyValue $q.persistenceLoad}
 'other source selection'={param($q)$q|Add-Member -NotePropertyName bootstrapInventory -NotePropertyValue ([pscustomobject]@{})}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($entry in $mutations.GetEnumerator()){Check ('full request refuses '+$entry.Key) $true {$q=New-Request;&$entry.Value $q;Validate $q}}
Check 'historical fixture request unchanged' $false {$q=New-Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('assistedColdInventory');$q.PSObject.Properties.Remove('persistenceLoad');Validate $q}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $testRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-assisted-cold-source','C7-assisted-cold-native-load','C7-assisted-cold-readonly','C7-assisted-cold-cleanup')
 $result=[pscustomobject]@{scenario='chunk7-assisted-cold-inventory';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=4;subscenarioPassCount=4;subscenarioFailCount=0;assertionPassCount=4;assertionFailCount=0}
 Check ($leaf+' exact cold rows') $false {Assert-SubscenarioResults $result}
 Check ($leaf+' arbitrary row refuses') $true {$q=Copy-Test $result;$q.subscenarioResults[0].name='C7-assisted-any';Assert-SubscenarioResults $q}
 Check ($leaf+' duplicate row refuses') $true {$q=Copy-Test $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $testRuntime 'Test-RuntimeGameResult.ps1')
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){
 $nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($nodes.Count-ne1){throw 'Actual quota assignment changed.'};$quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$name)
 foreach($family in @('chunk7-assisted-cold-inventory','fixture-intake')){$game=[pscustomobject]@{scenario=$family};$request=New-Request;$request.scenario=$family;$assistedFixture=$nativeBootstrap=$alternating=$campaignB=$absentKmc=$false;$expected=if($name-ceq'expectedNativeWrites'){0}else{1};Check ('actual '+$family+' quota '+$name) $false {if((&$quota)-ne$expected){throw 'Exact quota differs.'}}}
}
$runnerPath=Join-Path $testRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runner=Ast $runnerPath;$metadata=Get-Command $runnerPath
Check 'runner declares exact family and input path' $false {if(-not$metadata.Parameters.ContainsKey('AssistedColdInventorySelectionPath')-or'chunk7-assisted-cold-inventory'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Closed argument metadata differs.'}}
$guards=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7AssistedColdInventoryScenario $Scenario'-and$n.Extent.Text.Contains('requires base-only staging')},$true));if($guards.Count-ne1){throw 'Exact cold guard missing.'}
function Read-KmcChunk7AssistedColdInventorySelectionFile($Path,$RunId){return [pscustomobject]@{path=$Path;sha256=$script:intakeHash;selection=(New-Selection)}}
$script:intakeHash=('a'*64)
$guard=[scriptblock]::Create('param($Family,$Bindings,$Base,$Path) $Scenario=$Family;$PSBoundParameters=$Bindings;$BaseGameOnly=$Base;$AssistedColdInventorySelectionPath=$Path;$RunId="generated-assisted-cold";'+[Environment]::NewLine+$guards[0].Extent.Text)
function Select-Cold($Family,$Bindings,[bool]$Base=$true,[string]$Path='selection.json'){&$guard $Family $Bindings $Base $Path}
Check 'actual runner cold selection admitted' $false {Select-Cold 'chunk7-assisted-cold-inventory' @{AssistedColdInventorySelectionPath='selection.json'}}
Check 'actual runner missing selection refuses' $true {Select-Cold 'chunk7-assisted-cold-inventory' @{} $true ''}
Check 'actual runner non-base refuses' $true {Select-Cold 'chunk7-assisted-cold-inventory' @{AssistedColdInventorySelectionPath='selection.json'} $false}
Check 'actual runner foreign family refuses input' $true {Select-Cold 'fixture-intake' @{AssistedColdInventorySelectionPath='selection.json'}}
foreach($permission in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){Check ('actual runner extra permission refuses '+$permission) $true {$b=@{AssistedColdInventorySelectionPath='selection.json'};$b[$permission]='other';Select-Cold 'chunk7-assisted-cold-inventory' $b}}
$lockedNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7AssistedColdInventoryScenario $Scenario'-and$n.Extent.Text.Contains('Assisted cold selection changed before locked admission.')},$true))
$postNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'$gamePassed-and(Test-KmcChunk7AssistedColdInventoryScenario $Scenario)'},$true))
if($lockedNodes.Count-ne1-or$postNodes.Count-ne1){throw 'Actual cold source rechecks missing.'}
$lockedBody=[scriptblock]::Create($lockedNodes[0].Extent.Text);$postBody=[scriptblock]::Create($postNodes[0].Extent.Text)
$Scenario='chunk7-assisted-cold-inventory';$actualRunId='generated-assisted-cold';$request=New-Request;$fixturePayload=$request.fixture;$manifest=[pscustomobject]@{};$assistedColdInput=[pscustomobject]@{path='selection.json';sha256=('a'*64)}
$script:qualifiedSource=[pscustomobject]@{path='generated-native-manual';descriptor=$request.persistenceLoad;receipts=@('completed-source')};$assistedColdSource=Copy-Test $script:qualifiedSource
function Get-KmcChunk7AssistedColdInventorySource($Selection,$Fixture,$Candidate){return (Copy-Test $script:qualifiedSource)}
Check 'actual locked source remains exact' $false {&$lockedBody}
Check 'actual changed selection refuses under lock' $true {$script:intakeHash=('b'*64);try{&$lockedBody}finally{$script:intakeHash=('a'*64)}}
Check 'actual changed qualified source refuses under lock' $true {$script:qualifiedSource.receipts=@('changed');try{&$lockedBody}finally{$script:qualifiedSource.receipts=@('completed-source')}}
$script:coldEvidenceCalls=0;$script:archiveHash=$request.persistenceLoad.sha256;$script:sourcePid=1;$script:coldPid=2;$gamePassed=$true;$isolatedSaves='generated-Saved Games';$evidenceRoot='generated-evidence'
function Get-KmcSha256($Path){return $script:archiveHash}
function Read-KmcAssistedColdSourceArtifact($Request){return [pscustomobject]@{observations=[pscustomobject]@{processId=$script:sourcePid}}}
function Read-KmcJson($Path){return [pscustomobject]@{observations=[pscustomobject]@{processId=$script:coldPid}}}
function Assert-KmcChunk7AssistedColdInventoryEvidence($Request,$Artifact,$SourceArtifact,[string]$Status){if($Status-cne'PASS'-or$Artifact.observations.processId-eq$SourceArtifact.observations.processId){throw 'Cold evidence delegation differs.'};$script:coldEvidenceCalls++}
Check 'actual post-process source and readonly output rechecked' $false {&$postBody;if($script:coldEvidenceCalls-ne1){throw 'Cold evidence not reached.'}}
Check 'actual source receipt change after native refuses' $true {$script:qualifiedSource.receipts=@('changed');try{&$postBody}finally{$script:qualifiedSource.receipts=@('completed-source')}}
Check 'actual isolated Manual mutation after native refuses' $true {$script:archiveHash=('d'*64);try{&$postBody}finally{$script:archiveHash=$request.persistenceLoad.sha256}}
Check 'actual same process refuses cold claim' $true {$script:coldPid=1;try{&$postBody}finally{$script:coldPid=2}}
Check 'failed native result does not dispatch successful cold proof' $false {$gamePassed=$false;&$postBody;if($script:coldEvidenceCalls-ne1){throw 'Failed native result dispatched PASS evidence.'};$gamePassed=$true}
$ownerNodes=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.HashtableAst]-and$n.Extent.Text.Contains('sourceSha256=[string]$copyDescriptor.sha256')-and$n.Extent.Text.Contains('saveRoot=$isolatedSaves')},$true));if($ownerNodes.Count-ne1){throw 'Exact isolated owner formatter missing.'}
$ownerBody=[scriptblock]::Create('param([string]$PersistenceCase) '+$ownerNodes[0].Extent.Text);$lock=[pscustomobject]@{Token=('d'*64)};$copyDescriptor=$request.persistenceLoad;$ownerRecord=&$ownerBody
Check 'actual owner pins final Manual without generic case permission' $false {if($ownerRecord.sourceSha256-cne$request.persistenceLoad.sha256-or$ownerRecord.scenario-cne$Scenario-or$ownerRecord.persistenceCase-notin@($null,'')){throw 'Cold owner binding differs.'}}
$commands=@($runner.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true));$sources=@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcChunk7AssistedColdInventorySource'});$locks=@($commands|Where-Object{$_.GetCommandName()-ceq'Open-KmcRuntimeLock'});$copy=@($commands|Where-Object{$_.GetCommandName()-ceq'Copy-KmcChunk7AssistedColdInventorySourceArtifact'});$working=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcWorkingSaveTransaction'});$staging=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcModsTransaction'})
Check 'full source qualification remains before lock and under lock before copy' $false {if($sources.Count-ne3-or$locks.Count-ne1-or$copy.Count-ne1-or$working.Count-ne1-or$staging.Count-ne1-or$sources[0].Extent.StartOffset-ge$locks[0].Extent.StartOffset-or$sources[1].Extent.StartOffset-le$locks[0].Extent.StartOffset-or$sources[1].Extent.StartOffset-ge$copy[0].Extent.StartOffset-or$copy[0].Extent.StartOffset-le$working[0].Extent.StartOffset-or$sources[2].Extent.StartOffset-le$staging[0].Extent.StartOffset){throw 'Cold source ordering differs.'}}
$script:copyCalls=0
function Copy-KmcChunk7AssistedColdInventorySourceArtifact($Source,$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){if(-not[object]::ReferenceEquals($Source,$assistedColdSource)-or-not[object]::ReferenceEquals($Selection,$request.assistedColdInventory)-or-not[object]::ReferenceEquals($Fixture,$fixturePayload)-or-not[object]::ReferenceEquals($Candidate,$manifest)-or$ProfileRoot-cne'generated-profile'-or$Lock.Token-cne('d'*64)){throw 'Locked source-copy argument identity differs.'};$script:copyCalls++}
$profileRoot='generated-profile';Check 'actual artifact-copy delegate passes exact source lock and lineage' $false {&([scriptblock]::Create($copy[0].Extent.Text));if($script:copyCalls-ne1){throw 'Copy delegate absent.'}}
$script:artifactCalls=0
function Assert-KmcChunk7AssistedColdInventoryArtifact($Request,$Manifest,[string]$Status='PASS'){if($Request.scenario-cne'chunk7-assisted-cold-inventory'-or$Status-cne'FAIL'){throw 'Result delegation differs.'};$script:artifactCalls++}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){$a=Ast (Join-Path $testRuntime $leaf);$nodes=@($a.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcChunk7AssistedColdInventoryArtifact'},$true));if($nodes.Count-ne1){throw 'Result evidence delegate absent.'};$validatedArtifactManifest=[pscustomobject]@{};$game=$result=[pscustomobject]@{status='FAIL'};Check ($leaf+' actual cold artifact delegate') $false {&([scriptblock]::Create($nodes[0].Extent.Text))}}
Check 'both result paths retain exact status' $false {if($script:artifactCalls-ne2){throw 'Cold artifact delegates differ.'}}
[ordered]@{kind='actual-assisted-cold-wiring';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;rows=$rows.ToArray();substitutions=@('Generated private request paths; no Git/native/runtime data reads','Complete request validator executes unchanged except its import; actual runner AST blocks only','Completed-source qualification, source-file reads/hash and final Evidence/Artifact/Copy are explicit facades here; their full strict readers have separate coverage','No guarded runner, lock, save-copy, archive or native operation executes');nativeInvoked=$false;archivesRead=0}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $testOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passed+' FAIL='+$script:failed);if($script:failed-ne0){exit 1};exit 0
