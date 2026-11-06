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
    Assert-KmcFinalize5Tack $c.tackCold 'cold'
    Assert-KmcFinalize5Tack $c.tackRemounted 'cold remounted'
    foreach($sheet in @($c.sheetDogCold,$c.sheetMammothCold)){if($sheet.renameActive-ne$true-or$sheet.resetActive-ne$true){throw 'The cold sheet did not offer Rename and Default Name for a renamed companion.'}}
    if([string]$c.sheetDogCold.shown-cne[string]$c.dog.id-or[string]$c.sheetMammothCold.shown-cne[string]$c.mammoth.id){throw 'The cold sheet showed another unit.'}
    if($c.sheetClosed-ne$true){throw 'The cold character sheet did not close.'}
    foreach($shot in $script:KmcF5ConsumerShots){Assert-KmcFinalize5Shot $c.shots $shot $root}
}
