[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/chunk7twoownermanualevidence-'+[Guid]::NewGuid().ToString('N'))}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
function New-TwoOwnerManualFixture([bool]$Paid=$true){$name=if($Paid){'Paid'}else{'Existing'};[IO.File]::ReadAllText((Join-Path $FixtureDirectory ('TwoOwnerManual'+$name+'Fixture.json')))|ConvertFrom-Json}
function Check-TwoOwnerManual($F){Assert-KmcChunk7AcquiredManualEvidence $F.request $F.artifact $F.source}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$rejected=$false;try{&$Body}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS refuses '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
foreach($paid in @($true,$false)){Good ('closed two-owner Manual '+$paid){Check-TwoOwnerManual (New-TwoOwnerManualFixture $paid)}}
$cases=[ordered]@{
 'old source family substituted'={param($f)$f.source.scenario='chunk7-manual-derived-acquisition'}
 'prior selection aliases new actor'={param($f)$f.request.acquiredManual.priorOwnerId=$f.request.acquiredManual.actorId}
 'prior selection missing'={param($f)$f.request.acquiredManual.PSObject.Properties.Remove('priorPetId')}
 'source prior identity mismatched'={param($f)$f.source.observations.cumulative.settledPriorOwner.petId=$f.request.acquiredManual.petId}
 'source claims three owners'={param($f)$f.source.observations.cumulative.finalOwnerCount=3}
 'missing loaded prior receipt'={param($f)$f.artifact.observations.PSObject.Properties.Remove('loadedPriorOwner')}
 'missing final prior receipt'={param($f)$f.artifact.observations.PSObject.Properties.Remove('settledTwoOwners')}
 'two owner order reversed'={param($f)$a=$f.artifact.observations.twoOwners;$f.artifact.observations.twoOwners=@($a[1],$a[0])}
 'third explicit owner'={param($f)$f.artifact.observations.twoOwners+=,$f.artifact.observations.twoOwners[0]}
 'prior feature replaced'={param($f)$f.artifact.observations.twoOwners[0].featureObject++}
 'prior feature aliases new rank'={param($f)$f.artifact.observations.fieldTwoOwners[0].featureObject=$f.artifact.observations.fieldOwner.rankObject}
 'prior view aliases registered other role'={param($f)$f.artifact.observations.settledTwoOwners[0].actorView=$f.artifact.observations.settledWorld.state}
 'prior rank changed'={param($f)$f.artifact.observations.fieldTwoOwners[0].rank++}
 'prior source guid changed'={param($f)$f.artifact.observations.twoOwners[0].sourceGuid='f'*32}
 'prior actor commands pending'={param($f)$f.artifact.observations.loadedPriorOwner.actorCommandsEmpty=$false}
 'prior no longer field character'={param($f)$f.artifact.observations.twoOwners[0].partyCharacters=@($f.request.acquiredManual.actorId)}
 'new owner snapshot differs'={param($f)$f.artifact.observations.twoOwners[1].petId=$f.request.acquiredManual.priorPetId}
 'new extra role hidden'={param($f)$f.artifact.observations.twoOwners[1]|Add-Member -NotePropertyName foreignOwner -NotePropertyValue 'foreign'}
 'cold resource changed consistently after load'={param($f)foreach($k in @('resourcesBeforeField','resourcesAfterField','resourcesAfterSave')){($f.artifact.observations.$k.actors|Where-Object actorId -CEQ $f.request.acquiredManual.priorOwnerId).reactions++}}
 'prior resource at save lost'={param($f)$f.artifact.observations.resourcesAfterSave.actors[0].reactionAllowance++}
 'foreign native relationship'={param($f)($f.artifact.observations.nativeProfileInventory.party|Where-Object id -CEQ $f.request.acquiredManual.priorPetId).masterId=$f.request.acquiredManual.actorId}
 'worker still pending'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'native Prepare observed'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'writer quota repeated'={param($f)$f.artifact.observations.cleanup.authority.authorizedWrites=2}
 'input source mutated'={param($f)$f.artifact.observations.cleanup.sourceUnchanged=$false}
}
foreach($paid in @($true,$false)){foreach($c in $cases.GetEnumerator()){Bad ($paid.ToString()+' '+$c.Key){$f=New-TwoOwnerManualFixture $paid;&$c.Value $f;Check-TwoOwnerManual $f}}}
foreach($form in @('text','float','bool','array')){Bad ('prior role token '+$form){$f=New-TwoOwnerManualFixture;$p=$f.artifact.observations.twoOwners[0];$v=$p.actorObject;$p.actorObject=switch($form){'text'{[string]$v};'float'{[double]$v};'bool'{$true};'array'{,@($v)}};Check-TwoOwnerManual $f}}
@{schemaVersion=1;pass=$script:passes;fail=$script:failures;readerSha256=(Get-FileHash (Join-Path $ReaderDirectory 'Chunk7AcquiredManualEvidence.ps1')).Hash.ToLowerInvariant();nativeExecution=$false;archiveAccess=$false;fixtureKind='synthetic-json-only'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures){throw 'Two-owner Manual strict reader checks failed.'}
