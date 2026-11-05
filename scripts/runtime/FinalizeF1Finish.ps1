Set-StrictMode -Version Latest

# F1 consumes an already closed checkpoint, not another acquisition campaign.
# These are the existing literal16 pins, including the archive, native PASS,
# observations, original restoration and historical re-evaluation identities.
# Recheck every byte/length/time/link pin at intake and under lock. No caller-
# supplied receipt or generic cache authority is introduced.
function Get-KmcF1FinishSource {
 $pins=@(Get-KmcChunk7SixPairCheckpointPins);Assert-KmcChunk7SixPairCheckpointPins $pins
 $request=Read-KmcJson $pins[0].path
 $path=$pins[11].path
 [pscustomobject][ordered]@{path=$path;descriptor=$request.persistenceLoad;
  qualificationRunId='20261022-c7b-world185-b185s1-sixthhorse-cold';archiveProducerRunId='20261022-c7b-world184-b184s1-sixthhorse-manual';receiptPins=$pins}
}

function Get-KmcF1FinishChecks {
 @(Get-KmcChunk7NormalPlayerChecks -PreviousPackageLoad | ForEach-Object{$_.Replace('the-declared-previous-package','the-candidate').Replace('UMM-runs-exactly-KMC-and-the-observer-both-active','UMM-runs-exactly-KMC-ScrollFix-and-observer-all-active')}) + @(
 'all-four-declared-roles-are-loaded-with-views','each-declared-Horse-is-its-rider''s-own-native-pet','the-fixture-starts-unmounted-and-idle','the-rider-owns-the-ordinary-native-Mount-ability-without-any-grant',
 'native-mount-command-completed-successfully','the-rider-is-attached-to-its-own-Horse','the-neighboring-pair-is-untouched-after-mount','ordinary-Dismount-control-is-available-while-mounted',
 'f1-fresh-controller-has-no-inherited-MountedCombat-fact-or-selection','f1-rank-removal-rebinds-current-preview-disabled-row-and-clears-selection','f1-two-cancellations-follow-rank-removal-and-genuinely-fresh-preview',
 'f1-presentation-setup-is-exact-feature-and-disposable-rider','f1-native-feature-grant-provides-real-reaction-toggle','f1-native-reaction-on-and-off-transitions-observed')
}
function Assert-KmcF1FinishObserverResult($Request,$ObserverRequest,$Result) {
 if($ObserverRequest.normalPlayer.mode-cne'finalize-f1-finish'-or$ObserverRequest.timeoutSeconds-ne1100-or
    $ObserverRequest.candidate.dllSha256-cne$Request.dllSha256-or$ObserverRequest.candidate.dllMvid-cne$Request.dllMvid){throw 'F1 finishing observer binding differs.'}
 $names=@($Result.checks|ForEach-Object{$_.name}|Sort-Object)
 if(($names-join'|')-cne((Get-KmcF1FinishChecks|Sort-Object)-join'|')){throw 'F1 finish PASS check set differs.'}
 $o=$Result.observations;$n=$Request.normalPlayer
 Assert-KmcExactProperties $o @('routing','f1Creators','presenceAtMenu','profileBefore','world','controlsBefore','presenceLoaded','f1PresentationSetup','f1Reaction','inputs','controlsAfter','presenceAfter','archive','profileAfter','log') 'F1 finish observations'
 if($o.f1Creators.closed-ne2-or$o.f1Creators.rankRemoved-ne$true-or$o.f1Creators.fresh-ne$true-or
    $o.f1Reaction.observedOnTransition-ne$true-or$o.f1Reaction.observedOffTransition-ne$true-or
    $o.f1PresentationSetup.riderId-cne$n.riderId-or$o.f1PresentationSetup.featureGuid-cne'a7d2e94c6f1b4830b5c9e28d17f3a604'-or
    $o.f1PresentationSetup.toggleGuid-cne'3c81f5a02be74d96a4e07d1c92b6f385'-or$o.f1PresentationSetup.ordinaryAcquisitionProven-ne$false-or$o.f1PresentationSetup.saveWriteAuthorized-ne$false){throw 'F1 finish native sequence/setup incomplete.'}
 $events=@($o.f1Creators.events)
 if(($events.event-join'|')-cne'creator-open|creator-cancel|creator-open|creator-cancel'){throw 'F1 creator lifecycle order differs.'}
 $profile=[IO.Path]::GetFullPath([string]$ObserverRequest.profileRoot)
 if($o.routing.observedSavePath-cne(Join-Path $profile 'Saved Games')-or$o.routing.observedStashFolder-cne(Join-Path $profile 'Areas')-or
    $o.world.area-cne$Request.persistenceLoad.area-or$o.world.gameId-cne$Request.persistenceLoad.gameId-or$o.world.inCombat-ne$false){throw 'F1 scene/routing identity differs.'}
 foreach($p in @($o.presenceAtMenu,$o.presenceLoaded,$o.presenceAfter)){
  if((@($p.modEntries|Sort-Object)-join'|')-cne'KingmakerLevelUpSelectionScrollFix:active|KingmakerMountedCombat:active|KmcRemovalObserver:active'-or
     @($p.kmcRuntimeArguments).Count-ne0-or$p.kmcAssemblies-ne1-or$p.kmcDllSha256-cne$Request.dllSha256-or$p.kmcMvid-cne$Request.dllMvid-or
     $p.observerDllSha256-cne$n.observer.dllSha256-or$p.observerMvid-cne$n.observer.dllMvid-or$p.rootPresent-ne$true-or
     $p.automationHostField-cne'null'-or$p.staticAutomationHost-cne'null'-or$p.measurementSession-cne'null'-or$p.movementTelemetry-cne'null'-or
     $p.overlayObjects-ne0-or$p.effectiveSettings.EnableDiagnosticOverlay-ne$false){throw 'F1 ordinary KMC/observer presence differs.'}
 }
 $i=@($o.inputs)
 if($i.Count-ne1-or$i[0].label-cne'mount'-or$i[0].casterId-cne$n.riderId-or$i[0].targetId-cne$n.mountId-or$i[0].attachedTo-cne$n.mountId-or
    $i[0].result-cne'Success'-or$i[0].clicked-ne$true-or$i[0].commandCreated-ne$true-or$i[0].neighborUnchanged-ne$true-or
    $o.archive.sha256Before-cne$Request.persistenceLoad.sha256-or$o.archive.path-cne(Join-Path $o.routing.observedSavePath $Request.persistenceLoad.fileName)-or$o.log.kmcErrors-ne0){throw 'F1 normal Mount/archive evidence differs.'}
}

# The new run's own captured intake is authority. Only the observed creation UI
# preference family is normalized, under its live/restored transaction lock.
# This does not change the ordinary startup preference policy or old recoveries.
function Get-KmcF1CreationPreferenceDelta([string]$BeforeJson,[string]$AfterJson) {
 $before=ConvertFrom-Json $BeforeJson;$after=ConvertFrom-Json $AfterJson
 $allowed=@('AutoLevelup_h3494002937','CriticalHits_h3063476040','DamageToParty_h3737204571','DeadCompanionsRiseAfterCombat_h2349052283','DeathDoor_h1819350543',
 'EnemyDifficulty_h2919976620','EternalKingdom_h3591390253','KingdomDifficulty_h4200925179','OnlyInitiatorReceiveSkillCheckExperience_h3577921032',
 'RemoveNegativeEffectsOnRest_h3051215733','StartGameTutorialMesseges_h973415547','StatsAdjustments_h841702840','unity.player_session_count_h922449978','unity.player_sessionid_h1351336811')
 foreach($items in @($before,$after)){$set=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($item in $items){if(-not$set.Add([string]$item.name)){throw 'Duplicate F1 preference.'}}}
 foreach($new in $after){if(@($before|Where-Object name -CEQ $new.name).Count-ne1){throw 'Unobserved preference addition.'}}
 foreach($old in $before){
  $new=@($after|Where-Object name -CEQ $old.name)
  if($new.Count-eq1-and($old|ConvertTo-Json -Compress)-ceq($new[0]|ConvertTo-Json -Compress)){continue}
  if($old.name-cnotin$allowed-or$old.kind-cne'Binary'-or($new.Count-eq1-and$new[0].kind-cne'Binary')-or
     ($new.Count-eq0-and$old.name-cnotin@('OnlyInitiatorReceiveSkillCheckExperience_h3577921032','StartGameTutorialMesseges_h973415547'))){throw 'F1 creation preference delta outside its declared family.'}
  [void][Convert]::FromBase64String([string]$old.value)
  [pscustomobject]@{name=[string]$old.name;before=[string]$old.value;after=$(if($new.Count){[string]$new[0].value}else{$null})}
 }
}
function Restore-KmcF1FinishCreationPreferences($Lock,$Snapshot,[string]$EvidenceRoot) {
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 $request=Read-KmcJson (Join-Path $EvidenceRoot 'runtime-request.json')
 if($request.scenario-cne'finalize-f1-finish'-or$request.runId-cne$Lock.RunId-or$request.transactionToken-cne$Lock.Token-or
    $Snapshot.runId-cne$Lock.RunId-or$Snapshot.token-cne$Lock.Token){throw 'F1 preference restoration has no owned finishing request.'}
 $state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath (Join-Path (Get-KmcLabRoot) 'runtime-state') $Lock.RunId) -Lock $Lock
 if($state.phase-cne'restored'-or-not$state.modsRestored-or-not$state.saveProtectionPassed){throw 'F1 settings restoration requires restored Mods/saves.'}
 $after=Get-KmcPersistencePlayerPrefs
 $changes=@(Get-KmcF1CreationPreferenceDelta $Snapshot.playerPrefsJson $after)
 Write-KmcJsonCreateNewDurable -Path (Join-Path $EvidenceRoot 'f1-creation-preferences-before-restoration.json') -Value ([ordered]@{runId=$Lock.RunId;token=$Lock.Token;intakeSha256=(Get-KmcTextSha256 $Snapshot.playerPrefsJson);afterSha256=(Get-KmcTextSha256 $after);changes=$changes})
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 if((Get-KmcPersistencePlayerPrefs)-cne$after){throw 'Preferences changed after F1 review.'}
 if($changes.Count){
  $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
  try{foreach($change in $changes){$key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)};$key.Flush()}finally{if($null-ne$key){$key.Dispose()}}
 }
 if((Get-KmcPersistencePlayerPrefs)-cne$Snapshot.playerPrefsJson){throw 'F1 preferences differ after own-intake restoration.'}
 Write-KmcJsonCreateNewDurable -Path (Join-Path $EvidenceRoot 'f1-creation-preferences-restored.json') -Value ([ordered]@{runId=$Lock.RunId;preferencesRestoredExactly=$true;restoredCount=$changes.Count;intakeSha256=(Get-KmcTextSha256 $Snapshot.playerPrefsJson)})
}
