Set-StrictMode -Version Latest

# Offline, owned copies only. Native entity/header/screenshot bytes are never
# edited. This is fixture derivation, not a production save or migration writer.
function Get-KmcPersistenceValidationSource {
    param([string]$SourceRunId,[string]$ExpectedSha256,$Fixture,[string]$Case)
    if($SourceRunId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'-or$SourceRunId-in@('.','..')){
        throw 'Invalid validation source run.'
    }
    $root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'validation source'
    Assert-KmcDirectoryTreeCloneable $root 'validation source'
    $owner=Read-KmcJson (Join-Path $root 'owner.json')
    $category=$null
    if($Case-cin @('combat-missing','combat-ai')){
        if($owner.scenario-cne'persistence-p02-save'-or$owner.persistenceCase-cne'partial-movement'){
            throw 'Combat validation requires an exact completed partial-movement TB source.'
        }
        return Get-KmcPersistenceSource $SourceRunId $ExpectedSha256 $Fixture
    }
    if($owner.scenario-ceq'persistence-p05-save'){
        if($owner.persistenceCase-cnotin@('manual','queued')){throw 'Validation requires a completed outside-combat manual source.'}
        $category=[string]$owner.persistenceCase
    }elseif($owner.scenario-cne'persistence-p01-save'){throw 'Validation source is not an outside-combat manual fixture.'}
    if($null-eq$category){Get-KmcPersistenceSource $SourceRunId $ExpectedSha256 $Fixture}
    else{Get-KmcPersistenceSource $SourceRunId $ExpectedSha256 $Fixture -NativeCase $category}
}

function Get-KmcValidationMemberHashes {
    param([string]$Path)
    Add-Type -AssemblyName System.IO.Compression
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $zip=$null
    try {
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
        if($zip.Entries.Count-lt2-or$zip.Entries.Count-gt1024){throw 'Validation archive member count is not bounded.'}
        $hashes=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        [long]$total=0
        foreach($entry in $zip.Entries){
            $total+=$entry.Length
            if($entry.Length-gt256MB-or$total-gt512MB-or$hashes.ContainsKey($entry.FullName)){
                throw 'Validation archive members are oversized or ambiguous.'
            }
            $inner=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
            try{$hashes.Add($entry.FullName,([BitConverter]::ToString($sha.ComputeHash($inner))).Replace('-','').ToLowerInvariant())}
            finally{$sha.Dispose();$inner.Dispose()}
        }
        return ,$hashes
    }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
}

function New-KmcPersistenceValidationCopy {
    param([string]$SourceRunId,[string]$ExpectedSha256,$Fixture,
        [ValidateSet('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai')][string]$Case)
    Assert-KmcNoGameProcesses
    $source=Get-KmcPersistenceValidationSource $SourceRunId $ExpectedSha256 $Fixture -Case $Case
    $before=Get-KmcValidationMemberHashes $source.path
    $parent=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk5-validation-fixtures'
    [void][IO.Directory]::CreateDirectory($parent)
    Assert-KmcDirectoryTreeCloneable $parent 'owned validation fixtures'
    $owned=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'new validation fixture'
    if(Test-Path -LiteralPath $owned){throw 'Validation fixture ownership is ambiguous.'}
    [void][IO.Directory]::CreateDirectory($owned)
    $path=Join-Path $owned 'Manual_812_KMC_P06.zks'
    $input=[IO.File]::Open($source.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $output=$null
    try{
        $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $input.CopyTo($output)
    }finally{if($output){$output.Dispose()};$input.Dispose()}
    if((Get-KmcSha256 $path)-cne$ExpectedSha256){throw 'Validation source changed while copying.'}
    Assert-KmcNotReparsePoint $path 'validation copy'
    Assert-KmcNotHardLink $path 'validation copy'
    $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $zip=$null
    try{
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Update,$false)
        $entries=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
        if($entries.Count-ne1-or$entries[0].Length-le0-or$entries[0].Length-gt32KB){throw 'Source mounted metadata is not unique and bounded.'}
        $reader=[IO.StreamReader]::new($entries[0].Open(),[Text.UTF8Encoding]::new($false,$true))
        try{$json=$reader.ReadToEnd()}finally{$reader.Dispose()}
        Assert-KmcJsonObjectMembersUnique -Json $json -Description 'validation source metadata'
        $data=$json|ConvertFrom-Json
        $combat=$Case-cin @('combat-missing','combat-ai')
        if($data.SchemaVersion-ne2-or$data.Mounted-ne$true-or
            ($combat-and($null-eq$data.Combat-or$data.Combat.TurnBased-ne$true))-or
            (-not$combat-and$null-ne$data.Combat)-or
            $data.CampaignId-cne$Fixture.working.gameId-or$data.AreaId-cne$Fixture.working.area){
            throw 'The current mounted source does not match its exact validation mode.'
        }
        if($combat){Assert-KmcP02Snapshot $data 'partial-movement'}
        switch -CaseSensitive ($Case){
            'legacy' { $json=$null }
            'schema1' { $data.SchemaVersion=1;$data.PSObject.Properties.Remove('Combat');$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'future' { $data.SchemaVersion=99;$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'malformed' { $json='{"SchemaVersion":2,' }
            'profile' { $data.ProfileId='unsupported-profile';$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'campaign' { $data.CampaignId='c63b5e10-4db1-47d5-ae61-5c0788137a5d';$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'missing-rider' { $data.Rider.Id='c63b5e10-4db1-47d5-ae61-5c0788137a5d';$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'missing-mount' { $data.Mount.Id='c63b5e10-4db1-47d5-ae61-5c0788137a5d';$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'mismatched-profile' { $data.ProfileId=if($data.ProfileId-ceq'medium-humanoid-mammoth-v1'){'medium-humanoid-horse-v1'}else{'medium-humanoid-mammoth-v1'};$json=$data|ConvertTo-Json -Depth 12 -Compress }
            'policy' { } # Exact same bytes, interpreted under an incompatible active policy.
            'combat-missing' {
                $others=@($data.Combat.Actors|Where-Object {$_.Native.Id-cnotin@($data.Rider.Id,$data.Mount.Id)})
                if($others.Count-lt1){throw 'Combat fixture has no unrelated actor to invalidate'}
                $json=$json.Replace([string]$others[0].Native.Id,'c63b5e10-4db1-47d5-ae61-5c0788137a5d')
            }
            'combat-ai' {
                $actor=@($data.Combat.Actors|Where-Object {$_.Native.Id-ceq$data.Mount.Id})
                if($actor.Count-ne1){throw 'Combat fixture mount is ambiguous'}
                $actor[0].AiActions=@([ordered]@{BlueprintId='c63b5e104db147d5ae615c0788137a5d';Cooldown=1;Count=1})
                $json=$data|ConvertTo-Json -Depth 12 -Compress
            }
            default { throw 'Unknown bounded validation variant.' }
        }
        if($Case-cne'policy'){
            $entries[0].Delete()
            if($null-ne$json){
                $bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($json)
                if($bytes.Length-le0-or$bytes.Length-gt32KB){throw 'Derived validation metadata exceeded its bound.'}
                $entry=$zip.CreateEntry('kmc-mounted-state')
                $inner=$entry.Open()
                try{$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}
            }
        }
    }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
    $after=Get-KmcValidationMemberHashes $path
    foreach($name in $before.Keys){
        if($name-ceq'kmc-mounted-state'){continue}
        if(-not$after.ContainsKey($name)-or$after[$name]-cne$before[$name]){throw 'A native member changed during fixture derivation.'}
    }
    $expectedCount=if($Case-ceq'legacy'){$before.Count-1}else{$before.Count}
    if($after.Count-ne$expectedCount-or(Get-KmcSha256 $source.path)-cne$ExpectedSha256){throw 'Fixture derivation changed its source or member set.'}
    $file=Get-Item -LiteralPath $path
    $descriptor=[ordered]@{internalName=$source.descriptor.internalName;fileName=$file.Name;sha256=(Get-KmcSha256 $path);
        length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;
        gameId=$source.descriptor.gameId;gameName=$source.descriptor.gameName;area=$source.descriptor.area}
    Write-KmcJsonAtomic (Join-Path $owned 'owner.json') ([ordered]@{
        schemaVersion=1;purpose='P06 offline metadata-only validation copy';case=$Case;sourceRun=$SourceRunId;
        sourceSha256=$ExpectedSha256;descriptor=$descriptor;nativeMembersVerified=($before.Count-1);members=$after
    })
    return [pscustomobject]@{path=$path;descriptor=$descriptor}
}

# Offline, owned copy that deliberately corrupts exactly ONE native area member.
# This is the only derivation permitted to edit a native member, and it exists
# because SaveManager.LoadRoutine 0600BF00 parses the header at IL011E/0147 but
# destroys the loaded world at Game.DisposeState IL021F before ThreadedGameLoader
# reads area content at IL0262. A bad header therefore fails BEFORE disposal and
# only repeats existing admission coverage; only a bad area member fails after
# it. The metadata-only helper above keeps its stronger guarantee unchanged: this
# function does not relax it and is never used by those cases.
function New-KmcPersistenceFailedAreaLoadCopy {
    param([string]$SourceRunId,[string]$ExpectedSha256,$Fixture)
    Assert-KmcNoGameProcesses
    $source=Get-KmcPersistenceValidationSource $SourceRunId $ExpectedSha256 $Fixture -Case 'failed-area-load'
    $before=Get-KmcValidationMemberHashes $source.path
    # The member consumed on load is the archive's own loaded area state.
    $member=[string]$source.descriptor.area+'.json'
    if($source.descriptor.area-cne$Fixture.working.area){throw 'Failed-load source area differs from the fixture contract.'}
    if(-not$before.ContainsKey($member)){throw 'Failed-load source has no native area member to corrupt.'}
    if(-not$before.ContainsKey('header.json')-or-not$before.ContainsKey('kmc-mounted-state')){
        throw 'Failed-load source lacks its native header or mounted metadata.'
    }
    $parent=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk5-validation-fixtures'
    [void][IO.Directory]::CreateDirectory($parent)
    Assert-KmcDirectoryTreeCloneable $parent 'owned validation fixtures'
    $owned=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'new validation fixture'
    if(Test-Path -LiteralPath $owned){throw 'Validation fixture ownership is ambiguous.'}
    [void][IO.Directory]::CreateDirectory($owned)
    $path=Join-Path $owned 'Manual_813_KMC_P06_AREA.zks'
    $input=[IO.File]::Open($source.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $output=$null
    try{
        $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $input.CopyTo($output)
    }finally{if($output){$output.Dispose()};$input.Dispose()}
    if((Get-KmcSha256 $path)-cne$ExpectedSha256){throw 'Failed-load source changed while copying.'}
    Assert-KmcNotReparsePoint $path 'failed-load copy'
    Assert-KmcNotHardLink $path 'failed-load copy'
    # Structurally valid ZIP, structurally invalid JSON: the loader must reach
    # deserialization and fail there, not be rejected as an unreadable archive.
    $corrupt='{"$id":"1","$type":'
    $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $zip=$null
    try{
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Update,$false)
        $entries=@($zip.Entries|Where-Object FullName -CEQ $member)
        if($entries.Count-ne1-or$entries[0].Length-le0){throw 'Native area member is not unique and non-empty.'}
        $entries[0].Delete()
        $bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($corrupt)
        $entry=$zip.CreateEntry($member)
        $inner=$entry.Open()
        try{$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}
    }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
    $after=Get-KmcValidationMemberHashes $path
    foreach($name in $before.Keys){
        if($name-ceq$member){continue}
        if(-not$after.ContainsKey($name)-or$after[$name]-cne$before[$name]){
            throw 'A native member other than the declared area member changed during failed-load derivation.'
        }
    }
    if($after.Count-ne$before.Count){throw 'Failed-load derivation changed the native member set.'}
    if($after[$member]-ceq$before[$member]){throw 'Failed-load derivation did not actually corrupt its area member.'}
    if((Get-KmcSha256 $source.path)-cne$ExpectedSha256){throw 'Failed-load derivation changed its source archive.'}
    $file=Get-Item -LiteralPath $path
    $descriptor=[ordered]@{internalName=$source.descriptor.internalName;fileName=$file.Name;sha256=(Get-KmcSha256 $path);
        length=[long]$file.Length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;
        gameId=$source.descriptor.gameId;gameName=$source.descriptor.gameName;area=$source.descriptor.area}
    Write-KmcJsonAtomic (Join-Path $owned 'owner.json') ([ordered]@{
        schemaVersion=1;purpose='P06 offline single-native-area-member corruption for post-disposal load failure'
        case='failed-area-load';sourceRun=$SourceRunId;sourceSha256=$ExpectedSha256;descriptor=$descriptor
        corruptedMember=$member;corruptedBeforeSha256=$before[$member];corruptedAfterSha256=$after[$member]
        nativeMembersVerified=($before.Count-1);members=$after
    })
    return [pscustomobject]@{path=$path;descriptor=$descriptor;corruptedMember=$member}
}

# A native load that fails after the previous world was destroyed. The engine has
# no rollback there: SaveManager.LoadRoutine 0600BF00 disposes the world at
# IL021F and only reads area content at IL0262, and its single Catch rethrows.
# So the requirements are that the corrupt archive passed NORMAL admission, that
# it then failed for real, that nothing was reported as loaded, that no
# actionable remnant survived, and that a valid load recovers in the same process.
function Assert-KmcFailedAreaLoadEvidence {
    param($Request,$Rows,$Initial)
    $loads=@($Rows|Where-Object kind -CEQ 'validation-native-load-requested')
    $observed=@($Rows|Where-Object kind -CEQ 'failed-load-observed')
    $recovery=@($Rows|Where-Object kind -CEQ 'failed-load-recovery-requested')
    $retry=@($Rows|Where-Object kind -CEQ 'validation-valid-retry')
    if($loads.Count-ne2-or$observed.Count-ne1-or$recovery.Count-ne1-or$retry.Count-ne1-or
        $loads[0].detail.label-cne'B'-or$loads[1].detail.label-cne'A'-or
        $loads[0].detail.sha256-cne$Request.persistenceAlternate.sha256-or
        $loads[1].detail.sha256-cne$Request.persistenceLoad.sha256){
        throw 'P06 failed load lacks its corrupt selection, failure observation and valid retry.'
    }
    $kinds=@($Rows|ForEach-Object{$_.kind})
    $order=@('initial','validation-native-load-requested','failed-load-observed','failed-load-recovery-requested','validation-valid-retry')
    $previous=-1
    foreach($kind in $order){
        $at=[Array]::IndexOf($kinds,$kind)
        if($at-le$previous){throw "P06 failed-load evidence is out of order at $kind."}
        $previous=$at
    }
    if([Array]::LastIndexOf($kinds,'validation-native-load-requested')-le[Array]::IndexOf($kinds,'failed-load-recovery-requested')){
        throw 'P06 recovery load was not requested after the failure was observed.'
    }
    $d=$observed[0].detail
    # The corrupt member is the loaded area itself, which is the only member the
    # loader consumes after disposal.
    if($d.corruptedMember-cne([string]$Request.persistenceAlternate.area+'.json')){
        throw 'P06 failed load did not target the archive''s own loaded area member.'
    }
    if($d.nativeLoadFailures-ne1-or[string]::IsNullOrEmpty([string]$d.nativeLoadFailure)){
        throw 'P06 failed load did not record exactly one real native failure.'
    }
    if($d.rejections-ne0){throw 'P06 corrupt archive was refused at admission instead of failing later.'}
    if($d.nativeWorldDisposals-ne1){throw 'P06 failed load did not actually dispose the previous world first.'}
    if($d.afterLoadCallback-ne$false){throw 'P06 failed load reported a successful load callback.'}
    if($d.currentAreaNull-ne$true){throw 'P06 failed load left a completed world behind.'}
    if($d.loadedDataNull-ne$true-or$d.combatRestorationPending-ne$false){
        throw 'P06 failed load retained selected metadata or a combat fence.'
    }
    if($d.semantic-ne$Initial[0].persistence.semantics-or$d.presentation-ne$Initial[0].persistence.presentation){
        throw 'P06 failed load restored an actor or presentation.'
    }
    if($d.relationship-cne'Unmounted'){throw 'P06 failed load left an actionable partial pair.'}
    if($d.unitCount-gt0){throw 'P06 failed load left a stale live actor.'}
    if($d.saveSuspended-ne$false-or$d.pendingWrites-ne$false){
        throw 'P06 failed load left an owned save or serialization scope held.'
    }
    $r=$retry[0].detail
    if($r.nativeLoadFailures-ne1){throw 'P06 valid retry produced a further native load failure.'}
    if($r.afterLoadCallback-ne$true-or$r.relationship-cne'Mounted'-or$r.currentAreaNull-ne$false){
        throw 'P06 valid retry did not actually complete a usable world.'
    }
    if($r.semantic-ne($Initial[0].persistence.semantics+2)-or$r.presentation-ne($Initial[0].persistence.presentation+1)){
        throw 'P06 valid retry did not restore exactly one pair once.'
    }
    if($r.loadedDataNull-ne$false){throw 'P06 valid retry restored no mounted metadata.'}
    # Recorded, not required to be either value: the engine's own reset need is a
    # measurement. It is reported so a restart-only recovery cannot be described
    # as an in-session one.
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId)
    $receipt=Read-KmcJson (Join-Path $root 'validation-copy.json')
    if($receipt.corruptedMember-cne$d.corruptedMember-or
        $receipt.corruptedBeforeSha256-ceq$receipt.corruptedAfterSha256-or
        [string]::IsNullOrEmpty([string]$receipt.corruptedBeforeSha256)){
        throw 'P06 failed load lacks its single-member corruption provenance.'
    }
}

function Assert-KmcValidationPersistenceEvidence {
    param($Request,$Rows,$GameResult)
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P06 lacks its original cold native world.'}
    # A load that fails after Game.DisposeState describes a world the engine
    # already destroyed, so those two rows must carry NO actor at all. That is a
    # stricter claim than the identity check, not an exemption from it; every
    # other row still has to match the original pair exactly.
    $worldless=@('failed-load-observed','failed-load-recovery-requested')
    foreach($row in $Rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or
            $row.checkpoint-cne$Request.persistenceCase){
            throw 'P06 native source or run identity differs.'
        }
        if($row.kind-cin$worldless){
            if($Request.persistenceCase-cne'failed-area-load'){throw 'P06 recorded a worldless observation outside the failed-load case.'}
            if($null-ne$row.rider-or$null-ne$row.mount){throw 'P06 failed load kept a stale actor in its own observation.'}
            continue
        }
        if($row.rider.Id-cne$initial[0].rider.Id-or$row.mount.Id-cne$initial[0].mount.Id-or
            $row.controls.DuplicateFactCount-ne0){
            throw 'P06 native actor or owned-control identity differs.'
        }
    }
    $refused=$Request.persistenceCase-cin @('future','malformed','profile','campaign','policy')
    $combat=$Request.persistenceCase-cin @('combat-missing','combat-ai')
    if($Request.persistenceCase-ceq'failed-area-load'){
        Assert-KmcFailedAreaLoadEvidence $Request $Rows $initial
    }elseif($combat){
        $loads=@($Rows|Where-Object kind -CEQ 'validation-native-load-requested')
        $blocked=@($Rows|Where-Object kind -CEQ 'validation-combat-blocked')
        $canceled=@($Rows|Where-Object kind -CEQ 'validation-canceled-replacement')
        $retry=@($Rows|Where-Object kind -CEQ 'validation-valid-retry')
        $resumed=@($Rows|Where-Object kind -CEQ 'validation-combat-retry-initial')
        $next=@($Rows|Where-Object kind -CEQ 'next-paired-activation')
        if($loads.Count-ne2-or$blocked.Count-ne1-or$canceled.Count-ne1-or$retry.Count-ne1-or$resumed.Count-ne1-or$next.Count-ne2-or
            $loads[0].detail.label-cne'B'-or$loads[1].detail.label-cne'A'-or
            $loads[0].detail.sha256-cne$Request.persistenceAlternate.sha256-or$loads[1].detail.sha256-cne$Request.persistenceLoad.sha256){
            throw 'P06 combat lacks its native damaged load, canceled replacement and valid retry.'
        }
        $n=[int]$blocked[0].detail.initialActorCount
        $sem=2*$n-$(if($Request.persistenceCase-ceq'combat-missing'){1}else{0})
        if($n-lt2-or$n-gt64-or$initial[0].persistence.semantics-ne$n-or
            $blocked[0].detail.semantic-ne$sem-or$canceled[0].detail.semantic-ne$sem-or
            $blocked[0].detail.blocked-ne$true-or$canceled[0].detail.blocked-ne$true-or
            $blocked[0].relationship-cne'Unmounted'-or$canceled[0].relationship-cne'Unmounted'-or
            $blocked[0].detail.nativeWorldDisposals-ne1-or$canceled[0].detail.nativeWorldDisposals-ne1-or
            $canceled[0].detail.canceledCallback-ne$false-or
            $blocked[0].detail.nativeAdmissionProbes-ne1-or$canceled[0].detail.nativeAdmissionProbes-ne2-or
            $blocked[0].detail.feedback-cnotmatch'restoration is blocked'-or$canceled[0].detail.feedback-cnotmatch'restoration is blocked'-or
            $retry[0].detail.blocked-ne$false-or$retry[0].detail.nativeWorldDisposals-ne2-or
            $retry[0].detail.semantic-ne($sem+$n)-or$retry[0].detail.presentation-ne2-or
            $retry[0].detail.duplicateNotifications-ne4-or$retry[0].detail.nativeCallback-ne$true-or
            $retry[0].relationship-cne'Mounted'-or
            $next[0].detail.sequence-ne($resumed[0].detail.sequence+1)-or
            $next[1].detail.sequence-ne($next[0].detail.sequence+1)){
            throw 'P06 invalid combat granted readiness, lost its canceled-load fence or failed valid native recovery.'
        }
        foreach($actor in @('rider','mount')){
            foreach($field in @('Standard','Move','Swift','Initiative','Reaction','ReactionsRemaining','LastSurpriseTicks')){
                if($blocked[0].$actor.$field-ne$canceled[0].$actor.$field){throw 'Canceled invalid combat changed a resolvable obligation'}
            }
            foreach($row in $next){
                if($row.$actor.Standard-ne0-or$row.$actor.Move-ne0){throw 'Valid retry failed its true next activation refresh'}
            }
        }
    }elseif($refused){
        $denied=@($Rows|Where-Object kind -CEQ 'validation-native-load-refused')
        $retained=@($Rows|Where-Object kind -CEQ 'validation-original-world-retained')
        if($denied.Count-ne3-or$retained.Count-ne1){throw 'P06 did not exercise all three native refusal entries.'}
        for($index=0;$index-lt3;$index++){
            $row=$denied[$index]
            if($row.detail.entry-ne$index-or$row.detail.rejections-ne($index+1)-or$row.relationship-cne'Mounted'-or
                $row.detail.sha256-cne$Request.persistenceAlternate.sha256-or
                $row.detail.path-cne(Join-Path (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')) $Request.persistenceAlternate.fileName)-or
                $row.persistence.semantics-ne2-or$row.persistence.presentation-ne1-or[string]::IsNullOrEmpty($row.detail.feedback)){
                throw 'P06 rejection changed the world or lacks exact archive refusal evidence.'
            }
        }
    }else{
        $loads=@($Rows|Where-Object kind -CEQ 'validation-native-load-requested')
        $variant=@($Rows|Where-Object kind -CEQ 'validation-variant-loaded')
        $retry=@($Rows|Where-Object kind -CEQ 'validation-valid-retry')
        if($loads.Count-ne2-or$variant.Count-ne1-or$retry.Count-ne1-or$loads[0].detail.label-cne'B'-or$loads[1].detail.label-cne'A'-or
            $loads[0].detail.sha256-cne$Request.persistenceAlternate.sha256-or$loads[1].detail.sha256-cne$Request.persistenceLoad.sha256){
            throw 'P06 did not select B then A through native loads.'
        }
        $semantics=if($Request.persistenceCase-ceq'legacy'){2}elseif($Request.persistenceCase-cin @('missing-rider','missing-mount')){3}else{4}
        $presentation=if($Request.persistenceCase-ceq'schema1'){2}else{1}
        if($variant[0].detail.nativeWorldDisposals-ne1-or$retry[0].detail.nativeWorldDisposals-ne2-or
            $variant[0].detail.nativeCallback-ne$true-or$variant[0].detail.semantic-ne$semantics-or
            $variant[0].detail.presentation-ne$presentation-or$variant[0].detail.mounted-ne($Request.persistenceCase-ceq'schema1')-or
            $retry[0].detail.nativeCallback-ne$true-or$retry[0].detail.semantic-ne($semantics+2)-or
            $retry[0].detail.presentation-ne($presentation+1)-or$retry[0].detail.mounted-ne$true){
            throw 'P06 native restoration leaked prior state or invented participation.'
        }
    }
    foreach($kind in @('movement-dispatched','movement-completed','attack-dispatched','attack-delivered','usable-continuation-complete')){
        if(@($Rows|Where-Object kind -CEQ $kind).Count-ne1){throw "P06 lacks usable native continuation: $kind"}
    }
    $attack=@($Rows|Where-Object kind -CEQ 'attack-delivered')[0]
    if($attack.detail.rules-lt1-or$attack.detail.rolls-lt1){throw 'P06 ordinary attack did not resolve natively.'}
    $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId)
    $receipt=Read-KmcJson (Join-Path $root 'validation-copy.json')
    if($receipt.case-cne$Request.persistenceCase-or$receipt.sourceSha256-cne$Request.persistenceLoad.sha256-or
        $receipt.descriptor.sha256-cne$Request.persistenceAlternate.sha256-or$receipt.nativeMembersVerified-lt1){
        throw 'P06 lacks its bounded offline native-member preservation receipt.'
    }
    foreach($descriptor in @($Request.persistenceLoad,$Request.persistenceAlternate)){
        $path=Join-Path (Join-Path $root 'Saved Games') $descriptor.fileName
        if((Get-KmcSha256 $path)-cne$descriptor.sha256){throw 'P06 changed an input archive.'}
    }
}
