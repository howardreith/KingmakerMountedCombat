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
Write-Host "PERSISTENCE OWNED FIXTURE PASS=$passes FAIL=0"
# Preserve only owned synthetic evidence in ignored obj; no external fixture touched.
