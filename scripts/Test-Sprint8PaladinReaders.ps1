# Sprint 8 reader: synthetic positive and negative checks. No game, no build. A reader-only assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-S8($Value){ $Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json }
function New-S8Pet([int]$Rank,[int]$Celestial=0,[int]$Greater=0,[int]$Sr=0,[string[]]$Reduction=@()){
 $levels=@(0,2,3,3,4,5,6,6,7,8,9,9,10,11,12,12,13,14,15,15,16)
 [pscustomobject]@{id='pet';blueprint='4016c7db400ab721ff125aef9e65e202';master='owner';characterLevel=$levels[$Rank];canLevelUp=$false;intelligence=6;intelligenceBond=1;upgrade=$true;celestial=$Celestial;greaterCelestial=$Greater;spellResistance=$Sr;reductionSources=@($Reduction);smiteFact=[int]($Celestial+$Greater);smiteAbility=($Celestial+$Greater -gt 0);smiteUses=[int]($Celestial+$Greater)} }
function New-S8Mount([int]$Level,[int]$Uses,$Pet){ [pscustomobject]@{id='owner';paladinLevel=$Level;rank=$Level;size='Medium';weaponBond=$false;weaponUpgradeRanks=0;weaponAbilities=0;weaponResources=0;mountBond=$true;mountChoice='Horse';addPetFacts=1;ownedPets=1;callAbility=$true;callUses=1;callMax=$Uses;pet=$Pet;bondSelectionFact=$false} }
function New-S8Weapon([int]$Upgrades){ [pscustomobject]@{id='owner';paladinLevel=9;rank=0;weaponBond=$true;weaponUpgradeRanks=$Upgrades;weaponAbilities=2;weaponResources=2;mountBond=$false;mountChoice=$null;addPetFacts=0;ownedPets=0;callAbility=$false;callUses=0;pet=$null;bondSelectionFact=$true} }
function New-S8Offer([hashtable]$Items){ [pscustomobject]@{level=5;completeBeforeChoice=$false;items=@($Items.Keys|ForEach-Object{[pscustomobject]@{guid=$_;canSelect=$Items[$_]}})} }
$refusal=@([pscustomobject]@{name='x';accepted=$false})
$bond=New-S8Offer @{'1c7cdc1605554954f838d85bbdd22d90'=$true;'b14c9d27e6a3480fa97e03d5c18f6a42'=$true}
$medium=New-S8Offer @{'d97c20e5a84b4f16b3e87a1604cd2f95'=$true;'47ea1f96b0c24d38a6f5d2c83e917b04'=$false;'472091361cf118049a2b4339c4ea836a'=$false}
function New-S8MountRow(){ [pscustomobject]@{levels=@([pscustomobject]@{nextLevel=5;DivineBondSelection=$bond;DivineMountSelection=$medium;DivineMountSelectionIllegalAttempts=$refusal;DivineMountSelectionDuplicateAccepted=$false;DivineBondSelectionIllegalAttempts=@();DivineBondSelectionDuplicateAccepted=$false;after=(New-S8Mount 5 1 (New-S8Pet 5))});
 petCommits=@(1..8|ForEach-Object{[pscustomobject]@{committed=$true}});
 boundaries=[pscustomobject]@{'8'=[pscustomobject]@{after=New-S8Mount 8 1 (New-S8Pet 8)};'9'=[pscustomobject]@{after=New-S8Mount 9 2 (New-S8Pet 9)};
  '11'=[pscustomobject]@{after=New-S8Mount 11 2 (New-S8Pet 11 1 0 0 @('AddDamageResistancePhysical=5','AddDamageResistanceEnergy=10','AddDamageResistanceEnergy=10','AddDamageResistanceEnergy=10'))};'13'=[pscustomobject]@{after=New-S8Mount 13 3 (New-S8Pet 13 0 1 0 @('AddDamageResistancePhysical=10','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15'))};
  '15'=[pscustomobject]@{after=New-S8Mount 15 3 (New-S8Pet 15 0 1 26 @('AddDamageResistancePhysical=10','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15'))};'17'=[pscustomobject]@{after=New-S8Mount 17 4 (New-S8Pet 17 0 1 28 @('AddDamageResistancePhysical=10','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15','AddDamageResistanceEnergy=15'))}}} }
function Accepts([scriptblock]$Check,$Value){ try{ & $Check $Value; $true }catch{ Write-Output ('      '+$_.Exception.Message); $false } }
function Refuses([scriptblock]$Check,$Value,[string]$Name){ try{ & $Check $Value; Check $false ('refused: '+$Name) }catch{ Check $true ('refused: '+$Name) } }
$row=New-S8MountRow
Check (Accepts ${function:Assert-KmcS8MountCase} $row) 'a complete mount progression is accepted'
$r=Copy-S8 $row; $r.levels[0].after.weaponBond=$true; Refuses ${function:Assert-KmcS8MountCase} $r 'a hidden weapon bond on the mount path'
$r=Copy-S8 $row; $r.levels[0].after.weaponUpgradeRanks=1; Refuses ${function:Assert-KmcS8MountCase} $r 'a weapon upgrade regranted on the mount path'
$r=Copy-S8 $row; $r.boundaries.'8'.after.ownedPets=2; Refuses ${function:Assert-KmcS8MountCase} $r 'a second animal'
$r=Copy-S8 $row; $r.boundaries.'9'.after.rank=2; Refuses ${function:Assert-KmcS8MountCase} $r 'the Ranger''s reduced rate'
$r=Copy-S8 $row; $r.boundaries.'11'.after.pet.characterLevel=2; Refuses ${function:Assert-KmcS8MountCase} $r 'available experience without an actual pet level'
$r=Copy-S8 $row; $r.boundaries.'15'.after.pet.spellResistance=0; Refuses ${function:Assert-KmcS8MountCase} $r 'no spell resistance at 15'
$r=Copy-S8 $row; $r.boundaries.'11'.after.pet.celestial=0; Refuses ${function:Assert-KmcS8MountCase} $r 'no celestial facts at 11'
$r=Copy-S8 $row; $r.boundaries.'13'.after.pet.celestial=1; Refuses ${function:Assert-KmcS8MountCase} $r 'both celestial tiers at once'
$r=Copy-S8 $row; $r.boundaries.'9'.after.callMax=1; Refuses ${function:Assert-KmcS8MountCase} $r 'a missing call use'
$r=Copy-S8 $row; $r.levels[0].DivineMountSelection.items=@($r.levels[0].DivineMountSelection.items|ForEach-Object{$_.canSelect=$true;$_}); Refuses ${function:Assert-KmcS8MountCase} $r 'a Medium rider offered the Riding Dog'
$r=Copy-S8 $row; $r.levels[0].DivineMountSelectionIllegalAttempts[0].accepted=$true; Refuses ${function:Assert-KmcS8MountCase} $r 'an illegal choice the controller accepted'
$r=Copy-S8 $row; $r.levels[0].DivineMountSelectionIllegalAttempts=@(); Refuses ${function:Assert-KmcS8MountCase} $r 'a Medium choice with no illegal attempt'
$r=Copy-S8 $row; $r.levels[0].DivineMountSelectionDuplicateAccepted=$true; Refuses ${function:Assert-KmcS8MountCase} $r 'a duplicate selection'
$r=Copy-S8 $row; $r.boundaries.PSObject.Properties.Remove('15'); Refuses ${function:Assert-KmcS8MountCase} $r 'a missing observation is not a PASS'
$w=[pscustomobject]@{levels=@([pscustomobject]@{nextLevel=5;DivineBondSelection=$bond;after=(New-S8Weapon 0)});final=(New-S8Weapon 2)}
$w.final.paladinLevel=9
Check (Accepts ${function:Assert-KmcS8WeaponCase} $w) 'a complete weapon path is accepted'
$r=Copy-S8 $w; $r.final.ownedPets=1; Refuses ${function:Assert-KmcS8WeaponCase} $r 'an animal on the weapon path'
$r=Copy-S8 $w; $r.final.weaponUpgradeRanks=0; Refuses ${function:Assert-KmcS8WeaponCase} $r 'lost weapon upgrades'
$r=Copy-S8 $w; $r.levels[0].DivineBondSelection.completeBeforeChoice=$true; Refuses ${function:Assert-KmcS8WeaponCase} $r 'a level that never waited for the choice'
Check ((Get-KmcPhase3dEvidenceDispatch 52 'sprint8-paladin-mount-rt').validator -ceq 'Assert-KmcSprint8PaladinEvidence') 'schema 52 dispatches to the Sprint 8 reader'
foreach($s in 'sprint8-paladin-choice-rt','sprint8-paladin-mount-rt','sprint8-paladin-dog-rt','sprint8-paladin-mixed-rt'){ Check (Test-KmcPhase3dSchemaRegistration 52 $s) ($s+' registers schema 52'); Check (Test-KmcCompoundRuntimeScenario $s) ($s+' is a compound scenario') }
Check (-not (Test-KmcPhase3dSchemaRegistration 51 'sprint8-paladin-mount-rt')) 'a Sprint 7 schema is refused for a Sprint 8 scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 52 'sprint7-charge-rt')) 'the Sprint 8 schema is refused for a Sprint 7 scenario'
Check ('paladin-bond-exploration-manual' -cin (Get-KmcChunk7PersistenceCases)) 'the Paladin save case is registered'
Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ throw 'Sprint 8 reader checks failed.' }
