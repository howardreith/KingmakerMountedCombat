Set-StrictMode -Version Latest

# Existing owner-seed Ranger acquisition within the F09 fixture, independently
# bounded from the paid new-companion operation. The fixture verdict stays intact.
function Get-KmcChunk7SmallExistingAcquisitionProof($Request,$Artifact) {
    Assert-KmcChunk7SmallFixtureEvidence $Request $Artifact
    $o=$Artifact.observations;$rows=@($o.choices)
    if($rows.Count-lt5-or$rows.Count-gt1400){throw 'Existing-character native choice history is absent or unbounded.'}
    $existing=@(for($i=0;$i-lt$rows.Count;$i++){if($rows[$i].event-ceq'native-existing-level-admission'){$i}})
    $builders=@(for($i=0;$i-lt$rows.Count;$i++){if($rows[$i].event-ceq'native-builder-complete'){$i}})
    $paid=@(for($i=0;$i-lt$rows.Count;$i++){if($rows[$i].event-ceq'native-paid-hire-admission'){$i}})
    if($existing.Count-ne1-or$existing[0]-ne0-or$builders.Count-ne2-or$paid.Count-ne1-or
       $builders[0]-ge$paid[0]-or$paid[0]-ge$builders[1]){
        throw 'Existing native choice and later paid-hire boundaries differ.'
    }
    $choiceRows=@($rows[0..$builders[0]])
    $admission=$choiceRows[0]
    $selection=[pscustomobject]@{minimumRangerLevel=$Request.smallFixture.targetRangerLevel;existingActorId=$admission.actorId}
    $initialIds=@($o.loadedWorld.party|ForEach-Object id)
    $proof=Get-KmcChunk7OrdinaryChoiceProof $Request.runId $selection $false $initialIds $choiceRows `
        $admission.money $o.mainBuild $Request.productVersion
    $commits=@($choiceRows|Where-Object event -CEQ 'native-level-committed')
    if($commits.Count-ne1-or$null-ne$commits[0].before.petId-or
       $commits[0].after.petId-cne$o.mainBuild.petId-or
       $proof.identity.actorId-cne$o.mainBuild.actorId-or
       $proof.initialLevel-ne3-or$proof.finalLevel-ne4-or$proof.nativeCommitCount-ne1-or
       $proof.beforeMoney-ne$proof.money-or$proof.money-ne$o.paidHireAdmission.moneyBefore){
        throw 'Existing Ranger did not acquire the exact Horse in its single native commit.'
    }
    [pscustomobject][ordered]@{
        scope='existing-owner-seed-ranger-horse-acquisition-one-native-commit-only'
        sourceScenario=$Request.scenario;sourceRunId=$Request.runId
        fixtureSetupOnly=$true;producerAncestry=$false
        actorId=$proof.identity.actorId;horseId=$o.mainBuild.petId
        startingLevel=$proof.initialLevel;endingLevel=$proof.finalLevel
        nativeCommitCount=$proof.nativeCommitCount;acceptedChoiceCount=$proof.acceptedChoiceCount
        moneyBefore=$proof.beforeMoney;moneyAfter=$proof.money
        fieldRetained=$true;genuineManual=$true
        newCampaignPathQualified=$false;generalExistingCampaignSupportQualified=$false
    }
}
