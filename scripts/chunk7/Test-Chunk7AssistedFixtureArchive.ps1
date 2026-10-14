[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$ReaderPath,
    [string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $RepositoryRoot) { $RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) }
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
if (-not $ReaderPath) { $ReaderPath = Join-Path $RepositoryRoot 'scripts/runtime/Chunk7AssistedFixtureFixtures.ps1' }
if (-not $OutputRoot) { $OutputRoot = Join-Path (Join-Path (Get-KmcLabRoot) 'analysis-cache') ('chunk7-assisted-archive-'+[Guid]::NewGuid().ToString('N')) }
. $ReaderPath
$output = Assert-KmcChildPath $OutputRoot (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated assisted archive checks'
if (Test-Path -LiteralPath $output) { throw 'Preserve earlier assisted archive check outputs.' }
[void][IO.Directory]::CreateDirectory($output)
Assert-KmcDirectoryTreeCloneable $output 'generated assisted archive checks'
Add-Type -AssemblyName System.IO.Compression
$clock = [Diagnostics.Stopwatch]::StartNew()
$script:passed = 0
$script:failed = 0
$script:caseNumber = 0
$script:admissionCalls = 0
$cases = [Collections.Generic.List[object]]::new()
$inputPaths = @($PSCommandPath, $ReaderPath) + @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -Filter '*.ps1' -File | ForEach-Object FullName)
$inputPins = $inputPaths | Sort-Object -Unique | ForEach-Object {
    [pscustomobject]@{path=[IO.Path]::GetFullPath($_);sha256=(Get-KmcSha256 $_)}
}

# Only the native artifact/output admission boundary is substituted. The
# actual metadata reader, ZIP stream, strict UTF8, duplicate-member and schema
# validation execute against newly generated files in this output directory.
# The connected full native source gate is tested separately with its host DTO.
function Assert-KmcChunk7AssistedFixtureOutput($Request, $Artifact) {
    $script:admissionCalls++
    if ($Request.qualified -isnot [bool] -or -not $Request.qualified) { throw 'Synthetic native provenance was refused before archive access.' }
    $write = $Artifact.observations.finalManual
    $path = Assert-KmcChildPath $write.path $output 'this invocation generated archive'
    Assert-KmcNotReparsePoint $path 'generated archive'
    Assert-KmcNotHardLink $path 'generated archive'
    $file = Get-Item -LiteralPath $path
    if ($file.Length -ne $write.length -or $file.LastWriteTimeUtc.Ticks -ne $write.lastWriteTimeUtcTicks -or
        (Get-KmcSha256 $path) -cne $write.sha256) { throw 'Synthetic output identity differs.' }
}
function New-Metadata {
    [pscustomobject]@{
        descriptor=[pscustomobject]@{internalName='KMC_C7_ASSISTED_FIXTURE';gameId='aaaaaaaa-1111-4111-8111-111111111111';gameName='KMC Generated';area=('b'*32)}
        header=[pscustomobject]@{Name='KMC_C7_ASSISTED_FIXTURE';Type='Manual';GameId='aaaaaaaa-1111-4111-8111-111111111111';GameName='KMC Generated';Area=('b'*32);CompatibilityVersion=1;IsAutoLevelupSave=$false}
        archive=[pscustomobject]@{SchemaVersion=3;CampaignId='aaaaaaaa-1111-4111-8111-111111111111';AreaId=('b'*32);GameTimeTicks=1000L;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=@();Participations=@();Slots=@();Combat=$null}
    }
}
function Write-GeneratedArchive($Value, [string]$Variant) {
    $script:caseNumber++
    $path = Join-Path $output ('generated-'+$script:caseNumber.ToString('D3')+'.zks')
    $header = $Value.header | ConvertTo-Json -Depth 10 -Compress
    $metadata = $Value.archive | ConvertTo-Json -Depth 10 -Compress
    if ($Variant -ceq 'header-array') { $header = '['+$header+']' }
    if ($Variant -ceq 'state-array') { $metadata = '['+$metadata+']' }
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    $entries = [Collections.Generic.List[object]]::new()
    if ($Variant -cne 'missing-header') {
        $name = if ($Variant -ceq 'case-header') { 'Header.json' } else { 'header.json' }
        $headerBytes = $utf8.GetBytes($header)
        if ($Variant -ceq 'header-oversize') { $headerBytes = $utf8.GetBytes(' ' * (1MB+1)) }
        if ($Variant -ceq 'header-invalid-utf8') { $headerBytes = [byte[]]@(255,254,255) }
        if ($Variant -ceq 'header-bom') { $headerBytes = [byte[]](@(239,187,191)+$headerBytes) }
        $entries.Add([pscustomobject]@{name=$name;bytes=$headerBytes})
        if ($Variant -ceq 'duplicate-header') { $entries.Add([pscustomobject]@{name='header.json';bytes=$headerBytes}) }
    }
    if ($Variant -cne 'missing-state') {
        $stateBytes = $utf8.GetBytes($metadata)
        if ($Variant -ceq 'state-oversize') { $stateBytes = $utf8.GetBytes(' ' * (128KB+1)) }
        if ($Variant -ceq 'state-duplicate-json') { $stateBytes = $utf8.GetBytes($metadata.Replace('"SchemaVersion":3','"SchemaVersion":3,"schemaVersion":3')) }
        if ($Variant -ceq 'state-trailing-json') { $stateBytes = $utf8.GetBytes($metadata+' {}') }
        $entries.Add([pscustomobject]@{name='kmc-mounted-state';bytes=$stateBytes})
        if ($Variant -ceq 'case-alias-state') { $entries.Add([pscustomobject]@{name='KMC-MOUNTED-STATE';bytes=$stateBytes}) }
    }
    # Deliberately invalid text in an unrelated member proves it is not decoded.
    $entries.Add([pscustomobject]@{name='party.json';bytes=[byte[]]@(255,254,255)})
    if ($Variant -ceq 'entry-count') {
        for ($i=0; $i -lt 4094; $i++) { $entries.Add([pscustomobject]@{name=('opaque-'+$i);bytes=[byte[]]@(1)}) }
    }
    $stream = $null; $zip = $null
    try {
        $stream = [IO.FileStream]::new($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        $zip = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
        foreach ($entry in $entries) {
            $writer = $null
            try { $writer=$zip.CreateEntry($entry.name).Open(); $writer.Write($entry.bytes,0,$entry.bytes.Length) }
            finally { if ($writer) { $writer.Dispose() } }
        }
    }
    finally { if ($zip) { $zip.Dispose() }; if ($stream) { $stream.Dispose() } }
    $file=Get-Item -LiteralPath $path
    $write = [ordered]@{}
    foreach ($key in @('internalName','gameId','gameName','area')) { $write[$key]=$Value.descriptor.$key }
    $write.path=$path; $write.length=$file.Length; $write.lastWriteTimeUtcTicks=$file.LastWriteTimeUtc.Ticks; $write.sha256=Get-KmcSha256 $path
    [pscustomobject]@{observations=[pscustomobject]@{finalManual=[pscustomobject]$write}}
}
function Run-Case([string]$Name,[bool]$ExpectedPass,[scriptblock]$Mutation,[string]$Variant='valid',[bool]$Qualified=$true) {
    $value=New-Metadata
    if ($Mutation) { &$Mutation $value }
    $artifact=Write-GeneratedArchive $value $Variant
    if (-not $Qualified) { $artifact.observations.finalManual.path=Join-Path $output 'not-created.zks' }
    $before=$script:admissionCalls; $errorText=$null
    try { Assert-KmcChunk7AssistedFixtureArchive ([pscustomobject]@{qualified=$Qualified}) $artifact }
    catch { $errorText=$_.ToString() }
    $ok=($null-eq$errorText)-eq$ExpectedPass -and $script:admissionCalls-eq($before+1)
    if (-not $Qualified -and $errorText -cne 'Synthetic native provenance was refused before archive access.') { $ok=$false }
    if ($ok) { $script:passed++; Write-Host ('PASS '+$Name) }
    else { $script:failed++; Write-Host ('FAIL '+$Name+': '+$errorText) }
    $cases.Add([ordered]@{name=$Name;expectedPass=$ExpectedPass;passed=$ok;error=$errorText;archive=$artifact.observations.finalManual})
}
Run-Case 'exact empty schema3 Manual metadata; unrelated party bytes are unread' $true $null
Run-Case 'valid UTF8 BOM' $true $null 'header-bom'
Run-Case 'header singleton array root' $false $null 'header-array'
Run-Case 'metadata singleton array root' $false $null 'state-array'
Run-Case 'missing native header' $false $null 'missing-header'
Run-Case 'missing mounted metadata' $false $null 'missing-state'
Run-Case 'duplicate native header' $false $null 'duplicate-header'
Run-Case 'case-ambiguous metadata entry' $false $null 'case-alias-state'
Run-Case 'noncanonical header entry name' $false $null 'case-header'
Run-Case 'header byte bound' $false $null 'header-oversize'
Run-Case 'mounted metadata byte bound' $false $null 'state-oversize'
Run-Case 'invalid UTF8' $false $null 'header-invalid-utf8'
Run-Case 'duplicate or case-ambiguous JSON member' $false $null 'state-duplicate-json'
Run-Case 'trailing JSON value' $false $null 'state-trailing-json'
Run-Case 'bounded archive entry inventory' $false $null 'entry-count'
Run-Case 'schema2 cannot qualify new assisted output' $false {param($v) $v.archive.SchemaVersion=2}
Run-Case 'mounted membership is absent' $false {param($v) $v.archive.Pairs=@([pscustomobject]@{PairId='foreign'})}
Run-Case 'retained participation is absent' $false {param($v) $v.archive.Participations=@([pscustomobject]@{OwnerId='foreign'})}
Run-Case 'native control custody is absent' $false {param($v) $v.archive.Slots=@([pscustomobject]@{Kind=1})}
Run-Case 'combat custody is absent' $false {param($v) $v.archive.Combat=[pscustomobject]@{}}
Run-Case 'exact paired policy' $false {param($v) $v.archive.Policy='legacy'}
Run-Case 'exact supported rules' $false {param($v) $v.archive.RulesId='foreign'}
Run-Case 'native campaign binding' $false {param($v) $v.archive.CampaignId='cccccccc-1111-4111-8111-111111111111'}
Run-Case 'native area binding' $false {param($v) $v.archive.AreaId='c'*32}
Run-Case 'nonnegative saved clock' $false {param($v) $v.archive.GameTimeTicks=-1}
Run-Case 'integral saved clock' $false {param($v) $v.archive.GameTimeTicks=1.5}
Run-Case 'no string-coerced saved clock' $false {param($v) $v.archive.GameTimeTicks='1000'}
Run-Case 'complete metadata envelope' $false {param($v) $v.archive.PSObject.Properties.Remove('GameTimeTicks')}
Run-Case 'unknown metadata claim' $false {param($v) $v.archive|Add-Member -NotePropertyName Restored -NotePropertyValue $true}
Run-Case 'native header is Manual' $false {param($v) $v.header.Type='Auto'}
Run-Case 'native header preserves exact name' $false {param($v) $v.header.Name='KMC_OTHER'}
Run-Case 'auto-levelup cannot pose as final Manual' $false {param($v) $v.header.IsAutoLevelupSave=$true}
Run-Case 'failed native source gate refuses' $false $null 'valid' $false
$changed=@($inputPins|Where-Object {(Get-KmcSha256 $_.path)-cne$_.sha256})
if ($changed.Count -ne 0) { $script:failed++; Write-Host 'FAIL test inputs changed' }
$status=if($script:failed-eq0-and$script:passed-gt0){'PASS'}else{'FAIL'}
[ordered]@{kind='generated-assisted-fixture-archive-checks';status=$status;passed=$script:passed;failed=$script:failed;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputPins;changedInputs=$changed;cases=$cases.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;archiveOperations='Only this invocation generated ZIP fixtures';substitutions=@('native provenance/output admission boundary; full connected source gate has a separate focused test')} |
    ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $output 'receipt.json') -Encoding UTF8
if ($status-cne'PASS') { exit 1 }
exit 0
