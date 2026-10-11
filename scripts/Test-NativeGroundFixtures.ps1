function Copy-Passive($x){$x|ConvertTo-Json -Depth 60|ConvertFrom-Json}
function New-Passive([bool]$Tb=$false,[bool]$Combat=$true,[bool]$Passing=$false,[bool]$Surprised=$false,[bool]$Waiting=$false,[double]$Initiative=0,[double]$Delta=0.1,[double]$ExpectedDecay=0.1,[double]$ExpectedInitiative=0,[double]$ExpectedReactionCooldown=0,[int]$ExpectedAllowance=1){
 $s=[pscustomobject]@{actor='rider';actorObject=101;inCombat=$Combat;grantSequence=1;standard=2.0;move=1.0;swift=0.8;initiativeCooldown=$Initiative;initiativeOrder=7;reactionCooldown=0.05;reactions=0;reactionsPerRound=1;waitingInitiative=$Waiting}
 $mount=Copy-Passive $s;$mount.actor='mount';$mount.actorObject=102
 $before=[pscustomobject]@{frame=100;gameTicks=1000000L;allocationSequence=10;rider=$s;mount=$mount}
 $after=Copy-Passive $before;$after.frame=101;$after.gameTicks=2000000L;$after.allocationSequence=14
 foreach($actor in @('rider','mount')){
  $last=$after.$actor
  foreach($field in @('standard','move','swift')){$last.$field=[Math]::Max(0.0,[double]$last.$field-$ExpectedDecay)}
  $last.initiativeCooldown=$ExpectedInitiative;$last.reactionCooldown=$ExpectedReactionCooldown;$last.reactions=$ExpectedAllowance
 }
 $events=@();$sequence=10
 foreach($actor in @('rider','mount')){foreach($phase in @('before','after')){
  $state=if($phase -ceq 'before'){Copy-Passive $before.$actor}else{Copy-Passive $after.$actor}
  $events+=@([pscustomobject]@{sequence=++$sequence;frame=101;gameTicks=2000000L;boundary=('cooldown-tick-'+$phase);nativeTurnBased=$Tb;nativePassing=$Passing;nativeSurprised=$Surprised;gameDeltaTime=$Delta;state=$state})
 }}
 [pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';traceComplete=$true;turnBased=$Tb;riderId='rider';mountId='mount';before=$before;after=$after;events=$events;
 observerHooks=@(foreach($t in @('0600934A','060093A1','060093A4','06000C3C','0600C3BE','06009120','0600838F','060026B2','06000C37')){[pscustomobject]@{token=$t;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
}

function New-GroundResources([string]$Mover){
 $p=New-Passive -Combat $false;$p.contract='one-native-ground-command-outside-combat-with-observed-time-only'
 $p|Add-Member groundCommand ([pscustomobject]@{commandObject=333;casterId=$Mover})
 $p|Add-Member cooldownEligibilityContract 'observed-native-outside-combat-skip'
 foreach($role in @('rider','mount')){$p.before.$role.grantSequence=0;$p.after.$role=Copy-Passive $p.before.$role}
 $p.observerHooks+=@([pscustomobject]@{token='06009343';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';method='Kingmaker.Controllers.Combat.BaseUnitCombatController.ShouldTickOnUnit';prefix='CooldownEligibilityBefore';postfix='CooldownEligibilityAfter'})
 $events=@();$index=0
 foreach($boundary in @('admission-before','admission-after','cost-before','cost-after')){
  $events+=@([pscustomobject]@{sequence=11L+$index;frame=$p.before.frame;gameTicks=$p.before.gameTicks;boundary=$boundary;nativeTurnBased=$false;nativePassing=$false;nativeSurprised=$false;gameDeltaTime=0.1;state=(Copy-Passive $p.before.$Mover);command=333;commandActor=$(if($index-eq0){$null}else{$Mover});commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo';actionType='Move';simulatingClick=$false;acted=($index-ge2)})
  $index++
 }
 foreach($role in @('rider','mount')){foreach($phase in @('before','after')){
  $events+=@([pscustomobject]@{sequence=11L+$index;frame=$p.after.frame;gameTicks=$p.after.gameTicks;boundary=('cooldown-eligibility-'+$phase);nativeTurnBased=$false;nativePassing=$false;nativeSurprised=$false;gameDeltaTime=0.1;state=(Copy-Passive $p.before.$role);command=0;callbackObject=700;simulatingClick=$false;detail=$(if($phase-ceq'before'){'Kingmaker.Controllers.Combat.UnitCombatCooldownsController'}else{'eligible=False'})})
  $index++
 }}
 $p.events=$events;$p.after.allocationSequence=10+$events.Count;$p
}
