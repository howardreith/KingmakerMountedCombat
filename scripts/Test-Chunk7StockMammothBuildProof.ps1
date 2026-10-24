[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$rider=[pscustomobject]@{id='73681ef2-b281-4bc6-bc05-2de68c1616cb';object=1;descriptor=3};$pet=[pscustomobject]@{id='10000000-0000-0000-0000-000000000001';object=2};$r=[pscustomobject]@{runId='review-stock'}
$b=[pscustomobject]@{actorId=$rider.id;actorObject=1;descriptorObject=3;petId=$pet.id;petObject=2;petBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';level=10;rangerLevel=10;xp=105000;money=1;nativeCanLevelUp=$false}
$choices=@();for($i=0;$i-lt7;$i++){$choices+=[pscustomobject]@{event='native-level-committed';runId=$r.runId;commitCount=($i+1);before=[pscustomobject]@{actorId=$rider.id;actorObject=1;descriptorObject=3;level=($i+3)};after=[pscustomobject]@{actorId=$rider.id;actorObject=1;descriptorObject=3;level=($i+4);xp=105000}}}
$choices+=[pscustomobject]@{event='native-stock-mammoth-rank-seven';runId=$r.runId;rangerLevel=10;companionRank=7;riderSize='Medium';mountSize='Large';upgradeGuid='6a23d16a4476af644af89d91f9f96790';featureGuid='6adc3aab7cde56b40aa189a797254271';petGuid='e7aa96d15a45238438ae4cfb476f6bb9'}
$choices+=[pscustomobject]@{event='native-builder-complete';runId=$r.runId;actor=($b|ConvertTo-Json|ConvertFrom-Json);ordinaryChoicesComplete=$true;nativeFieldAdmissionProven=$false;nativeSaveSettlementProven=$false;poseQualified=$false}
for($i=0;$i-lt7;$i++){$choices+=[pscustomobject]@{event='native-level-open';runId=$r.runId}}
$o=[pscustomobject]@{initialMoney=1;nativeBuild=$b;stockMammoth=[pscustomobject]@{riderId=$rider.id;petId=$pet.id;riderObject=1;petObject=2;petBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';featureGuid='6adc3aab7cde56b40aa189a797254271';rankGuid='1670990255e4fe948a863bafd5dbda5d';upgradeGuid='6a23d16a4476af644af89d91f9f96790';riderSize='Medium';petSize='Large';featureSource='ee63330662126374e8785cc901941ac7';rank=7;rangerLevel=10;upgradePresent=$true;petMasterId=$rider.id};choices=$choices}
$script:p=0;$script:f=0
function Check([string]$Name,[scriptblock]$Mutation,[bool]$Reject=$false){$x=$o|ConvertTo-Json -Depth 30|ConvertFrom-Json;&$Mutation $x;$caught=$false;try{Assert-KmcStockMammothBuild $x $r $rider $pet}catch{$caught=$true;if(-not$Reject){Write-Output ('FAIL '+$Name+': '+$_)}};if($caught-eq$Reject){$script:p++;Write-Output ('PASS '+$Name)}else{$script:f++;if($Reject){Write-Output ('FAIL accepted '+$Name)}}}
Check 'C#-shaped seven commits and exact sourced rank-seven completion' {param($x)}
Check 'foreign choice run rejected' {param($x)$x.choices[0].runId='foreign-run'} $true
Check 'foreign committed actor rejected' {param($x)$x.choices[0].after.actorId='ffffffff-ffff-ffff-ffff-ffffffffffff'} $true
Check 'foreign committed object rejected' {param($x)$x.choices[0].before.actorObject=9} $true
Check 'foreign completion actor rejected' {param($x)$x.choices[8].actor.actorId='ffffffff-ffff-ffff-ffff-ffffffffffff'} $true
Check 'missing genuine Mammoth upgrade rejected' {param($x)$x.stockMammoth.upgradePresent=$false} $true
Write-Output ('TOTAL PASS='+$p+' FAIL='+$f);if($f){exit 1}
