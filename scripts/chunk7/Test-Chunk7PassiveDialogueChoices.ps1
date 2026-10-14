[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$SourceCompositionPath,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputRoot')){$OutputRoot=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-passive-dialogue-choices-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing passive choice output.'}
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
$choicePath=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7DialogueChoiceObservation.cs'
$identityPath=Source 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$scenarioPath=Source 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeBootstrapScenario.cs'
$templatePath=Source 'tests/Chunk7Diagnostics/PassiveDialogueChoicesTemplate.cs'
$readerPath=Source 'scripts/runtime/Chunk7NativeBootstrapEvidence.ps1'
$commonPath=Source 'scripts/runtime/RuntimeHarness.Common.ps1'
$fixturePath=Source 'tests/Chunk7Diagnostics/NativeBootstrapEvidenceFixture.json'
foreach($path in Get-ChildItem -LiteralPath (Split-Path $commonPath -Parent) -Filter '*.ps1' -File){[void](Pin $path.FullName)}
. $commonPath
. $readerPath
function Replace-Body([string]$Text,[string]$Signature,[string]$Body){$part=Member $Text $Signature;$open=$part.IndexOf('{');return $Text.Replace($part,($part.Substring(0,$open+1)+[Environment]::NewLine+$Body+[Environment]::NewLine+'}'))}
$observer=[IO.File]::ReadAllText($observerPath);$choices=[IO.File]::ReadAllText($choicePath);$scenario=[IO.File]::ReadAllText($scenarioPath);$template=[IO.File]::ReadAllText($templatePath)
$observer=Replace-Body $observer 'private static T ReadField<T>' '            return Boundary.Read<T>(type, instance, name, token);'
$observer=Replace-Body $observer 'private static void RequireModule' '            // Exact metadata MVID is an explicit external boundary in this adapter.'
$choices=Replace-Body $choices 'private static T ReadField<T>' '            return Boundary.Read<T>(type, instance, name, token);'
$update=Member $scenario 'internal void Update()';$marker='                if (!callbackReturned || Busy()) return;';$end=$update.IndexOf($marker,[StringComparison]::Ordinal)
$catch=$update.IndexOf('            catch (Exception error)',[StringComparison]::Ordinal)
if($end-lt0-or$catch-le$end){throw 'Exact bootstrap failure prefix/catch changed.'}
$prefix=$update.Substring(0,$end+$marker.Length)+[Environment]::NewLine+'                Continued++;'+[Environment]::NewLine+'            }'+[Environment]::NewLine+$update.Substring($catch)
$actual=$template.Replace('/*ACTUAL_SOURCES*/','').Replace('/*ACTUAL_UPDATE_FAILURE_PREFIX*/',$prefix).Replace('/*ACTUAL_BEGIN_CLEANUP*/',(Member $scenario 'private void BeginCleanup()'))
if($actual.Contains('/*ACTUAL_')){throw 'Unresolved exact choice adapter source.'}
$compiler=Pin 'C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$referenceRoot='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$references=@('mscorlib','System','System.Core'|ForEach-Object{Pin (Join-Path $referenceRoot ($_+'.dll'))})
$json=Pin 'C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$loader='static class Entry {static int Main(string[] args){AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>new AssemblyName(e.Name).Name=="Newtonsoft.Json"?Assembly.LoadFrom(@"'+$json+'"):null;return Invoke(args);}[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]static int Invoke(string[] args){if(args.Length!=1)throw new InvalidOperationException("Exact private output required");return Tests.Run(args[0]);}}'
$actualPath=Join-Path $OutputRoot 'actual.cs';$observerCopy=Join-Path $OutputRoot 'recorder.cs';$choiceCopy=Join-Path $OutputRoot 'choices.cs'
[IO.File]::WriteAllText($actualPath,($actual+[Environment]::NewLine+$loader));[IO.File]::WriteAllText($observerCopy,$observer);[IO.File]::WriteAllText($choiceCopy,$choices)
foreach($item in @(@($observerPath,'recorder.original.cs'),@($choicePath,'choices.original.cs'),@($scenarioPath,'scenario.original.cs'),@($templatePath,'template.original.cs'))){[IO.File]::Copy($item[0],(Join-Path $OutputRoot $item[1]),$false)}
$exe=Join-Path $OutputRoot 'Actual.exe';$rsp=Join-Path $OutputRoot 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/nowarn:0649','/nostdlib+','/deterministic+','/optimize+',('/out:'+$exe),('/reference:'+$json))+@($references|ForEach-Object{'/reference:'+$_})+@($actualPath,$observerCopy,$choiceCopy,$identityPath)
[IO.File]::WriteAllText($rsp,(($arguments|ForEach-Object{'"'+$_+'"'})-join[Environment]::NewLine))
$clock=[Diagnostics.Stopwatch]::StartNew();$global:LASTEXITCODE=0;$compileOutput=@(& $compiler ('@'+$rsp) 2>&1);$compileExit=$global:LASTEXITCODE;$compileSeconds=$clock.Elapsed.TotalSeconds
[IO.File]::WriteAllText((Join-Path $OutputRoot 'compile.log'),(($compileOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$testExit=$null;$testSeconds=0.0;$actualPassed=0;$actualFailed=0;$actualShape=$false;$testOutput=@()
if($compileExit-eq0){$clock.Restart();$global:LASTEXITCODE=0;$testOutput=@(& $exe $OutputRoot 2>&1);$testExit=$global:LASTEXITCODE;$testSeconds=$clock.Elapsed.TotalSeconds
 $lines=@($testOutput|ForEach-Object{$_.ToString()});$totals=@($lines|Where-Object{$_-cmatch'^PASSIVE CHOICE ACTUAL PASS=([0-9]+) FAIL=([0-9]+)$'})
 if($totals.Count-eq1-and$totals[0]-cmatch'^PASSIVE CHOICE ACTUAL PASS=([0-9]+) FAIL=([0-9]+)$'){$actualPassed=[int]$Matches[1];$actualFailed=[int]$Matches[2];$actualShape=$actualPassed-gt0-and(@($lines|Where-Object{$_-clike'PASS *'}).Count-eq$actualPassed)-and(@($lines|Where-Object{$_-clike'FAIL *'}).Count-eq$actualFailed)}
 $lines|ForEach-Object{Write-Host $_}
}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'run.log'),(($testOutput|ForEach-Object{$_.ToString()})-join[Environment]::NewLine))
$script:choicePass=0;$script:choiceFail=0
function Good-Choice([string]$Name,[scriptblock]$Action){try{&$Action;$script:choicePass++;Write-Host ('PASS '+$Name)}catch{$script:choiceFail++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad-Choice([string]$Name,[scriptblock]$Action,[string]$Reason=''){try{&$Action;$script:choiceFail++;Write-Host ('FAIL accepted '+$Name)}catch{if($Reason-and$_.ToString()-notmatch$Reason){$script:choiceFail++;Write-Host ('FAIL wrong refusal '+$Name+': '+$_)}else{$script:choicePass++;Write-Host ('PASS refuses '+$Name)}}}
function Read-Choice([string]$Leaf='choice-valid.json'){return ([IO.File]::ReadAllText((Join-Path $OutputRoot $Leaf))|ConvertFrom-Json)}
function Set-ChoiceMissingOwner($Row,[bool]$MissingGame,[bool]$Descendant){
 $priorUi=$Row.choices.ui
 $Row.controller=0
 foreach($key in @('dialog','currentCue','queuedCue','continueCue')){$Row.$key.object=0;$Row.$key.guid=$null}
 $Row.cuePlayScheduled=$null;$Row.dialogStopScheduled=$null
 $c=$Row.choices
 foreach($key in @('controller','dialog','currentCue','ui','common','manager','pcDialog','currentPart')){$c.$key=0}
 $c.uiStatus='missing-controller';$c.managerKind=$null;$c.managerShown=$null;$c.pcDialogActive=$null;$c.offered=@();$c.rows=@();$c.directTypeCount=0
 if($MissingGame){$Row.game=0;$Row.topMode=$null;$Row.dialogActive=$null;$Row.cutsceneLock=$null;$Row.dialogState=0;$Row.scheduledStart=0;$Row.scheduledStartIsScheduled=$null;$Row.scheduledDialog.object=0;$Row.scheduledDialog.guid=$null;$c.game=0;$c.uiStatus='missing-game'}
 if($Descendant){$c.ui=$priorUi;$c.uiStatus='missing-common'}
}
if($testExit-eq0-and$actualShape-and$actualFailed-eq0){
 foreach($leaf in @('choice-valid.json','choice-error.json','choice-max.json','choice-max-text.json','choice-foreign.json','choice-duplicates.json','choice-absent.json','choice-unsupported.json','choice-null-metadata.json')){Good-Choice ('actual producer '+$leaf){Assert-KmcChunk7BootstrapDialogueObservation (Read-Choice $leaf) $true}}
 Good-Choice 'missing controller returns before UI observation' {$v=Read-Choice;foreach($row in $v.snapshots){Set-ChoiceMissingOwner $row $false $false};Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Good-Choice 'v3 exact capacity and escaped byte budget' {$v=Read-Choice 'choice-max-text.json';$bytes=Read-Choice 'maximum-output-bytes.json';if($v.identityIds.Count-ne444-or$bytes.compactUtf8Bytes-ge2MB-or$bytes.indentedUtf8Bytes-ge2MB-or$bytes.existingBootstrapSourceArtifactCap-ne16MB){throw 'Exact maximal diagnostic budget differs.'}}
 Good-Choice 'full synthetic artifact stays below existing sixteen MiB cap' {$f=[IO.File]::ReadAllText($fixturePath)|ConvertFrom-Json;$f.artifact.observations|Add-Member -NotePropertyName dialogueObservation -NotePropertyValue (Read-Choice 'choice-max-text.json');$raw=$f.artifact|ConvertTo-Json -Depth 90;if([Text.Encoding]::UTF8.GetByteCount($raw)-ge16MB){throw 'Bounded complete artifact exceeded existing cap.'}}
 Good-Choice 'ordinary failure artifact remains FAIL with passive choice subtree' {$f=[IO.File]::ReadAllText($fixturePath)|ConvertFrom-Json;$f.artifact.status='FAIL';$v=Read-Choice;$v.identityReleased=$false;$v.snapshots=@($v.snapshots[0]);$f.artifact.observations|Add-Member -NotePropertyName dialogueObservation -NotePropertyValue $v;Assert-KmcChunk7NativeBootstrapEvidence $f.request $f.artifact 'FAIL';if($f.artifact.status-cne'FAIL'){throw 'Passive read rewrote original failure.'}}
 Good-Choice 'historical v2 observation removes only choice subtree and restores exact cap' {$v=Read-Choice;$v.contract='chunk7-bootstrap-dialogue-raw-v2';$v.identityCapacity=40;foreach($row in $v.snapshots){$row.PSObject.Properties.Remove('choices')};Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 Good-Choice 'historical v1 observation retains exact old shape' {$v=Read-Choice;$v.contract='chunk7-bootstrap-dialogue-raw-v1';$v.identityCapacity=32;foreach($row in $v.snapshots){foreach($key in @('choices','dialogState','scheduledStart','scheduledStartIsScheduled','scheduledDialog')){$row.PSObject.Properties.Remove($key)}};Assert-KmcChunk7BootstrapDialogueObservation $v $true}
 $mutations=[ordered]@{
 'v3 missing choices'={param($v)$v.snapshots[0].PSObject.Properties.Remove('choices')}
 'v3 null choices'={param($v)$v.snapshots[0].choices=$null}
 'v3 prior capacity'={param($v)$v.identityCapacity=40}
 'v3 registry overflow'={param($v)$v.identityIds=@(1..445)}
 'choice singleton object'={param($v)$v.snapshots[0].choices=@($v.snapshots[0].choices)}
 'choice read flag string'={param($v)$v.snapshots[0].choices.readSucceeded='true'}
 'choice scope array'={param($v)$v.snapshots[0].choices.scope=@($v.snapshots[0].choices.scope)}
 'choice scope alias'={param($v)$v.snapshots[0].choices.scope='active-legal-answers'}
 'choice cache cannot authorize'={param($v)$v.snapshots[0].choices.cachedStateIsAuthorization=$true}
 'choice nested evaluation claim'={param($v)$v.snapshots[0].choices.directTypesOnly=$false}
 'choice system button claim'={param($v)$v.snapshots[0].choices.systemButtonsIncluded=$true}
 'choice unknown property'={param($v)$v.snapshots[0].choices|Add-Member -NotePropertyName canSelect -NotePropertyValue $true}
 'choice error on success'={param($v)$v.snapshots[0].choices.readError='unexpected'}
 'choice context label array'={param($v)$v.snapshots[0].choices.controller=@($v.snapshots[0].choices.controller)}
 'choice context label outside registry'={param($v)$v.snapshots[0].choices.ui=-9999999}
 'choice context foreign phase'={param($v)$v.snapshots[0].choices.controller=$v.snapshots[0].game}
 'choice UI status array'={param($v)$v.snapshots[0].choices.uiStatus=@('pc-cached-rows')}
 'choice missing game contradiction'={param($v)$v.snapshots[0].choices.uiStatus='missing-game'}
 'choice ownerless controller UI descendant'={param($v)foreach($row in $v.snapshots){Set-ChoiceMissingOwner $row $false $true}}
 'choice ownerless game UI descendant'={param($v)foreach($row in $v.snapshots){Set-ChoiceMissingOwner $row $true $true}}
 'choice missing UI ancestry'={param($v)$v.snapshots[0].choices.ui=0}
 'choice manager kind array'={param($v)$v.snapshots[0].choices.managerKind=@(0)}
 'choice manager shown text'={param($v)$v.snapshots[0].choices.managerShown='true'}
 'choice PC flag numeric'={param($v)$v.snapshots[0].choices.pcDialogActive=1}
 'choice offered null'={param($v)$v.snapshots[0].choices.offered=$null}
 'choice offered over cap'={param($v)$v.snapshots[0].choices.offered=@(1..33|ForEach-Object{$v.snapshots[0].choices.offered[0]})}
 'choice rows over cap'={param($v)$v.snapshots[0].choices.rows=@(1..33|ForEach-Object{$v.snapshots[0].choices.rows[0]})}
 'choice total mistyped'={param($v)$v.snapshots[0].choices.directTypeCount='2'}
 'choice total wrong'={param($v)$v.snapshots[0].choices.directTypeCount=3}
 'choice answer ID zero'={param($v)$v.snapshots[0].choices.offered[0].answer.object=0}
 'choice answer ID array'={param($v)$v.snapshots[0].choices.offered[0].answer.object=@($v.snapshots[0].choices.offered[0].answer.object)}
 'choice answer GUID array'={param($v)$v.snapshots[0].choices.offered[0].answer.guid=@($v.snapshots[0].choices.offered[0].answer.guid)}
 'choice answer malformed GUID'={param($v)$v.snapshots[0].choices.offered[0].answer.guid='not-guid'}
 'choice selection type array'={param($v)$v.snapshots[0].choices.offered[0].selectionType=@(0)}
 'choice valid-cue text'={param($v)$v.snapshots[0].choices.offered[0].requireValidCue='false'}
 'choice type list scalar'={param($v)$v.snapshots[0].choices.offered[0].showConditionTypes='NativeType'}
 'choice type list over cap'={param($v)$v.snapshots[0].choices.offered[0].showConditionTypes=@(1..33|ForEach-Object{'Type'})}
 'choice type value object'={param($v)$v.snapshots[0].choices.offered[0].showConditionTypes=@([pscustomobject]@{name='Type'})}
 'choice type text over cap'={param($v)$v.snapshots[0].choices.offered[0].showConditionTypes=@(('x'*257))}
 'choice empty type text'={param($v)$v.snapshots[0].choices.offered[0].showConditionTypes=@('')}
 'choice UI row label array'={param($v)$v.snapshots[0].choices.rows[0].row=@($v.snapshots[0].choices.rows[0].row)}
 'choice native index float'={param($v)$v.snapshots[0].choices.rows[0].nativeIndex=0.5}
 'choice cached disabled array'={param($v)$v.snapshots[0].choices.rows[0].cachedDisabled=@($false)}
 'choice initialized text'={param($v)$v.snapshots[0].choices.rows[0].initialized='true'}
 'choice index list scalar'={param($v)$v.snapshots[0].choices.rows[0].matchingOfferIndices=0}
 'choice matching index wrong'={param($v)$v.snapshots[0].choices.rows[0].matchingOfferIndices=@(1)}
 'choice matching index string'={param($v)$v.snapshots[0].choices.rows[0].matchingOfferIndices=@('0')}
 'choice row duplicate count wrong'={param($v)$v.snapshots[0].choices.rows[0].sameRowReferences=2}
 'choice answer duplicate count wrong'={param($v)$v.snapshots[0].choices.rows[0].sameAnswerReferences=0}
 }
 foreach($entry in $mutations.GetEnumerator()){Bad-Choice $entry.Key {$v=Read-Choice;&$entry.Value $v;Assert-KmcChunk7BootstrapDialogueObservation $v $true}}
 Bad-Choice 'foreign same GUID cannot claim offered object match' {$v=Read-Choice 'choice-foreign.json';$v.snapshots[0].choices.rows[0].matchingOfferIndices=@(0);Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'offered-reference links'
 Bad-Choice 'duplicate offered identities require complete index list' {$v=Read-Choice 'choice-duplicates.json';$v.snapshots[0].choices.rows[0].matchingOfferIndices=@(0);Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'offered-reference links'
 Bad-Choice 'explicit choice failure refuses partial fields' {$v=Read-Choice 'choice-error.json';$v.snapshots[0].choices.rows=@();Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'failed read retained partial'
 Bad-Choice 'explicit choice failure needs error' {$v=Read-Choice 'choice-error.json';$v.snapshots[0].choices.readError=$null;Assert-KmcChunk7BootstrapDialogueObservation $v $true} 'failure needs'
 Bad-Choice 'old v2 refuses new undeclared choice field' {$v=Read-Choice;$v.contract='chunk7-bootstrap-dialogue-raw-v2';$v.identityCapacity=40;Assert-KmcChunk7BootstrapDialogueObservation $v $true}
}
$changed=@($inputs.Keys|Where-Object{(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()-cne$inputs[$_]})
$ok=$compileExit-eq0-and$testExit-eq0-and$actualShape-and$actualFailed-eq0-and$script:choicePass-gt0-and$script:choiceFail-eq0-and$changed.Count-eq0
$inputRows=@($inputs.Keys|Sort-Object|ForEach-Object{[ordered]@{path=$_;sha256=$inputs[$_]}})
$outputRows=@(Get-ChildItem -LiteralPath $OutputRoot -File|Where-Object{$_.Extension-cin@('.json','.cs','.rsp','.log')}|Sort-Object Name|ForEach-Object{[ordered]@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
[ordered]@{kind='actual-passive-dialogue-choice-regression';status=$(if($ok){'PASS'}else{'FAIL'});compileExit=$compileExit;exitCode=$testExit;passed=($actualPassed+$script:choicePass);failed=($actualFailed+$script:choiceFail);actualPassed=$actualPassed;actualFailed=$actualFailed;readerPassed=$script:choicePass;readerFailed=$script:choiceFail;resultShapeValid=$actualShape;compileDurationSeconds=$compileSeconds;testDurationSeconds=$testSeconds;inputs=$inputRows;outputs=$outputRows;changedInputs=$changed;substitutions=@('Exact native token/MVID/declared-field retrieval uses controlled native facades','Actual Update prefix/catch and BeginCleanup; later world-readiness continuation replaced by sentinel; authority/busy/poll external','Unity-null semantics modelled without native call','Serializer referenced in place; no native/asset/archive/guarded runner');nativeInvoked=$false;archiveOperations=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Host ('PASSIVE CHOICE READER PASS='+$script:choicePass+' FAIL='+$script:choiceFail)
if(-not$ok){exit 1};exit 0
