[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-derived-evidence-'+[Guid]::NewGuid().ToString('N'))}
$derivedReader=[IO.Path]::GetFullPath($ReaderDirectory);$derivedFixtures=[IO.Path]::GetFullPath($FixtureDirectory);$derivedOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $derivedReader 'RuntimeHarness.Common.ps1')
function New-ManualDerivedFixture([bool]$Paid=$true){$name=if($Paid){'Paid'}else{'Existing'};return [IO.File]::ReadAllText((Join-Path $derivedFixtures ('ManualDerived'+$name+'Fixture.json')))|ConvertFrom-Json}
function Check-ManualDerived($F){Assert-KmcChunk7ManualDerivedEvidence $F.request $F.artifact $F.source}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($derivedOutput);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_+' at '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$rejected=$false;try{&$Body}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS refuses '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
Good 'paid native Manual lineage, choice, Auto and field sequence' {$f=New-ManualDerivedFixture;Check-ManualDerived $f}
Good 'existing native eligible Ranger retains exact original world' {$f=New-ManualDerivedFixture $false;Check-ManualDerived $f}
$cases=[ordered]@{
 'foreign source candidate'={param($f)$f.source.dllSha256='e'*64}
 'failed source result'={param($f)$f.source.status='FAIL'}
 'same native process'={param($f)$f.artifact.observations.processId=$f.source.observations.processId}
 'Manual input replaced by Working'={param($f)$f.request.persistenceLoad.gameId=$f.request.fixture.working.gameId}
 'two loads'={param($f)$f.request.manualDerived.expectedNativeLoadCount=2}
 'two writes'={param($f)$f.request.manualDerived.expectedNativeWriteCount=2}
 'unselected output slot'={param($f)$f.request.manualDerived.nativeWriteLeaf='Auto_2.zks'}
 'money readiness substituted'={param($f)$f.request.manualDerived.readiness.money++}
 'eligibility without attained level'={param($f)$f.artifact.observations.liveEligibility.mainLevel=3}
 'eligibility XP table mismatch'={param($f)$f.artifact.observations.liveEligibility.nativeXpThresholds[4]=7000}
 'eligibility class entitlement invented'={param($f)$f.artifact.observations.liveEligibility.arrangementGranted=$true}
 'native purchase free'={param($f)$f.artifact.observations.choices[0].nativeCost=0}
 'native choice XP changed'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).after.xp++}
 'native choice rejected'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).nativeChoiceAccepted=$false}
 'missing native Horse choice'={param($f)($f.artifact.observations.choices|Where-Object { $_.event-ceq'native-choice'-and$_.kind-ceq'feature'}|Select-Object -Last 1).nativeSelectionGuid='a'*32}
 'native Total incomplete'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).nativeIsComplete=$false}
 'native Total skills unspent'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).skillRemaining=1}
 'native level skipped'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-level-committed'|Select-Object -First 1).after.level++}
 'unrelated actor progression changed'={param($f)$f.artifact.observations.afterChoices.actors[0].experience++}
 'native money refunded'={param($f)$f.artifact.observations.afterChoices.money+=2000}
 'native preparation after choices'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'foreign loaded Player'={param($f)$f.artifact.observations.nativeLoad.events[1].player=12345}
 'world labels collide'={param($f)$f.artifact.observations.loadedWorld.state=$f.artifact.observations.loadedWorld.game}
 'registry release premature'={param($f)$f.artifact.observations.nativeLoad.identities.released=$true}
 'registry capacity exceeded'={param($f)$f.artifact.observations.nativeLoad.identities.ids=@(1..129);$f.artifact.observations.nativeLoad.identities.retainedCount=129}
 'output source bytes alias'={param($f)$f.artifact.observations.nativeWrite.sha256=$f.request.persistenceLoad.sha256}
 'foreign descriptor slot'={param($f)$f.artifact.observations.nativeWrite.leaf='Auto_2.zks'}
 'native write repeated'={param($f)$f.artifact.observations.nativeWrite.completedWrites=2}
 'wrong native category'={param($f)$f.artifact.observations.nativeWrite.type='Manual'}
 'manual save callback injected'={param($f)$f.artifact.observations.nativeWrite.nativeCallbackSupplied=$true}
 'partial completion callback'={param($f)$f.artifact.observations.nativeAcquisition.completionExits--}
 'autosave outside actual completion'={param($f)($f.artifact.observations.nativeAcquisition.events|Where-Object kind -CEQ 'native-level-autosave-request').insideObservedCompletion=$false}
 'prepared save identity changed'={param($f)($f.artifact.observations.nativeAcquisition.events|Where-Object kind -CEQ 'native-level-autosave-prepared').saveObject++}
 'foreign native hook'={param($f)$f.artifact.observations.nativeAcquisition.hooks[0].moduleMvid='11111111-1111-4111-8111-111111111111'}
 'native field actor aliases world'={param($f)$f.artifact.observations.field.actorView=$f.artifact.observations.loadedWorld.state}
 'native pet labels collide'={param($f)$f.artifact.observations.field.petDescriptor=$f.artifact.observations.field.actorDescriptor}
 'native field Small race changed'={param($f)$f.artifact.observations.field.bodyObservation.RaceGuid='a'*32}
 'size effect substitutes Small'={param($f)$f.artifact.observations.field.bodyObservation.OriginalSizeOrdinal=4}
 'selector not closed'={param($f)$f.artifact.observations.fieldSelection.closedExactInstance=$false}
 'unowned actor removed from field'={param($f)$f.artifact.observations.field.partyCharacters=@($f.artifact.observations.field.actorId)}
 'static party facts promote selection authority'={param($f)$f.artifact.observations.fixtureReadiness.staticPartyFacts.membershipOrRemovalQualified=$true}
 'readiness projection already active'={param($f)$f.artifact.observations.fixtureReadiness.saveSettings.projectionEnabled=$true}
 'native saved flags contradictory'={param($f)$f.artifact.observations.fixtureReadiness.isolatedSaveDescriptors.descriptors[0].isActuallySaved=$false}
 'slot Single underflows'={param($f)$f.artifact.observations.fixtureReadiness.saveSettings.autosaveSlots=1e-100}
 'cleanup selection not restored'={param($f)$f.artifact.observations.cleanup.selectionRestored=$false}
 'cleanup UMM not restored'={param($f)$f.artifact.observations.cleanup.ummOpenStateRestored=$false}
 'cleanup observer retained'={param($f)$f.artifact.observations.cleanup.observersReleased=$false}
 'cleanup worker active'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'cleanup releases unfinished quota'={param($f)$f.artifact.observations.cleanup.authority.authorizedWrites=0}
 'cleanup refunds native spend'={param($f)$f.artifact.observations.cleanup.manualRefunds=1}
 'unearned pose claim'={param($f)$f.artifact.observations.smallPoseQualified=$true}
 'unearned vendor-dialogue claim'={param($f)$f.artifact.observations.vendorDialogueProven=$true}
 'elapsed beyond declared bound'={param($f)$f.artifact.observations.elapsedSeconds=1051.0}
 'choice phase array'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).phase=@('Total')}
 'paid source hash array'={param($f)$f.artifact.observations.choices[0].nativeInventorySha256=@($f.request.manualDerived.sourceArtifactSha256)}
}
foreach($pair in $cases.GetEnumerator()){Bad $pair.Key {$f=New-ManualDerivedFixture;&$pair.Value $f;Check-ManualDerived $f}}
foreach($path in @('nativeWrite.authorizedWrites','nativeAcquisition.preparedWrites','nativeAcquisition.events.0.actorObject','field.actorDescriptor','liveEligibility.defaultHireExperience','fixtureReadiness.mainProgression.characterLevel','settledWorld.completedSaves')){
 foreach($form in @('text','float','boolean','array')){Bad ($path+' coerces '+$form){$f=New-ManualDerivedFixture;$o=$f.artifact.observations;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){if($o-is[Array]){$o=$o[[int]$keys[$i]]}else{$o=$o.($keys[$i])}};$o.($keys[-1])=switch($form){'text'{[string]$o.($keys[-1])};'float'{[double]$o.($keys[-1])};'boolean'{$true};'array'{,@($o.($keys[-1]))}};Check-ManualDerived $f}}
}
Bad 'existing insufficient native earned XP' {$f=New-ManualDerivedFixture $false;$f.artifact.observations.liveEligibility.nativeXpThresholds[4]=7000;Check-ManualDerived $f}
Bad 'existing foreign pet retained' {$f=New-ManualDerivedFixture $false;$f.artifact.observations.liveEligibility.targetHasPet=$true;Check-ManualDerived $f}
# Preserve the confirmed coercion regressions for both native acquisition paths.
$choiceTokenCases=[ordered]@{
 'accepted class kind array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice'-and$_.kind-ceq'class'})[0];$r.kind=@($r.kind)}
 'accepted class choice array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice'-and$_.kind-ceq'class'})[0];$r.choice=@($r.choice)}
 'accepted feature kind array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice'-and$_.kind-ceq'feature'})[0];$r.kind=@($r.kind)}
 'accepted feature choice array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice'-and$_.kind-ceq'feature'})[0];$r.choice=@($r.choice)}
 'requested feature GUID array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice-requested'-and$_.kind-ceq'feature'})[0];$r.nativeSelectionGuid=@($r.nativeSelectionGuid)}
 'accepted feature GUID array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice'-and$_.kind-ceq'feature'})[0];$r.nativeSelectionGuid=@($r.nativeSelectionGuid)}
 'requested class kind array'={param($f)$r=@($f.artifact.observations.choices|Where-Object{$_.event-ceq'native-choice-requested'-and$_.kind-ceq'class'})[0];$r.kind=@($r.kind)}
}
foreach($paid in @($false,$true)){
 foreach($pair in $choiceTokenCases.GetEnumerator()){
  $route=if($paid){'paid'}else{'existing'}
  Bad ($route+' '+$pair.Key){$f=New-ManualDerivedFixture $paid;&$pair.Value $f;Check-ManualDerived $f}
 }
}
$receipt=@{schemaVersion=1;pass=$script:passes;fail=$script:failures;readerSha256=(Get-FileHash -LiteralPath (Join-Path $derivedReader 'Chunk7ManualDerivedEvidence.ps1')).Hash.ToLowerInvariant();nativeExecution=$false;archiveAccess=$false;fixtureKind='synthetic-json-only'}
$receipt|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $derivedOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures)
if($script:failures-ne0){throw 'Manual-derived strict evidence checks failed.'}
