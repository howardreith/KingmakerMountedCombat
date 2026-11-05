$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceProfileProtection.ps1')
$lab=Get-KmcLabRoot;$count=0
function Pass([scriptblock]$Body){&$Body;$script:count++}
function Refuse([scriptblock]$Body){$bad=$false;try{&$Body|Out-Null}catch{$bad=$true};if(-not$bad){throw 'Missing expected refusal'};$script:count++}
function Clone($Value){$Value|ConvertTo-Json -Depth 30|ConvertFrom-Json}
$base=Read-KmcJson (Join-Path $lab 'runtime-evidence/20261105-finalize-f1c-owner-creator-01/runtime-request.json')
$source=Read-KmcJson (Join-Path $lab 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')
$roles=Get-KmcChunk7NormalPlayerRoles
$candidate=Get-KmcF1CandidatePins
$r=Clone $base;$r.scenario='finalize-f1-finish';$r.commit=$candidate.commit;$r.dllSha256=$candidate.dllSha256;$r.dllMvid=$candidate.dllMvid;$r|Add-Member persistenceLoad $source.persistenceLoad
$r|Add-Member normalPlayer ([pscustomobject]@{contract='finalize-f1-finish-v1';sourceRunId='20261022-c7b-world185-b185s1-sixthhorse-cold';archiveProducerRunId='20261022-c7b-world184-b184s1-sixthhorse-manual';riderId=$roles.riderId;mountId=$roles.mountId;neighborRiderId=$roles.neighborRiderId;neighborMountId=$roles.neighborMountId;kmcAutomationRequest=$false;observer=[pscustomobject]@{version='0.1.0-observer.8';commit=('a'*40);packageSha256=('b'*64);manifestSha256=('c'*64);dllSha256=('d'*64);dllMvid='11111111-2222-3333-4444-555555555555'}})
Pass {Assert-KmcChunk7NormalPlayerDescriptor $r}
Refuse {$bad=Clone $r;$bad.normalPlayer.riderId=$roles.neighborRiderId;Assert-KmcChunk7NormalPlayerDescriptor $bad}
Refuse {$bad=Clone $r;$bad.commit='a'*40;Assert-KmcChunk7NormalPlayerDescriptor $bad}
Refuse {$bad=Clone $r;$bad.scenario='chunk7-normal-player-control';Assert-KmcChunk7NormalPlayerDescriptor $bad}
$manifest=[pscustomobject]@{commit=$r.commit;version=$r.productVersion;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;packageSha256=$candidate.packageSha256}
$observer=New-KmcChunk7NormalPlayerObserverRequest $r ('a'*64) $lab $lab $lab $manifest
Pass {if($observer.timeoutSeconds-ne1100-or$observer.normalPlayer.mode-cne'finalize-f1-finish'){throw 'Wrong F1 mode/deadline'}}
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
Add-Type -Path (Join-Path $managed 'Newtonsoft.Json.dll')
[void][Reflection.Assembly]::LoadFrom((Join-Path (Get-KmcRepositoryRoot) 'bin/Observer/Release/KmcRemovalObserver.dll'))
function NativeRequest($Value){[Newtonsoft.Json.JsonConvert]::DeserializeObject(($Value|ConvertTo-Json -Depth 30),[KmcRemovalObserver.ObserverRequest])}
Pass {$native=NativeRequest $observer;if($native.Validate().Count-ne0){throw ($native.Validate()-join';')}}
Pass {$bad=Clone $observer;$bad.timeoutSeconds=900;if((NativeRequest $bad).Validate().Count-eq0){throw 'Wrong deadline accepted'}}
Pass {$bad=Clone $observer;$bad.archive.sha256='0'*64;if((NativeRequest $bad).Validate().Count-eq0){throw 'Wrong archive accepted'}}
Pass {$bad=Clone $observer;$bad.normalPlayer.riderId=$roles.neighborRiderId;if((NativeRequest $bad).Validate().Count-eq0){throw 'Wrong actor accepted'}}
Pass {$bad=Clone $observer;$bad.normalPlayer.mode='controls';$bad.timeoutSeconds=600;if((NativeRequest $bad).Validate().Count-ne0){throw 'Existing controls mode changed'}}
$snap=Read-KmcPersistenceProfileSnapshot (Join-Path $lab 'runtime-backups/profile-20261105-finalize-f1c-owner-creator-01/snapshot.json')
$after=[IO.File]::ReadAllText((Join-Path $lab 'runtime-evidence/20261105-finalize-f1c-owner-creator-01/preferences-after-exit.json'))
Pass {if(@(Get-KmcF1CreationPreferenceDelta $snap.playerPrefsJson $after).Count-ne14){throw 'Expected14 observed preference changes'}}
Pass {if(@(Get-KmcF1CreationPreferenceDelta $snap.playerPrefsJson $snap.playerPrefsJson).Count-ne0){throw 'Unchanged preferences differ'}}
Refuse {$a=ConvertFrom-Json $after;$a[0].value='AA==';Get-KmcF1CreationPreferenceDelta $snap.playerPrefsJson ('['+(($a|ForEach-Object{$_|ConvertTo-Json -Compress})-join',')+']')}
Refuse {$a=ConvertFrom-Json $after;$a=@($a|Where-Object name -CNE 'AutoLevelup_h3494002937');Get-KmcF1CreationPreferenceDelta $snap.playerPrefsJson ('['+(($a|ForEach-Object{$_|ConvertTo-Json -Compress})-join',')+']')}
Pass {if(@(Get-KmcF1FinishChecks).Count-ne48){throw 'Unexpected exact check set'}}
Pass {$s=Get-KmcF1FinishSource;if($s.descriptor.sha256-cne'b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'-or$s.receiptPins.Count-ne16){throw 'Closed F1 source differs'}}
Refuse {$pins=@(Get-KmcChunk7SixPairCheckpointPins);$pins[11].sha256='0'*64;Assert-KmcChunk7SixPairCheckpointPins $pins}
$n=$r.normalPlayer
function Presence { [pscustomobject]@{modEntries=@('KingmakerLevelUpSelectionScrollFix:active','KingmakerMountedCombat:active','KmcRemovalObserver:active');kmcRuntimeArguments=@();kmcAssemblies=1;kmcDllSha256=$r.dllSha256;kmcMvid=$r.dllMvid;observerDllSha256=$n.observer.dllSha256;observerMvid=$n.observer.dllMvid;rootPresent=$true;automationHostField='null';staticAutomationHost='null';measurementSession='null';movementTelemetry='null';overlayObjects=0;effectiveSettings=[pscustomobject]@{EnableDiagnosticOverlay=$false}} }
$o=[pscustomobject]@{routing=[pscustomobject]@{observedSavePath=(Join-Path $lab 'Saved Games');observedStashFolder=(Join-Path $lab 'Areas')};f1Creators=[pscustomobject]@{closed=2;rankRemoved=$true;fresh=$true;nativeRefreshResidue=[pscustomobject]@{rows=@([pscustomobject]@{featureGuid='a7d2e94c6f1b4830b5c9e28d17f3a604';toggleIsOn=$true;groupAllowSwitchOff=$false})};selectedBefore=[pscustomobject]@{toggleIsOn=$true;selectedInCurrentPreview=$true};selectedNavigation=$true;removedRowStaleRead=$false;removedRow=[pscustomobject]@{toggleIsOn=$false;interactable=$false;selectedInCurrentPreview=$false;graphicAlpha=0.0};reselected=[pscustomobject]@{toggleIsOn=$true;selectedInCurrentPreview=$true};reselectedNavigation=$true;freshMounted=[pscustomobject]@{toggleIsOn=$false;selectedInCurrentPreview=$false};comparison=[pscustomobject]@{toggleIsOn=$true;selectedInCurrentPreview=$true;activeOnRows=1;overlapSeen=$false;distinctFeaturesSelected=2;featureGuid='b'*32};events=@('creator-open','creator-cancel','creator-open','creator-cancel'|ForEach-Object{[pscustomobject]@{event=$_}})};presenceAtMenu=(Presence);profileBefore=@{};world=[pscustomobject]@{area=$r.persistenceLoad.area;gameId=$r.persistenceLoad.gameId;inCombat=$false};controlsBefore=@{};presenceLoaded=(Presence);f1PresentationSetup=[pscustomobject]@{riderId=$n.riderId;featureGuid='a7d2e94c6f1b4830b5c9e28d17f3a604';toggleGuid='3c81f5a02be74d96a4e07d1c92b6f385';ordinaryAcquisitionProven=$false;saveWriteAuthorized=$false};f1Reaction=[pscustomobject]@{observedOnTransition=$true;observedOffTransition=$true};inputs=@([pscustomobject]@{label='mount';casterId=$n.riderId;targetId=$n.mountId;attachedTo=$n.mountId;result='Success';clicked=$true;commandCreated=$true;neighborUnchanged=$true});controlsAfter=@{};presenceAfter=(Presence);archive=[pscustomobject]@{sha256Before=$r.persistenceLoad.sha256;path=(Join-Path (Join-Path $lab 'Saved Games') $r.persistenceLoad.fileName)};profileAfter=@{};log=[pscustomobject]@{kmcErrors=0}}
$result=[pscustomobject]@{checks=@(Get-KmcF1FinishChecks|ForEach-Object{[pscustomobject]@{name=$_;passed=$true}});observations=$o}
Pass {Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $result}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.fresh=$false;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.closed=1;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1PresentationSetup.saveWriteAuthorized=$true;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
# The F1c defect's own state: a disabled, unselected row whose toggle and graphic stayed on.
Refuse {$bad=Clone $result;$bad.observations.f1Creators.removedRow.toggleIsOn=$true;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.removedRow.graphicAlpha=1.0;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Pass {$ok=Clone $result;$ok.observations.f1Creators.removedRow.graphicAlpha=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $ok}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.removedRowStaleRead=$true;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.nativeRefreshResidue=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.nativeRefreshResidue.rows[0].featureGuid='b'*32;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.selectedNavigation=$false;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.reselectedNavigation=$false;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.freshMounted.toggleIsOn=$true;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.freshMounted=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.reselected=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.comparison=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.comparison.distinctFeaturesSelected=1;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.selectedBefore=$null;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.reselected.toggleIsOn=$false;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.comparison.activeOnRows=2;Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.f1Creators.comparison.featureGuid='a7d2e94c6f1b4830b5c9e28d17f3a604';Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
Refuse {$bad=Clone $result;$bad.observations.presenceLoaded.modEntries=@('KingmakerMountedCombat:active','KmcRemovalObserver:active');Assert-KmcF1FinishObserverResult $r ([pscustomobject](Clone $observer)) $bad}
# The request validator runs only inside the live transaction; WhatIf never reaches it.
# The next request's exact shape: the retained refused request under the corrected identities.
$packageManifest=Join-Path $lab 'artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1d-diagnostic.zip.manifest.json'
$observerManifest=Read-KmcJson (Join-Path $lab 'artifacts/KmcRemovalObserver-0.1.0-observer.8-finalize-f1d-diagnostic.zip.manifest.json')
$next=Read-KmcJson (Join-Path $lab 'runtime-evidence/20261105-finalize-f1-finish-01/runtime-request.json')
$next.commit=$candidate.commit;$next.dllSha256=$candidate.dllSha256;$next.dllMvid=$candidate.dllMvid
$next.qualificationSuite.suiteId='finalize-f1d-'+$candidate.commit.Substring(0,8);$next.qualificationSuite.snapshotSha256='0'*64
$next.normalPlayer.observer.version=[string]$observerManifest.version;$next.normalPlayer.observer.commit=[string]$observerManifest.commit;$next.normalPlayer.observer.packageSha256=[string]$observerManifest.packageSha256;$next.normalPlayer.observer.dllSha256=[string]$observerManifest.dllSha256;$next.normalPlayer.observer.dllMvid=[string]$observerManifest.dllMvid
$preflight=Join-Path (Get-KmcRepositoryRoot) 'obj/f1-next-request.json';[void](New-Item -ItemType Directory -Force (Split-Path $preflight))
function WritePreflight($Value){[IO.File]::WriteAllText($preflight,($Value|ConvertTo-Json -Depth 40),(New-Object Text.UTF8Encoding $false))}
Pass {WritePreflight $next;& (Join-Path $PSScriptRoot 'runtime/Test-RuntimeRequest.ps1') -RequestPath $preflight -PackageManifestPath $packageManifest|Out-Null}
Refuse {$bad=Clone $next;$bad.scenario='finalize-f1-finish-unlisted';WritePreflight $bad;& (Join-Path $PSScriptRoot 'runtime/Test-RuntimeRequest.ps1') -RequestPath $preflight -PackageManifestPath $packageManifest|Out-Null}
Refuse {$bad=Clone $next;$bad.dllSha256='0'*64;WritePreflight $bad;& (Join-Path $PSScriptRoot 'runtime/Test-RuntimeRequest.ps1') -RequestPath $preflight -PackageManifestPath $packageManifest|Out-Null}
Write-Host ('F1 FINISH FOCUSED PASS='+$count+' FAIL=0; no game, staging or preference mutation.')
