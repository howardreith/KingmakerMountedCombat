[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OverlayDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
$ReaderDirectory=[IO.Path]::GetFullPath($ReaderDirectory)
if(-not$PSBoundParameters.ContainsKey('OverlayDirectory')){$OverlayDirectory=Join-Path $PSScriptRoot '../runtime'}
$OverlayDirectory=[IO.Path]::GetFullPath($OverlayDirectory)
. (Join-Path $PSScriptRoot 'Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
. (Join-Path $OverlayDirectory 'ScenarioDispatchEvidence.ps1')
$private=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/ordinary-wiring-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($private)
function Read-Ast([string]$Path){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors.Count){throw ($errors|Out-String)};return $ast}
function Exact-Function($Ast,[string]$Name){$matches=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($matches.Count-ne1){throw ('Function is missing or duplicated: '+$Name)};return $matches[0].Extent.Text}
function Commands($Ast,[string]$Name){@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}
$script:passes=0
foreach($leaf in @('Invoke-KingmakerRuntimeScenario.ps1','RuntimeHarness.Common.ps1','RuntimeArtifactManifestEvidence.ps1','ScenarioDispatchEvidence.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeRequest.ps1','Test-RuntimeResult.ps1','Chunk7CleanupEvidence.ps1')){
    Good ('parser '+$leaf) {[void](Read-Ast (Join-Path $OverlayDirectory $leaf))}
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionEvidence.ps1','Chunk7OrdinaryAcquisitionFixtures.ps1')){Good ('parser '+$leaf) {[void](Read-Ast (Join-Path $ReaderDirectory $leaf))}}
# Import the real save-backed registry only; no runner or transaction is invoked.
. ([scriptblock]::Create((Exact-Function (Read-Ast (Join-Path $OverlayDirectory 'RuntimeHarness.Common.ps1')) 'Get-KmcSaveBackedRuntimeScenarios')))
Good 'ordinary family is save-backed and compound' {if('chunk7-ordinary-acquisition'-cnotin(Get-KmcSaveBackedRuntimeScenarios)-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-ordinary-acquisition')){throw 'New family is not reachable through both exact registries.'}}
Bad 'similar unselected family remains closed' {if(-not(Test-KmcCompoundRuntimeScenario 'chunk7-ordinary-acquisition-extra')){throw 'correctly refused'}}
$metadata=Get-Command (Join-Path $OverlayDirectory 'Invoke-KingmakerRuntimeScenario.ps1')
Good 'guarded runner exposes explicit ordinary selection path' {if(-not$metadata.Parameters.ContainsKey('OrdinarySelectionPath')){throw 'Explicit selection parameter missing.'}}
$scenarioSet=@($metadata.Parameters['Scenario'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})
Good 'runner exact scenario metadata includes ordinary' {if($scenarioSet.Count-ne1-or'chunk7-ordinary-acquisition'-cnotin$scenarioSet[0].ValidValues){throw 'Ordinary scenario is not a closed selected runner case.'}}
$caseSet=@($metadata.Parameters['PersistenceCase'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})
foreach($case in Get-KmcChunk7PersistenceCases){Good ('historical selected persistence case preserved '+$case) {if($caseSet.Count-ne1-or$case-cnotin$caseSet[0].ValidValues){throw 'Existing persistence case lost.'}}}
$body=[IO.File]::ReadAllText((Join-Path $OverlayDirectory 'Test-RuntimeRequest.ps1'))
$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Request module import boundary changed.'}
$validator=[scriptblock]::Create($body.Replace($import,'# Common supplied by focused caller'))
function Descriptor([string]$Name,[string]$Hash){[pscustomobject]@{internalName=$Name;fileName='Manual_1_'+$Name+'.zks';sha256=$Hash;length=1024L;lastWriteTimeUtcTicks=1L;gameId='88888888-1111-4222-8333-aaaaaaaaaaaa';gameName='Synthetic';area='3'*32}}
function New-Request([bool]$Paid=$true){
    $q=(New-OrdinaryFixture $Paid).request
    $q.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion
    $q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId)
    $baseline=Descriptor 'KMC_AUTOMATION_BASELINE' ('4'*64);$working=Descriptor 'KMC_AUTOMATION_WORKING' $q.fixture.working.sha256
    $q.fixture=[pscustomobject]@{baseline=$baseline;working=$working;writeAuthorization=[pscustomobject]@{mode='working-only';allowedInternalName='KMC_AUTOMATION_WORKING';allowedFileName=$working.fileName;baselineImmutable=$true}}
    $q|Add-Member NoteProperty transactionToken ('6'*64)
    $q|Add-Member NoteProperty qualificationSuite ([pscustomobject]@{suiteId='synthetic-ordinary-suite';snapshotSha256='7'*64})
    return $q
}
function Invoke-Request($q){$path=Join-Path $private 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $q),[Text.UTF8Encoding]::new($false));& $validator -RequestPath $path|Out-Null}
foreach($paid in @($true,$false)){Good ('full actual request validator paid='+$paid) {Invoke-Request (New-Request $paid)}}
$mutations=[ordered]@{
 'missing selection'={param($q)$q.PSObject.Properties.Remove('ordinaryAcquisition')}
 'unselected case'={param($q)$q.ordinaryAcquisition.case='paid-any-race'}
 'body pin missing'={param($q)$q.ordinaryAcquisition.smallRaceGuid=$null}
 'two write requests'={param($q)$q.ordinaryAcquisition.expectedNativeWriteCount=2}
 'coerced write count'={param($q)$q.ordinaryAcquisition.expectedNativeWriteCount='1'}
 'other save leaf'={param($q)$q.ordinaryAcquisition.nativeWriteLeaf='Manual_300_KMC_C7_WORLD.zks'}
 'same-run inventory'={param($q)$q.ordinaryAcquisition.inventoryRunId=$q.runId}
 'foreign historical scenario'={param($q)$q.scenario='persistence-p01-save'}
 'schema one request'={param($q)$q.schemaVersion=1}
 'extra top-level acquisition grant'={param($q)$q|Add-Member NoteProperty grantXp 100}
 'extra selection acquisition grant'={param($q)$q.ordinaryAcquisition|Add-Member NoteProperty grantXp 100}
 'foreign persistence case'={param($q)$q|Add-Member NoteProperty persistenceCase 'multi-exploration-auto'}
 'cold archive input'={param($q)$q|Add-Member NoteProperty persistenceLoad (Descriptor 'KMC_C7_WORLD' ('a'*64))}
}
foreach($item in $mutations.GetEnumerator()){Bad ('full request '+$item.Key) {$q=New-Request;& $item.Value $q;Invoke-Request $q}}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
    . ([scriptblock]::Create((Exact-Function (Read-Ast (Join-Path $OverlayDirectory $leaf)) 'Assert-SubscenarioResults')))
    $names=@(Get-KmcChunk7OrdinaryRows|Where-Object{$_-cnotin@('chunk7-ordinary-acquisition','C7-ordinary-runtime','C7-ordinary-interrupted')})
    $v=[pscustomobject]@{scenario='chunk7-ordinary-acquisition';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=$names.Count;subscenarioPassCount=$names.Count;subscenarioFailCount=0;assertionPassCount=$names.Count;assertionFailCount=0}
    Good ($leaf+' exact ordinary rows') {Assert-SubscenarioResults $v}
    Bad ($leaf+' unregistered ordinary row') {$bad=Copy-O $v;$bad.subscenarioResults[0].name='C7-ordinary-manufactured';Assert-SubscenarioResults $bad}
}
$gameAst=Read-Ast (Join-Path $OverlayDirectory 'Test-RuntimeGameResult.ps1')
$quota=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$expectedNativeWrites'},$true))
if($quota.Count-ne1){throw 'Native write quota assignment is missing or ambiguous.'}
$quotaBlock=[scriptblock]::Create($quota[0].Extent.Text+[Environment]::NewLine+'return $expectedNativeWrites')
foreach($spec in @(@('chunk7-ordinary-acquisition',1,$null),@('chunk7-persistence-save',1,'multi-exploration-manual'),@('chunk7-persistence-load',0,'multi-exploration-manual'),@('chunk7-persistence-load',1,'legacy-schema1-resave-manual'),@('chunk7-persistence-load',1,'legacy-mounted-pending-tb-manual'),@('chunk7-two-pair-exploration',0,$null))){
    $game=[pscustomobject]@{scenario=$spec[0]};$request=New-Request;$alternating=$false;$campaignB=$false;$nativeBootstrap=(Test-KmcChunk7NativeBootstrapScenario $game.scenario)
    if($null-ne$spec[2]){$request|Add-Member -NotePropertyName persistenceCase -NotePropertyValue $spec[2]}
    Good ('actual native write quota '+$spec[0]+' '+$spec[2]) {if((& $quotaBlock)-ne$spec[1]){throw 'Actual write quota changed.'}}
}
$runnerAst=Read-Ast (Join-Path $OverlayDirectory 'Invoke-KingmakerRuntimeScenario.ps1')
$sourceCalls=@(Commands $runnerAst 'Get-KmcChunk7OrdinaryInventorySource');$copyCalls=@(Commands $runnerAst 'Copy-KmcChunk7OrdinaryInventory')
$lockCalls=@(Commands $runnerAst 'Open-KmcRuntimeLock');$snapshotCalls=@(Commands $runnerAst 'New-KmcPersistenceProfileSnapshot')
$entryCalls=@(Commands $runnerAst 'Enter-KmcWorkingSaveTransaction');$stageCalls=@(Commands $runnerAst 'Enter-KmcModsTransaction')
$approval=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
Good 'ordinary source continuity precedes approval and is repeated under lock before snapshot' {
    if($sourceCalls.Count-ne2-or$approval.Count-ne1-or$lockCalls.Count-ne1-or$snapshotCalls.Count-ne1-or
       $sourceCalls[0].Extent.StartOffset-ge$approval[0].Extent.StartOffset-or$approval[0].Extent.StartOffset-ge$lockCalls[0].Extent.StartOffset-or
       $lockCalls[0].Extent.StartOffset-ge$sourceCalls[1].Extent.StartOffset-or$sourceCalls[1].Extent.StartOffset-ge$snapshotCalls[0].Extent.StartOffset){throw 'Source/approval/lock/snapshot ordering changed.'}
}
Good 'ordinary immutable inventory copy is inside selected locked save transaction before mods staging' {
    if($copyCalls.Count-ne1-or$entryCalls.Count-ne1-or$stageCalls.Count-ne1-or
       $entryCalls[0].Extent.StartOffset-ge$copyCalls[0].Extent.StartOffset-or$copyCalls[0].Extent.StartOffset-ge$stageCalls[0].Extent.StartOffset){throw 'Native inventory copy escaped its transaction ordering.'}
}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; parser, exact request/completion and guarded ordering checks only; no runner invocation; fixtures='+$private)
