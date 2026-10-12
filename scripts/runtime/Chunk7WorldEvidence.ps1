# Original strict reader for the bounded native world fixture; no staging or native writes.
Set-StrictMode -Version Latest
function Get-KmcChunk7WorldRows {
    @('chunk7-two-pair-exploration','C7-world-fixture','C7-world-native-acquisition','C7-world-two-native-mounts','C7-world-selection-routing',
      'C7-world-independent-dismount','C7-world-cycles','C7-world-sampling','C7-world-cleanup','C7-world-runtime','C7-world-visual','C7-world-interrupted')
}
function Assert-KmcChunk7ArtifactEnvelope($Request,$Artifact,[string]$Kind) {
    Assert-KmcExactProperties $Artifact @('schemaVersion','evidenceKind','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','createdAtUtc','status','observations','errors') 'Chunk 7 artifact'
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if($Artifact.$field-isnot[string]-or$Artifact.$field-cne$Request.$field){throw "Chunk 7 artifact identity differs: $field"}
    }
    $date=[DateTimeOffset]::MinValue
    if($Artifact.schemaVersion-ne1-or$Artifact.evidenceKind-cne$Kind-or$Artifact.status-cnotin@('PASS','FAIL')-or
        $Artifact.errors-isnot[Array]-or-not[DateTimeOffset]::TryParse([string]$Artifact.createdAtUtc,[ref]$date)){throw 'Chunk 7 artifact envelope differs.'}
    if($Artifact.status-ceq'PASS'-and@($Artifact.errors).Count-ne0){throw 'PASS artifact contains errors.'}
}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,[string]$Leaf,[string]$Kind) {
    $records=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$Leaf-or$_.kind-ceq$Kind})
    if($records.Count-ne1-or$records[0].relativePath-cne$Leaf-or$records[0].kind-cne$Kind){throw 'Exact native artifact manifest binding is required.'}
    $path=Assert-KmcChildPath (Join-Path $Request.evidenceRoot $Leaf) $Request.evidenceRoot 'Chunk 7 artifact'
    Assert-KmcNotReparsePoint $path 'Chunk 7 artifact'; Assert-KmcNotHardLink $path 'Chunk 7 artifact'
    $before=Get-Item -LiteralPath $path -Force
    if($before.Length-ne[long]$records[0].length-or(Get-KmcSha256 $path)-cne$records[0].sha256){throw 'Native artifact differs from manifest bytes.'}
    Assert-KmcManifestJsonMembersUnique $path 'Chunk 7 native artifact'
    $artifact=Read-KmcJson $path
    if((Get-KmcSha256 $path)-cne$records[0].sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$before.LastWriteTimeUtc.Ticks){throw 'Native artifact changed during read.'}
    Assert-KmcChunk7ArtifactEnvelope $Request $artifact $Kind
    return $artifact
}
function Test-KmcChunk7FiniteNumber($Value) {
    if($Value-isnot[byte]-and$Value-isnot[int16]-and$Value-isnot[int32]-and$Value-isnot[int64]-and$Value-isnot[single]-and$Value-isnot[double]-and$Value-isnot[decimal]){return $false}
    return -not[double]::IsNaN([double]$Value)-and-not[double]::IsInfinity([double]$Value)
}
function Assert-KmcChunk7Boolean($Value,[bool]$Expected) {if($Value-isnot[bool]-or$Value-ne$Expected){throw 'Native evidence boolean is missing, mistyped or differs.'}}
function Assert-KmcChunk7ExactIds($Rows,[string[]]$Expected,[string]$Field='id') {
    $actual=@($Rows|ForEach-Object{[string]$_.$Field}|Sort-Object -CaseSensitive)
    if(($actual-join'|')-cne(($Expected|Sort-Object -CaseSensitive)-join'|')){throw 'Native actor identity set differs.'}
}
function Assert-KmcChunk7Resources($Before,$After,[string[]]$ActorIds) {
    Assert-KmcChunk7ExactIds @($Before.actors) $ActorIds
    Assert-KmcChunk7ExactIds @($After.actors) $ActorIds
    if(-not(Test-KmcExactJsonInteger $Before.gameTicks)-or-not(Test-KmcExactJsonInteger $After.gameTicks)){throw 'Native game time must be integer ticks.'}
    $elapsed=([double]$After.gameTicks-[double]$Before.gameTicks)/10000000.0
    if([double]::IsNaN($elapsed)-or[double]::IsInfinity($elapsed)-or$elapsed-lt0-or$elapsed-gt30){throw 'Native resource window time invalid.'}
    foreach($a in $Before.actors) {
        $b=@($After.actors|Where-Object id -CEQ $a.id)[0]
        foreach($field in @('move','standard','swift')) {
            if(-not(Test-KmcChunk7FiniteNumber $a.$field)-or-not(Test-KmcChunk7FiniteNumber $b.$field)){throw 'Native action window requires finite JSON numbers.'}
            $x=[double]$a.$field; $y=[double]$b.$field
            if([double]::IsNaN($x)-or[double]::IsInfinity($x)-or[double]::IsNaN($y)-or[double]::IsInfinity($y)-or$x-lt0-or$y-lt0-or
                [Math]::Abs($y-[Math]::Max(0,$x-$elapsed))-gt0.03){throw 'Exploration control changed native action spending.'}
        }
        if($a.prepared-isnot[bool]-or$b.prepared-isnot[bool]-or$a.prepared-ne$b.prepared-or$a.initiative-ne$b.initiative){throw 'Exploration changed native preparation/initiative.'}
    }
}
function Assert-KmcChunk7InputSequence($Rows,$Specs,[string[]]$ActorIds,$Commands,$Shells) {
    if(@($Rows).Count-ne@($Specs).Count){throw 'Exact native relationship input sequence incomplete.'}
    for($i=0;$i-lt@($Specs).Count;$i++) {
        $v=$Rows[$i];$s=$Specs[$i]
        Assert-KmcChunk7Boolean $v.clicked $true;Assert-KmcChunk7Boolean $v.shellTerminal $true;Assert-KmcChunk7Boolean $v.processTerminal $true
        if($v.label-cne$s[0]-or$v.casterId-cne$s[1]-or$v.targetId-cne$s[2]-or$v.abilityGuid-cne$s[3]-or
            $v.nativeCastDelta-ne1-or$v.nativeRefusalDelta-ne0-or$v.commandObject-eq0-or-not$Commands.Add([long]$v.commandObject)-or
            [string]::IsNullOrEmpty($v.shellControl)-or-not$Shells.Add([string]$v.shellControl)-or$v.generationAtInit-lt0-or
            $v.selectionAfterInput-ceq$v.casterId-or$v.selectionAfterInput-cnotin$ActorIds){throw 'Native input command/shell/selection identity differs.'}
        Assert-KmcChunk7Resources $v.resourcesBefore $v.resourcesAfter $ActorIds
    }
}
function Get-KmcChunk7Subscriptions($Snapshot) {
    Assert-KmcChunk7Boolean $Snapshot.executing $false
    $entries=@($Snapshot.entries|ForEach-Object{[string]$_.interface+'|'+[string]$_.type+'|'+[string]$_.identity}|Sort-Object -CaseSensitive)
    if($entries.Count-ne$Snapshot.ownedInterfaceEntries-or@($Snapshot.entries|Select-Object -ExpandProperty identity -Unique).Count-ne$Snapshot.uniqueOwnedSubscribers){throw 'Subscription counts differ from exact rows.'}
    return ($entries-join';')
}
function Assert-KmcChunk7Measurements($Rows) {
    $labels=@('unmounted-0','mounted-1','mounted-2','unmounted-after-cycles');$active=@(0,1,2,0)
    if(@($Rows).Count-ne4){throw 'All four comparable measurement windows are required.'}
    $subscriptions=$null
    for($i=0;$i-lt4;$i++) {
        $m=$Rows[$i]
        if($m.label-cne$labels[$i]-or$m.warmupFrames-ne30-or$m.sampleFrames-ne120-or
            @($m.serviceTicks).Count-ne120-or@($m.frameSeconds).Count-ne120-or$m.endFrame-$m.startFrame-lt119){throw 'Measurement sample window differs.'}
        if(-not(Test-KmcExactJsonInteger $m.stopwatchFrequency)-or$m.stopwatchFrequency-le0){throw 'Measurement clock frequency invalid.'}
        foreach($t in $m.serviceTicks){if(-not(Test-KmcExactJsonInteger $t)-or$t-lt0){throw 'Service timer sample invalid.'}}
        foreach($t in $m.frameSeconds){if(-not(Test-KmcChunk7FiniteNumber $t)-or$t-le0){throw 'Frame timer sample invalid.'}}
        foreach($field in @('managedHeapBefore','managedHeapAfter','managedHeapDelta','kmcLogMessages','kmcLogCharacters')){if(-not(Test-KmcExactJsonInteger $m.$field)){throw 'Measurement count invalid.'}}
        if($m.managedHeapBefore-lt0-or$m.managedHeapAfter-lt0-or$m.managedHeapDelta-ne($m.managedHeapAfter-$m.managedHeapBefore)-or$m.kmcLogMessages-lt0-or$m.kmcLogCharacters-lt0){throw 'Heap/log observations inconsistent.'}
        $sortedTicks=@($m.serviceTicks|Sort-Object);$sortedFrames=@($m.frameSeconds|Sort-Object)
        $expected=@{serviceMeanMs=([double]($m.serviceTicks|Measure-Object -Average).Average*1000.0/$m.stopwatchFrequency);
            serviceP50Ms=([double]$sortedTicks[59]*1000.0/$m.stopwatchFrequency);serviceP95Ms=([double]$sortedTicks[113]*1000.0/$m.stopwatchFrequency);
            frameP50Ms=([double]$sortedFrames[59]*1000.0);frameP95Ms=([double]$sortedFrames[113]*1000.0)}
        foreach($key in $expected.Keys){if(-not(Test-KmcChunk7FiniteNumber $m.$key)-or[Math]::Abs([double]$m.$key-$expected[$key])-gt0.001){throw 'Timing summary differs from raw samples.'}}
        if(@($m.collectionsBefore).Count-ne3-or@($m.collectionsAfter).Count-ne3){throw 'Native GC observations missing.'}
        for($g=0;$g-lt3;$g++){if(-not(Test-KmcExactJsonInteger $m.collectionsBefore[$g])-or-not(Test-KmcExactJsonInteger $m.collectionsAfter[$g])-or
            $m.collectionsBefore[$g]-lt0-or$m.collectionsAfter[$g]-lt$m.collectionsBefore[$g]){throw 'Native GC observations invalid.'}}
        Assert-KmcChunk7Boolean $m.forcedCollection $false
        foreach($owner in @($m.ownersBefore,$m.ownersAfter)) {
            if($owner.active-ne$active[$i]-or$owner.owners-ne$active[$i]-or$owner.boundaries-lt1-or$owner.boundaries-gt2-or
                $owner.participants-ne$owner.boundaries-or$owner.failedConstructions-ne0-or$owner.liveControlBoundaries-ne0-or$owner.preparedOrViewResidue-ne$active[$i]){throw 'Comparable window contains unexpected ownership, commands or view residue.'}
        }
        $before=Get-KmcChunk7Subscriptions $m.subscriptionsBefore;$after=Get-KmcChunk7Subscriptions $m.subscriptionsAfter
        if($before-cne$after-or($null-ne$subscriptions-and$before-cne$subscriptions)){throw 'Settled windows accumulated or lost owned native subscriptions.'}
        $subscriptions=$before
    }
    # Timing values are observations, not an invented universal performance threshold.
}
function Assert-KmcChunk7WorldEvidence($Request,$Artifact,[string]$Status) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-world-evidence'
    if($Request.scenario-cne'chunk7-two-pair-exploration'){throw 'Chunk 7 world evidence under another scenario.'}
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS world result requires PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations; Assert-KmcChunk7Boolean $o.capability.archiveNativeEnabled $true; $first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mammoth='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'
    $original=@($first,$mammoth,$second)
    if($o.capability.archiveNativeEnabled-ne$true-or$o.capability.nativeWriterSchema-ne3-or$o.capability.nativePairCapacity-lt2-or
        $o.capability.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
        $o.capability.workingSha256-cne$Request.fixture.working.sha256){throw 'World native capability or fixture receipt differs.'}
    Assert-KmcChunk7ExactIds @($o.partyBefore) $original
    $horseRows=@($o.partyWithHorse|Where-Object blueprint -CEQ '4016c7db400ab721ff125aef9e65e202')
    if($horseRows.Count-ne1-or$horseRows[0].masterId-cne$second){throw 'Exact native Horse acquisition identity differs.'}
    $horse=[string]$horseRows[0].id;[Guid]$parsed=[Guid]::Empty
    if(-not[Guid]::TryParse($horse,[ref]$parsed)-or$horse-cin$original){throw 'Created Horse ID invalid or reused.'}
    $actors=@($original)+@($horse);Assert-KmcChunk7ExactIds @($o.partyWithHorse) $actors
    foreach($party in @($o.partyBefore,$o.partyWithHorse)) {
        $r=@($party|Where-Object id -CEQ $first)[0];$m=@($party|Where-Object id -CEQ $mammoth)[0]
        if($r.petId-cne$mammoth-or$m.masterId-cne$first-or$m.blueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'){throw 'Original Mammoth ownership changed.'}
    }
    $a=$o.acquisition
    if($a.nativeRangerCommitCount-ne4-or$a.huntersBondSelectionLevel-ne4-or$a.rangerCompanionSelectionLevel-ne4-or$a.horseFactRankAtCommit-ne1-or
        $a.horsePresentAtNativeCommit-ne$true-or[string]::IsNullOrEmpty($a.horseFeatureSourceGuid)){throw 'Native Ranger acquisition proof incomplete.'}
    $inputSpecs=@(@('mount-mammoth',$first,$mammoth,'f053faad986631688defa003cd7bda0e'),@('mount-horse',$second,$horse,'f053faad986631688defa003cd7bda0e'),
        @('dismount-mammoth',$first,$first,'3af2b81f4d72bbb30501fa730fcdf36e'),@('dismount-horse',$second,$second,'3af2b81f4d72bbb30501fa730fcdf36e'))
    $commands=New-Object 'Collections.Generic.HashSet[long]';$shells=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    Assert-KmcChunk7InputSequence $o.inputs $inputSpecs $actors $commands $shells
    $both=@($o.snapshots|Where-Object label -CEQ 'both-mounted');$after=@($o.snapshots|Where-Object label -CEQ 'movement-complete')
    if($both.Count-ne1-or$after.Count-ne1){throw 'Two-pair native world windows missing.'}
    $owners=@($both[0].owners|Where-Object state -CEQ 'Active')
    if($owners.Count-ne2-or$owners[0].ownerId-ceq$owners[1].ownerId){throw 'Two distinct active native owners required.'}
    Assert-KmcChunk7ExactIds $owners @($first,$second) 'riderId';Assert-KmcChunk7ExactIds $owners @($mammoth,$horse) 'mountId'
    foreach($owner in $owners) {
        [Guid]$id=[Guid]::Empty
        if(-not[Guid]::TryParse([string]$owner.ownerId,[ref]$id)-or$owner.relationshipState-cne'Mounted'-or$owner.generation-lt1){throw 'Active world ownership identity invalid.'}
        $same=@($after[0].owners|Where-Object ownerId -CEQ $owner.ownerId)
        if($same.Count-ne1-or$same[0].riderId-cne$owner.riderId-or$same[0].mountId-cne$owner.mountId-or$same[0].generation-ne$owner.generation-or$same[0].state-cne'Active'){throw 'Selection/movement changed exact pair ownership.'}
    }
    $split=@($o.snapshots|Where-Object label -CEQ 'first-dismounted-second-retained')
    if($split.Count-ne1){throw 'Independent native Dismount window missing.'}
    $retained=@($split[0].owners|Where-Object state -CEQ 'Active');$expected=@($owners|Where-Object riderId -CEQ $second)[0]
    if($retained.Count-ne1-or$retained[0].ownerId-cne$expected.ownerId-or$retained[0].generation-ne$expected.generation){throw 'Dismount changed the other pair owner.'}
    $moveSpecs=@(@('mammoth',$first,$mammoth),@('horse',$second,$horse),@('unmounted-control',$first,$first))
    if(@($o.movements).Count-ne3){throw 'Three exact native movement windows required.'}
    for($i=0;$i-lt3;$i++) {
        $v=$o.movements[$i];$s=$moveSpecs[$i]
        Assert-KmcChunk7Boolean $v.commandTerminal $true
        foreach($point in @($v.start,$v.end,$v.requestedGoal)){foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Movement requires finite observed coordinates.'}}}
        if($v.label-cne$s[0]-or$v.selectedId-cne$s[1]-or$v.executorId-cne$s[2]-or$v.commandObject-eq0-or$v.commandTerminal-ne$true-or
            $v.selectionAfterInput-ceq$v.selectedId-or$v.selectionAfterInput-cnotin$actors){throw 'Native movement route identity differs.'}
        Assert-KmcChunk7ExactIds @($v.commandsBefore) $actors;Assert-KmcChunk7ExactIds @($v.commandsAfter) $actors
        foreach($before in $v.commandsBefore) {
            $post=@($v.commandsAfter|Where-Object id -CEQ $before.id)[0]
            if($before.id-ceq$v.executorId){if($post.moveObject-ne$v.commandObject-or$post.moveObject-eq$before.moveObject){throw 'Movement executor did not receive the exact new command.'}}
            elseif($post.moveObject-ne$before.moveObject){throw 'Native routing changed another actor command.'}
        }
        $distance=0.0;$residual=0.0
        foreach($axis in @('x','y','z')){$distance+=[Math]::Pow(([double]$v.end.$axis-[double]$v.start.$axis),2);$residual+=[Math]::Pow(([double]$v.end.$axis-[double]$v.requestedGoal.$axis),2)}
        if([double]::IsNaN($distance)-or[double]::IsInfinity($distance)-or[Math]::Sqrt($distance)-lt0.75-or
            [double]::IsNaN($residual)-or[double]::IsInfinity($residual)-or[Math]::Sqrt($residual)-gt1.0){throw 'Native movement did not meet bounded endpoint.'}
    }
    Assert-KmcChunk7Measurements $o.measurements
    $cycleSpecs=@();for($n=1;$n-le3;$n++){foreach($spec in $inputSpecs){$cycleSpecs+=,@(('cycle-'+$n+'-'+$spec[0]),$spec[1],$spec[2],$spec[3])}}
    Assert-KmcChunk7InputSequence $o.cycleInputs $cycleSpecs $actors $commands $shells
    if(@($o.cycles).Count-ne6){throw 'Three complete native mounted/settled lifecycle cycles required.'}
    $previous=@{};foreach($owner in $owners){$previous[$owner.riderId]=[long]$owner.generation}
    $allOwners=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($owner in $owners){[void]$allOwners.Add([string]$owner.ownerId)}
    $baselineSubscriptions=Get-KmcChunk7Subscriptions $o.measurements[0].subscriptionsBefore
    for($n=1;$n-le3;$n++) {
        $mounted=$o.cycles[($n-1)*2];$settled=$o.cycles[($n-1)*2+1]
        if($mounted.label-cne('cycle-'+$n+'-mounted')-or$settled.label-cne('cycle-'+$n+'-settled')-or
            @($mounted.owners).Count-ne2-or@($settled.owners).Count-ne0){throw 'Cycle ownership windows missing or unsettled.'}
        Assert-KmcChunk7ExactIds $mounted.owners @($first,$second) 'riderId'
        foreach($owner in $mounted.owners) {
            [Guid]$id=[Guid]::Empty
            if(-not[Guid]::TryParse([string]$owner.ownerId,[ref]$id)-or-not$allOwners.Add([string]$owner.ownerId)-or
                $owner.generation-le$previous[$owner.riderId]-or$owner.state-cne'Active'-or$owner.relationshipState-cne'Mounted'-or
                $owner.mountId-cne$(if($owner.riderId-ceq$first){$mammoth}else{$horse})){throw 'Remount reused stale ownership/generation or wrong actor.'}
            $previous[$owner.riderId]=[long]$owner.generation
        }
        foreach($window in @($mounted,$settled)) {
            if($window.boundaryCount-ne2-or$window.participantCount-ne2-or(Get-KmcChunk7Subscriptions $window.subscriptions)-cne$baselineSubscriptions){throw 'Lifecycle accumulated service boundaries or subscriptions.'}
        }
    }
    $c=$o.cleanup
    foreach($field in @('pairResidueAbsent','horseRemoved','controlsSettled','settingsAndSelectionRestored','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $c.$field $true}
    if($c.pairResidueAbsent-ne$true-or$c.horseRemoved-ne$true-or$c.controlsSettled-ne$true-or$c.settingsAndSelectionRestored-ne$true-or
        $c.nativeSaveWritten-ne$false-or$c.ordinaryRespecProven-ne$false-or$c.nativeProgressionRequiresExternalWorkingRestoration-ne$true-or
        $o.combatOrPersistenceProof-ne$false-or$o.thirdUnrelatedActorProven-ne$false){throw 'Exploration cleanup or evidence scope differs.'}
    Assert-KmcChunk7ExactIds @($c.partyAfter) $original
    if(($c.selectionBefore-join'|')-cne($c.selectionAfter-join'|')-or$c.pauseBefore-ne$c.pauseAfter-or$c.movementBefore-ne$c.movementAfter-or$c.pairedBefore-ne$c.pairedAfter){throw 'Settings/selection restoration differs.'}
    if(@($c.world.owners|Where-Object state -CNE 'Retired').Count-ne0-or@($c.pairBoundaries).Count-lt2){throw 'World cleanup left retained ownership.'}
    foreach($pair in $c.pairBoundaries){if($pair.state-cne'Unmounted'-or$pair.residueAbsent-ne$true-or$pair.ownerPresent-ne$false){throw 'Pair cleanup incomplete.'}}
}
function Assert-KmcChunk7WorldArtifact($Request,$Manifest,[string]$Status) {
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-world-evidence.json'-or$_.kind-ceq'chunk7-world-evidence'})
    if($Request.scenario-cne'chunk7-two-pair-exploration'){if($present.Count-ne0){throw 'Another scenario supplied Chunk 7 world evidence.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-world-evidence.json' 'chunk7-world-evidence'
    Assert-KmcChunk7WorldEvidence $Request $artifact $Status
    if($artifact.status-ceq'PASS') {
        $shots=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'movement-visuals/chunk7-two-pair-mounted.png'-and$_.kind-ceq'screenshot'})
        if($shots.Count-ne1){throw 'Two-pair screenshot is not immutably manifested.'}
    }
}
function Assert-KmcBaseGameNativeInventory($Request,$Artifact,$Dependencies,[switch]$PassThru) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'runtime-loader-inventory'
    if($Artifact.status-cne'PASS'-or$Artifact.observations.capturedInNativeProcess-ne$true-or
        -not(Test-KmcExactJsonInteger $Artifact.observations.processId)-or$Artifact.observations.processId-le0){throw 'Live loader inventory is incomplete.'}
    Assert-KmcChunk7Boolean $Artifact.observations.capturedInNativeProcess $true
    $mods=@($Artifact.observations.modEntries)
    foreach($entry in $mods){Assert-KmcChunk7Boolean $entry.active $true;Assert-KmcChunk7Boolean $entry.enabled $true}
    if($mods.Count-ne1-or$mods[0].id-cne'KingmakerMountedCombat'-or$mods[0].active-ne$true-or$mods[0].enabled-ne$true){throw 'Base-only run loaded another UMM entry or KMC was inactive/disabled.'}
    $assemblies=@($Artifact.observations.loadedAssemblies)
    if($assemblies.Count-lt4-or$assemblies.Count-gt2048){throw 'Loaded assembly inventory is incomplete or outside the bounded contract.'}
    $managedRoot=[IO.Path]::GetFullPath((Join-Path $Dependencies.gameRoot 'Kingmaker_Data/Managed')).TrimEnd('\')
    $loaderRoot=Join-Path $managedRoot 'UnityModManager'
    $kmcPath=[IO.Path]::GetFullPath((Join-Path $Dependencies.modsRoot 'KingmakerMountedCombat/KingmakerMountedCombat.dll'))
    $dynamicAssemblies=New-Object 'Collections.Generic.List[object]'
    foreach($assembly in $assemblies) {
        Assert-KmcExactProperties $assembly @('name','fullName','dynamic','location','mvid','sha256') 'Loaded native assembly'
        if($assembly.dynamic-isnot[bool]-or$assembly.name-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.name)-or
            $assembly.fullName-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.fullName)){throw 'Loaded assembly identity or dynamic flag is missing.'}
        if($assembly.dynamic) {
            # Runtime-generated assemblies have no file-backed byte identity.
            # Preserve every observed identity without inventing a stock/dynamic allowlist.
            if($null-ne$assembly.location-or$null-ne$assembly.mvid-or$null-ne$assembly.sha256){throw 'Dynamic assembly claims an unavailable file-backed identity.'}
            $dynamicAssemblies.Add([pscustomobject]@{name=[string]$assembly.name;fullName=[string]$assembly.fullName})
            continue
        }
        $parsedMvid=[Guid]::Empty
        if($assembly.location-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.location)-or
            -not[IO.Path]::IsPathRooted($assembly.location)-or$assembly.sha256-isnot[string]-or
            $assembly.sha256-cnotmatch'^[0-9a-f]{64}$'-or$assembly.mvid-isnot[string]-or
            -not[Guid]::TryParseExact($assembly.mvid,'D',[ref]$parsedMvid)-or$parsedMvid-eq[Guid]::Empty-or
            $parsedMvid.ToString('D')-cne$assembly.mvid){throw 'File-backed assembly requires an absolute location and exact observed SHA256/MVID.'}
        $location=[IO.Path]::GetFullPath([string]$assembly.location)
        $directory=[IO.Path]::GetDirectoryName($location).TrimEnd('\')
        if($assembly.name-ceq'KingmakerMountedCombat') {
            if(-not$location.Equals($kmcPath,[StringComparison]::OrdinalIgnoreCase)){throw 'Loaded KMC assembly is outside the exact staged candidate path.'}
        }
        elseif(-not$directory.Equals($managedRoot,[StringComparison]::OrdinalIgnoreCase)-and
            -not$directory.Equals($loaderRoot,[StringComparison]::OrdinalIgnoreCase)) {
            throw 'File-backed assembly is outside the exact installed Managed/UMM directories and staged KMC path.'
        }
    }
    $kmc=@($assemblies|Where-Object name -CEQ 'KingmakerMountedCombat')
    if($kmc.Count-ne1-or$kmc[0].dynamic-or$kmc[0].sha256-cne$Request.dllSha256-or$kmc[0].mvid-cne$Request.dllMvid){throw 'Loaded KMC assembly differs from candidate.'}
    foreach($relative in @('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll')) {
        $dependency=@($Dependencies.files|Where-Object relativePath -CEQ $relative)
        if($dependency.Count-ne1){throw 'Dependency receipt role missing.'}
        $expectedPath=[IO.Path]::GetFullPath((Join-Path $Dependencies.gameRoot $relative))
        $loaded=@($assemblies|Where-Object{-not$_.dynamic-and$_.location-and[IO.Path]::GetFullPath([string]$_.location)-ieq$expectedPath})
        if($loaded.Count-ne1-or$loaded[0].sha256-cne$dependency[0].sha256){throw 'Required pinned native loader assembly was not observed loaded.'}
    }
    if($PassThru) {
        return [pscustomobject][ordered]@{schemaVersion=1;fileBackedAssemblyCount=$assemblies.Count-$dynamicAssemblies.Count;
            dynamicAssemblyCount=$dynamicAssemblies.Count;dynamicAssemblies=@($dynamicAssemblies.ToArray());dynamicAssembliesQualified=$false;
            completeStockAssemblyAllowlistProven=$false;
            limitation='All non-dynamic assemblies have observed hashes/MVIDs and exact allowed directories; KMC/gameplay/UMM/Harmony12 are pinned. Other Managed/UMM binaries and runtime-generated assemblies are inventoried, not certified by a stock assembly allowlist.'}
    }
}
