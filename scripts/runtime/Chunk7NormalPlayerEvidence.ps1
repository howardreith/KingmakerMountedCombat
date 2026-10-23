Set-StrictMode -Version Latest
# F11 request-free normal-player control. KMC runs with NO automation request in
# the isolated base-game tree beside the external normal-player observer, which
# alone consumes a request. The fixture is the batch185 cold-qualified unmounted
# six-Horse Manual308 through its unchanged source gate; pair 0 is driven and
# pair 1 is the untouched neighbor.
function Test-KmcChunk7NormalPlayerScenario([string]$Scenario){[string]$Scenario-ceq'chunk7-normal-player-control'}
function Get-KmcChunk7NormalPlayerRows {@('chunk7-normal-player-control')}
function Get-KmcChunk7NormalPlayerRoles {
 $roles=@(Get-KmcChunk7SixPairRoles)
 [pscustomobject][ordered]@{riderId=$roles[0];mountId=$roles[1];neighborRiderId=$roles[2];neighborMountId=$roles[3]}
}
# The F12 previous-package load (u1/u3) has only the start, load and quiet checks.
function Get-KmcChunk7NormalPlayerChecks([switch]$PreviousPackageLoad) {
 $at=@('no-KMC-automation-argument-reached-the-process','UMM-runs-exactly-KMC-and-the-observer-both-active',$(if($PreviousPackageLoad){'the-loaded-KMC-assembly-is-the-declared-previous-package'}else{'the-loaded-KMC-assembly-is-the-candidate'}),'the-running-observer-is-the-declared-package',
  'KMC-integration-is-active','no-automation-host-measurement-or-telemetry','no-diagnostic-overlay-in-the-shipped-configuration')
 $presence=@(foreach($label in @('menu','loaded','after')){foreach($name in $at){'at-'+$label+'-'+$name}})
 if($PreviousPackageLoad){return @($presence+@('native-save-and-stash-paths-resolve-inside-the-owned-profile','fixture-archive-bytes-are-exactly-the-declared-copy','engine-reads-the-fixture-header-as-declared',
  'the-fixture-opened-its-own-campaign-and-area-out-of-combat','no-save-file-was-added-or-removed','KMC-logged-no-error-or-exception-in-ordinary-play'))}
 $presence + @(
  'native-save-and-stash-paths-resolve-inside-the-owned-profile','fixture-archive-bytes-are-exactly-the-declared-copy','engine-reads-the-fixture-header-as-declared',
  'the-fixture-opened-its-own-campaign-and-area-out-of-combat','all-four-declared-roles-are-loaded-with-views','each-declared-Horse-is-its-rider''s-own-native-pet',
  'the-fixture-starts-unmounted-and-idle','the-rider-owns-the-ordinary-native-Mount-ability-without-any-grant',
  'native-mount-command-completed-successfully','the-rider-is-attached-to-its-own-Horse','the-neighboring-pair-is-untouched-after-mount','ordinary-Dismount-control-is-available-while-mounted',
  'the-ground-click-moved-the-mounted-pair-together','the-neighboring-pair-is-untouched-after-mounted-move',
  'native-dismount-command-completed-successfully','the-rider-is-detached-after-Dismount','the-neighboring-pair-is-untouched-after-dismount',
  'the-unmounted-rider-moves-under-ordinary-authority','the-neighboring-pair-is-untouched-after-unmounted-move',
  'no-save-file-was-added-or-removed','KMC-logged-no-error-or-exception-in-ordinary-play')
}

# The observer is harness tooling: bound to the clean execution HEAD that built it,
# never to the KMC product commit (which may be an archived frozen candidate).
function Assert-KmcNormalPlayerObserverManifest([string]$ObserverPackagePath){
 $repoRoot=Get-KmcRepositoryRoot;$labRoot=Get-KmcLabRoot
 $package=[IO.Path]::GetFullPath($ObserverPackagePath)
 if(-not$package.StartsWith([IO.Path]::GetFullPath((Join-Path $labRoot 'artifacts')).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Observer package is outside the KMC artifacts root.'}
 $manifestPath=$package+'.manifest.json'
 Assert-KmcNotReparsePoint $package 'observer package';Assert-KmcNotHardLink $package 'observer package'
 Assert-KmcNotReparsePoint $manifestPath 'observer manifest';Assert-KmcNotHardLink $manifestPath 'observer manifest'
 $manifest=Read-KmcJson $manifestPath
 Assert-KmcExactProperties $manifest @('schemaVersion','generator','generatedAtUtc','branch','commit','worktreeClean','qualificationEligible','version','packagePath','packageSha256','dllSha256','dllMvid','entries') 'observer package manifest'
 $head=(& git -C $repoRoot rev-parse HEAD).Trim();$branch=(& git -C $repoRoot branch --show-current).Trim()
 if(@(& git -C $repoRoot status --porcelain --untracked-files=all).Count-ne0){throw 'Observer identity requires a clean execution worktree.'}
 if([int]$manifest.schemaVersion-ne2-or[string]$manifest.generator-cne'scripts/Package-Observer.ps1'-or$manifest.worktreeClean-ne$true-or
  $manifest.qualificationEligible-ne$true-or[string]$manifest.commit-cne$head-or[string]$manifest.branch-cne$branch-or
  [string]$manifest.packagePath-cne$package-or[string]$manifest.packageSha256-cne(Get-KmcSha256 $package)){
  throw 'Observer manifest does not bind the exact clean execution HEAD and observer ZIP.'
 }
 $manifest
}

# The request carries the source gate's own identities and both package identities,
# each bound to its own bytes rather than to one shared commit.
# An F12 previous-package load (u1/u3) loads the reference-free seed and drives no roles.
function New-KmcChunk7NormalPlayerSection($Source,$ObserverPackagePath,$ObserverManifest,$UpgradeRehearsal=$null){
 $roles=Get-KmcChunk7NormalPlayerRoles
 $load=$null-ne$UpgradeRehearsal-and(Test-KmcChunk7UpgradePreviousStep ([string]$UpgradeRehearsal.step))
 $section=[ordered]@{contract='request-free-normal-player-control-v1';sourceRunId=$(if($load){$null}else{[string]$Source.qualificationRunId});archiveProducerRunId=$(if($load){$null}else{[string]$Source.archiveProducerRunId})
  riderId=$(if($load){$null}else{$roles.riderId});mountId=$(if($load){$null}else{$roles.mountId});neighborRiderId=$(if($load){$null}else{$roles.neighborRiderId});neighborMountId=$(if($load){$null}else{$roles.neighborMountId})
  kmcAutomationRequest=$false
  observer=[ordered]@{version=[string]$ObserverManifest.version;commit=[string]$ObserverManifest.commit;packageSha256=(Get-KmcSha256 $ObserverPackagePath)
   manifestSha256=(Get-KmcSha256 ($ObserverPackagePath+'.manifest.json'));dllSha256=[string]$ObserverManifest.dllSha256;dllMvid=[string]$ObserverManifest.dllMvid}}
 if($null-ne$UpgradeRehearsal){$section['upgradeRehearsal']=$UpgradeRehearsal}
 $section
}
function Test-KmcChunk7NormalPlayerPreviousLoad($Request){
 $n=$Request.normalPlayer
 $null-ne$n-and$null-ne$n.PSObject.Properties['upgradeRehearsal']-and(Test-KmcChunk7UpgradePreviousStep ([string]$n.upgradeRehearsal.step))
}
function Assert-KmcChunk7NormalPlayerDescriptor($Request){
 if(-not(Test-KmcChunk7NormalPlayerScenario $Request.scenario)){throw 'Normal-player descriptor belongs only to its own scenario.'}
 Assert-KmcExactProperties $Request @('schemaVersion','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','evidenceRoot',
  'fixture','qualificationSuite','persistenceLoad','normalPlayer') 'normal-player request'
 $n=$Request.normalPlayer;$roles=Get-KmcChunk7NormalPlayerRoles
 $upgrade=$null-ne$n.PSObject.Properties['upgradeRehearsal']
 Assert-KmcExactProperties $n @(@('contract','sourceRunId','archiveProducerRunId','riderId','mountId','neighborRiderId','neighborMountId','kmcAutomationRequest','observer')+@(if($upgrade){'upgradeRehearsal'})) 'normal-player section'
 if($upgrade){Assert-KmcChunk7UpgradeRehearsalSection $Request}
 if(Test-KmcChunk7NormalPlayerPreviousLoad $Request){
  Assert-KmcExactProperties $n.observer @('version','commit','packageSha256','manifestSha256','dllSha256','dllMvid') 'normal-player observer identity'
  $load=$Request.persistenceLoad
  Assert-KmcExactProperties $load @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'previous-package fixture descriptor'
  if([string]$n.contract-cne'request-free-normal-player-control-v1'-or$n.kmcAutomationRequest-isnot[bool]-or$n.kmcAutomationRequest-or
   $null-ne$n.sourceRunId-or$null-ne$n.archiveProducerRunId-or$null-ne$n.riderId-or$null-ne$n.mountId-or$null-ne$n.neighborRiderId-or$null-ne$n.neighborMountId-or
   [string]$n.observer.version-cnotmatch'^0\.1\.0-observer\.[0-9]+$'-or[string]$n.observer.commit-cnotmatch'^[0-9a-f]{40}$'-or[string]$n.observer.packageSha256-cnotmatch'^[0-9a-f]{64}$'-or
   [string]$load.internalName-cne'KMC C7 NO MODS OWNER SEED'-or[string]$load.fileName-cne'Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks'-or
   [string]$load.sha256-cne'3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'-or[long]$load.length-ne1239025-or
   [string]$load.gameId-cne'3f7381f7-aba6-43bf-bae6-39c6a66a7180'-or[string]$load.gameName-cne'Testy the Modless'-or[string]$load.area-cne'b10e86516b6437d45a629931c30936fe'-or[long]$load.lastWriteTimeUtcTicks-lt1){
   throw 'Previous-package load request differs from the closed reference-free owner-seed load.'
  }
  return
 }
 Assert-KmcExactProperties $n.observer @('version','commit','packageSha256','manifestSha256','dllSha256','dllMvid') 'normal-player observer identity'
 $load=$Request.persistenceLoad
 Assert-KmcExactProperties $load @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'normal-player fixture descriptor'
 if([string]$n.contract-cne'request-free-normal-player-control-v1'-or$n.kmcAutomationRequest-isnot[bool]-or$n.kmcAutomationRequest-or
  [string]$n.sourceRunId-cne'20261022-c7b-world185-b185s1-sixthhorse-cold'-or[string]$n.archiveProducerRunId-cne'20261022-c7b-world184-b184s1-sixthhorse-manual'-or
  [string]$n.riderId-cne$roles.riderId-or[string]$n.mountId-cne$roles.mountId-or[string]$n.neighborRiderId-cne$roles.neighborRiderId-or[string]$n.neighborMountId-cne$roles.neighborMountId-or
  [string]$n.observer.version-cnotmatch'^0\.1\.0-observer\.[0-9]+$'-or[string]$n.observer.commit-cnotmatch'^[0-9a-f]{40}$'-or
  [string]$n.observer.packageSha256-cnotmatch'^[0-9a-f]{64}$'-or[string]$n.observer.manifestSha256-cnotmatch'^[0-9a-f]{64}$'-or
  [string]$n.observer.dllSha256-cnotmatch'^[0-9a-f]{64}$'-or[string]$n.observer.dllMvid-cnotmatch'^[0-9a-fA-F-]{36}$'-or
  [string]$load.internalName-cne'KMC_C7_SIX_HORSE_WORLD'-or[string]$load.fileName-cne'Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'-or
  [string]$load.sha256-cne'b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'-or[long]$load.length-ne1332308-or
  [string]$load.gameId-cne'3f7381f7-aba6-43bf-bae6-39c6a66a7180'-or[string]$load.gameName-cne'Testy the Modless'-or[long]$load.lastWriteTimeUtcTicks-ne639282712706531806-or[string]$load.area-cne'b10e86516b6437d45a629931c30936fe'){
  throw 'Normal-player request differs from the closed request-free control on the qualified six-Horse fixture.'
 }
}

# What the observer process is bound to. KMC itself receives nothing.
function New-KmcChunk7NormalPlayerObserverRequest($Request,[string]$Token,[string]$EvidenceRoot,[string]$ProfileRoot,[string]$ModsRoot,$Manifest){
 $n=$Request.normalPlayer;$load=Test-KmcChunk7NormalPlayerPreviousLoad $Request
 # The observer checks the loaded KMC assembly against the package actually staged.
 $staged=if($null-ne$n.PSObject.Properties['upgradeRehearsal']){$n.upgradeRehearsal.stagedKmc}else{$null}
 $candidate=if($null-ne$staged){[ordered]@{commit=[string]$staged.commit;productVersion=[string]$staged.version;dllSha256=[string]$staged.dllSha256;dllMvid=[string]$staged.dllMvid}}
  else{[ordered]@{commit=[string]$Manifest.commit;productVersion=[string]$Manifest.version;dllSha256=[string]$Manifest.dllSha256;dllMvid=[string]$Manifest.dllMvid}}
 [ordered]@{schemaVersion=3;runId=[string]$Request.runId;transactionToken=$Token;evidenceRoot=$EvidenceRoot;profileRoot=$ProfileRoot;modsRoot=$ModsRoot
  kmcModId='KingmakerMountedCombat';observerModId='KmcRemovalObserver'
  kmcHarmonyIds=@('KingmakerMountedCombat.Feasibility','KingmakerMountedCombat.PersistenceIsolation','KingmakerMountedCombat.Diagnostics.QueuedMountWindow','KingmakerMountedCombat.Diagnostics.ActorAllocation','KingmakerMountedCombat.Diagnostics.OrdinaryAttackTrace')
  archive=$Request.persistenceLoad
  kmcBlueprintGuids=@('f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e')
  mammothBlueprintGuid='e7aa96d15a45238438ae4cfb476f6bb9'
  candidate=$candidate
  observer=[ordered]@{version=[string]$n.observer.version;packageSha256=[string]$n.observer.packageSha256;dllSha256=[string]$n.observer.dllSha256;dllMvid=[string]$n.observer.dllMvid}
  timeoutSeconds=600;chargeSource=$null
  normalPlayer=[ordered]@{riderId=$(if($load){$null}else{[string]$n.riderId});mountId=$(if($load){$null}else{[string]$n.mountId});neighborRiderId=$(if($load){$null}else{[string]$n.neighborRiderId});neighborMountId=$(if($load){$null}else{[string]$n.neighborMountId})
   mountAbilityGuid='f053faad986631688defa003cd7bda0e';dismountAbilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';kmcPackageSha256=$(if($null-ne$staged){[string]$staged.packageSha256}else{[string]$Manifest.packageSha256});idleWindowFrames=240
   mode=$(if($load){'previous-package-load'}else{'controls'})}}
}

# Closed reader of the observer's own result. Every verdict is the observer's
# check, re-read and cross-checked against the launcher's request records.
function Assert-KmcChunk7NormalPlayerObserverResult($Request,$ObserverRequest,$Result,[string]$ObserverRequestSha256,[int]$ExpectedProcessId,[DateTimeOffset]$NotBeforeUtc){
 Assert-KmcExactProperties $Result @('schemaVersion','evidenceKind','runId','transactionToken','processId','startedAtUtc','completedAtUtc','status',
  'observerVersion','observerAssembly','requestSha256','stage','checks','checkPassCount','checkFailCount','errors','observations') 'normal-player observer result'
 $started=[DateTimeOffset]::MinValue;$completed=[DateTimeOffset]::MinValue
 if([int]$Result.schemaVersion-ne1-or[string]$Result.evidenceKind-cne'kmc-normal-player-observer'-or[string]$Result.runId-cne[string]$Request.runId-or
  [string]$Result.transactionToken-cne[string]$Request.transactionToken-or[int]$Result.processId-ne$ExpectedProcessId-or
  -not[DateTimeOffset]::TryParse([string]$Result.startedAtUtc,[ref]$started)-or-not[DateTimeOffset]::TryParse([string]$Result.completedAtUtc,[ref]$completed)-or
  $started-lt$NotBeforeUtc.AddSeconds(-5)-or$completed-lt$started-or[string]$Result.observerAssembly-cne'KmcRemovalObserver'-or
  [string]$Result.observerVersion-cne[string]$ObserverRequest.observer.version-or[string]$Result.requestSha256-cne$ObserverRequestSha256-or
  [string]$Result.status-cnotin@('PASS','FAIL')){throw ('Normal-player observer result does not bind this run, process, token and observer request; its first error: '+[string](@($Result.errors)+@('<none>'))[0])}
 $rows=@($Result.checks);foreach($row in $rows){Assert-KmcExactProperties $row @('name','passed') 'normal-player check';if($row.passed-isnot[bool]){throw 'Normal-player check verdict is not boolean.'}}
 $passed=@($rows|Where-Object{$_.passed});$failed=@($rows|Where-Object{-not$_.passed})
 if([int]$Result.checkPassCount-ne$passed.Count-or[int]$Result.checkFailCount-ne$failed.Count-or
  (([string]$Result.status-ceq'PASS')-ne($failed.Count-eq0-and@($Result.errors).Count-eq0))){throw 'Normal-player status does not follow its own checks and errors.'}
 if([string]$Result.status-cne'PASS'){return}
 $load=Test-KmcChunk7NormalPlayerPreviousLoad $Request
 $staged=if($null-ne$Request.normalPlayer.PSObject.Properties['upgradeRehearsal']){$Request.normalPlayer.upgradeRehearsal.stagedKmc}else{$null}
 $expectedDll=if($null-ne$staged){[string]$staged.dllSha256}else{[string]$Request.dllSha256};$expectedMvid=if($null-ne$staged){[string]$staged.dllMvid}else{[string]$Request.dllMvid}
 # Observer requests before observer.6 carried no mode; they are the controls mode.
 $observedMode=if($null-ne$ObserverRequest.normalPlayer.PSObject.Properties['mode']){[string]$ObserverRequest.normalPlayer.mode}elseif($null-eq$staged){'controls'}else{'<missing>'}
 if([string]$ObserverRequest.candidate.dllSha256-cne$expectedDll-or[string]$ObserverRequest.candidate.dllMvid-cne$expectedMvid-or
  $observedMode-cne$(if($load){'previous-package-load'}else{'controls'})){throw 'Observer request does not bind the staged KMC package and mode.'}
 $names=@($passed|ForEach-Object{[string]$_.name});$required=@(Get-KmcChunk7NormalPlayerChecks -PreviousPackageLoad:$load)
 if(($names|Sort-Object)-join'|'-cne(($required|Sort-Object)-join'|')){throw 'Normal-player PASS check set differs from its exact declared set.'}
 $o=$Result.observations;$n=$Request.normalPlayer
 if($load){Assert-KmcExactProperties $o @('routing','presenceAtMenu','profileBefore','world','presenceLoaded','frameWindows','inputs','presenceAfter','archive','profileAfter','log') 'previous-package observations'}
 else{Assert-KmcExactProperties $o @('routing','presenceAtMenu','profileBefore','world','controlsBefore','presenceLoaded','frameWindows','inputs','controlsAfter','presenceAfter','archive','profileAfter','log') 'normal-player observations'}
 $profileRoot=[IO.Path]::GetFullPath([string]$ObserverRequest.profileRoot).TrimEnd('\')
 if([string]$o.routing.observedSavePath-cne(Join-Path $profileRoot 'Saved Games')-or[string]$o.routing.observedStashFolder-cne(Join-Path $profileRoot 'Areas')){throw 'Native save routing did not resolve inside the owned profile.'}
 foreach($p in @($o.presenceAtMenu,$o.presenceLoaded,$o.presenceAfter)){
  if(@($p.kmcRuntimeArguments).Count-ne0-or((@($p.modEntries)|Sort-Object)-join'|')-cne'KingmakerMountedCombat:active|KmcRemovalObserver:active'-or
   [int]$p.kmcAssemblies-ne1-or[string]$p.kmcDllSha256-cne$expectedDll-or[string]$p.kmcMvid-cne$expectedMvid-or
   @($p.kmcHarmonyOwners).Count-eq0-or$p.rootPresent-ne$true-or[string]$p.automationHostField-cne'null'-or[string]$p.staticAutomationHost-cne'null'-or
   [string]$p.observerDllSha256-cne[string]$n.observer.dllSha256-or[string]$p.observerMvid-cne[string]$n.observer.dllMvid-or
   [string]$p.measurementSession-cne'null'-or[string]$p.movementTelemetry-cne'null'-or[int]$p.overlayObjects-ne0-or$p.effectiveSettings.EnableDiagnosticOverlay-ne$false){
   throw 'KMC was not present on its ordinary no-request path with the shipped quiet configuration.'
  }
 }
 if($load){
  if(@($o.inputs).Count-ne0-or@($o.frameWindows).Count-ne1-or[string]@($o.frameWindows)[0].label-cne'idle-loaded'){throw 'A previous-package load issued input or lacks its one idle window.'}
  if($o.world.inCombat-ne$false-or[string]$o.world.area-cne[string]$Request.persistenceLoad.area-or[string]$o.world.gameId-cne[string]$Request.persistenceLoad.gameId){throw 'The previous-package load did not open the seed campaign and area out of combat.'}
  if([string]$o.archive.sha256Before-cne[string]$Request.persistenceLoad.sha256-or[int]$o.log.kmcErrors-ne0){throw 'The seed changed or the previous package logged an error.'}
  $seedCopy=Join-Path ([string]$o.routing.observedSavePath) ([string]$Request.persistenceLoad.fileName)
  if([string]$o.archive.path-cne$seedCopy){throw 'The observed archive is not the owned seed copy.'}
  return
 }
 $inputs=@($o.inputs);$labels=@($inputs|ForEach-Object{[string]$_.label})
 if(($labels-join'|')-cne'mount|mounted-move|dismount|unmounted-move'){throw 'Normal-player inputs are not the exact ordered native sequence.'}
 foreach($i in @($inputs[0],$inputs[2])){
  if([string]$i.casterId-cne[string]$n.riderId-or$i.clicked-ne$true-or$i.commandCreated-ne$true-or[string]$i.result-cne'Success'-or$i.neighborUnchanged-ne$true){throw 'A native ability input was rejected, unresolved or touched the neighbor.'}
 }
 if([string]$inputs[0].targetId-cne[string]$n.mountId-or[string]$inputs[0].attachedTo-cne[string]$n.mountId-or[double]$inputs[0].riderMountDistance-ge1.0){throw 'Native Mount did not attach the rider to its own Horse.'}
 if([string]$inputs[2].targetId-cne[string]$n.riderId-or$null-ne$inputs[2].attachedTo){throw 'Native Dismount did not detach the rider.'}
 if([string]$inputs[1].moverId-cne[string]$n.riderId-or[double]$inputs[1].riderDisplacement-lt1.5-or[double]$inputs[1].mountDisplacement-lt1.5-or
  [string]$inputs[1].attachedTo-cne[string]$n.mountId-or$inputs[1].neighborUnchanged-ne$true){throw 'The mounted ground click did not move the exact pair together.'}
 if([string]$inputs[3].moverId-cne[string]$n.riderId-or[double]$inputs[3].riderDisplacement-lt1.5-or$null-ne$inputs[3].attachedTo-or$inputs[3].neighborUnchanged-ne$true){throw 'Unmounted movement authority was not restored.'}
 if(@($o.frameWindows).Count-ne2-or((@($o.frameWindows)|ForEach-Object{[string]$_.label})-join'|')-cne'idle-unmounted|idle-mounted'){throw 'Idle frame windows are incomplete.'}
 if([string]$o.archive.sha256Before-cne[string]$Request.persistenceLoad.sha256-or[int]$o.log.kmcErrors-ne0){throw 'The fixture changed or KMC logged an error.'}
 $copy=Join-Path ([string]$o.routing.observedSavePath) ([string]$Request.persistenceLoad.fileName)
 if([string]$o.archive.path-cne$copy){throw 'The observed archive is not the owned fixture copy.'}
}

# KMC logs through UMM's own logger, which never reaches Unity's log. After the
# attributed process exits, its one-session UMM log is KMC's own record of how it
# started: the ordinary load line, the quiet overlay, no accepted automation
# request and no error or exception line.
function Get-KmcChunk7NormalPlayerUmmLogSummary([string]$GameRoot,[DateTimeOffset]$NotBeforeUtc,[string]$ProductVersion){
 $path=Join-Path $GameRoot 'Kingmaker_Data/Managed/UnityModManager/Log.txt'
 Assert-KmcNotReparsePoint $path 'UMM session log'
 $item=Get-Item -LiteralPath $path
 if($item.LastWriteTimeUtc-lt$NotBeforeUtc.UtcDateTime){throw 'The UMM log was not written by this run''s session.'}
 $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
 try{$reader=New-Object IO.StreamReader($stream,[Text.UTF8Encoding]::new($false));$lines=@($reader.ReadToEnd()-split"\r?\n")}finally{$stream.Dispose()}
 $kmc=@($lines|Where-Object{$_.StartsWith('[KingmakerMountedCombat] ',[StringComparison]::Ordinal)})
 $summary=[ordered]@{schemaVersion=1;path=$path;sha256=(Get-KmcSha256 $path);sessionHeaders=@($lines|Where-Object{$_.StartsWith('[Manager] Version: ',[StringComparison]::Ordinal)}).Count
  kmcLines=$kmc.Count;loadedLine=@($kmc|Where-Object{$_.Contains('Kingmaker Mounted Combat '+$ProductVersion+' loaded')}).Count
  quietOverlayLine=@($kmc|Where-Object{$_.Contains('diagnostic overlay=False')}).Count
  automationAccepted=@($kmc|Where-Object{$_.Contains('Runtime automation request accepted')}).Count
  errorLines=@($kmc|Where-Object{$_.StartsWith('[KingmakerMountedCombat] [Error]',[StringComparison]::Ordinal)-or$_.StartsWith('[KingmakerMountedCombat] [Exception]',[StringComparison]::Ordinal)-or$_.Contains('Exception:')}).Count
  firstKmcLines=@($kmc|Select-Object -First 12)}
 $summary['passed']=($summary.sessionHeaders-eq1-and$summary.loadedLine-eq1-and$summary.quietOverlayLine-ge1-and$summary.automationAccepted-eq0-and$summary.errorLines-eq0)
 [pscustomobject]$summary
}

# The engine's own load rewrites only the archive's header member. Every other
# native member of the owned copy must equal the qualified source byte for byte.
function Assert-KmcChunk7NormalPlayerArchiveMembers([string]$SourcePath,[string]$CopyPath){
 $before=Get-KmcValidationMemberHashes $SourcePath;$after=Get-KmcValidationMemberHashes $CopyPath
 $changed=@($before.Keys|Where-Object{$after.ContainsKey($_)-and$after[$_]-cne$before[$_]})
 $missing=@($before.Keys|Where-Object{-not$after.ContainsKey($_)});$added=@($after.Keys|Where-Object{-not$before.ContainsKey($_)})
 if($missing.Count-ne0-or$added.Count-ne0-or@($changed|Where-Object{$_-cne'header.json'}).Count-ne0){
  throw ('The normal-player load changed a native fixture member: changed='+($changed-join',')+' missing='+($missing-join',')+' added='+($added-join','))
 }
 [pscustomobject][ordered]@{changed=@($changed);missing=@();added=@()}
}

# Outer binding: the composed game record names exactly this observer result.
function Assert-KmcChunk7NormalPlayerRuntimeBinding($Request,$GameEvidence,[string]$Status){
 if(-not(Test-KmcChunk7NormalPlayerScenario $Request.scenario)){return}
 $root=[IO.Path]::GetFullPath([string]$Request.evidenceRoot)
 $resultPath=Join-Path $root 'observer-result.json'
 if($null-eq$GameEvidence-or[string]$GameEvidence.evidenceKind-cne'kmc-normal-player-observer'-or-not(Test-Path -LiteralPath $resultPath -PathType Leaf)-or
  (Get-KmcSha256 $resultPath)-cne[string]$GameEvidence.observerResultSha256-or[string]$GameEvidence.status-cne$Status-or$GameEvidence.kmcAutomationRequest-ne$false){
  throw ('Normal-player run result does not bind its exact observer result (observer result present='+(Test-Path -LiteralPath $resultPath -PathType Leaf)+').')
 }
}
