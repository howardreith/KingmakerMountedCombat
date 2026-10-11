Set-StrictMode -Version Latest
function Assert-KmcNativeTerminalBridge($Proof,$Boundary,$Bridge,[bool]$TurnBased,[string]$Relationship){
 function Int($v){if($v -isnot [int]-and$v -isnot [long]){throw 'Terminal bridge integer missing'};[long]$v}
 function Bool($v){if($v -isnot [bool]){throw 'Terminal bridge boolean missing'};$v}
 function Number($v){if($v -isnot [int]-and$v -isnot [long]-and$v -isnot [double]-and$v -isnot [single]-and$v -isnot [decimal]){throw 'Terminal bridge number missing'};$n=[double]$v;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Terminal bridge number nonfinite'};$n}
 function Same($a,$b){if(($a|ConvertTo-Json -Depth 60 -Compress)-cne($b|ConvertTo-Json -Depth 60 -Compress)){throw 'Terminal bridge evidence copies differ'}}
 $id=$Proof.identity;$rider=$id.casterId;$mount=$Proof.mountId
 if([string]::IsNullOrWhiteSpace($rider)-or[string]::IsNullOrWhiteSpace($mount)-or$rider-ceq$mount){throw 'Terminal bridge pair missing'}
 if((Int $id.generationAtInit)-lt0){throw 'Terminal bridge command generation missing'}
 foreach($field in @('commandObject','processObject','contextObject')){if((Int $id.$field)-eq0){throw 'Terminal bridge command identity incomplete'}}
 foreach($field in @('controlIdentity','casterId','targetId','abilityGuid','commandType')){if([string]::IsNullOrWhiteSpace($id.$field)){throw 'Terminal bridge command text identity missing'}}
 Same $id $Bridge.commandIdentity
 $terminals=@($Proof.samples|Where-Object boundary -CEQ 'terminal');if($terminals.Count-ne1){throw 'Terminal bridge terminal count differs'};$terminal=$terminals[0]
 Same $id $terminal.identity
 if(-not(Bool $terminal.acted)-or-not(Bool $terminal.finished)-or-not(Bool $terminal.processEnded)-or$terminal.result-cne'Success'-or(Bool $terminal.simulatingClick)){throw 'Terminal bridge terminal command/process differs'}
 Assert-KmcNativePassiveResources $Bridge
 if($Bridge.riderId-cne$rider-or$Bridge.mountId-cne$mount-or(Bool $Bridge.turnBased)-ne$TurnBased-or(Bool $Boundary.turnBased)-ne$TurnBased){throw 'Terminal bridge pair/mode binding differs'}
 foreach($field in @('frame','gameTicks','allocationSequence')){if((Int $Bridge.before.$field)-ne(Int $terminal.$field)-or(Int $Bridge.after.$field)-ne(Int $Boundary.$field)){throw 'Terminal bridge clock/sequence binding differs'}}
 if($null-eq$terminal.state.ledger-or(Int $terminal.state.generation)-ne(Int $Boundary.state.generation)){throw 'Terminal bridge relationship ledger/generation differs'}
 Same $terminal.state.ledger $Boundary.state.ledger
 if($terminal.state.relationshipState-cne$Relationship-or$Boundary.state.relationshipState-cne$Relationship){throw 'Terminal bridge relationship boundary differs'}
 foreach($role in @('rider','mount')){
  $native=$terminal.nativeAllocation.$role;$final=$Boundary.($role+'Resources');$before=$terminal.state.$role;$after=$Boundary.state.$role
  if($null-eq$native){throw 'Terminal bridge native allocation missing'};Same $Bridge.before.$role $native;Same $Bridge.after.$role $final
  foreach($pair in @(@($native,$before),@($final,$after))){
   if($pair[0].actor-cne$pair[1].actor-or(Int $pair[0].grantSequence)-ne(Int $pair[1].nativePrepareCount)){throw 'Terminal bridge allocation identity/preparation differs'}
   foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){if([Math]::Abs((Number $pair[0].$field)-(Number $pair[1].$field))-gt0.0001){throw ('Terminal bridge command allocation resource differs '+$field)}}
  }
 }
}
