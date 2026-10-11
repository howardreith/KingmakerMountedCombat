param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/NativeAllocationContinuationEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeAllocationContinuationEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-Continuation($x){$x|ConvertTo-Json -Depth 80|ConvertFrom-Json}
function New-Continuation([bool]$Retained=$false){
 $script:cseq=10L;$script:cord=0;$script:cframe=100;$script:cticks=1000000L;$script:cround=1;$script:cevents=@();$script:ccalls=@()
 $r=[pscustomobject]@{actor='rider';actorObject=101;inCombat=$true;standard=2.0;move=4.2;swift=0.0;reactions=0;reactionsPerRound=1;reactionCooldown=0.5;initiativeOrder=7;initiativeCooldown=0.2;grantSequence=1;waitingInitiative=$false}
 $m=Copy-Continuation $r;$m.actor='mount';$m.actorObject=102;if($Retained){$m.standard=6.0;$m.move=0.0}
 $script:cresources=@{rider=$r;mount=$m}
 function Emit([string]$Actor,[string]$Boundary,[bool]$Passing=$false,[double]$Delta=0.0){
  $script:cseq++;$script:cevents+=@([pscustomobject]@{sequence=$script:cseq;frame=$script:cframe;gameTicks=$script:cticks;round=$script:cround;boundary=$Boundary;nativeTurnBased=$true;nativePassing=$Passing;nativeSurprised=$false;gameDeltaTime=$Delta;state=(Copy-Continuation $script:cresources[$Actor]);callbackObject=$(if($Actor-ceq'rider'){501}else{502});preparingTurn=$(if($Actor-ceq'rider'){601}else{602})})
 }
 function Snapshot([string]$Actor){
  [pscustomobject]@{frame=$script:cframe;gameTicks=$script:cticks;round=$script:cround;allocationSequence=$script:cseq;turnObject=$(if($Actor-ceq'rider'){501}else{502});actorId=$Actor;status='Acting';turnBased=$true;currentContext=($Actor-ceq'rider');partnerContext=($Actor-ceq'mount');completionDebtOwned=$true;conditionForfeitContext=$false;forfeitState=[pscustomobject]@{granted=$true;prepared=$true;forfeitRecorded=$false;forfeitSettled=$false;forfeitStandardAdded=0.0};pairedSequence=1;stepMetres=0.2;stepLimit=1.524;resources=(Copy-Continuation $script:cresources[$Actor])}
 }
 function Start-Call([string]$Actor,[string]$Kind){
  Emit $Actor ('completion-'+$Kind+'-before')
  $script:cord++;$c=[pscustomobject]@{kind=$Kind;setCooldowns=$(if($Kind-ceq'force-to-end'){$true}else{$null});enterOrdinal=$script:cord;exitOrdinal=0;complete=$false;before=(Snapshot $Actor);after=$null}
  if($Kind-ceq'end'){$c.before.stepMetres=1.524;$c.before.status='Ending'}
  $script:ccalls+=@($c);$c
 }
 function Close-Call($c){
  $actor=$c.before.actorId;$s=$script:cresources[$actor]
  if($c.kind-ceq'force-to-end'){$s.standard=6.0;$s.swift=6.0;$s.move=4.2}
  else{$s.standard=6.0}
  Emit $actor ('completion-'+$c.kind+'-after');$script:cord++;$c.exitOrdinal=$script:cord;$c.after=Snapshot $actor;$c.after.stepMetres=1.524;$c.after.status=if($c.kind-ceq'end'){'Ended'}else{'Ending'};$c.complete=$true
 }
 $before=[pscustomobject]@{frame=$script:cframe;gameTicks=$script:cticks;round=1;allocationSequence=$script:cseq;rider=(Copy-Continuation $r);mount=(Copy-Continuation $m)}
 $a=Start-Call 'rider' 'force-to-end'
 if(-not$Retained){$b=Start-Call 'mount' 'force-to-end';Close-Call $b}
 Close-Call $a
 $a=Start-Call 'rider' 'end';Emit 'rider' 'turn-end-before'
 if(-not$Retained){$b=Start-Call 'mount' 'end';Emit 'mount' 'turn-end-before';Emit 'mount' 'turn-end-after';Close-Call $b}
 Emit 'rider' 'turn-end-after';Close-Call $a
 $script:cframe=110;$script:cticks=61000000L
 foreach($actor in @('rider','mount')){
  Emit $actor 'cooldown-tick-before' $true 6.0
  $s=$script:cresources[$actor];$s.initiativeCooldown=0.0;$s.reactionCooldown=0.0
  foreach($f in @('standard','move','swift')){$s.$f=[Math]::Max(0.0,[double]$s.$f-5.8)}
  Emit $actor 'cooldown-tick-after' $true 6.0
 }
 $script:cframe=111;$script:cround=2
 foreach($actor in @('rider','mount')){
  $s=$script:cresources[$actor];$s.grantSequence++;Emit $actor 'prepare-before';Emit $actor 'clear-before'
  foreach($f in @('standard','move','swift','reactionCooldown','initiativeCooldown')){$s.$f=0.0}
  Emit $actor 'clear-after';$s.reactions=1;Emit $actor 'round-before';Emit $actor 'round-after';Emit $actor 'prepare-after'
 }
 $after=[pscustomobject]@{frame=$script:cframe;gameTicks=$script:cticks;round=2;allocationSequence=$script:cseq;rider=(Copy-Continuation $script:cresources.rider);mount=(Copy-Continuation $script:cresources.mount)}
 $completion=[pscustomobject]@{contract='observed-native-turn-completion-writes';traceComplete=$true;riderId='rider';mountId='mount';calls=$script:ccalls;errors=@();observerHooks=@(foreach($t in @('06000C46','06000C47')){[pscustomobject]@{token=$t;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
 [pscustomobject]@{contract='ordinary-paired-end-through-one-native-next-preparation';traceComplete=$true;riderId='rider';mountId='mount';before=$before;after=$after;events=$script:cevents;completion=$completion;
 observerHooks=@(foreach($t in @('06000C3C','0600C3BE','0600934A','060093A1','060093A4','06009120','0600838F','060026B2','06000C37')){[pscustomobject]@{token=$t;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
}
$script:checks=0
function Check-Continuation($p,[bool]$expected){
 $producer=$true;try{$args=[object[]]::new(1);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 80 -Compress));$null=$method.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcAllocationContinuation $p}catch{$external=$false;if($expected){throw}}
 if($producer-ne$expected-or$external-ne$expected){throw ('Continuation producer/external '+$producer+'/'+$external+' expected '+$expected)};$script:checks+=2
}
$p=New-Continuation;Check-Continuation $p $true;Check-Continuation (New-Continuation $true) $true
function Reject-Continuation([scriptblock]$mutate){$x=Copy-Continuation $p;& $mutate $x;try{Check-Continuation $x $false}catch{throw ('Mutation '+$mutate.ToString()+': '+$_)}}
foreach($actor in @('rider','mount')){
 foreach($f in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','grantSequence','actorObject')){
  Reject-Continuation {param($x)$x.after.$actor.$f++}
  Reject-Continuation {param($x)$x.before.$actor.$f++}
  Reject-Continuation {param($x)@($x.events|Where-Object { $_.state.actor-ceq$actor-and$_.boundary-ceq'round-before' })[0].state.$f++}
 }
 Reject-Continuation {param($x)@($x.events|Where-Object { $_.state.actor-ceq$actor-and$_.boundary-ceq'prepare-before' })[0].round=1}
 Reject-Continuation {param($x)@($x.events|Where-Object { $_.state.actor-ceq$actor-and$_.boundary-ceq'prepare-before' })[0].preparingTurn=0}
 Reject-Continuation {param($x)@($x.events|Where-Object { $_.state.actor-ceq$actor-and$_.boundary-ceq'clear-before' })[0].boundary='round-before'}
 Reject-Continuation {param($x)@($x.events|Where-Object { $_.state.actor-ceq$actor-and$_.boundary-ceq'cooldown-tick-before' })[0].gameDeltaTime=5.0}
}
foreach($endpoint in @('before','after')){
 foreach($field in @('granted','prepared','forfeitRecorded','forfeitSettled')){Reject-Continuation {param($x)$x.completion.calls[0].$endpoint.forfeitState.$field=-not$x.completion.calls[0].$endpoint.forfeitState.$field}}
 Reject-Continuation {param($x)$x.completion.calls[0].$endpoint.forfeitState.forfeitStandardAdded=0.5}
 Reject-Continuation {param($x)$x.completion.calls[0].$endpoint.forfeitState=$null}
}
foreach($f in @('frame','round')){
 Reject-Continuation {param($x)$x.events[0].$f--}
 Reject-Continuation {param($x)$x.events[0].$f=9999}
 Reject-Continuation {param($x)$x.completion.calls[0].before.$f++}
}
Reject-Continuation {param($x)$x.completion.calls[0].before.gameTicks++}
foreach($f in @('contract','riderId','mountId')){Reject-Continuation {param($x)$x.$f=$null}}
Reject-Continuation {param($x)$x.traceComplete=$false}
Reject-Continuation {param($x)$x.after.round=3}
Reject-Continuation {param($x)$x.after.frame=$x.before.frame}
Reject-Continuation {param($x)$x.events[0].sequence++}
Reject-Continuation {param($x)$x.events[0].gameTicks=0}
Reject-Continuation {param($x)$x.events[0].nativeTurnBased=$false}
Reject-Continuation {param($x)$x.events[0].callbackObject++}
Reject-Continuation {param($x)$x.events[0].state.actor='other'}
Reject-Continuation {param($x)$x.completion.calls[0].before.allocationSequence++}
Reject-Continuation {param($x)$x.completion.calls[0].after.resources.reactions++}
Reject-Continuation {param($x)$x.completion.calls=@($x.completion.calls|Select-Object -Skip 1)}
foreach($boundary in @('cost-before','actor-cost-after','opportunity-before','admission-after','combat-clear-before','approach-movement-before','native-movement-displacement','remove-unit-before')){Reject-Continuation {param($x)$x.events[0].boundary=$boundary}}
foreach($t in @('06000C3C','0600C3BE','0600934A','060093A1','060093A4','06009120','0600838F','060026B2','06000C37')){Reject-Continuation {param($x)$x.observerHooks=@($x.observerHooks|Where-Object token -CNE $t)}}
Write-Output ('ALLOCATION CONTINUATION PRODUCER+EXTERNAL PASS='+$script:checks+' FAIL=0; lab-only synthetic resource replay, no native qualification')
