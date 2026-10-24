[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
    [Parameter(Mandatory=$true)][string]$ExpectedRequestSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedArtifactSha256,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$requestPath=Join-Path $EvidenceDirectory 'runtime-request.json'
$artifactPath=Join-Path $EvidenceDirectory 'chunk7-small-fixture-evidence.json'
foreach($p in @($requestPath,$artifactPath)){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw "Missing retained F09 input: $p"}}
$requestHash=(Get-FileHash -LiteralPath $requestPath -Algorithm SHA256).Hash.ToLowerInvariant()
$artifactHash=(Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
if($requestHash-cne$ExpectedRequestSha256.ToLowerInvariant()-or$artifactHash-cne$ExpectedArtifactSha256.ToLowerInvariant()){
    throw 'Immutable F09 request/artifact input hash differs.'
}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Existing-acquisition receipt needs a fresh output directory.'}
. (Join-Path $root 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $root 'scripts/runtime/Chunk7OrdinaryAcquisitionEvidence.ps1')
. (Join-Path $root 'scripts/runtime/Chunk7SmallFixtureEvidence.ps1')
. (Join-Path $root 'scripts/runtime/Chunk7SmallExistingAcquisitionEvidence.ps1')
$request=Get-Content -LiteralPath $requestPath -Raw|ConvertFrom-Json
$artifact=Get-Content -LiteralPath $artifactPath -Raw|ConvertFrom-Json
$proof=Get-KmcChunk7SmallExistingAcquisitionProof $request $artifact
$passes=1
function Expect-Refusal([string]$Name,[scriptblock]$Change){
    $copy=($artifact|ConvertTo-Json -Depth 100|ConvertFrom-Json)
    & $Change $copy.observations
    $rejected=$false
    try{$null=Get-KmcChunk7SmallExistingAcquisitionProof $request $copy}catch{$rejected=$true}
    if(-not$rejected){throw "Existing-acquisition reader accepted $Name"}
    $script:passes++
}
Expect-Refusal 'Horse present before level-up' {param($o)$o.choices[0].petId=$o.mainBuild.petId}
Expect-Refusal 'Horse absent after native commit' {param($o)($o.choices|Where-Object event -CEQ 'native-level-committed'|Select-Object -First 1).after.petId=$null}
Expect-Refusal 'incomplete native Total' {param($o)($o.choices|Where-Object event -CEQ 'native-total-next-requested'|Select-Object -First 1).nativeIsComplete=$false}
Expect-Refusal 'unselected Horse feature' {param($o)($o.choices|Where-Object { $_.event-ceq'native-choice'-and$_.kind-ceq'feature' }|Select-Object -First 1).nativeSelectionGuid='a'*32}
Expect-Refusal 'existing actor missing from loaded party' {param($o)$o.loadedWorld.party[0].id='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$readerFiles=@('scripts/runtime/RuntimeHarness.Common.ps1','scripts/runtime/Chunk7OrdinaryAcquisitionEvidence.ps1',
    'scripts/runtime/Chunk7OrdinaryChoiceEvidence.ps1','scripts/runtime/Chunk7SmallFixtureEvidence.ps1',
    'scripts/runtime/Chunk7SmallExistingAcquisitionEvidence.ps1','scripts/chunk7/Test-Chunk7SmallExistingAcquisition.ps1')
$readerHashes=@($readerFiles|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $root $_) -Algorithm SHA256).Hash.ToLowerInvariant()}})
$receipt=[pscustomobject][ordered]@{
    status='PASS';kind='reader-only-small-existing-acquisition';runId=$request.runId
    requestSha256=$requestHash;artifactSha256=$artifactHash;readerFiles=$readerHashes
    checksPassed=$passes;checksFailed=0;proof=$proof
    originalFixtureVerdict='PASS; fixture setup only';newNativeRun=$false
}
$receiptPath=Join-Path $OutputDirectory 'small-existing-acquisition-reader-result.json'
[IO.File]::WriteAllText($receiptPath,($receipt|ConvertTo-Json -Depth 12))
Write-Host "PASS scoped existing-character acquisition and $($passes-1) refusal checks; $receiptPath"
