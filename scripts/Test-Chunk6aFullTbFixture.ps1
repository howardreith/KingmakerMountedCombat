param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
function Import-TbFixtureFunctions([string]$File,[string[]]$Names){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo $File),[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Fixture parse failed'}
 foreach($name in $Names){$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($f.Count-ne1){throw $name};. ([scriptblock]::Create($f[0].Extent.Text.Replace(('function '+$name),('function script:'+$name))))}
}
Import-TbFixtureFunctions 'scripts/Test-Chunk6aCausalProtocol.ps1' @('Copy-Value','Put-Value','New-CommandProof')
Import-TbFixtureFunctions 'scripts/Test-NativeMountPointer.ps1' @('Copy-Pointer','New-PointerCase')
Import-TbFixtureFunctions 'scripts/Test-NativeMountApproachPath.ps1' @('Copy-PathCase','New-PathCase')
Import-TbFixtureFunctions 'scripts/Test-NativeApproachMovement.ps1' @('Copy-Movement','New-MovementCase')
function New-FullTbProof([bool]$PartnerPrepare=$false){
 $p=New-CommandProof $true $true ([int]$PartnerPrepare);$pointer=New-PointerCase;$path=New-PathCase;$movement=New-MovementCase
 $id=$p.identity;$id.abilityGuid='f053faad986631688defa003cd7bda0e';$id.generationAtInit=7
 $p.preClick.gameTicks=1000000L;$p.preClick.state.generation=7;Put-Value $p.preClick frame 100
 Put-Value $p.preClick.state geometry ([pscustomobject]@{riderPosition=@{x=0.0;y=0.0;z=0.0};horsePosition=@{x=5.0;y=0.0;z=0.0}})
 foreach($field in @('nativeTurnObject','timeMoved','timeForced','timeStepped')){Put-Value $p.preClick.state.rider $field $movement.pre.state.rider.$field}
 # One predicted admission, then two native approach movement callback pairs.
 $prediction=Copy-Value $pointer.proof.predictionCommands
 foreach($phase in @('init','close')){
  $s=$prediction.commands[0].$phase;$s.identity.generationAtInit=7;$s.identity.abilityGuid=$id.abilityGuid;$s.abilityGuid=$id.abilityGuid
  $s.gameTicks=1000000L;$s.frame=100;$s.allocationSequence=0
 }
 $prediction.commands[0].close.frame=101;$prediction.commands[0].close.gameTicks=2000000L;$prediction.commands[0].close.allocationSequence=1
 $p.predictionCommands=$prediction
 $pred=Copy-Value $pointer.proof.resourceWindow.events[0];$pred.sequence=1;Put-Value $pred gameTicks 1000000L;Put-Value $pred.state inCombat $true
 foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){Put-Value $pred.state $field $p.preClick.state.rider.$field}
 $moves=@($movement.events|ForEach-Object{Copy-Value $_});$index=1
 foreach($e in $moves){
  $e.sequence=++$index;$e.frame=if($index-le3){102}else{103};$e.gameTicks=if($index-le3){3000000L}else{4000000L}
  $e.state.move+=0.75;foreach($field in @('standard','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){$e.state.$field=$p.preClick.state.rider.$field}
 }
 $costs=@($p.resourceWindow.events|ForEach-Object{Copy-Value $_})
 foreach($e in $costs){
  if($null-eq$e.PSObject.Properties['command']){Put-Value $e command 0};$e.sequence=++$index;$e.gameTicks=5000000L;Put-Value $e frame 104;Put-Value $e turn 501
  if($e.boundary-cin@('cost-before','actor-cost-before')){$e.state.move=1.125}
  if($e.boundary-cin@('cost-after','actor-cost-after')){$e.state.move=4.125}
 }
 $p.resourceWindow.events=@($pred)+$moves+$costs
 Put-Value $p.resourceWindow nativeApproachMovement ([pscustomobject]@{contract='normal-native-tb-mount-approach';pass=$true;errors=@();observerHooks=$movement.hooks})
 Put-Value $p.resourceWindow pass $true
 foreach($s in $p.samples){
  $s.identity.generationAtInit=7;$s.identity.abilityGuid=$id.abilityGuid;$s.gameTicks=2000000L;Put-Value $s frame 101;$s.allocationSequence=1;$s.state.generation=7
  if($s.boundary-ceq'terminal'){$s.gameTicks=5000000L;$s.frame=104;$s.allocationSequence=$index;$s.state.rider.move=4.125;$s.state.generation=8}
 }
 $p.preClick.state.rider.move=1.0
 foreach($s in $pointer.input.samples){$s.turnObject=501;$s.gameTicks=if($s.frame-eq100){1000000L}else{2000000L}}
 $p|Add-Member nativePointerInput $pointer.input
 foreach($e in $path.path.events){
  $e.frame=101;$e.gameTicks=2000000L;$e.turnObject=501
  if($null-ne$e.path-and$e.path.pathObject-eq201){$e.path.pathObject=701}
 }
 $p|Add-Member nativeApproachPath $path.path
 $p
}
