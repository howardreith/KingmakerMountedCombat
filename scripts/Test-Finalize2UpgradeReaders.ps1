# F2 upgrade-chain reader: synthetic producer and consumer evidence (archives and screenshots written to a temporary
# root) and targeted negatives, plus the source contracts the route depends on. No game, no build. A reader-only
# assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime\Finalize2UpgradeEvidence.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-F2($Value){ $Value | ConvertTo-Json -Depth 40 | ConvertFrom-Json }
$repo=Split-Path -Parent $PSScriptRoot
$ids=Get-KmcFinalize2UpgradeIdentities
$R='11111111-1111-1111-1111-111111111111'; $M='22222222-2222-2222-2222-222222222222'; $P=$ids.publicControl
function A([int]$i){ ('{0:x32}' -f ($i+1)) }
function Bar([hashtable]$Change){ $k=@(); for($i=0;$i -lt 22;$i++){ $k+=$(if($Change.ContainsKey($i)){$Change[$i]}elseif($i -lt 14){A $i}else{''}) }; ,$k }
function Vis($Bar){ $e=@(); for($i=0;$i -lt 18;$i++){ $e+=[ordered]@{active=$true;key=$Bar[$i];settingsIndex=$i} }; [ordered]@{manager=$true;managerActive=$true;selectedIsRider=$true;mainBar=$e} }
function Row([string]$Kind,$Detail,[string]$Relation='Mounted'){ [ordered]@{runId=$script:runId;scenario='persistence-p05-load';processId=$script:procId;kind=$Kind;checkpoint=$script:case;source=('c'*40);dll=('d'*64);
 relationship=$Relation;rider=[ordered]@{Id=$R};mount=[ordered]@{Id=$M};controls=[ordered]@{DuplicateFactCount=0};detail=$Detail} }
function Counters([int]$Placed,[int]$Legacy,[int]$Observed){ [ordered]@{initialPlacement=$Placed;legacySlotRepair=$Legacy;initialPlacementObservations=$Observed} }
function Actor($Bar,$Indices,$Counters,[string]$Relation='Mounted',$Placements=@()){ [ordered]@{relationship=$Relation;placementRecorded=$true;factActive=$true;indices=@($Indices);bar=$Bar;placements=@($Placements);exposure=[ordered]@{hiddenExposed=@()};counters=$Counters} }
function W([int]$Frames,[bool]$Removed=$false){ [ordered]@{frames=$Frames;removed=$Removed;movedFrames=0;notLiveFrames=0;factReplacedFrames=0;reinsertedFrames=0} }
function S([string]$A,[int]$I,[int]$K){ [ordered]@{ActorId=$A;Index=$I;Kind=$K} }
function Click([bool]$Mounting,[string]$Route){ $c=[ordered]@{route=$Route;mounting=$Mounting;slotAbilityBefore=$P;slotAbilityAfter=$P;slotHeldLiveFact=$true;slotDataRestored=$true;caster=$R;refusalDelta=0}
 if($Mounting){ $c.handlerAfterSlot=$ids.mountShell;$c.targetClicked=$true;$c.target=$M;$c.commandBlueprint=$ids.mountShell;$c.castDelta=1 } else { $c.handlerAfterSlot=$null;$c.target=$R;$c.commandBlueprint=$ids.dismountShell;$c.castDelta=0 }; $c }
function Strings{ [ordered]@{tableFound=$true;packPresent=$true;keys=@([ordered]@{key='k1';expected='Mount / Dismount';present=$true;actual='Mount / Dismount'});
 controls=@([ordered]@{guid=$P;name='Mount / Dismount';hidden=$false},[ordered]@{guid=$ids.mountShell;name='Mount Companion';hidden=$true},[ordered]@{guid=$ids.dismountShell;name='Dismount';hidden=$true},[ordered]@{guid=$ids.mountedChargeShell;name='Charge';hidden=$true})} }
function Shot([string]$Name,[string]$Root){ $file='movement-visuals/finalize-f2-p05-'+$Name+'.png'; $full=Join-Path $Root ($file.Replace('/','\')); [void](New-Item -ItemType Directory -Force (Split-Path -Parent $full))
 [IO.File]::WriteAllBytes($full,[Text.Encoding]::ASCII.GetBytes('png:'+$Name)); [ordered]@{name=$Name;file=$file;sha256=(Get-KmcSha256 $full)} }
function Write-Archive([string]$Path,[string]$Text){ [IO.File]::WriteAllBytes($Path,[Text.Encoding]::ASCII.GetBytes($Text)); [ordered]@{sha=(Get-KmcSha256 $Path);length=(Get-Item -LiteralPath $Path).Length} }
function Accept($Request,$Rows,$Game,$SaveRoot,$Parent){ try { Assert-KmcFinalize2UpgradeEvidence -Request $Request -Rows $Rows -GameResult $Game -SaveRoot $SaveRoot -EvidenceParent $Parent; $true } catch { Write-Output ('      reader: '+$_.Exception.Message); $false } }
function Refuse($Request,$Rows,$Game,$SaveRoot,$Parent,[string]$Name){ try { Assert-KmcFinalize2UpgradeEvidence -Request $Request -Rows $Rows -GameResult $Game -SaveRoot $SaveRoot -EvidenceParent $Parent; Check $false $Name } catch { Check $true $Name } }
function Kind($Rows,[string]$K){ @($Rows | Where-Object kind -CEQ $K)[0] }

$temp=Join-Path ([IO.Path]::GetTempPath()) ('kmc-f2-upgrade-'+[Guid]::NewGuid().ToString('N'))
try {
 # ---- producer -------------------------------------------------------------------------------------------------------
 $parent=Join-Path $temp 'runtime-evidence'; $pEv=Join-Path $parent '20991231-finalize-f2-test-p'; $pSave=Join-Path $temp 'p-saves'
 [void](New-Item -ItemType Directory -Force $pEv); [void](New-Item -ItemType Directory -Force $pSave)
 $quick=Write-Archive (Join-Path $pSave 'Quick_1.zks') 'legacy'
 $script:runId='20991231-finalize-f2-test-p'; $script:procId=100; $script:case='quick'
 $pRequest=[pscustomobject][ordered]@{runId=$script:runId;scenario='persistence-p05-load';persistenceCase='quick';upgradeChain=$true;finalize2Controls=$true;commit=('c'*40);dllSha256=('d'*64);evidenceRoot=$pEv
  persistenceLoad=[pscustomobject]@{fileName='Quick_1.zks';sha256=$quick.sha;area=('a'*32);gameId='game'}}
 $loaded=Bar @{14=$P}; $before=Bar @{13=''}; $placed=Bar @{13=$P}; $swapped=Bar @{9=$P;13=(A 9)}; $removedBar=Bar @{9='';13=(A 9)}
 $kept=Write-Archive (Join-Path $pSave $ids.keptLeaf) 'kept'; $gone=Write-Archive (Join-Path $pSave $ids.removedLeaf) 'removed'
 function Snapshot($Slots){ [ordered]@{SchemaVersion=3;AreaId=('a'*32);CampaignId='game';Pairs=@([ordered]@{Rider=[ordered]@{Id=$R};Mount=[ordered]@{Id=$M}});Slots=@($Slots)} }
 function Written([int]$O,[string]$Name,[string]$Leaf,$Arch,[int]$Index,$Bar,$Slots){ Row 'native-write-complete' ([ordered]@{ordinal=$O;name=$Name;path=(Join-Path $pSave $Leaf);sha256=$Arch.sha;length=$Arch.length;nativeType='Manual';
  nativeCallback=$true;operation='None';sourceSha256=$quick.sha;controlIndex=$Index;barBeforeSave=$Bar;scan=[ordered]@{publicHits=0;hitCount=0;members=5};snapshot=(Snapshot $Slots)}) }
 $pRows=@(
  (Row 'f2-cold-observed' ([ordered]@{contract='finalize-f2-upgrade-public-control-v1';role='producer';sourceSchema=2;savedRows=@((S $R 14 2),(S $R 15 3));liveRows=@((S $R 14 6),(S $R 0 7));
   actor=(Actor $loaded @(14) (Counters 0 1 0));retiredHelpersSelected=$false;identities=[ordered]@{publicControl=$P;mountShell=$ids.mountShell;dismountShell=$ids.dismountShell};legacyIndex=14;
   retiredRows=@([ordered]@{index=15;kind=3;now=''})})),
  (Row 'initial' $null),(Row 'movement-dispatched' $null),(Row 'movement-completed' $null),(Row 'attack-dispatched' $null),(Row 'attack-delivered' ([ordered]@{rules=1;rolls=1})),(Row 'usable-continuation-complete' $null),
  (Row 'f2-setup-removed-in-place' ([ordered]@{visibleBefore=(Vis $loaded);actor=(Actor $loaded @(14) (Counters 0 1 0))})),
  (Row 'f2-setup-freed-slot' ([ordered]@{watch=(W 30 $true);freedIndex=13;freedHeld=(A 13)})),
  (Row 'f2-placement-armed' ([ordered]@{visibleBefore=(Vis $before);actor=(Actor $before @() (Counters 0 1 0))})),
  (Row 'f2-placement-visible' ([ordered]@{shown=$true;framesObserved=2;visible=(Vis $placed);actor=(Actor $placed @(13) (Counters 1 1 1) 'Mounted' @([ordered]@{index=13;frame=50;mainBarBefore=@($before | Select-Object -First 14)}))})),
  (Row 'f2-cycle-dismount-click' (Click $false 'bar-slot')),
  (Row 'f2-cycle-dismounted' (Actor $placed @(13) (Counters 1 1 1) 'Unmounted') 'Unmounted'),
  (Row 'f2-cycle-mount-click' (Click $true 'bar-slot') 'Unmounted'),
  (Row 'f2-cycle-mounted' ([ordered]@{watch=(W 12);actor=(Actor $placed @(13) (Counters 1 1 1))})),
  (Row 'f2-setup-swapped' ([ordered]@{from=13;to=9;movedKey=(A 9);barBefore=@($placed | Select-Object -First 14);barAfter=@($swapped | Select-Object -First 14)})),
  (Row 'f2-kept-before-save' ([ordered]@{watch=(W 30);rows=@((S $R 9 6),(S $R 0 7))})),
  (Row 'f2-write-requested' $null),
  (Written 1 'KMC_P01' $ids.keptLeaf $kept 9 $swapped @((S $R 9 6),(S $R 0 7))),
  (Row 'f2-setup-removed' $null),
  (Row 'f2-removed-before-save' ([ordered]@{watch=(W 30 $true);rows=@((S $R 0 7))})),
  (Row 'f2-write-requested' $null),
  (Written 2 $ids.removedName $ids.removedLeaf $gone -1 $removedBar @((S $R 0 7))),
  (Row 'f2-complete' ([ordered]@{disclosedSetup=@(1..5 | ForEach-Object { [ordered]@{action=$(if($_ -eq 3){'NativeMountedControlService.BeginLoadedWorld'}else{'edit '+$_})} });shots=@((Shot 'placed' $pEv),(Shot 'removed' $pEv))}))
 )
 $pRows=Copy-F2 $pRows; $pGame=[pscustomobject]@{processId=100}
 Check (Accept $pRequest $pRows $pGame $pSave $parent) 'producer: authentic old row, drawn first placement, cycle, swapped and removed archives accepted'
 function PNeg([scriptblock]$Mutate,[string]$Name){ $r=Copy-F2 $pRows; & $Mutate $r; Refuse $pRequest $r $pGame $pSave $parent $Name }
 PNeg { param($r) (Kind $r 'f2-placement-visible').detail.shown=$false } 'producer: a placement the bar did not draw refuses'
 PNeg { param($r) $v=(Kind $r 'f2-placement-visible').detail.visible.mainBar[13]; $v.key=(A 13); $v.settingsIndex=-1 } 'producer: the stale slot r03 saw refuses'
 $order=[Collections.ArrayList]::new(); $order.AddRange(@(Copy-F2 $pRows | ForEach-Object { $_ })); $ai=[array]::IndexOf(@($order|ForEach-Object{$_.kind}),'f2-placement-armed'); $x=Copy-F2 $order[$ai]; $x.kind='f2-interloper'; $order.Insert($ai+1,$x)
 Refuse $pRequest $order.ToArray() $pGame $pSave $parent 'producer: any observation between the placement event and the drawn bar refuses'
 PNeg { param($r) (Kind $r 'f2-cold-observed').detail.liveRows=@((S $R 14 2),(S $R 0 7)) } 'producer: an unmigrated old row refuses'
 PNeg { param($r) (Kind $r 'f2-cold-observed').detail.retiredRows[0].now=(A 1) } 'producer: a restored retired primary refuses'
 PNeg { param($r) (Kind $r 'f2-cold-observed').detail.retiredHelpersSelected=$true } 'producer: a run on the retired helpers refuses'
 PNeg { param($r) (Kind $r 'f2-kept-before-save').detail.watch.movedFrames=1 } 'producer: a control that moved before the kept save refuses'
 PNeg { param($r) @($r | Where-Object kind -CEQ 'native-write-complete')[0].detail.snapshot.Slots+=(S $R 3 6) } 'producer: an extra row in the kept archive refuses'
 PNeg { param($r) @($r | Where-Object kind -CEQ 'native-write-complete')[0].detail.scan.publicHits=1 } 'producer: a native member naming the public control refuses'
 PNeg { param($r) @($r | Where-Object kind -CEQ 'native-write-complete')[1].detail.snapshot.Slots=@((S $R 9 6),(S $R 0 7)) } 'producer: a removed archive that still holds the control refuses'
 PNeg { param($r) @($r | Where-Object kind -CEQ 'native-write-complete')[0].detail.sha256=('e'*64) } 'producer: archive bytes that differ from the observation refuse'
 PNeg { param($r) (Kind $r 'f2-setup-swapped').detail.barAfter[2]=(A 7) } 'producer: a swap that changed another slot refuses'
 PNeg { param($r) (Kind $r 'f2-complete').detail.disclosedSetup+=[pscustomobject]@{action='extra'} } 'producer: an undisclosed-count setup refuses'
 PNeg { param($r) (Kind $r 'f2-cycle-mount-click').detail.castDelta=0 } 'producer: a mount click without its admitted cast refuses'
 PNeg { param($r) (Kind $r 'f2-cycle-dismount-click').detail.commandBlueprint=$ids.mountShell } 'producer: a dismount click that queued the opposite shell refuses'
 PNeg { param($r) (Kind $r 'attack-delivered').detail.rules=0 } 'producer: a continuation without its native attack refuses'
 PNeg { param($r) (Kind $r 'f2-complete').detail.shots[0].sha256=('f'*64) } 'producer: a screenshot whose bytes differ refuses'
 $failed=@((Copy-F2 $pRows)+@((Copy-F2 (Row 'assertion-failed' $null)))); Refuse $pRequest $failed $pGame $pSave $parent 'producer: a recorded observer failure refuses'
 [IO.File]::WriteAllBytes((Join-Path $pSave 'stray.zks'),[byte[]](1)); Refuse $pRequest (Copy-F2 $pRows) $pGame $pSave $parent 'producer: an extra archive in the producer root refuses'; Remove-Item -LiteralPath (Join-Path $pSave 'stray.zks')
 # The producer's evidence, as the consumer's reader finds it.
 $pRequest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $pEv 'runtime-request.json') -Encoding UTF8
 @($pRows | ForEach-Object { $_ | ConvertTo-Json -Depth 40 -Compress }) | Set-Content -LiteralPath (Join-Path $pEv 'persistence-observations.jsonl') -Encoding UTF8

 # ---- consumer -------------------------------------------------------------------------------------------------------
 $cEv=Join-Path $parent '20991231-finalize-f2-test-c'; $cSave=Join-Path $temp 'c-saves'
 [void](New-Item -ItemType Directory -Force $cEv); [void](New-Item -ItemType Directory -Force $cSave)
 Copy-Item -LiteralPath (Join-Path $pSave $ids.keptLeaf) -Destination $cSave; Copy-Item -LiteralPath (Join-Path $pSave $ids.removedLeaf) -Destination $cSave
 $script:runId='20991231-finalize-f2-test-c'; $script:procId=200; $script:case='manual'
 $cRequest=[pscustomobject][ordered]@{runId=$script:runId;scenario='persistence-p05-load';persistenceCase='manual';upgradeChain=$true;finalize2Controls=$true;commit=('c'*40);dllSha256=('d'*64);evidenceRoot=$cEv
  persistenceLoad=[pscustomobject]@{fileName=$ids.keptLeaf;sha256=$kept.sha;area=('a'*32);gameId='game'};persistenceAlternate=[pscustomobject]@{fileName=$ids.removedLeaf;sha256=$gone.sha;area=('a'*32);gameId='game'}}
 $cRows=@(
  (Row 'initial' $null),
  (Row 'f2-cold-observed' ([ordered]@{contract='finalize-f2-upgrade-public-control-v1';role='consumer';sourceSchema=3;savedRows=@((S $R 9 6),(S $R 0 7));liveRows=@((S $R 0 7),(S $R 9 6));
   actor=(Actor $swapped @(9) (Counters 0 0 0));retiredHelpersSelected=$false;identities=[ordered]@{publicControl=$P;mountShell=$ids.mountShell;dismountShell=$ids.dismountShell}})),
  (Row 'movement-dispatched' $null),(Row 'movement-completed' $null),(Row 'attack-dispatched' $null),(Row 'attack-delivered' ([ordered]@{rules=1;rolls=1})),(Row 'usable-continuation-complete' $null),
  (Row 'f2-cold-visible' ([ordered]@{visible=(Vis $swapped);strings=(Strings)})),
  (Row 'f2-cycle-dismount-click' (Click $false 'bar-slot')),
  (Row 'f2-cycle-dismounted' (Actor $swapped @(9) (Counters 0 0 0) 'Unmounted') 'Unmounted'),
  (Row 'f2-cycle-mount-click' (Click $true 'bar-slot') 'Unmounted'),
  (Row 'f2-cycle-mounted' ([ordered]@{watch=(W 12);actor=(Actor $swapped @(9) (Counters 0 0 0));strings=(Strings)})),
  (Row 'f2-load-requested' $null),
  (Row 'f2-removed-loaded' ([ordered]@{settled=$true;savedRows=@((S $R 0 7));liveRows=@((S $R 0 7));actor=(Actor $removedBar @() (Counters 0 0 0))})),
  (Row 'f2-removed-ticks' ([ordered]@{watch=(W 60 $true)})),
  (Row 'f2-removed-reselected' ([ordered]@{visible=(Vis $removedBar);watch=(W 80 $true)})),
  (Row 'f2-removed-dismount-click' (Click $false 'drawer-entry')),
  (Row 'f2-removed-mount-click' (Click $true 'drawer-entry') 'Unmounted'),
  (Row 'f2-removed-cycled' ([ordered]@{watch=(W 110 $true);actor=(Actor $removedBar @() (Counters 0 0 0));strings=(Strings)})),
  (Row 'f2-complete' ([ordered]@{disclosedSetup=@();shots=@((Shot 'cold-kept' $cEv),(Shot 'cold-removed' $cEv))}))
 )
 $cRows=Copy-F2 $cRows; $cGame=[pscustomobject]@{processId=200}
 Check (Accept $cRequest $cRows $cGame $cSave $parent) 'consumer: restored and drawn slot, cycle, removed control remembered across load, reselection and a control cycle accepted'
 function CNeg([scriptblock]$Mutate,[string]$Name){ $r=Copy-F2 $cRows; & $Mutate $r; Refuse $cRequest $r $cGame $cSave $parent $Name }
 CNeg { param($r) (Kind $r 'f2-removed-loaded').detail.actor.indices=@(13) } 'consumer: a removed control the load put back refuses'
 CNeg { param($r) (Kind $r 'f2-removed-loaded').detail.actor.counters.initialPlacementObservations=1 } 'consumer: a first-acquisition attempt on the removed load refuses'
 CNeg { param($r) (Kind $r 'f2-cold-observed').detail.actor.counters.initialPlacementObservations=1 } 'consumer: a placement attempt on the kept load refuses'
 CNeg { param($r) (Kind $r 'f2-cold-observed').detail.actor.bar[3]=(A 7) } 'consumer: a restored bar that differs from the saved one refuses'
 CNeg { param($r) (Kind $r 'f2-cold-observed').detail.actor.indices=@(13) } 'consumer: a control restored away from its saved slot refuses'
 CNeg { param($r) $e=(Kind $r 'f2-cold-visible').detail.visible.mainBar[9]; $e.key='' } 'consumer: a restored control the bar does not draw refuses'
 CNeg { param($r) (Kind $r 'f2-cycle-mounted').detail.strings.keys[0].present=$false } 'consumer: an unresolved key after the cold load refuses'
 CNeg { param($r) (Kind $r 'f2-cold-visible').detail.strings.controls[2].hidden=$false } 'consumer: an exposed delivery shell refuses'
 CNeg { param($r) $e=(Kind $r 'f2-removed-reselected').detail.visible.mainBar[9]; $e.key=$P } 'consumer: a reselected bar drawing the removed control refuses'
 CNeg { param($r) (Kind $r 'f2-removed-ticks').detail.watch.reinsertedFrames=1 } 'consumer: a reinsertion during update ticks refuses'
 CNeg { param($r) (Kind $r 'f2-removed-mount-click').detail.route='bar-slot' } 'consumer: a removed-control cycle that claims a bar slot refuses'
 CNeg { param($r) (Kind $r 'f2-complete').detail.disclosedSetup=@([pscustomobject]@{action='repair'}) } 'consumer: any consumer setup edit refuses'
 $wrote=@((Copy-F2 $cRows)+@((Copy-F2 (Row 'native-write-complete' $null)))); Refuse $cRequest $wrote $cGame $cSave $parent 'consumer: a consumer that wrote a save refuses'
 Refuse $cRequest (Copy-F2 $cRows) ([pscustomobject]@{processId=100}) $cSave $parent 'consumer: the producer''s own process refuses'
 $other=Copy-F2 $cRequest; $other.persistenceAlternate.sha256=('0'*64); Refuse $other (Copy-F2 $cRows) $cGame $cSave $parent 'consumer: inputs that no recorded producer wrote refuse'
 $cand=Copy-F2 $cRequest; $cand.dllSha256=('9'*64); $cr=Copy-F2 $cRows; foreach($x in $cr){ $x.dll=('9'*64) }; Refuse $cand $cr $cGame $cSave $parent 'consumer: a consumer on another candidate than its producer refuses'
 [IO.File]::WriteAllBytes((Join-Path $cSave $ids.keptLeaf),[Text.Encoding]::ASCII.GetBytes('changed')); Refuse $cRequest (Copy-F2 $cRows) $cGame $cSave $parent 'consumer: a changed input archive refuses'
} finally { if(Test-Path -LiteralPath $temp){ Remove-Item -LiteralPath $temp -Recurse -Force } }

# ---- source contracts the route depends on ------------------------------------------------------------------------------
function Src([string]$Relative){ [IO.File]::ReadAllText((Join-Path $repo $Relative)) }
$controls=Src 'src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs'
$composition=Src 'src/KingmakerMountedCombat/CompositionRoot.cs'
$removal=Src 'src/KingmakerMountedCombat/Integration/MountedRemovalPreparation.cs'
$protocol=Src 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs'
$isolation=Src 'src/KingmakerMountedCombat/Diagnostics/PersistenceIsolationBootstrap.cs'
$gameResult=Src 'scripts/runtime/Test-RuntimeGameResult.ps1'
$requestValidator=Src 'scripts/runtime/Test-RuntimeRequest.ps1'
$runner=Src 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'
$capture=$controls.Substring($controls.IndexOf('internal SavedMountedSlot[] CapturePersistentSlots(bool legacySchema = false)'))
Check ($capture.Substring(0,400).Contains('if (PersistentRowsSuppressed != null && PersistentRowsSuppressed()) return result.ToArray();')) 'removal safeguard: the slot capture emits no control or placement row while suppressed, before any row is built'
Check ($composition.Contains('nativeControls.PersistentRowsSuppressed = () => removalPreparation.CleanupSaveInFlight;')) 'removal safeguard: suppression is bound to the removal cleanup save'
Check ($removal.Contains('internal bool CleanupSaveInFlight => State == RemovalPreparationState.Saving && pendingSave != null &&') -and $removal.Contains('ReferenceEquals(persistence.ActiveSaveRequested, pendingSave);')) 'removal safeguard: only the exact cleanup save in flight suppresses rows'
Check ($protocol.Contains('if (Finalize2Controls.HasValue && (Finalize2Controls != true || UpgradeChain != true))') -and $protocol.Contains('else if (IsFinalize2UpgradeConsumer)')) 'the compiled request admits F2 only beside the upgrade chain, with its consumer''s second archive'
Check ($isolation.Contains('if (request.IsFinalize2UpgradeProducer)') -and $isolation.Contains('InitialSha256 = removed.Sha256, Writable = false')) 'the isolated save root declares the producer''s second output writable and the consumer''s copy read-only'
Check ($gameResult.Contains("elseif(`$f2Upgrade-and`$request.persistenceCase-ceq'quick'){2}") -and $gameResult.Contains("elseif(`$f2Upgrade-and`$request.persistenceCase-ceq'manual'){2}")) 'the game-result quotas are two writes for the producer and two loads for the consumer'
Check ($requestValidator.Contains("throw 'The F2 public-control chain is an explicit true opt-in beside the upgrade chain only.'") -and $requestValidator.Contains("elseif(`$second-and`$f2Consumer){'Manual_2_KMC_F2_REMOVED.zks'}")) 'the external request validator pins the F2 flag and the removed archive leaf'
Check ($runner.Contains("-ArtifactRole upgrade-removed-manual") -and $runner.Contains("if(-not`$UpgradeChain){throw 'The F2 public-control chain runs only on the upgrade chain.'}")) 'the runner stages the removed archive only from its exact role, on the upgrade chain'
Write-Output ('F2 upgrade readers: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
