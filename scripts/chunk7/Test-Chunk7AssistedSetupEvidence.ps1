[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$RepositoryRoot,
    [Parameter(Mandatory=$true)][string]$ReaderPath,
    [Parameter(Mandatory=$true)][string]$FixtureManifestPath,
    [Parameter(Mandatory=$true)][string]$OutputRoot
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. $ReaderPath
$output=Assert-KmcChildPath $OutputRoot (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated assisted evidence checks'
if(Test-Path -LiteralPath $output){throw 'Preserve earlier assisted evidence results.'}
[void][IO.Directory]::CreateDirectory($output)
$clock=[Diagnostics.Stopwatch]::StartNew()
$manifest=Get-Content -LiteralPath $FixtureManifestPath -Raw|ConvertFrom-Json
if($manifest.kind-cne'actual-assisted-setup-emitter-fixtures'-or$manifest.outputs-isnot[Array]-or$manifest.outputs.Count-ne6){throw 'Six actual Capture fixtures are required.'}
$inputPaths=@($PSCommandPath,$ReaderPath,$FixtureManifestPath)+@($manifest.outputs|ForEach-Object path)+@(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -Filter '*.ps1' -File|ForEach-Object FullName)
$inputPins=@($inputPaths|Sort-Object -Unique|ForEach-Object{[pscustomobject]@{path=[IO.Path]::GetFullPath($_);sha256=(Get-KmcSha256 $_)}})
$fixtures=@{}
foreach($pin in $manifest.outputs){
    $fixturePath=Assert-KmcChildPath $pin.path (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated actual Capture fixture'
    Assert-KmcNotReparsePoint $fixturePath 'generated Capture fixture'
    Assert-KmcNotHardLink $fixturePath 'generated Capture fixture'
    if((Get-KmcSha256 $fixturePath)-cne$pin.sha256-or(Get-Item -LiteralPath $fixturePath).Length-ne$pin.length){throw 'Actual Capture fixture bytes changed.'}
    $text=Get-Content -LiteralPath $fixturePath -Raw
    Assert-KmcJsonObjectMembersUnique $text 'actual Capture fixture'
    $fixtures.Add([IO.Path]::GetFileName($fixturePath),$text)
}
$script:passed=0;$script:failed=0
$cases=[Collections.Generic.List[object]]::new()
function Check-Setup([string]$Name,[string]$Leaf,[bool]$Accept,[scriptblock]$Mutate){
    $value=$fixtures[$Leaf]|ConvertFrom-Json
    if($null-ne$Mutate){& $Mutate $value|Out-Null}
    $accepted=$false;$message=$null
    try{Assert-KmcChunk7AssistedSetupEvidence $value $manifest.expected;$accepted=$true}catch{$message=$_.Exception.Message}
    $ok=$accepted-eq$Accept
    if($ok){$script:passed++}else{$script:failed++}
    $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Accept;actualAccepted=$accepted;error=$message})
    Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$message}else{''}))
}
foreach($leaf in @('ready-noop.json','click-xp-funds.json','xp-funds-sufficient.json','skip-disabled-first.json','system-button.json')){Check-Setup ('actual emitter '+$leaf) $leaf $true $null}
Check-Setup 'actual partial XP exception cannot qualify' 'partial-native-failure.json' $false $null
function Add-EligibleRow($Attempt){
 $b=$Attempt.before
 $b.rowObjects=@(2048);$b.rowAnswerObjects=@($b.offerObjects[0]);$b.rowNativeIndices=@(0)
 $b.rowInitialized=@($true);$b.rowDisabled=@($false);$b.rowChoosingCharacter=@($false)
 $b.rowInterchapter=@($false);$b.rowActiveAndEnabled=@($true);$b.rowActiveInHierarchy=@($true)
}
# The driver touches the shared system button only when the cue materialized ZERO cached rows, so a
# present-but-ineligible row is a shape it cannot emit and the reader refuses it like an eligible one.
function Add-IneligibleRow($Attempt){Add-EligibleRow $Attempt;$Attempt.before.rowDisabled=@($true);$Attempt.before.rowActiveAndEnabled=@($false)}
function Reject([string]$Name,[scriptblock]$Mutate,[string]$Leaf='click-xp-funds.json'){Check-Setup $Name $Leaf $false $Mutate}
Reject 'foreign native run' {param($v)$v.runId='another-run'}
Reject 'foreign campaign' {param($v)$v.gameId='aaaaaaaa-1111-4111-8111-111111111111'}
Reject 'foreign main actor' {param($v)$v.mainActorId='aaaaaaaa-1111-4111-8111-111111111111'}
Reject 'stale authority generation' {param($v)$v.authorityGeneration=2}
Reject 'ordinary acquisition cannot be claimed by setup' {param($v)$v.ordinaryAcquisitionProven=$true}
Reject 'ordinary progression cannot be claimed by setup' {param($v)$v.ordinaryProgressionProven=$true}
Reject 'Small pose cannot be claimed by setup' {param($v)$v.smallPoseQualified=$true}
Reject 'setup-only marker is mandatory' {param($v)$v.fixtureSetupOnly=$false}
Reject 'mistyped setup claim' {param($v)$v.fixtureSetupOnly='true'}
Reject 'missing setup result field' {param($v)$v.PSObject.Properties.Remove('shortfallFailed')}
Reject 'unknown setup field' {param($v)$v|Add-Member NoteProperty bypass $false}
Reject 'root singleton array is not an object' {param($v)$v.story=@($v.story)}
Reject 'story readiness must precede disposal' {param($v)$v.story.status='Disposed'}
Reject 'cached UI alone grants no authorization' {param($v)$v.story.cachedStateIsAuthorization=$true}
Reject 'story native failure retained' {param($v)$v.story.failure='click failed'}
Reject 'native click count must agree with attempts' {param($v)$v.story.nativeClickCount=0}
Reject 'attempt count must agree with observations' {param($v)$v.story.attemptCount=2}
Reject 'click sequence cannot skip admission' {param($v)$v.story.attempts[0].sequence=2}
Reject 'click never entered' {param($v)$v.story.attempts[0].nativeEntered=$false}
Reject 'click did not return' {param($v)$v.story.attempts[0].nativeReturned=$false}
Reject 'click lacks after read' {param($v)$v.story.attempts[0].afterReadSucceeded=$false}
Reject 'click exception cannot qualify' {param($v)$v.story.attempts[0].failure='native exception'}
Reject 'click identity has stale native game' {param($v)$v.story.attempts[0].before.identity.gameMatches=$false}
Reject 'click identity has foreign exact actor' {param($v)$v.story.attempts[0].before.identity.mainMatches=$false}
Reject 'click identity has stale authority' {param($v)$v.story.attempts[0].before.identity.authorityMatches=$false}
Reject 'click outside isolated campaign' {param($v)$v.story.attempts[0].before.identity.exactIsolatedFreshCampaign=$false}
Reject 'click during mounted custody' {param($v)$v.story.attempts[0].before.identity.noForeignOrMountedCustody=$false}
Reject 'click has string native flag' {param($v)$v.story.attempts[0].before.identity.playerMatches='true'}
Reject 'click requires actual Dialog mode' {param($v)$v.story.attempts[0].before.mode='Default'}
Reject 'click requires observed cached PC rows' {param($v)$v.story.attempts[0].before.uiStatus='pc-missing'}
Reject 'busy native operation excludes click' {param($v)$v.story.attempts[0].before.nativeBusy=$true}
Reject 'pending transition excludes click' {param($v)$v.story.attempts[0].before.transitionPending=$true}
Reject 'unexpected popup excludes click' {param($v)$v.story.attempts[0].before.unexpectedPopup=$true}
Reject 'missing mouse controller excludes click' {param($v)$v.story.attempts[0].before.mouseController=$false}
Reject 'inactive native dialog excludes click' {param($v)$v.story.attempts[0].before.dialogActive=$false}
Reject 'hidden manager excludes click' {param($v)$v.story.attempts[0].before.managerShown=$false}
Reject 'inactive PC dialog excludes click' {param($v)$v.story.attempts[0].before.pcActive=$false}
Reject 'wrong manager mode excludes click' {param($v)$v.story.attempts[0].before.managerKind=1}
Reject 'missing offered list identity' {param($v)$v.story.attempts[0].before.offersList=0}
Reject 'aliased native owner objects' {param($v)$v.story.attempts[0].before.rowsList=$v.story.attempts[0].before.offersList}
Reject 'selected offered answer must have exact GUID' {param($v)$v.story.attempts[0].answerGuid=('f'*32)}
Reject 'selected row must bind exact offered answer' {param($v)$v.story.attempts[0].before.rowAnswerObjects[0]=2048}
Reject 'duplicate offered identities are ambiguous' {param($v)$v.story.attempts[0].before.offerObjects[1]=$v.story.attempts[0].before.offerObjects[0]}
Reject 'duplicate offered blueprint IDs are ambiguous' {param($v)$v.story.attempts[0].before.offerGuids[1]=$v.story.attempts[0].before.offerGuids[0]}
Reject 'duplicate native row indices are ambiguous' {param($v)$v.story.attempts[0].before.rowNativeIndices[1]=$v.story.attempts[0].before.rowNativeIndices[0]}
Reject 'uninitialized row cannot be clicked' {param($v)$v.story.attempts[0].before.rowInitialized[0]=$false}
Reject 'choosing-character row cannot be clicked' {param($v)$v.story.attempts[0].before.rowChoosingCharacter[0]=$true}
Reject 'interchapter row cannot be clicked' {param($v)$v.story.attempts[0].before.rowInterchapter[0]=$true}
Reject 'disabled selected row cannot be clicked' {param($v)$v.story.attempts[0].before.rowDisabled[0]=$true}
Reject 'inactive selected row cannot be clicked' {param($v)$v.story.attempts[0].before.rowActiveAndEnabled[0]=$false}
Reject 'out-of-hierarchy selected row cannot be clicked' {param($v)$v.story.attempts[0].before.rowActiveInHierarchy[0]=$false}
Reject 'Manual selection is refused' {param($v)$v.story.attempts[0].before.offerSelectionTypes[0]=4}
Reject 'illegal selection enum refused' {param($v)$v.story.attempts[0].before.offerSelectionTypes[0]=99}
Reject 'row arrays must remain parallel' {param($v)$v.story.attempts[0].before.rowDisabled=@($false)}
Reject 'offer collection bound preserved' {param($v)$v.story.attempts[0].before.offerObjects=@(1..33)}
Reject 'selected offer order cannot be relabeled' {param($v)$v.story.attempts[0].offerIndex=1}
Reject 'selected row reference cannot be relabeled' {param($v)$v.story.attempts[0].row=16}
Reject 'native UI index cannot be relabeled' {param($v)$v.story.attempts[0].nativeIndex=2}
Reject 'row click cannot claim the system button' {param($v)$v.story.attempts[0].systemButton=$true}
Reject 'row click cannot drop its native row index' {param($v)$v.story.attempts[0].rowIndex=-1}
Reject 'row click cannot drop its native UI index' {param($v)$v.story.attempts[0].nativeIndex=-1}
Reject 'attempt kind flag is mandatory' {param($v)$v.story.attempts[0].PSObject.Properties.Remove('systemButton')}
Reject 'mistyped attempt kind flag' {param($v)$v.story.attempts[0].systemButton='true'}
Reject 'unknown attempt field' {param($v)$v.story.attempts[0]|Add-Member NoteProperty bypass $false}
Reject 'system-button click cannot claim a cached row' {param($v)$v.story.attempts[0].systemButton=$false} 'system-button.json'
Reject 'system-button click cannot carry a row index' {param($v)$v.story.attempts[0].rowIndex=0} 'system-button.json'
Reject 'system-button click cannot carry a native UI index' {param($v)$v.story.attempts[0].nativeIndex=0} 'system-button.json'
Reject 'system-button click cannot carry a row identity' {param($v)$v.story.attempts[0].row=2048} 'system-button.json'
Reject 'system-button click cannot bypass an eligible row' {param($v)Add-EligibleRow $v.story.attempts[0]} 'system-button.json'
Reject 'system-button click cannot carry any cached row' {param($v)Add-IneligibleRow $v.story.attempts[0]} 'system-button.json'
Reject 'system-button click must name an offered answer' {param($v)$v.story.attempts[0].offerIndex=1} 'system-button.json'
Reject 'system-button answer identity cannot be relabeled' {param($v)$v.story.attempts[0].answer=2048} 'system-button.json'
Reject 'system-button answer GUID must be exact' {param($v)$v.story.attempts[0].answerGuid=('f'*32)} 'system-button.json'
Reject 'Manual system answer is refused' {param($v)$v.story.attempts[0].before.offerSelectionTypes[0]=4} 'system-button.json'
Reject 'system-button attempt sequence cannot skip admission' {param($v)$v.story.attempts[0].sequence=2} 'system-button.json'
Reject 'system-button click never entered' {param($v)$v.story.attempts[0].nativeEntered=$false} 'system-button.json'
Reject 'system-button click exception cannot qualify' {param($v)$v.story.attempts[0].failure='native exception'} 'system-button.json'
Reject 'after observation cannot precede click' {param($v)$v.story.attempts[0].after.frame=9}
Reject 'after observation cannot precede elapsed clock' {param($v)$v.story.attempts[0].before.elapsedMilliseconds=10}
Reject 'same-frame Default cannot prove settlement' {param($v)$v.story.lastObserved.frame=10}
Reject 'final story state must be Default' {param($v)$v.story.lastObserved.mode='Dialog'}
Reject 'final dialog still active' {param($v)$v.story.lastObserved.dialogActive=$true}
Reject 'final cutscene still active' {param($v)$v.story.lastObserved.cutscene=$true}
Reject 'final main view not ready' {param($v)$v.story.lastObserved.mainViewReady=$false}
Reject 'final transition still pending' {param($v)$v.story.lastObserved.transitionPending=$true}
Reject 'story deadline cannot expand' {param($v)$v.story.maximumMilliseconds=300001}
Reject 'observation exceeds deadline' {param($v)$v.story.lastObserved.elapsedMilliseconds=300001}
Reject 'unknown observation fields refused' {param($v)$v.story.lastObserved|Add-Member NoteProperty synthetic $true}
Reject 'XP starts before settled story' {param($v)$v.experience.before.frame=9;$v.experience.after.frame=9}
Reject 'XP native call exception cannot qualify' {param($v)$v.experience.failure='event failed'}
Reject 'XP call cannot claim return without entry' {param($v)$v.experience.nativeEntered=$false}
Reject 'XP return must be observed' {param($v)$v.experience.nativeReturned=$false}
Reject 'XP after observation required' {param($v)$v.experience.afterReadSucceeded=$false}
Reject 'XP snapshot must be actual' {param($v)$v.experience.after=$null}
Reject 'XP delta must be exact native-table shortfall' {param($v)$v.experience.delta=8901;$v.experience.after.experience=9001}
Reject 'XP after value must reflect exactly one native call' {param($v)$v.experience.after.experience=17900}
Reject 'XP amount cap cannot expand' {param($v)$v.experience.delta=200001;$v.experience.before.targetThreshold=200101;$v.experience.after.targetThreshold=200101;$v.experience.after.experience=200101}
Reject 'XP assistance cannot directly grant a level' {param($v)$v.experience.after.level=4}
Reject 'XP assistance cannot change money' {param($v)$v.experience.after.money=8000}
Reject 'XP assistance preserves unrelated state' {param($v)$v.experience.after.preservedStateSha256=('c'*64)}
Reject 'XP assistance cannot rebind main descriptor' {param($v)$v.experience.after.identity.descriptorMatches=$false}
Reject 'XP assistance requires rate 100' {param($v)$v.experience.before.experienceRatePercent=200;$v.experience.after.experienceRatePercent=200}
Reject 'XP assistance refuses native pet multiplier' {param($v)$v.experience.before.isPet=$true;$v.experience.after.isPet=$true}
Reject 'XP assistance refuses owned pet propagation' {param($v)$v.experience.before.hasPet=$true;$v.experience.after.hasPet=$true}
Reject 'XP assistance refuses dual companion propagation' {param($v)$v.experience.before.hasDualCompanion=$true;$v.experience.after.hasDualCompanion=$true}
Reject 'XP assistance requires living idle field' {param($v)$v.experience.before.livingIdleField=$false}
Reject 'XP assistance refuses foreign builder' {param($v)$v.experience.before.foreignBuilder=$true}
Reject 'XP numeric strings refused' {param($v)$v.experience.delta='8900'}
Reject 'XP fractional values refused' {param($v)$v.experience.delta=8900.5}
Reject 'XP no-op cannot invent a native entry' {param($v)$v.experience.nativeEntered=$true} 'ready-noop.json'
Reject 'XP no-op cannot change its captured frame' {param($v)$v.experience.after.frame++} 'ready-noop.json'
Reject 'funds precede XP result' {param($v)$v.hireFunds.before.frame=11;$v.hireFunds.after.frame=11}
Reject 'funds require native level at least four' {param($v)$v.hireFunds.before.level=3;$v.hireFunds.after.level=3}
Reject 'funds require positive exact hire cost' {param($v)$v.hireFunds.before.nativeHireCost=0;$v.hireFunds.after.nativeHireCost=0}
Reject 'funds shortfall cannot overshoot' {param($v)$v.hireFunds.delta=7901;$v.hireFunds.after.money=8001}
Reject 'funds cannot alter XP' {param($v)$v.hireFunds.after.experience++}
Reject 'funds cannot alter level or hire cost' {param($v)$v.hireFunds.after.level=5}
Reject 'funds cannot change preserved actor state' {param($v)$v.hireFunds.after.preservedStateSha256=('c'*64)}
Reject 'funds cap cannot expand' {param($v)$v.hireFunds.delta=100001;$v.hireFunds.before.nativeHireCost=100101;$v.hireFunds.after.nativeHireCost=100101;$v.hireFunds.after.money=100101}
Reject 'funds no-op cannot claim native mutation' {param($v)$v.hireFunds.attempted=$true} 'xp-funds-sufficient.json'
Reject 'funds no-op cannot reduce existing money' {param($v)$v.hireFunds.after.money=8000} 'xp-funds-sufficient.json'
Reject 'partial native setup cannot be relabeled complete' {param($v)$v.shortfallFailed=$true}
foreach($pin in $inputPins){if((Get-KmcSha256 $pin.path)-cne$pin.sha256){throw ('Assisted evidence test input changed: '+$pin.path)}}
$receipt=[ordered]@{schemaVersion=1;kind='assisted-setup-evidence-focused-checks';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;durationSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;installedOrProtectedArchiveReads=0;inputPinsVerifiedAfter=$true;inputPins=$inputPins;actualEmitterFixtureCount=6;scope='Actual production Capture output plus malformed mutations; no native or complete host/output qualification';cases=$cases.ToArray()}
Write-KmcJsonAtomic (Join-Path $output 'receipt.json') $receipt
Write-Host ('ASSISTED SETUP READER: '+$script:passed+' PASS / '+$script:failed+' FAIL')
$global:LASTEXITCODE=$(if($script:failed-eq0){0}else{1})
