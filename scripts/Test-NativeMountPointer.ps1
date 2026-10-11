param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativeMountPointerEvidence.ps1')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repoRoot 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$pointerMethod=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeMountPointerEvidence',$true).GetMethod('AssertComplete',$flags)
$predictionMethod=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePredictionCommandEvidence',$true).GetMethod('AssertComplete',$flags)
function Copy-Pointer($v){$v|ConvertTo-Json -Depth 70 -Compress|ConvertFrom-Json}
function New-PointerCase {
 $id=[pscustomobject]@{commandObject=101;controlIdentity='shell-actual';casterId='rider';targetId='mount';generationAtInit=4;commandType='Move';abilityGuid='f053faad986631688defa003cd7bda0e';processObject=201;contextObject=301}
 $sid=Copy-Pointer $id;$sid.commandObject=102;$sid.controlIdentity='shell-predicted';$sid.processObject=0;$sid.contextObject=0
 $first=[pscustomobject]@{frame=101;gameTicks=1000L;allocationSequence=10;simulatingClick=$true;commandObject=102;casterId='rider';targetId='mount';commandType='Move';abilityGuid=$id.abilityGuid;started=$false;acted=$false;processObject=0;contextObject=0;identity=$sid}
 $last=Copy-Pointer $first;$last.frame=102;$last.gameTicks=2000;$last.allocationSequence=30;$last.simulatingClick=$false
 $proof=[pscustomobject]@{identity=$id;mountId='mount';preClick=[pscustomobject]@{frame=100;gameTicks=1000L;allocationSequence=0;state=[pscustomobject]@{geometry=[pscustomobject]@{riderPosition=[pscustomobject]@{x=0.0;y=0.0;z=0.0};horsePosition=[pscustomobject]@{x=5.0;y=0.0;z=0.0}}}};samples=@([pscustomobject]@{boundary='init';frame=101;gameTicks=1000L;allocationSequence=20;simulatingClick=$false});
  predictionCommands=[pscustomobject]@{contract='native-speculative-init-separated-from-one-committed-request';commands=@([pscustomobject]@{init=$first;close=$last});pass=$true};
  resourceWindow=[pscustomobject]@{events=@([pscustomobject]@{boundary='admission-after';command=102;sequence=11;simulatingClick=$true;started=$false;acted=$false;state=[pscustomobject]@{actor='rider'}})}}
 $path=[pscustomobject]@{pathObject=701;pathState='Complete';pathError=$false;points=@([pscustomobject]@{x=0.0;y=0.0;z=0.0},[pscustomobject]@{x=3.0;y=0.0;z=0.0});horizontalLength=3.0}
 $samples=@();$names=@('before-set-ability','after-set-ability','native-admission','native-admission','before-real-click','after-real-click')
 for($i=0;$i -lt $names.Count;$i++){
  $samples+=[pscustomobject]@{boundary=$names[$i];frame=$(if($i -lt 3){100}else{101});gameTicks=1000L;turnObject=401;turnActor='rider';turnStatus='Acting';selectedIds=@('rider');casterId='rider';targetId='mount';pointerObject=501;handlerObject=502;selectedHandlerObject=502;abilityObject=601;selectedAbilityObject=$(if($i -eq 0 -or $i -eq 5){0}else{601});abilityGuid=$id.abilityGuid;simulatingClick=$false;ignored=$(if($i -lt 2){$null}else{$i -eq 2});riderCommandsEmpty=($i -ne 5);moveSlotObject=$(if($i -eq 5){101}else{0});riderPosition=@(0.0,0.0,0.0);targetPosition=@(5.0,0.0,0.0);preview=(Copy-Pointer $path)}
 }
 $proof|Add-Member -NotePropertyName nativeApproachPath -NotePropertyValue ([pscustomobject]@{events=@([pscustomobject]@{boundary='preview-before-click';frame=101;turnObject=401;path=(Copy-Pointer $path)},[pscustomobject]@{boundary='preview-return';frame=102;turnObject=401;path=(Copy-Pointer $path)})})
 [pscustomobject]@{input=[pscustomobject]@{contract='native-selected-ability-hover-prediction-and-ignore-click-before-one-commit';casterId='rider';targetId='mount';clicked=$true;ready=$true;restored=$true;samples=$samples};proof=$proof}
}
$script:checks=0
function Check-Pointer($c,[bool]$expected,[string]$label,[bool]$predictionOnly=$false){
 $value=if($predictionOnly){$c.proof.predictionCommands}else{$c.input};$method=if($predictionOnly){$predictionMethod}else{$pointerMethod}
 $producer=$true;try{$args=[object[]]::new(2);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($value|ConvertTo-Json -Depth 70 -Compress));$args[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.proof|ConvertTo-Json -Depth 70 -Compress));$null=$method.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{if($predictionOnly){Assert-KmcNativePredictionCommands $value $c.proof}else{Assert-KmcNativeMountPointer $value $c.proof}}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Pointer producer/external result differs for '+$label+': '+$producer+'/'+$external+' expected '+$expected)};$script:checks+=2
}
function Reject-Pointer([string]$label,[scriptblock]$mutation){$c=New-PointerCase;& $mutation $c;Check-Pointer $c $false $label}
Check-Pointer (New-PointerCase) $true 'one admitted native preview'
$c=New-PointerCase;$c.proof.predictionCommands.commands=@();$c.proof.resourceWindow.events=@();Check-Pointer $c $true 'RT without native prediction' $true
foreach($flag in @('clicked','ready','restored')){Reject-Pointer $flag {param($c)$c.input.$flag=$false}}
foreach($field in @('casterId','targetId','contract')){Reject-Pointer $field {param($c)$c.input.$field='foreign'}}
foreach($field in @('turnObject','pointerObject','handlerObject','abilityObject','selectedAbilityObject','selectedHandlerObject','moveSlotObject')){Reject-Pointer $field {param($c)$c.input.samples[3].$field++}}
foreach($field in @('turnActor','turnStatus','casterId','targetId','abilityGuid','boundary')){Reject-Pointer $field {param($c)$c.input.samples[3].$field='foreign'}}
Reject-Pointer 'foreign selection' {param($c)$c.input.samples[3].selectedIds=@('mount')}
Reject-Pointer 'multiple selection' {param($c)$c.input.samples[3].selectedIds=@('rider','mount')}
Reject-Pointer 'simulation at real click' {param($c)$c.input.samples[4].simulatingClick=$true}
Reject-Pointer 'premature native command' {param($c)$c.input.samples[2].riderCommandsEmpty=$false}
Reject-Pointer 'empty after actual click' {param($c)$c.input.samples[-1].riderCommandsEmpty=$true}
Reject-Pointer 'another actual command' {param($c)$c.input.samples[-1].moveSlotObject=102}
Reject-Pointer 'ignored real click' {param($c)$c.input.samples[3].ignored=$true}
Reject-Pointer 'continues after native admission' {param($c)$c.input.samples[2].ignored=$false}
Reject-Pointer 'changed frame after native admission' {param($c)$c.input.samples[4].frame++}
Reject-Pointer 'changed time after native admission' {param($c)$c.input.samples[4].gameTicks++}
Reject-Pointer 'preview before baseline' {param($c)$c.proof.preClick.frame=101}
foreach($actor in @('riderPosition','targetPosition')){foreach($i in 0..2){Reject-Pointer ($actor+$i) {param($c)$c.input.samples[3].$actor[$i]+=0.1}}}
Reject-Pointer 'missing actual preview' {param($c)$c.proof.nativeApproachPath.events=@()}
Reject-Pointer 'different consumed preview object' {param($c)$c.proof.nativeApproachPath.events[1].path.pathObject++}
Reject-Pointer 'different consumed preview points' {param($c)$c.proof.nativeApproachPath.events[1].path.points[1].x=-15.36}
Reject-Pointer 'incomplete admitted preview' {param($c)$c.input.samples[3].preview.pathState='NotCalculated'}
Reject-Pointer 'failed admitted preview' {param($c)$c.input.samples[3].preview.pathError=$true}
Reject-Pointer 'empty admitted preview' {param($c)$c.input.samples[3].preview.points=@()}
Reject-Pointer 'foreign preview turn' {param($c)$c.proof.nativeApproachPath.events[1].turnObject++}
Reject-Pointer 'no speculative observation' {param($c)$c.proof.predictionCommands.commands=@()}
Reject-Pointer 'speculation shares actual command' {param($c)$c.proof.predictionCommands.commands[0].init.identity.commandObject=101}
Reject-Pointer 'speculation shares actual shell' {param($c)$c.proof.predictionCommands.commands[0].init.identity.controlIdentity='shell-actual'}
Reject-Pointer 'repeated prediction identity' {param($c)$c.proof.predictionCommands.commands+=@(Copy-Pointer $c.proof.predictionCommands.commands[0])}
foreach($field in @('casterId','targetId','commandType','abilityGuid')){Reject-Pointer ('prediction '+$field) {param($c)$c.proof.predictionCommands.commands[0].init.identity.$field='foreign'}}
Reject-Pointer 'prediction generation changed' {param($c)$c.proof.predictionCommands.commands[0].init.identity.generationAtInit++}
foreach($where in @('init','close')){
 foreach($field in @('started','acted')){Reject-Pointer ($where+$field) {param($c)$c.proof.predictionCommands.commands[0].$where.$field=$true}}
 foreach($field in @('processObject','contextObject','commandObject')){Reject-Pointer ($where+$field) {param($c)$c.proof.predictionCommands.commands[0].$where.$field++}}
}
Reject-Pointer 'real command mislabeled prediction' {param($c)$c.proof.predictionCommands.commands[0].init.simulatingClick=$false}
Reject-Pointer 'prediction after real Init' {param($c)$c.proof.predictionCommands.commands[0].init.allocationSequence=21}
Reject-Pointer 'missing temporary admission' {param($c)$c.proof.resourceWindow.events=@()}
Reject-Pointer 'actual admission of speculative command' {param($c)$c.proof.resourceWindow.events[0].simulatingClick=$false}
Reject-Pointer 'actual proof came from simulation' {param($c)$c.proof.samples[0].simulatingClick=$true}
# Since preview.152 the allocation trace records UnitCommand.Interrupt on the rider's commands: a speculative command
# ends through its temporary container's disposal (UnitCommands+Temporary.Dispose), unstarted and unacted, before the
# real Init. Producer and external rule accept exactly that pair and refuse every other interrupt shape.
$disposalCallers='callers=MonoMod.Utils.DynamicMethodDefinition.Kingmaker.UnitLogic.Commands.Base.UnitCommand.Interrupt_Patch3 < Kingmaker.UnitLogic.Commands.UnitCommands.InterruptAll < Kingmaker.UnitLogic.Commands.UnitCommands+Temporary.Dispose < Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.OnPredictClick'
function Add-DisposalInterrupt($c,[string]$Callers=$disposalCallers,[int]$Sequence=12,[bool]$Started=$false,[bool]$Finished=$true,[bool]$WithBefore=$true,[bool]$WithAfter=$true){
 if($WithBefore){$c.proof.resourceWindow.events+=@([pscustomobject]@{boundary='command-interrupt-before';command=102;sequence=$Sequence;simulatingClick=$false;started=$Started;acted=$false;finished=$false;result='None';detail=$Callers;state=[pscustomobject]@{actor='rider'}})}
 if($WithAfter){$c.proof.resourceWindow.events+=@([pscustomobject]@{boundary='command-interrupt-after';command=102;sequence=($Sequence+1);simulatingClick=$false;started=$Started;acted=$false;finished=$Finished;result='Interrupt';detail=$null;state=[pscustomobject]@{actor='rider'}})}
}
$c=New-PointerCase;Add-DisposalInterrupt $c;Check-Pointer $c $true 'temporary container disposal interrupt pair' $true
Reject-Pointer 'foreign interrupt caller' {param($c)Add-DisposalInterrupt $c -Callers 'callers=Kingmaker.UnitLogic.Commands.Base.UnitCommand.Interrupt < Kingmaker.Controllers.Units.UnitActionController.TickCommandTurnBased'}
Reject-Pointer 'interrupt after without its disposal' {param($c)Add-DisposalInterrupt $c -WithBefore $false}
Reject-Pointer 'disposal interrupt after the real Init' {param($c)Add-DisposalInterrupt $c -Sequence 21}
Reject-Pointer 'disposal interrupt of a started speculation' {param($c)Add-DisposalInterrupt $c -Started $true}
Reject-Pointer 'disposal interrupt not finished' {param($c)Add-DisposalInterrupt $c -Finished $false}
foreach($actor in @('rider','mount')){foreach($boundary in @('cost-before','actor-cost-after','prepare-before','clear-before','opportunity-before','approach-movement-before','native-movement-displacement')){
 Reject-Pointer ($actor+' speculative '+$boundary) {param($c)$c.proof.resourceWindow.events+=@([pscustomobject]@{boundary=$boundary;command=0;sequence=12;simulatingClick=$true;state=[pscustomobject]@{actor=$actor}})}
}}
foreach($field in @('frame','gameTicks','allocationSequence')){foreach($where in @('init','close')){Reject-Pointer ('missing '+$where+$field) {param($c)$c.proof.predictionCommands.commands[0].$where.$field=$null}}}
Reject-Pointer 'first pointer moved from baseline' {param($c)foreach($s in $c.input.samples){$s.riderPosition[0]=1.0}}
Reject-Pointer 'first pointer target moved from baseline' {param($c)foreach($s in $c.input.samples){$s.targetPosition[0]=6.0}}
Write-Host "NATIVE MOUNT POINTER SHARED EVIDENCE PASS=$script:checks FAIL=0; synthetic producer/external checks, not Unity qualification."
