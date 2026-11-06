[CmdletBinding()] param()
# F5 reader: synthetic producer/consumer observations, one accepted shape and the refusals that matter.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\Finalize5CompanionEvidence.ps1')
$passes=0
function Check([bool]$Condition,[string]$Message){ if(-not$Condition){throw ('FAIL '+$Message)}; $script:passes++; Write-Host ('PASS '+$Message) }
function Refuses([scriptblock]$Body,[string]$Message){ $threw=$false; try{ & $Body }catch{ $threw=$true }; Check $threw $Message }
function Clone($Value){ $Value|ConvertTo-Json -Depth 20|ConvertFrom-Json }

$shared=$script:KmcF5SharedName
$dogId='0f6c2a1e-1d3b-4c5a-9e8f-7a6b5c4d3e2f'; $mammothId='9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d'
$dogMaster='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'; $mammothMaster='2b4d6f80-1a3c-4e5f-8a9b-0c1d2e3f4a5b'
function Unit([string]$Id,[string]$Master,[string]$Blueprint,[string]$Default,[string]$Name){
    [pscustomobject]@{id=$Id;blueprint=$Blueprint;master=$Master;masterOwnsPet=$true;customName=$Name;characterName=$(if($Name){$Name}else{$Default});defaultName=$Default;eligible=$true}
}
function Dog([string]$Name){ Unit $dogId $dogMaster 'b7a14f25d3c94e0d8e6f1a2c5d90e431' 'Riding Dog' $Name }
function Mammoth([string]$Name){ Unit $mammothId $mammothMaster 'mammothblueprint' 'Mammoth' $Name }
function Labels([string]$Dog,[string]$Mammoth){ [pscustomobject]@{dog=[pscustomobject]@{id=$dogId;characterName=$Dog;customName=$Dog};mammoth=[pscustomobject]@{id=$mammothId;characterName=$Mammoth;customName=$Mammoth}} }
$tack=[pscustomobject]@{dogBindings=1;mammothBindings=0;adopted=$true;rendererEnabled=$true;sameBones=$true;vertices=600;triangles=900;casts=410;misses=3;meshBuilds=1;attachments=1;backAtSeat=0.89;seatTop=0.913;cantleTop=0.95;pommelTop=0.94}
function Shots($Names){ @($Names|ForEach-Object{ [pscustomobject]@{name=$_;file=('movement-visuals/finalize5-'+$_+'.png');bytes=10;sha256=('0'*64)} }) }
$typing=@($shared,'Nobody','<b>Rex</b>',$shared,'Biscuit',$shared|ForEach-Object{ [pscustomobject]@{intended=$_;field=$_;hotkeysSuspended=$true;actorsIdleBefore=$true;actorsIdleAfter=$true} })
$producer=[pscustomobject]@{companionNames=[pscustomobject]@{
    dog=(Dog '');mammoth=(Mammoth '');tackUnmounted=$tack;tackMounted=$tack;tackAfterMotion=$tack
    sheetMammoth=[pscustomobject]@{shown=$mammothId;renameActive=$true;resetActive=$false;renameLabel='Rename';resetLabel='Default Name'}
    sheetMammothRenamed=[pscustomobject]@{shown=$mammothId;renameActive=$true;resetActive=$true;renameLabel='Rename';resetLabel='Default Name'}
    dialogOpen=[pscustomobject]@{shown=$true;prefilled='Mammoth';hotkeysSuspended=$true;inputFocused=$true;actorCommandsIdle=$true}
    typing=$typing;mammothRenamed=(Mammoth $shared);cancelled=(Mammoth $shared);markupMessageShown=$true;markupReopenedWith='<b>Rex</b>';markupRefused=(Mammoth $shared)
    mammothReset=(Mammoth '');mammothRenamedAgain=(Mammoth $shared);nonPetHidden=$true;dogDistinct=(Dog 'Biscuit');distinctLabels=(Labels 'Biscuit' $shared)
    dogDuplicate=(Dog $shared);duplicateLabels=(Labels $shared $shared);atSave=(Labels $shared $shared);sheetClosed=$true;shots=(Shots $script:KmcF5ProducerShots)}}
$request=[pscustomobject]@{persistenceCase='companion-names-tack-manual'}

Assert-KmcFinalize5CompanionNames $request $producer $false; Check $true 'the producer reader accepts the complete naming and tack observation'
Check ($shared.Length-eq14-and[int]$shared[0]-eq0xC9) 'the shared name is the exact accented literal the scenario types'
$p=Clone $producer; $p.companionNames.tackMounted.dogBindings=2; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a duplicated tack refuses'
$p=Clone $producer; $p.companionNames.tackAfterMotion.mammothBindings=1; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'tack on the stock Mammoth refuses'
$p=Clone $producer; $p.companionNames.tackUnmounted.adopted=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a tack the native view did not adopt refuses'
$p=Clone $producer; $p.companionNames.mammothRenamed.id='other'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a changed unit identity refuses'
$p=Clone $producer; $p.companionNames.cancelled.customName='Nobody'; $p.companionNames.cancelled.characterName='Nobody'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a cancel that renamed refuses'
$p=Clone $producer; $p.companionNames.dialogOpen.hotkeysSuspended=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'typing with game hotkeys live refuses'
$p=Clone $producer; $p.companionNames.typing[0].field='bne lAncien'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a dropped glyph refuses'
$p=Clone $producer; $p.companionNames.nonPetHidden=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'Rename offered to a non-companion refuses'
$p=Clone $producer; $p.companionNames.mammothReset.customName=$shared; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a reset that kept the custom name refuses'
$p=Clone $producer; $p.companionNames.shots=@($p.companionNames.shots|Where-Object name -cne 'f5-dialog-typed'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a missing dialog capture refuses'

$consumer=[pscustomobject]@{ridingDogCold=[pscustomobject]@{owner=$dogMaster};loadedArchive=[pscustomobject]@{Pairs=@([pscustomobject]@{Mount=[pscustomobject]@{Id=$dogId}},[pscustomobject]@{Mount=[pscustomobject]@{Id=$mammothId}})}
    companionNamesCold=[pscustomobject]@{dog=(Dog $shared);mammoth=(Mammoth $shared);labels=(Labels $shared $shared);tackCold=$tack;tackRemounted=$tack
        sheetDogCold=[pscustomobject]@{shown=$dogId;renameActive=$true;resetActive=$true};sheetMammothCold=[pscustomobject]@{shown=$mammothId;renameActive=$true;resetActive=$true};sheetClosed=$true;shots=(Shots $script:KmcF5ConsumerShots)}}
Assert-KmcFinalize5CompanionNames $request $consumer $true; Check $true 'the consumer reader accepts persisted duplicate names, identities and one tack'
$c=Clone $consumer; $c.companionNamesCold.labels.dog.characterName='Riding Dog'; $c.companionNamesCold.labels.dog.customName=''; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a name lost on load refuses'
$c=Clone $consumer; $c.companionNamesCold.tackRemounted.dogBindings=2; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a tack duplicated by remount refuses'
$c=Clone $consumer; $c.loadedArchive.Pairs[1].Mount.Id='11111111-2222-4333-8444-555555555555'; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a companion that is not the restored mount refuses'
$c=Clone $consumer; $c.companionNamesCold.dog.master='other'; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a changed owner refuses'
Write-Host ('PASS '+$passes+' F5 companion reader checks')
