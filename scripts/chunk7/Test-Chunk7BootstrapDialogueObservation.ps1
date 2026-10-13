[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-bootstrap-dialogue-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing bootstrap dialogue output.'}
[void][IO.Directory]::CreateDirectory($OutputRoot)
$inputs=@{};$compositionEntries=@{}
function Pin([string]$path){$inputs[$path]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant();return $path}
[void](Pin $PSCommandPath)
if(-not[string]::IsNullOrWhiteSpace($SourceCompositionPath)){
    [void](Pin $SourceCompositionPath)
    foreach($entry in @((Get-Content -LiteralPath $SourceCompositionPath -Raw|ConvertFrom-Json).files)){
        if($compositionEntries.ContainsKey($entry.path)){throw ('Duplicate composition source '+$entry.path)}
        $compositionEntries[$entry.path]=$entry
    }
}
function Source([string]$relative){
    $path=$null
    if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$candidate=Join-Path $SourceOverlayRoot $relative;if(Test-Path -LiteralPath $candidate -PathType Leaf){$path=$candidate}}
    if($null-eq$path-and$compositionEntries.ContainsKey($relative)){$entry=$compositionEntries[$relative];$path=$entry.sourcePath;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$entry.sha256){throw ('Changed composition input '+$relative)}}
    if($null-eq$path){$path=Join-Path $RepositoryRoot $relative}
    return (Pin $path)
}
function Member([string]$text,[string]$signature){
    $start=$text.IndexOf($signature,[StringComparison]::Ordinal)
    if($start-lt0-or$text.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw ('Missing/ambiguous exact host member '+$signature)}
    $open=$text.IndexOf('{',$start);$depth=0
    for($i=$open;$i-lt$text.Length;$i++){if($text[$i]-eq'{'){$depth++}elseif($text[$i]-eq'}'){$depth--;if($depth-eq0){return $text.Substring($start,$i-$start+1)}}}
    throw ('Unclosed exact host member '+$signature)
}

$observerPath=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7BootstrapDialogueObservation.cs'
$hostPath=Source 'src/KingmakerMountedCombat/Diagnostics/RuntimeAutomationHost.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/BootstrapDialogueObservationTemplate.cs'
$identityPath=Source 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$readerPath=Source 'scripts/runtime/Chunk7NativeBootstrapEvidence.ps1'
$gameReaderPath=Source 'scripts/runtime/Test-RuntimeGameResult.ps1'
$commonPath=Source 'scripts/runtime/RuntimeHarness.Common.ps1'
$fixturePath=Source 'tests/Chunk7Diagnostics/NativeBootstrapEvidenceFixture.json'
# Common loads function definitions only. Pin its finite sibling script surface
# before import; this test never invokes a runtime launcher or archive operation.
foreach($path in Get-ChildItem -LiteralPath (Split-Path $commonPath -Parent) -Filter '*.ps1' -File){[void](Pin $path.FullName)}
. $commonPath
. $readerPath
$observerText=[IO.File]::ReadAllText($observerPath);$hostText=[IO.File]::ReadAllText($hostPath);$template=[IO.File]::ReadAllText($templatePath)
$start=$observerText.IndexOf('        internal const string Contract',[StringComparison]::Ordinal);$end=$observerText.IndexOf('        // The only native boundary',[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Exact recorder slice changed.'}
$actual=$template.Replace('__ACTUAL_RECORDER__',$observerText.Substring($start,$end-$start)).Replace('__ACTUAL_RAW_STATE__',(Member $observerText 'internal sealed class RawState')).Replace('__ACTUAL_JSON_SETTINGS__',((Member $hostText 'private static readonly JsonSerializerSettings JsonSettings')+';')).Replace('__ACTUAL_RESULT_DTO__',(Member $hostText 'private sealed class RuntimeGameResultV2')).Replace('__ACTUAL_NATIVE_READ__',((Member $observerText 'private static RawState ReadNative(').Replace('ReadNative(','ReadActualNative('))).Replace('__ACTUAL_GUID_READ__',(Member $observerText 'private static string GuidOf('))
if($actual.Contains('__ACTUAL_')){throw 'Unresolved passive recorder template.'}
$actualPath=Join-Path $OutputRoot 'actual.cs';[IO.File]::WriteAllText($actualPath,$actual)
[IO.File]::Copy($observerPath,(Join-Path $OutputRoot 'source.cs'),$false);[IO.File]::Copy($templatePath,(Join-Path $OutputRoot 'template.cs'),$false);[IO.File]::Copy($hostPath,(Join-Path $OutputRoot 'host.cs'),$false)
$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$referenceRoot='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib','System','System.Core','System.Xml','System.Runtime.Serialization'|ForEach-Object{Pin (Join-Path $referenceRoot ($_+'.dll'))})
$json=Pin 'C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputRoot 'Actual.exe';$rsp=Join-Path $OutputRoot 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/nowarn:0649,0169,0414','/nostdlib+','/deterministic+',('/out:'+$exe),('/reference:'+$json))+@($references|ForEach-Object{'/reference:'+$_})+@($actualPath,$identityPath)
[IO.File]::WriteAllText($rsp,(($arguments|ForEach-Object{'"'+$_+'"'})-join[Environment]::NewLine))
$loaderPath=Join-Path $OutputRoot 'loader.cs'
# In-place exact serializer resolution matches the installed DTO contract.
# No proprietary binary is copied; Unity/Game methods remain inert facades.
$loader='using System;using System.Reflection;static class Loader { public static int Main(string[] args){if(args.Length!=1)throw new InvalidOperationException("Expected one private output folder");var json=Assembly.LoadFrom(@"'+$json+'");AppDomain.CurrentDomain.AssemblyResolve+=(sender,eventArgs)=>eventArgs.Name==json.FullName?json:null;var tested=Assembly.LoadFrom(System.IO.Path.Combine(args[0],"Actual.exe"));return (int)tested.EntryPoint.Invoke(null,new object[]{new string[]{args[0]}});}}'
[IO.File]::WriteAllText($loaderPath,$loader)
$loaderExe=Join-Path $OutputRoot 'Loader.exe';$loaderRsp=Join-Path $OutputRoot 'loader.rsp'
[IO.File]::WriteAllText($loaderRsp,((@('/nologo','/target:exe','/langversion:7.3','/nostdlib+',('/out:'+$loaderExe),('/reference:'+$references[0]),$loaderPath)|ForEach-Object{'"'+$_+'"'})-join[Environment]::NewLine))
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=0
$compileOutput=@(& $compiler ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE
$global:LASTEXITCODE=0;$loaderCompileOutput=@(& $compiler ('@'+$loaderRsp) 2>&1);$loaderCompileExit=$global:LASTEXITCODE;$compileSeconds=$clock.Elapsed.TotalSeconds
[IO.File]::WriteAllText((Join-Path $OutputRoot 'compile.log'),(($compileOutput+$loaderCompileOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$testExit=$null;$testSeconds=0.0;$actualPassed=0;$actualFailed=0;$actualShape=$false;$testOutput=@()
if($compileExit-eq0-and$loaderCompileExit-eq0){
 $clock.Restart();$global:LASTEXITCODE=0;$testOutput=@(& $loaderExe $OutputRoot 2>&1);$testExit=$global:LASTEXITCODE;$testSeconds=$clock.Elapsed.TotalSeconds
 $lines=@($testOutput|ForEach-Object{$_.ToString()});$totals=@($lines|Where-Object{$_-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'})
 if($totals.Count-eq1-and$totals[0]-cmatch'^TOTAL PASS=([0-9]+) FAIL=([0-9]+)$'){$actualPassed=[int]$Matches[1];$actualFailed=[int]$Matches[2];$actualShape=$actualPassed-gt0-and(@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$actualPassed)-and(@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq$actualFailed)}
 $lines|ForEach-Object{Write-Host $_}
}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'run.log'),(($testOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$script:dialoguePass=0;$script:dialogueFail=0
function Good-Dialogue([string]$Name,[scriptblock]$Action){try{&$Action;$script:dialoguePass++;Write-Host ('PASS '+$Name)}catch{$script:dialogueFail++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad-Dialogue([string]$Name,[scriptblock]$Action,[string]$Reason=''){try{&$Action;$script:dialogueFail++;Write-Host ('FAIL accepted '+$Name)}catch{if($Reason-and$_.ToString()-notmatch$Reason){$script:dialogueFail++;Write-Host ('FAIL wrong refusal '+$Name+': '+$_)}else{$script:dialoguePass++;Write-Host ('PASS refuses '+$Name)}}}
function Read-Dialogue([string]$Leaf){return ([IO.File]::ReadAllText((Join-Path $OutputRoot $Leaf))|ConvertFrom-Json)}
if($testExit-eq0-and$actualShape-and$actualFailed-eq0){
 foreach($leaf in @('terminal.json','read-error.json','null-state.json','released-error.json','unity-null-popup.json','absent-popup.json','live-popup.json')){Good-Dialogue ('actual producer '+$leaf){Assert-KmcChunk7BootstrapDialogueObservation (Read-Dialogue $leaf) $true}}
 foreach($leaf in @('scheduled-start.json','scheduled-unmarked.json','scheduled-consumed.json','missing-dialog-state.json')){Good-Dialogue ('actual scheduled-start producer '+$leaf){Assert-KmcChunk7BootstrapDialogueObservation (Read-Dialogue $leaf) $true}}
 Good-Dialogue 'historical raw-v1 shape remains exact' {$v=Read-Dialogue 'terminal.json';$v.contract='chunk7-bootstrap-dialogue-raw-v1';$v.identityCapacity=32;foreach($row in $v.snapshots){foreach($key in @('dialogState','scheduledStart','scheduledStartIsScheduled','scheduledDialog')){$row.PSObject.Properties.Remove($key)}};Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Good-Dialogue 'v2 maximum forty distinct references' {Assert-KmcChunk7BootstrapDialogueObservation (Read-Dialogue 'maximum-distinct.json') $true}
 Bad-Dialogue 'v2 refuses old insufficient capacity' {$v=Read-Dialogue 'terminal.json';$v.identityCapacity=32;Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Bad-Dialogue 'v2 refuses forty-first reference' {$v=Read-Dialogue 'maximum-distinct.json';$v.identityIds+=@(-9999);Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'array bounds changed'
 Bad-Dialogue 'v1 still requires exactly thirty-two capacity' {$v=Read-Dialogue 'terminal.json';$v.contract='chunk7-bootstrap-dialogue-raw-v1';foreach($row in $v.snapshots){foreach($key in @('dialogState','scheduledStart','scheduledStartIsScheduled','scheduledDialog')){$row.PSObject.Properties.Remove($key)}};Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 $scheduledCases=[ordered]@{
  'v2 missing scheduled start'={param($v)$v.snapshots[0].PSObject.Properties.Remove('scheduledStart')}
  'v2 state label singleton'={param($v)$v.snapshots[0].dialogState=@($v.snapshots[0].dialogState)}
  'v2 start label singleton'={param($v)$v.snapshots[0].scheduledStart=@($v.snapshots[0].scheduledStart)}
  'v2 start label outside registry'={param($v)$v.snapshots[0].scheduledStart=-777}
  'v2 queued marker singleton'={param($v)$v.snapshots[0].scheduledStartIsScheduled=@($true)}
  'v2 queued marker text'={param($v)$v.snapshots[0].scheduledStartIsScheduled='true'}
  'v2 missing queued marker'={param($v)$v.snapshots[0].scheduledStartIsScheduled=$null}
  'v2 scheduled blueprint singleton'={param($v)$v.snapshots[0].scheduledDialog=@($v.snapshots[0].scheduledDialog)}
  'v2 scheduled GUID singleton'={param($v)$v.snapshots[0].scheduledDialog.guid=@($v.snapshots[0].scheduledDialog.guid)}
  'v2 absent state retains start'={param($v)$v.snapshots[0].dialogState=0}
  'v2 absent start retains fields'={param($v)$v.snapshots[0].scheduledStart=0}
  'v1 refuses undeclared new fields'={param($v)$v.contract='chunk7-bootstrap-dialogue-raw-v1';$v.identityCapacity=32}
 }
 foreach($entry in $scheduledCases.GetEnumerator()){Bad-Dialogue $entry.Key {$v=Read-Dialogue 'scheduled-start.json';&$entry.Value $v;Assert-KmcChunk7BootstrapDialogueObservation $v $true}}
 Bad-Dialogue 'v2 failed row refuses partial scheduled reference' {$v=Read-Dialogue 'read-error.json';$v.snapshots[0].scheduledStart=-1001;Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'failed read retained partial state'
 Good-Dialogue 'actual preterminal artifact snapshot' {Assert-KmcChunk7BootstrapDialogueObservation (Read-Dialogue 'artifact.json') $false}
 Good-Dialogue 'historical result absence remains accepted' {Assert-KmcChunk7BootstrapDialogueResult (Read-Dialogue 'game-absent.json') ([pscustomobject]@{scenario='legacy-family'})}
 Good-Dialogue 'actual optional bootstrap DTO result' {Assert-KmcChunk7BootstrapDialogueResult (Read-Dialogue 'game-present.json') ([pscustomobject]@{scenario='chunk7-native-main-bootstrap'})}
 Bad-Dialogue 'optional field under foreign result family' {$g=Read-Dialogue 'game-present.json';$g.scenario='legacy-family';Assert-KmcChunk7BootstrapDialogueResult $g ([pscustomobject]@{scenario='legacy-family'})} 'forbidden under another family'
 Bad-Dialogue 'null present optional field' {$g=Read-Dialogue 'game-present.json';$g.bootstrapDialogue=$null;Assert-KmcChunk7BootstrapDialogueResult $g ([pscustomobject]@{scenario='chunk7-native-main-bootstrap'})}
 $cases=[ordered]@{
  'contract singleton'={param($v)$v.contract=@($v.contract)}
  'contract alias'={param($v)$v.contract='other'}
  'success authority assertion'={param($v)$v.rawSnapshotsOnly=$false}
  'unbounded capacity'={param($v)$v.identityCapacity=33}
  'unreleased terminal'={param($v)$v.identityReleased=$false}
  'released text'={param($v)$v.identityReleased='true'}
  'identity outside Int32'={param($v)$v.identityIds[0]=2147483648}
  'identity zero'={param($v)$v.identityIds[0]=0}
  'identity duplicate'={param($v)$v.identityIds[0]=$v.identityIds[1]}
  'identity numeric string'={param($v)$v.identityIds[0]=[string]$v.identityIds[0]}
  'row array absent'={param($v)$v.snapshots=$null}
  'row quota'={param($v)$v.snapshots+=@($v.snapshots[3])}
  'phase singleton'={param($v)$v.snapshots[0].phase=@($v.snapshots[0].phase)}
  'phase reorder'={param($v)$v.snapshots[1].phase='bootstrap-readiness'}
  'frame text'={param($v)$v.snapshots[0].frame='10'}
  'frame rewind'={param($v)$v.snapshots[1].frame=9}
  'read flag numeric'={param($v)$v.snapshots[0].readSucceeded=1}
  'successful row error'={param($v)$v.snapshots[0].readError='unexpected'}
  'object singleton'={param($v)$v.snapshots[0].controller=@($v.snapshots[0].controller)}
  'object unregistered'={param($v)$v.snapshots[0].controller=500}
  'mode singleton'={param($v)$v.snapshots[0].topMode=@('Default')}
  'mode over bound'={param($v)$v.snapshots[0].topMode='x'*65}
  'dialog active text'={param($v)$v.snapshots[0].dialogActive='false'}
  'blueprint shape array'={param($v)$v.snapshots[0].dialog=@($v.snapshots[0].dialog)}
  'blueprint GUID singleton'={param($v)$v.snapshots[0].dialog.guid=@($v.snapshots[0].dialog.guid)}
  'blueprint GUID over bound'={param($v)$v.snapshots[0].dialog.guid='x'*65}
  'null blueprint string'={param($v)$v.snapshots[0].dialog.object=0}
  'absent game with controller'={param($v)$v.snapshots[0].game=0}
  'absent controller with flags'={param($v)$v.snapshots[0].controller=0}
  'schedule singleton'={param($v)$v.snapshots[0].cuePlayScheduled=@($false)}
  'absent popup with state'={param($v)$v.snapshots[0].messageBox=0}
  'popup boolean text'={param($v)$v.snapshots[0].messageBoxShown='false'}
  'popup type string'={param($v)$v.snapshots[0].messageBoxType='0'}
  'errors scalar'={param($v)$v.errors='error'}
  'oversized error'={param($v)$v.errors=@(('x'*257))}
  'new authority property'={param($v)$v|Add-Member -NotePropertyName nativeLoadProven -NotePropertyValue $true}
  'missing terminal without error'={param($v)$v.snapshots=@($v.snapshots[0..2])}
 }
 foreach($entry in $cases.GetEnumerator()){Bad-Dialogue $entry.Key {$v=Read-Dialogue 'terminal.json';&$entry.Value $v;Assert-KmcChunk7BootstrapDialogueObservation $v $true}}
 Bad-Dialogue 'partial failed native row' {$v=Read-Dialogue 'read-error.json';$v.snapshots[0].game=-1001;Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'failed read retained partial state'
 Bad-Dialogue 'missing explicit failed-read error' {$v=Read-Dialogue 'read-error.json';$v.snapshots[0].readError=$null;Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'failed read needs a bounded error'
 Bad-Dialogue 'only popup shown unavailable' {$v=Read-Dialogue 'live-popup.json';$v.snapshots[0].messageBoxShown=$null;Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Bad-Dialogue 'only popup type unavailable' {$v=Read-Dialogue 'live-popup.json';$v.snapshots[0].messageBoxType=$null;Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Bad-Dialogue 'terminal phase in artifact' {$v=Read-Dialogue 'terminal.json';$v.identityReleased=$false;Assert-KmcChunk7BootstrapDialogueObservation $v $false} 'phase order changed'
 Good-Dialogue 'historical full bootstrap artifact remains accepted' {$f=[IO.File]::ReadAllText($fixturePath)|ConvertFrom-Json;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
 Good-Dialogue 'new passive artifact field delegates unchanged bootstrap proof' {$f=[IO.File]::ReadAllText($fixturePath)|ConvertFrom-Json;$f.artifact.observations|Add-Member -NotePropertyName dialogueObservation -NotePropertyValue (Read-Dialogue 'artifact.json');Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact}
 Bad-Dialogue 'malformed observation rejected even on failed bootstrap artifact' {$f=[IO.File]::ReadAllText($fixturePath)|ConvertFrom-Json;$f.artifact.status='FAIL';$f.artifact.observations|Add-Member -NotePropertyName dialogueObservation -NotePropertyValue $null;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact 'FAIL'}
 # Execute only exact real schema-v2 shape and existing final boundary clauses.
 # Full request/manifest/filesystem/native qualification is not selected here.
 $tokens=$null;$parseErrors=$null;$gameAst=[Management.Automation.Language.Parser]::ParseInput([IO.File]::ReadAllText($gameReaderPath),[ref]$tokens,[ref]$parseErrors)
 if($parseErrors.Count-ne0){throw 'Game result reader failed parsing.'}
 $commonAssignment=@($gameAst.FindAll({param($a)$a-is[Management.Automation.Language.AssignmentStatementAst]-and$a.Left.Extent.Text-ceq'$commonRequired'},$true));if($commonAssignment.Count-ne1){throw 'Exact common result shape assignment changed.'}
 $schemaClause=@($gameAst.FindAll({param($a)$a-is[Management.Automation.Language.IfStatementAst]},$true)|ForEach-Object{$_.Clauses}|Where-Object{$_.Item1.Extent.Text-ceq'$schemaVersion -eq 2'});if($schemaClause.Count-ne1){throw 'Exact v2 result clause changed.'}
 $shapeBody=$schemaClause[0].Item2.Extent.Text;$shapeBody=$shapeBody.Substring(1,$shapeBody.Length-2)
 $terminalIf=@($gameAst.FindAll({param($a)$a-is[Management.Automation.Language.IfStatementAst]-and$a.Extent.Text.Contains('Save-backed PASS did not restore its exact game-mode and diagnostic-setting boundary.')},$true)|Sort-Object {$_.Extent.Text.Length}|Select-Object -First 1)
 if($terminalIf.Count-ne1){throw 'Exact terminal result predicate changed.'}
 function Check-DialogueResultShape($game){$request=[pscustomobject]@{scenario='chunk7-native-main-bootstrap'};. ([scriptblock]::Create($commonAssignment[0].Extent.Text));&([scriptblock]::Create($shapeBody))}
 Good-Dialogue 'actual old v2 result property set' {Check-DialogueResultShape (Read-Dialogue 'game-absent.json')}
 Good-Dialogue 'actual new v2 result property set' {Check-DialogueResultShape (Read-Dialogue 'game-present.json')}
 Bad-Dialogue 'actual new v2 shape refuses unrelated extra field' {$g=Read-Dialogue 'game-present.json';$g|Add-Member -NotePropertyName dialogSuccess -NotePropertyValue $true;Check-DialogueResultShape $g}
 Good-Dialogue 'Default terminal predicate remains exact' {$game=Read-Dialogue 'game-present.json';$game.currentGameMode='Default';$game.loadedAreaPresent=$true;$request=[pscustomobject]@{scenario=$game.scenario};&([scriptblock]::Create($terminalIf[0].Extent.Text))}
 Bad-Dialogue 'raw Dialog sample never qualifies final PASS' {$game=Read-Dialogue 'game-present.json';$game.loadedAreaPresent=$true;$request=[pscustomobject]@{scenario=$game.scenario};&([scriptblock]::Create($terminalIf[0].Extent.Text))} 'exact game-mode and diagnostic-setting boundary'
 Bad-Dialogue 'raw snapshot never excuses movement enabled' {$game=Read-Dialogue 'game-present.json';$game.currentGameMode='Default';$game.loadedAreaPresent=$true;$game.movementExperimentEnabled=$true;$request=[pscustomobject]@{scenario=$game.scenario};&([scriptblock]::Create($terminalIf[0].Extent.Text))} 'exact game-mode and diagnostic-setting boundary'
}
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$ok=$compileExit-eq0-and$loaderCompileExit-eq0-and$testExit-eq0-and$actualShape-and$actualFailed-eq0-and$script:dialoguePass-gt0-and$script:dialogueFail-eq0-and$changed.Count-eq0
$inputRows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
$outputRows=@(Get-ChildItem -LiteralPath $OutputRoot -File|Where-Object{$_.Extension-cin@('.json','.cs','.rsp','.log')}|Sort-Object Name|ForEach-Object{[ordered]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{kind='actual-passive-bootstrap-dialogue-regression';status=$(if($ok){'PASS'}else{'FAIL'});compileExit=$compileExit;loaderCompileExit=$loaderCompileExit;exitCode=$testExit;passed=($actualPassed+$script:dialoguePass);failed=($actualFailed+$script:dialogueFail);actualPassed=$actualPassed;actualFailed=$actualFailed;readerPassed=$script:dialoguePass;readerFailed=$script:dialogueFail;resultShapeValid=$actualShape;compileDurationSeconds=$compileSeconds;testDurationSeconds=$testSeconds;inputs=$inputRows;outputs=$outputRows;changedInputs=$changed;substitutions=@('ReadNative and frame/Game inputs are inert facades','Actual installed serializer referenced in place; no proprietary binary copies','Full request, manifest, filesystem and native qualification are not invoked');nativeInvoked=$false;archiveOperations=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Host ('PASSIVE READER PASS='+$script:dialoguePass+' FAIL='+$script:dialogueFail)
if(-not$ok){exit 1};exit 0
