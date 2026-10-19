Set-StrictMode -Version Latest
# Pure native-choice evidence only. Caller owns scenario/source/output admission.
# RuntimeHarness.Common supplies the exact JSON primitives and ordinary snapshot validator.
function Get-KmcChunk7OrdinaryChoiceProof($RunId,$Selection,$Paid,$InitialActorIds,$Rows,$BeforeMoney,$BuilderComplete,[string]$ProductVersion) {
    if($RunId-isnot[string]-or$RunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Native ordinary choice run ID must be an exact scalar identifier.'}
    if($Selection-isnot[pscustomobject]-or$Paid-isnot[bool]){throw 'Native ordinary choice selection and paid mode must be explicitly typed.'}
    Assert-KmcOrdinaryInteger $Selection.minimumRangerLevel 4 20
    if($InitialActorIds-isnot[Array]-or$InitialActorIds.Count-lt1){throw 'Native ordinary initial actor IDs must be a nonempty array.'}
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($id in $InitialActorIds){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw 'Native ordinary initial actor IDs must be unique.'}}
    if($Paid){
        foreach($field in @('smallRaceGuid','smallPresetGuid')){if($Selection.$field-isnot[string]-or$Selection.$field-cnotmatch'^[a-f0-9]{32}$'){throw 'Observed native Small blueprint pin is required.'}}
        if($Selection.inventoryArtifactSha256-isnot[string]-or$Selection.inventoryArtifactSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Native paid inventory hash must be an exact scalar SHA256.'}
    }else{
        Assert-KmcOrdinaryGuid $Selection.existingActorId
        if(-not$ids.Contains($Selection.existingActorId)){throw 'Existing native choice actor must belong to the initial actor set.'}
    }
    Assert-KmcOrdinaryInteger $BeforeMoney
    if($BuilderComplete-isnot[pscustomobject]){throw 'Native ordinary builder snapshot must be an object.'}
    $s=$Selection;$original=$InitialActorIds;$ChoiceRows=$Rows
    if($ChoiceRows-isnot[Array]-or$ChoiceRows.Count-lt4-or$ChoiceRows.Count-gt1400){throw 'Bounded native ordinary events are missing.'}
    $rows=@($ChoiceRows);$admission=$rows[0]
    if($paid){
        if($admission.event-cne'native-paid-hire-admission'-or$rows[1].event-cne'native-new-actor-bound'){throw 'Paid native admission/binding is missing.'}
        Assert-KmcOrdinaryInteger $admission.moneyBefore
        Assert-KmcOrdinaryInteger $admission.nativeInheritedXp 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $admission.nativeCost 1;Assert-KmcOrdinaryInteger $admission.nativeExpectedFinalLevel $s.minimumRangerLevel 20
        if($admission.moneyBefore-lt$admission.nativeCost-or$admission.nativeInventorySha256-cne$s.inventoryArtifactSha256){throw 'Native paid admission is unaffordable or unbound.'}
        $explicitSpend=$admission.PSObject.Properties.Name-ccontains'spendObservationContract'
        if($explicitSpend-and$admission.spendObservationContract-cne'native-hire-spent-v1'){throw 'Native hire spend observation contract differs.'}
        $legacySpendVersion=$ProductVersion-cin@('0.1.0-test','synthetic')-or
            ($ProductVersion-cmatch'^0\.1\.0-chunk7b-preview\.(\d+)$'-and[int]$Matches[1]-lt63)
        if(-not$explicitSpend-and-not$legacySpendVersion){throw 'Current paid native evidence omitted its required spend observation.'}
        $identity=$rows[1];$xp=$admission.nativeInheritedXp;$level=0;$money=$admission.moneyBefore;$expectedMoney=$money-$admission.nativeCost
        $first=2
        if($identity.actorId-cin$original-or$identity.nativeMode-cne'CharGen'){throw 'Native paid hire reused an actor or skipped CharGen.'}
    }else{
        if($admission.event-cne'native-existing-level-admission'){throw 'Existing native readiness admission missing.'}
        $identity=$admission;$xp=$identity.xp;$level=$identity.level;$money=$identity.money;$expectedMoney=$money;$first=1
        if($identity.actorId-cne$s.existingActorId-or$null-ne$identity.petId){throw 'Existing native actor or prior companion differs.'}
    }
    Assert-KmcOrdinaryGuid $identity.actorId;Assert-KmcOrdinarySnapshot $identity $identity $xp $level $money
    Assert-KmcChunk7Boolean $identity.nativeCanLevelUp $true
    Assert-KmcOrdinaryInteger $BeforeMoney $money $money
    $accepted=0;$commits=0;$pendingChoice=$null;$pendingCommit=$null;$finished=$false;$lastFrame=-1;$lastTime=-1
    $horseSelected=$false;$bondSelected=$false
    for($i=0;$i-lt$rows.Count;$i++){
        $row=$rows[$i]
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        if($row.runId-cne$RunId-or$row.frame-lt$lastFrame-or-not(Test-KmcChunk7FiniteNumber $row.elapsedSeconds)-or
           $row.elapsedSeconds-lt$lastTime-or$row.elapsedSeconds-gt720){throw 'Native ordinary event run/frame/time order differs.'}
        $lastFrame=$row.frame;$lastTime=$row.elapsedSeconds
        if($i-lt$first){continue};if($finished){throw 'Native ordinary choices continued after completion.'}
        switch -CaseSensitive ($row.event){
            'native-level-open' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Native ordinary level opened before its preceding operation settled.'}
                Assert-KmcOrdinarySnapshot $row $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true
                Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1)
            }
            'native-choice-requested' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Overlapping ordinary choice requests.'}
                if($row.kind-cnotin@('portrait','race','gender','native-race-preset','native-handedness','class','attribute-purchase','racial-attribute','level-attribute','feature','skill','name','alignment','voice','native-total-phase')-or
                   $row.choice-isnot[string]-or[string]::IsNullOrEmpty($row.choice)){throw 'Unknown native ordinary choice.'}
                Assert-KmcOrdinarySnapshot $row.before $identity $xp $level $money;$pendingChoice=$row
            }
            'native-choice' {
                if($null-eq$pendingChoice-or$row.kind-cne$pendingChoice.kind-or$row.choice-cne$pendingChoice.choice-or$row.nativeSelectionGuid-cne$pendingChoice.nativeSelectionGuid-or
                   (ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingChoice.before)){throw 'Native ordinary choice lost its exact requested binding.'}
                Assert-KmcChunk7Boolean $row.nativeChoiceAccepted $true
                Assert-KmcOrdinarySnapshot $row.before $identity $xp $level $money;Assert-KmcOrdinarySnapshot $row.after $identity $xp $level $money
                if($row.kind-ceq'feature'){
                    if($row.nativeSelectionGuid-isnot[string]-or$row.nativeSelectionGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native feature selection identity missing.'}
                    if($row.nativeSelectionGuid-ceq'b705c5184a96a84428eeb35ae2517a14'-and$row.choice.StartsWith('ee63330662126374e8785cc901941ac7/',[StringComparison]::Ordinal)){$bondSelected=$true}
                    if($row.nativeSelectionGuid-ceq'ee63330662126374e8785cc901941ac7'-and$row.choice.StartsWith('7db7c50677e39f09feef56f3831fc723/',[StringComparison]::Ordinal)){$horseSelected=$true}
                }elseif($null-ne$row.nativeSelectionGuid){throw 'Non-feature choice contains feature selection identity.'}
                if($row.kind-ceq'class'-and$row.choice-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native ordinary class differs.'}
                if($paid-and$row.kind-ceq'race'-and$row.choice-cne$s.smallRaceGuid){throw 'Native selected Small race differs.'}
                if($paid-and$row.kind-ceq'native-race-preset'-and$row.choice-cne$s.smallPresetGuid){throw 'Native selected Small preset differs.'}
                $pendingChoice=$null;$accepted++
            }
            'native-total-next-requested' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Native Total overlapped unfinished choices.'}
                Assert-KmcOrdinarySnapshot $row $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true;Assert-KmcChunk7Boolean $row.nativeIsComplete $true
                if($row.phase-cne'Total'-or$row.selectedClass-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native completion bypassed its Total/selected class gate.'}
                foreach($field in @('attributePoints','distributionPoints','skillRemaining','pendingFeatures','pendingSpellGroups')){Assert-KmcOrdinaryInteger $row.$field 0 0}
                Assert-KmcOrdinaryInteger $row.skillTotal 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $row.skillSpent $row.skillTotal $row.skillTotal
                Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1);$pendingCommit=$row
            }
            'native-level-committed' {
                if($null-eq$pendingCommit-or(ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingCommit)){throw 'Native level commit lacks its exact prior complete Total gate.'}
                $commits++;Assert-KmcOrdinaryInteger $row.commitCount $commits $commits
                if($paid-and-not$explicitSpend-and$money-eq$admission.moneyBefore-and$row.after.money-eq$expectedMoney){$money=$expectedMoney}
                Assert-KmcOrdinaryInteger $row.moneyExpected $money $money
                $level++;Assert-KmcOrdinarySnapshot $row.after $identity $xp $level $money
                $pendingCommit=$null
            }
            'native-hire-spent' {
                if(-not$paid-or-not$explicitSpend-or$money-ne$admission.moneyBefore){throw 'Native hire spent more than once or outside the declared paid observation.'}
                Assert-KmcOrdinaryInteger $row.moneyBefore $admission.moneyBefore $admission.moneyBefore
                Assert-KmcOrdinaryInteger $row.nativeCost $admission.nativeCost $admission.nativeCost
                Assert-KmcOrdinaryInteger $row.moneyAfter $expectedMoney $expectedMoney
                $money=$expectedMoney
            }
            'native-builder-complete' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit-or$commits-lt1){throw 'Ordinary builder completion precedes committed choices.'}
                if($paid-and$money-ne$expectedMoney){throw 'Paid native builder completed before exact hire expenditure.'}
                Assert-KmcOrdinarySnapshot $row.actor $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.actor.nativeCanLevelUp $false;Assert-KmcChunk7Boolean $row.ordinaryChoicesComplete $true
                foreach($field in @('nativeFieldAdmissionProven','nativeSaveSettlementProven','poseQualified')){Assert-KmcChunk7Boolean $row.$field $false}
                if((ConvertTo-KmcChunk7Canonical $row.actor)-cne(ConvertTo-KmcChunk7Canonical $BuilderComplete)){throw 'Ordinary terminal actor snapshot differs.'}
                if($paid-and$level-ne$admission.nativeExpectedFinalLevel){throw 'Native hire reached a level not supported by its inherited XP.'}
                Assert-KmcOrdinaryInteger $row.actor.rangerLevel $s.minimumRangerLevel 20
                Assert-KmcOrdinaryGuid $row.actor.petId
                if($row.actor.petBlueprint-cne'4016c7db400ab721ff125aef9e65e202') {throw 'Ordinary native pet differs.'}
                $finished=$true
            }
            default {throw 'Unexpected ordinary native event.'}
        }
    }
    if(-not$finished-or-not$horseSelected-or-not$bondSelected){throw 'Native ordinary completion/companion selection chain is incomplete.'}
    if($accepted-lt2-or$accepted-gt512-or$commits-gt20){throw 'Ordinary native choice bounds differ.'}
    [pscustomobject]@{identity=$identity;xp=$xp;initialLevel=$identity.level;finalLevel=$level;beforeMoney=$BeforeMoney;money=$money;expectedMoney=$expectedMoney;acceptedChoiceCount=$accepted;nativeCommitCount=$commits;lastFrame=$lastFrame;lastElapsedSeconds=$lastTime}
}
