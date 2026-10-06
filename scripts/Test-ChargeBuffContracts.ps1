[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$lab=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$data=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$script:passed=0
function Check([bool]$Value,[string]$Label){if(-not$Value){throw ('Native Charge asset contract: '+$Label)};$script:passed++;Write-Host ('PASS '+$Label)}
function IntAt($Reader,[long]$Offset){[void]$Reader.BaseStream.Seek($Offset,[IO.SeekOrigin]::Begin);$Reader.ReadInt32()}
function PtrAt($Reader,[long]$Offset,[int]$File,[long]$Path){[void]$Reader.BaseStream.Seek($Offset,[IO.SeekOrigin]::Begin);($Reader.ReadInt32()-eq$File)-and($Reader.ReadInt64()-eq$Path)}
function UnityString($Reader){
 $length=$Reader.ReadInt32();if($length-lt0-or$length-gt256){throw 'Unexpected bounded Unity string'}
 $text=[Text.Encoding]::UTF8.GetString($Reader.ReadBytes($length))
 [void]$Reader.BaseStream.Seek((4-$Reader.BaseStream.Position%4)%4,[IO.SeekOrigin]::Current)
 $text
}
$assets=Join-Path $data 'sharedassets1.assets'
$scripts=Join-Path $data 'globalgamemanagers.assets'
Check ((Get-FileHash -LiteralPath $assets).Hash.ToLowerInvariant()-ceq'cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785') 'pinned sharedassets1 identity'
Check ((Get-FileHash -LiteralPath $scripts).Hash.ToLowerInvariant()-ceq'633c82b4869b8879556496b58efd1de78a5bee26ab5dd1d77643952913e574d2') 'pinned MonoScript asset identity'
$reader=[IO.BinaryReader]::new([IO.File]::OpenRead($assets))
$mono=[IO.BinaryReader]::new([IO.File]::OpenRead($scripts))
try {
 Check (PtrAt $reader (157852952+16632) 0 92567) 'BlueprintRoot SystemMechanics references exact ChargeBuff object'
 Check (PtrAt $reader (158237792+16) 1 2948) 'ChargeBuff managed script pointer'
 [void]$reader.BaseStream.Seek(158237792+28,[IO.SeekOrigin]::Begin)
 Check ((UnityString $reader)-ceq'ChargeBuff') 'exact native Charge buff name'
 Check ((IntAt $reader (158237792+56))-eq3) 'exactly three authored native components'
 $paths=@(198150,243977,200402)
 for($i=0;$i-lt3;$i++){Check (PtrAt $reader (158237792+60+12*$i) 0 $paths[$i]) ('exact component order '+$i)}
 [void]$reader.BaseStream.Seek(158237792+96,[IO.SeekOrigin]::Begin)
 Check ((UnityString $reader)-ceq'f36da144a379d534cad8e21667079066') 'exact Charge buff GUID'
 foreach($entry in @(
  @(158237792,2948,531208,'BlueprintBuff','Kingmaker.UnitLogic.Buffs.Blueprints'),
  @(183110328,4792,770200,'AddStatBonus','Kingmaker.UnitLogic.FactLogic'),
  @(189361984,2314,448104,'AddCondition','Kingmaker.UnitLogic.FactLogic'),
  @(183417688,4707,759024,'AttackOfOpportunityAttackBonus','Kingmaker.Designers.Mechanics.Facts')
 )){
  Check (PtrAt $reader ($entry[0]+16) 1 $entry[1]) ('native component MonoScript pointer '+$entry[3])
  [void]$mono.BaseStream.Seek($entry[2],[IO.SeekOrigin]::Begin)
  Check ((UnityString $mono)-ceq$entry[3]) ('MonoScript name '+$entry[3])
  [void]$mono.BaseStream.Seek(20,[IO.SeekOrigin]::Current) # execution order and native hash
  $class=UnityString $mono;$namespace=UnityString $mono;$assembly=UnityString $mono
  Check ($class-ceq$entry[3]-and$namespace-ceq$entry[4]-and$assembly-ceq'Assembly-CSharp.dll') ('exact managed native component '+$class)
 }
 foreach($field in @(@(96,0,'Descriptor'),@(100,11,'Stat AC'),@(104,-2,'Value'),@(108,0,'ScaleByBasicAttackBonus'))){
  Check ((IntAt $reader (183110328+$field[0]))-eq$field[1]) ('AddStatBonus '+$field[2])
 }
 Check ((IntAt $reader (189361984+96))-eq40) 'AddCondition StealthForbidden'
 foreach($field in @(@(112,1,'NotAttackOfOpportunity'),@(116,1,'AttackBonus'),@(120,0,'Descriptor'),@(124,0,'Simple'),@(128,2,'Value'),@(132,0,'Default rank'),@(136,0,'Shared'),@(140,0,'Property'))){
  Check ((IntAt $reader (183417688+$field[0]))-eq$field[1]) ('AttackOfOpportunityAttackBonus '+$field[2])
 }
 Check (PtrAt $reader (183417688+144) 0 0) 'AoO component has no custom property'
 Check (PtrAt $reader (163031088+288) 0 25498) 'Flaming enchantment references the finite weapon FX root'
 Check (PtrAt $reader (163031392+184) 0 0) 'FlamingBurst enchantment has no FX prefab'
 foreach($entry in @(
  @(127387344,83,'d0d51c13d638f02b66fb5199128dca24e6a5072ee4355bda96f6bfe4690074e3','Flaming root'),
  @(209148632,160,'8cd554bbf6693374b3835f7b067cdd22c9a6d8021ac319114d3238f0e9e0cfbd','single locator'),
  @(210650632,52,'2584b52feb0d6207400489dd3fc4e997e29c7af258835f8e38503bc7dc2636bb','native fade'),
  @(211951296,32,'4c8a36d722bf9cbdf02d86877a4251fb6fdb16a708e4c90799e05c7ad0544b1a','native pooled FX')
 )){
  [void]$reader.BaseStream.Seek($entry[0],[IO.SeekOrigin]::Begin)
  $sha=[Security.Cryptography.SHA256]::Create()
  try{$actual=([BitConverter]::ToString($sha.ComputeHash($reader.ReadBytes($entry[1])))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
  Check ($actual-ceq$entry[2]) ('exact bounded original '+$entry[3]+' input bytes')
 }
 [void]$reader.BaseStream.Seek(210650632+32,[IO.SeekOrigin]::Begin)
 Check ($reader.ReadSingle()-eq1.5) 'Flaming native fade is finite 1.5 seconds'
 Check ((IntAt $reader (209148632+124))-eq1) 'Flaming has exactly one locator name and no multi-locator clone branch'
 [void]$reader.BaseStream.Seek(209148632+128,[IO.SeekOrigin]::Begin)
 Check ((UnityString $reader)-ceq'Locator_WeaponCenterFX_00') 'Flaming native weapon locator identity'
} finally {$reader.Dispose();$mono.Dispose()}
Write-Host ('NATIVE CHARGE BUFF ASSET PASS='+$script:passed+' FAIL=0; read-only installed asset contracts, not gameplay qualification')
