[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/f09-evidence-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$paths=@('scripts/runtime/Chunk7SmallFixtureEvidence.ps1','tests/Chunk7Diagnostics/SmallFixtureFixture.ps1','scripts/chunk7/Test-Chunk7SmallFixtureEvidence.ps1')
$inputPins=@($paths|ForEach-Object{$p=Join-Path $RepositoryRoot $_;@{path=$p;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}})
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureEvidence.ps1')
. (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallFixtureFixture.ps1')
$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Action){$caught=$false;try{&$Action}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
function Check($F){Assert-KmcChunk7SmallFixtureEvidence $F.request $F.artifact}
Good 'native-shaped F09 unmounted seven-actor one-Manual evidence' {Check (New-SmallFixtureSynthetic)}
$cases=[ordered]@{
 'wrong family'={param($f)$f.request.scenario='chunk7-native-progression'}
 'historical F10 selection alias'={param($f)$f.request.smallFixture.case='existing-ranger-native-level'}
 'foreign-mod installed-clone staging'={param($f)$f.request.smallFixture.baseGameOnly=$false}
 'uncapped load'={param($f)$f.request.smallFixture.expectedNativeLoadCount=2}
 'extra native write quota'={param($f)$f.request.smallFixture.expectedNativeWriteCount=2}
 'assisted main target'={param($f)$f.request.smallFixture.targetRangerLevel=8}
 'seed changed'={param($f)$f.request.smallFixture.seedSha256='0'*64}
 'catalogue changed'={param($f)$f.request.smallFixture.nativeInventoryArtifactSha256='0'*64}
 'qualification changed'={param($f)$f.request.smallFixture.seedQualificationArtifactSha256='0'*64}
 'extra body option'={param($f)$f.request.smallFixture|Add-Member gender 'Female'}
 'array case'={param($f)$f.request.smallFixture.case=@($f.request.smallFixture.case)}
 'string quota'={param($f)$f.request.smallFixture.expectedNativeWriteCount='1'}
 'Working alias'={param($f)$f.request.fixture.working.gameId=$f.request.persistenceLoad.gameId}
 'Auto input'={param($f)$f.request.persistenceLoad.fileName='Auto_1.zks'}
 'master loaded claim'={param($f)$f.artifact.observations.source.masterLoaded=$true}
 'producer ancestry claim'={param($f)$f.artifact.observations.producerAncestry=$true}
 'profile admission claim'={param($f)$f.artifact.observations.profileAdmissionChanged=$true}
 'pose qualification claim'={param($f)$f.artifact.observations.poseQualified=$true}
 'cold qualification claim'={param($f)$f.artifact.observations.coldLoadQualified=$true}
 'XP assistance'={param($f)$f.artifact.observations.xpWrites=1}
 'money assistance'={param($f)$f.artifact.observations.moneyWrites=1}
 'native Prepare event'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='Prepare'})}
 'early Manual reservation'={param($f)$f.artifact.observations.fieldWorld.authority.reservations=1}
 'native load observer absent'={param($f)$f.artifact.observations.nativeLoad.complete=$false}
 'observer foreign world'={param($f)$f.artifact.observations.nativeLoad.events[0].player=9}
 'unretained actor label'={param($f)$f.artifact.observations.fieldWorld.party[0].view=1001}
 'mounted behavior claim'={param($f)$f.artifact.observations.mountedBehaviorQualified=$true}
 'Mount input'={param($f)$f.artifact.observations.mountRequests=1}
 'owner retained'={param($f)$f.artifact.observations.savedWorld.owners=1}
 'slot retained'={param($f)$f.artifact.observations.savedWorld.slots=1}
 'unrelated progression changed'={param($f)$f.artifact.observations.neighboursAfter.actors[0].level++}
 'save mutated money'={param($f)$f.artifact.observations.afterSaveArrangement.money++}
 'free hire'={param($f)$f.artifact.observations.paidHireAdmission.forFree=$true}
 'zero hire cost'={param($f)$f.artifact.observations.paidHireAdmission.nativeCost=0}
 'native inherited XP mismatch'={param($f)$f.artifact.observations.paidHireAdmission.nativeInheritedXp++}
 'incomplete paid native commits'={param($f)$f.artifact.observations.paidNativeCommitCount=3}
 'native main level mismatch'={param($f)$f.artifact.observations.mainBuild.level=3}
 'native main money changed'={param($f)$f.artifact.observations.mainBuild.money++}
 'duplicate owner role'={param($f)$f.artifact.observations.acquiredOwners[1]=$f.artifact.observations.acquiredOwners[0]}
 'nonreciprocal Horse'={param($f)$f.artifact.observations.acquiredOwners[1].horseId=$f.artifact.observations.acquiredOwners[0].horseId}
 'unsourced Horse fact'={param($f)$f.artifact.observations.acquiredOwners[1].featureSourceGuid='0'*32}
 'native companion rank mismatch'={param($f)$f.artifact.observations.acquiredOwners[1].rank=2}
 'Halfling female'={param($f)$f.artifact.observations.bodyObservation.gender='Female'}
 'Halfling fat preset'={param($f)$f.artifact.observations.bodyObservation.presetGuid='4031e194544440c47a50ed30073bcf09'}
 'left handed'={param($f)$f.artifact.observations.bodyObservation.leftHanded=$true}
 'Medium body'={param($f)$f.artifact.observations.bodyObservation.effectiveSizeOrdinal=4}
 'polymorphed body'={param($f)$f.artifact.observations.bodyObservation.riderPolymorphed=$true}
 'nonfinite avatar scale'={param($f)$f.artifact.observations.bodyObservation.avatarLocalScale.x=[double]::NaN}
 'missing equipment slot'={param($f)$f.artifact.observations.bodyObservation.currentEquipmentBlueprints=@($null)*11}
 'empty live visual equipment'={param($f)$f.artifact.observations.bodyObservation.activeVisualEquipmentNames=@()}
 'extra limb'={param($f)$f.artifact.observations.bodyObservation.additionalLimbCount=1}
 'missing live transform'={param($f)$f.artifact.observations.unmountedProfileInventory.party[0].character.poseTransforms[0].matches=@()}
 'duplicate live bone'={param($f)$f.artifact.observations.unmountedProfileInventory.party[0].character.poseTransforms[1].name='Pelvis'}
 'foreign live rig'={param($f)$f.artifact.observations.unmountedProfileInventory.party[0].id='aaaaaaaa-1111-4111-8111-111111111111'}
 'foreign Horse anchor'={param($f)$f.artifact.observations.unmountedProfileInventory.party[1].horseSeatAnchors[0].name='Seat'}
 'write is Auto'={param($f)$f.artifact.observations.nativeWrite.type='Auto'}
 'fake levelup save'={param($f)$f.artifact.observations.nativeWrite.autoLevelup=$true}
 'schema two output'={param($f)$f.artifact.observations.nativeWrite.sourceSchema=2}
 'hidden mounted pair'={param($f)$f.artifact.observations.nativeWrite.archive.Pairs=@([pscustomobject]@{})}
 'hidden participation'={param($f)$f.artifact.observations.nativeWrite.archive.Participations=@([pscustomobject]@{})}
 'hidden hotbar slot'={param($f)$f.artifact.observations.nativeWrite.archive.Slots=@([pscustomobject]@{})}
 'hidden combat'={param($f)$f.artifact.observations.nativeWrite.archive.Combat=[pscustomobject]@{}}
 'native save worker absent'={param($f)$f.artifact.observations.nativeSave.completions=0}
 'second callback'={param($f)$f.artifact.observations.nativeWrite.callbackCount=2}
 'cleanup not restored'={param($f)$f.artifact.observations.cleanup.pauseRestored=$false}
 'cleanup removed companion'={param($f)$f.artifact.observations.cleanup.manualCompanionRemovals=1}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key {$f=New-SmallFixtureSynthetic;&$case.Value $f;Check $f}}
$watch.Stop();$unchanged=$true;foreach($pin in $inputPins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false}}
if(-not$unchanged){$script:fail++;Write-Host 'FAIL selected test/reader inputs changed during execution'}
@{schemaVersion=1;pass=$script:pass;fail=$script:fail;elapsedSeconds=$watch.Elapsed.TotalSeconds;inputsUnchanged=$unchanged;nativeExecution=$false;claims='synthetic strict external-reader behavior only';sourceHashes=$inputPins}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){exit 1}
