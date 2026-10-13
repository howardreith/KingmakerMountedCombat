Set-StrictMode -Version Latest

function Initialize-KmcWorkingActorGraphReader {
    $source=Join-Path $PSScriptRoot 'WorkingActorGraphReader.cs'
    $hash=Get-KmcSha256 $source
    if('KingmakerMountedCombat.Harness.WorkingActorGraphReader' -as [type]) {
        $prior=Get-Variable -Scope Script -Name kmcWorkingActorGraphReaderHash -ErrorAction SilentlyContinue
        if($null-eq$prior-or$prior.Value-cne$hash){throw 'Working graph reader type is already loaded with unproven source identity.'}
    } else {
        Add-Type -TypeDefinition ([IO.File]::ReadAllText($source)) -ReferencedAssemblies @('System.dll','System.Core.dll','System.Xml.dll','System.Runtime.Serialization.dll')
        $script:kmcWorkingActorGraphReaderHash=$hash
    }
    return $hash
}

# Internal stream boundary. The public command derives this descriptor only from
# the unchanged qualified disposable Baseline/Working pair; there is no path option.
function Read-KmcQualifiedWorkingActorGraphArchive {
    param([Parameter(Mandatory=$true)]$Working,[Parameter(Mandatory=$true)][string]$SaveRoot)
    $path=Assert-KmcChildPath ([string]$Working.path) $SaveRoot 'qualified Working actor-graph input'
    if([IO.Path]::GetDirectoryName($path)-cne[IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')-or
        [IO.Path]::GetFileName($path)-cne$Working.fileName-or$Working.fileName-cnotmatch'^Manual_[0-9]+_KMC_AUTOMATION_WORKING\.zks$'-or
        $Working.name-cne'KMC_AUTOMATION_WORKING'){throw 'Working graph input is not the exact qualified Working leaf.'}
    Assert-KmcNotReparsePoint $path 'qualified Working graph input'
    Assert-KmcNotHardLink $path 'qualified Working graph input'
    $before=Get-Item -LiteralPath $path -Force
    [KingmakerMountedCombat.Harness.WorkingActorGraphReader]::CheckArchiveLength($before.Length)
    if($before.Length-ne$Working.length-or$before.LastWriteTimeUtc.Ticks-ne$Working.lastWriteTimeUtcTicks){throw 'Working graph input metadata changed before observation.'}
    Add-Type -AssemblyName System.IO.Compression
    $stream=$null;$archive=$null;$digest=$null
    try {
        $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $digest=[Security.Cryptography.SHA256]::Create()
        $openedHash=([BitConverter]::ToString($digest.ComputeHash($stream))).Replace('-','').ToLowerInvariant()
        if($openedHash-cne$Working.sha256-or$stream.Length-ne$Working.length){throw 'Working graph input bytes changed before observation.'}
        $stream.Position=0
        $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$true)
        if($archive.Entries.Count-lt3-or$archive.Entries.Count-gt1024){throw 'Working graph archive member-count limit.'}
        $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach($entry in $archive.Entries){
            $name=$entry.FullName
            if([string]::IsNullOrEmpty($name)-or$name.Length-gt256-or$name-match'[\\/:\x00-\x1f\x7f]'-or$name-cin@('.','..')-or-not$names.Add($name)){
                throw 'Working graph archive has traversal, nested, duplicate or case-colliding member names.'
            }
        }
        $selected=@{}
        foreach($name in @('header.json','player.json','party.json')){
            $entries=@($archive.Entries|Where-Object FullName -CEQ $name)
            if($entries.Count-ne1){throw 'Working graph requires one exact root header/player/party member.'}
            if($entries[0].Length-le0-or$entries[0].Length-gt$(if($name-ceq'header.json'){1MB}else{16MB})){
                throw 'Working graph selected-member byte limit.'
            }
            $selected[$name]=$entries[0]
        }
        # Header contents remain solely under the existing fixture guard. Only the
        # selected player/party bytes are read here, never any other native member.
        $playerStream=$selected['player.json'].Open()
        try{$player=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::ReadMember($playerStream,$selected['player.json'].Length)}finally{$playerStream.Dispose()}
        $partyStream=$selected['party.json'].Open()
        try{$party=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::ReadMember($partyStream,$selected['party.json'].Length)}finally{$partyStream.Dispose()}
        $observation=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::Analyze($player,$party)
        $memberCount=$archive.Entries.Count
        $archive.Dispose();$archive=$null
        $stream.Position=0
        $afterHash=([BitConverter]::ToString($digest.ComputeHash($stream))).Replace('-','').ToLowerInvariant()
        $after=Get-Item -LiteralPath $path -Force
        if($afterHash-cne$Working.sha256-or$stream.Length-ne$Working.length-or$after.Length-ne$Working.length-or
            $after.LastWriteTimeUtc.Ticks-ne$Working.lastWriteTimeUtcTicks){throw 'Working graph input identity changed during observation.'}
        return [pscustomobject]@{archiveMemberCount=$memberCount;playerBytes=$player.Length;partyBytes=$party.Length;
            beforeSha256=$openedHash;afterSha256=$afterHash;observation=$observation}
    } finally {
        if($archive){$archive.Dispose()};if($digest){$digest.Dispose()};if($stream){$stream.Dispose()}
    }
}

function Invoke-KmcQualifiedWorkingActorGraphDiagnostic {
    param(
        [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQualificationSha256,
        [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedWorkingSha256)
    $lab=Get-KmcLabRoot
    $stateRoot=Join-Path $lab 'runtime-state'
    Assert-KmcNotReparsePoint $stateRoot 'Working graph runtime state'
    $qualification=Assert-KmcChildPath (Join-Path $stateRoot 'fixture-qualification.json') $stateRoot 'Working graph qualification'
    Assert-KmcNotReparsePoint $qualification 'Working graph qualification'
    Assert-KmcNotHardLink $qualification 'Working graph qualification'
    $qualificationBefore=Get-Item -LiteralPath $qualification -Force
    if((Get-KmcSha256 $qualification)-cne$ExpectedQualificationSha256){throw 'Working graph qualification hash differs.'}
    $intakePath=Join-Path $lab 'environment-intake.json'
    Assert-KmcNotReparsePoint $intakePath 'Working graph environment intake'
    Assert-KmcNotHardLink $intakePath 'Working graph environment intake'
    $intakeHash=Get-KmcSha256 $intakePath
    $intake=Read-KmcJson $intakePath
    $saveRoot=[IO.Path]::GetFullPath([string]$intake.requestedLayout.kingmakerSaveRoot).TrimEnd('\')
    Assert-KmcNotReparsePoint $saveRoot 'Working graph configured save root'
    Assert-KmcPathsDoNotOverlap -First $saveRoot -Second $stateRoot -Description 'Working graph save and state roots'
    $activeLock=Join-Path $stateRoot 'active-transaction.lock'
    Assert-KmcNoGameProcesses
    if(Test-Path -LiteralPath $activeLock){throw 'Active runtime transaction prevents Working graph observation.'}
    $pair=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualification
    if($pair.working.sha256-cne$ExpectedWorkingSha256){throw 'Working graph expected qualified Working hash differs.'}
    $readerHash=Initialize-KmcWorkingActorGraphReader
    $observed=Read-KmcQualifiedWorkingActorGraphArchive -Working $pair.working -SaveRoot $saveRoot
    Assert-KmcNoGameProcesses
    if(Test-Path -LiteralPath $activeLock){throw 'Runtime transaction appeared during Working graph observation.'}
    if((Get-KmcSha256 $intakePath)-cne$intakeHash-or(Get-KmcSha256 $qualification)-cne$ExpectedQualificationSha256){
        throw 'Working graph environment or qualification changed during observation.'
    }
    $qualificationAfter=Get-Item -LiteralPath $qualification -Force
    if($qualificationAfter.Length-ne$qualificationBefore.Length-or$qualificationAfter.LastWriteTimeUtc.Ticks-ne$qualificationBefore.LastWriteTimeUtc.Ticks){
        throw 'Working graph qualification metadata changed during observation.'
    }
    $after=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualification
    foreach($role in @('baseline','working')){
        foreach($key in @('path','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area')){
            if($after.$role.$key-cne$pair.$role.$key){throw 'Qualified fixture pair changed during Working graph observation.'}
        }
    }
    return [pscustomobject]@{schemaVersion=1;kind='qualified-working-actor-graph-diagnostic';status=$observed.observation.status;
        qualificationSha256=$ExpectedQualificationSha256;environmentIntakeSha256=$intakeHash;readerSha256=$readerHash;
        working=$pair.working;archive=$observed;sourceBytesPreserved=$true;nativeAdmission=$false;
        limits=[ordered]@{archiveBytes=268435456;archiveMembers=1024;selectedMemberBytes=16777216;selectedCombinedBytes=33554432;
            jsonDepth=64;jsonTokensPerMember=250000;scalarTextChars=1048576;entityRecords=4096;identityChars=256}}
}
