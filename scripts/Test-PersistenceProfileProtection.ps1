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
    Write-Host "PROFILE PROTECTION PASS=$passes FAIL=0; actual human profile and registry were read only."
}finally{Close-KmcRuntimeLock $lock}
