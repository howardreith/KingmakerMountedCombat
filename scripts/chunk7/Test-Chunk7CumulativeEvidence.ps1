[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/chunk7cumulativeevidence-'+[Guid]::NewGuid().ToString('N'))}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'Convert-Chunk7CumulativeManualFixture.ps1')
function New-CumulativeFixture([bool]$Paid=$true){$name=if($Paid){'Paid'}else{'Existing'};$historical=[IO.File]::ReadAllText((Join-Path $FixtureDirectory ('Cumulative'+$name+'Fixture.json')))|ConvertFrom-Json;Convert-KmcSyntheticCumulativeManualFixture $historical $FixtureDirectory $name}
function Check-Cumulative($F){Assert-KmcChunk7CumulativeEvidence $F.request $F.artifact $F.source}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$rejected=$false;try{&$Body}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS refuses '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
foreach($paid in @($true,$false)){Good ('closed cumulative '+$paid){Check-Cumulative (New-CumulativeFixture $paid)}}
Good 'cumulative Manual native choices omit the Auto observer' {
 $f=New-CumulativeFixture;$f.request.cumulativeAcquisition|Add-Member -NotePropertyName sourceMode -NotePropertyValue 'assisted-producer-and-cold'
 $choice=Assert-KmcManualDerivedNativeChoices $f.request $f.artifact.observations $f.request.cumulativeAcquisition
 if($choice.actorId-cne$f.artifact.observations.field.actorId){throw 'Manual choice identity differs.'}
}
Bad 'frozen Auto fixture cannot pass the new Manual reader' {
 $f=[IO.File]::ReadAllText((Join-Path $FixtureDirectory 'CumulativePaidFixture.json'))|ConvertFrom-Json
 Check-Cumulative $f
}
$cases=[ordered]@{
 'one-owner missing prior pin'={param($f)$f.request.cumulativeAcquisition.priorOwnerId=$null}
 'prior actor chosen for second build'={param($f)$f.request.cumulativeAcquisition.existingActorId=$f.request.cumulativeAcquisition.priorOwnerId}
 'prior pet is party character'={param($f)$f.request.cumulativeAcquisition.readiness.partyCharacterIds+=,$f.request.cumulativeAcquisition.priorPetId}
 'foreign candidate'={param($f)$f.source.dllSha256='e'*64}
 'third-owner source family'={param($f)$f.source.observations.selection.case='preserve-two-acquired-field'}
 'different valid source number'={param($f)$f.request.persistenceLoad.fileName='Manual_305_KMC_C7_ACQUIRED_FIELD.zks'}
 'source relationship reversed'={param($f)$f.source.observations.nativeProfileInventory.party[1].masterId=$f.artifact.observations.field.actorId}
 'prior pair missing in source'={param($f)$f.source.observations.nativeProfileInventory.party[0].petId=$null}
 'prior field owner remote'={param($f)$f.source.observations.savedOwner.remoteCompanions=@($f.request.cumulativeAcquisition.priorOwnerId)}
 'prior descriptor changed'={param($f)$f.artifact.observations.cumulative.settledPriorOwner.actorDescriptor++}
 'prior feature replaced'={param($f)$f.artifact.observations.cumulative.settledPriorOwner.featureObject++}
 'prior source object replaced'={param($f)$f.artifact.observations.cumulative.settledPriorOwner.sourceObject++}
 'prior rank changed'={param($f)$f.artifact.observations.cumulative.settledPriorOwner.rank++}
 'prior feature aliases world'={param($f)$c=$f.artifact.observations.cumulative;$c.loadedPriorOwner.featureObject=$f.artifact.observations.loadedWorld.state;$c.settledPriorOwner.featureObject=$c.loadedPriorOwner.featureObject}
 'prior source identity unretained'={param($f)$f.artifact.observations.nativeLoad.identities.ids=@($f.artifact.observations.nativeLoad.identities.ids|Where-Object{$_-ne25});$f.artifact.observations.nativeLoad.identities.retainedCount--}
 'new pet reuses prior actor ID'={param($f)$f.artifact.observations.field.petId=$f.request.cumulativeAcquisition.priorOwnerId}
 'new actor aliases prior fact'={param($f)$f.artifact.observations.field.actorView=$f.artifact.observations.cumulative.loadedPriorOwner.featureObject}
 'cold action debt lost'={param($f)$f.artifact.observations.cumulative.resourcesLoaded.actors[0].standard++}
 'unrelated reaction spent'={param($f)$f.artifact.observations.cumulative.unrelatedResourcesAfter.actors[0].reactions++}
 'unrelated reaction allowance changed'={param($f)$f.artifact.observations.cumulative.unrelatedResourcesAfter.actors[0].reactionAllowance++}
 'unrelated preparation manufactured'={param($f)$f.artifact.observations.cumulative.unrelatedResourcesAfter.actors[0].prepared=$true}
 'unrelated initiative changed'={param($f)$f.artifact.observations.cumulative.unrelatedResourcesAfter.actors[0].initiative++}
 'unrelated companion detached'={param($f)$f.artifact.observations.cumulative.relationshipsAfter.actors[0].petId=$null}
 'unrelated XP changed'={param($f)$f.artifact.observations.cumulative.progressionAfter.actors[0].experience++}
 'third owner claimed'={param($f)$f.artifact.observations.cumulative.finalOwnerCount=3}
 'registry released before observations'={param($f)$f.artifact.observations.nativeLoad.identities.released=$true}
 'worker still live'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'extra native Prepare'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'second write'={param($f)$f.artifact.observations.nativeWrite.authorizedWrites=2}
 'manual source overwritten'={param($f)$f.artifact.observations.nativeWrite.sha256=$f.request.persistenceLoad.sha256}
 'save scope manufactures callback'={param($f)$f.artifact.observations.nativeWrite.nativeCallbackSupplied=$true}
 'static getter proof missing'={param($f)$f.artifact.observations.fixtureReadiness.staticPartyFacts.PSObject.Properties.Remove('observationScope')}
}
foreach($paid in @($true,$false)){foreach($c in $cases.GetEnumerator()){Bad ($paid.ToString()+' '+$c.Key){$f=New-CumulativeFixture $paid;&$c.Value $f;Check-Cumulative $f}}}
foreach($path in @('finalOwnerCount','loadedPriorOwner.actorObject','unrelatedResourcesAfter.actors.0.reactions')){foreach($form in @('text','float','bool','array')){Bad ($path+' '+$form){$f=New-CumulativeFixture;$v=$f.artifact.observations.cumulative;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){if($v-is[Array]){$v=$v[[int]$keys[$i]]}else{$v=$v.($keys[$i])}};$old=$v.($keys[-1]);$v.($keys[-1])=switch($form){'text'{[string]$old};'float'{[double]$old};'bool'{$true};'array'{,@($old)}};Check-Cumulative $f}}}
@{schemaVersion=1;pass=$script:passes;fail=$script:failures;readerSha256=(Get-FileHash (Join-Path $ReaderDirectory 'Chunk7CumulativeAcquisitionEvidence.ps1')).Hash.ToLowerInvariant();nativeExecution=$false;archiveAccess=$false;fixtureKind='synthetic-json-only'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures);if($script:failures){throw 'Cumulative strict reader checks failed.'}
