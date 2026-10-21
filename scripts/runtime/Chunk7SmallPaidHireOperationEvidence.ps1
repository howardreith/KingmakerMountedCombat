Set-StrictMode -Version Latest

# A limited operation proof over the F09 fixture producer. It does not change
# that producer's fixture-only ancestry or qualify other F10 paths.
function Get-KmcChunk7SmallPaidHireOperationProof($Request,$Artifact) {
    Assert-KmcChunk7SmallFixtureEvidence $Request $Artifact
    $o=$Artifact.observations;$rows=@($o.choices)
    if($rows.Count-lt5-or$rows.Count-gt1400){throw 'Paid-hire operation has no bounded native choice history.'}
    $builders=@(for($i=0;$i-lt$rows.Count;$i++){if($rows[$i].event-ceq'native-builder-complete'){$i}})
    $admissions=@(for($i=0;$i-lt$rows.Count;$i++){if($rows[$i].event-ceq'native-paid-hire-admission'){$i}})
    if($builders.Count-ne2-or$admissions.Count-ne1-or$builders[0]-ge$admissions[0]-or
       $admissions[0]-ge$builders[1]-or$builders[1]-ne($rows.Count-1)){
        throw 'Native main build, paid admission and paid completion order differs.'
    }
    $paidRows=@($rows[$admissions[0]..$builders[1]])
    $selection=[pscustomobject]@{
        minimumRangerLevel=$Request.smallFixture.targetRangerLevel
        smallRaceGuid=$o.bodyObservation.raceGuid
        smallPresetGuid=$o.bodyObservation.presetGuid
        inventoryArtifactSha256=$Request.smallFixture.nativeInventoryArtifactSha256
    }
    $initialIds=@($o.loadedWorld.party|ForEach-Object id)
    $proof=Get-KmcChunk7OrdinaryChoiceProof $Request.runId $selection $true $initialIds $paidRows `
        $o.paidHireAdmission.moneyBefore $o.paidBuild $Request.productVersion
    if($null-ne$proof.identity.petId){throw 'Paid actor already had a Horse at native admission.'}
    $commits=@($paidRows|Where-Object event -CEQ 'native-level-committed')
    if($commits.Count-ne4){throw 'Paid Horse requires four exact native commits.'}
    for($i=0;$i-lt4;$i++){
        if($null-ne$commits[$i].before.petId-or($i-lt3-and$null-ne$commits[$i].after.petId)){
            throw 'Paid Horse was present before its selected final native commit.'
        }
    }
    if($commits[3].after.petId-cne$o.paidBuild.petId){throw 'Final native commit did not acquire the saved Horse.'}
    if($proof.identity.actorId-cne$o.paidBuild.actorId-or$proof.identity.actorId-cne$o.selector.actorId-or
       $proof.initialLevel-ne0-or$proof.finalLevel-ne4-or$proof.nativeCommitCount-ne4-or
       $proof.money-ne$o.afterSaveArrangement.money-or$proof.acceptedChoiceCount-lt2){
        throw 'Paid native operation differs from the admitted field actor or settled save.'
    }
    [pscustomobject][ordered]@{
        scope='paid-custom-companion-horse-acquisition-and-four-native-commits-only'
        sourceScenario=$Request.scenario;sourceRunId=$Request.runId
        fixtureSetupOnly=$true;producerAncestry=$false
        actorId=$proof.identity.actorId;horseId=$o.paidBuild.petId
        startingLevel=$proof.initialLevel;endingLevel=$proof.finalLevel
        nativeCommitCount=$proof.nativeCommitCount;acceptedChoiceCount=$proof.acceptedChoiceCount
        moneyBefore=$proof.beforeMoney;nativeStartingGold=$o.nativeStartingGold
        nativeCost=$o.paidHireAdmission.nativeCost;moneyAfter=$proof.money
        fieldAdmission=$true;genuineManual=$true
        existingCharacterPathQualified=$false;newCampaignPathQualified=$false
    }
}
