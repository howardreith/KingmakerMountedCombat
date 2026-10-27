[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ReaderPath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory);if(-not$out.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'Fresh private reader output required.'}
$base=Join-Path $PSScriptRoot 'connection/unpatch-retry'
$common=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeHarness.Common.ps1';$manifest=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeArtifactManifestEvidence.ps1'
$paths=@($PSCommandPath,[IO.Path]::GetFullPath($ReaderPath),$common,$manifest,(Join-Path $base 'request.json'),(Join-Path $base 'working-load-deserialization-evidence.json'),(Join-Path $base 'working-load-deserialization-pending.json'))
$pins=@($paths|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
function F([string]$path,[string]$name){$t=$null;$e=$null;$a=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$t,[ref]$e);if($e.Count){throw 'Parse failure.'};$n=@($a.FindAll({param($x)$x-is[Management.Automation.Language.FunctionDefinitionAst]-and$x.Name-ceq$name},$true));if($n.Count-ne1){throw 'Missing exact helper.'};$n[0].Extent.Text}
. ([ScriptBlock]::Create((F $common 'Assert-KmcExactProperties')));. ([ScriptBlock]::Create((F $manifest 'Test-KmcManifestJsonInteger')));. $ReaderPath
$request=Get-Content (Join-Path $base 'request.json') -Raw|ConvertFrom-Json;$final=Get-Content (Join-Path $base 'working-load-deserialization-evidence.json') -Raw|ConvertFrom-Json;$pending=Get-Content (Join-Path $base 'working-load-deserialization-pending.json') -Raw|ConvertFrom-Json
$pass=0;$fail=0;$rows=[Collections.Generic.List[string]]::new()
function Check([string]$name,[scriptblock]$body){try{&$body;$script:pass++;$script:rows.Add('PASS '+$name)}catch{$script:fail++;$script:rows.Add('FAIL '+$name+': '+$_.Exception.Message)}}
function Clone($value){$value|ConvertTo-Json -Depth 30 -Compress|ConvertFrom-Json}
function Bad([string]$name,$source,[scriptblock]$mutate,[bool]$isPending=$false){Check $name {$a=Clone $source;&$mutate $a;$caught=$false;try{Assert-KmcWorkingLoadDeserializationValue $a $request -Pending:$isPending}catch{$caught=$true};if(-not$caught){throw 'Unexpected acceptance.'}}}
Check 'actual final after unpatch retry' {Assert-KmcWorkingLoadDeserializationValue $final $request}
Check 'actual pending before unpatch retry' {Assert-KmcWorkingLoadDeserializationValue $pending $request -Pending}
Bad 'cross-scene player replacement' $final {$args[0].observations.events[8].scene.playerIdentity=127}
Bad 'cross-scene party replacement with consistent row labels' $final {$e=$args[0].observations.events[8];$e.valueIdentity=127;$e.scene.sceneIdentity=127;$e.scene.currentSceneIdentity=127}
Bad 'pending claims observer release' $pending {$o=$args[0].observations;$o.observerReleased=$true;$o.retainedNativeIdentityCount=0;$o.nativeBusyAtCapture=$false;$o.taskSucceeded=$true;$o.taskSettled=$true} $true

$readFailure=Clone $final;$readFailure.observations.events[8].scene=$null;$readFailure.observations.faults=@('ReadFailure')
Check 'cross-scene read failure retains a valid returned Party identity' {Assert-KmcWorkingLoadDeserializationValue $readFailure $request}
Bad 'cross-scene read failure cannot replace returned Party identity' $readFailure {$args[0].observations.events[8].valueIdentity=127}

$boundaryReadFailure=Clone $final;$boundaryReadFailure.observations.events[9].scene=$null;$boundaryReadFailure.observations.events[10].scene=$null;$boundaryReadFailure.observations.faults=@('ReadFailure')
Check 'before-after null snapshot read failures retain exact event identity' {Assert-KmcWorkingLoadDeserializationValue $boundaryReadFailure $request}
Bad 'after null snapshot cannot replace exact before scene identity' $boundaryReadFailure {$args[0].observations.events[10].valueIdentity=127}
[void][IO.Directory]::CreateDirectory($out);Copy-Item -LiteralPath $ReaderPath -Destination (Join-Path $out 'Reader.ps1');Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $out 'Test.ps1')
$same=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$rows.Add("WORKING LOAD READER BINDINGS PASS=$pass FAIL=$fail");[IO.File]::WriteAllLines((Join-Path $out 'checks.log'),$rows)
[ordered]@{schemaVersion=1;status=$(if($fail-eq0-and$same){'PASS'}else{'FAIL'});passed=$pass;failed=$fail;inputPins=$pins;inputsUnchanged=$same;nativeInvocation=$false;archiveOperation=$false}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $out 'receipt.json') -Encoding UTF8
$rows;if($fail-ne0-or-not$same){throw 'Working reader binding tests failed.'}
