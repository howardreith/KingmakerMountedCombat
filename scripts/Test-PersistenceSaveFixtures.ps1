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
Write-Host "PERSISTENCE OWNED FIXTURE PASS=$passes FAIL=0"
# Preserve only owned synthetic evidence in ignored obj; no external fixture touched.
