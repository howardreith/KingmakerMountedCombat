param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-NativeAllocationContinuation.ps1') -Configuration $Configuration
. (Join-Path $PSScriptRoot 'runtime/NativeMountOrderEvidence.ps1')
$orderType=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeMountOrderEvidence',$true)
$orderMethod=$orderType.GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
$restoreMethod=$orderType.GetMethod('AssertRestoration',[Reflection.BindingFlags]'Static,NonPublic')
function New-Order([bool]$First=$true){
 $c=New-Continuation (-not$First)
 $fixture=Copy-Continuation $c.before;foreach($role in @('rider','mount')){$fixture.$role.inCombat=$false;$fixture.$role.grantSequence=0}
 $resources=[pscustomobject]@{rider=$fixture.rider;mount=$fixture.mount}
 $roster=if($First){@([pscustomobject]@{actor='rider'},[pscustomobject]@{actor='mount'})}else{@([pscustomobject]@{actor='mount'},[pscustomobject]@{actor='rider'})}
 $mounted=[pscustomobject]@{round=1;frame=100;gameTicks=1000000L;allocationSequence=10;turnObject=501;currentActor='rider';status='Acting';controllerObject=901;sessionObject=902;turnBased=$true;adoptionCount=1;pairedSplit=$false;pairedSequence=1;pairedFinalized=$false;partnerContextObject=$(if($First){502}else{0});partnerActor=$(if($First){'mount'}else{$null});state=[pscustomobject]@{relationshipState='Mounted';generation=8;ledger=[pscustomobject]@{mountTransitions=1;dismountTransitions=0;forcedDetach=2}};roster=$roster;riderResources=(Copy-Continuation $c.before.rider);mountResources=(Copy-Continuation $c.before.mount)}
 $next=Copy-Continuation $mounted;$next.round=2;$next.frame=$c.after.frame;$next.gameTicks=$c.after.gameTicks;$next.allocationSequence=$c.after.allocationSequence;$next.turnObject=601;$next.pairedSequence=2;$next.partnerContextObject=602;$next.partnerActor='mount';$next.riderResources=Copy-Continuation $c.after.rider;$next.mountResources=Copy-Continuation $c.after.mount
 $prefix=@();$ps=0
 if(-not$First){foreach($b in @('prepare-before','prepare-after','turn-end-after')){$ps++;$prefix+=@([pscustomobject]@{sequence=$ps;boundary=$b;round=1;state=[pscustomobject]@{actor='mount'}})}}
 foreach($b in @('prepare-before','prepare-after')){$ps++;$prefix+=@([pscustomobject]@{sequence=$ps;boundary=$b;round=1;state=[pscustomobject]@{actor='rider'}})}
 if($First){foreach($b in @('prepare-before','prepare-after')){$ps++;$prefix+=@([pscustomobject]@{sequence=$ps;boundary=$b;round=1;state=[pscustomobject]@{actor='mount'}})}}
 while($ps-lt10){$ps++;$prefix+=@([pscustomobject]@{sequence=$ps;boundary='fixture-observation';round=1;state=[pscustomobject]@{actor='fixture'}})}
 $turns=@();if(-not$First){$earlier=Copy-Continuation $mounted;$earlier.currentActor='mount';$earlier.allocationSequence=2;$turns+=@($earlier)};$turns+=@($mounted,$next)
 $result=[pscustomobject]@{contract='fresh-native-allocation-order-through-next-paired-round';scenario=$(if($First){'chunk6a-allocation-rider-first-tb'}else{'chunk6a-allocation-mount-first-tb'});riderFirst=$First;riderId='rider';mountId='mount';disposition=$(if($First){'PreparePartnerThisRound'}else{'RetainPartnerParticipation'});
 fixture=[pscustomobject]@{outsideCombat=$true;targetTurnBased=$true;modeLeaseCurrent=$true;relationshipState='Unmounted';riderOriginalBase=2;mountOriginalBase=3;riderInputBase=$(if($First){40}else{-40});mountInputBase=$(if($First){-40}else{40});beforeResources=$resources;afterResources=(Copy-Continuation $resources)};
 positiveProof=[pscustomobject]@{pass=$true;identityComplete=$true;sameCommandAtEveryBoundary=$true;exactActedObserved=$true;nativeTerminal=$true;nativeResult='Success';initCount=1;identity=[pscustomobject]@{casterId='rider';targetId='mount';abilityGuid='f053faad986631688defa003cd7bda0e';commandType='Move';generationAtInit=7};preClick=[pscustomobject]@{allocationSequence=9};resourceWindow=[pscustomobject]@{pass=$true;reactionResources=[pscustomobject]@{pass=$true}}};
 mountBefore=[pscustomobject]@{round=1;currentTurnActor='rider';riderRosterIndex=$(if($First){0}else{1});mountRosterIndex=$(if($First){1}else{0});rider=[pscustomobject]@{nativeTurnObject=501;nativePrepareCount=1};mount=[pscustomobject]@{nativePrepareCount=$(if($First){0}else{1})}};
 mounted=$mounted;beforeEndInput=(Copy-Continuation $mounted);afterEndInput=(Copy-Continuation $mounted);nextRound=$next;input=[pscustomobject]@{method='Kingmaker.Game.PauseBind';token='06000CB7';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';count=1};
 allocationTrace=[pscustomobject]@{dropped=0;observationErrors=0;events=@($prefix)+@($c.events)};turns=$turns;completion=(Copy-Continuation $c.completion);continuation=$c;
 restoration=[pscustomobject]@{exact=$true;outsideCombat=$true;riderBase=2;mountBase=3}}
 $terminal=[pscustomobject]@{boundary='terminal';frame=$mounted.frame;gameTicks=$mounted.gameTicks;allocationSequence=$mounted.allocationSequence;state=(Copy-Continuation $mounted.state);nativeAllocation=[pscustomobject]@{rider=(Copy-Continuation $c.before.rider);mount=(Copy-Continuation $c.before.mount)}}
 foreach($role in @('rider','mount')){$actor=Copy-Continuation $terminal.nativeAllocation.$role;$actor|Add-Member nativePrepareCount $actor.grantSequence;$actor|Add-Member nativeTurnObject $(if($role-ceq'rider'){501}elseif($First){502}else{0});$terminal.state|Add-Member $role $actor}
 $result.positiveProof|Add-Member samples @($terminal)
 $bridgeBefore=[pscustomobject]@{frame=$terminal.frame;gameTicks=$terminal.gameTicks;allocationSequence=$terminal.allocationSequence;rider=(Copy-Continuation $terminal.nativeAllocation.rider);mount=(Copy-Continuation $terminal.nativeAllocation.mount)}
 $result|Add-Member terminalBridge ([pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';turnBased=$true;traceComplete=$true;riderId='rider';mountId='mount';before=$bridgeBefore;after=(Copy-Continuation $bridgeBefore);events=@();observerHooks=@($c.observerHooks|ForEach-Object {Copy-Continuation $_})})
 $result
}
$script:orderChecks=0
function Check-Order($p,[bool]$expected,[bool]$Restore=$false){
 $producer=$true;try{$args=[object[]]::new(1);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 90 -Compress));$m=if($Restore){$restoreMethod}else{$orderMethod};$null=$m.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{if($Restore){Assert-KmcMountOrderRestoration $p}else{Assert-KmcMountOrder $p}}catch{$external=$false;if($expected){throw}}
 if($producer-ne$expected-or$external-ne$expected){throw ('Mount order producer/external '+$producer+'/'+$external+' expected '+$expected)};$script:orderChecks+=2
}
foreach($first in @($true,$false)){
 $p=New-Order $first;Check-Order $p $true;Check-Order $p $true $true
 function Reject-Order([scriptblock]$Mutate,[bool]$Restore=$false){$x=Copy-Continuation $p;& $Mutate $x;try{Check-Order $x $false $Restore}catch{throw ('Mutation '+$Mutate.ToString()+': '+$_)}}
 # Exact terminal-to-continuation binding rejects otherwise valid case flags.
 foreach($role in @('rider','mount')){
  foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){
   Reject-Order {param($x)$x.positiveProof.samples[0].nativeAllocation.$role.$field++;$x.terminalBridge.before.$role.$field++}
   Reject-Order {param($x)$x.positiveProof.samples[0].state.$role.$field++}
  }
  Reject-Order {param($x)$x.positiveProof.samples[0].state.$role.nativePrepareCount++}
 }
 foreach($field in @('frame','gameTicks','allocationSequence')){Reject-Order {param($x)$x.terminalBridge.before.$field++}}
 Reject-Order {param($x)$x.positiveProof.samples[0].state.rider.nativeTurnObject++}
 Reject-Order {param($x)$x.terminalBridge.turnBased=$false}
 Reject-Order {param($x)$x.terminalBridge.traceComplete=$false}
 Reject-Order {param($x)$x.terminalBridge.observerHooks=@()}
 Reject-Order {param($x)$x.positiveProof.samples[0].state.ledger.forcedDetach++}
 Reject-Order {param($x)$x.positiveProof.samples[0].state.generation++}
 Reject-Order {param($x)$x.positiveProof.samples[0].state.relationshipState='Unmounted'}
 foreach($field in @('pass','identityComplete','sameCommandAtEveryBoundary','exactActedObserved','nativeTerminal')){Reject-Order {param($x)$x.positiveProof.$field=$false}}
 foreach($field in @('casterId','targetId','abilityGuid','commandType')){Reject-Order {param($x)$x.positiveProof.identity.$field='wrong'}}
 Reject-Order {param($x)$x.positiveProof.initCount=2}
 Reject-Order {param($x)$x.positiveProof.resourceWindow.reactionResources.pass=$false}
 foreach($role in @('rider','mount')){
  foreach($field in @('reactions','reactionCooldown','initiativeOrder','initiativeCooldown','standard','move','swift')){
   Reject-Order {param($x)$x.fixture.afterResources.$role.$field++}
   Reject-Order {param($x)$x.continuation.after.$role.$field++}
  }
  Reject-Order {param($x)$x.fixture.beforeResources.$role.grantSequence=1;$x.fixture.afterResources.$role.grantSequence=1}
  Reject-Order {param($x)$x.mountBefore.$role.nativePrepareCount++}
 }
 foreach($field in @('targetTurnBased','modeLeaseCurrent')){Reject-Order {param($x)$x.fixture.$field=$false}}
 foreach($field in @('riderInputBase','mountInputBase')){Reject-Order {param($x)$x.fixture.$field++}}
 foreach($boundary in @('mounted','beforeEndInput','afterEndInput','nextRound')){
  foreach($field in @('controllerObject','sessionObject','adoptionCount')){if($boundary-cne'mounted'){Reject-Order {param($x)$x.$boundary.$field++}}}
  Reject-Order {param($x)$x.$boundary.state.generation++}
  Reject-Order {param($x)$x.$boundary.pairedSplit=$true}
  if($boundary-cne'mounted'){Reject-Order {param($x)$x.$boundary.state.ledger.forcedDetach++}}
 }
 Reject-Order {param($x)$x.nextRound.pairedSequence=3}
 Reject-Order {param($x)$x.nextRound.round=3}
 Reject-Order {param($x)$x.nextRound.turnObject=$x.mounted.turnObject}
 Reject-Order {param($x)$x.nextRound.partnerContextObject=0}
 Reject-Order {param($x)$x.nextRound.pairedFinalized=$true}
 Reject-Order {param($x)$x.mounted.partnerContextObject=$(if($first){0}else{502})}
 Reject-Order {param($x)$x.disposition='wrong'}
 Reject-Order {param($x)$x.mounted.roster=@($x.mounted.roster[1],$x.mounted.roster[0])}
 Reject-Order {param($x)$x.input.count=2}
 Reject-Order {param($x)$x.input.token='wrong'}
 Reject-Order {param($x)$x.beforeEndInput.status='Ending'}
 Reject-Order {param($x)$x.allocationTrace.dropped=1}
 Reject-Order {param($x)$x.allocationTrace.events[0].sequence++}
 Reject-Order {param($x)$x.turns+=@([pscustomobject]@{currentActor='mount';round=1;allocationSequence=10})}
 Reject-Order {param($x)$x.turns=@()}
 Reject-Order {param($x)$x.completion.calls=@($x.completion.calls|Select-Object -Skip 1)}
 Reject-Order {param($x)$x.completion.calls[0].before.turnObject=999}
 Reject-Order {param($x)$x.restoration.exact=$false} $true
 Reject-Order {param($x)$x.restoration.outsideCombat=$false} $true
 Reject-Order {param($x)$x.restoration.riderBase++} $true
 Reject-Order {param($x)$x.restoration.mountBase++} $true
}

# Exact preview130 runtime regression: constructing a C# null string is not
# equivalent to parsing its serialized JSON. Keep the validator strict.
$captureActor=$orderType.GetMethod('CaptureOptionalActor',[Reflection.BindingFlags]'Static,NonPublic')
$implicitString=@([Newtonsoft.Json.Linq.JToken].GetMethods([Reflection.BindingFlags]'Public,Static')|Where-Object {$_.Name-ceq'op_Implicit'-and$_.GetParameters().Count-eq1-and$_.GetParameters()[0].ParameterType-eq[string]})[0]
$oneNull=[object[]]::new(1)
$rawNull=$implicitString.Invoke($null,$oneNull)
if($rawNull.Type-ne[Newtonsoft.Json.Linq.JTokenType]::String-or$null-ne$rawNull.Value){throw 'Pinned typed-string-null reproduction differs'}
$live=[Newtonsoft.Json.Linq.JObject]::Parse(((New-Order $false)|ConvertTo-Json -Depth 90 -Compress))
$live['mounted']['partnerActor']=$rawNull
$invoke=[object[]]::new(1);$invoke[0]=$live
$refused=$false;try{$null=$orderMethod.Invoke($null,$invoke)}catch{
 if($_.Exception.InnerException.Message-cne'Mount allocation order: retained versus prepared partner context differs'){throw}
 $refused=$true
}
if(-not$refused){throw 'Original in-memory failure no longer reproduced'}
$explicitNull=$captureActor.Invoke($null,$oneNull)
if($explicitNull.Type-ne[Newtonsoft.Json.Linq.JTokenType]::Null){throw 'Live observer did not emit explicit JSON null'}
$live['mounted']['partnerActor']=$explicitNull
$null=$orderMethod.Invoke($null,$invoke)
$actorArgs=[object[]]::new(1);$actorArgs[0]='mount';$present=$captureActor.Invoke($null,$actorArgs)
if($present.Type-ne[Newtonsoft.Json.Linq.JTokenType]::String-or$present.Value-cne'mount'){throw 'Present partner identity changed'}
$script:orderChecks+=4

'MOUNT ORDER PRODUCER+EXTERNAL PASS='+$script:orderChecks+' FAIL=0; two synthetic native orders, no Unity or envelope qualification'
