Set-StrictMode -Version Latest

function Assert-KmcAssistedNativeIdentity($Identity, $Setup) {
    Assert-KmcExactProperties $Identity @('runId','gameId','gameName','mainActorId','generation','gameMatches','playerMatches','saveManagerMatches','mainMatches','descriptorMatches','authorityMatches','exactIsolatedFreshCampaign','noForeignOrMountedCustody') 'assisted native identity'
    foreach ($key in @('runId','gameId','gameName','mainActorId')) { Assert-KmcBootstrapInventoryString $Identity.$key $Setup.$key }
    Assert-KmcOrdinaryInteger $Setup.authorityGeneration 1 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Identity.generation $Setup.authorityGeneration $Setup.authorityGeneration
    foreach ($key in @('gameMatches','playerMatches','saveManagerMatches','mainMatches','descriptorMatches','authorityMatches','exactIsolatedFreshCampaign','noForeignOrMountedCustody')) {
        Assert-KmcChunk7Boolean $Identity.$key $true
    }
}

function Assert-KmcAssistedShortfallSnapshot($Value, $Setup) {
    Assert-KmcExactProperties $Value @('frame','experience','level','experienceRatePercent','targetLevel','targetThreshold','nativeHireCost','money','readyDefault','livingIdleField','isPet','hasPet','hasDualCompanion','foreignBuilder','preservedStateSha256','identity') 'assisted shortfall snapshot'
    Assert-KmcOrdinaryInteger $Value.frame 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.experience 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.level 1 8
    Assert-KmcOrdinaryInteger $Value.experienceRatePercent 1 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.targetLevel 4 8
    Assert-KmcOrdinaryInteger $Value.targetThreshold 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.nativeHireCost 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.money 0 ([long]::MaxValue)
    foreach ($key in @('readyDefault','livingIdleField')) { Assert-KmcChunk7Boolean $Value.$key $true }
    Assert-KmcChunk7Boolean $Value.foreignBuilder $false
    foreach ($key in @('isPet','hasPet','hasDualCompanion')) {
        if ($Value.$key -isnot [bool]) { throw 'Assisted shortfall native role flags must be Boolean.' }
    }
    if ($Value.preservedStateSha256 -isnot [string] -or $Value.preservedStateSha256 -cnotmatch '^[a-f0-9]{64}$') {
        throw 'Assisted shortfall preserved-state digest is malformed.'
    }
    Assert-KmcAssistedNativeIdentity $Value.identity $Setup
}

function Assert-KmcAssistedStorySnapshot($Value, $Setup, [bool]$Selected) {
    $roles=@('controller','dialog','cue','ui','common','manager','pcDialog','part','offersList','rowsList')
    $flags=@('nativeBusy','transitionPending','dialogActive','unexpectedPopup','mouseController','cutscene','mainViewReady')
    $offerFields=@('offerObjects','offerSelectionObjects','offerGuids','offerSelectionTypes')
    $rowFields=@('rowObjects','rowAnswerObjects','rowNativeIndices','rowInitialized','rowDisabled','rowChoosingCharacter','rowInterchapter','rowActiveAndEnabled','rowActiveInHierarchy')
    Assert-KmcExactProperties $Value (@('frame','elapsedMilliseconds','mode','uiStatus','dialogGuid','cueGuid','managerKind','managerShown','pcActive','identity')+$roles+$flags+$offerFields+$rowFields) 'assisted story snapshot'
    Assert-KmcOrdinaryInteger $Value.frame 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.elapsedMilliseconds 0 300000
    if ($Value.mode -isnot [string] -or $Value.mode.Length -eq 0 -or $Value.mode.Length -gt 64 -or $Value.mode -match '[\x00-\x1f\x7f]' -or
        $null -ne $Value.uiStatus -and ($Value.uiStatus -isnot [string] -or $Value.uiStatus.Length -gt 64 -or $Value.uiStatus -match '[\x00-\x1f\x7f]')) {
        throw 'Assisted story mode or UI status is malformed.'
    }
    foreach ($key in $flags) { if ($Value.$key -isnot [bool]) { throw 'Assisted story native gate is not Boolean.' } }
    foreach ($key in @('managerShown','pcActive')) { if ($null -ne $Value.$key -and $Value.$key -isnot [bool]) { throw 'Assisted story cached UI gate is not nullable Boolean.' } }
    if ($null -ne $Value.managerKind) { Assert-KmcOrdinaryInteger $Value.managerKind 0 ([int]::MaxValue) }
    foreach ($key in @('dialogGuid','cueGuid')) { if ($null -ne $Value.$key -and ($Value.$key -isnot [string] -or $Value.$key -cnotmatch '^[a-f0-9]{32}$')) { throw 'Assisted story blueprint identity is malformed.' } }
    foreach ($key in $roles) { Assert-KmcOrdinaryInteger $Value.$key 0 2048 }
    Assert-KmcAssistedNativeIdentity $Value.identity $Setup
    foreach ($key in @($offerFields+$rowFields)) { if ($Value.$key -isnot [Array] -or $Value.$key.Count -gt 32) { throw 'Assisted story collection exceeds its actual bounded schema.' } }
    foreach ($key in $offerFields) { if ($Value.$key.Count -ne $Value.offerObjects.Count) { throw 'Assisted story offer arrays disagree.' } }
    foreach ($key in $rowFields) { if ($Value.$key.Count -ne $Value.rowObjects.Count) { throw 'Assisted story row arrays disagree.' } }
    for ($i=0; $i-lt$Value.offerObjects.Count; $i++) {
        Assert-KmcOrdinaryInteger $Value.offerObjects[$i] 0 2048
        Assert-KmcOrdinaryInteger $Value.offerSelectionObjects[$i] 0 2048
        if ($null-ne$Value.offerGuids[$i]-and($Value.offerGuids[$i]-isnot[string]-or$Value.offerGuids[$i]-cnotmatch'^[a-f0-9]{32}$')) { throw 'Assisted offered answer GUID is malformed.' }
        if ($null-ne$Value.offerSelectionTypes[$i]) { Assert-KmcOrdinaryInteger $Value.offerSelectionTypes[$i] ([int]::MinValue) ([int]::MaxValue) }
    }
    for ($i=0; $i-lt$Value.rowObjects.Count; $i++) {
        foreach($key in @('rowObjects','rowAnswerObjects')) { Assert-KmcOrdinaryInteger $Value.$key[$i] 0 2048 }
        Assert-KmcOrdinaryInteger $Value.rowNativeIndices[$i] ([int]::MinValue) ([int]::MaxValue)
        foreach($key in @('rowInitialized','rowDisabled','rowChoosingCharacter','rowInterchapter','rowActiveAndEnabled','rowActiveInHierarchy')) {
            if ($Value.$key[$i]-isnot[bool]) { throw 'Assisted row cache flags are not Boolean.' }
        }
    }
    if (-not $Selected) { return }
    Assert-KmcBootstrapInventoryString $Value.mode 'Dialog'
    # Common PC dialogues, book pages and interchapter pages are the selected native kinds; each has one ready status.
    if ($null -eq $Value.managerKind) { throw 'Selected story snapshot lacks its native dialog kind.' }
    Assert-KmcOrdinaryInteger $Value.managerKind 0 2
    Assert-KmcBootstrapInventoryString $Value.uiStatus (@('pc-cached-rows','book-rows','interchapter-rows')[[int]$Value.managerKind])
    foreach($key in @('mouseController','dialogActive','managerShown','pcActive')) { Assert-KmcChunk7Boolean $Value.$key $true }
    foreach($key in @('nativeBusy','transitionPending','unexpectedPopup')) { Assert-KmcChunk7Boolean $Value.$key $false }
    foreach($key in @('dialogGuid','cueGuid')) { if ($null-eq$Value.$key) { throw 'Selected story answer lacks its actual dialog/cue.' } }
    $roleIds=[Collections.Generic.HashSet[int]]::new()
    foreach($key in $roles) { Assert-KmcOrdinaryInteger $Value.$key 1 2048; if(-not$roleIds.Add($Value.$key)){throw 'Selected story context aliases distinct native roles.'} }
    $answers=[Collections.Generic.HashSet[int]]::new(); $guids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for($i=0;$i-lt$Value.offerObjects.Count;$i++) {
        Assert-KmcOrdinaryInteger $Value.offerObjects[$i] 1 2048; Assert-KmcOrdinaryInteger $Value.offerSelectionObjects[$i] 1 2048
        Assert-KmcOrdinaryInteger $Value.offerSelectionTypes[$i] 0 5
        if($null-eq$Value.offerGuids[$i]-or-not$answers.Add($Value.offerObjects[$i])-or-not$guids.Add($Value.offerGuids[$i])) { throw 'Selected story offers are absent or duplicate.' }
    }
    $views=[Collections.Generic.HashSet[int]]::new(); $rowAnswers=[Collections.Generic.HashSet[int]]::new(); $indices=[Collections.Generic.HashSet[int]]::new()
    for($i=0;$i-lt$Value.rowObjects.Count;$i++) {
        Assert-KmcOrdinaryInteger $Value.rowObjects[$i] 1 2048; Assert-KmcOrdinaryInteger $Value.rowAnswerObjects[$i] 1 2048; Assert-KmcOrdinaryInteger $Value.rowNativeIndices[$i] 0 32
        if(-not$answers.Contains($Value.rowAnswerObjects[$i])-or-not$views.Add($Value.rowObjects[$i])-or-not$rowAnswers.Add($Value.rowAnswerObjects[$i])-or-not$indices.Add($Value.rowNativeIndices[$i])) { throw 'Selected story rows have foreign or ambiguous ownership.' }
        Assert-KmcChunk7Boolean $Value.rowInitialized[$i] $true
        Assert-KmcChunk7Boolean $Value.rowChoosingCharacter[$i] $false
        if ([int]$Value.managerKind -ne 2) { Assert-KmcChunk7Boolean $Value.rowInterchapter[$i] $false }
    }
}

function Assert-KmcAssistedStoryEvidence($Value, $Setup) {
    Assert-KmcExactProperties $Value @('policy','cachedStateIsAuthorization','objectLabelScope','maximumChoices','maximumMilliseconds','progressMilliseconds','status','failure','nativeClickCount','attemptCount','attempts','lastObserved') 'assisted story result'
    Assert-KmcBootstrapInventoryString $Value.policy 'first-unused-eligible-row-else-root-system-button'
    Assert-KmcBootstrapInventoryString $Value.objectLabelScope 'story-driver-only'
    Assert-KmcBootstrapInventoryString $Value.status 'ReadyAtObservedFrame'
    Assert-KmcChunk7Boolean $Value.cachedStateIsAuthorization $false
    # The count cap is the driver's raised MaximumChoices; the 300000 ms total deadline and the 30000 ms
    # no-progress bound are the real safety nets and must still read exactly as before.
    Assert-KmcOrdinaryInteger $Value.maximumChoices 48 48
    Assert-KmcOrdinaryInteger $Value.maximumMilliseconds 300000 300000
    Assert-KmcOrdinaryInteger $Value.progressMilliseconds 30000 30000
    if($null-ne$Value.failure-or$Value.attempts-isnot[Array]-or$Value.attempts.Count-gt48){throw 'Failed or unbounded story setup cannot qualify.'}
    Assert-KmcOrdinaryInteger $Value.attemptCount $Value.attempts.Count $Value.attempts.Count
    Assert-KmcOrdinaryInteger $Value.nativeClickCount $Value.attempts.Count $Value.attempts.Count
    $used=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $prior=$null
    for($n=0;$n-lt$Value.attempts.Count;$n++) {
        $attempt=$Value.attempts[$n]
        Assert-KmcExactProperties $attempt @('sequence','offerIndex','rowIndex','nativeIndex','systemButton','isExitAnswer','isContinueAnswer','continueCueDisplayed','answer','row','answerGuid','nativeEntered','nativeReturned','afterReadSucceeded','failure','before','after') 'assisted story attempt'
        Assert-KmcOrdinaryInteger $attempt.sequence ($n+1) ($n+1)
        foreach($key in @('nativeEntered','nativeReturned','afterReadSucceeded')) { Assert-KmcChunk7Boolean $attempt.$key $true }
        if($attempt.systemButton-isnot[bool]){throw 'Assisted story attempt kind flag is not Boolean.'}
        # The observed native kind of the clicked answer. The product classifies four DialogRoot system
        # answers by reference identity: IsExitAnswer is Exit/InterchapterExit, IsContinueAnswer is
        # Continue/InterchapterContinue, so the two are mutually exclusive and a cached PC row answer is
        # neither. These are recorded observations, not derivations from the after-read.
        foreach($key in @('isExitAnswer','isContinueAnswer','continueCueDisplayed')) { if($attempt.$key-isnot[bool]){throw 'Assisted story attempt native answer-kind flag is not Boolean.'} }
        if($attempt.isExitAnswer-and$attempt.isContinueAnswer){throw 'A story attempt cannot click a native exit and continue answer at once.'}
        if($null-ne$attempt.failure){throw 'A partially failed native click cannot qualify story completion.'}
        Assert-KmcAssistedStorySnapshot $attempt.before $Setup $true
        Assert-KmcAssistedStorySnapshot $attempt.after $Setup $false
        $before=$attempt.before; $after=$attempt.after
        Assert-KmcOrdinaryInteger $after.frame $before.frame ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $after.elapsedMilliseconds $before.elapsedMilliseconds 300000
        if($null-ne$prior) {
            Assert-KmcOrdinaryInteger $before.frame ([Math]::Max(([long]$prior.before.frame+1),[long]$prior.after.frame)) ([int]::MaxValue)
            Assert-KmcOrdinaryInteger $before.elapsedMilliseconds $prior.after.elapsedMilliseconds 300000
        }
        # Recompute the product's own selection policy: in the native offered order, the FIRST eligible
        # cached row whose (controller, dialog, cue, answer) tuple this run has not already used. The
        # unused-tuple test is the same one the dedupe below applies, so a story hub that re-presents the
        # same answers at one cue legitimately records a click on offer 1..n-1 and is accepted here, while
        # an attempt that skipped an eligible unused row, or replayed a used tuple, is still refused.
        $selectedOffer=-1; $selectedRow=-1
        for($i=0;$i-lt$before.offerObjects.Count;$i++) {
            for($j=0;$j-lt$before.rowObjects.Count;$j++) {
                if($before.rowAnswerObjects[$j]-eq$before.offerObjects[$i]-and-not$before.rowDisabled[$j]-and$before.rowActiveAndEnabled[$j]-and$before.rowActiveInHierarchy[$j]-and$selectedOffer-lt0-and-not$used.Contains((@($before.controller,$before.dialog,$before.cue,$before.offerObjects[$i])-join':'))) { $selectedOffer=$i; $selectedRow=$j }
            }
        }
        # A cue is advanced two ways. A cached TempOptionUI row click records its real row index and
        # native UI index; the shared DialogueSystemButton click (continue or the terminal Exit) is
        # taken only when the cue materializes NO cached row at all, so it records none. Each kind is
        # validated as itself and neither may masquerade as the other. The zero-rows requirement is
        # exactly the product's own (Rows.Length==0), so a present-but-ineligible row also refuses.
        if($attempt.systemButton) {
            if($before.rowObjects.Count-ne0){throw 'A system-button story attempt cannot bypass an available native row.'}
            if(-not(Test-KmcExactJsonInteger $attempt.offerIndex)-or$attempt.offerIndex-lt0-or$attempt.offerIndex-ge$before.offerObjects.Count){throw 'A system-button story attempt did not click an offered native answer.'}
            if($before.offerSelectionTypes[$attempt.offerIndex]-eq4){throw 'A system-button story attempt cannot drive Manual character selection.'}
            # The product reaches the shared DialogueSystemButton only for a root DialogRoot system answer,
            # and only a terminal Exit answer may be taken without the displayed native continue cue.
            if(-not($attempt.isExitAnswer-or$attempt.isContinueAnswer)){throw 'A system-button story attempt must click a root native system answer.'}
            if($attempt.isContinueAnswer-and-not$attempt.continueCueDisplayed){throw 'A continue system-button story attempt requires the displayed native continue cue.'}
            Assert-KmcOrdinaryInteger $attempt.rowIndex (-1) (-1)
            Assert-KmcOrdinaryInteger $attempt.nativeIndex (-1) (-1)
            Assert-KmcOrdinaryInteger $attempt.row 0 0
            Assert-KmcOrdinaryInteger $attempt.answer $before.offerObjects[$attempt.offerIndex] $before.offerObjects[$attempt.offerIndex]
            Assert-KmcBootstrapInventoryString $attempt.answerGuid $before.offerGuids[$attempt.offerIndex]
        } else {
            if($selectedOffer-lt0-or$before.offerSelectionTypes[$selectedOffer]-eq4){throw 'Story attempt did not select the first unused available non-Manual native row.'}
            Assert-KmcOrdinaryInteger $attempt.offerIndex $selectedOffer $selectedOffer
            Assert-KmcOrdinaryInteger $attempt.rowIndex $selectedRow $selectedRow
            Assert-KmcOrdinaryInteger $attempt.answer $before.offerObjects[$selectedOffer] $before.offerObjects[$selectedOffer]
            Assert-KmcOrdinaryInteger $attempt.row $before.rowObjects[$selectedRow] $before.rowObjects[$selectedRow]
            Assert-KmcOrdinaryInteger $attempt.nativeIndex $before.rowNativeIndices[$selectedRow] $before.rowNativeIndices[$selectedRow]
            Assert-KmcBootstrapInventoryString $attempt.answerGuid $before.offerGuids[$selectedOffer]
        }
        $key=(@($before.controller,$before.dialog,$before.cue,$attempt.answer)-join':')
        if(-not$used.Add($key)){throw 'Story setup clicked an already consumed context.'}
        $prior=$attempt
    }
    Assert-KmcAssistedStorySnapshot $Value.lastObserved $Setup $false
    $last=$Value.lastObserved
    Assert-KmcBootstrapInventoryString $last.mode 'Default'
    foreach($key in @('nativeBusy','transitionPending','dialogActive','unexpectedPopup','cutscene')) { Assert-KmcChunk7Boolean $last.$key $false }
    foreach($key in @('mouseController','mainViewReady')) { Assert-KmcChunk7Boolean $last.$key $true }
    if($null-ne$prior) {
        Assert-KmcOrdinaryInteger $last.frame ([Math]::Max(([long]$prior.before.frame+1),[long]$prior.after.frame)) ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $last.elapsedMilliseconds $prior.after.elapsedMilliseconds 300000
    }
}

function Assert-KmcChunk7AssistedSetupEvidence($Value, $Expected) {
    Assert-KmcExactProperties $Value @('contract','fixtureSetupOnly','ordinaryAcquisitionProven','ordinaryProgressionProven','smallPoseQualified','runId','gameId','gameName','mainActorId','authorityGeneration','story','experience','hireFunds','shortfallFailed') 'assisted fixture setup evidence'
    Assert-KmcBootstrapInventoryString $Value.contract 'assisted-fixture-setup-v1'
    Assert-KmcChunk7Boolean $Value.fixtureSetupOnly $true
    foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','smallPoseQualified','shortfallFailed')) { Assert-KmcChunk7Boolean $Value.$key $false }
    foreach($key in @('runId','gameId','gameName','mainActorId')) { Assert-KmcBootstrapInventoryString $Value.$key $Expected.$key }
    if($Value.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or[string]::IsNullOrWhiteSpace($Value.gameName)-or$Value.gameName.Length-gt256-or$Value.gameName-match'[\x00-\x1f\x7f]'){throw 'Assisted setup run/campaign name is malformed.'}
    foreach($key in @('gameId','mainActorId')) { Assert-KmcOrdinaryGuid $Value.$key }
    Assert-KmcOrdinaryInteger $Expected.authorityGeneration 1 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.authorityGeneration $Expected.authorityGeneration $Expected.authorityGeneration
    Assert-KmcAssistedStoryEvidence $Value.story $Value
    Assert-KmcAssistedShortfallEffect $Value.experience 'native-xp-shortfall' $Value
    Assert-KmcAssistedShortfallEffect $Value.hireFunds 'native-hire-money-shortfall' $Value
    Assert-KmcOrdinaryInteger $Value.experience.before.frame $Value.story.lastObserved.frame ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.hireFunds.before.frame $Value.experience.after.frame ([int]::MaxValue)
}

function Assert-KmcAssistedShortfallEffect($Value, [string]$Kind, $Setup) {
    if ($Kind -cne 'native-xp-shortfall' -and $Kind -cne 'native-hire-money-shortfall') { throw 'Unknown assisted shortfall kind.' }
    Assert-KmcExactProperties $Value @('kind','delta','attempted','nativeEntered','nativeReturned','afterReadSucceeded','noOp','failure','before','after') 'assisted shortfall effect'
    Assert-KmcBootstrapInventoryString $Value.kind $Kind
    if ($null -ne $Value.failure) { throw 'Failed native assistance cannot qualify a fixture source.' }
    $maximum=if($Kind-ceq'native-xp-shortfall'){200000L}else{100000L}
    Assert-KmcOrdinaryInteger $Value.delta 0 $maximum
    Assert-KmcAssistedShortfallSnapshot $Value.before $Setup
    Assert-KmcAssistedShortfallSnapshot $Value.after $Setup
    $before=$Value.before; $after=$Value.after
    Assert-KmcOrdinaryInteger $after.frame $before.frame ([int]::MaxValue)
    Assert-KmcChunk7Boolean $Value.afterReadSucceeded $true
    $mutated=$Value.delta-gt0
    foreach($key in @('attempted','nativeEntered','nativeReturned')) { Assert-KmcChunk7Boolean $Value.$key $mutated }
    Assert-KmcChunk7Boolean $Value.noOp (-not $mutated)
    foreach($key in @('level','experienceRatePercent','targetLevel','targetThreshold','nativeHireCost','preservedStateSha256','isPet','hasPet','hasDualCompanion','readyDefault','livingIdleField','foreignBuilder','identity')) {
        if ((ConvertTo-KmcChunk7Canonical $before.$key) -cne (ConvertTo-KmcChunk7Canonical $after.$key)) {
            throw ('Native assistance changed a preserved field: '+$key)
        }
    }
    if ($Kind -ceq 'native-xp-shortfall') {
        $expected=[Math]::Max(0L,([long]$before.targetThreshold-[long]$before.experience))
        if ($Value.delta -ne $expected -or [decimal]$after.experience -ne ([decimal]$before.experience+[decimal]$Value.delta) -or $after.money -ne $before.money) {
            throw 'Native experience effect differs from its exact table-derived shortfall.'
        }
        if ($mutated) {
            Assert-KmcOrdinaryInteger $before.experienceRatePercent 100 100
            foreach($key in @('isPet','hasPet','hasDualCompanion')) { Assert-KmcChunk7Boolean $before.$key $false }
            if ([decimal]$Value.delta * 100 -gt [int]::MaxValue) { throw 'Native experience scaling would overflow its checked integer.' }
        }
    }
    else {
        Assert-KmcOrdinaryInteger $before.level 4 8
        Assert-KmcOrdinaryInteger $before.nativeHireCost 1 ([int]::MaxValue)
        $expected=[Math]::Max(0L,([long]$before.nativeHireCost-[long]$before.money))
        if ($Value.delta -ne $expected -or [decimal]$after.money -ne ([decimal]$before.money+[decimal]$Value.delta) -or $after.experience -ne $before.experience) {
            throw 'Native funds effect differs from its exact positive hire-cost shortfall.'
        }
    }
    if (-not $mutated -and (ConvertTo-KmcChunk7Canonical $before) -cne (ConvertTo-KmcChunk7Canonical $after)) {
        throw 'A no-op setup step invented an additional native observation or mutation.'
    }
}


function Assert-KmcAssistedObjectValue($Value,[string[]]$Properties,[string]$Description) {
    if($null-eq$Value-or$Value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw ($Description+' must be a JSON object.')}
    Assert-KmcExactProperties $Value $Properties $Description
}
function Assert-KmcAssistedGuid32($Value) {
    if($Value-isnot[string]-or$Value-cnotmatch'^[a-f0-9]{32}$'){throw 'Assisted native blueprint identity is malformed.'}
}
function Assert-KmcAssistedLabel($Value,$Labels) {
    Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue)
    if($Value-eq0){throw 'Assisted retained object label is zero.'}
    if($null-ne$Labels-and-not$Labels.Contains([int]$Value)){throw 'Assisted native object is outside its retained registry.'}
}
function Assert-KmcAssistedArray($Value,[int]$Minimum,[int]$Maximum,[string]$Description) {
    if($Value-isnot[Array]-or$Value.Rank-ne1-or$Value.Count-lt$Minimum-or$Value.Count-gt$Maximum){throw ($Description+' is absent or outside its collection bound.')}
}
function Assert-KmcAssistedSame($Left,$Right,[string]$Description) {
    if((ConvertTo-KmcChunk7Canonical $Left)-cne(ConvertTo-KmcChunk7Canonical $Right)){throw ($Description+' changed.')}
}
function Assert-KmcAssistedActorRows($Rows,[string]$MainId,$Labels,[bool]$IncludeMainExperience,[int]$MaximumActors=44) {
    Assert-KmcAssistedArray $Rows 1 $MaximumActors 'assisted native actors'
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $objects=[Collections.Generic.HashSet[int]]::new();$facts=[Collections.Generic.HashSet[int]]::new();$totalFacts=0
    foreach($actor in $Rows) {
        $keys=@('id','object','descriptor','view','blueprint','masterId','petId','inState','inGame','conscious','dead','inCombat','commandsEmpty','queuedCommands','characterLevel','classes','features','buffs','actions')
        if($IncludeMainExperience-or$actor.id-cne$MainId){$keys+=,'experience'}
        Assert-KmcAssistedObjectValue $actor $keys 'assisted preserved actor'
        Assert-KmcOrdinaryGuid $actor.id
        if(-not$ids.Add($actor.id)){throw 'Assisted preserved actor repeats an identity.'}
        foreach($key in @('object','descriptor')){Assert-KmcAssistedLabel $actor.$key $Labels;if($facts.Contains([int]$actor.$key)-or-not$objects.Add($actor.$key)){throw 'Assisted preserved actor aliases an object, descriptor or fact.'}}
        Assert-KmcOrdinaryInteger $actor.view ([int]::MinValue) ([int]::MaxValue)
        if($actor.view-ne0){Assert-KmcAssistedLabel $actor.view $Labels;if($facts.Contains([int]$actor.view)-or-not$objects.Add($actor.view)){throw 'Assisted preserved actor aliases a view or fact.'}}
        Assert-KmcAssistedGuid32 $actor.blueprint
        foreach($key in @('masterId','petId')){if($null-ne$actor.$key){Assert-KmcOrdinaryGuid $actor.$key;if($actor.$key-ceq$actor.id){throw 'Assisted native companion owns itself.'}}}
        foreach($key in @('inState','inGame','conscious','dead','inCombat','commandsEmpty')){if($actor.$key-isnot[bool]){throw 'Assisted actor native state must remain Boolean.'}}
        Assert-KmcOrdinaryInteger $actor.queuedCommands 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $actor.characterLevel 0 20
        if($IncludeMainExperience-or$actor.id-cne$MainId){Assert-KmcOrdinaryInteger $actor.experience 0 ([int]::MaxValue)}
        Assert-KmcAssistedArray $actor.classes 0 20 'assisted native classes'
        $classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$sum=0L
        foreach($c in $actor.classes){
            Assert-KmcAssistedObjectValue $c @('guid','level') 'assisted native class'
            Assert-KmcAssistedGuid32 $c.guid;Assert-KmcOrdinaryInteger $c.level 1 20
            if(-not$classes.Add($c.guid)){throw 'Assisted native class repeats.'};$sum+=$c.level
        }
        if($sum-ne$actor.characterLevel){throw 'Assisted native class ranks differ from character level.'}
        Assert-KmcAssistedArray $actor.features 0 128 'assisted native features'
        Assert-KmcAssistedArray $actor.buffs 0 64 'assisted native buffs'
        $totalFacts+=$actor.features.Count+$actor.buffs.Count
        if($totalFacts-gt1024){throw 'Assisted aggregate native facts exceed their bound.'}
        foreach($f in $actor.features){
            Assert-KmcAssistedObjectValue $f @('object','guid','source','rank') 'assisted feature'
            Assert-KmcAssistedLabel $f.object $Labels;Assert-KmcAssistedGuid32 $f.guid
            if($null-ne$f.source){Assert-KmcAssistedGuid32 $f.source}
            Assert-KmcOrdinaryInteger $f.rank 0 ([int]::MaxValue)
            if($objects.Contains([int]$f.object)-or-not$facts.Add($f.object)){throw 'Assisted native feature/buff object repeats or aliases an actor.'}
        }
        foreach($b in $actor.buffs){
            Assert-KmcAssistedObjectValue $b @('object','guid') 'assisted buff'
            Assert-KmcAssistedLabel $b.object $Labels;Assert-KmcAssistedGuid32 $b.guid
            if($objects.Contains([int]$b.object)-or-not$facts.Add($b.object)){throw 'Assisted native feature/buff object repeats or aliases an actor.'}
        }
        Assert-KmcAssistedObjectValue $actor.actions @('move','standard','swift','prepared','reactions','reactionAllowance','initiative') 'assisted native actions'
        foreach($key in @('move','standard','swift','initiative')){if(-not(Test-KmcFiniteJsonNumber $actor.actions.$key)){throw 'Assisted native action observation is not finite numeric data.'}}
        if($actor.actions.prepared-isnot[bool]){throw 'Assisted native preparation observation is not Boolean.'}
        foreach($key in @('reactions','reactionAllowance')){Assert-KmcOrdinaryInteger $actor.actions.$key ([int]::MinValue) ([int]::MaxValue)}
    }
    if(-not$ids.Contains($MainId)){throw 'Assisted preservation lost its exact main actor.'}
}
function Assert-KmcAssistedPreservedState($Value,$Expected,$Labels,[int]$Writes) {
    Assert-KmcAssistedObjectValue $Value @('game','player','saveManager','state','areaState','mainState','mainActor','mainActorId','gameId','gameName','area','mode','gameTimeTicks','experienceRatePercent','nativeCasts','snapshots','completedSaves','semanticRestores','presentationRestores','nativePostLoads','mountedOwners','mountedSlots','partyCharacters','remoteCompanions','actors') 'assisted preserved state'
    foreach($key in @('mainActorId','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Value.$key $Expected.$key}
    Assert-KmcBootstrapInventoryString $Value.mode 'Default'
    $roles=[Collections.Generic.HashSet[int]]::new()
    foreach($key in @('game','player','saveManager','state','areaState','mainState','mainActor')){Assert-KmcAssistedLabel $Value.$key $Labels;if(-not$roles.Add($Value.$key)){throw 'Assisted preserved native roles alias.'}}
    foreach($key in @('nativeCasts','semanticRestores','presentationRestores','nativePostLoads','mountedOwners','mountedSlots')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
    foreach($key in @('snapshots','completedSaves')){Assert-KmcOrdinaryInteger $Value.$key $Writes $Writes}
    Assert-KmcOrdinaryInteger $Value.gameTimeTicks 0 ([long]::MaxValue)
    Assert-KmcOrdinaryInteger $Value.experienceRatePercent 1 ([int]::MaxValue)
    Assert-KmcAssistedActorRows $Value.actors $Expected.mainActorId $Labels $false
    $main=@($Value.actors|Where-Object id -CEQ $Expected.mainActorId)
    Assert-KmcOrdinaryInteger $main[0].object $Value.mainActor $Value.mainActor
    Assert-KmcAssistedArray $Value.partyCharacters 1 6 'assisted field characters'
    Assert-KmcAssistedArray $Value.remoteCompanions 0 32 'assisted remote companions'
    $roster=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($member in @($Value.partyCharacters+$Value.remoteCompanions)){
        Assert-KmcAssistedObjectValue $member @('id','object') 'assisted native roster member'
        Assert-KmcOrdinaryGuid $member.id;Assert-KmcAssistedLabel $member.object $Labels
        if(-not$roster.Add($member.id)){throw 'Assisted field and remote character rosters overlap.'}
        $match=@($Value.actors|Where-Object id -CEQ $member.id)
        if($match.Count-ne1-or$match[0].object-ne$member.object){throw 'Assisted native roster member lacks its exact actor object.'}
    }
    if($Expected.mainActorId-cnotin@($Value.partyCharacters.id)){throw 'Assisted main actor is not a field character.'}
    $remoteIds=@($Value.remoteCompanions|ForEach-Object id)

    $allowedActors=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($member in @($Value.partyCharacters+$Value.remoteCompanions)){[void]$allowedActors.Add($member.id)}
    foreach($member in $Value.partyCharacters){
        $character=@($Value.actors|Where-Object id -CEQ $member.id)[0]
        if($null-ne$character.masterId){throw 'Assisted field character has a companion master.'}
        if($null-ne$character.petId){
            $pet=@($Value.actors|Where-Object id -CEQ $character.petId)
            if($pet.Count-ne1-or$pet[0].masterId-cne$character.id-or-not$allowedActors.Add($character.petId)){throw 'Assisted field pet is missing, nonreciprocal or overlaps another native member.'}
        }
    }
    if(@($Value.actors|Where-Object{$_.id-cnotin$remoteIds}).Count-gt12){throw 'Assisted native field exceeds twelve actors.'}
    foreach($actor in $Value.actors){
        if(-not$allowedActors.Contains($actor.id)){throw 'Assisted actor is outside the exact field/pet/remote graph.'}
        foreach($key in @('object','descriptor','view')){
            $label=[int]$actor.$key
            $isMainObject=$key-ceq'object'-and$actor.id-ceq$Expected.mainActorId-and$label-eq$Value.mainActor
            if($label-ne0-and$roles.Contains($label)-and-not$isMainObject){throw 'Assisted actor role aliases a native world owner.'}
        }
        foreach($fact in @($actor.features+$actor.buffs)){if($roles.Contains([int]$fact.object)){throw 'Assisted native fact aliases a world owner.'}}
    }

    foreach($actor in $Value.actors){
        if($actor.id-cnotin$remoteIds){
            foreach($key in @('inState','inGame','conscious','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
            foreach($key in @('dead','inCombat')){Assert-KmcChunk7Boolean $actor.$key $false}
            Assert-KmcOrdinaryInteger $actor.queuedCommands 0 0
            Assert-KmcAssistedLabel $actor.view $Labels
        }
    }
}
function Assert-KmcChunk7AssistedPreservation($Value,$Setup,$Expected,$Labels,[int]$LevelupWrites) {
    Assert-KmcAssistedObjectValue $Value @('contract','excluded','maximumSnapshots','rows') 'assisted shortfall preservation'
    Assert-KmcBootstrapInventoryString $Value.contract 'assisted-main-shortfall-preservation-v1'
    Assert-KmcAssistedSame $Value.excluded @('mainExperience','partyMoney') 'assisted preservation exclusions'
    Assert-KmcOrdinaryInteger $Value.maximumSnapshots 12 12
    Assert-KmcOrdinaryInteger $LevelupWrites 0 1
    $xpCount=if($Setup.experience.noOp){1}else{3};$moneyCount=if($Setup.hireFunds.noOp){1}else{3}
    $count=$xpCount+$moneyCount
    Assert-KmcAssistedArray $Value.rows $count $count 'assisted actual preservation reads'
    $index=0;$priorFrame=0;$firstState=$null
    foreach($kind in @('experience','hire-funds')){
        $effect=if($kind-ceq'experience'){$Setup.experience}else{$Setup.hireFunds}
        $effectCount=if($effect.noOp){1}else{3};$beforeState=$null;$beforeHash=$null
        $writes=if($kind-ceq'experience'){1}else{1+$LevelupWrites}
        for($i=0;$i-lt$effectCount;$i++){
            $row=$Value.rows[$index]
            Assert-KmcAssistedObjectValue $row @('sequence','frame','purpose','state','canonicalJson','sha256') 'assisted preservation row'
            Assert-KmcOrdinaryInteger $row.sequence ($index+1) ($index+1)
            Assert-KmcOrdinaryInteger $row.frame $priorFrame ([int]::MaxValue)
            $phase=if($i-eq0){'before'}elseif($i-eq1){'entry'}else{'after'}
            Assert-KmcBootstrapInventoryString $row.purpose ($kind+'-'+$phase)
            if($row.canonicalJson-isnot[string]-or$row.canonicalJson.Length-eq0-or$row.canonicalJson-cnotmatch'^\{'-or[Text.Encoding]::UTF8.GetByteCount($row.canonicalJson)-gt256KB){throw 'Assisted canonical preservation text is absent or unbounded.'}
            Assert-KmcJsonObjectMembersUnique $row.canonicalJson 'assisted canonical preservation'
            $decoded=$row.canonicalJson|ConvertFrom-Json
            Assert-KmcAssistedSame $decoded $row.state 'assisted canonical preservation payload'
            $sha=[Security.Cryptography.SHA256]::Create()
            try{$actual=[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false,$true).GetBytes($row.canonicalJson))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
            Assert-KmcBootstrapInventoryString $row.sha256 $actual
            Assert-KmcAssistedPreservedState $row.state $Expected $Labels $writes
            $snapshot=if($i-eq2){$effect.after}else{$effect.before}
            Assert-KmcOrdinaryInteger $row.state.experienceRatePercent $snapshot.experienceRatePercent $snapshot.experienceRatePercent
            $capturedMain=@($row.state.actors|Where-Object id -CEQ $Expected.mainActorId)[0]
            Assert-KmcOrdinaryInteger $capturedMain.characterLevel $snapshot.level $snapshot.level
            if($i-eq0){
                $beforeState=$row.state;$beforeHash=$row.sha256
                Assert-KmcOrdinaryInteger $row.frame $effect.before.frame $effect.before.frame
                Assert-KmcBootstrapInventoryString $row.sha256 $effect.before.preservedStateSha256
                if($null-eq$firstState){$firstState=$row.state}else{
                    foreach($key in @('game','player','saveManager','state','areaState','mainState','mainActor','mainActorId','gameId','gameName','area','partyCharacters','remoteCompanions')){Assert-KmcAssistedSame $row.state.$key $firstState.$key ('assisted shortfall world identity '+$key)}
                }
            }else{
                Assert-KmcAssistedSame $row.state $beforeState 'assisted native shortfall preserved state'
                Assert-KmcBootstrapInventoryString $row.sha256 $beforeHash
                if($i-eq2){Assert-KmcOrdinaryInteger $row.frame $effect.after.frame $effect.after.frame;Assert-KmcBootstrapInventoryString $row.sha256 $effect.after.preservedStateSha256}
                else{Assert-KmcOrdinaryInteger $row.frame $effect.before.frame $effect.before.frame}
            }
            $priorFrame=$row.frame;$index++
        }
    }
}


function Test-KmcChunk7AssistedFixtureScenario([string]$Scenario){$Scenario-ceq'chunk7-assisted-fixture-preparation'}
function Get-KmcChunk7AssistedFixtureRows {
    @('chunk7-assisted-fixture-preparation','C7-assisted-preflight','C7-assisted-native-start','C7-assisted-story','C7-assisted-setup','C7-assisted-native-choices','C7-assisted-manual','C7-assisted-cleanup','C7-assisted-runtime','C7-assisted-interrupted')
}
function Assert-KmcChunk7AssistedFixtureSelection($Request) {
    # The runner builds its request as an ordered dictionary before serialization;
    # dictionary keys are not PSObject properties, so normalize before detection.
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $has=$null-ne$Request.PSObject.Properties['assistedFixture']
    if(-not(Test-KmcChunk7AssistedFixtureScenario $Request.scenario)){if($has){throw 'Assisted fixture selection is forbidden under another scenario.'};return}
    if(-not$has){throw 'Exact assisted fixture selection is required.'}
    $s=$Request.assistedFixture
    Assert-KmcAssistedObjectValue $s @('case','ownerAuthorizationSha256','baseGameOnly','assistedSetupOnly','storyPolicy','assistancePolicy','expectedNativeLoadCount','maximumNativeWriteCount','targetRangerLevel','maximumTargetLevel','maximumStoryChoices','maximumStorySeconds','maximumExperienceShortfall','maximumMoneyShortfall','nativeInitialAutoLeaf','nativeManualName') 'assisted fixture selection'
    foreach($pair in @(@('case','authored-main-ranger4-manual'),@('ownerAuthorizationSha256','052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e'),@('storyPolicy','first-unused-eligible-row-else-root-system-button'),@('assistancePolicy','native-main-ranger4-and-one-hire-shortfalls'),@('nativeInitialAutoLeaf','Auto_1.zks'),@('nativeManualName','KMC_C7_ASSISTED_FIXTURE'))){Assert-KmcBootstrapInventoryString $s.($pair[0]) $pair[1]}
    foreach($key in @('baseGameOnly','assistedSetupOnly')){Assert-KmcChunk7Boolean $s.$key $true}
    # maximumStoryChoices declares the driver's own raised count cap; maximumStorySeconds, the 300 s total
    # deadline that remains the real safety net, is unchanged.
    foreach($pair in @(@('expectedNativeLoadCount',0),@('maximumNativeWriteCount',3),@('targetRangerLevel',4),@('maximumTargetLevel',8),@('maximumStoryChoices',48),@('maximumStorySeconds',300),@('maximumExperienceShortfall',200000),@('maximumMoneyShortfall',100000))){Assert-KmcOrdinaryInteger $s.($pair[0]) $pair[1] $pair[1]}
}
function Get-KmcAssistedSaveRoot($Request) {
    if($Request.runId-isnot[string]-or$Request.runId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'){throw 'Assisted fixture run ID is malformed.'}
    $lab=Get-KmcLabRoot
    Assert-KmcBootstrapInventoryString $Request.evidenceRoot (Join-Path (Join-Path $lab 'runtime-evidence') $Request.runId)
    Join-Path (Join-Path (Join-Path $lab 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
}
function Assert-KmcAssistedAuthority($Value,[string]$Root,[long]$Generation,$Expected,[string]$Phase,[int]$Writes,[int]$LevelupWrites,[int]$Reservations,[int]$ManualNumber) {
    Assert-KmcAssistedObjectValue $Value @('root','generation','phase','maximumNativeLoads','maximumNativeWrites','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites','freezeCount','windowOpen','frozenGameId','frozenGameName','manualNumber','manualLeaf','reservationCount','levelupWrites') 'assisted native authority'
    Assert-KmcBootstrapInventoryString $Value.root $Root
    Assert-KmcOrdinaryInteger $Generation 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Value.generation $Generation $Generation
    Assert-KmcBootstrapInventoryString $Value.phase $Phase
    foreach($key in @('maximumNativeLoads','admittedNativeLoads','authorizedLoads','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
    Assert-KmcOrdinaryInteger $Value.maximumNativeWrites 3 3
    Assert-KmcOrdinaryInteger $Writes 0 3
    foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Value.$key $Writes $Writes}
    Assert-KmcChunk7Boolean $Value.windowOpen $false
    $freeze=if($Writes-eq0){0}else{1};Assert-KmcOrdinaryInteger $Value.freezeCount $freeze $freeze
    foreach($key in @('gameId','gameName')){
        $field=if($key-ceq'gameId'){'frozenGameId'}else{'frozenGameName'}
        if($Writes-eq0){if($null-ne$Value.$field){throw 'Unused assisted scope already claims a minted campaign.'}}else{Assert-KmcBootstrapInventoryString $Value.$field $Expected.$key}
    }
    Assert-KmcOrdinaryInteger $Value.levelupWrites $LevelupWrites $LevelupWrites
    Assert-KmcOrdinaryInteger $Value.reservationCount $Reservations $Reservations
    Assert-KmcOrdinaryInteger $Value.manualNumber $ManualNumber $ManualNumber
    if($ManualNumber-eq0){if($null-ne$Value.manualLeaf){throw 'Assisted scope invented a Manual reservation.'}}else{Assert-KmcBootstrapInventoryString $Value.manualLeaf ('Manual_'+$ManualNumber.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ASSISTED_FIXTURE.zks')}
}
function Assert-KmcAssistedIdentityBridge($Bridge,[string]$ObserverDomain,[string[]]$Roles,$Labels) {
    Assert-KmcAssistedObjectValue $Bridge @('contract','hostDomain','observerDomain','roles') 'assisted exact-object domain bridge'
    Assert-KmcBootstrapInventoryString $Bridge.contract 'exact-object-label-domains-v1'
    Assert-KmcBootstrapInventoryString $Bridge.hostDomain 'retained-host-2048'
    Assert-KmcBootstrapInventoryString $Bridge.observerDomain $ObserverDomain
    Assert-KmcAssistedObjectValue $Bridge.roles $Roles 'assisted object bridge roles'
    $hostIds=[Collections.Generic.HashSet[int]]::new();$observerIds=[Collections.Generic.HashSet[int]]::new()
    foreach($role in $Roles){
        $entry=$Bridge.roles.$role;Assert-KmcAssistedObjectValue $entry @('host','observer') 'assisted bridged object'
        Assert-KmcAssistedLabel $entry.host $Labels
        if($ObserverDomain-ceq'retained-bootstrap-128'){Assert-KmcNativeBootstrapObject $entry.observer}else{
            Assert-KmcOrdinaryInteger $entry.observer ([int]::MinValue) ([int]::MaxValue)
            if($entry.observer-eq0){throw 'Assisted observer object hash is absent.'}
        }
        if(-not$hostIds.Add($entry.host)-or-not$observerIds.Add($entry.observer)){throw 'Assisted identity bridge has ambiguous distinct roles.'}
    }
}
function Assert-KmcAssistedWorld($Value,$Expected,$Labels,[long]$MinimumFrame,[bool]$Ready) {
    Assert-KmcAssistedObjectValue $Value @('frame','game','player','saveManager','mainActor','mainActorId','gameId','gameName','area','mode','nativeLoading','authority','party') 'assisted native world'
    Assert-KmcOrdinaryInteger $Value.frame $MinimumFrame ([int]::MaxValue)
    foreach($key in @('game','player','saveManager','mainActor')){Assert-KmcAssistedLabel $Value.$key $Labels}
    foreach($key in @('mainActorId','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Value.$key $Expected.$key}
    Assert-KmcChunk7Boolean $Value.nativeLoading $false
    if($Value.mode-isnot[string]-or$Value.mode.Length-eq0-or$Value.mode.Length-gt64){throw 'Assisted native world mode is malformed.'}
    if($Ready){Assert-KmcBootstrapInventoryString $Value.mode 'Default'}
    Assert-KmcAssistedActorRows $Value.party $Expected.mainActorId $Labels $false 12
    $main=@($Value.party|Where-Object id -CEQ $Expected.mainActorId)[0]
    Assert-KmcOrdinaryInteger $main.object $Value.mainActor $Value.mainActor
    if($Ready){foreach($actor in $Value.party){
        foreach($key in @('inState','inGame','conscious','commandsEmpty')){Assert-KmcChunk7Boolean $actor.$key $true}
        foreach($key in @('dead','inCombat')){Assert-KmcChunk7Boolean $actor.$key $false}
        Assert-KmcOrdinaryInteger $actor.queuedCommands 0 0;Assert-KmcAssistedLabel $actor.view $Labels
    }}
}
function Assert-KmcAssistedWrite($Value,$Request,$Expected,$Labels,[string]$Root,[int]$Sequence,[string]$Phase,[long]$MinimumFrame) {
    Assert-KmcAssistedObjectValue $Value @('phase','frame','path','fileName','internalName','gameId','gameName','area','type','operation','autoLevelup','length','sha256','lastWriteTimeUtcTicks','requestedDescriptor','preparedDescriptor','completionSequence','snapshots','completedSaves','authority','contentInspected','coldLoadQualified') 'assisted native write'
    Assert-KmcBootstrapInventoryString $Value.phase $Phase
    Assert-KmcOrdinaryInteger $Value.frame $MinimumFrame ([int]::MaxValue)
    foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Value.$key $Expected.$key}
    foreach($key in @('completionSequence','snapshots','completedSaves')){Assert-KmcOrdinaryInteger $Value.$key $Sequence $Sequence}
    Assert-KmcBootstrapInventoryString $Value.operation 'None'
    foreach($key in @('contentInspected','coldLoadQualified')){Assert-KmcChunk7Boolean $Value.$key $false}
    # The level-up request carries this flag, while native PrepareSave writes
    # the settled Auto descriptor with the flag cleared.
    Assert-KmcChunk7Boolean $Value.autoLevelup $false
    foreach($key in @('requestedDescriptor','preparedDescriptor')){Assert-KmcAssistedLabel $Value.$key $Labels}
    if($Value.requestedDescriptor-eq$Value.preparedDescriptor){throw 'Assisted native PrepareSave did not create a distinct descriptor.'}
    if($Value.sha256-isnot[string]-or$Value.sha256-cnotmatch'^[a-f0-9]{64}$'-or$Value.sha256-ceq$Request.fixture.working.sha256-or$Value.sha256-ceq$Request.fixture.baseline.sha256){throw 'Assisted output is not a fresh native archive identity.'}
    Assert-KmcOrdinaryInteger $Value.length 1 268435456
    Assert-KmcOrdinaryInteger $Value.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
    if($Phase-ceq'final-manual'){
        Assert-KmcBootstrapInventoryString $Value.type 'Manual';Assert-KmcBootstrapInventoryString $Value.internalName 'KMC_C7_ASSISTED_FIXTURE'
        Assert-KmcOrdinaryInteger $Value.authority.manualNumber 1 ([int]::MaxValue)
        $leaf='Manual_'+$Value.authority.manualNumber.ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ASSISTED_FIXTURE.zks'
    }else{
        Assert-KmcBootstrapInventoryString $Value.type 'Auto';$leaf='Auto_1.zks'
        if($Value.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($Value.internalName)-or$Value.internalName.Length-gt256-or$Value.internalName-match'[\x00-\x1f\x7f]'){throw 'Assisted native Auto name is malformed.'}
    }
    Assert-KmcBootstrapInventoryString $Value.fileName $leaf
    Assert-KmcBootstrapInventoryString $Value.path (Join-Path $Root $leaf)
}


function Assert-KmcAssistedBootstrapProbe($O,$Expected,$Labels) {
    $trace=$O.bootstrapProbe;$bridge=$O.bootstrapIdentityBridge;$write=$O.nativeWrites[0];$world=$O.initialWorld
    Assert-KmcAssistedIdentityBridge $bridge 'retained-bootstrap-128' @('game','player','saveManager','main','requested','prepared','preset') $Labels
    foreach($key in @('game','player','saveManager')){Assert-KmcOrdinaryInteger $bridge.roles.$key.host $world.$key $world.$key}
    Assert-KmcOrdinaryInteger $bridge.roles.main.host $world.mainActor $world.mainActor
    Assert-KmcOrdinaryInteger $bridge.roles.requested.host $write.requestedDescriptor $write.requestedDescriptor
    Assert-KmcOrdinaryInteger $bridge.roles.prepared.host $write.preparedDescriptor $write.preparedDescriptor
    Assert-KmcAssistedObjectValue $trace @('hooks','events','errors','installed','installationAttempted','entered','exited','minted','saveRequests','prepared','complete','identityCallsite','identities') 'assisted bootstrap observer'
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $trace.$key $true}
    foreach($key in @('entered','exited','minted','saveRequests','prepared')){Assert-KmcOrdinaryInteger $trace.$key 1 1}
    Assert-KmcAssistedArray $trace.errors 0 0 'assisted bootstrap errors'
    Assert-KmcAssistedArray $trace.hooks 3 3 'assisted bootstrap hooks'
    $tokens=@('06000CDC','06008029','06008025');$methods=@('LoadNewGame','SaveRoutine','PrepareSave')
    for($i=0;$i-lt3;$i++){
        $h=$trace.hooks[$i];Assert-KmcAssistedObjectValue $h @('type','method','token','moduleMvid','transpiler') 'assisted native bootstrap hook'
        $type=if($i-eq0){'Kingmaker.Game'}else{'Kingmaker.EntitySystem.Persistence.SaveManager'}
        foreach($pair in @(@('type',$type),@('method',$methods[$i]),@('token',$tokens[$i]),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}
        if($i-eq0){Assert-KmcBootstrapInventoryString $h.transpiler 'IdentityCallsite'}elseif($null-ne$h.transpiler){throw 'Assisted bootstrap has an unexpected transpiler.'}
    }
    $site=$trace.identityCallsite
    Assert-KmcAssistedObjectValue $site @('contract','installed','ownerToken','setterToken','stateFieldToken','playerFieldToken','setterIlOffset','moduleMvid','nativeSetterPreserved','setterPostfixUsed','matchedSites') 'assisted native mint callsite'
    foreach($pair in @(@('contract','load-new-game-guid-setter-callsite-v1'),@('ownerToken','06000CDC'),@('setterToken','06000DE3'),@('stateFieldToken','040006B6'),@('playerFieldToken','0400533C'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $site.($pair[0]) $pair[1]}
    Assert-KmcOrdinaryInteger $site.setterIlOffset 302 302;Assert-KmcOrdinaryInteger $site.matchedSites 1 1
    foreach($key in @('installed','nativeSetterPreserved')){Assert-KmcChunk7Boolean $site.$key $true};Assert-KmcChunk7Boolean $site.setterPostfixUsed $false
    $r=$trace.identities
    Assert-KmcAssistedObjectValue $r @('contract','capacity','ids','retainedCount','faults','released') 'assisted bootstrap identity registry'
    Assert-KmcBootstrapInventoryString $r.contract 'retained-native-object-identity-v1'
    Assert-KmcOrdinaryInteger $r.capacity 128 128;Assert-KmcOrdinaryInteger $r.faults 0 0;Assert-KmcChunk7Boolean $r.released $false
    Assert-KmcAssistedArray $r.ids 1 128 'bootstrap retained labels';Assert-KmcOrdinaryInteger $r.retainedCount $r.ids.Count $r.ids.Count
    $probeLabels=[Collections.Generic.HashSet[int]]::new()
    foreach($id in $r.ids){Assert-KmcNativeBootstrapObject $id;if(-not$probeLabels.Add($id)){throw 'Bootstrap labels repeat.'}}
    foreach($role in $bridge.roles.PSObject.Properties){if(-not$probeLabels.Contains([int]$role.Value.observer)){throw 'Bridge observer label was not retained by the bootstrap probe.'}}
    Assert-KmcAssistedArray $trace.events 5 5 'assisted bootstrap native events'
    $kinds=@('native-new-game-enter','native-campaign-minted','native-bootstrap-autosave-request','native-new-game-exit','native-bootstrap-autosave-prepared');$prior=$O.before.frame
    for($i=0;$i-lt5;$i++){
        $e=$trace.events[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $world.frame;$prior=$e.frame
        Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcBootstrapInventoryString $e.presetGuid $O.preset.guid
        foreach($key in @('game','player','saveManager','preset')){Assert-KmcOrdinaryInteger $e.$key $bridge.roles.$key.observer $bridge.roles.$key.observer}
        Assert-KmcOrdinaryInteger $e.menuPlayer $bridge.roles.player.observer $bridge.roles.player.observer
        Assert-KmcChunk7Boolean $e.sameMenuPlayer $true;Assert-KmcChunk7Boolean $e.insideNativeNewGame ($i-lt4)
        if($i-gt0){Assert-KmcBootstrapInventoryString $e.gameId $Expected.gameId;Assert-KmcBootstrapInventoryString $e.mainActorId $Expected.mainActorId}
        if($i-eq2-or$i-eq4){
            $descriptor=if($i-eq2){$bridge.roles.requested.observer}else{$bridge.roles.prepared.observer}
            Assert-KmcOrdinaryInteger $e.save $descriptor $descriptor;Assert-KmcBootstrapInventoryString $e.saveType 'Auto';Assert-KmcChunk7Boolean $e.autoLevelup $false
        }
    }
    $requested=$trace.events[2];$prepared=$trace.events[4]
    Assert-KmcOrdinaryInteger $prepared.requestedSave $requested.save $requested.save
    Assert-KmcOrdinaryInteger $prepared.observedSaveManager $bridge.roles.saveManager.observer $bridge.roles.saveManager.observer
    Assert-KmcChunk7Boolean $prepared.accepted $true
    Assert-KmcBootstrapInventoryString $requested.saveName $write.internalName
    foreach($pair in @(@('saveLeaf','fileName'),@('saveName','internalName'),@('savePath','path'),@('saveArea','area'),@('saveGameId','gameId'),@('saveGameName','gameName'))){Assert-KmcBootstrapInventoryString $prepared.($pair[0]) $write.($pair[1])}
    $names=@('ordered','sameGame','sameManager','samePlayer','requestRetained','distinctDescriptor','sameName','autoCategory','expectedLeaf','sameCampaign')
    Assert-KmcAssistedObjectValue $prepared.checks $names 'assisted prepared bootstrap checks'
    foreach($key in $names){Assert-KmcChunk7Boolean $prepared.checks.$key $true}
}
function Assert-KmcAssistedManualProbe($Request,$O,$Labels) {
    $p=$O.manualProbe;$write=$O.finalManual;$world=$O.fieldWorld;$priorWrites=$write.completionSequence-1
    Assert-KmcAssistedObjectValue $p @('contract','installed','installationAttempted','complete','requests','prepares','starts','stages','completions','callbacks','events','hooks','errors') 'assisted Manual observer'
    Assert-KmcBootstrapInventoryString $p.contract 'manual-seed-native-save-v1'
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $p.$key $true}
    foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1}
    Assert-KmcAssistedArray $p.errors 0 0 'assisted Manual observer errors'
    Assert-KmcAssistedArray $p.events 6 6 'assisted Manual events';Assert-KmcAssistedArray $p.hooks 3 3 'assisted Manual hooks'
    $methods=@('SaveRoutine','PrepareSave','RecordCompletedSave');$tokens=@('06008029','06008025')
    for($i=0;$i-lt3;$i++){
        $h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $methods[$i]
        if($i-lt2){Assert-KmcBootstrapInventoryString $h.type 'Kingmaker.EntitySystem.Persistence.SaveManager';Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}else{
            Assert-KmcBootstrapInventoryString $h.type 'KingmakerMountedCombat.Integration.MountedPersistenceService';Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid
            if($h.token-isnot[string]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'){throw 'Assisted Manual completion hook token is invalid.'}
        }
    }
    $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback');$prior=$world.frame
    for($i=0;$i-lt6;$i++){
        $e=$p.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1);Assert-KmcOrdinaryInteger $e.frame $prior $O.savedWorld.frame;$prior=$e.frame
        foreach($key in @('game','player','saveManager')){Assert-KmcOrdinaryInteger $e.$key $world.$key $world.$key}
        Assert-KmcOrdinaryInteger $e.requested $write.requestedDescriptor $write.requestedDescriptor
        $descriptor=if($i-eq0){$write.requestedDescriptor}else{$write.preparedDescriptor};Assert-KmcOrdinaryInteger $e.descriptor $descriptor $descriptor
        foreach($key in @('internalName','type','gameName','area')){Assert-KmcBootstrapInventoryString $e.$key $write.$key}
        if($i-eq0){foreach($key in @('fileName','gameId','path')){if($null-ne$e.$key){throw 'Assisted fresh Manual request was already prepared.'}}}else{foreach($key in @('fileName','gameId','path')){Assert-KmcBootstrapInventoryString $e.$key $write.$key}}
        $snapshots=if($i-lt3){$priorWrites}else{$priorWrites+1};$completed=if($i-lt5){$priorWrites}else{$priorWrites+1}
        Assert-KmcOrdinaryInteger $e.snapshotCount $snapshots $snapshots;Assert-KmcOrdinaryInteger $e.completedSaveCount $completed $completed
    }
    $worker=$p.events[4]
    Assert-KmcBootstrapInventoryString $worker.iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46'
    Assert-KmcBootstrapInventoryString $worker.workerStatus 'RanToCompletion';Assert-KmcOrdinaryInteger $worker.workerId 1 ([int]::MaxValue)
    foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $worker.$key $true}
    foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $worker.$key $false}
    $objects=[Collections.Generic.HashSet[int]]::new()
    foreach($actor in $world.party){foreach($key in @('object','descriptor','view')){[void]$objects.Add([int]$actor.$key)}}
    foreach($key in @('game','player','saveManager')){[void]$objects.Add([int]$world.$key)}
    foreach($value in @($write.requestedDescriptor,$write.preparedDescriptor,$worker.scope,$worker.iterator,$worker.worker)){
        Assert-KmcAssistedLabel $value $Labels
        if(-not$objects.Add($value)){throw 'Assisted Manual scope, iterator, worker or descriptor aliases another retained owner.'}
    }
}

function Assert-KmcAssistedFixtureReadiness($Row,$Request,$World,$Settled,$Inventory,[string]$Root,$CurrentAuto){
 Assert-KmcExactProperties $Row @('schemaVersion','contract','captureStatus','frame','gameplayAssemblyMvid','observationOnly','saveSettings','experienceRatePercent','mainProgression','staticPartyFacts','isolatedSaveDescriptors') 'cold fixture readiness'
 Assert-KmcOrdinaryInteger $Row.schemaVersion 1 1;Assert-KmcOrdinaryInteger $Row.frame $World.frame $Settled.frame
 Assert-KmcBootstrapInventoryString $Row.contract 'installed-native-fixture-readiness-v1';Assert-KmcBootstrapInventoryString $Row.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $Row.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';Assert-KmcChunk7Boolean $Row.observationOnly $true
 Assert-KmcOrdinaryInteger $Row.experienceRatePercent ([int]::MinValue) ([int]::MaxValue)
 $s=$Row.saveSettings;Assert-KmcExactProperties $s @('observationKind','projectionEnabled','autosaveSlotsNativeType','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten','onlyOneSave','autosaveEnabled','autosaveSlots') 'observed native save settings'
 Assert-KmcBootstrapInventoryString $s.observationKind 'native-effective-getters';Assert-KmcBootstrapInventoryString $s.autosaveSlotsNativeType 'System.Single'
 foreach($key in @('projectionEnabled','autosaveCapacityInferred','sliderFallbackCallbackPresent','settingsWritten')){Assert-KmcChunk7Boolean $s.$key $false}
 foreach($key in @('onlyOneSave','autosaveEnabled')){if($s.$key-isnot[bool]){throw 'Native save setting must remain a boolean observation.'}}
 if(-not(Test-KmcFiniteJsonNumber $s.autosaveSlots)){throw 'Native slot setting must remain a finite numeric observation.'}
 try{$nativeSlots=[single]$s.autosaveSlots}catch{throw 'Native slot setting cannot convert to Single.'}
 if([single]::IsNaN($nativeSlots)-or[single]::IsInfinity($nativeSlots)-or([double]$s.autosaveSlots-ne0.0-and$nativeSlots-eq0.0)){throw 'Native slot setting overflows or underflows Single; no slot capacity is inferred.'}
 $m=$Row.mainProgression;Assert-KmcExactProperties $m @('actorId','characterLevel','experience','classes') 'main native classes';Assert-KmcBootstrapInventoryString $m.actorId $World.mainActorId
 Assert-KmcOrdinaryInteger $m.characterLevel 1 20;Assert-KmcOrdinaryInteger $m.experience 0 ([int]::MaxValue)
 if($m.characterLevel-ne$Inventory.ordinaryAcquisitionPrerequisites.mainCharacterLevel-or$m.experience-ne$Inventory.ordinaryAcquisitionPrerequisites.mainCharacterExperience-or$m.classes-isnot[Array]-or$m.classes.Count-lt1-or$m.classes.Count-gt20){throw 'Native main class observation differs from its readiness or is unbounded.'}
 $classes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$levels=0
 foreach($c in $m.classes){Assert-KmcExactProperties $c @('classGuid','level','archetypeGuids') 'native class rank'
  if($c.classGuid-isnot[string]-or$c.classGuid-cnotmatch'^[a-f0-9]{32}$'-or-not$classes.Add($c.classGuid)){throw 'Native class GUID is malformed or repeated.'};Assert-KmcOrdinaryInteger $c.level 1 20;$levels+=$c.level
  if($c.archetypeGuids-isnot[Array]-or$c.archetypeGuids.Count-gt16){throw 'Native archetypes are unbounded.'};$archetypes=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($a in $c.archetypeGuids){if($a-isnot[string]-or$a-cnotmatch'^[a-f0-9]{32}$'-or-not$archetypes.Add($a)){throw 'Native archetype GUID is malformed or repeated.'}}
 }
 if($levels-ne$m.characterLevel){throw 'Native class ranks do not sum to the observed main level.'}
 $p=$Row.staticPartyFacts;Assert-KmcExactProperties $p @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified','characters') 'static native party facts'
 foreach($key in @('selectorOpened','liveSelectorRequiredAuthorityAvailable','membershipOrRemovalQualified')){Assert-KmcChunk7Boolean $p.$key $false}
 if($p.characters-isnot[Array]-or$p.characters.Count-lt1-or$p.characters.Count-gt6){throw 'Static native party-character facts are unbounded.'}
 $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$ordered=@()
 foreach($c in $p.characters){Assert-KmcExactProperties $c @('actorId','blueprintGuid','isMain','lockedCompanionComponent') 'static character fact';Assert-KmcOrdinaryGuid $c.actorId
  if(-not$ids.Add($c.actorId)){throw 'Static native actor repeated.'};$ordered+=,$c.actorId;$actor=@($World.party|Where-Object id -CEQ $c.actorId);if($actor.Count-ne1){throw 'Static native actor is foreign.'};Assert-KmcBootstrapInventoryString $c.blueprintGuid $actor[0].blueprint
  Assert-KmcChunk7Boolean $c.isMain ($c.actorId-ceq$World.mainActorId);if($c.lockedCompanionComponent-isnot[bool]){throw 'Locked-companion observation must remain a boolean fact.'}
 }
 if(-not$ids.Contains($World.mainActorId)-or(ConvertTo-KmcChunk7Canonical $ordered)-cne(ConvertTo-KmcChunk7Canonical $Inventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds)){throw 'Static party facts differ from the actual native character roster.'}

    $d=$Row.isolatedSaveDescriptors
    Assert-KmcAssistedObjectValue $d @('root','metadataOnly','maximumObservedDescriptors','descriptors') 'assisted readiness descriptor inventory'
    Assert-KmcBootstrapInventoryString $d.root $Root;Assert-KmcChunk7Boolean $d.metadataOnly $true;Assert-KmcOrdinaryInteger $d.maximumObservedDescriptors 32 32
    Assert-KmcAssistedArray $d.descriptors 2 2 'assisted current native descriptor inventory'
    $leaves=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($v in $d.descriptors){
        Assert-KmcAssistedObjectValue $v @('fileName','internalName','gameId','gameName','area','type','isAutoLevelupSave','isActuallySaved','hasFileOnDisk','operationState','systemSaveTimeTicks','systemSaveTimeKind') 'assisted readiness native descriptor'
        if($v.fileName-isnot[string]-or-not$leaves.Add($v.fileName)){throw 'Assisted readiness descriptor leaf is missing or repeated.'}
        $isAnchor=$v.fileName-ceq$Request.fixture.working.fileName
        if(-not$isAnchor-and$v.fileName-cne'Auto_1.zks'){throw 'Assisted readiness contains an unselected descriptor.'}
        $expected=if($isAnchor){$Request.fixture.working}else{$CurrentAuto}
        foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $v.$key $expected.$key}
        Assert-KmcBootstrapInventoryString $v.type $(if($isAnchor){'Manual'}else{'Auto'})
        Assert-KmcChunk7Boolean $v.isAutoLevelupSave $(-not$isAnchor-and$CurrentAuto.autoLevelup)
        Assert-KmcBootstrapInventoryString $v.operationState 'None'
        foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $v.$key $true}
        Assert-KmcOrdinaryInteger $v.systemSaveTimeTicks 0 ([DateTime]::MaxValue.Ticks)
        if($v.systemSaveTimeKind-isnot[string]-or$v.systemSaveTimeKind-cnotin@('Unspecified','Utc','Local')){throw 'Assisted native save timestamp kind is malformed.'}
    }
    if(-not$leaves.Contains($Request.fixture.working.fileName)-or-not$leaves.Contains('Auto_1.zks')){throw 'Assisted readiness lost its immutable Working anchor or current Auto.'}
}
function Assert-KmcAssistedDescriptors($Rows,$Request,$Auto,$Manual,$Labels) {
    $withManual=$null-ne$Manual;$count=if($withManual){3}else{2}
    Assert-KmcAssistedArray $Rows $count $count 'assisted settled descriptor graph'
    $leaves=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$objects=[Collections.Generic.HashSet[int]]::new()
    foreach($row in $Rows){
        Assert-KmcAssistedObjectValue $row @('descriptor','fileName','internalName','gameId','gameName','area','type','autoLevelup','isActuallySaved','hasFileOnDisk','operation') 'assisted settled descriptor'
        Assert-KmcAssistedLabel $row.descriptor $Labels
        if($row.fileName-isnot[string]-or-not$leaves.Add($row.fileName)-or-not$objects.Add($row.descriptor)){throw 'Assisted descriptor graph repeats a leaf or object.'}
        if($row.fileName-ceq$Request.fixture.working.fileName){$expected=$Request.fixture.working;$type='Manual';$autoLevel=$false;$descriptor=$null}
        elseif($row.fileName-ceq$Auto.fileName){$expected=$Auto;$type='Auto';$autoLevel=$Auto.autoLevelup;$descriptor=$Auto.preparedDescriptor}
        elseif($withManual-and$row.fileName-ceq$Manual.fileName){$expected=$Manual;$type='Manual';$autoLevel=$false;$descriptor=$Manual.preparedDescriptor}
        else{throw 'Assisted settled descriptor graph contains a foreign leaf.'}
        foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $row.$key $expected.$key}
        Assert-KmcBootstrapInventoryString $row.type $type;Assert-KmcBootstrapInventoryString $row.operation 'None'
        foreach($key in @('isActuallySaved','hasFileOnDisk')){Assert-KmcChunk7Boolean $row.$key $true}
        Assert-KmcChunk7Boolean $row.autoLevelup $autoLevel
        if($null-ne$descriptor){Assert-KmcOrdinaryInteger $row.descriptor $descriptor $descriptor}
    }
}
function Assert-KmcAssistedArrangement($Value,$World,$Setup) {
    Assert-KmcAssistedObjectValue $Value @('money','experienceRatePercent','actors') 'assisted native save arrangement'
    Assert-KmcOrdinaryInteger $Value.money $Setup.hireFunds.after.money $Setup.hireFunds.after.money
    Assert-KmcOrdinaryInteger $Value.experienceRatePercent $Setup.hireFunds.after.experienceRatePercent $Setup.hireFunds.after.experienceRatePercent
    Assert-KmcAssistedArray $Value.actors $World.party.Count $World.party.Count 'assisted save-only actors'
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($row in $Value.actors){
        Assert-KmcAssistedObjectValue $row @('id','object','descriptor','view','blueprint','masterId','petId','experience','level','classes') 'assisted save-only actor'
        Assert-KmcOrdinaryGuid $row.id;if(-not$ids.Add($row.id)){throw 'Assisted save-only actor repeats.'}
        $actors=@($World.party|Where-Object id -CEQ $row.id)
        if($actors.Count-ne1){throw 'Assisted save-only actor is not in the exact native field.'};$actor=$actors[0]
        foreach($key in @('id','object','descriptor','view','blueprint','masterId','petId','classes')){Assert-KmcAssistedSame $row.$key $actor.$key ('assisted save-only actor '+$key)}
        Assert-KmcOrdinaryInteger $row.level $actor.characterLevel $actor.characterLevel
        $xp=if($row.id-ceq$World.mainActorId){$Setup.hireFunds.after.experience}else{$actor.experience}
        Assert-KmcOrdinaryInteger $row.experience $xp $xp
    }
}

function Assert-KmcAssistedOrdinaryProbe($O,$Proof,$Labels) {
    $probe=$O.ordinaryProbe;$identity=$Proof.identity;$commits=$Proof.nativeCommitCount;$bridge=$O.ordinaryIdentityBridge;$write=$O.nativeWrites[1]
    Assert-KmcAssistedIdentityBridge $bridge 'runtime-object-hash' @('actor','descriptor','requested','prepared','saveManager') $Labels
    foreach($pair in @(@('actor',$O.fieldWorld.mainActor),@('requested',$write.requestedDescriptor),@('prepared',$write.preparedDescriptor),@('saveManager',$O.fieldWorld.saveManager))){Assert-KmcOrdinaryInteger $bridge.roles.($pair[0]).host $pair[1] $pair[1]}
    Assert-KmcOrdinaryInteger $identity.actorObject $bridge.roles.actor.observer $bridge.roles.actor.observer
    $nativeMain=@($O.fieldWorld.party|Where-Object id -CEQ $O.fieldWorld.mainActorId)[0]
    Assert-KmcOrdinaryInteger $bridge.roles.descriptor.host $nativeMain.descriptor $nativeMain.descriptor
    Assert-KmcOrdinaryInteger $identity.descriptorObject $bridge.roles.descriptor.observer $bridge.roles.descriptor.observer
    Assert-KmcAssistedObjectValue $probe @('installationAttempted','installed','complete','completionEntries','completionExits','saveRequests','preparedWrites','events','hooks','errors') 'assisted ordinary observer'
    foreach($field in @('installationAttempted','installed','complete')){Assert-KmcChunk7Boolean $probe.$field $true}
    if($probe.errors-isnot[Array]-or$probe.errors.Count-ne0-or$probe.hooks-isnot[Array]-or$probe.hooks.Count-ne3-or$probe.events-isnot[Array]-or$probe.events.Count-ne(2*$commits+2)){throw 'Native assisted ordinary observer is incomplete.'}
    $contracts=@(@('Kingmaker.Controllers.LevelUpAutoSaveController','HandleLevelUpComplete','0600906B'),@('Kingmaker.Game','SaveGame','06000CE4'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025'))
    for($i=0;$i-lt3;$i++){$h=$probe.hooks[$i];foreach($pair in @(@('type',$contracts[$i][0]),@('method',$contracts[$i][1]),@('token',$contracts[$i][2]),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $h.($pair[0]) $pair[1]}}
    Assert-KmcOrdinaryInteger $probe.completionEntries $commits $commits;Assert-KmcOrdinaryInteger $probe.completionExits $commits $commits
    Assert-KmcOrdinaryInteger $probe.saveRequests 1 1;Assert-KmcOrdinaryInteger $probe.preparedWrites 1 1
    $inside=$null;$entries=0;$exits=0;$requests=0;$writes=0;$lastFrame=$identity.frame
    foreach($row in $probe.events){
        if($row.kind-isnot[string]){throw 'Assisted native autosave callback kind must be a scalar string.'}
        Assert-KmcOrdinaryInteger $row.frame $lastFrame $write.frame;$lastFrame=$row.frame
        Assert-KmcBootstrapInventoryString $row.actorId $identity.actorId
        Assert-KmcOrdinaryInteger $row.actorObject $identity.actorObject $identity.actorObject
        switch -CaseSensitive ($row.kind){
            'native-level-completion-enter' {
                if($null-ne$inside){throw 'Overlapping assisted native completion callbacks.'};$entries++;$inside=$row
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true
                Assert-KmcChunk7Boolean $row.isChargen ($entries-lt$commits)
                Assert-KmcOrdinaryInteger $row.characterLevel ($identity.level+$entries) ($identity.level+$entries)
            }
            'native-level-autosave-request' {
                if($null-eq$inside-or$entries-ne$commits-or$row.completionActorId-cne$identity.actorId-or$row.frame-ne$inside.frame){throw 'Assisted autosave did not originate inside the exact terminal completion.'}
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;Assert-KmcChunk7Boolean $row.isChargen $false;Assert-KmcChunk7Boolean $row.inState $true
                Assert-KmcChunk7Boolean $row.autoLevelup $true;Assert-KmcBootstrapInventoryString $row.saveType 'Auto'
                $requests++;Assert-KmcOrdinaryInteger $row.saveObject $bridge.roles.requested.observer $bridge.roles.requested.observer
            }
            'native-level-autosave-prepared' {
                $writes++;if($requests-ne1){throw 'Assisted PrepareSave preceded its exact request.'}
                Assert-KmcOrdinaryInteger $row.saveObject $bridge.roles.prepared.observer $bridge.roles.prepared.observer
                Assert-KmcBootstrapInventoryString $row.saveLeaf 'Auto_2.zks';Assert-KmcChunk7Boolean $row.autoLevelup $true;Assert-KmcBootstrapInventoryString $row.saveType 'Auto'
            }
            'native-level-completion-exit' {
                if($null-eq$inside-or$row.frame-ne$inside.frame-or$row.completionActorId-cne$identity.actorId){throw 'Assisted completion exit lost exact callback custody.'}
                Assert-KmcChunk7Boolean $row.isChargen $inside.isChargen;Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;$inside=$null;$exits++
            }
            default {throw 'Unknown assisted native autosave observer event.'}
        }
    }
    if($null-ne$inside-or$entries-ne$commits-or$exits-ne$commits-or$requests-ne1-or$writes-ne1){throw 'Assisted completion/autosave events do not settle exactly once.'}
    # This projection binds the separate observer label domain; it never invents
    # a save or changes the producer's native authority/cumulative write quota.
    $projection=[pscustomobject]@{requestedObject=$bridge.roles.requested.observer;writtenObject=$bridge.roles.prepared.observer;name=$write.internalName;completedWritesBefore=1;completionSequence=$write.completionSequence}
    Assert-KmcOrdinarySaveDescriptorBinding $probe $projection
}

function Assert-KmcAssistedProfile($Inventory,$World,$Setup) {
    Assert-KmcOrdinaryInteger $Inventory.schemaVersion 1 1
    foreach($pair in @(@('captureStatus','PASS'),@('captureKind','installed-native-small-body-catalogue'),@('gameplayAssemblyMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $Inventory.($pair[0]) $pair[1]}
    foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $Inventory.$key $false}
    Assert-KmcAssistedArray $Inventory.party $World.party.Count $World.party.Count 'assisted native profile actors'
    foreach($actor in $World.party){
        $rows=@($Inventory.party|Where-Object id -CEQ $actor.id);if($rows.Count-ne1){throw 'Assisted native profile actor changed.'};$row=$rows[0]
        Assert-KmcBootstrapInventoryString $row.blueprintGuid $actor.blueprint
        foreach($key in @('masterId','petId')){Assert-KmcAssistedSame $row.$key $actor.$key ('assisted native profile '+$key)}
        $l=$row.nativeLevelUpReadiness;Assert-KmcOrdinaryInteger $l.characterLevel $actor.characterLevel $actor.characterLevel
        $xp=if($actor.id-ceq$World.mainActorId){$Setup.hireFunds.after.experience}else{$actor.experience};Assert-KmcOrdinaryInteger $l.experience $xp $xp
        if($null-ne$l.nextLevelExperience){Assert-KmcOrdinaryInteger $l.nextLevelExperience 0 ([int]::MaxValue)}
        Assert-KmcChunk7Boolean $l.isDead $false
        foreach($key in @('partyInCombat','controllerCreated','completeChoicesObserved','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $l.$key $false}
        if($l.canLevelUp-isnot[bool]){throw 'Assisted level readiness must be boolean.'}
        if($actor.id-ceq$World.mainActorId){Assert-KmcChunk7Boolean $l.canLevelUp $false}
    }
    $r=$Inventory.ordinaryAcquisitionPrerequisites
    Assert-KmcChunk7Boolean $r.observationOnly $true
    foreach($key in @('purchaseAuthorized','sixPairFixtureQualified','defaultHireMatchesPlayerExperienceExactly')){Assert-KmcChunk7Boolean $r.$key $false}
    Assert-KmcOrdinaryInteger $r.nativeMoney $Setup.hireFunds.after.money $Setup.hireFunds.after.money
    Assert-KmcOrdinaryInteger $r.nativeHireCostNow $Setup.hireFunds.after.nativeHireCost $Setup.hireFunds.after.nativeHireCost
    Assert-KmcOrdinaryInteger $r.mainCharacterExperience $Setup.hireFunds.after.experience $Setup.hireFunds.after.experience
    Assert-KmcOrdinaryInteger $r.nativeDefaultHireExperienceNow 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $r.mainCharacterLevel $Setup.hireFunds.after.level $Setup.hireFunds.after.level
    Assert-KmcOrdinaryInteger $r.nativePartyLevel 1 20;Assert-KmcOrdinaryInteger $r.nativeFieldCharacterLimit 6 6
    Assert-KmcBootstrapInventoryString $r.mainCharacterId $World.mainActorId;Assert-KmcChunk7Boolean $r.nativeAffordableNow ($r.nativeMoney-ge$r.nativeHireCostNow)
    Assert-KmcAssistedArray $r.nativePartyCharacterIds 1 6 'assisted native field characters'
    $characters=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($id in $r.nativePartyCharacterIds){Assert-KmcOrdinaryGuid $id;if(-not$characters.Add($id)-or$id-cnotin@($World.party.id)){throw 'Assisted native characters are duplicate or foreign.'}}
    if(-not$characters.Contains($World.mainActorId)){throw 'Assisted main character is absent from native roster.'}
}

function Assert-KmcAssistedSetupWorldBindings($O,$Expected) {
    $first=$O.preservation.rows[0].state
    $funds=@($O.preservation.rows|Where-Object purpose -CEQ 'hire-funds-before')[0].state
    foreach($pair in @(@($first,$O.beforeSetup),@($funds,$O.fieldWorld))){
        $state=$pair[0];$world=$pair[1]
        foreach($key in @('game','player','saveManager','mainActor','mainActorId','gameId','gameName','area')){Assert-KmcAssistedSame $state.$key $world.$key ('assisted preserved world '+$key)}
        $fieldIds=@($state.partyCharacters.id)
        foreach($member in $state.partyCharacters){$character=@($state.actors|Where-Object id -CEQ $member.id)[0];if($null-ne$character.petId){$fieldIds+=,$character.petId}}
        if($world.party.Count-ne$fieldIds.Count){throw 'Assisted world differs from the exact preserved field graph.'}
        foreach($actor in $world.party){
            if($actor.id-cnotin$fieldIds){throw 'Assisted world contains a foreign field actor.'}
            $captured=@($state.actors|Where-Object id -CEQ $actor.id)[0]
            # Live action cooldowns can advance between frames. Bind the retained
            # actors and native progression, without claiming a frozen game clock.
            foreach($key in @('id','object','descriptor','view','blueprint','masterId','petId','characterLevel','classes')){Assert-KmcAssistedSame $captured.$key $actor.$key ('assisted preserved field '+$key)}
            if($actor.id-cne$Expected.mainActorId){Assert-KmcOrdinaryInteger $captured.experience $actor.experience $actor.experience}
        }
    }
    foreach($kind in @('experience','hire-funds')){
        $state=if($kind-ceq'experience'){$first}else{$funds};$effect=if($kind-ceq'experience'){$O.setup.experience}else{$O.setup.hireFunds}
        $main=@($state.actors|Where-Object id -CEQ $Expected.mainActorId)[0]
        foreach($snapshot in @($effect.before,$effect.after)){
            Assert-KmcChunk7Boolean $snapshot.isPet ($null-ne$main.masterId)
            Assert-KmcChunk7Boolean $snapshot.hasPet ($null-ne$main.petId)
        }
    }
    foreach($key in @('partyCharacters','remoteCompanions')){Assert-KmcAssistedSame $first.$key $funds.$key ('assisted unchanged character membership '+$key)}
    foreach($actor in $O.beforeSetup.party){
        $rows=@($O.fieldWorld.party|Where-Object id -CEQ $actor.id);if($rows.Count-ne1){throw 'Assisted setup replaced an original actor.'};$after=$rows[0]
        foreach($key in @('object','descriptor','view','blueprint','masterId')){Assert-KmcAssistedSame $actor.$key $after.$key ('assisted original actor '+$key)}
        if($actor.id-cne$Expected.mainActorId){foreach($key in @('experience','characterLevel','classes','petId')){Assert-KmcAssistedSame $actor.$key $after.$key ('assisted unrelated progression '+$key)}}
    }
    $main=@($O.fieldWorld.party|Where-Object id -CEQ $Expected.mainActorId)[0]
    foreach($actor in $O.fieldWorld.party){if($actor.id-cnotin@($O.beforeSetup.party.id)-and$actor.id-cne$main.petId){throw 'Assisted setup admitted an unselected new actor.'}}
    $ranger=@($main.classes|Where-Object guid -CEQ 'cda0615668a6df14eb36ba19ee881af6')
    if($ranger.Count-ne1){throw 'Assisted main native Ranger class missing.'};Assert-KmcOrdinaryInteger $ranger[0].level 4 8
    Assert-KmcOrdinaryGuid $main.petId
    $pet=@($O.fieldWorld.party|Where-Object id -CEQ $main.petId)
    if($pet.Count-ne1){throw 'Assisted acquired Horse is not in the native field.'}
    Assert-KmcBootstrapInventoryString $pet[0].masterId $main.id
    Assert-KmcBootstrapInventoryString $pet[0].blueprint '4016c7db400ab721ff125aef9e65e202'
    $feature=@($main.features|Where-Object guid -CEQ '7db7c50677e39f09feef56f3831fc723')
    if($feature.Count-ne1-or$null-eq$feature[0].source){throw 'Assisted main lacks its sourced native Horse feature.'}
    Assert-KmcOrdinaryInteger $feature[0].rank 1 20
    $rank=@($main.features|Where-Object guid -CEQ '1670990255e4fe948a863bafd5dbda5d')
    if($rank.Count-ne1){throw 'Assisted main lacks its native companion rank fact.'}
    Assert-KmcOrdinaryInteger $rank[0].rank 1 20
}

function Assert-KmcChunk7AssistedFixtureEvidence($Request,$Artifact,[string]$Status='PASS') {
    Assert-KmcChunk7AssistedFixtureSelection $Request
    if(-not(Test-KmcChunk7AssistedFixtureScenario $Request.scenario)){throw 'Assisted artifact belongs to another scenario.'}
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-assisted-fixture-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS assisted result requires a PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$root=Get-KmcAssistedSaveRoot $Request
    Assert-KmcAssistedArray $o.nativeWrites 2 3 'assisted native writes'
    $levelups=$o.nativeWrites.Count-2;$writes=$o.nativeWrites.Count;$manual=$o.nativeWrites[-1];$auto=$o.nativeWrites[$levelups]
    $hasBuilder=$null-ne$o.PSObject.Properties['builderComplete']
    $keys=@('selection','processId','assistanceOnly','ordinaryAcquisitionProven','ordinaryProgressionProven','mountedCombatProven','smallPoseQualified','workingDeserialized','diagnosticArchiveContentRead','nativeWrites','choices','preservation','preflight','before','initialWorld','bootstrapIdentityBridge','bootstrapProbe','derivedTarget','setup','beforeSetup','nativeProfileInventory','fixtureReadiness','fieldWorld','afterSetupActors','nativeNumbering','beforeManualArrangement','afterManualArrangement','settledDescriptors','finalManual','savedWorld','setupAtCleanup','manualProbe','settledWorld','identityRegistry','cleanup','elapsedSeconds','preset')
    if($hasBuilder){$keys+='builderComplete'}
    if($levelups-eq1){$keys+=@('ordinaryProbe','ordinaryIdentityBridge','afterLevelupDescriptors')}
    Assert-KmcAssistedObjectValue $o $keys 'assisted successful observations'
    Assert-KmcAssistedSame $o.selection $Request.assistedFixture 'assisted request selection'
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt600){throw 'Assisted elapsed evidence is outside its finite setup and settlement window.'}
    Assert-KmcChunk7Boolean $o.assistanceOnly $true
    foreach($key in @('ordinaryAcquisitionProven','ordinaryProgressionProven','mountedCombatProven','smallPoseQualified','workingDeserialized','diagnosticArchiveContentRead')){Assert-KmcChunk7Boolean $o.$key $false}
    $registry=$o.identityRegistry
    Assert-KmcAssistedObjectValue $registry @('capacity','faultCount','retainedIds') 'assisted retained host registry'
    Assert-KmcOrdinaryInteger $registry.capacity 2048 2048;Assert-KmcOrdinaryInteger $registry.faultCount 0 0
    Assert-KmcAssistedArray $registry.retainedIds 1 2048 'assisted retained host labels'
    $labels=[Collections.Generic.HashSet[int]]::new()
    foreach($id in $registry.retainedIds){Assert-KmcNativeBootstrapObject $id;if(-not$labels.Add($id)){throw 'Assisted host identity registry repeats a label.'}}
    $initial=$o.initialWorld;$generation=$o.before.authority.generation
    $expected=[pscustomobject]@{runId=$Request.runId;gameId=$initial.gameId;gameName=$initial.gameName;mainActorId=$initial.mainActorId;area=$initial.area;authorityGeneration=$generation}
    foreach($key in @('gameId','mainActorId')){Assert-KmcOrdinaryGuid $expected.$key};Assert-KmcAssistedGuid32 $expected.area
    if($expected.gameId-ceq$Request.fixture.working.gameId-or$expected.gameId-ceq$Request.fixture.baseline.gameId){throw 'Assisted native new game reused a protected campaign.'}
    if($expected.gameName-isnot[string]-or[string]::IsNullOrWhiteSpace($expected.gameName)-or$expected.gameName.Length-gt256-or$expected.gameName-match'[\x00-\x1f\x7f]'){throw 'Assisted native campaign name is malformed.'}
    Assert-KmcChunk7NativeBootstrapPreflight $o.preflight $true
    $preset=$o.preset
    Assert-KmcAssistedObjectValue $preset @('source','guid','area','enterPoint','enterPointArea','makeAutosave','charGen','playerBlueprint','usesPresetPlayer','companionCount') 'assisted authored preset'
    Assert-KmcBootstrapInventoryString $preset.source 'BlueprintRoot.NewGamePreset'
    foreach($key in @('guid','area','enterPoint','enterPointArea','playerBlueprint')){Assert-KmcAssistedGuid32 $preset.$key}
    foreach($key in @('area','enterPointArea')){Assert-KmcBootstrapInventoryString $preset.$key $expected.area}
    Assert-KmcChunk7Boolean $preset.makeAutosave $true
    foreach($key in @('charGen','usesPresetPlayer')){if($preset.$key-isnot[bool]){throw 'Assisted authored preset flag is not boolean.'}}
    Assert-KmcOrdinaryInteger $preset.companionCount 0 6
    $before=$o.before
    Assert-KmcAssistedObjectValue $before @('frame','game','player','saveManager','mainActor','mainActorId','gameId','gameName','area','mode','nativeLoading','authority','party') 'assisted empty menu world'
    Assert-KmcOrdinaryInteger $before.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $before.mainActor 0 0
    foreach($key in @('mainActorId','gameName','area')){if($null-ne$before.$key){throw 'Assisted menu was not empty before native new game.'}}
    if($null-ne$before.gameId-and$before.gameId-isnot[string]){throw 'Assisted menu campaign placeholder is malformed.'}
    if($before.mode-isnot[string]-or$before.mode.Length-eq0-or$before.mode.Length-gt64){throw 'Assisted menu mode is missing.'}
    Assert-KmcChunk7Boolean $before.nativeLoading $false;Assert-KmcAssistedArray $before.party 0 0 'assisted initial menu roster'
    $owners=[Collections.Generic.HashSet[int]]::new()
    foreach($key in @('game','player','saveManager')){Assert-KmcAssistedLabel $before.$key $labels;if(-not$owners.Add($before.$key)){throw 'Assisted native menu owners alias.'};Assert-KmcOrdinaryInteger $initial.$key $before.$key $before.$key}
    Assert-KmcAssistedAuthority $before.authority $root $generation $expected 'initial-auto' 0 0 0 0
    Assert-KmcAssistedWorld $initial $expected $labels $before.frame $false
    $main=@($initial.party|Where-Object id -CEQ $expected.mainActorId)[0];Assert-KmcBootstrapInventoryString $main.blueprint $preset.playerBlueprint
    Assert-KmcAssistedAuthority $initial.authority $root $generation $expected 'initial-auto' 1 0 0 0
    Assert-KmcAssistedWorld $o.beforeSetup $expected $labels $initial.frame $true
    Assert-KmcAssistedAuthority $o.beforeSetup.authority $root $generation $expected 'setup' 1 0 0 0
    Assert-KmcAssistedWorld $o.fieldWorld $expected $labels $o.beforeSetup.frame $true
    Assert-KmcAssistedAuthority $o.fieldWorld.authority $root $generation $expected 'setup' (1+$levelups) $levelups $levelups 0
    Assert-KmcAssistedWorld $o.savedWorld $expected $labels $o.fieldWorld.frame $true
    Assert-KmcAssistedWorld $o.settledWorld $expected $labels $o.savedWorld.frame $true
    Assert-KmcOrdinaryInteger $manual.authority.manualNumber 1 ([int]::MaxValue);$number=$manual.authority.manualNumber
    foreach($value in @($o.savedWorld,$o.settledWorld)){
        Assert-KmcAssistedAuthority $value.authority $root $generation $expected 'complete' $writes $levelups (1+$levelups) $number
    }
    foreach($value in @($initial,$o.beforeSetup,$o.fieldWorld,$o.savedWorld,$o.settledWorld)){
        foreach($key in @('game','player','saveManager','mainActor')){Assert-KmcOrdinaryInteger $value.$key $initial.$key $initial.$key}
        foreach($actor in $value.party){
            foreach($key in @('object','descriptor','view')){if($owners.Contains([int]$actor.$key)){throw 'Assisted native actor aliases a world owner.'}}
            foreach($fact in @($actor.features)+@($actor.buffs)){if($owners.Contains([int]$fact.object)){throw 'Assisted native fact aliases a world owner.'}}
        }
        $observedMain=@($value.party|Where-Object id -CEQ $expected.mainActorId)[0]
        foreach($key in @('descriptor','view')){Assert-KmcAssistedLabel $main.$key $labels;Assert-KmcOrdinaryInteger $observedMain.$key $main.$key $main.$key}
    }
    foreach($value in @($o.savedWorld,$o.settledWorld)){
        if($value.party.Count-ne$o.fieldWorld.party.Count){throw 'Assisted save/cleanup changed native field membership.'}
        foreach($actor in $o.fieldWorld.party){$rows=@($value.party|Where-Object id -CEQ $actor.id);if($rows.Count-ne1){throw 'Assisted save/cleanup replaced a field actor.'};foreach($key in @('object','descriptor','view','blueprint','masterId','petId','characterLevel','classes')){Assert-KmcAssistedSame $actor.$key $rows[0].$key ('assisted saved actor '+$key)}}
    }
    for($i=0;$i-lt$writes;$i++){
        $phase=if($i-eq0){'initial-auto'}elseif($i-eq$writes-1){'final-manual'}else{'levelup-auto'}
        $minimum=if($i-eq0){$before.frame}elseif($i-eq$writes-1){$o.fieldWorld.frame}else{$o.beforeSetup.frame}
        Assert-KmcAssistedWrite $o.nativeWrites[$i] $Request $expected $labels $root ($i+1) $phase $minimum
        $authPhase=if($phase-ceq'final-manual'){'manual'}else{$phase}
        $completedLevels=if($phase-ceq'final-manual'){$levelups}else{0}
        $reservations=if($phase-ceq'initial-auto'){0}elseif($phase-ceq'levelup-auto'){1}else{1+$levelups}
        $manualNumber=if($phase-ceq'final-manual'){$number}else{0}
        Assert-KmcAssistedAuthority $o.nativeWrites[$i].authority $root $generation $expected $authPhase ($i+1) $completedLevels $reservations $manualNumber
    }
    Assert-KmcOrdinaryInteger $o.nativeWrites[0].frame $initial.frame $initial.frame
    Assert-KmcOrdinaryInteger $manual.frame $o.savedWorld.frame $o.savedWorld.frame
    Assert-KmcAssistedSame $o.finalManual $manual 'assisted final Manual receipt'
    Assert-KmcAssistedBootstrapProbe $o $expected $labels
    $d=$o.derivedTarget
    Assert-KmcAssistedObjectValue $d @('mainActorId','characterLevel','rangerLevel','targetLevel') 'assisted derived level target'
    Assert-KmcBootstrapInventoryString $d.mainActorId $expected.mainActorId
    Assert-KmcOrdinaryInteger $d.characterLevel $main.characterLevel $main.characterLevel
    $rank=@($main.classes|Where-Object guid -CEQ 'cda0615668a6df14eb36ba19ee881af6');$rangerLevel=if($rank.Count-eq0){0}else{$rank[0].level}
    Assert-KmcOrdinaryInteger $d.rangerLevel $rangerLevel $rangerLevel
    Assert-KmcOrdinaryInteger $d.targetLevel 4 8
    if($d.targetLevel-ne($d.characterLevel+[Math]::Max(0,4-$d.rangerLevel))){throw 'Assisted target was not derived from native Ranger rank.'}
    Assert-KmcChunk7AssistedSetupEvidence $o.setup $expected
    Assert-KmcChunk7AssistedSetupEvidence $o.setupAtCleanup $expected
    Assert-KmcAssistedSame $o.setup $o.setupAtCleanup 'assisted setup retained through native cleanup'
    $storyFrames=@($o.setup.story.lastObserved)
    foreach($attempt in $o.setup.story.attempts){$storyFrames+=@($attempt.before,$attempt.after)}
    foreach($snapshot in $storyFrames){Assert-KmcOrdinaryInteger $snapshot.frame $initial.frame $o.beforeSetup.frame}
    Assert-KmcOrdinaryInteger $o.setup.experience.before.frame $o.beforeSetup.frame $o.beforeSetup.frame
    foreach($effect in @($o.setup.experience,$o.setup.hireFunds)){foreach($snapshot in @($effect.before,$effect.after)){Assert-KmcOrdinaryInteger $snapshot.targetLevel $d.targetLevel $d.targetLevel}}
    Assert-KmcOrdinaryInteger $o.setup.experience.before.level $d.characterLevel $d.characterLevel
    Assert-KmcOrdinaryInteger $o.setup.hireFunds.before.level $d.targetLevel $d.targetLevel
    Assert-KmcOrdinaryInteger $o.fieldWorld.frame $o.setup.hireFunds.after.frame ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $o.setup.hireFunds.before.experience $o.setup.experience.after.experience $o.setup.experience.after.experience
    Assert-KmcOrdinaryInteger $o.setup.hireFunds.before.money $o.setup.experience.after.money $o.setup.experience.after.money
    Assert-KmcChunk7AssistedPreservation $o.preservation $o.setup $expected $labels $levelups
    Assert-KmcAssistedSetupWorldBindings $o $expected
    Assert-KmcAssistedActorRows $o.afterSetupActors $expected.mainActorId $labels $true 12
    if($o.afterSetupActors.Count-ne$o.fieldWorld.party.Count){throw 'Assisted after-setup actors differ from native field.'}
    foreach($actor in $o.fieldWorld.party){
        $matches=@($o.afterSetupActors|Where-Object id -CEQ $actor.id);if($matches.Count-ne1){throw 'Assisted after-setup actor changed.'};$after=$matches[0]
        foreach($key in @('object','descriptor','view','blueprint','masterId','petId','characterLevel','classes','features','buffs','actions')){Assert-KmcAssistedSame $after.$key $actor.$key ('assisted after-setup '+$key)}
        $xp=if($actor.id-ceq$expected.mainActorId){$o.setup.hireFunds.after.experience}else{$actor.experience};Assert-KmcOrdinaryInteger $after.experience $xp $xp
    }
    if($hasBuilder-ne($d.targetLevel-gt$d.characterLevel)){throw 'Assisted native builder evidence differs from the derived Ranger level target.'}
    if($levelups-eq1){
        if(-not$hasBuilder){throw 'Legacy level-up Auto lacks its native builder proof.'}
        $s=[pscustomobject]@{existingActorId=$expected.mainActorId;minimumRangerLevel=4}
        $proof=Get-KmcChunk7OrdinaryChoiceProof $Request.runId $s $false @($o.beforeSetup.party.id) $o.choices $o.setup.experience.after.money $o.builderComplete
        Assert-KmcOrdinaryInteger $proof.identity.rangerLevel $d.rangerLevel $d.rangerLevel
        Assert-KmcOrdinaryInteger $proof.initialLevel $d.characterLevel $d.characterLevel;Assert-KmcOrdinaryInteger $proof.finalLevel $d.targetLevel $d.targetLevel
        Assert-KmcOrdinaryInteger $proof.xp $o.setup.experience.after.experience $o.setup.experience.after.experience
        Assert-KmcOrdinaryInteger $proof.identity.frame $o.setup.experience.after.frame $auto.frame
        Assert-KmcOrdinaryInteger $proof.lastFrame $proof.identity.frame $auto.frame
        Assert-KmcOrdinaryInteger $o.setup.hireFunds.before.frame $auto.frame ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $auto.requestedDescriptor $o.nativeWrites[0].preparedDescriptor $o.nativeWrites[0].preparedDescriptor
        if($auto.preparedDescriptor-eq$o.nativeWrites[0].requestedDescriptor){throw 'Assisted level Auto reused an old requested descriptor.'}
        Assert-KmcAssistedOrdinaryProbe $o $proof $labels
        Assert-KmcAssistedDescriptors $o.afterLevelupDescriptors $Request $auto $null $labels
        $mainFinal=@($o.fieldWorld.party|Where-Object id -CEQ $expected.mainActorId)[0]
        Assert-KmcBootstrapInventoryString $o.builderComplete.petId $mainFinal.petId
        $finalRank=@($mainFinal.classes|Where-Object guid -CEQ 'cda0615668a6df14eb36ba19ee881af6')[0].level
        Assert-KmcOrdinaryInteger $o.builderComplete.rangerLevel $finalRank $finalRank
    }elseif($hasBuilder){
        # The installed game has no LevelUpAutoSaveController in its active mode
        # factory. The current producer therefore proves native choices and the
        # resulting Horse, then writes only its selected final native Manual.
        $s=[pscustomobject]@{existingActorId=$expected.mainActorId;minimumRangerLevel=4}
        $proof=Get-KmcChunk7OrdinaryChoiceProof $Request.runId $s $false @($o.beforeSetup.party.id) $o.choices $o.setup.experience.after.money $o.builderComplete
        Assert-KmcOrdinaryInteger $proof.identity.rangerLevel $d.rangerLevel $d.rangerLevel
        Assert-KmcOrdinaryInteger $proof.initialLevel $d.characterLevel $d.characterLevel
        Assert-KmcOrdinaryInteger $proof.finalLevel $d.targetLevel $d.targetLevel
        Assert-KmcOrdinaryInteger $proof.xp $o.setup.experience.after.experience $o.setup.experience.after.experience
        Assert-KmcOrdinaryInteger $proof.identity.frame $o.setup.experience.after.frame $o.fieldWorld.frame
        Assert-KmcOrdinaryInteger $proof.lastFrame $proof.identity.frame $o.fieldWorld.frame
        Assert-KmcOrdinaryInteger $o.setup.hireFunds.before.frame $proof.lastFrame ([int]::MaxValue)
        $mainFinal=@($o.fieldWorld.party|Where-Object id -CEQ $expected.mainActorId)[0]
        Assert-KmcBootstrapInventoryString $o.builderComplete.petId $mainFinal.petId
        $finalRank=@($mainFinal.classes|Where-Object guid -CEQ 'cda0615668a6df14eb36ba19ee881af6')[0].level
        Assert-KmcOrdinaryInteger $o.builderComplete.rangerLevel $finalRank $finalRank
    }else{
        Assert-KmcAssistedArray $o.choices 0 0 'assisted no-levelup native choices'
        if($d.characterLevel-ne$d.targetLevel-or$d.rangerLevel-lt4){throw 'Assisted no-levelup route lacks existing attained Ranger progression.'}
    }
    Assert-KmcAssistedProfile $o.nativeProfileInventory $o.fieldWorld $o.setup
    $preservedCharacters=@($o.preservation.rows|Where-Object purpose -CEQ 'hire-funds-before')[0].state.partyCharacters
    Assert-KmcAssistedSame $o.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativePartyCharacterIds @($preservedCharacters.id) 'assisted raw native character roster'
    $nativeClasses=@($o.fixtureReadiness.mainProgression.classes|ForEach-Object{[pscustomobject]@{guid=$_.classGuid;level=$_.level}}|Sort-Object guid)
    $mainClasses=@(@($o.fieldWorld.party|Where-Object id -CEQ $expected.mainActorId)[0].classes|Sort-Object guid)
    Assert-KmcAssistedSame $nativeClasses $mainClasses 'assisted raw native main class ranks'
    Assert-KmcAssistedFixtureReadiness $o.fixtureReadiness $Request $o.fieldWorld $o.settledWorld $o.nativeProfileInventory $root $auto
    Assert-KmcOrdinaryInteger $o.fixtureReadiness.experienceRatePercent $o.setup.hireFunds.after.experienceRatePercent $o.setup.hireFunds.after.experienceRatePercent
    Assert-KmcChunk7Boolean $o.fixtureReadiness.saveSettings.onlyOneSave $false
    $numbering=$o.nativeNumbering
    Assert-KmcAssistedObjectValue $numbering @('method','token','moduleMvid','frame','number','fileName','sourceDescriptors','outputInitiallyAbsent','slotProjectionEnabled') 'assisted native Manual numbering'
    foreach($pair in @(@('method','FindUnusedSaveNumber'),@('token','06008024'),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'),@('fileName',$manual.fileName))){Assert-KmcBootstrapInventoryString $numbering.($pair[0]) $pair[1]}
    Assert-KmcOrdinaryInteger $numbering.frame $o.fieldWorld.frame $manual.frame;Assert-KmcOrdinaryInteger $numbering.number $number $number
    Assert-KmcChunk7Boolean $numbering.outputInitiallyAbsent $true;Assert-KmcChunk7Boolean $numbering.slotProjectionEnabled $false
    Assert-KmcAssistedDescriptors $numbering.sourceDescriptors $Request $auto $null $labels
    Assert-KmcAssistedDescriptors $o.settledDescriptors $Request $auto $manual $labels
    Assert-KmcAssistedArrangement $o.beforeManualArrangement $o.fieldWorld $o.setup
    Assert-KmcAssistedArrangement $o.afterManualArrangement $o.savedWorld $o.setup
    Assert-KmcAssistedSame $o.beforeManualArrangement $o.afterManualArrangement 'assisted save-only native arrangement'
    Assert-KmcAssistedManualProbe $Request $o $labels
    $c=$o.cleanup
    Assert-KmcAssistedObjectValue $c @('custodySettled','callbackEntered','callbackReturned','dispatched','nativeWorkerSettled','observersReleased','identityRegistryReleased','nativeGateReleased','slotProjectionBefore','slotProjectionAfter','presentationRestored','manualRefunds','manualFactRemovals','manualCompanionRemovals','authority') 'assisted native cleanup'
    foreach($key in @('custodySettled','callbackEntered','callbackReturned','dispatched','nativeWorkerSettled','observersReleased','identityRegistryReleased','nativeGateReleased','presentationRestored')){Assert-KmcChunk7Boolean $c.$key $true}
    foreach($key in @('slotProjectionBefore','slotProjectionAfter')){Assert-KmcChunk7Boolean $c.$key $false}
    foreach($key in @('manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $c.$key 0 0}
    Assert-KmcAssistedAuthority $c.authority $root $generation $expected 'closed' $writes $levelups (1+$levelups) $number
}

function Assert-KmcChunk7AssistedPendingArtifact($Request,$Artifact) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-assisted-fixture-cleanup-pending'
    Assert-KmcBootstrapInventoryString $Artifact.status 'FAIL'
    if($Artifact.errors.Count-eq0){throw 'Assisted pending custody lacks its failure reason.'}
    foreach($errorText in $Artifact.errors){if($errorText-isnot[string]-or[string]::IsNullOrWhiteSpace($errorText)){throw 'Assisted pending error is malformed.'}}
    $o=$Artifact.observations
    Assert-KmcAssistedObjectValue $o @('cleanupCustodyRetained','cleanupPendingAtHostBoundary','reason','authority') 'assisted pending custody'
    foreach($key in @('cleanupCustodyRetained','cleanupPendingAtHostBoundary')){Assert-KmcChunk7Boolean $o.$key $true}
    if($o.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($o.reason)){throw 'Assisted pending reason is missing.'}
    # Failure can occur before BindAndStart. Nullable native authority fields are
    # observations only and never substitute for the successful terminal proof.
    $a=$o.authority
    Assert-KmcAssistedObjectValue $a @('root','generation','phase','maximumNativeLoads','maximumNativeWrites','admittedNativeLoads','admittedNativeWrites','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites','freezeCount','windowOpen','frozenGameId','frozenGameName','manualNumber','manualLeaf','reservationCount','levelupWrites') 'assisted pending authority'
    foreach($key in @('generation','authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $a.$key 0 ([int]::MaxValue)}
    foreach($key in @('maximumNativeLoads','maximumNativeWrites','admittedNativeLoads','admittedNativeWrites','freezeCount','manualNumber','reservationCount','levelupWrites')){if($null-ne$a.$key){Assert-KmcOrdinaryInteger $a.$key 0 ([int]::MaxValue)}}
    foreach($key in @('phase','frozenGameId','frozenGameName','manualLeaf')){if($null-ne$a.$key-and$a.$key-isnot[string]){throw 'Assisted pending nullable authority text is malformed.'}}
    if($null-ne$a.windowOpen-and$a.windowOpen-isnot[bool]){throw 'Assisted pending window state is malformed.'}
    if($null-ne$a.root){Assert-KmcBootstrapInventoryString $a.root (Get-KmcAssistedSaveRoot $Request)}
}

function Assert-KmcChunk7AssistedFixtureArtifact($Request,$Manifest,[string]$Status='PASS') {
    $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-assisted-fixture-evidence'-or$_.relativePath-ceq'chunk7-assisted-fixture-evidence.json'})
    $pending=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-assisted-fixture-cleanup-pending'-or$_.relativePath-ceq'chunk7-assisted-fixture-cleanup-pending.json'})
    if(-not(Test-KmcChunk7AssistedFixtureScenario $Request.scenario)){if($records.Count-ne0-or$pending.Count-ne0){throw 'Foreign request supplied assisted fixture evidence.'};return}
    Assert-KmcChunk7AssistedFixtureSelection $Request
    if($pending.Count-gt0){
        $sidecar=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-assisted-fixture-cleanup-pending.json' 'chunk7-assisted-fixture-cleanup-pending'
        Assert-KmcChunk7AssistedPendingArtifact $Request $sidecar
        if($Status-ceq'PASS'){throw 'Assisted pending custody cannot qualify a successful result.'}
    }
    if($Status-ceq'FAIL'-and$records.Count-eq0){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-assisted-fixture-evidence.json' 'chunk7-assisted-fixture-evidence'
    if($pending.Count-gt0-and$artifact.status-ceq'PASS'){throw 'Assisted pending failure conflicts with successful terminal evidence.'}
    Assert-KmcChunk7AssistedFixtureEvidence $Request $artifact $Status
}
