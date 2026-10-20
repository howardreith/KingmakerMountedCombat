Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$runtime=Join-Path $PSScriptRoot '../runtime'
. (Join-Path $runtime 'RuntimeHarness.Common.ps1')
. (Join-Path $runtime 'PersistenceProfileProtection.ps1')
. (Join-Path $runtime 'PersistenceProfileAreasIsolation.ps1')

$script:passed=0
$script:root=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-areas-isolation-test-'+[guid]::NewGuid().ToString('N'))
$script:denyProcess=$false
function Get-KmcLabRoot { return $script:root }
function Assert-KmcRuntimeLockOwner($Lock) {
    if($null-eq$Lock-or$Lock.RunId-cne'test-areas'-or$Lock.Token-cne('a'*64)){throw 'Test lock differs.'}
    return $Lock
}
function Assert-KmcNoGameProcesses {if($script:denyProcess){throw 'Test process remains active.'}}
function Get-KmcPersistencePlayerPrefs {return '[]'}
function Check([bool]$value,[string]$name){if(-not$value){throw ('FAIL '+$name)};$script:passed++;Write-Host ('PASS '+$name)}
function New-Fixture([string]$name,[switch]$PreexistingCache){
    $base=Join-Path $script:root $name
    $profile=Join-Path $base 'profile';$save=Join-Path $profile 'Saved Games';$areas=Join-Path $profile 'Areas'
    $game=Join-Path $base 'game';$params=Join-Path $game 'Kingmaker_Data/Managed/UnityModManager/Params.xml'
    $backup=Join-Path $base 'backups'
    foreach($directory in @($save,$areas,(Split-Path $params),$backup)){[void][IO.Directory]::CreateDirectory($directory)}
    [IO.File]::WriteAllText((Join-Path $areas 'original.bin'),'original')
    if($PreexistingCache){
        [IO.File]::WriteAllBytes((Join-Path $profile ('achievements.dat'+[char]0xfffd+'@')),[byte[]]@())
    }
    [IO.File]::WriteAllText($params,'<Params />')
    Write-KmcJsonAtomic (Join-Path $script:root 'environment-intake.json') ([ordered]@{requestedLayout=[ordered]@{kingmakerSaveRoot=$save;kingmakerInstallDir=$game}})
    $lock=[pscustomobject]@{RunId='test-areas';Token=('a'*64)}
    $snapshot=New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $save -GameRoot $game -BackupRoot $backup
    return [pscustomobject]@{profile=$profile;areas=$areas;backup=$backup;lock=$lock;snapshot=$snapshot
        state=(Join-Path $backup 'profile-test-areas/Areas-isolation.json')
        original=(Join-Path $backup 'profile-test-areas/Areas-original')
        generated=(Join-Path $backup 'profile-test-areas/Areas-generated')}
}

foreach($row in @(
    @('export-mounted-contracts','',$true,$true),@('export-mounted-contracts','',$false,$false),
    @('persistence-p07-load','removal-no-dll',$false,$true),@('persistence-p07-save','area-cross-entry',$false,$true),
    @('persistence-p07-save','area-reload',$false,$true),
    @('persistence-p07-save','timeout',$false,$false),
    @('chunk7-owned-two-pair-campaign','multi-area-autosave',$false,$true),
    @('chunk7-owned-two-pair-campaign','multi-exploration-three-area-transfers-manual',$false,$true),
    @('chunk7-owned-two-pair-campaign','owner-seed-manticore-fixture-manual',$false,$true),
    @('chunk7-owned-two-pair-campaign','manticore-fixture-cold-manual',$false,$true),
    @('chunk7-persistence-load','manticore-fixture-cold-manual',$false,$false),
    @('chunk7-persistence-load','owner-seed-manticore-fixture-manual',$false,$false),
    @('chunk7-owned-two-pair-campaign','multi-rt-debt-manual',$false,$false))){
    Check ((Test-KmcPersistenceAreasIsolationSelection -Scenario $row[0] -PersistenceCase $row[1] -ObserveWorkingDeserialization $row[2])-eq$row[3]) ('closed selection '+$row[0]+'/'+$row[1]+'/'+$row[2])
}

$f=New-Fixture 'normal'
[void](Enter-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
Check ((Test-Path -LiteralPath $f.original -PathType Container)-and(Test-Path -LiteralPath $f.areas -PathType Container)) 'original held and isolated copy staged'
[IO.File]::WriteAllText((Join-Path $f.areas 'native.bin'),'new native Areas bytes')
$receipt=Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup
Check ($receipt.restored-and(Test-Path -LiteralPath $f.generated -PathType Container)-and
    (Test-Path -LiteralPath (Join-Path $f.generated 'native.bin'))-and
    -not(Test-Path -LiteralPath (Join-Path $f.areas 'native.bin'))-and
    [IO.File]::ReadAllText((Join-Path $f.areas 'original.bin'))-ceq'original') 'generated Areas quarantined and exact original restored'
$again=Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup
Check ($again.restored-and$again.generatedDigest-ceq$receipt.generatedDigest) 'settlement is idempotent'
[void](Assert-KmcPersistenceProfileUnchanged $f.snapshot)

$f=New-Fixture 'cache' -PreexistingCache
[void](Enter-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
[void](Assert-KmcPersistenceProfileOutsideAreasUnchanged $f.snapshot)
Check $true 'preexisting empty achievement cache remains in outside-Areas intake'
$newCache=Join-Path $f.profile 'achievements.dat_new'
[IO.File]::WriteAllBytes($newCache,[byte[]]@())
try{[void](Assert-KmcPersistenceProfileOutsideAreasUnchanged $f.snapshot);throw 'Expected unsettled cache refusal.'}
catch{Check ($_.Exception.Message -like 'A new profile leaf used the achievement cache name without its settled native size:*') 'new empty achievement cache still refuses Areas settlement'}
[IO.File]::WriteAllBytes($newCache,(New-Object byte[] 12288))
[void](Assert-KmcPersistenceProfileOutsideAreasUnchanged $f.snapshot)
Check $true 'new settled achievement cache admitted without changing Areas custody'
[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
Check ((Test-Path -LiteralPath (Join-Path $f.areas 'original.bin'))-and
    (Test-Path -LiteralPath $newCache)) 'cache fixture restores original Areas and retains admitted cache'

$f=New-Fixture 'partial'
$savedCopy=(Get-Command Copy-KmcDirectoryTreeExact).ScriptBlock
function Copy-KmcDirectoryTreeExact {throw 'Injected clone failure after original move.'}
try{[void](Enter-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup);throw 'Expected clone failure.'}
catch{Check ($_.Exception.Message-ceq'Injected clone failure after original move.') 'partial stage failure surfaced'}
finally{Set-Item function:Copy-KmcDirectoryTreeExact $savedCopy}
Check ((Test-Path -LiteralPath $f.original -PathType Container)-and-not(Test-Path -LiteralPath $f.areas)) 'partial stage retains original custody'
[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
Check ((Test-Path -LiteralPath (Join-Path $f.areas 'original.bin'))-and-not(Test-Path -LiteralPath $f.original)) 'partial stage restored in finally'

$f=New-Fixture 'partial-settlement'
[void](Enter-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
[IO.File]::WriteAllText((Join-Path $f.areas 'native.bin'),'native')
# Simulate interruption after the generated tree moves but before state advances.
[IO.Directory]::Move($f.areas,$f.generated)
[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
Check ((Test-Path -LiteralPath (Join-Path $f.areas 'original.bin'))-and
    (Test-Path -LiteralPath (Join-Path $f.generated 'native.bin'))-and
    -not(Test-Path -LiteralPath $f.original)) 'partial settlement resumes from durable custody'
try{[IO.File]::WriteAllText((Join-Path $f.generated 'native.bin'),'changed quarantine');
    [void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup);throw 'Expected quarantine drift refusal.'}
catch{Check ($_.Exception.Message-ceq'Quarantined Areas changed; restoration refused.') 'idempotent settlement refuses quarantine drift'}

$f=New-Fixture 'refusal'
[void](Enter-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
$script:denyProcess=$true
try{[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup);throw 'Expected process refusal.'}
catch{Check ($_.Exception.Message-ceq'Test process remains active.') 'active process refuses restoration'}
$script:denyProcess=$false
Check ((Test-Path -LiteralPath $f.original)-and(Test-Path -LiteralPath $f.areas)) 'active-process refusal retains both trees'
[IO.File]::WriteAllText((Join-Path $f.profile 'unexpected.txt'),'outside Areas')
try{[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup);throw 'Expected concurrent delta refusal.'}
catch{Check ($_.Exception.Message -like 'Profile outside the isolated Areas tree changed*') 'unexpected profile delta refuses restoration'}
Check ((Test-Path -LiteralPath $f.original)-and(Test-Path -LiteralPath $f.areas)) 'concurrent delta retains custody'
Remove-Item -LiteralPath (Join-Path $f.profile 'unexpected.txt')
[IO.File]::WriteAllText((Join-Path $f.original 'original.bin'),'changed held original')
try{[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup);throw 'Expected original drift refusal.'}
catch{Check ($_.Exception.Message-ceq'Held original Areas changed; restoration refused.') 'original drift refuses overwrite'}
[IO.File]::WriteAllText((Join-Path $f.original 'original.bin'),'original')
[void](Restore-KmcPersistenceAreasIsolation -Lock $f.lock -Snapshot $f.snapshot -BackupRoot $f.backup)
Check ((Test-Path -LiteralPath (Join-Path $f.areas 'original.bin'))-and-not(Test-Path -LiteralPath $f.original)) 'refusal leaves resumable safe settlement'

Write-Host ('CHUNK7 PROFILE AREAS ISOLATION PASS='+$script:passed+' FAIL=0')
