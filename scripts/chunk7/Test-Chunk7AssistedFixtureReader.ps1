[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if([string]::IsNullOrWhiteSpace($RepositoryRoot)){throw 'RepositoryRoot must not be blank.'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if($PSBoundParameters.ContainsKey('SourceOverlayRoot')){
 if([string]::IsNullOrWhiteSpace($SourceOverlayRoot)){throw 'SourceOverlayRoot must not be blank.'}
 $SourceOverlayRoot=[IO.Path]::GetFullPath($SourceOverlayRoot)
}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$analysisRoot=Join-Path (Get-KmcLabRoot) 'analysis-cache'
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path $analysisRoot ('chunk7-assisted-reader-'+[Guid]::NewGuid().ToString('N'))}
if([string]::IsNullOrWhiteSpace($OutputRoot)){throw 'OutputRoot must not be blank.'}
$OutputRoot=Assert-KmcChildPath $OutputRoot $analysisRoot 'generated assisted reader checks'
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve earlier assisted reader output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{};$stages=[Collections.Generic.List[object]]::new();$generated=[Collections.Generic.List[object]]::new()
$clock=[Diagnostics.Stopwatch]::StartNew();$failure=$null;$changed=@();$complete=$false
function Pin-ReaderInput([string]$Path){
 $p=[IO.Path]::GetFullPath($Path);Assert-KmcNotReparsePoint $p 'reader input';Assert-KmcNotHardLink $p 'reader input'
 if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw ('Missing reader input: '+$p)}
 $inputs[$p]=(Get-KmcSha256 $p);return $p
}
function Reader-Source([string]$Relative){
 $p=Join-Path $RepositoryRoot $Relative
 if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$o=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $o -PathType Leaf){$p=$o}}
 return (Pin-ReaderInput $p)
}
function Pin-Generated([string]$Path){
 $p=Assert-KmcChildPath $Path $OutputRoot 'generated reader input';Assert-KmcNotReparsePoint $p 'generated reader input';Assert-KmcNotHardLink $p 'generated reader input'
 $i=Get-Item -LiteralPath $p
 if($i.PSIsContainer-or$i.Length-lt1-or$i.Length-gt16777216){throw 'Generated reader input must be a nonempty file within 16 MiB.'}
 return [pscustomobject]@{path=$p;sha256=(Get-KmcSha256 $p);length=$i.Length}
}
function Read-Generated([string]$Path){
 $pin=Pin-Generated $Path;$generated.Add($pin)
 $text=[IO.File]::ReadAllText($pin.path);Assert-KmcJsonObjectMembersUnique $text 'generated reader input'
 return ($text|ConvertFrom-Json)
}
function Require-PassCount($Value,[string]$Kind){
 if($null-eq$Value-or$Value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'-or$Value.kind-isnot[string]-or$Value.kind-cne$Kind-or$Value.status-isnot[string]-or$Value.status-cne'PASS'){throw ('Generated stage receipt is not PASS '+$Kind)}
 foreach($name in @('passed','failed')){if($Value.$name-isnot[int]-and$Value.$name-isnot[long]){throw 'Generated stage count is not an integer.'}}
 if($Value.passed-lt1-or$Value.failed-ne0){throw 'Generated stage did not pass its complete selected checks.'}
}
function Require-PinnedOutput($Row,[string]$Path){
 $pin=Pin-Generated $Path
 if($null-eq$Row-or$Row.path-isnot[string]-or$Row.sha256-isnot[string]-or$Row.sha256-cnotmatch'^[0-9a-f]{64}$'-or-not[string]::Equals([IO.Path]::GetFullPath($Row.path),$pin.path,[StringComparison]::OrdinalIgnoreCase)-or$Row.sha256-cne$pin.sha256){throw 'Generated producer output pin differs.'}
 if($null-ne$Row.PSObject.Properties['length']-and($Row.length-isnot[int]-and$Row.length-isnot[long]-or$Row.length-ne$pin.length)){throw 'Generated producer output length differs.'}
 $generated.Add($pin);return $pin
}
function Save-ReaderManifest([string]$Leaf,$Value){
 $p=Join-Path $OutputRoot $Leaf
 $stream=[IO.File]::Open($p,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 try{$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($Value|ConvertTo-Json -Depth 20));$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
 $pin=Pin-Generated $p;$generated.Add($pin);return $pin.path
}
function Invoke-ReaderStage([string]$Name,[string]$Script,[string[]]$Arguments){
 $stageRoot=Join-Path $OutputRoot ('process-'+$Name);[void][IO.Directory]::CreateDirectory($stageRoot)
 $argv=@('-NoProfile','-NonInteractive','-File',$Script)+$Arguments
 foreach($arg in $argv){if($null-eq$arg-or$arg.Contains('"')-or$arg.Contains([string][char]0)-or$arg.Contains([string][char]10)-or$arg.Contains([string][char]13)){throw 'Unsupported generated stage argument.'}}
 $psi=[Diagnostics.ProcessStartInfo]::new();$psi.FileName=$script:readerPowerShell;$psi.Arguments=(@($argv|ForEach-Object{'"'+$_+('\'*($_.Length-$_.TrimEnd([char]'\').Length))+'"'})-join' ');$psi.WorkingDirectory=$stageRoot
 $psi.UseShellExecute=$false;$psi.CreateNoWindow=$true;$psi.WindowStyle=[Diagnostics.ProcessWindowStyle]::Hidden;$psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true
 $psi.EnvironmentVariables['TEMP']=$stageRoot;$psi.EnvironmentVariables['TMP']=$stageRoot
 $p=[Diagnostics.Process]::new();$p.StartInfo=$psi;$timer=[Diagnostics.Stopwatch]::StartNew();$exitCode=$null;$pidValue=$null;$started=$null;$stdout='';$stderr='';$problem=$null
 try{
  if(-not$p.Start()){throw 'Generated reader child did not start.'};$pidValue=$p.Id;$started=$p.StartTime.ToUniversalTime().ToString('o')
  $out=$p.StandardOutput.ReadToEndAsync();$err=$p.StandardError.ReadToEndAsync();$p.WaitForExit()
  $stdout=$out.GetAwaiter().GetResult();$stderr=$err.GetAwaiter().GetResult();$exitCode=$p.ExitCode
 }catch{$problem=$_.Exception.Message}finally{$p.Dispose()}
 $stdoutPath=Join-Path $stageRoot 'stdout.log';$stderrPath=Join-Path $stageRoot 'stderr.log'
 [IO.File]::WriteAllText($stdoutPath,$stdout,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText($stderrPath,$stderr,[Text.UTF8Encoding]::new($false))
 $stages.Add([pscustomobject]@{name=$Name;executable=$script:readerPowerShell;argumentList=$argv;cwd=$stageRoot;processId=$pidValue;startedUtc=$started;exitCode=$exitCode;durationSeconds=$timer.Elapsed.TotalSeconds;stdoutPath=$stdoutPath;stderrPath=$stderrPath;stdoutSha256=(Get-KmcSha256 $stdoutPath);stderrSha256=(Get-KmcSha256 $stderrPath);error=$problem})
 if($null-ne$problem-or$exitCode-isnot[int]-or$exitCode-ne0-or$stderr.Length-ne0){throw ('Generated reader stage failed: '+$Name+' / '+$problem+' / exit '+$exitCode)}
}
function Require-TestReceipt([string]$Path,[string]$Kind){
 $r=Read-Generated $Path;Require-PassCount $r $Kind
 if($r.cases-isnot[Array]-or$r.cases.Count-ne$r.passed-or@($r.cases|Where-Object{$_.status-isnot[string]-or$_.status-cne'PASS'}).Count-ne0-or$r.nativeInvoked-isnot[bool]-or$r.nativeInvoked-or$r.installedOrProtectedArchiveReads-ne0){throw 'Generated reader case receipt is incomplete or outside scope.'}
 if($null-ne$r.PSObject.Properties['inputPinsVerifiedAfter']-and($r.inputPinsVerifiedAfter-isnot[bool]-or-not$r.inputPinsVerifiedAfter)){throw 'Generated reader input pins changed.'}
 return $r
}
try{
 [void](Pin-ReaderInput $PSCommandPath)
 # The running Windows platform executable may be an OS hardlink. It is a
 # hashed tool input, not a fixture claiming independent file identity.
 $script:readerPowerShell=[IO.Path]::GetFullPath((Join-Path $PSHOME 'powershell.exe'));$inputs[$script:readerPowerShell]=(Get-KmcSha256 $script:readerPowerShell)
 if($PSVersionTable.PSEdition-cne'Desktop'-or$PSVersionTable.PSVersion.Major-ne5){throw 'This portable entry requires Windows PowerShell 5.'}
 foreach($p in @(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -File -Filter '*.ps1')){[void](Pin-ReaderInput $p.FullName)}
 [void](Pin-ReaderInput (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryEvidence.ps1'))
 $emitter=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedFixtureEvidenceEmitter.ps1'
 $hostProducer=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedFixtureHost.ps1'
 $setupTest=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedSetupEvidence.ps1'
 $preservationTest=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedPreservationEvidence.ps1'
 $compositionTest=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedCompositionEvidence.ps1'
 $outputCompositionTest=Reader-Source 'scripts/chunk7/Test-Chunk7AssistedOutputComposition.ps1'
 $fixtures=Reader-Source 'scripts/runtime/Chunk7AssistedFixtureFixtures.ps1'
 $builder=Reader-Source 'tests/Chunk7Diagnostics/AssistedCompositionFixtures.ps1'
 $reader=Reader-Source 'scripts/runtime/Chunk7AssistedFixtureEvidence.ps1'
 $choice=Reader-Source 'scripts/runtime/Chunk7OrdinaryChoiceEvidence.ps1'
 $coreRoot=Join-Path $OutputRoot 'core-emitter';$hostRoot=Join-Path $OutputRoot 'host-emitter'
 $overlayArgs=@();if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$overlayArgs=@('-SourceOverlayRoot',$SourceOverlayRoot)}
 Invoke-ReaderStage 'core' $emitter (@('-RepositoryRoot',$RepositoryRoot,'-OutputRoot',$coreRoot)+$overlayArgs)
 $coreManifestPath=Join-Path $coreRoot 'manifest.json';$core=Read-Generated $coreManifestPath
 Require-PassCount $core 'actual-assisted-setup-portable-emitter'
 if($core.passed-ne6-or$core.fixtures-isnot[Array]-or$core.fixtures.Count-ne6-or$core.changedInputs-isnot[Array]-or$core.changedInputs.Count-ne0-or$core.nativeEngineInvoked-isnot[bool]-or$core.nativeEngineInvoked-or$core.archiveOperations-ne0){throw 'Actual core emitter receipt differs.'}
 $corePins=@();$coreLeaves=@('ready-noop.json','click-xp-funds.json','xp-funds-sufficient.json','skip-disabled-first.json','system-button.json','partial-native-failure.json')
 foreach($leaf in $coreLeaves){$rows=@($core.fixtures|Where-Object{$_.leaf-is[string]-and$_.leaf-ceq$leaf});if($rows.Count-ne1){throw 'Core fixture leaf is missing or duplicated.'};$corePins+=Require-PinnedOutput $rows[0] (Join-Path $coreRoot $leaf)}
 Invoke-ReaderStage 'host' $hostProducer (@('-RepositoryRoot',$RepositoryRoot,'-OutputDirectory',$hostRoot)+$overlayArgs)
 $hostReceiptPath=Join-Path $hostRoot 'receipt.json';$hostEvidence=Read-Generated $hostReceiptPath
 Require-PassCount $hostEvidence 'actual-assisted-fixture-host'
 if($hostEvidence.compileExit-ne0-or$hostEvidence.loaderCompileExit-ne0-or$hostEvidence.exitCode-ne0-or$hostEvidence.resultShapeValid-isnot[bool]-or-not$hostEvidence.resultShapeValid-or$hostEvidence.changedInputs-isnot[Array]-or$hostEvidence.changedInputs.Count-ne0-or$hostEvidence.nativeInvoked-isnot[bool]-or$hostEvidence.nativeInvoked-or$hostEvidence.archivesRead-ne0-or$hostEvidence.outputs-isnot[Array]){throw 'Actual host emitter receipt differs.'}
 $hostPins=@();foreach($leaf in @('host-fragments-positive-xp.json','host-fragments-zero-xp.json')){
  $p=Join-Path (Join-Path $hostRoot 'fragments') $leaf;$rows=@($hostEvidence.outputs|Where-Object{$_.path-is[string]-and[string]::Equals([IO.Path]::GetFullPath($_.path),$p,[StringComparison]::OrdinalIgnoreCase)})
  if($rows.Count-ne1){throw 'Host fixture leaf is missing or duplicated.'};$hostPins+=Require-PinnedOutput $rows[0] $p
 }
 # These adapters change only manifest shape and path pins. The seven actual
 # formatter output byte streams and all existing assertion scripts stay unchanged.
 $setupManifest=Save-ReaderManifest 'setup-fixtures.json' ([ordered]@{kind='actual-assisted-setup-emitter-fixtures';outputs=$corePins;expected=$core.expected;producer=Pin-Generated $coreManifestPath})
 $preservationManifest=Save-ReaderManifest 'preservation-fixtures.json' ([ordered]@{kind='actual-assisted-host-preservation-fixtures';outputs=$hostPins;producer=Pin-Generated $hostReceiptPath})
 $compositionManifest=Save-ReaderManifest 'composition-fixtures.json' ([ordered]@{kind='controlled-assisted-complete-fixtures';host=$hostPins[0];core=$corePins[0];builder=[pscustomobject]@{path=$builder;sha256=$inputs[$builder];length=(Get-Item -LiteralPath $builder).Length};choiceHelper=[pscustomobject]@{path=$choice;sha256=$inputs[$choice];length=(Get-Item -LiteralPath $choice).Length};fixtures=[pscustomobject]@{path=$fixtures;sha256=$inputs[$fixtures];length=(Get-Item -LiteralPath $fixtures).Length};limits=@('Generated whole composition explicitly substitutes native probes/profile/authority/XP/progression/frame boundaries.','No native fixture or ordinary acquisition qualification.')})
 $testArgs=@('-RepositoryRoot',$RepositoryRoot,'-ReaderPath',$reader)
 Invoke-ReaderStage 'setup' $setupTest ($testArgs+@('-FixtureManifestPath',$setupManifest,'-OutputRoot',(Join-Path $OutputRoot 'setup')))
 $setupResult=Require-TestReceipt (Join-Path $OutputRoot 'setup/receipt.json') 'assisted-setup-evidence-focused-checks'
 Invoke-ReaderStage 'preservation' $preservationTest ($testArgs+@('-FixtureManifestPath',$preservationManifest,'-OutputRoot',(Join-Path $OutputRoot 'preservation')))
 $preservationResult=Require-TestReceipt (Join-Path $OutputRoot 'preservation/receipt.json') 'actual-assisted-preservation-focused-checks'
 Invoke-ReaderStage 'composition' $compositionTest ($testArgs+@('-FixtureManifestPath',$compositionManifest,'-OutputRoot',(Join-Path $OutputRoot 'composition')))
 $compositionResult=Require-TestReceipt (Join-Path $OutputRoot 'composition/receipt.json') 'assisted-generated-composition'
 Invoke-ReaderStage 'output-composition' $outputCompositionTest ($testArgs+@('-FixtureManifestPath',$compositionManifest,'-OutputRoot',(Join-Path $OutputRoot 'output-composition')))
 $outputCompositionResult=Require-TestReceipt (Join-Path $OutputRoot 'output-composition/receipt.json') 'generated-assisted-full-output-composition'
 # Producer receipts pin exact compiled sources/dependencies. Recheck those
 # inputs after all consumers as well as the selected scripts/runtime helpers.
 foreach($producer in @($core,$hostEvidence)){foreach($pin in $producer.inputs){if($pin.path-isnot[string]-or$pin.sha256-isnot[string]-or(Get-KmcSha256 $pin.path)-cne$pin.sha256){throw 'Actual producer input changed after reader checks.'}}}
 foreach($pin in $generated){if((Get-Item -LiteralPath $pin.path).Length-ne$pin.length-or(Get-KmcSha256 $pin.path)-cne$pin.sha256){throw 'Generated fixture/manifest changed during reader checks.'}}
 $complete=$true
}catch{$failure=$_.Exception.Message;Write-Host ('FAIL assisted reader entry: '+$failure)}
$changed=@($inputs.Keys|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)-or(Get-KmcSha256 $_)-cne$inputs[$_]})
$ok=$complete-and$null-eq$failure-and$changed.Count-eq0-and$stages.Count-eq6
$results=@();if($complete){$results=@([ordered]@{name='core-emitter';passed=$core.passed;failed=$core.failed},[ordered]@{name='host-emitter';passed=$hostEvidence.passed;failed=$hostEvidence.failed},[ordered]@{name='setup';passed=$setupResult.passed;failed=$setupResult.failed},[ordered]@{name='preservation';passed=$preservationResult.passed;failed=$preservationResult.failed},[ordered]@{name='composition';passed=$compositionResult.passed;failed=$compositionResult.failed},[ordered]@{name='output-composition';passed=$outputCompositionResult.passed;failed=$outputCompositionResult.failed})}
$receipt=[ordered]@{schemaVersion=1;kind='assisted-reader-portable-composition';status=$(if($ok){'PASS'}else{'FAIL'});error=$failure;durationSeconds=$clock.Elapsed.TotalSeconds;producerInvocations=[ordered]@{core=@($stages|Where-Object name -CEQ 'core').Count;host=@($stages|Where-Object name -CEQ 'host').Count};stages=$stages.ToArray();results=$results;inputPins=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}});changedInputs=$changed;generatedPins=$generated.ToArray();nativeInvoked=$false;installedOrProtectedArchiveReads=0;limits=@('Core and host tests each run once; four reader scripts reuse those generated outputs; Output/Archive open only freshly generated ZIPs.','Whole-composition positive evidence is controlled and includes declared external native boundary substitutions; never native qualification.','Outer output assumes cooperative exclusive private-directory ownership, not continuous namespace custody.')}
[void](Save-ReaderManifest 'receipt.json' $receipt)
Write-Host ('ASSISTED READER PORTABLE: '+$(if($ok){'PASS'}else{'FAIL'}))
if(-not$ok){exit 1};exit 0
