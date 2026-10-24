[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ReaderRoot,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$out=[IO.Path]::GetFullPath($OutputDirectory);if(Test-Path -LiteralPath $out){throw 'Fresh generated reader output required.'}
$runtime=[IO.Path]::GetFullPath($ReaderRoot)
$base=Join-Path $PSScriptRoot 'connection/unpatch-retry'
$finalPath=Join-Path $PSScriptRoot 'connection/complete-observation/working-load-deserialization-evidence.json'
$common=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeHarness.Common.ps1';$manifest=Join-Path $PSScriptRoot 'source-tree/scripts/runtime/RuntimeArtifactManifestEvidence.ps1'
$reader=Join-Path $runtime 'WorkingLoadDeserializationEvidence.ps1';$gameReader=Join-Path $runtime 'Test-RuntimeGameResult.ps1'
$paths=@($PSCommandPath,$reader,$gameReader,$common,$manifest,(Join-Path $base 'request.json'),$finalPath,(Join-Path $base 'working-load-deserialization-pending.json'))
$pins=@($paths|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})
function F([string]$path,[string]$name){$t=$null;$e=$null;$a=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$t,[ref]$e);if($e.Count){throw 'Parse failure.'};$n=@($a.FindAll({param($x)$x-is[Management.Automation.Language.FunctionDefinitionAst]-and$x.Name-ceq$name},$true));if($n.Count-ne1){throw 'Missing exact helper.'};$n[0].Extent.Text}
foreach($name in @('Assert-KmcExactProperties','Read-KmcJson','Assert-KmcNotReparsePoint','Assert-KmcNotHardLink')){. ([ScriptBlock]::Create((F $common $name)))}
foreach($name in @('Test-KmcManifestJsonInteger','Assert-KmcManifestJsonMembersUnique')){. ([ScriptBlock]::Create((F $manifest $name)))}
. $reader
$text=[IO.File]::ReadAllText($gameReader);$start=$text.IndexOf('Assert-KmcReadOnlyArtifactManifest $request $game.evidenceManifestSha256 -GameResult');if($start-lt0){throw 'Original manifest gate absent.'};$start=$text.IndexOf("`n",$start)+1;$end=$text.IndexOf('if ([int]$game.loadRequestCount',$start);if($end-le$start){throw 'Exact sidecar dispatch extent absent.'}
$dispatch=[ScriptBlock]::Create('param($request,$game,$PSScriptRoot)'+"`n"+$text.Substring($start,$end-$start))
$request=Read-KmcJson (Join-Path $base 'request.json');$final=Read-KmcJson $finalPath;$pending=Read-KmcJson (Join-Path $base 'working-load-deserialization-pending.json')
[void][IO.Directory]::CreateDirectory($out);Copy-Item -LiteralPath $reader -Destination (Join-Path $out 'Reader.ps1');Copy-Item -LiteralPath $gameReader -Destination (Join-Path $out 'GameReader.ps1');Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $out 'Test.ps1')
$pass=0;$fail=0;$rows=[Collections.Generic.List[string]]::new();$caseNumber=0
function Check([string]$name,[scriptblock]$assertion){try{&$assertion;$script:pass++;$script:rows.Add('PASS '+$name)}catch{$script:fail++;$script:rows.Add('FAIL '+$name+': '+$_.Exception.Message)}}
function Clone($value){$value|ConvertTo-Json -Depth 30 -Compress|ConvertFrom-Json}
function Refuse([scriptblock]$action){$caught=$false;try{&$action|Out-Null}catch{$caught=$true};if(-not$caught){throw 'Unexpected acceptance.'}}
function GeneratedCase([bool]$selected,$finalValue,$pendingValue,[string]$status){
    $script:caseNumber++;$dir=Join-Path $out ('case'+$script:caseNumber);[void][IO.Directory]::CreateDirectory($dir)
    $r=Clone $request;$r.evidenceRoot=$dir;if(-not$selected){$r.PSObject.Properties.Remove('observeWorkingDeserialization')}
    if($null-ne$finalValue){[IO.File]::WriteAllText((Join-Path $dir 'working-load-deserialization-evidence.json'),($finalValue|ConvertTo-Json -Depth 30 -Compress),[Text.UTF8Encoding]::new($false))}
    if($null-ne$pendingValue){[IO.File]::WriteAllText((Join-Path $dir 'working-load-deserialization-pending.json'),($pendingValue|ConvertTo-Json -Depth 30 -Compress),[Text.UTF8Encoding]::new($false))}
    &$dispatch $r ([pscustomobject]@{status=$status}) $runtime
}
Check 'old no-opt-in no-sidecar remains accepted' {GeneratedCase $false $null $null 'PASS'}
Check 'selected final producer output accepted' {GeneratedCase $true $final $null 'PASS'}
Check 'selected pending producer output retained as FAIL' {GeneratedCase $true $null $pending 'FAIL'}
Check 'selected pending then final outputs both accepted' {GeneratedCase $true $final $pending 'PASS'}
Check 'unselected final sidecar refused' {Refuse {GeneratedCase $false $final $null 'PASS'}}
Check 'unselected pending sidecar refused' {Refuse {GeneratedCase $false $null $pending 'FAIL'}}
Check 'malformed pending-only released claim refused' {$p=Clone $pending;$p.observations.observerReleased=$true;$p.observations.retainedNativeIdentityCount=0;$p.observations.nativeBusyAtCapture=$false;Refuse {GeneratedCase $true $null $p 'FAIL'}}
Check 'malformed pending alongside valid final refused' {$p=Clone $pending;$p.observations.formatVersion=2;Refuse {GeneratedCase $true $final $p 'PASS'}}
Check 'selected PASS without final refused' {Refuse {GeneratedCase $true $null $pending 'PASS'}}
Check 'selected early FAIL without observation remains accepted' {GeneratedCase $true $null $null 'FAIL'}
$early=Clone $final;$early.status='FAIL';$early.observations.runtimeStatusAtPublication='FAIL';$early.observations.nativeLoadCount=0;$early.observations.afterLoadCallbackObserved=$false;$early.observations.fixtureIdentityVerified=$false;$early.observations.loaderObserved=$false;$early.observations.taskObserved=$false;$early.observations.taskSettled=$false;$early.observations.taskSucceeded=$false;$early.observations.events=@();$early.observations.faults=@()
Check 'generated zero-load partial-install released FAIL accepted' {Assert-KmcWorkingLoadDeserializationValue $early $request}
Check 'issued load missing all hooks cannot be a released final' {$issued=Clone $early;$issued.observations.nativeLoadCount=1;Refuse {Assert-KmcWorkingLoadDeserializationValue $issued $request}}
$same=$true;foreach($pin in $pins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$same=$false}}
$rows.Add("WORKING LOAD READER DISPATCH PASS=$pass FAIL=$fail");[IO.File]::WriteAllLines((Join-Path $out 'checks.log'),$rows)
[ordered]@{schemaVersion=1;status=$(if($fail-eq0-and$same){'PASS'}else{'FAIL'});passed=$pass;failed=$fail;inputPins=$pins;inputsUnchanged=$same;boundaries='Actual new reader and exact original game-reader sidecar-dispatch extent on generated JSON. Real existing JSON/member/link guards execute. The unchanged generic manifest and whole game-result validator are not executed here; separate producer/manifest checks cover their selected additions. Early final is a deliberately constructed reader neighbor, not a native observation.';nativeInvocation=$false;archiveOperation=$false}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $out 'receipt.json') -Encoding UTF8
$rows;if($fail-ne0-or-not$same){throw 'Working reader dispatch tests failed.'}
