param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$RuntimeDirectory){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'};if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-owned-wiring-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$bridgeRoot=$RepositoryRoot;$bridgeRuntime=$RuntimeDirectory;$bridgeOutput=$OutputDirectory
. (Join-Path $bridgeRuntime 'RuntimeHarness.Common.ps1')
$fixtures=(Get-Content -LiteralPath $FixturePath -Raw|ConvertFrom-Json).fixtures;$anchor=Read-KmcJson (Join-Path $bridgeRoot 'tests/Chunk7Diagnostics/TwoOwnerManualExistingFixture.json')
function Get-KmcRepositoryRoot{$bridgeRoot};function Get-KmcLabRoot{Join-Path $bridgeOutput 'virtual-lab'}
function Ast([string]$Path){$tokens=$null;$parseErrors=$null;$v=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$parseErrors);if($parseErrors){throw ($parseErrors|Out-String)};$v}
function ExactFunction($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Missing exact function '+$Name)};$v[0].Extent.Text}
function CopyValue($Value){$Value|ConvertTo-Json -Depth 90|ConvertFrom-Json}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false){$e=$null;$pass=$false;try{&$Body;$pass=-not$Refuse}catch{$e=$_.Exception.Message;$pass=$Refuse};$results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Refuse;error=$e});if(-not$pass){Write-Output ('FAIL '+$Name+': '+$e)}}
function Request([string]$Name='exploration'){$q=CopyValue @($fixtures|Where-Object name -CEQ $Name)[0].request;$protectedSha=$q.fixture.working.sha256;$q.fixture=CopyValue $anchor.request.fixture;$q.fixture.working.sha256=$protectedSha;$q.fixture.working.gameId='abababab-abab-4bab-8bab-abababababab';$q.fixture.baseline.gameId=$q.fixture.working.gameId;$q|Add-Member schemaVersion 2 -Force;$q.fixture.baseline.sha256='4'*64;$q|Add-Member qualificationSuite (CopyValue $anchor.request.qualificationSuite);$q|Add-Member transactionToken ('a'*64);$q.branch='codex/mounted-combat-owned-reader-tests';$q.productVersion=(Read-KmcJson (Join-Path $bridgeRoot 'version.json')).productVersion;$q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId);$q}

foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual')){
 $f=CopyValue @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0];$f.name='persistence-load-'+$case;$f.request.persistenceCase=$case;$f.request.ownedPairs.expectedNativeLoadCount=if($case-ceq'multi-exploration-three-loads-manual'){3}else{1}
 if($case-ceq'multi-exploration-three-area-transfers-manual'){$f.request|Add-Member persistenceReplayTargets @(for($i=0;$i-lt3;$i++){[pscustomobject]@{enterPoint=@('1','2','3')[$i]*32;area=@('b','c','d')[$i]*32;autoSaveMode='None'}})};$fixtures+=@($f)
}
$leaves=@('Invoke-KingmakerRuntimeScenario.ps1','RuntimeHarness.Common.ps1','Test-RuntimeRequest.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1','ScenarioDispatchEvidence.ps1','Chunk7OwnedPairsEvidence.ps1','Chunk7OwnedPairsFixtures.ps1')
foreach($leaf in $leaves){Check ('parse '+$leaf){[void](Ast (Join-Path $bridgeRuntime $leaf))}}
Check 'closed family registered once as save-backed compound'{if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-owned-two-pair-campaign'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-owned-two-pair-campaign')){throw 'Registration differs'}}
$body=[IO.File]::ReadAllText((Join-Path $bridgeRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')";if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import changed'};$validator=[scriptblock]::Create($body.Replace($import,'# Common loaded; actual validator body follows.'))
function Validate($Q){$path=Join-Path $bridgeOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
foreach($f in $fixtures){Check ('full request '+$f.name){Validate (Request $f.name)}}
# The runner holds its request as an ordered dictionary until serialization; the owned selection assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
Check 'owned selection assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7OwnedPairsSelection (ConvertTo-RunnerRequest (Request))}
Check 'ordered-dictionary request without its selection is refused' {$r=ConvertTo-RunnerRequest (Request);$r.Remove('ownedPairs');Assert-KmcChunk7OwnedPairsSelection $r} $true
Check 'ordered-dictionary foreign family carrying a selection is refused' {$r=ConvertTo-RunnerRequest (Request);$r['scenario']='fixture-intake';Assert-KmcChunk7OwnedPairsSelection $r} $true
$negative=[ordered]@{
 'selection omitted'={param($q)$q.PSObject.Properties.Remove('ownedPairs')}
 'foreign family'={param($q)$q.scenario='fixture-intake'}
 'case alias'={param($q)$q.ownedPairs.case='exploration-any'}
 'role collision'={param($q)$q.ownedPairs.mountBId=$q.ownedPairs.mountAId}
 'missing full roster'={param($q)$q.ownedPairs.partyIds=@()}
 'base-only false'={param($q)$q.ownedPairs.baseGameOnly=$false}
 'load two'={param($q)$q.ownedPairs.expectedNativeLoadCount=2}
 'load string'={param($q)$q.ownedPairs.expectedNativeLoadCount='1'}
 'read-only write'={param($q)$q.ownedPairs.expectedNativeWriteCount=1}
 'foreign native leaf'={param($q)$q.persistenceLoad.fileName='Manual_42_KMC_AUTOMATION_WORKING.zks'}
 'array native leaf'={param($q)$q.persistenceLoad.fileName=@($q.persistenceLoad.fileName)}
 'protected campaign'={param($q)$q.persistenceLoad.gameId=$q.fixture.working.gameId}
 'input absent'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'alternate'={param($q)$q|Add-Member persistenceAlternate $q.persistenceLoad}
 'foreign case'={param($q)$q|Add-Member persistenceCase 'multi-exploration-manual'}
 'route'={param($q)$q|Add-Member persistenceReplayTargets @()}
 'foreign source family'={param($q)$q|Add-Member acquiredManual $anchor.request.acquiredManual}
 'schema1'={param($q)$q.schemaVersion=1}
}
foreach($n in $negative.GetEnumerator()){Check ('full request refuses '+$n.Key){$q=Request;&$n.Value $q;Validate $q} $true}
Check 'historical fixture request unchanged'{$q=Request;$q.scenario='fixture-intake';$q.PSObject.Properties.Remove('ownedPairs');$q.PSObject.Properties.Remove('persistenceLoad');Validate $q}
foreach($case in @('multi-area-autosave','multi-exploration-three-loads-manual')){Check ('unselected persistence case '+$case){$q=Request 'persistence-save-multi-exploration-manual';$q.persistenceCase=$case;Validate $q} $true}
foreach($case in @('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual')){
 Check ('full exact owned source boundary '+$case){$q=Request 'persistence-save-multi-exploration-manual';$q.persistenceCase=$case;$q.ownedPairs.expectedNativeWriteCount=if($case-ceq'multi-owned-worker-cancel-manual'){2}else{1};Validate $q}
 Check ('full wrong boundary quota '+$case){$q=Request 'persistence-save-multi-exploration-manual';$q.persistenceCase=$case;$q.ownedPairs.expectedNativeWriteCount=if($case-ceq'multi-owned-worker-cancel-manual'){1}else{2};Validate $q} $true
 Check ('full owned boundary cold alias '+$case){$q=Request 'persistence-load-multi-exploration-manual';$q.persistenceCase=$case;Validate $q} $true
}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
 . ([scriptblock]::Create((ExactFunction (Ast (Join-Path $bridgeRuntime $leaf)) 'Assert-SubscenarioResults')))
 $names=@('C7-owned-pairs-inherited-custody','C7-world-cleanup');$result=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=2;subscenarioPassCount=2;subscenarioFailCount=0;assertionPassCount=2;assertionFailCount=0}
 Check ($leaf+' exact rows'){Assert-SubscenarioResults $result};Check ($leaf+' arbitrary row'){$q=CopyValue $result;$q.subscenarioResults[0].name='C7-owned-any';Assert-SubscenarioResults $q} $true;Check ($leaf+' duplicate row'){$q=CopyValue $result;$q.subscenarioResults[1].name=$q.subscenarioResults[0].name;Assert-SubscenarioResults $q} $true
}
$gameAst=Ast (Join-Path $bridgeRuntime 'Test-RuntimeGameResult.ps1')
foreach($quotaName in @('expectedNativeWrites','expectedWorkingLoads')){$nodes=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$quotaName)},$true));if($nodes.Count-ne1){throw 'Exact quota changed'};$quota=[scriptblock]::Create($nodes[0].Extent.Text+[Environment]::NewLine+'return $'+$quotaName)
 foreach($f in $fixtures){$game=[pscustomobject]@{scenario=$f.request.scenario};$request=$f.request;$nativeBootstrap=$assistedFixture=$alternating=$campaignB=$absentKmc=$false;$assistedWriteCount=0;$wanted=if($quotaName-ceq'expectedNativeWrites'){$f.request.ownedPairs.expectedNativeWriteCount}else{$f.request.ownedPairs.expectedNativeLoadCount};Check ('actual quota '+$quotaName+'/'+$f.name){if((&$quota)-ne$wanted){throw 'Exact quota differs'}}}
}
$runnerPath=Join-Path $bridgeRuntime 'Invoke-KingmakerRuntimeScenario.ps1';$runnerAst=Ast $runnerPath;$metadata=Get-Command $runnerPath
Check 'runner exact family and selection parameter'{if(-not$metadata.Parameters.ContainsKey('OwnedPairsSelectionPath')-or'chunk7-owned-two-pair-campaign'-cnotin@($metadata.Parameters.Scenario.Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Parameter missing'}}
$selection=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcChunk7OwnedPairsScenario $Scenario'-and$n.Extent.Text.Contains('requires explicit base-only staging and a qualified two-owner native Manual selection')},$true));if($selection.Count-ne1){throw 'Exact selection guard missing'}
function Read-KmcChunk7OwnedPairsSelectionFile($Path,$RunId,$Checkpoint){[pscustomobject]@{path=$Path;selection=@{}}}
$guard=[scriptblock]::Create('param($Family,$Bindings,$Base,$InputPath) $Scenario=$Family;$BaseGameOnly=$Base;$OwnedPairsSelectionPath=$InputPath;$RunId="owned";$PersistenceCase=$null;$PSBoundParameters=$Bindings;'+[Environment]::NewLine+$selection[0].Extent.Text)
function Selected([string]$Family,[hashtable]$Bindings,[bool]$Base=$true,[string]$InputPath='selection.json'){&$guard $Family $Bindings $Base $InputPath}
Check 'explicit selected base-only'{Selected 'chunk7-owned-two-pair-campaign' @{OwnedPairsSelectionPath='selection.json'}}
Check 'input missing'{Selected 'chunk7-owned-two-pair-campaign' @{} $true ''} $true
Check 'base-only missing'{Selected 'chunk7-owned-two-pair-campaign' @{OwnedPairsSelectionPath='selection.json'} $false} $true
Check 'foreign selection'{Selected 'fixture-intake' @{OwnedPairsSelectionPath='selection.json'}} $true
foreach($permission in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){Check ('extra runner authority '+$permission){$b=@{OwnedPairsSelectionPath='selection.json'};$b[$permission]='foreign';Selected 'chunk7-owned-two-pair-campaign' $b} $true}
$commands=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]},$true));$sourceCalls=@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcChunk7OwnedPairsSource'});$lock=@($commands|Where-Object{$_.GetCommandName()-ceq'Open-KmcRuntimeLock'});$copy=@($commands|Where-Object{$_.GetCommandName()-ceq'Copy-KmcChunk7OwnedPairsSourceArtifact'});$working=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcWorkingSaveTransaction'});$staging=@($commands|Where-Object{$_.GetCommandName()-ceq'Enter-KmcModsTransaction'})
Check 'source before lock, locked recheck/copy, after-native recheck'{if($sourceCalls.Count-ne3-or$lock.Count-ne1-or$copy.Count-ne1-or$working.Count-ne1-or$staging.Count-ne1-or$selection[0].Extent.EndOffset-ge$sourceCalls[0].Extent.StartOffset-or$sourceCalls[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-le$lock[0].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-ge$copy[0].Extent.StartOffset-or$copy[0].Extent.StartOffset-le$working[0].Extent.StartOffset-or$sourceCalls[2].Extent.StartOffset-le$staging[0].Extent.StartOffset){throw 'Guard order differs'}}

foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual')){
 foreach($bad in @('load4','write1','route','source-only','checkpoint-missing')){Check ('full replay request refuses '+$case+'/'+$bad){$q=Request ('persistence-load-'+$case);switch($bad){'load4'{$q.ownedPairs.expectedNativeLoadCount=4}'write1'{$q.ownedPairs.expectedNativeWriteCount=1}'route'{if($case-ceq'multi-exploration-three-loads-manual'){$q|Add-Member persistenceReplayTargets @()}else{$q.persistenceReplayTargets[0].area=$q.persistenceLoad.area}}'source-only'{$q.ownedPairs.case='persistence-save'}'checkpoint-missing'{$q.ownedPairs.checkpointSourceRunId=$null}};Validate $q} $true}
}
# Owner-seed kind through the actual full request validator, quotas and runner timeout guard.
function OwnerSeedRequest{$q=Request;$p=$q.ownedPairs;$q.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-exploration';seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;riderBId=$p.riderBId;partyIds=@($p.riderAId,$p.riderBId,'50505050-5050-4050-8050-505050505050');xpAssistance=[pscustomobject][ordered]@{unitId=$p.riderBId;targetCharacterLevel=8};baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0};$q.persistenceLoad.internalName='KMC_C7_OWNER_SEED';$q.persistenceLoad.fileName='Manual_302_KMC_C7_OWNER_SEED.zks';$q.persistenceLoad.sha256='9'*64;$q}
Check 'full request owner-seed exploration admitted' {Validate (OwnerSeedRequest)}
Check 'full request owner-seed exploration without assistance admitted' {$q=OwnerSeedRequest;$q.ownedPairs.xpAssistance=$null;Validate $q}
Check 'owner-seed selects the exploration engine with one load and zero writes' {$q=OwnerSeedRequest;if((Get-KmcChunk7SelectedScenario $q)-cne'chunk7-two-pair-exploration'-or(Get-KmcChunk7OwnedWriteCount $q)-ne0-or(Get-KmcChunk7OwnedLoadCount $q)-ne1){throw 'Owner-seed quota or engine differs'}}
# The installed game has no level-up autosave: the seed kind selects zero writes only.
Check 'full request owner-seed exploration with two setup level-up Autos refused' {$q=OwnerSeedRequest;$q.ownedPairs.expectedNativeWriteCount=2;Validate $q} $true
Check 'runner owner-seed final save root admits only the seed copy' {if(-not$runnerAst.Extent.Text.Contains('Assert-KmcChunk7OwnerSeedFinalSaveRoot ([pscustomobject]$request) $allLeaves')-or$runnerAst.Extent.Text.Contains("Owner-seed settled level-up Auto differs")){throw 'Owner-seed final-root check differs'}}
Check 'full request owner-seed exploration with the spaced registry seed name admitted' {$q=OwnerSeedRequest;$q.ownedPairs.seedId='KMC_C7_NO_MODS_OWNER_SEED';$q.persistenceLoad.internalName='KMC C7 NO MODS OWNER SEED';$q.persistenceLoad.fileName='Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks';Validate $q}
$ownerSeedNegative=[ordered]@{
 'rider not in party'={param($q)$q.ownedPairs.partyIds=@($q.ownedPairs.riderAId,'50505050-5050-4050-8050-505050505050')}
 'rider A equals rider B'={param($q)$q.ownedPairs.riderBId=$q.ownedPairs.riderAId;$q.ownedPairs.xpAssistance.unitId=$q.ownedPairs.riderAId}
 'assistance for rider A'={param($q)$q.ownedPairs.xpAssistance.unitId=$q.ownedPairs.riderAId}
 'assistance for another party actor'={param($q)$q.ownedPairs.xpAssistance.unitId='50505050-5050-4050-8050-505050505050'}
 'assistance beyond the setup ceiling'={param($q)$q.ownedPairs.xpAssistance.targetCharacterLevel=9}
 'assistance amount key'={param($q)$q.ownedPairs.xpAssistance|Add-Member experience 42000}
 'assistance key omitted'={param($q)$q.ownedPairs.PSObject.Properties.Remove('xpAssistance')}
 'producer run pin on the seed kind'={param($q)$q.ownedPairs|Add-Member sourceRunId 'synthetic-two-owner-manual'}
 'producer archive pin on the seed kind'={param($q)$q.ownedPairs|Add-Member sourceArchiveSha256 ('9'*64)}
 'pre-granted pet pin on the seed kind'={param($q)$q.ownedPairs|Add-Member mountAId '20202020-2020-4020-8020-202020202020'}
 'checkpoint on the seed kind'={param($q)$q|Add-Member persistenceCase 'multi-exploration-manual'}
 'native writes beyond one per rider build'={param($q)$q.ownedPairs.expectedNativeWriteCount=3}
 'one setup level-up Auto'={param($q)$q.ownedPairs.expectedNativeWriteCount=1}
 'two setup level-up Autos'={param($q)$q.ownedPairs.expectedNativeWriteCount=2}
 'negative native write count'={param($q)$q.ownedPairs.expectedNativeWriteCount=-1}
 'second load on the seed kind'={param($q)$q.ownedPairs.expectedNativeLoadCount=2}
 'seed SHA differs from the copied input'={param($q)$q.ownedPairs.seedSha256='8'*64}
 'seed input impersonates the producer fixture'={param($q)$q.persistenceLoad.internalName='KMC_C7_ASSISTED_FIXTURE';$q.persistenceLoad.fileName='Manual_9_KMC_C7_ASSISTED_FIXTURE.zks'}
 'seed campaign aliases Working'={param($q)$q.persistenceLoad.gameId=$q.fixture.working.gameId}
 'seed id aliases the run'={param($q)$q.ownedPairs.seedId=$q.runId}
}
foreach($n in $ownerSeedNegative.GetEnumerator()){Check ('full owner-seed request refuses '+$n.Key){$q=OwnerSeedRequest;&$n.Value $q;Validate $q} $true}
foreach($pin in @('seedId','seedSha256','xpAssistance')){Check ('full producer exploration refuses owner-seed pin '+$pin){$q=Request;$q.ownedPairs|Add-Member $pin $(if($pin-ceq'xpAssistance'){$null}else{'KMC_C7_OWNER_SEED'});Validate $q} $true}
$timeoutGuard=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text.Contains('Test-KmcChunk7OwnedPairsOwnerSeed')-and$n.Extent.Text.Contains('Owner-seed two-pair exploration requires its declared 900-second')},$true))
Check 'runner owner-seed outer budget guard is exact' {if($timeoutGuard.Count-ne1){throw 'Owner-seed budget guard missing'};$g=[scriptblock]::Create('param($Sel,$Bound,$Value) $Scenario="chunk7-owned-two-pair-campaign";$ownedPairsInput=[pscustomobject]@{selection=$Sel};$TimeoutSeconds=$Value;$PSBoundParameters=@{};if($Bound){$PSBoundParameters["TimeoutSeconds"]=$Value};'+[Environment]::NewLine+$timeoutGuard[0].Extent.Text+[Environment]::NewLine+'$TimeoutSeconds');$seed=(OwnerSeedRequest).ownedPairs;if((&$g $seed $false 600)-ne900-or(&$g (Request).ownedPairs $false 600)-ne600){throw 'Owner-seed budget default differs'};$refused=$false;try{[void](&$g $seed $true 600)}catch{$refused=$true};if(-not$refused){throw 'Short owner-seed budget accepted'}}
# Owner-seed-derived persistence: the producer keeps the owner-seed setup budget and one Manual
# write; the consumer is an ordinary owned cold load with one load and zero writes.
function SeedManualRequest{$q=OwnerSeedRequest;$q.ownedPairs.case='owner-seed-persistence-save';$q.ownedPairs.expectedNativeWriteCount=1;$q|Add-Member persistenceCase 'multi-exploration-manual' -Force;$q}
function SeedDerivedRequest{$q=Request 'persistence-load-multi-exploration-manual';$p=$q.ownedPairs;$q.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-persistence-load';sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$q.persistenceLoad.sha256;seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;mountAId=$p.mountAId;riderBId=$p.riderBId;mountBId=$p.mountBId;partyIds=$p.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0};$q.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$q.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks';$q}
Check 'full request owner-seed Manual producer admitted' {Validate (SeedManualRequest)}
Check 'owner-seed Manual producer selects the exploration engine with one load and one write' {$q=SeedManualRequest;if((Get-KmcChunk7SelectedScenario $q)-cne'chunk7-two-pair-exploration'-or(Get-KmcChunk7OwnedWriteCount $q)-ne1-or(Get-KmcChunk7OwnedLoadCount $q)-ne1){throw 'Producer quota or engine differs'}}
Check 'full request owner-seed Manual producer without its checkpoint refused' {$q=SeedManualRequest;$q.PSObject.Properties.Remove('persistenceCase');Validate $q} $true
Check 'full request owner-seed Manual producer with two writes refused' {$q=SeedManualRequest;$q.ownedPairs.expectedNativeWriteCount=2;Validate $q} $true
Check 'full request owner-seed-derived consumer admitted' {Validate (SeedDerivedRequest)}
Check 'owner-seed-derived consumer selects the cold persistence engine with one load and zero writes' {$q=SeedDerivedRequest;if((Get-KmcChunk7SelectedScenario $q)-cne'chunk7-persistence-load'-or(Get-KmcChunk7OwnedWriteCount $q)-ne0-or(Get-KmcChunk7OwnedLoadCount $q)-ne1){throw 'Consumer quota or engine differs'}}
Check 'full request owner-seed-derived consumer with an acquired-ancestry Manual refused' {$q=SeedDerivedRequest;$q.persistenceLoad.internalName='KMC_C7_WORLD';$q.persistenceLoad.fileName='Manual_303_KMC_C7_WORLD.zks';Validate $q} $true
Check 'full request owner-seed-derived consumer with a mismatched input hash refused' {$q=SeedDerivedRequest;$q.persistenceLoad.sha256='e'*64;Validate $q} $true
Check 'full request owner-seed-derived consumer with a write refused' {$q=SeedDerivedRequest;$q.ownedPairs.expectedNativeWriteCount=1;Validate $q} $true
Check 'runner owner-seed outer budget covers the Manual producer only' {$g=[scriptblock]::Create('param($Sel,$Bound,$Value) $Scenario="chunk7-owned-two-pair-campaign";$ownedPairsInput=[pscustomobject]@{selection=$Sel};$TimeoutSeconds=$Value;$PSBoundParameters=@{};if($Bound){$PSBoundParameters["TimeoutSeconds"]=$Value};'+[Environment]::NewLine+$timeoutGuard[0].Extent.Text+[Environment]::NewLine+'$TimeoutSeconds');if((&$g (SeedManualRequest).ownedPairs $false 600)-ne900-or(&$g (SeedDerivedRequest).ownedPairs $false 600)-ne600){throw 'Owner-seed-derived budgets differ'}}
Check 'runner reads the producer world evidence and pins the consumer to its producer' {$t=$runnerAst.Extent.Text;foreach($s in @('Assert-KmcChunk7OwnerSeedDerivedColdSource ([pscustomobject]$request) (Read-KmcJson $verifiedOwnedSource.artifactPath) $derivedColdArtifact',"if(`$seedManualRun){'chunk7-world-evidence.json'}",'Assert-KmcChunk7OwnerSeedManualFinalSaveRoot ([pscustomobject]$request)')){if(-not$t.Contains($s)){throw ('Runner owner-seed-derived step absent: '+$s)}}}
# The companion window accounts only boundaries the allocation trace actually emits, and the reader
# accounts exactly the producer's list (the interrupt hooks emit command-interrupt-*).
Check 'companion accounted boundaries are emitted by the allocation trace and mirrored by the reader' {
 $diag=Join-Path $bridgeRoot 'src/KingmakerMountedCombat/Diagnostics';$scenario=[IO.File]::ReadAllText((Join-Path $diag 'Chunk7CombatScenario.OwnerSeed.cs'));$traceText=[IO.File]::ReadAllText((Join-Path $diag 'NativeActorAllocationTrace.cs'))
 $m=[regex]::Match($scenario,'CompanionAccountedBoundaries\s*=\s*\{([^}]*)\}');if(-not$m.Success){throw 'Producer boundary list missing.'}
 $names=@([regex]::Matches($m.Groups[1].Value,'"([^"]+)"')|ForEach-Object{$_.Groups[1].Value});if($names.Count-ne8){throw 'Producer boundary list changed size.'}
 $emitted=@([regex]::Matches($traceText,'Record\("([a-z-]+)"')|ForEach-Object{$_.Groups[1].Value}|Select-Object -Unique)
 foreach($name in $names){if($name-cnotin$emitted){throw ('Accounted boundary never emitted by the trace: '+$name)}}
 $reader=ExactFunction (Ast (Join-Path $bridgeRuntime 'Chunk7OwnedPairsEvidence.ps1')) 'Assert-KmcChunk7OwnerSeedCompanionAction'
 $r=[regex]::Match($reader,'\$_\.boundary-cin@\(([^)]*)\)');$readerNames=@([regex]::Matches($r.Groups[1].Value,"'([^']+)'")|ForEach-Object{$_.Groups[1].Value})
 if(($readerNames-join'|')-cne($names-join'|')){throw 'Reader accounted boundaries differ from the producer.'}
 if($traceText-cnotmatch'declaredCommandOwner'-or$scenario-cnotmatch'trace\.DeclareCommandOwner\(companion\);\s*companionTraceOffset = trace\.EventCount;'){throw 'Companion admissions are not declared to the trace before the window opens.'}
}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $bridgeOutput 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$clock.Elapsed.TotalSeconds;gameLaunches=0;archivesCreated=0;archivesRead=0;scope='actual full request validator, named result functions, quota assignments and runner guard AST; OS repository/output binding substituted';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Owned wiring: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
