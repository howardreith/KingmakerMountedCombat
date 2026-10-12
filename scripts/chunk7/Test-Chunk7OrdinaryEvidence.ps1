[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
$ReaderDirectory=[IO.Path]::GetFullPath($ReaderDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OrdinaryAcquisitionEvidence.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OrdinaryAcquisitionFixtures.ps1')
$script:passes=0
function Copy-O($Value){ConvertFrom-Json -InputObject (ConvertTo-KmcChunk7Canonical $Value)}
function Good([string]$Name,[scriptblock]$Body){& $Body;$script:passes++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Body){$bad=$false;try{& $Body}catch{$bad=$true};if(-not$bad){throw ('Accepted invalid ordinary evidence: '+$Name)};$script:passes++;Write-Host ('PASS refuses '+$Name)}
function New-OrdinaryFixture([bool]$Paid=$true){
    $original=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')
    $actor=if($Paid){'11111111-2222-4333-8444-555555555555'}else{$original[2]};$pet='66666666-7777-4888-8999-aaaaaaaaaaaa'
    $obj=if($Paid){99}else{3};$money=20000;$cost=if($Paid){2000}else{0};$initial=if($Paid){0}else{1};$count=4-$initial
    $selection=[pscustomobject]@{case=if($Paid){'paid-small-ranger-horse'}else{'existing-ranger-horse'};inventoryRunId='source-world';inventoryArtifactSha256='a'*64;
        smallRaceGuid=if($Paid){'1'*32}else{$null};smallPresetGuid=if($Paid){'2'*32}else{$null};skeletonName=if($Paid){'ObservedSmallRig'}else{$null};
        gender=if($Paid){'Male'}else{$null};leftHanded=if($Paid){$false}else{$null};existingActorId=if($Paid){$null}else{$actor};minimumRangerLevel=4;expectedNativeWriteCount=1;nativeWriteLeaf='Auto_1.zks'}
    $request=[pscustomobject]@{schemaVersion=2;scenario='chunk7-ordinary-acquisition';runId='synthetic-ordinary';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;productVersion='synthetic';
        dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';evidenceRoot=(Join-Path $PSScriptRoot 'fixture-lab/runtime-evidence/synthetic-ordinary');ordinaryAcquisition=$selection;
        fixture=[pscustomobject]@{working=[pscustomobject]@{sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6';gameId='88888888-1111-4222-8333-aaaaaaaaaaaa';gameName='Synthetic';area='3'*32}}}
    $root=Join-Path (Join-Path $PSScriptRoot 'fixture-lab') 'runtime-staging/persistence-synthetic-ordinary/Saved Games'
    function Snap([int]$Level,[int]$Money,[bool]$Complete=$false){[pscustomobject]@{actorId=$actor;actorObject=$obj;descriptorObject=100+$obj;nativeControllerObject=555;
        level=$Level;rangerLevel=$Level;xp=6000;money=$Money;nativeCanLevelUp=($Level-lt4);nativeIsComplete=$Complete;nativeMode=if($Level-eq0){'CharGen'}else{'LevelUp'};
        phase=if($Complete){'Total'}else{'Class'};nextLevel=$Level+1;selectedClass='cda0615668a6df14eb36ba19ee881af6';attributePoints=0;distributionPoints=0;
        skillTotal=4;skillSpent=4;skillRemaining=0;pendingFeatures=0;pendingSpellGroups=0;petId=if($Level-eq4){$pet}else{$null};petBlueprint=if($Level-eq4){'4016c7db400ab721ff125aef9e65e202'}else{$null}}}
    $events=[Collections.Generic.List[object]]::new();$frame=0
    function Event([string]$Name,$Value){$script:unused=$null;$Value|Add-Member -NotePropertyName event -NotePropertyValue $Name;$Value|Add-Member -NotePropertyName runId -NotePropertyValue $request.runId;$Value|Add-Member -NotePropertyName frame -NotePropertyValue $events.Count;$Value|Add-Member -NotePropertyName elapsedSeconds -NotePropertyValue ([double]$events.Count/10);$events.Add($Value)}
    if($Paid){Event 'native-paid-hire-admission' ([pscustomobject]@{moneyBefore=$money;nativeCost=$cost;nativeInheritedXp=6000;nativeExpectedFinalLevel=4;nativeInventorySha256=$selection.inventoryArtifactSha256});Event 'native-new-actor-bound' (Snap 0 $money)}
    else{Event 'native-existing-level-admission' (Snap $initial $money)}
    $accepted=0
    for($level=$initial;$level-lt4;$level++){
        $atMoney=if($Paid-and$level-eq0){$money}else{$money-$cost}
        Event 'native-level-open' (Snap $level $atMoney)
        $specs=,@('class','cda0615668a6df14eb36ba19ee881af6',$null)
        if($level-eq3){$specs+=@(@('feature','ee63330662126374e8785cc901941ac7/None','b705c5184a96a84428eeb35ae2517a14'),@('feature','7db7c50677e39f09feef56f3831fc723/None','ee63330662126374e8785cc901941ac7'))}
        foreach($spec in $specs){
            $before=Snap $level $atMoney
            Event 'native-choice-requested' ([pscustomobject]@{kind=$spec[0];choice=$spec[1];nativeSelectionGuid=$spec[2];before=(Copy-O $before)})
            Event 'native-choice' ([pscustomobject]@{kind=$spec[0];choice=$spec[1];nativeSelectionGuid=$spec[2];before=(Copy-O $before);after=(Copy-O $before);nativeChoiceAccepted=$true});$accepted++
        }
        $total=Snap $level $atMoney $true;Event 'native-total-next-requested' $total
        Event 'native-level-committed' ([pscustomobject]@{before=(Copy-O $total);after=(Snap ($level+1) ($money-$cost));moneyExpected=$money-$cost;commitCount=$level-$initial+1})
    }
    $last=Snap 4 ($money-$cost);$last.nativeControllerObject=0
    Event 'native-builder-complete' ([pscustomobject]@{actor=(Copy-O $last);ordinaryChoicesComplete=$true;nativeFieldAdmissionProven=$false;nativeSaveSettlementProven=$false;poseQualified=$false})
    $beforeActors=@();for($i=0;$i-lt3;$i++){$beforeActors+=[pscustomobject]@{id=$original[$i];object=$i+1;inState=$true;inGame=$true;level=1;xp=6000;petId=if($i-eq0){$original[1]}else{$null}}}
    $afterActors=Copy-O $beforeActors;if(-not$Paid){$afterActors[2].level=4;$afterActors[2].petId=$pet}
    $partyBefore=[pscustomobject]@{money=$money;partyCharacters=@($original[0],$original[2]);remoteCompanions=@();originalActors=$beforeActors}
    $partyAfter=[pscustomobject]@{money=$money-$cost;partyCharacters=@($original[0],$original[2])+$(if($Paid){@($actor)}else{@()});remoteCompanions=@();originalActors=$afterActors}
    $authority=[pscustomobject]@{root=$root;generation=2;exactScope=$true;maximumNativeWrites=1;admittedNativeWrites=0;authorizedLoads=1;authorizedWrites=0;unauthorizedLoads=0;unauthorizedWrites=0;fatalViolations=0;suppressedWrites=0}
    $finalAuthority=Copy-O $authority;$finalAuthority.admittedNativeWrites=1;$finalAuthority.authorizedWrites=1
    $hooks=@();foreach($spec in @(@('Kingmaker.Controllers.LevelUpAutoSaveController','HandleLevelUpComplete','0600906B'),@('Kingmaker.Game','SaveGame','06000CE4'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025'))){$hooks+=[pscustomobject]@{type=$spec[0];method=$spec[1];token=$spec[2];moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}}
    $probeRows=@();for($i=1;$i-le$count;$i++){
        $event=[pscustomobject]@{kind='native-level-completion-enter';frame=$i;actorId=$actor;actorObject=$obj;characterLevel=$initial+$i;inState=$true;isChargen=($i-lt$count);saveObject=0;saveType=$null;saveName=$null;saveLeaf=$null;autoLevelup=$null;savePath=$null;completionActorId=$actor;insideObservedCompletion=$true}
        $probeRows+=Copy-O $event
        if($i-eq$count){$save=Copy-O $event;$save.kind='native-level-autosave-request';$save.saveObject=999;$save.saveType='Auto';$save.saveLeaf='Auto_1.zks';$save.autoLevelup=$true;$probeRows+=Copy-O $save;$save.kind='native-level-autosave-prepared';$probeRows+=Copy-O $save}
        $event.kind='native-level-completion-exit';$probeRows+=Copy-O $event
    }
    $inventory=[pscustomobject]@{schemaVersion=1;captureStatus='PASS';gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';profileAdmissionChanged=$false}
    $body=[pscustomobject]@{OriginalSizeOrdinal=3;EffectiveSizeOrdinal=3;NativeRaceAndPresetIdentityExact=$true;NativeSkeletonIsExactPreset=$true;NativeRigAvailable=$true;RiderPolymorphed=$false;MountPolymorphed=$false;RaceGuid=$selection.smallRaceGuid;PresetGuid=$selection.smallPresetGuid;Gender=$selection.gender;SkeletonName=$selection.skeletonName;LeftHanded=$false}
    $o=[pscustomobject]@{case=$selection.case;selection=(Copy-O $selection);processId=123;elapsedSeconds=25.0;inventoryProvenance=[pscustomobject]@{runId=$selection.inventoryRunId;sha256=$selection.inventoryArtifactSha256;length=100;sameCandidate=$true;sameWorking=$true};
        nativeProgressionRequiresExternalWorkingRestoration=$true;vendorDialogueProven=$false;freshMainCampaignProven=$false;smallPoseQualified=$false;mountedCombatProven=$false;originalParty=$original;choices=$events.ToArray();
        authority=$authority;before=$partyBefore;builderComplete=$last;acceptedChoiceCount=$accepted;nativeCommitCount=$count;
        nativeObserver=[pscustomobject]@{installationAttempted=$true;installed=$true;complete=$true;errors=@();hooks=$hooks;events=$probeRows;completionEntries=$count;completionExits=$count;saveRequests=1;preparedWrites=1};
        nativeWrite=[pscustomobject]@{path=(Join-Path $root 'Auto_1.zks');leaf='Auto_1.zks';type='Auto';operation='None';sha256='f'*64;gameId=$request.fixture.working.gameId;gameName=$request.fixture.working.gameName;area=$request.fixture.working.area;length=100;schema=3;name='Autosave';authorizedWrites=1;admittedWrites=1;completedWrites=1;snapshotCount=1;autoLevelup=$true;nativeCallbackSupplied=$false;saveBeforeFieldSelection=$Paid};
        field=[pscustomobject]@{actorId=$actor;actorObject=$obj;petId=$pet;horseBlueprint='4016c7db400ab721ff125aef9e65e202';featureBlueprint='7db7c50677e39f09feef56f3831fc723';featureSourceGuid='ee63330662126374e8785cc901941ac7';rank=1;reciprocal=$true;featureSourcePresent=$true;ordinaryFieldAdmissionProven=$true;parties=(Copy-O $partyAfter);profileInventory=$inventory;bodyObservation=$body};
        fieldSelection=[pscustomobject]@{manualPartyMutation=$false;nativeCap=6;actorId=$actor;action='native-ShowPartySelection';openActiveSelf=$true;readyActiveInHierarchy=$true;readyAlpha=1.0;closedActiveSelf=$false};
        cleanup=[pscustomobject]@{custodySettled=$true;acquiredNativeStateRetained=$true;nativeBuilderClosed=$true;nativeWorkerSettled=$true;selectionRestored=$true;pauseRestored=$true;manualRefunds=0;manualFactRemovals=0;manualCompanionRemovals=0;after=(Copy-O $partyAfter);authority=$finalAuthority}}
    $artifact=[ordered]@{schemaVersion=1;evidenceKind='chunk7-ordinary-acquisition-evidence';createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$o}
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$artifact[$field]=$request.$field}
    [pscustomobject]@{request=$request;artifact=[pscustomobject]$artifact}
}
if($FixturesOnly){return}
Good 'paid native choices, one causal autosave and separate field admission' {$f=New-OrdinaryFixture;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
Good 'naturally ready existing Ranger levels retain original identity' {$f=New-OrdinaryFixture $false;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
$cases=[ordered]@{
 'string write count'={param($f)$f.artifact.observations.nativeWrite.completedWrites='1'}
 'boolean write count'={param($f)$f.artifact.observations.nativeWrite.completedWrites=$true}
 'fractional native count'={param($f)$f.artifact.observations.acceptedChoiceCount=6.0}
 'second write'={param($f)$f.artifact.observations.nativeWrite.authorizedWrites=2}
 'authority rebound'={param($f)$f.artifact.observations.cleanup.authority.generation=3}
 'authority reference lost'={param($f)$f.artifact.observations.cleanup.authority.exactScope=$false}
 'free hire substitute'={param($f)$f.artifact.observations.choices[0].nativeCost=0}
 'XP changed in choice'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).after.xp++}
 'actor object changed'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).after.actorObject++}
 'native rejected choice'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).nativeChoiceAccepted=$false}
 'skipped mandatory skills'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).skillRemaining=1}
 'native Total incomplete'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).nativeIsComplete=$false}
 'native XP readiness false'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).nativeCanLevelUp=$false}
 'level skipped'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-level-committed'|Select-Object -First 1).after.level++}
 'native payment refunded'={param($f)$f.artifact.observations.cleanup.after.money+=2000}
 'native payment twice'={param($f)($f.artifact.observations.choices|Where-Object event -CEQ 'native-level-committed'|Select-Object -Last 1).after.money-=2000}
 'unsourced companion choice'={param($f)($f.artifact.observations.choices|Where-Object{ $_.event-ceq'native-choice'-and$_.kind-ceq'feature'}|Select-Object -Last 1).nativeSelectionGuid='a'*32}
 'manual callback manufactured'={param($f)$f.artifact.observations.nativeWrite.nativeCallbackSupplied=$true}
 'autosave outside completion'={param($f)($f.artifact.observations.nativeObserver.events|Where-Object kind -CEQ 'native-level-autosave-request').insideObservedCompletion=$false}
 'completion callback unfinished'={param($f)$f.artifact.observations.nativeObserver.completionExits--}
 'wrong prepared descriptor'={param($f)($f.artifact.observations.nativeObserver.events|Where-Object kind -CEQ 'native-level-autosave-prepared').saveObject++}
 'observer installation incomplete'={param($f)$f.artifact.observations.nativeObserver.installed=$false}
 'foreign installed MVID'={param($f)$f.artifact.observations.nativeObserver.hooks[0].moduleMvid='1'*32}
 'wrong native archive category'={param($f)$f.artifact.observations.nativeWrite.type='Manual'}
 'invented field-before-save proof'={param($f)$f.artifact.observations.nativeWrite.saveBeforeFieldSelection=$false}
 'native field Small race differs'={param($f)$f.artifact.observations.field.bodyObservation.RaceGuid='a'*32}
 'unobserved native selector activation'={param($f)$f.artifact.observations.fieldSelection.openActiveSelf=$false}
 'text native selector activation'={param($f)$f.artifact.observations.fieldSelection.openActiveSelf='true'}
 'selector input before opening animation settles'={param($f)$f.artifact.observations.fieldSelection.readyAlpha=0.5}
 'text selector animation observation'={param($f)$f.artifact.observations.fieldSelection.readyAlpha='1'}
 'selector input under inactive hierarchy'={param($f)$f.artifact.observations.fieldSelection.readyActiveInHierarchy=$false}
 'native selector still active after Go'={param($f)$f.artifact.observations.fieldSelection.closedActiveSelf=$true}
 'size effect substituted for Small'={param($f)$f.artifact.observations.field.bodyObservation.OriginalSizeOrdinal=4}
 'foreign rig identity'={param($f)$f.artifact.observations.field.bodyObservation.NativeSkeletonIsExactPreset=$false}
 'acquired companion removed'={param($f)$f.artifact.observations.cleanup.manualCompanionRemovals=1}
 'cleanup native worker unfinished'={param($f)$f.artifact.observations.cleanup.nativeWorkerSettled=$false}
 'cleanup unproven'={param($f)$f.artifact.observations.cleanup.custodySettled=$false}
 'unrelated actor XP altered'={param($f)$f.artifact.observations.cleanup.after.originalActors[0].xp++}
 'unrelated actor removed from party'={param($f)$f.artifact.observations.cleanup.after.partyCharacters=@($f.artifact.observations.field.actorId)}
 'unearned fresh campaign claim'={param($f)$f.artifact.observations.freshMainCampaignProven=$true}
 'unearned Small pose claim'={param($f)$f.artifact.observations.smallPoseQualified=$true}
}
foreach($pair in $cases.GetEnumerator()){Bad $pair.Key {$f=New-OrdinaryFixture;& $pair.Value $f;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}}
function Rewrite-OrdinaryNumber($Object,[string[]]$Fields,$Value){
    if($null-eq$Object){return}
    if($Object-is[Array]){foreach($item in $Object){Rewrite-OrdinaryNumber $item $Fields $Value};return}
    if($Object-isnot[pscustomobject]){return}
    foreach($p in $Object.PSObject.Properties){if($Fields-ccontains$p.Name){$p.Value=$Value}else{Rewrite-OrdinaryNumber $p.Value $Fields $Value}}
}
foreach($form in @('text','double','boolean','array')){
    Bad ('prepared descriptor coerced from '+$form) {$f=New-OrdinaryFixture;$row=@($f.artifact.observations.nativeObserver.events|Where-Object kind -CEQ 'native-level-autosave-prepared')[0]
        $row.saveObject=switch($form){'text'{'999'};'double'{[double]999};'boolean'{$true};'array'{,@(999)}}
        Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
}
foreach($field in @('object','xp','level')){
    Bad ('original native '+$field+' beyond declared range') {$f=New-OrdinaryFixture;$value=if($field-ceq'level'){21}else{[long]2147483648}
        $f.artifact.observations.before.originalActors[0].$field=$value;$f.artifact.observations.cleanup.after.originalActors[0].$field=$value
        Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
}
Bad 'all progression XP consistently exceeds native Int32' {$f=New-OrdinaryFixture;Rewrite-OrdinaryNumber $f.artifact @('xp','nativeInheritedXp') ([long]2147483648);Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
Bad 'zero original actor identity' {$f=New-OrdinaryFixture;$f.artifact.observations.before.originalActors[0].object=0;$f.artifact.observations.cleanup.after.originalActors[0].object=0;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
Good 'signed native identities remain valid' {$f=New-OrdinaryFixture;Rewrite-OrdinaryNumber $f.artifact @('actorObject') (-99);Rewrite-OrdinaryNumber $f.artifact @('descriptorObject') (-100)
    foreach($row in $f.artifact.observations.nativeObserver.events){if($row.saveObject-ne0){$row.saveObject=-999}}
    $f.artifact.observations.field.actorObject=-99;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
Good 'native Int64 money range retained' {$f=New-OrdinaryFixture $false;Rewrite-OrdinaryNumber $f.artifact @('money','moneyExpected') ([long]2147483648);Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact}
foreach($field in @('expectedNativeWriteCount','minimumRangerLevel')){Bad ('request typed '+$field) {$f=New-OrdinaryFixture;$f.request.ordinaryAcquisition.$field=[string]$f.request.ordinaryAcquisition.$field;Assert-KmcChunk7OrdinarySelection $f.request}}
Bad 'ordinary request under historical family' {$f=New-OrdinaryFixture;$f.request.scenario='persistence-p01-save';Assert-KmcChunk7OrdinarySelection $f.request}
Bad 'unknown ordinary case' {$f=New-OrdinaryFixture;$f.request.ordinaryAcquisition.case='paid-any-body';Assert-KmcChunk7OrdinarySelection $f.request}
Bad 'ordinary source same run' {$f=New-OrdinaryFixture;$f.request.ordinaryAcquisition.inventoryRunId=$f.request.runId;Assert-KmcChunk7OrdinarySelection $f.request}
Bad 'unknown input fields' {$f=New-OrdinaryFixture;$f.request.ordinaryAcquisition|Add-Member -NotePropertyName grantXp -NotePropertyValue 100;Assert-KmcChunk7OrdinarySelection $f.request}
Good 'failed artifact retained without passing progression claims' {$f=New-OrdinaryFixture;$f.artifact.status='FAIL';$f.artifact.errors=@('actual native refusal');$f.artifact.observations=[pscustomobject]@{};Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact 'FAIL'}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic strict-reader checks only')
