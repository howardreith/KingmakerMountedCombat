[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceProfileProtection.ps1')
$root=Assert-KmcChildPath (Join-Path (Get-KmcRepositoryRoot) ('obj/profile-protection-tests/'+[Guid]::NewGuid().ToString('N'))) (Join-Path (Get-KmcRepositoryRoot) 'obj') 'profile test'
$profile=Join-Path $root 'profile'
$game=Join-Path $root 'game'
$state=Join-Path $root 'state'
$backups=Join-Path $root 'backups'
$saves=Join-Path $profile 'Saved Games'
$params=Join-Path $game 'Kingmaker_Data/Managed/UnityModManager/Params.xml'
foreach($dir in @($saves,(Join-Path $profile 'Areas'),(Split-Path $params),$state,$backups)){[void][IO.Directory]::CreateDirectory($dir)}
$cache=Join-Path $profile 'Areas/test.json'
[IO.File]::WriteAllText($cache,'original')
[IO.File]::WriteAllText($params,'<params/>')
[IO.File]::WriteAllText((Join-Path $saves 'foreign.zks'),'opaque save')
$lock=Open-KmcRuntimeLock $state 'profile-test'
try{
    $snapshot=New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saves -GameRoot $game -BackupRoot $backups
    [void](Assert-KmcPersistenceProfileUnchanged $snapshot)
    if(Test-Path (Join-Path $backups 'profile-profile-test/profile/Saved Games')){throw 'Profile helper copied human saves.'}
    $reordered=[pscustomobject]@{entries=@($snapshot.inventory.entries)}
    [Array]::Reverse($reordered.entries)
    if((Get-KmcPersistenceProfileDigest $reordered)-cne$snapshot.profileDigest){throw 'Profile identity depends on enumeration order.'}
    $passes=2
    foreach($path in @($cache,$params)){
        $original=[IO.File]::ReadAllBytes($path)
        try{
            [IO.File]::WriteAllText($path,'changed')
            $rejected=$false
            try{[void](Assert-KmcPersistenceProfileUnchanged $snapshot)}catch{$rejected=$true}
            if(!$rejected){throw 'Modified profile/settings accepted.'}
            $passes++
        }finally{[IO.File]::WriteAllBytes($path,$original)}
    }
    [void](Assert-KmcPersistenceProfileUnchanged $snapshot)
    $pref='[{"name":"KingdomDifficulty_h4200925179","kind":"Binary","value":"MQA="}]'
    $changed='[{"name":"KingdomDifficulty_h4200925179","kind":"Binary","value":"NAA="}]'
    if(@(Get-KmcPersistencePreferenceChanges $pref $changed).Count-ne1){throw 'Observed startup change not identified.'}
    $passes++
    $rejected=$false
    try{[void]@(Get-KmcPersistencePreferenceChanges ($pref.Replace('KingdomDifficulty_h4200925179','Unrelated')) ($changed.Replace('KingdomDifficulty_h4200925179','Unrelated')))}catch{$rejected=$true}
    if(!$rejected){throw 'Unrelated preference change accepted.'};$passes++
    $original='<Params><ModParamsList><Mod Id="Existing" Enabled="false"/></ModParamsList></Params>'
    $after='<Params><ModParamsList><Mod Id="Existing" Enabled="false"/><Mod Id="SkipIntro" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod></ModParamsList></Params>'
    Assert-KmcNativeUmmStartupDelta $original $after;$passes++
    $rejected=$false
    try{Assert-KmcNativeUmmStartupDelta $original ($after.Replace('Enabled="true"','Enabled="false"'))}catch{$rejected=$true}
    if(!$rejected){throw 'Unrelated UMM value accepted.'};$passes++
    $known='20260921-chunk5-P03-condition-preparing-save-A'
    $old=[pscustomobject]@{name='EternalKingdom_h3591390253';kind='Binary';value='RmFsc2UA'}
    $new=[pscustomobject]@{name=$old.name;kind='Binary';value='VHJ1ZQA='}
    if(-not(Test-KmcObservedPreparationResetPreference $known $old $new)){throw 'Exact attributed reset was not recognized.'};$passes++
    if(Test-KmcObservedPreparationResetPreference 'other-run' $old $new){throw 'Another run can use the one-run reset recovery.'};$passes++
    $new.value='RmFsc2UA'
    if(Test-KmcObservedPreparationResetPreference $known $old $new){throw 'Unobserved reset value accepted.'};$passes++
    $new.value='VHJ1ZQA=';$old.kind='String'
    if(Test-KmcObservedPreparationResetPreference $known $old $new){throw 'Unobserved reset type accepted.'};$passes++
    $old.kind='Binary';$old.name='Unrelated';$new.name='Unrelated'
    if(Test-KmcObservedPreparationResetPreference $known $old $new){throw 'Unrelated key accepted by reset recovery.'};$passes++
    $unicode=[pscustomobject]@{path=('cache-'+[char]0x00e9+[char]0xfffd);value='unchanged'}
    $unicodePath=Join-Path $root 'utf8-snapshot.json'
    [IO.File]::WriteAllText($unicodePath,($unicode|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    $read=Read-KmcPersistenceProfileSnapshot $unicodePath
    if($read.path-cne$unicode.path-or$read.value-cne$unicode.value){throw 'UTF-8 snapshot path did not round trip.'};$passes++
    $analytics=[pscustomobject]@{kind='file';path='Unity/2b02a6f4-4611-4ce0-b230-f9998567c3af/Analytics/ArchivedEvents/178998321600004.7fa040cf/c';
        length=1;sha256='6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b'}
    $ownedRun='20260921-chunk5-P06-future-A'
    if(-not(Test-KmcObservedValidationAnalyticsEntry $ownedRun $analytics)){throw 'Exact observed owned cache rejected.'};$passes++
    if(Test-KmcObservedValidationAnalyticsEntry 'another-run' $analytics){throw 'Another run gained cache recovery.'};$passes++
    $analytics.sha256='a'*64
    if(Test-KmcObservedValidationAnalyticsEntry $ownedRun $analytics){throw 'Changed cache bytes admitted.'};$passes++
    $analytics.sha256='6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b'
    $analytics.length=2
    if(Test-KmcObservedValidationAnalyticsEntry $ownedRun $analytics){throw 'Changed cache length admitted.'};$passes++
    $analytics.length=1;$analytics.path='../'+$analytics.path
    if(Test-KmcObservedValidationAnalyticsEntry $ownedRun $analytics){throw 'Cache traversal admitted.'};$passes++
    $analytics.path='Saved Games/human.zks'
    if(Test-KmcObservedValidationAnalyticsEntry $ownedRun $analytics){throw 'Human save admitted as owned cache.'};$passes++
    # The installed game's achievement cache churns on many launches. Admission
    # is a before/after transition, never a name alone: a new leaf must arrive
    # at the settled native size, an existing leaf may be rewritten but must
    # keep its exact length, and nothing may shrink, grow or disappear.
    $native=12288
    $ach=Join-Path $profile 'achievements.dat$'
    [IO.File]::WriteAllBytes($ach,(New-Object byte[] $native))
    $accepted=@(Assert-KmcPersistenceProfileUnchanged $snapshot)
    if($accepted.Count-ne1-or$accepted[0].change-cne'created'-or$accepted[0].path-cne'achievements.dat$'){
        throw 'Created native cache leaf was not reported as expected churn.'};$passes++
    $bytes=New-Object byte[] $native;$bytes[0]=7
    [IO.File]::WriteAllBytes($ach,$bytes)
    $accepted=@(Assert-KmcPersistenceProfileUnchanged $snapshot)
    if($accepted.Count-ne1-or$accepted[0].change-cne'created'-or$accepted[0].afterSha256-ceq$accepted[0].beforeSha256){
        throw 'Rewritten native cache leaf was not reported with its bytes.'};$passes++
    foreach($bad in @(0,4096,24576)){
        [IO.File]::WriteAllBytes($ach,(New-Object byte[] $bad))
        $rejected=$false
        try{[void](Assert-KmcPersistenceProfileUnchanged $snapshot)}catch{$rejected=$true}
        if(!$rejected){throw ('New cache leaf accepted at unnative size '+$bad+'.')};$passes++
    }
    [IO.File]::Delete($ach)
    # A populated pre-existing leaf may never be truncated, grown or removed,
    # and a pre-existing empty leaf grants no truncation permission.
    $populated=[pscustomobject]@{kind='file';path='achievements.dat7';length=$native;sha256=('a'*64)}
    $emptyLeaf=[pscustomobject]@{kind='file';path='achievements.datZ';length=0;sha256=('c'*64)}
    $before=[pscustomobject]@{entries=@($populated,$emptyLeaf)}
    foreach($case in @(
        @{name='truncated to empty';after=@([pscustomobject]@{kind='file';path='achievements.dat7';length=0;sha256=('b'*64)},$emptyLeaf)},
        @{name='grown';after=@([pscustomobject]@{kind='file';path='achievements.dat7';length=24576;sha256=('b'*64)},$emptyLeaf)},
        @{name='deleted';after=@($emptyLeaf)},
        @{name='empty leaf populated';after=@($populated,[pscustomobject]@{kind='file';path='achievements.datZ';length=$native;sha256=('d'*64)})})){
        $rejected=$false
        try{[void](Get-KmcPersistenceProfileCacheDelta $before ([pscustomobject]@{entries=@($case.after)}))}catch{$rejected=$true}
        if(!$rejected){throw ('Cache transition accepted: '+$case.name+'.')};$passes++
    }
    $rewritten=[pscustomobject]@{entries=@([pscustomobject]@{kind='file';path='achievements.dat7';length=$native;sha256=('b'*64)},$emptyLeaf)}
    $delta=@(Get-KmcPersistenceProfileCacheDelta $before $rewritten)
    if($delta.Count-ne1-or$delta[0].change-cne'rewritten'-or$delta[0].beforeSha256-cne('a'*64)-or$delta[0].afterSha256-cne('b'*64)){
        throw 'Legitimate rewrite churn was not reported with before and after hashes.'};$passes++
    if(@(Get-KmcPersistenceProfileCacheDelta $before $before).Count-ne0){throw 'Unchanged cache reported churn.'};$passes++
    # A nested or unrelated leaf is outside the exception at either size, and a
    # dotted lookalike is not the demonstrated native naming rule.
    foreach($leaf in @('Areas/achievements.dat$','settings.dat','achievements.dat.bak')){
        foreach($size in @($native,0)){
            $path=Join-Path $profile $leaf
            [void][IO.Directory]::CreateDirectory((Split-Path $path))
            [IO.File]::WriteAllBytes($path,(New-Object byte[] $size))
            $rejected=$false
            try{[void](Assert-KmcPersistenceProfileUnchanged $snapshot)}catch{$rejected=$true}
            [IO.File]::Delete($path)
            if(!$rejected){throw ('Leaf outside the native cache exception accepted: '+$leaf+' at '+$size+'.')};$passes++
        }
    }
    if(@(Assert-KmcPersistenceProfileUnchanged $snapshot).Count-ne0){throw 'Restored profile reported unexpected churn.'};$passes++

    # Unity appends an analytics batch on exit. Only ADDITIONS under that exact
    # path shape are admitted, and they must still be reported with their exact
    # hashes; nothing else about that tree may move.
    $guid='2b02a6f4-4611-4ce0-b230-f9998567c3af'
    $batch=('Unity/'+$guid+'/Analytics/ArchivedEvents/179018439100004.538885c6')
    $base=[pscustomobject]@{entries=@([pscustomobject]@{kind='file';path='settings.json';length=4;sha256=('a'*64)})}
    $withBatch=[pscustomobject]@{entries=@($base.entries+@(
        [pscustomobject]@{kind='directory';path=$batch;length=0;sha256=$null},
        [pscustomobject]@{kind='file';path=($batch+'/e');length=1367;sha256=('c'*64)}))}
    $created=@(Get-KmcPersistenceProfileAnalyticsDelta $base $withBatch)
    if($created.Count-ne2-or@($created|Where-Object{$_.change-cne'created'}).Count-ne0-or
        @($created|Where-Object{$_.kind-ceq'file'-and$_.afterSha256-cne('c'*64)}).Count-ne0){
        throw 'An appended analytics batch was not reported as created with its exact hashes.'};$passes++
    if((Get-KmcPersistenceProfileIdentityDigest $withBatch $base)-cne(Get-KmcPersistenceProfileIdentityDigest $base $withBatch)){
        throw 'An appended analytics batch changed the profile identity digest.'};$passes++
    # The engine also deletes a batch once it has dispatched it, which is the
    # same comparison in the other direction.
    $dispatched=@(Get-KmcPersistenceProfileAnalyticsDelta $withBatch $base)
    if($dispatched.Count-ne2-or@($dispatched|Where-Object{$_.change-cne'dispatched'}).Count-ne0){
        throw 'A dispatched analytics batch was not reported as dispatched.'};$passes++
    if((Get-KmcPersistenceProfileIdentityDigest $base $withBatch)-cne(Get-KmcPersistenceProfileIdentityDigest $withBatch $base)){
        throw 'A dispatched analytics batch changed the profile identity digest.'};$passes++
    # An entry present on BOTH sides is neither created nor dispatched, so
    # rewriting it in place must still move the digest, and so must anything
    # outside the exact batch shape.
    $changedBatch=[pscustomobject]@{entries=@($base.entries+@(
        [pscustomobject]@{kind='directory';path=$batch;length=0;sha256=$null},
        [pscustomobject]@{kind='file';path=($batch+'/e');length=99;sha256=('d'*64)}))}
    if((Get-KmcPersistenceProfileIdentityDigest $changedBatch $withBatch)-ceq(Get-KmcPersistenceProfileIdentityDigest $withBatch $changedBatch)){
        throw 'A rewritten preexisting analytics entry was admitted.'};$passes++
    if(@(Get-KmcPersistenceProfileAnalyticsDelta $withBatch $changedBatch).Count-ne0){
        throw 'A rewritten preexisting analytics entry was reported as created or dispatched.'};$passes++
    foreach($outside in @(('Unity/'+$guid+'/Analytics/ArchivedEvents/notabatch/e'),
        ('Unity/'+$guid+'/Analytics/Values/179018439100004.538885c6/e'),
        ('Unity/'+$guid+'/Analytics/ArchivedEvents/179018439100004.538885c6/toolong'),
        ('Unity/'+$guid+'/Analytics/ArchivedEvents/179018439100004.538885c6/sub/e'))){
        $foreign=[pscustomobject]@{entries=@($base.entries+@([pscustomobject]@{kind='file';path=$outside;length=1;sha256=('e'*64)}))}
        if((Get-KmcPersistenceProfileIdentityDigest $foreign $base)-ceq(Get-KmcPersistenceProfileIdentityDigest $base $foreign)){
            throw ('A path outside the analytics batch shape was admitted: '+$outside+'.')};$passes++
    }
    Write-Host "PROFILE PROTECTION PASS=$passes FAIL=0; actual human profile and registry were read only."
}finally{Close-KmcRuntimeLock $lock}
