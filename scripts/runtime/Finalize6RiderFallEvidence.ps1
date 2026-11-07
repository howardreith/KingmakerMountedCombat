# F6 (FALL-01/FALL-02) rider-fall-integration-manual: the producer on the F5 named-companion world and its separate-process
# cold consumer. The post-fall archive holds one mounted pair (the Mastodon's); the unseated Riding Dog pair is restored by
# the native game alone. The F5 naming and tack helpers are reused for the parts this route repeats; the second pair's
# continuation plan and archive membership do not apply to an unseated pair and are replaced by the post-fall checks.
$script:KmcF6RiderFallProducerShots = @('f6-rt-fall-log','f6-rt-fall-side')
$script:KmcF6RiderFallConsumerShots = @('f6-cold-post-fall','f6-cold-unseated-side','f6-cold-remounted-side')

function Assert-KmcFinalize6RiderFallShot($Shots,[string]$Name,[string]$Root){
    $found=@($Shots|Where-Object{[string]$_.name-ceq$Name})
    if($found.Count-ne1){throw ('F6 view missing or duplicated: '+$Name)}
    $shot=$found[0]
    if($null-ne$shot.PSObject.Properties['error']-or[string]$shot.file-cnotmatch'^movement-visuals/(sprint5|finalize5)-f6-[a-z0-9-]+\.png$'-or[long]$shot.bytes-le0){throw ('F6 view failed: '+$Name)}
    if($Root){
        $path=Join-Path $Root ([string]$shot.file).Replace('/','\')
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)-or(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$shot.sha256){throw ('F6 view file is absent or differs: '+$Name)}
    }
}

function Get-KmcFinalize6Service($World,[string]$Label){
    $s=$World.service
    if($null-eq$s){throw ('F6 '+$Label+': no fall service state')}
    foreach($f in 'begun','resolved','abandoned','pending'){if(-not(Test-KmcExactJsonInteger $s.$f)){throw ('F6 '+$Label+': fall service '+$f+' is not an integer')}}
    $s
}

# One mounted pair, the Mastodon's; nothing invented.
function Assert-KmcFinalize6PostFallArchive($Archive,$Request,[string]$FirstRiderId,[string]$SecondRiderId){
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'F6 post-fall archive'
    if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne(Get-KmcChunk7SelectedFixture $Request).gameId-or
       $Archive.AreaId-cne(Get-KmcChunk7ExpectedArchiveArea $Request)-or$Archive.Policy-cne'rider-principal-distinct-native-v1'-or$Archive.RulesId-cne'crpg-transport-v1'-or
       $Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne1-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$null-ne$Archive.Combat-or
       $Archive.Slots-isnot[Array]){throw 'F6 post-fall archive is not the one-pair exploration world.'}
    $pair=$Archive.Pairs[0]
    if([string]$pair.ProfileId-cne'medium-humanoid-mammoth-v1'-or([Guid]$pair.Rider.Id)-ne([Guid]$FirstRiderId)-or([Guid]$pair.Rider.Id)-eq([Guid]$SecondRiderId)){throw 'F6 post-fall archive pair is not the Mastodon pair.'}
    foreach($slot in $Archive.Slots){if([string]$slot.ActorId-cnotin@($FirstRiderId,$SecondRiderId)-or[int]$slot.Kind-notin@(3,6,7)){throw 'F6 post-fall archive carries a control outside the two riders'' post-F2 bars.'}}
    $pair
}

function Assert-KmcFinalize6RiderFallObservation($Request,$Artifact){
    if($Artifact.status-cne'PASS'){throw 'Native persistence artifact failed.'}
    $o=$Artifact.observations;$cold=(Get-KmcChunk7SelectedScenario $Request)-ceq'chunk7-persistence-load'
    $root=if($null-ne$Request.PSObject.Properties['evidenceRoot']){[string]$Request.evidenceRoot}else{$null}
    Assert-KmcChunk7NativeCounters $o @('nativeSchema','nativeCapacity','processId')
    if($o.checkpoint-cne'rider-fall-integration-manual'-or$o.sourceWorkingSha256-cne$Request.fixture.working.sha256-or
       $o.sourceWorkingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or$o.nativeSchema-ne3-or
       -not(Test-KmcChunk7NativePairCapacity $o.nativeCapacity)-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Native persistence family/fixture/capability identity differs.'}
    Assert-KmcChunk7Boolean $o.cold $cold
    Assert-KmcChunk7Boolean $o.nativeProgressionRequiresExternalWorkingRestoration $true
    Assert-KmcChunk7Boolean $o.ordinaryRespecProven $false
    Assert-KmcChunk7Boolean $o.combatDebtProof $false
    foreach($field in @('pairsClear','sourceHorseRemoved','controlsSettled','saveCustodyClear','selectionRestored','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
    Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0){throw 'Cleanup retained native custody.'}
    $f=$o.riderFall
    if($null-eq$f-or[string]$f.contract-cne'finalize6-rider-fall-persistence-v1'){throw 'F6 rider-fall evidence is missing or has another contract.'}
    $shared=$script:KmcF5SharedName
    if(-not$cold){
        $n=$o.companionNames
        if($null-eq$n){throw 'The F6 producer recorded no naming evidence.'}
        $dog=$n.dog;$mammoth=$n.mammoth
        if($dog.blueprint-cne'b7a14f25d3c94e0d8e6f1a2c5d90e431'-or[string]$dog.id-ceq[string]$mammoth.id){throw 'The F6 producer did not start from the two owned companions.'}
        Assert-KmcFinalize5Identity $n.dogDuplicate $dog $shared 'dog duplicate'
        Assert-KmcFinalize5Labels $n.duplicateLabels $shared $shared 'duplicate'
        Assert-KmcFinalize5Labels $n.atSave $shared $shared 'mounted'
        Assert-KmcFinalize5Overtips $n.overtipsAtSave $shared $shared 'mounted'
        Assert-KmcFinalize5Tack $n.tackMounted 'mounted'
        $policy=$f.policy
        if([int]$policy.dc-ne15-or[int]$policy.damageDice-ne1-or[int]$policy.damageDieSides-ne6-or[string]$policy.stat-cne'SkillMobility'){throw 'F6 D5 policy constants differ.'}
        $setup=@($f.disclosedSetup)
        if(@($setup|Where-Object{[string]$_.action-like'*labelled to fail*'}).Count-ne1-or@($setup|Where-Object{[string]$_.action-like'*native knock-off writer*'}).Count-ne1){throw 'F6 setup was not disclosed.'}
        # The harmless voluntary Dismount and an ordinary remount begin no fall.
        $vb=Get-KmcFinalize6Service $f.voluntaryBefore 'voluntary before';$va=Get-KmcFinalize6Service $f.voluntaryAfter 'voluntary after';$rm=Get-KmcFinalize6Service $f.remounted 'remounted'
        if($f.voluntaryBefore.secondPair.mounted-ne$true-or$f.voluntaryAfter.secondPair.mounted-ne$false-or$f.remounted.secondPair.mounted-ne$true){throw 'F6 voluntary Dismount and remount did not complete natively.'}
        if([long]$va.begun-ne[long]$vb.begun-or[long]$rm.begun-ne[long]$vb.begun-or[long]$vb.begun-ne0){throw 'F6 a voluntary Dismount or remount began a fall.'}
        if([long]$f.remounted.secondPair.relationshipGeneration-le[long]$f.voluntaryBefore.secondPair.relationshipGeneration){throw 'F6 the remount did not start a new pair generation.'}
        if([string]$f.voluntaryRoute-cnotin@('public-control','dismount-shell')){throw 'F6 voluntary Dismount route was not recorded.'}
        # The prone fall.
        $fb=$f.fallBefore;$fa=$f.fallAfter;$sb=Get-KmcFinalize6Service $fb 'fall before';$sa=Get-KmcFinalize6Service $fa 'fall after'
        if($f.knockOff.canBeKnockedOff-ne$true-or$fb.secondPair.mounted-ne$true-or$fb.firstPair.mounted-ne$true){throw 'F6 both pairs were not mounted before the native knock-off.'}
        if($fa.secondPair.mounted-ne$false-or$fa.secondMount.prone-ne$true-or[string]$fa.secondMount.life-cne'Conscious'){throw 'F6 the conscious Riding Dog did not fall prone and unseat its rider.'}
        if($fa.firstPair.mounted-ne$true-or[string]$fa.firstPair.ownerId-cne[string]$fb.firstPair.ownerId-or[long]$fa.firstPair.generation-ne[long]$fb.firstPair.generation){throw 'F6 the identically named Mastodon pair did not stay mounted under the same owner and generation.'}
        if([long]$sa.begun-ne[long]$sb.begun+1-or[long]$sa.resolved-ne[long]$sb.resolved+1-or[long]$sa.pending-ne0-or[long]$sa.abandoned-ne0){throw 'F6 the prone did not resolve exactly one fall.'}
        Assert-KmcFinalize6FallRecord -Record $f.fall -What 'RT prone' -Cause 'MountFellProne' -RiderId ([string]$fb.secondRider.id) -MountId ([string]$fb.secondMount.id) -Pass $false -Mode 'RT' -Generation $fb.secondPair.relationshipGeneration
        if(@($fa.log)-cnotcontains[string]$f.fall.logText){throw 'F6 the explanation is not in the native battle log.'}
        if(-not([string]$f.fall.logText).StartsWith($shared+' falls prone; ')){throw 'F6 the explanation does not use the named Riding Dog.'}
        Assert-KmcFinalize6Placement $f.placement 'the rider landed beside the Riding Dog, not inside it'
        if($f.fallSettled.secondRider.prone-ne$true){throw 'F6 the hard landing did not leave the rider natively prone.'}
        if($f.standing.secondRider.prone-ne$false){throw 'F6 the prone rider did not stand natively.'}
        $st=Get-KmcFinalize6Service $f.standing 'standing'
        if([long]$st.begun-ne[long]$sa.begun-or$f.standing.secondRider.prone-ne$false-or$f.standing.secondMount.prone-ne$false){throw 'F6 both did not stand natively without a further fall.'}
        $at=$f.atSave
        if($at.secondPair.mounted-ne$false-or$at.firstPair.mounted-ne$true-or[long]$at.secondRider.damage-ne[long]$f.fall.riderDamageAfter){throw 'F6 the save did not capture the post-fall world.'}
        $pair=Assert-KmcFinalize6PostFallArchive $o.nativeWrite.archive $Request ([string]$at.firstRider.id) ([string]$at.secondRider.id)
        if([string]$o.nativeWrite.nativeType-cne'Manual'-or[string]$o.nativeWrite.sha256-cnotmatch'^[0-9a-f]{64}$'){throw 'F6 the post-fall world was not written through the native Manual.'}
        foreach($shot in $script:KmcF6RiderFallProducerShots){Assert-KmcFinalize6RiderFallShot $n.shots $shot $root}
        return
    }
    $c=$o.companionNamesCold
    if($null-eq$c){throw 'The F6 consumer recorded no naming evidence.'}
    Assert-KmcFinalize5Labels $c.labels $shared $shared 'cold'
    if($null-eq$c.labels.PSObject.Properties['writes']-or[int]$c.labels.writes-ne0-or$null-ne$c.labels.lastOutcome){throw 'The cold names were observed after a naming-service write.'}
    Assert-KmcFinalize5Identity $c.dog $c.dog $shared 'cold dog'
    Assert-KmcFinalize5Identity $c.mammoth $c.mammoth $shared 'cold mammoth'
    Assert-KmcFinalize5Overtips $c.overtipsCold $shared $shared 'cold'
    Assert-KmcFinalize5Tack $c.tackCold 'cold unseated'
    Assert-KmcFinalize5Tack $c.tackRemounted 'cold remounted'
    Assert-KmcFinalize5DogStats @($c.tackCold,$c.tackRemounted) 'cold'
    foreach($sheet in @($c.sheetDogCold,$c.sheetMammothCold)){if($sheet.renameActive-ne$true-or$sheet.resetActive-ne$true){throw 'The cold sheet did not offer Rename and Default Name.'}}
    if($c.sheetClosed-ne$true){throw 'The cold character sheet did not close.'}
    $w=$f.cold;$s=Get-KmcFinalize6Service $w 'cold'
    if([long]$s.begun-ne0-or[long]$s.resolved-ne0-or[long]$s.pending-ne0-or@($s.records).Count-ne0){throw 'F6 the cold load replayed a fall, check or damage.'}
    if($w.secondPair.mounted-ne$false-or[string]$w.secondPair.state-cne'None'-or$w.firstPair.mounted-ne$true){throw 'F6 the cold world is not the saved one-pair world.'}
    if([string]$w.secondRider.life-cne'Conscious'-or[string]$w.secondMount.life-cne'Conscious'){throw 'F6 the cold unseated pair is not alive.'}
    $pair=Assert-KmcFinalize6PostFallArchive $o.loadedArchive $Request ([string]$w.firstRider.id) ([string]$w.secondRider.id)
    if([string]$c.dog.id-cne[string]$w.secondMount.id-or([Guid]$pair.Mount.Id).ToString('D')-cne[string]$c.mammoth.id){throw 'F6 the cold companions are not the unseated dog and the archive Mastodon.'}
    $r=$f.coldRemounted;$rs=Get-KmcFinalize6Service $r 'cold remounted'
    if($f.coldBeforeRemount.secondPair.mounted-ne$false-or$r.secondPair.mounted-ne$true-or[long]$rs.begun-ne0){throw 'F6 the ordinary Mount control did not seat the unseated rider again without a fall.'}
    foreach($shot in $script:KmcF6RiderFallConsumerShots){Assert-KmcFinalize6RiderFallShot $c.shots $shot $root}
}

# Cross-run: the consumer against its own accepted producer, by recorded values.
function Assert-KmcFinalize6ColdAgainstSource($ProducerRequest,$ProducerEvidence,$ConsumerRequest,$ConsumerEvidence){
    if([string]$ProducerEvidence.status-cne'PASS'-or[string]$ConsumerEvidence.status-cne'PASS'){throw 'Both F6 runs must have passed.'}
    if([string]$ProducerRequest.scenario-cne'chunk7-persistence-save'-or[string]$ConsumerRequest.scenario-cne'chunk7-persistence-load'-or
       [string]$ProducerRequest.persistenceCase-cne'rider-fall-integration-manual'-or[string]$ConsumerRequest.persistenceCase-cne'rider-fall-integration-manual'){throw 'The runs are not the F6 producer and consumer.'}
    # The pinned reader-only producer (Finalize6PinnedFallSource.ps1) ran on its own commit; the consumer carries its identical DLL.
    $pinned=$null-ne(Get-Variable -Name KmcF6PinnedFallSource -Scope Script -ErrorAction SilentlyContinue)-and[string]$ProducerRequest.runId-ceq$script:KmcF6PinnedFallSource.runId-and
        [string]$ProducerRequest.commit-ceq$script:KmcF6PinnedFallSource.commit
    foreach($field in 'commit','branch','dllSha256','dllMvid'){if(($field-cne'commit'-or-not$pinned)-and[string]$ProducerRequest.$field-cne[string]$ConsumerRequest.$field){throw ('The consumer ran another '+$field+' than its producer.')}}
    if($pinned-and[string]$ProducerEvidence.status-cne'PASS'){throw 'The pinned producer evidence did not pass natively.'}
    $write=$ProducerEvidence.observations.nativeWrite;$load=$ConsumerRequest.persistenceLoad
    if($null-eq$write-or[string]$write.nativeType-cne'Manual'-or[string]$load.sha256-cne[string]$write.sha256-or[long]$load.length-ne[long]$write.length-or
       [string]$load.fileName-cne[IO.Path]::GetFileName([string]$write.path)){throw 'The consumer did not load the producer''s exact Manual.'}
    $at=$ProducerEvidence.observations.riderFall.atSave;$cold=$ConsumerEvidence.observations.riderFall.cold
    foreach($role in 'firstRider','firstMount','secondRider','secondMount'){if([string]$cold.$role.id-cne[string]$at.$role.id){throw ('The cold '+$role+' is not the saved unit.')}}
    if([long]$cold.secondRider.damage-ne[long]$at.secondRider.damage-or[long]$cold.firstRider.damage-ne[long]$at.firstRider.damage){throw 'The fallen rider''s saved damage did not persist exactly (or was replayed).'}
    if([string]$cold.firstPair.ownerId-cne[string]$at.firstPair.ownerId-or[long]$cold.firstPair.generation-ne[long]$at.firstPair.generation){throw 'The Mastodon pair changed owner or generation across the reload.'}
    $n=$ProducerEvidence.observations.companionNames;$c=$ConsumerEvidence.observations.companionNamesCold
    foreach($role in 'dog','mammoth'){
        $saved=$n.atSave.$role;$coldName=$c.labels.$role
        if([string]$coldName.id-cne[string]$saved.id-or[string]$coldName.customName-cne[string]$saved.customName-or[string]$coldName.characterName-cne[string]$saved.characterName){throw ('The cold '+$role+' is not the saved unit with its saved name.')}
    }
}
