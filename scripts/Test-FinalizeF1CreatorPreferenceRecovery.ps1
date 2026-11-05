$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime/Recover-FinalizeF1CreatorPreferences.ps1') -ContractOnly
$lab=Get-KmcLabRoot
$snapshot=Read-KmcPersistenceProfileSnapshot (Join-Path $lab 'runtime-backups/profile-20261105-finalize-f1c-owner-creator-01/snapshot.json')
$observed=[IO.File]::ReadAllText((Join-Path $lab 'runtime-evidence/20261105-finalize-f1c-owner-creator-01/preferences-after-exit.json'))
$count=0
function Convert-PreferenceArray($Items){return '['+(($Items|ForEach-Object{$_|ConvertTo-Json -Depth 8 -Compress}) -join ',')+']'}
function Expect-Refusal([scriptblock]$Action){
 $refused=$false;try{&$Action|Out-Null}catch{$refused=$true}
 if(-not$refused){throw 'Expected refusal.'};$script:count++
}
if(@(Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed $observed).Count-ne14){throw 'Observed delta count differs.'};$count++
if(@(Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed $snapshot.playerPrefsJson).Count-ne0){throw 'Original state must be idempotent.'};$count++
$current=ConvertFrom-Json $observed
$original=ConvertFrom-Json $snapshot.playerPrefsJson
$restore=$original|Where-Object name -CEQ 'AutoLevelup_h3494002937'
($current|Where-Object name -CEQ $restore.name).value=$restore.value
$partial=Convert-PreferenceArray $current
if(@(Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed $partial).Count-ne13){throw 'Partial restoration must resume narrowly.'};$count++
$current=ConvertFrom-Json $observed;$current[0].value='AA=='
Expect-Refusal {Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed (Convert-PreferenceArray $current)}
$current=ConvertFrom-Json $observed
$deleted=@($current|Where-Object name -CNE 'AutoLevelup_h3494002937')
Expect-Refusal {Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed (Convert-PreferenceArray $deleted)}
$added=@($current)+@([pscustomobject]@{name='foreign';kind='Binary';value='AA=='})
Expect-Refusal {Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed (Convert-PreferenceArray $added)}
$duplicate=@($current)+@($current[0])
Expect-Refusal {Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed (Convert-PreferenceArray $duplicate)}
Expect-Refusal {Get-F1CreatorPreferenceRestoration ($snapshot.playerPrefsJson+' ') $observed $observed}
Write-Host ('TOTAL PASS='+$count+' FAIL=0; exact retained run inputs only; no native execution or mutation.')
