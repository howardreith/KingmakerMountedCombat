Set-StrictMode -Version Latest

# Offline, owned copies only. Native entity/header/screenshot bytes are never
# edited. This is fixture derivation, not a production save or migration writer.
function Get-KmcPersistenceValidationSource {
    param([string]$SourceRunId,[string]$ExpectedSha256,$Fixture)
    if($SourceRunId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'-or$SourceRunId-in@('.','..')){
        throw 'Invalid validation source run.'
    }
    $root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$SourceRunId)) (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'validation source'
    Assert-KmcDirectoryTreeCloneable $root 'validation source'
    $owner=Read-KmcJson (Join-Path $root 'owner.json')
    $category=$null
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
        [ValidateSet('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy')][string]$Case)
    Assert-KmcNoGameProcesses
    $source=Get-KmcPersistenceValidationSource $SourceRunId $ExpectedSha256 $Fixture
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
        if($data.SchemaVersion-ne2-or$data.Mounted-ne$true-or$null-ne$data.Combat-or
            $data.CampaignId-cne$Fixture.working.gameId-or$data.AreaId-cne$Fixture.working.area){
            throw 'Only the current outside-combat mounted source may seed these variants.'
        }
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

function Assert-KmcValidationPersistenceEvidence {
    param($Request,$Rows,$GameResult)
    $initial=@($Rows|Where-Object kind -CEQ 'initial')
    if($initial.Count-ne1){throw 'P06 lacks its original cold native world.'}
    foreach($row in $Rows){
        if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.source-cne$Request.commit-or
            $row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or
            $row.checkpoint-cne$Request.persistenceCase-or$row.rider.Id-cne$initial[0].rider.Id-or
            $row.mount.Id-cne$initial[0].mount.Id-or$row.controls.DuplicateFactCount-ne0){
            throw 'P06 native actor, source or owned-control identity differs.'
        }
    }
    $refused=$Request.persistenceCase-cin @('future','malformed','profile','campaign','policy')
    if($refused){
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
