param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHotbarEvidence.ps1')
$command=[pscustomobject]@{identity=[pscustomobject]@{casterId='rider'}}
$input=[pscustomobject]@{registration=[pscustomobject]@{managerObject=701;ownerObject=702;route='group'}}
$before=@([pscustomobject]@{index=0;groupObject=702;type='ActivatableAbility';toggle=$false},[pscustomobject]@{index=1;groupObject=703;type='Spell';toggle=$true})
$opened=@([pscustomobject]@{index=0;groupObject=702;type='ActivatableAbility';toggle=$true},[pscustomobject]@{index=1;groupObject=703;type='Spell';toggle=$false})
$ui=[pscustomobject]@{contract='native-action-bar-group-input-and-exact-restoration';restored=$true;casterId='rider';managerObject=701;abilityGroupObject=702;before=$before;opened=$opened;after=$before}
$script:checks=0
function Copy-HotbarUi($v){$v|ConvertTo-Json -Depth 40|ConvertFrom-Json}
function Reject-HotbarUi([scriptblock]$mutation){
    $copy=Copy-HotbarUi $ui;& $mutation $copy
    $rejected=$false;try{Assert-KmcHotbarUi $copy $input $command}catch{$rejected=$true}
    if(-not $rejected){throw 'Invalid hotbar UI restoration accepted'}
    $script:checks++
}
Assert-KmcHotbarUi $ui $input $command;$script:checks++
Reject-HotbarUi {param($v) $v.restored=$false}
Reject-HotbarUi {param($v) $v.casterId='foreign'}
Reject-HotbarUi {param($v) $v.managerObject++}
Reject-HotbarUi {param($v) $v.abilityGroupObject++}
Reject-HotbarUi {param($v) $v.after[0].toggle=$true}
Reject-HotbarUi {param($v) $v.after[1].toggle=$false}
Reject-HotbarUi {param($v) $v.after[1].groupObject++}
Reject-HotbarUi {param($v) $v.after[1].index=0}
Reject-HotbarUi {param($v) $v.opened[0].toggle=$false}
Reject-HotbarUi {param($v) $v.opened[0].type='Spell'}
Reject-HotbarUi {param($v) $v.after=@($v.after[0])}
Reject-HotbarUi {param($v) $v.after[0].toggle=0}
Reject-HotbarUi {param($v) $v.before[1].groupObject=$v.before[0].groupObject}
Write-Output ('HOTBAR UI PASS='+$script:checks+' FAIL=0; synthetic restoration checks only')