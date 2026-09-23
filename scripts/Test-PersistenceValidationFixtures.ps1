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
# --- Post-disposal failed native load ---------------------------------------
# The only derivation allowed to edit a native member, because the loader parses
# the header before Game.DisposeState and reads area content only afterwards.
$failSource='owned-p06-failed-area'
$failRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$failSource)
$failSaves=Join-Path $failRoot 'Saved Games'
$failEvidence=Join-Path $script:ownedTestLab ('runtime-evidence/'+$failSource)
[void][IO.Directory]::CreateDirectory($failSaves);[void][IO.Directory]::CreateDirectory($failEvidence)
Write-KmcJsonAtomic (Join-Path $failRoot 'owner.json') ([ordered]@{runId=$failSource;scenario='persistence-p01-save';transactionToken=$token})
Write-KmcJsonAtomic (Join-Path $failEvidence 'runtime-result.json') ([ordered]@{runId=$failSource;
    scenario='persistence-p01-save';transactionToken=$token;status='PASS';modsRestored=$true;workingRestored=$true})
$failFixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64)};working=[pscustomobject]@{
    gameId='89c49a86-a171-4ea9-871a-f6b3b53d19b9';gameName='owned fixture';area=('a'*32)}}
$areaMember=('a'*32)+'.json'
$failPath=Join-Path $failSaves 'Manual_300_KMC_P01.zks'
$failData=[ordered]@{SchemaVersion=2;Mounted=$true;Combat=$null;CampaignId=$failFixture.working.gameId
    AreaId=$failFixture.working.area;ProfileId='medium-humanoid-mammoth-v1'
    Rider=@{Id='59252d29-8512-4ce1-b04d-3b7a6110e1ad'};Mount=@{Id='c0487f38-678d-4c82-b0b5-f46476e0cf17'};Slots=@()}
$stream=[IO.File]::Open($failPath,[IO.FileMode]::CreateNew)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try{
    foreach($name in @('header.json','kmc-mounted-state',$areaMember,'party.json','screenshot.png')){
        $value=switch($name){
            'header.json'{$header|ConvertTo-Json -Compress}
            'kmc-mounted-state'{$failData|ConvertTo-Json -Depth 12 -Compress}
            $areaMember{'{"$id":"1","Entities":[]}'}
            default{'owned test sentinel: '+$name}}
        $writer=[IO.StreamWriter]::new($zip.CreateEntry($name).Open(),[Text.UTF8Encoding]::new($false))
        try{$writer.Write($value)}finally{$writer.Dispose()}
    }
}finally{$zip.Dispose();$stream.Dispose()}
$failHash=Get-KmcSha256 $failPath
$failCopy=New-KmcPersistenceFailedAreaLoadCopy $failSource $failHash $failFixture
Check-Kmc ((Get-KmcSha256 $failPath)-ceq$failHash) 'Failed-load derivation changed its source'
Check-Kmc ($failCopy.corruptedMember-ceq$areaMember) 'Failed-load derivation targeted the wrong member'
Check-Kmc ($failCopy.descriptor.fileName-ceq'Manual_813_KMC_P06_AREA.zks') 'Failed-load leaf lacks its own identity'
Check-Kmc ($failCopy.descriptor.sha256-cne$failHash) 'Failed-load derivative is byte-identical to its source'
Check-Kmc ($failCopy.descriptor.gameId-ceq$failFixture.working.gameId-and$failCopy.descriptor.area-ceq$failFixture.working.area) 'Failed-load derivative lost its native identity'
$beforeMembers=Get-KmcValidationMemberHashes $failPath
$afterMembers=Get-KmcValidationMemberHashes $failCopy.path
Check-Kmc ($beforeMembers.Count-eq$afterMembers.Count) 'Failed-load derivation changed the native member set'
foreach($name in $beforeMembers.Keys){
    if($name-ceq$areaMember){Check-Kmc ($afterMembers[$name]-cne$beforeMembers[$name]) 'Area member was not actually corrupted'}
    else{Check-Kmc ($afterMembers[$name]-ceq$beforeMembers[$name]) ('Failed-load derivation changed native member '+$name)}
}
# Structurally valid ZIP, structurally invalid JSON: the loader has to reach
# deserialization rather than reject the archive itself.
$stream=[IO.File]::OpenRead($failCopy.path)
$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
try{
    $reader=[IO.StreamReader]::new($zip.GetEntry($areaMember).Open())
    try{$corruptText=$reader.ReadToEnd()}finally{$reader.Dispose()}
    $parsed=$true
    try{[void]($corruptText|ConvertFrom-Json)}catch{$parsed=$false}
    Check-Kmc (-not$parsed) 'Corrupted area member still parses as JSON'
    Check-Kmc ($null-ne$zip.GetEntry('header.json')-and$null-ne$zip.GetEntry('kmc-mounted-state')) 'Failed-load derivative lost header or metadata'
}finally{$zip.Dispose();$stream.Dispose()}
Reject-Kmc {New-KmcPersistenceFailedAreaLoadCopy $failSource ('c'*64) $failFixture} 'Failed-load accepted an incorrect source hash'
Reject-Kmc {New-KmcPersistenceFailedAreaLoadCopy '../human' $failHash $failFixture} 'Failed-load accepted a traversal source'
Reject-Kmc {New-KmcPersistenceFailedAreaLoadCopy $source $hash $fixture} 'Failed-load accepted a source with no native area member'
$failFixture.working.area=('f'*32)
Reject-Kmc {New-KmcPersistenceFailedAreaLoadCopy $failSource $failHash $failFixture} 'Failed-load accepted a foreign declared area'
$failFixture.working.area=('a'*32)
Check-Kmc ((Get-KmcSha256 $failPath)-ceq$failHash) 'Failed-load negatives changed the original'

$failRunId='owned-result-failed-area-load'
$failResultRoot=Join-Path $script:ownedTestLab ('runtime-staging/persistence-'+$failRunId)
[void][IO.Directory]::CreateDirectory((Join-Path $failResultRoot 'Saved Games'))
[IO.File]::Copy($failPath,(Join-Path $failResultRoot 'Saved Games/Manual_300_KMC_P01.zks'),$false)
[IO.File]::Copy($failCopy.path,(Join-Path $failResultRoot 'Saved Games/Manual_813_KMC_P06_AREA.zks'),$false)
[IO.File]::Copy((Join-Path (Split-Path -Parent $failCopy.path) 'owner.json'),(Join-Path $failResultRoot 'validation-copy.json'),$false)
$failRequest=[pscustomobject]@{runId=$failRunId;scenario='persistence-p06-load';persistenceCase='failed-area-load'
    commit=('d'*40);dllSha256=('e'*64)
    persistenceLoad=[pscustomobject]@{fileName='Manual_300_KMC_P01.zks';sha256=$failHash}
    persistenceAlternate=$failCopy.descriptor}
$failGame=[pscustomobject]@{processId=1}
function New-KmcFailRow { param([string]$Kind,[bool]$Worldless)
    $row=[pscustomobject]@{kind=$Kind;runId=$failRunId;scenario='persistence-p06-load';source=('d'*40);dll=('e'*64)
        processId=1;checkpoint='failed-area-load';relationship='Mounted'
        rider=$null;mount=$null;controls=$null
        persistence=[pscustomobject]@{semantics=2;presentation=1}
        detail=[pscustomobject]@{label='B';sha256=$failCopy.descriptor.sha256;corruptedMember=$areaMember
            nativeLoadFailures=0;nativeLoadFailure=$null;rejections=0;nativeWorldDisposals=0
            afterLoadCallback=$false;currentAreaNull=$false;loadedDataNull=$false;combatRestorationPending=$false;gameMode='Default'
            semantic=2;presentation=1;relationship='Mounted';unitCount=2;saveSuspended=$false;pendingWrites=$false
            stopAllRequired=$true;rules=1;rolls=1}}
    if(-not$Worldless){
        $row.rider=[pscustomobject]@{Id=$failData.Rider.Id;Standard=0;Move=0;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0}
        $row.mount=[pscustomobject]@{Id=$failData.Mount.Id;Standard=0;Move=0;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0}
        $row.controls=[pscustomobject]@{DuplicateFactCount=0}
    }
    return $row
}
$failRows=@(
    (New-KmcFailRow 'initial' $false),
    (New-KmcFailRow 'validation-native-load-requested' $false),
    (New-KmcFailRow 'failed-load-observed' $true),
    (New-KmcFailRow 'failed-load-recovery-requested' $true),
    (New-KmcFailRow 'validation-native-load-requested' $false),
    (New-KmcFailRow 'validation-valid-retry' $false),
    (New-KmcFailRow 'movement-dispatched' $false),(New-KmcFailRow 'movement-completed' $false),
    (New-KmcFailRow 'attack-dispatched' $false),(New-KmcFailRow 'attack-delivered' $false),
    (New-KmcFailRow 'usable-continuation-complete' $false)
)
foreach($index in 2,3){
    $failRows[$index].relationship='Unmounted'
    $d=$failRows[$index].detail
    $d.nativeLoadFailures=1;$d.nativeLoadFailure='JsonReaderException: unexpected end of input'
    $d.nativeWorldDisposals=1;$d.currentAreaNull=$true;$d.loadedDataNull=$true;$d.afterLoadCallback=$true;$d.gameMode='None'
    $d.relationship='Unmounted';$d.unitCount=0
}
$failRows[4].detail.label='A';$failRows[4].detail.sha256=$failHash
$failRows[5].detail.label='A';$failRows[5].detail.sha256=$failHash
$failRows[5].detail.nativeLoadFailures=1;$failRows[5].detail.afterLoadCallback=$true;$failRows[5].detail.gameMode='Default'
$failRows[5].detail.semantic=4;$failRows[5].detail.presentation=2
$failRows[5].detail.nativeWorldDisposals=2
Assert-KmcValidationPersistenceEvidence $failRequest $failRows $failGame;$script:passes++
foreach($bad in @('no-failure','admission-refusal','no-disposal','active-game-mode','world-survived','metadata-retained',
    'combat-fence','restored-actor','stale-pair','stale-unit','save-suspended','pending-writes','wrong-member',
    'empty-failure-text','retry-no-callback','retry-still-unmounted','retry-extra-failure','retry-wrong-semantics',
    'retry-no-metadata','stale-actor-in-worldless-row','missing-observation','missing-recovery','out-of-order','wrong-selection')){
    $negative=($failRows|ConvertTo-Json -Depth 16)|ConvertFrom-Json
    switch($bad){
        'no-failure' {$negative[2].detail.nativeLoadFailures=0}
        'admission-refusal' {$negative[2].detail.rejections=1}
        'no-disposal' {$negative[2].detail.nativeWorldDisposals=0}
        'active-game-mode' {$negative[2].detail.gameMode='Default'}
        'world-survived' {$negative[2].detail.currentAreaNull=$false}
        'metadata-retained' {$negative[2].detail.loadedDataNull=$false}
        'combat-fence' {$negative[2].detail.combatRestorationPending=$true}
        'restored-actor' {$negative[2].detail.semantic=3}
        'stale-pair' {$negative[2].detail.relationship='Mounted'}
        'stale-unit' {$negative[2].detail.unitCount=1}
        'save-suspended' {$negative[2].detail.saveSuspended=$true}
        'pending-writes' {$negative[2].detail.pendingWrites=$true}
        'wrong-member' {$negative[2].detail.corruptedMember='header.json'}
        'empty-failure-text' {$negative[2].detail.nativeLoadFailure=''}
        'retry-no-callback' {$negative[5].detail.afterLoadCallback=$false}
        'retry-still-unmounted' {$negative[5].detail.relationship='Unmounted'}
        'retry-extra-failure' {$negative[5].detail.nativeLoadFailures=2}
        'retry-wrong-semantics' {$negative[5].detail.semantic=5}
        'retry-no-metadata' {$negative[5].detail.loadedDataNull=$true}
        'stale-actor-in-worldless-row' {$negative[2].rider=[pscustomobject]@{Id=$failData.Rider.Id}}
        'missing-observation' {$negative=@($negative[0],$negative[1],$negative[3],$negative[4],$negative[5],$negative[6],$negative[7],$negative[8],$negative[9],$negative[10])}
        'missing-recovery' {$negative=@($negative[0],$negative[1],$negative[2],$negative[4],$negative[5],$negative[6],$negative[7],$negative[8],$negative[9],$negative[10])}
        'out-of-order' {$negative=@($negative[0],$negative[2],$negative[1],$negative[3],$negative[4],$negative[5],$negative[6],$negative[7],$negative[8],$negative[9],$negative[10])}
        'wrong-selection' {$negative[1].detail.sha256=$failHash}
    }
    Reject-Kmc {Assert-KmcValidationPersistenceEvidence $failRequest $negative $failGame} ('Failed load accepted '+$bad)
}

Write-Output ("P06 FIXTURE GUARDS PASS="+$passes+" FAIL=0")
