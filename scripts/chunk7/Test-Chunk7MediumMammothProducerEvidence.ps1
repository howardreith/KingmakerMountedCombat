[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionEvidence.ps1')
$root=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world120-b116s3-medium-mammoth-retrain'
$path=Join-Path $root 'chunk7-progression-evidence.json'
if((Get-KmcSha256 $path)-cne'68e4b456d9601f97c79f34c8a523a90d716fdcfe5eebd524032af1a308c64336'){
    throw 'Batch116 native Mammoth producer changed; reader regression cannot transfer its result.'
}
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
$original=Read-KmcJson $path
function Copy-Evidence { $original|ConvertTo-Json -Depth 100|ConvertFrom-Json }
function Refuses([string]$Name,[scriptblock]$Mutation,[string]$ExpectedMessage){
    $artifact=Copy-Evidence;&$Mutation $artifact
    $reason=$null
    try{Assert-KmcChunk7ProgressionEvidence $request $artifact 'PASS'}catch{$reason=$_.Exception.Message}
    if($null-eq$reason-or-not$reason.Contains($ExpectedMessage)){throw ('Mammoth reader did not reject '+$Name+' at its intended gate: '+$reason)}
    Write-Host ('PASS refuses '+$Name)
}
Assert-KmcChunk7ProgressionEvidence $request $original 'PASS'
Write-Host 'PASS exact native Mammoth producer public reader'
Refuses 'missing rank-seven Large event' {
    param($a)
    $a.observations.session.events=@($a.observations.session.events|Where-Object{$null-eq$_.PSObject.Properties['event'] -or $_.event -cne 'native-retrain-mammoth-rank-seven'})
} 'Native progression choices/companion chain incomplete.'
Refuses 'false Large mount' {
    param($a)
    @($a.observations.session.events|Where-Object{$_.PSObject.Properties['event'] -and $_.event -ceq 'native-retrain-mammoth-rank-seven'})[0].mountSize='Medium'
} 'Bootstrap inventory exact scalar identity differs.'
Refuses 'changed one-time XP grant' {
    param($a)
    $a.observations.fixtureExperienceSetup.nativeGrant=95999
} 'Ordinary native counter is missing, mistyped or out of range.'
$early=Copy-Evidence
$rows=@($early.observations.session.events)
$rank=@($rows|Where-Object{$_.PSObject.Properties['event'] -and $_.event -ceq 'native-retrain-mammoth-rank-seven'})[0]
$rows=@($rows|Where-Object{$_ -ne $rank})
$first=@($rows|Where-Object{$_.PSObject.Properties['event'] -and $_.event -ceq 'native-level-committed'})[0]
$index=[Array]::IndexOf($rows,$first)
if($index-lt0){throw 'Batch116 lacks its first real native level commit.'}
$rank.frame=$first.frame;$rank.elapsedSeconds=$first.elapsedSeconds
$early.observations.session.events=@($rows[0..$index])+@($rank)+@($rows[($index+1)..($rows.Count-1)])
$reason=$null
try{Assert-KmcChunk7ProgressionEvidence $request $early 'PASS'}catch{$reason=$_.Exception.Message}
if($null-eq$reason-or-not$reason.Contains('Unexpected, early or repeated native Mammoth upgrade observation.')){
    throw ('Mammoth reader did not reject a rank event after the first commit at its intended gate: '+$reason)
}
Write-Host 'PASS refuses rank event after first rather than tenth native commit'
Write-Host 'TOTAL PASS=5 FAIL=0; immutable batch116 producer and in-memory negative reader mutations only'
