[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/field-wiring-'+[Guid]::NewGuid().ToString('N'))}
$wiringRuntime=[IO.Path]::GetFullPath($RuntimeDirectory);$wiringOutput=[IO.Path]::GetFullPath($OutputDirectory);[void][IO.Directory]::CreateDirectory($wiringOutput)
. (Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $wiringRuntime -FixturesOnly
. (Join-Path $wiringRuntime 'ScenarioDispatchEvidence.ps1')
. (Join-Path $wiringRuntime 'RuntimeArtifactManifestEvidence.ps1')
. (Join-Path $wiringRuntime 'Chunk7CleanupEvidence.ps1')
function Ast([string]$Path){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $ast}
function Exact-Function($Ast,[string]$Name){$v=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($v.Count-ne1){throw ('Exact function missing: '+$Name)};$v[0].Extent.Text}
function Commands($Ast,[string]$Name){@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}

# Field and Small deliberately reuse the same source/copy helpers. A call is a
# Field proof only when both its exact arguments and its own positive family
# clause match. Merely finding the shared command name cannot establish this.
function Exact-Arguments($Command,[string[]]$Arguments){
    if($Command.CommandElements.Count-ne($Arguments.Count+1)){return $false}
    for($i=0;$i-lt$Arguments.Count;$i++){if($Command.CommandElements[$i+1].Extent.Text-cne$Arguments[$i]){return $false}}
    return $true
}
function Enclosing-Clause($Command){
    for($parent=$Command.Parent;$null-ne$parent;$parent=$parent.Parent){
        if($parent-isnot[Management.Automation.Language.IfStatementAst]){continue}
        foreach($clause in $parent.Clauses){
            if($Command.Extent.StartOffset-ge$clause.Item2.Extent.StartOffset-and$Command.Extent.EndOffset-le$clause.Item2.Extent.EndOffset){return $clause.Item1}
        }
        return $null # An else arm is not authorized by the preceding if guard.
    }
    return $null
}
function Exact-FieldCondition($Condition,[bool]$AfterNative=$false){
    if($Condition-isnot[Management.Automation.Language.PipelineAst]-or$Condition.PipelineElements.Count-ne1){return $false}
    $element=$Condition.PipelineElements[0]
    if($AfterNative){
        if($element-isnot[Management.Automation.Language.CommandExpressionAst]){return $false}
        $and=$element.Expression
        if($and-isnot[Management.Automation.Language.BinaryExpressionAst]-or$and.Operator-ne[Management.Automation.Language.TokenKind]::And-or
           $and.Left-isnot[Management.Automation.Language.VariableExpressionAst]-or$and.Left.VariablePath.UserPath-cne'gamePassed'-or
           $and.Right-isnot[Management.Automation.Language.ParenExpressionAst]){return $false}
        return Exact-FieldCondition $and.Right.Pipeline
    }
    return $element-is[Management.Automation.Language.CommandAst]-and$element.GetCommandName()-ceq'Test-KmcChunk7FieldScenario'-and
        $element.CommandElements.Count-eq2-and$element.CommandElements[1]-is[Management.Automation.Language.VariableExpressionAst]-and
        $element.CommandElements[1].VariablePath.UserPath-ceq'Scenario'-and$element.Redirections.Count-eq0
}
function Field-Sources($Ast){
    foreach($command in @(Commands $Ast 'Get-KmcChunk7FieldSource')){
        $argsMatch=(Exact-Arguments $command @('$fieldSelectionInput.selection','$fixturePayload','$manifest'))-or
            (Exact-Arguments $command @('$request.ordinaryField','$fixturePayload','$manifest'))
        $condition=Enclosing-Clause $command
        if($argsMatch-and((Exact-FieldCondition $condition)-or(Exact-FieldCondition $condition $true))){$command}
    }
}
function Field-Copies($Ast){
    foreach($command in @(Commands $Ast 'Copy-KmcChunk7FieldSourceArtifact')){
        if((Exact-Arguments $command @('$fieldSource','$profileRoot','$lock'))-and(Exact-FieldCondition (Enclosing-Clause $command))){$command}
    }
}
function Assert-FieldSourceOrder($Ast){
    $sources=@(Field-Sources $Ast);$lock=@(Commands $Ast 'Open-KmcRuntimeLock');$snapshot=@(Commands $Ast 'New-KmcPersistenceProfileSnapshot');$stage=@(Commands $Ast 'Enter-KmcModsTransaction')
    $approval=@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
    if($sources.Count-ne3-or$approval.Count-ne1-or$lock.Count-ne1-or$snapshot.Count-ne1-or$stage.Count-ne1-or
       -not(Exact-Arguments $sources[0] @('$fieldSelectionInput.selection','$fixturePayload','$manifest'))-or
       -not(Exact-Arguments $sources[1] @('$request.ordinaryField','$fixturePayload','$manifest'))-or
       -not(Exact-Arguments $sources[2] @('$request.ordinaryField','$fixturePayload','$manifest'))-or
       -not(Exact-FieldCondition (Enclosing-Clause $sources[0]))-or-not(Exact-FieldCondition (Enclosing-Clause $sources[1]))-or
       -not(Exact-FieldCondition (Enclosing-Clause $sources[2]) $true)-or
       $sources[0].Extent.StartOffset-ge$approval[0].Extent.StartOffset-or$approval[0].Extent.StartOffset-ge$lock[0].Extent.StartOffset-or
       $lock[0].Extent.StartOffset-ge$sources[1].Extent.StartOffset-or$sources[1].Extent.StartOffset-ge$snapshot[0].Extent.StartOffset-or
       $stage[0].Extent.StartOffset-ge$sources[2].Extent.StartOffset){throw 'Field source transaction ordering differs.'}
}
function Assert-FieldCopyOrder($Ast){
    $copies=@(Field-Copies $Ast);$snapshot=@(Commands $Ast 'New-KmcPersistenceProfileSnapshot');$entry=@(Commands $Ast 'Enter-KmcWorkingSaveTransaction');$stage=@(Commands $Ast 'Enter-KmcModsTransaction')
    if($copies.Count-ne1-or$snapshot.Count-ne1-or$entry.Count-ne1-or$stage.Count-ne1-or
       $snapshot[0].Extent.StartOffset-ge$entry[0].Extent.StartOffset-or$entry[0].Extent.StartOffset-ge$copies[0].Extent.StartOffset-or
       $copies[0].Extent.StartOffset-ge$stage[0].Extent.StartOffset){throw 'Field immutable copy escaped owned transaction.'}
}
function Assert-FieldTransaction($Ast){Assert-FieldSourceOrder $Ast;Assert-FieldCopyOrder $Ast}
function Runner-TextAst([string]$Text){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseInput($Text,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $ast}
function Replace-Node([string]$Text,$Node,[string]$Replacement){return $Text.Substring(0,$Node.Extent.StartOffset)+$Replacement+$Text.Substring($Node.Extent.EndOffset)}

$script:passes=0
foreach($file in Get-ChildItem -LiteralPath $wiringRuntime -File -Filter '*.ps1'){Good ('parse '+$file.Name) {[void](Ast $file.FullName)}}
. ([scriptblock]::Create((Exact-Function (Ast (Join-Path $wiringRuntime 'RuntimeHarness.Common.ps1')) 'Get-KmcSaveBackedRuntimeScenarios')))
Good 'field admission registered once in exact save-backed and compound lists' {if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object{$_-ceq'chunk7-ordinary-field-admission'}).Count-ne1-or-not(Test-KmcCompoundRuntimeScenario 'chunk7-ordinary-field-admission')){throw 'Field family missing or duplicated.'}}
foreach($name in @('chunk7-ordinary-field-any','CHUNK7-ORDINARY-FIELD-ADMISSION')){Good ('closed family refuses '+$name){if(Test-KmcChunk7FieldScenario $name){throw 'Inexact family admitted.'}}}
$metadata=Get-Command (Join-Path $wiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1')
Good 'explicit field selection runner parameter and exact enum value' {if(-not$metadata.Parameters.ContainsKey('OrdinaryFieldSelectionPath')-or'chunk7-ordinary-field-admission'-cnotin@($metadata.Parameters['Scenario'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues){throw 'Field runner admission absent.'}}
$body=[IO.File]::ReadAllText((Join-Path $wiringRuntime 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed.'}
$validator=[scriptblock]::Create($body.Replace($import,'# Existing Common loaded by caller; exact validator body follows.'))
function Descriptor([string]$Name,[string]$Hash){[pscustomobject]@{internalName=$Name;fileName='Manual_1_'+$Name+'.zks';sha256=$Hash;length=1024L;lastWriteTimeUtcTicks=1L;gameId='88888888-1111-4222-8333-aaaaaaaaaaaa';gameName='Synthetic';area='3'*32}}
function Request {
    $q=(New-FieldFixture).request;$q.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion
    $q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId)
    $b=Descriptor 'KMC_AUTOMATION_BASELINE' ('4'*64);$w=Descriptor 'KMC_AUTOMATION_WORKING' $q.fixture.working.sha256
    $q.fixture=[pscustomobject]@{baseline=$b;working=$w;writeAuthorization=[pscustomobject]@{mode='working-only';allowedInternalName=$w.internalName;allowedFileName=$w.fileName;baselineImmutable=$true}}
    $q|Add-Member -NotePropertyName transactionToken -NotePropertyValue ('6'*64)
    $q|Add-Member -NotePropertyName qualificationSuite -NotePropertyValue ([pscustomobject]@{suiteId='synthetic-field-suite';snapshotSha256='7'*64});return $q
}
function Validate($Q){$path=Join-Path $wiringOutput 'request.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $path|Out-Null}
Good 'full actual field request validator accepts exact zero-write cold bridge' {Validate (Request)}
# The runner holds its request as an ordered dictionary until serialization; the field selection assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
Good 'field selection assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7FieldSelection (ConvertTo-RunnerRequest (Request))}
Bad 'ordered-dictionary request without its selection' {$r=ConvertTo-RunnerRequest (Request);$r.Remove('ordinaryField');Assert-KmcChunk7FieldSelection $r}
Bad 'ordered-dictionary foreign family carrying a selection' {$r=ConvertTo-RunnerRequest (Request);$r['scenario']='fixture-intake';Assert-KmcChunk7FieldSelection $r}
$cases=[ordered]@{
 'missing field selection'={param($q)$q.PSObject.Properties.Remove('ordinaryField')}
 'missing source archive'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'historical family borrowing selection'={param($q)$q.scenario='persistence-p05-load'}
 'unexpected native write case'={param($q)$q|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-auto'}
 'extra source permission'={param($q)$q.ordinaryField|Add-Member -NotePropertyName nativeWrites -NotePropertyValue 1}
 'extra remote actor'={param($q)$q.ordinaryField|Add-Member -NotePropertyName secondActorId -NotePropertyValue $q.ordinaryField.actorId}
 'wrong native leaf'={param($q)$q.persistenceLoad.fileName='Manual_300_KMC_C7_WORLD.zks'}
 'wrong source bytes'={param($q)$q.persistenceLoad.sha256='9'*64}
 'source Working alias'={param($q)$q.persistenceLoad.sha256=$q.fixture.working.sha256;$q.ordinaryField.sourceArchiveSha256=$q.fixture.working.sha256}
 'schema one'={param($q)$q.schemaVersion=1}
}
foreach($case in $cases.GetEnumerator()){Bad ('full request '+$case.Key){$q=Request;&$case.Value $q;Validate $q}}
foreach($leaf in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
    . ([scriptblock]::Create((Exact-Function (Ast (Join-Path $wiringRuntime $leaf)) 'Assert-SubscenarioResults')))
    $names=@('C7-field-readonly-source','C7-field-native-party-placement','C7-field-cleanup')
    $v=[pscustomobject]@{scenario='chunk7-ordinary-field-admission';subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=3;subscenarioPassCount=3;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0}
    Good ($leaf+' exact field rows'){Assert-SubscenarioResults $v}
    Bad ($leaf+' foreign manufactured row'){$q=Copy-O $v;$q.subscenarioResults[0].name='C7-field-manufactured';Assert-SubscenarioResults $q}
}
$gameAst=Ast (Join-Path $wiringRuntime 'Test-RuntimeGameResult.ps1')
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){
    $assign=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($assign.Count-ne1){throw 'Exact native quota boundary differs.'}
    $quota=[scriptblock]::Create($assign[0].Extent.Text+"`nreturn `$"+$name);$game=[pscustomobject]@{scenario='chunk7-ordinary-field-admission'};$request=Request;$assistedFixture=$false;$assistedWriteCount=0;$alternating=$false;$campaignB=$false;$absentKmc=$false;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $game.scenario
    $wantedQuota=if($name-ceq'expectedNativeWrites'){0}else{1}
    Good ('field actual native quota '+$name){if((&$quota)-ne$wantedQuota){throw 'Field request quota changed.'}}
}
$runnerPath=Join-Path $wiringRuntime 'Invoke-KingmakerRuntimeScenario.ps1'
$runnerAst=Ast $runnerPath;$runnerText=[IO.File]::ReadAllText($runnerPath)
Good 'source proven before approval and again under lock before snapshot then rechecked after native' {Assert-FieldSourceOrder $runnerAst}
Good 'immutable source copy remains inside owned Working transaction before Mods staging' {Assert-FieldCopyOrder $runnerAst}
$sources=@(Field-Sources $runnerAst);$copies=@(Field-Copies $runnerAst)
$smallSources=@(Commands $runnerAst 'Get-KmcChunk7FieldSource'|Where-Object{(Exact-Arguments $_ @('$smallSelectionInput.selection.source','$fixturePayload','$manifest'))-or(Exact-Arguments $_ @('$request.smallCalibration.source','$fixturePayload','$manifest'))})
$smallCopies=@(Commands $runnerAst 'Copy-KmcChunk7FieldSourceArtifact'|Where-Object{Exact-Arguments $_ @('$smallSource','$profileRoot','$lock')})
Good 'shared Small helpers remain separate from all three Field proofs and one copy' {if($smallSources.Count-ne3-or$smallCopies.Count-ne1-or$sources.Count-ne3-or$copies.Count-ne1){throw 'Shared-family test control changed.'}}
for($phase=0;$phase-lt3;$phase++){
    Bad ('Small proofs cannot replace missing Field phase '+$phase){$text=Replace-Node $runnerText $sources[$phase] '$null';Assert-FieldTransaction (Runner-TextAst $text)}
    Bad ('Field proof refuses Small family guard phase '+$phase){$guard=Enclosing-Clause $sources[$phase];$text=Replace-Node $runnerText $guard ($guard.Extent.Text.Replace('Test-KmcChunk7FieldScenario','Test-KmcChunk7SmallScenario'));Assert-FieldTransaction (Runner-TextAst $text)}
}
Bad 'Small copy cannot replace missing Field copy' {$text=Replace-Node $runnerText $copies[0] '$null';Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'Field copy refuses Small family guard' {$guard=Enclosing-Clause $copies[0];$text=Replace-Node $runnerText $guard ($guard.Extent.Text.Replace('Test-KmcChunk7FieldScenario','Test-KmcChunk7SmallScenario'));Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'Small arguments under Field guard cannot count as Field proof' {$text=Replace-Node $runnerText $sources[0] $smallSources[0].Extent.Text;Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'Small arguments under Field guard cannot count as Field copy' {$text=Replace-Node $runnerText $copies[0] $smallCopies[0].Extent.Text;Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'Field family guard must inspect actual Scenario variable' {$guard=Enclosing-Clause $sources[0];$text=Replace-Node $runnerText $guard 'Test-KmcChunk7FieldScenario $AnotherScenario';Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'Field family OR bypass cannot authorize source proof' {$guard=Enclosing-Clause $sources[0];$text=Replace-Node $runnerText $guard '((Test-KmcChunk7FieldScenario $Scenario)-or$true)';Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'post-native proof retains gamePassed conjunction' {$guard=Enclosing-Clause $sources[2];$text=Replace-Node $runnerText $guard 'Test-KmcChunk7FieldScenario $Scenario';Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'post-native proof inverts no success or family condition' {$guard=Enclosing-Clause $sources[2];$text=Replace-Node $runnerText $guard '$gamePassed -and (-not (Test-KmcChunk7FieldScenario $Scenario))';Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'duplicate Field proof cannot hide among shared calls' {$text=Replace-Node $runnerText $sources[0] ($sources[0].Extent.Text+';'+$sources[0].Extent.Text);Assert-FieldTransaction (Runner-TextAst $text)}
Bad 'duplicate Field copy cannot hide among shared calls' {$text=Replace-Node $runnerText $copies[0] ($copies[0].Extent.Text+';'+$copies[0].Extent.Text);Assert-FieldTransaction (Runner-TextAst $text)}

function ManifestFixture([bool]$Pending=$false){
    $f=New-FieldFixture;$f.request.evidenceRoot=Join-Path $wiringOutput ('manifest-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($f.request.evidenceRoot)
    $r=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($f.request.evidenceRoot))) ('runtime-staging\persistence-'+$f.request.runId+'\Saved Games')
    $f.artifact.observations.authority.root=$r;$f.artifact.observations.cleanup.authority.root=$r
    $kind='chunk7-ordinary-field-evidence'
    if($Pending){$kind='chunk7-ordinary-field-cleanup-pending';$f.artifact.evidenceKind=$kind;$f.artifact.status='FAIL';$f.artifact.errors=@('native UI retained');$f.artifact.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true}}
    $path=Join-Path $f.request.evidenceRoot ($kind+'.json');[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $f.artifact),[Text.UTF8Encoding]::new($false))
    $m=[pscustomobject]@{schemaVersion=1;runId=$f.request.runId;scenario=$f.request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$kind+'.json';kind=$kind;length=[long](Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})}
    $f|Add-Member -NotePropertyName manifest -NotePropertyValue $m;return $f
}
function Manifest($F){$path=Join-Path $F.request.evidenceRoot 'runtime-artifacts.json';[IO.File]::WriteAllText($path,(ConvertTo-KmcChunk7Canonical $F.manifest),[Text.UTF8Encoding]::new($false));Assert-KmcReadOnlyArtifactManifest $F.request (Get-KmcSha256 $path) -GameResult}
Good 'exact field native artifact registered with strict hashed envelope' {$f=ManifestFixture;Manifest $f;Assert-KmcChunk7FieldArtifact $f.request $f.manifest 'PASS'}
Bad 'field artifact wrong registered kind' {$f=ManifestFixture;$f.manifest.artifacts[0].kind='chunk7-ordinary-acquisition-evidence';Manifest $f}
Bad 'field artifact under arbitrary leaf' {$f=ManifestFixture;$f.manifest.artifacts[0].relativePath='field-any.json';Manifest $f}
Bad 'field artifact belongs to foreign scenario' {$f=ManifestFixture;$f.request.scenario='chunk7-ordinary-acquisition';Assert-KmcChunk7FieldArtifact $f.request $f.manifest}
Good 'retained field custody receipt is admitted only as FAIL' {$f=ManifestFixture $true;Manifest $f;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Bad 'retained field custody cannot be promoted to PASS' {$f=ManifestFixture $true;Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'PASS'}
Bad 'foreign scenario cannot borrow field custody receipt' {$f=ManifestFixture $true;$f.request.scenario='chunk7-two-pair-exploration';Assert-KmcChunk7CleanupArtifacts $f.request $f.manifest 'FAIL'}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; exact AST/request/reader/quota only; no guarded runner invocation')
