# F5 (PET-01, PET-02): the companion-names-tack-manual producer and its separate-process cold consumer.
# Names are the native per-unit CustomName; the tack is one adopted renderer on the Riding Dog's view and none elsewhere.
$script:KmcF5SharedName = [string]::new([char[]]@([char]0x00C9,'b',[char]0x00E8,'n','e',' ','l',"'",'A','n','c','i','e','n'))
$script:KmcF5DogDistinctName = 'Biscuit'
$script:KmcF5ProducerShots = @('f5-dog-unmounted-side','f5-dog-unmounted-other','f5-dog-bite-unmounted','f5-sheet-mammoth','f5-dialog-open','f5-dialog-typed',
    'f5-sheet-mammoth-renamed','f5-invalid-message','f5-reset-confirm','f5-sheet-dog-distinct','f5-sheet-dog-duplicate',
    'f5-dog-mounted-side','f5-dog-mounted-other','f5-dog-mounted-cast','f5-dog-mounted-bite','f5-dog-mounted-turn')
$script:KmcF5ConsumerShots = @('f5-cold-sheet-dog','f5-cold-sheet-mammoth')

function Assert-KmcFinalize5Shot($Shots,[string]$Name,[string]$Root){
    $found=@($Shots|Where-Object{[string]$_.name-ceq$Name})
    if($found.Count-ne1){throw ('F5 view missing or duplicated: '+$Name)}
    $shot=$found[0]
    if($null-ne$shot.PSObject.Properties['error']-or[string]$shot.file-cnotmatch'^movement-visuals/(sprint5|finalize5)-f5-[a-z0-9-]+\.png$'-or[long]$shot.bytes-le0){throw ('F5 view failed: '+$Name)}
    if($Root){
        $path=Join-Path $Root ([string]$shot.file).Replace('/','\')
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw ('F5 view file is absent: '+$Name)}
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$shot.sha256){throw ('F5 view hash differs: '+$Name)}
    }
}

function Assert-KmcFinalize5Tack($Tack,[string]$Label){
    if($null-eq$Tack-or$Tack.dogBindings-ne1-or$Tack.mammothBindings-ne0-or$Tack.adopted-ne$true-or$Tack.rendererEnabled-ne$true-or$Tack.sameBones-ne$true-or
       [int]$Tack.vertices-le0-or[int]$Tack.triangles-le0-or([int]$Tack.misses*4)-gt[int]$Tack.casts-or[int]$Tack.meshBuilds-lt1-or[int]$Tack.attachments-lt1){
        throw ('The Riding Dog tack is missing, duplicated, unadopted, on another animal or rebuilt: '+$Label)
    }
    if([double]$Tack.seatTop-le[double]$Tack.backAtSeat-or[double]$Tack.cantleTop-le[double]$Tack.seatTop-or[double]$Tack.pommelTop-le[double]$Tack.seatTop){throw ('The tack does not sit above the back with its rolls: '+$Label)}
    # Upper fit envelope (metres, mount-local), from the design and its geometry regressions: the seat rides 1-5 cm over the back
    # (design 2.3 cm), each roll and every pad or seat point stays under 8 cm above the back. r03's pommel stood 31 cm over it.
    if($null-eq$Tack.PSObject.Properties['maxRise']){throw ('The tack fit recorded no rise above the back: '+$Label)}
    $lift=[double]$Tack.seatTop-[double]$Tack.backAtSeat
    if($lift-lt0.010-or$lift-gt0.050){throw ('The seat is buried in or floats above the back ('+$lift.ToString('F3')+' m): '+$Label)}
    if([double]$Tack.cantleTop-[double]$Tack.backAtSeat-ge0.08-or[double]$Tack.pommelTop-[double]$Tack.backAtSeat-ge0.08){throw ('A roll stands too tall above the back: '+$Label)}
    if([double]$Tack.maxRise-ge0.08-or[double]$Tack.maxRise-le0){throw ('A pad or seat point leaves the back ('+([double]$Tack.maxRise).ToString('F3')+' m): '+$Label)}
}

# The same dog keeps its statistics across tack, mount, motion and reload.
function Assert-KmcFinalize5DogStats($Tacks,[string]$Label){
    $first=$Tacks[0].dogStats
    foreach($t in $Tacks){$s=$t.dogStats
        if($null-eq$s-or[string]$s.id-cne[string]$first.id-or[string]$s.size-cne[string]$first.size-or[int]$s.armorClass-ne[int]$first.armorClass-or[int]$s.speed-ne[int]$first.speed){throw ('The Riding Dog statistics changed: '+$Label)}}
}

# The ordinary in-world label: each companion's native overtip draws its CharacterName (decorative first letter in rich text).
function Assert-KmcFinalize5Overtips($Overtips,[string]$Dog,[string]$Mammoth,[string]$Label){
    if($null-eq$Overtips){throw ('No overtip labels were recorded: '+$Label)}
    foreach($pair in @(@($Overtips.dog,$Dog),@($Overtips.mammoth,$Mammoth))){$row=$pair[0]
        $plain=if($null-eq$row.text){$null}else{[regex]::Replace([string]$row.text,'<[^>]*>','')}
        if([string]$row.controllerUnit-cne[string]$row.id-or$plain-cne$pair[1]-or[string]$row.characterName-cne$pair[1]){throw ('A companion overtip shows another name or unit: '+$Label)}}
    if([string]$Overtips.dog.id-ceq[string]$Overtips.mammoth.id){throw ('The overtips name one unit twice: '+$Label)}
}

function Assert-KmcFinalize5Identity($Value,$Expected,[string]$Name,[string]$Label){
    if($null-eq$Value-or[string]$Value.id-cne[string]$Expected.id-or[string]$Value.master-cne[string]$Expected.master-or[string]$Value.blueprint-cne[string]$Expected.blueprint-or
       $Value.masterOwnsPet-ne$true-or$Value.eligible-ne$true-or[string]$Value.customName-cne$Name-or[string]$Value.characterName-cne$(if($Name){$Name}else{[string]$Expected.defaultName})){
        throw ('The companion name, identity or ownership differs: '+$Label)
    }
}

function Assert-KmcFinalize5Labels($Labels,[string]$Dog,[string]$Mammoth,[string]$Label){
    if($null-eq$Labels-or[string]$Labels.dog.characterName-cne$Dog-or[string]$Labels.dog.customName-cne$Dog-or[string]$Labels.mammoth.characterName-cne$Mammoth-or
       [string]$Labels.mammoth.customName-cne$Mammoth-or[string]$Labels.dog.id-ceq[string]$Labels.mammoth.id){throw ('The companion labels differ: '+$Label)}
}

# Cross-run: the cold consumer against its own accepted producer, by actual recorded values rather than shared literals. Same
# binary and commit, the producer's exact Manual as the declared source, and the same companions, owners, names, labels and dog.
function Assert-KmcFinalize5ColdAgainstSource($ProducerRequest,$ProducerEvidence,$ConsumerRequest,$ConsumerEvidence){
    if([string]$ProducerEvidence.status-cne'PASS'-or[string]$ConsumerEvidence.status-cne'PASS'){throw 'Both F5 runs must have passed.'}
    if([string]$ProducerRequest.scenario-cne'chunk7-persistence-save'-or[string]$ConsumerRequest.scenario-cne'chunk7-persistence-load'-or
       [string]$ProducerRequest.persistenceCase-cne'companion-names-tack-manual'-or[string]$ConsumerRequest.persistenceCase-cne'companion-names-tack-manual'){throw 'The runs are not the F5 producer and consumer.'}
    foreach($field in 'commit','branch','dllSha256','dllMvid'){if([string]$ProducerRequest.$field-cne[string]$ConsumerRequest.$field){throw ('The consumer ran another '+$field+' than its producer.')}}
    $write=$ProducerEvidence.observations.nativeWrite;$load=$ConsumerRequest.persistenceLoad
    if($null-eq$write-or[string]$write.nativeType-cne'Manual'-or[string]$load.sha256-cne[string]$write.sha256-or[long]$load.length-ne[long]$write.length-or
       [string]$load.fileName-cne[IO.Path]::GetFileName([string]$write.path)){throw 'The consumer did not load the producer''s exact Manual.'}
    $n=$ProducerEvidence.observations.companionNames;$c=$ConsumerEvidence.observations.companionNamesCold
    foreach($role in 'dog','mammoth'){
        $saved=$n.atSave.$role;$cold=$c.labels.$role
        if([string]$cold.id-cne[string]$saved.id-or[string]$cold.customName-cne[string]$saved.customName-or[string]$cold.characterName-cne[string]$saved.characterName){throw ('The cold '+$role+' is not the saved unit with its saved name.')}
        if([string]$c.$role.id-cne[string]$n.$role.id-or[string]$c.$role.master-cne[string]$n.$role.master-or[string]$c.$role.blueprint-cne[string]$n.$role.blueprint){throw ('The cold '+$role+' changed identity, owner or blueprint.')}
        if([string]$c.overtipsCold.$role.text-cne[string]$n.overtipsAtSave.$role.text){throw ('The cold '+$role+' overtip differs from the saved one.')}
    }
    $before=$n.tackAfterMotion.dogStats;$after=$c.tackCold.dogStats
    if([string]$after.id-cne[string]$before.id-or[string]$after.size-cne[string]$before.size-or[int]$after.armorClass-ne[int]$before.armorClass-or[int]$after.speed-ne[int]$before.speed){throw 'The Riding Dog statistics changed across the reload.'}
}

function Assert-KmcFinalize5CompanionNames($Request,$Observations,[bool]$Cold){
    $root=if($null-ne$Request.PSObject.Properties['evidenceRoot']){[string]$Request.evidenceRoot}else{$null}
    $shared=$script:KmcF5SharedName
    if(-not$Cold){
        $n=$Observations.companionNames
        if($null-eq$n){throw 'The F5 producer recorded no naming evidence.'}
        $dog=$n.dog;$mammoth=$n.mammoth
        if($dog.blueprint-cne'b7a14f25d3c94e0d8e6f1a2c5d90e431'-or$dog.eligible-ne$true-or$mammoth.eligible-ne$true-or[string]$dog.customName-cne''-or[string]$mammoth.customName-cne''-or
           [string]$dog.id-ceq[string]$mammoth.id-or[string]$dog.master-ceq[string]$mammoth.master){throw 'The F5 producer did not start from two default-named owned companions.'}
        Assert-KmcFinalize5Tack $n.tackUnmounted 'unmounted'
        Assert-KmcFinalize5Tack $n.tackMounted 'mounted'
        Assert-KmcFinalize5Tack $n.tackAfterMotion 'after motion'
        if($n.sheetMammoth.renameActive-ne$true-or$n.sheetMammoth.resetActive-ne$false-or[string]$n.sheetMammoth.shown-cne[string]$mammoth.id-or[string]$n.sheetMammoth.renameLabel-cne'Rename'){throw 'The sheet did not offer Rename for the default-named Mammoth.'}
        if($n.sheetMammothRenamed.resetActive-ne$true-or[string]$n.sheetMammothRenamed.resetLabel-cne'Default Name'){throw 'The sheet did not offer Default Name after the rename.'}
        $open=$n.dialogOpen
        if($open.shown-ne$true-or[string]$open.prefilled-cne[string]$mammoth.characterName-or$open.hotkeysSuspended-ne$true-or$open.inputFocused-ne$true-or$open.actorCommandsIdle-ne$true){throw 'The naming dialog did not open focused on the current name with game hotkeys suspended.'}
        $typing=@($n.typing)
        if($typing.Count-ne6){throw 'The naming dialog was not typed into exactly six times.'}
        foreach($entry in $typing){if([string]$entry.field-cne[string]$entry.intended-or$entry.hotkeysSuspended-ne$true-or$entry.actorsIdleAfter-ne$true){throw ('Typing did not reach the field alone: '+[string]$entry.intended)}}
        Assert-KmcFinalize5Identity $n.mammothRenamed $mammoth $shared 'renamed'
        Assert-KmcFinalize5Identity $n.cancelled $mammoth $shared 'cancel'
        if($n.cancelReleasedKeyboard-ne$true){throw 'A cancelled rename left the name field holding the keyboard.'}
        if($n.markupMessageShown-ne$true-or[string]$n.markupReopenedWith-cne'<b>Rex</b>'){throw 'The refused markup name did not explain itself and reopen with the typed text.'}
        Assert-KmcFinalize5Identity $n.markupRefused $mammoth $shared 'markup refused'
        Assert-KmcFinalize5Identity $n.mammothReset $mammoth '' 'reset'
        Assert-KmcFinalize5Identity $n.mammothRenamedAgain $mammoth $shared 'renamed again'
        if($n.nonPetHidden-ne$true){throw 'Rename was offered for a character who is not a companion.'}
        Assert-KmcFinalize5Identity $n.dogDistinct $dog $script:KmcF5DogDistinctName 'dog distinct'
        Assert-KmcFinalize5Labels $n.distinctLabels $script:KmcF5DogDistinctName $shared 'distinct'
        Assert-KmcFinalize5Identity $n.dogDuplicate $dog $shared 'dog duplicate'
        Assert-KmcFinalize5Labels $n.duplicateLabels $shared $shared 'duplicate'
        Assert-KmcFinalize5Labels $n.atSave $shared $shared 'at save'
        Assert-KmcFinalize5Overtips $n.overtipsAtSave $shared $shared 'at save'
        Assert-KmcFinalize5DogStats @($n.tackUnmounted,$n.tackMounted,$n.tackAfterMotion) 'producer'
        if([string]$n.tackUnmounted.dogStats.id-cne[string]$dog.id){throw 'The tack statistics are not the named Riding Dog.'}
        if($n.sheetClosed-ne$true){throw 'The character sheet did not close.'}
        foreach($shot in $script:KmcF5ProducerShots){Assert-KmcFinalize5Shot $n.shots $shot $root}
        return
    }
    $c=$Observations.companionNamesCold
    if($null-eq$c){throw 'The F5 consumer recorded no naming evidence.'}
    $d=$Observations.ridingDogCold
    if([string]$c.dog.master-cne[string]$d.owner-or[string]$c.dog.blueprint-cne'b7a14f25d3c94e0d8e6f1a2c5d90e431'-or$c.dog.masterOwnsPet-ne$true-or$c.mammoth.masterOwnsPet-ne$true-or
       $c.dog.eligible-ne$true-or$c.mammoth.eligible-ne$true){throw 'The cold companions lost their owner, blueprint or eligibility.'}
    $archive=$Observations.loadedArchive
    $mounts=@($archive.Pairs|ForEach-Object{([Guid]$_.Mount.Id).ToString('D')})
    foreach($id in @([string]$c.dog.id,[string]$c.mammoth.id)){if($id-cnotin$mounts){throw 'A cold companion is not the restored archive mount of its pair.'}}
    Assert-KmcFinalize5Labels $c.labels $shared $shared 'cold'
    # Persistence, not a test-side rename: the cold names were observed with no naming-service write in this process.
    if($null-eq$c.labels.PSObject.Properties['writes']-or[int]$c.labels.writes-ne0-or$null-ne$c.labels.lastOutcome){throw 'The cold names were observed after a naming-service write.'}
    Assert-KmcFinalize5Identity $c.dog $c.dog $shared 'cold dog'
    Assert-KmcFinalize5Identity $c.mammoth $c.mammoth $shared 'cold mammoth'
    if([string]$c.labels.dog.id-cne[string]$c.dog.id-or[string]$c.labels.mammoth.id-cne[string]$c.mammoth.id){throw 'The cold labels belong to other units.'}
    Assert-KmcFinalize5Overtips $c.overtipsCold $shared $shared 'cold'
    if([int]$c.overtipsCold.writes-ne0){throw 'The cold overtips were observed after a naming-service write.'}
    Assert-KmcFinalize5Tack $c.tackCold 'cold'
    Assert-KmcFinalize5Tack $c.tackRemounted 'cold remounted'
    Assert-KmcFinalize5DogStats @($c.tackCold,$c.tackRemounted) 'cold'
    if([string]$c.tackCold.dogStats.id-cne[string]$c.dog.id){throw 'The cold tack statistics are not the named Riding Dog.'}
    foreach($sheet in @($c.sheetDogCold,$c.sheetMammothCold)){if($sheet.renameActive-ne$true-or$sheet.resetActive-ne$true){throw 'The cold sheet did not offer Rename and Default Name for a renamed companion.'}}
    if([string]$c.sheetDogCold.shown-cne[string]$c.dog.id-or[string]$c.sheetMammothCold.shown-cne[string]$c.mammoth.id){throw 'The cold sheet showed another unit.'}
    if($c.sheetClosed-ne$true){throw 'The cold character sheet did not close.'}
    foreach($shot in $script:KmcF5ConsumerShots){Assert-KmcFinalize5Shot $c.shots $shot $root}
}
