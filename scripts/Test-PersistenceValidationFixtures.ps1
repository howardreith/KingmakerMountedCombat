[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceValidationFixtures.ps1')
$script:ownedTestLab=Join-Path (Get-KmcRepositoryRoot) ('obj/persistence-validation-tests/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { return $script:ownedTestLab }
$source='owned-p06-source';$token='a'*64
$root=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$source)
$saveRoot=Join-Path $root 'Saved Games';$evidence=Join-Path $script:ownedTestLab ('runtime-evidence/'+$source)
[void][IO.Directory]::CreateDirectory($saveRoot);[void][IO.Directory]::CreateDirectory($evidence)
Write-KmcJsonAtomic (Join-Path $root 'owner.json') ([ordered]@{runId=$source;scenario='persistence-p01-save';transactionToken=$token})
$result=[ordered]@{runId=$source;scenario='persistence-p01-save';transactionToken=$token;status='PASS';modsRestored=$true;workingRestored=$true}
Write-KmcJsonAtomic (Join-Path $evidence 'runtime-result.json') $result
$fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64)};working=[pscustomobject]@{
    gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';gameName='owned fixture';area=('a'*32)}}
$path=Join-Path $saveRoot 'Manual_300_KMC_P01.zks'
$header=[ordered]@{Name='KMC_P01';Type='Manual';CompatibilityVersion=1;GameId=$fixture.working.gameId;GameName=$fixture.working.gameName;Area=$fixture.working.area}
$data=[ordered]@{SchemaVersion=2;Mounted=$true;Combat=$null;CampaignId=$fixture.working.gameId;AreaId=$fixture.working.area;
    ProfileId='medium-humanoid-mammoth-v1';Rider=@{Id='59252d29-8512-4ce1-b04d-3b7a6110e1ad'};
    Mount=@{Id='c0487f38-678d-4c82-b0b5-f46476e0cf17'};Slots=@()}
Add-Type -AssemblyName System.IO.Compression
$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    foreach($name in @('header.json','kmc-mounted-state','party.json','screenshot.png')){
        $value=switch($name){'header.json'{$header|ConvertTo-Json -Compress};'kmc-mounted-state'{$data|ConvertTo-Json -Depth 12 -Compress};default{'owned test sentinel: '+$name}}
        $writer=[IO.StreamWriter]::new($zip.CreateEntry($name).Open(),[Text.UTF8Encoding]::new($false))
        try{$writer.Write($value)}finally{$writer.Dispose()}
    }
}finally{$zip.Dispose();$stream.Dispose()}
$hash=Get-KmcSha256 $path
$script:passes=0
function Check-Kmc([bool]$Value,[string]$Message){if(-not$Value){throw $Message};$script:passes++}
function Reject-Kmc([scriptblock]$Action,[string]$Message){$rejected=$false;try{& $Action|Out-Null}catch{$rejected=$true};Check-Kmc $rejected $Message}
foreach($case in @('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy')){
    $copy=New-KmcPersistenceValidationCopy $source $hash $fixture -Case $case
    Check-Kmc ((Get-KmcSha256 $path)-ceq$hash) 'Fixture derivative changed immutable input'
    $receipt=Read-KmcJson (Join-Path (Split-Path -Parent $copy.path) 'owner.json')
    Check-Kmc ($receipt.case-ceq$case-and$receipt.sourceSha256-ceq$hash-and$receipt.nativeMembersVerified-eq3) 'Derived copy lacks ownership/member preservation'
    $stream=[IO.File]::OpenRead($copy.path)
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
    try{
        $entry=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        Check-Kmc ($entry.Count-eq$(if($case-ceq'legacy'){0}else{1})) 'Wrong metadata member count'
        if($case-ceq'legacy'){continue}
        $reader=[IO.StreamReader]::new($entry[0].Open())
        try{$json=$reader.ReadToEnd()}finally{$reader.Dispose()}
        if($case-ceq'malformed'){Check-Kmc ($json-ceq'{"SchemaVersion":2,') 'Malformed fixture not bounded';continue}
        $observed=$json|ConvertFrom-Json
        $valid=switch($case){
            'schema1'{$observed.SchemaVersion-eq1-and@($observed.PSObject.Properties.Name)-cnotcontains'Combat'}
            'future'{$observed.SchemaVersion-eq99}
            'profile'{$observed.ProfileId-ceq'unsupported-profile'}
            'campaign'{$observed.CampaignId-cne$fixture.working.gameId}
            'missing-rider'{$observed.Rider.Id-ceq'c63b5e10-4db1-47d5-ae61-5c0788137a5d'}
            'missing-mount'{$observed.Mount.Id-ceq'c63b5e10-4db1-47d5-ae61-5c0788137a5d'}
            'mismatched-profile'{$observed.ProfileId-ceq'medium-humanoid-horse-v1'}
            'policy'{$observed.SchemaVersion-eq2-and$observed.ProfileId-ceq$data.ProfileId}
        }
        Check-Kmc $valid 'Variant did not perform its exact bounded mutation'
    }finally{$zip.Dispose();$stream.Dispose()}
}
Reject-Kmc {New-KmcPersistenceValidationCopy '../human' $hash $fixture -Case future} 'Traversal source admitted'
Reject-Kmc {New-KmcPersistenceValidationCopy $source ('c'*64) $fixture -Case legacy} 'Incorrect source hash admitted'
Reject-Kmc {New-KmcPersistenceValidationCopy $source $hash $fixture -Case arbitrary} 'Unbounded variant admitted'
$result.modsRestored=$false;Write-KmcJsonAtomic (Join-Path $evidence 'runtime-result.json') $result
Reject-Kmc {New-KmcPersistenceValidationCopy $source $hash $fixture -Case future} 'Unrestored source admitted'
$result.modsRestored=$true;Write-KmcJsonAtomic (Join-Path $evidence 'runtime-result.json') $result
$fixture.working.gameId='c63b5e10-4db1-47d5-ae61-5c0788137a5d'
Reject-Kmc {New-KmcPersistenceValidationCopy $source $hash $fixture -Case future} 'Foreign native campaign admitted'
Check-Kmc ((Get-KmcSha256 $path)-ceq$hash) 'Negative test changed original'

$fixture.working.gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9'
Reject-Kmc {New-KmcPersistenceValidationCopy $source $hash $fixture -Case combat-missing} 'Combat mutation accepted outside-combat source'
$combatSource='owned-p06-combat'
$combatRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$combatSource)
$combatSaves=Join-Path $combatRoot 'Saved Games'
$combatEvidence=Join-Path $script:ownedTestLab ('runtime-evidence/'+$combatSource)
[void][IO.Directory]::CreateDirectory($combatSaves);[void][IO.Directory]::CreateDirectory($combatEvidence)
$combatPath=Join-Path $combatSaves 'Manual_300_KMC_P01.zks'
[IO.File]::Copy($path,$combatPath,$false)
$otherId='639c449b-6de5-42c9-aad2-92970e9afc92'
$data.Rider.Standard=0;$data.Rider.Move=0;$data.Mount.Standard=0;$data.Mount.Move=1
$data.Combat=[ordered]@{TurnBased=$true;Round=1;Current=@{ActorId=$data.Rider.Id};
    Paired=@{Activation=@{Sequence=1}};Actors=@(
        @{Native=$data.Rider;AiActions=@()},@{Native=$data.Mount;AiActions=@()},@{Native=@{Id=$otherId};AiActions=@()});
    Roster=@(@{ActorId=$otherId});Engagements=@(@{From=$data.Rider.Id;To=$otherId})}
$stream=[IO.File]::Open($combatPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Update,$false)
try{
    $zip.GetEntry('kmc-mounted-state').Delete()
    $writer=[IO.StreamWriter]::new($zip.CreateEntry('kmc-mounted-state').Open(),[Text.UTF8Encoding]::new($false))
    try{$writer.Write(($data|ConvertTo-Json -Depth 12 -Compress))}finally{$writer.Dispose()}
}finally{$zip.Dispose();$stream.Dispose()}
$combatHash=Get-KmcSha256 $combatPath
$owner=[ordered]@{runId=$combatSource;scenario='persistence-p02-save';persistenceCase='partial-movement';transactionToken=$token}
Write-KmcJsonAtomic (Join-Path $combatRoot 'owner.json') $owner
Write-KmcJsonAtomic (Join-Path $combatEvidence 'runtime-result.json') ([ordered]@{runId=$combatSource;scenario='persistence-p02-save';
    transactionToken=$token;status='PASS';modsRestored=$true;workingRestored=$true})
foreach($case in @('combat-missing','combat-ai')){
    $copy=New-KmcPersistenceValidationCopy $combatSource $combatHash $fixture -Case $case
    Check-Kmc ((Get-KmcSha256 $combatPath)-ceq$combatHash) 'Combat derivative changed source'
    $stream=[IO.File]::OpenRead($copy.path)
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
    try{
        $reader=[IO.StreamReader]::new($zip.GetEntry('kmc-mounted-state').Open())
        try{$observed=$reader.ReadToEnd()|ConvertFrom-Json}finally{$reader.Dispose()}
        if($case-ceq'combat-missing'){
            Check-Kmc ($observed.Combat.Actors[2].Native.Id-ceq'c63b5e10-4db1-47d5-ae61-5c0788137a5d'-and
                $observed.Combat.Roster[0].ActorId-ceq$observed.Combat.Actors[2].Native.Id-and
                $observed.Combat.Engagements[0].To-ceq$observed.Combat.Actors[2].Native.Id) 'Missing combat actor mutation lost reciprocal semantic references'
        }else{
            Check-Kmc ($observed.Combat.Actors[1].AiActions.Count-eq1-and
                $observed.Combat.Actors[1].AiActions[0].BlueprintId-cmatch'^[0-9a-f]{32}$'-and
                $observed.Mount.Move-eq1) 'AI reference fixture is not bounded or changed legitimate debt'
        }
    }finally{$zip.Dispose();$stream.Dispose()}
    $runId='owned-result-'+$case
    $resultRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$runId)
    [void][IO.Directory]::CreateDirectory((Join-Path $resultRoot 'Saved Games'))
    [IO.File]::Copy($combatPath,(Join-Path $resultRoot 'Saved Games/Manual_300_KMC_P01.zks'),$false)
    [IO.File]::Copy($copy.path,(Join-Path $resultRoot 'Saved Games/Manual_812_KMC_P06.zks'),$false)
    [IO.File]::Copy((Join-Path (Split-Path -Parent $copy.path) 'owner.json'),(Join-Path $resultRoot 'validation-copy.json'),$false)
    $request=[pscustomobject]@{runId=$runId;scenario='persistence-p06-load';persistenceCase=$case;commit=('d'*40);dllSha256=('e'*64);
        persistenceLoad=[pscustomobject]@{fileName='Manual_300_KMC_P01.zks';sha256=$combatHash};persistenceAlternate=$copy.descriptor}
    $game=[pscustomobject]@{processId=1}
    $rows=@(foreach($kind in @('initial','validation-native-load-requested','validation-combat-blocked','validation-canceled-replacement',
        'validation-native-load-requested','validation-valid-retry','validation-combat-retry-initial','movement-dispatched','movement-completed',
        'attack-dispatched','attack-delivered','next-paired-activation','next-paired-activation','usable-continuation-complete')){
        [pscustomobject]@{kind=$kind;runId=$runId;scenario=$request.scenario;source=$request.commit;dll=$request.dllSha256;processId=1;checkpoint=$case;
            rider=[pscustomobject]@{Id=$data.Rider.Id;Standard=0;Move=0;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0};
            mount=[pscustomobject]@{Id=$data.Mount.Id;Standard=0;Move=0;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0};
            relationship='Mounted';controls=[pscustomobject]@{DuplicateFactCount=0};persistence=[pscustomobject]@{semantics=3};
            detail=[pscustomobject]@{label='B';sha256=$copy.descriptor.sha256;initialActorCount=3;semantic=0;presentation=1;blocked=$false;
                nativeWorldDisposals=1;canceledCallback=$false;nativeAdmissionProbes=0;feedback='Mounted combat restoration is blocked';
                duplicateNotifications=0;nativeCallback=$true;sequence=1;rules=1;rolls=1}}
    })
    $sem=if($case-ceq'combat-missing'){5}else{6}
    $rows[2].detail.semantic=$sem;$rows[3].detail.semantic=$sem
    $rows[2].detail.blocked=$true;$rows[3].detail.blocked=$true
    $rows[2].relationship='Unmounted';$rows[3].relationship='Unmounted'
    $rows[2].detail.nativeAdmissionProbes=1;$rows[3].detail.nativeAdmissionProbes=2
    $rows[4].detail.label='A';$rows[4].detail.sha256=$combatHash
    $rows[5].detail.semantic=$sem+3;$rows[5].detail.presentation=2
    $rows[5].detail.nativeWorldDisposals=2;$rows[5].detail.duplicateNotifications=4
    $rows[11].detail.sequence=2;$rows[12].detail.sequence=3
    Assert-KmcValidationPersistenceEvidence $request $rows $game;$script:passes++
    foreach($bad in @('lost-fence','fresh-debt','false-callback','duplicate-refresh')){
        $negative=($rows|ConvertTo-Json -Depth 16)|ConvertFrom-Json
        switch($bad){
            'lost-fence' {$negative[3].detail.blocked=$false}
            'fresh-debt' {$negative[3].mount.Move=1}
            'false-callback' {$negative[3].detail.canceledCallback=$true}
            'duplicate-refresh' {$negative[12].detail.sequence=2}
        }
        Reject-Kmc {Assert-KmcValidationPersistenceEvidence $request $negative $game} ('Combat result accepted '+$bad)
    }
}
$owner.persistenceCase='rider-spent';Write-KmcJsonAtomic (Join-Path $combatRoot 'owner.json') $owner
Reject-Kmc {New-KmcPersistenceValidationCopy $combatSource $combatHash $fixture -Case combat-missing} 'Foreign combat boundary source admitted'
Write-Output ("P06 FIXTURE GUARDS PASS="+$passes+" FAIL=0")
