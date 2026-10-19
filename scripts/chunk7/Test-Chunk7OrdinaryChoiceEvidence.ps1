[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/ordinary-choice-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh output required'}
[void][IO.Directory]::CreateDirectory($OutputDirectory);$choiceOutput=$OutputDirectory
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7OrdinaryEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixturesOnly
$script:choicePass=0;$script:choiceFail=0;$script:choiceLines=[Collections.Generic.List[string]]::new()
function Check-Choice([string]$CaseLabel,[scriptblock]$Action,[bool]$Reject=$false){
    $caught=$null;try{&$Action}catch{$caught=$_};$ok=if($Reject){$null-ne$caught}else{$null-eq$caught}
    if($ok){$script:choicePass++;$line='PASS '+$CaseLabel}else{$script:choiceFail++;$line='FAIL '+$CaseLabel+': '+$(if($null-eq$caught){'unexpected acceptance'}else{$caught.ToString()+' '+$caught.ScriptStackTrace})}
    $script:choiceLines.Add($line);Write-Host $line
}
function Choice-Args([bool]$Paid=$false){$f=New-OrdinaryFixture $Paid;$o=$f.artifact.observations;@{RunId=$f.request.runId;Selection=$f.request.ordinaryAcquisition;Paid=$Paid;InitialActorIds=$o.originalParty;Rows=$o.choices;BeforeMoney=$o.before.money;BuilderComplete=$o.builderComplete;ProductVersion=$f.request.productVersion}}
function Need-Choice([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Use-ExplicitHireSpend($ProofArgs){
    $admission=$ProofArgs.Rows[0]
    $admission | Add-Member -NotePropertyName spendObservationContract -NotePropertyValue 'native-hire-spent-v1'
    $commit=@($ProofArgs.Rows|Where-Object event -CEQ 'native-level-committed')[0]
    $spend=[pscustomobject]@{event='native-hire-spent';runId=$commit.runId;frame=$commit.frame;
        elapsedSeconds=$commit.elapsedSeconds;moneyBefore=$admission.moneyBefore;nativeCost=$admission.nativeCost;
        moneyAfter=($admission.moneyBefore-$admission.nativeCost)}
    $rows=[Collections.Generic.List[object]]::new()
    foreach($row in $ProofArgs.Rows){if([object]::ReferenceEquals($row,$commit)){$rows.Add($spend)};$rows.Add($row)}
    $ProofArgs.Rows=$rows.ToArray()
}
foreach($paidCase in @($false,$true)){
    Check-Choice ('actual '+$(if($paidCase){'paid'}else{'existing'})+' sequence returns exact state without mutation') {
        $a=Choice-Args $paidCase;$before=ConvertTo-KmcChunk7Canonical $a;$p=Get-KmcChunk7OrdinaryChoiceProof @a
        Need-Choice ((ConvertTo-KmcChunk7Canonical $a)-ceq$before) 'Input evidence mutated'
        $initial=if($paidCase){0}else{1};$money=if($paidCase){18000}else{20000};$choices=if($paidCase){6}else{5};$commits=4-$initial
        Need-Choice ($p.xp-eq6000-and$p.initialLevel-eq$initial-and$p.finalLevel-eq4-and$p.beforeMoney-eq20000-and$p.money-eq$money-and$p.expectedMoney-eq$money-and$p.acceptedChoiceCount-eq$choices-and$p.nativeCommitCount-eq$commits) 'Derived sequence state differs'
        $identity=if($paidCase){$a.Rows[1]}else{$a.Rows[0]};Need-Choice ([object]::ReferenceEquals($identity,$p.identity)) 'Admission identity reference changed'
        Need-Choice ($p.lastFrame-eq$a.Rows[-1].frame-and$p.lastElapsedSeconds-eq$a.Rows[-1].elapsedSeconds) 'Terminal chronology differs'
    }
}
Check-Choice 'minimal explicit existing selection is independent of ordinary artifact' {
    $a=Choice-Args;$a.Selection=[pscustomobject]@{existingActorId=$a.Selection.existingActorId;minimumRangerLevel=4};$a.InitialActorIds=@($a.Selection.existingActorId)
    $p=Get-KmcChunk7OrdinaryChoiceProof @a;Need-Choice ($p.finalLevel-eq4-and$p.nativeCommitCount-eq3) 'Explicit minimal selection differs'
}
Check-Choice 'explicit native hire spend precedes first paid commit' {
    $a=Choice-Args $true;Use-ExplicitHireSpend $a
    $p=Get-KmcChunk7OrdinaryChoiceProof @a
    Need-Choice ($p.money-eq18000-and$p.nativeCommitCount-eq4) 'Explicit native spend proof differs'
}
Check-Choice 'contracted paid evidence refuses missing spend event' {
    $a=Choice-Args $true;$a.Rows[0]|Add-Member -NotePropertyName spendObservationContract -NotePropertyValue 'native-hire-spent-v1'
    $null=Get-KmcChunk7OrdinaryChoiceProof @a
} $true
Check-Choice 'current paid candidate refuses missing spend contract' {
    $a=Choice-Args $true;$a.ProductVersion='0.1.0-chunk7b-preview.63'
    $null=Get-KmcChunk7OrdinaryChoiceProof @a
} $true
Check-Choice 'contracted paid evidence refuses duplicate spend event' {
    $a=Choice-Args $true;Use-ExplicitHireSpend $a
    $a.Rows=@($a.Rows[0..6])+@($a.Rows[6])+@($a.Rows[7..($a.Rows.Count-1)])
    $null=Get-KmcChunk7OrdinaryChoiceProof @a
} $true
Check-Choice 'contracted paid evidence refuses wrong spend amount' {
    $a=Choice-Args $true;Use-ExplicitHireSpend $a
    $a.Rows[6].moneyAfter++
    $null=Get-KmcChunk7OrdinaryChoiceProof @a
} $true
Check-Choice 'declared ordinary accepted count still checked by old caller' {$f=New-OrdinaryFixture;$f.artifact.observations.acceptedChoiceCount++;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact} $true
Check-Choice 'declared ordinary commit count still checked by old caller' {$f=New-OrdinaryFixture;$f.artifact.observations.nativeCommitCount++;Assert-KmcChunk7OrdinaryEvidence $f.request $f.artifact} $true
$badArgs=[ordered]@{
 'run null'={param($a)$a.RunId=$null}
 'run array'={param($a)$a.RunId=@($a.RunId)}
 'run whitespace'={param($a)$a.RunId=' '}
 'run over bound'={param($a)$a.RunId='x'*97}
 'selection scalar'={param($a)$a.Selection=7}
 'selection array'={param($a)$a.Selection=@($a.Selection)}
 'paid string'={param($a)$a.Paid='false'}
 'paid integer'={param($a)$a.Paid=0}
 'paid array'={param($a)$a.Paid=@($false)}
 'minimum string'={param($a)$a.Selection.minimumRangerLevel='4'}
 'minimum below Ranger bond'={param($a)$a.Selection.minimumRangerLevel=3}
 'minimum above native cap'={param($a)$a.Selection.minimumRangerLevel=21}
 'actors scalar'={param($a)$a.InitialActorIds=$a.InitialActorIds[0]}
 'actors empty'={param($a)$a.InitialActorIds=@()}
 'actors duplicate'={param($a)$a.InitialActorIds=@($a.Selection.existingActorId,$a.Selection.existingActorId)}
 'actor noncanonical'={param($a)$a.InitialActorIds[0]=$a.InitialActorIds[0].ToUpperInvariant()}
 'existing actor outside initial set'={param($a)$a.InitialActorIds=@($a.InitialActorIds[0])}
 'existing actor array'={param($a)$a.Selection.existingActorId=@($a.Selection.existingActorId)}
 'rows null'={param($a)$a.Rows=$null}
 'rows scalar'={param($a)$a.Rows=$a.Rows[0]}
 'rows empty'={param($a)$a.Rows=@()}
 'rows over cap'={param($a)$a.Rows=@($a.Rows[0])*1401}
 'before money string'={param($a)$a.BeforeMoney='20000'}
 'before money boolean'={param($a)$a.BeforeMoney=$true}
 'before money negative'={param($a)$a.BeforeMoney=-1}
 'before money differs'={param($a)$a.BeforeMoney++}
 'builder null'={param($a)$a.BuilderComplete=$null}
 'builder scalar'={param($a)$a.BuilderComplete=$true}
 'builder array'={param($a)$a.BuilderComplete=@($a.BuilderComplete)}
 'builder differs'={param($a)$a.BuilderComplete=Copy-O $a.BuilderComplete;$a.BuilderComplete.xp++}
 'foreign sequence run'={param($a)$a.Rows[0].runId='foreign'}
 'unfinished sequence'={param($a)$a.Rows=@($a.Rows|Select-Object -SkipLast 1)}
 'XP changes inside native choice'={param($a)($a.Rows|Where-Object event -CEQ 'native-choice'|Select-Object -First 1).after.xp++}
}
foreach($entry in $badArgs.GetEnumerator()){Check-Choice $entry.Key {$a=Choice-Args;&$entry.Value $a;$null=Get-KmcChunk7OrdinaryChoiceProof @a} $true}
foreach($field in @('smallRaceGuid','smallPresetGuid','inventoryArtifactSha256')){Check-Choice ('paid '+$field+' array') {$a=Choice-Args $true;$a.Selection.$field=@($a.Selection.$field);$null=Get-KmcChunk7OrdinaryChoiceProof @a} $true}
Check-Choice 'paid actor reused from initial set' {$a=Choice-Args $true;$a.InitialActorIds+=,$a.Rows[1].actorId;$null=Get-KmcChunk7OrdinaryChoiceProof @a} $true
$terminal='ORDINARY CHOICE EVIDENCE PASS='+$script:choicePass+' FAIL='+$script:choiceFail;$script:choiceLines.Add($terminal);Write-Host $terminal
[IO.File]::WriteAllLines((Join-Path $choiceOutput 'output.log'),$script:choiceLines,[Text.UTF8Encoding]::new($false))
$r=[ordered]@{schemaVersion=1;pass=$script:choicePass;fail=$script:choiceFail;testPath=$PSCommandPath;testSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash.ToLowerInvariant();repositoryRoot=$RepositoryRoot;nativeInvoked=$false;archiveRead=$false;scope='Actual pure choice validator over generated native event rows; exact argument and return/immutability boundaries, no source/fixture/output qualification.'}
[IO.File]::WriteAllText((Join-Path $choiceOutput 'receipt.json'),($r|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false));if($script:choiceFail-ne0){throw 'Ordinary choice evidence regressions failed; preserved.'}
