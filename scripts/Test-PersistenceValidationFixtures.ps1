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
Write-Output ("P06 FIXTURE GUARDS PASS="+$passes+" FAIL=0")
