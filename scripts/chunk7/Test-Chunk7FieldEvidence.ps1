[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
$fieldReaderDirectory=[IO.Path]::GetFullPath($ReaderDirectory)
$fieldFixturesOnly=$FixturesOnly.IsPresent
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixturesOnly
. (Join-Path $fieldReaderDirectory 'Chunk7OrdinaryFieldEvidence.ps1')
. (Join-Path $fieldReaderDirectory 'Chunk7OrdinaryFieldFixtures.ps1')
function New-FieldFixture {
    $paid=New-OrdinaryFixture;$o=$paid.artifact.observations;$s=[pscustomobject]@{case='paid-small-remote-field';sourceRunId=$paid.request.runId;sourceArtifactSha256='e'*64;sourceArchiveSha256='f'*64;actorId=$o.field.actorId;petId=$o.field.petId}
    $r=Copy-O $paid.request;$r.scenario='chunk7-ordinary-field-admission';$r.runId='synthetic-field';$r.PSObject.Properties.Remove('ordinaryAcquisition');$r|Add-Member -NotePropertyName ordinaryField -NotePropertyValue $s
    $r.evidenceRoot=Join-Path ([IO.Path]::GetDirectoryName($paid.request.evidenceRoot)) $r.runId
    $r.fixture|Add-Member -NotePropertyName baseline -NotePropertyValue ([pscustomobject]@{sha256='b'*64})
    $r|Add-Member -NotePropertyName persistenceLoad -NotePropertyValue ([pscustomobject]@{internalName='Autosave';fileName='Auto_1.zks';sha256='f'*64;length=100L;lastWriteTimeUtcTicks=638900000000000000L;gameId=$r.fixture.working.gameId;gameName=$r.fixture.working.gameName;area=$r.fixture.working.area})
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($r.evidenceRoot))) ('runtime-staging/persistence-'+$r.runId+'/Saved Games')
    $authority=[pscustomobject]@{root=$root;generation=2;exactScope=$true;maximumNativeLoads=1;maximumNativeWrites=$null;admittedNativeLoads=1;admittedNativeWrites=0;authorizedLoads=1;authorizedWrites=0;unauthorizedLoads=0;unauthorizedWrites=0;fatalViolations=0;suppressedWrites=0}
    $before=[pscustomobject]@{actorId=$s.actorId;actorObject=-99;descriptorObject=199;petId=$s.petId;petObject=299;petDescriptorObject=399;money=18000L;xp=6000;level=4;petLevel=4;rank=1;featureSourceGuid='ee63330662126374e8785cc901941ac7';reciprocal=$true;
        riderInGame=$false;petInGame=$false;riderInState=$true;petInState=$true;riderAgentReady=$false;petAgentReady=$false;partyCharacters=$o.before.partyCharacters;remoteCompanions=@($s.actorId)}
    $after=Copy-O $before;foreach($key in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){$after.$key=$true};$after.partyCharacters+=@($s.actorId);$after.remoteCompanions=@()
    $field=[pscustomobject]@{processId=456;elapsedSeconds=12.0;selection=(Copy-O $s);sourceWasRemoteAutosave=$true;sourceArchiveStillRemote=$true;smallPoseQualified=$false;mountedCombatProven=$false;freshMainCampaignProven=$false;manualPartyMutation=$false;
        source=[pscustomobject]@{runId=$s.sourceRunId;artifactSha256=$s.sourceArtifactSha256;archiveSha256=$s.sourceArchiveSha256;archiveLeaf='Auto_1.zks';archiveName='Autosave';archiveLength=100L;bodySelection=(Copy-O $o.selection);sameCandidate=$true;unchangedArchive=$true};
        authority=$authority;before=$before;field=$after;profileInventory=(Copy-O $o.field.profileInventory);bodyObservation=(Copy-O $o.field.bodyObservation);
        selector=[pscustomobject]@{action='native-ShowPartySelection';actorId=$s.actorId;nativeCap=6;openCount=1;switchCount=1;goCount=1;openActiveSelf=$true;readyAlpha=1.0;readyActiveInHierarchy=$true;closedActiveSelf=$false;closedExactInstance=$true;manualPartyMutation=$false};
        cleanup=[pscustomobject]@{custodySettled=$true;nativeWorkerSettled=$true;selectorClosed=$true;selectionRestored=$true;pauseRestored=$true;acquiredNativeStateRetained=$true;sourceUnchanged=$true;nativeWrites=0;manualPartyMutations=0;manualRefunds=0;manualFactRemovals=0;manualCompanionRemovals=0;authority=(Copy-O $authority)}}
    $a=Copy-O $paid.artifact;$a.evidenceKind='chunk7-ordinary-field-evidence';$a.scenario=$r.scenario;$a.runId=$r.runId;$a.observations=$field
    $field.profileInventory|Add-Member -NotePropertyName party -NotePropertyValue @([pscustomobject]@{id=$s.actorId;character=[pscustomobject]@{present=$true}},[pscustomobject]@{id=$s.petId;character=[pscustomobject]@{present=$true}})
    [pscustomobject]@{request=$r;artifact=$a}
}
if($fieldFixturesOnly){return}
$script:passes=0
Good 'remote acquisition cold load has distinct native field placement and no writes' {$f=New-FieldFixture;Assert-KmcChunk7FieldEvidence $f.request $f.artifact}
$cases=[ordered]@{
 'write authority'={param($f)$f.artifact.observations.authority.maximumNativeWrites=1}
 'second load'={param($f)$f.artifact.observations.cleanup.authority.admittedNativeLoads=2}
 'string load counter'={param($f)$f.artifact.observations.authority.authorizedLoads='1'}
 'boolean load cap'={param($f)$f.artifact.observations.authority.maximumNativeLoads=$true}
 'native write'={param($f)$f.artifact.observations.cleanup.nativeWrites=1}
 'scope rebound'={param($f)$f.artifact.observations.cleanup.authority.generation=3}
 'scope false'={param($f)$f.artifact.observations.authority.exactScope=$false}
 'saved actor incorrectly field placed'={param($f)$f.artifact.observations.before.riderInGame=$true}
 'saved actor not remote'={param($f)$f.artifact.observations.before.remoteCompanions=@()}
 'remote archive promoted to mounted fixture'={param($f)$f.artifact.observations.sourceArchiveStillRemote=$false}
 'Small pose promoted'={param($f)$f.artifact.observations.smallPoseQualified=$true}
 'native actor object text'={param($f)$f.artifact.observations.field.actorObject='-99'}
 'native object out of int32'={param($f)$f.artifact.observations.field.actorObject=[long]::MaxValue}
 'native object zero'={param($f)$f.artifact.observations.before.actorObject=0}
 'native actor object replaced'={param($f)$f.artifact.observations.field.actorObject++}
 'XP refunded'={param($f)$f.artifact.observations.field.xp++}
 'native XP outside int32'={param($f)$f.artifact.observations.before.xp=[long]::MaxValue;$f.artifact.observations.field.xp=[long]::MaxValue}
 'native level unbounded'={param($f)$f.artifact.observations.before.level=21;$f.artifact.observations.field.level=21}
 'native money refunded'={param($f)$f.artifact.observations.field.money++}
 'unrelated party removed'={param($f)$f.artifact.observations.field.partyCharacters=@($f.request.ordinaryField.actorId)}
 'duplicate field rider'={param($f)$f.artifact.observations.field.partyCharacters+=@($f.request.ordinaryField.actorId)}
 'foreign blueprint body'={param($f)$f.artifact.observations.bodyObservation.RaceGuid='9'*32}
 'foreign preset body'={param($f)$f.artifact.observations.bodyObservation.PresetGuid='9'*32}
 'foreign rig body'={param($f)$f.artifact.observations.bodyObservation.SkeletonName='unknown'}
 'unready rig'={param($f)$f.artifact.observations.bodyObservation.NativeRigAvailable=$false}
 'future profile observation schema'={param($f)$f.artifact.observations.profileInventory.schemaVersion=2}
 'missing exact profile actors'={param($f)$f.artifact.observations.profileInventory.party=@()}
 'foreign profile actor'={param($f)$f.artifact.observations.profileInventory.party[0].id='99999999-1111-4111-8111-111111111111'}
 'missing rider native rig inventory'={param($f)$f.artifact.observations.profileInventory.party[0].character.present=$false}
 'text native rig proof'={param($f)$f.artifact.observations.bodyObservation.NativeRigAvailable='true'}
 'early selector input'={param($f)$f.artifact.observations.selector.readyAlpha=.5}
 'NaN selector readiness'={param($f)$f.artifact.observations.selector.readyAlpha=[double]::NaN}
 'repeat native open'={param($f)$f.artifact.observations.selector.openCount=2}
 'repeat native Go'={param($f)$f.artifact.observations.selector.goCount=2}
 'text native Go'={param($f)$f.artifact.observations.selector.goCount='1'}
 'foreign closed selector'={param($f)$f.artifact.observations.selector.closedExactInstance=$false}
 'selector still active'={param($f)$f.artifact.observations.selector.closedActiveSelf=$true}
 'native retained worker'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'native selector retained'={param($f)$f.artifact.observations.cleanup.selectorClosed=$false}
 'source archive changed'={param($f)$f.artifact.observations.cleanup.sourceUnchanged=$false}
 'manual party mutation'={param($f)$f.artifact.observations.cleanup.manualPartyMutations=1}
 'arbitrary native leaf'={param($f)$f.request.persistenceLoad.fileName='Auto_2.zks'}
 'foreign archive sha'={param($f)$f.request.persistenceLoad.sha256='a'*64}
 'foreign area'={param($f)$f.request.persistenceLoad.area='9'*32}
 'nonpaid source'={param($f)$f.artifact.observations.source.bodySelection.case='existing-ranger-horse'}
 'source self aliases'={param($f)$f.request.ordinaryField.sourceRunId=$f.request.runId}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key {$f=New-FieldFixture;&$case.Value $f;Assert-KmcChunk7FieldEvidence $f.request $f.artifact}}
Good 'field result accepts signed native identities and int64 native money' {$f=New-FieldFixture;$f.artifact.observations.before.money=[long]2147483648;$f.artifact.observations.field.money=[long]2147483648;Assert-KmcChunk7FieldEvidence $f.request $f.artifact}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic field reader only')
