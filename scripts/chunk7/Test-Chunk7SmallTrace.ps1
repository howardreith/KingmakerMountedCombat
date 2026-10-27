[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureSource,[string]$FieldFixtureSource,[string]$FieldReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$FixtureSource){$FixtureSource=Join-Path $PSScriptRoot 'Test-Chunk7SmallEvidence.ps1'}
if(-not$FieldFixtureSource){$FieldFixtureSource=Join-Path $PSScriptRoot 'Test-Chunk7FieldEvidence.ps1'}
if(-not$FieldReaderDirectory){$FieldReaderDirectory=$ReaderDirectory}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-trace-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$traceReaderSourcePin=Join-Path $ReaderDirectory 'Chunk7SmallCalibrationEvidence.ps1'
. $FixtureSource -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FieldTestSource $FieldFixtureSource -FieldReaderDirectory $FieldReaderDirectory -FixturesOnly
$passes=0;$failures=0
$f=New-SmallFixture;Assert-KmcChunk7SmallEvidence $f.request $f.artifact;$passes++;Write-Host 'PASS actual-shaped retained native trace positive'
$cases=[ordered]@{
 'identity collision fault'={param($o)$o.allocationTrace.identityRegistry.faults=1}
 'wrong identity contract'={param($o)$o.allocationTrace.identityRegistry.contract='unretained-hash'}
 'text identity capacity'={param($o)$o.allocationTrace.identityRegistry.capacity='16000'}
 'released registry before capture'={param($o)$o.allocationTrace.identityRegistry.released=$true}
 'retained count numeric text'={param($o)$o.allocationTrace.identityRegistry.retainedCount='13'}
 'retained count mismatch'={param($o)$o.allocationTrace.identityRegistry.retainedCount=12}
 'missing native cost hook'={param($o)$o.allocationTrace.observerHooks=@($o.allocationTrace.observerHooks|Where-Object token -CNE '06009120')}
 'native cost hook from other module'={param($o)$o.allocationTrace.observerHooks[1].moduleMvid='11111111-1111-4111-8111-111111111111'}
 'duplicate native labels'={param($o)$o.allocationTrace.identityRegistry.ids[0]=31}
 'unregistered actor label'={param($o)$o.allocationTrace.identityRegistry.ids[4]=999}
 'unregistered primary command'={param($o)$o.allocationTrace.identityRegistry.ids[10]=999}
 'unregistered native shell'={param($o)$o.allocationTrace.identityRegistry.ids[6]=999}
 'actor label substituted with another registered actor'={param($o)$o.allocationTrace.events[0].state.actorObject=34;$o.inputs[1].nativeEvents[0].state.actorObject=34}
 'native sequence numeric text'={param($o)$o.allocationTrace.events[0].sequence='1';$o.inputs[1].nativeEvents[0].sequence='1'}
 'native command numeric text'={param($o)$o.allocationTrace.events[0].command='201';$o.inputs[1].nativeEvents[0].command='201'}
 'native actor label numeric text'={param($o)$o.allocationTrace.events[0].state.actorObject='33';$o.inputs[1].nativeEvents[0].state.actorObject='33'}
 'identity fault numeric text'={param($o)$o.allocationTrace.identityRegistry.faults='0'}
 'hook token array'={param($o)$o.allocationTrace.observerHooks[1].token=@('06009120')}
 'foreign target trace actor'={param($o)$o.allocationTrace.events[-1].state.actor='88888888-1111-4111-8111-111111111111'}
 'registered target label replaced by controlled actor'={param($o)$o.target.object=33;$o.allocationTrace.events[-1].state.actorObject=33;$o.allocationTrace.events[-2].state.actorObject=33}
 'unregistered owned target label'={param($o)$o.allocationTrace.identityRegistry.ids[-1]=999}
 'other owned target group'={param($o)$o.target.groupId='KMC.RuntimeHostile.other:small-profile-target'}
 'other target blueprint'={param($o)$o.target.blueprint='1'*32}
}
foreach($case in $cases.GetEnumerator()){$f=New-SmallFixture;&$case.Value $f.artifact.observations;$refused=$false;try{Assert-KmcChunk7SmallEvidence $f.request $f.artifact}catch{$refused=$true};if($refused){$passes++;Write-Host ('PASS refuses '+$case.Key)}else{$failures++;Write-Host ('FAIL accepts '+$case.Key)}}
@{passes=$passes;failures=$failures;nativeProof=$false;readerSha256=(Get-FileHash -LiteralPath $traceReaderSourcePin).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
Write-Host ('SMALL TRACE PASS '+$passes+' FAIL '+$failures);if($failures){throw 'Small reader did not enforce retained native trace identity.'}
