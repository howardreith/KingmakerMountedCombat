param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aNativeMammothEvidence.ps1')
function Copy-Profile($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
$pair=[pscustomobject]@{riderId='rider';mountId='mount';riderObject=101;mountObject=102;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';petObject=102;petId='mount';masterObject=101;masterId='rider';riderInState=$true;mountInState=$true;riderIsPartyMember=$true;samePlayerPartyGroup=$true}
$command=[pscustomobject]@{pass=$true;mountId='mount';identity=[pscustomobject]@{casterId='rider';targetId='mount'}}
$rows=@('CM01-exploration-free','CM01-combat-mount-accepted','CM02-approach-arrival','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations')
$profile=[pscustomobject]@{status='PASS';schemaVersion=1;evidenceKind='chunk6a-native-mammoth-profile';scenario='chunk6a-mammoth-mount-rt';errors=@();
 observations=[pscustomobject]@{before=$pair;after=(Copy-Profile $pair);restored=$true;selectionBefore=@('original');selectionAfter=@('original');pauseBefore=$true;pauseAfter=$true;turnBasedBefore=$false;turnBasedAfter=$false;pairedBefore=$true;pairedAfter=$true;childScenario='chunk6a-mammoth-mount-rt';childRows=@(foreach($row in $rows){[pscustomobject]@{name=$row;status='PASS'}})}}
$script:checks=0
function Reject-Profile([scriptblock]$mutation){
 $p=Copy-Profile $profile;$c=Copy-Profile $command;& $mutation $p $c
 $rejected=$false;try{Assert-KmcNativeMammothProfile $p $c}catch{$rejected=$true}
 if(-not $rejected){throw 'Corrupt Mammoth profile accepted'}
 $script:checks++
}
Assert-KmcNativeMammothProfile $profile $command;$script:checks++
Reject-Profile {param($p,$c) $p.status='FAIL'}
Reject-Profile {param($p,$c) $p.evidenceKind='horse'}
Reject-Profile {param($p,$c) $p.scenario='chunk6a-combat-mount-rt'}
Reject-Profile {param($p,$c) $p.errors=@('failure')}
Reject-Profile {param($p,$c) $p.schemaVersion=2}
Reject-Profile {param($p,$c) $p.observations.restored=$false}
Reject-Profile {param($p,$c) $p.observations.selectionAfter=@('other')}
foreach($field in @('pauseAfter','turnBasedAfter','pairedAfter')){Reject-Profile {param($p,$c) $p.observations.$field=-not $p.observations.$field}}
Reject-Profile {param($p,$c) $p.observations.childScenario='foreign'}
Reject-Profile {param($p,$c) $p.observations.before.mountBlueprint='horse'}
Reject-Profile {param($p,$c) $p.observations.after.petObject++}
Reject-Profile {param($p,$c) $c.pass=$false}
Reject-Profile {param($p,$c) $c.mountId='foreign'}
Reject-Profile {param($p,$c) $c.identity.casterId='foreign'}
Reject-Profile {param($p,$c) $c.identity.targetId='foreign'}
foreach($row in $rows){Reject-Profile {param($p,$c) $p.observations.childRows=@($p.observations.childRows|Where-Object name -CNE $row)}}
Reject-Profile {param($p,$c) $p.observations.childRows+=@($p.observations.childRows[0])}
Reject-Profile {param($p,$c) $p.observations.childRows[0].status='FAIL'}
Write-Output ('NATIVE MAMMOTH PROFILE PASS='+$script:checks+' FAIL=0; synthetic identity/child/restoration checks only')