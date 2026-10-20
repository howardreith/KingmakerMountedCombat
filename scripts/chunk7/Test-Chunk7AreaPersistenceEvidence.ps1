$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
# Reuse the actual native archive shape and its affected reader neighbors.
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7AreaPersistenceEvidence.ps1')
$neighborPass=$passed;$passed=0
$areaRequest=Copy-Test $request;$areaRequest.persistenceCase='multi-area-autosave'
$areaRequest|Add-Member NoteProperty persistenceAreaTarget ([pscustomobject]@{enterPoint=('c'*32);area=('b'*32);autoSaveMode='AfterEntry'})
$areaArchive=Copy-Test $archive;$areaArchive.AreaId='b'*32
$phases=@();$labels=@('before-native-transfer','native-header-starting','native-header-staged','after-native-transfer')
for($phase=0;$phase-lt4;$phase++) {
    $s=Copy-Test $snapshot;$s.label=$labels[$phase];$s.gameTicks=10000000L+1000L*$phase;$s.snapshots=if($phase-ge2){1}else{0}
    $s|Add-Member NoteProperty frame (10+$phase)
    $s|Add-Member NoteProperty area $(if($phase-eq0){'a'*32}else{'b'*32})
    $s|Add-Member NoteProperty sameWorld $true
    $s|Add-Member NoteProperty suspensions $(if($phase-eq0){0}else{1})
    $s|Add-Member NoteProperty resumes $(if($phase-eq0){0}else{2})
    $s|Add-Member NoteProperty pending ($phase-eq1-or$phase-eq2)
    $s|Add-Member NoteProperty activeOwners 2
    $s|Add-Member NoteProperty activeSaveScope ($phase-eq1-or$phase-eq2)
    $views=@();for($actor=0;$actor-lt4;$actor++){$views+=[pscustomobject]@{id=$ids[$actor];viewId=200+$actor;baselineViewId=200+$actor;viewBound=$true;nativeMatches=1;commandsEmpty=$true}}
    $s|Add-Member NoteProperty views $views
    $movements=@();for($pair=0;$pair-lt2;$pair++){
        $runtime=if($phase-eq0){100+$pair}else{110+$pair}
        $movements+=[pscustomobject]@{riderId=$pairs[$pair].Rider.Id;baselineOverride=100+$pair;runtimeOverride=$runtime;viewOverride=$runtime;overrideComponents=@($runtime);
            invariant=$null;saveSerializationSuspended=($phase-eq2);stockEnabled=$false;mountId=$pairs[$pair].Mount.Id;mountStockEnabled=$true;mountOverride=0;mountOverrideComponents=@();riderViewBoundToOwner=$true;mountViewBoundToOwner=$true}
    }
    $s|Add-Member NoteProperty movementAuthorities $movements
    $phases+=$s
}
$areaObservation=[pscustomobject]@{nativeWrite=[pscustomobject]@{sha256=('5'*64);nativeCallback=$false;nativeType='Auto';operation='None';snapshotCount=1};
    areaTransfer=[pscustomobject]@{mode='AfterEntry';enterPoint=('c'*32);sourceArea=('a'*32);destinationArea=('b'*32);before=$phases[0];headerStarting=$phases[1];headerStaged=$phases[2];after=$phases[3];
        nativeSaveCallbackExpected=$false;explicitSaveGameDispatches=0;observerError=$null;loadingTicks=40;suspensionObserved=$true;duplicateReadyNoOp=$true;nativeArchiveSha256=('5'*64)}}
Check 'two native owners restored before AfterEntry autosave with retained views and fresh movers' {Assert-KmcChunk7AreaAutosave $areaRequest $areaObservation $areaArchive}
$mutations=@(
 @('omitted second header owner',{param($v)$v.areaTransfer.headerStarting.owners=@($v.areaTransfer.headerStarting.owners[0])}),
 @('wrong destination',{param($v)$v.areaTransfer.headerStarting.area='d'*32}),
 @('one pair restored',{param($v)$v.areaTransfer.headerStarting.resumes=1}),
 @('forged callback',{param($v)$v.nativeWrite.nativeCallback=$true}),
 @('explicit SaveGame substitute',{param($v)$v.areaTransfer.explicitSaveGameDispatches=1}),
 @('string dispatch counter',{param($v)$v.areaTransfer.explicitSaveGameDispatches='0'}),
 @('header observed twice',{param($v)$v.areaTransfer.headerStaged.snapshots=2}),
 @('source generation changed',{param($v)$v.areaTransfer.headerStaged.owners[1].generation=99}),
 @('cross-owner actor substitution',{param($v)$v.areaTransfer.headerStaged.owners[1].riderId=$first}),
 @('unobserved suspension',{param($v)$v.areaTransfer.suspensionObserved=$false}),
 @('missing loading interval',{param($v)$v.areaTransfer.loadingTicks=0}),
 @('observer fault',{param($v)$v.areaTransfer.observerError='failure'}),
 @('staged save scope missing',{param($v)$v.areaTransfer.headerStaged.activeSaveScope=$false}),
 @('staged second lease missing',{param($v)$v.areaTransfer.headerStaged.movementAuthorities[1].saveSerializationSuspended=$false}),
 @('starting lease suspended early',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[0].saveSerializationSuspended=$true}),
 @('settled lease retained',{param($v)$v.areaTransfer.after.movementAuthorities[0].saveSerializationSuspended=$true}),
 @('staged serialized invariant failed',{param($v)$v.areaTransfer.headerStaged.movementAuthorities[1].invariant='serialized AI mismatch'}),
 @('replaced native actor view',{param($v)$v.areaTransfer.after.views[2].viewId=444}),
 @('duplicate native actor',{param($v)$v.areaTransfer.headerStaged.views[3].nativeMatches=2}),
 @('unbound native view',{param($v)$v.areaTransfer.headerStarting.views[1].viewBound=$false}),
 @('string view binding',{param($v)$v.areaTransfer.headerStarting.views[1].viewBound='true'}),
 @('stale native movement override',{param($v)$m=$v.areaTransfer.headerStarting.movementAuthorities[1];$m.runtimeOverride=$m.baselineOverride;$m.viewOverride=$m.baselineOverride;$m.overrideComponents=@($m.baselineOverride)}),
 @('duplicate movement component',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[1].overrideComponents+=555}),
 @('numeric text baseline component',{param($v)$m=$v.areaTransfer.before.movementAuthorities[0];$m.overrideComponents=@([string]$m.runtimeOverride)}),
 @('numeric text header-starting component',{param($v)$m=$v.areaTransfer.headerStarting.movementAuthorities[0];$m.overrideComponents=@([string]$m.runtimeOverride)}),
 @('numeric text staged component',{param($v)$m=$v.areaTransfer.headerStaged.movementAuthorities[1];$m.overrideComponents=@([string]$m.runtimeOverride)}),
 @('numeric text settled component',{param($v)$m=$v.areaTransfer.after.movementAuthorities[1];$m.overrideComponents=@([string]$m.runtimeOverride)}),
 @('fractional component identity',{param($v)$m=$v.areaTransfer.headerStarting.movementAuthorities[0];$m.overrideComponents=@([double]$m.runtimeOverride+0.5)}),
 @('first restored mover replaced at header staging',{param($v)$m=$v.areaTransfer.headerStaged.movementAuthorities[0];$m.runtimeOverride=700;$m.viewOverride=700;$m.overrideComponents=@(700)}),
 @('second restored mover replaced at header staging',{param($v)$m=$v.areaTransfer.headerStaged.movementAuthorities[1];$m.runtimeOverride=701;$m.viewOverride=701;$m.overrideComponents=@(701)}),
 @('first restored mover replaced after header',{param($v)$m=$v.areaTransfer.after.movementAuthorities[0];$m.runtimeOverride=700;$m.viewOverride=700;$m.overrideComponents=@(700)}),
 @('second restored mover replaced after header',{param($v)$m=$v.areaTransfer.after.movementAuthorities[1];$m.runtimeOverride=701;$m.viewOverride=701;$m.overrideComponents=@(701)}),
 @('wrong view movement component',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[1].viewOverride=222}),
 @('rider native mover still active',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[0].stockEnabled=$true}),
 @('mount native mover disabled',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[1].mountStockEnabled=$false}),
 @('mount owns stale rider override',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[1].mountOverrideComponents=@(999)}),
 @('wrong mount view binding',{param($v)$v.areaTransfer.headerStarting.movementAuthorities[1].mountViewBoundToOwner=$false}),
 @('retained command',{param($v)$v.areaTransfer.after.views[1].commandsEmpty=$false}),
 @('invented native preparation',{param($v)$v.areaTransfer.headerStarting.actors[1].prepared=$true}),
 @('refunded nonselected debt',{param($v)$v.areaTransfer.headerStarting.actors[3].native.Standard=0.0}),
 @('replayed native spending',{param($v)$v.areaTransfer.headerStarting.actors[2].native.Standard=7.0}),
 @('restored debt through load path',{param($v)$v.areaTransfer.headerStarting.semanticRestores=4}),
 @('replayed mount input',{param($v)$v.areaTransfer.headerStarting.nativeCastRequests=3}),
 @('string restoration counter',{param($v)$v.areaTransfer.headerStarting.resumes='2'}),
 @('out of order header',{param($v)$v.areaTransfer.headerStaged.frame=1}),
 @('retained area custody',{param($v)$v.areaTransfer.after.pending=$true}),
 @('duplicate-ready replay',{param($v)$v.areaTransfer.duplicateReadyNoOp=$false}),
 @('different immutable autosave',{param($v)$v.areaTransfer.nativeArchiveSha256='6'*64})
)
foreach($mutation in $mutations){Reject $mutation[0] {$a=Copy-Test $areaObservation;& $mutation[1] $a;Assert-KmcChunk7AreaAutosave $areaRequest $a $areaArchive}}
Reject 'cold process cannot substitute for source transfer' {$q=Copy-Test $areaRequest;$q.scenario='chunk7-persistence-load';Assert-KmcChunk7AreaAutosave $q $areaObservation $areaArchive}
Reject 'another archive area' {$a=Copy-Test $areaArchive;$a.AreaId='a'*32;Assert-KmcChunk7AreaAutosave $areaRequest $areaObservation $a}
Write-Host ('CHUNK7 AREA READER PASS='+$passed+' FAIL=0; affected persistence neighbors='+$neighborPass+'; synthetic evidence only.')
