[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$RuntimeDirectory){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-wiring-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $PSScriptRoot 'Test-Chunk7SmallManualEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $RuntimeDirectory -FixturePath $FixturePath -OutputDirectory $OutputDirectory -FixturesOnly
$script:smallManualWiringRepository=[IO.Path]::GetFullPath($RepositoryRoot)
# Only the external repository-location lookup is substituted for a private runtime shadow.
function Get-KmcRepositoryRoot{return $script:smallManualWiringRepository}
$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Action){$caught=$false;try{&$Action}catch{if($_.Exception-is[Management.Automation.CommandNotFoundException]){throw};$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
function AstText([string]$Text){$t=$null;$e=$null;$a=[Management.Automation.Language.Parser]::ParseInput($Text,[ref]$t,[ref]$e);if($e){throw ($e|Out-String)};return $a}
function Commands($A,[string]$Name){@($A.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}
function HasGuard($Node,[string[]]$Conditions){for($p=$Node.Parent;$null-ne$p;$p=$p.Parent){if($p-is[Management.Automation.Language.IfStatementAst]){foreach($c in $p.Clauses){if($c.Item1.Extent.Text-cin$Conditions-and$Node.Extent.StartOffset-gt$c.Item2.Extent.StartOffset-and$Node.Extent.EndOffset-lt$c.Item2.Extent.EndOffset){return $true}}}};return $false}
function Flow([string]$Text){
 $a=AstText $Text;$sources=@(Commands $a 'Get-KmcChunk7SmallManualSource');$copies=@(Commands $a 'Copy-KmcChunk7SmallManualSourceArtifact');$locks=@(Commands $a 'Open-KmcRuntimeLock');$snapshots=@(Commands $a 'New-KmcPersistenceProfileSnapshot');$entries=@(Commands $a 'Enter-KmcWorkingSaveTransaction');$stages=@(Commands $a 'Enter-KmcModsTransaction');$approval=@($a.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
 if($sources.Count-ne3-or$copies.Count-ne1-or$locks.Count-ne1-or$snapshots.Count-ne1-or$entries.Count-ne1-or$stages.Count-ne1-or$approval.Count-ne1){throw 'Exact Small Manual proof/copy or transaction counts differ.'}
 $expected=@('Get-KmcChunk7SmallManualSource $smallSelectionInput.selection.manualSource $fixturePayload $manifest','Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource $fixturePayload $manifest','Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource $fixturePayload $manifest')
 $family=@('Test-KmcChunk7SmallScenario $Scenario','$gamePassed-and(Test-KmcChunk7SmallScenario $Scenario)')
 for($i=0;$i-lt3;$i++){if($sources[$i].Extent.Text-cne$expected[$i]-or-not(HasGuard $sources[$i] $family)-or-not(HasGuard $sources[$i] @('Test-KmcChunk7SmallManualCase $smallSelectionInput.selection','Test-KmcChunk7SmallManualCase $request.smallCalibration'))){throw 'Manual source proof has wrong arguments or family/case guard.'}}
 if($copies[0].Extent.Text-cne'Copy-KmcChunk7SmallManualSourceArtifact $smallSource $profileRoot $lock'-or-not(HasGuard $copies[0] $family)-or-not(HasGuard $copies[0] @('Test-KmcChunk7SmallManualCase $request.smallCalibration'))){throw 'Manual source copy has wrong arguments or family/case guard.'}
 if(-not($sources[0].Extent.StartOffset-lt$approval[0].Extent.StartOffset-and$approval[0].Extent.StartOffset-lt$locks[0].Extent.StartOffset-and$locks[0].Extent.StartOffset-lt$sources[1].Extent.StartOffset-and$sources[1].Extent.StartOffset-lt$snapshots[0].Extent.StartOffset-and$snapshots[0].Extent.StartOffset-lt$entries[0].Extent.StartOffset-and$entries[0].Extent.StartOffset-lt$copies[0].Extent.StartOffset-and$copies[0].Extent.StartOffset-lt$stages[0].Extent.StartOffset-and$stages[0].Extent.StartOffset-lt$sources[2].Extent.StartOffset)){throw 'Manual source admission/copy/recheck ordering differs.'}
 $oldSources=@(Commands $a 'Get-KmcChunk7FieldSource'|Where-Object{$_.Extent.Text-cmatch'small|Small'});$oldCopies=@(Commands $a 'Copy-KmcChunk7FieldSourceArtifact'|Where-Object{$_.Extent.Text-cmatch'small|Small'});if($oldSources.Count-ne3-or$oldCopies.Count-ne1){throw 'Legacy source guards were removed.'}
}
$runner=[IO.File]::ReadAllText((Join-Path $RuntimeDirectory 'Invoke-KingmakerRuntimeScenario.ps1'))
Good 'actual AST exact3 source proofs and1 copy with family/case and full transaction order' {Flow $runner}
$mutations=[ordered]@{
 'other source cannot replace initial Manual proof'={param($t)$t.Replace('Get-KmcChunk7SmallManualSource $smallSelectionInput.selection.manualSource','Get-KmcChunk7FieldSource $smallSelectionInput.selection.manualSource')}
 'other copy cannot replace Manual copy'={param($t)$t.Replace('Copy-KmcChunk7SmallManualSourceArtifact $smallSource','Copy-KmcChunk7FieldSourceArtifact $smallSource')}
 'wrong first family guard'={param($t)$t.Replace('if(Test-KmcChunk7SmallScenario $Scenario){$smallSource=','if(Test-KmcChunk7FieldScenario $Scenario){$smallSource=')}
 'wrong first case guard'={param($t)$t.Replace('if(Test-KmcChunk7SmallManualCase $smallSelectionInput.selection)','if(Test-KmcChunk7FieldScenario $smallSelectionInput.selection)')}
 'old DTO cannot supply new proof'={param($t)$t.Replace('Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource','Get-KmcChunk7SmallManualSource $request.smallCalibration.source')}
 'copy requires exact lock object'={param($t)$t.Replace('Copy-KmcChunk7SmallManualSourceArtifact $smallSource $profileRoot $lock','Copy-KmcChunk7SmallManualSourceArtifact $smallSource $profileRoot $foreignLock')}
 'postproof needs Small family'={param($t)$t.Replace('if($gamePassed-and(Test-KmcChunk7SmallScenario $Scenario))','if($gamePassed-and(Test-KmcChunk7FieldScenario $Scenario))')}
 'extra proof is not accepted'={param($t)$t+[Environment]::NewLine+'Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource $fixturePayload $manifest'}
}
foreach($m in $mutations.GetEnumerator()){$changed=&$m.Value $runner;if($changed-ceq$runner){throw 'Mutation failed to bind'};Bad ('actual AST '+$m.Key){Flow $changed}}
$body=[IO.File]::ReadAllText((Join-Path $RuntimeDirectory 'Test-RuntimeRequest.ps1'));$import=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')";if(([regex]::Matches($body,[regex]::Escape($import))).Count-ne1){throw 'Actual request import boundary changed.'};$validator=[scriptblock]::Create($body.Replace($import,'# Exact Common already loaded; all validator bodies unchanged.'))
function Descriptor([string]$Name,[string]$Hash){[pscustomobject]@{internalName=$Name;fileName='Manual_1_'+$Name+'.zks';sha256=$Hash;length=1024L;lastWriteTimeUtcTicks=1L;gameId='88888888-1111-4222-8333-aaaaaaaaaaaa';gameName='Synthetic';area='3'*32}}
function Request{$q=(New-SmallManualFixture).request;$q.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion;$q.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$q.runId);$b=Descriptor 'KMC_AUTOMATION_BASELINE' ('4'*64);$w=Descriptor 'KMC_AUTOMATION_WORKING' $q.fixture.working.sha256;$q.fixture=[pscustomobject]@{baseline=$b;working=$w;writeAuthorization=[pscustomobject]@{mode='working-only';allowedInternalName=$w.internalName;allowedFileName=$w.fileName;baselineImmutable=$true}};$q|Add-Member transactionToken ('6'*64);$q|Add-Member qualificationSuite ([pscustomobject]@{suiteId='synthetic-small-manual-suite';snapshotSha256='7'*64});return $q}
function Validate($Q){$p=Join-Path $OutputDirectory 'request.json';[IO.File]::WriteAllText($p,(ConvertTo-KmcChunk7Canonical $Q),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $p|Out-Null}
Good 'full actual request admits exact compiled Manual source case' {Validate (Request)}
$invalid=[ordered]@{
 'foreign family'={param($q)$q.scenario='chunk7-ordinary-field-admission'}
 'old case borrowing Manual DTO'={param($q)$q.smallCalibration.case='compiled-small-horse-rt'}
 'old source property'={param($q)$q.smallCalibration|Add-Member source $q.smallCalibration.manualSource}
 'missing Manual input'={param($q)$q.PSObject.Properties.Remove('persistenceLoad')}
 'mutable output alias'={param($q)$q|Add-Member persistenceCase 'multi-exploration-manual'}
 'wrong file type'={param($q)$q.persistenceLoad.fileName='Auto_1.zks'}
 'wrong artifact hash'={param($q)$q.smallCalibration.manualSource.sourceArchiveSha256='a'*64}
 'two-owner DTO injection'={param($q)$q.smallCalibration.manualSource|Add-Member priorOwnerId $q.smallCalibration.manualSource.actorId}
}
foreach($v in $invalid.GetEnumerator()){Bad ('full actual request '+$v.Key){$q=Request;&$v.Value $q;Validate $q}}
$gameAst=AstText ([IO.File]::ReadAllText((Join-Path $RuntimeDirectory 'Test-RuntimeGameResult.ps1')))
foreach($name in @('expectedNativeWrites','expectedWorkingLoads')){$a=@($gameAst.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq('$'+$name)},$true));if($a.Count-ne1){throw 'Exact quota boundary differs.'};$quota=[scriptblock]::Create($a[0].Extent.Text+[Environment]::NewLine+'return $'+$name);$game=[pscustomobject]@{scenario='chunk7-small-profile-calibration'};$request=Request;$alternating=$false;$campaignB=$false;$absentKmc=$false;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $game.scenario;$wanted=if($name-ceq'expectedNativeWrites'){0}else{1};Good ('actual completed result quota '+$name){if((&$quota)-ne$wanted){throw 'Read-only Manual quota changed.'}}}
$watch.Stop();@{pass=$script:pass;fail=$script:fail;seconds=$watch.Elapsed.TotalSeconds;runnerSha256=(Get-FileHash -LiteralPath (Join-Path $RuntimeDirectory 'Invoke-KingmakerRuntimeScenario.ps1')).Hash.ToLowerInvariant();requestValidatorSha256=(Get-FileHash -LiteralPath (Join-Path $RuntimeDirectory 'Test-RuntimeRequest.ps1')).Hash.ToLowerInvariant();sourceScope='actual request validator, AST source/copy/family/order and quota bodies; external repository-location lookup substituted; runner never invoked';nativeInvoked=$false;archiveReads=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS '+$script:pass+' FAIL '+$script:fail);if($script:fail){throw 'Small Manual wiring checks failed.'}
