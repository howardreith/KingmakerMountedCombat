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
    Write-Host "PROFILE PROTECTION PASS=$passes FAIL=0; actual human profile and registry were read only."
}finally{Close-KmcRuntimeLock $lock}
