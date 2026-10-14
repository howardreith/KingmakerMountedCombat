[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[Parameter(Mandatory=$true)][string]$EmittedRoot,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$actualLab=Get-KmcLabRoot;$output=Assert-KmcChildPath $OutputRoot (Join-Path $actualLab 'analysis-cache') 'generated assisted cold evidence checks'
if(Test-Path -LiteralPath $output){throw 'Preserve earlier assisted cold evidence checks.'};[void][IO.Directory]::CreateDirectory($output)
$script:inputPins=[Collections.Generic.List[object]]::new()
function Pin([string]$Path){$i=Get-Item -LiteralPath $Path;[pscustomobject]@{path=$Path;sha256=(Get-KmcSha256 $Path);length=$i.Length}}
function Source([string]$Relative){$p=Join-Path $RepositoryRoot $Relative;if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$o=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $o -PathType Leaf){$p=$o}};$script:inputPins.Add((Pin $p));$p}
. (Source 'scripts/runtime/Chunk7AssistedFixtureEvidence.ps1')
. (Source 'scripts/runtime/Chunk7AssistedFixtureFixtures.ps1')
. (Source 'scripts/runtime/Chunk7AssistedColdInventoryEvidence.ps1')
. (Source 'scripts/runtime/Chunk7AssistedColdInventoryFixtures.ps1')
$script:inputPins.Add((Pin $PSCommandPath))
foreach($p in @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -Filter '*.ps1' -File)){$script:inputPins.Add((Pin $p.FullName))}
$raw=@{};foreach($leaf in @('cold-actual-capture.json','cold-actual-request.json','cold-generated-source.json')){$p=Join-Path $EmittedRoot $leaf;$script:inputPins.Add((Pin $p));$raw[$leaf]=Read-KmcJson $p}
$template=Read-KmcJson (Source 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json')
# All validators execute unchanged. Only the filesystem lab root is private;
# native profile/restoration/installed-MVID and omitted outer request/source
# envelopes are generated external boundaries, not a qualified runtime parent.
function Get-KmcLabRoot {$script:caseLab}
function Clone($Value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $Value -Depth 80 -Compress)}
function Put([string]$Path,[string]$Text){[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path));[IO.File]::WriteAllText($Path,$Text,[Text.UTF8Encoding]::new($false))}
function Json($Value){ConvertTo-Json -InputObject $Value -Depth 80 -Compress}
function Need([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
$script:caseNumber=0;$script:passed=0;$script:failed=0;$script:seedVerified=$false;$cases=[Collections.Generic.List[object]]::new();$timer=[Diagnostics.Stopwatch]::StartNew()
function New-Fixture {
 $script:caseNumber++;$script:caseLab=Join-Path $output ('case'+$script:caseNumber)
 $r=Clone $raw['cold-actual-request.json'];$a=Clone $raw['cold-actual-capture.json'];$s=Clone $raw['cold-generated-source.json'];$o=$a.observations
 $r.evidenceRoot=Join-Path $script:caseLab ('runtime-evidence/'+$r.runId)
 $r|Add-Member NoteProperty fixture ([pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64);gameId='bbbbbbbb-1111-1111-1111-111111111111'};working=[pscustomobject]@{sha256=('c'*64);gameId='cccccccc-1111-1111-1111-111111111111'}})
 $root=Join-Path $script:caseLab ('runtime-staging/persistence-'+$r.runId+'/Saved Games')
 foreach($row in @($o.beforeLoad,$o.loadedWorld,$o.settledWorld,$o.loader,$o.settledLoader,$o.cleanup)){$row.authority.root=$root}
 foreach($row in @($o.loader,$o.settledLoader)){$row.path=Join-Path $root $r.persistenceLoad.fileName}
 $o.source.copiedArtifactPath=Join-Path (Split-Path $root -Parent) 'assisted-cold-source.json'
 $o.fixtureReadiness.isolatedSaveDescriptors.root=$root
 $o.fixtureReadiness.gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 $s|Add-Member NoteProperty schemaVersion 1
 $s.observations|Add-Member NoteProperty assistanceOnly $true
 foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','mountedCombatProven','smallPoseQualified')){$s.observations|Add-Member NoteProperty $key $false}
 # Actual full source world/readiness are retained without cross-process label conversion.
 $p=Clone $template.artifact.observations.nativeProfileInventory;$rows=@()
 foreach($actor in $o.loadedWorld.party){$v=Clone $p.party[0];$v.id=$actor.id;$v.blueprintGuid=$actor.blueprint;$v.nativeLevelUpReadiness.characterLevel=$actor.characterLevel;$v.nativeLevelUpReadiness.experience=$actor.experience;$rows+=,$v};$p.party=$rows
 $m=@($o.loadedWorld.party|Where-Object id -CEQ $o.loadedWorld.mainActorId)[0];$q=$p.ordinaryAcquisitionPrerequisites
 $q.nativeMoney=$o.sourceNativeMoney;$q.nativeHireCostNow=2000;$q.nativeAffordableNow=$true;$q.nativePartyLevel=$m.characterLevel;$q.mainCharacterId=$m.id;$q.mainCharacterLevel=$m.characterLevel;$q.mainCharacterExperience=$m.experience;$q.nativePartyCharacterIds=@($o.loadedWorld.partyCharacterIds|ForEach-Object{[string]$_})
 $o.nativeProfileInventory=$p
 $o.noPreparation=Clone $template.artifact.observations.noPreparation
 $o.noPreparation.hooks[2].moduleMvid=$r.dllMvid;$o.noPreparation.hooks[3].moduleMvid=$r.dllMvid
 $sourcePath=Join-Path $script:caseLab ('runtime-evidence/'+$r.assistedColdInventory.sourceRunId+'/chunk7-assisted-fixture-evidence.json')
 Put $sourcePath (Json $s);$hash=Get-KmcSha256 $sourcePath
 $r.assistedColdInventory.sourceArtifactSha256=$hash;$o.selection.sourceArtifactSha256=$hash;$o.source.artifactSha256=$hash
 $f=[pscustomobject]@{request=$r;artifact=$a;source=$s;sourcePath=$sourcePath;root=$root;artifactPath=(Join-Path $r.evidenceRoot 'chunk7-assisted-cold-inventory-evidence.json')}
 $f
}
function Verify($f,[string]$Status='PASS'){Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source $Status}
function Publish($f){
 Put $f.artifactPath (Json $f.artifact);$pin=Pin $f.artifactPath
 [pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-assisted-cold-inventory-evidence.json';kind='chunk7-assisted-cold-inventory-evidence';length=$pin.length;sha256=$pin.sha256})}
}
function Pending($f){
 $a=Clone $f.artifact;$a.evidenceKind='chunk7-assisted-cold-inventory-cleanup-pending';$a.status='FAIL';$a.errors=@('Generated native worker retained.')
 $a.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true;cleanupCustodyRetained=$true;reason='Generated worker not settled.';authority=(Clone $f.artifact.observations.beforeLoad.authority)};$a
}
function Check([string]$Name,[bool]$Expected,[scriptblock]$Action){
 $accepted=$true;$errorText=$null;$ready=$false
 try{$f=New-Fixture;$ready=$true;&$Action $f|Out-Null}catch{$accepted=$false;$errorText=$_.Exception.Message}
 $ok=$ready-and$accepted-eq$Expected
 if($ok){$script:passed++}else{$script:failed++}
 $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Expected;actualAccepted=$accepted;fixtureComposed=$ready;error=$errorText})
 Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok-and$errorText){': '+$errorText}else{''}))
}
Check 'actual Capture world loader identities and cleanup accept declared external boundary composition' $true {param($f)Verify $f;$script:seedVerified=$true;Put (Join-Path $output 'composed-positive.json') (Json $f)}
Check 'actual manifest and held source read accept matching generated bytes' $true {param($f)$m=Publish $f;Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'source and destination process-local labels are not cross-compared' $true {param($f)Need ($f.source.observations.settledWorld.player-ne$f.artifact.observations.loadedWorld.player) 'Generated domains should differ';Verify $f}
Check 'signed native object labels retain exact registry relationships' $true {param($f)$o=$f.artifact.observations;$id=$o.loadedWorld.party[0].object;$o.loadedWorld.party[0].object=-$id;$o.settledWorld.party[0].object=-$id;$o.nativeLoad.identities.ids=@($o.nativeLoad.identities.ids|ForEach-Object{if($_-eq$id){-$id}else{$_}});Verify $f}
Check 'raw failed artifact is retained as FAIL without a success inference' $true {param($f)$f.artifact.status='FAIL';$f.artifact.errors=@('Generated load failure');$f.artifact.observations=[pscustomobject]@{};Verify $f 'FAIL'}
Check 'failed artifact cannot satisfy successful completion' $false {param($f)$f.artifact.status='FAIL';$f.artifact.errors=@('Generated load failure');Verify $f}
Check 'pending cleanup remains a valid nonauthorizing FAIL observation' $true {param($f)$p=Pending $f;Assert-KmcAssistedColdPending $f.request $p}
Check 'pending cleanup cannot claim released custody' $false {param($f)$p=Pending $f;$p.observations.cleanupCustodyRetained=$false;Assert-KmcAssistedColdPending $f.request $p}
Check 'PASS manifest cannot include pending cleanup' $false {param($f)$m=Publish $f;$p=Pending $f;$leaf='chunk7-assisted-cold-inventory-cleanup-pending.json';$pPath=Join-Path $f.request.evidenceRoot $leaf;Put $pPath (Json $p);$pin=Pin $pPath;$m.artifacts+=,[pscustomobject]@{relativePath=$leaf;kind=$p.evidenceKind;length=$pin.length;sha256=$pin.sha256};Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'failed result with no sidecar does not fabricate successful proof' $true {param($f)Assert-KmcChunk7AssistedColdInventoryArtifact $f.request ([pscustomobject]@{artifacts=@()}) 'FAIL'}
Check 'foreign legacy family without new selection and sidecar stays unchanged' $true {param($f)$f.request.scenario='chunk7-bootstrap-cold-inventory';$f.request.PSObject.Properties.Remove('assistedColdInventory');Assert-KmcChunk7AssistedColdInventoryArtifact $f.request ([pscustomobject]@{artifacts=@()})}
Check 'foreign family refuses new selection' $false {param($f)$f.request.scenario='chunk7-bootstrap-cold-inventory';Verify $f}
Check 'foreign family refuses new sidecar even without selection' $false {param($f)$m=Publish $f;$f.request.scenario='chunk7-bootstrap-cold-inventory';$f.request.PSObject.Properties.Remove('assistedColdInventory');Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'root singleton artifact is refused' $false {param($f)$f.artifact=@($f.artifact);Verify $f}
Check 'root singleton source is refused' $false {param($f)$f.source=@($f.source);Verify $f}
foreach($key in @('status','evidenceKind','createdAtUtc')){Check ('singleton artifact '+$key+' is refused') $false {param($f)$f.artifact.$key=@($f.artifact.$key);Verify $f}}
Check 'unknown observation field is refused' $false {param($f)$f.artifact.observations|Add-Member NoteProperty extra $true;Verify $f}
Check 'same source process is refused' $false {param($f)$f.artifact.observations.processId=$f.source.observations.processId;Verify $f}
Check 'foreign source candidate is refused' $false {param($f)$f.source.dllSha256='f'*64;Verify $f}
Check 'source setup proof cannot become ordinary acquisition' $false {param($f)$f.source.observations.ordinaryAcquisitionProven=$true;Verify $f}
Check 'source settled roster clone must remain exact' $false {param($f)$f.artifact.observations.sourceNativeWorld.party[1].blueprint='e'*32;Verify $f}
Check 'observed Manual descriptor cannot become Auto' $false {param($f)$f.request.persistenceLoad.fileName='Auto_1.zks';Verify $f}
Check 'source archive byte identity cannot drift' $false {param($f)$f.artifact.observations.source.archiveSha256='f'*64;Verify $f}
Check 'before-load menu may not already contain a loaded area' $false {param($f)$f.artifact.observations.beforeLoad.loadedAreaPresent=$true;Verify $f}
Check 'before-load authority may not consume the load early' $false {param($f)$f.artifact.observations.beforeLoad.authority.authorizedLoads=1;Verify $f}
Check 'null write maximum is required instead of invented zero quota' $false {param($f)$f.artifact.observations.loadedWorld.authority.maximumNativeWrites=0;Verify $f}
Check 'read-only plan cannot contain writable entry' $false {param($f)$f.artifact.observations.loader.authority.writableEntries=1;Verify $f}
Check 'second native load is refused' $false {param($f)$f.artifact.observations.settledLoader.authority.admittedNativeLoads=2;Verify $f}
Check 'actual native write is refused' $false {param($f)$f.artifact.observations.cleanup.authority.authorizedWrites=1;Verify $f}
Check 'native loader must observe after-load callback' $false {param($f)$f.artifact.observations.loader.callbackObserved=$false;Verify $f}
Check 'native loader cannot remain loading' $false {param($f)$f.artifact.observations.settledLoader.state='Loading';Verify $f}
Check 'native loader descriptor must match retained world label' $false {param($f)$f.artifact.observations.loader.descriptor=$f.artifact.observations.loadedWorld.player;Verify $f}
Check 'native loader must retain exact run-owned path' $false {param($f)$f.artifact.observations.loader.path=Join-Path $f.root 'Auto_1.zks';Verify $f}
Check 'loaded world native Game owner cannot change' $false {param($f)$f.artifact.observations.loadedWorld.game=$f.artifact.observations.beforeLoad.menuPlayer;Verify $f}
Check 'loaded actor cannot retain missing view' $false {param($f)$f.artifact.observations.loadedWorld.party[0].view=0;Verify $f}
Check 'loaded actor cannot alias unrelated view' $false {param($f)$f.artifact.observations.loadedWorld.party[0].view=$f.artifact.observations.loadedWorld.party[1].view;Verify $f}
Check 'registry missing loaded actor identity is refused' $false {param($f)$o=$f.artifact.observations;$id=$o.loadedWorld.party[0].object;$o.nativeLoad.identities.ids=@($o.nativeLoad.identities.ids|Where-Object{$_-ne$id});$o.nativeLoad.identities.retainedCount--;Verify $f}
Check 'released observer registry cannot prove retained snapshot identities' $false {param($f)$f.artifact.observations.nativeLoad.identities.released=$true;Verify $f}
Check 'PostLoad after-event wrong Player is refused' $false {param($f)$f.artifact.observations.nativeLoad.events[1].player=$f.artifact.observations.beforeLoad.menuPlayer;Verify $f}
Check 'PostLoad return cannot move to a different frame' $false {param($f)$f.artifact.observations.nativeLoad.events[1].frame++;Verify $f}
Check 'PostLoad native hook contract cannot drift' $false {param($f)$f.artifact.observations.nativeLoad.hooks[0].token='06000DC8';Verify $f}
Check 'PostLoad event singleton kind is refused' $false {param($f)$e=$f.artifact.observations.nativeLoad.events[1];$e.kind=@($e.kind);Verify $f}
Check 'source character cannot disappear from cold full field' $false {param($f)$f.artifact.observations.loadedWorld.party=@($f.artifact.observations.loadedWorld.party|Select-Object -Skip 1);Verify $f}
Check 'full field duplicate IDs are refused' $false {param($f)$f.artifact.observations.loadedWorld.party[1].id=$f.artifact.observations.loadedWorld.party[0].id;Verify $f}
Check 'native PartyCharacters cannot silently include pet' $false {param($f)$o=$f.artifact.observations;$o.loadedWorld.partyCharacterIds+=,$o.loadedWorld.party[2].id;Verify $f}
Check 'remote character cannot overlap field actor' $false {param($f)$o=$f.artifact.observations;$o.loadedWorld.remoteCompanionIds=@($o.loadedWorld.party[1].id);Verify $f}
Check 'pet ownership must remain reciprocal' $false {param($f)$f.artifact.observations.loadedWorld.party[2].masterId=$f.artifact.observations.loadedWorld.party[1].id;Verify $f}
Check 'pet identity singleton array is refused' $false {param($f)$a=$f.artifact.observations.loadedWorld.party[0];$a.petId=@($a.petId);Verify $f}
Check 'ordinary actor XP must match source' $false {param($f)$f.artifact.observations.loadedWorld.party[1].experience++;Verify $f}
Check 'main XP must match source readiness' $false {param($f)$f.artifact.observations.loadedWorld.party[0].experience++;Verify $f}
Check 'cold class ranks must match source' $false {param($f)$f.artifact.observations.loadedWorld.party[1].classes[0].guid='e'*32;Verify $f}
Check 'loaded native money must match source' $false {param($f)$f.artifact.observations.loadedWorld.nativeMoney++;Verify $f}
Check 'settled native money must match source' $false {param($f)$f.artifact.observations.settledWorld.nativeMoney++;Verify $f}
Check 'source native money scalar type is exact' $false {param($f)$f.artifact.observations.sourceNativeMoney=@($f.artifact.observations.sourceNativeMoney);Verify $f}
Check 'new native commands are refused' $false {param($f)$f.artifact.observations.loadedWorld.party[1].queuedCommands=1;Verify $f}
Check 'native scheduled dialogue prevents stable world proof' $false {param($f)$f.artifact.observations.loadedWorld.dialogScheduled=$true;Verify $f}
Check 'native state replacement during cleanup is refused' $false {param($f)$f.artifact.observations.settledWorld.state=$f.artifact.observations.settledWorld.areaState;Verify $f}
Check 'mounted owner count must remain zero' $false {param($f)$f.artifact.observations.loadedWorld.owners=1;Verify $f}
Check 'empty archive cannot contain pairs' $false {param($f)$f.artifact.observations.archive.pairs=1;Verify $f}
Check 'empty schema3 cannot use legacy schema2' $false {param($f)$f.artifact.observations.archive.schemaVersion=2;Verify $f}
Check 'archive decode cannot grant restoration' $false {param($f)$f.artifact.observations.archive.nativeRestorationGrantedByDiagnostic=$true;Verify $f}
Check 'native Prepare entry remains refused' $false {param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{generated=$true});Verify $f}
Check 'restoration observer must precede load' $false {param($f)$f.artifact.observations.noPreparation.installedBeforeWorldLoad=$false;Verify $f}
Check 'restoration native method token singleton is refused' $false {param($f)$h=$f.artifact.observations.noPreparation.hooks[0];$h.token=@($h.token);Verify $f}
Check 'fixture readiness Manual cannot change to Auto' $false {param($f)$f.artifact.observations.fixtureReadiness.isolatedSaveDescriptors.descriptors[0].type='Auto';Verify $f}
Check 'fixture readiness cannot imply live selector ownership' $false {param($f)$f.artifact.observations.fixtureReadiness.staticPartyFacts.membershipOrRemovalQualified=$true;Verify $f}
Check 'native profile inventory cannot grant pose qualification' $false {param($f)$f.artifact.observations.nativeProfileInventory.poseQualified=$true;Verify $f}
Check 'native profile inventory must retain main XP' $false {param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.mainCharacterExperience++;Verify $f}
Check 'native profile inventory must retain native money' $false {param($f)$f.artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney++;Verify $f}
Check 'native profile capture status singleton is refused' $false {param($f)$p=$f.artifact.observations.nativeProfileInventory;$p.captureStatus=@($p.captureStatus);Verify $f}
foreach($key in @('resourceWrites','xpWrites','moneyWrites','mountRequests','choiceRequests')){Check ('cold '+$key+' must remain zero') $false {param($f)$f.artifact.observations.$key=1;Verify $f}}
foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','worldAdmitted','sourcePinned','sourceUnchanged')){Check ('final cleanup '+$key+' must be true') $false {param($f)$f.artifact.observations.cleanup.$key=$false;Verify $f}}
Check 'manifest body mismatch is refused' $false {param($f)$m=Publish $f;Put $f.artifactPath '{}';Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'manifest matching singleton leaf is refused' $false {param($f)$m=Publish $f;$m.artifacts[0].relativePath=@($m.artifacts[0].relativePath);Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'manifest duplicate exact artifact is refused' $false {param($f)$m=Publish $f;$m.artifacts+=,$m.artifacts[0];Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'held source hash mismatch is refused' $false {param($f)$m=Publish $f;Put $f.sourcePath '{}';Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'matching source hash does not admit duplicate JSON members' $false {param($f)$m=Publish $f;Put $f.sourcePath '{"schemaVersion":1,"schemaVersion":1}';$hash=Get-KmcSha256 $f.sourcePath;$f.request.assistedColdInventory.sourceArtifactSha256=$hash;Assert-KmcChunk7AssistedColdInventoryArtifact $f.request $m}
Check 'matching singleton source observations object is refused' $false {param($f)$f.source.observations=@($f.source.observations);Verify $f}
Check 'matching singleton profile inventory object is refused' $false {param($f)$f.artifact.observations.nativeProfileInventory=@($f.artifact.observations.nativeProfileInventory);Verify $f}
Check 'matching singleton profile actor object is refused' $false {param($f)$p=$f.artifact.observations.nativeProfileInventory;$p.party[0]=@($p.party[0]);Verify $f}
Check 'matching singleton native readiness object is refused' $false {param($f)$p=$f.artifact.observations.nativeProfileInventory.party[0];$p.nativeLevelUpReadiness=@($p.nativeLevelUpReadiness);Verify $f}
Check 'matching singleton native prerequisites object is refused' $false {param($f)$p=$f.artifact.observations.nativeProfileInventory;$p.ordinaryAcquisitionPrerequisites=@($p.ordinaryAcquisitionPrerequisites);Verify $f}
Check 'matching singleton source native profile object is refused' $false {param($f)$s=$f.source.observations;$s.nativeProfileInventory=@($s.nativeProfileInventory);Verify $f}
Check 'matching singleton source native prerequisites is refused' $false {param($f)$p=$f.source.observations.nativeProfileInventory;$p.ordinaryAcquisitionPrerequisites=@($p.ordinaryAcquisitionPrerequisites);Verify $f}
Check 'matching singleton fixture readiness is refused' $false {param($f)$o=$f.artifact.observations;$o.fixtureReadiness=@($o.fixtureReadiness);Verify $f}
Check 'matching singleton fixture main progression is refused' $false {param($f)$r=$f.artifact.observations.fixtureReadiness;$r.mainProgression=@($r.mainProgression);Verify $f}
Check 'matching singleton source final Manual is refused' $false {param($f)$s=$f.source.observations;$s.finalManual=@($s.finalManual);Verify $f}
Check 'matching singleton source selection is refused' $false {param($f)$s=$f.source.observations;$s.selection=@($s.selection);Verify $f}
Check 'matching singleton successful observations is refused' $false {param($f)$f.artifact.observations=@($f.artifact.observations);Verify $f}
$changed=@($script:inputPins|Where-Object{(Get-KmcSha256 $_.path)-cne$_.sha256})
if($changed.Count-ne0){throw 'Assisted cold full-reader input changed.'}
$receipt=[ordered]@{schemaVersion=1;kind='assisted-cold-full-evidence-generated-composition';status=$(if($script:failed-eq0-and$script:seedVerified){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;positiveSeedVerified=$script:seedVerified;durationSeconds=$timer.Elapsed.TotalSeconds;inputPins=$script:inputPins.ToArray();changedInputs=$changed;cases=$cases.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;archiveDecodes=0;limits=@('Actual scenario Capture world/loader/cleanup/registry and raw field/character formatting is retained from generated actual Update/PollCleanup execution.','External native profile/restoration probe, installed MVID, protected request fixture, source envelope/proof flags and filesystem paths are controlled substitutions; complete producer qualification is not claimed or executed.','Readable exported source was reserialized by the native adapter fixture. This composition serializes and hashes its own generated complete source bytes; it does not claim the exported readable bytes were the native held source.','All actual evidence validators, manifest and held-source/hash/path checks execute unchanged. No native restoration, resources, fixture eligibility, ordinary progression or acquisition is qualified.')}
Write-KmcJsonAtomic (Join-Path $output 'receipt.json') $receipt
Write-Host ('ASSISTED COLD FULL EVIDENCE: '+$script:passed+' PASS / '+$script:failed+' FAIL')
if($script:failed-ne0-or-not$script:seedVerified){exit 1};exit 0
