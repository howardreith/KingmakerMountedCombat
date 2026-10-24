[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$FixtureRoot,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory);$fixtures=[IO.Path]::GetFullPath($FixtureRoot)
if(-not$out.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)-or-not$fixtures.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Private generated input and fresh output required.'}
$reader=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/WorkingLoadDeserializationEvidence.ps1'
$common=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeHarness.Common.ps1'
$manifest=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeArtifactManifestEvidence.ps1'
$rows=@('complete-observation','failed-load','unpatch-retry','writer-retry','faulted-pending')
$paths=@($PSCommandPath,$reader,$common,$manifest,(Join-Path $fixtures 'receipt.json'))
foreach($row in $rows){$paths+=Join-Path $fixtures ($row+'/request.json');$leaf=if($row-ceq'faulted-pending'){'working-load-deserialization-pending.json'}else{'working-load-deserialization-evidence.json'};$paths+=Join-Path $fixtures ($row+'/'+$leaf)}
$pins=@($paths|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
[void][IO.Directory]::CreateDirectory($out);Copy-Item -LiteralPath $reader -Destination (Join-Path $out 'Reader.ps1');Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $out 'Test.ps1')
function FunctionText([string]$path,[string]$name){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors);if($errors.Count){throw 'Helper source does not parse.'};$nodes=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($nodes.Count-ne1){throw 'Helper function not unique.'};$nodes[0].Extent.Text}
. ([ScriptBlock]::Create((FunctionText $common 'Assert-KmcExactProperties')))
. ([ScriptBlock]::Create((FunctionText $manifest 'Test-KmcManifestJsonInteger')))
. $reader
$passed=0;$failed=0;$log=[Collections.Generic.List[string]]::new()
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:passed++;$script:log.Add('PASS '+$Name)}catch{$script:failed++;$script:log.Add('FAIL '+$Name+': '+$_.Exception.Message)}}
function Clone($value){$value|ConvertTo-Json -Depth 30 -Compress|ConvertFrom-Json}
function Bad([string]$Label,[scriptblock]$Mutation){Check $Label {$a=Clone $script:valid;&$Mutation $a;$refused=$false;try{Assert-KmcWorkingLoadDeserializationValue $a $script:request}catch{$refused=$true};if(-not$refused){throw 'Malformed generated evidence accepted.'}}}
foreach($row in $rows){$leaf=if($row-ceq'faulted-pending'){'working-load-deserialization-pending.json'}else{'working-load-deserialization-evidence.json'};$a=Get-Content -LiteralPath (Join-Path $fixtures ($row+'/'+$leaf)) -Raw|ConvertFrom-Json;$r=Get-Content -LiteralPath (Join-Path $fixtures ($row+'/request.json')) -Raw|ConvertFrom-Json;Check ('actual producer '+$row) {Assert-KmcWorkingLoadDeserializationValue $a $r -Pending:($row-ceq'faulted-pending')}}
$valid=Get-Content -LiteralPath (Join-Path $fixtures 'complete-observation/working-load-deserialization-evidence.json') -Raw|ConvertFrom-Json
$request=Get-Content -LiteralPath (Join-Path $fixtures 'complete-observation/request.json') -Raw|ConvertFrom-Json
foreach($name in @('status','evidenceKind','runId','dllSha256','createdAtUtc')){$n=$name;Bad ('header singleton '+$n) {$args[0].$n=@($args[0].$n)}}
foreach($name in @('observationOnly','nativeResolutionProven','taskSucceeded','observerReleased')){$n=$name;Bad ('Boolean singleton '+$n) {$args[0].observations.$n=@($args[0].observations.$n)}}
foreach($name in @('internalName','fileName','sha256')){$n=$name;Bad ('source singleton '+$n) {$args[0].observations.source.$n=@($args[0].observations.source.$n)}}
Bad 'unknown artifact property' {$args[0]|Add-Member -NotePropertyName unexpected -NotePropertyValue 1}
Bad 'fake native resolution' {$args[0].observations.nativeResolutionProven=$true}
Bad 'fake atomic list' {$args[0].observations.atomicityProven=$true}
Bad 'wrong module' {$args[0].observations.gameMvid=[Guid]::NewGuid().ToString()}
Bad 'wrong source hash' {$args[0].observations.source.sha256='c'*64}
Bad 'write not allowed' {$args[0].observations.nativeWriteCount=1}
Bad 'second load not allowed' {$args[0].observations.nativeLoadCount=2}
Bad 'unreleased final' {$args[0].observations.observerReleased=$false}
Bad 'retained final identities' {$args[0].observations.retainedNativeIdentityCount=1}
Bad 'busy final' {$args[0].observations.nativeBusyAtCapture=$true}
Bad 'faulted task release' {$args[0].observations.taskSucceeded=$false}
Bad 'unsettled succeeded task' {$args[0].observations.taskSettled=$false}
Bad 'unobserved task' {$args[0].observations.taskObserved=$false}
Bad 'PASS no original callback' {$args[0].observations.afterLoadCallbackObserved=$false}
Bad 'PASS no original fixture verification' {$args[0].observations.fixtureIdentityVerified=$false}
Bad 'invalid integer token' {$args[0].observations.nativeLoadCount='1'}
Bad 'event sequence duplicate' {$args[0].observations.events[1].sequence=1}
Bad 'event phase singleton' {$args[0].observations.events[0].phase=@('loader-created')}
Bad 'event lane singleton' {$args[0].observations.events[2].lane=@('Player')}
Bad 'event identity scalar array' {$args[0].observations.events[0].valueIdentity=@(3)}
Bad 'return assignment replacement' {$args[0].observations.events[4].valueIdentity=127}
Bad 'serializer before loader' {$args[0].observations.events[0].phase='serializer-returned';$args[0].observations.events[0].lane='Player'}
Bad 'missing snapshot without read fault' {$args[0].observations.events[2].serializer=$null}
Bad 'foreign snapshot phase' {$args[0].observations.events[0].scene=$args[0].observations.events[8].scene}
Bad 'converter unproven enablement' {$args[0].observations.events[2].serializer.converterTypes=@('Foreign.Converter')}
Bad 'converter flag wrong type' {$args[0].observations.events[2].serializer.collectionsEnabled=@($true)}
Bad 'converter count mismatch' {$args[0].observations.events[2].serializer.converterCount=2}
Bad 'scene singleton raw ID' {$args[0].observations.events[8].scene.mainId=@('literal-main')}
Bad 'missing main cannot compare' {$args[0].observations.events[8].scene.mainId=$null;$args[0].observations.events[8].scene.mainIdComplete=$false}
Bad 'false literal equality' {$args[0].observations.events[8].scene.prefix[0].id='other'}
Bad 'raw prefix count mismatch' {$args[0].observations.events[8].scene.entityCount=2}
Bad 'false match count' {$args[0].observations.events[8].scene.matchingPrefixCount=0}
Bad 'atomicity from counts' {$args[0].observations.events[8].scene.atomicityProven=$true}
Bad 'text control C1' {$args[0].observations.events[8].scene.prefix[0].type='a'+[char]0x85}
Bad 'scene raw identity differs from event' {$args[0].observations.events[8].scene.sceneIdentity=127}
Bad 'scene after replaces captured player' {$args[0].observations.events[10].scene.playerIdentity=127}
Bad 'serializer raw identity differs from event' {$args[0].observations.events[2].serializer.identity=127}
Bad 'null loader identity' {$args[0].observations.events[0].valueIdentity=0}
Bad 'null scene player identity' {$args[0].observations.events[8].scene.playerIdentity=0}
Bad 'unknown fault' {$args[0].observations.faults=@('Foreign')}
Bad 'duplicate fault' {$args[0].observations.faults=@('ReadFailure','ReadFailure')}
Check 'read failure snapshot remains nonauthorizing' {$a=Clone $valid;$a.status='FAIL';$a.observations.runtimeStatusAtPublication='FAIL';$a.observations.events[2].serializer=$null;$a.observations.faults=@('ReadFailure');Assert-KmcWorkingLoadDeserializationValue $a $request}
Check 'null ID stays unavailable' {$a=Clone $valid;$s=$a.observations.events[8].scene;$s.mainId=$null;$s.mainIdComplete=$false;$s.complete=$false;$s.matchingPrefixCount=0;$s.prefix[0].mainIdMatches=$null;Assert-KmcWorkingLoadDeserializationValue $a $request}
$same=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$log.Add("WORKING LOAD READER PASS=$passed FAIL=$failed");[IO.File]::WriteAllLines((Join-Path $out 'checks.log'),$log,[Text.UTF8Encoding]::new($false))
[ordered]@{schemaVersion=1;status=$(if($failed-eq0-and$same){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;inputsUnchanged=$same;inputPins=$pins;actualProducerFixtures=$rows;externalBoundary='Exact value reader and two unchanged common shape functions; only generated JSON from actual session/recorder/artifact writer under explicit native test boundaries. No qualification/archive/native operations.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'receipt.json') -Encoding UTF8
$log
if($failed-ne0-or-not$same){throw 'Focused Working evidence checks failed.'}
