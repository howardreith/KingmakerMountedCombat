[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
$script:ownedTestLab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/persistence-fixture-tests/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { return $script:ownedTestLab }
$sourceId='owned-source'
$root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$sourceId)
$saveRoot=Join-Path $root 'Saved Games'
$evidence=Join-Path $script:ownedTestLab ('runtime-evidence/'+$sourceId)
[void][IO.Directory]::CreateDirectory($saveRoot);[void][IO.Directory]::CreateDirectory($evidence)
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p01-save';transactionToken=('a'*64)})
$resultPath=Join-Path $evidence 'runtime-result.json'
$result=[ordered]@{runId=$sourceId;scenario='persistence-p01-save';transactionToken=('a'*64);status='PASS';modsRestored=$true;workingRestored=$true}
Write-KmcJsonAtomic $resultPath $result
$fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64)};working=[pscustomobject]@{
    gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';gameName='owned fixture';area=('a'*32)}}
$path=Join-Path $saveRoot 'Manual_300_KMC_P01.zks'
Add-Type -AssemblyName System.IO.Compression
$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
$archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    $header=[ordered]@{Name='KMC_P01';Type='Manual';CompatibilityVersion=1;GameId=$fixture.working.gameId;GameName=$fixture.working.gameName;Area=$fixture.working.area}
    foreach($name in @('header.json','kmc-mounted-state')){
        $writer=[IO.StreamWriter]::new($archive.CreateEntry($name).Open())
        try{$writer.Write($(if($name-ceq'header.json'){$header|ConvertTo-Json -Compress}else{'{}'}))}finally{$writer.Dispose()}
    }
}finally{$archive.Dispose();$stream.Dispose()}
$hash=Get-KmcSha256 $path
$passes=0
function Must-Reject([scriptblock]$Action,[string]$Message){
    $rejected=$false;try{& $Action|Out-Null}catch{$rejected=$true}
    if(-not$rejected){throw $Message}
    $script:passes++
}
$read=Get-KmcPersistenceSource -SourceRunId $sourceId -ExpectedSha256 $hash -Fixture $fixture
if($read.path-cne$path-or$read.descriptor.sha256-cne$hash){throw 'Actual archive selection failed'};$passes++
Must-Reject {Get-KmcPersistenceSource '../human' $hash $fixture} 'Traversal admitted'
Must-Reject {Get-KmcPersistenceSource '..' $hash $fixture} 'Parent alias admitted'
Must-Reject {Get-KmcPersistenceSource $sourceId ('a'*64) $fixture} 'Wrong hash admitted'
$fixture.working.gameId='e3afcc98-b337-4f99-99d3-e1ab9f893bc2'
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Foreign campaign admitted'
$fixture.working.gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'
$result.status='FAIL';Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Failed native source admitted'
$result.status='PASS';$result.workingRestored=$false;Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Unrestored native source admitted'
$result.status='PASS';$result.workingRestored=$true;$result.scenario='persistence-p02-save'
Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Mismatched P02 ownership admitted'
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p02-save';transactionToken=('a'*64)})
$p02=Get-KmcPersistenceSource -SourceRunId $sourceId -ExpectedSha256 $hash -Fixture $fixture
if($p02.descriptor.sha256-cne$hash){throw 'P02 actual archive identity changed'};$passes++
$result.scenario='unrecognized-save';Write-KmcJsonAtomic $resultPath $result
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'Unrecognized save scenario admitted'
if((Get-KmcSha256 $path)-cne$hash){throw 'Read-only source inspection mutated archive'};$passes++
$snapshot=[pscustomobject]@{Rider=[pscustomobject]@{Id='r';Standard=0};Mount=[pscustomobject]@{Standard=0;Move=0.5};Combat=[pscustomobject]@{
    Round=1;Current=[pscustomobject]@{ActorId='r'};Paired=[pscustomobject]@{Activation=[pscustomobject]@{Sequence=1;Ending=$false}}}}
foreach($case in @('partial-movement','rider-spent','between-partner-orders','exhausted','explicit-end')){
    $snapshot.Rider.Standard=if($case-cin @('rider-spent','exhausted','explicit-end')){6}else{0}
    $snapshot.Mount.Standard=if($case-cin @('between-partner-orders','exhausted','explicit-end')){6}else{0}
    $snapshot.Mount.Move=if($snapshot.Mount.Standard-gt0){3}else{0.5}
    $snapshot.Combat.Paired.Activation.Ending=$case-ceq'explicit-end'
    Assert-KmcP02Snapshot $snapshot $case;$passes++
    $sequence=$snapshot.Combat.Paired.Activation.Sequence
    $snapshot.Combat.Paired.Activation.Sequence=0
    Must-Reject {Assert-KmcP02Snapshot $snapshot $case} 'Checkpoint accepted missing participation'
    $snapshot.Combat.Paired.Activation.Sequence=$sequence
}
$snapshot.Combat.Paired.Activation.Ending=$false
Must-Reject {Assert-KmcP02Snapshot $snapshot 'explicit-end'} 'Explicit End accepted a fresh participation fallback'
$snapshot.Rider.Standard=0;$snapshot.Mount.Standard=0;$snapshot.Mount.Move=0.5
Must-Reject {Assert-KmcP02Snapshot $snapshot 'rider-spent'} 'Spent-rider fixture accepted restored Standard'
$snapshot.Mount.Standard=6;$snapshot.Mount.Move=0
Must-Reject {Assert-KmcP02Snapshot $snapshot 'between-partner-orders'} 'Spent mount accepted refunded movement'
Must-Reject {Assert-KmcP02Snapshot $snapshot 'unknown'} 'Unknown checkpoint accepted'
$snapshot.Rider|Add-Member NoteProperty Move 0
$snapshot.Mount|Add-Member NoteProperty Id 'm'
$snapshot.Mount.Standard=0;$snapshot.Mount.Move=0
$snapshot.Combat|Add-Member NoteProperty Allocations @([pscustomobject]@{
    ActorId='m';StandardCommitted=$false;Movement=[pscustomobject]@{MetresStepped=1.2;TimeStepped=0.3}})
Assert-KmcP03Snapshot $snapshot 'step';$passes++
$snapshot.Combat.Allocations[0].Movement.TimeStepped=0
Must-Reject {Assert-KmcP03Snapshot $snapshot 'step'} 'Step without native time accepted'
$snapshot.Mount.Standard=0;$snapshot.Mount.Move=4
$snapshot.Combat.Allocations[0].Movement|Add-Member NoteProperty TimeMoved 4
Assert-KmcP03Snapshot $snapshot 'conversion';$passes++
$snapshot.Combat.Allocations[0].Movement.TimeMoved=0
Must-Reject {Assert-KmcP03Snapshot $snapshot 'conversion'} 'Conversion without native movement time accepted'
$snapshot.Combat.Allocations[0].Movement.TimeMoved=4;$snapshot.Mount.Move=6
Must-Reject {Assert-KmcP03Snapshot $snapshot 'conversion'} 'Conversion fixture without available remainder accepted'
$snapshot.Mount.Move=0
Assert-KmcP03Snapshot $snapshot 'round-effect';$passes++
$snapshot.Mount.Standard=6
Must-Reject {Assert-KmcP03Snapshot $snapshot 'round-effect'} 'Round effect cannot invent an unused action'
Must-Reject {Assert-KmcP03Snapshot $snapshot 'unknown'} 'Unknown P03 checkpoint accepted'
$snapshot.Mount|Add-Member NoteProperty ReactionsRemaining 0
$snapshot.Combat.Current.ActorId='enemy'
$snapshot.Combat.Paired.Activation|Add-Member NoteProperty Rider ([pscustomobject]@{Ended=$true})
$snapshot.Combat.Paired.Activation|Add-Member NoteProperty Mount ([pscustomobject]@{Ended=$true})
$snapshot.Combat|Add-Member NoteProperty Actors @([pscustomobject]@{Native=[pscustomobject]@{Id='m'};DisengageTargets=@('enemy')})
Assert-KmcP03Snapshot $snapshot 'reaction';$passes++
$snapshot.Mount.ReactionsRemaining=1
Must-Reject {Assert-KmcP03Snapshot $snapshot 'reaction'} 'Unspent reaction cannot qualify consumption'
$snapshot.Mount.ReactionsRemaining=0;$snapshot.Combat.Paired.Activation.Mount.Ended=$false
Must-Reject {Assert-KmcP03Snapshot $snapshot 'reaction'} 'Reaction fixture requires ended pair participation'
$snapshot.Combat.Paired.Activation.Mount.Ended=$true;$snapshot.Combat.Actors[0].DisengageTargets=@()
Must-Reject {Assert-KmcP03Snapshot $snapshot 'reaction'} 'Consumed native reaction target must survive'
# Category tests use newly created synthetic archives under this owned obj root.
foreach($case in @('manual','quick','auto')){
    $type=if($case-ceq'manual'){'Manual'}elseif($case-ceq'quick'){'Quick'}else{'Auto'}
    $leaf=if($type-ceq'Manual'){'Manual_300_KMC_P01.zks'}else{$type+'_1.zks'}
    $slotPath=Join-Path $saveRoot $leaf
    if($type-cne'Manual'){
        $slotStream=[IO.File]::Open($slotPath,[IO.FileMode]::CreateNew)
        $slotArchive=[IO.Compression.ZipArchive]::new($slotStream,[IO.Compression.ZipArchiveMode]::Create,$false)
        try{
            $slotHeader=[ordered]@{Name=('native '+$type+' 1');Type=$type;CompatibilityVersion=1;GameId=$fixture.working.gameId;GameName=$fixture.working.gameName;Area=$fixture.working.area}
            foreach($member in @('header.json','kmc-mounted-state')){
                $writer=[IO.StreamWriter]::new($slotArchive.CreateEntry($member).Open())
                try{$writer.Write($(if($member-ceq'header.json'){$slotHeader|ConvertTo-Json -Compress}else{'{}'}))}finally{$writer.Dispose()}
            }
        }finally{$slotArchive.Dispose();$slotStream.Dispose()}
    }
    $slotHash=Get-KmcSha256 $slotPath
    $result.scenario='persistence-p05-save';Write-KmcJsonAtomic $resultPath $result
    Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p05-save';persistenceCase=$case;transactionToken=('a'*64)})
    $slot=Get-KmcPersistenceSource $sourceId $slotHash $fixture -NativeCase $case
    if($slot.path-cne$slotPath-or$slot.descriptor.sha256-cne$slotHash){throw 'Native category source identity changed'};$passes++
    Must-Reject {Get-KmcPersistenceSource $sourceId $slotHash $fixture} 'P05 accepted missing category'
    $wrong=if($case-ceq'quick'){'auto'}else{'quick'}
    Must-Reject {Get-KmcPersistenceSource $sourceId $slotHash $fixture -NativeCase $wrong} 'P05 category and source ownership differed'
    $result.modsRestored=$false;Write-KmcJsonAtomic $resultPath $result
    Must-Reject {Get-KmcPersistenceSource $sourceId $slotHash $fixture -NativeCase $case} 'P05 admitted an unrestored source'
    $result.modsRestored=$true;Write-KmcJsonAtomic $resultPath $result
    if((Get-KmcSha256 $slotPath)-cne$slotHash){throw 'P05 source inspection changed archive'};$passes++
}
$alternatePath=Join-Path $saveRoot 'Manual_301_KMC_P05_UNMOUNTED.zks'
$slotStream=[IO.File]::Open($alternatePath,[IO.FileMode]::CreateNew)
$slotArchive=[IO.Compression.ZipArchive]::new($slotStream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    $slotHeader=[ordered]@{Name='KMC_P05_UNMOUNTED';Type='Manual';CompatibilityVersion=1;GameId=$fixture.working.gameId;GameName=$fixture.working.gameName;Area=$fixture.working.area}
    foreach($member in @('header.json','kmc-mounted-state')){
        $writer=[IO.StreamWriter]::new($slotArchive.CreateEntry($member).Open())
        try{$writer.Write($(if($member-ceq'header.json'){$slotHeader|ConvertTo-Json -Compress}else{'{}'}))}finally{$writer.Dispose()}
    }
}finally{$slotArchive.Dispose();$slotStream.Dispose()}
$alternateHash=Get-KmcSha256 $alternatePath
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p05-save';persistenceCase='alternating';transactionToken=('a'*64)})
$a=Get-KmcPersistenceSource $sourceId $hash $fixture -NativeCase alternating
$b=Get-KmcPersistenceSource $sourceId $alternateHash $fixture -NativeCase alternating -Alternate
if($a.path-cne$path-or$b.path-cne$alternatePath-or$b.descriptor.internalName-cne'KMC_P05_UNMOUNTED'){throw 'Alternating exact source selection failed'};$passes++
Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture -NativeCase alternating -Alternate} 'Alternate accepted primary hash'
Must-Reject {Get-KmcPersistenceSource $sourceId $alternateHash $fixture -NativeCase manual -Alternate} 'Alternate escaped its case'
Must-Reject {Get-KmcPersistenceSource $sourceId $alternateHash $fixture -NativeCase alternating} 'Primary accepted alternate hash'
if((Get-KmcSha256 $path)-cne$hash-or(Get-KmcSha256 $alternatePath)-cne$alternateHash){throw 'Alternating source inspection changed inputs'};$passes++
foreach($case in @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting')){
    $result.scenario='persistence-p04-save';Write-KmcJsonAtomic $resultPath $result
    Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p04-save';persistenceCase=$case;transactionToken=('a'*64)})
    $rt=Get-KmcPersistenceSource $sourceId $hash $fixture -NativeCase $case
    if($rt.path-cne$path){throw 'P04 source selected another archive'};$passes++
    Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture} 'P04 accepted a missing checkpoint'
    Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture -NativeCase manual} 'P04 accepted a P05 category'
    Must-Reject {Get-KmcPersistenceSource $sourceId $hash $fixture -NativeCase $case -Alternate} 'P04 accepted a secondary archive'
}
# Evidence mutation checks use synthetic bytes in a separate owned root.
$proofId='owned-alternating-evidence'
$proofRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$proofId+'/Saved Games')
[void][IO.Directory]::CreateDirectory($proofRoot)
foreach($leaf in @('Manual_299_KMC_AUTOMATION_WORKING.zks','Manual_300_KMC_P01.zks','Manual_301_KMC_P05_UNMOUNTED.zks')){
    [IO.File]::WriteAllText((Join-Path $proofRoot $leaf),('synthetic '+$leaf),[Text.UTF8Encoding]::new($false))
}
$proof=[pscustomobject]@{runId=$proofId;scenario='persistence-p05-save';commit=('e'*40);dllSha256=('f'*64);fixture=$fixture}
$proofGame=[pscustomobject]@{processId=123}
function New-ProofRow([string]$kind,[string]$state,$detail){
    return [pscustomobject]@{kind=$kind;runId=$proof.runId;scenario=$proof.scenario;source=$proof.commit;dll=$proof.dllSha256;
        processId=123;checkpoint='alternating';rider=[pscustomobject]@{Id='r'};mount=[pscustomobject]@{Id='m'};
        relationship=$state;controls=[pscustomobject]@{DuplicateFactCount=0};detail=$detail}
}
function New-ProofWrite([string]$label,[int]$ordinal,[string]$leaf,[bool]$mounted){
    $p=Join-Path $proofRoot $leaf
    $d=[pscustomobject]@{label=$label;ordinal=$ordinal;path=$p;nativeType='Manual';nativeCallback=$true;operation='None';
        sha256=(Get-KmcSha256 $p);length=(Get-Item -LiteralPath $p).Length;
        snapshot=[pscustomobject]@{Mounted=$mounted;Rider=$null;Mount=$null;ProfileId=$null;
            CampaignId=$fixture.working.gameId;AreaId=$fixture.working.area}}
    if($mounted){$d.snapshot.Rider=[pscustomobject]@{Id='r'};$d.snapshot.Mount=[pscustomobject]@{Id='m'};$d.snapshot.ProfileId='Mammoth'}
    return $d
}
$rows=@(
    (New-ProofRow 'initial' 'Mounted' $null),
    (New-ProofRow 'alternate-write-requested' 'Mounted' $null),
    (New-ProofRow 'alternate-native-write-complete' 'Mounted' (New-ProofWrite 'A' 1 'Manual_300_KMC_P01.zks' $true)),
    (New-ProofRow 'alternate-voluntary-dismount' 'Unmounted' $null),
    (New-ProofRow 'alternate-write-requested' 'Unmounted' $null),
    (New-ProofRow 'alternate-native-write-complete' 'Unmounted' (New-ProofWrite 'B' 2 'Manual_301_KMC_P05_UNMOUNTED.zks' $false)),
    (New-ProofRow 'alternating-source-complete' 'Unmounted' $null)
)
Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame;$passes++
$rows[5].detail.snapshot.Rider=[pscustomobject]@{Id='r'}
Must-Reject {Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame} 'Unmounted evidence accepted stale pair'
$rows[5].detail.snapshot.Rider=$null
$rows[2].detail.nativeCallback=$false
Must-Reject {Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame} 'Incomplete native write qualified'
$rows[2].detail.nativeCallback=$true
$rows[2].detail.sha256=('0'*64)
Must-Reject {Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame} 'Observed archive hash mismatch qualified'
$rows[2].detail.sha256=Get-KmcSha256 $rows[2].detail.path
$rows[3].relationship='Mounted'
Must-Reject {Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame} 'Missing legitimate dismount qualified'
$rows[3].relationship='Unmounted'
$rows[4].processId=124
Must-Reject {Assert-KmcAlternatingPersistenceEvidence $proof $rows $proofGame} 'Foreign process observation qualified'
$rows[4].processId=123
# P04 evidence checks use synthetic bytes and copied observation shapes only.
$rtProofId='owned-realtime-evidence'
$rtProofRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$rtProofId+'/Saved Games')
[void][IO.Directory]::CreateDirectory($rtProofRoot)
$rtPath=Join-Path $rtProofRoot 'Manual_300_KMC_P01.zks'
[IO.File]::WriteAllText($rtPath,'synthetic real-time archive',[Text.UTF8Encoding]::new($false))
$rtRequest=[pscustomobject]@{runId=$rtProofId;scenario='persistence-p04-save';persistenceCase='unmounted-spent';commit=('e'*40);dllSha256=('f'*64);fixture=$fixture}
function New-RtProof([string]$kind,[long]$ticks,$detail){
    return [pscustomobject]@{runId=$rtProofId;scenario=$rtRequest.scenario;source=$rtRequest.commit;dll=$rtRequest.dllSha256;processId=123;
        kind=$kind;checkpoint=$rtRequest.persistenceCase;gameTicks=$ticks;relationship='Unmounted';
        rider=[pscustomobject]@{Id='r';Standard=5};mount=[pscustomobject]@{Id='m'};
        controls=[pscustomobject]@{DuplicateFactCount=0};native=[pscustomobject]@{tbSetting=$false;tbInitialized=$false};detail=$detail}
}
$rtSnapshot=[pscustomobject]@{Mounted=$false;CampaignId=$fixture.working.gameId;AreaId=$fixture.working.area;
    Combat=[pscustomobject]@{TurnBased=$false;Current=$null;Roster=@();Paired=$null;
        Actors=@([pscustomobject]@{Native=[pscustomobject]@{Id='r';Standard=5}})}}
$rtWrite=[pscustomobject]@{path=$rtPath;sha256=(Get-KmcSha256 $rtPath);length=(Get-Item -LiteralPath $rtPath).Length;
    nativeType='Manual';nativeCallback=$true;operation='None';snapshot=$rtSnapshot}
$rtRows=@(
    (New-RtProof 'initial' 0 $null),
    (New-RtProof 'rt-repeated-attack-requested' 1 $null),
    (New-RtProof 'rt-repeated-attack-resolved' 2 ([pscustomobject]@{ordinaryAttacks=2;resolved=2;forcedD20=0})),
    (New-RtProof 'rt-before-save' 3 $null),
    (New-RtProof 'rt-native-save-requested' 4 $null),
    (New-RtProof 'native-write-complete' 5 $rtWrite),
    (New-RtProof 'rt-spent-attack-queued' 6 ([pscustomobject]@{readyTicks=50000000;resolved=2;riderRounds=1})),
    (New-RtProof 'rt-native-debt-wait' 7 $null),
    (New-RtProof 'rt-later-attack' 50000000 ([pscustomobject]@{resolved=3;riderRounds=2;forcedD20=0})),
    (New-RtProof 'rt-later-attack' 110000000 ([pscustomobject]@{resolved=4;riderRounds=3;forcedD20=0})),
    (New-RtProof 'usable-continuation-complete' 110000001 $null)
)
Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame;$passes++
$rtSnapshot.Combat.TurnBased=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame} 'P04 accepted a TB context'
$rtSnapshot.Combat.TurnBased=$false;$rtRows[8].detail.resolved=4
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame} 'P04 accepted duplicate effects'
$rtRows[8].detail.resolved=3;$rtRows[8].gameTicks=8
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame} 'P04 accepted an early refunded attack'
$rtRows[8].gameTicks=50000000;$rtRows[9].detail.riderRounds=4
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame} 'P04 accepted an extra round refresh'
$rtRows[9].detail.riderRounds=3;$rtWrite.nativeCallback=$false
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $rtRows $proofGame} 'P04 accepted an incomplete native write'
$rtWrite.nativeCallback=$true
$rtRequest.persistenceCase='unmounted-attack'
foreach($row in $rtRows){$row.checkpoint=$rtRequest.persistenceCase}
$rtSnapshot|Add-Member -NotePropertyName GameTimeTicks -NotePropertyValue 5
$rtWrite|Add-Member -NotePropertyName actual -NotePropertyValue ([pscustomobject]@{
    deferredSaves=1;snapshotCount=1;resolved=3;unresolvedProjectiles=$false})
$activeDetail=[pscustomobject]@{resolved=2;snapshotCount=0;nativeCommands=@([pscustomobject]@{
    actor='r';raw=@([pscustomobject]@{started=$true;acted=$false;finished=$false})})}
$waitDetail=[pscustomobject]@{nativeSaveWaiting=$true;deferredSaves=1;snapshotCount=0}
$activeRow=New-RtProof 'rt-active-attack-save-request' 3 $activeDetail
$waitRow=New-RtProof 'rt-native-wait-started' 4 $waitDetail
$activeRows=@($rtRows)+@($activeRow,$waitRow)
Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame;$passes++
$activeDetail.nativeCommands[0].raw[0].acted=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 active accepted an already delivered request'
$activeDetail.nativeCommands[0].raw[0].acted=$false;$waitDetail.snapshotCount=1
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 active took its snapshot before effect settlement'
$waitDetail.snapshotCount=0;$rtWrite.actual.resolved=4
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 active accepted a duplicate native effect'
$rtWrite.actual.resolved=3;$rtSnapshot.GameTimeTicks=3
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 active clock did not advance while waiting'
$rtSnapshot.GameTimeTicks=5
$rtRequest.persistenceCase='unmounted-projectile'
foreach($row in $activeRows){$row.checkpoint=$rtRequest.persistenceCase}
$activeRow.kind='rt-projectile-save-request'
$projectile=[pscustomobject]@{actor='r';target='t';arrived=$false;weapon=$true;resolve=$true}
$activeDetail|Add-Member -NotePropertyMembers @{projectiles=@($projectile);target='t';riderRanged=$true;ordinaryAttacks=3;unresolvedProjectiles=$true}
Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame;$passes++
$projectile.arrived=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 accepted a post-arrival projectile request'
$projectile.arrived=$false;$projectile.actor='foreign'
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 accepted a foreign projectile'
$projectile.actor='r';$activeDetail.resolved=3
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 accepted a replayed projectile effect'
$activeDetail.resolved=2;$rtWrite.actual.unresolvedProjectiles=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $activeRows $proofGame} 'P04 snapshot retained undelivered projectile'
$rtWrite.actual.unresolvedProjectiles=$false
$rtWrite.actual|Add-Member -NotePropertyMembers @{target='t';targetDamage=7;riderWeapon='bow';riderRanged=$true}
$coldActual=[pscustomobject]@{target='t';targetDamage=7;riderWeapon='bow';riderRanged=$true;unresolvedProjectiles=$false;
    resolved=0;ordinaryAttacks=0;rules=[pscustomobject]@{pairDamageRules=0;pairDamage=0}}
$coldRow=[pscustomobject]@{kind='rt-cold-debt-restored';processId=456;detail=[pscustomobject]@{actual=$coldActual}}
Assert-KmcProjectileColdOutcome $rtRows @($coldRow);$passes++
$coldActual.targetDamage=14
Must-Reject {Assert-KmcProjectileColdOutcome $rtRows @($coldRow)} 'P04 accepted duplicated projectile damage'
$coldActual.targetDamage=0
Must-Reject {Assert-KmcProjectileColdOutcome $rtRows @($coldRow)} 'P04 accepted lost projectile damage'
$coldActual.targetDamage=7;$coldActual.riderWeapon='replacement'
Must-Reject {Assert-KmcProjectileColdOutcome $rtRows @($coldRow)} 'P04 accepted cold equipment repair'
$coldActual.riderWeapon='bow';$coldActual.resolved=1
Must-Reject {Assert-KmcProjectileColdOutcome $rtRows @($coldRow)} 'P04 accepted replayed cold resolution'
$coldActual.resolved=0;$coldRow.processId=123
Must-Reject {Assert-KmcProjectileColdOutcome $rtRows @($coldRow)} 'P04 accepted a warm projectile load'
$rtRequest.persistenceCase='unmounted-approach'
$rtSnapshot.Combat.Actors[0].Native.Standard=0
$rtWrite.actual.resolved=0
$approachActual=[pscustomobject]@{approach=[pscustomobject]@{moving=$true;travelled=0.7;remaining=10};
    resolved=0;ordinaryAttacks=0;inputRequests=1;snapshotCount=0;deferredSaves=0;target='t';targetDamage=0;
    riderPosition=@(1.0,2.0,3.0);mountPosition=@(2.0,2.0,3.0)}
$approachRequest=New-RtProof 'rt-approach-save-request' 3 $approachActual
$approachBarrier=New-RtProof 'rt-native-snapshot' 5 ($approachActual|ConvertTo-Json -Depth 8|ConvertFrom-Json)
$approachBarrier.detail.snapshotCount=1
$approachBarrier.controls|Add-Member -NotePropertyName SerializationSuspended -NotePropertyValue $true
$approachRows=@($rtRows|Where-Object {$_.kind-cnotin @('rt-repeated-attack-requested','rt-repeated-attack-resolved')})
foreach($row in $approachRows){$row.checkpoint=$rtRequest.persistenceCase}
$approachRows+=@((New-RtProof 'rt-approach-dispatched' 1 $null),$approachRequest,$approachBarrier,
    (New-RtProof 'rt-approach-continuation' 6 ([pscustomobject]@{resolved=0;ordinaryAttacks=0;inputRequests=1;riderRounds=0})),
    (New-RtProof 'rt-approach-first-delivery' 6 ([pscustomobject]@{resolved=1;ordinaryAttacks=1;inputRequests=1;riderRounds=0})))
Assert-KmcRealtimePersistenceEvidence $rtRequest $approachRows $proofGame;$passes++
$approachBarrier.detail.approach.travelled=0
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $approachRows $proofGame} 'P04 accepted stationary approach evidence'
$approachBarrier.detail.approach.travelled=0.7;$approachBarrier.detail.ordinaryAttacks=1
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $approachRows $proofGame} 'P04 approach accepted an already launched attack'
$approachBarrier.detail.ordinaryAttacks=0;$approachBarrier.detail.deferredSaves=1
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $approachRows $proofGame} 'P04 approach silently changed native snapshot timing'
$approachBarrier.detail.deferredSaves=0;$approachRows[-1].detail.inputRequests=2
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $approachRows $proofGame} 'P04 warm approach concealed discarded intent with new input'
$approachRows[-1].detail.inputRequests=1
$approachLoaded=[pscustomobject]@{kind='rt-cold-approach-restored';processId=456;detail=[pscustomobject]@{actual=[pscustomobject]@{
    target='t';targetDamage=0;resolved=0;ordinaryAttacks=0;inputRequests=0;unresolvedProjectiles=$false;
    riderPosition=@(1.0,2.0,3.0);mountPosition=@(2.0,2.0,3.0)}}}
Assert-KmcApproachColdOutcome @($approachBarrier) @($approachLoaded);$passes++
$approachLoaded.detail.actual.riderPosition[0]=0
Must-Reject {Assert-KmcApproachColdOutcome @($approachBarrier) @($approachLoaded)} 'P04 cold movement returned to the origin'
$approachLoaded.detail.actual.riderPosition[0]=1;$approachLoaded.detail.actual.inputRequests=1
Must-Reject {Assert-KmcApproachColdOutcome @($approachBarrier) @($approachLoaded)} 'P04 injected cold approach before observation'
$approachLoaded.detail.actual.inputRequests=0;$approachLoaded.processId=123
Must-Reject {Assert-KmcApproachColdOutcome @($approachBarrier) @($approachLoaded)} 'P04 accepted a warm approach reload'
# Casting evidence has no injected health/slot data in the native request.
$rtRequest.persistenceCase='unmounted-casting'
$castBefore=[pscustomobject]@{caster='c';subject='m';blueprint='heal';slotCount=1;availableSlots=1;slotAvailable=$true;
    spellAvailable=$true;inputs=1;heals=0;damage=3;standard=0;
    commands=@([pscustomobject]@{blueprint='heal';started=$true;acted=$false;finished=$false})}
$castAfter=[pscustomobject]@{caster='c';subject='m';blueprint='heal';slotCount=1;availableSlots=0;slotAvailable=$false;
    spellAvailable=$false;inputs=1;heals=1;damage=0;standard=4;commands=@()}
$castRequest=New-RtProof 'rt-casting-save-request' 3 ([pscustomobject]@{casting=$castBefore;snapshotCount=0})
$castWait=New-RtProof 'rt-native-wait-started' 4 ([pscustomobject]@{nativeSaveWaiting=$true;snapshotCount=0;deferredSaves=1})
$castActual=[pscustomobject]@{casting=$castAfter;inputRequests=0;resolved=0;deferredSaves=1;snapshotCount=1;
    unresolvedAbilities=$false;unresolvedProjectiles=$false}
$rtSnapshot.Combat.Actors=@([pscustomobject]@{Native=[pscustomobject]@{Id='r';Standard=0}},[pscustomobject]@{Native=[pscustomobject]@{Id='c';Standard=4}})
$rtWrite.actual=$castActual
$castingRows=@($rtRows|Where-Object {$_.kind-cnotin @('rt-repeated-attack-requested','rt-repeated-attack-resolved')})
foreach($row in $castingRows){$row.checkpoint=$rtRequest.persistenceCase}
$castingRows+=@((New-RtProof 'rt-casting-input' 1 $null),$castRequest,$castWait,
    (New-RtProof 'rt-casting-continuation' 6 ([pscustomobject]@{resolved=0;casting=$castAfter})),
    (New-RtProof 'rt-casting-first-delivery' 6 ([pscustomobject]@{resolved=1;inputRequests=1;casting=$castAfter})))
Assert-KmcRealtimePersistenceEvidence $rtRequest $castingRows $proofGame;$passes++
$castAfter.heals=2
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $castingRows $proofGame} 'P04 duplicated a spell effect before saving'
$castAfter.heals=1;$castAfter.availableSlots=1
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $castingRows $proofGame} 'P04 refunded the native spell slot'
$castAfter.availableSlots=0;$castActual.unresolvedAbilities=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $castingRows $proofGame} 'P04 erased an unfinished native spell process'
$castActual.unresolvedAbilities=$false;$castBefore.commands[0].acted=$true
Must-Reject {Assert-KmcRealtimePersistenceEvidence $rtRequest $castingRows $proofGame} 'P04 cast request occurred after delivery'
$castBefore.commands[0].acted=$false
$castCold=New-RtProof 'rt-cold-debt-restored' 6 ([pscustomobject]@{actual=($castActual|ConvertTo-Json -Depth 10|ConvertFrom-Json)})
$castCold.processId=456;$castCold.detail.actual.casting.inputs=0;$castCold.detail.actual.casting.heals=0
$castWrite=@($castingRows|Where-Object kind -CEQ 'native-write-complete')
Assert-KmcCastingColdOutcome $castWrite @($castCold);$passes++
$castCold.detail.actual.casting.damage=3
Must-Reject {Assert-KmcCastingColdOutcome $castWrite @($castCold)} 'P04 lost native healing on cold load'
$castCold.detail.actual.casting.damage=0;$castCold.detail.actual.casting.availableSlots=1
Must-Reject {Assert-KmcCastingColdOutcome $castWrite @($castCold)} 'P04 cold reconstructed a consumed slot'
$castCold.detail.actual.casting.availableSlots=0;$castCold.detail.actual.casting.heals=1
Must-Reject {Assert-KmcCastingColdOutcome $castWrite @($castCold)} 'P04 cold replayed healing'
$castCold.detail.actual.casting.heals=0;$castCold.processId=123
Must-Reject {Assert-KmcCastingColdOutcome $castWrite @($castCold)} 'P04 accepted warm casting reload'


# Condition/split proof keeps the already charged native obligation and real harm.
$conditionSnapshot=[pscustomobject]@{Mounted=$false;Combat=[pscustomobject]@{
 TurnBased=$true;Round=1;Current=[pscustomobject]@{ActorId='r'}
 Actors=@([pscustomobject]@{Native=[pscustomobject]@{Id='r';Standard=0;Move=0}},
          [pscustomobject]@{Native=[pscustomobject]@{Id='m';Standard=12;Move=3}})
 Paired=[pscustomobject]@{RiderId='r';MountId='m';BoundaryIsCurrent=$true;Activation=[pscustomobject]@{
 Split=$true;Rider=[pscustomobject]@{Ended=$false};Mount=[pscustomobject]@{Ended=$true;ForfeitRecorded=$true;ForfeitAdded=6}
 }}
}}
Assert-KmcConditionSnapshot $conditionSnapshot;$passes++
$conditionSnapshot.Mounted=$true
Must-Reject {Assert-KmcConditionSnapshot $conditionSnapshot} 'P03 condition invented a remounted pair'
$conditionSnapshot.Mounted=$false;$conditionSnapshot.Combat.Paired.Activation.Mount.Ended=$false
Must-Reject {Assert-KmcConditionSnapshot $conditionSnapshot} 'P03 condition granted an ended actor'
$conditionSnapshot.Combat.Paired.Activation.Mount.Ended=$true;$conditionSnapshot.Combat.Paired.Activation.Mount.ForfeitRecorded=$false
Must-Reject {Assert-KmcConditionSnapshot $conditionSnapshot} 'P03 condition lost its forfeiture bookkeeping'
$conditionSnapshot.Combat.Paired.Activation.Mount.ForfeitRecorded=$true;$conditionSnapshot.Combat.Actors[0].Native.Standard=6
Must-Reject {Assert-KmcConditionSnapshot $conditionSnapshot} 'P03 condition consumed the principal remainder'
$conditionSnapshot.Combat.Actors[0].Native.Standard=0
$conditionState=[pscustomobject]@{
 combat=[pscustomobject]@{round=1};damage=2;originalDamage=0;mountEnded=$true;riderEnded=$false
 nativeRiderPreparations=1;nativeMountPreparations=1;conditionActive=$false;nativePartPresent=$false
 stimulus=[pscustomobject]@{nativeSelfDamageRules=1;choiceOverrides=1;cleanupResourcesUnchanged=$true;directControlRestored=$true}
 fact=[pscustomobject]@{damageRestorationEnabled=$false;healPerRound=0;restored=$true}
}
function New-ConditionProof([string]$kind,$detail){
 [pscustomobject]@{runId=$sourceId;scenario='persistence-p03-save';source='source';dll='dll';processId=123;checkpoint='condition'
 kind=$kind;relationship='Unmounted';rider=[pscustomobject]@{Id='r';Standard=0};mount=[pscustomobject]@{Id='m';Standard=12}
 controls=[pscustomobject]@{DuplicateFactCount=0;NativeCastRequestCount=0};native=[pscustomobject]@{targetId='t'};detail=$detail}
}
$conditionWrite=[pscustomobject]@{condition=$conditionState;snapshot=$conditionSnapshot;path=$path;nativeType='Manual'
 operation='None';nativeCallback=$true;sha256=(Get-KmcSha256 $path);length=(Get-Item -LiteralPath $path).Length}
$conditionRows=@((New-ConditionProof 'initial' $conditionState),
 (New-ConditionProof 'condition-forfeit-retained' $conditionState),
 (New-ConditionProof 'condition-remainder-before-input' $conditionState),
 (New-ConditionProof 'native-write-complete' $conditionWrite),
 (New-ConditionProof 'attack-delivered' ([pscustomobject]@{rules=1;rolls=1})),
 (New-ConditionProof 'usable-continuation-complete' ([pscustomobject]@{riderPreparations=2;mountPreparations=2;turnVisits=@('1r','1t','2r','2m')})))
foreach($ordinal in @(1,2)){foreach($actor in @('r','m')){
 $conditionRows+=New-ConditionProof 'next-independent-activation' ([pscustomobject]@{actor=$actor;count=$ordinal;round=($ordinal+1)})
}}
$conditionRequest=[pscustomobject]@{runId=$sourceId;scenario='persistence-p03-save';persistenceCase='condition';commit='source';dllSha256='dll'}
Assert-KmcConditionPersistenceEvidence $conditionRequest $conditionRows ([pscustomobject]@{processId=123});$passes++
$conditionState.fact.damageRestorationEnabled=$true
Must-Reject {Assert-KmcConditionPersistenceEvidence $conditionRequest $conditionRows ([pscustomobject]@{processId=123})} 'P03 test cleanup erased native harm'
$conditionState.fact.damageRestorationEnabled=$false;$conditionState.stimulus.nativeSelfDamageRules=2
Must-Reject {Assert-KmcConditionPersistenceEvidence $conditionRequest $conditionRows ([pscustomobject]@{processId=123})} 'P03 duplicated native condition resolution'
$conditionState.stimulus.nativeSelfDamageRules=1;$conditionRows[-1].detail.count=1
Must-Reject {Assert-KmcConditionPersistenceEvidence $conditionRequest $conditionRows ([pscustomobject]@{processId=123})} 'P03 missed a later true actor preparation'
$conditionRows[-1].detail.count=2
$conditionColdState=$conditionState|ConvertTo-Json -Depth 8|ConvertFrom-Json
$conditionColdState.nativeRiderPreparations=0;$conditionColdState.nativeMountPreparations=0
$conditionColdState.stimulus=$null;$conditionColdState.fact=$null
$conditionCold=New-ConditionProof 'initial' $conditionColdState
$conditionCold.processId=456
Assert-KmcConditionColdOutcome $conditionRows @($conditionCold);$passes++
$conditionCold.detail.damage=0
Must-Reject {Assert-KmcConditionColdOutcome $conditionRows @($conditionCold)} 'P03 lost native self-harm on load'
$conditionCold.detail.damage=2;$conditionCold.detail.nativeMountPreparations=1
Must-Reject {Assert-KmcConditionColdOutcome $conditionRows @($conditionCold)} 'P03 cold replayed preparation'
$conditionCold.detail.nativeMountPreparations=0;$conditionCold.processId=123
Must-Reject {Assert-KmcConditionColdOutcome $conditionRows @($conditionCold)} 'P03 accepted a warm condition reload'

# The preparation request occurs before native command creation; serialization
# waits for real completion/forfeiture and never serializes that command.
$preparationRequest=New-ConditionProof 'condition-preparation-save-request' ([pscustomobject]@{
 nativePreparing=$true;commandPresent=$false;snapshotCount=0})
$preparationWait=New-ConditionProof 'condition-preparation-wait' ([pscustomobject]@{
 waiting=$true;ownedPreparationStart=$true;unownedOrdinaryStart=$false;deferredSaves=1;snapshotCount=0;condition=[pscustomobject]@{
 command=[pscustomobject]@{type='Kingmaker.UnitLogic.Commands.UnitSelfHarm';ignoreCooldown=$false}}})
$preparationBarrier=New-ConditionProof 'condition-preparation-barrier' ([pscustomobject]@{
 snapshotCount=0;deferredSaves=1;condition=[pscustomobject]@{
 command=[pscustomobject]@{started=$true;finished=$true;result='Success'}
 damage=2;mountEnded=$true;riderEnded=$false}})
$preparationRows=@($preparationRequest,$preparationWait,$preparationBarrier)
Assert-KmcConditionPreparationEvidence $preparationRows;$passes++
$preparationRequest.detail.commandPresent=$true
Must-Reject {Assert-KmcConditionPreparationEvidence $preparationRows} 'P03 preparation request occurred after command creation'
$preparationRequest.detail.commandPresent=$false;$preparationWait.detail.snapshotCount=1
Must-Reject {Assert-KmcConditionPreparationEvidence $preparationRows} 'P03 serialized before preparation completed'
$preparationWait.detail.snapshotCount=0;$preparationWait.detail.unownedOrdinaryStart=$true
Must-Reject {Assert-KmcConditionPreparationEvidence $preparationRows} 'P03 preparation wait permitted an unowned ordinary attack'
$preparationWait.detail.unownedOrdinaryStart=$false;$preparationBarrier.detail.condition.command.finished=$false
Must-Reject {Assert-KmcConditionPreparationEvidence $preparationRows} 'P03 preparation barrier abandoned a native command'
$preparationBarrier.detail.condition.command.finished=$true;$preparationBarrier.detail.condition.mountEnded=$false
Must-Reject {Assert-KmcConditionPreparationEvidence $preparationRows} 'P03 preparation barrier lost native forfeiture'
$preparationBarrier.detail.condition.mountEnded=$true
$conditionRequest.persistenceCase='condition-preparing'
$allConditionRows=@($conditionRows)+$preparationRows
foreach($row in $allConditionRows){$row.checkpoint='condition-preparing'}
Assert-KmcConditionPersistenceEvidence $conditionRequest $allConditionRows ([pscustomobject]@{processId=123});$passes++

$delaySnapshot=[pscustomobject]@{Rider=[pscustomobject]@{Id='r';Standard=1;Move=0};Mount=[pscustomobject]@{Id='m';Standard=0;Move=0};Combat=[pscustomobject]@{
    Round=2;Current=$null;Paired=[pscustomobject]@{BoundaryIsCurrent=$false;Boundary=[pscustomobject]@{ActorId='r';Status=4};Activation=[pscustomobject]@{
        Sequence=2;Suspended=$true;Split=$false;Ending=$false;Rider=[pscustomobject]@{Ended=$false};Mount=[pscustomobject]@{Ended=$false}}}}}
Assert-KmcP03Snapshot $delaySnapshot 'suspended';$passes++
$delaySnapshot.Combat.Paired.Activation.Suspended=$false
Must-Reject {Assert-KmcP03Snapshot $delaySnapshot 'suspended'} 'Delay snapshot without suspended participation accepted'
$delaySnapshot.Combat.Paired.Activation.Suspended=$true;$delaySnapshot.Combat.Paired.Activation.Rider.Ended=$true
Must-Reject {Assert-KmcP03Snapshot $delaySnapshot 'suspended'} 'Delay renewed ended rider accepted'
$delaySnapshot.Combat.Paired.Activation.Rider.Ended=$false;$delaySnapshot.Combat.Paired.Boundary.Status=2
Must-Reject {Assert-KmcP03Snapshot $delaySnapshot 'suspended'} 'Preparing boundary accepted as pending Delay'
$delaySnapshot.Combat.Paired.Boundary.Status=4;$delaySnapshot.Combat.Current=[pscustomobject]@{ActorId='r'}
Must-Reject {Assert-KmcP03Snapshot $delaySnapshot 'suspended'} 'Already resumed save accepted as suspended'
$delaySnapshot.Combat.Current=$null;$delaySnapshot.Mount.Standard=6
Must-Reject {Assert-KmcP03Snapshot $delaySnapshot 'suspended'} 'Spent mount accepted as unused suspended pair'
$delaySnapshot.Mount.Standard=0
$delayRows=@()
foreach($kind in @('suspended-retained','suspended-grant-resumed','cross-round-delay-rejected','next-paired-activation','next-paired-activation')){
    $increment=if($delayRows.Count-lt3){0}else{$delayRows.Count-2}
    $effect=[pscustomobject]@{rounds=1;damage=2}
    $counts=[pscustomobject]@{'clear-after'=$increment;'round-state-after'=$increment}
    $delayRows += [pscustomobject]@{kind=$kind;detail=[pscustomobject]@{sequence=(2+$increment);round=(2+$increment);
        delayNativeCounts=[pscustomobject]@{rider=($counts|ConvertTo-Json|ConvertFrom-Json);mount=($counts|ConvertTo-Json|ConvertFrom-Json)};
        roundEffects=[pscustomobject]@{rider=$effect;mount=$effect}}}
}
Assert-KmcSuspendedEvidence $delayRows $false;$passes++
$delayRows[1].detail.delayNativeCounts.mount.'round-state-after'=1
Must-Reject {Assert-KmcSuspendedEvidence $delayRows $false} 'Load replayed mount round effect accepted'
$delayRows[1].detail.delayNativeCounts.mount.'round-state-after'=0;$delayRows[4].detail.delayNativeCounts.rider.'clear-after'=1
Must-Reject {Assert-KmcSuspendedEvidence $delayRows $false} 'Missing real next-round clear accepted'
$delayRows[4].detail.delayNativeCounts.rider.'clear-after'=2;$delayRows[1].detail.sequence=3
Must-Reject {Assert-KmcSuspendedEvidence $delayRows $false} 'Delay fresh activation fallback accepted'

# Three synthetic files exercise only the outer queue evidence validator.
$queueRoot=Join-Path $script:ownedTestLab 'queued-completion'
[void][IO.Directory]::CreateDirectory($queueRoot)
[IO.File]::WriteAllText((Join-Path $queueRoot 'Working.zks'),'protected-input')
$queueNames=@('KMC_P05_QUEUE_1','KMC_P05_QUEUE_2','KMC_P01')
$queueRows=@([pscustomobject]@{kind='queued-native-requests';rider=[pscustomobject]@{Id='r'};mount=[pscustomobject]@{Id='m'};
    detail=[pscustomobject]@{count=3;snapshots=0;callbacks=0;names=$queueNames}})
for($i=0;$i-lt3;$i++){
 $queueRows += [pscustomobject]@{kind='queued-native-callback';detail=[pscustomobject]@{ordinal=($i+1);snapshots=($i+1)}}
 $queuePath=Join-Path $queueRoot ('Manual_'+(300+$i)+'_'+$queueNames[$i]+'.zks')
 [IO.File]::WriteAllText($queuePath,('owned-synthetic-'+$i))
 $queueRows += [pscustomobject]@{kind=$(if($i-lt2){'queued-native-write-complete'}else{'native-write-complete'});
  detail=[pscustomobject]@{ordinal=($i+1);path=$queuePath;nativeType='Manual';nativeCallback=$true;operation='None';
   sha256=(Get-KmcSha256 $queuePath);length=(Get-Item $queuePath).Length;
   snapshot=[pscustomobject]@{Mounted=$true;Rider=[pscustomobject]@{Id='r'};Mount=[pscustomobject]@{Id='m'}}}}
}
Assert-KmcQueuedSaveEvidence $queueRows $queueRoot;$passes++
$queueRows[0].detail.snapshots=1
Must-Reject {Assert-KmcQueuedSaveEvidence $queueRows $queueRoot} 'Queue creation counted as native snapshot'
$queueRows[0].detail.snapshots=0;$queueRows[3].detail.ordinal=1
Must-Reject {Assert-KmcQueuedSaveEvidence $queueRows $queueRoot} 'Duplicate native callback accepted'
$queueRows[3].detail.ordinal=2;$queueRows[4].detail.operation='Saving'
Must-Reject {Assert-KmcQueuedSaveEvidence $queueRows $queueRoot} 'Callback accepted as completed native archive'
$queueRows[4].detail.operation='None';$queueRows[6].detail.sha256=('a'*64)
Must-Reject {Assert-KmcQueuedSaveEvidence $queueRows $queueRoot} 'Wrong queued archive accepted'
$queueRows[6].detail.sha256=Get-KmcSha256 $queueRows[6].detail.path
Copy-Item -LiteralPath (Join-Path $saveRoot 'Manual_300_KMC_P01.zks') -Destination (Join-Path $saveRoot 'Manual_302_KMC_P01.zks')
$result.scenario='persistence-p05-save';$result.status='PASS';$result.modsRestored=$true;$result.workingRestored=$true
Write-KmcJsonAtomic $resultPath $result
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p05-save';persistenceCase='queued';transactionToken=('a'*64)})
$queuedHash=Get-KmcSha256 (Join-Path $saveRoot 'Manual_302_KMC_P01.zks')
$queuedSource=Get-KmcPersistenceSource $sourceId $queuedHash $fixture -NativeCase queued
if($queuedSource.descriptor.fileName-cne'Manual_302_KMC_P01.zks'){throw 'Queued cold source silently substituted another archive'};$passes++
Must-Reject {Get-KmcPersistenceSource $sourceId $queuedHash $fixture -NativeCase manual} 'Queued source admitted wrong category/leaf'


$result.scenario='persistence-p07-save'
Write-KmcJsonAtomic $resultPath $result
foreach($case in @('timeout','cancel-wait','locked-replace','area-reload')){
    Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$sourceId;scenario='persistence-p07-save';persistenceCase=$case;transactionToken=('a'*64)})
    $recoveryHash=Get-KmcSha256 $path
    $recoverySource=Get-KmcPersistenceSource $sourceId $recoveryHash $fixture -NativeCase $case
    if($recoverySource.descriptor.fileName-cne'Manual_300_KMC_P01.zks'){throw 'P07 cold archive was substituted'};$passes++
    Must-Reject {Get-KmcPersistenceSource $sourceId $recoveryHash $fixture -NativeCase manual} 'P07 admitted a mismatched source category'
    Must-Reject {Get-KmcPersistenceSource $sourceId $recoveryHash $fixture} 'P07 source omitted its explicit case'
}
$recoveryRequest=[pscustomobject]@{runId='owned-recovery';persistenceCase='timeout'}
$recoveryArchive=Join-Path $script:ownedTestLab 'runtime-staging/persistence-owned-recovery/Saved Games/Manual_300_KMC_P01.zks'
$recoveryRows=@(
    foreach($kind in @('recovery-initial-write','recovery-wait-started','recovery-unwritten-operation','recovery-last-good-loaded','native-write-complete')){
        [pscustomobject]@{kind=$kind;checkpoint='timeout';gameTicks=100;
            persistence=[pscustomobject]@{semantics=2;presentation=1};
            native=[pscustomobject]@{paused=$false;mode='Default'};
            controls=[pscustomobject]@{SerializationSuspended=$false};
            detail=[pscustomobject]@{path=$recoveryArchive;sha256=('a'*64);snapshots=1;failedSaveCallback=$false;
                canceledLoadCallback=$false;failedSaves=1;nativeWorldDisposals=1;nativeCallback=$true;ordinal=2}}
    }
)
$recoveryRows[2].gameTicks=110
$recoveryRows[3].persistence.semantics=4;$recoveryRows[3].persistence.presentation=2
$recoveryRows[3].detail.nativeWorldDisposals=2;$recoveryRows[4].detail.sha256=('b'*64)
Assert-KmcRecoveryPersistenceEvidence $recoveryRequest $recoveryRows;$passes++
foreach($bad in @('false-success','new-world','no-clock','changed-last-good','no-reload','no-retry')){
    $copy=($recoveryRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
    switch($bad){
        'false-success' {$copy[2].detail.failedSaveCallback=$true}
        'new-world' {$copy[2].persistence.semantics=4}
        'no-clock' {$copy[2].gameTicks=100}
        'changed-last-good' {$copy[2].detail.sha256=('b'*64)}
        'no-reload' {$copy[3].detail.nativeWorldDisposals=1}
        'no-retry' {$copy[4].detail.sha256=('a'*64)}
    }
    Must-Reject {Assert-KmcRecoveryPersistenceEvidence $recoveryRequest $copy} ('P07 accepted '+$bad)
}

$commitRows=($recoveryRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
$recoveryRequest.persistenceCase='locked-replace'
foreach($row in $commitRows){
    $row.checkpoint='locked-replace'
    $row.detail|Add-Member replacementFailures 0
}
$commitRows[2].kind='recovery-failed-commit'
$commitRows[2].detail.snapshots=2;$commitRows[3].detail.snapshots=2
$commitRows[2].detail.replacementFailures=1
$commitRows[2].gameTicks=$commitRows[1].gameTicks
Assert-KmcRecoveryPersistenceEvidence $recoveryRequest $commitRows;$passes++
foreach($bad in @('no-commit-failure','no-snapshot','clock-rewind')){
    $copy=($commitRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
    switch($bad){
        'no-commit-failure' {$copy[2].detail.replacementFailures=0}
        'no-snapshot' {$copy[2].detail.snapshots=1}
        'clock-rewind' {$copy[2].gameTicks=99}
    }
    Must-Reject {Assert-KmcRecoveryPersistenceEvidence $recoveryRequest $copy} ('P07 accepted '+$bad)
}

# An ordinary native transfer keeps CrossSceneRoot, so the party pair retains
# its exact views; the qualification is that they stay live, bound and singly
# owned, and that the recorded label agrees with the measured instance IDs.
$areaRequest=[pscustomobject]@{persistenceCase='area-reload';fixture=[pscustomobject]@{working=[pscustomobject]@{
    area=('a'*32);gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'}}}
function New-KmcAreaActorRow { param([string]$Id,[int]$View)
    [pscustomobject]@{id=$Id;nativeActorCount=1;baselineViewId=$View;viewId=$View;viewAlive=$true;
        viewBound=$true;viewDisposition='retained';viewExactForPair=$true;boundToRelationship=$true}
}
function New-KmcAreaRow { param([string]$Kind,[string]$Area,[string]$Expected)
    [pscustomobject]@{kind=$Kind;gameTicks=100;native=[pscustomobject]@{paused=$false};
        persistence=[pscustomobject]@{semantics=0;presentation=0};
        controls=[pscustomobject]@{ExactFactCount=5;ManagedHotbarSlotCount=2;SerializationSuspended=$false};
        rider=[pscustomobject]@{Id='owned-rider';Standard=2;Move=1;Swift=3;Initiative=0;Reaction=1;ReactionsRemaining=0};
        mount=[pscustomobject]@{Id='owned-mount';Standard=0;Move=2;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1};
        detail=[pscustomobject]@{area=$Area;expectedArea=$Expected;sourceArea=('a'*32);suspensions=0;resumes=0;pending=$false;
            suspensionObserved=$true;loadingFrames=10;sameWorld=$true;riderView=1;mountView=2;
            nativeCastRequests=0;ordinal=2;sha256=('a'*64);expectedViewDisposition='retained';
            loadingInProcess=$false;queuedLoads=0;deferredSaveWaiting=$false;mountedInvariant=$null;
            presentation='relationship=Mounted;riderViewExact=True';
            riderActor=(New-KmcAreaActorRow 'owned-rider' 1);mountActor=(New-KmcAreaActorRow 'owned-mount' 2)}}
}
$areaRows=@(
    foreach($kind in @('area-initial-write','area-reload-requested','area-reload-observed','area-reload-complete','native-write-complete')){
        New-KmcAreaRow $kind ('a'*32) ('a'*32)
    }
)
foreach($i in 2,3){$areaRows[$i].detail.suspensions=1;$areaRows[$i].detail.resumes=1}
$areaRows[4].detail.sha256=('b'*64)
Assert-KmcAreaPersistenceEvidence $areaRequest $areaRows;$passes++
foreach($bad in @('replaced-views','mislabeled-disposition','missing-view','unbound-view','stale-pair-view',
    'duplicate-native-actor','broken-invariant','relabeled-expectation','no-observation','late-observation',
    'legacy-view-drift','unsettled-queue','observed-mid-load','no-unload','duplicate-resume','wrong-world',
    'remount','debt-refund','reaction-refresh','missing-slots')){
    $copy=($areaRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
    switch($bad){
        'replaced-views' {$copy[3].detail.riderActor.viewId=9;$copy[3].detail.riderActor.viewDisposition='replaced';$copy[3].detail.riderView=9}
        'mislabeled-disposition' {$copy[3].detail.mountActor.viewId=9}
        'missing-view' {$copy[2].detail.riderActor.viewAlive=$false;$copy[2].detail.riderActor.viewId=$null}
        'unbound-view' {$copy[3].detail.mountActor.viewBound=$false}
        'stale-pair-view' {$copy[3].detail.riderActor.viewExactForPair=$false}
        'duplicate-native-actor' {$copy[3].detail.mountActor.nativeActorCount=2}
        'broken-invariant' {$copy[3].detail.mountedInvariant='A mounted unit view was detached or replaced.'}
        'relabeled-expectation' {foreach($i in 1,2,3){$copy[$i].detail.expectedViewDisposition='replaced'}}
        'no-observation' {$copy=@($copy[0],$copy[1],$copy[3],$copy[4])}
        'late-observation' {$copy=@($copy[0],$copy[1],$copy[3],$copy[2],$copy[4])}
        'legacy-view-drift' {$copy[3].detail.mountView=9}
        'unsettled-queue' {$copy[3].detail.queuedLoads=1}
        'observed-mid-load' {$copy[2].detail.loadingInProcess=$true}
        'no-unload' {$copy[3].detail.suspensionObserved=$false}
        'duplicate-resume' {$copy[3].detail.resumes=2}
        'wrong-world' {$copy[3].detail.sameWorld=$false}
        'remount' {$copy[3].detail.nativeCastRequests=1}
        'debt-refund' {$copy[3].rider.Standard=0}
        'reaction-refresh' {$copy[3].rider.ReactionsRemaining=1}
        'missing-slots' {$copy[3].controls.ManagedHotbarSlotCount=0}
    }
    Must-Reject {Assert-KmcAreaPersistenceEvidence $areaRequest $copy} ('P07 area accepted '+$bad)
}

# A real cross-area transfer replaces the scenario's opening manual write with
# the engine's own authored autosave, and its barrier counters are the ordering
# proof: AfterEntry must already see the restored pair in the destination,
# BeforeExit must still see it mounted in the departure area.
foreach($mode in @('AfterEntry','BeforeExit')){
    $case=if($mode-ceq'AfterEntry'){'area-cross-entry'}else{'area-cross-exit'}
    $source=('a'*32);$target=('e'*32)
    $autosaveArea=if($mode-ceq'AfterEntry'){$target}else{$source}
    $resumes=if($mode-ceq'AfterEntry'){1}else{0}
    $crossRequest=[pscustomobject]@{persistenceCase=$case
        fixture=[pscustomobject]@{working=[pscustomobject]@{area=$source;gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'}}
        persistenceAreaTarget=[pscustomobject]@{enterPoint=('d'*32);area=$target;autoSaveMode=$mode}}
    $crossRows=@(
        (New-KmcAreaRow 'area-reload-requested' $source $target),
        (New-KmcAreaRow 'area-reload-observed' $target $target),
        (New-KmcAreaRow 'area-native-autosave' $target $target),
        (New-KmcAreaRow 'area-reload-complete' $target $target),
        (New-KmcAreaRow 'native-write-complete' $target $target)
    )
    foreach($i in 1,2,3){$crossRows[$i].detail.suspensions=1;$crossRows[$i].detail.resumes=1}
    $crossRows[4].detail.sha256=('b'*64)
    $crossRows[2].detail | Add-Member -NotePropertyName mode -NotePropertyValue $mode
    $crossRows[2].detail | Add-Member -NotePropertyName nativeType -NotePropertyValue 'Auto'
    $crossRows[2].detail | Add-Member -NotePropertyName length -NotePropertyValue 4096
    $crossRows[2].detail.expectedArea=$autosaveArea
    $crossRows[2].detail | Add-Member -NotePropertyName snapshot -NotePropertyValue ([pscustomobject]@{
        Mounted=$true;AreaId=$autosaveArea;CampaignId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'
        Rider=[pscustomobject]@{Id='owned-rider'};Mount=[pscustomobject]@{Id='owned-mount'}})
    $crossRows[2].detail | Add-Member -NotePropertyName barrier -NotePropertyValue ([pscustomobject]@{
        relationship='Mounted';area=$autosaveArea;snapshots=1;resumes=$resumes;suspensions=$resumes
        riderId='owned-rider';mountId='owned-mount'})
    Assert-KmcAreaPersistenceEvidence $crossRequest $crossRows;$passes++
    foreach($bad in @('no-autosave','stayed-in-source','autosave-unmounted','autosave-wrong-area','autosave-foreign-campaign',
        'autosave-wrong-actor','barrier-unmounted','barrier-wrong-order','barrier-wrong-area','autosave-not-auto',
        'autosave-aliases-manual','late-autosave','target-equals-source','wrong-expected-area')){
        $copy=($crossRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
        $req=$crossRequest
        switch($bad){
            'no-autosave' {$copy=@($copy[0],$copy[1],$copy[3],$copy[4])}
            'stayed-in-source' {$copy[3].detail.area=$source;$copy[1].detail.area=$source}
            'autosave-unmounted' {$copy[2].detail.snapshot.Mounted=$false}
            'autosave-wrong-area' {$copy[2].detail.snapshot.AreaId=('f'*32)}
            'autosave-foreign-campaign' {$copy[2].detail.snapshot.CampaignId='00000000-0000-0000-0000-000000000001'}
            'autosave-wrong-actor' {$copy[2].detail.snapshot.Rider.Id='other-rider'}
            'barrier-unmounted' {$copy[2].detail.barrier.relationship='Unmounted'}
            'barrier-wrong-order' {$copy[2].detail.barrier.resumes=(1-$resumes)}
            'barrier-wrong-area' {$copy[2].detail.barrier.area=('f'*32)}
            'autosave-not-auto' {$copy[2].detail.nativeType='Manual'}
            'autosave-aliases-manual' {$copy[4].detail.sha256=$copy[2].detail.sha256}
            'late-autosave' {$copy=@($copy[0],$copy[1],$copy[3],$copy[4],$copy[2])}
            'target-equals-source' {$req=[pscustomobject]@{persistenceCase=$case
                fixture=[pscustomobject]@{working=[pscustomobject]@{area=$target;gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'}}
                persistenceAreaTarget=[pscustomobject]@{enterPoint=('d'*32);area=$target;autoSaveMode=$mode}}}
            'wrong-expected-area' {$copy[3].detail.expectedArea=$source}
        }
        Must-Reject {Assert-KmcAreaPersistenceEvidence $req $copy} ('P07 cross-area accepted '+$bad+' ('+$mode+')')
    }
}

# The transition autosave cold-loaded on its own. This is a different claim from
# the destination Manual cold load: it proves the authored Auto archive is itself
# a loadable world. BeforeExit must reopen the departure area and must not
# inherit the destination identity or transfer state of the process that wrote it.
function New-KmcColdActorRow { param([string]$Id,[int]$View)
    [pscustomobject]@{id=$Id;nativeActorCount=1;baselineViewId=$View;viewId=$View;viewAlive=$true;
        viewBound=$true;viewDisposition='cold-world';viewExactForPair=$true;boundToRelationship=$true}
}
foreach($mode in @('AfterEntry','BeforeExit')){
    $case=if($mode-ceq'AfterEntry'){'area-cross-entry-auto'}else{'area-cross-exit-auto'}
    $source=('a'*32);$target=('e'*32);$campaign='89c49a86-a171-4ea9-871a-f6b3b53d19b9'
    # AfterEntry committed in the destination after restoration; BeforeExit
    # committed in the departure area before suspension.
    $openArea=if($mode-ceq'AfterEntry'){$target}else{$source}
    $otherArea=if($mode-ceq'AfterEntry'){$source}else{$target}
    $otherMode=if($mode-ceq'AfterEntry'){'BeforeExit'}else{'AfterEntry'}
    $sourceSha=('c'*64)
    $autoRequest=[pscustomobject]@{persistenceCase=$case
        fixture=[pscustomobject]@{working=[pscustomobject]@{area=$source;gameId=$campaign}}
        persistenceAreaTarget=[pscustomobject]@{enterPoint=('d'*32);area=$target;autoSaveMode=$mode}
        persistenceLoad=[pscustomobject]@{fileName='Auto_1.zks';sha256=$sourceSha}}
    $autoRows=@(
        [pscustomobject]@{kind='auto-cold-loaded';detail=[pscustomobject]@{
            case=$case;autoSaveMode=$mode;expectedArea=$openArea;loadedArea=$openArea;archiveArea=$openArea
            archiveCampaign=$campaign;sourceFileName='Auto_1.zks';sourceSha256=$sourceSha
            suspensions=0;resumes=0;pending=$false
            riderActor=(New-KmcColdActorRow 'owned-rider' 11);mountActor=(New-KmcColdActorRow 'owned-mount' 12)}},
        [pscustomobject]@{kind='native-write-complete';detail=[pscustomobject]@{
            ordinal=1;sha256=('b'*64);nativeType='Manual'
            snapshot=[pscustomobject]@{Mounted=$true;AreaId=$openArea;CampaignId=$campaign}}}
    )
    Assert-KmcTransitionAutoColdEvidence $autoRequest $autoRows;$passes++
    foreach($bad in @('no-load','no-write','wrong-case','swapped-mode','swapped-leg-area','loaded-wrong-area',
        'archive-wrong-area','foreign-campaign','wrong-source-hash','source-role-manual','write-not-manual',
        'write-aliases-source','write-wrong-area','write-unmounted','write-foreign-campaign',
        'inherited-suspension','inherited-resume','pending-transfer','retained-cold-view')){
        $copy=($autoRows|ConvertTo-Json -Depth 12)|ConvertFrom-Json
        switch($bad){
            'no-load' {$copy=@($copy[1])}
            'no-write' {$copy=@($copy[0])}
            'wrong-case' {$copy[0].detail.case='area-cross-entry'}
            'swapped-mode' {$copy[0].detail.autoSaveMode=$otherMode}
            # The whole point of the BeforeExit case: opening the other leg's area.
            'swapped-leg-area' {$copy[0].detail.expectedArea=$otherArea;$copy[0].detail.loadedArea=$otherArea
                $copy[0].detail.archiveArea=$otherArea;$copy[1].detail.snapshot.AreaId=$otherArea}
            'loaded-wrong-area' {$copy[0].detail.loadedArea=('f'*32)}
            'archive-wrong-area' {$copy[0].detail.archiveArea=('f'*32)}
            'foreign-campaign' {$copy[0].detail.archiveCampaign='00000000-0000-0000-0000-000000000001'}
            'wrong-source-hash' {$copy[0].detail.sourceSha256=('d'*64)}
            'source-role-manual' {$copy[0].detail.sourceFileName='Manual_300_KMC_P01.zks'}
            'write-not-manual' {$copy[1].detail.nativeType='Auto'}
            'write-aliases-source' {$copy[1].detail.sha256=$sourceSha}
            'write-wrong-area' {$copy[1].detail.snapshot.AreaId=('f'*32)}
            'write-unmounted' {$copy[1].detail.snapshot.Mounted=$false}
            'write-foreign-campaign' {$copy[1].detail.snapshot.CampaignId='00000000-0000-0000-0000-000000000001'}
            'inherited-suspension' {$copy[0].detail.suspensions=1}
            'inherited-resume' {$copy[0].detail.resumes=1}
            'pending-transfer' {$copy[0].detail.pending=$true}
            'retained-cold-view' {$copy[0].detail.riderActor.viewDisposition='retained'}
        }
        Must-Reject {Assert-KmcTransitionAutoColdEvidence $autoRequest $copy} ('P07 transition auto cold accepted '+$bad+' ('+$mode+')')
    }
}

# --- Save-worker drain: cancellation while the worker can still commit --------
function New-KmcDrainRow { param([string]$Kind)
    [pscustomobject]@{kind=$Kind;checkpoint='serialization-cancel'
        detail=[pscustomobject]@{case='serialization-cancel';preparedLeaf='Manual_300_KMC_P01.zks';workerTaskId=41
            workerRunning=$true;workerHeld=$true;heldLeaf='Manual_300_KMC_P01.zks';workerHolds=1;workerEntries=1
            draining=$false;activeScope=$true;deferredCancellations=0;drains=0;drainCommitted=$false
            saveSuspended=$true;serializationSuspended=$true;saveCallback=$false;snapshots=1;failedSaves=0
            rejections=0;nativeWorldDisposals=0;overlapRefused=$false;loadRefused=$false
            repeatedStopSafe=$false;disableRefused=$false;ordinal=1;sha256=('a'*64)
            lastGoodSha256=('a'*64);currentSha256=$null;interruptedPath='C:\owned\Manual_300_KMC_P01.zks';replacedInPlace=$true
            # The held world while the worker can still commit.
            pausedBeforeStop=$false;heldPaused=$false;pauseRestored=$false;resetDeferred=$false;resetPending=$false
            unrelatedActorId='';unrelatedCommandQueued=$false;simCommandQueued=$false;simCommandStarted=$false
            simTicksAdvanced=0;simWorkerStillHeld=$false}}
}
$drainRequest=[pscustomobject]@{persistenceCase='serialization-cancel'}
$drainRows=@(
    (New-KmcDrainRow 'drain-initial-write'),(New-KmcDrainRow 'worker-in-flight-observed'),
    (New-KmcDrainRow 'drain-cancellation-deferred'),(New-KmcDrainRow 'drain-settled'),
    (New-KmcDrainRow 'native-write-complete'),(New-KmcDrainRow 'drain-simulation-probe'))
$dd=$drainRows[2].detail
$dd.draining=$true;$dd.deferredCancellations=1
$dd.overlapRefused=$true;$dd.loadRefused=$true;$dd.repeatedStopSafe=$true;$dd.disableRefused=$true
$dd.currentSha256=('a'*64)
# The held world: the main-menu reset deferred (and its replay discarded by the
# diagnostic), the world paused the moment the abandonment deferred.
$dd.resetDeferred=$true;$dd.heldPaused=$true;$dd.resetPending=$false
$ds=$drainRows[3].detail
$ds.draining=$false;$ds.activeScope=$false;$ds.deferredCancellations=1;$ds.drains=1
$ds.saveSuspended=$false;$ds.serializationSuspended=$false
$ds.drainCommitted=$true;$ds.currentSha256=('b'*64);$ds.failedSaves=0
$ds.pauseRestored=$true
$drainRows[4].detail.ordinal=2;$drainRows[4].detail.sha256=('b'*64)
$dp=$drainRows[5].detail
$dp.draining=$true;$dp.deferredCancellations=1;$dp.simWorkerStillHeld=$true;$dp.unrelatedActorId='unit-b'
Assert-KmcRecoveryPersistenceEvidence $drainRequest $drainRows;$passes++
# The uncommitted settlement is equally valid and equally checked.
$uncommitted=($drainRows|ConvertTo-Json -Depth 16)|ConvertFrom-Json
$uncommitted[3].detail.drainCommitted=$false;$uncommitted[3].detail.failedSaves=1;$uncommitted[3].detail.currentSha256=('a'*64)
Assert-KmcRecoveryPersistenceEvidence $drainRequest $uncommitted;$passes++
foreach($bad in @('no-flight','worker-finished','no-hold','held-other-leaf','already-draining','lease-dropped',
    'no-active-scope','released-instead-of-deferred','reported-cancellation','overlap-allowed','load-disposed-world',
    'repeat-released','disable-released','last-good-changed','settled-twice','lease-left-held','different-worker',
    'committed-without-change','committed-without-path','committed-reported-failed','uncommitted-not-reported','uncommitted-changed-archive',
    'no-subsequent-write','out-of-order',
    'reset-not-deferred','world-not-held','reset-left-pending','pause-not-restored',
    'no-simulation-probe','owned-command-queued','unrelated-command-queued','clock-advanced','probe-after-release')){
    $n=($drainRows|ConvertTo-Json -Depth 16)|ConvertFrom-Json
    switch($bad){
        'no-flight' {$n=@($n[0],$n[2],$n[3],$n[4],$n[5])}
        'worker-finished' {$n[1].detail.workerRunning=$false}
        'no-hold' {$n[1].detail.workerHeld=$false}
        'held-other-leaf' {$n[1].detail.heldLeaf='Manual_301_OTHER.zks'}
        'already-draining' {$n[1].detail.deferredCancellations=1}
        'lease-dropped' {$n[1].detail.serializationSuspended=$false}
        'no-active-scope' {$n[1].detail.activeScope=$false}
        'released-instead-of-deferred' {$n[2].detail.draining=$false}
        'reported-cancellation' {$n[2].detail.saveCallback=$true}
        'overlap-allowed' {$n[2].detail.overlapRefused=$false}
        'load-disposed-world' {$n[2].detail.nativeWorldDisposals=1}
        'repeat-released' {$n[2].detail.repeatedStopSafe=$false}
        'disable-released' {$n[2].detail.disableRefused=$false}
        'last-good-changed' {$n[2].detail.currentSha256=('c'*64)}
        'settled-twice' {$n[3].detail.drains=2}
        'lease-left-held' {$n[3].detail.serializationSuspended=$true}
        'different-worker' {$n[2].detail.workerTaskId=99}
        'committed-without-change' {$n[3].detail.currentSha256=('a'*64)}
        'committed-without-path' {$n[3].detail.interruptedPath=''}
        'committed-reported-failed' {$n[3].detail.failedSaves=1}
        'uncommitted-not-reported' {$n[3].detail.drainCommitted=$false;$n[3].detail.failedSaves=0;$n[3].detail.currentSha256=('a'*64)}
        'uncommitted-changed-archive' {$n[3].detail.drainCommitted=$false;$n[3].detail.failedSaves=1}
        'no-subsequent-write' {$n=@($n[0],$n[1],$n[2],$n[3],$n[5])}
        'out-of-order' {$n=@($n[0],$n[2],$n[1],$n[3],$n[4],$n[5])}
        # The held world: each rule rejects on its own, with every other row valid.
        'reset-not-deferred' {$n[2].detail.resetDeferred=$false}
        'world-not-held' {$n[2].detail.heldPaused=$false}
        'reset-left-pending' {$n[2].detail.resetPending=$true}
        'pause-not-restored' {$n[3].detail.pauseRestored=$false}
        'no-simulation-probe' {$n=@($n[0],$n[1],$n[2],$n[3],$n[4])}
        'owned-command-queued' {$n[5].detail.simCommandQueued=$true}
        'unrelated-command-queued' {$n[5].detail.unrelatedCommandQueued=$true}
        'clock-advanced' {$n[5].detail.simTicksAdvanced=1}
        'probe-after-release' {$n[5].detail.simWorkerStillHeld=$false}
    }
    Must-Reject {Assert-KmcRecoveryPersistenceEvidence $drainRequest $n} ('P07 drain accepted '+$bad)
}

Write-Host "PERSISTENCE OWNED FIXTURE PASS=$passes FAIL=0"
# Preserve only owned synthetic evidence in ignored obj; no external fixture touched.
