$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$TrialBody){& $TrialBody;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$RejectedTrial){$refused=$false;try{& $RejectedTrial}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-TestValue($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
$first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mammoth='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';$horse='77777777-7777-4777-8777-777777777777'
$ids=@($first,$mammoth,$second,$horse)
$request=[pscustomobject]@{schemaVersion=2;scenario='chunk7-two-pair-exploration';runId='synthetic-c7-reader';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;
    productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';fixture=[pscustomobject]@{working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'}}}
function New-Artifact([string]$Kind,$Observations){
    $value=[ordered]@{schemaVersion=1;evidenceKind=$Kind;createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$Observations}
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$value[$field]=$request.$field}
    return [pscustomobject]$value
}
function Actor([string]$Id,[string]$Blueprint,[AllowNull()][string]$Pet,[AllowNull()][string]$Master){[pscustomobject]@{id=$Id;blueprint=$Blueprint;petId=$Pet;masterId=$Master}}
$party=@((Actor $first 'rider-one' $mammoth $null),(Actor $mammoth 'e7aa96d15a45238438ae4cfb476f6bb9' $null $first),(Actor $second 'rider-two' $null $null))
$withHorse=Copy-TestValue $party;$withHorse[2].petId=$horse;$withHorse+=Actor $horse '4016c7db400ab721ff125aef9e65e202' $null $second
$resources=[pscustomobject]@{gameTicks=10000000L;actors=@($ids|ForEach-Object{[pscustomobject]@{id=$_;move=0.0;standard=0.0;swift=0.0;prepared=$false;reactionAllowance=1;initiative=0}})}
$inputRows=@();$specs=@(@('mount-mammoth',$first,$mammoth,'f053faad986631688defa003cd7bda0e',$second),@('mount-horse',$second,$horse,'f053faad986631688defa003cd7bda0e',$first),
    @('dismount-mammoth',$first,$first,'3af2b81f4d72bbb30501fa730fcdf36e',$second),@('dismount-horse',$second,$second,'3af2b81f4d72bbb30501fa730fcdf36e',$first))
for($i=0;$i-lt4;$i++){$s=$specs[$i];$inputRows+=[pscustomobject]@{label=$s[0];casterId=$s[1];targetId=$s[2];abilityGuid=$s[3];clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;commandObject=100+$i;
    shellControl=('shell-'+$i);generationAtInit=1;selectionAfterInput=$s[4];shellTerminal=$true;processTerminal=$true;resourcesBefore=(Copy-TestValue $resources);resourcesAfter=(Copy-TestValue $resources)}}
$owners=@([pscustomobject]@{ownerId='11111111-1111-4111-8111-111111111111';riderId=$first;mountId=$mammoth;generation=1;state='Active';relationshipState='Mounted'},
    [pscustomobject]@{ownerId='22222222-2222-4222-8222-222222222222';riderId=$second;mountId=$horse;generation=2;state='Active';relationshipState='Mounted'})
$snapshots=@([pscustomobject]@{label='both-mounted';owners=(Copy-TestValue $owners)},[pscustomobject]@{label='movement-complete';owners=(Copy-TestValue $owners)},
    [pscustomobject]@{label='first-dismounted-second-retained';owners=@((Copy-TestValue $owners[1]))})
$moveRows=@();$moves=@(@('mammoth',$first,$mammoth,$second),@('horse',$second,$horse,$first),@('unmounted-control',$first,$first,$second))
for($i=0;$i-lt3;$i++){$s=$moves[$i];$commands=@($ids|ForEach-Object{[pscustomobject]@{id=$_;moveObject=0}});$after=Copy-TestValue $commands;@($after|Where-Object id -CEQ $s[2])[0].moveObject=200+$i
    $moveRows+=[pscustomobject]@{label=$s[0];selectedId=$s[1];executorId=$s[2];commandObject=200+$i;selectionAfterInput=$s[3];commandTerminal=$true;
        commandsBefore=$commands;commandsAfter=$after;start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};end=[pscustomobject]@{x=2.0;y=0.0;z=0.0};requestedGoal=[pscustomobject]@{x=2.0;y=0.0;z=0.0}}}
$observation=[pscustomobject]@{capability=[pscustomobject]@{archiveNativeEnabled=$true;nativeWriterSchema=3;nativePairCapacity=2;workingSha256=$request.fixture.working.sha256};partyBefore=$party;partyWithHorse=$withHorse;
    acquisition=[pscustomobject]@{nativeRangerCommitCount=4;huntersBondSelectionLevel=4;rangerCompanionSelectionLevel=4;horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true;horseFeatureSourceGuid='native-ranger'};
    inputs=$inputRows;snapshots=$snapshots;movements=$moveRows;combatOrPersistenceProof=$false;thirdUnrelatedActorProven=$false;
    cleanup=[pscustomobject]@{pairResidueAbsent=$true;horseRemoved=$true;controlsSettled=$true;settingsAndSelectionRestored=$true;nativeSaveWritten=$false;ordinaryRespecProven=$false;
        nativeProgressionRequiresExternalWorkingRestoration=$true;partyAfter=(Copy-TestValue $party);selectionBefore=@($first);selectionAfter=@($first);pauseBefore=$true;pauseAfter=$true;
        movementBefore=$false;movementAfter=$false;pairedBefore=$true;pairedAfter=$true;world=[pscustomobject]@{owners=@()};
        pairBoundaries=@([pscustomobject]@{state='Unmounted';residueAbsent=$true;ownerPresent=$false},[pscustomobject]@{state='Unmounted';residueAbsent=$true;ownerPresent=$false})}}
$subscriptions=[pscustomobject]@{executing=$false;uniqueOwnedSubscribers=1;ownedInterfaceEntries=1;entries=@([pscustomobject]@{interface='native-interface';type='KmcSubscriber';identity=77})}
$measured=@();$labels=@('unmounted-0','mounted-1','mounted-2','unmounted-after-cycles');$counts=@(0,1,2,0)
for($i=0;$i-lt4;$i++) {
    $ownership=[pscustomobject]@{active=$counts[$i];owners=$counts[$i];boundaries=2;failedConstructions=0;participants=2;liveControlBoundaries=0;preparedOrViewResidue=$counts[$i];allocationActors=0}
    $measured+=[pscustomobject]@{label=$labels[$i];warmupFrames=30;sampleFrames=120;startFrame=1;endFrame=121;stopwatchFrequency=1000000L;
        serviceTicks=@(1..120|ForEach-Object{100L});frameSeconds=@(1..120|ForEach-Object{0.016});serviceMeanMs=0.1;serviceP50Ms=0.1;serviceP95Ms=0.1;frameP50Ms=16.0;frameP95Ms=16.0;
        managedHeapBefore=100000L;managedHeapAfter=110000L;managedHeapDelta=10000L;forcedCollection=$false;collectionsBefore=@(1,1,1);collectionsAfter=@(1,1,1);kmcLogMessages=2L;kmcLogCharacters=30L;
        ownersBefore=(Copy-TestValue $ownership);ownersAfter=(Copy-TestValue $ownership);subscriptionsBefore=(Copy-TestValue $subscriptions);subscriptionsAfter=(Copy-TestValue $subscriptions)}
}
$cycleRows=@();$cycleSnapshots=@()
for($n=1;$n-le3;$n++) {
    foreach($input in $inputRows){$row=Copy-TestValue $input;$row.label='cycle-'+$n+'-'+$row.label;$row.commandObject+=400*$n;$row.shellControl='cycle-'+$n+'-'+$row.shellControl;$row.generationAtInit+=2*$n;$cycleRows+=$row}
    $cycleOwners=Copy-TestValue $owners;foreach($owner in $cycleOwners){$owner.ownerId=[Guid]::NewGuid().ToString();$owner.generation+=2*$n}
    $cycleSnapshots+=[pscustomobject]@{label=('cycle-'+$n+'-mounted');owners=$cycleOwners;boundaryCount=2;participantCount=2;subscriptions=(Copy-TestValue $subscriptions)}
    $cycleSnapshots+=[pscustomobject]@{label=('cycle-'+$n+'-settled');owners=@();boundaryCount=2;participantCount=2;subscriptions=(Copy-TestValue $subscriptions)}
}
$observation|Add-Member measurements $measured
$observation|Add-Member cycleInputs $cycleRows
$observation|Add-Member cycles $cycleSnapshots
$good=New-Artifact 'chunk7-world-evidence' $observation
Check 'complete synthetic exploration observations' {Assert-KmcChunk7WorldEvidence $request $good 'PASS'}
$mutations=@(
    @('source identity',{param($v)$v.commit='9'*40}),@('closed native gate',{param($v)$v.observations.capability.archiveNativeEnabled=$false}),
    @('native schema2',{param($v)$v.observations.capability.nativeWriterSchema=2}),@('one-pair native capacity',{param($v)$v.observations.capability.nativePairCapacity=1}),
    @('unqualified fixture',{param($v)$v.observations.capability.workingSha256='9'*64}),@('invented original actor',{param($v)$v.observations.partyBefore[0].id=$horse}),
    @('foreign companion owner',{param($v)$v.observations.partyWithHorse[3].masterId=$first}),@('original Mammoth replaced',{param($v)$v.observations.partyBefore[0].petId=$horse}),
    @('direct fact instead of native commits',{param($v)$v.observations.acquisition.nativeRangerCommitCount=0}),@('missing native input',{param($v)$v.observations.inputs=@($v.observations.inputs[0..2])}),
    @('wrong input actor',{param($v)$v.observations.inputs[1].casterId=$first}),@('repeat input command',{param($v)$v.observations.inputs[1].commandObject=100}),
    @('missing shell binding',{param($v)$v.observations.inputs[1].shellControl=$null}),@('unobserved native cast',{param($v)$v.observations.inputs[1].nativeCastDelta=0}),
    @('input boolean string',{param($v)$v.observations.inputs[1].clicked='true'}),@('unfinished native execution process',{param($v)$v.observations.inputs[1].processTerminal=$false}),@('execution process boolean string',{param($v)$v.observations.inputs[1].processTerminal='true'}),@('missing cooldown',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].move=$null}),
    @('native move spending',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].move=3.0}),@('native action refund',{param($v)$v.observations.inputs[1].resourcesBefore.actors[0].standard=3.0}),
    @('invented preparation',{param($v)$v.observations.inputs[1].resourcesAfter.actors[0].prepared=$true}),@('actor resource dropped',{param($v)$v.observations.inputs[1].resourcesAfter.actors=@($v.observations.inputs[1].resourcesAfter.actors[0..2])}),
    @('same pair owner',{param($v)$v.observations.snapshots[0].owners[1].ownerId=$v.observations.snapshots[0].owners[0].ownerId}),
    @('selection changes generation',{param($v)$v.observations.snapshots[1].owners[1].generation=3}),@('dismount drops other owner',{param($v)$v.observations.snapshots[2].owners=@()}),
    @('movement routes to rider',{param($v)$v.observations.movements[0].executorId=$first}),@('movement commands another actor',{param($v)$v.observations.movements[0].commandsAfter[0].moveObject=999}),
    @('no actual movement',{param($v)$v.observations.movements[0].end.x=0.0}),@('null movement coordinate',{param($v)$v.observations.movements[0].end.x=$null}),
    @('late movement command',{param($v)$v.observations.movements[0].commandTerminal=$false}),@('retained cleanup owner',{param($v)$v.observations.cleanup.pairBoundaries[0].ownerPresent=$true}),
    @('settings not restored',{param($v)$v.observations.cleanup.pairedAfter=$false}),@('claim unproved combat',{param($v)$v.observations.combatOrPersistenceProof=$true}),
    @('claim ordinary respec from cleanup',{param($v)$v.observations.cleanup.ordinaryRespecProven=$true}),
    @('invented timing summary',{param($v)$v.observations.measurements[2].serviceP95Ms=0.0}),
    @('missing sample window',{param($v)$v.observations.measurements=@($v.observations.measurements[0..2])}),
    @('empty timer samples',{param($v)$v.observations.measurements[1].serviceTicks=@()}),
    @('invented heap change',{param($v)$v.observations.measurements[2].managedHeapDelta=0}),
    @('forced collection substitutes quiet heap',{param($v)$v.observations.measurements[2].forcedCollection=$true}),
    @('additional global subscriber',{param($v)$v.observations.measurements[2].subscriptionsAfter.entries[0].identity=99}),
    @('wrong pair count sample',{param($v)$v.observations.measurements[1].ownersAfter.active=2}),
    @('missing cycle input',{param($v)$v.observations.cycleInputs=@($v.observations.cycleInputs[0..10])}),
    @('reused native cycle command',{param($v)$v.observations.cycleInputs[0].commandObject=100}),
    @('stale cycle generation',{param($v)$v.observations.cycles[0].owners[0].generation=1}),
    @('retained cycle owner',{param($v)$v.observations.cycles[1].owners=@($v.observations.cycles[0].owners[0])}),
    @('accumulated pair boundaries',{param($v)$v.observations.cycles[5].boundaryCount=3})
)
foreach($mutation in $mutations){$changed=Copy-TestValue $good;& $mutation[1] $changed;Reject $mutation[0] {Assert-KmcChunk7WorldEvidence $request $changed 'PASS'}}
Check 'FAIL partial artifact preserves failed prefix' {$partial=Copy-TestValue $good;$partial.status='FAIL';$partial.errors=@('fixture unavailable');$partial.observations=[pscustomobject]@{};Assert-KmcChunk7WorldEvidence $request $partial 'FAIL'}
Reject 'FAIL artifact cannot fulfill PASS run' {$partial=Copy-TestValue $good;$partial.status='FAIL';Assert-KmcChunk7WorldEvidence $request $partial 'PASS'}
$dependencies=[pscustomobject]@{gameRoot='C:\SyntheticKingmaker';modsRoot='C:\SyntheticKingmaker\Mods';files=@()}
$assemblyRows=@([pscustomobject]@{name='KingmakerMountedCombat';fullName='KingmakerMountedCombat';dynamic=$false;location='C:\SyntheticKingmaker\Mods\KingmakerMountedCombat\KingmakerMountedCombat.dll';sha256=$request.dllSha256;mvid=$request.dllMvid})
foreach($relative in @('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll')){
    $dependencies.files+=[pscustomobject]@{relativePath=$relative;sha256='8'*64};$assemblyRows+=[pscustomobject]@{name=([IO.Path]::GetFileNameWithoutExtension($relative));fullName=$relative;dynamic=$false;location=(Join-Path $dependencies.gameRoot $relative);sha256='8'*64;mvid='99999999-9999-4999-8999-999999999999'}
}
$inventory=New-Artifact 'runtime-loader-inventory' ([pscustomobject]@{capturedInNativeProcess=$true;processId=100;modEntries=@([pscustomobject]@{id='KingmakerMountedCombat';active=$true;enabled=$true});loadedAssemblies=$assemblyRows})
Check 'exact native base-only loader inventory' {Assert-KmcBaseGameNativeInventory $request $inventory $dependencies}
function New-TestAssembly([string]$Name,[string]$Location) {
    return [pscustomobject]@{name=$Name;fullName=$Name;dynamic=$false;location=$Location;sha256='7'*64;mvid='77777777-7777-4777-8777-777777777777'}
}
Check 'additional native Managed assembly is recorded with hash and MVID' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'System' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\System.dll'
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.fileBackedAssemblyCount-ne5-or$scope.completeStockAssemblyAllowlistProven-ne$false){throw 'Directory allowance was mistaken for a complete stock identity allowlist.'}
}
Check 'additional native UMM dependency has a file-backed identity' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'dnlib' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\UnityModManager\dnlib.dll'
    Assert-KmcBaseGameNativeInventory $request $extra $dependencies
}
Check 'dynamic assembly identities retained without qualification claim' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=[pscustomobject]@{name='DynamicNative';fullName='DynamicNative, Version=0.0.0.0';dynamic=$true;location=$null;sha256=$null;mvid=$null}
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.dynamicAssemblyCount-ne1-or$scope.dynamicAssemblies[0].fullName-cne'DynamicNative, Version=0.0.0.0'-or
        $scope.dynamicAssembliesQualified-ne$false-or$scope.completeStockAssemblyAllowlistProven-ne$false-or[string]::IsNullOrWhiteSpace($scope.limitation)){throw 'Dynamic evidence limit was dropped.'}
}
$inventoryMutations=@(@('foreign active mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$true;enabled=$true}}),
    @('foreign disabled mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$false;enabled=$false}}),@('KMC inactive',{param($v)$v.observations.modEntries[0].active=$false}),
    @('KMC enabled string',{param($v)$v.observations.modEntries[0].enabled='true'}),@('missing loaded Harmony',{param($v)$v.observations.loadedAssemblies=@($v.observations.loadedAssemblies[0..2])}),
    @('changed loaded KMC',{param($v)$v.observations.loadedAssemblies[0].sha256='1'*64}),@('changed loaded gameplay',{param($v)$v.observations.loadedAssemblies[1].sha256='1'*64}),
    @('foreign Mods assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Mods\ForeignMod\Foreign.dll'}),
    @('external file-backed assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\ForeignMods\Foreign.dll'}),
    @('Managed sibling prefix path',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed-foreign\Foreign.dll'}),
    @('Managed nested foreign directory',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\Foreign\Foreign.dll'}),
    @('Managed traversal outside exact roots',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\..\Foreign.dll'}),
    @('non-dynamic assembly without location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' $null}),
    @('non-dynamic assembly with whitespace location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' ' '}),
    @('relative file-backed assembly location',{param($v)$v.observations.loadedAssemblies[1].location='Kingmaker_Data\Managed\Assembly-CSharp.dll'}),
    @('KMC loaded from Managed instead of candidate path',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Kingmaker_Data\Managed\KingmakerMountedCombat.dll'}),
    @('KMC loaded from another Mods directory',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Mods\ForeignMod\KingmakerMountedCombat.dll'}),
    @('missing observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256=$null}),
    @('malformed observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256='not-a-hash'}),
    @('missing observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid=$null}),
    @('malformed observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='not-a-guid'}),
    @('empty observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='00000000-0000-0000-0000-000000000000'}),
    @('missing assembly identity field',{param($v)$v.observations.loadedAssemblies[1].PSObject.Properties.Remove('fullName')}),
    @('assembly observation error hidden by PASS',{param($v)$v.observations.loadedAssemblies[1]|Add-Member observationError 'hash unavailable'}),
    @('dynamic assembly claims a file identity',{param($v)$v.observations.loadedAssemblies[1].dynamic=$true}),
    @('dynamic KMC cannot stand in for candidate',{param($v)$v.observations.loadedAssemblies[0].dynamic=$true;$v.observations.loadedAssemblies[0].location=$null;$v.observations.loadedAssemblies[0].sha256=$null;$v.observations.loadedAssemblies[0].mvid=$null}),
    @('duplicated required loaded gameplay assembly',{param($v)$v.observations.loadedAssemblies+=(Copy-TestValue $v.observations.loadedAssemblies[1])}),
    @('process ID string',{param($v)$v.observations.processId='100'}))
foreach($mutation in $inventoryMutations){$changed=Copy-TestValue $inventory;& $mutation[1] $changed;Reject $mutation[0] {Assert-KmcBaseGameNativeInventory $request $changed $dependencies}}
Check 'exact compound/request case registered once' {if(-not(Test-KmcCompoundRuntimeScenario $request.scenario)-or@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq$request.scenario}).Count-ne1){throw 'World case registration differs.'}}
Check 'new scripts parse' {foreach($file in @('runtime/Chunk7WorldEvidence.ps1','runtime/Invoke-KingmakerRuntimeScenario.ps1','runtime/Test-RuntimeRequest.ps1','runtime/Test-RuntimeResult.ps1','runtime/Test-RuntimeGameResult.ps1')){$syntax=$null;$tokens=$null;[void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $file),[ref]$tokens,[ref]$syntax);if($syntax.Count-ne0){throw ($file+': '+($syntax|Out-String))}}}
# Synthetic filesystem artifacts verify the exact manifest/immutable-byte boundary.
$fixtureRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/chunk7-reader-tests/'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $fixtureRoot)
$boundRequest=Copy-TestValue $request;$boundRequest|Add-Member evidenceRoot $fixtureRoot
$worldPath=Join-Path $fixtureRoot 'chunk7-world-evidence.json';$loaderPath=Join-Path $fixtureRoot 'runtime-loader-inventory.json'
Write-KmcJsonAtomic $worldPath $good;Write-KmcJsonAtomic $loaderPath $inventory
$visualRoot=Join-Path $fixtureRoot 'movement-visuals';[void](New-Item -ItemType Directory -Path $visualRoot)
$visualPath=Join-Path $visualRoot 'chunk7-two-pair-mounted.png'
[IO.File]::WriteAllBytes($visualPath,[Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII='))
$artifactRows=@(foreach($pair in @(@($worldPath,'chunk7-world-evidence.json','chunk7-world-evidence'),@($loaderPath,'runtime-loader-inventory.json','runtime-loader-inventory'),@($visualPath,'movement-visuals/chunk7-two-pair-mounted.png','screenshot'))){[pscustomobject]@{relativePath=$pair[1];kind=$pair[2];length=[long](Get-Item -LiteralPath $pair[0]).Length;sha256=(Get-KmcSha256 $pair[0])}})
$manifest=[pscustomobject]@{schemaVersion=1;runId=$request.runId;scenario=$request.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=$artifactRows}
$manifestPath=Join-Path $fixtureRoot 'runtime-artifacts.json';Write-KmcJsonAtomic $manifestPath $manifest
Check 'exact world and loader artifact manifest allowed' {[void](Assert-KmcReadOnlyArtifactManifest $boundRequest (Get-KmcSha256 $manifestPath));Assert-KmcChunk7WorldArtifact $boundRequest $manifest 'PASS'}
Reject 'missing screenshot manifest receipt' {$missing=Copy-TestValue $manifest;$missing.artifacts=@($missing.artifacts[0..1]);Assert-KmcChunk7WorldArtifact $boundRequest $missing 'PASS'}
Reject 'world artifact under other request' {$other=Copy-TestValue $boundRequest;$other.scenario='chunk7-two-pair-combat';Assert-KmcChunk7WorldArtifact $other $manifest 'PASS'}
Reject 'artifact identity changed' {$bad=Copy-TestValue $good;$bad.dllSha256='9'*64;Write-KmcJsonAtomic $worldPath $bad;Assert-KmcChunk7WorldArtifact $boundRequest $manifest 'PASS'}
Write-KmcJsonAtomic $worldPath $good
Reject 'unregistered world evidence kind' {$bad=Copy-TestValue $manifest;$bad.artifacts[0].kind='chunk7-combat-evidence';Write-KmcJsonAtomic $manifestPath $bad;[void](Assert-KmcReadOnlyArtifactManifest $boundRequest (Get-KmcSha256 $manifestPath))}
Write-KmcJsonAtomic $manifestPath $manifest
Check 'foreign scenario cannot claim missing world evidence as PASS' {$other=Copy-TestValue $boundRequest;$other.scenario='mod-load-smoke';$empty=Copy-TestValue $manifest;$empty.artifacts=@();Assert-KmcChunk7WorldArtifact $other $empty 'PASS'}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0; synthetic reader contracts only, no native qualification')
