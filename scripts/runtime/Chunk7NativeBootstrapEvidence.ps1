Set-StrictMode -Version Latest
# RetainedObjectIdentity labels are RuntimeHelpers hash values; capacity bounds their count.
function Assert-KmcNativeBootstrapObject($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'Native bootstrap object identity is zero.'}}
function Test-KmcChunk7NativeBootstrapScenario([string]$Scenario){$Scenario-ceq'chunk7-native-main-bootstrap'}
function Get-KmcChunk7NativeBootstrapRows {@('chunk7-native-main-bootstrap','C7-bootstrap-preflight','C7-bootstrap-native-start','C7-bootstrap-native-world','C7-bootstrap-autosave','C7-bootstrap-cleanup','C7-bootstrap-runtime','C7-bootstrap-interrupted')}
function Assert-KmcChunk7NativeBootstrapSelection($Request){
 $has=$null-ne$Request.PSObject.Properties['nativeBootstrap']
 if(-not(Test-KmcChunk7NativeBootstrapScenario $Request.scenario)){if($has){throw 'Native bootstrap selection is forbidden under another scenario.'};return}
 if(-not$has){throw 'Native bootstrap requires an explicit selection.'};$s=$Request.nativeBootstrap
 Assert-KmcExactProperties $s @('case','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount','nativeWriteLeaf') 'native bootstrap selection'
 if($s.case-isnot[string]-or$s.case-cne'authored-main-campaign'-or$s.nativeWriteLeaf-isnot[string]-or$s.nativeWriteLeaf-cne'Auto_1.zks'){throw 'Native bootstrap selection differs from its closed authored-main case.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true
 Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 0 0;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 1 1
}
function Assert-KmcChunk7NativeBootstrapAuthority($Row,[string]$Root,[long]$Generation,[long]$Writes){
 if($Row.root-isnot[string]-or$Row.root-cne$Root){throw 'Native bootstrap isolated root changed.'}
 Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
 if($Row.generation-ne$Generation){throw 'Native bootstrap authority generation changed.'}
 foreach($key in @('maximumNativeLoads','admittedNativeLoads','authorizedLoads','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
 Assert-KmcOrdinaryInteger $Row.maximumNativeWrites 1 1
 foreach($key in @('admittedNativeWrites','authorizedWrites','freezeCount')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
 Assert-KmcChunk7Boolean $Row.windowOpen $false
 if($Writes-eq0){if($null-ne$Row.frozenGameId-or$null-ne$Row.frozenGameName){throw 'Native bootstrap identity was frozen before native new game.'}}
 else {Assert-KmcOrdinaryGuid $Row.frozenGameId;if($Row.frozenGameName-isnot[string]-or[string]::IsNullOrWhiteSpace($Row.frozenGameName)){throw 'Native campaign name missing.'}}
}
function Assert-KmcChunk7NativeBootstrapPreflight($Value,[bool]$Passed){
 foreach($key in @('horseState','rangerSelectionGuid')){if($Value.$key-isnot[string]){throw 'Bootstrap preflight exact string missing.'}}
 if($Value.rangerSelectionGuid-cne'ee63330662126374e8785cc901941ac7'){throw 'Bootstrap observed a foreign Ranger selection.'}
 foreach($key in @('libraryReady','rangerSelectionPresent','loadedAreaPresent','mainActorPresent','nativeNewGameDispatched')){if($Value.$key-isnot[bool]){throw 'Bootstrap preflight flag must be boolean.'}}
 Assert-KmcOrdinaryInteger $Value.rangerAllFeaturesCount -1 ([int]::MaxValue)
 if($Value.rangerFirst32Guids-isnot[Array]-or$Value.rangerFirst32Guids.Count-ne[Math]::Min(32,[Math]::Max(0,$Value.rangerAllFeaturesCount))){throw 'Bounded native Ranger list differs from its actual count.'}
 foreach($guid in $Value.rangerFirst32Guids){if($null-ne$guid-and($guid-isnot[string]-or$guid-cnotmatch'^[a-f0-9]{32}$')){throw 'Native Ranger option identity is malformed.'}}
 foreach($key in @('authorizedLoads','authorizedWrites')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
 if($Passed){
  foreach($key in @('horseFeatureGuid','horseUnitGuid')){if($Value.$key-isnot[string]){throw 'Registered Horse blueprint identity must be a string.'}}
  if($Value.horseState-cne'Registered'-or$null-ne$Value.horseFailure-or$Value.horseFeatureGuid-cne'7db7c50677e39f09feef56f3831fc723'-or$Value.horseUnitGuid-cne'4016c7db400ab721ff125aef9e65e202'){throw 'Bootstrap did not pass exact Horse registration before new game.'}
  Assert-KmcChunk7Boolean $Value.libraryReady $true;Assert-KmcChunk7Boolean $Value.rangerSelectionPresent $true
  foreach($key in @('loadedAreaPresent','mainActorPresent','nativeNewGameDispatched')){Assert-KmcChunk7Boolean $Value.$key $false}
  # This family is base-game-only. The historical seven-entry mod-adjusted
  # selection is valid for the registrar, but cannot prove this native contract.
  $nativePrefix=@('472091361cf118049a2b4339c4ea836a','f6f1cdcc404f10c4493dc1e51208fd6f',
   'afb817d80b843cc4fa7b12289e6ebe3d','f9ef7717531f5914a9b6ecacfad63f46',
   'f894e003d31461f48a02f5caec4e3359','e992949eba096644784592dc7f51a5c7',
   'aa92fea676be33d4dafd176d699d7996','2ee2ba60850dd064e8b98bf5c2c946ba',
   '6adc3aab7cde56b40aa189a797254271','ece6bde3dfc76ba4791376428e70621a',
   '126712ef923ab204983d6f107629c895','67a9dc42b15d0954ca4689b13e8dedea')
  Assert-KmcOrdinaryInteger $Value.rangerAllFeaturesCount 13 13
  for($index=0;$index-lt$nativePrefix.Count;$index++){
   if($Value.rangerFirst32Guids[$index]-cne$nativePrefix[$index]){throw 'Base-only Ranger option prefix changed.'}
  }
  if($Value.rangerFirst32Guids[12]-cne$Value.horseFeatureGuid){throw 'Registered Horse did not occupy its one appended native Ranger option.'}
 }
}
function Assert-KmcChunk7NativeBootstrapEvidence($Request,$Artifact,[string]$Status='PASS'){
 Assert-KmcChunk7NativeBootstrapSelection $Request
 Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-native-bootstrap-evidence'
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS bootstrap requires a PASS native artifact.'}
 $o=$Artifact.observations
 if($null-ne$o.PSObject.Properties['preflight']){Assert-KmcChunk7NativeBootstrapPreflight $o.preflight ($Artifact.status-ceq'PASS')}
 if($Artifact.status-cne'PASS'){return}
 if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $Request.nativeBootstrap)){throw 'Native bootstrap selection changed.'}
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
 foreach($key in @('workingDeserialized','mountedCombatProven','ordinaryAcquisitionProven','smallPoseQualified','diagnosticArchiveContentRead')){Assert-KmcChunk7Boolean $o.$key $false}
 foreach($key in @('nativeSourceMetadataEnumerationSelected','nativeSourceMetadataEnumerationCompleted')){Assert-KmcChunk7Boolean $o.$key $true}
 Assert-KmcChunk7NativeBootstrapPreflight $o.preflight $true
 $p=$o.preset
 if($p.source-isnot[string]-or$p.source-cne'BlueprintRoot.NewGamePreset'){throw 'Native preset is not the authored main-campaign root.'}
 foreach($key in @('guid','area','enterPoint','enterPointArea','playerBlueprint')){if($p.$key-isnot[string]-or$p.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Authored native preset identity is invalid.'}}
 if($p.area-cne$p.enterPointArea){throw 'Authored preset and enter-point areas differ.'}
 Assert-KmcChunk7Boolean $p.makeAutosave $true
 foreach($key in @('charGen','usesPresetPlayer')){if($p.$key-isnot[bool]){throw 'Native preset flag must be boolean.'}}
 Assert-KmcOrdinaryInteger $p.companionCount 0 128
 $b=$o.before;$w=$o.nativeWorld;$a=$w.authority
 foreach($key in @('gameId','gameName','mainActorId','area','mainBlueprint','mode')){if($w.$key-isnot[string]){throw 'Native world identity must be exact scalar strings.'}}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 Assert-KmcChunk7NativeBootstrapAuthority $b.authority $root $b.authority.generation 0
 Assert-KmcChunk7NativeBootstrapAuthority $a $root $b.authority.generation 1
 foreach($row in @($b,$w)){
  Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
  foreach($key in @('game','player','saveManager')){Assert-KmcNativeBootstrapObject $row.$key}
  foreach($key in @('owners','slots','casts','semanticRestores','presentationRestores','nativePostLoads')){Assert-KmcOrdinaryInteger $row.$key 0 0}
  Assert-KmcChunk7Boolean $row.nativeLoading $false
 }
 foreach($key in @('game','player','saveManager')){if($b.$key-ne$w.$key){throw 'Native bootstrap replaced its admitted Game, Player or SaveManager.'}}
 foreach($key in @('mainActor','mainView','snapshots','completedSaves')){Assert-KmcOrdinaryInteger $b.$key 0 0}
 if($null-ne$b.mainActorId-or$null-ne$b.area-or$null-ne$b.gameName){throw 'Native bootstrap did not begin at the empty menu.'}
 if($b.party-isnot[Array]-or$b.party.Count-ne0){throw 'Native bootstrap inherited a prior party.'}
 foreach($key in @('mainActor','mainView')){Assert-KmcNativeBootstrapObject $w.$key}
 foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $w.$key 1 1}
 Assert-KmcOrdinaryGuid $w.mainActorId;Assert-KmcOrdinaryGuid $w.gameId
 if($w.gameId-ceq$Request.fixture.working.gameId-or$w.gameId-cne$a.frozenGameId-or$w.gameName-isnot[string]-or$w.gameName-cne$a.frozenGameName-or
    $w.area-cne$p.area-or$w.mainBlueprint-cne$p.playerBlueprint-or$w.mode-cne'Default'-or$w.frame-lt$b.frame){throw 'Native main world differs from its observed mint or authored preset.'}
 Assert-KmcChunk7Boolean $w.cutscene $false
 if($w.party-isnot[Array]-or$w.party.Count-lt1-or$w.party.Count-gt128){throw 'Native main-world party inventory invalid.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($unit in $w.party){Assert-KmcOrdinaryGuid $unit.id;if(-not$ids.Add($unit.id)){throw 'Native party duplicates an actor.'};foreach($key in @('object','descriptor','view')){Assert-KmcNativeBootstrapObject $unit.$key};if($unit.blueprint-isnot[string]-or$unit.blueprint-cnotmatch'^[a-f0-9]{32}$'){throw 'Native party blueprint invalid.'}}
 $main=@($w.party|Where-Object id -CEQ $w.mainActorId)
 if($main.Count-ne1-or$main[0].object-ne$w.mainActor-or$main[0].view-ne$w.mainView-or$main[0].blueprint-cne$p.playerBlueprint){throw 'Native main actor does not bind its party/view entry.'}
 $menu=$o.menuAdmission
 Assert-KmcOrdinaryInteger $menu.frame $b.frame $w.frame;Assert-KmcNativeBootstrapObject $menu.menu
 foreach($key in @('pcController','mainMenuSceneLoaded','loadingScreenPresent')){Assert-KmcChunk7Boolean $menu.$key $true}
 foreach($key in @('nativeBusy','unexpectedDialog','alreadyEntering')){Assert-KmcChunk7Boolean $menu.$key $false}
 $trace=$o.probe
 foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $trace.$key $true}
 if($trace.errors-isnot[Array]-or$trace.errors.Count-ne0){throw 'Native bootstrap observer failed.'}
 foreach($key in @('entered','exited','minted','saveRequests','prepared')){Assert-KmcOrdinaryInteger $trace.$key 1 1}
  $tokens=@('06000CDC','06008029','06008025');$types=@('Kingmaker.Game','Kingmaker.EntitySystem.Persistence.SaveManager','Kingmaker.EntitySystem.Persistence.SaveManager');$methods=@('LoadNewGame','SaveRoutine','PrepareSave')
  if($trace.hooks-isnot[Array]-or$trace.hooks.Count-ne3){throw 'Native bootstrap exact hook set differs.'}
  for($i=0;$i-lt3;$i++){$h=$trace.hooks[$i];foreach($key in @('type','method','token','moduleMvid')){if($h.$key-isnot[string]){throw 'Native hook metadata must be scalar strings.'}};if($h.token-cne$tokens[$i]-or$h.type-cne$types[$i]-or$h.method-cne$methods[$i]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native bootstrap installed hook contract differs.'};if($i-eq0){if($h.transpiler-isnot[string]-or$h.transpiler-cne'IdentityCallsite'){throw 'Native bootstrap did not observe the exact identity callsite.'}}elseif($null-ne$h.transpiler){throw 'Unexpected native bootstrap transpiler.'}}
  $site=$trace.identityCallsite
  if($null-eq$site-or$site-is[Array]-or$site-isnot[pscustomobject]){throw 'Native bootstrap exact identity callsite evidence is absent.'}
  foreach($key in @('contract','ownerToken','setterToken','stateFieldToken','playerFieldToken','moduleMvid')){if($site.$key-isnot[string]){throw 'Native bootstrap identity callsite metadata must be scalar strings.'}}
  if($site.contract-cne'load-new-game-guid-setter-callsite-v1'-or$site.ownerToken-cne'06000CDC'-or$site.setterToken-cne'06000DE3'-or$site.stateFieldToken-cne'040006B6'-or$site.playerFieldToken-cne'0400533C'-or$site.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native bootstrap identity callsite contract changed.'}
  Assert-KmcOrdinaryInteger $site.setterIlOffset 302 302;Assert-KmcOrdinaryInteger $site.matchedSites 1 1
  if($site.installed-isnot[bool]-or-not$site.installed-or$site.nativeSetterPreserved-isnot[bool]-or-not$site.nativeSetterPreserved-or$site.setterPostfixUsed-isnot[bool]-or$site.setterPostfixUsed){throw 'Native bootstrap assignment was not observed at the preserved native callsite.'}
 $registry=$trace.identities
 if($registry.contract-isnot[string]-or$registry.contract-cne'retained-native-object-identity-v1'){throw 'Native bootstrap identity registry contract differs.'}
 Assert-KmcOrdinaryInteger $registry.capacity 128 128;Assert-KmcOrdinaryInteger $registry.faults 0 0;Assert-KmcChunk7Boolean $registry.released $false
 if($registry.ids-isnot[Array]-or$registry.ids.Count-lt1-or$registry.ids.Count-gt128){throw 'Native bootstrap retained identity inventory invalid.'}
 Assert-KmcOrdinaryInteger $registry.retainedCount $registry.ids.Count $registry.ids.Count
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $registry.ids){Assert-KmcNativeBootstrapObject $id;if(-not$labels.Add($id)){throw 'Native object identity is duplicated.'}}
 if(-not$labels.Contains([int]$menu.menu)){throw 'Native menu ownership label was not retained.'}
 foreach($row in @($b,$w)){foreach($key in @('game','player','saveManager','mainActor','mainView')){if($row.$key-ne0-and-not$labels.Contains([int]$row.$key)){throw 'Snapshot object was not retained.'}}}
 foreach($unit in $w.party){foreach($key in @('object','descriptor','view')){if(-not$labels.Contains([int]$unit.$key)){throw 'Native party label is outside retained registry.'}}}
 $kinds=@('native-new-game-enter','native-campaign-minted','native-bootstrap-autosave-request','native-new-game-exit','native-bootstrap-autosave-prepared')
 if($trace.events-isnot[Array]-or$trace.events.Count-ne5){throw 'Native bootstrap event count differs.'}
 $prior=$b.frame;$saveLabel=0
 for($i=0;$i-lt5;$i++){
  $e=$trace.events[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $w.frame;$prior=$e.frame
  if($e.kind-isnot[string]-or$e.kind-cne$kinds[$i]-or$e.presetGuid-isnot[string]-or$e.presetGuid-cne$p.guid){throw 'Native bootstrap event sequence or preset differs.'}
  foreach($key in @('game','player','menuPlayer','saveManager','preset')){Assert-KmcNativeBootstrapObject $e.$key;if(-not$labels.Contains([int]$e.$key)){throw 'Native event identity was not retained.'}}
  if($e.game-ne$w.game-or$e.player-ne$w.player-or$e.menuPlayer-ne$w.player-or$e.saveManager-ne$w.saveManager-or($i-gt0-and$e.preset-ne$trace.events[0].preset)){throw 'Native callback owner changed.'}
  Assert-KmcChunk7Boolean $e.sameMenuPlayer $true;Assert-KmcChunk7Boolean $e.insideNativeNewGame ($i-lt4)
  if($i-gt0-and($e.gameId-isnot[string]-or$e.mainActorId-isnot[string]-or$e.gameId-cne$w.gameId-or$e.mainActorId-cne$w.mainActorId)){throw 'Native minted campaign/main actor changed between callbacks.'}
  if($i-eq2-or$i-eq4){Assert-KmcNativeBootstrapObject $e.save;if(-not$labels.Contains([int]$e.save)-or$e.saveType-isnot[string]-or$e.saveType-cne'Auto'){throw 'Native autosave descriptor is not exact.'};Assert-KmcChunk7Boolean $e.autoLevelup $false;if($i-eq2){$saveLabel=$e.save}else{if($e.save-eq$saveLabel){throw 'PrepareSave did not receive the distinct native descriptor copy.'}}}
 }
 $d=$o.nativeWrite;$prepared=$trace.events[4];$requested=$trace.events[2]
 Assert-KmcNativeBootstrapObject $prepared.requestedSave;Assert-KmcNativeBootstrapObject $prepared.observedSaveManager
 Assert-KmcChunk7Boolean $prepared.accepted $true
 if($prepared.requestedSave-ne$requested.save-or$prepared.observedSaveManager-ne$w.saveManager-or$requested.saveName-isnot[string]-or
    [string]::IsNullOrWhiteSpace($requested.saveName)-or$prepared.saveName-isnot[string]-or$prepared.saveName-cne$requested.saveName){throw 'Prepared autosave changed its retained request, native name or manager.'}
 $checks=$prepared.checks;$checkNames=@('ordered','sameGame','sameManager','samePlayer','requestRetained','distinctDescriptor','sameName','autoCategory','expectedLeaf','sameCampaign')
 Assert-KmcExactProperties $checks $checkNames 'native prepared descriptor checks'
 foreach($key in $checkNames){Assert-KmcChunk7Boolean $checks.$key $true}
 Assert-KmcExactProperties $d @('path','fileName','internalName','gameId','gameName','area','type','length','sha256','operation','autoLevelup','contentInspected','coldLoadQualified','requestedDescriptor','preparedDescriptor','completionSequence') 'native bootstrap output'
 foreach($key in @('path','fileName','internalName','gameId','gameName','area','type','sha256','operation')){if($d.$key-isnot[string]){throw 'Native bootstrap output text must be exact strings.'}}
 if($d.path-cne(Join-Path $root 'Auto_1.zks')-or$d.fileName-cne'Auto_1.zks'-or$d.type-cne'Auto'-or$d.operation-cne'None'-or
    $d.gameId-cne$w.gameId-or$d.gameName-cne$w.gameName-or$d.area-cne$p.area-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'-or
    $d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or[string]::IsNullOrWhiteSpace($d.internalName)){throw 'Native bootstrap output descriptor invalid.'}
 foreach($key in @('autoLevelup','contentInspected','coldLoadQualified')){Assert-KmcChunk7Boolean $d.$key $false};Assert-KmcOrdinaryInteger $d.length 1 268435456
 foreach($pair in @(@('saveLeaf','fileName'),@('saveName','internalName'),@('savePath','path'),@('saveArea','area'),@('saveGameId','gameId'),@('saveGameName','gameName'))){if($prepared.($pair[0])-isnot[string]-or$prepared.($pair[0])-cne$d.($pair[1])){throw 'Native PrepareSave did not bind the completed output descriptor.'}}
 Assert-KmcNativeBootstrapObject $d.requestedDescriptor;Assert-KmcNativeBootstrapObject $d.preparedDescriptor;Assert-KmcOrdinaryInteger $d.completionSequence 1 1
 if($d.requestedDescriptor-ne$requested.save-or$d.preparedDescriptor-ne$prepared.save-or$d.requestedDescriptor-eq$d.preparedDescriptor){throw 'Native completion did not bind its exact requested/prepared descriptor pair.'}
 $c=$o.cleanup
 foreach($key in @('custodySettled','callbackEntered','callbackReturned','dispatched','nativeWorkerSettled','observerReleased')){Assert-KmcChunk7Boolean $c.$key $true}
 foreach($key in @('slotProjectionOwned','identityWindowOpen')){Assert-KmcChunk7Boolean $c.$key $false}
 if($c.slotProjectionBefore-isnot[bool]-or$c.slotProjectionAfter-isnot[bool]-or$c.slotProjectionBefore-ne$c.slotProjectionAfter){throw 'Native slot projection did not restore its exact prior state.'}
 Assert-KmcChunk7NativeBootstrapAuthority $c.authority $root $a.generation 1
 if($c.authority.frozenGameId-cne$w.gameId-or$c.authority.frozenGameName-cne$w.gameName){throw 'Cleanup changed the admitted native campaign.'}
}
function Assert-KmcChunk7NativeBootstrapArtifact($Request,$Manifest,[string]$Status='PASS'){
 $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-native-bootstrap-evidence'-or$_.relativePath-ceq'chunk7-native-bootstrap-evidence.json'})
 if(-not(Test-KmcChunk7NativeBootstrapScenario $Request.scenario)){if($records.Count-ne0){throw 'Foreign request supplied native bootstrap evidence.'};return}
 if($Status-ceq'FAIL'-and$records.Count-eq0){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-native-bootstrap-evidence.json' 'chunk7-native-bootstrap-evidence'
 Assert-KmcChunk7NativeBootstrapEvidence $Request $artifact $Status
}
function Assert-KmcChunk7NativeBootstrapOutput($Request,$Artifact){
 Assert-KmcChunk7NativeBootstrapEvidence $Request $Artifact
 $d=$Artifact.observations.nativeWrite;$f=Get-Item -LiteralPath $d.path
 if($f.PSIsContainer-or($f.Attributes-band[IO.FileAttributes]::ReparsePoint)-ne0-or$f.Length-ne$d.length-or(Get-KmcSha256 $d.path)-cne$d.sha256){throw 'Native bootstrap output bytes differ from the completed native write receipt.'}
}
