Set-StrictMode -Version Latest
function Test-KmcChunk7OwnedPairsScenario([string]$Scenario){$Scenario-ceq'chunk7-owned-two-pair-campaign'}
function Get-KmcChunk7SelectedScenario($Request){
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){return [string]$Request.scenario}
 Assert-KmcChunk7OwnedPairsSelection $Request
 switch -CaseSensitive ($Request.ownedPairs.case){
  'exploration'{'chunk7-two-pair-exploration'}
  'owner-seed-exploration'{'chunk7-two-pair-exploration'}
  'owner-seed-persistence-save'{'chunk7-two-pair-exploration'}
  'owner-seed-persistence-load'{'chunk7-persistence-load'}
  'owner-seed-realtime-core'{'chunk7-two-pair-realtime-core'}
  'owner-seed-combat-rider-first'{'chunk7-two-pair-combat-rider-first'}
  'owner-seed-combat-mount-first'{'chunk7-two-pair-combat-mount-first'}
  'owner-seed-combat-disable'{'chunk7-two-pair-combat-rider-first'}
  'owner-seed-checkpoint-save'{'chunk7-persistence-save'}
  'owner-seed-checkpoint-load'{'chunk7-persistence-load'}
  'owner-seed-load-refusal'{'chunk7-persistence-load'}
  'combat-rider-first'{'chunk7-two-pair-combat-rider-first'}
  'combat-mount-first'{'chunk7-two-pair-combat-mount-first'}
  'realtime-core'{'chunk7-two-pair-realtime-core'}
  'realtime-casting'{'chunk7-two-pair-realtime-casting'}
  'realtime-charge'{'chunk7-two-pair-realtime-charge'}
  'native-life-cleanup'{'chunk7-two-pair-native-life-cleanup'}
  'persistence-save'{'chunk7-persistence-save'}
  'persistence-load'{'chunk7-persistence-load'}
 }
}
function Get-KmcChunk7OwnedWriteCount($Request){
 # Owner seed: zero writes. The installed game registers no LevelUpAutoSaveController, so setup makes no level-up Auto.
 if(Test-KmcChunk7OwnedPairsOwnerSeed $Request){return 0}
 # Owner-seed-derived Manual producer: exactly its one measured Manual; its cold consumer: none.
 if(Test-KmcChunk7OwnedPairsSeedManual $Request){return 1}
 # The worker writes a seed and its cancelled replacement to one landed archive.
 if(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request){if($Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'){return 2};return 1}
 if(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request){if($Request.persistenceCase-cin@('legacy-mounted-pending-tb-manual','owned-schema1-resave-manual')){return 1};return 0}
 if(Test-KmcChunk7OwnedPairsSeedDerivedInput $Request){return 0}
 if($Request.ownedPairs.case-ceq'persistence-save'-and$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'){return 2}
 if($Request.ownedPairs.case-ceq'persistence-save'-or($Request.ownedPairs.case-ceq'persistence-load'-and$Request.persistenceCase-cin@('legacy-mounted-pending-tb-manual','owned-schema1-resave-manual'))){return 1};return 0
}
function Get-KmcChunk7OwnedLoadCount($Request){
 if($Request.ownedPairs.case-cin@('persistence-load','owner-seed-persistence-load')-and$Request.persistenceCase-ceq'multi-exploration-three-loads-manual'){return 3};return 1
}
function Get-KmcChunk7OwnedPairIds($Request){@($Request.ownedPairs.riderAId,$Request.ownedPairs.mountAId,$Request.ownedPairs.riderBId,$Request.ownedPairs.mountBId)}
function Get-KmcChunk7SelectedFixture($Request){if(Test-KmcChunk7OwnedPairsScenario $Request.scenario){return $Request.persistenceLoad};return $Request.fixture.working}
# The owner-supplied seed kind of the closed family: seed pins, two selected riders,
# the full party and the exact rider-B native XP setup; no producer or pet pins.
function Test-KmcChunk7OwnedPairsOwnerSeed($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 if($null-eq$Request-or$null-eq$Request.PSObject.Properties['ownedPairs']){return $false};$s=$Request.ownedPairs
 if($s-is[Collections.IDictionary]){$s=[pscustomobject]$s}
 $null-ne$s-and$null-ne$s.PSObject.Properties['case']-and$s.case-is[string]-and$s.case-ceq'owner-seed-exploration'
}
function Get-KmcChunk7OwnedPairsCase($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 if($null-eq$Request-or$null-eq$Request.PSObject.Properties['ownedPairs']){return $null};$s=$Request.ownedPairs
 if($s-is[Collections.IDictionary]){$s=[pscustomobject]$s}
 if($null-eq$s-or$null-eq$s.PSObject.Properties['case']-or$s.case-isnot[string]){return $null};[string]$s.case
}
# Owner-seed-derived persistence: the producer runs the unchanged disclosed setup and writes one
# measured Manual; the consumer cold-loads exactly that Manual in a separate process. Both are
# labelled owner-seed-derived; neither is acquired or ordinary ancestry.
function Test-KmcChunk7OwnedPairsSeedManual($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-persistence-save'}
function Test-KmcChunk7OwnedPairsSeedDerivedLoad($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-persistence-load'}
# Owner-seed-derived combat (scenarios 3-5): a disposable copy of the derived Manual, restored mounted.
function Test-KmcChunk7OwnedPairsSeedDerivedCombat($Request){(Get-KmcChunk7OwnedPairsCase $Request)-cin@('owner-seed-realtime-core','owner-seed-combat-rider-first','owner-seed-combat-mount-first','owner-seed-combat-disable')}
function Test-KmcChunk7OwnedPairsRegisteredDisable($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-combat-disable'}
# Owner-seed checkpoint pair: the producer cold-loads a disposable copy of the same derived Manual,
# restored mounted, and writes one genuine native checkpoint; its consumer cold-loads that checkpoint.
function Test-KmcChunk7OwnedPairsSeedCheckpointSave($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-checkpoint-save'}
function Test-KmcChunk7OwnedPairsSeedCheckpointLoad($Request){(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-checkpoint-load'}
# The checkpoints the owner-seed checkpoint pair admits; extend this one list as each family is connected.
function Get-KmcChunk7OwnerSeedCheckpoints {@('multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual','multi-area-autosave')}
function Get-KmcChunk7OwnerSeedSourceOnlyCheckpoints {@('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual','owned-schema1-manual')}
function Get-KmcChunk7OwnerSeedLoadOnlyCheckpoints {@('owned-schema1-resave-manual','owned-schema1-current-manual','owned-schema2-pending-current-manual')}
function Test-KmcChunk7OwnerSeedCurrentCheckpoint([string]$Checkpoint){$Checkpoint-cin@('owned-schema1-current-manual','owned-schema2-pending-current-manual')}
function Get-KmcChunk7OwnerSeedCurrentParentCase([string]$Checkpoint){
 switch -CaseSensitive ($Checkpoint){'owned-schema1-current-manual'{'owned-schema1-resave-manual'}'owned-schema2-pending-current-manual'{'legacy-mounted-pending-tb-manual'}default{throw 'Current-format load requires its exact historical resave parent.'}}
}
# The connected combat checkpoints write a genuine native Manual under their own distinct name, never
# KMC_C7_WORLD (acquired ancestry) or KMC_C7_SEED_WORLD (the derived input).
function Test-KmcChunk7OwnerSeedCombatCheckpoint([string]$Checkpoint){$Checkpoint-cin@('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual')}
function Get-KmcChunk7OwnerSeedCheckpointManualName {'KMC_C7_SEED_CHECKPOINT'}
# Every kind whose native input is the owner-seed-derived Manual itself.
function Test-KmcChunk7OwnedPairsSeedDerivedInput($Request){(Test-KmcChunk7OwnedPairsSeedDerivedLoad $Request)-or(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)-or(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)-or(Test-KmcChunk7LoadRefusal $Request)}
# Every kind whose inherited custody keeps the owner-seed-derived labels (its staged receipt is the Manual producer's).
function Test-KmcChunk7OwnedPairsSeedDerivedLineage($Request){(Test-KmcChunk7OwnedPairsSeedDerivedInput $Request)-or(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)}
function Test-KmcChunk7OwnedPairsSeedSetup($Request){(Test-KmcChunk7OwnedPairsOwnerSeed $Request)-or(Test-KmcChunk7OwnedPairsSeedManual $Request)}
function Get-KmcChunk7OwnerSeedManualName {'KMC_C7_SEED_WORLD'}
function Assert-KmcChunk7OwnerSeedDerivedLoadSelection($Request,$Selection,$Checkpoint){
 $s=$Selection
 Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'owner-seed-derived cold selection'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed-derived consumer run identity is malformed.'}
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-cin@($Request.runId,$s.seedId)){throw 'Owner-seed-derived cold load requires its exact distinct producer run.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','seedSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed-derived cold load requires exact SHA256 pins.'}}
 if($s.seedId-isnot[string]-or$s.seedId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.seedId-ceq$Request.runId){throw 'Owner-seed-derived cold load requires the exact owner seed identity.'}
 if($s.seedSha256-cin@($s.sourceArchiveSha256,$s.sourceArtifactSha256)){throw 'The derived Manual or its evidence cannot alias the immutable owner seed.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 if($Checkpoint-isnot[string]-or$Checkpoint-cnotin@('multi-exploration-manual','multi-exploration-three-loads-manual')){throw 'Owner-seed-derived cold load requires its exact exploration Manual or bounded three-load case.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;$loads=Get-KmcChunk7OwnedLoadCount $Request;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount $loads $loads;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 0
}
function Assert-KmcChunk7OwnerSeedPairsSelection($Request,$Selection,$Checkpoint){
 $s=$Selection;$manual=$s.case-ceq'owner-seed-persistence-save'
 Assert-KmcExactProperties $s @('case','seedId','seedSha256','riderAId','riderBId','partyIds','xpAssistance','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount') 'owner-seed two-pair selection'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed consumer run identity is malformed.'}
 if($s.seedId-isnot[string]-or$s.seedId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.seedId-ceq$Request.runId){throw 'Owner-seed two-pair exploration requires an exact registered seed identity.'}
 if($s.seedSha256-isnot[string]-or$s.seedSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed two-pair exploration requires its exact seed SHA256.'}
 Assert-KmcOrdinaryGuid $s.riderAId;Assert-KmcOrdinaryGuid $s.riderBId
 if($s.riderAId-ceq$s.riderBId){throw 'Owner-seed rider A and rider B must be distinct actors.'}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt2-or$s.partyIds.Count-gt6){throw 'Owner-seed full party must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Owner-seed party repeats an actor.'}}
 foreach($id in @($s.riderAId,$s.riderBId)){if(-not$party.Contains($id)){throw 'Owner-seed riders must be members of the selected party.'}}
 if($null-ne$s.xpAssistance){
  Assert-KmcExactProperties $s.xpAssistance @('unitId','targetCharacterLevel') 'owner-seed XP assistance'
  if($s.xpAssistance.unitId-isnot[string]-or$s.xpAssistance.unitId-cne$s.riderBId){throw 'Owner-seed XP assistance may target only rider B.'}
  Assert-KmcOrdinaryInteger $s.xpAssistance.targetCharacterLevel 2 8
 }
 if($manual){
  # The producer's only write is one measured native Manual after SETUP-COMPLETE.
  if($Checkpoint-isnot[string]-or$Checkpoint-cne'multi-exploration-manual'){throw 'Owner-seed-derived Manual production selects exactly the multi-exploration-manual checkpoint.'}
  Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 1 1;return
 }
 if($null-ne$Checkpoint){throw 'Owner-seed exploration cannot select a persistence checkpoint.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1
 if(($s.expectedNativeWriteCount-isnot[int]-and$s.expectedNativeWriteCount-isnot[long])-or$s.expectedNativeWriteCount-ne0){throw 'Owner-seed exploration selects zero native writes: the native game has no level-up autosave (LevelUpAutoSaveController is never registered).'}
}
function Assert-KmcChunk7OwnedPairsSelection($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['ownedPairs']
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){if($has){throw 'Inherited two-owner selection is forbidden under another family.'};return}
 if(-not$has){throw 'Inherited two-owner campaign requires its explicit selection.'};$s=$Request.ownedPairs
 $checkpoint=$null;if($null-ne$Request.PSObject.Properties['persistenceCase']){$checkpoint=$Request.persistenceCase}
 if(Test-KmcChunk7OwnedPairsSeedSetup $Request){Assert-KmcChunk7OwnerSeedPairsSelection $Request $s $checkpoint;return}
 if(Test-KmcChunk7OwnedPairsSeedDerivedLoad $Request){Assert-KmcChunk7OwnerSeedDerivedLoadSelection $Request $s $checkpoint;return}
 if(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request){Assert-KmcChunk7OwnerSeedDerivedCombatSelection $Request $s $checkpoint;return}
 if(Test-KmcChunk7LoadRefusal $Request){Assert-KmcChunk7LoadRefusalSelection $Request $s $checkpoint;return}
 if((Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)-or(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)){Assert-KmcChunk7OwnerSeedCheckpointSelection $Request $s $checkpoint;return}
 $area=$checkpoint-is[string]-and$checkpoint-ceq'multi-area-autosave'
 $fields=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','checkpointSourceRunId','checkpointSourceArtifactSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount');if($area){$fields+='sourceArea'}
 Assert-KmcExactProperties $s $fields 'inherited native pair selection'
 if($area-and($s.sourceArea-isnot[string]-or$s.sourceArea-cnotmatch'^[a-f0-9]{32}$'-or$s.sourceArea-ceq('0'*32))){throw 'Owned area checkpoint requires the exact original acquired Manual area.'}
 if($s.case-isnot[string]-or$s.case-cnotin@('exploration','combat-rider-first','combat-mount-first','realtime-core','realtime-casting','realtime-charge','native-life-cleanup','persistence-save','persistence-load')){throw 'Inherited pair case is not closed.'}
 if($s.sourceRunId-isnot[string]-or$s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Inherited pair source run must be distinct and exact.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited source requires exact SHA256 pins.'}}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 if($s.case-cin@('persistence-save','persistence-load')){if($checkpoint-isnot[string]-or($checkpoint-cnotin@('multi-exploration-manual','multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual','multi-area-autosave')-and($s.case-cne'persistence-load'-or$checkpoint-cnotin@('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual','owned-schema1-resave-manual'))-and($s.case-cne'persistence-save'-or$checkpoint-cnotin@('multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual','owned-schema1-manual')))){throw 'Inherited persistence retains its closed checkpoints; save boundaries are source-only.'}}
 elseif($null-ne$checkpoint){throw 'Non-persistence inherited campaign cannot select a checkpoint.'}
 if($s.case-ceq'persistence-load'){
  if($s.checkpointSourceRunId-isnot[string]-or$s.checkpointSourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.checkpointSourceRunId-cin@($Request.runId,$s.sourceRunId)-or$s.checkpointSourceArtifactSha256-isnot[string]-or$s.checkpointSourceArtifactSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Cold inherited checkpoint requires its exact separate successful source.'}
 }elseif($null-ne$s.checkpointSourceRunId-or$null-ne$s.checkpointSourceArtifactSha256){throw 'Only cold inherited persistence consumes a checkpoint parent.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;$loads=Get-KmcChunk7OwnedLoadCount $Request;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount $loads $loads
 $writes=Get-KmcChunk7OwnedWriteCount $Request;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount $writes $writes
}
function Assert-KmcChunk7OwnedPairsDescriptor($Request){
 Assert-KmcChunk7OwnedPairsSelection $Request
 if($null-ne$Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-ceq'multi-area-autosave'-and((Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)-or(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request))){
  Assert-KmcChunk7OwnerSeedAreaTarget $Request.ownedPairs $Request.persistenceCase $Request.persistenceAreaTarget
  Assert-KmcBootstrapInventoryString $Request.persistenceLoad.area $(if(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request){$Request.ownedPairs.sourceArea}else{$Request.persistenceAreaTarget.area})
 }
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)){throw 'Inherited descriptor belongs to the exact separate family.'}
 if(-not(Test-KmcChunk7LoadRefusal $Request)-and$null-ne$Request.PSObject.Properties['loadRefusalDerivationSha256']-and$null-ne$Request.loadRefusalDerivationSha256){throw 'Only the exact refusal request carries refusal provenance.'}
 $d=$Request.persistenceLoad;Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'inherited native descriptor'
 if(Test-KmcChunk7OwnedPairsSeedSetup $Request){
  # Registry-supplied exact names, admitted generically as the cold owner-seed kind does; an
  # owner-seed-derived Manual is never re-admitted as an owner seed.
  if($d.internalName-isnot[string]-or$d.internalName-cnotmatch(Get-KmcChunk7OwnerSeedNamePattern)-or$d.internalName-cin@('KMC_C7_ASSISTED_FIXTURE',(Get-KmcChunk7OwnerSeedManualName))){throw 'Owner seed internal name is malformed or impersonates the producer fixture.'}
  if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^[A-Za-z0-9][A-Za-z0-9_.-]{0,95}\.zks$'-or$d.fileName.EndsWith('_KMC_C7_ASSISTED_FIXTURE.zks',[StringComparison]::Ordinal)-or$d.fileName.EndsWith('_'+(Get-KmcChunk7OwnerSeedManualName)+'.zks',[StringComparison]::Ordinal)){throw 'Owner seed file name is malformed or impersonates the producer fixture.'}
  if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner seed archive hash is malformed.'};Assert-KmcBootstrapInventoryString $d.sha256 $Request.ownedPairs.seedSha256
  Assert-KmcOrdinaryGuid $d.gameId
  if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Owner seed campaign must remain distinct from protected Working/Baseline.'}
  Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks);return
 }
 if(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request){
  # Exactly the checkpoint producer's fresh native slot or distinct checkpoint Manual, never the derived Manual or the immutable seed.
  $type=Get-KmcChunk7PersistenceType $Request.persistenceCase
  if($type-ceq'Manual'){
   $name=if($Request.persistenceCase-ceq'owned-schema1-current-manual'){'KMC_C7_SCHEMA1'}elseif($Request.persistenceCase-ceq'owned-schema2-pending-current-manual'){'KMC_C7_SCHEMA2'}else{Get-KmcChunk7OwnerSeedCheckpointManualName};Assert-KmcBootstrapInventoryString $d.internalName $name
   if($d.fileName-isnot[string]-or$d.fileName-cnotmatch('^Manual_([1-9][0-9]*)_'+$name+'\.zks$')){throw 'Owner-seed checkpoint native Manual name/number is not canonical.'}
  }else{Assert-KmcBootstrapInventoryString $d.fileName ($type+'_1.zks')}
  if($d.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.internalName)-or$d.internalName.Length-gt256-or$d.internalName-match'[\x00-\x1f\x7f]'-or$d.internalName-cin@('KMC_C7_ASSISTED_FIXTURE',(Get-KmcChunk7OwnerSeedManualName))){throw 'Owner-seed checkpoint native localized slot name is invalid.'}
  if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'-or$d.sha256-cin@($Request.ownedPairs.sourceArchiveSha256,$Request.ownedPairs.seedSha256)){throw 'Owner-seed checkpoint archive hash is malformed or aliases its Manual or seed.'}
  Assert-KmcOrdinaryGuid $d.gameId
  if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Owner-seed checkpoint campaign must remain distinct from protected Working/Baseline.'}
  Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks);return
 }
 if(Test-KmcChunk7OwnedPairsSeedDerivedInput $Request){
  # Exactly the producer's written owner-seed-derived Manual, never the immutable seed.
  $name=Get-KmcChunk7OwnerSeedManualName;Assert-KmcBootstrapInventoryString $d.internalName $name
  if($d.fileName-isnot[string]-or$d.fileName-cnotmatch('^Manual_([1-9][0-9]*)_'+$name+'\.zks$')){throw 'Owner-seed-derived native Manual name/number is not canonical.'}
  if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed-derived archive hash is malformed.'};Assert-KmcBootstrapInventoryString $d.sha256 $Request.ownedPairs.sourceArchiveSha256
  Assert-KmcOrdinaryGuid $d.gameId
  if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or$d.sha256-ceq$Request.ownedPairs.seedSha256){throw 'Owner-seed-derived campaign must remain distinct from protected Working/Baseline and the immutable seed.'}
  Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks);return
 }
 $cold=$Request.ownedPairs.case-ceq'persistence-load';$type=if($cold){Get-KmcChunk7PersistenceType $Request.persistenceCase}else{'Manual'}
 if($d.sha256-isnot[string]-or$d.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Inherited archive hash is malformed.'};if(-not$cold){Assert-KmcBootstrapInventoryString $d.sha256 $Request.ownedPairs.sourceArchiveSha256}
 if($type-ceq'Manual'){
  $name=if($cold){'KMC_C7_WORLD'}else{'KMC_C7_ACQUIRED_FIELD'};Assert-KmcBootstrapInventoryString $d.internalName $name
  if($d.fileName-isnot[string]-or$d.fileName-cnotmatch('^Manual_([1-9][0-9]*)_'+$name+'\.zks$')){throw 'Inherited native Manual name/number is not canonical.'}
  $number=0;if(-not[int]::TryParse($Matches[1],[Globalization.NumberStyles]::None,[Globalization.CultureInfo]::InvariantCulture,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number.ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+$name+'.zks')){throw 'Inherited native number is out of range.'}
 }else{
  Assert-KmcBootstrapInventoryString $d.fileName ($type+'_1.zks')
  if($d.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.internalName)-or$d.internalName.Length-gt256-or$d.internalName-match'[\x00-\x1f\x7f]'){throw 'Inherited native localized slot name is invalid.'}
 }
 Assert-KmcOrdinaryGuid $d.gameId
 if($d.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.gameName)-or$d.gameName.Length-gt256-or$d.gameName-match'[\x00-\x1f\x7f]'-or$d.area-isnot[string]-or$d.area-cnotmatch'^[a-f0-9]{32}$'-or$d.gameId-ceq$Request.fixture.working.gameId-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256){throw 'Inherited native campaign must remain distinct from protected Working/Baseline.'}
 if($Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-ceq'multi-area-autosave'){Assert-KmcChunk7AreaTarget $Request;$expectedArea=if($cold){$Request.persistenceAreaTarget.area}else{$Request.ownedPairs.sourceArea};Assert-KmcBootstrapInventoryString $d.area $expectedArea}
 Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
}
function Assert-KmcChunk7InheritedSnapshot($Request,$Snapshot,[string]$Phase){
 Assert-KmcExactProperties $Snapshot @('contract','phase','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','sourceCase','nativeLoadVerifiedByHost','decodedReceiptAuthorizesRestoration','inheritedPetsPreserved','ownedFactRemovalCount','partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities') 'inherited native custody'
 # Owner-seed-derived custody keeps its own labels; it is never relabelled acquired ancestry.
 $derived=Test-KmcChunk7OwnedPairsSeedDerivedLineage $Request
 $contract=if($derived){'owner-seed-derived-two-horse-native-field-v1'}else{'inherited-two-horse-native-field-v1'};$sourceCase=if($derived){'owner-seed-persistence-save'}else{'preserve-two-acquired-field'}
 foreach($pair in @(@('contract',$contract),@('phase',$Phase),@('sourceCase',$sourceCase))){Assert-KmcBootstrapInventoryString $Snapshot.($pair[0]) $pair[1]}
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $Snapshot.$key $Request.ownedPairs.$key}
 foreach($key in @('nativeLoadVerifiedByHost','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $Snapshot.$key $true};Assert-KmcChunk7Boolean $Snapshot.decodedReceiptAuthorizesRestoration $false;Assert-KmcOrdinaryInteger $Snapshot.ownedFactRemovalCount 0 0
 if($Snapshot.partyIds-isnot[Array]-or(ConvertTo-KmcChunk7Canonical @($Snapshot.partyIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Request.ownedPairs.partyIds|Sort-Object -CaseSensitive))){throw 'Inherited full native roster differs.'}
 foreach($name in @('currentPartyIds','currentCharacterIds','remoteCapturedIds')){if($Snapshot.$name-isnot[Array]){throw 'Current native roster observations must be arrays.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $Snapshot.$name){Assert-KmcOrdinaryGuid $id;if(-not$seen.Add($id)){throw 'Current native roster repeats an actor.'}}}
 if((ConvertTo-KmcChunk7Canonical @($Snapshot.currentPartyIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($Snapshot.partyIds|Sort-Object -CaseSensitive))-or$Snapshot.remoteCapturedIds.Count-ne0-or$Snapshot.currentCharacterIds.Count-lt2-or$Snapshot.currentCharacterIds.Count-gt6){throw 'Current field/remote membership differs from inherited custody.'}
 foreach($id in $Snapshot.currentCharacterIds){if($id-cnotin$Snapshot.partyIds){throw 'Native field character is outside the admitted roster.'}}
 foreach($id in @($Request.ownedPairs.riderAId,$Request.ownedPairs.riderBId)){if($id-cnotin$Snapshot.currentCharacterIds){throw 'Inherited rider left the native character roster.'}}
 Assert-KmcExactProperties $Snapshot.identities @('capacity','ids','faults') 'inherited identity registry';Assert-KmcOrdinaryInteger $Snapshot.identities.capacity 128 128;Assert-KmcOrdinaryInteger $Snapshot.identities.faults 0 0
 if($Snapshot.identities.ids-isnot[Array]-or$Snapshot.identities.ids.Count-ne8){throw 'Inherited identity registry requires its exact eight retained objects.'}
 $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $Snapshot.identities.ids){Assert-KmcManualSeedObjectIdentity $id;if(-not$labels.Add([int]$id)){throw 'Inherited registry label repeats.'}}
 if($Snapshot.pairs-isnot[Array]-or$Snapshot.pairs.Count-ne2){throw 'Inherited snapshot requires prior/new two-pair order.'};$used=[Collections.Generic.HashSet[int]]::new();$ids=Get-KmcChunk7OwnedPairIds $Request
 for($i=0;$i-lt2;$i++){$p=$Snapshot.pairs[$i];Assert-KmcExactProperties $p @('riderId','mountId','rider','mount','feature','rankFact','sourceGuid','rank','profileId') 'inherited pair custody'
  Assert-KmcBootstrapInventoryString $p.riderId $ids[$i*2];Assert-KmcBootstrapInventoryString $p.mountId $ids[$i*2+1]
  foreach($key in @('rider','mount','feature','rankFact')){Assert-KmcManualSeedObjectIdentity $p.$key;if(-not$labels.Contains([int]$p.$key)-or-not$used.Add([int]$p.$key)){throw 'Inherited native roles alias or are unretained.'}}
  if($p.sourceGuid-isnot[string]-or$p.sourceGuid-cnotmatch'^[0-9a-f]{32}$'-or$p.profileId-isnot[string]-or$p.profileId-cnotmatch'^[a-z0-9][a-z0-9.-]{0,127}$'){throw 'Inherited native source/profile token is malformed.'};Assert-KmcOrdinaryInteger $p.rank 1 20
 }
 Assert-KmcExactProperties $Snapshot.arrangement @('money','experienceRatePercent','actors') 'inherited no-arrangement state';Assert-KmcOrdinaryInteger $Snapshot.arrangement.money 0;Assert-KmcOrdinaryInteger $Snapshot.arrangement.experienceRatePercent 100 100
 if($Snapshot.arrangement.actors-isnot[Array]-or$Snapshot.arrangement.actors.Count-ne$Request.ownedPairs.partyIds.Count){throw 'Inherited progression roster differs.'}
 $actors=[Collections.Generic.HashSet[string]]::new();foreach($a in $Snapshot.arrangement.actors){Assert-KmcExactProperties $a @('actorId','blueprint','experience','characterLevel','classes') 'inherited native progression';Assert-KmcOrdinaryGuid $a.actorId;if(-not$actors.Add($a.actorId)-or$a.actorId-cnotin$Request.ownedPairs.partyIds){throw 'Inherited progression actor repeats or is foreign.'}
  if($a.blueprint-isnot[string]-or$a.blueprint-cnotmatch'^[0-9a-f]{32}$'){throw 'Inherited actor blueprint invalid.'};Assert-KmcOrdinaryInteger $a.experience 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $a.characterLevel 1 20
  if($a.classes-isnot[Array]-or$a.classes.Count-lt1-or$a.classes.Count-gt20){throw 'Inherited native class rows are unbounded.'};$classes=[Collections.Generic.HashSet[string]]::new();$sum=0
  foreach($c in $a.classes){Assert-KmcExactProperties $c @('classGuid','level') 'inherited class';if($c.classGuid-isnot[string]-or$c.classGuid-cnotmatch'^[0-9a-f]{32}$'-or-not$classes.Add($c.classGuid)){throw 'Inherited class repeats or is malformed.'};Assert-KmcOrdinaryInteger $c.level 1 20;$sum+=$c.level};if($sum-ne$a.characterLevel){throw 'Inherited class levels disagree.'}
 }
}
function Assert-KmcChunk7InheritedEvidence($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 Assert-KmcChunk7InheritedSnapshot $Request $Observations.inheritedFixture 'admitted';Assert-KmcChunk7InheritedSnapshot $Request $Observations.inheritedCleanup 'cleanup'
 foreach($key in @('partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities')){if((ConvertTo-KmcChunk7Canonical $Observations.inheritedFixture.$key)-cne(ConvertTo-KmcChunk7Canonical $Observations.inheritedCleanup.$key)){throw 'Inherited native roster/facts/progression changed across execution and cleanup.'}}
 Assert-KmcChunk7Boolean $Observations.cleanup.inheritedPetsPreserved $true;Assert-KmcChunk7Boolean $Observations.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false
}

function Assert-KmcChunk7OwnedActionFixture($Request,$Observations){
 Assert-KmcChunk7InheritedEvidence $Request $Observations
 Assert-KmcBootstrapInventoryString $Observations.scenario $Request.scenario
 if($null-ne$Observations.workingSha256){throw 'Inherited action proof cannot claim historical Working fixture identity.'}
 if($Observations.actorIds-isnot[Array]-or(ConvertTo-KmcChunk7Canonical $Observations.actorIds)-cne(ConvertTo-KmcChunk7Canonical @(Get-KmcChunk7OwnedPairIds $Request))){throw 'Native action roles differ from qualified inherited pair order.'}
 Assert-KmcOrdinaryGuid $Observations.targetId
 if($Observations.targetId-cin$Request.ownedPairs.partyIds){throw 'Owned hostile target aliases an inherited native party actor.'}
 Assert-KmcBootstrapInventoryString $Observations.targetGroupId ('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')
}

function Assert-KmcChunk7OwnedArchiveProfiles($Request,$Observations,$Archive){
 foreach($pair in $Archive.Pairs){$rows=@($Observations.inheritedFixture.pairs|Where-Object riderId -CEQ $pair.Rider.Id)
  if($rows.Count-ne1){throw 'Mounted archive rider is outside inherited ownership.'};Assert-KmcBootstrapInventoryString $pair.Mount.Id $rows[0].mountId;Assert-KmcBootstrapInventoryString $pair.ProfileId $rows[0].profileId
 }
}
# The native Manual name a writing owned selection reserves (C# Chunk7OwnedPairsSelection.OwnedManualOutputName).
function Get-KmcChunk7OwnedManualOutputName($Request){
 if(Test-KmcChunk7OwnedPairsSeedManual $Request){return Get-KmcChunk7OwnerSeedManualName}
 if(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request){return Get-KmcChunk7OwnerSeedCheckpointManualName}
 if((Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)-and$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){return 'KMC_C7_SCHEMA2'}
 if((Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)-and$Request.persistenceCase-ceq'owned-schema1-resave-manual'){return 'KMC_C7_SCHEMA1'}
 if($Request.ownedPairs.case-ceq'persistence-load'){if($Request.persistenceCase-ceq'owned-schema1-resave-manual'){return 'KMC_C7_SCHEMA1'};return 'KMC_C7_SCHEMA2'}
 'KMC_C7_WORLD'
}
function Get-KmcChunk7OwnedOutputLeaf($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if((Get-KmcChunk7OwnedWriteCount $Request)-le0){throw 'Read-only inherited case has no output leaf.'}
 $a=$Observations.ownedOutputAdmission;Assert-KmcOrdinaryInteger $a.number 1 ([int]::MaxValue)
 $type=Get-KmcChunk7PersistenceType $Request.persistenceCase
 $seedManual=Test-KmcChunk7OwnedPairsSeedManual $Request
 $name=Get-KmcChunk7OwnedManualOutputName $Request
 if($type-ceq'Manual'){
  # The owner-seed producer numbers its Manual after the owner's registered seed leaf; the owner-seed
  # checkpoint producer after the derived Manual it loaded.
  $sourcePattern=if($seedManual){'^Manual_([1-9][0-9]*)_[A-Za-z0-9_.-]+[.]zks$'}elseif(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request){'^Manual_([1-9][0-9]*)_'+(Get-KmcChunk7OwnerSeedManualName)+'[.]zks$'}elseif(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request){'^Manual_([1-9][0-9]*)_'+(Get-KmcChunk7OwnerSeedCheckpointManualName)+'[.]zks$'}else{'^Manual_([1-9][0-9]*)_KMC_C7_(ACQUIRED_FIELD|WORLD)[.]zks$'}
  if($Request.persistenceLoad.fileName-cnotmatch$sourcePattern){throw 'Inherited Manual source numbering is not canonical.'}
  $sourceNumber=0;if(-not[int]::TryParse($Matches[1],[ref]$sourceNumber)-or$sourceNumber-eq[int]::MaxValue-or$a.number-ne$sourceNumber+1){throw 'Reserved output is not the exact native next Manual number.'}
  $leaf='Manual_'+([int]$a.number).ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+$name+'.zks'
 }else{if($a.number-ne1){throw 'Fresh native Quick/Auto inventory must select number one.'};$leaf=$type+'_1.zks'}
 Assert-KmcBootstrapInventoryString $a.leaf $leaf;return $leaf
}
function Assert-KmcChunk7OwnedNativeDescriptor($Value,$Descriptor,[string]$Type){
 Assert-KmcExactProperties $Value @('fileName','nativeName','nativeType','gameId','gameName','area','hasFileOnDisk','isActuallySaved','operation','autoLevelup') 'settled inherited native descriptor'
 foreach($pair in @(@('fileName','fileName'),@('nativeName','internalName'),@('gameId','gameId'),@('gameName','gameName'),@('area','area'))){Assert-KmcBootstrapInventoryString $Value.($pair[0]) $Descriptor.($pair[1])}
 Assert-KmcBootstrapInventoryString $Value.nativeType $Type;Assert-KmcBootstrapInventoryString $Value.operation 'None'
 Assert-KmcChunk7Boolean $Value.hasFileOnDisk $true;Assert-KmcChunk7Boolean $Value.isActuallySaved $true;Assert-KmcChunk7Boolean $Value.autoLevelup $false
}
function Assert-KmcChunk7OwnedOutputEvidence($Request,$Observations){
 $leaf=Get-KmcChunk7OwnedOutputLeaf $Request $Observations;$a=$Observations.ownedOutputAdmission;$b=$Observations.ownedOutputCompletion
 Assert-KmcExactProperties $a @('method','token','moduleMvid','number','leaf','nativeInventory','authority') 'inherited native save admission'
 Assert-KmcBootstrapInventoryString $a.method 'FindUnusedSaveNumber';Assert-KmcBootstrapInventoryString $a.token '06008024';Assert-KmcBootstrapInventoryString $a.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 $worker=$Request.ownedPairs.case-cin@('persistence-save','owner-seed-checkpoint-save')-and$Request.persistenceCase-ceq'multi-owned-worker-cancel-manual'
 $writeCount=Get-KmcChunk7OwnedWriteCount $Request
 $area=$Request.ownedPairs.case-cin@('persistence-save','owner-seed-checkpoint-save')-and$Request.persistenceCase-ceq'multi-area-autosave'
 $properties=@('completionSequence','callbackCount','nativeWorkerSettled','nativeInventory','authority');if($worker){$properties+='replacement'};if($area){$properties+='areaSaveIdentity'}
 Assert-KmcExactProperties $b $properties 'inherited native save settlement'
 if($worker){$replacement=$b.replacement;Assert-KmcExactProperties $replacement @('callbackCount','completedSaveCount','drainCount','committed','outcomeEstablished','path','stagingLeaf') 'inherited cancelled replacement';Assert-KmcOrdinaryInteger $replacement.callbackCount 0 0;foreach($key in @('completedSaveCount','drainCount')){Assert-KmcOrdinaryInteger $replacement.$key 1 1};foreach($key in @('committed','outcomeEstablished')){Assert-KmcChunk7Boolean $replacement.$key $true};Assert-KmcBootstrapInventoryString $replacement.path $Observations.nativeWrite.path;Assert-KmcBootstrapInventoryString $replacement.stagingLeaf ('Manual_'+($a.number+1).ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+(Get-KmcChunk7OwnedManualOutputName $Request)+'.zks')}
 Assert-KmcOrdinaryInteger $b.completionSequence 1 1;Assert-KmcOrdinaryInteger $b.callbackCount $(if($area){0}else{1}) $(if($area){0}else{1});Assert-KmcChunk7Boolean $b.nativeWorkerSettled $true
 $type=Get-KmcChunk7PersistenceType $Request.persistenceCase;$name=if($type-ceq'Manual'){Get-KmcChunk7OwnedManualOutputName $Request}else{$Observations.nativeWrite.nativeName}
 if($name-isnot[string]-or[string]::IsNullOrWhiteSpace($name)-or$name.Length-gt256-or$name-match'[\x00-\x1f\x7f]'){throw 'Native output name must be an exact bounded scalar.'}
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 for($i=0;$i-lt2;$i++){$v=if($i-eq0){$a.authority}else{$b.authority}
  Assert-KmcExactProperties $v @('root','loadCap','writeCap','nativeLoads','nativeWrites','authorizedLoads','authorizedWrites','reservationCount','number','leaf','nativeName','nativeType') 'inherited exact output authority'
  Assert-KmcBootstrapInventoryString $v.root $root;foreach($key in @('loadCap','nativeLoads','authorizedLoads','reservationCount')){Assert-KmcOrdinaryInteger $v.$key 1 1}
  Assert-KmcOrdinaryInteger $v.writeCap $writeCount $writeCount
  $writes=if($i-eq0){0}else{$writeCount};foreach($key in @('nativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $v.$key $writes $writes};Assert-KmcOrdinaryInteger $v.number $a.number $a.number
  Assert-KmcBootstrapInventoryString $v.leaf $leaf;Assert-KmcBootstrapInventoryString $v.nativeType $type;Assert-KmcBootstrapInventoryString $v.nativeName $name
 }
 if($a.nativeInventory-isnot[Array]-or$a.nativeInventory.Count-ne1-or$b.nativeInventory-isnot[Array]-or$b.nativeInventory.Count-ne2){throw 'Inherited native inventory did not transition from one immutable input to one fresh output.'}
 $inputType=if($Request.ownedPairs.case-cin@('persistence-load','owner-seed-checkpoint-load')){Get-KmcChunk7PersistenceType $Request.persistenceCase}else{'Manual'}
 Assert-KmcChunk7OwnedNativeDescriptor $a.nativeInventory[0] $Request.persistenceLoad $inputType
 $input=@($b.nativeInventory|Where-Object fileName -CEQ $Request.persistenceLoad.fileName);$output=@($b.nativeInventory|Where-Object fileName -CEQ $leaf)
 if($input.Count-ne1-or$output.Count-ne1-or$leaf-ceq$Request.persistenceLoad.fileName){throw 'Settled inherited inventory aliases or omits the immutable input/fresh output.'}
 Assert-KmcChunk7OwnedNativeDescriptor $input[0] $Request.persistenceLoad $inputType
 $descriptor=[pscustomobject]@{fileName=$leaf;internalName=$name;gameId=$Request.persistenceLoad.gameId;gameName=$Request.persistenceLoad.gameName;area=(Get-KmcChunk7ExpectedArchiveArea $Request)}
 Assert-KmcChunk7OwnedNativeDescriptor $output[0] $descriptor $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.path (Join-Path $root $leaf)
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeType $type
 Assert-KmcBootstrapInventoryString $Observations.nativeWrite.nativeName $name
 if($area){Assert-KmcChunk7OwnedAreaSaveIdentity $Request $Observations}
}
function Assert-KmcChunk7OwnedFinalSaveRoot($Request,[string[]]$Leaves,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 $writes=Get-KmcChunk7OwnedWriteCount $Request;$expected=@($Request.persistenceLoad.fileName)
 if(Test-KmcChunk7LoadRefusal $Request){Assert-KmcChunk7LoadRefusalDescriptor $Request;if($writes-ne0){throw 'Refusal cannot acquire a native output leaf.'};$expected+=$Request.persistenceAlternate.fileName}
 if($writes-gt0){Assert-KmcChunk7OwnedOutputEvidence $Request $Observations;$expected+=Get-KmcChunk7OwnedOutputLeaf $Request $Observations}
 # Two worker requests replace one native target. Its temporary next-number
 # staging archive must be gone, and no other file or directory is admitted.
 if($Leaves.Count-ne$expected.Count-or(@($Leaves|Sort-Object -CaseSensitive)-join'|')-cne(@($expected|Sort-Object -CaseSensitive)-join'|')){throw 'Inherited final save root differs from its exact immutable input and landed output inventory.'}
}

# Owner-seed two-pair exploration evidence. The setup disclosure, every assistance
# item, the SETUP-COMPLETE boundary and the fixture-setup (not acquisition) outcome
# per rider are required exactly; returns the observed roles riderA, mountA, riderB, mountB.
function Get-KmcChunk7OwnerSeedManualDisclosure {'Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter''s Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof. Setup makes no native write; after SETUP-COMPLETE the only native write is one measured Manual into the run-owned isolated save root, labelled owner-seed-derived with disclosed assistance.'}
function Get-KmcChunk7OwnerSeedDisclosure {'Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter''s Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof; the only native writes are the game''s own level-up Autos into the run-owned isolated save root during setup, none after SETUP-COMPLETE.'}
# Stock Kingmaker character XP table (BlueprintRoot.Progression.XPTable, the
# Pathfinder medium advancement track): index = character level, threshold = XP.
# Pinned here; the product emits the native table it used and must match exactly.
function Get-KmcChunk7StockXpTable {@(0,0,2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,3600000)}
function Get-KmcChunk7XpReadyLevel([long]$Experience){$t=Get-KmcChunk7StockXpTable;$level=0;while($level-lt20-and$t[$level+1]-le$Experience){$level++};$level}
function Assert-KmcChunk7OwnerSeedEconomy($Value,[string]$Name,$Selection){
 Assert-KmcExactProperties $Value @('money','itemRows','inventorySha256','characterIds') $Name
 Assert-KmcOrdinaryInteger $Value.money 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $Value.itemRows 0 100000
 if($Value.inventorySha256-isnot[string]-or$Value.inventorySha256-cnotmatch'^[a-f0-9]{64}$'-or$Value.characterIds-isnot[Array]-or$Value.characterIds.Count-lt1){throw ('Owner-seed '+$Name+' is malformed.')}
 # The digested characters are selected party members and include both riders.
 $ids=@($Value.characterIds);foreach($id in $ids){if($id-isnot[string]-or$id-cnotin@($Selection.partyIds)){throw ('Owner-seed '+$Name+' digests a character outside the selected party.')}}
 if(@($ids|Select-Object -Unique).Count-ne$ids.Count-or$Selection.riderAId-cnotin$ids-or$Selection.riderBId-cnotin$ids){throw ('Owner-seed '+$Name+' must digest both riders exactly once.')}
}
# Disclosure only, never compared: each setup Horse's items read from the Horse itself. No
# minimum count and no pinned blueprint/slot: the native layout (incl. any StartingInventory)
# is not yet observed natively. Each listed item must be held by that Horse.
function Assert-KmcChunk7OwnerSeedHorseItems($Value,[string[]]$HorseIds){
 Assert-KmcExactProperties $Value @('horses','note') 'owner-seed setup Horse item disclosure'
 Assert-KmcBootstrapInventoryString $Value.note 'native Horse body/inventory disclosure; not compared; layout not yet observed natively'
 if($Value.horses-isnot[Array]-or$Value.horses.Count-ne$HorseIds.Count){throw 'Owner-seed setup Horse item disclosure must cover each setup Horse.'}
 for($i=0;$i-lt$HorseIds.Count;$i++){
  $h=$Value.horses[$i];Assert-KmcExactProperties $h @('horseId','sharedCollection','items') 'owner-seed setup Horse items';Assert-KmcBootstrapInventoryString $h.horseId $HorseIds[$i]
  if($h.sharedCollection-isnot[bool]-or$h.items-isnot[Array]-or$h.items.Count-gt256){throw 'Owner-seed setup Horse item disclosure is malformed.'}
  foreach($item in $h.items){
   Assert-KmcExactProperties $item @('blueprint','slot','count','heldBy') 'owner-seed setup Horse item'
   if($item.blueprint-isnot[string]-or[string]::IsNullOrWhiteSpace($item.blueprint)-or$item.slot-isnot[string]-or[string]::IsNullOrWhiteSpace($item.slot)){throw 'Owner-seed setup Horse item identity is malformed.'}
   Assert-KmcOrdinaryInteger $item.count 1 100000;Assert-KmcBootstrapInventoryString $item.heldBy $HorseIds[$i]
  }
 }
}
function Assert-KmcChunk7OwnerSeedWorldSetup($Request,$Observations){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsSeedSetup $Request)){throw 'Owner-seed setup evidence belongs only to the owner-seed setup kinds.'}
 $s=$Request.ownedPairs;$o=$Observations;$manual=Test-KmcChunk7OwnedPairsSeedManual $Request
 foreach($key in @('inheritedFixture','inheritedCleanup','acquisition','failurePhase')){if($null-ne$o.PSObject.Properties[$key]){throw ('Owner-seed evidence cannot carry '+$key+'.')}}
 $seed=$o.ownerSeed;Assert-KmcExactProperties $seed @('contract','seedId','seedSha256','riderAId','riderBId','nativeLoadVerifiedByHost','fixtureSetupOnly','acquisitionProof') 'owner-seed identity'
 $seedContract=if($manual){'owner-seed-derived-manual-producer-v1'}else{'owner-seed-two-pair-exploration-v1'}
 foreach($pair in @(@('contract',$seedContract),@('seedId',$s.seedId),@('seedSha256',$s.seedSha256),@('riderAId',$s.riderAId),@('riderBId',$s.riderBId))){Assert-KmcBootstrapInventoryString $seed.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $seed.nativeLoadVerifiedByHost $true;Assert-KmcChunk7Boolean $seed.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $seed.acquisitionProof $false
 $c=$o.capability;Assert-KmcExactProperties $c @('archiveNativeEnabled','nativeWriterSchema','nativePairCapacity','workingSha256','inheritedManualSha256','ownerSeedSha256') 'owner-seed capability'
 Assert-KmcChunk7Boolean $c.archiveNativeEnabled $true;Assert-KmcChunk7WorldCounters $c @('nativeWriterSchema','nativePairCapacity')
 if($c.nativeWriterSchema-ne3-or$c.nativePairCapacity-lt2-or$null-ne$c.workingSha256-or$null-ne$c.inheritedManualSha256){throw 'Owner-seed capability differs or claims Working/inherited provenance.'}
 Assert-KmcBootstrapInventoryString $c.ownerSeedSha256 $s.seedSha256
 $u=$o.ownerSeedSetup
 Assert-KmcExactProperties $u @('contract','disclosure','seedId','seedSha256','fixtureSetupOnly','acquisitionProof','producerAncestry','loadedArchiveKind','loadedArchiveCanRestore','riderFacts','nativeLevelUpAutosave','riders','events','setupAutosaves','economyBefore','nativeXpTable','admissionRefusals','eventCount','eventsTruncated','riderBExperience','assistance') 'owner-seed fixture setup disclosure'
 $table=Get-KmcChunk7StockXpTable
 if($u.nativeXpTable-isnot[Array]-or$u.nativeXpTable.Count-ne20){throw 'Owner-seed native XP table observation is missing.'}
 for($level=1;$level-le20;$level++){Assert-KmcOrdinaryInteger $u.nativeXpTable[$level-1] $table[$level] $table[$level]}
 Assert-KmcChunk7OwnerSeedEconomy $u.economyBefore 'economy before setup' $s
 Assert-KmcBootstrapInventoryString $u.contract 'owner-seed-fixture-setup-v1';Assert-KmcBootstrapInventoryString $u.disclosure $(if($manual){Get-KmcChunk7OwnerSeedManualDisclosure}else{Get-KmcChunk7OwnerSeedDisclosure})
 Assert-KmcBootstrapInventoryString $u.seedId $s.seedId;Assert-KmcBootstrapInventoryString $u.seedSha256 $s.seedSha256;Assert-KmcBootstrapInventoryString $u.loadedArchiveKind 'Missing'
 Assert-KmcChunk7Boolean $u.fixtureSetupOnly $true;foreach($key in @('acquisitionProof','producerAncestry','loadedArchiveCanRestore','eventsTruncated')){Assert-KmcChunk7Boolean $u.$key $false}
 if($u.admissionRefusals-isnot[Array]-or$u.admissionRefusals.Count-ne0){throw 'Owner-seed setup recorded an admission refusal.'}
 $a=$u.nativeLevelUpAutosave;Assert-KmcExactProperties $a @('autosaveEnabled','onlyOneSave','autosaveSlots','settingsWritten','case','riderBuilds','expectedWrites','selectedWrites','slotProjectionDuringBuild','mountedPersistenceProof') 'native level-up autosave observation'
 if($a.autosaveEnabled-isnot[bool]){throw 'Native autosave setting must be an observed boolean.'}
 foreach($key in @('onlyOneSave','settingsWritten','mountedPersistenceProof')){Assert-KmcChunk7Boolean $a.$key $false}
 if(-not(Test-KmcChunk7FiniteNumber $a.autosaveSlots)){throw 'Native autosave slot setting must be a finite observation.'}
 # The installed game has no level-up autosave (LevelUpAutoSaveController is never registered):
 # zero setup writes whatever the observed (read-only) autosave setting.
 Assert-KmcOrdinaryInteger $a.riderBuilds 2 2;$writesExpected=0
 Assert-KmcBootstrapInventoryString $a.case 'no-native-levelup-autosave'
 Assert-KmcOrdinaryInteger $a.expectedWrites $writesExpected $writesExpected;Assert-KmcOrdinaryInteger $a.selectedWrites $writesExpected $writesExpected
 # The Manual producer's one selected write is its measured Manual, never a setup write.
 if(([int]$s.expectedNativeWriteCount-$(if($manual){1}else{0}))-ne$writesExpected){throw 'Selected native write count must be zero: the native game has no level-up autosave.'}
 Assert-KmcChunk7Boolean $a.slotProjectionDuringBuild $false
 $autos=@($u.setupAutosaves);if($u.setupAutosaves-isnot[Array]-or$autos.Count-ne$a.riderBuilds){throw 'Owner-seed setup must observe the absence of a native level-up autosave once per rider build.'}
 for($i=0;$i-lt$autos.Count;$i++){
  $w=$autos[$i];$rid=if($i-eq0){$s.riderAId}else{$s.riderBId}
  Assert-KmcExactProperties $w @('sequence','riderId','case','requested','written','probe') 'owner-seed absent level-up autosave'
  Assert-KmcOrdinaryInteger $w.sequence ($i+1) ($i+1);Assert-KmcBootstrapInventoryString $w.riderId $rid;Assert-KmcBootstrapInventoryString $w.case 'no-native-levelup-autosave'
  Assert-KmcChunk7Boolean $w.requested $false;Assert-KmcChunk7Boolean $w.written $false
  # The probe was installed for the build and observed no completion entry, save request or prepared write.
  $p=$w.probe;if($null-eq$p-or$null-eq$p.PSObject.Properties['installed']-or$null-eq$p.PSObject.Properties['complete']){throw 'Owner-seed level-up probe observation is missing.'}
  Assert-KmcChunk7Boolean $p.installed $true;Assert-KmcChunk7Boolean $p.complete $false
  if($p.errors-isnot[Array]-or@($p.errors).Count-ne0-or$p.events-isnot[Array]-or@($p.events).Count-ne0){throw 'Owner-seed level-up probe recorded an error or a native event.'}
  foreach($key in @('completionEntries','completionExits','saveRequests','preparedWrites')){
   if($null-eq$p.PSObject.Properties[$key]){throw 'Owner-seed level-up probe counter is missing.'}
   if([int]$p.$key-ne0){throw 'A native level-up completion, save request or prepared write was observed during owner-seed setup; the installed game registers no LevelUpAutoSaveController.'}
  }
 }
 if($u.events-isnot[Array]-or$u.events.Count-lt2-or$u.events.Count-gt768){throw 'Owner-seed native build events are missing or unbounded.'};Assert-KmcOrdinaryInteger $u.eventCount $u.events.Count $u.events.Count
 if($u.riderFacts-isnot[Array]-or$u.riderFacts.Count-ne2-or$u.riders-isnot[Array]-or$u.riders.Count-ne2){throw 'Owner-seed setup must disclose exactly two riders.'}
 $party=@($s.partyIds);$horses=@();$roles=@()
 for($i=0;$i-lt2;$i++){
  $role=@('A','B')[$i];$id=if($i-eq0){$s.riderAId}else{$s.riderBId};$f=$u.riderFacts[$i];$r=$u.riders[$i]
  Assert-KmcExactProperties $f @('role','riderId','size','originalSize','polymorphed','controllable','petless','characterLevel','rangerLevel','experience','readyLevel','eligibleWithoutAssistance','minimalTargetLevel') 'owner-seed rider facts'
  Assert-KmcBootstrapInventoryString $f.role $role;Assert-KmcBootstrapInventoryString $f.riderId $id;Assert-KmcBootstrapInventoryString $f.size 'Medium';Assert-KmcBootstrapInventoryString $f.originalSize 'Medium'
  Assert-KmcChunk7Boolean $f.polymorphed $false;Assert-KmcChunk7Boolean $f.controllable $true;Assert-KmcChunk7Boolean $f.petless $true
  foreach($key in @('characterLevel','rangerLevel','experience','readyLevel','minimalTargetLevel')){Assert-KmcOrdinaryInteger $f.$key 0 1000000000}
  if($f.eligibleWithoutAssistance-isnot[bool]-or$f.eligibleWithoutAssistance-ne(($f.rangerLevel+$f.readyLevel-$f.characterLevel)-ge4)-or$f.minimalTargetLevel-ne($f.characterLevel+4-$f.rangerLevel)){throw 'Owner-seed eligibility arithmetic differs from the ordinary driver rule.'}
  if($i-eq0-and-not$f.eligibleWithoutAssistance){throw 'Rider A received setup without its own native eligibility.'}
  Assert-KmcExactProperties $r @('role','riderId','before','after','nativeCommits','nativeChoices','nativeAutoLevelupOverrides','horseId','horseBlueprint','horseMasterId','horseFeatureSourceGuid','presentation','acquisitionProof','profileId','rigValidated') 'owner-seed rider setup outcome'
  Assert-KmcBootstrapInventoryString $r.role $role;Assert-KmcBootstrapInventoryString $r.riderId $id;Assert-KmcBootstrapInventoryString $r.horseMasterId $id
  Assert-KmcBootstrapInventoryString $r.horseBlueprint '4016c7db400ab721ff125aef9e65e202';Assert-KmcBootstrapInventoryString $r.profileId 'medium-humanoid-horse-v1'
  Assert-KmcBootstrapInventoryString $r.presentation 'fixture-setup';Assert-KmcChunk7Boolean $r.acquisitionProof $false;Assert-KmcChunk7Boolean $r.rigValidated $true
  Assert-KmcOrdinaryGuid $r.horseId;if($r.horseId-cin$party-or$r.horseId-cin$horses){throw 'Owner-seed setup Horse aliases a party actor or the other Horse.'};$horses+=$r.horseId
  if($r.horseFeatureSourceGuid-isnot[string]-or$r.horseFeatureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Owner-seed Horse feature lacks its native progression source.'}
  Assert-KmcOrdinaryInteger $r.nativeCommits 1 20;Assert-KmcOrdinaryInteger $r.nativeChoices 1 512;Assert-KmcOrdinaryInteger $r.nativeAutoLevelupOverrides 0 20
  foreach($state in @($r.before,$r.after)){Assert-KmcExactProperties $state @('characterLevel','experience','rangerLevel','classes') 'owner-seed rider progression';foreach($key in @('characterLevel','experience','rangerLevel')){Assert-KmcOrdinaryInteger $state.$key 0 1000000000}}
  if($r.after.rangerLevel-ne4-or$r.after.characterLevel-ne($r.before.characterLevel+$r.nativeCommits)-or$r.before.characterLevel-ne$f.characterLevel){throw 'Owner-seed rider did not reach exactly Ranger 4 through exactly its native commits.'}
  # Pinned XP table: reported ready levels and the final level follow the stock thresholds; no extra levels.
  if($f.readyLevel-ne(Get-KmcChunk7XpReadyLevel $f.experience)-or$r.after.characterLevel-ne(Get-KmcChunk7XpReadyLevel $r.after.experience)-or$r.after.experience-ne$r.before.experience){throw 'Owner-seed rider levels differ from the stock XP table or exceed the XP-ready level.'}
  $roles+=@($id,$r.horseId)
 }
 $x=$u.riderBExperience;$assist=$s.xpAssistance
 if($null-eq$assist){
  Assert-KmcExactProperties $x @('assisted','experience') 'rider B unassisted experience';Assert-KmcChunk7Boolean $x.assisted $false
  if(-not$u.riderFacts[1].eligibleWithoutAssistance){throw 'Rider B needed assistance that was not selected.'}
 }else{
  Assert-KmcExactProperties $x @('assisted','targetCharacterLevel','before','after','delta','threshold','characterLevelBefore','characterLevelAfter','levelReadyAfter','readyLevelAfter') 'rider B native XP assistance'
  Assert-KmcChunk7Boolean $x.assisted $true;Assert-KmcChunk7Boolean $x.levelReadyAfter $true
  foreach($key in @('before','after','delta','threshold','characterLevelBefore','characterLevelAfter','readyLevelAfter')){Assert-KmcOrdinaryInteger $x.$key 0 1000000000}
  if($u.riderFacts[1].eligibleWithoutAssistance-or$x.targetCharacterLevel-ne$assist.targetCharacterLevel-or$assist.targetCharacterLevel-ne$u.riderFacts[1].minimalTargetLevel-or
     $x.delta-le0-or$x.after-ne($x.before+$x.delta)-or$x.after-ne$x.threshold-or$x.characterLevelAfter-ne$x.characterLevelBefore-or$x.readyLevelAfter-ne$assist.targetCharacterLevel-or
     $x.before-ne$u.riderFacts[1].experience-or$u.riders[1].before.experience-ne$x.after){throw 'Rider B XP assistance exceeds or differs from the exact minimal eligibility it requires.'}
 # Pinned stock XP table: threshold is the target level's value, 'before' lies in the current level's band.
 $target=[int]$assist.targetCharacterLevel;$cl=[int]$x.characterLevelBefore
 if($cl-lt1-or$cl-ge20-or$x.threshold-ne$table[$target]-or$x.before-lt$table[$cl]-or$x.before-ge$table[$cl+1]-or$x.delta-ne($table[$target]-$x.before)-or
    $x.after-ne$table[$target]-or$u.riders[1].after.characterLevel-ne$target-or$u.riders[1].after.rangerLevel-ne4){throw 'Rider B XP assistance differs from the stock XP table minimum or ends beyond its exact target level.'}
 }
 # Ledger order: A build, [B XP], B build (no native level-up autosave exists).
 $expected=@();$units=@();$riderOf=@()
 foreach($step in @(@('native-ordinary-ranger-levels',$s.riderAId,0),@('native-xp',$s.riderBId,1),@('native-ordinary-ranger-levels',$s.riderBId,1))){
  if($step[0]-ceq'native-xp'-and$null-eq$assist){continue}
  $expected+=$step[0];$units+=$step[1];$riderOf+=$step[2]
 }
 $items=@($u.assistance)
 if($u.assistance-isnot[Array]-or$items.Count-ne$expected.Count){throw 'Owner-seed assistance disclosure is incomplete or contains an undeclared setup item.'}
 for($i=0;$i-lt$items.Count;$i++){
  $v=$items[$i];Assert-KmcExactProperties $v @('unitId','kind','nativeApi','before','after','amount','detail') 'owner-seed assistance item'
  Assert-KmcBootstrapInventoryString $v.kind $expected[$i];Assert-KmcBootstrapInventoryString $v.unitId $units[$i]
  if($v.nativeApi-isnot[string]-or[string]::IsNullOrWhiteSpace($v.nativeApi)-or$v.detail-isnot[string]){throw 'Owner-seed assistance must name its native API.'}
  foreach($key in @('before','after','amount')){Assert-KmcOrdinaryInteger $v.$key 0 1000000000}
  if($v.kind-ceq'native-xp'){if($v.nativeApi-cne'Kingmaker.UnitLogic.UnitProgressionData.GainExperience(int, log: false)'-or$v.before-ne$x.before-or$v.after-ne$x.after-or$v.amount-ne$x.delta){throw 'Owner-seed XP assistance item differs from its observation.'}}
  elseif($v.kind-ceq'native-levelup-autosave'){$w=$autos[$riderOf[$i]];if($v.nativeApi-cne'Kingmaker.Controllers.LevelUpAutoSaveController.HandleLevelUpComplete -> Game.SaveGame(Auto) into the run-owned isolated save root'-or$v.before-ne$riderOf[$i]-or$v.after-ne($riderOf[$i]+1)-or$v.amount-ne1-or$v.detail-cne$w.path){throw 'Owner-seed level-up Auto assistance item differs from its observed write.'}}
  else{$row=$u.riders[$riderOf[$i]];if($v.before-ne$row.before.characterLevel-or$v.after-ne$row.after.characterLevel-or$v.amount-ne$row.nativeCommits){throw 'Owner-seed build assistance item differs from its rider outcome.'}}
 }
 $b=$o.setupComplete;$boundaryFields=@('boundary','frame','gameTicks','assistanceCount','setupSeconds','measuredClockRestarted','state','writesAtBoundary','writeScopeClosed','economy','horseItems');if($manual){$boundaryFields+='measuredOutputReservation'}
 Assert-KmcExactProperties $b $boundaryFields 'SETUP-COMPLETE boundary'
 if($manual){
  # The sole measured Manual is reserved, unwritten, at SETUP-COMPLETE.
  $m=$b.measuredOutputReservation;Assert-KmcExactProperties $m @('leaf','nativeName','nativeType','number','reservationCount','unwritten') 'owner-seed measured Manual reservation'
  Assert-KmcBootstrapInventoryString $m.nativeName (Get-KmcChunk7OwnerSeedManualName);Assert-KmcBootstrapInventoryString $m.nativeType 'Manual'
  Assert-KmcOrdinaryInteger $m.number 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $m.reservationCount 1 1;Assert-KmcChunk7Boolean $m.unwritten $true
  Assert-KmcBootstrapInventoryString $m.leaf ('Manual_'+([int]$m.number).ToString([Globalization.CultureInfo]::InvariantCulture)+'_'+(Get-KmcChunk7OwnerSeedManualName)+'.zks')
 }
 Assert-KmcChunk7OwnerSeedEconomy $b.economy 'economy at SETUP-COMPLETE' $s
 if($b.state-eq$null-or[long]$b.economy.money-ne[long]$b.state.money){throw 'Owner-seed boundary economy money differs from the boundary state.'}
 Assert-KmcChunk7OwnerSeedHorseItems $b.horseItems @($u.riders[0].horseId,$u.riders[1].horseId)
 if((ConvertTo-KmcChunk7Canonical $b.economy)-cne(ConvertTo-KmcChunk7Canonical $u.economyBefore)){throw 'Owner-seed setup changed party money or the party item inventory.'}
 # SETUP-COMPLETE precedes the first measured observation.
 $firstMeasured=@($o.measurements)[0];$firstInput=@($o.inputs)[0]
 if($null-eq$firstMeasured-or$null-eq$firstInput-or[long]$b.frame-gt[long]$firstMeasured.startFrame-or[long]$b.gameTicks-gt[long]$firstInput.resourcesBefore.gameTicks){throw 'SETUP-COMPLETE does not precede the measured phase.'}
 Assert-KmcOrdinaryInteger $b.writesAtBoundary $writesExpected $writesExpected;Assert-KmcChunk7Boolean $b.writeScopeClosed $true
 Assert-KmcBootstrapInventoryString $b.boundary 'SETUP-COMPLETE';Assert-KmcOrdinaryInteger $b.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $b.gameTicks 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $b.assistanceCount $items.Count $items.Count
 Assert-KmcChunk7Boolean $b.measuredClockRestarted $true;if(-not(Test-KmcChunk7FiniteNumber $b.setupSeconds)-or$b.setupSeconds-lt0){throw 'Owner-seed setup duration is not a finite observation.'}
 if($b.state-eq$null-or$b.state.pairs-isnot[Array]-or$b.state.pairs.Count-ne2){throw 'SETUP-COMPLETE lacks its captured setup state.'}
 for($i=0;$i-lt2;$i++){$p=$b.state.pairs[$i];if($p.riderId-cne$roles[$i*2]-or$p.petId-cne$roles[$i*2+1]-or$p.petMasterId-cne$roles[$i*2]){throw 'SETUP-COMPLETE state differs from the disclosed setup pairs.'}}
 Assert-KmcChunk7ExactIds @($o.partyBefore) $party
 foreach($id in @($s.riderAId,$s.riderBId)){$row=@($o.partyBefore|Where-Object id -CEQ $id);if($row.Count-ne1-or$null-ne$row[0].petId){throw 'Owner-seed rider was not petless before setup.'}}
 Assert-KmcChunk7ExactIds @($o.partyWithHorse) @($party+$horses)
 for($i=0;$i-lt2;$i++){$rider=@($o.partyWithHorse|Where-Object id -CEQ $roles[$i*2]);$horse=@($o.partyWithHorse|Where-Object id -CEQ $roles[$i*2+1]);if($rider.Count-ne1-or$horse.Count-ne1-or$rider[0].petId-cne$horse[0].id-or$horse[0].masterId-cne$rider[0].id-or$horse[0].blueprint-cne'4016c7db400ab721ff125aef9e65e202'){throw 'Owner-seed setup Horse reciprocity differs.'}}
 return ,@($roles)
}
function Assert-KmcChunk7DoorNotApplicable($Door){
 Assert-KmcExactProperties $Door @('applicable','reason','candidatesScanned','radius') 'not-applicable native door'
 Assert-KmcChunk7Boolean $Door.applicable $false
 if($Door.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($Door.reason)-or$Door.reason.Length-gt512){throw 'Not-applicable door lacks its observed reason.'}
 Assert-KmcOrdinaryInteger $Door.candidatesScanned 0 100000
 if(-not(Test-KmcChunk7FiniteNumber $Door.radius)-or[double]$Door.radius-ne28.0){throw 'Not-applicable door scan radius differs from the exact probe radius.'}
}
function Assert-KmcChunk7OwnerSeedCleanup($Cleanup,[bool]$Manual=$false){
 foreach($field in @('pairResidueAbsent','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled','setupCompleted','ownerSeedSetupPetsPreserved','setupStateUnchangedAfterBoundary','setupRetainedInMemoryOnly')){Assert-KmcChunk7Boolean $Cleanup.$field $true}
 foreach($field in @('horseRemoved','inheritedPetsPreserved','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven')){Assert-KmcChunk7Boolean $Cleanup.$field $false}
 Assert-KmcChunk7Boolean $Cleanup.nativeSaveWritten $Manual
 Assert-KmcOrdinaryInteger $Cleanup.setupMutationsAfterBoundary 0 0
 # Exploration: zero native writes after SETUP-COMPLETE. Manual producer: exactly its one measured
 # Manual. None unauthorized; the owned projection released.
 $after=if($Manual){1}else{0};Assert-KmcOrdinaryInteger $Cleanup.writesAfterBoundary $after $after;Assert-KmcOrdinaryInteger $Cleanup.unauthorizedWrites 0 0;Assert-KmcChunk7Boolean $Cleanup.slotProjectionReleased $true
}
# Runner final-root check for the Manual producer: exactly the immutable seed copy and its one
# fresh owner-seed-derived Manual remain.
function Assert-KmcChunk7OwnerSeedManualFinalSaveRoot($Request,[string[]]$Leaves,[string]$OutputLeaf){
 $all=@($Leaves|Sort-Object -CaseSensitive);$expected=@(@($Request.persistenceLoad.fileName,$OutputLeaf)|Sort-Object -CaseSensitive)
 if(-not(Test-KmcChunk7OwnedPairsSeedManual $Request)-or(Get-KmcChunk7OwnedWriteCount $Request)-ne1-or[string]::IsNullOrEmpty($OutputLeaf)-or$OutputLeaf-ceq$Request.persistenceLoad.fileName-or($all-join'|')-cne($expected-join'|')){throw 'Owner-seed Manual save root must contain only the seed copy and its one fresh owner-seed-derived Manual.'}
}
# Runner final-root check: owner-seed setup makes zero writes (the installed game registers
# no LevelUpAutoSaveController), so only the immutable seed copy may remain: no Auto_1.
function Assert-KmcChunk7OwnerSeedFinalSaveRoot($Request,[string[]]$Leaves){
 $all=@($Leaves);if((Get-KmcChunk7OwnedWriteCount $Request)-ne0-or$all.Count-ne1-or$all[0]-cne$Request.persistenceLoad.fileName){throw 'Owner-seed isolated save root must contain only the seed copy (no Auto_1): the native game has no level-up autosave.'}
}
# Owner-seed-derived Manual producer evidence. The disclosed setup and two native Mounts reuse the
# owner-seed shape; after SETUP-COMPLETE exactly one native Manual was requested, prepared, staged
# by the KMC serialization barrier, written by the worker and completed into the run root. Its read-
# back schema-3 graph must match the live runtime owners, generations, profiles, control slots and
# actor expenditure at the save boundary. Ancestry is owner-seed-derived with disclosed assistance.
function Get-KmcChunk7OwnerSeedManualFields {@('contract','ancestry','disclosedAssistance','assistanceCount','acquisitionProof','ordinaryProducerAncestry','levelUpSave','restorationProof','setupCompletedBeforeSave','setupMutationsAfterBoundary','seedId','seedSha256','seedCopyUnchanged','processId','boundary','hotbar','headerStarting','headerStaged','workerObservedRunning','completionSequence','fieldWorld','twoOwners','arrangement','arrangementAfterSave')}
function Test-KmcChunk7DebtWithinClock($Before,$After,[double]$Elapsed){
 if($Before.Id-cne$After.Id-or$Before.ReactionsRemaining-ne$After.ReactionsRemaining-or$Before.LastSurpriseTicks-ne$After.LastSurpriseTicks){return $false}
 $slack=0.001+$Elapsed*0.1
 foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){$a=[double]$Before.$field;$b=[double]$After.$field;if($b-gt$a+0.001-or$b+$Elapsed+$slack-lt$a){return $false}}
 return $true
}
function Assert-KmcChunk7OwnerSeedManualArchive($Request,$Archive,[string[]]$Roles){
 Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'owner-seed Manual native archive'
 $d=$Request.persistenceLoad
 if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne$d.gameId-or$Archive.AreaId-cne$d.area-or
    -not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or$Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or
    $Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne2-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$null-ne$Archive.Combat-or
    $Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne4){throw 'Owner-seed Manual read-back omitted a pair, invented combat/retained state or changed version/policy/slots.'}
 $owners=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 for($i=0;$i-lt2;$i++){
  $pair=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Roles[$i*2]});if($pair.Count-ne1){throw 'Owner-seed Manual read-back substituted a rider role.'};$p=$pair[0]
  Assert-KmcExactProperties $p @('PairId','Generation','ProfileId','Rider','Mount') 'owner-seed Manual pair'
  $owner=[Guid]::Empty;if(-not[Guid]::TryParse([string]$p.PairId,[ref]$owner)-or$owner-eq[Guid]::Empty-or-not$owners.Add($owner.ToString('N'))-or-not(Test-KmcExactJsonInteger $p.Generation)-or$p.Generation-le0){throw 'Owner-seed Manual owner identity or generation invalid.'}
  if($p.Mount.Id-cne$Roles[$i*2+1]-or$p.ProfileId-cne'medium-humanoid-horse-v1'){throw 'Owner-seed Manual read-back substituted a mount role or profile.'}
  foreach($actor in @($p.Rider,$p.Mount)){Assert-KmcChunk7PersistenceNativeActor $actor}
 }
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($slot in $Archive.Slots){
  Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'owner-seed Manual slot'
  if($slot.ActorId-cnotin@($Roles[0],$Roles[2])-or-not(Test-KmcExactJsonInteger $slot.Index)-or$slot.Index-lt0-or$slot.Index-gt127-or-not(Test-KmcExactJsonInteger $slot.Kind)-or$slot.Kind-cnotin@(2,3)-or-not$seen.Add($slot.ActorId+':'+$slot.Index)){throw 'Owner-seed Manual control slot invalid or duplicated.'}
 }
 foreach($rider in @($Roles[0],$Roles[2])){if((@($Archive.Slots|Where-Object ActorId -CEQ $rider|ForEach-Object{$_.Kind}|Sort-Object)-join',')-cne'2,3'){throw 'Each owner-seed pair must retain its exact two native controls.'}}
}
function Assert-KmcChunk7OwnerSeedManualWorldEvidence($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsSeedManual $Request)){throw 'Owner-seed Manual evidence belongs only to its producer kind.'}
 $o=$Observations;$roles=[string[]](Assert-KmcChunk7OwnerSeedWorldSetup $Request $o);$actors=@($roles);$s=$Request.ownedPairs
 $original=@(@($s.partyIds)+@($roles[1],$roles[3]))
 foreach($snapshot in @($o.snapshots)){foreach($owner in $snapshot.owners){Assert-KmcChunk7WorldCounters $owner @('generation')}}
 # Only the two native Mounts precede the save: no move, door, cycle or matched-pool step.
 foreach($key in @('movements','groundInputs','cycleInputs','cycles')){$step=$o.PSObject.Properties[$key];if($null-eq$step-or$step.Value-isnot[Array]-or@($step.Value).Count-ne0){throw ('Owner-seed Manual producer carries an undeclared '+$key+' step.')}}
 if($null-ne$o.PSObject.Properties['door']){throw 'Owner-seed Manual producer carries an undeclared door step.'}
 $specs=@(@('mount-mammoth',$roles[0],$roles[1],'f053faad986631688defa003cd7bda0e'),@('mount-horse',$roles[2],$roles[3],'f053faad986631688defa003cd7bda0e'))
 $commands=New-Object 'Collections.Generic.HashSet[long]';$shells=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
 Assert-KmcChunk7InputSequence $o.inputs $specs $actors $commands $shells
 if(@($o.measurements).Count-ne2-or$o.measurements[0].label-cne'unmounted-0'-or$o.measurements[1].label-cne'mounted-1'){throw 'Owner-seed Manual producer measurement windows differ.'}
 $both=@($o.snapshots|Where-Object label -CEQ 'both-mounted');$complete=@($o.snapshots|Where-Object label -CEQ 'native-manual-complete')
 if($both.Count-ne1-or$complete.Count-ne1){throw 'Owner-seed Manual both-mounted or completion window missing.'}
 $owners=@($both[0].owners|Where-Object state -CEQ 'Active')
 if($owners.Count-ne2-or$owners[0].ownerId-ceq$owners[1].ownerId){throw 'Two distinct active native owners required.'}
 Assert-KmcChunk7ExactIds $owners @($roles[0],$roles[2]) 'riderId' -GuidValue;Assert-KmcChunk7ExactIds $owners @($roles[1],$roles[3]) 'mountId' -GuidValue
 foreach($owner in $owners){$same=@($complete[0].owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)-ceq(ConvertTo-KmcChunk7PairIdN $owner.ownerId)});if($same.Count-ne1-or$same[0].generation-ne$owner.generation-or$same[0].state-cne'Active'-or$same[0].relationshipState-cne'Mounted'){throw 'The native Manual changed live pair ownership or generation.'}}
 $q=$o.saveRequested;Assert-KmcExactProperties $q @('nativeType','nativeName','frame','gameTicks','snapshotCount','nativeSaveAllowed','newSlot','levelUpSave','api') 'owner-seed native Manual request'
 Assert-KmcBootstrapInventoryString $q.nativeType 'Manual';Assert-KmcBootstrapInventoryString $q.nativeName (Get-KmcChunk7OwnerSeedManualName);Assert-KmcOrdinaryInteger $q.snapshotCount 0 0
 foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $q.$key $true};Assert-KmcChunk7Boolean $q.levelUpSave $false
 Assert-KmcBootstrapInventoryString $q.api 'Game.SaveGame(SaveManager.CreateNewSave(name))'
 Assert-KmcChunk7OwnedOutputEvidence $Request $o
 $leaf=Get-KmcChunk7OwnedOutputLeaf $Request $o;$w=$o.nativeWrite;$d=$Request.persistenceLoad
 Assert-KmcExactProperties $w @('path','sha256','length','lastWriteTimeUtcTicks','fileName','nativeType','nativeName','gameId','gameName','area','nativeCallback','operation','snapshotCount','readKind','sourceSchema','archive') 'owner-seed native Manual write'
 if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[a-f0-9]{64}$'-or$w.sha256-cin@($d.sha256,$s.seedSha256)){throw 'Owner-seed Manual write hash is malformed or aliases the immutable seed.'}
 Assert-KmcOrdinaryInteger $w.length 1 268435456;Assert-KmcOrdinaryInteger $w.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
 foreach($pair in @(@('fileName',$leaf),@('gameId',$d.gameId),@('gameName',$d.gameName),@('area',$d.area),@('operation','None'),@('readKind','Current'))){Assert-KmcBootstrapInventoryString $w.($pair[0]) $pair[1]}
 Assert-KmcChunk7Boolean $w.nativeCallback $true;Assert-KmcOrdinaryInteger $w.snapshotCount 1 1;Assert-KmcOrdinaryInteger $w.sourceSchema 3 3
 Assert-KmcChunk7OwnerSeedManualArchive $Request $w.archive $roles
 $m=$o.ownerSeedManual;Assert-KmcExactProperties $m (Get-KmcChunk7OwnerSeedManualFields) 'owner-seed-derived Manual receipt'
 Assert-KmcBootstrapInventoryString $m.contract 'owner-seed-derived-manual-v1';Assert-KmcBootstrapInventoryString $m.ancestry 'owner-seed-derived'
 foreach($key in @('disclosedAssistance','setupCompletedBeforeSave','seedCopyUnchanged')){Assert-KmcChunk7Boolean $m.$key $true}
 foreach($key in @('acquisitionProof','ordinaryProducerAncestry','levelUpSave','restorationProof')){Assert-KmcChunk7Boolean $m.$key $false}
 $items=@($o.ownerSeedSetup.assistance).Count;Assert-KmcOrdinaryInteger $m.assistanceCount $items $items;Assert-KmcOrdinaryInteger $m.setupMutationsAfterBoundary 0 0
 Assert-KmcBootstrapInventoryString $m.seedId $s.seedId;Assert-KmcBootstrapInventoryString $m.seedSha256 $s.seedSha256
 Assert-KmcOrdinaryInteger $m.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $m.completionSequence 1 1
 if($m.workerObservedRunning-isnot[bool]){throw 'Owner-seed native worker observation must be an observed boolean.'}
 foreach($h in @(@($m.headerStarting,'native-header-starting',0),@($m.headerStaged,'native-header-staged',1))){
  $v=$h[0];Assert-KmcExactProperties $v @('label','frame','requestedIsSelectedDescriptor','preparedIsCopy','preparedLeaf','preparedName','preparedType','preparedPath','snapshots','activeOwners','nativeWrites','serializationBusy') 'owner-seed native Manual header'
  Assert-KmcBootstrapInventoryString $v.label $h[1];Assert-KmcBootstrapInventoryString $v.preparedLeaf $leaf;Assert-KmcBootstrapInventoryString $v.preparedName (Get-KmcChunk7OwnerSeedManualName);Assert-KmcBootstrapInventoryString $v.preparedType 'Manual'
  Assert-KmcBootstrapInventoryString $v.preparedPath $w.path;Assert-KmcOrdinaryInteger $v.snapshots $h[2] $h[2];Assert-KmcOrdinaryInteger $v.activeOwners 2 2;Assert-KmcOrdinaryInteger $v.nativeWrites 0 1
  foreach($key in @('requestedIsSelectedDescriptor','preparedIsCopy')){Assert-KmcChunk7Boolean $v.$key $true};if($v.serializationBusy-isnot[bool]){throw 'Native header serialization state must be an observed boolean.'}
 }
 # The two pair records correspond to the actual runtime at the save boundary.
 $b=$m.boundary;Assert-KmcExactProperties $b @('label','frame','gameTicks','actorIds','pairs','slots','actors','unrelated','rosterIds','arrangement') 'owner-seed Manual save boundary'
 Assert-KmcBootstrapInventoryString $b.label 'save-boundary';Assert-KmcOrdinaryInteger $b.gameTicks 0 ([long]::MaxValue)
 # The measured save follows SETUP-COMPLETE: frames and game time, not only the setupCompletedBeforeSave flag.
 $setup=$o.setupComplete
 if($null-eq$setup-or[long]$setup.frame-ge[long]$b.frame-or[long]$b.frame-gt[long]$q.frame-or[long]$q.frame-gt[long]$m.headerStarting.frame-or
    [long]$m.headerStarting.frame-gt[long]$m.headerStaged.frame-or[long]$setup.gameTicks-gt[long]$b.gameTicks-or[long]$b.gameTicks-gt[long]$q.gameTicks){throw 'The owner-seed Manual save does not follow SETUP-COMPLETE in frame and game-time order.'}
 if((ConvertTo-KmcChunk7Canonical @($b.actorIds))-cne(ConvertTo-KmcChunk7Canonical @($roles))){throw 'Owner-seed Manual boundary actors differ from the four setup roles.'}
 if(@($b.pairs).Count-ne2){throw 'Owner-seed Manual boundary requires both pair owners.'}
 for($i=0;$i-lt2;$i++){
  $row=$b.pairs[$i];Assert-KmcExactProperties $row @('ownerId','generation','riderId','mountId','profileId','state','relationshipState') 'owner-seed Manual boundary owner'
  $live=@($owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)-ceq(ConvertTo-KmcChunk7PairIdN $row.ownerId)});$saved=@($w.archive.Pairs|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.PairId)-ceq(ConvertTo-KmcChunk7PairIdN $row.ownerId)})
  if($row.riderId-cne$roles[$i*2]-or$row.mountId-cne$roles[$i*2+1]-or$row.profileId-cne'medium-humanoid-horse-v1'-or$row.state-cne'Active'-or$row.relationshipState-cne'Mounted'-or
     $live.Count-ne1-or$live[0].generation-ne$row.generation-or$saved.Count-ne1-or$saved[0].Generation-ne$row.generation-or$saved[0].Rider.Id-cne$row.riderId-or$saved[0].Mount.Id-cne$row.mountId){throw 'Owner-seed Manual read-back pair records differ from the live runtime owners, generations or actors.'}
 }
 if(@($b.slots).Count-ne4){throw 'Owner-seed Manual boundary requires its four bound controls.'}
 foreach($slot in $b.slots){if(@($w.archive.Slots|Where-Object{$_.ActorId-ceq$slot.actorId-and$_.Index-eq$slot.index-and$_.Kind-eq$slot.kind}).Count-ne1){throw 'Owner-seed Manual read-back slots differ from the bound native hotbar.'}}
 if(@($m.hotbar).Count-ne4){throw 'Owner-seed Manual hotbar binding must disclose its four controls.'}
 foreach($h in $m.hotbar){Assert-KmcExactProperties $h @('actorId','abilityGuid','index','nativeApi','afterSetupComplete') 'owner-seed hotbar binding';Assert-KmcChunk7Boolean $h.afterSetupComplete $true;if(@($b.slots|Where-Object{$_.actorId-ceq$h.actorId-and$_.index-eq$h.index}).Count-ne1){throw 'Owner-seed hotbar binding differs from the captured control slots.'}}
 Assert-KmcChunk7ExactIds @($b.actors) @($roles) 'Id'
 $elapsed=([double]$w.archive.GameTimeTicks-[double]$b.gameTicks)/10000000.0
 if($elapsed-lt0-or$elapsed-gt30){throw 'Owner-seed Manual snapshot escaped its bounded save-boundary window.'}
 foreach($actor in @($w.archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){$before=@($b.actors|Where-Object Id -CEQ $actor.Id);Assert-KmcChunk7PersistenceNativeActor $before[0];if(-not(Test-KmcChunk7DebtWithinClock $before[0] $actor $elapsed)){throw 'Owner-seed Manual read-back expenditure differs from the live runtime.'}}
 $unrelated=@($original|Where-Object{$_-cnotin$roles});Assert-KmcChunk7ExactIds @($b.unrelated) $unrelated 'actorId'
 foreach($row in $b.unrelated){Assert-KmcExactProperties $row @('actorId','native') 'owner-seed unrelated actor';Assert-KmcChunk7PersistenceNativeActor $row.native;Assert-KmcBootstrapInventoryString $row.native.Id $row.actorId}
 if((ConvertTo-KmcChunk7Canonical @($b.rosterIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($original|Sort-Object -CaseSensitive))){throw 'Owner-seed Manual roster differs from the setup field.'}
 $f=$m.fieldWorld;Assert-KmcExactProperties $f @('gameId','gameName','area','mainActorId','party') 'owner-seed Manual field world'
 foreach($pair in @(@('gameId',$d.gameId),@('gameName',$d.gameName),@('area',$d.area))){Assert-KmcBootstrapInventoryString $f.($pair[0]) $pair[1]}
 if($f.mainActorId-cnotin@($s.partyIds)){throw 'Owner-seed Manual main actor is outside the selected party.'};Assert-KmcChunk7ExactIds @($f.party) $original
 if(@($m.twoOwners).Count-ne2){throw 'Owner-seed Manual requires its two owner rows.'}
 for($i=0;$i-lt2;$i++){
  $r=$m.twoOwners[$i];Assert-KmcExactProperties $r @('actorId','petId','sourceGuid','featureGuid','rank','profileId','presentation','acquisitionProof') 'owner-seed Manual owner'
  if($r.actorId-cne$roles[$i*2]-or$r.petId-cne$roles[$i*2+1]-or$r.sourceGuid-cne$o.ownerSeedSetup.riders[$i].horseFeatureSourceGuid-or$r.profileId-cne'medium-humanoid-horse-v1'-or$r.presentation-cne'fixture-setup'-or
     $r.featureGuid-isnot[string]-or$r.featureGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Owner-seed Manual owner row differs from its disclosed setup pair.'}
  Assert-KmcOrdinaryInteger $r.rank 1 20;Assert-KmcChunk7Boolean $r.acquisitionProof $false
 }
 if((ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $m.arrangementAfterSave)-or(ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $b.arrangement)){throw 'Owner-seed Manual changed native class/XP/money state.'}
 Assert-KmcChunk7ExactIds @($m.arrangement.actors) $original 'actorId'
 $admission=$o.factRemovalAdmission
 foreach($field in @('owners','activePairs','frame','gameTicks')){if(-not(Test-KmcExactJsonInteger $admission.$field)-or$admission.$field-lt0){throw 'Fact-removal admission counter is missing or mistyped.'}}
 if($admission.owners-ne0-or$admission.activePairs-ne0){throw 'Owner-seed Manual cleanup admitted retained ownership.'}
 Assert-KmcChunk7ExactIds $admission.commands @(@($o.inputs)|ForEach-Object{[string]$_.commandObject}) 'commandObject'
 foreach($command in $admission.commands){Assert-KmcChunk7Boolean $command.finished $true;Assert-KmcChunk7Boolean $command.processEnded $true}
 $c=$o.cleanup;Assert-KmcChunk7OwnerSeedCleanup $c $true
 foreach($field in @('combatOrPersistenceProof','thirdUnrelatedActorProven')){Assert-KmcChunk7Boolean $o.$field $false}
 Assert-KmcChunk7ExactIds @($c.partyAfter) $original
 if(($c.selectionBefore-join'|')-cne($c.selectionAfter-join'|')-or$c.pauseBefore-ne$c.pauseAfter-or$c.movementBefore-ne$c.movementAfter-or$c.pairedBefore-ne$c.pairedAfter){throw 'Settings/selection restoration differs.'}
 if(@($c.world.owners|Where-Object state -CNE 'Retired').Count-ne0-or@($c.pairBoundaries).Count-lt2){throw 'World cleanup left retained ownership.'}
 foreach($pair in $c.pairBoundaries){if($pair.state-cne'Unmounted'-or$pair.residueAbsent-ne$true-or$pair.ownerPresent-ne$false){throw 'Pair cleanup incomplete.'}}
}
# Owner-seed-derived cold observation: the separate process compared its restored world with the
# producer runtime it was pinned to (graph, owners/generations, slots, unrelated expenditure).
function Assert-KmcChunk7OwnerSeedDerivedColdObservation($Request,$Observations,$Archive){
 if(-not((Test-KmcChunk7OwnedPairsSeedDerivedLoad $Request)-or(Test-KmcChunk7LoadRefusal $Request))){throw 'Owner-seed-derived cold proof belongs only to its exact consumer or refusal kind.'}
 if($null-eq$Observations.PSObject.Properties['ownerSeedDerivedCold']){throw 'Owner-seed-derived cold load lacks its comparison with the pinned producer runtime.'}
 $c=$Observations.ownerSeedDerivedCold;$s=$Request.ownedPairs
 Assert-KmcExactProperties $c @('contract','ancestry','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedSha256','producerProcessId','loadedEqualsProducerReadback','elapsedSeconds','owners','slots','unrelated') 'owner-seed-derived cold proof'
 Assert-KmcBootstrapInventoryString $c.contract 'owner-seed-derived-cold-v1';Assert-KmcBootstrapInventoryString $c.ancestry 'owner-seed-derived'
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','seedSha256')){Assert-KmcBootstrapInventoryString $c.$key $s.$key}
 Assert-KmcOrdinaryInteger $c.producerProcessId 1 ([int]::MaxValue);if($c.producerProcessId-eq$Observations.processId){throw 'A same-process world cannot prove owner-seed-derived cold restoration.'}
 Assert-KmcChunk7Boolean $c.loadedEqualsProducerReadback $true
 if(-not(Test-KmcChunk7FiniteNumber $c.elapsedSeconds)-or$c.elapsedSeconds-lt0){throw 'Owner-seed-derived cold clock observation is invalid.'}
 if(@($c.owners).Count-ne2){throw 'Owner-seed-derived cold proof requires both restored owners.'}
 foreach($pair in $Archive.Pairs){$row=@($c.owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)-ceq(ConvertTo-KmcChunk7PairIdN $pair.PairId)});if($row.Count-ne1-or$row[0].generation-ne$pair.Generation-or$row[0].riderId-cne$pair.Rider.Id-or$row[0].mountId-cne$pair.Mount.Id){throw 'Restored owner identity or generation differs from the restored archive.'}}
 if(@($c.slots).Count-ne@($Archive.Slots).Count){throw 'Restored control bindings differ from the restored archive.'}
 foreach($slot in $c.slots){if(@($Archive.Slots|Where-Object{$_.ActorId-ceq$slot.actorId-and$_.Index-eq$slot.index-and$_.Kind-eq$slot.kind}).Count-ne1){throw 'Restored control bindings differ from the restored archive.'}}
 $roles=Get-KmcChunk7OwnedPairIds $Request;Assert-KmcChunk7ExactIds @($c.unrelated) @($s.partyIds|Where-Object{$_-cnotin$roles}) 'actorId'
 foreach($row in $c.unrelated){Assert-KmcExactProperties $row @('actorId','before','after') 'owner-seed-derived unrelated actor';Assert-KmcChunk7PersistenceNativeActor $row.before;Assert-KmcChunk7PersistenceNativeActor $row.after
  Assert-KmcBootstrapInventoryString $row.before.Id $row.actorId;if(-not(Test-KmcChunk7DebtWithinClock $row.before $row.after ([double]$c.elapsedSeconds))){throw 'Unrelated native actor expenditure changed across the owner-seed-derived cold load.'}}
}
# Runner post-run comparison with the exact pinned producer receipt.
function Assert-KmcChunk7OwnerSeedDerivedColdSource($Request,$ProducerArtifact,$ColdArtifact){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsSeedDerivedLoad $Request)){throw 'Owner-seed-derived cold source belongs only to its consumer kind.'}
 $s=$Request.ownedPairs;$p=$ProducerArtifact.observations;$c=$ColdArtifact.observations;$m=$p.ownerSeedManual
 if($Request.persistenceCase-ceq'multi-exploration-three-loads-manual'){
  Assert-KmcChunk7RepeatedWorldObservation $Request $ColdArtifact
  Assert-KmcBootstrapInventoryString $ProducerArtifact.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $ProducerArtifact.status 'PASS'
  Assert-KmcBootstrapInventoryString $m.ancestry 'owner-seed-derived'
  Assert-KmcBootstrapInventoryString $m.seedId $s.seedId;Assert-KmcBootstrapInventoryString $m.seedSha256 $s.seedSha256
  Assert-KmcBootstrapInventoryString $p.nativeWrite.sha256 $s.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $Request.persistenceLoad.sha256 $s.sourceArchiveSha256
  Assert-KmcOrdinaryInteger $m.processId 1 ([int]::MaxValue)
  if($m.processId-eq$c.processId-or(ConvertTo-KmcChunk7Canonical $p.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){throw 'Repeated owner-seed loads substituted the original native Manual or process.'}
  if((ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedFixture.arrangement)-or(ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedCleanup.arrangement)){throw 'Repeated owner-seed loads changed the producer class/XP/money arrangement.'}
  if((ConvertTo-KmcChunk7Canonical @($m.fieldWorld.party|ForEach-Object{$_.id}|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($s.partyIds|Sort-Object -CaseSensitive))){throw 'Repeated owner-seed field roster differs from the original producer field.'}
  return
 }
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $ProducerArtifact.$key $Request.$key}
 Assert-KmcBootstrapInventoryString $ProducerArtifact.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $ProducerArtifact.status 'PASS';Assert-KmcBootstrapInventoryString $ColdArtifact.status 'PASS'
 Assert-KmcBootstrapInventoryString $m.ancestry 'owner-seed-derived';Assert-KmcBootstrapInventoryString $m.seedSha256 $s.seedSha256;Assert-KmcBootstrapInventoryString $m.seedId $s.seedId
 Assert-KmcBootstrapInventoryString $p.nativeWrite.sha256 $s.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $Request.persistenceLoad.sha256 $s.sourceArchiveSha256
 if($m.processId-eq$c.processId-or$c.ownerSeedDerivedCold.producerProcessId-ne$m.processId){throw 'Owner-seed-derived cold proof did not run in a process separate from its producer.'}
 if((ConvertTo-KmcChunk7Canonical $p.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){throw 'Cold process did not restore the exact producer read-back graph.'}
 $produced=@($m.boundary.pairs|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)+'|'+$_.generation+'|'+$_.riderId+'|'+$_.mountId}|Sort-Object -CaseSensitive)
 $restored=@($c.ownerSeedDerivedCold.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)+'|'+$_.generation+'|'+$_.riderId+'|'+$_.mountId}|Sort-Object -CaseSensitive)
 if(($produced-join';')-cne($restored-join';')){throw 'Restored owners or generations differ from the producer runtime.'}
 $producedSlots=@($m.boundary.slots|ForEach-Object{$_.actorId+':'+$_.index+':'+$_.kind}|Sort-Object -CaseSensitive);$restoredSlots=@($c.ownerSeedDerivedCold.slots|ForEach-Object{$_.actorId+':'+$_.index+':'+$_.kind}|Sort-Object -CaseSensitive)
 if(($producedSlots-join';')-cne($restoredSlots-join';')){throw 'Restored control bindings differ from the producer runtime.'}
 foreach($row in $c.ownerSeedDerivedCold.unrelated){$before=@($m.boundary.unrelated|Where-Object actorId -CEQ $row.actorId);if($before.Count-ne1-or(ConvertTo-KmcChunk7Canonical $before[0].native)-cne(ConvertTo-KmcChunk7Canonical $row.before)){throw 'Cold unrelated-actor baseline differs from the producer runtime.'}}
 if(@($c.ownerSeedDerivedCold.unrelated).Count-ne@($m.boundary.unrelated).Count){throw 'Cold unrelated-actor custody differs from the producer runtime.'}
 if((ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedFixture.arrangement)-or(ConvertTo-KmcChunk7Canonical $m.arrangement)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedCleanup.arrangement)){throw 'Cold process changed the producer class/XP/money arrangement.'}
 if((ConvertTo-KmcChunk7Canonical @($m.fieldWorld.party|ForEach-Object{$_.id}|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($s.partyIds|Sort-Object -CaseSensitive))){throw 'Consumer field roster differs from the producer field.'}
}
# Owner-seed-derived combat selection (scenarios 3-5): the producer and seed pins of the cold
# consumer plus the ACCEPTED consumer receipt (checkpointSource*) and, for RT only, the companion.
function Assert-KmcChunk7OwnerSeedDerivedCombatSelection($Request,$Selection,$Checkpoint){
 $s=$Selection;$rt=$s.case-ceq'owner-seed-realtime-core'
 $fields=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','checkpointSourceRunId','checkpointSourceArtifactSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount');if($rt){$fields+='companionId'}
 Assert-KmcExactProperties $s $fields 'owner-seed-derived combat selection'
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed-derived combat run identity is malformed.'}
 foreach($key in @('sourceRunId','checkpointSourceRunId','seedId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed-derived combat requires exact producer, consumer and seed identities.'}}
 if(@(@($Request.runId,$s.sourceRunId,$s.checkpointSourceRunId,$s.seedId)|Select-Object -Unique).Count-ne4){throw 'Owner-seed-derived combat producer, consumer, seed and run identities must be distinct.'}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','checkpointSourceArtifactSha256','seedSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed-derived combat requires exact SHA256 pins.'}}
 if(@(@($s.sourceArtifactSha256,$s.sourceArchiveSha256,$s.checkpointSourceArtifactSha256,$s.seedSha256)|Select-Object -Unique).Count-ne4){throw 'Owner-seed-derived combat pins alias one another or the immutable seed.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 if($rt){Assert-KmcOrdinaryGuid $s.companionId;if(-not$party.Contains($s.companionId)-or$ids.Contains($s.companionId)){throw 'The RT companion must be one unrelated member of the selected party.'}}
 if($null-ne$Checkpoint){throw 'Owner-seed-derived combat selects no persistence checkpoint.'}
 Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 0
}
# Owner-seed checkpoint pair. Producer: source* is the owner-seed Manual producer, checkpointSource*
# the ACCEPTED cold consumer (as derived combat), one load and one write. Consumer: source* keeps the
# Manual producer ancestry, checkpointSource* is the checkpoint producer run and its persistence
# evidence, one load and zero writes. Both select one connected checkpoint.
function Assert-KmcChunk7OwnerSeedCheckpointSelection($Request,$Selection,$Checkpoint){
 $s=$Selection;$save=$s.case-ceq'owner-seed-checkpoint-save';$area=$Checkpoint-ceq'multi-area-autosave'
 $fields=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','checkpointSourceRunId','checkpointSourceArtifactSha256','seedId','seedSha256','riderAId','mountAId','riderBId','mountBId','partyIds','baseGameOnly','expectedNativeLoadCount','expectedNativeWriteCount');if($area){$fields+='sourceArea'}
 Assert-KmcExactProperties $s $fields 'owner-seed checkpoint selection'
 if($area){Assert-KmcBootstrapInventoryString $s.sourceArea 'b10e86516b6437d45a629931c30936fe'}
 if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed checkpoint run identity is malformed.'}
 foreach($key in @('sourceRunId','checkpointSourceRunId','seedId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Owner-seed checkpoint requires exact Manual producer, checkpoint parent and seed identities.'}}
 if(@(@($Request.runId,$s.sourceRunId,$s.checkpointSourceRunId,$s.seedId)|Select-Object -Unique).Count-ne4){throw $(if($save){'The owner-seed checkpoint producer requires the exact distinct accepted cold-consumer run.'}else{'The owner-seed checkpoint consumer requires the exact distinct checkpoint producer run.'})}
 foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','checkpointSourceArtifactSha256','seedSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Owner-seed checkpoint requires exact SHA256 pins.'}}
 if(@(@($s.sourceArtifactSha256,$s.sourceArchiveSha256,$s.checkpointSourceArtifactSha256,$s.seedSha256)|Select-Object -Unique).Count-ne4){throw 'Owner-seed checkpoint pins alias one another or the immutable seed.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($key in @('riderAId','mountAId','riderBId','mountBId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Inherited role IDs collide.'}}
 if($s.partyIds-isnot[Array]-or$s.partyIds.Count-lt4-or$s.partyIds.Count-gt12){throw 'Complete inherited field roster must be bounded.'}
 $party=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($id in $s.partyIds){Assert-KmcOrdinaryGuid $id;if(-not$party.Add($id)){throw 'Inherited roster repeats an actor.'}}
 foreach($id in $ids){if(-not$party.Contains($id)){throw 'Inherited roster omits a selected pair role.'}}
 if($Checkpoint-isnot[string]-or($Checkpoint-cnotin(Get-KmcChunk7OwnerSeedCheckpoints)-and(-not$save-or$Checkpoint-cnotin(Get-KmcChunk7OwnerSeedSourceOnlyCheckpoints))-and($save-or$Checkpoint-cnotin(Get-KmcChunk7OwnerSeedLoadOnlyCheckpoints)))){throw ('The owner-seed checkpoint pair admits only its connected checkpoints in their exact source or load direction: '+((Get-KmcChunk7OwnerSeedCheckpoints)-join', ')+'.')}
 $writes=Get-KmcChunk7OwnedWriteCount $Request;Assert-KmcChunk7Boolean $s.baseGameOnly $true;Assert-KmcOrdinaryInteger $s.expectedNativeLoadCount 1 1;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount $writes $writes
}
function Assert-KmcChunk7OwnerSeedControlFacts($Value,[string[]]$Roles,[bool]$After,$Controls){
 Assert-KmcExactProperties $Value @('contract','rows','totalFacts','activeFacts') 'owner-seed native control facts'
 Assert-KmcBootstrapInventoryString $Value.contract 'owner-seed-control-facts-v1'
 if($Value.rows-isnot[Array]-or$Value.rows.Count-ne20){throw 'Native control facts must observe each of four actors and all five registered control kinds.'}
 $guids=@('f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$total=0;$active=0
 foreach($row in $Value.rows){
  Assert-KmcExactProperties $row @('actorId','kind','blueprintGuid','totalFacts','activeFacts','facts') 'owner-seed actor control facts'
  if($row.actorId-isnot[string]-or$row.actorId-cnotin$Roles){throw 'Native control fact actor is outside the selected four roles.'}
  Assert-KmcOrdinaryInteger $row.kind 1 5
  if(-not$seen.Add($row.actorId+':'+$row.kind)){throw 'Native actor/control kind is repeated.'}
  Assert-KmcBootstrapInventoryString $row.blueprintGuid $guids[$row.kind-1]
  $expected=if($row.actorId-ceq$Roles[0]-and$row.kind-in@(2,3,4)){1}elseif($row.actorId-ceq$Roles[2]-and$(if($After){$row.kind-eq1}else{$row.kind-in@(2,3,4)})){1}else{0}
  foreach($field in @('totalFacts','activeFacts')){Assert-KmcOrdinaryInteger $row.$field $expected $expected}
  if($row.facts-isnot[Array]-or$row.facts.Count-ne$expected){throw 'Native actor/control fact inventory differs from its required mounted or unmounted role.'}
  foreach($fact in $row.facts){
   Assert-KmcExactProperties $fact @('active','factBlueprintExact','dataPresent','dataBlueprintExact','casterExact','nativeLookupExact') 'owner-seed exact native control fact'
   foreach($field in @('active','factBlueprintExact','dataPresent','dataBlueprintExact','casterExact','nativeLookupExact')){Assert-KmcChunk7Boolean $fact.$field $true}
  }
  $total+=$row.totalFacts;$active+=$row.activeFacts
 }
 foreach($field in @('totalFacts','activeFacts')){Assert-KmcOrdinaryInteger $Value.$field $(if($After){4}else{6}) $(if($After){4}else{6})}
 Assert-KmcChunk7NativeCounters $Controls @('ExactFactCount')
 if($total-ne$Value.totalFacts-or$active-ne$Value.activeFacts-or$total-ne$Controls.ExactFactCount){throw 'Native control fact matrix does not match its aggregate inventory.'}
}
function Assert-KmcChunk7OwnerSeedCheckpointPreparation($Request,$Observations){
 $o=$Observations;$roles=Get-KmcChunk7OwnedPairIds $Request;$pending=$Request.persistenceCase-ceq'multi-nonselected-mount-unsettled-manual'
 $p=$o.ownerSeedCheckpointPreparation;$fields=@('contract','checkpoint','before','slotsBefore','controlFactsBefore','input','after','slotsAfter','controlFactsAfter');if($pending){$fields+='remountedSecondOwner'}
 Assert-KmcExactProperties $p $fields 'owner-seed native second-owner preparation'
 Assert-KmcBootstrapInventoryString $p.contract 'owner-seed-checkpoint-native-preparation-v1';Assert-KmcBootstrapInventoryString $p.checkpoint $Request.persistenceCase
 if($o.inputs-isnot[Array]-or$o.inputs.Count-ne$(if($pending){2}else{1})-or(ConvertTo-KmcChunk7Canonical $p.input)-cne(ConvertTo-KmcChunk7Canonical $o.inputs[0])){throw 'Native second-owner preparation lacks its exact recorded Dismount input.'}
 $input=$p.input
 foreach($spec in @(@('label','owner-seed-native-dismount-second-pair'),@('casterId',$roles[2]),@('targetId',$roles[2]),@('abilityGuid','3af2b81f4d72bbb30501fa730fcdf36e'),@('selectionAfterInput',$roles[0]))){Assert-KmcBootstrapInventoryString $input.($spec[0]) $spec[1]}
 Assert-KmcChunk7Boolean $input.clicked $true;Assert-KmcChunk7Boolean $input.terminal $true;Assert-KmcOrdinaryInteger $input.nativeCastDelta 1 1;Assert-KmcOrdinaryInteger $input.nativeRefusalDelta 0 0;Assert-KmcOrdinaryInteger $input.generationAtInit 1 ([long]::MaxValue)
 if($input.control-isnot[string]-or[string]::IsNullOrWhiteSpace($input.control)){throw 'Native second-owner Dismount has no exact control identity.'}
 $before=$p.before;$after=$p.after
 Assert-KmcBootstrapInventoryString $before.label 'owner-seed-before-dismount-second-pair';Assert-KmcBootstrapInventoryString $after.label 'owner-seed-after-dismount-second-pair'
 foreach($snapshot in @($before,$after)){
  Assert-KmcChunk7NativeCounters $snapshot @('frame','gameTicks','nativeCastRequests','snapshots','semanticRestores','presentationRestores','nativePostLoads')
  if($snapshot.actors-isnot[Array]-or$snapshot.actors.Count-ne4-or$snapshot.owners-isnot[Array]-or$snapshot.snapshots-ne0-or$snapshot.semanticRestores-ne4-or$snapshot.presentationRestores-ne2-or$snapshot.nativePostLoads-ne4){throw 'Native preparation omitted the restored actors or entered serialization/restoration again.'}
  Assert-KmcChunk7NativeCounters $snapshot.controls @('ManagedHotbarSlotCount','DuplicateFactCount');Assert-KmcChunk7Boolean $snapshot.controls.SerializationSuspended $false
  if($snapshot.controls.DuplicateFactCount-ne0){throw 'Native preparation duplicated owned controls.'}
 }
 Assert-KmcChunk7OwnerSeedControlFacts $p.controlFactsBefore $roles $false $before.controls
 Assert-KmcChunk7OwnerSeedControlFacts $p.controlFactsAfter $roles $true $after.controls
 # ManagedHotbarSlotCount also counts recognized stale UI templates. The exact live
 # bindings below must still contain only A's original two controls; B has Mount only.
 if($before.owners.Count-ne2-or$after.owners.Count-ne1-or$before.controls.ManagedHotbarSlotCount-ne4-or$after.controls.ManagedHotbarSlotCount-notin@(2,4)-or$before.nativeCastRequests-ne0-or$after.nativeCastRequests-ne1-or$after.frame-lt$before.frame){throw 'Native Dismount did not retire exactly the second owner and its controls.'}
 foreach($field in @('secondRiderOwnerAbsent','secondMountOwnerAbsent','secondPairUnmounted','secondPairNoPreparedResidue','secondPairCanRelease','actorsIdle')){Assert-KmcChunk7Boolean $after.$field $true}
 foreach($field in @('unsettledPreparation','unsettledTransition','secondPairCommandsRetained','worldSerializationBusy','saveBusy')){Assert-KmcChunk7Boolean $after.$field $false}
 $elapsed=([double]$after.gameTicks-[double]$before.gameTicks)/10000000.0;if($elapsed-lt0-or$elapsed-gt30){throw 'Native second-owner preparation escaped its bounded native clock.'}
 for($i=0;$i-lt2;$i++){
  $prior=@($before.owners|Where-Object riderId -CEQ $roles[$i*2]);$loaded=@($o.mountedStart.owners|Where-Object riderId -CEQ $roles[$i*2])
  if($prior.Count-ne1-or$loaded.Count-ne1-or$prior[0].ownerId-cne$loaded[0].ownerId-or$prior[0].generation-ne$loaded[0].generation-or$prior[0].mountId-cne$roles[$i*2+1]-or$prior[0].state-cne'Active'){throw 'Native preparation did not begin with the exact two restored owners.'}
  Assert-KmcExactProperties $prior[0] @('ownerId','generation','riderId','mountId','state') 'native preparation owner'
  foreach($spec in @(@('ownerId',$loaded[0].ownerId),@('riderId',$roles[$i*2]),@('mountId',$roles[$i*2+1]),@('state','Active'))){Assert-KmcBootstrapInventoryString $prior[0].($spec[0]) $spec[1]}
  Assert-KmcOrdinaryInteger $prior[0].generation 1 ([long]::MaxValue)
 }
 $a=$before.owners|Where-Object riderId -CEQ $roles[0];$b=$before.owners|Where-Object riderId -CEQ $roles[2]
 if((ConvertTo-KmcChunk7Canonical $after.owners[0])-cne(ConvertTo-KmcChunk7Canonical $a)-or$input.generationAtInit-ne$b.generation){throw 'Native Dismount changed the surviving owner or used another generation.'}
 foreach($id in $roles){
  $prior=@($before.actors|Where-Object{$_.native.Id-ceq$id});$current=@($after.actors|Where-Object{$_.native.Id-ceq$id})
  if($prior.Count-ne1-or$current.Count-ne1){throw 'Native preparation omitted or duplicated an actor.'};$prior=$prior[0];$current=$current[0]
  Assert-KmcChunk7PersistenceNativeActor $prior.native;Assert-KmcChunk7PersistenceNativeActor $current.native
  foreach($v in @($prior,$current)){Assert-KmcChunk7Boolean $v.inCombat $false;if($v.prepared-isnot[bool]){throw 'Native preparation flag is not Boolean.'}}
  if($prior.prepared-ne$current.prepared){throw 'Native Dismount manufactured preparation.'}
  foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){if([double]$current.native.$field-gt[double]$prior.native.$field+0.001-or[double]$current.native.$field+$elapsed*1.1+0.001-lt[double]$prior.native.$field){throw 'Native second-owner Dismount changed actor debt beyond elapsed game time.'}}
  foreach($field in @('ReactionsRemaining','LastSurpriseTicks')){if($current.native.$field-ne$prior.native.$field){throw 'Native Dismount changed native reaction/surprise history.'}}
 }
 $loadedSlots=@($o.mountedStart.slots|ForEach-Object{$_.actorId+':'+$_.index+':'+$_.kind}|Sort-Object -CaseSensitive)
 $beforeSlots=@($p.slotsBefore|ForEach-Object{$_.ActorId+':'+$_.Index+':'+$_.Kind}|Sort-Object -CaseSensitive)
 $afterSlots=@($p.slotsAfter|ForEach-Object{$_.ActorId+':'+$_.Index+':'+$_.Kind}|Sort-Object -CaseSensitive)
 $expectedSlots=@($p.slotsBefore|Where-Object ActorId -CEQ $roles[0]|ForEach-Object{$_.ActorId+':'+$_.Index+':'+$_.Kind}|Sort-Object -CaseSensitive)
 foreach($slot in @($p.slotsBefore)+@($p.slotsAfter)){Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'native preparation control slot';Assert-KmcOrdinaryGuid $slot.ActorId;Assert-KmcOrdinaryInteger $slot.Index 0 127;Assert-KmcOrdinaryInteger $slot.Kind 2 3}
 if($p.slotsBefore-isnot[Array]-or$p.slotsBefore.Count-ne4-or$p.slotsAfter-isnot[Array]-or$p.slotsAfter.Count-ne2-or($beforeSlots-join';')-cne($loadedSlots-join';')-or($afterSlots-join';')-cne($expectedSlots-join';')){throw 'Native second-owner Dismount changed surviving controls or retained B bindings.'}
 foreach($pair in @(@($input.before,$before),@($input.after,$after))){foreach($field in @('frame','gameTicks','owners','actors','controls','semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests','archivePhase')){if((ConvertTo-KmcChunk7Canonical $pair[0].$field)-cne(ConvertTo-KmcChunk7Canonical $pair[1].$field)){throw 'Native preparation snapshots do not match its actual Dismount command boundary.'}}}
 $captured=Get-KmcChunk7OwnerSeedSnapshot $o 'before-native-save';foreach($field in @('owners','actors','controls','nativeCastRequests')){if((ConvertTo-KmcChunk7Canonical $captured.$field)-cne(ConvertTo-KmcChunk7Canonical $after.$field)){throw 'The one-owner before-save boundary differs from settled native preparation.'}}
 if($pending){
  $next=$p.remountedSecondOwner;Assert-KmcExactProperties $next @('ownerId','generation','riderId','mountId') 'native remounted second owner';Assert-KmcOrdinaryInteger $next.generation 1 ([long]::MaxValue)
  Assert-KmcBootstrapInventoryString $next.riderId $roles[2];Assert-KmcBootstrapInventoryString $next.mountId $roles[3]
  if($next.ownerId-isnot[string]-or$next.ownerId-cne(ConvertTo-KmcChunk7PairIdN $next.ownerId)-or$next.ownerId-cin@($a.ownerId,$b.ownerId)-or$next.riderId-cne$roles[2]-or$next.mountId-cne$roles[3]){throw 'Native remount did not acquire one distinct exact second owner.'}
  $mount=$o.inputs[1];foreach($spec in @(@('label','mount-horse'),@('casterId',$roles[2]),@('targetId',$roles[3]),@('abilityGuid','f053faad986631688defa003cd7bda0e'),@('selectionAfterInput',$roles[0]))){Assert-KmcBootstrapInventoryString $mount.($spec[0]) $spec[1]}
  Assert-KmcChunk7Boolean $mount.clicked $true;Assert-KmcChunk7Boolean $mount.terminal $true;Assert-KmcOrdinaryInteger $mount.nativeCastDelta 1 1;Assert-KmcOrdinaryInteger $mount.nativeRefusalDelta 0 0;Assert-KmcOrdinaryInteger $mount.generationAtInit 0 ([long]::MaxValue)
  if($mount.control-isnot[string]-or[string]::IsNullOrWhiteSpace($mount.control)-or$next.generation-ne$mount.generationAtInit+1){throw 'Native remount generation/control differs from its observed command.'}
 }
}
# Owner-seed checkpoint producer start: the restored mounted graph replaces the native Mount inputs
# and new bindings; the producer adopts the restored owners, generations and slots exactly.
function Assert-KmcChunk7OwnerSeedCheckpointStart($Request,$Observations,$Archive){
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)){throw 'Restored-start evidence belongs only to the owner-seed checkpoint producer.'}
 $o=$Observations;$s=$Request.ownedPairs;$roles=Get-KmcChunk7OwnedPairIds $Request
 $combat=Test-KmcChunk7OwnerSeedCombatCheckpoint $Request.persistenceCase;$zero=$Request.persistenceCase-ceq'zero-mounted-tb-retained-manual'
 $legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual';$pending=$Request.persistenceCase-ceq'multi-nonselected-mount-unsettled-manual';$schema1=$Request.persistenceCase-ceq'owned-schema1-manual';$single=$legacy-or$pending-or$schema1
 if(-not$single-and@($o.inputs).Count-ne0){throw 'The owner-seed checkpoint producer restored its pairs mounted; a native Mount input is not admitted.'}
 if(-not$single-and$null-ne$o.PSObject.Properties['ownerSeedCheckpointPreparation']){throw 'Native second-owner preparation belongs only to its selected source cases.'}
 if($null-eq$o.PSObject.Properties['mountedStart']){throw 'The owner-seed checkpoint producer lacks its restored mounted-start admission.'}
 $m=$o.mountedStart
 Assert-KmcExactProperties $m @('contract','restoredFromColdLoad','nativeMountInputs','newSlotBindings','archivePhase','schemaVersion','semanticRestores','presentationRestores','nativePostLoads','nativeCastRequests','snapshots','profiles','owners','slots','loadedEqualsProducerReadback','producerProcessId','processId','debtRetained','loadedArchive') 'owner-seed checkpoint restored start'
 Assert-KmcBootstrapInventoryString $m.contract 'owner-seed-checkpoint-restored-start-v1';Assert-KmcBootstrapInventoryString $m.archivePhase 'all-pair-presentation-restored'
 foreach($key in @('restoredFromColdLoad','loadedEqualsProducerReadback','debtRetained')){Assert-KmcChunk7Boolean $m.$key $true}
 foreach($pair in @(@('nativeMountInputs',0),@('newSlotBindings',0),@('schemaVersion',3),@('semanticRestores',4),@('presentationRestores',2),@('nativePostLoads',4),@('nativeCastRequests',0),@('snapshots',0))){Assert-KmcOrdinaryInteger $m.($pair[0]) $pair[1] $pair[1]}
 Assert-KmcOrdinaryInteger $m.producerProcessId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $m.processId 1 ([int]::MaxValue);if($m.producerProcessId-eq$m.processId-or$m.processId-ne$o.processId){throw 'A same-process world cannot prove the restored mounted start.'}
 if(@($m.profiles).Count-ne2-or@($m.owners).Count-ne2){throw 'The owner-seed checkpoint producer requires both restored pairs.'}
 if($single){Assert-KmcChunk7OwnerSeedCheckpointPreparation $Request $o}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 for($i=0;$i-lt2;$i++){
  $p=@($m.profiles|Where-Object riderId -CEQ $roles[$i*2]);if($p.Count-ne1-or$p[0].mountId-cne$roles[$i*2+1]-or$p[0].profileId-cne'medium-humanoid-horse-v1'){throw 'A restored pair lost its exact rider, mount or Horse profile.'}
  $w=@($m.owners|Where-Object riderId -CEQ $roles[$i*2]);if($w.Count-ne1-or$w[0].mountId-cne$roles[$i*2+1]){throw 'A restored owner is missing or bound to another actor.'}
  $n=ConvertTo-KmcChunk7PairIdN $w[0].ownerId;if($w[0].ownerId-cne$n-or-not$seen.Add($n)){throw 'Restored owner identity is not one exact archive (N) form.'};Assert-KmcOrdinaryInteger $w[0].generation 1 ([long]::MaxValue)
  # The selected preparation retired B natively; pending Mount acquires a new
  # B owner. All other owners retain their restored identity and generation.
  if($single-and$i-eq1){
   if($legacy-or$schema1){if(@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]-or$_.Mount.Id-ceq$roles[3]}).Count-ne0-or@($Archive.Participations|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.OwnerId)-ceq$n}).Count-ne0){throw 'The legacy source retained the natively released second owner.'};continue}
   $w=@($o.ownerSeedCheckpointPreparation.remountedSecondOwner);$n=$w[0].ownerId
  }
  $saved=@($Archive.Pairs|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.PairId)-ceq$n})
  if($combat){
   $archiveOwner=$n
   $row=@($Archive.Participations|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.OwnerId)-ceq$archiveOwner})
   $archiveGeneration=$w[0].generation
   if($row.Count-ne1-or$row[0].Generation-ne$archiveGeneration-or(-not$legacy-and($row[0].State.RiderId-cne$roles[$i*2]-or$row[0].State.MountId-cne$roles[$i*2+1]))-or($legacy-and($null-ne$row[0].State.RiderId-or$null-ne$row[0].State.MountId-or$null-ne$row[0].State.Activation-or$row[0].State.PendingSplitId-cne$roles[1]))){throw 'The written checkpoint changed its exact live or historically migrated owner participation.'}
  }
  if($zero){if($saved.Count-ne0){throw 'The zero-mounted checkpoint kept a mounted pair.'}}
  elseif($saved.Count-ne1-or$saved[0].Generation-ne$w[0].generation-or$saved[0].Rider.Id-cne$roles[$i*2]-or$saved[0].Mount.Id-cne$roles[$i*2+1]){throw 'The written checkpoint changed a restored owner or its original generation.'}
 }
 if($combat-and@($Archive.Participations).Count-ne$(if($legacy){1}else{2})){throw 'The written combat checkpoint lost or invented an owner participation.'}
 if(@($m.slots).Count-ne4){throw 'The owner-seed checkpoint producer requires its four restored controls.'}
 foreach($rider in @($roles[0],$roles[2])){if((@($m.slots|Where-Object actorId -CEQ $rider|ForEach-Object{$_.kind}|Sort-Object)-join',')-cne'2,3'){throw 'A restored pair lost its exact native controls.'}}
 # Native Dismounts release the mounted controls; every other checkpoint keeps the restored bindings.
 if($zero){if(@($Archive.Slots).Count-ne0){throw 'The zero-mounted checkpoint kept a mounted control.'}}else{
 if(@($Archive.Slots).Count-ne$(if($single){2}else{4})){throw 'The written checkpoint changed the restored control bindings.'}
 foreach($slot in @($m.slots|Where-Object{-not$single-or$_.actorId-ceq$roles[0]})){if(@($Archive.Slots|Where-Object{$_.ActorId-ceq$slot.actorId-and$_.Index-eq$slot.index-and$_.Kind-eq$slot.kind}).Count-ne1){throw 'The written checkpoint changed the restored control bindings.'}}}
 $l=$m.loadedArchive;if($null-eq$l-or$l.SchemaVersion-ne3-or@($l.Pairs).Count-ne2-or@($l.Participations).Count-ne0-or$null-ne$l.Combat-or$l.AreaId-cne$Request.persistenceLoad.area-or$l.CampaignId-cne$Request.persistenceLoad.gameId){throw 'The restored start is not the exact two-pair exploration graph of the selected Manual copy.'}
 [void](Get-KmcChunk7OwnerSeedSnapshot $o 'before-native-save')
 $src=$o.ownerSeedCheckpointSource
 Assert-KmcExactProperties $src @('contract','role','ancestry','acquisitionProof','ordinaryProducerAncestry','manualProducerRunId','manualProducerArtifactSha256','manualSha256','acceptedConsumerRunId','acceptedConsumerArtifactSha256','seedId','seedSha256','checkpoint','candidates') 'owner-seed checkpoint source'
 Assert-KmcBootstrapInventoryString $src.contract 'owner-seed-checkpoint-source-v1';Assert-KmcBootstrapInventoryString $src.role 'producer';Assert-KmcBootstrapInventoryString $src.ancestry 'owner-seed-derived'
 Assert-KmcChunk7Boolean $src.acquisitionProof $false;Assert-KmcChunk7Boolean $src.ordinaryProducerAncestry $false;Assert-KmcBootstrapInventoryString $src.checkpoint $Request.persistenceCase
 foreach($pair in @(@('manualProducerRunId','sourceRunId'),@('manualProducerArtifactSha256','sourceArtifactSha256'),@('manualSha256','sourceArchiveSha256'),@('acceptedConsumerRunId','checkpointSourceRunId'),@('acceptedConsumerArtifactSha256','checkpointSourceArtifactSha256'),@('seedId','seedId'),@('seedSha256','seedSha256'))){Assert-KmcBootstrapInventoryString $src.($pair[0]) $s.($pair[1])}
 Assert-KmcChunk7OwnerSeedCandidates $Request $src.candidates
}
function Get-KmcChunk7OwnerSeedSnapshot($Observations,[string]$Label){$rows=@($Observations.snapshots|Where-Object label -CEQ $Label);if($rows.Count-ne1){throw ('Exact '+$Label+' snapshot is missing or duplicated.')};$rows[0]}
# Both receipts' recorded candidate identities, as derived combat records them.
function Assert-KmcChunk7OwnerSeedCandidates($Request,$Candidates){
 $c=$Candidates;$s=$Request.ownedPairs;Assert-KmcExactProperties $c @('contract','sameCandidate','crossCandidateAllowed','producer','current','consumerRunId','consumerArtifactSha256','manualSchema','currentWriterSchema') 'owner-seed-derived candidate identities'
 Assert-KmcBootstrapInventoryString $c.contract 'owner-seed-derived-candidate-identity-v1';Assert-KmcChunk7Boolean $c.crossCandidateAllowed $true;if($c.sameCandidate-isnot[bool]){throw 'Cross-candidate fact must be an observed boolean.'}
 Assert-KmcOrdinaryInteger $c.manualSchema 3 3;Assert-KmcOrdinaryInteger $c.currentWriterSchema 3 3
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $c.current.$key $Request.$key}
 if($c.producer.commit-isnot[string]-or$c.producer.commit-cnotmatch'^[a-f0-9]{40}$'-or$c.producer.dllSha256-isnot[string]-or$c.producer.dllSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Producer candidate identity is not recorded exactly.'}
 $same=$c.producer.commit-ceq$Request.commit-and$c.producer.dllSha256-ceq$Request.dllSha256-and$c.producer.dllMvid-ceq$Request.dllMvid
 if($same-ne$c.sameCandidate){throw 'The recorded cross-candidate fact differs from the two candidate identities.'}
 Assert-KmcBootstrapInventoryString $c.producer.runId $s.sourceRunId;Assert-KmcBootstrapInventoryString $c.consumerRunId $s.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $c.consumerArtifactSha256 $s.checkpointSourceArtifactSha256
}
# Runner post-run comparison of the checkpoint consumer with its producer's OWN recorded read-back
# (never the Manual boundary): the exact written graph, owners, generations and slots, another process.
function Assert-KmcChunk7OwnerSeedCheckpointColdSource($Request,$ProducerArtifact,$ColdArtifact){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)){throw 'Owner-seed checkpoint cold source belongs only to its consumer kind.'}
 if(Test-KmcChunk7OwnerSeedCurrentCheckpoint $Request.persistenceCase){Assert-KmcChunk7CurrentMigrationParent $Request $ProducerArtifact $ColdArtifact;return}
 $s=$Request.ownedPairs;$p=$ProducerArtifact.observations;$c=$ColdArtifact.observations
 Assert-KmcBootstrapInventoryString $ProducerArtifact.runId $s.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $ProducerArtifact.status 'PASS';Assert-KmcBootstrapInventoryString $ColdArtifact.status 'PASS'
 Assert-KmcBootstrapInventoryString $p.checkpoint (Get-KmcChunk7PersistenceSourceCase $Request.persistenceCase);Assert-KmcChunk7Boolean $p.cold $false;Assert-KmcChunk7Boolean $c.cold $true
 $src=$p.ownerSeedCheckpointSource;foreach($pair in @(@('manualProducerRunId','sourceRunId'),@('manualProducerArtifactSha256','sourceArtifactSha256'),@('manualSha256','sourceArchiveSha256'),@('seedId','seedId'),@('seedSha256','seedSha256'))){Assert-KmcBootstrapInventoryString $src.($pair[0]) $s.($pair[1])}
 if($Request.persistenceCase-ceq'owned-schema1-resave-manual'){
  Assert-KmcOwnedSchema1Observation $Request $ColdArtifact
  Assert-KmcOrdinaryInteger $p.processId 1 ([int]::MaxValue);if($p.processId-eq$c.processId){throw 'Owner-seed schema-1 cold proof did not run in a process separate from its producer.'}
  Assert-KmcBootstrapInventoryString $c.ownedSchema1.originalNativeArchiveSha256 $p.nativeWrite.sha256
  if((ConvertTo-KmcChunk7Canonical $c.schema1Migration.nativeInputMetadata)-cne(ConvertTo-KmcChunk7Canonical (ConvertTo-KmcOwnedSchema1Metadata $p.nativeWrite.archive $Request))-or(ConvertTo-KmcChunk7Canonical $c.ownedSchema1.sourceField)-cne(ConvertTo-KmcChunk7Canonical $p.ownedSchema1.fields[1])){throw 'Owner-seed historical schema-1 input differs from its native producer graph or full field.'}
  return
 }
 $legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'
 if($legacy){
  Assert-KmcOwnedSchema2Migration $Request $c;Assert-KmcOrdinaryInteger $p.nativeWrite.sourceSchema 3 3
  Assert-KmcBootstrapInventoryString $p.nativeSourceContract 'owner-seed-current-pending-source-v1'
  Assert-KmcBootstrapInventoryString $c.schema2Migration.originalNativeArchiveSha256 $p.nativeWrite.sha256
  $projection=ConvertTo-KmcOwnedSchema2Metadata $p.nativeWrite.archive $Request
  if((ConvertTo-KmcChunk7Canonical $projection)-cne(ConvertTo-KmcChunk7Canonical $c.schema2Migration.nativeInputMetadata)){throw 'Historical schema2 input differs from its genuine current producer graph.'}
  $legacyImported=ConvertFrom-KmcOwnedSchema2Metadata $projection $Request
  if((ConvertTo-KmcChunk7Canonical $legacyImported)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){throw 'Native schema2 migration changed more than deterministic historical owner custody.'}
 }else{Assert-KmcBootstrapInventoryString $p.nativeWrite.sha256 $Request.persistenceLoad.sha256;Assert-KmcOrdinaryInteger $p.nativeWrite.length $Request.persistenceLoad.length $Request.persistenceLoad.length}
 Assert-KmcOrdinaryInteger $p.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $c.processId 1 ([int]::MaxValue);if($p.processId-eq$c.processId){throw 'Owner-seed checkpoint cold proof did not run in a process separate from its producer.'}
 if(-not$legacy-and(ConvertTo-KmcChunk7Canonical $p.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){throw 'Cold process did not restore the exact checkpoint producer read-back graph.'}
 $combat=Test-KmcChunk7OwnerSeedCombatCheckpoint $Request.persistenceCase;$zero=$Request.persistenceCase-ceq'zero-mounted-tb-retained-manual'
 $legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'
 if($legacy){Assert-KmcChunk7OwnerSeedCheckpointPreparation $Request $p;Assert-KmcChunk7CombatArchive $c.loadedArchive $Request;Assert-KmcChunk7LegacyPendingMigration $projection $c.loadedArchive}
 # Schema 2 stores no stable owner identity/generation: its explicit adapter
 # derives deterministic custody at generation one. Live preparation preserves
 # A; the cold process must restore this validated migrated graph exactly.
 $produced=@(if($legacy){$legacyImported.Pairs|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.PairId)+'|'+$_.Generation+'|'+$_.Rider.Id+'|'+$_.Mount.Id}}else{$p.mountedStart.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.ownerId)+'|'+$_.generation+'|'+$_.riderId+'|'+$_.mountId}})|Sort-Object -CaseSensitive
 $restored=@($c.loadedArchive.Pairs|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.PairId)+'|'+$_.Generation+'|'+$_.Rider.Id+'|'+$_.Mount.Id}|Sort-Object -CaseSensitive)
 # A combat checkpoint restores every owner through its participation; the zero-mounted one has no mounted pair.
 if($combat){
  $participating=@($c.loadedArchive.Participations|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.OwnerId)+'|'+$_.Generation+'|'+$(if($legacy){$s.riderAId}else{$_.State.RiderId})+'|'+$(if($legacy){$s.mountAId}else{$_.State.MountId})}|Sort-Object -CaseSensitive)
  if(($produced-join';')-cne($participating-join';')){throw 'Restored combat participations or generations differ from the checkpoint producer runtime.'}
  if($zero){$restored=$produced;if(@($c.loadedArchive.Pairs).Count-ne0){throw 'The zero-mounted checkpoint restored a mounted pair.'}}
  Assert-KmcChunk7Boolean $p.combatDebtProof $true;Assert-KmcChunk7Boolean $c.combatDebtProof $true
  if($p.combatCheckpoint.targetId-isnot[string]-or$p.combatCheckpoint.targetId-cne$c.combatContinuation.targetId-or$p.combatCheckpoint.targetGroupId-cne$c.combatContinuation.targetGroupId){throw 'Cold combat bound another target than the checkpoint producer saved.'}
 }
 if(($produced-join';')-cne($restored-join';')){throw 'Restored owners or generations differ from the checkpoint producer runtime.'}
 $producedSlots=@(if(-not$zero){$p.mountedStart.slots|Where-Object{-not$legacy-or$_.actorId-ceq$s.riderAId}|ForEach-Object{$_.actorId+':'+$_.index+':'+$_.kind}|Sort-Object -CaseSensitive});$restoredSlots=@($c.loadedArchive.Slots|ForEach-Object{$_.ActorId+':'+$_.Index+':'+$_.Kind}|Sort-Object -CaseSensitive)
 if(($producedSlots-join';')-cne($restoredSlots-join';')){throw 'Restored control bindings differ from the checkpoint producer runtime.'}
 foreach($key in @('partyIds','currentCharacterIds','remoteCapturedIds','arrangement')){if((ConvertTo-KmcChunk7Canonical $p.inheritedCleanup.$key)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedFixture.$key)){throw 'Checkpoint cold source changed full-field ancestry or progression.'}}
 for($i=0;$i-lt2;$i++){foreach($key in @('riderId','mountId','sourceGuid','rank','profileId')){if((ConvertTo-KmcChunk7Canonical $p.inheritedCleanup.pairs[$i].$key)-cne(ConvertTo-KmcChunk7Canonical $c.inheritedFixture.pairs[$i].$key)){throw 'Checkpoint cold source changed inherited role/fact provenance.'}}}
}
# Combat-reader mapping for the derived kinds: the restored mounted start replaces the native
# Mount inputs, the target authorization is the derived-copy basis and the source keeps its
# owner-seed-derived label with both receipts' recorded candidate identities.
function Assert-KmcChunk7OwnerSeedDerivedCombatStart($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)){throw 'Mounted-start evidence belongs only to owner-seed-derived combat.'}
 $o=$Observations;$s=$Request.ownedPairs;$roles=Get-KmcChunk7OwnedPairIds $Request
 if(@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b')}).Count-ne0){throw 'Owner-seed-derived combat restored its pairs mounted; a native Mount input is not admitted.'}
 if($null-eq$o.PSObject.Properties['mountedStart']){throw 'Owner-seed-derived combat lacks its restored mounted-start admission.'}
 $m=$o.mountedStart
 Assert-KmcExactProperties $m @('contract','restoredFromColdLoad','nativeMountInputs','archivePhase','schemaVersion','semanticRestores','presentationRestores','nativePostLoads','nativeCastRequests','snapshots','profiles','owners','slots','loadedEqualsProducerReadback','producerProcessId','processId','debtRetained','elapsedSeconds','debt','world') 'owner-seed-derived mounted start'
 Assert-KmcBootstrapInventoryString $m.contract 'owner-seed-derived-mounted-start-v1';Assert-KmcBootstrapInventoryString $m.archivePhase 'all-pair-presentation-restored'
 foreach($key in @('restoredFromColdLoad','loadedEqualsProducerReadback','debtRetained')){Assert-KmcChunk7Boolean $m.$key $true}
 foreach($pair in @(@('nativeMountInputs',0),@('schemaVersion',3),@('semanticRestores',4),@('presentationRestores',2),@('nativePostLoads',4),@('nativeCastRequests',0),@('snapshots',0))){Assert-KmcOrdinaryInteger $m.($pair[0]) $pair[1] $pair[1]}
 Assert-KmcOrdinaryInteger $m.producerProcessId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $m.processId 1 ([int]::MaxValue);if($m.producerProcessId-eq$m.processId){throw 'A same-process world cannot prove the restored mounted start.'}
 if(-not(Test-KmcChunk7FiniteNumber $m.elapsedSeconds)-or$m.elapsedSeconds-lt0){throw 'Mounted-start clock observation is invalid.'}
 if(@($m.profiles).Count-ne2-or@($m.owners).Count-ne2){throw 'Owner-seed-derived combat requires both restored pairs.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 for($i=0;$i-lt2;$i++){
  $p=@($m.profiles|Where-Object riderId -CEQ $roles[$i*2]);if($p.Count-ne1-or$p[0].mountId-cne$roles[$i*2+1]-or$p[0].profileId-cne'medium-humanoid-horse-v1'){throw 'A restored pair lost its exact rider, mount or Horse profile.'}
  $w=@($m.owners|Where-Object riderId -CEQ $roles[$i*2]);if($w.Count-ne1-or$w[0].mountId-cne$roles[$i*2+1]){throw 'A restored owner is missing or bound to another actor.'}
  $n=ConvertTo-KmcChunk7PairIdN $w[0].ownerId;if($w[0].ownerId-cne$n-or-not$seen.Add($n)){throw 'Restored owner identity is not one exact archive (N) form.'};Assert-KmcOrdinaryInteger $w[0].generation 1 ([long]::MaxValue)
  # The live world snapshot binds the restored owner (pair-owner record ids in N form) to the actor UniqueIds.
  $live=@($m.world.owners|Where-Object{$_.state-ceq'Active'-and(ConvertTo-KmcChunk7PairIdN $_.id)-ceq$n})
  if($live.Count-ne1-or-not(Test-KmcChunk7OwnerActorMatch $live[0].riderId $roles[$i*2])-or-not(Test-KmcChunk7OwnerActorMatch $live[0].mountId $roles[$i*2+1])-or$live[0].generation-ne$w[0].generation){throw 'Restored owner identity or generation differs from the live registry.'}
 }
 if(@($m.slots).Count-ne4){throw 'Owner-seed-derived combat requires its four restored controls.'}
 foreach($rider in @($roles[0],$roles[2])){if((@($m.slots|Where-Object actorId -CEQ $rider|ForEach-Object{$_.kind}|Sort-Object)-join',')-cne'2,3'){throw 'A restored pair lost its exact native controls.'}}
 if(@($m.debt).Count-ne4){throw 'Restored actor debt must be observed for all four actors.'}
 foreach($row in $m.debt){if($row.actorId-cnotin$roles){throw 'Restored debt names a foreign actor.'};Assert-KmcChunk7PersistenceNativeActor $row.saved;Assert-KmcChunk7PersistenceNativeActor $row.restored
  if(-not(Test-KmcChunk7DebtWithinClock $row.saved $row.restored ([double]$m.elapsedSeconds))){throw 'Restored debt was lost or regranted before combat.'}}
 $src=$o.ownerSeedDerivedSource
 Assert-KmcExactProperties $src @('contract','ancestry','acquisitionProof','ordinaryProducerAncestry','producerRunId','producerArtifactSha256','derivedManualSha256','consumerRunId','consumerArtifactSha256','seedId','seedSha256','candidates') 'owner-seed-derived combat source'
 Assert-KmcBootstrapInventoryString $src.contract 'owner-seed-derived-combat-source-v1';Assert-KmcBootstrapInventoryString $src.ancestry 'owner-seed-derived'
 Assert-KmcChunk7Boolean $src.acquisitionProof $false;Assert-KmcChunk7Boolean $src.ordinaryProducerAncestry $false
 foreach($pair in @(@('producerRunId','sourceRunId'),@('producerArtifactSha256','sourceArtifactSha256'),@('derivedManualSha256','sourceArchiveSha256'),@('consumerRunId','checkpointSourceRunId'),@('consumerArtifactSha256','checkpointSourceArtifactSha256'),@('seedId','seedId'),@('seedSha256','seedSha256'))){Assert-KmcBootstrapInventoryString $src.($pair[0]) $s.($pair[1])}
 Assert-KmcChunk7OwnerSeedCandidates $Request $src.candidates
 Assert-KmcChunk7OwnerSeedTargetAuthorization $Request $o.targetAuthorization
}
# The owner-approved runtime-only stock Mammoth target contract shared by derived combat and the
# owner-seed checkpoint producer's combat checkpoints: the derived-copy basis, never Working.
function Assert-KmcChunk7OwnerSeedTargetAuthorization($Request,$Target){
 $t=$Target;$s=$Request.ownedPairs
 if($null-eq$t){throw 'Owner-seed-derived combat lacks its target authorization.'}
 Assert-KmcExactProperties $t @('contract','basis','ownerApproval','workingSha256','seedSha256','derivedManualSha256','producerRunId','producerArtifactSha256','consumerRunId','consumerArtifactSha256','area','riderPosition','position','placementValidated') 'owner-seed-derived target authorization'
 Assert-KmcBootstrapInventoryString $t.contract 'chunk7-combat-target-authorization-v1';Assert-KmcBootstrapInventoryString $t.basis 'owner-seed-derived-disposable-copy'
 if($t.ownerApproval-isnot[string]-or-not$t.ownerApproval.StartsWith('docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md',[StringComparison]::Ordinal)-or$null-ne$t.workingSha256){throw 'Derived target authorization must cite the owner approval and never the Working identity.'}
 foreach($pair in @(@('seedSha256','seedSha256'),@('derivedManualSha256','sourceArchiveSha256'),@('producerRunId','sourceRunId'),@('producerArtifactSha256','sourceArtifactSha256'),@('consumerRunId','checkpointSourceRunId'),@('consumerArtifactSha256','checkpointSourceArtifactSha256'))){Assert-KmcBootstrapInventoryString $t.($pair[0]) $s.($pair[1])}
 Assert-KmcBootstrapInventoryString $t.area $Request.persistenceLoad.area;Assert-KmcChunk7Boolean $t.placementValidated $true
}
# Scenario 3's unrelated companion: exactly one ordinary native command with its own single
# native cost, and no admission, cancellation, cost or command change on either pair.
function Assert-KmcChunk7OwnerSeedCompanionAction($Request,$Observations){
 $o=$Observations;$s=$Request.ownedPairs;$roles=Get-KmcChunk7OwnedPairIds $Request
 if($null-eq$o.PSObject.Properties['companionAction']){throw 'Owner-seed-derived RT lacks its unrelated companion action.'}
 $c=$o.companionAction
 Assert-KmcExactProperties $c @('contract','kind','justification','actorId','targetId','traceSequenceBefore','before','clicked','command','commandType','admitted','selectionAfterInput','after','traceSequenceAfter','accountedEvents','companionCommandCount','companionCostEvents','pairRefusedProposals','pairAccountedEvents','foreignCompanionEvents','nativeStop','companionInputs','declaredLeaseCommands','terminal') 'owner-seed companion action'
 Assert-KmcBootstrapInventoryString $c.contract 'owner-seed-unrelated-companion-action-v1';Assert-KmcBootstrapInventoryString $c.kind 'ordinary-native-attack'
 Assert-KmcBootstrapInventoryString $c.actorId $s.companionId;Assert-KmcBootstrapInventoryString $c.targetId $o.targetId;Assert-KmcBootstrapInventoryString $c.commandType 'Kingmaker.UnitLogic.Commands.UnitAttack'
 if($c.actorId-cin$roles-or$c.selectionAfterInput-cnotin$roles-or[string]::IsNullOrWhiteSpace($c.justification)){throw 'The companion action escaped its unrelated actor or selection.'}
 foreach($key in @('clicked','terminal')){Assert-KmcChunk7Boolean $c.$key $true}
 Assert-KmcOrdinaryInteger $c.command ([int]::MinValue) ([int]::MaxValue);if($c.command-eq0){throw 'The companion command has the null identity.'};Assert-KmcOrdinaryInteger $c.declaredLeaseCommands 1 1
 Assert-KmcOrdinaryInteger $c.traceSequenceBefore 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $c.traceSequenceAfter ([int]$c.traceSequenceBefore) ([int]::MaxValue)
 $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$c.traceSequenceBefore-and$_.sequence-le$c.traceSequenceAfter-and$_.boundary-cin@('admission-before','admission-after','cost-before','cost-after','actor-cost-before','actor-cost-after','command-interrupt-before','command-interrupt-after')})
 if((ConvertTo-KmcChunk7Canonical @($window))-cne(ConvertTo-KmcChunk7Canonical @($c.accountedEvents))){throw 'The companion window events differ from the native trace.'}
  # Native RT party brains keep proposing UnitAttacks on a pair actor's ManualTarget; KMC refuses each
 # before native Run. Only closed refused-before-Run pairs are tolerated, derived here independently.
 foreach($w in @($c.before,$c.admitted,$c.after)){if($null-eq$w.PSObject.Properties['pairCommands']){throw 'Companion window lacks the pair command custody.'}}
 # The window opens only on a settled diagnostic target: a native stand-up from prone provokes every engaging unit.
 $settled=$c.before.PSObject.Properties['target'];if($null-eq$settled){throw 'Companion window lacks the diagnostic target state.'};$settled=$settled.Value;Assert-KmcBootstrapInventoryString $settled.id $c.targetId
 foreach($key in @('prone','proneRequested')){Assert-KmcChunk7Boolean $settled.$key $false};foreach($key in @('conscious','commandsEmpty')){Assert-KmcChunk7Boolean $settled.$key $true}
 $slotted=@(foreach($w in @($c.before,$c.admitted,$c.after)){foreach($side in @('a','b')){foreach($slot in @($w.pairCommands.$side)){$slot.identity}}})
 $pair=@($window|Where-Object{$_.commandActor-cin$roles-or$_.state.actor-cin$roles});$refused=[Collections.Generic.List[object]]::new();$accepted=[Collections.Generic.HashSet[long]]::new()
 foreach($group in @($pair|Where-Object{$_.command-is[long]-or$_.command-is[int]}|Group-Object{[long]$_.command})){
  $rows=@($group.Group|Sort-Object sequence);$identity=[long]$group.Name;$all=@($window|Where-Object{$null-ne$_.command-and[long]$_.command-eq$identity});$actor=$rows[0].state.actor
  $closed=$rows.Count-eq2-and$all.Count-eq2-and$rows[0].boundary-ceq'admission-before'-and$rows[1].boundary-ceq'admission-after'-and$identity-ne[long]$c.command-and$identity-notin$slotted-and$actor-cin$roles
  foreach($row in $rows){if($row.state.actor-cne$actor-or$row.commandType-cne'Kingmaker.UnitLogic.Commands.UnitAttack'-or$null-ne$row.commandActor-or$row.started-ne$false-or$row.acted-ne$false-or$row.finished-ne$false-or$row.result-cne'None'){$closed=$false}}
  if(-not$closed){continue};foreach($row in $rows){[void]$accepted.Add([long]$row.sequence)}
  $refused.Add([pscustomobject][ordered]@{actorId=$actor;command=$identity;sequenceBefore=$rows[0].sequence;sequenceAfter=$rows[1].sequence})}
 $touched=@($pair|Where-Object{-not$accepted.Contains([long]$_.sequence)})
 if($touched.Count-ne0-or$c.pairAccountedEvents-ne0){throw 'A pair actor was admitted, cancelled or charged during the companion action.'}
 if((ConvertTo-KmcChunk7Canonical @($refused|Sort-Object sequenceBefore))-cne(ConvertTo-KmcChunk7Canonical @($c.pairRefusedProposals|Sort-Object sequenceBefore))){throw 'The recorded refused pair proposals differ from the native trace.'}
 $mine=@($window|Where-Object{$_.commandActor-ceq$c.actorId-or$_.state.actor-ceq$c.actorId})
 $admitted=@($mine|Where-Object boundary -CEQ 'admission-after');if($admitted.Count-ne1-or$admitted[0].command-ne$c.command-or$admitted[0].commandActor-cne$c.actorId-or$c.companionCommandCount-ne1){throw 'The companion did not take exactly one native command.'}
 if(@($mine|Where-Object{$_.command-ne$c.command}).Count-ne0-or$c.foreignCompanionEvents-ne0){throw 'The companion took a second native command.'}
 $before=@($mine|Where-Object{$_.boundary-ceq'cost-before'-and$_.command-eq$c.command});$after=@($mine|Where-Object{$_.boundary-ceq'cost-after'-and$_.command-eq$c.command})
 if($before.Count-ne1-or$after.Count-ne1-or$c.companionCostEvents-ne2){throw 'The companion action lacks its own single native cost event.'}
 if($after[0].sequence-le$before[0].sequence-or$after[0].state.standard-le$before[0].state.standard-or$after[0].commandType-cne'Kingmaker.UnitLogic.Commands.UnitAttack'){throw 'The companion cost was not her own native Standard spend.'}
 # Her only other input: one native Stop with only her selected, after her command finished natively.
 if((@($c.companionInputs)-join'|')-cne'ordinary-attack-click|native-stop'){throw 'The companion took an undeclared native input.'}
 $stop=$c.nativeStop;Assert-KmcExactProperties $stop @('selectedId','selectionAfterStop','traceSequenceBefore','traceSequenceAfter','companionManualTargetCleared','companionCommandsEmpty') 'owner-seed companion native Stop'
 Assert-KmcBootstrapInventoryString $stop.selectedId $c.actorId;Assert-KmcBootstrapInventoryString $stop.selectionAfterStop $s.riderAId
 foreach($key in @('companionManualTargetCleared','companionCommandsEmpty')){Assert-KmcChunk7Boolean $stop.$key $true}
 Assert-KmcOrdinaryInteger $stop.traceSequenceBefore ([int]$after[0].sequence) ([int]$c.traceSequenceAfter);Assert-KmcOrdinaryInteger $stop.traceSequenceAfter ([int]$stop.traceSequenceBefore) ([int]$c.traceSequenceAfter)
 if(@($mine|Where-Object{$_.sequence-gt$stop.traceSequenceBefore}).Count-ne0){throw 'The companion native Stop admitted, charged or interrupted a command.'} if((ConvertTo-KmcChunk7Canonical $c.before.pairCommands)-cne(ConvertTo-KmcChunk7Canonical $c.after.pairCommands)-or(ConvertTo-KmcChunk7Canonical $c.before.pairCommands)-cne(ConvertTo-KmcChunk7Canonical $c.admitted.pairCommands)){throw 'The companion action reassigned or cancelled a pair command.'}
 foreach($id in $roles){$a=Get-KmcChunk7CombatActor $c.before $id;$b=Get-KmcChunk7CombatActor $c.after $id
  foreach($field in @('standard','move','swift')){if([double]$b.$field-gt[double]$a.$field+0.001){throw 'A pair actor spent native resources during the companion action.'}};if($a.reactions-ne$b.reactions){throw 'A pair actor spent a reaction during the companion action.'}}
 $ownersBefore=@($c.before.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.id)+'|'+$_.generation+'|'+$_.state}|Sort-Object -CaseSensitive);$ownersAfter=@($c.after.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.id)+'|'+$_.generation+'|'+$_.state}|Sort-Object -CaseSensitive)
 if(($ownersBefore-join';')-cne($ownersAfter-join';')-or@($c.after.owners|Where-Object state -CEQ 'Active').Count-ne2){throw 'The companion action changed pair custody.'}
}
# Directive 4 R3: after the first-action Dismount, Horse A spends its own native move on its independent
# turn, then rider A mounts as the first action of its next prepared turn while pair B stays owned. The
# roster decides the adoption: mount-first retains the Horse's participation (no preparation, its spent
# move stands); rider-first prepares it exactly once. Preparations are recounted from the trace.
function Assert-KmcChunk7OwnerSeedFirstActionMount($Request,$Observations){
 $o=$Observations;$s=$Request.ownedPairs;$ids=@($o.actorIds)
 if($null-eq$o.PSObject.Properties['firstActionMount']){throw 'Owner-seed-derived TB lacks its first-action Mount.'}
 $f=$o.firstActionMount;Assert-KmcBootstrapInventoryString $f.contract 'owner-seed-tb-first-action-mount-v1'
 Assert-KmcBootstrapInventoryString $f.riderId $s.riderAId;Assert-KmcBootstrapInventoryString $f.mountId $s.mountAId
 $m=$f.horseMove;foreach($key in @('clicked','terminal')){Assert-KmcChunk7Boolean $m.$key $true}
 if($m.round-ne$o.r1.independentRound-or-not(Test-KmcChunk7FiniteNumber $m.distance)-or[double]$m.distance-le0.02-or$m.command-eq0){throw 'The independent Horse did not take its own native move on its released turn.'}
 $hb=Get-KmcChunk7CombatActor $m.before $s.mountAId;$ha=Get-KmcChunk7CombatActor $m.after $s.mountAId
 if([double]$ha.move-le[double]$hb.move){throw 'The independent Horse move spent no movement.'}
 Assert-KmcChunk7SameCombatBudget $m.before $m.after @($s.riderBId,$s.mountBId)
 $t=$f.mountTurn;Assert-KmcChunk7Boolean $t.terminal $true
 if($t.turnStatus-cnotin@('Preparing','Acting')){throw 'The first-action Mount was not taken in the rider''s prepared turn.'}
 $retain=$Request.scenario-ceq'chunk7-two-pair-combat-mount-first'-or(Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-two-pair-combat-mount-first'
 $expected=if($retain){'RetainPartnerParticipation'}else{'PreparePartnerThisRound'};$calls=if($retain){0}else{1}
 if($t.expectedDisposition-cne$expected-or-not([string]$t.adoptionObservation).Contains('disposition='+$expected)-or-not([string]$t.adoptionObservation).Contains(';partnerPrepareCalls='+$calls)){throw 'First-action Mount did not take the roster''s adoption disposition.'}
 if(-not([string]$t.transactionObservation).StartsWith('committed;')){throw 'First-action Mount did not commit its adoption transaction.'}
 if($t.round-ne$m.round+$(if($retain){0}else{1})){throw 'The first-action Mount was not on the rider''s next prepared turn after the Horse moved.'}
 $prepares=@($o.allocationTrace.events|Where-Object{$_.boundary-ceq'prepare-after'-and$_.state.actor-ceq$s.mountAId-and$_.frame-ge$t.before.frame-and$_.frame-le$t.after.frame})
 if($prepares.Count-ne$calls-or$t.horseGrantsDuringMount-ne$calls){throw 'First-action Mount prepared the partner the wrong number of times.'}
 $before=Get-KmcChunk7CombatActor $t.before $s.mountAId;$after=Get-KmcChunk7CombatActor $t.after $s.mountAId
 if($retain){foreach($field in @('standard','move','swift')){if([double]$after.$field-lt[double]$before.$field-0.001){throw 'Retained participation replenished the Horse''s prior native spending.'}};if([double]$after.move-le0.001){throw 'Retained participation lost the Horse''s spent move.'}}
 if($retain-and@($o.allocationTrace.events|Where-Object{$_.state.actor-ceq$s.mountAId-and($_.boundary-clike'prepare-*'-or$_.boundary-clike'clear-*')-and$_.frame-ge$m.after.frame-and$_.frame-le$t.after.frame}).Count-ne0){throw 'Retained participation prepared or cleared the Horse between its move and the Mount.'}
 Assert-KmcChunk7SameCombatBudget $t.before $t.after @($s.riderBId,$s.mountBId)
 $ownersBefore=@($t.before.owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.id)-ceq(ConvertTo-KmcChunk7PairIdN $f.neighbourOwnerId)});$ownersAfter=@($t.after.owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.id)-ceq(ConvertTo-KmcChunk7PairIdN $f.neighbourOwnerId)})
 if($ownersBefore.Count-ne1-or$ownersAfter.Count-ne1-or$ownersBefore[0].generation-ne$f.neighbourGeneration-or$ownersAfter[0].generation-ne$f.neighbourGeneration-or$ownersAfter[0].state-cne'Active'){throw 'Pair B''s ownership changed during pair A''s first-action Mount.'}
}
# Directive 4 R1: one real ordinary player hostile click by rider A while native party-brain proposals
# continue. Re-derived from the trace: exactly one rider-A native attack on the target before the
# player's Stop, no KMC command on pair B, no player-facing warning, pair B unspent, custody unchanged.
function Assert-KmcChunk7OwnerSeedPlayerClick($Request,$Observations){
 $o=$Observations;$s=$Request.ownedPairs;$roles=Get-KmcChunk7OwnedPairIds $Request
 if($null-eq$o.PSObject.Properties['playerClick']){throw 'Owner-seed-derived RT lacks its player-click integrity window.'}
 $p=$o.playerClick;$pairAttack='KingmakerMountedCombat.Integration.MountedPairAttackCommand'
 Assert-KmcBootstrapInventoryString $p.contract 'owner-seed-rt-player-click-integrity-v1';Assert-KmcBootstrapInventoryString $p.actorId $s.riderAId;Assert-KmcBootstrapInventoryString $p.targetId $o.targetId
 foreach($key in @('clicked','terminal')){Assert-KmcChunk7Boolean $p.$key $true}
 foreach($w in @($p.before,$p.admitted,$p.after)){foreach($key in @('intent','manualTargets','pairCommands','target')){if($null-eq$w.PSObject.Properties[$key]){throw 'Player-click window lacks its intent, target or command custody.'}}}
 if([long]$p.admitted.intent.aStarts-ne[long]$p.before.intent.aStarts+1-or[long]$p.after.intent.bStarts-ne[long]$p.before.intent.bStarts-or$p.after.intent.bActive-ne$false){throw 'The player click did not start exactly one pair A stock intent, or touched pair B.'}
 Assert-KmcOrdinaryInteger $p.traceSequenceBefore 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $p.traceSequenceAfter ([int]$p.traceSequenceBefore) ([int]::MaxValue)
 $stop=$p.stop;Assert-KmcBootstrapInventoryString $stop.selectedId $s.riderAId;Assert-KmcChunk7Boolean $stop.intentAfterStop $false
 Assert-KmcOrdinaryInteger $stop.traceSequenceBefore ([int]$p.traceSequenceBefore) ([int]$p.traceSequenceAfter)
 $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$p.traceSequenceBefore-and$_.sequence-le$p.traceSequenceAfter})
 $costs=@($window|Where-Object{$_.boundary-ceq'cost-after'-and$_.state.actor-ceq$s.riderAId-and$_.commandType-ceq$pairAttack-and$_.sequence-le$stop.traceSequenceBefore})
 if($costs.Count-ne1-or$costs[0].commandTarget-cne$o.targetId){throw 'The player click did not cause exactly one rider-A native attack on its target before Stop.'}
 if(@($p.riderAttackCosts).Count-ne1-or$p.riderAttackCosts[0].sequence-ne$costs[0].sequence){throw 'The recorded player-click attack differs from the native trace.'}
 if(@($window|Where-Object{$_.commandType-ceq$pairAttack-and$_.state.actor-cin@($s.riderBId,$s.mountBId)}).Count-ne0-or$p.pairBKmcCommandRows-ne0){throw 'Pair B received a KMC attack command during the player click.'}
 if(@($p.warnings).Count-ne0){throw 'A player-facing warning appeared while only native proposals were refused.'}
 foreach($id in @($s.riderBId,$s.mountBId)){$a=Get-KmcChunk7CombatActor $p.before $id;$b=Get-KmcChunk7CombatActor $p.after $id
  foreach($field in @('standard','move','swift')){if([double]$b.$field-gt[double]$a.$field+0.001){throw 'Pair B spent native resources during the player click.'}};if($a.reactions-ne$b.reactions){throw 'Pair B spent a reaction during the player click.'}}
 $ownersBefore=@($p.before.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.id)+'|'+$_.generation+'|'+$_.state}|Sort-Object -CaseSensitive);$ownersAfter=@($p.after.owners|ForEach-Object{(ConvertTo-KmcChunk7PairIdN $_.id)+'|'+$_.generation+'|'+$_.state}|Sort-Object -CaseSensitive)
 if(($ownersBefore-join';')-cne($ownersAfter-join';')){throw 'The player click changed pair custody.'}
}

# The selected owner-seed destination is fixed and already visited. Other
# owner-seed families still forbid area targets rather than inheriting this grant.
function Assert-KmcChunk7OwnerSeedAreaTarget($Selection,[AllowNull()][string]$Checkpoint,$Target){
 if($Checkpoint-cne'multi-area-autosave'){
  if($null-ne$Target){throw 'Only the selected owner-seed area checkpoint carries an area target.'};return
 }
 if($Selection.case-cnotin@('owner-seed-checkpoint-save','owner-seed-checkpoint-load')){throw 'Owner-seed area target requires its exact checkpoint family.'}
 Assert-KmcBootstrapInventoryString $Selection.sourceArea 'b10e86516b6437d45a629931c30936fe'
 Assert-KmcExactProperties $Target @('enterPoint','area','autoSaveMode') 'owner-seed controlled cross-entry target'
 foreach($spec in @(@('enterPoint','fd225a93828d26449803a4847947f8c1'),@('area','ead426a6c23d39548a670ee515d77df4'),@('autoSaveMode','AfterEntry'))){Assert-KmcBootstrapInventoryString $Target.($spec[0]) $spec[1]}
}

. (Join-Path $PSScriptRoot 'Chunk7LoadRefusalEvidence.ps1')
