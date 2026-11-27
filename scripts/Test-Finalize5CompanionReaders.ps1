[CmdletBinding()] param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
# F5 reader: synthetic producer/consumer observations, one accepted shape and the refusals that matter.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\Finalize5CompanionEvidence.ps1')
$passes=0
function Check([bool]$Condition,[string]$Message){ if(-not$Condition){throw ('FAIL '+$Message)}; $script:passes++; Write-Host ('PASS '+$Message) }
function Refuses([scriptblock]$Body,[string]$Message){ $threw=$false; try{ & $Body }catch{ $threw=$true }; Check $threw $Message }
function Clone($Value){ $Value|ConvertTo-Json -Depth 20|ConvertFrom-Json }

$shared=$script:KmcF5SharedName

# The harness reads the game's evidence (UTF-8 without a byte-order mark) back as UTF-8: r06 was refused when it read ANSI.
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$utf8Probe=[IO.Path]::Combine([IO.Path]::GetTempPath(),'kmc-f5-utf8-'+[Guid]::NewGuid().ToString('N')+'.json')
try{
    [IO.File]::WriteAllBytes($utf8Probe,(New-Object Text.UTF8Encoding($false)).GetBytes('{"customName":"'+$shared+'"}'))
    Check ([string](Read-KmcJson $utf8Probe).customName-ceq$shared) 'the harness JSON reader keeps a game-written accented name intact'
}finally{ Remove-Item -LiteralPath $utf8Probe -Force -ErrorAction SilentlyContinue }
$dogId='0f6c2a1e-1d3b-4c5a-9e8f-7a6b5c4d3e2f'; $mammothId='9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d'
$dogMaster='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'; $mammothMaster='2b4d6f80-1a3c-4e5f-8a9b-0c1d2e3f4a5b'
function Unit([string]$Id,[string]$Master,[string]$Blueprint,[string]$Default,[string]$Name){
    [pscustomobject]@{id=$Id;blueprint=$Blueprint;master=$Master;masterOwnsPet=$true;customName=$Name;characterName=$(if($Name){$Name}else{$Default});defaultName=$Default;eligible=$true}
}
function Dog([string]$Name){ Unit $dogId $dogMaster 'b7a14f25d3c94e0d8e6f1a2c5d90e431' 'Riding Dog' $Name }
function Mammoth([string]$Name){ Unit $mammothId $mammothMaster 'mammothblueprint' 'Mammoth' $Name }
function Labels([string]$Dog,[string]$Mammoth){ [pscustomobject]@{dog=[pscustomobject]@{id=$dogId;characterName=$Dog;customName=$Dog};mammoth=[pscustomobject]@{id=$mammothId;characterName=$Mammoth;customName=$Mammoth};writes=0;lastOutcome=$null} }
# The overtip text as native SetName formats it: the first letter in the decorative saber font.
function Overtip([string]$Id,[string]$Name){ [pscustomobject]@{id=$Id;controllerUnit=$Id;characterName=$Name;text=('<font="Saber_Dist32"><color=#672B31><size=140%>'+$Name[0]+'</size></color></font>'+$Name.Substring(1))} }
function Overtips([string]$Name){ [pscustomobject]@{dog=(Overtip $dogId $Name);mammoth=(Overtip $mammothId $Name);writes=0;lastOutcome=$null} }
$tack=[pscustomobject]@{dogBindings=1;mammothBindings=0;adopted=$true;rendererEnabled=$true;sameBones=$true;vertices=600;triangles=900;casts=410;misses=3;meshBuilds=1;attachments=1;backAtSeat=0.89;seatTop=0.913;cantleTop=0.95;pommelTop=0.94;maxRise=0.06;minClearance=0.002;maxClearance=0.051;unsupported=0
    dogStats=[pscustomobject]@{id=$dogId;size='Medium';armorClass=19;speed=40}}
# One Rename control at the right end of the name line (sheet canvas units at 1280x720), clear of the level/experience texts.
function Box([double]$X0,[double]$Y0,[double]$X1,[double]$Y1){ [pscustomobject]@{xMin=$X0;yMin=$Y0;xMax=$X1;yMax=$Y1} }
function Sheet([string]$Id){
    [pscustomobject]@{shown=$Id;renameActive=$true;renameLabel='Rename';layout='name-line-right';buttons=1;reservedMargin=@(93,0,93,0)
        line=(Box 47 640 437 666);control=(Box 352 646 437 660);nameText=(Box 190 642 300 664);levelTexts=@((Box 59 622 134 636),(Box 223 622 273 636),(Box 352 622 435 636))
        controlInsideLineHorizontally=$true;controlCentredOnLine=$true;textClearOfControl=$true;controlClearOfLevel=$true;textClearOfLevel=$true;textOverflowing=$false;fontSize=30}
}
function Shots($Names){ @($Names|ForEach-Object{ [pscustomobject]@{name=$_;file=('movement-visuals/finalize5-'+$_+'.png');bytes=10;sha256=('0'*64)} }) }
$typing=@($shared,'Nobody','Nobody','<b>Rex</b>','   ',$script:KmcF5LongName,$shared,'Biscuit',$shared|ForEach-Object{ [pscustomobject]@{intended=$_;field=$_;hotkeysSuspended=$true;actorsIdleBefore=$true;actorsIdleAfter=$true} })
function Point([double]$X,[double]$Z){ [pscustomobject]@{x=$X;y=6.096;z=$Z} }
function Moves { @([pscustomobject]@{label='first-pair';executorId=$mammothId;goal=(Point 3.87 50.17)},[pscustomobject]@{label='second-pair';executorId=$dogId;goal=(Point -0.05 56.2)}) }
function Plans { [pscustomobject]@{'second-pair'=[pscustomobject]@{moverId=$dogId;selectedAngle=0;selectedGoal=(Point -0.05 56.2);proposals=@([pscustomobject]@{angle=90;eligible=$false},[pscustomobject]@{angle=0;eligible=$true})}} }
$producer=[pscustomobject]@{companionNames=[pscustomobject]@{
    dog=(Dog '');mammoth=(Mammoth '');tackUnmounted=$tack;tackMounted=$tack;tackAfterMotion=$tack
    sheetMammoth=(Sheet $mammothId);sheetMammothRenamed=(Sheet $mammothId);sheetMammothLong=(Sheet $mammothId);sheetDog=(Sheet $dogId)
    dialogOpen=[pscustomobject]@{shown=$true;prefilled='Mammoth';hotkeysSuspended=$true;inputFocused=$true;actorCommandsIdle=$true}
    typing=$typing;mammothRenamed=(Mammoth $shared);cancelled=(Mammoth $shared);cancelReleasedKeyboard=$true;cancelButton=(Mammoth $shared);markupMessageShown=$true;markupReopenedWith='<b>Rex</b>';markupRefused=(Mammoth $shared)
    blankMessageShown=$true;blankReopenedWith='   ';blankRefused=(Mammoth $shared);mammothLong=(Mammoth $script:KmcF5LongName)
    ummEntry=[pscustomobject]@{windowOpen=$true;tab=0;id='KingmakerMountedCombat';displayName="Mr. Bard's Kingmaker Mounted Combat";version='0.2.1';active=$true;optionsPanel=$false;chargeEnabled=$true}
    mammothRenamedAgain=(Mammoth $shared);nonPetHidden=$true;riderNameUnstyled=$true;dogDistinct=(Dog 'Biscuit');distinctLabels=(Labels 'Biscuit' $shared)
    dogDuplicate=(Dog $shared);duplicateLabels=(Labels $shared $shared);atSave=(Labels $shared $shared);overtipsAtSave=(Overtips $shared);sheetClosed=$true;shots=(Shots $script:KmcF5ProducerShots)}}
$producer | Add-Member movements (Moves); $producer | Add-Member areaContinuationPlans (Plans)
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
$p=Clone $producer; $p.companionNames.cancelReleasedKeyboard=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a cancel that kept the keyboard refuses'
$p=Clone $producer; $p.companionNames.nonPetHidden=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'Rename offered to a non-companion refuses'
$p=Clone $producer; $p.companionNames.blankRefused.customName=''; $p.companionNames.blankRefused.characterName='Mammoth'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a blank entry that restored the default name refuses'
$p=Clone $producer; $p.companionNames.blankMessageShown=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a blank entry with no explanation refuses'
$p=Clone $producer; $p.companionNames.cancelButton.customName='Nobody'; $p.companionNames.cancelButton.characterName='Nobody'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a Cancel button that renamed refuses'
$p=Clone $producer; $p.companionNames.sheetMammothRenamed|Add-Member resetActive $true; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a sheet that still offers Default Name refuses'
$p=Clone $producer; $p.companionNames.sheetMammothLong.buttons=2; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a duplicated Rename control refuses'
$p=Clone $producer; $p.companionNames.sheetMammothLong.controlClearOfLevel=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a Rename control over the level line refuses'
$p=Clone $producer; $p.companionNames.sheetMammothLong.nameText.yMin=620; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a name wrapped below its own line into the level row refuses'
$p=Clone $producer; $p.companionNames.sheetMammothRenamed.textClearOfLevel=$false; $p.companionNames.sheetMammothRenamed.nameText.yMin=641; Assert-KmcFinalize5CompanionNames $request $p $false; Check $true 'a native-size name whose glyph box touches the level text but stays in its line is accepted (r01 renamed Mammoth)'
$p=Clone $producer; $p.companionNames.sheetMammothLong.textClearOfControl=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a long name running under the control refuses'
$p=Clone $producer; $p.companionNames.sheetDog.control.yMin=622; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a control that moved for another companion refuses'
$p=Clone $producer; $p.companionNames.ummEntry.optionsPanel=$true; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a UMM entry with an options page refuses'
# PR-4 rounds 13-15 (R12-F6, R13-F1, R14-F2): the compiled product's own observer. The built KingmakerMountedCombat.dll for the requested
# -Configuration (forwarded by Test-Tiers.ps1 through Test-MmrHorseTripFocused.ps1) is loaded with the installed game assemblies, as the native
# contracts load it; a delegate is bound to the compiled Main.OnMaintenanceGui and passed through the production
# Chunk7PersistenceScenario.UmmPanelObservation; its returned fields are what the reader receives. No UI is drawn.
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$productDll=Join-Path $repoRoot ('bin\'+$Configuration+'\KingmakerMountedCombat.dll')
if(-not(Test-Path -LiteralPath $productDll -PathType Leaf)){throw ('The '+$Configuration+' product is not built: '+$productDll)}
$layout=(Get-Content -Raw -LiteralPath (Join-Path ([IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))) 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$managed=Join-Path (Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data') 'Managed'
$script:resolving=@{}
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender,$eventArgs)
    $short=($eventArgs.Name -split ',')[0]
    if($short.EndsWith('.resources') -or $script:resolving.ContainsKey($short)){return $null}
    $loaded=@([AppDomain]::CurrentDomain.GetAssemblies()|Where-Object{$_.GetName().Name -eq $short})|Select-Object -First 1
    if($loaded){return $loaded}
    $script:resolving[$short]=$true
    try{ foreach($dir in @($managed,(Join-Path $managed 'UnityModManager'))){ $path=Join-Path $dir ($short+'.dll'); if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::LoadFile($path)} }; return $null }
    finally{ $script:resolving.Remove($short) } })
$kmc=[Reflection.Assembly]::LoadFile($productDll)
Check ([string]::Equals($kmc.Location,$productDll,[StringComparison]::OrdinalIgnoreCase)) ('loaded product artifact: configuration='+$Configuration+' path='+$kmc.Location+' sha256='+(Get-FileHash -LiteralPath $productDll -Algorithm SHA256).Hash.ToLowerInvariant()+' mvid='+$kmc.ManifestModule.ModuleVersionId)
$any=[Reflection.BindingFlags]'Static,Public,NonPublic'
$mainType=$kmc.GetType('KingmakerMountedCombat.Main',$true)
$authorized=$mainType.GetMethod('OnMaintenanceGui',$any)
$observer=$kmc.GetType('KingmakerMountedCombat.Diagnostics.Chunk7PersistenceScenario',$true).GetMethod('UmmPanelObservation',$any)
Check ($null -ne $authorized -and $null -ne $observer) 'the compiled Main.OnMaintenanceGui and Chunk7PersistenceScenario.UmmPanelObservation are present'
$modEntryType=$authorized.GetParameters()[0].ParameterType
$onGuiType=$modEntryType.GetField('OnGUI').FieldType
$maintenance=[Delegate]::CreateDelegate($onGuiType,$authorized)
# An unrelated static handler of the same UMM callback type (not the product's), for the unrelated and mixed controls.
Add-Type -TypeDefinition 'public static class KmcUmmUnrelatedHandler { public static void Draw(UnityModManagerNet.UnityModManager.ModEntry entry) { } }' -ReferencedAssemblies $modEntryType.Assembly.Location
$other=[Delegate]::CreateDelegate($onGuiType,[KmcUmmUnrelatedHandler].GetMethod('Draw'))
function Observe($Callback){ $fields=[bool[]]$observer.Invoke($null,[object[]]@(,$Callback)); [pscustomobject]@{optionsPanel=$fields[0];maintenancePanel=$fields[1]} }
function WithPanel($Observation){ $p=Clone $producer; $p.companionNames.ummEntry.optionsPanel=$Observation.optionsPanel; $p.companionNames.ummEntry|Add-Member -NotePropertyName maintenancePanel -NotePropertyValue $Observation.maintenancePanel; $p }
$o=Observe $maintenance; Check ($o.optionsPanel -eq $false -and $o.maintenancePanel -eq $true) 'the compiled observer reports the compiled Main.OnMaintenanceGui as the maintenance panel only'
Assert-KmcFinalize5CompanionNames $request (WithPanel $o) $false; Check $true 'the reader accepts the compiled observation of the repair-only maintenance callback'
$o=Observe $other; Check ($o.optionsPanel -eq $true -and $o.maintenancePanel -eq $false) 'the compiled observer reports an unrelated handler as an options panel'
Refuses { Assert-KmcFinalize5CompanionNames $request (WithPanel $o) $false } 'the reader refuses the compiled observation of an unrelated OnGUI callback (an options page)'
$o=Observe ([Delegate]::Combine($other,$maintenance)); Check ($o.optionsPanel -eq $true -and $o.maintenancePanel -eq $false) 'a mixed callback (unrelated, then maintenance) is an options panel'
Refuses { Assert-KmcFinalize5CompanionNames $request (WithPanel $o) $false } 'the reader refuses the mixed callback (unrelated, then maintenance)'
$o=Observe ([Delegate]::Combine($maintenance,$other)); Check ($o.optionsPanel -eq $true -and $o.maintenancePanel -eq $false) 'a mixed callback (maintenance, then unrelated) is an options panel'
Refuses { Assert-KmcFinalize5CompanionNames $request (WithPanel $o) $false } 'the reader refuses the mixed callback (maintenance, then unrelated)'
$o=Observe $null; Check ($o.optionsPanel -eq $false -and $o.maintenancePanel -eq $false) 'no callback is neither panel'
$p=Clone $producer; $p.companionNames.ummEntry|Add-Member -NotePropertyName maintenancePanel -NotePropertyValue 'yes'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a non-boolean maintenance-panel observation refuses'
Check (-not ($producer.companionNames.ummEntry.PSObject.Properties.Name -contains 'maintenancePanel')) 'the historical no-panel observation (no maintenancePanel field) is the accepted base fixture'
$p=WithPanel (Observe $maintenance); $p.companionNames.ummEntry.chargeEnabled=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'with the maintenance panel, Mounted Charge off still refuses'
$p=WithPanel (Observe $maintenance); $p.companionNames.ummEntry.active=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'with the maintenance panel, an inactive entry still refuses'
$p=Clone $producer; $p.companionNames.ummEntry.displayName='Kingmaker Mounted Combat'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'the old UMM display name refuses'
$p=Clone $producer; $p.companionNames.ummEntry.chargeEnabled=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'Mounted Charge off at startup refuses'
$p=Clone $producer; $p.companionNames.riderNameUnstyled=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a character name that kept the reserved slot refuses'
$p=Clone $producer; $p.companionNames.tackMounted.seatTop=0.88; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a seat buried in the back refuses (r03: 0.734 under 0.863)'
$p=Clone $producer; $p.companionNames.tackMounted.pommelTop=1.175; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a pommel 30 cm over the back refuses (r03)'
$p=Clone $producer; $p.companionNames.tackAfterMotion.maxRise=0.21; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a fin over a correctly placed seat refuses'
$p=Clone $producer; $p.companionNames.tackUnmounted.seatTop=0.96; $p.companionNames.tackUnmounted.cantleTop=0.965; $p.companionNames.tackUnmounted.pommelTop=0.962; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a seat floating 7 cm over the back refuses'
$p=Clone $producer; $p.companionNames.tackUnmounted.PSObject.Properties.Remove('maxRise'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a fit without the rise diagnostic (r01-r03 evidence shape) refuses'
$p=Clone $producer; $p.companionNames.tackUnmounted.minClearance=-0.132; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a girth 13 cm into the chest refuses (r04 replay)'
$p=Clone $producer; $p.companionNames.tackMounted.unsupported=39; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'pad shards with no body under them refuse (r04 replay)'
$p=Clone $producer; $p.companionNames.tackAfterMotion.maxClearance=0.17; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a pad point 17 cm off the flank refuses (r04 replay)'
$p=Clone $producer; $p.companionNames.tackUnmounted.PSObject.Properties.Remove('minClearance'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a fit without the clearance diagnostic (r01-r05 evidence shape) refuses'
$p=Clone $producer; $p.companionNames.tackMounted.minClearance=$null; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a clearance that was not finite refuses'
$p=Clone $producer; $p.companionNames.tackMounted.minClearance=-0.004; $p.companionNames.tackMounted.maxClearance=0.079; Assert-KmcFinalize5CompanionNames $request $p $false; Check $true 'clearance just inside the envelope (-4 mm, 7.9 cm) is accepted'
$p=Clone $producer; $p.companionNames.tackMounted.minClearance=-0.006; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a point 6 mm inside the body refuses'
$p=Clone $producer; $p.companionNames.tackMounted.maxClearance=0.081; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a point 8.1 cm off the coat refuses'
$p=Clone $producer; $p.companionNames.tackMounted.unsupported=1; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a single unsupported vertex refuses'
$p=Clone $producer; $p.companionNames.tackMounted.PSObject.Properties.Remove('maxClearance'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a fit without its largest clearance refuses'
$p=Clone $producer; $p.companionNames.tackMounted.PSObject.Properties.Remove('unsupported'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a fit without its unsupported count refuses'
$p=Clone $producer; $p.companionNames.tackMounted.unsupported=$null; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a null unsupported count refuses'
$p=Clone $producer; $p.areaContinuationPlans.'second-pair'.selectedGoal.z=58.2; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a continuation click its plan did not choose refuses'
$p=Clone $producer; $p.areaContinuationPlans.'second-pair'.moverId=$mammothId; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a continuation plan for another unit refuses'
$p=Clone $producer; $p.areaContinuationPlans.'second-pair'.proposals[1].eligible=$false; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a continuation plan whose chosen ray was ineligible refuses'
$p=Clone $producer; $p.PSObject.Properties.Remove('areaContinuationPlans'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a producer without its continuation plan (r05 shape) refuses'
$p=Clone $producer; $p.companionNames.tackMounted.dogStats.armorClass=21; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a dog whose armour class changed under the tack refuses'
$p=Clone $producer; $p.companionNames.overtipsAtSave.dog.text='Riding Dog'; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'an overtip still showing the default name refuses'
$p=Clone $producer; $p.companionNames.overtipsAtSave.mammoth.controllerUnit=$dogId; Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'an overtip bound to the other companion refuses'
$p=Clone $producer; $p.companionNames.shots=@($p.companionNames.shots|Where-Object name -cne 'f5-dialog-typed'); Refuses { Assert-KmcFinalize5CompanionNames $request $p $false } 'a missing dialog capture refuses'

$consumer=[pscustomobject]@{ridingDogCold=[pscustomobject]@{owner=$dogMaster};loadedArchive=[pscustomobject]@{Pairs=@([pscustomobject]@{Mount=[pscustomobject]@{Id=$dogId}},[pscustomobject]@{Mount=[pscustomobject]@{Id=$mammothId}})}
    companionNamesCold=[pscustomobject]@{dog=(Dog $shared);mammoth=(Mammoth $shared);labels=(Labels $shared $shared);overtipsCold=(Overtips $shared);tackCold=$tack;tackRemounted=$tack
        sheetDogCold=(Sheet $dogId);sheetMammothCold=(Sheet $mammothId);sheetClosed=$true;shots=(Shots $script:KmcF5ConsumerShots)}}
$consumer | Add-Member movements (Moves); $consumer | Add-Member areaContinuationPlans (Plans)
Assert-KmcFinalize5CompanionNames $request $consumer $true; Check $true 'the consumer reader accepts persisted duplicate names, identities and one tack'
$c=Clone $consumer; $c.companionNamesCold.labels.dog.characterName='Riding Dog'; $c.companionNamesCold.labels.dog.customName=''; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a name lost on load refuses'
$c=Clone $consumer; $c.companionNamesCold.tackRemounted.dogBindings=2; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a tack duplicated by remount refuses'
$c=Clone $consumer; $c.loadedArchive.Pairs[1].Mount.Id='11111111-2222-4333-8444-555555555555'; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a companion that is not the restored mount refuses'
$c=Clone $consumer; $c.companionNamesCold.dog.master='other'; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a changed owner refuses'
$c=Clone $consumer; $c.companionNamesCold.labels.writes=1; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'cold names observed after a naming-service write refuse'
$c=Clone $consumer; $c.companionNamesCold.overtipsCold.mammoth.text='Mastodon'; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a cold overtip showing the default name refuses'
$c=Clone $consumer; $c.companionNamesCold.tackRemounted.dogStats.speed=30; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a cold dog whose speed changed refuses'
$c=Clone $consumer; $c.companionNamesCold.tackCold.maxRise=0.3; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a cold fit outside the envelope refuses'
$c=Clone $consumer; $c.companionNamesCold.tackCold.unsupported=12; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a cold rebuilt tack with shards refuses'
$c=Clone $consumer; $c.areaContinuationPlans.'second-pair'.selectedGoal.x=1.5; Refuses { Assert-KmcFinalize5CompanionNames $request $c $true } 'a cold continuation click its plan did not choose refuses'
# Cross-run: the consumer against its producer's recorded values.
$manual='5'*64
$preq=[pscustomobject]@{scenario='chunk7-persistence-save';persistenceCase='companion-names-tack-manual';commit='abc';branch='codex/kmc-finalize-f5-names-dog-tack';dllSha256=('d'*64);dllMvid='m'}
$creq=[pscustomobject]@{scenario='chunk7-persistence-load';persistenceCase='companion-names-tack-manual';commit='abc';branch='codex/kmc-finalize-f5-names-dog-tack';dllSha256=('d'*64);dllMvid='m'
    persistenceLoad=[pscustomobject]@{fileName='Manual_300_KMC_C7_WORLD.zks';sha256=$manual;length=857874}}
$pobs=Clone $producer; $pobs|Add-Member nativeWrite ([pscustomobject]@{path='C:\x\Saved Games\Manual_300_KMC_C7_WORLD.zks';sha256=$manual;length=857874;nativeType='Manual'})
$pev=[pscustomobject]@{status='PASS';observations=$pobs}; $cev=[pscustomobject]@{status='PASS';observations=(Clone $consumer)}
Assert-KmcFinalize5ColdAgainstSource $preq $pev $creq $cev; Check $true 'the cross-run reader accepts the consumer of its own producer'
$x2=Clone $creq; $x2.persistenceLoad.sha256='6'*64; Refuses { Assert-KmcFinalize5ColdAgainstSource $preq $pev $x2 $cev } 'a consumer of another archive refuses'
$x2=Clone $creq; $x2.dllSha256='e'*64; Refuses { Assert-KmcFinalize5ColdAgainstSource $preq $pev $x2 $cev } 'a consumer on another binary refuses'
$y=Clone $cev; $y.observations.companionNamesCold.labels.dog.id=$mammothId; $y.observations.companionNamesCold.labels.mammoth.id=$dogId; Refuses { Assert-KmcFinalize5ColdAgainstSource $preq $pev $creq $y } 'duplicate names that exchanged their units refuse'
$y=Clone $cev; $y.observations.companionNamesCold.tackCold.dogStats.armorClass=17; Refuses { Assert-KmcFinalize5ColdAgainstSource $preq $pev $creq $y } 'a dog whose armour class changed across the reload refuses'
$y=Clone $pev; $y.status='FAIL'; Refuses { Assert-KmcFinalize5ColdAgainstSource $preq $y $creq $cev } 'a failed producer is not a source'
Write-Host ('PASS '+$passes+' F5 companion reader checks')
